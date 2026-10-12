#!/usr/bin/env python3
"""land_lane.py — THE LOOP'S LANDING LANE: every branch the survey lists WAITING TO LAND goes through `enqueue.py land`
the moment it is ready, while whatever unit runs keeps running (kb/Work PB3103; DESIGN-orchestrator-loop.md section
4.9; MANDATORY-PRACTICES O11). Deterministic, no model: it decides nothing a script cannot, and dispatches no agent.

    python scripts/orchestrator/land_lane.py tick [--repo DIR]      reap, survey, start every eligible landing
    python scripts/orchestrator/land_lane.py status [--json]        one line per branch: running, stopped, queued
    python scripts/orchestrator/land_lane.py collect <branch> ...   the enqueue unit has handled these attempts
    python scripts/orchestrator/land_lane.py clear-halt             the operator: the merge queue is ACTIVE again
    python scripts/orchestrator/land_lane.py run <attempt file>     (the detached runner `tick` starts; not by hand)
    python scripts/orchestrator/land_lane.py --self-test

⛔ WHY (the owner's choice 2026-10-12, "Supervisor lane (Recommended)"). Before the lane a reviewed branch landed only
when the loop's NEXT `enqueue` unit started, after the whole wave ended: wave 1069's group D was stamped Review CLEAN at
12:34 PDT and E and F by 13:05, and all three idled while G and H ran; then, while the enqueue unit gated and waited on
the merge queue, no implementer ran. The supervisor (`orchestrate.ps1`) now runs `tick` in the BACKGROUND every few
minutes while any unit runs, so a branch lands within its own gate and CI time of being ready.

STATE, all under `<coord>/land-lane/` (the coordination directory, never a worktree):
  attempts/<id>.json  one per `enqueue.py land` run: branch, head (its tip at launch), worktree, argv, log
                      (`<coord>/logs/land-lane-<branch>-<stamp>.log`), runner pid, and once it ends rc, the log's
                      `=== ENQUEUE: ` verdict, end_head (the tip after it: the run merges main in and stamps the entry),
                      and `collected` (the enqueue unit handled it). Only the runner writes a running attempt; the tick
                      writes one only to mark it DIED (its runner is gone with no rc).
  survey.json         the last tick's branch survey (prune_worktrees' rows, imported, never re-parsed from its text):
                      what next_unit.py reads to decide whether the enqueue unit has AGENT work.
  HALT                written when a run ends QUEUE NOT ACTIVE (exit 8): no attempt starts until the operator runs
                      `clear-halt`; the operator is named through the mailbox.
  tick.lock           an OS lock: one tick at a time (a second exits at once, saying so).

THE TICK, in order:
  1. REAP: an attempt whose runner is gone without an rc is DIED. A run that ended QUEUE NOT ACTIVE writes HALT.
  2. SURVEY: `git fetch`, then `prune_worktrees.survey` and its landing order (the most harmful lead first, kb/Work
     PB2981), written to survey.json.
  3. LAUNCH, unless HALT exists or a stop does (the loop's STOP-UNIT or STOP, or the owner's global scratch\\STOP; a
     running `enqueue.py land` finishes either way: it is idempotent). The candidates are the WAITING TO LAND rows in
     survey order, then the branches whose last attempt a script may retry (below) and whose tip is still that
     attempt's end head (a run that committed makes the survey read the branch IN FLIGHT, so a retry is the lane's own
     record, never the survey's). Each candidate is decided by `decide`:
       no attempt, or a NEW HEAD (a finisher's commit, reviewed)  launch
       a run in flight                                             wait
       ENQUEUED or NOTHING TO LAND (exit 0) at this head           nothing to do: the queue has it
       exit 3 — the landing check's STOP: the branch shares a file with earlier-dispatched in-flight work (kb/Work
                PB2701)                                            retried at this head once `landing_check.py` passes
                                                                   on the branch (the overlap is gone); never as is
       exit 2 (main moved under every catch-up, or the branch could not be read), exit 9 (GitHub refused the push, the
                PR or the queue), or a runner that DIED           retried ONCE at this head (enqueue.md's own rule)
       exit 8 — QUEUE NOT ACTIVE                                   HALT (above); relaunched once the halt is cleared
       anything else — CONFLICT 4, GATE RED 5, a DEVLOG refusal 6, a stale oracle baseline 7, UNREVIEWED 11, an exit
                2 or 9 retried once already                       STOPPED: never relaunched at this head; the
                                                                   enqueue unit dispatches its finisher or reviewer
     A launch writes the attempt and starts `land_lane.py run <attempt>` DETACHED (its own process group, out of the
     supervisor's job where Windows allows it), which runs `enqueue.py land <branch> --worktree <wt>` with its output in
     the log and records the outcome. (A permission refusal cannot happen here: a script has no permission layer.)
`status` prints each branch's latest attempt; the enqueue unit reads it (units/enqueue.md step 3), sends each STOPPED
attempt to a finisher or a reviewer, files the leads of each ENQUEUED one, and then `collect`s them. `agent_work` is
next_unit.py's one reader: the enqueue unit starts only for agent work (WAITING FOR REVIEW, BOUNCED, a stopped attempt,
an ENQUEUED one to collect), never merely because a handoff lists DONE branches the lane is landing.

Exit: 0 done (a tick already running included) · 1 the survey or a launch failed (printed; the supervisor never treats
it as fatal) · 2 usage.
"""
from __future__ import annotations

import argparse
import dataclasses
import datetime as dt
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import time
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402
from enqueue import (EXIT_GITHUB, EXIT_QUEUE_NOT_ACTIVE, VERDICT)  # noqa: E402  (the landing's own exit contract)
from gate_slot import ExclusiveLock  # noqa: E402  (an OS lock: the tick lock frees itself when its holder dies)

