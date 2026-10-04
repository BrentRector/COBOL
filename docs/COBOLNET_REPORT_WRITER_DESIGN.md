# WiseOwl COBOL — Report Writer (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL → idiomatic
> typed-native C# via Roslyn; no byte substrate). This doc CLOSES the SSOT's "designed only to the seam"
> scope flag for Report Writer (`docs/COBOLNET_DESIGN.md` §14 verb table / §15.5): the subsystem is now
> implemented. The locked invariants and cross-cutting consistency live in the SSOT; spec authority is
> `specs/ISO_COBOL.md` (ISO/IEC 1989:2023) — every behavior below carries its § citation.

## Summary

The Report Writer Control System (RWCS): the REPORT SECTION (ISO §13.8/§13.14/§13.15 + the §13.18 report
clauses) and the INITIATE / GENERATE / TERMINATE / SUPPRESS verbs (§14.9.21/§14.9.16/§14.9.46/§14.9.45), with
the per-report LINE-COUNTER / PAGE-COUNTER registers (§8.4.3.15) and USE BEFORE REPORTING declaratives
(§14.9.49 Format 2).
Validated by NIST RW101A–RW104A (byte-match) **plus a spec-pinned conformance net for the report-file CONTENT
the NIST goldens never compare** (`ReportWriterConformanceTests`) — load-bearing because the legacy oracle's
report-file content is demonstrably WRONG in two places (see §7); the spec, not the oracle, governs.

