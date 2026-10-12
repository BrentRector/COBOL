## Entry 1656 — 2026-09-22 23:53 PDT — Landing train 52: wave 52 (DA–DG) and PB985, eight clusters, eighteen notes, GAP −16 to 2114

Train 52 was pipelined behind train 51 (workstream SKILL.md "Lander throughput"). When the lander started, train 51
was already on main, so it did not wait at all (0 minutes). It merged and gated on `57d36e4c6`. Before push-main,
train 50 and the ledger refresh (`acff7ee45`) had also landed. The train rebased onto them. The rebase had CODE
conflicts, so the whole local gate ran again on the rebased tree. A later rebase over the docs-only ledger commit
`e143cb07b` was clean, and later docs-only commits took the entries below this one, so this entry is 1656.

**PB985 — the Conformance runner's compiled-program cache (landed first).**
- One gateway for every compiler read outside the source text and options:
  `src/Cobol.Net.Frontend/Pipeline/CompilationInputs.cs`. It covers copybook reads and probes (misses included), the
  NIST copylib probe, `>>DEFINE … PARAMETER` environment reads (§7.3.11.4 GR4) and the WHEN-COMPILED clock
  (§15.99.3 GR2). `CompilerDriver.Result` now carries `Inputs` and `OutputFiles`.
- The test-side `CompiledProgramCache` keys on:
  - the compiler's reference-closure hash;
  - every `Options` property;
  - the source path and content;
  - the input record, which is re-checked on every hit.
- The store is per worktree and git-ignored (`.cache/compiled-programs`). It is OFF under `CI` and in `battery.sh`.
- `EnableSourceControlManagerQueries=false` (Debug) stops the SDK from stamping the commit sha into every assembly.
  That stamp was changing every compiler assembly on every commit.
- **Measured in this train:** the cold whole Conformance run took 20m14s. The warm re-gate after a test-only fixture
  fix took **1m43s** for 8,155 cases.

**DA — PB970 arm 1 (half) + PB968 (discharged).** The ONE rule: at the CALL and INVOKE boundary, a numeric string
carrier is the item's STORAGE image (§14.2.3 GR8/GR9).
- Before, the native side sent storage while the image-carried side sent operand text. The probe
  `PK AFTER 02303` now prints `-00043`.
- Changed: `CallEmitter.IsImageCarriedNumeric` (one predicate); `CallAbi`'s landing, argument and RETURNING lanes
  now use ParseImage/FormatImage.
- The INVOKE arm now crosses through the storage image. It also fixes a CS0103: BY CONTENT into an image-carried
  formal had used the bare callee profile.
- The inherited "according to the rules of the COMPUTE statement" quote failed `cite.py`. It was re-derived to
  §14.8.2.3.3 2) a).
- PB968 was already fixed on main (wave 50 BA, `6ec26b4ba`). It was re-probed and discharged.
- Arm 2 (the §14.8.2.3.3 1) length rule) stays NEXT.
- The branch carried merges of wave 50 BA/BC, which landed in train 49. So only `bb71e3626..8b79a0bd0` was brought in.

**DB — PB210 + PB211 + PB212.** Three class-closed identifier positions had only a bare resolve, with no screen:
- GO TO DEPENDING (§14.9.17.3 SR1): a `PIC 9V9` of 2.7 silently took branch 2.
- SEARCH VARYING (§14.9.37.3 SR5, both sentences).
- SET Format 1 (§14.9.39.3 SR1–SR4).
The fix makes each a row of ONE operand-class screen (`OperandClassScreen` / `OperandPositions`), with codes
COBOLNET2324–2326. The sibling sweep added the PERFORM VARYING induction variable (§14.9.28.3 SR2 / SR5 a),
COBOLNET2120). SEARCH ALL SR10's private `UsesIndex` is deleted: `ReferenceResolver.SubscriptNamesIndex` is the one
reader. Fixtures written in SR3/SR4-forbidden SET source were re-pointed.

**DC — PB215 (+ PB213, PB214 closed).** One `OperandContext` member served sites that disagree on §13.18.60.3 SR10.
It is now two rows:
- `ArithmeticIndexWindow`: SET, SEARCH, relation.
- `ArithmeticIndexNameWindow`: subscript segment, PERFORM VARYING FROM/BY.
An index data item now draws COBOLNET0844 in the subscript and in PERFORM VARYING FROM/BY. The Report Writer VARYING
draws 0844 for an index data item and 1637 for an index-name. PB213 and PB214 were already fixed (`1884ba1ea`); only
their status fields were stale.

