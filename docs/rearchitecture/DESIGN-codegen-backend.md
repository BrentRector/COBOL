# DESIGN — Target codegen & backend abstraction

**Dimension:** Target emit architecture — string-emit vs Roslyn SyntaxFactory; the `ICodeGenBackend` seam for a
future Cecil/CIL backend; killing the binder↔emitter per-verb duplication; renderer decomposition; the `CodeWriter`;
how `CSharpEmitter` stops being a god class.

**Status:** DESIGN (rearchitecture target) — REALIZED through PHASE-07 (Steps 1–12). IMPLEMENTED in the tree: the
`ICodeGenBackend`/`RoslynBackend` seam, the source-generated exhaustive bound-tree visitor, the immutable
`EmitContext` + `ReceiverContext` parameter (save/restore re-entrant public render entries), the typed `RuntimeApi`
façade, cached framework references, the `AssemblyPackager` split, the dissolved emitter god class (`CSharpEmitter`
is a thin bind-host facade until P9), the backend-neutral **structural `Place`** (§2.3 — rendered by
`CodeGen/Roslyn/PlaceRenderer`; `PlaceNeutralityTests` = the §6 R5 net), and the SINGLE-channel `IntrinsicRenderer`
(§2.5 — the static evaluator is deleted; FUNCTION args parse as real expressions and render through the ONE
expression renderer). OUTSTANDING: the `Emit/`→`Roslyn/` renderer consolidation + the `NumericRenderer`→
`ExpressionRenderer` rename (ride P9), and the future **`CilBackend`**.

**Upholds the four owner-locked invariants** (COBOLNET_DESIGN §1.2): typed-native data only; native numerics;
single PC dispatcher; idiomatic/readable C# where the construct allows. **Upholds the dual-backend goal**
(`project_dual_backend_goal`, SSOT §1.1 / §18 #23): a **backend-neutral bound tree** feeds a **selectable**
`--backend roslyn|cil`; every semantic decision lives in the binder/bound nodes; the backend only *renders*.

---

## 0. TL;DR — the decisions

1. **Keep string emit; reject SyntaxFactory.** Readable `.g.cs` is an owner-locked invariant (§1.2 #4, §1.4).
   `CodeWriter`-driven text is proven byte-exact across the whole battery and reads like hand-written C#. SyntaxFactory
   is 5–10× more verbose, produces normalized-but-ugly trees, is far harder to review, and buys type-safety we get for
   free from the *Roslyn compile step that already runs*. We close the one real string-emit smell (untyped runtime
   coupling) with a **typed `RuntimeApi` façade** instead — see §3.
2. **Make the seam real: `ICodeGenBackend`.** `CSharpEmitter`+`RoslynBackend` become `RoslynBackend : ICodeGenBackend`;
   a future `CilBackend : ICodeGenBackend` (Mono.Cecil) is additive. The enabler is #3.
3. **Backend-neutralize the bound tree and `Place`.** Today `Place.Read()/Write()` and several bound nodes carry
   **pre-rendered C# strings** — a direct violation of the G4 invariant the design doc already states (§3.3 note). The
   target: `Place` becomes a **structural** description (accessor chain + subscript expression nodes + ref-mod span);
   the Roslyn backend owns a `PlaceRenderer` that turns structure→C#; the CIL backend lowers the *same* structure to
   load/store. No C# fragment survives in a bound node.
4. **One exhaustive dispatch, source-generated.** Replace the two hand-maintained god-switches (`BindStatementCore`
   ~55 arms; `EmitStatement` 79 arms) and the four renderer switches with a **generated visitor** over the sealed
   bound hierarchy. A missing arm becomes a **compile error**, not a runtime `LoudStmt`. Each backend implements the
   visitor **once**.
5. **Decompose the emitter into real collaborators over an immutable `EmitContext`.** `CSharpEmitter` (15 partials,
   ~one class) → a thin `RoslynBackend` orchestrator + `DataEmitter`, `DispatchEmitter`, `StatementEmitter` (+per-verb
   collaborators), `ExpressionRenderer`, `ConditionRenderer`, `PlaceRenderer`, `RuntimeApi`. Kill the mutable
   `EmissionContext.Target*` state (the H1 hazard) via a scoped `ReceiverContext` passed as a parameter.
6. **`CodeWriter` stays** as the Roslyn backend's text sink (the CIL backend uses Cecil's `ILProcessor` instead), with
   minor hardening.

---

## 1. Current problems (grounded in the survey + critique + code)

### 1.1 The seam is materialized
`docs/COBOLNET_DESIGN.md` §1.1 and §18 #23 promise `ICodeGenBackend` with `--backend roslyn|cil`. The interface is
materialized (`CodeGen/ICodeGenBackend.cs`); `RoslynBackend : ICodeGenBackend` is the default backend and the only
owner of C# syntax knowledge, selected via `BackendFactory.For`. The compile path binds the whole compilation group
to an immutable `BoundCompilation` (`BinderDriver.Bind`) and then renders it (`RoslynBackend.Emit` → `ProgramEmitter`).
The string-carrying `Place` (§1.2) that stood between this and a fully backend-neutral boundary is RESOLVED —
`Place` is structural (P7 Step 11) and the last parallel evaluator is gone (P7 Step 12).

### 1.2 Bound tree and `Place` carried C# text (RESOLVED at P7 Step 11)
The G4 invariant (SSOT §3.3): *"bound nodes carry no pre-rendered C#-specific fragments; the CIL backend lowers the
same structure."* The pre-Step-11 reality this section recorded (now historical — every item below is fixed;
`PlaceNeutralityTests` enforces the invariant):
- `Place.Read()/Write(rhs)` return **C# strings** (`Place.cs:22-25`). Every subtype hard-codes runtime call text:
  `MemberPlace` → `"{Path} = {rhs};"`; `RedefViewPlace` → `"CobolString.SpliceInto(...)"` (`Place.cs:94-101`);
  `NumericImagePlace` → `"CobolNum.FormatDisplay(...)"` (`Place.cs:160-164`); `RefModPlace`, `RenamesPlace`,
  `DynTablePlace`, `CapacityRegisterPlace` similarly.
