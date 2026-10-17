#!/usr/bin/env python3
"""Self-test for forbidden_commands.py — every rule fires on its shape AND stays silent on the legitimate neighbour.

A guard hook is a gate, and a gate is only trusted once its failure branch has fired (feedback: prove the watchdog
fails). Run: python scripts/hooks/test_forbidden_commands.py   (every gate and CI run it through scripts/self_tests.py.)
"""
import base64
import json
import os
import pathlib
import subprocess
import sys

HOOK = pathlib.Path(__file__).with_name("forbidden_commands.py")
BS = chr(92)  # a backslash, spelled without one
NL = chr(10)
# kb/Work PB3198: every rule reads the ONE model of the command line (shell_location.steps), so each rule is checked
# AFTER a heredoc (the rules that scanned only the text before the first `<<` passed these) and after a QUOTED `<<`
# (which made the rest of the line a "heredoc body" for the rules that did not).
HD = "cat > m.txt <<'EOF'" + NL + "hello" + NL + "EOF" + NL
QS = 'python -c "print(1<<2)"; '

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
    ("git push origin HEAD:worktree-agent-abc123", False),                # a work branch, never main
    ("python scripts/orchestrator/enqueue.py land worktree-agent-abc123", False),   # the landing script itself
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
    ("python scripts/orchestrator/enqueue.py land b > l.log 2>&1; git log -1", True),
    ("pwsh -File scripts/build-local.ps1 -Priority BelowNormal *> b.log || true", True),
    ("python scripts/orchestrator/enqueue.py land b > l.log 2>&1; echo \"rc=$?\"; grep -E ENQUEUE l.log | tail -2", False),
    ("dotnet build Cobol.Net.sln -c Debug -v q 2>&1 | grep -E 'error|Build succeeded'", False),
    ("cd /e/COBOL && dotnet build Cobol.Net.sln -c Debug > b.log 2>&1", False),
    ("timeout 580 bash -c 'tail -n +1 -f b.log | grep -m1 VERDICT' ; tail -3 b.log", False),
    ("git status --short && git log --oneline -3", False),
    ("bash -c 'dotnet test x.csproj > t.log && echo done'", False),
    # 5b. a command that merely NAMES a gate or landing script is not a verdict command
    ("git add -- scripts/build-local.ps1 DEVLOG/1961-x.md && git commit -q -m x", False),
    ("sed -n 17,20p scripts/orchestrator/enqueue.py && head -3 x.txt", False),
    ("git add -- .github/workflows/build-and-test.yml && git commit -q -m x", False),
    ("pwsh -NoProfile -File scripts/build-local.ps1 > b.log 2>&1; git status", True),
    # the repros of kb/Work PB1725 and PB1736: a read-only command whose ARGUMENTS name a verdict command or script
    ('grep -n "dotnet test" scripts/x.sh; echo ---', False),
    ('sed -i "s/x/overlaps battery.sh/" STATUS.md; git add -A', False),
    ("cat scripts/build-local.ps1; echo", False),
    ("git ls-tree HEAD scripts/linux-gate.sh scripts/build-local.sh; git ls-tree HEAD x", False),
    ("grep -rlE 'x|build-local.ps1' kb/Work | head; echo", False),
    ("pwsh scripts/build-local.ps1 -Priority BelowNormal; git commit -m x", True),
    ("python scripts/orchestrator/enqueue.py land b > l.log 2>&1 && git log -1", True),
    # 5b'. the chain is read by the SHARED parser (review R1-1's sibling): a comment's apostrophe, an escaped quote or a
    # heredoc before the chain no longer hides it
    ("git status  # don't" + chr(10) + "dotnet build x.sln > b.log 2>&1 && git commit -m x", True),
    ("echo it" + BS + "'s here && dotnet test x.csproj > t.log 2>&1 && git commit -m x", True),
    ("cat > m.txt <<'EOF'" + chr(10) + "x" + chr(10) + "EOF" + chr(10) + "dotnet test x.csproj > t.log 2>&1 && git commit -m x", True),
    ("dotnet test x.csproj > t.log 2>&1 & git status", False),           # a background `&` is not a chain (as before)
    # 5c. the merge-queue landing is a verdict command too (kb/Work PB3024)
    ("python scripts/orchestrator/enqueue.py queue --title T --body-file m.txt > q.log 2>&1 && git log -1", True),
    ("python scripts/orchestrator/enqueue.py queue --title T --body-file m.txt > q.log 2>&1; echo \"rc=$?\"", False),
    ("git add -- scripts/orchestrator/enqueue.py && git commit -q -m x", False),
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
    ("bash -c 'wsl --shutdown'", True),                                  # a -c payload's command is a command
    ("echo y | wsl --terminate Ubuntu", True),                           # so is a pipeline's later side
    # 7. a `cd` inside a BACKGROUNDED list with another command after its `&` (MANDATORY-PRACTICES P14(b), kb/Work PB3095)
    ("cd /e/x && git log -1 > a.txt & git status > b.txt &", True),     # the 2026-10-11 shape: b ran in the caller's dir
    ("cd /e/x && a & b", True),
    ("pushd /e/x && a & b", True),
    ("cd /e/x || exit 1 && a & wait", True),
    ("cd /e/x && git log -1", False),                                    # a plain chain: the cd binds what follows
    ("cd /e/x && git log -1 &", False),                                  # backgrounded, nothing after the `&`
    ("cd /e/x; a & b", False),                                           # the cd is its own list: it moves the caller
    ("git -C /e/x log -1 > a.txt & git -C /e/y status > b.txt &", False),
    ("cd /e/x && dotnet build x.sln > b.log 2>&1", False),               # `2>&1` is a redirection, not a background `&`
    ("cd /e/x && a &> l.log", False),                                    # `&>` too
    ("cd /e/x && a |& tee l.log", False),                                # and `|&`
    ("echo 'cd /e/x && a & b'", False),                                  # quoted text is text
    # review R1-1: the rule reads the SHARED parser, so a comment's apostrophe or an escaped quote cannot hide the list
    ("git status  # don't worry" + chr(10) + "cd /e/x && a & b", True),
    ("echo it" + BS + "'s here && cd /e/x && a & b", True),
    ("cat > m.txt <<'EOF'" + chr(10) + "cd /e/x && a & b" + chr(10) + "EOF" + chr(10) + "git status", False),   # heredoc body
    ("bash -c 'cd /e/x && a & b'", True),                                # a -c payload is a command line
    ("(cd /e/x && a) & b", False),                                       # the subshell already scopes the cd
    ("{ cd /e/x; a; } & b", True),                                       # a { } group is in the backgrounded list
    # PB3198: every rule after a heredoc, after a quoted `<<`, and silent on its words inside a body or a quote
    (HD + "git stash", True),
    (QS + "git stash", True),
    ("cat > m.txt <<'EOF'" + NL + "git stash" + NL + "EOF" + NL + "git status", False),   # the body is text
    ('git commit -m "never git stash"', False),                          # so is a quoted message
    (HD + "git pull --autostash", True),
    ("git -c rebase.autoStash=true rebase origin/main", True),           # autostash switched on through -c
    ("git -c rebase.autostash=false rebase origin/main", False),
    (HD + "git push origin main", True),
    (QS + "git push origin HEAD:main", True),
    ("git push origin +HEAD:main", True),                                # a forced refspec is still main
    ("git push origin +main", True),
    ("cd /e/claude-skills && git push origin x; git -C /e/COBOL push origin main", True),   # each push in its own dir
    ("cat > m.txt <<'EOF'" + NL + "git push origin main" + NL + "EOF" + NL + "git status", False),
    (QS + 'printf "a' + BS + 'n"', False),                               # a quoted `<<` is no heredoc: no body
    (HD + 'printf "a' + BS + 'n"', False),                               # the command after a body is no body
    ("bash -c 'cat' <<'EOF'" + NL + "a" + BS + "nb" + NL + "EOF", True),  # a body feeding a -c payload is a body
    (HD + 'dotnet test x.csproj --filter "~Foo" > log.txt', True),
    (QS + 'dotnet test x.csproj --filter "~Foo" > log.txt', True),
    (HD + 'dotnet test x.csproj --filter "FullyQualifiedName~Foo"', True),   # unredirected, after a heredoc
    ('dotnet test x.csproj --filter=~Foo > log.txt', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Foo" & echo x > y.txt', True),  # another command's redirect
    ('git commit -m "dotnet test --filter ~X"', False),                  # quoted text is no test run
    (QS + "dotnet build x.sln > b.log && git commit -m x", True),
    (HD + "wsl --shutdown", True),
    (QS + "wsl --shutdown", True),
    ("cat > m.txt <<'EOF'" + NL + "wsl --shutdown" + NL + "EOF", False),
    (HD + "cd /e/x && a & b", True),
    (QS + "cd /e/x && a & b", True),
    # a heredoc a shell reads as its script is a command line, so every rule reads it (the w1098c lead)
    ("bash <<'EOF'" + NL + "git stash" + NL + "EOF", True),
    ("bash -s <<'EOF'" + NL + "git push origin main" + NL + "EOF", True),
    ("sh <<'EOF'" + NL + 'dotnet test x.csproj --filter "~Foo" > log.txt' + NL + "EOF", True),
    ("pwsh -NoProfile -Command - <<'EOF'" + NL + "git stash" + NL + "EOF", True),
    ("bash run.sh <<'EOF'" + NL + "git stash" + NL + "EOF", False),   # stdin of a script is that script's data
    # the branch review of PB3198, R1-1: a command substitution is a command line, in a word or inside double quotes
    ("x=$(git stash)", True),
    ("echo `git stash`", True),
    ('echo "$(git stash)"', True),
    ("cat <(git stash)", True),
    ("x=$(git stash list)", False),
    ("r=$(git push origin HEAD:main 2>&1)", True),
    ('r=$(dotnet test x.csproj --filter "~Foo")', True),
    ("git commit -m \"$(cat <<'EOF'" + NL + "fix a" + BS + "nb" + NL + "EOF" + NL + ')"', True),   # the commit shape
    ("git commit -m \"$(cat <<'EOF'" + NL + "fix a" + NL + "EOF" + NL + ')"', False),
    # a quote or a bracket in the body never ends the substitution, so the command after it is still read
    ("git commit -m \"$(cat <<'EOF'" + NL + 'say "hi" (and \') too' + NL + "EOF" + NL + ')" && git stash', True),
    ("git commit -m \"$(cat <<'EOF'" + NL + 'say "hi" (and \') too' + NL + "EOF" + NL + ')" && git status', False),
    ("dotnet test x.csproj > t.log 2>&1; echo \"EXIT=$? $(date)\"", False),   # a substitution is its echo's own
    ('dotnet test x.csproj --filter "FullyQualifiedName~X" | tail $(echo -5)', False),   # the reader is still tail
    # R2-1: a `dotnet test` inside a substitution pipes to the next stage OF THAT SUBSTITUTION, and the reader's own
    # substitution (one level deeper) is never the reader
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | tail -5)', False),
    ("bash -c 'r=$(dotnet test x.csproj --filter FullyQualifiedName~Foo | tail -5)'", False),
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | grep $(echo Passed))', False),
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | sort)', True),       # piped, but not to a reader
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | sort $(echo x | tail -1))', True),  # tail is sort's
    ('dotnet test x.csproj --filter "FullyQualifiedName~Foo" | ( tail -5 )', False),    # a grouped reader is a reader
    # a group is a pipeline stage: it reads the stage before it, and the stage after it reads the group
    ("( echo 'git stash' ) | bash", True),
    ("echo 'git stash' | ( bash )", True),
    ("echo 'git stash list' | ( bash )", False),
    # R3-1: reading a pipe never makes a group's FIRST command a pipeline side, so its cd moves the group's later steps
    # (at ff8f1c01b the first two passed, a push to this repository's main, and the third was a false block)
    ("cd /e/claude-skills; echo y | ( cd /e/COBOL && git push origin HEAD:main )", True),
    ("cd /e/claude-skills; true | { cd /e/COBOL; git push origin main; }", True),
    ("echo y | ( cd /e/claude-skills && git push -q origin main )", False),
    # and the group's stdin reaches each of its commands, a -c payload's and a substitution's
    ("echo 'git stash' | ( cd /e/W; bash )", True),
    ("echo 'git stash list' | ( cd /e/W; bash )", False),
    ("echo 'git stash' | bash -c 'cd /e/W; bash'", True),
    ("echo 'git stash' | x=$(bash)", True),
    # R1-2: every other way a shell is handed a command line as a string or through stdin
    ('eval "git stash"', True),
    ('eval "git stash list"', False),
    ('cmd /c "git stash"', True),
    ('cmd //c "git push origin main"', True),
    ('cmd /c "cd /d E:/COBOL & git push origin main"', True),
    ('cmd /c "cd /d E:/claude-skills & git push origin main"', False),      # another repository, read as cmd.exe does
    ("echo 'git stash' | bash", True),
    ("printf 'git stash" + BS + "n' | sh", True),
    ("cat <<EOF | bash" + NL + "git stash" + NL + "EOF", True),
    ("echo 'git stash' | grep stash", False),                              # piped into a reader, not a shell
    ("cat run.sh | bash", False),                                          # what a file holds is not this line's
    ('bash <<< "git stash"', True),
    ('grep x <<< "git stash"', False),
    ("timeout 5 bash -c 'git stash'", True),                               # behind a command that runs another
    ("env A=1 sh -c 'git stash'", True),
    ("xargs -n1 sh -c 'git stash' < x.txt", True),
    ("wsl -d Ubuntu -e bash -lc 'git stash'", True),
    ("bash -euc 'git stash'", True),                                       # `-c` among single-letter options
    ("bash -C run.sh", False),                                             # `-C` is noclobber, not a payload
]


