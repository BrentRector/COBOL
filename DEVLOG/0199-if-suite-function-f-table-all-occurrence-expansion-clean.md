## Entry 199 — 2026-05-28: IF suite — FUNCTION f(table(ALL)) occurrence expansion (CLEAN 18→28)

The remaining statistical-function fails (MEAN/MEDIAN/RANGE/MIDRANGE/SUM/VARIANCE) all shared
one sub-test: the ALL-subscript form, e.g. `FUNCTION MEAN(IND(ALL))` where `IND OCCURS 5`. Per
ISO §15.4 this passes *every* occurrence of the table as a separate argument — `MEAN(IND(ALL))`
≡ `MEAN(IND(1), IND(2), IND(3), IND(4), IND(5))`. We were binding `IND(ALL)` as a single
reference with the literal "ALL" as its subscript, which read garbage.

Fix in `BindFunctionCall`: after assembling the argument list, expand any `table(ALL)` reference
in place. `IsAllSubscriptedRef` detects a `BoundIdentifierExpression` whose subscript list
contains an "ALL" literal; `ExpandAllSubscript` finds the ALL position, reads the occurrence
count from the symbol's `OccursInfo.MaxOccurs`, and emits one `BoundIdentifierExpression(sym,
cat, [...idx...])` per occurrence (1…n), preserving any fixed subscripts in other dimensions.
The expanded element references then flow through the normal numeric-argument path.

Verified: `MEAN(IND(ALL))` over `ARR VALUE "40537"` = 3.8, `SUM(IND(ALL))` = 19. IF CLEAN
18→28, FAIL* 23→13 — ten statistical tests cleared at once. Guard ALL GREEN (999 / 336 / 95).

