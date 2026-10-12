## Entry 734 — 2026-07-09 22:37 PDT — PHASE-03 Step 14g.2 review-fix: TYPEDEF → parse-arm (not bound-arm) + a level-66/88 guard

An adversarial find→verify review of the 14g.2 commit (`2efa4ea`, one subagent attacking H1–H5 over/under-fire
hypotheses) found **3 CONFIRMED byte-neutrality violations** — all low-severity (secondary 0900s on already-failing
`--std < 2002` programs, no real/NIST program hits them) but genuine, and NOT covered by any test. **Fixed at the root
cause this commit; all three now have regression witnesses.**

**The finding + the design correction.** The 14g.2 plan put TYPEDEF in the BOUND-arm on the premise "`DataItem.IsTypedef`
is init-only and survives declaration errors." True at the FLAG level — but the review proved it false at the
ENUMERATION level: the typedef ITEM is discarded from `ConformanceForest()` whenever `RegisterTypeDecl` rejects it (an
unnamed/FILLER typedef `return`s at DataBinder.cs:381; a duplicate type-name fails `TryAdd` at :386 — neither lands in
`TypeDecls` nor `Roots`), or when a misplaced subordinate typedef binds into a method's `LocalRoots`/`StaticRoots`
(off the forest `Run` walks). So the bound-arm silently dropped the 0900 on exactly those paths — the SAME
DEVLOG-724 bug class the whole 14h/14g effort exists to kill. **Root cause: TYPEDEF is a pure syntactic clause that
needs NO resolved fact, so it belongs on the parse-arm (recognition), not the resolved-fact bound-arm.** The plan
mis-classified it; corrected here.

- **DEFECT 1 (under-fire):** `01 TYPEDEF PIC X.` (FILLER typedef) at 85 — OLD `{0900, 1529}`, NEW-bound-arm `{1529}`.
- **DEFECT 2 (under-fire):** two same-named `01 T TYPEDEF …` at 85 — OLD two 0900, NEW-bound-arm one (the sink keeps
  duplicates; the second item was dropped). The most plausible real input (a duplicate-name authoring slip).
- **DEFECT 3 (over-fire):** a data clause mis-attached to a level-88 entry (`88 C TYPE IS FOO.`, which the permissive
  grammar admits — `dataDescriptionEntry : levelNumber dataName? dataDescriptionBody`, and a level-88 body is
  `dataDescriptionClauses`) — OLD no 0900 (`BindEntries` intercepts `lvl is 66 or 88` BEFORE the clause loop),
  NEW-parse-arm a spurious 0900.

**The fix.** All four data-description-clause gates (BASED/TYPE/PROPERTY/TYPEDEF) now gate uniformly on the parse-arm —
`GateData` reverts to USAGE-only; a new `ParseArm.VisitTypedefClause` joins the three siblings — each guarded by
`InConditionOrRenamesEntry` (walk to the enclosing `dataDescriptionEntry`, skip level 66/88, mirroring the binder's
skip). Recognition fires on the always-present parse node, so DEFECTS 1–2 name their edition regardless of the item's
registration fate, and the guard closes DEFECT 3. 3 refuted hypotheses stood: H2 (`CloneItem` never copies `IsTypedef`
+ clones carry `TypeAnchor`), H4 (ParseArm walks the full unit incl. methods/classes), H5 (ids + where-strings
byte-identical).

`DataClauseEditionTests` +3 regressions (`FillerTypedef…`/`DuplicateTypedefName…`/`ClauseUnderLevel88…`), 9 total.
Byte-neutral STILL holds (characterization 32 byte-exact — the content-sorted diag surface is blind to the arm change;
`char_neg_typedef85` unchanged). Battery: greenfield conformance **3131** (+9) · unit **223** · characterization **32** ·
INV-1-strong @ 2023 permissive **349/349** · FULL legacy guard **NIST 353 MATCH / 0 regressions** (legacy unit 1196 ·
integration 608). RESUME AT Step 14g.3.