**Edition status:** RW is an optional module in COBOL-85 (the NIST RW suite runs `--std 85`) and an optional
language element in 2023 (A.4.11). Its module-level 2002 status is NOT derivable from the 2023 spec text —
flagged as a `VERSION_CHANGE_REFERENCE.md` follow-up row. The '85 clause surface is not edition-gated; the
2002 RW additions ARE (the VersionConformancePass parse arm, 0900 below 2002): PRESENT WHEN (§13.18.41 F1),
VARYING (§13.18.64), the multiple/relative COLUMN forms + COL/COLS/COLUMNS/NUMBERS/ARE spellings (§13.18.14 F1
— the '85 form was exactly `COLUMN NUMBER IS integer-1`), and the multiple-LINE form + LINES/NUMBERS/ARE
spellings (§13.18.35 F1). Matrix rows `report-present-when-2002` / `report-varying-2002` /
`report-multi-column-2002` (+ `report-multi-line-2002` pending).

## 1. Architecture (ONE mechanism — compose-at-presentation)

```
DataBinder.Reports.cs            ReportWriterBinder.cs                ReportWriterEmitter.cs
RD → ReportModel                 INITIATE/GENERATE/TERMINATE →        engine field __RPT_n + per-line
  geometry (§13.18.39 GR3        BoundInitiate/Generate/Terminate;    compose methods; construction in
  defaults), CONTROL list,       LINE-/PAGE-COUNTER →                 __Activate beside the file
  groups → lines → fields        BoundReportCounterRef                registration; verbs → engine calls
                ↘                                ↘                                  ↘
                         Cobol.Net.Runtime/IO/ReportWriter.cs — CobolReport
                         (the per-report RWCS engine: page geometry, counters,
                          page-fit/advance, control breaks, SUM, group hooks)
```

- **Every report line is ONE generated compose method** (`Func<string>` over the program instance's typed
  fields), invoked by the engine **at presentation time, after LINE-COUNTER is set to the line's number**.
  This realizes §13.18.53.4 GR1/GR3 (SOURCE is an implicit MOVE "executed before the associated report line
  is printed") and §13.18.35.4 GR6 (LINE-COUNTER set FIRST, then the line printed) **by construction** — a
  `SOURCE IS LINE-COUNTER` item on any group prints that line's own number (the RW103A page-heading check).
- The legacy split composition into TWO mechanisms (runtime-registered byte FieldPlans for auto groups vs
  code-composed byte buffers for details) — a singular-pattern violation and the proven source of its two
  §13.18.53 content bugs. The greenfield has no registration kinds, no byte buffers, no storage offsets.
- **Printable items are SYNTHETIC `DataItem`s** (PicInfo + JUSTIFIED/BLANK WHEN ZERO flags, never added to
  the storage forest — report items are not accessed as ordinary storage, §13.8.6.2.3). Numeric printable items
  are `StoreAsImage`, so every rendering path below yields the printable CHARACTER image directly; their
  `NumProfile` statics are emitted by the RW emitter (`ReportWriterEmitter`) — the field emitter only walks the
  storage forest.
- ⛔ **TWO CLAUSES FILL A PRINTABLE ITEM AND THEY ARE NOT THE SAME RULE** (kb/Work PB506). A **SOURCE** operand
  renders through the orchestrator's ONE MOVE conversion (`MoveEmitter.ConvertSource`), so a numeric SOURCE
  edits through the printable PICTURE exactly like `MOVE src TO item` (alignment, truncation, editing,
  JUSTIFIED, BLANK WHEN ZERO) — §13.18.53.4 GR1 verbatim. A **VALUE** operand is an INITIALIZATION and renders
  through the ONE §13.18.63 VALUE recipe the working-storage lane uses
  (`ValueInitializer.InitializerFrom`, reached via `DataEmitter.ValueImageOf`), because §13.18.63.4 GR21
  imports GR7 — "aligned … except that initialization is not affected by a JUSTIFIED clause and no editing
  takes place" — and GR8 (BLANK WHEN ZERO has no effect for an alphanumeric or national literal), and
  §13.18.63.3 SR34 imports SR11 ("Editing characters in a picture character-string for an alphanumeric-edited
  or national-edited data item do not cause editing of the initial value"). Routing a VALUE through the MOVE
  applied exactly those three excluded transforms: `PIC XXBXX VALUE "AB CD"` printed `AB  C`,
  `PIC X(5) JUSTIFIED VALUE "AB"` printed `   AB`, `PIC ZZZ9 BLANK WHEN ZERO VALUE "0000"` printed spaces —
  each while the IDENTICAL working-storage entry was right. `ReportOperandListDriftTests` keeps the MOVE out of
  the VALUE lane.
- ⛔ **BUT THE FORMAT-4 LITERAL IS STILL GOVERNED BY §13.18.63.3, AND ITS EDITION SCREEN IS THE DATA
  DIVISION'S** (kb/Work PB921). SR6 — "literals in formats 1, 2, and **4** of the VALUE clause may be numeric" —
  NAMES this format, and Annex E.3.3 item 43 dates that permission as a COBOL-2023 addition, so a numeric
  literal on a numeric-edited printable item is refused below 2023 exactly as its working-storage twin is. It
  was not: a report entry's VALUE operands are collected by `ExtractValueOperandList` and stored as
  `FieldValueSource`, passing through neither `ScreenValueLiteral` nor `ValidateValueCategory`, so
  `03 COLUMN 1 PIC ZZ9.99 VALUE 10.` compiled clean at `--std 85` and PRINTED ` 10.00` while
  `01 X PIC ZZ9.99 VALUE 10.` was refused there. The bind now asks the ONE screen,
  `DataBinder.ScreenNumericEditedNumericLiteral`, once per operand and once the printable item's picture is
  settled (a multi-operand format-4 clause — SR35 — gates each of its literals). `NumericEditedValueEditionGateDriftTests`
  carries the report spelling beside the three data-division ones, so the property is one property.
- **Physical output** goes through the report file's ordinary connector (`CobolFile.WriteAdvancing` — the
  print-control stream). The engine tracks `_physLine` (physical position) separately from LINE-COUNTER,
  because a NEXT GROUP clause moves LINE-COUNTER without printing (§8.4.3.15.4 GR4, §13.18.37.4).
  ⛔ **A LINE NUMBER IS A PAGE LINE NUMBER, AND LINE 1 IS WHERE THE STREAM ALREADY RESTS** (kb/Work PB484):
  §13.18.35.4 GR6 prints the line "on the page at that vertical location" and GR7 makes every unoccupied
  line above it blank, so the travel to line `target` is `target − 1` while the page is still empty
  (`_physLine == 0`, at INITIATE and after the §14.9.16.4 GR6b form feed) and `target − _physLine` once a
  line has been printed on it. Reading `_physLine == 0` as a line to advance OFF put every line of every
  report one line too low; the GR3 overlap clamp applies only once a line exists to overlap.

## 2. The engine (`CobolReport`) — spec-keyed behavior table

| Operation | Rules encoded (all cited in code) |
|---|---|
| `Initiate` | §14.9.21.4 GR1a–c (sums←0, LC←0, PC←1), GR2 (active re-INITIATE **raises EC-REPORT-ACTIVE**, no other effect), GR3 (the file is NOT opened here — it shall ALREADY be open OUTPUT/EXTEND, else **EC-REPORT-FILE-MODE** and no action is taken on the report; the detection half of §14.9.27.4 GR7), GR4 (→active). §14.9.49.4 GR10 outranks all three — see the RANGE row |
| `Generate(detail?)` | GR4 first-GENERATE sequence (RH once → PH → CHs major→minor → detail); GR5 subsequent (break: CFs minor→break with PRIOR control values per §13.18.16.4 GR4a, then CHs break→minor); GR2 summary (null detail); GR7 inactive **raises EC-REPORT-INACTIVE** and does nothing; SUM accumulation per §13.18.54.4 GR7c (after break processing) |
| page fit | §13.18.35.4 GR4b absolute (integer-1 > LC) / GR4c relative (trial = LC + Σ relative values ≤ the §13.18.57.4 GR8 lower limit: DE→LAST DETAIL, CH→LAST CH, CF→FOOTING); the chronologically FIRST body group since INITIATE is exempt (GR4); only body groups test (§13.18.57.3 SR15); a first PRESENT line carrying the NEXT PAGE phrase takes no test and the fit is unsuccessful (GR4a — see the LINE NEXT PAGE row) |
| page advance | §14.9.16.4 GR6 in order: PF → physical advance (form feed) → CODE evaluation (`EvaluateCode`, PB1129) → PC+1, or PC←1 after a NEXT GROUP NEXT PAGE WITH RESET (GR6d / §13.18.37.4 GR6) → LC←0 → PH. The feed itself (b–e) is `PageFeed`, shared with the report heading that stands on a page by itself |
| NEXT GROUP | §13.18.37 (kb/Work PB957) — the bound clause is the runtime's own `ReportNextGroup` record on `ReportGroup.NextGroup`, applied by the ONE method `ApplyNextGroup` after a group's last line (GR2). RH (GR3): absolute LC←integer-1, relative LC+=integer-2, NEXT PAGE → the RH is alone on page 1 and `PageFeed` runs with no PF (§13.18.57.4 GR6f 1) and PC←1 when WITH RESET (§14.9.16.4 GR4a). Body (GR4): absolute LC←integer-1 when LC is below it, else integer-1 goes to the SAVE LOCATION with LC←FOOTING, and the next non-dummy body group takes the forced advance then GR4a 1 (absolute first line: LC←saved, fit re-applied) or GR4a 3 (relative: first line at saved+1 unless the group would pass its lower limit — then a second advance and FIRST DETAIL); a TERMINATE next discards the save and restores LC ("no effect at all"); relative adds integer-2 below FOOTING else LC←FOOTING (an unpaged report has no FOOTING — the distance is added); NEXT PAGE LC←FOOTING and arms the GR6 reset. CF (GR1): only the footing AT the break level applies its clause — at TERMINATE the most major one (§14.9.46.4 GR3b). PF (GR5): absolute/relative move LC, which places a relative RF (§13.18.35.4 GR5b5). A dummy or SUPPRESSed group never reaches it (§8.4.3.15.4 GR5, §14.9.45.4 GR3). GR4a 2 (a next group opening with an absolute LINE … NEXT PAGE) IS GR4a 1: the advance it names is the one the save location already forces, never a second (docs/CONFORMANCE.md, the LINE NEXT PAGE block) |
| LINE NEXT PAGE | §13.18.35.2 Format 1 (kb/Work PB1001) — `integer-1 ON NEXT PAGE` and the bare `ON NEXT PAGE` operand. ⛔ **A FLAG, NOT A KIND**: `ReportLineModel.NextPage` / `ReportGroupLine.NextPage`, set by `DataBinder.Reports.RepeatedLine` on the group's FIRST report line only (SR7), on an Absolute line (integer-1) or a Relative one (the bare operand), so every phrase-less placement rule stays the one rule. The engine reads it on the group's first PRESENT line (GR4/GR5 — PRESENT WHEN decides which line is first): a body group declares the fit unsuccessful without a test (GR4a) — the chronologically first body group since INITIATE takes no fit test at all, so the phrase does not advance it — and then places by GR5a (integer-1) or GR5b3 (the bare form, the first body group on its page → FIRST DETAIL); a report footing is on a page by itself (GR5a) — `PresentHeadingFooting` takes the bare `PageFeed` (no PH before it, §13.18.57.4 GR6b; no PF after it, GR6f 2) and places at integer-1, or at the HEADING integer for the bare form (§13.18.57.4 GR7f — a DETERMINATION). Syntax rules on COBOLNET2199, screened once per WRITTEN clause over the flat entry array by `ScreenReportLineClauses` (with SR3/SR5 — kb/Work PB1002): SR3 (integer ≤ the page limit, or 9999 unpaged), SR5 (unpaged → relative only; the bare operand is not the relative form, as §13.18.37.3 SR3 reads NEXT GROUP NEXT PAGE), SR7 (only the first LINE clause), SR8 (body group or report footing only). SR10a keeps the phrase to a multiple LINE clause's first operand. Pinned by `2002/pb1001_line_next_page` and four negatives |
| line placement | §13.18.35.4 GR5a (absolute → integer-1), GR5b1 RH (HEADING+n−1), GR5b2 PH (RH-on-page aware), **GR5b3 body (FIRST body group on page → FIRST DETAIL, relative value IGNORED; else LC+n)**, GR5b4 PF (FOOTING+n), GR5b5 RF (PF-on-page aware), **GR5c (a report NOT divided into pages: every relative first line, RH and RF included, → LC+n)** — the whole GR5 rule is ONE method, `FirstLineTarget`, asked by all four presentation paths (kb/Work PB1247: the unpaged arm used to live only in the body-group copy), GR7 subsequent lines, GR6 LC-before-compose, GR8 final LC = last line printed. **GR3 overlap / overprint (kb/Work PB1247, PB1130)**: the engine HOLDS each composed line back until the next line proves it is not an overprint; a relative line with integer-2 zero on the held line MERGES its non-space characters into it (one physical line, LC still names it — GR1, §8.4.3.15.4 GR4); any other line on or above the device's line sets EC-REPORT-LINE-OVERLAP — raised (checking on) it abandons that line and LC is untouched (§14.9.16.4 GR8 / §14.9.46.4 GR5 resume at the next line), unchecked it prints on the next physical line and `_physLine` records where it really went. The held line belongs to the FILE CONNECTOR (`FileConnector.HeldLineDrain`, one holder per connector, claimed through `CobolFile.HoldLine` before every write, drained by the next line, a page feed, the end of TERMINATE and every CLOSE path) |
| `Terminate` | §14.9.46.4 GR1 (inactive → **EC-REPORT-INACTIVE**, the statement is unsuccessful), **GR2 (no GENERATE ⇒ NO groups print — only →inactive)**, GR3a–d as ONE bracket: controls→prior (a), CFs minor→major (b), §13.18.57.4 GR6f final-page PF ("immediately followed by" the RF) and the RF (c), and only THEN the restore (d) — so a PF/RF that SOURCEs a control item shows its prior value (kb/Work PB1187), GR6 (file NOT closed) |
| controls | §13.18.16.4 GR1 (operand order = hierarchy), GR2 (FINAL highest, never breaks mid-report), GR3 (first GENERATE saves priors; major→minor compare), GR4a (CF composes under restored prior values), GR5 (TERMINATE = most-major break). The SAVED prior = the item's CHARACTER IMAGE via generated get/set delegates (representation-faithful for every category; restore decodes via `CobolNum.StoreDisplay` for native numeric leaves); the break TEST is the program's own equality comparison (§12.3.6.4 GR11 c) names the CONTROL clause as an implicit user of the program collating sequence — kb/Work PB1131): the emitter passes `AddControl` the collated `CobolString.Compare` a relation condition over the item would render, and none when that IS code-unit equality (no sequence, or a numeric/boolean item) |
| SUM | §13.18.54.4 GR1 (ONE counter per ENTRY OCCURRENCE — a repeating entry's counters are a table, `ReportSumFamily`, kb/Work PB1271 — scale from the entry's PICTURE, and its IDENTITY is that occurrence's id, never GR5's data-name — two entries may legally share one name, kb/Work PB882), GR2 (reset at the end of the group it prints in — EVERY presentation path, the page footing's page total and the report footing included, and also a group with no present line or a SUPPRESSed one (`BeginGroup`) — or, with RESET ON, at the end of its LEVEL's control-footing processing, `ProcessControlFootings`, whether or not a control footing is DECLARED for the level: "If no such control footing is defined, it is assumed to be present and to consist of a 01-level entry alone" (kb/Work PB1297); a RESET ON entry's own presence is not asked there — GR10 speaks of "the current instance of the report group" the entry is IN, which SR8 keeps from being that footing), GR10's other half (an occurrence an OCCURS … DEPENDING count excludes is neither printed nor reset — the counter's presence slot ANDs its PRESENT WHEN chain with its occurrence's `ReportSumModel.RepetitionGuards`, kb/Work PB1271), GR4 (the counter is the printable entry's source item — `ReportSumCounterPlace`, the ONE place a counter is read and written), GR7c1/c2 (accumulate per GENERATE / per the term's OWN UPON filter — a detail named n times in the phrase adds n times, `SumTerm.Fires` is a COUNT, kb/Work PB1297), GR9 (multi-addend). **GR1's SIZE ERROR INDICATOR (kb/Work PB1130)**: the counter's capacity is the PICTURE's digit count (enforced by the store each term's addition ends in, through the counter's own `NumProfile` — `AddSum` carries no digit count since kb/Work PB1686), derived per category by `DataBinder.SumCounterDigits` (kb/Work PB1296) — numeric: the 9s; numeric-edited: every DIGIT POSITION (`PicInfo.DigitPositions` — Z, *, floating insertion, not only the 9s: ZZ9 is three digits); alphanumeric / national (edited or not): each character position that is not a B / 0 / / insertion character. **The carrier is `Int128` end to end** (kb/Work PB1509/PB1560/PB1666): `SumEntry.Value`, `SumValue`/`SetSumValue`, and every `SumTerm`, which is a `Func<Int128, Int128>` — ONE ADDITION: the counter's content in, its content after the term's ADD-consistent addition out (kb/Work PB1686); the read lands the value in the counter's own CLR carrier (`RuntimeApi.ReportSumRead` — `long` ≤ 18 digits, `Int128` beyond), and `PicInfo.SumCounterItem` keeps GR1's digit count up to 38 (every numeric PICTURE, ≤ 31 digit positions by §13.18.40.3 SR14; only an alphanumeric/national PICTURE of more than 38 positions is bounded by the carrier). **GR3's "consistent with the general rules of the ADD statement" is literal (kb/Work PB1686): a term's addition is emitted by the ADD statement's own machinery** — `NumericRenderer.Fold` over the term's addends (GR9's sum, the one initial evaluation), `Combine(counter content, "+", sum)` at the WIDER of the two scales in the unit's lane (the scaled `Int128`, or the SDIDI under a standard mode, §11.9.5.2 GR1/GR3), and `StoreExpr(…, raiseOnSizeError: true)` through the counter's profile with the clause's ROUNDED mode, so the sum is stored ONCE at the counter's scale. Aligning each addend to the counter's scale first (the former form) cut an addend finer than the counter one addend at a time: a 9V99 counter fed 1.000 then −0.005 held 1.00 where `ADD` gives 0.99. Any `CobolSizeError` / `OverflowException` from that evaluation (a sum past the capacity, an intermediate past the carrier, a PROHIBITED-inexact transfer) is the GR3 size error: the counter keeps its value and the indicator is set. An addition past the counter's capacity is the GR3 size error — the counter keeps its value (ADD ON SIZE ERROR, §14.7.5 1)), the indicator is set, EC-REPORT-SUM-SIZE is raised (fatal, checking-gated); GR4 fills the printable item with SPACES while the indicator is set (unconditionally — the emitted compose asks `SumPresentable`, which also raises); INITIATE (§14.9.21.4 GR1a) and every GR2 reset unset it. The counter carries a LIST of `SumTerm`s — one per `SUM … [UPON …]` group, because §13.18.54.3 SR1 lets the SUM keyword "appear more than once" in one clause and GR7c2 attaches each UPON phrase to ITS group |
| GROUP INDICATE | §13.18.28.4 GR1 — "the same effect as a PRESENT WHEN clause" whose condition is true only on the first GENERATE issued for the current DETAIL group after an INITIATE (a), a page advance (b) or a control break (c). The condition is PER DETAIL GROUP (`ReportGroup.GroupIndicatePending`): `ArmGroupIndicate` sets it on every detail at each of the three events, and `GenerateCore` clears it on the GENERATEd detail AFTER its presentation (so the group's own page-fit advance re-arms what its lines read; a SUPPRESSed or all-absent presentation still consumes it — the GENERATE was issued). `PresentBody` publishes it as `CobolReport.GroupIndicatePresent` before composing, and the emitter ANDs that into the indicated item's presence test beside its PRESENT WHEN chain and OCCURS DEPENDING guards (§13.18.63.4 GR22 names all three suppressors) — so a non-presented indicated item is ABSENT (§13.18.41.4 GR2b): it places nothing, moves no horizontal counter, and a relative COLUMN operand needs nothing of its own (kb/Work PB1244; the former report-wide flag and post-compose column-span blanking are gone) |
| USE BEFORE REPORTING | §14.9.49 Format 2 GR8/SR9 — the declarative section binds to the named group (`BoundDeclarative.ReportGroup`) and runs (a `__RunUse` bounded dispatch) just before the group is produced. ⛔ **Which declarative runs is chosen per STATEMENT, not per group** (kb/Work PB369): §14.9.49.4 GR4 is headed FORMATS 1 AND 2 — a) the qualifying declarative of the source element containing the GENERATE / TERMINATE, else b) a GLOBAL one of the next containing element, repeated outward. So there is NO per-group hook: every unit that can see a report and has a qualifying declarative in its GR4 chain emits `__BeforeReporting_{uid}(int gi, bool globalOnly)` (a non-GLOBAL case runs only when `!globalOnly`; the tail is `__outer.__BeforeReporting_{uid}(gi, true)` while a container up to the declaring program has a GLOBAL one), and GENERATE / TERMINATE hand the engine that selector (`Generate(name, sel)` / `Terminate(sel)`; cached per instance in `__brSel_{uid}`). The engine keys it by `ReportGroup.Index`, assigned by `AddGroup` in report-description order. `ReportWriterEmitter.EmitBeforeReportingSelectors` / `ChainSelects` |
| GLOBAL RD | §13.18.27.3 SR1 e) / §13.18.27.4 GR1–GR2 (kb/Work PB369) — `ReportModel.IsGlobal`; `BinderDriver.BindUnitData` makes each container's GLOBAL report visible to a contained program (`DataBinder.InheritGlobalReport` — nearest container first, a local name hides), with its groups and its sum counters (GR1: data-names subordinate to a global name are global). ⛔ Two lists: `DataBinder.Reports` is what a unit DECLARES (constructs, validates, binds the description of); `DataBinder.VisibleReports` is what its procedure division can NAME — every report-name / group / counter / USE resolution reads it, pinned by `GlobalReportScopeTests.ProcedureBinding_ResolvesReportsOnlyThroughVisibleReports`. The report is ONE engine in the declaring program, reached through the `__outer` chain (`DataBinder.ReportDepth` → `RuntimeApi.ReportEngine(index, depth)`; the counter places carry the depth). A contained GENERATE / INITIATE / TERMINATE also requires the report's FD to be GLOBAL (§14.9.16.3 SR3/SR4, §14.9.21.3 SR2, §14.9.46.3 SR2 → COBOLNET2392). SR4 (no GLOBAL in a class) → COBOLNET1520 |
| the REPORT-GROUP REFERENCE | ⛔ **ONE funnel, `Binding/ReportGroupResolution.cs`** — THREE sites name a report group by name (`GENERATE data-name-1` §14.9.16.3 SR1, `USE BEFORE REPORTING identifier-1` §14.9.49.3 SR9, and the SUM clause's `UPON data-name-2` §13.18.54.3 SR7 — the DATA-division one, resolved in `ResolveReports`). The two statement sites share the parse rule `reportGroupReference` (`cobolWord ((IN|OF) reportName)?` — §8.4.2.2.2 Format 1's file-report-qualifier, the only qualifier a level-01 group has; §8.4.2.2.3 SR3 makes IN ≡ OF) and the one resolver. It collects EVERY candidate and reports **COBOLNET1920** when more than one survives (§8.4.2.2.1 / §8.4.2.2.3 SR1). Before kb/Work PB365 both sites returned on the FIRST match, so the same 01-level name in two RDs bound to whichever report was written first, silently — and GENERATE's qualifier did not parse at all. `ReportGroupResolutionDriftTests` keeps the search out of every other file |
| the BEFORE REPORTING RANGE | §14.9.49.4 GR10 — a GENERATE, INITIATE or TERMINATE executed **within the range of** a USE BEFORE REPORTING declarative sets EC-FLOW-REPORT, is **unsuccessful**, and leaves **the state of the report unchanged**. `RunBeforeReporting` — the ONE funnel every presentation path uses — brackets the statement's GR4 selector with `RunUnit.ReportFlow.Enter()/Exit()` (a `finally`, so a fatal EC out of the declarative cannot latch the range), and the three verbs consult `InBeforeReporting` first. ⛔ **The range is the RUN UNIT's, not one report's and not one program's**: GR10 attaches no element qualifier where §14.9.18.4 GR6 attaches one explicitly for EC-FLOW-GLOBAL-GOBACK ("… in the same program as the GOBACK statement"), and the standard's other flow rules read the same way (§14.9.32.4 GR1's "within the range of an input procedure"). A per-`CobolReport` flag would pass `2023/pb326_flow_report_cross_report` while being wrong. A DEPTH, not a bool — two different groups' declaratives nest |
| SUPPRESS | §14.9.45 — `SUPPRESS PRINTING` names its GROUP (`BoundSuppress.Group`, `__RPT_n.SuppressPrinting(groupIndex)`), and the engine honours it only while THAT group's own BEFORE REPORTING hook runs (`RunBeforeReporting` scopes `_hookGroup`/`_hookSuppressed`: cleared on entry, restored on exit) — GR1 "only for the report group named in the USE procedure", GR2 current instance only; reached by a PERFORM from elsewhere (§14.9.49.3 SR4) it has no effect (kb/Work PB1186). The target group is the lexically-enclosing USE BEFORE REPORTING group, resolved at bind from `BindCursor` ∈ the declarative's pc range (GR1); a SUPPRESS outside such a procedure is COBOLNET1581 (SR1). GR3 a–d inhibit printing / page advance / NEXT GROUP / LINE-COUNTER, but NOT sum accumulation (GR7, already done in Generate) nor the end-of-group sum reset (GR2) — so a suppressed control footing's totals stay correct (only PRESENT WHEN / ODO absence skips the reset, GR10). Body groups run `EndOfGroupSumReset` on the suppressed path; heading/footing groups (no reset) return after the hook |
| PRESENT WHEN | §13.18.41 Format 1 — ⛔ **ONE PRESENCE SNAPSHOT PER GROUP PRESENTATION** (kb/Work PB1272). The emitter gives each report group with a conditioned entry a generated probe (`__RPT_P_{r}_{g}(bool[])`, `ReportWriterEmitter.PresencePlan`) holding one slot per conditioned LINE (its chain AND its enclosing OCCURS … DEPENDING counts), per conditioned printable ITEM (its field-local chain AND its repetition guards) and per conditioned SUM entry the group prints (its full chain); the engine runs it ONCE per presentation (`BeginPresentation` → `ReportGroup.SnapshotPresence`), BEFORE any LINE processing (GR2; §13.18.38.4 GR13 for DEPENDING) and AFTER the USE BEFORE REPORTING selection, and the placement, the page-fit trial, the compose (`__RPT_n.IsPresent(g, k)`) and the sum reset all read the same answers. Only the LINE chains used to be snapshotted: an item's chain ran inside its compose — after the page advance the group's own fit test had caused, so a page heading's declarative could change it — and a SUM entry's chain ran again at the end of the group. A GROUP INDICATE condition is NOT a slot: it is engine state the group's own page advance re-arms (§13.18.28.4 GR1; kb/Work PB1244). ⛔ **That order is a DETERMINATION, not a reading** (kb/Work PB367b): §14.9.49.4 GR9 d) performs the declarative "Before the processing of any LINE clauses defined for the report group" and GR2 evaluates condition-1 "before the processing of any LINE clauses for the report group" — the SAME boundary, with no rule ordering them, and both precede the page fit test (GR9 c); §13.18.41.4 GR3 d). It is settled this way because the reverse makes a declarative's execution depend on data the declarative exists to set: condition-1 is any condition (§13.18.41.2), typically over the very items §14.9.49.4 GR8 lets the procedure prepare ("just before the named report group is produced"), and a level-01 PRESENT WHEN would otherwise silently suppress the procedure that would have made the group present. Witnessed by `ReportWriterConformanceTests.UseBeforeReporting_PresentWhen_DeclarativeRunsBeforeTheConditionIsEvaluated`; an absent line is SKIPPED so the next relative line re-anchors on LINE-COUNTER (GR2b — the line collapse); the fit-test form, the trial sum, and the GR5 first-line placement key on the first PRESENT line (§13.18.35.4 GR4/GR5; absent relative lines excluded from the trial, §13.18.41.4 GR3d); no PRESENT line ⇒ no fit test, no printing, no counter movement and no NEXT GROUP (a dummy group, §8.4.3.15.4 GR5) — but the group's processing still ENDS, so its end-of-group sum reset runs (`BeginGroup`): GR2b omits the ENTIRE group only for an absent LEVEL-01 entry, whose condition every counter of the group carries in its chain, so those counters are not reset anyway (kb/Work PB1272); an absent SUM entry is neither printed (the compose reads its slot) nor reset (`EndOfGroupSumReset` reads the same slot — GR3g/§13.18.54.4 GR10); absent printable items place nothing and never advance the horizontal counter (GR3e/GR3f) |
| VARYING | §13.18.64 — per-repetition counters over the multiple-COLUMN repetition vehicle (SR1): compose-local `Int128`s (GR1 — "an independent temporary integer data item that shall be large enough to contain the maximum expected value"; kb/Work PB1305), first occurrence ← FROM (default 1, GR3a — re-evaluated per presentation), += BY per repetition (default 1, GR3b); each value persists through its occurrence (GR4 — `SOURCE IS counter` renders it, GR4 NOTE). FROM/BY land through `CobolReport.VaryingInteger` in their own lane (fixed point unscaled at its scale, binary64, or the standard-decimal intermediate); a noninteger value raises EC-REPORT-VARYING (GR5, fatal, bound to GENERATE/TERMINATE through `EcBinder`'s report-production names) and, with checking off, takes the value's integer part — GR5 leaves the print line undefined (§A.2 item 63) |
| multiple/relative COLUMN | §13.18.14 F1 — a multiple COLUMN clause defines one printable item per operand (GR12); relative (PLUS) operands place at `horizontal counter + integer-2` (GR8) with the counter starting at 0 (GR7) and set to each placed item's rightmost column (GR9) |
| the VALUE / SOURCE OPERAND LIST | ⛔ **ONE list, ONE cycling reader, ONE syntax screen** (kb/Work PB506). ISO writes the same two rules twice, once per clause: §13.18.63.3 SR35 = §13.18.53.3 SR6 (a multi-operand clause requires a repeating entry — §13.15.4 GR3 — and an operand count equal to its repetitions, or that number multiplied by the repetitions of successive higher repeating entries) and §13.18.63.4 GR23 = §13.18.53.4 GR4 ("successive operands are assigned to successive repeating printable items, horizontally and then vertically … If no further operands remain, assignment begins again from the first operand"). So `ReportFieldModel.Sources` is a LIST (a single-operand clause is a one-element list), `ReportFieldModel.SourceAt(rep)` is the only per-repetition reader — indexed by the repetition ORDINAL, which is what makes GR23's last sentence true by construction ("If any of the printable items are suppressed as a result of a PRESENT WHEN clause … operands are nevertheless assigned to them"), and `DataBinder.Reports.ScreenRepeatingOperandCount` is the ONE screen, fed by both clauses with its own diagnostic each (**COBOLNET2012** VALUE / **COBOLNET2013** SOURCE) and the repetition chain read off the entry scope stack. Before PB506 `FieldValueSource` held ONE glued string (`ExtractValue`'s `GetText()` over the whole list, so `VALUE "XX" "YY"` reached the emitter as `"XX""YY"` and printed `XX"YY`) and the SOURCE clause had no multi-operand grammar surface at all. GR23's wrap-around sentence became REACHABLE when both repetition axes did (kb/Work PB565) — a repeating entry subordinate to another one has more placements than a conforming operand count, `conformance:2002/pb565_report_vertical_repetition` lines 30–31 — and `ReportOperandListDriftTests` still asserts it on the model |

