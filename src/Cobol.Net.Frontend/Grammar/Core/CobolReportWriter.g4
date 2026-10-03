// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// REPORT SECTION rules (ISO 1989:2023 §13.14 report description entry / §13.15 report group description
// entry; clauses §13.18.12 CODE, .14 COLUMN, .16 CONTROL, .29 GROUP-USAGE, .35 LINE, .37 NEXT GROUP,
// .39 PAGE, .41 PRESENT WHEN, .53 SOURCE, .54 SUM, .57 TYPE, .64 VARYING).
// The COBOL-85 surface plus the 2002 additions (PRESENT WHEN, VARYING, the multiple/relative COLUMN and
// multiple LINE operand forms) — superset parse; the 2002 forms are introduction-gated post-bind by the
// VersionConformancePass ParseArm (0900 below 2002). Imported by CobolParserCore.g4 — no options block of
// its own beyond tokenVocab.

parser grammar CobolReportWriter;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// REPORT SECTION
// ==========================================

reportSection
    : REPORT SECTION DOT reportDescriptionEntry*
    ;

// RD report-name [report-description-clause]... .  { constant-entry | report-group-description-entry } ...
// (§13.8.2). The brace group's ellipsis is "one or more", and §13.8.4 says "An RD entry shall be followed by one
// or more report group description entries": the grammar admits the list EMPTY and DataBinder.BindReportSection
// reports the RD with no report group entry (COBOLNET2708), exactly as the sort-merge twin's "one or more record
// description entries" is a bind-time rule (COBOLNET1837) — a rule-named diagnostic, not a parse error.
reportDescriptionEntry
    : RD reportName reportDescriptionClause* DOT (constantEntry | reportGroupEntry)*
    ;

reportName
    : cobolWord
    ;

// ⛔ THE ONE PARSE SHAPE OF A REPORT-GROUP REFERENCE (`GENERATE`'s data-name-1, §14.9.16.3 SR1; USE BEFORE
// REPORTING's identifier-1, §14.9.49.3 SR9). §8.4.2.2.2 Format 1 is `data-name-1 [ data-qualifier ] …
// [ file-report-qualifier ]`, and a report group is a LEVEL-01 entry — it has no superordinate data-name, so
// the only available qualifier is the `IN/OF report-name-1` half of file-report-qualifier. §14.9.16.3 SR1
// spells it out: "Data-name-1 shall name a detail report group. It may be qualified by a report-name."
// §8.4.2.2.3 SR3 makes IN and OF equivalent. Both consumers bind through ReportGroupResolution — the one
// funnel that turns (head, qualifier) into exactly one group and diagnoses an ambiguous reference
// (§8.4.2.2.1 / §8.4.2.2.3 SR1) instead of taking the first report that happens to carry the name.
reportGroupReference
    : cobolWord ((IN | OF) reportName)?
    ;

reportDescriptionClause
    : reportGlobalClause
    | reportCodeClause
    | reportControlClause
    | reportPageClause
    ;

// IS GLOBAL (§13.18.27)
reportGlobalClause
    : IS? GLOBAL
    ;

// CODE literal (§13.18.12) — a 2-char prefix written on every line of the report.
reportCodeClause
    : CODE IS? (literal | dataReference)
    ;

// CONTROL(S) {FINAL | data-name}... (§13.18.16) — the control hierarchy (major→minor).
reportControlClause
    : (CONTROL IS? | CONTROLS ARE?) (FINAL | dataReference)+
    ;

