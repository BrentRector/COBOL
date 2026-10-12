## Entry 613 - 2026-07-04 23:14 PDT - Phase 4 track (b) increment 1: USAGE POINTER data on the ManagedPointer carrier

The first slice of the pointer subsystem (the DEVLOG-610 reconciliation's biggest hidden-work reservoir:
the M2-DATA rows were LEGACY-only mirages - greenfield staged them all loud). pointer_data.cob scopes
itself to increment 1 by design (NULL-holding pointers + equality; ADDRESS OF/BASED/ALLOCATE are
increment 2+), so this closes it cleanly while laying the carrier foundation.

**Data model.** New `PicCategory.Pointer` + `PicInfo.PointerItem` (PICTURE-less, the IndexItem synthesis
pattern); ClrType → the runtime `ManagedPointer` (feedback_managed_pointers - the ONE managed-ref carrier,
already used for CALL BY REFERENCE), DefaultInitializer → `ManagedPointer.Null` (the predefined NULL data
pointer, §8.4.3.10). ParseUsage retired the 0899 skeleton for POINTER, keeping only the introduction gate
(0900 below 2002, like OBJECT REFERENCE); DataBinder synthesizes PointerItem for a PICTURE-less USAGE
POINTER. `Usage.Pointer` left IsUnimplementedSkeleton.

**SET pointer (§14.9.39 Format 4).** `SET p TO NULL` / `SET p TO q` - a new `BoundSetPointer` +
`BindSetPointer`, routed from BOTH SET grammar shapes (the setObjectReferenceStatement NULL leg AND the
BindSetTo re-route), keyed on a POINTER target/sender BEFORE the object-reference Format 5. 0869 band:
non-pointer target, non-pointer/non-NULL sender, SELF/SUPER sender (object-only).

**Pointer equality (§8.8.4.1.3).** BindComparison gained a data-pointer relation branch mirroring the
object-relation one: [NOT] EQUAL only (ordering → 0869), both operands pointer-or-NULL (else 0869). The
ConditionRenderer renders `ManagedPointer.SameTarget(a, b)` (both-NULL / same-instance; the NULL
figurative → the null carrier) BEFORE the figurative branch, so NULL never width-materializes against a
pointer. New runtime helper `ManagedPointer.SameTarget` (increment 2's ADDRESS OF makes it compare
referenced storage).

**Golden + tests.** pointer_data.cob (2002) ENABLED (PNULL=YES / PNOTNULL=NO / PEQQ=YES / AFTERSET=YES -
the legacy engine agrees, so NOT greenfield-only); +5 PointerDataTests (the 2002 introduction gate, the
0869 SET-band ×2, the 0869 relation-band ×2). The usage-pointer-2002 matrix row flipped active; two
LoudGuardTests that asserted POINTER-is-a-skeleton updated (POINTER left the skeleton set; FLOAT-SHORT now
carries the storage-mapping loud-guard fact); the DataSkeletonEditionTests PTR1 row removed.

**Battery:** greenfield conformance **1608/1608** (+8: the enabled golden + 5 pointer tests + the matrix
row's below-edition leg, net of the removed skeleton case) · unit **100/100** · guard-fast **ALL GREEN**
(no grammar change - USAGE POINTER/SET/comparison all use existing grammar). Reconciliation M2-DATA-5 →
PARTIAL (increment 1 landed).


