## Entry 520 — 2026-06-09 17:45 PDT — Default --standard → COBOL-2023 (owner decision); --nist ⇒ 85

Implemented owner decision #2 (DEVLOG 519): an unflagged compile now targets the LATEST edition.
`CompilerDriver.Options.DialectLevel` default 85 → **2023**; `CliOptions` default `std` 85 → **2023**. Because the
NIST CCVS corpus is COBOL-85, the CLI now sets `std = 85` when `--nist` is given without an explicit `--std` (else a
CCVS program would hit new-2023 reserved words / removed constructs by default). Verified safe: every edition-specific
caller passes `DialectLevel` explicitly — `CobolNetCompiler` and `NistDifferentialTests` (85), `CobolNetTestBase`
(default param 85), `VersionMatrixTests` (per case); only the default-reliant nucleus smoke/unit callers
(`DifferentialHarness`, `DeepNestingTests`) shift to 2023 and still pass (nucleus is edition-invariant). Regression
test `CompilerDriver_DefaultsToLatestEdition` pins the default. Conformance 307; unit 14 → 15; all green.

FINDING (logged, not yet fixed — a future matrix red): the grammar gates standalone `END` (a COBOL-85 no-op
imperative) with `{is85()}?` — i.e. 85-ONLY — but its own comment says it was "removed in COBOL-2023", implying it
should be valid through 2014. The matrix's removed-construct row for standalone END would therefore be RED at
2002/2014 (grammar over-restricts to 85-only). A version-gating correctness item for Phase 2 (the EditionValidator /
grammar-gate audit), captured here so the matrix surfaces it deliberately.