// PAGE [LIMIT IS | LIMITS ARE] { integer-1 | [integer-1 {LINE | LINES}] [integer-2 {COLS | COLUMNS}] }
//      [HEADING IS integer-3] [FIRST {DETAIL | DE} IS integer-4] [LAST {CONTROL HEADING | CH} IS integer-5]
//      [LAST {DETAIL | DE} IS integer-6] [FOOTING IS integer-7]    (§13.18.39.2, the PDF diagram rendered)
// The brace group is a CHOICE between integer-1 alone and the two optional brackets: so the LINE/LINES word belongs
// with integer-1 (it cannot follow an integer-1 that is followed by integer-2), and integer-2 may stand alone — the
// three alternatives below are exactly those shapes, and SR2's "either integer-1 or integer-2 or both" is the
// grammar's own (an empty brace group has no alternative). SR3 — the five trailing phrases "may be specified only
// if integer-1 is specified" — is a bind-time rule (COBOLNET2709), because the grammar cannot see which
// alternative preceded a repeated phrase list without duplicating it.
reportPageClause
    : PAGE (LIMIT IS? | LIMITS ARE?)?
      ( integerLiteral (LINE | LINES) reportPageWidth?
      | integerLiteral
      | reportPageWidth )
      reportPageSubclause*
    ;

// integer-2 {COLS | COLUMNS} — the page width (§13.18.39.4 GR2b).
reportPageWidth
    : integerLiteral (COLS | COLUMNS)
    ;

// The five trailing phrases, each its own bracket with no ellipsis (SR4 gives the order licence; §5.2.7 the
// at-most-once). SR1: FIRST DE = FIRST DETAIL, LAST CH = LAST CONTROL HEADING, LAST DE = LAST DETAIL.
// ⚠ HEADING is a terminal of TWO alternatives (the HEADING phrase and LAST CONTROL HEADING): a reader classifies
// a phrase by DataBinder.PagePhraseOf (its first token and the one after it), never by `HEADING()`.
reportPageSubclause
    : HEADING IS? integerLiteral
    | FIRST (DETAIL | DE) IS? integerLiteral
    | LAST (CONTROL HEADING | CH) IS? integerLiteral
    | LAST (DETAIL | DE) IS? integerLiteral
    | FOOTING IS? integerLiteral
    ;

// ==========================================
// REPORT GROUP DESCRIPTION ENTRY (§13.15)
// ==========================================

// level-number [entry-name-clause] [clause]... .  (§13.15.2). The entry-name clause is §13.18.20, THE SAME RULE
// as the data description entry's: data-name-1 or FILLER (§13.18.20.3 SR3 — "either data-name-1 or FILLER shall
// be specified" in a data description entry or a report group description entry; §13.18.20.4 GR1 — "The word
// FILLER may be used to name a data, report, or screen item"). The report arm reads `dataName` rather than a
// hand-written name rule of its own (kb/Work PB1226/PB1287): two arms with two name lists is the shape in which
// one of them forgets FILLER, and one entry-name rule makes the next word it admits automatic here too.
// DataBinder reads the name through CstExtensions.NameOrNull (null for the omitted and the FILLER forms).
reportGroupEntry
    : levelNumber dataName? reportGroupClause* DOT
    ;

reportGroupClause
    : reportTypeClause
    | reportLineClause
    | reportNextGroupClause
    | reportColumnClause
    | reportSourceClause
    | reportSumClause
    | reportGroupIndicateClause
    | reportPresentWhenClause
    | reportVaryingClause
    | pictureClause
    | usageClause
    | signClause
    | justifiedClause
    | blankWhenZeroClause
    | occursClause
    | valueClause
    ;

// TYPE IS {REPORT HEADING|RH | PAGE HEADING|PH | CONTROL HEADING|CH [FINAL|data] | DETAIL|DE |
//          CONTROL FOOTING|CF [FINAL|data] | PAGE FOOTING|PF | REPORT FOOTING|RF}  (§13.18.57)
reportTypeClause
    : TYPE IS? reportGroupType
    ;

reportGroupType
    : (REPORT HEADING | RH)
    | (PAGE HEADING | PH)
    | (CONTROL HEADING | CH) (reportControlName (OR PAGE)?)?
    | (DETAIL | DE)
    | (CONTROL FOOTING | CF) reportControlName?
    | (PAGE FOOTING | PF)
    | (REPORT FOOTING | RF)
    ;