**DD — PB239 + PB234 + PB240.**
- **PB239:** `CALL … USING ADDRESS OF x` was a parse error. One grammar rule, `addressIdentifier`, now serves every
  CALL argument phrase and the SET Format-7 sender. `DataBinder.Ptr`'s storage-forcing scan walks every
  `DataAddressIdentifierContext`.
- **PB234:** ADDRESS OF a RECURSIVE unit's static WS item was refused with 0899. Its cell now emits static
  (`StaticAddressableCells`, §13.5.4 GR1) and is re-seeded in place by `__ResetStatics`.
- **PB240:**
  - Adds the REDEFINES-view arm of the bit-alignment screen (COBOLNET1683).
  - The ANY LENGTH screen now covers SR11 and SR18.
  - CALL BY REFERENCE now relaxes ANY LENGTH the way INVOKE always did.
  - `CancelCascadeOrderTests` pins the §14.9.5.4 GR4 order.

**DE — PB956 + PB521 + PB946 + PB187.**
- **PB956:** BASED data and ADDRESS OF in a class definition are now implemented. They used to get a whole-unit 0899
  (`OoBasedInClass`, now deleted).
  - The pre-scan walks the method bodies.
  - One `OoEmitter.EmitPointerBackings` renders the program path and both type-halves.
  - Storage duration follows §8.6.4. The inherited "§14.5.3" (13×) was corrected to §8.6.4.
- **PB521:** §13.16.3 SR21, COBOLNET2333.
- **PB946:** a repeated IMPLEMENTS name is absorbed; no rule forbids it.
- **PB187:** windowed float crossings no longer fail with a backend CS1503.

**DF — PB202 + PB189 (PB244 left open).**
- **PB202:** `PlaceRenderer.UsesCurrentExtent(odo, dir)` is now the only §13.18.38.4 GR8 direction test. The ref-mod
  READ uses the sending view; the RECEIVING splice uses the receiving view.
- **PB189:** `Place.ImageCapable` / `BoundaryImageCapable` replace the item-level guard. One occurrence of a DYNAMIC
  table now displays, moves, ref-mods and is passed to CALL.

**DG — PB835.** NUMVAL-C locale grouping follows mon_grouping (§15.68.3 SR5 r6). One size-list reader
(`MonetaryFacts.GroupSize` / `GroupingAdmits` / `GroupingIsExact`) serves both the scan and the §14.6.13.2 r4
format-2 de-edit, which used to accept separators anywhere. The determination is recorded in CONFORMANCE.md A.4.9.

**The train — merge and composition findings.**
- **DB × DC, one arm fixed twice.** Both clusters fixed the PERFORM VARYING induction variable: DB as an operand-class
  row (COBOLNET2120), DC through `ScreenResultant` (COBOLNET1675).
  - The lander kept DB's screen, because it also serves SR5 a).
  - The two `negative/pb215-perform-varying-target-*` goldens were retargeted to 2120, and DC's addition to 1675's
    description was reverted.
  - Both verdict batches recorded SR-14.9.28.3-2, so `record_verdicts` refused the pair. The train applied merged copies
    (`t52/pb210-212-batch.train.json`, `t52/PB215-batch.train.json`). Recorded in `kb/Work/PB215`.
- **DD × DE, one new member declared twice.** Both introduced `StaticAddressableCells`. One declaration now serves both
  (`DataBinder.Oo`). `DataBinder.Ptr` keeps DE's per-method scan over DD's `DataAddressIdentifierContext` walk. The
  plan §8 ledger records both discharges.
- **DA × DE × main's PB186 drift.** DA and DE each added a private native↔storage-image pair. DA had `ValueImage` /
  `ImageValue`; DE had `OoNativeToImage` / `OoImageToNative` plus a float arm in `PlaceRenderer`. Train 50's
  `SortNumericKeyLaneTests.OnlyTheNamedSites_DecodeANumericImage` names `NumericRenderer` as the decode site, and it
  went red on all three. They are now ONE pair, `NumericRenderer.CarrierOfImage` / `ImageOfCarrier`, and both design
  docs name it.