LANE_DIR = "land-lane"
ATTEMPTS = "attempts"
SURVEY = "survey.json"
HALT = "HALT"
TICK_LOCK = "tick.lock"
EXIT_CHECK_STOP = 3      # enqueue.py: landing_check.py's STOP (the branch shares a file with in-flight work)
EXIT_STATE = 2           # enqueue.py: usage or state, among them "main moved under every catch-up merge"
# How old survey.json may be before next_unit.py treats the lane as not ticking (the supervisor ticks every
# -LandLaneSeconds, 300 by default, while any unit runs, and once as each unit starts).
SURVEY_MAX_AGE_MIN = 30
# The classes of a finished attempt (classify). STOPPED and DONE-to-collect are the enqueue unit's; the rest the lane's.
DONE, FREE, ONCE, HALTED, STOPPED = "done", "retry-when-free", "retry-once", "halt", "stopped"
RETRYABLE = (FREE, ONCE, HALTED)
VERDICT_LINE = re.compile(r"^" + re.escape(VERDICT) + r"(?P<outcome>.*?)(?: \((?P<branch>[^()]*)\))? ===\s*$")

Say = Callable[[str], None]


class LaneError(RuntimeError):
    """The survey could not be taken (not a repository, git failed): the tick launches nothing and says why."""


# ── the state ──────────────────────────────────────────────────────────────────────────────────────────────────────
def lane_dir(cdir: pathlib.Path) -> pathlib.Path:
    return cdir / LANE_DIR


def _now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc)


def _slug(branch: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]+", "-", branch).strip("-") or "branch"


def attempts(cdir: pathlib.Path) -> list[dict[str, Any]]:
    """Every attempt, oldest first (ids sort by their UTC stamp). An unreadable file is skipped, said by `status`."""
    d = lane_dir(cdir) / ATTEMPTS
    out = []
    for p in sorted(d.glob("*.json")) if d.is_dir() else []:
        try:
            rec = coord.read_json(p, None)
        except ValueError:
            continue
        if isinstance(rec, dict):
            rec["_path"] = str(p)
            out.append(rec)
    return out


def by_branch(atts: list[dict[str, Any]]) -> dict[str, list[dict[str, Any]]]:
    out: dict[str, list[dict[str, Any]]] = {}
    for a in atts:
        out.setdefault(a["branch"], []).append(a)
    return out


def save(rec: dict[str, Any]) -> None:
    coord.write_json(pathlib.Path(rec["_path"]), {k: v for k, v in rec.items() if k != "_path"})


def finished(a: dict[str, Any]) -> bool:
    return "rc" in a or a.get("outcome") == "DIED"


def classify(a: dict[str, Any]) -> str:
    """What a finished attempt asks for next (module doc's table)."""
    if a.get("outcome") == "DIED":
        return ONCE
    rc = a.get("rc")
    if rc == 0:
        return DONE
    if rc == EXIT_CHECK_STOP:
        return FREE
    if rc in (EXIT_STATE, EXIT_GITHUB):
        return ONCE
    if rc == EXIT_QUEUE_NOT_ACTIVE:
        return HALTED
    return STOPPED


def trailing(atts: list[dict[str, Any]], cls: str) -> int:
    """How many of the branch's newest finished attempts, in a row, are of class `cls`."""
    n = 0
    for a in reversed(atts):
        if not finished(a) or classify(a) != cls:
            break
        n += 1
    return n


def effective(atts: list[dict[str, Any]]) -> str:
    """The class of a branch's LATEST attempt as the enqueue unit sees it: a retry-once outcome already retried is
    STOPPED (the second exit 2 or 9 at one head is named in the handoff, enqueue.md step 3)."""
    last = atts[-1]
    if not finished(last):
        return "running"
    cls = classify(last)
    return STOPPED if cls == ONCE and trailing(atts, ONCE) >= 2 else cls


def last_verdict(log: pathlib.Path) -> str | None:
    """The outcome of the log's last `=== ENQUEUE: <outcome> (<branch>) ===` line, or None."""
    try:
        text = log.read_bytes().decode("utf-8", errors="replace")
    except OSError:
        return None
    found = None
    for line in text.splitlines():
        if m := VERDICT_LINE.match(line.strip()):
            found = m.group("outcome").strip()
    return found


# ── the decisions ──────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass(frozen=True)
class Decision:
    launch: bool
    why: str


def decide(tip: str, atts: list[dict[str, Any]], overlap_free: Callable[[], tuple[bool, str]]) -> Decision:
    """Whether to start `enqueue.py land` for a branch at `tip`, given its attempts (oldest first). The global stops
    (HALT, STOP, STOP-UNIT) are the caller's; `overlap_free` is asked only for an exit 3 at this head."""
    if not atts:
        return Decision(True, "first attempt")
    last = atts[-1]
    if not finished(last):
        return Decision(False, f"being landed (attempt {last['id']}, pid {last.get('pid')})")
    if tip not in (last.get("head"), last.get("end_head")):
        return Decision(True, f"new head {tip[:12]} (the last attempt ended at {str(last.get('end_head'))[:12]})")
    cls = classify(last)
    rc = last.get("rc")
    if cls == DONE:
        return Decision(False, f"{last.get('verdict') or 'exit 0'} at this head: the merge queue has it")
    if cls == HALTED:
        return Decision(True, "the queue halt was cleared since its exit 8")
    if cls == FREE:
        free, detail = overlap_free()
        return Decision(free, ("the in-flight work it shared a file with is gone; retried at this head" if free else
                               f"exit 3 at this head and it still shares a file with in-flight work ({detail})"))
    if cls == ONCE and trailing(atts, ONCE) < 2:
        return Decision(True, f"{'its runner DIED' if rc is None else f'exit {rc}'} at this head: retried once")
    return Decision(False, f"STOPPED at this head ({'exit ' + str(rc) if rc is not None else 'DIED'}: "
                           f"{last.get('verdict') or 'no verdict line'}): the enqueue unit's; a new head re-arms it")


