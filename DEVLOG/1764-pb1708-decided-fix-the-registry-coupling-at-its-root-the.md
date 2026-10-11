## Entry 1764 — 2026-09-28 17:11 PDT — PB1708 decided: fix the registry coupling at its root; the design fleet is dispatched

The PB1683 impact map selects 5,504–9,268 of 9,274 Conformance tests for every replayed cluster, because the static
registries (DiagnosticCatalog, DiagnosticDescriptors, ConstructRegistry) are built together and touched by almost
every test. Asked for "the best long-term, commercial-product, decades-long supportability solution", the owner
chose to fix that root cause, then: "do the design doc and work now. let's get onto the right solution. It may make
up for lost time over future runs".

The decision has three layers:
1. Modular, data-driven registries that do not initialize together.
2. The impact map recorded at ENTRY granularity: a lookup depends on its entry, and an enumeration depends on the
   whole registry. It is exact by construction and recorded automatically per main commit (PB1709).
3. The whole suite remains the permanent backstop at the lander and in CI.
Capping concurrent whole-assembly implementer gates is only the interim.

The design goes first (CLAUDE.md rule 2). Workflow wf_2a880f25-d10 has an architect measure the coupling and extend
`DESIGN-test-build-ci.md` §3.5 and §3.13, then an adversarial design reviewer, then a reviser if the review finds
blocking issues. A stall watchdog runs beside it (practice O8).

The weekly meter read 40 % at dispatch, above Monday's 28.6 % allowance and approaching Tuesday's 42.9 %. The owner
directed the work now.

