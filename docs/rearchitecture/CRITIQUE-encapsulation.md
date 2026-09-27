# CRITIQUE — Encapsulation / Coupling / God-Classes

> Dimension: **encapsulation, coupling, and god-classes** across the WiseOwl COBOL compiler
> (`src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}`, ~40k LOC). Method: read the real code — the three god
> classes and a sampling of their partials, the data model (`DataItem`, `Place`), the bound-store analysis,
> the emitter orchestrator, and the frontend edition surface. Every finding cites `file:line`.
> Invariants under test: typed-native only; one canonical mechanism per job; NO god classes; ownership +
> immutability (passes mutate through a write handle, downstream consumes immutable views; **the emitter must
> NEVER mutate the binder's model**).

---

## Overall assessment

Encapsulation health is **POOR at the module/class boundary, good at the assembly boundary**. The four-project
split is genuinely clean and one-directional — `Cobol.Net.Frontend` depends only on the ANTLR runtime,
`Cobol.Net.Runtime` is a dependency-free leaf, and `Cobol.Net.Compiler` references
Frontend + Runtime + Roslyn (`src/Cobol.Net.Compiler/Cobol.Net.Compiler.csproj`). There is no assembly cycle
and no runtime→compiler back-reference. That is the one part of the topology the codebase gets right.

Inside `Cobol.Net.Compiler`, however, encapsulation is largely a **fiction produced by `sealed partial class`
file-slicing**. The three dominant types — `StatementBinder` (21 partials), `CSharpEmitter` (15 partials),
`DataBinder` (6 partials) — are each ONE object whose full private mutable state is visible to every partial.
A partial is a *naming-convention firewall, not an encapsulation boundary*: the C# compiler enforces nothing
across the slices, so "how binding works" requires opening ~21 files and every cross-slice coupling is invisible.
This directly violates the "NO god classes" invariant — the split is cosmetic, not structural.

Worse, the two hardest invariants — **immutability + no emitter→model write-back** — are both broken today:

- The **data model is pervasively mutable** and used as a shared blackboard. `DataBinder` exposes ~15 public
  get-only-but-**mutable** collections that ARE the module's entire API; `DataItem` has ~14 public settable
  properties. Multiple layers read *and write* them across passes with no ownership discipline.
- The **emitter mutates the binder's model** — `CSharpEmitter.MarkStoreAsImage` writes `DataItem.StoreAsImage`
  post-bind (`CSharpEmitter.cs:65`), the run-unit orchestrator re-syncs `StoreAsImage` on compiler-temp clones
  (`CSharpEmitter.Call.cs:118`), the OO emitter flips `StoreAsImage` (`CSharpEmitter.Oo.cs:697`), and the
  emitter even writes into `DataBinder.IndexFields` (`CSharpEmitter.Call.cs:319`). This is the single most
  serious invariant breach — the emitter is supposed to be a pure renderer.
- The **bind/emit phase boundary does not exist**. The real middle-end orchestrator lives *inside* the codegen
  class (`CSharpEmitter.Call.cs`, `CallEmitRunUnit`), and the emitter calls binder passes directly
  (`data.BindResolve(...)` at `CSharpEmitter.Oo.cs:64,86,104`). CodeGen drives binding.

`Place` is a well-conceived single lvalue abstraction, but it is **leaky in the emit direction**: `Read()`/
`Write()` return raw C# strings and the subtypes hard-code runtime call text, so the "backend-neutral bound
tree" promise is not kept.

Net: the *value model* (`Place`, `NumX`, `BoundTree` records) shows good taste, but the *process model*
(who owns what, who may write what, when) has no enforced boundaries. The good news is that the encapsulation
defects are structural and mechanical, not algorithmic — they are addressable by real class extraction +
immutability, which is exactly what the rearchitecture roadmap targets (see ROADMAP GAP CHECK).

---

## Findings

### F1 — `sealed partial class` slicing is not decomposition (the three god classes share ALL private state)  — HIGH
**Location:** `src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.cs:18` (+20 partials);
`src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.cs:24` (+14 partials);
`src/Cobol.Net.Compiler/Binding/DataBinder.cs:15` (+5 partials).

**Description.** Each of the three biggest types is one `sealed partial class` scattered across many files. The
partitioning is by *feature* (`.KeyedIo`, `.Sort`, `.Intrinsics`, `.Oo`, …), which reads like decomposition, but
every partial has unrestricted access to every private field of the whole object. There is no interface, no
constructor injection, no read-only contract between the slices — the compiler cannot tell you that
`StatementBinder.Sort` depends on state owned by `StatementBinder.Exceptions`.

**Evidence.** The shared mutable private state is itself *declared across the slices*, proving the object is one
tangled blackboard rather than N cohesive units:
- `StatementBinder.cs:20-24` declares `_paras`, `_paraIndex`, `_sections`, `_paraSection`, `_currentSection`.
- `StatementBinder.Oo.cs:121-122` declares `_currentMethodScope`, `_paraMethod`.
- `StatementBinder.Exceptions.cs:20-28` declares `_turn`, `_programName`, `_currentBindPc`, and a bank of
  `_ecChecked/_ecIoChecked/_ecRaise/…` flags.
- `StatementBinder.Declaratives.cs:18-22` declares `_entryPc`, `_declaratives`, `_declScopedFiles`, …
- `StatementBinder.Udf.cs:49`, `.Accept.cs:33`, `.AlterSwitches.cs:43-46`, `.KeyedIo.cs:90`,
  `.Initialize.cs:50`, `.Corresponding.cs:41` each add *their own* private field to the same object.

`StatementBinder.BindStatement` (`StatementBinder.cs:162-167`) mutates state owned by three different partials in
five lines (`_udfPendingCalls.Count` from `.Udf`, `data.OoPendingPropertyOps.Count` from `DataBinder`,
`EcWrap` from `.Exceptions`) — the coupling the partition pretends to hide. Sizes (from the topology design,
survey-confirmed): `StatementBinder` ≈ 9.4k LOC / 21 files; `CSharpEmitter` ≈ 9k LOC / 15 files; `DataBinder`
≈ 3.9k LOC / 6 files.

**Recommendation.** Convert each partial set into **real collaborator classes constructed over an injected
context** (`BinderContext` / `EmitContext`), so the genuinely-shared state is explicit and everything else
becomes private-to-a-class. Keep a thin dispatch core (`BindStatementCore` / `EmitStatement`) and one class per
verb-family. Ban `sealed partial class X` for size (allow partials only for source-generated halves). This is
roadmap changes 25-28 / 39-43 and PHASE-07.

---

### F2 — The emitter MUTATES the binder's data model (cross-layer write-back) — HIGH  *(worst invariant breach)*
**Location:** `src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.cs:50-68` (`MarkStoreAsImage`, writes at `:65`);
`CSharpEmitter.Call.cs:115-120` (compiler-temp `StoreAsImage` re-sync at `:118`, `MarkStoreAsImage` calls at
`:119-120`); `CSharpEmitter.Oo.cs:697` (`native.StoreAsImage = true`); `CSharpEmitter.Call.cs:319`
(`data.IndexFields.TryAdd(...)`).

**Description.** The hard invariant is "the emitter must NEVER mutate the binder's model." It is violated in four
distinct places. `MarkStoreAsImage` is a *binding-time semantic decision* (ISO §14.9.25.4 MOVE GR4 — which
numeric-DISPLAY leaves must be stored as their character image) that runs **from inside the code generator**,
writing `DataItem.StoreAsImage` after binding is nominally complete. The run-unit orchestrator additionally
re-syncs that flag across `CompilerTempClones`, and the OO emit path flips it again. The emitter even inserts
entries into `DataBinder.IndexFields` (a binder-owned dictionary) while emitting.

**Evidence.**
```
CSharpEmitter.cs:65             child.StoreAsImage = true;                 // emitter writing the data model
CSharpEmitter.Call.cs:118       temp.StoreAsImage = model.StoreAsImage;    // emitter re-syncing the model
CSharpEmitter.Call.cs:119-120   MarkStoreAsImage(cls.Data); … MarkStoreAsImage(unit.Data);
CSharpEmitter.Oo.cs:697         native.StoreAsImage = true;
CSharpEmitter.Call.cs:319       … && data.IndexFields.TryAdd(idxName, field))   // emitter writing a binder dict
```
The flag is a genuine cross-layer channel: it is set in the binder too (`DataBinder.cs:255`,
`StatementBinder.MoveFigurative.cs:123,262`, `DataBinder.Linkage.cs:275`, `DataBinder.Reports.cs:352`) and read
in the emitter's numeric pipeline (`Emit/NumericRenderer.cs:105`, `CSharpEmitter.Sort.cs:225`) — 7+ write sites
across three layers with no single owner. The emitter's own comment concedes the desync hazard
(`CSharpEmitter.Call.cs:111-118`: "StoreAsImage is still mutable while procedure bodies bind … both sides of the
activation boundary must agree on the carrier form").

**Recommendation.** Move the whole-group→image decision into **one `StorageFormPass`** that runs after procedure
binding (where `WholeGroupReferenced` is fully collected), writes an **init-only** `StorageForm` discriminator,
and hands the emitter a read-only view. Delete `MarkStoreAsImage`, the compiler-temp re-sync, and the
emitter-side `IndexFields` write. This is roadmap §2.7 / §3.4 and PHASE-06 Step 3 (exit criterion #2: "no
CodeGen writes into the Binding data model").

---

### F3 — The Bind/Emit phase boundary does not exist; CodeGen drives binding (layer inversion) — HIGH
**Location:** `src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Call.cs:88-194` (`CallEmitRunUnit`, the real
middle-end orchestrator); `CSharpEmitter.Oo.cs:64,86,104` (`data.BindResolve(synthetic)` / `fdata.BindResolve`).

**Description.** The compiler's *actual* pass pipeline — collect units, bind interface/class/program data,
validate overrides, bind bodies, build the UDF table, bind procedures, re-sync `StoreAsImage`, `MarkStoreAsImage`,
compute the EC gate, qualify file connectors — lives **inside the codegen class**, and the emitter reaches back
into the binder to run resolution passes on synthesized parse trees. The driver's phase names (Bind / Emit /
Roslyn) are a fiction; `CSharpEmitter.Emit` binds *and* emits in one call. This is a direct layer inversion:
CodeGen (top) invokes Binding (middle) as a subroutine.

**Evidence.**
```
CSharpEmitter.Call.cs:96-120    var (units, classes) = CallCollectUnits(...); … OoBind*; CallBindUnitData;
                                 CallBindUnitProcedure; StoreAsImage re-sync; MarkStoreAsImage(unit.Data)
CSharpEmitter.Oo.cs:64          data.BindResolve(synthetic);    // emitter calling a binder resolution pass
CSharpEmitter.Oo.cs:86,104      data.BindResolve(synthetic); fdata.BindResolve(fsynthetic);
```
The emitter's read-coupling to the binder is also broad — `grep` of `CodeGen/*.cs` shows the emitter touching
`data.Files`, `data.ByName`, `data.Conditions`, `data.CallSeedUids`, `data.LinkageFormals`,
`data.WholeGroupReferenced`, `data.RepositoryIntrinsics`, `data.PtrBasedBridges`, and ~20 more binder members.

**Recommendation.** Extract the binder half of `CallEmitRunUnit` into a `BinderDriver.Bind(...)` returning an
**immutable `BoundCompilation`**; the emitter consumes it read-only via `EmitBound(comp)`. `CheckOnly` then
halts after Bind. This is roadmap change 39 and PHASE-06 Steps 2/4.

---

### F4 — `DataBinder` is a public-mutable-dictionary blackboard with no ownership discipline — HIGH
**Location:** `src/Cobol.Net.Compiler/Binding/DataBinder.cs:26-108`; plus
`DataBinder.Linkage.cs:65,70` (`CallSuppressedRootFields`, `CallGlobalRoots`).

**Description.** `DataBinder`'s public surface *is* ~15 get-only-but-mutable collections; there is no symbol-table
type, no read-only projection, and no rule about who may write which collection when. Downstream components both
read and write them across pass boundaries, so the "passes mutate through a write handle, downstream consumes
immutable views" invariant is entirely absent.

**Evidence.** The public mutable surface:
```
DataBinder.cs:26   public List<DataItem> Roots { get; }
DataBinder.cs:33   public Dictionary<string, List<DataItem>> ByName { get; }
DataBinder.cs:37   public Dictionary<string, string> IndexFields { get; }
DataBinder.cs:41   public Dictionary<string, List<Condition88>> Conditions { get; }
DataBinder.cs:48   public Dictionary<string, DataItem> CapacityRegisters { get; }
DataBinder.cs:56   public Dictionary<string, DataItem> TypeDecls { get; }
DataBinder.cs:65   public HashSet<DataItem> WholeGroupReferenced { get; }
DataBinder.cs:73   public List<FileModel> Files { get; }
DataBinder.cs:77   public Dictionary<string, FileModel> FilesByName { get; }
DataBinder.cs:88   public OoClassTable? OoClasses { get; set; }        // fully settable
```
Cross-pass, cross-class writers (no ownership):
- `ReferenceResolver.cs:280,303` — the resolver writes `data.WholeGroupReferenced.Add(item)` *during* procedure
  binding.
- `ReferenceResolver.cs:86` — the resolver writes `data.OoPendingPropertyOps.Add(...)`.
- `CSharpEmitter.Call.cs:319` — the **emitter** writes `data.IndexFields.TryAdd(...)` (see F2).
- `DataBinder.cs:88` — `OoClasses` is `public … set` and is assigned by the emitter before `Bind`
  (`CSharpEmitter.Call.cs:308`).

Name lookup is also quadrupled (`LookupData` / `LookupDataInScopeOf` / `TryGetVisibleIndexField` / `IndexFieldFor`
per `DataBinder.Oo.cs`) because OO method scoping is a parallel shadow name-model callers must opt into — a
caller that reaches `ByName`/`IndexFields` directly silently misses §11.7 sibling-invisibility.

**Recommendation.** Replace the ~15 public collections with ONE scope-aware `SymbolTable` written only through a
`SymbolTableBuilder` inside passes and sealed into an immutable projection on `BoundCompilation`; collapse the
lookup quadruple into one `TryResolve(name, scope)`. Roadmap §3.2 / PHASE-06 Steps 5-7.

---

### F5 — `DataItem` is broadly mutable and multi-phase-written (no immutable core) — HIGH
**Location:** `src/Cobol.Net.Compiler/Binding/DataItem.cs` — settable properties at
`:23` (`Uid`), `:29` (`CsName`), `:32` (`Pic`), `:60` (`TypeRefName`), `:64` (`TypeName`), `:68` (`StrongType`),
`:169` (`StoreAsImage`), `:193` (`RedefinesTarget`), `:196` (`Renames`), `:200` (`Class`), `:204` (`IsCanonical`),
`:209` (`IsBased`), `:213` (`ClassOffset`), `:221` (`Parent`).

**Description.** The central data-model node has ~14 public `{ get; set; }` properties written by different post-
build passes and even by the emitter. There is no "declaration-time immutable core + pass-produced init-only
facts" separation — any holder of a `DataItem` can mutate identity (`Uid`), storage form (`StoreAsImage`),
redefines membership (`Class`, `IsCanonical`, `ClassOffset`), and strong-typing (`TypeName`, `StrongType`) at any
time.

**Evidence.** The same field is written from many places with only prose ordering to keep them consistent:
```
DataBinder.cs:304, 852, 949   item.Uid = _uidCounter++;                 // identity assigned in ≥3 spots
DataBinder.cs:1512-1517        anchor.Class = cls; item.Class = cls; item.IsCanonical = false;
DataBinder.cs:1592-1597        item.ClassOffset = off; item.Class = cls; c.IsCanonical = false;
DataBinder.cs:782-783          item.TypeName = typeName; item.StrongType = template.TypedefStrong;
StatementBinder.MoveFigurative.cs:123,262   t.Item.StoreAsImage = true;  // binder writing the model at bind time
CSharpEmitter.cs:65 / .Oo.cs:697            child.StoreAsImage = true;   // emitter writing the model (F2)
```
`DataItem.StrongRoot`/`TypeAnchor`/`IsCharacterImage`/`ImageWidth` (`DataItem.cs:74-109,245-300`) are recursive
computed properties that walk `Parent` on every access, so their correctness silently depends on `Parent`,
`StrongType`, and `Class` having been fully written by the right upstream pass — with nothing asserting it.

**Recommendation.** Split into an immutable declaration core + a pass-written side-table (`StrongTypeModel`,
`RedefinesClass`) set init-only through the pass write handle; make the recursive computed properties into
init-only fields set once by a bottom-up pass; add a completion-phase watermark so reading a late fact before
its producing pass ran is a located compiler error. Roadmap §2.7 / §3.1 / PHASE-06 Step 6.

---

### F6 — `Place` leaks emit-time C# text (the abstraction is not backend-neutral) — MED
**Location:** `src/Cobol.Net.Compiler/Binding/Place.cs:22-25` (`Read()`/`Write()` return `string`);
subtypes hard-coding runtime text at `:45` (`MemberPlace`), `:94-101` (`RedefViewPlace` →
`CobolString.RefMod`/`SpliceInto`), `:160-164` (`NumericImagePlace` → `CobolNum.FormatDisplay`/`StoreDisplay`),
`:185` (`CapacityRegisterPlace` → `{TablePath}.Capacity`), `:58` (`DynTablePlace` carries two precomputed path
strings).

**Description.** `Place` is otherwise exemplary — it is the *single* canonical lvalue abstraction that every verb
routes through (satisfying "one canonical mechanism per job"), and the subtype set (MemberPlace, RefModPlace,
RedefViewPlace, RenamesPlace, NumericImagePlace, DynTablePlace, CapacityRegisterPlace) is a clean taxonomy. But
`Read()`/`Write()` return **raw C# strings** and the subtypes bake in specific runtime method names. That makes
the binding layer a producer of emit-time C#, blurring the bind/emit boundary and making `Place` unusable by the
promised second (CIL) backend — a leaky abstraction in the emit direction.

**Evidence.**
```
Place.cs:24-25   public abstract string Read();  public abstract string Write(string rhs);
Place.cs:100-101 $"{Backing} = CobolString.SpliceInto({Backing}, (int)({OffsetExpr} + 1), {Width}, {rhs});"
Place.cs:160-164 $"CobolNum.FormatDisplay({Inner.Read()}, {Inner.Item.ProfileName})" …
```
The binder assembling emit-time C# strings is a pattern beyond `Place` too (e.g. `Initialize`/`Corresponding`
build `CobolTable.At(...)` / member-path strings at bind time) — the same boundary blur.

**Recommendation.** Make `Place` carry **structure** (root item + `BoundExpr` subscripts + optional ref-mod span)
and move all C#-text rendering into a Roslyn-side `PlaceRenderer` behind an `ICodeGenBackend` seam. Route bare
runtime member names through a typed `RuntimeApi` façade so a runtime rename is a compile error, not a
generated-code failure. Roadmap §2.12 / PHASE-07 Steps 5, 11-12.

---

### F7 — Edition metadata is duplicated across Frontend and Compiler (no shared home) — MED
**Location:** `src/Cobol.Net.Frontend/Parsing/EditionGateHints.cs:35-63` (a `Gate` table with `IntroducedIn` +
ISO citation + `constructs.json` row id, in the **legacy** `CobolSharp.Compiler.Parsing` namespace);
`src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs:17-22` (`DialectLevel` + `is2002/2014/2023`);
`src/Cobol.Net.Compiler/Binding/EditionContext.cs:26-49` (`DialectLevel`, `MaxDigits`).

**Description.** The construct→introduction-edition catalogue exists in two places on two sides of the assembly
boundary. The frontend's `EditionGateHints` hard-codes ~30 `Gate` rows (construct, introducing edition, ISO
citation), which the comment itself says duplicates the canonical `constructs.json` / `ConstructRegistry.Check`
that lives in `Cobol.Net.Compiler` ("one wording, two emit layers", `EditionGateHints.cs:14-18`). The frontend
cannot reference the compiler's registry (would cycle), so the metadata is copied. Two edition scalars
(`CobolParserCoreBase.DialectLevel` int and `EditionContext.DialectLevel` int) model the same concept
independently.

