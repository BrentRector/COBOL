// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// File I/O statements and file control (INPUT-OUTPUT SECTION).
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolIO;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// INPUT-OUTPUT SECTION
// ==========================================

inputOutputSection
    : INPUT_OUTPUT SECTION DOT
      fileControlParagraph?
      ioControlParagraph?
    ;

// FILE-CONTROL.
fileControlParagraph
    : FILE_CONTROL DOT fileControlClauseGroup*
    ;

// ISO §12.4.5.2 SR1: SELECT comes first; all following clauses (including ASSIGN) may appear in
// ANY ORDER. So ASSIGN is just one of the order-free fileControlClauses, not a fixed-position slot.
// The SELECT file-name is the file's DEFINITION (§12.4.5.1 general format `SELECT [OPTIONAL] file-name-1`) and
// the third IsProvableUserWordPosition slot. `reservedGatedWord` is the declaration re-admission the reservation
// gate needs (kb/Work PB693, the dataName/programName precedent): a §8.9-reserved word is gated OUT of cobolWord
// at the editions that reserve it, so without this alternative `SELECT DEFAULT …` at --std 2002 answers a raw
// COBOL0001 instead of the targeted COBOLNET0901. It sits HERE and not in `fileName`, which every REFERENCE uses
// — OPEN/CLOSE take file-name LISTS, and re-admitting the word there would let a list absorb the next
// statement's leading keyword again (the PB693 defect). DataBinder reads `grp.fileName()` null-safely.
fileControlClauseGroup
    : SELECT OPTIONAL? (fileName | reservedGatedWord) { declareName(TokenStream.LT(-1)); }   // keywordContinuesHere (kb/Work PB805)
      fileControlClauses*
      DOT
    ;

// ASSIGN [TO] {device-name-1 | literal-1} … [USING data-name-1] | ASSIGN USING data-name-1 (ISO §12.4.5.1, every
// format). ⛔ THE ELLIPSIS IS ON THE INNER BRACE PAIR (rendered, kb/Work PB829 — folios 312-314), so the TO phrase
// is a LIST and `assignTarget+` is the general format. What the list MEANS, and which lists are allowed, is the
// implementor's (§12.4.5.2 SR5): WiseOwl COBOL's determination (docs/CONFORMANCE.md §7 row DOC-A.1-71) is applied BY
// NAME at bind, in `AssignTargetRule` — never by a one-operand grammar that answered a legal list with COBOL0308.
assignClause
    : ASSIGN TO? assignTarget+ (USING dataReference)?
    | ASSIGN USING dataReference
    ;

assignTarget
    : cobolWord
    | STRINGLIT
    ;

fileControlClauses
    : assignClause
    // relativeKeyClause precedes organizationClause: both can begin with RELATIVE (organizationType
    // has a bare RELATIVE), and under leniency L2 `RELATIVE data-name` (no KEY) is the key clause while
    // a lone RELATIVE is the organization. Trying the key clause first means `RELATIVE <data-name>`
    // binds the key; a bare RELATIVE (no following data-name) fails the key clause and falls through to
    // organizationClause. `ORGANIZATION RELATIVE` is unaffected (it begins with ORGANIZATION).
    | relativeKeyClause
    | organizationClause
    | accessModeClause
    // recordKeyClause and recordDelimiterClause both begin with RECORD. With KEY now optional in
    // recordKeyClause (leniency L3), disambiguation rests on the operand: DELIMITER is a reserved token
    // so `RECORD DELIMITER …` cannot satisfy recordKeyClause's dataReference and falls through to
    // recordDelimiterClause. List recordKeyClause first; the parser back-tracks to the delimiter form.
    | recordKeyClause
    | recordDelimiterClause
    | alternateKeyClause
    | fileStatusClause
    | fileReserveClause
    | paddingCharacterClause
    // COBOL-2002 file sharing / record locking (ISO §12.4.5.15 / §12.4.5.9) — unique leading tokens
    // (SHARING / LOCK), {is2002()}?-gated, ADDITIVE (the DEVLOG 621/622 lesson).
    | sharingClause   // COBOL-2002; parses at all editions (superset), introduction-gated post-bind by VersionConformancePass ParseArm.VisitSharingClause (rearch 14g.4)
    | lockModeClause   // LOCK MODE (LOCK/MODE hard-reserved) introduction-gated post-bind by VersionConformancePass ParseArm.VisitLockModeClause (rearch 14g.4)
    | fileCollatingSequenceClause   // §12.4.5.7 INDEXED per-key collating; parses at all editions (superset), introduction-gated post-bind by VersionConformancePass
    // ⛔ THE §12.4.5.1 GENERAL FORMATS ARE CLOSED (kb/Work PB829; RENDERED from the printed pages — PDF p342-344 /
    // folios 312-314). The union of Format 1 (indexed), 2 (relative), 3 (sequential) and 4 (sort-merge) is SELECT
    // [OPTIONAL] · ASSIGN · ACCESS MODE · ALTERNATE RECORD KEY · collating-sequence-clause · FILE STATUS ·
    // LOCK MODE · ORGANIZATION · RECORD KEY · RELATIVE KEY · RECORD DELIMITER · RESERVE · SHARING, plus the
    // COBOL-85 PADDING CHARACTER clause removed at COBOL-2002. Every one of them opens with a reserved word, so
    // this last alternative — whose first token is an IDENTIFIER — can only ever match source the general format
    // does not admit. It is NOT a vendor hook (see CobolExpressions.g4#unrecognizedClause).
    | unrecognizedClause   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1970)
    ;

// ISO §12.4.5.7 COLLATING SEQUENCE clause — the collating sequence for the record keys of an INDEXED file.
// Format 1 (file-level): reuses the shared `collatingForPhrase` (FOR ALPHANUMERIC / FOR NATIONAL — §5.2.6.4
// choice indicators, one-or-more any-order) and the `IS alphabet-name-1 [alphabet-name-2]` form. Format 2
// (key-level, OF-led): names specific RECORD KEY / ALTERNATE RECORD KEY items and their alphabet-name-3. Parses
// at all editions (superset); the post-85 FOR-split / national / key-level legs are introduction-gated at bind,
// and SR1-8 (alphabet class, single file-level clause, key existence, no subscript, single clause per key) are
// enforced by the binder — the grammar is a permissive shape (the sharing/lock precedent). `OF` disambiguates
// Format 2 from Format 1; list it first so the parser commits on the OF token.
fileCollatingSequenceClause
    : COLLATING? SEQUENCE
      ( OF cobolWord+ IS? cobolWord                 // Format 2: OF {data-name-1 | record-key-name-1}… IS alphabet-name-3
      | collatingForPhrase+                         // Format 1: {FOR ALPHANUMERIC | FOR NATIONAL} IS alphabet-name …
      | IS? cobolWord cobolWord?                    // Format 1: IS alphabet-name-1 [alphabet-name-2]
      )
    ;

// COBOL-2002 SHARING clause (ISO §12.4.5.15): the file-connector sharing mode.
sharingClause
    : SHARING WITH? sharingMode
    ;
sharingMode
    : ALL OTHER?
    | NO OTHER?
    | READ ONLY
    ;

// COBOL-2002 LOCK MODE clause (ISO §12.4.5.9): MANUAL / AUTOMATIC (no EXCLUSIVE), with an optional
// single/multiple record-lock granularity.
// ⛔ MODE IS AN OPTIONAL WORD (§8.3.2.4.3; kb/Work PB695 family 3), so `LOCK IS MANUAL` and the bare
// `LOCK MANUAL` are conforming source this rule used to refuse with COBOL0001. MEASURED on printed page 355 /
// folio 325: LOCK's box 86.56–112.03 carries a rule at 87.96–111.07 (90.8% cover) while MODE's box
// 115.12–144.04 has NO rule under it at all, and neither does IS. LOCK heads no other file-control clause, so
// dropping MODE introduces no new prediction ambiguity. The COBOL-2002 introduction gate keys on the CLAUSE
// (VersionConformancePass ParseArm.VisitLockModeClause), never on `ctx.MODE()`, so it survives the omission —
// probed, `lock85-bare-gated` → COBOLNET0900.
lockModeClause
    : LOCK MODE? IS? (MANUAL | AUTOMATIC) lockOnPhrase?
    ;
