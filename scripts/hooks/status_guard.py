#!/usr/bin/env python3
"""status_guard.py — keep an agent's STATUS.md handoff describing its CURRENT commit, mechanically.

The handoff rule: after every checkpoint commit, rewrite STATUS.md with first line `STATUS-AT: <sha of HEAD>`. It was
a written instruction, and it failed silently. Measured: 3 of 14 finished implementer branches (21 %) ended with a
STATUS.md that did not describe their own last commit, with no crash involved; the usual shape is a small final
commit (a gate-red fix, a regenerated index) made after the last status rewrite. A resuming agent then trusts a
summary that is wrong. This hook turns the rule into two checks that fire ONLY on a real violation:

  PreToolUse  (a command that runs `git commit`)  STATUS.md does not describe HEAD → REFUSE the commit until it is
                                                  rewritten, so at most one commit is ever undescribed.
  Stop / SubagentStop                             STATUS.md does not describe HEAD → REFUSE to end the turn (once; a
                                                  second stop with stop_hook_active is let through, so a broken repo
                                                  cannot trap the agent).

There is deliberately NO reminder after each command. Measured in a sandbox A/B (2026-09-28, n = 10 per arm), a
PostToolUse reminder keyed on state fired in the ordinary window between a commit and the agent's own rewrite (0–36
times per session) and cost about 20 % more turns and dollars, while every arm ended with a current STATUS.md. These
two checks cost nothing when the rule is followed.

It acts only where the protocol is in use: a git work tree whose top level holds STATUS.md. Anywhere else it is
silent. A STATUS.md without a stamp line counts as not describing HEAD. Any internal error lets the action through
(fail open): a broken guard must never block work.

Register it for PreToolUse on the shell tools WITHOUT an `if` filter, and for Stop and SubagentStop without a
matcher. Agents commit through chains (`git add -A && git commit …`), which an `if: Bash(git commit*)` filter does
not match (measured: it silenced the hook in most sessions); the script finds `git commit` anywhere in the command,
and checks the tree that commit runs in: the shared parser `shell_location.py` follows the command's location changes
(`cd <worktree> && git commit`, `Set-Location <worktree>; git commit`) and `git -C <dir>`, so an agent that addresses
its worktree by path from the main checkout is checked against ITS STATUS.md, not the main tree's absent one (which
left the guard silent for exactly those agents, kb/Work PB2599). A directory the line does not determine falls back
to the payload's cwd.
A commit made inside a script the agent runs is invisible to PreToolUse, and the Stop check catches it.
`--self-test` exercises every branch in a throwaway repository.
"""
import json, os, pathlib, re, subprocess, sys, tempfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import shell_location  # noqa: E402  (the one parser of a command's location changes, kb/Work PB2599)

STAMP = re.compile(r"^\s*STATUS-AT:\s*([0-9a-fA-F]{7,40})\b", re.M)
COMMIT = re.compile(r"(^|[;&|(]\s*|\s)git(\s+-C\s+\S+)?\s+commit(?![-\w])")


def git(cwd, *args):
    r = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8")
    return r.stdout.strip() if r.returncode == 0 else None


def state(cwd):
    """(top, head, stamp_sha_or_None, status_exists) or None when the protocol is not in use here."""
    top = git(cwd, "rev-parse", "--show-toplevel")
    if not top:
        return None
    status = pathlib.Path(top) / "STATUS.md"
    if not status.exists():
        return None
    head = git(top, "rev-parse", "HEAD")
    if not head:
        return None
    m = STAMP.search(status.read_text(encoding="utf-8", errors="replace"))
    stamp = git(top, "rev-parse", "--verify", "--quiet", m.group(1) + "^{commit}") if m else None
    return top, head, stamp


def describe(head, stamp):
    if stamp is None:
        return (f"STATUS.md has no valid `STATUS-AT:` stamp, so it does not describe HEAD {head[:12]}. Rewrite it now "
                f"(DONE / NEXT / BLOCKED / GATE) with first line `STATUS-AT: {head}`.")
    return (f"STATUS.md describes {stamp[:12]} but HEAD is {head[:12]}: the last commit is not in the handoff. Rewrite "
            f"STATUS.md now so DONE / NEXT / GATE cover it, with first line `STATUS-AT: {head}`.")


def commit_dirs(payload, cwd):
    """The directories the call's `git commit` commands run in (location changes and `-C` followed); `cwd` stands in
    for one the command line does not determine."""
    cmd = (payload.get("tool_input") or {}).get("command", "")
    found = [shell_location.git_dir(s) or cwd
             for s in shell_location.steps(cmd, cwd, shell_location.shell_of(payload.get("tool_name")))
             if COMMIT.search(s.text)]
    return list(dict.fromkeys(found)) or [cwd]


