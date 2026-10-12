## Entry 801 — 2026-07-11 17:03 PDT — P7 Step 9g2: MoveEmitter's 24 bares → RuntimeApi (18 fragment rewrites; residue 3 typed)

**What.** The 18 emitted-text fragments in `Verbs/MoveEmitter.cs` route through the façade — `RuntimeApi` grew
`FloatToScaled` (the D16 float→scaled landing, §14.6.8.2 truncation) and `StrStoreBoolean` (the §14.6.8.6
boolean-ZERO-pad store); the conditional JUSTIFIED stores split explicitly over `StrStore`/`StrStoreJustified`
(byte-identical text — the 32 snapshots prove each rewrite). The MoveEmitter pin drops 24 → 3; the residue is
the compile-time `CobolEdit.MaskScale` call (the compiler itself computing the mask's fraction scale — typed,
not emitted) + 2 typed `CobolRounding` arguments. Doc-comment type-prefix mentions reworded per the ratchet's
full-text census.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

