#!/usr/bin/env python3
"""self_tests.py — THE ONE RUNNER OF EVERY SCRIPT SELF-TEST under scripts/ (kb/Work PB2563).

    python scripts/self_tests.py            # every self-test that needs no build, in parallel; exit 0 only when all pass
    python scripts/self_tests.py --built    # the ones that read the BUILT test assemblies (after a solution build)
    python scripts/self_tests.py --list     # print what discovery found, and how each would run here
    python scripts/self_tests.py --self-test

⛔ WHY ONE RUNNER, AND WHY DISCOVERY. Until PB2563 the gates ran self-tests from hand lists: the gate driver's AUDITS
named five, CI's audits job named those five plus `landing_check.py`, linux-gate.sh named three, and eleven of the
fourteen orchestrator self-tests (the loop's unit choice, the allocator's lock, the wave planner, …) ran in no gate and
no CI job at all, so a regression in any of them passed everything. A list goes stale the day a self-test is added.
This runner FINDS them, and the local gate (scripts/run_gate_legs.py AUDITS, before the gate slot), the Linux gate
(scripts/linux-gate.sh leg `selftests`) and CI (the `audits` job on Linux and a step of `windows-build-test`) all run
THIS entry point, so no gate can run a self-test CI does not, or the reverse (the CI invariant, kb/Work PB1957).

THE DISCOVERY RULE (one rule, `discover`): a tracked file under scripts/ is a self-test when
  (a) it HANDLES `--self-test`: a .py file whose code (not a docstring or a comment) compares an argument with the
      string `--self-test` or declares it with `add_argument` — read from its syntax tree, so a script that only
      PASSES `--self-test` to another one is not one; a .sh file with a `--self-test)` case arm or a
      `= "--self-test"` test outside a comment. It runs as `<file> --self-test`.
  (b) otherwise, it is NAMED `test_*.py` or `test_*.ps1`: a standalone test script. It runs bare.
`SelfTestDiscoveryDriftTests` (Unit) holds the rule to a broader net, so a self-test written in a shape this rule
misses fails a test instead of silently never running.

PLATFORM: every self-test runs on every platform unless its file declares otherwise, on a `#` comment line among its
first HEADER_LINES lines (so a docstring that SHOWS the marker, as this one does, never declares it):
    # SELF-TEST-PLATFORM: windows — <why it cannot run elsewhere>
On another platform it is reported `NOT RUN on <platform>: …` with that reason, on its own line and in the verdict —
never skipped silently. CI's `windows-build-test` job runs this runner too, so a Windows-only self-test still runs in
CI.

BUILD: a self-test that reads the BUILT test assemblies (filter_population.py asks vstest which tests a filter selects)
declares it the same way:
    # SELF-TEST-NEEDS: build — <what it reads>
The default run names it `deferred to --built` in its verdict; `--built` runs exactly those, after a solution build
(the gate driver right after its build, concurrently with the legs; CI's `greenfield-unit` job; linux-gate.sh after its
unit and conformance legs). The default run never builds: it is one of the gate's audits, which run before the gate
slot, and the slot is what rations builds.

ISOLATION: each self-test runs with `COBOL_COORD_DIR` pointed at a fresh private directory, so none can read or write
the live coordination state (`E:\\COBOL-coord`: the loop's units, leases and allocations), and without git's
repository-selection variables (`git rev-parse --local-env-vars`), so a self-test that builds a scratch repository
never acts on the caller's (kb/Work PB1719), and with `GIT_CONFIG_GLOBAL` naming a private config that holds only a
git identity, so a self-test that commits behaves the same on a developer's machine and on a CI runner with no global
config. On Windows `bash` is Git for Windows' own (from `git --exec-path`), never System32's WSL launcher, which a
PATH lookup can find first. The public-skills submodule some of them import (`tools/claude-skills`:
`check_practices.py`) is fetched once when absent, as CI's fleet-practices step does.

REUSE (kb/Work PB2914; DESIGN-test-build-ci.md §3.14.10): every run of a self-test is TRACED, and each PASS is recorded
with the content of everything it read (scripts/self_test_inputs.py: the files and directory listings its Python
processes touched, plus what a PowerShell or bash script mentions and the runner itself) in ONE store shared by every
checkout of the repository, `<git common dir>/cobol-self-test-passes/` (beside the gate slots, never keyed on a branch).
`--reuse` — the IMPLEMENTER gate's run — reports a self-test REUSED, without running it, while a recorded PASS's inputs
are all unchanged on this tree; a branch that changes nothing a self-test reads reuses the PASS main's lander gate
recorded, and a change re-runs exactly the self-tests that read what it changed. The default run — the LANDER's gate,
CI, the Linux gate, a hand run — reuses nothing: it runs every self-test, and records each PASS. The `--built` phase
reads build output no key covers, so it always runs and never records.

THE BUDGET (kb/Work PB2914): at most BUDGET_CAP self-tests run at once on the MACHINE, from every gate of every checkout
together — one OS lock file per place under `<git common dir>/cobol-self-test-slots/`. A self-test waits for a place
before it starts and holds it to its end, so one gate alone runs as wide as the cores, and N gates share the cores
instead of multiplying them: a gate's self-test time no longer grows with the number of gates. The implementer's
self-tests keep running BEFORE its gate slot (kb/Work PB2524: a red one fails the gate without queueing behind other
gates' builds and legs, PB2523); the budget is their own, so they never wait for a whole gate either.

NO VERDICT FROM THE CLOCK (the owner's no-wall-clock rule, 2026-09-25): a self-test is never RED for being slow. A run
that is still going HANG_AFTER_S after it took its budget place is stopped and reported HANG, with its output so far —
its own state, verdict and exit code, never RED.

THE VERDICT LINE: `=== SELF-TESTS: GREEN — n ran, r reused (inputs unchanged since a recorded PASS) in m s (k not run on
<platform>) ===`, `=== SELF-TESTS: RED — …` or `=== SELF-TESTS: HANG — …`.
Exit 0 GREEN, 1 RED, 2 NOT RUN (discovery or the submodule fetch failed), 3 HANG (no red, at least one hang).
"""
from __future__ import annotations

