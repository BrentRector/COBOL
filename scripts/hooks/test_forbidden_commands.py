#!/usr/bin/env python3
"""Self-test for forbidden_commands.py — every rule fires on its shape AND stays silent on the legitimate neighbour.

A guard hook is a gate, and a gate is only trusted once its failure branch has fired (feedback: prove the watchdog
fails). Run: python scripts/hooks/test_forbidden_commands.py   (every gate and CI run it through scripts/self_tests.py.)
"""
import json
import os
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
    ("git -C /e/COBOL/tools/claude-skills push -q origin main --follow-tags", False),  # a registered submodule is another repo
    ("cd tools/claude-skills && git push origin main", False),                         # relative, same submodule
    ("cd /e/COBOL/tools && git push origin main", True),                               # its parent folder is still this repo
    # every location change the shared parser reads (kb/Work PB2599), not only a bare `cd`
    ("pushd /e/claude-skills && git push -q origin main", False),
    ('cd "/e/Sites/wise owl" && git push origin main', False),                          # a quoted path with a space
    ("ls; cd /e/claude-skills && git push origin main", False),
    ("pushd /e/COBOL && git push origin main", True),
    ("python - <<'EOF'\nprint('a" + BS + "nb')\nEOF", True),
    ("python - <<'EOF'\nprint('plain')\nEOF", False),
    ('dotnet test x.csproj --filter "~Drift|~EditionGate" > log.txt', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift|FullyQualifiedName~EditionGate" > log.txt', False),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift"', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift" 2>&1 | tail -5', False),
    ("dotnet test x.csproj", False),
    # 5. chaining after a verdict command
    ("dotnet test x.csproj > t.log 2>&1 && git commit -m x", True),
    ("dotnet build Cobol.Net.sln -c Debug && dotnet test x.csproj > t.log", True),
    ("bash scripts/push-main.sh > p.log 2>&1; git log -1", True),
    ("pwsh -File scripts/build-local.ps1 -Mode implementer *> b.log || true", True),
    ("bash scripts/push-main.sh > p.log 2>&1; echo \"rc=$?\"; grep -E landed p.log | tail -2", False),
    ("dotnet build Cobol.Net.sln -c Debug -v q 2>&1 | grep -E 'error|Build succeeded'", False),
    ("cd /e/COBOL && dotnet build Cobol.Net.sln -c Debug > b.log 2>&1", False),
    ("timeout 580 bash -c 'tail -n +1 -f b.log | grep -m1 VERDICT' ; tail -3 b.log", False),
    ("git status --short && git log --oneline -3", False),
    ("bash -c 'dotnet test x.csproj > t.log && echo done'", False),
    # 5b. a command that merely NAMES a gate or landing script is not a verdict command
    ("git add -- scripts/push-main.sh DEVLOG.md && git commit -q -m x", False),
    ("sed -n 17,20p scripts/push-main.sh && head -3 x.txt", False),
    ("git add -- .github/workflows/build-and-test.yml && git commit -q -m x", False),
    ("pwsh -NoProfile -File scripts/build-local.ps1 -Mode lander > b.log 2>&1; git status", True),
    ("bash scripts/push-main.sh > p.log 2>&1 && git log -1", True),
    # 6. WSL lifecycle commands are the owner's to run
    ("wsl --terminate Ubuntu", True),
    ("wsl.exe --export Ubuntu E:/x.tar", True),
    ("wsl --shutdown", True),
    ("wsl --update --web-download", True),
    ("wsl -t Ubuntu", True),
    ("wsl --unregister Ubuntu-26.04", True),
    ("wsl --install Ubuntu-26.04 --no-launch", True),
    ("wsl --terminate Ubuntu | Out-Null; wsl --export Ubuntu E:/x.tar", True),
    ("wsl -d Ubuntu -e bash -lc 'ls ~'", False),
    ("wsl -d Ubuntu --cd /mnt/e/COBOL -- bash -lc 'bash scripts/linux-gate.sh'", False),
    ("wsl -l -v", False),
    ("git commit -q -m 'document wsl --terminate and wsl --export for the owner'", False),
    ("grep -n 'wsl --export' docs/x.md", False),
]


