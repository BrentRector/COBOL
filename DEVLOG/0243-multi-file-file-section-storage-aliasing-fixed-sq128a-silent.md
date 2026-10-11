## Entry 243 — Multi-file FILE SECTION storage aliasing fixed (SQ128A; silent data corruption)

SQ128A (write 750 records to two sequential files, close, reopen, verify the first record of each)
was FAIL* on "VERIFY FILE SQ-FS1". Inspecting the physical files revealed the smoking gun:
**SQ-FS1's file (`tfil1.txt`) was byte-identical to SQ-FS2's file (`sq-fs2.txt`) — both held SQ-FS2's
data.** The write loop is `MOVE info(1) TO FS1-rec / MOVE info(2) TO FS2-rec / WRITE FS1-rec /
WRITE FS2-rec`; since `WRITE FS1-rec` emitted SQ-FS2's content, the two records had to be sharing
storage, so the second MOVE clobbered the first before either WRITE.

Root cause in `StorageLayoutComputer`: the FILE SECTION layout loop laid out **every** 01-level
record at offset 0 ("all 01-level records under the same FD share the same record buffer") — but it
never grouped by FD, so records of *different* files aliased the same bytes. Within one FD, multiple
01 records DO share the record area (implicit REDEFINES, ISO §13.18.42), but records under different
FDs are independent record areas and must occupy distinct storage.

Fix:
- `DataSymbol.OwningFile` (new) — the FD that owns a FILE SECTION 01 record.
- `SemanticBuilder` tags every FILE SECTION 01 record with `_currentFdFile` (guarded by
  `_currentArea == FileSection` so WORKING-STORAGE 01s aren't tagged). Previously only the *first*
  01 per FD was linked, via `FileSymbol.Record`.
- `StorageLayoutComputer` now walks the file-section records (contiguous per FD in source order),
  starts a new base offset on each `OwningFile` change, lays each FD's records at that base (so
  same-FD records still alias — implicit REDEFINES preserved), and advances the base past the FD's
  max record size. `FileSectionSize` becomes the sum of per-FD record areas instead of the single
  max. Verified offsets for SQ128A: PRINT-FILE {PRINT-REC, DUMMY-RECORD} both @0, SQ-FS1 @120,
  SQ-FS2 @240, SQ-FS3 @360 — distinct per file, shared within PRINT-FILE.

This was **silent data corruption** for any program writing to multiple files via a shared work
area — the kind of bug that produces a green compile and wrong output. No prior test pinned it
because the single-file SQ tests never exercised cross-FD aliasing.

Result: **SQ128A CLEAN** (0 FAIL*, deterministic), baselined → `tests/nist/valid/` + `scripts/guard.sh`.
**NIST baselines 183 → 184** (… **26 SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration
(347+1 skip) / 184 NIST, **0 regressions** — the broad layout change broke nothing. SQ re-survey:
CLEAN 25→26, FAIL* 15, COMPILE_FAIL 10, NO_OUTPUT 31, RUNTIME 3. RL/IX CLEAN counts unchanged, but
their multi-file FAIL* tests now hold correct data (they still FAIL* on other, distinct runtime
bugs). Next SQ runtime-tail targets: SQ130A / SQ156A / SQ214A (1 FAIL* each), SQ106A (variable-length
WRITE status).

