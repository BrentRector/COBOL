## Entry 288 — Variable-length-record SORT … USING/GIVING → ST111A + ST124A (NIST 287→289)

Fixed the variable-length-record SORT bug from 287, unblocking both `build → sort → verify` chains over
40-variable-length-record files: **ST109A→ST110A→ST111A (7/7)** and **ST122A→ST123A→ST124A (7/7)**.

The file-based `SORT … USING … GIVING` emitters threw the per-record length away:
- `EmitSortUsingFile` read each input record into `inputFile.Record` — the FIRST (smallest, 50-byte) 01 —
  truncating 75/100-byte records, then released the SD record at its fixed declared size.
- `EmitSortGivingFile` returned into the (small) SD record and wrote a fixed length.

So records came back shifted/NUL. The runtime already had the pieces (SequentialFileHandler.LastRecordLength,
FileRuntime.GetLastRecordLength, StorageHelpers.WriteRecordVariableToFile) from the SQ/RL variable-length
work — they just weren't wired into the sort. Fix (gated on `IsVaryingRecord`):
- **USING:** read into the LARGEST input 01 (`ResolveReadRecordLocation`) and release the ACTUAL bytes via
  new `IrSortReleaseVariable` → `SortRuntime.ReleaseRecord(sort, area, off, FileRuntime.GetLastRecordLength
  (inputFile))`. The input record's layout matches the SD record's, so the sort keys land at the same
  offsets — no fixed-size SD copy that would truncate.
- **GIVING:** RETURN into the LARGEST output 01 (not the small SD record, which would truncate), then write
  each record at its own length via new `IrSortGivingWriteVariable` → `StorageHelpers.WriteRecordVariableToFile
  (output, area, off, SortRuntime.GetLastReturnedLength(sort))`. `ReturnRecord` now records each returned
  record's byte length (`GetLastReturnedLength`). The sort buffer already stored `byte[]` per record, so the
  actual lengths survive the sort untouched.

Both emitters keep the fixed-length path unchanged; the change is gated by `IsVaryingRecord` and is inert
for fixed sorts. Baselined ST111A/ST124A; their builders (ST109A/ST122A, 000-of-000) and sorters (ST110A/
ST123A, NO_OUTPUT) run as non-baselined producers, consecutive before each verifier. Guard ALL GREEN: 1000
unit / 347 integration / **289 NIST** (… + 40 IX + 19 ST), 0 regressions (the change is shared by every
file-based SORT; the procedural RELEASE/RETURN path is untouched). ST: 19 baselined; remaining = chain
consumers ST107A/117A/121A, FAIL* family ST126A/139A/140A/144A/146A/147A (MERGE custom-alphabet), NO_OUTPUT,
ST115A timeout, ST301M flagging.

