## Entry 331 — WS-FLAG harness (FlaggingConformanceTests) + the complete `…M` flagging-axis classification

Built the flagging-conformance harness as integration tests: a `CompileNistDiagnostics(testName, dialect)`
helper (compile a NIST program under a given dialect **without** `--nist`, so dialect flagging fires; return the
diagnostics as strings carrying their `CBL####` codes) + `FlaggingConformanceTests` asserting NC303M
(CBL3602 ×1 + CBL3606 ×2) and SQ303M (CBL3607 ×2) under cobol85. Guard ALL GREEN (integration 347→349).

Then read the **complete** `FLAG_MANIFESTS`, which classifies the entire flagging axis into **three** classes —
and shows the gap is far smaller than the raw "~39 `…M`" count:

1. **Subset-flaggers → N/A at high subset (the MAJORITY, ~15 modules):** IF401M/402M/403M, IX301M/401M,
   RL301M/401M, SQ401M, SM301M/401M, ST301M, RW301M, IC401M, IC116M/117M/118M. Every flag is "feature X is above
   the minimum/intermediate subset" (intrinsics, INDEXED org, NOT INVALID KEY, SD/MERGE/RELEASE, REPORT SECTION,
   IS INITIAL/GLOBAL/CALL BY CONTENT, …). CobolSharp implements the **full high subset**, so these are native and
   emitting **zero** flags is correct conformance. **Documented N/A (owner decision A, Entry 329).**
2. **Class-B obsolete (core) → IMPLEMENT (small):** NC303M (DATE-COMPILED pending), SQ303M (✅), RW302M
   (MULTIPLE FILE TAPE ✅ via CBL3607 + LABEL RECORDS / VALUE OF pending).
3. **CM/DB/SG/OBNC removed-module flaggers → WS-DIALECT (parse + flag, no runtime):** the CM modules flag the
   whole communication facility (COMMUNICATION SECTION / CD / SEND / RECEIVE / ENABLE / DISABLE / ACCEPT MESSAGE
   COUNT) as removed-after-'85; DB flags `USE FOR DEBUGGING`/`DEBUG-ITEM`; SG flags segment-numbers/`SEGMENT-LIMIT`.
   These are **COMPILE_FAIL today** (grammar doesn't parse them) — WS-DIALECT must add the grammar to parse +
   a removed-feature diagnostic. The one substantial remaining flagging piece.

Net: the flagging axis to 100% = finish ~3 small Class-B obsolete flags + the WS-DIALECT grammar; ~15 modules are
N/A by the high-subset profile.

