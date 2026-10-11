## Entry 989 — 2026-07-22 18:03 PDT — §24 V52/V53: the run-unit termination epilogue is runtime-owned (RunMain), and the abnormal surface now covers the CALL family too

The V47 pattern-completion. V47's review had surfaced two same-class siblings — run-unit-termination observables
emitted in the generated `Main` gated on a compile-time scan of the MAIN compilation group, so each was blind to a
separately-compiled CALLed module. Fixed together by moving the epilogue into the runtime's `RunMain` boundary.

**V52** — the §14.6.12 abnormal-termination surface (the `abnormal run-unit termination: <msg>` diagnostic +
`Environment.ExitCode = 1`) was emitted only under `if (_ecState.Active)`. **V53** — the §14.6.11(2) implicit CLOSE
of "all open files in the run unit" was emitted only under `if (anyFiles)`. So a `CobolFatalException` from a source
the main group's EC scan can't see (EC-OO-UNIVERSAL, a NULL BASED deref, a checking-on sub) escaped an EC-free main
as a raw CLR crash; and a file-less main that CALLed a file-writing sibling never closed the sibling's connectors.

**Fix (one coupled move).** `ProgramTable.RunMain` now wraps the main activation in an inner
`catch (CobolFatalException)` + `finally { _owner.Files.CloseAll(); }`. The generated `Main` reduces to the
unconditional `try { RunMain(path); } catch (StopRun) { }` — the `if (_ecState.Active)` fatal-catch and the
`if (anyFiles) finally { CloseAll }` are gone. Byte-neutral for EC-free/file-free programs (§18.16); the sole
tracked byte-golden that changed is `char_seq_file.85` (one line, the removed finally). Removed the orphaned
`RuntimeApi.FileCloseAll` + swept the stale comments. `StopRun` still propagates through RunMain's finally;
`CloseAll` idempotent so the `RunUnit.Run` embedding double is harmless; `FileInit` stays gated on `anyFiles`
(the FileRegistry is fresh per RunUnit — verified safe).

**Adversarial review (`wf_b799fd6d-dd4`) earned its keep — a scope gap folded in-batch (no-deferral).** Ordering and
byte-impact lenses SOUND; the spec-fidelity lens + the completeness critic both build-verified a MAJOR: the fix
caught only `CobolFatalException`, so the **`CobolCallException` family** (EC-PROGRAM-NOT-FOUND / -RECURSIVE-CALL /
-CANCEL-ACTIVE / EC-FUNCTION-NOT-FOUND — `: Exception`, not a `CobolFatalException` subclass) still escaped as a raw
CLR crash (exit 127) — the *exact* V52 pattern, incl. V52's own cross-assembly sibling-not-found case. Folded the
completion in: RunMain now catches BOTH families via a shared `AbnormalTermination` helper (the settled §18.16 surface
— stderr diagnostic + exit 1; §14.9.4.4 GR3h → §14.6.13.1.3 #8). The critic also swept every other §14.6.11 START/END
step CLEAR (external-describe per-activation, MODULE register unconditional, the EC/external/switch stores fresh per
RunUnit, locale not-emitted, APPLY COMMIT vacuous, ALLOCATE/object GC) — no further same-class sibling.

**Tests (`RunUnitTerminationTests`, new):** EC-free-main null-deref → surface + exit 1 (was a raw crash);
cross-assembly sub-fatal → same; CALL-not-found → same; file-less main CALLs a file-writing sub → the sub's file is
flushed/closed; abnormal-path CLOSE (fatal after an unclosed WRITE) → exit 1 AND the file flushed. The pre-existing
`Call_AbsentSiblingModule_StillNotFound` now sees the clean surface (still green). Gate: greenfield Conformance 3860/3860
0-reg · characterization 33/33 · GnuCOBOL differential 0-reg (report byte-identical). This completes the V47 pattern
(exit code, abnormal surface, implicit CLOSE — all now run-unit-scoped, runtime-side).

