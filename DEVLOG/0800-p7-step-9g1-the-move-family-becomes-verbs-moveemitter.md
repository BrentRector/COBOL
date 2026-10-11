## Entry 800 — 2026-07-11 16:48 PDT — P7 Step 9g1: the MOVE family becomes `Verbs/MoveEmitter` (mechanical split; 98 = 74 + 24 proven by the ratchet)

**What (the phase doc's 9g, first half — the extraction split from the façade routing to keep each commit
small-blast-radius).** The MOVE family (`EmitMove` per-kind renderer · `EmitFigurativeToNumericImage` ·
`EmitGroupToElementaryMove` · `EmitGroupMove` + `AlignedLeafPairs` · `AllDigitFill` · `IsNumericOperand` ·
`ConvertSource`, ~340 lines) moves out of the orchestrator into `Verbs/MoveEmitter.cs` — a real collaborator
over (ctx, num, refs); it needed NO host reference at all (its former cross-partial edges — `Narrow`/`BwzFlag` —
are internal statics; CS9113 flagged the over-provisioned ctor again). The host keeps TWO one-line forwarding
shims (`EmitMove`/`ConvertSource` → `_move`) so the six collaborator callers, the Dispatch visit, and Report
Writer's Convert edge stay untouched until the 9n composition root retargets them. Ratchet re-keyed:
`CSharpEmitter.cs` 98 → 74 and `Verbs/MoveEmitter.cs` = 24 — summing exactly to the old pin, proving the split
moved text without adding a single bare access. 9g2 routes the 24 through `RuntimeApi`.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

