## Entry 512 — 2026-06-09 13:35 PDT — Fix: a static field in BoundTreeValidator was a compiler data race → spurious CBL1603 (the recurring CI flake)

CI went red on a **docs-only** commit (`c9f0065`) — the Linux Guard job failed on
`SpecFixTests.ReadPrevious_AfterStartEqual_ReturnsTheEqualKey`, while the Windows job passed and the immediately
preceding docs-only commit was green. No code changed between them, so this was non-deterministic, not a regression —
the "CI breaking again" the owner flagged. The base-class comment already named the two recurring flakes:
`FileIO_Start_PositionsForReadNext` and `ReadPrevious_AfterStartEqual` — **both are `START … KEY IS …` programs**.

**Root cause (a real compiler thread-safety bug, not a test problem).** `BoundTreeValidator` held the program's
`SemanticModel` in a **`private static SemanticModel? _model`** field, assigned at the top of `Validate` and nulled in
a `finally`. The integration suite runs xUnit test classes in parallel (`MaxParallelThreads` = CPU count, no opt-out)
and each test compiles **in-process**, so multiple compilations overlap and share that one static field. When compile
A is mid-walk and compile B assigns/teardown-nulls `_model`, A's `ValidateStart` runs
`_model.ResolveKeyOfReference(start.File, keyId.Symbol)` against the **wrong/null** model → it can't resolve the
START key → A reports a **spurious `CBL1603` ("START KEY operand not a record key of file")** → A's compile fails →
`Assert.True(ok, stderr)` fails. `ResolveKeyOfReference` (→ CBL1603, in `ValidateStart`) is the **only**
compile-gating path that reads `_model`, and `_model` is the **only** non-`readonly` static field in the whole
compile path — which is exactly why the flake is specific to START-with-KEY programs. Because the validator is
validation-only, a clobber can **only** produce a spurious diagnostic (compile fail), never wrong runtime output.

**Reproduced first, on Linux (WSL), against the buggy build** — a new concurrency-stress test
(`ConcurrentCompilationTests`) fans 64 threads × 40 iterations over **three distinct** START-with-KEY programs
(distinct so a clobbered `_model` points at a *different* program's model — the cross-model race, not merely the
`finally`-null path) and asserts every compile succeeds with zero diagnostics. Pre-fix it failed **every run** —
e.g. **58 of 2560** compiles with `error CBL1603` across all three programs — confirming the chain end-to-end and
confirming it manifests as a **compile failure** (the one fact the CI log never captured at `--verbosity quiet`).

**The fix (root-cause, singular-pattern).** `BoundTreeValidator` is now an instance class: the public
`static Validate(program, diagnostics, model)` entry constructs `new BoundTreeValidator(model)` (model in a
`readonly` instance field) and runs the walk; every walker/validator method became an instance method; the
`try/finally` reset is gone (no shared state to reset). Per-instance state is inherently race-free. No validation
logic changed — only where the model lives — so single-threaded behavior is byte-identical. Deliberately NOT masked
by reducing test parallelism: a thread-unsafe compiler is a real bug worth fixing regardless of CI.

**Verification.** Post-fix the stress test is solid: **10/10 runs, 25,600 compiles, 0 failures** on Linux. The full
integration suite passes 3/3 under Linux parallel load (the previously-flaky START tests included). Windows guard
ALL GREEN — NIST 364 MATCH / 0 regressions, 1204 unit, 536 integration (the prior 535 + the new
`ConcurrentCompilationTests`). The thread-safety regression test ships in this commit.

