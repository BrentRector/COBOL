## Entry 197 — 2026-05-28: IF suite — two systematic value bugs: untrimmed string args + dropped nested subscripts (CLEAN 8→17)

Grouping every IF FAIL* by function name turned 30-odd individual failures into a handful of
shared root causes. Two fixes cleared most of them.

**1. String intrinsics were trimming their argument (REVERSE/UPPER-CASE/LOWER-CASE, 11 fails).**
`REVERSE(WS)` for `WS PIC X(10) = "tumble"` produced `COMPUTED= elbmut` where the test expected
`CORRECT = "    elbmut"` — the value was right but left-justified instead of right-justified.
Cause: the alphanumeric-argument path emitted `StorageHelpers.ReadFieldAsString`, which does
`.TrimEnd()`. Per ISO §15 an intrinsic argument is the data item's *full* content including
trailing spaces — REVERSE turns them into leading spaces, and the result must keep the field
width. Switched the arg path to the existing `ReadFieldAsRawString` (no trim). Verified the
space-sensitive functions are unaffected: TRIM/NUMVAL/NUMVAL-C/NUMVAL-F all `.Trim()`
internally, and FUNCTION LENGTH should return the *defined* size — so raw is not just safe, it
also fixes LENGTH (was returning the trimmed length).

**2. Subscripted arguments dropped their subscript (MEAN/MEDIAN/RANGE/MIDRANGE/VARIANCE/SUM,
~35 fails).** `MEDIAN(IND(1), IND(2), IND(3))` over `ARR VALUE "40537"` / `IND OCCURS 5 PIC 9`
should be `MEDIAN(4, 0, 5) = 4`, but computed `40537` — the whole base table, the subscript
silently dropped. Cause: `BindSubscriptSegment` extracted the base name then *broke* at the
nested `(`, treating it only as a possible relative `±N` offset; an actual `(subscript)` group
was ignored. Inside SUBSCRIPT mode a nested `(` pushes another SUBSCRIPT mode (CobolLexer.g4
SUB_LPAREN), so the inner subscripts arrive as ordinary SUBSCRIPT tokens. Added a nested
SUB_LPAREN…SUB_RPAREN handler in `BindSubscriptSegment`: collect the balanced inner tokens,
split on depth-0 commas (reusing `SplitSubscriptTokens`), bind each as a subscript, and build a
subscripted `BoundIdentifierExpression(sym, cat, subs)` — the same node the normal data-ref
path produces. A `:` inside the group routes to reference-modification instead. This is the
single binder gap behind every statistical-list function, since they all take subscripted or
table arguments.

Result: IF CLEAN 8→17, FAIL* 32→24, the lone remaining crash (IF110A) also cleared. Guard ALL
GREEN (999 / 336 / 95). Remaining IF tail: per-function value/precision fails plus 3 rc=0
no-output driver modules and the IF127A timeout.

