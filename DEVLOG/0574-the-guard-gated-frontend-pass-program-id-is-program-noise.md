## Entry 574 — 2026-06-11 01:16 PDT — The guard-gated frontend pass: PROGRAM-ID IS…PROGRAM noise words (§11.4.2) — IC401M parses (1008 conformance; FULL legacy guard ALL GREEN)

The one frontend fix Wave 2 queued: `programIdParagraph` now admits the §11.4.2 optional noise words —
`PROGRAM-ID. name [IS] attributes [PROGRAM].` (IC401M writes `IC401M IS INITIAL.`). Pre-authorized
NIST-expansion grammar change; FULL legacy guard re-run: 353 MATCH + 11 LEGACY DIVERGENT, 0 regressions,
1204 unit + 536 integration ALL GREEN; regenerated `Generated/*` committed alongside (the DEVLOG-554 CI
fallback rule). Parsing IC401M exposed one greenfield emission gap, also fixed: declaratives emit the
`__IoCheck`/`__RunGlobalUse` machinery, which reads `CobolFile` even in a FILE-LESS program (IC401M's
mode-scoped USE procedures) — the generated `using CobolNet.Runtime.IO` is now gated on files OR
declaratives. IC401M compiles and runs to its cross-assembly CALL of the no-golden FIC401M companion
(run-only by NIST design — flagging-module programs carry no goldens). New spec test
`ProgramId_IsInitialProgram_NoiseWordsParse`. **1008 conformance + 16 unit green.**

