#!/usr/bin/env python3
"""Self-test for readonly_repo.py — a write inside a git tree is blocked, a write in a non-repo directory passes.
Run: python scripts/hooks/test_readonly_repo.py   (every gate and CI run it through scripts/self_tests.py.)"""
import json
import pathlib
import subprocess
import sys
import tempfile

HOOK = pathlib.Path(__file__).with_name("readonly_repo.py")
REPO = pathlib.Path(__file__).resolve().parents[2]
OUTSIDE = pathlib.Path(tempfile.mkdtemp())

CASES = [
    (str(REPO / "src" / "x.cs"), True),
    (str(REPO / "docs" / "new-dir" / "deeper" / "y.md"), True),   # a not-yet-existing directory inside the tree
    (str(OUTSIDE / "out" / "refute-a.jsonl"), False),
    (str(OUTSIDE / "report.md"), False),
]


def blocked(path: str) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps({"tool_input": {"file_path": path}}),
                       capture_output=True, text=True, timeout=30)
    return r.returncode == 2


fails = [(p, e) for p, e in CASES if blocked(p) != e]
for p, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {p}")
print(f"readonly_repo self-test: {len(CASES) - len(fails)}/{len(CASES)} " + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
