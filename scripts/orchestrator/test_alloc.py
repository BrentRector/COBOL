#!/usr/bin/env python3
"""Self-test for alloc.py: the truth rules, the reservation high-water mark, stale-lock recovery, and that parallel
REAL processes never receive the same value.
Run: python scripts/orchestrator/test_alloc.py   (no submodule, no build; a fabricated repo in a temp directory)."""
import os
import pathlib
import subprocess
import sys
import tempfile
import time

HERE = pathlib.Path(__file__).resolve().parent
ALLOC = HERE / "alloc.py"
sys.path.insert(0, str(HERE))
import alloc  # noqa: E402

TMP = pathlib.Path(tempfile.mkdtemp(prefix="alloc-test-"))
REPO = TMP / "repo"
(REPO / "kb" / "Work").mkdir(parents=True)
(REPO / "src" / "Cobol.Net.Editions" / "Diagnostics").mkdir(parents=True)
(REPO / "src" / "A" / "bin").mkdir(parents=True)
(REPO / "DEVLOG.md").write_text("# DEVLOG\n\nordering note: newest first\n\n## Entry 41 — x\n\n## Entry 40 — y\n",
                                encoding="utf-8")
for n in (7, 12, 9):
    (REPO / "kb" / "Work" / f"PB{n}.md").write_text("---\nid: x\n---\n", encoding="utf-8")
(REPO / "kb" / "Work" / "PB99-notes.txt").write_text("not a note", encoding="utf-8")
(REPO / "src" / "Cobol.Net.Editions" / "Diagnostics" / "DiagnosticCatalog.cs").write_text(
    'Add("COBOLNET0100", ...); Add("COBOLNET0105", ...);', encoding="utf-8")
(REPO / "src" / "A" / "X.cs").write_text("// emits COBOLNET0103 and COBOLNET0101", encoding="utf-8")
(REPO / "src" / "A" / "bin" / "Y.cs").write_text("// COBOLNET9999 lives in bin and is ignored", encoding="utf-8")

fails = []
checked = []


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def run(cdir, *args):
    env = dict(os.environ, COBOL_COORD_DIR=str(cdir))
    r = subprocess.run([sys.executable, str(ALLOC), *args, "--repo", str(REPO)], capture_output=True, text=True,
                       encoding="utf-8", env=env, timeout=120)
    return r.returncode, r.stdout.strip()


# 1. truth rules
check("devlog top", alloc.devlog_top(REPO), 41)
check("pb max", alloc.pb_max(REPO), 12)
# The catalog file is under src/, so the src scan includes it (as the probe's scan always has); bin/ is skipped.
check("code scans (bin ignored)", alloc.code_scans(REPO), (105, 105))

# 2. CLI: peek reserves nothing; allocations advance past truth and past each other
c1 = TMP / "c1"
check("peek code", run(c1, "peek", "code"), (0, "COBOLNET0106"))
check("peek again unchanged", run(c1, "peek", "code"), (0, "COBOLNET0106"))
check("alloc code 3", run(c1, "code", "3"), (0, "COBOLNET0106-COBOLNET0108"))
check("alloc code 1", run(c1, "code", "1"), (0, "COBOLNET0109"))
check("alloc pb 5", run(c1, "pb", "5"), (0, "PB13-PB17"))
check("alloc devlog", run(c1, "devlog"), (0, "42"))
check("alloc devlog again", run(c1, "devlog"), (0, "43"))
rc, line = run(c1, "peek", "code", "--probe")
check("probe line names the reserved mark", line.endswith("reserved max COBOLNET0109 → next free = COBOLNET0110"), True)

# 3. truth wins over a stale reservation (a note landed past the reserved mark)
(REPO / "kb" / "Work" / "PB50.md").write_text("---\n---\n", encoding="utf-8")
check("truth wins", run(c1, "pb", "1"), (0, "PB51"))

# 4. seed raises, never lowers
check("seed raises", run(c1, "seed", "code", "500"), (0, "code reserved through COBOLNET0500"))
check("seed never lowers", run(c1, "seed", "code", "200"), (0, "code reserved through COBOLNET0500"))
check("after seed", run(c1, "code", "1"), (0, "COBOLNET0501"))

# 5. a bad count is refused
check("zero count refused", run(c1, "pb", "0")[0], 2)

# 6. stale-lock recovery: a lock left by a crashed allocator is broken; a fresh one is waited on
c2 = TMP / "c2"
c2.mkdir()
lock = c2 / "alloc.lock"
lock.write_text("12345 0\n", encoding="utf-8")
old = time.time() - alloc.STALE_S - 5
os.utime(lock, (old, old))
t0 = time.monotonic()
check("stale lock broken", run(c2, "code", "1"), (0, "COBOLNET0106"))
check("stale lock broken promptly", time.monotonic() - t0 < 20, True)
check("lock released", lock.exists(), False)

# 7. parallel REAL processes: 8 workers x 6 allocations of 3 codes, released together, never overlap
c3 = TMP / "c3"
c3.mkdir()
WORKER = TMP / "worker.py"
WORKER.write_text(
    "import subprocess, sys, time\n"
    "start = float(sys.argv[1])\n"
    "while time.time() < start: time.sleep(0.005)\n"
    "for _ in range(6):\n"
    "    r = subprocess.run([sys.executable, sys.argv[2], 'code', '3', '--repo', sys.argv[3]], capture_output=True, text=True)\n"
    "    print(r.stdout.strip() if r.returncode == 0 else 'ERR ' + r.stderr.strip())\n",
    encoding="utf-8")
start = time.time() + 3
env = dict(os.environ, COBOL_COORD_DIR=str(c3))
procs = [subprocess.Popen([sys.executable, str(WORKER), str(start), str(ALLOC), str(REPO)], stdout=subprocess.PIPE,
                          text=True, env=env) for _ in range(8)]
outs = [p.communicate(timeout=300)[0].split() for p in procs]
ranges = [r for o in outs for r in o]
errors = [r for r in ranges if r.startswith("ERR")]
check("parallel: no errors", errors, [])
values = []
for r in ranges:
    a, b = r.split("-")
    values += list(range(int(a.removeprefix("COBOLNET")), int(b.removeprefix("COBOLNET")) + 1))
check("parallel: every allocation returned", len(ranges), 48)
check("parallel: no value handed out twice", len(values) - len(set(values)), 0)
check("parallel: dense from truth+1", sorted(values) == list(range(106, 106 + 144)), True)

for f in fails:
    print("FAIL:", f)
print(f"alloc self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
