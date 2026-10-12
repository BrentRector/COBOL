## Entry 1659 — 2026-09-23 02:43 PDT — Landing train 55: wave 55 finishers (GA, GB, GC, GD), five notes landed, GAP −3 to 2092

Train 55 lands the four wave-55 finisher clusters on top of train 54, one commit per cluster. Every implementer branch carried its wave-54 predecessor as a MERGE (and GA also carried train 52's PB956). Those predecessors were already on main through train 54, so each cluster came in as the diff from its LAST MERGE COMMIT to its head, not from the merge base the manifest suggested. The merge base (`acff7ee45`) would have re-applied train 54's content a second time over the train 54 lander's own composition. Each cluster was applied `git apply -3`, and its conflicts were composed against the spec and the reports. Manifests were verified per cluster (element counts, no duplicates), both marker checks came back clean, and every added file was cross-checked against the cluster's name-status.

**GA — PB1019 (method arm) + PB1010.**
- **PB1019.** `ADDRESS OF` a method's LINKAGE formal or RETURNING item was refused with COBOLNET0869, and the message named an OCCURS residue. PB956's skip in `DataBinder.PtrBindBasedAndAddressables` is deleted, so such an item is now forced onto its per-activation cell. `OoEmitter.MethodCellFormalStore` copies the argument in at entry, and copy-out and RETURNING read one composer, `MethodBoundaryValue` (§8.4.3.11.3 1), §14.2.3 8), §14.8.3.3).
- **PB1010.** DECLARATIVES in a METHOD were refused with COBOLNET0899, although §14.2.2 SR10 admits Formats 1 and 2 in a method definition. Each method's declarative sections now bind through the program's own `DeclCollectSection`.
- **One emitter.** `DispatchEmitter.EmitUseMachinery` is now the ONE selection-machinery emitter. A method calls it `asLocal`, so the local `__RunUse`/`__IoCheck`/`__EcDispatch` functions shadow the class funnels (§14.9.49.4 GR3/GR4 a)).
- **Runtime.** A new runtime `INonfatalSelector` gives each method activation its own nonfatal selector, which is restored on exit.
- **What the lander did not take.** The implementer's merge-seam commit introduced `NumericRenderer.FloatImageToCarrier`, and its invoker-side RETURNING fix used `NumericImagePlace`. Main's `NumericRenderer.CarrierOfImage` (PB970/PB187, train 54) already decodes the returning item's byte form, so both are superseded and neither was introduced. The rest of the report held.
- **Goldens:** 2002 `pb1019_address_of_method_formal` and `pb1010_method_declaratives`, plus the negative `pb1010-declaratives-outside-method`.
- **Drift test:** `MethodSelectionScopeDriftTests`.
- **Rows:** SR-14.2.2-10 is now CONFORMS.

**GB — PB1025.**
- **The filed probes were illegal.** On re-probe, the source the note filed is ILLEGAL. §12.4.5.12.3 SR4 and §12.4.5.6.3 SR5 require an indexed RECORD or ALTERNATE key to lie within the first n (minimum-size) bytes of the record, and §14.9.40.3 SR6 g) / §14.9.24.3 SR4 g) say the same of a SORT/MERGE key.
- **What was wrong before.** The indexed pair was enforced nowhere, so READ … KEY returned the wrong record. SR6 g) fired only when integer-2 was written, and it measured the fixed-run offset.
- **The fix.** One reach reader, `RecordLayout.KeyWindowOf`, now drives all three rules on every variable-length file: COBOLNET0874, plus two new `FileControlKeyRules` rows on COBOLNET0863.
- **The legal residue.** Trailing fixed material after a dynamic member is located per record by ONE `CobolContiguousLayout` per record type. Its `Position` shares `CobolVarGroup.ContiguousTake` with `Decompose` (§8.5.1.11.2; determination D-KWV).
- **Deleted:** `SortBinder.FirstVariableOffset`, PB981's by-name refusal, and `RuntimeApi.VarGroupFromContiguous`.
- **The implementer's one red (gate3).** It was the `GrammarDiagramGeneratorDriftTests` subprocess race (PB376). The train's full Unit run is green.
- **Goldens:** 2014 `pb1025_key_after_dynamic_member` and three negatives.
- **Rows:** SR-12.4.5.12.3-4 and SR-12.4.5.6.3-5 are now CONFORMS.
- **Still open:** PB1026 (the EXTERNAL FD out-of-line record carrier) is a separate mechanism.

