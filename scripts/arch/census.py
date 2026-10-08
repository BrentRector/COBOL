#!/usr/bin/env python3
"""census.py — the architecture review's R0 census (kb/Work PB2115; docs/rearchitecture/DESIGN-architecture-review.md §3 R0).

Opinions come after measurements. This is the measuring instrument: it records, at one commit, what the product code
IS — so R1 designs the target from numbers, and every R3 wave and Delete wave (kb/Work PB2119) starts from a measured
site instead of a remembered one.

What it measures (the facts come from a Roslyn semantic model, never from grep):
  * per type, every partial included: lines, members, fields, nested types, fan-in, fan-out, and whether each
    partial's folder agrees with its namespace;
  * per project, the dependency graph (declared project references), and the namespace-to-namespace graph measured
    from the code's actual uses;
  * type-2 clone families (the roslyn-analysis skill's detector, run inside the host over the same documents);
  * reachability: every authored member's reach — used, test-only, unreferenced, or kept by a STATED exclusion rule
    (the record carries each rule's text); every unreferenced answer is confirmed by a second, independent query;
  * dead artifacts beyond code: scripts, docs, repository-root files, test scaffolds and drift-test literals with no
    caller or reader, each measured by a caller query over every tracked file.

How:
  1. a DETACHED worktree at the commit (default HEAD) — a record describes exactly one tree, never a dirty one;
  2. `dotnet build Cobol.Net.sln -c Debug` there (the population check reads the BUILT assemblies);
  3. the Roslyn host `tools/ArchCensus` (built from THIS checkout, so an older commit can be measured) loads the
     solution through MSBuildWorkspace and writes the raw facts; it REFUSES a solution whose projects the scope does
     not partition, a workspace load failure, and a compilation with errors;
  4. THE POPULATION CHECK: for every census project, the types the census saw must equal the type definitions of the
     built assembly (read from its metadata by a reader independent of the workspace) — a skipped project, document
     or source generator is a FAILED run, never a smaller census;
  5. the dead-artifact queries, then the record `docs/rearchitecture/evidence/arch-census/<sha>.json` (numbers and
     names, no source text) and its findings `<sha>.findings.json` (one entry per god-class candidate, clone family,
     unreachable member family, dead artifact and folder/namespace disagreement, each with its site, the rule it
     breaks, a scenario, the proposed target and the wave kind). ⛔ The findings are filed as kb/Work notes by a
     clerk with orchestrator-allocated ids; this script never writes the register (CLAUDE.md rule 8).

Usage:
    python scripts/arch/census.py                    # HEAD
    python scripts/arch/census.py --commit <sha>
    python scripts/arch/census.py --self-test        # every policy arm on planted inputs (ArchCensusDriftTests)

The last line printed is the verdict: `=== ARCH CENSUS: RECORDED <path> … ===` or `=== ARCH CENSUS: FAILED … ===`.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SCHEMA = 1
RECORD_DIR = Path("docs/rearchitecture/evidence/arch-census")
# The frozen evidence tree (owner decision kb/Work/PB785; its own README states the directory rule): a record there
# is written once, when its experiment or measurement ends, and kept as that evidence; a later result supersedes it
# with a `superseded_by` marker, never a deletion. Its reader is the decision or report it serves, never a caller, so
# the caller query cannot measure a record dead and the census does not judge one (kb/Work PB2231, PB2232). The census
# records themselves live inside it.
EVIDENCE_DIR = Path("docs/rearchitecture/evidence")
HOST = Path("tools/ArchCensus")
# The record lives in the frozen evidence directory and keeps its convention (docs/DOC_INDEX.md; kb/Work/PB785).
FROZEN_BANNER = (
    "⛔ FROZEN EVIDENCE — the architecture census of ONE commit, recorded by scripts/arch/census.py and never edited "
    "to stay current (owner decision, kb/Work/PB785): a newer measurement is a new census run and a new file named by "
    "its commit. Findings are actioned through kb/Work/ notes, never by editing this record. A claim the tree has "
    "since REFUTED carries a `superseded_by` object — note · devlog · date · why — as the FIRST key of the object "
    "holding it. ⚠ Every path:line coordinate is a coordinate AS OF the measured commit — resolve the NAME.")

# ── THE SCOPE RULE ─────────────────────────────────────────────────────────────────────────────────────────────
# Every solution project is exactly one of these, or the run is refused (the host checks the partition too):
#   census   — a product project: under src/, named Cobol.Net.*;
#   reader   — a test or benchmark project under tests/, named Cobol.Net.*: its uses count as test uses, its
#              scaffolds and drift tests are measured, its own size is not;
#   excluded — the legacy engine (CobolSharp.*), being deleted (kb/Work R69 §1; the Delete program PB2119):
#              nothing in the census depends on it, so leaving it out changes no number about the product.
CENSUS_PREFIX = "Cobol.Net."
LEGACY_PREFIX = "CobolSharp."
# The assembly generated programs call: CodeGen/AssemblyPackager.cs copies Cobol.Net.Runtime.dll beside every compiled
# program, and the emitter writes calls to it as TEXT, so its members are reached by names inside string literals.
EMITTED_SURFACE = ["Cobol.Net.Runtime"]

# ── THE GOD-CLASS AND WAVE-KIND POLICY ─────────────────────────────────────────────────────────────────────────
GOD_CLASS_LINES = 800          # DESIGN-architecture-review §3 R0: "any type over ~800 lines across its partials"
DATA_TABLE_SHARE = 0.6         # a candidate whose members are mostly fields is a data table written as code: data-ize
RESPONSIBILITY_MIN_METHODS = 3  # a method-name subject names a responsibility when at least this many methods share it

# ── THE DEAD-ARTIFACT POLICY ───────────────────────────────────────────────────────────────────────────────────
# A reader is any tracked text file but the artifact itself. Two kinds of mention are NOT a caller or a reader:
HISTORY = {"DEVLOG.md"}                        # the history narrative (CLAUDE.md rule 6)
# Census records list every artifact they measure. A tracked path is always `/`-separated, so the prefix is the POSIX
# form: `str(RECORD_DIR)` is backslashed on Windows, matched nothing, and counted a record as a LIVE reader of every
# artifact it found dead (kb/Work PB2234's sweep).
GENERATED_READERS = (RECORD_DIR.as_posix() + "/",)
REGISTER = "kb/"                               # a kb/Work mention counts only while its note is open
CLOSED_STATUSES = {"landed", "retired", "discharged", "closed", "superseded", "duplicate"}
# Files read by a tool or a person BY CONVENTION, never by name (git, MSBuild, dotnet, GitHub, the harness):
CONVENTIONAL = {"README.md", "LICENSE", "LICENSE.md", "LICENSE.txt", "__init__.py", ".gitignore", ".gitattributes",
                ".gitmodules", "CLAUDE.md", "AGENTS.md", "CONTRIBUTING.md", "SECURITY.md", "CODE_OF_CONDUCT.md",
                "CLA.md", "global.json", "nuget.config", "Directory.Build.props", "Directory.Packages.props",
                "Directory.Build.targets", "Cobol.Net.sln", "PROMPT.md", "DEVLOG.md", ".agent-fleet.json",
                "conftest.py"}
# ...and any file whose name, or whose folder's name, says it is a notice for people (a README, a LICENSE, a NOTICE).
NOTICE = re.compile(r"(?i)readme|license|notice")
ARTIFACT_CLASSES = {"script": ("scripts/", "tools/"), "doc": ("docs/",)}
SUBMODULES = ("tools/claude-skills/", "specs-private")
# A file inside a C# project's folder is compiled through its project (MSBuild globs it), never named by a caller: the
# PROJECT is the artifact, and a caller names it by its file or its folder (`dotnet build tools/impact/X`).
PROJECT_MEMBERS = (".cs", ".targets", ".props", ".json", ".resx")
# A path-like token; `$`, `{`, `}` and `%` keep a COMPOSED name (`units/$unit.md`) whole, so it reads as a template.
PATHLIKE = re.compile(r"[A-Za-z0-9_.\-/\\${}%]+")
TEMPLATE_MARK = re.compile(r"[${%]")
IMPORT = re.compile(r"^\s*(?:from\s+([\w.]+)\s+import|import\s+([\w.]+(?:\s*,\s*[\w.]+)*))", re.MULTILINE)
LITERAL_FILE = re.compile(r"^[\w.\-]+\.(?:cs|py|ps1|sh|md|json|g4|csproj|props|targets|yml|yaml|txt|cbl|cob|cpy|js)$")
LITERAL_PATH = re.compile(r"^[\w.\-]+(?:/[\w.\-]+)+/?$")


class CensusFailure(Exception):
    """A run that cannot produce a trustworthy record: the script prints FAILED and writes nothing."""


# ── the scope ──────────────────────────────────────────────────────────────────────────────────────────────────

SLN_PROJECT = re.compile(r'^Project\("\{(?P<kind>[0-9A-F-]+)\}"\)\s*=\s*"(?P<name>[^"]+)",\s*"(?P<path>[^"]+)"',
                         re.MULTILINE)
SLN_FOLDER_KIND = "2150E333-8FDC-42A3-9474-1A3956D46DE8"


def solution_projects(sln_text: str) -> list[tuple[str, str]]:
    """(name, forward-slashed path) of every project the solution builds — solution folders are not projects."""
    return [(m["name"], m["path"].replace("\\", "/")) for m in SLN_PROJECT.finditer(sln_text)
            if m["kind"].upper() != SLN_FOLDER_KIND]


def scope_of(projects: list[tuple[str, str]]) -> dict[str, list[str]]:
    """Partition the solution's projects by THE SCOPE RULE, or raise naming the ones it does not classify."""
    scope: dict[str, list[str]] = {"census": [], "readers": [], "excluded": []}
    unclassified = []
    for name, path in projects:
        if name.startswith(LEGACY_PREFIX):
            scope["excluded"].append(name)
        elif name.startswith(CENSUS_PREFIX) and path.startswith("src/"):
            scope["census"].append(name)
        elif name.startswith(CENSUS_PREFIX) and path.startswith("tests/"):
            scope["readers"].append(name)
        else:
            unclassified.append(f"{name} ({path})")
    if unclassified:
        raise CensusFailure("the scope rule does not classify " + ", ".join(unclassified)
                            + " — extend THE SCOPE RULE in scripts/arch/census.py deliberately")
    if not scope["census"]:
        raise CensusFailure("the scope rule found no census project")
    missing_surface = [s for s in EMITTED_SURFACE if s not in scope["census"]]
    if missing_surface:
        raise CensusFailure(f"the emitted surface {missing_surface} is not a census project")
    return {k: sorted(v) for k, v in scope.items()} | {"emittedSurface": list(EMITTED_SURFACE)}