import argparse
import ast
import concurrent.futures
import contextlib
import datetime as dt
import functools
import hashlib
import json
import os
import random
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Iterator

import sharedfile  # this directory's: the one Windows share-retry rule for files several processes read and replace
import self_test_inputs as inputs  # this directory's: what a self-test read, and the tracer that records it
from gate_slot import ExclusiveLock, git_common_dir  # this directory's: the OS file lock every gate place is

REPO = Path(__file__).resolve().parents[1]
ROOT = "scripts"
FLAG = "--self-test"
VERDICT = "=== SELF-TESTS: "
#: The submodule `check_practices.py` lives in (test_dispatch_guard.py, test_plan_wave.py import it).
SUBMODULE = "tools/claude-skills"
#: ⛔ A HANG DETECTOR, NEVER A VERDICT ON SPEED (module docstring): counted from the moment the self-test holds its
#: budget place, so waiting behind other gates never counts, and set far above every PASS measured — the slowest,
#: test_orchestrate_stop.ps1, took 466 s beside four other gates (2026-10-10) and 1,291 s at the worst of the unbounded
#: fleet load this budget removed (kb/Work PB2897's measurements). A run past it is HANG, exit HANG_EXIT.
HANG_AFTER_S = 3600
HANG_EXIT = 3
#: The machine-wide budget (module docstring): one place per core, the width one gate alone always ran at.
BUDGET_CAP = os.cpu_count() or 4
BUDGET_DIR = "cobol-self-test-slots"
BUDGET_POLL_S = 0.25
#: The shared PASS store (module docstring), and how many PASS records it keeps per self-test and platform.
PASS_STORE_DIR = "cobol-self-test-passes"
PASS_SCHEMA = 2  # 2: Deps.trees (kb/Work PB2914)
PASS_KEEP = 20
#: A marker counts only on a `#` line among a file's first lines: its header comment.
HEADER_LINES = 5

PLATFORM_RE = re.compile(r"^#\s*SELF-TEST-PLATFORM:\s*(windows|linux)\s*[—-]+\s*(\S.*?)\s*$", re.MULTILINE)
NEEDS_RE = re.compile(r"^#\s*SELF-TEST-NEEDS:\s*(build)\s*[—-]+\s*(\S.*?)\s*$", re.MULTILINE)
SH_FLAG_RE = re.compile(r"""^(?!\s*#).*(?:^|\s)--self-test\)|^(?!\s*#).*=\s*["']--self-test["']""", re.MULTILINE)


def _tool(name: str) -> str:
    """The interpreter the PATH names. On Windows, `bash` is Git Bash (`_git_bash`), never a PATH lookup."""
    if name == "bash" and os.name == "nt":
        return _git_bash()
    return shutil.which(name) or name


@functools.cache
def _git_bash() -> str:
    """Git for Windows' own bash, found from git's install directory (`git --exec-path` is
    <root>/mingw64/libexec/git-core). A bare `bash` handed to CreateProcess is System32's WSL launcher, which runs the
    script in Linux (exit 127 for a Windows path), and a PATH lookup finds that launcher first on a host whose PATH puts
    System32 ahead of Git (a CI runner's can); git's own directory names the same bash on every Windows host."""
    r = subprocess.run(["git", "--exec-path"], capture_output=True, text=True, encoding="utf-8")
    exec_path = r.stdout.strip()
    if r.returncode != 0 or not exec_path:
        raise OSError(f"`git --exec-path` failed (exit {r.returncode}): {r.stderr.strip()}")
    root = Path(exec_path).parents[2]
    for candidate in (root / "bin" / "bash.exe", root / "usr" / "bin" / "bash.exe"):
        if candidate.is_file():
            return str(candidate)
    raise OSError(f"no Git Bash under git's install directory {root} (from `git --exec-path` = {exec_path})")


def this_platform() -> str:
    return "windows" if os.name == "nt" else "linux"


@dataclass(frozen=True)
class SelfTest:
    path: str                 # repository-relative, forward slashes
    handles_flag: bool        # runs as `<file> --self-test` (rule a), else bare (rule b)
    platform: str | None      # None: every platform
    platform_reason: str
    needs_build: bool = False  # reads the built test assemblies: run by `--built`, never by the default run
    build_reason: str = ""

    def command(self) -> list[str]:
        """The command line, run with the repository root as its cwd. The path stays RELATIVE with forward slashes:
        Git Bash on Windows reads the backslashes of an absolute Windows path as escapes."""
        f = self.path
        tail = [FLAG] if self.handles_flag else []
        if self.path.endswith(".py"):
            return [sys.executable, f, *tail]
        if self.path.endswith(".ps1"):
            return [_tool("pwsh"), "-NoProfile", "-NonInteractive", "-File", f, *tail]
        return [_tool("bash"), f, *tail]

    def runs_on(self, platform: str) -> bool:
        return self.platform is None or self.platform == platform


# ── discovery ────────────────────────────────────────────────────────────────────────────────────────────────────────


def _is_flag(node: ast.AST) -> bool:
    return isinstance(node, ast.Constant) and node.value == FLAG


def _contains_flag(node: ast.AST) -> bool:
    return any(_is_flag(n) for n in ast.walk(node))


def py_handles_flag(source: str) -> bool:
    """True when the module's CODE compares an argument with `--self-test` (`"--self-test" in sys.argv`,
    `argv[:1] == ["--self-test"]`) or declares it (`add_argument("--self-test", …)`). A list that only passes the flag
    to a subprocess, a docstring and a comment are not. A module that does not parse raises SyntaxError: discovery
    cannot tell whether it is a self-test, so it fails rather than leaving it out."""
    tree = ast.parse(source)
    for node in ast.walk(tree):
        if isinstance(node, ast.Compare) and _contains_flag(node):
            return True
        if (isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute) and node.func.attr == "add_argument"
                and any(_is_flag(a) for a in node.args)):
            return True
    return False


