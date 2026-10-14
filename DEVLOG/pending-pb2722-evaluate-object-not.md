## PB2722: an EVALUATE selection object's NOT belongs to the five alternatives that print it

Wave 1077 group F (PB2722, one root: the selection-object rule in `CobolControlFlow.g4` and its binder).

**The note, re-probed.** On this branch's base build (a6744fa4d) `EVALUATE X WHEN NOT ANY … WHEN OTHER` compiled clean
and printed OTHER, and `EVALUATE TRUE WHEN NOT TRUE … WHEN OTHER` printed OTHER3: §14.9.13.2 prints ANY, TRUE and
FALSE with no NOT ("The bracketed `NOT` applies only to the first five alternatives, exactly as stacked"), so both are
source no format admits and §4.2.2 owes them an indication.

**What the re-probe found beyond the note: a silent wrong answer.** The grammar was `evaluateWhenGroup : NOT?
evaluateWhenItem` — one optional NOT hoisted over all seven alternatives. ANTLR enters a greedy `?` whenever both
readings are viable, so the hoisted NOT also took the leading NOT of every condition-2 (a negated condition,
§8.8.4.10.1) and every partial-expression-1 (`NOT >`, `NOT NUMERIC`, §14.9.13.3 SR5), and the binder wrapped the WHOLE
object in `BoundNot`. Measured: `EVALUATE TRUE WHEN NOT A > B AND A > B` (A=1, B=2) selected the arm, though
§8.8.4.11.3 ("The order of precedence of logical operators is 'NOT', 'AND' …") makes it (NOT 1>2) AND 1>2, never true;
`EVALUATE X WHEN NOT > 5 AND < 2` (X=3) selected the arm, though SR8 reads it as X NOT > 5 AND X < 2. The note's
proposed target — refuse a NOT on a Condition or PartialExpression object in the binder — would have rejected that
legal source, so the fix moved to the parse.

**The fix.** `evaluateWhenGroup` is deleted; `evaluateWhenPhrase` repeats `evaluateWhenItem` through ALSO, and
`evaluateWhenItem` spells the printed brace: `NOT?` on `valueRange`, `addressIdentifier`, `valueOperand` and
`{boolExprAhead()}? NOT? booleanExpression` (the predicate stays at the left edge), and on two words only as the
parse-wide half of a named screen — `NOT? booleanLiteral` and `NOT? ANY`. `condition` and `partialExpression` own their
leading NOT. `EvaluateBinder.BindSelectionObject` wraps the term in `BoundNot` only for the bracketed NOT (GR4 a) 5.
and 6.), after `ScreenObjectNegation` refuses NOT ANY / NOT TRUE / NOT FALSE as **COBOLNET3710**
(`evaluate-object-negation-not-permitted`, at the NOT, every edition). TRUE/FALSE is now its own alternative ahead of
`condition`, so the binder reads the object word off `item.booleanLiteral()` as it reads ANY, and the
`SoleBooleanLiteral` parse-tree walk is gone. `FlagConformancePass.FileStatus` follows the rule shape.

**Design.** `DESIGN-frontend-grammar.md` gains invariant 9 — a prefix stands on the alternatives that print it, never
hoisted over an alternative that owns the same word — with this as its worked example;
`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D5b states where the NOT is placed and screened.

**Goldens and drift.** `85/pb2722_evaluate_object_not_ownership` (NOT identifier / literal / arithmetic expression /
range, negated conditions whose precedence the hoist broke, a negated condition-name, an ALSO pair) and
`2002/pb2722_evaluate_partial_not_ownership` (`NOT > 5 AND < 2`, `NOT < 5 OR = 3`, `NOT NUMERIC`, `NOT POSITIVE`,
NOT boolean-expression-2); negatives `pb2722-evaluate-not-{any,true,false}` at every edition.
`EvaluateSelectionObjectArityDriftTests` gains `BracketedNot_StandsOnThePrintedAlternativesOnly` and
`ALeadingNot_BelongsToTheShapeThatPrintsIt`; `EvaluateBooleanExpressionOperandDriftTests` reads the alternatives less
their `NOT?`.

**Inventory.** No row was non-OK: FMT-14.9.13.2 and GR-14.9.13.4-4 were CONFORMS over the defect. Batch
`w1077f-pb2722-evaluate-object-not` re-witnesses both (the new goldens and drift facts) and retires the witnesses the
rename removed (`#evaluateWhenGroup`, `#BindWhenGroup`, `WhenGroup_HoldsExactlyOneSelectionObject`); GAP 101 → 101.

**Sibling sweep.** The selection SUBJECT prints no bracketed NOT, and `EVALUATE NOT TRUE` is already refused as
COBOLNET2318; the `>>WHEN` directive refuses `NOT 2` (COBOLNET1619); no other grammar `NOT?` precedes a sub-rule that
can itself begin with NOT (`unaryLogicalExpression`, `cceNot`, the class / sign / omitted / DEFINED tails all precede
keywords or a NOT-free primary).

**Self-review and gate.** The first gate (20261014T194008Z-f7709b) was RED on three Unit drift facts: a `;` in an
alternative's trailing comment ended the regex-located rule body, so the two EVALUATE drift classes now strip line
comments before locating a rule (PartialExpressionSpineDriftTests' own lesson) — and that class's
`WhenItem_PutsPartialExpression_AfterTheOtherShapes` had gone VACUOUS on the `NOT?`-prefixed shapes, so it now strips
the prefix and requires each shape to be present. Re-gate 20261014T194745Z-233bde:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 5,857/11,669 · Unit 32,826/33,625 ·
Characterization 6/36 in leg 1; leg 2 left to CI), all five pb2722 goldens Passed.
