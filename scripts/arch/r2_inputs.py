#!/usr/bin/env python3
"""r2_inputs.py — the R2 review fleet's mechanical inputs, computed ONCE per pinned tree, and one self-contained input
file per shard; plus the Workflow args of a batch (kb/Work PB2559; docs/rearchitecture/DESIGN-architecture-review.md
§3 R2, §5.4; the R2 adversarial review's B4, N2, N3).

WHY. The review skill's first step is "run the mechanical checks — they are free", and design §5.4 puts the semgrep
invariants, the analyzers at `latest-all` and the drift-rule query before any judgment. The first R2 brief told every
reviewer to "use the census and the LSP": eighty agents would each have decided whether to scan, and a clone family
spanning two subsystems would have been reported twice under two wave kinds, or by nobody. Here the checks run once
on the pin, every reviewer gets ITS facts in one input file (agent-fleet §2: one agent, one self-contained input), and
duplication's whole-codebase clone report is one input for one pass.

WHAT, in `<out>/` (each step cached in `<out>/mech/<step>.json` with the KEY it was made for — the pin, the step's
STEP_VERSION and what else it depends on — and reused only for the same key, so a re-run resumes):
  mech/semgrep.json     `scripts/semgrep/invariants.yml` over the pin's src/ (the six §1.2 invariants), hits by file;
  mech/analyzers-<project>.json   `dotnet build <project> -p:AnalysisLevel=latest-all` per C# project, in dependency
                        order, in one detached worktree of the pin (`mech/tree`) whose `.globalconfig` raises the
                        §5.5 IDE/SYSLIB rules (MODERN_RULES) to warning, every analyzer warning by file (the
                        modern-C# dimension's ONLY evidence: a modern-C# finding without an analyzer rule id is a
                        suggestion, N2);
  mech/drift.json       `scripts/spec/drift_rules.py <files>`: the specific drift rules that govern each file;
  mech/notes.json       every open kb/Work note and the domain files it names (a finding already tracked is
                        reported as "already PBnnnn", never re-filed);
  inputs/in-<shard>.json        the shard: its files and lines, and the facts above filtered to its files, plus the
                        census rows for it (god classes, unreachable and test-only families, folder/namespace
                        disagreements, clone families touching it, its namespaces' measured edges);
  inputs/in-duplication.json    the whole-codebase clone report (every census clone family, each copy mapped to its
                        shard) for the ONE duplication pass;
  inputs/perf.json      the R0 performance baseline record and the command that re-measures against it: a
                        performance claim without a measurement on the pin is a lead, never a finding (N3).

`--batch <label>` writes a batch's args to `<out>/batch-<label>/batch-args.json` (the shards in the order given, the
dimensions, the stop files, the pin); the batch's checkpoint files go to that directory, `r2_collect.py --out
<out>/batch-<label> --launch` plans each launch from them, and `r2_collect.py --out <out>/batch-<label>` decides
them. A batch is SIZED first (kb/Work PB2707; `r2_cost.py`): one that cannot finish in the session's and the week's
room left is refused, nothing is written, and the estimate is printed with the batches to write instead (`<label>a`,
`<label>b`, ... one command each: the first fits the room left now, each later one a fresh session window).

Usage:
    python scripts/arch/r2_inputs.py --pin <built tree at the pin> --out <dir>            # shards + mechanical + inputs
    python scripts/arch/r2_inputs.py --out <dir> --batch 1 --shards a,b,c [--duplication] [--width 8]
                                     [--borrow-days N] [--session-reserve P]
    python scripts/arch/r2_inputs.py --self-test

The last line printed is the verdict: `=== R2 INPUTS: ... ===`.
"""
from __future__ import annotations

import argparse
import gzip
import json
import os
import re
import subprocess
import sys
import tempfile
import time
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
import r2_collect  # noqa: E402  (the launch plan a batch is sized by)
import r2_cost  # noqa: E402
import r2_subsystems  # noqa: E402
import work  # noqa: E402

CENSUS_DIR = Path("docs/rearchitecture/evidence/arch-census")
PERF_DIR = Path("docs/rearchitecture/evidence/perf-baseline")
COORD = Path(os.environ.get("COBOL_COORD", r"E:\COBOL-coord"))
# The dimensions (PROMPT.md §4's four, plus §5.5's modern-C#). Their criteria live in ONE place each — the review
# skill and design §5 — and the workflow brief points there; nothing here restates them (N4).
DIMENSIONS = ["architecture", "code", "performance", "duplication", "modern-csharp"]
WARNING = re.compile(r"^(?P<path>[A-Za-z]:[^(]+|/[^(]+)\((?P<line>\d+),\d+\): warning (?P<id>[A-Za-z]+\d+): (?P<msg>.*?)(?: \[[^\]]+\])?$")


def rel(path: str, root: Path) -> str | None:
    p = Path(path)
    try:
        return p.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return None


