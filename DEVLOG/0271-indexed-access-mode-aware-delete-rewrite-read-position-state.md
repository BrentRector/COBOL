## Entry 271 — Indexed access-mode-aware DELETE/REWRITE + read-position state (43/46) → IX103A/106A/119A/203A

`IndexedFileHandler` had no access-mode awareness — DELETE/REWRITE always operated on `_currentKey` and
returned 43 only when it was null. Correct for ACCESS SEQUENTIAL, wrong for RANDOM/DYNAMIC, and missing the
read-position rules.

- **Access mode conveyed to the handler.** New `FileRuntime.SetIndexedAccess(name, sequential)` (mirrors
  `SetRelativeAccess`), emitted by the Binder for every INDEXED file (`sequential = AccessMode is null or
  "SEQUENTIAL"`; SEQUENTIAL is the indexed default). NB the recurring gotcha — a new emitted runtime call
  needs an explicit `CilEmitter.EmitRuntimeCall` case or it NOPs with its args on the stack
  (`InvalidProgramException` at Main); added the `FileRuntime.SetIndexedAccess` dispatch case.
- **ACCESS SEQUENTIAL DELETE/REWRITE** now act on the last-read record and require the immediately
  preceding operation to have been a successful READ — status 43 if not (ISO §9.1.13.6). Added
  `_prevOpWasSuccessfulRead` (true only right after a successful READ; cleared by WRITE/REWRITE/DELETE/START),
  so a DELETE/REWRITE after a REWRITE/WRITE — not just after a never-read file — gives 43 (IX119A; the old
  `_currentKey == null` check missed it). REWRITE still rejects a primary-key change with 21.
- **ACCESS RANDOM/DYNAMIC DELETE/REWRITE** operate on the record identified by the primary key with no prior
  read; a missing record is 23 (invalid key) rather than 43. This fixed the IX103A/IX203A delete chains and
  IX106A's REWRITE.
- **READ NEXT after an at-end READ → 46** (ISO §14.9.30 GR) via `_lastReadUnsuccessful`, the indexed analog
  of DEVLOG 267 (IX109A's READ-46 half). START/READ-by-key reposition and clear it.

A note on test isolation surfaced while validating: IX103A/106A/203A are CLEAN only with their producer
(the IX101A/IX201A bundles, which create TF024) ahead of them — my first standalone runs showed spurious
extra failures because repeated runs had depleted the shared TF024 (IX103A is a delete test). IX110A was
baselined then reverted: it OPENs TF024 **I-O** expecting it pre-populated, but the now-baselined IX103A
delete-test depletes TF024 before it in the guard (a non-baselined test recreated it between them during the
survey) — genuinely order-fragile with the shared file, so it is left un-baselined.

IX103A/106A/119A/203A → CLEAN, baselined (numeric order, producers ahead). Guard ALL GREEN: 1000 unit /
347 integration / **251 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 21 IX), 0 regressions.
Remaining IX FAIL*: sequential WRITE ascending-key 21 (IX109A's other half / IX112A), variable-length
indexed records (IX105A), and IX218A (SELECT OPTIONAL indexed absent-file READ → 10, blocked by the same
shared-TF isolation limitation — its optional file XXXXP024 → TF024 is present once a producer has run).