def tip_of(worktree: str | None) -> str | None:
    """The worktree's HEAD commit, or None when there is no worktree or git cannot read it."""
    if not worktree:
        return None
    tip = subprocess.run(["git", "-C", worktree, "rev-parse", "HEAD"], capture_output=True, text=True,
                         encoding="utf-8", errors="replace")
    return tip.stdout.strip() if tip.returncode == 0 else None


# ── the collaborators (the self-test replaces each) ────────────────────────────────────────────────────────────────
def pid_alive(pid: int | None) -> bool:
    """The runner is alive: a live process with that id whose image is a Python (a reused id is another program)."""
    if not isinstance(pid, int):
        return False
    name = coord.process_name(pid)
    return bool(name) and "python" in name.lower()


def survey_rows(repo: pathlib.Path, cdir: pathlib.Path) -> tuple[list[dict[str, Any]], list[str]]:
    """(every surveyed row as {name, state, tip, worktree, why, lead}, the WAITING TO LAND names in landing order) —
    prune_worktrees' own survey, imported: one lifecycle rule (MANDATORY-PRACTICES O10). The reports and the dispatch
    ledger are the TICK's coordination directory's, never the environment's default."""
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports coord and plan_wave back)
    probe = prune_worktrees.git("rev-parse", "--git-common-dir", cwd=repo)
    if probe.returncode != 0:
        raise LaneError(f"{repo} is not a git repository ({probe.stderr.strip()[:120]})")
    fetch = prune_worktrees.git("fetch", "-q", "origin", cwd=repo)
    if fetch.returncode != 0:
        raise LaneError(f"git fetch origin failed in {repo}: {fetch.stderr.strip()[:200]}")
    terminal = prune_worktrees.note_terminal()
    rows = prune_worktrees.survey(repo, reports=prune_worktrees.report_index(coord.reports_dir(str(cdir)), terminal),
                                  slugs=prune_worktrees.in_flight_slugs(cdir, terminal),
                                  prs=prune_worktrees.pull_requests(), coord_dir=cdir)
    order = [r.name for r in prune_worktrees.landing_order(rows, prune_worktrees.note_rank())]
    return ([{"name": r.name, "state": r.state, "tip": r.tip, "worktree": r.worktree, "why": r.why,
              "lead": r.report.lead if r.report else None} for r in rows], order)


def landing_check_free(worktree: str) -> tuple[bool, str]:
    """`landing_check.py` on the branch's own range (from its merge base with origin/main, which `enqueue.py land` would
    merge in), as the landing runs it: exit 0 means the overlap that stopped it is gone."""
    base = subprocess.run(["git", "-C", worktree, "merge-base", "refs/remotes/origin/main", "HEAD"],
                          capture_output=True, text=True, encoding="utf-8", errors="replace")
    if base.returncode != 0:
        return False, f"no merge base with origin/main in {worktree}"
    p = subprocess.run([sys.executable, str(HERE / "landing_check.py"), "--rev", "HEAD", "--base", base.stdout.strip()],
                       cwd=worktree, capture_output=True, text=True, encoding="utf-8", errors="replace")
    stops = [ln for ln in p.stdout.splitlines() if ln.startswith("⛔")]
    return p.returncode == 0, (stops[0] if stops else (p.stdout.strip().splitlines() or ["no output"])[-1])[:240]


def spawn(argv: list[str], log: pathlib.Path) -> int:
    """Start `argv` DETACHED, its output appended to `log`; its pid. On Windows it leaves the supervisor's console and,
    where the job allows it, the supervisor's job, so a supervisor that ends does not end a landing mid-gate; a job that
    refuses breakaway keeps it (the landing is idempotent: the next tick reaps it DIED and retries it once)."""
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open("ab") as out:
        kw: dict[str, Any] = {"stdin": subprocess.DEVNULL, "stdout": out, "stderr": subprocess.STDOUT, "close_fds": True}
        if os.name == "nt":
            base = subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
            try:
                return subprocess.Popen(argv, creationflags=base | subprocess.CREATE_BREAKAWAY_FROM_JOB, **kw).pid
            except OSError:
                return subprocess.Popen(argv, creationflags=base, **kw).pid
        return subprocess.Popen(argv, start_new_session=True, **kw).pid


def notify_operator(cdir: pathlib.Path, subject: str, body: str, ref: str, say: Say) -> None:
    """One open mailbox message per subject (the supervisor's own pattern for an owed publish); never fatal."""
    try:
        import mailbox  # noqa: PLC0415
        mailbox.send(cdir, to="operator", kind="info", subject=subject, body=body[:mailbox.BODY_MAX], refs=[ref],
                     sender="loop", session=f"land_lane.py pid {os.getpid()}", unless_pending=True)
    except Exception as exc:  # noqa: BLE001  (a mailbox that cannot be written must not stop the lane; it is said)
        say(f"land lane: the operator's mailbox was not written ({exc}); {subject}")


# ── the tick ───────────────────────────────────────────────────────────────────────────────────────────────────────
def stop_reason(cdir: pathlib.Path) -> str | None:
    """Why no attempt may start now: the loop's STOP-UNIT or STOP, the owner's global stop, or HALT."""
    for path, what in ((coord.stop_unit(str(cdir)), "STOP-UNIT"), (cdir / "STOP", "the loop's STOP"),
                       (coord.global_stop(str(cdir)), "the owner's global STOP"),
                       (lane_dir(cdir) / HALT, "HALT (a run ended QUEUE NOT ACTIVE; `land_lane.py clear-halt`)")):
        if path.exists():
            return f"{what} ({path})"
    return None


