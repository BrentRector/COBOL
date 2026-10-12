## Entry 1300 — 2026-08-18 02:01 PDT — PB68 lands: one class-boolean rule, one classifier — the computed boolean operand at every checkpoint (and the PB62+PB63 battery is green)

**Battery first (PB62+PB63 batch, tree `21af3703`):** Conformance **4628/4628** · Unit **4187/4187** ·
Characterization 33/33 · NIST 353 MATCH / 0 · GnuCOBOL differential 1323 cases, **0 per-case flips**. All green.

**PB68 — RV-15.13.4-1 → CONFORMS; GAP 4048 → 4047.** §15.13.1 "The function type is boolean" and §15.2 item 2 make
BOOLEAN-OF-INTEGER's result class and category boolean, and the catalog and `IntrinsicResultType.OperandCategory`
already said so — but four checkpoints (five, once swept) each re-derived an operand's class with a local switch that
had no arm for a `BoundComputedOperand` wrapping a boolean-result call:

- `StatementValidation.CheckRelationalOperands` — `IF FUNCTION BOOLEAN-OF-INTEGER(544, 6) = B"100000"`, exactly the
  §8.8.4.2.8 comparison, was rejected as a class mix (COBOLNET0844), while the ILLEGAL mirror against an
  alphanumeric literal was accepted and evaluated TRUE. Both local switches are deleted; the checkpoint asks
  `OperandCategory` (which gained the `BoundBoolOperand` arm and is now total over the operand kinds).
- `ConditionBinder.BindBoolOperandValue` — §8.8.2 admits "an identifier referencing a boolean data item", and a
  function-identifier IS an identifier (§8.4.3.1.2) referencing a temporary boolean item (§8.4.3.2.4 GR1); it fell
  to the 1511 default. A sole function reference with a boolean result now binds a new `BoundBoolCall` (rendered as
  the call's '0'/'1' image through the ONE string channel; `IsBooleanValueOperand` routes it; `Gr3Width`,
  `BoolExprAllLengthOne` and the EC intrinsic walk know the node — a runtime-length result fails open).
- `IntrinsicBinder.BindLengthFold` / `IsStringOperand` — the string-class result arms said "alphanumeric or
  national"; Boolean is the third string-class type (its image is the '0'/'1' string), so LENGTH answers r1's
  boolean positions (8) and a boolean function is a string operand.
- The FIFTH site, found by the sweep the note demanded — `ConditionRenderer.StringCategoryOf`/`BoolRead`: two
  boolean function results compared each other rode the alphanumeric collate-and-space-pad branch; they now take the
  boolean right-zero-extension, and a boolean function beside a boolean EXPRESSION reads through `BoolRead`.
- `ExpressionBinder.BindPrimary` — `COMPUTE N = FUNCTION BOOLEAN-OF-INTEGER(5, 8) + 1` compiled clean and died at run
  time with an unhandled NotImplemented (the renderer's loud value for a string-class function in a numeric context
  — the wrong stage). §8.8.1.1's class screen now fires at BIND for a string-class function-identifier operand,
  COBOLNET0844, with the SAME dialect gate the DA6 data-item rule has: `--permissive` decodes the digit characters
  (`CobolNum.FromAlphanumeric` over the function's string image), so the leniency is one rule for both shapes.

Measured (golden `pb68_boolean_function_operand_contexts`): five relation shapes TRUE (equality, against a bit item,
against another function reference, `(5, 4) = B"01010000"` zero-extended, NOT =), `B-AND`/`B-XOR` over the function
(00000100 / 00000110), LENGTH 8, a boolean-expression relation and a simple boolean condition, MOVE to a bit item,
a display boolean and an alphanumeric item (Table 16's Yes cells; the No cells reject as before), the function as an
EVALUATE subject and WHEN object. Negatives `pb68-boolean-function-vs-alphanumeric-literal` (the accepted mirror)
and `pb68-boolean-function-arithmetic` (the crash).

**Gates (wave-local).** Solution build · Characterization 33/33 (after routing the permissive decode through
`RuntimeApi.NumFromAlphanumeric` — the guard caught the bare `CobolNum.` again) · Conformance
`Corpus|Negative|Boolean|Bool|Condition|Relation|Intrinsic|Evaluate|SpecTraceability|Function` **1244/1244** · Unit
`SpecTraceabilityInventory|Bool|Ec|Intrinsic|Bound|Condition|Manifest` **3180/3180** · CLI probes. Battery owed for
the PB68 batch. Docs: COBOLNET_INTRINSICS_DESIGN.md (the one-classifier section), kb/Work PB68 → landed, plan §0.
Next: PB69 (the saturated `**` sentinel consumed as an argument — the design decision is written up in the note and
below in 1301).

