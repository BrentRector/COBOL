#!/usr/bin/env python3
"""record_impact_map.py — RECORD the per-test impact map at a commit (kb/Work PB1683).

An implementer's gate used to run the tests its author NAMED. Train 68b dropped two groups on whole-assembly reds
their filtered gates never ran (`DiagnosticPositionTests` for a COPY library-search change; a `CONSTANT AS NULL`
crash in `DataBinder.Constants.cs` no filter term named), and wave 69 existed only to finish them. The map this
script records answers the question the names were guessing: WHICH TESTS EXECUTE WHICH SOURCE FILES.
`scripts/spec/impacted_tests.py` turns it into the TIERS of a change, which `scripts/gate_plan.py` uses to ORDER the
implementer's whole-population gate — the map never selects a gate (kb/Work PB1708, PB1717).

How (the design is docs/rearchitecture/DESIGN-test-build-ci.md §3.13):

  1. a DETACHED worktree at the commit (never the caller's tree — the recording build replaces its binaries);
  2. a RECORDING build of the solution: `-p:CustomAfterMicrosoftCommonTargets=tools/impact/ImpactRecording.targets`
     compiles `tools/impact/ImpactProbe.cs` into the greenfield assemblies and the three test assemblies, and
     `ImpactTestFramework.cs` (xunit's framework plus a context-tracking message bus) into the test assemblies;
  3. `tools/impact/ImpactInstrumenter` rewrites those assemblies so every method calls the probe on entry, and every
     `Process.Start` hands the child (a compiled COBOL program) a hits file its test folds back in;
  4. each test assembly runs ONCE, with the compiled-program cache OFF (a cache hit would skip the compiler and
     record nothing for it), writing one context line per test, class and collection;
  5. the contexts are merged into `<store>/<sha>.json.gz`: per source file, the tests whose execution reached it.

The store defaults to `<git common dir>/cobol-impact/`, which every worktree of the repository shares, so an
implementer's worktree finds a map recorded in any worktree without copying anything.

⛔ It is RECORDED, never hand-maintained, and a map is valid for exactly the tree it was recorded at —
for a map older than the change's base by more than documentation, `impacted_tests.py` puts every test in tier 1 and
uses the map only for its recorded durations and test names.

Usage:
    python scripts/spec/record_impact_map.py                      # HEAD, all three assemblies
    python scripts/spec/record_impact_map.py --commit <sha>
    python scripts/spec/record_impact_map.py --commit <old-sha> --overlay   # a tree older than this tooling
    python scripts/spec/record_impact_map.py --filter "FullyQualifiedName~DiagnosticPosition" --assemblies Conformance
        (a TRIAL: the map is written as <sha>.partial.json.gz, which impacted_tests.py never uses by default)

The last line printed is the verdict: `=== IMPACT MAP: RECORDED <path> … ===` or `=== IMPACT MAP: FAILED … ===`.
"""

from __future__ import annotations

import argparse
import base64
import datetime as dt
import gzip
import json
import os
import shutil
import subprocess
import sys
import time
import zlib
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
# The population and the scrub rule are scripts/test_population.py's (kb/Work PB1718): one `--list-tests` reader,
# one statement of the environment variables that may narrow a `dotnet test` run.
sys.path.insert(0, str(REPO / "scripts"))
from test_population import GATED_ASSEMBLIES as ASSEMBLIES, PopulationError, list_population, scrubbed_env  # noqa: E402
# ⛔ The recorder builds and runs every test assembly, so it takes a GATE SLOT like an implementer gate
# (DESIGN-test-build-ci.md §3.14.6): held from before the worktree is made to the map's write, and every child is
# spawned with `**slot.spawn_kwargs()`, or a Linux child runs outside the cap and a build server outlives the slot.
from gate_slot import GateSlots, Slot  # noqa: E402

SCHEMA = 2
# The assemblies whose execution the map records — keep in step with tools/impact/ImpactRecording.targets.
PROBED = [
    "cobol", "Cobol.Net.Compiler", "Cobol.Net.Editions", "Cobol.Net.Frontend", "Cobol.Net.Runtime",
    "Cobol.Net.Tests.Characterization", "Cobol.Net.Tests.Conformance", "Cobol.Net.Tests.Unit",
]


def git(*args: str, cwd: Path = REPO) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout.strip()


def default_store() -> Path:
    common = Path(git("rev-parse", "--git-common-dir"))
    if not common.is_absolute():
        common = (REPO / common).resolve()
    return common / "cobol-impact"


def priority_flags(priority: str) -> dict:
    if os.name == "nt" and priority == "BelowNormal":
        return {"creationflags": subprocess.BELOW_NORMAL_PRIORITY_CLASS}
    if os.name == "nt" and priority == "Idle":
        return {"creationflags": subprocess.IDLE_PRIORITY_CLASS}
    return {}


