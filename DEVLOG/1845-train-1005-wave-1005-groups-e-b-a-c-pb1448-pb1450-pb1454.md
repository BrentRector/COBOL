## Entry 1845 — 2026-10-03 01:46 PDT — Train 1005: wave 1005 groups E, B, A, C (PB1448, PB1450, PB1454, PB1279, PB1372, PB1669, PB1459, PB1220, PB1129, PB1050, PB1423, PB1164, PB1419, and 3 more); group D dropped

**Group E — PB1448, PB1450, PB1454, PB1279 (reference resolver; PB1271 split).** Every note reproduced on a fresh
build. PB1448: an object property whose accessor describes a GROUP was refused (`OoGroupValuedProperty`, deleted); the
one temp constructor already deep-clones a group, so §8.4.3.9.4 GR1/GR2's temp-1/temp-2 now carry the accessor's whole
description. PB1450: nothing compared a property's GET RETURNING item with its SET USING parameter; §8.4.3.9.3 SR7 is
now `OoConformance.ValidatePropertyAccessorPairs`, asking the one `DescriptionMismatch` of each pair as the class sees
it (inherited accessors included, instance and factory rosters separately) — COBOLNET2727. PB1454: a sum counter was
modelled as qualifiable only by its report-name, which was false (§8.4.2.2.3 SR4); `ReportSumModel.Qualification`
records the report group entries above the SUM entry, and `DataBinder.QualifierWalk` is now the ONE inner-to-outer
qualifier walk, used by the file-record arm too. PB1279: a REDEFINES class whose canonical lies within a fixed or
OCCURS DEPENDING table is reached through the subscripted parent path (§13.18.44.3 SR5, GR1 per occurrence); the
OCCURS DYNAMIC arm remains (PB1933). PB1271 (per-occurrence sum counters) stays open with its design recorded. Goldens
85/pb1454_sum_counter_group_qualifier, 85/pb1279_redefines_within_occurs, 2002/pb1448_group_valued_property and three
negatives; 8 rows closed (GR-8.4.3.9.4-1/2/3, SR-8.4.3.9.3-7, SR-8.4.2.2.3-2/4, GR-13.18.44.4-1/2).

**Group B — PB1372, PB1915, PB1669, PB1459 (lexer cluster; PB1402 not started).** PB1372's root: the >>COBOL-WORDS
retype ran after lexing, so no lexer decision keyed on a keyword (PIC's mode, the FUNCTION argument region, the
subscript trigger) saw a synonym or a de-reserved word. `CobolWordsRewriter.Plan` is now applied by
`CobolLexer.NextToken` to each token as it is emitted, PIC pushes PICMODE there, and the post-lex `Rewrite` and the
`SetCobolWordsDataNames` seam are deleted. Its second half, a second `Resolve` of an already-resolved function name, is
replaced by `FunctionWord` (written + canonical, made once), which also fixed the REPOSITORY `FUNCTION TOTAL INTRINSIC`
and keyword-omitted forms. PB1915 needed no code: `DebuggingLineRewriter` now reads the retyped stream. PB1669: the
note's fix shape (decide by position in the parser) was not the root; whether a reserved word's '(' opens a subscript
depends on whether the program DECLARES the word, so the lexer's trigger set is "reserved at the strict edition minus
declared" and `Frontend.LexAndParse` lexes again when a parse finds such a declaration. PB1459: `<>` is gated to
COBOL-2002 (`not-equal-operator-2002`, VCR row 7.26, derived like `concat-operator-2002`). 4 rows closed
(GR-7.3.10.4-2/3/4, SR-8.3.2.4.2-2); no code claimed (2715-2718 returned).

