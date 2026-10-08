#!/usr/bin/env python3
"""PreToolUse hook — BLOCK the shell commands this repo's rules forbid, with the reason fed back to the agent.

Owner decision 2026-09-25 (tooling recommendation 2): prose rules are forgotten, a hook is not, and a PreToolUse hook
fires inside subagents and Workflow agents too. Each rule below was written in a skill or memory AND broken anyway:

  git stash / --autostash   the stash stack is SHARED by every linked worktree (common git dir); PB713's implementer
                            popped the registrar's stash. WIP goes into a commit (workstream skill, references/landing.md).
  bare push to main         main requires the ci-gate check; the only route is `bash scripts/push-main.sh`
                            (owner decision Q23, 2026-09-06). A push to ci/<sha> or any other branch is fine.
  direct push in a unit     inside an orchestrator unit (COBOL_COORD_DIR set) ANY `git push`, any repository, any
                            flags: a unit lands through `bash scripts/push-main.sh` and never publishes another repo
                            (owner 2026-10-05, kb/Work/PB2044: the wave unit pushed the public skills repo's main with
                            `git push -q origin HEAD:main`, which the deny rule's text pattern missed).
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

  WSL lifecycle commands    `wsl --terminate/--shutdown/--update/--export/--import/--unregister/--install` (owner
                            2026-10-04): they can end the owner's open session or burn tens of GB, so only the owner runs
                            them. Commands inside a distro (`wsl -d Ubuntu -e ...`) stay allowed. This is the guard that
                            matters most under an unattended `bypassPermissions` run, where prompts no longer protect.

Exit 2 blocks the call and returns stderr to the agent. Anything unparseable passes (a hook must never wedge a session).
"""
import json
import os
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import shell_location  # noqa: E402  (the one parser of a command's location changes, kb/Work PB2599)


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
_ROOT_DIR = pathlib.Path(__file__).resolve().parents[2]
_ROOT = _ROOT_DIR.name.lower()
_THIS_REPO = {_ROOT, "cobol", "cobolsharp"}
# A path registered in .gitmodules is ANOTHER repository nested under this one (tools/claude-skills is the public
# skills repo, whose main has no ci-gate), so a push of ITS main is not a push to this repo's main. The folder-name
# rule below would otherwise catch it, because the submodule's path contains the repo's name (2026-10-06).
_GITMODULES = _ROOT_DIR / ".gitmodules"
_SUBMODULES = [tuple(p.lower().split("/")) for p in re.findall(r"^\s*path\s*=\s*(\S+)",
               _GITMODULES.read_text(encoding="utf-8"), re.M)] if _GITMODULES.exists() else []


def _is_this_repo(target: str) -> bool:
    t = target.lower().replace("\\", "/")
    segs = [s for s in t.split("/") if s]
    if any(len(segs) >= len(sub) and tuple(segs[-len(sub):]) == sub for sub in _SUBMODULES):
        return False  # a registered submodule, absolute or relative: another repository
    if not (t.startswith(("/", "~")) or re.match(r"[a-z]:", t)):
        return True
    return any(s in _THIS_REPO or any(s.startswith(n + "-") for n in _THIS_REPO) for s in segs)


# Every location change either shell accepts (`cd`, `pushd`, `Set-Location`/`sl`/`chdir`/`Push-Location`, `-Path`,
# quoted paths with spaces, `-WorkingDirectory`), read by the one shared parser (kb/Work PB2599), plus git's own `-C`.
try:
    cds = shell_location.location_targets(commands, shell_location.shell_of(data.get("tool_name")))
except Exception:  # noqa: BLE001 — no target read means no other repo: the push rule still applies (fails closed)
    cds = []
cds += re.findall(r"\bgit\s+-C\s+[\"']?([^\s;&|\"']+)", commands)
other_repo = any(not _is_this_repo(c) for c in cds)
for m in ([] if other_repo else re.finditer(r"\bgit\s+(?:-C\s+\S+\s+)?push\b([^;&|\n]*)", commands)):
    args = m.group(1)
    if re.search(r"(?:^|\s|:)(?:refs/heads/)?main(?:\s|$)", args):
        block("a direct push to main is refused by the server (required `ci-gate` check). Land with "
              "`bash scripts/push-main.sh` — it pushes ci/<sha>, waits for green, then fast-forwards main. "
              "It is idempotent; run it with run_in_background and block on its log.")
