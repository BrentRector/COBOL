#!/usr/bin/env python3
"""PreToolUse hook — BLOCK the shell commands this repo's rules forbid, with the reason fed back to the agent.

Owner decision 2026-09-25 (tooling recommendation 2): prose rules are forgotten, a hook is not, and a PreToolUse hook
fires inside subagents and Workflow agents too. Each rule below was written in a skill or memory AND broken anyway:

  git stash / --autostash   the stash stack is SHARED by every linked worktree (common git dir); PB713's implementer
                            popped the registrar's stash. WIP goes into a commit (workstream §3).
  bare push to main         main requires the ci-gate check; the only route is `bash scripts/push-main.sh`
                            (owner decision Q23, 2026-09-06). A push to ci/<sha> or any other branch is fine.
  escapes in a heredoc      heredoc bodies with backslash escapes were mangled or mis-escaped in six sessions (and the
                            harness decodes some escapes in tool input); write the file with the Write tool instead.
  bare `~X` test filter     `--filter "~X|~Y"` matches NOTHING and exits 0 — a silent green; every term needs a
                            property (FullyQualifiedName~X).
  unredirected filtered     a filtered `dotnet test` whose output is neither redirected nor piped floods the context
  dotnet test               and hides the verdict line; log it and read the verdict line (the gate skill).
  chaining after a verdict  a build / test / gate / push-main command followed by `&&`, `||` or `;` and another
                            command: the chain's exit status is its LAST command's, so the verdict is masked or acted
                            on unread (`dotnet test … && git commit` committed on a false green). Allowed after it: an
                            `echo … $?` that CAPTURES the status, and once captured, read-only commands on the log.
                            Owner 2026-09-27 (adopting the no-chaining idea from carlymr/carlys-claude-skills in a
                            targeted form: independent commands go as parallel tool calls in one turn, because a
                            blanket ban would add turns, and agent cost is quadratic in turns).

Exit 2 blocks the call and returns stderr to the agent. Anything unparseable passes (a hook must never wedge a session).
"""
import json
import re
import sys


# the harness reads hook stderr as UTF-8; Windows would otherwise encode it in the console code page (an em dash arrived
# mangled in the first live block, 2026-09-25)
sys.stderr.reconfigure(encoding="utf-8")

def block(reason: str) -> None:
    sys.stderr.write("BLOCKED by scripts/hooks/forbidden_commands.py: " + reason + "\n")
    sys.exit(2)


try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    sys.exit(0)

cmd = (data.get("tool_input") or {}).get("command", "") or ""

# Split on the heredoc marker so a rule about COMMANDS never fires on text inside a heredoc body (and vice versa).
head, _, body = cmd.partition("<<")
commands = head if body else cmd

# 1. git stash (listing/showing is harmless) and --autostash
for m in re.finditer(r"\bgit\s+(?:-C\s+\S+\s+)?stash\b(\s+\w+)?", commands):
    sub = (m.group(1) or "").strip()
    if sub not in ("list", "show"):
        block("`git stash` is forbidden in this repo — the stash stack is shared by every worktree and another "
              "agent's pop takes your entry. Commit WIP on your own branch (`git commit -m \"WIP checkpoint: ...\"`); "
              "to compare against a clean tree use `git worktree add --detach <path> <sha>`.")
if re.search(r"--autostash\b", commands):
    block("`--autostash` uses the shared stash stack (see `git stash`). Commit WIP first, then rebase.")

# 2. a push that targets main directly — THIS repo only (another repo reached by `cd`/`-C` has its own rules).
# "This repo" is recognized by its FOLDER NAME, derived from where this hook lives — never a hard-coded literal: the
# literal "cobolsharp" silently failed OPEN when the repo became E:\COBOL (2026-09-26). The old name is kept so a
# path written before the rename is still recognized; a RELATIVE `cd` never leaves the repo.
_ROOT = __import__("pathlib").Path(__file__).resolve().parents[2].name.lower()
_THIS_REPO = {_ROOT, "cobol", "cobolsharp"}


def _is_this_repo(target: str) -> bool:
    t = target.lower().replace("\\", "/")
    if not (t.startswith(("/", "~")) or re.match(r"[a-z]:", t)):
        return True
    segs = [s for s in t.split("/") if s]
    return any(s in _THIS_REPO or any(s.startswith(n + "-") for n in _THIS_REPO) for s in segs)