# ── the population check ───────────────────────────────────────────────────────────────────────────────────────

def population_check(projects: list[dict]) -> list[dict]:
    """Per census project, the census's types against the built assembly's. Raises on ANY difference."""
    rows, problems = [], []
    for p in projects:
        if p["role"] != "census":
            continue
        source, compiled = set(p["sourceTypes"] or []), set(p["compiledTypes"] or [])
        if not compiled:
            problems.append(f"{p['name']}: the built assembly has no authored type (was it built?)")
        missing, extra = sorted(compiled - source), sorted(source - compiled)
        if missing:
            problems.append(f"{p['name']}: {len(missing)} compiled type(s) the census did not see: {missing[:10]}")
        if extra:
            problems.append(f"{p['name']}: {len(extra)} census type(s) the assembly does not define: {extra[:10]}")
        rows.append({"project": p["name"], "census": len(source), "compiled": len(compiled)})
    if problems:
        raise CensusFailure("THE POPULATION CHECK failed:\n  " + "\n  ".join(problems))
    return rows


# ── god-class responsibilities ─────────────────────────────────────────────────────────────────────────────────

WORD = re.compile(r"[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z][a-z0-9]*")


def responsibilities(type_name: str, partial_files: list[str], method_names: list[str],
                     fan_out: dict[str, int]) -> dict[str, list]:
    """Name a type's concerns from what it declares, by stated rules (no judgment):
      * concerns — each partial file's suffix after the type name (DataBinder.Reports.cs → Reports);
      * subjects — the PascalCase word after each method's leading verb (BindReportGroup → Report), kept when at
        least RESPONSIBILITY_MIN_METHODS methods share it, most frequent first (at most 8);
      * depends  — the namespaces it uses most (at most 4)."""
    simple = type_name.split("+")[-1].split(".")[-1].split("`")[0]
    concerns = sorted({Path(f).name[len(simple) + 1:-len(".cs")] for f in partial_files
                       if Path(f).name.startswith(simple + ".") and Path(f).name.endswith(".cs")
                       and len(Path(f).name) > len(simple) + 4})
    subjects: Counter[str] = Counter()
    for name in method_names:
        words = WORD.findall(name)
        if len(words) >= 2:
            subjects[words[1]] += 1
    named = [f"{s} ({n})" for s, n in sorted(subjects.items(), key=lambda kv: (-kv[1], kv[0]))
             if n >= RESPONSIBILITY_MIN_METHODS][:8]
    depends = [ns for ns, _ in sorted(fan_out.items(), key=lambda kv: (-kv[1], kv[0]))[:4]]
    return {"concerns": [c for c in concerns if c], "subjects": named, "depends": depends}


