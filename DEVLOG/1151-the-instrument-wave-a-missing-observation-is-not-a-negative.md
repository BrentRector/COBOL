## Entry 1151 — 2026-08-03 13:25 PDT — The instrument wave: a missing observation is not a negative observation (§11 A12·A12b·A12c·A12d·A12e)

§5b makes closing the instrument defects step 1 on the road to zero GAP, and the reason held up: every claim the
campaign will make is validated by these harnesses, and each of them could report something the run had never
observed. **No compiler behaviour changed in this wave.** Every change is to a gate, a guard, or a test harness.

**ONE ROOT CAUSE, FIVE HARNESSES.** §0 had already named it — *a missing observation is being read as a negative
observation* — but it was carried as three separate backlog rows, which is why it kept recurring in new places.
It is one rule, and it now lives in one place: `DESIGN-test-build-ci.md` §3.10, the **VERDICT-EVIDENCE
INVARIANT**. A verdict about the compiler is produced ONLY from an observation actually made — ACCEPT needs the
artifact it claims to have produced, REJECT needs the reason it claims to have found, MATCH/DIFF needs a run that
finished. Anything else is a `NO-VERDICT`: never MATCH, never REGRESSION, and loud. Two corollaries: every
iterating harness asserts its results are a **PARTITION of its declared population**, and the expected verdict is
read from a **committed manifest**, never from a number a human remembers.

**PER HARNESS.**

- **A12c — the false green.** `guard-fast` computed its verdict from the REGRESSION count alone, so a program
  that vanished lowered MATCH and still printed `=== ALL GREEN ===` (352 against a 353 baseline; `SQ135A`
  produced no line at all). `scripts/guard-nist-audit.sh` — consumed by BOTH guards so the rule is written once —
  asserts one verdict per declared program, no strays, no duplicates, and that each verdict CLASS equals what
  `tests/nist/corpus.tsv` predicts, plus a manifest↔disk cross-check so a golden that VANISHES cannot quietly
  turn its program into an expected `NO BASELINE`. The expectation is DERIVED (`divergent`→LEGACY DIVERGENT,
  `golden==none`→NO BASELINE, else MATCH) and reproduces **353/12/11 over the 376-program population exactly**.
- **A12b — a compile with no diagnostic.** Compiler output went to `/dev/null` and the verdict was inferred from
  whether a `.dll` existed, so a transient and a real syntax error reported identically while the one thing that
  would settle it was discarded. Per-test `.compile.log` + `.compile.rc` are kept; FAILED requires a non-zero rc
  AND diagnostic text.
- **A12 — a timeout read as a value mismatch.** The mechanism was never the timeout VALUE: on expiry the harness
  killed the process and returned its PARTIAL OR EMPTY stdout, which the caller compared against a golden.
- **A12e — a rejection with no reason.** `we_ok = (returncode == 0)` alone, so a process killed under load scored
  `WE_REJECT_THEY_ACCEPT` — the zero-tolerance direction — with `ourCodes: []` and an empty error string.
  Rejection now requires a diagnostic; evidence-free compiles retry once, then bucket as
  `NO_COMPILER_EVIDENCE`, counted as a HARNESS failure and NAMED for re-run.
- **A12d — the whole-suite serialization**, replaced by the declared chain graph (below).

**FOUR THINGS THAT GENERALISE PAST THIS WAVE.**

1. **The same fifteen lines existed SEVEN times.** "Start `dotnet`, wait N seconds, return whatever came back"
   lived in `CutRunner.RunExit`, `AcceptDifferentialTests.AcceptRun`, `CobolNetTestBase.CompileAndRun`, three
   copies in the legacy `EndToEndTestBase`, and `BinderDecompositionTests` — the last reading both streams
   synchronously (a full-pipe deadlock) and then reading `ExitCode` without checking `WaitForExit`'s result.
   `EndToEndTestBase`'s own comment already named the symptom ("mistaken for a hang … the source of the transient
   file-I/O guard flakes, DEVLOG 352/355") and answered it by RAISING the timeout while still returning the
   partial output. All seven now route through `tests/_shared/ProcessObservation.cs`, which raises rather than
   fabricating a value; `ProcessObservationDriftTests` found the seventh copy on its first run.
2. **A hand-maintained list lost to the data it approximated — twice in one session.** `guard-fast`'s "these six
   suites run serially" grouping became the connected components of `corpus.tsv`'s declared `chain-preds`
   (longest serial group 40 → 9), and doing so corrected the hand list, which over-grouped `SQ204A` — a program
   that `OPEN OUTPUT`s its own file and needs no predecessor. Separately, this audit's own drift guard began as a
   hand-written list of `WaitForExit(` numeric prefixes that would have blessed `WaitForExit(45000)` silently; it
   is a regex now.
