## Entry 749 — 2026-07-10 18:55 PDT — PHASE 05 Step 2 (D0): StorageForm parallel SSOT + StorageFormPass + corpus equivalence proof (prove-then-delete)

Landed PHASE-05 Step 2 — the DESIGN Phase-D0 "prove" half. `Binding/Model/StorageForm.cs` is the closed
value-representation discriminator (9 cases: `NativeInt`/`NativeFloat`/`CharImage`/`TierBWindow`/`TierCWindow`/
`DynamicTable`/`ObjectRef`/`PointerRef`/`IndexCell`, abstract `IsCharacterImage`/`ImageWidth`). `Binding/Passes/
StorageFormPass.Compute(DataBinder)` assigns `DataItem.Storage` to every elementary item ONCE, wired into
`CSharpEmitter.CallBindRunUnit` right after `OoHarmonizeOverrideCrossings` — the FINAL post-procedure-bind /
post-`MarkStoreAsImage` / post-OO-harmonize state where every `StoreAsImage` flag is settled. Nothing reads `Storage`
yet (a temporary MUTABLE field on `DataItem`, init-only from Step 10); it runs in PARALLEL with the legacy flag,
deleting nothing.

**Prove-then-delete (exit criterion 3).** `StorageFormEquivalenceTests` drives the FULL `emitter.Bind` path (NOT the
bare `DataBinder.Bind`, which skips the middle-end) over the NIST corpus + 5 crafted tricky-path programs (whole-group
MOVE promotion, fixed-OCCURS-under-whole-group, SIGN SEPARATE width, mixed native usages, REDEFINES view) and asserts
`StorageFormPass.Verify` finds ZERO divergences — the four identities per item: (1) `Storage is CharImage{Numeric}` ==
`StoreAsImage`, (2) `IsCharacterImage`, (3) `ImageWidth`, (4) `ElementType`, each DERIVED from `Storage` vs the legacy
recursive property. In D0 identity #1 is tautological (Compute reads the settled `StoreAsImage` for the numeric-image
promotion — the from-scratch whole-group union is Step 7's delete-phase work); the VALUE is that #2/#3/#4 hold once
`Storage`, not the scattered flag, is the source — proving the model before any deletion.

**Design amendment (DESIGN-data-model §2.1, same change set per process rule 4, from the recon `wf_911ad088-76f`):** the
D0-locked `NativeInt(Wide, Digits)` / `NativeFloat(Single)` shapes carried no separate-sign width, so their `ImageWidth`
could not reproduce `DataItem.ElementaryImageWidth`'s +1 for SIGN IS SEPARATE (§13.18.52) — load-bearing even for a
NON-promoted native leaf (a group sums its native children's widths). Amended to carry a precomputed `int Width`
(= digits + separate-sign) on `NativeInt`/`NativeFloat`/`IndexCell`.

**Behavior-neutral:** `Compute` runs on all 3157 conformance programs without crashing; greenfield conformance 3157 ·
unit 254 (+6) · characterization 32 byte-exact (Compute is inert — no emit change) · FULL legacy guard NIST 353 MATCH
(greenfield-only change). Parity traps the recon flagged + verified clean: the Tier-B numeric-leaf CharImage-vs-window
split (consult `StoreAsImage` before `TierBWindow`), off-Roots report print items, the LINKAGE double-visit
(reference-dedup, no `Concat(LinkageRoots)`), and groups carry NO Storage (recursive `Children.All` / redefiner-filtered
width sum).

**RESUME AT: PHASE-05 Step 3** — the `IBindPass` pipeline scaffolding + `BindPipeline.ValidateDag` startup assert
(no-op wrappers, zero reorder, zero behavior change).

