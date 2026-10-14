## Entry 2016 — 2026-10-13 22:29 PDT — PB3133: the OBJECT-COMPUTER paragraph's cardinality and order have ONE reader, the clause table; PB3132 retired (its repro is already refused)

Wave 1072, group D (the data binder: PB3132, PB3133).

**PB3132, retired.** The note said a METHOD-ID spelling its CLASS-ID that also uses the class-name in its body is not
refused by the one-type-per-word census (ISO §8.3.2.2). Re-probed on this build at `--std 2002` it is refused,
COBOLNET2692, at the METHOD-ID, with or without the body's `INVOKE`: the declaration funnel announces both words, and a
class-name use can only resolve to a declaration (a REPOSITORY entry or the containing class). The note's one repro was
never written down, so nothing reproduced. `negative/pb3132-method-id-spells-its-class-id-and-invokes-it` now pins the
shape (it had no golden). The real residue is a different mechanism, reported as a lead: a name a containing element's
SPECIAL-NAMES passes down is inherited, never declared, so a contained element may declare the same word as another type
(PB990, §8.4.6.1) and then use both (`outer CLASS FOO`, `inner 01 FOO`, `IF X IS FOO` binds the class while `DISPLAY FOO`
binds the data-name). §8.4.6.1 makes the element's own item win, which would refuse `IS FOO`: a lookup-shadowing rule
across the SPECIAL-NAMES registries, not taken here.

**PB3133, landed.** At `--std 85`, `OBJECT-COMPUTER. XYZ MEMORY SIZE 10 WORDS MEMORY SIZE 20 WORDS.` and
`SEGMENT-LIMIT IS 10` before `MEMORY SIZE 20 WORDS` compiled clean. The note put the design question: the clause table
or the binder owns the §5.2.6.4 counts. Answer: the table. The binder counted PROGRAM COLLATING SEQUENCE and CHARACTER
CLASSIFICATION itself (COBOLNET1652) while `ClauseCardinalityDriftTests` exempted the list, so the 1985 figure had no
reader. The list is now a `ClauseCardinalities` row on PB2532's `Earlier`/`At(year)` chain (merged in from that branch
while it was queued): from 2002 the §12.3.6.2 figure, rendered from folio 285, encloses the two clauses in choice
indicators (each once, any order; a repeat is COBOLNET2104, the one choice-indicator reader); below 2002 the COBOL-85 figure ranks MEMORY SIZE, PROGRAM COLLATING
SEQUENCE, SEGMENT-LIMIT once each in that sequence (COBOLNET2423 repeat, COBOLNET2987 out of order). The 85 figure is
derived, not quoted (no 1985 text in the repository): IBM Enterprise COBOL prints it, GnuCOBOL takes any order and
number so it is no arm of the precedence, and the seven NIST CCVS-85 programs that write the clauses write one each.
The binder's counts and COBOLNET1652 are deleted; the code stays retired.

**Found and fixed in the files read (I9).** `UnrepeatedElements.AtMostOnce` told every list it "encloses it in its own
bracket with no ellipsis", false for a choice-indicator list. That list is now reported by
`ChoiceIndicators.AtMostOnce` (COBOLNET2104; see the review round below), and COBOLNET2423 and its message keep
§5.2.6.2/§5.2.7 only.

**Goldens.** `85/pb3133_object_computer_clauses_in_sequence_85`; `negative/pb3133-object-computer-clause-repeated-85`,
`negative/pb3133-object-computer-clause-out-of-order-85`, `negative/pb3132-method-id-spells-its-class-id-and-invokes-it`;
`pb78-object-computer-duplicate-clause` and `pb64t5-classification-twice` now expect COBOLNET2104. Unit:
`ClauseCardinalityDriftTests.TheObjectComputerRow_ScreensTheCobol85FigureBelow2002`. Rows: none claimed by either note.

**Self-review.** Architecture: one reader per rule (the binder lost its copy). Code: the row cites the figure it
implements; the 2023 figure's `Sequence` is null because §5.2.6.4 frees the order. Performance: one dictionary lookup per
clause, as before. Duplication: the row first counted through COBOLNET2423, a second reader of §5.2.6.4; review round 1
(R1-1, below) moved it onto the one reader. The gate's verdict is recorded in the branch's report.

**Branch review, round 1 (wave 1073 B).** The reviewer found two defects, both fixed. R1-1: four current-state doc
sites still named the deleted COBOLNET1652 (`DESIGN-frontend-grammar.md` 3.3e, `CONFORMANCE.md` CHARACTER
CLASSIFICATION, `DESIGN-locale-facility.md` twice); they now name the code that fires (COBOLNET2104, after the finisher's R1-1 fix below) and the `ClauseCardinalities` row, and the
two inventory rows whose notes carried the old code (FMT-12.3.6.2, RV-15.57.4-3) were restated through one
`record_verdicts` batch (no verdict moved, GAP 103 to 103) that also adds the PB3133 goldens and the table's file to
FMT-12.3.6.2's witnesses. R1-2: the PB3132 negative's header named the scratch word XB; it names PB3132X. Re-probed on the
merged build: both PB3133 repros are refused at `--std 85` (COBOLNET2423, COBOLNET2987) and the PB3132 negative is
COBOLNET2692. A sweep of `docs/` for diagnostic codes absent from the catalog finds many older stale names (design docs
that quote placeholders or retired codes); a guard over them is a redesign beyond this slice and is reported as a lead.

**Review round 1, the second finding (R1-1 of the choice-indicator review), fixed at its root.** The OBJECT-COMPUTER row
reported a repeat as COBOLNET2423 (a bracket without an ellipsis) after the branch widened that descriptor to claim
§5.2.6.4, so one sentence of the standard ("any single alternative may be specified only once") had two readers and two
codes: `PROGRAM-ID. X IS INITIAL INITIAL PROGRAM.` was COBOLNET2104 and a repeated CHARACTER CLASSIFICATION clause
COBOLNET2423. `ClauseList` gained `ChoiceIndicators` (true on the 2002+ OBJECT-COMPUTER figure only; the COBOL-85 figure
is three separate brackets and stays COBOLNET2423). `ClosedFormatPass` reports a row so marked through a new count
overload, `ChoiceIndicators.AtMostOnce(edition, count, ...)`, which the array overload now calls too, so the message and
the code are written once. COBOLNET2423's descriptor and `UnrepeatedElements`' message lost the §5.2.6.4 widening. The
four doc sites, PB3133's note and two inventory rows (FMT-12.3.6.2, RV-15.57.4-3; batch w1073bf) now name COBOLNET2104;
`pb78-object-computer-duplicate-clause` and `pb64t5-classification-twice` expect it; the 85 negatives still expect
COBOLNET2423; `ClauseCardinalityDriftTests.TheObjectComputerRow_ScreensTheCobol85FigureBelow2002` pins the flag per
figure and `EveryChoiceIndicatorFigure_IsOrderFree` ties the flag to an absent `Sequence`.

**Review round 2 (R2-1, a comment).** The binder comment in `DataBinder.Switches.cs` still said the OBJECT-COMPUTER repeat is
COBOLNET2423; round 1 had moved the 2002+ repeat to COBOLNET2104. It now names both codes by figure. Swept every
COBOLNET2423 mention under src/ and docs/: the rest describe the 85 figure, the OPTIONS paragraph or RD brackets, and are true.