def cached(out: Path, step: str, key: dict, fn):
    """A mechanical step's result, recomputed unless `mech/<step>.json` was made for the same KEY (the pin, the step's
    version, and what else its result depends on). Keyed by the step name alone, a step cached before its code
    changed was silently reused (the w1034 refuter: semgrep.json predated the fleet's final code)."""
    f = out / "mech" / (step + ".json")
    if f.exists():
        have = json.loads(f.read_text(encoding="utf-8"))
        if isinstance(have, dict) and have.get("key") == key:
            print("r2_inputs: %s cached (%s)" % (step, f))
            return have["data"]
        print("r2_inputs: %s cached for another key — recomputing" % step)
    print("r2_inputs: %s ..." % step, flush=True)
    t0 = time.monotonic()
    data = fn()
    f.parent.mkdir(parents=True, exist_ok=True)
    f.write_text(json.dumps({"key": key, "seconds": round(time.monotonic() - t0, 1), "data": data}, indent=1),
                 encoding="utf-8")
    return data


def cached_data(out: Path, step: str, key: dict):
    """A step's cached result when it was made for KEY, else None (a shard waits for its projects' steps)."""
    f = out / "mech" / (step + ".json")
    if not f.exists():
        return None
    have = json.loads(f.read_text(encoding="utf-8"))
    return have["data"] if isinstance(have, dict) and have.get("key") == key else None


# ── the mechanical steps ─────────────────────────────────────────────────────────────────────────────────────────
def run_semgrep(pin: Path):
    cfg = pin / "scripts" / "semgrep" / "invariants.yml"
    r = subprocess.run(["semgrep", "scan", "--config", str(cfg), "--metrics=off", "--json", "-q", str(pin / "src")],
                       capture_output=True, text=True, encoding="utf-8")
    if r.returncode not in (0, 1) or not r.stdout.strip():
        raise SystemExit("semgrep failed (exit %d): %s" % (r.returncode, r.stderr[-2000:]))
    by_file = defaultdict(list)
    for x in json.loads(r.stdout).get("results", []):
        p = rel(x["path"], pin) or x["path"].replace("\\", "/")
        by_file[p].append({"rule": x["check_id"].rsplit(".", 1)[-1], "line": x["start"]["line"],
                           "message": x.get("extra", {}).get("message", "")[:300]})
    return dict(by_file)


def parse_warnings(text: str, root: Path):
    seen, by_file = set(), defaultdict(list)
    for line in text.splitlines():
        m = WARNING.match(line.strip())
        if not m:
            continue
        p = rel(m["path"], root)
        if p is None:
            continue
        key = (p, m["line"], m["id"])
        if key in seen:
            continue
        seen.add(key)
        by_file[p].append({"id": m["id"], "line": int(m["line"]), "message": m["msg"][:300]})
    return dict(by_file)


# The analyzer build runs ONE PROJECT AT A TIME, each cached as its own step (`mech/analyzers-<project>.json`), in
# dependency order. Measured 2026-10-07 (w1034): with every rule on, csc on Cobol.Net.Compiler ran single-threaded past
# 2,000 CPU-seconds and 28 GB while the leaf projects took under a minute, so a whole-solution step held EVERY shard's
# input hostage to the slowest project. Per project, a shard's input is written as soon as the projects holding its
# files are measured, and the batch that needs only the leaves need not wait for the compiler.
#
# ⛔ THE MODERN-C# DIMENSION NEEDS THE IDE AND SYSLIB RULES (the w1034 refuter's C4). `AnalysisLevel=latest-all` turns
# on every CA rule, but the code-style (IDE) rules keep their default severity (hidden or suggestion) and are never
# printed as warnings, so batch 1's inputs held 284 CA warnings and ZERO IDE/SYSLIB ones while design §5.5's features
# are exactly those rules. The analyzer tree therefore gets a `.globalconfig` (the SDK reads one at the tree root)
# raising the rules below to warning, written AFTER the plain build (SYSLIB1045 is a generator analyzer, and the
# pin's TreatWarningsAsErrors would fail that build). Measured on the pin 2026-10-07 (w1037s): Cobol.Net.Editions 4 s
# and Cobol.Net.Runtime 6 s with the rules on, the same as without; Runtime then reports IDE0305, IDE0032, IDE0028,
# IDE0300, IDE0290, IDE0078, IDE0306, IDE0330 and IDE0066. A §5.5 feature with no rule here has no analyzer: a point
# about it is a lead (N2). This table is the ONE place the mapping is written; each shard input carries it.
MODERN_RULES = {
    "primary constructors": ["IDE0290"],
    "collection expressions": ["IDE0028", "IDE0300", "IDE0301", "IDE0302", "IDE0303", "IDE0304", "IDE0305", "IDE0306"],
    "the field keyword (auto properties)": ["IDE0032"],
    "System.Threading.Lock": ["IDE0330"],
    "[GeneratedRegex]": ["SYSLIB1045"],
    "pattern matching over type tests": ["IDE0019", "IDE0020", "IDE0038", "IDE0066", "IDE0078", "IDE0083", "IDE0260"],
    "span-based parsing on hot paths": ["CA1845", "CA1846"],   # CA rules: already on at latest-all
    "extension members (C# 14)": [], "params spans": [], "frozen collections for static tables": [],
    "required members": [],
}
# a step's version: bump it when the step's code or its rule set changes, and every cached result of it is recomputed
STEP_VERSION = {"semgrep": 2, "drift": 2, "notes": 2, "analyzers": 3}