// §12.4.5.9.2's second half: [ [WITH] LOCK ON [MULTIPLE] {RECORD | RECORDS} ]. WITH is un-underlined (measured
// p355: its box 251.70–278.69 has no rule; the rule at 283.11–306.22 belongs to the LOCK that follows it).
// ⚠ ON IS UNDERLINED THERE (83.5% cover) and MULTIPLE too (95.0%) — MULTIPLE is optional because the printed
// format BRACKETS it, not because it is un-underlined, and `ON?` is an over-acceptance this change does not
// take on (see the PB695 family-3 report's new-defect paragraph).
lockOnPhrase
    : WITH? LOCK ON? MULTIPLE? (RECORD | RECORDS)
    ;

fileReserveClause
    : RESERVE integerLiteral (AREA | AREAS)?
    ;

// PADDING CHARACTER: PADDING [CHARACTER] IS {data-name-1 | literal-1} — the ANSI X3.23-1985 Sequential I-O
// block-fill character. THE 2023 STANDARD HAS NO SUCH CLAUSE: the file control entry's clauses are
// §12.4.5.4 (the ACCESS MODE clause) through §12.4.5.15 (the SHARING clause) and none of them is PADDING
// (§12.4.5.9, which this comment once cited, is the LOCK MODE
// clause; kb/Work PB159), and the word occurs NOWHERE in the 2023 text — §8.9's list runs PACKED-DECIMAL → PAGE.
// SUPERSET PARSE at every edition, gated post-bind from 2014 by VersionConformancePass ParseArm
// .VisitPaddingCharacterClause (registry row `padding-character-removed-2014`, COBOLNET0902 — an error under
// strict, a warning with the pre-removal no-op reading under --permissive). Deleting the rule was MEASURED and
// rejected: NIST SQ216A writes `PADDING CHARACTER IS "9"` and SQ217A the operand form `PADDING PADDING-CHARACTER`
// (both compiled at --std 85), so the 85/2002 reading has to stay. Nothing is captured on the FileModel and that
// is deliberate — WiseOwl COBOL has no blocking model, so a stored padding character would be a field nothing reads
// (feedback_a_dead_lookup_is_also_unverified); the clause is inert in the program's own terms, which is what
// makes the --permissive migration reading honest.
// ⛔ TWO ARMS, ONE TOKEN (kb/Work PB300 — the second arm is why a decline may not cost the user the WORD):
// PADDING is also a `reservationGated` row in tests/version-matrix/cobol-words.json, so `01 PADDING PIC X.` is a
// legal declaration exactly at 2014/2023, where §8.9 does not reserve the word, and draws a targeted
// COBOLNET0901 at 85/2002 through dataName's generated `reservedGatedWord` alternative.
paddingCharacterClause
    : PADDING CHARACTER? IS? (literal | dataReference)
    ;

// ISO §12.4.5.11 RECORD DELIMITER clause: RECORD DELIMITER IS {STANDARD-1 | feature-name-1}. The general
// format (§12.4.5.11.2, verified against the printed page) underlines RECORD, DELIMITER and STANDARD-1 and
// leaves IS unmarked, so IS is the only optional word and exactly one alternative of the brace shall be
// written — which is this rule verbatim. `cobolWord` is feature-name-1's permissive surface.
// ⛔ PARSED AND *DIAGNOSED*, NOT PARSED AND IGNORED (kb/Work PB292). Until 2026-09-05 nothing anywhere read
// this rule and the clause compiled clean at every edition on BOTH arms, which left docs/CONFORMANCE.md §2
// row 26's "Not claimed" unobservable and §4.2.6 ¶3's MANDATORY compile-time warning mechanism unbuilt. The
// decline now lands at DataBinder.BindFileControl as COBOLNET1778 (accept-inert Warning) — Annex A.3 item 26
// for STANDARD-1, Annex A.1 item 150 for feature-name-1. Variable-length records keep the §12.4.5.11.4 GR5
// implementor framing (4-byte length prefix, Annex A.1 item 151), which §12.4.5.11.4 GR1 keeps invisible to
// the record area and the record size.
recordDelimiterClause
    : RECORD DELIMITER IS? (STANDARD_1 | cobolWord)
    ;

// ISO §12.4.5.10: the leading "ORGANIZATION IS" is optional — a bare organization type
// (e.g. a lone SEQUENTIAL) is a valid ORGANIZATION clause.
organizationClause
    : (ORGANIZATION IS?)? organizationType
    ;

// ISO §12.4.5.10.2 prints `{ {LINE|RECORD} SEQUENTIAL | RELATIVE | INDEXED }`; RENDERED (PDF p.357 / folio 327):
// LINE is underlined, RECORD is NOT — an optional word (§5.2.3), so the RECORD arm is written `RECORD? SEQUENTIAL`
// and bare SEQUENTIAL is the same alternative (§12.4.5.10.3 GR6). The written-out RECORD word is the 2023 inner
// choice and is edition-gated by VersionConformancePass.ParseArm.VisitOrganizationClause (kb/Work PB706).
organizationType
    : LINE SEQUENTIAL
    | RECORD? SEQUENTIAL
    | RELATIVE
    | INDEXED
    ;

accessModeClause
    : ACCESS MODE? IS? accessMode
    ;

accessMode
    : SEQUENTIAL
    | RANDOM
    | DYNAMIC
    ;

// IS is an optional word in the RECORD KEY / ALTERNATE RECORD KEY clauses (ISO §12.4.5 — the CCVS
// suite writes "RECORD KEY data-name" without IS).
//
// Leniency L3 (see docs/dialect-strictness.md): ISO §12.4.5.12 requires `RECORD KEY IS data-name`
// (⚠ KEY is NOT required — see below), and the CCVS suite writes `RECORD data-name` without KEY (e.g.
// IX103A `RECORD IX-FS1-KEY`). The grammar parses the permissive superset `RECORD KEY? IS?
// dataReference`; the no-KEY form is accepted in DialectMode.Default and diagnosed under named-strict
// modes by the inline check in SemanticBuilder.VisitFileControlClauseGroup (CBL3615/3616). Disambiguation
// from recordDelimiterClause still holds: DELIMITER is a reserved token, so `RECORD DELIMITER …` cannot
// match dataReference and falls through to recordDelimiterClause (likewise RECORD CONTAINS/VARYING in an FD).
//
// ⛔ BOTH CLAUSES CARRY A **REQUIRED CHOICE OF TWO KEY FORMS**, and the second one is DECLINED — so it is a
// DISTINCT PARSE ALTERNATIVE the binder can SEE and refuse by name, exactly as `WRITE … FILE file-name-1` is
// (Annex A.4.13 item 2 → COBOLNET1706, below). kb/Work PB358 / PB293. The general formats, RENDERED from the
// printed pages rather than read off the OCR (§12.4.5.12.2 = PDF p359/folio 329, §12.4.5.6.2 = p350/folio 320):
//     <u>RECORD</u> KEY IS { data-name-1 | record-key-name-1 <u>SOURCE</u> IS { data-name-2 } … }
//     <u>ALTERNATE</u> <u>RECORD</u> KEY IS { … same brace … } [ WITH <u>DUPLICATES</u> ] [ <u>SUPPRESS</u> WHEN literal-1 ]
// Underlining measured on both pages: RECORD / ALTERNATE / SOURCE carry rules, KEY and BOTH occurrences of IS
// do not — so `SOURCE IS?` and the pre-existing `KEY? IS?` are the conforming spellings. The inner
// `{ data-name-2 } …` brace pair takes the ellipsis, hence `dataReference+` (ONE or more), and §12.4.5.12.4
// GR2 / §12.4.5.6.4 GR2 say what the repetition MEANS: "Record-key-name-1 defines a record key consisting of
// the concatenation of all occurrences of data-name-2 in the order specified."
// ⛔ WHY IT PARSES AND IS THEN REFUSED, rather than having no surface at all: Annex A.3 item 40 — "The
// capability of specifying the SOURCE phrase of the RECORD KEY clause and ALTERNATE RECORD KEY clause is
// dependent on the capabilities of the processor" — makes it PROCESSOR-DEPENDENT, docs/CONFORMANCE.md §2 row 40
// declines it, and §4.2.6 ¶3 then obliges this compiler to SAY SO: "An implementation shall provide a warning
// mechanism at compile time to indicate use of syntactically-detectable processor-dependent language elements
// not supported by that implementation." Until 2026-09-09 the phrase matched no alternative anywhere and the
// user got `COBOL0312: unexpected 'SOURCE' … A period may be missing at the end of the previous sentence` —
// a generic parse error pointing at the wrong repair. DataBinder.BindFileControl now names it: COBOLNET1954.
// The refusal is an ERROR rather than the accept-inert warning RECORD DELIMITER gets, because there is no inert
// reading — a declared key that names no data item leaves the file with NO prime key — and §4.2.6 ¶3's own
// closing sentence is the licence: "The implementor is not required to produce executable code when unsupported
// processor-dependent language elements are used."
// ⛔ ONE RULE, USED BY BOTH CLAUSES. A.3 item 40 names RECORD KEY and ALTERNATE RECORD KEY in ONE sentence, so
// they get ONE shared sub-rule and ONE diagnostic; a fix touching only `recordKeyClause` would be this repo's
// most reproducible defect shape (feedback_two_arm_dispatch) repeating on the clause that already has a twin.
// The SOURCE arm is listed FIRST so ANTLR tries the more specific alternative first (feedback_grammar_precedence);
// `dataReference` cannot swallow `SOURCE` in any case, because SOURCE is a reserved token at every edition
// (tests/version-matrix/reserved-words.json: r85/r2002/r2014/r2023 all true) and is not in `cobolWord`.
recordKeyClause
    : RECORD KEY? IS? (recordKeySourcePhrase | dataReference)
    ;

