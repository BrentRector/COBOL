## Entry 313 — Implicit CLOSE at run-unit termination → RL208A baselined (NIST 350→351)

First of the parallel-diagnosed file-I/O backlog (a 9-agent diagnosis workflow root-caused the remaining RL/IC
tests; this is the first fix). **RL208A** (RL206A→RL207A→RL208A delete/update chain) failed 2/11: REL-TEST-012
COMPUTED 400 update#=00 vs CORRECT 395, and 100 vs 105 updated. Root cause is NOT the long-suspected
"Rewrite-pads-varying" latent bug — it is that **the generated program never executes an implicit CLOSE at
run-unit termination**, so I-O changes to a file the COBOL program leaves open at STOP RUN are silently
discarded. RL207A OPENs RL-FD1 I-O and REWRITEs records keyed 1/11/21/31/32 with UPDATE-NUMBER=98, then STOP
RUNs without an explicit CLOSE; the REWRITEs updated the in-memory slots (RL207A's own re-reads PASS 20/20) but
were never persisted, so the downstream RL208A consumer saw the pre-update file. ISO §14.6.11 / §14.4: at normal
run-unit termination the runtime executes an implicit CLOSE (no phrases) for every file still in the open mode.
`STOP RUN` in our model just exits the paragraph-dispatch loop (returns from `Entry`); `CloseAll` was never
called. Fix: in `CilEmitter` the `Self.Entry` runtime-call branch (Main-only) now appends a
`FileRuntime.CloseAll()` after `Entry()` returns — disposing each handler, which persists it (an
already-explicitly-closed handler is a guarded no-op, so no double-write). Scoped to Main (the run unit), so a
CALLed subprogram's EXIT PROGRAM/GOBACK is unaffected. The diagnosis (empirically verified: an OPEN-I-O +
REWRITE program persists WITH an explicit CLOSE, loses the change WITHOUT) also corrected the resume-prompt's
wrong producer — RL208A's producer is **RL206A** (500 varying records), not RL212A (a SEQUENTIAL test leaving
416). RL208A baselined (011 OF 011), placed after RL207A; the following RL210A/211A/209A re-create TF021 so the
deletes don't leak. Guard ALL GREEN: **1040 unit / 347 integration / 351 NIST** (RL 28→29), 0 regressions —
confirming the broad implicit-close change persists nothing harmful for the ~70 STOP-RUN programs (every
already-explicitly-closing producer is unchanged; each test runs in its own process + start-clean output dir).

