## Entry 771 — 2026-07-11 10:48 PDT — P6 Step 6: the completion-phase watermark gate — "read a fact before its producing pass ran" is now a loud, located DEBUG error

The DAG assert guards the declared LIST; the new watermark guards what actually RAN. `DataBinder` gains
`Watermark` (the highest `PassPhase` produced on THIS binder) + `MarkProduced` + `Require(phase, fact)` —
`[Conditional("DEBUG")]`, THROWING (not `Debug.Assert`) so the failure is loud AND testable under the unit
harness; Release strips the call sites and keeps only the always-on construction-time DAG validation.
Advancement: `BindResolve` marks after each resolve pass (and Requires before — so a pass invoked out of order
on a binder trips even when the LIST is valid); `BinderDriver.Bind`'s tail loop Requires-then-marks EVERY binder
of the group per group pass (`RequireAll` is itself `[Conditional("DEBUG")]` — zero Release cost). The flagged
late-fact read point got its structural guard: the `CapacityRegisters` GETTER Requires `OccursResolved` (the
"read a null CapacityRegister" miscompile class); `OdoModel`'s own mid-pass dup-check reads the backing field.
`GroupBindContext.AllBinders()` becomes THE one group-forest enumerator (StorageFormPass's private copy now
forwards). `Tier`/`ClassOffset`/`Storage` item-level guards are NOT realizable without a DataItem→binder backref
— covered by pass-entry Requires instead; noted as P7 scope when `Place` gets structure.

`WatermarkTests` (4): Require-before-produced throws with the located message; after-produced passes;
CapacityRegisters-before-OccursResolved trips; and an end-to-end bind advances every unit binder to
`EditionConformanceChecked`. The gate never fires on a real program: the full 3158-program conformance suite runs
DEBUG with the gate live — all green, snapshots 32/32 byte-identical, unit 276 (+4), legacy guard green.

