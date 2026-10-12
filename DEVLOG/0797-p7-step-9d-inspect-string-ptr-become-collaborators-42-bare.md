## Entry 797 — 2026-07-11 16:27 PDT — P7 Step 9d: Inspect · String · Ptr become collaborators; 42 bare runtime sites RuntimeApi-routed; the ratchet catches a missed doc mention

**What (the phase doc's 9d — the counter-bearing middle tier).** Three more partials → real classes (git-mv'd):
`Verbs/InspectEmitter.cs` (over ctx+num+host), `Verbs/StringEmitter.cs` (STRING/UNSTRING; the overflow dispatch
keeps riding the host's `EcEmitOverflow`), `Verbs/PtrEmitter.cs` (ADDRESS OF/SET F7/F10/ALLOCATE/FREE — takes
`EcState` directly for the §14.6.13.1.4 nonfatal gate; `AddressOfText` stays public, the SET-pointer F7 sender
edge re-pointed). Three EC helpers went internal for the migration edges (`EcDispatchExpr`/`EcStmtLoc`/
`EcEmitOverflow`).

**Ratchet:** `RuntimeApi` grew the INSPECT surface (`InspectTally`/`InspectReplace`/`InspectConvert` + the
`nameof`-anchored tally/replace kind-discriminator maps — the BoolOpName precedent), STRING/UNSTRING kernels
(`StrTransfer`/`UnstringExtract`/`StrStoreJustified`), pointers (`PtrUpBy`/`PtrUpByScaled`/`PtrAllocate`/
`PtrFree`), and `NumFromAlphanumeric`/`NumRescale`/`RoundingText` (the ONE emitted-rounding-name anchor). The
three old baseline entries (16+17+9) are DELETED; the residues pinned (String 4 · Inspect 1 · Ptr 1) are TYPED
compile-time `CobolRounding` arguments to `StoreArith`/`NumRescale` — not emitted text; the ratchet's regex is
over-broad for them and 9-final decides the refinement (noted in the baseline comment). **The ratchet caught a
real miss:** my first PtrEmitter draft left a `CobolPtr.UpBy` doc-comment mention — 2 > the pinned 1 — proving
the full-text census works; reworded (comment-only fix, the prebuilt-binary conformance leg unaffected).

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

