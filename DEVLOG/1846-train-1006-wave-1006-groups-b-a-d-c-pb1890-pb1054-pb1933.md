## Entry 1846 — 2026-10-03 09:14 PDT — Train 1006: wave 1006 groups B, A, D, C (PB1890, PB1054, PB1933, PB574, PB1113, PB1498, PB1116, PB1144, PB1118, PB1368, PB1228, PB1227, PB1232)

**Group B — PB1890, PB1054, PB1933, PB574 (reference resolver; PB1271 split).** PB1890: a subscript or ref-mod bound
written as an expression (or a float item) was stored into the D18 temporary (21 + 9 digits, truncated) and tested for
integrality only at the position read, so `T(IX + 0.0000000001)` and a COMP-2 `2.0000000001` subscript selected
occurrence 2 with checking on. The new pre-op `BoundPositionValue` renders the expression receiver-less and hands the
value on its own carrier to `CobolTable.OccValue*` / `CobolString.RefModValue*`, which raise before the saturated
position is stored in a 19-digit integer temp (§8.4.2.3.4 GR1b, §8.4.3.3.4 rule 5c); `NumericRenderer.IntakeCarrier` is
now the one carrier map, shared with the §15.3 intake. PB1054: a RENAMES THRU span deferred every non-character numeric
leaf; `SpanLeafPlace` now takes every numeric usage through `NumericImagePlace` (§13.18.45.4 GR2), and the green negative
that pinned the deferral is retired. PB1933: a REDEFINES backing inside an OCCURS DYNAMIC element is reachable, and the
window stores render Receiving so a store grows the table. PB574 did not reproduce (PB522 fixed it) and is discharged
with witnesses. PB1271 stays open with its re-probe recorded. The report put B at GAP −2; the rows it re-verdicted are
four (GR-5.5-3, DOC-A.1-124, GR-13.18.45.4-2, SR-13.18.63.3-1), so B closed 4.

**Group A — PB1113, PB1498, PB1116 SR13 (OO conformance; PB480, PB1112 and PB1116's carrier split).** PB1113: the BY
CONTENT literal, expression and boolean lanes carried three hand-written admission lists, each narrower than §14.8.2.3.3
rule 2 (`12.5` into ZZ9.99, `12` into N(4) and `-5` into X(4) refused; a PIC 9(4) argument into an index-data-item formal
and 9V9 into N ANY LENGTH admitted). One verdict, `OoConformance.ContentValueMismatch`, asks rule 2 in order (2b SET, 2a
COMPUTE, 2c length only, 2d MOVE) of a described sender, and both carriers store a literal into a non-numeric formal
through the receiving category's MOVE store; a sibling (a CALL literal into a group formal printed the overpunched `1K`)
was fixed. PB1498: §9.3.8.2.3's closing sentence, the mutual-reference ban between RETURNING descriptions, is a rule in
`MethodConformanceMismatches` (transitive closure per owner decision R63, CONFORMANCE.md D-OOCIRC), so all three askers
apply it; the FACTORY-axis refusal wording, which read the reverse of the fact, was fixed in the same file. PB1116: SR13
(a group INVOKE operand with a subordinate ACTIVE-CLASS reference) is its own screen, COBOLNET2728. 2 rows closed. The
arithmetic-expression-into-non-numeric-formal reading is filed for adjudication as PB1936.

**Group D — PB1144, PB1118.** PB1144: a variable-length group MOVE moved corresponding tables as element images, so
differently described elements were never converted (`E=1234 5678`; §14.6.9.2 gives `E=3400 7800`). The element moves
ride `BoundMove.ElementMoves`, bound in `MoveBinder.BindMoveOf`, so every implicit MOVE arm (READ/RETURN INTO,
WRITE/REWRITE/RELEASE FROM, GOBACK RETURNING) inherits them; the GR9 antecedent is one predicate
(`MoveClassifier.VariableLengthGroupSender`) and `OccurrenceCounts` is shared with INITIALIZE. PB1118: a dynamic-length
item read through a LINKAGE formal or a shared cell now raises EC-DATA-INCOMPATIBLE when its content exceeds the reading
description's maximum (§14.6.13.2 rule 5, `CobolDynString.Agree`); rule 6 is determined unreachable and pinned by
`DynamicCapacityAgreementDriftTests`. 3 rows closed. Its three leads are PB1937-PB1939.

**Group C — PB1368, PB1228, PB1227, PB1232 (constants; PB1231 split).** The >>DEFINE table became a timeline:
`ConditionalCompilationProcessor` records each DEFINE change at its resultant line, `Frontend.Preprocess` replays
PUSH/POP over them into `DirectiveResults.CompilationVariables`, and one fold (`CompilationVariableTimeline.DefinitionAt`)
serves `CONSTANT … FROM` (§7.3.11.4 GR1/GR2, §13.10.3 SR8 at the entry's line) and the >>COBOL-WORDS literal slots.
`CONSTANT AS BYTE-LENGTH OF` binds beside LENGTH OF through one `BindConstantLength(bytes)` (its 0899 staging rested on
the false premise that FUNCTION BYTE-LENGTH was deferred), and SR3 admits an integer constant-name as a subscript. Both
0899 descriptors are deleted. 11 rows closed. PB1231's forward references stay open; its note already covers the
SPECIAL-NAMES literal positions the report flagged.

**The train.** Each cluster came in as a patch against 805fb16c2 and applied cleanly, the inventory included; a row-level
comparison showed the merged inventory equal to the union of the four branches (26 rows changed, none by two clusters),
and every landed note names its rows in `closes_rows` or carries a reason. Gate (lander mode, run
20261003T160511Z-ac6a5d): GREEN — Conformance 9,932/9,932 · Unit 30,396/30,396 · Characterization 35/35; legacy
Integration 503 passed, 1 skipped; Linux gate GREEN on ad0b66b2a (the same tree); semgrep PASS with every count unchanged;
audit_code_citations, audit_doc_citations, audit_evidence_supersession, audit_witness_loss (0 unexcused, 5 retired, 19
re-sited), drift_rules --check and work.py check all clean; 13 citations re-run with `cite.py --check`, all OK. The
full-code review of the merged diff found no confirmed correctness defect. GAP 564 → 544. Code claimed: COBOLNET2728
(2729-2743 returned). Leads filed: PB1936-PB1939. No cluster dropped.

