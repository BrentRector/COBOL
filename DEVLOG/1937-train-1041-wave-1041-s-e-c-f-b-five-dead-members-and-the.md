## Entry 1937 — 2026-10-08 06:34 PDT — Train 1041: wave 1041's E, C, F, B — five dead members and the dead SourceText class deleted under the Delete program; D dropped at the landing check (PB2179, PB2168, PB2187, PB2189, PB2190, PB1821 landed; PB2678, PB2679 filed)

**E — PB2179 (landed; census R0-0141).** `NameAllocator.NextVary` and its `_vary` field had no caller since PB459
(`10f2a6be6`) moved PERFORM VARYING's FROM landing onto `SetEmitter.LandAmount(..., "pv")`, which mints its id from
`NextSet()`. Both are deleted. The re-probe held on today's tree, and the implementer's sweep of the other 16 counters
found a caller for each. **Train review:** the `_set` field's comment, which the implementer had corrected to name
`__pv`, still left out the `__amt` and `__sz` temporaries the same sequence mints. It now lists all of them.

**C — PB2168 (landed; census R0-0130).** The private `IntrinsicResultType.CategoryOf(args, i)` had no caller and is
deleted. Its summary was also wrong: it called a group and an ALL literal category-less, where `OperandCategory`
answers alphanumeric or the literal's category. The `UniformArgumentType` comment that still named it now names
`OperandCategory`, the reader actually consulted. Every other member of the class has a caller.

**F — PB2187 (landed; census R0-0149).** `SourceLineMap.Count` had no reader and is deleted. `_origins.Length` is
still read by `Origin`. Every other member of `SourceLineMap.cs` has a caller.

**B — PB2189, PB2190, PB1821 (landed; census R0-0151, R0-0152).** The census measured 4 dead members of
`CobolNet.Frontend.Common.SourceText` and 1 of `TextSpan`. But the whole `SourceText` class has had no consumer since
the legacy unit project went (PB2110): the rest was test-only at a02b165da (R0-0081, R0-0198), and the 8be230068
census marks the type unreachable. `Common/SourceText.cs` is deleted whole. That also lands PB1821, the same file's
second line model with a lone-CR line end, which contradicted the one `PhysicalLines` stage. `TextSpan` loses
`OverlapsWith` and its siblings `FromBounds` and `Contains` (R0-0082); `End` stays because `ToString` reads it.
`docs/COBOLNET_DESIGN.md` and `docs/COBOLNET_PROJECT_ORG_DESIGN.md` §1.2 now list the current `Common/` (7 files)
and `Diagnostics/` (5 files). **Train review:** the implementer's lead that R0-0081, R0-0082 and R0-0198 "never got
notes" is by design, not a register gap. DESIGN-architecture-review §8.7 leaves all 67 test-only findings to R2's
Opus reviewer. Its "Not yet notes" row now names these three as done under rule (c).

**D — PB2169 (DROPPED at the landing check; PB2679 filed).** D deletes the uncalled `IntrinsicSig.ArgKind(int)`
(census R0-0131) and corrects every comment that narrated it. The train review found three more stale comments in
`IntrinsicBinder`, all fixed in D's commit: the `CheckArgumentClasses` summary said it screens against `ArgKinds`
(it screens against the Verified schema), an orphaned second `<summary>` sat on the same method, and "an empty
ArgKinds defaults to 'n'" described the deleted accessor's default as current. The five-cluster train was GREEN on
every gate. But `push-main.sh` stopped at the landing check (exit 3, main untouched) with six stops, all D's.
`IntrinsicArgumentClassDriftTests.cs` lies outside D's declared file set. `IntrinsicBinder.cs`,
`IntrinsicArgumentRules.cs` and `DiagnosticCatalog.cs` are shared with earlier dispatches still in flight: w1039B
(PB1722 open), w1039F (PB2496, PB2497 open) and three `whand` entries. D is the later branch, so it waits. The lander
dropped D and rebuilt the train from E, C, F and B; the check then passed. D, with the review's fixes, is preserved on
branch `train1041-dropped-d-pb2169` (`1c42e5e09`). PB2169 stays open. PB2679 records the mechanism: a member with no
callers declares only its own file, but the Delete contract's comment and drift-test sweep edits files that name it
as text. It also records the re-land condition.

**PB2678 (filed).** Every spec of wave 1041 named two sites that no note declares:
`scripts/arch/compare_oracle.py` and a git-ignored BenchmarkDotNet results `.md` whose stem starts `CobolNet.`.
`fix_clusters.source_index` indexes ignored build output under the campaign roots, so each `CobolNet.<Ns>.<Type>` in
a census note resolves through the `Type.Member` arm to that file. The census wave-contract boilerplate's oracle
script also scores as a site of every R3 note. Fix shape: index the tracked tree only, and read a note's **Sites**
block when it has one.

**The train.** Five clusters dispatched, four landed, one dropped (D, at the landing check); none ejected. Gating was
batched: every implementer gated leg 1 only. All five branches applied cleanly to 440b526a3, which stayed origin/main
throughout, so no rebase was needed. Whole-population runs: 3. The first ran on the five as brought in. The second
ran on the five after the review's comment and doc fixes. Both were GREEN, Conformance 11,234/11,234 · Unit
32,678/32,678 · Characterization 36/36, and the Linux gate was GREEN on that head. The third ran on the four after D
was dropped, followed by the Linux gate again. Review: 4 findings, 4 fixed in the train (D's three leave with D), 0
clusters dropped by review. Semgrep verify PASS with no count up. No diagnostic codes claimed: the ranges
COBOLNET3173–3187 are returned. No traceability rows moved: inventory GAP 136 → 136. Oracle `=== ARCH-ORACLE:
IDENTICAL — 7614 cases (baseline e2c463c4bbbe)`, as the Delete program's contract requires; re-recorded as
23f3eb1efbe5.