def reap(cdir: pathlib.Path, atts: list[dict[str, Any]], alive: Callable[[int | None], bool], say: Say) -> None:
    for a in atts:
        if finished(a):
            continue
        if alive(a.get("pid")):
            continue
        fresh = coord.read_json(pathlib.Path(a["_path"]), {})   # the runner writes its rc and then exits
        if "rc" in fresh:
            a.update(fresh)
            continue
        # Its tip now is its end head: a runner that died after `enqueue.py land` committed (main merged in, the stamp)
        # leaves the branch IN FLIGHT in the survey, and only this record lets the lane retry it.
        a.update(outcome="DIED", ended=_now().isoformat(timespec="seconds"),
                 verdict=last_verdict(pathlib.Path(a["log"])), end_head=tip_of(a.get("worktree")))
        save(a)
        say(f"land lane: {a['branch']}: the runner of attempt {a['id']} is gone with no exit code (DIED; {a['log']})")
    for a in atts:
        if finished(a) and a.get("rc") == EXIT_QUEUE_NOT_ACTIVE and not a.get("halted"):
            halt = lane_dir(cdir) / HALT
            if not halt.exists():
                halt.write_text(f"{_now().isoformat(timespec='seconds')} {a['branch']} attempt {a['id']}: "
                                f"{a.get('verdict')}\n", encoding="utf-8", newline="\n")
                say(f"land lane: HALTED — {a['branch']} ended QUEUE NOT ACTIVE; no landing starts until the operator "
                    f"runs `land_lane.py clear-halt` once `ruleset.py check` reports ACTIVE")
                notify_operator(cdir, "Landing lane halted: the merge queue is not ACTIVE",
                                f"enqueue.py land {a['branch']} ended QUEUE NOT ACTIVE (exit 8): "
                                f"{a.get('verdict')}. The landing lane starts no landing until the queue is ACTIVE "
                                "(`python scripts/orchestrator/ruleset.py check`) and the operator runs "
                                "`python scripts/orchestrator/land_lane.py clear-halt`.", a["log"], say)
            a["halted"] = True
            save(a)


def candidates(rows: list[dict[str, Any]], order: list[str],
               branches: dict[str, list[dict[str, Any]]]) -> list[dict[str, Any]]:
    """The WAITING TO LAND rows in landing order, then every branch whose last attempt the lane may retry and whose tip
    is still that attempt's end head, unless the queue holds it or it landed."""
    named = {r["name"]: r for r in rows}
    out = [named[n] for n in order if n in named and named[n]["state"] == "WAITING TO LAND"]
    seen = {r["name"] for r in out}
    for branch, atts in branches.items():
        r = named.get(branch)
        last = atts[-1]
        if branch in seen or r is None or not finished(last) or classify(last) not in RETRYABLE:
            continue
        if r["state"] in ("ENQUEUED", "BOUNCED", "LANDED") or r["tip"] != last.get("end_head") or not r["worktree"]:
            continue
        out.append(r)
    return out


def launch(cdir: pathlib.Path, repo: pathlib.Path, row: dict[str, Any], why: str,
           start: Callable[[list[str], pathlib.Path], int], argv: list[str] | None = None) -> dict[str, Any]:
    now = _now()
    stamp = now.strftime("%Y%m%dT%H%M%S%fZ")
    aid = f"{stamp}-{_slug(row['name'])}"
    path = lane_dir(cdir) / ATTEMPTS / f"{aid}.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    log = cdir / "logs" / f"land-lane-{_slug(row['name'])}-{stamp}.log"
    rec = {"id": aid, "branch": row["name"], "head": row["tip"], "worktree": row["worktree"], "why": why,
           "argv": argv or [sys.executable, str(HERE / "enqueue.py"), "land", row["name"], "--worktree", row["worktree"]],
           "cwd": str(repo), "log": str(log), "started": now.isoformat(timespec="seconds"), "pid": None,
           "collected": False, "_path": str(path)}
    save(rec)
    rec["pid"] = start([sys.executable, str(pathlib.Path(__file__).resolve()), "run", str(path)], log)
    # The runner may already have finished (a fast refusal) and written its outcome: keep it, add only the pid.
    current = coord.read_json(path, {})
    if "rc" in current:
        rec.update(current)
        rec["_path"] = str(path)
    save(rec)
    return rec


def tick(cdir: pathlib.Path, repo: pathlib.Path, *,
         survey: Callable[[pathlib.Path, pathlib.Path], tuple[list, list[str]]] = survey_rows,
         start: Callable[[list[str], pathlib.Path], int] = spawn,
         alive: Callable[[int | None], bool] = pid_alive,
         overlap_free: Callable[[str], tuple[bool, str]] = landing_check_free,
         argv_for: Callable[[dict[str, Any]], list[str] | None] = lambda row: None, say: Say = print) -> int:
    d = lane_dir(cdir)
    d.mkdir(parents=True, exist_ok=True)
    lock = ExclusiveLock.try_take(d / TICK_LOCK, f"land_lane.py tick (pid {os.getpid()}, {_now():%Y-%m-%d %H:%M:%S}Z)")
    if lock is None:
        say(f"land lane: a tick is already running ({ExclusiveLock.holder(d / TICK_LOCK)}); nothing done")
        return 0
    with lock:
        atts = attempts(cdir)
        reap(cdir, atts, alive, say)
        try:
            rows, order = survey(repo, cdir)
        except (LaneError, OSError, subprocess.SubprocessError) as exc:
            say(f"land lane: no survey, nothing launched: {exc}")
            return 1
        coord.write_json(d / SURVEY, {"at": _now().isoformat(timespec="seconds"), "rows": rows, "landing_order": order})
        if why := stop_reason(cdir):
            say(f"land lane: {why}: no landing starts (a running one finishes)")
            return 0
        branches = by_branch(atts)
        failed = 0
        for row in candidates(rows, order, branches):
            dec = decide(row["tip"], branches.get(row["name"], []),
                         lambda r=row: overlap_free(r["worktree"]) if r["worktree"] else (False, "no worktree"))
            if not dec.launch:
                continue
            if not row["worktree"]:
                say(f"land lane: {row['name']}: WAITING TO LAND with no worktree; not launched (enqueue.py needs one)")
                continue
            try:
                rec = launch(cdir, repo, row, dec.why, start, argv_for(row))
            except OSError as exc:
                failed += 1
                say(f"land lane: {row['name']}: the runner did not start ({exc})")
                continue
            branches.setdefault(row["name"], []).append(rec)
            say(f"land lane: {row['name']} @{row['tip'][:12]}: enqueue.py land started ({dec.why}; pid {rec['pid']}, "
                f"{rec['log']})")
        return 1 if failed else 0


