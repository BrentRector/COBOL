## Entry 1852 — 2026-10-03 13:44 PDT — Train 1008: the wave 1005 D condition-binder finisher (PB1668, PB1412, PB1464, PB1034, PB1391) and PB1946

Two clusters, landed as two commits plus this entry. Both implementer branches were cut at `b6993f400`, which was still
origin/main when the lander applied them, so neither needed a rebase.

**Cluster A — wave 1005 group D's condition binder, finished (PB1668, PB1412 half, PB1464, PB1034, PB1391).** Train
1005 dropped this group on a composition red. The wave 1008 finisher merged it onto main (it regenerated
`DIAGNOSTICS.md` and `DRIFT_RULES.md`, and kept both sides of the negative manifest), then traced the red to
`85/pb1034_printed_relational_operator_set`. That golden held `IF A IS <> B`, and `<>` is a COBOL-2002 introduction
(PB1459, COBOLNET0900 below 2002), so the line moved to a new `2002/pb1034_is_not_equal_symbol`. The finisher re-probed
every note on its own build and none was discharged. The mechanisms: **PB1668**: TRUE and FALSE used as a condition now
draw COBOLNET2318 naming the word (§8.8.4.2.1 lists no bare truth word; §14.9.13.3 SR7 b places the words as EVALUATE
selection subjects and objects) instead of the COBOLNET2319 internal-error net, and EVALUATE's TRUE/FALSE × TRUE/FALSE
pair is a bind-time constant (GR3 f, GR4 a 4). **PB1412**: `BooleanOperatorTokens` is now the one list of B-operator
tokens, read by the parser predicates, `HasBoolOp` and the introduction gate. The binder's copy had lacked the four
2023 shift tokens, so a shift-only boolean expression was refused. `BooleanOperatorTokenDriftTests` pins the list to
the lexer. The EVALUATE operator-bearing subject/object half stays open on the note. **PB1464**: parentheses around a
simple condition change nothing (§8.8.4.2.1). `ConditionOperandExpression` looks through them at the classification
seam, and `EnclosedBooleanOperand` reads `(BW)` as BW in a relation and in EVALUATE. **PB1391**: COBOLNET2723 enforces
§8.8.4.12.3 SR1, decided on the relation's operand class where the abbreviation carry is seeded. **PB1034**:
`comparisonOperator` is §8.8.4.2.2 Format 1's printed set and no more (NOT >=, NOT <=, NOT … OR EQUAL and EQUAL THAN are
gone), the compile-time relation lost its second NOT prefix, and the START KEY membership screen was deleted because the
grammar now does that screening. `RelationalOperatorFormatDriftTests` pins the alternatives. Goldens: 85, 2002 and 2023
positives, plus fifteen negatives. Nine rows closed. Code COBOLNET2723 was used and 2724–2726 were returned.

**Cluster B — PB1946, a BY CONTENT arithmetic expression into a non-numeric formal.** This applies the PB1936 verdict:
rule 2 d) asks the MOVE question of the argument's value, so an expression is a numeric sender on Table 16's noninteger
row. `OoConformance.ContentValueMismatch` dropped its blanket expression refusal. An expression into a numeric-edited
formal now edits, and into an alphanumeric or national formal it gets the noninteger row's No. The function now also
owns the SET-paragraph refusal for pointer and object-reference formals and the group-formal refusal for an expression,
which has no character image for GR4's copy. The CALL emitter has a new arm and the INVOKE emitter changed one arm, so
both now store through the receiving category's MOVE store. The implementer also found and fixed a sibling:
`IntrinsicResultMismatch` gave an integer-valued NUMERIC function the noninteger row, so `CALL … BY CONTENT FUNCTION MAX
(A B)` into PIC X(4) was refused while the INVOKE compiled. Goldens: `2002/pb1946_content_expression_move_regime` (both
lanes) and four negatives. No codes were used (2727–2729 were returned) and no row was re-verdicted.

**The train.** Lander gate (whole population, one leg): `=== BUILD-LOCAL GATE: GREEN — Conformance 9,980/9,980 · Unit
30,405/30,405 · Characterization 35/35 cases ran (skipped 0) in 1 of 1 leg(s) · lander mode (no plan (lander); run
20261003T203949Z-2e59d5) ===`. The legacy integration assembly ran 503 passed and 1 skipped. Semgrep counts were
unchanged (BigInteger 46, decimal 2, raw diagnostic code 288). The Linux gate under WSL was GREEN on `d8b703dd5` (unit 30,436, characterization 35, conformance 9,980). Its first
run, on the checkpoint head, passed all three legs but went RED on its repository-written check, because the lander
reworded the checkpoint commits during the run. The trees were identical, and the re-run on the final head was clean.
The CI audits all
passed: code and doc citations had 0 findings, evidence supersession 0, witness loss GREEN (3 retired, all
`InGeneralRelationFormat`), drift_rules current, and work.py 1741 items well-formed. The two manifest conflicts (2002
and negative) were whole list elements; keeping both sides gave 635 and 2040 entries, set-equal to the union of the two
branches with no duplicates. The pre-push review of the diff confirmed no findings, and no cluster was dropped. GAP
532 → 523. New leads: none. The reports' two leads are already filed (PB1412's open half, PB1935).

