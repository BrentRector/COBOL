## Entry 2113 — 2026-10-16 14:46 PDT — PB3197: every gate script runs the one probed Python interpreter, in bash and in PowerShell

PB1754 R3 wave 1098, group B (unify; kb/Work PB3197, an R2 duplication finding). `scripts/python-resolve.sh` (kb/Work
PB1637) declared itself THE interpreter the gate scripts run, probing `$PYTHON`, `python3`, `python` by RUNNING each
against the 3.12 floor, because a name on PATH is not an interpreter. The gate's own entry points bypassed it: both
`build-local` twins ran a bare `python`, and `linux-gate.sh` took `command -v python3 || command -v python`, with no
probe and no floor.

**Re-probe, base 16b82f639.** `bash scripts/build-local.sh` on a PATH holding only a `python3` (the stock Ubuntu 24.04
shape) died with `python: command not found`, exit 127, and printed no `=== BUILD-LOCAL GATE` line. That is the gate
command CONTRIBUTING.md gives for bash. On a PATH whose `python3` answers the floor probe as 3.11 does, linux-gate's
line took it and ran it, and the resolver refused it. The note's fourth site, a bare `python3` in a hook loop at
`linux-gate.sh:107`, no longer exists.

**Fix.** The probe has to run before any Python can, so it is written once per shell that starts a gate:
- `build-local.sh` and `linux-gate.sh` source `python-resolve.sh` and run `"$PY"`.
- `build-local.ps1` dot-sources a new PowerShell twin, `scripts/python-resolve.ps1`, and runs `& $PY`. The twin
  probes the same candidates in the same order with the same probe program. When nothing runs it throws a
  terminating error. Measured: an `exit` in a dot-sourced script ends only that script, so its caller would keep
  running with no interpreter.
- A host with no runnable Python ends on each entry point's own verdict line, NOT RUN with exit 2, the same as every
  other NOT RUN. The bash entries source the resolver in a subshell, and the PowerShell entry catches the twin's error.
  Before the fix, the Linux gate's verdict for this case was "no python" and the build-local gates had none.

The resolver's other callers (`battery.sh`, `measure-battery-determinism.sh`, CI's conformance-population steps) keep
its contract unchanged. After-probes, using a copy of each entry point with a stand-in driver:
- a python3-only PATH runs the driver under that python3, exit 0;
- the 3.11 stand-in gives `=== BUILD-LOCAL GATE: NOT RUN — python-resolve.sh found no runnable Python … ===`, exit 2;
- on this Windows host, `build-local.ps1` passes over the Store alias `python3`, runs `python` and propagates the
  driver's exit code (3);
- with an unrunnable `$PYTHON` it falls through, and with no Python on PATH it ends `NOT RUN — python-resolve: ⛔ no
  runnable Python >= 3.12 …`, exit 2.

**Witnesses.** `linux-gate.sh --self-test` gains three arms that run with stand-in interpreters (shell functions the
resolver's probe calls): a python3 below the floor is passed over for a python at it, `$PYTHON` is probed first, and
no interpreter at all is NOT RUN with exit 2. All 13 arms are green from the root and from another directory. The new
Unit drift test `PythonInterpreterDriftTests` is a `[Trait("Sweep", "tree")]` sweep that checks three things:
- no bash script under `scripts/` and no PowerShell twin of a bash script names an interpreter as a COMMAND (at a
  command's start, after `command -v`/`which`/`env`/`nice`, a `"${prefix[@]}"` array or an environment assignment);
- every script that runs `$PY` sources its own shell's resolver;
- the two resolvers' candidate lists and probe programs are equal.

The test asserts its own population: the four bash users, the PowerShell twin, and the two provisioners
(`scripts/cloud/setup-env.sh`, `scripts/wsl/setup-wsl.sh`, exempt by content because they `uv python install` the
interpreter). It was seen red on this branch before the fix at exactly the three note sites (`build-local.sh:19`,
`build-local.ps1:26`, `linux-gate.sh:129`). Its command regex was also checked against 11 planted lines, catching the
6 that run an interpreter and passing the 5 that do not.

**Sibling sweep.** No other bash script names an interpreter: the drift test is the sweep, and its population check
shows it read them. The PowerShell tooling that is not a gate entry still runs a bare `python`: `session-probe.ps1`,
`gen-vcr.ps1`, `gen-diagnostics-doc.ps1`, `orchestrator/stop.ps1` and the orchestrator self-tests. On the Windows box
that name is the real CPython, and those files are outside this wave's file set, so they are reported as a lead. The
CI workflow likewise names its runner images' interpreters directly. Also fixed: a stale `linux-gate.sh` comment that
said its selftests leg runs `python3`.

**Docs.** DESIGN-test-build-ci.md §3.14.11 is new (the rule, where it lives, the drift test). DESIGN-architecture-review.md
§5.6 now says a rule every script needs is stated once per shell and held equal by a drift test. DOC_INDEX.md has a
row for `scripts/python-resolve.{sh,ps1}`. PB3197 is landed with `closes_rows: []`, a behavior-neutral restructuring
with no inventory row.

**Self-review.** The twin first caught only `CommandNotFoundException`. A candidate that exists but cannot be started
therefore ended the PowerShell gate NOT RUN, while the bash probe tries the next candidate. The twin now answers "no"
for any candidate it cannot start, the same as the bash probe; re-probed.

**Neutrality and gate.** `python scripts/arch/compare_oracle.py` printed `=== ARCH-ORACLE: IDENTICAL — 8025 cases
(baseline 16b82f639c26, new 6e45bc04acb7) ===`. `python scripts/semgrep/verify.py` printed PASS with no count up. The
gate printed `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN ===` at 6e45bc04a (run
20261016T205034Z-70ab7d) and again at 2b47742fd (run 20261016T210148Z-9500b6). Its leg-1 Unit trx has all three
`PythonInterpreterDriftTests` facts Passed.

