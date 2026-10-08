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


# Every coordination read and write goes through sharedfile.py: a reader holding a coordination file open on Windows
# makes a writer's os.replace or unlink fail for milliseconds, and that one refusal is retried there (kb/Work PB2537,
# PB2564).
def read_json(path: pathlib.Path, default: Any) -> Any:
    text = sharedfile.read_text(path)
    return default if text is None else json.loads(text)


def write_json(path: pathlib.Path, value: Any) -> None:
    """Write to a sibling temp file and rename over the target, so a crash never leaves a half-written file."""
    sharedfile.replace_text(path, json.dumps(value, indent=1) + "\n")