**GC — PB993.**
- **The defect.** The SORT/MERGE implicit transfers ran each as-if statement's USE procedure and then carried on regardless. A fatal GIVING OPEN ('30') ran the declarative again for every WRITE ('48') and for the CLOSE ('42').
- **One rule table.** `SortEmitter.RuleFor` maps each (verb, as-if statement) pair to a disposition: Continue, Terminate or Bypass. The default is §9.1.13.1's transfer to the end of the statement. The specific cells come from §14.9.40.4 GR12 b)/GR15 and §14.9.24.4 GR7 a), GR12 a)/b).
- **The hooks report completion.** They now return -3 when no procedure applied, distinct from -1 (completed normally), and the EC-free `__IoCheck` returns bool.
- **Verb precedence.** `__IoCheckEc(__verbRule)` lets the verb decide the fatal disposition (§14.6.13.1.3 2) precedes 5)/7)).
- **Composed with GA.** The composed hook signatures carry GA's `asLocal` member modifier over GA's `(decls, w, asLocal)` parameters.
- **Goldens:** 2002 `pb993_sort_merge_transfer_termination` (nine legs) and a negative. Four pb714/pb837 goldens that pinned the old carry-on were re-derived.
- **Drift test:** `SortTransferRuleDriftTests`.
- **Rows:** GR-14.9.40.4-15 is now PARTIAL; PB994 claims the residual.

**GD — PB1030.**
- **The defect.** The reference resolver returned `Place?`, and each of ~45 callers guessed what a null meant. As a result, illegal source was announced as a COBOL.NET gap: `DISPLAY E("A")`, `E(ALL)` and an alphabet-name each compiled with COBOLNET1756 and then aborted the run. Callers also stacked rule-less "unresolvable" errors on the resolver's own diagnostic, and a CANCEL target was dropped silently.
- **One answer type.** A closed `RefResolution` (Place | Reported | Deferred) plus a `DeferredShape` census now answers every reference. The resolver puts every deferral on the unbuilt ledger itself.
- **New diagnostics:**
  - COBOLNET2363: something that is not a subscript is written in a subscript or reference-modifier position (§8.4.2.3.2, §8.4.2.3.3 SR6, §8.4.3.3.3 SR4).
  - COBOLNET2364: a reference to a refused declaration.
  - COBOLNET1639: an alphabet-name used as data (§8.4.2.1).
- **One subscript reading.** `ReadSubscripts` replaces three copies.
- **Composition with GB.** `SortAddFileKeys` keeps GB's reach rule inside GD's bool/`Reject` reporting shape.
- **Composition with main.** `BindProgramAddressOperand` (train 52; null means reported) answers a Deferred reference with `PlaceOrReported` and then returns null. `MaterializeSubscriptSegment` keeps main's `Ctx.Retypes` argument. PB1029's refusal shape is kept throughout.
- **Goldens:** 85 `pb1030_reference_answers` and five negatives.
- **Drift test:** `RefResolutionDriftTests`.
- **Rows:** a witness re-site on FMT-15.17.2 (`RefFailure` retired, reported by the witness-loss audit as RETIRED, not lost).

**The train.**
- **Gate.** One build, then:

  | Assembly | Result |
  |---|---|
  | Conformance (whole, unfiltered) | 8,286 / 8,286 green |
  | Characterization | 33 / 33 green |
  | Unit (full) | 29,008 / 29,008 green, after one fix below |
  | Legacy integration | 503 passed + 1 skipped |

- **The first gate had three Unit reds, all attributed:**
  - `ClosesRowsBackLinkDriftTests` and `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`: the verdict batches had not yet been applied (a lander ordering slip). After the batches were applied, the full Unit run was green.
  - `RefResolutionDriftTests.EveryNoPlaceBranch_BuildsItsRefusalFromTheAnswer` fired on the lander's OWN composition. A comment line in `BindProgramAddressOperand` pushed the `Outcome` read out of the scan's three-line window. The comment moved to the end of the line.
- **Verdicts.** The batches were re-applied with the MERGE writer. The witness-loss audit reported 0 unexcused, 1 retired.
- **Checks:** `work.py check`, `audit_code_citations` and `audit_doc_citations` found 0 findings. Semgrep counts are unchanged.
- **GAP 2095 → 2092.**
- **Notes landed:** PB1010, PB1019, PB1025, PB993 and PB1030. PB1026 stays open.
- **Dropped:** none.
- **Leads for the registrar** (from the reports, no notes filed; ids are central):
  - An override addressing a formal its base does not (a CS0115 risk, `OoEmitter.OoCrossingType`).
  - The stale SR-14.9.49.3-1 note text.
  - §14.9.40.3 SR6 d) (a dynamic-length SORT key) is likely unenforced.
  - D-FRA non-invertibility with dynamic members on both sides of a fixed key.
  - EC-SORT-MERGE-FILE-OPEN/RELEASE are never raised.
  - RENAMES THRU over a non-character leaf is deferred.
  - Seven `DeferredShape` members have no owning note.
  - Four resolver-adjacent entries still answer null through `WasDiagnosed`.