- The per-verb binders assemble runtime C# strings directly (`Initialize.cs`, `TryExpandAll`, `CobolTable.At(...)`),
  per the StatementBinder-verb-partials survey ("bind-time code building emit-time C#, blurring the §2 boundary").

A CIL backend cannot consume any of this — it re-imposes C# syntax on a supposedly neutral tree. **This is the single
largest structural blocker for the dimension** and the root of the "smart-emitter/leaky-binder" duplication.

### 1.3 Dispatch — source-generated and exhaustive
The emit and analysis walks over the sealed bound hierarchy are a **source-generated exhaustive visitor**
(`Cobol.Net.Compiler.SourceGen` emits the `IBound*Visitor` interfaces + `Accept`): `StatementEmitter :
IBoundStatementVisitor<bool>`, `NumericRenderer : IBoundExprVisitor<NumX>`/`IBoundOperandVisitor<NumX>`, and
`ConditionRenderer` each implement their visitor once. **Adding a bound leaf without a visitor arm is a compile
error**, not a runtime `LoudStmt` fall-through. The binder's statement dispatch works over *parse* contexts (not the
bound tree), so it stays a switch — but it is no longer kept in lockstep with an emit god-switch, which was the
actual hazard.

### 1.4 The emitter god class is dissolved
`CSharpEmitter` no longer generates code: emission lives on real collaborators — `ProgramEmitter` (run-unit
orchestration) → `UnitEmitters` (the per-unit composition root) → `DispatchEmitter`/`StatementEmitter` + the
`Verbs/*Emitter` set + `EcEmitter` + the `DataDivision/` emitters — over an immutable `EmitContext`. Binding is a
real phase boundary: `BinderDriver.Bind` runs the bind passes (collect units, bind interface/class/program data,
validate overrides, bind bodies, build the UDF table, bind procedures, compute the EC gate, qualify file connectors)
and returns an immutable `BoundCompilation`; the codegen layer performs no binding. `CSharpEmitter` survives only as a
thin **bind-host facade** (`Bind`/`EmitBound` + the `IOoBindHost` seam and the OO bind bodies) until P9 relocates the
OO bind bodies into the binder; it emits nothing.

### 1.5 Per-emit receiver state — the H1 staleness hazard, closed
`EmitContext` (renamed from `EmissionContext`, `EmitCore.cs`) is immutable: the four mutable receiver fields
(`TargetScale`/`TargetReal`/`TargetRounding`/`InSizeErrorContext`) and their manual set/reset discipline are gone.
The receiver travels as a `ReceiverContext` value (§2.5) — required on every public render entry and read from one
private renderer field the visitor recursion assigns at entry — so a render can never run under a stale receiver: the
H1 staleness class is closed by construction rather than by discipline.

### 1.6 Renderer smells (from the Emit-renderers survey)
- **Untyped runtime coupling:** ~60 runtime members (`CobolNum.*`, `CobolString.*`, `CobolDec.*`, `CobolFloat.*`,
  `CobolBool.*`, `EcFunctions.*`, `ManagedPointer.*`, …) named **by string**. A runtime rename is invisible until the
  *generated* C# fails to Roslyn-compile at run time.
- **Parallel numeric evaluators (×3) — DELETED at P7 Step 12:** `IntrinsicRenderer`'s static string channel
  (`NumStatic/StaticAdditive/StaticMul`) re-implemented a division/float-*incapable* subset of `NumericRenderer`;
  the hand-rolled intrinsic-arg parser in the binder was its twin. Both are gone: FUNCTION arguments parse as real
  `arithmeticExpression`s and render through the ONE renderer under `ReceiverContext.None`.
- **Figurative-fill quadruplicated** with divergent return types (`EmitText.FigurativeFill`,
  `FieldEmitter.FillCharFor`, `ConditionRenderer.FigurativeFillChar`, `EmissionContext.FigFill`) — a real HIGH/LOW-VALUE
  divergence risk.
- **`FieldEmitter` is miscategorized:** a ~484-LOC DATA-DIVISION emitter parked in `CodeGen/Emit/` and re-`new`'d ad
  hoc (`Call.cs:563,573`; `Oo.cs:331,647`), bundling ≥4 concerns.
- **Apostrophe-VALUE silent miscompile** (latent-bugs critique, HIGH): `EmitText.AllLiteralText` and
  `FieldEmitter.GroupValueText` hard-code the `"` delimiter while `DecodeCobolString` handles both — a singular-pattern
  violation that corrupts data with no diagnostic. The `CsLiteralCodec` consolidation below fixes it structurally.
- **Naming collision:** a second unrelated `EmissionContext` exists in the legacy tree
  (`legacy-compiler/CodeGen/Emission/EmissionContext.cs`).

