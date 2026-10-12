## Entry 192 — 2026-05-28: Intrinsic-function arguments as arithmetic expressions (NIST IF suite — foundation)

Begin the non-NC NIST suites. All 45 IF (intrinsic-function) tests failed to compile because
FUNCTION arguments only accepted simple subscript forms. Fixes (grammar/lexer/binder/runtime),
all guard-verified ALL GREEN (95 NC + 999 unit + 336 integration):

- **Arithmetic in arguments (ISO §15).** FUNCTION args are captured in SUBSCRIPT lexer mode
  (this *preserves* the COBOL comma/space separators — essential so `MAX(-4, 7, 3, -8)` stays
  four args and is not re-read as `3 - 8`). Added `* / **` to SUBSCRIPT mode + `subToken`.
  `BindSubscriptTokensAsArithmetic` is now a real precedence parser over SUB tokens
  (additive→mult→power→unary→primary, parens, qualified/subscripted refs, decimals); segments
  containing `* / **` route to it. (A first attempt — suppressing SUBSCRIPT mode and using a
  default-mode argumentList — was reverted: default mode skips the comma separators, so
  `MAX(A, -B)` collapsed to `A - B`; it also regressed 11 multi-arg integration tests.)
- **Argument separation respects nesting.** `SplitSubscriptTokens` now tracks paren depth
  (only splits at depth 0) and never splits after the FUNCTION keyword, so nested/multi-arg
  function args — `ACOS(FUNCTION ACOS(D / D))`, `MEAN(FUNCTION X(A, B), C)` — stay intact.
  `ParseSubPrimary` recognizes a nested `FUNCTION name(args)`.
- **Function call as a comparison operand.** A numeric intrinsic call used directly in a
  condition (`IF FUNCTION ACOS(0.5) >= MIN-RANGE`) is now classified as an arithmetic operand
  (ConditionLowerer), evaluated via the decimal accumulator. Fixed COBOL0504 for numeric funcs.
- **Runtime robustness.** Out-of-domain math (NaN — ACOS of |x|>1, SQRT of negative, LOG of ≤0)
  and ±Infinity no longer crash the decimal cast: `IntrinsicFunctions.FromDouble` maps NaN→0,
  clamps ±Inf/overflow to decimal.Max/Min.

IF: 0→ (2 clean, plenty still failing — many runtime crashes and wrong-value FAIL* remain,
to be worked next; the earlier "19 clean" was inflated by partial reports from crashing runs).
Added `scripts/run-suite.sh <PREFIX>` to survey any suite's compile/run/FAIL* status.

---