**Evidence.**
```
EditionGateHints.cs:36  new("the ALLOCATE statement", 2002, "ISO §14.9.3", "allocate-2002")     // frontend copy
EditionGateHints.cs:14-18  "the registry row is the canonical metadata; this table only maps a PARSE-time surface"
CobolParserCoreBase.cs:17   public int DialectLevel { get; set; } = 85;                          // frontend edition
EditionContext.cs:26        public sealed class EditionContext(int dialectLevel, …)              // compiler edition
```
This is a *topology* defect (no shared lower layer), not a logic bug — but it means an edition-introduction
change must be made in two assemblies, and drift silently diverges the parse-time and bind-time diagnosis.

**Recommendation.** Extract a leaf `Cobol.Net.Editions` assembly (referenced by both Frontend and Compiler)
holding `constructs.json` + `ConstructRegistry` + `EditionGateHints` + an immutable `EditionInfo`, deleting the
frontend copy. Roadmap §2.1 / §2.11 (new assembly, changes 1, 7, 46-48).

---

### F8 — Stale legacy namespace inside the greenfield Frontend (assembly/namespace mismatch) — LOW
**Location:** `src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs:5` (`namespace
CobolSharp.Compiler.Generated`); `EditionGateHints.cs:6` (`namespace CobolSharp.Compiler.Parsing`); every
Compiler consumer aliases `using Core = CobolParserCore` (e.g. `DataBinder.cs:7`, `StatementBinder.cs:11`,
`CSharpEmitter.cs:11`).

