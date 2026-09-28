#!/usr/bin/env python3
"""impacted_tests.py — the implementer's gate filter, DERIVED from what a change touches (kb/Work PB1683).

An implementer's gate used to run the tests its author NAMED — its own goldens, `~Drift|~EditionGate`, and
`~CorpusRunner|~Nist` on a shared seam — and a change that broke a test nobody thought to name passed that gate and
went red only at the lander's whole-assembly gate (train 68b: `DiagnosticPositionTests` after a COPY library-search
change; a `CONSTANT AS NULL` crash in `DataBinder.Constants.cs`; wave 69 existed only to finish them). This script
reads the RECORDED impact map (`scripts/spec/record_impact_map.py`: per test, the METHODS its execution reached,
each with the line ranges it owns) and prints the vstest filter selecting every Conformance test the change can
affect, unioned with the standing terms. Unit and Characterization are not filtered: the gate runs them whole.

How a changed C# file selects (the design is docs/rearchitecture/DESIGN-test-build-ci.md §3.13):
  * a hunk INSIDE the lines a method owns selects the tests that executed that method (a static constructor's
    change selects every test that executed any member of its type — a static initializer runs once);
  * a hunk that only touches blank or comment lines selects nothing;
  * any other hunk — outside every method: a new member, a signature, a constant, an enum value, a field, an
    attribute, a `using` — selects every test that executed ANY code in the file, plus every test that executed a
    file NAMING a type the file declares (constants and enum values are compiled INTO their consumers);
  * without `--base` (files named on the command line) every file is taken at that file level.

⛔ CONSERVATIVE BY CONSTRUCTION — it never silently narrows. Each of these prints the WHOLE-assembly filter
(`FullyQualifiedName~.`) and says why:
  * no map recorded for the change's base, or the newest usable one is older than the base by anything but
    documentation (a map describes exactly the tree, and the line numbers, it was recorded at);
  * a changed `.g4`, project/build file (`.csproj` `.props` `.targets` `.sln` `global.json` `nuget.config`), or
    the source generator (its output is every generated file);
  * a NEW source file under src/ (the map has never seen it), or a declarations-only file no executed file names;
  * a changed non-C# file under src/ (embedded data: collation tables, tailorings);
  * a change whose only recorded execution was OUTSIDE every test's context;
  * test data it cannot attribute, or a selection too large to express in the term budget.
Anything it narrows is printed with the reason (`--explain` lists every decision).

Usage:
    python scripts/spec/impacted_tests.py --base <sha>             # the worktree's change against its base
    python scripts/spec/impacted_tests.py src/Cobol.Net.Compiler/Binding/DataBinder.Constants.cs
    python scripts/spec/impacted_tests.py --base <sha> --plus "DisplayName~my_new_golden"
    python scripts/spec/impacted_tests.py --self-test

The LAST line on stdout is the filter, ready for `build-local.ps1 -Filter`; everything else goes to stderr.
"""

from __future__ import annotations

import argparse
import base64
import fnmatch
import gzip
import json
import re
import subprocess
import sys
import zlib
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SCHEMA = 2
WHOLE = "FullyQualifiedName~."
STANDING = ["FullyQualifiedName~Drift", "FullyQualifiedName~EditionGate"]
FILTERED_ASSEMBLY = "Conformance"
CONFORMANCE_DIR = "tests/Cobol.Net.Tests.Conformance/"
TEST_PROJECTS = ("tests/Cobol.Net.Tests.Conformance/", "tests/Cobol.Net.Tests.Unit/",
                 "tests/Cobol.Net.Tests.Characterization/", "tests/_shared/")
# Paths no test EXECUTES: they can only be READ by a test, which the reader search below finds.
DOC_GLOBS = ["docs/*", "kb/*", "*.md", ".claude/*", "adjudication/*", "specs/*", "scripts/*", "tools/impact/*",
             ".github/*", "samples/*"]
# Structural inputs: every assembly may change.
STRUCTURAL_RE = re.compile(r"(\.g4|\.csproj|\.props|\.targets|\.sln|global\.json|nuget\.config|\.editorconfig)$"
                           r"|^src/Cobol\.Net\.Compiler\.SourceGen/|(^|/)Directory\.[^/]+$", re.IGNORECASE)
# A theory row is selected by the value of its last string argument when it is distinctive enough to be a name.
ROW_VALUE_RE = re.compile(r'"([A-Za-z0-9_./-]{6,})"\)$')
TRIVIAL_RE = re.compile(r"^\s*(//.*|/\*.*|\*.*|)$")
HUNK_RE = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+\d+(?:,\d+)? @@")
HEAD: str | None = None  # --head: diff base..HEAD instead of the worktree (a replay, or a landed branch)
MAX_TERMS = 150         # command-line length and build-local's per-term population probe bound it


