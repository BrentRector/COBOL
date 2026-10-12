## Entry 697 — 2026-07-08 12:38 PDT — P3 Step 0 — AS-BUILT reconciliation recorded in the docs (what PHASE 03 already has vs what remains)

**PHASE 03 opened; Step 0 (baseline + AS-BUILT reconciliation, no code).** The PHASE-03 doc was authored
BEFORE P2 executed, so several of its steps were already landed by P2 + the bind-time gating migration. Ran
the recon (greps over `EditionValidator`, the inline-gate codes, `ConstructRegistry.Check` sites,
`constructs.json` fields, the VCR, the existing test set) and **recorded the full reconciliation table in the
PHASE-03 doc's STATUS block** so no built work is redone. Findings:

- **Step 1 (P2 framework surface) ✅** — `EditionInfo`/`IDiagnosticSink`/`EditionSeverity(Policy)`/
  `EditionDiagnostic`/sink-based `ConstructRegistry.Check`/`Constructs.g.cs` all present.
- **Step 4 (fold the 5 inline gates 0816/0803/0810/0811/0882) ✅** — done in P2.6b; zero bare literals remain.
- **Step 2 (re-home `EditionValidator`) ◑ PARTIAL** — its `Check` calls already pass `EditionInfo`+sink, but the
  ctor is still `EditionValidator(EditionContext)`; the 2 `VisitCobolWord` direct writes + the driver hook
  remain. **← the current step.**
- **Step 3 (enrich `constructs.json`) ◑ PARTIAL** — 95 rows, but missing `expectDiagnostic`(+`Below`)/`variant`
  on most; some `status:"?"`.
- **Steps 5–10 ❌ NEW work** — backfill 85→2002/2002→2014 rows; mechanize the VCR audit (117 hand-`TODO` rows →
  `gen-vcr.ps1` + `VcrStatusEmitter` + `VcrDriftTests`); the behavior-variant matrix (INV-3); the in-process
  continuity + INV-1-strong-2023 gates; corpus discovery runners + 2014 seeds; loud hole cataloguing
  (SYNCHRONIZED-on-group emits a generic `COBOL0001` today).

Also noted: P2.10's `DiagnosticCatalog` + `docs/DIAGNOSTICS.md` already back the Step-10 "loud, not generic"
intent, and `EditionGateHints`→`ReservedWordEditionHints` (the reservation-word residue). Exit-criterion-6
nuance recorded: the validator will depend only on the `EditionInfo`+`IDiagnosticSink` TYPES; the concrete sink
may stay the `EditionContext` collector (P2 kept it, Q5).

Baseline (green, from the PHASE-02 close `61248d88`): conformance 2055 · unit 227 · characterization 32; FULL
legacy guard NIST 353 MATCH. Docs synced: PHASE-03 STATUS block (the table), the master roadmap banner,
`resume-prompt.md`, and the memory index — all now say PHASE 03 IN PROGRESS @ step 2. No code change.