# The PowerShell tool's commands: the same rules, read with PowerShell's location changes (kb/Work PB2599).
PS_CASES = [
    ("Set-Location E:/claude-skills; git push -q origin main", False),
    ("sl E:/claude-skills; git push -q origin main", False),
    ("Push-Location -Path 'E:/Sites/wise owl'; git push origin main", False),
    ("Set-Location E:/COBOL; git push origin main", True),
    ("git push origin HEAD:main", True),
    ("git stash", True),
    ("Set-Location E:/x && a & b", True),                                # the same backgrounded list in PowerShell 7
    ("Set-Location E:/x; & pwsh -File y.ps1", False),                    # `&` that starts a command is the call operator
    ("& git stash pop", True),                                           # the call operator before git (PB3198)
    ("git commit -m 'never git stash'", False),
    # the branch review of PB3198: PowerShell's subexpressions, expressions and script blocks are command lines
    ("echo $(git stash)", True),
    ('Write-Output "x $(git stash)"', True),
    ("Write-Output (git stash)", True),
    ("Get-ChildItem | ForEach-Object { git stash }", True),
    ("if ($x) { git stash }", True),
    # R3-1's PowerShell forms: a script block reading the pipeline runs in this runspace, so its Set-Location moves it
    ("Set-Location E:/claude-skills; 'y' | & { Set-Location E:/COBOL; git push origin main }", True),
    ("Set-Location E:/claude-skills; 'y' | ForEach-Object { Set-Location E:/COBOL; git push origin main }", True),
    ("'y' | & { Set-Location E:/claude-skills; git push origin main }", False),
    ("$m = (git log -1 --format=%s)", False),
    ("Write-Output 'git stash'", False),
    # R2-1: a filtered run inside an expression or subexpression pipes to the reader beside it
    ('$r = (dotnet test x.csproj --filter "FullyQualifiedName~Foo" | Select-String Passed)', False),
    ('$r = $(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | Select-String Passed)', False),
    ('$r = (dotnet test x.csproj --filter "FullyQualifiedName~Foo" | Sort-Object)', True),
    # and every interpreter handed a string
    ('Invoke-Expression "git stash"', True),
    ("iex 'git stash'", True),
    ("'git stash' | Invoke-Expression", True),
    ("'git stash' | Write-Output", False),
    ("& pwsh -NoProfile -c 'git stash'", True),
    ('cmd /c "git stash"', True),
    ("pwsh -EncodedCommand " + base64.b64encode("git stash".encode("utf-16-le")).decode(), True),
    ("pwsh -EncodedCommand " + base64.b64encode("git status".encode("utf-16-le")).decode(), False),
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
    ("git push origin HEAD:worktree-agent-abc123", True),
    ("cd /e/Sites/wiseowlsoftware.com; git push", True),
    ("git push -u origin claude/batch-7", True),
    ("python scripts/orchestrator/enqueue.py land worktree-agent-abc123", False),   # its push is the script's
    ("python scripts/orchestrator/enqueue.py queue --title T --body-file m.txt", False),   # its push is the script's
    ("git log --grep=push", False),
    ("git log --oneline -3 -- scripts/orchestrator/enqueue.py", False),
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
    (QS + "git push", True),                                                   # a quoted `<<` hides nothing (PB3198)
    ("git commit -q -m 'x' && xargs -n1 git push origin < refs.txt", True),    # git wherever it stands in the step
    # the branch review of PB3198, R1-1: a push inside a command substitution is a push
    ("out=$(git -C /e/claude-skills push origin main 2>&1)", True),
    ("out=$(git push -q origin HEAD:main 2>&1)", True),
    ("echo `git push`", True),
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
