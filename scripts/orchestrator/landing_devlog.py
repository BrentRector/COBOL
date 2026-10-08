#!/usr/bin/env python3
"""landing_devlog.py — EVERY LANDING ON MAIN CARRIES A NEW DEVLOG ENTRY, checked where every landing passes
(scripts/push-main.sh, before a CI run is spent).

    python scripts/orchestrator/landing_devlog.py [--rev HEAD] [--base origin/main]
    python scripts/orchestrator/landing_devlog.py --self-test

THE RULE AND ITS SCOPE (owner decision 2026-10-08 ~07:45 PDT, kb/Work PB2605, asked "does the DEVLOG-entry rule cover
every commit, or only the commits that land on main?", answered verbatim "Only commits landing on main"). DEVLOG.md is
the project's ONLY history (CLAUDE.md rule 6), so a landing that reaches main without an entry loses its narrative.
An implementer's `WIP checkpoint:` commits in its worktree carry none by design: the lander writes ONE entry for the
whole train (MANDATORY-PRACTICES L1). So the rule is checked on the LANDING, and only there.

WHY HERE AND NOT AT `git commit`. The rule used to be a PreToolUse hook (scripts/hooks/devlog_staged.py) that asked
on any commit whose staged set lacked DEVLOG.md. It asked about the WRONG tree: it read `git diff --cached` of the
main checkout the hook file lived in, so a commit in a worktree (`cd <wt> && git commit`, or an agent whose cwd is
its worktree) was judged by main's empty index and passed silently (kb/Work PB2605), and the rule held only for
commits made in the main checkout. Making it follow the commit's own tree would have put an `ask` in front of every
implementer checkpoint (a stall, or a silent pass, in an unattended bypassPermissions unit), and a commit cannot know
whether it will land. A landing can: push-main.sh is THE ONLY WAY a commit reaches main (owner decision Q23,
kb/Work PB796), so this check runs there once, on the landing's own commit range, and nowhere else.

WHAT IT CHECKS, on the trees of the range itself (`git show <base>:DEVLOG.md` against `git show <rev>:DEVLOG.md`,
never an index or a working tree):
  1. at least one NEW entry: an `## Entry <id>` heading whose id is not already on <base>;
  2. each new heading carries the stamp CLAUDE.md rule 6 requires: `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title`;
  3. the new entries sit at the TOP (the file is DESCENDING): no new heading follows a heading already on <base>.
A heading whose id is already on <base> (a stale number after a rebase, MANDATORY-PRACTICES L8) is not new.

Exit 0: the landing carries its entry. Exit 1: REFUSED (each reason printed). Exit 2: the range could not be read.
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile

DEVLOG = "DEVLOG.md"
HEADING = re.compile(r"^## Entry (\S+)")
STAMPED = re.compile(r"^## Entry \d+[a-z]? — \d{4}-\d{2}-\d{2} \d{2}:\d{2} [A-Za-z][A-Za-z0-9+:-]* — \S")
FORMAT = "## Entry NNN — YYYY-MM-DD HH:MM TZ — Title"


class RangeError(Exception):
    """The landing range itself could not be read (not a refusal: the check could not run)."""


def _git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace")


def headings(text: str) -> list[tuple[str, str]]:
    """Every `## Entry <id>` heading of a DEVLOG text, in file order, as (id, line). The file is unnormalized CRLF/LF."""
    out = []
    for line in text.splitlines():
        if m := HEADING.match(line):
            out.append((m.group(1), line))
    return out


def devlog_at(repo: pathlib.Path, rev: str) -> str | None:
    """DEVLOG.md as committed at `rev`; None when that tree has no DEVLOG.md. Raises RangeError on a bad revision."""
    if _git(repo, "rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}").returncode != 0:
        raise RangeError(f"{rev} is not a commit")
    r = _git(repo, "show", f"{rev}:{DEVLOG}")
    return r.stdout if r.returncode == 0 else None


def check(base_text: str | None, rev_text: str | None) -> tuple[list[str], list[str]]:
    """(refusal reasons, the new headings) for a landing whose range runs from `base_text` to `rev_text`."""
    if rev_text is None:
        return [f"the landing's tree has no {DEVLOG}"], []
    old = headings(base_text or "")
    old_ids = {i for i, _ in old}
    now = headings(rev_text)
    new = [(pos, line) for pos, (i, line) in enumerate(now) if i not in old_ids]
    if not new:
        reused = len(now) > len(old)
        return [("the landing adds a DEVLOG heading whose number is already on main (a stale number after a rebase): "
                 "renumber it to the top entry + 1 (MANDATORY-PRACTICES L8)") if reused else
                ("the landing carries no new DEVLOG entry. Every landing on main carries one (owner 2026-10-08, "
                 "kb/Work PB2605): write it at the TOP of DEVLOG.md with `python scripts/prepend-devlog.py "
                 f"<entry.md>`, headed `{FORMAT}`, commit it, and run push-main.sh again")], []
    reasons = [f"the new heading `{line[:90]}` lacks the required stamp `{FORMAT}` (take it from "
               f"`date \"+%Y-%m-%d %H:%M %Z\"`)" for _, line in new if not STAMPED.match(line)]
    if [pos for pos, _ in new] != list(range(len(new))):
        reasons.append("a new DEVLOG entry sits BELOW an entry already on main; DEVLOG.md is DESCENDING, so new "
                       "entries go at the TOP, directly under the ordering note (scripts/prepend-devlog.py)")
    return reasons, [line for _, line in new]


def run(repo: pathlib.Path, base: str, rev: str) -> int:
    try:
        reasons, new = check(devlog_at(repo, base), devlog_at(repo, rev))
    except RangeError as e:
        print(f"⛔ landing devlog: {e}")
        print("=== LANDING DEVLOG: NOT RUN (the range could not be read) ===")
        return 2
    for r in reasons:
        print(f"⛔ landing devlog: {r}")
    if reasons:
        print(f"=== LANDING DEVLOG: REFUSED ({len(reasons)} reason(s)) ===")
        return 1
    print(f"=== LANDING DEVLOG: PASS ({len(new)} new entr{'y' if len(new) == 1 else 'ies'}: {new[0][:72]}) ===")
    return 0


# ── self-test: real repositories, real ranges, and the PB2605 repro against the registered hooks ────────────────
def self_test() -> int:
    # HERMETIC, like status_guard.py's: drop git's repository-selection variables so nothing reaches the caller's repo.
    for name in subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                               text=True).stdout.split():
        os.environ.pop(name, None)
    root = pathlib.Path(tempfile.mkdtemp(prefix="landing-devlog-"))
    repo = root / "repo"
    repo.mkdir()
    env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t",
               GIT_COMMITTER_EMAIL="t@t")
    results: list[tuple[str, bool, str]] = []

    def sh(*args: str, cwd: pathlib.Path = repo) -> str:
        return subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True, text=True,
                              env=env).stdout.strip()

    def write(rel: str, data: bytes, cwd: pathlib.Path = repo) -> None:
        (cwd / rel).write_bytes(data)

    note = b"# DEVLOG\r\n\r\n> **Ordering: DESCENDING.**\r\n\r\n"
    e11 = b"## Entry 11 \xe2\x80\x94 2026-10-08 06:00 PDT \xe2\x80\x94 older train\r\n\r\nbody\r\n\r\n"
    e10 = b"## Entry 10 \xe2\x80\x94 2026-10-07 06:00 PDT \xe2\x80\x94 oldest\n\nbody\n\n"   # unnormalized: LF
    e12 = b"## Entry 12 \xe2\x80\x94 2026-10-08 08:00 PDT \xe2\x80\x94 Train 12: the landing\r\n\r\nbody\r\n\r\n"
    sh("init", "-q", "-b", "main")
    write(DEVLOG, note + e11 + e10)
    write("code.py", b"x = 1\n")
    sh("add", "-A")
    sh("commit", "-qm", "base")
    base = sh("rev-parse", "HEAD")

    def landing(label: str, commits: list[dict[str, bytes]], want: int, *, needle: str = "") -> None:
        """Commit `commits` (each a {path: bytes} edit) on a branch off base, then check base..tip."""
        sh("checkout", "-q", "-B", "landing", base)
        for i, edits in enumerate(commits):
            for p, data in edits.items():
                write(p, data)
            sh("add", "-A")
            sh("commit", "-qm", f"WIP checkpoint: step {i}")
        reasons, _ = check(devlog_at(repo, base), devlog_at(repo, "HEAD"))
        got = 1 if reasons else 0
        ok = got == want and (not needle or any(needle in r for r in reasons))
        results.append((label, ok, "; ".join(reasons) or "PASS"))

    landing("a train: WIP checkpoints with no entry, then the train's one entry at the top -> PASS",
            [{"code.py": b"x = 2\n"}, {"code.py": b"x = 3\n"}, {DEVLOG: note + e12 + e11 + e10}], 0)
    landing("a landing with no DEVLOG change -> REFUSED", [{"code.py": b"x = 4\n"}], 1, needle="no new DEVLOG entry")
    landing("a landing that only edits an existing heading -> REFUSED",
            [{DEVLOG: note + e11.replace(b"older train", b"older train, retitled") + e10}], 1,
            needle="no new DEVLOG entry")
    landing("a new entry whose number is already on main (stale after a rebase) -> REFUSED",
            [{DEVLOG: note + e11.replace(b"older train", b"another train") + e11 + e10}], 1, needle="renumber")
    landing("a new entry without the required stamp -> REFUSED",
            [{DEVLOG: note + b"## Entry 12 - Train 12\r\n\r\n" + e11 + e10}], 1, needle="lacks the required stamp")
    landing("a new entry BELOW an entry already on main -> REFUSED",
            [{DEVLOG: note + e11 + e12 + e10}], 1, needle="BELOW")
    landing("two new entries at the top (a pipelined train) -> PASS",
            [{DEVLOG: note + e12.replace(b"Entry 12", b"Entry 13") + e12 + e11 + e10}], 0)
    sh("checkout", "-q", "-B", "landing", base)
    sh("rm", "-q", DEVLOG)
    sh("commit", "-qm", "drop it")
    reasons, _ = check(devlog_at(repo, base), devlog_at(repo, "HEAD"))
    results.append(("a tree with no DEVLOG.md -> REFUSED", bool(reasons) and "has no" in reasons[0], "; ".join(reasons)))
    try:
        devlog_at(repo, "no-such-rev")
        results.append(("an unreadable revision -> NOT RUN (exit 2), never a pass", False, "no RangeError"))
    except RangeError:
        results.append(("an unreadable revision -> NOT RUN (exit 2), never a pass", True, ""))

    # The PB2605 repro, per the owner's decision: a commit in a WORKTREE with nothing but code staged (an implementer's
    # WIP checkpoint) draws no DEVLOG ask or deny from ANY PreToolUse hook this repository registers, in either shell
    # and either shape (`cd <wt> && git commit`, `git -C <wt> commit`). The rule is the landing's, above.
    project = pathlib.Path(__file__).resolve().parents[2]
    sh("checkout", "-q", "main")
    wt = root / "wt"
    sh("worktree", "add", "-q", "-b", "impl", str(wt), base)
    write("code.py", b"x = 9\n", cwd=wt)
    sh("add", "code.py", cwd=wt)
    settings = json.loads((project / ".claude" / "settings.json").read_text(encoding="utf-8"))
    hooks = [(m.get("matcher", ""), h["command"]) for m in settings["hooks"].get("PreToolUse", [])
             for h in m["hooks"]]
    script = re.compile(r'"\$CLAUDE_PROJECT_DIR/([^"]+)"')
    asked, ran, missing = [], 0, set()
    for tool, command in (("Bash", f"cd {wt.as_posix()} && git commit -m 'WIP checkpoint: x'"),
                          ("Bash", f"git -C {wt.as_posix()} commit -m 'WIP checkpoint: x'"),
                          ("PowerShell", f"Set-Location {wt}; git commit -m 'WIP checkpoint: x'")):
        payload = json.dumps({"hook_event_name": "PreToolUse", "tool_name": tool, "cwd": str(project),
                              "tool_input": {"command": command}})
        for matcher, cmd in hooks:
            if not re.fullmatch(matcher, tool) or not (m := script.search(cmd)):
                continue
            if not (project / m.group(1)).is_file():
                missing.add(m.group(1))
                continue
            ran += 1
            out = subprocess.run([sys.executable, str(project / m.group(1))], input=payload, capture_output=True,
                                 text=True, encoding="utf-8", errors="replace",
                                 env=dict(os.environ, CLAUDE_PROJECT_DIR=str(project))).stdout
            if "DEVLOG" in out:
                asked.append(f"{m.group(1)} on `{command}`")
    staged = sh("diff", "--cached", "--name-only", cwd=wt).split()
    results.append(("the PB2605 repro: a worktree WIP commit without DEVLOG.md draws no DEVLOG ask from any hook",
                    staged == ["code.py"] and not asked and not missing and ran >= 3,
                    f"staged={staged}; asked by: {asked}; hooks run: {ran}; registered but missing: {sorted(missing)}"))

    shutil.rmtree(root, ignore_errors=True)
    bad = [r for r in results if not r[1]]
    for label, ok, got in results:
        print(f"  {'ok ' if ok else 'BAD'} {label}" + ("" if ok else f": {got}"))
    print(f"=== LANDING DEVLOG SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--rev", default="HEAD")
    ap.add_argument("--base", default="origin/main")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    top = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    if top.returncode != 0:
        print("=== LANDING DEVLOG: NOT RUN (not inside a git work tree) ===")
        return 2
    return run(pathlib.Path(top.stdout.strip()), a.base, a.rev)


if __name__ == "__main__":
    sys.exit(main())
