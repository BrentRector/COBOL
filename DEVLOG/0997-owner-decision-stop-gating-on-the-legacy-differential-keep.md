## Entry 997 — 2026-07-22 22:46 PDT — Owner decision: STOP GATING on the legacy differential (keep the engine for the P14 equivalence proof only)

Owner asked, mid-fix-queue: "Why are we still comparing to the known-buggy legacy compiler? Is it time to abandon
it?" — prompted by four `GreenfieldOnly` legacy-exclusions added today (CA31/CA32/CA1/CA2). The honest answer: the
legacy's CORRECTNESS-ORACLE role is already dead by the project's own doctrine — goldens are SPEC-derived now
(`feedback_spec_first_only_priority`), not legacy-copied, and the spec-first fix-queue specifically targets bugs the
FROZEN legacy oracle SHARES, so the byte-compare gives near-zero correctness signal and pure friction (a growing
GreenfieldOnly skip-list). The authoritative regression net is the greenfield golden corpus + the GnuCOBOL EXTERNAL
differential (an independent implementation, not our lineage). The ONE remaining legitimate use of the legacy is the
P14 Step-0 guard-equivalence proof (exit criteria 5–6 — prove `greenfield-guard.sh` covers `guard.sh` before severing
it), which needs the engine PRESENT but not GATED-ON.

**Owner chose: "Stop gating, keep engine."** Implemented:

- **Legacy Integration `ConformanceTests.Conformance` (the shared-corpus differential — the GreenfieldOnly friction
  source) is now OPT-IN.** `Cases()` yields a single no-op sentinel unless `COBOLSHARP_LEGACY_DIFFERENTIAL=1` is set
  (for the P14 equivalence proof / on-demand). Default gate: the theory runs 1 sentinel in 3 ms instead of compiling
  hundreds of programs through the frozen engine. The legacy Integration suite stays green (503 pass / 1 skip, 21 s).
- **No new `GreenfieldOnly` entries going forward.** The existing ones + `LegacyDivergent` stay for the opt-in run.

**What was ALREADY decoupled (verified while implementing — the P0 rearchitecture anticipated this):** (1) the
greenfield `*DifferentialTests` (302 cases across 44 files) route through `DifferentialGolden`, which in the DEFAULT
`COBOLNET_DIFF_MODE=golden` compares COBOL.NET to COMMITTED goldens under `tests/differential/` and NEVER runs the
legacy engine (explicitly G8-safe); (2) the CI `legacy-oracle` verify job (`COBOLNET_DIFF_MODE=verify`, live-legacy
cross-check) already runs NIGHTLY/manual only, never on a PR. So the legacy Integration `Conformance` theory was the
LAST required-gate consumer of the live legacy engine — now closed.

**Residual (documented, not friction):** the greenfield differential goldens are legacy-PROVENANCE (baked once from
legacy where legacy == spec). If a future fix corrects a shared bug that a differential golden happens to encode, that
ONE golden is re-baked to the spec-correct value as part of that fix (a per-case re-baseline like a characterization
snapshot) — NOT a blanket tax. The current queue's fixes so far touched none (Conformance 3871/3871 stayed green).

**Kept:** the legacy engine, `guard.sh`, and the opt-in verify paths — for the P14 equivalence proof. Full legacy
deletion (byte engine + `CobolSharp.Tests.*` + the `CobolSharp.* → CobolNet.*` namespace flip) remains P15. Memories
`feedback_legacy_suite_on_shared_corpus` + `feedback_corpus_golden_manifest_registration` updated so future sessions
do not re-add GreenfieldOnly.

