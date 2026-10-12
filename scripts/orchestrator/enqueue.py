#!/usr/bin/env python3
"""enqueue.py — THE LANDING STEP: the landing checks, then one pull request per landing into GitHub's merge queue
(kb/Work PB3024 phase 1; DESIGN-test-build-ci.md §3.17.2 step 3, §3.17.6).

    python scripts/orchestrator/enqueue.py queue --title "<one-line subject>" --body-file <forensic message> [--dry-run]
    python scripts/orchestrator/enqueue.py checks [--rev HEAD] [--base origin/main]
    python scripts/orchestrator/enqueue.py --self-test

`queue`, run in the landing worktree on its committed, rebased HEAD:
  1. ⛔ THE QUEUE GUARD, FIRST: `ruleset.py check` must report the merge queue ACTIVE on main, or nothing is pushed or
     opened (exit 8). The operator's review (2026-10-11 13:39): a pull request runs only the cheap CI jobs, so its
     `ci-gate` passes on them; opened before the ruleset exists, `gh pr merge --auto` would merge it with NO
     whole-population run. While the queue is not ACTIVE a landing goes through `bash scripts/push-main.sh`, which
     earns the full matrix on `ci/<sha>` first. LandingPipelineDriftTests holds that no other file in the tree opens or
     queues a pull request, so this guard is the only door.
  2. HEAD must be on a branch (not main, not detached) and a descendant of a freshly fetched origin/main (rebase first).
  3. `checks` (below) on origin/main..HEAD.
  4. Push HEAD to the branch of the same name on origin (forced: the branch is this landing's own and moves on a
     rebase), reuse the branch's open pull request or open one (`gh pr create`), and queue it (`gh pr merge --auto`; the
     merge method is the queue's own, MERGE, so the branch's commits stay ancestors of main).
  5. Print `ENQUEUED pr=<n> sha=<sha> branch=<branch> url=<url>`, write the landing's measurement record (kb/Work
     PB3028, landing_measure.py: each carried change's DONE from git (landing_measure.done_times), enqueued now, the
     worktree's local gate seconds;
     a record that cannot be written is a loud warning, never a failed landing) and exit 0. It never waits for CI:
     the merge group runs the matrix, and a red there bounces only this PR (`landing_measure.py complete` reads the
     outcome from GitHub).

`checks` — THE LANDING CHECKS, one sequence in one place (push-main.sh runs it too while it lives), each refusing on
the landing's own range before a CI run is spent:
  landing_devlog.py (a new, stamped, top-placed DEVLOG entry; kb/Work PB2605) → exit 6
  landing_oracle.py (the arch-oracle baseline describes the landed tree; kb/Work PB2885) → exit 7
  landing_check.py  (the R3 file-set partition and in-flight overlap; kb/Work PB2118) → exit 3 (STOP: re-plan, never
                    retry as is)
A check that cannot read its range is exit 2 (NOT RUN, never a pass).

Exit: 0 queued (or nothing to land) · 2 usage or state (rebase first, no branch, a check could not run) · 3/6/7 a
landing check refused (above) · 8 the merge queue is not ACTIVE: land with push-main.sh · 9 GitHub refused the push,
the pull request or the queue (main untouched; re-run after the cause is fixed — every step is idempotent).
"""
from __future__ import annotations

import argparse
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
import landing_measure  # noqa: E402
import ruleset  # noqa: E402

EXIT_QUEUE_NOT_ACTIVE = 8
EXIT_GITHUB = 9
# (script, its refusal's exit code here). A refusal is the script's exit 1; any other non-zero is "could not run".
CHECKS = (("landing_devlog.py", 6, True), ("landing_oracle.py", 7, False), ("landing_check.py", 3, True))

Say = Callable[[str], None]
Gh = Callable[[list[str]], tuple[int, str, str]]
Measure = Callable[[str, int], None]   # (branch, pull-request number): write the landing's record (kb/Work PB3028)