### 1.7 `RoslynBackend` throughput + packaging
The framework `MetadataReference` set is built once and cached (`static readonly Lazy<ImmutableArray<MetadataReference>>`
in `RoslynBackend.cs`) instead of rebuilt per compile — the in-process test/batch cost that once dominated is gone.
Packaging (runtimeconfig + runtime-dll deploy) is split out of the compile path into `AssemblyPackager`, so the
`RoslynBackend.Compile` step is pure compilation with no packaging/deploy side effects — its only writes are the
emitted assembly itself and the failed-emit cleanup of that partial output.

**Deterministic PE (kb/Work PB120, §15.99.3 r3):** `RoslynBackend.Compile` sets `deterministic: true` — the COFF `TimeDateStamp` is a content hash and the MVID is content-derived, so the generated object code provides NO compilation date and time and §15.99.3 r3's “if provided in … the generated object code” condition is not engaged (there is no listing facility either; the only compilation time a produced program carries is WHEN-COMPILED's own baked constant). Identical source + references → a byte-identical assembly (the module name embeds the output FILE name, so “identical” includes the output name). The WHEN-COMPILED stamp itself is COMPILATION-scoped, not process-scoped: `ProgramEmitter` captures it once per `EmitBound` through the injectable `IntrinsicBinder.CompileClock` seam and threads it via `EmitContext.WhenCompiledStamp`, so a container and its contained units bake the SAME constant (§15.99.3 r2) while each successive compilation in a long-lived process gets its own.

---

## 2. Target design

### 2.0 Layer map (target)

```
Binding (backend-neutral, NO C# text)
  BoundCompilation ──────── the immutable SSOT both backends consume
    ├─ BoundProgram/BoundClass/BoundInterface units
    ├─ BoundStatement/BoundExpr/BoundCondition/BoundOperand  (sealed, [BoundNode] → generated visitor)
    └─ Place (STRUCTURAL: accessor chain + BoundExpr subscripts + ref-mod span; no Read()/Write() strings)

CodeGen (backend-selectable)
  ICodeGenBackend  ── Emit(BoundCompilation, BackendOptions) → BackendArtifact
    ├─ RoslynBackend : ICodeGenBackend            (default; string-emit → Roslyn)
    │     EmitContext (immutable per-unit)  + CodeWriter (text sink) + RuntimeApi (typed fragment façade)
    │     ProgramEmitter → DataEmitter, DispatchEmitter, StatementEmitter(+per-verb), PlaceRenderer,
    │                      ExpressionRenderer, ConditionRenderer, IntrinsicRenderer, FigurativeConstants
    └─ CilBackend : ICodeGenBackend               (future-additive; Mono.Cecil → IL, private branch lowering)
  AssemblyPackager  ── runtimeconfig + runtime-dll deploy (split out of RoslynBackend)
```

### 2.1 Decision A — string emit stays; SyntaxFactory rejected (with the mitigation)

| Axis | String emit (`CodeWriter`) — CHOSEN | Roslyn `SyntaxFactory` — rejected |
|---|---|---|
| Readable `.g.cs` (owner invariant §1.2 #4/§1.4) | ✅ authored to read hand-written | ✅ only after `NormalizeWhitespace()`, still machine-shaped |
| Authoring cost | low (`w.Line($"…")`) | 5–10× verbose builder trees |
| Reviewability of emit code | high | low (builder soup) |
| Type-safety of generated code | from the Roslyn compile step already in the pipeline | from the builder API |
| Catches runtime-API drift at author time | ❌ (mitigated by `RuntimeApi`, §3) | partial |
| CIL backend reuse | n/a (CIL uses Cecil) | n/a |

**Conclusion:** keep the text channel. The *only* advantage SyntaxFactory has — compile-time validity of the emitted
code — is already delivered by the mandatory Roslyn `Compile` (a bind-success-⇒-emit-compiles ICE per §1.4). Adopt
SyntaxFactory **nowhere**. Close the untyped-runtime-coupling smell with `RuntimeApi` (§3).

### 2.2 Decision B — `ICodeGenBackend`

```csharp
namespace CobolNet.CodeGen;

/// The one seam between the backend-neutral BoundCompilation and a target.
public interface ICodeGenBackend
{
    BackendId Id { get; }                                   // Roslyn | Cil
    /// Render + build. NEVER binds — receives a fully-bound, desugared BoundCompilation.
    BackendArtifact Emit(BoundCompilation program, BackendOptions options);
}

public enum BackendId { Roslyn, Cil }

public sealed record BackendOptions(
    string OutputPath, string AssemblyName, EditionInfo Edition,
    bool EmitPdb = true, bool WriteSource = true /* .g.cs */);

/// The result the driver consumes; backend-agnostic.
public sealed record BackendArtifact(
    bool Success,
    IReadOnlyList<Diagnostic> Diagnostics,     // ONE structured Diagnostic type (driver dim)
    string? GeneratedSourcePath,               // .g.cs for Roslyn; null for Cil
    string? AssemblyPath);
```

- `RoslynBackend` implements it: `PlaceRenderer`+renderers → `CodeWriter` text → `CSharpCompilation` → assembly, then
  hands packaging to `AssemblyPackager`. It becomes the **only** owner of C# syntax knowledge.
- `CilBackend` (future) implements it over Mono.Cecil, doing its own private structure→branch lowering (SSOT §1.1:
  no shared lowered IR). Not built in this rearchitecture; the seam and the neutral tree make it *possible* and let the
  differential harness cross-check the two backends.
- The driver selects by `--backend` (default Roslyn): `ICodeGenBackend backend = BackendFactory.For(options.Backend);`

**Hard boundary:** the backend's `Emit` receives a `BoundCompilation` and produces an artifact. It performs **no
binding**. The bind orchestration lives in the binder phase (`BinderDriver.Bind` → `BoundCompilation`), owned by the
**binder/driver dimensions**; this dimension **consumes** that immutable tree (see Depends-on + Open Question 1).

### 2.3 Decision C — backend-neutral `Place` (structural)

**Invariant C-1 — THE ONE GROUP-IMAGE CHANNEL.** The character image of a group operand has exactly four arms,
and they are stated once, in `PlaceRenderer`: a Tier-B / BASED string-canonical view's `Read` ALREADY IS the
image (§13.18.44.4 GR1 — one storage area, so a second encoding would be a second storage); an `OdoGroupPlace`
is unwrapped first, and sliced to §13.18.38.4 GR8's CURRENT extent exactly when `UsesCurrentExtent(odo, dir)` says
so — the direction law written once (GR8a and GR8b agree on the SENDING direction; only a depending-INSIDE group as a
RECEIVING operand takes the maximum), asked by every channel (`AsImage` / `AsBits` / `AsNat`), by the reference-
modification READ (a sending reference, §8.4.3.3.4 GR5) and by the ref-mod receiver's splice BASE (`SpliceBase`, the
receiving direction — kb/Work PB202); an imageless OPERAND (`Place.ImageCapable` false — asked of the referenced
operand, never of the declaring entry, so a subscripted dynamic-capacity-table element is image-capable although its
table entry is not, kb/Work PB189) stages the Tier-C loud; otherwise it is the struct's generated `AsImage()`. `GroupImage` / `SendingGroupImage` /
`WriteGroupImage` / `WriteFullGroupImage` are those arms. **No consumer may spell `.AsImage()` / `.FromImage(`
for itself**, and each takes a `context` string so routing through the ONE reader costs no diagnostic quality.

