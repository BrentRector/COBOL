## Entry 803 — 2026-07-11 17:16 PDT — P7 Step 9h2: ArithmeticEmitter's 40 bares → RuntimeApi (18 rewrites; residue 10 typed) — and the batching decision

**What.** The arithmetic fragments route through the façade — `RuntimeApi` grew `NumDivide` (the Divide/
DivideOrThrow checked-context pair, anchored like `BoolOpName`), `NumTryStore`/`NumStoreRounded` (the §14.7.5
checked/unchecked store funnel pair), `DecToUnscaled` (the SDIDI landing), `FloatInexactAtScale` (the ROUNDED
PROHIBITED gate), `EditTryFormat` (the §14.7.5-case-3 capacity-checked edit), `BoolResize`, and `NumRescale`
gained the checked-path flag. Pin 40 → 10 (compile-time `MaskScale` ×2 + typed `CobolRounding`
comparisons/arguments). The `fn` string-literal method-name selection ("DivideOrThrow"/"Divide") is GONE —
the façade's `nameof` pair replaces the last stringly-typed kernel-name choice in arithmetic.

**Owner directive (this entry forward): BATCHED sub-commits.** The battery cycle, not the edit, is the
bottleneck; the gates (byte-exact snapshots + conformance + the ratchet's exact-sum accounting) hold identically
for a batch. Remaining Step-9 work proceeds as three batches (9i+9j · 9k+9l · 9m+9n), one battery cycle and one
commit each; Step 11 keeps its per-subtype gating (the phase doc mandates it); Step 12 keeps the full legacy
guard.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

