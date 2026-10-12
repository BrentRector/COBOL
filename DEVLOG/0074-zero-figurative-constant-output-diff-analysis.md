## Entry 074 — 2026-03-15: ZERO Figurative Constant + Output Diff Analysis

### ZERO Bug

Figurative constant ZERO was bound as `BoundLiteralExpression("0", Numeric)` — string "0",
not decimal 0m. When the binder compared `IF REC-CT NOT EQUAL TO ZERO`, it checked
`litRight.Value is decimal d` which failed (Value is string). Fell through to the
`IrSetBool(result, true)` fallback — meaning every comparison with ZERO evaluated as TRUE.

This caused: `.00` remark appearing on every test (REC-CT NOT EQUAL TO ZERO was always
"true", so the `.` and DOTVALUE were always written), and PAR-NAME being cleared for
sub-tests (REC-CT EQUAL TO ZERO was always "true" via the same fallback).

**Fix:** Changed ZERO binding from string `"0"` to decimal `0m` in BoundTreeBuilder.

### Remaining Output Mismatches (vs expected file diff)

After the ZERO fix, diffing `print-file.txt` vs `tests/nist/valid/NC101A.txt`:

1. **Missing leading blank line** — expected starts with a blank line, actual doesn't
2. **`*** INFORMATION ***` after every PASS** — BAIL-OUT paragraph falls through to
   BAIL-OUT-WRITE instead of GO TO BAIL-OUT-EX. Traced: both COMPUTED-A and CORRECT-A
   are correctly all-spaces (0x20), CompareFieldToString returns 0 (equal). The comparisons
   are correct but the GO TO inside `IF cond GO TO para` within a PERFORM THRU range isn't
   changing control flow. This is a PERFORM THRU + GO TO interaction bug.
3. **`93 OF 93` vs `093 OF 093`** — numeric-to-alphanumeric MOVE formatting (PIC 999 →
   PIC XXX should produce zero-padded "093", not "93 ").

### Status

**NC101A: 93/93 internal PASS.** Output diff has 3 categories of mismatch remaining, all
in test harness behavior (BAIL-OUT control flow, number formatting, leading blank line).
No arithmetic bugs remain.

---

