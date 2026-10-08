"""The orchestrator's coordination directory: one place that names it, and the atomic JSON write every tool uses.

The directory lives OUTSIDE every git worktree (docs/rearchitecture/DESIGN-orchestrator-loop.md section 2), so no
branch switch, worktree removal or `git clean` can lose a reservation or a meter reading. Default `E:\\COBOL-coord`;
the environment variable `COBOL_COORD_DIR` overrides it (the supervisor exports it, and the tests point it at a temp
directory).
"""
from __future__ import annotations

import contextlib
import json
import os
import pathlib
import re
import sys
import time
from typing import Any, Iterator

# scripts/, for the one Windows share-retry rule (sharedfile.py, kb/Work PB2564), which gate_slot.py uses too. Appended,
# so a module of the importer's own directory is never shadowed.
_SCRIPTS = str(pathlib.Path(__file__).resolve().parents[1])
if _SCRIPTS not in sys.path:
    sys.path.append(_SCRIPTS)
import sharedfile  # noqa: E402

DEFAULT = r"E:\COBOL-coord"
ENV = "COBOL_COORD_DIR"
REPO = pathlib.Path(__file__).resolve().parents[2]


RULES_PATH = pathlib.Path(__file__).resolve().with_name("model_rules.json")


def rules(path: pathlib.Path | None = None) -> dict[str, Any]:
    """model_rules.json: the routing, cost and quota constants budget.py and plan_wave.py share."""
    return json.loads((path or RULES_PATH).read_text(encoding="utf-8"))


def family(model: str) -> str:
    """A model id or alias ('claude-sonnet-5-5', 'sonnet', 'claude-opus-5-5[1m]', 'claude-mythos-5-1') -> 'sonnet' | 'opus' |
    'haiku' | 'fable' | 'mythos' | ''. Fable and Mythos are priced at their own rate (model_rules.json; kb/Work R69)."""
    m = (model or "").lower()
    return next((f for f in ("opus", "sonnet", "haiku", "fable", "mythos") if f in m), "")


def coord_path(override: str | None = None) -> pathlib.Path:
    """The coordination directory's path, WITHOUT creating it (a reader on a machine without one stays read-only)."""
    return pathlib.Path(override or os.environ.get(ENV) or DEFAULT)


def coord_dir(override: str | None = None) -> pathlib.Path:
    """The coordination directory, created when missing (the tools that write into it). It must be ABSOLUTE: the
    default is a Windows path, which on Linux is a relative name, so a writer there would create a private directory
    inside its own checkout and every lock and lease in it would bind no one else."""
    d = coord_path(override)
    if not d.is_absolute():
        raise SystemExit(f"⛔ the coordination directory {d} is not an absolute path on this host (the default "
                         f"{DEFAULT} is a Windows path): set {ENV} to the shared directory")
    d.mkdir(parents=True, exist_ok=True)
    return d


# THE GRACEFUL-STOP FILES (kb/Work PB2483, design section 4.6). A stop is SCOPED: one session's stop must never abort
# another session's agents (2026-10-07 13:26, the loop's wind-down split the Mythos session's refuter).
#   global  <coord>\scratch\STOP           the OWNER's stop: every agent of every session and the loop obey it
#   fleet   <scratch>\STOP-<scope>          ONE fleet's stop: only the agents whose dispatch names it obey it
# Every dispatch names both (make_dispatch_specs.py, wf_rolling_wave.js args); the loop's fleet uses the scope `loop`.
STOP_SCOPE = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]*")


def global_stop(override: str | None = None) -> pathlib.Path:
    """The owner's global stop file. Never creates the directory (a check on a machine without one stays read-only)."""
    return coord_path(override) / "scratch" / "STOP"


def fleet_stop(scratch: pathlib.Path | str, scope: str) -> pathlib.Path:
    """One fleet's own stop file in its scratch directory: `STOP-<scope>` (a wave `w1033`, the loop's `loop`)."""
    if not STOP_SCOPE.fullmatch(scope):
        raise ValueError(f"stop scope {scope!r} is not a plain name ([A-Za-z0-9._-])")
    return pathlib.Path(scratch) / f"STOP-{scope}"


# THE COORDINATION DIRECTORY'S ONE SHORT MUTEX (the allocator's `alloc.lock`, the landing lease's `landing-lease.lock`):
# held for the milliseconds of a read-modify-write of one JSON file, never across a wait. It is an atomic create
# (os.open with O_CREAT|O_EXCL) holding the PID and time, because the coordination tools are short processes that share
# no OS handle. A lock older than LOCK_STALE_S was left by a crashed holder and is broken by an atomic rename to a name
# only the breaker uses, so two breakers cannot both win; a waiter gives up after LOCK_WAIT_S.
LOCK_STALE_S = 60.0
LOCK_WAIT_S = 30.0


