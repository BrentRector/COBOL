## Entry 791 — 2026-07-11 14:59 PDT — P7 Step 4b: the `RuntimeApi` façade seeded + the bare-`Cobol*.` RATCHET guard (Step 4 complete)

**What (Step 4 second half; DESIGN-codegen-backend §3).** `CodeGen/Roslyn/RuntimeApi.cs` NEW — the typed
`nameof`-anchored façade over the emitted runtime surface; a runtime rename now breaks ONE compiler file at
compile time instead of silently mis-emitting text. Seeded with the `CobolBool` member set and
`BooleanRenderer` fully migrated (its bare count → 0; incl. the `BoolOpName` anchor table, preserving the
original `_ => And` default shape). **The RATCHET** (`RuntimeApiGuardTests` in the Characterization project,
now 33 tests): pins each CodeGen file's bare `Cobol*.` count (19 files, 394 occurrences at landing — the audit
census confirmed) and FAILS on any INCREASE; a file not listed must be at ZERO. Reads via `File.ReadAllText`
(never a grep subprocess — the historical NUL-in-ConditionRenderer rg blindness). Step 9's per-verb rewrites
drive the baselines to zero per the doc's shrinking-whitelist plan; the guard then flips to forbid-all.
Excluded by construction: `CobolNet.` (namespace) and `CobolLiteral.` (compiler codec); typed compile-time uses
(e.g. `CobolRounding` enum values) count within the pinned baselines — the per-file migration decides their
façade treatment (the audit's policy question, resolved as ratchet-tolerance).

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots
byte-exact + the ratchet). Step 4 (a+b) and Step 5 complete — D3 done; next: D4 (Steps 7+8).

