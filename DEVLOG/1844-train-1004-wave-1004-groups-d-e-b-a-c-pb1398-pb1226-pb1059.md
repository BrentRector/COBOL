## Entry 1844 — 2026-10-02 23:29 PDT — Train 1004: wave 1004 groups D, E, B, A, C (PB1398, PB1226, PB1059, PB1298, PB1360, PB807, PB1095, PB990, PB1175, PB1139, and 18 more)

**Group D — PB1398, PB1472, PB1662, PB1913 (function LENGTH, ALL subscripts, debugging lines in regions).** PB1398
reproduced with two siblings (an integer function, and the nested `LENGTH(FUNCTION LENGTH(X))`): both LENGTH folds had
no arm for a function-identifier of non-string type. The one arm is `IntrinsicBinder.FunctionResultLength`, keyed on the
§15.2 type (§8.4.3.2.1 "the unique data item that results from the evaluation of a function", cite-checked): string
types take the runtime length (BYTE-LENGTH 1, 2 or ceil(n/8) through `CobolIntrinsics.ByteLengthNational/Boolean`),
integer and numeric functions take the §15.4 temporary shared with the statement hoist
(`SendingValueTemp.NumericReturnedValuePic`, S9(30)), an index 8. `BoundNumericLiteral.FunctionValue` marks a folded
function value at the one funnel `BindIntrinsicCore`. PB1472: `RenderIndexSegment` takes the collector and
`TryBindAllArgument` asks `ScreenIndexNameAssociation` (§8.4.2.3.3 SR4). PB1662's premise did not hold: §8.4.3.2.1 and
§15.2 make a numeric function-identifier a numeric data item, so §8.8.4.2.13 row 2 ADMITS it against an index-name; the
folded form was admitted and the unfolded one refused (a two-arm dispatch), and both are now admitted
(`RelationComparability.IndexNameAgainstNumeric`, D-RELCLASS corrected). PB1913: the note's design could not work
because a debugging line's text decides the lexer's own mode inside a PICTURE or subscript region; the region modes now
skip the line and leave a virtual `DEBUG_LINE_IN_REGION` marker, `DebuggingLineRewriter.Rewrite` returns the first such
line whose unit declares WITH DEBUGGING MODE, and `Frontend.LexAndParse` blanks its carrier (`WithCarrierBlanked`, same
offsets) and lexes again until none is left. Five goldens and two negatives; rows GR-8.5.2.3-4, GR-8.5.2.5-4,
GR-8.5.2.10-4, GR-8.5.2.8-2 and SR-8.4.2.3.3-4 closed (GAP −5). No code claimed.

**Group E — PB1226, PB1287, PB1059, PB1248, PB1298 (report section surface; three mechanisms).** The REPORT SECTION
entry grammar now takes constant entries after an RD (`constantEntry`, a second host of the one `constantEntryBody`,
bound by `BindConstantEntry(level, name, body)`), the report group's name slot is the data description entry's
`dataName` (so FILLER, and `reportGroupName` is deleted), and an RD with no report group is COBOLNET2708 (§13.8.4). A
sibling surfaced and was fixed: `PIC X(K)` in a report entry never expanded an integer constant (§13.10.3 SR2). The PAGE
clause follows the rendered p468: integer-2 COLUMNS, LAST CONTROL HEADING and FIRST/LAST DE; `ReportModel.PageWidth`
(999 by default, §13.18.39.4 GR5, cite-checked) reaches the runtime, with COBOLNET2709 (SR3) and COBOLNET2710 (§13.18.14.3
SR6, a COLUMN beyond the page width). The TYPE clause follows the rendered p524: ON/FOR, and OR PAGE on a control heading
with an operand; the engine reprints each OR PAGE heading after a page advance (`ReportGroup.OrPage`,
`CobolReport.PresentOrPageHeadings`, §13.18.57.4 GR6 c)), and §13.18.35.3 SR9 screens its LINE clauses. Fifteen corpus
rows; GAP −23. PB1226 stays open on SR-13.10.3-11 and PB1298 on GR-13.18.57.4-7/-L2.4 (with PB1270). Two readings went
to the register: whether `TYPE CH OR PAGE` without an operand is legal (PB1926) and how GR6 c)'s proviso and GR7 d) 4.
agree for a lower-level footing (PB1927).

