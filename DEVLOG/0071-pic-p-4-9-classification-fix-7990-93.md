## Entry 071 — 2026-03-15: PIC P(4)9 Classification Fix — 79→90/93

### The Bug (Two-Part)

**Part 1: PicUsageResolver misclassification.**
`PIC P(4)9` was classified as integerDigits=1, fractionDigits=0. But leading P shifts the
decimal left *before* the stored digit — so the `9` is actually a fractional digit. NIST
declares `PIC P(4)9 VALUE .00001`, meaning stored `1` must decode as 10⁻⁵, not 10⁻⁴.

**Fix:** Post-adjustment in PicUsageResolver: when leadingPScaling > 0 with no V and no
existing fractionDigits, reclassify integerDigits as fractionDigits. Now P(4)9 gives
fractionDigits=1, integerDigits=0, totalScale = 1+4 = 5. Stored 1 → 1/10⁵ = .00001. ✅

**Part 2: ApplyScalingAndRounding used FractionDigits alone.**
Even after Part 1, `MOVE .00001 TO PIC P(4)9` still stored 0 because
`ApplyScalingAndRounding` truncated to `FractionDigits` decimal places (1), losing
precision. The effective precision for P-scaled fields is `FractionDigits + LeadingScaleDigits`.

**Fix:** Changed `ApplyScalingAndRounding` to use `FractionDigits + LeadingScaleDigits`.

### AI Process Failure — Incomplete Fix Propagation

After finding and fixing `ApplyScalingAndRounding`, the user asked: "Are there other places
that need the same fix?" There were. Four more locations used `FractionDigits` alone without
`LeadingScaleDigits`:

1. `FormatNumericEdited` — numeric edited formatting
2. `FormatNumericForDisplay` call in `MoveNumericToAlphanumeric`
3. `WouldOverflow` COMP branch — overflow detection
4. `WouldOverflow` COMP-3 branch — overflow detection

**The lesson:** When fixing a pattern bug (using X where you should use X+Y), immediately
grep for ALL occurrences of the pattern and fix them in one pass. Don't fix one spot, test,
and move on. The user had to prompt this audit — it should have been automatic.

### Result

**NC101A: 90/93 pass** (was 79/90). +11 tests from P scaling classification fix.
Only remaining failures:
- F1-6 (1 test): COMP S9(6)V9(6) with REDEFINES — COMPUTED empty
- Footer "NO TEST(S) FAILED" display bug (2 lines)

---

