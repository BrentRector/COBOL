## Entry 2137 — 2026-10-16 20:58 PDT — PB3191: a constant-name written as a bare condition or boolean operand is its literal, not an undefined name

kb/Work PB3191 reported that `01 K CONSTANT AS 0.` then `IF K` was refused COBOLNET1639 "'K' is not defined",
a false message, and left open whether §8.8.4.3.3 SR1 admits a boolean constant-name as a condition.

**Re-probe.** It reproduced on this build (`IF SWON` and `IF BK` both 1639), and the note under-stated the
defect. The constant-name is not a data item, so every procedure-division reader that asked the data-item
resolver only (`ctx.Refs.Probe` / `ResolveSending`) saw nothing. In `ConditionBinder` that was three sites:
`RefuseNonCondition` (the not-a-condition report), `IsBooleanValueOperand` (the routing predicate) and
`BindBoolOperandValue` (the §8.8.2 leaf). So the break was wider than the bare condition: `BK B-AND B1`,
`B-NOT BK`, `COMPUTE B3 = BK B-AND B1` and `B2 = BK2 B-OR B"01"` were all refused 1639 although the same name in
a plain relation (`IF B1 = BK`) was fine, because `ExpressionBinder.FieldOperand` already asks
`LiteralAliasOf` (kb/Work PB1544) first. This is the usual two-arm dispatch: the operand arm had the alias, the
condition and boolean-leaf arms did not.

**The rule (spec first).** §13.10.4 GR1: "the effect of specifying constant-name-1 in other than this entry is as
if literal-1 ... were written where constant-name-1 is written"; §13.10.3 SR2: it "may be used anywhere that a
format specifies a literal of the class and category of constant-name-1". §8.8.2 lists "a boolean literal" as a
boolean expression, so a boolean constant-name IS a boolean expression operand and, of length 1, a §8.8.4.3
simple boolean condition (SR1 length 1; GR1 true iff 1; GR2 NOT). That settles the note's open question: a
`B"1"` constant is legal source and was being rejected (`rejects_legal_source`), while `AS 0` is `IF 0`, a
numeric literal, which §8.8.4.2.1's list of simple conditions does not contain and which stays refused.

**Fix.** The three sites now ask the one literal-alias resolution (`ExpressionBinder.LiteralAliasOf`, the
LiteralAliasResolutionDriftTests funnel; no second reader of the constant table):
`IsBooleanValueOperand` routes a boolean constant to the boolean channel, `BindBoolOperandValue` binds it as the
`BoundBoolLiteral` of its bit string (and refuses any other alias, numeric or string constant or symbolic
character, as COBOLNET1511 naming what it is, instead of 1639), and `RefuseNonCondition` names the alias in
COBOLNET2318. No new diagnostic code (the three allocated, COBOLNET4181-4183, are returned unused).

**Goldens.** Positive `2002/pb3191_boolean_constant_name_as_condition` (the constant entry and the boolean
operators both arrive in 2002; the 85 refusal is the existing COBOLNET0900 gates): the bare condition, NOT,
parenthesized, AND/OR with a boolean item, B-AND and COMPUTE, a boolean relation with B-OR, and both EVALUATE
forms, expected values derived from §8.8.2/§8.8.4.3. Negatives `pb3191-numeric-constant-name-as-condition`
(2318), `pb3191-numeric-constant-name-boolean-operand` (1511) and `pb3191-boolean-constant-name-length-two-condition`
(§8.8.4.3.3 SR1 on `B"10"`).

**Register.** PB3191 closes no inventory row (`closes_rows: []` with its reason); severity stays MINOR.

**Gate red, and a golden that encoded the false message.** The first gate read RED on one case:
`negative/pb3153-method-constant-hides-object-condition-name` expected COBOLNET1639 for `IF X` over a method-local
`01 X CONSTANT AS 7`, the very message this note reports as false (X is declared). Its expectation is now
COBOLNET2318 and its header says why; a sweep of every negative that expects 1639 beside a CONSTANT entry found no other.

