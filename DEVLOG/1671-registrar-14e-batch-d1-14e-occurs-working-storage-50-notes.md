## Entry 1671 — 2026-09-24 09:40 PDT — Registrar 14e: batch d1-14e (OCCURS → WORKING-STORAGE), 50 notes PB1260–PB1309, GAP 1875 → 1839

**What.** Registrar 14e registered adjudication batch d1-14e: 22 subjects (OCCURS p1/p2, PAGE, physical subdivisions of a
report, PRESENT WHEN, PROPERTY, RECORD, REDEFINES, RENAMES, REPORT, RD entry, report group entry, REPORT SECTION, SAME
AS, SD entry, SOURCE, SUM, TYPE p1/p2, TYPEDEF, VARYING, WORKING-STORAGE). The adjudicators and refuters ran on the
pinned tree `E:\CobolSharp-pins\adj-d1` @68bbdb00f; every finding was checked against main before filing.

**Shape.** 220 rules recorded: DIVERGES 86 · CONFORMS 69 (37 closed the GAP, 32 still need a spec-derived test) ·
PARTIAL 49 · NOT-IMPLEMENTED 16. No NEEDS-OWNER-DECISION rows. The refuters overturned 18 CONFORMS: 13 to PARTIAL,
4 to DIVERGES, and 1 upheld as free text. Three of the overturns found an ORDER dependence the adjudicators' probes
never flipped: SAME AS SR3's cycle check depends on declaration order, PROPERTY SR4's superclass-collision check
depends on source order, and STRONG-ness through a TYPE chain is answered two ways. GAP 1875 → 1839.

**One re-verdict.** SR-13.18.44.3-9 (no VALUE in a REDEFINES entry) was DIVERGES on the pin. PB550 fixed it in train
58: on main 071ff04ea both shapes draw COBOLNET2406. It is recorded CONFORMS against `negative/pb550-value-in-redefining-entry`.
One finding needed no note: RD clause repetition, `PAGE … HEADING 1 HEADING 2`, now draws COBOLNET2423 through PB483.

**Mechanisms (one note per root cause, `cluster:` lists the fill units).** 81 findings plus 32 orphan defective
rows became 50 new notes and 5 extended ones:
- OCCURS: PB1260 (SR1/SR10 placement: level-01/77, 8 subscripts, and ODO under DYNAMIC, which aborts at run time),
  PB1261 (DEPENDING-object screens SR18/20/21), PB1262 (CONSTANT RECORD vs ODO/DYNAMIC), PB1263 (KEY constraints),
  PB1264 (Format 4 integers dropped by TryParse; CAPACITY namespace), PB1265 (grammar superset), PB1266 (false index-name
  and KEY-order messages), PB1267 (INITIALIZED seed), PB1268 (EC-BOUND-ODO/-SUBSCRIPT raise sites), PB1269 (RESUME loses
  the growth).
- Report writer: PB1270 (PAGE integer and page-region screens), PB1271 (a sum counter is never a table), PB1272
  (PRESENT WHEN snapshot timing, all-lines-absent), PB1285 (FD↔RD correspondence), PB1286 (RD CODE clause refused),
  PB1287 (FILLER group name and RD with no groups), PB1288 (BindReportEntry screens, with BWZ/JUSTIFIED never applied to
  report entries), PB1289 (PRESENT WHEN operand scan), PB1292 (the SOURCE identifier arm: subscripts and sum counters),
  PB1293 (the SOURCE implicit MOVE validity), PB1294 (SUM data-name-1 staged loud), PB1295 (SUM bind screens), PB1296
  (no sum SIZE ERROR), PB1297 (RESET without a CF; UPON DET DET), PB1298 (TYPE ON/FOR/OR PAGE grammar), PB1299 (group
  census SR13–15), PB1305 (VaryValue `long`, which crashes the C# build; EC-REPORT-VARYING), PB1306 (VARYING on group
  entries).
- OO: PB1273 (PROPERTY screens and an inert clause outside OO), PB1274 (accessors synthesized after MarkRoster and in
  source order), PB1275 (object property as a SET receiver).
- Files: PB1276 (RECORD CONTAINS sizes nothing; VaryMax two-arm), PB1277 (record sizes in characters, not bytes),
  PB1278 (A.1 items 146/148 undocumented), PB1290 (SD/FD with no SELECT), PB1291 (SD record in ADVANCING).
- REDEFINES/RENAMES: PB1279 (nested-class backing under OCCURS), PB1280 (ResolveRedefines entry screens
  SR1/2/3/8/13/15), PB1281 (target name by GetText), PB1282 (REDEFINES inside a STRONG template), PB1283/PB1284 (RENAMES
  operand, extent and range-content screens).
- TYPE/TYPEDEF/SAME AS: PB1300 (composition copies the wrong clause set: BASED, the implied PICTURE, the USAGE group
  kind), PB1301 (STRONG through a TYPE chain), PB1302 (cycle detection per reference, order-dependent), PB1303 (GLOBAL
  types not inherited), PB1304 (RENAMES in a TYPEDEF staged).
- WORKING-STORAGE: PB1307 (recursive program with contained programs refused), PB1308 (method WS gated only at 2023),
  PB1309 (the first WS entry's level is unscreened).
- Extended: PB1059 (LAST CH, the PAGE abbreviations, SR2/SR3, the GR5 width), PB1050 (FMT-13.18.46.2), PB545
  (SR-13.18.57.3-8), PB1046 (SR-13.15.3-7), and PB372 (this batch's dossier gaps).

**Landing (finisher).** The branch was rebased across 14b, 14c and 14d; each time the inventory was taken from
origin/main and the batch re-applied with record_verdicts (L6): 220 records, none dropped, GAP 1875 → 1839. PB372 and
PB1059 conflicted with 14b/14c/14d's extensions of the same notes; both sides were kept (PB1059's `inventory_rows` is
the union). Two drafts duplicated a mechanism 14b had already filed and were folded, as 14d did: PB1286 → PB1129 (the
RD CODE clause's COBOLNET0899 stage, FMT-13.14.2) and PB1307 → PB1133 (COBOLNET0899 RecursiveContainedWs,
GR-13.5.4-1). Each folded note is `status: retired` with `closes_rows: []` and a `closes_rows_reason`; the 14e notes
whose `cluster:` named them now name the successor.

**Normalization.** Nearly every record's `editions` was prose, and 53 code-locations carried commentary or qualified
symbols. Both were normalized to the schema. The prose was MOVED into `notes`, never dropped, and each qualified
symbol was reduced to the member name the drift test greps for.

**Leads with no id left (the allocation was used up):** `specs/ISO_COBOL.md` §13.18.57.2 lost both TYPE general-format
diagrams (a transcription defect). §13.18.35.3 SR6 a/b/d/e and §13.18.14.3 SR7/SR8 overlap rules are unenforced, and
EC-REPORT-LINE-OVERLAP/COLUMN-OVERLAP/PAGE-WIDTH have no raise site. `docs/COBOLNET_FILES_DESIGN.md` still calls RECORD
VARYING a 2002 introduction.

**Gate.** Built `CobolSharp.sln` Debug on the rebased tree (origin/main 72dcff749, Registrar 14d), then ran the WHOLE Unit
assembly: `Failed: 2, Passed: 29188` — the two reds are the environmental ExternalCorpusPopulationDriftTests (no
GnuCOBOL corpus at `tests/external/gnucobol/tests/testsuite.src` in the worktree). `work.py check`: 1310 work items,
all well-formed.

