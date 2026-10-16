#!/usr/bin/env python3
"""PreToolUse (Bash | PowerShell): GRANT `git rm -r` inside a git worktree under `.claude/worktrees/` (owner decision
2026-10-07, kb/Work PB2142: "A scoped permission rule allows git rm -r under .claude/worktrees/").

Why a hook and not a settings rule: a `permissions.allow` entry sees only the command text, never the working
directory, so `Bash(git rm -r:*)` would allow the removal in the main checkout too. The auto-mode classifier refused
wave 1025's Cut 2 (`git rm -r` of the five legacy trees) as "Irreversible Local Destruction", correctly for an
unscoped command and wrongly for a worktree branch, where every removal is one commit away from recovery. This hook
returns `permissionDecision: allow` — which the harness honours ahead of the classifier — ONLY when all of these hold:

  1. the call is ONE `git rm` command: an optional location change joined by `&&` (`cd <dir> &&`, `pushd <dir> &&`,
     PowerShell `Set-Location <dir> &&`; read by the shared parser `shell_location.py`, kb/Work PB2599), then
     `git [-C <dir>] rm <options> <paths>`, and nothing else (no `;`, `&&`, `||`, `|` after it, no subshell or `-c`
     payload: an allow covers the whole tool call, so a chain is never allowed here; `;` after a location change is
     refused too, because a failed change would leave the removal running in the original directory), and with no
     substitution or redirect in the git command (`$`, a backquote, `<`, `>`: each runs or writes before git starts);
  2. the directory the removal runs in — `-C <dir>`, else the `cd` target, else the call's `cwd` — resolves to a path
     under `<repo>/.claude/worktrees/` (this hook's repository, derived from its own location, never a literal);
  3. every path argument stays inside that worktree: relative with no `..` segment, or absolute under the worktree;
  4. the options are at most `-r`, `-q`, `--quiet`, `--cached`, `--`: `-f` / `--force` discards uncommitted edits, so
     it falls through to the normal permission flow (the classifier or the person decides).

Anything else: NO decision (exit 0, no output), so the other hooks and the permission system behave exactly as before.
A deny from `forbidden_commands.py` still wins over this allow. Self-test: scripts/hooks/test_worktree_rm_allow.py
(CI's `audits` job runs it with the other hook self-tests).
"""
import json
import os
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import shell_location  # noqa: E402  (the one parser of a command's location changes, kb/Work PB2599)

REPO = pathlib.Path(__file__).resolve().parents[2]
WORKTREES = (REPO / ".claude" / "worktrees").resolve()
ALLOWED_OPTIONS = {"-r", "-q", "--quiet", "--cached", "--"}


def _norm(p: str) -> str:
    return p.strip("\"'").replace("\\", "/")


def _drive_relative(p: str) -> bool:
    """`E:x`: a drive with no root, relative to that drive's own current directory, which this hook cannot know (Bash
    turns an unquoted `E:\\x` into `E:x`): never covered."""
    return bool(pathlib.PureWindowsPath(p).drive) and not shell_location.is_absolute(p)


def _under(path: pathlib.Path, root: pathlib.Path) -> bool:
    try:
        path.resolve().relative_to(root)
        return True
    except ValueError:
        return False


def decide(command: str, cwd: str, shell: str = shell_location.BASH) -> str | None:
    """The reason the call is allowed, or None for no decision."""
    if not command or not cwd:
        return None
    steps = shell_location.steps(command, _norm(cwd), shell)
    # exactly one top-level command, or one location change `&&` the git command; `is_absolute` answers for EITHER
    # platform (on Linux `E:/elsewhere` read as relative and a `-C` target outside the worktree was allowed, 2026-10-07)
    if any(s.depth for s in steps) or not steps or steps[-1].sep:
        return None
    if len(steps) == 2 and steps[0].location and steps[0].sep == "&&":
        step = steps[1]
    elif len(steps) == 1:
        step = steps[0]
    else:
        return None
    # A substitution (`$(...)`, a backquote, `<(...)`, PowerShell's `$(...)`), a redirect or a heredoc runs, writes or
    # reads before git sees its argv, and an allow covers the whole call: none is ever covered (train 1039b review).
    if step.cwd is None or step.heredocs or any(ch in step.text for ch in ("$", "`", "<", ">")):
        return None
    # git's argv is the shared parser's words of the step (kb/Work PB3198: one model of the command line, never a second
    # splitter): quotes removed as the shell removes them, so the paths judged are the paths git receives.
    # Its git invocation is `git_calls`' reading (the same one every guard rule reads): the step must BE that call, and
    # its only global options `-C <dir>` (any other, `-c`, `--git-dir`, `--work-tree`, changes what `rm` acts on).
    calls = shell_location.git_calls(step)
    if (not step.words or step.words[0] not in ("git", "git.exe") or len(calls) != 1 or calls[0].subcommand != "rm"
            or calls[0].cwd is None):
        return None
    call = calls[0]
    flags, dirs = call.options[0::2], call.options[1::2]
    if len(flags) != len(dirs) or any(f != "-C" for f in flags) or any(_drive_relative(_norm(d)) for d in dirs):
        return None
    base = pathlib.Path(_norm(call.cwd))
    opts, paths = [], []
    for a in (_norm(w) for w in call.args):
        (opts if a.startswith("-") and a != "--" or a == "--" else paths).append(a)
    if not paths or any(o not in ALLOWED_OPTIONS for o in opts) or "-r" not in opts:
        return None
    base = base.resolve()
    if not _under(base, WORKTREES) or base == WORKTREES:
        return None
    worktree = WORKTREES / base.relative_to(WORKTREES).parts[0]
    for p in paths:
        if _drive_relative(p):
            return None
        if shell_location.is_absolute(p):
            if not _under(pathlib.Path(p), worktree):
                return None
        elif ".." in pathlib.PurePosixPath(p).parts:
            return None
    return (f"git rm -r inside the worktree {worktree.name} (scoped permission, owner 2026-10-07, kb/Work PB2142): "
            f"every removed file is in the branch's history, one commit from recovery")


def main() -> int:
    try:
        data = json.load(sys.stdin)
    except Exception:  # noqa: BLE001 — a hook must never wedge a session
        return 0
    cmd = (data.get("tool_input") or {}).get("command", "") or ""
    try:
        reason = decide(cmd, data.get("cwd") or os.getcwd(), shell_location.shell_of(data.get("tool_name")))
    except Exception:  # noqa: BLE001 — a parser failure is NO decision (the normal permission flow), never an allow
        return 0
    if reason:
        sys.stdout.write(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "allow",
                                                             "permissionDecisionReason": reason}}))
    return 0


if __name__ == "__main__":
    sys.exit(main())