# ── the runner ─────────────────────────────────────────────────────────────────────────────────────────────────────
def run_attempt(path: pathlib.Path) -> int:
    """Run the attempt's argv to completion, its output appended to the attempt's log (handed over explicitly: a
    detached Windows process's own standard handles are not inherited by a child it starts without redirection), then
    record rc, the verdict and the branch's tip after it."""
    rec = coord.read_json(path, None)
    if not isinstance(rec, dict):
        print(f"land lane: {path} is not an attempt record")
        return 2
    with open(rec["log"], "ab") as log:
        rc = subprocess.run(rec["argv"], cwd=rec.get("cwd") or None, stdin=subprocess.DEVNULL, stdout=log,
                            stderr=subprocess.STDOUT).returncode
    current = coord.read_json(path, rec)
    current.update(rc=rc, ended=_now().isoformat(timespec="seconds"), end_head=tip_of(rec.get("worktree")),
                   verdict=last_verdict(pathlib.Path(rec["log"])))
    coord.write_json(path, current)
    print(f"land lane: attempt {rec['id']} ended exit {rc}: {current['verdict'] or 'no verdict line'}", flush=True)
    return rc


# ── what the enqueue unit and next_unit.py read ────────────────────────────────────────────────────────────────────
ACTION = {STOPPED: "the enqueue unit's: step 4 finisher (UNREVIEWED: step 2 reviewer; a repeated exit 2 or 9: handoff)",
          DONE: "the enqueue unit's: file its leads (step 5), complete its landing record (step 6b), then collect it",
          FREE: "the lane retries it once landing_check.py passes (the in-flight work it shares a file with is gone)",
          ONCE: "the lane retries it once at this head",
          HALTED: "HALT: the operator clears it once the merge queue is ACTIVE",
          "running": "being landed"}


@dataclasses.dataclass
class Lane:
    survey_at: dt.datetime | None
    rows: list[dict[str, Any]]
    latest: list[tuple[dict[str, Any], str]]   # each branch's latest attempt and its effective class
    halt: str | None

    def survey_age_min(self, now: dt.datetime | None = None) -> float | None:
        return None if self.survey_at is None else ((now or _now()) - self.survey_at).total_seconds() / 60


def view(cdir: pathlib.Path) -> Lane:
    d = lane_dir(cdir)
    try:
        s = coord.read_json(d / SURVEY, None)
    except ValueError:
        s = None
    at = dt.datetime.fromisoformat(s["at"]) if isinstance(s, dict) and s.get("at") else None
    latest = [(atts[-1], effective(atts)) for atts in by_branch(attempts(cdir)).values()]
    halt = (d / HALT).read_text(encoding="utf-8").strip() if (d / HALT).exists() else None
    return Lane(at, (s or {}).get("rows", []) if isinstance(s, dict) else [], latest, halt)


@dataclasses.dataclass(frozen=True)
class AgentWork:
    reasons: list[str]
    survey_age_min: float | None   # None: the lane has never surveyed


def agent_work(cdir: pathlib.Path, now: dt.datetime | None = None) -> AgentWork:
    """What only the enqueue unit's agents (or its clerk and its records) can do: rows WAITING FOR REVIEW or BOUNCED in
    a survey younger than SURVEY_MAX_AGE_MIN, STOPPED attempts not yet collected, and ENQUEUED attempts not yet
    collected (their leads and landing records)."""
    lane = view(cdir)
    age = lane.survey_age_min(now)
    reasons = []
    if age is not None and age <= SURVEY_MAX_AGE_MIN:
        for state in ("WAITING FOR REVIEW", "BOUNCED"):
            names = [r["name"] for r in lane.rows if r.get("state") == state]
            if names:
                reasons.append(f"{state.lower()}: {', '.join(names)}")
    stopped = [f"{a['branch']} ({outcome(a)})" for a, c in lane.latest if c == STOPPED and not a.get("collected")]
    queued = [a["branch"] for a, c in lane.latest if c == DONE and not a.get("collected")]
    if stopped:
        reasons.append(f"stopped landings: {', '.join(stopped)}")
    if queued:
        reasons.append(f"landed by the lane, leads and records to complete: {', '.join(queued)}")
    return AgentWork(reasons, age)


def outcome(a: dict[str, Any]) -> str:
    """`exit N`, `DIED` or the running runner's pid: how an attempt stands, in one word."""
    if "rc" in a:
        return f"exit {a['rc']}"
    return "DIED" if a.get("outcome") == "DIED" else f"pid {a.get('pid')}"


