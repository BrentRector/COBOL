## Entry 823 — 2026-07-11 20:44 PDT — P7 Step 10m: `Verbs/SetBinder` + `Verbs/SearchBinder`

**What (the AS-BUILT PLAN's 10m batch).** `SetBinder` — the 13-format SET dispatch with every
CONTRACT-ORDER semantic re-route preserved verbatim: the F10 pointer peek FIRST (`host.Ptr.TryBindSetUpDown`
— the tri-state contract from 10g), the F14 CAPACITY-register peek UPSTREAM of ResolveReceiving (the
non-consuming `CapacityRegisterFor` probe — never `refs.Resolve`, the OCCURS-DYNAMIC review-#7 lesson moved
with its comment), the F4-before-F5 pointer/object-reference discrimination on the FIRST target, switches
via `host.SwitchBindSet` (10n retargets), objects via `host.OoBindSetObjectRef` (10s). `SetTargetOf` moved
INTO SetBinder with a host forwarder for ControlFlowBinder's VARYING targets (flips at 10t);
`ResolveReceiving` internal (the hoisted receiving spine). `SearchBinder` — SEARCH/SEARCH ALL on the shared
`BoundSearch` machinery over `ctx.Symbols.IndexCellOf` (scope-aware, method cell first) +
`OdoModel.SearchBound(table, ctx.Refs)`. One compile catch: `OoExtractBareReference` is STATIC — the
host-instance retarget corrected to the type-qualified call.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

