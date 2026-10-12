#!/usr/bin/env python3
"""enqueue.py — THE LANDING STEP, A SCRIPT: one finished branch, brought up to main, gated, its DEVLOG entry stamped,
checked, and handed to GitHub's merge queue as one pull request (kb/Work PB3023, PB3024; DESIGN-test-build-ci.md
sections 3.17.2 step 3, 3.17.5 and 3.17.6). No agent lands anything: the loop's `enqueue` unit runs this per WAITING TO LAND
branch, and an agent is dispatched only for what a script cannot decide (a conflict, a red gate, a queue bounce).

    python scripts/orchestrator/enqueue.py land <branch> [--worktree <path>] [--lane L] [--dry-run]  THE landing step
    python scripts/orchestrator/enqueue.py queue --title "<one-line subject>" --body-file <forensic message> [--dry-run]
    python scripts/orchestrator/enqueue.py checks [--rev HEAD] [--base origin/main]
    python scripts/orchestrator/enqueue.py --self-test

`land <branch>`, from any checkout (the branch's worktree is found by `git worktree list`, or named by --worktree):
  1. The worktree must be clean (STATUS.md and the harness's settings.local.json aside): a landing carries only commits.
     THE QUEUE GUARD, before anything is merged or gated: `ruleset.py check` must report the merge queue ACTIVE, or the
     exit is 8 with the branch unchanged. Main accepts only queued pull requests (the ruleset binds administrators too),
     so there is no other path to land by, and a gate spent while the queue is not ACTIVE could land nothing (kb/Work
     PB3023 review R1-1: the former push-main.sh arm merged and gated for half an hour with no landing lease held).
     THE REVIEW (MANDATORY-PRACTICES I10, L9; kb/Work PB3027): the branch's newest fix-lane report must carry
     `**Review:** CLEAN <reviewer> <head>` naming a commit of the branch, and every later commit must be main's, a clean
     merge of main (its tree the auto-merge of its parents) or DEVLOG-only; anything else (a finisher's fix, a
     hand-resolved conflict) is unreviewed: exit 11, the report gains `BLOCKED: enqueue — not reviewed …`, and the new
     tip goes to a reviewer. Only the lanes I10 does not review (UNREVIEWED_LANES: the registrar, the golden lane, the
     clerk) skip it, naming themselves with `--lane`.
  2. `git fetch` origin's main and, when the branch does not already contain it, MERGE it into the branch (never a
     rebase: a branch that merged a same-file predecessor or a sibling carries conflict resolutions in its merge
     commits, which a rebase throws away and asks for again; the queue lands by merge commit anyway, so the branch's
     history stays as written). A conflict is aborted, the branch's report gains `BLOCKED: … conflict in <files>`, and
     the exit is 4: a finisher resolves it.
  3. THE GATE on the merged head, every host in GATES, each under the shared gate slot `build-local.ps1` takes (the cap
     of 3, so concurrent enqueues queue there and never beyond it): a verdict that is not GREEN (or no verdict line) is
     exit 5, the report gains `BLOCKED: <host> gate … at enqueue — <verdict>`. A green head is recorded in the worktree's
     git directory (`enqueue-gated`); a later run whose head differs from it only by commits that are main's, clean
     merges or DEVLOG-only (`prune_worktrees.unreviewed_since`, the review's coverage rule) is not gated again.
  4. MAIN AGAIN, then the stamp, then `queue` (below). Main moves while a branch gates (the queue merges other pull
     requests), and `queue` takes only a head that descends from main; so origin/main is fetched again and, when it
     moved, MERGED in once more, without a second gate (kb/Work PB3023 review R1-2): the local gate confirms the change
     (step 3), and the merge group, which builds this pull request on the current main, runs the whole population
     (DESIGN-test-build-ci.md section 3.17.2 step 4). A conflict here is exit 4, as in step 2. Then `devlog.py stamp
     <branch>`: the branch's `DEVLOG/pending-<slug>.md` is numbered (alloc.py devlog: enqueue order is number order) and
     stamped in one commit, and an entry main has overtaken is re-stamped. Then `queue`, the pull request titled with
     that entry's title and carrying the entry and the commit list as its body. Should main move again between the
     fetch and `queue`'s own (seconds), step 4 runs again, at most CATCH_UP times.
Every outcome ends on ONE line `=== ENQUEUE: <OUTCOME> … ===` for a caller to block on (MANDATORY-PRACTICES P2).

`queue`, run in the landing worktree on its committed, up-to-date HEAD:
  1. ⛔ THE QUEUE GUARD, FIRST: `ruleset.py check` must report the merge queue ACTIVE on main, or nothing is pushed or
     opened (exit 8). The operator's review (2026-10-11 13:39): a pull request runs only the cheap CI jobs, so its
     `ci-gate` passes on them; opened before the ruleset exists, `gh pr merge --auto` would merge it with NO
     whole-population run. LandingPipelineDriftTests holds that no other file in the tree opens or queues a pull
     request, so this guard is the only door.
  2. HEAD must be on a branch (not main, not detached) and a descendant of a freshly fetched origin/main (`land` merges
     it in; exit 2 otherwise).
  3. `checks` (below) on origin/main..HEAD.
  4. Push HEAD to the branch of the same name on origin (forced: the branch is this landing's own and moves on a
     rebase), reuse the branch's open pull request or open one (`gh pr create`), and queue it (`gh pr merge --auto`; the
     merge method is the queue's own, MERGE, so the branch's commits stay ancestors of main).
  5. Print `ENQUEUED pr=<n> sha=<sha> branch=<branch> url=<url>` and exit 0. It never waits for CI: the merge group
     runs the matrix, and a red there bounces only this PR.

`checks` — THE LANDING CHECKS, one sequence in one place (push-main.sh runs it too until PB3024 deletes it), each
refusing on the landing's own range before a CI run is spent:
  devlog.py check   (exactly one new, stamped DEVLOG/ entry, numbered above main's; kb/Work PB2605, PB3025) → exit 6
  landing_oracle.py (the arch-oracle baseline describes the landed tree; kb/Work PB2885) → exit 7
  landing_check.py  (the R3 file-set partition and in-flight overlap; kb/Work PB2118) → exit 3 (STOP: re-plan, never
                    retry as is)
A check that cannot read its range is exit 2 (NOT RUN, never a pass).

Exit: 0 queued or nothing to land · 2 usage or state (a dirty worktree, no branch, HEAD behind main, a check could not
run, main still moving after CATCH_UP merges) · 3/6/7 a landing check refused (above) · 4 bringing main in conflicted ·
5 a gate is not GREEN · 8 the merge queue is not ACTIVE (nothing merged, gated, pushed or opened) · 9 GitHub refused
the push, the pull request or the queue (main untouched; re-run after the cause is fixed — every step is idempotent) ·
11 the branch is not reviewed at its tip (`land` only; above).
"""
from __future__ import annotations

import argparse
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import ruleset  # noqa: E402