def status_lines(cdir: pathlib.Path, now: dt.datetime | None = None) -> list[str]:
    lane = view(cdir)
    age = lane.survey_age_min(now)
    n = {c: sum(1 for _, k in lane.latest if k == c) for c in ("running", STOPPED, DONE, FREE, ONCE, HALTED)}
    survey = ("no survey yet: the lane has not ticked" if age is None else
              f"survey {age:.0f} min old" + (f" — STALE (over {SURVEY_MAX_AGE_MIN} min): the lane is not ticking; run "
                                              "`land_lane.py tick`" if age > SURVEY_MAX_AGE_MIN else ""))
    out = [f"=== LAND LANE: {n['running']} running · {n[STOPPED]} stopped · {n[DONE]} enqueued · "
           f"{n[FREE] + n[ONCE]} to retry · {survey}{' · HALTED: ' + lane.halt if lane.halt else ''} ==="]
    order = {"running": 0, STOPPED: 1, HALTED: 2, FREE: 3, ONCE: 3, DONE: 4}
    for a, cls in sorted(lane.latest, key=lambda t: (order.get(t[1], 9), t[0]["branch"])):
        mark = " [collected]" if a.get("collected") else ""
        out.append(f"{cls.upper():16} {a['branch']} @{str(a.get('head'))[:12]} {outcome(a)}: {a.get('verdict') or '-'} — "
                   f"{ACTION.get(cls, '')}{mark} (attempt {a['id']}; {a['log']})")
    return out


def collect(cdir: pathlib.Path, branches: list[str], say: Say = print) -> int:
    """Mark each named branch's finished attempts handled by the enqueue unit; a running one is never collected."""
    by = by_branch(attempts(cdir))
    missing = 0
    for b in branches:
        done = [a for a in by.get(b, []) if finished(a) and not a.get("collected")]
        if not done:
            say(f"land lane: {b}: no finished, uncollected attempt")
            missing += 1
            continue
        for a in done:
            a["collected"] = True
            save(a)
        say(f"land lane: {b}: {len(done)} attempt(s) collected")
    return 1 if missing else 0


