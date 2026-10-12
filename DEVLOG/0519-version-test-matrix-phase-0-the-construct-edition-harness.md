## Entry 519 — 2026-06-09 17:22 PDT — Version test matrix Phase 0: the (construct × edition) harness, proven end-to-end

Owner resolved the design's open decisions: removed/obsolete construct → ERROR under strict `--standard cobolNNNN`,
WARNING under permissive (legacy `FlagsFeaturesRemovedAfter85` policy); the DEFAULT `--standard` (no flag) →
**COBOL-2023** (differs from the legacy strict-85 default); build Phase 0 now. Recorded in
`docs/VERSION_TEST_MATRIX_DESIGN.md` §10.

Adversarial review (advisor) caught two design gaps, fixed in the doc before building: (1) INV-1 continuity excused
only REMOVED constructs — it must also excuse a COBOL-85 program using a later-RESERVED word as a user name (Annex
E.3.2). This is LIVE: `RECEIVE` is a data name in 4 NIST-85 programs and a new 2023 reserved word (ref-doc Row 32), so
those must compile at 85/2002/2014 and be rejected at 2023 — the naive form would mis-flag them as regressions. (2)
The catalogue must source edition metadata from THREE places, not just the ref doc — the 85↔non-85 boundary (the
owner's #1) is exactly what the 2023 Annex E covers least; harvest `introducedIn` from the greenfield grammar
`is2002/is2014/is2023` gates and `removedIn`/85↔2002 from the legacy `FlagsFeaturesRemovedAfter85`/`DialectStrictnessChecks`/FLAG-02.

**Phase 0 built (`VersionMatrixTests.cs`):** the matrix `[Theory]` over (construct × {85,2002,2014,2023}) with the
computed `f(case,V)` (compiles iff `introducedIn ≤ V < removedIn`) + an inline seed catalogue, and the INV-1
continuity `[Theory]`. Proven end-to-end on current greenfield capability:
- **Introduction-gating both directions:** DELETE FILE (introduced 2023, grammar `is2023()`) is REJECTED at
  85/2002/2014 and COMPILES at 2023. (DELETE FILE replaces the design's ALTER example — ALTER is only a lexer token,
  no greenfield statement rule; removed-construct gating awaits the Phase-2 EditionValidator.)
- **Continuity (INV-1):** NC101A compiles at 2002/2014/2023; NC211A and NC136A compile at 2023 (real COBOL-85 NIST
  programs, green at 85, still compiling later).
- **Nucleus** (MOVE/DISPLAY) compiles at every edition.

13 matrix cells green; conformance 294 → 307; 14 unit. Greenfield-only; doc + test only.

**Immediate next step (decision #2, recorded not yet done):** flip `CompilerDriver.Options.DialectLevel` default
85→2023 with a regression test (the test harnesses pass the edition explicitly, so only the CLI no-flag path changes);
then Phase 1 — the canonical `constructs.json`, the greenfield diagnostic-assertion harness, threading DialectLevel
into bind/emit, and seeding the highest-value reference-doc rows.

