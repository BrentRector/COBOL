#!/usr/bin/env python3
"""run_gate_legs.py — THE GATE DRIVER: the ordered whole-population gate (kb/Work PB1708, PB1721;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.1, §3.14.3–3.14.4, §3.14.6).

    python scripts/run_gate_legs.py --mode implementer [--base <sha>]   # two legs, fail-fast, a gate slot
    python scripts/run_gate_legs.py --mode lander                       # one leg, every red, no slot
    python scripts/run_gate_legs.py --self-test

Called by `scripts/build-local.ps1 -Mode …` and `scripts/build-local.sh --mode …`, which only set the process
priority. ⛔ EVERY GATE RUNS THE WHOLE DISCOVERED POPULATION of Conformance, Unit and Characterization. Ordering
changes WHEN a case runs, never WHETHER (owner, 2026-09-28: "order, don't skip") — with ONE exception the owner
chose as a measured trial: the implementer SCOPE `leg1` below (kb/Work PB2515, 2026-10-07).

THE MODES are named by the caller, never inferred (§3.14.1):
  implementer  the order plan (scripts/gate_plan.py), TWO legs — the likely-red cases first — and FAIL-FAST: a red in
               leg 1 stops the gate there, RED/INCOMPLETE, with the remainder named. It holds a GATE SLOT
               (scripts/gate_slot.py) from before the build to the verdict, so at most N implementer gates build or
               test at once, repository-wide; its audits run BEFORE it queues for the slot (kb/Work PB2524).
               Its SCOPE is a shared gate setting (`gate_slot.py set-implementer-scope`, read once at the start):
               `whole` (the default) runs both legs; `leg1` — the batched-gating trial, which must carry an expiry
               and reverts to `whole` by itself — runs leg 1 only and names every leg-2 case NOT RUN, and its verdict
               line says `LEG 1 ONLY (batched-gating trial, PB2515)` so it is never read as a whole-population GREEN.
               The lander's train gate is then the population check for every cluster in the train.
  lander       no plan, ONE leg, so every red of every cluster shows in one run, and no slot: the lander never waits.
               The implementer scope never applies to it.

ONE GATE, in this order (§3.14.3):
  1. the worktree's GATE LOCK (`<worktree git dir>/cobol-gate.lock`): a second gate in the same worktree is REFUSED at
     once, naming the holder — its build would overwrite the binaries between this gate's legs;
  2. a fresh RUN DIRECTORY `TestResults/build-local/<UTC stamp>-<nonce>/` holding the listings, the plan, every trx,
     log and identity record, and the verdict file — nothing at a fixed name, so no earlier gate's file is read as
     this one's; the newest earlier run holding a verdict gives the plan its timings and reds; five are kept;
  3. the audits (the one list below) — in implementer mode FAIL-FAST: a red audit ends the gate RED right there, with
     no leg run (kb/Work PB2523) — then the GnuCOBOL corpus fetch. Both run BEFORE the slot (kb/Work PB2524): they
     read only the tree, and inside the slot they were 35-45 % of its hold while every other implementer gate queued;
  4. the gate slot (implementer only), always after the lock, so no two gates can wait on each other in a cycle; then
     the solution build and the SHA-256 of every binary the legs will run;
  5. `--list-tests` per assembly (scrubbed), the plan, then per leg the three assemblies CONCURRENTLY, each through the
     leg handshake (all three COBOLNET_GATE_* variables, or none) and printed through scripts/test_leg_report.py;
     an assembly the plan gives no case in a leg is not invoked for it;
  6. the POPULATION CHECK (scripts/test_population.py) per assembly over its legs' trx files, the publish of every
     measured duration to the SHARED TIMINGS STORE that times the next implementer plan, even in a fresh worktree
     (`<git common dir>/cobol-gate-timings/`, gate_plan.py, kb/Work PB2527), the IDENTITY check (each
     leg host's record names this plan's digest and the binaries step 3 hashed), then the verdict line:
     `=== BUILD-LOCAL GATE: GREEN — Conformance 9,317/9,317 · Unit … cases ran (skipped 0) in 2 legs · … ===`.
It is GREEN only when every leg ran, every leg is green, every population is exact and every identity matches. Under
the `leg1` scope the verdict is `LEG 1 ONLY (batched-gating trial, PB2515): GREEN` or `…: RED` instead, whenever the
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
import subprocess
import sys
import tempfile
import time
import traceback
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
import gate_plan  # noqa: E402
import impacted_tests  # noqa: E402
from gate_slot import (DEFAULT_SCOPE, ExclusiveLock, GateSettings, GateSlots, Setting, Slot,  # noqa: E402
                       drop_git_local_env, git_common_dir, slot_dir)
from test_population import (GATED_ASSEMBLIES, HANDSHAKE, Population, PopulationError, check as check_population,  # noqa: E402
                             list_population, scrubbed_env, synthetic_listing, synthetic_trx)

MODES = ("implementer", "lander")
VERDICT = "=== BUILD-LOCAL GATE: "
#: The verdict prefix of an implementer gate that ran leg 1 only under the `leg1` scope (kb/Work PB2515): never a
#: whole-population GREEN, and never mistaken for one — `GATE: GREEN` does not match it.
LEG1_ONLY = "LEG 1 ONLY (batched-gating trial, PB2515)"
RUN_ROOT = Path("TestResults") / "build-local"
RUN_DIR = re.compile(r"\d{8}T\d{6}Z-[0-9a-f]{6}")
KEEP_RUNS = 5
LOCK_NAME = "cobol-gate.lock"
VERDICT_FILE = "verdict.json"
#: A stopped gate's remainder is named in full in the run directory; the console shows this many per assembly.
NOT_RUN_SHOWN = 10

#: ⛔ THE AUDITS, in order — THE list for the gate (build-local.ps1 and .sh used to carry a copy each); battery.sh PHASE
#: -1 and CI's `audits` job run the same scripts. A wrong § is the one defect no test can catch, and each costs about a
#: second. None needs a build or a test result, so they run FIRST: an implementer gate stops on a red one before it
#: builds (kb/Work PB2523 — four of wave 1034's eight first-run reds were audit-only, each found after a whole ~6-min
#: population); the lander's gate goes on building and testing, so its one run shows every red of the train. And they
#: run OUTSIDE the implementer's gate slot (kb/Work PB2524): the slot rations builds and test legs, not reading the tree.
AUDITS = (
    ("CITATIONS", ["scripts/spec/audit_code_citations.py", "--check"]),
    ("DOC CITATIONS", ["scripts/spec/audit_doc_citations.py", "--check"]),
    ("EVIDENCE SUPERSESSION", ["scripts/spec/audit_evidence_supersession.py", "--check"]),
    ("DRIFT RULES INDEX", ["scripts/spec/drift_rules.py", "--check"]),
    ("GUARD HOOK SELF-TEST", ["scripts/hooks/test_forbidden_commands.py"]),
    ("READ-ONLY HOOK SELF-TEST", ["scripts/hooks/test_readonly_repo.py"]),
    ("WORKTREE RM-ALLOW HOOK SELF-TEST", ["scripts/hooks/test_worktree_rm_allow.py"]),
    ("WITNESS LOSS", ["scripts/spec/audit_witness_loss.py", "--check"]),
    ("RULE CATALOG", ["scripts/spec/extract_rule_catalog.py", "--check"]),
    ("SPEC CORRECTIONS", ["scripts/spec/verify_publishable.py"]),
    ("CITE SELF-TEST", ["scripts/spec/cite.py", "--self-test"]),
    ("LEDGER PLAN SELF-TEST", ["scripts/spec/ledger_plan.py", "--self-test"]),
)
#: The git-ignored GPL corpus ExternalCorpusPopulationDriftTests measure (kb/Work PB209, PB897): absent in a fresh
#: worktree, so the gate fetches it; a failed fetch makes the gate RED, attributed to the fetch.
CORPUS_MARKER = Path("tests/external/gnucobol/tests/testsuite.src")

Spawn = Callable[..., dict]


def _no_slot(**kwargs) -> dict:
    return kwargs


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
        """The shared implementer scope (gate_slot.py; kb/Work PB2515): `whole` or `leg1`, and the live setting that
        chose it (None: the default). Malformed settings raise ValueError: the gate is NOT RUN, with the reason."""
        return GateSettings.read(slot_dir(self.repo)).effective_scope()

    def default_base(self) -> str:
        return self.git("merge-base", "HEAD", "origin/main")

    def audits(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """The audits (AUDITS), every one of them. Returns the RED reasons; output goes straight to the console."""
        reds = []
        for name, cmd in AUDITS:
            if subprocess.run([sys.executable, *cmd], cwd=self.repo, **spawn()).returncode != 0:
                say(f"=== {name}: RED (see above) ===")
                reds.append(f"{name} RED")
        return reds

    def fetch_corpus(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """The GnuCOBOL corpus fetch when the worktree lacks it. Returns the RED reason, if any."""
        reds = []
        if not (self.repo / CORPUS_MARKER).exists():
            say("=== EXTERNAL CORPUS: absent in this worktree — fetching (GPL, git-ignored, never committed) ===")
            rc = subprocess.run(["pwsh", "-NoProfile", "-File", "scripts/fetch-gnucobol-tests.ps1"], cwd=self.repo,
                                **spawn()).returncode
            if rc != 0 or not (self.repo / CORPUS_MARKER).exists():
                say(f"=== EXTERNAL CORPUS: FETCH FAILED (exit {rc}; the FETCH FAILED line above names the cause) — "
                    "the ExternalCorpusPopulationDriftTests reds in the Unit leg are ATTRIBUTABLE TO IT, and this gate "
                    "is RED because it could not measure that population ===")
                reds.append("EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED")
        return reds

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
        plan = gate_plan.build_plan({a: list(c.elements()) for a, c in listings.items()}, analysis, timings, reds,
                                    base)
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
    mode: str
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


class Gate:
    def __init__(self, mode: str, host: Host, run_root: Path, base: str | None = None,
                 say: Callable[[str], None] = lambda s: print(s, flush=True)):
        if mode not in MODES:
            raise ValueError(f"--mode must be one of {MODES}, not {mode!r}")
        self.mode, self.host, self.run_root, self.base, self.say = mode, host, run_root, base, say

    # The verdict line and exit code are decided here, once, for every path through the gate.
    def _finish(self, out: Outcome, run: Path | None, verdict: str, detail: str, code: int) -> Outcome:
        out.verdict, out.exit_code = verdict, code
        out.line = f"{VERDICT}{verdict} — {detail} ==="
        if run is not None:
            (run / VERDICT_FILE).write_text(json.dumps(self._record(out, run), indent=1, sort_keys=True) + "\n",
                                            encoding="utf-8")
        self.say(out.line)
        return out

    def run(self) -> Outcome:
        out = Outcome(self.mode)
        started = time.monotonic()
        label = f"{self.mode} gate {self.host.repo} (pid {os.getpid()}, since {time.strftime('%Y-%m-%d %H:%M:%S')})"
        lock_path = self.host.lock_path()
        lock = ExclusiveLock.try_take(lock_path, label)
        if lock is None:
            return self._finish(out, None, "NOT RUN", f"another gate holds this worktree's gate lock ({lock_path}): "
                                f"{ExclusiveLock.holder(lock_path) or '(no label)'}", 2)
        with lock:
            if self.mode == "implementer":
                try:
                    out.scope, setting = self.host.implementer_scope()
                except ValueError as e:  # malformed shared gate settings stop the gate, with the reason
                    return self._finish(out, None, "NOT RUN", str(e), 2)
                if setting is not None and setting.until is not None:
                    out.scope_until = setting.until.isoformat()
            try:
                return self._gate(out, label, started)
            except Exception as e:  # noqa: BLE001 — a defect in the driver still ends in a verdict line, never silence:
                # every caller BLOCKS on that line (MANDATORY-PRACTICES P2), so a traceback alone would hang it.
                self.say(traceback.format_exc().rstrip())
                return self._finish(out, None, "NOT RUN", f"the gate driver failed: {type(e).__name__}: {e}", 2)

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
        self.say(f"build-local: {self.mode} gate, run directory {run}")
        # ⛔ THE AUDITS AND THE CORPUS FETCH RUN BEFORE THE GATE SLOT IS TAKEN (kb/Work PB2524). They read only the
        # tree — no build output, no test result — so they need nothing the slot rations (concurrent builds and test
        # legs, DESIGN-test-build-ci §3.14.6). Inside the slot they were 35-45 % of its hold (~230 s of serial Python
        # per gate, one core busy) while every other implementer gate queued behind it. They still BLOCK the verdict:
        # a red audit ends an implementer gate RED before it ever queues, and is merged into the lander's verdict.
        t = time.monotonic()
        audit_reds = self.host.audits(_no_slot, self.say)
        out.timings["audits_s"] = round(time.monotonic() - t, 1)
        if audit_reds and self.mode == "implementer":
            # FAIL FAST ON THE AUDITS (kb/Work PB2523): they need no build and no test result and take seconds, while
            # a red one used to be reported only after the whole ~6-min population — 4 of wave 1034's 8 first-run
            # reds were audit-only. The lander keeps going: its one run reports every red of the train at once.
            out.reasons += audit_reds
            return self._finish(out, run, "RED", f"{'; '.join(audit_reds)} — fail-fast: the audits run before the "
                                f"slot and the build and NO LEG RAN; fix the audit and re-gate · {self.mode} mode "
                                f"(run {run.name})", 1)
        out.reasons += audit_reds + self.host.fetch_corpus(_no_slot, self.say)
        if self.mode != "implementer":
            return self._tested(out, run, None, started)
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

    def _tested(self, out: Outcome, run: Path, slot: Slot | None, started: float) -> Outcome:
        """The build, the listings, the plan, the legs and the checks: the part of the gate the slot rations."""
        spawn: Spawn = slot.spawn_kwargs if slot is not None else _no_slot
        t = time.monotonic()
        if not self.host.build(spawn):
            return self._finish(out, run, "BUILD FAILED", "the solution did not build (see above)", 1)
        out.timings["build_s"] = round(time.monotonic() - t, 1)
        try:
            binaries = self.host.binaries()
            (run / "binaries.json").write_text(json.dumps(binaries, indent=1, sort_keys=True), encoding="utf-8")
            # The three listings run CONCURRENTLY, as the legs do: each is a separate `--list-tests` host (~2 s).
            with concurrent.futures.ThreadPoolExecutor(max_workers=len(GATED_ASSEMBLIES)) as pool:
                listed = dict(zip(GATED_ASSEMBLIES, pool.map(lambda a: self.host.listing(a, spawn), GATED_ASSEMBLIES)))
            listings = {}
            for asm, (names, raw) in listed.items():
                listings[asm] = names
                (run / f"listing-{asm}.txt").write_text(raw, encoding="utf-8")
        except PopulationError as e:
            return self._finish(out, run, "NOT RUN", f"the population could not be listed: {e}", 2)

        plan, digest = self._plan(out, listings, run)
        legs = self._legs(plan)
        to_run = legs
        if self.mode == "implementer" and out.scope == "leg1" and len(legs) > 1:
            # The batched-gating trial (kb/Work PB2515): leg 2 is the lander's whole-population train gate. The
            # population check below still measures every planned leg, so each leg-2 case is named NOT RUN.
            to_run = {1: legs[1]}  # two planned legs: leg 1 is one of them
            out.leg1_only = True
            self.say(f"build-local: {LEG1_ONLY} — the implementer scope is leg1"
                     + (f" until {out.scope_until}" if out.scope_until else "")
                     + "; leg 2 is left to the lander's whole-population train gate")
        legs_started = time.monotonic()
        for leg, assemblies in to_run.items():
            if out.stopped:
                break
            out.legs_invoked[leg] = assemblies
            leg_runs = self._run_leg(out, leg, assemblies, run, plan, digest, spawn, legs_started)
            if any(r.red for r in leg_runs) and self.mode == "implementer" and leg < max(to_run):
                out.stopped = True  # FAIL FAST: the remainder is named by the population check below
        out.timings["legs_s"] = round(time.monotonic() - legs_started, 1)
        reds = [r for r in out.runs if r.red]
        if reds:
            out.timings["first_red_s"] = round(min(r.finished_s for r in reds), 1)

        self._check_populations(out, listings, legs, run)
        self._publish_timings(out, run, listings)
        if plan is not None:
            self._check_identity(out, plan, digest, binaries, listings, run)
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        self._prune(run)
        return self._verdict(out, run, plan, legs)

    def _plan(self, out: Outcome, listings: dict[str, Counter], run: Path) -> tuple[dict | None, str | None]:
        """Implementer: the order plan. No plan input, a stale one or a broken one gives ONE leg in the plain order
        (§3.14.1) — named, never a skip. Lander: no plan."""
        if self.mode == "lander":
            out.plan_note = "no plan (lander)"
            return None, None
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
                (run / f"not-run-{asm}.txt").write_text("\n".join(named) + "\n", encoding="utf-8")
                lines = [lines[0], *[l for l in lines[1:] if l not in named], *named[:NOT_RUN_SHOWN]]
                if len(named) > NOT_RUN_SHOWN:
                    lines.append(f"  … {len(named) - NOT_RUN_SHOWN:,} more NOT RUN, every one named in "
                                 f"{run / f'not-run-{asm}.txt'}")
            for line in lines:
                self.say(line)

    def _publish_timings(self, out: Outcome, run: Path, listings: dict[str, Counter]) -> None:
        """Every gate that ran a leg, lander included, publishes its measured durations to the shared timings store
        (kb/Work PB2527), so the NEXT implementer gate — in a fresh worktree — plans longest first. The store is a
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
        detail = (f"{tally} cases ran (skipped {skipped:,}) in {len(out.legs_invoked)} of {n_legs} leg(s) · "
                  f"{self.mode} mode" + (f", {out.slot}" if out.slot else "") + f" ({out.plan_note}; run {run.name})")
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
            left = (f"leg 2 left to the lander's whole-population train gate: {remainder} (named in "
                    f"{run / 'not-run-*.txt'}; scope leg1" + (f" until {out.scope_until}" if out.scope_until else "")
                    + ")")
            if problems:
                return self._finish(out, run, f"{LEG1_ONLY}: RED", f"{'; '.join(problems)}; {left} — {detail}", 1)
            return self._finish(out, run, f"{LEG1_ONLY}: GREEN", f"{left} — {detail}", 0)
        if problems:
            return self._finish(out, run, "RED", f"{'; '.join(problems)} — {detail}", 1)
        return self._finish(out, run, "GREEN", detail, 0)

    def _record(self, out: Outcome, run: Path) -> dict:
        return {"schema": 1, "mode": out.mode, "verdict": out.verdict, "line": out.line, "reasons": out.reasons,
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
                 scope: str = DEFAULT_SCOPE):
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
        """The planted scope; reading the real shared settings file is gate_slot.py's self-test."""
        if "settings" in self.plant:
            raise ValueError("planted: a malformed settings.json")
        if self.scope == DEFAULT_SCOPE:
            return DEFAULT_SCOPE, None
        return self.scope, Setting(self.scope, dt.datetime(2999, 1, 1, tzinfo=dt.timezone.utc), "planted", "t", "t")

    def default_base(self) -> str:
        return "0" * 40

    def audits(self, spawn, say) -> list[str]:
        self.audited = True
        self.events.append("audits")
        return ["DRIFT RULES INDEX RED"] if "audit" in self.plant else []

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
        (run / f"{stem}.log").write_text("\n".join(log) + "\n", encoding="utf-8")
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
            (run / f"{stem}.json").write_text(json.dumps(rec), encoding="utf-8")
        return 1 if red else 0


