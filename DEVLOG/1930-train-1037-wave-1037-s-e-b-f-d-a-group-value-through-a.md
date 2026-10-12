## Entry 1930 — 2026-10-07 22:33 PDT — Train 1037: wave 1037's E, B, F, D, A — group VALUE through a strong TYPE, the RD level hierarchy, REDEFINES in templates and SR10, mnemonic-names are words, run-time CALL conformance (PB2517, PB2516, PB1954, PB2071, PB2518, PB2499, PB165)

**E — PB2517 (landed).** A group VALUE that an entry only ASSUMED from a type declaration was screened where the
clause was written, for every predicate. That holds for §13.18.63.3 SR13 and SR14 (properties of the entry that wrote
the clause) and not for SR1 ("The subject of the entry shall not be a strongly-typed group item"), which is a property
of what the subject IS: a TYPEDEF STRONG template's group is not a typed item (§8.5.3.1), and `01 V TYPE U.` is what
makes it one. Re-probe: `01 U IS TYPEDEF STRONG. 05 G VALUE "AB". 10 A. 10 B. 01 V TYPE U.` compiled clean at 2002
while the inline spelling drew COBOLNET1703. Fix: `DataItem.ValueIsCopied` (a flag) became `ValueCopiedFrom` (the
source entry), every reader changed and the flag deleted, and `ScreenAssumedGroupValueSubject` asks SR1 of every
assumed group VALUE, silent only when the source was already in the same shape (a chained `TYPEDEF TYPE S`, a SAME AS
of a reported entry). The verdict is anchored at `StrongTypeModel.StrongRoot`, the entry whose TYPE clause composed the
strength. Probed beyond the note: VALUE on the template root had the same hole; same predicate. Three negatives at
2002-2023 and one positive (`2002/pb2517_group_value_in_weak_type_ok`); row SR-13.18.63.3-1 gains its witnesses (GAP
unchanged).