**Group A — PB1220, PB1129, PB1050, PB1285, PB1706 (report writer; PB1306 split).** PB1706 was already fixed by PB1707
and is discharged with its missing CONTROL-clause negative. PB1220: COLUMN `LEFT|CENTER|RIGHT` parses (optional, since
§13.18.14.3 SR9 licenses omission although the rendered diagram prints a brace; CENTER is a context-sensitive word),
and `ReportColumnSpec.AbsoluteLeftmost/Rightmost` is GR6 b)-d)/GR9 written once for the emitter and the width walk;
COBOLNET2711 (SR9) and COBOLNET2712 (an aligned item left of column 1, where the standard is silent). PB1129: the RD
CODE clause is live — SR1-SR3 (COBOLNET2713), the code prefixed to every logical record (GR1), the line width
`RECORD CONTAINS − code` (GR2), the identifier re-evaluated at each page advance or body group (GR3). PB1050: several
reports on one file were refused although the runtime already had one engine per report. PB1285 (found in the same
method, I9): the FD-side REPORT-clause name rules, COBOLNET2714. 19 rows closed.

**Group C — PB1423, PB1164, PB1419 (user-defined functions).** PB1423: a user-function activation is a statement
pre-op, so an earlier argument that only reads state was evaluated after a later argument's activation (`EXPR=5010`);
`ArgumentOrder` opens a window per argument list (intrinsic and user-defined alike, §8.4.3.2.4 GR1) and stores an
earlier BY CONTENT / BY VALUE argument in an intermediate item at its own position (GR2). PB1164: CALL RETURNING asked
the §9.3.8.2.3 identity rule where §14.8.3.3 rule 1 says "as if a SET statement were performed";
`ParameterConformance.ReturningConformanceReason` is now the one returning-item rule for CALL and INVOKE, and the
runtime stores a subclass result into a superclass or universal carrier (`ManagedPointer.TryAssign`). PB1419: the
COBOLNET1510 residue list (FLOAT, BOOLEAN, INDEX, pointer, object, strong / REDEFINES / ODO groups) is deleted — the CALL
path already carried every category; the one half-wired channel it predicted (a boolean user function in a boolean
expression) is fixed in `ConditionBinder`. 3 rows closed; no code claimed (2719-2722 returned).

**Group D — DROPPED (PB1668, PB1412, PB1464, PB1391, PB1034).** The whole-population gate on the five-cluster train was
RED on exactly one case: `CorpusRunnerTests_P1.EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(85,
pb1034_printed_relational_operator_set)` — COBOLNET0900 "the relational operator <> requires COBOL-2002" at line 33.
Each cluster's own gate was green; the red is the composition of group D's new COBOL-85 golden, which writes
`IF A IS <> B`, with group B's PB1459 gate. `<>` is not a COBOL-85 operator (B's derivation), so the defect is D's
golden, and D was dropped by resetting to C's checkpoint (the lander does not edit an implementer's golden). D's
branch `worktree-wf_ccb5717f-ed8-4` @8828dea57 lands after one change: move the `<>` line of that golden to a 2002
golden. D's notes stay open on main; its code COBOLNET2723 is unclaimed here.

**The train.** Clusters brought in as patches against e0e7878e1 with the inventory hunks excluded and each verdict
batch re-applied by `record_verdicts.py` (row-for-row equal to each branch); conflicts resolved: `docs/DIAGNOSTICS.md`
(whole rows, regenerated from the catalog), `IntrinsicBinder.BindIntrinsicCore` (C's ArgumentOrder window over B's
`FunctionWord`), `docs/DRIFT_RULES.md` (regenerated). Gate (lander mode, run 20261003T084210Z-5f761e): GREEN —
Conformance 9,906/9,906 · Unit 30,386/30,386 · Characterization 35/35; legacy Integration 503 passed, 1 skipped;
semgrep PASS (raw-diagnostic-code-literal 291 → 290, nothing up); CI audits clean. Review of the merged diff: no
confirmed correctness finding; one plausible composition gap filed as PB1928 (C's boolean-routing predicate re-resolves
the written name, against B's `FunctionWord`). Leads filed: PB1928-PB1935, and three additions to PB1292. GAP 598 →
564. Codes claimed: COBOLNET2711-2714, COBOLNET2727.

