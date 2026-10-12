## Entry 196 — 2026-05-28: IF suite — intrinsic crash-robustness + MAX/MIN result-category propagation (crashes 9→1)

After all 45 IF programs compiled, a batch run surfaced six distinct runtime crashes. Fixed
each at its root in `IntrinsicFunctions`:

- **OverflowException (Int32) in CHAR, INTEGER-OF-DATE, INTEGER-OF-DAY** — these did a raw
  `(int)decimal` cast on argument values that exceeded `Int32`. Added a `ToInt(decimal)` clamp
  helper and routed the casts through it; CHAR additionally clamps to the valid char range
  (`< 0 or > 0xFFFF` → space).
- **ArgumentOutOfRangeException in INTEGER-OF-DATE/DAY and DATE-OF-INTEGER/DAY-OF-INTEGER** —
  out-of-range Gregorian dates threw from `DateTime`. Wrapped in `try/catch` → 0 (the CCVS
  convention for an undefined date result).
- **OverflowException (Decimal) in FACTORIAL** — `FACTORIAL(n)` for large `n` overflows
  `decimal`. Guard: negative → 0, `n >= 28` → `decimal.MaxValue` (28! is the last factorial
  that fits in a decimal).

The harder one was **InvalidCastException (String → Decimal) in MAX/MIN** at
`MOVE FUNCTION MAX("R", I, "I", "a") TO WS-ANUM` (IF119A/IF123A). MAX/MIN are
category-polymorphic (ISO §15): with all-alphanumeric arguments they return the *selected
string*, not a number. The binder already computed this correctly (Entry from this session set
`BoundFunctionCallExpression.Category = Alphanumeric` when every arg is non-numeric) — but the
category was **lost during lowering**. `EmitFunctionCall` re-derived string-vs-numeric from a
*hardcoded function-name switch* that listed the nine always-string functions and therefore
classified MAX as numeric, unboxing its String result to decimal → crash.

Root-cause fix (single source of truth): added `bool ReturnsString` to `IrFunctionCall`,
populated from `func.Category` in `DataMovementLowerer`, and replaced the emitter's name-switch
with that flag. The switch listed *exactly* the same nine names as
`BindingContext.AlphanumericFunctions`, so the bound category is a strict superset — no
regression — and it additionally expresses the per-call polymorphism a static list cannot. This
is the canonical-dispatch discipline: classify once in the binder, propagate, never re-classify
at the leaf.

Result: IF crashes 9→1 (only IF110A remains — FACTORIAL → MaxValue now overflows on *store*
into a smaller field, a SIZE-ERROR-on-MOVE gap, not a function bug). IF402M recovered to rc=0.
Guard ALL GREEN (999 unit / 336 integration / 95 NC). IF now: 8 CLEAN, 32 FAIL*, 1 crash,
3 rc=0 no-output, 1 timeout — the remaining work is the per-intrinsic value-correctness tail.

