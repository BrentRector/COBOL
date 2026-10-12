## Entry 1014 — 2026-07-23 09:58 PDT — Conformance fix-queue: CA8 + V56 (conditions) — a bare standard-float SIGN condition tests the IEEE sign bit (Format 2, §8.8.4.7.4 GR2), and a float relation under STANDARD-DECIMAL compares in SDIDI, not native double (§8.8.4.2.4)

**CA8 (MINOR/M) + V56 (MINOR/S), both in `ConditionRenderer.cs`, landed together** (independent-minors batch item 2 —
shared-file, collision-free edit sites: CA8 in `RenderSign`, V56 in `RenderRelational`).

**CA8 — Format-2 float sign condition.** ISO §8.8.4.7.3 SR2: a sign condition whose data-name-1 is a single
standard-float item NOT enclosed in parentheses is Format 2; §8.8.4.7.4 GR2 then tests the IEEE-754 SIGN BIT
"regardless of whether the content would evaluate to true in a NUMERIC class test or a ZERO sign test" — so **+0.0 IS
POSITIVE** and **−0.0 IS NEGATIVE**. `ConditionRenderer.RenderSign` rendered EVERY sign condition (float included) with
the Format-1 algebraic test `v.Expr > 0 / < 0 / == 0`, so a bare-float POSITIVE on +0.0 was false and −0.0 IS NEGATIVE
failed. FIX: a `Format2Float` flag on `BoundSignCondition` (defaulted false), set in `ConditionBinder` by
`IsFormat2FloatSign` — which reduces the operand's parse subtree to its SOLE unparenthesized `dataReference` (mirroring
`IntrinsicBinder.SoleDataReference`; the list-patterns exclude any operator/unary-sign and `primaryExpression().dataReference()`
excludes the `LPAREN … RPAREN` alt, realizing SR2's "not enclosed in parentheses") AND checks the resolved item's
`Pic.IsFloat`. The paren distinction is invisible in the bound tree (`(FL)` and `FL` bind identically), so it is decided
on the PARSE shape. `RenderSign` emits `!double.IsNegative(Real(v))` / `double.IsNegative(Real(v))` / `Real(v) == 0.0`
for Format 2 (sign-bit read; `floatSendingExempt:true` already suppresses EC-DATA-NOT-FINITE so a NaN/±Inf sign test is
well-defined), keeping the algebraic test for Format 1.

**V56 — float relation under STANDARD-DECIMAL.** ISO §8.8.4.2.4: under native arithmetic a comparison uses native rules,
but under standard-decimal "each operand ... [is] converted to [standard-decimal intermediate] form, and the comparison
made between the two corresponding standard-decimal intermediate data items." `RenderRelational` fired
`if (l.Real || rr.Real) return Real(l) op Real(rr)` UNCONDITIONALLY, so a float-vs-fixed relation under STANDARD-DECIMAL
took the native IEEE branch and `Real(fixed)` rounded the fixed operand to double, discarding precision SDIDI keeps
exact. FIX (two lines): gate the native branch `&& !num.StandardDecimal`, and add `|| l.Real || rr.Real` to the SDIDI
branch so a skipped Real operand routes through `CobolDec.Compare(DecOperand(l), DecOperand(rr))` — `DecOperand` already
lifts a Real via `CobolDec.FromDouble` and a fixed operand EXACTLY via `CobolDec.From`. The figurative-vs-ZERO float leg
(:191) is left unchanged: comparing a float to zero agrees native-vs-SDIDI (sign/zero-ness is preserved), so it has no
observable divergence.

**Blast radius.** Native-mode behavior is byte-identical — with no OPTIONS ARITHMETIC clause `num.StandardDecimal` is
false, so V56's line-151 guard reduces to the old condition and a Real operand never reaches the widened SDIDI branch;
CA8's Format2 path is entered only for a bare standard-float sign condition. **Characterization 33/33 byte-neutral.**
**Goldens:** `2002/ca8_float_sign_format2` (FLOAT-LONG is a 2002 usage here per D16 — +0.0 IS POSITIVE, `(FL)` Format-1
control, −0.0 IS NEGATIVE via `COMPUTE FL = FL * -1`, a normal positive); `2014/v56_reldec_float_sdidi`
(STANDARD-DECIMAL is 2014 — COMP-2 1.0 vs `9V9(17)` 1.00000000000000001 ⇒ NE, pre-fix EQ). **Gate (wave-local):** both
goldens compile-strict+run+match; manifest 5/5; conditions/comparison/float golden sweep 404/404 green across editions;
Debug build clean. Re-scout `wf_a09670d5-cdc` confirmed both valid with only NumericRenderer cross-reference line drift.

