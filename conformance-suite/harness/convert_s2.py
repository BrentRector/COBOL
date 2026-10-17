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
import hashlib
import json
import re
import shutil
import sys
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
GENERIC_HEADINGS = {"general", "general format", "general formats", "syntax rules", "general rules"}
TRAILING_NOUNS = {"statement", "directive", "clause", "entry", "paragraph", "function", "phrase"}
LIBRARY_SUFFIXES = (".CPY", ".CBL", ".COB", ".cpy", ".cbl", ".cob")
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
    for m in re.finditer(r"(?m)^#{2,7} ([0-9]+(?:\.[0-9]+)*) (.+?)\s*$", (REPO / "specs" / "ISO_COBOL.md").read_text(
            encoding="utf-8")):
        out.setdefault(m.group(1), m.group(2))
    return out


def clause_folder(section: str, titles: dict[str, str]) -> str:
    parts = section.strip().strip(".").split(".")
    while len(parts) > 1 and titles.get(".".join(parts), "").strip().lower() in GENERIC_HEADINGS:
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
            text = cob.read_text(encoding="utf-8")
            out = base / (cob.stem + ".out")
            err = base / (cob.stem + ".err")
            first = text.split("\n")[0]
            reject = first.split(REJECT_AT, 1)[1].split() if d == "negative" and REJECT_AT in first else []
            if d == "negative" and not reject:
                raise SystemExit(f"{d}/{cob.name}: a negative without its '*> reject-at:' first line")
            programs.append(Program(
                directory=d, name=cob.stem, text=text,
                out=out.read_text(encoding="utf-8") if out.is_file() else None,
                err=err.read_text(encoding="utf-8").strip() if err.is_file() else None,
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
    queue = [(program.text, directory)]
    seen: set[str] = set()
    while queue:
        text, where = queue.pop()
        tokens = cobol.tokenize(cobol.read_lines(text, program.fmt))
        for i, stmt in enumerate(cobol.copy_statements(tokens)):
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
                queue.append((cobol.decode(path.read_bytes()), path.parent))
    return found


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
    def lst(values: list[str]) -> str:
        return "[" + ", ".join(v if re.fullmatch(r"[^\s\[\]{},:\"]+", v) else '"' + v.replace('"', '""') + '"'
                               for v in values) + "]"
    fields = [f"id: {program.test_id}", f"rules: {lst(program.rows)}"]
    if program.editions:
        fields.append(f"accept: {lst(program.editions)}")
    if program.files:
        fields.append(f"files: {lst(program.files)}")
    fields += [f"source-format: {program.fmt}", "portable: unverified"]
    prefix = "      " if program.fmt == "fixed" else ""
    lines = ["*>--- conformance-case", *(f"*> {f}" for f in fields), "*>---"]
    return "\n".join(prefix + line for line in lines) + "\n"


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
        p.tree = "wiseowl" if dns or declined or wiseowl_option or library else "cases"

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
            if dest in placed and placed[dest] != data:
                conflicts.append(f"{p.ref}: {rel} differs from the {dest.relative_to(SUITE).as_posix()} another test "
                                 f"placed")
            placed[dest] = data
            files.append(rel)
        p.files = files
        body = strip_wiseowl_lines(p.text, p.negative)
        if not args.dry_run:
            folder.mkdir(parents=True, exist_ok=True)
            (folder / (p.test_id + ".cob")).write_text(header(p) + body, encoding="utf-8", newline="")
            if not p.negative:
                (folder / (p.test_id + ".out")).write_text(p.out if p.out is not None else "", encoding="utf-8",
                                                         newline="")
        if p.out is None and not p.negative:
            problems.append(f"NOTE {p.ref}: compile-only: an empty expected output (§3.1)")
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
