#!/usr/bin/env python3
"""impacted_tests.py — what a change REACHES, as the order plan's TIERS (kb/Work PB1683, PB1708 M11, PB1712).

The map ORDERS the gate; it never SELECTS it (owner, 2026-09-28, kb/Work PB1708). Every implementer gate runs the
whole discovered population of Conformance, Unit and Characterization; this script tells `scripts/gate_plan.py`
which tests a change can affect, so they run FIRST and a red surfaces in minutes. It reads the RECORDED impact map
(`scripts/spec/record_impact_map.py`: per test, the METHODS its execution reached, each with the line ranges it
owns) and puts every recorded test in a tier (docs/rearchitecture/DESIGN-test-build-ci.md §3.13):

  tier 1  executed a changed method body (the direct hits);
  tier 2  reached only through a widening — a static constructor, a declaration change, the file level;
  tier 3  reached by nothing the change touched.

How a changed C# file reaches tests:
  * a hunk INSIDE the lines a method owns reaches the tests that executed that method (tier 1); a static
    constructor's change also reaches every test that executed any member of its type (tier 2 — a static
    initializer runs once);
  * a hunk that only touches blank or comment lines reaches nothing;
  * any other hunk — outside every method: a new member, a signature, a constant, an enum value, a field, an
    attribute, a `using` — reaches every test that executed ANY code in the file, plus every test that executed a
    file NAMING a type the file declares (tier 2: constants and enum values are compiled INTO their consumers);
  * without `--base` (files named on the command line) every file is taken at that file level.
A changed TEST file reaches tests the same way (the test assemblies are recorded too): an edited test body or a
theory's harness is tier 1 for the rows that executed it. It also names the tier-0a facts only the diff knows: the
test METHODS the change adds (no map or timing knows them yet), and the corpus goldens whose manifest entry or files
the change touches (`gate_plan.py` puts their cases in leg 1). A whole changed test FILE is not tier 0a: a harness
edit reaches every row of its theory, and 68b X's (b4) put 5,774 cases and 62.8 % of the work ahead of its first red.

EVERY TEST IS TIER 1, with the reason printed, whenever the change cannot be attributed: no map for the base, a map
older than the base by more than documentation (its line numbers describe another tree), a `.g4`, project, build or
source-generator input, a NEW src file, non-C# data under `src/`, a method reached only outside every test's
context. Nothing here can drop a test: a test the map under-reaches (static FIELD reads are invisible to the
recorder — PB1712) lands in a later tier of the SAME gate and still runs.

⛔ NO FILTER. This script prints no vstest filter and takes no `--plus`: M11 deleted the narrowing (a map for a base
re-armed a silent under-selection, PB1712), and M13 deleted the whole-assembly filter line and `--plus` together with
`build-local.ps1 -Filter` and every caller (kb/Work PB1721). Its one consumer is `scripts/gate_plan.py`, which the
gate driver `scripts/run_gate_legs.py` runs in-process; stdout is the tier summary, for people.

Usage:
    python scripts/spec/impacted_tests.py --base <sha>                 # the worktree's change against its base
    python scripts/spec/impacted_tests.py --base <sha> --plan tiers.json
    python scripts/spec/impacted_tests.py src/Cobol.Net.Compiler/Binding/DataBinder.Constants.cs
    python scripts/spec/impacted_tests.py --self-test
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
import tempfile
import zlib
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts"))
from test_population import GATED_ASSEMBLIES  # noqa: E402
SCHEMA = 2
CONFORMANCE_DIR = "tests/Cobol.Net.Tests.Conformance/"
# The gated test projects, by the assembly name the map and the gate use ("Conformance", "Unit", ...).
TEST_ASSEMBLIES = {f"{d}/": k for k, d in GATED_ASSEMBLIES.items()}
SHARED_TEST_DIR = "tests/_shared/"   # linked into every test project
TEST_PROJECTS = (*TEST_ASSEMBLIES, SHARED_TEST_DIR)
# Paths no test EXECUTES: they can only be READ by a test, which the reader search below finds.
DOC_GLOBS = ["docs/*", "kb/*", "*.md", ".claude/*", "adjudication/*", "specs/*", "scripts/*", "tools/impact/*",
             ".github/*", "samples/*"]
# Structural inputs: every assembly may change.
STRUCTURAL_RE = re.compile(r"(\.g4|\.csproj|\.props|\.targets|\.sln|global\.json|nuget\.config|\.editorconfig)$"
                           r"|^src/Cobol\.Net\.Compiler\.SourceGen/|(^|/)Directory\.[^/]+$", re.IGNORECASE)
TRIVIAL_RE = re.compile(r"^\s*(//.*|/\*.*|\*.*|)$")
HUNK_RE = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+\d+(?:,\d+)? @@")
HEAD: str | None = None  # --head: diff base..HEAD instead of the worktree (a replay, or a landed branch)
TIER_DIRECT, TIER_WIDENED, TIER_UNREACHED = 1, 2, 3


@dataclass
class Reach:
    """What a change reaches. `direct` and `widened` are masks of map entries (tiers 1 and 2); `every_tier1` holds
    the reasons the change cannot be attributed, which put every test in tier 1."""
    direct: int = 0
    widened: int = 0
    direct_names: set[str] = field(default_factory=set)            # display-name substrings reached outside the map
    added_tests: dict[str, set[str]] = field(default_factory=dict)  # assembly -> test methods the change ADDS
    goldens: set[str] = field(default_factory=set)                 # corpus goldens the change touches
    every_tier1: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)


@dataclass
class Analysis:
    """One change against one map: the map found for the base (possibly STALE — then its durations and test names
    still serve the plan, but its line numbers do not describe the tree, so every test is tier 1), what the change
    reaches, and each recorded test's tier per assembly, keyed by its RAW display name."""
    map: dict | None
    map_state: str            # "exact" · "documentation-only drift" · "stale" · "none"
    reach: Reach
    tiers: dict[str, dict[str, int]]


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
    """The map, decoded once: per file its entries and their line ranges; per recorded test its bitset."""

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
        # tests[i] = [assembly, method, display name, class, "assembly:collection", seconds, listed]
        self.tests = m["tests"]
        self.bits = [unpack(b) for b in m["test_bits"]]
        self.by_assembly: dict[str, list[int]] = {}
        for i, t in enumerate(self.tests):
            self.by_assembly.setdefault(t[0], []).append(i)
        self.class_bits = {k: unpack(v) for k, v in m["class_bits"].items()}            # "assembly:class"
        self.collection_bits = {k: unpack(v) for k, v in m["collection_bits"].items()}  # "assembly:collection"
        self.reached = 0
        for b in [*self.bits, *self.class_bits.values(), *self.collection_bits.values()]:
            self.reached |= b
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

    def executed(self, i: int) -> int:
        """Everything test i's execution reached: its own context, its class's (constructor, fixture) and its
        collection's."""
        asm, _, _, cls, coll = self.tests[i][:5]
        return self.bits[i] | self.class_bits.get(f"{asm}:{cls}", 0) | self.collection_bits.get(coll, 0)


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
    it — except through overload resolution, so it reaches the tests that reached any same-named method of the
    file's types. Anything that changes dispatch or state for UNCHANGED code — an override, a virtual, an
    operator or conversion, an extension method, a field, property or constant, a type with a base list, an
    attribute — returns None (a declaration change). A new xunit test attribute adds nothing here: the test
    method it declares is tier 0a (`Reach.added_tests`)."""
    mask = 0
    for line in added:
        if TRIVIAL_RE.match(line) or TEST_ATTR_RE.match(line):
            continue
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
    return sorted(set(re.findall(r"\b(?:class|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)", text_of(path))))


def assemblies_of(path: str) -> list[str]:
    """The gated test assemblies a test source file is compiled into (a `tests/_shared/` file: all of them)."""
    if path.startswith(SHARED_TEST_DIR):
        return list(TEST_ASSEMBLIES.values())
    return [a for d, a in TEST_ASSEMBLIES.items() if path.startswith(d)]


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
    texts = [text_of(REPO / path)]
    if base:
        try:
            texts.append(git("show", f"{base}:{path}"))
        except subprocess.CalledProcessError:
            pass
    types = declared_types(texts)
    if not types:
        return None
    pattern = re.compile(r"\b(?:" + "|".join(map(re.escape, types)) + r")\b")
    return [f for f in sorted(ix.executed_files)
            if f != path and f.endswith(".cs") and pattern.search(text_of(REPO / f))]


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
            if prog.stem.lower() != stem.lower() and word.search(text_of(prog)):
                todo.append(prog.relative_to(REPO))
    return goldens or None


def manifest_entries(path: str, base: str) -> set[str]:
    """The entries of a corpus manifest the change ADDS (present now, absent at the base)."""
    def enabled(rev: str | None) -> set[str]:
        try:
            text = git("show", f"{rev}:{path}") if rev else text_of(REPO / path)
            return set(json.loads(text).get("enabled", []))
        except (subprocess.CalledProcessError, ValueError, AttributeError):
            return set()  # absent on that side (an added or deleted manifest)
    return enabled(HEAD) - enabled(base)


def ancestor_distance(sha: str, base: str) -> int | None:
    """How many commits `sha` is behind `base`, or None when it is not an ancestor of `base`."""
    if subprocess.run(["git", "merge-base", "--is-ancestor", sha, base], cwd=REPO, capture_output=True).returncode:
        return None
    return int(git("rev-list", "--count", f"{sha}..{base}"))


def non_doc_drift(sha: str, base: str) -> list[str]:
    """The files other than documentation that differ between `sha` and `base`."""
    return [f for f in git("diff", "--name-only", sha, base).splitlines() if f and not is_doc(f)]


def find_map(store: Path, base: str, reach: Reach) -> tuple[dict | None, str]:
    """The map recorded at `base`; else the newest one at an ancestor of `base`. An ancestor's map whose difference
    from the base is only documentation describes the base exactly. Any other ancestor's map is STALE: its line
    numbers describe another tree, so every test is tier 1 — but its durations and test names still serve the plan."""
    exact = store / f"{base}.json.gz"
    if exact.exists():
        return load_map(exact), "exact"
    candidates = []
    for p in store.glob("*.json.gz") if store.is_dir() else []:
        sha = p.name.split(".")[0]
        if p.name != f"{sha}.json.gz":
            continue  # partial (trial) maps are never used implicitly
        distance = ancestor_distance(sha, base)
        if distance is not None:
            candidates.append((distance, sha, p))
    for _, sha, p in sorted(candidates):
        drift = non_doc_drift(sha, base)
        if not drift:
            reach.notes.append(f"map {sha[:12]} is older than the base {base[:12]} only by documentation — used")
            return load_map(p), "documentation-only drift"
        reach.every_tier1.append(f"the newest map at an ancestor ({sha[:12]}) is STALE: {len(drift)} "
                                 f"non-documentation file(s) changed between it and the base {base[:12]} (first: "
                                 f"{drift[0]}) — its durations and names are used, its line numbers are not")
        return load_map(p), "stale"
    reach.every_tier1.append(f"no impact map recorded for the base {base[:12]} or any ancestor in {store} — "
                             f"record one (record_impact_map.py --commit {base[:12]})")
    return None, "none"


TEST_METHOD_RE = re.compile(r"^\s*(?:(?:public|internal|protected|private|static|async)\s+)*"
                            r"(?:void|Task|ValueTask)\s+([A-Za-z_]\w*)\s*[(<]")


def test_methods(text: str) -> set[str]:
    """The names of the xunit test methods a C# source declares: the first method declaration after a `[Fact]` or
    `[Theory]` attribute line (data attributes and other attributes may sit between them)."""
    names, pending = set(), False
    for line in text.splitlines():
        if re.match(r"^\s*\[(Fact|Theory)\b", line):
            pending = True
        elif pending and (m := TEST_METHOD_RE.match(line)):
            names.add(m.group(1))
            pending = False
    return names


def reach_test_file(path: str, base: str | None, reach: Reach) -> None:
    """Tier 0a from the diff alone: the test methods a changed test source file ADDS (every one of them when the file
    is named without a diff), in each assembly the file is compiled into."""
    now = test_methods(git("show", f"{HEAD}:{path}") if HEAD else text_of(REPO / path))
    before: set[str] = set()
    if base:
        try:
            before = test_methods(git("show", f"{base}:{path}"))
        except subprocess.CalledProcessError:
            pass  # an added file
    added = now - before
    for asm in assemblies_of(path):
        reach.added_tests.setdefault(asm, set()).update(added)
    if added:
        reach.notes.append(f"{path}: adds test method(s) {', '.join(sorted(added))} — tier 0a")


def reach_cs(ix: MapIndex, path: str, status: str, base: str | None, reach: Reach) -> None:
    if path not in ix.ranges:
        if path.startswith(TEST_PROJECTS):
            return  # never executed by the recording: its tests are tier 0a, and nothing else runs it
        if path not in ix.known:
            reach.every_tier1.append(f"{path}: the map has never seen it (a file NEW since the map's commit)")
            return
        declaration_change(ix, path, base, reach, "declarations only (no method of it has sequence points)")
        return
    if base is None or status != "M":
        declaration_change(ix, path, base, reach, "named without a diff" if base is None else f"status {status}")
        return
    mask = 0
    for h in hunks(base, path):
        ids = method_ids(ix, path, h)
        if ids is None:
            declaration_change(ix, path, base, reach, f"a change at line {h[0]} outside every method")
            return
        mask |= ids
    reach.direct |= mask
    reach.widened |= ix.expand(mask)
    names = sorted({ix.entries[i][0].split("::")[-1].split("(")[0] for i in bit_ids(mask)})
    reach.notes.append(f"{path}: {len(names)} changed method(s) {', '.join(names[:6])}"
                       + (" …" if len(names) > 6 else "") if names else f"{path}: comments and blank lines only")


def declaration_change(ix: MapIndex, path: str, base: str | None, reach: Reach, why: str) -> None:
    """The file level: every test that executed the file, and every test that executed a file naming its types."""
    users = consumers(ix, path, base)
    own = ix.file_ids.get(path, 0)
    if not own and not users:
        reach.every_tier1.append(f"{path}: {why}, and no executed file names a type it declares — cannot bound it")
        return
    reach.widened |= ix.expand(own)
    for u in users or []:
        reach.widened |= ix.file_ids[u]
    reach.notes.append(f"{path}: {why} — the file level, plus the {len(users or [])} executed file(s) naming its types")


def reach_of(ix: MapIndex | None, changes: list[tuple[str, str]], base: str | None, reach: Reach) -> None:
    """Fill `reach` for the changed files. The tier-0a facts (added test methods, corpus goldens) come from the diff
    alone and are gathered with or without a map; the tiers need the map."""
    sources = None
    for status, path in changes:
        if path.startswith("tools/impact/"):
            reach.notes.append(f"{path}: the impact recorder itself — only a recording build compiles it")
            continue
        if STRUCTURAL_RE.search(path):
            reach.every_tier1.append(f"{path}: a grammar, build or source-generator input — every assembly may change")
            continue
        if path.endswith(".cs") and path.startswith(("src/", "tests/")):
            if path.startswith("tests/") and not path.startswith(TEST_PROJECTS):
                reach.notes.append(f"{path}: in a test project outside the three gated assemblies — no gated test "
                                   f"runs it")
                continue
            if ix is not None:
                reach_cs(ix, path, status, base, reach)
            if path.startswith(TEST_PROJECTS):
                reach_test_file(path, base, reach)
            continue
        if path.startswith("src/"):
            reach.every_tier1.append(f"{path}: a non-C# input under src/ (embedded data or resources)")
            continue
        if path.startswith("tests/conformance/"):
            if path.endswith("manifest.json"):
                if base is None:
                    reach.notes.append(f"{path}: named without a diff — which entries changed is unknown")
                else:
                    added = manifest_entries(path, base)
                    reach.goldens |= added
                    reach.notes.append(f"{path}: manifest entries added: {', '.join(sorted(added)) or 'none'}")
            else:
                goldens = corpus_goldens(path)
                if goldens is None:
                    reach.every_tier1.append(f"{path}: corpus data no golden program names — cannot attribute it")
                else:
                    reach.goldens |= goldens
                    reach.notes.append(f"{path}: the golden(s) {', '.join(sorted(goldens))}")
                continue
        if ix is None:
            continue
        if path.startswith("tests/nist/"):
            reach.direct_names.add("Nist")
        if path.startswith("tests/version-matrix/"):
            reach.direct_names.add("VersionMatrix")
        # Anything else is DATA some test may READ: the tests whose source names it.
        sources = sources if sources is not None else test_sources()
        name = Path(path).name
        who = readers(name, sources)
        if who:
            for r in who:
                if r in ix.file_ids:
                    reach.direct |= ix.file_ids[r]
                else:
                    reach.direct_names.update(f".{c}." for c in classes_declared(REPO / r))
            reach.notes.append(f"{path}: read by {len(who)} test source file(s) that name '{name}'")
        elif path.startswith(("tests/nist/", "tests/version-matrix/")):
            reach.notes.append(f"{path}: the {'Nist' if 'nist' in path else 'VersionMatrix'} tests read it")
        elif is_doc(path) or path.endswith("manifest.json"):
            reach.notes.append(f"{path}: no test source names it — documentation or data the tiers do not need")
        elif path.startswith("tests/"):
            reach.every_tier1.append(f"{path}: test data no test source names and no rule attributes")
        else:
            reach.notes.append(f"{path}: no test source names it and it is not built — not a test input")
    if ix is None:
        return
    # ⛔ A changed entry reached ONLY outside every test's context. In a test file that is xunit DISCOVERY running
    # a theory's data source — the file is changed, so its classes are already tier 0a; in product code it is a
    # finalizer or a thread that does not flow the context, and nothing attributes it.
    orphan = (reach.direct | reach.widened) & ix.ambient & ~ix.reached
    unattributed = [ix.entries[i][0] for i in bit_ids(orphan)
                    if not all(f.startswith(TEST_PROJECTS) for f, _, _ in ix.entries[i][1])]
    if unattributed:
        reach.every_tier1.append(f"{len(unattributed)} changed method(s) executed only OUTSIDE every test's context "
                                 f"(e.g. {unattributed[0]}) — cannot attribute them")


def tiers(ix: MapIndex, reach: Reach) -> dict[str, dict[str, int]]:
    """Per assembly, per recorded display name, its tier (1 · 2 · 3). Rows sharing a display name (xunit truncates
    long theory arguments) take the lowest tier among them."""
    out: dict[str, dict[str, int]] = {}
    for i, t in enumerate(ix.tests):
        asm, display = t[0], t[2]
        if reach.every_tier1:
            tier = TIER_DIRECT
        else:
            executed = ix.executed(i)
            if executed & reach.direct or any(n in display for n in reach.direct_names):
                tier = TIER_DIRECT
            elif executed & reach.widened:
                tier = TIER_WIDENED
            else:
                tier = TIER_UNREACHED
        per = out.setdefault(asm, {})
        per[display] = min(per.get(display, TIER_UNREACHED), tier)
    return out


def analyse(base: str | None, files: list[str] | None = None, map_path: Path | None = None,
            store: Path | None = None) -> Analysis:
    """The change (the worktree, or `HEAD`, against `base`; or `files` at the file level) against its map."""
    reach = Reach()
    base = git("rev-parse", base) if base else None
    if map_path:
        m, state = load_map(map_path), "exact"
        if m.get("partial"):
            reach.notes.append(f"⚠ {map_path} is a PARTIAL (trial) map over {m.get('filter')!r}")
    else:
        m, state = find_map(store or default_store(), base or git("rev-parse", "HEAD"), reach)
    ix = MapIndex(m) if m is not None else None
    changes = [("M", f) for f in files] if files else (changed_files(base) if base else [])
    if not changes:
        reach.notes.append("no changed files")
    reach_of(ix if state != "stale" else None, changes, base if not files else None, reach)
    return Analysis(m, state, reach, tiers(ix, reach) if ix is not None else {})


def plan_json(a: Analysis) -> dict:
    """The analysis as `--plan` writes it — the same one gate_plan.py computes in-process through `analyse`."""
    return {"map": a.map["commit"] if a.map else None, "map_state": a.map_state,
            "every_tier1": a.reach.every_tier1, "tiers": a.tiers,
            "tier0a": {"added_tests": {k: sorted(v) for k, v in sorted(a.reach.added_tests.items())},
                       "goldens": sorted(a.reach.goldens)},
            "notes": a.reach.notes}


def synthetic_map() -> dict:
    """Two methods in A.cs (lines 10-20 and 30-40), a static constructor of A implied by One(), a type-level entry in
    B.cs reached only ambiently, and three Conformance tests: M1 reaches One(), M2 only the constructor, M3 nothing
    of A."""
    def pack(n: int) -> str:
        return base64.b64encode(zlib.compress(n.to_bytes((n.bit_length() + 7) // 8, "little"))).decode()
    return {"schema": SCHEMA, "commit": "0" * 40,
            "entries": [["N.A::One()", [["src/Cobol.Net.Compiler/A.cs", 10, 20]]],
                        ["N.A::Two()", [["src/Cobol.Net.Compiler/A.cs", 30, 40]]],
                        ["N.B <type-level>", [["src/Cobol.Net.Compiler/B.cs", 0, 0]]],
                        ["N.A::.cctor()", [["src/Cobol.Net.Compiler/A.cs", 5, 8]]]],
            "implies": {"0": [3]},
            "known_files": ["src/Cobol.Net.Compiler/A.cs", "src/Cobol.Net.Compiler/B.cs",
                            "src/Cobol.Net.Compiler/Enum.cs"],
            "tests": [["Conformance", "N.T.M1", "N.T.M1", "N.T", "Conformance:c1", 1.0, True],
                      ["Conformance", "N.T.M2", "N.T.M2", "N.T", "Conformance:c1", 1.0, True],
                      ["Conformance", "N.T.M3", "N.T.M3", "N.T", "Conformance:c1", 1.0, True]],
            "test_bits": [pack(0b0001), pack(0b1000), pack(0)], "class_bits": {}, "collection_bits": {},
            "ambient_bits": pack(0b0100)}


def self_test() -> int:
    """Prove each arm FIRES against a synthetic map (feedback_prove_the_watchdog_fails)."""
    ix = MapIndex(synthetic_map())
    failures = 0

    def check(label: str, ok: bool, detail: object) -> None:
        nonlocal failures
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}: {detail}")

    def tiers_for(files: list[str]) -> tuple[dict[str, int], Reach]:
        reach = Reach()
        reach_of(ix, [("M", f) for f in files], None, reach)
        return tiers(ix, reach)["Conformance"], reach

    t, _ = tiers_for(["src/Cobol.Net.Compiler/A.cs"])
    check("a mapped file (file level) puts its tests in tier 2 and the others in tier 3",
          t == {"N.T.M1": 2, "N.T.M2": 2, "N.T.M3": 3}, t)
    for label, f in [("an UNMAPPED (new) src file puts every test in tier 1", "src/Cobol.Net.Compiler/New.cs"),
                     ("a known file no test executed puts every test in tier 1", "src/Cobol.Net.Compiler/Enum.cs"),
                     ("an ambient-only file puts every test in tier 1", "src/Cobol.Net.Compiler/B.cs"),
                     ("a grammar file puts every test in tier 1", "src/Cobol.Net.Frontend/Grammar/Core/X.g4"),
                     ("a props file puts every test in tier 1", "Directory.Build.props"),
                     ("a runtime data file puts every test in tier 1", "src/Cobol.Net.Runtime/Collation/Data/x.bin")]:
        t, reach = tiers_for([f])
        check(label, set(t.values()) == {1} and bool(reach.every_tier1), reach.every_tier1[:1])
    planted = "\n".join(["public sealed class T", "{", "    [Fact]", "    public void A() { }", "    [Theory]",
                         "    [InlineData(\"x]\")]", "    public async Task B(string s) { }", "    [Fact(Skip = \"y\")]",
                         "    [Trait(\"k\", \"v\")]", "    public void C<TSlot>() { }",
                         "    private static int Helper() => 1;", "}"])
    got = test_methods(planted)
    check("an added test method is named for tier 0a (Fact, Theory with data, async, generic; never a helper)",
          got == {"A", "B", "C"}, sorted(got))
    t, reach = tiers_for(["tests/_shared/TestPartitioning.cs"])
    check("a shared test file is compiled into every assembly", set(reach.added_tests) == set(TEST_ASSEMBLIES.values()),
          sorted(reach.added_tests))

    # The method level: the entries a hunk changes (0 = One(), which M1 reached; 1 = Two(), which no test reached),
    # or None for a declaration change, which falls back to the file level.
    method_cases = [
        ("a hunk inside an unreached method reaches no test", (32, 1, ["x = 1;"], ["x = 2;"]), {1}),
        ("a hunk inside a reached method changes that method", (12, 1, ["x = 1;"], ["x = 2;"]), {0}),
        ("a comment-only hunk between methods reaches nothing", (25, 0, [], ["// note"]), set()),
        ("code inserted between methods is a declaration change", (25, 0, [], ["    private int y = 3;"]), None),
        ("an override inserted between methods is a declaration change",
         (25, 0, [], ["    public override string ToString()", "    {", "        return \"\";", "    }"]), None),
        ("a new ordinary method reaches no test by itself",
         (25, 0, [], ["    private static int Helper(int x)", "    {", "        return x;", "    }"]), set()),
    ]
    for label, h, want in method_cases:
        got = method_ids(ix, "src/Cobol.Net.Compiler/A.cs", h)
        check(label, (got is None) if want is None else (got is not None and set(bit_ids(got)) == want), got)
    reach = Reach()
    reach.direct |= 1 << 0
    reach.widened |= ix.expand(1 << 0)
    t = tiers(ix, reach)["Conformance"]
    check("a hunk inside a reached method puts its tests in tier 1 and the rest in tier 3",
          t == {"N.T.M1": 1, "N.T.M2": 3, "N.T.M3": 3}, t)
    reach = Reach()
    reach.direct |= 1 << 3
    reach.widened |= ix.expand(1 << 3)
    t = tiers(ix, reach)["Conformance"]
    check("a changed static constructor: its own tests tier 1, its type's users tier 2, the rest tier 3",
          t == {"N.T.M1": 2, "N.T.M2": 1, "N.T.M3": 3}, t)

    r = Reach()
    m, state = find_map(Path(REPO / "tests" / "no-such-impact-store"), git("rev-parse", "HEAD"), r)
    check("no map for the base puts every test in tier 1", m is None and state == "none" and bool(r.every_tier1),
          r.every_tier1[:1])

    # A map at an ANCESTOR of the base: documentation-only drift describes the base; any other drift is STALE —
    # still returned (durations, names) but every test tier 1. Git ancestry is planted, so a shallow clone runs it.
    global ancestor_distance, non_doc_drift
    real = (ancestor_distance, non_doc_drift)
    with tempfile.TemporaryDirectory() as store:
        with gzip.open(Path(store) / f"{'a' * 40}.json.gz", "wt", encoding="utf-8") as fh:
            json.dump(synthetic_map(), fh)
        try:
            ancestor_distance = lambda sha, base: 1  # noqa: E731
            for label, drift, want in [("a documentation-only older map is used as exact", [],
                                        "documentation-only drift"),
                                       ("a STALE map is returned for its durations but puts every test in tier 1",
                                        ["src/Cobol.Net.Compiler/A.cs"], "stale")]:
                non_doc_drift = lambda sha, base, d=drift: d  # noqa: E731
                r = Reach()
                m, state = find_map(Path(store), "b" * 40, r)
                ix_old = MapIndex(m) if m else None
                t = tiers(ix_old, r)["Conformance"] if ix_old else {}
                ok = state == want and m is not None and (
                    (bool(r.every_tier1) and set(t.values()) == {1}) if want == "stale" else not r.every_tier1)
                check(label, ok, f"{state}; {r.every_tier1[:1]}")
        finally:
            ancestor_distance, non_doc_drift = real
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
    ap.add_argument("--plan", type=Path, help="write the tiers (JSON) that gate_plan.py reads to this file")
    ap.add_argument("--explain", action="store_true", help="print every reach decision")
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
    a = analyse(args.base, args.files, args.map, args.store)
    if args.plan:
        args.plan.write_text(json.dumps(plan_json(a), indent=1, sort_keys=True) + "\n", encoding="utf-8")
    err = sys.stderr
    for why in a.reach.every_tier1:
        print(f"  every test tier 1: {why}", file=err)
    if args.explain or not a.reach.every_tier1:
        for n in a.reach.notes:
            print(f"  {n}", file=err)
    counts = {asm: dict(sorted(Counter(t.values()).items())) for asm, t in sorted(a.tiers.items())}
    print(f"impacted_tests: map {a.map_state}; tiers per assembly {counts or '(no map: every test tier 1)'}; "
          f"tier 0a: {len(set().union(*a.reach.added_tests.values()))} added test method(s), {len(a.reach.goldens)} "
          f"golden(s). The gate runs the WHOLE population in this order (scripts/run_gate_legs.py)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