# ── dead artifacts ─────────────────────────────────────────────────────────────────────────────────────────────

@dataclass
class Corpus:
    """Every tracked text file's path-like tokens and Python imports, indexed once (a caller query per artifact is a
    dictionary lookup, not a scan of the tree)."""
    files: list[str]
    by_basename: dict[str, set[str]]
    by_qualified: dict[str, set[str]]
    by_module: dict[str, set[str]]
    by_template: dict[str, set[str]]
    by_phrase: dict[str, set[str]]
    open_notes: set[str]


def note_is_open(text: str) -> bool:
    if not text.startswith("---"):
        return True
    end = text.find("\n---", 3)
    m = re.search(r"^status:\s*\"?([A-Za-z-]+)", text[: end if end > 0 else len(text)], re.MULTILINE)
    return not (m and m.group(1).lower() in CLOSED_STATUSES)


def build_corpus(root: Path, files: list[str], phrases: set[str] = frozenset()) -> Corpus:
    """Index every tracked text file once. A token is indexed by its last segment and by its last two segments;
    a COMPOSED token (`units/$unit.md`) by `<folder>/*<extension>`; a Python import by each module name; and each
    of <phrases> (artifact names a path token cannot hold, such as a name with a space) by plain containment."""
    by_basename: dict[str, set[str]] = defaultdict(set)
    by_qualified: dict[str, set[str]] = defaultdict(set)
    by_module: dict[str, set[str]] = defaultdict(set)
    by_template: dict[str, set[str]] = defaultdict(set)
    by_phrase: dict[str, set[str]] = defaultdict(set)
    open_notes: set[str] = set()
    for rel in files:
        path = root / rel
        if not path.is_file():
            continue   # a tracked submodule or symlink entry: not a file a caller can be written in
        try:
            raw = path.read_bytes()
        except OSError as e:
            # A reader the query cannot read would make everything it names look dead: refuse instead.
            raise CensusFailure(f"cannot read the tracked file {rel}: {e}") from e
        if b"\0" in raw[:8192]:
            continue   # binary
        text = raw.decode("utf-8", errors="replace")
        if rel.startswith(REGISTER) and note_is_open(text):
            open_notes.add(rel)
        for token in PATHLIKE.findall(text):
            parts = [p for p in re.split(r"[/\\]", token) if p]
            if not parts:
                continue
            last = parts[-1]
            if TEMPLATE_MARK.search(last) and len(parts) >= 2 and "." in last:
                by_template[f"{parts[-2]}/*{Path(last).suffix}"].add(rel)
            by_basename[last].add(rel)
            if len(parts) >= 2:
                by_qualified[f"{parts[-2]}/{last}"].add(rel)
        if rel.endswith(".py"):
            for m in IMPORT.finditer(text):
                for module in (m.group(1) or m.group(2) or "").split(","):
                    for piece in module.strip().split("."):
                        if piece:
                            by_module[piece].add(rel)
        for phrase in phrases:
            if phrase in text:
                by_phrase[phrase].add(rel)
    return Corpus(files, by_basename, by_qualified, by_module, by_template, by_phrase, open_notes)


def classify_reader(rel: str, corpus: Corpus) -> str:
    if rel in HISTORY:
        return "history"
    if rel.startswith(GENERATED_READERS):
        return "generated"
    if rel.startswith(REGISTER):
        return "register-open" if rel in corpus.open_notes else "register-closed"
    return "live"


def readers_of(artifact: str, corpus: Corpus, ambiguous: set[str]) -> set[str]:
    """The files that name the artifact: its basename, or — when two tracked artifacts share the basename — its
    parent-folder-qualified name; a template composing names in its folder with its extension; a Python module also
    by import; a C# project also by its folder."""
    p = Path(artifact)
    name, parent = p.name, p.parent.name
    found = set(corpus.by_qualified.get(f"{parent}/{name}", set()))
    if name not in ambiguous:
        found |= corpus.by_basename.get(name, set())
    found |= corpus.by_template.get(f"{parent}/*{p.suffix}", set())
    found |= corpus.by_phrase.get(name, set())
    if artifact.endswith(".py"):
        found |= corpus.by_module.get(p.stem, set())
    if artifact.endswith(".csproj"):
        found |= corpus.by_qualified.get(f"{p.parent.parent.name}/{parent}", set())
    found.discard(artifact)
    return found


