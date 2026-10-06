# WiseOwl COBOL — Interprogram (CALL / cross-program data) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §9; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Decision-complete design for cross-program data + calls in WiseOwl COBOL (COBOL→typed-native C#→Roslyn). The load-bearing problem is BY REFERENCE on typed-native fields with NO byte window. Resolution: ONE managed-reference carrier (the typed-native re-implementation of `ManagedPointer`, internally `ManagedRef<T>`) that serves BY REFERENCE args, LINKAGE items, USAGE POINTER, ADDRESS OF, BASED, ALLOCATE, and SET ADDRESS OF — honoring the owner's singular-pattern rule. Crucially the carrier does NOT box WORKING-STORAGE: an ordinary `01 WS-X PIC 9(4)` stays a native `long`; a carrier is built ONLY at a call site as `ManagedRef<long>.OverField(()=>WS_X, v=>WS_X=v)` (an accessor over the caller's native field) so DISPLAY/MOVE/arithmetic keep zero indirection. The calling convention has two layers: a uniform opaque ABI (`ICobolProgram.Call(CobolArgs)`, the typed analog of the rejected `Entry(ManagedPointer[])`) for dynamic/cross-assembly CALL, and a typed fast path (direct `R=_SUB.Run(...)`) for same-assembly, statically-resolvable, PIC-conforming calls. Each program becomes its own (instantiable, not static) C# class; nested programs are nested classes; recursion/functions/methods get a per-activation instance, plain programs a cached singleton for last-used persistence. RETURNING maps to the C# method return value (idiomatic). The only sanctioned transient-byte boundary is category-mismatch BY REFERENCE (PIC X(4) arg viewed as PIC 9(4)); same-category — the common case — is always fully typed. Pointers are always-typed (never byte-gated).

## Decisions

### D1. ONE managed-reference carrier `ManagedRef<T>` (the typed-native re-implementation of `ManagedPointer`) serves BY REFERENCE args, LINKAGE items, USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET ADDRESS OF — with two construction modes (accessor-over-native-field | standalone cell) plus a Null state.

**Rationale.** Owner singular-pattern + managed-pointer rules mandate exactly ONE carrier for 'a managed reference to a storage location'; BY REFERENCE and pointers are the same concept. Accessor-over-field means WORKING-STORAGE stays native (no boxing), so only genuine aliases (LINKAGE / ALLOCATE) carry indirection — preserving the native-field North Star.

**Rejected alternatives.** (a) `CobolRef<T>` as a NEW parallel reference type — rejected: violates singular-pattern (owner would correct it). (b) Make every aliasable item itself a `CobolRef<T>` heap cell — rejected: re-introduces a uniform indirection layer over all storage (the byte-State sin in new clothes; reads through .Value on a heap object). (c) Legacy `ManagedPointer(byte[],offset,length)` — rejected: that IS the abandoned byte substrate; a managed ref can't serialize to stable bytes anyway. (d) C# `ref` parameters everywhere — rejected: `ref` can't be stored in a field (LINKAGE item must persist for the activation), can't be re-pointed by SET ADDRESS OF, and can't cross the opaque dynamic-CALL ABI.

### D2. Two-layer calling convention: a uniform opaque ABI (`interface ICobolProgram { int Call(CobolArgs args); void Cancel(); }`, `CobolArgs` = ordered (PassMode, caller PicMeta, carrier)) for dynamic + cross-assembly CALL; a typed fast path (direct typed method `R=_SUB.Run(carrier...)`) for same-assembly, statically-resolvable, conforming calls.

**Rationale.** The tightest constraint is `CALL identifier` and CALL to a separate `<name>.dll`: the caller can't see the callee's LINKAGE, forcing a uniform entry signature + uniform carrier array (the typed analog of the rejected `Entry(ManagedPointer[])`). Designing the opaque ABI first means the typed fast path is a pure optimization, not a retrofit.

**Rejected alternatives.** Typed ref-param signatures only — rejected: dynamic CALL and cross-assembly resolution would retrofit badly (the callee signature is unknown at the call site). Trailing-scratch-buffer RETURNING (legacy `InvokeNumericFunction`) — rejected: that is the byte ABI; RETURNING → idiomatic C# method return value instead.

### D3. Each program → its own instantiable (non-static) C# class; nested/contained programs → nested classes; the run unit compiles to one assembly with the first program as entry.

**Rationale.** Recursion, functions, and methods need a per-activation instance (§14.6.2.3); a static class cannot recurse. Plain non-recursive programs use a cached singleton instance to realize last-used persistence. Nested classes mirror COBOL nested-program scope (outer GLOBAL/COMMON visible, outer LOCAL-STORAGE not).

**Rejected alternatives.** Today's single `internal static class Program` bound to the first program unit — rejected: cannot host multiple/nested programs and cannot recurse. One assembly per program by default — deferred to an owner flag (separately-compiled `.dll` is supported via the opaque ABI but loses the typed fast path).

**The container's CONFIGURATION SECTION and OPTIONS apply to its containees (§12.3.4 GR1 / §11.9.4 GR1; PB60 / AR-15.67.3-5, 2026-08-17).** A contained program shall not have a configuration section of its own (§12.3.3 SR1 — COBOLNET1643), so it inherits the container's WHOLE configuration-derived state — SPECIAL-NAMES (DECIMAL-POINT IS COMMA, CURRENCY SIGN, CLASS names, ALPHABETs, switch mnemonics/conditions), the OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE, SOURCE-COMPUTER DEBUGGING MODE, and the REPOSITORY specifiers — through the ONE `DataBinder.InheritConfiguration(container)`, called by `BinderDriver.BindUnitData` BEFORE the containee binds (its first literal and PICTURE already see the switches; units bind container-first, so one level carries the ancestry). OPTIONS inherit with clause-by-clause override: `OptionsBinder.Bind(program, edition, baseline)` starts from the container's model. Before this, only the REPOSITORY sets were inherited (after Bind), and a contained program under DECIMAL-POINT IS COMMA parsed `NUMVAL("123,45")` as 0 and `NUMVAL("123.45")` as 123.45 — the exact inversion of §15.67.3 r5, undiagnosed. Pinned by `pb60_nested_configuration_inheritance` (2023: SPECIAL-NAMES + OPTIONS inheritance and an inner OPTIONS override) and `nested_special_names_inheritance` (85: the edition-invariant SPECIAL-NAMES half); the negative `config-section-in-contained-program` carries SR1.

### D4. State→storage matrix (the §13.5.4/§13.6.4/§14.6.2.3 storage-class × unit-kind model): plain program → WS as instance fields on a cached singleton (static data realized by instance caching — the unit can never be concurrently active, §8.6.6, so one cached instance IS the one last-used copy); INITIAL → fresh instance per activation (WS is INITIAL data, §13.5.4 GR2 — instance fields re-initialize by construction); RECURSIVE (and every function/method, §8.6.6) → WS as STATIC C# fields (ONE per-class copy — §13.5.4 GR1 static data, last-used across ALL concurrent/successive activations per §14.6.2.3.3; INDEXED BY cells and Tier-B backings ride the same `StaticRootFields`/`StaticIndexCells` channel as method WS — one mechanism, two producers) over a fresh instance per activation that carries everything per-activation; LOCAL-STORAGE (program/function-level, bound via the ONE `BindEntries(EntrySection.LocalStorage)` path) → instance fields = automatic data (§13.6.4 GR1), in initial state EVERY activation — realized by the fresh instance for INITIAL/RECURSIVE units and by an emitted `Call`-entry re-initialization (the same composed `ValueInitializer` initializers) for cached-singleton units — whose CELL-BACKED roots (a BASED item's implicit `__addr_X` pointer → NULL, §14.6.2.3.2 action 5; an ADDRESS-OF-taken record's `StorageCell` → a fresh cell at its initial image, §8.6.4) and non-formal LINKAGE based entries (§8.6.5: the association ends with the activation) are re-seeded by `OoEmitter.ActivationPointerSeeds`, the ONE seed the method activation uses too (kb/Work PB1132; `ActivationStorageReseedDriftTests`); EXTERNAL → one run-unit holder per externalized name on the `ExternalStore` (not reset by CANCEL, §14.9.5 GR8; never a per-class static); GLOBAL → field on the outer class instance, reached by nested classes through `__outer` ref-bridges; LINKAGE → carrier-bound (never initialized); CANCEL → next CALL finds initial state (§14.9.5 GR3): the instance drops AND — for a RECURSIVE unit — the registered `__ResetStatics` reassigns every static WS field/index cell to its declaration initializer (also invoked at run-unit registration = the §14.6.2.3.2 case-1 trigger, and via an INITIAL container's implicit cancel cascade = case 2).

**Rationale.** Directly implements ISO §13.5.4 (WS = static/initial data), §13.6.4 (LS = automatic data), §14.6.2.3 (initial/last-used: automatic+initial data → initial state every activation; static data → initial only on the three §14.6.2.3.2 triggers; static+external are the ONLY last-used data), §8.6.6 (COMMON/INITIAL/RECURSIVE; functions always recursive), §8.6.7 (EXTERNAL sharing), §14.9.5 (CANCEL). A recursive unit's WS MUST NOT live on the per-activation instance — that would re-initialize it per activation, violating §14.6.2.3.3.

**Rejected alternatives.** All-static fields (legacy) — rejected: cannot represent RECURSIVE per-activation LOCAL-STORAGE/formal copies or INITIAL re-init cleanly. WS-on-the-fresh-instance for RECURSIVE units — rejected: miscompiles shared last-used WS into per-activation initial state (the pre-P10 defect, DEVLOG 864). A per-program ExecutionContext object threading byte StorageBlocks (legacy) — rejected: byte substrate.

**Static storage and contained programs (kb/Work PB1133):** a RECURSIVE (or inherited-recursive, §11.10.4 GR4) program owns static WS and FILE SECTION storage whether or not it contains programs. A containee's GLOBAL bridge reaches a container member through ONE anchor, `BoundUnit.AnchorOf`: the `__outer` instance chain for an instance member, the container's CLASS name for a static one (`DataBinder.IsStaticMember` — the union of the four static channels; C# cannot name a static through an instance, CS0176), and the inherited FILE STATUS places re-anchor per root the same way (`AccessPath.Reroot`). The former stage (0899 `recursive-contained-working-storage`) is gone. ~~BASED / ADDRESS-OF-taken records in a RECURSIVE unit's WS stage 0899 `recursive-working-storage-pointer-backed`~~ — **CLOSED (kb/Work PB154 + PB234):** a BASED root's `__addr_X` bridge emits STATIC and `__ResetStatics` sets it to NULL (§14.6.2.3.2 action 5); an ADDRESS-OF-taken record's `StorageCell` — its one storage, which `ManagedPointer.At` aliases — emits STATIC (`DataBinder.StaticAddressableCells`, §13.5.4 GR1) and `__ResetStatics` re-seeds it IN PLACE with the declaration's own VALUE image and clears its managed slots (`StorageCell.Reinitialize`, action 2), so a pointer taken before a CANCEL still names the unit's one copy; a level-01 REDEFINES of a BASED record rides the based root's static bridge. ~~Recorded residue: per-activation file connectors~~ — **CLOSED (kb/Work PB168):** a RECURSIVE unit's internal file connectors AND FD record areas are unit-scoped last-used state (§8.6.4 — one copy per run unit; §14.6.2.3.2 action 3 re-initializes them only on the static initial-state cases 1–3): the `__filesRegistered` guard emits STATIC (`DataBinder.UnitStaticWs`, the one flag for WS, FD record areas and the registration guard), `__ResetStatics` clears it on exactly those cases, FILE SECTION records route through `RouteStaticUnitStorage`'s per-root routine like WS, report-engine construction rides its own per-INSTANCE `__reportsConstructed` guard, and NOTHING per-activation is installed on the shared connector at all: the LINAGE operand values and the ASSIGN … USING association are arguments of the OPEN/WRITE statements that read them (kb/Work PB673 — §13.18.34 GR6 b) and §12.4.5.3 GR3 b) name the runtime element that EXECUTES the statement, and an unguarded install answers with the last activation to have run its prologue, which after an inner activation RETURNS is dead storage).

### D5. The single sanctioned transient-byte boundary is confined to category-mismatch BY REFERENCE (e.g. a PIC X(4) argument viewed by the callee as PIC 9(4)) and to a data-pointer rebasing a differently-typed BASED item; same-category aliasing is always fully typed.

**Rationale.** `ManagedRef<string>` and `ManagedRef<long>` cannot alias the same typed location, but ISO BY REFERENCE = 'same storage area reinterpreted'. The architecture already permits a transient (never-persisted) byte image at unavoidable boundaries; confining it to category mismatch keeps the overwhelmingly common same-category case fully native.

**Rejected alternatives.** A universal byte window under all BY REFERENCE (legacy) — rejected: byte substrate for the common case. Forbidding category mismatch outright — rejected: it is legal COBOL exercised by NIST.

### D6. The program-prototype registry is ONE per-unit name→`ProgramPrototype` table, built the way the user-function table is built, and it feeds ONE `CalleeSignature` shared with `AS NESTED` (kb/Work PB237).

`REPOSITORY. PROGRAM program-prototype-name-1 [AS literal-3].` (§12.3.8.2's program-specifier; its AS literal is screened by `DataBinder.BindSpecifierExternalizedName`, the ONE screen every REPOSITORY specifier's `[ AS literal-n ]` shares since kb/Work PB974 — the user-defined-function specifier's twin resolution is `BinderDriver.UserFunctionsOf`) is the ONE surface that declares a program-prototype-name, and three constructs take that name as their subject: CALL Format 2's `AS` arm (§14.9.4.3 SR16), CALL Format 2's bare operand (§14.9.4.4 GR3 b) third bullet), and CANCEL's third brace alternative (§14.9.5.3 SR3 / §14.9.5.4 GR1 c)).

**The pipeline, in three stages, each in the place its inputs are.**

1. **Syntax → `DataBinder.ProgramSpecifiers`** (`BindProgramSpecifier`): one entry per written specifier, keyed by prototype name, carrying the externalized name (literal-3, else the name — §12.3.8.4 GR10 NOTE 1). §12.3.8.3 SR1 (repeated specifications shall be identical) and SR2 (literal-3's class) are checked here, where the written text is — SR1 by the ONE `DataBinder.CheckRepositorySpecification`, keyed on the NAME across every specifier kind and comparing the WHOLE specification (kind, AS literal, EXPANDS phrase; an intrinsic-function-name-1 is kind `FUNCTION … INTRINSIC`), kb/Work PB1017. The set inherits into contained units through `InheritConfiguration`, beside its REPOSITORY siblings — §12.3.8.4 GR10 scopes the name to "the containing environment division".
2. **Resolution → `BinderDriver.ProgramPrototypesOf(data, unit, …)`**, against the group tables `BinderDriver.BuildGroupRepository` builds once (`BindSession.Repository`) after every unit's DATA division has bound and before the first procedure body — a CLASS's method bodies included, whose binders get the class REPOSITORY's prototypes through the same call (`unit` null; §12.3.4 GR1, kb/Work PB1100). `BuildProgramDetailsTable` is the IN-GROUP half of §12.3.8.4 GR10, keyed by EXTERNALIZED name (`BoundUnit.ExternalizedName` — see D7): the group's outermost program definitions (GR10 a)) and, behind them, its program PROTOTYPE definitions — §11.10.2 Format 2's `PROGRAM-ID … IS PROTOTYPE` source unit (GR10 b), kb/Work PB894). The two arms have one consequence ("the details are taken from" that unit), so they are ONE table layered in a)-then-b) order rather than two lookups. A specifier whose externalized name hits it resolves WITH a signature; one that misses resolves WITHOUT one, which is GR10 c): "the details are taken from the external repository", and **this implementation's external repository is the run unit's program registry**, consulted at activation (§14.9.4.4 GR3 b), EC-PROGRAM-NOT-FOUND on a miss). §8.4.6.8's second spelling (a containing program definition's program-name, no specifier needed) registers in the same table, which is also how §12.3.8.3 SR15's "this specifier is ignored" is realized — the definition overwrites the specifier's entry.
3. **Reference → `CallBinder`**, through the ONE `ResolvePrototype` lookup that both verbs call, and the ONE `BarePrototypeWord` shape test that decides whether an identifier-shaped operand may be read as a prototype name at all.

**Why `CalleeSignature` is not named for `AS NESTED` any more.** §14.9.4.3 SR25 makes §14.8.2 and §14.8.3 apply to *a Format-2 CALL*, not to AS NESTED specifically. `BindCall`'s argument-count, SR19/SR21 mode-agreement, SR24 OPTIONAL-correspondence, §14.8.2 description-conformance and §14.8.3 returning-conformance checks all read one `callee` / `calleeFormals` pair, so wiring the prototype producer delivered the whole regime at once. The single deliberate exception is the §14.8.2.3.2 restricted-data-pointer check, which stays `asNested`-only: `StrongTypeModel`'s equivalence is name-based within a source element and defers cross-program EXTERNAL equivalence, and a prototype's GR10 a) definition is a separate outermost element.

**Discrimination order, and why.** `CALL X` and `CANCEL X` are spelled identically whether X is identifier-1 or program-prototype-name-1 (the §14.9.4.2 Format 2 bracket encloses the target brace *and* the word `AS`, so it may be omitted whole — PDF page 619). Both binders therefore **probe the data reference first** — `Refs.Probe`, which reports nothing on a miss — and read the word as a prototype only when it is not a data item. That ordering is what makes the feature strictly additive: no program that binds today changes meaning.

**Rejected alternatives.** A second signature record for prototypes — rejected: it would have duplicated seven §14.8.2/§14.8.3 checks and let the two copies drift (the two-arm-dispatch defect shape). Rejecting a prototype whose program is not in the compilation group — rejected: GR10 c) admits exactly that, and it is the ordinary separately-compiled callee.

### D7. A unit carries TWO names, not one: the DECLARED `Name` and the `ExternalizedName` the `AS` phrase gives it. A reference by a LITERAL resolves the externalized one; a reference by a user-defined WORD resolves the declared one (kb/Work PB303).

ISO §8.3.2.2 2) is the whole rule: *"For any externalized user-defined words for which the AS phrase is specified, the content of the literal specified in that AS phrase is a name that is externalized to the operating environment. The implementor defines the formation and mapping rules of these names."* §11.10.4 GR1 keeps the pair distinct in so many words — *"Program-name-1 names the program declared by this program definition. Literal-1, if specified, is the name of the program that is externalized to the operating environment"* — and §11.5.4 GR1, §11.3.4 GR1, §11.6.4 GR1 and §11.7.4 GR1 b) repeat it for FUNCTION-ID, CLASS-ID, INTERFACE-ID and METHOD-ID.