EXIT_CONFLICT = 4
EXIT_GATE = 5
EXIT_QUEUE_NOT_ACTIVE = 8
EXIT_GITHUB = 9
EXIT_UNREVIEWED = 11
# How many times `land` brings a moved main in after its gate before it gives up (exit 2): main moves under a gate
# about once per landing the queue merges, and between `land`'s fetch and `queue`'s only by seconds.
CATCH_UP = 3
VERDICT = "=== ENQUEUE: "
# The lanes MANDATORY-PRACTICES I10 does not review: they write no fix-lane report, change no compiled program's source
# (the registrar's and the clerk's notes; the golden lane's spec-derived goldens, which the gate runs), and their briefs
# name themselves with `land --lane <lane>`. Every other branch lands only under a CLEAN review stamp (review_gate).
UNREVIEWED_LANES = ("registrar", "golden", "clerk")
# THE GATE AT ENQUEUE (DESIGN-test-build-ci.md §3.17.2 step 3, §3.17.3): (host, command run in the worktree, its verdict
# line's prefix). THE ONE GATE (kb/Work PB3022: one shape, no mode; a landing's gate runs at Normal priority) on
# Windows, then CI's Linux legs under WSL (the operator's steer 2026-10-11 12:37: the landing keeps the whole-leg WSL
# gate until PB3022 item 10 makes the one gate run leg 1 on both hosts; then the Linux row goes and build-local's own
# verdict covers both). On a Linux host only the Linux twin of the one gate runs.
GATES_WINDOWS = (("Windows", ["pwsh", "-NoProfile", "-File", "scripts/build-local.ps1"], "=== BUILD-LOCAL GATE: "),
                 ("Linux", ["bash", "scripts/linux-gate.sh", "--nice"], "=== LINUX GATE: "))
GATES_LINUX = (("Linux", ["bash", "scripts/build-local.sh"], "=== BUILD-LOCAL GATE: "),)
GREEN = re.compile(r"^(?:LEG 1 ONLY \([^)]*\): )?GREEN\b")
GATED_MARK = "enqueue-gated"     # in the worktree's git directory: the head this script last gated GREEN
# (the check's name, its command under scripts/, its refusal's exit code here, whether it takes --base, whether its
# every non-zero is a refusal). A refusal is the script's exit 1; any other non-zero is "could not run", except for
# landing_check.py, whose every non-zero is a STOP (its own contract: re-plan, never retry as is).
CHECKS = (("devlog.py check", ("devlog.py", "check"), 6, True, False),
          ("landing_oracle.py", ("orchestrator/landing_oracle.py",), 7, False, False),
          ("landing_check.py", ("orchestrator/landing_check.py",), 3, True, True))

Say = Callable[[str], None]
Gh = Callable[[list[str]], tuple[int, str, str]]


def run_checks(rev: str, base: str, run: Callable[[tuple[str, ...], list[str]], int], say: Say = print) -> int:
    """The landing checks in order; the first refusal's code, else 0. `run(command, args)` returns the exit of the
    script `command[0]` (a path under scripts/) run with `command[1:]` and then `args`."""
    for name, command, refused, takes_base, every_nonzero_refuses in CHECKS:
        rc = run(command, ["--rev", rev, *(["--base", base] if takes_base else [])])
        if rc == 0:
            continue
        if rc == 1 or every_nonzero_refuses:
            say(f"⛔ enqueue: {name} refused this landing (above) — nothing pushed, nothing opened.")
            return refused
        say(f"⛔ enqueue: {name} could not read the range (exit {rc}, above) — NOT RUN, never a pass.")
        return 2
    return 0


def _script_runner(cwd: pathlib.Path) -> Callable[[tuple[str, ...], list[str]], int]:
    def run(command: tuple[str, ...], args: list[str]) -> int:
        return subprocess.run([sys.executable, str(HERE.parent / command[0]), *command[1:], *args], cwd=cwd).returncode
    return run