// §13.18.57.2 Format 2, rendered: `{CONTROL HEADING | CH} [ [ON | FOR] {data-name-1 | FINAL} [OR PAGE] ]` and
// `{CONTROL FOOTING | CF} [ [ON | FOR] {data-name-2 | FINAL} ]` — ON and FOR are not underlined (optional words,
// §5.2.3), the whole operand phrase is ONE optional bracket (so OR PAGE needs a control operand; SR11's omission is
// of the operand), and OR PAGE exists on the heading only. The group kind is read by GroupKindOf from the phrase's
// FIRST token: `PAGE` is also a terminal of OR PAGE, so a reader keyed on PAGE() would take a control heading for a
// page heading.
reportControlName
    : (ON | FOR)? (FINAL | dataReference)
    ;

// {LINE|LINES} [NUMBER|NUMBERS] [IS|ARE] {integer [ON NEXT PAGE] | {PLUS|+} integer | [ON] NEXT PAGE}...  (§13.18.35 F1)
// The multi-operand form (a "multiple LINE clause", §13.18.35.3 SR10) and the LINES/NUMBERS/ARE spellings are
// COBOL-2002 — introduction-gated post-bind by VersionConformancePass ParseArm.VisitReportLineClause; the
// multi-operand repetition itself stages LOUD at bind (COBOLNET0899 report-multiple-line).
reportLineClause
    : (LINE | LINES) (NUMBER | NUMBERS)? (IS | ARE)? reportLineOperand+
    ;

reportLineOperand
    : reportRelativeSign integerLiteral
    | integerLiteral (ON? NEXT PAGE)?
    | ON? NEXT PAGE
    ;

// ⛔ THE ONE SPELLING OF A REPORT-WRITER RELATIVE OPERAND (kb/Work PB951). The same sentence is printed at three
// clauses — ISO §13.18.35.3 SR1 (LINE), §13.18.14.3 SR2 (COLUMN) and §13.18.37.3 SR2 (NEXT GROUP): "PLUS and + are
// synonyms." — and each general format prints the pair as a nested brace (PDF pp. 386/420/427, rendered). The three
// productions each wrote the WORD alone, so `LINE + 1` was a bare COBOL0001; one fragment referenced by every
// relative operand makes the next clause that takes one unable to regress to a single spelling
// (GrammarRelativeSignDriftTests pins it). PLUSWORD is the reserved word PLUS, PLUS the '+' symbol (CobolLexer.g4).
reportRelativeSign
    : PLUSWORD
    | PLUS
    ;

// NEXT GROUP IS {integer-1 | {PLUS|+} integer-2 | NEXT PAGE [WITH RESET]}  (§13.18.37.2, PDF p427 rendered — both
// the outer and the inner delimiters are braces). Bound by DataBinder.Reports BindNextGroupClauses (kb/Work PB957).
reportNextGroupClause
    : NEXT GROUP IS? (reportRelativeSign integerLiteral | integerLiteral | NEXT PAGE (WITH? RESET)?)
    ;

// {COLUMN|COLUMNS|COL|COLS} [NUMBER|NUMBERS] [IS|ARE] {integer | {PLUS|+} integer}...  (§13.18.14 F1)
// The multi-operand form (a "multiple COLUMN clause", §13.18.14.3 SR10), the relative PLUS operand, and the
// COL/COLS/COLUMNS/NUMBERS/ARE spellings are COBOL-2002 — introduction-gated post-bind by VersionConformancePass
// ParseArm.VisitReportColumnClause. The LEFT/CENTER/RIGHT alignment phrase has no grammar surface
// (COBOLNET_REPORT_WRITER_DESIGN §5 — the SR9 LEFT default applies).
reportColumnClause
    : (COLUMN | COLUMNS | COL | COLS) (NUMBER | NUMBERS)? (IS | ARE)? reportColumnOperand+
    ;

reportColumnOperand
    : reportRelativeSign? integerLiteral
    ;