**The dispatch is on the FORM of the reference, and §8.3.2.2 states it NORMATIVELY** — it is not inferred from the individual verbs. *"Externalized names shall be referenced in a source element only: 1) in the AS phrase in a repository paragraph entry, 2) in the AS phrase in an EXTERNAL clause, 3) as program-name in a CALL statement, 4) as program-name in a CANCEL statement, 5) as program-name in a program-address-identifier, 6) as method-name in an INVOKE statement or inline method invocation. All other references to names for which externalization is permitted shall be specified using the user-defined words, as opposed to the externalized names."* Six sites take the externalized name and nothing else does, which is exactly the table below; each verb's own general rule (§14.9.4.4 GR3 b), §14.9.23.4 GR2 a)) then routes through §8.3.2.2 for the mapping:

| Reference | Rule | Resolves against |
|---|---|---|
| `CALL` / `CANCEL` literal-1 or identifier-1 | §14.9.4.4 GR3 b) — "the program-name of the program being called, **as described in 8.3.2.2**" | `ExternalizedName` |
| the program-address-identifier operand — ISO `ADDRESS OF PROGRAM { identifier-1 \| literal-1 \| program-prototype-name-1 }` (§8.4.3.13.2) and the Micro Focus / IBM `TO ENTRY` spelling alike | §8.4.3.13.4 GR1/GR2 — "the outermost program identified by the **externalized** program-name" | `ExternalizedName` |
| the EXTERNAL clause's `AS literal-1` | §13.18.22.4 GR5 — "Literal-1, if specified, is the name of the file connector or record that is **externalized to the operating environment**" | the run-unit `ExternalStore` cell key (`DataItem.ExternalizedAs` / `FileModel.ExternalName`) |
| §12.3.8.4 GR10 a) search of the compilation group | "the externalized name of a program definition" | `ExternalizedName` |
| `INVOKE` literal-1 or identifier-2 | §14.9.23.4 GR2 a) — the same formula; and §14.9.23.2 gives INVOKE **no word form at all** | the method roster key = `ExternalizedName` |
| `END PROGRAM` / `END FUNCTION` / `END CLASS` / `END INTERFACE` / `END METHOD` | §10.7.3 SR2 — "identical to the program-name declared in a preceding PROGRAM-ID paragraph" (SR4–SR9 the same of each other alternative; all asked once, pre-bind, by `Validation.EndMarkerPass` — kb/Work PB988) | the WORD in the ID paragraph (`Name`) |
| `CALL ... AS NESTED` literal-1 | §14.9.4.3 SR15 — "the same as the **program-name** specified in a PROGRAM-ID paragraph" | `Name` (and §11.10.3 SR2 forbids AS on a containee, so the two coincide there) |
| `FUNCTION user-function-name` | §8.4.6.6 / §8.4.6.7 scope the WORD | `Name` |
| an object-class-name / interface-name | §8.4.6.4 scopes the WORD | `Name` |
| `FUNCTION MODULE-NAME` | §15.65.4 r4 is implementor-defined; CONFORMANCE.md DOC-A.1-135 chooses the program-id form | `Name` |

**AND THE SAME CLAUSE MAKES EACH OF THOSE NAMES NAME ONE THING (kb/Work PB660).** §8.3.2.2 does not only say
WHICH name a reference resolves; it says a name resolves to ONE definition: *“Within a run unit, all instances
of a given name that is externalized to the operating environment shall identify the same kind of entity or
item. Except for method-names and property-names, when two or more source elements identify something with the
same externalized name, they refer to the same instance.”* Two distinct definitions cannot be one instance, so a
compilation group that defines one externalized name twice is nonconforming and the compiler — which sees both
— reports it. `BinderDriver.CheckDefinitionNameUniqueness` is the ONE place that rule lives, in the two scopes
the standard gives it:

| scope | population | rule | code |
|---|---|---|---|
| the whole compilation group | §8.3.2.2's OWN list item 1, as far as this compiler models it — OUTERMOST program definitions, FUNCTION definitions, CLASS definitions and INTERFACE definitions, by `ExternalizedName` | §8.3.2.2 — SAME-kind pairs by its “refer to the same instance” sentence, CROSS-kind pairs by its “same kind of entity or item” sentence | COBOLNET2213 |
| ONE outermost program | its containment subtree, by declared `Name` | §8.4.6.3 — *“The names assigned to programs that are contained directly or indirectly within the same outermost program shall be unique within that outermost program”* | COBOLNET2214 |
| the compilation group | object-class / interface definitions, by `Name` | §8.4.6.4 — its own explicit uniqueness sentences | COBOLNET0820 / COBOLNET0840 |
| the compilation group | FUNCTION definitions, by declared WORD | §8.4.6.7 — a REPOSITORY `FUNCTION word` entry names the user-function-NAME, so two definitions sharing a word are ambiguous even under different `AS` literals | COBOLNET1508 |

All four definition kinds are ONE namespace in the first row, not four, because §8.3.2.2's list item 1
externalizes all of them — which is why the function-only duplicate check that used to sit in
`BuildUserFunctionTable` (and cited §8.4.6.6, the scope of function-prototype-NAMES, a real clause answering
a different question) is gone, and why a CLASS-ID sharing a PROGRAM-ID's externalized name used to compile
clean: **every namespace policed only itself, so nothing ever compared a definition of one kind against a
definition of another.** The two rows below keep exactly what their own clauses own — §8.4.6.7's WORD
ambiguity for functions, and §8.4.6.4's class/interface word uniqueness, which row 1 deliberately does not
restate (a class/class or class/interface pair sharing a WORD is skipped there, so one fault draws one
diagnostic; a pair whose words differ and whose `AS` literals coincide is §8.3.2.2's alone and row 1 has
it).

⚠ **DETERMINATION — the comparison is CASE-INSENSITIVE**, the ONE mapping `ExternalizedNames.Same` (DOC-A.1-68) that `ProgramTable` resolves a CALL
with. §8.3.2.2 leaves the mapping to the implementor (*“The implementor defines the formation and mapping rules
of these names”*); what is not optional is that the bind-time check and the run-unit resolver agree, or source
this check passes still resolves to the wrong definition. PROTOTYPE units contribute nothing here: §10.6.2
SR2 legislates FOR the pair — *“If a compilation group contains both a program definition and a program prototype
definition with the same externalized name, the signatures of these two compilation units shall be the same”* —
and SR3 is its function twin. The complement is pinned as hard as the
refusals (`DefinitionNameUniquenessTests`): two different containers may EACH contain a program of one name,
and two outermost programs may share the declared word under different `AS` literals.

**FORMATION is the mapping's other half, and it lives in the same place** (`CobolNet.Runtime.ExternalizedNames`,
DOC-A.1-68; kb/Work PB1539): `Form` removes a name's leading and trailing SPACES, `Same` / `Comparer` ignore case.
Both sides of every lookup ask it — the bind-time AS screen (`ExternalizedName.Screen`, which warns COBOLNET2642
when it removes spaces and refuses an all-space literal with COBOLNET2643 where the clause refuses a zero-length
one), the CALL / CANCEL literal reader, the INVOKE method name, and at run time every CALL / CANCEL /
program-address target, the registered `CallName`, the EXTERNAL store's keys and pointer equality. No emitter
trims a name expression: the runtime forms it (`ExternalizedNameFormationDriftTests`).

