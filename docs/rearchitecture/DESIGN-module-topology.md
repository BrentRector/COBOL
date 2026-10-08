# DESIGN — Target Module / Folder / File Topology (Rearchitecture)

> Status: DESIGN (rearchitecture wave). Dimension owner deliverable.
> SSOT alignment: `docs/COBOLNET_DESIGN.md` (§16 G0–G8 order, §17 emitter decomposition, §18 settled decisions).
> Sibling design dimensions this depends on / feeds: **pass-pipeline** (Bind phase extraction), **bound-tree dispatch**
> (visitor), **diagnostics registry**, **edition framework**, **data-model / StorageForm**. This document decides ONLY
> the physical topology — *where every type lives, what it is named, and which assembly owns it*. It defers the internal
> algorithm of each extracted class to those sibling dimensions, but it fixes the class **boundaries and signatures**
> firmly enough that they can be executed mechanically.

---

## 1. The current problem (grounded in the survey + critique)

The greenfield tree `src/Cobol.Net.*` (~40k LOC, ~153 files, 4 projects) is functionally strong but its **topology has
drifted** in five concrete, survey-confirmed ways:

1. **God classes hidden behind partial-file slicing.** Three types dominate and each is ONE `sealed partial class`
   shattered across many files that share full private state:
   - `StatementBinder` — **23 files, ~9.4k LOC** (`Binding/Bound/StatementBinder*.cs`). Does five fused jobs:
     procedure-table build, statement dispatch, expression/condition binding, the relation checkpoint, and inline
     SR/edition validation.
   - `CSharpEmitter` — **15 files, ~9k LOC** (`CodeGen/CSharpEmitter*.cs`). Additionally *hosts the real middle-end
     orchestrator* (`CallEmitRunUnit`, ~12 implicit binder passes) — there is **no Binder phase boundary**; binding is
     fused into codegen.
   - `DataBinder` — **7 files, ~3.9k LOC** (`Binding/DataBinder*.cs`). Declaration + a 15-pass resolution pipeline +
     per-feature semantics (files, special-names, reports, linkage, external/global, pointers, OO, typedef) + ~30 public
     mutable collections that are the module's entire API surface.

   Partial files are a **naming-convention firewall, not real encapsulation** — every partial has full private access,
   so "how binding works" requires opening 21 files and cross-partial coupling is invisible to the compiler.

2. **Cross-layer write-backs and side-channels break phase ownership.** The emitter *mutates the binder's data model*
   (`DataItem.StoreAsImage` recomputed/written at 7+ sites across three layers, incl. `CSharpEmitter.MarkStoreAsImage`);
   `ReferenceResolver` mutates `WholeGroupReferenced` mid-resolve; OO orchestration lives in the emitter and *pushes
   emit-form facts backward* into `DataBinder.Oo` fields. The Binding/CodeGen boundary is not clean in either direction.

3. **The edition/version subsystem is fragmented across 3 namespaces / 2 assemblies with no home** — `EditionContext`,
   `ConstructRegistry`, `ReservedWords` live in `Cobol.Net.Compiler`; `ReservedWordEditionHints` lives in the *legacy*
   `legacy-root.Parsing` namespace inside `Cobol.Net.Frontend`; the preprocessor re-implements the strict/
   permissive severity policy twice. Because the canonical registry sits in Compiler (which Frontend cannot reference),
   the metadata is duplicated in the frontend — a topology defect, not a logic one.

4. **Namespace/assembly split — resolved (PHASE 01).** The tree once emitted into the legacy root namespace while the assemblies
   were already renamed `Cobol.Net.*`; it now declares `CobolNet.*`, the ANTLR package is `CobolNet.Frontend.Generated` (an MSBuild
   property) and `Frontend.cs` is self-contained. Every consumer still aliases `using Core = CobolParserCore`.

5. **Dead / mislabeled files in the tree.** Five top-level grammars (`CobolParserJsonXml.g4`, `CobolParserGenerics.g4`,
   `CobolParserOO.g4`, `CobolDialect.g4`, `CobolPreprocessor.g4`) are neither generated nor referenced. JSON/XML lives in
   a LIVE imported fragment (`Core/CobolExtensionsJsonXml.g4`) though it is non-ISO (0 spec occurrences). `FieldEmitter`
   (a 484-LOC DATA DIVISION emitter) is miscategorized under `CodeGen/Emit/` (value renderers). The `Accept` partials
   are the ACCEPT *verb*, not a visitor `Accept` — an actively-misleading name. Committed `.antlr`/`obj/antlr-lib`
   caches sit in the source tree.

The rearchitecture's topology job is to convert **partial-file slicing into real class boundaries**, give every drifted
subsystem **one owning folder/assembly**, kill the cross-layer channels by relocating their producers/consumers into a
**real Bind phase**, and complete the **namespace rename** so `Cobol.Net.*` is true top to bottom.

---

## 2. Target design

### 2.1 Project / assembly split — KEEP 4, ADD 1 (`Cobol.Net.Editions`)