alternateKeyClause
    : ALTERNATE RECORD? KEY? IS? (recordKeySourcePhrase | dataReference)
      (WITH? DUPLICATES)?
      alternateKeySuppressWhen?
    ;

// ISO §12.4.5.12.2 / §12.4.5.6.2 — the declined second arm of both clauses' required key-form choice.
// record-key-name-1 is its OWN name class (§8.3.2.2.24), not a data-name: §8.4.6.2.4 "Scope of
// record-key-names" gives it file-level scope, and it is DEFINED here rather than referenced, which is why
// this is a bare `cobolWord` with no qualification or subscript suffix — the printed format shows a plain
// `record-key-name-1`. data-name-2 keeps the general `dataReference` surface the data-name-1 arm already uses,
// so an OF/IN-qualified source item is recognized (and refused) rather than tripped over mid-clause.
recordKeySourcePhrase
    : recordKeyName SOURCE IS? dataReference+
    ;

recordKeyName
    : cobolWord
    ;

// ISO §12.4.5.6.2 — SUPPRESS [WHEN] literal-1: alternate-key suppression (a COBOL-2023 addition). literal-1 is the
// key suppression value: a record's alternate access path is withheld when the key equals literal-1 (§12.4.5.6.4
// GR6). Fixed order — DUPLICATES precedes SUPPRESS. Parses at all editions (superset); introduction-gated at 2023
// by VersionConformancePass ParseArm.VisitAlternateKeySuppressWhen (recognition-fire on this dedicated rule, NOT
// on the WHEN token — so the gate survives the omission; probed, `alt85-suppress-bare-gated` → COBOLNET0900).
// ⛔ WHEN IS AN OPTIONAL WORD (§8.3.2.4.3: an un-underlined uppercase word "may be specified at the user's option
// with no effect on the semantics of the format"; kb/Work PB695 family 3). MEASURED on printed page 350 / folio
// 320 off the PDF's vector rectangles, not read off the OCR: SUPPRESS carries a rule at x 254.17–299.45 under a
// box of 252.66–300.40 (94.8% cover) and DUPLICATES one at 95.8%, while WHEN's box 303.50–334.12 has NO
// horizontal rule anywhere in its band — the nearest ends 4 pt to its left. SUPPRESS stays the required anchor,
// so the phrase can never match empty, and the operand cannot begin with the WHEN token.
// ⛔ literal-1 IS A LITERAL POSITION, SO IT IS THE VALUE CLAUSE'S OPERAND RULE, NOT `literal` (kb/Work PB1072).
// §13.10.3 SR2 lets a constant-name stand "anywhere that a format specifies a literal", and §12.4.5.6.3 SR7 admits
// every figurative constant, the Format-7 symbolic-character among them — both are spelled as ordinary WORDS, which
// `literal` refused at parse time (COBOL0309). `valueClauseOperand` is the deliberate superset the data division's
// ONE literal-position chokepoint (DataBinder.RawValueOperandText) screens by the symbol tables, exactly as it does
// for the VALUE clause and the PICTURE EDITING phrase (kb/Work PB778).
alternateKeySuppressWhen
    : SUPPRESS WHEN? valueClauseOperand
    ;

// ISO §12.4.5.8.2: only STATUS is a required keyword (FILE STATUS IS data-name-1, STATUS underlined);
// FILE and IS are optional, so a bare "STATUS data-name" is a valid FILE STATUS clause.
fileStatusClause
    : FILE? STATUS IS? dataReference
    ;

// Leniency L2 (see docs/dialect-strictness.md): ISO §12.4.5.13 requires `RELATIVE KEY IS data-name`,
// but the CCVS suite writes `RELATIVE data-name` without KEY (e.g. RL109A `RELATIVE RL-FR1-KEY`). The
// grammar parses the permissive superset `RELATIVE KEY? IS? dataReference`; the no-KEY form is accepted
// in DialectMode.Default and diagnosed under named-strict modes by
// DialectStrictnessChecks.CheckRelativeKeyNoiseWord (called from SemanticBuilder). The dataReference is
// captured as the relative key either way, so random/dynamic WRITE/REWRITE/DELETE position correctly.
relativeKeyClause
    : RELATIVE KEY? IS? dataReference
    ;

// I-O-CONTROL. The paragraph holds one or more clauses terminated by a period; per ISO §12.4.6 the
// clauses are not individually period-terminated, but compilers commonly tolerate a period after each,
// so accept an optional period after every clause (SQ206A writes two SAME clauses before one period).
ioControlParagraph
    : I_O_CONTROL DOT (ioControlClause DOT?)*
    ;

ioControlClause
    : sameClause
    // The DECLINED A.4.3 APPLY COMMIT clause (ISO §12.4.6.3; Grammar/Core/CobolDeclined.g4) — recognized so
    // DataBinder refuses it BY NAME (COBOLNET1709). It must precede unrecognizedClause, which would otherwise
    // match the bare word APPLY and then die on COMMIT with an unnamed error. {is2023()}? because the whole
    // commit-and-rollback facility is a COBOL-2023 addition (Annex E.3.2 item 2) — below 2023 APPLY and
    // COMMIT are user-defined words and the clause simply does not exist.
    | {is2023()}? applyCommitClause
    | multipleFileClause
    | rerunClause
    // ⛔ THE §12.4.6.2 GENERAL FORMAT IS CLOSED (kb/Work PB829; RENDERED from the printed page — PDF p363 /
    // folio 333): `I-O-CONTROL. [[apply-commit-clause].] [[{same-clause}…].]` and nothing else, plus the
    // COBOL-85 RERUN and MULTIPLE FILE TAPE clauses removed at COBOL-2002 (superset-parsed, removal-gated
    // post-bind). ⚠ THIS is the alternative kb/Work PB487's sibling sweep missed: it is INLINE, not a named
    // `xxxClause : genericClause` wrapper, so a grep read by rule NAME skips it. It is NOT a vendor hook (see
    // CobolExpressions.g4#unrecognizedClause).
    | unrecognizedClause   // ⛔ LAST — refused BY NAME by ClosedFormatPass (COBOLNET1970)
    ;

// ISO §12.4.6.4 SAME clause. In every format only SAME (and RECORD/SORT/SORT-MERGE) is a required word;
// AREA and FOR are optional words (Format 1 `SAME AREA FOR file-1 …` underlines only SAME), so the
// "SAME AREA" clause may be written `SAME file-1 file-2` (as SQ206A does). Files may be comma-separated.
sameClause
    : SAME (RECORD | SORT | SORT_MERGE)? AREA? FOR? fileName (COMMA? fileName)*
    ;

// MULTIPLE FILE TAPE clause (obsolete; removed from later standards). Describes several files sharing
// one physical reel — irrelevant to disk storage, so parsed and ignored.
multipleFileClause
    : MULTIPLE FILE TAPE? (CONTAINS? multipleFileTapeEntry (COMMA? multipleFileTapeEntry)*)?
    ;

multipleFileTapeEntry
    : fileName (POSITION integerLiteral)?
    ;

