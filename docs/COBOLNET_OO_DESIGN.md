# WiseOwl COBOL — OO COBOL -> .NET classes (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §10; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.
>
> **The OO subsystem is IMPLEMENTED end-to-end** and is the CURRENT behavior of the compiler;
> the decisions, rationale, C# mapping, hard problems, and edge cases below describe that live design.
> Implementation briefs for the OO slices live in `docs/COBOLNET_OO_SLICE_BRIEFS.md`.
>
> The delivered surface: the `CobolObject` runtime base (D2) + USAGE OBJECT REFERENCE (typed and
> universal); the `OoClassUnit` collection + the pass-1 class symbol table (D1); the emit-into-a-type
> parameterization of the emitter; method exit-bounded PC ranges + `BoundMethodReturn` (D8, the
> catch-at-entry form); INVOKE NEW / instance calls with typed object references and the
> 0813/0820–0827 diagnostic band; method LINKAGE → typed `ref` C# parameters over capturable locals,
> LOCAL-STORAGE → C# locals (re-init per activation), method WS → STATIC fields under the §13.5.3 SR 1
> edition window, per-method DATA scopes (§11.7 GR5 shadowing), the LOCAL-FUNCTION dispatcher, INVOKE
> USING/RETURNING marshaling under §14.8.2 (D6 — type-preserving crossings, §14.9.23.3 SR 10 object-data
> auto-CONTENT), and implicit-RECURSIVE reentrancy (:12032); INHERITS (`: BASE`, override marking +
> §9.3.8.2 signature checks via the shared DescriptionMismatch rule) and SELF/SUPER (D5: `this.M(…)`
> virtual GR2 / `base.M(…)` non-virtual GR3).
> FACTORY (§11.4) is realized as per-class factory SINGLETON classes (`FOO__FACTORY : BASE__FACTORY |
> CobolObject` with `__Instance` + a covariant `__Create` when the class inherits BASE — §8.6.4 per-class copies of inherited factory
> data, SELF-in-factory polymorphism SR4f/GR2, §9.3.6 chain resolution; `INVOKE Class "M"` →
> `CLS__FACTORY.__Instance.M(…)`). OVERRIDE / [IS] FINAL enforce strict §11.7 SR4a/SR3 + the FINAL
> family and drive the total D7 modifier table. INTERFACE-ID + IMPLEMENTS + PROPERTY declarations and
> references are live: C# interface emission over the prototypes' signatures (§11.5/§11.6; prototype
> LINKAGE per §10.6.2 SR4), the BINDER-authoritative §9.3.11/§9.3.8.2.3 conformance pass over the
> §11.8.4 GR2 closure (Roslyn is provably insufficient in BOTH directions — 9(4)/9(8)→`ref long`
> under-rejects; legal covariant returns over-reject, cured by explicit-interface-implementation
> ADAPTERS), interface-typed receivers, and PROPERTY (§13.18.42 clause-synthesized accessors under the
> pinned §11.7.4 GR1a names `__GET_/__SET_`, plus the §8.4.3.9.4 GR1–GR3 property-reference desugar).
> Universal/dynamic dispatch (D10 `__CobolInvoke`) and EC-OO (the exception-OBJECT channel over the
> landed EC engine) are live. All `oo_*` conformance goldens are byte-exact.

## Summary

OO COBOL → idiomatic .NET classes, designed fresh for the C#-native (Roslyn) target. The byte-model legacy kept its whole data engine and only swapped `ldsfld State`→`ldfld State`; the C#-native target instead emits ONE real C# class per CLASS-ID (the driver PROGRAM stays the static `Program` class), instance fields per OBJECT data item, real C# methods per METHOD-ID, and INVOKE → real C# `new`/`obj.M(...)`/`base.M(...)`. This is a major simplification: the one thing the byte model needed Mono.Cecil two-pass resolution for (cross-type method/type binding, virtual dispatch, parameter-conformance checks) Roslyn now gives for free — the residual "two-pass" belongs to the BINDER: pass-1 builds the class/method symbol table (class → method-name → {param modes, return}), pass-2 binds bodies, so each BoundInvoke carries its resolved call form (new/virtual/base/static/dynamic) and per-arg marshal mode (ref/value/copy) as bound-tree facts; BOTH backends (Roslyn primary, future CIL — G4 `ICodeGenBackend`) only render those facts, and the C# compiler merely re-resolves what the binder already validated.

