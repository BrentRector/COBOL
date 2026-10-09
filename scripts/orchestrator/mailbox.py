#!/usr/bin/env python3
"""The two attended sessions' mailbox (`<coord>/mailbox`): send, list, take, finish and watch messages, and keep
`<coord>/operator-session.json`, always through a JSON library and an atomic write (kb/Work PB2482).

    python scripts/orchestrator/mailbox.py send --to operator --kind land --subject "..." --body "..." --ref kb/Work/PB1.md [--ref ...]
                                                [--from mythos] [--needs-owner] [--owner-quote "..."] [--session NAME] [--unless-pending]
    python scripts/orchestrator/mailbox.py list --inbox operator [--json]
    python scripts/orchestrator/mailbox.py take <id>                 # print it; a malformed one is declined, never acted on
    python scripts/orchestrator/mailbox.py done <id> --status done|declined|split [--report PATH-OR-SHA]
    python scripts/orchestrator/mailbox.py watch --inbox operator [--interval 10] [--timeout 0]   # for a background Bash
    python scripts/orchestrator/mailbox.py status                    # this session's lane, open messages, watcher armed?
    python scripts/orchestrator/mailbox.py session [--name NAME] [--session-id ID] [--has-tick true|false]
    python scripts/orchestrator/mailbox.py --self-test

`<coord>/mailbox/README.md` (owner 2026-10-07 13:35 PDT: "pass work to each other without me in the loop") is the
protocol; this tool ENFORCES its message shape, which a hand-written message did not (13:47: a `\\b` escape mangled a path
in `refs`; `operator-session.json` was invalid JSON with unescaped backslashes):
- one message is one JSON file `<yyyyMMdd-HHmmss>-<from>-<kind>-<slug>.json` in `to-<recipient>/`, written whole and
  atomically, with the README's keys (id, from, to, kind, subject, body, refs, needs_owner, owner_quote, created_at,
  sender_session);
- `kind` is one of KINDS; `body` is at most 900 characters; `refs` is non-empty and every ref that is a path exists
  (relative paths from the repository root); no field carries a control character;
- `task` (Mythos work) and a `dispatch` to the Mythos session carry the owner's approval (`--owner-quote`) or
  `--needs-owner` (MANDATORY-PRACTICES P1: a Mythos dispatch needs the owner's approval each time);
- `from` is `mythos`, `operator`, or `loop` (the unattended supervisor, which posts `publish` to the operator when the
  ledger publish is owed, design section 14);
- the recipient finishes a message with `done`: the `result` {status, at, report} is added and the file MOVES to `done/`,
  so an inbox always equals the open set. This is a transport of pointers, never a work register (CLAUDE.md rule 8).
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import re
import sys
import time
from typing import Any

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import account  # noqa: E402
import coord  # noqa: E402

PARTIES = ("operator", "mythos")
SENDERS = PARTIES + ("loop",)
KINDS = ("land", "dispatch", "publish", "approval", "task", "question", "info")
STATUSES = ("done", "declined", "split")
BODY_MAX = 900
SESSION_FILE = "operator-session.json"
CONTROL = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]")
PATHLIKE = re.compile(r"^(?:[A-Za-z]:[\\/]|[.\\/~])|^[^\s:]+[\\/][^\s]*$")


class Malformed(ValueError):
    """A message that breaks the README's shape: it is refused at send and declined at take."""


def box(cdir: pathlib.Path) -> pathlib.Path:
    return cdir / "mailbox"


def inbox(cdir: pathlib.Path, party: str) -> pathlib.Path:
    return box(cdir) / f"to-{party}"


def _now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc).astimezone()