// X3.23-1985 RERUN clause (I-O-CONTROL) — a checkpoint hint stating WHEN rerun records are written; the
// rerun mechanism itself is implementor-defined, so a null rerun facility accepts and ignores it (parsed
// and ignored, the MULTIPLE FILE posture). Obsolete '85 element DELETED by ISO 2002 (RERUN is absent from
// the 2023 text entirely, §8.9 included); the EditionValidator flags it COBOLNET0902 ≥2002
// (`rerun-removed-2002`, VCR Table 7 row 7.15).
//   RERUN [ON {file-name-1 | implementor-name-1}]
//         EVERY { [END OF] {REEL|UNIT} OF file-name-2
//               | integer-1 RECORDS [OF file-name-2]
//               | integer-2 CLOCK-UNITS
//               | condition-name-1 }
// The ON operand and condition-name-1 (a switch-status condition) are both plain words → cobolWord.
rerunClause
    : RERUN (ON cobolWord)? EVERY rerunEvery
    ;

rerunEvery
    : (END OF)? (REEL | UNIT) OF fileName
    | integerLiteral (RECORDS (OF fileName)? | CLOCK_UNITS)
    | cobolWord
    ;

// ==========================================
// OPEN / CLOSE (§14.9.27, §14.9.6)
// ==========================================

openStatement
    : OPEN openClause+
    ;

openClause
    : openMode (sharingPhrase)? ({is2002() || retryPhraseAhead()}? retryPhrase)? openFileSpec+   // both phrases superset-parsed, gated at BIND (BindOpen → Check(FileSharingClause2002) / GateRetryIntro → Check(RetryPhrase2002)). SHARING is unambiguous; RETRY sits in the openFileSpec+ name-list, so below 2002 (where RETRY is a legal §8.9 user word) the forward-detect retryPhraseAhead() only enters the phrase on an UNAMBIGUOUS numeric tail (n TIMES | FOR? n SECONDS — an integer can never be a file name) with a file name still to follow; RETRY FOREVER / bare RETRY stay file names (fail-safe).
    ;

// COBOL-2002 OPEN SHARING phrase (ISO §14.9.27) — overrides the file-control SHARING clause for this OPEN.
sharingPhrase
    : SHARING WITH? sharingMode
    ;

// COBOL-2002 RETRY phrase (ISO §14.7.9) on OPEN / READ / WRITE / REWRITE / DELETE — how to react to a lock.
retryPhrase
    : RETRY (arithmeticExpression TIMES | FOR? arithmeticExpression SECONDS | FOREVER)
    ;

// ⛔ THE LOCK-RETENTION BRACKET, AND ONLY IT — `[ WITH LOCK | WITH NO LOCK ]`. Every printed general format
// that has this phrase prints it as its OWN plain bracket of exactly two alternatives: READ (§14.9.30.2, PDF
// p722, Format 1 y=339.26 and Format 2 y=547.48 measured with `figure_geometry.py 722`), WRITE (§14.9.51.2,
// p815/p816) and REWRITE (§14.9.35.2, p740). `WITH` carries NO underline rule on any of those pages
// (`figure_extract.py` reports `WITH _LOCK_` / `WITH _NO_ _LOCK_`), so it is an optional word (§5.2.3).
// Order: WITH? NO LOCK before WITH? LOCK so `WITH NO LOCK` is not shadowed by `WITH LOCK`.
//
// ⛔ `IGNORING LOCK` WAS AN ALTERNATIVE HERE UNTIL kb/Work PB331, AND THAT COLLAPSE INVERTED TWO CARDINALITIES
// AT ONCE. IGNORING LOCK belongs to READ's OTHER bracket (`readLockContentionPhrase` below), which §5.2.6.1
// makes INDEPENDENT of this one — "a unique combination of possibilities from a series of brackets" — so the
// merge made the legal `IGNORING LOCK WITH NO LOCK` a syntax error while leaving `ADVANCING ON LOCK RETRY 3
// TIMES IGNORING LOCK` (three alternatives out of ONE bracket, which §5.2.6.2 admits one of) accepted. It also
// offered IGNORING LOCK on WRITE and REWRITE, whose printed formats do not have it at all.
recordLockPhrase
    : WITH? NO LOCK
    | WITH? LOCK
    ;

// Each opened file may carry an obsolete tape phrase (ISO §14.9.27): REVERSED or WITH NO REWIND —
// vertical/tape positioning hints with no effect on disk files, parsed and ignored.
openFileSpec
    : dataReference (REVERSED | WITH? NO REWIND)?
    ;

openMode
    : INPUT
    | OUTPUT
    | I_O
    | EXTEND
    ;

closeStatement
    : CLOSE closeFilePhrase (closeFilePhrase)*
    ;

closeFilePhrase
    : fileName closeOption?
    ;

closeOption
    : (REEL | UNIT) (FOR? REMOVAL)?
    | WITH? NO REWIND
    | WITH? LOCK
    ;

// ==========================================
// READ (§14.9.30 — full expansion)
// ==========================================

// ⛔ THE PHRASE ORDER BELOW IS THE PRINTED ONE, AND THAT IS A NORMATIVE REQUIREMENT, NOT A STYLE CHOICE.
// §5.2.1: "The words, phrases, clauses, punctuation, and operands in each general format shall be written in
// the compilation group in the sequence given in the general format, unless otherwise specified by the rules of
// that format." The printed READ formats (§14.9.30.2, PDF p722 — RENDERED, not read off the transcription) are:
//
//   Format 1 (sequential)  READ f { NEXT | PREVIOUS } RECORD [ INTO id ]
//                          [ ADVANCING ON LOCK | IGNORING LOCK | retry-phrase ]
//                          [ WITH LOCK | WITH NO LOCK ]
//                          [| AT END … | NOT AT END … |]   [ END-READ ]
//   Format 2 (random)      READ f RECORD [ INTO id ]
//                          [ IGNORING LOCK | retry-phrase ]
//                          [ WITH LOCK | WITH NO LOCK ]
//                          [ KEY IS { data-name-1 | record-key-name-1 } ]
//                          [| INVALID KEY … | NOT INVALID KEY … |]   [ END-READ ]
//
// ⛔ THE KEY PHRASE FOLLOWS THE TWO LOCK BRACKETS; IT PRECEDED THEM HERE UNTIL kb/Work PB331, AND BECAUSE
// ANTLR takes the first matching alternative in written order, the parser had already passed the `readKey`
// slot by the time it consumed a lock phrase — so the SPEC SPELLING `READ IXF RECORD WITH NO LOCK KEY IS
// IX-KEY` was `error COBOL0001: no viable alternative at input 'KEY'` while the unprinted reverse compiled.
// This rule is the superset of both formats (the format is DECIDED at bind time from the file's organization
// and access mode — §14.9.30.3 SR6/SR8/SR9 in KeyedIoBinder.BindRead), which is why one ordering has to serve
// both; the two formats agree on the order of every phrase they share, so the superset is unambiguous.
readStatement
    : READ (fileName | FILE fileName)
      readDirection?
      RECORD?
      readInto?
      readLockContentionPhrase?   // bracket 1 (§14.9.30.2) — at most ONE of ADVANCING ON LOCK / IGNORING LOCK / retry-phrase
      recordLockPhrase?           // bracket 2 (§14.9.30.2) — at most ONE of WITH LOCK / WITH NO LOCK; INDEPENDENT of bracket 1 (§5.2.6.1)
      readKey?
      readAtEnd?
      readInvalidKey?
      END_READ?

    ;

// ⛔ BRACKET 1 OF THE READ LOCK OPTIONS — one grammar object for one printed bracket, which is what keeps
// "at most one of these three" written down in exactly ONE place. §5.2.6.2: a plain bracket admits "the syntax
// element contained within the brackets or one of the alternatives contained within the brackets"; the printed
// bracket carries NO choice indicators (`figure_geometry.py 722`: Format 1 y=280.90 h=48.79 over three stacked
// alternatives, plain stems — contrast the AT END group at y=377.08, which the same tool flags
// `<-- CHOICE INDICATORS (5.2.6.4)`), so the three alternatives are mutually exclusive.
//
// SUPERSET NOTE: Format 2 prints only two alternatives (it has no ADVANCING ON LOCK). The ADVANCING arm's
// Format-1-only restriction is §14.9.30.3 SR6, enforced at bind time (KeyedIoBinder.BindRead names ADVANCING
// in its forbidden-phrase list for ACCESS MODE RANDOM), so the parse rule stays the union of both formats.
readLockContentionPhrase
    : readAdvancingOnLock
    | readIgnoringLock
    | retryPhrase   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated in the VersionConformancePass parse arm (VisitRetryPhrase → Check(RetryPhrase2002))
    ;