cds = re.findall(r"(?:\bcd|\bSet-Location|\bgit\s+-C)\s+[\"']?([^\s;&|\"']+)", commands)
other_repo = any(not _is_this_repo(c) for c in cds)
for m in ([] if other_repo else re.finditer(r"\bgit\s+(?:-C\s+\S+\s+)?push\b([^;&|\n]*)", commands)):
    args = m.group(1)
    if re.search(r"(?:^|\s|:)(?:refs/heads/)?main(?:\s|$)", args):
        block("a direct push to main is refused by the server (required `ci-gate` check). Land with "
              "`bash scripts/push-main.sh` — it pushes ci/<sha>, waits for green, then fast-forwards main. "
              "It is idempotent; run it with run_in_background and block on its log.")
# 3. backslash escapes inside a heredoc body
if body and re.search(r"\\[nrtuUx0abfvdswDSW\\'\"]", body):
    block("this heredoc body contains backslash escape sequences, which have been mangled here repeatedly. Write the "
          "script or file with the Write tool (into the scratchpad) and run it, or express the character without an "
          "escape (chr(10), a character class).")

# 4. dotnet test filters
if re.search(r"\bdotnet\s+test\b", commands) and "--filter" in commands:
    fm = re.search(r"--filter\s+(\"[^\"]*\"|'[^']*'|\S+)", commands)
    if fm:
        terms = re.split(r"[|&]", fm.group(1).strip("\"'"))
        bare = [t for t in terms if t.strip().startswith(("~", "!~", "="))]
        if bare:
            block(f"filter term(s) {bare} have no property, so they match NOTHING and the run exits 0 — a silent "
                  "green. Write `FullyQualifiedName~X` for every term (or use scripts/build-local.{ps1,sh}).")
    if not re.search(r"(>|\|\s*(tail|grep|Select-String|Tee-Object|tee|findstr|Out-File))", commands):
        block("a filtered `dotnet test` must redirect its output to a log (or pipe it through tail/grep) and read the "
              "verdict line — unredirected output floods the context and hides the verdict. Prefer "
              "scripts/build-local.{ps1,sh}.")

# 5. chaining after a verdict command
VERDICT = re.compile(r"\bdotnet\s+(?:test|build)\b|build-local\.(?:ps1|sh)\b|push-main\.sh\b|battery\.sh\b|"
                     r"guard(?:-fast)?\.sh\b")
READ_ONLY = re.compile(r"^\s*(?:grep|rg|tail|head|cat|sed\s+-n|wc|ls|Select-String|Get-Content|findstr|type)\b")


def _segments(text: str):
    """Split a command line on top-level `&&`, `||` and `;`, ignoring quoted text (a separator inside a quoted
    string or a `bash -c '…'` argument is not a separator of THIS command line)."""
    out, cur, q, i = [], [], None, 0
    while i < len(text):
        ch = text[i]
        if q:
            cur.append(ch)
            if ch == q:
                q = None
        elif ch in "'\"":
            q = ch
            cur.append(ch)
        elif text.startswith("&&", i) or text.startswith("||", i):
            out.append("".join(cur)); cur = []; i += 2; continue
        elif ch == ";" or ch == "\n":
            out.append("".join(cur)); cur = []
        else:
            cur.append(ch)
        i += 1
    out.append("".join(cur))
    return [s.strip() for s in out if s.strip()]


segs = _segments(commands)
for i, seg in enumerate(segs[:-1]):
    if not VERDICT.search(seg):
        continue
    captured = False
    for nxt in segs[i + 1:]:
        if re.match(r"^\s*echo\b", nxt) and ("$?" in nxt or "PIPESTATUS" in nxt or "$LASTEXITCODE" in nxt):
            captured = True
            continue
        if captured and READ_ONLY.match(nxt):
            continue
        block(f"`{nxt[:60]}` is chained after the verdict command `{seg[:60]}`: the chain's exit status is its LAST "
              "command's, so the verdict is masked or acted on unread. Run the verdict command alone (redirect it to "
              "a log), read its verdict line, then run the next step as its own call. To capture the status in the "
              "same call, append `; echo \"EXIT=$?\"` (read-only commands may follow that). Independent commands go "
              "as parallel tool calls in one turn.")
        break

sys.exit(0)
