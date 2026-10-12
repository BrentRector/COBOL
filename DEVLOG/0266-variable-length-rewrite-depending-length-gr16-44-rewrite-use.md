## Entry 266 — Variable-length REWRITE (DEPENDING length, GR16 44) + REWRITE USE declarative → SQ227A/SQ228A

Two of the SQ FAIL* runtime tail, both about a different-length REWRITE on a RECORD VARYING sequential file:

- **REWRITE length for a RECORD VARYING DEPENDING ON file (ISO §14.9.35 GR16).** SQ227A's SQ-FS4 is
  `RECORD VARYING 50 TO 138 DEPENDING ON SQ-FS4-RECSIZE`; `SEQ-TEST-RW-06` reads a 120-byte record, sets
  the DEPENDING item to 130, and `REWRITE SQ-FS4R1-F-G-120` — expecting status **44** (the rewrite length
  130 ≠ the replaced record's 120). The REWRITE was passing the record-name's *declared* size (120) to the
  runtime, so the DEVLOG-261 GR16 check saw 120 == 120 and returned 00. Fixed: `IrRewriteRecordFromStorage`
  gained an optional `LengthLocation`; for a record-sequential RECORD VARYING file `LowerRewrite` passes the
  DEPENDING ON item (via `ResolveRecordLengthLocation`), and `EmitRewriteRecordFromStorage` reads that length
  at runtime (like the variable WRITE) so `FileRuntime.Rewrite` receives the true byte count → 130 ≠ 120 →
  44. **RELATIVE files are excluded** (their handler carries a per-slot length and §14.9.35 GR18 permits a
  relative REWRITE to differ in length) — a first cut that applied the DEPENDING length to *all* varying
  rewrites regressed RL207A (guard-caught); the fix is scoped to `!IsRelative && IsVaryingRecord`.
- **USE declarative on a REWRITE exception (ISO §14.9.49).** `LowerRewrite` emitted FILE STATUS + the INVALID
  KEY branch but never `EmitUseDeclarative`, so a REWRITE that raised an exception (e.g. status 44) did not
  invoke the USE AFTER EXCEPTION/ERROR declarative (SQ228A: "DECLARATIVE NOT EXECUTED ON REWRITE"). Added
  `EmitUseDeclarative` to the no-INVALID-KEY REWRITE path, matching READ/OPEN.

**SQ227A 16/16** (the REWRITE now returns 44 and the declarative fires, adding 3 more PASSes), **SQ228A 1/1**,
both baselined. Guard ALL GREEN: 1000 unit / 347 integration / **226 NIST** (94 NC + 42 IF + 12 SM + 4 IC +
53 SQ + 19 RL + 2 IX), 0 regressions (REWRITE changes touch every rewrite incl. the relative RL chain — all
unchanged after the relative exclusion). Remaining SQ FAIL*: the REWRITE/READ-after-AT-END cluster
(SQ133A → 43, SQ136A → 46, SQ144A declarative) and the OPEN-absent/OPTIONAL cluster (SQ141A/142A/203A).

