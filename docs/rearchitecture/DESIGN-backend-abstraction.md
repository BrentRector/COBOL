# DESIGN — Backend Abstraction & the Backend-Neutrality Contract (dual codegen backends)

**Dimension:** The `ICodeGenBackend` seam made *real and enforced* — the precise, testable contract the bound tree /
`Place` must satisfy so a second codegen backend (direct CIL via Mono.Cecil) is droppable in **without touching the
frontend, binder, or bound tree**; the interface both backends implement; how both call the ONE
`Cobol.Net.Runtime`; the concrete additions each of PHASE-05/06/07 needs; and the executable **backend-contract
test** that fails the build if a bound node ever exposes C#.

**Status:** DESIGN (rearchitecture target). Author: backend-abstraction elevation pass. Date: 2026-07-07.

**Relationship to the existing plan — BUILD ON, do not contradict.** This document is the decision-complete
elaboration of the dual-backend goal the plan already names but under-weights. It **extends**:
- `DESIGN-codegen-backend.md` (§2.2 `ICodeGenBackend`, §2.3 structural `Place`, §3 `RuntimeApi`, §6 R5 neutrality
  test) — every decision here is compatible with that doc; where it adds detail it says so explicitly.
- `DESIGN-binder-bound-tree.md` (§3.3 "no lowered IR; semantic normalization on the bound tree") — this doc **adds
  the missing guarantee** that §3.3 states as intent but does not make enforceable: the bound tree carries **no C#**.