**Description.** The assemblies are renamed `Cobol.Net.*` but the code still emits into `namespace
CobolSharp.Compiler.*` (the pre-PIVOT legacy root) and the ANTLR package is `CobolSharp.Compiler.Generated`. This
is only a cosmetic/understandability coupling, but it forces the `using Core =` alias noise into every file and
keeps a false "reuses the legacy assembly" mental model.

**Evidence.** `namespace CobolSharp.Compiler.Generated;` (`CobolParserCoreBase.cs:5`) inside the
`Cobol.Net.Frontend` project; `using Core = CobolParserCore;` at the top of essentially every binder/emitter file.

**Recommendation.** Complete the `CobolSharp.Compiler.* → CobolNet.*` rename now (single scripted commit, MSBuild
`<AntlrNamespace>` property), decoupled from G8. Roadmap §2.2 (changes 2-3, Wave 0).

---

### Positive notes (what NOT to disturb)
- **Assembly layering is clean and one-directional** (`Cobol.Net.Compiler.csproj`; Runtime is a dependency-free
  leaf). Keep it; add only the `Cobol.Net.Editions` leaf.
- **`Place` is the right *shape*** for a single canonical lvalue mechanism (F6 is about the string leak, not the
  abstraction).
- **`BoundStores.StoreKindOf` (`Binding/Bound/BoundStores.cs:47-183`) is a model of disciplined analysis** — a
  total explicit taxonomy over every `BoundStatement` that returns `null` (stage loud) for an unclassified node
  rather than guessing. This is exactly the read-only, no-side-effect posture the mutating passes lack; it is,
  however, one of the ≥5 hand-maintained bound-tree switches that a source-generated visitor should make
  exhaustive (see ROADMAP GAP CHECK).
