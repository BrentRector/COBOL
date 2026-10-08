#!/usr/bin/env python3
"""member_index.py — the member index: a restructuring note's file set COMPUTED from the code, never hand-listed
(kb/Work PB2118 Draft 8, J2; docs/rearchitecture/DESIGN-architecture-review.md §8.7 "the file-set partition").

WHY. A wave that moves or changes a symbol edits every file that uses it in the same change (CLAUDE.md rule 4: no
alias, shim or forwarder), so those files are in its file set, and the file-set partition (the ONLY gate between a
restructuring wave and the fix lane's trains; owner, PB2118 question 5) protects only what the set holds. Draft 7's
PB2296 hand-listed 15 files and missed 16 callers (the seventh refuter, J2); `scripts/spec/where.py` maps spec clauses
to files, not C# members to callers. This is the mechanism.

WHAT. The census's Roslyn host (`tools/ArchCensus --member-index`) runs the census's one semantic walk over every
product and test project and writes, per symbol NAME (`Ns.Type.Member`, overloads merged), the authored files that
use it — a type's entry also holds every file that uses any of its members — and, per type and per authored member
(used or not), the files declaring it, and, for every type and member of the EMITTED SURFACE (the runtime generated
programs call), the authored files that write its name as C# TEXT (`emitted`: the code generator's `catch
(ProgramReturn)` binds no symbol; PB2118 Draft 9, the eighth refuter's K1).
This script records that at one commit as frozen evidence (`docs/rearchitecture/evidence/arch-census/<sha>.members.json.gz`,
with every walked file's git blob id), and answers queries against the CURRENT tree:

  * a note's NAMES: every name on its `**Moves or changes:**` line and every documentation id in a code span
    (`M:Ns.T.Member(...)`, the Delete notes' sites), PLUS every word of its target paragraph that is a member of a
    type the note names or its sites declare (`Options` in PB2296's target is `DataBinder.Options`: a hand-listed
    line missed it, the eighth refuter's K1); a note naming none falls back to every type DECLARED in the files and
    folders its sites name (a Move/rename note's folder, an Extract note's class files) — the conservative set;
  * their CALLERS: the index's files for each name (a type with its nested types and declaring files; a member with
    its type's declaring files; the emitters writing either as text), plus every tracked file under a FOLDER a site
    names, on the current tree, plus DRIFT: every tracked `.cs` under src/ or tests/ whose blob differs from the
    index's (or that is new since) is scanned for each name's last segment as a whole word and included on a hit;
    files deleted since drop out. Drift widens the callers of a name the index KNOWS; it cannot see a type a landing
    ADDED (the index never named it), which is why the planner never plans on a stale index;
  * a name the index does not know is UNKNOWN and reported (a typo or a renamed member is a failure, not a smaller set).

CURRENCY (the eighth refuter's K1 (3): PB833 added `IO/Sharing/OfdRegionLocks.cs` inside PB2251's site folder after
the index was recorded, and the frozen index never saw it). Every index carries `treeKey`, the hash of the inputs it
was walked from (`tree_key`: the tracked C#, project and solution files under src/ and tests/, and the host's own
sources). `current()` returns the index whose key is the checkout's: a committed record, or one regenerated into a
cache directory (the coordination directory's `member-index/`), or, with regeneration off, a REFUSAL. The planner
(`plan_wave.py --cluster`) plans only on a current index. The planner's set is still the ADMISSION estimate; the
GUARANTEE is the landing check (`scripts/orchestrator/landing_check.py`, run by push-main.sh), which compares each
landing's ACTUAL diff with its declared set and with every in-flight branch (DESIGN-architecture-review §8.7).

Callers: `plan_wave.py --cluster` adds a note's callers to its sites before the collision check (`file_set_collisions`),
`file_census_notes.py` renders them as the note's "files this step edits", `work.py check` fails an open PB2119
note whose names are unknown, and the dispatch guard's hand-dispatch record uses `load()` (the newest committed
record plus drift and folder expansion: fast, and widening only; a hook has 90 seconds).

Usage:
    python scripts/arch/member_index.py --record            # build Cobol.Net.sln, run the host, write the record for HEAD
    python scripts/arch/member_index.py --current --cache <dir>   # the index of this tree: found, or regenerated
    python scripts/arch/member_index.py --note PB2296       # print a note's names, unknowns and computed file set
    python scripts/arch/member_index.py --self-test
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[2]
RECORD_DIR = Path("docs/rearchitecture/evidence/arch-census")
SUFFIX = ".members.json.gz"   # gzip: the index names every symbol (about 4.5 MB as text)
HOST = Path("tools/ArchCensus")
SCANNED_ROOTS = ("src/", "tests/")
SCHEMA = 2   # 2: `emitted` (names written as C# text) and `treeKey` (PB2118 Draft 9)
KEY_INPUTS = ("src/", "tests/", "tools/ArchCensus/", "Cobol.Net.sln")   # what the walk reads: tree_key hashes them
KEY_EXTS = (".cs", ".csproj", ".props", ".targets", ".sln")
FROZEN = ("⛔ FROZEN EVIDENCE — the member index of ONE commit, recorded by scripts/arch/member_index.py --record and "
          "never edited (kb/Work PB785, PB2118 Draft 9): a newer measurement is a new file named by its commit. It "
          "describes the tree `treeKey` names and no other: the planner plans only on the index of its own tree "
          "(member_index.current); drift re-scanning widens the callers of the names it knows but cannot see a type "
          "added since (the eighth refuter's K1).")
MOVES_LINE = re.compile(r"^\*\*Moves or changes:\*\*(.*)$", re.M)
TARGET_PARA = re.compile(r"^\*\*This note's target[^*]*\*\*(.*)$", re.M)
TARGET_WORD = re.compile(r"(?<![A-Za-z0-9_])([A-Z][A-Za-z0-9_]+)(?![A-Za-z0-9_])")
CODE_SPAN = re.compile(r"`([^`]+)`")
DOC_ID = re.compile(r"`[MPFETN]:([^`\s@]+)")
SITE_PATH = re.compile(r"`((?:src|tests)/[^`\s:#]+)")


def name_of_doc_id(text: str) -> str:
    """`Ns.T.M(args)~Ret` (after the `M:` prefix) → `Ns.T.M(args)`, the index's key form (one entry per overload)."""
    return text.split("~", 1)[0]


