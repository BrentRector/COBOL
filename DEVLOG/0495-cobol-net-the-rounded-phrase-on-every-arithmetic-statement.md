## Entry 495 — COBOL.NET: the ROUNDED phrase on every arithmetic statement (ISO §14.7.4), per receiver

The harness finding from Entry 494 (`MULTIPLY … ROUNDED` truncating) is the tip of a whole missing feature: the
**ROUNDED phrase** was parsed but discarded on every arithmetic verb. Implemented it completely, per receiver, on
ADD / SUBTRACT / MULTIPLY / DIVIDE / COMPUTE (the grammar already had `roundedPhrase: ROUNDED (MODE IS?
roundingModeName)?` on `receivingArithmeticOperand` / `multiplyByOperand` / `computeStore` — this was bind + emit +
runtime-wiring, no grammar change).

**Bound tree.** A new `record Receiver(Place Place, CobolRounding Rounding)` replaces the bare `IReadOnlyList<Place>
Targets` on all nine arithmetic nodes. Each resultant identifier now carries its own rounding mode — `ADD A TO B
ROUNDED MODE IS NEAREST-EVEN C ROUNDED MODE IS TRUNCATION` rounds B and C differently.

**Binder.** `RoundingOf(roundedPhrase?)` maps the phrase per ISO §14.7.4.3: no phrase → `Truncation` (rule 2); a bare
`ROUNDED` → `NearestAwayFromZero` (rule 1 → the §11.9.6 r2 default — see the open item below); `MODE IS x` → the named
mode. `Receivers(...)` resolves each `receivingArithmeticOperand`/`multiplyByOperand`/`computeStore` to a `Receiver`.

**Emitter — and the division subtlety.** `StoreArith` threads each receiver's mode into `CobolNum.Store`, which rounds
when it rescales the result down to the receiver scale. That covers ADD/SUBTRACT/MULTIPLY (their result scale ≥ the
receiver scale) and COMPUTE of a non-division expression. But a **division** quotient computed directly at the receiver
scale is already truncated, so `Store`'s rescale is a no-op and the mode would be silently ignored (`DIVIDE 20 BY 3
GIVING X` with X scale 1 → 6.6, not 6.7). Fix (ISO §8.8.1 / §14.7.4): the renderer now rounds the quotient *at the
receiver scale with the receiver's mode* in one exact step (`CobolNum.Divide` → `RoundDiv` uses the true integer
remainder, so no guard digits are needed) when the division's working scale equals the receiver scale
(`EmissionContext.TargetRounding`); when an operand carries more fraction digits than the receiver, the quotient keeps
those extra digits with TRUNCATION and `Store` does the single rounding. No `long`-overflowing guard-digit inflation —
the deeper guard-scale model for divisions *nested inside* a larger expression stays deferred to the Int128 carrier.

**Verification (the spec is authority, the oracle is a net).** 21 differential tests pin each of the 8 modes to
**hand-computed §14.7.4.3 values** — NEAREST-EVEN 2.5→2 *and* 3.5→4 (banker's rounding both directions), the
sign-sensitive trio on a −2.5 tie (TOWARD-GREATER→−2, TOWARD-LESSER→−3, AWAY-FROM-ZERO→−3) displayed via SIGN LEADING
SEPARATE, fractional-receiver-scale, and the no-ROUNDED-truncates regression — across COMPUTE / DIVIDE-GIVING /
in-place ADD / in-place MULTIPLY. The legacy NIST-85 corpus only ever exercises bare ROUNDED (MODE is COBOL-2002), so
"matches legacy" is a weak witness for the seven other modes; those are pinned to the spec, and bare ROUNDED + the
truncation default are additionally cross-checked against the legacy (they agree). Conformance **174 → 195** green; 13
unit green; legacy oracle untouched (greenfield-only change).

**Deferred / next (all spec-staged, not test-scoped):** (1) `ROUNDED MODE IS PROHIBITED` on an *inexact* result must
raise EC-SIZE-TRUNCATION and leave the receiver unchanged (§14.7.4.3 r7) — that is the SIZE ERROR feature (§14.7.5,
the `TryStore` two-phase), the next item; PROHIBITED on an exact result is correct today (no loss). (2) The
`ds > TargetScale` path is truncate-then-round, which has a sticky-bit error only at adversarial exact ties for
NEAREST-EVEN / NEAREST-TOWARD-ZERO — same root as the deferred Int128 guard-scale work. (3) A **bare** `ROUNDED`
currently always means NEAREST-AWAY-FROM-ZERO; honoring an OPTIONS `DEFAULT ROUNDED MODE IS x` (§11.9.6 r1) is the next
commit — the owner-directed "fully parse the OPTIONS paragraph" divert, which makes the whole OPTIONS paragraph
structurally available and wires DEFAULT ROUNDED as ROUNDED's first consumer.