⛔ Enforcement is a SOURCE-LEVEL drift test, `tests/Cobol.Net.Tests.Unit/BoundaryImageChannelTests.cs`, not a
doc-comment — because the law WAS already a doc-comment and four consumers spelled the call anyway, each missing
a different arm (kb/Work PB178 + PB177). Its allow-list is two entries with written justifications; an addition
without one is a review failure. Note the trap the cluster was built out of: `IsImageCapable` answers "does the
struct have a codec", NOT "is this Place already a string window" — a Tier-B group view is fully image-CAPABLE
and still CS1061'd, so adding another capability check instead of routing through the ONE reader fixes nothing.
See also DESIGN-data-model §2.3's Tier-B section: the window's "Read IS the image" fact is what this protects.

`Place` loses `Read()/Write()`. It becomes a pure structural value the backend renders.

```csharp
namespace CobolNet.Binding.Model;

public abstract record Place { public abstract DataItem Item { get; } public abstract PicInfo? Pic { get; } }

// Direct (possibly nested/subscripted) member access. Subscripts are BoundExpr, NOT strings.
public sealed record MemberPlace(AccessPath Path, DataItem MemberItem) : Place;

// AccessPath = an ordered chain of segments the backend renders:
public sealed record AccessPath(IReadOnlyList<AccessSegment> Segments);
public abstract record AccessSegment;
public sealed record RootFieldSegment(string CsField) : AccessSegment;           // a static/instance root field
public sealed record MemberSegment(string CsMember) : AccessSegment;             // .Foo
public sealed record IndexSegment(BoundExpr ZeroBased) : AccessSegment;          // [expr]  (1→0 already applied)
public sealed record FixedTableSegment(BoundExpr OneBased, AccessDir Dir) : AccessSegment; // CobolTable.At polarity
public sealed record DynTableSegment(BoundExpr OneBased, AccessDir Dir) : AccessSegment;   // RefSending/RefReceiving

// The wrapping subtypes carry STRUCTURE + resolved DataItems, never call text:
public sealed record RefModPlace(Place Inner, BoundExpr Start, BoundExpr? Length) : Place;
public sealed record RedefViewPlace(Place Backing, BoundExpr ZeroBasedOffset, int Width, DataItem ViewItem) : Place;
public sealed record NumericImagePlace(Place Inner) : Place;                      // format/parse image
public sealed record RenamesPlace(IReadOnlyList<Place> Leaves, DataItem AliasItem) : Place;
public sealed record CapacityRegisterPlace(Place Table, DataItem RegisterItem) : Place;  // read-only view
```

`AccessDir` (`Read`/`Write`) already exists for OCCURS DYNAMIC polarity — promote it to the shared segment model so
`DynTablePlace`'s two-string hack disappears; polarity is a render-time choice from the segment + the operation.

The Roslyn backend gets **`PlaceRenderer`**, the sole owner of the old `Read()/Write()` logic:

```csharp
internal sealed class PlaceRenderer(EmitContext ctx, RuntimeApi rt)
{
    public string Read(Place p);                 // rvalue C# expression
    public string Write(Place p, string rhs);    // C# store statement
    public string WriteFill(RefModPlace p, string seed);   // figurative seed repeated over the slice (§8.3.3.6.4 GR2)
}
```

Every runtime call it emits routes through `RuntimeApi` (§3). `CapacityRegisterPlace.Write` becoming an unrepresentable
operation (it is read-only) is expressed by `PlaceRenderer.Write` rejecting it as an ICE — the throwing `Write`
override on the record disappears.

