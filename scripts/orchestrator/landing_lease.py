#!/usr/bin/env python3
"""landing_lease.py — ONE LANDER ON MAIN AT A TIME, mechanically (kb/Work PB2537).

    python scripts/orchestrator/landing_lease.py acquire --holder TEXT --reason TEXT [--wait-min M] [--ttl-min T]
    python scripts/orchestrator/landing_lease.py renew
    python scripts/orchestrator/landing_lease.py release [--outcome TEXT]
    python scripts/orchestrator/landing_lease.py check        # push-main.sh: does THIS worktree hold a live lease?
    python scripts/orchestrator/landing_lease.py status [--json]
    python scripts/orchestrator/landing_lease.py --self-test
    ... [--coord DIR] [--worktree DIR]

WHY. `push-main.sh` serialized only the final push (it refuses a HEAD that is not a descendant of origin/main), but a
landing costs about 50 minutes BEFORE that step: the final rebase, the lander's whole-population gate, the Linux gate,
the oracle and CI. On 2026-10-07 the R1 lander rebased three times as the loop landed trains 1034 and 1034b inside its
window, re-gated each time, and ended SPLIT at its turn cap with every gate and CI green and nothing on main. Owner
19:30 PDT: "this is stupid. we cannot keep running r1 repeatedly for zero gain".

THE LEASE. One JSON file, `<coord>/landing-lease.json`, outside every worktree (coord.py). A lander ACQUIRES it before
its final rebase and its gates, RENEWS it while it works, and `push-main.sh` RELEASES it when the push succeeds or
fails. It records the holder, the reason, the worktree that holds it, a heartbeat and an expiry, so a dead lander
cannot hold main: a lease whose expiry has passed, or whose worktree no longer exists, is TAKEN OVER by the next
acquire (and the takeover is logged). Its owner is the WORKTREE: a lander that resumes in the same worktree re-takes
its own lease, and `push-main.sh` needs no argument to know whether its caller holds it.
  acquire  0 = held now (taken, re-taken by its own worktree, or taken over); 4 = another worktree holds a live
           lease after --wait-min minutes of polling (0 = do not wait). A waiting lander RE-ISSUES it; it never gates
           while another lander holds main, so it never re-gates for a race it was going to lose.
  renew    0 = the heartbeat and the expiry moved; 4 = this worktree no longer holds it (released, or taken over
           after it expired) — acquire again, then rebase onto origin/main before gating.
  release  0 = released (or there was nothing of this worktree's to release); 4 = another worktree holds it, and it
           is left alone: one lander never frees another's lease.
  check    0 = this worktree holds a live lease; 1 = no live lease; 4 = another worktree holds a live lease.
Every verb exits 2, with its reason on stderr, when it cannot answer (a lease file that is not a lease, a lock it
cannot take, no git worktree): never 0, 1 or 4, which push-main.sh acts on.
Every read-modify-write runs under `coord.locked(cdir, "landing-lease.lock")`. Events (acquire, takeover, release) are
appended to `<coord>/landing-lease.jsonl`, the record of how long landers waited and what was taken over.

Callers: the lander briefs (.claude/skills/workstream/templates/lander-train-brief.md step 2b, lander-brief.md step 2b),
`scripts/push-main.sh` (takes the lease itself when its caller holds none, refuses with exit 4 when another holds it,
renews it while CI runs, releases it on exit), `next_unit.py` (no `land` unit while a lease is live) and
`stop.ps1 -Status`. Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 4.7.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import platform
import queue
import shutil
import subprocess
import sys
import tempfile
import threading
import time
import traceback
from typing import Any

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402
import sharedfile  # noqa: E402

FILE = "landing-lease.json"
LOG = "landing-lease.jsonl"
LOCK = "landing-lease.lock"
SCHEMA = 1
#: A lander renews at every step and in every blocking wait (each under 10 minutes), and push-main.sh every minute while
#: CI runs, so a live lander's heartbeat is never this old; a lander that died is overtaken this long after its last one.
DEFAULT_TTL_MIN = 30.0
DEFAULT_POLL_S = 10.0
#: The exit codes (the module docstring says what each verb means by them).
OK, FREE, ERROR, HELD_ELSEWHERE = 0, 1, 2, 4


def _say(line: str) -> None:
    """Every line is flushed at once: a waiting lander's line must reach a piped reader before its poll sleeps."""
    print(line, flush=True)