- **`EmissionContext` (`CodeGen/Emit/EmitCore.cs:15-80`) is a real injected collaborator spine** — the emitter's
  decomposed renderers (`NumericRenderer`, `ConditionRenderer`, `FieldEmitter`) genuinely cooperate over it,
  which is the pattern the binder should copy. Its four public mutable fields
  (`TargetScale/TargetReal/TargetRounding/InSizeErrorContext`, `EmitCore.cs:60-79`) are the one blemish (a
  write-before-read hazard) to fix with a scoped `ReceiverContext`.

---

## ROADMAP GAP CHECK

I read `DESIGN-module-topology.md`, `DESIGN-binder-bound-tree.md`, `PHASE-06-…md`, and
`PHASE-07-…md`. **The plan is unusually complete and adequately addresses every finding above.** The three
pillars the task named all land: real collaborator classes over an injected context, a `SymbolTable` replacing
the public dictionaries, and an immutable `BoundCompilation`. Concretely:

| Finding | Addressed by | Adequate? |
|---|---|---|
| F1 god-class slicing | topology §2.4/§2.5 + binder-tree §3.5 + PHASE-07 Steps 7-10 (`BinderContext`/`EmitContext`, per-verb classes, ban on `partial class` for size §2.15) | **Yes** |
| F2 emitter→model write-back | topology §2.7/§3.4 + PHASE-06 Step 3 (`StorageFormPass`; delete `MarkStoreAsImage`; exit criterion #2 grep-proves no CodeGen write) | **Yes** |
| F3 no bind/emit boundary | binder-tree §3.1 + PHASE-06 Steps 2/4 (`BinderDriver.Bind → BoundCompilation`, `EmitBound(comp)`, `CheckOnly` halts after Bind) | **Yes** |
| F4 public-dict blackboard + lookup quadruple | binder-tree §3.2 + PHASE-06 Steps 5/7 (`SymbolTable`/`SymbolTableBuilder`, read-only views, one `TryResolve(name, scope)`) | **Yes** |
| F5 mutable `DataItem` | topology §2.7 + binder-tree §3.1 (`StorageForm` init-only, `StrongTypeModel` side-table, watermark gate) + PHASE-06 Step 6 | **Yes** |
| F6 `Place` leaks C# text | topology §2.12 + binder-tree §3.3 + PHASE-07 Steps 5/11-12 (structural `Place`, `PlaceRenderer`, `RuntimeApi`, `ICodeGenBackend`) | **Yes** |
| F7 edition metadata dup | topology §2.1/§2.11 (new `Cobol.Net.Editions` leaf) | **Yes** |
| F8 stale namespace | topology §2.2 (rename now, Wave 0) | **Yes** |

The plan is also *stronger than my critique in two respects*: (a) it adds a **DAG-validated pass manifest +
completion-phase watermark** (binder-tree §3.1), which structurally fixes the implicit-ordering latent-bug class
that underlies F2/F5 — I flagged the mutation, they also fix the *ordering* that makes the mutation fragile; and
(b) it correctly identifies the **source-generated exhaustive visitor** (binder-tree §3.3, PHASE-07) as the fix
for the ≥5 hand-maintained bound-tree switches (`BindStatementCore`, `EmitStatement`, `BoundStores.StoreKindOf`,
`NumericRenderer`, `ConditionRenderer`) — an encapsulation problem I under-weighted (those switches are a
different flavor of the "coupling invisible to the compiler" defect). The migration discipline
(prove-then-delete for every mutable flag, cross-check across the corpus before removing `MarkStoreAsImage`, land
the OO `SymbolTable` collapse last behind goldens) is exactly right for a behavior-preserving refactor.

### Gaps / corrections to the plan

1. **The OO `OoBindCallbacks` seam (PHASE-06 Step 2.4) *reintroduces* an emitter→binder inversion — track it as
   debt, don't let it settle.** To avoid moving OO code in P6, the plan keeps the OO binding methods physically on
   `CSharpEmitter` and calls them back from `BinderDriver` via a delegate bundle. That is a pragmatic bridge, but
   for the duration of P6→P9 the "no CodeGen drives Bind" invariant (F3) is only *partially* restored — binder
   passes call back into methods that live in the codegen class. This is acknowledged as a P6→P9 seam (PHASE-06
   R3), but it is worth an explicit **exit assertion in P9** that zero binder-invoked methods remain on any
   `CodeGen/` type. Recommend adding to P9 a grep-gate mirroring PHASE-06 exit criterion #2. (The evidence this
   matters: `CSharpEmitter.Oo.cs:64,86,104` call `data.BindResolve` today — those must not survive as callbacks.)