**The four statement-precondition conditions, and what `>>TURN` does and does not gate.** EC-FLOW-REPORT
(§14.9.49.4 GR10), EC-REPORT-ACTIVE (§14.9.21.4 GR2), EC-REPORT-FILE-MODE (§14.9.21.4 GR3) and
EC-REPORT-INACTIVE (§14.9.16.4 GR7 / §14.9.46.4 GR1) are Table 13 **Fatal**, and each rule states its LENIENT
outcome outright — "no other effect", "no action is taken on the report", "the execution of the statement is
unsuccessful", "the state of the report is unchanged". So the engine's **return is unconditional** and only the
**raise** is gated by checking (§14.6.13.1.1), exactly as for EC-FLOW-SEARCH / EC-BOUND-TABLE-LIMIT. The raise
channel is the ordinary one: `EcBinder` binds a PRECISE `BoundEcChecked` wrapper on `BoundInitiate` /
`BoundGenerate` / `BoundTerminate`, `EcEmitter.FatalAmbientGates` sets the `ExceptionState.…Checking` flag
around the statement, and `CobolReport` calls the matching `ExceptionState.…Error` helper — which raises a
`CobolFatalException` the statement guard catches for USE-F3 dispatch / RESUME. All four were catalogue rows
with no raise site anywhere in `src` before kb/Work PB326.

A MULTI-NAME INITIATE or TERMINATE is several statements, not one: §14.9.21.4 GR5 and §14.9.46.4 GR4 make it
"the same as if a separate … statement had been executed for each report-name-1 in the same order", and both
add "processing resumes at the next implicit … statement, if any" for a declarative that ends in RESUME … NEXT
STATEMENT. `BindInitiate` / `BindTerminate` therefore build ONE `BoundInitiate` / `BoundTerminate` per
report-name inside a `BoundImplicitSeries`, and the checked wrapper distributes over its members — so the
statement guard a raise from `__RPT_n.Initiate()` unwinds to is THAT report's, and a resume lands on the next
report rather than past the whole verb (kb/Work PB419; COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D11, "The
MULTI-OPERAND arm").

**The GR4c trial-sum ambiguity (decided):** the 2023 wording "incremented by integer-2 for each *subsequent*
LINE clause" is ambiguous for the FIRST relative line's integer-2. The NIST goldens + the legacy resolve it
as **trial = LINE-COUNTER + Σ integer-2 over ALL relative lines** (RW103A overflows exactly at LC=25 with
LAST DETAIL 25 and one `PLUS 1` line); GR5b3 then ignores the first line's relative value on the new page
anyway. Encoded as Σ over all; the alternative reading prints one detail past LAST DETAIL and cascades
off-by-one through every later counter check.

## 3. Binding (`DataBinder.Reports.cs`)

- `BindReportSection` runs in `DataBinder.Bind` right after `BindFileSection`; `ResolveReports` runs
  post-build after `ResolveFiles` (the FILE STATUS capture-then-resolve pattern — ONE resolution point).
- **Geometry defaults** per §13.18.39.4 GR3 (HEADING→1; FIRST DETAIL→HEADING; LAST CH→LD else FOOTING else
  limit; LD→FOOTING else limit; FOOTING→LD else limit). No PAGE clause ⇒ unpaged (GR2a — one page of
  indefinite length; fit/advance machinery inert).
- **Line-building rule** (§13.15): walking a group's entries in declaration order, an entry with a LINE
  clause OPENS a new report line (**LINE is legal at ANY level** — RW101A puts `LINE PLUS 1` on an 03; a
  binder that reads LINE only at the 01 produces a lineless group and a never-moving LINE-COUNTER); an entry
  with a COLUMN clause appends a printable field to the CURRENT line. TYPE abbreviations per §13.18.57.3 SR9.
- **EVERY integer-n OF THE REPORT WRITER IS A LITERAL POSITION, AND ONE RULE SAYS SO** (kb/Work PB1947). §5.5 1) calls
  an `integer-n` "a fixed-point integer literal" and §13.10.3 SR2 lets a constant-name stand "anywhere that a format
  specifies a literal of the class and category of constant-name-1", so `LINE PLUS KL`, `COLUMN KC`, `COLUMN PLUS KR`,
  `NEXT GROUP PLUS KG`, every integer of the PAGE clause (LIMIT, the COLUMNS width, HEADING, FIRST DETAIL, LAST
  CONTROL HEADING, LAST DETAIL, FOOTING) and `OCCURS … STEP KS` are legal. The grammar spells each of them
  `integerOperand : integerLiteral | cobolWord` (`CobolExpressions.g4`, beside `integerLiteral`) — the rule the OCCURS
  bounds wrote privately as `occursBound` before — and `IntegerOperandSlotDriftTests.EveryRuleWritingABareIntegerLiteral_IsAnArguedLiteralOnlySlot`
  keeps a bare `integerLiteral` out of the report-writer grammar (and every other), so the clause added next admits a
  constant without anyone remembering to. The binder reads every one through the ONE `DataBinder.IntegerOperandValue` (the OCCURS reader,
  generalised): the integer as written, or the integer constant's value, which §13.10.4 GR1 makes "as if the literal
  were written" and therefore meets what the written literal meets — §5.5 1)'s unsigned/nonzero default with each
  clause's own zero permission (`IntegerOperandRules.Slots`, classified by the CLAUSE that owns the operand, not by
  the carrier rule) and the host limit (COBOLNET2427). A constant that is not an integer is COBOLNET1547, a zero one
  COBOLNET2386. The value is memoised per operand node because the report binder replays a repeating entry's
  subtree (§13.18.38.4 GR10) and one bad constant is one diagnostic.