@dataclass
class Selection:
    mask: int = 0                                        # probe entries whose tests are selected
    extra_terms: set[str] = field(default_factory=set)   # terms not derived from the map
    whole: list[str] = field(default_factory=list)       # reasons the whole assembly is selected
    notes: list[str] = field(default_factory=list)


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout.strip()


def default_store() -> Path:
    common = Path(git("rev-parse", "--git-common-dir"))
    return (common if common.is_absolute() else (REPO / common).resolve()) / "cobol-impact"


def is_doc(path: str) -> bool:
    return any(fnmatch.fnmatch(path, g) for g in DOC_GLOBS) and not path.startswith(("src/", "tests/"))


def unpack(b64: str) -> int:
    return int.from_bytes(zlib.decompress(base64.b64decode(b64)), "little") if b64 else 0


def bit_ids(n: int):
    for bi, byte in enumerate(n.to_bytes((n.bit_length() + 7) // 8, "little")):
        if byte:
            for k in range(8):
                if byte >> k & 1:
                    yield bi * 8 + k


def load_map(path: Path) -> dict:
    with gzip.open(path, "rt", encoding="utf-8") as fh:
        m = json.load(fh)
    if m.get("schema") != SCHEMA:
        raise SystemExit(f"{path}: impact map schema {m.get('schema')} — this script reads schema {SCHEMA}; "
                         f"re-record it (record_impact_map.py)")
    return m


class MapIndex:
    """The map, decoded once: per file its entries and their line ranges; per Conformance test its bitset."""

    def __init__(self, m: dict):
        self.m = m
        self.entries = m["entries"]
        self.ranges: dict[str, list[tuple[int, int, int]]] = {}   # file -> [(lo, hi, id)], lines 0 = type-level
        self.file_ids: dict[str, int] = {}                          # file -> mask of every entry in it
        for i, (_, segs) in enumerate(self.entries):
            for f, lo, hi in segs:
                self.ranges.setdefault(f, []).append((lo, hi, i))
                self.file_ids[f] = self.file_ids.get(f, 0) | (1 << i)
        self.implied_by: dict[int, int] = {}                        # cctor id -> mask of the entries implying it
        for k, cctors in m["implies"].items():
            for c in cctors:
                self.implied_by[c] = self.implied_by.get(c, 0) | (1 << int(k))
        self.tests = m["tests"]
        self.conf = [i for i, t in enumerate(self.tests) if t[0] == FILTERED_ASSEMBLY]
        self.conf_bits = {i: unpack(m["test_bits"][i]) for i in self.conf}
        self.reached = 0
        for b in m["test_bits"]:
            self.reached |= unpack(b)
        self.class_bits = {k: unpack(v) for k, v in m["class_bits"].items()}
        self.collection_bits = {k: unpack(v) for k, v in m["collection_bits"].items()}
        for v in list(self.class_bits.values()) + list(self.collection_bits.values()):
            self.reached |= v
        self.ambient = unpack(m.get("ambient_bits", ""))
        self.known = set(m["known_files"])
        self.executed_files = {f for f, ids in self.file_ids.items() if ids & self.reached}
        # (method name, declaring type's simple name) -> entries: what a new overload can steal calls from
        self.by_name: dict[tuple[str, str], int] = {}
        for i, (name, _) in enumerate(self.entries):
            if "::" not in name:
                continue
            owner, _, member = name.partition("::")
            simple = re.split(r"[./]", owner.split(" ")[-1])[-1]
            key = (member.split("(")[0], simple)
            self.by_name[key] = self.by_name.get(key, 0) | (1 << i)

    def named(self, path: str, method: str) -> int:
        """Every entry called `method` on a type the file declares (a constructor when it is the type's name)."""
        types = set(TYPE_DECL_RE_ALL.findall(text_of(REPO / path)))
        mask = 0
        for t in types:
            mask |= self.by_name.get((".ctor" if method == t else method, t), 0)
        return mask

    def expand(self, mask: int) -> int:
        """A changed static constructor reaches every test that touched its type."""
        for c, users in self.implied_by.items():
            if mask >> c & 1:
                mask |= users
        return mask

    def selection(self, mask: int) -> tuple[set[int], set[str]]:
        """(Conformance test indices, whole Conformance classes) whose recorded execution intersects `mask`."""
        tests = {i for i in self.conf if self.conf_bits[i] & mask}
        classes = {k.split(":", 1)[1] for k, b in self.class_bits.items()
                   if k.startswith(FILTERED_ASSEMBLY + ":") and b & mask}
        for k, b in self.collection_bits.items():
            if k.startswith(FILTERED_ASSEMBLY + ":") and b & mask:
                tests |= {i for i in self.conf if self.tests[i][4] == k}
        return tests, classes


def changed_files(base: str) -> list[tuple[str, str]]:
    """(status, path) for every file the worktree changed against `base`, untracked files as added."""
    out = []
    for line in git("diff", "--name-status", "--no-renames", base, *([HEAD] if HEAD else [])).splitlines():
        status, _, path = line.partition("\t")
        out.append((status[:1], path))
    if HEAD is None:  # the worktree: untracked files are added files
        out += [("A", p) for p in git("ls-files", "--others", "--exclude-standard").splitlines() if p]
    return sorted({(s, p) for s, p in out if p and p != "STATUS.md"}, key=lambda x: x[1])


def hunks(base: str, path: str) -> list[tuple[int, int, list[str], list[str]]]:
    """The -U0 hunks of `path` against `base`: (old start, old count, removed lines, added lines)."""
    out: list[tuple[int, int, list[str], list[str]]] = []
    for line in git("diff", "-U0", "--no-color", "--no-renames", base, *([HEAD] if HEAD else []), "--", path).splitlines():
        h = HUNK_RE.match(line)
        if h:
            out.append((int(h.group(1)), int(h.group(2) if h.group(2) is not None else 1), [], []))
        elif out and line.startswith("-") and not line.startswith("---"):
            out[-1][2].append(line[1:])
        elif out and line.startswith("+") and not line.startswith("+++"):
            out[-1][3].append(line[1:])
    return out


MODIFIERS = r"(?:public|private|protected|internal|static|readonly|sealed|abstract|virtual|override|async|unsafe|new|" \
            r"extern|partial|const|file|required|volatile|event|implicit|explicit)"
DECL_RE = re.compile(rf"^\s*(?:\[[^\]]*\]\s*)*((?:{MODIFIERS}\s+)+)(.*)$")
TYPE_DECL_RE = re.compile(r"\b(class|record|struct|interface|enum)\s+([A-Za-z_]\w*)")
DISPATCH_RE = re.compile(r"\b(override|virtual|abstract|extern|partial|new|implicit|explicit|operator)\b")
TEST_ATTR_RE = re.compile(r"^\s*\[(Fact|Theory|InlineData|MemberData|ClassData)\b")


def method_ids(ix: MapIndex, path: str, h: tuple[int, int, list[str], list[str]]) -> int | None:
    """The entries a hunk changes, or None when it is a DECLARATION change the method level cannot bound."""
    start, count, removed, added = h
    ranges = [(lo, hi, i) for lo, hi, i in ix.ranges.get(path, []) if lo > 0]
    mask = 0
    if count == 0:  # pure insertion after line `start`
        inside = [i for lo, hi, i in ranges if lo <= start and start + 1 <= hi]
        for i in inside:
            mask |= 1 << i
        return mask if inside else inserted_members(ix, path, added)
    for n, text in zip(range(start, start + count), removed):
        owners = [i for lo, hi, i in ranges if lo <= n <= hi]
        if owners:
            for i in owners:
                mask |= 1 << i
        elif not TRIVIAL_RE.match(text):
            return None  # a declaration line changed or removed: a signature, a constant, a field, an attribute
    extra = inserted_members(ix, path, added)
    return None if extra is None else mask | extra


def inserted_members(ix: MapIndex, path: str, added: list[str]) -> int | None:
    """What ADDED code can change beyond the lines it adds. A new ordinary method runs only when changed code calls
    it — except through overload resolution, so it selects the tests that reached any same-named method of the
    file's types. Anything that changes dispatch or state for UNCHANGED code — an override, a virtual, an
    operator or conversion, an extension method, a field, property or constant, a type with a base list, an
    attribute — returns None (a declaration change). A new xunit test in a Conformance file selects its class."""
    mask = 0
    for line in added:
        if TRIVIAL_RE.match(line):
            continue
        if TEST_ATTR_RE.match(line):
            if path.startswith(CONFORMANCE_DIR):
                continue  # the new test's class is selected by name in select_cs
            return None
        if re.match(r"^\s*\[", line):
            return None  # an attribute: read by reflection, not by a call
        t = TYPE_DECL_RE.search(line)
        if t and DECL_RE.match(line):
            if ":" in line.split(t.group(2), 1)[1] or t.group(1) in ("interface", "enum"):
                return None
            continue
        d = DECL_RE.match(line)
        if not d:
            continue  # a body line of the added member
        mods, rest = d.group(1), d.group(2)
        if DISPATCH_RE.search(mods) or DISPATCH_RE.search(rest.split("(", 1)[0]):
            return None
        call = re.search(r"([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(", rest)
        if not call or "=" in rest.split("(", 1)[0] or re.search(r"\(\s*this\s", rest):
            return None  # a field, property, constant or event; or an extension method
        mask |= ix.named(path, call.group(1))
    return mask


_TEXT: dict[Path, str] = {}
_PROGRAMS: dict[Path, list[Path]] = {}


def text_of(path: Path) -> str:
    """A file's text, read once per run ("" when unreadable) — the consumer and corpus searches re-read the same
    thousand files for every changed file otherwise."""
    if path not in _TEXT:
        try:
            _TEXT[path] = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            _TEXT[path] = ""
    return _TEXT[path]


def corpus_programs(root: Path) -> list[Path]:
    if root not in _PROGRAMS:
        _PROGRAMS[root] = [p for p in root.rglob("*") if p.suffix.lower() in (".cob", ".cbl")]
    return _PROGRAMS[root]


def test_sources() -> list[Path]:
    out = []
    for proj in TEST_PROJECTS:
        out += [p for p in (REPO / proj).rglob("*.cs") if "/bin/" not in p.as_posix() and "/obj/" not in p.as_posix()]
    return out


def readers(name: str, sources: list[Path]) -> list[str]:
    """Test source files (repo-relative) that name `name` inside a STRING LITERAL — the tests that can open a data
    or doc file. A mention in a comment (`<c>DESIGN-….md</c> §3.12`) is a citation, not a read; counting it made
    every harness file that cites its design doc a reader of it."""
    literal = re.compile(r'"[^"\n]*' + re.escape(name) + r'[^"\n]*"')
    return [p.relative_to(REPO).as_posix() for p in sources if literal.search(text_of(p))]


def classes_declared(path: Path) -> list[str]:
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return []
    return sorted(set(re.findall(r"\b(?:class|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)", text)))


TYPE_DECL_RE_ALL = re.compile(r"\b(?:class|record|struct|enum|interface)\s+([A-Za-z_]\w*)")


def declared_types(texts: list[str]) -> list[str]:
    """The OUTERMOST types a file declares — those at its shallowest declaration indentation. A nested type is
    named from outside only through its container, and a private nested `Entry` or `Result` would otherwise
    match half the tree."""
    names: set[str] = set()
    for text in texts:
        decls = [(len(m.group(1)), m.group(2)) for m in
                 re.finditer(r"(?m)^([ \t]*)(?:[\w\[\]]+\s+)*(?:class|record|struct|enum|interface)\s+([A-Za-z_]\w{3,})",
                             text)]
        if decls:
            top = min(d for d, _ in decls)
            names |= {n for d, n in decls if d == top}
    return sorted(names)


def consumers(ix: MapIndex, path: str, base: str | None) -> list[str] | None:
    """The EXECUTED files that name a type `path` declares (in the base or the worktree version) — the code a
    constant, an enum value or a type shape is compiled into. None when it declares no type."""
    texts = []
    try:
        texts.append((REPO / path).read_text(encoding="utf-8", errors="replace"))
    except OSError:
        pass
    if base:
        try:
            texts.append(git("show", f"{base}:{path}"))
        except subprocess.CalledProcessError:
            pass
    types = declared_types(texts)
    if not types:
        return None
    pattern = re.compile(r"\b(?:" + "|".join(map(re.escape, types)) + r")\b")
    users = []
    for f in sorted(ix.executed_files):
        if f == path or not f.endswith(".cs"):
            continue
        try:
            if pattern.search(text_of(REPO / f)):
                users.append(f)
        except OSError:
            continue
    return users


def corpus_goldens(path: str) -> set[str] | None:
    """The corpus goldens a file under tests/conformance/ belongs to: itself when it is a program at an edition
    root, else every program that names its stem (a copybook, a library member, a data file), transitively through
    support programs. None when no golden names it."""
    root = REPO / "tests" / "conformance"
    programs = corpus_programs(root)
    goldens: set[str] = set()
    seen: set[str] = set()
    todo = [Path(path)]
    while todo:
        p = todo.pop()
        stem = p.stem
        if stem.lower() in seen:
            continue
        seen.add(stem.lower())
        if len(p.parts) == 4 and (root / p.parts[2] / (stem + ".cob")).exists():
            goldens.add(stem)  # tests/conformance/<edition>/<name>.<ext>: the golden itself or its output/input
            continue
        word = re.compile(r"(?<![A-Za-z0-9-])" + re.escape(stem) + r"(?![A-Za-z0-9-])", re.IGNORECASE)
        for prog in programs:
            if prog.stem.lower() == stem.lower():
                continue
            try:
                if word.search(text_of(prog)):
                    todo.append(prog.relative_to(REPO))
            except OSError:
                continue
    return goldens or None


def find_map(store: Path, base: str, sel: Selection) -> dict | None:
    """The map recorded at `base`, else the newest one at an ancestor of `base` whose difference from it is only
    documentation. Anything else is stale: the tree (and the line numbers) it describes is not the tree under
    change."""
    exact = store / f"{base}.json.gz"
    if exact.exists():
        return load_map(exact)
    candidates = []
    for p in store.glob("*.json.gz") if store.is_dir() else []:
        sha = p.name.split(".")[0]
        if p.name != f"{sha}.json.gz":
            continue  # partial (trial) maps are never used implicitly
        if subprocess.run(["git", "merge-base", "--is-ancestor", sha, base], cwd=REPO,
                          capture_output=True).returncode == 0:
            candidates.append((int(git("rev-list", "--count", f"{sha}..{base}")), sha, p))
    for _, sha, p in sorted(candidates):
        drift = [f for f in git("diff", "--name-only", sha, base).splitlines() if f and not is_doc(f)]
        if not drift:
            sel.notes.append(f"map {sha[:12]} is older than the base {base[:12]} only by documentation — used")
            return load_map(p)
        sel.whole.append(f"the newest map at an ancestor ({sha[:12]}) is STALE: {len(drift)} non-documentation "
                         f"file(s) changed between it and the base {base[:12]} (first: {drift[0]}) — "
                         f"record one at the base (record_impact_map.py --commit {base[:12]})")
        return None
    sel.whole.append(f"no impact map recorded for the base {base[:12]} or any ancestor in {store} — "
                     f"record one (record_impact_map.py --commit {base[:12]})")
    return None


def select_cs(ix: MapIndex, path: str, status: str, base: str | None, sel: Selection) -> None:
    if path.startswith("tests/") and not path.startswith(TEST_PROJECTS):
        sel.notes.append(f"{path}: in a test project outside the three gated assemblies — no gated test runs it")
        return
    if path not in ix.ranges:
        if path.startswith(TEST_PROJECTS) and not path.startswith(CONFORMANCE_DIR):
            sel.notes.append(f"{path}: test code in an assembly the gate runs whole")
            return
        if path.startswith(CONFORMANCE_DIR) and path not in ix.known:
            decl = classes_declared(REPO / path)
            if not decl:
                sel.whole.append(f"{path}: a test source file the map has never seen and that declares no class")
            sel.extra_terms.update(f"FullyQualifiedName~.{c}." for c in decl)
            sel.notes.append(f"{path}: new test code — its classes are selected by name ({', '.join(decl)})")
            return
        if path not in ix.known:
            sel.whole.append(f"{path}: the map has never seen it (a file NEW since the map's commit)")
            return
        declaration_change(ix, path, base, sel, "declarations only (no method of it has sequence points)")
        return
    if base is None or status != "M":
        declaration_change(ix, path, base, sel, "named without a diff" if base is None else f"status {status}")
        return
    mask = 0
    for h in hunks(base, path):
        ids = method_ids(ix, path, h)
        if ids is None:
            declaration_change(ix, path, base, sel, f"a change at line {h[0]} outside every method")
            return
        mask |= ids
        if path.startswith(CONFORMANCE_DIR) and any(TEST_ATTR_RE.match(a) for a in h[3]):
            sel.extra_terms.update(f"FullyQualifiedName~.{c}." for c in classes_declared(REPO / path))
    sel.mask |= ix.expand(mask)
    names = sorted({ix.entries[i][0].split("::")[-1].split("(")[0] for i in bit_ids(mask)})
    sel.notes.append(f"{path}: {len(names)} changed method(s) {', '.join(names[:6])}"
                     + (" …" if len(names) > 6 else "") if names else f"{path}: comments and blank lines only")


def declaration_change(ix: MapIndex, path: str, base: str | None, sel: Selection, why: str) -> None:
    """The file level: every test that executed the file, and every test that executed a file naming its types."""
    users = consumers(ix, path, base)
    own = ix.file_ids.get(path, 0)
    if not own and not users:
        sel.whole.append(f"{path}: {why}, and no executed file names a type it declares — cannot bound it")
        return
    sel.mask |= ix.expand(own)
    for u in users or []:
        sel.mask |= ix.file_ids[u]
    sel.notes.append(f"{path}: {why} — the file level, plus the {len(users or [])} executed file(s) naming its types")


def select(ix: MapIndex | None, changes: list[tuple[str, str]], base: str | None, sel: Selection) -> None:
    if ix is None:
        return
    sources = None
    for status, path in changes:
        if path.startswith("tools/impact/"):
            sel.notes.append(f"{path}: the impact recorder itself — only a recording build compiles it")
            continue
        if STRUCTURAL_RE.search(path):
            sel.whole.append(f"{path}: a grammar, build or source-generator input — every assembly may change")
            continue
        if path.endswith(".cs") and path.startswith(("src/", "tests/")):
            select_cs(ix, path, status, base, sel)
            continue
        if path.startswith("src/"):
            sel.whole.append(f"{path}: a non-C# input under src/ (embedded data or resources)")
            continue
        if path.startswith("tests/conformance/") and not path.endswith("manifest.json"):
            goldens = corpus_goldens(path)
            if goldens is None:
                sel.whole.append(f"{path}: corpus data no golden program names — cannot attribute it")
            else:
                sel.extra_terms.update(f"DisplayName~{g}" for g in goldens)
                sel.notes.append(f"{path}: the golden(s) {', '.join(sorted(goldens))}")
            continue
        if path.startswith("tests/nist/"):
            sel.extra_terms.add("FullyQualifiedName~Nist")
        if path.startswith("tests/version-matrix/"):
            sel.extra_terms.add("FullyQualifiedName~VersionMatrix")
        # Anything else is DATA some test may READ: the tests whose source names it.
        sources = sources if sources is not None else test_sources()
        name = Path(path).name
        who = readers(name, sources)
        if who:
            for r in who:
                if r in ix.file_ids:
                    sel.mask |= ix.file_ids[r]
                elif r.startswith(CONFORMANCE_DIR):
                    sel.extra_terms.update(f"FullyQualifiedName~.{c}." for c in classes_declared(REPO / r))
            sel.notes.append(f"{path}: read by {len(who)} test source file(s) that name '{name}'")
        elif path.startswith(("tests/nist/", "tests/version-matrix/")):
            sel.notes.append(f"{path}: the {'Nist' if 'nist' in path else 'VersionMatrix'} tests read it")
        elif is_doc(path):
            sel.notes.append(f"{path}: documentation no test source names — the standing ~Drift term is its net")
        elif path.startswith("tests/"):
            sel.whole.append(f"{path}: test data no test source names and no rule attributes")
        else:
            sel.notes.append(f"{path}: no test source names it and it is not built — not a test input")
    # ⛔ A changed entry reached ONLY outside every test's context cannot be attributed.
    # ⛔ A changed entry reached ONLY outside every test's context. In a test file that is xunit DISCOVERY running
    # a theory's data source, so the classes the file declares are the tests it can change; in product code it is
    # a finalizer or a thread that does not flow the context, and nothing attributes it.
    orphan = sel.mask & ix.ambient & ~ix.reached
    unattributed = []
    for i in bit_ids(orphan):
        files = {f for f, _, _ in ix.entries[i][1]}
        if all(f.startswith(TEST_PROJECTS) for f in files):
            for f in files:
                if f.startswith(CONFORMANCE_DIR):
                    sel.extra_terms.update(f"FullyQualifiedName~.{c}." for c in classes_declared(REPO / f))
        else:
            unattributed.append(ix.entries[i][0])
    if unattributed:
        sel.whole.append(f"{len(unattributed)} changed method(s) executed only OUTSIDE every test's context "
                         f"(e.g. {unattributed[0]}) — cannot attribute them")


def escape(value: str) -> str:
    return re.sub(r"([\\()&|=!~])", r"\\\1", value)


def build_filter(ix: MapIndex | None, sel: Selection, plus: list[str]) -> tuple[str, dict]:
    stats: dict = {"selected_tests": 0, "population": 0, "terms": 0, "map_selected": 0}
    terms: set[str] = set(STANDING) | sel.extra_terms | set(p for p in plus if p)
    if ix is None:
        return WHOLE, stats
    tests = ix.tests
    pop = ix.conf
    chosen, chosen_classes = ix.selection(sel.mask)
    stats["population"] = len(pop)
    stats["map_selected"] = len(chosen | {i for i in pop if tests[i][3] in chosen_classes})
    stats["population_seconds"] = round(sum(tests[i][5] for i in pop), 1)
    if sel.whole:
        return WHOLE, stats
    chosen = {i for i in chosen if tests[i][3] not in chosen_classes}
    by_method: dict[str, list[int]] = {}
    for i in pop:
        by_method.setdefault(tests[i][1], []).append(i)
    chosen_by_method: dict[str, list[int]] = {}
    for i in chosen:
        chosen_by_method.setdefault(tests[i][1], []).append(i)

    # Start at the finest level every row allows, then coarsen the LARGEST groups first until the terms fit.
    method_terms: dict[str, set[str]] = {}
    for meth, rows in chosen_by_method.items():
        vals = [ROW_VALUE_RE.search(tests[i][2] or "") for i in rows]
        if len(rows) < len(by_method[meth]) and all(v and tests[i][6] for v, i in zip(vals, rows)):
            method_terms[meth] = {f"DisplayName~{v.group(1)}" for v in vals}
        else:
            method_terms[meth] = {f"FullyQualifiedName={escape(meth)}"}
    class_terms = {f"FullyQualifiedName~{escape(c)}." for c in chosen_classes}

    def count() -> int:
        return len(terms | class_terms | set().union(*method_terms.values()))

    for meth in sorted(method_terms, key=lambda k: -len(method_terms[k])):
        if count() <= MAX_TERMS:
            break
        if len(method_terms[meth]) > 1:
            method_terms[meth] = {f"FullyQualifiedName={escape(meth)}"}
    if count() > MAX_TERMS:
        by_class: dict[str, list[str]] = {}
        for meth, rows in chosen_by_method.items():
            by_class.setdefault(tests[rows[0]][3], []).append(meth)
        for cls in sorted(by_class, key=lambda c: -len(by_class[c])):
            if count() <= MAX_TERMS:
                break
            for meth in by_class[cls]:
                method_terms.pop(meth, None)
            class_terms.add(f"FullyQualifiedName~{escape(cls)}.")
            chosen_classes.add(cls)
    if count() > MAX_TERMS:
        sel.whole.append(f"the selection ({stats['map_selected']} of {len(pop)} tests) needs more than {MAX_TERMS} "
                         f"filter terms even at class level")
        return WHOLE, stats

    selected = {i for i in pop if tests[i][3] in chosen_classes}
    for meth, ts in method_terms.items():
        if any(t.startswith("FullyQualifiedName=") for t in ts):
            selected.update(by_method[meth])
        else:
            selected.update(chosen_by_method[meth])
    terms |= class_terms | set().union(*method_terms.values())
    stats["selected_tests"] = len(selected)
    stats["selected_seconds"] = round(sum(tests[i][5] for i in selected), 1)
    stats["terms"] = len(terms)
    return "|".join(sorted(terms)), stats


def run(args: argparse.Namespace) -> tuple[str, Selection, dict, MapIndex | None]:
    sel = Selection()
    store = args.store or default_store()
    base = git("rev-parse", args.base) if args.base else None
    if args.map:
        m = load_map(args.map)
        if m.get("partial"):
            sel.notes.append(f"⚠ {args.map} is a PARTIAL (trial) map over {m.get('filter')!r}")
    else:
        m = find_map(store, base or git("rev-parse", "HEAD"), sel)
    ix = MapIndex(m) if m is not None else None
    changes = [("M", f) for f in args.files] if args.files else (changed_files(base) if base else [])
    if not changes:
        sel.notes.append("no changed files — the standing terms only")
    select(ix, changes, base if not args.files else None, sel)
    flt, stats = build_filter(ix, sel, args.plus.split("|") if args.plus else [])
    return flt, sel, stats, ix


def synthetic_map() -> dict:
    """Two methods in A.cs (lines 10-20 and 30-40), a type-level entry in B.cs reached only ambiently, and a
    Conformance test T.M1 reaching the first method."""
    def pack(n: int) -> str:
        return base64.b64encode(zlib.compress(n.to_bytes((n.bit_length() + 7) // 8, "little"))).decode()
    return {"schema": SCHEMA,
            "entries": [["N.A::One()", [["src/Cobol.Net.Compiler/A.cs", 10, 20]]],
                        ["N.A::Two()", [["src/Cobol.Net.Compiler/A.cs", 30, 40]]],
                        ["N.B <type-level>", [["src/Cobol.Net.Compiler/B.cs", 0, 0]]]],
            "implies": {},
            "known_files": ["src/Cobol.Net.Compiler/A.cs", "src/Cobol.Net.Compiler/B.cs",
                            "src/Cobol.Net.Compiler/Enum.cs"],
            "tests": [["Conformance", "N.T.M1", "N.T.M1", "N.T", "Conformance:c1", 1.0, True],
                      ["Conformance", "N.T.M2", "N.T.M2", "N.T", "Conformance:c1", 1.0, True]],
            "test_bits": [pack(0b001), pack(0)], "class_bits": {}, "collection_bits": {},
            "ambient_bits": pack(0b100)}


def self_test() -> int:
    """Prove each conservative arm FIRES against a synthetic map (feedback_prove_the_watchdog_fails)."""
    ix = MapIndex(synthetic_map())
    cases = [
        ("a mapped file selects its tests", ["src/Cobol.Net.Compiler/A.cs"], "FullyQualifiedName=N.T.M1"),
        ("an UNMAPPED (new) src file selects the whole assembly", ["src/Cobol.Net.Compiler/New.cs"], WHOLE),
        ("a known file no test executed selects the whole assembly", ["src/Cobol.Net.Compiler/Enum.cs"], WHOLE),
        ("an ambient-only file selects the whole assembly", ["src/Cobol.Net.Compiler/B.cs"], WHOLE),
        ("a grammar file selects the whole assembly", ["src/Cobol.Net.Frontend/Grammar/Core/X.g4"], WHOLE),
        ("a props file selects the whole assembly", ["Directory.Build.props"], WHOLE),
        ("a runtime data file selects the whole assembly", ["src/Cobol.Net.Runtime/Collation/Data/x.bin"], WHOLE),
    ]
    failures = 0
    for label, files, want in cases:
        sel = Selection()
        select(ix, [("M", f) for f in files], None, sel)
        flt, _ = build_filter(ix, sel, [])
        ok = (flt == WHOLE) if want == WHOLE else (want in flt.split("|") and flt != WHOLE)
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}: {flt[:120]}")
    # The method level: a hunk inside Two() selects nothing (no test reached it); inside One() selects T.M1; a
    # comment-only hunk between them selects nothing; code inserted between them falls back to the file level.
    method_cases = [
        ("a hunk inside an unreached method selects no test", (32, 1, ["x = 1;"], ["x = 2;"]), 0),
        ("a hunk inside a reached method selects its tests", (12, 1, ["x = 1;"], ["x = 2;"]), 1),
        ("a comment-only hunk between methods selects nothing", (25, 0, [], ["// note"]), 0),
        ("code inserted between methods is a declaration change", (25, 0, [], ["    private int y = 3;"]), None),
        ("an override inserted between methods is a declaration change",
         (25, 0, [], ["    public override string ToString()", "    {", "        return \"\";", "    }"]), None),
        ("a new ordinary method selects no test by itself",
         (25, 0, [], ["    private static int Helper(int x)", "    {", "        return x;", "    }"]), 0),
    ]
    for label, h, want in method_cases:
        got = method_ids(ix, "src/Cobol.Net.Compiler/A.cs", h)
        ok = (got is None) if want is None else (got is not None and bool(got & 1) == bool(want))
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}: {got}")
    sel = Selection()
    find_map(Path(REPO / "tests" / "no-such-impact-store"), git("rev-parse", "HEAD"), sel)
    ok = bool(sel.whole)
    failures += 0 if ok else 1
    print(f"  {'PASS' if ok else '⛔ FAIL'}  no map for the base selects the whole assembly")
    print("ALL GREEN" if not failures else f"⛔ {failures} SELF-TEST FAILURE(S)")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("files", nargs="*", help="changed files (repo-relative), taken at the FILE level; default: "
                                              "the diff against --base, taken at the method level")
    ap.add_argument("--base", help="the change's base commit (the worktree's cut point)")
    ap.add_argument("--head", help="diff --base..--head instead of the worktree (replaying a branch or commit)")
    ap.add_argument("--map", type=Path, help="an explicit map file (otherwise the store is searched)")
    ap.add_argument("--store", type=Path, help="the map store (default <git common dir>/cobol-impact)")
    ap.add_argument("--plus", help="extra terms, '|'-joined, e.g. your own goldens by name")
    ap.add_argument("--explain", action="store_true", help="print every selection decision")
    ap.add_argument("--dump", type=Path, help="write the Conformance tests the MAP selected, one per line")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if args.self_test:
        return self_test()
    global HEAD
    HEAD = git("rev-parse", args.head) if args.head else None
    flt, sel, stats, ix = run(args)
    err = sys.stderr
    if args.dump and ix is not None:
        tests, classes = ix.selection(sel.mask)
        names = {ix.tests[i][2] for i in tests} | {ix.tests[i][2] for i in ix.conf if ix.tests[i][3] in classes}
        args.dump.write_text("\n".join(sorted(names)) + "\n", encoding="utf-8")
    for why in sel.whole:
        print(f"⛔ WHOLE ASSEMBLY: {why}", file=err)
    if args.explain or not sel.whole:
        for n in sel.notes:
            print(f"  {n}", file=err)
    if flt == WHOLE:
        print(f"impacted_tests: the WHOLE Conformance assembly ({len(sel.whole)} reason(s) above; the map alone "
              f"selected {stats.get('map_selected', 0)} of {stats.get('population', 0)} tests)", file=err)
    else:
        print(f"impacted_tests: {stats['selected_tests']} of {stats['population']} Conformance tests selected by "
              f"the map ({stats['terms']} terms; recorded time {stats.get('selected_seconds', 0)} s of "
              f"{stats.get('population_seconds', 0)} s), plus the standing terms; Unit and Characterization run "
              f"whole", file=err)
    print(flt)
    return 0


if __name__ == "__main__":
    sys.exit(main())
