# WiseOwl COBOL — Project Organization, Rename & Code-Structure (deep-dive design)

> **Status: LIVE / authoritative.** The rename to Cobol.NET / cobol.exe, target solution/folder/namespace
> layout, front-end extraction, no-god-class discipline, and C# 14 usage. Condensed view: `COBOLNET_DESIGN.md` §17.

> **Execution state.** The §1.1 project set, the front-end extraction (§1.4), and the split + rename of the compiler/CLI/runtime (§1.5 steps 1–5) are complete — `src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}` and `tests/Cobol.Net.Tests.{Unit,Conformance}` exist and are green. The no-god-class decomposition of §2 is complete: both the emitter and the binder god classes are fully dissolved (§1.2 / §2 / §2.2 describe the resulting structure). Pending: the G8 cut-over — legacy-engine deletion (§1.5 step 6).

---

# Project Organization & Code-Structure (deep-dive; condensed copy lives in `COBOLNET_DESIGN.md` §17)

> Scope of this section: the target solution/project layout, the front-end extraction, the rename, the no-god-class structural rules, and the C# 14/.NET 10 usage guidelines. It expands `COBOLNET_ARCHITECTURE.md` §5 (which currently sketches the layout in three bullets) into the decision-complete plan. Implementation: **G0 is DONE** (the layout below reflects the current tree, and the emitter and binder god classes are fully decomposed); the namespace-rename + legacy-deletion half lands at **G8** cut-over.

## A. Findings that drive every decision below

These were verified against the live tree (2026-06-08), not assumed:

1. **The front-end is assembly-cleanly separable.** `CobolNet` consumes exactly four legacy namespaces — `CobolSharp.Compiler.Diagnostics`, `.Generated`, `.Parsing`, `.Preprocessor` (plus `.Common` transitively). The dirs `Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/` have **zero** `using` references to the legacy `Semantics`/`IR`/`CodeGen`/`FlowAnalysis` layers, and the front-end does not reference `CobolSharp.Runtime`. So the front-end can be lifted into its own assembly with no code edits to the moved files. *(Current state: the front-end references `Cobol.Net.Editions` and, since kb/Work PB1592, the typed-native `Cobol.Net.Runtime` — see §1.4. It still never references the legacy `CobolSharp.Runtime`.)*
2. **The dependency the task wants killed is an *assembly* dependency, not a namespace dependency.** Moving `*.cs` files into a new `.csproj` ends the new compiler's reference to `CobolSharp.Compiler.dll` **without renaming a single namespace** — namespaces are independent of the project that compiles them. This lets us split "physical move" (G0) from "cosmetic namespace rename" (G8) and stay green throughout.
3. **The legacy byte engine must keep parsing.** `COBOLNET_ARCHITECTURE.md` keeps `CobolSharp.Compiler` alive as a differential oracle until G8. After extraction, the *legacy* engine, the legacy CLI, and **both test projects** (all four currently reference `CobolSharp.Compiler.csproj`) must repoint to the new Frontend assembly for parsing/diagnostics. The front-end namespaces are consumed compiler-wide by the legacy `Semantics`/`IR`/`CodeGen` (every parse-tree `CobolParserCore.*` context; `DiagnosticBag` everywhere) — which is *why* a namespace rename has a wide blast radius and is deferred.
4. **The front-end is more than `.cs`.** It includes the grammar-generation machinery: `Grammar/` + `Grammar/Core/*.g4`, `GenerateIfNewer.ps1`, `Invoke-Antlr4CSharp.ps1`, the `ANTLR4/antlr-4.13.2-complete.jar`, the `EnsureGeneratedFiles`/`CleanGenerated` MSBuild targets, and the `Generated/` output. These move as one unit; the generated namespace is set by the generation script and is held constant through the move.

---

## 1. Solution / project reorganization + rename

### 1.1 Target project set (exact names)

