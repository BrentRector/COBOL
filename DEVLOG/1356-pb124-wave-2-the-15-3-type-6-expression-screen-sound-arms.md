## Entry 1356 — 2026-08-29 01:03 PDT — PB124 wave 2: the §15.3 type-6 expression screen — sound arms only, and the adversarial zoo that bounds them is now a golden

AR-15.3-6's open half: "an arithmetic expression that will always result in an integer value" went entirely
unscreened — FUNCTION CHAR(I / 2) and CHAR(SCALED + 1) bound clean and the runtime landed the fraction
silently. The fix adds exactly the PROVABLY not-always-integral arms to IntegerViolation, where soundness is
the whole design: an arm may fire only when a witness valuation yielding a non-integer provably exists.
Arm 1 — a root quotient of a BARE numeric item by an integral literal d > 1, unless d divides the item's
picture granularity (so I / 2 rejects; a 9PP item / 100 stays legal). Arm 2 — a scale>0 item occurring
purely as additive leaves whose net coefficient k is not cancelled by 10^scale (consecutive results are less
than 1 apart, so a non-integer exists; S − S nets to zero and stays legal, an occurrence inside a
non-additive subtree voids the witness and marks the item opaque).

The adjudication row's broader claim — "a division, a scaled operand inside an expression are decidable at
bind" — was NARROWED on counterexamples, not implemented as written (validate_the_premise): (I/2)*2 ≡ I,
I/I ≡ 1, 362880/PIC-9 (9! is divisible by every 1-digit value), SCALED*10, SCALED−SCALED are each
always-integral legal source that defeats a broader screen. That zoo is pinned COMPILING AND RUNNING as
2023/pb124_always_integral_zoo — the fail-open complement as a positive golden — beside the two negatives
pb124-char-item-div-literal / pb124-char-scaled-additive. AR-15.3-6 → CONFORMS. PB124 stays open (the
class/category model half, the boolean-predicate mis-route, the variable-length-group prohibitions).