**Migration is the risk** (see §5) — done subtype-by-subtype behind a shim so the battery stays green.

### 2.4 Decision D — generated exhaustive visitor (kills the god-switches)

Mark the sealed roots and use a small source generator (or `[GeneratedDispatch]`) to emit exhaustive `Accept`:

```csharp
[BoundNode] public abstract record BoundStatement;   // 70+ sealed leaves
public interface IBoundStatementVisitor<out T> { T Visit(BoundMove n); T Visit(BoundIf n); /* …every leaf… */ }
// generated: partial method  T BoundStatement.Accept<T>(IBoundStatementVisitor<T> v) => v switch { … exhaustive … };
```

- The generator emits one `Visit(TLeaf)` per sealed leaf; **adding a leaf without a visitor arm is a compile error**.
- Same treatment for `BoundExpr`, `BoundCondition`, `BoundOperand`.
- Give the five error families (`BoundUnsupported/BoundOperandError/BoundExprError/BoundConditionError/BoundBoolError`)
  a common `IBoundError` marker so the loud defaults collapse to one handled arm.
- Consumers become visitors implemented once per backend: `RoslynStatementEmitter : IBoundStatementVisitor<bool>`
  (bool = "unconditionally transfers control", preserving today's `EmitStatement` contract);
  `RoslynExpressionRenderer : IBoundExprVisitor<NumX>`; `RoslynConditionRenderer : IBoundConditionVisitor<string>`;
  `PlaceRenderer` walks `AccessSegment`. `BoundStores.StoreKindOf` becomes an analysis visitor.
- The **binder** side (`BindStatementCore`) is not a bound-tree walk — it dispatches over *parse* contexts, so it stays
  a switch, but the emit/analysis lockstep problem (the actual hazard) is gone. The CIL backend later adds
  `CilStatementEmitter : IBoundStatementVisitor<…>` and inherits the same exhaustiveness guarantee.

### 2.5 Decision E — decompose the emitter over an **immutable** `EmitContext`

Rename `EmissionContext` → **`EmitContext`** (ends the legacy name collision, SSOT §17) and make it immutable/read-only
config:

```csharp
internal sealed class EmitContext(CodeWriter writer, BoundUnit unit, EditionInfo edition, NameAllocator names)
{
    public CodeWriter Writer { get; }
    public BoundUnit Unit { get; }               // bound data model view (not the mutable DataBinder)
    public EditionInfo Edition { get; }
    public NameAllocator Names { get; }
    public string CollateArg { get; }            // derived once
    public string EditCfgArgs { get; }           // derived once
    // NO TargetScale/TargetReal/TargetRounding/InSizeErrorContext — see ReceiverContext
}
```

The four mutable receiver fields become a **value passed as a parameter** to the numeric renders:

```csharp
public readonly record struct ReceiverContext(int Scale, bool Real, CobolRounding Rounding, bool InSizeError);
NumX ExpressionRenderer.Render(BoundExpr e, in ReceiverContext rcv);   // no ambient state, no reset dance
```

*(Because the render dispatch is the source-generated single-arg `Visit`/`Accept<T>`, it cannot thread a parameter
through the recursion. So `ReceiverContext` is REQUIRED on every PUBLIC render entry (`Render`/`AsNum`/`Fold`/`Combine`
all take `in ReceiverContext rcv`) and carried in ONE private renderer field the internal `Visit` recursion reads —
freshly assigned at every entry, so no render can run under a stale receiver: the by-construction guarantee lives at
the API boundary. One render tree serves exactly one receiver, so field-vs-parameter is semantically identical inside
the recursion. The mutually-recursive `IntrinsicRenderer` reads the live receiver via `NumericRenderer.Receiver`.
`EmitArith` threads the size-error flag into its store closure as `Action<bool>` — `InSizeError` has TWO triggers,
the phrase and `>>TURN EC-SIZE` checking — and receiver-less sites pass `ReceiverContext.None`.)*

This closes the H1 staleness class by construction and made `IntrinsicRenderer`'s "static channel" unnecessary —
DONE at P7 Step 12: the string channel is INSTANCE, renders numeric arguments through the **one** expression
renderer under `ReceiverContext.None` (the public render entries save/restore, so the mid-render default-receiver
call is re-entrant), and `NumStatic/NumStaticExpr/StaticAdditive/StaticMul` are deleted.

**Collaborator structure** (real classes, not partials of one god class), matching SSOT §2:

| Class | Owns |
|---|---|
| `RoslynBackend` | `ICodeGenBackend` impl: drives `ProgramEmitter` per unit, packaging, `.g.cs` write |
| `ProgramEmitter` | one program/class/interface unit → its C# type + `__Dispatch` + entry wrapper |
| `DataEmitter` | DATA DIVISION facade in `CodeGen/DataDivision/`: record structs, `NumProfile` decls, fields, initializers, group image codec — over `RecordStructEmitter`/`GroupImageCodec`/`GroupValueSlicer`/`PhysicalModel`/`ValueInitializer` |
| `DispatchEmitter` | the PC dispatcher (`__Dispatch`, pc cases, ALTER fields) |
| `StatementEmitter` | the `IBoundStatementVisitor<bool>` core + shared helpers |
| per-verb emitters | `KeyedIoEmitter`, `SortEmitter`, `StringEmitter`, `InspectEmitter`, `ReportWriterEmitter`, `CallEmitter`, `OoEmitter`, … — real classes over `EmitContext`, invoked by `StatementEmitter` |
| `ExpressionRenderer` | `IBoundExprVisitor<NumX>` (was `NumericRenderer`) |
| `ConditionRenderer` | `IBoundConditionVisitor<string>` |
| `IntrinsicRenderer` | §15 FUNCTION dispatch — single channel |
| `PlaceRenderer` | `Place`+`AccessSegment` → C# read/write |
| `FigurativeConstants` | ONE figurative service (word/kind + PicCategory + collate → {runtime char, C# char literal, C# string literal}) — the string form is the SEED an ISO §8.3.3.6.4 GR2 sizing repeats; it carries no width, because a figurative's width is the associated operand's and belongs to `CobolString.FigToWidth`/`CompareFig` at runtime (kb/Work PB297). It is also the ONE OPERAND CLASSIFIER (`Classify` → `FigurativeOperand{Kind, AllLiteral}`, kb/Work PB461): §8.3.3.6.2's Formats 1–5, where the word `ALL` is OPTIONAL, versus Format 6 `ALL literal-1`, where it is required — in both the glued spelling `GetText()` produces and the spaced one. Every reader of a VALUE-clause / level-88 operand text asks it (the VALUE initializer, the group-VALUE slicer, the SET store, the condition test, the SR9 implied picture, the §13.18.63.3 class validator, and — since kb/Work PB933 — `InitializeBinder.InitializeValueOperand`, the §14.9.20.4 GR6a3 sender, whose private word map sent `NULL` to a pointer-class `'N'` where the shared one sends it to `'L'`); the five private ALL-strips that preceded it each recognized a different subset of the spellings, so the store and the test of ONE condition-name disagreed |
| `RuntimeApi` | typed façade over the runtime API (§3) |
| `NameAllocator` | C#-identifier generation (SSOT §2 — already planned) |

`OO orchestration` (`CallEmitRunUnit`'s 8-step OO sequence) leaves the codegen layer entirely — it is bind
orchestration and moves to the OO-pass/`BindPipeline` (OO dimension). `ProgramEmitter` just emits the already-bound
class/factory/interface units.

### 2.6 `CodeWriter` — keep, harden

`CodeWriter` stays as the Roslyn text sink (73 LOC, correct). Minor: add `w.Stmt(string)` (line + `;` discipline is
currently ad hoc), keep `Block`/`BlockScope`. The CIL backend never touches `CodeWriter` — it writes IL via Cecil.

---

## 3. `RuntimeApi` — the typed fragment façade (closes the untyped-coupling smell)

One reviewable contract over the ~60 runtime members codegen emits as text. Each method returns a **C# fragment
string** but is itself a typed C# method, so a runtime rename/signature change breaks **one file at compile time**
instead of silently at generated-compile time.

```csharp
internal sealed class RuntimeApi   // one instance per EmitContext
{
    // numeric
    public string NumStore(string expr, string profile) => $"CobolNum.Store({expr}, {profile})";
    public string NumFormatDisplay(string v, string profile) => $"CobolNum.FormatDisplay({v}, {profile})";
    public string NumDivideOrThrow(string a, string b, int scale, string rounding) => …;
    // strings
    public string StrRefMod(string s, string start, string len) => $"CobolString.RefMod({s}, {start}, {len})";
    public string StrSpliceInto(string s, string start, string len, string rhs, char? pad = null) => …;
    // edit / bool / float / pointer / ec / date …  (one method per emitted runtime member)
}
```

- `nameof`-anchored where possible (`nameof(CobolNum.Store)`) so the façade references the *actual* runtime symbols and
  a rename is a compiler error in `RuntimeApi` — the single point of truth for the codegen↔runtime ABI.
- `PlaceRenderer`, `ExpressionRenderer`, `ConditionRenderer`, `IntrinsicRenderer`, `DataEmitter` all emit runtime calls
  **only** through `RuntimeApi`. Grep-forbid bare `Cobol*.` string literals in `CodeGen/` (an analyzer/test guard).
- Fold the `CsLiteralCodec` split here or beside it: **one** literal boundary
  (`CsLiteral/DecodeCobolString/AllLiteralText/UnscaledLit/RepeatToWidth`) that recognizes **both** ISO string
  delimiters — fixing the apostrophe-VALUE silent miscompile (latent-bugs HIGH) structurally.

This is the pragmatic answer to "string emit vs SyntaxFactory": we keep readable string emit **and** get a typed,
single-file ABI contract — the best of both without SyntaxFactory's verbosity.

---

## 4. Module changes (current → target)

| Action | From → To | Why |
|---|---|---|
| create | `CodeGen/ICodeGenBackend.cs` (`ICodeGenBackend`, `BackendId`, `BackendOptions`, `BackendArtifact`, `BackendFactory`) | Materialize the promised seam (SSOT §1.1/§18 #23) |
| refactor | `RoslynBackend` (static string→dll) → `RoslynBackend : ICodeGenBackend` | The default backend behind the seam; owns all C# syntax |
| move/split | `CSharpEmitter.*` (15 partials, one god class) → `ProgramEmitter` + `StatementEmitter` + per-verb emitter classes + `DispatchEmitter` over `EmitContext` | Kill the god class; real class boundaries (understandability HIGH) |
| move | `CallEmitRunUnit` bind orchestration (`CSharpEmitter.Call.cs`) → `Binding/BindPipeline` returning `BoundCompilation` | No binding in the codegen layer; a real Bind phase (driver dim dependency) |
| refactor | `Binding/Model/Place.cs` `Read()/Write()` strings → structural `Place` + `AccessSegment`/`AccessPath` | Backend-neutral bound tree (G4); the blocking enabler for CIL |
| create | `CodeGen/Roslyn/PlaceRenderer.cs` | Sole owner of Place→C# rendering (moved out of `Place`) |
| rename | `EmissionContext` → `EmitContext`; drop `TargetScale/TargetReal/TargetRounding/InSizeErrorContext` | End name collision (SSOT §17); kill H1 mutable state |
| create | `CodeGen/Roslyn/ReceiverContext.cs` (readonly record struct) | Replace ambient receiver state with a parameter |
| create | `CodeGen/Roslyn/RuntimeApi.cs` | Typed façade over the ~60 runtime members (closes untyped coupling) |
| create | `CodeGen/Roslyn/FigurativeConstants.cs` | ONE figurative service; delete the 4 divergent copies |
| rename/move | `Emit/NumericRenderer` → `ExpressionRenderer`; `Emit/*` → `CodeGen/Roslyn/*` | Names match role; folder matches layer |
| move/split | `Emit/FieldEmitter.cs` → `CodeGen/DataDivision/{RecordStructEmitter,GroupImageCodec,GroupValueSlicer,ValueInitializer}` | Miscategorized 484-LOC DATA emitter; 4 concerns |
| create | source generator `CodeGen/BoundVisitorGenerator` + `[BoundNode]` on the sealed roots | Exhaustive dispatch; missing arm = compile error |
| delete ✅ P7.12 | `IntrinsicRenderer` static channel (`NumStatic/NumStaticExpr/StaticAdditive/StaticMul`) | Duplicate evaluator; routes through the one expression renderer via `ReceiverContext.None` |
| merge | `EmitText`/`CsLiteral`/`DecodeCobolString`/`AllLiteralText` → one `CsLiteralCodec` recognizing both delimiters | Fix apostrophe-VALUE miscompile; singular pattern |
| split | `RoslynBackend` compile vs packaging → `AssemblyPackager` (runtimeconfig + runtime-dll deploy) | Pure compile; packaging side-effects isolated |
| refactor | `RoslynBackend.ReferenceAssemblies()` → `static readonly Lazy<ImmutableArray<MetadataReference>>` | Cache framework refs (efficiency HIGH) |
| delete | dead JSON/XML grammar surface reaching codegen (coordinated with frontend dim) | Non-ISO; 0 spec occurrences (hard invariant #5) |

---

## 5. Migration — keeping the battery green throughout

The battery (greenfield conformance + unit + 33 characterization + legacy guard NIST 353 MATCH) must stay green at **every** step. Sequence
smallest-blast-radius first; each step is behavior-neutral and independently committable.

- **M0 — `ICodeGenBackend` wrap (no behavior change).** Introduce the interface; make the current `CSharpEmitter`+
  `RoslynBackend` implement it verbatim behind `RoslynBackend : ICodeGenBackend`. Driver selects `BackendId.Roslyn`.
  Guard green — pure indirection.
- **M1 — cache references + split packaging.** `Lazy` reference set; extract `AssemblyPackager`. Behavior-identical;
  large test-throughput win. Guard green.
- **M2 — rename `EmissionContext`→`EmitContext`; `ReceiverContext` parameter.** Mechanical; thread the record through
  the numeric renders, delete the four mutable setters and the manual resets. This is a refactor proven by the full
  battery (the H1 discipline becomes structural). Guard green.
- **M3 — `RuntimeApi` + `CsLiteralCodec`.** Route every runtime-call site and every literal-decode through the new
  helpers. Add the apostrophe-VALUE conformance goldens (elementary/group/`ALL 'x'`/Report-Writer SOURCE) — these
  **flip red→green** (a fixed bug), the one intentional battery change; land the fix + goldens together.
- **M4 — generated visitor.** Add `[BoundNode]` + the generator; convert `EmitStatement`→`StatementEmitter :
  IBoundStatementVisitor<bool>` and the renderer switches to visitors. Same output; the loud `_ =>` defaults become one
  `IBoundError` arm. Guard green; now a missing node is a compile error.
- **M5 — decompose the emitter.** Split the 15 partials into `ProgramEmitter` + per-verb emitter classes over
  `EmitContext`. Move `FieldEmitter`→`CodeGen/DataDivision/*`; unify `FigurativeConstants`. Purely structural; guard
  green after each verb group.
  *(Resulting structure: 18 `Verbs/*Emitter` collaborators + `EcEmitter` (cross-cutting, `CodeGen/` root) +
  `NameAllocator` (the §2.5 table's allocator — 15 per-counter sequences) + the `DispatchState`/`EcState`/
  `CallUnitState` typed state model (`EmitterState.cs`). `FieldEmitter` is split five ways — the physical-field
  model is a distinct 5th concern (`DataDivision/PhysicalModel`); the five are mutually recursive by design, wired
  by the `DataEmitter` facade. `ProgramEmitter` owns run-unit orchestration + the LIVE `Current` root and re-creates
  it per unit switch (`BeginUnit`); `DispatchEmitter`; `StatementEmitter` (the `IBoundStatementVisitor<bool>`); the
  `UnitEmitters` PER-UNIT composition root constructs all collaborators in acyclic ctor order and property-wires the
  cyclic edges (verbs↔Statements↔Ec, KeyedIo↔SeqIo). Emission reads ZERO bind-host session state — `OoEmitter` takes
  `comp.OoClasses`/`comp.InterfaceData` off the immutable compilation and reads the per-unit set through
  `ProgramEmitter.Current`. `CSharpEmitter` survives as the thin bind-host facade until P9 — a deviation from this
  doc's "CSharpEmitter is gone" end-state. The §3 guard's whitelist is the three remaining
  expression/condition renderers (`NumericRenderer`, `ConditionRenderer`, `OperandText`), which route at the P9
  `Roslyn/` consolidation; `IntrinsicRenderer` routed at Step 12 and left the whitelist
  (the `CobolRounding.`/`CobolPassMode.` typed-enum accesses are excluded — their emitted forms route
  exclusively via the `RoundingText`/`PassModeText` anchors).)*
- **M6 — extract `BindPipeline` (cross-dimension).** Move `CallEmitRunUnit`'s bind orchestration into the binder's
  pipeline returning `BoundCompilation`; `RoslynBackend.Emit` consumes it. Coordinated with the driver/binder
  dimensions — the highest-coordination step; land after those dimensions' pipeline extraction. Guard green.
- **M7 — structural `Place` (highest risk, incremental).** Convert one `Place` subtype at a time: move its `Read/Write`
  into `PlaceRenderer`, replace its string fields with structure, keep a temporary `Place.RenderReadLegacy()` shim so
  un-migrated verbs still compile. When all subtypes and all verb emitters consume `PlaceRenderer`, delete the shim.
  Each subtype conversion is guard-verified in isolation. This is where the differential harness earns its keep — byte
  output must be identical pre/post.
- **M8 (future, out of this rearchitecture's critical path) — `CilBackend`.** Implement `ICodeGenBackend` over
  Mono.Cecil with private branch lowering; add `--backend cil`; the differential harness cross-checks Roslyn vs CIL
  stdout. Only possible because M7 made the tree neutral.

**Green-keeping tools:** the differential harness (Roslyn output vs the committed goldens vs `nist/valid/*.txt`) at every step;
`guard-fast.sh` before every commit; the new "no bare `Cobol*.` in `CodeGen/`" analyzer test after M3; the
compile-error-on-missing-visitor-arm after M4.

---

## 6. Risks

1. **Structural `Place` blast radius (M7) — HIGH.** ~all verbs read `Place.Read()/Write()`. Mitigation: the
   subtype-at-a-time shim; the differential harness asserts byte-identical output; do it last, after the visitor and
   decomposition make consumers few and well-typed.
2. **`RuntimeApi` surface is large.** ~60 members. Mitigation: `nameof`-anchor to catch drift; migrate incrementally
   (a site not yet routed still works). It is additive, not a rewrite.
3. **Source generator complexity/build cost.** A hand-written exhaustive `switch` with a `default => throw` that the
   generator *replaces* is a fallback if the generator proves fiddly; even a T4/manual partial gives most of the
   benefit. Keep it small (one `Accept` per root).
4. **Cross-dimension coupling at M6.** `BindPipeline` extraction is shared with the driver/binder dimensions; a
   mis-sequenced merge could double-move `CallEmitRunUnit`. Mitigation: land M6 only after those dimensions publish the
   `BoundCompilation` type; treat it as a joint checkpoint.
5. **CIL backend never actually built ⇒ neutrality bit-rots.** If M8 is indefinitely deferred, the G4 neutrality could
   silently regress. Mitigation: a lightweight `NeutralityTest` that asserts no `Place` subtype or bound node exposes a
   `string`-returning render method (reflection/analyzer) — keeps the invariant enforced even without a live CIL
   backend.

---

## 7. Open questions for the owner

1. **Bind-phase ownership.** M6 moves `CallEmitRunUnit`'s orchestration into a `BindPipeline`/`BoundCompilation` that
   spans the binder + driver dimensions. Confirm this dimension **consumes** (not owns) that type, and that the
   `BindPipeline` extraction is scheduled in the driver dimension's roadmap before M6.
2. **CilBackend timing.** Is the CIL backend still an active goal, or aspirational? If active, it should get its own
   dimension/deep-dive after M7. If aspirational, do we still pay the M7 `Place`-neutralization cost now (recommended:
   **yes** — it also kills the leaky-emitter/duplication class and is the correct architecture regardless of CIL), or
   defer M7 and accept the string-carrying `Place` as tech debt? Recommendation: keep M7; it is worth it on its own
   merits.
3. **Source generator vs hand-written exhaustive switch.** Approve adding a Roslyn source generator to the build
   (another `java+pwsh`-class build prerequisite is *not* added — it is a NuGet analyzer), or prefer a hand-maintained
   `Accept` with a `default => throw` (loses compile-time exhaustiveness but zero build machinery)? Recommendation:
   source generator — exhaustiveness is the whole point.
4. **`.g.cs` always vs on-demand.** Packaging split (M1) makes it cheap to gate `.g.cs` writing behind a flag. Keep
   always-on (current behavior, aids the loud-ICE culture) or make it `--emit-source`? Recommendation: keep always-on.
5. **Diagnostic type unification.** `BackendArtifact.Diagnostics` assumes the ONE structured `Diagnostic` type the
   driver dimension proposes (retiring `EditionContext`'s `List<string>`). Confirm that unification lands so the
   backend can carry code+location+severity to the CLI.