- `DESIGN-module-topology.md` (§2.1 deferred `Cobol.Net.BoundTree` assembly; Open Q4 "is a second backend
  scheduled?") — this doc answers **yes** (PHASE-16) and fixes the CIL backend's assembly home.
- `docs/COBOLNET_DESIGN.md` §1.1 (the 5-phase pipeline; `--backend roslyn|cil`; "NO *shared* lowered IR"), §1.3
  (`Place` is "the ROSLYN backend's RENDERING of a backend-neutral structural resolution"), §18 #23.

**Upholds the four owner-locked invariants** (SSOT §1.2) and the dual-backend goal (`project_dual_backend_goal`,
SSOT §1.1/§18 #23): typed-native data only; native scaled-integer numerics; one PC dispatcher; idiomatic C# where
the construct allows — **plus** the fifth, elevated here to first-class: *the bound tree is backend-neutral, and that
neutrality is enforced by a test, not by convention.*

---

## 0. TL;DR — the decisions

1. **Neutrality is a CONTRACT with a test, not a comment.** Today the bound tree's own doc-comment claims neutrality
   (`BoundTree.cs:7-13`) while `Place` returns C# strings (`Place.cs:22-25`) and several `Bound*` statement nodes
   carry pre-rendered C# access-path strings (`BoundSetCapacity.TablePath` `BoundTree.cs:493`; `BoundSearch`
   `BoundTree.cs:508-511`; `BoundIndexRef` `BoundTree.cs:109`; `BoundMethod.CsName` `BoundTree.cs:32`). §1 defines
   exactly what a bound node **MAY** and **MUST NEVER** contain and enumerates every current leak with its removal.
2. **`ICodeGenBackend` consumes an immutable `BoundCompilation` and produces a `BackendArtifact`** (§2) — the same
   seam `DESIGN-codegen-backend.md §2.2` declares, with the **shared vs per-backend** split made concrete: the
   exhaustive `IBound*Visitor<T>` interfaces, the bound tree, the `NameMangler`, and a new backend-neutral
   `RuntimeMembers` catalogue are **shared**; the visitor *implementations*, the `Place` rendering, and all
   structure→branch lowering are **per-backend**.
3. **Both backends call the ONE `Cobol.Net.Runtime` through ONE ABI catalogue.** `DESIGN-codegen-backend.md §3`'s
   `RuntimeApi` (a C#-fragment-string façade) is **Roslyn-specific**; it is generalized here into a neutral,
   `nameof`-anchored `RuntimeMembers` (the typed list of runtime members) with two thin renderers over it —
   `RoslynRuntimeApi` (→ C# text) and `CilRuntimeApi` (→ Cecil `MethodReference`) — so a runtime rename breaks
   **both** backends at compile time, singular-pattern preserved.
4. **The second backend is Mono.Cecil, in its own assembly** `Cobol.Net.Backend.Cil` (§3.2) — recommendation with
   rationale over `System.Reflection.Emit` in §3.3. The seam-proof milestone (PHASE-16 Milestone 0) deliberately
   uses an **in-box** `System.Reflection.Emit` DISPLAY-only backend + a `NullBackend` so the seam is proven real
   **before** the Cecil dependency is taken.
5. **PHASE-05/06/07 each gain one concrete guarantee** (§5) so the IR is neutral and the seam is executably proven
   the moment `Place` goes structural — the neutrality can then never silently rot across phases 08–15.
6. **The backend-contract test** (§6) is the enforcement: a reflection + analyzer test that FAILS the build if any
   `Place` subtype or `Bound*` node exposes a `string`-returning render member or stores a raw C# identifier/path.

---

## 1. The backend-neutrality contract (the core deliverable)

The bound tree (`BoundCompilation`) is the **single semantic model both backends consume**. For a CIL backend to be
droppable in without touching the frontend/binder/bound tree, every fact a backend needs must be present in a
**target-independent** form, and no fact may presuppose C#.

### 1.1 What a bound node MAY contain (the pure semantic model)

- **Resolved symbols** — a `DataItem`, `FileModel`, `ReportModel`, `Condition88`, `IntrinsicSig`, `OoMethodSymbol`,
  a paragraph **pc index** (`int`), a `FileOpenMode`/`SetCapacityKind`/`CobolRounding` enum. References, not names.
- **A STRUCTURAL `Place` lvalue** — an accessor chain of `AccessSegment`s (root symbol + member steps + `BoundExpr`
  subscripts + a ref-mod `BoundExpr` span), per `DESIGN-codegen-backend.md §2.3` / `COBOLNET_DESIGN.md §1.3`. The
  *structure* of the location, never its C# rendering.
- **Categories & scaled-integer numeric facts** — `PicCategory`, `Usage`, `StorageForm` (the P5 discriminator),
  fractional `Scale`, `ImageWidth`, `Real`/`Dec` flags. These are COBOL/`.NET-shape` facts (scale is compile-time
  metadata per SSOT §1.2 #2), target-independent.
- **Literal *values*** — a decoded string value (`BoundStringLiteral.Value`), or numeric **source text** kept for
  the backend to scale (`BoundNumLiteral.Text` `BoundTree.cs:93`, `BoundNumericLiteral.Text`). Source text is
  neutral: it is *data to be scaled*, and the scaling rule is identical for both backends.
- **Neutral runtime-data strings** — e.g. `EcStatementInfo.Location` (`BoundTree.cs:606-610`), the
  §15.30.3-formatted `"element; para; line"` string. This is a **COBOL runtime datum** (it ends up in a program's
  last-exception state), not C# code — allowed.
- **Structured control-flow shape** — `BoundIf(cond, then, else)`, `BoundInlinePerform(control, body)`,
  `BoundOutOfLinePerform(startPc, endPc, control)`, `BoundParagraph.Sentences`. The structure both backends walk;
  each lowers it as far as its target needs (Roslyn: not at all; CIL: to branches, privately).

### 1.2 What a bound node MUST NEVER contain

- **C# fragment strings** — no `Read()`/`Write(rhs)` returning C# (`Place.cs:22-25`), no `"{Path} = {rhs};"`
  (`Place.cs:45`), no C# statement blocks (`RenamesPlace.Write` `Place.cs:124-138`).
- **.NET / runtime type names as text** — no `"CobolString.SpliceInto(...)"` (`Place.cs:101`),
  `"CobolNum.FormatDisplay(...)"` (`Place.cs:160`), `"{TablePath}.Capacity"` (`Place.cs:185`). Runtime members are
  referenced through the `RuntimeMembers` catalogue (§2.3), never spelled in a node.
- **Pre-mangled C# identifiers** — no C# field/method names baked into a node: `BoundIndexRef.IndexField`
  (`BoundTree.cs:109`), `SetIndexTarget.IndexField` (`BoundTree.cs:471`), `BoundMethod.CsName` (`BoundTree.cs:32`),
  `BoundSetCapacity.TablePath` (`BoundTree.cs:493`), `BoundSearch.IndexField/DependCount/DynTable`
  (`BoundTree.cs:508-511`), `DynTablePlace.SendingPath/ReceivingPath` (`Place.cs:58`),
  `CapacityRegisterPlace.TablePath` (`Place.cs:176`), `RedefViewPlace.Backing` (`Place.cs:82`). An identifier is a
  **target-naming decision** owned by the shared `NameMangler` (§2.4), resolved from a symbol at render time.
- **Roslyn `Syntax*` nodes or `using` decisions** — the `using CobolNet.Runtime;` / `using CobolNet.Runtime.IO;`
  choices (`ProgramEmitter.cs`) are a C#-source concern; a CIL backend has no `using`s. These live in
  the Roslyn backend, keyed off which `RuntimeMembers` members were referenced.
- **Format literals of the target language** — no C# escape/`SymbolDisplay.FormatLiteral` output stored on a node
  (that is a Roslyn-render step over a decoded value).

### 1.3 The current leaks (grounded) and how each is removed

| # | Leak (file:line) | Why it breaks a second backend | Removal |
|---|---|---|---|
| L1 | **`Place.Read()` / `Place.Write(rhs)` abstract C#-string contract** — `Place.cs:22-25`; every subtype hard-codes runtime text: `MemberPlace` `Place.cs:42,45`, `RedefViewPlace` `Place.cs:94,97-101`, `NumericImagePlace` `Place.cs:160-164`, `RenamesPlace` (a full C# block) `Place.cs:120-138`, `RefModPlace` `Place.cs:215-230`. | The lvalue's public API *is* "emit C#". A CIL backend cannot lower a string. | **P7 Step 11**: `Place` becomes structural (`AccessPath`/`AccessSegment` + `BoundExpr` subscripts); a Roslyn-side `PlaceRenderer` and a CIL-side `CilPlaceLower` own rendering. `Read/Write` deleted from `Place`. |
| L2 | **`DynTablePlace(string SendingPath, string ReceivingPath, …)`** — `Place.cs:58,67,70`: TWO pre-rendered C# path strings encoding OCCURS DYNAMIC read/write polarity. | Two C# strings; polarity baked as text. | Replace with a `DynTableSegment(BoundExpr OneBased, AccessDir Dir)` in the `AccessPath`; polarity is a render-time choice from `AccessDir` + operation (P7 Step 11 already sketches `AccessDir`). |
| L3 | **Bound STATEMENT nodes carrying C# access-path strings** — `CapacityRegisterPlace.TablePath` `Place.cs:176`; `BoundSetCapacity.TablePath` `BoundTree.cs:493`; `BoundSearch.IndexField / DependCount / DynTable` `BoundTree.cs:508-511`. | The *bound tree itself* (not just `Place`) presupposes a C# path — the leak `DESIGN-binder-bound-tree.md` does not guard. | Carry the **table `Place`/symbol** (a `CapacityRegisterPlace` holding a `Place Table`, per `DESIGN-codegen-backend.md §2.3`) and an index **symbol**; the backend renders `.Capacity`/the field. `BoundSetCapacity(Place Table, …)`; `BoundSearch(DataItem Index, …, Place? DynTable)`. **(Addition to P6 — §5.)** |
| L4 | **C#-identifier fields on bound nodes** — `BoundIndexRef.IndexField` `BoundTree.cs:109`; `SetIndexTarget.IndexField` `BoundTree.cs:471`; `BoundMethod.CsName` `BoundTree.cs:32`. | A mangled C# name is a target-naming decision frozen into the neutral tree. | Store the **index `DataItem`** (SSOT §3.5: an index IS its 1-based occurrence field — the symbol suffices) and the **method `OoMethodSymbol`**; the shared `NameMangler` (§2.4) forms the identifier per backend. **(Addition to P6 — §5.)** |
| L5 | **The backend seam — IN PLACE.** `ICodeGenBackend` (`CodeGen/ICodeGenBackend.cs`) is the ONE seam; `RoslynBackend : ICodeGenBackend` consumes the immutable `BoundCompilation` and performs no binding; `BinderDriver` produces the tree; `CSharpEmitter` is now only the bind-host facade whose `EmitBound` renders C# from the bound tree. | (Resolved — a second backend now has a defined `ICodeGenBackend` to implement against `BoundCompilation`.) | **DONE**: binding is extracted into the Binder phase (`BinderDriver` → `BoundCompilation`); `ICodeGenBackend` is materialized so the driver hands `BoundCompilation` to a selected backend. |
| L6 | **The backend input carries the binder and the parse tree** — `BoundUnit.Ctx`/`.Data`/`.Refs` (`Binding/Model/BoundUnit.cs:34,48-49`), `BoundCompilation.Tree` and `.InterfaceData` (`Binding/Model/BoundCompilation.cs:34,38`), `EmitContext.Data` (`CodeGen/Emit/EmitCore.cs:19,34`), `UnitEmitters.Refs`; the OO symbols hold parse contexts too (`Oo/OoClassSymbol.cs:29`, `Oo/OoInterfaceSymbol.cs:25`, `Oo/OoMethodSymbol.cs:15`, so §1.1's `OoMethodSymbol` is not yet the pure resolved symbol it lists); emitters call `ReferenceResolver.ResolveItem` (the `Place` builder) 24 times at emit time. | A second backend cannot form a `Place` or read a data fact without driving the binder and the ANTLR tree; §1.1 lists neither. | `PlaceBuilder` to the model layer, run at bind time; an immutable `DataBindResult` in place of `DataBinder` on the backend input; parse-context reads become bound-node facts, and the OO symbols' contexts move to a binder-side map before the symbols move to the model (`DESIGN-architecture-review.md` §8.1 and §8.3 (1), (4); kb/Work PB2293, and the waves PB2375, PB2361, PB2398, PB2401). |

L1/L2 are already scheduled by `DESIGN-codegen-backend.md §2.3` + PHASE-07 Step 11. **L3/L4 are NOT** — they live in
`BoundTree.cs` records the bound-tree design treats as neutral but which still carry C# path/identifier strings. §5
adds them to PHASE-06 explicitly. L5 (the P6→P7 seam extraction) is **DONE** — `ICodeGenBackend` is in place and a `BoundCompilation` crosses the backend boundary. L6 is OPEN: that `BoundCompilation` still carries the binder and the parse tree, and the architecture review's edges file (DESIGN-architecture-review §8.1) ratchets it to zero.

---

## 2. `ICodeGenBackend` — the seam (concrete)

### 2.1 The interface (as `DESIGN-codegen-backend.md §2.2`, restated for reference)

```csharp
namespace CobolNet.CodeGen;

/// The ONE seam between the backend-neutral BoundCompilation and a target. NEVER binds.
public interface ICodeGenBackend
{
    BackendId Id { get; }                                   // Roslyn | Cil
    BackendArtifact Emit(BoundCompilation program, BackendOptions options);
}

public enum BackendId { Roslyn, Cil }

public sealed record BackendOptions(
    string OutputPath, string AssemblyName, EditionInfo Edition,
    bool EmitPdb = true, bool WriteSource = true /* .g.cs — Roslyn only */);

public sealed record BackendArtifact(
    bool Success,
    IReadOnlyList<Diagnostic> Diagnostics,     // ONE structured Diagnostic type (driver/editions dim)
    string? GeneratedSourcePath,               // .g.cs for Roslyn; null for Cil
    string? AssemblyPath);

public static class BackendFactory
{
    // Roslyn is in-assembly; Cil is resolved from the plugged Cobol.Net.Backend.Cil (§3.2) when present.
    public static ICodeGenBackend For(BackendId id, ICodeGenBackend? cil = null) => id switch
    {
        BackendId.Roslyn => new RoslynBackend(),
        BackendId.Cil    => cil ?? throw new NotSupportedException("--backend cil: Cobol.Net.Backend.Cil not loaded"),
        _ => throw new NotSupportedException(),
    };
}
```

- **Consumes:** the immutable `BoundCompilation` (P6 result) — units, the OO class model, the `SymbolTable`, and
  structural `Place`s — plus `BackendOptions`. **Performs no binding** (binding is the Binder phase's job —
  `BinderDriver` produces the `BoundCompilation`, per `DESIGN-binder-bound-tree.md §3.1`).
- **Produces:** a `BackendArtifact` (success + diagnostics + optional `.g.cs` path + assembly path). Roslyn writes a
  `.dll` + `.runtimeconfig.json` (via `AssemblyPackager`); Cil writes a `.dll` + `.runtimeconfig.json`
  + `.pdb` via Cecil.

### 2.2 Shared visitor vs per-backend visitor

**Decision: the visitor *interface* is shared; the *implementation* is per-backend.** The exhaustive
`IBoundStatementVisitor<T>` / `IBoundExprVisitor<T>` / `IBoundConditionVisitor<T>` / operand + bool visitors are
source-generated from `[BoundNode]`. Those interfaces + the generated `Accept<T>` + the sealed hierarchy are the
**shared model walk**. Each backend implements them once:

| Concern | Roslyn (P7) | Cil (PHASE-16) |
|---|---|---|
| statement dispatch | `StatementEmitter : IBoundStatementVisitor<bool>` | `CilStatementEmitter : IBoundStatementVisitor<CilFlow>` |
| numeric expr | `ExpressionRenderer : IBoundExprVisitor<NumX>` (C# text) | `CilExpressionEmitter : IBoundExprVisitor<CilVal>` (IL stack) |
| condition | `ConditionRenderer : IBoundConditionVisitor<string>` | `CilConditionEmitter : IBoundConditionVisitor<CilBranch>` |
| lvalue | `PlaceRenderer` (structure → C# read/write) | `CilPlaceLower` (structure → ldfld/stfld/ldelema…) |
| control flow | preserves structure (`if`/`while`/`switch`) | private structure→branch lowering (SSOT §1.1: **NOT** a shared phase) |

The **exhaustiveness guarantee is inherited for free**: a new `BoundStatement` leaf is a compile error in *both*
`StatementEmitter` and `CilStatementEmitter` — the source generator forces every backend's visitor to add an arm.

### 2.3 One runtime, one ABI catalogue — how both backends call `Cobol.Net.Runtime`

`DESIGN-codegen-backend.md §3`'s `RuntimeApi` returns **C# fragment strings** — correct for Roslyn, useless for Cecil
(which needs `MethodReference`s). To keep the singular-pattern rule (`feedback_one_mechanism_per_job`) across two backends,
split it into a **neutral catalogue** + two thin renderers:

> **Amended 2026-10-07 (DESIGN-architecture-review §8.3 (9), R1 Draft 3):** the catalogue is named `RuntimeMembers`, not
> `RuntimeAbi`. The owner-approved `DESIGN-external-repository.md` (2026-10-05, the later decision) has the RUNTIME
> declare `RuntimeAbi` (its `Version` and `CallAbi`, read by `ProgramTable.RegisterModule`), and two types of one name
> would meet in every emitter that has `using CobolNet.Runtime` (CS0104). Nothing else in this section changes.

```csharp
// SHARED (Cobol.Net.Compiler) — the ONE typed description of the runtime ABI, nameof-anchored.
public static class RuntimeMembers
{
    // Each member is a typed descriptor: declaring type + method name + arity, anchored to the real symbol so a
    // runtime rename is a COMPILE error here (the single codegen↔runtime contract, for BOTH backends).
    public static readonly RuntimeMember NumStore        = M(typeof(CobolNum),    nameof(CobolNum.Store));
    public static readonly RuntimeMember NumFormatDisplay= M(typeof(CobolNum),    nameof(CobolNum.FormatDisplay));
    public static readonly RuntimeMember StrRefMod       = M(typeof(CobolString), nameof(CobolString.RefMod));
    public static readonly RuntimeMember StrSpliceInto   = M(typeof(CobolString), nameof(CobolString.SpliceInto));
    // … one per emitted runtime member (~60) …
}
public sealed record RuntimeMember(Type Declaring, string Name /* + arity/overload key */);
```

```csharp
// PER-BACKEND renderers over the ONE catalogue:
internal sealed class RoslynRuntimeApi(EmitContext ctx)   // → C# fragment strings
{ public string NumStore(string expr, string profile) => Call(RuntimeMembers.NumStore, expr, profile); /* $"CobolNum.Store(…)" */ }

internal sealed class CilRuntimeApi(ModuleDefinition mod) // → Cecil MethodReference (resolved once, cached)
{ public MethodReference NumStore => Import(RuntimeMembers.NumStore); }
```

Both derive their target-specific form from the SAME `RuntimeMembers` descriptor, so a runtime member rename breaks the
catalogue's `nameof` at compile time and both renderers with it. This **reconciles** `DESIGN-codegen-backend.md §3`:
the Roslyn `RuntimeApi` stays (renamed `RoslynRuntimeApi`), now sourced from `RuntimeMembers`; the CIL backend gets its
own resolver for free.

### 2.4 The shared `NameMangler`

The COBOL-name → target-identifier mapping (today scattered as pre-baked strings in nodes — L4 — and as ad-hoc
`CsName`/`__` conventions in the emitter) becomes **one shared service** owned by P5/P6
(`Model/NameMangler.cs`, per topology §2.10's `NamingConvention` for OO; generalized to all identifiers). Both
backends call it: Roslyn to spell a C# field/method name; Cil to name a `FieldDefinition`/`MethodDefinition`. It is
**deterministic** (same COBOL name → same identifier) so the equivalence harness (PHASE-16) can even cross-check
emitted metadata names if desired. No mangled name is ever stored on a bound node — the node holds the **symbol**;
the mangler is applied at render time.

---

## 3. The two concrete backends

### 3.1 `RoslynBackend` (default, primary) — reconciled with `DESIGN-codegen-backend.md`

Unchanged from `DESIGN-codegen-backend.md`: **string emit stays; SyntaxFactory rejected** (§2.1 there) — readable
`.g.cs` is owner-locked (SSOT §1.2 #4). `RoslynBackend : ICodeGenBackend` drives `ProgramEmitter` per unit over an
immutable `EmitContext`, renders via `PlaceRenderer`/`ExpressionRenderer`/`ConditionRenderer`/`RoslynRuntimeApi` to a
`CodeWriter` text sink, compiles with `CSharpCompilation` (cached framework refs), and
hands packaging to `AssemblyPackager`. It is the **only** owner of C# syntax knowledge. This document changes nothing
about it except: its runtime-call rendering routes through `RoslynRuntimeApi` **over `RuntimeMembers`** (§2.3).

### 3.2 `CilBackend` (future-additive) — assembly home

`CilBackend : ICodeGenBackend` lives in a **NEW assembly `Cobol.Net.Backend.Cil`** that references
`Cobol.Net.Compiler` (for `BoundCompilation` + `RuntimeMembers` + the visitor interfaces), `Cobol.Net.Runtime`
(metadata, to import `MethodReference`s), and the `Mono.Cecil` NuGet. **Rationale for a separate assembly** (answers
topology Open Q4): the whole point of the CIL backend is "*no Roslyn dependency*"; symmetrically, Roslyn-only callers
should not carry the `Mono.Cecil` dependency. Isolating Cecil in a leaf backend assembly the CLI plugs into
`BackendFactory` keeps the default path Cecil-free and the core compiler backend-agnostic. It does its **own private**
structure→branch lowering (SSOT §1.1) — no shared lowered IR.

### 3.3 Mono.Cecil vs System.Reflection.Emit — recommendation

**Recommendation: Mono.Cecil** for the production `CilBackend`; **System.Reflection.Emit** for the throwaway
seam-proof only (§ PHASE-16 Milestone 0).

| Axis | Mono.Cecil — **CHOSEN** | System.Reflection.Emit |
|---|---|---|
| Persisted `.dll` on disk (must match Roslyn's `AssemblyPath` output) | ✅ native — writes a full PE to a path | ⚠ only since .NET 9 `PersistedAssemblyBuilder`; pre-9 was in-memory only |
| **Debug symbols (PDB)** — the decisive factor | ✅ full portable-PDB / sequence-point control | ✗ `PersistedAssemblyBuilder` has essentially no usable sequence-point/PDB story on .NET 9/10 |
| Explicit metadata control (cross-assembly refs for EXTERNAL/GLOBAL FDs + cross-CALL; OO type hierarchies) | ✅ inspectable, hand-authored metadata | ⚠ more constrained; newer persisted API, less battle-tested |
| Startup / AOT for the *emitted* program | ✅ plain assembly, no Roslyn | ✅ plain assembly |
| Licensing | ✅ MIT | ✅ in-box (.NET Foundation) |
| Maturity for a "decades-sustainable, commercial-quality" compiler (SSOT north star) | ✅ industry standard for IL authoring (Unity, ILSpy-adjacent, obfuscators — the owner's own Demeanor domain) | ⚠ persisted-builder path is new as of .NET 9 |
| Dependency cost | one external NuGet, **isolated** to `Cobol.Net.Backend.Cil` | none (in-box) |

The single external NuGet is an acceptable, isolated cost; the **PDB gap** is the decisive disqualifier for
Reflection.Emit as the production backend (a compiler must be able to emit debuggable assemblies). The SSOT already
names Cecil (§1.1). The seam-proof deliberately uses **in-box Reflection.Emit** precisely so the seam is proven real
**before** the Cecil dependency is added — a cheap, dependency-free proof that `ICodeGenBackend` and the neutral tree
are genuine.

### 3.4 Shared vs backend-specific — the summary

- **Shared** (`Cobol.Net.Compiler`, consumed by both): the frontend, the binder, `BoundCompilation` + the sealed
  bound hierarchy, the structural `Place`/`AccessSegment`, the source-generated `IBound*Visitor<T>` interfaces + the
  exhaustive `Accept<T>`, the `RuntimeMembers` catalogue, the `NameMangler`, the ONE `Cobol.Net.Runtime`.
- **Backend-specific**: the visitor *implementations* (text vs IL), `PlaceRenderer` vs `CilPlaceLower`, the
  runtime-call renderer (`RoslynRuntimeApi` vs `CilRuntimeApi`), the control-flow lowering (Roslyn preserves;
  Cil branches privately), the assembly writer (`CSharpCompilation` + `AssemblyPackager` vs Cecil `ModuleDefinition`
  + PDB writer), and `.g.cs` (Roslyn only).

---

## 4. Module changes (current → target)

Consistent with `DESIGN-module-topology.md §2.1/§2.3` (which already homes `Backend/RoslynCompiler.cs` +
`AssemblyPackager.cs`, and defers the second-backend assembly). Adds only what the dual-backend goal needs beyond
what P7 already lists.

| Action | From → To | Why |
|---|---|---|
| create | `CodeGen/ICodeGenBackend.cs` (`ICodeGenBackend`, `BackendId`, `BackendOptions`, `BackendArtifact`, `BackendFactory`) | The seam (also in P7 Step 1 / `DESIGN-codegen-backend.md §2.2`). |
| refactor | `RoslynBackend` → `RoslynBackend : ICodeGenBackend` consuming `BoundCompilation` | The default backend behind the seam; owns all C# syntax. |
| create | `Model/RuntimeMembers.cs` (neutral catalogue) + `CodeGen/Roslyn/RoslynRuntimeApi.cs` (was `RuntimeApi`) | ONE runtime ABI for BOTH backends (§2.3); generalizes `DESIGN-codegen-backend.md §3`. |
| create | `Model/NameMangler.cs` (COBOL-name → target identifier) | ONE naming service; removes L4 baked identifiers (§2.4). |
| refactor | `Binding/Model/Place.cs` `Read()/Write()` strings → structural `Place` + `AccessSegment` | L1/L2 removal (also P7 Step 11 / `DESIGN-codegen-backend.md §2.3`). |
| refactor | `BoundTree.cs` C#-string node fields (`BoundSetCapacity.TablePath` :493; `BoundSearch.IndexField/DependCount/DynTable` :508-511; `BoundIndexRef.IndexField` :109; `SetIndexTarget.IndexField` :471; `BoundMethod.CsName` :32; `CapacityRegisterPlace.TablePath` `Place.cs:176`) → symbol/`Place` references | **L3/L4 removal — NEW; not in P7.** (§5, addition to P6.) |
| create | `CodeGen/BackendContractTest` fixture (reflection + `CodeGen/**` analyzer scan) | Enforce §1: no `string`-returning render member / no raw-C#-identifier field on any `Place`/`Bound*` (§6). Generalizes `DESIGN-codegen-backend.md §6 R5`. |
| create *(PHASE-16)* | assembly `Cobol.Net.Backend.Cil/` (`CilBackend`, `CilStatementEmitter`, `CilExpressionEmitter`, `CilConditionEmitter`, `CilPlaceLower`, `CilRuntimeApi`, `CilDispatcher`) | The second backend, Cecil, isolated (§3.2). Answers topology Open Q4. |
| create *(PHASE-16 M0)* | `CodeGen/NullBackend.cs` + `CodeGen/DisplayBackend.cs` (in-box `System.Reflection.Emit`) | Dependency-free seam-proof (§3.3 / PHASE-16 M0). |
| edit | `Cli/CliOptions.cs` + `CompilerDriver.Options` (`CompilerDriver.cs:34`) — add `BackendId Backend = Roslyn`; `Cli/Program.cs` add `--backend {roslyn|cil}` | CLI selection (default Roslyn). |
| create *(PHASE-16)* | `tests/Cobol.Net.Tests.BackendEquivalence/` | Byte-compare Roslyn vs Cil stdout on the corpus (§ PHASE-16). |

---

## 5. Required additions to existing phases (the concrete guarantees)

These are the **exact** additions so the IR is backend-neutral and the seam is real. Each is a one-line guarantee to
splice into the named phase file (fuller bullets follow).

- **PHASE-05 (`PHASE-05-unified-data-model-storageform.md`) — one-line addition:**
  > *Declare `Place`/`AccessSegment`/`StorageForm` in `Model/` as **backend-neutral value types carrying zero target
  > text** (segments hold a symbol + `BoundExpr` subscripts, never a C# field/path string), and introduce the shared
  > `Model/NameMangler` as the ONE place a COBOL name becomes a target identifier — so no `Place`/`DataItem` ever
  > stores a pre-mangled C# name.*

  Fuller: P5 owns the structural `Place` shape and the `StorageForm` discriminator; it must specify them as neutral
  (symbol references + `BoundExpr`, per §1.1) and add `NameMangler` (§2.4). This makes L1/L2/L4 removable in P7
  without inventing the naming service there.

- **PHASE-06 (`PHASE-06-binder-pipeline-symbol-table-bindphase.md`) — one-line addition:**
  > *Replace the C#-identifier/path string fields on bound nodes (`BoundIndexRef.IndexField`,
  > `SetIndexTarget.IndexField`, `BoundMethod.CsName`, `BoundSetCapacity.TablePath`,
  > `BoundSearch.IndexField/DependCount/DynTable`, `CapacityRegisterPlace.TablePath`) with **symbol/`Place`
  > references** resolved through the `SymbolTable`/`NameMangler`, and add a `Produces` capability `BackendNeutral`
  > that the pipeline-completion gate asserts (the backend-contract precondition).*

  Fuller: P6 already builds `BoundCompilation` and the `SymbolTable`; it must additionally **de-C#** the L3/L4 node
  fields (they are produced by the binder — `BoundSearch`/`BoundSetCapacity` are built in
  the SEARCH/SET binders `Binding/Procedure/Verbs/SearchBinder.cs` / `SetBinder.cs`) so the bound tree that crosses the seam is neutral.
  The `BackendNeutral` capability makes "the tree is neutral" a declared, asserted pipeline fact (§3.1 there).

- **PHASE-07 (`PHASE-07-visitor-dispatch-emitter-decomposition.md`) — one-line addition:**
  > *Add **Step 13** — land the executable **backend-contract test** (§6) AND a **seam-proof second backend** (a
  > `NullBackend` + a tiny in-box `System.Reflection.Emit` `DisplayBackend`) behind `ICodeGenBackend`, run right
  > after Step 11's structural `Place`, so neutrality is **proven by a second consumer**, not merely asserted —
  > before phases 08–15 can silently re-introduce C# into a node.*

  Fuller: P7 Step 11 already makes `Place` structural and adds the neutrality *reflection* test (§6 R5 there). This
  addition **strengthens** it: (a) the contract test also scans `Bound*` node fields for raw C# identifiers (L3/L4),
  not just `Place` render methods; (b) a real second `ICodeGenBackend` implementation compiles against
  `BoundCompilation` at the end of P7, catching any residual C#-in-node by *construction* (it simply won't compile /
  won't have a string to lower). This is the "seam never silently rots" guarantee.

---

## 6. The backend-contract test (the enforcement)

One test fixture in the Characterization/Unit project, three assertions — all fail the build if violated:

1. **No `string`-returning render member on the neutral tree.** Reflect over every `Place` subtype and every
   `Bound*` record; assert none declares a public/internal `string Read(...)`, `string Write(...)`, or any member
   whose name matches `^(Read|Write|Render|Emit|AsCs|ToCsharp)` returning `string`. (Generalizes
   `DESIGN-codegen-backend.md §6 R5`.)
2. **No raw-C#-identifier field on a bound node.** Assert no `Place`/`Bound*` record has a `string` field whose
   name matches `(CsName|Path|Field|IndexField|TablePath|Backing|SendingPath|ReceivingPath)` — the L2/L3/L4 shapes.
   (A curated allow-list covers the *legitimately-neutral* strings: `BoundStringLiteral.Value`,
   `*.Text` source literals, `EcStatementInfo.Location`, `SectionName`/`CobolName`.)
3. **The seam has ≥2 real consumers.** Assert `ICodeGenBackend` has at least the `RoslynBackend` and the seam-proof
   backend implementations, and that a fixed tiny `BoundCompilation` (a DISPLAY-only program) round-trips through
   **both** producing byte-identical stdout. This is the executable proof that the tree carries no C#: a
   non-C# backend can consume it.

The test is **incremental-friendly**: assertions 1–2 land at P7 Step 11 (an allow-list shrinks to the curated set as
subtypes convert); assertion 3 lands at P7 Step 13 (the seam-proof). In CI it is a hard gate — a new node with a C#
string field fails the build, exactly the "missing-arm-is-a-compile-error" culture extended to neutrality.

---

## 7. Risks & open questions for the owner

**Risks**
1. **Node-field de-C#-ing (L3/L4) touches binder verb files (MEDIUM).** `BoundSearch`/`BoundSetCapacity` are built in
   the OCCURS DYNAMIC / SORT / SEARCH binders; swapping their string fields for symbol/`Place` refs is a P6 change
   with a P7 render follow-through. Mitigation: prove-then-delete (compute the symbol form alongside, assert the
   rendered path is byte-identical corpus-wide, then delete the string field), exactly as P7 Step 8 does for
   `StoreAsImage`.
2. **`RuntimeMembers` overload identity (MEDIUM).** ~60 members, several overloaded (`FormatDisplay`/`StoreDisplay`
   families). A descriptor must key on the overload the backend needs. Mitigation: `RuntimeMember` carries an arity
   / parameter-shape key; `nameof`-anchor the type + method; the Roslyn compile-step still catches a wrong overload
   in the generated C#, and the CIL resolver throws loudly on an ambiguous import.
3. **Neutrality bit-rot if PHASE-16 is deferred (the original problem).** Mitigation: the §6 contract test +
   assertion 3's seam-proof backend keep neutrality enforced *even with no full CIL backend built* — the whole point
   of pulling the seam-proof forward into P7 Step 13.
4. **CIL control-flow lowering parity (HIGH, PHASE-16 only).** The PC dispatcher (`while(true)switch`), inline
   PERFORM loops, EVALUATE chains, and EC try/finally must lower to correct branches privately. Mitigation: the
   backend-equivalence harness byte-compares stdout on a growing corpus subset; CIL is additive and never default.

**Open questions**
1. **CIL backend scope — full parity or a subset first?** Recommendation: **subset-first, additive.** PHASE-16
   Milestone 0 (seam-proof, DISPLAY-only) then grows the equivalence corpus feature-by-feature (numerics → moves →
   control flow → files → OO → EC); full parity is the exit criterion, not the entry cost. Roslyn stays default
   throughout, so a partial CIL backend never blocks a release.
2. **Debug-symbol strategy for the CIL backend.** Recommendation: **portable PDB via Cecil**, sequence points mapped
   to the COBOL source lines (the frontend already carries line info the binder can thread onto bound nodes as an
   optional neutral `SourceSpan`). Confirm whether COBOL-source-level debugging of the emitted assembly is a v1 goal
   or a follow-on (it does not affect the seam or neutrality — only the CIL backend's PDB writer).
3. **Separate `Cobol.Net.Backend.Cil` assembly vs a `CodeGen/Cil/` folder in Compiler.** Recommendation: **separate
   assembly** (isolates the Cecil dependency from the default Roslyn path; answers topology Open Q4 affirmatively).
   Acceptable alternative if the extra project is unwanted: a `CodeGen/Cil/` folder with Cecil referenced by the
   whole Compiler assembly — simpler, but every Roslyn-only caller then carries Cecil.
4. **Does the neutral `SourceSpan` (for CIL PDBs + EC location) belong on every bound node now, or added with
   PHASE-16?** Recommendation: add it opportunistically in P6 as an optional neutral field (it is data, not C#), so
   the CIL backend has it when needed and the Roslyn backend can ignore it.
