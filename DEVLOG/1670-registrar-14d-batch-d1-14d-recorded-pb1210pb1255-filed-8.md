## Entry 1670 — 2026-09-24 09:18 PDT — Registrar 14d: batch d1-14d recorded, PB1210–PB1255 filed (8 folded), GAP 1900 → 1875

**Registrar 14d took adjudication batch d1-14d — 23 data-division subjects (ANY LENGTH through LOCAL-STORAGE,
including the report writer's CODE, COLUMN, CONTROL, GROUP INDICATE and LINE clauses) — and recorded 147 verdicts:
58 DIVERGES, 53 CONFORMS, 25 PARTIAL, 11 NOT-IMPLEMENTED. GAP 1900 → 1875.** Of the 53 CONFORMS, 27 closed their row
with a spec-derived witness; 26 are CONFORMS-but-untested and go to the golden lane's next input set.

**Main had moved under the adjudicators.** They worked on the pinned tree `adj-d1` at `68bbdb00f`. Trains 58 and 59
landed since, so every finding was re-run on this worktree's own build of `071ff04ea` before filing. Seven rows were
already fixed on main and were DROPPED from the batch instead of being recorded over newer closures:
- CONTROL FINAL position (`FMT-13.18.16.2`) is now COBOLNET2421, from PB483.
- The EXTERNAL placement screen (`SR-13.18.22.3-1`) is now COBOLNET2404, from PB518.
- The JUSTIFIED group and category screens (`SR-13.18.32.3-1`/`-3`) are now COBOLNET2403/2405, from PB512/PB507.
- A GLOBAL level-1 file-section record under a non-GLOBAL FD now resolves in the containee. `GR-13.18.27.4-1` and
  `SR-13.18.27.3-1` are fixed, and the SR3 under-reject the refuter feared (`SR-13.18.27.3-3`) is refused
  COBOLNET1757.

The 77/05 arms of the open PB1045 are also refused now (COBOLNET2404). The note records the re-probe; its SR4 CONSTANT
arm stays open.

**Clustering: 67 surviving findings became 45 new notes, and four open notes were extended.** One note per mechanism.
Where one root cause spanned subjects, the cluster crossed them:
- PB1212 covers the BASED SR1 and EXTERNAL SR4/SR5 screens, which read only what is written on the entry and never a
  TYPE-derived or FD-derived property.
- PB1213: no single variable-length predicate exists (BASED SR2 and CONSTANT SR12).
- PB1214: the cached-singleton LOCAL-STORAGE re-init loop misses cell-backed roots. This is both the BASED GR2 finding
  and the LOCAL-STORAGE GR1/GR2 finding.
- PB1221: the report-clause keyword prefixes are cross products (COLUMN SR4/SR5 and LINE Format 1).
- PB1222 covers the report group layout screens: COLUMN SR7/SR8a/SR10 and LINE SR6.
- PB1223: four EC-REPORT-* names have no raise site.
- PB1226: the REPORT SECTION entry grammar rejects constant entries and FILLER, and admits zero entries.
- PB1236: externalized-name identity is decided by two comparisons (Ordinal vs OrdinalIgnoreCase), and GR6
  case-folds VALUE content.
- PB1238: the FD format is never determined, and the FILE CONTROL clause alternatives in the FD are silently dropped.
- PB1244: the GROUP INDICATE run-time model is wrong in three ways.
- PB1247: the run-time line placement has the LINE PLUS 0 forced advance and no unpaged RH/RF arm. It is clustered with
  PB1003.

The remaining notes each own one mechanism:

| Note | Mechanism |
|---|---|
| PB1210 | ANY LENGTH count-1 picture spellings |
| PB1211 | ANY LENGTH RETURNING staged |
| PB1215 | method LOCAL-STORAGE ignores OPTIONS INITIALIZE |
| PB1216 | EC-BOUND-PTR not raised for ended LS/cancelled WS storage |
| PB1217 | BLOCK CONTAINS SR1 ordering |
| PB1218 | report CODE clause staged loud |
| PB1219 | CODE-SET SR3 screen applied to the implied record |
| PB1220 | COLUMN LEFT/CENTER/RIGHT has no grammar |
| PB1224 | COLUMN SR3 reads build state instead of ancestry |
| PB1225 | constant entry makes AS required |
| PB1227 | CONSTANT BYTE-LENGTH OF staged on a stale premise |
| PB1228 | CONSTANT FROM compilation-variable staged |
| PB1229 | figurative ZERO as an arithmetic operand |
| PB1230 | constant keeps a normalized value, not the written literal (wrong answer under DECIMAL-POINT IS COMMA) |
| PB1231 | constants resolved in declaration order |
| PB1232 | SR3 constant-name subscript |
| PB1233 | CONSTANT RECORD default content |
| PB1234 | float CONTROL operand crashes at activation |
| PB1235 | TERMINATE restores control values before the RF |
| PB1237 | FD with no SELECT |
| PB1239 | report-file references beyond OPEN |
| PB1240 | DISPLAY LINAGE-COUNTER crashes (R16 shape) |
| PB1241 | file-section VALUE sets a dynamic-length item's initial length |
| PB1242 | GLOBAL × SAME RECORD AREA |
| PB1243 | a containee's own subordinate vs an injected GLOBAL is reported ambiguous |
| PB1245 | GROUP INDICATE SR1 screen, and every §13.18.29 citation that should be §13.18.28 |
| PB1246 | level-number first-entry and digit count |
| PB1248 | TYPE CH … OR PAGE has no grammar |
| PB1249 | LINKAGE SR4 referenceability |
| PB1250 | function formal as a receiving operand |
| PB1251 | LINKAGE/LOCAL-STORAGE in FACTORY/OBJECT |
| PB1252 | the non-ISO linkageProcedureParameter is silently dropped |
| PB1253 | A.1 items 116/159/160 undetermined |
| PB1254 | user-function arguments never checked against formals (wrong answer) |

Four open notes were extended:
- PB1059 now also claims `GR-13.18.14.4-5`, the page width.
- PB694 now claims `SR-13.4.5.3-3`.
- PB1051 now claims `GR-13.7.4-4`.
- PB1045 was re-probed.

**Eight of the 45 drafts were FOLDED at landing into the notes registrars 14b and 14c filed concurrently for
the same mechanism** (the finisher, after 14b and 14c reached main): PB1218 → PB1129 (the RD CODE clause's COBOLNET0899
stage), PB1240 → PB1153 (the special-register OperandText crash), PB1214 → PB1132 (the cached-singleton LOCAL-STORAGE
re-init loop), PB1254 → PB1115 (user-function arguments never reach the §14.8.2.3.2 comparator), PB1235 → PB1187
(TERMINATE's GR3 step order), PB1211 → PB1167 (the 'ANY-LENGTH-RETURNING wave' stage), PB1223 → PB1188 (the four
EC-REPORT-* conditions with no raise site) and PB1239 → PB1171 (FileModel.IsReportFile read only by OPEN). Each folded
note is `status: retired` with `closes_rows: []` and a `closes_rows_reason` naming its successor, which now claims its
rows and carries its evidence verbatim. That leaves 37 open notes from this batch. The table and bullets above list the
drafts by their filed ids. PB1193 (14c) was also re-probed on `8bd4c7c9f`. Its premise, that a record-level GLOBAL
under a non-GLOBAL FD is dropped, no longer reproduces (`p6.cob` prints `C R1=OUTER` / `OUT R1=INNER`). The re-probe
is appended to the note for its implementer's I3 check. PB1199 (14c) duplicates PB1153 and is left to its owner.

**One owner question: PB1255** (`kind: decision`, `status: owner`). Every arm of CODE-SET §13.18.13.4 GR4 is
conditioned on a declined module: A.4.8 SELECT WHEN, or A.4.13 WRITE FILE. Both derived selectors deliberately exclude
`GR-13.18.13.4-4`, with reasons that do not survive reading. So does the row take a joint declined-module disposition,
or stay live? That row is excluded from the batch.

**Refuters overturned the adjudicator nine times among the recorded rows, every time in the stricter direction**
(CONFORMS → PARTIAL five times, CONFORMS → DIVERGES four times). One more refuter note kept CONFORMS. Examples:
- `GR-13.18.5.4-4`: a pointer into ended storage (PB1216).
- `GR-13.18.27.4-2`: a subordinate-name ambiguity (PB1243).
- `SR-13.7.3-3`: the function-activation arm (PB1254).
- `SR-13.10.3-1`: the normalized substitution (PB1230).
- `GR-13.18.35.4-1`: LINE-COUNTER drift (PB1247).
- `GR-13.18.14.4-9`: the unaligned counter (PB1220).
- `FMT-13.6.2`: BYTE-LENGTH in LOCAL-STORAGE (PB1227).

**Record shape.** The adjudicators wrote prose into `editions` (128 records) and `code-location` (47 records). The
registrar normalized both to the schema shape and moved the prose verbatim into `notes`. The first gate caught 19
`Class.Member` symbols that the code-location resolver cannot find by word search, such as
`ReportWriter.cs#CobolReport.PresentLine`. Those were re-anchored on the member name, and the batch was re-recorded
onto a clean inventory.

**Dossier gaps** went to PB372. It is the same citation-keyed blindness: MaterializeImpliedRecord, the LOCAL-STORAGE
re-init loop, ConformanceForest, FillFor and TerminateCore implement rules without citing them. Two gaps are tooling
defects:
- The optional-words audit missed constantEntryBody's required `AS`.
- COBOLNET_REPORT_WRITER_DESIGN §5 records under-rejections as prose.

Gate: `dotnet build CobolSharp.sln -c Debug` is green. Gate on the rebased tree (origin/main `8bd4c7c9f`): `dotnet build CobolSharp.sln -c Debug` green; the WHOLE Unit assembly 29188/29190, the two reds being ExternalCorpusPopulationDriftTests naming their cause ("EXTERNAL POPULATION ABSENT - no corpus at tests/external/gnucobol/tests/testsuite.src"); `work.py check` ✓ 1260 well-formed. `work.py check` is well-formed.

