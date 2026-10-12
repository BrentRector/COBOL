## Entry 061 — 2026-03-15: DISPLAY Numeric Encoding Fix — Implied Decimal

### The Bug

`EncodeDisplay` used `value.ToString("G")` which embeds a literal decimal point
in the output string. COBOL DISPLAY numeric with implied decimal (PIC 999V99)
stores **digits only** — no decimal point character. For 320.48 in PIC 999V99:
correct storage is `"32048"` (5 bytes), but we were producing `"320.48"` (6 bytes),
which overflowed the field and lost the last digit.

### The Fix

Rewrote both `EncodeDisplay` and `DecodeDisplay` to use `PicDescriptor.FractionDigits`:

**EncodeDisplay**: Scale the decimal value by 10^FractionDigits to get an integer,
then format as zero-padded digits. 320.48 × 10^2 = 32048 → `"32048"`. Right-justified,
zero-filled. Leading `-` for signed negative values.

**DecodeDisplay**: Parse the field as a long integer (digits-only), then divide by
10^FractionDigits to restore the decimal value. `"32048"` → 32048 / 100 = 320.48.
Includes fallback for legacy data with embedded decimal points.

### Result

NC101A: 241 lines, 21/60 pass (was 243 lines, 20/59). The encoding fix changed
some test results. COMPUTED values now show correct digit-only format. Further
debugging needed: F1-1 shows DE-LETE instead of PASS (comparison may still have
a subtle issue with the new encoding), F1-2 shows 72 vs 73 (rounding with ROUNDED
keyword).

The fix is directionally correct — DISPLAY numeric fields now store pure digits
per ISO spec. Remaining issues are likely in how the initial VALUE clause writes
data and how the comparison decodes it.

---

---

