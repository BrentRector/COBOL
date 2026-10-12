## Entry 330 — Class-B obsolete-flagging: CBL3607 + OPEN REVERSED + MULTIPLE FILE TAPE → SQ303M 2/2 under cobol85

Implemented two of the three pinned Class-B obsolete-element flags (Entry 329). Added a **generic** descriptor
`CBL3607` ("{0} is an obsolete element; removed from COBOL-2002 and later standards") and emitted it at two
sites, gated `< cobol2002` (consistent with the existing CBL3602/CBL3606 obsolete warnings):

- **OPEN … REVERSED** — `DialectStrictnessChecks.CheckObsoleteOpenReversed(ctx, OpenFileSpecContext)`, called
  from `FileIoBinder.BindOpen` (the REVERSED phrase was already detected-and-ignored there).
- **MULTIPLE FILE TAPE** — `SemanticBuilder.VisitIoControlClause` via `ctx.multipleFileClause()`.

Verified under `--standard cobol85`: **SQ303M now emits both expected obsolete flags (2/2)**; NC303M holds at
3/4 (ALTER + 2× GO TO already warn; DATE-COMPILED remains). Guard **ALL GREEN** — warnings never touch the CCVS
report, and the 364 baselines compile under `--nist`/Default where these warnings are harmless. Remaining
Class-B: **DATE-COMPILED** (NC303M's 4th flag — the Stage-0 preprocessor commented-out path) + a flagging
harness asserting the per-module obsolete-flag counts.