- **PLUS and + are ONE grammar fragment** (kb/Work PB951): `reportRelativeSign : PLUSWORD | PLUS` is the only
  spelling of a relative operand in `CobolReportWriter.g4`, referenced by the LINE, COLUMN and NEXT GROUP
  productions (§13.18.35.3 SR1 / §13.18.14.3 SR2 / §13.18.37.3 SR2 each print "PLUS and + are synonyms"), and
  every binder reader asks `reportRelativeSign()`. `GrammarRelativeSignDriftTests` pins that every parser rule
  naming the word also names the symbol, and that the report grammar spells the pair in that one rule.
- **NEXT GROUP** (§13.18.37; kb/Work PB957) is captured on its level 1 entry's group during the walk and bound
  by `BindNextGroupClauses` once the RD's groups are complete (the TYPE clause may follow it in the entry, and
  SR6a/SR6c/SR7 read the group's lines). It screens §13.15.3 SR6 over the flat entry array and §13.18.37.3
  SR1/SR3/SR4/SR5/SR6/SR7 per group on **COBOLNET2284**; "the minimum last line number of the report group" is
  the GR5/GR7 placement walked over the group's UNCONDITIONAL lines (`MinimumLastLine` — a PRESENT WHEN or
  OCCURS DEPENDING line may be absent, and absence can only leave the last line higher up). The binding is the
  runtime's own `ReportNextGroup` record, which the emitter writes verbatim.
- **A REPEATING ENTRY IS A SUBTREE REPLAY, AND THAT IS THE ONLY REPETITION MECHANISM** (§13.18.38 format 3 —
  `OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]`; kb/Work PB565).
  `ReportOccursOf` reads the clause and enforces its syntax rules (one bundled code, `COBOLNET2021`: SR1a no
  OCCURS on an 01 entry · SR10 nesting only without DEPENDING · SR16 bounds · SR17 integer data-name-1 · SR24
  TO⊥DEPENDING together · SR25 STEP required over an absolute COLUMN · SR26 integer-3 ≥ the repeated item's
  span · SR27 a DEPENDING entry is followed only by its subordinates · and the DYNAMIC/KEY/INDEXED phrases,
  which belong to formats 1, 2 and 4). `BindReportEntries` then binds the entry AND every entry subordinate
  to it once per repetition. **Replaying the subtree is what makes GR11 free** — "any PICTURE, USAGE, SIGN,
  VALUE, JUSTIFIED, BLANK WHEN ZERO, or GROUP INDICATE clauses have the same effect on each repetition as
  they would on a single data item without the OCCURS clause" is satisfied by running the whole clause binder
  again, not by teaching each clause about repetition; §13.18.63.4 GR21's import of GR9 (a VALUE reaches
  every occurrence, both the *contains* and the *subordinate to* leg) rides it unchanged.
  - **THE AXIS is fixed once** (§13.18.38.4 GR10/GR12), on `ReportOccursSpec.Axis`: GR10a/GR10b and
    GR12a/GR12b are the COLUMN arms, GR10c/GR10d and GR12c/GR12d the LINE arms, so the same integer-3 is a
    horizontal interval on one entry and a vertical one on another. An entry that contains, or has subordinate
    to it, a LINE clause repeats VERTICALLY; every other repeating entry repeats horizontally. Every reader
    asks `ReportGroupBuild.Shift(axis)` / `Undisplaced(axis)`, never `Spec.Step` directly, so integer-3 can
    never displace on the axis it does not belong to (`ReportRepeatingEntryDriftTests`).
  - **THE MULTIPLE LINE CLAUSE IS THAT SAME REPLAY**, because §13.18.35.4 GR9 says so: "A multiple LINE clause
    is functionally equivalent to a LINE clause with a single operand, together with a simple OCCURS clause
    whose integer is equal to the number of operands of the LINE clause, except that the multiple LINE clause
    allows the report lines to be defined at unequal vertical intervals." `DataBinder.ReportRepetitionOf`
    builds exactly that — a simple, STEP-less, vertical `ReportOccursSpec` — and the repetition ORDINAL then
    selects the LINE operand. §13.18.35.3 SR10 a/b/c/d and SR4 are screened on **COBOLNET2199**; SR10d (no
    OCCURS in the same entry) is what keeps §13.15.4 GR3's repetition count single-valued.
  - **Placement** (§13.18.38.4 GR12) is `ReportColumnKindModel` horizontally and `ReportLineKindModel`
    vertically, built by the twin helpers `RepeatedPlacements` and `RepeatedLine`: the displacement
    Σ ordinal × integer-3 is ADDITIVE over the enclosing repeating entries OF THAT AXIS, so an `Absolute`
    operand simply moves by it. A relative
    operand has no compile-time column, so its first repetition is an `AnchorSeed` — it places as GR8 says
    and remembers the column in a compose-local `__raN` — and the later ones are `AnchorStep`, placing at
    anchor + displacement. The anchor and not the horizontal counter is the datum because GR12 measures from
    the preceding occurrence's LEFTMOST while §13.18.14.4 GR9's counter holds its RIGHTMOST. With no STEP
    phrase nothing is displaced — GR12's closing sentence gives the interval to the relative COLUMN numbers,
    which the replayed operand reproduces against the counter.
  - **Vertically the same shape, one storage level up**: an absolute LINE clause displaces to
    integer-1 + Σ ordinal × integer-3 and stays a compile-time constant (§13.18.38.3 SR25a/SR25b REQUIRE the
    STEP phrase there, so the constant always exists); a relative LINE clause has no compile-time page line,
    so its first occurrence seeds an engine-held anchor (`ReportGroupLine.Anchor`, cleared per presentation)
    with the line it lands on and later occurrences are `ReportLineKind.Step`, placing at anchor +
    displacement. The anchor and not LINE-COUNTER is the datum because GR12d measures from "the line they
    occupy in the preceding occurrence" while §13.18.35.4 GR1's LINE-COUNTER holds the last line PRINTED, and
    the lines between them belong to the intervening occurrences. `ReportGroupLine.TrialInterval` carries the
    line's §13.18.35.4 GR4c page-fit contribution — its expected offset minus the running one — so Σ over the
    group is exactly "the expected position of the last line of the report group" however the repetitions
    interleave. The engine's four group presentations share ONE subsequent-line rule, `SubsequentTarget`
    (GR7 + GR12c/GR12d); before the vertical axis they were four copies of `LINE-COUNTER + integer-2`.
  - **DEPENDING** (GR13) is a presence test composed into the SAME snapshot slot as the PRESENT WHEN chain —
    per PLACEMENT horizontally (`EmitFieldPlacements`) and per report LINE vertically (`LinePresent`), read
    "just before the processing for the first LINE clause of the report group" (the group's presence probe,
    kb/Work PB1272), because a
    repetition the count excludes does not EXIST and on the vertical axis that is a whole absent line, not a
    blank one — so the two suppressors §13.18.63.4 GR22 names cannot drift: repetition *n* appears iff
    `n < (data-name-1 ∈ [integer-1, integer-2−1] ? data-name-1 : integer-2)`. Every repetition is BOUND
    either way, which is GR23's "VALUE operands are nevertheless assigned to them, even though they are not
    printed".
  - **A VARYING COUNTER IS THE DATA ITEM OF ITS ENTRY** (§13.18.64; kb/Work PB1306). GR1 gives "each entry
    containing a VARYING clause" its counter, SR2 scopes it "only within the current entry or a subordinate entry"
    and SR3 lets a subordinate entry's FROM and BY name it. So the counter is a `ReportVaryingModel` made by
    `VaryingCountersOf` ONCE per written entry per enclosing occurrence — in `BindReportEntries`, around the entry's
    replay, so every repetition shares it and a group entry's counter exists though no printable field of its own
    carries one — and `ReportGroupBuild.Varying` is the scope stack of the entries whose subtree is being bound
    (the declaring entry, its counters, and the repetition frame whose ordinal is the occurrence in hand). Each
    printable field snapshots the counters in scope as `ReportVaryingUse`s (counter + the occurrence of its DECLARING
    entry the placement lies in). GR3's "first occurrence" restarts with every occurrence of an enclosing
    repeating entry, because the counter is a new instance there. `ScreenReportVaryingClauses` asks SR1/SR2/SR3 once
    per WRITTEN entry over the flat entry array (so a replay cannot multiply them); the arm of SR2 that asks what ELSE
    defines the name runs in `ResolveReports`, over `ReportModel.VaryingNames`, once the whole source element is
    described. A name is read through the scope: `ReportWriterBinder.VaryingExpr` (the operand and expression
    binders, right after the report counters) and the subscript renderer's `ResolveSubscriptName` both ask
    `ReferenceResolver.VaryingScope`, set for the span of one entry's clause binding — a counter is no data item,
    so no name table holds it.
    **Emission.** Each placement runs in its own `{ }` block (`EmitVaryingCounters`) that declares every counter in
    scope as the one compose-local `__kv{Uid}` at its occurrence's value, declaring entries outermost first because
    a FROM or BY may name the counters above it. Occurrence `n` of the declaring entry holds the closed form
    `FROM + n × BY` — never an accumulator spanning replays, since a replayed entry is one field per repetition. The
    forms are equal, not approximate: GR3 adds arithmetic-expression-2 itself and both operands truncate to scale 0
    once. The one exception is a BY that names a counter of its OWN entry (SR3's "same VARYING clause", `Recurrent`):
    its additions differ with every step, so the entry's counters are advanced `n` times in clause order from FROM
    (CONFORMANCE.md §3 "VARYING counters of one clause").
  - **SR25, LEG BY LEG** — "The STEP phrase shall be specified if the entry: a) contains an absolute LINE
    clause, or b) has an entry with an absolute LINE clause subordinate to it, or c) contains an absolute
    COLUMN clause, or d) is subordinate to an entry with a LINE clause and has an entry with an absolute
    COLUMN clause subordinate to it." a) ∪ b) is one subtree scan; c) and d) are NOT — c) asks about the
    entry's own clause and d) adds GR12b's "being itself subordinate to an entry with a LINE clause". Reading
    c/d as a single subtree scan refused `03 LINE PLUS 1 OCCURS 3 TIMES.` over a subordinate `05 COLUMN 1`,
    which is GR10c and conforming source. SR26 is measured on the entry's OWN axis: the repeated item's
    printed width horizontally, the lines one occurrence occupies vertically (`ReportEntryLineSpan`).
  - **No residue on either axis.** `COBOLNET0899 report-occurs-in-group` and `report-multiple-line` are
    RETIRED, never to be reallocated, and `ReportRepeatingEntryDriftTests.NeitherRepetitionAxis_StagesLoud`
    fails if either descriptor is re-declared.
- **A CONTROL-clause OPERAND is a written reference, and there is ONE of it** (`ReportControlRef` — name +
  IN/OF qualifiers + the reference modification, captured by the one helper `ControlOperandRef`). THREE clauses
  write such an operand and all three permit the ref-mod with the same integer-literal restriction: the CONTROL
  clause (§13.18.16.3 SR4), a TYPE CH/CF operand (§13.18.57.3 SR10) and `SUM … RESET ON` (§13.18.54.3 SR8). SR10
  and SR8 then ask the SAME question — "the same as one of the operands of the CONTROL clause" — which is
  `ControlLevelOf`, one comparison over the whole written reference. It has to be the written reference and not
  the resolved item, because §13.18.16.3 SR6's second sentence lets two operands "refer to the same physical data
  item or to overlapping data items"; `CONTROLS ARE CX(1:3) CX(4:3)` is the legal shape that proves it.
  A ref-modded operand is emitted through `ReferenceResolver.ResolveItemRefMod` — the SAME ref-mod view builder
  the procedure division uses (`RefModView`), reached by item instead of by parse context — so §13.18.16.4 GR3's
  prior control is the §8.4.3.3.4 GR5 unique data item, the SLICE. §13.18.16.3's operand rules are all screened
  in one place: SR2 (asked only after resolution fails — a name also in ordinary storage resolves there), SR3,
  SR4, SR5, SR6, SR7 and the subscript that SR3 + §8.4.2.3.3 SR2 leave no legal spelling for; §8.4.3.3.3 SR1 on a
  ref-modded operand reuses `RefModExclusion`, the one SR1 reader. (kb/Work PB205 — before it, `KeyReference`
  kept only the qualification suffix and TYPE/RESET kept only the base word, so `CX(1:3)` and `CX(4:3)` were one
  operand and every report broke on the whole item.)
