## Entry 303 — Relative-file feature fixes from the diagnosis: RL106A + RL119A (NIST 347→349)

First two feature-completion fixes from the 11-test root-cause diagnosis (docs/ARCHITECTURE_ASSESSMENT.md),
both relative-I/O, both high-confidence and localized.

**RL119A — OPEN I-O/EXTEND on a missing NON-optional relative file must give status 35, not 00.**
`RelativeFileHandler.Open`'s InputOutput/Extend branch loaded the file if present and special-cased only a
missing *optional* file (→05), then fell through to status 00 for a missing *non-optional* file — silently
creating an empty in-memory file (its inline comment "the standard permits creating a relative I-O/EXTEND
file" was wrong: creation on OPEN I-O/EXTEND is permitted ONLY for an OPTIONAL file, ISO §9.1.13 item 5).
RL-FD3 is non-optional and absent, so OPEN I-O returned 00, the USE AFTER ERROR declarative never fired, and
REL-TEST-1 read status 00 vs expected 35. Fixed to mirror the Sequential/Indexed handlers (which already
return 35): missing + optional → 05 (records stay initialized so WRITEs persist), missing + non-optional →
null records + 35. RL119A 1/1 (was 0/1). It uses a unique `XXXXX092` (OPEN I-O only, never created) so it is
genuinely absent regardless of guard order.

**RL106A — variable-length relative records must size the handler by the MAX record, not the first 01.**
RL-FR6 is a Format-2 variable file (`RECORD CONTAINS 56 TO 102`) with two 01 records — the 56-byte SHORT (6A)
declared FIRST and the 102-byte LONG (6B) second. The handler was registered with `recordLength` =
`GetStorageLocation(fileSym.Record).Length` = the FIRST 01 = 56 (the minimum), so `RelativeFileHandler`
capped every record/slot to 56 and truncated the 102-byte LONG records → REL-TEST-16/18 read wrong
record/length. Per ISO §13.18.43 (Format 2: integer-3 is the MAXIMUM bytes in any record), the record area and
handler slot must hold the maximum. Fix: added `SemanticModel.IsVariableLengthRecord` (org-agnostic =
`IsRecordVarying || HasMultipleRecordSizes`; `IsVariableLengthSequential` now reuses it but keeps its
sequential-org gate for the length-framed *sequential* storage decision) + `SemanticModel.MaxRecordLength`
(max of `RecordVaryingMax` and every level-1 record under the FD); `Binder` handler registration now uses
`MaxRecordLength` for any variable-length file instead of the first-01 length. RL106A 4/4 (was 2/4).
Single-01 RECORD VARYING DEPENDING ON (RL206A/207A/208A) is unchanged (max == that record's size); the full
guard confirms no SQ/ST varying regression.

Both deterministic, non-empty, 0 FAIL\*, footer "NO TEST(S) FAILED" (the hardened guard checks all three).
Guard ALL GREEN: 1000 unit / 347 integration / **349 NIST** (RL 26→28), 0 regressions. Remaining diagnosed
gaps: SQ212A (RECORD VARYING DEPENDING ON WRITE length crash in `FileRuntime.WriteRecordVariable` — deeper),
RL205A/213A/208A, RL111A (dispatch stack-overflow = P2), IC233A/234A (GLOBAL FILE inheritance), IC227A/235A
(EXTERNAL), IC114A.