def classify(rel: str, text: str) -> SelfTest | None:
    """The discovery rule for ONE file (module docstring): a SelfTest, or None when the file is not one."""
    name = rel.rsplit("/", 1)[-1]
    if rel.endswith(".py"):
        handles = py_handles_flag(text)
    elif rel.endswith(".sh"):
        handles = bool(SH_FLAG_RE.search(text))
    elif rel.endswith(".ps1"):
        handles = False
    else:
        return None
    if not handles and not (name.startswith("test_") and name.endswith((".py", ".ps1"))):
        return None
    header = "\n".join(text.splitlines()[:HEADER_LINES])
    m = PLATFORM_RE.search(header)
    b = NEEDS_RE.search(header)
    return SelfTest(rel, handles, m.group(1) if m else None, m.group(2) if m else "", b is not None,
                    b.group(2) if b else "")


def tracked_scripts(repo: Path) -> list[str]:
    out = subprocess.run(["git", "ls-files", "-z", "--", ROOT], cwd=repo, check=True, capture_output=True,
                         env=_scrubbed_env(os.environ)).stdout
    return sorted(p for p in out.decode("utf-8").split("\0") if p.endswith((".py", ".sh", ".ps1")))


def discover(repo: Path = REPO) -> list[SelfTest]:
    found = []
    for rel in tracked_scripts(repo):
        # A tracked script that cannot be read is a discovery FAILURE (the verdict says NOT RUN), never a file quietly
        # left out: an unreadable self-test would otherwise run nowhere while the verdict stayed GREEN.
        try:
            text = (repo / rel).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError) as e:
            raise OSError(f"cannot read the tracked script {rel} to classify it: {e}") from e
        try:
            t = classify(rel, text)
        except SyntaxError as e:
            raise OSError(f"the tracked script {rel} does not parse, so discovery cannot classify it: {e}") from e
        if t is not None:
            found.append(t)
    return found


# ── running ──────────────────────────────────────────────────────────────────────────────────────────────────────────

_GIT_LOCAL_ENV: list[str] | None = None


def _git_local_env_vars() -> list[str]:
    global _GIT_LOCAL_ENV
    if _GIT_LOCAL_ENV is None:
        env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
        _GIT_LOCAL_ENV = subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                                        text=True, encoding="utf-8", env=env, cwd=tempfile.gettempdir()).stdout.split()
    return _GIT_LOCAL_ENV


def _scrubbed_env(base: dict[str, str]) -> dict[str, str]:
    """`base` without git's repository-selection variables (kb/Work PB1719)."""
    drop = set(_git_local_env_vars())
    return {k: v for k, v in base.items() if k not in drop}


def ensure_submodule(repo: Path, say) -> str | None:
    """Fetch the public-skills submodule when this checkout lacks it. Returns the failure, if any."""
    if (repo / SUBMODULE / "skills").is_dir():
        return None
    say(f"self-tests: fetching the {SUBMODULE} submodule (absent in this checkout)")
    r = subprocess.run(["git", "submodule", "update", "--init", SUBMODULE], cwd=repo, capture_output=True, text=True,
                       encoding="utf-8", errors="replace", env=_scrubbed_env(os.environ))
    if r.returncode != 0 or not (repo / SUBMODULE / "skills").is_dir():
        return f"`git submodule update --init {SUBMODULE}` failed (exit {r.returncode}): {r.stderr.strip()}"
    return None


#: A self-test's state in one run: it ran and passed or failed, it hung, a recorded PASS was reused, or it does not
#: run on this platform.
GREEN, RED, HANG, REUSED, NOT_RUN = "GREEN", "RED", "HANG", "REUSED", "NOT RUN"


@dataclass
class Result:
    test: SelfTest
    state: str                # GREEN · RED · HANG · REUSED · NOT RUN
    rc: int | None            # the exit code when it ran; None otherwise
    seconds: float
    output: str
    note: str = ""            # what the console line adds: why no PASS was recorded, which PASS was reused


#: The git identity every self-test commits under, from its private global config (`private_git_config`).
GIT_IDENTITY = ("self-test", "self-test@localhost")


def private_git_config(scratch: Path) -> Path:
    """A private global git config holding only an identity. A self-test that commits in a scratch repository then
    behaves the same on a developer's machine (whose global config names a user, an editor, credential helpers) and on
    a CI runner (whose global config names no user, so `git commit` fails): local == CI, the CI invariant (kb/Work
    PB1957). The system config (Git for Windows' core.autocrlf) is left as it is, as on every host."""
    cfg = scratch / "gitconfig"
    cfg.write_text("\n".join(["[user]", f"\tname = {GIT_IDENTITY[0]}", f"\temail = {GIT_IDENTITY[1]}", ""]),
                   encoding="utf-8")
    return cfg


class Budget:
    """THE MACHINE-WIDE SELF-TEST BUDGET (module docstring): `cap` places, each an OS lock file
    `<directory>/<k>.lock` (gate_slot.ExclusiveLock — the OS drops a lock when its holder dies, so a crashed gate never
    keeps a place). One thread per Budget polls for a place at a time; the others wait on it without spinning, so N
    gates poll the directory N times per BUDGET_POLL_S, never once per waiting self-test."""

    def __init__(self, directory: Path, cap: int):
        if cap < 1:
            raise ValueError(f"a self-test budget needs at least one place, not {cap}")
        self.directory, self.cap = directory, cap
        self._poller = threading.Lock()

    @classmethod
    def for_repo(cls, repo: Path) -> "Budget":
        return cls(git_common_dir(repo) / BUDGET_DIR, BUDGET_CAP)

    @contextlib.contextmanager
    def place(self, label: str) -> Iterator[int]:
        """Hold one place while the block runs; wait for one first. Yields the place's number."""
        lock, k = None, -1
        with self._poller:
            start = random.randrange(self.cap)
            while lock is None:
                for i in range(self.cap):
                    k = (start + i) % self.cap
                    lock = ExclusiveLock.try_take(self.directory / f"{k}.lock", label)
                    if lock is not None:
                        break
                else:
                    time.sleep(BUDGET_POLL_S)
        try:
            yield k
        finally:
            lock.release()


