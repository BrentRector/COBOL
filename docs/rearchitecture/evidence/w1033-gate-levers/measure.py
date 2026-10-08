"""Before/after: whole-assembly test runs (cold compile cache), alternating arms, with the host load sampled.

usage: python measure.py <before worktree> <after worktree> <out file> <assembly>[,<assembly>...] <rounds>
"""
import os
import re
import statistics
import subprocess
import sys
import threading
import time

before, after, out_path, assemblies, rounds = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4].split(","), int(sys.argv[5])


def load() -> float:
    r = subprocess.run(["powershell", "-NoProfile", "-c",
                        "(Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average"],
                       capture_output=True, text=True)
    try:
        return float(r.stdout.strip())
    except ValueError:
        return float("nan")


def run(arm: str, tree: str, asm: str) -> str:
    samples: list[float] = []
    stop = threading.Event()

    def sampler():
        while not stop.is_set():
            samples.append(load())
            stop.wait(10)

    env = {k: v for k, v in os.environ.items() if not k.upper().startswith("COBOLNET_GATE_")}
    env["COBOLNET_COMPILE_CACHE"] = "off"
    env["DOTNET_CLI_UI_LANGUAGE"] = "en"
    t = threading.Thread(target=sampler, daemon=True)
    t.start()
    t0 = time.monotonic()
    r = subprocess.run(["dotnet", "test", os.path.join(tree, "tests", f"Cobol.Net.Tests.{asm}"), "--no-build"],
                       cwd=tree, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    wall = time.monotonic() - t0
    stop.set()
    t.join()
    m = re.findall(r"(Passed!|Failed!)\s+-\s+Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+)", r.stdout)
    tally = m[-1] if m else ("?",) * 5
    fails = re.findall(r"^\s+Failed (\S+)", r.stdout, re.M)
    good = [s for s in samples if s == s]
    return (f"{arm:6} {asm:16} wall {wall:7.1f} s  {tally[0]} failed {tally[1]} passed {tally[2]} total {tally[4]}  "
            f"load mean {statistics.fmean(good) if good else float('nan'):5.1f} % (n={len(good)})"
            + (f"  fails: {', '.join(f[:80] for f in fails[:3])}" if fails else ""))


with open(out_path, "a", encoding="utf-8") as out:
    for i in range(rounds):
        for asm in assemblies:
            for arm, tree in (("before", before), ("after", after)) if i % 2 == 0 else (("after", after), ("before", before)):
                line = run(arm, tree, asm)
                print(line, flush=True)
                out.write(line + "\n")
                out.flush()
    out.write("MEASURE DONE\n")
print("MEASURE DONE")
