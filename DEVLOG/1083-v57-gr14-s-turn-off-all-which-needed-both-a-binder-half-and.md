## Entry 1083 — 2026-07-28 15:48 PDT — V57: GR14's TURN OFF ALL, which needed BOTH a binder half and a runtime half

Step 3 of the EC super-batch, on top of the flag restructure (entry 1082). The finding called this a
binder-only fix; it is not, and the experiment below is what settles it.

**The rule.** §14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the END of
imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit POP ALL …". imp-2/3/4
(WHEN / OTHER / COMMON) and imp-5 (FINALLY) all execute between those two points, so no checking is enabled
inside them, and §14.6.13.1.1 then means nothing is raised there. GR21 is NOT the governing rule and the binder
used to cite it: it says only that an exception raised in imp-2..5 "will not cause transfer of control to any of
these imperative-statements" — re-entry, not checking. GR22 governs what survives AFTER the PERFORM.

**BOTH HALVES ARE LOAD-BEARING — proved by running each alone.**
  · binder half only → still aborts. The ambient gates are set by the guard around the RAISING statement, and a
    handler is dispatched from inside that guard before its `finally` clears them.
  · runtime half only → still aborts. The handler's own statement guard RE-ENABLES the flag right after the
    PUSH ALL cleared it, because the body was still bound under the enclosing checking state.
  · both → HANDLER-DONE / FINALLY-DONE / AFTER.
Neither half is redundant, which is the answer to "is the binder change now dead weight after the restructure".

**Binder half:** `TurnState.WithAllDisabledFrom(handlerLine)` splices an `EC-ALL OFF` floor at the first
handler's line (the END-PERFORM line when there is no WHEN) and imp-2..5 bind under it, restored to the base
after imp-5 so GR22 governs. The floor sits BEFORE any real handler-local `>>TURN`, so a directive written
inside a handler still wins — GR14 disables, it does not freeze.

**Runtime half:** `__RunF3` wraps imp-2/3/4 in `PushAllCheckingOff()` / `finally PopAllChecking()`, and the
inline FINALLY body gets the same. Both only exist in a unit that has an F3 PERFORM, so the byte-stability
invariant holds for EC-free units.

**Two existing tests were asserting pre-GR14 behaviour and had to change — with the spec, not to fit the code.**
`Gr21_ReRaiseInHandler_NotReCaught_FallsToFatal` (program and method variants) overflowed an ADD inside a
handler and expected the implicit EC-SIZE-TRUNCATION. Under GR14 that ADD raises NOTHING, so the tests were
encoding the leak. First rewrite used an explicit `RAISE` in the handler — rejected by our own compiler with
COBOLNET1611, and correctly: §14.9.29.3 SR4, "Within an exception-checking PERFORM statement, the RAISE
statement shall not be specified in any imperative statement other than imperative-statement-1". The landed
form re-enables checking with a handler-local `>>TURN EC-SIZE-TRUNCATION CHECKING ON`, which is the legal way to
raise inside a handler AND doubles as the proof that a handler-local directive beats the GR14 floor. Both tests
keep testing GR21; only the mechanism changed.

**Golden** `2023/f3_perform_handler_turn_off_all`, registered same commit. Both the WHEN body and the FINALLY
body perform an out-of-range ref-mod that GR14 says cannot raise; expected HANDLER-DONE / FINALLY-DONE / AFTER.
⚠ Edition: I first wrote it for 2002 and the compiler rejected it — the Format-3 PERFORM is a 2023 introduction
(Annex E item 36, "an exception checking variant of this statement has been added"), so it lives in the 2023
corpus.

**Also corrected en route:** I read the post-handler abnormal termination as a bug before finding that
`PerformFormat3BehaviorTests` documents it as correct — a fatal EC's WHEN runs, then §14.6.13.1.3 #5 terminates
without a RESUME. The golden uses the NONFATAL EC-USER-DEMO in imp-1 for that reason.

**Gate:** characterization 33/33 · Conformance CorpusRunner+PerformFormat3 402/402 · Unit
Exception+PerformFormat3+Turn 71/71.