**Where the pair lives.** `BoundUnit.Name` / `BoundUnit.ExternalizedName` for programs and functions; `OoClassSymbol` / `OoInterfaceSymbol` / `OoMethodSymbol.ExternalizedName` for the OO trio; `DataItem.ExternalizedAs` and `FileModel.ExternalName` for §8.3.2.2's site 2), the EXTERNAL clause — whose default is §13.18.22.4 GR5's second sentence (the subject's own data-name or file-name) and is applied at the ONE cell-keying site, `DataBinder.CallMakeExternal` (kb/Work PB511). Each defaults to the declared word, so a source unit with no AS phrase is bit-for-bit what it was. The run-unit registry carries both (`ProgramTable.Node.Name` for MODULE-NAME, `Node.CallName` for resolution) and `ProgramRegistry.Register` takes its `externalizedName` argument only when they differ, keeping every AS-less unit emitted registration line byte-identical.

**Why `CsName` is deliberately NOT derived from the externalized name.** For a class the emitted C# type name is a wire contract with `PicInfo.ClrType`, which maps a declared object-class-name to a C# type with no access to the class table; deriving it from the AS literal would break that mapping. The method side has no such constraint, which is why the METHOD roster IS keyed on the externalized name — and that key also realizes §11.7.3 SR9 (*"if method-name-1 **or literal-1** is the same as a method-name inherited or implemented"*) without a second lookup path.

**Rejected alternative: one field.** That is what the tree had, and it was wrong in both directions. `MakeUnit` overwrote `name` with the AS literal, so §10.7.3 SR2 END PROGRAM match and the §15.65.4 r4 MODULE-NAME determination would both have broken the moment the phrase parsed — which it could not, because the phrase had no grammar surface at all (kb/Work PB303: rejected at 2002+ with COBOLNET0901 because `AS` fell through the PROGRAM-ID attribute list as a user-defined word, and ACCEPTED at 85, the one edition whose PROGRAM-ID paragraph has no AS phrase).

### D8. The FUNCTION-POINTER carrier is the PROGRAM-POINTER carrier's TWIN and deliberately a SEPARATE type; the `TO {function|program}-prototype-name-1` restriction is ONE field and its same-signature rule ONE test (kb/Work PB452 + PB817).

**The carriers.** `CobolNet.Runtime.FunctionPointer(string? Name)` is shape-for-shape `ProgramPointer`: a readonly record struct over the EXTERNALIZED function-name, which ISO §8.4.3.12.4 GR2 makes the address of a COBOL function outright (*“For a COBOL function, the address is that of the function identified by the externalized function-name in its FUNCTION-ID paragraph”*), resolved through the ONE run-unit `ProgramTable` (`FunctionAddressOf`, the `EntryOf` twin, differing only in the `Node.IsFunction` discriminator §8.4.6.3's first paragraph requires in both directions). ⛔ **Two structs, not one.** §8.4.3.10.4 gives each category its own predefined NULL and §14.9.39.3 SR20/SR21 forbid mixing the two categories in one SET, so a shared struct would make a program-pointer and a function-pointer assignment-compatible in the GENERATED C# — the one place the binder's category screens cannot reach. Alignment / size / representation / allowable languages are published as `docs/CONFORMANCE.md` §7 row **DOC-A.1-210** (§13.18.60.4 GR26 requires it); GR15's “effect of SET on the function” is row **DOC-A.1-174** and the determination is NONE.

**The restriction is one field.** `PicInfo.RestrictedPrototypeName` carries the `TO` operand for BOTH prototype carriers — §13.18.60.4 GR25 (program) and GR26 (function) — because the two general rules are one rule twice and so are their consumers, §14.9.39.3 SR20 and SR22: *“the {function|program}-prototypes associated with identifier-N and identifier-M shall have the same signature”*. The CATEGORY says which namespace resolves the name (§8.4.6.6 vs §8.4.6.8 — themselves the same sentence twice, and screened by one block in `DataBinder`); the RULE over it is `PrototypeSignatures.Same`, which compares the two `CalleeSignature`s position by position through `OoConformance.DescriptionMismatch`, the compiler's ONE description-equality check, and — because a group's description IS its subordinate entries (§13.7.3 SR2, §8.13; kb/Work PB1115) — requires a group to meet a group with the same §8.5.3.1 elementary layout (`StrongTypeModel.SameElementaryLayout`, the structural half of type-declaration equivalence: each elementary item at the same relative position, of the same length, with the same eight clauses; names and intermediate grouping excluded). It is DISTINCT from `RestrictedTypeName` (§13.18.60.4 GR23) because that one names a TYPE, not a prototype. §13.18.60.3 **SR18 and SR19** — the TYPEDEF requirement — are likewise one screen; there is NO function twin of them (measured: §13.18.60.3 stops at SR21 and carries no function-prototype-name sentence), so a level-1 `USAGE FUNCTION-POINTER TO fp` needs no TYPEDEF while `USAGE PROGRAM-POINTER TO pp` does.

**The CALL consumer of a restricted program-pointer (kb/Work PB609).** §14.9.4.3 SR14 is Format 2 only (`CALL identifier-1 AS program-prototype-name-1`): when identifier-1 is a restricted program-pointer, the signature of the prototype its TYPEDEF names shall equal the AS phrase's. `CallBinder` asks it where it classifies identifier-1 as a program-pointer, through `PrototypeSignatures.Same` over `StatementBinder.ProgramSignatureOf` — the ONE name-to-signature reader the SET program-pointer pairs (SR22) and the address-identifier argument share — and refuses a mismatch as COBOLNET2936. A Format 1 `CALL pp USING …` has no program-prototype-name-1 for SR14 to compare, and §14.9.4.4 GR7 takes the callee's characteristics only from a prototype the statement names, so no signature is imported from the pointer there (Annex D.9.3.2 is informative); the run unit's §14.9.4.4 GR3 d) check still applies at activation.

**Optionality is measured, not assumed.** On the printed §13.18.60.2 general format (folio 503) POINTER's and PROGRAM-POINTER's `TO` operands are BRACKETED and FUNCTION-POINTER's is NOT — so every function-pointer is restricted to a prototype, GR26's signature invariant always has one to name, and a bare `USAGE FUNCTION-POINTER.` is COBOLNET1958. The word `TO` itself is NOT underlined in any of the three (kb/Work PB848), so it is an optional word (§5.2.3): `USAGE FUNCTION-POINTER fp-proto` restricts exactly as `… TO fp-proto` does, and `DataBinder` reads the OPERAND's presence, never the TO token's.

**The consumer — a function-identifier through the pointer (kb/Work PB847).** §8.4.3.2.2 gives the function-identifier a third name form, function-pointer-name-1 (SR4: a function-pointer data item; SR2: FUNCTION may be omitted; SR5: the parentheses are required). It binds in `IntrinsicBinder` — the FUNCTION-keyword form in `BindIntrinsicCore` after the prototype arm, the keyword-omitted `fp (args)` form in `KeywordOmittedFunction` (a function-pointer is never a table, so the '(' takes nothing from the subscript path), both through ONE `FunctionPointerNamed` test — and lowers in `UdfBinder.UdfActivate`, the SAME prototype activation a function-prototype reference takes: §8.4.3.2.4 GR1/GR4 take the result description and the activated element's characteristics from the prototype the pointer's TO phrase names, so the arguments, the §14.8.2 conformance and the result temporary are the prototype path's own. The ONE difference is the target: the hoisted `BoundCallProgram` carries the pointer operand (`IsFunction` + `IsPointerTarget`) and the emitter's ONE invocation renderer (`CallEmitter.InvocationText`, shared by the statement and per-evaluation sites) activates `ProgramRegistry.CallFunctionPointer` — the function the pointer HOLDS at run time (GR6c), resolved through the same `CallProgram` lookup with GR6b's EC-FUNCTION-NOT-FOUND, and a NULL pointer raising **EC-FUNCTION-PTR-NULL** (Fatal), queried precisely for that node in `EcBinder`. `FUNCTION fp` without parentheses is COBOLNET2234 (SR5).

**The sender.** SET Format 8's own printed figure (folio 730) is `SET { identifier-12 } … TO identifier-13` with no choice indicators — identical to Format 9 — so the PLAIN form needs no grammar rule: `SET fp1 TO fp2` parses as `setToValueStatement` and `SET fp TO NULL` as `setObjectReferenceStatement`, and `SetBinder` re-routes on the receiver's resolved category exactly as Format 9 does. What needs a rule is §8.4.3.12's **function-address-identifier**, `ADDRESS OF FUNCTION { function-prototype-name-1 | identifier-1 }` (`setFunctionAddressStatement`; `OF` is unstressed on the printed folio 141, so it is an optional word, and there is no literal-1 arm — §8.4.3.13's PROGRAM twin has one and this does not). Without it a function-pointer could never hold a non-NULL value.

**And the PROGRAM twin, which was missing for longer** (kb/Work PB549). §8.4.3.13's
**program-address-identifier** — `ADDRESS OF PROGRAM { identifier-1 | literal-1 | program-prototype-name-1 }`,
RENDERED from the printed figure (PDF p172 / folio 142: ADDRESS and PROGRAM underlined, `OF` not, so it is an
optional word on the same evidence its FUNCTION and data twins take theirs from) — is the ONLY syntax ISO gives
for putting a program's address into a program-pointer, and it was in the grammar at NO edition. The only route
in was `SET pp TO ENTRY {literal | identifier}`, the Micro Focus / IBM spelling whose words occur nowhere in
ISO/IEC 1989:2023 — which two comments in the tree nevertheless described AS §14.9.39 Format 9 with the
§8.4.3.13 sender. Both spellings now exist, `programAddressIdentifier` is its own grammar rule because
§8.4.3.13 is an IDENTIFIER format rather than a statement phrase, and both bind through ONE body: the
§14.9.39.3 SR21 receiver screen is shared (`BindProgramAddressTargets`) and both reach the same bound node, so
the vendor surface cannot drift away from the standard one. The three braced arms carry §8.4.3.13.3 SR1 / SR2 /
SR3 on COBOLNET2157, the prototype arm resolves through the EXTERNALIZED program-name §8.4.3.13.4 GR2 names, and
GR3's restricted-pointer characteristic meets a restricted receiver through the SAME `PrototypeSignatures.Same`
test SR22 and SR20 already share. Edition row: `set-program-pointer-2002`, beside `set-function-pointer-2014`.

**Two determinations on §14.9.39.4 GR14's run-time screen.**
1. GR14 says the address shall be *“the address of a function defined with the same signature as the function referenced in the definition of identifier-13”*, which cannot be read literally when identifier-13 is a function-address-identifier of the `identifier-1` form (§8.4.3.12.4 GR1 a) — that form names no prototype at all. The screen is therefore implemented against the RECEIVING item's declared function-prototype, which is §13.18.60.4 GR26's own standing invariant and is what GR14 enforces statement by statement; wherever the sender DOES carry a prototype, SR20 has already made the two identical at bind, so no conforming program can observe the difference.
2. The run-time compare is **arity-only**, because a registered unit carries `FormalCount`/`RequiredCount` and nothing finer — the same granularity as the existing compile-time COBOLNET1513 prototype-vs-definition check. The COMPILE-TIME SR20/SR22 compare is richer: it has both bound `CalleeSignature`s. When the receiver's prototype has NO compile-time signature (a §12.3.8.4 GR11 c) external-repository prototype) the screen is not emitted at all rather than run against a guessed arity — GR4's not-found screen still runs, because locating a function needs no signature.

**The two EC raises ride opposite rules and are written that way.** §8.4.3.12.4 GR4 (not locatable) DEFINES the value as NULL, so the store happens and only the EC raise is checking-gated (§14.6.13.1.4). §14.9.39.4 GR14 NAMES its outcome — *“no data items are changed, and the execution of the SET statement is terminated”* — so the store-skip is unconditional and only the raise is gated.

## C# mapping

> Dual-backend rule (SSOT §18 #23): ALL semantics in this section live in backend-neutral bound nodes behind `ICodeGenBackend` (`--backend roslyn|cil`); what follows is the RoslynBackend's C# rendering. Bound nodes carry structured forms (pass mode, carrier construction mode, caller PicMeta) — never pre-rendered C#-specific fragments; a future CIL backend renders the same bound semantics with its own private lowering.

PASSING MODES — caller side:
  BY REFERENCE: CALL "INC" USING CTR.  (01 CTR PIC 9(4))  ->  _INC.Run(ManagedRef<long>.OverField(()=>CTR, v=>CTR=v));   // callee mutation visible
  BY CONTENT:   CALL "P" USING BY CONTENT CTR.            ->  _P.Run(ManagedRef<long>.Cell(CTR));                       // copy; not visible
  BY VALUE:     CALL "P" USING BY VALUE N. (2002)         ->  new CobolArg(CobolPassMode.Value, ManagedPointer<long>.Cell(N), <N's NumProfile>);   // §14.9.4.4 GR8: a lone N is identifier-4, so it crosses on ITS OWN carrier and its own description (kb/Work PB238; the whole profile since PB873)
  BY VALUE expr: CALL "P" USING BY VALUE N * 2 - 3.       ->  new CobolArg(CobolPassMode.Value, ManagedPointer<Int128>.Cell((Int128)(…)), <signed DISPLAY 38-digit profile at scale>);   // arithmetic-expression-1 — the exact lane's own carrier; a FLOAT-lane expression takes ManagedPointer<double> instead
                                                              // both are "a value copy allocated at call initiation and conformed to the formal" (§14.2.3 GR10) — the conformance runs in the CALLEE, at the formal's scale
  subscripted:  CALL "P" USING TBL(I).   -> int _i=(int)I-1; _P.Run(ManagedRef<long>.OverField(()=>TBL[_i], v=>TBL[_i]=v));  // index captured once (GR3a)
  literal/expr: CALL "P" USING 5.        -> _P.Run(ManagedRef<long>.Cell(5L));                                          // inherently BY CONTENT
  OMITTED:      CALL "P" USING OMITTED.   -> _P.Run(ManagedRef<long>.Null);
CALLEE side — LINKAGE + PROCEDURE DIVISION USING:
  LINKAGE SECTION. 01 LK-CTR PIC 9(4).   PROCEDURE DIVISION USING LK-CTR RETURNING LK-R.
  ->  private ManagedRef<long> LK_CTR;
      public long Run(ManagedRef<long> p0){ LK_CTR = p0; /*proc body*/ return _ret; }   // refs to LK-CTR read/write LK_CTR.Value
  ADD 1 TO LK-CTR.  ->  LK_CTR.Value = CobolNum.Store(LK_CTR.Value + 1L, 0, _P_LK_CTR);   // the one unavoidable indirection
  header BY VALUE (ISO §14.2.2 using-phrase; 2002): PROCEDURE DIVISION USING BY VALUE LK-V.
  ->  __lnk{Uid} = CobolArgAdapt.NumValue(__args, 0, _P_LK_V, scale);   // a DETACHED cell conformed to the formal — the §14.2.3
      // GR10 "COMPUTE without ROUNDED" value copy; stores hit only the cell (NO copy-out — never the caller). Modes thread
      // per §14.2.3 GR4 (transitive; BY REFERENCE assumed first); LinkageFormal.ByValue carries the resolution. §14.2.2 SR2
      // restricts BY VALUE formals to class numeric/message-tag/object/pointer (COBOLNET1553); the carried leg is fixed-point
      // numeric AND the managed classes — GR10 names both fillings, "a COMPUTE statement without the ROUNDED phrase"
      // for a numeric formal and "a SET statement" for one of class object or pointer (CobolArgAdapt.SlotValue; kb/Work PB663).
      // Every SR2 class is carried (floating-point since kb/Work PB1114, a METHOD's BY VALUE formal since kb/Work PB1051: both are detached copies the callee never writes back). A UDF activation's arguments take BY VALUE
      // whenever the formal says so (§8.4.3.2.4 GR5c; argument class per §8.4.3.2.3 SR10 = COBOLNET1554) — ONE ABI, both paths.
UNIFORM ABI (dynamic / cross-assembly):
  CALL identifier WS-PGM USING A.  ->
      var _p = Registry.Resolve(WS_PGM);
      if (_p is null) { /* ON EXCEPTION s1 | throw EC-PROGRAM-NOT-FOUND */ }
      else { int _rc = _p.Call(new CobolArgs{ Args=[ new CobolArg(PassMode.Reference, PicMeta.A, ManagedRef<...>.OverField(...)) ] }); /* RETURNING; NOT ON EXCEPTION s2 */ }
POINTERS (2002):
  01 P USAGE POINTER. 01 B PIC X(5) BASED.
  SET P TO ADDRESS OF X.       -> P = ManagedRef<string>.OverField(()=>X, v=>X=v);
  SET ADDRESS OF B TO P.       -> _B_ref = P;                          // B has no own storage; refs to B go via _B_ref.Value
  ALLOCATE B.                  -> _B_ref = ManagedRef<string>.Cell(new string(' ',5));
  ALLOCATE 5 CHARACTERS RETURNING P. -> P = ManagedRef<string>.Cell(new string(' ',5));
  FREE P.                      -> P = ManagedRef<string>.Null;         // GC reclaims
  IF P = NULL                  -> if (P.IsNull)
RETURNING / GOBACK (the result is the PROCEDURE DIVISION header RETURNING item, §14.9.18.4 GR2 — in ISO it is NOT a GOBACK operand):
  PROCEDURE DIVISION … RETURNING LK-R.  GOBACK.  ->  return _ret;         // _ret tracks the header RETURNING item; called program → result to the activator, main → run unit ends
  GOBACK RETURNING WS-R.  (accepted vendor extension, gated 2002+)  ->  _ret = WS_R; return _ret;   // moves the operand into the header RETURNING item first
EXTERNAL / GLOBAL:
  01 SHARED PIC 9(4) EXTERNAL.  ->  long SHARED { get=>ExternalStore.GetL("SHARED"); set=>ExternalStore.SetL("SHARED",value); }   // one copy per run unit
  01 G PIC 9 GLOBAL. (outer)    ->  field on the OUTER class; nested classes read via the enclosing-instance reference
NESTED:
  PROGRAM-ID Inner inside Outer ->  private sealed class Inner : ICobolProgram { ... }  ; CALL "Inner" -> typed fast path to the nested class

## Hard problems

### BY REFERENCE with category mismatch (arg PIC X(4) seen as formal PIC 9(4)) — same storage reinterpreted, but ManagedRef<string> cannot alias a ManagedRef<long>.

Confine to the sanctioned transient-byte boundary: materialize the arg's value into a scratch byte image, hand the callee a ManagedRef<long> whose get/set decode/encode that buffer, write bytes back through the caller field's codec on return. Same-category (the common case) is always fully typed and never touches this path.

### BY REFERENCE of a group item — `record struct` is a value type; a closure `()=>G` copies on read, so naive aliasing loses callee mutations.

The group carrier round-trips the WHOLE struct per access: get reads a copy, set writes the entire struct back to the caller field, so subordinate-item mutations propagate as a unit at each store. A group passed BY REFERENCE is held in a `StorageCell` instead and crosses with its AREA as well (see **Area formals** below, kb/Work PB2087); the per-access carrier is then a window over the cell.

**ODO full-allocation rule (§14.2.3 GR8):** an occurs-depending group crosses the CALL boundary at its FULL maximum allocation, never the §13.18.38 GR8 current-extent window — §14.2.3 GR8 says BY REFERENCE "operates as if the formal parameter occupies the same storage area as the argument", and the *storage* is the maximum allocation (the ODO window is a *sending-operand* rule for MOVE/compare/INSPECT, not a storage-aliasing rule). BY CONTENT groups follow the same full-allocation rule (GR9 — the copy is of the record). Mechanically: `CodeGen/Verbs/CallEmitter.cs::CallStringRead/CallStringWrite` are the ONE boundary read/write pair (the BY REFERENCE carrier, the BY CONTENT snapshot, the callee copy-in/copy-out, RETURNING) and bypass `OdoGroupPlace.SendingImage()`/`ReceiveInto` for the full `AsImage()`/`FromImage` forms.

**THE VARIABLE-LENGTH CROSSING — a THIRD carrier form (§8.5.1.12; kb/Work PB204).** §14.9.4.3 SR12's flat
prohibition is FORMAT 1's; a Format-2 CALL, a CALL RETURNING and an INVOKE are governed by SR25 → §14.8.2.2 and
§14.8.3.2, which **admit** a variable-length group "subject to compatibility as described in 8.5.1.12". So the
boundary carries three forms, not two:

| the argument's storage | carrier | read / write |
|---|---|---|
| a native fixed-point leaf | `ManagedPointer<T>` over the field's own carrier type | `PlaceRenderer.Read`/`Write` |
| a native FLOATING-POINT leaf | `ManagedPointer<double>` / `<float>` — the field's own carrier (kb/Work PB238) | `PlaceRenderer.Read`/`Write` |
| any fixed-window character storage, a group included | `ManagedPointer<string>` — the record image | `CallEmitter.CallStringRead`/`CallStringWrite` |
| a VARIABLE-LENGTH group | `ManagedPointer<CobolVarGroup>` | `PlaceRenderer.VarGroupImage`/`WriteVarGroupImage` |
| a MANAGED SLOT — class pointer (data / program / function) or class object-reference | `ManagedPointer<ManagedPointer\|ProgramPointer\|FunctionPointer\|«class»?>` — the item's own `PicInfo.ClrType` (kb/Work PB663) | `PlaceRenderer.Read`/`Write` |
| a boolean-expression-1 VALUE (§14.9.4.2 Format 2 BY CONTENT) | `ManagedPointer<string>` — the §8.8.2 bit-string value, taken at the largest boolean ITEM referenced (carried at run time — a slice or a function result can have a run-time length, kb/Work PB589) | `BooleanRenderer.RenderAtItemWidth` (no place; `BoundCallArg.ContentBool`) |
| an ADDRESS-IDENTIFIER argument (§8.4.3.1.2 Format 9; §14.9.4.3 SR3/SR4 — kb/Work PB239), in EVERY mode | `ManagedPointer<ManagedPointer>` (data arm, §8.4.3.11) / `ManagedPointer<ProgramPointer>` (program arm, §8.4.3.13) — a detached CELL holding the pointer value, never an accessor | `PtrEmitter.AddressOfText` / `ProgramRegistry.EntryOfArgument` (no place; `BoundCallArg.DataAddress` / `ProgramAddress`) |

**THE ADDRESS-IDENTIFIER ARGUMENT (kb/Work PB239).** §14.9.4.3 SR3 admits "an address-identifier" as identifier-2
beside a data item, and SR4 makes it a SENDING operand even BY REFERENCE ("… or if identifier-2 is an
address-identifier, identifier-2 is a sending operand"), so it crosses as a detached value in all three modes —
Annex D.6.5.6.4: "it will never be updated even when passed by reference. It behaves like being passed by
content." The grammar spells the identifier through ONE `addressIdentifier` rule (`dataAddressIdentifier` |
`programAddressIdentifier`) shared with the SET senders, so the data pass forces the named record onto cell
storage by walking those nodes, and each arm binds through the ONE operand binder its SET twin uses
(`PtrBinder.BindDataAddress`, `SetBinder.BindProgramAddressOperand`). At a Format-2 boundary the pairing is
§14.8.2.3.2's class-pointer paragraph (same category; restricted ⇔ restricted to the same type) —
`PtrBinder.AddressConformanceReason`, the ONE verdict the INVOKE argument reads too. A program arm that cannot be located is §8.4.3.13.4 GR4's NULL, raised as
EC-PROGRAM-NOT-FOUND while the argument array is built when the CALL's checking enables it (§14.9.4.4 GR3a — "no
program is called"), so the CALL's own GR3h arms take it. ⚠ DETERMINATION: §14.9.4.3 SR10's Format-1 ban on a
BY REFERENCE item "of class … pointer" does not reach an address-identifier — SR3–SR5 and Annex D.6.5.6.4 admit it
in the non-prototype format with every mechanism, and there is no program storage for SR10 to protect.

**THE SAME IDENTIFIER AS AN ORDINARY OPERAND — the INVOKE argument and the relation operand (kb/Work PB1021).**
§14.9.23.3 SR9 names the address-identifier for INVOKE's identifier-3 exactly as §14.9.4.3 SR3 does for CALL, and
SR19 makes it a sending operand; §8.8.4.2.2 Format 3's identifier-3 / identifier-4 admit it as "a unique data item
of class pointer" (§8.4.3.11.4 GR1 / §8.4.3.13.4 GR1). Both surfaces spell the ONE `addressIdentifier` rule
(`invokeArgument` in every phrase, the §8.4.3.4 inline `argument`, and `comparisonOperand`), so the storage forcing
and the 2002 introduction gate reach them with no change of their own, and both bind through the ONE
`PtrBinder.BindAddressIdentifier` into a `BoundAddressOperand` — a `BoundOperand` of its own, because the item it
denotes has no storage and therefore no `Place`. The ONE category / class classifiers
(`IntrinsicResultType.OperandCategory`, `IntrinsicArgumentRules.ClassOf`) answer data-pointer / program-pointer
and class pointer for it, so the §8.8.4.2.3 SR5 band screens it like any pointer operand, and
`ConditionRenderer`'s pointer arms read it through the ONE value renderer (`PtrEmitter.AddressOperandText`). An
INVOKE argument crosses the pointer carrier verbatim and never writes back. A program arm that cannot be located as
an ORDINARY operand raises EC-PROGRAM-NOT-FOUND the way every in-statement fatal condition is raised
(`ProgramRegistry.EntryOfOperand`), because §14.9.4.4 GR3a's pre-transfer delivery is the CALL statement's alone.

**THE MANAGED SLOT — THE FOURTH FORM, AND ONE CLASSIFICATION FOR BOTH SIDES (kb/Work PB663).** A class-pointer
or class-object-reference item's value IS a managed reference, not a byte image (the same fact
`SlotWindow.CarriedBySlot` states for storage — PB231). Its carrier is therefore its own `PicInfo.ClrType`,
exactly as a numeric leaf's is, and §14.8.2.3.2 removes any need to convert at the boundary: *“If either the
argument or the formal parameter is of class pointer, the corresponding formal parameter or argument shall be of
class pointer and the corresponding items shall be of the same category”*, and its object-reference rules 1–3
force the same universal/interface-name/object-class-name on both ends. `CobolArgAdapt.Slot<T>` therefore
aliases a same-`T` carrier and fails the activation with EC-PROGRAM-ARG-MISMATCH for anything else (kb/Work
PB615 — see "A supplied argument the formal cannot read" below), and `SlotValue<T>` is
§14.2.3 GR10's detached record — which GR10 fills with *“a SET statement”* for this class, i.e. the reference
copy itself.

**⛔ THE CLASSIFICATION ASKS THE PLACE'S CODING, NOT ITS SHAPE (kb/Work PB1632).** A cell-backed area (BASED,
EXTERNAL, or one whose ADDRESS OF is taken) renders every member as a `RedefViewPlace`, and a managed member's
view carries the `SlotWindow` coding. That view IS the item's storage, not a redefinition of it.
`CallEmitter.CallPlaceIsManaged` therefore admits `RedefViewPlace { Coding: SlotWindow }` beside the plain
place, as `CallPlaceIsVarGroup` admits its own `VarGroupWindow` coding. Any other redefinition or
reference-modification over a pointer stays excluded, because §13.18.44.3 SR12/SR14 and §8.4.3.3.3 SR1 bar both
on conforming source. `PlaceRenderer.Read`/`Write` already render a slot window
(`CobolPtr.SlotRead`/`SlotWrite`), so the Managed arm's snapshot and accessor carriers need no second rendering.
Excluding every view sent `SET WP TO ADDRESS OF WP` followed by `CALL … BY CONTENT WP` to the character arm,
where the compiler threw. Golden: `tests/conformance/2002/pb1632_pointer_arg_cell_backed` (a self-addressed
pointer, a BASED pointer and a cell-backed program-pointer, BY CONTENT and BY REFERENCE).

**⛔ THE PREDEFINED NULL AS AN ARGUMENT IS AN IDENTIFIER OF THE FORMAL'S CLASS, NOT A FILL (kb/Work PB1630).**
§8.4.3.1.2 lists NULL as identifier Format 8 (predefined-address) and Format 6 (predefined-object), and §8.4.3.10.3
SR1 a) admits it *“as an argument in a program-prototype format CALL statement, a function-prototype format
function activation, or a method invocation”*, its category decided by *“the associated data item's class”* — the
formal's. So it crosses in its written mode (BY CONTENT, or BY VALUE: §14.9.4.3 SR22 admits identifier-4 of class
pointer or object, and SR23's numeric-literal rule is not about it) as ONE storage-free carrier,
`PredefinedNullArgument.Instance`, and the formal's `Slot<T>` / `SlotValue<T>` supplies a fresh cell holding the null
of its own carrier (`CobolArgAdapt.PredefinedNull<T>`: `ManagedPointer.Null`, a default `ProgramPointer` /
`FunctionPointer`, a null reference — §8.4.3.10.4 GR1–GR3, §8.4.3.7.4 GR1). The carrier is the same whether or not
the activating element knows the formal, which it does not under §12.3.8.4 GR10 c); every non-slot adapter refuses it
as EC-PROGRAM-ARG-MISMATCH. At bind, `ParameterConformance.ContentConformanceReason` gives NULL the §14.8.2.3.3 verdict
of an identifier of class pointer — the SET paragraph for a pointer / object-reference formal, and a refusal for every
other formal, whose MOVE or COMPUTE admits no pointer or object operand (§14.9.25.3 SR1). The INVOKE lane is typed, so
it renders the formal's own `PicInfo.DefaultInitializer` (`BoundInvokeArg.PredefinedNull`). Before PB1630 the CALL and
function lanes crossed NULL as a one-character LOW-VALUE fill, which a pointer formal refused at run time and a PIC X
formal accepted.