**Group B — PB1360, PB1384, PB807, PB1538, PB1533 (directive lines and the DISPLAY directive; landed SPLIT).** A
compiler directive line is never a continuation join target now: `LogicalLineBuilder` drops and diagnoses a fixed or
floating continuation of it (COBOLNET2696, §7.3.3 SR1, cite-checked). A directive inside an open COPY or REPLACE
statement is named (COBOLNET2697, §7.3.3 SR8 b)) by `CopyProcessor.OpenStatementAt`, the statements' own pseudo-text
aware walk. The DISPLAY directive parses its operands with a grammar fragment, evaluates them with the shared
evaluator, resolves UPON against the one implementor-name table (moved to `Cobol.Net.Editions`, every caller changed,
the old file deleted; COBOLNET2698) and writes its line at compile time through `DiagnosticBag.Transfer` →
`CompilerDriver.Result.CompileOutput` → the CLI. PB1533 pins DEFINE PARAMETER typing. Two premises were stale on
re-probe: PB1360's EVALUATE half was already COBOLNET2649, and DOC-A.1-53..55 had been written ahead of an
implementation that did not exist. GAP −17. PB1368 keeps its CONSTANT FROM and >>COBOL-WORDS literal pieces, which need
the compilation-variable timeline carried to the binder (design in the note).

**Group A — PB1095, PB1537, PB1082, PB990, PB791, PB1403 (SPECIAL-NAMES literals and the user-word census).** Each was a
two-arm dispatch with one strict slot and one lenient sibling. CLASS … IN now names every member of an ALSO ordinal
(determination: §12.3.7.4 GR12 a)'s "a character" read as the plural); ORDER TABLE and LOCALE share one
`SpecialNamesFigurative`; PROGRAM COLLATING SEQUENCE and the SORT/MERGE phrase ask the file clause's
`CollatingAlphabetFault`. `DeclareUserWord`, the one declaration funnel, refuses one word used as two types
(COBOLNET2692, §8.3.2.2, cite-checked, with the exceptions held as data in `UserWordKinds.MayBeOneWord`) and a letterless
word that is not a section-name, paragraph-name or level-number (COBOLNET2693). CURRENCY SIGN refuses the literal forms
SR18/SR19/SR26 bar and exempts a hexadecimal literal-7 from SR23 b). Four non-conforming goldens were repaired, 24
goldens added, the semgrep raw-diagnostic-code baseline fell 294 → 291. GAP −6.

**Group C — PB1175, PB1055, PB1173, PB1052, PB1139, PB994 (the SORT/MERGE binder; four mechanisms).** The Format-2
table SORT decides its storage form once (`TableSortStorage.TypedArray | SharedArea`), so a table in a REDEFINES class,
a shared record area or a nested table sorts by element number through the one key comparer instead of aborting
(COBOLNET1756); the subject's subscripts are read once (COBOLNET2097; determination: `SORT TE(1)` over a one-level table
is refused, PB1923). One `SortKeyAdmission` predicate serves SORT Format 1, Format 2 and MERGE (§14.9.40.3 SR6 d),
cite-checked; the ODO-group reading is PB1924). Placement is one rule over three regions (COBOLNET2700 for a declarative,
§14.9.40.3 SR3 and §14.9.24.3 SR1, cite-checked), with the SAME clauses modelled on `FileModel.SameClauses`; relative and
indexed USING/GIVING files work (RANDOM refused by name, indexed GIVING key SR9, relative keys 1..n). The L5 finding was
refuted by the printed p745: COLLATING is an optional word. Ten goldens and 32 negatives; GAP −20.

**The train.** Five clusters, one patch each from its implementer branch onto 5bdbfd241, in the order D, E, B, A, C.
Conflicts were whole elements only: the negative and edition manifests' lists, the generated `docs/DIAGNOSTICS.md`
rows and two adjacent `DiagnosticCatalog` blocks, all taken from both sides; one doc comment in
`SortBinder.SortBindCollating` was merged by hand to carry A's shared alphabet test and C's L5 correction. A script
compared every merged manifest and the inventory with base plus each branch: every list exact, 92 inventory rows each
changed by exactly one cluster, none lost. Codes claimed COBOLNET2692-2693, 2696-2698, 2700, 2708-2710 (each inside
its cluster's range); returned 2694, 2695, 2699, 2701-2707, 2711. Gate (`build-local -Mode lander`, run
20261003T061819Z-92c357): GREEN, Conformance 9,857/9,857 · Unit 30,367/30,367 · Characterization 35/35, external
corpus fetched; legacy Integration 503 passed, 1 skipped; Linux gate GREEN on 3be673527 (the same tree); semgrep PASS,
no count rose; the six CI audits and `work.py check` clean. The lander's review of the train diff found no correctness
defect; three citations per report were re-run, all OK. **GAP 669 → 598.** Nothing dropped. New notes PB1918-PB1927
from the reports' leads: Frontend's stale InternalsVisibleTo name, the untested CLI output loop, the CURRENCY cascade,
a name-scanning drift test, the pointer-in-shared-element table SORT, two SORT determinations to confirm, the
unpositioned placement diagnostics, and the two report-writer readings.

