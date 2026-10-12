## Entry 1126 - 2026-07-30 04:10 PDT - PB6: the pre-merge differential earned its place, and my first fix for it was a silent regression

**THE COMPREHENSIVE PRE-MERGE GATE FOUND A DEFECT NO OTHER LEG COULD.** Conformance, Unit, characterization and
guard-fast were all green. The GnuCOBOL differential's per-case diff showed **10 flips: 8 FIX, 2 REGRESSION** —
and plan §0 tolerates zero unexplained AGREE→divergence flips, so the two had to be traced rather than accepted
on the strength of their titles saying "(extension)".

Both are `CALL … USING BY VALUE` with an alphanumeric and a national operand. Reproduced, the rejection was
`COBOLNET0844` — **DA6's §8.8.1.1 ARITHMETIC screen, firing on a CALL argument.** The grammar production is named
`arithmeticExpression`, the binder called `BindExpr` on it, and `BindExpr` hard-codes `OperandContext.Arithmetic`.

⚖ **THE VERDICT WAS RIGHT BY ACCIDENT.** §14.9.4.3 SR22 (validated): "If identifier-4 or its corresponding formal
parameter is specified with a BY VALUE phrase, identifier-4 shall be of class numeric, object, or pointer." So an
alphanumeric operand IS illegal and rejecting it is correct — GnuCOBOL accepts it as an extension and silently
assumes BY CONTENT, a DIFFERENT passing mode from the one written. But the rule QUOTED was not the rule BROKEN,
and a diagnostic that names the wrong clause sends the programmer to the wrong place to fix it. **A production's
NAME is not its operand's rule** — the same shape DA6 recorded about itself: a rule enforced at a site that could
not know its own context.

**⛔ AND MY FIRST FIX WAS A SILENT REGRESSION THAT LOOKED LIKE SUCCESS.** I added `OperandContext.CallByValue` so
the arithmetic screen would not fire, and screened with `IntrinsicArgumentRules.ClassOf(operand)`. Both cases then
**compiled clean** — which reads as "fixed" and is strictly worse than what it replaced: `ClassOf` maps any
`BoundComputedOperand` to NUMERIC (correct in its own context, where a computed operand really is an arithmetic
expression), so the check was a no-op and SR22 stopped being enforced at all. I had converted a wrongly-worded
REJECT into no enforcement. Caught only because I re-ran the repro instead of trusting a green build — **a fix
whose evidence is "the error went away" cannot distinguish a fix from a deletion.** It now classifies the
underlying `BoundNumRef`'s `Place`, and the comment says why.

`COBOLNET1628` (`call-by-value-operand-class`) names SR22 directly, rejects strict with `--permissive` leniency
(the DA6 disposition), and ships with the negative golden `pb6-call-by-value-alphanumeric`.

**EVERY FLIP IS NOW ATTRIBUTED** — the rule plan §0 states as "attribute every red":

- **6 FIX from PB2** — `FUNCTION CHAR` / `CURRENT-DATE` / `SIGN` / `SUBSTITUTE` / `LOWER-CASE` and
  `CALL with OCCURS DEPENDING ON`, all WE_REJECT to AGREE_ACCEPT. PB2's `CS1503` was a BACKEND COMPILATION
  FAILURE, which the differential counts as a reject; fixing the float path turned them into accepts.
  ⚠ Worth keeping: §0 records the differential as "structurally blind to a change in RUNTIME OUTPUT", and that is
  true — but a runtime-shaped defect that manifests as a *failed compile* is fully visible to it.
- **2 FIX from PB1** — "Intrinsic functions: argument type" and "Category check of Format 1", both
  WE_ACCEPT_THEY_REJECT to AGREE_REJECT: we now reject what GnuCOBOL rejects.
- **2 spec-derived tightenings** — the CALL BY VALUE pair, which stay WE_REJECT_THEY_ACCEPT permanently and
  correctly, the same shape as CA34's recorded flips.

⚠ **A FALSE ALARM OF MY OWN MAKING, worth the line.** `guard-fast`'s re-run reported EXIT 1 while its output
ended `=== ALL GREEN ===`. The exit code was my command chain's: I had ended it with a `grep -c` for
crash/abort/fail strings, and **grep exits 1 when it finds nothing**. Gating on that would have reported a failing
gate on a green run — the mirror of the failure `feedback_gate_on_the_verdict_line` exists to prevent, and the
reason plan §0 says to grep the verdict rather than read the exit code.

**GATES (the comprehensive pre-merge set).** Conformance **4148/4148** · Unit **963/963** · characterization
**33/33** · `guard-fast` **ALL GREEN**, NIST **353 MATCH / 0 REGRESSION**, legacy Unit 1203/1203, Integration
503/504 (1 skipped) · differential **10 flips, all attributed, 0 unexplained**.

