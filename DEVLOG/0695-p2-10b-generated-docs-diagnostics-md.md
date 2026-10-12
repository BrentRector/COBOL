## Entry 695 — 2026-07-08 12:12 PDT — P2.10b — generated docs/DIAGNOSTICS.md + DiagnosticRegistryDriftTests (exit criteria 4 + 7)

**PHASE 02 step 10 (part b — the doc + the drift guard).** Binds the P2.10a catalogue to code + docs:

- `docs/DIAGNOSTICS.md` (NEW, GENERATED) — a 49-row table (Code · Id · Severity · ISO § · Suppress key ·
  Title) rendered from `DiagnosticCatalog.All`. It makes the old `COBOLNET0899` mess VISIBLE: 40 rows share
  the code, cleanly split into the deferred-feature half (suppress key `recognized-not-implemented`) and the
  semantic-validation half (suppress key = the code); the 3 `COBOLNET1533` rows show the reused-code split.
- `tests/Cobol.Net.Tests.Unit/DiagnosticRegistryDriftTests.cs` (NEW, 3 facts): (1) every descriptor `Id` is
  unique; (2) NO bare `.Error("COBOLNET0899"…)` / `.Error("COBOLNET1533"…)` literal survives in the compiler
  (a source scan — the split is complete, so a future re-introduction fails CI); (3) `docs/DIAGNOSTICS.md` is
  regenerated-and-in-sync with the catalogue (editing the catalogue without regenerating the doc fails CI).
- `scripts/gen-diagnostics-doc.ps1` (NEW) — runs the drift test with `COBOLNET_WRITE_DIAGNOSTICS_DOC=1` so it
  WRITES the doc (the renderer lives in the test because the catalogue is C#; the same test asserts in CI). The
  ONE source is the C# catalogue → the doc, mirroring the reserved-words / constructs generator discipline.
- `docs/DOC_INDEX.md` — the `DIAGNOSTICS.md` LEDGER row added.

Satisfies PHASE-02 exit criteria 4 (`DiagnosticRegistryDriftTests`) + 7 (`docs/DIAGNOSTICS.md` generated +
in-sync + a DOC_INDEX row). No source/behavior change — tests + docs + a script only. **Battery:** unit **227**
(224 + the 3 new drift facts) GREEN; conformance/characterization/guard unaffected (no `src/` behavior change).
This completes PHASE-02 step 10; step 11 (phase close) is next.