// COBOL-2002 READ … ADVANCING ON LOCK (ISO §14.9.30 GR22): skip-scan locked records on NEXT/PREVIOUS.
// Introduction-gated in the VersionConformancePass parse arm (VisitReadAdvancingOnLock -> Check(RecordLockPhrase2002)),
// beside its two printed siblings; it was gated on BoundRead/BoundKeyedRead.AdvancingOnLock until kb/Work PB353,
// which measured that a READ the binder refuses never builds the node and so never named the edition.
// `ON` is an OPTIONAL WORD: printed page 722 underlines ADVANCING and LOCK and NOT ON (`figure_extract.py 722`
// → `_ADVANCING_ ON _LOCK_`), and §5.2.3 makes a non-underlined word one that "may be specified" — so
// `READ … ADVANCING LOCK` is conforming source. It was rejected `error COBOL0001: missing token before 'LOCK'`
// until kb/Work PB331 measured the page.
readAdvancingOnLock
    : ADVANCING ON? LOCK
    ;

// COBOL-2002 READ … IGNORING LOCK (ISO §14.9.30 GR12): make the record available even if it is locked.
// Both words are underlined on p722 (`_IGNORING_ _LOCK_`), so both are required. Introduction-gated in the
// VersionConformancePass parse arm (VisitReadIgnoringLock → Check(RecordLockPhrase2002)), which is where its
// former host `recordLockPhrase` gates the retention bracket — the split must not lose the 2002 gate.
readIgnoringLock
    : IGNORING LOCK
    ;

// The Format-1 direction, printed as BRACES: `READ file-name-1 { NEXT | PREVIOUS } RECORD` (§14.9.30.2, PDF
// p722). NEXT is NOT underlined there (`figure_extract.py 722`) and PREVIOUS is, so by §5.2.6.3 the NEXT
// alternative "contains only optional words" and is "the default … selected unless another alternative is
// explicitly specified" — which is why the whole rule is optional at the call site and why §14.9.30.3 SR8/SR9
// say the NEXT phrase is implied.
// ⛔ `RECORD` IS NOT PART OF THIS RULE. It is the statement's own optional word, printed ONCE after the braces,
// and `readStatement` already carries it. Writing it here as well put ONE optional word in TWO places, and the
// grammar then accepted it TWICE: `READ SQF NEXT RECORD RECORD … END-READ` compiled clean (kb/Work PB331's
// sweep of §14.9.30.2 — the same figure whose lock brackets that note repaired).
readDirection
    : NEXT | PREVIOUS
    ;

readInto
    : INTO dataReference
    ;

readKey
    : KEY IS? dataReference
    ;

// AT is an optional word in the at-end phrase (ISO §6 optional-word rule; the CCVS suite and
// mainstream compilers accept "READ … RECORD END …" without AT).
// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
readAtEnd
    : AT? END statementBlock
      (NOT AT? END statementBlock)?
    | NOT AT? END statementBlock
      (AT? END statementBlock)?
    ;

// ⚠ NOT A LENIENCY — THIS IS CONFORMANCE. Recorded as "Leniency L1" on the reasoning that KEY is
// "unbracketed → required". That criterion is wrong: ISO 5.2.2/5.2.3 make UNDERLINING the test for a required
// word, not bracketing. Bracketing marks whether a whole PHRASE may be omitted; underlining marks whether a
// WORD within it must be written. Measured on the printed pages, INVALID carries an underline rule and KEY
// carries none in ALL FIVE statements that have the phrase — DELETE (p635), READ (p722), REWRITE (p740),
// START (p784) and WRITE (p816) — so `INVALID <imperative>` is conforming ISO. The same mistake was made for
// the RECORD KEY clause above (p359: RECORD and SOURCE underlined, KEY and IS not) and for COLLATING in
// SORT/MERGE (p687, p776). The legacy compiler still DIAGNOSES these under strict/warn modes, i.e. it reports
// conforming source; that is filed as SR2 in CONFORMANCE-FIX-QUEUE.md. The grammar accepting them is correct.
// Retained history: the CCVS suite
// and 1980s/90s compilers tolerate "INVALID <imperative>" without it. The dropped 'KEY' is accepted
// in DialectMode.Default and diagnosed under named-strict modes by
// DialectStrictnessChecks.CheckInvalidKeyNoiseWord (called from FileIoBinder). 'INVALID' is a reserved
// word, so this relaxation is unambiguous. Applies to all five INVALID KEY phrases below.
// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
readInvalidKey
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// WRITE (§14.9.51 — full expansion)
// ==========================================
// ⛔ THE CLAUSE NUMBER HERE READ "§14.9.46" UNTIL 2026-09-02, AND §14.9.46 IS THE **TERMINATE** STATEMENT
// (`cite.py --check 14.9.46` → "TERMINATE statement"). The WRITE statement is §14.9.51
// (`--check 14.9.51.1` → "The WRITE statement releases a logical record for an output or input-output
// file."). The same file's BODY comments cite §14.9.51 correctly throughout — the header was the one stale
// copy, which is exactly CLAUDE.md rule 1's inherited-citation failure: never re-derived, and it propagated
// into the A.4.13 module survey. Its REWRITE twin below carried the same defect.
//
// `FILE fileName` is a DISTINCT parse alternative because it is Annex A.4.13's optional element (item 2),
// which this implementation does NOT claim — the binder needs to SEE the arm to refuse it by name
// (SequentialIoBinder.DeclinedFilePhrase → COBOLNET1706). Both printed formats — Format 1 (sequential) and
// Format 2 (random) — carry the same `{ record-name-1 | FILE file-name-1 }` choice, so ONE alternative
// serves both and the sequential/keyed reroute below inherits the refusal.

writeStatement
    : WRITE (recordName | FILE fileName)
      writeFrom?
      writeBeforeAfter?
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      (recordLockPhrase)?   // introduction-gated in the VersionConformancePass parse arm (VisitRecordLockPhrase → Check(RecordLockPhrase2002))
      writeAtEndOfPage?
      writeInvalidKey?
      END_WRITE?

    ;

// ⛔ ADDITIVE, not a rewrite to `moveSendingOperand` — and the reason is worth recording. §14.9.51.4 GR5a makes this phrase equivalent to `MOVE identifier-1 TO record-name-1`,
// so a function-identifier belongs here: §8.4.3.1.2 Format 1 makes one an IDENTIFIER and §8.4.3.2.3 SR1
// bars it only from RECEIVING operands, so `FROM FUNCTION UPPER-CASE(X)` was legal source we rejected
// with COBOL0001 (fix-queue PB10). Replacing the alternatives with a single shared rule is the tidier
// shape and was tried FIRST; it deletes the generated `.dataReference()`/`.literal()` accessors and so
// breaks ~8 call sites across BOTH compilers — this grammar is shared with the legacy
// `CobolSharp.Compiler`, which survives until the P15 cut-over. Adding an alternative keeps every
// existing accessor, so the legacy binders compile untouched. The unification belongs to P15, when the
// legacy side is deleted rather than migrated.
writeFrom
    : FROM (functionCall | inlineMethodInvocation | dataReference | literal)
    ;

