## Entry 1264 — 2026-08-09 13:55 PDT — PB65: BOOLEAN-OF-INTEGER's argument-1 crosses the wide bridge — the 2^63 cliff dies

RV-15.13.4-1's D1 layer: `FUNCTION BOOLEAN-OF-INTEGER(2^63, 70)` returned seventy zeros, silently. Two
narrowings stacked — the emitter's `AsInt` bridge EC'd any argument past `long` to the checking-off default 0,
and the runtime bit walk stopped at bit 62. §15.13.4 r1's bit configuration is a MATHEMATICAL one (its own
NOTE), and §15.13.3 r1 admits any positive integer, so both narrowings were ours, not the spec's.

The fix mirrors INTEGER-OF-BOOLEAN's earlier widening onto the argument side: the runtime body takes Int128
(walk to bit 126; bit 127 is the sign bit and negatives never reach the walk), and the emitter gains the ONE
wide bridge — `ArgIntWide`/`AsIntWide` beside `ArgInt`/`AsInt`, carrier-total (Real → the new
`IntegerArgWideReal` intake at the FromDouble saturation constant; Dec → unscaled at 0 BEFORE the scale test,
the PB14/PB32 lesson; U → the R10 Widen funnel; scale-0 → RAW, no narrowing and hence no raise point).
BOOLEAN-OF-INTEGER argument-1 is the ONE §15 integer argument whose domain exceeds long — swept the catalog
to confirm — so no other arm changes. Stale doc swept in the same file: INTEGER-OF-BOOLEAN's summary still
claimed the signed-64-bit channel its body no longer has.

Golden `pb65_boolean_wide` (2^62 / 2^63−1 / 2^63 / 2^64+5 at length 70, each bit string derived
independently in python, plus the Annex D.10 544→6-position left-truncation control). Wave-local gate:
Conformance ~Intrinsic|~Boolean|~Corpus 778/778, Unit 128/128, the new golden asserted present (1/1).

The row STAYS DIVERGES: its D2/D3 layers — the four class-boolean checkpoints with no arm for a computed
boolean-intrinsic operand (rejects legal boolean comparison/B-AND, ACCEPTS the illegal alphanumeric mirror,
crashes in a numeric context) — are now their own register note, PB68.

