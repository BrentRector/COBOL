#!/usr/bin/env python3
"""gate_slot.py — THE CROSS-WORKTREE GATE CAP: one FIFO counting semaphore per repository (kb/Work PB1708, PB1720).

    python scripts/gate_slot.py run [--label TEXT] -- <command> [args…]   # take a slot, run the command inside it
    python scripts/gate_slot.py status                                   # the slots, their holders, the queue
    python scripts/gate_slot.py --self-test                              # the five arms of DESIGN §3.14.6

⛔ WHY. Concurrent whole-population implementer gates starve the lander's gate: train 48's lander leg took 30.6 min
against 9.6 quiet. Every `-Mode implementer` gate (and the impact recorder) therefore takes a SLOT before it builds and
holds it through its last leg, so at most N of them build or test at once, repository-wide. The lander, the battery
and CI never take a slot and never wait (DESIGN-test-build-ci.md §3.14.6; the gate driver and the recorder are wired
to it by mechanism M13, §3.14.9).

THE MECHANISM — every piece is an OS file lock, so nothing is ever cleaned up by guessing whether a pid is alive:
  slots    N lock files `<git common dir>/cobol-gate-slots/<k>.lock`, each held by an exclusive OS lock (LockFile on
           Windows, flock on Linux). The OS drops a lock when its holder dies, so a crashed gate frees its slot.
  tickets  A waiter first takes a TICKET: under the exclusive lock of `tickets.lock` it increments the monotonic
           counter in `tickets.seq` and creates and locks `ticket-<seq>.lock`, which it holds while it waits. A ticket
           is LIVE while its file is locked, so a waiter that dies leaves the queue by itself. A waiter may take a free
           slot only when no live ticket has a lower number: a gate that finishes and re-gates at once queues BEHIND
           everyone already waiting instead of winning every race by polling first.
  the tree The slot is held by the holder's whole PROCESS TREE. On Windows the holder puts itself into a Job object
           with JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE before it spawns anything, so every descendant is in the job and, if
           the holder dies, the OS kills them all as its handle closes — an orphaned build or test host can never run
           on outside the cap. On Linux the slot's descriptor is inherited by the children (`Slot.spawn_kwargs`), and
           an flock lives until the LAST descriptor closes, so an orphaned tree keeps the slot until it exits.
  order    Acquisition nests in one fixed order — (the caller's worktree lock, then) ticket, then slot — so no two
           gates can wait on each other in a cycle.

N is `COBOLNET_GATE_SLOTS`, else DEFAULT_SLOTS. ⛔ The cap does not span operating systems: a Windows lock and a WSL
lock on a drvfs mount do not see each other. The repository's gates run on Windows; a WSL run is an ad hoc Linux
reproduction, and the self-test proves the Linux arm there.
"""
from __future__ import annotations

import argparse
import os
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import threading
import time
import queue
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
SLOT_DIR_NAME = "cobol-gate-slots"
SLOTS_ENV = "COBOLNET_GATE_SLOTS"

# ⛔ PROVISIONAL (DESIGN §3.14.6, kb/Work PB1720): the default is to be the largest N at which the lander's whole-
# Conformance leg stays within 1.25x of its quiet time with N implementer gates running, builds included. That needs a
# quiet host and was not measured when the tool landed (the host was running wave 71); 2 is the provisional value
# until the measurement recorded in PB1720 replaces it.
DEFAULT_SLOTS = 2
DEFAULT_POLL_S = 1.0

# Windows LockFile locks a byte RANGE. The range sits far past the end of the file, so the holder's label written at
# the start of the file stays readable to `status` while the lock is held (locking beyond EOF is legal on Windows).
_LOCK_OFFSET = 1 << 30

IS_WINDOWS = os.name == "nt"

if IS_WINDOWS:
    import msvcrt

    def _try_lock(fd: int) -> bool:
        os.lseek(fd, _LOCK_OFFSET, os.SEEK_SET)
        try:
            msvcrt.locking(fd, msvcrt.LK_NBLCK, 1)
            return True
        except OSError as e:
            # LockFile's refusal surfaces as EACCES (13) or EDEADLOCK (36); anything else is a real failure.
            if e.errno in (13, 36):
                return False
            raise

    def _unlock(fd: int) -> None:
        os.lseek(fd, _LOCK_OFFSET, os.SEEK_SET)
        msvcrt.locking(fd, msvcrt.LK_UNLCK, 1)
