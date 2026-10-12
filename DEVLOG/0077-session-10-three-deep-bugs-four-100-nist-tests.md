## Entry 077 — 2026-03-15: Session 10 — Three Deep Bugs, Four 100% NIST Tests

**Session**: #10
**Time**: ~2 hours

### Starting State
- NC101A (MULTIPLY): 93/93 — 100% pass
- NC171A (DIVIDE F1): 108/108 — 100% pass
- NC106A (SUBTRACT F1): 116/126 — 92% pass, 11 failures
- NC176A (ADD F1): 98/124 — 79% pass, 27 failures

### Ending State
- NC101A: 94/94 — 100% pass (byte-for-byte match)
- NC171A: 109/109 — 100% pass
- NC106A: 127/127 — 100% pass (was 11 failures)
- NC176A: 125/125 — 100% pass (was 27 failures)
- Unit tests: 99/99 pass
- Integration tests: 15/15 pass (7 skipped for unimplemented features)

### Bug 1: Multi-Operand ADD/SUBTRACT Did Incremental Operations (27 NC176A failures fixed)

The COBOL spec says: "All operands preceding TO are added together, and this sum is added to each identifier following TO." Our compiler was adding each operand individually to each target, applying rounding at each step. For `ADD 1.1 2.4 6 TO WS-FIELD ROUNDED`, we were doing:
1. WS-FIELD = 0 + 1.1 = 1.1, round to 1
2. WS-FIELD = 1 + 2.4 = 3.4, round to 3
3. WS-FIELD = 3 + 6 = 9

But the correct behavior is: sum = 1.1 + 2.4 + 6 = 9.5, then WS-FIELD = 0 + 9.5 = 9.5, round to 10.

**Fix**: New accumulator pattern in IR — `IrInitAccumulator`, `IrAccumulateField`, `IrAccumulateLiteral`, `IrAddAccumulatedToTarget`, `IrSubtractAccumulatedFromTarget`. The binder sums all operands into a decimal accumulator first, then applies the sum to each target with that target's rounding mode. New `AddAccumulatedToField` and `SubtractAccumulatedFromField` runtime methods.

This also fixed the "WRONGLY AFFECTED BY SIZE ERROR" failures: the old code would modify the target with intermediate values before overflow was detected on a later operand. The spec requires the target to be unchanged if SIZE ERROR occurs.

### Bug 2: PIC Parser Mishandled Decimal Point in Numeric-Edited (NC106A display)

PIC `9(16).99` was being parsed as TotalDigits=19, FractionDigits=0 — the `.` was counted as a digit position instead of a decimal point insertion. This caused `FormatNumericEdited` to produce output without decimal points.

**Root cause**: The PIC parser's switch case lumped `.` with all other editing symbols (Z, *, +, -, $, B, 0, /) and incremented `integerDigits` for all of them. But `.` is a decimal point insertion — it marks the implied decimal position and contributes to storage length but NOT to digit count.

**Fix**: Split the PIC parser cases:
- `.` → sets `pastDecimal = true`, increments `insertionChars` (not digits)
- `,`, `B`, `/` → insertion editing, increments `insertionChars` only
- `Z`, `*`, `+`, `-`, `$`, `0` → replacement editing, increments digit counts

Also rewrote `FormatNumericEdited` to split digits into integer and fraction parts, then insert the `.` at the proper position.

### Bug 3: Float-to-Double Precision Loss in WouldOverflow (NC106A limit tests)

`WouldOverflow` used `Math.Floor(Math.Log10((double)Math.Abs(intVal)))` to count digits. For `intVal = 999999999999998765` (18 digits), the `(double)` cast rounds to `1.0E+18`, making `Log10` return 18.0 and counting 19 digits. Since TotalDigits was 18, the function incorrectly reported overflow.

**Fix**: Replaced floating-point digit counting with integer-only `CountDigits` — a simple `while (value > 0) { count++; value /= 10; }` loop. No precision loss possible.

### Integration Tests Fixed

All 22 integration tests were pre-existing failures (not our regression). Root cause: the ANTLR grammar requires statements inside named paragraphs, but test programs had statements directly under `PROCEDURE DIVISION.` with no paragraph name. Also fixed: DISPLAY of identifier fields (was showing `[WS-NUM]` placeholders instead of actual values), GOBACK statement not being lowered.

### Canonical Expected Output Files

Saved `tests/nist/valid/{NC106A,NC171A,NC176A}.txt` as regression baselines. NC101A already had one.

### Lessons

1. **Floating-point digit counting is a trap.** `Math.Log10((double)bigLong)` silently rounds, giving wrong digit counts for 17-18 digit numbers. Use integer arithmetic.
2. **The spec's phrase "all operands are summed" isn't just style — it's semantics.** Per-operand rounding and per-operand overflow detection produce different results than sum-first-then-apply.
3. **PIC parsing for edited fields is much more complex than numeric fields.** Insertion characters (`.`, `,`, `B`, `/`) contribute to storage but not digit count. Replacement characters (`Z`, `*`) take digit positions. Getting this wrong produces subtly corrupt displays.

---

