## Entry 1925 — 2026-10-07 18:20 PDT — Train 1034b (wave 1034: G, H, B): prototype pointers typed by signature, sort files per run unit, tables of variable-length elements compose and carry

**Cluster G — PB2464 (landed); PB2103 blocked.** PB2103 (external-repository slice 7) could not start: it needs slices 4
and 5 (PB2100, PB2101), which are open and themselves need PB2099 and PB2097, and no `RepositoryReader`/`Locator`/`Record`
exists in `src`. The note is now `blocked: true`, `blocked_by: [PB2100, PB2101]`. PB2464: ISO §13.18.60.4 GR25 / GR26 type a
restricted program- or function-pointer by its prototype's SIGNATURE, so two pointers restricted to differently named
prototypes of one signature are of the same type (§14.8.2.3.2 4)) and §14.9.39.3 SR20 / SR22 admit a SET between them. The
re-probe reproduced the universal lane (EC-OO-METHOD from §9.3.6 rule 6's name compare), and the sibling sweep found that the
TYPED lane refused the same pair at compile time (COBOLNET0828 from `OoConformance.CategoryArmMismatch`). The note's
"canonical signature key" did not hold: `PrototypeSignatures.Same` is not an equivalence (its group, bit-group and
national-group arms and its null wildcard), so the type is a CLASS that `Same` builds (`PrototypeSignatureClasses`, one per
bind session, identity = the founder's externalized name). `BinderDriver.ResolvePointerRestrictions` stamps
`DataItem.PointerRestrictionIdentity` over every forest after the repository exists, and `PrototypeSignatures.RestrictionIdentity`
is the one reader, so `ActivationDescription.Clauses` carries it and `MaySet`, `Matches` and `ReturningViolation` agree with
the typed arm. `PrototypePointerTypeDriftTests` forbids a name compare. Goldens `2002/pb2464_prototype_pointer_signature_type`
and `2014/pb2464_function_pointer_signature_type`, plus three negatives. No inventory row. Leads filed: PB2494 (the
external-repository design still names the deleted `ConformanceDescriptor`) and PB2495 (mutually recursive prototypes are
bisimilar but fall into two classes, because the nested restriction compares by name).

**Cluster H — PB1570 (landed).** The SORT/MERGE stores were a process-wide static `Dictionary` on `CobolSort`. Two run units
in one process therefore shared and mutated each other's sort files, an abandoned statement's store outlived its run unit,
and one run unit's procedure phase raised EC-SORT-MERGE-ACTIVE in another. §14.6.1 makes a run unit independent except for
messages, files and switches, and a sort-merge file is none of those, so the "executing SORT/MERGE statement" of §14.9.40.4
GR10 / GR13 belongs to the same run unit. The stores moved to `RunUnit.SortFiles` (`IO/SortFileTable.cs`), and `CobolSort`
keeps the statement logic and the emitted surface (generated code unchanged). Two Unit witnesses
(`SortFileRunUnitScopeTests`) failed on the old store and pass now. The new
`RunUnitStateDriftTests.NoStaticMutableCollection_OutsideTheDocumentedProcessStores` requires a written reason for every
static readonly mutable collection in the runtime (14 documented). No golden: one program cannot reach the defect. PB1953
recurred in the implementer's first gate (eight `UniversalCrossingShapeDriftTests` rows NEVER RAN in leg 2), and the
recurrence line is in that note.

**Cluster B — PB244 (landed; closes GR-14.9.11.4-7).** A cell-backed variable-length group holding an OCCURS DEPENDING or
dynamic-capacity table of variable-length elements now DISPLAYs and measures like its declared twin (`CellOdoTail.Comps` /
`CutComponents`, the runtime `CellGroupShape`). An OCCURS DEPENDING table of such elements rides the §8.5.1.12 carrier for
MOVE, relation, CALL and INITIALIZE (`VarPartKind.OdoTable`, unrolled at its maximum, an owner-overturnable determination in
the note). `DataItem.CanCompose(CurrentExtentUse)` is the one spelling of the three nested capabilities: one-way image,
carrier and record. The sibling sweep found two more defects, both fixed. A cell window's record store let such a window
through to a store that writes only the fixed run (silently wrong; now `RecordImageCapable`). And `FromVarImage` stored a
receiving OCCURS DEPENDING table to its MAXIMUM where §13.18.38.4 GR8 a) uses only the current count, which a w1033c golden
(`pb244_vlg_cell_odo_and_dynamic` B2, `Qmm12 ` → `Qmm12z`) had encoded. Goldens `2014/pb244_vlg_display_cell_element_tables`,
`pb244_vlg_carrier_odo_element_tables` and `pb244_vlg_receiving_odo_count`, plus three negatives at 2002. The w1033c claim
that a dynamic-capacity table of variable-length elements "has no matching rule" was overturned (§14.6.9.2 2), §14.6.9.3).
Leads filed: PB2496 (that dynamic-capacity carrier), PB2497 (a record holding an OCCURS DEPENDING table of variable-length
elements, which needs an owner determination of the READ count source) and PB2498 (FUNCTION LENGTH of a variable-length group
with USAGE BIT items, still COBOLNET1756).

**The train.** The clusters were applied in order onto train 1034 (`54b828c5c`). G's `docs/DRIFT_RULES.md` conflict was
regenerated (287 drift tests). B conflicted in `PlaceRenderer.WriteVarGroupImageAt` with PB2094's formal-storage `DynMax`,
and both sides were kept. B's inventory hunk was discarded and its batch `verdicts-w1034b.json` re-applied with
`record_verdicts` (GR-14.9.11.4-7 PARTIAL → CONFORMS). Lander gate on the merged tree: GREEN, Conformance 10,946/10,946 ·
Unit 32,575/32,575 · Characterization 35/35 (run `20261008T005916Z-4501e7`). semgrep verify: no count rose. The review found
one problem, fixed in B's commit: `DataItem.CurrentImageCapable`'s remark repeated the overturned "no matching rule" claim,
and now cites §14.6.9.2 / §14.6.9.3 and PB2496. The review also judged G's group-local signature-class identity: it is
comparable only between descriptions compiled in one group, which the class's own remark documents until slice 7 replaces
it. The oracle against `436a64b50ac0` was DIFFERENT on 31 of 7,462 cases: 11 ADDED (this train's goldens and negatives) and
20 CSHARP, all cluster B, in four classes: (1) `StorageCell.StoreVarGroupAt` takes the OCCURS DEPENDING tail and count
(`default, 0` where the group holds none), a neutral rendering; (2) `new CellOdoTail(e, m)` gains its components argument
(`, 0`), also neutral; (3) a group holding an OCCURS DEPENDING table gains `FromVarImage(.., int __odo = int.MaxValue)` with
the GR8 a) keep-the-tail line, and a MOVE passes the count, which is the fix; (4) a group holding an OCCURS DEPENDING table of
variable-length elements gains its carrier (`AsVarImage(int __odo)`, `FromVarImage`, `__Contiguous`), which is the new
capability. G and H changed no existing case. The baseline was re-recorded. GAP 153 → 152. No cluster dropped. Diagnostic
codes COBOLNET3029-3031 and 3044-3049 are unused and returned.