def simple(name: str) -> str:
    """`Ns.T`1.M``1(args)` → `Ns.T.M`: the bare form a note's `Moves or changes` line writes, which means every
    overload."""
    return re.sub(r"``?\d+", "", name.split("(", 1)[0])


# ── the record ─────────────────────────────────────────────────────────────────────────────────────────────────
def git(*args: str, cwd: Path = REPO) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout


def tracked_blobs(repo: Path = REPO) -> dict[str, str]:
    """{path: blob id} of every tracked .cs under src/ and tests/ (the staged blob; unstaged edits are drift too)."""
    out = {}
    for line in git("ls-files", "-s", "--", *SCANNED_ROOTS, cwd=repo).splitlines():
        meta, _, path = line.partition("\t")
        if path.endswith(".cs"):
            out[path] = meta.split()[1]
    return out


def tree_key(repo: Path = REPO) -> str | None:
    """The hash of what the walk reads (every tracked C#, project and solution file under KEY_INPUTS, by staged blob
    id, and the schema), or None when one has an unstaged or untracked edit (a dirty tree has no recordable index)."""
    if git("status", "--porcelain", "--", *KEY_INPUTS, cwd=repo).strip():
        return None
    lines = [ln for ln in git("ls-files", "-s", "--", *KEY_INPUTS, cwd=repo).splitlines()
             if ln.partition(chr(9))[2].endswith(KEY_EXTS)]
    return hashlib.sha256((f"schema {SCHEMA}" + chr(10) + chr(10).join(sorted(lines))).encode("utf-8")).hexdigest()


