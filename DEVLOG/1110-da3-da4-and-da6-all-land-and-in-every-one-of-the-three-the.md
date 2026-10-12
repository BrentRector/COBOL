## Entry 1110 - 2026-07-29 19:20 PDT - DA3, DA4 and DA6 all land, and in every one of the three the real defect was DUPLICATION

Owner directive: implement the real, ISO-agreed fixes, all of them, to production quality - refactor to whatever
extent that takes. Three queue items closed, and the striking thing is that none of the three was ultimately a
missing feature. Each was ONE RULE implemented in several places, with one of those places out of step.

**DA6 - the arithmetic operand class, and the design the failed attempt bought.** §8.8.1.1 admits only "an
identifier referencing a NUMERIC data item, a numeric literal, the figurative constant ZERO" in an arithmetic
expression, so a group (class alphanumeric), an elementary alphanumeric/national item, and a reference-modified
slice (§8.4.2.4) are all inadmissible. Entry 1109 recorded the first attempt failing with 79 conformance failures
because the check sat at `RefExpr`, a deliberately CONTEXT-FREE chokepoint that also serves intrinsic arguments.

The fix is that the operand context TRAVELS BY PARAMETER - the same discipline the render-side receiver already
follows (P7 Step 3: "never mutable context state"). ⛔ And an ENUM, not a bool, for three concrete reasons: a bool
reads as `BindExpr(node, true)` at the call site; it does not survive a third context; and as an OPTIONAL parameter
it silently breaks the method-group conversions this spine is used through (`Select(host.Expr.BindExpr)`) - which is
literally how my first pass failed to compile, in `ArithmeticBinder`. So the public surface is TWO
intention-revealing entries over one private core: `BindExpr` (arithmetic - the default, and all ~36 existing
callers keep working untouched) and `BindFunctionArgumentExpr` (the ONE opt-out), with `BindExprCore` threading an
`OperandContext` enum. No caller passes a flag, and the 79 failures stay fixed because an argument's legality comes
from its function's §15.x argument rule, not §8.8.1.1.

Strict now rejects all three shapes at all four editions via COBOLNET0844 - reused, not minted, because 0844
already IS "not a numeric operand (§8.8.1.1)" for a constant-name in the same position. Under `--permissive` both
group kinds finally decode IDENTICALLY (`R=001235`), which was the actual defect: a PIC X-leaf group computed while
a PIC 9-leaf group threw at run time, so the operand whose digits were unambiguous failed and the merely-textual one
succeeded. `NumericRenderer`'s group arm moved to `IsImageCapable` in the SAME change set - correct only paired with
the rejection, since it is now reachable only under `--permissive`.

**DA3 - the hex literal, where the root cause was THREE COPIES of one dispatch.** §8.3.3.2 Format 2 is the
hexadecimal-alphanumeric FORMAT *of* the alphanumeric literal and §8.3.3.2.1 makes every format of it "of the class
and category alphanumeric", so `X"…"` belongs wherever an alphanumeric literal belongs. It did not: a relation
condition staged loud as "comparison operand" at run time while the SAME literal worked in a MOVE.

I added the hex arm, rebuilt, and the comparison still threw - which was the useful moment. The
"non-numeric literal → operand" chain existed in THREE hand-maintained copies: `ExpressionBinder.LiteralOperand`,
`IntrinsicBinder.NonNumericOperand`, and inline in `ConditionBinder`'s comparison-operand binder. I had patched two.
Adding a fourth arm to a third copy would have guaranteed the next literal form broke the same way, so the fix
EXTRACTED one canonical `NonNumericLiteralOperand` mapping that all three now call. A new literal form is now a
one-line change in one place. That is the difference between fixing the instance and fixing the class.

**DA4 - a function-identifier in every STRING/UNSTRING SENDING position.** §14.9.43.2 / §14.9.48.2 write those
operands as identifier-N and §8.4.3.1.2 Format 1 makes `function-identifier-1` a FORMAT of an identifier, so all
four admit one. None of them PARSED before ("no viable alternative at input 'FUNCTION'") - the rejection happened a
whole stage earlier than the string-context renderer DA2 fixed, which is why DA2 could never reach it.
⚠ The INTO phrases are deliberately NOT opened: §8.4.3.2.3 SR1, "A function-identifier shall not be specified as a
receiving operand." All four changed positions are sending. `functionCall` is keyword-led so it goes first in each
alternative and cannot shadow `dataReference`.

The UNSTRING source needed a bound-tree change, and it made the code SIMPLER rather than more complex.
`BoundUnstringStmt.Source` was a `Place`, which a function result has none of; it is now a `BoundOperand` like every
other sending operand in these two statements. The emitter had only ever been WRAPPING that Place to reach
`OperandText.AsString` - THE one string-context renderer - so it now passes the operand straight through, one
indirection lighter. SR2's category screen reads whichever shape arrived (a field's PICTURE or an intrinsic's §15.2
result category), so `UNSTRING FUNCTION ORD(C)` is still correctly refused as category numeric.

**THE PATTERN ACROSS ALL THREE, WHICH IS THE THING WORTH KEEPING.** DA3 was three copies of a literal dispatch.
DA5 (yesterday's) was two predicates for one question. DA6 was one rule enforced at a site that could not know its
own context. In each case the reported symptom was a single construct failing, and in each case fixing only that
construct would have left the mechanism that produced it intact. The tell is always the same: the same rule
written down more than once.

**Gates.** Greenfield Unit 942/942, zero skipped. FULL Conformance **4137/4137, zero skipped, nothing red** - +14
over 4123, being the two new goldens, three DA6 negative fixtures, and nine `ArithmeticOperandClassTests` facts
(which own the `--permissive` leniency, the both-group-kinds consistency, and the false-positive guard proving
alphanumeric INTRINSIC arguments still compile under strict).