// ⛔ THIS RULE READ `writeAdvancePhrase writeAdvancePhrase?` — THE WHOLE PHRASE REPEATED — UNTIL kb/Work PB712,
// and it was wrong in BOTH directions: it REJECTED the only spelling §14.9.51.2 prints for the combination
// (`WRITE R BEFORE AFTER ADVANCING 4 LINES` → COBOL0001 "no viable alternative at input 'AFTER'") and ACCEPTED
// a two-ADVANCING-operand form the standard never prints and its general rules cannot interpret
// (`WRITE R BEFORE ADVANCING 4 LINES AFTER ADVANCING 0 LINES`). The 2023 introduction gate fired correctly on
// the second phrase, which is exactly what kept the over-acceptance invisible: the EDITION was checked, so the
// SPELLING never was.
//
// MEASURED off the canonical PDF, not read from the transcription (`python scripts/spec/clause_page.py
// 14.9.51.2` → PDF page 815, printed folio 785; `python scripts/render-spec-page.py 815`). Format 1's
// print-control phrase is ONE optional bracket:
//
//     [  { | BEFORE | }  ADVANCING  { { identifier-2 | integer-1 } [ LINE | LINES ] } ]
//        { |  AFTER | }             { { mnemonic-name-1 | PAGE }                    }
//
// The choice indicators — the `|` bars — enclose ONLY the two WORDS. `ADVANCING` and the whole operand group sit
// OUTSIDE them, inside the single bracket, ONCE. ISO §5.2.6.4: "When enclosed by braces, one or more of the
// alternatives contained within the choice indicators shall be specified, but any single alternative shall be
// specified only once. … The alternatives may be specified in any order." So the four printed spellings are
// `BEFORE ADVANCING …`, `AFTER ADVANCING …`, `BEFORE AFTER ADVANCING …` and `AFTER BEFORE ADVANCING …` —
// always ONE `ADVANCING`, ONE operand. `ADVANCING`, `LINE` and `LINES` are NOT underlined: optional words.
//
// The combined form is a COBOL-2023 addition (Annex §E.3.3 item 2, "Both BEFORE and AFTER are allowed together
// in WRITE ADVANCING") and is introduction-gated on the CO-OCCURRENCE OF THE TWO WORDS in
// VersionConformancePass.VisitWriteBeforeAfter — never on a second phrase, which no longer exists. §14.9.51.3
// SR17 then forbids PAGE with the pair (StatementValidation.CheckWriteBeforeAfterPage). A single BEFORE or a
// single AFTER is edition-invariant (85+).
writeBeforeAfter
    : (BEFORE AFTER? | AFTER BEFORE?) ADVANCING?
      ( PAGE
      | (dataReference | integerLiteral | literal) (LINE | LINES)?
      )
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
writeAtEndOfPage
    : AT? (END_OF_PAGE | EOP) statementBlock
      (NOT AT? (END_OF_PAGE | EOP) statementBlock)?
    | NOT AT? (END_OF_PAGE | EOP) statementBlock
      (AT? (END_OF_PAGE | EOP) statementBlock)?
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
writeInvalidKey
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

recordName
    : dataReference
    ;

// ==========================================
// REWRITE (§14.9.35)
// ==========================================
// ⛔ THIS HEADER READ "§14.9.36" UNTIL 2026-09-02, AND §14.9.36 IS THE **ROLLBACK** STATEMENT
// (`cite.py --check 14.9.36` → "ROLLBACK statement"). REWRITE is §14.9.35 (`--check 14.9.35.1` → "The
// REWRITE statement logically replaces a record existing in a mass storage file."). The WRITE twin above
// carried the identical defect — one stale citation written down twice.
//
// `RECORD` is an OPTIONAL WORD of §14.9.35.2's printed general format (RENDERED, PDF p710: `REWRITE
// { record-name-1 | FILE file-name-1 } RECORD [ FROM … ]`, with RECORD NOT underlined). It was absent from
// this rule, so `REWRITE REC RECORD FROM WS` — legal source at every edition, on the MANDATORY record-name
// arm — was rejected outright. That is an under-accept independent of A.4.13 and is fixed here rather than
// only on the FILE arm, because half a fix on a two-arm construct is the shape that keeps coming back
// (feedback_two_arm_dispatch). WRITE's two formats carry NO such word — do not mirror it there.

rewriteStatement
    : REWRITE (recordName | FILE fileName) RECORD?
      rewriteFrom?
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      (recordLockPhrase)?   // introduction-gated in the VersionConformancePass parse arm (VisitRecordLockPhrase → Check(RecordLockPhrase2002))
      rewriteInvalidKeyPhrase?
      END_REWRITE?

    ;

// ⛔ ADDITIVE, not a rewrite to `moveSendingOperand` — and the reason is worth recording. §14.9.35.4 makes this phrase the same MOVE,
// so a function-identifier belongs here: §8.4.3.1.2 Format 1 makes one an IDENTIFIER and §8.4.3.2.3 SR1
// bars it only from RECEIVING operands, so `FROM FUNCTION UPPER-CASE(X)` was legal source we rejected
// with COBOL0001 (fix-queue PB10). Replacing the alternatives with a single shared rule is the tidier
// shape and was tried FIRST; it deletes the generated `.dataReference()`/`.literal()` accessors and so
// breaks ~8 call sites across BOTH compilers — this grammar is shared with the legacy
// `CobolSharp.Compiler`, which survives until the P15 cut-over. Adding an alternative keeps every
// existing accessor, so the legacy binders compile untouched. The unification belongs to P15, when the
// legacy side is deleted rather than migrated.
rewriteFrom
    : FROM (functionCall | inlineMethodInvocation | dataReference | literal)
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
rewriteInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// DELETE RECORD (§14.9.10)
// ==========================================

deleteStatement
    : DELETE fileName RECORD?
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      deleteInvalidKeyPhrase?
      END_DELETE?

    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
deleteInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// DELETE FILE (§14.9.10 — COBOL 2023)
// ==========================================

deleteFileStatement
    : DELETE FILE OVERRIDE? fileName
      // §14.9.10.2 Format 2's {file-name-1}… repetition (kb/Work PB134; GR12 — as-if one statement per
      // name). A phrase keyword (RETRY …) ends the list through the ONE generated `{!keywordContinuesHere()}?`
      // on cobolWord's keyword alternatives (kb/Work PB805) — this loop's follow set holds the RETRY / ON
      // EXCEPTION / END-DELETE phrases — never through the five-word hand predicate that stood here.
      fileName*
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      deleteFileOnException?
      END_DELETE?

    ;

// COBOL-2002 UNLOCK statement (ISO §14.9.47): release all this-connector record locks on the file.
unlockStatement
    : UNLOCK fileName (RECORD | RECORDS)?
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
// ⛔ TWO ARMS, AND ONLY ONE OF THEM HAD BEEN RELAXED (kb/Work PB695 family 3). PB134 made ON optional in the
// POSITIVE-first arm and left the NEGATIVE-first arm demanding it, so `DELETE FILE F NOT EXCEPTION …` — the same
// omission, in the arm §5.2.6.4's choice indicators say is equally legal — stayed a COBOL0001. MEASURED on
// printed page 635 / folio 605: in `[ ON EXCEPTION … ]` ON's box 101.42–115.69 has no rule (EXCEPTION's is at
// 120.18–172.23, 95.5%), and in `[ NOT ON EXCEPTION … ]` NOT is underlined (89.5%) while its ON's box
// 125.82–140.09 again has none. All four occurrences are therefore optional words (§8.3.2.4.3).
deleteFileOnException
    : ON? EXCEPTION statementBlock
      (NOT ON? EXCEPTION statementBlock)?   // ON is not underlined (§5.2.3 optional word; kb/Work PB134)
    | NOT ON? EXCEPTION statementBlock
      (ON? EXCEPTION statementBlock)?
    ;

// ==========================================
// START (§14.9.41)
// ==========================================

startStatement
    : START fileName
      (FIRST | LAST | startKeyPhrase)?
      startInvalidKeyPhrase?
      END_START?

    ;

// ISO §14.9.41.2 prints `KEY relational-operator { data-name-1 | record-key-name-1 }` — the operator carries NO
// brackets on the printed page (measured at 600 dpi, PDF page 784 / printed folio 754), so it is REQUIRED
// whenever the KEY phrase is written. It was `comparisonOperator?` under a comment asserting "the operator may
// be omitted (then EQUAL is assumed)" on a §14.9.41 citation, and §14.9.41.4 GR15 says the opposite of what that
// comment claimed: EQUAL is implied only "If the KEY phrase is not specified" — never when the operator alone
// is. So `START f KEY IS K` compiled and ran, an UNDER-REJECTION of a spelling the format does not print
// (kb/Work PB332). The separate `IS?` went with it: every `comparisonOperator` alternative already absorbs its
// own leading IS (§8.8.4.2.2), so the outer one added nothing but `KEY IS IS = K`.
// The operand SET is a different question — `comparisonOperator` is the condition language's whole operator
// list, wider than the §8.8.4.2 general-relation format SR3 admits here (kb/Work PB333).
startKeyPhrase
    : KEY comparisonOperator dataReference startWithLength?   // WITH LENGTH introduction-gated in the VersionConformancePass parse arm (VisitStartWithLength → Check(StartWithLength2002))
    ;

// ISO §14.9.41.2 prints `[ WITH LENGTH arithmetic-expression-1 ]` with LENGTH underlined and WITH NOT
// underlined (measured off the vector rectangles — `figure_extract.py 784` → `[ WITH _LENGTH_ … ]`, confirmed on
// the 600 dpi render of PDF page 784 / printed folio 754; the transcription's own figure note says the same).
// §5.2.3 makes a non-underlined uppercase word an OPTIONAL word that "may be written to add clarity", so
// `START f KEY IS >= K LENGTH 3` is conforming source; it was rejected `COBOL0001: missing token before '3'`
// with a COBOL0306/COBOL0307 cascade behind it until kb/Work PB332. The whole PHRASE stays optional at its use
// site — that is the printed bracket — and LENGTH remains required inside it.
startWithLength
    : WITH? LENGTH arithmeticExpression
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
startInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// SORT (§14.9.40)
// ==========================================

