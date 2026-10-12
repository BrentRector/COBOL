## Entry 1936 — 2026-10-08 05:25 PDT — Train 1040: wave 1040's W, U, S, N — one FROM-phrase rule and edition gate, the run unit's stack and its resource check, unbound and omitted formals at their category default; tooling notes (PB2650, PB2659, PB2671 landed; PB2660, PB2665, PB2672 filed)

**W — PB2650 (landed) and golden lane gn1's close.** WRITE and REWRITE `FROM literal-1` compiled clean at `--std 85`
while RELEASE `FROM literal-1` was refused. X3.23-1985 prints `FROM identifier-1` for all three statements, and
ISO/IEC 1989:2023 prints `FROM { identifier-1 | literal-1 }` (§14.9.51.2, §14.9.35.2, §14.9.32.2), each the same
implicit `MOVE literal-1 TO record-name-1` (§14.9.51.4 GR5 a), §14.9.35.4 GR7 a), §14.9.32.4 GR4 a)). The 1985 edge is
a derived determination (VCR row 7.34). The mechanism was three grammar copies of `FROM sendingOperand` with the gate on
RELEASE's alone. `CobolIO.g4` now has ONE rule, `recordFromPhrase`, and one gate,
`VersionConformancePass.ParseArm.VisitRecordFromPhrase` (construct `release-from-literal-2002` renamed
`from-literal-2002`, COBOLNET0871), so a statement that names the rule is gated by construction. Goldens:
`2002/pb2650_from_literal` and three `pb2650-*-from-literal-85` negatives; four 85 sources that relied on the
under-rejection now write FROM an identifier, outputs unchanged. Golden lane gn1's three goldens
(`2002/gn1_lock_mode_omitted_implementor_default`, `negative/gn1-data-address-receiving-{set-up-by,initialize}`) close
SR-8.4.3.11.3-5 and GR-12.4.5.9.4-1; their generic codes stay kb/Work PB1547. Rows: 6 CONFORMS, 3 orphaned
code-locations retired; GAP 144 → 142.

**U — PB2659 (landed); PB2660 (filed).** A RECURSIVE CALL chain of about 250 activations killed the process with a CLR
stack overflow: no exception condition, no §14.6.11 CLOSE. `Runtime/Control/ActivationStack.cs` now runs the run unit's
main program on a 384 MiB thread (sized from measurement to reach GnuCOBOL 3.2's 74,687-activation depth under full
checking), and `IsAvailable` is the one resource check of §14.9.4.4 GR3c (EC-PROGRAM-RESOURCES, CALL and function
through `ProgramTable.CallProgram`) and §14.9.23.4 GR7b (EC-OO-METHOD, the emitted method prologue); DOC-A.1-14, 89 and
102 are re-determined. A second defect was fixed on the way: the GR3i boundary mark was a catch-and-rethrow, which
stacked one nested dispatch per boundary and overflowed again while unwinding; it is an exception filter now. Goldens:
`2002/pb2659_call_resources_recursion`, `2023/doc_a1_132_module_name_stack_deep` (68201 at depth 2200), two negatives,
`ActivationStackTests` and `ActivationResourcesTests`. Rows: 5 CONFORMS; GAP 142 → 141. PB2660 filed: cross-run-unit
RELATIVE/INDEXED sharing under ALL OTHER loses updates and ignores record locks. **Train review:** the method frame push
had lost an emitted space (`var __ms =CobolModule.Stack`); restored in U's commit.

**S — PB2671 (landed).** Host tests for Annex A.1 items 116, 118 and 120 and §13.7.4 GR3/GR5 showed DOC-A.1-116 false
at the main-program entry (`ICobolProgram.Activate`): a group formal failed with EC-DATA-PTR-NULL and binary or packed
formals held a space image. The refuter's two rounds showed the first fix keyed the image on the item rather than its
storage (an addressed `PIC N(2)` died with EC-BOUND-PTR) and that the determination's comparison with a
WORKING-STORAGE item was false under `OPTIONS. INITIALIZE WORKING-STORAGE SECTION TO HIGH-VALUES`; the branch head
carries the corrected wording (independent of any OPTIONS INITIALIZE clause), checked here. An unbound or omitted
formal now reads its category default in a storage-shaped image (`DataEmitter.UnboundFormalRecord` /
`UnboundFormalCarrier` / `AreaFormalShape`, `CobolArgAdapt.Unbound` / `OmittedText`) on the program and method ABIs.
Goldens: `NonCobolActivatorLinkageTests`, `2002/pb2671_omitted_formal_category_default`,
`DefaultLocaleDeterminationTests`. Rows: 5 CONFORMS (GR-13.7.4-3, GR-13.7.4-5 and DOC-A.1-120 re-adjudicated from
PARTIAL); GAP 141 → 136. **Train review:** the train's first whole-population run went red on
`DeepNestingTests.DeeplyNestedGroup_EmissionWorkIsLinearInDepth` ("The collection contained 3 items"): `ProgramEmitter`
built two more `DataEmitter`s, each with its own `PhysicalModel`, per program class for the unbound seeds. S alone
reproduced it; W and U alone passed. Fixed in S's commit: the class's one `DataEmitter` composes the seeds. Also a
doubled blank line in `RuntimeApi.cs`.

**N — notes (PB2665, PB2672 filed; PB2609, PB2426 records).** PB2665: the resume/meter livelock behind a lander's kept
lease. PB2672: a landed train's members have no LANDED path in `prune_worktrees.py`. PB2609's resolution time, and the
PB2426 ledger trend points through c799a62b1.

**The train.** Four clusters, none dropped or ejected; batched gating (every implementer gated leg 1 only). Whole-population
gate runs: 2. The first was RED on the one DeepNesting case (attributed to S in about 3 minutes by three per-cluster
runs, fixed in the train); the second GREEN: `=== BUILD-LOCAL GATE: GREEN — Conformance 11,234/11,234 · Unit
32,679/32,679 · Characterization 36/36`. The Linux gate's legs all passed on the gated head (unit 32,710, characterization
36, conformance 11,234, guard NIST 362 MATCH); its repository check went red only because this lander rewrote the
branch (the fixup fold) while the gate ran, so it was re-run on the final head. The verdict batches were re-applied with
`record_verdicts.py` on the merged tree, never merged as JSON; the negative and 2002 manifests' conflicts were whole
list elements (2,559 and 793 entries after). Review: 3 findings, 3 fixed in the train, 0 clusters dropped. No
diagnostic codes claimed. Inventory GAP 144 → 136. Oracle DIFFERENT, 425 of 7,619 cases, every one explained by
class: 17 ADDED goldens and matrix cells and 5 REMOVED matrix cells (the `release-from-literal-2002` →
`from-literal-2002` rename, W; the new goldens of W, U and S); 1 DIAGNOSTICS (`negative/pb854-eop-use-name-below-2002`:
its two diagnostics move down two lines because W edited its source); CSHARP 402: 186 programs with a LINKAGE formal
take the category-default seed, the `unbound` factory argument of `CobolArgAdapt.Text`/`TextValue`, or the
`ICobolProgram.Activate` block (S); 159 gain the method prologue `ActivationStack.RequireForMethod` or
`EC-PROGRAM-RESOURCES` in a CALL's catch filter under EC checking (U); 54 both (S and U); 3 are W's edited 85 sources.
Re-recorded as `arch-oracle/e2c463c4bbbe`.