def _git(cwd: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")


def queue_state() -> tuple[str, str]:
    state, detail, diffs = ruleset.check(ruleset.real_gh, ruleset.load_committed())
    return state, detail + "".join(f"; {d}" for d in diffs[:3])


def queue(cwd: pathlib.Path, title: str, body_file: str, *, gh: Gh, state: Callable[[], tuple[str, str]],
          checks: Callable[[str, str], int], dry_run: bool = False, say: Say = print) -> int:
    # 1. THE GUARD — before every other step, so a refusal has pushed and opened nothing.
    st, detail = state()
    if st != ruleset.ACTIVE:
        say(f"⛔ enqueue: the merge queue is not ACTIVE on main (ruleset: {st} — {detail}). Nothing pushed, nothing "
            "opened: a pull request would run only CI's cheap jobs and could merge with no whole-population run. "
            "Re-run once `python scripts/orchestrator/ruleset.py check` reports ACTIVE.")
        return EXIT_QUEUE_NOT_ACTIVE
    # 2. What lands.
    if not title.strip():
        say("⛔ enqueue: --title is empty — the pull request's title is the landing's one-line subject")
        return 2
    body = pathlib.Path(body_file)
    if not body.is_file() or not body.read_text(encoding="utf-8").strip():
        say(f"⛔ enqueue: --body-file {body_file} is missing or empty — it is the landing's forensic message")
        return 2
    branch = _git(cwd, "branch", "--show-current").stdout.strip()
    if not branch or branch == "main":
        say(f"⛔ enqueue: HEAD is {'detached' if not branch else 'on main'} — a landing is queued from its own branch")
        return 2
    dirty = [ln for ln in _git(cwd, "status", "--porcelain").stdout.splitlines()
             if not ln.endswith((".claude/settings.local.json", " STATUS.md"))]
    if dirty:
        say("⚠ enqueue: the working tree is DIRTY — these paths are NOT part of what is queued:")
        for ln in dirty:
            say(f"    {ln}")
    f = _git(cwd, "fetch", "--quiet", "origin", "+refs/heads/main:refs/remotes/origin/main")
    if f.returncode != 0:
        say(f"⛔ enqueue: git fetch origin main failed: {f.stderr.strip()}")
        return 2
    base = _git(cwd, "rev-parse", "refs/remotes/origin/main").stdout.strip()
    sha = _git(cwd, "rev-parse", "HEAD").stdout.strip()
    if sha == base:
        say(f"enqueue: origin/main is already at {sha[:12]} — nothing to land.")
        return 0
    if _git(cwd, "merge-base", "--is-ancestor", base, sha).returncode != 0:
        say(f"⛔ enqueue: HEAD ({sha[:12]}) is not a descendant of origin/main ({base[:12]}): bring main in first "
            "(`enqueue.py land <branch>` merges it), then re-run.")
        return 2
    # 3. The landing checks.
    rc = checks(sha, base)
    if rc:
        return rc
    # 4. Push, open (or reuse), queue.
    if dry_run:
        say(f"enqueue (dry run): would push {sha[:12]} to origin/{branch}, open or reuse its pull request into main "
            f"titled {title!r}, and queue it with `gh pr merge --auto`.")
        return 0
    p = _git(cwd, "push", "--quiet", "--force", "origin", f"{sha}:refs/heads/{branch}")
    if p.returncode != 0:
        say(f"⛔ enqueue: could not push {branch} (git exit {p.returncode}): {p.stderr.strip()} — main untouched.")
        return EXIT_GITHUB
    rc, out, err = gh(["pr", "list", "--head", branch, "--base", "main", "--state", "open",
                       "--json", "number,url", "--jq", ".[0] | select(.) | \"\\(.number) \\(.url)\""])
    if rc != 0:
        say(f"⛔ enqueue: gh could not list {branch}'s pull requests (exit {rc}): {(out + err).strip()}")
        return EXIT_GITHUB
    if out.strip():
        number, url = out.split()[0], out.split()[1]
        say(f"enqueue: reusing the open pull request #{number} for {branch} ({url}); its head is now {sha[:12]}.")
    else:
        rc, out, err = gh(["pr", "create", "--base", "main", "--head", branch, "--title", title,
                           "--body-file", str(body)])
        m = re.search(r"/pull/(\d+)\s*$", out.strip())
        if rc != 0 or not m:
            say(f"⛔ enqueue: gh pr create failed (exit {rc}): {(out + err).strip()} — main untouched.")
            return EXIT_GITHUB
        number, url = m.group(1), out.strip().splitlines()[-1]
        say(f"enqueue: opened pull request #{number}: {url}")
    rc, out, err = gh(["pr", "merge", number, "--auto"])
    if rc != 0 and "already" not in (out + err).lower():
        say(f"⛔ enqueue: gh pr merge --auto #{number} failed (exit {rc}): {(out + err).strip()} — the pull request is "
            "open but NOT queued; re-run once the cause is fixed.")
        return EXIT_GITHUB
    say(f"ENQUEUED pr={number} sha={sha} branch={branch} url={url}")
    return 0


# ── land: the whole step for one branch ─────────────────────────────────────────────────────────────────────────
Gate = Callable[[pathlib.Path], list[tuple[str, bool, str]]]


def worktree_of(repo: pathlib.Path, branch: str) -> pathlib.Path | None:
    """The worktree that has `branch` checked out, or None."""
    path = None
    for line in _git(repo, "worktree", "list", "--porcelain").stdout.splitlines():
        if line.startswith("worktree "):
            path = pathlib.Path(line[len("worktree "):])
        elif line == f"branch refs/heads/{branch}" and path is not None:
            return path
    return None


def dirty_paths(wt: pathlib.Path) -> list[str]:
    return [ln for ln in _git(wt, "status", "--porcelain").stdout.splitlines()
            if not ln.endswith((".claude/settings.local.json", " STATUS.md"))]


def run_gates(wt: pathlib.Path, gates=None) -> list[tuple[str, bool, str]]:
    """Each host's gate in order, fail-fast: [(host, green, its verdict line or why there is none)]. The output goes to
    `<worktree>/TestResults/enqueue/<host>-gate.log`; only the verdict line decides (never the exit code alone)."""
    gates = gates if gates is not None else (GATES_WINDOWS if os.name == "nt" else GATES_LINUX)
    logs = wt / "TestResults" / "enqueue"
    logs.mkdir(parents=True, exist_ok=True)
    out: list[tuple[str, bool, str]] = []
    for host, cmd, prefix in gates:
        log = logs / f"{host.lower()}-gate.log"
        with log.open("wb") as f:
            rc = subprocess.run(cmd, cwd=wt, stdout=f, stderr=subprocess.STDOUT).returncode
        lines = [ln.strip() for ln in log.read_bytes().decode("utf-8", errors="replace").splitlines()
                 if ln.strip().startswith(prefix)]
        if not lines:
            out.append((host, False, f"no `{prefix.strip()}` line (exit {rc}; {log})"))
            return out
        verdict = lines[-1]
        green = rc == 0 and bool(GREEN.match(verdict[len(prefix):]))
        out.append((host, green, verdict))
        if not green:
            return out
    return out


def record_blocked(branch: str, wt: pathlib.Path, line: str, say: Say,
                   reports_dir: pathlib.Path | None = None) -> None:
    """Append `line` to the branch's newest fix-lane report (prune_worktrees' reader), so the finisher and the next
    survey see why the landing stopped. A branch with no report is said so; nothing is invented."""
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports coord and plan_wave back)
    import coord  # noqa: PLC0415
    rdir = reports_dir or coord.reports_dir()
    rep = prune_worktrees.newest_report(prune_worktrees.report_index(rdir, lambda n: False), branch, str(wt))
    if rep is None:
        say(f"enqueue: no fix-lane report under {rdir} names {branch}; recorded nowhere but here: {line}")
        return
    with (rdir / rep.name).open("a", encoding="utf-8") as f:
        f.write(f"\n{line}\n")
    say(f"enqueue: recorded in {rep.name}: {line}")


def review_gate(branch: str, wt: pathlib.Path, base: str, reports_dir: pathlib.Path | None = None) -> str | None:
    """THE REVIEW BEFORE THE LANDING (MANDATORY-PRACTICES I10, L9; kb/Work PB3027; DESIGN-test-build-ci.md section
    3.17.2 step 2): None when the branch may land, else why not. Its newest fix-lane report (prune_worktrees' reader)
    must carry `**Review:** CLEAN <reviewer> <head>` naming a commit of the branch, and every commit of the branch after
    that head must be main's or mechanical (`prune_worktrees.unreviewed_since`, the one coverage rule the survey's
    WAITING TO LAND reads too): a finisher's fix, a hand-resolved conflict, any later commit is unreviewed work, and the
    new tip is reviewed before it lands."""
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports coord and plan_wave back)
    import coord  # noqa: PLC0415
    rdir = reports_dir or coord.reports_dir()
    rep = prune_worktrees.newest_report(prune_worktrees.report_index(rdir, lambda n: False), branch, str(wt))
    if rep is None:
        return (f"no fix-lane report under {rdir} names {branch}, so no review stamp does (a lane I10 does not review "
                f"names itself: --lane {'|'.join(UNREVIEWED_LANES)})")
    rv = rep.review
    if rv is None or rv.verdict != "CLEAN":
        return (f"{rep.name} carries {'a FINDINGS' if rv else 'no'} review stamp: a cobol-reviewer reviews the branch "
                "(and a finisher answers its findings) before it lands")
    extra = prune_worktrees.unreviewed_since(rv.sha, _git(wt, "rev-parse", "HEAD").stdout.strip(), repo=wt, base=base)
    if extra is None:
        return f"{rep.name}'s CLEAN stamp names {rv.sha}, which is not a commit of {branch}"
    if extra:
        return (f"{len(extra)} commit(s) after the CLEAN review at {rv.sha[:12]} (newest {extra[0][:12]}) are neither "
                "main's nor a clean merge of it nor DEVLOG-only: the new tip is reviewed before it lands")
    return None