**B — PB2516, PB1954, PB2071 (landed).** PB2516: a constant entry is its own alternative of the section's entry list
(§13.5.2, beside record-description-entry), so it ends the record before it; `BindEntries` left the level stack open,
and `01 D. 05 A. 01 K CONSTANT AS 3. 05 B.` bound B into D. One local `CloseRecord` now ends a record for the
constant arm and the prebound arm, and the open record (not `newRoots[^1]`) owns a level-66 entry: COBOLNET2771 and
COBOLNET2740. PB1954: report group entries now obey §8.5.1.3.2's equal-sibling rule through the data arm's own
comparison (`ScreenImmediateMemberLevel`), and `ReportEntryRuns` cuts an RD's entries at its constants once, for the
walk and every flat screen (the cut also removed a spurious COBOLNET2199). PB2071: a REDEFINES written in a TYPEDEF
template is resolved and screened as written (`TemplateItems`; the clause rules SR5/SR12/SR14/SR17 extracted into
`ScreenRedefinesClause`, asked of templates with no storage class); an unused STRONG template's internal REDEFINES
still conforms (§8.5.3.1). The §13.18.57.3 SR6 message now quotes the rule ("subordinate to a type declaration that
includes the STRONG phrase"). 18 goldens; rows GR-13.18.33.4-1, SR-13.18.45.3-2 and GR-8.5.1.3.2-2 re-verdicted, ten
§13.18.44.3 / §13.18.57.3 rows gain witnesses. Lead filed: PB2548 (RENAMES, OCCURS KEY and DEPENDING ON resolvers
still skip templates).

**F — PB2518 (landed).** §13.18.44.3 SR10 ("without intervening entries that define new storage areas") was never
diagnosed, and its inventory row said CONFORMS on the note that the overlay layout tolerates the gap.
`ScreenRedefinesEntry` now asks it over the scope the pair was resolved in (`FirstInterveningStorage`: the first entry
between data-name-2 and the subject with no REDEFINES of its own; other sections' roots and compiler temporaries never
intervene), COBOLNET2739. The same loop never reached a redefiner inside a type declaration: `ResolveRedefines` now
walks `CompositionForest()`, resolves every item and screens only WRITTEN entries (`ClonedFrom` null), so a type is
diagnosed once, at its declaration. `2023/pb96_renames_span_over_redefines` was itself SR10-illegal and was reordered
(output unchanged). Two positives, three negatives, two unit tests; SR-13.18.44.3-10 now truthful (GAP unchanged).

**D — PB2499 (landed).** A mnemonic-name "identifies an implementor-defined device-name, feature-name, or
switch-name" (§8.3.2.2.16): a word, with nothing to subscript or reference-modify and no qualified format in
§8.4.2.2.2. ACCEPT … FROM and SET Format 3 spelled the slot as a `dataReference` and their binders read only the word,
so `ACCEPT X FROM MYIN (1)`, `FROM MYIN OF WS-G` and `SET MYSW (1) TO ON` compiled clean and dropped the suffix. Both
slots are now `cobolWord`, as DISPLAY UPON's was, and `CobolErrorStrategy.MnemonicSuffixMessage` names the syntax
error COBOLNET2269. WRITE ADVANCING, whose slot shares identifier-2's `dataReference`, rejected the suffix but called a
declared mnemonic "not defined" (COBOLNET1639); `SequentialIoBinder.MnemonicOperand` asks the registry about the
reference's word and `BindAdvancing` refuses the suffix with the same text (`MnemonicNameSlot.SuffixMessage`). Ten
negatives at 85-2023 and the first runtime witness of ACCEPT FROM mnemonic-name; FMT-14.9.1.2 PARTIAL → CONFORMS.

**A — PB165 (landed; PB2097 not started, SPLIT).** A CALL whose activated program the activating element holds no
signature for (a dynamic Format-1 CALL, or a program-prototype whose details §12.3.8.4 GR10 c) takes "from the
external repository") now checks all of §14.8.2 and §14.8.3 at call initiation, as §14.9.4.4 GR3 d) requires: each
operand's `ActivationDescription` (the universal-INVOKE builder) rides its `CobolArg`, the activated unit registers
its formals', and `ActivationRelations.CallArgumentViolation` / `CallReturningViolation` decide rule 2's clause
identity or COMPUTE / SET / MOVE validity, or rule 1's length, from the program actually reached and the caller's
program-specifiers (`ProgramTable.CallProgram`). The §14.8.2.2 group arm, the object-reference identity and the
§14.8.3 core are one copy each, shared with the universal INVOKE lane. `BoundaryClass` and `CallEmitter.IsDynamicLane`
are deleted. Golden `2002/pb165_repository_prototype_description_check` (18 cases); SR-14.9.4.3-25 PARTIAL →
CONFORMS; PB289 unblocked. PB2097 (external repository slice 1) was not started; its note carries the hand-off. Lead
filed: PB2549 (the run-time-located rule-2 lanes adopt a BY CONTENT image unconverted: 1234 into PIC 9(3) arrives 123
where §14.2.3 rule 9's COMPUTE gives 234).

**The train.** Gating: batched (every report quotes a leg-1-only verdict). Each patch was applied with the inventory
excluded and its `record_verdicts` batches re-applied on the merged tree, in order. One code conflict: B (PB2071) and
F (PB2518) both rewrote the `ResolveRedefines` loop header; resolved to F's `CompositionForest()` walk with
written-only screening, which subsumes B's `AllItems().Concat(TemplateItems())` there (B's `TemplateItems` stays for
the clause screens and the SR4 strong-type arm), and the summary now names both notes. Three corpus manifests took
both sides' appended elements. Whole-population gate run 1 GREEN at first try; the lander's review found two defects,
both fixed in the train: D's `MnemonicOperand` summary had been inserted between `FeatureAdvancing`'s summary and its
method (doc order), and B's `ScreenReportLevelNumbers` counted an 88 written in an RD (already COBOLNET1746) as a
sibling, so `negative/pb558-condition-name-in-report-section` gained a cascade COBOLNET2771; entries outside levels
1-49 now take no place in the hierarchy. Gate run 2: `=== BUILD-LOCAL GATE: GREEN — Conformance 11,008/11,008 · Unit
32,614/32,614 · Characterization 36/36` (run 20261008T052252Z-33cb66). Oracle against ba9bfd1ad825: 69 of 7537
cases differ — 38 ADDED (this train's goldens); 28 CSHARP, every differing line a `ProgramRegistry.Register`,
`CallProgram` or `CallPointer` call (A: formals and RETURNING items register their descriptions, run-time-located
sites describe their operands and pass the program-specifiers, `BoundaryClass` gone); 3 DIAGNOSTICS —
`negative/pb1281-redefines-qualified` and `negative/pb513-type-entry-composition` each gain a true SR10 COBOLNET2739
(F; both fixtures' sources hold a storage entry between the pair) and `negative/pb1301-strong-type-chain-level`'s
SR6 COBOLNET1532 is reworded (B). Re-recorded as 82e96cfba071. Semgrep unchanged; every CI audit green locally. GAP 147 → 145. No
diagnostic codes claimed (COBOLNET3074-3079 and 3083-3091 returned). Leads PB2548 and PB2549 filed; PB2550-PB2552
unused.