def record(repo: Path = REPO, out_dir: Path | None = None) -> Path:
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import census  # noqa: PLC0415 — the census owns the scope rule; one rule in one place

    if git("status", "--porcelain", "--", *SCANNED_ROOTS, cwd=repo).strip():
        raise SystemExit("⛔ member_index --record: src/ or tests/ has uncommitted changes; an index describes one tree")
    sha = git("rev-parse", "HEAD", cwd=repo).strip()
    key = tree_key(repo)
    date = git("show", "-s", "--format=%cI", sha, cwd=repo).strip()
    work = Path(tempfile.mkdtemp(prefix="member-index-"))
    scope = census.scope_of(census.solution_projects((repo / "Cobol.Net.sln").read_text(encoding="utf-8-sig")))
    (work / "scope.json").write_text(json.dumps(scope), encoding="utf-8")
    census.run_logged(["dotnet", "build", "Cobol.Net.sln", "-c", "Debug", "-v", "quiet"], repo, work / "build.log")
    census.run_logged(["dotnet", "build", str(repo / HOST), "-c", "Release", "-o", str(work / "host"), "-v", "quiet"],
                      repo, work / "host-build.log")
    census.run_logged(["dotnet", str(work / "host" / "ArchCensus.dll"), "--solution", str(repo / "Cobol.Net.sln"),
                       "--scope", str(work / "scope.json"), "--member-index", str(work / "raw.json")], repo,
                      work / "host.log")
    raw = json.loads((work / "raw.json").read_text(encoding="utf-8"))
    blobs = tracked_blobs(repo)
    if raw.get("schema") != SCHEMA:
        raise SystemExit(f"⛔ member_index --record: the host wrote schema {raw.get('schema')}, this script reads {SCHEMA}")
    doc = {"_frozen": FROZEN, "schema": SCHEMA, "commit": sha, "commitDate": date, "treeKey": key,
           "instrument": {"script": "scripts/arch/member_index.py", "host": "tools/ArchCensus --member-index",
                          "note": "kb/Work/PB2118 (Draft 8, J2; Draft 9, K1)"},
           "blobs": {f: blobs[f] for f in raw["files"] if f in blobs},
           "files": raw["files"], "uses": raw["uses"], "types": raw["types"], "declared": raw["declared"],
           "emitted": raw["emitted"]}
    out = (out_dir / f"{key}{SUFFIX}") if out_dir else (repo / RECORD_DIR / f"{sha}{SUFFIX}")
    out.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(doc, ensure_ascii=False, separators=(",", ":"))
    out.write_bytes(gzip.compress(text.encode("utf-8"), mtime=0))
    return out


def _read(p: Path) -> dict:
    return json.loads(gzip.decompress(p.read_bytes()).decode("utf-8"))


def _records(repo: Path) -> list[dict]:
    """The committed records this script reads (schema SCHEMA; an older schema is frozen history, never read)."""
    return [d for d in (_read(p) for p in sorted((repo / RECORD_DIR).glob(f"*{SUFFIX}"))) if d.get("schema") == SCHEMA]


def load(repo: Path = REPO) -> dict:
    """The newest committed index (by commit date), for the readers that widen by drift (work.py check, the dispatch
    record). A missing index is a refusal: a file set is never guessed."""
    found = _records(repo)
    if not found:
        raise SystemExit(f"⛔ no schema-{SCHEMA} member index under {RECORD_DIR}: run "
                         "`python scripts/arch/member_index.py --record`")
    return with_simple(max(found, key=lambda d: d["commitDate"]))


class StaleIndex(SystemExit):
    """No index describes this tree and regeneration is off (or impossible)."""


