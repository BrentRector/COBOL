#!/usr/bin/env python3
"""run_gate_legs.py — THE GATE DRIVER: the ordered gate (kb/Work PB1708, PB1721, PB3022;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.1, §3.14.3–3.14.4, §3.14.6, §3.17.3).

    python scripts/run_gate_legs.py [--base <sha>]   # the plan, leg 1, fail-fast, a gate slot
    python scripts/run_gate_legs.py --self-test

Called by `scripts/build-local.ps1` and `scripts/build-local.sh`, which only set the process priority. Ordering changes
WHEN a case runs, never WHETHER (owner, 2026-09-28: "order, don't skip").

⛔ ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; §3.17.3): there is no mode. Every gate — an
implementer's, a re-gate after a fix, a landing's — makes the order plan (scripts/gate_plan.py), holds a GATE SLOT
(scripts/gate_slot.py) from before the build to the verdict, so at most N gates build or test at once,
repository-wide, runs its audits BEFORE it queues for the slot (kb/Work PB2524), and runs leg 1 FAIL-FAST. Leg 1 is
the change's own tests and goldens and the previous gate's reds (tier 0a), the host-only witnesses (tier 0h: the
Windows file-system race witnesses CI's NTFS runners may not reproduce), the cheap tree-wide sweeps (tier 0c) and the
cheapest cases the change reaches; so a re-gate after a fix runs leg 1 again — seconds for a bookkeeping fix, never a
population run. The local whole-population run is GONE (it was the lander's: 7-53 min, 8 of 15 RED under load on
2026-10-10); CI's merge-group run is the whole-population regression net.
Its SCOPE is a shared gate setting (gate_slot.py, read once at the start): `leg1` (the coded default, batched gating,
kb/Work PB2515) runs leg 1 only and names every leg-2 case NOT RUN, and its verdict line says
`LEG 1 ONLY (batched gating, PB2515)` so it is never read as a whole-population GREEN; `whole`
(`gate_slot.py set-implementer-scope whole`, the owner's on-demand switch and the battery's form) runs both legs,
stopping RED/INCOMPLETE at a red in leg 1 with the remainder named.

ONE GATE, in this order (§3.14.3):
  1. the worktree's GATE LOCK (`<worktree git dir>/cobol-gate.lock`): a second gate in the same worktree is REFUSED at
     once, naming the holder — its build would overwrite the binaries between this gate's legs;
  2. a fresh RUN DIRECTORY `TestResults/build-local/<UTC stamp>-<nonce>/` holding the listings, the plan, every trx,
     log and identity record, and the verdict file — nothing at a fixed name, so no earlier gate's file is read as
     this one's; the newest earlier run holding a verdict gives the plan its timings and reds; five are kept;
  3. the audits (the one list below, run CONCURRENTLY; one of them is every script self-test, found by
     scripts/self_tests.py — kb/Work PB2563), FAIL-FAST: a red audit ends the gate RED right there, with no leg run
     (kb/Work PB2523) — then the GnuCOBOL corpus, by the ONE rule scripts/external_corpus.py (kb/Work PB2611). Both
     run BEFORE the slot (kb/Work PB2524): they read only the tree, and inside the slot they were 35-45 % of its hold
     while every other gate queued;
  4. the gate slot, always after the lock, so no two gates can wait on each other in a cycle; then
     the solution build and the SHA-256 of every binary the legs will run;
  5. `--list-tests` per assembly (scrubbed), then BESIDE the legs the self-tests that read the built assemblies
     (`self_tests.py --built`, joined after the legs; a red one makes the gate RED), the plan, then per leg the three
     assemblies CONCURRENTLY, each through the
     leg handshake (all three COBOLNET_GATE_* variables, or none) and printed through scripts/test_leg_report.py;
     an assembly the plan gives no case in a leg is not invoked for it;
  6. the POPULATION CHECK (scripts/test_population.py) per assembly over its legs' trx files, the publish of every
     measured duration to the SHARED TIMINGS STORE that times the next gate's plan, even in a fresh worktree
     (`<git common dir>/cobol-gate-timings/`, gate_plan.py, kb/Work PB2527), the IDENTITY check (each
     leg host's record names this plan's digest and the binaries step 3 hashed), then the verdict line:
     `=== BUILD-LOCAL GATE: GREEN — Conformance 9,317/9,317 · Unit … cases ran (skipped 0) in 2 legs · … ===`.
It is GREEN only when every leg ran, every leg is green, every population is exact and every identity matches. Under
the `leg1` scope the verdict is `LEG 1 ONLY (batched gating, PB2515): GREEN` or `…: RED` instead, whenever the
plan had a leg 2 it did not run (a gate with no plan runs its one leg — the whole population — and says plain GREEN).

Exit codes: 0 GREEN (or LEG 1 ONLY: GREEN) · 1 RED (or RED/INCOMPLETE, LEG 1 ONLY: RED, or the build failed) · 2 NOT
RUN (the lock is held, the shared gate settings are malformed, the population could not be listed).
"""
from __future__ import annotations

import argparse
import concurrent.futures
import datetime as dt
import hashlib
import json
import os
import re
import secrets
import shutil
import signal
import subprocess
import sys
import tempfile
import threading
import time
import traceback
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
import external_corpus  # noqa: E402
import gate_plan  # noqa: E402
import impacted_tests  # noqa: E402
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT  # noqa: E402
from gate_slot import _pid_alive  # noqa: E402  — the first-red arm checks that a stopped audit's tree is gone
from gate_slot import (DEFAULT_SCOPE, ExclusiveLock, GateSettings, GateSlots, Setting, Slot,  # noqa: E402
                       drop_git_local_env, git_common_dir, slot_dir)
from test_population import (GATED_ASSEMBLIES, HANDSHAKE, Population, PopulationError, check as check_population,  # noqa: E402
                             list_population, scrubbed_env, synthetic_listing, synthetic_trx)

VERDICT = "=== BUILD-LOCAL GATE: "
#: The verdict prefix of a gate that ran leg 1 only under the `leg1` scope (kb/Work PB2515): never a whole-population
#: GREEN, and never mistaken for one — `GATE: GREEN` does not match it.
LEG1_ONLY = "LEG 1 ONLY (batched gating, PB2515)"
#: Who runs the leg 2 a `leg1` gate leaves: the whole-population regression net (kb/Work PB3015, PB3022, §3.17.2).
LEG2_OWNER = "CI's whole-population run"
RUN_ROOT = Path("TestResults") / "build-local"
RUN_DIR = re.compile(r"\d{8}T\d{6}Z-[0-9a-f]{6}")
KEEP_RUNS = 5
LOCK_NAME = "cobol-gate.lock"
VERDICT_FILE = "verdict.json"
#: A stopped gate's remainder is named in full in the run directory; the console shows this many per assembly.
NOT_RUN_SHOWN = 10

#: ⛔ THE AUDITS, in order — THE list for the gate (build-local.ps1 and .sh used to carry a copy each); battery.sh PHASE
#: -1 and CI's `audits` job run the same scripts. A wrong § is the one defect no test can catch, and each costs about a
#: second. None needs a build or a test result, so they run FIRST: a gate stops on a red one before it builds
#: (kb/Work PB2523 — four of wave 1034's eight first-run reds were audit-only, each found after a whole ~6-min
#: population). And they run OUTSIDE the gate slot (kb/Work PB2524): the slot rations builds and test legs, not
#: reading the tree.
#: They run CONCURRENTLY, each its own process with its output captured and printed whole (kb/Work PB2563), so the
#: phase costs its slowest audit, not their sum.
#: ⛔ AND THE FIRST RED STOPS THE REST (kb/Work PB3061): the moment one audit exits non-zero, every audit still running
#: is stopped — its whole process tree, so the self-test runner's children go with it — and reported STOPPED, never
#: RED. Wave 1062's agents A and B each had CITATIONS RED at 23 s and their verdict 5–7 min later, after the
#: self-tests. A stopped self-test run loses nothing: every self-test that finished has recorded its PASS, which the
#: re-gate reuses; the rest run then.
#: ⛔ SELF-TESTS is EVERY script self-test, found by discovery (scripts/self_tests.py; kb/Work PB2563) — never a self-test
#: named here by hand: this tuple once named five while eleven orchestrator self-tests ran in no gate at all. The few
#: that read the BUILT assemblies run after the build (`self_tests.py --built`, `Host.built_self_tests`).
AUDITS = (
    ("CITATIONS", ["scripts/spec/audit_code_citations.py", "--check"]),
    ("DOC CITATIONS", ["scripts/spec/audit_doc_citations.py", "--check"]),
    ("EVIDENCE SUPERSESSION", ["scripts/spec/audit_evidence_supersession.py", "--check"]),
    ("RENDERED VIEWS", ["scripts/rendered_views.py", "--check"]),
    ("WITNESS LOSS", ["scripts/spec/audit_witness_loss.py", "--check"]),
    ("RULE CATALOG", ["scripts/spec/extract_rule_catalog.py", "--check"]),
    ("SPEC CORRECTIONS", ["scripts/spec/verify_publishable.py"]),
    ("LINE ENDINGS", ["scripts/line_endings.py", "--check"]),
    ("SELF-TESTS", ["scripts/self_tests.py"]),
    # kb/Work PB1957 row 44, the CI invariant: CI's `audits` job ran these two and the gate did not, so train 1067's
    # PR 9 went RED in CI on a `blocked: true` the work register refuses, behind a GREEN gate.
    ("WORK REGISTER", ["scripts/spec/work.py", "check"]),
    ("FLEET PRACTICES", [".claude/skills/workstream/check_practices.py"]),
    # DESIGN-conformance-suite.md §9 (kb/Work PB3174): the conformance suite's generator refuses a malformed test
    # (its header, its rules, its control, its expected outputs, the portability lint) reading only the tree, so a
    # refused test is red here in a fraction of a second, before the slot and the build.
    ("CONFORMANCE SUITE", ["conformance-suite/harness/generate.py", "--check"]),
)
#: ⛔ WHAT AN AUDIT NEEDS IN THE TREE, made ready before any audit runs, exactly as CI's `audits` job makes it ready:
#: FLEET PRACTICES reads the pinned public skills submodule, which a fresh worktree does not have. A prerequisite that
#: fails is a RED and no audit runs. `SelfTestDiscoveryDriftTests` maps every command of CI's `audits` job to an AUDITS
#: entry, to one of these, or to its recorded reason for running elsewhere, so the next audit CI gains reaches the gate.
AUDIT_PREREQUISITES = (
    ("PUBLIC SKILLS SUBMODULE", ["git", "submodule", "update", "--init", "tools/claude-skills"]),
)
#: ⛔ EVERY GATE REUSES A SELF-TEST'S RECORDED PASS (kb/Work PB2914, generalized to the one gate by PB3022;
#: DESIGN-test-build-ci.md §3.14.10, §3.17.3): the gate's audits add this to SELF-TESTS, so a self-test whose inputs
#: are unchanged since a recorded PASS is REUSED, not run, and one that does run records its PASS for the next gate.
#: CI's `audits` job, the Linux gate and a hand run never pass it: they run every self-test.
SELF_TESTS_REUSE = "--reuse"
#: The post-build half of the self-tests: the ones declaring `SELF-TEST-NEEDS: build` (scripts/self_tests.py).
BUILT_SELF_TESTS = ["scripts/self_tests.py", "--built"]
#: ⛔ THE WORK THAT RUNS BESIDE THE GATE'S CRITICAL PATH (`Gate._beside`, kb/Work PB2880), each joined once before the
#: verdict: the post-build self-tests, which read the built assemblies, beside the legs. A step that needs nothing the
#: critical path is still making belongs here, not in a serial valley ahead of it. (The audits stay AHEAD of the slot
#: and the build on purpose: they fail fast, kb/Work PB2523.)
BESIDE = ("post_build_self_tests",)

