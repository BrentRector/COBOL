## Entry 1770 — 2026-09-28 20:57 PDT — PB1708 pivot design revised: only the driver can make a host filter, and nothing deletes an interface its callers still read

The adversarial review of the "order, don't skip" design (Entry 1769) returned 2 BLOCKING and 9 NON_BLOCKING
findings. This revision resolves all eleven in `DESIGN-test-build-ci.md` sections 3.13 and 3.14, and three of them
were settled by new measurements rather than argument.

**Blocking 1: the environment alone made a test host filter.** `GateTestFramework` filtered whenever two variables
were set, and only the gate driver checked the population. A developer who exported them by hand to reproduce a leg
would later get a battery that ran a third of Conformance and printed `Passed!`. The review proposed throwing at
framework construction on any partial environment. The extended `b3` probe measured that this does NOT work: xunit
catches a constructor's exception and silently falls back to its default framework, so every case ran, `Passed!`,
exit 0. An executor that throws is a "Catastrophic failure" with no verdict line and no trx result. What does work is
reporting every case as xunit's own `ExecutionErrorTestCase` carrying the cause: every case Failed, a `Failed!`
line, exit 1. The design is now:
- a three-variable handshake (`COBOLNET_GATE_PLAN`, `_LEG` and `_PLAN_SHA256`, the last matching the file);
- every other combination turns the whole run red;
- every other `dotnet test` caller scrubs the variables, with a drift test to keep it so;
- one population check, `scripts/test_population.py`, which the battery and CI now run as well as the gate.

**Blocking 2: M11 deleted an output its callers still read.** M11 removed `impacted_tests.py`'s filter line and
`--plus`, but the practices, the three briefs, the gate skill and `check_practices.py` were rewired only in M13.
M11 now deletes only the NARROWING, so its filter line is always the whole assembly. That closes PB1712 at M11,
while M13 deletes the line, `--plus` and `-Filter` together with every caller, named file by file. Until M11 lands,
no map may be recorded into the shared store. Main had meanwhile retired per-commit maps (PB1709; merged here).

**What the new measurements showed.**
- `b8` (one build, Conformance and Unit, full unfiltered runs): `--list-tests` equals the trx's TEST DEFINITIONS as
  a multiset (9,317 and 29,703), but not its RESULT names. xunit lists a theory whose data it cannot serialize as one
  case and reports a result per row, so Unit's results carry 33 names the listing never printed. The population
  check therefore keys on definitions. 404 Unit names embed the worktree's absolute path, and a placeholder cannot
  normalize them, because xunit truncates the argument at a fixed length.
- `b7` (battery #87's durations, today's manifests): appending one golden to the 85 manifest renames 1,398
  partitioned corpus rows (450 test-seconds), which pushes leg 1's serial floor from 46 s to 208 s under raw-name
  keys. The plan now keys on `NameKey` (partition suffix removed) and caps each collection's budgeted leg-1 time at
  15 s, which gives 2,740 cases and a 15.2 s floor. It also budgets genuinely unknown cases separately.
- `b3` (extended): a dropped case leaves no trace in the trx. Two legs' definitions reunite into the listing, both
  rows of a truncated-name theory included. An empty leg prints no verdict line and exits 0, so the driver skips an
  assembly for a leg its plan leaves empty. A skipped fact is NotExecuted, and the verdict now counts it separately.

**The rest.**
- M6 no longer moves predicates one character in. The decompiled ANTLR 4.13.1 simulator never stores a DFA edge
  through a predicate, so that would leave a lock on every D-, comma- and sign-initial token. The invariant is now
  zero ATN simulation after warm-up, counted by a simulator subclass.
- The gate gets a required `-Mode lander|implementer`.
- Each gate holds a per-worktree lock from before the build, writes into a fresh run directory, and has every leg
  record its binaries' hashes.
- The cap serves waiters FIFO by ticket and is taken before the build.
- M14 is new: the population tool and its battery and CI callers. CI's inline `grep -c` count block goes.

Two product defects found by the design still need kb/Work notes: M6's lexer lock and M7's 64 M-element table-value
fill. Their ids are allocated by the orchestrator (agents never allocate ids), which is an open item in the report.
The design branch's own entry, numbered 1765 there, is renumbered 1768 here, above main's 1765–1767. The branch's
`__pycache__` litter is removed.

