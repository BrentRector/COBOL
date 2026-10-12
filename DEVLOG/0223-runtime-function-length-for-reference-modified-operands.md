## Entry 223 — 2026-05-29: Runtime FUNCTION LENGTH for reference-modified operands + depth-0 ref-mod colon fix

The audit flagged FUNCTION LENGTH as compile-time only. `LENGTH(x(s:l))` returned the base field
size (or 0), not the substring length. Two fixes:

1. **Runtime LENGTH for ref-mod operands.** `BindLength` now returns `x(s:l)` → `l` (the length
   expression, literal or runtime) and rest-of-field `x(s:)` → `defined-size − s + 1`; every other
   operand still folds via `StaticLength`. So `LENGTH(WS(1:N))` is `N` at runtime.

2. **Depth-0 ref-mod colon (a real parse bug it exposed).** `InterpretSubscriptTokens` detected the
   ref-mod colon with `FindIndex`, which matched a colon *nested* inside a subscript operand —
   `LENGTH(WS(1:N))` was mis-parsed as a ref-mod of the whole `WS(1:N)`. Now only a **depth-0**
   colon (outside nested parentheses) is the ref-mod separator; a nested colon is left for the
   operand's own segment binding. This also fixes `f(T(I)(1:3))`-style nested ref-mod generally.

Verified: `LENGTH(WS(1:N))`=7, `LENGTH(WS(S:))`=18 (S=3), `LENGTH(WS)`=20. Guard ALL GREEN
(1000 / 336 / 149). *Not yet covered:* LENGTH of an ODO group still returns the maximum layout size
rather than the current (DEPENDING ON) size — a separate runtime-size follow-up.