def decide(payload):
    """Returns (stdout_json_or_None). Never raises past main()."""
    event = payload.get("hook_event_name", "")
    cwd = payload.get("cwd") or os.getcwd()
    if event == "PreToolUse":
        cmd = (payload.get("tool_input") or {}).get("command", "")
        if not COMMIT.search(cmd):
            return None
        dirs = commit_dirs(payload, cwd)
    elif event in ("Stop", "SubagentStop"):
        dirs = [cwd]
    else:
        return None
    stale = None
    for d in dirs:
        st = state(d)
        if st is not None and st[2] != st[1]:
            stale = st
            break
    if stale is None:
        return None
    top, head, stamp = stale
    msg = describe(head, stamp)
    if event == "PreToolUse":
        return {"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny",
                                       "permissionDecisionReason": "Commit refused: " + msg + " Then commit again."}}
    if event in ("Stop", "SubagentStop"):
        if payload.get("stop_hook_active"):
            return None
        return {"decision": "block", "reason": "Before you finish: " + msg}
    return None


def main():
    if "--self-test" in sys.argv:
        return self_test()
    try:
        payload = json.loads(sys.stdin.read() or "{}")
        out = decide(payload)
    except Exception:
        return 0
    if out is not None:
        sys.stdout.write(json.dumps(out))
    return 0


def self_test():
    # HERMETIC, like gate_slot.py's: the self-test builds its own repository, so a caller's repository selection
    # (GIT_DIR/GIT_INDEX_FILE from a git hook, or any exported GIT_DIR/GIT_WORK_TREE) would send its init, config and commit to the
    # caller's REAL repository. Git names the variables itself (kb/Work PB1719, 2026-09-29).
    for name in subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                               text=True).stdout.split():
        os.environ.pop(name, None)
    ok = True

    def check(name, got, want_kind):
        nonlocal ok
        kind = ("none" if got is None else "deny" if "permissionDecision" in json.dumps(got)
                else "block" if got.get("decision") == "block" else "context")
        good = kind == want_kind
        ok &= good
        print(("ok      " if good else "FAIL    ") + f"{name}: {kind} (want {want_kind})")

    with tempfile.TemporaryDirectory() as d:
        run = lambda *a: subprocess.run(["git", *a], cwd=d, capture_output=True, check=True)
        run("init", "-q"); run("config", "user.email", "t@t"); run("config", "user.name", "t")
        p = pathlib.Path(d)
        (p / "a.txt").write_text("1"); run("add", "a.txt"); run("commit", "-qm", "one")
        commit = lambda ev, **kw: decide({"hook_event_name": ev, "cwd": d, "tool_input": {"command": "git commit -m x"}, **kw})
        check("no STATUS.md: pre-commit silent", commit("PreToolUse"), "none")
        check("no STATUS.md: stop silent", decide({"hook_event_name": "Stop", "cwd": d}), "none")
        (p / "STATUS.md").write_text("DONE: one\n")
        check("unstamped: pre-commit denied", commit("PreToolUse"), "deny")
        head = git(d, "rev-parse", "HEAD")
        (p / "STATUS.md").write_text(f"STATUS-AT: {head}\nDONE: one\n")
        check("current: pre-commit allowed", commit("PreToolUse"), "none")
        check("current: stop allowed", decide({"hook_event_name": "SubagentStop", "cwd": d}), "none")
        (p / "a.txt").write_text("2"); run("commit", "-qam", "two")
        check("stale: a PostToolUse event is ignored (no per-command reminder)", commit("PostToolUse"), "none")
        check("stale: next commit denied", commit("PreToolUse"), "deny")
        check("stale: stop blocked", decide({"hook_event_name": "Stop", "cwd": d}), "block")
        check("stale: second stop let through", decide({"hook_event_name": "Stop", "cwd": d, "stop_hook_active": True}), "none")
        check("non-commit command ignored", decide({"hook_event_name": "PreToolUse", "cwd": d, "tool_input": {"command": "git status"}}), "none")
        check("chained commit detected", decide({"hook_event_name": "PreToolUse", "cwd": d, "tool_input": {"command": "git add -A && git commit -m y"}}), "deny")
        check("commit-tree not a commit", decide({"hook_event_name": "PreToolUse", "cwd": d, "tool_input": {"command": "git commit-tree abc"}}), "none")
        (p / "STATUS.md").write_text("STATUS-AT: 0123456789abcdef0123456789abcdef01234567\n")
        check("unknown sha stamp: denied", commit("PreToolUse"), "deny")
        # kb/Work PB2599: the commit's own tree, reached by a location change or -C from a directory with no STATUS.md
        with tempfile.TemporaryDirectory() as away:
            pd = p.as_posix()
            pre = lambda command, tool="Bash": decide({"hook_event_name": "PreToolUse", "cwd": away, "tool_name": tool,
                                                       "tool_input": {"command": command}})
            check("stale, reached by `cd <tree> && git commit`: denied", pre(f"cd {pd} && git add -A && git commit -m y"), "deny")
            check("stale, reached by `git -C <tree> commit`: denied", pre(f"git -C {pd} commit -m y"), "deny")
            check("stale, reached by PowerShell `Set-Location <tree>; git commit`: denied",
                  pre(f"Set-Location '{p}'; git commit -m y", "PowerShell"), "deny")
            check("a commit elsewhere after visiting the tree in a subshell: silent", pre(f"(cd {pd} && ls) && git commit -m y"), "none")
    with tempfile.TemporaryDirectory() as d2:
        check("not a git repo: silent", decide({"hook_event_name": "Stop", "cwd": d2}), "none")
    print("=== SELF-TEST: " + ("GREEN" if ok else "RED") + " ===")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
