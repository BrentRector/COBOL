#!/usr/bin/env python3
"""perf_baseline.py — THE performance baseline of WiseOwl COBOL, recorded and compared (kb/Work PB2117, kb/Work A6;
docs/rearchitecture/DESIGN-architecture-review.md §3 R0 and §4 item 5).

    python scripts/arch/perf_baseline.py --gate-run TestResults/build-local/<run>      # record a baseline
    python scripts/arch/perf_baseline.py --against docs/rearchitecture/evidence/perf-baseline/<sha>.md
    python scripts/arch/perf_baseline.py --against <old.md> --current <new.md>          # compare two records
    python scripts/arch/perf_baseline.py --self-test                                    # prove the compare can fail

WHAT IT MEASURES, each with the witness that proves the work ran (an unwitnessed number is never recorded):
  * compile throughput, WARM: tests/Cobol.Net.Benchmarks' CompileBenchmarks (BenchmarkDotNet, in-process): the
    largest NIST programs and the most copybook-heavy one, front end + bind alone and the whole compile;
  * compile throughput, COLD: a fresh Release `cobol` process per compile of the same programs (what a user waits for);
  * generated-program hot paths: GeneratedProgramBenchmarks (BenchmarkDotNet, in-process: the generated code and the
    runtime, no start-up) over tests/Cobol.Net.Benchmarks/Programs/ (MOVE-heavy loop, PERFORM dispatch, sequential and
    indexed file I/O), with allocated bytes per run;
  * the EXTERNAL comparison: the same programs as whole processes, ours (`dotnet prog.dll`) against GnuCOBOL 3.2
    (`cobc -x -O2`), both under WSL on the same kernel and the same native file system, each net of its own start-up
    floor (Programs/EMPTYRUN.cob), at 1x, 2x and 4x the program's LOOP-COUNT — the scaling curve, whose growth ratio
    names the complexity class independently of the machine. The legacy byte engine is NOT a baseline (kb/Work R69 §1);
  * the whole-population gate time, read from build-local.ps1's own verdict.json for each --gate-run given.

THE NOISE BAND (how a later wave reads a delta): every row keeps its samples. Its noise is the larger of the robust
within-session spread (1.4826 * MAD / median, the normal-equivalent relative standard deviation) and, when the record
holds two sessions, the between-session spread (|m1 - m2| / (m1 + m2)). A comparison flags a row when
|current / baseline - 1| exceeds max(5 %, 3 * sqrt(noise_base^2 + noise_current^2)); allocated bytes use a 2 % floor
(they are near-deterministic), and gate times a fixed 25 % (one run each, under whatever else the host was running).
A row slower than its band is a REGRESSION and a finding (DESIGN-architecture-review §4 item 5), never a footnote;
a baseline row the current run did not produce is a red too (a measurement that silently vanished).

⛔ No test asserts any of these numbers (owner 2026-09-25, NoWallClockAssertionDriftTests): the baseline is a RECORD,
and this script is the only thing that compares one.

Exit status: 0 recorded / no regression · 1 a regression or a missing row · 2 a measurement or witness failure ·
3 the two records were taken under different machine conditions (the table still prints).
"""
from __future__ import annotations

import argparse
import datetime
import glob
import json
import math
import os
import platform
import re
import shutil
import statistics
import subprocess
import sys
import tempfile
import time

try:
    import resource   # POSIX only: the child-CPU accounting of the WSL leg
except ImportError:
    resource = None   # Windows reads the child's CPU through GetProcessTimes instead (_windows_cpu)

ROOT =os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BENCH = os.path.join(ROOT, "tests", "Cobol.Net.Benchmarks")
PROGRAMS = os.path.join(BENCH, "Programs")
EVIDENCE = os.path.join(ROOT, "docs", "rearchitecture", "evidence", "perf-baseline")
SCRATCH = os.path.join(ROOT, "TestResults", "perf-baseline")
FLOOR_PROGRAM = "EMPTYRUN"
SCALES = (1, 2, 4)
NOISE_FLOOR, ALLOC_FLOOR, GATE_BAND, K_SIGMA = 0.05, 0.02, 0.25, 3.0
WIDE_NOISE = 0.15   # a row noisier than this is named in the record as one whose band only catches a large change
RUN_LIMIT_S = 900   # one timed process; the slowest row (our IDXHOT at 4x) takes about 11 s
DATA_MARK = "<!-- perf-baseline-data: the block below is what perf_baseline.py --against reads -->"
LOOP_COUNT = re.compile(r"(\b01 LOOP-COUNT\s+PIC 9\(9\) COMP VALUE )(\d+)(\.)")


class MeasurementError(Exception):
    """A measurement could not be taken or its witness failed: nothing is recorded (exit 2)."""


# ── small helpers ─────────────────────────────────────────────────────────────────────────────────────────────────

def bench_params(cs_file: str) -> list[str]:
    """The `[Params(...)]` list of a benchmark class, read from its source so the population this script expects is
    the one the C# declares (one list, one place)."""
    text = open(os.path.join(BENCH, cs_file), encoding="utf-8").read()
    m = re.search(r"\[Params\(([^)]*)\)\]", text)
    if not m:
        raise MeasurementError(f"no [Params(...)] in {cs_file}")
    return re.findall(r'"([^"]+)"', m.group(1))


def witnesses() -> dict[str, str]:
    out = {}
    for line in open(os.path.join(PROGRAMS, "witnesses.tsv"), encoding="utf-8"):
        line = line.rstrip("\r\n")
        if line and not line.startswith("#"):
            name, text = line.split("\t", 1)
            out[name] = text
    return out


def robust_noise(samples: list[float]) -> float:
    """1.4826 * MAD / median: the relative spread a normal sample would have, robust to the odd preempted run."""
    if len(samples) < 2:
        return 0.0
    med = statistics.median(samples)
    if med == 0:
        return 0.0
    mad = statistics.median(abs(s - med) for s in samples)
    return 1.4826 * mad / abs(med)


