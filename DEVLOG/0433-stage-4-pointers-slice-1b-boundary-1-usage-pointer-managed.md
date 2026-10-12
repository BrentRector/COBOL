## Entry 433 — Stage-4 pointers slice 1b boundary 1: USAGE POINTER → managed ManagedPointer field (8-byte handle deleted)

First working-core commit of the pointer rearchitecture (`docs/RECORD_STRUCT_STORAGE_DESIGN.md` §10.5, the turnkey
surface map from Entry 432). A `USAGE POINTER` item is now ALWAYS a managed `ManagedPointer` — there is no 8-byte
byte handle (Entry 431) — and pointers are always-typed, **NOT gated by `EnableTypedFields`** (a managed reference is
the only correct representation). Blast radius is exactly one corpus program (`tests/conformance/2002/pointer_data.cob`,
confirmed Entry 432), so boundary 1 is fully verified against it + the guard.

**Representation.** A new always-on pass `Binder.CollectPointerFields` registers one `static ManagedPointer
_PTR_<name>` field per `USAGE POINTER` elementary item AND per `BASED` item (`IrModule.PointerFieldDefs` →
`CilEmitter.EmitProgramState`). The field's default value (`Buffer` null) IS the COBOL NULL initial state, so NO
explicit initializer is emitted — the one place `default(T)` is the correct COBOL initial value (ADR §1.7 exception).
`StorageLayoutComputer` now SKIPS a `USAGE POINTER` item's layout (like a `BASED` item) — a pointer no longer
occupies a WORKING-STORAGE byte slot; `FieldSizeCalculator`'s 8 is retained only as the *logical* `LENGTH OF` value.

**SET.** `SET p TO NULL` / `SET p TO q` rebind off the byte-MOVE path to a new `BoundSetPointerStatement` →
`IrPointerStore` (NULL = `ldsflda; initobj`; FROM another pointer = `ldsfld; stsfld` struct copy) against the target's
`_PTR_` field.

**Compare.** `ConditionLowerer` intercepts a pointer `=`/`NOT =` relation BEFORE operand normalization (a pointer has
no byte location to normalize) and emits `IrPointerCompare` — **address identity** (ISO §8.8.4.1.4):
`ReferenceEquals(l.Buffer, r.Buffer) && l.Offset == r.Offset` (right `NULL` ⇒ Buffer null / Offset 0), inverted for
`NOT =`. NOT the record-struct `Equals` (which also weighs `Length`/`Pic` and would wrongly distinguish two pointers
to the same address typed differently — the trap flagged in Entry 432).

**Dead 8-byte-handle paths DELETED** (PROMPT.md zero-dead-code): the `CilDataEmitter` pointer-MOVE branch (the
`IsPointerLike → MoveAlphanumericToAlphanumeric` 8-byte handle copy) and the `StorageLocation` 8-byte pointer-PIC
synth. The `DataMovementLowerer` INITIALIZE pointer-skip stays (correct — INITIALIZE leaves pointers unchanged).

New IR: `IrPointerStore` / `IrPointerCompare` / `IrPointerFieldDef` (+ `IrModule.PointerFieldDefs`,
`LoweringContext.PointerFieldRefs`, `EmissionContext.PointerFields`). `pointer_data` stays green
(PNULL=YES / PNOTNULL=NO / PEQQ=YES / AFTERSET=YES) on the new path, observably identical to the deleted handle path.
The new pointer code paths are inert for every non-pointer program (the pass registers nothing; the SET/compare hooks
early-return), so the rest of the corpus is byte-identical. Guard ALL GREEN (**1196 / 507 / 364**). The two
IX216A/217A absent-file "regressions" the first guard run reported were a known stale-`.txt` isolation artifact
(both pass cleanly from a clean data-file state — the guard's own start-clean `rm -f *.txt`); the code is provably
inert for them. **Next: boundary 2** — `ADDRESS OF` / `SET ADDRESS OF` / BASED-deref (`IrBasedDerefLocation`,
classifier trigger 6) + a new `tests/conformance/2002/based_pointer.cob` test.

