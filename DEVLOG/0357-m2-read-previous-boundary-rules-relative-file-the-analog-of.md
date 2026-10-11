## Entry 357 — M2: READ … PREVIOUS boundary rules (RELATIVE file) — the analog of Entry 355

Entry 355 fixed READ PREVIOUS for INDEXED files; this does the same for RELATIVE. `RelativeFileHandler.ReadPrevious`
had the identical two bugs: (1) post-OPEN INPUT, with `_currentRecord == 0` and no file position indicator, it fell
through the predecessor loop and returned the *highest-numbered* record instead of raising AT END; (2) there was no
inclusive post-START path, so the first reverse read after `START KEY = EQUAL n` returned the strict predecessor.

The relative handler is trickier than the indexed one because `Start` uses a **slot-1 NEXT-only hack**:
`_currentRecord = matchedSlot - 1`, so the next READ NEXT returns `matchedSlot`. That hack (a) makes
`_currentRecord == 0` ambiguous — post-OPEN vs START-matched-slot-1 — and (b) makes a strict-predecessor READ
PREVIOUS skip *both* the FPI slot and its neighbour.

Fix — minimal, leaving the load-bearing slot-1 NEXT hack untouched (so the RL NIST baselines that depend on
START→READ NEXT are unaffected): two new fields `_startFpiSlot` / `_startPositioned`. `Start` records the matched
slot and sets the flag; `Open`, `ReadNext`, and `ReadByKey` clear it (a fresh OPEN, a forward read, or a keyed read
all consume the START-inclusive state). `ReadPrevious` now: if `_startPositioned` → return the FPI slot's record
inclusively (ISO §14.9.30 GR21 d.2); else if `_currentRecord == 0` → AT END (GR21 d.1, §9.1.13); else the strict
predecessor (now that post-OPEN is handled up front, the loop guard simplifies to `k >= _currentRecord`). Two
integration tests mirror the indexed pair: `ReadPrevious_Relative_AfterOpen_RaisesAtEnd` → `ATEND 10`,
`ReadPrevious_Relative_AfterStartEqual_ReturnsTheEqualSlot` → `GOT 22`.

Guard ALL GREEN: **1047 unit / 435 integration / 364 NIST**, 0 regressions.

