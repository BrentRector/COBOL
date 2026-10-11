## Entry 318 — CALLed subprogram registers its own files at Entry → IC114A baselined (NIST 355→356)

Second of the parallel-implementation backlog fixes. **IC114A** (1 FAIL*) holds the run-unit main IC114A + the
CALLed subprogram IC115A, which owns file SQ-FS3 and creates/reads it across five CALLs. A CALLed subprogram's
internal file connectors were NEVER registered: the file-handler registration IR was emitted only into each
program's `Main`, but the assembly entry point is the run-unit main; a CALLed program is entered via its `Entry`
method, so IC115A.Main is dead code and IC115A.Entry never registered SQ-FS3 → every I/O no-opped (OPEN 35,
WRITE/READ/CLOSE 42), AT END never fired, the read loop overran (“MORE THAN 649 RECORDS”). ISO §14.6: a
called program's file connectors are established when it is activated. Fix: the per-file registration is split
out of `Binder.CreateEntryPoint`’s Main into a new parameterless `RegisterFiles` IR method, called once per
activation at the top of `Entry` (guarded by a per-program `_filesRegistered` static, cleared on the
INITIAL/post-CANCEL re-init path so CANCEL+re-CALL re-registers); `FileRuntime.Init` (the manager wipe) stays
ONLY in the run-unit main’s Main so a subprogram never disposes the caller’s open files. `RegisterFileHandlerWithOrg`
also became register-IF-ABSENT, so a subprogram that SELECTs a name the caller already opened (the IC suite’s
shared PRINT-FILE) keeps the caller’s open connector instead of replacing it. IC114A → 003 OF 003, 0 FAIL*,
baselined (after IC112A). (The agent dropped the secondary column-7 ‘X’-card exclusion — it regressed IC112A
— so the XFILE-DUMP diagnostic section’s NUL bytes remain in the report; deterministic, in both baseline and
output, so the guard MATCHes; the three LINK-TESTs pass correctly.) Guard ALL GREEN: **1040 unit / 347
integration / 356 NIST** (IC 19→20), 0 regressions — incl. the CANCEL tests (IC222A–228A/237A) and the
shared-PRINT-FILE callee tests (IC101A/103A).

