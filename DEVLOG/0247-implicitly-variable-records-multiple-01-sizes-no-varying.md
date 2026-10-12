## Entry 247 — Implicitly-variable records (multiple 01 sizes, no VARYING clause) → SQ106A, SQ107A

SQ107A and SQ106A use a sequential FD with two 01 record descriptions of *different* sizes
(`SQ-VS7R1-M-G-120` = 120, `SQ-VS7R2-M-G-151` = 151) and **no** RECORD VARYING clause. Per ISO
§13.18.43, when an FD has multiple record descriptions of differing sizes the records are
variable-length implicitly — so they need the same no-trim WRITE + read-into-largest treatment as an
explicit VARYING file. The pt.9 feature was gated on `IsRecordVarying` (the explicit clause only), so
these still wrote via the trimming fixed path and read into the first (120) record → long records
truncated, "ERROR ON SECOND READ" / "UNEXPECTED EOF", and (SQ106A) wrong "buffer extension" content.

Broadened `FileIoLowerer.IsVaryingSequential` to also return true when the FD has two or more 01
records of differing storage sizes (`FileHasMultipleRecordSizes`, walking the FD's records via
`DataSymbol.OwningFile`). No DEPENDING item exists in this case, so the WRITE length is the written
record's declared size and no length-store is emitted — exactly the SQ222A path. SQ106A's
"buffer extension" failures vanished as a consequence: the no-trim WRITE now persists each record's
full declared length, so a short record read after a long one recovers the right bytes.

Result: **SQ106A and SQ107A CLEAN** (0 FAIL*, deterministic), baselined. **NIST baselines 191 → 193**
(… **36 SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) / 193 NIST, 0
regressions. Remaining SQ FAIL* are now distinct non-VARYING issues: SQ116A (variable-record REWRITE —
"FROM AREA CLOBBERED" / larger-record rewrite), SQ124A (CLOSE REEL/UNIT status 07 — tape multi-volume,
+ a WRITE-status sub-case), SQ109M/SQ110M (read-count short, 325/750 — distinct line-sequential read
bug), SQ105A/SQ114A (runtime hang). SQ session arc this run: 33 → 36 baselined (+SQ106A/107A/214A).