- **A SUM clause OPERAND is a written reference too, and its VALUE is bound in the PROCEDURE phase**
  (`ReportSumAddend` — the `dataReference` parse context + base name + qualifiers, captured by the one helper
  `SumAddendRef`; kb/Work PB482). §13.18.54.3 SR1 makes *identifier-1* an addend and §8.4.3.1.2 Format 2
  makes an identifier a *qualified-data-name-with-subscripts*, so the SUBSCRIPT is part of the operand — and a
  subscript may be an integer literal, an index-name or an arithmetic expression (§8.4.2.3), none of which has
  a value in the data division. The addend is therefore bound in `ReportWriterBinder.BindReportGroupClauses`
  through the ONE `ExpressionBinder.BindExpr` (the `PRESENT WHEN` / `VARYING` precedent) and rendered by the ONE
  `NumericRenderer`; `ReferenceResolver` does the subscript arithmetic exactly as it does for a procedure-division
  reference. The arm choice is made once, in `ResolveSumAddend`: SR4's *data-name-1* (a report-section item —
  a rolled total, GR6 — and SR4 g)'s cross-report spelling of it) is resolved by `ResolveRolledAddend` (the
  "ROLLED TOTAL" bullet below), and SR5's *identifier-1* is screened for BOTH
  halves of its sentence — resolution outside the report section AND the CATEGORY (**COBOLNET2045**), which
  also refuses every reference-modified spelling, since §8.4.3.3.4 GR6 c) makes a ref-mod's unique data item
  alphanumeric and SR5 requires a numeric one. `UPON data-name-2` (SR7) is captured by `UponDetailRef` and
  resolved through the report-group funnel — it "shall be the name of a detail" and "may be qualified only by a
  report-name" (**COBOLNET2046**); a detail of ANOTHER report is legal, and because GR7 c) 2) accumulates on a
  GENERATE run against THAT report's engine the addition registers there (`ReportDetailRef.Owner`,
  `CobolReport.AddGenerateTrigger`). **ONE counter per ENTRY** (GR1) however many times the SUM keyword
  appears (SR1): the grammar's `reportSumClause` is the repeated `reportSumGroup` followed by the clause's ONE RESET
  phrase and ONE rounded-phrase (§13.18.54.2 — the outer brace closes after the UPON phrase; kb/Work PB1295), and
  each group becomes a `ReportSumTerm` with its OWN UPON list, emitted as one `AddSumTerm` call. A phrase written
  BETWEEN two SUM groups ends the clause, so the entry carries a second `reportSumClause`, which
  `ScreenReportEntryClausePresence` refuses (COBOLNET1559). The entry's PICTURE is screened once, from the site that
  analyses it (`ScreenReportEntryPicture`): the digit-position limit of §13.18.40.3 SR14 through the data division's
  own `ScreenPictureDigitCapacity` (kb/Work PB1687) and §13.18.54.3 SR2 through `MoveTable16.NumericSenderRefusal`
  (COBOLNET2750; docs/CONFORMANCE.md, the integer-row determination). A subscript in any report entry clause is asked
  once per written entry by `ScreenReportSubscripts` (§8.4.2.3.3 SR8, COBOLNET2751): the lexer captures a subscript as
  SUBSCRIPT-mode tokens, so `SubscriptWordsOfReference` reads its words, and a report section item that is not a
  counter is the SUM expression's / SOURCE's own section rule (`ReportSectionNameIn`). (Before PB482 the addend went through `KeyReference` — the
  FILE STATUS key helper — so `SUM WS-CELL(2)` compiled and ABORTED at the first GENERATE, `SUM WS-TXT(1:2)`
  silently summed the whole item, `SUM WS-TXT` over a `PIC X(6)` summed its digits, `UPON <a control footing>`
  and `UPON <an undeclared word>` were accepted and totalled nothing, and a second `SUM … UPON …` group
  overwrote the first. `SUM OF` — the format's optional word, §8.3.2.4.3 — was a parse error.)
  ⛔ The SOURCE clause's identifier-1 (§13.18.53) is the SAME mechanism's other arm and IS converted (kb/Work
  PB1292): it is kept as its written reference (`FieldReferenceSource`) and bound in the procedure phase by the ONE
  operand binder a MOVE's sending operand takes (`ExpressionBinder.FieldOperand`), which is §13.18.53.4 GR1's
  "sending operand of an implicit MOVE statement" said as code. That binder already knows every shape an
  identifier can take, so a subscript, a reference modification, a LINE-/PAGE-COUNTER of this or another report, a
  sum counter of the current report (`ReferenceResolver.ReportScope` narrows its candidates to the current report,
  §13.18.53.3 SR4), a VARYING counter in scope and a constant-name each stopped being a separate staged refusal.
  `ReportSumOperandCaptureDriftTests` no longer carries a `KeyReference` caller for SOURCE; the RD CODE clause's
  identifier-1 (§13.18.12.2) keeps the whole written reference the same way and asks the key helper only for the
  BASE item its SR2 screen is about (`ResolveReportCode`).
- **ONE OPERAND PRODUCTION FOR BOTH VALUE CLAUSES, AND THE BINDER DECIDES THE FORM** (kb/Work PB852 × PB883).
  §13.18.53.2 and §13.18.54.2 print the SAME operand brace — `{ identifier-1 | arithmetic-expression-1 }`, SUM
  adding *data-name-1*, itself an identifier — and both close with `[ rounded-phrase ]`. The grammar therefore
  has ONE `reportValueOperand : arithmeticExpression`, referenced by `reportSourceClause` and `reportSumClause`,
  and ONE `roundedPhrase?` on each (the shared §14.7.4 production, so the 2014 `MODE IS` gate rides along with
  no report-local rule). `DataBinder.Reports.IdentifierOperandOf` is the ONE classifier (over the syntactic walk
  `BareReferenceOf`): a tree that is exactly one `dataReference` NOT naming a constant is the identifier form
  (§13.18.53.4 GR1's implicit MOVE / §13.18.54.4 GR3's implicit ADD) — a bare constant-name is no identifier but
  arithmetic-expression-1 with one operand (§13.10.3 SR2 with §8.8.1.1; kb/Work PB1316), so a numeric constant is a
  SOURCE operand and an alphanumeric one is §8.8.1.1's / SR3's refusal;
  anything else, and any operand under the clause's ROUNDED phrase (§13.18.53.3 SR5 — "it is considered to be an
  arithmetic-expression"), is `FieldComputeSource` / an expression `ReportSumAddend`, bound in the PROCEDURE
  phase through the ONE `ExpressionBinder.BindExpr` and rendered into GR2's implicit COMPUTE. The screens are
  each clause's own: **COBOLNET2141** (§13.18.53.3 SR3 — the entry shall be numeric or numeric-edited),
  **COBOLNET2142** (SR7 — a multi-operand clause containing an expression parenthesizes EVERY operand; enforced,
  never inferred from the greedy parse), **COBOLNET2143** (§13.18.54.3 SR3 — SUM's ROUNDED needs a COLUMN
  clause) and **COBOLNET2144** (the SECTION rules, which differ by clause and are screened by the one walk
  `ReportSectionNameIn`: SOURCE's SR4 admits a report counter or a sum counter of the CURRENT report, SUM's SR6
  admits nothing of the report section at all).
  ⚠ **DETERMINATION — what the SUM clause's ROUNDED phrase rounds.** §13.18.54.4 GR4 ("the content of the sum
  counter is computed according to the general rules for the COMPUTE statement with the ROUNDED phrase") is read
  as governing GR3's ACCUMULATION of each addend into the counter. The rejected reading — that it governs GR4's
  subsequent MOVE of the counter to the printable item — makes the phrase provably dead, because GR1 derives the
  counter's integral AND fractional digits from that item's own PICTURE, so that transfer is always
  scale-identical. The mode reaches the accumulation through `NumericRenderer.Align`'s one optional parameter.
- **THE SUM COUNTER IS A PLACE, AND ITS IDENTITY IS ITS ENTRY** (kb/Work PB882 × PB840). §13.18.54.4 GR1 gives
  EACH entry its own counter, so `ReportSumModel.Id` is the entry's ORDINAL in its report description and the
  engine holds the counters in a `List<SumEntry>` indexed by it; GR5's data-name rides alongside as
  `ReportSumModel.Name`. (It used to BE the id, and `CobolReport._sums` was keyed by it: two entries legally
  sharing a data-name shared one counter and the second registration destroyed the first — `0022  0022` printed
  where the standard owes `0011  0022`.) GR5 also puts that name in the source element's name space, because
  GR12 permits procedure division statements to "alter the content of sum counters" and altering presupposes
  referencing: `DataBinder.SumCounters` (name → EVERY counter carrying it, a list because the collision is legal
  to DECLARE) feeds `ReferenceResolver.SumCounterFor`, which builds a `ReportSumCounterPlace` — the CAPACITY /
  DEBUG register pattern, a VIEW over engine state whose read and write `PlaceRenderer` maps to
  `CobolReport.SumValue` / `SetSumValue`. Because it is a Place, every verb reaches it through the machinery it
  already has, receiving side included; the former read-only `BoundReportSumRef` is DELETED, and the compose of
  a SUM entry's printable face now goes through the same place (which is what GR4's "moved, according to the
  general rules of the MOVE statement" asks for). An ambiguous or mis-qualified reference is **COBOLNET2145**;
  the counter's own profile (§13.18.54.4 GR1 — signed, digits from the entry's PICTURE) is
  `PicInfo.SumCounterItem`, emitted beside the printable items' `NumProfile`s.
- **A REPEATING ENTRY'S SUM COUNTER IS A TABLE, ONE COUNTER PER OCCURRENCE** (kb/Work PB1271). §13.18.54.4 GR8 a)
  adds each occurrence of a repeating addend "into the corresponding occurrence of the sum counter", GR10 speaks of
  "the corresponding sum counter" of an absent occurrence, and all three §13.15.4 GR3 repetition vehicles are OCCURS
  levels — the OCCURS clause itself, the multiple LINE clause (§13.18.35.4 GR9) and the multiple COLUMN clause
  (§13.18.14.4 GR12: "functionally equivalent to a COLUMN clause with a single operand, together with a simple OCCURS
  clause"). So a SUM entry is a `ReportSumFamily` — its name, its ONE register, scale and qualification — and its
  occurrences are `ReportSumModel`s. `DataBinder.SumFamilyOf` creates the family at the entry's FIRST replay and
  RESERVES its counter-id block there: the extents are the enclosing repetitions' counts (`ReportGroupBuild.Repetitions`,
  outermost first) then the entry's own multiple COLUMN count (§13.18.14.3 SR10 a) keeps OCCURS out of that entry),
  and an occurrence's id is `BaseId` + its row-major coordinate (`ReportSumFamily.IdAt`). The block has to be
  reserved because the replay binds siblings between repetitions (A0 B0 A1 B1), so binding order is not id order;
  `SealSumCounters` sorts `ReportModel.Sums` into id order after the RD is bound and throws if a reserved id has no
  counter (a binder defect — every repetition is bound). A multiple COLUMN SUM entry binds one counter per operand
  and its printable field's `Sources` pair them with the repetitions through `SourceAt`, exactly as a multi-operand
  SOURCE does — the single-register model this replaced shared ONE counter among the entry's printable items.
  **The reference side is the ordinary table machinery**: the family register carries a synthetic ancestor per
  enclosing repetition with `Occurs` = its extent (and its own `Occurs` = the COLUMN count), so
  `ReferenceResolver.SumCounterFor` reads the written subscripts through the ONE `ReadSubscripts` (§8.4.2.3.3 SR3
  count, SR5 "Each table element reference shall be subscripted" — an unsubscripted `CF-U` of a multiple COLUMN
  entry is now COBOLNET2270, SR4 index-name association) and the table(ALL) intrinsic argument reads the levels and
  their counts off the family (`IntrinsicBinder.TryBindAllArgument` asks `SumCounterFamilyFor` first, in
  `Resolve`'s order; `ReferenceResolver.SumCounterOccurrenceCount` ranges a DEPENDING level over §13.18.38.4 GR13's
  count — §15.3: "the range of values is determined by the object of the OCCURS DEPENDING ON clause" — as
  `AllCount.ReportDepending`, the ONE GR13 count the per-repetition presence test also renders, evaluated by
  `CobolReport.DependingCount`; it is not the data division's clamp with EC-BOUND-ODO). The place (`ReportSumCounterPlace.Subscripts`) carries the rendered subscripts; the renderer
  addresses the counter through the ONE `RuntimeApi.ReportSumAddress` — the id, or the family's `BaseId`, the
  extents and the subscripts as `ReadOnlySpan` collection expressions (no allocation) — and
  `CobolReport.SumOccurrence` turns them into the occurrence's id, testing each level on its own and raising
  §8.4.2.3.4 GR2's EC-BOUND-SUBSCRIPT for one out of range (checking off: the read is zero and the store is
  discarded, the twin of an ordinary table's scratch occurrence). The printable face and the engine's
  registrations keep addressing a counter by its id. An addend that is itself a repeating REPORT item (GR8 a)/b),
  the rolled total) maps its occurrences onto the counter's by those same levels — the next bullet.