def artifact_reach(artifact: str, corpus: Corpus, ambiguous: set[str]) -> str:
    """conventional · live · register-open · history-only (only DEVLOG, closed notes or census records name it) ·
    unreferenced."""
    p = Path(artifact)
    if p.name in CONVENTIONAL or NOTICE.search(p.name) or any(NOTICE.search(d) for d in p.parent.parts):
        return "conventional"
    kinds = {classify_reader(r, corpus) for r in readers_of(artifact, corpus, ambiguous)}
    if "live" in kinds:
        return "live"
    if "register-open" in kinds:
        return "register-open"
    return "history-only" if kinds else "unreferenced"


def artifact_class(rel: str, projects: set[str]) -> str | None:
    """The artifact class of a tracked file, or None when the census does not judge it by a caller query."""
    if rel.startswith(SUBMODULES):
        return None
    if "/" not in rel:
        return "root-file"
    if rel.startswith(EVIDENCE_DIR.as_posix() + "/"):
        return None
    for cls, prefixes in ARTIFACT_CLASSES.items():
        if rel.startswith(prefixes):
            if cls == "doc":
                return cls if rel.endswith(".md") else None
            in_project = any(rel.startswith(d + "/") for d in projects)
            return None if in_project and rel.endswith(PROJECT_MEMBERS) else cls
    return None


def dead_artifacts(root: Path, tracked: list[str], on_disk: set[str], raw: dict) -> list[dict]:
    projects = {str(Path(f).parent).replace("\\", "/") for f in tracked if f.endswith(".csproj")}
    phrases = {Path(f).name for f in tracked if " " in Path(f).name}
    corpus = build_corpus(root, tracked, phrases)
    names = Counter(Path(f).name for f in tracked)
    ambiguous = {n for n, c in names.items() if c > 1}
    out = []
    for rel in tracked:
        cls = artifact_class(rel, projects)
        if cls is None:
            continue
        reach = artifact_reach(rel, corpus, ambiguous)
        if reach in ("unreferenced", "history-only"):
            out.append({"class": cls, "path": rel, "reach": reach})
    for s in raw["testScaffolds"]:
        out.append({"class": "test-scaffold", "path": s["file"], "line": s["line"], "type": s["id"],
                    "projects": s["projects"], "lines": s["lines"], "reach": "unreferenced"})
    for lit in dangling_literals(raw["driftLiterals"], tracked, on_disk):
        out.append({"class": "drift-literal", "path": lit["file"], "line": lit["line"], "literal": lit["text"],
                    "reach": "dangling"})
    return out


def dangling_literals(literals: list[dict], tracked: list[str], on_disk: set[str]) -> list[dict]:
    """A drift-test string that claims to name something in the tree — a path built through TestRepo, or an entry
    of a static exemption/allow list shaped like a repository path or a file name — and names nothing that exists
    (tracked, or a build output on disk): the test pins nothing. Any other literal is the test's own fixture."""
    tops = {f.split("/", 1)[0] for f in tracked if "/" in f}
    known = set(tracked) | on_disk
    dirs = {str(Path(f).parent).replace("\\", "/") for f in known}
    dirs |= {"/".join(d.split("/")[:i]) for d in list(dirs) for i in range(1, d.count("/") + 1)}
    basenames = {Path(f).name for f in known}
    out, seen = [], set()
    for lit in literals:
        text = lit["text"].strip().rstrip("/")
        if lit["kind"] not in ("path", "list") or not text or "*" in text or "{" in text:
            continue
        if lit["kind"] == "path" or (LITERAL_PATH.match(text) and text.split("/", 1)[0] in tops):
            exists = text in known or text in dirs
        elif LITERAL_FILE.match(text):
            exists = text in basenames
        else:
            continue
        key = (lit["file"], text)
        if not exists and key not in seen:
            seen.add(key)
            out.append(lit)
    return out


# ── the record and the findings ────────────────────────────────────────────────────────────────────────────────

REACH_RULES_EXTRA = {
    "used": "authored or generated product code uses it outside its own body",
    "test-only": "only a test project uses it",
    "unreferenced": "nothing in the solution uses it — confirmed by SymbolFinder (cascading through overrides and "
                    "implementations; an XML-doc cref is not a use)",
}

TYPE_COLUMNS = ["id", "project", "kind", "lines", "members", "fields", "nested", "fanIn", "fanInTests", "fanOut",
                "partials", "folderMismatches", "generated", "reach"]


def type_row(t: dict) -> list:
    mismatches = sorted({p["file"] for p in t["partials"]
                         if p["folderNamespace"] is not None and p["folderNamespace"] != t["namespace"]})
    return [t["id"], t["project"], t["kind"], t["lines"], t["members"], t["fields"], t["nestedTypes"], t["fanIn"],
            t["fanInTests"], t["fanOut"], len(t["partials"]), mismatches, t["generated"], t["reach"]]


def god_classes(raw: dict) -> list[dict]:
    out = []
    for t in raw["types"]:
        if t["generated"] or t["lines"] < GOD_CLASS_LINES:
            continue
        files = sorted({p["file"] for p in t["partials"] if not p["generated"]})
        out.append({**t, "files": files,
                    "responsibilities": responsibilities(t["id"], files, t["methodNames"], t["fanOutNamespaces"])})
    return sorted(out, key=lambda t: (-t["lines"], t["id"]))


