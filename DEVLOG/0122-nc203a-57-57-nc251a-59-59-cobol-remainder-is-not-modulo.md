## Entry 122 — 2026-03-20: NC203A 57/57, NC251A 59/59 — COBOL REMAINDER Is Not Modulo

### The bug

`decimal.Remainder(174, 16)` returns 14. COBOL says the answer is 1.

COBOL-85 §14.9.11 GR4: the REMAINDER is `dividend - truncatedQuotient × divisor`, where
`truncatedQuotient` is the quotient **as stored in the GIVING field** — with the GIVING
field's precision applied. For `DIVIDE 16 INTO 174 GIVING C(PIC ****.9) REMAINDER R`:
- Exact quotient: 10.875
- Stored in GIVING (1 decimal): 10.8
- COBOL remainder: 174 − 10.8 × 16 = 1.2 → truncated to REMAINDER field → 1
- .NET `decimal.Remainder`: 174 − 10 × 16 = 14 (uses integer truncation)

The difference: COBOL uses the GIVING field's decimal precision for truncation. .NET uses
integer truncation. When the GIVING field has decimal places, the results diverge.

### Three bugs in one commit

1. **SafeRemainder**: `decimal.Remainder` throws `DivideByZeroException` on zero divisor.
   Added `SafeRemainder` (mirrors existing `SafeDivide`) with zero check → SizeError flag.
   This was the crash that made NC203A/NC251A unrunnable.

2. **COBOL REMAINDER semantics**: New `IrCobolRemainder` instruction carries the quotient
   accumulator value and the GIVING field's fraction digit count. Runtime
   `ComputeCobolRemainder` truncates the raw quotient to the GIVING precision, then
   computes `R = dividend − truncatedQ × divisor`. No read-back from the GIVING field
   needed — avoids the numeric-edited decode problem entirely.

3. **Numeric edited REMAINDER destination**: `ComputeCobolRemainder` was calling
   `EncodeNumeric` (raw digits) for the output. For `PIC .9999/99999,99999,99`, this
   produced `00000000926535897932` instead of `.0000/92653,58979,32`. Fixed by checking
   `destPic.Category == NumericEdited` and calling `FormatNumericEdited` instead.

### Design decision: accumulator, not read-back

First attempt read the quotient back from the GIVING field after it was stored. This failed
for numeric edited GIVING fields (`PIC ****.9`) because `DecodeNumeric` can't parse edit
characters like `*` and `.` back into a number. The correct approach: keep the raw quotient
in the accumulator (a CIL local variable), truncate it to the GIVING field's precision using
`decimal.Truncate(q * 10^f) / 10^f`, and use that for the remainder calculation. No
decode-from-edited needed.

### Numbers

- NC203A: **57/57** (was crashing with DivideByZeroException)
- NC251A: **59/59** (was crashing with DivideByZeroException)
- 119 unit, 182 integration (1 skip), guard ALL GREEN

---