def run(cmd: list[str], log: Path, cwd: Path, slot: Slot, env: dict | None = None,
        priority: str = "BelowNormal") -> int:
    with open(log, "w", encoding="utf-8", errors="replace") as fh:
        proc = subprocess.run(cmd, cwd=cwd, stdout=fh, stderr=subprocess.STDOUT, **priority_flags(priority),
                              **slot.spawn_kwargs(env=env))
    return proc.returncode


def verdict_line(log: Path) -> str:
    for line in reversed(log.read_text(encoding="utf-8", errors="replace").splitlines()):
        s = line.strip()
        if s.startswith(("Passed!", "Failed!")):
            return s
    return "(no verdict line)"


def read_hits(path: Path) -> set[int]:
    return {int(x) for x in path.read_text(encoding="utf-8").split() if x.strip().isdigit()}


def pack(n: int) -> str:
    """A bitset (bit i = probe entry i) in the recorder's own encoding: little-endian bytes, zlib, base64."""
    return base64.b64encode(zlib.compress(n.to_bytes((n.bit_length() + 7) // 8, "little"))).decode("ascii")


def unpack(b64: str) -> int:
    """A recorded bitset as a Python int: bit i set = probe entry i was reached."""
    return int.from_bytes(zlib.decompress(base64.b64decode(b64)), "little") if b64 else 0


def bit_ids(n: int):
    """The set bits of `n`, ascending — byte-wise, since shifting a 45,000-bit int bit by bit is quadratic."""
    for bi, byte in enumerate(n.to_bytes((n.bit_length() + 7) // 8, "little")):
        if byte:
            for k in range(8):
                if byte >> k & 1:
                    yield bi * 8 + k


def merge(raw: Path, table: dict, sha: str, known: list[str], listed: dict[str, list[str]],
          verdicts: dict[str, str], timings: dict, trial_filter: str | None) -> dict:
    tests: list[list] = []
    test_bits: list[str] = []
    class_bits: dict[str, int] = {}
    collection_bits: dict[str, int] = {}
    children = missing = 0
    asm_of = {Path(p).name: a for a, p in ASSEMBLIES.items()}
    for jl in sorted((raw / "contexts").glob("*.jsonl")):
        asm = asm_of.get(jl.stem, jl.stem)
        listed_set = set(listed.get(asm, []))
        for line in jl.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue
            c = json.loads(line)
            children += c.get("children", 0)
            missing += c.get("missing_children", 0)
            if c["kind"] == "test":
                tests.append([asm, c["name"], c["display"], c["class"], f"{asm}:{c['collection']}",
                              float(c.get("seconds") or 0), c["display"] in listed_set])
                test_bits.append(c["bits"])
            elif c["kind"] == "class":
                key = f"{asm}:{c['name']}"
                class_bits[key] = class_bits.get(key, 0) | unpack(c["bits"])
            else:
                key = f"{asm}:{c['collection']}"
                collection_bits[key] = collection_bits.get(key, 0) | unpack(c["bits"])

    ambient = 0
    for d in ("ambient", "orphans"):
        for h in (raw / d).glob("*.hits") if (raw / d).is_dir() else []:
            for i in read_hits(h):
                ambient |= 1 << i
    orphans = len(list((raw / "orphans").glob("*.hits"))) if (raw / "orphans").is_dir() else 0

    population = {}
    for asm, names in listed.items():
        recorded = {t[2] for t in tests if t[0] == asm}
        # A theory whose data is not serializable is ONE listed case whose rows are named only when they run
        # ("Method" listed, "Method(item: …)" recorded) — its rows cover it.
        row_prefixes = {d.split("(", 1)[0] for d in recorded if "(" in d}
        unrecorded = [n for n in names if n not in recorded and n not in row_prefixes]
        population[asm] = {"listed": len(names), "recorded": sum(1 for t in tests if t[0] == asm),
                           "listed_but_unrecorded": unrecorded[:50], "listed_but_unrecorded_count": len(unrecorded)}

    return {
        "schema": SCHEMA,
        "commit": sha,
        "recorded_at": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"),
        "partial": trial_filter is not None,
        "filter": trial_filter,
        "timings_s": timings,
        "verdicts": verdicts,
        "population": population,
        "children": {"started": children, "without_hits_file": missing, "orphans": orphans},
        "entries": [[e["n"], e["s"]] for e in table["entries"]],
        "implies": {str(k): v for k, v in table["implies"].items()},
        "known_files": known,
        "tests": tests,
        "test_bits": test_bits,
        "class_bits": {k: pack(v) for k, v in class_bits.items()},
        "collection_bits": {k: pack(v) for k, v in collection_bits.items()},
        "ambient_bits": pack(ambient),
        "assemblies": table.get("assemblies", []),
    }


def reached_files(m: dict) -> set[str]:
    union = 0
    for b in m["test_bits"]:
        union |= unpack(b)
    return {seg[0] for i in bit_ids(union) for seg in m["entries"][i][1]}


def check(m: dict) -> list[str]:
    """⛔ The recording's own watchdog: a map that silently recorded nothing for a component would make every
    change to it look like it touches no test."""
    problems = []
    hit = reached_files(m)
    for prefix in ("src/Cobol.Net.Compiler/", "src/Cobol.Net.Runtime/", "src/Cobol.Net.Frontend/"):
        if not any(f.startswith(prefix) for f in hit):
            problems.append(f"no test context reached any file under {prefix}")
    if not m["partial"]:
        for asm, p in m["population"].items():
            if p["recorded"] == 0:
                problems.append(f"{asm}: no test recorded")
            elif p["listed_but_unrecorded_count"] > 0:
                problems.append(f"{asm}: {p['listed_but_unrecorded_count']} listed test(s) never recorded "
                                f"(first: {p['listed_but_unrecorded'][:3]})")
    return problems


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--commit", default="HEAD")
    ap.add_argument("--assemblies", default=",".join(ASSEMBLIES), help="comma-separated: " + ",".join(ASSEMBLIES))
    ap.add_argument("--filter", help="a TRIAL run over this vstest filter (Conformance only); writes a partial map")
    ap.add_argument("--store", type=Path, help="where maps live (default <git common dir>/cobol-impact)")
    ap.add_argument("--work", type=Path, help="scratch root for the worktree and raw hits (default: the store)")
    ap.add_argument("--overlay", action="store_true",
                    help="copy tools/impact from THIS tree into the recorded one (a commit older than the tooling)")
    ap.add_argument("--keep", action="store_true", help="keep the detached worktree afterwards")
    ap.add_argument("--priority", default="BelowNormal", choices=["Normal", "BelowNormal", "Idle"])
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)
    except Exception:  # noqa: BLE001
        pass

    sha = git("rev-parse", args.commit)
    store = (args.store or default_store()).resolve()
    store.mkdir(parents=True, exist_ok=True)
    work = (args.work or store).resolve() / f"rec-{sha[:12]}-{int(time.time())}"
    wt, raw, logs = work / "wt", work / "raw", work / "logs"
    for d in (raw, logs):
        d.mkdir(parents=True, exist_ok=True)
    asms = [a.strip() for a in args.assemblies.split(",") if a.strip()]
    print(f"impact map: recording {sha} in {wt}")

    def fail(msg: str) -> int:
        print(f"=== IMPACT MAP: FAILED — {msg} (logs {logs}) ===")
        return 1

    slots = GateSlots.for_repo()
    try:
        slot = slots.acquire(f"record_impact_map {sha[:12]} (pid {os.getpid()}, {time.strftime('%Y-%m-%d %H:%M:%S')})",
                             say=lambda line: print(line, flush=True))
    except ValueError as e:  # malformed shared gate settings (gate_slot.py, kb/Work PB2514): never a silent default
        print(f"=== IMPACT MAP: FAILED — {e} ===")
        return 2
    with slot:
        return record(args, sha, store, wt, raw, logs, asms, slot, fail)


def record(args: argparse.Namespace, sha: str, store: Path, wt: Path, raw: Path, logs: Path, asms: list[str],
           slot: Slot, fail) -> int:
    """Everything the slot covers: the detached worktree, the recording build, instrumentation, the runs, the map."""
    timings: dict[str, float] = {}
    t0 = time.time()
    git("worktree", "add", "--detach", str(wt), sha)
    try:
        if args.overlay:
            shutil.copytree(REPO / "tools" / "impact", wt / "tools" / "impact", dirs_exist_ok=True,
                            ignore=shutil.ignore_patterns("bin", "obj"))
        targets = wt / "tools" / "impact" / "ImpactRecording.targets"
        if not targets.exists():
            return fail(f"{targets} does not exist at {sha[:12]} — pass --overlay for a tree older than the tooling")

        # The git-ignored GPL corpus some Unit tests measure (build-local fetches it the same way): without it those
        # tests are red for a reason that is not the tree's, and the recording would say so for the wrong cause.
        if not (wt / "tests/external/gnucobol/tests/testsuite.src").exists():
            if run(["pwsh", "-NoProfile", "-File", "scripts/fetch-gnucobol-tests.ps1"], logs / "fetch-corpus.log",
                   wt, slot, priority=args.priority) != 0:
                print("  ⚠ the GnuCOBOL corpus fetch failed — ExternalCorpusPopulationDriftTests will be red in the "
                      "Unit leg for that reason (their hits are still recorded)")
        rc = run(["dotnet", "build", "Cobol.Net.sln", "-c", "Debug", "-v", "quiet",
                  f"-p:CustomAfterMicrosoftCommonTargets={targets}"], logs / "build.log", wt, slot,
                 priority=args.priority)
        if rc != 0:
            return fail("the recording build failed")
        instr = raw.parent / "instrumenter"  # beside raw/ and logs/ in the recording's work directory
        rc = run(["dotnet", "build", str(wt / "tools/impact/ImpactInstrumenter"), "-c", "Release", "-o",
                  str(instr), "-v", "quiet"], logs / "instrumenter-build.log", wt, slot, priority=args.priority)
        if rc != 0:
            return fail("the instrumenter did not build")
        timings["build"] = round(time.time() - t0, 1)

        t1 = time.time()
        bins = [str(p) for p in sorted(wt.glob("src/*/bin/Debug/net10.0"))]
        bins += [str(wt / p / "bin/Debug/net10.0") for p in ASSEMBLIES.values()]
        table_path = raw / "probes.json"
        cmd = ["dotnet", str(instr / "ImpactInstrumenter.dll"), "--repo", str(wt), "--table", str(table_path)]
        for name in PROBED:
            cmd += ["--assembly", name]
        rc = run(cmd + bins, logs / "instrument.log", wt, slot, priority=args.priority)
        print((logs / "instrument.log").read_text(encoding="utf-8", errors="replace").rstrip())
        if rc != 0:
            return fail("instrumentation failed")
        table = json.loads(table_path.read_text(encoding="utf-8"))
        timings["instrument"] = round(time.time() - t1, 1)

        # SCRUBBED (§3.14.3): a stray gate handshake or `VSTestTestCaseFilter` would record a PART of an assembly.
        env = {**scrubbed_env(), "COBOLNET_IMPACT_DIR": str(raw),
               "COBOLNET_IMPACT_PROBES": str(len(table["entries"])), "COBOLNET_COMPILE_CACHE": "off",
               "DOTNET_CLI_UI_LANGUAGE": "en"}
        verdicts: dict[str, str] = {}
        listed: dict[str, list[str]] = {}
        for asm in asms:
            proj = wt / ASSEMBLIES[asm]
            try:
                listed[asm] = list(list_population(proj, cwd=wt, **slot.spawn_kwargs())[0].elements())
            except PopulationError as e:
                return fail(f"the {asm} population could not be listed: {e}")
            t2 = time.time()
            cmd = ["dotnet", "test", str(proj), "--no-build"]
            if args.filter:
                # ⛔ A trial filter is a claim about which tests ran (kb/Work PB708): every term must name one.
                guard = subprocess.run([sys.executable, str(REPO / "scripts" / "filter_population.py"), "--filter",
                                        args.filter, "--filtered", str(proj)], cwd=wt, **slot.spawn_kwargs())
                if guard.returncode not in (0, 3):
                    return fail(f"the trial filter does not name what it claims (filter_population.py rc="
                                f"{guard.returncode})")
                cmd += ["--filter", args.filter]
            run(cmd, logs / f"test-{asm}.log", wt, slot, env=env, priority=args.priority)
            timings[f"test-{asm}"] = round(time.time() - t2, 1)
            verdicts[asm] = verdict_line(logs / f"test-{asm}.log")
            print(f"  {asm}: {verdicts[asm]}  ({timings[f'test-{asm}']} s)")
            if args.filter:
                # A trial's population is what the filter selected, not the whole listing.
                listed[asm] = [n for n in listed[asm]]

        known = [f for f in git("ls-tree", "-r", "--name-only", sha).splitlines()
                 if f.endswith(".cs") and (f.startswith("src/") or f.startswith("tests/"))]
        t3 = time.time()
        m = merge(raw, table, sha, known, listed if not args.filter else {}, verdicts, timings, args.filter)
        timings["merge"] = round(time.time() - t3, 1)
        timings["total"] = round(time.time() - t0, 1)
        m["timings_s"] = timings
        problems = check(m)
        m["problems"] = problems
        out = store / (f"{sha}.partial.json.gz" if args.filter else f"{sha}.json.gz")
        with gzip.open(out, "wt", encoding="utf-8") as fh:
            json.dump(m, fh, separators=(",", ":"))
        print(f"  tests recorded: {len(m['tests'])} · probe entries: {len(m['entries'])} · "
              f"children: {m['children']}")
        print(f"  timings (s): {timings}")
        for p in problems:
            print(f"  ⛔ {p}")
        if problems:
            return fail(f"{len(problems)} watchdog problem(s); the map was written to {out} with them recorded")
        print(f"=== IMPACT MAP: RECORDED {out} ({len(m['tests'])} tests, {timings['total']} s) ===")
        return 0
    finally:
        if not args.keep:
            subprocess.run(["git", "worktree", "remove", "--force", str(wt)], cwd=REPO, capture_output=True)
            shutil.rmtree(wt, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