def _now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc)


def _iso(t: dt.datetime) -> str:
    return t.astimezone(dt.timezone.utc).isoformat(timespec="seconds")


def _when(text: str) -> dt.datetime:
    return dt.datetime.fromisoformat(text)


def _same_tree(a: str, b: str) -> bool:
    return os.path.normcase(os.path.realpath(a)) == os.path.normcase(os.path.realpath(b))


def default_worktree() -> str:
    """The caller's worktree: the top level of the git tree its current directory is in."""
    r = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, encoding="utf-8")
    if r.returncode != 0 or not r.stdout.strip():
        raise SystemExit("landing_lease: not inside a git worktree (pass --worktree DIR)")
    return r.stdout.strip()


def _branch(worktree: str) -> str:
    r = subprocess.run(["git", "-C", worktree, "branch", "--show-current"], capture_output=True, text=True,
                       encoding="utf-8")
    return r.stdout.strip() if r.returncode == 0 else ""


def read(cdir: pathlib.Path) -> dict[str, Any] | None:
    """The lease file as written, live or not (None: no lease). A file that is not a lease stops the caller loudly:
    guessing it free would let two landers onto main, guessing it held would block every landing without a reason."""
    path = cdir / FILE
    try:
        lease = coord.read_json(path, None)
    except ValueError as e:
        raise SystemExit(f"landing_lease: {path} is not valid JSON ({e}); inspect it, and delete it only when no "
                         "lander holds main") from None
    if lease is not None and not (isinstance(lease, dict) and {"holder", "worktree", "expires_at"} <= lease.keys()):
        raise SystemExit(f"landing_lease: {path} is not a landing lease: {lease!r}")
    return lease


def snapshot(cdir: pathlib.Path) -> dict[str, Any] | None:
    """`read` under the lease's lock, for the readers (next_unit.py, check, status): a read never overlaps a write."""
    with coord.locked(cdir, LOCK):
        return read(cdir)


def dead_reason(lease: dict[str, Any], now: dt.datetime) -> str:
    """Why a lease no longer binds anyone ('' while it is live): its expiry passed, or its worktree is gone."""
    if _when(lease["expires_at"]) <= now:
        return f"expired at {_iso(_when(lease['expires_at']))}"
    if not os.path.isdir(lease["worktree"]):
        return f"its worktree {lease['worktree']} no longer exists"
    return ""


def current(cdir: pathlib.Path, now: dt.datetime | None = None) -> dict[str, Any] | None:
    """The LIVE lease, or None (no lease, or a dead one the next acquire takes over). next_unit.py's question."""
    lease = snapshot(cdir)
    return lease if lease and not dead_reason(lease, now or _now()) else None


def describe(lease: dict[str, Any], now: dt.datetime | None = None) -> str:
    now = now or _now()
    beat = (now - _when(lease["heartbeat_at"])).total_seconds() / 60
    return (f"held by {lease['holder']} ({lease['worktree']}"
            + (f", branch {lease['branch']}" if lease.get("branch") else "")
            + f") since {_iso(_when(lease['acquired_at']))}, heartbeat {beat:.0f} min ago, expires "
            f"{_iso(_when(lease['expires_at']))}: {lease['reason']}")


def _log(cdir: pathlib.Path, event: dict[str, Any]) -> None:
    with (cdir / LOG).open("a", encoding="utf-8") as f:
        f.write(json.dumps(event) + "\n")


def try_acquire(cdir: pathlib.Path, worktree: str, holder: str, reason: str, ttl_min: float,
                now: dt.datetime | None = None) -> tuple[bool, dict[str, Any], str]:
    """One attempt: (held, the lease now in force, what happened)."""
    with coord.locked(cdir, LOCK):
        now = now or _now()
        lease = read(cdir)
        if lease and _same_tree(lease["worktree"], worktree):
            lease.update(heartbeat_at=_iso(now), expires_at=_iso(now + dt.timedelta(minutes=lease["ttl_min"])))
            coord.write_json(cdir / FILE, lease)
            return True, lease, "re-taken by its own worktree"
        dead = dead_reason(lease, now) if lease else ""
        if lease and not dead:
            return False, lease, "held elsewhere"
        new = {"schema": SCHEMA, "holder": holder, "reason": reason, "worktree": worktree, "branch": _branch(worktree),
               "host": platform.node(), "acquired_at": _iso(now), "heartbeat_at": _iso(now),
               "expires_at": _iso(now + dt.timedelta(minutes=ttl_min)), "ttl_min": ttl_min}
        if lease:
            new["took_over"] = {"holder": lease["holder"], "worktree": lease["worktree"], "why": dead}
        coord.write_json(cdir / FILE, new)
        _log(cdir, {"at": _iso(now), "event": "takeover" if lease else "acquire", "holder": holder,
                    "worktree": worktree, **({"from": new["took_over"]} if lease else {})})
        return True, new, f"took over the lease of {lease['holder']} ({dead})" if lease else "taken"


