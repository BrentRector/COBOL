## Entry 1888 — 2026-10-05 06:51 PDT — Train 1020: wave 1020 groups E, F, D (split, PB1470 held back), A, B: omitted-branch directive checks, FLAG defaults, type declarations, LINAGE-COUNTER in reports, nonstandard-extension flagging, RESERVE areas

Train 1020 carried five clusters from wave 1020, in order E, F, D, A, B. Every cluster was cut at `7546af101`, so each
patch applied without a code conflict; the only conflicts were whole-element additions to the conformance manifests and
`docs/DIAGNOSTICS.md`, kept on both sides with the element counts checked (negative manifest 2,361 to 2,389, 2023
manifest 800 to 803). Two clusters were also finishing an earlier wave's unlanded branch: E carried wave 1019 G (PB1373)
and D carried wave 1019 D (PB1301, PB1302, PB1476, with PB1415 withdrawn). The inventory hunks were not merged: the six
verdict batches were re-applied to the merged tree in order.

**E: PB2003, and wave 1019 G's PB1373.** The >>COBOL-WORDS operand had been hand-rolled against the wrong populations
(PB1373). The directive-line parse now reads the separator comma and semicolon once (§8.3.5 2), so every directive
operand gets it. The literal reader refuses an unterminated literal. SR3 and SR4 share one 2023 population (§8.9 R2023,
§8.10 and §8.11) instead of the lexer's vocabulary. A fresh word is held to the 63-character ceiling. The SR1 boundary
is read through the group's own EQUATE synonyms and judged by one COBOLNET2652 judge after the COBOL-WORDS stage.
PB2003: the omitted branch of a conditional-compilation directive consumed a >>TURN, >>FLAG-02/14 or >>COBOL-WORDS line
unseen, so `>>TURN GARBAGE` compiled clean there although §7.2.1 asks every directive to be "syntactically correct in
the initial source text and library text". `StageOperandChecks` now holds one check-only entry per owner stage. The
driver asks it for every stage-owned row in an omitted branch, and two drift tests derive the population from the
catalog. The finisher also deleted the test-only `CobolKeywordTokens.IsKeyword` (the three drift tests now ask
`TryTokenType`, the identical predicate). Re-probed: the four w1018c repros went from rc 0 to their own diagnostics.
Golden `negative/pb2003-omitted-branch-turn-operand` plus five PB1373 goldens. Rows FMT-7.3.10.2 and SR-7.3.10.3-1..4
closed. Its lead, that a VersionConformancePass diagnostic carries no source location, is filed as PB2034.

**F: PB1374, >>FLAG-02 and >>FLAG-14 take ALL and ON as the printed defaults.** The parser required the option list and,
for FLAG-14, the ON/OFF word, so `>>FLAG-14 VALUE-ZERO`, `>>FLAG-14 OFF`, a bare `>>FLAG-14` and their FLAG-02 twins drew
COBOLNET1622, while `>>FLAG-14 VALUE-ZERO VALUE-ZERO ON` compiled clean because the repeat was merged. On the rendered
pages ALL and ON carry no underline in either format, so they are optional words (§5.2.3) and the defaults of their
braces (§5.2.6.3): a bare directive is `ALL ON`, a lone `OFF` is `ALL OFF` and a lone option list is `ON`. One
`FlagDirectiveLine.TryParse` serves both directives. A repeated word or a repeated ALL is now refused under §5.2.6.4,
and so is an ON/OFF that is not the last word. The transcription's FLAG-14 figure note, which called the braces
"required", is corrected. Goldens `2023/pb1374_flag14_defaults`, `2014/pb1374_flag02_defaults` and two negatives.
Rows FMT-7.3.14.2, FMT-7.3.15.2 and GR-7.3.15.4-1..3 closed.

**D: split, and four of its notes landed without PB1470.** The wave 1019 D finish (PB1301, PB1302, PB1476) completes
every type declaration before anything copies it. `ExpandTemplate` expands its chained TYPE and every member's TYPE and
SAME AS, and a declaration reached while in progress is §13.18.58.3 SR2. `DataItem.DeclaresStrongType` is the one
strength predicate (§13.18.57.4 GR1 "coded in place"), and §8.4.2.2.1 rule 4's lapsed members join the one lookup as
candidates that count but never resolve alone. PB1415 stays open on evidence: NIST NC201A/NC211A/NC250A write `PIC 999
VALUE ALL ZEROS`. PB2018: a relation or EVALUATE operand that is an arithmetic expression now binds under `BindExpr`, so
an index-name inside it is COBOLNET1637 and an index data item COBOLNET0844. §13.18.38.3 SR7 and §8.8.4.2.13 name the
relation's own operand, kb/Work R29's wider reading is withdrawn, and `85/index_name_r7_windows` loses its RELEXPR-OK line
(CONFORMANCE.md D-RELINDEXEXPR). PB1431: `ExpressionBinder.LinageFileOf`, the one producer of the LINAGE-COUNTER
operand, refuses it while a report is in scope, and a bare SUM addend is refused in `ResolveSumAddend`. That is
COBOLNET2904 (§8.4.3.14.3 SR1); before it, `SOURCE LINAGE-COUNTER + 1` bound and read the live counter. PB543's sweep found
STRING/UNSTRING POINTER, COUNT IN, TALLYING and OCCURS DEPENDING ON accepting USAGE INDEX through
`Pic is { Category: Numeric, IsFloat: false, Scale: 0 }`; one predicate, `PicInfo.IsUnscaledInteger`, replaces the three
copies. PB1544 was re-probed and re-scoped: its CALL BY CONTENT / INVOKE literal-alias sites are still open.
**PB1470 was held back.** The implementer's gate was RED on that one rule (COBOLNET2903, §8.8.4.2.1 "at least one
reference to an operand that is not a literal"): 21 existing corpus programs (49 sites, mostly `PERFORM UNTIL 1 = 2`)
and `FigurativeSizingTests` compare two literals. The lander removed its seam (`ConditionBinder.WrittenRelational`), its
validation, its descriptor and DIAGNOSTICS row, its six goldens and its inventory record, and split the shared
CONFORMANCE paragraph so that only the PB2018 half lands. The note stays open, carrying the branch, the site list and
what the finisher owes. COBOLNET2903 stays reserved for it. Rows SR-13.18.57.3-3/-6, SR-13.18.58.3-1/-2,
SR-13.18.49.3-3, SR-8.4.2.2.1-4, SR-13.18.60.3-10 and SR-8.4.3.14.3-1 closed.

