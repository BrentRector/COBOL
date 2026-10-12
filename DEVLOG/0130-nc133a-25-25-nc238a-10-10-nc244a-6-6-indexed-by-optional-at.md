## Entry 130 — 2026-03-20: NC133A 25/25, NC238A 10/10, NC244A 6/6 — INDEXED BY Optional, AT-less END

Two grammar fixes:

1. **INDEXED BY? (optional BY)**: `INDEXED IDX-1` (without BY) is used by NIST and accepted
   by all major COBOL compilers. Changed `INDEXED BY dataReferenceList` to
   `INDEXED BY? dataReferenceList`. Unblocked NC133A, NC238A, NC244A (all 100%).

2. **AT-less END in SEARCH**: `SEARCH ALL ... END statement` (without AT) is an IBM/NIST
   dialect extension. Added `| END statementBlock` alternative to `searchAtEndClause`.
   NC237A now compiles but hangs at runtime (PERFORM VARYING with negative step issue).

---