// {SOURCE|SOURCES} [IS|ARE] {identifier-1}...  (§13.18.53 Format)
// THE OPERAND LIST IS PLURAL BY DESIGN (kb/Work PB506). §13.18.53.2's ellipsis follows the
// `{ identifier-1 / arithmetic-expression-1 }` brace pair, so the clause takes one or MORE operands, and
// §13.18.53.3 SR6 confines the multi-operand form to a repeating entry — the VALUE clause's §13.18.63.3 SR35
// written a second time for SOURCE, with §13.18.53.4 GR4 the twin of §13.18.63.4 GR23. SOURCES/ARE are the
// 2002 spellings (§13.18.53.3 SR1 — SOURCE and SOURCES are synonyms); the multi-operand form and the plural
// spellings are introduction-gated post-bind by VersionConformancePass ParseArm.VisitReportSourceClause.
// What stops the greedy operand list is the §8.9 reservation gate on cobolWord (the PB792 argument): every
// clause that can follow opens with a reserved word.
// The operand is `reportValueOperand` — identifier-1 AND arithmetic-expression-1, one production (kb/Work
// PB852). The `[ rounded-phrase ]` closing the general format sits OUTSIDE the ellipsis (PDF p485 rendered), so
// ONE phrase governs the whole clause; §13.18.53.3 SR5 then makes a ROUNDED identifier an arithmetic-expression
// and §13.18.53.4 GR2 gives it the implicit COMPUTE.
reportSourceClause
    : (SOURCE | SOURCES) (IS | ARE)? reportValueOperand+ roundedPhrase?
    ;

// ⛔ THE ONE OPERAND OF A REPORT VALUE CLAUSE (kb/Work PB852 × PB883). §13.18.53.2 (SOURCE) and §13.18.54.2
// (SUM) print the SAME operand brace — `{ identifier-1 | arithmetic-expression-1 }`, SUM adding `data-name-1`,
// which is an identifier too — so both clauses reference ONE production and a second copy cannot drift from it.
// §8.4.3.1.2 Format 2 makes an identifier a primary of an arithmetic expression, so `arithmeticExpression`
// admits the identifier form as its degenerate case and the BINDER classifies which form was written (a tree
// that is exactly one `dataReference` is identifier-1 / data-name-1; anything else is arithmetic-expression-1).
// That is also the standard's own reading: SR5 says an identifier written WITH the ROUNDED phrase "is
// considered to be an arithmetic-expression".
// ⚠ The operand list is separated by nothing but a space, so a written operator BINDS (the `(addOp
// multiplicativeExpression)*` loop is greedy): `SOURCES ARE A + B` is ONE operand. §13.18.53.3 SR7 is what makes
// that unambiguous — with more than one operand and any of them an expression, "each operand shall be enclosed
// in parentheses" — and the binder ENFORCES it (COBOLNET2142) rather than inferring it from this shape.
// What stops the greedy list is unchanged: the §8.9 reservation gate on cobolWord (the PB792 argument) — every
// clause that can follow opens with a reserved word, and so does ROUNDED.
reportValueOperand
    : arithmeticExpression
    ;

// SUM OF {data-name-1|identifier-1|arithmetic-expression-1}... [UPON data-name-2...]
//   [RESET ON {FINAL|data-name-3}] [rounded-phrase]   (§13.18.54.2)
// `OF` is an OPTIONAL word: the printed general format (§13.18.54.2, PDF p487 rendered) underlines SUM, UPON,
// RESET and FINAL and leaves OF and ON plain, and §8.3.2.4.3 makes an un-underlined uppercase word optional.
// Without it `SUM OF WS-A` — conforming source — was a raw COBOL0001 parse error (kb/Work PB482).
// The addend is `reportValueOperand`, the SAME production SOURCE uses: §13.18.54.3 SR1 — "Each data-name-1,
// identifier-1 or arithmetic-expression-1 is an addend" — and §13.18.54.4 GR3 gives an expression addend the
// COMPUTE-with-ON-SIZE-ERROR accumulation (kb/Work PB883). The RESET group and the rounded-phrase sit OUTSIDE
// the repeated `SUM … [UPON …]` group (PDF p487 rendered), so at most one of each governs the whole clause,
// however many times the SUM keyword appears (SR1) — the binder diagnoses a second one.
reportSumClause
    : SUM OF? reportValueOperand (COMMA? reportValueOperand)*
      (UPON dataReference (COMMA? dataReference)*)?
      reportSumReset? roundedPhrase?
    ;
