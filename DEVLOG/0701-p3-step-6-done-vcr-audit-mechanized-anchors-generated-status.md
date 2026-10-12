## Entry 701 — 2026-07-08 15:41 PDT — P3 Step 6 DONE — VCR audit mechanized (anchors + generated status index + VcrDriftTests)

**PHASE 03 step 6 complete — the headline leg: `VERSION_CHANGE_REFERENCE.md` status is now DERIVED, not
hand-ticked (fixes P8).**

- **VCR transform** (a one-time verified script): dropped the hand `Status` column from all **147** change-table
  rows; replaced it with a machine anchor appended to the `Compiler gating action` cell — `<!-- gate:id -->`
  (×17, seeded from the old GATED cells' backtick ids), `<!-- todo -->` (×103), `<!-- ref-only -->` (×12,
  no-gate-obligation rows), `<!-- pin-to-spec -->` (×3). Per-table cell-count verified consistent before applying
  (Table 7 — a 4-col table with embedded `|` in backtick code — correctly untouched); the `Old → New` prose is
  byte-unchanged (the transform only edits the gating cell + drops Status).
- **Generated "Gating status index"** block (`<!-- GEN:VCR-STATUS START/END -->`, before Table 1): one row per
  VCR-anchored construct (15) — id · introduced · removed/obsolete · diagnostic · derived status · VCR row(s).
  Status is derived from the catalogue's `status` (`active`→`done`), itself fixture-gated by `VersionMatrixTests`.
- **`scripts/gen-vcr.ps1`** (thin writer) + **`VcrDriftTests`** (the renderer + 3 CI-gated facts): forward
  coverage (every `gate:id` anchor resolves to a real construct), citation-exists (every appendix specLines ref is
  within the spec's line count), and index-in-sync (regenerate == committed). Mirrors the `gen-diagnostics-doc.ps1`
  / `DiagnosticRegistryDriftTests` discipline (ONE renderer, in the test).
- Prose updated: the status-legend now describes the anchor model + the generated index; DOC_INDEX VCR row noted.

Design deviations from the phase doc's Step 6 (recorded DEVLOG 700, in the STATUS block): **no separate
`VcrStatusEmitter`/`vcr-status.json`** (it would re-run the matrix cells — redundant; derived status = the
matrix-gated `status` flag), and **forward-only coverage** (only 15/95 constructs are VCR-narrated by the doc's
own scope limit; the link is the VCR anchor, no `vcrRow` field on `constructs.json`).

No `src/` change (docs + one test + a script). **Battery:** conformance **2058** (+3 drift facts) · unit 227 ·
characterization 32 GREEN; FULL legacy guard NIST **353 MATCH**, legacy unit 1196, integration 607 GREEN.
RESUME AT step 5 (backfill rows) + the ultracode adversarial-verify pass over Steps 2 + 6.