def acquire(cdir: pathlib.Path, worktree: str, holder: str, reason: str, ttl_min: float, wait_min: float,
            poll_s: float) -> int:
    deadline = time.monotonic() + wait_min * 60
    started = time.monotonic()
    announced = False
    while True:
        held, lease, what = try_acquire(cdir, worktree, holder, reason, ttl_min)
        if held:
            waited = time.monotonic() - started
            _say(f"landing-lease: ACQUIRED ({what}{f', after waiting {waited / 60:.1f} min' if announced else ''}): "
                + describe(lease))
            return OK
        if time.monotonic() >= deadline:
            _say(f"landing-lease: WAIT: {describe(lease)}. Another lander holds main: do not rebase or gate; "
                "re-issue this acquire (with --wait-min) until it prints ACQUIRED.")
            return HELD_ELSEWHERE
        if not announced:
            _say(f"landing-lease: waiting: {describe(lease)}")
            announced = True
        time.sleep(poll_s)


def renew(cdir: pathlib.Path, worktree: str) -> int:
    with coord.locked(cdir, LOCK):
        now = _now()
        lease = read(cdir)
        if not lease or not _same_tree(lease["worktree"], worktree):
            _say("landing-lease: LOST: this worktree does not hold the landing lease ("
                + (describe(lease, now) if lease else "no lease is held")
                + "). Acquire it again, then rebase onto origin/main before you gate.")
            return HELD_ELSEWHERE
        lease.update(heartbeat_at=_iso(now), expires_at=_iso(now + dt.timedelta(minutes=lease["ttl_min"])))
        coord.write_json(cdir / FILE, lease)
    _say(f"landing-lease: renewed until {lease['expires_at']}")
    return OK


def release(cdir: pathlib.Path, worktree: str, outcome: str) -> int:
    with coord.locked(cdir, LOCK):
        now = _now()
        lease = read(cdir)
        if not lease:
            _say("landing-lease: nothing to release (no lease is held)")
            return OK
        if not _same_tree(lease["worktree"], worktree):
            _say(f"landing-lease: NOT released: {describe(lease, now)}; only its own worktree releases it")
            return HELD_ELSEWHERE
        sharedfile.remove(cdir / FILE, missing_ok=False)
        held = (now - _when(lease["acquired_at"])).total_seconds() / 60
        _log(cdir, {"at": _iso(now), "event": "release", "holder": lease["holder"], "worktree": lease["worktree"],
                    "held_min": round(held, 1), "outcome": outcome})
    _say(f"landing-lease: released by {lease['holder']} after {held:.0f} min ({outcome or 'no outcome given'})")
    return OK


def check(cdir: pathlib.Path, worktree: str) -> int:
    lease = current(cdir)
    if not lease:
        _say("landing-lease: no live lease")
        return FREE
    if _same_tree(lease["worktree"], worktree):
        _say(f"landing-lease: this worktree holds it: {describe(lease)}")
        return OK
    _say(f"landing-lease: {describe(lease)}")
    return HELD_ELSEWHERE


def status_line(cdir: pathlib.Path, now: dt.datetime | None = None) -> str:
    now = now or _now()
    lease = snapshot(cdir)
    if not lease:
        return "landing lease: free"
    dead = dead_reason(lease, now)
    if dead:
        return f"landing lease: DEAD ({dead}): the next acquire takes it over; was {describe(lease, now)}"
    return f"landing lease: {describe(lease, now)}"


# ── self-test ───────────────────────────────────────────────────────────────────────────────────────────────────