class PassStore:
    """THE SHARED PASS STORE (module docstring): `<directory>/<platform>/<test digest>/<deps digest>.json`, one record
    per distinct set of read contents, written whole by an atomic replace (sharedfile) so a reader sees a record or
    none. A record is a PASS for this tree only while none of what its run read has changed (self_test_inputs.Deps)."""

    def __init__(self, directory: Path):
        self.directory = directory

    @classmethod
    def for_repo(cls, repo: Path) -> "PassStore":
        return cls(git_common_dir(repo) / PASS_STORE_DIR)

    def _dir(self, test: str, platform: str) -> Path:
        return self.directory / platform / hashlib.sha256(test.encode("utf-8")).hexdigest()[:20]

    def _records(self, test: str, platform: str) -> list[Path]:
        """Newest first. A record removed while listed is skipped by the reader."""
        try:
            found = list(self._dir(test, platform).glob("*.json"))
        except OSError:
            return []
        stamped = []
        for p in found:
            with contextlib.suppress(OSError):
                stamped.append((p.stat().st_mtime, p))
        return [p for _, p in sorted(stamped, reverse=True)]

    def find(self, test: str, platform: str, tree: "inputs.Tree") -> dict | None:
        """The newest recorded PASS of `test` whose inputs are all unchanged on `tree`, or None."""
        for path in self._records(test, platform):
            try:
                text = sharedfile.read_text(path)
                rec = json.loads(text) if text is not None else None
            except (OSError, ValueError):
                continue  # pruned or half-gone under us: not a PASS
            if (not isinstance(rec, dict) or rec.get("schema") != PASS_SCHEMA or rec.get("test") != test
                    or rec.get("python") != _python()):
                continue
            try:
                unchanged = not inputs.Deps.from_json(rec["deps"]).changed(tree)
            except (KeyError, TypeError, ValueError, AttributeError):
                continue  # a record of another shape is not a PASS: the self-test runs, never a crash of the gate
            if unchanged:
                return rec
        return None

    def record(self, test: str, platform: str, deps: "inputs.Deps", seconds: float, by: str) -> Path:
        d = self._dir(test, platform)
        d.mkdir(parents=True, exist_ok=True)
        body = json.dumps(deps.to_json(), sort_keys=True)
        path = d / f"{hashlib.sha256(body.encode('utf-8')).hexdigest()[:20]}.json"
        sharedfile.replace_text(path, json.dumps(
            {"schema": PASS_SCHEMA, "test": test, "platform": platform, "python": _python(), "seconds": round(seconds, 1),
             "when": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"), "by": by,
             "deps": deps.to_json()}, indent=1, sort_keys=True) + "\n")
        for old in self._records(test, platform)[PASS_KEEP:]:
            with contextlib.suppress(OSError):
                sharedfile.remove(old, missing_ok=True)
        return path


def _python() -> str:
    return f"{sys.version_info[0]}.{sys.version_info[1]}"