# 2b. ANY direct `git push`, in ANY repository, inside an orchestrator unit (owner 2026-10-05, kb/Work/PB2044).
# Rule 2 guards THIS repo's main and deliberately leaves another repository to its own rules, so an attended session may
# push the sites and the public skills repo. A unit has no such need and runs under `bypassPermissions`: on 2026-10-05 the
# wave unit published the public skills repo's main with `git push -q origin HEAD:main` from a scratch clone, and the
# project's deny rule (a TEXT pattern, `git push origin HEAD:main`) missed it because of the extra `-q`. A unit lands
# through `bash scripts/push-main.sh`, whose command text contains no `git push`; the supervisor exports
# COBOL_COORD_DIR into every unit's environment and hooks (also in its subagents) inherit it. Attended sessions and
# cloud sessions never have it set, so their pushes are untouched. The pattern is the git SUBCOMMAND after any global
# options (`-C dir`, `-c k=v`, `--no-pager`), so flags before or after cannot dodge it and `git log --grep=push` is not hit.
_PUSH = re.compile(r"\bgit(?:\s+(?:-C\s+\S+|-c\s+\S+|--[\w-]+(?:=\S+)?))*\s+push\b")
_QUOTED = re.compile(r"\"[^\"]*\"|'[^']*'")
# the payload of `bash -c '…'` / `pwsh -Command "…"` IS a command line, so it is scanned; every other quoted span is text
# (a commit message that says "git push" is not a push)
_SHELL_PAYLOAD = re.compile(r"\b(?:bash|sh|zsh|pwsh|powershell)(?:\.exe)?\s+(?:-\w+\s+)*-(?:c|lc|Command)\s+([\"'])(.*?)\1", re.S)


def _without_heredoc_bodies(text: str) -> str:
    """Everything except the BODIES of heredocs. `commands` above stops at the first `<<`, so a push written AFTER a heredoc
    (`cat > msg <<'EOF' … EOF` then `git commit -F msg; git push …`, the shape the unit used) would never be scanned."""
    out, pos = [], 0
    for m in re.finditer(r"<<-?\s*[\"']?(\w+)[\"']?[^\n]*\n", text):
        if m.start() < pos:
            continue
        out.append(text[pos:m.end()])
        end = re.search(r"^\s*" + re.escape(m.group(1)) + r"\s*$", text[m.end():], re.M)
        pos = m.end() + (end.end() if end else len(text) - m.end())
    out.append(text[pos:])
    return "".join(out)


if os.environ.get("COBOL_COORD_DIR"):
    scan = _without_heredoc_bodies(cmd)
    payloads = [m.group(2) for m in _SHELL_PAYLOAD.finditer(scan)]
    if any(_PUSH.search(t) for t in [_QUOTED.sub(" ", scan)] + payloads):
        block("a direct `git push` is refused inside an orchestrator unit (any repository, any flags). Land this "
              "repository's work with `bash scripts/push-main.sh`. A change to ANOTHER repository (the public skills "
              "repo, a site) is never published by a unit: hand it off as an owed push in the handoff and the "
              "attended session or the owner publishes it (kb/Work/PB2044).")
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
                  "green. Write `FullyQualifiedName~X` for every term. (A GATE never filters: it is "
                  "`scripts/build-local.ps1 -Mode implementer`, the ordered whole population.)")
    if not re.search(r"(>|\|\s*(tail|grep|Select-String|Tee-Object|tee|findstr|Out-File))", commands):
        block("a filtered `dotnet test` must redirect its output to a log (or pipe it through tail/grep) and read the "
              "verdict line — unredirected output floods the context and hides the verdict. A GATE is "
              "`scripts/build-local.ps1 -Mode implementer`, never a filtered run.")

# 5. chaining after a verdict command
# A verdict command is a command that RUNS one: `dotnet test|build`, or a gate / landing script invoked directly or through
# an interpreter (`bash scripts/push-main.sh`, `pwsh -File scripts/build-local.ps1`). A command that merely NAMES one of
# those files (`git add -- scripts/push-main.sh`, `sed -n 1,5p scripts/push-main.sh`) is not one: matching anywhere in the
# segment blocked such harmless commands and cost extra turns (2026-10-04).
_GATE_SCRIPT = r"(?:\./)?\S*(?:build-local\.(?:ps1|sh)|push-main\.sh|battery\.sh|guard(?:-fast)?\.sh)\b"
VERDICT = re.compile(r"^(?:\w+=\S*\s+)*(?:dotnet\s+(?:test|build)\b|"
                     r"(?:(?:bash|sh|pwsh|powershell)(?:\.exe)?\s+(?:-\S+\s+)*)?" + _GATE_SCRIPT + r")")
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

# 6. WSL lifecycle commands (owner 2026-10-04): `wsl --terminate/--shutdown/--update/--export/--import/--unregister/--install`
# can end the owner's open session, race a procedure they run by hand, or burn tens of GB of disk, so NO agent or unattended
# unit runs them; the owner does. Reads and commands INSIDE a distro (`wsl -d Ubuntu -e ...`) stay allowed. Only a segment
# whose command IS wsl is checked, so a commit message or a grep pattern that mentions the flags passes.
WSL_LIFECYCLE = re.compile(r"^(?:\w+=\S*\s+)*(?:&\s+)?(?:\S*[\\/])?wsl(?:\.exe)?\s+(?:[^|;&]*\s)?"
                           r"(?:--terminate|-t|--shutdown|--update|--export|--import|--unregister|--install)\b")
for seg in segs:
    if WSL_LIFECYCLE.match(seg):
        block("WSL lifecycle commands (`--terminate`, `--shutdown`, `--update`, `--export`, `--import`, `--unregister`, "
              "`--install`) are the OWNER's to run (owner 2026-10-04: they can end an open session or burn disk). Give the "
              "owner the exact command and wait; commands inside a distro (`wsl -d Ubuntu -e ...`) are fine.")

for i, seg in enumerate(segs[:-1]):
    if not VERDICT.match(seg):
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