def self_test() -> int:
    drop_git_local_env()  # ⛔ hermetic: the arms below make git repositories (gate_slot.drop_git_local_env)
    results: list[tuple[str, bool, str]] = []
    root = Path(tempfile.mkdtemp(prefix="gate-legs-selftest-"))
    sink: list[str] = []

    def gate(mode: str, name: str, **fake) -> tuple[Outcome, FakeHost]:
        d = root / name
        d.mkdir()
        host = FakeHost(d, d / "gate.lock", **fake)
        sink.clear()
        return Gate(mode, host, d / "runs", say=sink.append).run(), host

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    def said(text: str) -> bool:
        return any(text in line for line in sink)

    try:
        o, h = gate("implementer", "green")
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

        o, _ = gate("implementer", "drop", plant={"drop"})
        arm("a dropped case is NEVER RAN, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].never_ran.values()) == 1
            and said("NEVER RAN"), o.line)
        o, _ = gate("implementer", "dup", plant={"duplicate"})
        arm("a case run in both legs is RAN TWICE, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].ran_twice.values()) == 1, o.line)
        o, _ = gate("implementer", "skip", plant={"skip"})
        arm("a skipped case is counted skipped, never as ran — and the gate stays GREEN",
            o.verdict == "GREEN" and o.populations["Conformance"].skipped == 1 and "skipped 1" in o.line, o.line)
        o, _ = gate("implementer", "digest", plant={"digest"})
        arm("a leg host that read another plan digest is an IDENTITY MISMATCH, and the gate is RED",
            o.verdict == "RED" and any("plan digest" in p for p in o.identity), str(o.identity))
        o, _ = gate("implementer", "binary", plant={"binary"})
        arm("a binary changed between the legs is an IDENTITY MISMATCH, and the gate is RED",
            o.verdict == "RED" and any("other product binaries" in p for p in o.identity), str(o.identity))

        o, h = gate("implementer", "red", plant={"red-leg1"})
        arm("a red in leg 1 STOPS the gate: leg 2 is never invoked, the verdict is RED/INCOMPLETE, exit 1",
            o.verdict == "RED/INCOMPLETE" and o.exit_code == 1 and 2 not in o.legs_invoked and o.stopped
            and not any(k[0] == 2 for k in h.envs), o.line)
        arm("the stopped gate NAMES its remainder: the counts on the verdict line, every case in the run directory",
            o.not_run == {"Conformance": 4, "Unit": 4} and "leg 2 NOT RUN: Conformance 4 · Unit 4" in o.line
            and said("NOT RUN") and "first_red_s" in o.timings, f"{o.not_run} {o.line}")
        arm("a red leg's whole output, its failure message included, reaches the console through test_leg_report.py",
            said("PLANTED-RED-MESSAGE") and said("Conformance leg 1: RED"), "\n".join(sink[-20:]))

        o, h = gate("implementer", "noplan", plan_fails=True)
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

        # The batched-gating trial (kb/Work PB2515): the shared implementer scope `leg1`.
        o, h = gate("implementer", "leg1", scope="leg1")
        arm("scope leg1 (the batched-gating trial): leg 1 only, every leg-2 case named NOT RUN, the verdict line says "
            "LEG 1 ONLY and is never a plain GREEN",
            o.verdict == f"{LEG1_ONLY}: GREEN" and o.exit_code == 0 and o.leg1_only and not o.stopped
            and 2 not in o.legs_invoked and not any(k[0] == 2 for k in h.envs)
            and o.not_run == {"Conformance": 4, "Unit": 4} and "GATE: GREEN" not in o.line
            and o.line.startswith(f"{VERDICT}{LEG1_ONLY}: GREEN") and "until 2999-01-01" in o.line
            and json.loads(next((root / "leg1" / "runs").iterdir()).joinpath(VERDICT_FILE).read_text(
                encoding="utf-8"))["leg1_only"] is True, o.line)
        o, _ = gate("implementer", "leg1-red", scope="leg1", plant={"red-leg1"})
        arm("scope leg1: a red in leg 1 is LEG 1 ONLY: RED, exit 1",
            o.verdict == f"{LEG1_ONLY}: RED" and o.exit_code == 1 and "Conformance leg 1 RED" in o.line, o.line)
        o, _ = gate("implementer", "leg1-noplan", scope="leg1", plan_fails=True)
        arm("scope leg1 with no plan: the one leg IS the whole population, so the verdict is a plain GREEN",
            o.verdict == "GREEN" and not o.leg1_only and all(p.exact for p in o.populations.values()), o.line)
        o, _ = gate("implementer", "settings", plant={"settings"})
        arm("malformed shared gate settings: the implementer gate is NOT RUN, exit 2, with the reason",
            o.verdict == "NOT RUN" and o.exit_code == 2 and "malformed settings.json" in o.line, o.line)

        o, h = gate("lander", "lander", plant={"red-leg1"}, scope="leg1")
        arm("-Mode lander: ONE leg, every assembly, no plan, no handshake, no slot, and no fail-fast (RED, complete)",
            o.verdict == "RED" and o.legs_invoked == {1: list(GATED_ASSEMBLIES)} and h.slots_taken == 0
            and not o.stopped and all(not v for v in o.handshakes.values()) and o.slot is None
            and not o.leg1_only and all(p.exact for p in o.populations.values()), o.line)
        o, h = gate("implementer", "audit", plant={"audit"})
        arm("a red audit (a stale drift-rules index) STOPS the implementer gate before the build: RED, the audit "
            "named, no leg ran",
            o.verdict == "RED" and o.exit_code == 1 and "DRIFT RULES INDEX RED" in o.line and "NO LEG RAN" in o.line
            and h.audited and not h.built and not o.runs and not o.legs_invoked and sink[-1] == o.line, o.line)
        arm("the audits and the corpus fetch run BEFORE the gate slot is taken, and a red audit never queues for one "
            "(kb/Work PB2524)",
            h.slots_taken == 0 and h.events == ["audits"] and o.slot is None
            and gate("implementer", "order")[1].events == ["audits", "fetch", "slot", "build", "publish"],
            str(h.events))
        o, h = gate("lander", "publish-lander")
        o2, h2 = gate("implementer", "publish-stopped", plant={"red-leg1"})
        arm("every gate that ran a leg publishes its measured durations to the shared timings store, the lander's and "
            "a stopped gate's included (kb/Work PB2527)",
            h.events[-1] == "publish" and o.timings_store.startswith("published 12 ")
            and h2.events[-1] == "publish" and o2.timings_store.startswith("published 4 ")
            and said("to the shared timings store"), f"{o.timings_store} | {o2.timings_store}")
        o, h = gate("implementer", "publish-fails", plant={"publish"})
        rec = json.loads(next((root / "publish-fails" / "runs").iterdir()).joinpath(VERDICT_FILE).read_text(
            encoding="utf-8"))
        arm("a failed publish to the timings store is reported and recorded, and the verdict is unaffected (GREEN)",
            o.verdict == "GREEN" and said("timings store was NOT updated")
            and rec["timings_store"].startswith("NOT updated (OSError"), o.line)
        o, h = gate("implementer", "slot-settings", plant={"slot-settings"})
        arm("shared gate settings that turn malformed while the gate queues: NOT RUN, exit 2, with the reason, "
            "the audits already run",
            o.verdict == "NOT RUN" and o.exit_code == 2 and "while the gate queued" in o.line and not h.built
            and h.events == ["audits", "fetch", "slot"], o.line)
        o, h = gate("lander", "audit-lander", plant={"audit"})
        arm("a red audit in -Mode lander is RED and the legs still run: the train's one run shows every red",
            o.verdict == "RED" and "DRIFT RULES INDEX RED" in o.line and h.built and len(o.runs) == 3, o.line)
        o, _ = gate("implementer", "crash", plant={"crash"})
        arm("a defect in the driver still ends in ONE verdict line (NOT RUN, exit 2) — the callers block on it",
            o.verdict == "NOT RUN" and o.exit_code == 2 and sink[-1] == o.line and "RuntimeError" in o.line
            and said("Traceback"), o.line)
        o, _ = gate("implementer", "build", plant={"build"})
        arm("a failed build is BUILD FAILED, exit 1, and no leg runs",
            o.verdict == "BUILD FAILED" and o.exit_code == 1 and not o.runs, o.line)

        # Timings and reds come from the newest EARLIER run that reached a verdict; five runs are kept.
        d = root / "history"
        (d / "runs").mkdir(parents=True)
        for i in range(7):
            p = d / "runs" / f"2026010{i}T000000Z-00000{i}"
            p.mkdir()
            (p / VERDICT_FILE).write_text("{}", encoding="utf-8")
            synthetic_trx(p / "leg-1-Unit.trx", [("N.Unit.A", ["Passed"])])
        (d / "runs" / "20260109T000000Z-000009").mkdir()  # newest, but it never reached a verdict
        g = Gate("implementer", FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append)
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
            o = Gate("implementer", host2, d / "runs", say=sink.append).run()
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
    ap.add_argument("--mode", choices=MODES, help="implementer (two legs, fail-fast, a slot) or lander (one leg)")
    ap.add_argument("--base", help="implementer: the change's cut point (default: git merge-base HEAD origin/main)")
    ap.add_argument("--self-test", action="store_true", help="fire every arm on planted inputs (DESIGN §3.14.4 (3))")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.mode is None:
        ap.error("--mode lander|implementer is required: the caller names the gate's mode, it is never inferred")
    host = Host()
    return Gate(args.mode, host, REPO / RUN_ROOT, base=args.base).run().exit_code


if __name__ == "__main__":
    sys.exit(main())