// SORT Format 1 (file sort): requires USING/GIVING or INPUT/OUTPUT PROCEDURE
// SORT Format 2 (table sort, §14.9.40): no USING/GIVING — in-place sort
// Disambiguation deferred to semantic layer (file vs table target).
// The key-phrase list is `*`, not `+` (kb/Work PB846): the printed Format 2 BRACKETS the KEY phrase, and
// §14.9.40.3 SR15 — "The KEY phrase may be omitted only if the description of the table referenced by data-name-2
// contains a KEY phrase" — is a rule about the RESOLVED table, so it is screened in SortBinder (with Format 1's
// braced at-least-one), never by the parser. MERGE has no table format, so mergeKeyPhrase+ is correct as it is.
sortStatement
    : SORT sortFileName
      sortKeyPhrase*
      sortDuplicatesPhrase?
      sortCollatingPhrase?
      ( ( sortUsingPhrase | sortInputProcedurePhrase )
        ( sortGivingPhrase | sortOutputProcedurePhrase ) )?
      END_SORT?

    ;

sortFileName
    : dataReference
    ;

sortKeyPhrase
    : ON? (ASCENDING | DESCENDING) KEY? dataReferenceList?
    ;

// [ WITH DUPLICATES IN ORDER ] (ISO §14.9.40.2, Formats 1 and 2 — the phrase that buys §14.9.40.4 GR3's defined
// release order for records equal on every key, in place of GR4's undefined one).
// ⛔ MEASURED ON THE PRINTED PAGE, not inferred (kb/Work PB704): of the four words only DUPLICATES carries an
// underline rule, so WITH, IN and ORDER are all OPTIONAL words (§5.2.3) and every subset spelling is conforming.
// ⛔ ORDER IS A TOKEN HERE AND MAY NOT BORROW `cobolWord` (kb/Work PB704, the PB693 shape;
// DESIGN-frontend-grammar "A KEYWORD SLOT MAY NOT BORROW cobolWord"). §8.9 reserves ORDER from 2002, and the
// funnel checks IDENTIFIER occurrences POSITION-BLIND, so while the word arrived through the user-word rule this
// whole phrase was answered with `'ORDER' is a reserved word … and cannot be used as a user-defined word` at
// 2002/2014/2023 — the only phrase that can demand GR3 was unusable. The token also ends an under-rejection the
// name slot allowed: `WITH DUPLICATES IN <any word>` no longer parses, which is what §5.2.3 requires.
sortDuplicatesPhrase
    : WITH? DUPLICATES IN? ORDER?
    ;

sortCollatingPhrase
    // COLLATING is required by the ISO SORT/MERGE format; the CCVS suite (ST139A) writes the phrase as
    // `SEQUENCE alphabet-name` with COLLATING omitted, so the keyword is OPTIONAL in the permissive
    // superset and the omission is flagged under strict modes (leniency L5, docs/dialect-strictness.md).
    // COLLATING SEQUENCE {IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS alphabet-name-1 |
    // FOR NATIONAL IS alphabet-name-2}…} (ISO §14.9.40.2 / §14.9.24.2). alphabet-name-2 + the FOR forms
    // are the 2002 national class — gated on recognition (VisitSortCollatingPhrase).
    : COLLATING? SEQUENCE (collatingForPhrase+ | IS? cobolWord (cobolWord)?)
    ;

// The ONE FOR-class collating subrule (ISO §12.3.6.2 OBJECT-COMPUTER / §14.9.40.2 SORT — the PROGRAM
// COLLATING SEQUENCE clause
// and the SORT/MERGE COLLATING SEQUENCE phrase share it).
// ⛔ FOR IS AN OPTIONAL WORD IN EVERY FORMAT THAT PRINTS THIS PHRASE (kb/Work PB695) — measured, not inferred,
// because this ONE rule serves three of them: OBJECT-COMPUTER §12.3.6.2 (folio 285 rules {ALPHANUMERIC,
// CLASSIFICATION, LOCALE, NATIONAL, OBJECT-COMPUTER., SEQUENCE, SYSTEM-DEFAULT, USER-DEFAULT}), the file-control
// COLLATING SEQUENCE clause §12.4.5.7.2 (folio 322 rules {ALPHANUMERIC, NATIONAL, OF, SEQUENCE}) and SORT/MERGE
// §14.9.40.2 (folio 745 rules {ALPHANUMERIC, …, NATIONAL, SEQUENCE, …}). FOR appears in none of the three, so
// relaxing the shared rule accepts no illegal source at any of its sites.
collatingForPhrase
    : FOR? (ALPHANUMERIC | NATIONAL) IS? cobolWord
    ;

sortUsingPhrase
    : USING dataReferenceList
    ;

sortGivingPhrase
    : GIVING dataReferenceList
    ;

sortInputProcedurePhrase
    : INPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
    ;

sortOutputProcedurePhrase
    : OUTPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
    ;

// ==========================================
// MERGE (§14.9.24)
// ==========================================

mergeStatement
    : MERGE mergeFileName
      mergeKeyPhrase+
      sortCollatingPhrase?
      mergeUsingPhrase
      ( mergeGivingPhrase | mergeOutputProcedurePhrase )?
      END_MERGE?

    ;

mergeFileName
    : dataReference
    ;

mergeKeyPhrase
    : ON? (ASCENDING | DESCENDING) KEY? dataReferenceList
    ;

mergeUsingPhrase
    : USING dataReferenceList
    ;

mergeGivingPhrase
    : GIVING dataReferenceList
    ;

mergeOutputProcedurePhrase
    : OUTPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
    ;

// ==========================================
// RETURN (§14.9.34)
// ==========================================

returnStatement
    : RETURN fileName RECORD?
      (INTO dataReference)?
      returnAtEndPhrase?
      END_RETURN?

    ;

// AT is optional in the AT END phrase (ISO §14.9.34.2 — "AT" is not underlined in RETURN's general
// format, so it is an optional word),
// and RECORD above is optional, so "RETURN f END …" and "RETURN f RECORD AT END …" both parse.
// ISO §14.9.34.3 SR4: the AT END and NOT AT END phrases may be written in REVERSED order.
//
// ⛔ THE `?` ON `returnAtEndPhrase` ABOVE IS DELIBERATE AND IS NOT A LICENCE — DO NOT MAKE IT REQUIRED.
// §14.9.34.2 prints `AT END imperative-statement-1` with NO brackets (rendered from the printed page,
// PDF p738 / folio 708 — the bracketed `[ NOT AT END … ]` and `[ END-RETURN ]` sit on the lines around
// it), so the phrase IS mandatory: §5.2.6.2 makes brackets the only convention that lets a portion of a
// format be omitted. It is enforced at BIND time — StatementValidation.ScreenOmittedRequiredPhrase from
// SortBinder.BindReturn, COBOLNET1850 — because a bind-time diagnostic can NAME the omitted phrase, the
// statement and the clause where a parse error can only report an unexpected token, and because the
// second alternative below (SR4's reversed order) would otherwise fail mid-alternative rather than
// reporting the real rule. Keeping the phrase optional here is what lets the binder see the tree and say
// so; the grammar recognizes, the binder conforms (kb/Work PB350).
// ⚠ readAtEnd is NOT the same case and must not be changed to match: §14.9.30.2 Format 1 prints READ's
// AT END / NOT AT END pair inside BRACKETS WITH CHOICE INDICATORS, which §5.2.6.4 reads as "zero or more
// of the alternatives", so there BOTH phrases really are optional. The asymmetry is the point, and it is
// why the two pages are measured separately.
returnAtEndPhrase
    : AT? END statementBlock
      (NOT AT? END statementBlock)?
    | NOT AT? END statementBlock
      (AT? END statementBlock)?
    ;

// ==========================================
// RELEASE (§14.9.32)
// ==========================================

releaseStatement
    : RELEASE dataReference
      releaseFrom?

    ;

