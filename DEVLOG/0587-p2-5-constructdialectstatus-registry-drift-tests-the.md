## Entry 587 — 2026-07-03 18:45 PDT — P2.5 ConstructDialectStatus registry + drift tests — the metadata is FROZEN

**The P2.5 registry is live** (`Validation/ConstructDialectStatus.cs`): the in-code rendering of the (freshly
scrubbed) canonical `constructs.json` — 21 entries incl. the 4 pending seeds — with `StatusAt(edition)`
(NotYetIntroduced / Removed / Obsolete / Available) and **`ConstructRegistry.Check(edition, id, where)` as THE
one gating entry point**: NotYetIntroduced ⇒ error on BOTH axes (0900 band, or the pinned legacy code —
pic-wide-19 carries its live 0802); Removed ⇒ `EditionContext.Removed` (strict error / permissive warning);
Obsolete ⇒ 0903 warning. Every P2.6 gate (validator or binder-side) routes here — one policy, several emit
sites. **Both drift disciplines now hold:** `ConstructRegistryDriftTests` asserts registry↔constructs.json
equality BOTH directions (+ the optional `expectDiagnostic`↔`DiagnosticCode` tie), so a gate cannot land
without its matrix row nor a row without its registry entry; `ReservedWordsDriftTests` (585) covers the word
tables the same way. `Check_RoutesVerdicts_OntoTheChannels` pins the channel routing incl. the
permissive-independence of introduction gating and the loud unregistered-id throw. Unit 38/38; conformance
1069/1069 (registry addition is behavior-neutral until the P2.6 gates call it).

