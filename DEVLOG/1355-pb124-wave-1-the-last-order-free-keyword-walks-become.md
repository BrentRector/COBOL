## Entry 1355 — 2026-08-29 00:57 PDT — PB124 wave 1: the last order-free keyword walks become positional — and the ISO TRIM order is argument-1 FIRST, so my own pb117 golden was carrying the vendor habit

Three binders walked their §15.x.2 keyword formats order-free (kb/Work PB124 — AR-15.3-7 / FMT-15.96.2,
batch 7): BindTrim took LEADING/TRAILING anywhere and repeatedly; BindLengthFamily swallowed the word
PHYSICAL wherever it appeared — and PHYSICAL is NOT reserved, so `LENGTH(PHYSICAL)` consumed a user's
data-name and degraded the call to zero arguments; BindSubstitute's pending-flag accumulation accepted
`FIRST ANYCASE` (the reverse of the printed order), repeats, and keywords BETWEEN argument-2 and argument-3.
All three now walk positionally on the BindFindString pattern, each violation drawing COBOLNET1504 with the
format spelled out. The recovered legal source is pinned positively: a data item NAMED PHYSICAL measures as
itself, `LENGTH(WS-G PHYSICAL)` stays the keyword form.

The spec-first read paid twice. §15.96.2's diagram (figure notes confirming) places [LEADING|TRAILING]
AFTER argument-1 — `TRIM(x LEADING)`, not the GnuCOBOL/IBM habit `TRIM(LEADING x)` — and my OWN
pb117_trim_sequential golden used the vendor order, accepted by the then-lax binder; corrected in this
commit. And FMT-15.96.2's refuter found the edition-gate hole the positional walk alone would have kept: a
table(ALL) argument enumerates a variable argument list — the 2023 argument-2 form whatever the element
count — so `TRIM(E(ALL))` at --std 2014 now draws COBOLNET1502 where the old `operands.Count > 1` gate saw
one operand. RV-15.96.4-3 re-verdicted on PB117's evidence (the sequential fold + the interleaved pin).
AR-15.3-7 → CONFORMS, FMT-15.96.2 → CONFORMS, RV-15.96.4-3 → CONFORMS. PB124 STAYS OPEN — the
class/category model half (Index/Alphabetic/function-usage), the boolean-predicate mis-route, the
always-integer screen, and the variable-length-group prohibitions are the remaining waves.