# The PowerShell tool's commands: the same rules, read with PowerShell's location changes (kb/Work PB2599).
PS_CASES = [
    ("Set-Location E:/claude-skills; git push -q origin main", False),
    ("sl E:/claude-skills; git push -q origin main", False),
    ("Push-Location -Path 'E:/Sites/wise owl'; git push origin main", False),
    ("Set-Location E:/COBOL; git push origin main", True),
    ("git push origin HEAD:main", True),
    ("git stash", True),
]


def blocked(cmd: str, unit: bool = False, tool: str = "Bash") -> bool:
    # The environment is part of the case: an orchestrator unit has COBOL_COORD_DIR, an attended session never does. Set or
    # removed EXPLICITLY, so the result never depends on who runs this test.
    env = {k: v for k, v in os.environ.items() if k != "COBOL_COORD_DIR"}
    if unit:
        env["COBOL_COORD_DIR"] = "E:/COBOL-coord"
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps({"tool_name": tool, "tool_input": {"command": cmd}}),
                       capture_output=True, text=True, timeout=30, env=env)
    return r.returncode == 2


# Inside an orchestrator unit EVERY direct `git push` is refused, whatever the repository or the flags (PB2044: a unit pushed
# the public skills repo with `git push -q origin HEAD:main`, which the deny rule's text pattern missed). The legitimate
# neighbours stay silent: the landing script, other git subcommands that merely mention the word, and a stash (rule 1's).
UNIT_CASES = [
    ("git push -q origin HEAD:main", True),
    ("cd /e/claude-skills && git push -q origin main", True),
    ("git -C E:/claude-skills push origin main", True),
    ("git -c core.askpass=x push origin v1.18.0", True),
    ("git --no-pager push --quiet origin HEAD:refs/heads/main", True),
    ("git push origin HEAD:ci/abc123", True),
    ("cd /e/Sites/wiseowlsoftware.com; git push", True),
    ("git push -u origin claude/batch-7", True),
    ("bash scripts/push-main.sh", False),
    ("git log --grep=push", False),
    ("git log --oneline -3 -- scripts/push-main.sh", False),
    ("git commit -q -m 'document git push for the owner'", False),
    ('git commit -q -m "PB2044: a unit ran git push -q origin HEAD:main"', False),
    ("bash -c 'cd /e/claude-skills && git push -q origin main'", True),       # the payload of bash -c IS a command line
    ('pwsh -Command "git -C E:/claude-skills push origin main"', True),
    ("git status -s", False),
    # THE SHAPE THE UNIT ACTUALLY USED: a heredoc, then the commit and the push after its terminator
    ("cd /x/scratch; git add -A; cat > ../m.txt <<'EOF'" + chr(10) + "v1.17.1: a fix" + chr(10) + "EOF" + chr(10)
     + "git commit -q -F ../m.txt; git push -q origin HEAD:main", True),
    ("cat > ../m.txt <<'EOF'" + chr(10) + "the unit ran git push -q origin HEAD:main" + chr(10) + "EOF" + chr(10)
     + "git commit -q -F ../m.txt", False),                                    # the word inside the heredoc BODY is text
]

fails =[(c, e) for c, e in CASES if blocked(c) != e]
fails += [(f"[unit] {c}", e) for c, e in UNIT_CASES if blocked(c, unit=True) != e]
fails += [(f"[PowerShell] {c}", e) for c, e in PS_CASES if blocked(c, tool="PowerShell") != e]
# The same pushes in an ATTENDED session (no COBOL_COORD_DIR) keep their old behaviour: rule 2 alone decides.
ATTENDED = [("git push -u origin claude/batch-7", False), ("git -C E:/claude-skills push origin v1.18.0", False),
            ("git push -q origin HEAD:main", True)]
fails += [(f"[attended] {c}", e) for c, e in ATTENDED if blocked(c) != e]
for c, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {c!r}")
total = len(CASES) + len(UNIT_CASES) + len(PS_CASES) + len(ATTENDED)
print(f"forbidden_commands self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