@dataclass
class Runner:
    """One run of a phase's self-tests (module docstring): REUSE (only when `reuse`), the BUDGET, the trace, the PASS
    record and the hang detector."""
    repo: Path
    platform: str
    say: Callable[[str], None]
    reuse: bool = False
    budget: Budget | None = None
    store: PassStore | None = None
    hang_after_s: float = HANG_AFTER_S
    by: str = ""

    def __post_init__(self) -> None:
        self.budget = self.budget or Budget.for_repo(self.repo)
        self.store = self.store or PassStore.for_repo(self.repo)
        self.by = self.by or f"{self.repo} pid {os.getpid()}"
        self.tree = inputs.Tree.load(self.repo)
        self.deriver = inputs.Deriver(self.tree, HEADER_LINES)

    def run(self, tests: list[SelfTest], jobs: int | None = None) -> list[Result]:
        here = [t for t in tests if t.runs_on(self.platform)]
        results = [Result(t, NOT_RUN, None, 0.0, "") for t in tests if not t.runs_on(self.platform)]
        todo = []
        for t in here:
            rec = self.store.find(t.path, self.platform, self.tree) if self.reuse and not t.needs_build else None
            if rec is None:
                todo.append(t)
                continue
            r = Result(t, REUSED, None, 0.0, "", f"the PASS of {rec.get('when', '?')} by {rec.get('by', '?')}")
            results.append(r)
            self._report(r)
        scratch = Path(tempfile.mkdtemp(prefix="self-tests-"))
        try:
            git_config = private_git_config(scratch)
            tracer = inputs.install_tracer(scratch / "tracer")
            workers = max(1, min(len(todo) or 1, jobs or BUDGET_CAP))
            with concurrent.futures.ThreadPoolExecutor(max_workers=workers) as pool:
                futures = [pool.submit(self.run_one, t, scratch, git_config, tracer) for t in todo]
                for f in concurrent.futures.as_completed(futures):
                    r = f.result()
                    results.append(r)
                    self._report(r)
        finally:
            shutil.rmtree(scratch, ignore_errors=True)
        return sorted(results, key=lambda r: r.test.path)

    def _report(self, r: Result) -> None:
        flag = " --self-test" if r.test.handles_flag else ""
        state = f"RED (exit {r.rc})" if r.state == RED else r.state
        self.say(f"self-test {r.test.path}{flag}: {state}"
                 + (f" in {r.seconds:.1f}s" if r.state not in (REUSED, NOT_RUN) else "")
                 + (f" — {r.note}" if r.note else ""))
        if r.state in (RED, HANG):  # a red or hung self-test's COMPLETE output, as for a red gate leg (kb/Work PB1573)
            for line in r.output.rstrip().splitlines():
                self.say(f"    {line}")

    def run_one(self, test: SelfTest, scratch: Path, git_config: Path, tracer: Path) -> Result:
        coord = Path(tempfile.mkdtemp(prefix="coord-", dir=scratch))
        trace = Path(tempfile.mkdtemp(prefix="trace-", dir=scratch))
        env = _scrubbed_env(os.environ)
        env.update(COBOL_COORD_DIR=str(coord), GIT_CONFIG_GLOBAL=str(git_config), PYTHONIOENCODING="utf-8",
                   PYTHONUTF8="1")
        env = inputs.traced_env(env, tracer, trace, self.repo)
        with self.budget.place(f"self-test {test.path} of {self.by}"):
            start = time.monotonic()
            try:
                r = subprocess.run(test.command(), cwd=self.repo, env=env, stdin=subprocess.DEVNULL,
                                   capture_output=True, text=True, encoding="utf-8", errors="replace",
                                   timeout=self.hang_after_s)
            except subprocess.TimeoutExpired as e:
                return Result(test, HANG, None, time.monotonic() - start,
                              f"{_text(e.stdout)}{_text(e.stderr)}\n⛔ HANG: still running {self.hang_after_s:.0f} s "
                              f"after it took its budget place; stopped (a hang detector, never a verdict on speed)")
            except OSError as e:
                return Result(test, RED, 127, time.monotonic() - start, f"⛔ could not start {test.command()[0]}: {e}")
            seconds = time.monotonic() - start
        out = (r.stdout or "") + (r.stderr or "")
        if r.returncode != 0:
            return Result(test, RED, r.returncode, seconds, out)
        try:
            return Result(test, GREEN, 0, seconds, out, self._record(test, trace, seconds))
        except ValueError as e:  # a `SELF-TEST-READS` that names nothing: the self-test's declaration is the defect
            return Result(test, RED, 0, seconds, f"{out}\n⛔ it passed, but its PASS cannot be recorded: {e}")

    def _record(self, test: SelfTest, trace_dir: Path, seconds: float) -> str:
        """Record the PASS with what it read; returns the console note when it records none."""
        if test.needs_build:
            return ""
        seen = inputs.read_trace(trace_dir, self.tree)
        if test.path.endswith(".py") and not seen.saw(test.path):
            return "no PASS recorded: the trace does not show the self-test's own file, so the tracer did not run"
        self.store.record(test.path, self.platform, inputs.run_deps(test.path, self.tree, self.deriver, seen),
                          seconds, self.by)
        whole = {kind for kind, d in seen.git_read if d == ""}
        if whole:
            what = "the whole tree's content" if "content" in whole else "the name of every tracked file"
            return (f"its PASS is keyed on {what}: a git command it ran inside the repository read it "
                    f"({'; '.join(sorted(seen.git_in_repo))})")
        return ""


def _text(b) -> str:
    if b is None:
        return ""
    return b if isinstance(b, str) else b.decode("utf-8", "replace")


def select(tests: list[SelfTest], built: bool) -> tuple[list[SelfTest], list[SelfTest]]:
    """(this phase's self-tests, the other phase's): `--built` runs exactly the ones that need a build."""
    return [t for t in tests if t.needs_build == built], [t for t in tests if t.needs_build != built]


def verdict(results: list[Result], platform: str, wall: float,
            deferred: list[SelfTest] | None = None) -> tuple[int, str]:
    ran = [r for r in results if r.state in (GREEN, RED, HANG)]
    red = [r for r in ran if r.state == RED]
    hung = [r for r in ran if r.state == HANG]
    reused = [r for r in results if r.state == REUSED]
    skipped = [r for r in results if r.state == NOT_RUN]
    by_reason: dict[tuple[str, str], list[str]] = {}
    for r in skipped:  # one reason, said once, however many files share it (the twelve test_orchestrate parts do)
        by_reason.setdefault((r.test.platform or "", r.test.platform_reason), []).append(r.test.path)
    not_run = (f" ({len(skipped)} not run on {platform}: "
               + "; ".join(f"{', '.join(paths)} — {p}-only: {reason}" for (p, reason), paths in by_reason.items()) + ")"
               if skipped else "")
    if deferred:
        not_run += f" ({len(deferred)} deferred to --built: {', '.join(t.path for t in deferred)})"
    hangs = (f"{len(hung)} HUNG (stopped by the hang detector; a hang, not a red): "
             f"{', '.join(r.test.path for r in hung)}" if hung else "")
    if red:
        return 1, (f"{VERDICT}RED — {len(red)} of {len(ran)} failed: {', '.join(r.test.path for r in red)}"
                   f"{'; ' + hangs if hangs else ''}{not_run} ===")
    if hung:
        return HANG_EXIT, f"{VERDICT}HANG — {hangs}{not_run} ==="
    done = f"{len(ran)} ran, {len(reused)} reused (inputs unchanged since a recorded PASS)" if reused else f"{len(ran)} ran"
    return 0, f"{VERDICT}GREEN — {done} in {wall:.0f}s{not_run} ==="


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--list", action="store_true", help="print what discovery found and exit")
    ap.add_argument("--built", action="store_true",
                    help="run the self-tests that read the built test assemblies (SELF-TEST-NEEDS: build)")
    ap.add_argument("--jobs", type=int, default=None,
                    help="how many of this run's self-tests wait for a budget place at once (default: the cores)")
    ap.add_argument("--reuse", action="store_true",
                    help="report a self-test REUSED while a recorded PASS's inputs are unchanged (the implementer gate)")
    ap.add_argument("--self-test", action="store_true", help="prove the discovery rule and the verdict")
    args = ap.parse_args(argv)
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if args.self_test:
        return self_test()
    platform = this_platform()
    try:
        tests = discover(REPO)
    except (OSError, subprocess.CalledProcessError) as e:
        print(f"{VERDICT}NOT RUN — discovery failed: {e} ===")
        return 2
    if args.list:
        for t in tests:
            where = "here" if t.runs_on(platform) else f"NOT on {platform} ({t.platform}-only: {t.platform_reason})"
            phase = "  [--built]" if t.needs_build else ""
            print(f"{t.path}{' --self-test' if t.handles_flag else ''}  [{where}]{phase}")
        print(f"{len(tests)} self-tests")
        return 0
    say = lambda s: print(s, flush=True)  # noqa: E731
    failure = ensure_submodule(REPO, say)
    if failure:
        print(f"{VERDICT}NOT RUN — {failure} ===")
        return 2
    phase, deferred = select(tests, args.built)
    start = time.monotonic()
    try:
        runner = Runner(REPO, platform, say, reuse=args.reuse)
    except (OSError, ValueError, subprocess.CalledProcessError) as e:  # the tree or a declared read cannot be read
        print(f"{VERDICT}NOT RUN — the inputs of the self-tests cannot be read: {e} ===")
        return 2
    results = runner.run(phase, args.jobs)
    rc, line = verdict(results, platform, time.monotonic() - start, None if args.built else deferred)
    print(line, flush=True)
    return rc


