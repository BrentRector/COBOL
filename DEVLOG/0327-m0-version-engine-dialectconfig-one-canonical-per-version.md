## Entry 327 — M0 version engine: DialectConfig, one canonical per-version dispatch (behavior-preserving)

First code increment of the multi-version roadmap (Milestone 0). The `--standard` CLI option already selected a
`DialectMode` enum (Default < StrictCobol85 < Cobol2002 < Cobol2014 < Cobol2023), but the actual version
decisions were **scattered as raw `Dialect >= X` comparisons across five files** — the exact anti-pattern the
doctrine forbids (no ad-hoc re-derivation; one canonical dispatch).

Introduced `Semantics/DialectConfig.cs`: an immutable, cached-per-mode model resolved via `DialectConfig.For()`,
exposing every version-conditional fact in one place —
- **strictness axis:** `IsStrict` (= Version != Default);
- **version thresholds:** `IsCobol2002OrLater` / `…2014…` / `…2023…`, `ParserLevel`, `DisplayName`;
- **removed-after-'85 policy:** `FlagsFeaturesRemovedAfter85` (what WS-DIALECT will extend per-construct);
- **forward feature flags** (the M2–M4 foundation): `SupportsFreeFormSource`, `SupportsCompilerDirectives`,
  `SupportsUserDefinedFunctions`, `SupportsObjectOrientation`, `SupportsNationalData`,
  `SupportsBitAndBooleanData`, `SupportsPointers`, `SupportsValidate`, `SupportsDynamicTables`,
  `SupportsTypedef`.

Migrated all 12 call sites (`Compilation`, `ControlFlowBinder`, `DialectStrictnessChecks`, `SemanticBuilder`) to
read from `Options.Config`, and **removed** the now-redundant `CompilationOptions.IsCobol2002OrLater` +
`DialectName` so the compiler would flag any missed site (it didn't — clean build). 7 unit tests pin the
matrix. Pure refactor, no behavior change: guard ALL GREEN (1047 unit / 347 integration / 364 NIST). This
config is the spine reused by M1's WS-DIALECT (removed-feature flagging) and every forward milestone.

