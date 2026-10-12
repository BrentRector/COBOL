## Entry 757 — 2026-07-11 01:10 PDT — EXEC STEP A / P7 §6d+6e: convert NumericRenderer + ConditionRenderer onto the exhaustive expr/operand/condition visitors — three more loud `_ =>` defaults GONE

Two more consumers onto the 6a generator (the byte-exact-covered renderers, so safe to do confidently — a
transcription slip surfaces as a characterization byte diff).

**6d — NumericRenderer** now implements `IBoundExprVisitor<NumX>, IBoundOperandVisitor<NumX>`. `Render(BoundExpr)`
and `AsNum(BoundOperand)` became thin `=> …Accept(this)` dispatchers + 11 expr + 8 operand `Visit` methods (each
switch arm verbatim, pattern var → `n`; the two pattern-guarded arms — `BoundFigurative{Kind:'Z'}` and
`BoundStringLiteral{Category:Boolean}` — merged into one `Visit` each with an inner guard, preserving arm priority).
`Render`'s `_ =>` was already dead (all 11 expr leaves covered); `AsNum`'s `_ =>` genuinely caught `BoundAllLiteral`
and `BoundBoolOperand` (ALL-literal / class-boolean operand in a numeric context — not numeric operands, §8.8.1), now
explicit loud `Visit`s emitting the byte-identical loud value (`nameof(X)` == the old `op.GetType().Name`).

**6e — ConditionRenderer** now implements `IBoundConditionVisitor<string>`. `Render` keeps its `ctx.TargetReal = false`
preamble (the H1 float-staleness clear) then `=> c.Accept(this)`, + 10 `Visit` methods; the two `BoundLogical` switch
arms (empty-tautology vs general) merged into one `Visit` with an `Operands.Count == 0` guard. `_ =>` was dead. The
helper renderers (`RenderRelational` and friends) are untouched.

Byte-exact behavior-neutral: 32 characterization + 3158 conformance + 265 unit UNCHANGED across both. Docs swept:
PHASE-07 STATUS + 6d/6e bullets DONE, the 6c bullet ENRICHED with the done analysis (9 `_ => null` nodes must stay
`null`; the stateful-`StoreKindVisitor` shape; the coverage caveat that `StoreKindOf` is NOT byte-exact-covered so 6c
needs an arm-by-arm diff, not a rushed pass), resume-prompt RESUME-AT (→ 6c + 6f). **NEXT: 6c (delicate — carefully)
then 6f.**