# ── the self-test: the discovery rule, the platform marker, the isolation and the verdict ─────────────────────────


def self_test() -> int:
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        print(f"  {'ok  ' if got == want else 'FAIL'} {name}" + ("" if got == want else f": got {got!r}, want {want!r}"))
        if got != want:
            fails.append(name)

    def kind(rel: str, text: str):
        t = classify(rel, text)
        return None if t is None else (t.handles_flag, t.platform)

    # (a) the flag, read from the syntax tree
    check("arm: argparse --self-test is a self-test",
          kind("scripts/x.py", 'ap.add_argument("--self-test", action="store_true")\n'), (True, None))
    check("arm: `\"--self-test\" in sys.argv` is a self-test",
          kind("scripts/x.py", 'import sys\nif "--self-test" in sys.argv: pass\n'), (True, None))
    check("arm: `argv[:1] == [\"--self-test\"]` is a self-test",
          kind("scripts/x.py", 'def main(argv):\n    if argv[:1] == ["--self-test"]: pass\n'), (True, None))
    check("arm: a script that only PASSES --self-test to another is not",
          kind("scripts/x.py", 'CMDS = [["scripts/y.py", "--self-test"]]\n'), None)
    check("arm: a docstring or comment mention is not",
          kind("scripts/x.py", '"""run it with --self-test"""\n# python x.py --self-test\n'), None)
    check("arm: a bash `--self-test)` case arm is a self-test",
          kind("scripts/x.sh", 'case "$1" in\n  --self-test) t ;;\nesac\n'), (True, None))
    check("arm: a bash `= \"--self-test\"` test is a self-test",
          kind("scripts/x.sh", 'if [ "${1:-}" = "--self-test" ]; then t; fi\n'), (True, None))
    check("arm: a bash comment mention is not",
          kind("scripts/x.sh", '# bash x.sh --self-test\necho hi\n'), None)
    # (b) the name
    check("arm: test_*.py is a standalone self-test", kind("scripts/o/test_a.py", "print(1)\n"), (False, None))
    check("arm: test_*.ps1 is a standalone self-test", kind("scripts/o/test_a.ps1", "1\n"), (False, None))
    check("arm: a test_*.py that handles --self-test runs with it",
          kind("scripts/test_tool.py", 'ap.add_argument("--self-test")\n'), (True, None))
    check("arm: a plain script is not", kind("scripts/tool.py", "print(1)\n"), None)
    check("arm: a test_*.sh is not (bash self-tests declare the flag)", kind("scripts/test_a.sh", "true\n"), None)
    # the platform marker
    t = classify("scripts/test_w.ps1", "# SELF-TEST-PLATFORM: windows — needs Task Scheduler\n")
    check("arm: the platform marker is read with its reason", (t.platform, t.platform_reason),
          ("windows", "needs Task Scheduler"))
    check("arm: a windows-only self-test does not run on linux", t.runs_on("linux"), False)
    check("arm: a windows-only self-test runs on windows", t.runs_on("windows"), True)
    b = classify("scripts/x.py", "# SELF-TEST-NEEDS: build — reads the built assemblies\nap.add_argument('--self-test')\n")
    check("arm: the build marker is read with its reason", (b.needs_build, b.build_reason),
          (True, "reads the built assemblies"))
    shown = classify("scripts/test_doc.py", '"""\n1\n2\n3\n4\n    # SELF-TEST-PLATFORM: windows — shown, not declared\n"""\n')
    check("arm: a marker SHOWN past the header (a docstring example) declares nothing", shown.platform, None)
    check("arm: --built runs exactly the build-needing ones, the default run the rest",
          (select([b, t], built=True), select([b, t], built=False)), (([b], [t]), ([t], [b])))
    if os.name == "nt":
        check("arm: on Windows `bash` is Git Bash from git's install directory, never System32's WSL launcher",
              ("system32" in _tool("bash").lower(), Path(_tool("bash")).name.lower()), (False, "bash.exe"))
    check("arm: the default verdict names what it deferred to --built",
          "1 deferred to --built: scripts/x.py" in verdict([], "linux", 0.0, [b])[1], True)

    # the runner over a planted scratch tree: a passing, a failing and a platform-excluded self-test; the isolation
    other = "linux" if this_platform() == "windows" else "windows"
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        (root / "scripts").mkdir()
        (root / "scripts" / "test_green.py").write_text(
            "import os, subprocess, sys\nprint('coord=' + os.environ.get('COBOL_COORD_DIR', ''))\n"
            "print('user=' + subprocess.run(['git', 'config', '--global', 'user.name'], capture_output=True,"
            " text=True).stdout.strip())\n"
            "sys.exit(0 if os.environ.get('GIT_DIR') is None else 3)\n", encoding="utf-8")
        (root / "scripts" / "test_red.py").write_text("import sys\nprint('planted failure')\nsys.exit(1)\n",
                                                      encoding="utf-8")
        (root / "scripts" / "elsewhere.py").write_text(
            f"# SELF-TEST-PLATFORM: {other} — planted\nimport sys\nif '--self-test' in sys.argv: sys.exit(1)\n",
            encoding="utf-8")
        env = _scrubbed_env(os.environ)
        for cmd in (["init", "-q"], ["add", "-A"]):
            subprocess.run(["git", *cmd], cwd=root, check=True, capture_output=True, env=env)
        tests = discover(root)
        check("arm: discovery finds the planted tree's three self-tests", [t.path for t in tests],
              ["scripts/elsewhere.py", "scripts/test_green.py", "scripts/test_red.py"])
        for planted, body in (("broken.py", b"def (:\n"), ("latin1.py", b"# caf\xe9\n")):
            (root / "scripts" / planted).write_bytes(body)
            subprocess.run(["git", "add", "-A"], cwd=root, check=True, capture_output=True, env=env)
            try:
                discover(root)
                check(f"arm: a tracked script discovery cannot read ({planted}) fails discovery, never drops out",
                      "discovered", "raised")
            except OSError as e:
                check(f"arm: a tracked script discovery cannot read ({planted}) fails discovery, never drops out",
                      f"scripts/{planted}" in str(e), True)
            (root / "scripts" / planted).unlink()
            subprocess.run(["git", "add", "-A"], cwd=root, check=True, capture_output=True, env=env)
        said: list[str] = []
        runner = Runner(root, this_platform(), said.append)
        saved = os.environ.get("GIT_DIR")
        os.environ["GIT_DIR"] = str(root / "not-a-repo")   # the caller's variable must not reach a self-test
        try:
            results = runner.run(tests)
        finally:
            if saved is None:
                os.environ.pop("GIT_DIR", None)
            else:
                os.environ["GIT_DIR"] = saved
        by = {r.test.path: r for r in results}
        check("arm: a passing self-test is GREEN", (by["scripts/test_green.py"].state, by["scripts/test_green.py"].rc),
              (GREEN, 0))
        coord = next((ln[6:] for ln in by["scripts/test_green.py"].output.splitlines() if ln.startswith("coord=")), "")
        check("arm: each self-test gets a private COBOL_COORD_DIR",
              bool(coord) and Path(coord).name.startswith("coord-") and coord != os.environ.get("COBOL_COORD_DIR"),
              True)
        check("arm: each self-test commits under the private global config's identity, never the caller's",
              f"user={GIT_IDENTITY[0]}" in by["scripts/test_green.py"].output.splitlines(), True)
        check("arm: a failing self-test is RED", (by["scripts/test_red.py"].state, by["scripts/test_red.py"].rc), (RED, 1))
        check("arm: a failing self-test's output reaches the console", any("planted failure" in s for s in said), True)
        check("arm: a self-test for another platform is NOT RUN, not run", by["scripts/elsewhere.py"].state, NOT_RUN)
        rc, line = verdict(results, this_platform(), 1.0)
        check("arm: one red makes the verdict RED", (rc, line.startswith(VERDICT + "RED")), (1, True))
        check("arm: the verdict names the red and the not-run one with its reason",
              "scripts/test_red.py" in line and f"{other}-only: planted" in line, True)
        rc, line = verdict([r for r in results if r.test.path != "scripts/test_red.py"], this_platform(), 1.0)
        check("arm: no red is GREEN, still naming what did not run here",
              (rc, line.startswith(VERDICT + "GREEN"), "1 not run on" in line), (0, True, True))

    fleet_arms(check)
    print(f"self_tests.py --self-test: {'ALL GREEN' if not fails else f'{len(fails)} FAILED'}")
    return 1 if fails else 0


