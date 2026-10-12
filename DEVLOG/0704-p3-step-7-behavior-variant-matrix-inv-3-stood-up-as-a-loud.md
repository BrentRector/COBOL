## Entry 704 — 2026-07-08 17:21 PDT — P3 Step 7 — behavior-variant matrix (INV-3) stood up as a loud discovery tool

**PHASE 03 step 7 — the weakest leg of "four compilers in one": does the SAME source produce different OUTPUT
across editions?** Stood up `VersionBehaviorMatrixTests` — `[Theory][MemberData(Cells)]` over `constructs.json`
rows carrying a `variant` block × their valid editions — using the SAME mechanism + catalogue as
`VersionMatrixTests` (per the owner-corrected singular-pattern principle: reuse the existing mechanism, don't fork).
Each cell runs the case at its edition via a new `EditionHarness.CompileAndRun` (compile→run→stdout, reusing the
golden runner's `CutRunner`); a `confirmed:true` variant asserts stdout == `variant.outputs[edition]`, a
`confirmed:false` candidate is catalogued LOUD (must carry a `confirm` note) — the case still runs at every edition
(it must compile+run), only its output's edition-dependence is left unasserted until confirmed. A
`BehaviorVariantCatalogue_IsWellFormed` `[Fact]` keeps the "zero confirmed variants yet" state loud, not an empty
surface.

Seeded ONE cited pending candidate: `arithmetic-intermediate-precision-2023` (compile-time arithmetic mode became
implementor-defined at 2023 — Annex E.2 item 6 / VCR row 12), a documented behavior-change suspect. No confirmed
edition-variant behavior exists yet (DEVLOG 517 found the investigated de-sign/DISPLAY diffs version-INVARIANT;
COBOL.NET's one scaled-integer numeric pipeline makes arithmetic edition-invariant), so the matrix is a DISCOVERY
tool that catalogues candidates for investigation, not a gate — exactly its design intent. When a candidate is
confirmed, it flips `confirmed:true` + populates per-edition `outputs` and becomes an asserted golden.

Data + test only (constructs.json 98 rows; regenerated registry; `EditionHarness.CompileAndRun`;
`VersionBehaviorMatrixTests`). ⚠ Self-inflicted transient: after `gen-constructs.ps1` I ran the unit suite
`--no-build` against a pre-regen `Editions.dll` → a spurious `ConstructRegistryDriftTests` "json-only" failure;
rebuilt the solution → green (reminder: a `gen-constructs` regen feeds the SHARED Editions assembly — full rebuild
before any `--no-build` run). **Battery:** conformance **3097** · unit 227 · characterization 32 GREEN; FULL legacy
guard NIST **353 MATCH**, legacy unit 1196, integration 607 GREEN. RESUME AT step 9.

