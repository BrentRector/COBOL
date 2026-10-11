## Entry 343 — WS-SPEC #3: variable-length-record MERGE … GIVING keeps each record's source length

Backlog #3 (M1/'85), runtime-only. The variable-length plumbing was wired into SORT's USING path
(EmitSortUsingFile → IrSortReleaseVariable → ReleaseRecord, which stores the exact read length) but NOT
into MERGE: `SortRuntime.MergeRecordsInternal` read each input record into a max-size buffer
(`new byte[sf.RecordLength]`) and added that FULL buffer to `sf.Records`, discarding the actual read
length. `ReturnRecord` then reported `record.Length` = the SD maximum for every record, and the
(already-correct) variable-aware GIVING write loop (`IrSortGivingWriteVariable` → `GetLastReturnedLength`
→ `WriteRecordVariableToFile`) faithfully wrote them all at max — short records space-padded to the
maximum. ISO §14.9.24.4 GR7b/GR12b: a record written to a variable-length file keeps the size it had
when read.

Fix: after each successful `FileRuntime.ReadRecord`, take `FileRuntime.GetLastRecordLength(inputName)`
(clamped to [1, sf.RecordLength]) and store a right-sized `byte[]`. A fixed-length file reports the full
length, so the clamp is a no-op there — fixed-length MERGE is unchanged, and SORT (a different path) is
untouched. Sort/compare unaffected: keys lie within the minimum record size (§14.9.24.3 SR4g) and the
comparers bounds-check.

Test (`SpecFixTests.MergeVaryingRecord_Giving_PreservesPerRecordLength`): writes two ascending varying
input files (lengths 2/3/5/10), MERGEs USING…GIVING a varying output, reads it back, and asserts each
record's restored length — `[Az] LEN=02 / [Bxx] LEN=03 / [Cwwww] LEN=05 / [Dyyyyyyyyy] LEN=10`. (The
test also confirms variable-length MERGE GIVING works end-to-end — no prior integration test covered it.)
Guard ALL GREEN: **1047 unit / 420 integration (+1) / 364 NIST**, 0 regressions.