| # | Project (assembly) | Kind | `RootNamespace` | `AssemblyName` | Purpose |
|---|---|---|---|---|---|
| P1 | **`Cobol.Net.Frontend`** | library | `CobolNet.Frontend` | `Cobol.Net.Frontend` | Preprocessor + ANTLR lexer/parser + parse-tree + diagnostics. Extracted from `CobolSharp.Compiler`. The single front-end for both the new compiler and (until G8) the legacy oracle. |
| P2 | **`Cobol.Net.Compiler`** | library | `CobolNet` | `Cobol.Net.Compiler` | Bind → backend-neutral bound tree → `ICodeGenBackend` (`--backend roslyn\|cil`: RoslynBackend = primary/v1 C# source; CilBackend = future-additive, with its OWN private structure→branch lowering — NO shared lowered IR). The compiler proper, minus the CLI shell. |
| P3 | **`Cobol.Net.Cli`** | exe | `CobolNet.Cli` | **`cobol`** | Thin command-line driver (`Main`, arg parsing, file orchestration). Produces **`cobol.exe`**. |
| P4 | **`Cobol.Net.Runtime`** | library | `CobolNet.Runtime` | `Cobol.Net.Runtime` | The typed-native runtime the *generated* programs call (`CobolNum`, `CobolString`, `NumProfile`, `ManagedPointer`, file/format helpers). |
| T1 | **`Cobol.Net.Tests.Unit`** | xUnit | `CobolNet.Tests.Unit` | — | Unit tests for the new compiler + runtime. |
| T2 | **`Cobol.Net.Tests.Conformance`** | xUnit | `CobolNet.Tests.Conformance` | — | NIST + post-85 conformance corpus + the differential harness + the VERSION TEST MATRIX (`VersionMatrixTests` — the compiler exercised as four per-edition compilers per `docs/VERSION_TEST_MATRIX_DESIGN.md`), run against the new compiler. |

**Decision — name form.** Assembly/package/folder names use the dotted product brand **`Cobol.Net.*`** (reads as the product "WiseOwl COBOL"); **root namespaces stay the single token `CobolNet`** (e.g. `CobolNet.Frontend`, `CobolNet.CodeGen`). Rationale: dotted `Cobol.Net.*` is the marketing/NuGet identity; `CobolNet` as the namespace root avoids a clash with the `.Net`/`System.Net` reading and keeps `using CobolNet.CodeGen;` clean. One rule, applied consistently. (Owner may prefer `Cobol.Net` namespaces too — trivially flippable since it is just the `<RootNamespace>` value; not load-bearing.)

**Decision — CLI split (P2/P3).** Today `Program.cs` lives *inside* the exe project, so tests cannot reference the compiler without referencing an exe. Split it: `Cobol.Net.Compiler` (library, everything except the CLI shell) + `Cobol.Net.Cli` (exe, `<AssemblyName>cobol</AssemblyName>`, ~120-line driver). This mirrors the proven legacy `CobolSharp.Compiler`/`CobolSharp.CLI` split and lets the test projects reference a library.

**Decision — Diagnostics/Common placement.** Fold `Diagnostics/` and `Common/` into `Cobol.Net.Frontend` (a `Diagnostics/` folder + a `Common/` folder) for v1. They are small (4 + 3 files), have no independent consumer, and a separate `Cobol.Net.Diagnostics` would be premature. Revisit only if a non-frontend consumer of diagnostics appears.

### 1.2 Folder layout per project (folder = subsystem)

```
src/Cobol.Net.Frontend/
  Cobol.Net.Frontend.csproj          (carries the ANTLR codegen targets)
  Grammar/         CobolParserCore.g4, CobolDialect/OO/JsonXml/Generics.g4, CobolPreprocessor.g4
    Core/          CobolLexer.g4, CobolData/ControlFlow/Expressions/IO/OO/ReportWriter/Screen/SpecialNames.g4
  Generated/       CobolLexer.cs, CobolParserCore.cs, *Visitor.cs  (build output; git-ignored or tracked per current policy)
  Parsing/         CobolParserCoreBase.cs, CobolErrorListener.cs, CobolErrorStrategy.cs, ZeroTokenRewriter.cs
  Preprocessor/    ReferenceFormatProcessor.cs, ConditionalCompilationProcessor.cs, CopyProcessor.cs, NistPreprocessor.cs
  Diagnostics/     Diagnostic.cs, DiagnosticBag.cs, DiagnosticDescriptors.cs, DiagnosticSeverity.cs
  Common/          SourceText.cs, SourceLocation.cs, TextSpan.cs
  ANTLR4/          antlr-4.13.2-complete.jar
  GenerateIfNewer.ps1, Invoke-Antlr4CSharp.ps1
  Pipeline/        Frontend.cs        (the orchestrator — MOVED from src/CobolNet/Frontend/, the one client of all of the above)

src/Cobol.Net.Compiler/
  Cobol.Net.Compiler.csproj
  Binding/         DataBinder.cs (+ .Linkage/.Odo/.Oo/.Ptr/.Reports/.Switches partials), ReferenceResolver.cs,
                   PictureAnalyzer.cs, IntrinsicCatalog.cs, OptionsBinder.cs, OptionsModel.cs, RoundingModes.cs,
                   CollatingModel.cs, EditionContext.cs, OoClassTable.cs, IOoBindHost.cs, TurnState.cs, BinderDriver.cs
    Model/         the bound data model: DataItem.cs, PicInfo.cs, Place.cs, Condition88.cs, FileModel.cs,
                   RedefinesModel.cs, RecordLayout.cs, StorageForm.cs, StrongTypeModel.cs, OdoModel.cs, SymbolTable.cs,
                   BoundCompilation.cs, BoundUnit.cs, OoClassUnit.cs
    Passes/        BindPipeline.cs, IBindPass.cs, GroupBindPass.cs, StorageFormPass.cs, UsageCollectionPass.cs
    Bound/         BoundTree.cs + the Bound*.cs node records, MoveClassifier.cs, and StatementBinder.cs (dispatch +
                   mark/drain wrap + composition root) — the backend-neutral bound tree. ALL semantics bind here; there
                   is NO shared lowered IR (COBOLNET_DESIGN §1.1) — any structure→branch lowering is PRIVATE to a backend.
    Procedure/     the decomposed statement binder: BinderContext.cs (the spine), ExpressionBinder.cs,
                   ProcedureTableBuilder.cs, PhraseBlocks.cs, MnemonicRegistry.cs, EcBindState.cs, OoMethodScope.cs,
                   SectionInfo.cs, and Verbs/*Binder.cs (one binder per statement family)
    Validation/    StatementValidation.cs (the pure statement-shape + relational checks)
  CodeGen/Emit/    EmitCore.cs (the `EmitContext`), NumericRenderer.cs (the NumX machinery), ConditionRenderer.cs,
                   BooleanRenderer.cs, IntrinsicRenderer.cs, OperandText.cs — the shared expression/condition renderers
  CodeGen/DataDivision/  DataEmitter.cs, RecordStructEmitter.cs, GroupImageCodec.cs, GroupValueSlicer.cs,
                   PhysicalModel.cs, ValueInitializer.cs — DATA DIVISION → C# fields / record structs
  CodeGen/Verbs/   one *Emitter.cs per statement family (MoveEmitter, ArithmeticEmitter, EvaluateEmitter, CallEmitter, …)
  CodeGen/         The `ICodeGenBackend` seam: RoslynBackend.cs (primary — C# source), AssemblyPackager.cs,
                   CodeWriter.cs. The emitter god class is dissolved — ProgramEmitter.cs (run-unit orchestration) →
                   UnitEmitters.cs (per-unit composition root) → DispatchEmitter.cs/StatementEmitter.cs + the
                   Verbs/*Emitter.cs, with EcEmitter.cs, EmitterState.cs, NameAllocator.cs and the Roslyn/ helpers
                   (RuntimeApi.cs, ReceiverContext.cs, FigurativeConstants.cs); CSharpEmitter.cs (+ .Oo.cs) is now only
                   the bind-host facade (Bind/EmitBound + IOoBindHost) until P9 relocates the OO bind bodies. The
                   future-additive CilBackend (Mono.Cecil, its OWN private structure→branch lowering) slots
                   in beside Roslyn here, behind the same interface (COBOLNET_DESIGN §1.1/§18.23).
  CompilerDriver.cs                  (the library entry: source path → result; what Program.Main calls)

src/Cobol.Net.Cli/
  Cobol.Net.Cli.csproj               (<OutputType>Exe</OutputType>, <AssemblyName>cobol</AssemblyName>)
  Program.cs                         (Main + arg orchestration)
  CliOptions.cs                      (the parsed-options record — extracted from Program.cs)

src/Cobol.Net.Runtime/
  Cobol.Net.Runtime.csproj
  Numeric/   CobolNum.cs, NumProfile.cs, CobolRounding.cs, (later CobolDecimal.cs)
  Text/      CobolString.cs
  Control/   StopRun.cs
  Pointers/  ManagedPointer.cs       (ported clean at G7)
  Files/     (G6: typed-record ↔ byte serialization at the medium boundary)
```

> The runtime subsystem folders (`Numeric/`, `Text/`, …) mirror `COBOLNET_ARCHITECTURE.md` §3's data-model rows, so a reader maps "COBOL national string" → `Text/` and "USAGE POINTER" → `Pointers/` directly.

### 1.3 Complete item-by-item mapping

| Current item | Verb | Destination |
|---|---|---|
| `src/CobolNet/Program.cs` | **split + move** | `Cobol.Net.Cli/Program.cs` (Main + Run); the `CliOptions` record → `Cobol.Net.Cli/CliOptions.cs`; the compile orchestration body → `Cobol.Net.Compiler/CompilerDriver.cs` |
| `src/CobolNet/Frontend/Frontend.cs` | **move** | `Cobol.Net.Frontend/Pipeline/Frontend.cs` (it *is* the front-end orchestrator; belongs with what it drives) |
| `src/CobolNet/Binding/*.cs` | move | `Cobol.Net.Compiler/Binding/` |
| `src/CobolNet/CodeGen/CSharpEmitter.cs` | **decompose + move** | `Cobol.Net.Compiler/Emit/**` (see §2 for the split) |
| `src/CobolNet/CodeGen/CodeWriter.cs`, `RoslynBackend.cs` | move | `Cobol.Net.Compiler/CodeGen/` (and factor `ReferenceAssemblies`/`RuntimeConfigWriter` out of `RoslynBackend`) |
| `src/CobolNet.Runtime/**` | move (rename project) | `src/Cobol.Net.Runtime/**`, re-foldered (`CobolString.cs`→`Text/`, `StopRun.cs`→`Control/`) |
| `src/CobolSharp.Compiler/Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/`, `Grammar/`, `ANTLR4/`, `GenerateIfNewer.ps1`, `Invoke-Antlr4CSharp.ps1`, the ANTLR MSBuild targets | **extract** | `src/Cobol.Net.Frontend/` (same subfolder names) |
| `src/CobolSharp.Compiler/` remainder — `Semantics/`, `IR/`, `CodeGen/` (the 11 `Cil*`/`*Lowerer`), `FlowAnalysis/`, `Compilation.cs`, `CompilationResult.cs` | **retire at G8** | deleted at cut-over; until then stays as `CobolSharp.Compiler` (the differential oracle), now referencing `Cobol.Net.Frontend` |
| `src/CobolSharp.Runtime/**` | **retire at G8** | the byte engine's runtime; deleted at cut-over (its clean substrates already ported into `Cobol.Net.Runtime`) |
| `src/CobolSharp.CLI/**` | **retire at G8** | replaced by `Cobol.Net.Cli`; until then repointed to `Cobol.Net.Frontend` |
| `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/` | keep (legacy), **add new** | stay until G8 (test the oracle). New `tests/Cobol.Net.Tests.Unit/` + `tests/Cobol.Net.Tests.Conformance/` added in G0; legacy test projects deleted at G8 |
| `tests/nist/`, `tests/conformance/` | **keep in place** | the corpus is compiler-agnostic; the new conformance test project points at it |
| `docs/COBOLNET_ARCHITECTURE.md`, `COBOLNET_DESIGN.md` (this doc), `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` | keep | update §5 of ARCHITECTURE to point here; add a `DOC_INDEX.md` row for `COBOLNET_DESIGN.md` |

### 1.4 Front-end extraction — how, precisely

The new `Cobol.Net.Frontend.csproj`:
- Inherits TFM/lang from `Directory.Build.props` (net10.0 / C# 14) — **do not** re-declare.
- `<PackageReference Include="Antlr4.Runtime.Standard" />` — **no `Version`** (central package management via `Directory.Packages.props`). It needs `Antlr4.Runtime`; it does **not** need `Mono.Cecil` (that was the byte emitter's IL writer).
- `ProjectReference`s: `Cobol.Net.Editions` (edition metadata) and `Cobol.Net.Runtime` (since kb/Work PB1592). It never references the legacy `CobolSharp.Runtime`. **Why the Runtime edge:** the compile-time expression evaluator (`>>DEFINE`/`>>IF`/`>>EVALUATE` and the CONSTANT binder) carries its values in the runtime's ONE SDIDI decimal engine (`CobolDec`) and selects its arithmetic mode through the ONE per-edition behaviour register (`DialectBehaviors`); ISO Annex E.2 6) and 21) prescribe standard arithmetic for compile-time arithmetic at 2002/2014. The alternative was a second copy of that engine in the front-end, which CLAUDE.md rule 5 rejects (one arithmetic engine). **Layering: `Cobol.Net.Editions` and `Cobol.Net.Runtime` are leaves (they reference nothing in the solution) → Frontend → Compiler → Cli. `Cobol.Net.Runtime` must NEVER reference `Cobol.Net.Frontend` (or the Compiler)**: the runtime is the assembly every generated program loads, and an edge back up would pull the parser into each compiled program's closure and close a cycle.
- Carries the ANTLR generation: copy `EnsureGeneratedFiles` + `CleanGenerated` targets and the `<None Include="Grammar\…">`/jar items verbatim from the legacy csproj; the `Inputs`/`Outputs` paths stay relative so they work post-move.
- `<InternalsVisibleTo Include="Cobol.Net.Tests.Unit" />` (replaces the legacy one) if any internals need testing.

**Namespaces stay `CobolSharp.Compiler.*` through G0–G7.** The moved files are not edited. Consumers reference the new *assembly*; the `using CobolSharp.Compiler.Parsing;` lines in `Frontend.cs` still resolve. The cosmetic rename `CobolSharp.Compiler.* → CobolNet.Frontend.*` is a single mechanical big-bang at **G8**, when the legacy engine is being deleted anyway — so only the new compiler's `using`s need updating, the smallest possible diff. (Doing it at G0 would force-touch all of legacy `Semantics`/`IR`/`CodeGen`, which we are about to delete — wasted churn.)

### 1.5 Ordered git-mv sequence (build + guard green per step)

Each step is a self-contained commit; `dotnet build CobolSharp.sln` (and the guard, once scripts are repointed) is green at the end of each. `git mv` preserves history.

**Step 1 — Extract the front-end (kills the legacy-assembly dependency).**
1. `git mv` `Parsing/ Preprocessor/ Diagnostics/ Common/ Generated/ Grammar/ ANTLR4/ GenerateIfNewer.ps1 Invoke-Antlr4CSharp.ps1` from `src/CobolSharp.Compiler/` → `src/Cobol.Net.Frontend/`.
2. Create `Cobol.Net.Frontend.csproj` (with the ANTLR targets); add it to `CobolSharp.sln`.
3. Repoint the **four** consumers' `ProjectReference` from `CobolSharp.Compiler.csproj` → `Cobol.Net.Frontend.csproj` **and add** a `CobolSharp.Compiler → Cobol.Net.Frontend` reference (the byte engine now consumes the extracted front-end). Consumers: `src/CobolNet`, `src/CobolSharp.CLI`, both `tests/CobolSharp.Tests.*`. (`CobolSharp.Compiler` keeps its `Cobol.Net.Frontend` ref + its own `Mono.Cecil`/`CobolSharp.Runtime` refs for the byte engine.)
4. Build. The new compiler no longer references `CobolSharp.Compiler.dll`. ✅ *Goal "stop depending on the legacy assembly" met here, with zero namespace churn.*

**Step 2 — Rename the runtime.** `git mv src/CobolNet.Runtime src/Cobol.Net.Runtime`; rename the `.csproj`; set `<AssemblyName>Cobol.Net.Runtime</AssemblyName>`/`<RootNamespace>CobolNet.Runtime</RootNamespace>`; re-fold (`Text/`, `Numeric/`, `Control/`); update the `RoslynBackend` runtime-DLL path constant (`CobolNet.Runtime.dll` → `Cobol.Net.Runtime.dll`); update `.sln`. Build + run a HELLO compile. ✅

**Step 3 — Split + rename the compiler & CLI.** `git mv src/CobolNet src/Cobol.Net.Compiler`; create `Cobol.Net.Cli` and move `Program.cs`/`CliOptions` into it; extract `CompilerDriver` into the library; set assembly/root-namespace/`OutputType` per §1.1 (Cli `<AssemblyName>cobol</AssemblyName>`); update `.sln`. Build; `cobol hello.cob --run`. ✅

**Step 4 — Add the new test projects.** Create `tests/Cobol.Net.Tests.Unit` + `tests/Cobol.Net.Tests.Conformance`, referencing the new compiler library + the in-place `tests/nist`/`tests/conformance` corpus; add `InternalsVisibleTo`. Legacy test projects untouched (still guarding the oracle). ✅

**Step 5 — Solution / scripts / CI / props (one commit).**
- `CobolSharp.sln`: as executed the filename was KEPT (entries updated in place); the `Cobol.Net.sln` brand rename remains an owner-taste option, best bundled with the G8 big-bang.
- Update the **five** scripts that hardcode paths: `scripts/guard.sh`, `guard-fast.sh`, `guard-run-group.sh`, `nist-batch.sh`, `run-suite.sh` (the `.sln` name and any `src/CobolNet*`/`CobolSharp.*` project paths and the runtime-DLL copy target).
- Update CI `.github/workflows/build-and-test.yml` (it hardcodes `CobolSharp.sln` and the two `tests/CobolSharp.Tests.*` paths) in this same commit so CI tracks the rename.
- `Directory.Build.props` / `Directory.Packages.props` need **no functional change** (TFM/lang/central-versions are name-agnostic); confirm the package-id list still covers `Antlr4.Runtime.Standard`, `Microsoft.CodeAnalysis.CSharp`, the test packages. (`Mono.Cecil` becomes legacy-only; keep its `PackageVersion` until G8.)
- Full guard green. ✅

**Step 6 — (at G8, not G0) Namespace big-bang + legacy deletion.** Delete `CobolSharp.Compiler` (`Semantics`/`IR`/`CodeGen`/`FlowAnalysis`/`Compilation*`), `CobolSharp.Runtime`, `CobolSharp.CLI`, and the legacy test projects. Rename `CobolSharp.Compiler.* → CobolNet.Frontend.*` across the surviving Frontend files + the new compiler's `using`s (one search-replace, now small because only the new compiler consumes it). Drop the `Mono.Cecil` `PackageVersion` (it returns only as the future-additive `CilBackend`'s IL writer, if/when that backend lands — COBOLNET_DESIGN §18.23). Update the generation script's emitted namespace. Final guard green. ✅

> **`.sln` / config implications, summarized:** central package management means every new `.csproj` `PackageReference` is **version-less**; `Directory.Build.props` is the single TFM/lang/nullable/warnings-as-errors source — projects never re-declare those; the `.sln`, the 5 scripts, and the CI YAML are the only places project/solution *paths* are hardcoded and must move in lockstep (Step 5).

---

## 2. No god classes — structural discipline

The legacy `CilEmitter` reached 2600 lines before being split into 11 `Cil*Emitter`s sharing an `EmissionContext`. The WiseOwl COBOL backend follows the same discipline — emission is split one-file-per-statement-family rather than accreting into a single class, with the proven legacy shape as the template. `CodeGen/CSharpEmitter.cs` (+ `.Oo.cs`) is now only the bind-host facade (`Bind`/`EmitBound` + `IOoBindHost`); emission proper lives on `CodeGen/ProgramEmitter.cs` (run-unit orchestration) → `CodeGen/UnitEmitters.cs` (per-unit composition root) → `CodeGen/DispatchEmitter.cs`/`CodeGen/StatementEmitter.cs` + the `CodeGen/Verbs/*Emitter.cs` family emitters, over the shared `CodeGen/Emit/{EmitCore (the EmitContext), NumericRenderer, ConditionRenderer, BooleanRenderer, IntrinsicRenderer, OperandText}` renderers and `CodeGen/DataDivision/*` for the DATA DIVISION. The binder is decomposed the same way: `Binding/Bound/StatementBinder.cs` is dispatch + composition-root over `Binding/Procedure/*`, `Binding/Procedure/Verbs/*Binder.cs`, and `Binding/Validation/StatementValidation.cs`. Continue the one-file-per-statement-family split as verb families are added.

### 2.1 Rules (non-negotiable, in PROMPT.md spirit)

1. **Shared state lives in a context object, never a mega-class.** An immutable `EmitContext` (the `CodeWriter`, the `DataBinder`, the `NameAllocator`, the dialect config) is passed to every emitter; the per-render receiver travels by a `ReceiverContext` parameter, never as mutable context state. Emitters are stateless-but-for-the-context cooperating units — the same context-object pattern the legacy `EmissionContext`/`LoweringContext` used.
2. **One file per statement-family emitter.** A verb family = a file. Adding a verb = a method in its family's emitter (or a new file for a new family), never a new branch threaded into a shared switch in a 2000-line file.
3. **Respect the bind → emit boundary** (and `project_dual_backend_goal`): the **binder** produces the backend-neutral bound tree (`BoundProgram`, `DataItem`/`PicInfo`, `Place`) and resolves ALL references and semantics; **emit** is a backend behind `ICodeGenBackend` that only RENDERS the bound tree — there is NO shared lowering phase; any structure→branch lowering (e.g. for the future CIL backend) is PRIVATE to that backend (COBOLNET_DESIGN §1.1). An emitter must not re-discover semantics the binder owns (e.g. category compatibility), bound nodes must not carry pre-rendered C#-specific fragments where a structured form is feasible, and the binder must not emit text.
4. **Dispatch generically, refactor-first** (`feedback_change_the_dispatch_not_the_callers`): the statement dispatcher routes by node type to the owning emitter; you never add per-caller if-else chains. New variant ⇒ extend the dispatch table, not each call site.
5. **Size is a *smell*, not the law — SRP is the law.** Heuristic thresholds: a class > ~400 lines or a method > ~60 lines triggers a "does this have one responsibility?" review. *But note `CilDataEmitter.cs` is 44 KB even after the split* — data is intrinsically broad; the test is cohesion, not line count. A 500-line class with one job is fine; a 200-line class doing two jobs is not.
6. **Runtime split by concern** (already followed): `Numeric/`, `Text/`, `Control/`, `Pointers/`, `Files/` — never a `CobolRuntime` god class.
7. **Edition gating is structural (G1 — four compilers in one executable).** `--std 85|2002|2014|2023` (default 2023) is parsed in `Cobol.Net.Cli`, flows as the dialect level through the binder and `EmitContext`, and every edition-varying construct carries BOTH obligations in code: the per-edition spec behavior AND the correct diagnostic in every edition that lacks the construct (not-yet-introduced or removed — `docs/VERSION_CHANGE_REFERENCE.md` is the checklist). No binder/emitter hard-codes a single edition's semantics, and no edition check is an ad-hoc comparison scattered per call site — gate through the one canonical dialect-level carrier.

### 2.2 Concrete decomposition of `CSharpEmitter` (the class list)

> **Note:** read the table below for the target file granularity, not for method signatures. Dispatch is over **bound
> nodes** (`BoundDisplay`, `BoundMove`, … from `Binding/Bound/BoundTree.cs`), not the parse-tree `*Context` types shown
> in the "Lifted from" column — emitters only render the bound tree, so that column names each responsibility's original
> home, not the emitter's input. The realized family emitters live under `CodeGen/Verbs/*Emitter.cs` (see §1.2).

| New class (file) | Responsibility | Lifted from current `CSharpEmitter` |
|---|---|---|
| `EmitContext` (in `Emit/EmitCore.cs`) | Holds `CodeWriter`, `DataBinder`, `NameAllocator`, dialect config. Immutable — the per-render receiver travels by a `ReceiverContext` parameter. The shared spine. | the private fields scattered today |
| `CSharpProgramEmitter.cs` | Top-level orchestration: class shell, `Main`, the paragraph→method loop. ~80 lines. | `Emit`, the Main/paragraph loop |
| `Emit/Data/FieldEmitter.cs` | DATA DIVISION → C# fields/profiles; VALUE initializers; (G2) group→`record struct`, OCCURS→`T[]`. | `EmitWorkingStorage`, `EmitFieldRecursive`, `InitializerFor`, `UnscaledAtScale`, `ProfileName` |
| `Emit/Statements/DisplayEmitter.cs` | `DISPLAY` (and later `ACCEPT`). | `EmitDisplay` |
| `Emit/Statements/MoveEmitter.cs` | `MOVE` (+ later CORR). | `EmitMove`, `ConvertToTarget`, `SendAsString`, `SendAsNumber` |
| `Emit/Statements/ArithmeticEmitter.cs` | `ADD`/`SUBTRACT`/`MULTIPLY`/`DIVIDE`/`COMPUTE`; GIVING/ROUNDED/SIZE ERROR. | `EmitAdd`…`EmitCompute`, `AssignScaled`, `AssignDivide`, `EmitArithAssign` |
| `Emit/Statements/ConditionalEmitter.cs` | `IF`/`EVALUATE` block splitting + branch emit. | `EmitIf`, `EmitBlocks` |
| `Emit/Statements/PerformEmitter.cs` | `PERFORM` (inline/out-of-line/THRU/TIMES/UNTIL; G4 VARYING/GO TO). | `EmitPerform`, `EmitInlinePerform`, `EmitUntil`, `EmitLoop`, `TimesCount` |
| `Emit/StatementDispatcher.cs` | Routes a `StatementContext` to its owning emitter (the generic switch). | `EmitStatement` |
| `Emit/Numerics/NumericExprRenderer.cs` | The whole `NumX` scale-tracked renderer (`Num`, `NumChain`, `Combine`, `Align`, `UnscaledLit`, `FieldNum`, …). One cohesive unit. | the `NumX` region |
| `Emit/Conditions/ConditionRenderer.cs` | COBOL condition → C# boolean (`RenderCondition`, logical chains). | `RenderCondition` |
| `Emit/Conditions/ComparisonRenderer.cs` | Relational comparison + `MapOperator` + operand string/number rendering. | `RenderComparison`, `MapOperator`, `IsStringOperand`, `OperandAsString`, `OperandNum` |
| `Binding/ReferenceResolver.cs` | `DataReferenceContext` → `DataItem` (name/qualified/subscript). | `Resolve`, `ReadAsString` |
| `CodeGen/CodeWriter.cs` | unchanged (already single-purpose). | — |
| `CodeGen/RoslynBackend.cs` | split: keep `Compile`; extract `ReferenceAssemblies.cs` + `RuntimeConfigWriter.cs` (each already a distinct concern in the file). | — |

The free helpers (`DecodeCobolString`, `CsStringLiteral`, `Children`, `DataRefs`, `FirstToken`) become a small internal `EmitHelpers` static class or move next to their primary user. `RenderLiteralAsString` lives with whoever owns literals (the renderers).

---

## 3. C# 14+ / .NET 10+ feature usage guideline

> Toolchain floor: .NET 10 / C# 14 — **or later**. Upgrading (e.g. .NET 11, currently in preview) is pre-authorized
> whenever newer features make the compiler clearer, safer, or more productive; the TFM/lang live in exactly one place
> (`Directory.Build.props`), so the flip is a one-line change.

**Principle: readability and correctness first, not feature golf.** A modern feature earns its place only when it makes the *emitter* code clearer or safer. Examples below are drawn from constructs already in this codebase.

| Feature | Use it when | Avoid when | In-repo example |
|---|---|---|---|
| `record` / **`record struct`** | immutable value bundles with value equality — bound model, small renderer results | a type with identity/mutable lifecycle | `readonly record struct NumX(string Expr, int Scale)`; `record struct Result(bool, IReadOnlyList<Diagnostic>)` |
| **primary constructors** | a class/struct whose ctor just captures collaborators into fields | when the param needs validation/transformation before storing | `readonly struct BlockScope(CodeWriter writer)`; apply to the new emitters: `sealed class MoveEmitter(EmitContext ctx)` |
| **collection expressions `[]`** | initializing lists/arrays | — (always clearer than `new List<T>()`) | `List<Para> _paras = [];`, `_copySearchPaths = []` |
| **property / list patterns** | inspecting the bound model without temp vars | deeply nested patterns that out-clever the reader | `is { Pic: { Category: PicCategory.Numeric, IsFloat: false } } t` |
| **switch expressions** | total mapping from one closed set to another | side-effecting branches (use a `switch` statement) | `MapOperator`, `Combine`, `PicInfo.ClrType` |
| **file-scoped namespaces** | every file (one ns per file here) | — | every `.cs` in the project |
| **`using` aliases** | taming a long generated type name | aliasing for brevity's sake | `using Core = CobolParserCore;` |
| **`required` members** | a non-nullable field with no sensible default | optional/defaulted state | `DataItem.Level`/`.CsName` are `required` |
| **raw / interpolated string literals** | emitting multi-line C#/JSON templates | single tokens | the `$$"""…"""` `runtimeconfig.json` in `RoslynBackend` |
| **`static` lambdas** | a closure that captures nothing (signals + prevents capture) | when capture is intended | `.Where(static p => p.EndsWith(".dll", …))` |
| **`params` collections (C# 13+)** | variadic helpers taking spans/lists | — | candidate for `CodeWriter` multi-line helpers |
| **`field` keyword (C# 14)** | a property needing light backing-field logic without declaring the field | trivial auto-props (no gain) | future: a lazily-built profile cache on `PicInfo` |

**Before / after (compiler-relevant):**

*Statement dispatch — switch statement with cohesive arms (keep) vs. an if-else ladder (avoid):*
```csharp
// GOOD — the existing pattern-switch dispatch, one arm per family, easy to extend:
case var _ when s.moveStatement()    is { } m: _move.Emit(m);   break;
case var _ when s.addStatement()     is { } a: _arith.EmitAdd(a); break;
// BAD — a growing if/else ladder in every caller (forbidden by feedback_change_the_dispatch_not_the_callers)
```

*Result bundle — `record struct` vs. out-params:*
```csharp
public readonly record struct Result(bool Success, IReadOnlyList<Diagnostic> Diagnostics); // GOOD: named, immutable, value-equal
// BAD: bool Compile(..., out IReadOnlyList<Diagnostic> diags)  — positional, easy to misuse
```

*Emitter construction — primary ctor + context (the decomposition target):*
```csharp
internal sealed class ArithmeticEmitter(EmitContext ctx)   // GOOD: collaborators captured once
{
    public void EmitAdd(Core.AddStatementContext add) { ... ctx.Writer.Line(...); }
}
// BAD: a 790-line CSharpEmitter holding _data, _paras, _targetScale, and every Emit* method.
```

**Standing conventions** (already in force, restated): full XML doc comments on public surface + inline rationale on non-obvious COBOL semantics (with ISO §citations — `feedback_spec_is_the_oracle`); generated C# written to `<name>.g.cs`, always inspectable; `SymbolDisplay.FormatLiteral` for every emitted string literal (never hand-rolled escaping).

---

## Historical assembly notes (reconciled 2026-06-10 — kept for provenance)

- This section is the **Project Organization & Code-Structure** chapter; it expands `COBOLNET_ARCHITECTURE.md` **§5** (currently 3 bullets) — add a cross-link there pointing to this doc, and keep ARCHITECTURE's roadmap (G1–G8) authoritative for *feature* sequencing while this doc owns the *structure* sequencing (the new G0).
- ~~`docs/DOC_INDEX.md` needs a new row for `docs/COBOLNET_DESIGN.md`~~ DONE — the index carries rows for the SSOT and for this doc.
- Two owner-level decisions are taste calls flagged inline, defaulted but trivially flippable: (a) root namespace `CobolNet` vs `Cobol.Net` (I default to single-token `CobolNet`); (b) rename `.sln` file vs keep filename (I default to rename for brand consistency).
- Relevant absolute paths: compiler `E:\COBOL\src\CobolNet\CodeGen\CSharpEmitter.cs` (the decomposition target), `E:\COBOL\src\CobolNet\Program.cs` (the P2/P3 split source); front-end extraction set under `E:\COBOL\src\CobolSharp.Compiler\{Parsing,Preprocessor,Diagnostics,Common,Generated,Grammar,ANTLR4}\`; the architecture doc `E:\COBOL\docs\COBOLNET_ARCHITECTURE.md`; consumers to repoint = the 4 `.csproj` referencing `CobolSharp.Compiler.csproj` + 5 scripts under `E:\COBOL\scripts\` + `E:\COBOL\.github\workflows\build-and-test.yml`.