**A: PB1525, PB855, PB876 (the PICTURE analyzer).** PB1525 adds the ISO §4.2.10 warning mechanism: one
`NonstandardExtensionRegister`, one `EditionContext.Extension` seam, `cobol --flag-extensions` and COBOLNET2894
(CONFORMANCE §3.2). Drift tests join the register, the docs, the reserved words, the grammar's usage keywords and the
binder sites. Without the option a compile prints exactly what it did before. PB855: a non-floating extended editing
sign control symbol (FOR form) takes the 'cs' Table-10 role (§13.18.40.6's closing sentence), so `PIC 99L99` is
COBOLNET1935 instead of rendering `00(12`. SR25 is read as written and SR24 counts symbols over the picture. The
renderer's floating flag is rule 6's adjacency, not an occurrence count (`L999L` rendered `(0012`, now `(012(`). Three
goldens that pinned a mid-string `es` were re-derived. PB876's premise was gone (national-edited is no longer staged),
and both national legs of GR7/GR8 measured correct, with goldens. Beside it, DISPLAY-OF and NATIONAL-OF now read the
Table-2 CLASS where §15.26.3 and §15.66.3 say class. Rows GR-4.2.10-1/-3 and GR-13.18.63.4-7/-8 closed, and
SR-13.18.40.3-24..26 restated.

**B: PB643, RESERVE integer-1 allocates integer-1 input-output areas (§12.4.5.14.3 GR1).** A fixed 4,096-byte host
buffer, not the clause, decided when a record reached the medium. Each connector now keeps one buffer between its
record area and the medium: the sequential reader or writer, whose host handles are unbuffered in every posture
(PB753's rule), or the keyed store handle. That buffer is its areas, sized in one place (`HostFile.InputOutputAreaBuffer`).
With RESERVE 2 AREAS a sibling connector sees 0 of 100 records before CLOSE, against 64 with one area. DOC-A.1-164
determines a 4,096-byte area and one area by default, and COBOLNET2897 refuses more than 524,287 areas (§4.2.15).
An OPEN that cannot allocate them returns '30'. The sibling sweep found that the report-file registration skipped
every connector property, so report files now honor CODE-SET and SHARING too. Rows GR-12.4.5.14.3-1 and DOC-A.1-164
closed. Its '61'-at-85 lead is PB670's (re-confirmed there, not re-filed).

**The train.** The lander gate ran the whole population in one leg: `=== BUILD-LOCAL GATE: GREEN — Conformance 10,595/10,595 · Unit 31,602/31,602 · Characterization 35/35 cases ran (skipped 0) in 1 of 1 leg(s) · lander mode` (run 20261005T133354Z-d0362c), with every population EXACT and the legacy `CobolSharp.Tests.Integration` assembly at 503 passed, 1 skipped. Linux gate: `=== LINUX GATE: GREEN (legs unit characterization conformance guard) ===` (NIST 362 MATCH, 0 regressions). The
semgrep counts held (BigInteger 46, raw-code-literal 273, decimal 2, bound-node-text 3, before and after). The
lander's review of the train's diff (correctness, the drift rules of each changed file, twelve citations re-run
through `cite.py --check`) found no correctness defect; the one premise it checked hardest, that `ReferenceResolver.ReportScope` is set only while report clauses bind, holds (set in `BindReportClauses`, cleared in `BindReportGroupClauses`'s finally), so a LINAGE-COUNTER in a procedure statement is never refused. GAP 232 → 208: 27 records, 24 rows closed. Notes landed: PB2003,
PB1373, PB1374, PB1301, PB1302, PB1476, PB2018, PB543, PB1431, PB1525, PB855, PB876, PB643. Open: PB1470 (held back),
PB1544 (re-scoped), PB1415. Filed: PB2034. Codes claimed: COBOLNET2894, 2897, 2904. COBOLNET2903 is held for PB1470;
2895-2896, 2898-2899, 2905 and 2906-2911 were returned unused.

