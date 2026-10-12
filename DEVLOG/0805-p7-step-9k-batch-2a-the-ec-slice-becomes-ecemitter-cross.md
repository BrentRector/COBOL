## Entry 805 — 2026-07-11 17:38 PDT — P7 Step 9k (BATCH-2a): the EC slice becomes `EcEmitter` (cross-cutting, not a verb — CodeGen/ root)

**What.** The whole Exceptions partial — the `BoundEcChecked`/RAISE/RESUME renderers, the EC-SIZE/OVERFLOW/I-O
services, and the generated `__EcDispatch`/`__EcObjDispatch`/`__IoCheckEc` machinery — moves to
`CodeGen/EcEmitter.cs` over (ctx, EcState — now OWNED where the phase doc said it would land — DispatchState,
host). The partial file survives as the host-side shim sheet: the bind-session `TurnState` field (written by
`BeginBind`/`CallEmitRunUnit`, read by the OO bind half — it stays host state until P9) plus the eight
forwarding shims the not-yet-extracted collaborators reach the EC services through. The two `CobolFile`
status/open-mode fragments inside `__IoCheckEc` routed through the façade — EcEmitter lands at 0 bare
fragments and the Exceptions entry (2) is DELETED. Host `EmitStatement` widened internal (the checked-wrapper
recursion edge).

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

