## Entry 745 — 2026-07-10 16:59 PDT — PHASE 04 Group C: typed Cst/ façade + migrate the two anchor consumers (commit boundary C3)

Landed PHASE-04 Group C: a typed `Cst/` façade over the generated ANTLR contexts, and migrated the two highest-churn
anchor consumers off raw positional `GetText()`.

**New façade** (`src/Cobol.Net.Frontend/Cst/`, namespace `CobolNet.Frontend.Cst` — placed FRONTEND-side per the phase
doc so it becomes the cross-assembly surface P7 flips the generated contexts to `internal` behind): `SourceSpan`;
`DataReferenceCst` (`Register` / `BaseName` / `HasNoSuffix`); `DataDescriptionCst` + `DataDescriptionClauseCst`
(`Level` / `Name` / `Clauses`; per-clause `PictureText` / `TypeRefName` / `RedefinesTargetName` / `ObjectClassName` /
`OccursMax` / `IndexNames`); `CstExtensions` (`cobolWord.Name()`, `integerLiteral.AsInt()`/`.TryAsInt()`). Thin
`readonly struct`s, 1:1 with the grammar rules, NO semantic/computed state (risk R-C1), with implicit conversions so a
call site passes the raw context unchanged (`DataReferenceCst r = dref;`).

**Migrated:** `ReferenceResolver` (the dataReference→`Place` funnel) — its 3 `GetText()` sites gone (base name ×2 via
`BaseName`, qualifier via `cobolWord().Name()`), the register early-returns via `Register`. `DataBinder.BindEntry`
(the dataDescriptionEntry clause decoder) — 8 raw `GetText()` clusters migrated (level, name, picture, type, redefines,
object-class, occurs-max, indexed). The presence-only clause predicates (BASED/EXTERNAL/TYPEDEF/JUSTIFIED/SYNC/SIGN/
USAGE/OCCURS) stay RAW via `.Context` so the else-if order + edition-gate side effects are byte-identical. The flat
SUBSCRIPT-mode token group stays a raw `SubscriptOrRefModContext` — the seam D10 reshapes; `SplitSubscriptTokens`/
`InterpretSubscripts` are untouched (the P4-vs-P7 boundary).

**Singular-pattern correction (recon over-count fixed at implementation, per `feedback_singular_pattern`):** the recon
flagged `DataBinder.UsageKeyword`/`ExtractValue` as BindEntry-only and folded them into the façade, but a grep at
implementation showed a SECOND caller in `DataBinder.Reports.cs` (P7 scope). Forking a façade copy would be a second
mechanism for one job, so I REUSED the existing shared helpers — BindEntry still calls them (a named helper is not a
raw positional `GetText`) — and dropped the duplicate façade `UsageKeyword`/`FirstValueText` properties.

**Behavior-neutral:** greenfield conformance 3157 · unit 227 · **characterization 32 byte-exact (Gate 3 = emitted C#
byte-identical — the codegen-neutrality proof)** · FULL legacy guard NIST **353 MATCH / ALL GREEN / 0 regressions**
(Group C is compiler-side + inert frontend additions, so the legacy compiler is unaffected — a belt-and-braces
confirm). `BindEntry` `GetText()` 45→37; `ReferenceResolver` 3→0. The remaining ~336 `GetText()` sites + flipping the
generated contexts to `internal` are P7 (FU-2).

**RESUME AT: PHASE-04 Group D reconciliation** (confirm the P3 two-arm `VersionConformancePass` superseded the design's
construct-id annotation side-table; the superset grammar is complete) → D10.0 (delete the 4 dead grammar rules,
byte-neutral) → the D10 design note + the ISO space-separator design decision that gates the SUBSCRIPT-mode removal.

