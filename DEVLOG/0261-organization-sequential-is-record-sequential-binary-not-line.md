## Entry 261 — ORGANIZATION SEQUENTIAL is record-sequential (binary), not line-sequential → SQ116A/SQ121A

**The bug, found by tracing SQ121A.** SQ121A creates a 550×126-byte SEQUENTIAL file, then OPEN I-O reads it
back rewriting every 10th record, and counts 550. It counted **555**. Instrumenting the handler showed the
file was **69850 bytes = 550×127**, read with `reclen=126` → misaligned. Root cause: `Binder` classified
`ORGANIZATION SEQUENTIAL` (and the unspecified default) as **line-sequential** — so OUTPUT wrote each record
`WriteLine(TrimEnd)` (trailing-space-trimmed text + CRLF → 127 bytes/record), INPUT read via `ReadLine`
(realigned, so the create-then-verify pass masked it), but **OPEN I-O always used a binary `FileStream`**
(the `InputOutput` Open case had no line-sequential branch) reading fixed 126-byte chunks from a
127-byte-per-record file → misalignment + a corrupt rewrite. A line-sequential file fundamentally cannot
support fixed-length in-place REWRITE: trimming makes the on-disk record length variable.

Per ISO §9.1.2 / §12.4.5.2, `ORGANIZATION SEQUENTIAL` is **record sequential** — fixed-length records stored
contiguously, no delimiters. Only the `LINE SEQUENTIAL` extension is text/line-delimited. Fixed this:
- **Binder**: `lineSequential` is now true only for `ORGANIZATION LINE SEQUENTIAL` *or* a printer/report file
  (see below). Record-sequential files use the binary `FileStream` in all modes (OUTPUT/INPUT/I-O), so
  REWRITE seeks back one fixed record and overwrites in place, consistently.
- **Variable-length record-sequential** (RECORD IS VARYING or multiple 01 sizes): stored length-framed —
  a 4-byte little-endian length prefix + the data bytes (`SequentialFileHandler.ReadNextVarying`/
  `WriteVariable`), the implementor-defined length-determination the spec allows (§12.4.5.11 RECORD
  DELIMITER / §13.18.43), so lengths round-trip without newline framing. New `FileRuntime.SetSequentialVarying`
  + a CilEmitter emission case; the varying decision is centralized in new
  `SemanticModel.IsVariableLengthSequential` (shared by Binder registration and FileIoLowerer, replacing
  the duplicated `IsVaryingSequential`/`FileHasMultipleRecordSizes`).
- **REWRITE GR16** (§14.9.35): a record-sequential REWRITE whose length differs from the replaced record's
  length is unsuccessful with status 44 (`RecordBoundaryViolation`); the fixed-length path always matches,
  the variable path enforces it against the last-read frame's data length.

**Printer/report files stay line-rendered (the spec's device decision).** Real implementations key the
text-vs-binary choice off the ASSIGN **device** — IBM renders a record-sequential file assigned to SYSOUT
one print line per record, Micro Focus uses ASSIGN TO PRINTER, GnuCOBOL uses LINE SEQUENTIAL for listings;
the NIST suite encodes this as PRINT-FILE `ASSIGN TO XXXXX055` (printer) vs data files `XXXXX014`. The spec's
portable, device-independent expression of "this file is a printed page" is its printer feature set:
`WRITE … ADVANCING` (§14.9.51 vertical page positioning) and the `LINAGE` clause (§13.18.30 logical page).
So a file written with ADVANCING (new `FileSymbol.WrittenWithAdvancing`, set in `BindWrite`) or with a LINAGE
clause is line-rendered; everything else record-sequential. This matches real-world behavior without
hard-coding `XXXXX055`, and explains why most reports already worked under the binary change —
`WRITE … ADVANCING` routes through `WriteRawText` (text + CRLF) regardless of mode; only NC135A/SQ101M mixed
in plain `WRITE` report lines (a NOTE block / blank lines) that needed the line-rendered classification.

**Results — full guard ALL GREEN, 1000 unit / 347 integration / 216 NIST, zero regressions:**
- **SQ116A 10/10** (was 1/10): REWRITE … FROM larger/shorter working-storage areas — the implicit MOVE
  truncates/space-pads into the fixed 130-byte record, then the in-place REWRITE replaces it.
- **SQ121A 3/3** (was 1/3): OPEN I-O sequential read+REWRITE-every-10th now reads exactly 550.
- Both baselined (added to guard). All variable-length sequential baselines (SQ220A/221A/106A/107A/109M/110M/
  214A) MATCH under the new length-prefix framing; all report-bearing baselines MATCH (printer-file rendering
  unchanged). NC135A/SQ101M (which surfaced as transient regressions while the printer-file rule was being
  derived) are MATCH.

