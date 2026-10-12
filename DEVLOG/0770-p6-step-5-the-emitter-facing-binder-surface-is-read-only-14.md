## Entry 770 — 2026-07-11 10:35 PDT — P6 Step 5: the emitter-facing binder surface is READ-ONLY — 14 collection views sealed, the last two bind-phase bodies pulled out of CodeGen, a 3-agent audit proving zero writes

**The audit first (ultracode fan-out, 3 parallel agents over every mutable binder-model surface):** (1) all 31
DataBinder collection members are get-only auto-properties with ZERO CodeGen/Validation writers — every external
writer is Binding-side and legitimate (BinderDriver.BindUnitData/RegisterSubtree for the GLOBAL-inheritance maps,
UsageCollectionPass for WholeGroupReferenced, ReferenceResolver/StatementBinder.Oo for the OoPendingPropertyOps
queue); (2) the only DataItem/FileModel writers physically in CodeGen files were `OoUnifyCrossing`
(StoreAsImage) and `OoQualifyClassFiles` (FileModel.CobolName/InstanceKeyField) — both BIND-time code reached
exclusively through the IOoBindHost seam, i.e. misplaced bodies, not emit-time writes; (3) CodeGen READS exactly 14
collection members, every idiom IReadOnly-satisfiable. (Also surfaced: OdoModel.cs is a SEVENTH DataBinder partial;
and the OO DataBinder construction at the emitter is the known P6→P9 seam.)

**The fixes:** the two misplaced bodies moved to Binding — the override-crossing harmonize is now
`StorageFormPass.HarmonizeOverrideCrossings` (it IS a StoreAsImage settle-step, sequenced between marking and
classification) with the crossing-form predicate unified as `OoClassTable.StringCarried` (ONE definition; the
emitter's 13 signature/marshaling reads go through a thin forward — the EmitCore.AllLiteralText precedent); the
class file-connector qualification is `BinderDriver.QualifyClassFiles`. The IOoBindHost seam SHRANK to the four
genuinely-OO-bind members (BeginBind, BindInterfaceData, BindClassData, BindClassBody + the InterfaceData
accessor); GroupBindContext deliberately does NOT carry the seam — a group pass reaching for emit state is now a
compile error by construction.

**The seal:** the 14 CodeGen-read members are IReadOnly-typed views over private backings (`Roots`, `Files`,
`Reports`, `IndexFields`, `CapacityRegisters`, `LinkageFormals`, `CallGlobalRoots`, `CallExternalBackings`,
`CallSuppressedRootFields`, `OoStaticRootFields`, `OoStaticIndexCells`, the 3 Ptr members) — writing through the
public surface no longer compiles; the binder's partials write the private fields; the ONE cross-class write
channel is the new domain mutator `DataBinder.SeedInheritedGlobalIndex` (BinderDriver's GLOBAL-index preseed,
which also owns the paired suppression — the two writes were always one operation). Known bounds, documented in
the property docs: element mutability (FileModel fields, ByName's List values) is not sealed by a view type —
that is the data-model track's deep-immutability work; ByName/Conditions/IndexFields-as-written-by-binding get
absorbed by Step 7's SymbolTableBuilder. Battery: 3158 conformance · 272 unit · 32 characterization
BYTE-IDENTICAL · legacy guard green. Zero new traversals.

