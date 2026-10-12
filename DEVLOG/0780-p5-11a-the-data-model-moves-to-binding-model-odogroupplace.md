## Entry 780 — 2026-07-11 13:13 PDT — P5.11a: the data model moves to `Binding/Model/`; `OdoGroupPlace` folds into `Place.cs` under the new `PlaceDecorator` base

**What.** PHASE-05 Step 11a (DESIGN-data-model §2.2 item 1 + §6 file map; DESIGN-module-topology rows 19–22):
- `git mv` `DataItem.cs` / `PicInfo.cs` / `Place.cs` / `RedefinesModel.cs` / `FileModel.cs` / `Condition88.cs` →
  `Binding/Model/`, namespace `CobolNet.Binding.Model` (joining the already-there `StorageForm.cs` / `RecordLayout.cs`
  / `SymbolTable.cs` / the P6 bound model). `OdoModel.cs` SPLIT: the model half (`OccursSpec`, `OdoModel` static) →
  `Model/OdoModel.cs`; the `DataBinder` ODO partial (OdoResolve/DynamicResolve) → `Binding/DataBinder.Odo.cs` — the
  old file mixed a pure model with a binder pass, exactly the layering the Model/ folder exists to separate.
- `using CobolNet.Binding.Model;` blanket-injected (65 consumer files, script); 2 fully-qualified stragglers
  (`Binding.PicCategory` ×3, `Binding.FileModel`) compiler-caught and repointed; the Model files' self-usings pruned.
  Child-namespace lookup does the rest (a `CobolNet.Binding.Model` file sees `CobolNet.Binding` without a using).
- **`PlaceDecorator`** (`abstract record PlaceDecorator(Place Inner) : Place`) lands in `Place.cs`, forwarding
  `Pic`/`Item` AND (as the overridable default) `Read`/`Write`. `NumericImagePlace`, `RefModPlace`, and the moved-in
  `OdoGroupPlace` derive from it — `OdoGroupPlace` sheds all four forwards (its Read/Write WERE the plain forwards;
  the GR8 seams `LengthExpr`/`SendingImage`/`ReceiveInto` stay beside), its `Inner` widening `MemberPlace`→`Place`
  (no consumer needed the narrower type; C# pass-through positional records need name+type identity with the base).
- **DEVIATION (recorded in DESIGN §2.2):** `RenamesPlace` — on the design's derive list — STAYS direct: it composes
  N spanned leaves (no single inner) and its `Pic`/`Item` are the level-66 ALIAS's own (§13.18.45), so deriving it
  would mean overriding every forwarded member — inheritance without reuse.

**Verify.** Sln Debug + Release clean (no CS0108/hiding warnings — the pass-through check); 3166 conformance ·
281 unit · 32 characterization byte-exact; legacy guard MATCH (greenfield-only move, guard per standing rule).