def findings(raw: dict, gods: list[dict], dead: list[dict]) -> list[dict]:
    out: list[dict] = []

    for t in gods:
        r = t["responsibilities"]
        data_table = t["members"] > 0 and t["fields"] / t["members"] >= DATA_TABLE_SHARE
        named = r["concerns"] + r["subjects"] or [f"{t['fields']} field definitions"]
        out.append({
            "kind": "god-class", "wave": "data-ize" if data_table else "extract",
            "site": {"type": t["id"], "files": t["files"]},
            "measure": {k: t[k] for k in ("lines", "members", "fields", "nestedTypes", "fanIn", "fanInTests", "fanOut")},
            "responsibilities": r,
            "rule": "single responsibility per type (DESIGN-architecture-review §5.1); a type over "
                    f"~{GOD_CLASS_LINES} lines across its partials is a god-class candidate (§3 R0)",
            "scenario": (f"{t['lines']} lines and {t['members']} members in one type that {t['fanIn']} product types "
                         f"depend on: a change to any one concern ({', '.join(named[:6])}) edits "
                         "and re-reviews all of them, and a rule added to one arm is not seen by the others"),
            "target": ("move the data into a declarative table loaded once, with the code keeping only the lookup"
                       if data_table else
                       "extract each named concern into its own type, its partial files becoming the first seams"),
        })

    for f in raw["clones"]:
        inst = f["instances"]
        out.append({
            "kind": "clone-family", "wave": "unify",
            "site": [f"{i['file']}:{i['start']}-{i['end']} ({i['member']})" for i in inst],
            "measure": {"copies": len(inst), "tokens": f["tokens"], "linesEach": inst[0]["end"] - inst[0]["start"] + 1},
            "rule": "one rule in one place; duplicate code is found by structure, not text (§5.2)",
            "scenario": (f"{len(inst)} bodies with the same token structure: a fix made to one copy leaves "
                         f"{len(inst) - 1} unfixed — the forgotten-arm defect shape"),
            "target": ("triage semantically (roslyn-analysis §1): if the bodies bind the same members, extract one "
                       "helper with a domain name; if they only share a shape, record why they stay apart"),
        })

    families: dict[tuple[str, str], list[dict]] = defaultdict(list)
    for m in raw["members"]:
        if m["reach"] in ("unreferenced", "test-only"):
            families[(m["reach"], m["family"] or m["type"])].append(m)
    for (reach, family), members in sorted(families.items()):
        lines = sum(m["lines"] for m in members)
        tested = reach == "test-only"
        out.append({
            "kind": "test-only-family" if tested else "unreachable-family", "wave": "delete",
            "site": {"family": family, "members": [f"{m['id']} @ {m['file']}:{m['line']}" for m in members]},
            "measure": {"members": len(members), "lines": lines},
            "rule": ("dead and obsolete code is removed (kb/Work R69 §3); reachability is measured, never deduced "
                     "(engineering-standards)"),
            "scenario": (f"{len(members)} member(s) ({lines} lines) that only tests use: the tests pin code no "
                         "product path runs, so they keep it alive and reviewed for nothing" if tested else
                         f"{len(members)} member(s) ({lines} lines) nothing in the solution uses (walk + SymbolFinder): "
                         "they compile, are read and are maintained for nothing"),
            "target": ("delete them and the tests that exercise only them, or name the product path that should use "
                       "them" if tested else "delete them with every caller-free dependency they keep alive"),
        })
    for t in raw["types"]:
        if t["reach"] in ("unreferenced", "test-only"):
            out.append({
                "kind": "unreachable-type" if t["reach"] == "unreferenced" else "test-only-type", "wave": "delete",
                "site": {"type": t["id"], "files": sorted({p["file"] for p in t["partials"]})},
                "measure": {"lines": t["lines"], "members": t["members"]},
                "rule": "dead and obsolete code is removed (kb/Work R69 §3); reachability is measured",
                "scenario": f"no type outside {t['id']} uses it ({t['reach']})",
                "target": "delete it, with its tests if they test only it",
            })

    for a in dead:
        where = a["path"] + (f":{a['line']}" if "line" in a else "")
        subject = a.get("type") or a.get("literal") or a["path"]
        out.append({
            "kind": "dead-artifact", "wave": "delete", "class": a["class"], "site": where,
            "rule": ("dead artifacts beyond code are removed (kb/Work R69 §3, PB2119): no caller or reader, measured by "
                     "a caller query per artifact"),
            "scenario": {
                "test-scaffold": f"test type {subject} has no test method and no user in any test assembly",
                "drift-literal": f"the drift test names `{subject}`, which exists nowhere in the tree: it pins nothing",
            }.get(a["class"], f"no live caller or reader names {Path(a['path']).name} "
                              f"({a['reach']}: " + ("only DEVLOG, closed notes or census records mention it)"
                                                    if a["reach"] == "history-only" else "no mention at all)")),
            "target": {"drift-literal": "correct the literal to the path it means, or drop the assertion that pins "
                                        "nothing"}.get(a["class"], "delete it (verify no out-of-band consumer first)"),
        })

    folders: dict[str, set[str]] = defaultdict(set)
    for t in raw["types"]:
        for p in t["partials"]:
            if p["folderNamespace"] is not None and p["folderNamespace"] != t["namespace"]:
                folders[str(Path(p["file"]).parent).replace("\\", "/")].add(f"{t['namespace']} != {p['folderNamespace']}")
    for folder, pairs in sorted(folders.items()):
        out.append({
            "kind": "folder-namespace", "wave": "move-rename", "site": folder,
            "measure": {"disagreements": sorted(pairs)},
            "rule": "folder equals namespace (§5.3)",
            "scenario": "a reader looking for a type by its namespace looks in the wrong folder",
            "target": "move the files or rename the namespace so the two agree",
        })
    for i, f in enumerate(out, 1):
        f["id"] = f"R0-{i:04d}"
    return out