**⛔ A POINTER BY CONTENT INTO A NON-POINTER FORMAL IS ITS STORAGE IMAGE (kb/Work PB970 arm 2).** §14.8.2.3.2 is
the BY REFERENCE rule; BY CONTENT, §14.8.2.3.3 rule 1 asks only that *“the formal parameter shall be of the same
length as the corresponding argument”* when the program is called with no program-specifier and no NESTED phrase,
and §14.2.3 GR9 moves the argument *“to this allocated record without conversion”*. So a `USAGE POINTER` (or
PROGRAM-POINTER / FUNCTION-POINTER) argument BY CONTENT into a `PIC X(8)` or 8-byte binary formal is conforming,
and the formal holds the pointer's 8 storage positions. `CobolArgAdapt.WithPointerContent` — asked first by
`Text` (non-group formal) and `Num` — replaces a class-pointer carrier of a BY CONTENT argument with a DETACHED
string cell holding `PointerImage.OfCarrier(…)` (DOC-A.1-216), after which the ordinary storage-image arms adopt
it. BY REFERENCE and BY VALUE (GR10 fills the formal's own description by COMPUTE/SET) stay EC-PROGRAM-ARG-MISMATCH,
a group formal stays refused (§14.8.2.2 rule 2's MOVE), and a rule-2 activation is refused at BIND —
`OoConformance.MoveContentMismatch` asks §14.9.25.3 SR1 of the sender before Table 16, because rule 2d is *“the
same as for a MOVE statement”*.

⛔ **The ACTIVATING and the ACTIVATED sides now read ONE classification**, `CallCrossing` +
`CallEmitter.CrossingOf` / `ProgramEmitter.FormalCrossing`. They were two formulations of one rule — a caller
chain of three place predicates and a callee `bool isNum` — and the callee's had only two arms, so a
`USAGE POINTER` formal was declared `ManagedPointer<string>` over a space image and its first reference was a
Roslyn CS1503 on conforming source. §14.9.4.3 SR10 bars a FORMAT 1 CALL from passing such an item BY REFERENCE,
so the callee arm is reachable only through a Format-2 activation (`AS NESTED`, a prototype) or a separately
compiled activator — which is why the hole survived the whole pointer increment. `LinkageCarrierDriftTests`
walks `PicCategory` itself and pins the invariant that makes the next class automatic: **a formal crosses as a
character image only when its own storage IS a C# string.**

**THE FLOAT LANE (kb/Work PB238).** `CobolArg`'s numeric meta was then a `(Digits, Scale)` pair describing a FIXED-POINT picture, and the
four integer carriers of kb/Work R12 were the whole numeric vocabulary — a float leaf was routed onto the
character image and a computed float BY VALUE argument was narrowed to `(Int128)` **at the caller**. Both lose
the fraction: §14.2.3 GR10 makes a BY VALUE crossing "a COMPUTE statement without the ROUNDED phrase" *into the
formal's description*, so the quantization belongs at the RECEIVER, at the formal's own scale — a caller-side
cast performs it at scale 0 (`01 F FLOAT-LONG VALUE 1.5` reached a `PIC S9(3)V99` formal as `001.00`). The
carrier is therefore the lane's own: `CobolArgAdapt.ReadRealCell`/`WriteRealCell` are the callee half, and
`Num`/`NumValue` quantize through `CobolFloat.ToScaledUnchecked(v, formalScale, Truncation)` — the un-ROUNDED
COMPUTE, once, where the scale is known. A float reaching a CHARACTER formal through GR9's first branch (no
program-specifier, no NESTED phrase — §14.8.2.3.2 / §14.8.2.3.3 rule 1 ask only for the same length there) sees
its STORAGE — its IEEE interchange bytes, through the carried description (kb/Work PB873, below); through
rule 2's branch §14.8.2.3.3's MOVE rules give a float sender no alphanumeric receiver, so that
branch excludes the pairing.

**WHICH SIDE PERFORMS THE LANDING — THE ACTIVATING ONE, WHENEVER IT CAN (kb/Work PB640).** §14.2.3 GR9's
second branch and GR10 both say the linkage record is *"allocated by the activating runtime element during the
process of initiating the activation"*, and both make the argument the sending operand of *"a COMPUTE statement
without the ROUNDED phrase"* into it. Every consequence of that COMPUTE is therefore the **activating** element's:

- its `>>TURN EC-SIZE CHECKING` state decides between §14.7.5's no-phrase rule 4 (EC-SIZE-TRUNCATION is set to
  exist) and DOC-A.1-70's low-order digits — enablement is a property of a compilation group (§14.6.13.1.1),
  never something the callee can be asked about;
- its USE declaratives are what §14.6.13.1.3 selects over; and
- §14.9.4.4 GR3 g) transfers control to the called program only *"if a fatal exception condition has not been
  raised"*, which a landing performed **after** the transfer can no longer honour.

So `CallEmitter.ArgText` wraps every BY CONTENT / BY VALUE argument whose corresponding formal is a numeric
item (fixed-point or floating-point — `PicInfo.IsClassNumeric`) in `CobolArgAdapt.LandForFormal<T>` — one wrapper around every carrier shape, `T` being the
formal's `PicInfo.ClrType` (**not** `DataItem.ElementType`, which answers `"string"` for an image-stored formal
and cannot satisfy the landing's `struct, INumberBase<T>` constraint). The landed `CobolArg` carries the
**formal's** whole description (`Num = formal`), because GR9's last sentence makes the allocated record *the*
argument from that point on — which is exactly what makes the callee-side landing the identity, and what makes an
image-carried formal see the record's own sign (kb/Work PB873).

**When the caller cannot, and why that is the standard's own line.** GR9's FIRST branch — a program with no
program-specifier in the activating element's REPOSITORY paragraph and no NESTED phrase — allocates a record
*"of the same length as the argument"* and moves it *"without conversion"*. There is no COMPUTE there, and no
formal description to perform one against. The set of crossings that ARE a COMPUTE (a prototyped program, a
NESTED CALL, a method, a function) is precisely the set whose formal is knowable at the call site, and
§14.8.2.3.3 draws the same partition for conformance (rule 1 vs rule 2 a)). `CobolArgAdapt.NumValue` / `Num`
therefore keep the landing for that residue and for a non-COBOL activator, sharing `LandScalar` with the
activating side so the two can never answer differently; `BoundCallArg.Formal` is null exactly on that branch.

**The raise needs no new machinery.** EC-SIZE-TRUNCATION is a FATAL ambient gate (`EcEmitter.FatalAmbientGates`)
and a CALL/INVOKE is not an `IArithmeticStatement`, so a statement compiled under EC-SIZE checking already
carries the try/catch that sets the last exception status, runs the §14.9.49 F3 selection and honours RESUME.
The landing is emitted INSIDE the argument expression, so the raise happens while the `CobolArg[]` is being
built — before `ProgramRegistry.CallProgram` is entered, which is GR3 g)'s ordering — and it works in an
EXPRESSION-position activation (a user-defined function reference inside a per-evaluation condition window) as
well as at statement position. The kernel is chosen at COMPILE time from `EcState.SizeTruncationChecking`,
the same way the arithmetic store chooses `checkedLanding`, so a unit with checking off emits the landing it
always had.

**A FLOATING-POINT FORMAL IS A NUMERIC RECEIVER OF THE SAME COMPUTE (kb/Work PB1114).** §14.8.2.3.3 2) a) and
§14.2.3 GR9/GR10 say *"if the formal parameter is numeric"* and name no floating-point exemption, yet the landing
above was guarded on the fixed-point description: a `PIC 9(3)V99` 12.5 BY CONTENT to a `FLOAT-LONG` formal of a
NESTED activation crossed with no landing and the callee's `Num` then landed it through the float formal's
description — which has ZERO digit positions, so the capacity reduction stored 0. The float lane is
`CobolArgAdapt.LandFloat`, the float twin of `LandScalar`, and it lands through **`FloatResultant`** — THE one
transfer of an arithmetic value into a float resultant identifier (kb/Work PB1196), not a second float store:
implied TRUNCATION (§14.7.4.3 rule 10 — the representable value nearer to zero, so `0.1` into a FLOAT-SHORT formal
is the binary32 just BELOW 0.1), a fixed-point or integer sender converted EXACTLY in one rounding (never through
binary64 first — kb/Work PB1110), and a value past the format's range is §14.7.5 case 3's size error: raised as
EC-SIZE-TRUNCATION (`FloatResultant.From…OrRaise`, the float twin of `CobolNum.StoreOrRaise`) when checking is
enabled at the activating statement, the no-phrase unchecked landing (DOC-A.1-70) otherwise. One predicate,
`CallAbi.IsFloatFormal`, sends the crossing to that lane at every arm that has a fixed-point and a float landing:
`LandForFormal` (activating side), `NumValue` (BY VALUE residue), `TextValue` (an image-carried float formal) and the `Num` converting view (the callee-side GR8
view over a native cell of another description, whose write-back un-scales into the caller's own representation).
The BY VALUE float formal needed nothing more: it is non-resident (a float formal is not carrier-resident), so the
existing copy-in / no-copy-out round trip over the detached cell carries it; the header's COBOLNET0899 refusal of
it (a "value-copy carrier not yet implemented" that was not true of the program and function arms) is gone. The
literal and arithmetic-expression arguments into a float formal, refused COBOLNET1688 as "a documented
refinement", are conformant (`OoConformance.ContentValueMismatch` — the ONE §14.8.2.3.3 verdict for a
storage-less sending value: a numeric literal, an arithmetic expression or a boolean value, kb/Work PB1113).

