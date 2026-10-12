## Entry 234 — CANCEL returns a program to its initial state on the next CALL (IC203A → CLEAN)

IC203A (13 FAIL*, "SET TO INITIAL STATE … DNn INCORRECT") tests CANCEL. The bundled subprogram
IC204A keeps a call counter `WS1 PIC 99 VALUE ZERO` and a first-call flag `WS2 PIC X(5) VALUE
"FIRST"`; after `CANCEL "IC204A"` the next CALL must find them reset (WS1→0, WS2→"FIRST"). We never
reset a canceled program's state, so the counter kept accumulating (DN1=4 instead of 1; DN2="NO"
instead of "YES").

Implemented CANCEL per ISO §14.9.5 GR3 ("if the program … is subsequently called …, that program is
in its initial state", §14.6.2.3.2). Two parts:

1. **Return-to-initial-state on re-CALL.** Refactored the emitter so the WORKING-STORAGE/VALUE/ALTER/
   LOCAL-STORAGE setup that used to live inline in `.cctor` is now a reusable static
   `InitializeState()` method; `.cctor` just calls it (first activation). `CobolProgramRegistry`
   gained a `_needsReinit` set: `Cancel(name)` adds the name (and still drops the cached entry);
   `ConsumeReinitFlag(name)` returns+clears it. The emitted `Entry` now, for a non-INITIAL program,
   calls `ConsumeReinitFlag(<program-id>)` and re-runs `InitializeState()` when set — so a normal CALL
   keeps last-used state (static items persist, §14.6.2.3.2) while the first CALL after a CANCEL gets
   a fresh initial state. INITIAL programs call `InitializeState()` unconditionally as before (this
   also fixes a latent gap: the old `ResetState` re-allocated zeroed storage but never re-applied
   VALUE clauses — `InitializeState` does).

2. **Dynamic CANCEL** (`CANCEL identifier`, §14.9.5 GR1a). Previously `BindCancel` stored the
   *identifier's name* ("ID1") rather than its runtime content, so `CANCEL ID1` canceled a non-existent
   program "ID1". `BoundCancelStatement` now carries `BoundCancelTarget(Name, IsDynamic)`;
   `IrCancelProgram` carries `IsDynamic` + an `IrLocation`; lowering resolves the data item; the
   emitter reads the program-name at runtime via `PicRuntime.GetDisplayString` (mirroring dynamic
   CALL). Literal `CANCEL "lit"` is unchanged.

EXTERNAL data is preserved across CANCEL (§14.9.5 GR8) for free: EXTERNAL items can't carry VALUE and
their storage is the shared `ExternalStorage` array, untouched by re-allocating WORKING-STORAGE.

Verified: minimal repro (literal + dynamic cancel both reset the counter to 1 while normal calls
accumulate) and IC203A 13→0 FAIL* ("021 OF 021 TESTS WERE EXECUTED SUCCESSFULLY"). Baselined +
added to guard (NIST baselines now 151 = 94 NC + 42 IF + 12 SM + 3 IC). Full guard ALL GREEN — the
`.cctor`/Entry refactor touches every program's codegen with no regression (1000 / 345 / 151).

Documented limitations (not exercised by IC203A, not yet implemented): §14.9.5 GR4 (canceling a
program does not yet cascade to its *contained* programs), GR9 (no implicit CLOSE of the canceled
program's open files — ties into the file-I/O subsystem), GR5 (no EC-PROGRAM-CANCEL-ACTIVE check for
canceling an active program).

