## Entry 193 — 2026-05-28: IF suite — nested-function-arg routing + FACTORIAL overflow guard

Follow-up to Entry 192. Two runtime-crash fixes (guard ALL GREEN):
- A function-argument segment that is a nested call (`FUNCTION INTEGER(1.6)`) but contains no
  `* / **` was taking the simple subscript path (treating `FUNCTION` as a data-name) instead
  of the arithmetic parser, so the outer call received a bogus string arg and the dispatcher's
  `numArgs[0]` threw. `BindSubscriptSegment` now also routes segments containing the FUNCTION
  keyword to the arithmetic parser. This cleared 12 of the 16 IF runtime crashes.
- `IntrinsicFunctions.Factorial` overflowed decimal for n ≳ 28; now clamps to decimal.MaxValue
  (and returns 0 for a negative argument).

IF suite now: 3 clean, 22 FAIL* (per-function value bugs), 16 compile-fail, 4 crashes — the
systematic argument/dispatch blockers are resolved; remaining failures are per-intrinsic
correctness (ANNUITY, date conversions, CHAR/ORD, statistical functions, …) and a few more
parse constructs.

---

