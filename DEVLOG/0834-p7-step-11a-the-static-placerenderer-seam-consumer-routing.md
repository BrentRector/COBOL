## Entry 834 — 2026-07-12 01:35 PDT — P7 Step 11a: the static PlaceRenderer seam + consumer routing (byte-neutral)

**Why.** Step 11 makes `Place` structural (backend-neutral, the G4 invariant): `Place` will carry an access path +
`BoundExpr` subscripts + a ref-mod span, and the Roslyn backend's `PlaceRenderer` will own ALL the C# read/write
text that `Place.Read()/Write()` hard-code today. 11a lays the seam: create `PlaceRenderer` and route every consumer
through it, with ZERO behavior change, so the subtypes can then be converted one at a time behind it.

**Two decisions that shaped the mechanics.** (1) **`PlaceRenderer` is a STATIC class** (`CodeGen/Roslyn/
PlaceRenderer.cs`), not the sketch's `PlaceRenderer(EmitContext ctx, RuntimeApi rt)`. Every current `Place.Read()/
Write()` is context-free — self-contained on the `Place`'s own fields — and `RuntimeApi` is static, so nothing needs
threading. (2) **The migration routes consumers FIRST, then migrates subtypes** — the reverse of the "Place.Read()
delegates to a legacy shim" sketch, because that would be a **layer inversion**: `Place` is in `Binding`,
`PlaceRenderer` in `CodeGen`, and a `Binding → CodeGen` call is exactly what the seam exists to forbid. So the shim
lives INSIDE `PlaceRenderer` (its default arm calls the legacy `p.Read()`), consumers move to `PlaceRenderer.Read(p)`,
and `Place.Read()/Write()` stay until the final delete.

**What landed.** `PlaceRenderer` (Read/Write/WriteFill/SendingImage/ReceiveInto/LengthExpr, all forwarding to the
legacy `Place` methods) + ~124 routed call sites across 20 files (the 4 renderers OperandText/NumericRenderer/
ConditionRenderer/BooleanRenderer + 16 verb/spine emitters). A one-file-per-agent workflow did the bulk with
receiver-type verification against `BoundTree.cs`/`BoundOo.cs`; structural field reads (`m.Path`, `dyn.ReceivingPath`)
and non-Place `.Read()/.Write()` (CobolFile, Console, the CodeWriter, RuntimeApi) were correctly left. One site the
agents skipped for its null-conditional form — the EC EXCEPTION-OBJECT `ro.Source?.Read()` — routed by hand via an
explicit null check. **Transparency note:** my first "all consumers routed" grep false-negatived that EC site (the
line also contains `w.Line(`, which my exclude filter dropped) — caught it via the missing-from-staging signal.

**Byte-neutral, proven.** characterization 33/33 (32 snapshots byte-exact + the RuntimeApi ratchet — `PlaceRenderer`
routes through `RuntimeApi`, no bare `Cobol*.`), unit 281/281, conformance 3166/3166. No grammar touch, so the FULL
legacy guard is reserved for Step 12 + phase end (PHASE-07 §1). NEXT: migrate the no-subscript subtypes
(`NumericImagePlace`/`RenamesPlace`/`OdoGroupPlace`) to structural rendering in `PlaceRenderer`, then the index
infrastructure (subscript → `BoundExpr` + the byte-exact index renderer) for `MemberPlace`/`RefModPlace`/etc.

