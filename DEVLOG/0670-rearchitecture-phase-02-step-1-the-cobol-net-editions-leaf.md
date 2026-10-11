## Entry 670 — 2026-07-07 20:52 PDT — Rearchitecture PHASE 02 step 1 — the `Cobol.Net.Editions` leaf assembly

Opened Phase 02 (the edition/diagnostics leaf + first-class diagnostic registry — the root fix for the frontend↔compiler
edition-metadata duplication). This session began with a full recon (workflow `wf_9944fe61-fcc`, 10 parallel readers,
1.23M tok) mapping every subsystem the phase touches and reconciling the doc's file:line claims against the code — the
correction log is preserved in the scratchpad notes (headline drift: the binder gate files are under `Binding/Bound/`
not `Binding/`; ALTER/GO-TO codes are line-swapped; `ConstructRegistry.Entries` = **91** rows not 110;
`ConstructRegistry.Require` does not exist yet; the "290 EditionContext call sites" is really ~475 across 36 files but the
adapter keeps them all untouched; `EditionContext` refactor is 53 `ConstructRegistry.Check` sites + a 10-member surface).
Baseline confirmed green before any edit: conformance 2036 · unit 213 · FULL legacy guard NIST 353 MATCH.

**Step 1 (this commit).** Stood up `src/Cobol.Net.Editions` (`AssemblyName Cobol.Net.Editions`, `RootNamespace
CobolNet.Editions`) — the new LOWEST leaf, dependency-free (Open Q3 recommendation; it must stay below Frontend so it
cannot see the frontend `SourceLocation`/`DiagnosticBag`). Referenced by **both** `Cobol.Net.Frontend` (its FIRST-ever
ProjectReference) and `Cobol.Net.Compiler` (exit criterion 2). Added to `CobolSharp.sln` via `dotnet sln add` — verified it
picked up the full 6-platform (AnyCPU/x64/x86 × Debug/Release) config matrix and the `src` solution-folder nesting, matching
`Cobol.Net.Compiler`. Seeded with `EditionInfo` — the immutable `readonly record struct EditionInfo(int Year, bool
Permissive)` that will single-source `DialectLevel` (`MaxDigits`=18 at 85 / 31 at 2002+; `Has(introducedIn)`; validating
`Of(...)` that rejects a year outside {85,2002,2014,2023} to defend against a defaulted `Year==0`, risk R5). No
`TargetFramework`/`LangVersion` in the csproj — inherited from the repo-root `Directory.Build.props` (net10.0 / C#14 /
`TreatWarningsAsErrors`). Standalone Editions build proves NO reference cycle; full solution builds 0 warnings / 0 errors.
Battery after: conformance 2036 · unit 213 (unchanged — the assembly is referenced but not yet consumed). Sequencing note:
the executing plan reorders the doc's steps 3↔4 (EditionContext adapter FIRST, then move the registry) because once
`ConstructRegistry.Check` moves into Editions it can no longer take an `EditionContext`, so the adapter must exist before
the call sites change (the doc itself notes "update call sites … once the adapter exists").

