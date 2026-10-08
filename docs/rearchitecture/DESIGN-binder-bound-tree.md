# DESIGN — Target Binder Pipeline & Bound-Tree / IR

Status: IMPLEMENTED (rearchitecture — PHASE-07 Steps 1–12, phase complete). The binder decomposition in this design
is built in the tree: the DAG-validated pass framework, the immutable `BoundCompilation` + the ONE scoped
`SymbolTable`, the source-generated exhaustive bound-tree visitor, the god-class dissolution into
`BinderContext`-threaded per-verb collaborators, the `StorageFormPass`, the structural (non-string) `Place`
(Step 11), and the FUNCTION-argument grammar (Step 12 — arguments bind through the ONE `ExpressionBinder.BindExpr`;
the hand-rolled intrinsic argument parser is deleted). The §7 open questions were resolved during execution (the
source generator was adopted).
Scope: the binding middle-end of the greenfield compiler (`src/Cobol.Net.Compiler/Binding/**`) and the
bound-tree contract it produces (`Binding/Bound/BoundTree.cs`). Dimension: **Target binder pipeline &
bound-tree/IR design**. Sibling design docs own the data model (`DataItem`/`PicInfo`/`Place`/`StorageForm`),
the emitter decomposition, the editions framework, and the driver/pipeline shell; this doc cites them where
they meet the binder and declares the dependencies explicitly at the end.

SSOT alignment: `docs/COBOLNET_DESIGN.md` §1.1 (no shared lowered IR), §2 (bind-once bound tree), §16 (build
order), §18 (settled decisions). This design **keeps** the owner-locked §1.1/§18.23 "no shared lowered IR"
decision and works within it — see §3.3 for the precise distinction between the *rejected* CIL-shaped branch IR
and the *adopted* semantic-normalization-on-the-bound-tree.

> **The binder is edition-agnostic (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1).** Version conformance is one mechanism, and this doc's binder contract reflects it
> in three ways: (1) the binder makes ZERO `ConstructRegistry.Check` calls — edition gating is a single
> `VersionConformancePass` over the bound tree (the sole syntactic+semantic gate). (2) NO
> `.Syntax` back-reference is added to any bound node — the `BoundTree.cs` invariant stands. The pass identifies
> syntactic introduction/removal/phrase gates via a PRESENCE-based parse-tree arm (over `GroupBindContext.Tree`, running
> after bind) — introduction gates must fire on the construct's RECOGNITION, not its bound node, which a below-edition +
> semantically-invalid construct never produces; semantic gates use bound-node type/attribute. (3) Bind and emit are separate driver-gated
> phases — codegen runs emit-only-if-clean, never on an errored tree. Pipeline: parse → edition-agnostic bind →
> VersionConformancePass → emit-if-clean → backend; there is no `ReservedWordEditionHints`. Full design:
> `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`.

---

## 1. Current problem (grounded)

The binding middle-end is one of the two worst architectural-debt centers in the tree. Five concrete,
code-anchored problems:

### 1.1 Implicit pass ordering — the prime latent-bug class
`DataBinder.BindResolve` (`Binding/DataBinder.cs:210-258`) is a hand-ordered sequence of ~15 post-build passes
(`ExpandTypes → UsageInheritancePass → InheritSignClauses → ResolveRedefines →
ClassifyRedefinesClasses → CheckStrongTypeDeclarations → OoRouteMethodRedefinesBackings → OdoResolve →
DynamicResolve → ResolveFiles → GateNationalRecords → ResolveReports → CallBindExternalAndGlobal →
PtrBindBasedAndAddressables → the FILE-record whole-group loop`). The ordering constraints are real (the code
comment at :212-217 explains that `InheritSignClauses` MUST precede `ClassifyRedefinesClasses` because a
SEPARATE sign widens the image that feeds class-max width) but they exist ONLY as prose + call order. Nothing
asserts that a pass's inputs were produced. Reordering, inserting, or a future feature's new pass silently
mis-computes with no failure. This is the exact "check gated on the wrong condition → silent mis-compile" class
the rearchitecture targets.

There is a SECOND, hidden pass pipeline: the real middle-end orchestrator is `CSharpEmitter.CallEmitRunUnit`
(`CodeGen/CSharpEmitter.Call.cs:88-147`) — ~12 more implicit sequential passes (collect units, bind
interface/class/program data, validate overrides, bind bodies, build the UDF table, bind procedures, re-sync
`StoreAsImage`, `MarkStoreAsImage`, compute the EC gate, qualify file connectors) buried INSIDE the codegen
class. The driver's own phase names (Bind / Emit / Roslyn) are a fiction: there is no Binder phase boundary.

### 1.2 No symbol table — a public-mutable-dictionary blackboard
`DataBinder` exposes ~30 public get-only-but-mutable collections (`Roots`, `ByName`, `Conditions`,
`IndexFields`, `CapacityRegisters`, `TypeDecls`, `Files`, `FilesByName`, `Switch*`, `Alphabets`, `UserClasses`,
`Reports`, `WholeGroupReferenced`, `OoPendingPropertyOps`, `CompilerTempClones`, …). These ARE the module's API.
Downstream phases both read AND write them: `ReferenceResolver` writes `WholeGroupReferenced` during procedure
binding (`ReferenceResolver.cs:280,303`); `StatementBinder.MoveFigurative` writes `DataItem.StoreAsImage`;
`CSharpEmitter` writes `StoreAsImage` via `MarkStoreAsImage` AFTER binding and re-syncs `CompilerTempClones`.
There is no ownership boundary and no immutability — every collection is an open write channel.

Name lookup is quadrupled: `LookupData`, `LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor` —
because OO method scoping (`ActiveMethodScope`, `OoMethodDataScope`) is a parallel shadow name-model that
consumers must opt into. Callers pick the wrong overload and silently miss method-local shadowing.

### 1.3 God-class binders
`StatementBinder` is ONE `sealed partial class` shattered across 23 files (~9.4k LOC) doing five fused jobs:
procedure-table construction, statement dispatch, expression/condition binding, the single relation checkpoint,
and inline SR/edition validation. The partials share full private state, so "how binding works" requires opening
23 files. `DataBinder` is the same pattern across 7 files (~3.9k LOC). Feature partials give the *illusion* of
decomposition (`Binding/Bound/StatementBinder.KeyedIo.cs`, `.Sort.cs`, `.Intrinsics.cs`, …) but they are the same
object with no enforced boundary.

### 1.4 Hand-maintained god-switches, no exhaustiveness
Statement dispatch is two parallel hand-switches kept in lockstep by convention: `BindStatementCore`
(`StatementBinder.cs:170-231`, ~50 arms) and `EmitStatement` (`CSharpEmitter.cs:347-...`, ~79 cases), each
ending in a loud `_ =>` / `default` that defers an unhandled node to a RUNTIME `LoudStmt`/`BoundUnsupported`
rather than a compile error. The same bound tree is walked by ≥5 more independent type-switches
(`BoundStores.StoreKindOf`, `NumericRenderer.Render/AsNum`, `OperandText.AsString/IsString`,
`ConditionRenderer.Render`, `AlterCollectFields`). Adding a bound node is shotgun surgery across ≥7 sites with
zero compiler help; a forgotten arm ships as a runtime throw.

### 1.5 Smart emitter / semantics leaking past the bound tree
The bound tree is supposed to be the SSOT (`BoundTree.cs:7-13` — "the backend renders the bound tree, it never
re-walks the parse tree"). In practice the emitter re-derives semantics the binder resolved: `EmitMove`'s
`ConvertSource` re-classifies MOVE category at emit time (`CSharpEmitter.cs:481-501,714`); the emitter reads
binder-set `DataItem.StoreAsImage`/`IsStrongGroup`/`ImageWidth` — a binder→emitter channel OUTSIDE the bound
tree; and binder verb partials assemble runtime C# STRINGS directly (`CobolTable.At(...)`, `RefReceiving(v)`,
`$"{Path}.{child.CsName}"` in `Initialize.cs:327-385`) — bind-time code building emit-time C#, blurring the §2
bind/emit boundary. `Place.Read()/Write()` emit raw C# strings (`Place.cs:124-138`).

Net effect: correctness depends on an implicit, undeclared execution order spread across the binder, the shared
data model, and the emitter — the definition of the fragility the rearchitecture must remove.

---

## 2. Target design — overview

Four structural moves, each attacking one problem class, all preserving the HARD INVARIANTS (typed-native only,
spec-first, one canonical mechanism, no shared lowered IR):

1. **A real `Binder` phase** with an explicit, **manifest-driven pass pipeline** (`BindPipeline`) whose passes
   declare `Requires`/`Produces` capability tokens; the DAG is validated at startup and a completion-phase gate
   makes "read a fact before its producing pass ran" a hard error, not a silent miscompile. This absorbs BOTH
   `BindResolve` and the binder half of `CallEmitRunUnit`.
2. **An immutable `BoundCompilation` result + a `SymbolTable`/`ScopeResolver`** replacing the 30 public mutable
   dictionaries. Mutation happens only inside passes via a write-capability handle; downstream phases (emitter)
   receive a read-only view. OO method shadowing folds into the ONE `ScopeResolver.Resolve`, deleting the lookup
   quadruple.
3. **Source-generated exhaustive dispatch** over the bound tree (`IBoundStatementVisitor<T>` + a generated
   `Accept`), so `EmitStatement`, `BoundStores`, and the renderer switches become exhaustive — a missing arm is a
   COMPILE error. The five error-node families get one `IBoundError` marker.
4. **Real collaborator classes** over an injected `BinderContext`, replacing the partial-class god objects; and a
   thin **semantic-normalization step on the bound tree** (NOT a lowered branch IR) that moves emit-time
   re-classification (MOVE kind, storage form, table-access polarity) back onto the bound node, making the
   emitter a pure renderer.

The pipeline shape becomes literal and matches the driver's phase names:

```
Frontend (parse) ──► Binder (BindPipeline: N ordered passes) ──► BoundCompilation ──► Backend (emit) ──► Roslyn
```

---

## 3. Target design — concrete

### 3.1 The pass framework (`Binding/Pipeline/`)

A pass is a unit of forest computation that declares what forest facts it consumes and produces. Capabilities are
an enum of named facts, NOT free strings, so the DAG is compile-time enumerable and greppable.

```csharp
namespace CobolNet.Binding.Pipeline;

/// One computed forest fact. A pass may only READ a capability listed in Requires and may only
/// WRITE (mark produced) a capability listed in Produces. The manifest validates the DAG at startup.
public enum Capability
{
    EntryTree,             // the DataItem forest exists (produced by declaration binding)
    TypesExpanded,         // TYPEDEF/TYPE clones materialized (ExpandTypes)
    UsageMarkersResolved,  // USAGE INDEX/marker PicInfo resolved (was ResolveIndexItems)
    UsageInherited,        // group USAGE pushed to leaves (InheritUsageClauses)
    SignInherited,         // group SIGN pushed to leaves (InheritSignClauses)
    RedefinesResolved,     // REDEFINES/RENAMES targets bound (ResolveRedefines)
    RedefinesClassified,   // shared-storage classes + tiers assigned (ClassifyRedefinesClasses)
    StrongTypesChecked,    // §13.18.57 SR3/SR4
    OdoResolved, DynamicResolved, FilesResolved, NationalGated, ReportsResolved,
    ExternalGlobalBound, PointersBound,
    ProcedureBound,        // PROCEDURE DIVISION bound to BoundProgram(s) (produces WholeGroupReferenced facts)
    StorageFormComputed,   // the single StorageForm/StoreAsImage decision (§3.4) — REQUIRES ProcedureBound
}

public interface IBindPass
{
    string Name { get; }
    IReadOnlyList<Capability> Requires { get; }
    IReadOnlyList<Capability> Produces { get; }
    void Run(BindContext ctx);
}
```

