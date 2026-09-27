#!/usr/bin/env python3
"""Self-test for forbidden_commands.py — every rule fires on its shape AND stays silent on the legitimate neighbour.

A guard hook is a gate, and a gate is only trusted once its failure branch has fired (feedback: prove the watchdog
fails). Run: python scripts/hooks/test_forbidden_commands.py   (CI `audits` job and build-local run it too.)
"""
import json
import pathlib
import subprocess
import sys

HOOK = pathlib.Path(__file__).with_name("forbidden_commands.py")
BS = chr(92)  # a backslash, spelled without one

CASES = [
    # (command, expect_block)
    ("git stash push -m wip", True),
    ("git stash", True),
    ("git stash pop", True),
    ("git -C E:/COBOL stash apply", True),
    ("git stash list", False),
    ("git rebase --autostash origin/main", True),
    ("git rebase origin/main", False),
    ("git push origin HEAD:main", True),
    ("git push origin main", True),
    ("git push -q origin HEAD:refs/heads/main", True),
    ("git push origin HEAD:ci/abc123", False),
    ("bash scripts/push-main.sh", False),
    ("cd /e/claude-skills && git push -q origin main", False),
    ("cd /e/COBOL && git push origin HEAD:main", True),
    ("cd E:/CobolSharp && git push origin HEAD:main", True),     # the pre-rename folder name is still this repo
    ("cd scripts && git push origin main", True),               # a relative cd never leaves the repo
    ("cd /e/COBOL-private && git push origin main", True),      # a sibling of this repo's name (fail closed)
    ("cd /e/Sites/wiseowlsoftware.com && git push origin main", False),
    ("python - <<'EOF'\nprint('a" + BS + "nb')\nEOF", True),
    ("python - <<'EOF'\nprint('plain')\nEOF", False),
    ('dotnet test x.csproj --filter "~Drift|~EditionGate" > log.txt', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift|FullyQualifiedName~EditionGate" > log.txt', False),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift"', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift" 2>&1 | tail -5', False),
    ("dotnet test x.csproj", False),
    # 5. chaining after a verdict command
    ("dotnet test x.csproj > t.log 2>&1 && git commit -m x", True),
    ("dotnet build CobolSharp.sln -c Debug && dotnet test x.csproj > t.log", True),
    ("bash scripts/push-main.sh > p.log 2>&1; git log -1", True),
    ("pwsh -File scripts/build-local.ps1 -Filter X *> b.log || true", True),
    ("bash scripts/push-main.sh > p.log 2>&1; echo \"rc=$?\"; grep -E landed p.log | tail -2", False),
    ("dotnet build CobolSharp.sln -c Debug -v q 2>&1 | grep -E 'error|Build succeeded'", False),
    ("cd /e/COBOL && dotnet build CobolSharp.sln -c Debug > b.log 2>&1", False),
    ("timeout 580 bash -c 'tail -n +1 -f b.log | grep -m1 VERDICT' ; tail -3 b.log", False),
    ("git status --short && git log --oneline -3", False),
    ("bash -c 'dotnet test x.csproj > t.log && echo done'", False),
]


def blocked(cmd: str) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps({"tool_input": {"command": cmd}}),
                       capture_output=True, text=True, timeout=30)
    return r.returncode == 2


fails = [(c, e) for c, e in CASES if blocked(c) != e]
for c, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {c!r}")
print(f"forbidden_commands self-test: {len(CASES) - len(fails)}/{len(CASES)} "
      + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