#: How long each planted fleet self-test holds its place, and the budget the simulated gates share.
_FLEET_SLEEP_S, _FLEET_CAP, _FLEET_GATES = 0.5, 2, 4


def _max_overlap(spans: list[tuple[float, float]]) -> int:
    """The most spans open at one instant (an end at the same instant as a start closes first)."""
    events = sorted([(a, 1) for a, _ in spans] + [(b, -1) for _, b in spans], key=lambda e: (e[0], e[1]))
    most = now = 0
    for _, step in events:
        now += step
        most = max(most, now)
    return most


def fleet_arms(check: Callable[[str, object, object], None]) -> None:
    """kb/Work PB2914: the self-test phase from N CONCURRENT simulated gates over one planted repository — every gate
    its own Runner, Budget and PassStore objects (separate processes in life) over the SAME shared directories. One
    lander run records; N implementer gates on the unchanged tree re-run nothing; a changed input re-runs exactly the
    self-tests that read it, never more than the budget at once; a hang is HANG, never RED."""
    with tempfile.TemporaryDirectory() as d, tempfile.TemporaryDirectory() as s:
        root, spans_dir = Path(d), Path(s)
        (root / "scripts").mkdir()
        (root / "data").mkdir()
        (root / "data" / "a.txt").write_text("a\n", encoding="utf-8")
        (root / "data" / "c.txt").write_text("c\n", encoding="utf-8")
        (root / "scripts" / "helper_b.py").write_text("VALUE = 1\n", encoding="utf-8")
        span = ("import os, sys, time\nt0 = time.time(); time.sleep({sleep}); t1 = time.time()\n"
                f"open(os.path.join({str(spans_dir)!r}, '%s-%d-%s' % (os.path.basename(sys.argv[0]), os.getpid(),"
                " os.urandom(4).hex())), 'w').write('%f %f' % (t0, t1))\n")
        bodies = {
            "test_a.py": "open('data/a.txt').read()\n",                   # a plain read
            "test_b.py": "import helper_b\n",                              # an import
            "test_c.py": "open('data/' + 'c' + '.txt').read()\n",          # a name built at run time: only the trace sees it
        }
        for name, body in bodies.items():
            (root / "scripts" / name).write_text(body + span.format(sleep=_FLEET_SLEEP_S), encoding="utf-8")
        (root / "scripts" / "test_hang.py").write_text("import time\ntime.sleep(60)\n", encoding="utf-8")
        env = _scrubbed_env(os.environ)
        for cmd in (["init", "-q"], ["add", "-A"]):
            subprocess.run(["git", *cmd], cwd=root, check=True, capture_output=True, env=env)
        found = {t.path.rsplit("/", 1)[-1]: t for t in discover(root)}
        tests = [found[n] for n in sorted(bodies)]
        plat = this_platform()
        budget_dir, store_dir = root / ".git" / BUDGET_DIR, root / ".git" / PASS_STORE_DIR

        def gate(reuse: bool, said: list[str], hang_after_s: float = HANG_AFTER_S, which=None) -> list[Result]:
            return Runner(root, plat, said.append, reuse=reuse, budget=Budget(budget_dir, _FLEET_CAP),
                          store=PassStore(store_dir), hang_after_s=hang_after_s, by="fleet arm").run(which or tests)

        def fleet(reuse: bool, which=None) -> list[list[Result]]:
            barrier = threading.Barrier(_FLEET_GATES)

            def one(_: int) -> list[Result]:
                barrier.wait()
                return gate(reuse, [], which=which)
            with concurrent.futures.ThreadPoolExecutor(max_workers=_FLEET_GATES) as pool:
                return list(pool.map(one, range(_FLEET_GATES)))

        def new_runs(name: str, before: set[Path]) -> list[tuple[float, float]]:
            return [tuple(map(float, p.read_text(encoding="utf-8").split()))
                    for p in set(spans_dir.glob(f"{name}-*")) - before]

        lander = gate(False, [])
        check("arm: the lander's run runs every self-test and records each PASS",
              ([r.state for r in lander], len(list(store_dir.rglob("*.json")))), ([GREEN] * 3, 3))
        rec = PassStore(store_dir).find("scripts/test_c.py", plat, inputs.Tree.load(root))
        check("arm: the recorded PASS holds a file read through a name built at run time (the trace saw it)",
              rec is not None and "data/c.txt" in rec["deps"]["files"], True)
        bad = PassStore(store_dir)._dir("scripts/test_a.py", plat) / "malformed.json"
        bad.write_text(json.dumps({"schema": PASS_SCHEMA, "test": "scripts/test_a.py", "python": _python(),
                                   "deps": {"files": 5}}), encoding="utf-8")
        try:
            found_a = PassStore(store_dir).find("scripts/test_a.py", plat, inputs.Tree.load(root)) is not None
        except Exception as e:  # noqa: BLE001 — the arm reports a raise as its failure
            found_a = f"raised {type(e).__name__}: {e}"
        check("arm: a malformed record in the store is skipped, never a crash of the gate, and the good PASS still found",
              found_a, True)
        bad.unlink()
        before = set(spans_dir.iterdir())
        unchanged = fleet(True)
        check(f"arm: {_FLEET_GATES} concurrent implementer gates on an unchanged tree re-run nothing (every one REUSED)",
              ({r.state for g in unchanged for r in g}, set(spans_dir.iterdir()) - before), ({REUSED}, set()))
        (root / "data" / "c.txt").write_text("c changed\n", encoding="utf-8")
        before = set(spans_dir.iterdir())
        changed = fleet(True)
        by_test = {t.path: {r.state for g in changed for r in g if r.test.path == t.path} for t in tests}
        greens = sum(r.state == GREEN for g in changed for r in g if r.test.path == "scripts/test_c.py")
        # A gate whose lookup comes after another gate RECORDED the new PASS reuses it — right, and timing-dependent,
        # so the arm requires the reader re-run at least once and nothing else re-run, not a count of gates.
        check("arm: a changed input re-runs exactly the self-test that read it (each run a real one); the others are "
              "REUSED in every gate",
              (by_test["scripts/test_a.py"], by_test["scripts/test_b.py"], GREEN in by_test["scripts/test_c.py"],
               by_test["scripts/test_c.py"] <= {GREEN, REUSED}, len(new_runs("test_c.py", before)) == greens),
              ({REUSED}, {REUSED}, True, True, True))
        before = set(spans_dir.iterdir())
        crowd = fleet(False, which=[found["test_c.py"]])  # four gates that each run it: lander-like, no reuse
        check(f"arm: {_FLEET_GATES} gates running the same self-test at once run at most the budget's {_FLEET_CAP} "
              f"at a time, and none hangs",
              (len(new_runs("test_c.py", before)), _max_overlap(new_runs("test_c.py", before)) <= _FLEET_CAP,
               {r.state for g in crowd for r in g}), (_FLEET_GATES, True, {GREEN}))
        (root / "scripts" / "helper_b.py").write_text("VALUE = 2\n", encoding="utf-8")
        again = gate(True, [])
        check("arm: an edited import re-runs the self-test that imports it, and only that one",
              {r.test.path: r.state for r in again},
              {"scripts/test_a.py": REUSED, "scripts/test_b.py": GREEN, "scripts/test_c.py": REUSED})
        said: list[str] = []
        hung = gate(False, said, hang_after_s=1.0, which=[found["test_hang.py"]])
        rc, line = verdict(hung, plat, 1.0)
        check("arm: a self-test still running past the hang detector is HANG — its own state, verdict and exit code, "
              "never RED", (hung[0].state, rc, line.startswith(VERDICT + "HANG")), (HANG, HANG_EXIT, True))
        check("arm: a hung self-test's output names the hang",
              any("HANG: still running" in s for s in said), True)
        empty = Path(tempfile.mkdtemp(dir=s))
        note = Runner(root, plat, lambda _: None, budget=Budget(budget_dir, 1),
                      store=PassStore(store_dir))._record(found["test_a.py"], empty, 1.0)
        check("arm: a Python self-test whose trace does not show its own file records no PASS (the tracer did not run)",
              note.startswith("no PASS recorded"), True)


if __name__ == "__main__":
    sys.exit(main())
