"""Time each gate audit (run_gate_legs.AUDITS) serially, as the gate runs them."""
import os
import subprocess
import sys
import time

repo = sys.argv[1]
sys.path.insert(0, os.path.join(repo, "scripts"))
import run_gate_legs  # noqa: E402

total = 0.0
for name, cmd in run_gate_legs.AUDITS:
    t = time.monotonic()
    rc = subprocess.run([sys.executable, *cmd], cwd=repo, capture_output=True).returncode
    dt_ = time.monotonic() - t
    total += dt_
    print(f"{name:34} {dt_:6.1f} s  rc={rc}")
print(f"{'TOTAL':34} {total:6.1f} s")