**The INVOKE lane is the same rule and was the same defect's other arm.** `OoEmitter`'s BY CONTENT arms already
landed caller-side (§14.8.2.3.3 rule 2 a): *"If the formal parameter is numeric, the conformance rules are the
same as for a COMPUTE statement"*), but always through the UNCHECKED `CobolNum.Store`, so they were silent under
checking too; they now take `NumericRenderer.StoreExpr(…, raiseOnSizeError:)` → `CobolNum.StoreOrRaise`, which
is the same primitive `LandForFormal`'s checked lane uses. The numeric-LITERAL arm went through a bare
`RuntimeApi.NumStore` and joined the other two at the same time — it had been missing the unsigned-wide lane as
well. Pinned per edition by `{2002,2014,2023}/pb640_call_argument_landing_checked` and
`{2002,2014,2023}/pb640_invoke_argument_landing_checked`, and structurally by
`CallAbiNumericCarrierDriftTests.TheCheckedLandingRaisesExactlyWhereTheValueDoesNotFit` plus the
activating-side identity assertions inside `TheGr8ViewAndTheGr10Copy_LandIdentically`. A FLOATING-POINT method
formal takes the same landing through `FloatResultant` in `OoEmitter`'s float arm (`IsFloatLanding`: a fixed-point
or other-usage float identifier, a literal-2 or an arithmetic expression; a same-usage float identifier keeps its
verbatim §14.9.25.4 GR6 c) read), raising under EC-SIZE checking through `RuntimeApi.FloatResultantStoreOrRaise`;
the binder's "fixed-point⇄float CONTENT conversion is not carried across INVOKE" refusal is retired
(kb/Work PB1114; `conformance:2002/pb1114_invoke_content_float_formal`, CALL lane
`conformance:2023/pb1114_call_content_float_formal`, `CallAbiFloatLandingTests`). **The third sender class** (the train review's
finding): a FLOATING-POINT sender BY CONTENT into a FIXED-POINT image-carried method formal takes the SAME §14.2.3 GR9 COMPUTE as a
fixed-point sender — `OoEmitter.OoStringReadOf`'s landing asks `IsClassNumericFixedPoint` of the formal and `IsClassNumeric` of the
sender (a COMPUTE's sending operand may be floating-point), through `NumericRenderer.StoreExpr`, which tells the lanes apart; it fell
to a MOVE store rendering the formal's profile bare (a Roslyn CS0103), and a MOVE where §14.7.5 r4 owes the EC-SIZE raise
(`conformance:2023/pb1114_invoke_content_float_to_fixed_formal`).

**THE ONE NUMERIC LANDING — `CobolArgAdapt.Land` (kb/Work PB288).** Every numeric arm of the callee-side adapter
reaches its receiving side through a single private helper, because §14.2.3 GR9 and GR10 describe the *same*
conversion — "if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase" into a record
of the FORMAL's description — and GR11 ("references to data-name-1 … are resolved in accordance with their
description in the linkage section") makes the GR8 aliasing view owe the identical value on every access. The
landing has two halves, and each was independently missing somewhere before the extraction:

1. **The scale alignment widens with STORE semantics** — `CobolNum.RescaleStoreCap`, not the unchecked
   `CobolNum.Rescale`. A magnitude the formal cannot hold is §14.7.5 case 3 ("after radix point alignment … is
   further from zero than permitted for the associated resultant data item"), and the no-SIZE-ERROR-phrase,
   checking-off disposition is documented in `CONFORMANCE.md` DOC-A.1-70: the receiver takes the result's
   LOW-ORDER digits at its own scale. It is never the two's complement of an `Int128` intermediate, which is not
   any rule — `BY CONTENT 1000000000000000000000000000000` into a `PIC S9(9)V9(9)` formal used to arrive as
   `873995514.006732800` because the widening formed 10³⁹ and wrapped.
2. **The digit-capacity conformance** — `CobolNum.Store` through the formal's own `NumProfile`. Without it the
   adapter's closing `T.CreateTruncating` is itself a BINARY truncation to the carrier, so an 18-digit argument
   viewed through a `PIC S9(4)V99` formal arrived as `−9838.16` where the low-order digits are `5678.00`.

`NumValue` had half (2) and not (1); `Num` had neither, on both the Int128 and the float lane — the two-arm shape
the shared helper exists to make unrepeatable, pinned by
`CallAbiNumericCarrierDriftTests.TheGr8ViewAndTheGr10Copy_LandIdentically` and by the golden
`2023/pb288_call_argument_scale_landing`, whose rows assert that the two arms and the equivalent inline
MOVE/COMPUTE all print the same characters. The write-back half of the GR8 view takes the same store-semantics
widening; its receiving *capacity* stays the caller's own carrier discipline (`WriteNumericCell`), because
the write-back lands in the caller's cell, whose own type is the capacity that applies there.

**THE CARRIED DESCRIPTION IS A WHOLE `NumProfile` (kb/Work PB873).** `CobolArg` is `(Mode, Carrier, NumProfile?
Num)`; `Digits` and `Scale` are derived from `Num`. A native cell holds a VALUE, but the storage it stands for has
a REPRESENTATION — the operational sign and its position (§13.18.52), the usage's byte form (§13.18.60.4) — and a
formal that sees the argument as characters sees that representation: §14.2.3 GR8 "operates as if the formal
parameter occupies the same storage area as the argument", and GR9's first branch moves the argument "without
conversion". The `(Digits, Scale)` pair it replaced could spell only a digit run, so `CobolArgAdapt.Text` and
`TextValue` each built the image from a hard-coded `Signed = false` profile: `-12.34` in a `PIC S9(4)V99`
argument reached a REDEFINED (image-carried) `PIC S9(4)V99` formal as `00123D` (+12.34) where the storage holds
`00123M`, on both arms. Now:

- the ACTIVATING side emits the argument's own `PicInfo.ProfileInitializer` for every elementary numeric place
  (a reference-modified view and a USAGE INDEX item carry `null`), and a signed DISPLAY profile of the value's own
  digits and scale for a literal or a computed expression (signed exactly when the value can be negative);
- `Text` renders the cell through THE record-image codec under that description (`CobolNum.FormatImage` /
  `FormatImageFloat`) and writes back through its inverse, splicing only the formal's positions (GR8);
- `TextValue` is GR10's record, "a data item of the same description as the formal parameter": it lands the
  argument through the shared `LandScalar` into the FORMAL's profile (emitted beside it) and renders that;
- an image-carried numeric ARGUMENT's carrier text is decoded through ITS OWN description, not the formal's,
  before GR9/GR10's COMPUTE.

**⛔ EVERY NUMERIC STRING CARRIER IS STORAGE (kb/Work PB970).** The `ManagedPointer<string>` carrier of a numeric
item holds its RECORD IMAGE under the description beside it — zoned digits, radix-2, BCD or IEEE bytes — on both
sides, in both directions, for every pass mode and for RETURNING. The activating element reads an image-carried
numeric place (an image-stored leaf or a REDEFINES view — `CallEmitter.IsImageCarriedNumeric`, the ONE predicate
the read and write halves share) as its window, which already holds those bytes, and stores a returned text into it
as it stands; the ABI's numeric legs over a string carrier (`LandScalar`, `LandFloat`, `Num`'s GR8 view,
`StoreReturn`) decode and encode with THE record-image codec (`CobolNum.ParseImage`/`FormatImage`, the float lane on
`ParseImageFloat`/`FormatImageFloat`), never the DISPLAY codec — whose zoned arm is the same code, so a zoned item
is unchanged. Before PB970 the image-carried side spoke the OPERAND text (`OperandText.FieldImage`) while the
native side's `Text` adapter had spoken storage since PB873: `PIC S9(5) COMP-3` −42 passed BY REFERENCE to a
REDEFINED formal arrived as bytes `00 00 0C` and came home as `02303`. The INVOKE boundary follows the same
convention (`NumericRenderer.ImageOfCarrier`/`CarrierOfImage` — the ONE native↔storage-image pair, shared with the OO universal crossing — for the native↔image copy-out and RETURNING arms; a BY CONTENT
argument into an image-carried fixed-point formal is the §14.8.2.3.3 rule-2a COMPUTE into the formal's
description, encoded under the OWNER-qualified profile).

**A supplied argument the formal cannot read (kb/Work PB615).** Every adapter's type switch used to end in the
§14.9.4.4 GR12 OMITTED carrier, whose read answered `default` — a supplied argument read as ZERO, silently, indistinguishable from an omitted one. It is not omitted:
a carrier outside the formal's crossing form is a §14.8.2 conformance violation, and §14.9.4.4 GR3 d) answers it
"the program call is not successful" with EC-PROGRAM-ARG-MISMATCH. `CobolArgAdapt.Unreadable<T>` raises exactly
that through the same `CobolCallException` the GR3d count check and `StoreReturn`'s `Undeliverable` use, marked
`RaisedAtAdoption`: adoption runs inside `ICobolProgram.Call` but before any of the element's statements, so the
activation boundary (`ProgramTable.CallProgram`) CONSUMES the mark instead of setting `ControlTransferred` — the
failure is this CALL's GR3h (its ON EXCEPTION phrase runs), and a boundary further out, for which control had
been transferred, marks it in the ordinary GR3i way. `Omitted<T>` is now reached only for a genuinely omitted
argument. Golden `2002/pb615_unreadable_argument_carrier`; unit
`CallAbiNumericCarrierDriftTests.ACarrierOutsideTheSix_FailsTheActivationWithArgMismatch`.

**⛔ A FIXED-LENGTH GROUP OPPOSITE A VARIABLE-LENGTH ONE (kb/Work PB965).** §8.5.1.12.1 admits the pair ("only
one of the operands may be a variable-length group") and §14.8.2.2 / §14.8.3.2 import it into every boundary. The
fixed group decomposes into the same `CobolVarGroup` (`FromFixedImage`) and is rebuilt from it (`ToFixedImage`)
at the spans of ITS tables that CORRESPOND to the variable-length group's dynamic-capacity tables — and
correspondence is a fact about the PAIR (§8.5.1.12.2: "they occupy the same relative byte positions within their
groups"), so it is computed by ONE walk, `CobolVarGroup.CorrespondingSpans(fixedLayout, varLayout)`, over each
group's §8.5.1.12 LAYOUT (`VariableLengthCompatibility.Layout` — `(kind, chars, elementChars)` triples: fixed run,
fixed table, occurs-depending table, dynamic-capacity table, dynamic-length item). The corresponding fixed table
crosses at its fixed occurrence count (§8.5.1.12.3 sentence 3); a fixed table opposite plain bytes is plain
material (the former `FlatTableSpans` lifted EVERY table and moved the wrong one); a dynamic-capacity table past the
fixed group's last character gets no component (§8.5.1.12.2's last sentence — the receiver's §14.6.9.4 space fill).
The two sides of a CALL are compiled apart, so each side's LAYOUT TRAVELS: `CobolArg.Layout` carries an argument's
(or the RETURNING receiver's) layout, emitted for a group with a table or a variable-length member; the formal's
adapter (`CobolArgAdapt.VarGroup/VarGroupValue(args, i, formalLayout)`) and the RETURNING legs
(`StoreReturnGroup`, `StoreReturn(ret, CobolVarGroup, layout)`) pair them; a table-less fixed group states only
its length (`CobolVarGroup.FixedRun`). The §14.9.25.4 GR9 MOVE and the typed INVOKE know both descriptions at
compile time and call the SAME walk there (`VariableLengthCompatibility.CorrespondingSpans`). ⚠ DETERMINATION: a
callee cannot grow a fixed-length argument's table past its fixed extent — that storage has no more occurrences —
so the write-back fits each component to its table as §14.6.9.2 fits a dynamic sending table into a non-dynamic
receiving one ("superfluous elements are not moved"; missing ones are space filled). Goldens
`2014/pb965_fixed_group_vlg_boundary`, `2014/pb965_fixed_group_vlg_invoke`; negative
`pb965-fixed-table-off-position`; unit `BoundaryGroupCorrespondenceTests`.

**⛔ THE REVERSE PAIR, AND THE SIZE RULE A VARIABLE-LENGTH PAIR TAKES (kb/Work PB965, finisher).** A variable-length
group has no length of its own: its collapsed `ImageWidth` counts a dynamic-capacity table as one element, which
§8.5.1.12.3 grants only to two MATCHING dynamic-capacity tables. So `OoConformance.DescriptionMismatch` — the one
comparator behind COBOLNET1688 (CALL argument), 1736 (CALL RETURNING) and 0828 (INVOKE) — decides the size question
per activation mode once either side is a variable-length group: a RETURNING pair takes compatibility alone (§14.8.3.2's
length sentence applies only when "neither of them is strongly typed or a variable length group"); BY REFERENCE,
§14.8.2.2 rule 1 binds only an ALPHANUMERIC group item (§3.11 excludes a variable-length group), so two
variable-length groups take compatibility alone and a fixed group opposite one compares in the lengths §8.5.1.12.3
sentence 3 gives the PAIR ("the dynamic-capacity table is considered to be the same length as the corresponding
table") — `VariableLengthCompatibility.PairCharWidths`, the SAME walk that decides the correspondence. Override and
prototype signature equality (§9.3.8.2.3) keeps strict equality. The admitted VARIABLE-into-FIXED pairs run through
the same correspondence: every group formal states its layout to `CobolArgAdapt.Text` / `TextValue`
(`ProgramEmitter.GroupFormalLayout`; `System.Array.Empty<int>()` for a table-less group, read as `FixedRun(width)`),
whose variable-carrier arm reads the argument through `ToFixedImage`; the typed INVOKE does the same at compile time
(`OoEmitter.VarPlaceSpans`), including both mixed RETURNING directions. ⚠ DETERMINATION: a formal with a FIXED
occurrence count cannot change the argument table's current capacity, so a BY REFERENCE store OVERLAYS the argument's
storage (`CobolVarGroup.OverlayFixedImage`, §14.2.3 GR8): the occurrences the argument has are written, one it lacks
is not, one past the formal's count is untouched, and material past a prefix formal survives. Goldens
`2014/pb965_vlg_into_fixed_boundary`, `2014/pb965_vlg_into_fixed_invoke`; negative
`pb965-vlg-into-larger-fixed-formal`.

**An OCCURS DEPENDING group argument's two lengths.** §14.8.2.2: "For an argument or formal parameter that is
described as an occurs-depending group item passed by reference, the maximum length is used. For an occurs-depending
group item passed by content, the length of the argument is determined by the rules of the OCCURS clause for a
sending data item." BY REFERENCE reads `CallEmitter.CallStringRead` (the whole allocation); BY CONTENT reads
`CallEmitter.CallContentRead` — the §13.18.38.4 GR8 current extent through `PlaceRenderer.SendingGroupImage` — at the
CALL snapshot and at the INVOKE BY CONTENT arm alike. Golden `2002/pb965_odo_group_argument_length`.

`CobolVarGroup` is `(string Fixed, string[] Dynamic)` and is the §8.5.1.12 model itself, not an encoding:
`Fixed` is the group's image with every variable-length component collapsed to nothing — the exact accounting
§8.5.1.12.3 states the relation in, which is why two COMPATIBLE groups lay it out identically — and `Dynamic`
carries each component's current content in declaration order, which §8.5.1.12.2's positional correspondence puts
one-for-one on both sides. A receiving dynamic-capacity table recovers its capacity by dividing its component by
its OWN element width, legitimate because §8.5.1.12.3 admits corresponding tables only "when the byte length of
their elements is equal". Nested variable-length groups FLATTEN into the same carrier (`CobolVarGroup.Slice`
hands one its window back), because the relation is stated over relative byte positions and is blind to the
declaration tree. The emitted pair is `AsVarImage()`/`FromVarImage()`, gated on `DataItem.CurrentExtentImageCapable`
— **the same** capability the §14.9.11.4 GR7 DISPLAY format uses, so a group that displays is a group that
crosses. The one crossing-form predicate is `CallEmitter.CallPlaceIsVarGroup` (CALL/RETURNING) and
`OoEmitter.OoCrossingType` (the INVOKE signature, box lanes and marshaling); the ADMISSION is decided once, at
bind, by `VariableLengthCompatibility.Mismatch` through `OoConformance.DescriptionMismatch` — the same comparator
the argument, RETURNING and override/implements checks all read.

**Tier-C at the boundary, in BOTH halves.** A group with no boundary image at all (a pointer/object-class leaf,
or a variable-length shape outside the current-extent gate — a runtime-length item inside an OCCURS DEPENDING or
dynamic-capacity table's element; `DataItem.BoundaryImageCapable`) stages the documented Tier-C loud rather than
crossing. (An OCCURS DEPENDING table BESIDE a dynamic member, and a runtime-length item inside a FIXED-OCCURS
element, cross since kb/Work PB244: the former rides the carrier's fixed run at its **maximum** — §14.8.2.2, "the
maximum length is used" — through `PlaceRenderer.VarGroupBoundaryImage`, the latter flattens `Occurs` times in place,
`VarPartKind.NestedTable`.) ⛔ The WRITE half does not test that predicate itself: `CallStringWrite` hands **every** non-`RedefViewPlace`
group to `PlaceRenderer.WriteFullGroupImage`, whose arm order owns the guard, so the read/write lockstep this
paragraph asserts is a STRUCTURAL fact rather than a coincidence two guards have to maintain. It was not, once:
the write half carried its own `IsImageCapable` conjunct and an imageless group fell through to a raw
`PlaceRenderer.Write`, rendering `_G = <string>;` — a backend CS0029 — while the read half correctly staged the
loud (kb/Work PB177 arm B, the eighth two-arm-dispatch instance). The exposure was not only at a CALL site: the
CALLEE's own LINKAGE formal copy-in (`ProgramEmitter`) and Report Writer's `CONTROL IS <group>` both call this
pair, and separate compilation means the caller-side `ArgText` screen gives the callee nothing.

**Area formals — group, REDEFINED and addressed formals share the argument's storage (kb/Work PB2087).** ISO §14.2.3 GR8: "If the argument is passed by reference, the activated runtime element operates as if the formal parameter occupies the same storage area as the argument." Elementary formals are CARRIER-RESIDENT (per-access aliasing through `__lnk{Uid}.Value`). Every other formal — a group, a formal another LINKAGE entry REDEFINES, a formal whose `ADDRESS OF` is taken — is an AREA formal (`LinkageFormal.IsArea`): `DataBinder.PtrBindBasedAndAddressables` moves its class onto a cell through the ONE forcer (`ForceStringCanonical`) and describes it exactly as a BASED item, whose implicit data-address pointer IS the formal's carrier field. At activation `CobolArgAdapt.Area` sets that pointer to the ARGUMENT'S OWN AREA — `CobolArg.Area`, the cell and the offset the argument begins at — so nothing is copied and a second formal passed the same argument, the activating element's own references while the activated one is active (a callback, a RECURSIVE re-entry), an EXTERNAL record also passed, and a contained program reaching a GLOBAL formal all see every store at once; `ADDRESS OF` the formal is the argument's address (§8.4.3.11.4 GR1), and a strongly-typed group's pointer / object-reference leaves cross whole through the cell's managed slots. The ACTIVATING half: the same pass claims onto a per-instance cell (the ADDRESS OF path, `ClaimAddressableCell`) every group and every character-class elementary item named as a BY REFERENCE CALL USING operand or a user-defined function argument (`CallScanByReferenceOperands`, `ClaimsAnArea`), so its `CobolArg` states its area (`CallEmitter.AreaFact`, also for a reference-modified or subscripted operand). Storage that is not a cell — a NATIVE numeric or pointer elementary item, a non-formal LINKAGE item, method data — crosses through its carrier alone, and so does every BY CONTENT argument (GR9's record is a copy) and every argument of a BY VALUE formal (GR10): `Area` then hands the formal a FRESH cell which the activation fills from the argument's carrier and, BY REFERENCE, stores back at return (`CobolArgAdapt.Aliased` tells the two apart). A VARIABLE-LENGTH group formal (a dynamic-capacity table or dynamic-length item — no data pointer can address such an area, §13.18.5.3 SR2; `DataBinder.CellCanCarry`) keeps the §8.5.1.12 image round trip, the one formal shape that still copies. Cost: a store into a cell-backed member rebuilds the cell string, O(width) — measured in the PB2087 landing; only boundary-aliased storage pays it, the same price every EXTERNAL record already paid. A strongly-typed group with a pointer leaf passed BY CONTENT stays refused at bind (COBOLNET1688, `ParameterConformance`): its copy would drop the references. The INVOKE lane (`OoEmitter`) still copies a group formal in and out at the method boundary.

**Carrier names and GLOBAL formals (kb/Work PB1009).** A formal's carrier is `__lnk{Uid}`, not positional: a GLOBAL formal (§13.18.27.3 SR1 b) admits GLOBAL in the linkage section) is referenced from every contained program by the SAME field text (`__lnk{Uid}.Value`) through a ref-bridge, and a positional name collided with the contained program's own formals. The bridges a contained program declares come from ONE list, `DataBinder.GlobalBridgesOf` — per root: the field, or a Tier-B backing plus the `StorageCell` behind a cell-backed class plus a BASED class's address pointer, or a resident formal's carrier; then the omitted-presence member and the index fields. A Tier-A (Alias) REDEFINES view owns no member, so a GLOBAL one bridges its CANONICAL's field (`DataBinder.AnchorOf`, the rule `ReferenceResolver.PlaceForItem` applies to every reference; §13.18.27.4 GR3 makes only the SUBJECT global, the shared storage is the original's), and `BinderDriver.BindUnitData` collects the roots' bridges into a SET keyed on the emitted member, because two global roots can name one member (the records of a GLOBAL FD share one Tier-B backing: kb/Work PB1523). `GlobalBridgeResidenceDriftTests` (Unit) runs one row per residence.

**Linkage referenceability — §13.7.3 SR4 and SR5 are decided once, from the header (kb/Work PB1249 / PB1250).** Once a unit's USING and RETURNING phrases are bound, `DataBinder.SealLinkageRules` (the program/function arm in `CallBindLinkage` and the method arm in `OoBindMethodData` call the same one) records which linkage RECORDS the procedure division may not reference: a record is referenceable when it is a header operand (a)), is BASED (the lead-in's "a based data item may be referenced as described in 13.18.5"), or REDEFINES one of those (c)); everything under it (b), d)), its RENAMES entries and its condition-names and index-names (e)) follow the record. A reference to anything else is COBOLNET2746, reported once per written reference by the ONE resolver (`ReferenceResolver.ScreenLinkageReference` for a data-name, the condition-name arm in `ConditionBinder.ConditionOf`, `ReferenceResolver.ScreenLinkageIndex` for an index-name; one wording, `ReportUnreferenceableLinkage`); the reference still resolves, so the statement binds without a cascade. The rule is per source element: a GLOBAL linkage item a contained program inherits is reached through §13.18.27.4 GR2 and is not asked. SR5 ("A formal parameter of a function shall not be used as a receiving operand") is an operand-ROLE rule, so it is one arm of `ExpressionBinder.ReceivingPlaceBar` (COBOLNET2747): every statement's receiver, and §8.4.3.2.4 GR5's BY-CONTENT choice for an argument, inherit it. The parameter is barred in any reference form (whole or reference-modified); an item SUBORDINATE to a group parameter is not "a formal parameter" (SR4 b) of the same clause says "subordinate to" when it means it), and the RETURNING item is no formal parameter.

