## Entry 1904 — 2026-10-07 01:13 PDT — Train 1025: the legacy retirement's Cut 1 (PB2108 + PB2109); no greenfield test, script or CI job reaches the CobolSharp engine

**Cluster A — PB2108 (the conformance project drops its legacy oracle).** Wave 1025 group A re-probed both notes on
`a425fcb2b` and found more than they measured: six Conformance classes (`AbbreviatedCondition`, `AllLiteral`,
`ClassCondition`, `GroupData`, `MoveEdition`, `SignedAlphanumericMove` `*DifferentialTests`) still ran the legacy engine
LIVE in the default mode as a cross-check beside their spec assertion, and `windows-build-test` still ran the legacy Unit
and Integration suites. `tests/Cobol.Net.Tests.Conformance` now references no `CobolSharp.*` project: the two
`ProjectReference`s, `LegacyCompiler`, the `ICompilerUnderTest` interface (one implementation was left, so the
abstraction went with it) and `DifferentialGolden`'s `bake` and `verify` arms with their `COBOLNET_DIFF_MODE` switch are
deleted. The comparison with the committed `tests/differential/**/*.out` goldens is the only mode, and a missing golden
now says to commit a spec-derived expected stdout. Each of the six classes keeps its spec assertion through one
`AssertSpec` (MoveEdition's `AssertSpecAndLegacy` and `AssertPinned` merge into it, normalizing the expected value as
`AssertSpecAndLegacy` did). The 42 `AssertSameAsLegacy` forwarders are `AssertMatchesGolden`. A raw NUL byte in
`DifferentialGolden.Hash` became the `\0` escape, the same character, so all 364 golden names are unchanged. CI's nightly
`legacy-oracle` job, its `schedule:` trigger and its `ci-gate` entry are gone. `run_gate_legs.py#binaries` and
`GateLegs.cs#ProductAssemblies` drop the `CobolSharp.` prefix together (none of the three gate assemblies references the
engine any more).

**Cluster A — PB2109 (the guard scripts and CI drive one compiler).** `guard-compiler.sh` selects `cobol` only; its
self-test now proves the PB750 refusal on synthetic `deps.json` manifests (a CLI without the code generator, and a
manifest that only mentions the name), where it used to SKIP when the legacy CLI was not built. The legacy Unit and
Integration legs are gone from `guard.sh`, `guard-fast.sh` and `windows-build-test`; so are `guard_legacy_divergent`,
`LEGACY_DIVERGENT`, `GUARD_DIVERGENT`, the `LEGACY DIVERGENT` verdict word and the audit's compiler argument. Every
`divergent` NIST row is compared with its ISO-conforming golden, and only a `TERMINATES EC-…` row is judged by its
declared exception. `compliance.sh`, `nist-batch.sh` and `run-suite.sh` are deleted. The new drift fact
`CorpusManifestTests.GuardScripts_NameNoLegacyEngine` keeps every `scripts/guard*.sh` from naming the engine or its
exemption again. The docs that became false were fixed in the same change (DESIGN-test-build-ci, COBOLNET_DESIGN,
PIPELINE_DESIGN, plan P15 Cut 1 Steps 1-4). Both notes landed with `closes_rows: []` and a reason (engineering debt, R69;
no inventory row). The engine directories themselves go in PB2110 (group B), which carries the list of remaining
references the implementer handed it.

**The train.** One cluster, applied cleanly on `4b8045980` (the working tree then differed from the implementer's branch
only by main's five later files). Whole-population gate, run `20261007T080144Z-4f45d6`:
`=== BUILD-LOCAL GATE: GREEN — Conformance 10,715/10,715 · Unit 31,724/31,724 · Characterization 35/35 cases ran
(skipped 0) in 1 of 1 leg(s) ===`. The legacy `CobolSharp.Tests.Integration` assembly, `--no-build`: 503 passed, 1
skipped, 0 failed. `=== LINUX GATE: GREEN (legs unit characterization conformance guard) ===`, with the guard leg at
`NIST (cobol): 362 MATCH, 2 TERMINATES (declared), 0 REGRESSION(S)` and the audit at population 376, clean. Semgrep
verify PASS, no count up (`cobolnet-raw-diagnostic-code-literal` reads 263 against a baseline of 273; the baseline is
not re-locked here). The CI audits (code and doc citations, evidence supersession, witness loss, `drift_rules --check`,
`work.py check`) are green. The lander's review of the train's diff found no correctness defect; the two citations the
edited comments keep (§14.9.11.4 GR1, §14.9.1.4 GR6) pass `cite.py --check`. One lead from the report became a note:
PB2121, an orphaned differential golden is undetectable now that the bake re-sweep is gone (silent dead data, no user
program affected). GAP 173 → 173. Nothing dropped.