def current(repo: Path = REPO, cache_dir: Path | None = None, regenerate: bool = True,
            recorder: Callable[[Path, Path], Path] | None = None) -> dict:
    """THE index of this checkout's tree (tree_key): a committed or cached record with that key, else one regenerated
    into `cache_dir` (`recorder(repo, cache_dir)`, default `record`), else a refusal (StaleIndex). What the planner
    plans on (PB2118 Draft 9, the eighth refuter's K1 (3))."""
    key = tree_key(repo)
    if key is None:
        raise StaleIndex("⛔ member index: src/, tests/ or the host has uncommitted changes; an index describes one "
                         "committed tree: commit, or plan from a clean checkout")
    cached = [_read(p) for p in sorted(cache_dir.glob(f"{key}{SUFFIX}"))] if cache_dir and cache_dir.is_dir() else []
    for doc in _records(repo) + cached:
        if doc.get("treeKey") == key and doc.get("schema") == SCHEMA:
            return with_simple(doc)
    if not regenerate or cache_dir is None:
        raise StaleIndex(f"⛔ member index: no index describes this tree (treeKey {key[:12]}); a stale index cannot "
                         f"see a file or type a landing added (PB2118 Draft 9). Regenerate: python "
                         f"scripts/arch/member_index.py --current --cache <coord>/member-index")
    return with_simple(_read((recorder or record)(repo, cache_dir)))


def with_simple(index: dict) -> dict:
    """The index plus `_simple`: {bare name: every key it stands for} over its three maps."""
    index["_simple"] = {}
    index.setdefault("emitted", {})
    for m in ("uses", "types", "declared"):
        for k in index[m]:
            index["_simple"].setdefault(simple(k), set()).add(k)
    return index


# ── queries (pure over an index dict; the self-test drives them on planted indexes) ────────────────────────────
def _files(index: dict, key: str, name: str) -> set[str]:
    return {index["files"][i] for i in index[key].get(name, [])}


def keys_for(index: dict, name: str) -> set[str]:
    """The index keys a name stands for: itself when the index has it (an exact overload, a type), else every key
    with its bare form (a bare member name means every overload). Empty: unknown."""
    if any(name in index[m] for m in ("uses", "types", "declared")):
        return {name}
    return set(index["_simple"].get(simple(name), ()))


def resolve(index: dict, names: list[str]) -> tuple[set[str], list[str]]:
    """(the index's files for the names, the names it does not know). A type brings its nested types and its declaring
    files; a member brings its own uses, its declaring files and its type's declaring files."""
    out: set[str] = set()
    unknown = []
    for n in names:
        keys = keys_for(index, n)
        if not keys:
            unknown.append(n)
        for k in keys:
            out |= _files(index, "uses", k) | _files(index, "types", k) | _files(index, "emitted", k)
            if k in index["types"]:
                for t in index["types"]:
                    if t.startswith(k + "."):
                        out |= _files(index, "uses", t) | _files(index, "types", t) | _files(index, "emitted", t)
            else:
                for t in index["_simple"].get(simple(k).rsplit(".", 1)[0], ()):
                    out |= _files(index, "types", t)
                out |= _files(index, "declared", k)
    return out, unknown


def types_declared_in(index: dict, paths: list[str]) -> list[str]:
    """Every type whose declaring file is one of `paths` or lies under one of them (a folder)."""
    roots = [p.rstrip("/") for p in paths]
    return sorted(t for t in index["types"]
                  if any(f == r or f.startswith(r + "/") for f in _files(index, "types", t) for r in roots))


def target_names(text: str, index: dict, named: list[str]) -> list[str]:
    """The members a note's TARGET paragraph names: every capitalized word of it that is a member of a SUBJECT type
    (a type the note's names are members of, or are, or a type its sites declare). Mechanical, so a hand-listed Moves
    line cannot leave out what the target says moves (PB2296's `Options`, the eighth refuter's K1 (1))."""
    m = TARGET_PARA.search(text)
    if not m:
        return []
    subjects = set(types_declared_in(index, sorted(set(SITE_PATH.findall(text)))))
    for n in named:
        for k in keys_for(index, n):
            base = simple(k)
            subjects.add(base if k in index["types"] else base.rsplit(".", 1)[0])
    subjects = {s for s in subjects if s in index["_simple"]}
    out = []
    for word in dict.fromkeys(TARGET_WORD.findall(m.group(1))):
        out += [f"{s}.{word}" for s in sorted(subjects) if f"{s}.{word}" in index["_simple"]]
    return out


