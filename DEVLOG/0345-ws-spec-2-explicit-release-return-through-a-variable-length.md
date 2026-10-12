## Entry 345 — WS-SPEC #2: explicit RELEASE/RETURN through a variable-length SD keeps each record's length

Backlog #2 (M1/'85). The variable-length machinery was wired only into the implicit SORT … USING/GIVING path,
never into explicit RELEASE/RETURN (Format-1 SORT with INPUT/OUTPUT PROCEDURE). `FileIoLowerer.LowerRelease`
always emitted `IrSortRelease` at the SD's declared max size, and `LowerReturn` never wrote the returned
length back into the SD's DEPENDING ON item — so every released record was stored at max and every returned
record came back padded to the maximum with a stale DEPENDING value. ISO §13.18.43 GR13a (RELEASE byte count
= the DEPENDING item) / GR15 (RETURN restores it).

Fix (mirrors the existing variable-length WRITE/READ patterns):
- Two IR nodes — `IrSortReleaseFromDepending` (ReleaseRecord with length = ReadFieldAsInt(depending), like
  IrSortReleaseVariable but sourcing the length from a storage item rather than a file) and
  `IrSortReturnStoreLength` (MoveIntToField with SortRuntime.GetLastReturnedLength, like IrStoreRecordLength
  but for the sort) — plus their CilFileIoEmitter emitters and CilEmitter dispatch cases.
- LowerRelease branches on `IsVaryingRecord(SortFile)` → IrSortReleaseFromDepending; LowerReturn adds
  IrSortReturnStoreLength after the IrSortReturn. The runtime was already correct (ReleaseRecord stores the
  exact length; ReturnRecord sets LastReturnedLength + space-pads) — only the explicit-statement lowering had
  bypassed it. Gated on IsVaryingRecord so fixed-length SDs take the unchanged path.

ST146A (RETURN INTO a format-3 ODO record) is untouched: it has no `RECORD VARYING DEPENDING ON` clause, so
ResolveRecordLengthLocation returns null.

Test (`SpecFixTests.SortVaryingRecord_ReleaseReturn_PreservesPerRecordLength`): a variable SD (1..5), a 1-char
key (within the minimum record length), RELEASE three records of length 5/1/3, OUTPUT-PROCEDURE RETURN each
and DISPLAY at its restored length — `[AZZZZ] LEN=05 / [B] LEN=01 / [CYY] LEN=03`.
Guard ALL GREEN: **1047 unit / 423 integration (+1) / 364 NIST**, 0 regressions.

