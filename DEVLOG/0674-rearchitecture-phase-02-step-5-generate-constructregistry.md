## Entry 674 — 2026-07-07 21:26 PDT — Rearchitecture PHASE 02 step 5 — generate `ConstructRegistry.Entries` + `Constructs.*` + `GateId` from `constructs.json`

`tests/version-matrix/constructs.json` is now THE single source for the registry (fixes P7): the 91-row hand-written
`Entries` array is deleted; `ConstructRegistry` is a `partial` whose Entries half is generated. Mirrors the reserved-words
discipline — a manual `scripts/gen-constructs.ps1` emits three COMMITTED build-outputs, guarded by the (repurposed)
`ConstructRegistryDriftTests` (not an MSBuild hook; a stale `.g.cs` is a CI failure, per the doc's "follow the existing
choice for ReservedWords.Table"). Chose the reserved-words pattern over the doc's default MSBuild-`BeforeTargets` because
`constructs.json` is a *test* asset and a src project reading it at build time is a layering smell.

**Enrichment (faithful by construction).** `constructs.json` lacked the C# registry's `display` / `citation` /
`diagnosticCode` (its `vcr` is a *different* doc-citation, not the diagnostic `Citation`). A one-time Python extractor
parsed the current hand-written Entries and **self-verified by re-emitting each row and byte-comparing to the original**
(0 mismatches over 91 rows) before injecting the three fields — so the enrichment provably matches. The only other
constructs.json change is Unicode-escape normalization on 2 descriptions (semantically identical; not drift-checked).

**Generated (all committed, `src/Cobol.Net.Editions/`):** `ConstructRegistry.g.cs` (the Entries partial — codes emitted as
literals, byte-equal to the `EditionCodes.*` consts); `Constructs.g.cs` (one `public const string PascalId = "kebab-id"`
per row — P2.6 turns magic-string ids into compile-checked consts); `Gating/GateId.cs` (the introduction-gate identity
enum — 54 members where `removedIn==null && introducedIn>85` — + a `GateIds.ConstructId` map, ready for P2.7 predicate
stamping). The drift test now guards ALL generated fields (display/citation/diagnosticCode) + Constructs coverage (no
orphans) + GateId ↔ introduction-gate set equality, so an edit to the json without a regen fails CI.

Byte-stable, greenfield-only: conformance **2036** (the generated Entries produce byte-identical diagnostics) · unit **224**
(222 + 2 net from the expanded drift tests). Build 0/0.