def build_record(sha: str, date: str, scope: dict, raw: dict, population: list[dict], gods: list[dict],
                 dead: list[dict], counts: dict) -> dict:
    return {
        "_frozen": FROZEN_BANNER,
        "schema": SCHEMA,
        "commit": sha,
        "commitDate": date,
        "instrument": {"script": "scripts/arch/census.py", "host": str(HOST).replace("\\", "/"),
                       "design": "docs/rearchitecture/DESIGN-architecture-review.md §3 R0", "note": "kb/Work/PB2115"},
        "scope": scope,
        "rules": {
            "generated": "a document under an obj/ or Generated/ folder, a *.g.cs/*.g.i.cs/*.Designer.cs, one whose "
                         "first comment is <auto-generated>, or a source generator's output: measured for population, "
                         "never reported",
            "lines": "the declaration span of each authored partial (attributes in, leading comments out), summed; a "
                     "nested type's lines count in its container too",
            "members": "authored fields, methods, constructors, operators, properties, indexers and events (accessors "
                       "and nested types not counted)",
            "fan": "fanOut: distinct other census types the type's own code uses; fanIn: distinct product types using "
                   "it; fanInTests: distinct test types using it; a type and its nested types are one unit",
            "folderNamespace": "a partial agrees when its namespace is the project's root namespace plus its folders",
            "godClass": f"an authored type of at least {GOD_CLASS_LINES} lines; data-ize when fields are at least "
                        f"{DATA_TABLE_SHARE:.0%} of its members",
            "clones": "type-2: identical token-kind streams once identifiers and literals are canonicalized, over "
                      "whole bodies of at least minTokens tokens (roslyn-analysis detect-clones.cs, default mode)",
            "reach": {**REACH_RULES_EXTRA, **raw["reachRules"]},
            "deadArtifacts": {
                "classes": "script: tracked files under scripts/ and tools/ (submodules excluded); doc: tracked "
                           "docs/**/*.md (the frozen evidence tree docs/rearchitecture/evidence/ excluded: its "
                           "records are kept as evidence, kb/Work/PB785); root-file: tracked repository-root files; "
                           "test-scaffold: a test type with no test method and no user; drift-literal: a drift-test "
                           "string naming a path or file that exists nowhere",
                "query": "a caller is a mention of the basename (the parent-qualified name when two tracked files "
                         "share it) or a Python import, in any tracked text file but the artifact itself",
                "notCallers": "DEVLOG.md (history), closed kb/Work notes, and census records",
                "conventional": sorted(CONVENTIONAL),
            },
        },
        "population": population,
        "projects": [{k: p[k] for k in ("name", "path", "role", "assemblyName", "defaultNamespace",
                                        "projectReferences", "documents", "generatedDocuments")}
                     for p in raw["projects"]],
        "typeColumns": TYPE_COLUMNS,
        "types": [type_row(t) for t in raw["types"]],
        "namespaceEdges": [[e["from"], e["to"], e["pairs"]] for e in raw["namespaceEdges"]],
        "clones": {"minTokens": raw["cloneMinTokens"], "mode": "type-2", "families": len(raw["clones"]),
                   "duplicatedLines": duplicated_lines(raw["clones"])},
        "reachability": {
            "members": dict(sorted(Counter(m["reach"] for m in raw["members"]).items())),
            "types": dict(sorted(Counter(t["reach"] for t in raw["types"] if t["reach"]).items())),
            "walk": {"documents": raw["walk"]["documents"], "uses": raw["walk"]["referencesRecorded"],
                     "confirmedCandidates": raw["walk"]["candidates"], "walkMisses": raw["walk"]["walkMisses"]},
        },
        "deadArtifacts": dict(sorted(Counter(a["class"] for a in dead).items())),
        "godClasses": [{"type": g["id"], "lines": g["lines"], "members": g["members"], "fanIn": g["fanIn"],
                        "fanOut": g["fanOut"], "partials": len(g["files"])} for g in gods],
        "summary": counts,
    }


def duplicated_lines(clones: list[dict]) -> int:
    lines = set()
    for f in clones:
        for i in f["instances"][1:]:
            lines.update((i["file"], n) for n in range(i["start"], i["end"] + 1))
    return len(lines)


# ── running it ─────────────────────────────────────────────────────────────────────────────────────────────────

def git(*args: str, cwd: Path = REPO) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout.strip()


def low_priority() -> dict:
    return {"creationflags": subprocess.BELOW_NORMAL_PRIORITY_CLASS} if os.name == "nt" else {}


def run_logged(cmd: list[str], cwd: Path, log: Path) -> None:
    with open(log, "w", encoding="utf-8", errors="replace") as fh:
        rc = subprocess.run(cmd, cwd=cwd, stdout=fh, stderr=subprocess.STDOUT, **low_priority()).returncode
    if rc != 0:
        tail = log.read_text(encoding="utf-8", errors="replace").splitlines()[-15:]
        raise CensusFailure(f"`{' '.join(cmd)}` exited {rc} (log {log}):\n  " + "\n  ".join(tail))


def files_on_disk(root: Path) -> set[str]:
    skip = {".git", "bin", "obj", "TestResults", "node_modules"}
    out = set()
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in skip]
        rel = Path(dirpath).relative_to(root).as_posix()
        out.update(f if rel == "." else f"{rel}/{f}" for f in filenames)
    return out