def run_cmd(argv: list[str], cwd: str | None = None, check: bool = True) -> subprocess.CompletedProcess:
    r = subprocess.run(argv, cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and r.returncode != 0:
        raise MeasurementError(f"{' '.join(argv)} (cwd {cwd}) exited {r.returncode}:\n{r.stdout[-2000:]}\n{r.stderr[-2000:]}")
    return r


def timed(argv: list[str], cwd: str) -> tuple[float, float | None, str]:
    """Run one process to completion: (wall seconds, CPU seconds of the child or None, stdout). A non-zero exit is a
    measurement failure, never a fast run, and so is a run past RUN_LIMIT_S (a hang is not a slow measurement: the
    witness never printed)."""
    before = resource.getrusage(resource.RUSAGE_CHILDREN) if resource else None   # the waited-for children's CPU
    t0 = time.perf_counter()
    p = subprocess.Popen(argv, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, errors="replace")
    try:
        out, err = p.communicate(timeout=RUN_LIMIT_S)
    except subprocess.TimeoutExpired:
        p.kill()
        p.communicate()
        raise MeasurementError(f"{' '.join(argv)} (cwd {cwd}) ran past {RUN_LIMIT_S} s and was killed")
    wall = time.perf_counter() - t0
    if resource:
        after = resource.getrusage(resource.RUSAGE_CHILDREN)
        cpu =(after.ru_utime - before.ru_utime) + (after.ru_stime - before.ru_stime)
    else:
        cpu = _windows_cpu(p)
    if p.returncode != 0:
        raise MeasurementError(f"{' '.join(argv)} (cwd {cwd}) exited {p.returncode}: {err[-1500:]}")
    return wall, cpu, out


def _windows_cpu(p: subprocess.Popen) -> float | None:
    """The finished child's user + kernel time (GetProcessTimes on the handle Popen still holds)."""
    try:
        import ctypes
        from ctypes import wintypes
        c, e, k, u = (wintypes.FILETIME() for _ in range(4))
        handle = int(p._handle)  # noqa: SLF001 — the only way to the process handle Popen keeps open
        if not ctypes.windll.kernel32.GetProcessTimes(handle, ctypes.byref(c), ctypes.byref(e), ctypes.byref(k), ctypes.byref(u)):
            return None
        to_s = lambda ft: ((ft.dwHighDateTime << 32) | ft.dwLowDateTime) / 1e7
        return to_s(k) + to_s(u)
    except (AttributeError, OSError, ValueError):
        return None


def row(samples: list[float], unit: str, kind: str = "time", cpu: list[float] | None = None, **extra) -> dict:
    r = {"unit": unit, "kind": kind, "samples": [round(s, 6) for s in samples],
         "median": round(statistics.median(samples), 6), "noise": round(robust_noise(samples), 4)}
    if cpu:
        r["cpu_median"] = round(statistics.median(cpu), 6)
    r.update(extra)
    return r


def to_wsl_path(path: str) -> str:
    path = os.path.abspath(path)
    drive, rest = os.path.splitdrive(path)
    return "/mnt/" + drive.rstrip(":").lower() + rest.replace("\\", "/")


def scaled_source(name: str, factor: int) -> tuple[str, int]:
    text = open(os.path.join(PROGRAMS, name + ".cob"), encoding="utf-8").read()
    matches = LOOP_COUNT.findall(text)
    if name == FLOOR_PROGRAM:
        return text, 0
    if len(matches) != 1:
        raise MeasurementError(f"{name}.cob: expected exactly one '01 LOOP-COUNT PIC 9(9) COMP VALUE n.'")
    n = int(matches[0][1]) * factor
    return LOOP_COUNT.sub(lambda m: f"{m.group(1)}{n}{m.group(3)}", text), n


# ── conditions ────────────────────────────────────────────────────────────────────────────────────────────────────

def _powershell(expr: str) -> str:
    try:
        return run_cmd(["powershell", "-NoProfile", "-Command", expr], check=False).stdout.strip()
    except OSError:
        return ""


def conditions() -> dict:
    git = lambda *a: run_cmd(["git", "-C", ROOT, *a], check=False).stdout.strip()
    runtimes = run_cmd(["dotnet", "--list-runtimes"], check=False).stdout
    netcore = re.findall(r"Microsoft\.NETCore\.App (\S+)", runtimes)
    c = {
        "head": git("rev-parse", "HEAD"),
        "product_commit": git("log", "-1", "--format=%H", "--", "src"),
        "src_tree": git("rev-parse", "HEAD:src"),
        "dirty_src": bool(git("status", "--porcelain", "--", "src")),
        "taken_at": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
        "host": platform.node(),
        "os": platform.platform(),
        "logical_cpus": os.cpu_count(),
        "python": platform.python_version(),
        "dotnet_sdk": run_cmd(["dotnet", "--version"], check=False).stdout.strip(),
        "netcore_runtimes": netcore,
    }
    if os.name == "nt":
        c["cpu"] = _powershell("(Get-CimInstance Win32_Processor | Select-Object -First 1).Name")
        ram = _powershell("(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory")
        c["ram_gb"] = round(int(ram) / 2**30) if ram.isdigit() else None
    else:
        m = re.search(r"model name\s*:\s*(.*)", open("/proc/cpuinfo").read())
        c["cpu"] = m.group(1).strip() if m else platform.processor()
    return c


def load_sample() -> dict:
    """What else the host was doing: CPU load and the number of other .NET and test-host processes (other agents'
    gates share this machine; a baseline taken under a concurrent gate is a different condition, and says so)."""
    if os.name != "nt":
        return {"loadavg": os.getloadavg()[0]}
    load = _powershell("(Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average")
    procs = _powershell("@(Get-Process -Name dotnet,testhost,cobol -ErrorAction SilentlyContinue).Count")
    return {"cpu_load_pct": float(load) if re.fullmatch(r"[\d.]+", load or "") else None,
            "dotnet_processes": int(procs) if procs.isdigit() else None}


# ── the measurements ──────────────────────────────────────────────────────────────────────────────────────────────

def release_exe(project_dir: str, name: str) -> str:
    exe = os.path.join(project_dir, "bin", "Release", "net10.0", name + (".exe" if os.name == "nt" else ""))
    if not os.path.exists(exe):
        raise MeasurementError(f"{exe} is missing: build Cobol.Net.sln in Release first (or drop --no-build)")
    return exe


def measure_bdn(work: str) -> dict:
    """Run the two PB2117 benchmark classes and read BenchmarkDotNet's full JSON report. The witness: each class's
    GlobalSetup checks its own output, and the case population here must be exactly the classes' [Params] x methods."""
    compile_programs = bench_params(os.path.join("Compile", "CompileBenchmarks.cs"))
    hot = bench_params(os.path.join("GeneratedCode", "GeneratedProgramBenchmarks.cs"))
    expected = {f"compile/{p}/{m}" for p in compile_programs for m in ("FrontEndAndBind", "FullCompile")} \
        | {f"run/{p}" for p in hot}
    artifacts = os.path.join(work, "bdn")
    exe = release_exe(BENCH, "Cobol.Net.Benchmarks")
    log = os.path.join(work, "bdn.log")
    with open(log, "w", encoding="utf-8") as fh:
        rc = subprocess.run([exe, "--filter", "*CompileBenchmarks*", "*GeneratedProgramBenchmarks*",
                             "--exporters", "json", "--artifacts", artifacts], cwd=ROOT, stdout=fh,
                            stderr=subprocess.STDOUT).returncode
    if rc != 0:
        raise MeasurementError(f"BenchmarkDotNet exited {rc}; see {log}")
    rows: dict[str, dict] = {}
    for report in glob.glob(os.path.join(artifacts, "results", "*-report-full*.json")):
        data = json.load(open(report, encoding="utf-8"))
        for b in data.get("Benchmarks", []):
            params = dict(re.findall(r"(\w+)=([^,&\s]+)", b.get("Parameters", "")))
            program = params.get("Program", "?")
            key = f"compile/{program}/{b['Method']}" if "CompileBenchmarks" in b.get("Type", "") else f"run/{program}"
            actual = [m["Nanoseconds"] / m["Operations"] / 1e6 for m in b.get("Measurements", [])
                      if m.get("IterationMode") == "Workload" and m.get("IterationStage") == "Actual"]
            if not actual:
                raise MeasurementError(f"BenchmarkDotNet case {key} has no measurements (it failed); see {log}")
            alloc = (b.get("Memory") or {}).get("BytesAllocatedPerOperation")
            rows["bdn/" + key] = row(actual, "ms")
            if alloc is not None:
                rows["bdn-alloc/" + key] = row([float(alloc)], "bytes", kind="alloc")
    got = {k[len("bdn/"):] for k in rows if k.startswith("bdn/")}
    if got != expected:
        raise MeasurementError(f"BenchmarkDotNet population: expected {sorted(expected)}, got {sorted(got)} (see {log})")
    return rows


def measure_cold_compile(work: str, repeat: int) -> dict:
    """A fresh Release `cobol` process per compile, exactly as the NIST harness compiles (--nist: COBOL 85 and the
    CCVS preprocessing); the witness is exit 0 and a written assembly."""
    cobol = release_exe(os.path.join(ROOT, "src", "Cobol.Net.Cli"), "cobol")
    rows = {}
    for p in bench_params(os.path.join("Compile", "CompileBenchmarks.cs")):
        src = os.path.join(ROOT, "tests", "nist", "programs", p + ".cob")
        lines = sum(1 for _ in open(src, encoding="latin-1"))
        out_dir = os.path.join(work, "cold", p)
        os.makedirs(out_dir, exist_ok=True)
        dll = os.path.join(out_dir, p + ".dll")
        walls, cpus = [], []
        for _ in range(repeat):
            if os.path.exists(dll):
                os.remove(dll)
            wall, cpu, _ = timed([cobol, src, "--nist", p, "-o", dll], cwd=out_dir)
            if not os.path.exists(dll):
                raise MeasurementError(f"cold compile of {p} wrote no assembly")
            walls.append(wall)
            if cpu is not None:
                cpus.append(cpu)
        rows[f"cold-compile/{p}"] = row(walls, "s", cpu=cpus, lines=lines)
    return rows


def stage_programs(work: str) -> str:
    """Compile every hot-path program at every scale with the Release compiler into <work>/stage/<P>@x<k>/, the
    directory both the Windows and the WSL runs use (the same assembly, so the two OSes measure one compile)."""
    cobol = release_exe(os.path.join(ROOT, "src", "Cobol.Net.Cli"), "cobol")
    stage = os.path.join(work, "stage")
    hot = bench_params(os.path.join("GeneratedCode", "GeneratedProgramBenchmarks.cs"))
    for name in [FLOOR_PROGRAM, *hot]:
        for k in (SCALES if name != FLOOR_PROGRAM else (1,)):
            case = os.path.join(stage, f"{name}@x{k}")
            os.makedirs(case, exist_ok=True)
            text, n = scaled_source(name, k)
            with open(os.path.join(case, name + ".cob"), "w", encoding="utf-8", newline="\n") as fh:
                fh.write(text)
            with open(os.path.join(case, "LOOP-COUNT"), "w") as fh:
                fh.write(str(n))
            run_cmd([cobol, name + ".cob"], cwd=case)
    shutil.copy(os.path.join(PROGRAMS, "witnesses.tsv"), stage)
    return stage


def measure_windows_processes(stage: str, repeat: int) -> dict:
    """Ours as whole processes on the host OS (`dotnet prog.dll`), scale 1 only: start-up included, so the EMPTYRUN
    row is the floor to read the others against."""
    wit = witnesses()
    rows = {}
    for staged in sorted(glob.glob(os.path.join(stage, "*@x1"))):
        name = os.path.basename(staged).split("@")[0]
        # Run in a COPY: the stage must hold only what the WSL leg copies (a data file written here is ours, and
        # GnuCOBOL's indexed handler opening our idxhot.dat spun at 100 % CPU instead of replacing it).
        case = os.path.join(os.path.dirname(stage), "windows", os.path.basename(staged))
        shutil.copytree(staged, case)
        walls, cpus = [], []
        for _ in range(repeat):
            wall, cpu, out = timed(["dotnet", name + ".dll"], cwd=case)
            if out.rstrip("\r\n") != wit[name]:
                raise MeasurementError(f"windows {name}: witness mismatch [{out.strip()}] != [{wit[name]}]")
            walls.append(wall)
            if cpu is not None:
                cpus.append(cpu)
        rows[f"process-windows/ours/{name}"] = row(walls, "s", cpu=cpus)
    return rows


def measure_wsl(stage: str, repeat: int, distro: str) -> tuple[dict, dict]:
    # A login shell, so the user's PATH (where `dotnet` lives) is in effect; the arguments travel as "$@", unquoted
    # by nothing.
    argv = ["wsl", "-d", distro, "-e", "bash", "-lc", 'exec python3 "$0" "$@"', to_wsl_path(os.path.abspath(__file__)),
            "--wsl-leg", to_wsl_path(stage), "--repeat", str(repeat)]
    r = subprocess.run(argv, capture_output=True, text=True, encoding="utf-8", errors="replace")
    payload = [l for l in r.stdout.splitlines() if l.startswith("PERF-WSL-JSON: ")]
    if r.returncode != 0 or not payload:
        raise MeasurementError(f"the WSL leg failed ({r.returncode}):\n{r.stdout[-3000:]}\n{r.stderr[-3000:]}")
    data = json.loads(payload[-1][len("PERF-WSL-JSON: "):])
    return data["rows"], data["environment"]


def wsl_leg(stage_mnt: str, repeat: int) -> int:
    """Runs INSIDE WSL: copy the stage to the Linux file system (so file I/O is measured on a native file system, not
    through the Windows mount), compile each program with GnuCOBOL, and time both implementations, interleaved."""
    native = os.path.join(os.path.expanduser("~"), ".cache", "pb2117-perf", os.path.basename(os.path.dirname(stage_mnt)))
    shutil.rmtree(native, ignore_errors=True)
    shutil.copytree(stage_mnt, native, ignore=shutil.ignore_patterns("*.dat"))   # inputs only, never a data file
    wit = {}
    for line in open(os.path.join(native, "witnesses.tsv"), encoding="utf-8"):
        if line.strip() and not line.startswith("#"):
            n, t = line.rstrip("\n").split("\t", 1)
            wit[n] = t
    env = {"uname": " ".join(platform.uname()[:3]),
           "cobc": run_cmd(["cobc", "--version"]).stdout.splitlines()[0],
           "cobc_flags": "-x -O2",
           "dotnet_runtime": run_cmd(["dotnet", "--list-runtimes"]).stdout.strip().splitlines()}
    m = re.search(r"model name\s*:\s*(.*)", open("/proc/cpuinfo").read())
    env["cpu"] = m.group(1).strip() if m else ""
    env["loadavg_before"] = round(os.getloadavg()[0], 2)
    rows = {}
    try:
        for case in sorted(glob.glob(os.path.join(native, "*@x*"))):
            name, scale = os.path.basename(case).split("@")
            run_cmd(["cobc", "-x", "-O2", "-o", name, name + ".cob"], cwd=case)
            inputs = set(os.listdir(case))
            n = int(open(os.path.join(case, "LOOP-COUNT")).read())
            samples = {"gnucobol": ([], []), "ours": ([], [])}
            outputs = {}
            # Scaled runs are slow on purpose (that is the curve); one fewer repetition keeps the leg bounded.
            reps = repeat if scale == "x1" else max(3, repeat - 2)
            for _ in range(reps):
                for impl, argv in (("gnucobol", ["./" + name]), ("ours", ["dotnet", name + ".dll"])):
                    # Every run starts from no data file: the two implementations' file formats differ, and
                    # GnuCOBOL's indexed handler spins at 100 % CPU on OPEN OUTPUT over a file it did not write.
                    for leftover in set(os.listdir(case)) - inputs:
                        os.remove(os.path.join(case, leftover))
                    wall, cpu, out = timed(argv, cwd=case)
                    outputs.setdefault(impl, out.rstrip("\n"))
                    if out.rstrip("\n") != outputs[impl]:
                        raise MeasurementError(f"{impl} {name}@{scale}: output changed between runs")
                    samples[impl][0].append(wall)
                    samples[impl][1].append(cpu)
            # The witness: at scale 1 both print the computed line; at every scale the two agree.
            if scale == "x1" and outputs["ours"] != wit[name]:
                raise MeasurementError(f"ours {name}: [{outputs['ours']}] != witness [{wit[name]}]")
            if outputs["gnucobol"] != outputs["ours"]:
                raise MeasurementError(f"{name}@{scale}: GnuCOBOL printed [{outputs['gnucobol']}], ours [{outputs['ours']}]")
            for impl, (walls, cpus) in samples.items():
                rows[f"process-wsl/{impl}/{name}@{scale}"] = row(walls, "s", cpu=cpus, loop_count=n)
    except MeasurementError as e:
        print(f"WSL LEG FAILED: {e}")
        return 2
    finally:
        shutil.rmtree(native, ignore_errors=True)
    env["loadavg_after"] = round(os.getloadavg()[0], 2)
    print("PERF-WSL-JSON: " + json.dumps({"rows": rows, "environment": env}))
    return 0


def gate_rows(run_dirs: list[str]) -> tuple[dict, list[dict]]:
    """The whole-population gate time, from build-local's own verdict.json. Only a GREEN gate is a whole-population
    time (a RED one stopped early)."""
    rows, runs = {}, []
    for d in run_dirs:
        path = os.path.join(d, "verdict.json")
        v = json.load(open(path, encoding="utf-8"))
        if v.get("verdict") != "GREEN":
            raise MeasurementError(f"{path} is {v.get('verdict')}: only a GREEN gate ran the whole population")
        mode, t = v["mode"], v["timings"]
        # The gate's wall time includes its wait for a gate slot (other worktrees' gates), which measures the queue,
        # not the gate: the compared row is the time the gate itself ran.
        waited = re.search(r"waited ([0-9.]+) s", v.get("slot") or "")
        rows[f"gate/{mode}/ran_s"] = row([float(t["wall_s"]) - (float(waited.group(1)) if waited else 0.0)], "s",
                                         kind="gate")
        for part in ("build_s", "legs_s"):
            rows[f"gate/{mode}/{part}"] = row([float(t[part])], "s", kind="gate")
        runs.append({"run": os.path.basename(os.path.normpath(d)), "mode": mode, "line": v.get("line"),
                     "slot": v.get("slot"), "populations": {k: p.get("ran") for k, p in v.get("populations", {}).items()},
                     "assembly_wall_s": {r["asm"]: r["wall_s"] for r in v.get("runs", [])}})
    return rows, runs


def measure_session(args, work: str) -> tuple[dict, dict]:
    rows, env = {}, {}
    print("  benchmarks (BenchmarkDotNet) ...", flush=True)
    rows.update(measure_bdn(work))
    print("  cold compiles ...", flush=True)
    rows.update(measure_cold_compile(work, args.repeat))
    print("  staging the hot-path programs ...", flush=True)
    stage = stage_programs(work)
    print("  whole processes on Windows ...", flush=True)
    rows.update(measure_windows_processes(stage, args.repeat))
    if not args.skip_wsl:
        print("  GnuCOBOL and ours under WSL ...", flush=True)
        wsl_rows, env = measure_wsl(stage, args.repeat, args.distro)
        rows.update(wsl_rows)
    return rows, env


def merge_sessions(sessions: list[dict]) -> dict:
    """One row per key from S sessions: all samples pooled, and the noise widened to the between-session spread."""
    merged = {}
    for key in sorted(set().union(*sessions)):   # every key of EVERY session: a row one session lacks is a failure
        parts = [s[key] for s in sessions if key in s]
        if len(parts) != len(sessions):
            raise MeasurementError(f"row {key} is missing from a session")
        samples = [x for p in parts for x in p["samples"]]
        r = dict(parts[0])
        r["samples"] = samples
        r["median"] = round(statistics.median(samples), 6)
        within = max(robust_noise(p["samples"]) for p in parts)
        meds = [p["median"] for p in parts]
        between = (max(meds) - min(meds)) / (max(meds) + min(meds)) if len(meds) > 1 and sum(meds) else 0.0
        r["noise"] = round(max(within, between), 4)
        r["session_medians"] = meds
        if all("cpu_median" in p for p in parts):
            r["cpu_median"] = round(statistics.median(p["cpu_median"] for p in parts), 6)
        merged[key] = r
    return merged


# ── the record ────────────────────────────────────────────────────────────────────────────────────────────────────

def fmt(v: float | None, unit: str) -> str:
    if v is None:
        return "—"
    if unit == "bytes":
        return f"{v / 2**20:,.1f} MiB" if v >= 2**20 else f"{v / 1024:,.1f} KiB"
    if unit == "s":
        return f"{v * 1000:,.0f} ms" if v < 10 else f"{v:,.1f} s"
    return f"{v:,.1f} {unit}"


def render(record: dict) -> str:
    c, rows = record["conditions"], record["rows"]
    get = lambda k: rows.get(k, {}).get("median")
    noise = lambda k: f"±{rows[k]['noise'] * 100:.1f} %" if k in rows else "—"
    out = [f"# Performance baseline — product commit {c['product_commit'][:12]}", "",
           "Recorded by `scripts/arch/perf_baseline.py` (kb/Work PB2117, the instrument of kb/Work A6; "
           "`docs/rearchitecture/DESIGN-architecture-review.md` §3 R0 and §4 item 5). Every R3 and R4 wave compares "
           "against it with `python scripts/arch/perf_baseline.py --against <this file>`: a row slower than its band "
           "is a REGRESSION, and a finding. This file is generated; regenerate it, never edit it.", "",
           "## Conditions", "",
           "| | |", "|---|---|"]
    for k in ("product_commit", "src_tree", "head", "dirty_src", "taken_at", "host", "cpu", "logical_cpus", "ram_gb",
              "os", "dotnet_sdk", "netcore_runtimes", "python"):
        out.append(f"| {k} | `{c.get(k)}` |")
    w = record.get("wsl") or {}
    for k in ("uname", "cpu", "cobc", "cobc_flags", "loadavg_before", "loadavg_after"):
        if k in w:
            out.append(f"| wsl {k} | `{w[k]}` |")
    out.append(f"| sessions | {record['sessions']} (repeat {record['repeat']} per process row; BenchmarkDotNet "
               "3 warm-up + 12 measured iterations per case) |")
    for i, l in enumerate(record.get("load", []), 1):
        out.append(f"| host load, session {i} | `{l}` |")
    out += ["", "## Noise band", "",
            "Each row's noise is the larger of its within-session robust spread (1.4826·MAD/median) and its "
            f"between-session spread over the {record['sessions']} session(s). `--against` flags a row when "
            f"|current/baseline − 1| > max({NOISE_FLOOR:.0%}, {K_SIGMA:g}·√(noise_base² + noise_current²)); allocated "
            f"bytes use a {ALLOC_FLOOR:.0%} floor, gate times a fixed {GATE_BAND:.0%}.", ""]
    wide = sorted(k for k, r in rows.items() if r["kind"] == "time" and r["noise"] > WIDE_NOISE)
    if wide:
        out += [f"⚠ **{len(wide)} row(s) carry more than {WIDE_NOISE:.0%} noise**, so their band is wide and only a "
                "large change in them is flagged: " + ", ".join(f"`{k}`" for k in wide) + ". Read the host-load rows "
                "above: a session taken while other worktrees' gates ran is slower and less repeatable. A re-baseline "
                "on a quiet host (no concurrent gate) narrows them; the whole-process rows are the load-robust ones.",
                ""]

    out += ["## Compile throughput", "",
            "Warm = BenchmarkDotNet in one process (JIT and the backend's reference-assembly cache warm); cold = a "
            "fresh Release `cobol --nist` process per compile.", "",
            "| program | lines | front end + bind (warm) | full compile (warm) | lines/s (warm, full) | allocated (full) "
            "| cold process | noise (warm full / cold) |", "|---|--:|--:|--:|--:|--:|--:|--:|"]
    for key in sorted(k for k in rows if k.startswith("cold-compile/")):
        p = key.split("/")[1]
        fe, full = get(f"bdn/compile/{p}/FrontEndAndBind"), get(f"bdn/compile/{p}/FullCompile")
        lines = rows[key].get("lines", 0)
        out.append(f"| {p} | {lines:,} | {fmt(fe, 'ms')} | {fmt(full, 'ms')} | "
                   f"{lines / (full / 1000):,.0f} | {fmt(get(f'bdn-alloc/compile/{p}/FullCompile'), 'bytes')} | "
                   f"{fmt(get(key), 's')} | {noise(f'bdn/compile/{p}/FullCompile')} / {noise(key)} |")

    out += ["", "## Generated-program hot paths (in-process, BenchmarkDotNet)", "",
            "| program | per run | allocated per run | noise |", "|---|--:|--:|--:|"]
    for key in sorted(k for k in rows if k.startswith("bdn/run/")):
        p = key.split("/")[2]
        out.append(f"| {p} | {fmt(get(key), 'ms')} | {fmt(get(f'bdn-alloc/run/{p}'), 'bytes')} | {noise(key)} |")

    if any(k.startswith("process-wsl/") for k in rows):
        out += ["", "## Against GnuCOBOL (whole processes under WSL, same kernel and file system)", "",
                f"Each time is the median wall time net of that implementation's `{FLOOR_PROGRAM}` start-up floor; "
                "ratio = ours / GnuCOBOL. Both print the same witness line at every scale.", "",
                "| program | LOOP-COUNT | ours (net) | GnuCOBOL (net) | ours / GnuCOBOL | ours CPU | GnuCOBOL CPU |",
                "|---|--:|--:|--:|--:|--:|--:|"]
        floor = {i: get(f"process-wsl/{i}/{FLOOR_PROGRAM}@x1") or 0.0 for i in ("ours", "gnucobol")}
        out.append(f"| {FLOOR_PROGRAM} (start-up floor) | — | {fmt(floor['ours'], 's')} | {fmt(floor['gnucobol'], 's')} "
                   f"| — | | |")
        for key in sorted(k for k in rows if k.startswith("process-wsl/ours/") and FLOOR_PROGRAM not in k):
            case = key.split("/")[2]
            o, g = get(key), get(f"process-wsl/gnucobol/{case}")
            on, gn = max(o - floor["ours"], 0.0), max(g - floor["gnucobol"], 0.0)
            ratio = f"{on / gn:,.1f}x" if gn > 0 else "—"
            out.append(f"| {case} | {rows[key].get('loop_count', ''):,} | {fmt(on, 's')} | {fmt(gn, 's')} | {ratio} | "
                       f"{fmt(rows[key].get('cpu_median'), 's')} | "
                       f"{fmt(rows[f'process-wsl/gnucobol/{case}'].get('cpu_median'), 's')} |")
        out += ["", "### Scaling curve (growth when LOOP-COUNT doubles; 2.0 = linear, 4.0 = quadratic)", "",
                "| program | ours x1→x2 | ours x2→x4 | GnuCOBOL x1→x2 | GnuCOBOL x2→x4 |", "|---|--:|--:|--:|--:|"]
        names = sorted({k.split("/")[2].split("@")[0] for k in rows if k.startswith("process-wsl/ours/")} - {FLOOR_PROGRAM})
        for p in names:
            cells = []
            for impl in ("ours", "gnucobol"):
                t = [max((get(f"process-wsl/{impl}/{p}@x{k}") or 0) - floor[impl], 1e-9) for k in SCALES]
                cells += [f"{t[1] / t[0]:.2f}", f"{t[2] / t[1]:.2f}"]
            out.append(f"| {p} | " + " | ".join(cells) + " |")

    if any(k.startswith("process-windows/") for k in rows):
        out += ["", "## Ours as whole processes on the host OS (start-up included)", "",
                "| program | wall | CPU | noise |", "|---|--:|--:|--:|"]
        for key in sorted(k for k in rows if k.startswith("process-windows/")):
            out.append(f"| {key.split('/')[2]} | {fmt(get(key), 's')} | {fmt(rows[key].get('cpu_median'), 's')} | "
                       f"{noise(key)} |")

    out += ["", "## Whole-population gate time", ""]
    if record.get("gates"):
        out += ["| run | mode | ran (wall less the slot wait) | build | legs | populations | slot |",
                "|---|---|--:|--:|--:|---|---|"]
        for g in record["gates"]:
            m = g["mode"]
            out.append(f"| `{g['run']}` | {m} | {fmt(get(f'gate/{m}/ran_s'), 's')} | {fmt(get(f'gate/{m}/build_s'), 's')} "
                       f"| {fmt(get(f'gate/{m}/legs_s'), 's')} | {g['populations']} | `{g['slot']}` |")
        out += ["", "Verdict lines:", ""] + [f"- `{g['line']}`" for g in record["gates"]]
    else:
        out.append("Not recorded: no `--gate-run` was given.")
    out += ["", "## Machine-readable record", "", DATA_MARK, "", "```json",
            json.dumps(record, indent=1, sort_keys=True), "```", ""]
    return "\n".join(out)


def read_record(path: str) -> dict:
    text = open(path, encoding="utf-8").read()
    at = text.find(DATA_MARK)
    m = re.search(r"```json\n(.*?)\n```", text[at:], re.S) if at >= 0 else None
    if not m:
        raise SystemExit(f"{path}: no machine-readable record (the '{DATA_MARK}' block)")
    return json.loads(m.group(1))


# ── the comparison ────────────────────────────────────────────────────────────────────────────────────────────────

def compare(base: dict, cur: dict) -> int:
    differ = [k for k in ("host", "cpu", "logical_cpus", "os", "netcore_runtimes")
              if base["conditions"].get(k) != cur["conditions"].get(k)]
    bw, cw = base.get("wsl") or {}, cur.get("wsl") or {}
    differ += [f"wsl {k}" for k in ("cpu", "cobc") if bw.get(k) != cw.get(k) and bw and cw]
    print(f"baseline: product {base['conditions']['product_commit'][:12]} (src tree {base['conditions']['src_tree'][:12]}), "
          f"taken {base['conditions']['taken_at']}")
    print(f"current:  product {cur['conditions']['product_commit'][:12]} (src tree {cur['conditions']['src_tree'][:12]}), "
          f"taken {cur['conditions']['taken_at']}")
    if base["conditions"]["src_tree"] == cur["conditions"]["src_tree"]:
        print("the product is IDENTICAL in both records: every delta below is noise, and none should leave its band")
    regress, missing = [], []
    print(f"\n{'row':58} {'baseline':>12} {'current':>12} {'delta':>8} {'band':>7}  verdict")
    for key in sorted(base["rows"]):
        b = base["rows"][key]
        c = cur["rows"].get(key)
        if c is None:
            missing.append(key)
            print(f"{key:58} {fmt(b['median'], b['unit']):>12} {'—':>12} {'':>8} {'':>7}  MISSING")
            continue
        if b["kind"] == "gate":
            band = GATE_BAND
        else:
            band = max(ALLOC_FLOOR if b["kind"] == "alloc" else NOISE_FLOOR,
                       K_SIGMA * math.sqrt(b["noise"] ** 2 + c["noise"] ** 2))
        delta = c["median"] / b["median"] - 1 if b["median"] else 0.0
        verdict = "REGRESSION" if delta > band else "IMPROVED" if delta < -band else "same"
        if verdict == "REGRESSION":
            regress.append(key)
        print(f"{key:58} {fmt(b['median'], b['unit']):>12} {fmt(c['median'], c['unit']):>12} {delta:>+8.1%} {band:>7.1%}  {verdict}")
    for key in sorted(set(cur["rows"]) - set(base["rows"])):
        print(f"{key:58} {'—':>12} {fmt(cur['rows'][key]['median'], cur['rows'][key]['unit']):>12} {'':>8} {'':>7}  new")
    if differ:
        print(f"\n⚠ NOT COMPARABLE CONDITIONS: {', '.join(differ)} differ between the records")
    if regress or missing:
        print(f"\n=== PERF-BASELINE COMPARE: REGRESSION — {len(regress)} row(s) beyond the band, {len(missing)} missing: "
              f"{', '.join(regress + missing)} (each is a finding: DESIGN-architecture-review §4 item 5) ===")
        return 1
    print(f"\n=== PERF-BASELINE COMPARE: WITHIN THE BAND — {len(base['rows'])} row(s) compared"
          f"{' (conditions differ)' if differ else ''} ===")
    return 3 if differ else 0


# ── the self-test (PerfBaselineSelfTestDriftTests runs it every gate) ─────────────────────────────────────────────

def self_test() -> int:
    """Prove the comparison can FAIL — on a planted regression, a vanished row, an allocation change and a machine
    change — and stays silent inside the band; that the record survives its own round trip; and that the hot-path
    programs, their witnesses and the benchmark's [Params] are one set. Each case prints its name."""
    import contextlib
    import copy
    import io

    def record() -> dict:
        cond = {"product_commit": "a" * 40, "src_tree": "b" * 40, "head": "c" * 40, "dirty_src": False,
                "taken_at": "2026-10-07 00:00 UTC", "host": "H", "cpu": "C", "logical_cpus": 32, "ram_gb": 64,
                "os": "O", "dotnet_sdk": "10", "netcore_runtimes": ["10.0.12"], "python": "3"}
        rows = {"bdn/compile/IX113A/FrontEndAndBind": row([60.0, 61.0, 59.0], "ms"),
                "bdn/compile/IX113A/FullCompile": row([460.0, 470.0, 450.0], "ms"),
                "bdn-alloc/compile/IX113A/FullCompile": row([1.7e8], "bytes", kind="alloc"),
                "cold-compile/IX113A": row([1.9, 2.0, 2.1], "s", cpu=[3.0], lines=5816),
                "bdn/run/MOVEHOT": row([700.0, 702.0, 698.0], "ms"),
                "bdn-alloc/run/MOVEHOT": row([1.8e9], "bytes", kind="alloc"),
                "gate/implementer/ran_s": row([400.0], "s", kind="gate")}
        return {"schema": 1, "conditions": cond, "wsl": {}, "sessions": 2, "repeat": 5, "load": [], "rows": rows,
                "gates": [], "command": "self-test"}

    def quiet_compare(base: dict, cur: dict) -> int:
        with contextlib.redirect_stdout(io.StringIO()):
            return compare(base, cur)

    failures = []

    def case(name: str, ok: bool) -> None:
        print(f"  {'ok  ' if ok else 'FAIL'} {name}")
        if not ok:
            failures.append(name)

    base = record()
    case("silent on identical records", quiet_compare(base, copy.deepcopy(base)) == 0)
    cur = copy.deepcopy(base)
    cur["rows"]["bdn/run/MOVEHOT"]["median"] *= 1.03
    case("silent within the band (+3 %, under the 5 % floor)", quiet_compare(base, cur) == 0)
    cur = copy.deepcopy(base)
    cur["rows"]["bdn/run/MOVEHOT"]["median"] *= 1.5
    case("fires REGRESSION on a planted +50 % time", quiet_compare(base, cur) == 1)
    cur = copy.deepcopy(base)
    cur["rows"]["bdn-alloc/run/MOVEHOT"]["median"] *= 1.05
    case("fires ALLOC on +5 % allocated bytes", quiet_compare(base, cur) == 1)
    cur = copy.deepcopy(base)
    del cur["rows"]["cold-compile/IX113A"]
    case("fires MISSING on a vanished row", quiet_compare(base, cur) == 1)
    cur = copy.deepcopy(base)
    cur["conditions"]["cpu"] = "another machine"
    case("flags CONDITIONS on a different CPU", quiet_compare(base, cur) == 3)
    with tempfile.TemporaryDirectory() as tmp:
        path = os.path.join(tmp, "r.md")
        with open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(render(base))
        case("round-trips the record", read_record(path) == json.loads(json.dumps(base)))
    hot = bench_params(os.path.join("GeneratedCode", "GeneratedProgramBenchmarks.cs"))
    wit = witnesses()
    agree = set(wit) == set(hot) | {FLOOR_PROGRAM} and all(
        os.path.exists(os.path.join(PROGRAMS, p + ".cob")) for p in wit)
    try:
        agree = agree and all(scaled_source(p, 2)[1] > 0 for p in hot)
    except MeasurementError:
        agree = False
    case("programs agree (Programs/*.cob, witnesses.tsv, [Params], one LOOP-COUNT each)", agree)
    print(f"SELF-TEST: {'PASS' if not failures else 'FAIL — ' + ', '.join(failures)}")
    return 0 if not failures else 1


# ── main ──────────────────────────────────────────────────────────────────────────────────────────────────────────

def main(argv: list[str]) -> int:
    # The tables carry non-ASCII (§, —, ±); a Windows console's code page would otherwise raise on the first one.
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0],
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--against", metavar="RECORD", help="compare a fresh measurement (or --current) with this record")
    ap.add_argument("--current", metavar="RECORD", help="with --against: compare this record instead of measuring")
    ap.add_argument("--gate-run", action="append", default=[], metavar="DIR",
                    help="a GREEN TestResults/build-local/<run> of this commit (repeatable: one per mode)")
    ap.add_argument("--sessions", type=int, default=None,
                    help="measurement sessions pooled into the record (default 2 for a baseline, 1 with --against)")
    ap.add_argument("--repeat", type=int, default=5, help="runs per whole-process row (default 5)")
    ap.add_argument("--out", metavar="FILE", help="where to write the record (default: the evidence directory for a "
                    "baseline, TestResults/perf-baseline/ with --against)")
    ap.add_argument("--no-build", action="store_true", help="measure the existing Release build")
    ap.add_argument("--skip-wsl", action="store_true", help="leave out the GnuCOBOL comparison (recorded as absent)")
    ap.add_argument("--distro", default="Ubuntu", help="the WSL distribution holding cobc and dotnet (default Ubuntu)")
    ap.add_argument("--wsl-leg", metavar="STAGE", help=argparse.SUPPRESS)
    ap.add_argument("--self-test", action="store_true", help="prove the comparison can fail (no measurement)")
    args = ap.parse_args(argv)

    if args.self_test:
        return self_test()
    if args.wsl_leg:
        return wsl_leg(args.wsl_leg, args.repeat)
    if args.current and not args.against:
        ap.error("--current needs --against")
    if args.against and args.current:
        return compare(read_record(args.against), read_record(args.current))

    sessions = args.sessions or (1 if args.against else 2)
    try:
        if not args.no_build:
            print("building Cobol.Net.sln (Release) ...", flush=True)
            run_cmd(["dotnet", "build", os.path.join(ROOT, "Cobol.Net.sln"), "-c", "Release", "-nologo", "-v:q"], cwd=ROOT)
        cond = conditions()
        if cond["dirty_src"]:
            raise MeasurementError("src/ has uncommitted changes: a baseline must name the product it measured")
        stamp = datetime.datetime.now().strftime("%Y%m%dT%H%M%S")
        all_rows, loads, wsl_env = [], [], {}
        for s in range(sessions):
            work = os.path.join(SCRATCH, f"{stamp}-s{s + 1}")
            os.makedirs(work, exist_ok=True)
            print(f"session {s + 1} of {sessions} ({work})", flush=True)
            before = load_sample()
            rows, env = measure_session(args, work)
            loads.append({"before": before, "after": load_sample()})
            wsl_env = env or wsl_env
            all_rows.append(rows)
        gates, gate_info = gate_rows(args.gate_run)
        record = {"schema": 1, "conditions": cond, "wsl": wsl_env, "sessions": sessions, "repeat": args.repeat,
                  "load": loads, "rows": {**merge_sessions(all_rows), **gates}, "gates": gate_info,
                  "command": "python scripts/arch/perf_baseline.py " + " ".join(argv)}
    except MeasurementError as e:
        print(f"\n=== PERF-BASELINE: MEASUREMENT FAILED — {e} ===")
        return 2

    out = args.out or (os.path.join(SCRATCH, f"{stamp}.md") if args.against
                       else os.path.join(EVIDENCE, f"{cond['product_commit'][:12]}.md"))
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    # The raw measurement first, so nothing a long run measured is lost to a rendering fault.
    with open(os.path.join(SCRATCH, f"{stamp}.json"), "w", encoding="utf-8") as fh:
        json.dump(record, fh, indent=1, sort_keys=True)
    with open(out, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(render(record))
    print(f"record written: {out}")
    if args.against:
        return compare(read_record(args.against), record)
    print(f"=== PERF-BASELINE: RECORDED — {len(record['rows'])} rows, {sessions} session(s) → {out} ===")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