else:
    import fcntl

    def _try_lock(fd: int) -> bool:
        try:
            fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
            return True
        except BlockingIOError:
            return False

    def _unlock(fd: int) -> None:
        fcntl.flock(fd, fcntl.LOCK_UN)


def _open(path: Path) -> int:
    # os.open descriptors are non-inheritable (PEP 446): only a slot's descriptor is ever handed to children, and only
    # on Linux, by Slot.spawn_kwargs.
    return os.open(path, os.O_RDWR | os.O_CREAT | getattr(os, "O_BINARY", 0), 0o666)


def _write_label(fd: int, label: str) -> None:
    """Record who holds a lock at the start of its file — informational only; `status` shows it, nothing decides on it."""
    os.lseek(fd, 0, os.SEEK_SET)
    data = label.encode("utf-8")
    os.write(fd, data)
    os.ftruncate(fd, len(data))


def _read_label(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8", errors="replace").strip()
    except OSError:
        return ""


def slot_dir(repo: Path = REPO) -> Path:
    """`<git common dir>/cobol-gate-slots` of the repository `repo` is a checkout of — one directory shared by the main
    checkout and every linked worktree."""
    out = subprocess.run(["git", "rev-parse", "--git-common-dir"], cwd=repo, check=True, capture_output=True,
                         text=True, encoding="utf-8").stdout.strip()
    common = Path(out)
    if not common.is_absolute():
        common = Path(repo) / common
    return common.resolve() / SLOT_DIR_NAME


def configured_slots(value: str | None = None) -> int:
    """N: `value`, else COBOLNET_GATE_SLOTS, else DEFAULT_SLOTS. A malformed value is an error, never a silent default."""
    raw = value if value is not None else os.environ.get(SLOTS_ENV)
    if raw is None or raw == "":
        return DEFAULT_SLOTS
    if not re.fullmatch(r"[1-9][0-9]*", raw.strip()):
        raise ValueError(f"{SLOTS_ENV}={raw!r}: the gate cap must be a positive integer")
    return int(raw)


class _Mutex:
    """The exclusive lock on `tickets.lock` — held only for the few milliseconds of a ticket operation."""

    def __init__(self, path: Path, poll_s: float):
        self._path, self._poll_s, self._fd = path, min(poll_s, 0.05), -1

    def __enter__(self) -> "_Mutex":
        self._fd = _open(self._path)
        while not _try_lock(self._fd):
            time.sleep(self._poll_s)
        return self

    def __exit__(self, *exc) -> None:
        _unlock(self._fd)
        os.close(self._fd)


_TICKET = re.compile(r"ticket-(\d+)\.lock")


def _remove_ticket_file(path: Path) -> None:
    """Delete an unlocked ticket's file. A ticket is live by its LOCK, never by its file, so a file some other process
    (an indexer, an antivirus scan) holds open on Windows is left for the next scan to delete; that is the one
    refusal tolerated here."""
    try:
        path.unlink(missing_ok=True)
    except PermissionError:
        pass


@dataclass
class Slot:
    """A held slot. Release it with `release()` or by leaving its `with` block; the OS releases it if the holder dies."""
    index: int
    count: int
    ticket: int
    waited_s: float
    _fd: int = field(repr=False)

    def spawn_kwargs(self, **kwargs) -> dict:
        """`subprocess` keyword arguments that keep the slot held by a child for as long as it lives. On Linux that is
        the slot's descriptor in `pass_fds`; on Windows nothing is needed, since the job holds the whole tree."""
        if IS_WINDOWS or self._fd < 0:
            return kwargs
        return {**kwargs, "pass_fds": tuple(kwargs.get("pass_fds", ())) + (self._fd,)}

    def describe(self) -> str:
        return f"slot {self.index + 1} of {self.count} (ticket {self.ticket}, waited {self.waited_s:.1f} s)"

    def release(self) -> None:
        if self._fd >= 0:
            _unlock(self._fd)
            os.close(self._fd)
            self._fd = -1

    def __enter__(self) -> "Slot":
        return self

    def __exit__(self, *exc) -> None:
        self.release()


@dataclass(frozen=True)
class SlotState:
    """What `status` reports: which slots are held (with their holders' labels) and the live tickets, in queue order."""
    directory: Path
    count: int
    held: dict[int, str]
    waiting: list[tuple[int, str]]


class GateSlots:
    """The repository's gate cap: N slots, served in ticket order."""

    def __init__(self, directory: Path, count: int, poll_s: float = DEFAULT_POLL_S):
        if count < 1:
            raise ValueError(f"the gate cap must be at least 1 (got {count})")
        self.directory, self.count, self.poll_s = directory, count, poll_s

    @classmethod
    def for_repo(cls, repo: Path = REPO, count: int | None = None, poll_s: float = DEFAULT_POLL_S) -> "GateSlots":
        return cls(slot_dir(repo), count if count is not None else configured_slots(), poll_s)

    def _slot_path(self, k: int) -> Path:
        return self.directory / f"{k}.lock"

    def _take_ticket(self, label: str) -> tuple[int, int]:
        self.directory.mkdir(parents=True, exist_ok=True)
        seq_file = self.directory / "tickets.seq"
        with _Mutex(self.directory / "tickets.lock", self.poll_s):
            text = seq_file.read_text(encoding="ascii").strip() if seq_file.exists() else "0"
            if not text.isdigit():
                # Written only by os.replace below, so a torn counter cannot happen; a hand-edited one must be LOUD —
                # resetting it could hand a new waiter a number below a live one and let it jump the queue.
                raise RuntimeError(f"{seq_file} holds {text!r}, not a ticket number; delete it when no gate is waiting")
            seq = int(text) + 1
            tmp = seq_file.with_suffix(".tmp")
            tmp.write_text(str(seq), encoding="ascii")
            os.replace(tmp, seq_file)
            fd = _open(self.directory / f"ticket-{seq}.lock")
            if not _try_lock(fd):
                os.close(fd)
                raise RuntimeError(f"ticket {seq} is locked by another process — {seq_file} was rolled back by hand")
            _write_label(fd, label)
        return seq, fd

    def _live_tickets_before(self, seq: int | None) -> list[tuple[int, str]]:
        """The live tickets below `seq` (all of them when None), in order; dead ones are deleted on the way. Runs under
        the tickets mutex, so a ticket is never probed between its creation and its lock."""
        live: list[tuple[int, str]] = []
        with _Mutex(self.directory / "tickets.lock", self.poll_s):
            for p in self.directory.glob("ticket-*.lock"):
                m = _TICKET.fullmatch(p.name)
                if not m or (seq is not None and int(m.group(1)) >= seq):
                    continue
                fd = _open(p)
                try:
                    if _try_lock(fd):          # nobody holds it: its waiter died — it leaves the queue
                        _unlock(fd)
                        os.close(fd)
                        fd = -1
                        _remove_ticket_file(p)
                    else:
                        live.append((int(m.group(1)), _read_label(p)))
                finally:
                    if fd >= 0:
                        os.close(fd)
        return sorted(live)

    def _drop_ticket(self, seq: int, fd: int) -> None:
        with _Mutex(self.directory / "tickets.lock", self.poll_s):
            _unlock(fd)
            os.close(fd)
            _remove_ticket_file(self.directory / f"ticket-{seq}.lock")

    def _try_slot(self, label: str) -> tuple[int, int] | None:
        for k in range(self.count):
            fd = _open(self._slot_path(k))
            if _try_lock(fd):
                _write_label(fd, label)
                return k, fd
            os.close(fd)
        return None

    def acquire(self, label: str, say: Callable[[str], None] = print) -> Slot:
        """Wait in ticket order for a slot, then bind this process's tree to it. Blocks until a slot is free and every
        earlier live ticket has been served."""
        start = time.monotonic()
        seq, ticket_fd = self._take_ticket(label)
        try:
            reported = None
            while True:
                ahead = len(self._live_tickets_before(seq))
                got = self._try_slot(label) if ahead == 0 else None
                if got is not None:
                    break
                if ahead != reported:
                    say(f"gate-slot: waiting, {ahead} ahead (ticket {seq}, {self.count} slot(s), {self.directory})")
                    reported = ahead
                time.sleep(self.poll_s)
        except BaseException:
            self._drop_ticket(seq, ticket_fd)
            raise
        # The slot is taken BEFORE the ticket is dropped, so no later waiter can slip in between.
        k, fd = got
        slot = Slot(k, self.count, seq, time.monotonic() - start, fd)
        try:
            self._drop_ticket(seq, ticket_fd)
            _bind_process_tree()
            if not IS_WINDOWS:
                os.set_inheritable(fd, True)
        except BaseException:
            slot.release()  # a holder whose tree cannot be bound must not keep the slot it cannot honour
            raise
        say(f"gate-slot: took {slot.describe()}")
        return slot

    def state(self) -> SlotState:
        held: dict[int, str] = {}
        if self.directory.is_dir():
            for k in range(self.count):
                fd = _open(self._slot_path(k))
                try:
                    if _try_lock(fd):
                        _unlock(fd)
                    else:
                        held[k] = _read_label(self._slot_path(k))
                finally:
                    os.close(fd)
            waiting = self._live_tickets_before(None)
        else:
            waiting = []
        return SlotState(self.directory, self.count, held, waiting)


# ── the process tree ────────────────────────────────────────────────────────────────────────────────────────────

_JOB_HANDLE = None  # kept for the life of the process: closing it would kill the tree it holds


def _bind_process_tree() -> None:
    """Windows: put this process in a kill-on-close Job object, so every process it starts from now on is in the job
    and dies with it. Idempotent. Linux: nothing here — the slot descriptor's inheritance does the job."""
    global _JOB_HANDLE
    if not IS_WINDOWS or _JOB_HANDLE is not None:
        return
    import ctypes
    from ctypes import wintypes

    class IO_COUNTERS(ctypes.Structure):
        _fields_ = [(n, ctypes.c_ulonglong) for n in ("ReadOperationCount", "WriteOperationCount",
                                                      "OtherOperationCount", "ReadTransferCount",
                                                      "WriteTransferCount", "OtherTransferCount")]

    class JOBOBJECT_BASIC_LIMIT_INFORMATION(ctypes.Structure):
        _fields_ = [("PerProcessUserTimeLimit", ctypes.c_int64), ("PerJobUserTimeLimit", ctypes.c_int64),
                    ("LimitFlags", wintypes.DWORD), ("MinimumWorkingSetSize", ctypes.c_size_t),
                    ("MaximumWorkingSetSize", ctypes.c_size_t), ("ActiveProcessLimit", wintypes.DWORD),
                    ("Affinity", ctypes.c_size_t), ("PriorityClass", wintypes.DWORD),
                    ("SchedulingClass", wintypes.DWORD)]

    class JOBOBJECT_EXTENDED_LIMIT_INFORMATION(ctypes.Structure):
        _fields_ = [("BasicLimitInformation", JOBOBJECT_BASIC_LIMIT_INFORMATION), ("IoInfo", IO_COUNTERS),
                    ("ProcessMemoryLimit", ctypes.c_size_t), ("JobMemoryLimit", ctypes.c_size_t),
                    ("PeakProcessMemoryUsed", ctypes.c_size_t), ("PeakJobMemoryUsed", ctypes.c_size_t)]

    JobObjectExtendedLimitInformation = 9
    JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
    k32 = ctypes.WinDLL("kernel32", use_last_error=True)
    k32.CreateJobObjectW.argtypes = [wintypes.LPVOID, wintypes.LPCWSTR]
    k32.CreateJobObjectW.restype = wintypes.HANDLE
    k32.SetInformationJobObject.argtypes = [wintypes.HANDLE, ctypes.c_int, wintypes.LPVOID, wintypes.DWORD]
    k32.SetInformationJobObject.restype = wintypes.BOOL
    k32.AssignProcessToJobObject.argtypes = [wintypes.HANDLE, wintypes.HANDLE]
    k32.AssignProcessToJobObject.restype = wintypes.BOOL
    k32.GetCurrentProcess.restype = wintypes.HANDLE

    job = k32.CreateJobObjectW(None, None)  # NULL security attributes: the handle is not inheritable
    if not job:
        raise ctypes.WinError(ctypes.get_last_error())
    info = JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
    info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
    if not k32.SetInformationJobObject(job, JobObjectExtendedLimitInformation, ctypes.byref(info),
                                       ctypes.sizeof(info)):
        raise ctypes.WinError(ctypes.get_last_error())
    if not k32.AssignProcessToJobObject(job, k32.GetCurrentProcess()):
        raise ctypes.WinError(ctypes.get_last_error())
    _JOB_HANDLE = job


# ── command line ────────────────────────────────────────────────────────────────────────────────────────────────

def _cmd_run(args: argparse.Namespace) -> int:
    command = args.command[1:] if args.command[:1] == ["--"] else args.command
    if not command:
        print("gate_slot.py run: no command given (usage: run [--label TEXT] -- <command> [args…])", file=sys.stderr)
        return 2
    slots = GateSlots.for_repo(args.repo, configured_slots(args.slots), args.poll)
    label = f"{args.label or ' '.join(command)[:120]} (pid {os.getpid()}, {time.strftime('%Y-%m-%d %H:%M:%S')})"
    with slots.acquire(label, say=lambda s: print(s, flush=True)) as slot:
        return subprocess.run(command, **slot.spawn_kwargs()).returncode


def _cmd_status(args: argparse.Namespace) -> int:
    st = GateSlots.for_repo(args.repo, configured_slots(args.slots)).state()
    print(f"gate-slot: {st.directory} — {len(st.held)} of {st.count} slot(s) held, {len(st.waiting)} waiting")
    for k in range(st.count):
        print(f"  slot {k + 1}: " + (f"HELD by {st.held[k] or '(no label)'}" if k in st.held else "free"))
    for seq, label in st.waiting:
        print(f"  ticket {seq}: waiting — {label or '(no label)'}")
    return 0


# ── self-test ───────────────────────────────────────────────────────────────────────────────────────────────────

_GUARD_S = 90.0  # a HANG guard: an arm that has not seen its event by then has failed; it is never a speed bound


class _Proc:
    """A child `gate_slot.py run` whose stdout lines are read on a thread, so the test can wait for one. On Linux it
    leads its own session, so `kill()` can take its whole tree (on Windows the tree is the holder's job)."""

    def __init__(self, argv: list[str], cwd: Path):
        self.p = subprocess.Popen(argv, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                                  encoding="utf-8", env={**os.environ, "PYTHONUNBUFFERED": "1"},
                                  start_new_session=not IS_WINDOWS)
        self.lines: "queue.Queue[str]" = queue.Queue()
        self.seen: list[str] = []
        threading.Thread(target=self._pump, daemon=True).start()

    def _pump(self) -> None:
        for line in self.p.stdout:
            self.lines.put(line.rstrip("\n"))
        self.lines.put("\x00EOF")

    def expect(self, pattern: str) -> re.Match:
        deadline = time.monotonic() + _GUARD_S
        rx = re.compile(pattern)
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise AssertionError(f"no line matching /{pattern}/ within the hang guard; saw {self.seen}")
            try:
                line = self.lines.get(timeout=remaining)
            except queue.Empty:
                continue
            if line == "\x00EOF":
                raise AssertionError(f"exited (rc {self.p.wait()}) before a line matching /{pattern}/; saw {self.seen}")
            self.seen.append(line)
            if m := rx.search(line):
                return m

    def finish(self) -> int:
        try:
            return self.p.wait(timeout=_GUARD_S)
        except subprocess.TimeoutExpired:
            raise AssertionError(f"did not exit within the hang guard; saw {self.seen}")

    def kill_holder_only(self) -> None:
        """Kill the `run` process alone, orphaning whatever it started."""
        self.p.kill()
        self.p.wait()

    def kill(self) -> None:
        """Kill the holder's whole tree: the job does it on Windows, the session's process group on Linux."""
        if IS_WINDOWS:
            if self.p.poll() is None:
                self.p.kill()
        else:
            try:
                os.killpg(self.p.pid, 9)
            except ProcessLookupError:
                pass
        self.p.wait()


def _pid_alive(pid: int) -> bool:
    if IS_WINDOWS:
        import ctypes
        k32 = ctypes.WinDLL("kernel32", use_last_error=True)
        k32.OpenProcess.restype = ctypes.c_void_p
        h = k32.OpenProcess(0x00100000 | 0x1000, False, pid)  # SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION
        if not h:
            return False
        try:
            return k32.WaitForSingleObject(ctypes.c_void_p(h), 0) != 0  # 0 = WAIT_OBJECT_0: it has exited
        finally:
            k32.CloseHandle(ctypes.c_void_p(h))
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    try:  # a zombie still answers kill(0); it holds nothing, so it counts as dead
        with open(f"/proc/{pid}/stat", encoding="ascii") as fh:
            return fh.read().split(") ", 1)[1][:1] != "Z"
    except OSError:
        return True


def _wait_until(predicate: Callable[[], bool], what: str) -> None:
    deadline = time.monotonic() + _GUARD_S
    while not predicate():
        if time.monotonic() > deadline:
            raise AssertionError(f"{what}: not observed within the hang guard")
        time.sleep(0.05)


def self_test() -> int:
    me = str(Path(__file__).resolve())
    py = sys.executable
    root = Path(tempfile.mkdtemp(prefix="gate-slot-selftest-"))
    # Every helper body below loops only while this file exists, and the cleanup deletes it FIRST: a red arm (a tree
    # the job failed to kill, a holder that never saw its release) must not leave a process spinning on the host.
    alive = root / "alive"
    alive.touch()
    procs: list[_Proc] = []
    failures: list[str] = []

    def gate(repo: Path, label: str, command: list[str]) -> _Proc:
        pr = _Proc([py, me, "run", "--repo", str(repo), "--slots", "1", "--poll", "0.05", "--label", label, "--",
                    *command], root.parent)  # a cwd inside `root` would pin it against deletion on Windows
        procs.append(pr)
        return pr

    def wait_for(tag: Path) -> str:
        return "\n".join([f"a=pathlib.Path({str(alive)!r}); t=pathlib.Path({str(tag)!r})",
                          "while a.exists() and not t.exists(): time.sleep(0.02)"])

    def hold(tag: Path) -> list[str]:
        """A slot body that prints `held pid=<pid>` and stays until the file `tag` exists."""
        return [py, "-c", "\n".join(["import os,time,pathlib", "print('held pid=%d' % os.getpid(), flush=True)",
                                     wait_for(tag)])]

    def append(log: Path, name: str) -> list[str]:
        return [py, "-c", f"open({str(log)!r},'a').write({name!r}+'\\n')"]

    try:
        git = ["git", "-c", "user.name=gate-slot", "-c", "user.email=gate-slot@invalid", "-c", "init.defaultBranch=main"]
        main_repo, linked = root / "repo", root / "linked"
        main_repo.mkdir()
        for argv in (["init", "-q"], ["commit", "-q", "--allow-empty", "-m", "self-test"],
                     ["worktree", "add", "-q", "--detach", str(linked)]):
            subprocess.run(git + argv, cwd=main_repo, check=True, capture_output=True)

        def arm(name: str, body: Callable[[], None]) -> None:
            try:
                body()
                print(f"  PASS  {name}")
            except AssertionError as e:
                failures.append(name)
                print(f"  FAIL  {name}: {e}")
            finally:
                for pr in procs:
                    pr.kill()
                procs.clear()

        def fifo() -> None:
            go, log = root / "fifo.go", root / "fifo.order"
            h = gate(main_repo, "H", hold(go))
            h.expect(r"^held pid=")
            waiters = []
            for name in ("W1", "W2", "W3"):
                w = gate(main_repo, name, append(log, name))
                w.expect(r"gate-slot: waiting")  # its ticket is live before the next waiter takes one
                waiters.append(w)
            go.touch()
            # H finishes and re-gates AT ONCE: plain polling would let it win the race it starts closest to.
            again = gate(main_repo, "H-again", append(log, "H-again"))
            for pr in (h, *waiters, again):
                assert pr.finish() == 0, f"a gate exited non-zero; saw {pr.seen}"
            order = log.read_text().split()
            assert order == ["W1", "W2", "W3", "H-again"], f"served out of ticket order: {order}"

        def killed_holder() -> None:
            go = root / "killed.never"
            h = gate(main_repo, "doomed", hold(go))
            h.expect(r"^held pid=")
            w = gate(main_repo, "next", [py, "-c", "print('ran', flush=True)"])
            w.expect(r"gate-slot: waiting")
            h.kill()                                     # the holding tree dies (the orphan arm is separate)
            w.expect(r"^ran$")
            assert w.finish() == 0

        def dead_waiter() -> None:
            go = root / "dead.go"
            h = gate(main_repo, "H", hold(go))
            h.expect(r"^held pid=")
            doomed = gate(main_repo, "doomed-waiter", [py, "-c", "print('doomed ran', flush=True)"])
            doomed.expect(r"gate-slot: waiting, 0 ahead")
            later = gate(main_repo, "later", [py, "-c", "print('ran', flush=True)"])
            later.expect(r"gate-slot: waiting, 1 ahead")
            doomed.kill()
            later.expect(r"gate-slot: waiting, 0 ahead")  # the dead ticket left the queue by itself
            go.touch()
            later.expect(r"^ran$")
            assert h.finish() == 0 and later.finish() == 0

        def orphaned_tree() -> None:
            go = root / "orphan.never"
            grandchild_body = "\n".join(["import time,pathlib", wait_for(go)])
            # close_fds=False: the grandchild inherits what its parent holds, as a dotnet test host does.
            spawner = "\n".join([
                "import os,subprocess,sys,time,pathlib",
                f"g = subprocess.Popen([sys.executable, '-c', {grandchild_body!r}], close_fds=False)",
                "print('tree pid=%d grandchild=%d' % (os.getpid(), g.pid), flush=True)",
                wait_for(go)])
            h = gate(main_repo, "tree", [py, "-c", spawner])
            m = h.expect(r"^tree pid=(\d+) grandchild=(\d+)")
            child, grandchild = int(m.group(1)), int(m.group(2))
            h.kill_holder_only()                         # its tree is now orphaned
            state = GateSlots.for_repo(main_repo, 1)
            if IS_WINDOWS:
                _wait_until(lambda: not _pid_alive(child) and not _pid_alive(grandchild),
                            "the job killing the orphaned child and grandchild")
                _wait_until(lambda: not state.state().held, "the slot freed after the tree died")
            else:
                assert _pid_alive(grandchild), "the grandchild should outlive the holder on Linux"
                assert state.state().held, "an orphaned tree must keep the slot while it runs (flock inheritance)"
                h.kill()                                 # the orphans' process group outlived its leader
                _wait_until(lambda: not state.state().held, "the slot freed once the orphaned tree exited")
            w = gate(main_repo, "after", [py, "-c", "print('ran', flush=True)"])
            w.expect(r"^ran$")
            assert w.finish() == 0

        def across_worktrees() -> None:
            a, b = slot_dir(main_repo), slot_dir(linked)
            assert a == b, f"the main checkout and a linked worktree resolve different slot directories: {a} vs {b}"
            assert a.parent == (main_repo / ".git").resolve(), f"the slot directory is not under the common dir: {a}"
            go = root / "wt.go"
            h = gate(main_repo, "main-checkout", hold(go))
            h.expect(r"^held pid=")
            w = gate(linked, "linked-worktree", [py, "-c", "print('ran', flush=True)"])
            w.expect(r"gate-slot: waiting, 0 ahead")     # the linked worktree sees the main checkout's slot
            go.touch()
            w.expect(r"^ran$")
            assert h.finish() == 0 and w.finish() == 0

        print(f"gate_slot.py --self-test ({'Windows Job object' if IS_WINDOWS else 'Linux flock'} arm)")
        arm("FIFO order: a later waiter never overtakes a live earlier ticket, a re-gate queues last", fifo)
        arm("a killed holder releases its slot", killed_holder)
        arm("a dead waiter leaves the queue", dead_waiter)
        arm("an orphaned tree is killed (Windows) or keeps its slot until it exits (Linux)", orphaned_tree)
        arm("the slots are shared across worktrees", across_worktrees)
    finally:
        alive.unlink(missing_ok=True)
        for pr in procs:
            pr.kill()
        for dirpath, _, files in os.walk(root):  # git writes its objects read-only, which rmtree cannot delete on Windows
            for f in files:
                os.chmod(os.path.join(dirpath, f), stat.S_IWRITE | stat.S_IREAD)
        shutil.rmtree(root, ignore_errors=True)

    if failures:
        print(f"gate_slot.py --self-test: RED — {len(failures)} arm(s) failed")
        return 1
    print("gate_slot.py --self-test: ALL GREEN — 5 arms")
    return 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the five arms of DESIGN-test-build-ci.md §3.14.6")
    sub = ap.add_subparsers(dest="action")
    for name in ("run", "status"):
        p = sub.add_parser(name)
        p.add_argument("--repo", type=Path, default=REPO, help="a checkout of the repository (default: this one)")
        p.add_argument("--slots", help=f"N (default: {SLOTS_ENV}, else {DEFAULT_SLOTS})")
        if name == "run":
            p.add_argument("--label", help="who is waiting, as `status` shows it (default: the command)")
            p.add_argument("--poll", type=float, default=DEFAULT_POLL_S, help=argparse.SUPPRESS)
            p.add_argument("command", nargs=argparse.REMAINDER)
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    try:
        if args.action == "run":
            return _cmd_run(args)
        if args.action == "status":
            return _cmd_status(args)
    except ValueError as e:  # a malformed N: the gate must not start, and must say why
        print(f"gate_slot.py: {e}", file=sys.stderr)
        return 2
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