def _write(path: pathlib.Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    coord.write_json(path, value)


def is_path(ref: str) -> bool:
    return "://" not in ref and bool(PATHLIKE.match(ref))


def problems(m: dict[str, Any], repo: pathlib.Path = coord.REPO) -> list[str]:
    """Why `m` is not a well-formed message, or []."""
    bad = []
    if m.get("from") not in SENDERS:
        bad.append(f"from {m.get('from')!r} is not one of {SENDERS}")
    if m.get("to") not in PARTIES:
        bad.append(f"to {m.get('to')!r} is not one of {PARTIES}")
    if m.get("kind") not in KINDS:
        bad.append(f"kind {m.get('kind')!r} is not one of {KINDS}")
    if not str(m.get("subject") or "").strip() or "\n" in str(m.get("subject") or ""):
        bad.append("subject must be one non-empty line")
    if not str(m.get("body") or "").strip():
        bad.append("body is empty")
    elif len(m["body"]) > BODY_MAX:
        bad.append(f"body is {len(m['body'])} characters, the limit is {BODY_MAX}: the detail belongs in a ref")
    refs = m.get("refs")
    if not isinstance(refs, list) or not [r for r in refs if str(r).strip()]:
        bad.append("refs is empty: every message points at a note, brief, report or branch")
        refs = []
    for k in ("subject", "body", "owner_quote", "sender_session", *[f"refs[{i}]" for i in range(len(refs))]):
        v = refs[int(k[5:-1])] if k.startswith("refs[") else m.get(k)
        if isinstance(v, str) and CONTROL.search(v):
            bad.append(f"{k} carries a control character (an escape mangled in a hand-built string?): {v!r}")
    for r in refs:
        r = str(r)
        if is_path(r) and not CONTROL.search(r):
            p = pathlib.Path(os.path.expanduser(r))
            if not (p if p.is_absolute() else repo / p).exists():
                bad.append(f"ref {r!r} names a path that does not exist")
    needs_approval = m.get("kind") == "task" or (m.get("kind") == "dispatch" and m.get("to") == "mythos")
    if needs_approval and not m.get("owner_quote") and not m.get("needs_owner"):
        bad.append(f"a {m.get('kind')} to the Mythos session carries the owner's approval (--owner-quote) or "
                   f"--needs-owner (MANDATORY-PRACTICES P1)")
    return bad


def slug(subject: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", subject.lower()).strip("-")[:40].strip("-") or "message"


def send(cdir: pathlib.Path, *, to: str, kind: str, subject: str, body: str, refs: list[str], sender: str | None = None,
         needs_owner: bool = False, owner_quote: str | None = None, session: str = "", unless_pending: bool = False,
         now: dt.datetime | None = None, repo: pathlib.Path = coord.REPO) -> pathlib.Path | None:
    """Write one message into the recipient's inbox; None when --unless-pending found the same one still open."""
    sender = sender or next(p for p in PARTIES if p != to)
    m = {"id": "", "from": sender, "to": to, "kind": kind, "subject": subject, "body": body, "refs": refs,
         "needs_owner": needs_owner, "owner_quote": owner_quote, "created_at": "", "sender_session": session}
    bad = problems(m, repo)
    if bad:
        raise Malformed("; ".join(bad))
    if unless_pending and any(o.get("kind") == kind and o.get("subject") == subject for o in listing(cdir, to)):
        return None
    now = now or _now()
    base = f"{now:%Y%m%d-%H%M%S}-{sender}-{kind}-{slug(subject)}"
    path, n = inbox(cdir, to) / f"{base}.json", 1
    while path.exists():
        n += 1
        path = inbox(cdir, to) / f"{base}-{n}.json"
    m["id"], m["created_at"] = path.stem, now.isoformat(timespec="seconds")
    _write(path, m)
    return path


def _load(path: pathlib.Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def listing(cdir: pathlib.Path, party: str) -> list[dict[str, Any]]:
    out = []
    for p in sorted(inbox(cdir, party).glob("*.json")):
        try:
            m = _load(p)
        except (OSError, ValueError) as e:
            m = {"id": p.stem, "kind": "?", "subject": f"UNREADABLE: {e}"}
        out.append(dict(m, _path=str(p)))
    return out


STALE_MINUTES = 30


def stale(cdir: pathlib.Path, party: str, now: dt.datetime | None = None,
          minutes: int = STALE_MINUTES) -> list[dict[str, Any]]:
    """The messages in `party`'s inbox open longer than `minutes`: a peer that leaves them is probably not watching,
    and `send` tells the sender so (kb/Work PB2816 item c). A message with no readable created_at is not judged."""
    now = now or _now()
    out = []
    for m in listing(cdir, party):
        try:
            made = dt.datetime.fromisoformat(m.get("created_at") or "")
        except (ValueError, TypeError):   # a hand-edited, non-string created_at is not judged
            continue
        if made.tzinfo is not None and (now - made).total_seconds() > minutes * 60:
            out.append(m)
    return out


def find(cdir: pathlib.Path, mid: str) -> pathlib.Path:
    hits = [p for party in PARTIES for p in [inbox(cdir, party) / f"{mid}.json"] if p.exists()]
    if not hits:
        raise FileNotFoundError(f"no open message {mid!r} in {box(cdir)} (a finished one is in done/)")
    return hits[0]


def done(cdir: pathlib.Path, mid: str, status: str, report: str, now: dt.datetime | None = None) -> pathlib.Path:
    """Add the result and MOVE the message to done/ (the new file is written whole before the inbox copy goes)."""
    if status not in STATUSES:
        raise Malformed(f"status {status!r} is not one of {STATUSES}")
    src = find(cdir, mid)
    try:
        m = _load(src)
    except ValueError:
        m = {"id": mid, "unparsed": src.read_text(encoding="utf-8", errors="replace")}
    m["result"] = {"status": status, "at": (now or _now()).isoformat(timespec="seconds"), "report": report}
    dest = box(cdir) / "done" / src.name
    _write(dest, m)
    src.unlink()
    return dest


def take(cdir: pathlib.Path, mid: str, repo: pathlib.Path = coord.REPO) -> tuple[dict[str, Any] | None, list[str]]:
    """The message to act on, or (None, why) after declining a malformed one (never act on it)."""
    src = find(cdir, mid)
    try:
        m = _load(src)
        bad = problems(m, repo) if isinstance(m, dict) else ["not a JSON object"]
    except ValueError as e:
        bad = [f"not valid JSON: {e}"]
    if bad:
        done(cdir, mid, "declined", "malformed: " + "; ".join(bad))
        return None, bad
    return m, []


def watch(cdir: pathlib.Path, party: str, interval: float, timeout: float) -> list[dict[str, Any]]:
    """Return the inbox as soon as it holds a message; [] once `timeout` seconds pass (0 = wait forever)."""
    start = time.monotonic()
    while True:
        msgs = listing(cdir, party)
        if msgs or (timeout and time.monotonic() - start >= timeout):
            return msgs
        time.sleep(interval)


# THE WATCHER IS THE ONLY WAKE-UP between the two lanes (the ListAgents doorbell does not cross config dirs), and only
# the session itself can arm it: a watcher is useful only as that session's own BACKGROUND Bash task, whose exit
# re-invokes the session. A hook cannot arm it, so every place a session looks at the mailbox — session-probe at every
# start, resume, clear and compaction, and every list and done — says whether THIS session has one (kb/Work PB2814:
# a restarted operator armed none, and the Mythos lane's question waited 1 h 40 min).
WATCH_CMD = "python scripts/orchestrator/mailbox.py watch --inbox {lane} --timeout 0"
CLAUDE_IMAGES = ("claude", "claude.exe")
WATCHER = re.compile(r"mailbox\.py\S*\s(?:.*\s)?watch\b.*?--inbox[=\s]+(\w+)")   # global options may precede watch


def lane(cdir: pathlib.Path, env: dict[str, str] | None = None) -> str | None:
    """The inbox this session answers: None for a loop unit (the supervisor exports coord.LOOP_UNIT_ENV to its units,
    and the attended operator owns the inbox), 'operator' under the config dir operator-session.json names, else
    'mythos'."""
    env = os.environ if env is None else env
    if env.get(coord.LOOP_UNIT_ENV):
        return None
    op = session(cdir, None, None, None).get("config_dir")
    return "operator" if op and account.same_dir(pathlib.Path(op), account.config_dir(env)) else "mythos"


def _ancestors(table: list[dict[str, Any]], pid: int | None) -> list[int]:
    parent = {p["pid"]: p["ppid"] for p in table}
    chain: list[int] = []
    while pid and pid not in chain:
        chain.append(pid)
        pid = parent.get(pid)
    return chain


def session_root(table: list[dict[str, Any]], me: int) -> int | None:
    """The pid of the Claude Code process `me` runs under: its nearest ancestor whose image is claude (or a node
    process running claude, as the CLI runs on Linux)."""
    procs = {p["pid"]: p for p in table}
    for pid in _ancestors(table, me):
        p = procs.get(pid, {})
        name = (p.get("name") or "").lower()
        if name in CLAUDE_IMAGES or (name.startswith("node") and "claude" in (p.get("cmd") or "")):
            return pid
    return None


def watchers(table: list[dict[str, Any]], root: int, inbox_lane: str) -> list[int]:
    """The `mailbox.py watch --inbox <lane>` processes descended from `root`: this session's own watchers. One another
    session armed wakes that session, never this one, so it does not count."""
    found = [p for p in table if (m := WATCHER.search(p.get("cmd") or "")) and m.group(1) == inbox_lane
             and root in _ancestors(table, p["ppid"])]
    pythons = [p["pid"] for p in found if (p.get("name") or "").lower().startswith("python")]
    return sorted(pythons or [p["pid"] for p in found])   # the shell wrappers carry the same command line


def status(cdir: pathlib.Path, *, env: dict[str, str] | None = None, table: list[dict[str, Any]] | None = None,
           me: int | None = None) -> tuple[str, bool]:
    """(one line, armed-or-not-needed): this session's lane, its open messages, and whether THIS session has a watcher
    armed. False only when the session must arm one now."""
    ln = lane(cdir, env)
    if ln is None:
        return "mailbox: loop unit — the attended operator session owns the inbox; arm no watcher here", True
    msgs = listing(cdir, ln)
    urgent = sorted({m["kind"] for m in msgs if m.get("kind") in ("question", "approval", "task")}
                    | ({"needs_owner"} if any(m.get("needs_owner") for m in msgs) else set()))
    oldest = min((m.get("created_at") or "" for m in msgs), default="")
    opened = f"{len(msgs)} open in to-{ln}" + (
        f" (oldest {oldest[11:16]}{'; ' + ', '.join(urgent) if urgent else ''})" if msgs else "")
    table = coord.process_table() if table is None else table
    root = session_root(table, os.getpid() if me is None else me)
    if root is None:
        return f"mailbox ({ln}): {opened}; watcher state unknown (not running under a Claude Code session)", True
    armed = watchers(table, root, ln)
    if armed:
        return f"mailbox ({ln}): {opened}; watcher armed (pid {armed[0]})", True
    act = f"act on them (python scripts/orchestrator/mailbox.py list --inbox {ln}), then " if msgs else ""
    return (f"⛔ mailbox ({ln}): {opened}; NO watcher armed for this session — {act}arm one as a BACKGROUND Bash task: "
            f"`{WATCH_CMD.format(lane=ln)}`, and re-arm it every time it fires (kb/Work PB2814)"), False


def session(cdir: pathlib.Path, name: str | None, session_id: str | None, has_tick: bool | None,
            now: dt.datetime | None = None) -> dict[str, Any]:
    """Read, or update and read, operator-session.json: the operator's account and config dir (account.py), its
    session id and name (the doorbell's address), and whether it runs a tick."""
    path = cdir / SESSION_FILE
    try:
        cur = _load(path)
    except FileNotFoundError:
        cur = {}
    except ValueError:
        cur = {}  # the hand-written file this replaces was invalid JSON once (13:47); a write rebuilds it
    if name is None and session_id is None and has_tick is None:
        return cur
    acct = account.current()
    new = dict(cur, account=acct.name, config_dir=str(acct.config_dir))
    if session_id is not None and session_id != cur.get("session"):
        new.update(session=session_id, started_at=(now or _now()).isoformat(timespec="seconds"))
    new.setdefault("started_at", (now or _now()).isoformat(timespec="seconds"))
    if name is not None:
        new["session_name"] = name
    if has_tick is not None:
        new["has_tick"] = has_tick
    _write(path, new)
    return new


def _self_test() -> int:
    import tempfile
    fails, checked = [], []

    def check(what, got, want):
        checked.append(what)
        if got != want:
            fails.append(f"{what}: got {got!r}, want {want!r}")

    def refused(what, fn):
        try:
            fn()
            check(what, "accepted", "refused")
        except Malformed as e:
            check(what, True, True)
            return str(e)
        return ""

    with tempfile.TemporaryDirectory() as t:
        cdir = pathlib.Path(t)
        note = "kb/Work/PB2482.md"
        when = dt.datetime(2026, 10, 7, 13, 47, 5, tzinfo=dt.timezone(dt.timedelta(hours=-7)))
        p = send(cdir, to="operator", kind="land", subject="Land branch r1 (Draft 7)", body="land it", refs=[note],
                 session="cobol-7c", now=when)
        check("the file name", p.name, "20261007-134705-mythos-land-land-branch-r1-draft-7.json")
        m = _load(p)
        check("the README's keys", sorted(m), sorted(["id", "from", "to", "kind", "subject", "body", "refs", "needs_owner",
                                                      "owner_quote", "created_at", "sender_session"]))
        check("id is the file name", m["id"], p.stem)
        check("created_at has a zone", m["created_at"], "2026-10-07T13:47:05-07:00")
        win = r"E:\COBOL-coord\scratch\briefs\x.md"
        (cdir / "x.md").write_text("x", encoding="utf-8")
        p2 = send(cdir, to="mythos", kind="info", subject="a Windows path", body="b", refs=[str(cdir / "x.md"), "branch x @ abc"],
                  sender="operator", now=when)
        check("a Windows path survives the round trip", _load(p2)["refs"][0], str(cdir / "x.md"))
        check("same second, another file", send(cdir, to="operator", kind="land", subject="Land branch r1 (Draft 7)",
                                                body="again", refs=[note], now=when).name.endswith("-2.json"), True)
        refused("an unknown kind", lambda: send(cdir, to="operator", kind="chat", subject="s", body="b", refs=[note]))
        refused("a body over 900 characters", lambda: send(cdir, to="operator", kind="info", subject="s", body="x" * 901, refs=[note]))
        refused("empty refs", lambda: send(cdir, to="operator", kind="info", subject="s", body="b", refs=[]))
        refused("a path ref that does not exist", lambda: send(cdir, to="operator", kind="info", subject="s", body="b",
                                                               refs=[r"E:\COBOL-coord\nowhere\gone.md"]))
        why = refused("the 13:47 mangled escape (\\b became a backspace)",
                      lambda: send(cdir, to="operator", kind="info", subject="s", body="b", refs=[win.replace("\\b", "\b")]))
        check("the refusal names the control character", "control character" in why, True)
        refused("a Mythos task without the owner's approval", lambda: send(cdir, to="mythos", kind="task", subject="s",
                                                                           body="b", refs=[note], sender="operator"))
        check("a Mythos task with the owner's approval", send(cdir, to="mythos", kind="task", subject="t", body="b",
                                                              refs=[note], sender="operator",
                                                              owner_quote="owner 13:40: go").exists(), True)
        refused("an unknown recipient", lambda: send(cdir, to="nobody", kind="info", subject="s", body="b", refs=[note]))
        # the loop posts an owed publish once, not once per unit
        a = send(cdir, to="operator", kind="publish", subject="Ledger publish owed at abc", body="b", refs=[note],
                 sender="loop", unless_pending=True)
        b = send(cdir, to="operator", kind="publish", subject="Ledger publish owed at abc", body="b", refs=[note],
                 sender="loop", unless_pending=True)
        check("--unless-pending: one open publish", (a is not None, b), (True, None))
        check("list the operator inbox", len(listing(cdir, "operator")), 3)
        # PB2816 c): messages a peer left open over 30 minutes tell the sender it is probably not watching
        check("stale: none is older than 30 minutes at 14:10", len(stale(cdir, "operator", when + dt.timedelta(minutes=23))), 0)
        check("stale: the two sent at 13:47 by 14:20 (the publish was stamped later)",
              [m["created_at"] for m in stale(cdir, "operator", when + dt.timedelta(minutes=33))],
              ["2026-10-07T13:47:05-07:00"] * 2)
        # take + done
        got, why2 = take(cdir, m["id"])
        check("take a good message", (got["kind"], why2), ("land", []))
        d = done(cdir, m["id"], "done", "sha abc", now=when)
        check("done moves it with the result", (p.exists(), _load(d)["result"]["status"], d.parent.name), (False, "done", "done"))
        hand = inbox(cdir, "operator") / "20261007-134800-mythos-info-hand.json"
        hand.write_text(json.dumps({"id": hand.stem, "from": "mythos", "to": "operator", "kind": "info", "subject": "s",
                                    "body": "b", "refs": []}), encoding="utf-8")
        got, why3 = take(cdir, hand.stem)
        check("a hand-built message with empty refs is declined, not taken",
              (got, (box(cdir) / "done" / hand.name).exists(), "refs is empty" in "; ".join(why3)), (None, True, True))
        check("declined carries the reason", "malformed" in _load(box(cdir) / "done" / hand.name)["result"]["report"], True)
        refused("an unknown status", lambda: done(cdir, a.stem, "finished", ""))
        # watch: returns at once when the inbox holds a message; an empty inbox returns [] at the timeout
        check("watch returns the waiting messages", len(watch(cdir, "operator", 0.01, 0)) >= 1, True)
        check("watch on an empty inbox times out empty", watch(cdir, "nobody-here", 0.01, 0.05), [])
        # operator-session.json through a JSON library: backslashes survive, a re-write keeps started_at
        s1 = session(cdir, "COBOL [7a2da9]", "sid-1", True, now=when)
        raw = (cdir / SESSION_FILE).read_text(encoding="utf-8")
        check("operator-session.json is valid JSON with the config dir intact",
              json.loads(raw)["config_dir"], str(account.current().config_dir))
        s2 = session(cdir, "COBOL [renamed]", None, None)
        check("a rename keeps the session and its start", (s2["session"], s2["started_at"], s2["session_name"]),
              ("sid-1", s1["started_at"], "COBOL [renamed]"))
        check("a read changes nothing", session(cdir, None, None, None), s2)
        # PB2814: whose inbox, and does THIS session have a watcher armed?
        op_env = {account.ENV: json.loads(raw)["config_dir"]}
        check("the config dir operator-session.json names is the operator lane", lane(cdir, op_env), "operator")
        check("any other config dir is the Mythos lane", lane(cdir, {account.ENV: str(cdir / "acct1")}), "mythos")
        check("a loop unit answers no inbox", lane(cdir, dict(op_env, **{coord.LOOP_UNIT_ENV: "wave"})), None)
        arm = "python scripts/orchestrator/mailbox.py watch --inbox operator --timeout 0"
        table = [{"pid": 10, "ppid": 1, "name": "claude.exe", "cmd": "claude --resume s"},          # the operator
                 {"pid": 11, "ppid": 10, "name": "bash.exe", "cmd": f"bash -c eval '{arm}'"},
                 {"pid": 12, "ppid": 11, "name": "python.exe", "cmd": arm},
                 {"pid": 20, "ppid": 10, "name": "python.exe", "cmd": "python scripts/hooks/session_start.py"},
                 {"pid": 30, "ppid": 1, "name": "claude.exe", "cmd": "claude --model claude-mythos-5-1"},  # the other
                 {"pid": 31, "ppid": 30, "name": "python.exe", "cmd": arm.replace("operator", "mythos")},
                 {"pid": 40, "ppid": 30, "name": "python.exe", "cmd": "python probe"},
                 {"pid": 50, "ppid": 51, "name": "x", "cmd": ""}, {"pid": 51, "ppid": 50, "name": "y", "cmd": ""}]
        check("the session root is the nearest claude ancestor", (session_root(table, 20), session_root(table, 40)), (10, 30))
        check("a parent cycle ends the walk", session_root(table, 50), None)
        check("this session's watcher is the python process, not its shell wrappers", watchers(table, 10, "operator"), [12])
        check("another session's watcher, or another lane's, is not ours", watchers(table, 30, "operator"), [])
        check("a watcher armed with a global option or --inbox=<lane> is still seen",
              [m.group(1) if (m := WATCHER.search(c)) else None for c in (
                  "python scripts/orchestrator/mailbox.py --coord E:/c watch --inbox operator",
                  "python mailbox.py watch --inbox=mythos --timeout 0")], ["operator", "mythos"])
        line, ok = status(cdir, env=op_env, table=table, me=20)
        check("armed: one plain line", (ok, "watcher armed (pid 12)" in line, line.startswith("⛔")), (True, True, False))
        bare = [p for p in table if p["pid"] not in (11, 12)]
        line, ok = status(cdir, env=op_env, table=bare, me=20)
        check("unarmed: a ⛔ line that names the command to arm",
              (ok, line.startswith("⛔"), f"`{arm}`" in line, "open in to-operator" in line), (False, True, True, True))
        line, ok = status(cdir, env=op_env, table=table, me=40)
        check("a watcher armed by ANOTHER session does not arm this one", (ok, line.startswith("⛔")), (False, True))
        check("a loop unit is told to arm nothing",
              status(cdir, env=dict(op_env, **{coord.LOOP_UNIT_ENV: "land"}), table=bare, me=20)[1], True)
        check("outside a Claude session the state is unknown, not a false alarm",
              status(cdir, env=op_env, table=table, me=99), (status(cdir, env=op_env, table=table, me=99)[0], True))
        check("unknown says so", "watcher state unknown" in status(cdir, env=op_env, table=table, me=99)[0], True)
    for f in fails:
        print("FAIL:", f)
    print(f"mailbox self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    s = sub.add_parser("send")
    s.add_argument("--to", required=True, choices=PARTIES)
    s.add_argument("--from", dest="sender", choices=SENDERS, help="default: the other party")
    s.add_argument("--kind", required=True, choices=KINDS)
    s.add_argument("--subject", required=True)
    s.add_argument("--body", required=True)
    s.add_argument("--ref", action="append", default=[], required=True)
    s.add_argument("--needs-owner", action="store_true")
    s.add_argument("--owner-quote")
    s.add_argument("--session", default="", help="the sender's session name (sender_session)")
    s.add_argument("--unless-pending", action="store_true", help="send nothing while the same kind and subject is open")
    li = sub.add_parser("list")
    li.add_argument("--inbox", required=True, choices=PARTIES)
    li.add_argument("--json", action="store_true")
    tk = sub.add_parser("take")
    tk.add_argument("id")
    dn = sub.add_parser("done")
    dn.add_argument("id")
    dn.add_argument("--status", required=True, choices=STATUSES)
    dn.add_argument("--report", default="")
    w = sub.add_parser("watch")
    w.add_argument("--inbox", required=True, choices=PARTIES)
    w.add_argument("--interval", type=float, default=10)
    w.add_argument("--timeout", type=float, default=0, help="seconds; 0 waits until a message arrives")
    sub.add_parser("status", help="this session's lane, open messages and whether it has a watcher armed (PB2814)")
    se = sub.add_parser("session")
    se.add_argument("--name")
    se.add_argument("--session-id")
    se.add_argument("--has-tick", choices=("true", "false"))
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return _self_test()
    if not a.cmd:
        ap.error("a subcommand or --self-test")
    cdir = coord.coord_path(a.coord)
    try:
        if a.cmd == "send":
            p = send(cdir, to=a.to, kind=a.kind, subject=a.subject, body=a.body, refs=a.ref, sender=a.sender,
                     needs_owner=a.needs_owner, owner_quote=a.owner_quote, session=a.session,
                     unless_pending=a.unless_pending)
            print(f"already open: a {a.kind} '{a.subject}' (nothing sent)" if p is None else p)
            if old := stale(cdir, a.to):
                print(f"⚠ to-{a.to} already holds {len(old)} message(s) open over {STALE_MINUTES} min (oldest "
                      f"{min(m['created_at'] for m in old)[11:16]}): the {a.to} session is probably not watching its "
                      f"inbox; tell the owner or that session (kb/Work PB2816)")
        elif a.cmd == "list":
            msgs = listing(cdir, a.inbox)
            if a.json:
                print(json.dumps(msgs, indent=1, ensure_ascii=False))
            else:
                for m in msgs:
                    print(f"{m.get('id')}  {m.get('kind')}  from {m.get('from')}"
                          f"{'  NEEDS OWNER' if m.get('needs_owner') else ''}  {m.get('subject')}")
                print(f"{len(msgs)} open in to-{a.inbox}")
                print(status(cdir)[0])
        elif a.cmd == "take":
            m, bad = take(cdir, a.id)
            if m is None:
                print(f"DECLINED {a.id} (moved to done/): {'; '.join(bad)}", file=sys.stderr)
                return 1
            print(json.dumps(m, indent=1, ensure_ascii=False))
        elif a.cmd == "done":
            print(done(cdir, a.id, a.status, a.report))
            line, armed = status(cdir)
            if not armed:
                print(line)
        elif a.cmd == "watch":
            msgs = watch(cdir, a.inbox, a.interval, a.timeout)
            for m in msgs:
                print(f"{m.get('_path')}  {m.get('kind')}  {m.get('subject')}")
            if msgs:
                print(f"this watcher has exited: act on the inbox, then re-arm it as a BACKGROUND Bash task: "
                      f"`{WATCH_CMD.format(lane=a.inbox)}` (kb/Work PB2814)")
            return 0 if msgs else 1
        elif a.cmd == "status":
            print(status(cdir)[0])
        elif a.cmd == "session":
            tick = None if a.has_tick is None else a.has_tick == "true"
            print(json.dumps(session(cdir, a.name, a.session_id, tick), indent=1))
    except (Malformed, FileNotFoundError, account.UnknownAccount) as e:
        print(f"mailbox: {e}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
