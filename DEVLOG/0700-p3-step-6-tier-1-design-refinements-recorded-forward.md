## Entry 700 — 2026-07-08 15:24 PDT — P3 Step 6 — Tier-1 design refinements recorded (forward-coverage; no separate emitter)

Two build-recon findings that materially refine the Step-6 mechanization design (recorded in the PHASE-03 doc's
STATUS block so the build proceeds on the corrected design; the doc predates the build):

1. **Coverage is FORWARD only.** Only **15 of 95 constructs** are named in any VCR row — the VCR deliberately
   doesn't narrate most 85→2002 / 2002→2014 introductions (its documented scope limit). So the Tier-1 coverage
   check is "every `<!-- gate:id -->` anchor resolves to a real construct," NOT a "every gated construct has a
   row" biconditional. The link lives in the VCR anchor; there is no `vcrRow` field on `constructs.json`.
2. **No separate `VcrStatusEmitter` / `vcr-status.json`.** The plan's emitter would re-run the exact
   (construct × edition) cells the matrix theories already gate (~380 redundant compiles). The derived status is
   instead the `constructs.json` `status` flag (`active`→done / `pending`→pending) — which the matrix ALREADY
   makes fixture-verified (a row is `active` only when its cells pass, CI-enforced). `gen-vcr.ps1` renders that
   flag (ANDing multiple anchored constructs per row). One accept/reject gate (the matrix), not a parallel
   recompute engine (feedback_singular_pattern). (Speculative internal-exposure of `VersionMatrixTests.Catalogue`
   was added then reverted — the drift test reads `constructs.json` directly.)

Also: Step-3's `expectDiagnostic` enrichment is already adequate (the 3 dual-window rows carry
`expectDiagnosticBelow`; pure-intro rows use the fallback), so the only remaining Step-3 field is `variant`
(Step 7). No code change (design record + the PHASE-03 STATUS block). Remaining Tier-1 build: the VCR transform +
`gen-vcr.ps1` + `VcrDriftTests` (block-in-sync + forward-coverage + citation-exists), then the ultracode
adversarial-verify pass.