// ⛔ ADDITIVE, not a rewrite to `moveSendingOperand` — and the reason is worth recording. §14.9.32.4 makes this phrase the same MOVE,
// so a function-identifier belongs here: §8.4.3.1.2 Format 1 makes one an IDENTIFIER and §8.4.3.2.3 SR1
// bars it only from RECEIVING operands, so `FROM FUNCTION UPPER-CASE(X)` was legal source we rejected
// with COBOL0001 (fix-queue PB10). Replacing the alternatives with a single shared rule is the tidier
// shape and was tried FIRST; it deletes the generated `.dataReference()`/`.literal()` accessors and so
// breaks ~8 call sites across BOTH compilers — this grammar is shared with the legacy
// `CobolSharp.Compiler`, which survives until the P15 cut-over. Adding an alternative keeps every
// existing accessor, so the legacy binders compile untouched. The unification belongs to P15, when the
// legacy side is deleted rather than migrated.
releaseFrom
    : FROM (functionCall | inlineMethodInvocation | dataReference | literal)
    ;

// ==========================================
// STRING (§14.9.43)
// ==========================================

stringStatement
    : STRING stringSendingPhrase+ stringIntoPhrase stringWithPointer? stringOnOverflow? END_STRING?
    ;

// ── The two STRING/UNSTRING SENDING-operand shapes, named ONCE each ──────────────────────────────────────
// The general formats distinguish them, so the grammar does too rather than repeating an alternative list in
// four productions:
//   §14.9.48.2  `UNSTRING identifier-1`               -> an IDENTIFIER only; a literal sender is not admitted.
//   §14.9.43.2  `STRING {identifier-1 | literal-1}`   -> an identifier OR a literal.
// The sender is therefore a strict SUBSET of the operand, and strUnstrOperand says so by referencing it.
//
// DA4: functionCall is FIRST in the sender — §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so
// every "identifier-N" SENDING position admits one. It is keyword-led (FUNCTION …), so it can never be shadowed
// by dataReference (ANTLR takes the first matching alternative). §8.4.3.2.3 SR1 bars a function-identifier only
// from a RECEIVING operand, which is why the INTO phrases below are untouched. A keyword-OMITTED function (a
// repository name + parens) still parses as a dataReference and is resolved by the binder's
// KeywordOmittedFunction path — unchanged.
strUnstrSender
    : functionCall | inlineMethodInvocation | dataReference
    ;

strUnstrOperand
    : strUnstrSender | literal | figurativeConstant
    ;

stringSendingPhrase
    : strUnstrOperand delimitedByPhrase?
    ;

delimitedByPhrase
    : DELIMITED BY? (ALL)? (strUnstrOperand | SIZE)
    ;

stringIntoPhrase
    : INTO dataReference
    ;

stringWithPointer
    : WITH? POINTER dataReference
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
stringOnOverflow
    : ON? OVERFLOW statementBlock (NOT ON? OVERFLOW statementBlock)?
    | NOT ON? OVERFLOW statementBlock (ON? OVERFLOW statementBlock)?
    ;

// ==========================================
// UNSTRING (§14.9.48)
// ==========================================

unstringStatement
    : UNSTRING strUnstrSender
      unstringDelimiterPhrase?
      unstringIntoPhrase+
      unstringWithPointer?
      unstringTallying?
      unstringOnOverflow?
      END_UNSTRING?

    ;

unstringDelimiterPhrase
    : DELIMITED BY? unstringDelimiterItem (OR unstringDelimiterItem)*
    ;

unstringDelimiterItem
    : (ALL)? strUnstrOperand
    ;

unstringIntoPhrase
    : INTO unstringIntoTarget+
    ;

unstringIntoTarget
    : dataReference
      (DELIMITER IN? dataReference)?
      (COUNT IN? dataReference)?
    ;

unstringWithPointer
    : WITH? POINTER dataReference
    ;

unstringTallying
    : TALLYING IN? dataReference
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
unstringOnOverflow
    : ON? OVERFLOW statementBlock (NOT ON? OVERFLOW statementBlock)?
    | NOT ON? OVERFLOW statementBlock (ON? OVERFLOW statementBlock)?
    ;

// ==========================================
// INSPECT (§14.9.22 — COBOL-85)
// ==========================================

// identifier-1 admits a FUNCTION-IDENTIFIER, but only in Format 1 (PB10). §8.4.3.1.2 Format 1 makes a
// function-identifier an IDENTIFIER, so every identifier-N position admits one unless a syntax rule excludes it —
// and §8.4.3.2.3 SR1 excludes it from a RECEIVING operand. INSPECT is FORMAT-DEPENDENT here, which is why the
// grammar cannot decide it alone: identifier-1 is SENDING only in Format 1 (TALLYING). §14.9.22.4 GR1 concedes
// only that "for purposes of determining its length, identifier-1 is treated as a sending data item" — a scoped
// concession that would be unnecessary if it were generally sending — and GR7 has each match "tallied (format 1)
// or replaced by literal-3 (format 2)", while GR20 makes format 4 execute AS a format 2 over the same
// identifier-1. So Formats 2/3/4 MODIFY it and bar a function-identifier. ⛔ THE BINDER SCREENS PER FORMAT
// (InspectBinder, COBOLNET1632); widening the grammar alone would ACCEPT ILLEGAL SOURCE.
// ⛔ ADDITIVE — an ALTERNATIVE, never a rewrite to a shared rule: this grammar is shared with the legacy
// CobolSharp.Compiler until the P15 cut-over, and collapsing the rule would DELETE the generated
// .dataReference() accessor its binder reads. The legacy binder guards on the new null instead.
inspectStatement
    : INSPECT BACKWARD? (functionCall | inlineMethodInvocation | dataReference)
      ( inspectTallyingPhrase inspectReplacingPhrase?
      | inspectReplacingPhrase
      | inspectConvertingPhrase )
    ;

// ----- TALLYING -----

inspectTallyingPhrase
    : TALLYING inspectTallyingItem+
    ;

inspectTallyingItem
    : dataReference inspectForClause+
    ;

inspectForClause
    : FOR inspectCountPhrase+
    ;

// ALL/LEADING are transitive across the bare operands that follow them (GR 10), so a
// count phrase may omit the adjective: "FOR LEADING ""S"" ""S"" ""T""" lists three
// operands. That bare form is ambiguous with the next counter in multi-counter TALLYING
// ("c1 FOR ALL x  c2 FOR ALL y"): a greedy parser swallows c2 as a pattern of c1. The
// IsBareInspectOperand() predicate resolves it — a data-name immediately followed by FOR
// is the next counter, so the bare alternative declines it and the count-phrase loop ends.
inspectCountPhrase
    : CHARACTERS inspectDelimiters?
    | (ALL | LEADING | FIRST | TRAILING) inspectChar inspectDelimiters?
    | {IsBareInspectOperand()}? inspectChar inspectDelimiters?
    ;

// ⛔ A FUNCTION-IDENTIFIER IS ADMISSIBLE HERE (ISO §8.4.3.1.2 Format 1 makes it an identifier; fix-queue PB45).
// §14.9.22.2 writes every one of these operands as `identifier-n | literal-n`, and each use of inspectChar is a
// SENDING position — the TALLYING pattern, and BOTH sides of REPLACING (the item being inspected, identifier-1, is
// the only receiver and is a separate rule). So `INSPECT S TALLYING N FOR ALL FUNCTION TRIM(X)` is conforming
// source that was a PARSE error. Additive and unambiguous: functionCall begins with the FUNCTION token, so it
// cannot be confused with the dataReference alternative or with the {IsBareInspectOperand()}? bare form above.
inspectChar
    : literal
    | functionCall
    | inlineMethodInvocation   // ISO 8.4.3.1.2 Format 4 (8.4.3.4) - the Format-1 twin above; kb/Work PB428
    | dataReference
    | figurativeConstant
    ;

// ----- REPLACING -----

inspectReplacingPhrase
    : REPLACING inspectReplacingItem+
    ;

inspectReplacingItem
    : CHARACTERS BY inspectChar inspectDelimiters?
    | (ALL | LEADING | FIRST | TRAILING)? inspectChar BY inspectChar inspectDelimiters?
    ;

// ----- CONVERTING -----

inspectConvertingPhrase
    : CONVERTING inspectChar
      TO inspectChar
      inspectBeforeAfterPhrase*
    ;

inspectBeforeAfterPhrase
    : BEFORE INITIAL_? inspectChar
    | AFTER INITIAL_? inspectChar
    ;

inspectDelimiters
    : BEFORE INITIAL_? inspectChar (AFTER INITIAL_? inspectChar)?
    | AFTER INITIAL_? inspectChar (BEFORE INITIAL_? inspectChar)?
    ;
