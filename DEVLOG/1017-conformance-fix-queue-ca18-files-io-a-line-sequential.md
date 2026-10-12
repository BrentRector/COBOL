## Entry 1017 — 2026-07-23 10:36 PDT — Conformance fix-queue: CA18 (files-io) — a line-sequential REWRITE overwrites in place per ISO §14.9.35.4 GR17 (00/44/71), no longer a blanket '30'

**CA18 (MINOR/L, files-io) landed** — the effort-L tail of the independent-minors batch. ISO §14.9.35.4 GR17 defines a
legal line-sequential REWRITE: (a) a preceding partial read (the CA15 over-length '06') ⇒ '44'; (b) a record LONGER than
the one being replaced ⇒ '44'; (c) a shorter/equal record is space-padded to the replaced length and written ⇒ '00';
(d) a record carrying a character outside the line character set ⇒ '71'. `SequentialConnector.Rewrite` guarded its
seekable in-place branch with `!_lineSequential`, so EVERY line-sequential REWRITE fell through to
`Status = PermanentError` ('30') — a status GR17 never yields for a well-formed statement, leaving the file unchanged.

**The obstacle (why effort-L):** an in-place overwrite needs the physical BYTE offset of the last-read line, but the
line-seq READ used `StreamReader.ReadLine()`, which hides both the byte position (the reader buffers ahead, so
BaseStream.Position is the fill boundary) and the terminator width. **Fix, two parts.** (1) A `ReadPhysicalLine(out int
delimBytes)` helper replaces `ReadLine()` on the line-seq READ path — char-by-char, faithful to ReadLine's CR/LF/CRLF
semantics, but reporting the terminator width so a running `_lineByteOffset` (Latin1: 1 char == 1 byte, the same
logical-offset discipline as `_readOffset`) tracks the physical position; each read records `_lastLineStart`,
`_lastLineBytes` (the replaced record's data length), and `_lastReadLinePartial` (an over-length '06' read or a served
`_lineRemainder` — GR17a). (2) A line-seq arm in `Rewrite` (before the '30' fall-through) applies GR17a→'44',
GR17d→'71' (a CR/LF in the record), GR17b→'44', else space-pads the trimmed record to `_lastLineBytes` and overwrites
exactly that byte span at `_lastLineStart` via `_reader.BaseStream` (seek/write/flush/restore — the record-seq pattern).
Padding to `_lastLineBytes` keeps the delimiter position invariant, so the overwrite is exact; the trimmed length
matches the line-seq WRITE model (`WriteLine(image.TrimEnd())`). New `FileStatusCode.LineRecordInvalidChar = "71"`.

**Blast radius / risk.** The READ-path rewrite touches a hot path shared by every line-sequential read, so the manual
reader was verified faithful FIRST: CA15's over-length '06'+remainder test, CA16, the FileIoDifferential suite (12
green), 61 file-IO/report-writer tests, 71 NIST SQ programs, and characterization 33/33 (runtime-only, byte-neutral) all
pass. **Goldens** (`SequentialFileIoSpecTests`): GR17 equal/longer/shorter — HELLO→WORLD '00', AB→XYZ '44' (longer,
unchanged), CDEFG→HI '00' (shorter, padded to "HI   "); and GR17a — an over-length '06' read then REWRITE ⇒ '44'.
Expected values SPEC-DERIVED. Re-scout `wf_a09670d5-cdc` flagged the fix text as materially stale (anchors 454/466→477/489;
reuse `lineTooLong`/`LastReadLength`; only the byte anchor was genuinely new) — reconciled here.

