## Entry 1109 - 2026-07-29 17:15 PDT - DA6 attempted and REVERTED: the decision was right, the site was wrong, and 79 failures said so

The owner chose strict rejection with the leniency dialect-gated behind `--permissive`. I implemented it, measured
it, and backed it out. The measurement is the deliverable, so this entry records the negative result rather than a
landed fix.

**FIRST, THE SCOPE WAS WIDER THAN I HAD FILED - AND THE DECISION WAS TAKEN ON MY NARROWER PREMISE.** I had described
DA6 as "a GROUP used as a numeric operand". §8.8.1.1 bars every ALPHANUMERIC arithmetic operand. Measured, all three
forms are accepted today:

    COMPUTE R = X + 1        X PIC X(4) VALUE "0012"   ->  13
    COMPUTE R = X(1:2) + 1   a ref-mod slice           ->   1
    COMPUTE R = G + 1        the group                 ->  35

`NumericRenderer` has FOUR `FromAlphanumeric` arms - literal, ref-mod, group, elementary alphanumeric/national - so
rejecting only the group half would have created a FRESH inconsistency of exactly the kind DA5 and DA6 exist to
remove. Worth noting the elementary arm justifies itself by citing §14.9.25.4 GR6, which is the MOVE rule; a MOVE
citation cannot license an arithmetic operand.

**THEN THE IMPLEMENTATION, WHICH LOOKED RIGHT AND WAS NOT.** The natural site is
`ExpressionBinder.RefExpr` - it is where a resolved data reference becomes a numeric expression operand, and the
method immediately below it, `NonNumericConstantExpr`, already raises **COBOLNET0844** for this very clause when a
non-numeric CONSTANT-name appears there. Reusing 0844 rather than minting a code seemed obviously correct: a data
item is the third shape of one rule.

Strict rejected all three forms; `--permissive` accepted all three with warnings and identical values; and pairing
it with the `NumericRenderer` group-arm migration made the two group kinds finally agree (both `001235` lenient,
both rejected strict). Every targeted check passed.

**Then the full corpus: 79 failures.** And they were not programs abusing the extension. They were
`FUNCTION TRIM(S)`, `SUBSTITUTE`, `FIND-STRING`, `CONVERT`, `RefModArgument_Renders` - **legal alphanumeric
ARGUMENTS to string intrinsics**, governed by §15.3, which §8.8.1.1 has nothing to do with.

**The root cause is structural, and it is the actual finding.** `RefExpr`'s own doc comment says it is "The ONE
dataReference→`BoundExpr` mapping, used by every expression path". That is precisely why it cannot host this check:
it is CONTEXT-FREE by design, serving arithmetic operands and string-intrinsic arguments through the same door. I
put a context-sensitive syntax rule at a deliberately context-free chokepoint. The 79 failures are that mistake
measured, not a tuning problem - no amount of narrowing the predicate fixes a site that cannot know the context.

So it is reverted, clean, and DA6 now carries what the attempt bought: the real scope, the real requirement (thread
an operand-context - arithmetic vs. intrinsic-argument vs. reference-modifier - through the expression spine, which
is a design change, not a guard), and the pairing constraint that `NumericRenderer.cs:186` must move to
`IsImageCapable` in the SAME change set and never before it.

**Why this is worth an entry rather than a silent revert.** I set the bar myself in DEVLOG 1108: adding a diagnostic
risks rejecting legal source, which is a worse defect than the one being fixed, so the false-positive check IS the
change. DA7 passed that bar. DA6 failed it - and it failed it only at the FULL corpus, after every targeted probe I
wrote had gone green. The targeted probes were all in arithmetic contexts because that is what I was thinking about;
none of them was a string intrinsic. That is the lesson: a hand-built probe set inherits the author's framing, and
the corpus is what does not.

**Gates.** Tree reverted to `df8dc11a` behaviour and re-verified: the three alphanumeric forms compute again exactly
as before. Nothing landed from this attempt except the queue entry.

