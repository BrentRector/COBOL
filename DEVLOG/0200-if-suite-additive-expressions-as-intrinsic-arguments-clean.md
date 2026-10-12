## Entry 200 — 2026-05-28: IF suite — additive expressions as intrinsic arguments (CLEAN 28→37)

The transcendental cluster (LOG/LOG10/ATAN/SIN/SQRT) had a tell-tale signature: `FUNCTION
LOG(E + .001)` computed exactly `LOG(E) = 0.999999`, and `LOG(1 - .1)` likewise dropped its
second term. The `+ .001` / `- .1` was being discarded.

`BindSubscriptSegment` routed a segment to the full arithmetic parser only when it contained a
multiplicative/power operator (`* / **`) or the FUNCTION keyword. An additive expression like
`E + .001`, `B + C`, `1 - .1`, or `B - 2` fell instead to the simple base-name path, whose
relative-offset handler recognises only the `IDENT ± INTEGER` subscript form (and looked for a
`SUB_INTEGERLIT` specifically) — so a decimal second operand, or an identifier/literal first
operand, was silently dropped, leaving just the base term.

Fix: add `SUB_PLUS`/`SUB_MINUS` to the operators that route a segment to the arithmetic parser.
A relative subscript `IDENT ± integer` yields the identical bound tree through that parser
(`BoundBinaryExpression(load IDENT, Add/Subtract, literal)`), so nothing regresses; and a single
signed literal (`+8`, `-3`) is one SIGNED_INTEGERLIT token with no separate `SUB_PLUS`/`SUB_MINUS`,
so it still takes the simple path. The dedicated relative-offset block is now effectively
superseded for spaced `±` forms but left in place.

Verified `LOG(E+.001)=1.00036`, `LOG(1-.1)=-0.10536`. IF CLEAN 28→37, FAIL* 13→4 — nine
transcendental tests cleared. Guard ALL GREEN (999 / 336 / 95).

