## Entry 838 — 2026-07-12 04:05 PDT — P7 Step 11 COMPLETE — Place.Read()/Write() DELETED; the R5 neutrality test locks G4

**The final delete.** With all eight subtypes migrated, `Place.Read()`/`Write()` (and the `RenderedElsewhere()`
tripwire, the `PlaceDecorator` inner-forwards, every subtype render stub, and `RefModPlace.WriteFill`) are DELETED —
`Place` is now a pure STRUCTURAL value (an `AccessPath` + resolved `DataItem`s + the D10-transitional index/offset
STRINGS). The `PlaceRenderer` legacy default arms (`_ => p.Read()`) became an `Unhandled(p)` ICE (unreachable — every
concrete subtype has an explicit arm). Build proved it: a delete of the abstract `Read()`/`Write()` surfaces ANY
missed caller as a compile error — there were none (all routed through `PlaceRenderer` at 11a, the binder-side leaks
fixed at 11e).

**R5 neutrality test (DESIGN-codegen-backend §6).** `PlaceNeutralityTests.PlaceHierarchyDeclaresNoStringReturningRenderMethod`
reflects over the `Place`/`AccessPath`/`AccessSegment` hierarchy and asserts NO string-returning render METHOD is
declared (property getters — the D10-transitional string carriers — are allowed; the ban is on render methods). This
keeps G4 enforced even without a live CIL backend: the bound tree is backend-neutral, so a future `CilBackend` can
consume it.

**⛔ STEP 11 (structural `Place` + `PlaceRenderer`) IS COMPLETE.** The highest-risk step of PHASE-07 landed byte-neutral
across the whole run: characterization 33/33 byte-exact (+ the RuntimeApi ratchet), unit 282/282 (incl. R5),
conformance 3166/3166. The bound tree no longer carries `Place.Read()/Write()` C# text; `CodeGen.PlaceRenderer` is the
sole owner of `Place`→C# rendering. Landed as 11a (seam+routing) · 11b (Renames/Odo) · 11c/d (RefMod/NumericImage) ·
11e (the leaf tier + forced binder-leak fixes) · 11-final (delete + R5). ONE documented deferral: the subscript /
ref-mod / RedefView-offset INDEX expressions stay transitional STRINGS on the structural nodes, folding into the D10
SUBSCRIPT-mode removal (PHASE 15). RESUME AT **Step 12** — FUNCTION args parse as `arithmeticExpression` + delete the
`IntrinsicRenderer` static channel (a shared `.g4` change ⇒ FULL legacy guard).

