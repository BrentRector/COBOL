## PB2690 closed: a BY REFERENCE store of exactly the image the formal was shown raises the dynamic table's capacity

**Mechanism.** A BY REFERENCE formal whose fixed table faces the argument's dynamic-capacity table occupies a fresh area
filled from the argument and stored back at return. Since wave 1069 C the write-back raised the capacity to the highest
occurrence whose image differed from the one the formal was shown (the INITIALIZED default, or spaces), so a callee that
stored exactly that image (`MOVE "a" TO FE (3)` with `AE VALUE "a"`, or SPACES into a table without INITIALIZED) left the
capacity where it was. ISO 8.5.1.9.3 raises it for any data item "referenced as a receiving item" past the capacity,
whatever is stored (14.2.3 GR8 makes the formal's store that reference).

**Re-probe.** `E:\COBOL-coord\scratch\w1077a\p\p1.cob` (`--std 2014`) on a6744fa4d: `CALL=1 DIRECT=3 NUM=3`; after the
fix `CALL=3 DIRECT=3 NUM=3`.

**Fix shape.** The receiving references are recorded where they are made. Every store through a Tier-B view of a class
located by a data-address pointer (a LINKAGE or BASED item) is now followed by `StorageCell.Received(offset, width)`
(`PlaceRenderer.Received`), a null test unless a boundary watches the cell. The boundary starts the record after the
formal's copy-in (`ManagedPointer<T>.Watch`, emitted for every area formal, program and method), and a view over storage
a store can grow (`ManagedPointer<T>.OverStorage`: `CobolArgAdapt.Text` and `VarGroup`, the typed INVOKE's overlaying
argument store, the universal box) hands the record (`ReceivingReferences`, in the formal's storage positions) to
`CobolVarGroup.Overlay`, whose `OverlaidTable` and `GrownCount` create every occurrence up to the highest one referenced.
A boundary copy of a whole group (a copy-in, the write half of an argument's view) records nothing
(`CallEmitter.BoundaryCopyWrite` → `PlaceRenderer.WriteGroupCopy`), and a store that reaches the area only through such a
copy is still recognised by its effect. The runtime major is 4 (`Overlay` and `UniversalGroupCarrier.WriteBack*` changed
signature in the shipped surface; `RuntimeAbiPinDriftTests` re-pinned); the call ABI number stays 1 because no carrier,
crossing arm or description changed. Docs: `docs/CONFORMANCE.md` D-VGB (the known departure is gone),
`docs/COBOLNET_INTERPROGRAM_DESIGN.md` (the receiving-reference record).

**Goldens.** `2014/pb2690_store_of_shown_image_grows_capacity_call` (the probe, a direct store, SPACES into a table without
INITIALIZED, occurrence 2 only, a read-only callee, a variable-length formal, the nested element arm, BY CONTENT, a formal
passed on to a program of the same description, and one passed on to an elementary formal that stores only inside the capacity, which creates nothing) and `_invoke` (typed, variable formal, universal, SPACES, BY CONTENT);
negative `pb2690-store-of-shown-image-grows-capacity-below-2014` (COBOLNET0900); unit
`BoundaryGroupCorrespondenceTests.AStoreOfTheShownImagePastTheCapacity_CreatesTheOccurrence_ByTheReceivingReference`.
No inventory row claimed or closed (kb/Work PB2690 `closes_rows_reason`); PB2690 flipped to landed.

**Gate.** Run 20261014T194320Z-7a8a00: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 6,089, Unit 32,991, Characterization 6 cases in leg 1). The first run (20261014T193710Z-12a96f) was RED on the boundary-layout pin, moved again by the copy-in change, and on the `filter_population.py` self-test (MSB4236, an SDK not resolved during a dotnet first-run in its scrubbed environment; it passes alone and passed in the re-gate). Self-review: the record is emitted only for descriptions holding a table of a fixed or DEPENDING count (a table-less LINKAGE record pays nothing), and a whole-group boundary copy records nothing (golden lane GE).

**Branch review round 1.** Four findings, all fixed. R1-1: `PlaceRenderer.WriteView` switched on the storage form with a
catch-all arm, which `PlaceStorageFormSwitchDriftTests` rejects; it now switches on the window coding. The gate had not run
that tree-wide sweep (it was planned into leg 2), so CI would have been the first to see it. R1-2 and R1-3: the
non-recording boundary copy covered only a bare view, so the write half of an occurs-depending group (CALL and typed
INVOKE) and of a universal box recorded every occurrence. When the next callee stored nothing, the caller's table still
grew to the formal's maximum. The test now looks at the group's full allocation; the universal box's write half and the
method cell copy-in use it, and the RETURNING delivery still records because it is the statement's own store. An
elementary or reference-modified argument's view is written per store, so it keeps recording (probes `e1.cob` and
`u3.cob` in `E:/COBOL-coord/scratch/w1077a/fin/`). New golden lanes: `_call` GQ and GP, `_invoke` UO, US and TO. On the
reviewed head, GQ, UO and TO printed 3 where 8.5.1.9.3 gives 1. R1-4: the record's coordinates are the formal's storage
positions, not its fixed run, and the comments now say so. Re-gate: run 20261014T201825Z-618d8a, `=== BUILD-LOCAL GATE:
LEG 1 ONLY (batched gating, PB2515): GREEN`; `PlaceStorageFormSwitchDriftTests` was again planned into leg 2, and it passed
when run on its own.

**Landing stop (Linux gate RED on 917df8e03).** Three 2002 table-SORT goldens over a BASED table
(`pb1175_table_sort_shared_storage`, `pb1922_table_sort_pointer_element`, `pb1951_table_sort_pointer_table_element`)
no longer compiled: CS0103 on the loop variable `__eN`. `PlaceRenderer.Write` returns ONE C# statement, and the table
SORT's place-back loop embeds it as the loop body (`for (…) <store>`), but `Received` appended the record as a second bare
statement, which fell outside the loop. The store and its record are now one block statement, so every caller that
embeds a store keeps both. It reproduced on Windows with the same compiler, so it was the branch's own defect, not a
host difference. The Windows leg 1 never ran the three: no impact map was recorded for the base, so every case was tier 1
and leg 1 took the cheapest half by recorded time. The Linux gate runs the whole population and caught it.

**Branch review round 3 (R3-1, a test gap).** The one-block fix also corrected a wrong answer on the SIZE ERROR path.
`ArithmeticEmitter` emits `else <store>` for its five resultant arms, and with the record as a second bare statement the
record ran even when the size error left the receiver unchanged, so a callee's `ADD 5 TO FQ (3) ON SIZE ERROR` raised the
caller's capacity to 3 where ISO §14.7.5 SIZE ERROR rule 2) and the caller's own statement give 1. Nothing witnessed it,
so `_call` gains lanes GZ (the callee's four ON SIZE ERROR stores: fixed numeric, numeric-edited, floating-point edited,
COMP-1) and DZ (the same four in the caller), both `[QEGF] 1`. A mutation check confirmed that the lane bites: with the
braces removed, GZ printed 3.

**Branch review of wave 1078 A, round 1 (R1-1, a wrong answer one scope down).** The receiving-reference record covered
only the formal area's own cell. A fixed table nested in an occurrence of the formal's OWN dynamic-capacity table lives
in that occurrence's element cell, so `PlaceRenderer.Received` skipped the store (it required the cell path to be the
bare root) and `CobolVarGroup.Overlay` handed no record to a dynamic-opposite-dynamic pair. The reviewer's probe: a
callee's `MOVE SPACE TO FE (1, 3)` (formal `FO OCCURS DYNAMIC` of `FY, FI OCCURS 3`, argument `NOT1 OCCURS DYNAMIC` of
`NY, NI OCCURS DYNAMIC FROM 1`) printed `FUNCTION LENGTH (NOT1 (1))` = 2 where ISO §8.5.1.9.3 and the caller's own twin
give 4. The re-probe also found the sibling arm: an occurrence the argument lacked (created by the formal's own table, or
by a formal whose outer table is fixed) was the formal's element RESHAPED, so its nested table took the formal's fixed
count 3 even when the callee referenced only its first occurrence (`LENGTH` 4, expected 2).
Fix: a store into an element cell is recorded on the AREA's cell, keyed by the subscript path it took (each level's
component ordinal and occurrence number; `StorageCell.Received(path, at, width)`, emitted by `PlaceRenderer.ReceivedScope`),
so an element cell the store itself created is covered; `ReceivingReferences` became a view over a tree of scopes with
`Element(component, occurrence)`; `Overlay` passes each occurrence its own record (`TableReferences`, fixed or dynamic
table), and every occurrence the argument lacked is the argument's created element (the INITIALIZED default, else the new
`SpaceFilled`) with the formal's element stored over it. Goldens: `_call` lanes GW `1 4`, GI `[iii] 1 4`, GX `[Q] 2 2`,
GF `[Q] 2 2`, GD `4` and the caller twin DW `2 4 2`; `_invoke` lanes NW `1 4`, NU `1 4`, NX `2 2`. On the reviewed
head the same lanes printed GW `1 2`, GI `1 2`, GX `2 4`, GF `2 4`, GD `2`, NW `1 2`, NU `1 2`, NX `2 4`.

**Landing stop (Linux gate RED on dc0214b9f).** Characterization 14 of 36: every `EmittedCSharp_MatchesSnapshot` case.
The runtime major went from 3 to 4 above, and every emitted module records the runtime version in its
`CobolRepositoryAttribute` and its `EnsureRegistered` call, so the committed snapshots still carried "3.0.0.0". The
finisher re-recorded them from the emitter (`COBOLNET_UPDATE_SNAPSHOTS=1`); the diff is exactly those two lines per
snapshot, "3.0.0.0" to "4.0.0.0", and all 36 characterization cases then pass on Windows. This is the second time the
branch stopped this way (the 2.0.0 to 3.0.0 bump stopped at 305fba0d8): Windows leg 1 again ran 6 of 36 cases, because a
`<Version>` change does not raise the snapshot cases into leg 1 (kb/Work PB3144, open).
