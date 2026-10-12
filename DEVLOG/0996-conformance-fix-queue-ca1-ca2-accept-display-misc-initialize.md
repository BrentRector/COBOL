## Entry 996 — 2026-07-22 22:38 PDT — Conformance fix-queue: CA1 + CA2 (accept-display-misc) — INITIALIZE of a dynamic-length item → length 0; of a pointer/object-ref → SET TO NULL

Two majors, both in the INITIALIZE binder (§14.9.20.4).

**CA1 (GR7).** "When a dynamic-length elementary item is initialized, its length is set to zero." The bare INITIALIZE
ran the GR6c figurative-SPACE fill through the one MOVE path — `CobolDynString.Store(" ", limit)` — so the item was
left at LENGTH 1 (a single space), not 0. FIX: in `ExpandInitialize`'s elementary arm, after the GR5c qualification
(`InitializeSender` non-null), override a dynamic-length receiver's sender to an empty `BoundStringLiteral` so the SAME
dynamic-length store yields length 0. GR5c qualification is preserved (a REPLACING-of-a-non-matching-category
INITIALIZE still leaves the item unchanged — the override runs only after a sender is chosen).

**CA2 (GR4 + GR6c).** A data-pointer / program-pointer / object-reference receiver is initialized by an IMPLICIT
`SET … TO` the predefined NULL (GR6c: data/program-pointer → NULL address, object-reference → NULL reference); GR5a1
keeps these categories as receiving operands. `InitializeItemCategory` had no arm for `PicCategory.Pointer/
ProgramPointer/ObjectReference` → they fell to null-category and `ExpandInitialize` silently emitted nothing (the item
kept its prior value). FIX: (1) the three `InitializeItemCategory` arms → new `InitializeCategory` members
DataPointer/ProgramPointer/ObjectReference; (2) `ExpandInitialize` routes them to a NEW `InitializeSetNull` action (a
SET, not a MOVE — must not go through the `InitializeStore` conversion path), gated on the SAME GR5c qualification
(`InitializeSender`'s non-null result is the receiving-operand signal; its fill operand is unused, the SET target is
always the predefined NULL); (3) `InitializeEmitter` renders it as `Target = <DefaultInitializer>` (ManagedPointer.Null
/ ProgramPointer.Null / null — the same predefined-NULL idiom as `SetEmitter.EmitSetPointer`). `BoundStores.InitStores`
+ `UsageCollectionPass` gained the `InitializeSetNull` case (it writes its target). The REPLACING-pointer-category path
is grammar-unreachable today (a documented separate residue), so SET-TO-NULL is the complete parseable-surface fix.

**Verified (direct CLI):** CA1 `INITIALIZE WS-D` (PIC X DYNAMIC LENGTH, prior "HELLO") → FUNCTION LENGTH = 00 (was 01);
CA2 `INITIALIZE PP` (program-pointer at ENTRY) → NULL after (was still the entry address); the data-pointer variant
(POINTER at ADDRESS OF) → NULL after. Goldens `tests/conformance/2014/initialize_dynamic_length` (CA1) +
`2002/initialize_program_pointer` (CA2).

**Gate:** characterization 33/33 (no snapshot drift), unit 575/575, greenfield **Conformance 3871/3871, 0 regression**
(+2 goldens). NOTE — this is the LAST batch to add legacy `GreenfieldOnly` exclusions: see Entry 997 (owner decision
to stop gating on the legacy differential).