def note_names(text: str, index: dict) -> tuple[list[str], str]:
    """(the names a note moves or changes, how they were found): its `**Moves or changes:**` line and the
    documentation ids in its code spans, plus the members its target paragraph names (target_names); else every type
    declared in the files and folders its sites name."""
    names = []
    if m := MOVES_LINE.search(text):
        names += [s.strip() for s in CODE_SPAN.findall(m.group(1))]
    names += [name_of_doc_id(d) for d in DOC_ID.findall(text)]
    if names:
        have = {simple(n) for n in names}
        derived = [n for n in target_names(text, index, names) if n not in have]
        return sorted(dict.fromkeys(names + derived)), "declared" + (" and its target" if derived else "")
    return types_declared_in(index, sorted(set(SITE_PATH.findall(text)))), "types declared in its sites"


def folder_files(text: str, repo: Path = REPO) -> set[str]:
    """Every tracked .cs file under a FOLDER the note's sites name, on the current tree: a file a landing added inside
    a site folder belongs to the note whatever the index knew (PB2251's OfdRegionLocks.cs, the eighth refuter's K1)."""
    folders = [p.rstrip("/") for p in dict.fromkeys(SITE_PATH.findall(text)) if (repo / p).is_dir()]
    if not folders:
        return set()
    return {p for p in git("ls-files", "--", *folders, cwd=repo).splitlines() if p.endswith(".cs")}


def drift_hits(names: list[str], drifted: dict[str, str]) -> set[str]:
    """The drifted files ({path: text}) naming any name's last segment as a whole word."""
    words = {simple(n).rsplit(".", 1)[-1] for n in names}
    if not words:
        return set()
    pat = re.compile(r"(?<![A-Za-z0-9_])(?:" + "|".join(sorted(map(re.escape, words))) + r")(?![A-Za-z0-9_])")
    return {p for p, t in drifted.items() if pat.search(t)}


def drifted_files(index: dict, repo: Path = REPO) -> dict[str, str]:
    """{path: text} of every tracked .cs under src/ and tests/ whose blob differs from the index's, is new since it,
    or has an unstaged edit."""
    now = tracked_blobs(repo)
    edited = {p for p in git("diff", "--name-only", "--no-renames", "--", *SCANNED_ROOTS, cwd=repo).splitlines()
              if p.endswith(".cs")}   # --no-renames: a moved file is drifted at both paths (PB2118 Draft 10, K1)
    paths = {p for p, b in now.items() if index["blobs"].get(p) != b} | edited
    return {p: (repo / p).read_text(encoding="utf-8", errors="replace") for p in sorted(paths) if (repo / p).is_file()}


def callers(index: dict, names: list[str], repo: Path = REPO, drifted: dict[str, str] | None = None
            ) -> tuple[set[str], list[str]]:
    """(every file a change to `names` must edit, on the CURRENT tree; the unknown names)."""
    files, unknown = resolve(index, names)
    drifted = drifted_files(index, repo) if drifted is None else drifted
    files |= drift_hits(names, drifted)
    return {f for f in files if (repo / f).is_file()}, unknown


def note_file_set(text: str, repo: Path = REPO, index: dict | None = None,
                  drifted: dict[str, str] | None = None) -> dict:
    """{names, how, unknown, files} of one note: what plan_wave adds to its sites, what file_census_notes renders."""
    index = load(repo) if index is None else index
    names, how = note_names(text, index)
    files, unknown = callers(index, names, repo, drifted)
    files |= folder_files(text, repo)
    return {"names": names, "how": how, "unknown": unknown, "files": sorted(files), "index": index["commit"]}


