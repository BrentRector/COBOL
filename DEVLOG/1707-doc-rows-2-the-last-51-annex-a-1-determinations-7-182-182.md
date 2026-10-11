## Entry 1707 — 2026-09-25 10:52 PDT — doc-rows-2: the last 51 Annex A.1 determinations (§7 182/182); FMT-5.2 retired; GAP 1248 → 1241

**Every inventory row is now adjudicated (4,347 rows, 0 verdict-less), and §7's documentation obligations are 182 of
182 discharged.** kb/Work PB1522 step 3 finished: the 51 A.1 items doc-rows-1 had to hold back (their determination was
missing, or the code's behaviour was wrong and so could not be documented) were each written as the INTENDED
determination by one of seven writers, attacked by a refuter, corrected where the refuter won, and recorded WITH its
§7 row in one batch (`adjudication/doc-rows-2/`). Verdicts: DIVERGES 24 · PARTIAL 14 · CONFORMS 11 · NOT-IMPLEMENTED 1 ·
DOCUMENTED-NON-SUPPORT 1; 51 of 51 records accepted; GAP 1247 → 1241. Merged first: w60b (kb/Work PB1536 Q2) — the FMT
scan now needs a diagram or a Format label, so the parent clause §5.2 'General formats' is no longer a false FMT row
(GAP 1248 → 1247), and the §15.78.2 REVERSE diagram the transcription had lost is restored (FMT-15.78.2 restated CONFORMS).

**Two owner decisions were taken during the round.** R47: a record written to a sequential or report file with no
CODE-SET clause is Latin-1, one byte per position, and a character above U+00FF is REFUSED — '71' for line sequential
(the standard's value), '91' for record sequential and report files (a new implementor-defined, fatal 9x status;
DOC-A.1-110 now defines '90' and '91') — instead of today's silent '?' (PB690). R48 (confirmed "keep R48" after a
pros/cons review): STOP RUN or a fatal EC in a program a .NET host activated ends the RUN UNIT only, closes its files,
resets its state and returns one catchable result to the host — deliberately unlike GnuCOBOL's process exit (PB1149,
PB1253, PB1069).

**The refuters overturned 21 of 51 items, and the corrections are the substance of this landing.** Examples: item 75's
lock-type table could not deliver §9.1.15 Table 19 on Unix (an fcntl read lock refuses only a writer that asks for a
write lock), so the row now states the guarantee and PB833 records the design flaw; items 66/67/161/162 had published
option spellings, defaults and resolution order that owner decision R44 never made — each is trimmed to R44 and the rest
is PB1086's design doc (67 is recorded NOT-IMPLEMENTED on an R44-only anchor row); item 68's "GnuCOBOL trims AS names"
was false (it keeps AS literals verbatim); item 157's TAB is the next tab stop of 8, not one position (new PB1586);
item 164's input-output area is the connector's host buffer, separate from the record area, and IS observable (PB1587);
item 40 now documents GnuCOBOL's COPY suffix AND directory order (orchestrator rule-1 decision; PB1355).

**Lander decisions beyond the refuters (for review).** (1) Items 14 and 89 had been adjudicated to OPPOSITE meanings of
"located" for the same `ProgramTable.ProbeSiblingModule` — CALL: an unloadable module is not located
(EC-PROGRAM-NOT-FOUND, CONFORMS); function: it is located and resources are missing (EC-PROGRAM-RESOURCES, DIVERGES),
each upheld by its own refuter. ISO leaves the locating mechanics to the implementor, so rule 1 goes to GnuCOBOL
(libcob/call.c `set_resolve_error`: NOT-FOUND for both), and item 89 was rewritten to item 14's determination (CONFORMS,
test-needed); PB1422 keeps only the INVOKE twin (item 102). (2) Item 99 opens "Not provided." on an optional item, so the
derived-verdict selector fixes it at DOCUMENTED-NON-SUPPORT (PB280 Q1); the EC-IMP-suffix under-rejection stays
PB1531's. (3) A probe showed a FUNCTION-POINTER leaf of a STRONG group counts 8 positions imaged as spaces, so DOC-A.1-210's
"no character positions" was rewritten to the DOC-A.1-56 posture — and the two duplicate DOC-A.1-56 rows were merged.

**Register (rule 8).** Rows moved to their mechanism owners (PB1534 landed: every row it tracked has its §7 row; PB1522
now holds only DOC-A.1-201, a PARTIAL whose ordering edge lacks a committed witness — landing PB1522 outright turned
DefectiveRowCoverageDriftTests red on it); PB1527 re-scoped to the STRING crash only (crashes, not wrong-answer); arms added to
PB1529 (Int128 product wrap), PB1355, PB1539, PB1520, PB833, PB1086, PB690, PB1397, PB1586, PB1587; rows released from
PB1422, PB1369, PB547, PB322 (determination E is documentation only), PB1178, PB1094, PB833. `DerivedVerdictDriftTests`'
"undetermined" anchors moved (optional 67 → 85; the conditional anchor retired — every conditional item now has a row).

**Gate (L2, this worktree, Normal priority):** build 0 errors; Conformance (whole assembly, unfiltered) 8852 passed / 0 failed;
Unit 29307 passed / 0 failed; Characterization 33 passed / 0 failed; audits (annex A.1, doc citations, code citations, evidence supersession,
witness loss) green; `work.py check` green; semgrep verify PASS.

