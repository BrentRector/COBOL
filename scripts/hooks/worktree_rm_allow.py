#!/usr/bin/env python3
"""PreToolUse (Bash | PowerShell): GRANT `git rm -r` inside a git worktree under `.claude/worktrees/` (owner decision
2026-10-07, kb/Work PB2142: "A scoped permission rule allows git rm -r under .claude/worktrees/").

Why a hook and not a settings rule: a `permissions.allow` entry sees only the command text, never the working
directory, so `Bash(git rm -r:*)` would allow the removal in the main checkout too. The auto-mode classifier refused
wave 1025's Cut 2 (`git rm -r` of the five legacy trees) as "Irreversible Local Destruction", correctly for an
unscoped command and wrongly for a worktree branch, where every removal is one commit away from recovery. This hook
returns `permissionDecision: allow` — which the harness honours ahead of the classifier — ONLY when all of these hold:

  1. the call is ONE `git rm` command: an optional `cd <dir> &&` prefix, then `git [-C <dir>] rm <options> <paths>`,
     and nothing else (no `;`, `&&`, `||`, `|` after it: an allow covers the whole tool call, so a chain is never
     allowed here);
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
import re
import shlex
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
WORKTREES = (REPO / ".claude" / "worktrees").resolve()
ALLOWED_OPTIONS = {"-r", "-q", "--quiet", "--cached", "--"}
CHAIN = re.compile(r"(\|\||&&|;|\|)")
CD = re.compile(r"^(?:cd|Set-Location)\s+(\S+)\s*$")


def _norm(p: str) -> str:
    return p.strip("\"'").replace("\\", "/")


def _is_abs(p: str) -> bool:
    """Absolute on EITHER platform: a POSIX root or a drive letter. `pathlib.Path.is_absolute` answers for the host only,
    so on Linux `E:/elsewhere` read as relative and a `-C` target outside the worktree was allowed (CI red, 2026-10-07)."""
    return pathlib.PurePosixPath(p).is_absolute() or pathlib.PureWindowsPath(p).is_absolute()


def _under(path: pathlib.Path, root: pathlib.Path) -> bool:
    try:
        path.resolve().relative_to(root)
        return True
    except ValueError:
        return False


def decide(command: str, cwd: str) -> str | None:
    """The reason the call is allowed, or None for no decision."""
    if not command or not cwd:
        return None
    parts = [s.strip() for s in CHAIN.split(command.strip()) if s.strip()]
    # parts alternates segment, operator, segment …; exactly one segment, or `cd X` && `git …`
    if len(parts) == 1:
        prefix, git_cmd = None, parts[0]
    elif len(parts) == 3 and parts[1] == "&&":
        prefix, git_cmd = parts[0], parts[2]
    else:
        return None
    base = pathlib.Path(_norm(cwd))
    if prefix is not None:
        m = CD.match(prefix)
        if not m:
            return None
        target = _norm(m.group(1))
        base = pathlib.Path(target) if _is_abs(target) else base / target
    try:
        argv = shlex.split(git_cmd.replace("\\", "/"), posix=True)
    except ValueError:
        return None
    if not argv or argv[0] not in ("git", "git.exe"):
        return None
    i = 1
    if i < len(argv) and argv[i] == "-C":
        if i + 1 >= len(argv):
            return None
        target = argv[i + 1]
        base = pathlib.Path(target) if _is_abs(target) else base / target
        i += 2
    if i >= len(argv) or argv[i] != "rm":
        return None
    opts, paths = [], []
    for a in argv[i + 1:]:
        (opts if a.startswith("-") and a != "--" or a == "--" else paths).append(a)
    if not paths or any(o not in ALLOWED_OPTIONS for o in opts) or "-r" not in opts:
        return None
    base = base.resolve()
    if not _under(base, WORKTREES) or base == WORKTREES:
        return None
    worktree = WORKTREES / base.relative_to(WORKTREES).parts[0]
    for p in paths:
        if _is_abs(p):
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
    reason = decide(cmd, data.get("cwd") or os.getcwd())
    if reason:
        sys.stdout.write(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "allow",
                                                             "permissionDecisionReason": reason}}))
    return 0


if __name__ == "__main__":
    sys.exit(main())
