## Entry 278 — Variable-length indexed records → IX105A

IX105A's three indexed files declare `RECORD CONTAINS 56 TO 100/101/102 CHARACTERS` with both a short (56)
and a long (100) 01 record, then write SHORT and LONG records and read them back checking length/content
("READ LONG RECORDS — WRONG LENGTH OR WRONG RECORD"). `IndexedFileHandler` stored every record at a fixed
length and reported `LastRecordLength => _recordLength`, so a SHORT record read back wrong.

Added variable-length support to the indexed handler, gated on a new `IsRecordVarying` flag (fixed-length
indexed files are byte-for-byte unaffected — the guard confirms):
- **Detection + wiring.** `SemanticModel.HasMultipleRecordSizes(file)` (extracted from the sequential check)
  is org-independent. The Binder emits `FileRuntime.SetIndexedVarying` for an INDEXED file with RECORD IS
  VARYING or multiple 01 sizes (+ its CilEmitter dispatch case — the recurring "new emitted runtime call
  needs an explicit case" gotcha). `FileIoLowerer.IsVaryingRecord` now returns true for such INDEXED files,
  so WRITE lowers to `IrWriteRecordVariable` (the written 01's actual size).
- **Storage + length.** Records are stored at their actual byte length; `LastRecordLength` is now a field set
  on every read via a `CopyOut` helper. Persistence is length-framed (4-byte LE prefix + data) when varying,
  contiguous fixed records otherwise; fixed save now space-pads a short record to `_recordLength`.
- **Bug caught + fixed before commit:** a `sed` that rewrote the `Array.Copy(... recordBuffer ...)` read
  pattern into `CopyOut(...)` also rewrote `CopyOut`'s own body → infinite recursion → stack overflow on the
  first keyed READ. Restored `CopyOut` to call `Array.Copy` directly.

IX105A → CLEAN, baselined. Guard ALL GREEN: 1000 unit / 347 integration / **263 NIST** (94 NC + 42 IF + 12 SM
+ 4 IC + 59 SQ + 19 RL + 33 IX), 0 regressions. IX is now 33/42 baselined; the remaining are deep/risky: the
column-7 X-card layout-variant substitution (IX207A), alternate-key relational START runtime (IX208A 9 FAIL*),
qualified-key / REDEFINES-of-key START (IX215A), and SELECT-OPTIONAL absent-file isolation under the shared-TF
model (IX216A/217A/218A) — plus the two flagging modules IX301M/IX401M (no CCVS report, excluded by design).