def pull_request_text(wt: pathlib.Path, base: str, branch: str) -> tuple[str, str]:
    """(title, body) of the branch's pull request: the title of the ONE DEVLOG entry the landing adds — the landing
    rule's own definition, `devlog.landing_entries` (kb/Work PB3023 review R1-3: a file-name test read the migrating
    landing, which adds the whole history as files, as ~1,940 entries) — and as the body that entry and the branch's
    commit list. ("", "") when the landing adds no single entry or the range cannot be read."""
    import devlog  # noqa: PLC0415  (scripts/: the one definition of a landing's new entry)
    try:
        files, new = devlog.landing_entries(wt, base, "HEAD")
    except devlog.RangeError:
        return "", ""
    if len(new) != 1:
        return "", ""
    header, _, body = files[new[0]].partition("\n")
    m = re.match(r"^## Entry (\d+) \S+ \S+ \S+ \S+ \S+ (.+)$", header)
    title = f"DEVLOG {m.group(1)}: {m.group(2).strip()}" if m else header.lstrip("# ").strip()
    commits = _git(wt, "log", "--no-merges", "--format=- %h %s", f"{base}..HEAD").stdout.strip()
    return title, f"{body.strip()}\n\nBranch: `{branch}`\n\nCommits:\n{commits}\n"


def merge_main(wt: pathlib.Path, branch: str, base: str) -> list[str] | None:
    """Fetch origin's main and MERGE it into the branch when the branch does not already contain it (steps 2 and 4).
    None when the branch now contains main; else the conflicted files, with the merge aborted and the branch unchanged.
    A fetch that fails is ["(git fetch failed: …)"], never a pass."""
    f = _git(wt, "fetch", "--quiet", "origin", "+refs/heads/main:refs/remotes/origin/main")
    if f.returncode != 0:
        return [f"(git fetch origin main failed: {f.stderr.strip()})"]
    if _git(wt, "merge-base", "--is-ancestor", base, "HEAD").returncode == 0:
        return None
    if _git(wt, "merge", "--no-edit", "-m", f"Merge origin/main into {branch} (enqueue.py)", base).returncode == 0:
        return None
    files = _git(wt, "diff", "--name-only", "--diff-filter=U").stdout.split()
    _git(wt, "merge", "--abort")
    return files or ["?"]


def gated_covers(wt: pathlib.Path, mark: pathlib.Path, head: str, base: str) -> bool:
    """Whether the head the gated mark names still covers `head`: it IS that head, or every later commit is main's, a
    clean merge or DEVLOG-only (`prune_worktrees.unreviewed_since`, the one coverage rule the review gate reads too),
    so the tree a gate would test differs from the gated one only by main's own, already-landed changes and the log."""
    if not mark.is_file():
        return False
    gated = mark.read_text(encoding="utf-8").strip()
    if gated == head:
        return True
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports coord and plan_wave back)
    return prune_worktrees.unreviewed_since(gated, head, repo=wt, base=base) == []


def land(repo: pathlib.Path, branch: str, worktree: pathlib.Path | None, *, gh: Gh,
         state: Callable[[], tuple[str, str]], checks: Callable[[pathlib.Path, str, str], int],
         gates: Gate = run_gates, stamp: Callable[[pathlib.Path, str], int] | None = None,
         blocked: Callable[[str, pathlib.Path, str], None] | None = None,
         review: Callable[[str, pathlib.Path, str], str | None] = review_gate,
         lane: str | None = None, dry_run: bool = False, say: Say = print) -> int:
    """THE landing step (module doc); the collaborators are parameters so the self-test drives every exit arm."""
    def end(rc: int, outcome: str) -> int:
        say(f"{VERDICT}{outcome} ({branch}) ===")
        return rc

    def conflicted(files: list[str], when: str) -> int:
        blocked(branch, wt, f"BLOCKED: enqueue — bringing origin/main in {when} conflicted in {', '.join(files)} "
                            f"(kb/Work PB3023): a finisher merges origin/main, resolves, re-gates, re-runs enqueue.py")
        return end(EXIT_CONFLICT, f"CONFLICT in {', '.join(files)} — merge aborted, branch unchanged")
    blocked = blocked or (lambda b, w, line: record_blocked(b, w, line, say))
    wt = worktree or worktree_of(repo, branch)
    if wt is None or _git(wt, "branch", "--show-current").stdout.strip() != branch:
        return end(2, f"REFUSED — no worktree has {branch} checked out (pass --worktree)")
    if branch == "main":
        return end(2, "REFUSED — main is not a branch to land")
    if dirty := dirty_paths(wt):
        return end(2, f"REFUSED — the worktree {wt} has {len(dirty)} uncommitted path(s) ({dirty[0].strip()}): commit "
                      "or remove them; a landing carries only commits")
    f = _git(wt, "fetch", "--quiet", "origin", "+refs/heads/main:refs/remotes/origin/main")
    if f.returncode != 0:
        return end(2, f"REFUSED — git fetch origin main failed: {f.stderr.strip()}")
    base = "refs/remotes/origin/main"
    if _git(wt, "rev-parse", "HEAD").stdout.strip() == _git(wt, "rev-parse", base).stdout.strip():
        return end(0, "NOTHING TO LAND — the branch is origin/main")
    # 1. THE QUEUE GUARD, before anything is merged or gated: the queue is the only way onto main (module doc).
    st, detail = state()
    if st != ruleset.ACTIVE:
        return end(EXIT_QUEUE_NOT_ACTIVE, f"QUEUE NOT ACTIVE (ruleset: {st} — {detail}) — nothing merged or gated; "
                                          "re-run once `python scripts/orchestrator/ruleset.py check` reports ACTIVE")
    # 1b. The branch review (MANDATORY-PRACTICES I10): only a tip its CLEAN stamp covers lands.
    if lane:
        say(f"enqueue: the {lane} lane, which MANDATORY-PRACTICES I10 does not review; no review stamp read")
    elif why := review(branch, wt, base):
        blocked(branch, wt, f"BLOCKED: enqueue — not reviewed at its tip (kb/Work PB3027, MANDATORY-PRACTICES I10): {why}")
        return end(EXIT_UNREVIEWED, f"UNREVIEWED — {why}")
    # 2. Main in, by merge.
    if dry_run:
        behind = _git(wt, "merge-base", "--is-ancestor", base, "HEAD").returncode != 0
        return end(0, f"DRY RUN — would {'merge origin/main into ' + branch + ', ' if behind else ''}gate, stamp and "
                      "queue it")
    if files := merge_main(wt, branch, base):
        return conflicted(files, "before the gate")
    # 3. The gate, once per tested tree.
    head = _git(wt, "rev-parse", "HEAD").stdout.strip()
    mark = pathlib.Path(_git(wt, "rev-parse", "--absolute-git-dir").stdout.strip()) / GATED_MARK
    if gated_covers(wt, mark, head, base):
        say(f"enqueue: {head[:12]} is covered by the head an earlier enqueue run gated GREEN ({mark}); not gated again")
    else:
        results = gates(wt)
        for host, green, line in results:
            say(f"enqueue: {host} gate: {line}")
        bad = next(((h, line) for h, g, line in results if not g), None) if results else ("(none)", "no gate ran")
        if bad:
            blocked(branch, wt, f"BLOCKED: enqueue — the {bad[0]} gate is not GREEN on {head[:12]}: {bad[1]}")
            return end(EXIT_GATE, f"GATE RED ({bad[0]}) on {head[:12]}")
        mark.write_text(head + "\n", encoding="utf-8")
    # 4. Main again (it moves while a branch gates: R1-2), the DEVLOG stamp, the queue.
    for attempt in range(1, CATCH_UP + 1):
        if files := merge_main(wt, branch, base):
            return conflicted(files, "after the gate")
        if (stamp or _stamp)(wt, branch) != 0:
            return end(6, "DEVLOG STAMP FAILED (above): the branch needs one `DEVLOG/pending-<slug>.md`")
        title, body = pull_request_text(wt, base, branch)
        if not title:
            return end(6, "NO ENTRY — the branch adds no single stamped DEVLOG entry to title its pull request")
        body_file = pathlib.Path(_git(wt, "rev-parse", "--absolute-git-dir").stdout.strip()) / "enqueue-pr-body.md"
        body_file.write_text(body, encoding="utf-8")
        rc = queue(wt, title, str(body_file), gh=gh, state=state,
                   checks=lambda sha, b: checks(wt, sha, b), say=say)
        if rc != 2 or _git(wt, "merge-base", "--is-ancestor", base, "HEAD").returncode == 0:
            return end(rc, "ENQUEUED" if rc == 0 else f"NOT QUEUED (exit {rc}, above)")
        say(f"enqueue: main moved again before the queue took the branch; bringing it in (attempt {attempt + 1} of "
            f"{CATCH_UP})" if attempt < CATCH_UP else "enqueue: main kept moving")
    return end(2, f"NOT QUEUED — main moved under every one of {CATCH_UP} catch-up merges; re-run")


