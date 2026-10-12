## Entry 219 — 2026-05-29: IC suite — READ … RECORD, FD GLOBAL/EXTERNAL, REDEFINES-in-LINKAGE (COMPILE_FAIL 10→5)

Three FILE-CONTROL/data-division gaps in the IC suite:
- **`READ file-name RECORD`** — the optional `RECORD` noise word after the file name (ISO §14.9.21)
  was not accepted (IC112A/114A/115A: `READ SQ-FS3 RECORD AT END …`). Added `RECORD?` to
  `readStatement`.
- **`FD … GLOBAL` / `… EXTERNAL`** — IC233A/234A declare `FD TEST-FILE GLOBAL` for nested-program
  visibility. Added `fileGlobalExternalClause: IS? (GLOBAL | EXTERNAL)` to the FD clauses.
- **REDEFINES in the LINKAGE SECTION** was wrongly rejected (CBL3111) for 01-level items. ISO
  §13.18.44 imposes no section/level restriction on REDEFINES, and NIST IC237A relies on it.
  Removed the bogus check in `SymbolValidator` and corrected the unit test to assert it is allowed.

IC COMPILE_FAIL 10→5 (remaining: IC228A, IC233A/234A's deeper nested-GLOBAL resolution, IC235A
duplicate-name across nested programs, IC401M). CLEAN 9→10. Guard ALL GREEN (1000 / 336 / 149).

