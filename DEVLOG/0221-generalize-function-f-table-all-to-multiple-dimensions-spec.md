## Entry 221 — 2026-05-29: Generalize FUNCTION f(table(ALL)) to multiple dimensions (spec audit follow-up)

A self-audit flagged the `table(ALL)` expansion (Entry 199) as too narrow: it found only the FIRST
`ALL` subscript and used the symbol's own `OCCURS` bound, so `T(ALL, ALL)` expanded only one
dimension and `T(I, ALL)` could take the wrong bound. Rewrote `ExpandAllSubscript` to handle `ALL`
in any/all positions (ISO §15.4): collect the item's OCCURS bounds outermost-first (aligned to
subscript order), then expand the **cartesian product** over every `ALL` position, each using its
own dimension's bound; fixed subscripts are preserved.

Verified on a 2×3 table: `SUM(CEL(ALL,ALL))`=21, `SUM(CEL(1,ALL))`=6, `SUM(CEL(ALL,2))`=7. Guard
ALL GREEN (1000 / 336 / 149) — single-dimension `table(ALL)` (the IF baselines) unchanged.