#: The self-test's arms, by name (LandingLeaseDriftTests asserts each one ran and passed).
ARMS = ("one lander holds the lease: a second acquire is refused, at once or when its wait runs out",
        "a second lander waits instead of gating, and gates once the first releases",
        "an expired lease is taken over, and so is one whose worktree is gone",
        "only the holder renews or releases; a lost lease says so",
        "the holder's own worktree re-takes its lease, by any spelling of its path, and its expiry moves",
        "parallel acquires: exactly one wins",
        "check answers push-main: own, free or held elsewhere",
        "a file that is not a lease stops every verb loudly",
        "the loop starts no land unit while a lease is live")
_GUARD_S = 90.0  # a HANG guard for the waiting child: never a speed bound


def _cli(cdir: pathlib.Path, *argv: str) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), *argv, "--coord", str(cdir)],
                          capture_output=True, text=True, encoding="utf-8", timeout=_GUARD_S)


def _self_test(root: pathlib.Path) -> list[str]:
    fails: list[str] = []

    def expect(arm: str, cond: bool, what: str) -> None:
        if not cond:
            fails.append(f"{arm}: {what}")

    trees = {name: root / name for name in ("lander-a", "lander-b", "lander-c")}
    for t in trees.values():
        t.mkdir()
    a, b, c = (str(t) for t in trees.values())

    # 1. one holder; a second acquire that does not wait is refused at once
    arm = ARMS[0]
    cdir = root / "coord1"
    cdir.mkdir()
    r = _cli(cdir, "acquire", "--holder", "lander A", "--reason", "train 1", "--worktree", a)
    expect(arm, r.returncode == OK and "ACQUIRED (taken)" in r.stdout, f"A's acquire: {r.returncode} {r.stdout}{r.stderr}")
    r = _cli(cdir, "acquire", "--holder", "lander B", "--reason", "train 2", "--worktree", b)
    expect(arm, r.returncode == HELD_ELSEWHERE and "held by lander A" in r.stdout, f"B's acquire: {r.returncode} {r.stdout}")
    expect(arm, (read(cdir) or {}).get("holder") == "lander A", f"the lease changed hands: {read(cdir)}")
    r = _cli(cdir, "acquire", "--holder", "lander B", "--reason", "train 2", "--worktree", b, "--wait-min", "0.005",
             "--poll-s", "0.1")
    expect(arm, r.returncode == HELD_ELSEWHERE and "WAIT: held by lander A" in r.stdout,
           f"B's wait did not run out with WAIT: {r.returncode} {r.stdout}{r.stderr}")
    expect(arm, (read(cdir) or {}).get("holder") == "lander A", f"the wait took the lease: {read(cdir)}")

    # 2. TWO LANDERS: B is a lander process that acquires (waiting) and only then "gates"; while A holds the lease B
    #    has not gated; once A releases, B takes the lease and gates.
    arm = ARMS[1]
    gated = root / "b-gated.txt"
    lander_b = root / "lander_b.py"
    lander_b.write_text(
        "import pathlib, subprocess, sys\n"
        "r = subprocess.run([sys.executable, sys.argv[1], 'acquire', '--holder', 'lander B', '--reason', 'train 2',\n"
        "                    '--worktree', sys.argv[2], '--coord', sys.argv[3], '--wait-min', '30', '--poll-s', '0.2'])\n"
        "if r.returncode == 0:\n"
        "    pathlib.Path(sys.argv[4]).write_text('B gated', encoding='utf-8')\n"
        "sys.exit(r.returncode)\n", encoding="utf-8")
    p = subprocess.Popen([sys.executable, str(lander_b), str(pathlib.Path(__file__).resolve()), b, str(cdir), str(gated)],
                         stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8")
    try:
        lines: "queue.Queue[str]" = queue.Queue()
        reader = threading.Thread(target=lambda: [lines.put(line) for line in p.stdout], daemon=True)
        reader.start()
        try:  # the child prints (and flushes) its waiting line before its first poll sleeps
            first = lines.get(timeout=_GUARD_S)
        except queue.Empty:
            first = ""
        expect(arm, "waiting: held by lander A" in first, f"B did not report that it waits: {first!r}")
        expect(arm, p.poll() is None and not gated.exists(), "B gated while A held the lease")
        r = _cli(cdir, "release", "--worktree", a, "--outcome", "landed")
        expect(arm, r.returncode == OK and "released by lander A" in r.stdout, f"A's release: {r.stdout}{r.stderr}")
        p.wait(timeout=_GUARD_S)
        reader.join(timeout=_GUARD_S)  # the pipe is at EOF once the child has exited
        rest = "".join(lines.get_nowait() for _ in range(lines.qsize()))
        expect(arm, p.returncode == OK and "ACQUIRED (taken, after waiting" in rest, f"B after A released: {p.returncode} {rest}")
        expect(arm, gated.exists(), "B never gated after it took the lease")
        expect(arm, (read(cdir) or {}).get("holder") == "lander B", f"the lease is not B's: {read(cdir)}")
    finally:
        if p.poll() is None:
            p.kill()
            p.communicate()
    events = [json.loads(line)["event"] for line in (cdir / LOG).read_text(encoding="utf-8").splitlines()]
    expect(arm, events == ["acquire", "release", "acquire"], f"the event log: {events}")

    # 3. a dead lander cannot hold main: an expired lease, and one whose worktree is gone, are taken over
    arm = ARMS[2]
    cdir = root / "coord3"
    cdir.mkdir()
    old = {"schema": SCHEMA, "holder": "dead lander", "reason": "train 0", "worktree": a, "branch": "", "host": "h",
           "acquired_at": "2000-01-01T00:00:00+00:00", "heartbeat_at": "2000-01-01T00:00:00+00:00",
           "expires_at": "2000-01-01T00:30:00+00:00", "ttl_min": 30.0}
    coord.write_json(cdir / FILE, old)
    expect(arm, current(cdir) is None and "DEAD (expired" in status_line(cdir), f"an expired lease reads live: {status_line(cdir)}")
    r = _cli(cdir, "acquire", "--holder", "lander C", "--reason", "train 3", "--worktree", c)
    expect(arm, r.returncode == OK and "took over the lease of dead lander (expired" in r.stdout, f"takeover: {r.stdout}{r.stderr}")
    expect(arm, (read(cdir) or {}).get("took_over", {}).get("holder") == "dead lander", f"the takeover is not recorded: {read(cdir)}")
    gone = root / "removed-worktree"
    coord.write_json(cdir / FILE, {**old, "worktree": str(gone), "expires_at": "2999-01-01T00:00:00+00:00"})
    r = _cli(cdir, "acquire", "--holder", "lander C", "--reason", "train 3", "--worktree", c)
    expect(arm, r.returncode == OK and "no longer exists" in r.stdout, f"a vanished worktree's lease held: {r.stdout}")
    events = [json.loads(line)["event"] for line in (cdir / LOG).read_text(encoding="utf-8").splitlines()]
    expect(arm, events == ["takeover", "takeover"], f"the event log: {events}")

    # 4. only the holder renews or releases
    arm = ARMS[3]
    before = read(cdir)
    r = _cli(cdir, "renew", "--worktree", b)
    expect(arm, r.returncode == HELD_ELSEWHERE and "LOST" in r.stdout, f"a non-holder renewed: {r.stdout}")
    r = _cli(cdir, "release", "--worktree", b)
    expect(arm, r.returncode == HELD_ELSEWHERE and read(cdir) == before, f"a non-holder released: {r.stdout}")
    coord.write_json(cdir / FILE, {**before, "expires_at": "2000-01-01T00:00:00+00:00"})
    r = _cli(cdir, "renew", "--worktree", c)
    lease = read(cdir) or {}
    expect(arm, r.returncode == OK and _when(lease["expires_at"]) > _now(), f"the holder's renew: {r.stdout} {lease}")
    r = _cli(cdir, "release", "--worktree", c, "--outcome", "CI red")
    expect(arm, r.returncode == OK and read(cdir) is None, f"the holder's release: {r.stdout}{r.stderr}")
    r = _cli(cdir, "renew", "--worktree", c)
    expect(arm, r.returncode == HELD_ELSEWHERE and "no lease is held" in r.stdout, f"a renew after release: {r.stdout}")
    r = _cli(cdir, "release", "--worktree", c)
    expect(arm, r.returncode == OK and "nothing to release" in r.stdout, f"a second release: {r.stdout}")

    # 5. a lander resuming in its own worktree re-takes its lease (holder and start kept)
    arm = ARMS[4]
    _cli(cdir, "acquire", "--holder", "lander A", "--reason", "train 4", "--worktree", a)
    first = read(cdir) or {}
    r = _cli(cdir, "acquire", "--holder", "lander A (resumed)", "--reason", "train 4", "--worktree", a)
    again = read(cdir) or {}
    expect(arm, r.returncode == OK and "re-taken by its own worktree" in r.stdout, f"the resume: {r.stdout}")
    expect(arm, again.get("holder") == "lander A" and again.get("acquired_at") == first.get("acquired_at"),
           f"the resume replaced the lease: {first} -> {again}")
    soon = _now() + dt.timedelta(minutes=1)
    coord.write_json(cdir / FILE, {**again, "expires_at": _iso(soon)})
    alias = str(pathlib.Path(a) / ".." / pathlib.Path(a).name)
    r = _cli(cdir, "acquire", "--holder", "lander A", "--reason", "train 4", "--worktree", alias)
    moved = _when((read(cdir) or {}).get("expires_at", "2000-01-01T00:00:00+00:00"))
    expect(arm, r.returncode == OK and "re-taken by its own worktree" in r.stdout and moved > soon,
           f"the re-take by {alias}: {r.returncode} {r.stdout} expires {moved}")
    for verb in ("renew", "check"):
        r = _cli(cdir, verb, "--worktree", alias)
        expect(arm, r.returncode == OK, f"{verb} by {alias}: {r.returncode} {r.stdout}")

    # 6. parallel acquires from six worktrees, released together: exactly one holds the lease
    arm = ARMS[5]
    cdir = root / "coord6"
    cdir.mkdir()
    racers = []
    for k in range(6):
        t = root / f"racer-{k}"
        t.mkdir()
        racers.append(subprocess.Popen([sys.executable, str(pathlib.Path(__file__).resolve()), "acquire", "--holder",
                                        f"racer {k}", "--reason", "race", "--worktree", str(t), "--coord", str(cdir)],
                                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8"))
    codes = [q.wait(timeout=_GUARD_S) for q in racers]
    for q in racers:
        q.communicate()
    winner = (read(cdir) or {}).get("holder", "")
    expect(arm, sorted(codes) == [OK] + [HELD_ELSEWHERE] * 5, f"exit codes {codes}")
    expect(arm, winner == f"racer {codes.index(OK)}" if OK in codes else False, f"the lease is {winner!r}, codes {codes}")

    # 7. check, as push-main.sh asks it
    arm = ARMS[6]
    win = root / f"racer-{codes.index(OK) if OK in codes else 0}"
    expect(arm, _cli(cdir, "check", "--worktree", str(win)).returncode == OK, "the holder's check is not 0")
    expect(arm, _cli(cdir, "check", "--worktree", a).returncode == HELD_ELSEWHERE, "another worktree's check is not 4")
    _cli(cdir, "release", "--worktree", str(win))
    expect(arm, _cli(cdir, "check", "--worktree", a).returncode == FREE, "a free lease's check is not 1")
    s = _cli(cdir, "status")
    expect(arm, s.returncode == OK and s.stdout.strip() == "landing lease: free", f"status: {s.stdout}")

    # 8. a corrupt or foreign file is neither free nor held: every verb stops with a reason (push-main dies on it)
    arm = ARMS[7]
    for body, why in (("{ not json", "is not valid JSON"), ('{"holder": "x"}', "is not a landing lease")):
        (cdir / FILE).write_text(body, encoding="utf-8")
        for verb in (["acquire", "--holder", "z", "--reason", "z", "--worktree", a], ["check", "--worktree", a],
                     ["status"], ["renew", "--worktree", a], ["release", "--worktree", a]):
            r = _cli(cdir, *verb)
            expect(arm, r.returncode == ERROR and why in r.stderr,
                   f"{verb[0]} on {body!r}: exit {r.returncode} {r.stdout}{r.stderr}")
        expect(arm, (cdir / FILE).read_text(encoding="utf-8") == body, f"a verb rewrote the unreadable lease {body!r}")

    # 9. the loop (next_unit.py rules 2 and 6): finished branches, or a handoff naming `land`, start a land unit only
    #    while no lease is live; meanwhile rule 7 chooses and the reason names the holder
    arm = ARMS[8]
    import next_unit  # noqa: PLC0415  (the loop's chooser; imported here so the lease module does not depend on it)
    next_unit.repo_state = lambda repo: ""  # the repository rule is not under test: no git call on a temp directory
    cdir = root / "coord9"
    cdir.mkdir()
    now = _now()
    coord.write_json(cdir / "readings.json", [{"noted_at": _iso(now), "account": "acct"}])
    done = {"outcome": "done", "branches_pending": [{"branch": "w9a", "status": "DONE"}]}
    named = {"outcome": "done", "next_unit": "land", "next_unit_reason": "train ready"}

    def choose(handoff: dict) -> dict:
        return next_unit.choose(handoff, cdir, root, now, False, None, 3.0, "acct")

    expect(arm, choose(done)["unit"] == "land" and choose(named)["unit"] == "land", "no lease, and no land unit")
    try_acquire(cdir, a, "attended R1 lander", "R1", DEFAULT_TTL_MIN, now)
    for handoff, why in ((done, "w9a"), (named, "named by the last handoff")):
        c = choose(handoff)
        expect(arm, c["unit"] == "wave" and f"land deferred ({why}): the landing lease is held by attended R1 lander"
               in c["reason"], f"a land unit while the lease is held: {c}")
    release(cdir, a, "landed")
    expect(arm, choose(done)["unit"] == "land", "the land did not run once the lease was released")
    return fails


def self_test() -> int:
    d = pathlib.Path(tempfile.mkdtemp(prefix="landing-lease-"))
    try:
        fails = _self_test(d)
    finally:
        shutil.rmtree(d, ignore_errors=True)
    for name in ARMS:
        bad = [f for f in fails if f.startswith(name)]
        print(f"  {'FAIL' if bad else 'PASS'}  {name}" + "".join(f"\n        {f}" for f in bad))
    print(f"=== landing_lease SELF-TEST: {'PASS' if not fails else 'FAIL'} ({len(ARMS)} arms) ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    for stream in (sys.stdout, sys.stderr):  # a cp1252 console must not turn a holder's name into a crash
        stream.reconfigure(encoding="utf-8", errors="replace")
    if (argv if argv is not None else sys.argv[1:]) == ["--self-test"]:
        return self_test()
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="verb", required=True)
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--coord", help="the coordination directory (default: coord.py's)")
    common.add_argument("--worktree", help="the lander's worktree (default: the git top level of the current directory)")
    acq = sub.add_parser("acquire", parents=[common], help="take the lease, waiting up to --wait-min minutes")
    acq.add_argument("--holder", required=True, help="who: e.g. 'train 1037 lander (loop land unit)'")
    acq.add_argument("--reason", required=True, help="what is landing: the train, its clusters or notes")
    acq.add_argument("--wait-min", type=float, default=0.0, help="poll this long while another holds it (default 0)")
    acq.add_argument("--ttl-min", type=float, default=DEFAULT_TTL_MIN, help=f"expiry after the last heartbeat (default {DEFAULT_TTL_MIN:.0f})")
    acq.add_argument("--poll-s", type=float, default=DEFAULT_POLL_S, help=argparse.SUPPRESS)
    sub.add_parser("renew", parents=[common], help="move the heartbeat and the expiry (the holder only)")
    rel = sub.add_parser("release", parents=[common], help="free the lease (the holder only)")
    rel.add_argument("--outcome", default="", help="landed | CI red | refused | dropped …")
    sub.add_parser("check", parents=[common], help="0 = this worktree holds a live lease, 1 = free, 4 = held elsewhere")
    st = sub.add_parser("status", parents=[common], help="one line: free, HELD or DEAD")
    st.add_argument("--json", action="store_true", help="the lease file as JSON (null when free)")
    a = ap.parse_args(argv)
    if a.verb == "acquire" and (a.ttl_min <= 0 or a.wait_min < 0):
        ap.error("--ttl-min must be positive and --wait-min non-negative")
    try:
        return _run(a)
    except SystemExit as e:
        if isinstance(e.code, str):  # a refusal with its reason (the lease file, the lock, the worktree): ERROR
            print(e.code, file=sys.stderr, flush=True)
            return ERROR
        raise
    except Exception:  # an unforeseen failure must not exit 1, which `check` means as "no live lease"
        traceback.print_exc()
        return ERROR


def _run(a: argparse.Namespace) -> int:
    cdir = coord.coord_dir(a.coord)
    if a.verb == "status":
        print(json.dumps(snapshot(cdir)) if a.json else status_line(cdir))
        return OK
    worktree = a.worktree or default_worktree()
    if a.verb == "acquire":
        return acquire(cdir, worktree, a.holder, a.reason, a.ttl_min, a.wait_min, a.poll_s)
    if a.verb == "renew":
        return renew(cdir, worktree)
    if a.verb == "release":
        return release(cdir, worktree, a.outcome)
    return check(cdir, worktree)


if __name__ == "__main__":
    sys.exit(main())