**Nested-name resolution — one set, depth tiers (kb/Work PB1047 / PB1243).** A contained program's name set is §8.4.6.2.1 1)'s: its own names plus EVERY global name of every container. `BinderDriver.BindUnitData` inherits each container's GLOBAL roots whole (`DataBinder.InheritGlobalSubtree` — data-names, their condition-names, RENAMES, index-names and the named OCCURS DYNAMIC CAPACITY registers (§13.18.38.3 SR30 places a register at the OCCURS entry's level, so under the global record; kb/Work PB1674), each tagged with the container's nesting depth; `GlobalNameClassDriftTests` finds every name index of `DataBinder` by reflection and holds that list total), never skipping a root whose spelling this unit also declares: §8.4.6.2.2 makes each subordinate of a global name global in its own right, so a local `01 G` hides the container's `G`, not its `G.Y`. WHICH item a reference names is decided at reference time, after qualification over the whole set, by §8.4.6.2.1 3)'s nearest-declaring-element rule — `SymbolTable.NearestDeclaring`, asked by `DataBinder.QualifiedCandidates` and `ConditionBinder.ConditionOf` after qualification, and — through `SymbolTable.TryResolveUnqualified`, the raw set narrowed at once because there is no qualifier — by every UNQUALIFIED lookup: `ReferenceResolver.ResolveUnqualified`, and the lookups that pick a candidate BY KIND, which filter the narrowed set and never the raw one (`IntrinsicBinder.FunctionPointerNamed` for §8.4.3.2.3 SR4, `InitializeBinder`'s whole-dynamic-table arm — filtering first counted a hidden global function-pointer as a rival, and initialized a container's global table in place of a nearer local item); `SymbolTable.IndexCandidates` and `ReferenceResolver.CapacityRegisterFor` weigh a nearer data-name against the index-names' or the register's depth through the one `SymbolTable.NearerDataNameHides`, and `DataBinder.DropShadowedConstants` a nearer data-name against an inherited global constant's. Two survivors in the one nearest element are still §8.4.2.2.3 SR1 ambiguity. Because every global root is now bridged, a local root spelled like one takes a distinct C# member name (`DataBinder.ReserveInheritedMemberNames`, run before the unit's data division binds, over `DataBinder.InheritedMemberNamesOf` — every bridge member AND the root's own C# name, the STEM its derived members `_redef_` / `_scell_` / `__addr_` are spelled from, so a local REDEFINES or BASED root's backing or address pointer never re-spells a container's bridged one; a derived member is therefore spelled from the root's C# name, never its COBOL word — `NamingConvention.AddressCarrierName` takes the C# name). An `ADDRESS OF` a global name taken in a CONTAINED program forces the cell in the CONTAINER, which owns the storage (`PtrBindBasedAndAddressables` scans the nested programs for this unit's GLOBAL names), and `ADDRESS OF` renders the cell the class names (`RedefinesClass.BackingCellCsName`), bridged or not.

### RECURSIVE programs and functions/methods need per-activation data; a `static class Program` cannot recurse or hold per-activation copies.

