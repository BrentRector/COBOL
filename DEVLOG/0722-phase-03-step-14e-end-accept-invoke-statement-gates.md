## Entry 722 — 2026-07-09 16:21 PDT — PHASE-03 Step 14e: END-ACCEPT + INVOKE statement gates relocated

Two more statement gates move binder→`VersionConformancePass`:

- **Invoke2002** — `is BoundInvoke or BoundInvokeUniversal` (a distinctive bound-node type; the recon ledger's Group A).
  The `OoBindInvoke` Check is removed. The synthesized property-op `BoundInvoke` (a GET/SET pre/post op) is ALSO gated
  by the pass — the known benign over-fire: property use is itself 2002+, so a below-2002 program using it is already
  rejected at the data/repository level; the extra 0900 lands on an already-errored unit. Confirmed harmless — the full
  OO suite (1999 edition/ACCEPT/INVOKE/OO tripwires) is green.
- **EndAccept2002** — new byte-neutral flag `BoundAccept.HasEndTerminator` (the END-ACCEPT scope terminator, §14.9.1,
  COBOL-2002; no semantic effect, so only presence is recorded). Computed once in `BindAccept` (`AcceptHasTerminator`)
  and stamped on each of the three ACCEPT construction paths (the mnemonic path via a `with` expression); the pass
  gates on it.

**Battery:** edition/ACCEPT/INVOKE/OO tripwires **1999**, conformance **3114**, characterization **32**, unit **227**,
FULL legacy guard NIST **353 MATCH** (0 regressions — greenfield-only). NEXT — **14f:** the multi-node/receiver flag
gates (ROUNDED MODE IS via `Receiver.ModeIsExplicit`; sequential record-lock via `Lock` on the WRITE/READ/REWRITE
nodes; RETRY via `Retry` across its verb nodes).

