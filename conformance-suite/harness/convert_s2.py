#!/usr/bin/env python3
"""convert_s2.py — slice S2's converter (DESIGN-conformance-suite.md §6 step 1, §13 S2; kb/Work PB3177).

    python conformance-suite/harness/convert_s2.py [--dry-run] [--map PATH]

⛔ A ONE-TIME TOOL. It is committed on the move's branch for review and DELETED before the branch lands (§13 S2: the
move is its output). It stays on the branch until then because the branch re-runs it after merging main at the quiet
point, so it is INCREMENTAL: it converts every program it finds under `tests/conformance/` into the suite and leaves
everything already in `conformance-suite/` alone, so a re-run converts exactly the programs main added meanwhile.

WHAT IT DOES, per program (an enabled or pending positive of `tests/conformance/<edition>/`, an enabled negative of
`tests/conformance/negative/`):
  * its TREE: `wiseowl/cases/` when it witnesses documented non-support (every inventory row citing it is
    DOCUMENTED-NON-SUPPORT, or its `.err` names a declined facility's code), or it compiles under a WiseOwl-only option
    (`*> options: sign-encoding=` or `flag-extensions=`, §3.3); otherwise `cases/`.
  * its CLAUSE FOLDER: the clause of the first inventory row citing it (for a positive, the first whose `editions`
    band holds its edition), climbed past the standard's generic sub-headings (General, General format(s), Syntax
    rules, General rules) to the heading that names a language element, `<clause>-<Name>`; `_unassigned` when no row
    cites it.
  * its HEADER (§3.1): `id` (its base name; a base name two programs share takes a suffix, `_<edition>` or `_neg`,
    unless the programs and their outputs are identical, when they are one test accepted at both editions), `rules`
    (those rows), `accept` (its directory's edition; none for a negative), `files` (the library text its COPY
    statements locate, recursively, and the files beside it they name), `source-format` (its `*> options:`
    source-format, else what the `--source-format auto` detector the old runner compiled under reads:
    ReferenceFormatProcessor.IsFixedForm, ported below), `portable: unverified`. The negative's `*> reject-at:` line
    and every `*> options:` line are removed (they are now `wiseowl/diagnostics.json` and `wiseowl/options.json`).
  * a negative's refusal goes to `wiseowl/diagnostics.json` (its reject-at editions; its `.err`, code or message
    substring), a WiseOwl-only compile option to `wiseowl/options.json`.
  * every `conformance:<edition>/<name>` citation of the traceability inventory (test-ref and notes) becomes
    `conformance:<tree>/<folder>/<id>` (the ref grammar of tests/version-matrix/inventory-schema.json).
  * the old files are deleted; `tests/conformance/` with them once it is empty of programs.
The mapping (old program -> new test, editions) is written to `--map` for the neutrality proof.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field
from pathlib import Path

HARNESS = Path(__file__).resolve().parent
SUITE = HARNESS.parent
REPO = SUITE.parent
OLD = REPO / "tests" / "conformance"
sys.path.insert(0, str(HARNESS))
sys.path.insert(0, str(REPO / "scripts" / "spec"))

import cobol_source as cobol  # noqa: E402
import inventory_schema  # noqa: E402

EDITIONS = ["85", "2002", "2014", "2023"]
#: The codes WiseOwl reports for a declined facility (docs/CONFORMANCE.md §1): an unclaimed A.4 module refused
#: (COBOLNET1560 data/environment, COBOLNET1707 procedure) and the declined accept-inert warnings.
DECLINED_CODES = ("COBOLNET1560", "COBOLNET1707", "COBOLNET1578", "COBOLNET1579", "COBOLNET1580", "COBOLNET2424")
GENERIC_HEADINGS = {"general", "general format", "general formats", "syntax rule", "syntax rules", "general rule",
                    "general rules"}
TRAILING_NOUNS = {"statement", "directive", "clause", "entry", "paragraph", "function", "phrase"}
LIBRARY_SUFFIXES = (".CPY", ".CBL", ".COB", ".cpy", ".cbl", ".cob")
#: A header line stays inside margin R (column 72) in fixed form and well inside free form's 255 positions.
HEADER_WIDTH = 72
#: The run-time output that names a source line (EXCEPTION-LOCATION, the debugging special register): the programs
#: whose expected output moves with the header the move adds above their code.
LINE_REPORTING = re.compile(r"EXCEPTION-LOCATION|DEBUG-LINE|DEBUG-ITEM", re.IGNORECASE)
COBOL = REPO / "src" / "Cobol.Net.Cli" / "bin" / "Debug" / "net10.0" / ("cobol.exe" if os.name == "nt" else "cobol")
OPTIONS = "*> options:"
REJECT_AT = "reject-at:"
WISEOWL_OPTIONS = ("sign-encoding", "flag-extensions")


# ── the --source-format auto detector the old runner compiled under ─────────────────────────────────────────────────

_SOURCE_DIRECTIVE = re.compile(r"^>>\s*SOURCE\b", re.IGNORECASE)


def _first_source_directive(lines: list[str]) -> int:
    """ReferenceFormatProcessor.FirstSourceDirectiveCandidate: the first line that is a >>SOURCE directive in either
    reading."""
    for i, line in enumerate(lines):
        if _SOURCE_DIRECTIVE.match(line.lstrip()) or _SOURCE_DIRECTIVE.match(line[7:].lstrip()):
            return i
    return len(lines)


def is_fixed_form(text: str) -> bool:
    """ReferenceFormatProcessor.IsFixedForm over the text before the first >>SOURCE candidate (a port, used only by this
    converter, whose output the in-process runner then compiles in the stated form: a wrong reading is a red)."""
    lines = [ln.rstrip("\r").expandtabs(8) for ln in text.split("\n")]
    lines = lines[:_first_source_directive(lines)]
    fixed = total = 0
    numeric = glyph = past = False
    for line in lines:
        if not line.strip():
            continue
        total += 1
        if line[72:].strip():
            past = True
        if len(line) > 6 and line[6] in " */Dd-":
            seq = line[:6]
            if all(c.isdigit() or c == " " for c in seq):
                fixed += 1
                if any(c.isdigit() for c in seq):
                    numeric = True
                if line[6] != " ":
                    glyph = True
    shape = numeric or (glyph and not past)
    return total > 0 and shape and fixed * 100 // total > 60


# ── the spec's headings ──────────────────────────────────────────────────────────────────────────────────────────────

def headings() -> dict[str, str]:
    out = {}
    for m in re.finditer(r"(?m)^#{2,7} ((?:[0-9]+|[A-Z])(?:\.[0-9]+)*) (.+?)\s*$", (REPO / "specs" / "ISO_COBOL.md").read_text(
            encoding="utf-8")):
        out.setdefault(m.group(1), m.group(2))
    return out


def clause_folder(section: str, titles: dict[str, str]) -> str:
    parts = section.strip().strip(".").split(".")
    while len(parts) > 1 and titles.get(".".join(parts), "").strip().rstrip(".").lower() in GENERIC_HEADINGS:
        parts.pop()
    while len(parts) > 1 and ".".join(parts) not in titles:
        parts.pop()
    number = ".".join(parts)
    title = re.sub(r"\(.*?\)", "", titles.get(number, "Clause"))
    words = [w for w in re.split(r"[^A-Za-z0-9]+", title) if w]
    if len(words) > 1 and words[-1].lower() in TRAILING_NOUNS:
        words.pop()
    return f"{number}-{'-'.join(words[:6]) or 'Clause'}"


# ── the old corpus ──────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass
class Program:
    directory: str  # an edition or "negative"
    name: str
    text: str
    out: str | None
    err: str | None
    reject_at: list[str]
    options: dict[str, str]
    fmt: str = "fixed"
    rows: list[str] = field(default_factory=list)
    files: list[str] = field(default_factory=list)  # names in the old directory
    tree: str = "cases"
    folder: str = "_unassigned"
    test_id: str = ""
    editions: list[str] = field(default_factory=list)  # accepted at

    @property
    def ref(self) -> str:
        return f"{self.directory}/{self.name}"

    @property
    def negative(self) -> bool:
        return self.directory == "negative"


def leading_comment_options(text: str) -> dict[str, str]:
    """The `*> options:` declarations of the leading comment block (ConformanceCorpus.ApplySourceOptions' reading)."""
    found: dict[str, str] = {}
    for raw in text.split("\n"):
        line = raw.strip()
        if not line:
            continue
        if not line.startswith("*>"):
            break
        if line.startswith(OPTIONS):
            for token in line[len(OPTIONS):].split():
                key, _, value = token.partition("=")
                found[key] = value
    return found


def strip_wiseowl_lines(text: str, negative: bool) -> str:
    """The program without its `*> reject-at:` first line (a negative) and its leading `*> options:` lines."""
    lines = text.split("\n")
    out = []
    leading = True
    for i, raw in enumerate(lines):
        line = raw.strip()
        if i == 0 and negative and REJECT_AT in line:
            continue
        if leading and line and not line.startswith("*>"):
            leading = False
        if leading and line.startswith(OPTIONS):
            continue
        out.append(raw)
    return "\n".join(out)


def load_programs() -> list[Program]:
    programs = []
    for d in EDITIONS + ["negative"]:
        base = OLD / d
        if not base.is_dir():
            continue
        manifest = base / "manifest.json"
        names = None
        if manifest.is_file():
            m = json.loads(manifest.read_text(encoding="utf-8"))
            names = list(m.get("enabled", [])) + list(m.get("pending", []))
        for cob in sorted(base.glob("*.cob")):
            if names is not None and cob.stem not in names:
                print(f"  NOTE {d}/{cob.name}: on disk, in no manifest list: converted as enabled")
        for cob in sorted(base.glob("*.cob")):
            text = cob.read_bytes().decode("utf-8")  # no newline translation: a lone CR is a character (DOC-A.1-156)
            out = base / (cob.stem + ".out")
            err = base / (cob.stem + ".err")
            first = text.split("\n")[0]
            reject = first.split(REJECT_AT, 1)[1].split() if d == "negative" and REJECT_AT in first else []
            if d == "negative" and not reject:
                raise SystemExit(f"{d}/{cob.name}: a negative without its '*> reject-at:' first line")
            programs.append(Program(
                directory=d, name=cob.stem, text=text,
                out=out.read_bytes().decode("utf-8") if out.is_file() else None,
                err=err.read_bytes().decode("utf-8").strip() if err.is_file() else None,
                reject_at=reject, options=leading_comment_options(text)))
    return programs


def locate(directory: Path, name: str) -> Path | None:
    """The library text a COPY names, as the old runner's --copy <edition directory> located it (DOC-A.1-40: the
    name as spelled when it holds a period, else with .CPY .CBL .COB .cpy .cbl .cob in that order), or None."""
    candidates = [name] if "." in name else [name + s for s in LIBRARY_SUFFIXES]
    on_disk = {p.name.lower(): p for p in directory.iterdir() if p.is_file()} if directory.is_dir() else {}
    for c in candidates:
        if (directory / c).is_file() and c in {p.name for p in directory.iterdir()}:
            return directory / c
    for c in candidates:
        if c.lower() in on_disk:
            return on_disk[c.lower()]
    return None


def copied_files(program: Program, problems: list[str]) -> list[str]:
    """The library texts the program's COPY statements locate in its old directory, recursively (old-directory-relative
    names, a library subdirectory included)."""
    directory = OLD / program.directory
    found: list[str] = []
    queue = [program.text]
    seen: set[str] = set()
    while queue:
        text = queue.pop()
        tokens = cobol.tokenize(cobol.read_lines(text, program.fmt))
        for stmt in cobol.copy_statements(tokens):
            library = _library_name(tokens, stmt)
            look = directory / library if library else directory
            path = locate(look, stmt.name)
            if path is None:
                problems.append(f"{program.ref}: COPY {stmt.name}{' OF ' + library if library else ''} locates no "
                                f"library text in {look.relative_to(REPO).as_posix()}")
                continue
            rel = path.relative_to(directory).as_posix()
            if rel not in seen:
                seen.add(rel)
                found.append(rel)
                queue.append(cobol.decode(path.read_bytes()))
            # The texts the search passed over are what makes it witness its order (DOC-A.1-40): every other file
            # the name could have located, in the library and, for a COPY naming one, in the default library.
            for other in [*candidates(look, stmt.name), *(candidates(directory, stmt.name) if library else [])]:
                if (rel_other := other.relative_to(directory).as_posix()) not in seen:
                    seen.add(rel_other)
                    found.append(rel_other)
    return found


def candidates(directory: Path, name: str) -> list[Path]:
    """Every file in `directory` the library-text search for `name` tries (locate's spellings)."""
    if not directory.is_dir():
        return []
    spellings = {s.lower() for s in ([name] if "." in name else [name + s for s in LIBRARY_SUFFIXES])}
    return sorted(p for p in directory.iterdir() if p.is_file() and p.name.lower() in spellings)


def _library_name(tokens: list[cobol.Token], stmt: cobol.CopyStatement) -> str | None:
    for i, t in enumerate(tokens):
        if t.line == stmt.line and t.kind == cobol.WORD and t.upper == "COPY" and i + 3 < len(tokens):
            nxt = tokens[i + 2]
            if nxt.kind == cobol.WORD and nxt.upper in ("OF", "IN"):
                lib = tokens[i + 3]
                return lib.text[1:-1] if lib.kind == cobol.ALPHANUMERIC else lib.text
            return None
    return None


# ── the conversion ──────────────────────────────────────────────────────────────────────────────────────────────────

def header(program: Program) -> str:
    """The metadata block (§3): one line per field, a long list continued on indented lines within HEADER_WIDTH."""
    prefix = "      " if program.fmt == "fixed" else ""

    def item(v: str) -> str:
        return v if re.fullmatch(r"[^\s\[\]{},:\"]+", v) else '"' + v.replace('"', '""') + '"'

    def field(name: str, values: list[str] | None = None, scalar: str | None = None) -> list[str]:
        if scalar is not None:
            return [f"*> {name}: {scalar}"]
        lines = [f"*> {name}: ["]
        for i, v in enumerate(values):
            text = item(v) + (", " if i < len(values) - 1 else "]")
            if len(prefix + lines[-1] + text.rstrip()) > HEADER_WIDTH and not lines[-1].endswith("["):
                lines[-1] = lines[-1].rstrip()
                lines.append("*>    ")
            lines[-1] += text
        if not values:
            lines[-1] += "]"
        return lines

    lines = ["*>--- conformance-case", *field("id", scalar=program.test_id), *field("rules", program.rows)]
    if program.editions:
        lines += field("accept", program.editions)
    if program.files:
        lines += field("files", program.files)
    lines += [*field("source-format", scalar=program.fmt), *field("portable", scalar="unverified"), "*>---"]
    return "\n".join(prefix + line for line in lines) + "\n"


def fixed_compatible(text: str) -> bool:
    """Does `text` read as the same program in fixed form: every line blank, a comment in column 7, or program text
    from column 8 that ends by margin R (column 72), with nothing in the sequence number area but digits and spaces?"""
    for raw in text.split("\n"):
        line = raw.rstrip("\r").expandtabs(8)
        if not line.strip():
            continue
        if not all(c.isdigit() or c == " " for c in line[:6]) or len(line) < 7 or line[6] not in " */-Dd":
            return False
        if line[6] == " " and line[72:].strip():
            return False
    return True


def shifted(text: str, delta: int, pattern: re.Pattern) -> str:
    """`text` with every line number `pattern` captures (its group 1) moved by `delta`."""
    return pattern.sub(lambda m: m.group(0)[:m.start(1) - m.start(0)] + str(int(m.group(1)) + delta)
                       + m.group(0)[m.end(1) - m.start(0):], text)


#: A diagnostic's position as a WiseOwl `.err` expectation quotes it: `(<line>,<column>):`.
DIAGNOSTIC_POSITION = re.compile(r"\((\d+),\d+\)")


def run(program_text: str, files: dict[str, bytes], edition: str, fmt: str, options: dict[str, str]) -> str | None:
    """The program's standard output from the `cobol` command (compile, then run with dotnet, as the WiseOwl adapter
    does), its declared files beside it; None when it does not compile."""
    with tempfile.TemporaryDirectory() as work:
        for name, data in files.items():
            Path(work, name).parent.mkdir(parents=True, exist_ok=True)
            Path(work, name).write_bytes(data)
        Path(work, "prog.cob").write_text(program_text, encoding="utf-8", newline="")
        cmd = [str(COBOL), "prog.cob", "--std", edition, "--source-format", fmt, "-o", "prog.dll"]
        if options.get("sign-encoding"):
            cmd += ["--sign-encoding", options["sign-encoding"]]
        if options.get("flag-extensions") == "on":
            cmd += ["--flag-extensions"]
        env = {**os.environ, "COBOL_USER_LOCALE": "INVARIANT", "COBOL_SYSTEM_LOCALE": "INVARIANT"}
        if subprocess.run(cmd, cwd=work, capture_output=True, env=env).returncode != 0:
            return None
        r = subprocess.run(["dotnet", "prog.dll"], cwd=work, capture_output=True, env=env, stdin=subprocess.DEVNULL,
                           timeout=120)
        return r.stdout.decode("utf-8", errors="replace")


def normalize(text: str) -> str:
    """CutRunner.Normalize / run.py's normalize(): LF line endings, each line's trailing spaces trimmed."""
    return "\n".join(line.rstrip() for line in text.replace("\r\n", "\n").split("\n")).rstrip("\n")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--map", type=Path, default=None)
    args = ap.parse_args()

    titles = headings()
    schema = inventory_schema.load_schema()
    inventory = inventory_schema.load_inventory()
    ref_re = re.compile(r"conformance:(85|2002|2014|2023|negative)/([A-Za-z0-9_-]+)")
    cited: dict[str, list[dict]] = {}
    for row in inventory:
        for ref in str(row.get("test-ref", "")).split("; "):
            m = ref_re.fullmatch(ref.strip())
            if m:
                cited.setdefault(f"{m.group(1)}/{m.group(2)}", []).append(row)

    programs = load_programs()
    problems: list[str] = []
    for p in programs:
        stated = p.options.get("source-format")
        p.fmt = stated if stated in ("fixed", "free") else ("fixed" if is_fixed_form(p.text) else "free")
        if p.fmt == "free" and not stated and p.directory == "85" and fixed_compatible(p.text):
            # COBOL-85 has no free form (ISO 6.4 is a 2002 introduction): the detector read a fixed-form program
            # whose comment lines run past column 72 as free form. It is the fixed-form program it was written as.
            problems.append(f"NOTE {p.ref}: detected free at 85, fixed-form text: written as fixed")
            p.fmt = "fixed"
        rows = cited.get(p.ref, [])
        if not p.negative:
            in_band = [r for r in rows if p.directory in schema.split(r.get("editions", ""), schema.editions_sep)]
            if len(in_band) != len(rows):
                problems.append(f"{p.ref}: NOTE {len(rows) - len(in_band)} citing row(s) whose band lacks "
                                f"{p.directory} left out of rules: "
                                + ", ".join(r["rule-id"] for r in rows if r not in in_band))
            rows = in_band
            p.editions = [p.directory]
        p.rows = list(dict.fromkeys(r["rule-id"] for r in rows))
        p.folder = clause_folder(rows[0]["section"], titles) if rows else "_unassigned"
        dns = bool(rows) and all(r.get("verdict") == "DOCUMENTED-NON-SUPPORT" for r in cited.get(p.ref, []))
        declined = p.err is not None and any(code in p.err for code in DECLINED_CODES)
        wiseowl_option = any(k in p.options for k in WISEOWL_OPTIONS)
        p.files = copied_files(p, problems)
        # A library-name's text in a library folder: where it is found is the implementor's (ISO 7.2.3.4 GR 2-3).
        library = any("/" in f for f in p.files)
        # A free-form program at 85 runs under WiseOwl's extension (free form at every edition): WiseOwl's own test.
        free85 = p.fmt == "free" and p.directory == "85"
        p.tree = "wiseowl" if dns or declined or wiseowl_option or library or free85 else "cases"

    # Ids: a base name two programs share.
    by_name: dict[str, list[Program]] = {}
    for p in programs:
        by_name.setdefault(p.name, []).append(p)
    merged: dict[int, Program] = {}
    for name, group in by_name.items():
        if len(group) == 1:
            group[0].test_id = name
            continue
        positives = [p for p in group if not p.negative]
        same = (len(positives) == len(group) and len({p.text for p in group}) == 1 and len({p.out for p in group}) == 1
                and len({tuple(p.files) for p in group}) == 1)
        if same:
            keep = group[0]
            keep.test_id = name
            keep.editions = sorted({e for p in group for e in p.editions}, key=EDITIONS.index)
            keep.rows = list(dict.fromkeys(r for p in group for r in p.rows))
            for p in group[1:]:
                merged[id(p)] = keep
            problems.append(f"NOTE {name}: one program in {', '.join(p.directory for p in group)}: one test")
        else:
            for p in group:
                p.test_id = f"{name}_{'neg' if p.negative else p.directory}"
            problems.append(f"NOTE {name}: {len(group)} programs ({', '.join(p.directory for p in group)}): ids "
                            + ", ".join(p.test_id for p in group))

    existing = {c.stem for c in SUITE.glob("**/cases/*/*.cob")}
    for p in programs:
        if id(p) not in merged and p.test_id in existing:
            raise SystemExit(f"{p.ref}: id {p.test_id} is already a suite test")

    # Write.
    tree_dir = {"cases": SUITE / "cases", "wiseowl": SUITE / "wiseowl" / "cases"}
    diagnostics_path = SUITE / "wiseowl" / "diagnostics.json"
    options_path = SUITE / "wiseowl" / "options.json"
    diagnostics = json.loads(diagnostics_path.read_text(encoding="utf-8")) if diagnostics_path.is_file() else {}
    options = json.loads(options_path.read_text(encoding="utf-8")) if options_path.is_file() else {}
    mapping = {}
    placed: dict[Path, bytes] = {}
    sources: set[Path] = set()
    conflicts = []
    for p in programs:
        target = merged.get(id(p), p)
        folder = tree_dir[target.tree] / target.folder
        mapping[p.ref] = {"id": target.test_id, "path": (folder / (target.test_id + ".cob")).relative_to(SUITE).as_posix(),
                          "editions": p.editions if not p.negative else [], "refuse-at": p.reject_at,
                          "source-format": target.fmt}
        if id(p) in merged:
            continue
        files = []
        for rel in p.files:
            src = OLD / p.directory / rel
            dest = folder / rel
            data = src.read_bytes()
            sources.add(src)
            if dest in placed and placed[dest] != data:
                conflicts.append(f"{p.ref}: {rel} differs from the {dest.relative_to(SUITE).as_posix()} another test "
                                 f"placed")
            placed[dest] = data
            files.append(rel)
        p.files = files
        body = strip_wiseowl_lines(p.text, p.negative)
        block = header(p)
        # Every code line moves by the same delta: the header above it, less the lines the move removed (the
        # negative's reject-at line and the leading options lines, all above the code).
        delta = block.count("\n") - (p.text.count("\n") - body.count("\n"))
        if p.negative and p.err:
            moved = shifted(p.err, delta, DIAGNOSTIC_POSITION)
            if moved != p.err:
                problems.append(f"NOTE {p.ref}: its diagnostic's position moved {delta} lines with the header")
                p.err = moved
        if not p.negative and (p.out is None or LINE_REPORTING.search(p.text)) and not args.dry_run:
            staged = {rel: (OLD / p.directory / rel).read_bytes() for rel in files}
            got = run(block + body, staged, p.directory, p.fmt, p.options)
            if p.out is None:
                if got is not None:
                    p.out = got
                    problems.append(f"NOTE {p.ref}: compile-only: its output recorded ({len(got.splitlines())} line(s))")
            elif got is not None and normalize(got) != normalize(p.out):
                want, have = normalize(p.out).split("\n"), normalize(got).split("\n")
                numbers = re.compile(r"\d+")
                same_shape = len(want) == len(have) and all(
                    numbers.sub("#", w) == numbers.sub("#", h)
                    and all(int(b) - int(a) in (0, delta) for a, b in zip(numbers.findall(w), numbers.findall(h)))
                    for w, h in zip(want, have))
                if same_shape:
                    problems.append(f"NOTE {p.ref}: its reported source lines moved {delta} with the header")
                    p.out = got
                else:
                    problems.append(f"WARN {p.ref}: its output differs beyond the line shift: left as it was")
        if not args.dry_run:
            folder.mkdir(parents=True, exist_ok=True)
            (folder / (p.test_id + ".cob")).write_text(block + body, encoding="utf-8", newline="")
            if not p.negative:
                (folder / (p.test_id + ".out")).write_text(p.out if p.out is not None else "", encoding="utf-8",
                                                         newline="")
        if p.out is None and not p.negative:
            problems.append(f"WARN {p.ref}: compile-only and it does not compile: an empty expected output")
        if p.negative:
            diagnostics[p.test_id] = {"refuse-at": p.reject_at, "expect": p.err}
        wise = {k: v for k, v in p.options.items() if k in WISEOWL_OPTIONS}
        if wise:
            options[p.test_id] = wise
    if conflicts:
        for c in conflicts:
            print("  CONFLICT " + c)
        return 1
    if not args.dry_run:
        for dest, data in placed.items():
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(data)
        diagnostics_path.write_text(json.dumps(dict(sorted(diagnostics.items())), indent=1) + "\n", encoding="utf-8",
                                    newline="\n")
        options_path.write_text(json.dumps(dict(sorted(options.items())), indent=1) + "\n", encoding="utf-8",
                                newline="\n")
        # The inventory's citations.
        def new_ref(m: re.Match) -> str:
            hit = mapping.get(f"{m.group(1)}/{m.group(2)}")
            return f"conformance:{hit['path'][:-len('.cob')]}" if hit else m.group(0)
        for row in inventory:
            for key in ("test-ref", "notes"):
                if isinstance(row.get(key), str):
                    row[key] = ref_re.sub(new_ref, row[key])
            if isinstance(row.get("test-ref"), str):
                row["test-ref"] = "; ".join(dict.fromkeys(r for r in row["test-ref"].split("; ")))
        inventory_schema.write_inventory(inventory)
        for p in programs:
            for suffix in (".cob", ".out", ".err"):
                (OLD / p.directory / (p.name + suffix)).unlink(missing_ok=True)
        for src in sources:
            src.unlink(missing_ok=True)
        for d in EDITIONS + ["negative"]:
            (OLD / d / "manifest.json").unlink(missing_ok=True)
        for leftover in sorted(f for f in OLD.rglob("*") if f.is_file()) if OLD.is_dir() else []:
            print(f"  LEFT {leftover.relative_to(REPO).as_posix()}: no program converted it")
        for d in sorted((d for d in OLD.rglob("*") if d.is_dir()), reverse=True) if OLD.is_dir() else []:
            if not any(d.iterdir()):
                d.rmdir()
    if args.map:
        args.map.write_text(json.dumps(mapping, indent=1) + "\n", encoding="utf-8")
    for line in problems:
        print("  " + line)
    trees = {t: sum(1 for p in programs if p.tree == t and id(p) not in merged) for t in tree_dir}
    print(f"=== CONVERT S2: {len(programs)} program(s) -> {sum(trees.values())} test(s) "
          f"({trees['cases']} cases, {trees['wiseowl']} wiseowl), {len(diagnostics)} refusal(s), "
          f"{len(options)} option entr(ies){' (dry run)' if args.dry_run else ''} ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