# ── self-test ──────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    root = Path(tempfile.mkdtemp(prefix="member-index-test-"))
    for f in ("src/A/Binder.cs", "src/A/Binder.Oo.cs", "src/A/Use.cs", "src/B/Emit.cs", "tests/T.cs", "src/A/Sym.cs"):
        (root / f).parent.mkdir(parents=True, exist_ok=True)
        (root / f).write_text("// " + f, encoding="utf-8")
    idx = {"commit": "c0", "blobs": {}, "files": ["src/A/Binder.cs", "src/A/Binder.Oo.cs", "src/A/Use.cs",
                                                    "src/B/Emit.cs", "tests/T.cs", "src/A/Sym.cs", "src/A/Gone.cs"],
           "uses": {"N.Binder": [0, 1, 2, 3, 4], "N.Binder.Pending": [1, 2], "N.Binder.Inner": [3],
                    "N.Sym.Flag": [3, 6], "N.Binder.Inner.X": [4]},
           "types": {"N.Binder": [0, 1], "N.Binder.Inner": [0], "N.Sym": [5]},
           "declared": {"N.Binder.Pending": [1], "N.Sym.Unused": [5], "N.Sym.Flag": [5], "N.Sym.Go(System.Int32)": [5],
                        "N.Sym.Go(System.String)": [5]}}
    idx["uses"]["N.Sym.Go(System.String)"] = [2]
    with_simple(idx)
    checks = []

    def check(label, got, want):
        checks.append(label)
        assert got == want, f"{label}: {got!r} != {want!r}"

    # a member: its uses plus its type's declaring files
    check("member", resolve(idx, ["N.Binder.Pending"])[0], {"src/A/Binder.cs", "src/A/Binder.Oo.cs", "src/A/Use.cs"})
    # a type: its uses, its nested types' uses and its declaring files
    check("type", resolve(idx, ["N.Binder"])[0], {"src/A/Binder.cs", "src/A/Binder.Oo.cs", "src/A/Use.cs",
                                                  "src/B/Emit.cs", "tests/T.cs"})
    # a declared member no code uses is known (a Delete note names those): its declaring file only
    check("unused member", resolve(idx, ["N.Sym.Unused"]), ({"src/A/Sym.cs"}, []))
    # an overload named exactly is that overload; a bare name is every overload
    check("one overload", resolve(idx, ["N.Sym.Go(System.Int32)"])[0], {"src/A/Sym.cs"})
    check("every overload", resolve(idx, ["N.Sym.Go"])[0], {"src/A/Sym.cs", "src/A/Use.cs"})
    # an unknown name is reported, never silently dropped
    check("unknown", resolve(idx, ["N.Binder.Typo"]), (set(), ["N.Binder.Typo"]))
    # the explicit line and documentation ids win over the fallback
    text = "**Moves or changes:** `N.Binder.Pending`, `N.Sym.Flag`\n- `M:N.Binder.Inner.X(System.Int32)~N.T @ src/A/Binder.cs:1`"
    check("declared", note_names(text, idx), (["N.Binder.Inner.X(System.Int32)", "N.Binder.Pending", "N.Sym.Flag"],
                                              "declared"))
    # the fallback: types declared in the site files and folders
    check("fallback-file", note_names("- `src/A/Binder.cs:12`", idx), (["N.Binder", "N.Binder.Inner"],
                                                                     "types declared in its sites"))
    check("fallback-folder", note_names("- folder `src/A`", idx)[0], ["N.Binder", "N.Binder.Inner", "N.Sym"])
    # drift: a file changed since the index that names a member is included; a deleted file drops out
    files, unknown = callers(idx, ["N.Sym.Flag"], root, drifted={"src/A/Use.cs": "x.Flag = 1;", "src/B/New.cs": "Flagged"})
    check("drift", (files, unknown), ({"src/A/Sym.cs", "src/B/Emit.cs", "src/A/Use.cs"}, []))
    # emitted text (K1 (2)): an emitter writing a runtime type's or member's name as C# text is a caller of it
    idx["emitted"] = {"N.Binder.Inner": [3], "N.Sym.Flag": [4]}
    check("emitted-type", "src/B/Emit.cs" in resolve(idx, ["N.Binder.Inner"])[0], True)
    check("emitted-nested", "src/B/Emit.cs" in resolve(idx, ["N.Binder"])[0], True)
    check("emitted-member", "tests/T.cs" in resolve(idx, ["N.Sym.Flag"])[0], True)
    # the target paragraph (K1 (1)): a member of a subject type the target names joins the hand-listed line
    text = ("- `src/A/Sym.cs`" + chr(10) + "**This note's target (§8.3), step 1 of 2:** the scoped Flag moves; Unused "
            "stays; Pending is another type's" + chr(10) + "**Moves or changes:** `N.Sym.Go`")
    check("target", note_names(text, idx), (["N.Sym.Flag", "N.Sym.Go", "N.Sym.Unused"], "declared and its target"))
    # folder sites expand on the current tree (K1 (3)): a file added under a site folder after the index joins
    (root / "src/A/Added.cs").write_text("// new", encoding="utf-8")
    subprocess.run(["git", "init", "-q"], cwd=root, check=True)
    subprocess.run(["git", "add", "-A"], cwd=root, check=True)
    check("folder", "src/A/Added.cs" in folder_files("- folder `src/A`", root), True)
    # currency (K1 (3)): the planner's index is the one whose treeKey is this tree's, else regenerated, else refused
    subprocess.run(["git", "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "c0"], cwd=root, check=True)
    key = tree_key(root)
    check("key-stable", key == tree_key(root) and key is not None, True)
    cache = root / "cache"

    def recorder(repo, out_dir):
        out_dir.mkdir(parents=True, exist_ok=True)
        out = out_dir / f"{tree_key(repo)}{SUFFIX}"
        out.write_bytes(gzip.compress(json.dumps(dict(idx, _simple=None, schema=SCHEMA, treeKey=tree_key(repo),
                                                      commitDate="x")).encode("utf-8")))
        return out
    try:
        current(root, cache, regenerate=False)
        check("stale-refused", "no refusal", "refusal")
    except StaleIndex:
        check("stale-refused", True, True)
    check("regenerated", current(root, cache, regenerate=True, recorder=recorder)["treeKey"], key)
    check("cached", current(root, cache, regenerate=False)["treeKey"], key)
    (root / "src/A/Added.cs").write_text("// changed", encoding="utf-8")
    check("dirty-no-key", tree_key(root), None)
    subprocess.run(["git", "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qam", "c1"], cwd=root, check=True)
    try:
        current(root, cache, regenerate=False)
        check("landing-makes-stale", "no refusal", "refusal")
    except StaleIndex:
        check("landing-makes-stale", True, True)
    check("docid", name_of_doc_id("N.T.M``1(System.Int32)~N.R"), "N.T.M``1(System.Int32)")
    check("simple", simple("N.T`1.M``1(System.Int32)"), "N.T.M")
    print(f"=== MEMBER INDEX SELF-TEST: PASS ({len(checks)} checks: {', '.join(checks)}) ===")
    return 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--record", action="store_true")
    g.add_argument("--note", metavar="ID")
    g.add_argument("--self-test", action="store_true")
    g.add_argument("--current", action="store_true", help="find or regenerate the index of this tree (--cache)")
    ap.add_argument("--cache", type=Path, help="with --current: where a regenerated index goes")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    if a.record:
        print(f"=== MEMBER INDEX: RECORDED {record().relative_to(REPO).as_posix()} ===")
        return 0
    if a.current:
        idx = current(REPO, a.cache, regenerate=a.cache is not None)
        print(f"=== MEMBER INDEX: CURRENT (commit {idx['commit'][:12]}, treeKey {idx['treeKey'][:12]}) ===")
        return 0
    text = (REPO / "kb" / "Work" / f"{a.note}.md").read_text(encoding="utf-8")
    fs = note_file_set(text)
    print(json.dumps(fs, indent=1, ensure_ascii=False))
    return 1 if fs["unknown"] else 0


if __name__ == "__main__":
    sys.exit(main())