2. **`OoClasses` is publicly settable and assigned by the emitter *before* `Bind` — call it out as its own
   deletion.** `DataBinder.cs:88` (`public OoClassTable? OoClasses { get; set; }`) is set at
   `CSharpEmitter.Call.cs:308`. The roadmap folds `OoClasses` into `BoundCompilation.Classes` (read-only
   projection, binder-tree §3.2) and moves OO to `Oo/` (§2.10), which covers it — but the *pre-Bind settable
   handshake* is a distinct write channel not enumerated in the "close every open write channel" list (§1.2 names
   `WholeGroupReferenced`, `CompilerTempClones`, `StoreAsImage`, but not the `OoClasses` setter). Add it to the
   PHASE-06 Step 5 grep so the sealing is complete.

3. **`ReferenceResolver` writing `WholeGroupReferenced` mid-resolve (F4 evidence) is correctly kept as a
   Bind-phase write, but the design should name its OWNING pass.** PHASE-06 Step 5.2 leaves the write at
   `ReferenceResolver.cs:280,303` in place ("a Bind-phase write and is legitimate") and defers a dedicated
   `UsageCollectionPass` to the data-model track. That is fine, but until that pass exists the resolver still
   mutates a binder collection as a side effect of resolution — a *within-Bind* ownership blur. Recommend
   promoting `UsageCollectionPass` from "data-model track, maybe P5" to a **named required pass in the P6
   manifest** (it is the producer of the `WholeGroupReferenced` fact that `StorageFormPass` requires), so the DAG
   actually encodes the dependency rather than relying on the resolver's incidental writes.

4. **`ProcedureBindPass` "may still run inside `BinderDriver` rather than the manifest" (PHASE-06 Step 3.1) is a
   correctness escape hatch that weakens the watermark guarantee.** If the two most important passes
   (`ProcedureBindPass`, `StorageFormPass`) can run *outside* `BindPipeline.Run`, the watermark gate (Step 6) does
   not cover the exact ordering edge (whole-group-use → storage-form) that F2 is about. Recommend making
   "route through the manifest" a hard requirement of P6 (not a "prefer … else leave a TODO"), since it is the one
   ordering the whole `StoreAsImage` fix depends on.

5. **No finding-level gap on the *value* side.** The plan does not over-reach: it explicitly keeps the
   owner-locked "no shared lowered IR" decision (binder-tree §3.3) and keeps `Place` as the lvalue abstraction
   (structural, not deleted). That restraint is correct — `Place`, `NumX`, and the `Bound*` records are the
   healthy core and should not be re-architected beyond the string-leak fix.

**Verdict on the roadmap:** adequate and, in the ordering/exhaustiveness dimensions, more thorough than this
critique. The four gaps above are refinements (make two implicit seams explicit, add two grep-gates), not missing
pillars.