TYPE-EMISSION MODEL: every CLASS-ID Foo → `public class Foo : CobolObject` (or `: Base`; the standard class BASE is the runtime `BASE`, D4); a `CobolObject` runtime base hosts universal/dynamic dispatch + NULL/IS semantics reflection-free (rejected: derive from System.Object → universal dispatch then needs reflection). The "emit fields+paragraphs+statements into a type" routine is parameterized by (type, instance-vs-static, storage-source), so a driver program and a class emit their members on the SAME statement/PC-dispatch machinery. STORAGE SCOPES (two counterintuitive): OBJECT-para WORKING-STORAGE → INSTANCE fields; METHOD WORKING-STORAGE → STATIC fields (pre-2023 editions ONLY — **ILLEGAL in 2023 per §13.5.3 SR 1, see Spec corrections #1**; where legal: method WS persists across activations, shared across instances — NOT per-instance); METHOD LOCAL-STORAGE → C# locals (re-init each call); LINKAGE → method parameters; method-local names SHADOW object data (§11.7 GR5). New (the standard class BASE's factory method, D4) creates through the ctor: chain base ctor first (C# default, matches COBOL base-then-derived init), then VALUE-init this class's instance fields. FACTORY → a REAL sibling singleton class per CLASS-ID (`FOO__FACTORY : BASE__FACTORY | CobolObject` with `__Instance` + a covariant `__Create` when FOO inherits BASE — NOT C# statics: §8.6.4 gives every class its OWN copy of inherited factory data, SELF-in-factory is polymorphic per §14.9.23.3 SR4f + §8.4.3.8 GR2, and §9.3.6 factory resolution walks INHERITS — three facts statics cannot satisfy).

INVOKE RESOLUTION (each grounded in a conformance .cob): `Class "New" RETURNING o`→`o=(C?)C__FACTORY.__Instance.__New()` (only when C inherits the standard class BASE — D4); `obj "M" USING.. RETURNING..`→`obj.M(args)` virtual; `SELF "M"`→`this.M()` (virtual, §8.4.3.8 GR2 — runtime-class dispatch); `SUPER "M"`→`base.M()`; `Class "M"`(non-NEW)→static call. Dynamic/universal (method-name is a data item, or receiver is a universal `CobolObject?`) cannot be a static C# call → a virtual `__CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning)` on CobolObject whose body is a switch over the class's methods (reflection-free, AOT/WASM-safe); typed-literal INVOKE stays the fast path. INVOKE on a real .NET object (interop) is deferred — §14.9.23.4 GR2b makes non-COBOL INVOKE implementor-defined.

METHOD ATTRS map cleanly because COBOL forbids implicit hiding: instance methods → `virtual` by default (§9.3.6 runtime-class dispatch); OVERRIDE→`override`; FINAL→`sealed override`; §11.7 SR4a (redefining a base signature without OVERRIDE is an ERROR) means we NEVER emit C# `new`/hiding. (ABSTRACT is NOT ISO — vendor extension only; dropped from the ISO surface. **Spec corrections #4.**)

CORRECTNESS BLOCKER (the only one): GOBACK in a method ≠ STOP RUN. In a method GOBACK returns from the method only; STOP RUN ends the run unit. GOBACK and STOP RUN are distinct signals: STOP RUN binds to `BoundStop`, program GOBACK to `BoundGoback`, and a method-context GOBACK to a distinct `BoundMethodReturn` (carrying the RETURNING item, if any) whose method-return path is separate from the run-unit `StopRun` (see D8 and "Greenfield seams"). SETTLED (SSOT §18 item 18): COBOL allows MULTIPLE class inheritance (§11.3.2 `INHERITS FROM {name}…`), which `: Base` cannot express in C# — v1 restricts to single inheritance and rejects 2+ bases LOUDLY — LANDED (P9 Step 10): the grammar parses the §11.3.2 repetition (`INHERITS FROM? className+`, superset-parse doctrine — FROM is un-underlined on printed page 294, so it is an OPTIONAL WORD per §8.3.2.4.3 and `CLASS-ID. C INHERITS B.` is conforming; kb/Work PB695), `OoClassSymbol.Bases` carries the full list, and pass-1 raises COBOLNET0849 (negative corpus `oo-multi-base-inherits` + the `Class_MultiBaseInherits_0849` spine fact).

### The P9 subsystem topology (as-built 2026-07-16 — PHASE-09 Part A)

OO lives in `src/Cobol.Net.Compiler/Oo/` (`CobolNet.Compiler.Oo`): `OoClassTable` (the PURE pass-1 symbol
table) + `OoClassSymbol`/`OoInterfaceSymbol`/`OoMethodSymbol` (immutable pass-1 identity; the after-data-bind
signature — formals/RETURNING/data roots/pc range — attaches as `OoMethodBinding`, a null-deref if read too
early) + `OoConformance` (the §9.3.8.2/§9.3.11 validator; `ValidateImplements` RETURNS the covariant
`AdapterPair` list, threaded via `BoundCompilation.OoAdapters` to the interface emitter) + `OoDriver` (the
bind bodies: interface prototype data, class OBJECT/FACTORY halves + signatures, method-body rosters — owned
and sequenced by `BinderDriver.Bind`; the former emitter-hosted `IOoBindHost` seam is DELETED) +
`NamingConvention` (the ONE home for `__GET_`/`__SET_`, `__FACTORY`/`__Instance`, the
`::EXT::`/`::INST::`/`::FACT::` file-key bands — the runtime's `::EXT::` recognition is the documented wire
contract — and EVERY synthesized C# name that embeds a user-defined word: each such family carries a NON-EMPTY
tag (`__formal_` for a method formal's `ref` parameter, `__addr_`, `__fkey_`, `__cap_`, `__dbg_`, `__new_`,
`__sum_`), listed once in `CobolWordDerivedPrefixes`; `SynthesizedNameFamilyDriftTests` proves no emitter-fixed
`__X` member enters a family and no site derives an untagged `"__" + word` — the shape that turned a formal named
N into the paragraph-count constant `__N`, kb/Work PB973). Statement binding is the P7 collaborator `Binding/Procedure/Verbs/OoBinder.cs`; emission is the P7
collaborator `CodeGen/Verbs/OoEmitter.cs`. The former ambient flags are gone: `ActiveMethodScope` push/pops
via `BinderContext.EnterMethodScope`; `OoIsClassUnit`/`OoCurrentClass`/`OoInFactory` are `init`-only
per-binder configuration set by `OoDriver` at construction. P9's feature closes: ANY LENGTH §13.18.2
(all three unit-kind legs; `IsAnyLength` + the runtime-length store/LENGTH channels + the `CobolArgAdapt.Text`
width −1 full-string mode; RETURNING leg staged loud); the §4.2.2 interface conformance leg proven
(`oo_interface_conformance` + the 0828 lossy-projection negative); and the class-level ENVIRONMENT DIVISION
scoping fixed (`DataBinder.EnvDivisions`, outermost-first — a half's own env no longer shadows the class env;
`oo_class_env` pins CURRENCY-through-shadow; the DEVLOG-738 latent bug).

## Spec corrections

> These four corrections are each verified against `specs/ISO_COBOL.md` (ISO/IEC 1989:2023) with the
> line anchors below. The affected decisions/hard problems/edge cases carry matching CORRECTION notes.

1. **Method WORKING-STORAGE is ILLEGAL in 2023.** §13.5.3 Syntax rule 1 (specs/ISO_COBOL.md:16461): within a class definition the working-storage section may be specified only in a factory definition or an instance definition, "but not in a method definition"; nor in an interface definition. Corroborated by INVOKE SR 10 (:28443 — object WS exists but cannot be an INVOKE argument). Method definitions still take a full data division (§10.6 :12818) — LOCAL-STORAGE + LINKAGE are the 2023-legal method storage; method-vs-object name shadowing per §11.7.4 GR 5 (:13281). The live grammar still parses a full dataDivision inside methodDefinition (`Core/CobolOO.g4:43-49`), so the G7 EditionValidator must reject method WS at `--std 2023` with a versioned diagnostic; the D3 static-field mapping applies only in editions whose standard permits method WS (pin the exact edition boundary in `docs/VERSION_CHANGE_REFERENCE.md` during G7).
2. **EXIT METHOD was REMOVED in 2023.** §14.9.14.2 has exactly four EXIT formats — simple EXIT / EXIT PROGRAM [RAISING] (archaic) / EXIT PERFORM [CYCLE] / EXIT PARAGRAPH|SECTION — no METHOD or FUNCTION alternative (specs/ISO_COBOL.md:27346-27381); Annex E.2 explicitly lists "EXIT METHOD statement" (:49034) and "EXIT FUNCTION statement" (:49036) among removals since the previous standard. In 2023 methods terminate via GOBACK or by falling out of the procedure division. The shared grammar still accepts `EXIT METHOD [RAISING]` (`Core/CobolControlFlow.g4:213`) — edition-gate it: a method-return synonym at 2002/2014, a removed-feature diagnostic at 2023.
3. **INVOKE has NO ON EXCEPTION phrase (in ANY spec surface designed here).** The §14.9.23.2 general format is exactly `INVOKE {class|identifier} {method} [USING …] [RETURNING identifier-4]` — no ON EXCEPTION / NOT ON EXCEPTION and no handler inside END-INVOKE (specs/ISO_COBOL.md:28376-28390). Failures surface as EC exception conditions instead: null receiver → EC-OO-NULL (GR 5, :28506); method not found / insufficient resources → EC-OO-METHOD (:28528) — routed through the EXISTING §14.6.13 EC machinery (declaratives, >>TURN, EXCEPTION-* functions), never a handler phrase. This differs from CALL. The live grammar is already correct (invokeStatement has no exception alternative); the dead sketch `CobolParserOO.g4`'s `invokeOnException` must NOT be revived.
4. **ABSTRACT is NOT ISO.** A grep over the entire spec returns zero matches — ABSTRACT is not a reserved word, clause, or concept anywhere in ISO 1989:2023. CLASS-ID's only modifiers are `[AS literal-1] [IS FINAL] [INHERITS FROM …] [USING …]` (specs/ISO_COBOL.md:12742-12744) — all four PARSE (`classIdParagraph`; USING since kb/Work PB759, D12), and a method definition's only attributes are `[OVERRIDE] [IS FINAL]` (plus the GET/SET PROPERTY selector form, :12798-12821). Any "ABSTRACT class" notion is a vendor extension (Micro Focus/IBM) and must not appear as ISO surface in this design.

## Version gating (G1 — four compilers in one)

The ENTIRE OO subsystem (CLASS-ID/FACTORY/OBJECT/METHOD-ID, INVOKE, USAGE OBJECT REFERENCE, SELF/SUPER, the standard class BASE (New / FactoryObject), INTERFACE-ID/IMPLEMENTS, SET … TO object-reference, repository CLASS entries) is introduced in COBOL-2002 and present in 2014/2023. Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Concretely:
1. `--std 2002|2014|2023` → the full per-edition spec behavior designed in this doc.
2. `--std 85` → every OO construct is REJECTED with a specific edition diagnostic (e.g. "CLASS-ID requires COBOL-2002 or later; compiling as COBOL-1985"), NOT a generic syntax error. The live grammar already gates the parse with `{is2002()}?` predicates (CobolParserCore.g4: classDefinition in compilationGroup, the repository CLASS entry, invokeStatement, PD returningClause, BY VALUE args, SET-to-objectReference, GOBACK RETURNING); a diagnostic pass must map each gated rule to its versioned diagnostic so the version test matrix's negative corpus (`docs/VERSION_TEST_MATRIX_DESIGN.md` — the (construct × edition) matrix; Phase 0 done) can assert it.
3. Edition deltas WITHIN OO (2002→2014→2023) are gated by DialectLevel per `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023 deltas ONLY; OO's own 2002 introduction has NO row there and derives from the 2002 standard) (e.g. row 97: FLAG-02 flagging of EC-PROGRAM TURN in elements that invoke methods, 2002→2014; row 15: external-item conformance-check ECs, 2014→2023) — never hard-coded to 2023.
Positive conformance tests live in `tests/conformance/2002/oo_*.cob`; the rejected-at-85 cases are matrix cases, never skipped.

## Decisions

### D1. One C# type per CLASS-ID; the driver PROGRAM stays the static `Program` class; the BINDER owns cross-type resolution and parameter/return conformance DIAGNOSTICS (backend-neutral, G4); Roslyn re-checks the rendered C# as a safety net only.

**Rationale.** The C#-native target's defining advantage: the byte model needed Mono.Cecil two-pass resolution (define all type+method signatures, then bodies) purely to bind cross-type INVOKE and emit callvirt; emitting C# source hands all of that to the C# compiler. The residual 'two-pass' is the BINDER's: pass-1 builds the class/method symbol table (class→method→{param modes,return type}); BoundInvoke then carries the chosen call form (new/virtual/base/static/dynamic) and per-arg marshal modes, so both backends (Roslyn now, CIL later — G4) render the same bound facts. A user-visible Roslyn CS error from emitted code is an emitter BUG (loud-failure invariant), never the user diagnostic surface. ISO §9.3.6 runtime-class dispatch ≡ C# virtual dispatch exactly.

**Rejected alternatives.** Port the legacy per-instance-ProgramState model (instance `State` byte field + `ldarg.0;ldfld State`) — rejected by the OWNER-LOCKED no-byte-substrate rule and because it throws away Roslyn's free type-checking. Emit via Mono.Cecil like legacy — rejected: re-introduces the manual cross-type MethodDefinition registry the source target eliminates.

### D2. Introduce a `CobolObject` runtime base class; every emitted COBOL class is `class Foo : CobolObject` (or `: Base` when it INHERITS, whose own root is CobolObject — for the standard class BASE, the runtime `BASE : CobolObject`, D4).

**Rationale.** Gives a single reflection-free home for universal/dynamic dispatch (`__CobolInvoke`), object-identity/NULL/`IS class` semantics, and a future EC-OO surface — AOT/WASM-safe (no Type.GetType, no reflection).

**Rejected alternatives.** Derive each class straight from System.Object — rejected: universal-object-reference dispatch (`INVOKE U name`) and `obj IS Class` would then require reflection or a marker interface, defeating AOT safety. A marker interface ICobolObject instead of a base class — rejected: a base class can carry the default `__CobolInvoke` body and shared state; an interface cannot supply a default reflection-free dispatcher cleanly.

### D3. Storage-scope mapping: OBJECT-paragraph WORKING-STORAGE → INSTANCE fields; METHOD WORKING-STORAGE → STATIC fields; METHOD LOCAL-STORAGE → C# locals (re-init per call); LINKAGE → method parameters. Method-local names shadow object data.

**Rationale.** ISO §8.6.4 (persistence of automatic/initial/static internal items) fixes each scope's persistence: working-storage of an instance definition IS object data → "in each instance object of a given class, there is one copy of each static item" → per-instance object state; a method's WORKING-STORAGE is a static item that is NOT object data → "there is one copy in a run unit" → it persists across activations and is SHARED across instances (so it is static, NOT per-instance — the counterintuitive case); LOCAL-STORAGE items are automatic → "allocated and set to initial state each time the runtime element … is activated" → re-initialized on each entry (locals). §11.7.4 GR5 makes a method-local name shadow the same name in object data.

**Rejected alternatives.** Make method WORKING-STORAGE instance fields (the naive 'it's inside the object' reading) — rejected: violates §11.7 persistence/sharing semantics and would silently miscompile a counter kept in method WS. Make LOCAL-STORAGE static — rejected: LOCAL-STORAGE must re-init each activation.

**CORRECTION (Spec corrections #1).** In ISO 2023 a method definition may NOT contain a working-storage section at all (§13.5.3 SR 1, specs/ISO_COBOL.md:16461; interfaces get none either). So the method-WS→static-fields mapping is a pre-2023 behavior only, edition-gated; at `--std 2023` method WS is a compile-time versioned diagnostic. The 2023-legal method storage is LOCAL-STORAGE (→ C# locals) + LINKAGE (→ parameters); OBJECT/FACTORY WS is unaffected.

**Implementation.** D3 is realized in full: method WS → `private static` fields (the two-instance shared-counter behavior is verified by `OoSpineTests.MethodWorkingStorage_StaticSemantics_EditionWindow`); LOCAL-STORAGE → C# locals declared in the method entry (re-init per activation is the language semantics of a local declaration); LINKAGE → `ref` parameters copied into CAPTURABLE locals (see the D6/D8 notes for why locals, not raw params). The 2023 ban is the `method-working-storage-window` registry row (0900 below 2002 / 0902 at 2023; `--permissive` keeps the static semantics — the §10 #1 migration contract). ⚠ The pre-2023 legality boundary is PINNED PROVISIONAL at 2002–2014 (Annex E.2 does not itemize the removal and the 2002/2014 texts are not in-repo — the pin records the one-line `removedIn` shift if the 2014 text proves otherwise). Method-data shapes not yet supported (loud today): REDEFINES / OCCURS INDEXED / ODO / level-66 / EXTERNAL / GLOBAL inside method data, and FILE/REPORT/SCREEN sections in a method.

**BASED data and ADDRESS OF targets in class data (kb/Work PB956).** §13.16.3 SR16 admits the BASED clause in the working-storage, local-storage and linkage sections, which a factory, an instance and a method definition all have, so a class's data is pointer-backed exactly like a program's: the ONE cell-backing forcer (`DataBinder.ForceStringCanonical`) re-bases each BASED root and each record named by a `SET … TO ADDRESS OF x` sender, and ONE renderer (`OoEmitter.EmitPointerBackings`, shared with `ProgramEmitter`) declares the implicit pointer + deref cell + backing, or the addressable `StorageCell` + backing, in the type-half that owns the forest. Two class-unit specifics: (1) the ADDRESS-OF pre-scan walks each bound METHOD's procedure division (`DataBinder.OoBoundMethods` — the synthetic class unit has none of its own) and resolves every head in the method's own scope (§11.7.4 GR5); (2) the member's STORAGE DURATION follows its section per D3 — object/factory WS → an instance member of that type-half; method WS → a `static` member (`StaticBasedBridgeAddrs` / `StaticAddressableCells`, §8.6.4 static items); method LOCAL-STORAGE / non-formal LINKAGE → a per-ACTIVATION member: `EmitMethod` saves the activator's value, starts the activation fresh (a NULL pointer, §13.18.5.4 GR2; a new cell seeded with the record's initial image, §8.6.4) and restores it in the activation's `finally`, so a recursive INVOKE on the same object neither inherits nor clobbers its caller's address. They are members rather than C# locals because the method's dispatch is a local function and C# cannot capture a `ref` local (the backing is a `ref string` bridge). A method formal / RETURNING item named by `ADDRESS OF` IS forced like any other method LINKAGE root (kb/Work PB1019, method arm — §8.4.3.11.3 SR1 admits a linkage-section item): its per-activation cell is seeded with the initial image, a PRESENT formal's argument is copied into it at entry and copied back out at exit, and the RETURNING item is delivered from it — through ONE composer, `OoEmitter.MethodBoundaryValue` (copy-out and RETURNING) and its inverse `MethodCellFormalStore` (copy-in). A cell-backed item crosses as its IMAGE when `OoCrossingType` says `string` (a group, or an elementary item stored as its image), else as its typed value read through its window. Because the crossing is a boundary copy, the program path's boundary-copy limitation (COBOLNET_INTERPROGRAM_DESIGN) applies: the address is the method cell's, not the invoker's argument's. The INVOKE site decodes a string-carried numeric RETURNING result through the receiver's own byte form (`NumericImagePlace`, §14.8.3.3 — same PICTURE and USAGE), the same decode its BY REFERENCE copy-out always used. A PROPERTY accessor reads and writes its subject through the subject's PLACE, so a subject whose record is cell-backed (or a Tier-B REDEFINES window) has no field of its own and still works; §13.16.3 SR21 (PROPERTY × BASED / TYPEDEF, COBOLNET2333) is asked of the entry's own clauses.

### D4. New is the STANDARD CLASS BASE's factory method (§16.2 BaseFactoryInterface), not a method every class has; its creation step is the generated public ctor: base ctor chains first, then VALUE-initialize this class's own instance fields.

**The standard class BASE (§16; kb/Work PB1548 / PB1506 / PB1524).** §16.1: "A standard class BASE shall be provided
by the implementation. It may be used as the root of a class hierarchy to provide standard object life-cycle
function. This use is not required". Its implementation is the runtime pair `CobolNet.Runtime.BASE` (object half —
BaseInterface's `FactoryObject`, §16.2.2.2 GR1, a virtual `FACTORYOBJECT()` returning the abstract
`__FactoryOfClass` every BASE-derived class overrides with its own singleton) and `BASE__FACTORY` (factory half —
BaseFactoryInterface's `New`: the ONE non-virtual body `__New()` wrapping the covariant abstract `__Create()` every
BASE-derived factory overrides with `new C()`; an `OutOfMemoryException` there is §16.2.1.2 GR2 — NULL, plus
EC-OO-RESOURCE through `ExceptionState.OoResourceError` when checking for it is enabled). The names are the
emitted-type convention for the class-name BASE (sanitize + uppercase, + `__FACTORY`), so `INHERITS FROM BASE`,
`USAGE OBJECT REFERENCE BASE` and `FACTORY OF BASE` need no special case in the emitter.
In the binder, `OoStandardClasses.BuildBase` gives every compilation group an `OoClassSymbol` for BASE (`IsStandard`,
never in `OoClassTable.Classes`, no source) whose factory roster holds New and whose instance roster holds
FactoryObject, each with its §16.2 signature as a real `OoMethodBinding` (`StandardMethod` marks them). It is reached
exactly like any class — through a REPOSITORY class-specifier (§8.4.6.4; §11.3.3 SR2; §12.3.8.3 SR6 b) — and
`OoClassTable.Find` answers BASE with it only when no class of the group is DEFINED under that name (the §12.3.8.4
GR6 implementor-defined choice). Methods then join a subclass's interfaces by ordinary §9.3.9 inheritance, so
`INVOKE C "New"` resolves only when C inherits BASE; a class that does not has NO New, and naming it through any
receiver form is COBOLNET2448 citing the governing §14.9.23.3 rule (SR3 class-name, SR4 a/c FACTORY OF, SR4 f/h
SELF/SUPER). New and FactoryObject are not inline-invocable: §16.2 describes both returning items with ACTIVE-CLASS,
which §8.4.3.4.3 SR4 excludes (COBOLNET2140). An invocation of either binds through `OoBinder.OoBindStandardInvoke`,
whose result description is the receiver's own class view (the named class ONLY for a class-name; the declared class
or ACTIVE-CLASS of a reference; ACTIVE-CLASS for SELF/SUPER) — that is what §16.2's `active-class` means for it.

**Rationale.** C# already runs base-then-derived ctor order, which matches COBOL initialize-inherited-then-own-data;
§16.2.1.2 GR1's "allocates storage for an object, initializes its instance data in accordance with 14.6.2.4" is
exactly `new C()`.

**Rejected alternatives.** A separate static `NEW()` factory method calling a parameterless ctor + an Initialize method (the legacy InitializeState shape) — rejected: redundant in C#; the ctor IS the initializer and field initializers + ctor body cover VALUE.

### D5. INVOKE call-form table: Class "New" RETURNING o → `o = null; o = (C?)C__FACTORY.__Instance.__New()` (a FACTORY OF reference or SELF/SUPER in a factory method name the factory instead); obj "M" → `obj.M(args)` (virtual); SELF "M" → `this.M()`; SUPER "M" → `base.M()`; Class "M" (non-New) → the factory singleton's virtual member; dynamic/universal → `recv.__CobolInvoke(name,args)`, whose object-reference deliveries narrow through `CobolObject.NarrowUniversal` (a non-conforming class is EC-OO-UNIVERSAL, §14.9.23.4 GR7c).

**Rationale.** Direct, idiomatic, statically-resolved (AOT-safe) for the literal-method 90% path; matches §9.3.6 (object→instance/factory resolution, SUPER restricted search), §8.4.3.8 (SELF virtual on runtime class, SUPER non-virtual).

**Rejected alternatives.** Route ALL invokes through a single `__CobolInvoke` string-switch (uniform but slow, unidiomatic, defeats Roslyn overload/type checks) — rejected; reserve __CobolInvoke only for the genuinely-dynamic cases. The legacy uniform `callvirt CobolProgramEntry.Invoke(ManagedPointer[])` ABI — rejected: not idiomatic C#, hides types from Roslyn.

### D6. Parameter passing: BY REFERENCE → C# `ref` of the typed field (value-class items) or the reference itself (object/string); BY CONTENT → pass a copy; BY VALUE → value parameter; RETURNING → C# return value; OMITTED → a `bool` presence parameter beside every formal + the omitted-arg condition.

**⛔ The method ABI is a PAIR per formal: `ref T value, bool omitted` (kb/Work PB757).** §14.9.23.4 GR9: "If an
OMITTED phrase is specified or a trailing argument is omitted, the omitted-argument condition for that parameter
shall be true in the invoked method" — and a C# `ref T` has no omitted state, so the ABI carries one. It is
EVERY formal's, not only an OPTIONAL one's: §8.8.4.8.4 GR1c makes omission transitive through a forwarded formal
whatever the receiving formal's own phrase, exactly as the program ABI's null carrier is (the rejected
alternative, a flag on OPTIONAL formals only, cannot carry that case). The flag is `OoFormal.OmittedFlag`
(`__omittedN`, positional and outside every word-derived family, so no `__formal_` `ParamName` can collide); `OoSignatureOf` declares the pair and
`OoArgPair` renders it at every caller — the typed INVOKE, the covariant adapter, the universal switch and the
PROPERTY setter — so signature and argument lists cannot drift. In the body an omitted formal's local starts at
its initial state (no copy-in) and is not copied out; every REFERENCE to it (or to a subordinate) renders through
`OmittedFormal.Ref(ref local, __omittedN, Method, …)`, which raises EC-OO-ARG-OMITTED under checking (§14.9.23.4
GR10; kb/Work PB971 — COBOLNET_INTERPROGRAM_DESIGN's OMITTED paragraph holds the one mechanism for all three
element kinds). The universal switch admits `__a.Length` down to one past
the last non-OPTIONAL formal (§14.8.2.1; §9.3.6 match rule 1), reads `CobolInvokeArg.Omitted`, and exempts a
spelled OMITTED (descriptor `CobolInvokeArg.OmittedDescriptor`) from the descriptor check against an OPTIONAL
formal only (§9.3.6 match rule 3 b) — "no further checking is performed"). Binder side: `OoBindResolvedInvoke`
appends an explicit omitted `BoundInvokeArg` for each trailing OPTIONAL formal, and §14.9.23.3 SR18 is
`COBOLNET2237`. Conformance: §9.3.8.2.3 rule 8 (OPTIONAL presence) is `OoConformance.OptionalMismatch`, read by
`MethodConformanceMismatches`. The presence fact itself is shared with the program ABI — see
COBOLNET_INTERPROGRAM_DESIGN, `OmittedProbe`.

**§9.3.8.2.3 per-method conformance is written ONCE — `OoConformance.MethodConformanceMismatches`** (rules 1–4,
5 covariant RETURNING, 6, 7 via `DescriptionMismatch`'s strongly-typed sentence, 8, and 9 RAISING via
`RaisingMismatches`), and it has exactly three askers, each naming which side is interface-1: §9.3.11 IMPLEMENTS
(`ValidateImplements` — the implementing class; COBOLNET0841), §11.7.3 SR9 overrides (`ValidateOverrideSignatures`
— the overriding method; COBOLNET0829) and the interface relation (`InterfaceConformsTo` — GOBACK §14.9.18.3 SR4 b)
/ EXIT §14.9.14.3 SR5 b)). Rule 9 covers every element of interface-1's RAISING phrase: a) the same exception-name;
b) the same class or a superclass with the same FACTORY presence, or an interface the factory/instance object
implements (`OoClassTable.ImplementsClosure`); c) the same interface or one it inherits. The override pass was a
second hand-written copy of rules 1–6 and 8 until kb/Work PB972, with rule 9 in neither;
`MethodConformanceRuleSetDriftTests` keeps every asker free of rule text of its own.

**A GROUP formal or RETURNING item crosses as its CHARACTER IMAGE, through THE ONE CHANNEL.** §14.2.3 GR8 makes
a BY REFERENCE formal "occupy the same storage area as the argument" and §14.9.23.4 GR8 delivers the RETURNING
item; WiseOwl COBOL realizes both through the generated `AsImage()`/`FromImage()` round trip. ⛔ `OoEmitter` does
**not** spell that round trip itself — it routes every group crossing through `PlaceRenderer.GroupImage` /
`PlaceRenderer.WriteFullGroupImage` (the law stated in `GroupImage`'s doc-comment: "THE ONE reader — a consumer
that spells `.AsImage()` itself is wrong for the window shape"), building the boundary root's `Place` with
`OoEmitter.MethodRootPlace` because a method's LINKAGE/LOCAL roots are C# **locals**, not fields (D3). A Tier-B
REDEFINES canonical root is the exception handled before that call: its storage IS its string backing local
(`MethodRedefinesBackingDecl`) at the **class** width, since a wider level-01 redefiner needs the full backing
(§13.18.44.3 SR8), not the canonical view's width.

**Tier-C posture at the method boundary.** A group with a pointer/object-class leaf, or a variable-length group
(§8.5.1.12.1), has no character image (`DataItem.IsImageCapable`) and the crossing stages the documented Tier-C
loud (COBOLNET_DESIGN §4.2) — it is never a backend compile error. That distinction is load-bearing: the bind
side (`DataBinder.Oo`) applies no image screen, and `OoConformance.DescriptionMismatch`'s `!formal.IsImageCapable`
arm runs only when an INVOKE or an override/implements **pair** exists, so a method that is merely DECLARED must
still emit compilable C#. Held by `tests/Cobol.Net.Tests.Unit/BoundaryImageChannelTests.cs` (a source-level drift
test forbidding a self-spelled `.AsImage()`/`.FromImage(` outside the generator) and by the golden
`pb177_method_group_boundary`, whose two never-invoked Tier-C methods are exactly this case. kb/Work PB177 arm A.

**Rationale.** §9.3.6 match-rule 3c requires a BY REFERENCE argument and its formal parameter to be the same class/category — so a real typed `ref` parameter is fully conformant; this is precisely why the C#-native target can use typed parameters instead of the legacy ManagedPointer[] byte ABI. RETURNING-as-return is the idiomatic and §14.9.23 GR8-faithful mapping.

**Rejected alternatives.** Keep the ManagedPointer[] args ABI (legacy) for uniformity with CALL — rejected: byte-substrate-adjacent, unidiomatic, and unnecessary now that types are native. Box everything to object[] for all params — rejected: loses Roslyn type checking and adds boxing.

**Implementation.** Every formal is a `ref` parameter (uniform; the value-param-when-never-written polish is deferred — deciding it needs the body's write-set, which isn't known when signatures must exist). The load-bearing realizations:
1. **The conformance rule is selected by the EFFECTIVE passing mode.** The identical-description rule is **§14.8.2.3.2, BY REFERENCE ONLY** (incl. SIGN representation via `ImageSignKind`, BLANK WHEN ZERO, JUSTIFIED; and the §14.8.2.2 rule-1 GROUP-PREFIX allowance: a by-ref group formal may be SMALLER than the argument — the callee sees the leading positions, the write-back SPLICES the prefix and preserves the tail). **BY CONTENT** (explicit, SR-10 auto-CONTENT, and all literals) follows **§14.8.2.3.3**: COMPUTE rules for numeric formals (ANY fixed-point numeric argument/literal — converted by rescale+truncate through the OWNER class's `internal` profile, qualified `{OWNER}._P_n`; the cross-class-profile rule), SET rules for object references — `OoConformance.ObjectRefAssignmentMismatch`, the ONE §14.9.39.3 SR10/SR12/SR14 sender-into-receiver table, keyed on the RECEIVER's §13.18.60.2 description (universal accepts anything; an interface receiver takes an implementing class or factory; an object-class receiver takes the same class or a subclass unless ONLY, with FACTORY presence invariant; an ACTIVE-CLASS receiver takes only ACTIVE-CLASS with the same FACTORY presence). §9.3.8.2.3 rule 5 shares the table with `activeClassSenderAdmitted: false` — its printed 5 b)/5 c) enumerate no ACTIVE-CLASS sender where SET SR10 c)/SR12 b) do, MOVE rules otherwise (pad/truncate; integer-display→alnum legal); §9.3.6 rule 5 makes TRUNCATING literals conforming. **RETURNING delivery** follows §14.8.3.3 rule 1 = SET rules (subclass results deliver into superclass-typed and universal receivers; C# 9+ covariant returns render override covariance, §9.3.8.2.3 5a/5c2). A reference-modified argument conforms by its EFFECTIVE description (elementary alphanumeric of the window length, §8.4.3.3.4).
2. **§14.9.23.4 GR6 a) auto-CONTENT, decided before resolution (kb/Work PB1137)**: a keyword-less identifier is BY REFERENCE only when it meets §14.9.23.3 SR9 (a data item of the file, working-storage, local-storage or linkage section) and SR10 (not factory/instance object data) — `ParameterConformance.MeetsByReferenceRules`, the test CALL's GR9 a) asks too — and is otherwise resolved as a SENDING operand and passed BY CONTENT (callee writes invisible): object data, an object property (§8.4.3.9.4's conceptual temporary — so only its GET runs), a report's PAGE-COUNTER, EXCEPTION-OBJECT. Explicit BY REFERENCE of object data is 0828 (SR10); of any other non-section item COBOLNET2452 (SR9 — an object property excepted, §8.4.3.9.3 SR6). Every BY REFERENCE argument and the RETURNING item (typed and universal) also take the SR12 bit-alignment proof (COBOLNET2453) and the SR11 section rule. literal-2 binds through the ONE `ExpressionBinder.NonNumericLiteralOperand` mapping (alphanumeric, hexadecimal, national, boolean; a constant-name substitutes its literal, §13.10.3 SR2), refuses a zero-length literal (SR17, COBOLNET2454), and takes the shared §14.8.2.3.3 verdict; NULL is identifier-5 and crosses BY CONTENT into an object-reference formal. Through a UNIVERSAL receiver an ADDRESS-IDENTIFIER crosses as its pointer value under a class-pointer descriptor (`OoConformance.AddressDescriptor` — `P:D:<type|*>` / `P:P:<prototype|*>`, the keys pointer ITEMS carry too; a program-pointer restriction is keyed by prototype NAME, a loud strictness delta against the typed path's signature comparison).
3. **Call-site lowering**: a plain field of matching storage passes `ref` DIRECTLY (subscripts evaluate once — GR7a for free); everything else is copy-in temp → `ref` temp → copy-out (BY REFERENCE identifiers only), groups crossing as character images, storage-form bridges via caller-side `FormatDisplay`/`ParseDisplay`/`NumericImagePlace`; float formals read the float value directly (never the scaled-integer path). **Emission order per GR8**: the call → the BY REFERENCE copy-outs → the RETURNING store into identifier-4 LAST.
4. **Crossing-form harmonization** (`HarmonizeStorageCrossings`, in `StorageFormPass`): once the storage form is computed, override chains UNION the image-stored form across corresponding formal/RETURNING pairs to a fixed point, so base and override always emit the same C# signature (otherwise a base/override storage-form desync is CS0115 on the emitted C#). The union covers EVERY byte-form numeric (`PicInfo.HasImageByteForm`, minus the national-form numeric per D-N1 carriage), not the DISPLAY pair alone (kb/Work PB187): a formal REDEFINED in its linkage section (§14.2.2 NOTE 2) is image-stored whatever its usage, so its plain prototype/override twin is promoted to the image too. The universal box (D-U6a) stays canonical BY DESCRIPTOR, so its two bridges are: a zoned DISPLAY box over a NATIVE local (`OoUnivImageBridged`), and a NATIVE box (float/binary/packed/index) over an IMAGE local (`OoUnivNativeBoxOverImage` — `NumericRenderer.CarrierOfImage`/`ImageOfCarrier` (the ONE native↔storage-image pair, shared with the CALL/INVOKE copy-out arms of kb/Work PB970) through the item's own byte form, both the callee's unbox/rebox and the caller's box/copy-out). The typed call site's `NumericImagePlace` takes the IEEE lane for a native float.
5. **Emitted-name safety**: a METHOD-ID named like its CLASS-ID renames the SYMBOL (`_M` suffix — §8.3.2.2's implementor-defined externalized mapping); overrides adopt the base slot's CsName verbatim; the one unrepresentable corner (a derived class named like an inherited slot) is a 0820 restriction diagnostic. Numeric profiles emit `internal` so CONTENT conversions qualify them cross-class.
BY VALUE args stage 0828 pending the unparsed header BY-phrases; cross-float CONTENT conversion and dynamic-length refmod BY REFERENCE args are documented later refinements (loud today). OMITTED and OPTIONAL are carried (the pair above).

### D7. Method attributes: instance methods are `virtual` by default; OVERRIDE→`override`; FINAL→`sealed override` (FINAL root → non-virtual); FACTORY methods → `virtual`/`override` members of the per-class FACTORY SINGLETON class, NEVER C# `static` (factory invocation dispatches on the runtime factory class, §9.3.6, and SELF-in-factory is polymorphic, §8.4.3.8.4 GR2 — statics cannot express either). Never emit C# `new`/method-hiding.

**Rationale.** §9.3.6 dispatch is always on the runtime class → virtual by default. §11.7 SR4a makes redefining a base signature WITHOUT OVERRIDE a compile error, so COBOL never expresses C# hiding — the mapping stays clean and total.

**Rejected alternatives.** Emit non-virtual methods and only mark virtual when overridden somewhere — rejected: requires whole-program analysis to know if a class is ever subclassed and breaks separate compilation; virtual-by-default matches the spec directly.

**CORRECTION (Spec corrections #4).** ABSTRACT is NOT ISO (zero occurrences in the whole spec; CLASS-ID modifiers are only AS/FINAL/INHERITS/USING, :12742-12744; method attributes only OVERRIDE and [IS] FINAL, :12798-12821). ABSTRACT (and STATIC/visibility attributes) are vendor extensions — out of the ISO surface; FACTORY methods emit as `virtual`/`override` members of the FACTORY SINGLETON class (§11.4; brief D11 — NOT C# statics: SELF-in-factory dispatches on the runtime factory, SR4f/GR2), never via a method attribute. If a vendor-dialect ABSTRACT is ever wanted it is a dialect-gated extension, never default 2023 surface.

**Implementation (OVERRIDE/FINAL).** The attributes are live: `METHOD-ID … [OVERRIDE] [IS FINAL]` + `CLASS-ID … [IS FINAL]` parse (spec order; the OVERRIDE token via the XOR-recipe — user word at 85, 0901 ≥2002), and STRICT §11.7 is enforced at pass-1: SR4a redefinition-without-OVERRIDE = **0837** through the ONE `EditionContext.Removed` policy seam (error strict; warning + the name-match inference under `--permissive` — the documented migration leniency); SR3 OVERRIDE-without-a-base-method = **0838**; the FINAL-violation family (override of a FINAL method, GR3; INHERITS FROM a FINAL class, §11.3 GR3) = **0839**. Both rosters (instance + factory) take identical rules. Emission is the TOTAL D7 table: override → `override` (`sealed override` when itself FINAL in a non-sealed class); FINAL root method or ANY fresh slot in a FINAL class → NON-virtual; FINAL class → `sealed` (both type halves — the generated `__Create` of a sealed factory is an `override`, never a fresh `virtual`: a `virtual` member in a `sealed` type is Roslyn **CS0549** on emitted code, the trap the table exists for, guarded by the oo_override_final golden). SR2/SR8 (no attributes in method PROTOTYPES) is enforced by the interface machinery (see below).

**Implementation (INTERFACE-ID / IMPLEMENTS / PROPERTY).** INTERFACE-ID (§11.5/§11.6) is live end-to-end.
GRAMMAR: `interfaceDefinition` ({is2002()}? in compilationGroup; INTERFACE_ID token; interface INHERITS
repetition supported — C#-native, the deliberate asymmetry with single class inheritance), the
`implementsClause` in both FACTORY/OBJECT paragraphs, `methodPropertySelector` on METHOD-ID, the
`propertyClause` in dataDescriptionClause, and the REPOSITORY INTERFACE/PROPERTY specifiers. WORDS:
GET/PROPERTY/INTERFACE are §8.9-reserved 2002+ via the XOR recipe (user words at 85, 0901 ≥2002);
IMPLEMENTS is §8.10 CONTEXT-SENSITIVE (spec :10853) — a user word at EVERY edition, token + cobolWord but
NEVER CheckedTokenTypes. The VALUE clause needed a loop guard (both the valueItem list AND the
multi-operand `valueClauseOperand+` item): at 2002+ PROPERTY terminates a VALUE clause — reserved, never a
constant-name operand — else `VALUE 100 PROPERTY.` swallows the clause. PASS-1: interfaces build FIRST
(OoInterfaceSymbol; prototypes via TryAddPrototype; the 0840 structural family — one class/interface
namespace §8.3.2.2, END INTERFACE §10.7, §11.7.3 SR2/SR8 no prototype attributes, §10.6.2 SR4 header-only +
LINKAGE-only data division); prototype LINKAGE binds through the SAME OoBindMethodData machinery
(`OoBindInterfaceData`), so ValidateImplements compares RESOLVED descriptions. CONFORMANCE: the
§9.3.11-via-§9.3.8.2.3 pass over the §11.8.4 GR2 closure (`ImplementsClosure`: direct + interface-INHERITed
+ class-INHERITed, cycle-safe; instance and factory sides separately) is BINDER-authoritative = **0841**,
because the C# projection is insufficient in BOTH directions: PIC 9(4) and 9(8) formals both emit
`ref long` (Roslyn under-rejects — the identical-description rules 2/3 live only in DescriptionMismatch),
and C# forbids the covariant interface-implementation returns that rules 5a/5c2 PERMIT (Roslyn
over-rejects — cured by `AdapterPairs` → explicit interface implementations
`PROTO_RET IFACE.M(…) => this.M(…);` as headerExtras on the instance half). EMISSION: `public interface
IFOO [: BASES]` with FieldEmitter statics (numeric profiles + group structs — C# 8+ interface statics, so
cross-unit CONTENT conversions qualify `{IFACE}._P_n`) + the prototypes' signatures through the ONE
`OoSignatureOf` builder (shared with class methods and adapters — the no-drift rule); the instance base
list joins direct Implements (the closure arrives transitively at the C# level); the factory half takes
FactoryImplements (the factory SINGLETON classes make factory IMPLEMENTS emittable, not validate-only).
A REPEATED interface-name in either IMPLEMENTS clause is legal and absorbed into the set (kb/Work PB946):
§11.8.3 / §11.4.3 carry no repetition rule, unlike §11.3.3 SR7 and §11.6.3 SR6 for the two INHERITS clauses.
RECEIVERS: an interface-typed `USAGE OBJECT REFERENCE IFOO` is legal (0813 accepts interfaces,
§13.18.60.2); INVOKE through it resolves over `AllPrototypes()` (§14.9.23.3 SR4e; 0825 on a miss) and emits
static C# interface dispatch behind the same GR5 null guard; `ObjectRefAssignmentMismatch` (kb/Work PB389) carries the
interface branch (class→implemented-interface via the closure; interface→inherited-interface) and ALL
NEW/RETURNING/argument paths route through it. PROPERTY (§13.18.42) DECLARATIONS are LIVE: the clause
synthesizes accessors per GR1/GR2 under the PINNED §11.7.4 GR1a names `__GET_<P>`/`__SET_<P>` — the
"clone" of the subject description is the SUBJECT DataItem itself (identical by construction), and the
emitter renders DIRECT field bodies (`=> subject;` / `{ subject = __V; }`) — observably identical to the
spec's implicit-MOVE methods; WITH NO GET/SET suppresses a side; FINAL carries. Explicit
`METHOD-ID. GET|SET PROPERTY p` methods join the roster under the SAME pinned names (real bodies), so
override/0829/implements machinery applies to accessors UNCHANGED. The 0842 band: SR6/SR7 accessor shapes,
SR5 clause+explicit duplicate, §13.18.42.3 SR4 superclass property collision, no-OCCURS subject, no-FILLER
subject. Property REFERENCES (`P OF obj` — the §8.4.3.9.4 GR1–GR3 implicit-INVOKE desugar with
BoundSequence + temps; detected at the ReferenceResolver resolution-failure chokepoint by the
single-qualifier + roster-property shape) are live. The GET's PLACEMENT follows the reference's evaluation
window exactly as a function activation's does (kb/Work PB987; §8.8.4.13 2)): a reference evaluated once per
statement is a statement-level pre-op, but one written in a per-evaluation window — a PERFORM UNTIL / VARYING
condition, a SEARCH WHEN, an EVALUATE object, a non-first AND/OR operand, a VARYING BY / AFTER FROM operand — is
drained by that window (`UdfBinder.Mark` marks BOTH pending lists; `OoBinder.OoDrainPropertyGets`) and fetched at
each evaluation, and not at all when a short-circuit never reaches it. Every such reference is sending, so the
window never needs the GR2 SET. STAGED (named 0899, never a generic guard): GET/SET
PROPERTY prototypes in interfaces. REGISTRY: interface-definition-2002, repository-interface-2002,
repository-property-2002, implements-clause-2002, property-clause-2002, method-property-selector-2002
(constructs.json rows for the four independently-reachable gates; bind-time `ConstructRegistry.Check` gates —
the INTERFACE-ID unit at `OoClassTable`, both repository specifiers in `DataBinder`, the PROPERTY clause in the
`DataBinder` clause loop; the `VersionConformancePass` funnel at end state). GOLDENS: oo_interface (two classes
implementing one interface, polymorphic dispatch through an interface-typed reference), oo_interface_covariant
(the adapter, compile+run), oo_property, oo_property_methods.

### D8. GOBACK inside a METHOD returns from the method only; STOP RUN ends the run unit. Emit a method-return path (a `return`/labeled break out of the method's PC-dispatch loop) distinct from the `StopRun` exception.

**Rationale.** §14.9.18.4 GR4 / method semantics: GOBACK in a method is a normal method return (control + any RETURNING value go back to the INVOKE site); STOP RUN (§14.9.42) terminates the whole run unit. Fix in the BINDER (G4): STOP RUN → `BoundStopRun`; GOBACK in a method context → a distinct `BoundMethodReturn` (carrying the RETURNING item, if any); each backend renders its own form. This is the single decision that silently miscompiles if missed.

**Rejected alternatives.** Keep throwing StopRun and catch it at each method boundary — rejected: a caught StopRun loses the RETURNING value and is fragile across nested INVOKEs; an explicit method return is correct and idiomatic.

**Signal architecture.** STOP RUN and GOBACK are distinct signals: STOP → `BoundStop` (`Binding/Procedure/Verbs/ControlFlowBinder.cs`) → `throw new StopRun()` (`CodeGen/StatementEmitter.cs`) unwinding to Main; GOBACK binds separately to `BoundGoback` → `CallEmitter.EmitGoback` (`CodeGen/Verbs/CallEmitter.cs`), which moves the RETURNING source, stages RAISING, and throws `ProgramReturn` — caught at the program's own `__Activate` (`CodeGen/DispatchEmitter.cs`), never crossing a CALL boundary (ProgramReturn = the settled SSOT §18 #10 carrier, `src/Cobol.Net.Runtime/Control/ProgramRegistry.cs`). A method-context GOBACK becomes `BoundMethodReturn` (D8); EXIT METHOD is edition-dependent — a method-return synonym at 2002/2014, REMOVED in 2023 (**Spec corrections #2**, specs/ISO_COBOL.md:27346-27381, :49034).

**Realization — the catch-at-entry form (chosen over the `return` form).** A method-context GOBACK executes inside however many NESTED bounded dispatch frames its out-of-line PERFORMs have stacked — a C# `return` exits ONE frame, resuming the outer PERFORM's loop (a silent wrong-control-flow miscompile; the test `OoSpineTests.MethodGoback_ReturnsFromMethodOnly_UnwindingNestedPerformFrames` reproduces exactly this shape). So the realization mirrors ProgramReturn precisely: `BoundMethodReturn` → `throw new MethodReturn()` (`src/Cobol.Net.Runtime/Control/MethodReturn.cs`), caught at the method's public entry — `try { __MDispatch(entry, last); } catch (MethodReturn) { }` (`CodeGen/Verbs/OoEmitter.cs`) — after which the entry returns the method's RETURNING item (the RETURNING LINKAGE local is the C# return value). The RETURNING value is NOT lost (the D8 rejection concerned conflation with StopRun, not the signal mechanism). STOP RUN inside a method deliberately stays `StopRun` — it must NOT be caught at the method boundary (§14.9.42). EXIT METHOD binds to the same `BoundMethodReturn` in a method (`OoBindExitMethod`), 0827 outside one; its 2023 removal rides the `exit-method-window` registry row (0900 below 2002 / 0902 at 2023).

### D9. Defer parametric polymorphism (method overloading by signature); v1 requires unique method names per class.

**Rationale.** Parametric polymorphism is an OPTIONAL feature in ISO §9.3.5.3 ('optional feature in this Working Draft International Standard'). COBOL resolves overloads by method-resolution-signature (PICTURE/USAGE/category, §9.3.5.3) but C# resolves by .NET type — lossy: PIC 9(4) and PIC 9(8) both map to `long`, colliding in C#. No conformance/NIST OO program uses overloading (verified: every METHOD-ID name is unique within its class). Deferring is safe and spec-permitted.

**Rejected alternatives.** Name-mangle each method by its full COBOL resolution signature (e.g. M__9_4) to keep distinct C# methods — rejected for v1: unidiomatic output and unneeded by the corpus; keep as the documented escape hatch if a future program needs overloading.

### D10. Universal/dynamic INVOKE → a virtual `object? __CobolInvoke(string name, object?[] args)` on CobolObject; each class overrides with a switch over its method roster.

**Rationale.** A method-name-in-a-data-item or a universal `object?` receiver cannot bind statically; a per-class generated switch is reflection-free and AOT/WASM-safe, and the literal-typed path stays the fast direct call.

**Rejected alternatives.** Reflection (MethodInfo.Invoke) — rejected: not AOT/WASM-safe, slow, and forbidden by the interop AOT rule. A global Dictionary<(Type,string),Delegate> — rejected: still needs runtime type lookup and is less debuggable than a per-class switch.

**Implementation.**
The signature is `void __CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning)`:
the mutable `CobolInvokeArg.Value` is the SR6 BY-REFERENCE
write-back channel and its `Descriptor` carries the runtime-conformance encoding
(`OoConformance.ConformanceDescriptor`, ONE rule beside DescriptionMismatch), compared by string equality AT
RUNTIME in the order §14.9.23.4 GR7 prescribes (kb/Work PB1500):
- **GR7 b) — resolution.** §9.3.6's match rules decide whether a method is BOUND at all: rule 1 (argument count,
  trailing OPTIONAL formals counting as equal; RETURNING present on both sides or neither), rule 3 (every universal
  argument is BY REFERENCE: an OMITTED one needs an OPTIONAL formal, any other one the formal's descriptor) and
  rules 6/7 (the RETURNING items share a SET/MOVE class, `OoConformance.ReturningMatchClass`). A method that does
  not match is NOT bound: its case `break`s out of the switch into `base.__CobolInvoke`, the search continues up the
  INHERITS chain (§9.3.6 2)/4)), and the CobolObject root sets EC-OO-METHOD (§9.3.6 6)) — a declarative on
  EC-OO-METHOD selects it.
- **GR7 c) — the bound method's conformance.** What §9.3.6 does not restate: an ANY LENGTH formal or returning item
  ("neither a formal parameter nor the returning item in the invoked method shall be described with the ANY LENGTH
  clause") and a RETURNING pair of one match class whose descriptions differ (§14.8.3.3). The EC-OO-UNIVERSAL
  exception condition is set to exist WHEN checking for it is enabled in BOTH the activated method and the
  activating runtime element; the method invocation is then unsuccessful and control continues per GR7g through
  the §14.6.13 EC machinery (declaratives / >>TURN), like every other EC. When checking is not enabled in both,
  EC-OO-UNIVERSAL is NOT set to exist; a nonconforming crossing cannot proceed in a typed-native model, so the
  implementor's abnormal-termination surface applies (`CobolImplementorFatalException`, no EC name).
- **GR7 d) — the external items (every INVOKE form, typed and universal, and PROPERTY accessors).** A method has
  no external items of its own (§13.4.3 SR1 keeps FILE SECTION out of a method; a method WORKING-STORAGE EXTERNAL
  item is refused), so its activation checks its factory's or object's: each class half with external items emits
  `private static void __DescribeExternals(int __self)` (the same `OoEmitter.EmitExternalDescribes` a program's
  `DescribeExternals()` is), and every method prologue — before the module-stack push, i.e. before GR7 e)'s
  transfer — calls `ExternalStore.DescribeAtMethodActivation(__DescribeExternals, <mask>)`. §14.8.4.1's pair: the
  activating half is the INVOKE statement guard's EC-EXTERNAL checking flags (the CALL boundary reads the same
  flags), the method's half is `OoMethodSymbol.ExternalCheckMaskHere`, folded before the METHOD's Environment
  division. A violation is a `CobolFatalException` carrying the Table 13 name, so the INVOKE's guard selects it like
  EC-OO-METHOD (kb/Work PB1138).

The case LABEL is the method's `OoMethodSymbol.DispatchKey` — its EXTERNALIZED name under the one case-insensitive
word mapping (`CobolObject.NormalizeMethodName`) the INVOKE literal (bind time) and identifier-2 (run time) take:
§8.3.2.2 1) maps a universal method-name "to the externalized name of the method to be invoked", the same roster
key the typed path resolves by (PB303), so `METHOD-ID M2 AS "MixedExt"` answers `"MixedExt"` and not `"M2"`
(kb/Work PB1405). A UNIVERSAL reference emits as **`CobolObject?`** (CobolObject IS the
runtime universal type, GR2b defers non-COBOL interop, no cast at dispatch sites). Roster rule: cases only
for methods the type DECLARES that are NOT overrides (the base's case + C# virtual dispatch delivers
overrides — proven by oo_universal_inherit's DERIVED-VOICE); an absent or non-matching name chains base — the chain IS §9.3.6;
zero non-override methods ⇒ no override emitted; BOTH type halves get switches (a universal can hold a
factory object). Box forms are CANONICAL BY DESCRIPTOR (each side's storage form is computed independently
per unit, so "box per your own storage" would desync): `S:*` →
string; `N:Display:*` → the display IMAGE string (the FormatDisplay/StoreDisplay overload pair bridges
native↔image on each side independently); other `N:*` → the native value; `O:*` → the reference. Binder:
`BoundInvokeUniversal`/`BoundUniversalArg` (facts differ in KIND from BoundInvoke — no roster exists);
SR6/SR7/SR8/SR10 + Tier-C = **0866**. SET Format 5 is live (grammar `dataReference+`; a
dataReference SENDER parses as the Format-1 shape by alternative order, so `BindSetTo` re-routes
SEMANTICALLY when either side is an object reference; NULL/SELF senders + SR13 class-name→factory-
singleton senders come through the gated rule; SR8/SR9/SR12 = **0867**; universal-into-typed is rejected —
the narrowing tool is an OBJECT VIEW, deferred to the EC-OO/object-view wave). Object relations are live
(Format 3 `=`/`<>` only + both-class-object = **0868**; identity renders
`object.ReferenceEquals(l, r)` in the ConditionRenderer's object branch BEFORE the figurative branch, so
NULL never width-materializes). Descriptor-vs-DescriptionMismatch drift protection is a UNIT MATRIX over
pictures ANALYZED by `PictureAnalyzer` (`PictureClauseIdentityDriftTests.ConformanceDescriptor_AgreesWithTheComparator_OverEveryCarriedPair`,
kb/Work PB1166) plus the behavioral EC-OO-METHOD (no match) / conforming-crossing pair over the 9(4)/9(8) hazard. Both
projections read the ONE PICTURE-clause identity (`PicInfo.Clause` — expanded character-string, currency STRING,
DECIMAL-POINT IS COMMA when a period/comma symbol is present), so a plain `X(n)` keeps the bare `S:n:J` key the
alphanumeric-group image pairs with and every other alphanumeric picture adds its clause key; a bit / national
group's key is `G:<usage>:<positions>` (it is an elementary item at the boundary, §14.8.2.1 NOTE).

### D12. Parameterized classes and interfaces (§9.3.12 / §9.3.13) are EXPANDED, one ordinary class per expansion — never C# generics; the parameterized definition itself is a skeleton that binds and emits nothing.

**Rationale.** §12.3.8.4 GR5 states the semantics as a construction: "The class object-class-name-1 is created
from the parameterized class object-class-name-2 by replacing each specification of the formal parameter by the
corresponding actual parameter" (GR8 the interface twin), and §9.3.12 then requires the result to be "treated in
all respects the same as if it were a class that is not a parameterized class", with "its own factory object".
The binder resolves method names, §9.3.8.2 conformance, typed `USAGE OBJECT REFERENCE` descriptions and INVOKE
rosters STATICALLY on a class NAME, and a formal parameter is unconstrained — so none of those can be answered
inside the definition, only inside an expansion. `Oo/OoExpansion.cs` therefore realizes GR5 literally: the
definition's own on-channel tokens are copied, each user-defined-word occurrence of a formal is replaced by its
actual, the header and end-marker names become the expansion's name, the USING clause and the header's
`AS literal-1` are dropped, and the run is RE-PARSED (`FragmentParse.ParseTokens`, tokens keep their line/column
so diagnostics point at the definition's own lines). The result enters `OoClassTable.Build` beside the written
definitions, and from there on NOTHING distinguishes an expansion from a written class — every later OO rule
(IMPLEMENTS conformance, override marking, factory singletons, EC-OO, USE Format 4) applies with no new code.
Token substitution is exact, not approximate, because §11.3.4 GR6 / §11.6.4 GR4 confine a parameter-name to
object-class-name / interface-name positions: every occurrence IS a specification of the formal. A formal written
in a DECLARATION slot (data-, condition-, paragraph-, section-, method-, file-, report-, screen-, property-name)
would be renamed silently, so it is COBOLNET2239; one written in a data REFERENCE fails downstream as an
undefined name once substituted.

**Determinations.** (1) IDENTITY is the expansion NAME (§9.3.12 / §9.3.13 "same externalized name … same actual
parameters … are the same class instance"): specifiers repeating one (name, definition, actuals) triple in any
number of source elements create ONE class; a name reused with a different definition or different actuals is
COBOLNET2240. (2) The skeleton's `AS literal-1` externalizes the SKELETON and is not carried into an expansion —
§9.3.12 forbids two expansions with different actuals to share an externalized name, so carrying it would make
every program with two expansions non-conforming by construction; an expansion's externalized name is its
specifier's object-class-name-1 (GR5: "a class object-class-name-1 is created"). (3) Only the header and end
marker are renamed: a METHOD-ID spelled like its class is legal (method-names and class-names are different
categories of user-defined word), so the definition's own name is not a
formal and is not substituted in the body; a body reference to it names the skeleton, which §12.3.8.4 GR1 confines
to the REPOSITORY paragraph. (4) An actual whose kind differs from its formal's REPOSITORY specifier (a class for
an `INTERFACE T` formal, or the reverse) is COBOLNET2240: the GR5 substitution would write a specifier naming the
wrong kind. (5) A defect in a skeleton line that no formal touches is found once per expansion and reported ONCE
(`EditionContext` records each distinct diagnostic line once — one fact, one report).

**Diagnostics.** COBOLNET2239 — the definition's own rules (§11.3.3 SR8/SR9, §11.6.3 SR4/SR7, §11.3.4 GR6 /
§11.6.4 GR4, §12.3.8.3 SR3), reported on the skeleton whether or not anything expands it. COBOLNET2240 — the
EXPANDS phrase (§12.3.8.3 SR4/SR7, §12.3.8.4 GR5/GR8, kind, identity). A reference to the skeleton anywhere but
a REPOSITORY paragraph is refused by `OoNameResolution.Resolve` through the REFERRING construct's own code with
the §12.3.8.4 GR1/GR7 reason (`OoClassTable.IsParameterized`). The whole surface rides the CLASS-ID /
INTERFACE-ID and REPOSITORY-specifier 2002 introduction gates (COBOLNET0900 below 2002).

**Rejected alternatives.** A C# generic `class C<T> where T : CobolObject` — rejected: an unconstrained T cannot
bind `INVOKE ITEM "SHOW"` statically, conformance per expansion (§9.3.8.2.4 "as if the actual parameter classes
or interfaces were substituted") would have to be re-derived at every use, and the binder's name-keyed model
would need a second, type-argument-keyed path everywhere. A deep CLONE of the parse tree with rewritten
terminals — rejected: the generated contexts carry labelled child fields and Start/Stop tokens that a generic
clone must re-point, where the re-parse builds them correctly by construction through the one grammar.

## C# mapping

TYPE + NEW + DISPLAY (from oo_hello.cob):
  CLASS-ID. GREETER INHERITS FROM BASE. / REPOSITORY. CLASS BASE. / OBJECT. / 01 MSG PIC X(13) VALUE "HELLO, WORLD!".
  / METHOD-ID. SAYHELLO. ... DISPLAY MSG.
  →
    public class GREETER : BASE {
        private string _MSG = CobolString.Store("HELLO, WORLD!", 13);  // instance field (OBJECT WS), VALUE-init in ctor
        public GREETER() { /* base() first; then VALUE inits */ _MSG = CobolString.Store("HELLO, WORLD!", 13); }
        protected override BASE__FACTORY __FactoryOfClass => GREETER__FACTORY.__Instance;   // FactoryObject
        public virtual void SAYHELLO() { System.Console.WriteLine(_MSG); }
    }
    public class GREETER__FACTORY : BASE__FACTORY { … protected override GREETER __Create() => new GREETER(); }
  Driver: INVOKE GREETER "NEW" RETURNING G  →  G = null; G = (GREETER?)GREETER__FACTORY.__Instance.__New();
          INVOKE G "SAYHELLO"               →  G.SAYHELLO();

LINKAGE (ref/returning) + per-instance state (from oo_method_args.cob):
  OBJECT WS: 01 BAL PIC 9(4). METHOD ADDTO / LINKAGE 01 LK-AMT PIC 9(4) 01 LK-RES PIC 9(4) / PD USING LK-AMT RETURNING LK-RES / ADD LK-AMT TO BAL / MOVE BAL TO LK-RES.
  →
    private long _BAL = 0L;  // per-instance
    public virtual long ADDTO(long LK_AMT) {        // USING→param (BY REFERENCE default; value-class so pass-by-value safe here), RETURNING→return
        _BAL = CobolNum.Store(_BAL + LK_AMT, 0, _P_BAL);
        return _BAL;                                 // MOVE BAL TO LK-RES then RETURNING LK-RES
    }
  INVOKE A1 "ADDTO" USING AMT RETURNING R  →  R = CobolNum.Store(A1.ADDTO(AMT), 0, _P_R);
  (BY REFERENCE for a typed value item that the method WRITES through → `ref long LK_AMT`; emit `A1.ADDTO(ref AMT)`. Justified by §9.3.6 match-rule 3c: BY REFERENCE requires same class/category, so a real typed `ref` is conformant — this is exactly why we drop the legacy ManagedPointer[] byte ABI.)

INHERITS + SUPER (from oo_super.cob):
  CLASS-ID. DOG INHERITS FROM ANIMAL. / METHOD SPEAK / INVOKE SUPER "SPEAK" / DISPLAY "DOG".
  →
    public class DOG : ANIMAL {
        public override void SPEAK() { base.SPEAK(); System.Console.WriteLine("DOG"); }
    }
  (ANIMAL.SPEAK is `public virtual void SPEAK()`. INVOKE D "SPEAK" on a DOG dispatches to DOG.SPEAK virtually.)

POLYMORPHIC SELF (from oo_self_polymorphic.cob):
  ANIMAL.DESCRIBE (inherited by DOG): INVOKE SELF "SOUND"; DOG overrides SOUND.
  →
    public class ANIMAL : CobolObject {
        public virtual void DESCRIBE() { System.Console.WriteLine("DESCRIBING:"); this.SOUND(); }  // this.SOUND() is virtual
        public virtual void SOUND() { System.Console.WriteLine("GENERIC"); }
    }
    public class DOG : ANIMAL { public override void SOUND() { System.Console.WriteLine("WOOF"); } }
  On a DOG, D.DESCRIBE() → this.SOUND() → DOG.SOUND() → "WOOF". (§8.4.3.8 GR2: SELF resolves on the runtime class.)

OBJECT GROUP + OCCURS per-instance (from oo_object_group.cob):
  OBJECT WS: 01 PERSON. 05 PNAME PIC X(4) VALUE "ANN". 05 PAGE-N PIC 9 VALUE 0. / 01 TBL. 05 SLOT PIC 9 OCCURS 3 VALUE 0.
  →
    private _T_PERSON _PERSON = new();                 // per-instance nested record struct
    private long[] _SLOT = new long[3];               // per-instance array; MOVE n TO SLOT(k) → _SLOT[k-1]=...
  (Group→record struct + fixed OCCURS→T[] mapping is identical to PROGRAM data; the ONLY delta is instance vs static field — no byte image.)

OBJECT REFERENCE storage — the §13.18.60.2 general format is a TUPLE (kind × FACTORY × ONLY × name),
carried by ONE `ObjectRefDescriptor` on PicInfo.ObjectRef; `ObjectRefDescriptor.ClrTypeName` is the SINGLE
derivation of the emitted type (PicInfo.ClrType and StorageForm.ObjectRef both read it):
  01 U USAGE OBJECT REFERENCE.                     → private CobolObject? U = null;   // GR22 b) universal (D-U1)
  01 R USAGE OBJECT REFERENCE IFOO.                → private IFOO? R = null;          // GR22 c) interface-name-1
  01 G USAGE OBJECT REFERENCE GREETER.             → private GREETER? G = null;       // GR22 d)1.b.
  01 GO USAGE OBJECT REFERENCE GREETER ONLY.       → private GREETER? GO = null;      // GR22 d)2.b. — EXACT class
  01 F USAGE OBJECT REFERENCE FACTORY OF GREETER.  → private GREETER__FACTORY? F = null;  // GR22 d)1.a.
  01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.        → private OWNER? A = null;         // GR22 e)2. — the CONTAINING class
  01 FA USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS. → private OWNER__FACTORY? FA = null;  // GR22 e)1.
  (ONLY and the FACTORY axis are STATIC facts the C# type cannot carry — an ONLY reference emits the same C#
   type as a plain one; §14.9.39.3 SR12 a)1./a)3. enforce them at BIND time, which is where the standard puts
   them. ACTIVE-CLASS emits the containing class: GR22 e) admits that class or a subclass of it, both
   assignable. §13.18.60.3 SR16 confines ACTIVE-CLASS to a factory definition, an instance definition, or a
   method's LINKAGE/LOCAL-STORAGE — COBOLNET1924; FACTORY OF / ONLY on an interface-name is COBOLNET1925.)
  SET G TO NULL → G = null;   IF G = NULL → G is null;   IF U = G → object.ReferenceEquals(U, G).
  ("IS class-name" is NOT ISO — no instance-of condition exists in the 2023 text; the runtime-type-test
  surfaces are the Format-3 relations above and OBJECT VIEWS (§8.4.3.5, EC-OO-CONFORMANCE — the deferred
  object-view surface). A vendor IS-class test would be dialect-gated extension surface, never default.)

DYNAMIC/UNIVERSAL dispatch (live; goldens oo_universal / _name / _inherit / _relation):
  INVOKE U MNAME USING X RETURNING R →
      var __ua = new CobolInvokeArg[]{ new("N:Display:4:0:U:None:-", <X boxed per descriptor>) };
      var __ur = new CobolInvokeArg("N:Display:4:0:U:None:-");
      CobolObject.RequireNonNull(U).__CobolInvoke(CobolObject.NormalizeMethodName(_MNAME), __ua, __ur);
      <X copy-out>; <R store>;   // SR6 write-back + GR8 delivery, forms per D-U6a
  // each class: public override void __CobolInvoke(string __name, CobolInvokeArg[] __a, CobolInvokeArg? __ret)
  //   { switch(__name){ case "BUMP": <GR7c checks> <unbox> this.BUMP(ref __p0); <re-box> return;
  //     default: base.__CobolInvoke(__name, __a, __ret); return; } }

## Hard problems

### COBOL permits MULTIPLE class inheritance — §11.3.2: `CLASS-ID. name … [INHERITS FROM { object-class-name-2 } … ]`. C# has only single class inheritance, so `: Base` cannot express two+ COBOL superclasses.

v1 RESTRICTS to single inheritance (every conformance/NIST program uses single `INHERITS FROM` — verified). Reject 2+ bases LOUDLY with a dedicated diagnostic. Settled in SSOT §18 item 18 (deferred owner decision — triggered only when a multi-base program actually appears). The decided escape hatch when it is needed: linearize the inheritance graph to one C# base chain + extract the secondary supers' instance interfaces into C# interfaces the class IMPLEMENTS, copying/forwarding the secondary supers' members — but that is design-deferred until a program demands it.

### GOBACK vs STOP RUN inside a method (the only correctness blocker). A method-context GOBACK must return from the method (with the RETURNING value), never unwind past the INVOKE caller.

Bind by context (semantics live in the binder, not an emitter flag): inside a method body the binder binds GOBACK to `BoundMethodReturn` (with the RETURNING item, if any) and STOP RUN to `BoundStop`; emitters only render each node. In a method, GOBACK (and, in pre-2023 editions only, EXIT METHOD — **Spec corrections #2**) terminates the method via `throw new MethodReturn()` caught at the method entry (the catch-at-entry form — a plain C# `return` would exit only ONE of the method's nested dispatch frames; see the D8 realization); STOP RUN keeps throwing the run-unit `StopRun`. The PROGRAM (Main) keeps the existing try/catch(StopRun). GOBACK and STOP RUN bind to distinct nodes (`BoundGoback`→ProgramReturn vs `BoundStop`→StopRun); the method-context split is `BoundMethodReturn`→MethodReturn (see D8 and "Greenfield seams").

### Per-instance vs static field selection must thread through the WHOLE statement/expression emitter (DISPLAY/MOVE/arithmetic/PERFORM read & write data items), not just field declarations — the legacy solved this with one chokepoint (ldarg.0;ldfld vs ldsfld).

The emitter's 'emit a data item reference' consults the bound item's owning scope: an instance object item emits `this._FIELD` (or `obj._FIELD` for a qualified receiver — but COBOL OO data is always SELF-relative inside a method), a static/program/method-WS item emits the bare static field. Because the statement emitters all funnel through one shared data-item rendering path, this is a single parameterization, mirroring the legacy chokepoint but in source. The per-unit emitter machinery keeps this small: program classes emit INSTANCE fields with per-unit emitter state, and inside its own class an instance field needs no prefix at all; the outer-storage case is solved with `private ref T fld => ref __outer.fld` bridge properties (see "Greenfield seams").

### DataBinder/DataItem/PicInfo currently model ONLY PICTURE items and silently ignore USAGE OBJECT REFERENCE (no PicCategory for it), so object-reference fields and INVOKE targets cannot be typed or resolved.

The bound model carries an `ObjectRefDescriptor` (Binding/Model/ObjectRefDescriptor.cs) on `PicInfo.ObjectRef`: **Kind ∈ {Universal, Interface, ObjectClass, ActiveClass} × Name × Factory × Only** — the four axes §13.18.60.2 prints. `DataBinder.OoBindObjectRefDescriptor` is the ONE adjudication of a written phrase (kind resolution against the pass-1 class table → 0813; the interface alternative's FACTORY/ONLY ban → 1925; §13.18.60.3 SR16 placement → 1924), and every consumer ASKS the descriptor rather than re-deriving the kind. ⛔ It replaced a single `string? ObjectClassName`, whose one axis made ACTIVE-CLASS and ONLY unrepresentable (and therefore REJECTED as legal source), left FACTORY OF loud-staged, and spelled "universal" as "no class name recorded" — a test equally true of every other shape (kb/Work PB389). INVOKE binding reads the descriptor to pick the roster: universal → the dynamic path, interface-name → `AllPrototypes()`, object-class-name/ACTIVE-CLASS → the named/containing class, and its FACTORY roster when FACTORY OF was written (§9.3.6 — a class has two separate method interfaces). A class-name target (not a data item) means NEW/factory dispatch.

**Conformance that depends on the INVOCATION (kb/Work PB1112).** An ACTIVE-CLASS formal or returning item means "the class of the object the method runs on", so ISO §14.8.2.3.2 rule 4, §14.8.2.3.3's ACTIVE-CLASS paragraph and §14.8.3.3 rule 2 state its conformance in terms of how the method is invoked. The INVOKE binder describes that invocation ONCE, as an `ObjectRefDescriptor` (`invokedWith` — §14.8.3.3 rule 2 b)'s four cases: a class-name is that object-class-name with ONLY, SELF and SUPER are ACTIVE-CLASS, an object reference is its own description), and hands it to the one comparator: `OoConformance.DescriptionMismatch` / `ContentMismatch` route an ACTIVE-CLASS formal of an activation to `ActiveClassFormalMismatch`, and an ACTIVE-CLASS returning item is delivered as `OoConformance.ReturningSender(returning, invokedWith)`. The standard class BASE's New / FactoryObject results (`OoBindStandardInvoke`) read the same view. The override / implements PAIR (§9.3.8.2.3 rule 2 d)) passes none and compares the two descriptions.

### Cross-compilation-unit resolution: the driver PROGRAM and each CLASS are separate program units in one compilation group; INVOKE in the driver references a class defined later in the file, and a method body references sibling methods/inherited members.

A genuine two-PASS over the compilation group, owned by the BINDER (G4): pass-1 walk every classDefinition and build the binder's class symbol table (class name, base name, method roster with each method's USING param modes + RETURNING presence/type, FACTORY-vs-OBJECT, attributes); pass-2 emit all class types and the Program into ONE C# source file (one compilation), so the C# compiler resolves all cross-type and inheritance references. The symbol table is used only to marshal INVOKE args and select the call form.

### INVOKE on a NULL object reference must raise EC-OO-NULL (§14.9.23.4 GR5) through the EC engine, not throw a raw NullReferenceException.

For a typed INVOKE emit a guard `recv ?? throw new CobolException(EC.OO_NULL)` (or a small `CobolObject.RequireNonNull(recv)` helper) before the call. **CORRECTION (Spec corrections #3):** INVOKE has NO exception phrase in ISO (the §14.9.23.2 format ends at RETURNING, specs/ISO_COBOL.md:28376-28390; the dead sketch's `invokeOnException` must not be revived). INVOKE failures route exclusively through the §14.6.13 EC machinery: EC-OO-NULL (:28506) / EC-OO-METHOD (:28528) raised as fatal exception conditions, handled by USE declaratives / >>TURN checking like every other EC — no per-statement handler. The full EC-OO family (Table 13, :24748-24756) is integrated.

### Method LINKAGE + PROCEDURE DIVISION USING/RETURNING: the live grammar carries USING on procedureDivision and RETURNING gated {is2002()} — but mapping positional COBOL params (BY REFERENCE default) to typed C# params requires per-method param-mode info at the INVOKE site, across units.

The pass-1 symbol table records each method's ordered formal parameters with mode (REFERENCE/CONTENT/VALUE/OMITTED-capable) + type, derived from its LINKAGE items and the PD USING/RETURNING. At an INVOKE, marshal each argument per §14.9.23.4 GR6 (BY REFERENCE assumed when the arg qualifies and the formal is REFERENCE; else CONTENT) → emit `ref`/copy/value accordingly; RETURNING → assign the C# return into the receiving item via the normal MOVE/Store path.

## Edge cases

- INVOKE on a null object reference → EC-OO-NULL (§14.9.23.4 GR5, specs/ISO_COBOL.md:28506), raised through the §14.6.13 EC engine. CORRECTED (Spec corrections #3): INVOKE has NO ON EXCEPTION phrase — no inline handler exists or may be designed; USE declaratives / fatal-EC semantics apply.
- Unknown method / method not found at the receiver's class hierarchy → EC-OO-METHOD (§14.9.23.4 GR7b). For the static typed path this is a COMPILE-time COBOL diagnostic raised by the BINDER (method-name lookup over the pass-1 class symbol table; backend-neutral per G4 — a Roslyn CS error here would be an emitter bug); for the dynamic __CobolInvoke path it is the switch default → raise EC-OO-METHOD.
- FACTORY data is one copy PER CLASS (§8.6.4 :8765 — including a class that merely INHERITS it: DOG-without-a-factory-paragraph still owns its OWN copy of ANIMAL's factory data — the anti-static fact, proven by oo_factory's FC=02/FC=01); factory data lives as INSTANCE fields of the per-class factory object, factory methods as its virtual members; the factory object is ONE PER RUN UNIT (`__Instance` reads `RunUnit.Current.FactoryObject<CLS__FACTORY>()`, created at the first reference — §9.3.14.2 "created before it is first referenced by a run unit", kb/Work PB1069 — never a process static, which outlived the run unit); `INVOKE Class "M"` (non-NEW) → `CLS__FACTORY.__Instance.M(…)`.
- Method WORKING-STORAGE (pre-2023 editions ONLY — ILLEGAL in 2023 per §13.5.3 SR 1, Spec corrections #1) persists across activations and is shared across instances → STATIC fields (NOT per-instance) — counterintuitive; the naive instance-field mapping silently miscompiles a method-WS counter. At `--std 2023` it is a versioned diagnostic instead.
- Method-local data-name identical to an object data-name → the method-local declaration wins inside the method; the object data is inaccessible there (§11.7.4 GR5 — "If a given user-defined word is defined in the data division of this method definition and in the data division of the containing object definition, the use of that word in this method refers to the declaration in this method. The declaration in the containing object definition is inaccessible to this method"). Emit the local/static method field, shadow the instance field. **⛔ That precedence is `SymbolTable.TryResolve`'s, and NO binder may resolve a name any other way (kb/Work PB467).** A rule that is a predicate over the *declaration* rather than over a storage place — the §14.9.39.3 SR18 / §14.9.3.3 SR1 based-item screen is the one that got this wrong — asks `ReferenceResolver.DeclarationOf`, never `DataBinder.ByName` directly: the unit-wide multimap cannot see a method, so it answered a scoped question with the wrong item and the verdict went wrong in BOTH directions (a legal method-local `01 MB BASED` refused; a method-local non-based item shadowing an object-level BASED one accepted for rebasing). `BasedItemScopeTests` moves one declaration between the two scopes and asserts the verdict follows it.
- RETURNING on a method declared void, or a void INVOKE that omits RETURNING when the method has one → compile-time COBOL diagnostic from the binder's signature check against the pass-1 symbol table (G4: the binder owns conformance diagnostics for both backends; Roslyn merely re-enforces in the rendered C#).
- ~~NEW on an ABSTRACT class~~ — DROPPED (Spec corrections #4): ABSTRACT is not ISO; no ISO construct produces an abstract class, so this case cannot arise on the ISO surface (revisit only if a vendor-dialect ABSTRACT extension is ever added).
- OVERRIDE without a matching base signature, or overriding a FINAL method → compile-time error (§11.7 SR3; C# `override` of a non-existent/sealed base is a compile error — reject at bind for a COBOL-worded diagnostic).
- Object data that is a GROUP or fixed OCCURS table → per-instance record struct / array, byte-identical mapping to PROGRAM data except instance-vs-static (proven by oo_object_group: R1 filled, untouched R2 keeps defaults).
- SELF in a FACTORY method resolves on the factory interface; SELF in an instance method on the instance interface (§14.9.23.3 SR4f/g) — pick `this` consistently; SUPER must name object-class-name when INHERITS lists 2+ bases (§8.4.3.8.3 SR5) — moot under the v1 single-inheritance restriction.
- Universal object reference (`OBJECT REFERENCE` with no class) → C# `CobolObject?` (D-U1); INVOKE through it forbids BY CONTENT/BY VALUE (§14.9.23.3 SR6) and uses the __CobolInvoke dynamic path; `identifier-2` (method-name in a data item) is allowed only for a universal reference (§14.9.23.3 SR7).
- `SET o TO NULL` → `o=null`; `IF o = NULL`/`o = q` → reference comparison `o is null`/`ReferenceEquals` (`o IS Class` is NOT an ISO condition — see the C# mapping note above).
- INVOKE arg is a literal or BY CONTENT → synthesize a copy (a temp local) so the method cannot mutate the caller's literal/source — mirrors the §14.9.23.4 GR6 content semantics.
- Same class inherited indirectly twice (diamond, §11.3.2 GR4) → one copy of its data; under single-inheritance v1 this cannot arise via direct INHERITS, but note it for the eventual multiple-inheritance design.
- Method-name AS literal-1 (externalized name, §11.7.4 GR1b) is MANDATORY for a conforming 2023 implementation — §A.4.10 enumerates EXACTLY three optional OO items (CLASS-ID INHERITS repetition, INTERFACE-ID INHERITS repetition, parametric polymorphism §9.3.5.3) and this is not among them — so it is a sequenced obligation, never optional scope; v1 currently uses the COBOL method-name directly as the C# method name (sanitized). CALL-CONVENTION/ENTRY-CONVENTION naming rides the same sequencing.

## Legacy port map

> The legacy OO design docs were deleted; the legacy code (`src/CobolSharp.Compiler`) is the only remaining
> record. This section records the slice order legacy landed, each slice's portable algorithm/decision, and —
> explicitly — what is NOT portable (the byte substrate). Port ALGORITHMS and slice sequencing; never the
> ManagedPointer/ProgramState mechanics.

### Slice 1 — CLASS-ID + one METHOD-ID + NEW + no-arg INVOKE + USAGE OBJECT REFERENCE — ✅ done (multi-method scoping landed with it via per-method scopes)
- **Portable — class-unit routing + group checks.** Compilation collects classDefinition contexts alongside program units; a case-insensitive `classNames` set is built up front and fed into semantic-model construction so class names are whitelisted in reference resolution (no undefined-name diagnostic on an INVOKE class target); after bind the module is tagged IsClass / ClassMethodName / BaseClassName (`CobolSharp.Compiler/Compilation.cs:60,125,140-194,531-548,749-767`). Substrate-free; ports directly (greenfield home: the pass-1 class symbol table after CallCollectUnits — see Greenfield seams).
- **Portable — the object-reference data model.** Usage alt → an Object usage kind + declared class on the symbol (`Semantics/SemanticBuilder.cs:1278,1596`); PIC-less legal → ObjectReference category (`PicUsageResolver.cs:58-60`); occupies NO storage (`StorageLayoutComputer.cs:230-233`); .NET default null IS COBOL initial NULL (no init emitted); usage-mapper default hardened to Unknown so a stray usage keyword can't masquerade as a zero-storage object ref.
- **NOT portable.** The `static <class> _OBJ_<name>` field registry (`CodeGen/Binder.cs:555-572`) — static fields are correct only for driver-program refs and can't live inside a class instance. Greenfield: an object-ref item is just a C# field of the class type (instance field when it lives in OBJECT data) — the limitation dissolves.

### Slice 2 — INVOKE USING/RETURNING + the COBOL0111 arg hardening
- **Portable — INVOKE discrimination + declared-class dispatch.** `CallBinder.BindInvoke` (`Semantics/Bound/Binding/CallBinder.cs:30-101`): SELF/SUPER detected from the objectReference alternatives (receiver = `this`); "NEW" recognized by method name on a class target; instance call resolves the receiver symbol and dispatches on its DECLARED class (`DataSymbol.cs:102`). (A real FACTORY design supersedes the NEW-by-name shortcut.)
- **Portable — the "never silently drop an argument" rule.** A dropped/unbound USING arg SHIFTS the trailing RETURNING slot (the blocker: first USING param bound to the RETURNING pointer, runtime crash, no diagnostic). Legacy rejected literal / BY VALUE / BY CONTENT args loudly (COBOL0111); greenfield must bind them COMPLETELY (D6) — either way, arity/mode mismatch is a loud compile error, never a silent shift.
- **NOT portable.** The whole `ManagedPointer[]` positional ABI, `public virtual void M(ManagedPointer[] args)` signatures, the STATIC `_linkage_<name>` fields (non-reentrant; sibling-method LINKAGE cross-wiring forced the COBOL0117 reject — `CilEmitter.cs:552-591`). Greenfield: real typed C# parameters + a real return value (D6); keep only the ORDERING contract (USING params in declaration order, RETURNING as the distinct out-slot) + the loud diagnostics.

### Slice 3a — INHERITS FROM + virtual dispatch/override — ✅ done (the Cecil depth-sort is obsolete — Roslyn resolves declaration order; override-under-exact-name is subsumed by the uppercase CsName convention)
- **Portable — emission ordering.** Classes before programs, base before subclass: sort IsClass first, then InheritanceDepth ascending (cycle-safe walk over BaseClassName with a seen-set), programs last, STABLE so the first program keeps the entry point (`CilEmitter.cs:101-145`). For Roslyn this is "generate all class declarations into one compilation so base types + INVOKE targets resolve"; the depth-sort stays reusable for any per-class ordering need.
- **Portable — override under the base's EXACT name.** COBOL method names are case-insensitive; .NET member matching is case-sensitive — an override whose spelling differs from the base ("speak" vs "SPEAK") silently took a NEW slot and dispatched to the base (a miscompile). Rule: detect overrides case-insensitively across the base chain and emit `override` under the base's exact spelling.
- **Portable — group diagnostics.** Unknown INHERITS base → loud (never degrade to a root class); subclass-own OBJECT data unsupported → loud, checked against the OBJECT paragraph's WS PARSE SUBTREE, not the flattened symbol list — methods' LINKAGE OCCURS INDEXED index-names are force-placed into WS (ISO §8.5.1.2) and false-fired the symbol-list check (`Compilation.cs:749-767`).

### Slice 3b — INVOKE SUPER (non-virtual) + SELF dispatch semantics — ✅ done (`this.M()` virtual / `base.M()` non-virtual per D5; SUPER-in-root → 0827; generated members are `__`-namespaced so the legacy name-collision wart cannot recur)
- **Portable — the three dispatch rules** (`CilEmitter.cs:1670-1760`): SELF → receiver `this`, resolved on the CURRENT class + chain, called VIRTUALLY (a subclass override wins even when the SELF call sits in an inherited base method — oo_self_polymorphic); SUPER → receiver `this`, resolved starting at the BASE class, called NON-virtually so override-calls-base cannot recurse; plain instance → receiver's declared class, virtual (a base-typed ref holding a subclass dispatches to the override — oo_inherit). C# mapping: virtual call / `base.M(...)` / virtual call — D5 already encodes this.
- **Portable — SUPER in a root class = clean compile diagnostic** (legacy COBOL0115, detected by scanning the module for the IsSuper invoke; `Compilation.cs:760-761`), never an internal error.
- **Legacy wart to NOT port:** by-name-only method resolution could collide with synthesized helper names (Dispatch/InitializeState); greenfield must namespace/escape generated members.

### Slices "typed-native flip" + multi-method — instance state + method scoping
- **Portable — the instance-state decisions:** per-instance state lives on the ROOT class only; a subclass adds only its own NEW fields (once subclass data is supported) and its parameterless ctor (= the COBOL NEW factory, D4) just chains base, which allocates+initializes exactly once; the TWO-OBJECT INDEPENDENCE test is the canonical proof (a static/shared field silently passes every single-object test — the #1 risk). In Cobol.Net this collapses naturally: OBJECT data = instance fields (record-struct members / natives) per the existing data mapping.
- **NOT portable:** the byte `State` field, `EmissionContext.StateIsInstance` chokepoint flag, `EmitTypedFieldOwner` ldarg.0 plumbing, ctor-time InitializeState — all ProgramState mechanics (`CilEmitter.cs:874-1110`, `Emission/CilDataEmitter.cs:33-46`). By the end of that legacy work the byte State was already EMPTY for a fully-typed class — the greenfield starts there.
- **Portable — the per-method paragraph-scope algorithm (ports VERBATIM onto the greenfield PC-dispatcher):** (1) each METHOD-ID gets its own child Scope; its paragraphs declare METHOD-LOCALLY (siblings may reuse paragraph names; PERFORM/GO TO resolve method-first, then program-wide) — and EVERY paragraph-resolution pass must be equally method-aware or the feature re-breaks (legacy needed it in three places: `SemanticBuilder.cs:1762-1810`, `ReferenceResolver.cs:41-51`, `ProcedureNameResolver.cs:111-193`); (2) the binder builds a method roster (Name, EntryParagraphIndex, LastParagraphIndex, USING param names in ABI order) by grouping the paragraph dispatch order on the paragraph's declaring scope (`CodeGen/Binder.cs:985-1005`); (3) each public method runs ONLY its contiguous range `Dispatch(entry, last)` with the exit bound = its LAST paragraph (NOT −1/end-of-program), so falling off the end is the implicit GOBACK instead of running into the sibling method's paragraphs (`CilEmitter.cs:370-391`). Guard with the fall-through test (`OoTests.Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond`). SECTIONs inside methods SILENTLY truncated the range to [first..first] — legacy rejected loudly (COBOL0116); greenfield must reject or implement deliberately.
  **In the greenfield (two deliberate deltas):** (a) resolution inside a method is CONFINED to the method's own scope with NO program-wide fallback (`OoMethodScope` + the ResolveProcedure method branch) — the legacy's "then program-wide" leg would have made a cross-method PERFORM resolve instead of failing (trap #10); the greenfield has exactly ONE resolution pass (ResolveProcedure), so the three-places re-break hazard is structural-impossible. (b) SECTIONs inside methods are IMPLEMENTED deliberately (a section registers in the method scope; its paragraphs are contiguous within the method range) — COBOL0116's reject is superseded, and the guard is `OoSpineTests.Trap4_MethodFallThrough_DoesNotEnterSiblingMethod` + `Trap10_CrossMethodPerform_FailsLoud`.
- **Portable — two-phase method emission:** declare all method signatures BEFORE any body so (a) INVOKE SELF resolves a sibling and (b) the public COBOL method names are reserved first, with paragraph-helper names uniquified AROUND them; keep the dispatch map keyed by method identity, not name (`CilEmitter.cs:335-362`).

### Never landed in legacy (net-new design here, NOT a port)
FACTORY (§11.4, D7), PROPERTY (§13.18.42), and EC-OO (Table 13) are net-new here and now implemented. Also net-new and implemented: per-method LINKAGE with multi-method params (per-method data scopes + typed `ref` params; the trap-#6 cross-wiring test is `OoSpineTests.Trap6_SiblingMethodLinkage_NoCrossWiring`); method-scoped SECTIONs (method-scoped ranges); INVOKE literal / BY CONTENT arguments (bound fully per D6; BY VALUE stages 0828 pending the header BY-phrases); and SET on object references (D-U7). Still open: subclass-own OBJECT data; data-name method selector (universal-ref-only per SR 7); object references held BY a class instance (an object-ref item in OBJECT data works today — oo_hello-shaped drivers hold refs in program WS; a CLASS holding refs to OTHER classes rides the same PicInfo machinery). (Per §A.4.10, specs/ISO_COBOL.md:40408-40414, only THREE OO items are optional — multiple-inheritance repetition ×2 and parametric polymorphism; everything above is MANDATORY for a conforming 2023 implementation.)

### Adversarial regression traps (each was a REAL caught bug — reproduce as day-one greenfield tests)
1. Two-object independence (shared-static corruption; oo_instance_data pattern b1/b2/b1).
2. Case-mismatched override still dispatches (emit under the base's exact name).
3. Dropped/reordered INVOKE arg shifting the RETURNING slot (bind literal/BY VALUE/BY CONTENT completely or reject loudly).
4. Method fall-through into the next method's paragraphs (exit-bounded range).
5. SECTION inside a method truncating the dispatch range.
6. Sibling-method LINKAGE cross-wiring.
7. SUPER in a root class → clean diagnostic, not an internal error.
8. INHERITS unknown base → loud, never silently a root class.
9. Subclass-own-data detection must not false-fire on synthesized index-names placed in WS (ISO §8.5.1.2).
10. Three-level SUPER chains; state allocated exactly once across ctor chaining; PERFORM/GO TO method-local resolution incl. backward GO TO; cross-method PERFORM rejected.

**Reusable test assets:** `tests/conformance/2002/oo_hello|oo_instance_data|oo_method_perform|oo_method_args|oo_inherit|oo_super|oo_self|oo_self_polymorphic|oo_object_group` (.cob + .out goldens — 9 pairs), all compiled strict + run + byte-compared by CorpusRunnerTests (oo_instance_data = trap #1 two-object independence; oo_method_args = USING/RETURNING). The adversarial suite is `tests/Cobol.Net.Tests.Conformance/OoSpineTests.cs` (traps #4/#10, the D8 GOBACK/STOP RUN/EXIT METHOD split, the 0813/0820–0827 diagnostic band). The legacy `OoTests` suite (`tests/CobolSharp.Tests.Integration/OoTests.cs`) remains the port checklist reference.

## Greenfield seams (the compiler seams the OO subsystem plugs into)

### Grammar seam — the OO surface already parses
The LIVE fragment is `src/Cobol.Net.Frontend/Grammar/Core/CobolOO.g4:18-98`: classDefinition (CLASS-ID, single INHERITS FROM), objectParagraph, methodDefinition (full env/data/procedure divisions), invokeStatement (USING BY VALUE/REFERENCE/CONTENT/bare/literal + RETURNING, NO exception phrase — already ISO-correct per Spec corrections #3), and objectReferenceUsage as TWO explicit alternatives (the lesson: never an optional `className?` tail — it regressed '85 `IS [NOT] NUMERIC`). `CobolParserOO.g4` is a DEAD unbuilt sketch (regen inputs are only `Grammar/CobolParserCore.g4;Grammar/Core/*.g4`, `Cobol.Net.Frontend.csproj:44`) — its FACTORY/attributes/generics/invokeOnException content is reference-only, and invokeOnException is spec-WRONG. Gate inventory (nine `{is2002()}?` hooks): `CobolParserCore.g4:105` (classDefinition in compilationGroup), `:412` (repository CLASS entry), `:442` (PD returningClause/raisingClause), `:626-627` (ALLOCATE/FREE), `:663` (invokeStatement), `:904` (BY VALUE arg), `:1004` (SET … TO objectReference), `:1061` (GOBACK RETURNING/GIVING), `Core/CobolData.g4:312` (objectReferenceUsage). **`inlineMethodInvocation` is NOT among them and must not become one** (kb/Work PB428): the §8.4.3.4 construct parses at EVERY edition and the `VersionConformancePass` names 2002 on RECOGNITION (`VisitInlineMethodInvocation` → `Check(InlineMethodInvocation2002)`), because the §8.7.4 `::` invocation operator is a distinctive token — a predicate would give a raw `COBOL0001` about punctuation where a named `COBOLNET0900` belongs. Missing: method-name AS literal and class OF SUPER only — added incrementally per the Version-gating rules. Edition-gates to ADD per the Spec corrections: method-WS rejection at 2023 (#1) and `EXIT METHOD` removal at 2023 (#2, `Core/CobolControlFlow.g4:213`).

### Binder seam
`CallCollectUnits` collects `classDefinition` units and builds the pass-1 `OoClassTable`
(`src/Cobol.Net.Compiler/Oo/OoClassTable.cs` — class → base link → method roster with HasUsing/HasReturning;
structural diagnostics 0820 duplicate-class/END-mismatch, 0821 unknown-INHERITS-base [trap #8], 0822
duplicate-method [D9]) BEFORE any unit binds, so a class defined anywhere in the group is RESOLVABLE from any
unit — the cross-unit forward reference D1 exists for.

⛔ **RESOLVABLE IS NOT REFERENCEABLE, and `OoNameResolution` (`src/Cobol.Net.Compiler/Oo/OoNameResolution.cs`)
is the ONE funnel that composes the two.** ISO §8.4.6.4 scopes every written object-class-name and
interface-name to "the name of the containing object class definition or declared in the REPOSITORY paragraph
of that or a containing source element" — a strictly NARROWER set than the group table, and the set every
per-construct rule instantiates (§11.3.3 SR2 INHERITS, §11.4.3 SR1 / §11.8.3 SR1 IMPLEMENTS, §11.6.3 SR2
interface INHERITS, §14.2.2 SR8/SR9 the RAISING phrase, §14.9.49.3 SR16/SR17 USE Format 4). The scope is
computed by walking the reference's parse-tree ancestors — §12.3.3 SR1/SR2/SR3 leave only the outermost
program, the function definition, the class definition and the interface definition able to carry a
REPOSITORY, and §12.3.4 GR1 spreads each one's entries to every contained source unit — memoized per source
element on `OoClassTable.RepositoryScopeFor`. `Resolve` diagnoses through the REFERRING construct's own code
(0813, 0821, 0840, 0859 …); `Lookup` is the non-diagnosing half for the sites that PARTITION on "is this word
a class-name here?" (INVOKE's receiver, the RAISING word, the property-reference qualifier). Every site called
`Find`/`FindInterface` directly until kb/Work PB365, so `USE AFTER EXCEPTION OBJECT C` in a program with NO
REPOSITORY compiled clean; `OoNameResolutionDriftTests` pins the remaining direct callers (all of them
re-lookups of an already-scope-checked declared name).
⛔ **A SPECIFIER'S `AS literal-n` MAKES THE WRITTEN WORD A LOCAL NAME** (kb/Work PB974). §12.3.8.2 prints `CLASS object-class-name-1 [ AS literal-1 ]` and `INTERFACE interface-name-2 [ AS literal-2 ]`, and §12.3.8.4 GR2 makes the literal "the externalized name by which the class [or] interface ... is known to the operating environment". So the scope is a MAP, declared word → externalized name (null when no AS was written, and for the containing definition's own name): `OoRepositoryScope.ClassTarget` / `InterfaceTarget`, and `Lookup` finds the definition by `OoClassTable.FindByExternalizedName` when a literal was written, by the declared word otherwise (the DOC-A.1-163 determination). The literal is read by the pure `ExternalizedName.Peek`; `DataBinder.BindSpecifierExternalizedName` is its one diagnosing screen (§12.3.8.3 SR2, COBOLNET1761), and SR1 — a repeated name's specifications identical — is `DataBinder.CheckRepositorySpecification`, over the whole specification (kind, AS literal, EXPANDS phrase) and keyed on the name across every kind (kb/Work PB1017), so `CLASS H EXPANDS P USING A` beside `CLASS H EXPANDS P USING B`, or a name specified as both a CLASS and an INTERFACE, is COBOLNET1761. A USAGE OBJECT REFERENCE descriptor carries the RESOLVED definition's name, never the local word, so every re-lookup of a descriptor name stays a lookup in the group table. The property specifier's `AS literal-4` is the property's name in the declared classes (§12.3.8.3 SR16 a)), read by `OoTryBindPropertyReference`.
⛔ **INVOKE AND THE INLINE METHOD INVOCATION ARE ONE ACTIVATION, NOT TWO** (kb/Work PB428; `OoBinder.InlineInvocation.cs`).
§8.4.3.4.4 GR1 does not merely resemble the INVOKE statement — it DEFINES the §8.4.3.1.2 Format 4 identifier as one of
four INVOKE statements written out longhand, and §8.4.3.4.3 SR3 requires that statement to be valid under §14.9.23's own
syntax rules. So the two syntaxes meet at an `InvocationSite`: the argument operands (as `InvocationArg`, decoupled from
whether a BY phrase could be written), whether an argument phrase was written at all, and the RETURNING destination —
either INVOKE's written identifier or, for the inline form, the GR1 b)/c) TEMPORARY. `OoBindByReceiver` (SELF/SUPER,
typed/interface/universal identifier-1, class-name-1), `OoBindInstanceInvoke`, `OoBindClassInvoke`,
`OoBindResolvedInvoke` and `OoBindInvocationArg` are shared verbatim; the inline binder adds exactly two things —
`DataBinder.OoCreateInvocationTemp` (the GR1 b) clone of the method's own RETURNING item, so the delivery is an IDENTITY
crossing and §14.8.3.3 has nothing to check) and registration of the activation on `DataBinder.PendingPreOps`, the ONE
statement-scoped pre-op list the user-function activation and the function-bearing subscript already use. It hoists
through `UdfBinder.UdfWrapCalls` / `UdfAttachPerEvaluation` unchanged, because §8.4.3.4.3 SR1 gives the construct the
same non-receiving property §8.4.3.2.3 SR1 gives a function-identifier: no store polarity, no post-ops. The expression
the site yields is a `BoundNumRef` over the temp — the SAME carrier a user-defined function result takes, so every
general-operand chokepoint (`IntrinsicBinder.OperandOf` → `BoundFieldOperand`) serves it with no new code. SR2 (no NULL,
no universal receiver) is COBOLNET2138, GR1 b)'s missing RETURNING is COBOLNET2139 and SR4 (ANY LENGTH / ACTIVE-CLASS
RETURNING) is COBOLNET2140. SR1 holds STRUCTURALLY for every purely receiving rule — the operand alternative was added
to exactly the rules that admit `functionCall` and to no receiving rule, pinned by
`InlineMethodInvocationOperandDriftTests` — but four of those rules are MIXED-ROLE (the arithmetic TO / FROM / BY /
INTO operand: a receiver in Format 1, a sender in Format 2), and there SR1 is the bind-time Format-1 screen every
arithmetic verb asks through the ONE `ArithmeticOperandRole` classification (COBOLNET1689; kb/Work PB1142, pinned by
`ArithmeticOperandRoleDriftTests`). As an arithmetic SENDING operand the invocation takes the same §8.8.1.1 class screen
a function-identifier does (`ExpressionBinder.BindIdentifierOperand`, COBOLNET0844): the temporary carries the method's
RETURNING class (§8.4.3.4.4 GR1), so an alphanumeric result is not an arithmetic operand; a SOLE invocation stays a
legal relation / EVALUATE comparand (`ConditionBinder.SoleInlineInvocation`).

INVOKE binds via `OoBindInvoke` (`Binding/Procedure/Verbs/OoBinder.cs`): identifier-1-shadows-class-name resolution, literal
selector, `BoundInvoke(Form, …)` for NEW (RETURNING required + §14.8 receiver conformance — 0826) and the
no-arg instance call (unknown method 0825 — the compile-time GR7b analog); SELF/SUPER (3b), factory calls,
USING/RETURNING marshaling, and universal/dynamic dispatch (D10) all bind. Method
bodies bind through `BindClassBody` — per-method `OoMethodScope`s confine paragraph/section resolution
(§11.7; a cross-method PERFORM/GO TO fails loud with the method-scope hint — trap #10 structural); method
GOBACK → `BoundMethodReturn` (D8), EXIT METHOD → same in a method / 0827 outside, EXIT PROGRAM in a method →
0827 (§14.9.14.3 SR7). SET on object references (Format 5) and RAISE identifier / EXIT PROGRAM RAISING
identifier bind through the same OO machinery (D-U7, EC-OO).

### Emitter seam
OO binding lives in `Oo/OoDriver.cs` (a `BinderDriver` collaborator): `OoClassUnit` (the ClassUnit counterpart of the
program unit), the two-phase bind (`BindClassData` for EVERY class — the synthetic-unit DataBinder + `OoBindMethodData`
per method between `BindDeclarations`/`BindResolve`, so signatures exist before ANY body binds regardless of source
order — then `BindClassBody`). `BinderDriver.Bind` runs `BindClassBody` only after EVERY program unit's DATA division
has bound and the group's REPOSITORY tables (`BindSession.Repository`) exist, and hands each class binder the class
REPOSITORY's user functions and program prototypes (`BinderDriver.UserFunctionsOf` / `ProgramPrototypesOf`) — §12.3.4
GR1 applies a class definition's configuration section to its factory, object and methods (kb/Work PB1100). OO emission lives in `CodeGen/Verbs/OoEmitter.cs`: `OoEmitter.EmitClassUnit` — the SAME per-unit
emitter-state switch as the program-class emitter renders `public class FOO : CobolObject` with FieldEmitter
INSTANCE fields (VALUE → field initializers; the implicit public ctor IS New's creation step — D4) and
method-WS STATIC fields (D3).
**Dispatcher realization:**
each METHOD-ID emits as `public virtual <ret> M(ref …)` whose paragraphs render inside a LOCAL FUNCTION
`__MDispatch` over the method's contiguous slice of the class's ONE pc space (`EmitDispatchMethod(header,
from, to)`; `_dispatchName` threads the PERFORM/SORT emit sites). The local function is what lets paragraph
code address the method's LINKAGE/LOCAL-STORAGE C# LOCALS by capture (a `ref` parameter is NOT capturable —
hence the param→local copy-in/copy-out at the method boundary); captures lower to by-ref frames (zero
allocation), and recursion (methods are implicitly RECURSIVE, :12032) is reentrant because every activation
owns fresh locals. Falling past the slice's last paragraph returns (trap #4); `__N` stays one class-level
const. INVOKE renders in `OoEmitter.EmitInvoke`: New → `OoEmitter.EmitNew` (the factory's `__New()`, D4/D5); instance → the D6 marshaling
around `CobolObject.RequireNonNull(recv).M(…)` (GR5; see the D6 implementation note). Classes emit before programs
(source order; Roslyn needs no depth sort — the legacy Cecil ordering is obsolete); a class-only compilation
unit emits classes + an empty Main.
The per-unit emitter machinery this builds on emits one `internal sealed class _PRG_<NAME> : ICobolProgram` per program into ONE `.g.cs` — each with INSTANCE fields, a ctor taking the container, instance `__Activate()` + `__Dispatch(int,int)` PC-loop methods, and `private ref T fld => ref __outer.fld` bridge properties. The per-unit emitter-state switch IS the "emit fields+paragraphs+statements into a type parameterized by (type, instance-vs-static, storage-source)" shape this doc's Summary calls for. The pass-1 class symbol table (D1) is built after `CallCollectUnits`, before per-unit binding, so a driver INVOKE sees later classes; methodDefinition contexts (which own their own data/procedure divisions) take a synthetic reshaping (direct-children-only accessor discipline); LINKAGE-formal resolution before field emission is where method LINKAGE→parameters slots in; the EC group gate spans all units and INVOKE sites emit EC propagation pickups (per Spec corrections #3 they surface EC-OO conditions, participating in the same propagation as CALL sites).

### Registry/GOBACK seam — OO bypasses the registry
`ProgramRegistry` (`src/Cobol.Net.Runtime/Control/ProgramRegistry.cs`) owns §8.4.6.3 program-name resolution, §14.6.2.3 instance-state, CANCEL, ExternalStore, and cross-assembly discovery via `__CobolModule.Register()` (`CodeGen/Verbs/CallEmitter.cs`) — all on the opaque `ICobolProgram.Call(CobolArg[], ManagedPointer?)` ABI, which D6 explicitly REJECTS for methods. So OO INVOKE bypasses the registry entirely (direct typed C# calls); classes need only type emission alongside program classes — registry involvement returns only if dynamic-name class lookup is ever wanted. Signals: STOP RUN → `BoundStop` → `throw new StopRun()` unwinds to Main; GOBACK → `BoundGoback` → `CallEmitter.EmitGoback` (`CodeGen/Verbs/CallEmitter.cs`: RETURNING move, RAISING staging, `throw new ProgramReturn()`) caught at the program's own `__Activate` (`CodeGen/DispatchEmitter.cs`); EXIT PROGRAM is `__asCalled`-gated. The method context adds the third signal: method GOBACK → `BoundMethodReturn` → `throw new MethodReturn()` caught at the method entry (D8), with EXIT METHOD edition-gated per Spec corrections #2.

## ISO citations

- ISO/IEC 1989:2023 §11.3 CLASS-ID paragraph (§11.3.2 general format: `INHERITS FROM {object-class-name-2}…` — multiple inheritance; AS literal; IS FINAL; USING parameterized class — D12)
- §9.3.12 / §9.3.13 parameterized classes / interfaces; §12.3.8.2 EXPANDS; §12.3.8.3 SR3/SR4/SR7; §12.3.8.4 GR1/GR5/GR7/GR8 — D12 (`Oo/OoExpansion.cs`)
- §11.4 FACTORY paragraph + §8.6.4 (one copy of each static/factory item per class) — FACTORY → `virtual`/`override` members of the per-class factory singleton (NOT C# statics: §9.3.6 runtime-class dispatch + SELF-in-factory polymorphism, §8.4.3.8.4 GR2)
- §11.7 METHOD-ID paragraph (SR3 OVERRIDE needs matching non-FINAL base signature; SR4a redefining base signature w/o OVERRIDE is an error → no C# hiding; GR5 method-local shadows object data; SR6/7 GET/SET PROPERTY shape; SR2/8 OVERRIDE/FINAL not in prototypes)
- §11.8 OBJECT paragraph (instance object definition; IMPLEMENTS interface-name list)
- §8.4.3.8 SELF and SUPER (GR2 SELF resolves on the runtime class → virtual; GR3 SUPER restricted search → base.; SR4/5 object-class-name OF SUPER when 2+ bases)
- §9.3.6 Method invocation (object→instance vs factory resolution; SUPER restricted search; match-rule 3c BY REFERENCE same class/category → typed `ref` is conformant)
- §14.9.23 INVOKE statement (general format USING BY REFERENCE/CONTENT/VALUE/OMITTED + RETURNING; SR4 method-name lookup by receiver kind; SR6 universal ref forbids BY CONTENT/VALUE; SR7 identifier-2 method-name only for universal; GR5 null→EC-OO-NULL; GR6 default arg-passing; GR7b not-found→EC-OO-METHOD; GR8 RETURNING)
- §9.3.5.3 (method resolution signature + parametric polymorphism is OPTIONAL)
- §16.1 / §16.2 the standard class BASE — New (§16.2.1, BaseFactoryInterface) and FactoryObject (§16.2.2, BaseInterface), present only in a class that inherits BASE (D4)
- §13.18.60.4 USAGE OBJECT REFERENCE (typed vs universal object references)
- §14.9.23.4 GR2b non-COBOL INVOKE is implementor-defined (basis for deferring .NET-object interop INVOKE)

### Verified 2023 line anchors (specs/ISO_COBOL.md)

- Structure §10.6: class-definition :12739-12750 (CLASS-ID modifiers :12742-12744 — AS/FINAL/INHERITS/USING ONLY); factory-definition :12752-12760; instance-definition :12762-12770 (both take IMPLEMENTS, :12755/:12765); interface-definition :12783-12796 (NO data division; methods are prototypes, :12823); method-definition :12798-12821 (attrs = [OVERRIDE] [IS FINAL] only; GET/SET PROPERTY selector :12810-12814); end markers §10.7 :12883.
- §11 paragraph map: §11.3 CLASS-ID :12999; §11.4 FACTORY :13069; §11.6 INTERFACE-ID :13157; §11.7 METHOD-ID :13210; §11.8 OBJECT :13295. Every method is implicitly RECURSIVE :12032.
- Spec corrections: method WS ban §13.5.3 SR 1 :16461 (+ INVOKE SR 10 :28443); EXIT formats §14.9.14.2 :27346-27381 + Annex E.2 removals :49034/:49036; INVOKE format §14.9.23.2 :28376-28390 (no exception phrase), EC-OO-NULL :28506, EC-OO-METHOD :28528; ABSTRACT — zero matches in the whole spec.
- PROPERTY clause §13.18.42 :21136-21170 (WS of factory/instance only, SR 1 :21153; elementary, no OCCURS, no ACTIVE-CLASS refs :21165; generated GET/SET :21170; property temps via implicit INVOKE :7393-7395).
- Universal object reference: §13.18.60 USAGE :22636, GR 22b :22912, glossary :2352; INVOKE via universal forbids BY CONTENT/VALUE (SR 6 :28435); identifier-2 method-name ONLY with universal (SR 7 :28437); runtime (not compile-time) conformance :12291, EC-OO-UNIVERSAL :24756; EXCEPTION-OBJECT is implicitly universal :7249; NULL/SELF/SUPER are NOT :7283/:7331.
- EC-OO family: Table 13 under §14.6.13.1.6 :24748-24756 — EC-OO-ARG-OMITTED / -CONFORMANCE / -EXCEPTION / -IMP / -METHOD / -NULL / -RESOURCE / -UNIVERSAL (all Fatal except EC-OO-IMP); object-view raise sites :7211-7229.
- RAISE §14.9.29 :29727-29761 (RAISE identifier-1 = exception object; NULL/SUPER forbidden, SELF legal; unhandled-at-exit → EC-OO-EXCEPTION per §14.6.13.1.5 :24575,:24602-24608) — the landed EC engine gets an object channel, not a new mechanism.
- Conformance is TWO distinct rule sets: §14.8 (:25245, params :25270, returning :25419 — argument↔formal at INVOKE sites, cited by INVOKE SR 5c :28433; ANY LENGTH pairing :25375/:25377/:25414/:25503-25505) vs §9.3.6 (:12105) + §9.3.8.2 (:12294) / §9.3.8.2.3 interfaces (:12309) — method-SIGNATURE conformance for override/implements (:12177,:12247); inline-invocation RETURNING may not be ANY LENGTH/ACTIVE-CLASS :7151; ANY LENGTH clause §13.18.2 :17576 (SR 2 :17593).
- §A.4.10 :40408-40414 — EXACTLY three optional OO items (CLASS-ID INHERITS repetition, INTERFACE-ID INHERITS repetition, parametric polymorphism §9.3.5.3 :12102); ALL other OO surface (FACTORY/OBJECT, interfaces, PROPERTY, universal refs, object views, SELF/SUPER/NULL/EXCEPTION-OBJECT, INVOKE + inline invocation, ACTIVE-CLASS, single-INHERITS, RAISE with objects, EC-OO) is MANDATORY for a conforming 2023 implementation. Single INHERITS is mandatory; only the repetition is optional.

## Open questions (multiple inheritance settled in `COBOLNET_DESIGN.md` §18 item 18; the rest are live here)

- OWNER-LEVEL: COBOL allows MULTIPLE class inheritance (§11.3.2 `INHERITS FROM {object-class-name-2}…`); C# allows only one base class. v1 restricts to single inheritance (sufficient for the entire current corpus). When a multi-base program appears, choose: (a) linearize to one C# base + extract secondary supers as C# interfaces the class IMPLEMENTS (with member forwarding), or (b) declare multiple inheritance unsupported. Needs an owner decision before any multi-base program is targeted.
- Parametric polymorphism (overloading by method-resolution-signature, §9.3.5.3) is OPTIONAL and currently deferred (no corpus use). If targeted, decide between C# name-mangling by signature vs leaning on the processor's object-management-system methodology the spec permits — an owner/architecture call about generated-name idiomaticity.
- Grammar extension scope/ordering — MOSTLY RESOLVED: FACTORY, OVERRIDE/[IS] FINAL, INTERFACE-ID/IMPLEMENTS, and PROPERTY (clause + GET/SET selector) are ALL implemented behind `{is2002()}?` gates with paired pre-2002 diagnostics and matrix rows. Still missing: method-name `AS literal` and qualified `object-class-name OF SUPER` (per §A.4.10 :40408-40414 both MANDATORY for a conforming 2023 implementation — sequencing, never optional scope).
- INTERFACE-ID + IMPLEMENTS → C# interfaces — RESOLVED: an explicit BINDER conformance pass is REQUIRED, not optional; Roslyn satisfaction is provably insufficient in both directions (lossy projections under-reject: 9(4)/9(8) both `ref long`; covariant returns over-reject: rules 5a/5c2 permit what C# interface implementations forbid — cured by explicit-implementation adapters). See the INTERFACE/PROPERTY implementation note.
- EC-OO — RESOLVED: the exception-OBJECT channel rides the EC engine through ONE signal architecture (BoundRaising carries ObjectSource; ExceptionState carries the object register + a propagation slot mutually exclusive with the named slot; the SAME propagation-pickup emission consumed at CALL AND every INVOKE site incl. universal dispatch). RAISE identifier-1 (never fatal by itself, GR2 — continue-after-RAISE golden-proven), EXCEPTION-OBJECT (CobolObject? per D-U1; the "EXCEPTION-OBJECT" LastName SENTINEL makes §15.33.3 r1's EXCEPTION-STATUS work with ZERO function changes), USE Format 4 (__EcObjDispatch — the operand is the discriminated `BoundEoOperand`, `BoundEoClass(OoClassSymbol)` | `BoundEoInterface(OoInterfaceSymbol)`, both resolved through `OoNameResolution`'s §8.4.6.4 scope; GR14 is TWO PASSES — every object-class-name-1 entry in source order, then, only if none qualified, every interface-name-1 entry, kb/Work PB365 — and each pass tests its alternative's OWN census of emitted C# types: GR14a selects a FACTORY OR INSTANCE object of the class or a subclass and the two are DISJOINT emitted hierarchies, so a class entry tests `OoClassSymbol.FactoryOrInstanceCsTypes` as one C# or-pattern, `__obj is FOO or FOO__FACTORY` — kb/Work PB366 — while GR14b's IMPLEMENTS closure (§11.8.4 GR2 / §11.4.4 GR2) rides `is` over the single emitted interface both halves carry, `OoInterfaceSymbol.ImplementedCsTypes`; GR3: F4 replaces the F1/F3 tiers for object raises), method/program GOBACK//EXIT RAISING identifier (SR4a discharged at COMPILE time — with SR4d no-universal and factory objects unable to enter typed references, the activated-side rule-1 check is STATICALLY true in v1; the D-EO5 note), the rule-4 EC-OO-EXCEPTION conversion (fatal; enters the F3 tiers — surviving needs RESUME, the standard fatal-EC protocol), SET … TO EXCEPTION-OBJECT (universal target copies; typed target runtime-narrows → EC-OO-UNIVERSAL). The EXCEPTION-OBJECT word is §8.9-reserved 2002+; references in object-operand positions (objectReference / the SET re-route shape) are EXEMPT from the 0901 funnel — the reservation is what makes the reference unambiguous. Method declaratives are implemented (kb/Work PB1010 — a method selects over its OWN USE statements through method-local selection machinery; COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN, METHOD-SCOPED SELECTION). Residue (0899-named): PROPAGATE ON; object VIEWS (§8.4.3.5, EC-OO-CONFORMANCE) — MANDATORY per §A.4.10 (not one of the three optional OO items), sequenced as the narrowing surface.
