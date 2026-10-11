## Entry 1111 - 2026-07-29 21:05 PDT - Naming the sending operand once, and the battery with nothing carried

Two cleanup pieces, both prompted by the owner restating the standing bar: production quality, decade-supportable,
refactor to whatever extent that takes.

**THE REFACTOR I HAD LEFT UNDONE.** DA4 shipped working but with two smells I would have criticised in anyone
else's change. `StrUnstrOperand` had grown to FIVE parameters, four of them mutually-exclusive nullable ANTLR
contexts - the kind of signature a caller silently transposes - and the grammar repeated the same four-alternative
list in FOUR productions. That second one is precisely the "same rule written down more than once" pattern entry
1110 identified as the root cause of DA3, DA5 and DA6. Leaving it would have meant shipping the disease alongside
the cure.

The two shapes are not arbitrary; the general formats distinguish them, so the grammar now does too:
  §14.9.48.2  `UNSTRING identifier-1`              -> an IDENTIFIER only, no literal sender
  §14.9.43.2  `STRING {identifier-1 | literal-1}`  -> an identifier OR a literal
so the sender is a strict SUBSET of the operand, and `strUnstrOperand : strUnstrSender | literal |
figurativeConstant` states that subset relationship directly rather than restating the alternatives. Four
productions now reference the two names. The binder's helper takes ONE context parameter, and `StrUnstrOperand`
delegates its two identifier arms to `StrUnstrSender` so the shapes cannot drift apart. Adding a fifth operand
form is a one-line grammar change.

**⛔ AND THE GRAMMAR IS A LEGACY-SHARED SEAM, which the build proved immediately.** The legacy compiler consumes the
same productions, so `StringStatementBinder` broke in eight places the moment the rule shapes changed - it was
reaching `phrase.dataReference()` where the operand now lives one level down. Updated to go through the named rules,
with the function-identifier arm falling to its existing skip posture (legacy survives only as the P15 differential
oracle, and DA4 is greenfield). This is exactly the case the gate policy reserves `guard-fast` for, so it was re-run:
ALL GREEN, NIST 353 MATCH / 0 REGRESSION, legacy Unit 1203/1203, Integration 503/504.

**THE BATTERY NOW HAS NOTHING CARRIED.** I had flagged characterization and `guard-fast`/NIST as carried forward
from the step-4 gate three separate times while reporting other work as green. Both are now measured at this commit,
and plan §0 says so explicitly instead of quoting a figure whose provenance a reader has to reconstruct.
Characterization 33/33 matched its carried value - so the carry was accurate, which is worth knowing but is not the
same as having checked.

**A DOC DEFECT WORTH NAMING.** DA6's queue heading still read "⏳ OPEN" while its own body recorded it as landed: I
had updated the bullets and missed the line above them. In a register the project treats as live-state SSOT, a
heading that contradicts its body is a defect in itself, not untidiness - a future session reads the heading. Fixed,
and the queue now has ZERO open items.

**Gates.** Greenfield Unit 942/942, zero skipped. FULL Conformance 4137/4137, zero skipped, nothing red - unchanged
across the refactor, which is what behaviour-neutral means. `guard-fast` ALL GREEN with NIST 353 MATCH / 0
REGRESSION. Every figure in plan §0's battery line measured at this commit.

