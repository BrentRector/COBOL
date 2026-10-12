## Entry 1348 — 2026-08-28 23:47 PDT — PB118 LANDED: the cross-argument class rule reaches the variadic tail — §15.87.3 r2 governs EVERY SUBSTITUTE pair, and the screen stopped at the three declared schema positions

`ArgSchema.MatchedPositions(argCount)` now extends the MatchArgument1 governed set over the variadic TAIL
whenever the tail's kind matches position 0's — generic, so any tail-bearing MatchArgument1 schema is covered,
not just SUBSTITUTE's. The negative `pb118-substitute-tail-class` pins the SECOND pair's national-vs-alphanumeric
mismatch (`FUNCTION SUBSTITUTE(X3 "A" "Z" N"B" N"Y")` → COBOLNET1627 naming argument-4 and citing r2); the
matched all-national pair list still binds (no over-reject). AR-15.87.3-2 → CONFORMS.