- **A ROLLED TOTAL — a SUM clause's data-name-1 — IS AN ADDITION REGISTERED WITH THE GROUP THAT CONTAINS ITS ADDEND**
  (kb/Work PB1294). §13.18.54.4 GR6 gives data-name-1 one value, by what its entry carries — "the corresponding sum
  counter" for an entry that contains a SUM clause, else "the operand of the SOURCE or VALUE clause" — and GR7 a)/b)
  say WHEN it is added: "when the report group description containing data-name-1 is processed", i.e. before the
  group's lines are printed, whether or not anything prints (a SUPPRESSed or dummy group is still processed:
  `BeginGroup` is the one prologue, §14.9.45.4 — SUPPRESS "does not inhibit sum accumulation"). The ENTRY is therefore
  the unit: `ReportEntryFamily` (the written entry — name, qualification hierarchy, group, report and the repetition
  geometry a sum counter always had; `ReportSumFamily` and the new `ReportSourceFamily` derive from it) with one
  `ReportItemOccurrence` per replay and per operand of its own multiple COLUMN clause, each carrying its coordinates,
  its SUM counter occurrence or its SOURCE/VALUE operand, and the FULL presence chain GR11 asks ("declared to be absent
  as a result of a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase"). An UNPRINTABLE SOURCE entry
  (no COLUMN, §13.18.53.4 GR3) has an occurrence too — it prints nothing, but a SUM clause may still name it.
  `ResolveRolledAddend` resolves the written name once (the qualifiers consumed against each entry's hierarchy, the
  report-name last — SR4 g) is the same arm, not a second one), screens it once per WRITTEN addend (SR4: a numeric item
  with a value, unsubscripted — COBOLNET2730; a) no UPON; b)/c)/d) the levels of repetition, `RolledRepetitionFault`;
  f) the report-type pairing, COBOLNET2731, within one report only; e) the chain terminates, `ScreenRolledChains` over
  every report) and then maps per COUNTER OCCURRENCE (GR8): the INNERMOST levels of the addend and the counter are the
  same levels (SR4 d) — "in order beginning with the lowest level of nesting"), so an addend occurrence belongs to the
  counter occurrence whose coordinates equal its own innermost ones, and its outer levels are the "complete table"
  GR8 b) totals; a non-repeating addend belongs to every counter occurrence. The addition is registered, once per
  (counter occurrence, addend occurrence), with the report GROUP that contains the addend — `CobolReport.AddRolled`
  on the counter's engine, stored on the source group (`ReportGroup.Rolled`), because the two may belong to
  DIFFERENT reports — and performed by that group's prologue (`BeginGroup` → `ApplyRolled`, skipped on an OR PAGE
  reprint, which is the same instance printed again). It is the SAME closure a GENERATE-driven term builds
  (`ReportWriterEmitter.SumAddition` — GR3's ADD with ON SIZE ERROR), over a value bound once every report's
  operands are (`ReportWriterBinder.BindRolledValues`: the counter occurrence's content as a `ReportSumCounterPlace`
  read, or the SOURCE/VALUE operand as a number). Registrations are emitted after EVERY engine is constructed
  (`RegisterCrossEngineAdditions`), and the ones on one group are put in dependency order, because GR6 requires "the
  additions necessary to compute its value are completed before the adding of the operand". The same list registers
  an UPON on a detail of another report. A term's rolled addends leave its GENERATE-driven addition (GR9 sums addends
  "separately according to the above rules"); the UPON phrase is refused with data-name-1 (SR4 a)). A SOURCE operand
  that names a VARYING counter (§13.18.64.4 GR3) is that occurrence's value, a compose-local when the entry prints,
  so such an addition's closure is a BLOCK that declares the same counter locals for the occurrence it adds, by the
  one `EmitVaryingCounters` the compose uses (`ReportItemOccurrence.VaryingDependent`).
- **The FD side**: `FileModel.ReportNames` (the §13.18.46 REPORT clause, captured in `BindFileSection`);
  a report file is an FD with a non-empty list — legally record-less (§9.1.22). `FileModel.RecordContains`
  captures the fixed Format-1 RECORD CONTAINS for the line width; otherwise the width is the widest field
  extent (column + image width − 1) — the §13.18.39.4 GR5 page-width default 999 is a maximum, not a record
  length, and the legacy's hardcoded 132 was arbitrary.
- **Counters** (§8.4.3.15; kb/Work PB1049, PB1456): `ReportWriterBinder.CounterExpr` intercepts LINE-/PAGE-COUNTER in
  `FieldOperand`/`RefExpr` ahead of name resolution (the LINAGE-COUNTER idiom); the OF/IN `cobolWord` is the report-name
  qualifier. ONE resolution, `CounterReportOf`, answers which report a counter names, for both directions and both
  divisions: a qualifier shall name a report description entry (and a counter of a DIFFERENT report is referenced
  exactly so — §8.4.2.2.3 SR9/SR10's third sentence — in the report section and the procedure division alike); in the
  PROCEDURE division an unqualified counter resolves only against a sole report; in the REPORT SECTION an unqualified
  counter "is qualified implicitly by the name of the report in whose report description entry the reference is made"
  (`ReferenceResolver.ReportScope`, set for the span of one RD's clause binding). A qualification violation is
  **COBOLNET2729** (`report-counter-qualification`). `ReferenceResolver.Resolve` early-returns
  for the counter tokens — LOAD-BEARING for the qualified form, where `cobolWord()` is the qualifier and
  would otherwise mis-resolve as a base data-name.
- **Receiving guard**: ALL receiving resolution (MOVE targets, arithmetic resultants ×3, SET targets) goes
  through ONE chokepoint, `ResolveReceiving` — a counter receiver is rejected at bind (LINE-COUNTER illegal
  per §8.4.3.15.3 SR3; PAGE-COUNTER legal-but-staged) instead of being silently dropped by
  `.OfType<Place>()` (the silent-miscompile hazard).
- **PRESENT WHEN chains** (§13.18.41 F1): conditions accumulate down a level-number stack while the flat entry
  list walks — §13.18.41.4 GR2b makes an absent ancestor absent every subordinate "irrespective of any PRESENT
  WHEN clauses they may also contain", so presence = the AND of INDEPENDENT chain conditions. A LINE carries
  the chain 01→line-entry; a printable field the slice strictly BELOW its line entry (the line's own chain
  already gates the whole line); a SUM entry the FULL chain (the GR3g print/reset suppression). Conditions are
  captured as parse contexts at data bind and bound in the procedure phase through the ONE `ConditionBinder`
  (`ReportWriterBinder.BindReportGroupClauses`, memoized per distinct context, called at the top of
  `StatementBinder.Bind`); VARYING FROM/BY bind the same way through the ONE expression binder.
- **The §13.15.3/§13.18.64.3 SR family = COBOLNET1559** (`report-group-clause-rule`, one bundled code): SR16 —
  condition-1 shall not reference LINE-/PAGE-COUNTER or a report-section data item (token scan in
  `ResolveReports` — `CheckConditionOperands` — over the PRESENT WHEN clauses the report WRITES, read off
  `ReportModel.WrittenEntries`, never off the bound lines and fields, so an entry that produces no line, field or
  counter is scanned too; the names are the report-section-EXCLUSIVE ones of EVERY report description, each
  written entry's data-name — a name also in ordinary storage resolves there and is exempt; kb/Work PB1289); SR17 — GROUP INDICATE ⊥ PRESENT WHEN in one entry; VARYING SR1 — the entry needs OCCURS / multiple
  LINE / multiple COLUMN — all three LIVE vehicles;
  SR2 — the counter shall not be defined elsewhere (a name twice in one entry, a name an enclosing entry defines, a
  data item or report section name — `ScreenReportVaryingClauses` and `ResolveReports`, once per written entry); SR3 —
  the counter shall not appear in its own FROM. A counter named in SOURCE, a subscript, FROM or BY resolves through
  `ReferenceResolver.VaryingScope` (§13.18.64.4 GR4 NOTE; see the VARYING bullet above).
- **The §13.15.3 CLAUSE-PRESENCE family = COBOLNET2247** (`report-entry-clause-presence`, one bundled code;
  kb/Work PB853), screened by `ScreenReportEntryClausePresence` ONCE per written entry over the flat RD entry
  array, before the walk — the `ScreenReportLineNesting` shape, so a §13.18.38 format 3 replay cannot
  multiply it. An entry is ELEMENTARY when the entry after it is not subordinate to it (§13.15.4 GR1). SR10 —
  an elementary entry with a COLUMN clause also contains a SOURCE, VALUE or SUM clause; SR11 — PICTURE,
  COLUMN, SOURCE, VALUE, SUM and GROUP INDICATE only in an elementary entry; SR13 — an elementary VALUE entry
  has a COLUMN clause; SR15 — BLANK WHEN ZERO / JUSTIFIED need a COLUMN clause; **SR5** — the TYPE clause only in,
  and in every, level 1 entry (`BindGroupType` runs for level 1 alone, so a stray TYPE never retypes its group;
  kb/Work PB1288); **SR9 / §13.18.14.3 SR3** — a COLUMN entry has a LINE clause of its own or an ancestor that does,
  read off the level hierarchy by `SubordinateToLineClause` (never off the line the build last opened — a COLUMN
  entry that is a sibling of a LINE subtree used to inherit it; kb/Work PB1224); **SR12** — a PICTURE in every
  elementary SOURCE or SUM entry, printable or not; and SR14's VALUE-only entry with no implied picture, in the
  binder's PICTURE arm (the two former COBOLNET0899 descriptors `report-column-without-line` and
  `report-item-missing-picture` are folded into this code). **§13.15.4 GR2's BLANK WHEN ZERO and JUSTIFIED subject
  rules** (§13.18.8.3 SR1/SR2, §13.18.32.3 SR3) are the data division's `BlankWhenZeroViolation` /
  `JustifiedViolation` predicates (COBOLNET2405), asked of the printable item's analysed picture. The same screen carries
  **§13.18.28.3 SR1 = COBOLNET2519** (`report-group-indicate-placement`, kb/Work PB1245): GROUP INDICATE only in a
  DETAIL group (the TYPE read from the level-01 entry through `WrittenGroupKind`, the helper
  `ScreenReportLineClauses` shares), and on an elementary entry only with a COLUMN clause and a SOURCE or VALUE
  clause — the elementary half is SR11's. SR8 is `ScreenReportLineNesting`
  (§13.18.35.3 SR4). ⛔ **The binder never invents an operand**: a printable entry with no SOURCE/VALUE/SUM
  operand left (SR10 refused it, or each written operand was refused at its own clause) produces NO field —
  the figurative-SPACE sender that once stood in for it is gone, pinned by
  `OccursOperandCaptureDriftTests.TheReportBinder_NeverFabricatesAnOperand`.
- **Every `data-name-n` operand of the OCCURS clause goes through the ONE capture `ClauseDataName`**
  (kb/Work PB885) — format 3's `DEPENDING ON data-name-1` here and the data-division format 2/4 operands
  (`DEPENDING`, `KEY`, `CAPACITY IN`) alike: §13.18.38.3 SR2/SR5/SR31 forbid a subscript, and §8.4.2.2.2
  Format 1 admits IN/OF qualifiers, so a subscripted operand draws COBOLNET2024 and a qualified one keeps its
  qualifiers. data-name-1 then resolves through `ResolveClauseOperand` (survivor-counting, silent for an
  operand the capture refused). `OccursOperandCaptureDriftTests` pins the callers of the dropping
  `KeyReference` capture to the named report operands whose own rules permit or screen what it drops.
- **The report FILE is the report writer's alone** (kb/Work PB1171). §13.4.5.3 SR8 — "No record description
  entries or constant entries shall be associated with the file description entry for a report file" — is asked
  in the FD loop after the clauses (`DataBinder`, **COBOLNET2578**). §13.4.5.3 SR9 (= §13.18.46.3 SR3) — the
  file-name "may be referenced in the procedure division only by the USE statement, the WHEN phrase of a PERFORM
  statement, the CLOSE statement, or the OPEN statement with the OUTPUT or EXTEND phrase" — is ONE screen,
  `StatementValidation.ScreenReportFileReference` (**COBOLNET2577**), asked by `ResolveFile` for every statement
  except OPEN and CLOSE (`admitsReportFile`), so a new file statement is refused a report file by construction;
  SORT / MERGE USING-GIVING resolve through `ResolveFile` with their own restatement (§14.9.40.3 SR8, §14.9.24.3
  SR9), WRITE / REWRITE ask it of their record's file (§14.9.51.3 SR12, §14.9.35.3 SR11), and FUNCTION
  EXCEPTION-FILE asks it of its argument. OPEN's INPUT / I-O half stays §14.9.27.3 SR1 (COBOLNET2371).

## 4. Emission (`CodeGen/Verbs/ReportWriterEmitter.cs`)

- Per report: `private CobolReport __RPT_n` + construction inside `__Activate`'s `if (!__filesRegistered)`
  block, **after** `EmitFileRegistration` — the registration order is load-bearing (§7 hazard 1). Report FDs
  with `Records.Count == 0` register in `EmitFileRegistration` with the report's line width (without this
  the OPEN falls into the keyed-organization else-branch and every report write silently no-ops).
- Per line: `private string __RPT_C_{r}_{g}_{l}()` — a space-filled `ReportLineImage` of LineWidth
  (`__RPT_n.NewLine()`), each field placed at its COLUMN (`__RPT_n.Place`) with the image its own clause
  gives it (`ConvertSource` for a SOURCE operand, the VALUE recipe for a VALUE operand — see §3), taken from
  `ReportFieldModel.SourceAt(rep)` so each repetition of a multiple COLUMN entry gets its own operand. A SOURCE
  identifier-1 arrives as the BOUND OPERAND the one operand binder made of it (`FieldReferenceSource.Value`) and takes
  the same one conversion as every other operand; SOURCE counters/sums/VARYING counters render through `NumericRenderer`
  (`BoundReportCounterRef` / the sum counter's place / `BoundReportVaryingRef` — one case each; both relation
  conditions and MOVE sources route through the renderer).
- Compose-side 2002 decoration (`EmitFieldPlacements`; the plain '85 shape — one absolute operand,
  unconditional, no VARYING, all-absolute line — keeps its exact single-statement emission, the
  characterization-pinned text): a field's PRESENT WHEN chain and its OCCURS … DEPENDING tests (§13.18.38.4
  GR13 / §13.18.63.4 GR22 — §3's replay) wrap its placements in `if (__RPT_n.IsPresent(g, k))` — its slot of
  the group's presence snapshot (kb/Work PB1272; §13.18.41.4 GR2b/GR3f: an absent item places nothing and never
  advances the horizontal counter); the line is a `ReportLineImage` (`__RPT_n.NewLine()`) and every item goes
  through the engine's `Place`, which tests the §13.18.14.4 GR4 column overlap against the line's occupancy and
  the GR5 page width (kb/Work PB1188); a multiple COLUMN entry unrolls one `Place` per operand, each with
  its placement's `{ }` block declaring the VARYING counters in scope as `__kv{Uid}` at the closed form
  `FROM + n × BY` of the declaring entry's occurrence `n` (§13.18.64.4 GR3; `Int128`
  locals; FROM/BY land as integers through `CobolReport.VaryingInteger`, which raises GR5's EC-REPORT-VARYING for
  a noninteger value — kb/Work PB1305; a BY naming its own entry's counter is the step-by-step form, kb/Work PB1306); `int __hc` (emitted only when a
  relative operand exists) realizes the §13.18.14.4 GR7/GR8/GR9 horizontal counter, and `int __raN` the
  §13.18.38.4 GR12 step anchors of this line's repeating entries.