3. **Prove the instrument can fail, AND that it fails for the RIGHT REASON.** The audit's `--self-test` runs
   eleven synthetic runs, each built to break exactly one check — and the first draft had two "passing" because
   an unrelated path bug had already reddened the control. Pinning each case to its own expected finding is what
   caught that. `gnucobol_differential.py` likewise runs an **evidence control** before scoring anything (one
   program that must be accepted, one that must be rejected *with a reason*) and aborts if this build cannot be
   told apart — otherwise `has_evidence` would reclassify every genuine rejection as a lost result and the
   differential would go quietly, uselessly clean. Verified against two stub compilers.
4. **A correct check that is slow gets deleted later for being slow.** The audit's first draft looped in bash and
   spawned two subprocesses per program: >2 minutes over the real population. Rewritten as one `awk` pass: 0.9 s.

**⚠ AND A CORRECTION I HAD TO MAKE MID-SESSION, in the owner's direction.** My first response to the contention
was to CAP the guard's compile fan-out to `nproc/2`. That is backwards: it pays a throughput tax on every run to
protect against something the evidence rules now DETECT rather than mis-score. Reverted — full fan-out is kept,
and only the LOST observations are re-taken, serially, in their own groups. The general form is the owner's
standing rule: **run as concurrently as correctness allows, and reduce the WORK rather than the parallelism.**
`scripts/battery.sh` now runs Conformance ∥ Unit ∥ Characterization concurrently (§0's "one leg at a time" is
specifically about a REBUILDING leg overlapping a `--no-build` one, not two `--no-build` assemblies), and
`GUARD_TESTS` makes the serial guard subsettable.

**⚠ WHAT IS NOT CLOSED, stated plainly.** §11 **A12d keeps its measurement half open.** I collected exactly ONE
clean post-change guard run (353 MATCH · 0 REGRESSION · AUDIT CLEAN 376/376 · 0 harness events) and then
destroyed the second by killing the measurement loop on a log line that arrives *before* the run ends. One run
says nothing about variance, and the pre-change runs in the same session showed the variance is real. The
replacement is better than the one-off anyway — every ordinary run now emits the audit line and appends any
retry or non-observation to `COBOLNET_HARNESS_LOG`, so the distribution accrues passively — but the row stays
open until several ordinary gates have accrued clean. §11 A13(b), the per-class duration profile of the
Conformance leg, is likewise UNMEASURED: the claim that a 349-row `[Theory]` runs its rows serially on one
thread follows from xUnit 2.9.2's documented per-collection parallelism, and a plausible reading is not an
observation.

**⚠ AND A WASTE THAT WAS MINE, not the harness's.** I opened the session by running the FULL Conformance suite
(13 m 54 s) to "confirm the battery green", having *already* verified mechanically that the only two commits
since the recorded baseline were doc-only and could not reach the compiler. The tiered-gate rule
(`feedback_tiered_gates`) and the owner's standing instruction both say not to. Recorded because the instrument
half of this session is about not believing unearned evidence, and re-measuring an unchanged tree is the mirror
of that: paying for evidence already held.

**⭐ AND THE CONCURRENCY QUESTION TURNED UP SOMETHING LARGER, now MEASURED rather than asserted.**
`scripts/profile-test-parallelism.py` reads the per-class durations out of the `--logger trx` that
`battery.sh` already emits, so the profile costs nothing extra. xUnit 2.9.2 parallelizes per test COLLECTION
and by default each test CLASS is one collection — so every row of a `[Theory]` runs **serially on one
thread**, and nothing in `dotnet test`'s output says so. The greenfield **UNIT** assembly: **3,634 tests
across 63 classes in 210 s wall for 279 s of total test time — 1.3× average concurrency on a 32-core box** —
and ONE class, `StorageFormEquivalenceTests` (6 tests, 210 s), *is* that wall clock while 31 cores idle.
Splitting it into six collections would take the leg from ~210 s to ~35 s. And the **CONFORMANCE** assembly —
the ~13-minute pole of the whole battery — is worse: **4,180 tests across 109 classes in 783 s wall for
2,583 s of test time, 3.3×**, of which **`VersionMatrixTests` alone is 2,015 tests running serially for
780.2 s**. Essentially the entire leg is one class on one thread. ⚠ **I had written down
`NistDifferentialTests` as "the obvious suspect" before measuring; it is the SECOND one (349 tests, 317.3 s).**
The arithmetic was plausible and wrong — which is the entire reason §11 requires a measurement before the
work is scheduled. The split itself is deliberately NOT in this wave: it is a test-organisation refactor with
its own re-baselining risk, and it deserves its own change set and its own before/after profile. §11 A13(b).

**⚠ AN HONEST NEGATIVE, because a wave that only reports its wins is not evidence.** The chain-graph grouping did
NOT make the guard faster on this machine: the NIST phase measured **564 s before and 598 s after**. The leg is
THROUGHPUT-bound on `dotnet` cold-start (the compile phase alone is ~150 s, and observed effective concurrency
was ~7 of 32 lanes), not TAIL-bound, so cutting the longest serial group from 40 to 9 buys nothing here. It
should matter on Linux CI, where process spawn is far cheaper. The change is kept for CORRECTNESS and for
retiring a hand-maintained list — not for speed. The real lever is a persistent run-host, now recorded as such.