Make each program an instantiable class. RECURSIVE/function/method → new instance per activation; plain program → cached singleton (last-used persistence). Non-RECURSIVE re-entry while active → EC-PROGRAM-RECURSIVE-CALL. This forces instance-ization of the program-class shape now (today's static Program is replaced).

### Opaque ABI for dynamic CALL / cross-assembly: the caller cannot see the callee's LINKAGE to build typed args.

Uniform ICobolProgram.Call(CobolArgs) where CobolArgs is an ordered list of (PassMode, caller PicMeta, carrier) — the typed analog of the rejected ManagedPointer[]. Callee maps positionally onto LINKAGE items. Same-assembly + statically-resolvable + PIC-conforming calls specialize to a direct typed method (Run) as a fast path.

### GOBACK and EXIT PROGRAM terminate a unit differently by activation context — and the two statements DIVERGE in the not-under-a-caller (main) case.

Distinguish by BOTH the statement and the activation context (§14.9.18.4 / §14.9.14.4):
- In a CALLED program (under the control of a calling runtime element) GOBACK and EXIT PROGRAM both terminate the program and return control to the activator (§14.9.18.4 GR2 / §14.9.14.4 GR3); the activation result is the current value of the PROCEDURE DIVISION header RETURNING item (§14.9.18.4 GR2), and a RAISING phrase stages its exception condition for the activator.
- In the MAIN program (not under the control of a caller) the two DIVERGE: GOBACK operates as a STOP statement and terminates the run unit (§14.9.18.4 GR3 — a RAISING phrase is then ignored, and a WITH NORMAL/ERROR STATUS reaches the OS as the exit code); EXIT PROGRAM is treated as a CONTINUE — a no-op that falls through to the next statement, raising no exception condition even when RAISING is specified (§14.9.14.4 GR2).

Mechanism (settled, SSOT §9.4 + §18 #10): GOBACK raises `ProgramReturn`; EXIT PROGRAM raises `ProgramReturn` ONLY while the unit is active as a callee (guarded on the `__asCalled` activation flag) and is inert otherwise. A `ProgramReturn` is caught at the unit's own activation/dispatch boundary: for a called unit it returns control (and the header RETURNING value) to the activator; for the main unit's GOBACK it returns to that boundary and the run unit then ends, the main-program status flushing as the process exit code. The distinct `StopRun` signal (`src/Cobol.Net.Runtime/Control/Signals/StopRun.cs`) is the run-unit-termination half (STOP RUN, and the STOP-status exit-code flush); `ProgramReturn` is the called-program-return signal, not a refinement of `StopRun`.

### CANCEL must reset a program to initial state on next CALL, close its open files, cascade to contained programs, and be a no-op for never-called/already-canceled or active programs.

Registry.Cancel(name) drops/marks the singleton for re-init (WS reset to VALUE on next CALL), runs implicit CLOSE on its open files (§14.9.5 GR9), cascades to contained programs in reverse order (GR4), raises EC-PROGRAM-CANCEL-ACTIVE if active (GR5), no-ops if never-called/canceled (GR7); EXTERNAL data is NOT reset (GR8).

### Arguments must be evaluated exactly once at CALL time (§14.9.4.4 GR3a) even though carriers are lazy accessors.

Capture subscript/ref-mod bound expressions into locals BEFORE constructing carriers (e.g. `int _i=(int)I-1;` then close over `_i`), so re-evaluating the accessor inside the callee does not re-evaluate the COBOL subscript expression.

## Edge cases

- Transitive passing mode: at the CALL site the BY CONTENT and BY REFERENCE phrases are transitive across the following arguments until the next such phrase, defaulting to BY REFERENCE (§14.9.4.4 GR5 — a Format-1 CALL has no BY VALUE); BY VALUE (only in the Format-2 program-prototype CALL) rides the argument-correspondence BY REFERENCE/BY VALUE transitivity (§14.2.3 GR4). One CallBinder mode-threading pass threads all three, drop its byte emission. **The Format-1 narrowing's `--permissive` lane (kb/Work PB162):** §14.9.4.2 Format 1 admits `identifier-2` only, so BY VALUE, a BY CONTENT literal / expression and a bare literal / expression on a CALL with no AS phrase are COBOLNET0899 errors under strict; `--permissive` reports the same code as a warning and binds each with the Format-2 semantics it spells (a detached BY CONTENT value; the Value mode) — the vendor spellings GnuCOBOL, IBM and Micro Focus accept. All five sites frame through ONE helper, `CallBinder.Format1VendorSpelling`.

**FORMAT 2'S ARGUMENT CLASSIFICATION — ONE reduction, then the mode (§14.9.4.4 GR8 + GR9; kb/Work PB238).**
GR8 — "An argument that consists merely of a single identifier or literal is regarded as an identifier or
literal rather than an arithmetic or boolean expression" — is a rule about EVERY Format-2 argument, not about
one spelling of one. Two grammar facts make it load-bearing and they are the same two INVOKE faces:
`arithmeticExpression` subsumes `dataReference` and every numeric literal, and the `{boolExprAhead()}?`-gated
`booleanExpression` alternative subsumes both of those in turn (its leaf is `valueOperand`, and the predicate's
scan runs to the statement's period, so an earlier argument can land in a boolean node on a LATER argument's
B-operator). `CallBinder.Gr8Classify` is therefore THE reduction, shared by the BY CONTENT arm, the BY VALUE
arm and both keyword-less arms: it unwraps an operator-free boolean node to its bare `valueOperand` — **both**
legs, `arithmeticExpression | nonNumericLiteral` — and recovers a sole `dataReference` from an expression node.
Only the residue binds as an expression.

The MODE then comes from GR9, never from the copy-out: a keyword-less argument that meets §14.9.4.3 SR3 is
identifier-2 and takes BY REFERENCE (9a1); one that does not — a literal, a constant-name (which §13.10.4 GR1
substitutes a literal for), an object property — takes BY CONTENT (9a2); and a BY VALUE formal makes it BY VALUE
(9b) whatever the argument is. §14.9.4.3 SR20's carve-out is the object-property leg: "BY CONTENT may be omitted
when identifier-4 is an object property", so a bare property reference is a SENDING occurrence (SR17) and
`ReferenceResolver.IsObjectPropertyReference` routes it to Content — binding it BY REFERENCE made
`BoundStores` classify the occurrence ReadWrite and invoke the §8.4.3.9.4 SET accessor. SR23 ("literal-2 shall be
a numeric literal" when it OR ITS FORMAL carries BY VALUE) is `COBOLNET1762`, screened on both spellings and, for
a constant-name, on the literal §13.10.4 GR1/GR2 substitutes.
- Bare argument resolution (§14.9.4.4 GR9): a bare arg with a BY REFERENCE formal becomes BY REFERENCE if it is a valid receiving operand, else BY CONTENT (e.g. a literal/expression).
- **OMITTED / trailing-omitted argument (GR11–12), and FORWARDING one (kb/Work PB133 wave C → PB165).** The
  carrier is Null; the §8.8.4.8 omitted-argument condition IS `IsNull`; referencing an omitted formal outside
  the two sanctioned forms raises EC-PROGRAM-ARG-OMITTED **at the reference, in the callee** (checking off stays
  lenient with the documented benign-empty read — GR12 leaves the content undefined).
  ⛔ **THE RAISE IS THE REFERENCE'S, KEYED TO THE OWNING ELEMENT'S KIND (kb/Work PB971).** §14.9.4.4 GR12
  (program), §8.4.3.2.4 GR8 (function) and §14.9.23.4 GR10 (method) are one rule written three times, so it lives
  in one place per half. BIND: `EcBinder.MarkFormals` gives every formal's root `DataItem` an
  `OmittedFormalGuard` (presence expression · `ActivatedElementKind` · name) — only when the compilation group can
  enable the kind's name at all, so a unit with no such >>TURN renders byte-identically — and every access-path
  builder roots through `ReferenceResolver.RootOf` / `RootText`, so EVERY rendered reference (any verb, any operand,
  a subordinate, a REDEFINES view, a subscript or ref-mod position, a GLOBAL formal from a contained program) wraps
  its root in `OmittedFormal.Ref(ref …)` / `OmittedFormal.Carrier(…)`; and every statement queries >>TURN for
  `OmittedFormal.ConditionName(EcBinder.ElementKindOf(element))`, so the statement's §7.3.25.4 scope sets the flag
  the guard tests. RUNTIME: `OmittedFormal` raises through the kind's checked helper
  (`Program`/`Function`/`OoArgOmittedError`); the omitted CALL carrier raises NOTHING any more. Before PB971 the
  name was queried only around a CALL (as if the activator raised it), so no referencing statement ever enabled
  it; the carrier's read raised the PROGRAM name in a function; and a group formal's boundary copy and a method's
  copy-in local were never checked. The presence member of a program/function formal is `__omit{Uid}`
  (`ProgramEmitter`), bridged under the same name into a contained program for a GLOBAL formal (`CallBridge`
  kind "presence"); a method's is its `__omittedN` parameter. Pinned by `OmittedFormalGuardDriftTests`.
  GR12's exemption — *“except as an argument”* — and §8.8.4.8.4 GR1c's TRANSITIVE omission are ONE emitted
  fact: `CallEmitter.WholeFormal` recognizes an argument that IS a whole formal parameter **by identity against
  the unit's own `LinkageFormal` items** (`CallUnitState.Formals`) — never by a `__lnk` name match, which could
  only see a CARRIER-RESIDENT formal — and `CallEmitter.Forwarded` guards EVERY mode's carrier build with the
  incoming carrier's presence, so the forward neither reads the formal (a BY CONTENT snapshot is a read) nor
  loses its omitted state (a group formal's copy-in field always answers `IsNull` false). A SUBITEM, a
  subscripted reference and a reference-modified view are deliberately excluded: GR1c speaks of an argument
  that *is* a formal parameter, and referencing inside an omitted one is exactly the error GR12 states.
  ⛔ **The recognition and the presence test span EVERY activation ABI (kb/Work PB757).** The presence fact is
  `Binding/Model/OmittedProbe` — `Carrier` (a program/function formal's null carrier) or `MethodFlag` (a method
  formal's `bool` presence parameter, COBOLNET_OO_DESIGN D6) — and `CallUnitState.WholeFormalProbe` recognizes a
  whole formal of EITHER kind (`Formals` for a program, `MethodFormals` inside a method body). `CallEmitter.OmittedTest`
  is the one rendering; the §8.8.4.8 condition, `Forwarded` (CALL and user-defined-function arguments) and the INVOKE
  argument lowering all call it, so a CALL inside a method forwards an omitted method formal as the null carrier and
  an INVOKE inside a program forwards an omitted program formal as `true`.
- **User-defined-function arguments ride the same model (§8.4.3.2.3 SR9, §8.4.3.2.4 GR7; kb/Work PB757).** `UdfBinder`
  binds `FUNCTION f(… OMITTED …)` to an `Omitted` `BoundCallArg` (the null carrier above) when the formal is OPTIONAL —
  `COBOLNET2238` otherwise — and admits fewer arguments than formals when every trailing formal is OPTIONAL
  (§14.8.2.1), the callee's adapters answering a missing slot as omitted exactly as for CALL.
- **A function-identifier's arguments evaluate LEFT TO RIGHT (§8.4.3.2.4 GR2; kb/Work PB1423).** A user-defined
  activation is a statement-scoped PRE-op, hoisted ahead of the statement that reads its result, so an earlier argument
  that merely reads state (an identifier, an arithmetic expression, an intrinsic result, a subscripted item) used to be
  rendered AFTER a later argument's activation had changed it. ONE mechanism orders both function kinds:
  `ArgumentOrder.Window` is opened around an argument list by `IntrinsicBinder.BindIntrinsicCore` (the one point every
  intrinsic result passes) and by `UdfBinder.UdfActivate`, `IntrinsicBinder.BindArgOperand` records each operand with
  the pending pre-op range it registered, and when a LATER argument registered pre-ops every earlier argument whose
  value is read at the activation is stored into the §14.9.25.4 GR1 intermediate result item
  (`SendingValueTemp.Materialize`) at the position its own argument finished binding. A literal, a compiler temporary
  and a BY REFERENCE identifier of a user function (§8.4.3.2.4 GR6a: values are "made available … at the time control is
  transferred") are not frozen. `ArgumentOrderDriftTests` pins the two entries and the one argument binder.
- Argument/parameter count mismatch → EC-PROGRAM-ARG-MISMATCH (when checking enabled) or diagnostic; a missing parameter behaves as omitted.
- **Argument DESCRIPTION conformance is ONE rule set, written once, for CALL, the function-identifier and INVOKE
  (§14.9.4.3 SR25 / §8.4.3.2.3 SR13 → §14.8.2; kb/Work PB133 → PB204 → PB165 → PB1418).** Wherever the activated
  element's PD header is known at BIND — the AS NESTED containment table, a program prototype's §12.3.8.4 GR10 a)
  definition, or a function prototype (directly, or through a function-pointer's USAGE TO phrase) — the activating
  binder asks `ParameterConformance.CheckArgument` for every argument, and every rule it applies lives in
  `OoConformance`: `DescriptionMismatch` for
  §14.8.2.3.2 / §14.8.2.2 (BY REFERENCE, identical description with the rule-1 group-prefix allowance and
  §8.5.1.12 variable-length compatibility), and `ContentMismatch` + the four value-shape rules for §14.8.2.3.3
  (BY CONTENT / BY VALUE: 2a COMPUTE for a numeric formal, 2b SET for an index item, 2c ANY LENGTH, 2d MOVE via
  §14.9.25.3 Table 16, plus the class-pointer / object-reference SET paragraph).
  `ParameterConformance.ContentConformanceReason` dispatches on the BOUND argument's shape — identifier, boolean
  expression, arithmetic expression, alphanumeric literal, numeric literal, and an intrinsic function-identifier
  by its RESULT category (§15.4) — and an arithmetic expression into a formal that is not numeric is asked rule 2 d)'s
  MOVE question of its VALUE (a numeric sender on Table 16's noninteger row: edited into a numeric-edited formal,
  refused into an alphanumeric, national, group, pointer or object-reference one; kb/Work PB1946, verdict PB1936;
  `ArgText` crosses it through `ConvertSource`, the numeric literal's twin) — and a constant-name needs no arm
  because §13.10.4 GR1 has already substituted its literal. The verdict is COBOLNET1688 for a CALL and its twin
  COBOLNET2470 for a function-identifier, the same code the BY REFERENCE arm uses, because it is the same
  obligation. The same collaborator owns the bit-alignment proof (§14.9.4.3 SR6/SR8 and §8.4.3.2.3 SR14 —
  COBOLNET1683 / COBOLNET2471) and the BY VALUE class answer (§14.9.4.3 SR22 / §8.4.3.2.3 SR10 —
  `ValueArgumentClass`). ⚠ The function-identifier (`UdfBinder`) used to re-implement CALL's argument binding
  WITHOUT this regime, so a non-conforming function argument aliased its formal silently (kb/Work PB1418 /
  PB1115); `ActivationConformanceDriftTests` pins both lanes to the shared routine. INVOKE (`OoBinder`) still
  dispatches the same `OoConformance` comparators from its own lane for identifier arguments, but asks
  `ParameterConformance.ContentConformanceReason` for its literal-2 verdict (kb/Work PB1137), so a literal gets one
  answer from all three activations; a written nonnumeric literal is judged by its OWN category
  (`OoConformance.ContentNonNumericLiteralMismatch` — a national literal does not move to an alphanumeric formal),
  a figurative / ALL literal under any category.
- **The activation OPERAND screens are shared by CALL and INVOKE (kb/Work PB1137).** §14.9.4.3 and §14.9.23.3 state
  two operand rules in the same words, and `ParameterConformance` checks both for either statement: the
  STORAGE-SECTION rule — a BY REFERENCE argument is "an address-identifier or … a data item defined in the file,
  working-storage, local-storage, or linkage section" (CALL SR3, INVOKE SR9) and the RETURNING item such a data
  item (CALL SR7, INVOKE SR11) — answered once from the resolved place by `Model/SectionDataItem` (a report's
  PAGE-COUNTER, a sum counter, DEBUG-ITEM, EXCEPTION-OBJECT and every compiler temporary are not; an object property
  is admitted by §8.4.3.9.3 SR5/SR6) and reported by `ScreenSection` (COBOLNET1677 / COBOLNET2452); and the
  BIT-ALIGNMENT proof (CALL SR6/SR8, INVOKE SR12 — COBOLNET1683 / COBOLNET2453). The keyword-less MODE TEST both
  general rules build on it — §14.9.4.4 GR9 a) (Format-2 CALL) and §14.9.23.4 GR6 a) (INVOKE): BY REFERENCE when
  the argument meets the section rule and is not factory/instance object data, BY CONTENT otherwise — is
  `MeetsByReferenceRules`, asked BEFORE the operand is resolved because the mode decides its role (receiving vs
  sending). A keyword-less object property or PAGE-COUNTER therefore crosses BY CONTENT; through a UNIVERSAL
  receiver (SR6: BY REFERENCE only) the same operand is refused.
- **A function argument's passing manner is §8.4.3.2.4 GR5's (kb/Work PB1418).** BY VALUE when the formal is BY
  VALUE; BY REFERENCE for "an identifier that is permitted as a receiving operand, other than an object property
  or object data item" — asked of `ExpressionBinder.PermitsReceiving`, which consults the SAME prohibition table
  (`ReceivingFormBar` / `ReceivingPlaceBar`) the receiving chokepoint `ResolveReceiving` reports, so a CONSTANT
  RECORD item (§13.18.15.3 SR2) crosses BY CONTENT and the function can no longer overwrite the structured
  constant; BY CONTENT for every other shape — a literal, an arithmetic or boolean expression (the boolean rides
  CALL's `ContentBool` channel), a function-identifier (§8.4.3.2.3 SR1), an object property or object data item.
  A character-valued intrinsic function-identifier crosses on the string channel (`CallEmitter.ArgText`).
- **A figurative constant or ALL literal argument FILLS the formal's allocated record (kb/Work PB1418 + PB1617).**
  It is a literal (§8.3.3.6.3 SR1), so it crosses BY CONTENT, and §14.2.3 GR9's second branch — a NESTED or
  prototyped program, a method, a function — allocates "a data item with the same description and the same number
  of bytes as the formal parameter, where the maximum length is used if the formal parameter is described as a
  variable-occurrence data item" and MOVEs the argument into it; §8.3.3.6.4 GR2 repeats the figurative to that
  record's character positions. ONE helper, `CallEmitter.FigurativeArgumentImage`, renders the value for all three
  lanes (CALL and function through `ArgText`; INVOKE through `BoundInvokeArg.ContentFill`), at
  `FigurativeFillWidth` = the formal's text-crossing window (`BoundaryImageWidth`): an elementary non-numeric
  item's positions, an alphanumeric group's record image (an OCCURS DEPENDING group at its MAXIMUM, per GR9 — no
  run-time extent is involved), a bit / national group's as-if positions (§14.8.2.1 NOTE), in the formal's own
  category for HIGH-/LOW-VALUE. Where GR9 gives no fixed record — no formal known (the FIRST branch: "of the same
  length as the argument"), a numeric formal (the COMPUTE reads the value), ANY LENGTH, DYNAMIC LENGTH — the
  figurative keeps its §8.3.3.6.4 GR3 length (one character / one literal-1). A VARIABLE-LENGTH group formal
  (§8.5.1.12) is not a fill case at all: §8.5.1.12.1 bars a move into it from anything but a compatible group, so
  `ParameterConformance` refuses it at bind. It used to carry one occurrence into a group formal (`"*   "`), and
  INVOKE refused every figurative but NULL as "not yet carried".
- **A GROUP formal asks the WHOLE MOVE question (kb/Work PB1617).** §14.8.2.2 rule 2 makes a BY CONTENT argument's
  conformance that of "a MOVE statement with the argument as the sending operand and the corresponding formal
  parameter as the receiving operand", so a literal, ALL literal, figurative or numeric literal argument into a
  group formal takes `MoveTable16.Validity` — §14.9.25.3 SR2 (a strongly-typed group accepts only its own type)
  and SR9 (a variable-length group only a compatible group) included — never a blanket "conformant". Its
  class-pointer / object-reference twin: such a formal takes §14.8.2.3.3's SET paragraph, so a literal or any
  figurative but NULL is refused at bind (it used to pass Table 16 and die at the callee's managed slot).
  ⚠ The §14.8.2.3.3 rules were once PRIVATE to INVOKE, and
  the CALL lane therefore had no by-content screen at all while `CobolArgAdapt`'s converting views silently
  adapted whatever arrived; EXTRACTION, not a second copy, is what closed it. The DYNAMIC Format-1 lane checks the
  COUNT, and each argument's LENGTH and group CLASS, at runtime (`BoundaryItem.ArgumentConforms`, the bullet below
  on the argument's description; kb/Work PB165); a Format-2 CALL whose program-prototype has no compile-time
  signature (§12.3.8.4 GR10 c) is not yet checked, which is P13's prototype registry.
- **"The same PICTURE clause" is ONE identity (kb/Work PB1166).** Every identical-description rule — §8.5.3.1,
  §9.3.6 3), §9.3.8.2.3 rules 3/6, §14.8.2.3.2 rule 2, §14.8.3.3 — carries the same two exceptions (currency
  symbols match iff their currency STRINGS do; period/comma symbols match iff DECIMAL-POINT IS COMMA is in effect
  for both source elements or neither), and both are facts of the DECLARING source element, not of the
  character-string. `PictureAnalyzer.Analyze` attaches `PicInfo.Clause` (`PictureClauseIdentity`) to every analyzed
  profile; `DescriptionMismatch` compares it for every category (so `PIC A(5)` is not `PIC X(5)`), the §8.5.3.1
  profile compare gets it through `PicInfo` record equality, and the non-PICTURE clauses ALIGNED / DYNAMIC LENGTH
  are the one `DescriptionClauses` predicate both read. The LOCALE phrase compares the external identification AS
  WRITTEN (`LocaleSymbol.SameExternalIdentificationAs` — literal values ordinal, a word case-insensitively), never
  the L1-normalized tag that selects the locale at run time.
- **A bit / national group is an ELEMENTARY item at the boundary** (§14.8.2.1 / §14.8.3.1 NOTE): it matches an
  elementary bit / usage-national item of the same position count (§14.8.2.3.2 "Additionally" b)/c), §14.8.3.3
  2)/3)) and never an alphanumeric group, and it CROSSES in the elementary alphabet — `CallEmitter.CallStringRead` /
  `CallStringWrite`'s as-if arm (AsBits/FromBits, AsNat/FromNat over the full allocation), which the program ABI,
  the method ABI (`OoEmitter.MethodBoundaryValue` and the formal copy-in) and every INVOKE copy route through;
  `CallEmitter.BoundaryImageWidth` is its text width and it carries no §8.5.1.12 layout.
- RETURNING a group item: an **image-form** group — every leaf `DataItem.ElementImageCapable`, i.e. character-stored OR any pinned numeric byte form (zoned DISPLAY, binary, packed, COMP-5, IEEE float, INDEX) — is carried; the caller temp deep-clones the description and the image crosses via AsImage/FromImage (§8.4.3.2.4 GR1; §14.2.2 SR5 places no category restriction, and none on usage either). **A function's result temporary has NO category restriction at all (kb/Work PB1419):** FLOAT, BOOLEAN, INDEX, data-pointer, object-reference and every group shape (strongly typed, internal REDEFINES, variable length) are carried by the receiver's own crossing form (`CallEmitter.CrossingOf`), and `UdfBinder.UdfReturningResidue` with its per-shape COBOLNET1510 refusals is deleted. The boolean-expression channel reads a boolean-returning user function through `ConditionBinder` (a `BoundBoolRef` over the result temp) beside the intrinsic `BoundBoolCall`. What is NOT yet accepted is a function-identifier of class pointer / object / index as the SENDER of a SET statement or as a CALL USING argument (those binders classify a function-identifier as an arithmetic expression), which is a consumer routing matter of its own and not a property of the result temp.
- **A CALL's RETURNING item conforms by §14.8.3.3, not §9.3.8.2.3 (kb/Work PB1164).** `ParameterConformance.ReturningConformanceReason` is the ONE returning-item half of the returning-items clause, asked by the Format-2 CALL and by INVOKE: two object references conform "as if a SET statement were performed" (§14.8.3.3 rule 1) from the activated returning item into the activating one (a subclass-typed sender into a class-typed or universal receiver is legal; the superclass-into-subclass direction is refused), everything else keeps the description comparison. The run-time delivery (`CobolArgAdapt.StoreReturn<T>` over `ManagedPointer.TryAssign`) stores a subclass-typed result into a superclass-typed or universal carrier.
- **RETURNING delivery is TOTAL (§14.9.4.4 GR4; kb/Work PB165).** With no caller target the value is discarded —
  GR4 has no receiver. With one, `CobolArgAdapt.StoreReturn` **stores or raises**, never no-ops: legs exist for
  every `ElementType` the compiler emits (`long`/`ulong`/`Int128`/`UInt128`, `double`, `string`,
  `CobolVarGroup`, `ManagedPointer`, `ProgramPointer`, `CobolObject`), and a pair with no leg raises
  `CobolCallException("EC-PROGRAM-ARG-MISMATCH")` citing §14.8.3 and §14.9.4.4 GR3d. (The `double` leg was
  missing until kb/Work PB962's sweep: a `USAGE FLOAT-LONG` returning item did not compile, CS0315.) A silent
  discard is the one outcome GR4's *“is placed into identifier-3”* excludes.
- **⛔ The RETURNING item crosses as a `CobolArg` — carrier AND description (kb/Work PB962 + PB965).**
  `ICobolProgram.Call(CobolArg[] args, CobolArg? returning)`: the activating element builds the returning item's
  `CobolArg` exactly as it builds a BY REFERENCE argument's (`CallEmitter.ReturningArgText` over the one
  `PlaceDescription`), because its storage is the activating element's (§14.2.3 GR6 NOTE 1) and the delivery,
  which runs in the activated element, needs the RECEIVER's shape. The activated element passes its SENDING
  item's description beside the content (`ProgramEmitter.ReturningDelivery`). **The delivery is a CONTENT
  transfer** — §14.6.5: the result "is the content of the data item referenced by that RETURNING phrase" — under
  the one description a conforming pair shares (§14.8.3.3 requires the same PICTURE and USAGE): a fixed-point
  item's native value lands in a native cell and, in an image-carried receiver, as its representation under the
  description (`CobolNum.FormatImage` — the value's C# text dropped the sign's over-punch); an image-carried
  item's STORAGE image lands as it stands in an image-carried receiver and decoded under the description
  (`CobolNum.ParseImage`, whose zoned arm answers for any content) in a native cell. Before PB962 the text leg re-parsed
  the content as a C# number and ABORTED the run unit whenever it was not a digit run (spaces). ⚠ A NATIVE cell
  holds a value, so content that is not a valid numeric representation reads as the value `ParseImage` gives
  it — the same residue every character view of a native numeric cell has. The character legs of a
  description-free sender (an alphanumeric item into a numeric receiver — a pair §14.8.3.3 does not admit) still
  read the result's digit image, loud when there is none. An image-carried item's boundary text IS its storage,
  of every byte form (kb/Work PB970), so `CallEmitter.CallStringWrite` stores it as it stands (fitted) rather than
  decoding and re-encoding it, which lost exactly the non-digit content (kb/Work PB962).
- **⛔ The RETURNING pair's CONFORMANCE on the dynamic Format-1 lane (§14.9.4.4 GR3 d), §14.8.3.3; kb/Work PB1040).**
  The AS NESTED lane checks the pair at bind (COBOLNET1736); a CALL located by name at run time has no bind-time
  screen, so the check is a call-initiation one over facts BOTH sides state: `BoundaryItem(Num, Length, Class)` — a numeric
  item's `NumProfile` and a text-carried item's fixed character length — which the activating element sends on the
  returning `CobolArg` (`CobolArg.Length` beside `Num`/`Layout`; `CallEmitter.BoundaryLength` is the one measure, in
  `CallStringRead`'s unit) and the activated unit REGISTERS beside its formal counts (`ProgramRegistry.Register(…,
  returning:)`, `CallEmitter.RegisteredReturning`). `ProgramTable.CallProgram` compares them with
  `BoundaryItem.Conforms` before the activation — numeric-vs-character never conforms, two profiles conform when they
  state one PICTURE, SIGN and USAGE (`NumProfile.ConformsTo`), two lengths when equal; an item that states nothing
  (a pointer, a DYNAMIC LENGTH / ANY LENGTH item, a variable-length group) is not compared — and a violation is
  EC-PROGRAM-ARG-MISMATCH "if checking for it is enabled in both the activated program and activating runtime
  element" (`siteArgMismatchChecking && node.ArgMismatchChecking`, the same gate as the count check), with "the
  program call is not successful": the callee never runs and the receiver is untouched. **Unchecked**, the standard
  leaves the content to the implementor, and a text-carried receiver's string is its item's whole image, so every
  `StoreReturn` text leg stores into the receiver's OWN width (`FitToReceiver`: §14.6.8.5's left-justified,
  space-filled, right-truncated alphanumeric alignment; a numeric receiver reads the value the sender held under its
  own description). Every unit registers its count facts, a unit with no formals included (zero formals is a count),
  and every activation arm — by name, through a program-pointer, through a function-pointer — passes the activating
  half of the gate. `BoundaryItem` also carries the group class (`BoundaryClass`: §14.8.3.2 pairs an alphanumeric
  group only with a group or an elementary alphanumeric item of the same length, and lifts for a strongly-typed or
  variable-length group).
- **⛔ The ARGUMENT's description on the dynamic Format-1 lane (§14.9.4.4 GR3 d), §14.8.2.3.2 / §14.8.2.3.3 rule 1,
  §14.8.2.2; kb/Work PB165).** The same `BoundaryItem`, one per FORMAL: a unit that checks EC-PROGRAM-ARG-MISMATCH
  registers `formals:` at `ProgramRegistry.Register` (`CallEmitter.RegisteredFormal`), and a CALL site that checks it
  states `Length:` and `Class:` on each data-item argument's `CobolArg` (`CallEmitter.ArgumentFacts`), so an unchecked
  program's emitted text does not change. ONE measure on both sides: `CallEmitter.ArgumentLength` — the item's
  BYTE-LENGTH (`DataItem.ImageWidth`, for a text-carried or a native item) — and `CallEmitter.BoundaryClassOf`.
  `ProgramTable.CallProgram` compares each pair with `BoundaryItem.ArgumentConforms` under the enabled-in-both gate,
  before the callee runs: elementary items by the rule "same length" (§14.8.2.3.2 / §14.8.2.3.3 rule 1, mode-blind), an
  alphanumeric group BY REFERENCE by §14.8.2.2 1) (the other item a group or an elementary alphanumeric item, the
  formal no longer than the argument), BY CONTENT by 2) (a MOVE, no length relation). The length rule is that of the
  lane "a program with no program-specifier and no NESTED phrase", so `CallEmitter.IsDynamicLane` keeps it off a
  function, a NESTED call, a program-specifier's program and an identifier-named target in an element that writes any
  program-specifier — rule 2 converts there and the binder checks it where the callee is known. The facets beyond length
  and class (JUSTIFIED, BLANK WHEN ZERO, ALIGN, the PICTURE's currency and locale facets: rule 2's identity, for a
  prototype with no compile-time signature) are the next extension of `BoundaryItem`, not a second shape.
- CALL to a NULL program-pointer → EC-PROGRAM-PTR-NULL; unresolvable name → EC-PROGRAM-NOT-FOUND; both are activation failures and take the GR3h partition below.
- **The GR3h/GR3i partition of a failed activation, and the ACTIVATION BOUNDARY that makes it decidable.** §14.9.4.4 GR3h routes a failure on THREE independent facts, and the emitted CALL expresses each one separately (`CallEmitter.EmitCall`; kb/Work PB233):
  1. **Which phrase is written.** Only ON EXCEPTION diverts — GR3h item 1 names it, and §14.6.13.1.3 #1 admits only "a conditional phrase without the NOT phrase". A CALL carrying only NOT ON EXCEPTION is governed by item 2 or item 3 exactly as a phrase-free CALL is. (It formerly emitted the catch on *either* phrase and silently discarded the failure.)
  2. **Which family the condition belongs to.** Item 1 admits "any of the EC-PROGRAM or EC-EXTERNAL exception conditions"; every other condition the carrier can raise — today EC-FUNCTION-NOT-FOUND (§8.4.3.2.4 GR6b) — takes item 2's second disjunct, i.e. the applicable exception processing statements, with no ON EXCEPTION escape. The partition is written once, as `CobolCallException.IsProgramOrExternal`, and the carriable name set once as `CobolCallException.CarriedNames` (kept equal to the raise sites by `CallExceptionCarrierDriftTests`).
  3. **Whether control had already been transferred.** GR3h speaks only of a program that "was not successfully called"; GR3i says that once it was, "the ON EXCEPTION phrase, if specified, is ignored". The CALL site cannot know this — the runtime does, so `ProgramTable.CallProgram` marks `CobolCallException.ControlTransferred` on anything escaping `inst.Call`, and every emitted arm filters on `!ControlTransferred`. The mark is monotone, so a failure deep in a call chain is an activation failure for exactly one CALL site and post-transfer for every site above it.
  Because the boundary is "escaped from `inst.Call`", GR3e's external-conformance check — an activation-ATTEMPT step that precedes GR3g's transfer — is hoisted OUT of the callee body onto the ABI as `ICobolProgram.DescribeExternals()`, which the boundary calls before `Call`. The main-program entry (`Activate`) calls it itself. §14.8.4.1's ACTIVATING-element half is the CALL statement guard's EC-EXTERNAL checking flags (`EcEmitter.FatalAmbientGates`), read as `ExceptionEngine.ExternalActivatingMask` before the callee's checking scope opens and latched as `ActivatorExternalMask`; the flags are a saved-and-restored scope, so no failure path can leak them (the pending CALL-site mask register this replaced had to be zeroed by hand on every path). The INVOKE twin (§14.9.23.4 GR7 d), kb/Work PB1138) runs the same pairing in a method's prologue: `ExternalStore.DescribeAtMethodActivation` registers the containing factory's or object's external items with the METHOD's own mask (`OoMethodSymbol.ExternalCheckMaskHere`) and turns a violation into a `CobolFatalException` the INVOKE statement's guard selects on, as it does EC-OO-METHOD.
- ON EXCEPTION / ON OVERFLOW edition surface: `[NOT] ON EXCEPTION` is ANSI X3.23-1985 CALL Format 2 surface (CCVS-85 IC222A tests both phrases), valid at EVERY edition; `ON OVERFLOW` is the 74-carried synonym, valid 85–2014 and REMOVED at 2023 (`VERSION_CHANGE_REFERENCE.md` row 3 / E.2 item 1c → COBOLNET0882). Both spellings bind to the same phrase fields, so the GR3h partition above applies unchanged at 85. NOT ON EXCEPTION runs only on a successful, non-EC-propagating return (§14.9.4.4 GR3i).
- Variable-occurrence (OCCURS DEPENDING ON) BY CONTENT arg: copy the maximum size; callee honors the DEPENDING-ON value (§14.2.3 GR9).
- BASED item with no associated pointer (unallocated / after FREE): reference is undefined → IsNull guard; double FREE / use-after-free guarded.
- EXTERNAL items survive CANCEL (§14.9.5 GR8) and are shared across all programs describing the same external name (§8.6.7).
- COMMON nested program is callable by sibling contained programs (§8.4.6.3); non-COMMON only by its direct container.
- Recursive COMMON program sharing state — deterministic, allowed.
- REDEFINES-a-pointer-as-bytes or writing a pointer to a file → ISO implementor-defined; reject as undefined (a managed ref cannot serialize to stable bytes).
- ADDRESS OF as a sending operand passed BY REFERENCE across a CALL (address-identifier, §14.9.4 SR3-4) passes the carrier itself.
- **A character-carried formal has THREE length regimes, and `ProgramEmitter.FormalTextCarrier` is the one place
  they are distinguished (kb/Work PB165).** ANY LENGTH (§13.18.2 GR1) — the formal's length IS the argument's,
  fixed for the activation: the full-string view (`CobolArgAdapt.Text`, width sentinel −1). DYNAMIC LENGTH
  (§13.18.19.4 GR1/GR2) — the length VARIES during execution, minimum zero, maximum the LIMIT phrase:
  `CobolArgAdapt.DynText`, a full-string view whose store carries §8.5.1.10.4's replace-and-truncate rule.
  Fixed — the declared width window (§14.2.3 GR8: the callee touches only its formal's character positions).
  ⚠ The DYNAMIC LENGTH arm is not optional cosmetics: §13.18.19.3 SR1 pins such an item's PICTURE at exactly ONE
  symbol, so falling to the fixed arm delivers a ONE-CHARACTER formal for every such crossing, in both passing
  modes. BY VALUE cannot reach the dynamic arm — §14.2.2 SR2 admits only class numeric, message-tag, object or
  pointer BY VALUE.
- **An ANY LENGTH RETURNING item takes its length from the ACTIVATOR's receiver (kb/Work PB1167; §13.18.2.3 SR3 b),
  §13.18.2.4 GR1 b)).** The RETURNING item is a plain callee field, so nothing in the argument adapters can size it;
  `ProgramEmitter.EmitCallMethod` seeds it before `__Activate()` with `CobolArgAdapt.ReturningSeed(__ret, fill, declared)`
  (n = the receiver's stated `CobolArg.Length`, else an ANY LENGTH / DYNAMIC LENGTH receiver's carrier length, else the
  declared PICTURE length for an activation with no receiver — docs/CONFORMANCE.md D-ALR1). The delivery needs no
  change: `FitToReceiver` already fits to the receiver's stated length (rule 5, "considered to match"), and an ANY LENGTH
  receiver stores at its own length. The binder side is the existing activation-mode relaxation of
  `OoConformance.DescriptionMismatch` (rule 4 stays: an ANY LENGTH receiver needs an ANY LENGTH sender).
- SET ADDRESS OF on a LINKAGE item re-points the callee's formal mid-execution — the carrier field is reassigned; subsequent refs read the new target.

## Edition gating (G1 — four per-`--std` compilers in one executable)

Interprogram constructs vary heavily by edition. Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. At each `DialectLevel` (85/2002/2014/2023) the lacks-it diagnostic is a targeted `COBOLNET-` diagnostic — never a generic parse error. Every row gets (construct × edition) coverage per `docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition) matrix; Phase 0 done); verify rows against `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows, so derive 85↔2002 gating from the 2002 standard) and the per-edition spec text.

- **All editions (85+):** CALL USING BY REFERENCE / BY CONTENT, CALL … ON OVERFLOW (removed at 2023 — below), CALL … [NOT] ON EXCEPTION (X3.23-1985 CALL Format 2 — CCVS-85 IC222A tests both phrases), CANCEL, nested/contained programs, COMMON / INITIAL, GLOBAL / EXTERNAL, EXIT PROGRAM.
- **2002+ (under `--std 85` reject with an "introduced in COBOL-2002" diagnostic):** CALL … BY VALUE AND the procedure-division-header USING BY VALUE phrase (twin registry rows `call-by-value-2002` / `pd-header-by-value-2002`); RETURNING (CALL and the PROCEDURE DIVISION header — the ISO §14.9.18 GOBACK/EXIT PROGRAM carry NO RETURNING operand; a `GOBACK`/`EXIT PROGRAM` `RETURNING`/`GIVING` operand is an accepted vendor extension over that header-only mechanism, `GobackReturning2002`); OMITTED arguments + the omitted-argument condition; PROGRAM-ID … RECURSIVE (`program-id-recursive-2002`); the REPOSITORY PROGRAM entry — §12.3.8.2's program-specifier, and with it every program-prototype reference in CALL Format 2 and CANCEL (`repository-program-2002`, D6); LOCAL-STORAGE (`local-storage-section-2002`); GOBACK; USAGE POINTER / ADDRESS OF / SET ADDRESS OF; BASED / ALLOCATE / FREE; ANY LENGTH; and the whole EC-PROGRAM-* exception machinery (at `--std 85` CALL failures surface only via the ON OVERFLOW/EXCEPTION phrases / abnormal termination — no EC names, no `>>TURN`).
- **2014+:** `>>TURN` of EC-PROGRAM exceptions in a calling element is FLAG-02-flagged (`VERSION_CHANGE_REFERENCE.md` row 97).
- **2023:** CALL … ON OVERFLOW is REMOVED (row 3, E.2 item 1c) — reject at 2023, accept at 85/2002/2014; EXIT PROGRAM is archaic (rows 89/126) — flag at 2023; GOBACK gains the STOP-style status phrase (row 75) — 2023-only; EXTERNAL-item conformance exception checking is added (row 15).

## ISO citations

- ISO/IEC 1989:2023 §14.9.4 CALL statement (formats 1/2, syntax rules, general rules — incl. GR3a args-evaluated-once, GR3b-h resolution + EC-PROGRAM-NOT-FOUND/PTR-NULL/RESOURCES/ARG-MISMATCH/RECURSIVE-CALL, GR3i NOT ON EXCEPTION, GR5 transitive BY CONTENT/BY REFERENCE (Format 1), GR11-12 OMITTED)
- §14.2 Procedure division structure: §14.2.2 SR1 (formal param = level 01/77 in LINKAGE, no BASED/REDEFINES), §14.2.3 GR2 positional correspondence, GR4 transitive BY REFERENCE/VALUE, GR6-7 RETURNING (storage in caller), GR8 BY REFERENCE = same storage area, GR9 BY CONTENT = allocated copy then by-reference, GR10 BY VALUE = allocated value copy
- §14.8.2 Parameters / §14.8.3 Returning items / §14.8.4 External items (conformance; EC-EXTERNAL-DATA-MISMATCH / FORMAT-CONFLICT / FILE-MISMATCH)
- §14.6.2.3 Initial and last-used states of data (§14.6.2.3.2 initial, §14.6.2.3.3 last-used); §14.6.2.4 initial state of object data
- §8.6.4 Automatic/initial/static internal items; §8.6.5 Based entries and based data items (ADDRESS OF, association lifetime); §8.6.6 Common, initial, and recursive attributes; §8.6.7 Sharing data items (EXTERNAL)
- §14.9.5 CANCEL statement (GR3 initial state on next CALL, GR4 cascade to contained, GR5 EC-PROGRAM-CANCEL-ACTIVE, GR7 no-op never-called/canceled, GR8 EXTERNAL not reset, GR9 implicit CLOSE)
- §14.9.3 ALLOCATE statement / §14.9.15 FREE statement (managed dynamic storage; INITIALIZED)
- §14.9.39 SET statement — pointer/address forms (SET p TO ADDRESS OF x; SET ADDRESS OF based-item TO p)
- §14.9.14 EXIT statement (the EXIT PROGRAM format — archaic in 2023, Annex F.1 item 1). NOTE: ISO 1989 defines NO ENTRY statement — alternate entry points are a vendor extension outside `--std` conformance; if ever supported, gate as an extension.
- §13.18.5 BASED clause; §8.4.3 ADDRESS OF identifier; §13.18.60.3 USAGE clause SR8–SR9 (program-pointer / data-pointer reference restrictions)
- §8.4.6 / §8.4.6.3 Scope of names / Scope of program-names (nested + COMMON visibility, program-name resolution)
- §12.3.8 REPOSITORY paragraph — §12.3.8.2's program-specifier general format, §12.3.8.3 SR1/SR2/SR15 (the declaration rules), §12.3.8.4 GR2 and GR10 a)/b)/c) (the externalized name and the three-arm resolution); §8.4.6.8 Scope of program-prototype-names (D6)

## Open questions — ALL SETTLED (`COBOLNET_DESIGN.md` §18 + the owner-approved cross-assembly probe)

- Program model — **SETTLED (§18 #8):** ONE `.g.cs` + ONE assembly per compilation; multiple/contained programs → nested classes with same-assembly direct CALL (the typed fast path everywhere). A per-unit/by-name separately-compiled `.dll` option is a later need and, when used, crosses the uniform opaque ABI (typed fast path unavailable across assemblies).
- Carrier name + representation — **SETTLED (§18 #12):** the carrier is the typed `ManagedRef<T>` (NOT the abandoned `byte[]`+offset+length form); the public name **`ManagedPointer`** is kept over the typed carrier (owner preference). The `feedback_one_mechanism_per_job` memory has been updated to the typed form.
- Cross-assembly dynamic CALL discovery — **SETTLED (owner-approved): the `__CobolModule` registrar + sibling-assembly probe.** Every compiled module emits ONE public well-known discovery surface, `public static class __CobolModule { public static void Register() }`, performing the same `ProgramRegistry.Register(...)` calls `Program.Main` uses (Main delegates to it — one canonical registration body; the generated program classes stay `internal`, so the registrar IS the reflection surface — `CodeGen/ProgramEmitter.cs::EmitEntryWrapper`). `ProgramRegistry.ResolveVisible`'s rule-4 fallthrough probes `AppContext.BaseDirectory` for `<name>.dll` (exact name, then a case-insensitive scan — Linux), loads it into `AssemblyLoadContext.Default`, invokes `__CobolModule.Register()`, and retries rule 4 once; probed names are cached hit-or-miss (one I/O probe per name per run unit) and an unloadable/foreign dll is a quiet miss → the ordinary EC-PROGRAM-NOT-FOUND surface. Spec basis: §14.6.1 ("A run unit contains one or more runtime modules"), §14.9.4.4 GR3b (the locate step is implementor-defined beyond §8.4.6.3 name scope). The prebuilt-static-registry profile (AOT/trimming) remains possible — pre-register every name and the probe never fires. Known edge (accepted): a probed module registering a containment PATH that collides with an existing registration overwrites it; per-outermost program-names are unique in a conforming run unit (§8.4.6.3), so collisions arise only from non-conforming compositions.
- Category-mismatch BY REFERENCE byte boundary — **SETTLED (§18 #1; SSOT §9.4):** the transient, never-persisted byte image IS the one sanctioned boundary for this narrow case (category mismatch is legal COBOL exercised by NIST — not a compile error); same-category stays 100% typed.
