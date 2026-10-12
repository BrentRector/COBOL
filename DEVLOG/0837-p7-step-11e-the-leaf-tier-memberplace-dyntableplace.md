## Entry 837 — 2026-07-12 03:40 PDT — P7 Step 11e: the LEAF tier — MemberPlace/DynTablePlace/RedefViewPlace/CapacityRegisterPlace go structural (the AccessPath)

**What.** The four leaf `Place` subtypes lose their C# path strings for a structural `AccessPath` (a chain of
`RootFieldSegment`/`MemberSegment`/`FixedTableSegment`/`DynTableSegment` — `Binding/Model/AccessPath.cs`), rendered by
`CodeGen.PlaceRenderer.RenderPath` (field chain · `CobolTable.At` · `RefSending`/`RefReceiving` chosen at render time
from the read/write direction — the former `DynTablePlace` two-string polarity). `MemberPlace(AccessPath)`,
`DynTablePlace(AccessPath)` (kept distinct, not folded — bounds the blast radius; `UsageCollectionPass`/`MoveEmitter`
discriminate it by type), `RedefViewPlace(AccessPath Backing, …)`, `CapacityRegisterPlace(AccessPath Table)`. All eight
`Place` subtypes are now structural; the subscript INDEX + the RedefView offset stay the D10 transitional string.

**The atomic web (one commit — the sites consume the leaves by string).** `ReferenceResolver` grows `BuildAccessPath`/
`BuildTablePath`/`BuildBackingPath` (structural twins of the string builders, kept for the transitional subscript
render); `AccessDir` moved to `Binding/Model`. `CorrespondingBinder.CorrAccess` builds child places structurally and
the sniff `m.Path.Contains("CobolTable.At(")` → `m.Path.HasIndex`; the anchor hoist `CorrespondingHoist(string Init)` →
`(Place? RefGroup, string? LongInit)` — a BOUND-NODE change the emitter renders via `PlaceRenderer.Read`.
`InitializeBinder`'s three cursors accumulate `AccessPath` segments (the dead-looking `InitializeDynCursor` is LIVE —
whole-dynamic-table INITIALIZE). `ProgramEmitter.PrefixPlace` re-roots via `AccessPath.Reroot`. `MoveEmitter` renders
the dynamic receiving store via `RenderPath`.

**Two binder-side C#-string LEAKS the throw forced into scope (I'd planned to defer them, but a migrated `MemberPlace`'s
`Read()` now throws):** `BoundSetCapacity(string TablePath)` → `(AccessPath Table)` (SET Format 14 emitter renders
it), and `BoundSearch(string? DependCount)` → `(Place? DependItem)` — `OdoModel.SearchBound` (a `dp.Read()` on the
depending place) became `SearchDepending` returning the `Place`, and `ControlFlowEmitter` renders `CobolTable.Occ(...)`
/ the dynamic `.Capacity` bound. The `DynTable`/`OffsetExpr`/subscript strings stay transitional (D10).

**Debugging trail (transparency).** First run: 19 conformance + 1 unit red (SEARCH-over-ODO hit the `dp.Read()` throw
— the deferred leak). After the `SearchDepending` fix: 4 OO red — `OoEmitter`'s direct-ref fast path interpolated
`{mp.Path}` (a raw field access, not a `.Read()` call, so the 11a routing missed it), emitting the `AccessPath`
record's `ToString()` into the generated C#; routed to `PlaceRenderer.Read(mp)`. Characterization stayed byte-exact
throughout — the 14 snapshots don't exercise SEARCH-over-ODO or the OO ref path, so conformance/unit caught both.

**Battery green:** characterization 33/33 (byte-exact + ratchet), unit 281/281, conformance 3166/3166. NEXT (11-final):
delete `Place.Read()/Write()` + the `RenderedElsewhere()` tripwire + the `PlaceRenderer` legacy default, and add the
R5 neutrality reflection test.