The current four-project split (`Frontend` / `Compiler` / `Runtime` / `Cli`) is **correct and retained**. The one
structural change is extracting a new **lowest-layer** assembly so the edition catalogue has a single home both Frontend
and Compiler can reference (root cause of defect #3 above).

| Assembly | Depends on | Role (unchanged unless noted) |
|---|---|---|
| **`Cobol.Net.Editions`** *(NEW)* | *(nothing — leaf)* | The construct/reserved-word catalogue + edition-severity policy + `EditionInfo` value + diagnostic-code registry. Referenced by BOTH Frontend and Compiler. |
| **`Cobol.Net.Frontend`** | Editions | Preprocessor + ANTLR lexer/parser + parse tree + frontend diagnostics. |
| **`Cobol.Net.Runtime`** | *(nothing)* | Typed-native runtime kernel emitted programs bind against. |
| **`Cobol.Net.Compiler`** | Editions, Frontend, Runtime (compile-time ref for the emitted-API contract only) | Binder + Bind pipeline + CodeGen + Roslyn backend + driver. |
| **`Cobol.Net.Cli`** | Compiler | Thin System.CommandLine shell. |

Rationale for a **new assembly, not a folder**: Frontend cannot reference Compiler (would cycle), so the *only* way
Frontend and Compiler can share ONE `ConstructRegistry` + `EditionSeverityPolicy` is a common lower layer. This deletes
the parse-layer metadata copy and the two preprocessor severity copies in one move. `Cobol.Net.Editions` holds pure data
+ policy, no ANTLR, no Roslyn — it is genuinely leaf.

> Deferred (not now): a separate `Cobol.Net.BoundTree` assembly. The bound tree stays inside `Cobol.Net.Compiler` — it is
> consumed only by the binder and emitter which are both in Compiler; a separate assembly would add a boundary with no
> second consumer. Revisit only if a second backend (Cecil/CIL, per `project_dual_backend_goal`) is built.

### 2.2 Namespace policy — complete the rename NOW (do not wait for G8)

> **Superseded 2026-10-07 (owner, kb/Work PB2118 question 1):** every root becomes its project's name
> (`Cobol.Net.Editions`, `Cobol.Net.Frontend`, `Cobol.Net.Runtime` at P15 Cut 3, `Cobol.Net.Compiler`, `Cobol.Net.Cli`);
> the flips are `DESIGN-architecture-review.md` §8.2's waves PB2412–PB2415. The table below is the state before them
> (the compiler's root is `CobolNet` in its csproj, not the `CobolNet.Compiler` the table names).

**Decision: adopt `CobolNet.*` as the real namespace root across the greenfield tree as the FIRST mechanical step of the
rearchitecture, decoupled from the G8 legacy retirement.** The G8 "big-bang namespace rename" premise assumed the legacy
oracle shared the namespace; but the rearchitecture is a clean-slate reimplementation of the greenfield tree, so the
rename can and should happen up front — it is a pure `sed`-grade change and removes the `using Core = CobolParserCore`
alias noise from every file. Namespace roots:

| Assembly | Namespace root |
|---|---|
| `Cobol.Net.Editions` | `CobolNet.Editions` |
| `Cobol.Net.Frontend` | `CobolNet.Frontend` (generated parser: `CobolNet.Frontend.Generated`) |
| `Cobol.Net.Runtime` | `CobolNet.Runtime` (already partially so) |
| `Cobol.Net.Compiler` | `CobolNet.Compiler` with sub-namespaces per folder (below) |
| `Cobol.Net.Cli` | `CobolNet.Cli` |

The ANTLR package name is set in `Invoke-Antlr4CSharp.ps1` (currently hard-coded) → move to an MSBuild property
`<AntlrNamespace>CobolNet.Frontend.Generated</AntlrNamespace>` in the Frontend csproj and reference it in the generator
call, so it is single-sourced.

### 2.3 `Cobol.Net.Compiler` — target folder structure and sub-namespaces

```
Cobol.Net.Compiler/
├─ CompilerDriver.cs                         CobolNet.Compiler          (thin phase orchestrator — unchanged role)
├─ Pipeline/                                 CobolNet.Compiler.Pipeline (NEW — the explicit phase pipeline)
│    ├─ CompilationContext.cs                   (mutable carry: source→tree→bound→csharp→diagnostics)
│    ├─ IPass.cs / PassManifest.cs              (named passes, Requires/Produces, startup graph assert)
│    ├─ BindPhase.cs                            (runs the binder passes; produces BoundCompilation)
│    ├─ VersionConformancePass.cs               (the ONE edition-gating funnel over the bound tree; on errors, emit is unreachable)
│    ├─ EmitPhase.cs                            (BoundCompilation → C# text)
│    └─ RoslynPhase.cs                          (C# → dll; see Backend/)
│
├─ Model/                                    CobolNet.Compiler.Model    (NEW — the pure data model, was Binding/*)
│    ├─ DataItem.cs                             (slim core: level, name, Pic, Children, Parent, Occurs)
│    ├─ PicInfo.cs                              (pure value record; scanner extracted → PictureAnalyzer)
│    ├─ PictureAnalyzer.cs                      (NEW — the ~230-line Analyze scanner, split out of PicInfo)
│    ├─ StorageForm.cs                          (NEW — sealed discriminator; §2.7)
│    ├─ StrongTypeModel.cs                      (NEW — TYPEDEF/strong-typing side-table off DataItem)
│    ├─ RedefinesClass.cs / RedefinesModel.cs   (tiers A/B/C/D membership)
│    ├─ OccursSpec.cs / OdoModel.cs             (OCCURS/ODO/DYNAMIC geometry)
│    ├─ RenamesInfo.cs / Condition88.cs
│    ├─ FileModel.cs / CollatingModel.cs / OptionsModel.cs
│    ├─ RecordLayout.cs                         (NEW — the ONE offset/width/key-index service; §2.6)
│    └─ Place/                                  (the lvalue hierarchy — Place.cs + subtypes, OdoGroupPlace folded in)
│
├─ Binding/                                  CobolNet.Compiler.Binding  (the binders — real classes, not partials)
│    ├─ DataDivision/
│    │    ├─ DataBinder.cs                       (THIN orchestrator over the feature binders below)
│    │    ├─ EntryTreeBuilder.cs                 (BindEntries/BindEntry level-stack builder + clause decode)
│    │    ├─ FileControlBinder.cs                (FILE-CONTROL/SELECT + FD/SD + I-O-CONTROL + ResolveFiles)
│    │    ├─ SpecialNamesBinder.cs               (switches/alphabets/classes/currency/decimal-point — ENV concern)
│    │    ├─ ReportSectionBinder.cs              (RD/report groups)
│    │    ├─ LinkageBinder.cs / PointerBinder.cs
│    │    ├─ TypedefExpander.cs                  (ExpandTypes/CloneItem/ExpandType)
│    │    └─ Passes/                             (the ~15 resolution passes as named IPass — §2.5)
│    ├─ Procedure/
│    │    ├─ StatementBinder.cs                  (THIN dispatch table + shared BinderContext; §2.4)
│    │    ├─ BinderContext.cs                    (NEW — the injected shared context, replaces shared partial state)
│    │    ├─ ProcedureTableBuilder.cs            (paragraphs/sections/declaratives/pc + ResolveProcedure)
│    │    ├─ ExpressionBinder.cs                 (BindExpr chain)
│    │    ├─ ConditionBinder.cs                  (BindCondition + AbbrevCarry + CheckedRelational)
│    │    └─ Verbs/                              (one real class per verb-family; §2.4)
│    │         ├─ MoveBinder.cs   ArithmeticBinder.cs  IfEvaluateBinder.cs
│    │         ├─ SequentialIoBinder.cs  KeyedIoBinder.cs  FileLockBinder.cs  SortBinder.cs
│    │         ├─ StringBinder.cs  InspectBinder.cs  InitializeBinder.cs
│    │         ├─ CallBinder.cs  IntrinsicBinder.cs  UdfBinder.cs  ReportWriterBinder.cs
│    │         └─ AcceptDisplayBinder.cs  SetAlterBinder.cs  ExceptionBinder.cs
│    ├─ Bound/                                CobolNet.Compiler.Binding.Bound
│    │    ├─ BoundTree.cs                        (the record hierarchy — SSOT)
│    │    ├─ BoundNode.Visitor.cs                (NEW — IBoundVisitor<T> + source-gen'd exhaustive Accept; §2.8)
│    │    ├─ IBoundError.cs                      (NEW — marker over the 5 error-node families)
│    │    └─ BoundStoreAnalysis.cs               (renamed from BoundStores)
│    ├─ ReferenceResolver.cs                     (THIN funnel; sub-parsers extracted — §2.9)
│    ├─ NameResolver.cs                          (NEW — ResolveUnqualified/Qualified/FindDescendant, OO-scope aware)
│    ├─ SubscriptTokenParser.cs                  (NEW — subscript/refmod token machinery, out of ReferenceResolver)
│    └─ ScopeResolver.cs                         (NEW — the ONE scoped lookup, folds OO method shadowing; §2.9)
│
├─ Oo/                                       CobolNet.Compiler.Oo       (NEW — the OO subsystem, one home; §2.10)
│    ├─ OoClassTable.cs                          (PURE symbol table: Find/rosters/closures/Build)
│    ├─ OoConformance.cs                         (NEW — override/implements validation, split out)
│    ├─ OoMethodSymbol.cs / OoClassSymbol.cs / OoInterfaceSymbol.cs
│    ├─ OoMethodBinding.cs                       (NEW — bound signature, attached after data-bind; §2.10)
│    ├─ OoMethodDataBinder.cs                    (was DataBinder.Oo — method-scope data binding)
│    ├─ OoStatementBinder.cs                     (was StatementBinder.Oo — INVOKE/SET-objref/method-scope)
│    ├─ OoDriver.cs                              (NEW — the 8-step orchestration, was in CSharpEmitter.Call)
│    ├─ OoClassLayout.cs                         (NEW — emit-side facts, moved OUT of DataBinder)
│    └─ NamingConvention.cs                      (NEW — the ONE place for __GET_/__FACTORY/::INST:: names)
│
├─ Editions/  →  MOVED OUT to the Cobol.Net.Editions assembly (see §2.11)
│
├─ Validation/                               CobolNet.Compiler.Validation
│    └─ StatementValidation.cs                   (NEW — the inline SR checks lifted out of the binder; §2.4.
│                                                 Edition legality is NOT here — Pipeline/VersionConformancePass is the sole gate)
│
├─ CodeGen/                                  CobolNet.Compiler.CodeGen  (a PURE renderer over BoundCompilation)
│    ├─ CSharpEmitter.cs                         (THIN dispatch table; run-unit orchestration REMOVED to Pipeline)
│    ├─ CSharpEmitter.<Verb>.cs partials → real EmitXxx renderer classes under Emit/Statements/ (§2.4)
│    ├─ CodeWriter.cs
│    ├─ RuntimeApi.cs                            (NEW — typed façade over the ~60 emitted Runtime members; §2.12)
│    ├─ Emit/                                    (value/condition renderers — the §17 core, KEEP)
│    │    ├─ EmitContext.cs                       (renamed from EmissionContext; ReceiverContext scoped; §2.12)
│    │    ├─ NumericRenderer.cs  ConditionRenderer.cs  BooleanRenderer.cs
│    │    ├─ IntrinsicRenderer.cs  OperandRenderer.cs   (OperandText merged in; static channel deleted)
│    │    ├─ NumX.cs                              (3-case discriminated shape)
│    │    ├─ FigurativeConstants.cs               (NEW — the ONE fill service; §2.6)
│    │    ├─ CsLiteralCodec.cs                    (NEW — the ONE literal decode/encode; §2.6)
│    │    └─ LoudGuards.cs                        (split from EmitText grab-bag)
│    └─ DataDivision/                            (NEW — FieldEmitter re-homed here, split; §2.13)
│         ├─ RecordStructEmitter.cs   GroupImageCodec.cs
│         ├─ GroupValueSlicer.cs      ValueInitializer.cs
│
├─ Backend/                                  CobolNet.Compiler.Backend  (NEW — split from RoslynBackend)
│    ├─ RoslynCompiler.cs                        (pure C# → EmitResult; cached reference set)
│    └─ AssemblyPackager.cs                      (runtimeconfig + runtime-dll deploy)
│
└─ Diagnostics/  →  the registry moves to Cobol.Net.Editions (§2.11); thin sink stays if needed
```

### 2.4 God-class breakup #1 — `StatementBinder` → dispatch + collaborators

`StatementBinder` becomes a **thin core** holding only: `BindStatementCore` (the dispatch table), the shared
`BinderContext`, and the per-program-unit lifetime. Every verb family becomes a **real class** constructed over
`BinderContext` (ctor injection, no shared private state). Concrete decomposition:

| New class | Absorbs (current partials) | Public surface (illustrative) |
|---|---|---|
| `StatementBinder` (core) | `StatementBinder.cs` dispatch only | `BoundStatement BindStatement(StatementContext)` |
| `BinderContext` | the shared fields (`data`, `refs`, `Edition`, per-unit caches) | injected; read-only where possible |
| `ProcedureTableBuilder` | `.cs` collect + `.Declaratives` | `ProcedureTable Build(...)`, `int ResolveProcedure(name)` |
| `ExpressionBinder` | BindExpr chain in `.cs` | `BoundExpr Bind(ArithmeticExpressionContext)` |
| `ConditionBinder` | BindCondition/AbbrevCarry/CheckedRelational | `BoundCondition Bind(ConditionContext)` |
| `MoveBinder` | `.MoveFigurative` + BindMove | `BoundStatement Bind(MoveContext)` — resolves `MoveKind` |
| `ArithmeticBinder` | ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE in `.cs` | |
| `IfEvaluateBinder` | BindIf + `.Evaluate` | enforces EVALUATE WHEN-OTHER-last as a real diagnostic |
| `SequentialIoBinder` | OPEN/CLOSE/READ/WRITE/REWRITE in `.cs` | (was in the 1833-line core) |
| `KeyedIoBinder` | `.KeyedIo` | |
| `FileLockBinder` | `.FileLock` | |
| `SortBinder` | `.Sort` | consumes `RecordLayout` (not its own geometry) |
| `StringBinder` | `.StringUnstring` | |
| `InspectBinder` | `.Inspect` | |
| `InitializeBinder` | `.Initialize` | cursors yield structured `Place`, not C# strings |
| `CallBinder` | `.Call` | |
| `IntrinsicBinder` | `.Intrinsics` | hand-rolled arg parser deleted (§2.9) |
| `UdfBinder` | `.Udf` | |
| `ReportWriterBinder` | `.ReportWriter` | |
| `AcceptDisplayBinder` | `.Accept` (RENAME — this is the ACCEPT verb) | |
| `SetAlterBinder` | `.AlterSwitches` | |
| `ExceptionBinder` | `.Exceptions` + `.Boolean` | |
| `StatementValidation` | the inline `data.Edition.Error(...)` SR checks | non-edition statement legality only — "what is legal at which edition" is the `Pipeline/VersionConformancePass`; the binder is edition-agnostic (zero `ConstructRegistry.Check` calls) |

A shared `PhraseBlocks.BuildPair(StatementBlockContext[], bool notFirst)` helper (in `Binding/Procedure/`) replaces the
~8 duplicated ON/NOT-ON extractors.

### 2.5 God-class breakup #2 — `DataBinder` → thin orchestrator + named passes

`DataBinder.cs` keeps `Bind(program)` as a **thin two-phase driver** but delegates: `BindDeclarations` fans out to the
feature binders (`FileControlBinder`, `SpecialNamesBinder`, `ReportSectionBinder`, `LinkageBinder`, `PointerBinder`,
`EntryTreeBuilder`), and `BindResolve`'s 15 comment-ordered method calls become an **explicit `PassManifest`** — an
ordered list of named `IPass` objects each declaring `Requires`/`Produces`, asserted as a DAG at startup (owned by the
**pass-pipeline** sibling dimension; this doc only fixes *where the passes live*: `Binding/DataDivision/Passes/`). The
~30 public mutable collections are replaced by a read-only `DataModel` result object returned by `Bind()`.

Passes (names fixed here for the manifest): `ExpandTypesPass`, `ResolveUsageMarkersPass` (was `ResolveIndexItems` +
`InheritUsageClauses`), `InheritSignPass`, `ResolveRedefinesPass`, `ClassifyRedefinesPass`, `CheckStrongTypesPass`,
`OoRouteMethodRedefinesPass`, `OdoResolvePass`, `DynamicResolvePass`, `ResolveFilesPass`, `GateNationalPass`,
`ResolveReportsPass`, `ExternalGlobalPass`, `PointerBindPass`, `StorageFormPass` (NEW — §2.7), `UsageCollectionPass`
(NEW — collects `WholeGroupReferenced` *after* procedure binding, so the binder no longer gains facts from downstream).

### 2.6 One canonical mechanism per job (topology of the de-duplication)

The critique's duplication findings each resolve to a **single owning file** in the target tree:

| Job | ONE owner (target path) | Deletes |
|---|---|---|
| Literal decode/encode | `CodeGen/Emit/CsLiteralCodec.cs` | 3 `DecodeCobolString` copies + hard-coded `'"'` delimiter tests |
| Figurative fill | `CodeGen/Emit/FigurativeConstants.cs` | `FigurativeFill`/`FillCharFor`/`FigurativeFillChar`/`FigFill` |
| Record offset/width/key geometry | `Model/RecordLayout.cs` | `SortPhysicalWidth`/`KeyedAreaOffset`/… (Sort vs KeyedIo dup) |
| Phrase-pair (ON/NOT-ON) | `Binding/Procedure/PhraseBlocks.cs` | ~8 inline copies |
| Tree root walk | `DataItem.Root` (+ scoped variant on `ScopeResolver`) | 4 `RootOf` copies |
| Storage-form decision | `Model/StorageForm.cs` + `StorageFormPass` | `MarkStoreAsImage` + 7 `StoreAsImage` write sites |
| Powers of ten | one `Pow10` in Runtime `Numeric/` | 4 duplicate loops |

> The **apostrophe-delimited VALUE literal silent-miscompile** (latent-bugs HIGH) is fixed *by construction* once
> `CsLiteralCodec` is the only literal boundary — `AllLiteralText`, `GroupValueText`, and the Report Writer SOURCE path
> all route through it. This is a topology-enforced correctness fix.

### 2.7 `StorageForm` — the topology of the StoreAsImage fix

Replace the scattered `(Pic, StoreAsImage, Class.Tier, IsDynamicTable)` inference with ONE sealed discriminator
`Model/StorageForm.cs`: `NativeLong | Int128 | Float | Double | StringImage | TierBWindow | TierCByte | DynTable |
ObjectRef | Pointer`. It is computed **once** in `StorageFormPass` (after `UsageCollectionPass` so whole-group use is
known) and stored **init-only** on `DataItem`. CodeGen reads it and can no longer mutate it — the emitter's cross-layer
write (`CSharpEmitter.cs:50-68`) is deleted. `DataItem`'s recursive computed properties
(`IsCharacterImage`/`ImageWidth`/`StrongRoot`) become init-only fields set by this bottom-up O(n) pass (also the
efficiency MEDIUM fix).

### 2.8 Bound-tree dispatch topology

`BoundStatement`/`BoundExpr`/`BoundOperand`/`BoundCondition` gain `Accept(IBoundVisitor<T>)` (source-generated
exhaustive dispatch — owned by the **bound-tree-dispatch** sibling dimension; this doc fixes the file home:
`Binding/Bound/BoundNode.Visitor.cs`). The two hand-maintained god-switches (`BindStatementCore`, `EmitStatement`) and
the renderer switches route through it so a missing arm is a **compile error**. The five error-node families get one
`IBoundError` marker (`Binding/Bound/IBoundError.cs`).

### 2.9 `ReferenceResolver` decomposition

`ReferenceResolver` becomes a thin orchestrator; three collaborators are extracted into `Binding/`:
`SubscriptTokenParser` (Split/Render/Interpret/CollectLeaf), `NameResolver` (unqualified/qualified/descendant/
file-qualifier), and `ScopeResolver` (the ONE scoped lookup that understands OO method shadowing — collapses the
`LookupData`/`LookupDataInScopeOf`/`TryGetVisibleIndexField`/`IndexFieldFor` quadruple). OO property binding
(`ReferenceResolver.ResolveObjectProperty`) moves to `Oo/OoStatementBinder.cs`. The hand-rolled intrinsic-arg expression parser is
deleted — FUNCTION args parse as real grammar `arithmeticExpression` (edition-gated) and bind through
`ExpressionBinder` (removes the 3rd parallel numeric evaluator).

### 2.10 OO — from cross-layer sprawl to one `Oo/` folder + `CobolNet.Compiler.Oo`

All four OO slices move under `Oo/`. `OoClassTable` splits into a pure symbol table + `OoConformance` (validation).
`OoDriver` owns the 8-step orchestration currently inlined in `CSharpEmitter.CallEmitRunUnit` (removing OO pass-ordering
from CodeGen). Emit-form facts leave `DataBinder.Oo` for `OoClassLayout` (emit-side). The ambient mutable binder flags
(`ActiveMethodScope`/`OoInFactory`/`OoCurrentClass`/`OoIsClassUnit`) become a scoped push/pop on `BinderContext`
(using-disposable that guarantees reset). All OO C# name conventions centralize in `NamingConvention`.

### 2.11 Edition framework → `Cobol.Net.Editions`

Move to the new leaf assembly (namespace `CobolNet.Editions`): `ConstructRegistry`/`ConstructDialectStatus`,
`ReservedWords`(+`.Table`), `EditionCodes`, the diagnostic-code **`DiagnosticDescriptors` registry** (NEW — the ONE
home for the 163 codes, generated consts + ISO §/severity/message-template/suppress-key), and a single
`EditionSeverityPolicy.Removed(...)`. There is NO reverse-signature recogniser in this topology — hard-reserved
constructs gate at bind time through the `VersionConformancePass`, and the vendor JSON/XML COBOL0313 disposition lives
in `CobolErrorStrategy` as a token-keyed vendor hint (a parse-error re-diagnosis, not an ISO edition gate).
`EditionContext` **splits**: an immutable `EditionInfo` (DialectLevel/Permissive/MaxDigits) stays in Editions; the
diagnostic-sink half becomes `DiagnosticSink` (in `Frontend/Common` or Editions, `IReadOnlyList` views) — the 290
`data.Edition.Error(...)` sites retarget to the sink. `constructs.json`/`reserved-words.json` remain the canonical
catalogue with drift tests.

### 2.12 CodeGen ↔ Runtime coupling

Introduce `CodeGen/RuntimeApi.cs` — a typed façade returning fragment strings for the ~60 emitted Runtime members, so a
Runtime rename breaks ONE file at compile time instead of silently at generated-compile time. `EmissionContext` renames
to `EmitContext` (ends the cross-tree name collision with the legacy tree) and its four public mutable fields
(`TargetScale`/`TargetReal`/`TargetRounding`/`InSizeErrorContext`) become a scoped `ReceiverContext` passed into
`Render`/`AsNum` (or a `using ctx.WithReceiver(...)` disposable) — closing the H1 write-before-read hazard by
construction.

### 2.13 CodeGen/Emit hygiene

`FieldEmitter` moves out of `CodeGen/Emit/` (value renderers) into `CodeGen/DataDivision/` and splits into
`RecordStructEmitter` / `GroupImageCodec` / `GroupValueSlicer` / `ValueInitializer`. `OperandText` merges into
`OperandRenderer` (one operand channel). `RoslynBackend` splits into `Backend/RoslynCompiler` (pure, cached reference
set — the efficiency HIGH fix) + `Backend/AssemblyPackager` (deploy/runtimeconfig).

### 2.14 Frontend cleanup

Delete the five dead top-level grammars. Quarantine JSON/XML: strip `jsonStatement`/`xmlStatement` from
`Core/CobolExtensionsJsonXml.g4` and their `{is2014()}?` wiring; move `inlineMethodInvocationStatement` into
`Core/CobolOO.g4`; delete the now-empty fragment. Remove committed `.antlr` / `obj/antlr-lib` caches from source control
(gitignore). Fix the stale `Frontend.cs` "reuses the legacy compiler assembly" banner. Rename the generated
package to `CobolNet.Frontend.Generated` via the MSBuild property.

### 2.15 Naming & partial-file convention (the rule going forward)

1. **Partial files are ALLOWED only for source-generated halves** (e.g. `*.Visitor.g.cs`, ANTLR `Generated/`). A logical
   unit that a human maintains is **one class in one file**, OR a set of **real collaborator classes** — never a
   `sealed partial class X` split for size.
2. **File name = the single public type it contains.** `KeyedIoBinder.cs` contains `KeyedIoBinder`.
3. **Folder = sub-namespace.** `Binding/Procedure/Verbs/KeyedIoBinder.cs` → `CobolNet.Compiler.Binding.Procedure.Verbs`.
4. **No verb named `Accept` unless it is the ACCEPT verb** — resolved (`AcceptDisplayBinder`); `Accept(IBoundVisitor)` is
   the only visitor Accept.
5. Each subsystem file carries a short header block: *owns / requires-passes / produces* (preserving the strong
   §-citation comment culture).

---

## 3. Current → target module_changes table

Legend: **S**=split, **M**=move, **R**=rename, **X**=delete, **C**=create, **F**=merge.

### Projects / assemblies
| # | Action | From | To | Why |
|---|---|---|---|---|
| 1 | C | — | `src/Cobol.Net.Editions/` | Leaf assembly Frontend+Compiler both reference; kills edition metadata dup |
| 2 | R (ns) | `namespace legacy-root.*` | `namespace CobolNet.Compiler.*` (per §2.3 sub-ns) | Complete the deferred rename up front |
| 3 | R (ns) | `legacy-root.Generated` | `CobolNet.Frontend.Generated` | Remove `using Core=` alias; MSBuild property |

### Frontend
| # | Action | From | To | Why |
|---|---|---|---|---|
| 4 | X | `Grammar/CobolParserJsonXml.g4`, `CobolParserGenerics.g4`, `CobolParserOO.g4`, `CobolDialect.g4`, `CobolPreprocessor.g4` | — | Dead: not generated, not referenced |
| 5 | S/X | `Grammar/Core/CobolExtensionsJsonXml.g4` | JSON/XML stripped; `inlineMethodInvocationStatement`→`Core/CobolOO.g4`; file deleted | JSON/XML non-ISO (0 spec occ) |
| 6 | X | `Grammar/.antlr`, `obj/antlr-lib/*.g4` caches | — (gitignore) | Build output in source tree |
| 7 | X | `ReservedWordEditionHints.cs` (ns `legacy-root.Parsing`) | — (vendor JSON/XML COBOL0313 hint → `CobolErrorStrategy`, token-keyed) | No reverse-signature recogniser; the `VersionConformancePass` is the sole edition gate |
| 8 | edit | `Pipeline/Frontend.cs` stale banner | corrected banner | Understandability |

### DataBinder god-class → binders + model + passes
| # | Action | From | To | Why |
|---|---|---|---|---|
| 9 | S | `Binding/DataBinder.cs` (1723) | `Binding/DataDivision/DataBinder.cs` (thin) + `EntryTreeBuilder.cs` + `Passes/*Pass.cs` | Orchestrator vs builder vs passes |
| 10 | S/M | `Binding/DataBinder.Switches.cs` | `Binding/DataDivision/SpecialNamesBinder.cs` | ENV-division concern, not DATA |
| 11 | S/M | `DataBinder.Reports.cs` | `Binding/DataDivision/ReportSectionBinder.cs` | Self-contained |
| 12 | S/M | `DataBinder.Linkage.cs` | `Binding/DataDivision/LinkageBinder.cs` | |
| 13 | S/M | `DataBinder.Ptr.cs` | `Binding/DataDivision/PointerBinder.cs` | |
| 14 | S/M | `DataBinder.Oo.cs` | `Oo/OoMethodDataBinder.cs` | One OO home |
| 15 | C | (FILE-CONTROL/FD/I-O-CONTROL logic in DataBinder.cs) | `Binding/DataDivision/FileControlBinder.cs` | Extract file concern |
| 16 | C | (ResolveRedefines/Classify/Tier logic) | `Binding/DataDivision/Passes/ResolveRedefinesPass.cs` + `ClassifyRedefinesPass.cs` | Named passes |
| 17 | C | (ExpandTypes/CloneItem) | `Binding/DataDivision/TypedefExpander.cs` | |
| 18 | C | (implicit BindResolve order) | `Pipeline/PassManifest.cs` + `IPass.cs` | Explicit DAG-asserted ordering |

### Data model → `Model/`
| # | Action | From | To | Why |
|---|---|---|---|---|
| 19 | M/S | `Binding/DataItem.cs` | `Model/DataItem.cs` (slim) + `Model/StrongTypeModel.cs` | Immutable core; strong-typing side-table |
| 20 | M/S | `Binding/PicInfo.cs` | `Model/PicInfo.cs` (value) + `Model/PictureAnalyzer.cs` | Split scanner from value record |
| 21 | M | `Binding/{RedefinesModel,OdoModel,FileModel,CollatingModel,OptionsModel,Condition88,RoundingModes}.cs` | `Model/` | Pure model home |
| 22 | M/S | `Binding/Place.cs` (+ OdoGroupPlace) | `Model/Place/` (folded, PlaceDecorator base) | Consolidate lvalue |
| 23 | C | (offset/width/key geometry, Sort+KeyedIo+Field+Odo) | `Model/RecordLayout.cs` | ONE layout service |
| 24 | C | (StoreAsImage inference, 7 sites) | `Model/StorageForm.cs` + `Passes/StorageFormPass.cs` | ONE derived discriminator, init-only |

### StatementBinder god-class → dispatch + verb classes
| # | Action | From | To | Why |
|---|---|---|---|---|
| 25 | S | `Binding/Bound/StatementBinder.cs` (1833) | `Binding/Procedure/StatementBinder.cs` (thin) + `BinderContext.cs` + `ProcedureTableBuilder.cs` + `ExpressionBinder.cs` + `ConditionBinder.cs` + `Verbs/{Move,Arithmetic,IfEvaluate,SequentialIo,SetAlter}Binder.cs` | Real classes over injected context |
| 26 | M/R | `StatementBinder.{KeyedIo,FileLock,Sort,StringUnstring,Inspect,Initialize,Call,Intrinsics,Udf,ReportWriter}.cs` | `Binding/Procedure/Verbs/{KeyedIo,FileLock,Sort,String,Inspect,Initialize,Call,Intrinsic,Udf,ReportWriter}Binder.cs` | One class per verb family |
| 27 | R | `StatementBinder.Accept.cs` | `Binding/Procedure/Verbs/AcceptDisplayBinder.cs` | Kill misleading "Accept" name |
| 28 | M | `StatementBinder.{Oo,MoveFigurative→Move,Evaluate→IfEvaluate,Corresponding,Boolean+Exceptions→Exception,AlterSwitches→SetAlter}.cs` | per §2.4 rows | Cohesion |
| 29 | C | (inline `data.Edition.Error` SR checks) | `Validation/StatementValidation.cs` (edition legality → `Pipeline/VersionConformancePass.cs`) | Validation layer, binder stays "no-IR" and edition-agnostic |
| 30 | C | (8 ON/NOT-ON extractors) | `Binding/Procedure/PhraseBlocks.cs` | ONE phrase-pair helper |
| 31 | R | `Binding/Bound/BoundStores.cs` | `Binding/Bound/BoundStoreAnalysis.cs` | It is an analysis, not storage |
| 32 | C | (BoundTree dispatch) | `Binding/Bound/BoundNode.Visitor.cs` + `IBoundError.cs` | Exhaustive compile-checked dispatch |

### ReferenceResolver
| # | Action | From | To | Why |
|---|---|---|---|---|
| 33 | S | `Binding/ReferenceResolver.cs` (685) | thin `ReferenceResolver.cs` + `NameResolver.cs` + `SubscriptTokenParser.cs` + `ScopeResolver.cs` | Single-responsibility; ONE scoped lookup |

### OO → `Oo/`
| # | Action | From | To | Why |
|---|---|---|---|---|
| 34 | S/M | `Binding/OoClassTable.cs` | `Oo/OoClassTable.cs` (pure) + `Oo/OoConformance.cs` | Symbol table vs validator |
| 35 | M | Oo{Method,Class,Interface}Symbol (in OoClassTable) | `Oo/OoMethodSymbol.cs` etc. + `Oo/OoMethodBinding.cs` (NEW) | Phase-explicit; too-early read = type error |
| 36 | M | `Binding/Bound/StatementBinder.Oo.cs` | `Oo/OoStatementBinder.cs` | One home |
| 37 | C/M | `CSharpEmitter.CallEmitRunUnit` OO steps | `Oo/OoDriver.cs` | OO orchestration out of CodeGen |
| 38 | C | emit-form facts in `DataBinder.Oo` | `Oo/OoClassLayout.cs` + `Oo/NamingConvention.cs` | Emit facts leave Binding |

### CSharpEmitter god-class → thin dispatch + renderers + Pipeline
| # | Action | From | To | Why |
|---|---|---|---|---|
| 39 | S | `CodeGen/CSharpEmitter.Call.cs` (`CallEmitRunUnit`) | `Pipeline/BindPhase.cs` (binder passes) + `Pipeline/EmitPhase.cs` | Real Bind phase; emitter renders only |
| 40 | R/M | `CSharpEmitter.{Evaluate,Accept,Corresponding,Inspect,AlterSwitches,StringUnstring,Exceptions,Sort,ReportWriter,KeyedIo,Ptr,Oo,Initialize}.cs` | `CodeGen/Emit/Statements/Emit{IfEvaluate,AcceptDisplay,Corresponding,...}.cs` real renderer classes | Match binder decomposition |
| 41 | M/S | `CodeGen/Emit/FieldEmitter.cs` | `CodeGen/DataDivision/{RecordStructEmitter,GroupImageCodec,GroupValueSlicer,ValueInitializer}.cs` | Miscategorized; 4 concerns |
| 42 | F | `CodeGen/Emit/OperandText.cs` | merged into `CodeGen/Emit/OperandRenderer.cs` | ONE operand channel |
| 43 | R/refactor | `CodeGen/Emit/EmitCore.cs` (`EmissionContext`, `EmitText`) | `Emit/EmitContext.cs` + `CsLiteralCodec.cs` + `FigurativeConstants.cs` + `LoudGuards.cs` | Rename + split grab-bag; scoped ReceiverContext |
| 44 | C | (~60 bare Runtime member strings) | `CodeGen/RuntimeApi.cs` | Typed façade; rename-safe |
| 45 | S | `CodeGen/RoslynBackend.cs` | `Backend/RoslynCompiler.cs` + `Backend/AssemblyPackager.cs` | Pure compile vs deploy; cache refs |

### Editions → new assembly
| # | Action | From | To | Why |
|---|---|---|---|---|
| 46 | M/S | `Binding/EditionContext.cs` | `Cobol.Net.Editions/EditionInfo.cs` (immutable) + `DiagnosticSink.cs` | Split "edition" from "diagnostic sink" |
| 47 | M | `Validation/{ReservedWords,ReservedWords.Table,EditionCodes}.cs`, `Validation/ConstructDialectStatus.cs` | `Cobol.Net.Editions/` | ONE catalogue home |
| 48 | C | (163 bare code literals) | `Cobol.Net.Editions/DiagnosticDescriptors.cs` (+ `docs/DIAGNOSTICS.md` generated) | Central registry; matrix/--suppress targets |
| 49 | S/X | `Validation/EditionValidator.cs` | absorbed by `Pipeline/VersionConformancePass.cs` (its §8.9 reserved-word funnel moves into the pass) | ONE edition-gating funnel over the bound tree |

### CompilerDriver / Cli
| # | Action | From | To | Why |
|---|---|---|---|---|
| 50 | refactor | `CompilerDriver.cs` | phase-delegate list over `Pipeline/CompilationContext`; top-level try/catch → `Outcome.InternalError` | Explicit ordered abortable pipeline: bind → `VersionConformancePass` → HALT on errors → emit; CheckOnly stops after the pass (verdicts include pass diagnostics) |
| 51 | F | `Cli/CliOptions.cs` + `CompilerDriver.Options` | one `Options` | No silently-dropped field |

### Runtime (topology-light — mostly stays)
| # | Action | From | To | Why |
|---|---|---|---|---|
| 52 | F | `CobolNum.Pow10Wide` (+3 dup loops) | one internal `Pow10` table | De-dup (efficiency MEDIUM) |
| — | keep | all `Runtime/{Numeric,Strings,Text,Tables,IO,Control,Exceptions,Intrinsics}/` folders | already cohesive | Runtime topology is sound |

---

## 4. Migration notes — keeping the battery green throughout

The battery (greenfield conformance + unit + characterization + the FULL NIST legacy guard — current counts live in the
STATUS banners) must stay green at **every commit**. Sequence
the topology work as **behavior-preserving mechanical steps**, each independently green:

1. **Wave 0 — namespace rename (mechanical, zero behavior).** `sed` the greenfield tree `legacy-root.* →
   CobolNet.Compiler.*`; set `<AntlrNamespace>` MSBuild property; drop the `using Core=` aliases. Update
   `InternalsVisibleTo` and test project references. One commit, guard green. **Do this first** — every later move is a
   file relocation within a stable namespace scheme.
2. **Wave 1 — dead-file deletion + Frontend hygiene** (changes 4–6, 8). No live code references them; guard green
   immediately. JSON/XML strip (change 5) is behavior-affecting only if a test exercises it — confirm 0 conformance tests
   use JSON/XML first (dossier says 0 spec occurrences), then delete.
3. **Wave 2 — `Cobol.Net.Editions` extraction** (changes 1, 7, 46–48). Move types with namespace-forwarding: keep the
   old namespaces as `[Obsolete]` type-forwarders or global-usings during the move so the 290 call sites compile
   unchanged, then retarget in a follow-up. The `DiagnosticDescriptors` registry can be introduced **additively** (codes
   still strings) and call sites migrated incrementally.
4. **Wave 3 — data model to `Model/`** (changes 19–24). Pure relocation first (green), THEN introduce `StorageForm` +
   `StorageFormPass` behind the existing `StoreAsImage` (compute both, assert equal in a debug guard, flip readers over,
   delete the mutable flag last). This is the riskiest correctness step — gate it with the differential oracle.
5. **Wave 4 — pass manifest** (changes 9, 16–18). Wrap the existing 15 `BindResolve` calls in named `IPass` objects with
   the *current* order; assert the DAG matches; only then reorder/insert `UsageCollectionPass`/`StorageFormPass`.
6. **Wave 5 — extract the Bind phase from CSharpEmitter** (change 39). Move `CallEmitRunUnit`'s binder steps into
   `Pipeline/BindPhase.cs` returning `BoundCompilation`; the emitter consumes it. This is where the "no Binder phase
   boundary" defect closes. Do it as a pure extraction (same call order) before any decomposition of the emitter itself.
7. **Wave 6 — StatementBinder & CSharpEmitter class breakups** (changes 25–32, 40–43). Per verb family, in ~20 small
   commits: extract the binder class + its renderer class + wire through `BinderContext`/`EmitContext`, run guard, next.
   The `BoundNode.Visitor` (change 32) lands early in this wave so each extracted renderer registers a compile-checked
   arm. **One verb at a time** (feedback_tiered_gates).
8. **Wave 7 — OO to `Oo/`** (changes 34–38) and **ReferenceResolver split** (change 33). OO last among the binders
   because `OoDriver` depends on the Bind phase (wave 5) already existing.
9. **Wave 8 — Backend split + de-dup helpers** (changes 44–45, 52, 23, 30) and the driver pipeline (changes 50–51).
10. **Legacy retirement (G8, out of this dimension's critical path).** Once the greenfield tree is fully `CobolNet.*` and
    the differential oracle is no longer needed, delete `src/CobolSharp.*`. The namespace rename done in Wave 0 means G8
    is now *just a deletion*, not a rename big-bang.

**Discipline per commit:** guard-fast + greenfield suites green; a shared-`.g4` change (waves 1) runs the FULL legacy
guard; each extracted class ships with the header block; DEVLOG entry per landing on main (kb/Work PB2605).

---

## 5. Risks

1. **StorageForm cutover (Wave 3) is the correctness hot-spot.** `StoreAsImage` is mutated by 7 sites incl. the emitter;
   a subtly-different derived rule silently mis-scales/mis-images data. *Mitigation:* compute-both-and-assert-equal
   behind a debug guard across the whole conformance corpus before deleting the mutable flag; drive with the differential
   oracle.
2. **Namespace rename touching the ANTLR generated package.** The generator hard-codes the package; if the MSBuild
   property isn't threaded correctly the regen fails the build on fresh checkout.
   *Mitigation:* verify regen on both Windows and WSL (feedback_generated_parser_is_a_build_output) in Wave 0.
3. **Cross-partial hidden coupling surfaces as compile errors during Wave 6.** Real class boundaries will expose
   currently-implicit shared-state reads. *Mitigation:* `BinderContext`/`EmitContext` carry the genuinely-shared state
   explicitly; anything else that surfaces is a latent coupling bug to fix, not to re-hide.
4. **`Cobol.Net.Editions` reference direction.** If any edition type accidentally depends on a Frontend or Compiler type,
   the leaf assembly won't build. *Mitigation:* the split is data+policy only; the `VersionConformancePass` (needs the
   bound tree) deliberately stays in Compiler.
5. **Volume of mechanical renames** across ~150 files risks reference-drift in test projects and `InternalsVisibleTo`.
   *Mitigation:* Wave 0 is a single scripted commit; CI is the backstop.
6. **Scope creep into sibling dimensions.** This doc fixes *topology*; the pass-DAG algorithm, the visitor codegen, and
   the diagnostic-registry schema belong to sibling dimensions. *Mitigation:* the table cites file homes, not internal
   algorithms.

---

## 6. Open questions for the owner

1. **Namespace rename timing (§2.2).** This doc recommends doing the `legacy-root.* → CobolNet.*` rename **now**
   (Wave 0), decoupled from G8, because the rearchitecture is a clean-slate greenfield reimplementation and the rename is
   mechanical. The current SSOT (§1.4 / csproj banner) defers it to a G8 big-bang. **Confirm we may pull the rename
   forward.**
2. **New `Cobol.Net.Editions` assembly vs a shared folder link.** Recommended: a real assembly (clean leaf, no cycle).
   Acceptable alternative: keep the registry in Compiler and give Frontend a *generated* copy from `constructs.json` at
   build time. Assembly is cleaner; confirm the extra project is acceptable.
3. **JSON/XML deletion vs quarantine (§2.14).** Delete outright (recommended — non-ISO, 0 spec occurrences) or keep
   behind a `--enable-json-xml` off-by-default flag for any downstream user? Confirm outright deletion.
4. **`Cobol.Net.BoundTree` as its own assembly** — deferred here (single consumer). Revisit only if the second backend
   (Cecil/CIL, `project_dual_backend_goal`) is actually scheduled. Is it?
5. **Depth of the `Verbs/` folder split.** ~18 verb-binder classes is a real decomposition but a lot of files. Acceptable,
   or prefer coarser grouping (e.g. one `FileIoBinder` for Sequential+Keyed+Lock+Sort)? Recommendation: keep them
   separate (cohesion > file count) but group Sequential+Keyed if the owner prefers fewer files.
6. **`Tier-C ByteCanonical`** — the one sanctioned `byte[]` boundary of the typed-native invariant is declared but
   unimplemented. Topology reserves `Model/StorageForm.TierCByte` + a `GroupImageCodec` home for it. Implement during
   rearchitecture, or keep as a single documented rejection? (Correctness-completeness call — see iso-pending.)