Spawn = Callable[..., dict]


def _no_slot(**kwargs) -> dict:
    return kwargs


def _stop_tree(p: subprocess.Popen) -> None:
    """Stop `p` and every process it started (kb/Work PB3061): on Windows `taskkill /T /F` walks the tree by parent
    id; on POSIX the process group `start_new_session` gave it. A process already gone is no error."""
    if p.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(p.pid), "/T", "/F"], capture_output=True)
    else:
        try:
            os.killpg(p.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass


class _Live:
    """The audit processes of one `Host.audits()` call (kb/Work PB3061), so the first RED can stop the rest. `add` under
    the lock stops a process that starts after `stop_all` ran, so no audit slips through the gap between its spawn and
    its registration; `stopped` holds the pids stopped, so their exits are reported STOPPED, never RED."""

    def __init__(self) -> None:
        self.lock = threading.Lock()
        self.stopping = threading.Event()
        self.procs: set[subprocess.Popen] = set()
        self.stopped: set[int] = set()

    def add(self, p: subprocess.Popen) -> None:
        with self.lock:
            self.procs.add(p)
            if self.stopping.is_set():
                self.stopped.add(p.pid)
                _stop_tree(p)

    def discard(self, p: subprocess.Popen) -> None:
        with self.lock:
            self.procs.discard(p)

    def stop_all(self) -> int:
        """Stop every registered process still running, and every one registered from now on; returns how many."""
        with self.lock:
            self.stopping.set()
            running = [p for p in self.procs if p.poll() is None]
            for p in running:
                self.stopped.add(p.pid)
                _stop_tree(p)
            return len(running)


# ── the host: everything the driver does to the machine (the self-test plants a fake one) ────────────────────────


class Host:
    """The real machine: git, the build, the listings, the test hosts."""

    def __init__(self, repo: Path = REPO):
        self.repo = repo

    def git(self, *args: str) -> str:
        return subprocess.run(["git", *args], cwd=self.repo, check=True, capture_output=True, text=True,
                              encoding="utf-8").stdout.strip()

    def lock_path(self) -> Path:
        """`<worktree git dir>/cobol-gate.lock` — per WORKTREE (a linked worktree's git dir is its own)."""
        return Path(self.git("rev-parse", "--absolute-git-dir")) / LOCK_NAME

    def take_slot(self, label: str, say: Callable[[str], None]) -> Slot:
        return GateSlots.for_repo(self.repo).acquire(label, say=say)

    def implementer_scope(self) -> tuple[str, Setting | None]:
        """The shared implementer scope (gate_slot.py; kb/Work PB2515): `leg1` or `whole`, and the live setting that
        chose it (None: no live setting, so the coded default gate_slot.DEFAULT_SCOPE). Malformed settings raise
        ValueError: the gate is NOT RUN, with the reason."""
        return GateSettings.read(slot_dir(self.repo)).effective_scope()

    def default_base(self) -> str:
        return self.git("merge-base", "HEAD", "origin/main")

    def _captured(self, cmd: list[str], spawn: Spawn, live: "_Live | None" = None) -> tuple[int | None, str, float]:
        """Run `cmd` under this interpreter to its end: (exit code, its output, seconds). With `live` — one `audits()`
        call's registry (kb/Work PB3061) — the process is registered while it runs so the first RED can stop it; an
        exit code of None means STOPPED: it was stopped, or never started because the stop came first."""
        t = time.monotonic()
        if live is not None and live.stopping.is_set():
            return None, "", 0.0
        p = subprocess.Popen([sys.executable, *cmd], cwd=self.repo, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                             text=True, encoding="utf-8", errors="replace", start_new_session=os.name != "nt",
                             **spawn())
        if live is not None:
            live.add(p)
        try:
            out = p.communicate()[0]
        finally:
            if live is not None:
                live.discard(p)
        if live is not None and p.pid in live.stopped:
            return None, out or "", time.monotonic() - t
        return p.returncode, out or "", time.monotonic() - t

    def audit_commands(self) -> tuple[tuple[str, list[str]], ...]:
        """The audits this gate runs: AUDITS (the first-red arm of the self-test plants its own)."""
        return AUDITS

    def audit_prerequisites(self) -> tuple[tuple[str, list[str]], ...]:
        """What the audits need in the tree: AUDIT_PREREQUISITES (the self-test's arms plant their own)."""
        return AUDIT_PREREQUISITES

    def _prepared(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """Run every prerequisite (`audit_prerequisites`) in order; the first that fails is its RED reason."""
        for name, cmd in self.audit_prerequisites():
            p = subprocess.run(cmd, cwd=self.repo, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                               encoding="utf-8", errors="replace", **spawn())
            if p.returncode != 0:
                say(f"--- audit prerequisite {name} ({' '.join(cmd)}) ---")
                for line in (p.stdout or "").rstrip().splitlines():
                    say(line)
                say(f"=== {name}: RED (exit {p.returncode}; no audit ran) ===")
                return [f"{name} RED"]
        return []

    def audits(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """The audits (`audit_commands`), every one of them, CONCURRENTLY, once their prerequisites
        (`audit_prerequisites`, run first and in order) are ready; the self-tests reuse recorded PASSes
        (SELF_TESTS_REUSE). Each audit's whole output is printed as one block when it finishes, so no two interleave.
        ⛔ THE FIRST RED STOPS THE REST (kb/Work PB3061): the audits still running are stopped, process trees and all
        (`_Live.stop_all`), and reported STOPPED — never RED — so a red found in 23 s is the verdict in 23 s and not
        after the slowest audit (the self-tests: 5–9 min in every first gate of wave 1062). Returns the RED reasons in
        AUDITS order; a hung self-test is `SELF-TESTS HANG`, never RED (kb/Work PB2914)."""
        if not_ready := self._prepared(spawn, say):
            return not_ready
        reds: dict[str, str] = {}
        live = _Live()
        commands = [(name, [*cmd, SELF_TESTS_REUSE] if name == "SELF-TESTS" else cmd)
                    for name, cmd in self.audit_commands()]
        with concurrent.futures.ThreadPoolExecutor(max_workers=len(commands)) as pool:
            futures = {pool.submit(self._captured, cmd, spawn, live): name for name, cmd in commands}
            for f in concurrent.futures.as_completed(futures):
                name = futures[f]
                rc, text, secs = f.result()
                if rc is None:
                    say(f"--- audit {name}: STOPPED after {secs:.1f}s — another audit was already RED (kb/Work PB3061); "
                        f"the re-gate runs it ---")
                    continue
                say(f"--- audit {name} ({secs:.1f}s) ---")
                for line in text.rstrip().splitlines():
                    say(line)
                if rc != 0:
                    state = "HANG" if name == "SELF-TESTS" and rc == SELF_TESTS_HANG_EXIT else "RED"
                    say(f"=== {name}: {state} (see above) ===")
                    reds[name] = f"{name} {state}"
                    stopped = live.stop_all()
                    if stopped:
                        say(f"=== {name} is {state}: {stopped} audit(s) still running stopped, process trees and all "
                            f"(kb/Work PB3061) ===")
        return [reds[name] for name, _ in commands if name in reds]

    def built_self_tests(self, spawn: Spawn) -> tuple[list[str], list[str]]:
        """The self-tests that read the built assemblies (BUILT_SELF_TESTS), after the build. Returns (the RED reasons,
        the output lines): the gate prints them when it joins this, so they never interleave with a leg's report."""
        rc, text, secs = self._captured(BUILT_SELF_TESTS, spawn)
        lines = [f"--- post-build self-tests ({secs:.1f}s) ---", *text.rstrip().splitlines()]
        return ([] if rc == 0 else ["POST-BUILD SELF-TESTS RED"]), lines

    def fetch_corpus(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """The GnuCOBOL corpus when the worktree lacks it — THE one rule, scripts/external_corpus.py (kb/Work
        PB2611), which battery.sh and every other caller apply too. Returns the RED reason, if any."""
        return external_corpus.ensure(self.repo, say,
                                      lambda cmd, cwd: subprocess.run(cmd, cwd=cwd, **spawn()).returncode)

    def build(self, spawn: Spawn) -> bool:
        return subprocess.run(["dotnet", "build", "Cobol.Net.sln", "-v", "quiet"], cwd=self.repo,
                              **spawn()).returncode == 0

    def binaries(self) -> dict[str, dict[str, str]]:
        """Per assembly, the SHA-256 of its test assembly and of every product assembly beside it — the set a leg
        host's identity record names (tests/_shared/GateLegs.cs `GateIdentityRecord.ProductAssemblies`)."""
        out = {}
        for asm, project in GATED_ASSEMBLIES.items():
            tests = sorted((self.repo / project / "bin" / "Debug").glob(f"*/Cobol.Net.Tests.{asm}.dll"))
            if len(tests) != 1:
                raise PopulationError(f"{project}: expected one built Cobol.Net.Tests.{asm}.dll under bin/Debug, "
                                      f"found {len(tests)}")
            test_dll = tests[0]
            hashes = {"<test>": _sha256(test_dll)}
            for dll in sorted(test_dll.parent.glob("*.dll")):
                n = dll.name
                if dll != test_dll and (n.startswith("Cobol.Net.") or n == "cobol.dll"):
                    hashes[n] = _sha256(dll)
            out[asm] = hashes
        return out

    def listing(self, asm: str, spawn: Spawn) -> tuple[Counter, str]:
        return list_population(self.repo / GATED_ASSEMBLIES[asm], cwd=self.repo, **spawn())

    def plan(self, listings: dict[str, Counter], base: str, previous: Path | None, out: Path) -> tuple[dict, str]:
        """The order plan (DESIGN §3.14.2), written to `out`; returns (plan, the SHA-256 of the file's bytes)."""
        impacted_tests.HEAD = None  # the worktree, uncommitted edits included, against its cut point
        analysis = impacted_tests.analyse(base)
        own, reds = (gate_plan.read_previous_run(previous) if previous is not None
                     else (gate_plan.Timings("none"), set()))
        # TIMINGS from the shared store every gate publishes to (kb/Work PB2527), so a FRESH worktree runs leg 2
        # longest first too; this worktree's own previous run only before any gate on this machine has published.
        # REDS only ever from this worktree's own previous run.
        shared = gate_plan.read_shared_timings(gate_plan.timings_store(git_common_dir(self.repo)))
        timings = shared if shared else own
        # TRAITS from the test sources (kb/Work PB3022): the host-only witnesses (tier 0h) and the cheap sweeps (0c).
        plan = gate_plan.build_plan({a: list(c.elements()) for a, c in listings.items()}, analysis, timings, reds,
                                    base, gate_plan.gated_traits(self.repo))
        return plan, gate_plan.write_plan(plan, out)

    def publish_timings(self, run: Path, listings: dict[str, Counter]) -> tuple[int, str]:
        """Merge this run's measured durations into the shared timings store (kb/Work PB2527)."""
        return gate_plan.publish_timings(
            gate_plan.timings_store(git_common_dir(self.repo)), run, {d for c in listings.values() for d in c},
            by=f"{self.repo} run {run.name}", now=dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"))

    def run_leg(self, asm: str, leg: int, run: Path, env: dict[str, str], spawn: Spawn) -> int:
        """One assembly's leg: `dotnet test --no-build` with a trx logger, its whole output to `<leg file>.log`."""
        stem = leg_stem(leg, asm)
        cmd = ["dotnet", "test", str(self.repo / GATED_ASSEMBLIES[asm]), "--no-build", "--results-directory",
               str(run), "--logger", f"trx;LogFileName={stem}.trx"]
        with open(run / f"{stem}.log", "wb") as fh:
            return subprocess.run(cmd, cwd=self.repo, stdout=fh, stderr=subprocess.STDOUT, **spawn(env=env)).returncode


def _sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def leg_stem(leg: int, asm: str) -> str:
    """The file stem of one assembly's leg — the name the leg host's identity record also takes (leg-<n>-<key>)."""
    return f"leg-{leg}-{asm}"


def leg_env(handshake: dict[str, str]) -> dict[str, str]:
    """⛔ THE ONLY PLACE A TEST HOST'S ENVIRONMENT IS MADE (§3.14.3): the scrubbed environment — no stray
    COBOLNET_GATE_*, VSTest* or RunSettingsFilePath — in English, with this gate's handshake merged over it: all three
    variables, or none."""
    if handshake and set(handshake) != set(HANDSHAKE):
        raise ValueError(f"a partial handshake {sorted(handshake)}: the driver hands all three variables or none")
    return {**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en", **handshake}


# ── the gate ─────────────────────────────────────────────────────────────────────────────────────────────────────


@dataclass
class LegRun:
    asm: str
    leg: int
    rc: int
    red: bool
    wall_s: float
    finished_s: float  # since the legs started


@dataclass
class Outcome:
    """What one gate did — the verdict file's content, and what the self-test asserts on."""
    verdict: str = "NOT RUN"
    line: str = ""
    exit_code: int = 2
    reasons: list[str] = field(default_factory=list)
    slot: str | None = None
    scope: str = DEFAULT_SCOPE
    scope_until: str | None = None
    leg1_only: bool = False  # the `leg1` scope cut a planned leg 2 (never the fail-fast stop, which is `stopped`)
    plan_sha256: str | None = None
    plan_note: str = ""
    legs_invoked: dict[int, list[str]] = field(default_factory=dict)
    handshakes: dict[tuple[int, str], dict[str, str]] = field(default_factory=dict)
    runs: list[LegRun] = field(default_factory=list)
    populations: dict[str, Population] = field(default_factory=dict)
    not_run: dict[str, int] = field(default_factory=dict)
    identity: list[str] = field(default_factory=list)
    stopped: bool = False
    timings: dict[str, float] = field(default_factory=dict)
    timings_store: str = ""  # what the publish to the shared timings store did (kb/Work PB2527)
    #: Work running BESIDE the gate's critical path (`Gate._beside`), joined once before the verdict: (name, future).
    beside: list[tuple[str, concurrent.futures.Future]] = field(default_factory=list)


class Gate:
    def __init__(self, host: Host, run_root: Path, base: str | None = None,
                 say: Callable[[str], None] = lambda s: print(s, flush=True)):
        self.host, self.run_root, self.base, self.say = host, run_root, base, say
        self._pool: concurrent.futures.ThreadPoolExecutor | None = None  # `run` owns it for the work `_beside` starts

    # The verdict line and exit code are decided here, once, for every path through the gate.
    def _finish(self, out: Outcome, run: Path | None, verdict: str, detail: str, code: int) -> Outcome:
        self._join_beside(out)  # an early end (a failed build, an unlistable population) still reports work beside it
        out.verdict, out.exit_code = verdict, code
        out.line = f"{VERDICT}{verdict} — {detail} ==="
        if run is not None:
            (run / VERDICT_FILE).write_text(json.dumps(self._record(out, run), indent=1, sort_keys=True) + "\n",
                                            encoding="utf-8", newline="\n")
        self.say(out.line)
        return out

    def run(self) -> Outcome:
        out = Outcome()
        started = time.monotonic()
        label = f"gate {self.host.repo} (pid {os.getpid()}, since {time.strftime('%Y-%m-%d %H:%M:%S')})"
        lock_path = self.host.lock_path()
        lock = ExclusiveLock.try_take(lock_path, label)
        if lock is None:
            return self._finish(out, None, "NOT RUN", f"another gate holds this worktree's gate lock ({lock_path}): "
                                f"{ExclusiveLock.holder(lock_path) or '(no label)'}", 2)
        with lock:
            try:
                out.scope, setting = self.host.implementer_scope()
            except ValueError as e:  # malformed shared gate settings stop the gate, with the reason
                return self._finish(out, None, "NOT RUN", str(e), 2)
            if setting is not None and setting.until is not None:
                out.scope_until = setting.until.isoformat()
            # The pool the work BESIDE the critical path runs in (`_beside`); leaving it waits for that work, so no
            # gate returns while a thread it started still runs.
            with concurrent.futures.ThreadPoolExecutor(max_workers=len(BESIDE), thread_name_prefix="beside") as pool:
                self._pool = pool
                try:
                    return self._gate(out, label, started)
                except Exception as e:  # noqa: BLE001 — a defect in the driver still ends in a verdict line, never
                    # silence: every caller BLOCKS on that line (MANDATORY-PRACTICES P2), so a traceback alone would
                    # hang it.
                    self.say(traceback.format_exc().rstrip())
                    return self._finish(out, None, "NOT RUN", f"the gate driver failed: {type(e).__name__}: {e}", 2)

    def _beside(self, out: Outcome, name: str, work: Callable[[], tuple[list[str], list[str]]]) -> None:
        """Start `work` BESIDE the gate's critical path (kb/Work PB2880): a step that needs nothing the critical path
        is still making runs concurrently with it instead of in a serial valley ahead of it. `work` returns (its RED
        reasons, its output lines); the lines are printed whole when it is joined (`_join_beside`, once, before the
        verdict), so they never interleave with a leg's report. `name` is one of BESIDE."""
        if name not in BESIDE:
            raise ValueError(f"beside work {name!r} is not one of BESIDE {BESIDE}")
        if self._pool is None:
            raise RuntimeError("beside work can only start inside Gate.run, which owns its pool")
        out.beside.append((name, self._pool.submit(work)))

    def _join_beside(self, out: Outcome) -> None:
        """Join every piece of work started beside the critical path, in the order it was started: print its lines,
        merge its RED reasons, time the wait as `<name>_wait_s`. A piece that RAISED is a named RED with its
        traceback — never a silent pass, never an escape that leaves the gate without its verdict line."""
        while out.beside:
            name, future = out.beside.pop(0)
            t = time.monotonic()
            try:
                reds, lines = future.result()
            except Exception as e:  # noqa: BLE001 — see the docstring
                reds = [f"{name.upper().replace('_', ' ')} FAILED ({type(e).__name__}: {e})"]
                lines = traceback.format_exception(e)
            out.timings[f"{name}_wait_s"] = round(time.monotonic() - t, 1)
            for line in "".join(f"{l}\n" for l in lines).rstrip().splitlines():
                self.say(line)
            out.reasons += reds

    def _new_run(self) -> Path:
        self.run_root.mkdir(parents=True, exist_ok=True)
        stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        run = self.run_root / f"{stamp}-{secrets.token_hex(3)}"
        run.mkdir()
        return run

    def _runs(self) -> list[Path]:
        return sorted(p for p in self.run_root.iterdir() if p.is_dir() and RUN_DIR.fullmatch(p.name)) \
            if self.run_root.is_dir() else []

    def previous_run(self, current: Path) -> Path | None:
        """The newest EARLIER run that reached a verdict and kept a trx: the plan's timings and reds."""
        for p in reversed(self._runs()):
            if p != current and (p / VERDICT_FILE).exists() and any(p.glob("*.trx")):
                return p
        return None

    def _prune(self, current: Path) -> None:
        for p in self._runs()[:-KEEP_RUNS]:
            if p != current:
                shutil.rmtree(p, ignore_errors=True)

    def _gate(self, out: Outcome, label: str, started: float) -> Outcome:
        run = self._new_run()
        self.say(f"build-local: gate, run directory {run}")
        # ⛔ THE AUDITS AND THE CORPUS FETCH RUN BEFORE THE GATE SLOT IS TAKEN (kb/Work PB2524). They read only the
        # tree — no build output, no test result — so they need nothing the slot rations (concurrent builds and test
        # legs, DESIGN-test-build-ci §3.14.6). Inside the slot they were 35-45 % of its hold (~230 s of serial Python
        # per gate, one core busy) while every other gate queued behind it.
        t = time.monotonic()
        audit_reds = self.host.audits(_no_slot, self.say)  # an unchanged self-test is REUSED (PB2914)
        out.timings["audits_s"] = round(time.monotonic() - t, 1)
        if audit_reds:
            # FAIL FAST ON THE AUDITS (kb/Work PB2523): they need no build and no test result and take seconds, while
            # a red one used to be reported only after the whole ~6-min population — 4 of wave 1034's 8 first-run
            # reds were audit-only.
            out.reasons += audit_reds
            return self._finish(out, run, "RED", f"{'; '.join(audit_reds)} — fail-fast: the audits run before the "
                                f"slot and the build and NO LEG RAN; fix the audit and re-gate (run {run.name})", 1)
        out.reasons += self.host.fetch_corpus(_no_slot, self.say)
        t = time.monotonic()
        try:
            slot = self.host.take_slot(label, self.say)
        except ValueError as e:  # the shared gate settings turned malformed while this gate queued: stop, with the reason
            return self._finish(out, run, "NOT RUN", str(e), 2)
        out.timings["slot_wait_s"] = round(time.monotonic() - t, 1)
        out.slot = slot.describe()  # acquire() has printed `gate-slot: took slot k of N …`; the verdict line names it
        try:
            return self._tested(out, run, slot, started)
        finally:
            slot.release()

    def _tested(self, out: Outcome, run: Path, slot: Slot, started: float) -> Outcome:
        """The build, the listings, the plan, the legs and the checks: the part of the gate the slot rations."""
        spawn: Spawn = slot.spawn_kwargs
        t = time.monotonic()
        if not self.host.build(spawn):
            return self._finish(out, run, "BUILD FAILED", "the solution did not build (see above)", 1)
        out.timings["build_s"] = round(time.monotonic() - t, 1)
        try:
            binaries = self.host.binaries()
            (run / "binaries.json").write_text(json.dumps(binaries, indent=1, sort_keys=True), encoding="utf-8", newline="\n")
            # The three listings run CONCURRENTLY, as the legs do: each is a separate `--list-tests` host (~2 s).
            with concurrent.futures.ThreadPoolExecutor(max_workers=len(GATED_ASSEMBLIES)) as pool:
                listed = dict(zip(GATED_ASSEMBLIES, pool.map(lambda a: self.host.listing(a, spawn), GATED_ASSEMBLIES)))
            listings = {}
            for asm, (names, raw) in listed.items():
                listings[asm] = names
                (run / f"listing-{asm}.txt").write_text(raw, encoding="utf-8", newline="\n")
        except PopulationError as e:
            return self._finish(out, run, "NOT RUN", f"the population could not be listed: {e}", 2)

        # The self-tests that read the BUILT assemblies (kb/Work PB2563) run beside the legs: they only list tests, and
        # the legs are the gate's longest step, so they add no wall time. Joined, and printed, after the legs.
        self._beside(out, "post_build_self_tests", lambda: self.host.built_self_tests(spawn))
        return self._legs_and_checks(out, run, listings, binaries, started, spawn)

    def _legs_and_checks(self, out: Outcome, run: Path, listings: dict[str, Counter], binaries: dict,
                         started: float, spawn: Spawn) -> Outcome:
        plan, digest = self._plan(out, listings, run)
        legs = self._legs(plan)
        to_run = legs
        if out.scope == "leg1" and len(legs) > 1:
            # Batched gating (kb/Work PB2515), the one gate's shape (PB3022): leg 2 is CI's whole-population run.
            # The population check below still measures every planned leg, so each leg-2 case is named NOT RUN.
            to_run = {1: legs[1]}  # two planned legs: leg 1 is one of them
            out.leg1_only = True
            self.say(f"build-local: {LEG1_ONLY} — the scope is leg1"
                     + (f" until {out.scope_until}" if out.scope_until else "")
                     + f"; leg 2 is left to {LEG2_OWNER}")
        legs_started = time.monotonic()
        for leg, assemblies in to_run.items():
            if out.stopped:
                break
            out.legs_invoked[leg] = assemblies
            leg_runs = self._run_leg(out, leg, assemblies, run, plan, digest, spawn, legs_started)
            if any(r.red for r in leg_runs) and leg < max(to_run):
                out.stopped = True  # FAIL FAST: the remainder is named by the population check below
        out.timings["legs_s"] = round(time.monotonic() - legs_started, 1)
        reds = [r for r in out.runs if r.red]
        if reds:
            out.timings["first_red_s"] = round(min(r.finished_s for r in reds), 1)
        self._join_beside(out)

        self._check_populations(out, listings, legs, run)
        self._publish_timings(out, run, listings)
        if plan is not None:
            self._check_identity(out, plan, digest, binaries, listings, run)
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        self._prune(run)
        return self._verdict(out, run, plan, legs)

    def _plan(self, out: Outcome, listings: dict[str, Counter], run: Path) -> tuple[dict | None, str | None]:
        """The order plan. No plan input, a stale one or a broken one gives ONE leg in the plain order (§3.14.1) —
        named, never a skip."""
        try:
            base = self.base or self.host.default_base()
            previous = self.previous_run(run)
            plan, digest = self.host.plan(listings, base, previous, run / "plan.json")
        except Exception as e:  # noqa: BLE001 — ANY planning failure falls back to the whole population in one leg
            out.plan_note = f"no plan ({type(e).__name__}: {str(e).splitlines()[0] if str(e) else ''})"
            self.say(f"build-local: ⚠ the order plan could not be made — {out.plan_note}; ONE leg in the plain order")
            return None, None
        for line in gate_plan.summary_lines(plan, run / "plan.json"):
            self.say(line)
        out.plan_sha256 = plan["sha256"]
        out.plan_note = (f"plan {plan['sha256'][:12]}: map {(plan['map'] or 'none')[:12]} ({plan['map_state']}), "
                         f"timings {plan['timings']}")
        return plan, digest

    @staticmethod
    def _legs(plan: dict | None) -> dict[int, list[str]]:
        """Which assemblies each leg invokes. An assembly the plan gives no case in a leg is not invoked for it: a
        host handed no case prints no verdict line and exits 0, which the leg report would score RED (§3.14.2)."""
        if plan is None:
            return {1: list(GATED_ASSEMBLIES)}
        legs: dict[int, list[str]] = {}
        for leg in (1, 2):
            names = [a for a in GATED_ASSEMBLIES if leg in plan["assemblies"][a]["legs"]]
            if names:
                legs[leg] = names
        return legs

    def _run_leg(self, out: Outcome, leg: int, assemblies: list[str], run: Path, plan: dict | None,
                 digest: str | None, spawn: Spawn, legs_started: float) -> list[LegRun]:
        self.say(f"build-local: leg {leg} — {', '.join(assemblies)} concurrently")
        handshake = {} if plan is None else {"COBOLNET_GATE_PLAN": str((run / "plan.json").resolve()),
                                             "COBOLNET_GATE_LEG": str(leg), "COBOLNET_GATE_PLAN_SHA256": digest}

        def one(asm: str) -> tuple[str, int, float, float]:
            env = leg_env(handshake)
            out.handshakes[(leg, asm)] = {k: v for k, v in env.items() if k.upper().startswith("COBOLNET_GATE_")}
            t = time.monotonic()
            rc = self.host.run_leg(asm, leg, run, env, spawn)
            return asm, rc, time.monotonic() - t, time.monotonic() - legs_started

        with concurrent.futures.ThreadPoolExecutor(max_workers=len(assemblies)) as pool:
            results = {asm: (rc, wall, fin) for asm, rc, wall, fin in pool.map(one, assemblies)}
        leg_runs = []
        for asm in assemblies:  # reported in a fixed order, whatever order they finished in
            rc, wall, fin = results[asm]
            stem = leg_stem(leg, asm)
            # THE ONE RULE for what a leg prints (kb/Work PB1573): a green leg its verdict line, a red one its
            # COMPLETE output. Its exit code is the leg's verdict: 0 green, anything else red.
            rep = subprocess.run([sys.executable, str(REPO / "scripts" / "test_leg_report.py"), "--name",
                                  f"{asm} leg {leg}", "--log", str(run / f"{stem}.log"), "--rc", str(rc)],
                                 capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
            for line in (rep.stdout + rep.stderr).splitlines():
                self.say(line)
            leg_runs.append(LegRun(asm, leg, rc, rep.returncode != 0, round(wall, 1), round(fin, 1)))
        out.runs += leg_runs
        return leg_runs

    def _check_populations(self, out: Outcome, listings: dict[str, Counter], legs: dict[int, list[str]],
                           run: Path) -> None:
        for asm in GATED_ASSEMBLIES:
            invoked = [leg for leg, names in legs.items() if asm in names]
            ran = [leg for leg in invoked if asm in out.legs_invoked.get(leg, [])]
            trx = [str(run / f"{leg_stem(leg, asm)}.trx") for leg in ran if (run / f"{leg_stem(leg, asm)}.trx").exists()]
            stopped = len(ran) < len(invoked)
            try:
                if not trx and not ran:  # every leg of it was cut off by the stop: its whole population is NOT RUN
                    pop = Population(asm, sum(listings[asm].values()), 0, 0, 0, Counter(listings[asm]), Counter(),
                                     Counter(), stopped_early=True)
                elif not trx:
                    raise PopulationError(f"{asm}: no leg of it wrote a trx")
                else:
                    pop = check_population(asm, listings[asm], trx, stopped_early=stopped)
            except PopulationError as e:
                out.reasons.append(f"{asm} POPULATION UNMEASURED ({e})")
                self.say(f"=== POPULATION {asm}: UNMEASURED — {e} ===")
                continue
            out.populations[asm] = pop
            lines = pop.report()
            if stopped:
                named = [l for l in lines[1:] if l.lstrip().startswith("NOT RUN")]
                out.not_run[asm] = sum(pop.never_ran.values())
                (run / f"not-run-{asm}.txt").write_text("\n".join(named) + "\n", encoding="utf-8", newline="\n")
                lines = [lines[0], *[l for l in lines[1:] if l not in named], *named[:NOT_RUN_SHOWN]]
                if len(named) > NOT_RUN_SHOWN:
                    lines.append(f"  … {len(named) - NOT_RUN_SHOWN:,} more NOT RUN, every one named in "
                                 f"{run / f'not-run-{asm}.txt'}")
            for line in lines:
                self.say(line)

    def _publish_timings(self, out: Outcome, run: Path, listings: dict[str, Counter]) -> None:
        """Every gate that ran a leg, a stopped one included, publishes its measured durations to the shared timings
        store (kb/Work PB2527), so the NEXT gate — in a fresh worktree — plans longest first. The store is a
        hint for ordering: a failed write is reported on the console and in the verdict file, never in the verdict."""
        if not any(run.glob("*.trx")):
            return
        try:
            n, note = self.host.publish_timings(run, listings)
        except Exception as e:  # noqa: BLE001 — see the docstring: ordering degrades, the verdict is unaffected
            out.timings_store = f"NOT updated ({type(e).__name__}: {e})"
            self.say(f"build-local: ⚠ the shared timings store was {out.timings_store}; later plans keep the "
                     "durations already there")
            return
        out.timings_store = f"published {n:,} measured case durations" + (f"; {note}" if note else "")
        self.say(f"build-local: {out.timings_store} to the shared timings store")

    def _check_identity(self, out: Outcome, plan: dict, digest: str, binaries: dict[str, dict[str, str]],
                        listings: dict[str, Counter], run: Path) -> None:
        """Each leg host ran THIS plan on THESE binaries (§3.14.4): its identity record names the digest the driver
        handed it, the plan's content digest, and the hashes step 3 recorded; it was handed the whole discovered
        population and ran only its own leg's keys. A leg run under a handshake that wrote no record did not run the
        gate's framework at all."""
        for leg, assemblies in out.legs_invoked.items():
            for asm in assemblies:
                where = f"leg {leg} {asm}"
                path = run / f"{leg_stem(leg, asm)}.json"
                try:
                    rec = json.loads(path.read_text(encoding="utf-8"))
                except (OSError, ValueError) as e:
                    out.identity.append(f"{where}: no readable identity record ({path.name}: {e})")
                    continue
                problems = []
                if str(rec.get("plan_sha256", "")).lower() != digest.lower():
                    problems.append(f"read plan digest {rec.get('plan_sha256')}, the driver handed {digest}")
                if rec.get("plan_content_sha256") != plan["sha256"]:
                    problems.append("its plan's content digest is not this plan's")
                if rec.get("leg") != leg or rec.get("assembly") != asm:
                    problems.append(f"it says leg {rec.get('leg')} {rec.get('assembly')}")
                built = binaries[asm]
                if rec.get("test_assembly", {}).get("sha256") != built["<test>"]:
                    problems.append("its test assembly is not the one this gate built")
                products = {k: v for k, v in built.items() if k != "<test>"}
                if rec.get("product_assemblies") != products:
                    changed = sorted(set(products) ^ set(rec.get("product_assemblies", {})) | {
                        k for k in products if rec.get("product_assemblies", {}).get(k) not in (None, products[k])})
                    problems.append(f"it ran other product binaries than this gate built ({', '.join(changed)})")
                if rec.get("received") != sum(listings[asm].values()):
                    problems.append(f"it was handed {rec.get('received')} cases of {sum(listings[asm].values())} "
                                    "discovered")
                wrong_leg = [k for k in rec.get("runs", []) if gate_plan.leg_of(plan, asm, k) != leg]
                if wrong_leg:
                    problems.append(f"it ran {len(wrong_leg)} case(s) of the other leg (first: {wrong_leg[0]})")
                out.identity += [f"{where}: {p}" for p in problems]
        if out.identity:
            self.say(f"=== IDENTITY: MISMATCH — {len(out.identity)} ===")
            for p in out.identity:
                self.say(f"  IDENTITY MISMATCH  {p}")
        else:
            self.say(f"=== IDENTITY: every leg host ran plan {plan['sha256'][:12]} on the binaries this gate built ===")

    def _verdict(self, out: Outcome, run: Path, plan: dict | None, legs: dict[int, list[str]]) -> Outcome:
        pops = out.populations
        tally = " · ".join(f"{a} {pops[a].ran:,}/{pops[a].discovered:,}" if a in pops else f"{a} UNMEASURED"
                           for a in GATED_ASSEMBLIES)
        skipped = sum(p.skipped for p in pops.values())
        n_legs = len(legs)
        detail = (f"{tally} cases ran (skipped {skipped:,}) in {len(out.legs_invoked)} of {n_legs} leg(s)"
                  + (f" · {out.slot}" if out.slot else "") + f" ({out.plan_note}; run {run.name})")
        reds = [f"{r.asm} leg {r.leg} RED" for r in out.runs if r.red]
        problems = reds + out.reasons + [f"{a} POPULATION {'NOT EXACT' if not pops[a].stopped_early else 'STOPPED'}"
                                         for a in pops if not pops[a].exact and not pops[a].stopped_early]
        if out.identity:
            problems.append(f"{len(out.identity)} IDENTITY MISMATCH(ES)")
        remainder = " · ".join(f"{a} {n:,}" for a, n in out.not_run.items())
        if out.stopped:
            problems.append(f"STOPPED AFTER LEG 1 — leg 2 NOT RUN: {remainder} (named in {run / 'not-run-*.txt'})")
            return self._finish(out, run, "RED/INCOMPLETE", f"{'; '.join(problems)} — {detail}", 1)
        if out.leg1_only:
            left = (f"leg 2 left to {LEG2_OWNER}: {remainder} (named in "
                    f"{run / 'not-run-*.txt'}; scope leg1" + (f" until {out.scope_until}" if out.scope_until else "")
                    + ")")
            if problems:
                return self._finish(out, run, f"{LEG1_ONLY}: RED", f"{'; '.join(problems)}; {left} — {detail}", 1)
            return self._finish(out, run, f"{LEG1_ONLY}: GREEN", f"{left} — {detail}", 0)
        if problems:
            return self._finish(out, run, "RED", f"{'; '.join(problems)} — {detail}", 1)
        return self._finish(out, run, "GREEN", detail, 0)

    def _record(self, out: Outcome, run: Path) -> dict:
        return {"schema": 2, "verdict": out.verdict, "line": out.line, "reasons": out.reasons,
                "slot": out.slot, "plan_sha256": out.plan_sha256, "plan": out.plan_note, "stopped": out.stopped,
                "scope": out.scope, "scope_until": out.scope_until, "leg1_only": out.leg1_only,
                "legs": {str(leg): names for leg, names in out.legs_invoked.items()},
                "runs": [vars(r) for r in out.runs], "timings": out.timings, "not_run": out.not_run,
                "timings_store": out.timings_store,
                "identity": out.identity,
                "populations": {a: {"discovered": p.discovered, "ran": p.ran, "skipped": p.skipped, "exact": p.exact}
                                for a, p in out.populations.items()}}


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────


class FakeHost(Host):
    """A planted machine: a small population per assembly, a planted plan, and leg hosts that write the trx, the log
    and the identity record the real ones write — each arm plants one defect into them."""

    POPULATION = {
        "Conformance": ["N.Conf.A", "N.Conf.B", "N.Conf.C", "N.Conf.D", "N.Conf.E", "N.Conf.F"],
        "Unit": ["N.Unit.A", "N.Unit.B", "N.Unit.C", "N.Unit.D"],
        "Characterization": ["N.Char.A", "N.Char.B"],
    }
    #: Conformance runs in two legs; Unit's leg 1 is EMPTY (it runs in leg 2 only); Characterization in leg 1 only.
    LEGS = {"Conformance": ([1, 2], ["N.Conf.A", "N.Conf.B"], ["N.Conf.C", "N.Conf.D", "N.Conf.E", "N.Conf.F"]),
            "Unit": ([2], [], ["N.Unit.A", "N.Unit.B", "N.Unit.C", "N.Unit.D"]),
            "Characterization": ([1], ["N.Char.A", "N.Char.B"], [])}

    def __init__(self, root: Path, lock: Path, plant: set[str] = frozenset(), plan_fails: bool = False,
                 scope: str | None = None):
        super().__init__(root)
        self.root, self._lock, self.plant, self.plan_fails, self.scope = root, lock, set(plant), plan_fails, scope
        self.slots_taken = 0
        self.audited = self.built = False
        self.events: list[str] = []  # the order the gate drove the host in: audits, fetch, slot, build
        self.envs: dict[tuple[int, str], dict[str, str]] = {}
        self.plan_obj: dict | None = None

    def lock_path(self) -> Path:
        return self._lock

    def take_slot(self, label, say):
        self.events.append("slot")
        if "slot-settings" in self.plant:
            raise ValueError("planted: settings.json turned malformed while the gate queued")
        self.slots_taken += 1
        return Slot(0, 1, 1, 0.0, -1)  # a held slot's shape, no lock: the cap itself is gate_slot.py's self-test

    def implementer_scope(self):
        """The planted scope: None plants NO setting, so the coded default is in force, as `Host.implementer_scope`
        returns it; reading the real shared settings file is gate_slot.py's self-test."""
        if "settings" in self.plant:
            raise ValueError("planted: a malformed settings.json")
        if self.scope is None:
            return DEFAULT_SCOPE, None
        return self.scope, Setting(self.scope, dt.datetime(2999, 1, 1, tzinfo=dt.timezone.utc), "planted", "t", "t")

    def default_base(self) -> str:
        return "0" * 40

    def audits(self, spawn, say) -> list[str]:
        self.audited = True
        self.events.append("audits")
        say("PLANTED-AUDIT-OUTPUT")
        return ["RENDERED VIEWS RED"] if "audit" in self.plant else []

    def built_self_tests(self, spawn):
        self.events.append("post-build self-tests")
        if "post-build-crash" in self.plant:
            raise RuntimeError("planted: a defect in the post-build self-tests' runner")
        if "post-build" in self.plant:
            return ["POST-BUILD SELF-TESTS RED"], ["--- post-build self-tests (0.0s) ---", "PLANTED-POST-BUILD-RED"]
        return [], ["--- post-build self-tests (0.0s) ---"]

    def fetch_corpus(self, spawn, say) -> list[str]:
        self.events.append("fetch")
        return []

    def build(self, spawn) -> bool:
        self.built = True
        self.events.append("build")
        return "build" not in self.plant

    def publish_timings(self, run, listings):
        self.events.append("publish")
        if "publish" in self.plant:
            raise OSError("planted: the shared timings store is not writable")
        return len(list(gate_plan.trx_results(run))), ""

    def binaries(self):
        if "crash" in self.plant:
            raise RuntimeError("planted: a defect in the driver's own code")
        return {a: {"<test>": f"test-{a}", "Cobol.Net.Compiler.dll": f"compiler-{a}"} for a in GATED_ASSEMBLIES}

    def listing(self, asm, spawn):
        path = synthetic_listing(self.root / f"src-listing-{asm}.txt", self.POPULATION[asm])
        text = Path(path).read_text(encoding="utf-8")
        return Counter(self.POPULATION[asm]), text

    def plan(self, listings, base, previous, out):
        if self.plan_fails:
            raise RuntimeError("planted: the impact analysis crashed")
        plan = {"schema": 1, "base": base, "map": None, "map_state": "none", "every_tier1": ["planted"],
                "constants": {}, "timings": "none",
                "assemblies": {a: {"legs": legs, "leg1": l1, "leg2": l2, "stats": {
                    "cases": len(self.POPULATION[a]), "tiers": {}, "leg1_cases": len(l1), "leg1_seconds": 0,
                    "recorded_seconds": 0, "leg1_floor_seconds": 0, "leg1_floor_collection": None,
                    "one_leg_reason": ""}} for a, (legs, l1, l2) in self.LEGS.items()}}
        plan["sha256"] = hashlib.sha256(gate_plan.canonical(plan)).hexdigest()
        self.plan_obj = plan
        return plan, gate_plan.write_plan(plan, out)

    def run_leg(self, asm, leg, run, env, spawn) -> int:
        self.envs[(leg, asm)] = dict(env)
        planned = env.get("COBOLNET_GATE_LEG") is not None
        names = [n for n in self.POPULATION[asm] if not planned or gate_plan.leg_of(self.plan_obj, asm, n) == leg]
        cases = [(n, ["Passed"]) for n in names]
        red = False
        if asm == "Conformance" and leg == 1 and "red-leg1" in self.plant:
            cases[0] = (cases[0][0], ["Failed"])
            red = True
        if asm == "Conformance" and leg == 2 and "drop" in self.plant:
            cases = cases[1:]
        if asm == "Conformance" and leg == 2 and "duplicate" in self.plant:
            cases.append(("N.Conf.A", ["Passed"]))
        if asm == "Conformance" and "skip" in self.plant and leg == max(self.LEGS[asm][0]):
            cases[-1] = (cases[-1][0], ["NotExecuted"])
        stem = leg_stem(leg, asm)
        synthetic_trx(run / f"{stem}.trx", cases)
        passed = sum(o == ["Passed"] for _, o in cases)
        failed = sum(o == ["Failed"] for _, o in cases)
        log = [f"  Failed {cases[0][0]} [1 s]", "  Error Message:", "   PLANTED-RED-MESSAGE"] if red else []
        log.append(f"{'Failed!' if red else 'Passed!'}  - Failed:     {failed}, Passed:     {passed}, Skipped:     0, "
                   f"Total:     {len(cases)}, Duration: 1 s - Cobol.Net.Tests.{asm}.dll (net10.0)")
        (run / f"{stem}.log").write_text("\n".join(log) + "\n", encoding="utf-8", newline="\n")
        if planned:
            rec = {"schema": 1, "assembly": asm, "leg": leg, "plan_sha256": env["COBOLNET_GATE_PLAN_SHA256"],
                   "plan_content_sha256": self.plan_obj["sha256"],
                   "test_assembly": {"file": f"Cobol.Net.Tests.{asm}.dll", "mvid": "0", "sha256": f"test-{asm}"},
                   "product_assemblies": {"Cobol.Net.Compiler.dll": f"compiler-{asm}"},
                   "received": len(self.POPULATION[asm]), "runs": [gate_plan.name_key(n) for n in names]}
            if "digest" in self.plant and asm == "Conformance" and leg == 2:
                rec["plan_sha256"] = "f" * 64
            if "binary" in self.plant and asm == "Conformance" and leg == 2:
                rec["product_assemblies"] = {"Cobol.Net.Compiler.dll": "rebuilt-between-legs"}
            (run / f"{stem}.json").write_text(json.dumps(rec), encoding="utf-8", newline="\n")
        return 1 if red else 0


def self_test() -> int:
    drop_git_local_env()  # ⛔ hermetic: the arms below make git repositories (gate_slot.drop_git_local_env)
    results: list[tuple[str, bool, str]] = []
    root = Path(tempfile.mkdtemp(prefix="gate-legs-selftest-"))
    sink: list[str] = []

    def gate(name: str, **fake) -> tuple[Outcome, FakeHost]:
        """The arms of the two-leg mechanics (fail-fast, the population, the identity, …) run under the `whole`
        scope unless they name another; `scope=None` plants no setting, the coded default."""
        d = root / name
        d.mkdir()
        fake.setdefault("scope", "whole")
        host = FakeHost(d, d / "gate.lock", **fake)
        sink.clear()
        return Gate(host, d / "runs", say=sink.append).run(), host

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    def said(text: str) -> bool:
        return any(text in line for line in sink)

    try:
        o, h = gate("green")
        arm("a whole planted population in two legs is GREEN, every assembly EXACT",
            o.verdict == "GREEN" and o.exit_code == 0 and all(p.exact for p in o.populations.values())
            and len(o.populations) == 3 and h.slots_taken == 1, o.line)
        arm("an assembly whose leg 1 is empty is not invoked for leg 1, and its population is still whole",
            "Unit" not in o.legs_invoked.get(1, []) and "Unit" in o.legs_invoked.get(2, [])
            and o.populations["Unit"].exact and ("Characterization" not in o.legs_invoked.get(2, [])), str(o.legs_invoked))
        hs = o.handshakes
        arm("every leg host is handed all three handshake variables, naming its own leg and the plan's digest",
            all(set(v) == set(HANDSHAKE) and v["COBOLNET_GATE_LEG"] == str(leg) for (leg, _), v in hs.items()), str(hs))
        o2 = [l for l in sink if l.startswith(VERDICT)]
        arm("the verdict line is the last line, and counts every assembly's discovered cases",
            sink[-1] == o.line and "Conformance 6/6" in o.line and "Unit 4/4" in o.line
            and "Characterization 2/2" in o.line and len(o2) == 1, o.line)
        runs = sorted(p.name for p in (root / "green" / "runs").iterdir())
        rec = json.loads((root / "green" / "runs" / runs[0] / VERDICT_FILE).read_text(encoding="utf-8"))
        arm("the run directory is fresh and holds the verdict file, the plan, every trx, log and identity record",
            len(runs) == 1 and RUN_DIR.fullmatch(runs[0]) is not None and rec["verdict"] == "GREEN"
            and (root / "green" / "runs" / runs[0] / "plan.json").exists()
            and len(list((root / "green" / "runs" / runs[0]).glob("leg-*.trx"))) == 4, str(runs))

        o, _ = gate("drop", plant={"drop"})
        arm("a dropped case is NEVER RAN, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].never_ran.values()) == 1
            and said("NEVER RAN"), o.line)
        o, _ = gate("dup", plant={"duplicate"})
        arm("a case run in both legs is RAN TWICE, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].ran_twice.values()) == 1, o.line)
        o, _ = gate("skip", plant={"skip"})
        arm("a skipped case is counted skipped, never as ran — and the gate stays GREEN",
            o.verdict == "GREEN" and o.populations["Conformance"].skipped == 1 and "skipped 1" in o.line, o.line)
        o, _ = gate("digest", plant={"digest"})
        arm("a leg host that read another plan digest is an IDENTITY MISMATCH, and the gate is RED",
            o.verdict == "RED" and any("plan digest" in p for p in o.identity), str(o.identity))
        o, _ = gate("binary", plant={"binary"})
        arm("a binary changed between the legs is an IDENTITY MISMATCH, and the gate is RED",
            o.verdict == "RED" and any("other product binaries" in p for p in o.identity), str(o.identity))

        o, h = gate("red", plant={"red-leg1"})
        arm("a red in leg 1 STOPS the gate: leg 2 is never invoked, the verdict is RED/INCOMPLETE, exit 1",
            o.verdict == "RED/INCOMPLETE" and o.exit_code == 1 and 2 not in o.legs_invoked and o.stopped
            and not any(k[0] == 2 for k in h.envs), o.line)
        arm("the stopped gate NAMES its remainder: the counts on the verdict line, every case in the run directory",
            o.not_run == {"Conformance": 4, "Unit": 4} and "leg 2 NOT RUN: Conformance 4 · Unit 4" in o.line
            and said("NOT RUN") and "first_red_s" in o.timings, f"{o.not_run} {o.line}")
        arm("a red leg's whole output, its failure message included, reaches the console through test_leg_report.py",
            said("PLANTED-RED-MESSAGE") and said("Conformance leg 1: RED"), "\n".join(sink[-20:]))

        o, h = gate("noplan", plan_fails=True)
        arm("no plan (the planner failed): ONE leg in the plain order, every assembly, no handshake — named, GREEN",
            o.verdict == "GREEN" and o.legs_invoked == {1: list(GATED_ASSEMBLIES)}
            and all(not v for v in o.handshakes.values()) and "no plan (RuntimeError" in o.line, o.line)
        stray = {"COBOLNET_GATE_LEG": "2", "VSTestTestCaseFilter": "FullyQualifiedName~Nothing"}
        os.environ.update(stray)
        try:
            env = leg_env({})
            planned = leg_env({"COBOLNET_GATE_PLAN": "p", "COBOLNET_GATE_LEG": "1", "COBOLNET_GATE_PLAN_SHA256": "d"})
            try:
                leg_env({"COBOLNET_GATE_LEG": "1"})
                partial_refused = False
            except ValueError:
                partial_refused = True
        finally:
            for k in stray:
                os.environ.pop(k, None)
        arm("a leg host's environment is scrubbed of a stray handshake and VSTest property, and a partial handshake "
            "is refused",
            "COBOLNET_GATE_LEG" not in env and "VSTestTestCaseFilter" not in env and planned["COBOLNET_GATE_LEG"] == "1"
            and "VSTestTestCaseFilter" not in planned and partial_refused)

        # Batched gating (kb/Work PB2515): the shared implementer scope `leg1`, and with no setting the coded default.
        o, h = gate("default-scope", scope=None)
        arm("no scope set: the coded default is leg1 (batched gating) — leg 1 only, LEG 1 ONLY: GREEN, no expiry",
            o.scope == "leg1" and o.scope_until is None and o.leg1_only and 2 not in o.legs_invoked
            and o.verdict == f"{LEG1_ONLY}: GREEN" and o.exit_code == 0 and "until" not in o.line, o.line)
        o, h = gate("leg1", scope="leg1")
        arm("scope leg1 (batched gating): leg 1 only, every leg-2 case named NOT RUN, the verdict line says "
            "LEG 1 ONLY and is never a plain GREEN",
            o.verdict == f"{LEG1_ONLY}: GREEN" and o.exit_code == 0 and o.leg1_only and not o.stopped
            and 2 not in o.legs_invoked and not any(k[0] == 2 for k in h.envs)
            and o.not_run == {"Conformance": 4, "Unit": 4} and "GATE: GREEN" not in o.line
            and o.line.startswith(f"{VERDICT}{LEG1_ONLY}: GREEN") and "until 2999-01-01" in o.line
            and json.loads(next((root / "leg1" / "runs").iterdir()).joinpath(VERDICT_FILE).read_text(
                encoding="utf-8"))["leg1_only"] is True, o.line)
        o, _ = gate("leg1-red", scope="leg1", plant={"red-leg1"})
        arm("scope leg1: a red in leg 1 is LEG 1 ONLY: RED, exit 1",
            o.verdict == f"{LEG1_ONLY}: RED" and o.exit_code == 1 and "Conformance leg 1 RED" in o.line, o.line)
        o, _ = gate("leg1-noplan", scope="leg1", plan_fails=True)
        arm("scope leg1 with no plan: the one leg IS the whole population, so the verdict is a plain GREEN",
            o.verdict == "GREEN" and not o.leg1_only and all(p.exact for p in o.populations.values()), o.line)
        o, _ = gate("settings", plant={"settings"})
        arm("malformed shared gate settings: the gate is NOT RUN, exit 2, with the reason",
            o.verdict == "NOT RUN" and o.exit_code == 2 and "malformed settings.json" in o.line, o.line)

        o, h = gate("audit", plant={"audit"})
        arm("a red audit (a tracked rendered view) STOPS the gate before the build: RED, the audit "
            "named, no leg ran",
            o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line and "NO LEG RAN" in o.line
            and h.audited and not h.built and not o.runs and not o.legs_invoked and sink[-1] == o.line, o.line)
        arm("the audits and the corpus fetch run BEFORE the gate slot is taken, and a red audit never queues for one "
            "(kb/Work PB2524)",
            h.slots_taken == 0 and h.events == ["audits"] and o.slot is None
            and gate("order")[1].events == ["audits", "fetch", "slot", "build", "post-build self-tests",
                                                           "publish"],
            str(h.events))
        o, h = gate("post-build", plant={"post-build"})
        arm("a red post-build self-test (one that reads the built assemblies, kb/Work PB2563) runs beside the legs and "
            "makes the gate RED after them, its output printed whole",
            o.verdict == "RED" and o.exit_code == 1 and "POST-BUILD SELF-TESTS RED" in o.line
            and sorted(o.legs_invoked) == [1, 2] and not any(r.red for r in o.runs)
            and said("PLANTED-POST-BUILD-RED"), o.line)
        o, h = gate("publish-whole")
        o2, h2 = gate("publish-stopped", plant={"red-leg1"})
        arm("every gate that ran a leg publishes its measured durations to the shared timings store, a stopped "
            "gate's included (kb/Work PB2527)",
            h.events[-1] == "publish" and o.timings_store.startswith("published 12 ")
            and h2.events[-1] == "publish" and o2.timings_store.startswith("published 4 ")
            and said("to the shared timings store"), f"{o.timings_store} | {o2.timings_store}")
        o, h = gate("publish-fails", plant={"publish"})
        rec = json.loads(next((root / "publish-fails" / "runs").iterdir()).joinpath(VERDICT_FILE).read_text(
            encoding="utf-8"))
        arm("a failed publish to the timings store is reported and recorded, and the verdict is unaffected (GREEN)",
            o.verdict == "GREEN" and said("timings store was NOT updated")
            and rec["timings_store"].startswith("NOT updated (OSError"), o.line)
        o, h = gate("slot-settings", plant={"slot-settings"})
        arm("shared gate settings that turn malformed while the gate queues: NOT RUN, exit 2, with the reason, "
            "the audits already run",
            o.verdict == "NOT RUN" and o.exit_code == 2 and "while the gate queued" in o.line and not h.built
            and h.events == ["audits", "fetch", "slot"], o.line)

        class HungSelfTests(Host):  # the real Host.audits over planted exit codes: no audit runs
            def __init__(self):
                super().__init__(root)
                self.commands: list[list[str]] = []

            def audit_prerequisites(self):
                return ()

            def _captured(self, cmd, spawn, live=None):
                self.commands.append(cmd)
                return (SELF_TESTS_HANG_EXIT if cmd[0] == "scripts/self_tests.py" else 0), "", 0.0

        class PrerequisiteFails(HungSelfTests):  # a real prerequisite process that fails: no audit may start
            def audit_prerequisites(self):
                return (("PLANTED PREREQUISITE", [sys.executable, "-c", "print('not ready'); raise SystemExit(3)"]),)
        unready, heard_unready = PrerequisiteFails(), []
        reasons = unready.audits(_no_slot, heard_unready.append)
        arm("a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its "
            "output, and no audit runs (kb/Work PB1957 row 44)",
            reasons == ["PLANTED PREREQUISITE RED"] and unready.commands == [] and "not ready" in heard_unready,
            f"{reasons} {unready.commands} {heard_unready}")
        hung = HungSelfTests()
        reasons = hung.audits(_no_slot, lambda _: None)
        arm("a hung self-test is `SELF-TESTS HANG` in the gate's reasons, never RED, and every gate's self-tests run "
            "with --reuse (kb/Work PB2914, PB3022)",
            reasons == ["SELF-TESTS HANG"] and ["scripts/self_tests.py", SELF_TESTS_REUSE] in hung.commands,
            f"{reasons} {hung.commands}")

        class FirstRedStops(Host):  # the real Host.audits over REAL processes: a slow audit with a child, and a red one
            def __init__(self, mark: Path):
                super().__init__(root)
                self.mark = mark

            def audit_prerequisites(self):
                return ()

            def audit_commands(self):
                slow = ("import pathlib, subprocess, sys; "
                        "p = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(3600)']); "
                        f"pathlib.Path({str(self.mark)!r}).write_text(str(p.pid), encoding='utf-8'); p.wait()")
                red = (f"import pathlib, time\nm = pathlib.Path({str(self.mark)!r})\n"
                       "while not m.exists():\n    time.sleep(0.05)\nraise SystemExit(1)")
                return (("SLOW", ["-c", slow]), ("QUICK-RED", ["-c", red]))
        mark = Path(tempfile.mkdtemp(prefix="first-red-")) / "grandchild.pid"
        heard: list[str] = []
        reasons = FirstRedStops(mark).audits(_no_slot, heard.append)
        grandchild = int(mark.read_text(encoding="utf-8"))
        while _pid_alive(grandchild):  # termination is asynchronous; a tree that never dies is the hang detector's
            time.sleep(0.05)
        arm("the first RED audit stops every audit still running, its whole process tree included, and the stopped one "
            "is reported STOPPED, never RED (kb/Work PB3061)",
            reasons == ["QUICK-RED RED"] and any(s.startswith("--- audit SLOW: STOPPED") for s in heard)
            and any("1 audit(s) still running stopped" in s for s in heard), f"{reasons} {heard}")
        o, h = gate("post-build-crash", plant={"post-build-crash"})
        arm("a defect in work beside the critical path is a named RED with its traceback, and the legs still ran",
            o.verdict == "RED" and o.exit_code == 1 and "POST BUILD SELF TESTS FAILED (RuntimeError" in o.line
            and len(o.runs) == 4 and said("planted: a defect in the post-build self-tests' runner"), o.line)
        o, h = gate("one-shape", scope="leg1", plant={"red-leg1"})
        arm("ONE SHAPE (kb/Work PB3022): every gate takes a slot, runs its audits before it and stops at a red in "
            "leg 1; there is no mode",
            h.slots_taken == 1 and h.events[:3] == ["audits", "fetch", "slot"] and o.verdict == f"{LEG1_ONLY}: RED"
            and "mode" not in o.line and "mode" not in json.loads(next((root / "one-shape" / "runs").iterdir())
                                                                  .joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
            and f"leg 2 left to {LEG2_OWNER}" in o.line, o.line)
        o, _ = gate("crash", plant={"crash"})
        arm("a defect in the driver still ends in ONE verdict line (NOT RUN, exit 2) — the callers block on it",
            o.verdict == "NOT RUN" and o.exit_code == 2 and sink[-1] == o.line and "RuntimeError" in o.line
            and said("Traceback"), o.line)
        o, _ = gate("build", plant={"build"})
        arm("a failed build is BUILD FAILED, exit 1, and no leg runs",
            o.verdict == "BUILD FAILED" and o.exit_code == 1 and not o.runs, o.line)

        # Timings and reds come from the newest EARLIER run that reached a verdict; five runs are kept.
        d = root / "history"
        (d / "runs").mkdir(parents=True)
        for i in range(7):
            p = d / "runs" / f"2026010{i}T000000Z-00000{i}"
            p.mkdir()
            (p / VERDICT_FILE).write_text("{}", encoding="utf-8", newline="\n")
            synthetic_trx(p / "leg-1-Unit.trx", [("N.Unit.A", ["Passed"])])
        (d / "runs" / "20260109T000000Z-000009").mkdir()  # newest, but it never reached a verdict
        g = Gate(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append)
        prev = g.previous_run(d / "runs" / "20260110T000000Z-00000a")
        g._prune(d / "runs" / "20260109T000000Z-000009")
        kept = sorted(p.name for p in (d / "runs").iterdir())
        arm("the previous run is the newest one holding a verdict, and only the newest five runs are kept",
            prev is not None and prev.name == "20260106T000000Z-000006" and len(kept) == KEEP_RUNS
            and kept[-1] == "20260109T000000Z-000009", f"{prev} {kept}")

        # A second gate in the SAME worktree is refused at once, naming the holder (a real OS lock, two processes).
        d = root / "second"
        d.mkdir()
        lock, go = d / "gate.lock", d / "go"
        holder = subprocess.Popen([sys.executable, "-c", "\n".join([
            "import os, sys, time, pathlib",
            f"sys.path.insert(0, {str(REPO / 'scripts')!r})",
            "from gate_slot import ExclusiveLock",
            f"l = ExclusiveLock.try_take(pathlib.Path({str(lock)!r}), 'first gate (pid %d)' % os.getpid())",
            "print('held' if l else 'NOT HELD', flush=True)",
            f"g = pathlib.Path({str(go)!r})",
            "t = time.monotonic()",
            "while not g.exists() and time.monotonic() - t < 60: time.sleep(0.02)"])],
            stdout=subprocess.PIPE, text=True, cwd=root.parent)
        try:
            first = holder.stdout.readline().strip()
            host2 = FakeHost(d, lock)
            sink.clear()
            o = Gate(host2, d / "runs", say=sink.append).run()
        finally:
            go.touch()
            holder.wait(timeout=30)
        arm("a second gate in the same worktree is REFUSED at once, naming the holder's pid, and takes no slot",
            first == "held" and o.verdict == "NOT RUN" and o.exit_code == 2 and f"pid {holder.pid}" in o.line
            and host2.slots_taken == 0 and not (d / "runs").exists(), o.line)

        # The lock is per WORKTREE: a linked worktree's git dir is its own.
        git = ["git", "-c", "user.name=gate-legs", "-c", "user.email=gate-legs@invalid", "-c", "init.defaultBranch=main"]
        repo, linked = root / "repo", root / "linked"
        repo.mkdir()
        for argv in (["init", "-q"], ["commit", "-q", "--allow-empty", "-m", "self-test"],
                     ["worktree", "add", "-q", "--detach", str(linked)]):
            subprocess.run(git + argv, cwd=repo, check=True, capture_output=True)
        a, b = Host(repo).lock_path(), Host(linked).lock_path()
        arm("the gate lock is per worktree: the main checkout and a linked worktree lock different files",
            a != b and a.parent == (repo / ".git").resolve() and b.parent.parent == (repo / ".git" / "worktrees").resolve(),
            f"{a} vs {b}")
    finally:
        for dirpath, _, files in os.walk(root):  # git writes read-only objects, which rmtree cannot delete on Windows
            for f in files:
                os.chmod(os.path.join(dirpath, f), 0o666)
        shutil.rmtree(root, ignore_errors=True)

    failed = [n for n, ok, _ in results if not ok]
    print(f"=== run_gate_legs SELF-TEST: {'PASS' if not failed else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(failed)}" if failed else "") + ") ===")
    return 0 if not failed else 1


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001 — a stream without reconfigure keeps its encoding; the gate still runs
        pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base", help="the change's cut point (default: git merge-base HEAD origin/main)")
    ap.add_argument("--self-test", action="store_true", help="fire every arm on planted inputs (DESIGN §3.14.4 (3))")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    host = Host()
    return Gate(host, REPO / RUN_ROOT, base=args.base).run().exit_code


if __name__ == "__main__":
    sys.exit(main())
