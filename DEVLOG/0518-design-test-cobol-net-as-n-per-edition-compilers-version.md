## Entry 518 — 2026-06-09 16:58 PDT — Design: test COBOL.NET as N per-edition compilers (version test matrix)

Owner directive: conceptually we have a different COBOL compiler for each ISO edition (1985/2002/2014/2023) and must
TEST it as such — a (construct × target-edition) matrix where each construct's expected outcome is COMPUTED from its
introduced/removed/changed metadata (now supplied by `VERSION_CHANGE_REFERENCE.md`), with substantial test-infra
rework. Captured the requirement in memory `feedback_version_test_matrix`.

Grounded the design with a 4-agent parallel survey of the current state, which found: (1) the **greenfield** threads
`DialectLevel` only to the front-end grammar (39 `is85/is2002/is2014/is2023` gates) — ZERO binder/emit gating, no
post-parse semantic validation, `CBL3501/3502` defined-but-unused, and `CobolNetCompiler`/`NistDifferentialTests`
**hard-code DialectLevel 85**; (2) the **legacy** already has the proven per-edition model to port — `DialectMode` +
two-axis `DialectConfig` (one cached config queried everywhere) + `DialectStrictnessChecks` (the validator pattern:
parse permissive superset → post-parse accept/warn/reject by mode) + `FlagsFeaturesRemovedAfter85` + `CBL36xx` +
`ControlFlowBinder` gating (ALTER removed-2002 → CBL3601 error/CBL3602 warn) + the negative-test harnesses
(`DiagnosticTestBase.GetDiagnostics/AssertHasDiagnostic`, `FlaggingConformanceTests`, `ConformanceTests` auto-discovery
of `tests/conformance/<ver>/`); (3) ~30–40 of the 130 reference rows are mechanically test-able now (new reserved
words, removed constructs); intrinsics/EC/Unicode need curated snippets; highest-value seed rows identified.

Wrote `docs/VERSION_TEST_MATRIX_DESIGN.md` (added to DOC_INDEX §7): the matrix axes + the computed expected-outcome
function `f(case,V)`; the three invariants as property tests (CONTINUITY — every NIST-85 program compiles at every
later edition unless the ref doc marks the construct removed-by-then, else it's a regression; INTRODUCTION-GATING — a
construct is rejected below its introducing edition; BEHAVIOR-CORRECTNESS); the construct catalogue sourced from the
reference doc with the `gatingImplication → matrix obligation` mapping; the corpora (NIST-85 positive, per-edition
conformance, a NEW negative `_negative/*.err` corpus, behavior-variant); the harness rework (thread DialectLevel into
bind/emit; edition-parameterize `CobolNetCompiler`; port `DialectConfig`; add a greenfield `EditionValidator` +
`ConstructDialectStatus` registry diffed against the reference doc; port the diagnostic-assertion harness); "the matrix
IS the worklist" (each red cell = a ref-doc-driven gating task, TDD); a 4-phase rollout; a reuse/do-not-duplicate map;
and owner-gated open decisions (removed→error vs warn, default --standard, INV-1 strong/weak form). Doc-only.