def modern_globalconfig() -> str:
    rules = sorted({r for ids in MODERN_RULES.values() for r in ids if not r.startswith("CA")})
    return ("is_global = true\n# r2_inputs.py MODERN_RULES (design §5.5): the modern-C# rules raised to warning for the "
            "analyzer step\n" + "".join("dotnet_diagnostic.%s.severity = warning\n" % r for r in rules))


def analyzer_key(commit: str) -> dict:
    return {"pin": commit, "version": STEP_VERSION["analyzers"], "modern_rules": MODERN_RULES}


PROJECT_ORDER = ["Cobol.Net.Editions", "Cobol.Net.Runtime", "Cobol.Net.Frontend", "Cobol.Net.Cli",
                 "Cobol.Net.Compiler.SourceGen", "Cobol.Net.Compiler", "Cobol.Net.Benchmarks",
                 "Cobol.Net.Tests.Characterization", "Cobol.Net.Tests.Conformance", "Cobol.Net.Tests.Unit",
                 "Cobol.Net.ArchOracle"]


def project_of(path: str) -> str | None:
    """The C# project whose folder holds `path` (src/<P>/..., tests/<P>/...), or None (scripts, CI, tools)."""
    parts = path.split("/")
    if len(parts) > 2 and parts[0] in ("src", "tests") and parts[1].startswith("Cobol.Net.") and path.endswith(".cs"):
        return parts[1]
    return None


def analyzer_worktree(commit: str, out: Path) -> Path:
    """One detached worktree of the pin under `<out>/mech/tree`, reused by every project step (removed by --clean)."""
    wt = out / "mech" / "tree"
    if not (wt / "Cobol.Net.sln").exists():
        subprocess.run(["git", "-C", str(REPO), "worktree", "add", "--detach", str(wt), commit], check=True,
                       capture_output=True)
        # a plain build first, so each analyzer step compiles ONLY its own project (BuildProjectReferences=false)
        r = subprocess.run(["dotnet", "build", str(wt / "Cobol.Net.sln"), "-c", "Debug", "-nologo", "-nodeReuse:false",
                            "-v:minimal"], capture_output=True, text=True, encoding="utf-8", errors="replace")
        if r.returncode != 0:
            raise SystemExit("the pin's plain build failed (exit %d):%s" % (r.returncode, r.stdout[-3000:]))
    return wt


def write_modern_config(wt: Path, commit: str):
    """The analyzer tree's `.globalconfig` (MODERN_RULES), never over one the pin itself tracks."""
    tracked = subprocess.run(["git", "-C", str(REPO), "ls-tree", "--name-only", commit, ".globalconfig"],
                             capture_output=True, text=True, check=True).stdout.strip()
    if tracked:
        raise SystemExit("the pin tracks its own .globalconfig: merge MODERN_RULES into it here before measuring")
    (wt / ".globalconfig").write_text(modern_globalconfig(), encoding="utf-8")


