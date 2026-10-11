## Entry 180 — 2026-03-31: UNSTRING MOVE Semantics Fix — 6 More FAIL* Eliminated (47 remaining)

`UnstringExtract` performed raw byte copy into destination fields, bypassing MOVE
semantics. Added `PicDescriptor destPic` parameter and `CopyExtractedToDestination`
helper that dispatches to the correct `PicRuntime.Move*` method based on destination
category. Handles JUSTIFIED RIGHT (right-alignment) and numeric conversion (rightmost
digit extraction for PIC 9/S9) automatically through the existing MOVE infrastructure.

NC218A: 9→2 FAIL* (7 fixed: GF-2.02 JUST, GF-4.01 JUST, GF-5.01 PIC 9, GF-6.01
PIC S9, GF-9.01 trailing sep, GF-10.01 leading sep, GF-15.01 overflow). 2 remaining
are OR delimiter support (GF-21.03/04). Note: PERFORM VARYING AFTER reset attempted
but reverted — the naive reset broke 3 previously-passing tests; needs deeper analysis.

Guard: 47 FAIL* locked (was 53).

---