def _stamp(wt: pathlib.Path, branch: str) -> int:
    """`devlog.py stamp` when the branch still has a pending entry or one main has overtaken; 0 when it is stamped."""
    devlog = [sys.executable, str(HERE.parent / "devlog.py")]
    base = ["--base", "refs/remotes/origin/main"]
    if not any((wt / "DEVLOG").glob("pending-*.md")) and \
            subprocess.run([*devlog, "check", "--rev", "HEAD", *base], cwd=wt, capture_output=True).returncode == 0:
        return 0
    return subprocess.run([*devlog, "stamp", branch, "--repo", str(wt), *base], cwd=wt).returncode


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    root = pathlib.Path(tempfile.mkdtemp(prefix="enqueue-selftest-"))
    try:
        def sh(cwd: pathlib.Path, *args: str) -> str:
            r = _git(cwd, *args)
            if r.returncode != 0:
                raise RuntimeError(f"git {' '.join(args)}: {r.stderr}")
            return r.stdout.strip()

        origin, wt = root / "origin.git", root / "wt"
        sh(root, "init", "--quiet", "--bare", "-b", "main", str(origin))
        sh(root, "init", "--quiet", "-b", "main", str(wt))
        (wt / "a.txt").write_text("one", encoding="utf-8")
        sh(wt, "add", "a.txt")
        sh(wt, "commit", "--quiet", "-m", "base")
        sh(wt, "remote", "add", "origin", str(origin))
        sh(wt, "push", "--quiet", "origin", "main")
        sh(wt, "switch", "--quiet", "-c", "land-train")
        (wt / "a.txt").write_text("two", encoding="utf-8")
        sh(wt, "commit", "--quiet", "-am", "the landing")
        body = root / "body.txt"
        body.write_text("forensic message", encoding="utf-8")

        def fake_gh(open_prs: list[str], log: list[list[str]], fail: str = "") -> Gh:
            def gh(args: list[str]) -> tuple[int, str, str]:
                log.append(list(args))
                if fail and args[:2] == ["pr", fail]:
                    return 1, "", f"GraphQL: refused ({fail})"
                if args[:2] == ["pr", "list"]:
                    return 0, (open_prs[0] + "\n") if open_prs else "", ""
                if args[:2] == ["pr", "create"]:
                    open_prs.append("17 https://github.com/o/r/pull/17")
                    return 0, "https://github.com/o/r/pull/17\n", ""
                if args[:2] == ["pr", "merge"]:
                    return 0, "", ""
                return 1, "", "unrouted"
            return gh

        active = lambda: (ruleset.ACTIVE, "live")   # noqa: E731
        passing = lambda sha, base: 0             # noqa: E731
        lines: list[str] = []

        refused_all, touched = True, []
        for st in (ruleset.ABSENT, ruleset.DRIFT, ruleset.SKIPPED):
            log: list[list[str]] = []
            ran_checks: list[str] = []
            rc = queue(wt, "T", str(body), gh=fake_gh([], log), state=lambda st=st: (st, "x"),
                       checks=lambda s, b: ran_checks.append(s) or 0, say=lines.append)
            refused_all &= rc == EXIT_QUEUE_NOT_ACTIVE and not log and not ran_checks
            touched += log
        remote_branches = sh(wt, "ls-remote", "--heads", "origin")
        expect("the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) refuses with exit 8 before anything is checked, pushed "
               "or opened", refused_all and "land-train" not in remote_branches, f"{touched} {remote_branches}")

        codes = []
        for code in (6, 7, 3):
            log = []
            codes.append((queue(wt, "T", str(body), gh=fake_gh([], log), state=active,
                                checks=lambda s, b, code=code: code, say=lines.append), log))
        expect("a landing check's refusal stops the queue with its own exit code and opens nothing",
               [c for c, _ in codes] == [6, 7, 3] and all(not log for _, log in codes)
               and "land-train" not in sh(wt, "ls-remote", "--heads", "origin"), str(codes))

        prs: list[str] = []
        log = []
        rc = queue(wt, "Train 9: subject", str(body), gh=fake_gh(prs, log), state=active, checks=passing,
                   say=lines.append)
        head = sh(wt, "rev-parse", "HEAD")
        pushed = sh(wt, "ls-remote", "origin", "refs/heads/land-train").split()[:1]
        verbs = [a[:2] for a in log]
        expect("an ACTIVE queue pushes the branch, opens one pull request and queues it with --auto",
               rc == 0 and pushed == [head] and verbs == [["pr", "list"], ["pr", "create"], ["pr", "merge"]]
               and log[2] == ["pr", "merge", "17", "--auto"] and lines[-1].startswith("ENQUEUED pr=17 sha=" + head),
               f"rc={rc} pushed={pushed} log={log} last={lines[-1:]}")

        (wt / "a.txt").write_text("three", encoding="utf-8")
        sh(wt, "commit", "--quiet", "--amend", "-am", "the landing, rebased")   # the branch moves: a forced push
        log = []
        rc = queue(wt, "Train 9: subject", str(body), gh=fake_gh(prs, log), state=active, checks=passing,
                   say=lines.append)
        head = sh(wt, "rev-parse", "HEAD")
        expect("a re-run reuses the branch's open pull request, moves its head and queues it again",
               rc == 0 and [a[:2] for a in log] == [["pr", "list"], ["pr", "merge"]]
               and sh(wt, "ls-remote", "origin", "refs/heads/land-train").split()[:1] == [head], f"rc={rc} {log}")

        fails9 = []
        for verb in ("list", "create", "merge"):
            log = []
            fails9.append(queue(wt, "T", str(body), gh=fake_gh([], log, fail=verb), state=active, checks=passing,
                                say=lines.append))
        expect("GitHub's refusal of the pull request list, the pull request or the queue is exit 9, never 0",
               fails9 == [EXIT_GITHUB] * 3, str(fails9))

        sh(wt, "switch", "--quiet", "main")
        (wt / "b.txt").write_text("main moved", encoding="utf-8")
        sh(wt, "add", "b.txt")
        sh(wt, "commit", "--quiet", "-m", "main moves")
        sh(wt, "push", "--quiet", "origin", "main")
        rc_at_main = queue(wt, "T", str(body), gh=fake_gh([], []), state=active, checks=passing, say=lines.append)
        sh(wt, "switch", "--quiet", "land-train")
        log = []
        rc_beside = queue(wt, "T", str(body), gh=fake_gh([], log), state=active, checks=passing, say=lines.append)
        expect("HEAD beside origin/main is refused (main brought in first), and HEAD on main is refused before any "
               "check",
               rc_beside == 2 and not log and rc_at_main == 2, f"beside={rc_beside} at_main={rc_at_main}")

        sh(wt, "switch", "--quiet", "-c", "at-main", "origin/main")
        rc_nothing = queue(wt, "T", str(body), gh=fake_gh([], []), state=active, checks=passing, say=lines.append)
        rc_title = queue(wt, " ", str(body), gh=fake_gh([], []), state=active, checks=passing, say=lines.append)
        expect("HEAD at origin/main has nothing to land; an empty title or body is a usage error",
               rc_nothing == 0 and any("nothing to land" in ln for ln in lines) and rc_title == 2,
               f"{rc_nothing} {rc_title}")

        devlog_check, oracle, partition = ("devlog.py", "check"), ("orchestrator/landing_oracle.py",), \
            ("orchestrator/landing_check.py",)

        def runner(results: dict[tuple[str, ...], int], seen: list[tuple[tuple[str, ...], list[str]]]):
            def run(command: tuple[str, ...], args: list[str]) -> int:
                seen.append((command, args))
                return results.get(command, 0)
            return run
        mapped = []
        for results in ({devlog_check: 1}, {oracle: 1}, {partition: 1}, {devlog_check: 2}, {partition: 2}, {}):
            seen: list[tuple[tuple[str, ...], list[str]]] = []
            mapped.append(run_checks("S", "B", runner(results, seen), say=lines.append))
        seen = []
        run_checks("S", "B", runner({}, seen), say=lines.append)
        missing = [c[0] for _, c, *_ in CHECKS if not (HERE.parent / c[0]).is_file()]
        expect("checks runs the three landing scripts on the range in order and maps each refusal to its own code: "
               "DEVLOG 6, oracle 7, landing check 3, unreadable 2",
               mapped == [6, 7, 3, 2, 3, 0] and not missing
               and seen == [(devlog_check, ["--rev", "S", "--base", "B"]), (oracle, ["--rev", "S"]),
                            (partition, ["--rev", "S", "--base", "B"])], f"{mapped} {seen} missing={missing}")

        # The migrating landing (kb/Work PB3023 review R1-3): the base holds the monolithic DEVLOG.md, the branch adds
        # the whole history as files plus its own one entry; the pull request is titled from that one (devlog.check's).
        mig = root / "mig"
        sh(root, "init", "--quiet", "-b", "main", str(mig))
        hist = {10: "## Entry 10 — 2026-03-01 — oldest\n\nold body\n\n", 11: "## Entry 11 — 2026-03-02 — older\n\nb\n\n"}
        (mig / "DEVLOG.md").write_text("# Log\n\n" + hist[11] + hist[10], encoding="utf-8")
        sh(mig, "add", "DEVLOG.md")
        sh(mig, "commit", "--quiet", "-m", "the monolith")
        sh(mig, "switch", "--quiet", "-c", "migrating")
        sh(mig, "rm", "--quiet", "DEVLOG.md")
        (mig / "DEVLOG").mkdir()
        (mig / "DEVLOG" / "0010-oldest.md").write_text(hist[10], encoding="utf-8")
        (mig / "DEVLOG" / "0011-older.md").write_text(hist[11], encoding="utf-8")
        (mig / "DEVLOG" / "0012-pb7-the-migration.md").write_text(
            "## Entry 12 — 2026-10-12 09:00 PDT — PB7: the migration\n\nWhy it moved.\n", encoding="utf-8")
        sh(mig, "add", "DEVLOG")
        sh(mig, "commit", "--quiet", "-m", "split the log")
        mig_title, mig_body = pull_request_text(mig, "main", "migrating")
        expect("land: the migrating landing (the whole history added as files) titles its pull request from its ONE new "
               "entry, the landing rule's own (devlog.landing_entries)",
               mig_title == "DEVLOG 12: PB7: the migration" and "Why it moved." in mig_body, f"{mig_title!r}")

        # ── land (kb/Work PB3023): a branch `fix` in its own worktree, cut before main moved twice ──────────────────
        sh(wt, "switch", "--quiet", "main")
        sh(wt, "reset", "--quiet", "--hard", "origin/main")
        lw = root / "fix-wt"
        sh(wt, "worktree", "add", "--quiet", "-b", "fix", str(lw), "main")
        (lw / "c.txt").write_text("the fix", encoding="utf-8")
        sh(lw, "add", "c.txt")
        sh(lw, "commit", "--quiet", "-m", "the fix")
        (wt / "d.txt").write_text("main moves again", encoding="utf-8")
        sh(wt, "add", "d.txt")
        sh(wt, "commit", "--quiet", "-m", "main moves again")
        sh(wt, "push", "--quiet", "origin", "main")
        fix_tip = sh(lw, "rev-parse", "HEAD")

        def stamper(calls: list[str]):
            def stamp(w: pathlib.Path, b: str) -> int:
                calls.append(b)
                if not (w / "DEVLOG" / "0042-pb1-the-fix.md").is_file():
                    (w / "DEVLOG").mkdir(exist_ok=True)
                    for pending in (w / "DEVLOG").glob("pending-*.md"):
                        pending.unlink()
                    (w / "DEVLOG" / "0042-pb1-the-fix.md").write_text(
                        "## Entry 42 — 2026-10-11 14:00 PDT — PB1: the fix\n\nWhy it was wrong.\n", encoding="utf-8")
                    sh(w, "add", "DEVLOG")
                    sh(w, "commit", "--quiet", "-m", "DEVLOG 42: stamp the entry")
                return 0
            return stamp

        def gates_of(results: list[tuple[str, bool, str]], ran: list[int]) -> Gate:
            return lambda w: ran.append(1) or list(results)
        green = [("Windows", True, "=== BUILD-LOCAL GATE: GREEN ==="), ("Linux", True, "=== LINUX GATE: GREEN ===")]
        absent = lambda: (ruleset.ABSENT, "no ruleset")   # noqa: E731
        notes: list[str] = []
        record = lambda b, w, line: notes.append(line)    # noqa: E731
        reviewed = lambda b, w, base: None                # noqa: E731  (the review arms below drive review_gate)

        def run_land(state=active, gates: Gate | None = None, checks=lambda w, s, b: 0, gh: Gh | None = None,
                     stamped: list[str] | None = None, branch: str = "fix", wtree: pathlib.Path = lw, **kw) -> int:
            return land(root, branch, wtree, review=kw.pop("review", reviewed), gh=gh or fake_gh([], []), state=state,
                        checks=checks, gates=gates or gates_of(green, []), stamp=stamper([] if stamped is None else
                                                                                        stamped),
                        blocked=record, say=lines.append, **kw)

        def main_moves(name: str, text: str) -> None:
            """Another landing reaches main (the queue merged it): a commit on origin's main."""
            (wt / name).write_text(text, encoding="utf-8")
            sh(wt, "add", name)
            sh(wt, "commit", "--quiet", "-m", f"main moves: {name}")
            sh(wt, "push", "--quiet", "origin", "main")

        def own_commit(name: str) -> str:
            (lw / name).write_text(f"the branch's {name}", encoding="utf-8")
            sh(lw, "add", name)
            sh(lw, "commit", "--quiet", "-m", f"the branch's {name}")
            return sh(lw, "rev-parse", "HEAD")

        (lw / "stray.txt").write_text("uncommitted", encoding="utf-8")
        ran: list[int] = []
        rc_dirty = run_land(gates=gates_of(green, ran))
        (lw / "stray.txt").unlink()
        expect("land: a dirty worktree is refused (exit 2) before anything is fetched, merged or gated",
               rc_dirty == 2 and not ran and sh(lw, "rev-parse", "HEAD") == fix_tip
               and lines[-1].startswith(VERDICT + "REFUSED"), f"{rc_dirty} {lines[-1:]}")

        (lw / "d.txt").write_text("the branch's own d", encoding="utf-8")       # main added d.txt too: a conflict
        sh(lw, "add", "d.txt")
        sh(lw, "commit", "--quiet", "-m", "a clash")
        clash_tip = sh(lw, "rev-parse", "HEAD")
        ran = []
        rc_conflict = run_land(gates=gates_of(green, ran))
        expect("land: a conflict bringing main in is exit 4: the merge is aborted, the branch unchanged, the report "
               "told which files, and nothing gated",
               rc_conflict == EXIT_CONFLICT and not ran and sh(lw, "rev-parse", "HEAD") == clash_tip
               and not dirty_paths(lw) and notes and "d.txt" in notes[-1] and notes[-1].startswith("BLOCKED:"),
               f"{rc_conflict} {notes[-1:]}")
        sh(lw, "reset", "--quiet", "--hard", fix_tip)

        ran, stamped = [], []
        rc_inactive = [run_land(state=st, gates=gates_of(green, ran), stamped=stamped)
                       for st in (absent, lambda: (ruleset.DRIFT, "x"), lambda: (ruleset.SKIPPED, "offline"))]
        expect("land: the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) is exit 8 before anything is merged, gated, "
               "stamped or pushed: main takes only queued pull requests (kb/Work PB3023 review R1-1)",
               rc_inactive == [EXIT_QUEUE_NOT_ACTIVE] * 3 and not ran and not stamped
               and sh(lw, "rev-parse", "HEAD") == fix_tip and "fix" not in sh(wt, "ls-remote", "--heads", "origin")
               and lines[-1].startswith(VERDICT + "QUEUE NOT ACTIVE"), f"{rc_inactive} {lines[-1:]}")

        ran, stamped = [], []
        rc_red = run_land(gates=gates_of([("Windows", True, "G"), ("Linux", False, "=== LINUX GATE: RED (x) ===")], ran),
                          stamped=stamped)
        expect("land: main is MERGED in (the branch's commits kept), and a host's red gate is exit 5 with the host "
               "named in the report, before any stamp",
               rc_red == EXIT_GATE and ran and not stamped and "Linux" in notes[-1]
               and _git(lw, "merge-base", "--is-ancestor", "origin/main", "HEAD").returncode == 0
               and _git(lw, "merge-base", "--is-ancestor", fix_tip, "HEAD").returncode == 0, f"{rc_red} {notes[-1:]}")

        prs, log, ran = [], [], []
        rc_q = run_land(gh=fake_gh(prs, log), gates=gates_of(green, ran))
        create = next((a for a in log if a[:2] == ["pr", "create"]), [])
        expect("land: an ACTIVE queue opens one pull request titled from the DEVLOG entry, whose body carries the entry "
               "and the commits, and queues it",
               rc_q == 0 and ran and create and create[create.index("--title") + 1] == "DEVLOG 42: PB1: the fix"
               and "Why it was wrong." in pathlib.Path(create[create.index("--body-file") + 1]).read_text(encoding="utf-8")
               and lines[-1].startswith(VERDICT + "ENQUEUED"), f"{rc_q} {create} {lines[-3:]}")

        ran = []
        rc_again = run_land(gh=fake_gh(prs, []), gates=gates_of(green, ran), checks=lambda w, s, b: 6)
        ran_after_new_commit: list[int] = []
        own_commit("e.txt")
        run_land(gh=fake_gh(prs, []), gates=gates_of(green, ran_after_new_commit))
        expect("land: a re-run on the commit already gated GREEN does not gate again, a new commit does; a landing "
               "check's refusal keeps its own code",
               not ran and ran_after_new_commit and rc_again == 6, f"{ran} {ran_after_new_commit} {rc_again}")

        # kb/Work PB3023 review R1-2, the reviewer's repro: the queue merges another pull request while this branch gates
        gated_tip, ran, log = own_commit("f.txt"), [], []
        rc_moved = run_land(gh=fake_gh(prs, log), gates=lambda w: ran.append(1) or main_moves("g.txt", "g") or green)
        head = sh(lw, "rev-parse", "HEAD")
        ran_rerun: list[int] = []
        run_land(gh=fake_gh(prs, []), gates=gates_of(green, ran_rerun))
        expect("land: main moving during the gate is merged in again WITHOUT a second gate and the branch is queued on "
               "the new main; a re-run on that head (a clean merge after the gated one) is not gated again",
               rc_moved == 0 and ran == [1] and not ran_rerun and lines[-1].startswith(VERDICT + "ENQUEUED")
               and _git(lw, "merge-base", "--is-ancestor", "refs/remotes/origin/main", head).returncode == 0
               and _git(lw, "merge-base", "--is-ancestor", gated_tip, head).returncode == 0
               and sh(wt, "ls-remote", "origin", "refs/heads/fix").split()[:1] == [head]
               and [a[:2] for a in log] == [["pr", "list"], ["pr", "merge"]], f"{rc_moved} {ran} {ran_rerun} {log}")

        clash_after, ran, log, notes[:] = own_commit("h.txt"), [], [], []
        rc_late = run_land(gh=fake_gh(prs, log), gates=lambda w: ran.append(1) or main_moves("h.txt", "main's h")
                           or green)
        expect("land: a conflict bringing the moved main in after the gate is exit 4: the merge is aborted, the branch "
               "unchanged, the report told, and nothing pushed or queued",
               rc_late == EXIT_CONFLICT and ran == [1] and not log and sh(lw, "rev-parse", "HEAD") == clash_after
               and not dirty_paths(lw) and notes and "after the gate" in notes[-1] and "h.txt" in notes[-1],
               f"{rc_late} {ran} {log} {notes[-1:]}")
        sh(lw, "reset", "--quiet", "--hard", head)


        # ── the review before the landing (MANDATORY-PRACTICES I10; kb/Work PB3023 review R1-4) ───────────────────
        sh(wt, "fetch", "--quiet", "origin")
        rw = root / "rev-wt"
        sh(wt, "worktree", "add", "--quiet", "-b", "rev", str(rw), "origin/main")
        (rw / "r.txt").write_text("the reviewed work", encoding="utf-8")
        sh(rw, "add", "r.txt")
        sh(rw, "commit", "--quiet", "-m", "the reviewed work")
        rdir = root / "reports"
        rdir.mkdir()
        report = rdir / "w9a-PB1-report.md"

        def stamp_report(verdict: str) -> None:
            tail = " 1 w9a-review-r1 {} f.jsonl" if verdict == "FINDINGS" else " w9a-review-r1 {}"
            report.write_text(f"Status: DONE\n**Review:** {verdict}{tail.format(sh(rw, 'rev-parse', 'HEAD'))}\n"
                              f"Branch: rev\n\n## Rows\n", encoding="utf-8")
        stamp_report("CLEAN")
        base_ref = "refs/remotes/origin/main"
        at_review = review_gate("rev", rw, base_ref, rdir)
        (wt / "m.txt").write_text("main moves after the review", encoding="utf-8")
        sh(wt, "switch", "--quiet", "main")
        sh(wt, "add", "m.txt")
        sh(wt, "commit", "--quiet", "-m", "main moves after the review")
        sh(wt, "push", "--quiet", "origin", "main")
        sh(rw, "fetch", "--quiet", "origin")
        sh(rw, "merge", "--quiet", "--no-edit", base_ref)
        (rw / "DEVLOG").mkdir()
        (rw / "DEVLOG" / "pending-rev.md").write_text("## PB1: rev\n\nWhy.\n", encoding="utf-8")
        sh(rw, "add", "DEVLOG")
        sh(rw, "commit", "--quiet", "-m", "the entry")
        mechanical_only = review_gate("rev", rw, base_ref, rdir)
        expect("review: a tip that moved after its CLEAN review only by main's commits, a clean merge of main and a "
               "DEVLOG-only commit is still reviewed",
               at_review is None and mechanical_only is None, f"{at_review} / {mechanical_only}")

        (wt / "m2.txt").write_text("main moves again", encoding="utf-8")
        sh(wt, "add", "m2.txt")
        sh(wt, "commit", "--quiet", "-m", "main moves again")
        sh(wt, "push", "--quiet", "origin", "main")
        sh(rw, "fetch", "--quiet", "origin")
        sh(rw, "merge", "--quiet", "--no-commit", base_ref)
        (rw / "r.txt").write_text("a hand edit inside the merge", encoding="utf-8")
        sh(rw, "add", "r.txt")
        sh(rw, "commit", "--quiet", "--no-edit")
        hand_merge = review_gate("rev", rw, base_ref, rdir) or ""
        sh(rw, "reset", "--quiet", "--hard", "HEAD~1")
        (rw / "fix.txt").write_text("a finisher's fix", encoding="utf-8")
        sh(rw, "add", "fix.txt")
        sh(rw, "commit", "--quiet", "-m", "a finisher's fix")
        ran, notes[:] = [], []
        rc_unreviewed = run_land(branch="rev", wtree=rw, review=lambda b, w, base: review_gate(b, w, base, rdir),
                                 gates=gates_of(green, ran))
        expect("review: a commit after the CLEAN review that is not mechanical (a finisher's fix, a hand-resolved "
               "merge) is exit 11 before anything is merged or gated, the report told why",
               "after the CLEAN review" in hand_merge and rc_unreviewed == EXIT_UNREVIEWED and not ran
               and notes and notes[-1].startswith("BLOCKED:") and lines[-1].startswith(VERDICT + "UNREVIEWED"),
               f"{hand_merge!r} {rc_unreviewed} {notes[-1:]} {lines[-1:]}")

        stamp_report("FINDINGS")
        findings = review_gate("rev", rw, base_ref, rdir) or ""
        report.unlink()
        no_report = review_gate("rev", rw, base_ref, rdir) or ""
        ran = []
        rc_lane = run_land(branch="rev", wtree=rw, review=lambda b, w, base: review_gate(b, w, base, rdir),
                           lane="registrar", gates=gates_of(green, ran))
        expect("review: a FINDINGS stamp or no report is unreviewed; only a lane I10 does not review, named with "
               "--lane, lands without a stamp",
               "FINDINGS" in findings and "--lane" in no_report and rc_lane == 0 and ran,
               f"{findings!r} {no_report!r} {rc_lane}")
    finally:
        shutil.rmtree(root, ignore_errors=True)
    print(f"=== enqueue.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="verb")
    q = sub.add_parser("queue", help="the landing: guard, checks, push, open or reuse the pull request, queue it")
    q.add_argument("--title", required=True)
    q.add_argument("--body-file", required=True)
    q.add_argument("--dry-run", action="store_true")
    c = sub.add_parser("checks", help="the landing checks on base..rev")
    c.add_argument("--rev", default="HEAD")
    c.add_argument("--base", default="origin/main")
    lnd = sub.add_parser("land", help="THE landing step: main in, the gate, the DEVLOG stamp, the checks, the queue")
    lnd.add_argument("branch")
    lnd.add_argument("--worktree", type=pathlib.Path, help="the branch's worktree (default: found by git worktree list)")
    lnd.add_argument("--lane", choices=UNREVIEWED_LANES,
                     help="a lane MANDATORY-PRACTICES I10 does not review (its brief says so); every other branch "
                          "lands only under a CLEAN review stamp at its tip")
    lnd.add_argument("--dry-run", action="store_true", help="say what it would do; change nothing")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    top = _git(pathlib.Path.cwd(), "rev-parse", "--show-toplevel")
    if top.returncode != 0:
        print(f"⛔ enqueue: not in a git worktree: {top.stderr.strip()}")
        return 2
    cwd = pathlib.Path(top.stdout.strip())
    if a.verb == "checks":
        return run_checks(a.rev, a.base, _script_runner(cwd))
    if a.verb == "queue":
        return queue(cwd, a.title, a.body_file, gh=ruleset.real_gh, state=queue_state,
                     checks=lambda sha, base: run_checks(sha, base, _script_runner(cwd)), dry_run=a.dry_run)
    if a.verb == "land":
        return land(cwd, a.branch, a.worktree, gh=ruleset.real_gh, state=queue_state,
                    checks=lambda wt, sha, base: run_checks(sha, base, _script_runner(wt)), lane=a.lane,
                    dry_run=a.dry_run)
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