`BindPipeline` owns the ordered list, validates it once, and runs it:

```csharp
public sealed class BindPipeline
{
    private readonly IReadOnlyList<IBindPass> _passes;
    public BindPipeline(IReadOnlyList<IBindPass> passes) { _passes = passes; ValidateDag(); }

    // Startup assertion: for every pass, every Required capability is Produced by an EARLIER pass.
    // A violation throws a CompilerConfigurationException at construction (fail-fast, never at runtime
    // on a user program). This is the structural cure for the pass-ordering latent-bug class (§1.1).
    private void ValidateDag() { /* accumulate produced-set; assert Requires ⊆ produced-so-far */ }

    public void Run(BindContext ctx)
    {
        foreach (var p in _passes) { p.Run(ctx); ctx.MarkProduced(p.Produces); }
    }
}
```

**Completion-phase gate (the second half of the structural cure).** The manifest guards *pass* order; a
complementary guard protects *field reads* on the data model. `BindContext` carries a `Capability` "watermark"
(highest capability produced so far). Late-resolved `DataItem`/`OccursSpec`/`RedefinesClass` facts
(`Class`, `Tier`, `ClassOffset`, `CapacityRegister`, `StorageForm`) are read through accessors that assert the
watermark has reached the producing capability — in Debug builds this is an `assert`; it converts today's silent
"read a null Tier" into a loud, located compiler error. (Reads are pervasive, so the accessor is a thin
`ctx.Require(Capability.RedefinesClassified)` at the small number of pass entry points, not per-field.)

The canonical pass list (the manifest — the ONE place a maintainer reads to learn the order) lives in
`Binding/Pipeline/BindManifest.cs`:

```csharp
public static IReadOnlyList<IBindPass> Standard() =>
[
    new DeclarationPass(),          // Produces EntryTree (OPTIONS, SPECIAL-NAMES, FILE-CONTROL, FD/SD, WS, LINKAGE, PD formals)
    new ExpandTypesPass(),          // R:EntryTree            P:TypesExpanded
    new UsageMarkerPass(),          // R:TypesExpanded        P:UsageMarkersResolved   (renamed from ResolveIndexItems)
    new UsageInheritancePass(),     // R:UsageMarkersResolved P:UsageInherited
    new SignInheritancePass(),      // R:UsageInherited       P:SignInherited
    new RedefinesResolvePass(),     // R:SignInherited        P:RedefinesResolved
    new RedefinesClassifyPass(),    // R:RedefinesResolved    P:RedefinesClassified
    new StrongTypeCheckPass(),      // R:RedefinesClassified  P:StrongTypesChecked
    new OoRedefinesRoutePass(),     // R:RedefinesClassified  P:(routing)
    new OdoResolvePass(),           // R:RedefinesClassified  P:OdoResolved
    new DynamicResolvePass(),       // R:OdoResolved          P:DynamicResolved
    new FilesResolvePass(),         // R:RedefinesClassified  P:FilesResolved
    new NationalGatePass(),         // R:FilesResolved        P:NationalGated
    new ReportsResolvePass(),       // R:FilesResolved        P:ReportsResolved
    new ExternalGlobalPass(),       // R:EntryTree            P:ExternalGlobalBound
    new PointerBindPass(),          // R:EntryTree            P:PointersBound
    new ProcedureBindPass(),        // R:AllDataFacts         P:ProcedureBound      (runs StatementBinder over every unit)
    new StorageFormPass(),          // R:ProcedureBound       P:StorageFormComputed (§3.4 — the ONE StoreAsImage decision)
];
```

This single list REPLACES both `DataBinder.BindResolve:210-258` and the binder half of `CallEmitRunUnit`. Note
`ProcedureBindPass` runs BEFORE `StorageFormPass`: whole-group usage collected during procedure binding is an
INPUT to the storage-form decision, so the today's cross-phase mutation (emitter mutating the binder after the
fact) becomes a normal upstream→downstream data flow inside one pipeline.

### 3.2 The symbol table & the immutable result (`Binding/Model/`)

`Bind()` returns an immutable `BoundCompilation`; the emitter consumes a read-only view.

```csharp
public sealed record BoundCompilation(
    IReadOnlyList<BoundUnit> Units,          // one per program-unit / class-method-set
    OoClassModel Classes,                    // resolved class table (read-only projection)
    SymbolTable Symbols,                     // the ONE name/scope resolver
    IReadOnlyList<Diagnostic> Diagnostics);

public sealed record BoundUnit(
    DataModel Data,                          // the read-only data-division model (Roots + derived facts)
    BoundProgram Procedure,                  // the bound PROCEDURE DIVISION (unchanged node shapes)
    IReadOnlyList<BoundMethod> Methods);
```

`SymbolTable` collapses the lookup quadruple into ONE scoped resolver that already understands OO method
shadowing. There is exactly one lookup entry point:

```csharp
public sealed class SymbolTable
{
    // The ONLY name lookup. `scope` is the active method scope (or Program scope for non-OO code); OO
    // sibling-invisibility (§11.7 GR5) is enforced HERE, not by callers choosing an overload.
    public bool TryResolve(QualifiedName name, Scope scope, out DataItem item);
    public bool TryResolveCondition(QualifiedName name, Scope scope, out Condition88 cond);
    public bool TryResolveIndex(QualifiedName name, Scope scope, out DataItem index);
    public IReadOnlyList<DataItem> Roots(Scope scope);
}
```

`LookupData` / `LookupDataInScopeOf` / `TryGetVisibleIndexField` / `IndexFieldFor` are DELETED; every call site
passes an explicit `Scope` (defaulting to `Scope.Program`). This makes scoped lookup the ONLY lookup — the
singular-pattern rule — and removes the "caller forgot the scoped overload → missed shadowing" bug class.

**Mutation discipline.** During binding, passes need to write into the model (register a condition, a capacity
register, a whole-group-referenced fact). They receive a `SymbolTableBuilder` (a write handle) inside
`BindContext`; the builder is sealed into the immutable `SymbolTable` when the pipeline completes. The emitter
never receives the builder — it receives `BoundCompilation`, whose collections are `IReadOnlyList`/read-only
interfaces. This closes every open write channel in §1.2 by construction. In particular the emitter's
`MarkStoreAsImage` write-back is DELETED (see §3.4).

### 3.3 The bound tree & the IR decision (§18.23 upheld)

**Decision: KEEP §1.1/§18.23 — there is NO shared lowered branch IR.** The bound tree remains the single
backend-neutral SSOT; the Roslyn backend preserves structure, and the future CIL backend does its own
branch-level lowering privately. This is owner-locked; we do not relitigate it.

**But** we draw a sharp line the current code blurs, between two things that both got loosely called "the bound
tree":

- **Lowered IR (REJECTED, unchanged):** CIL-shaped basic blocks / branch instructions. Would destroy the readable
  C# output. Not introduced.
- **Semantic normalization ON the bound tree (ADOPTED):** the bound node carries the fully-resolved *semantic
  classification* of the operation, so no consumer re-derives it. This is not a new IR layer — it is completing
  the bind-once contract that §2/`BoundTree.cs:16` already promises but the emitter violates (§1.5).

Concretely, three classifications move from emit-time re-derivation onto the bound node:

```csharp
// MOVE: BindMove computes the category-conversion kind ONCE; EmitMove becomes a pure renderer.
public enum MoveKind { Group, ElementaryAlphanumeric, ElementaryNumeric, NumericEdited,
                       AlphaEdited, FigurativeFill, FigurativeToNumericImage, RefModSlice }
public sealed record BoundMove(Place Target, BoundOperand Source, MoveKind Kind, StorageForm TargetForm) : BoundStatement;

// Table access polarity travels on the Place, not re-decided at emit.
// Storage form (§3.4) travels on the Place/operand, not read off a mutated DataItem flag.
```

`ConvertSource`'s emit-time category switch (`CSharpEmitter.cs:714`) is deleted; `EmitMove` reads `move.Kind`.
The emitter stops reading `DataItem.StoreAsImage`/`IsStrongGroup` — those facts arrive on the bound node.

**AS BUILT, the classification is PER RECEIVING OPERAND and carries the SENDER with it.** One `MOVE` has a list
of receivers (`BoundMove(BoundOperand Source, IReadOnlyList<Place> Targets)`), and they genuinely differ — a
ref-mod slice, a group and an elementary receiver can appear in one statement — so the sketch's scalar `Kind`
became `IReadOnlyList<MoveStore> Stores`, one `MoveStore(BoundOperand Sender, MoveKind Kind, MoveSenderOrigin Origin)` per target,
computed once at construction by `MoveClassifier.Classify`. The **sender** half is there because ISO §14.9.25.4
GR2/GR3 rewrite the sending operand *per receiver*: a zero-length literal-1 "is treated as if it were the
figurative constant SPACE" (GR3: ZERO for a boolean literal) "and the receiving operand is other than a
dynamic-length elementary item", so `MOVE "" TO D-DYN, N-NUM` empties the dynamic-length item and space-fills
the numeric one from one statement (kb/Work PB425). `MoveClassifier.Sender` is the one home of that rule,
`ZeroLengthItemRoute` the runtime half GR1's zero-length-*item* clause needs, `Origin` says WHICH rule
put the sender there (a written figurative is §14.9.25.3 SR5's removed construct; a substituted one is
legal source, and a diagnostic about an unstorable fill must not blame the wrong rule), and every consumer — the
emitter's per-store renderer, `MoveBinder.MarkFillImageStorage`'s `StoreAsImage` fact — reads the answer off
`Stores` rather than re-deriving it. ⛔ `BoundMove.Source` remains the **written** sending operand and is what
every syntax screen reads (`MoveBinder`'s SR1/Table-16 arms, `VersionConformancePass.GateMove`'s SR5 gates):
GR2/GR3 substitute a *value*, and gating on the substitution would reject `MOVE "" TO PIC 9(3)`, which the
standard permits at every edition.

**GR1's zero-length-*item* clause is CONDITIONAL, and §8.5.4 says on what.** `ZeroLengthItemRoute` emits a
runtime length test only for a sending shape that carries its own length field; a reference-modified or
function-identifier sender is first frozen into `SendingValueTemp`'s carrier by `MoveBinder`, and
`MoveClassifier.NeedsLengthFreeze` decides when. It asks `CanBeZeroLengthItem` — §8.5.4's own enumeration —
because the *shape* is not the whole antecedent: item 9 admits a reference-modified item only "when that has
been permitted by use of the compiler directive REF-MOD-ZERO-LENGTH" (outside such a region §7.3.23.3 GR1 raises
EC-BOUND-REF-MOD instead of producing a zero-length item), and item 6 admits "an intrinsic function that returns
a zero-length value", which a NUMERIC function never is — §15.4 puts its returned value in a temporary
elementary data item with at least one digit position. Freezing on the shape alone was a wrong answer rather
than a wasted temp: a numeric function sender was re-described into `SendingValueTemp.FunctionValuePic` (21
integer + 9 fraction digits), narrower than a standard-decimal or floating-point result, so `MOVE FUNCTION E`
into a 31-digit item re-rounded (kb/Work PB425 finisher).