# ── self-test ──────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    root = pathlib.Path(tempfile.mkdtemp(prefix="land-lane-selftest-"))
    try:
        def fresh(name: str) -> pathlib.Path:
            c = root / name
            (c / "scratch").mkdir(parents=True)
            return c

        def row(name: str, state: str, tip: str) -> dict[str, Any]:
            return {"name": name, "state": state, "tip": tip, "worktree": str(root / f"wt-{name}"), "why": "",
                    "lead": None}

        class Fake:
            """A planted survey, a recording launcher and a liveness oracle: no git, no GitHub, no process."""
            def __init__(self, rows: list[dict[str, Any]]):
                self.rows, self.started, self.live, self.free = rows, [], set(), False
                self.asked: list[str] = []

            def survey(self, repo: pathlib.Path, cdir: pathlib.Path) -> tuple[list, list[str]]:
                return self.rows, [r["name"] for r in self.rows if r["state"] == "WAITING TO LAND"]

            def start(self, argv: list[str], log: pathlib.Path) -> int:
                self.started.append(json.loads(pathlib.Path(argv[-1]).read_text(encoding="utf-8"))["branch"])
                pid = 40000 + len(self.started)
                self.live.add(pid)
                return pid

            def alive(self, pid: int | None) -> bool:
                return pid in self.live

            def overlap(self, wt: str) -> tuple[bool, str]:
                self.asked.append(wt)
                return self.free, "shares scripts/x.py with w1069g (dispatched earlier)"

            def tick(self, cdir: pathlib.Path) -> list[str]:
                before = len(self.started)
                out: list[str] = []
                tick(cdir, root, survey=self.survey, start=self.start, alive=self.alive, overlap_free=self.overlap,
                     say=out.append)
                self.said = out
                return self.started[before:]

        def finish(cdir: pathlib.Path, branch: str, rc: int, verdict: str, end_head: str | None = None) -> None:
            """The runner's own completion of the branch's running attempt (the record `run_attempt` writes)."""
            for a in attempts(cdir):
                if a["branch"] == branch and not finished(a):
                    a.update(rc=rc, verdict=verdict, ended=_now().isoformat(), end_head=end_head or a["head"])
                    save(a)

        # 1. planted survey rows: exactly the WAITING TO LAND rows start, in survey order; a second tick starts none.
        c = fresh("survey")
        f = Fake([row("w1a", "WAITING TO LAND", "a1"), row("w1b", "WAITING FOR REVIEW", "b1"),
                  row("w1c", "WAITING TO LAND", "c1"), row("w1d", "ENQUEUED", "d1"), row("w1e", "IN FLIGHT", "e1")])
        expect("planted survey: the WAITING TO LAND rows start, in survey order, and nothing else", f.tick(c) == ["w1a", "w1c"],
               str(f.started))
        s = coord.read_json(lane_dir(c) / SURVEY, {})
        expect("the tick writes the survey it decided from (survey.json)",
               [r["name"] for r in s.get("rows", [])] == ["w1a", "w1b", "w1c", "w1d", "w1e"], str(s))
        expect("a second tick while both run starts nothing (being landed)", f.tick(c) == [], str(f.started))
        w = agent_work(c)
        expect("agent work: the row WAITING FOR REVIEW, and nothing for the runs in flight",
               w.reasons == ["waiting for review: w1b"], str(w.reasons))

        # 2. exit 5 (GATE RED) is never relaunched at the same head; a new head (a finisher's commit) is eligible.
        f.rows[0] = row("w1a", "DECISION", "a2")                  # enqueue merged main in: the survey no longer lists it
        finish(c, "w1a", 5, "GATE RED (Windows) on a2", end_head="a2")
        f.live.discard(40001)
        f.rows[0] = row("w1a", "WAITING TO LAND", "a2")           # even listed again at that head
        expect("exit 5 is never relaunched at the same head", f.tick(c) == [] and f.tick(c) == [], str(f.started))
        w = agent_work(c)
        expect("a stopped attempt is the enqueue unit's agent work until collected",
               any(r.startswith("stopped landings: w1a (exit 5)") for r in w.reasons), str(w.reasons))
        expect("status names the stopped attempt and its finisher", any(
            ln.startswith("STOPPED") and "w1a" in ln and "step 4 finisher" in ln for ln in status_lines(c)),
            "\n".join(status_lines(c)))
        collect(c, ["w1a"], say=lambda s: None)
        expect("collect clears it from the agent work, and it stays unlaunched at that head",
               not any("w1a" in r for r in agent_work(c).reasons) and f.tick(c) == [], str(agent_work(c).reasons))
        f.rows[0] = row("w1a", "WAITING TO LAND", "a3")           # the finisher's fix, reviewed CLEAN
        expect("a new head is eligible again", f.tick(c) == ["w1a"], str(f.started))

        # 3. exit 3 (the landing check's overlap): retried at the same head only once landing_check passes.
        c3 = fresh("overlap")
        f3 = Fake([row("w2a", "WAITING TO LAND", "h1")])
        f3.tick(c3)
        finish(c3, "w2a", 3, "NOT QUEUED (exit 3, above)", end_head="h2")
        f3.live.clear()
        f3.rows[0] = row("w2a", "IN FLIGHT", "h2")                # it committed (main merged, the stamp): IN FLIGHT
        expect("exit 3 while the overlap remains: not relaunched, and the check is asked",
               f3.tick(c3) == [] and f3.asked == [str(root / "wt-w2a")], f"{f3.started} {f3.asked}")
        expect("exit 3 is the lane's to retry, not the enqueue unit's", agent_work(c3).reasons == [],
               str(agent_work(c3).reasons))
        f3.free = True
        expect("exit 3 once the overlap is gone: retried at the same head", f3.tick(c3) == ["w2a"], str(f3.started))

        # 4. exit 2 / 9 retried ONCE at the same head; the second is STOPPED (the handoff's).
        c4 = fresh("once")
        f4 = Fake([row("w3a", "WAITING TO LAND", "k1")])
        f4.tick(c4)
        finish(c4, "w3a", 9, "NOT QUEUED (exit 9, above)")
        f4.live.clear()
        retried = f4.tick(c4)
        finish(c4, "w3a", 2, "NOT QUEUED — main moved under every one of 3 catch-up merges; re-run")
        f4.live.clear()
        expect("exit 9 is retried once, and a second exit 2 at that head is not", retried == ["w3a"] and f4.tick(c4) == [],
               str(f4.started))
        expect("the repeated exit is STOPPED: the enqueue unit's", any(
            r.startswith("stopped landings: w3a") for r in agent_work(c4).reasons), str(agent_work(c4).reasons))

        # 5. a runner that died with no rc is DIED, then retried once.
        c5 = fresh("died")
        f5 = Fake([row("w4a", "WAITING TO LAND", "m1")])
        f5.tick(c5)
        f5.live.clear()
        expect("a runner gone with no exit code is reaped DIED and retried once at its head",
               f5.tick(c5) == ["w4a"] and any(a.get("outcome") == "DIED" for a in attempts(c5)),
               f"{f5.started} {[a.get('outcome') for a in attempts(c5)]}")

        # 6. exit 8 halts every attempt and names the operator; clear-halt re-arms the lane.
        c6 = fresh("halt")
        f6 = Fake([row("w5a", "WAITING TO LAND", "q1")])
        f6.tick(c6)
        finish(c6, "w5a", 8, "QUEUE NOT ACTIVE (ruleset: DRIFT — x)")
        f6.live.clear()
        pathlib.Path(attempts(c6)[0]["log"]).parent.mkdir(parents=True, exist_ok=True)
        pathlib.Path(attempts(c6)[0]["log"]).write_text("", encoding="utf-8", newline="\n")
        f6.rows.append(row("w5b", "WAITING TO LAND", "r1"))
        expect("exit 8 stops all attempts: none starts, the other ready branch included", f6.tick(c6) == [] and
               (lane_dir(c6) / HALT).exists(), f"{f6.started} {f6.said}")
        import mailbox  # noqa: PLC0415  (this directory's: the sessions' mailbox)
        inbox_files = list(mailbox.inbox(c6, "operator").glob("*.json"))
        expect("exit 8 is named to the operator (one mailbox message)", len(inbox_files) == 1,
               f"{inbox_files} {f6.said}")
        expect("a later tick while halted still starts nothing", f6.tick(c6) == [], str(f6.started))
        (lane_dir(c6) / HALT).unlink()
        expect("after clear-halt both branches start, the halted one again at its head",
               sorted(f6.tick(c6)) == ["w5a", "w5b"], str(f6.started))

        # 7. STOP-UNIT, the loop's STOP and the global stop: nothing starts, yet the tick still reaps and surveys.
        for name, path in (("STOP-UNIT", lambda c: coord.stop_unit(str(c))), ("STOP", lambda c: c / "STOP"),
                           ("global STOP", lambda c: coord.global_stop(str(c)))):
            cs = fresh(f"stop-{name.replace(' ', '-')}")
            fs = Fake([row("w6a", "WAITING TO LAND", "s1")])
            path(cs).write_text("", encoding="utf-8", newline="\n")
            expect(f"{name}: no attempt starts, and the survey is still written",
                   fs.tick(cs) == [] and (lane_dir(cs) / SURVEY).exists(), str(fs.said))

        # 8. ENQUEUED: nothing more at that head; its leads and record are the enqueue unit's until collected.
        c8 = fresh("enqueued")
        f8 = Fake([row("w7a", "WAITING TO LAND", "t1")])
        f8.tick(c8)
        finish(c8, "w7a", 0, "ENQUEUED", end_head="t2")
        f8.live.clear()
        f8.rows[0] = row("w7a", "WAITING TO LAND", "t2")
        expect("ENQUEUED at this head: never relaunched", f8.tick(c8) == [], str(f8.started))
        expect("an ENQUEUED attempt is agent work (leads, the landing record) until collected",
               agent_work(c8).reasons == ["landed by the lane, leads and records to complete: w7a"],
               str(agent_work(c8).reasons))
        collect(c8, ["w7a"], say=lambda s: None)
        expect("collected: no agent work is left", agent_work(c8).reasons == [], str(agent_work(c8).reasons))
        old = coord.read_json(lane_dir(c8) / SURVEY, {})
        old["at"] = (_now() - dt.timedelta(minutes=SURVEY_MAX_AGE_MIN + 5)).isoformat()
        coord.write_json(lane_dir(c8) / SURVEY, old)
        expect("a survey older than SURVEY_MAX_AGE_MIN is reported stale (the lane is not ticking)",
               agent_work(c8).survey_age_min > SURVEY_MAX_AGE_MIN and "STALE" in status_lines(c8)[0], status_lines(c8)[0])

        # 9. one tick at a time: a held tick lock makes a second tick a no-op.
        c9 = fresh("lock")
        f9 = Fake([row("w8a", "WAITING TO LAND", "u1")])
        held = ExclusiveLock.try_take(lane_dir(c9) / TICK_LOCK, "another tick")
        try:
            expect("a second tick while one holds the lock does nothing", f9.tick(c9) == [] and
                   "already running" in " ".join(f9.said), str(f9.said))
        finally:
            held.release()

        # 10. the real launch path: a DETACHED runner records rc, the verdict and the end head of its argv.
        c10 = fresh("runner")
        wt = root / "wt-runner"
        wt.mkdir()
        g = ["git", "-c", "user.name=lane", "-c", "user.email=lane@invalid", "-c", "init.defaultBranch=main"]
        subprocess.run(g + ["init", "-q"], cwd=wt, check=True, capture_output=True)
        subprocess.run(g + ["commit", "-q", "--allow-empty", "-m", "x"], cwd=wt, check=True, capture_output=True)
        head = subprocess.run(["git", "-C", str(wt), "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
        fake = [sys.executable, "-c", "print('=== ENQUEUE: GATE RED (Windows) on x (w9a) ==='); raise SystemExit(5)"]
        r = {"name": "w9a", "state": "WAITING TO LAND", "tip": head, "worktree": str(wt), "why": "", "lead": None}
        out: list[str] = []
        tick(c10, root, survey=lambda repo, cdir: ([r], ["w9a"]), argv_for=lambda row: fake, say=out.append)
        deadline = time.monotonic() + 120
        while time.monotonic() < deadline and not all(finished(a) for a in attempts(c10)):
            time.sleep(0.2)
        a = (attempts(c10) or [{}])[0]
        expect("a detached runner runs the attempt and records rc 5, the verdict and the end head",
               a.get("rc") == 5 and a.get("verdict") == "GATE RED (Windows) on x" and a.get("end_head") == head,
               f"{a} {out}")

        # 11. a runner that died AFTER enqueue.py committed: the survey reads the branch IN FLIGHT at its new tip, and
        #     the lane retries it from its own record (the DIED attempt's end head is the worktree's tip at the reap).
        c11 = fresh("died-after-commit")
        f11 = Fake([{"name": "w9b", "state": "WAITING TO LAND", "tip": "before-merge", "worktree": str(wt), "why": "",
                     "lead": None}])
        f11.tick(c11)
        f11.live.clear()
        f11.rows[0] = {**f11.rows[0], "state": "IN FLIGHT", "tip": head}
        expect("a runner that died after committing is retried from its record at the branch's new tip",
               f11.tick(c11) == ["w9b"] and attempts(c11)[0].get("end_head") == head,
               f"{f11.started} {attempts(c11)[0]}")
    finally:
        for dirpath, _, files in os.walk(root):  # git writes read-only objects, which rmtree cannot delete on Windows
            for name in files:
                try:
                    os.chmod(os.path.join(dirpath, name), 0o600)
                except OSError:
                    pass
        shutil.rmtree(root, ignore_errors=True)
    print(f"=== land_lane.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--coord", help=f"the coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    sub = ap.add_subparsers(dest="verb")
    t = sub.add_parser("tick", help="reap, survey, start every eligible landing")
    t.add_argument("--repo", type=pathlib.Path, default=coord.REPO, help="the repository (default: this checkout)")
    st = sub.add_parser("status", help="each branch's latest attempt, one line each")
    st.add_argument("--json", action="store_true")
    co = sub.add_parser("collect", help="the enqueue unit handled these branches' finished attempts")
    co.add_argument("branches", nargs="+")
    sub.add_parser("clear-halt", help="the operator: the merge queue is ACTIVE again; the lane may start landings")
    ru = sub.add_parser("run", help=argparse.SUPPRESS)
    ru.add_argument("attempt", type=pathlib.Path)
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    cdir = coord.coord_dir(a.coord)
    if a.verb == "tick":
        return tick(cdir, a.repo.resolve())
    if a.verb == "status":
        if a.json:
            lane = view(cdir)
            print(json.dumps({"survey_at": lane.survey_at.isoformat() if lane.survey_at else None, "halt": lane.halt,
                              "attempts": [{**{k: v for k, v in at.items() if k != "_path"}, "class": cls}
                                           for at, cls in lane.latest],
                              "agent_work": agent_work(cdir).reasons}, indent=1))
        else:
            print("\n".join(status_lines(cdir)))
        return 0
    if a.verb == "collect":
        return collect(cdir, a.branches)
    if a.verb == "clear-halt":
        halt = lane_dir(cdir) / HALT
        if not halt.exists():
            print("land lane: not halted")
            return 0
        print(f"land lane: halt cleared ({halt.read_text(encoding='utf-8').strip()}); the next tick starts landings")
        halt.unlink()
        return 0
    if a.verb == "run":
        return run_attempt(a.attempt)
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