def run_analyzers(wt: Path, project: str, commit: str):
    """Build one project of the pin's worktree with every analyzer rule on (CA at latest-all, the MODERN_RULES IDE and
    SYSLIB rules through the tree's `.globalconfig`); its own MSBuild nodes and compiler (`-nodeReuse:false`,
    `UseSharedCompilation=false`): attached to the fleet gates' shared nodes the first run sat an hour with the
    machine 40 % idle. Only the warnings inside the project's own folder are kept."""
    proj = next(wt.glob("*/%s/%s.csproj" % (project, project)), None)
    if proj is None:
        raise SystemExit("no project %s in the pin's tree" % project)
    write_modern_config(wt, commit)
    r = subprocess.run(["dotnet", "build", str(proj), "-c", "Debug", "--no-incremental", "-nologo",
                        "-p:AnalysisLevel=latest-all", "-p:TreatWarningsAsErrors=false", "-p:WarningsAsErrors=",
                        "-p:EnforceCodeStyleInBuild=true", "-nodeReuse:false", "-p:UseSharedCompilation=false",
                        "-p:BuildProjectReferences=false", "-clp:NoSummary;ForceNoAlign", "-v:minimal"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        raise SystemExit("the analyzer build of %s failed (exit %d):\n%s" % (project, r.returncode, r.stdout[-3000:]))
    return {f: w for f, w in parse_warnings(r.stdout, wt).items() if project_of(f) == project}


def parse_drift(text: str):
    out, cur = {}, None
    for line in text.splitlines():
        m = re.match(r"^== (.+?): \d+ specific drift rule", line)
        if m:
            cur = m.group(1)
            out[cur] = []
        elif cur and line.startswith("  ") and not line.startswith("    ") and not line.startswith("  +"):
            out[cur].append(line.strip().split("  (")[0])
    return out


def run_drift(pin: Path, files):
    out = {}
    cs = [f for f in files if f.endswith(".cs")]
    for i in range(0, len(cs), 200):
        r = subprocess.run([sys.executable, str(pin / "scripts" / "spec" / "drift_rules.py")] + cs[i:i + 200],
                           cwd=str(pin), capture_output=True, text=True, encoding="utf-8")
        if r.returncode != 0:
            raise SystemExit("drift_rules.py failed: " + r.stderr[-2000:])
        out.update({k: v for k, v in parse_drift(r.stdout).items() if v})
    return out


def open_notes(files):
    """-> {file: [open note ids naming it]} over the register at this checkout."""
    want, by_file = set(files), defaultdict(list)
    for it in work.load():
        if it.get("status") in work.TERMINAL_STATUSES:
            continue
        for p, _ in work.named_paths(it.get("_body", "")):
            if p in want and it["_file"][:-3] not in by_file[p]:
                by_file[p].append(it["_file"][:-3])
    return dict(by_file)


# ── census rows ──────────────────────────────────────────────────────────────────────────────────────────────────
def finding_files(f):
    s = f.get("site")
    if isinstance(s, str):
        return {s.split(":")[0]}
    if isinstance(s, list):
        return {x.split(":")[0].split(" ")[0] for x in s}
    if isinstance(s, dict):
        return set(s.get("files", [])) | {m.rsplit(" @ ", 1)[1].rsplit(":", 1)[0] for m in s.get("members", []) if " @ " in m}
    return set()


def folder_of(f):
    return f["site"] if isinstance(f.get("site"), str) and f["kind"] == "folder-namespace" else None


def declared_namespaces(members):
    """-> {file: {namespace of each type declared in it}} from the member index of the pin."""
    out = defaultdict(set)
    if not members:
        return out
    for t, idx in members.get("types", {}).items():
        ns = t.split("+")[0].rsplit(".", 1)[0]
        for i in idx:
            out[members["files"][i]].add(ns)
    return out


def shard_input(sh, m, mech, census, record, ns_of=None):
    files = [p for p, _ in sh["files"]]
    fs = set(files)
    folders = {p.rsplit("/", 1)[0] for p in files}
    rows = defaultdict(list)
    clones = []
    for f in census["findings"]:
        hit = finding_files(f) & fs or (folder_of(f) in folders)
        if not hit:
            continue
        if f["kind"] == "clone-family":
            clones.append(f["id"])
        else:
            rows[f["kind"]].append(f)
    namespaces = sorted({ns for p in files for ns in (ns_of or {}).get(p, ())})
    edges = [e for e in record.get("namespaceEdges", []) if e[0] in namespaces or e[1] in namespaces]
    return {
        "shard": sh["id"], "subsystem": sh["subsystem"], "name": sh["name"], "pin_commit": m["commit"],
        "census_record": m["census"], "lines": sh["lines"],
        "files": sh["files"],
        "semgrep": {p: mech["semgrep"][p] for p in files if p in mech["semgrep"]},
        "analyzers": {p: mech["analyzers"][p] for p in files if p in mech["analyzers"]},
        "modern_rules": MODERN_RULES,   # the §5.5 feature -> analyzer rule ids map the modern-C# dimension counts by
        "drift_rules": {p: mech["drift"][p] for p in files if p in mech["drift"]},
        "open_notes": {p: mech["notes"][p] for p in files if p in mech["notes"]},
        "census": {k: v for k, v in rows.items()},
        "clone_families": clones,
        "god_classes": sh.get("god_classes", []),
        "namespaces": namespaces,
        "namespace_edges": edges,
        "namespace_edges_how": "the census record's measured namespaceEdges [from, to, uses] touching this shard's "
                               "namespaces; §8.1 of the design holds the allowed edges",
    }


def duplication_input(m, census):
    where = {p: s["id"] for s in m["shards"] for p, _ in s["files"]}
    fams = []
    for f in census["findings"]:
        if f["kind"] != "clone-family":
            continue
        copies = [{"site": x, "shard": where.get(x.split(":")[0])} for x in f["site"]]
        fams.append({"id": f["id"], "measure": f.get("measure"), "copies": copies,
                     "cross_shard": len({c["shard"] for c in copies}) > 1, "rule": f.get("rule"), "target": f.get("target")})
    return {"pin_commit": m["commit"], "census_record": m["census"], "families": fams,
            "how": "the census's type-2 detector (roslyn-analysis detect-clones, minTokens and mode in the record's "
                   "`clones`); a clone family is ONE finding however many shards its copies touch"}


def perf_input():
    recs = sorted((REPO / PERF_DIR).glob("*.md"), key=lambda p: p.stat().st_mtime)
    if not recs:
        raise SystemExit("no performance baseline record under " + str(PERF_DIR))
    r = recs[-1].relative_to(REPO).as_posix()
    # No re-measuring command: a reviewer is read-only and builds nothing (the fleet's build hook refuses it while
    # the fleet runs), and `perf_baseline.py --against` measures the checkout, not the pin (the w1034 refuter's C4).
    return {"baseline": r,
            "rule": "a performance claim needs a measurement ON THE PIN: a row of this baseline record, or a count at a "
                    "stated input size read from the pinned code (allocations per call, passes over a collection); "
                    "without one it is a LEAD (`kind: lead` in the finding), never a finding (§4 item 5, §5.4)"}


def build(pin: Path, out: Path, max_lines: int, projects=None):
    commit = subprocess.run(["git", "-C", str(pin), "rev-parse", "HEAD"], capture_output=True, text=True,
                            check=True).stdout.strip()
    m = r2_subsystems.measure(commit, max_lines)
    if m["holes"] or m["overlaps"]:
        raise SystemExit("the subsystem table is not a partition at %s: holes %s overlaps %s" % (
            commit[:12], m["holes"][:5], list(m["overlaps"])[:5]))
    if not m["census"]:
        raise SystemExit("no census record for the pin %s: run `python scripts/arch/census.py --commit %s` first (B7)" % (
            commit[:12], commit[:12]))
    out.mkdir(parents=True, exist_ok=True)
    (out / "shards.json").write_text(json.dumps(m, indent=1), encoding="utf-8")
    files = [p for s in m["shards"] for p, _ in s["files"]]
    head = subprocess.run(["git", "-C", str(REPO), "rev-parse", "HEAD"], capture_output=True, text=True,
                          check=True).stdout.strip()
    pin_key = lambda step: {"pin": commit, "version": STEP_VERSION[step]}  # noqa: E731
    mech = {"semgrep": cached(out, "semgrep", pin_key("semgrep"), lambda: run_semgrep(pin)),
            "drift": cached(out, "drift", pin_key("drift"), lambda: run_drift(pin, files)),
            # the open notes are this checkout's register, not the pin's: keyed by the checkout's HEAD
            "notes": cached(out, "notes", dict(pin_key("notes"), register=head), lambda: open_notes(files)),
            "analyzers": {}}
    measured = set()
    wanted = [p for p in PROJECT_ORDER if projects is None or p in projects]
    akey = analyzer_key(commit)
    for proj in PROJECT_ORDER:
        step = "analyzers-" + proj
        if proj in wanted and cached_data(out, step, akey) is None:
            wt = analyzer_worktree(commit, out)
            cached(out, step, akey, lambda: run_analyzers(wt, proj, commit))
        data = cached_data(out, step, akey)
        if data is not None:
            mech["analyzers"].update(data)
            measured.add(proj)
    if measured == set(PROJECT_ORDER) and (out / "mech" / "tree").exists():   # every project measured: drop the tree
        subprocess.run(["git", "-C", str(REPO), "worktree", "remove", "--force", str(out / "mech" / "tree")],
                       capture_output=True)
    census = json.loads((REPO / (m["census"][:-5] + ".findings.json")).read_text(encoding="utf-8"))
    record = json.loads((REPO / m["census"]).read_text(encoding="utf-8"))
    (out / "inputs").mkdir(exist_ok=True)

    mi = REPO / CENSUS_DIR / (commit + ".members.json.gz")
    if not mi.exists():
        raise SystemExit("no member index for the pin %s: run `python scripts/arch/member_index.py --record` there (B7)" % commit[:12])
    with gzip.open(mi, "rt", encoding="utf-8") as fh:
        ns_of = declared_namespaces(json.load(fh))
    pending = []
    for s in m["shards"]:
        need = {project_of(p) for p, _ in s["files"]} - {None}
        if need - measured:   # a shard's input is written only once every project holding its files is measured
            pending.append("%s (%s)" % (s["id"], ", ".join(sorted(need - measured))))
            continue
        d = shard_input(s, m, mech, census, record, ns_of)
        (out / "inputs" / ("in-%s.json" % s["id"])).write_text(json.dumps(d, indent=1), encoding="utf-8")
    for x in pending:
        print("r2_inputs: PENDING %s — analyzers not yet measured" % x)
    (out / "inputs" / "in-duplication.json").write_text(json.dumps(duplication_input(m, census), indent=1), encoding="utf-8")
    (out / "inputs" / "perf.json").write_text(json.dumps(perf_input(), indent=1), encoding="utf-8")
    print("=== R2 INPUTS: WROTE %d shard inputs, %d pending, at %s (semgrep %d files, analyzers %d files, drift %d files) to %s ===" % (
        len(m["shards"]) - len(pending), len(pending), commit[:12], len(mech["semgrep"]), len(mech["analyzers"]), len(mech["drift"]), out))
    return 0


def batch_args(out: Path, shards, dims, pin: str, duplication: bool, width: int, stop: str, global_stop: str,
               label: str = "1"):
    m = json.loads((out / "shards.json").read_text(encoding="utf-8"))
    by_id = {s["id"]: s for s in m["shards"]}
    missing = [s for s in shards if s not in by_id]
    if missing:
        raise SystemExit("unknown shard(s): " + ", ".join(missing))
    bad = [d for d in dims if d not in DIMENSIONS]
    if bad:
        raise SystemExit("unknown dimension(s): %s (known: %s)" % (bad, DIMENSIONS))
    for s in shards:
        if not (out / "inputs" / ("in-%s.json" % s)).exists():
            raise SystemExit("no input file for shard %s: run --pin first" % s)
    return {"batch": label, "pinnedTree": pin, "pinCommit": m["commit"], "outDir": str(out / ("batch-" + label)),
            "inputsDir": str(out), "stopFile": stop,
            "globalStopFile": global_stop, "width": width, "dimensions": dims, "duplicationPass": duplication,
            "shards": [{"id": s, "name": by_id[s]["name"], "lines": by_id[s]["lines"],
                        "input": str(out / "inputs" / ("in-%s.json" % s)),
                        "files": [p for p, _ in by_id[s]["files"]],
                        # per file, so a RESUMED pair sizes its unread remainder (two finders over 4,000 lines)
                        "fileLines": [n for _, n in by_id[s]["files"]]} for s in shards]}


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test():
    ok = True

    def arm(name, cond):
        nonlocal ok
        print(("PASS  " if cond else "FAIL  ") + name)
        ok = ok and cond

    root = Path(tempfile.gettempdir()) / "r2root"
    w = ("%s\\src\\A\\X.cs(12,5): warning CA1822: Member 'F' does not access instance data [%s\\src\\A\\A.csproj]\n"
         "%s\\src\\A\\X.cs(12,5): warning CA1822: Member 'F' does not access instance data [%s\\src\\A\\A.csproj]\n"
         "C:\\elsewhere\\Y.cs(1,1): warning IDE0005: Using directive is unnecessary.\n"
         "%s\\src\\A\\X.cs(30,1): error CS0001: not a warning\n") % ((str(root),) * 5)
    pw = parse_warnings(w.replace("\\", os.sep), root)
    arm("analyzer warnings parse by file, dedupe, and drop files outside the tree",
        pw == {"src/A/X.cs": [{"id": "CA1822", "line": 12, "message": "Member 'F' does not access instance data"}]})
    d = parse_drift("== src/A/X.cs: 2 specific drift rule(s)\n  ATests  (tests/ATests.cs)\n    the rule\n"
                    "  BTests  (tests/BTests.cs)\n    other\n  + 3 tree-wide sweep(s) also scan it: C\n"
                    "== src/A/Y.cs: 0 specific drift rule(s)\n")
    arm("a C# file maps to its project; a script maps to none",
        project_of("src/Cobol.Net.Runtime/IO/X.cs") == "Cobol.Net.Runtime" and project_of("scripts/a.py") is None
        and project_of("tests/Cobol.Net.Tests.Unit/A.cs") == "Cobol.Net.Tests.Unit" and project_of("src/Cobol.Net.Frontend/Grammar/G.g4") is None)
    arm("drift rules parse per file", d == {"src/A/X.cs": ["ATests", "BTests"], "src/A/Y.cs": []})
    arm("a member site resolves to its file",
        finding_files({"site": {"members": ["M:N.T.F() @ src/A/X.cs:12"]}}) == {"src/A/X.cs"})
    arm("a clone site resolves to its files",
        finding_files({"site": ["src/A/X.cs:1-9 (T.F)", "src/B/Y.cs:3-9 (U.G)"]}) == {"src/A/X.cs", "src/B/Y.cs"})
    m = {"commit": "c" * 40, "census": "r.json",
         "shards": [{"id": "a", "files": [["src/A/X.cs", 10]]}, {"id": "b", "files": [["src/B/Y.cs", 10]]}]}
    census = {"findings": [{"kind": "clone-family", "id": "R0-1", "site": ["src/A/X.cs:1-9 (T.F)", "src/B/Y.cs:3-9 (U.G)"]},
                           {"kind": "god-class", "id": "R0-2", "site": {"type": "T", "files": ["src/A/X.cs"]}},
                           {"kind": "folder-namespace", "id": "R0-3", "site": "src/B"}]}
    dup = duplication_input(m, census)
    arm("a clone family spanning two shards is ONE family, marked cross-shard",
        len(dup["families"]) == 1 and dup["families"][0]["cross_shard"] and
        [c["shard"] for c in dup["families"][0]["copies"]] == ["a", "b"])
    mech = {"semgrep": {"src/A/X.cs": [{"rule": "r"}]}, "analyzers": {}, "drift": {}, "notes": {"src/B/Y.cs": ["PB1"]}}
    ns_of = declared_namespaces({"files": ["src/A/X.cs", "src/B/Y.cs"], "types": {"N.A.T": [0], "N.A.T+In": [0], "N.B.U": [1]}})
    rec = {"namespaceEdges": [["N.A", "N.B", 3], ["N.C", "N.D", 1]]}
    ia = shard_input({"id": "a", "subsystem": "s", "name": "S", "lines": 10, "files": [["src/A/X.cs", 10]]}, m, mech, census, rec, ns_of)
    arm("a shard carries its namespaces' measured edges and no others",
        ia["namespaces"] == ["N.A"] and ia["namespace_edges"] == [["N.A", "N.B", 3]])
    ib = shard_input({"id": "b", "subsystem": "s", "name": "S", "lines": 10, "files": [["src/B/Y.cs", 10]]}, m, mech, census, {})
    arm("a shard input holds only its own files' facts",
        ia["semgrep"] and not ib["semgrep"] and ib["open_notes"] == {"src/B/Y.cs": ["PB1"]} and not ia["open_notes"])
    arm("census rows go to the shard holding their site or folder",
        list(ia["census"]) == ["god-class"] and list(ib["census"]) == ["folder-namespace"] and
        ia["clone_families"] == ["R0-1"] == ib["clone_families"])
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp)
        (out / "inputs").mkdir()
        (out / "shards.json").write_text(json.dumps({"commit": "c", "shards": [
            {"id": "cli", "name": "CLI", "lines": 3, "files": [["a", 3]]}]}), encoding="utf-8")
        try:
            batch_args(out, ["cli"], ["architecture"], "P", False, 8, "S", "G")
            arm("batch args refuse a shard with no input file", False)
        except SystemExit:
            arm("batch args refuse a shard with no input file", True)
        (out / "inputs" / "in-cli.json").write_text("{}", encoding="utf-8")
        a = batch_args(out, ["cli"], ["architecture", "code"], "P", True, 8, "S", "G", "7")
        arm("batch args carry the shard's files, input, stops, dimensions and the batch's own directory",
            a["shards"][0]["files"] == ["a"] and a["stopFile"] == "S" and a["globalStopFile"] == "G" and a["duplicationPass"]
            and a["outDir"] == str(out / "batch-7"))
        try:
            batch_args(out, ["cli"], ["style"], "P", False, 8, "S", "G")
            arm("batch args refuse an unknown dimension", False)
        except SystemExit:
            arm("batch args refuse an unknown dimension", True)
        arm("batch args carry each file's lines, so a resumed pair can size its remainder", a["shards"][0]["fileLines"] == [3])
        # PB2707: a new batch is sized as the plan of its first launch (every pair unread, the clone pass undone):
        # 2 dimensions x 3 lines x (100 finder + 3 x 0.01 x 10,000 verdict) tokens + the 50,000-token clone pass
        b = {"session_soft_stop_pct": 97, "session_est_pct": 0.0, "headroom_pct": 50.0, "resume_at": None}
        est = r2_cost.size(r2_collect.plan_of(a, {}, {}), r2_cost.fixture_rules(), b, 0.0)
        arm("a new batch is sized as its first launch's plan: every pair's finders and expected verdicts, the clone pass",
            est["tokens"] == 2 * 3 * 400 + 50_000 and est["pairs"] == 3 and est["fits"])
        est = r2_cost.size(r2_collect.plan_of(a, {}, {}), r2_cost.fixture_rules(), dict(b, session_est_pct=96.99), 0.0)
        arm("a new batch that cannot finish in the room left is refused, its parts named (the room now, then a window)",
            not est["fits"] and [p["shards"] for p in est["parts"]] == [["cli"], ["global"]]
            and [p["when"] for p in est["parts"]] == ["now", "next session window"])
        runs = []
        k1, k2 = {"pin": "c", "version": 1}, {"pin": "c", "version": 2}
        cached(out, "s", k1, lambda: runs.append(1) or {"x": 1})
        v = cached(out, "s", k1, lambda: runs.append(2) or {"x": 2})
        arm("a cached step is reused for the same key", runs == [1] and v == {"x": 1})
        v = cached(out, "s", k2, lambda: runs.append(3) or {"x": 3})
        arm("a cached step made for another key (pin, version, rules) is recomputed, never reused",
            runs == [1, 3] and v == {"x": 3} and cached_data(out, "s", k1) is None and cached_data(out, "s", k2) == {"x": 3})
    gc = modern_globalconfig()
    arm("the analyzer tree's globalconfig raises every §5.5 IDE/SYSLIB rule to warning, and no CA rule",
        gc.startswith("is_global = true") and "dotnet_diagnostic.IDE0290.severity = warning" in gc and
        "dotnet_diagnostic.SYSLIB1045.severity = warning" in gc and "IDE0330" in gc and "CA1845" not in gc)
    arm("the analyzer cache key moves with the rule table", analyzer_key("c")["modern_rules"] is MODERN_RULES)
    arm("a shard input carries the feature -> rule map the modern-C# dimension counts by", ia["modern_rules"] is MODERN_RULES)
    print("=== R2 INPUTS SELF-TEST: %s ===" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--pin", type=Path, help="the built, detached tree at the pinned commit (reviewers read it)")
    ap.add_argument("--out", type=Path)
    ap.add_argument("--max-lines", type=int, default=r2_subsystems.MAX_LINES)
    ap.add_argument("--projects", help="measure the analyzers of only these projects now (comma-separated); shards whose "
                                       "files lie in an unmeasured project stay PENDING until a later run measures it")
    ap.add_argument("--batch", help="write the Workflow args of batch <label> to <out>/batch-<label>/batch-args.json")
    ap.add_argument("--shards", help="with --batch: comma-separated shard ids, in dispatch order")
    ap.add_argument("--dimensions", default=",".join(DIMENSIONS))
    ap.add_argument("--duplication", action="store_true", help="with --batch: include the ONE whole-codebase clone pass")
    ap.add_argument("--width", type=int, default=8)
    ap.add_argument("--stop-file", default=str(COORD / "scratch" / "STOP-r2"))
    ap.add_argument("--global-stop-file", default=str(COORD / "scratch" / "STOP"))
    r2_cost.add_budget_args(ap)
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if not a.out:
        ap.error("--out is required")
    if a.batch:
        if not a.shards and not a.duplication:
            ap.error("--batch needs --shards (or --duplication alone: a batch of only the clone pass)")
        m = json.loads((a.out / "shards.json").read_text(encoding="utf-8"))
        pin = a.pin or Path(json.loads((a.out / "pin.json").read_text(encoding="utf-8"))["pin"])
        args = batch_args(a.out, [s.strip() for s in (a.shards or "").split(",") if s.strip()],
                          [d.strip() for d in a.dimensions.split(",") if d.strip()], str(pin), a.duplication, a.width,
                          a.stop_file, a.global_stop_file, a.batch)
        # SIZED BEFORE IT IS WRITTEN (kb/Work PB2707): a batch that cannot finish in the room left is refused, with
        # its estimate and the batches to write instead, one command each
        est = r2_cost.size(r2_collect.plan_of(args, {}, {}), r2_cost.rules(), r2_cost.budget_from(a), a.session_reserve)
        flags = "".join((" --duplication" if a.duplication else "", " --width %d" % a.width if a.width != 8 else "",
                         " --dimensions %s" % a.dimensions if a.dimensions != ",".join(DIMENSIONS) else "",
                         " --borrow-days %d" % a.borrow_days if a.borrow_days else "",
                         " --session-reserve %g" % a.session_reserve if a.session_reserve else ""))
        def part_command(p, i):
            # the clone pass is `--duplication`, never a shard id; a part of only the clone pass names no shard
            ids = [s for s in p["shards"] if s != "global"]
            return "python scripts/arch/r2_inputs.py --out %s --batch %s%s%s%s" % (
                a.out, a.batch, chr(ord("a") + i), " --shards " + ",".join(ids) if ids else "",
                flags if "global" in p["shards"] else flags.replace(" --duplication", ""))
        print(r2_cost.report(est, "batch %s" % a.batch, part_command))
        if not est["fits"]:
            print("=== R2 INPUTS: REFUSED batch %s — it cannot finish in the room left; write the parts above, one "
                  "command each, and launch each when its window comes (nothing written) ===" % a.batch)
            return r2_cost.REFUSED
        dest = Path(args["outDir"]) / "batch-args.json"
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(json.dumps(args, indent=1), encoding="utf-8")
        print("=== R2 INPUTS: WROTE batch args %s (%d shards x %d dimensions%s, pin %s) ===" % (
            dest, len(args["shards"]), len(args["dimensions"]), " + duplication pass" if a.duplication else "",
            m["commit"][:12]))
        return 0
    if not a.pin:
        ap.error("--pin is required to build the inputs")
    a.out.mkdir(parents=True, exist_ok=True)
    (a.out / "pin.json").write_text(json.dumps({"pin": str(a.pin)}), encoding="utf-8")
    return build(a.pin, a.out, a.max_lines,
                 set(x.strip() for x in a.projects.split(",")) if a.projects else None)


if __name__ == "__main__":
    sys.exit(main())