def run_checks(rev: str, base: str, run: Callable[[str, list[str]], int], say: Say = print) -> int:
    """The landing checks in order; the first refusal's code, else 0. `run(script, args)` returns the script's exit."""
    for script, refused, takes_base in CHECKS:
        rc = run(script, ["--rev", rev, *(["--base", base] if takes_base else [])])
        if rc == 0:
            continue
        if rc == 1 or script == "landing_check.py":   # landing_check's every non-zero is a STOP (push-main's rule)
            say(f"⛔ enqueue: {script} refused this landing (above) — nothing pushed, nothing opened.")
            return refused
        say(f"⛔ enqueue: {script} could not read the range (exit {rc}, above) — NOT RUN, never a pass.")
        return 2
    return 0


def _script_runner(cwd: pathlib.Path) -> Callable[[str, list[str]], int]:
    def run(script: str, args: list[str]) -> int:
        return subprocess.run([sys.executable, str(HERE / script), *args], cwd=cwd).returncode
    return run


def _git(cwd: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")


def queue_state() -> tuple[str, str]:
    state, detail, diffs = ruleset.check(ruleset.real_gh, ruleset.load_committed())
    return state, detail + "".join(f"; {d}" for d in diffs[:3])


def measure_landing(cwd: pathlib.Path, branch: str, pr: int, changes: int | None, say: Say = print) -> None:
    """Write the landing's record into the coordination directory (landing_measure.record_enqueued)."""
    d = coord.coord_dir()
    rec = landing_measure.record_enqueued(d / landing_measure.FILE, cwd, branch, pr, d / "scratch" / "reports", changes)
    say(f"landing-measure: recorded PR #{pr} ({branch}): DONE {rec['done'] or 'UNMEASURED'}, enqueued {rec['enqueued']}, "
        f"local gate {rec['local_gate_s'] if rec['local_gate_s'] is not None else 'UNMEASURED'} s "
        f"in {rec['local_gate_runs'] or 0} run(s)")


def queue(cwd: pathlib.Path, title: str, body_file: str, *, gh: Gh, state: Callable[[], tuple[str, str]],
          checks: Callable[[str, str], int], measure: Measure | None = None, dry_run: bool = False,
          say: Say = print) -> int:
    # 1. THE GUARD — before every other step, so a refusal has pushed and opened nothing.
    st, detail = state()
    if st != ruleset.ACTIVE:
        say(f"⛔ enqueue: the merge queue is not ACTIVE on main (ruleset: {st} — {detail}). Nothing pushed, nothing "
            "opened: a pull request would run only CI's cheap jobs and could merge with no whole-population run. Land "
            "with `bash scripts/push-main.sh` until `python scripts/orchestrator/ruleset.py check` reports ACTIVE.")
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
        say(f"⛔ enqueue: HEAD ({sha[:12]}) is not a descendant of origin/main ({base[:12]}). Rebase first: "
            "git fetch origin && git rebase origin/main, re-gate, then re-run.")
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
    # 5. Measure it (kb/Work PB3028). The pull request IS queued: a record that cannot be written is a loud warning,
    # never a failed landing and never silence.
    if measure is not None:
        try:
            measure(branch, int(number))
        except (OSError, ValueError, SystemExit) as e:
            say(f"⚠ enqueue: PR #{number} is queued but its landing record was NOT written ({e}); record it with "
                f"`python scripts/orchestrator/landing_measure.py record --pr {number} --branch {branch}`")
    return 0


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

        seen_m: list[tuple[str, int]] = []

        def broken(branch: str, number: int) -> None:
            raise OSError("disk full")
        warn: list[str] = []
        rc_ok = queue(wt, "T", str(body), gh=fake_gh([], []), state=active, checks=passing,
                      measure=lambda b, n: seen_m.append((b, n)), say=lines.append)
        rc_broken = queue(wt, "T", str(body), gh=fake_gh([], []), state=active, checks=passing, measure=broken,
                          say=warn.append)
        rc_refused = queue(wt, "T", str(body), gh=fake_gh([], []), state=lambda: (ruleset.ABSENT, "x"),
                           checks=passing, measure=lambda b, n: seen_m.append(("refused", n)), say=lines.append)
        expect("a queued pull request is measured once; a record that cannot be written warns loudly and the landing "
               "still succeeds; a refused landing is never measured",
               rc_ok == 0 and seen_m == [("land-train", 17)] and rc_broken == 0
               and any("landing record was NOT written (disk full)" in w for w in warn)
               and rc_refused == EXIT_QUEUE_NOT_ACTIVE, f"{rc_ok} {seen_m} {rc_broken} {warn} {rc_refused}")

        sh(wt, "switch", "--quiet", "main")
        (wt / "b.txt").write_text("main moved", encoding="utf-8")
        sh(wt, "add", "b.txt")
        sh(wt, "commit", "--quiet", "-m", "main moves")
        sh(wt, "push", "--quiet", "origin", "main")
        rc_at_main = queue(wt, "T", str(body), gh=fake_gh([], []), state=active, checks=passing, say=lines.append)
        sh(wt, "switch", "--quiet", "land-train")
        log = []
        rc_beside = queue(wt, "T", str(body), gh=fake_gh([], log), state=active, checks=passing, say=lines.append)
        expect("HEAD beside origin/main is refused (rebase first), and HEAD on main is refused before any check",
               rc_beside == 2 and not log and rc_at_main == 2, f"beside={rc_beside} at_main={rc_at_main}")

        sh(wt, "switch", "--quiet", "-c", "at-main", "origin/main")
        rc_nothing = queue(wt, "T", str(body), gh=fake_gh([], []), state=active, checks=passing, say=lines.append)
        rc_title = queue(wt, " ", str(body), gh=fake_gh([], []), state=active, checks=passing, say=lines.append)
        expect("HEAD at origin/main has nothing to land; an empty title or body is a usage error",
               rc_nothing == 0 and any("nothing to land" in ln for ln in lines) and rc_title == 2,
               f"{rc_nothing} {rc_title}")

        def runner(results: dict[str, int], seen: list[tuple[str, list[str]]]):
            def run(script: str, args: list[str]) -> int:
                seen.append((script, args))
                return results.get(script, 0)
            return run
        mapped = []
        for results in ({"landing_devlog.py": 1}, {"landing_oracle.py": 1}, {"landing_check.py": 1},
                        {"landing_devlog.py": 2}, {"landing_check.py": 2}, {}):
            seen: list[tuple[str, list[str]]] = []
            mapped.append(run_checks("S", "B", runner(results, seen), say=lines.append))
        seen = []
        run_checks("S", "B", runner({}, seen), say=lines.append)
        expect("checks runs the three landing scripts on the range in order and maps each refusal to its own code: "
               "DEVLOG 6, oracle 7, landing check 3, unreadable 2",
               mapped == [6, 7, 3, 2, 3, 0]
               and seen == [("landing_devlog.py", ["--rev", "S", "--base", "B"]), ("landing_oracle.py", ["--rev", "S"]),
                            ("landing_check.py", ["--rev", "S", "--base", "B"])], f"{mapped} {seen}")
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
    q.add_argument("--changes", type=int, default=None,
                   help="changes this pull request lands, for the measurement (a lander's train is several)")
    c = sub.add_parser("checks", help="the landing checks on base..rev")
    c.add_argument("--rev", default="HEAD")
    c.add_argument("--base", default="origin/main")
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
                     checks=lambda sha, base: run_checks(sha, base, _script_runner(cwd)),
                     measure=lambda branch, pr: measure_landing(cwd, branch, pr, a.changes), dry_run=a.dry_run)
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