// ⛔ `sumOperand : dataReference (OF reportName)?` IS GONE, and this comment stands where it was so the trailing
// qualifier is not re-added. It was DEAD: `dataReference`'s own `dataReferenceSuffix*` swallows `OF word` as an
// ordinary qualification before the optional tail is ever tried, the binder read only `op.dataReference()`, and
// §13.18.54.2's general format prints no qualifier on the addend at all. A cross-report addend's report-name
// qualifier is recognised where it is resolved (DataBinder.Reports ResolveSumAddend, SR4 g).

reportSumReset
    : RESET ON? (FINAL | dataReference)
    ;

// GROUP INDICATE  (§13.18.28) — print this field only on the first
// detail of a page or after a control break.
reportGroupIndicateClause
    : GROUP INDICATE?
    ;

// PRESENT WHEN condition-1  (§13.18.41 Format 1, COBOL-2002) — the entry (and its subordinates) is processed
// only when the condition is true at group-presentation time. Introduction-gated post-bind by
// VersionConformancePass ParseArm.VisitReportPresentWhenClause.
reportPresentWhenClause
    : PRESENT WHEN condition
    ;

// VARYING {data-name-1 [FROM arith-1] [BY arith-2]}...  (§13.18.64, COBOL-2002) — per-repetition counters for a
// repeating report entry (a multiple LINE/COLUMN clause or a report-group OCCURS, §13.18.64.3 SR1).
// Introduction-gated post-bind by VersionConformancePass ParseArm.VisitReportVaryingClause.
reportVaryingClause
    : VARYING reportVaryingSpec+
    ;

reportVaryingSpec
    : cobolWord (FROM arithmeticExpression)? (BY arithmeticExpression)?
    ;

// ==========================================
// REPORT WRITER PROCEDURE-DIVISION VERBS (§14.9.x)
// ==========================================

// INITIATE report-name... (§14.9.21) — begin report processing: reset LINE-COUNTER/PAGE-COUNTER + SUM counters.
initiateStatement
    : INITIATE reportName+
    ;

// GENERATE {data-name-1 | report-name-1} (§14.9.16.2) — produce a detail line (or summary reporting).
// The operand is a detail report-group name (SR1, detail reporting) or a report-name (SR2, summary
// reporting); the binder distinguishes by resolving the name against the report model. SR1's second sentence
// — "It may be qualified by a report-name" — is why the operand is a `reportGroupReference` and not a bare
// `reportName`: `GENERATE DET-A OF R-B` is legal COBOL and used to be COBOL0001 (kb/Work PB365). A qualified
// operand can only be the data-name form (a report-name has no qualifier), which the binder relies on.
generateStatement
    : GENERATE reportGroupReference
    ;

// TERMINATE report-name... (§14.9.46) — end report processing: produce final CONTROL/REPORT FOOTINGs.
terminateStatement
    : TERMINATE reportName+
    ;

// SUPPRESS PRINTING (§14.9.45) — inhibit printing of a report group for the current instance. The statement
// carries no operand (the single fixed form); §14.9.45.3 SR1 restricts it to a USE BEFORE REPORTING procedure,
// and §14.9.45.4 GR1 fixes the affected group as the one that USE procedure names — both enforced at BIND time
// (the superset grammar admits the verb anywhere; BindSuppress resolves the lexically-enclosing declarative).
// ISO 5.2.3: measured on page 795, SUPPRESS carries an underline rule (45.3 pt) and PRINTING carries NONE, so
// PRINTING is an OPTIONAL WORD and a bare `SUPPRESS` conforms.
suppressStatement
    : SUPPRESS PRINTING?
    ;