- Construction decoration: a group with a conditioned entry gets `__rg.SetPresence(n, __RPT_P_{r}_{g})` (its
  presence probe, run once per presentation — GR2, kb/Work PB1272); a conditioned line appends its snapshot slot
  to its `ReportGroupLine`, and a conditioned SUM entry appends its slot to `AddSum` (the GR3g reset
  suppression, read from the same snapshot as its printable face). Both parameters are optional —
  unconditioned emission is byte-identical.
- Verbs: `__RPT_n.Initiate()/.Generate("DETAIL-NAME" | null)/.Terminate()`; multi-name statements unroll in
  written order (§14.9.21.4 GR5 / §14.9.46.4 GR4).
- Multi-unit: engine fields are per-instance; the engine's file name is the SAME emit-qualified
  `"PROG::FILE"` name `EmitFileRegistration` registers (the IC114A connector precedent).

## 5. The full §13/§14 RW surface — implemented vs staged LOUD

**Implemented:** PAGE LIMIT geometry + GR3 defaults; RH/PH/CH/DE/CF/PF/RF groups; absolute + relative LINE
(any level); COLUMN/PIC/SOURCE/VALUE/JUSTIFIED/BLANK WHEN ZERO/SIGN printable items, **with the multi-operand
VALUE and SOURCE clauses and the SOURCES/ARE spellings** (§13.18.63.2 format 4 / §13.18.53.2, edition-gated 2002
for the SOURCE forms — `report-multi-source-2002`); SOURCE
LINE-/PAGE-COUNTER; **the arithmetic-expression-1 operand of BOTH value clauses and their ROUNDED phrase**
(§13.18.53.2 / §13.18.54.2 — one shared `reportValueOperand` production, §13.18.53.4 GR2's implicit COMPUTE,
SR3/SR5/SR7 screened, §13.18.54.3 SR3/SR6 screened; kb/Work PB852 × PB883); **the SUM counter's data-name in
the procedure division's name space** (§13.18.54.4 GR5 + GR12 — read AND altered, `ReportSumCounterPlace`;
kb/Work PB840); CONTROL/CONTROLS incl. FINAL (breaks, prior-value CF composition, TERMINATE final
break) — **FINAL only as the FIRST operand, once** (§13.18.16.2 `FINAL [ data-name-1 ] …`, COBOLNET2421, kb/Work PB483), each RD clause and PAGE phrase at most once (§13.14.2 / §13.18.39.2 with §5.2.6.2 / §5.2.7, COBOLNET2423, `UnrepeatedElements`), the printable item's SIGN clause screened by §13.18.52.3 SR1/SR2 through the data description entry's own test (COBOLNET2422, kb/Work PB537) — **and REFERENCE-MODIFIED control operands** (§13.18.16.3 SR4 — the break is sensed on the slice, and the
TYPE CH/CF and SUM RESET ON operands that name the level carry the same ref-mod, §13.18.57.3 SR10 /
§13.18.54.3 SR8); **SUM with SUBSCRIPTED addends** (§13.18.54.3 SR5's identifier-1 is §8.4.3.1.2 Format 2's qualified-data-name-with-subscripts — a literal, index-name or expression subscript, bound in the procedure phase) + UPON (SR7-screened against the report-group funnel) + RESET, the repeated `SUM … UPON …` group into the ONE counter of the entry (SR1 / GR1 / GR7c2) and the `SUM OF` optional word; GROUP INDICATE; summary `GENERATE report-name`; multi-name INITIATE/TERMINATE;
**the four RWCS statement-precondition exception conditions** (EC-FLOW-REPORT §14.9.49.4 GR10,
EC-REPORT-ACTIVE §14.9.21.4 GR2, EC-REPORT-FILE-MODE §14.9.21.4 GR3 / §14.9.27.4 GR7, EC-REPORT-INACTIVE
§14.9.16.4 GR7 and §14.9.46.4 GR1 — the unsuccessful/state-unchanged half unconditional, the raise `>>TURN`-gated);
**SUPPRESS PRINTING** (§14.9.45 — inhibit the current instance's printing/advance/NEXT GROUP/LINE-COUNTER,
NOT the sum accumulation or end-of-group reset; SR1/GR1 bind-resolve the enclosing USE BEFORE REPORTING group,
COBOLNET1581 on a misplaced SUPPRESS); PD counter references incl. qualified; USE BEFORE REPORTING (Format 2
declaratives); unpaged reports;
**PRESENT WHEN Format 1** (§13.18.41 — any entry level, chain semantics per GR2b, the GR3a–g LINE/COLUMN/SUM
interactions, edition-gated 2002); **VARYING** (§13.18.64 — over any repetition vehicle,
counter-as-SOURCE, per-presentation FROM); **multiple + relative (PLUS) COLUMN** (§13.18.14 F1 incl. the
COL/COLS/COLUMNS/NUMBERS/ARE spellings and the GR7–GR9 horizontal counter); **REPETITION ON BOTH AXES**
(kb/Work PB565) — the §13.18.38 format 3 OCCURS clause horizontally (GR10a/GR10b, GR12a/GR12b) AND vertically
(GR10c/GR10d, GR12c/GR12d) with its SR10/SR16/SR24–SR27 syntax rules, and the **multiple LINE clause**
(§13.18.35.3 SR10, bound as §13.18.35.4 GR9's simple OCCURS, SR4 and SR10 a/b/c/d screened on COBOLNET2199) —
all three §13.15.4 GR3 vehicles live, edition-gated 2002.

**The RD CODE clause is LIVE (§13.18.12; kb/Work PB1129).** `ReportModel.Code` (`ReportCodeModel`: literal-1's characters, or
identifier-1 as the WRITTEN reference, kept the way the SOURCE clause keeps its identifier — a subscripted or
reference-modified identifier is live, its value bound in the procedure phase through the one sending-operand
resolution, kb/Work PB1292; a reference-modified one needs integer-literal bounds, CONFORMANCE.md §3) is registered on the engine with
`CobolReport.SetCode(Func<string>)`, and the engine prefixes every line it writes with the characters in force (GR1; the
compose returns the line alone, GR2, so `RECORD CONTAINS` is the code PLUS the line — `ReportModel.LineWidth` gives the
code's length up). The one evaluation is `CobolReport.EvaluateCode` (GR3 — "at the start of the processing for each body
group, either during page advance processing … or whenever page advance processing is not performed"): from
`AdvancePage` at §14.9.16.4 GR6 c) — after the page footing, which is therefore written with the OLD value — from
`PresentBody` for a body group that took no advance, and from the first GENERATE (whose report and page headings precede
its first body group). SR1/SR2/SR3 are COBOLNET2713 (`ResolveReportCode`, `ScreenReportCodeAgreement`); the clause is not
edition-gated, because the 2023 text does not say which edition introduced the identifier form and an unverified gate
would reject legal source (docs/CONFORMANCE.md A.4.11).

**SEVERAL REPORTS ON ONE FILE are LIVE (§13.18.46; kb/Work PB1050, PB1285).** An FD's `REPORTS ARE r1 r2 …` gives each report
its OWN `CobolReport` engine over the one file connector: each keeps its own LINE-COUNTER, page model and held-back line, and
the connector's one held-back line (`CobolFile.HoldLine` / `ClaimDevice`, kb/Work PB1247) makes the records reach the medium
in GENERATE order, so the codes of §13.18.12 separate the reports' records. `CLOSE` asks every associated report whether it is
still active (§14.9.6.4 GR5, the `||` over the file's reports in `SequentialIoEmitter`). The REPORT clause's three
correspondence rules — a name with no RD (§13.18.46.3 SR1), a name in two REPORT clauses (SR2) and an RD named by no clause
(§13.14.3 SR1) — are COBOLNET2714 (`ScreenReportClauseNames`, the file resolution in `ResolveReports`).

**Staged LOUD at bind (`COBOLNET0899`, Edition.Error — legal-but-unimplemented, never silent):**
**FUNCTION inside a PRESENT WHEN condition
(`report-condition-function` — the UDF activation-hoist is statement-context machinery)**; an
arithmetic-expression-1 SUM addend written with a LEADING PARENTHESIS
(`SUM (A * B)` — the lexer's §8.4.3.2.3 SR2 keyword-omitted-intrinsic trigger on the SUM token pushes
SUBSCRIPT mode at that `(`, against §13.18.54.3 SR9's "Otherwise, SUM refers to the report writer SUM
clause"; every OTHER spelling of the expression addend is LIVE, and §13.18.54.3 states no parenthesization
rule that would force the refused one).

**A report group item's USAGE is DISPLAY or NATIONAL, and a group entry's clause is inherited**
(kb/Work PB541). §13.18.60.3 SR7 — "Only the DISPLAY or NATIONAL phrase may be specified in any USAGE
clause associated with a report group item" — is ONE screen, `DataBinder.Reports.ScreenReportUsage`, asked
at the two points a usage becomes known: of the USAGE CLAUSE as it is captured (the rule's own subject, and
the only place a GROUP entry's clause is visible) and of the usage a printable item settles on when no
clause stated one (§13.18.60.4 GR7/GR8's implied usage). The entry scope stack carries the effective usage,
which is GR1's "applies only to each elementary item in the group"; SR2's "the same usage shall be
specified in both entries" rides the same code (COBOLNET2198). **PAGE-COUNTER is a receiving operand**
wherever §8.4.3.15.3 SR1 admits an integer data item — `Model.ReportPageCounterPlace` resolved at the one
receiving chokepoint, never a per-verb arm (kb/Work PB429) — and SR3's LINE-COUNTER prohibition is
COBOLNET2197 beside it.

**The REPORT SECTION entry grammar and the PAGE clause are the printed ones** (kb/Work PB1226, PB1059):
`reportDescriptionEntry` is `RD … { constantEntry | reportGroupEntry } …` (§13.8.2; the constant entry rides the
data description entry's own `constantEntryBody` and `DataBinder.BindConstantEntry`, reached in source order by
`BindReportSectionEntries` and bound earlier on demand when a reference needs it (kb/Work PB1231). A constant's
`LENGTH OF` / `BYTE-LENGTH OF` may name a report entry: `DeclareReportEntries` records every named entry before
binding, `ReportLengthOperand` refuses a report group (§13.10.3 SR11 — elementary report items only, kb/Work PB1226)
and measures an elementary one as its printable item; and `ScreenReportDescriptionHasGroup` reports an RD with no group entry, COBOLNET2708,
§13.8.4); the report group entry's name slot is the data description entry's `dataName` (FILLER included,
§13.18.20.3 SR3), read through `CstExtensions.NameOrNull`; and `reportPageClause` carries the page-width operand
(`integer-2 {COLS | COLUMNS}`, `ReportModel.PageWidth`, default 999 by §13.18.39.4 GR5 → `CobolReport`'s page
width, the one the `Place` truncation and EC-REPORT-PAGE-WIDTH measure; §13.18.14.3 SR6 screens each written COLUMN
operand against it, COBOLNET2710), `LAST {CONTROL HEADING | CH} IS integer-5` (`ReportModel.LastControlHeading`,
the control headings' lower limit, GR8 d)) and the `FIRST DE`/`LAST DE` synonyms; §13.18.39.3 SR3 (a phrase
without integer-1) is COBOLNET2709. The TYPE clause's control rows are `{CONTROL HEADING | CH} (reportControlName
(OR PAGE)?)?` and `{CONTROL FOOTING | CF} reportControlName?` (`reportControlName : (ON | FOR)? (FINAL | dataReference)`,
§13.18.57.2): `ReportGroupModel.OrPage` → the engine's `ReportGroup.OrPage`, and `CobolReport.AdvancePage(causing)` ends
with `PresentOrPageHeadings` (§13.18.57.4 GR6 c): every OR PAGE heading, major → minor, as the first body group of the
new page via `PresentBody(reprint: true)` — no page-fit test, NEXT GROUP or SUM reset; the page advance of a control
heading reprints only the headings above it, and the proviso for a control footing is applied as written). The GR7 d)
upper limits are realised by placement, not modelled as limits. **The COLUMN LEFT/CENTER/RIGHT alignment
phrase (§13.18.14 F1, kb/Work PB1220)** is `reportColumnClause : … (NUMBER | NUMBERS)? (LEFT | CENTER | RIGHT)? (IS | ARE)?
reportColumnOperand+` — optional in the grammar although the printed diagram shows a brace, because SR9 licenses its
omission ("LEFT is assumed") — with CENTER a context-sensitive token (§8.10; cobol-words.json). The word rides
`ReportColumnSpec.Alignment`, and ONE function, `ReportColumnSpec.AbsoluteLeftmost(printable-size)` (GR6 b)–d)), feeds both
the emitter's placement and GR9's horizontal counter (`AbsoluteRightmost`) AND the bind-time line-width walk; SR9's
absolute-only rule is COBOLNET2711, and an aligned item whose leftmost column falls before column 1 — a case the
standard states no outcome for — is COBOLNET2712 (CONFORMANCE §3). The phrase gates with the other 2002 COLUMN forms
(`report-multi-column-2002`). The §13.18.14.3 SR4/SR5 IS/ARE-spelling pairings are not enforced (over-acceptance).
**The ARRANGEMENT rules** (kb/Work PB1222, PB1270) have three homes, one per kind of fact:
- *Written clause* — `ScreenReportColumnClauses` (COBOLNET2745, once per written clause over the flat entry array):
  §13.18.14.3 SR10 a) (a multiple COLUMN clause and an OCCURS clause in one entry — the COLUMN arm of the two-arm rule
  whose LINE arm is `MultipleLineOperands`) and SR10 b) (integer-1 occurrences strictly increasing); no PRESENT WHEN
  excuse. `ScreenReportPageIntegers` (COBOLNET2744, in `BindReportDescriptionClauses`, over the WRITTEN integers
  before the GR3 defaults): §13.18.39.3 SR5 (page limit ≤ 9999) and SR6 (HEADING ≤ FIRST DETAIL ≤ LAST CONTROL HEADING
  ≤ LAST DETAIL ≤ FOOTING ≤ page limit, pairwise over the ones written).
- *One printable line* — `ScreenReportColumnArrangement` (COBOLNET2745), per `ReportLineModel` after the whole source
  element is described (the printable-size is a picture's), over `NominalPlacements` — THE one nominal horizontal walk,
  shared with the line-width computation: SR7 (absolute items by written column number, in order), SR8 a) (overlap),
  SR8 b) (an absolute item's rightmost column past the page width) and SR8 c) (a relative tail past the page width,
  judged whole unless `EachDifferentPresentWhen`, and then by the largest alone). SR7 and SR8 a) hold for
  items "each subject to a different PRESENT WHEN clause": `DifferentPresentWhen` — each carries a clause (its own or an
  ancestor entry's below the line entry, or a GROUP INDICATE) the other does not. Pairs are over WRITTEN entries
  (`ReportFieldModel.Entry`), so a repeating entry's repetitions are never compared with each other (SR26 owns them).
- *One report group* — `ScreenReportGroupLines` (COBOLNET2199), per group once it is complete: §13.18.35.3 SR6 a) (absolute
  lines in increasing order), b) (equal absolute lines overlap; a relative line of integer-2 zero is excepted by
  §13.18.35.4 GR3), c) (every absolute line within `GroupLimits`, the §13.18.57.4 GR7/GR8 limits) and e) (an absolute
  line described after a relative one), each excused only by different PRESENT WHEN clauses on the lines'
  chains (`ReportLineModel.Entry`, `PresentWhenCtxs`); d) (an unconditional absolute line, then relative lines carrying
  the last line past the lower limit — judged whole unless `EachDifferentPresentWhen`, then by the largest alone)
  through `MinimumLastLine`, the one placement walk the NEXT GROUP screen already read, asked for a chosen set of
  present lines. `GroupLimits` takes the WIDEST region each conditional limit allows (docs/CONFORMANCE.md, the
  report group limits determination), so the screen never refuses a line some presentation may place there. The
  RELATIVE halves of §13.18.39.4 GR1/GR2 are the same function's: a report or page heading, page footing or report
  footing of relative lines is refused when `MinimumLastLine` carries its last line past its lower limit (the page
  limit for the footings, GR2 a)) and when its candidate FIRST line — `MinimumLastLine` asked for that line alone,
  every line above it absent — falls above `RelativeFirstLineFloor` (HEADING for the headings, FOOTING + 1 for the
  footings, GR2 c)/g)); a report footing whose first line is the bare `ON NEXT PAGE` starts at HEADING (GR7 f).
- *The set of groups* — `ScreenReportGroupCensus` (COBOLNET2749), once every CH/CF has its control level:
  §13.18.57.3 SR13 (RH/PH/PF/RF at most once), SR14 (one CH and one CF per control level, counted per RESOLVED level so
  an omitted operand and a written one collide) and SR15 (at least one body group). The run-time `AddGroup` holds one
  slot per type and level, so an unscreened duplicate replaced the first group silently.
The run-time conditions the screens cannot decide stay EC-REPORT-COLUMN-OVERLAP/-LINE-OVERLAP/-PAGE-WIDTH/-PAGE-LIMIT
(default-off). EC-REPORT-* checking is default-off
(SSOT §18.16). The engine raises, each bound PRECISELY to GENERATE and TERMINATE (`EcBinder`'s report-production
names) and each resuming where §14.9.16.4 GR8 / §14.9.46.4 GR5 put it — "at the next report item, line, or report
group, whichever follows in logical order":
- EC-REPORT-SUM-SIZE (§13.18.54.4 GR3/GR4, fatal — kb/Work PB1130);
- EC-REPORT-LINE-OVERLAP (§13.18.35.4 GR3 — `PresentLine`; the overlapping LINE is abandoned, kb/Work PB1130);
- EC-REPORT-PAGE-LIMIT (§13.18.35.4 GR2 — `PresentLine`, a line of a paged report below the page limit; the rest
  of the GROUP is abandoned, its NEXT GROUP and end-of-group reset with it, kb/Work PB1188);
- EC-REPORT-COLUMN-OVERLAP (§13.18.14.4 GR4 — `CobolReport.Place` over the `ReportLineImage` occupancy; the
  overlapping ITEM is not placed, kb/Work PB1188);
- EC-REPORT-PAGE-WIDTH (§13.18.14.4 GR5 — `Place`; the line is truncated at the page width and printed, the
  outcome GR5 states, raised or not; the page width is the PAGE clause's integer-2 COLUMNS operand, else
  §13.18.39.4 GR5's default 999 — `ReportModel.PageWidth` → the `CobolReport` constructor, kb/Work PB1059).
- EC-REPORT-VARYING (§13.18.64.4 GR5, fatal — `CobolReport.VaryingInteger`, called by the compose of the line
  carrying the counter; a declarative with RESUME AT NEXT STATEMENT continues after the unsuccessful statement,
  and with checking off the counter takes the value's integer part, kb/Work PB1305).

## 6. Design authority (this doc + the cited GRs, not the legacy)

The implementation follows THIS doc + the cited GRs rather than a §-by-§ port of the legacy (whose
report-file content is wrong — §7). The SSOT (§14 verb table, §15.5) records RW's locked scope and its §0.5
deep-dive table points here.

## 7. Hazards & oracle holes (validated)

1. **The legacy report-file content is NOT a content reference.** Two proven bugs: RW102A's
   `PIC 9(3) SOURCE IS WS-COUNTER` (a `PIC 9(6)` holding 1) printed `000` — a raw left-justified byte copy
   truncated to 3 — where §13.18.53.4 GR1's implicit MOVE yields `001`; and `SOURCE IS LINE-COUNTER` printed
   blank. Only the CCVS print file is golden-compared; the conformance net pins the spec content.
2. **Registration order** (§4): report FDs must register with the connectors, before any engine write.
3. **GR6 ordering**: `PresentLine` is the single method that sets LINE-COUNTER before composing — do not
   per-group reorder.
4. **First-GENERATE exemption** (§13.18.35.4 GR4): no page-fit for the chronologically first body group —
   RW101A's first detail lands at line 1, never triggers an advance.
5. **Trial-sum reading** (§2): Σ over ALL relative lines; the other reading shifts every page boundary.
6. **`\f` and the print stream**: a page advance appends a form feed to the stream after the page's last
   record; read-back style tests must keep markers clear of it (`String.ReplaceLineEndings` treats FF as a
   line ending).

## 8. Verification

- NIST: RW101A, RW102A, RW103A, RW104A GREEN (byte-match, `--std 85 --nist`) — counters (INITIATE values,
  per-GENERATE LINE-COUNTER, page-advance PAGE-COUNTER/FIRST-DETAIL placement over 3-page runs).
- `tests/Cobol.Net.Tests.Conformance/ReportWriterConformanceTests.cs` (15 spec-pinned tests): INITIATE GR1;
  GR5b3 first-body placement; GR4c trial-sum overflow; the §13.18.53.4 GR1 content pins (the two legacy
  bugs); PH composes its own line (GR6); RH-once/PH-per-page (GR4a/GR6f); PF at advance + final-page PF→RF
  (GR6a/GR6f/GR3c); TERMINATE-without-GENERATE (GR2); control-break prior/new values (GR4a); SUM
  accumulate/reset/UPON (GR2/GR7); USE BEFORE REPORTING (GR8); LINE-COUNTER receiving rejection (SR3);
  the §13.18.41.4 GR3g absent-SUM neither-prints-nor-resets pin.
- **⛔ EVERY corpus golden and unit fact below that observes report CONTENT lives at `--std 2023`, and the reason
  is not the Report Writer** (an 85 subsystem): the only way to read a report file back is a second SELECT with
  `ORGANIZATION LINE SEQUENTIAL`, a COBOL-2023 introduction (§12.4.5.10.3 GR2; kb/Work PB688), and a WiseOwl COBOL
  report file is CRLF-delimited text whatever its own ORGANIZATION, so a record-sequential read-back of it is
  misaligned. `ReportWriterConformanceTests` compiles whole at 2023 for that reason, and the nine 2002 goldens
  moved to `tests/conformance/2023/`. The ONE golden that stays at 85 —
  `85/pb326_flow_report_unconditional`, whose subject IS the oldest edition — dropped its read-back instead:
  §14.9.49.4 GR8/GR9d run the USE BEFORE REPORTING declarative once per PRESENTATION, so its `WS-N` already
  counts the detail lines the read-back was re-counting.
- Corpus goldens for the four statement-precondition conditions (kb/Work PB326), all byte-exact and verified by
  RUNNING: `85/pb326_flow_report_unconditional` (the §14.9.49.4 GR10 *unsuccessful / state unchanged* half at the
  edition that has no `>>TURN` at all — the nested GENERATE produces nothing); `2023/pb326_ec_flow_report` (its
  checked twin — the F3 declarative sees EC-FLOW-REPORT and RESUMEs); `2023/pb326_flow_report_cross_report` (the
  range is the RUN UNIT's — a GENERATE of a SECOND report from inside report R-A's declarative is refused, and
  the same GENERATE succeeds once MAIN has left the range); `2023/pb326_ec_report_file_mode` (two arms — not
  open, and the two modes §14.9.21.4 GR3 permits; an INPUT or I-O open of a report file is not writable source,
  §14.9.27.3 SR1 → COBOLNET2371, kb/Work PB318 — with the follow-on EC-REPORT-INACTIVE proving that
  GR3's "no action is taken" left the report inactive); `2023/pb326_ec_report_active_inactive` (GR2's "no other
  effect" witnessed by a SUM counter that keeps its total across a refused second INITIATE).
- Corpus golden `tests/conformance/2023/rw_present_when.cob` (+`.out`, byte-exact, verified by RUNNING): the
  PRESENT WHEN line collapse across a present and an absent presentation (the relative TAIL line re-anchors on
  LINE-COUNTER, §13.18.41.4 GR2b), field-level suppression within a present line, and `COLUMNS ARE 12 16 20 …
  VARYING RV-IDX FROM WS-SEQ BY 2` with the counter as SOURCE (per-presentation FROM). Negatives:
  `present-when-at-85` (0900), `report-varying-no-repetition` (1559 §13.18.64.3 SR1),
  `present-when-group-indicate` (1559 §13.15.3 SR17). Matrix rows `report-present-when-2002` /
  `report-varying-2002` / `report-multi-column-2002` ACTIVE (+ `report-multi-line-2002` pending).
