## Entry 191 — 2026-05-28: Latent-issue test cases — INSPECT REPLACING signed de-sign fixed; "qualified-cond parse" was a false alarm

Added custom integration test cases for the two latent issues logged in Entries 185/189,
reproduced both, then fixed/resolved them.

**Issue A — INSPECT REPLACING on a signed numeric (real bug, now fixed).** Entry 189 left
GR 4d de-signing implemented for TALLYING only. New test
`StringTests.Inspect_Replacing_OnSignedNumeric_DeSignsAndRetainsSign`:
`PIC S9(5) -12345` (stored overpunched as `1234N`), `INSPECT N REPLACING ALL "5" BY "8"`.
Before: the cycle saw the raw `1234N`, `"5"` never matched → result `-12345` (test red).
Fix: `ReplacingPass` now takes the target PIC; for a signed DISPLAY numeric it runs the
replace cycle over the de-signed digits (`FormatNumericForDisplay`) and re-encodes the
result with the original sign retained (ISO 14.9.22 GR 4d). The cycle body was extracted to
a shared `RunReplaceCycle` (no duplication). Non-numeric replacements leave the field
unchanged (undefined in the spec) rather than corrupt it. Now `-12348`. The decimal
round-trip handles scale (V) and any sign storage uniformly.

**Issue B — qualified condition-name in a combined condition (NOT a bug — false alarm).**
The Entry 185 note claimed `IF B OF X AND NOT B OF Y DISPLAY ... ELSE DISPLAY ...`
mis-parsed. That was wrong: my prior-session repro was **fixed-form** with a 97-character
line, and column 72 correctly truncated it (`...DISPLAY "ONELINE=PASS" ELS` | the rest in the
ignored identification area), which is exactly per COBOL fixed-form rules — not a compiler
fault. Verified the compiler is correct: free-form single-line and multi-line both work, and
a short fixed-form line (within col 72) works. Kept the new regression test
`ConditionTests.Combined_QualifiedConditionNames_WithInlineStatement` (passes) to lock the
correct behavior in. Lesson: keep generated fixed-form repros within columns 8–72.

Both new tests pass; full guard ALL GREEN (NC124A/NC216A/NC243A/NC244A still MATCH).

---

