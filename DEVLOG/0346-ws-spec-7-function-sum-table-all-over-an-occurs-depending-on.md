## Entry 346 — WS-SPEC #7: FUNCTION SUM(table(ALL)) over an OCCURS DEPENDING ON table (SUM-scoped active mask)

Backlog #7 (M1/'85 — the 1989 Intrinsic Function amendment). `FUNCTION SUM(T(ALL))` over `T … OCCURS 1 TO 5
DEPENDING ON N` summed all 5 physical slots, not the N currently-active ones (ISO §15.x / §8.4.2.3.4: the ALL
range over an ODO table is the current DEPENDING value). `ExpressionBinder.ExpandAllSubscript` statically
expanded ALL to the OCCURS MAXIMUM and never inspected `Occurs.DependingOnSymbol`.

Fix (no new IR — builds on existing nodes): ExpandAllSubscript now collects per-level OccursInfo, and for SUM
multiplies each occurrence beyond the level minimum by a runtime 1/0 active-mask
`MAX(0, MIN(1, depending - (idx-1)))` (= 1 when depending ≥ idx, else 0), so inactive tail slots contribute 0.
The mask is built as ordinary FUNCTION MAX/MIN + arithmetic bound nodes, which lower through the existing
expression machinery (a FUNCTION nested inside arithmetic) — verified by the test.

SCOPE: SUM only. A 0-mask is exact for an additive reduction but would corrupt count-sensitive aggregates
(MEAN/MEDIAN/MIDRANGE/RANGE/VARIANCE/…), which still expand to the maximum over an ODO table(ALL) — no worse
than before. The complete fix for all aggregates is a runtime-range argument (pass the table base + runtime
count, runtime iterates) that restructures the fixed `object[]` arg materialization; deferred as a follow-up
(noted in docs/SPEC_FIX_RECIPES_M1.md). Fixed-capacity tables are unaffected (no DependingOnSymbol → no mask).

Test (`SpecFixTests.SumAllSubscript_OverOccursDependingOn_UsesCurrentLength`): fill T(1..5)=1,2,3,9,9, set
N=3, `COMPUTE WS-R = FUNCTION SUM(T(ALL))` → 006 (1+2+3), not 024.
Guard ALL GREEN: **1047 unit / 424 integration (+1) / 364 NIST**, 0 regressions.

