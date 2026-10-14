## PB2728: READ has no FILE arm; `READ FILE f` is a syntax error at every edition

Wave 1077, group G (kb/Work PB2728, an R2 lexer-grammar finding).

**The defect.** `readStatement` began `READ (fileName | FILE fileName)`, a copy of the two-arm head that WRITE
(§14.9.51.2) and REWRITE (§14.9.35.2) print as `{ record-name-1 | FILE file-name-1 }`. On those two verbs the FILE
arm is Annex A.4.13's declined element and `SequentialIoBinder.DeclinedFilePhrase` refuses it by name
(COBOLNET1706). Nothing refused the third copy, and both READ formats print a bare `READ file-name-1`
(§14.9.30.2). Re-probed on this branch's build before the fix: a LINE SEQUENTIAL file read with
`READ FILE F1 RECORD AT END DISPLAY "EOF" END-READ.` compiled with exit 0 at --std 2023, with no diagnostic even
under --flag-extensions, and `--run` printed the record. That is a silent over-accept of source no edition
defines.

**The fix.** The arm is deleted from the grammar, so `readStatement` is `READ fileName …`. FILE is a reserved word
and never a fileName, so the parser stops at it with `error COBOL0001: unexpected 'FILE'`. §5.2.1 admits a general
format's words "in the sequence given in the general format", and neither READ format prints FILE. It is a parse
error rather than a named refusal because there is nothing to decline: A.4.13 names WRITE and REWRITE only, and
the error already points at the unprinted word. The rule's comment in `CobolIO.g4` and the declined-elements
section of `docs/COBOLNET_FILES_DESIGN.md` now record why READ carries no FILE arm.

**Sibling sweep.** Every I-O statement head in `CobolIO.g4` was checked. FILE before a file-name remains only in
WRITE and REWRITE (declined, COBOLNET1706) and in DELETE FILE (§14.9.10.2 Format 2, printed as
`DELETE FILE [ OVERRIDE ] { file-name-1 } …`). OPEN, CLOSE, START, DELETE, UNLOCK, RETURN and RELEASE take a bare
name, as printed. No C# read `ReadStatementContext.FILE()`, so nothing else changed shape.

**Found on the way.** `SequentialIoBinder.BindRead` computed `string name = r.fileName().GetText();` and never read
it. The dead local was removed in the same change.

**Goldens and inventory.** One negative, `tests/conformance/negative/pb2728-read-file-unprinted`
(`reject-at: 85 2002 2014 2023`, expecting `COBOL0001: unexpected 'FILE'`). The legal spelling is already pinned
by the positive control `85/a413_declined_phrase_positive_control` (`READ F-IO INTO WS-IN`). The witness was added
to FMT-14.9.30.2's test-ref with a witness-only `record_verdicts` batch. That row was already OK (CONFORMS), so
the note closes no row (`closes_rows: []`, with its reason).
