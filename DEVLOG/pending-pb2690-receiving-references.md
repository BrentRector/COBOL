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

**Gate.** Recorded below once the gate has run.
