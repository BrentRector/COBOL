## Entry 115 — 2026-03-20: NC222A 100%, OCCURS Exclusion, De-editing Sign Loss, Pattern Sweep

### NC222A: MOVE CORRESPONDING test (8/8, 100%)

Started at 4/8. Two distinct bugs.

**Bug 1: OCCURS items included in CORRESPONDING matching**

`MOVE CORRESPONDING TABLE1 TO TABLE2` was matching `RECORD2 OCCURS 2` — copying table
elements that should be excluded. Per ISO §14.9.26, items with an OCCURS clause are not
eligible for CORRESPONDING. Added `child.OccursCount > 1` guard to
`CorrespondingMatcher.EnumerateEligibleLeaves`. Fixed MOV-TEST-F2-1 and F2-2 (4/8 → 6/8).

**Bug 2: CR/DB sign loss in de-editing**

`MOVE MOVE-TEST-3-A TO MOVE-TEST-3-B` where 3-A is `PIC $(4)9.99CR` and 3-B is `PIC S9(4)V99`.
`MoveNumericEditedToNumeric` stripped `CR`/`DB` suffixes with `.Replace("CR", "")` but never
set the negative flag. Computed `+123.45`, expected `-123.45`.

Fix: detect `CR`/`DB` before stripping, set `negative = true`.

**Pattern sweep** (unprompted — following the "every bug is a pattern" rule):
Found the identical bug in `MoveAlphanumericToNumeric` at line 706 — same `.Replace("CR", "")`
without sign detection. Fixed both methods simultaneously. Also added stripping for `B` (blank
insertion), `/` (slash insertion), and space characters that appear in edited fields like
`PIC --9B.99B99/99`.

**Note from user**: "Claude followed, without specific additional prompting, the every bug is
a pattern rule and discovered additional instances of the bug. Good work Claude!" This is the
collaboration pattern working as intended — the rule is now internalized.

### New 100% tests from batch scan

Quick scan of remaining NC tests found 5 more already passing:
- NC170A (96/96), NC202A (77/77), NC207A (85/85), NC221A (17/17), NC224A (14/14)
- NC111A (7/7), NC112A (32/32), NC132A (25/25) also confirmed at 100%

Total NIST kernel tests at 100%: 33 programs.

---

