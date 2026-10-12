## Entry 182 — 2026-03-31: Figurative Collating + RENAMES Stack Fix — 6 More FAIL* (28 remaining)

**Figurative collating sequence** (NC215A 3→0, NC219A 1→0): ConditionLowerer's
`EmitLocationVsFigurative` now checks for active ProgramCollatingSequence and uses
`IrStringCompareLiteralWithSequence`. LOW-VALUE/HIGH-VALUE figurative constants
remapped to min/max weight characters in the custom sequence. NC214M also improved
(bonus). +82 lines in ConditionLowerer.

**RENAMES _dataStack.Clear()** (NC209A 1→0, NC252A 1→0): Level-66 RENAMES processing
called `_dataStack.Clear()` after the first RENAMES item, destroying the stack for
subsequent level-66 items under the same record. Second RENAMES items (like HARRY in
A-GLOB) never got added to parent Children. Removed the Clear() — stack is properly
cleared when next 01-level is encountered.

Guard: 28 FAIL* (was 34). Session total: 78→28 = 50 FAIL* eliminated.

---

