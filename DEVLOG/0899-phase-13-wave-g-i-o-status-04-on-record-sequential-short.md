## Entry 899 — 2026-07-18 19:40 PDT — PHASE-13 Wave G — I-O status '04' on record-sequential short/long READ (VCR 21)

Implemented I-O status '04' (ISO §9.1.13.2 item 3 / §14.9.35 GR14; clarified COBOL-2023 Annex E.2 item 15,
version-invariant behavior): a record-sequential READ whose physical record length is outside the file's min/max
record size is SUCCESSFUL but sets status '04' — the record is still delivered, right-padded (short) or truncated
(long). Runtime-only, no grammar / bound-node / emitter change.

**As-built + fix.** `FileStatusCode` had no '04' (added `RecordLengthShortLong`). `SequentialConnector.Read` set
`Status = Success` unconditionally; now a branch-specific `shortLong` flag: the FIXED leg (min==max==RecordWidth) sets
it when `n < RecordWidth` (a short/partial final record; a longer-than-max record cannot occur — the buffer reads in
RecordWidth chunks; `n == 0` is EOF), and the VARYING leg when `n < VaryMin || n > VaryMax` (the connector already
carries `VaryMin`/`VaryMax` from the '44' write check). **Line-sequential is deliberately excluded** — its short/long
conditions are '06'/'09', never '04'. The read stays successful (`PrevOpWasSuccessfulRead`, returns the record).

**Golden.** `tests/conformance/2002/io_status_04` (self-contained, GreenfieldOnly — new greenfield SequentialConnector
behavior): writes three 5-char records (a 15-byte file) then reads through a 10-char record description — read 1 =
`00`/`AAAAABBBBB`, read 2 = `04`/`CCCCC     ` (the trailing 5-byte partial record), read 3 = EOF. CLI-probed both this
self-contained path and an external short file (`X(10)` FD over a 5-byte file → `04`, `HELLO     `). SQ blast radius is
greenfield-side only (the legacy runtime is untouched; the greenfield NIST differential is the sweep target) — the
comprehensive gate adjudicates any existing SQ golden that legitimately flips '00'→'04'.

**⚠ Process miss caught (folded in): the VCR 21 FULL Conformance gate surfaced 2 VCR-86 (cluster, `1123a77f`)
regressions the cluster's own gate had missed** — I had FILTERED the cluster gate to `CorpusRunner|VersionMatrix|
CorpusManifest`, skipping the differential-test classes. `DecimalPointDifferentialTests.NumericValue_OnEditedItem_
ConvertsPerMoveRules` (`PIC 99.99 VALUE 1.5`) and `IntrinsicFunctionDifferentialTests.StringChannel_NumericEditedArg_
DeEdits` (`PIC Z9 VALUE 34`) both compile at dialect 85, where VCR 86 now correctly rejects the numeric-edited
numeric-literal VALUE. Fixed: the first pins at 2023 (it IS the VCR 86 feature — an `AssertSpecPinned` dialect param);
the second keeps 85 and initializes via MOVE (the de-editing under test is edition-invariant; the VALUE was incidental,
the func_expr_arg pattern). A broad `feedback_scan_all_similar` sweep of ALL test source found no other greenfield hits
(the `9(n)V99`/`P(n)` matches are category numeric, not numeric-edited; `EnvironmentSpecTests` `@99.99 VALUE 10.00` is
legacy-compiled, unaffected). **Lesson: a change to acceptance semantics needs the FULL Conformance project, never a
CorpusRunner-only filter** — the differential suites are where spec-pinned acceptance is asserted.

