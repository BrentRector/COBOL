## Entry 195 — 2026-05-28: IF suite — alphanumeric function comparisons + nested string-function args (all 45 compile)

Two fixes; all 45 IF tests now compile (COMPILE_FAIL 6→0). Guard ALL GREEN.

- **Alphanumeric function as a comparison operand** (`IF FUNCTION UPPER-CASE(X) = "ABC"`,
  CHAR/REVERSE/LOWER-CASE/CURRENT-DATE/WHEN-COMPILED). New `IrStringExprCompare` evaluates the
  function to a System.String and compares it against the other operand (literal/figurative/
  field) via `StorageHelpers.CompareStringValues` (trailing-space-insensitive). ConditionLowerer
  normalizes the function to the left (flipping the operator if it was on the right). Fixed the
  remaining COBOL0504 compile failures.
- **Nested alphanumeric function argument** (`LOWER-CASE(FUNCTION LOWER-CASE("X"))`). The inner
  string-returning call was classified as a numeric argument and unboxed to decimal at runtime
  (InvalidCastException). New `IrStringExprArg` (alongside numeric/alphanumeric/literal args)
  carries it; the emitter pushes the Call's String result directly (no decimal box).

IF: 8 clean, 27 FAIL* (per-intrinsic value bugs), 9 crashes, 1 timeout — all now *compiling*.
Crashes/timeouts and value bugs are the remaining per-function work.

---

