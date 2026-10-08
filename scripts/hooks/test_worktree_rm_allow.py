#!/usr/bin/env python3
"""Self-test for worktree_rm_allow.py — the grant fires on its exact shape and stays silent on every neighbour.

A permission-granting hook is the mirror image of a guard: its FAILURE branch is an allow that should not have been
given, so every case below that must NOT be allowed is as load-bearing as the ones that must. Run:
python scripts/hooks/test_worktree_rm_allow.py   (every gate and CI run it through scripts/self_tests.py.)
"""
import json
import pathlib
import subprocess
import sys

HOOK = pathlib.Path(__file__).with_name("worktree_rm_allow.py")
REPO = HOOK.resolve().parents[2]
WT = REPO / ".claude" / "worktrees" / "selftest-wt-1"   # need not exist: the hook resolves paths, never stats them
MAIN = str(REPO)
BS = chr(92)

CASES = [
    # (command, cwd, allowed?)
    ("git rm -r -q src/CobolSharp.Compiler tests/CobolSharp.Tests.Unit", str(WT), True),
    ("git rm -r src/X", str(WT), True),
    ("git rm -r --cached src/X", str(WT), True),
    ("git rm -r -- src/X", str(WT), True),
    (f"git rm -r src{BS}X", str(WT).replace("/", BS), True),                      # Windows spellings
    (f"git -C {WT.as_posix()} rm -r src/X", MAIN, True),                            # -C into a worktree from main
    (f"cd {WT.as_posix()} && git rm -r src/X", MAIN, True),                          # cd prefix, the one chain allowed
    (f"git rm -r {WT.as_posix()}/src/X", str(WT), True),                             # absolute path inside the worktree
    ("git rm -r src/X", MAIN, False),                                                # the main checkout
    ("git rm -r src/X", str(REPO / ".claude" / "worktrees"), False),                 # the worktrees folder itself
    (f"git rm -r {REPO.as_posix()}/src/X", str(WT), False),                          # absolute path outside
    ("git rm -r ../../../src/X", str(WT), False),                                    # escapes the worktree
    ("git rm -r -f src/X", str(WT), False),                                          # --force discards edits
    ("git rm -r --force src/X", str(WT), False),
    ("git rm src/X", str(WT), False),                                                # no -r: ordinary flow decides
    ("git rm -r src/X && git commit -q -m x", str(WT), False),                       # a chain is never covered
    ("git rm -r src/X; rm -rf /", str(WT), False),
    ("git rm -r src/X | tee log", str(WT), False),
    ("rm -rf src/X", str(WT), False),                                                # not git
    ("git -C E:/elsewhere rm -r src/X", str(WT), False),                             # -C out of the worktree (drive path)
    ("git -C /elsewhere rm -r src/X", str(WT), False),                               # -C out (POSIX root): absolute on EVERY host
    ("git -C C:/elsewhere rm -r src/X", str(WT), False),
    ("git rm -r /elsewhere/src/X", str(WT), False),                                  # a POSIX-absolute path outside
    ("cd /elsewhere && git rm -r src/X", str(WT), False),                            # cd to a POSIX-absolute path outside
    ("cd .. && git rm -r src/X", str(WT), False),                                    # cd out of the worktree
    ("git rm -r", str(WT), False),                                                   # no path
    ("echo 'git rm -r src/X'", str(WT), False),                                      # not the command
]


def allowed(cmd: str, cwd: str) -> bool:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps({"tool_name": "Bash", "tool_input": {"command": cmd}, "cwd": cwd}),
                       capture_output=True, text=True, timeout=30)
    if r.returncode != 0:
        print(f"FAIL: hook exited {r.returncode} on {cmd!r}: {r.stderr.strip()}")
        return False
    if not r.stdout.strip():
        return False
    out = json.loads(r.stdout)
    return out.get("hookSpecificOutput", {}).get("permissionDecision") == "allow"


def main() -> int:
    bad = 0
    for cmd, cwd, want in CASES:
        got = allowed(cmd, cwd)
        if got != want:
            bad += 1
            print(f"FAIL: {'allow' if want else 'no decision'} expected, got {'allow' if got else 'no decision'}: {cmd!r} in {cwd!r}")
    # unparseable input never wedges the session
    r = subprocess.run([sys.executable, str(HOOK)], input="not json", capture_output=True, text=True, timeout=30)
    if r.returncode != 0 or r.stdout.strip():
        bad += 1
        print("FAIL: unparseable input must exit 0 with no decision")
    print(f"worktree_rm_allow self-test: {len(CASES) + 1 - bad}/{len(CASES) + 1} {'GREEN' if not bad else 'RED'}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
