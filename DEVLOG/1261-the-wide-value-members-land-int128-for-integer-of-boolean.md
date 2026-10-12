## Entry 1261 — 2026-08-09 11:51 PDT — The wide-value members land: Int128 for INTEGER-OF-BOOLEAN, the guarded COMBINED-DATETIME fold, and the wide subfield accumulator — four more rows

Three carriers were too narrow and one guard was wired into one of two callers. INTEGER-OF-BOOLEAN
accumulated on a signed long, inheriting a 63-bit maximum from the carrier where §15.45.4 r1b
defines "the unsigned binary value" as a mathematical quantity — a 64-one-bit item silently
returned the EC default; it now rides Int128 (2^64−1 exact, the >126-bit escape LOUD at the D1
boundary). COMBINED-DATETIME's numeric fold had no guard prologue while its FORMATTED-* siblings
had both — the one-of-two-callers shape again: §15.17.3 r1's integer-date range and r2's standard
numeric time form now guard through the SAME SecondsOutOfStandardForm helper. And
CobolDate.Analyze accumulated every subfield through a 32-bit local, wrapping modulo 2^32 at the
tenth fractional digit (item 202 documents up to 18) — SECONDS-FROM-FORMATTED-TIME at ten digits
was wrong by exactly that wrap; the accumulator is wide, the small subfields cast losslessly after
their per-digit narrowing. Golden pb65_wide_values (5 derived lines; the probe's first cut used
VALUE ALL B"1", which §13.18.63 SR10 rejects — corrected to the boolean literal). Date+intrinsic
unit filters 85/85; SpecTraceability 10/10. FOUR rows re-verdict CONFORMS+tested. GAP 4,195 →
4,191; the defect backlog stands at 189.

