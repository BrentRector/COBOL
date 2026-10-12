## Entry 264 — LINAGE subsystem pt.2: data-name LINAGE phrases → SQ208M/SQ210M

Completes the LINAGE subsystem with the data-name phrase forms (ISO §13.18.34: `LINAGE data-name-1 …
FOOTING data-name-2 … TOP data-name-3 … BOTTOM data-name-4`). Per GR6b the page parameters are the
*runtime* values of those data items, read **at completion of OPEN OUTPUT** (not at compile time).

- **Semantic**: `VisitLinageClause` now also captures the data-name of each phrase that is a data reference
  rather than an integer literal (`FileSymbol.LinageBodyName`/`LinageFootingName`/`LinageTopName`/
  `LinageBottomName`; `HasLinageDataNames`).
- **Lowering**: on `OPEN OUTPUT` of a file with any data-name LINAGE phrase, `LowerOpen` emits a new
  `IrInitLinage` (after the open call) carrying, per phrase, the resolved data-name location or the
  captured integer-literal constant. `EmitInitLinage` decodes each data-name location to an int
  (DecodeNumeric → ToInt32) or pushes the literal, then calls `FileRuntime.InitLinage`, which applies the
  four page parameters and resets the LINAGE-COUNTER to one (GR7d). (Integer-only LINAGE keeps using the
  registration-time `SetFileLinage` + the OPEN-time counter reset in `SequentialFileHandler.Open`.)

With the page params now populated at OPEN OUTPUT, the counter advances correctly, so the
`PERFORM … UNTIL LINAGE-COUNTER EQUAL n` loops terminate. **SQ208M** (all data-name phrases) and **SQ210M**
(mixed: data-name body, integer TOP) run to completion at 0 FAIL* — both are CCVS visual-INSPECTION tests
(0 auto-checks), so they validate that LINAGE writing completes and is deterministic; the counter mechanics
themselves are auto-verified by SQ201M (Entry 263). Both baselined.

Guard ALL GREEN: 1000 unit / 347 integration / **223 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 50 SQ + 19 RL +
2 IX), 0 regressions. The LINAGE subsystem (LINAGE-COUNTER register, integer + data-name page parameters,
counter advance/reset/overflow, footing + overflow end-of-page, AT/NOT-AT END-OF-PAGE phrases) is complete;
all four SQ20xM LINAGE tests are baselined. Remaining SQ COMPILE_FAILs are unrelated I-O-CONTROL clauses
(SQ206A `SAME AREA` without RECORD, SQ303M obsolete `MULTIPLE FILE TAPE`) + the SQ401M flagging module.