- **Main's PB961 signature change.** Train 50 made `binary32` a required argument of
  `NumFormatImageFloat` / `NumParseImageFloat`. DA's and DE's new sites now state it: the item's `IsSingle`, a copy
  lane.
- **DD × DA, the ABI change.** DD's new test probe implemented the old `ICobolProgram.Call(…, ManagedPointer?)`.
  DA's ABI is `CobolArg?`.
- **DB and main.** DB's diagnostics spell operands with main's `DataBinder.WrittenText`. SEARCH VARYING resolves
  through main's receiving chokepoint (`ResolveReceiving`) before DB's screen.
- **DF and main.** DF's national ODO stride fix in `OdoModel.WrapGroup` had already landed on main as PB943, and
  main's version was kept.
- **A fixture DB's filtered gate did not select.** `SetLocaleDispositionTests.At85_LocaleIsAUserWord_SetFormat1Runs`
  used `SET LOCALE TO 5` over a numeric item. §14.9.39.3 SR4 forbids that, and COBOLNET2326 now refuses it. The fixture
  was re-pointed to index-name senders, and its output is unchanged ("77").
- One duplicate `using` in `SearchBinder`.
- The manifest resolutions took both sides, element-wise. After each one, every `enabled` list was checked for
  duplicates, missing files and unlisted goldens.
- The conflict-marker checks were clean at every checkpoint.

A consequence of the composition fixes: they live in the train's last commit. The DA and DE cluster commits therefore
do not build on their own on the rebased base, because main's PB961 changed the float API. The train head builds and
is gated.

**The gates.** Filter `FullyQualifiedName~CobolNet.Tests`, the WHOLE Conformance assembly:
- Pre-rebase, after the fixture fix: `Passed! 8155` Conformance, `Passed! 28882` Unit, `Passed! 33` Characterization.
  The legacy Integration assembly gave `Passed! 503, Skipped: 1`.
- **On the rebased head:** `Passed! - Failed: 0, Passed: 8206` Conformance, `Passed! - Failed: 0, Passed: 28927`
  Unit, `Passed! 33` Characterization. Integration gave `Passed! 503, Skipped: 1`.
- The GnuCOBOL corpus fetch succeeded.
- The citation, doc-citation, supersession and witness-loss audits were GREEN (witness loss: 0 unexcused, 1 retired,
  9 re-sited).
- `work.py check` passed (1,065 items).
- Semgrep: `cobolnet-raw-diagnostic-code-literal` went 376 → 375, and the baseline was locked.
- DIAGNOSTICS.md was regenerated and matched.
- The watched `EcCheckingProfileDriftTests` red from DA's first gate did not recur.

**GAP 2130 → 2114** (−16; the same rows as the −16 measured on train 51's inventory). No cluster was dropped.

**The first CI run was RED** (run `35826309450`). One test failed in the Linux `rest` Conformance shard, and every
other job was green:
- The test: `CompiledProgramCacheDriftTests.ADebugBuild_DoesNotStampTheCommit`.
- The message: "Cobol.Net.Compiler carries a source-revision stamp (1.0.0+6434c9a05c16…)".
- The cause is in PB985's own `Directory.Build.props`, which keyed the stamp-free Debug build on
  `'$(Configuration)' == 'Debug'`. That file is imported BEFORE `Microsoft.Common.props` defaults Configuration. So a
  project built on its own without `-c` sees `''`, and every CI build step builds a `.csproj` exactly that way. Only a
  solution build passes `Configuration=Debug` as a global property, which is why the lander's gate (`build-local.ps1`
  builds the `.sln`) was green.
- Attribution: cluster PB985. Nothing else in the train was involved.
- The fix: the condition also admits `''`. Release is always explicit (`-c Release`), so it keeps the stamp.
- Proof, with a project-alone build as CI does it: without the fix the test fails locally with the same message; with
  the fix all 20 `CompiledProgramCacheDriftTests` pass.
- It lands as its own commit, `PB985: an empty Configuration is Debug in Directory.Build.props`, just before the
  train commit. The landing is the second run.

The implementer worktrees can be removed:
- `wf_57e843b7-427-2`
- `wf_31320d12-51e-1` through `-7`

