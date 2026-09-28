#!/usr/bin/env python3
"""status_delta.py — how much of a branch's history its STATUS.md handoff does NOT cover.

    python scripts/spec/status_delta.py <worktree>                 # STATUS.md and HEAD of that worktree
    python scripts/spec/status_delta.py --status <file> --ref <branch-or-sha> [--base <ref>]

A handoff summary (STATUS.md) is trusted for NAVIGATION, never as evidence (kb/Work/PB1698). Its weak point is
staleness: an agent that dies after a commit but before rewriting STATUS.md leaves a summary that silently omits
the last commits, and a follow-on agent cannot tell without re-reading the whole branch. The fix is a stamp: every
STATUS.md carries

    STATUS-AT: <full sha of the commit this status describes>

written AFTER the checkpoint commit (STATUS.md is gitignored, so writing it never moves HEAD). This script turns the
stamp into the exact reading list:

  CURRENT   (exit 0) — the stamp is HEAD: the summary covers every commit; read it, then only the uncommitted work.
  STALE     (exit 1) — the stamp is an ancestor of HEAD: read the summary PLUS only the commits listed after it.
  UNSTAMPED / DIVERGED (exit 2) — coverage unknown: read every commit since the base (listed), as before the stamp.

Uncommitted changes in the worktree are always listed: no summary covers them.
"""
import argparse, pathlib, re, subprocess, sys

STAMP = re.compile(r"^\s*STATUS-AT:\s*([0-9a-fA-F]{7,40})\b", re.M)


def git(cwd, *args, check=True):
    r = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8")
    if check and r.returncode != 0:
        sys.exit(f"git {' '.join(args)}: {r.stderr.strip()}")
    return r


def log_since(cwd, since, head):
    out = git(cwd, "log", "--reverse", "--format=%h %s", "--stat=120", f"{since}..{head}").stdout.rstrip()
    n = int(git(cwd, "rev-list", "--count", f"{since}..{head}").stdout.strip() or 0)
    return n, out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("worktree", nargs="?", help="a worktree holding STATUS.md")
    ap.add_argument("--status", help="a STATUS.md file (with --ref)")
    ap.add_argument("--ref", help="the branch or sha the status describes")
    ap.add_argument("--base", default="origin/main", help="the fallback base for an unstamped status")
    a = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    if a.worktree:
        cwd = pathlib.Path(a.worktree)
        status, ref = cwd / "STATUS.md", "HEAD"
    elif a.status and a.ref:
        cwd, status, ref = pathlib.Path.cwd(), pathlib.Path(a.status), a.ref
    else:
        ap.error("give a worktree, or --status and --ref")

    head = git(cwd, "rev-parse", ref).stdout.strip()
    base = git(cwd, "merge-base", a.base, head).stdout.strip()
    text = status.read_text(encoding="utf-8", errors="replace") if status.exists() else ""
    m = STAMP.search(text)
    print(f"status : {status}{'' if status.exists() else '  (MISSING)'}")
    print(f"head   : {head[:12]}   base: {base[:12]} ({a.base})")

    code = 2
    if not m:
        n, out = log_since(cwd, base, head)
        print(f"UNSTAMPED — the summary's coverage is unknown. Read ALL {n} commit(s) since the base:")
        print(out or "  (none)")
    else:
        r = git(cwd, "rev-parse", "--verify", "--quiet", m.group(1) + "^{commit}", check=False)
        stamp = r.stdout.strip()
        if not stamp or git(cwd, "merge-base", "--is-ancestor", stamp, head, check=False).returncode != 0:
            n, out = log_since(cwd, base, head)
            print(f"DIVERGED — stamp {m.group(1)} is not in this branch's history (rebased or amended). "
                  f"Read ALL {n} commit(s) since the base:")
            print(out or "  (none)")
        elif stamp == head:
            code = 0
            print(f"CURRENT — STATUS.md describes HEAD ({head[:12]}): it covers every commit. Trust it for navigation.")
        else:
            code = 1
            n, out = log_since(cwd, stamp, head)
            print(f"STALE by {n} commit(s) — STATUS.md describes {stamp[:12]}. Read the summary, then ONLY these:")
            print(out)

    if a.worktree:
        dirty = git(cwd, "status", "--short", "--", ".", ":!.claude/settings.local.json", ":!STATUS.md").stdout.rstrip()
        print("uncommitted: " + ("none" if not dirty else "\n" + dirty))
    return code


if __name__ == "__main__":
    sys.exit(main())