@contextlib.contextmanager
def locked(cdir: pathlib.Path, name: str) -> Iterator[None]:
    lock = cdir / name
    deadline = time.monotonic() + LOCK_WAIT_S
    while True:
        try:
            fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            os.write(fd, f"{os.getpid()} {time.time():.3f}\n".encode())
            os.close(fd)
            break
        except FileExistsError:
            try:
                age = time.time() - lock.stat().st_mtime
            except FileNotFoundError:
                continue  # released between our create and our stat: try again at once
            if age > LOCK_STALE_S:
                # Between our stat and our rename another breaker may already have removed the stale lock and a live
                # holder created a fresh one; if the file we moved is fresh, put it back with a link (which, unlike a
                # POSIX rename, never overwrites a lock created meanwhile).
                grave = lock.with_name(f"{name}.stale.{os.getpid()}.{time.time_ns()}")
                with contextlib.suppress(OSError):
                    os.rename(lock, grave)
                    if time.time() - grave.stat().st_mtime <= LOCK_STALE_S:
                        with contextlib.suppress(OSError):
                            os.link(grave, lock)
                    os.remove(grave)
                continue
            if time.monotonic() > deadline:
                raise SystemExit(f"⛔ {name} held for more than {LOCK_WAIT_S:.0f}s: {lock} (a live holder never keeps it "
                                 f"for more than milliseconds; if no coordination tool is running, delete it)")
            time.sleep(0.02)
        except PermissionError:
            # Windows: the file is being deleted by its holder at this instant. Bounded like the wait above: a lock path
            # that can never be created (an ACL, a file stuck delete-pending) fails loudly instead of spinning.
            if time.monotonic() > deadline:
                raise SystemExit(f"⛔ {name} could not be created for {LOCK_WAIT_S:.0f}s: {lock} (access denied)")
            time.sleep(0.02)
    try:
        yield
    finally:
        sharedfile.remove(lock, missing_ok=True)


# THE UNIT'S WIND-DOWN SIGNAL (kb/Work PB2597, design section 4.3). The supervisor creates <coord>\STOP-UNIT to ask the
# running unit to start no new step, and exports LOOP_UNIT_ENV (the unit's type) to the unit's session, so
# scripts/hooks/dispatch_guard.py can refuse a fleet launch in a loop unit once the signal exists; an attended session
# has no LOOP_UNIT_ENV and is never refused for the loop's signal.
LOOP_UNIT_ENV = "COBOL_LOOP_UNIT"


def stop_unit(override: str | None = None) -> pathlib.Path:
    """The supervisor's wind-down signal to the running unit. Never creates the directory."""
    return coord_path(override) / "STOP-UNIT"


# Every coordination read and write goes through sharedfile.py: a reader holding a coordination file open on Windows
# makes a writer's os.replace or unlink fail for milliseconds, and that one refusal is retried there (kb/Work PB2537,
# PB2564).
def read_json(path: pathlib.Path, default: Any) -> Any:
    text = sharedfile.read_text(path)
    return default if text is None else json.loads(text)


def write_json(path: pathlib.Path, value: Any) -> None:
    """Write to a sibling temp file and rename over the target, so a crash never leaves a half-written file."""
    sharedfile.replace_text(path, json.dumps(value, indent=1) + "\n")


def process_name(pid: int) -> str | None:
    """The image name of the live process `pid`, or None when no process has that id (read-only; no psutil here)."""
    if os.name == "nt":
        import subprocess  # noqa: PLC0415
        out = subprocess.run(["tasklist", "/FI", f"PID eq {pid}", "/NH", "/FO", "CSV"], capture_output=True, text=True,
                             encoding="utf-8", errors="replace").stdout
        m = re.match(r'^"([^"]+)","(\d+)"', out.strip())
        return m.group(1) if m and int(m.group(2)) == pid else None
    try:
        return pathlib.Path(f"/proc/{pid}/comm").read_text().strip()
    except OSError:
        return None


# THE LOOP'S LOCK (orchestrate.ps1 Take-Lock): {"pid", "started_at", "host"}, held while the supervisor runs; a lock
# whose PID is gone is stale, exactly as the supervisor itself judges it. While the loop runs, its land unit is the ONE
# landing queue: the attended session dispatches no lander (MANDATORY-PRACTICES O11, kb/Work PB2602).
LOOP_LOCK = "orchestrate.lock"


def loop_state(override: str | None = None) -> tuple[str, str]:
    """('running' | 'stopped', the evidence) for the orchestrator loop, from its lock file."""
    lock = coord_path(override) / LOOP_LOCK
    try:
        held = json.loads(lock.read_text(encoding="utf-8-sig"))
    except FileNotFoundError:
        return "stopped", f"no {LOOP_LOCK}"
    except (OSError, ValueError) as exc:   # mid-write or damaged: assume the loop owns main rather than race it
        return "running", f"{LOOP_LOCK} unreadable ({exc}); assumed running"
    pid = held.get("pid") if isinstance(held, dict) else None
    if isinstance(pid, int) and process_name(pid):
        return "running", f"{LOOP_LOCK}: PID {pid} since {held.get('started_at', '?')}"
    return "stopped", f"stale {LOOP_LOCK} (PID {pid} is gone)"



def _self_test() -> int:
    """loop_state on each lock shape: absent, live PID, dead PID, unreadable."""
    import tempfile
    fails = []
    with tempfile.TemporaryDirectory() as d:
        lock = pathlib.Path(d) / LOOP_LOCK
        cases = [(None, "stopped"), ({"pid": os.getpid(), "started_at": "t"}, "running"),
                 ({"pid": 2 ** 22 + 12345}, "stopped"), ("{not json", "running")]
        for content, want in cases:
            if content is None:
                lock.unlink(missing_ok=True)
            else:
                lock.write_text(content if isinstance(content, str) else json.dumps(content), encoding="utf-8")
            got, why = loop_state(d)
            if got != want:
                fails.append(f"lock {content!r}: want {want}, got {got} ({why})")
    for f in fails:
        print(f"FAIL  {f}")
    print(f"=== COORD SELF-TEST: {'GREEN' if not fails else f'RED ({len(fails)})'} ===")
    return 1 if fails else 0


if __name__ == "__main__":
    import sys
    if sys.argv[1:] == ["loop-state"]:   # what a lander dispatch brief names (MANDATORY-PRACTICES O11)
        state, why = loop_state()
        print(f"LOOP STATE: {state} ({why})")
        sys.exit(0)
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(_self_test())
    print("usage: python scripts/orchestrator/coord.py loop-state | --self-test", file=sys.stderr)
    sys.exit(2)