**GR4's elementary-vs-group decision is ONE predicate asked of BOTH operands** — `MoveClassifier.IsGroupPlace`,
read by `IsGroupSender` for the sending side and by `Kind` for the receiving one, because GR4 states one rule
over both ("the sending operand is either a literal or an elementary item AND the receiving item is an
elementary item"). It is not purely structural in either direction: §13.18.45.4 GR2 makes a level-66
`RENAMES … THROUGH` alias an alphanumeric GROUP item although this compiler models it as one composed
elementary alphanumeric view (`RenamesPlace`, which exists only for the THROUGH form — GR1's no-THROUGH alias
forwards to the renamed item's own place), §13.18.29.4 GR1b/GR2b make a bit or national group act as an
elementary item, and §8.4.3.3.4 GR6 makes a reference-modified result elementary. Re-spelling the test on the
receiving side is how the two sides came to disagree (kb/Work PB430).

**GR4's "no conversion of data from one form of internal representation to another" is ONE codec asked in BOTH
directions** — `OperandText.NonElementaryMoveSender`, called by the group-receiver and group-sender arms of
`MoveEmitter` alike. For a field operand it is `AsStorageImage`, the per-shape storage recipes (zoned, radix-2,
BCD, IEEE, UTF-16BE national, packed bits); for a literal or figurative it is the operand text, which has no
internal representation to preserve. The operand *text* is the wrong reader for that clause on an elementary
sender: it renders a COMP-3 item as its zoned DISPLAY digits, which is the conversion the clause forbids and
also occupies the wrong number of character positions.

**Place stays the lvalue abstraction but stops being a string.** Today `Place.Read()/Write()` return raw C#
(`Place.cs:124-138`). Target: `Place` holds STRUCTURED path segments (root item + subscript `BoundExpr`s + optional
ref-mod span); the *emitter* owns rendering path→C# text. This removes the "binder builds emit-time strings"
leak (§1.5) and lets the CIL backend render the same structured Place differently. (Detailed Place shape is owned
by the data-model sibling design; this doc requires only that Place carry structure, not text, so the binder
produces no C# strings.)

**Dispatch: source-generated exhaustive visitor.** Add to `BoundTree.cs`:

```csharp
public interface IBoundStatementVisitor<out T> { T VisitMove(BoundMove n); /* … one per node … */ }
public abstract partial record BoundStatement { public abstract T Accept<T>(IBoundStatementVisitor<T> v); }
```

A small **source generator** (`Binding/Bound/BoundVisitor.g`) emits the per-record `Accept` overrides and the
visitor interface from the record hierarchy, so adding a `BoundStatement` record without handling it in every
visitor is a COMPILE error. `EmitStatement`, `BoundStoreAnalysis` (renamed from `BoundStores`),
`NumericRenderer`, `ConditionRenderer`, `OperandText`, `AlterCollectFields` all convert from a `switch`+`_ =>`
into `IBoundStatementVisitor<T>` implementations. The loud runtime defaults are DELETED. The five error families
(`BoundUnsupported`/`BoundOperandError`/`BoundExprError`/`BoundConditionError`/`BoundBoolError`) get one
`IBoundError` marker so a visitor handles "any error node" once. ⛔ **As built (kb/Work PB1029), the error families are CLOSED
types:** `BoundRejected`, `BoundExprError`, `BoundOperandError` and `BoundBoolError` have private constructors and are
obtained only through factories (`Report` / `Reported`; `Refused` / `Unbuilt` / `Carry` / `Report`) that put them on
the `EditionContext` refusal ledger, which `StatementBinder.BindStatement` checks per statement (COBOLNET2362 for a
refusal that drew no error; COBOLNET1756 for an unbuilt operand) — `COBOLNET_DESIGN.md` §1.4 is the contract and
`RefusalNodeDriftTests` the drift test. The reference resolver's own answer is closed the same way (kb/Work PB1030):
`RefResolution` — `Place | Reported | Deferred` — from which a caller picks its refusal node, with `DeferredShape` the one
census of unbuilt reference shapes (`RefResolutionDriftTests`). (If the owner declines a source generator, the
fallback is a hand-written `abstract` visitor base with no default method — the compiler then forces every
visitor to implement every node; slightly more boilerplate, same exhaustiveness guarantee. See OPEN QUESTION 1.)

### 3.4 The `StorageForm` pass — killing the `StoreAsImage` mutable flag

`StoreAsImage` is today recomputed/mutated at 7+ sites across three layers (§1.2, dossier duplication-dispatch
HIGH). The data-model sibling design defines a single computed `StorageForm` discriminator (NativeLong / Int128 /
Float / Double / StringImage / TierBWindow / TierCByte / DynTable / ObjectRef / Pointer). This binder design owns
WHERE it is computed: **`StorageFormPass`, a single pass running after `ProcedureBindPass`** so it sees all facts
including whole-group procedure-division use. It writes `StorageForm` as an init-only fact through the builder;
after the pipeline it is immutable. `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) and the
`CompilerTempClones` re-sync are DELETED. The FILE-record whole-group loop at `DataBinder.cs:238-257` folds into
`StorageFormPass` (it is the same rule). This removes the last emitter→binder write-back.

### 3.5 God-class decomposition — collaborators over `BinderContext`

`BindContext` is the shared, mostly-immutable spine threaded to every pass and every collaborator:

```csharp
public sealed class BindContext
{
    public required Core.ProgramUnitContext Parse { get; init; }
    public required EditionInfo Edition { get; init; }          // immutable (editions sibling design)
    public required IDiagnosticSink Diagnostics { get; init; }  // the ONE sink (editions/driver sibling design)
    public required SymbolTableBuilder Symbols { get; init; }
    public required ReferenceResolver Refs { get; init; }
    public required RecordLayout Layout { get; init; }          // the ONE offset/width service (dossier reorg)
    public Capability Watermark { get; private set; }           // completion-phase gate (§3.1)
    // scoped push/pop for OO method binding, replacing ambient ActiveMethodScope mutation:
    public IDisposable EnterMethodScope(Scope s);
}
```

`StatementBinder`'s 23 partials become real classes over `BindContext`, matched to the existing seams:
`ProcedureTableBuilder` (paragraphs/sections/declaratives/pc + `ResolveProcedure`), `ExpressionBinder`,
`ConditionBinder` (incl. `AbbrevCarry`, `CheckedRelational`), and per-verb binders `MoveBinder`, `ArithmeticBinder`,
`IfBinder`, `PerformBinder`, `KeyedIoBinder`, `SequentialIoBinder`, `SortBinder`, `StringBinder`, `InspectBinder`,
`InitializeBinder`, `IntrinsicBinder`, `UdfBinder`, `ReportWriterBinder`, `CallBinder`, `SetBinder`, `OoBinder`.
The core `StatementBinder` shrinks to the dispatch table + shared helpers. `DataBinder` likewise splits into
`FileControlBinder`, `SpecialNamesBinder`, `ReportSectionBinder`, `RedefinesClassifier`, `TypedefExpander`,
`LinkageBinder`, `PointerBinder`, `OoMethodDataBinder` — each a pass or a pass-owned collaborator.

**Inline SR/edition validation moves out.** The `data.Edition.Error(...)` calls smeared through the binder
(MOVE figurative gates, composite-of-operands, boolean/class/pointer relation rules) route through a
`StatementValidation` component beside `Validation/EditionValidator`, keeping the binder about *producing bound
nodes* (`project_dual_backend_goal` spirit). The binder calls `validation.CheckMove(...)`; the validation component
owns "what is legal at which edition" and reports to the `IDiagnosticSink`.

**Shared helpers deduplicated (one canonical mechanism per job):** `PhraseBlocks.BuildPair(blocks, notFirst)`
(the ON/NOT-ON extractor, ~8 clones → 1), `RecordLayout` (offset/width/key-index, replacing Sort vs KeyedIo vs
FieldEmitter vs OdoModel copies), `CobolLiteral.Decode` (the tripled `DecodeCobolString` → one Common/ codec),
`DataItem.Root` (the 4× RootOf walk → one accessor), `FigurativeConstants` (the 4-site figurative-fill → one
service). These are cross-cutting with the emitter-renderer sibling design; the binder owns the bind-side callers.

### 3.6 The §8.8.1.1 operand funnel — every entry declares its rule

⛔ **An operand slot in this compiler used to acquire ISO §8.8.1.1 BY DEFAULT — by being handed to
`ExpressionBinder.BindExpr` — rather than by declaring that §8.8.1.1 governs it.** Everything in the
PB169–PB172 burn-down cluster followed from that default, and the four defects look different only because the
decision was recorded in four different places: an enum member at ten sites, a call-site comment at two, a
private category switch at three, and **nowhere at all** in `ReferenceResolver`'s token renderer.

Every operand slot answers **three independent axes**, and both now live in `OperandContextRules.Rules()`
(`ExpressionBinder.cs`), a switch with no discard arm:

| Axis | Question | Clause |
|---|---|---|
| **A — NumericClassScreen** | Does §8.8.1.1's class-numeric screen govern this position, or does the position have its own syntax rule? | §8.8.1.1 |
| **B — IndexNameScreen** | Is the index-**name** screen applied here, or is this one of r7's five contexts? | §13.18.38.3 r7 |
| **C — IndexDataItemAdmitted** | May a class-index **data item** be referenced here? | §13.18.60.3 SR10 |

| `OperandContext` | A | B | C | Why |
|---|---|---|---|---|
| `Arithmetic` | screen | screen | no | the plain arithmetic-expression position |
| `ArithmeticIndexWindow` | screen | exempt | yes | BOTH lists name it (SET amounts and values; a SEARCH or relation operand that is a bare name never reaches the expression spine, PB2018) |
| `ArithmeticIndexNameWindow` | screen | exempt | no | r7 lists it, SR10 does not (subscript segment · PERFORM VARYING FROM/BY) — PB215 |
| `FunctionArgument` | exempt | screen | yes | the function's own §15.x argument rule governs (COBOLNET1627) |
| `CallByValue` | exempt | exempt | yes | §14.9.4.3 SR22 governs, and screens the operand itself (COBOLNET1628) |

⛔ **AXIS C IS NOT A CONSEQUENCE OF AXIS A, and assuming it was is a measured mistake.** The two index lists
are genuinely different rules about different things:

- **r7** (index-**name**): *as a subscript · PERFORM VARYING · SEARCH VARYING · SET · a relation-condition operand.*
- **SR10** (index **data item**): *a SEARCH or SET statement · a relation condition · an intrinsic function
  argument · an inline method invocation argument · the USING phrase of a procedure division header · the USING
  phrase of a CALL or INVOKE statement.*

A **subscript** and **PERFORM VARYING** are on r7's list and not SR10's; an **intrinsic argument** and a **CALL
USING** phrase are on SR10's and not r7's. Deriving C from A ("class index is not class numeric") rejected
`SET IN1 TO IDN1` in **eight NIST programs** — every one a SET statement SR10 names outright. A rule that
enumerates CONTEXTS cannot be modelled as a property of the OPERAND
(`feedback_model_the_rule_shape_not_one_case`). The two lists are therefore TWO ROWS (kb/Work PB215): the
contexts on both lists (`ArithmeticIndexWindow`) and the contexts on r7's alone (`ArithmeticIndexNameWindow`).
The Report Writer's VARYING (§13.18.64.2, plain `arithmetic-expression-1/-2`) is on NEITHER list and needs no
member of its own — the row it would declare is `Arithmetic`'s, so it binds through `BindExpr`.

**The funnel's entry points, classified.** A slot is in exactly one row; a new statement operand belongs in one
before it is written.

Each row also declares its **LANE POSTURE** — what `--permissive` does — because r7 is enforced at four sites
and they do not all answer that the same way, and the split is legitimate (kb/Work PB219). The axis is what the
SLOT IS, never which route reached it: an **arithmetic** position carries the documented GnuCOBOL
occurrence-number coercion (strict rejects · `--permissive` warns and computes); an **identifier** slot rejects
in BOTH lanes, because the slot needs an identifier and an occurrence number is not one — there is no coercion
to offer, and `dialect_two_axes` constrains the leniencies this compiler implements rather than requiring one.

| Entry | Rule that governs | Where | r7 lane |
|---|---|---|---|
| ADD/SUBTRACT/MULTIPLY/DIVIDE senders · COMPUTE RHS · CONTINUE AFTER · RETRY · ALLOCATE · START WITH LENGTH · boolean-shift count (bound here, then narrowed to §8.8.2 rule 5's ONE integer operand: Table 4 shape by `ExpressionFormationPass`, integer-ness by `IntrinsicResultType.IsIntegerOperand` — kb/Work PB1413) · CALL BY CONTENT/REFERENCE arithmetic arg | §8.8.1.1 — genuinely `arithmetic-expression-1` | `BindExpr` | arithmetic: warn+coerce |
| SET TO / UP BY / CAPACITY / SIZE · pointer SET UP BY | §8.8.1.1 + r7 window + SR10 | `BindIndexWindowExpr` | exempt (r7 lists them) |
| **COMPOUND** relation / EVALUATE operand (an arithmetic expression: `N < (K + 2)`, `IX + 1 = 4`) | §8.8.1.1 — the index-name or index data item is an operand of the EXPRESSION, while r7 and §8.8.4.2.13 name the relation's own operand; R29 had bound it in the SET window (PB2018) | `BindExpr` | arithmetic: warn+coerce |
| PERFORM VARYING FROM/BY · D18 **subscript** segment | §8.8.1.1 + r7 window; SR10 does NOT list them, so an index **data item** takes axis A (PB215) | `BindIndexNameWindowOperandExpr` / `BindIndexNameWindowExpr` | exempt (r7 lists them) |
| RW VARYING FROM/BY | §13.18.64.2 → §8.8.1.1; on neither index list (PB215) | `BindExpr` | arithmetic: warn+coerce |
| D18 **ref-mod** segment | §8.4.3.3.3 SR4 → §8.8.1.1; **r7 does NOT list a ref-mod position** | `BindExpr` (PB170/PB172) | arithmetic: warn+coerce |
| simple/compound subscript name · ref-mod bound name (the token renderer's fast path) | §8.4.2.3.2 / §8.4.3.3.3 SR4 → §8.8.1.1 | axis A: `ReferenceResolver.ScreenPositionOperandClass`; axis B: `ResolveSubscriptName`'s index arm → `IndexNameInPositionError` (a SEPARATE site — the table used to leave this row's axis B undeclared) | arithmetic: warn+coerce (PB219) |
| sign-condition operand | §8.8.4.7.3 SR1 → §8.8.1.1 | `BindOperandExpr` — reached by a **wrapper WALK**, not `BindExprCore` (PB171) | arithmetic: warn+coerce |
| **SOLE** relation / EVALUATE comparand | §8.8.4.2.1 + §15.2 — **exempt**; the operand is a sending item of its own class | `ComparisonOperandOf` / `BindValueOperand` short-circuits (PB172) — **arm for arm identical**, §14.9.13.4 GR2 makes them one question (PB224) | exempt (r7 lists a relation operand) |
| CALL … USING BY VALUE | §14.9.4.3 SR22 — **exempt** | `BindByValueExpr` + COBOLNET1628 | exempt |
| intrinsic-function argument | the function's §15.x argument rule — **exempt** | `BindFunctionArgumentExpr` + COBOLNET1627 | arithmetic: warn+coerce |
| PERFORM … TIMES count | §14.9.28.3 SR2 — **exempt** | `ControlFlowBinder.CountOperand` | n/a |
| OCCURS DEPENDING ON data-name-1 | §13.18.38.3 SR17 — **exempt** | `DataBinder.Odo` | n/a |
| ref-mod SUBJECT identifier-1 | §8.4.3.3.3 SR1 — **exempt** | `ReferenceResolver.RefModExclusion` | n/a |
| STOP RUN / GOBACK … STATUS | §14.9.42.3 SR2/SR3/SR4 · §14.9.18.3 SR6/SR7/SR8 (SR6 over **identifier-2**) — **exempt**; the format is `{identifier-1 \| literal-1}`, so the screen is keyed on the BOUND SHAPE and reaches both parse arms — a constant-name substitutes literal-1 under §13.10.4 GR1 (PB216). The USAGE alternative asks `ItemCategory.UsageOf`, the ONE §8.5.2.1 usage reader, so an alphanumeric group (*"treated as though it had a usage of display"*) is admitted and a strongly-typed / variable-length one is not (PB411) | `ControlFlowBinder.ScreenStatusOperand` + COBOLNET1704 (PB169/PB216/PB217/PB411) | identifier slot: reject in BOTH lanes |
| arithmetic RESULTANTS | each verb's resultant SR — **exempt**; NOT a fourth copy of the §8.8.1.1 sending question — a resultant turns on an axis §8.8.1.1 does not have (numeric-edited is admitted at GIVING/REMAINDER/COMPUTE and barred at the in-place receivers) | `ExpressionBinder.ScreenResultant` | n/a |
| GO TO … DEPENDING ON identifier-1 | §14.9.17.3 SR1 — **exempt**; "a numeric elementary data item that is an integer" | `OperandPositions.GoToDependingSelector` → `OperandClassScreen` + COBOLNET2324 (PB210) | identifier slot: reject in BOTH lanes |
| SEARCH … VARYING identifier-2 | §14.9.37.3 SR5 — **exempt**; "a data item whose usage is index or a data item that is an integer", and not subscripted by identifier-1's first index-name (`ReferenceResolver.SubscriptNamesIndex`, shared with SR10) | `OperandPositions.SearchVaryingIdentifier` → `OperandClassScreen` + COBOLNET2325 (PB211) | identifier slot: reject in BOTH lanes |
| SET Format-1 receiving identifier-1 | §14.9.39.3 SR1 — **exempt**; "a data item of class index or an integer data item". SR2–SR4 are the sender relation, asked once per statement in `SetBinder.ScreenIndexAssignmentReceiver` | `OperandPositions.SetIndexAssignmentReceiver` → `OperandClassScreen` + COBOLNET2326 (PB212) | SR1/SR2: reject in BOTH lanes · SR3/SR4: `Removed` seam (strict rejects, `--permissive` warns and stores) |
| PERFORM VARYING / AFTER varied identifier | §14.9.28.3 SR2 ("a numeric elementary item") and SR5 a) ("an integer data item" when the FROM phrase holds an index-name) | `OperandPositions.PerformVaryingIdentifier` / `OperandPositions.PerformVaryingIdentifierFromIndex` → `OperandClassScreen` + COBOLNET2120 | identifier slot: reject in BOTH lanes |
| WRITE … ADVANCING identifier-2 / integer-1 | §14.9.51.3 SR14 ("an integer data item") and SR15 ("Integer-1 shall be positive or zero"); keyed on the BOUND shape so a constant-name is integer-1 (§13.10.4 GR1) | `OperandPositions.WriteAdvancingIdentifier` → `OperandClassScreen`; integer-1 beside it in `SequentialIoBinder.ScreenAdvancingCount` — both COBOLNET2365 (PB1023) | reject in BOTH lanes |

**⛔ A CLASS-CLOSED IDENTIFIER POSITION IS A ROW OF `OperandPositions`, NEVER A HAND CHECK AT ITS BINDER**
(`Binding/Procedure/OperandClassScreen.cs`; kb/Work PB210 · PB211 · PB212). Where a statement's syntax rule reads
"identifier-n shall reference a data item of class …", the position is declared once — statement, operand, rule,
the rule's own words, the admitted `OperandClasses` set and the diagnostic — and the binder makes one call.
`OperandClassScreen.ClassesOf` answers the classes an operand is in from the ONE §8.5.2.1 Table-2 classifier
(`IntrinsicArgumentRules.ClassOfItem`) and the ONE §5.5 integer primitive (`PicInfo.IsIntegerDescription`); a
reference-modified operand is none of them (§8.4.3.3.4 GR6 c)), and an undecidable (recovery) class fails OPEN.
GO TO DEPENDING, SEARCH VARYING and the SET receiver were the SAME absence found three times — a bare
`FieldOperand` / `Refs.Resolve` and nothing asked — and the sibling sweep found the PERFORM VARYING induction
variable a fourth. `OperandClassScreenDriftTests` holds every row against every class shape END TO END with the
shapes' classes written from the standard, independently of the classifier, and fails a row that no binder asks,
that has no template, or that this table does not name. A FUNCTION-IDENTIFIER in a row's position is an
identifier (§8.4.3.1.2 Format 1) that references a temporary elementary item (§8.4.3.2.4 GR1), so the screen
classes it by its RESULT — the ONE integer classifier (`IntrinsicResultType.IsIntegerOperand`) and the ONE operand
category reader — rather than refusing it as "not a data item": an integer function meets "an integer data item",
a numeric one is refused under §8.4.3.2.3 SR11 (kb/Work PB1421; the sending rows GO TO DEPENDING and WRITE
ADVANCING parse `functionCall | inlineMethodInvocation | dataReference`, and the drift test holds each against
integer, numeric and alphanumeric functions). Positions whose rule also admits a LITERAL or a function
(PERFORM … TIMES, the STOP/GOBACK status) keep their bound-shape screens above; an arithmetic-expression position
is §8.8.1.1's.

**⛔ The SOLE-vs-COMPOUND boundary is a RULE, not a statement property.** `IF FUNCTION LOWER-CASE(X) = Y` is
legal and `IF FUNCTION LOWER-CASE(X) + 1 = Y` is illegal **in the same statement kind**, because the second
operand is an arithmetic expression and the first is a §15.2 sending item. No per-statement context member can
express that, which is why the comparand positions short-circuit their sole operands (data reference, numeric
literal, non-numeric literal, **function call**) instead of splitting the enum.

**⛔ "Is this operand of class numeric?" has exactly ONE answer**, `IntrinsicArgumentRules.CandidateClasses`
(the §8.5.2.1 Table-2 classifier) via `IsArithmeticOperandClass`. It previously had four — a private category
switch missing the index arm, a `ResultCategory` test that folded §15.2 item 6 index functions into numeric, the
receiving-side `ScreenResultant` (which DID have the index arm), and this classifier, which nothing in the funnel
used. The extraction is what closes the index-data-item, pointer and object holes without a new rule, and what
preserves the 2026-08-02 numeric-edited owner decision by construction rather than by a hand-written arm.

### 3.7 Statement PRE-ops and the ONE sending-value materialization

Several general rules say a value is computed **once, before the statement's effect**, and then used several
times. The bound tree expresses that with a statement-scoped **pre-op**: a binder appends a `BoundStatement` to
the shared `DataBinder.PendingPreOps`, and the `StatementBinder.BindStatement` chokepoint drains its own suffix
into a `BoundSequence` placed ahead of the carrying statement (mark on entry, drain the suffix — the same
protocol a hoisted user-function activation and a §15.4 function-bearing subscript temp already use). Registration
ORDER is evaluation order, so a nested materialization precedes its consumer without extra wiring.

**`Binding/Procedure/SendingValueTemp.cs` is the ONE materializer**, and the rule it implements is written in two
verbs' general rules:

- **§14.9.25.4 GR1 (MOVE)** — "If identifier-1 is reference-modified, subscripted, or is a function-identifier,
  the reference modifier, subscript, or function-identifier is evaluated only once, immediately before data is
  moved to the first of the receiving operands", stated again as an equivalence: `MOVE a (b) TO b, c (b)` ≡
  `MOVE a (b) TO temp` / `MOVE temp TO b` / `MOVE temp to c (b)`, "where 'temp' is an intermediate result item
  provided by the implementor".
- **§14.9.13.4 GR3 (EVALUATE)** — "At the beginning of the execution of the EVALUATE statement, each selection
  subject is evaluated and assigned a value, a range of values, or a truth value."

The materializer creates that intermediate result item through `DataBinder.CreateCompilerTemp` (the ONE
synthesized-temp constructor) with the **operand's own description**, so the store into it is an identity move
and the equivalence is exact: a data item clones its description (§8.4.3.3.4 GR6's "unique data item" when
reference-modified, carried by a run-time-length item per §8.5.1.10.4), and a function-identifier takes §15.4's
"temporary elementary data item" — the same `FunctionValuePic` the §15.4 subscript temp uses, written once, or
`IntegerFunctionValuePic` (scale 0) for an INTEGER function (§15.2 item 5). A NUMERIC function's temporary is flagged
`DataItem.IsFunctionReturnedValue`, and `OperandText`'s field image renders a flagged item in DOC-A.1-92's literal
form — so `MOVE temp TO b` stores in a CHARACTER receiver what `MOVE a TO b` stores (kb/Work PB1007: the
unflagged 30-digit description moved as zero-padded digits, `MOVE FUNCTION INTEGER(N) TO A B` storing `0000`).
A literal or figurative constant is NOT materialized: §8.3.3.6.4 GR2 sizes it from the RECEIVER, so it has no
description of its own. **The identity store is the copy the operand's CLASS has** (`SendingValueTemp.IdentityStore`):
a MOVE for every item MOVE can store, and the SET that names the copy for the classes it cannot — an index data item
(§14.9.39 GR2 b), kb/Work PB1661), a data-pointer, function-pointer or program-pointer (§14.9.39 Formats 7, 8, 9)
and an object reference (Format 5). A MOVE into a pointer clone stored nothing, so a pointer EVALUATE subject read by two WHEN
arms compared a null intermediate (kb/Work PB1060's sweep). An address-identifier is never materialized: it is a
unique item CREATED at the reference (§8.4.3.11.4 GR1), with no storage an earlier store could change.

**A group whose length is decided at run time freezes its EXTENT as well as its value.** A cloned DESCRIPTION has
a compile-time length, so an intermediate cloned from an `OCCURS DEPENDING` group would hold the group's MAXIMUM
extent rather than its current one, and that is observable: with a 3-of-5 occurs-depending group into a
`PIC X(5) JUSTIFIED` receiver, §13.18.38.4 GR8 a)'s current extent right-aligns to `"  125"` while a
maximum-extent intermediate gives `"125  "`. `SendingValueTemp.FreezeOdoExtent` therefore creates a SECOND temp
holding data-name-1's value, stored as a pre-op ahead of the group store, with the clone's `OccursSpec.Depending`
pointed at it (inside `CreateCompilerTemp`, because `DataItem.OccursSpec` is init-only). That is GR1's other
sentence — "The length of the data item referenced by identifier-1 is evaluated only once, immediately before the
data is moved to the first of the receiving operands" — and without it `MOVE ODO-G TO N, Z` re-read the length
AFTER storing into `N` and sent one character to the second receiver. A DYNAMIC-LENGTH elementary sender is
frozen by §8.5.1.10.4's own carrier.

**The intermediate's CAPACITY is counted in §8.4.3.3.4 GR5 positions, not in characters.** GR5 a) fixes the unit —
"If the usage of identifier-1 is bit, positions used in evaluation are bit positions; otherwise, positions used
in evaluation are character positions" — and `DataItem.ImageWidth` answers a different question, the item's
OCCUPANCY: a `PIC 1(8) USAGE BIT` item occupies ONE character (§8.5.1.6.3 packs eight boolean positions into a
byte) where GR5 counts EIGHT positions, and a DYNAMIC LENGTH item occupies none statically. `RefModPlace.PositionCount`
is the ONE reader of that count; the bit and national arms go through `DataItem.OperandPic` so §13.18.29.4
GR1 b)/GR2 b)'s as-if PICTURE answers for a bit group and a national group. Measured before it existed:
`MOVE BE(1:4) TO B1 B2` kept only the leading bit.

**GR1's ZERO-LENGTH-ITEM clause changes the move's KIND, so it is not a group-path concern.** "If identifier-1 is
a zero-length item, it is as if literal-1 were specified as a zero-length literal" substitutes a LITERAL, and
GR4's first sentence — "Any move in which the sending operand is either a literal or an elementary item and the
receiving item is an elementary item is an elementary move" — then makes the statement elementary. Whether the
sender IS zero-length is a run-time state, so `MoveClassifier.ZeroLengthItemRoute` decides when a test is owed
and `MoveEmitter` emits it with the statement's OWN store (`EmitStore`) in the else arm. The predicate for a
GROUP sender is `DataItem.MinimumLengthIsZero` — §8.5.4's stem ("a data item … whose minimum length is zero and
whose length at runtime is zero") as ONE recurrence over the non-redefining children at their MINIMUM occurrence
count, never a copy of the clause's four group bullets. Which of those four is reachable is §14.9.25.3 SR9's
business: a variable-length group may move only to or from a compatible group, so items 5 and 7 are refused at
compile time and item 1 — the occurs-depending group with integer-1 zero — is the shape that gets here.

**The route's RECEIVER set is GR2/GR3's, asked through one predicate** (kb/Work PB943).
`MoveClassifier.SubstitutesForZeroLength` — "the receiving operand is other than a dynamic-length elementary
item" — is asked by all three arms of the substitution: `Sender` (the written literal), `ZeroLengthItemRoute`
(the item) and `NeedsLengthFreeze` (the freeze that serves it). The item arms used to carry a NUMERIC /
NUMERIC-EDITED category filter, excused as a proof that every other category stores the same thing either way;
it was true only for an elementary alphanumeric or national sender into an unedited receiver. A zero-length
GROUP sender is a GR4 group move (no editing, alphanumeric fill), so an edited receiver lost its insertion
characters and a boolean one took alphanumeric spaces; a zero-length BIT group's GR3 ZERO became spaces in a
character receiver. The zero arm is therefore the statement's own dispatch (`MoveEmitter.EmitStore` over
`Kind(figurative, target)`), so whatever SPACE / ZERO means for that receiver — an image fill, an edited fill,
a slice fill, a group fill — is what it stores. ⚠ One DETERMINATION: when both operands are groups and one is a
VARIABLE-LENGTH group, GR9 governs and no test is emitted (SR9 admits the statement only as a compatible-group
move, which a figurative is not). `ZeroLengthLiteralMoveTests.Gr1_ZeroLengthItem_IsTheWrittenLiteral_AtEveryReceiverOfGr2sSet`
is the drift test: each zero-length sender shape against every receiver category Table 16 admits for it, the
item and the matching zero-length literal into twin receivers, the two images required equal.

**A group SENDER's value has one reader, and a group move's receiving AREA is in storage units** (kb/Work
PB943/PB944). `PlaceRenderer.SendingGroupValue` is the §13.18.29.4 dispatch — a bit group sends its boolean
positions, a national group its national positions, an alphanumeric group its storage image — and every
consumer asks it (`OperandText.FieldAsString`, `NumericRenderer.FieldNumCore`, the zero-length test); the numeric
decoder used to read every group through the image alone, so a national group into `PIC 9(3)` decoded UTF-16BE
bytes as digits. GR4's group move into an ELEMENTARY receiver (`MoveEmitter.EmitGroupToElementaryMove`) fits the
sending image to the receiver's STORAGE width (`DataItem.ByteWidth`) and decodes the receiver's representation
from it — a USAGE NATIONAL receiver's two bytes per position (D-N1), a USAGE BIT receiver's packed bits, a native
numeric receiver's zoned / radix-2 / BCD / IEEE bytes in its OWN byte form — because GR4 forbids "conversion of data
from one form of internal representation to another"; the sending characters are never re-encoded into it. The
decode is `ReceivingStore.StorageArea`, the receiving inverse of `OperandText.AsStorageImage`, shared with the
READ / RETURN store into an elementary record area (kb/Work PB1556). And the §13.18.38.4 GR8 current extent (`OdoModel.WrapGroup`) computes its
per-occurrence stride on the same PHYSICAL basis as the group total it is subtracted from
(`RecordLayout.PhysicalOccurrenceWidth`); a character-width stride against a byte-width total made a zero-count
national table send half its maximum image.

**Who calls it, and when.** `MoveBinder.BindMoveOf` materializes the sender when there is more than one
receiving operand; `EvaluateBinder`'s per-subject `SubjectSlot` materializes a value subject when more than one
selection pair reads it (a THRU object reads it twice — GR4 a) 5.). At ONE use the single render already IS one
evaluation and the intermediate is unobservable, so it is not created — MOVE and EVALUATE are hot verbs.

**The emitter half is `EmitContext.SendOnce`** — the same rule where a C# local suffices because the receivers
do not CONVERT from the value: the SET address formats (§14.9.39.4 GR12/GR14/GR16/GR18, "stored in each data item
referenced by identifier-N in the order specified"). The arithmetic family's `ArithmeticEmitter.Snapshot` is the
third face of the rule, for §14.7.7 GR4's one initial evaluation. Three carriers, one rule, each named for the
clause it serves; `SendingValueOnceDriftTests` measures all of them together with a re-seeded `FUNCTION RANDOM`
(§15.75.3 r3 / §15.75.4 r2) and pins the `BoundOperand` leaf set the materializer answers for.

### 3.8 The SET statement's general-format selection — one table over the WHOLE receiving list

ISO §14.9.39.2 prints seventeen general formats for one verb, and several of them share a token shape exactly.
A reserved word settles most of them at the grammar (`SET LOCALE`, `SET CONTENT`, `SET … TO TRUE`, `SET SIZE OF`,
`SET ADDRESS OF`, `SET … TO ENTRY`, `SET … TO ADDRESS OF FUNCTION`); what is left is three written shapes
carrying nine formats:

| written shape | grammar rule | formats it can be |
| --- | --- | --- |
| `SET receivers… TO arithmetic-expression` | `setToValueStatement` | 1 · 5 · 7 · 8 · 9 · 14 · 16 |
| `SET receivers… TO {NULL\|SELF\|SUPER\|reference}` | `setObjectReferenceStatement` | 5 · 7 · 8 · 9 |
| `SET receivers… {UP\|DOWN} BY arithmetic-expression` | `setIndexStatement` | 2 · 10 · 14 |

**`Binding/Procedure/Verbs/SetFormatSelection.cs` is the ONE place that choice is made**, and the fact it reads
is that EVERY receiving brace in §14.9.39.2 is written `{ … } …` — one or more operands of one kind. So the
format is a property of the WHOLE receiving list: classify each operand once (`KindOf` — a pure R30 probe, never
a committing resolve), then pick the single table row whose brace admits all of them. When no row does, pick the
NEAREST row — the first, in specificity order, that admits any of them — and the selected format's own syntax
rule refuses the operands it does not admit, by name and in its own words. Nothing at all matching is the
RESIDUAL, `COBOLNET2112`: no printed general format admits this receiving list.

The sender does not participate, with ONE exception the table carries as a column. §14.9.39.2 writes a format's
identity into its receiving brace; the sending brace is then constrained by that format's own rules (SR9 for
Format 5, SR17/SR20/SR21 for the carriers), which is why each carrier binder reports its own rule for a sender
that is not a reference at all. The exception is the case where the receiving brace says nothing: Format 1's
`identifier-1` admits any identifier, so a receiving list can select Format 1 while the SENDER is of a category
Format 1's own sending brace cannot hold — §8.8.1.1 admits only numeric operands in `arithmetic-expression-1`
and SR2 makes `identifier-2` "a data item of class index". A data item of class object, or of category
data-pointer / program-pointer / function-pointer, is admissible in no Format-1 sending position and is named by
exactly one other format's sending brace, so `SelectForTo` picks that format and its own receiving rule refuses
the receiver by name. `SET N4 TO U` used to reach the ARITHMETIC screen and be reported as "'U' … is not a
numeric operand" (§8.8.1.1) — true, and silent about §14.9.39.3 SR8, the rule the program broke. The column is
`Row.SendsOnly`, so a new carrier format is still one row and nothing else, and the drift test pins that no kind
Format 1 CAN send ever appears in it — otherwise the tie-break would take a legal Format-1 statement away from
its own format.

**The sender is an IDENTIFIER, of any of §8.4.3.1.2's formats — `SetSender` / `SetSenders.Classify` (kb/Work PB1399 +
PB1929).** Every sending brace of the To-formats names an identifier (identifier-2, -4, -6, -8, -13) and §8.4.3.1.3
SR1 makes that "any of the formats for an identifier". Three of them are not a data-name reference — a
function-identifier, an inline method invocation, and the keyword-omitted function (§8.4.3.2.3 SR2, which parses as a
subscripted data reference) — and each references a DATA ITEM (§8.4.3.2.1; §8.4.3.4.4 GR1), so the sender's class and
category are the RETURNING item's. The classifier reads the parse shape once and binds a sole such identifier ONCE
(its activation registers a statement pre-op, so a second bind would run a user function twice), returning one
`SetSender`: a `Ref` (a data reference, still to be resolved by the format that takes it), a bound identifier (whose
temporary data item — `TemporaryItem` — is what the carrier formats 5, 7, 8 and 9 read through
`CarrierSenderPlace`/`OoBindSetObjectRef`, and whose CLASS — `IdentifierClass` — decides Format 1: class index is
identifier-2, any other non-numeric class draws §8.8.1.1's screen through `ExpressionBinder.ScreenIdentifierOperand`),
or neither (a literal or an expression, which each format's own rule refuses). The formats whose binder takes the
amount as an expression of its own (14, 16) are decided from the receiving list BEFORE the sender is classified, for the
same single-bind reason. The earlier classifier — `OoExtractBareReference`, "exactly one bare data reference" — answered
null for all three shapes, so every format saw "no identifier" and refused `SET B TO FUNCTION FOBJ(1)` as a literal and
`SET TX TO FUNCTION MAX(IA IB)` as an arithmetic expression: one sniff, one arm of the dispatch fixed (PB456), the other
three shapes never reached.

**The name operand of an address-identifier is one rule in one place (kb/Work PB1417 + PB1452).** §8.4.3.12.3 SR1 and
§8.4.3.13.3 SR1 print the same sentence — "Identifier-1 shall be of category alphanumeric or national" — and
`SetBinder.ScreenAddressName` asks it of the data item, through `ItemCategory.IsAlphanumericOrNational(Place)` (the
`Place` form reads a reference-modified view's §8.4.3.3.4 GR6 category). Both sites used to test the raw
`Item.Pic?.Category`: null for a GROUP (refused as "category (none)"), and equal to alphanumeric for a PIC A item
(accepted). Identifier-1 is any identifier format too, so both grammar rules take `functionCall | inlineMethodInvocation
| dataReference` (`functionAddressIdentifier` is its own rule, as `programAddressIdentifier` is);
`AddressNameOfIdentifier` binds such an operand once and yields its returned temporary (an intrinsic's character result
is materialized by `SendingValueTemp`; one folded to a literal is the compile-time name). The emitter reads every such
name through `PtrEmitter.NameImage`, the operand's character image, because a group's `Read` is its record struct.

**What this replaced, and why a table rather than a chain.** The nine formats each peeked at `receivers[0]` — and,
in the TO direction, at the sender — in a fixed contract order, returning `null` to let the next candidate try.
Three defects follow structurally from a scalar standing in for a set-valued question, and all three were
measured (kb/Work PB449, PB456): the same two operands gave a correct SR23 diagnostic in one order and a run-time
crash in the other; a receiving list no printed format admits (`SET WS-N UP BY 4` over a `PIC 9(4)`) fell through
to Format 1/2 and EXECUTED; and a re-route that declined — Format 5's, whose precondition was "the sender is
exactly one bare data reference" — left nothing behind it, so `SET U TO 5` over an object reference compiled with
zero diagnostics. `SetFormatSelectionDriftTests` pins the invariants a table can have and a chain could not: within
one direction no kind is admitted by two rows, every classifiable kind has a row, a receiving list and its
reverse select the same format, and no sender kind names two formats or names one Format 1 could have sent.

**A predefined object reference is classified before the general lookup.** `EXCEPTION-OBJECT` is spelled as an
ordinary word rather than a reserved token, so it arrives as a written data reference that no data description
entry declares — and the general resolver therefore answered "'EXCEPTION-OBJECT' is not defined", false about a
name the standard declares (§8.4.3.6.3 SR2: "implicitly described as class object and category object
reference"). `KindOf` asks `OoBinder.OoIsExceptionObject` first, which is the ONE place that spelling is
compared, so the receiving list selects Format 5 and §8.4.3.6.3 SR1 — "EXCEPTION-OBJECT shall not be specified
as a receiving operand" — is the rule the statement draws. Its three siblings need no arm: NULL, SELF and SUPER
are grammar tokens (`objectReference`), so §8.4.3.7.3 SR1 and §8.4.3.8.3 SR2 are enforced by the syntax in a
receiving position.

**Format 1's row is deliberately the wide one and deliberately last.** Its brace is `{ index-name-1 |
identifier-1 } …`, and SR1's "a data item of class index or an integer data item" is a CATEGORY screen over that
brace, not a selection question — enforcing it here would make a missing screen look like a missing FORMAT.
That screen is the `OperandPositions.SetIndexAssignmentReceiver` row, asked by `BindSetTo` once Format 1 is
selected (kb/Work PB212), and SR2–SR4 are ONE receiver-class × sender-alternative table beside it
(`ScreenIndexAssignmentReceiver`): the sender is classified once as arithmetic-expression-1, index-name-2 or an
index-data-item identifier-2 — a bare numeric identifier IS arithmetic-expression-1 (the rendered figure prints
three alternatives, so SR2 governs the identifier-2 alternative and refuses only a bare identifier that is neither
of class index nor numeric) — and §14.9.39.4 GR2's two defined pairings are the only ones admitted. Format 2 has no such catch-all: its brace is `{ index-name-3 }
…` with no identifier alternative and no syntax rule, and §14.9.39.4 GR4 is written "For each occurrence of
index-name-3", so the UP/DOWN direction admits an index-name, a data-pointer (SR23) and a capacity register
(SR29) and nothing else.

**The literal amount is the same shape one level down.** §14.9.39.2 writes each amount-taking format as a choice
between a literal and an arithmetic expression, with a SYNTAX rule on the literal (SR30 for Format 14's
integer-1, SR34 for Format 16's integer-2) beside a GENERAL rule on the expression (GR29/GR30, GR37/GR38).
`SetLiteralAmount` is the one screen: it answers "is this operand a single integer literal" through
`SoleOperand.NumericLiteral` — THE §8.3.3.3.2 rule-2 contiguity reading, shared with DEFINE, CONSTANT and
EVALUATE — and compares it against a `SetAmountBound` the call site declares, so each format names its own
operand and its own rule (`COBOLNET2113`) and the next amount-taking format is a bound, not a fourth `if`.

### 3.9 The typed position carrier — subscripts and reference-modification positions (D10's second half, kb/Work PB2151)

**The rule: a position is a typed node from the parse to the code generator.** A subscript (ISO §8.4.2.3.2
`arithmetic-expression-1`), a reference-modifier leftmost position or length (§8.4.3.3.3 SR4), and every offset and
ordinal the place model computes from them is a `Position` (`Binding/Model/Position.cs`). The binder builds STRUCTURE;
`CodeGen/Roslyn/PositionRenderer.Render` is the one place a position becomes C# (the §3.3 rule "Place stays the lvalue
abstraction but stops being a string", applied to the positions inside a place). No position travels between the binder
and codegen as text, and `PositionCarrierDriftTests` keeps it so.

**Why (DEVLOG, PB2151).** Until PB2151 the binder rendered each position to a C# string at bind time, re-parsed the
positions it could not render through a second, isolated grammar entry, and spliced the strings into seven string
carriers of the place model that codegen re-read as text: byte offsets were computed as string arithmetic
(`0 + (Occ(I) + 1 - 1) * 3 + 1`), the evaluate-once identification decided "is this a constant" by scanning characters,
and the bit-alignment proof of §14.9.4.3 SR6 re-parsed the text with a hand-written evaluator.

#### 3.9.1 The node shapes (`Binding/Model/Position.cs`)

```csharp
public abstract record Position;                                    // an integer-valued run-time address term
public sealed record PositionConstant(long Value) : Position;       // an integer literal, an integer constant-name
public sealed record PositionIndexCell(string Cell) : Position;     // an index-name: its occurrence cell (IndexDeclaration.Cell)
public sealed record PositionItemRead(AccessPath Path, DataItem Item, PositionReadForm Form) : Position;
        // a data item read as an ordinal position through its own profile (CobolTable.Occ / CobolString.RefModPosition)
public sealed record PositionLocal(string CsName, bool AsLong = false) : Position;   // a statement local: a hoisted
        // identification, a report VARYING counter, an INITIALIZE loop variable, a CORRESPONDING view anchor
public sealed record LocalVector(string CsName);                    // a table(ALL) enumeration's index vector (ISO §15.3)
public sealed record PositionLocalElement(LocalVector Vector, int Index) : Position;   // one ALL level's occurrence
public sealed record PositionBinary(Position Left, PositionOperator Op, Position Right) : Position;   // + - *
public sealed record PositionNegate(Position Operand) : Position;
public sealed record PositionGroup(Position Inner) : Position;     // a WRITTEN parenthesis, kept so the order is the source's
public sealed record PositionPointerOffset(string PointerField) : Position;   // CobolPtr.OffsetOf(pointer), a BASED displacement
public sealed record PositionPointerDynBase(string PointerField) : Position;   // CobolPtr.DynBaseOf(pointer), its ordinal twin
public sealed record PositionOffset(Position Origin, IReadOnlyList<OffsetTerm> Terms) : Position;
public readonly record struct OffsetTerm(Position Index, long Stride);   // + (Index - 1) * Stride
```

`PositionOffset` is the ONE structured form of every address the place model computes from subscripts: an origin plus
Σ (index − 1) × stride, outermost level first. A Tier-B view's byte offset (`ClassOffset + Σ (index − 1) × level
width`, ISO §13.18.44), its bit twin (`8 × (displacement) + ClassBitOffset + Σ (index − 1) × stride bits`), a cell
component ordinal (`ClassDynOrdinal + Σ (index − 1) × components per occurrence`) and the ADDRESS OF displacement are all
this node. The ORIGIN is any position: the static in-class offset or ordinal (a `PositionConstant`), that constant
displaced by a BASED pointer (`PositionPointerOffset + c`), or an enclosing window's own offset — so a member window a
cursor builds from a group window is the group's offset displaced again, `PositionOffset(group offset [+ delta], terms)`.
A component ordinal in a class laid over storage through its data-address pointer (a BASED item, an AREA formal, kb/Work
PB2094) has the origin `c + PositionPointerDynBase` (`ReferenceResolver.OrdinalOrigin` over `CellOrdinalBase`), carried
by the window walk's scope and by a whole-table cursor until a dynamic-capacity table's element cell opens its own scope.
(The first design wrote the origin as a fixed `Displacement` plus a `long Constant`; the cursors' and CORRESPONDING's
origins — `k + (o) − e`, `L − e + c` — are not of that shape, and a general origin renders every one of them exactly.)

| carrier | field(s) | type |
|---|---|---|
| `AccessPath` table segments | `FixedTableSegment.OneBasedIndex`, `DynTableSegment.OneBasedIndex` | `Position` (a subscript) |
| | `CellTableSegment.Ordinal` | `Position` (a `PositionOffset` component ordinal) |
| `RedefViewPlace` | `Offset`, `DynOrdinal` | `Position` (a `PositionOffset`, or a hoisted `PositionLocal`) |
| `BitWindow` | `Offset`, `ClassRelative` | `Position` (`PositionOffset` in bits) |
| `DynSlotWindow` / `VarGroupWindow` | `Ordinal` / `DynBase` | `Position` |
| `TableAllPlace` | `Indices` | `LocalVector` (its elements are the ALL subscripts) |
| `RefModSpec` / `RefModPlace` | `Start`, `Length` | `Position` |
| bound nodes | `BoundAddressOf.OccursDisplacement`; `CorrespondingHoist.ViewOffset`; `InitializeOccurrenceSelect.Occurrences` | `OffsetTerm` list; `Position`; `Position` list |

**What a position MAY be is not decided by the node set.** ISO §8.4.2.3.2 makes a subscript `ALL`,
`arithmetic-expression-1` or `index-name-1 [{+|-} integer-1]`, and §8.4.3.3.3 SR4 makes both reference-modifier
positions arithmetic expressions. The node set is the set of shapes that RENDER DIRECTLY as an ordinal; every other
arithmetic expression (a function-identifier, `/`, `**`, a decimal literal, a scaled operand inside a compound, a
floating-point or group operand, the figurative ZERO) is bound by the ONE `ExpressionBinder.BindExpr` into the §15.4
position temporary (`BoundPositionValue`, kb/Work PB1890) and is then a `PositionItemRead` of that temporary. The
integrality rule of §8.4.2.3.4 GR1 b) is therefore asked once, of the result.

Two structural questions are answered by the node, never by text: `Position.ConstantValue` (the compile-time value of
a position built only from constants, an offset included — what the §14.9.4.3 SR6 / SR8 bit-alignment proof asks of
`BitWindow.ClassRelative`) and `Position.IsSelfEvaluating` (a non-negative constant, or an offset with no term over
one — what the evaluate-once identification asks before it hoists).

#### 3.9.2 The binder: the position walk

`ReferenceResolver.BindPosition(node, kind, indexNames)` walks the `functionArgument` PARSE NODE — never its tokens
— through the arithmetic grammar (`additiveExpression` → `multiplicativeExpression` → `powerExpression` →
`unaryExpression` → `primaryExpression`):

- an integer literal → `PositionConstant`; a written parenthesis → `PositionGroup`; `+ - *` → `PositionBinary`;
  a unary sign → `PositionNegate`;
- a bare (possibly qualified) name → the existing resolution: an index-name → `PositionIndexCell` (§8.4.2.3.3 SR4's
  collector, §13.18.38.3 r7's ref-mod screen); an integer constant-name → `PositionConstant`; a report VARYING counter
  → `PositionLocal(AsLong)`; a data item with a position overload → `PositionItemRead`, with the §8.8.1.1 class screen
  queued to the commit point exactly as PB220 required;
- anything else, at any depth, routes the WHOLE position to the D18 temporary: the `MaterializeSegment` hook
  (`StatementBinder.BindPositionTemporary`) takes the position's own `arithmeticExpression` node and binds it through
  the expression binder. A `functionArgument` that is not an `arithmeticExpression` (a nonnumeric literal, a boolean
  expression, `OMITTED`, an address identifier, a phrase word) and the word ALL outside §8.4.2.3.3 SR6's two contexts
  never reach the hook: the resolver refuses them by name — COBOLNET2363 (kb/Work PB1030).

The PB136 declaration-informed cut (Annex D.3.5.3 `DOG (XCOUNTER (- YCOUNTER))`: a name that cannot be subscripted
gives its paren to a NEW subscript) is applied to the tree (`CollectCuts`): the resolver records the cut
`dataReference` nodes, reads a cut reference without its subscript list, and the cut list's items become the
following subscripts, each a `PositionGroup` of its item. A cut list that does not close its subscript item
(`T (A + X (- Y) + 1)`, where the tree joins `+ 1` to `X (- Y)`) has no subscript reading the tree can give; it is
refused by name (COBOLNET2363).

The place model's producers build the offsets as nodes: `ReferenceResolver.WindowScopeOf` (the ONE walk of a Tier-B
reference's levels, its byte, bit and ordinal terms side by side), the ADDRESS OF displacement, the `ViewCursor` that
INITIALIZE and the strongly-typed group comparison walk (`Base`, `OccursTerms`, `OccursBitTerms`, `EntryOrdinal`,
`OrdinalTerms`), the CORRESPONDING view access, and the cell walk of `IntrinsicBinder`'s length composer. Every view
window is built by `RedefViewPlace.For` from a byte `Position`, a runtime displacement `Position?` and the bit terms.

#### 3.9.3 The code generator renders positions; identification hoists nodes

`CodeGen/Roslyn/PositionRenderer.Render(Position)` is the ONE renderer: a constant renders its value, an index cell
its cell, an item read `CobolTable.Occ(path, profile)` / `CobolString.RefModPosition(path, profile)` /
`CobolTable.Occ(path)` (the path through `PlaceRenderer.RenderPath`), a binary node `l op r`, a group `(x)`, a pointer
offset `CobolPtr.OffsetOf(p)` (`RuntimeApi.PtrOffsetOf`), an offset `origin + (i - 1) * s …` term by term. A term over a
constant index is rendered as written, not folded, so the emitted C# of a behaviour-neutral restructuring stays
identical. `PlaceRenderer`, `RuntimeApi.RefModStart` / `RefModLength`, `CallEmitter`'s argument area, `PtrEmitter`,
`CorrespondingEmitter` and `InitializeEmitter` call it wherever they read a position field. A formal's *-ARG-OMITTED
root guard renders in `PlaceRenderer.GuardedRoot`, beside the one path renderer that emits a root.

`PlaceIdentification.Freeze` hoists NODES: `Hoist(p) => p.IsSelfEvaluating ? p : new PositionLocal(hoist(Render(p)))`,
over every position of the place model's own walk (`FreezePath`, `FreezeView`, `FreezeCoding`, `FreezeRefMod`): the
table indices, the cell ordinals, the view offset and ordinal, the ref-mod bounds. A `BitWindow`'s offset is not
hoisted, because no verb that identifies its operands there can receive a USAGE BIT item with a run-time subscript: a
BY REFERENCE bit argument's subscripts are numeric literals or all-literal expressions (§14.9.4.3 SR6, §14.9.23.3
SR12), and INSPECT, STRING and UNSTRING take only usage display or national operands (§14.9.22.3 SR1, §14.9.43.3 SR1,
§14.9.48.3 SR4).

#### 3.9.4 How it landed — one carrier per commit, the oracle after each

Each step kept the emitted C# and the diagnostic stream identical (`scripts/arch/compare_oracle.py`) or explained every
difference by class: (1) the position source — `Position` and the position walk replaced the bind-time text renderer
and the D18 text re-parse (two explained classes: an integer subscript written `+N` or with leading zeros renders as its
value; a D18 temporary is named for its real source line); (2) `RefModSpec` / `RefModPlace`; (3) the `AccessPath` table
indices; (4) the component ordinals; (5) the window offsets and the ADDRESS OF displacement; (6) the table(ALL) index
vector and INITIALIZE's occurrence keys; (7) the close — the binder's last render call deleted, the text evaluator of
the bit-alignment proof and the string hoist deleted, and `PositionCarrierDriftTests`: every `string` property of a
`Place`, `AccessSegment`, `WindowCoding` or `Position` record is an adjudicated C# NAME, no binder source references
`PositionRenderer`, and none of the deleted text machinery is named anywhere under `src/`. Steps 3 to 7: IDENTICAL.


---

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `Binding/Pipeline/IBindPass.cs`, `BindPipeline.cs`, `BindManifest.cs`, `Capability.cs`, `BindContext.cs` | Explicit, DAG-validated pass ordering; the structural cure for §1.1 |
| split | `DataBinder.BindResolve` (`DataBinder.cs:210-258`) | ~15 `IBindPass` classes (`ExpandTypesPass`, `RedefinesClassifyPass`, …) | Each pass declares Requires/Produces; ordering asserted at startup |
| move | binder half of `CSharpEmitter.CallEmitRunUnit` (`CSharpEmitter.Call.cs:88-147`) | `ProcedureBindPass` + `StorageFormPass` + `ExternalGlobalPass` in `BindPipeline` | Extract the hidden second pipeline out of CodeGen into the real Binder phase |
| create | — | `Binding/Model/BoundCompilation.cs`, `BoundUnit.cs`, `DataModel.cs` | Immutable result the emitter consumes; ends the mutable-blackboard API |
| create | — | `Binding/Model/SymbolTable.cs` + `SymbolTableBuilder.cs` | One scoped resolver; write only via builder inside passes |
| merge/delete | `LookupData`, `LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor` | `SymbolTable.TryResolve*` (scope-aware) | Collapse the lookup quadruple; scoped lookup is the ONLY lookup |
| refactor | `DataBinder` ~30 public mutable dictionaries (`DataBinder.cs:26-77`) | private builder state → sealed into `BoundCompilation`/`SymbolTable` | Close every open write channel; no cross-phase mutation |
| split | `DataBinder` (7 partials, 3.9k LOC) | `FileControlBinder`, `SpecialNamesBinder`, `ReportSectionBinder`, `RedefinesClassifier`, `TypedefExpander`, `LinkageBinder`, `PointerBinder`, `OoMethodDataBinder` | God-class → focused pass collaborators over the model |
| split | `StatementBinder` (23 partials, 9.4k LOC) | `ProcedureTableBuilder`, `ExpressionBinder`, `ConditionBinder`, + per-verb `*Binder` classes | Real class boundaries over `BinderContext`; kill shared-private-state coupling |
| create | — | `Binding/Validation/StatementValidation.cs` | Move inline `data.Edition.Error` SR/edition gates out of the binder |
| create | — | `Binding/Bound/BoundVisitor.g` (source generator) + `IBoundStatementVisitor<T>` etc. in `BoundTree.cs` | Exhaustive dispatch; missing arm = compile error |
| refactor | `BindStatementCore` switch (`StatementBinder.cs:170-231`) | thin `Accept`-dispatching binder table (parse→bound stays a switch; it is the ONE parse-tree seam) | Parse dispatch is inherently a switch; only the BOUND-tree consumers become visitors |
| refactor | `EmitStatement` switch (`CSharpEmitter.cs:347`), `BoundStores.StoreKindOf`, `NumericRenderer`, `ConditionRenderer`, `OperandText` | `IBoundStatementVisitor<T>` implementations | Delete the loud `_ =>` defaults; exhaustiveness by type system |
| add | `BoundMove` (`BoundTree.cs:317`) et al. | `MoveKind` + `StorageForm` fields on the node | MOVE classification computed once in binder; `EmitMove`/`ConvertSource` become pure renderers |
| create | — | `Binding/Pipeline/StorageFormPass.cs` | Single owner of the storage-form decision, after procedure binding |
| delete | `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`), `CompilerTempClones` re-sync, FILE whole-group loop (`DataBinder.cs:238-257`) | folded into `StorageFormPass` | Remove the emitter→binder write-back; one StoreAsImage rule |
| rename | `StatementBinder.Accept.cs`, `CSharpEmitter.Accept.cs` (the ACCEPT *verb*) | `Binding/Procedure/Verbs/AcceptDisplayBinder.cs` + `CodeGen/Verbs/AcceptDisplayEmitter.cs` (the AcceptDisplay* names) | End the Visitor-term collision once a real visitor exists |
| rename | `BoundStores` | `BoundStoreAnalysis` | It is an analysis, not storage |
| create | — | `Common/CobolLiteral.cs` (Decode), `Binding/RecordLayout.cs`, `Binding/PhraseBlocks.cs`, `DataItem.Root` | One canonical helper per job (dedup) |
| move | `ReferenceResolver` subscript readers (`SegmentsOf`/`BindPosition`/`WindowScopeOf` — the hand-rolled token splitter is gone, kb/Work PB2113, and positions are the typed `Position` carrier, §3.9, kb/Work PB2151) | `NameResolver` collaborator | Thin the resolver; it becomes an orchestrator over SymbolTable |
| retire (G8) | `using Core = CobolParserCore; using legacy-root.Generated;` (`StatementBinder.cs:6,11`) | `CobolNet.Frontend.Generated` | Decouple from the legacy generated namespace at cut-over (driver/frontend sibling) |

---

## 5. Migration notes — keeping the battery green throughout

The migration is a sequence of behavior-preserving refactors; the 2028 conformance + 213 unit + NIST-353 legacy
guard stays green at every commit. Order chosen so each step is independently shippable and reversible.

1. **Introduce the pass framework as a no-op wrapper first.** Wrap each existing `BindResolve` method body in an
   `IBindPass` with correct `Requires`/`Produces`, keep the SAME call order, add `BindPipeline.ValidateDag()`.
   Zero behavior change; the manifest now documents the order and the DAG assert catches future reorders. Run the
   full battery. (This alone retires §1.1's silent-reorder risk.)
2. **Extract the binder half of `CallEmitRunUnit` into passes.** Move `ProcedureBindPass`/`ExternalGlobalPass`
   into the pipeline; `CSharpEmitter` calls `BindPipeline.Run` and then only emits. The driver's Phase 2 splits
   into Bind + Emit. Battery green (pure move).
3. **Wrap the mutable collections behind `SymbolTableBuilder`/read-only views WITHOUT changing lookup semantics.**
   Keep `LookupData` etc. as thin shims over `SymbolTable.TryResolve` initially; delete the shims once all callers
   pass a `Scope`. This is the riskiest step for OO shadowing — land it behind the OO conformance goldens and the
   method-scope unit tests; do OO last.
4. **StorageForm pass.** Introduce `StorageFormPass` computing the discriminator; have the emitter READ the new
   fact while `MarkStoreAsImage` still runs, assert they agree across the whole corpus (a temporary
   cross-check), THEN delete `MarkStoreAsImage`. This is the safe way to retire a 7-site mutable flag: prove
   equivalence before deletion. (Ties to the data-model sibling design landing `StorageForm` first.)
5. **Semantic normalization on BoundMove (and peers) + the visitor.** Add `MoveKind` to `BoundMove`, compute it
   in `BindMove`, switch `EmitMove` to read it, delete `ConvertSource`'s re-classification. Then land the source
   generator and convert consumers to `IBoundStatementVisitor<T>` ONE consumer at a time (emitter first, then
   `BoundStoreAnalysis`, then renderers) — each conversion is mechanical and independently testable.
6. **Class-boundary split.** Convert partials to real collaborators over `BinderContext` incrementally, verb by
   verb (the existing per-verb partial files map 1:1 to the new classes). Each verb: extract class, inject
   context, run battery, commit.
7. **Helper dedup + renames** (`CobolLiteral.Decode`, `RecordLayout`, `PhraseBlocks`, `DataItem.Root`,
   `BoundStores`→`BoundStoreAnalysis`, ACCEPT-verb rename) as small independent commits, each battery-green.
8. **G8 namespace retirement** happens with the frontend/driver cut-over, not here.

Rule throughout: **prove-then-delete** for every mutable flag / duplicated computation (compute the new form,
cross-check against the old across the corpus, delete the old). Never delete a mutation site on faith.

---

## 6. Risks

- **R1 — OO method-scope shadowing regression (HIGH).** Collapsing the lookup quadruple into one scoped resolver
  is the single most behavior-sensitive change; §11.7 GR5 sibling-invisibility is subtle. Mitigation: land last,
  behind the OO goldens + method-scope unit tests, with a temporary shim-and-cross-check.
- **R2 — StorageForm equivalence gaps (HIGH).** `StoreAsImage` has accreted special cases (SORT SD records, FILE
  whole-group, compiler temps). A single `StorageFormPass` must reproduce ALL of them. Mitigation: the
  prove-then-delete cross-check in step 4 across the full corpus before removing `MarkStoreAsImage`.
- **R3 — Source-generator build complexity (MEDIUM).** Adds a Roslyn source generator to the build; regen must be
  portable across both OSes (matches the existing ANTLR-regen constraint, `feedback_generated_parser_is_a_build_output`).
  Mitigation: the hand-written `abstract` visitor fallback (§3.3) removes the generator dependency if it proves
  costly.
- **R4 — Pass granularity churn (MEDIUM).** Over-fine passes multiply forest re-walks (the efficiency critique
  notes ~15 passes each re-walk via a fresh `AllItems()`). Mitigation: passes may share a single cached
  `BindContext.AllItems` snapshot; granularity is chosen at the natural capability boundaries above, not finer.
- **R5 — Place-becomes-structured ripples into the emitter (MEDIUM).** Removing C#-string emission from Place/
  binder touches every renderer. Mitigation: this is co-owned with the emitter sibling design; sequence it with
  their `RuntimeApi` façade work so the structured-Place render lands once.
- **R6 — Long-lived migration branch drift (MEDIUM).** The split spans many commits while feature work
  continues. Mitigation: each step is independently shippable to `main` behind the green battery; no long branch.

---

## 7. Open questions for the owner

1. **Source generator vs hand-written abstract visitor** for exhaustive bound-tree dispatch? The generator gives
   the cleanest ergonomics (auto `Accept` + interface) but adds a build-time generator (portability/regen cost,
   like ANTLR). The hand-written `abstract` visitor base achieves the same compile-time exhaustiveness with more
   boilerplate and no generator. Default recommendation: **source generator**, fallback ready.
2. **Capability granularity — enum vs per-pass typed tokens?** The `Capability` enum is simple and greppable but
   a pass could technically read a capability it did not declare (the enum does not enforce field-level access).
   A stricter design keys each late-resolved field behind a phase token type. Recommendation: **enum + the
   watermark accessor gate** (§3.1) — full field-level enforcement is likely over-engineering.
3. **Does the completion-phase gate ship in Release builds?** Debug-only `assert` is free but catches nothing in
   production; a Release guard costs a branch per pass entry. Recommendation: **Debug assert + a one-time Release
   DAG validation at pipeline construction** (the DAG validation is cheap and always on; the per-read watermark
   check is Debug-only).
4. **Scope of the "semantic normalization on the bound tree" (§3.3).** MOVE is the clear case. Should
   INSPECT/STRING/UNSTRING/arithmetic-store also carry fully-classified kinds now, or only where the emitter
   currently re-derives? Recommendation: **only where the emitter re-derives today** (MOVE, storage form, table
   polarity); expand opportunistically, do not pre-classify everything.
5. **Confirm the parse→bound dispatch stays a switch.** `BindStatementCore` is inherently a parse-tree switch
   (ANTLR contexts, no bound node yet to `Accept`). Only BOUND-tree consumers become visitors. Confirm this
   asymmetry is acceptable (it is the correct design — you cannot visit a node that does not exist yet).

---

## 8. Dependencies on sibling design dimensions

- **Data model (`DataItem`/`PicInfo`/`Place`/`StorageForm`):** owns the `StorageForm` discriminator shape and the
  structured (non-string) `Place`. This design REQUIRES those two and owns *where/when* StorageForm is computed
  (`StorageFormPass`) and that Place carries structure not text.
- **Emitter/renderer decomposition:** consumes `BoundCompilation` (read-only) and the exhaustive visitor;
  co-owns `RecordLayout`, `FigurativeConstants`, `CobolLiteral.Decode`, and the structured-Place render.
- **Editions framework:** provides the immutable `EditionInfo` + the single `IDiagnosticSink` that `BindContext`
  carries; `StatementValidation` reports through it.
- **Driver & pipeline shell:** the extracted Binder phase makes the driver's Phase 2 literally Bind then Emit;
  `CheckOnly`/`NoEmit` becomes "stop after `BindPipeline.Run`" (the efficiency critique's LOW item), enabled by
  this design's phase boundary.