def measure(commit: str, work: Path, out_dir: Path) -> Path:
    sha = git("rev-parse", commit)
    date = git("show", "-s", "--format=%cI", sha)
    wt = work / "tree"
    logs = work / "logs"
    logs.mkdir(parents=True, exist_ok=True)
    print(f"census: {sha[:12]} ({date}) in {wt}", flush=True)
    git("worktree", "add", "--detach", str(wt), sha)
    try:
        sln = (wt / "Cobol.Net.sln").read_text(encoding="utf-8-sig")
        scope = scope_of(solution_projects(sln))
        print(f"census: scope census={scope['census']} readers={scope['readers']} excluded={scope['excluded']}",
              flush=True)
        t0 = time.time()
        run_logged(["dotnet", "build", "Cobol.Net.sln", "-c", "Debug", "-v", "quiet"], wt, logs / "build.log")
        host = work / "host"
        run_logged(["dotnet", "build", str(REPO / HOST), "-c", "Release", "-o", str(host), "-v", "quiet"], REPO,
                   logs / "host-build.log")
        print(f"census: built in {time.time() - t0:.0f}s", flush=True)
        scope_file = work / "scope.json"
        scope_file.write_text(json.dumps(scope), encoding="utf-8")
        raw_file = work / "raw.json"
        t1 = time.time()
        run_logged(["dotnet", str(host / "ArchCensus.dll"), "--solution", str(wt / "Cobol.Net.sln"), "--scope",
                    str(scope_file), "--out", str(raw_file)], wt, logs / "host.log")
        print(f"census: measured in {time.time() - t1:.0f}s", flush=True)
        raw = json.loads(raw_file.read_text(encoding="utf-8"))
        population = population_check(raw["projects"])
        tracked = [f for f in git("ls-files", cwd=wt).splitlines() if f]
        dead = dead_artifacts(wt, tracked, files_on_disk(wt), raw)
    finally:
        removed = subprocess.run(["git", "worktree", "remove", "--force", str(wt)], cwd=REPO, capture_output=True,
                                 text=True)
        if removed.returncode != 0:
            print(f"census: ⚠ the measured worktree {wt} was not removed ({removed.stderr.strip()}); "
                  "remove it with `git worktree remove --force`", flush=True)

    gods = god_classes(raw)
    found = findings(raw, gods, dead)
    members = Counter(m["reach"] for m in raw["members"])
    counts = {
        "types": len(raw["types"]), "authoredTypes": sum(1 for t in raw["types"] if not t["generated"]),
        "godClassCandidates": len(gods), "cloneFamilies": len(raw["clones"]),
        "unreachableMembers": members["unreferenced"], "testOnlyMembers": members["test-only"],
        "unreachableTypes": sum(1 for t in raw["types"] if t["reach"] == "unreferenced"),
        "deadArtifacts": len(dead), "findings": len(found),
    }
    record = build_record(sha, date, scope, raw, population, gods, dead, counts)
    out_dir.mkdir(parents=True, exist_ok=True)
    path = out_dir / f"{sha}.json"
    path.write_text(json.dumps(record, indent=None, separators=(",", ":"), ensure_ascii=False) + "\n", encoding="utf-8")
    (out_dir / f"{sha}.findings.json").write_text(
        json.dumps({"_frozen": FROZEN_BANNER, "commit": sha, "schema": SCHEMA, "findings": found}, indent=1,
                   ensure_ascii=False) + "\n",
        encoding="utf-8")
    print_summary(gods, counts)
    return path


def print_summary(gods: list[dict], counts: dict) -> None:
    print("\ncensus: god-class candidates (top 15)")
    print(f"  {'type':<62} {'lines':>6} {'members':>7} {'fanIn':>5} {'fanOut':>6}")
    for g in gods[:15]:
        print(f"  {g['id']:<62} {g['lines']:>6} {g['members']:>7} {g['fanIn']:>5} {g['fanOut']:>6}")
    print("census: " + " · ".join(f"{k} {v}" for k, v in counts.items()))


