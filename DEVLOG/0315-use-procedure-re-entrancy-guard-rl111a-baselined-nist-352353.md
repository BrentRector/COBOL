## Entry 315 — USE-procedure re-entrancy guard → RL111A baselined (NIST 352→353)

**RL111A** crashed with a `Stack overflow.` — an infinite `Para_D-CLOSE-FILES ↔ Dispatch` recursion (the
print-file had already flushed a clean `022 OF 022 … NO TEST(S) FAILED`, but the *process* still crashed, so it
was a crash not a pass). The crash (from the parallel diagnosis): RL111A's `USE AFTER STANDARD ERROR` handler
for RL-FS3, while executing, does `CLOSE RL-FS3` — but the file is already closed, so CLOSE returns status 42,
and `FileRuntime.ShouldRunUseDeclarative(42)` returned true → the SAME declarative re-fired → its `CLOSE`
returned 42 → re-fired → ∞ (7390× to overflow). This violates ISO §14.9.49.4 GR2: a USE procedure must not be
re-invoked while it is still active (else EC-FLOW-USE). Fix (the diagnosis's lowest-risk option — additive, no
change to the common path): a re-entrancy guard. `FileRuntime._activeUseDeclaratives` tracks files whose USE
procedure is currently executing; `EnterUseDeclarative`/`ExitUseDeclarative` (new, emitted by
`FileIoLowerer.EmitUseDeclarative` bracketing the declarative PERFORM, with their CilEmitter dispatch cases)
set/clear it; `ShouldRunUseDeclarative` returns false when the file is already active. So a declarative whose
own body does I/O on its file cannot recurse into itself. (The deeper STOP-RUN-only-unwinds-one-Dispatch-frame
issue — FIX A in the diagnosis, making STOP RUN throw — was NOT taken: it touches every program's termination
convention; the re-entrancy guard alone clears RL111A and the report was already clean.) RL111A baselined
(022 OF 022), self-contained, placed after RL205A. Guard ALL GREEN: **1040 unit / 347 integration / 353 NIST**
(RL 30→31), 0 regressions — the re-entrancy guard is inert for every non-recursive USE-declarative baseline
(SQ/RL/IX error-path tests).