# ── self-test ──────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, passed: bool, detail: str = "") -> None:
        results.append((name, bool(passed), detail))

    def failure_of(fn) -> str:
        try:
            fn()
        except CensusFailure as e:
            return str(e)
        return ""

    real = solution_projects((REPO / "Cobol.Net.sln").read_text(encoding="utf-8-sig"))
    scope = scope_of(real)
    arm("the scope rule partitions the real Cobol.Net.sln (no unclassified project, a census and a reader each)",
        bool(scope["census"]) and bool(scope["readers"])
        and sorted(scope["census"] + scope["readers"] + scope["excluded"]) == sorted(n for n, _ in real), str(scope))
    planted = real + [("Cobol.Net.Planted", "src/Cobol.Net.Planted/Cobol.Net.Planted.csproj")]
    arm("a NEW src/Cobol.Net.* project joins the census by the rule, with no edit here",
        "Cobol.Net.Planted" in scope_of(planted)["census"])
    why = failure_of(lambda: scope_of(real + [("Contoso.Tool", "tools/Contoso.Tool/Contoso.Tool.csproj")]))
    arm("a project the rule does not classify REFUSES the run, naming it", "Contoso.Tool" in why, why)
    folder = 'Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "src", "src", "{X}"\n'
    arm("a solution folder is not a project", solution_projects(folder) == [])

    ok = {"name": "P", "role": "census", "sourceTypes": ["A", "B"], "compiledTypes": ["A", "B"]}
    arm("equal populations pass", population_check([ok]) == [{"project": "P", "census": 2, "compiled": 2}])
    why = failure_of(lambda: population_check([{**ok, "sourceTypes": ["A"]}]))
    arm("a compiled type the census did not see FAILS the run, naming it", "did not see" in why and "'B'" in why, why)
    why = failure_of(lambda: population_check([{**ok, "compiledTypes": ["A"]}]))
    arm("a census type the assembly lacks FAILS the run", "does not define" in why, why)
    why = failure_of(lambda: population_check([{**ok, "sourceTypes": [], "compiledTypes": []}]))
    arm("an EMPTY built assembly FAILS the run (never a clean zero)", "no authored type" in why, why)

    r = responsibilities("CobolNet.Binding.DataBinder", ["src/x/DataBinder.cs", "src/x/DataBinder.Reports.cs"],
                         ["BindReportGroup", "BindReportLine", "EmitReportSum", "BindMove", "Validate"],
                         {"CobolNet.A": 3, "CobolNet.B": 9})
    arm("responsibilities name the partial concerns, the shared method subjects and the main dependencies",
        r == {"concerns": ["Reports"], "subjects": ["Report (3)"], "depends": ["CobolNet.B", "CobolNet.A"]}, str(r))

    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        files = {
            "scripts/live.py": "x", "scripts/hist.py": "x", "scripts/alone.sh": "echo scripts/alone.sh",
            "scripts/a/run.sh": "x", "scripts/b/run.sh": "x", "scripts/mod.py": "x", "scripts/planned.ps1": "x",
            "scripts/user.py": "import mod\nsubprocess.run(['python', 'scripts/live.py'])\nb/run.sh\n",
            "scripts/units/meter.md": "x", "scripts/units/other.txt": "x",
            "scripts/drive.ps1": "Get-Content (Join-Path $Here \"units/$unit.md\")\nalone2 drive.ps1\n",
            "scripts/with space.md": "x", "scripts/rr/rr-README.txt": "x", "scripts/rr/LICENSE/NOTES.txt": "x",
            "tools/Probe/Probe.csproj": "x", "tools/Probe/Program.cs": "x",
            "tools/runner.py": "subprocess.run(['dotnet', 'build', 'tools/Probe'])\nsee 'with space.md'\n",
            "DEVLOG.md": "we once ran hist.py and a/run.sh, drive.ps1, runner.py", "README.md": "x",
            "kb/Work/PB1.md": "---\nstatus: open\n---\nwill call planned.ps1\n",
            "kb/Work/PB2.md": "---\nstatus: landed\n---\nused hist.py\n",
        }
        for rel, text in files.items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(text, encoding="utf-8")
        tracked = sorted(files)
        projects = {"tools/Probe"}
        corpus = build_corpus(root, tracked, {"with space.md"})
        ambiguous = {n for n, c in Counter(Path(f).name for f in tracked).items() if c > 1}
        got = {f: artifact_reach(f, corpus, ambiguous) for f in tracked if artifact_class(f, projects) == "script"}
        arm("a script a live file names is live; by import too", got["scripts/live.py"] == "live"
            and got["scripts/mod.py"] == "live", str(got))
        arm("a script only DEVLOG and a closed note name is history-only", got["scripts/hist.py"] == "history-only",
            str(got))
        arm("a script no file but itself names is unreferenced", got["scripts/alone.sh"] == "unreferenced"
            and got["scripts/user.py"] == "unreferenced", str(got))
        arm("an OPEN note keeps a script alive as register-open", got["scripts/planned.ps1"] == "register-open",
            str(got))
        arm("a basename two artifacts share needs its parent-qualified name", got["scripts/b/run.sh"] == "live"
            and got["scripts/a/run.sh"] == "history-only", str(got))
        arm("a composed name (units/$unit.md) reaches its folder's files of that extension, and only those",
            got["scripts/units/meter.md"] == "live" and got["scripts/units/other.txt"] == "unreferenced", str(got))
        arm("a C# project is reached by its folder, and its sources through it",
            got["tools/Probe/Probe.csproj"] == "live" and "tools/Probe/Program.cs" not in got, str(got))
        arm("a name a path token cannot hold (a space) is found by containment",
            got["scripts/with space.md"] == "live", str(got))
        arm("a conventional file, a README and a LICENSE folder's files are never dead",
            artifact_reach("README.md", corpus, ambiguous) == "conventional"
            and got["scripts/rr/rr-README.txt"] == "conventional"
            and got["scripts/rr/LICENSE/NOTES.txt"] == "conventional", str(got))

    # The tracked-path prefixes are `/`-separated on every OS (a backslashed prefix matched nothing on Windows).
    record = RECORD_DIR.as_posix() + "/a02b.findings.json"
    arm("a census record is a generated reader, never a live caller, on every OS",
        classify_reader(record, build_corpus(Path("."), [])) == "generated", record)
    evidence = EVIDENCE_DIR.as_posix() + "/fleet-optimization/2026-10-03-spike.md"
    arm("a frozen evidence record is never judged by a caller query; any other doc is",
        artifact_class(evidence, set()) is None and artifact_class(RECORD_DIR.as_posix() + "/x.md", set()) is None
        and artifact_class("docs/rearchitecture/plan.md", set()) == "doc", evidence)

    def lit(line: int, text: str, kind: str) -> dict:
        return {"file": "t/XDriftTests.cs", "line": line, "text": text, "kind": kind}

    lits = [lit(1, "scripts/live.py", "path"), lit(2, "scripts/gone.py", "path"), lit(3, "Gone.cs", "list"),
            lit(4, "Here.cs", "list"), lit(5, "not a path at all", "list"), lit(6, "src/Gen/Parser.cs", "list"),
            lit(7, "scripts", "path"), lit(8, "prog.cob", "other"), lit(9, "src/Missing.cs", "other")]
    dang = [x["line"] for x in dangling_literals(lits, ["scripts/live.py", "src/A/Here.cs"], {"src/Gen/Parser.cs"})]
    arm("a drift literal naming a missing path or file is dangling; an existing one, a directory, a build output on "
        "disk, prose, or the test's own fixture is not", dang == [2, 3], str(dang))

    for name, passed, detail in results:
        print(f"  {'PASS' if passed else 'FAIL'}  {name}" + ("" if passed or not detail else f"\n        {detail}"))
    failed = sum(1 for _, passed, _ in results if not passed)
    print(f"=== arch census SELF-TEST: {'PASS' if failed == 0 else f'FAIL ({failed})'} ({len(results)} arms) ===")
    return 0 if failed == 0 else 1


def main(argv: list[str]) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass
    if argv[:1] == ["--self-test"]:
        return self_test()
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--commit", default="HEAD", help="the commit to measure (default HEAD)")
    ap.add_argument("--out-dir", default=str(REPO / RECORD_DIR), help="where the record and findings are written")
    ap.add_argument("--work", help="a work directory to keep (default: a temporary one, removed after)")
    args = ap.parse_args(argv)
    work = Path(args.work) if args.work else Path(tempfile.mkdtemp(prefix="arch-census-"))
    try:
        path = measure(args.commit, work, Path(args.out_dir))
    except (CensusFailure, subprocess.CalledProcessError) as e:
        print(f"=== ARCH CENSUS: FAILED — {e} ===")
        return 1
    finally:
        if not args.work:
            shutil.rmtree(work, ignore_errors=True)
    print(f"=== ARCH CENSUS: RECORDED {path.relative_to(REPO) if path.is_relative_to(REPO) else path} ===")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
