# WiseOwl COBOL — OO slice implementation briefs

> **Status: LEDGER / implementation briefs** for the OO slices — all now implemented and folded into the
> AUTHORITATIVE design `docs/COBOLNET_OO_DESIGN.md`; each brief's decisions LIVE IN the deep-dive, and
> these working copies are kept in-repo as the derivation record (the feedback_create_documents discipline;
> they survive sessions — the /e/tmp brief-loss lesson). Verify spec line anchors on use.


---

# Brief: FACTORY (§11.4)

# FACTORY slice — decision-complete implementation brief (OO port, roadmap Phase 3)

> Scope: ISO 11.4 FACTORY paragraph (specs/ISO_COBOL.md:13069), factory data + factory methods, `INVOKE class-name "M"` (non-NEW), SELF/SUPER inside factory methods (§14.9.23.3 SR4f/g/h/i, :28407+), and the NEW interplay (§16.2.1, :39170). Deep-dive: docs/COBOLNET_OO_DESIGN.md ("Never landed" list — FACTORY is net-new, no legacy port). Everything below is spec-derived and cites §/line anchors; tests VERIFY, never scope.

## ⛔ D11 (the one load-bearing decision) — a factory is a REAL sibling C# class + one singleton instance, NOT static members. This SUPERSEDES the deep-dive's "FACTORY data → static fields" sketch (Summary :37, D7 :115, edge case :232); update the deep-dive in the SAME change set with this correction (process rule feedback_follow_the_deep_dive).

**Chosen design.** For EVERY `CLASS-ID. FOO` emit a second class

```csharp
public class FOO__FACTORY : ANIMAL__FACTORY /* or CobolObject when no INHERITS */ {
    public static /*new when base is a factory class*/ readonly FOO__FACTORY __Instance = new();
    public virtual FOO __New() => new FOO();          // BaseFactoryInterface.New (§16.2.1; ACTIVE-CLASS creation)
    // factory WS → INSTANCE fields of this class (VALUE → field initializers)
    // each factory METHOD-ID → public virtual/override method (same OoEmitMethod machinery)
}
```

`INVOKE FOO "M" …` → `FOO__FACTORY.__Instance.M(…)` (virtual, no null guard — the singleton is never null).

**Why statics are a silent miscompile (three independent spec facts):**
1. **Per-class copies of INHERITED factory data** — §8.6.4 (specs/ISO_COBOL.md:8765-8769): "In the factory object of a given class, there is one copy of each static item that is *described in or inherited by* the factory definition of that class." DOG (INHERITS ANIMAL, no factory paragraph of its own) has its OWN factory object with its OWN copy of ANIMAL's factory data. C# statics on the class would share ONE copy between ANIMAL and DOG — the factory-level analog of adversarial trap #1 (two-object independence). Instance fields of two singleton instances give two copies for free.
2. **SELF in a factory method is polymorphic** — §14.9.23.3 SR4f (:28407-ish, "SELF … factory method → factory interface of the class containing the INVOKE") combined with §8.4.3.8 GR2 (:7331+, SELF resolution is on the RUNTIME class). An ANIMAL factory method doing `INVOKE SELF "F"` must dispatch to DOG's factory override when reached via `INVOKE DOG "M"`. Statics cannot dispatch virtually; a real factory class hierarchy makes it C# `this.F()` — correct by construction.
3. **Factory method resolution walks the inheritance chain** — §9.3.6 (:12105+): "If an invocation specifies the object using a class name, the factory object of the specified class is used … resolve to a factory method", resolution rules 1-2 walk INHERITS upward. The factory C# hierarchy mirrors the class hierarchy, so lookup + override are Roslyn-checked renders of binder facts (D1 discipline).

**Rejected alternatives.** (a) `static` members on FOO (the deep-dive's original sketch) — fails all three facts above. (b) Nested factory type inside FOO — legal C# (nested can inherit a nested), but breaks the per-unit emitter-state switch uniformity (OoEmitClassUnit emits one top-level type per pass); sibling type keeps the switch identical. (c) `Lazy<T>`/explicit lifecycle — unneeded: §9.3.14.2 (:12499-12504) requires only "created before it is first referenced"; .NET static-readonly type initialization (beforefieldinit) satisfies it exactly. (d) Emitting the factory class only when a FACTORY paragraph exists — rejected: DOG-without-factory still needs its own factory object (fact 1) and a chain node for inherited factory methods (fact 3). **Every CLASS-ID emits a factory class, always** (a class with no factory anywhere in its chain gets an empty `FOO__FACTORY : CobolObject` + `__Instance` + `__New` — uniform, cheap, and the future FactoryObject/§16.2.2 hook).

**Naming safety.** `__FACTORY` / `__Instance` / `__New` contain `__`, which no COBOL-derived CsName can contain (§8.3.2.1: a user-defined word never begins with a hyphen or underscore, so `DataItem.Sanitize` never yields a leading `__`; word-derived `__` names come only from `NamingConvention`'s tagged families). No `usedCsNames` collision is possible; still add `csName + "__FACTORY"` to the `usedCsNames` set in OoClassTable.Build (OoClassTable.cs:131) for belt-and-braces. The derived singleton needs the C# `new` modifier (`public new static readonly DOG__FACTORY __Instance = new();`) to suppress CS0108; `__New` overrides use C#-9 covariant returns (`public override DOG __New() => new DOG();`).

## D12 — NEW interplay (§16.2.1 :39170-39189; D4 unchanged)

- `INVOKE FOO "NEW" RETURNING o` stays EXACTLY as landed: `o = new FOO()` (the generated ctor IS the predefined New; D4). No change to OoBindClassInvoke's NEW branch (StatementBinder.Oo.cs:241-281).
- **`INVOKE SELF "NEW" RETURNING r` inside a FACTORY method** (the canonical factory-MAKE pattern): §16.2.1.2 GR1 + the BaseFactoryInterface prototype (:39148-39158 — `returning outObject usage object reference active-class`) mean New creates an instance of the RUNTIME factory's class. Realization: the generated `__New()` virtual (covariantly overridden per class) → bind to a new `InvokeForm.NewSelf`; emit `r = this.__New()`. An inherited MAKE reached via `INVOKE DOG "MAKE"` thereby creates a DOG. RETURNING is required (reuse the 0826 wording); USING on it is 0826 (NEW takes no arguments). Receiver conformance = the existing NEW check with cls = the CONTAINING class (containing.ConformsTo(declared) — a runtime subclass instance still conforms downstream).
- **`INVOKE SUPER "NEW"`** in a factory method binds to the SAME `this.__New()`: SUPER restricts the METHOD SEARCH (§8.4.3.8 GR3) to the inherited factory interface, but the found method IS the predefined New, whose behavior is active-class creation on the object SUPER references (= the same runtime factory object, GR1 :7331). Document this equivalence in a code comment.
- **A user factory METHOD-ID named NEW → COBOLNET0836** (new code; 0830-0835 are taken by INITIALIZE, 0836+ verified free). ISO permits overriding New, but v1 keeps NEW = the generated ctor (D4); reject loudly, cite §16.2.1 + the v1 restriction. (`INVOKE obj "NEW"` through an instance receiver already 0825s naturally — NEW is not in the instance interface.)
- SELF "NEW" in an INSTANCE method: falls out — the instance roster has no NEW → existing 0825.

## D13 — factory data binds in its OWN DataBinder forest (name separation is structural)

The factory definition and instance definition are separate source elements (§10.6 structure :12752-12770; the class-definition format at :12735-12760 shows factory-definition and instance-definition as sibling units). Factory data names are therefore invisible to instance methods and vice versa — realize it the way slice 2 realized method scoping: a SECOND DataBinder per class over a factory synthetic unit (the OoReparentClassData pattern, CSharpEmitter.Oo.cs:83-91). An instance method referencing a factory data name gets the normal unknown-name diagnostic (+ extend OoScopeHint's wording to mention factory/instance separation). SR 10 (INVOKE argument ban on factory/instance WS, :28443) works FREE: the factory binder sets `OoIsClassUnit = true`, its WS roots are not method-scoped → `OoIsObjectData` returns true → the landed 0828/auto-CONTENT logic applies unchanged (DataBinder.Oo.cs:49-57, StatementBinder.Oo.cs:412-431).

## Grammar (src/Cobol.Net.Frontend/Grammar — regen inputs already cover Core/*.g4)

1. **Core/CobolLexer.g4** (~line 133, next to `OBJECT`): add `FACTORY : 'FACTORY' ;`. Mirror in `_dataNameTokens` (:30-52) — FACTORY may be a user data name at COBOL-85.
2. **CobolParserCore.g4 cobolWord** (:25+): add `| FACTORY  // context: FACTORY paragraph (§11.4, 2002+); '85 user word — §8.9 funnel rejects ≥2002 (ReservedWords.Table.cs:184, already present: added-2002, high confidence)`.
3. **Validation/EditionValidator.cs CheckedTokenTypes** (:268-284): add `CobolLexer.FACTORY` — position-blind is SAFE: its keyword occurrences parse only through factoryParagraph / END FACTORY / the FACTORY OF usage alternative, never a cobolWord name slot (the same argument as the EC band). Net edition behavior: FACTORY as a data name is legal at `--std 85`, 0901 at 2002+ — the continuity invariant, no new table row needed.
4. **Core/CobolOO.g4**:
```antlr
classDefinition
    : (IDENTIFICATION DIVISION DOT)? classIdParagraph
      environmentDivision?
      factoryParagraph?          // NEW — ISO §11.4 (:13069); order per the §10.6 format (:12745-12748)
      objectParagraph?
      endClassHeader
    ;

// FACTORY definition — factory data (DATA DIVISION) + factory methods. IMPLEMENTS is the INTERFACE-ID
// slice (deliberately unparsed, matching objectParagraph). END FACTORY carries NO name (§10.6 :12760).
factoryParagraph
    : (IDENTIFICATION DIVISION DOT)? FACTORY DOT
      environmentDivision?
      dataDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END FACTORY DOT
    ;
```
   Mirrors objectParagraph (:34-40) exactly — same methodDefinition rule, so OoBindMethodData/OoEmitMethod reuse is verbatim. No `{is2002()}?` gate needed: factoryParagraph is reachable only inside classDefinition, which is already gated at compilationGroup (CobolParserCore.g4:123).
5. **Core/CobolOO.g4 objectReferenceUsage** (:95-98): add a FIRST alternative `OBJECT REFERENCE FACTORY OF className` (keep the two-explicit-alternatives discipline — never an optional tail, the DEVLOG-438/439 lesson). BINDING stages it loud: in DataBinder.cs:609 (the objectReferenceUsage consumer), when the new alternative is present → COBOLNET0899 "factory object references (USAGE OBJECT REFERENCE FACTORY OF, §13.18.60 :22681; method resolution via the factory interface, §14.9.23.3 SR4 a)) are recognized but not yet implemented (the universal-reference wave, with §16.2.2 FactoryObject)". Rationale: graceful staged diagnostic beats a bare parse error; the dead sketch CobolParserOO.g4 is reference-only (do NOT revive its invokeOnException/ABSTRACT/generics content — spec-wrong per Spec corrections #3/#4).
6. Dead-sketch note: CobolParserOO.g4:57-63 imagines `CLASS SECTION`/`OBJECT SECTION` data divisions — NOT ISO; ignore entirely. The ISO shape is the factory-definition block (:12752-12760), which factoryParagraph above matches.
Process: grammar change → regen both OSes, guard-fast after the grammar commit (feedback_grammar_version_factoring; OO/NIST-expansion grammar work is pre-authorized, log it).

## Pass-1 symbol table (src/Cobol.Net.Compiler/Binding/OoClassTable.cs)

- **OoClassSymbol** gains: `public string FactoryCsName => CsName + "__FACTORY";`, a SEPARATE factory roster (`_factoryMethods` dict + `FactoryMethods` list + `TryAddFactoryMethod` — clone of :240-251), and `FindFactoryMethod(string)` walking the base chain (clone of FindMethod :255-262). The rosters NEVER merge: an instance method and a factory method may share a name (§9.3.6 — two interfaces); day-one test.
- **OoMethodSymbol** gains `public bool IsFactory { get; init; }` (diagnostic wording + the SELF/SUPER roster choice).
- **Build()** (:128-226): after the instance-method loop (:158-178), an identical loop over `ctx.factoryParagraph()?.methodDefinition() ?? []` → TryAddFactoryMethod (dup → 0822 with "factory method" wording), END METHOD mismatch → 0820, name NEW → **COBOLNET0836**. END FACTORY has no name — no mismatch check exists. Reserve `FactoryCsName` in usedCsNames (:131,:142).
- **OverrideOf marking** (:218-224): second loop marking factory overrides against `sym.Base.FindFactoryMethod` (factory overrides factory ONLY — §9.3.8.2 signature conformance per interface, never cross-roster).
- **ValidateOverrideSignatures** (:34-59): iterate `cls.Methods.Concat(cls.FactoryMethods)` — 0829 unchanged.

## Bind seams

**CSharpEmitter.Oo.cs** — OoClassUnit (:26-34) gains `FactoryData`, `FactoryRefs`, `FactoryBound`. 
- `OoBindClassData` (:48-64): after the instance half, build the factory half — new `OoReparentFactoryData(ctx)` (factory env → class env → factory dataDivision; same direct-children discipline as :83-91), a second `new DataBinder(edition) { OoClasses = _ooClasses, OoIsClassUnit = true }` with its OWN uid band (`CallSeedUids(_callUidBand); _callUidBand += 100_000;`), `BindDeclarations` → `OoBindMethodData(m)` for each FACTORY method → `BindResolve`; factory FILE SECTION → 0899 (clone :58-61). Factory env division content beyond what reparent binds: same behavior as the object paragraph today (bound via reparent). Runs for EVERY class before ANY body binds (the existing loop in CallEmitRunUnit:89 covers it — signatures-before-bodies, D1).
- `OoBindClassBody` (:68-77): bind TWO rosters. Refactor `StatementBinder.BindClassBody(cls)` → `BindMethodRoster(OoClassSymbol cls, IReadOnlyList<OoMethodSymbol> roster)`; the factory binder is a SEPARATE StatementBinder over (FactoryData, FactoryRefs) with `OoCurrentClass = cls.Symbol` and new flag `OoInFactory = true`, producing `FactoryBound` (its own pc space + `__N`). ConfigureEc identically (:75).

**StatementBinder.Oo.cs**
- `InvokeForm` (:10-25) gains `Factory` (class-name receiver, non-NEW → `CLS__FACTORY.__Instance.M(…)`) and `NewSelf` (SELF/SUPER "NEW" in a factory method → `this.__New()`).
- New `public bool OoInFactory { get; set; }`.
- `OoBindClassInvoke` (:241-281): replace the non-NEW BoundUnsupported (:243-245) with: `cls.FindFactoryMethod(method)` → found ⇒ `OoBindResolvedInvoke(inv, fm, InvokeForm.Factory, receiver: null)` and set `BoundInvoke.ClassCsName = cls.CsName`; not found ⇒ 0825 worded per SR3 (:28399 — "literal-1 shall be the name of a method defined in the factory interface of object-class-name-1"; compile-time analog of EC-OO-METHOD GR7b :28528).
- SELF/SUPER branch (:184-217): roster selection by context — when `OoInFactory`, search `cur.FindFactoryMethod` (SELF, SR4f) / `cur.Base.FindFactoryMethod` (SUPER, SR4h); when not, the existing instance rosters (SR4g/i). BEFORE the roster search, special-case method name "NEW" when `OoInFactory`: bind `InvokeForm.NewSelf` (RETURNING required + containing-class conformance — reuse the 0826 checks from the NEW branch). Trap #7 (SUPER in a root class → 0827) applies to the factory flavor too (:203-207 — same code path).
- `OoBindInstanceInvoke` (:286-315): when the instance-roster lookup fails but `cls.FindFactoryMethod(method)` succeeds, extend the 0825 message with the hint "…'M' is a FACTORY method of class 'X' — invoke it through the class-name (ISO §14.9.23.3 SR4b: an instance receiver resolves the instance interface)".
- `OoBindResolvedInvoke`/`OoBindInvokeArg`/GOBACK/EXIT METHOD (:319-528): UNCHANGED — marshaling, conformance, and D8 method-return are roster-agnostic (factory methods are methods; MethodReturn/the catch-at-entry pattern applies verbatim).

## Emit seams

**CSharpEmitter.Oo.cs**
- Factor the body of `OoEmitClassUnit` (:102-128) into `OoEmitTypeHalf(csName, baseCsName, DataBinder, ReferenceResolver, BoundProgram, roster, w, extraHeaderLines)` — the emit-into-a-type parameterization, called twice per class: once for the instance class (unchanged output), once for the factory class with: base = `Symbol.Base?.FactoryCsName ?? "CobolObject"`, header extras = the `__Instance` singleton (`new` modifier when Base != null) and `__New` (`public virtual {CsName} __New() => new {CsName}();` at a root, `public override {CsName} __New() => new {CsName}();` covariant otherwise). Factory WS roots emit as INSTANCE fields of the factory class via the same FieldEmitter (VALUE → initializers = §14.6.2.4 initial state :24121-24125, initialized at singleton construction, satisfying §9.3.14.2 :12502); factory METHOD WS rides the existing static-field mechanism (`OoStaticRootFields`, FieldEmitter.cs:43) on the FACTORY class — the §13.5.3 SR 1 2023 window (VCR Table 6 row 130e) applies to factory methods identically, no new row.
- `OoEmitInvoke` (:210-229): `case InvokeForm.Factory:` → route into `OoEmitInstanceInvoke` with target expression `$"{inv.ClassCsName}__FACTORY.__Instance"` (extend the target switch :301-306; NO RequireNonNull — never null). `case InvokeForm.NewSelf:` → `w.Line(inv.Returning!.Write("this.__New()") + " // INVOKE SELF|SUPER \"NEW\" in a factory method (§16.2.1 ACTIVE-CLASS creation via the covariant __New)")`.
- `OoEmitMethod` (:140-196): reused verbatim for factory methods (roster-driven; LINKAGE/LOCAL-STORAGE/image crossings/copy-out all identical).

**CSharpEmitter.Call.cs `CallEmitRunUnit`** (:79-144): `MarkStoreAsImage(cls.FactoryData)` beside :93; `_ecActive` (:99-101) must also test `classes.Any(c => c.FactoryBound?.Ec is { Any: true })`. Class emission order unchanged (source order, factory class emitted immediately after its instance class). No ProgramRegistry involvement (typed calls — registry bypass unchanged).

**Runtime**: NO changes. CobolObject (Control/CobolObject.cs) and MethodReturn (Control/MethodReturn.cs) already cover factory classes (they derive CobolObject → future universal __CobolInvoke free).

## Diagnostics (complete list)
- **COBOLNET0836 (NEW)**: "class 'X': a factory method may not be named 'NEW' — the predefined New (ISO §16.2.1) is realized by the generated constructor (deep-dive D4); overriding New is a deferred v1 restriction". Raised in OoClassTable.Build.
- 0822 (existing): duplicate factory method name within the factory roster.
- 0825 (existing, new wordings): factory-interface lookup failure (SR3 :28399) at INVOKE class-name; the "it's a factory method" hint on instance-receiver misses.
- 0826 (existing, reused): SELF "NEW" without RETURNING / with USING; NewSelf receiver conformance.
- 0827 (existing): SUPER in the factory of a root class (trap #7, factory flavor).
- 0828 (existing, free): BY REFERENCE of factory WS (SR 10 :28443) via OoIsObjectData; all USING/RETURNING conformance.
- 0829 (existing): factory override signature mismatch (§9.3.8.2).
- 0899 staged: `OBJECT REFERENCE FACTORY OF` (universal-reference wave); FILE/REPORT/SCREEN SECTION in the factory paragraph; FACTORY IMPLEMENTS deliberately unparsed (INTERFACE-ID slice — grammar comment, not a diagnostic); DECLARATIVES in factory methods (existing path).
- 0901 funnel: FACTORY as a user word at 2002+ (table row exists, ReservedWords.Table.cs:184); legal at 85.
- Edition negative: a compilation group containing FACTORY at `--std 85` → the existing classDefinition-requires-2002 diagnostic (compilationGroup gate) — matrix case, not new code.

## Test matrix rows
| # | Case | Kind | Expected |
|---|------|------|----------|
| 1 | Factory data independence: DOG (no factory para) vs ANIMAL each bump an inherited factory counter | conformance (oo_factory_inherit) | two copies (§8.6.4 :8765) — the anti-static trap, new adversarial trap #11 |
| 2 | Same name in factory AND instance roster; INVOKE class → factory, INVOKE obj → instance | unit (OoFactoryTests) | dual dispatch, no collision (§9.3.6) |
| 3 | INVOKE class "M", M nowhere in the factory chain | unit | 0825 (SR3) |
| 4 | Factory METHOD-ID. NEW | unit | 0836 |
| 5 | SUPER in a root-class factory method | unit | 0827 (trap #7) |
| 6 | BY REFERENCE arg reading factory WS | unit | 0828 (SR 10); bare arg auto-CONTENT (GR6a2) |
| 7 | SELF "F" in an inherited factory method, derived factory overrides F | conformance (oo_factory_self variant) | derived override wins (SR4f + §8.4.3.8 GR2) |
| 8 | SELF "NEW" in an inherited factory method via INVOKE DOG "MAKE" | conformance (oo_factory_self) | creates a DOG (covariant __New) |
| 9 | GOBACK inside a factory method under nested PERFORM frames | unit | method-return only (D8/MethodReturn reuse) |
| 10 | Method WS inside a FACTORY method at --std 2023 / 2002 | matrix | 0902 / OK (row 130e unchanged, covers factory methods) |
| 11 | FACTORY as a data-name at 85 / 2002 | matrix | OK / 0901 (continuity invariant) |
| 12 | Whole class group at --std 85 | matrix | classDefinition edition diagnostic (existing gate) |

## 3 conformance programs (tests/conformance/2002/, + .out, + manifest.json "enabled" — same commit, feedback_goldens_ship_with_the_feature)

**oo_factory_basic.cob** — factory data persistence + static-call binding. Expected out: `CNT=0002`
```cobol
      *> ISO 1989:2023 §11.4 FACTORY — factory data is one copy per class (§8.6.4); INVOKE class-name "M"
      *> resolves the factory interface (§9.3.6 / §14.9.23.3 SR3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOFACB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS COUNTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE COUNTER "BUMP".
           INVOKE COUNTER "BUMP".
           INVOKE COUNTER "GETCNT" RETURNING R.
           DISPLAY "CNT=" R.
           STOP RUN.
       END PROGRAM OOFACB.
       IDENTIFICATION DIVISION.
       CLASS-ID. COUNTER.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO CNT.
       END METHOD BUMP.
       METHOD-ID. GETCNT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-RES PIC 9(4).
       PROCEDURE DIVISION RETURNING LK-RES.
       MAIN.
           MOVE CNT TO LK-RES.
       END METHOD GETCNT.
       END FACTORY.
       END CLASS COUNTER.
```

**oo_factory_inherit.cob** — per-class copies of inherited factory data + chain resolution. Expected out: `DOG=0001` / `ANIMAL=0002` (two lines)
```cobol
      *> §8.6.4 (:8765): DOG's factory object holds its OWN copy of ANIMAL's factory data — the anti-static
      *> adversarial test (trap #11). DOG has NO factory paragraph; BUMP/GETCNT resolve up the chain (§9.3.6).
       ... driver OOFACI: REPOSITORY CLASS ANIMAL. CLASS DOG.
       MAIN.
           INVOKE DOG "BUMP".
           INVOKE ANIMAL "BUMP".
           INVOKE ANIMAL "BUMP".
           INVOKE DOG "GETCNT" RETURNING R.    DISPLAY "DOG=" R.
           INVOKE ANIMAL "GETCNT" RETURNING R. DISPLAY "ANIMAL=" R.
           STOP RUN.
       *> CLASS-ID. ANIMAL. — FACTORY. with 01 CNT PIC 9(4) VALUE 0. + methods BUMP / GETCNT (as in oo_factory_basic)
       *> plus an OBJECT. paragraph with METHOD-ID. SOUND. DISPLAY "GENERIC". (reused by test 3)
       *> CLASS-ID. DOG INHERITS FROM ANIMAL. — OBJECT. only (SOUND override DISPLAY "WOOF"); NO factory paragraph.
```

**oo_factory_self.cob** — SR4f SELF-in-factory + SELF "NEW" active-class creation. Expected out: `WOOF` / `GENERIC`
```cobol
      *> §14.9.23.3 SR4f: SELF in a factory method resolves the factory interface of the RUNTIME factory;
      *> §16.2.1: New creates an instance of the runtime factory's class — INVOKE DOG "MAKE" (MAKE inherited
      *> from ANIMAL's factory) creates a DOG, proven by the overridden SOUND.
       ... driver OOFACS: 01 A USAGE OBJECT REFERENCE ANIMAL.
       MAIN.
           INVOKE DOG "MAKE" RETURNING A.
           INVOKE A "SOUND".
           INVOKE ANIMAL "MAKE" RETURNING A.
           INVOKE A "SOUND".
           STOP RUN.
       *> CLASS-ID. ANIMAL. FACTORY. METHOD-ID. MAKE.
       *>   DATA DIVISION. LINKAGE SECTION. 01 LK-OBJ USAGE OBJECT REFERENCE ANIMAL.
       *>   PROCEDURE DIVISION RETURNING LK-OBJ.
       *>   MAIN. INVOKE SELF "NEW" RETURNING LK-OBJ.
       *>   END METHOD MAKE. END FACTORY.
       *>   OBJECT. METHOD-ID. SOUND. DISPLAY "GENERIC". END OBJECT.
       *> CLASS-ID. DOG INHERITS FROM ANIMAL. OBJECT. METHOD-ID. SOUND. DISPLAY "WOOF". (no factory paragraph)
```

## File/seam checklist (every touch)
1. src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4 — FACTORY token (~:133) + _dataNameTokens (~:30).
2. src/Cobol.Net.Frontend/Grammar/Core/CobolOO.g4 — classDefinition (:18-23) + new factoryParagraph + objectReferenceUsage FACTORY OF alternative (:95-98).
3. src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4 — cobolWord += FACTORY (:25+).
4. src/Cobol.Net.Compiler/Validation/EditionValidator.cs — CheckedTokenTypes += FACTORY (:268-284). (ReservedWords.Table.cs:184 row already exists — no change.)
5. src/Cobol.Net.Compiler/Binding/OoClassTable.cs — FactoryCsName / factory roster + FindFactoryMethod / Build() factory loop + 0836 + factory OverrideOf / ValidateOverrideSignatures over both rosters / usedCsNames reservation.
6. src/Cobol.Net.Compiler/Binding/DataBinder.cs:609 — FACTORY OF → 0899 stage.
7. src/Cobol.Net.Compiler/Binding/DataBinder.Oo.cs — NO change (OoBindMethodData reused on the factory binder instance).
8. src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.Oo.cs — InvokeForm.Factory/NewSelf, OoInFactory, OoBindClassInvoke non-NEW branch, SELF/SUPER roster switch + NEW special-case, instance-miss hint, BindClassBody → BindMethodRoster refactor.
9. src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Oo.cs — OoClassUnit fields; OoBindClassData factory half + OoReparentFactoryData; OoBindClassBody factory half; OoEmitTypeHalf refactor + factory class/__Instance/__New emission; OoEmitInvoke Factory/NewSelf cases.
10. src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Call.cs — CallEmitRunUnit: MarkStoreAsImage(FactoryData) (~:93), _ecActive += FactoryBound (~:99).
11. src/Cobol.Net.Runtime — none.
12. docs/COBOLNET_OO_DESIGN.md — D11/D12/D13 added; CORRECTION notes on Summary :37, D7 :115, edge case :232 (static-fields sketch superseded — cite §8.6.4/:8765, SR4f, §9.3.6); STATUS banner + Greenfield seams; same change set.
13. tests/conformance/2002/ — oo_factory_basic|oo_factory_inherit|oo_factory_self .cob/.out + manifest.json enabled entries.
14. tests/Cobol.Net.Tests.Conformance/ — OoFactoryTests (matrix rows 2-6, 9-10) + trap #11; version-matrix negative cases (rows 11-12).
15. DEVLOG.md entry (top, real timestamp) + the plan §0 update; commit per feedback_commit_messages; full guard before commit.

## Sequencing note
Lands AFTER slice 3b (SELF/SUPER instance dispatch — the roster-switch code extends :184-217, and __New's covariance rides the 3a base-chain emission). The remaining banner order after this slice: PROPERTY → INTERFACE-ID (adds IMPLEMENTS to FACTORY + OBJECT) → universal reference (FACTORY OF refs + §16.2.2 FactoryObject + __CobolInvoke) → EC-OO (EC-OO-RESOURCE on New per §16.2.1 GR2 :39187 belongs there; today allocation failure is a .NET OOM — document, don't fake).

---

# Brief: OVERRIDE / FINAL method attributes (§11.7 SR2–SR8)

# OVERRIDE / FINAL method-attribute wave — decision-complete implementation brief

> Target: the OO deep-dive's "Spec correction #4 / D7" grammar gap — `METHOD-ID … [OVERRIDE] [IS FINAL]` and
> `CLASS-ID … [IS FINAL]` — plus the strict §11.7 SR4a enforcement it unlocks, FINAL→`sealed` emission, edition
> posture, diagnostics, and tests. Style: `docs/COBOLNET_OO_DESIGN.md` Decisions. Everything below is verified
> against the live tree (OoClassTable already marks `OverrideOf` by
> name; the emitter already renders `override` vs `virtual`).

## 0. Spec basis (ISO/IEC 1989:2023, `specs/ISO_COBOL.md` line anchors — re-verified this brief)

- **§10.6 method-definition** :12798–12821 — the ONLY method attributes are `[ OVERRIDE ] [ IS FINAL ]`,
  placed after the `{method-name-1 [AS literal-1] | {GET|SET} PROPERTY property-name-1}` brace (:12805, :12812).
- **§10.6 class-definition** :12739–12750 — CLASS-ID modifiers are `[AS literal-1] [IS FINAL] [INHERITS FROM …] [USING …]`
  in that ORDER (:12742–12744).
- **§11.3 CLASS-ID** :12999 — format :13009–13013; SR5 :13026 (`object-class-name-2 shall not be … defined with the
  FINAL clause`); GR3 :13043 (a FINAL class shall not be a superclass).
- **§11.7 METHOD-ID** :13210 — format figure :13220–13231; **SR2 :13238** (OVERRIDE not in a method prototype);
  **SR3 :13240** (OVERRIDE ⇒ a superclass method with the same method resolution signature exists AND that method
  is not FINAL); **SR4a :13242–13244** (no OVERRIDE ⇒ NO inherited method may have the same signature — i.e.
  redefinition WITHOUT OVERRIDE is an ERROR; this is why C# `new`/hiding is never emitted, D7); **SR8 :13262**
  (FINAL not in a method prototype); SR9 :13264 (inherited/implemented-name conformance per §9.3.8.2.3);
  **GR2 :13275** (OVERRIDE = overrides the inherited method); **GR3 :13277** (FINAL = shall not be overridden).
- §9.3.8.2 method-signature conformance :12294 (already enforced — COBOLNET0829, `OoClassTable.ValidateOverrideSignatures`).
- §A.4.10 :40408–40414 — OVERRIDE/FINAL are MANDATORY 2023 surface (not among the three optional OO items).
- Reserved words: `OVERRIDE` added 2002 (already a row in `ReservedWords.Table.cs:315` — R85=false, R2002+=true, high);
  `FINAL` reserved continuously since 1985 (row :191; it is already a lexer token, `CobolLexer.g4:358`, used by Report Writer).

## 1. Decisions

### D-A. Grammar shape: attributes inline in the two existing CobolOO.g4 rules, spec order, no new rules, no new predicates
`src/Cobol.Net.Frontend/Grammar/Core/CobolOO.g4`:

```
classIdParagraph                                            // :25–27 today
    : CLASS_ID DOT className (IS? FINAL)? (INHERITS FROM className)? DOT
    ;

methodDefinition                                            // :43–49 today
    : (IDENTIFICATION DIVISION DOT)? METHOD_ID DOT methodName OVERRIDE? (IS? FINAL)? DOT
      environmentDivision?  dataDivision?  procedureDivision?
      END METHOD methodName? DOT
    ;
```

- `IS` is a non-underlined (optional noise) word in both general formats → `IS?`.
- FINAL precedes INHERITS exactly per :12742–12744 — do NOT also accept the reversed order.
- LL-safe: `className`/`methodName` are single `cobolWord`s; FINAL is never in `cobolWord` ('85-reserved
  continuously), and OVERRIDE appears as a direct token in the attribute slot, so no left-factoring hazard.
  `id.className(0)` / `id.className(1)` and `m.methodName(0/1)` accessor indices in `OoClassTable.Build` are
  UNCHANGED (FINAL/IS/OVERRIDE are not className/methodName operands).
- No new `{is2002()}?` gate: both rules are reachable only through `classDefinition`, already gated at
  `CobolParserCore.g4:123` (`compilationGroup`). At `--std 85` the whole class unit is rejected today; nothing new.
- **Rejected:** a separate `methodAttributes` rule — one optional-token pair does not warrant a rule, and the
  future GET/SET PROPERTY selector (:12810–12814) will slot before the attributes in the SAME rule when the
  PROPERTY wave lands. **Rejected:** accepting `AS literal-1` now — method/class externalized names are an
  explicitly deferred item (deep-dive Edge cases, last bullet); adding half of it here widens the wave.

### D-B. OVERRIDE token strategy: a real context-sensitive lexer token, the established XOR/RAISE pattern
`OVERRIDE` currently lexes as IDENTIFIER (only the dead sketch `CobolParserOO.g4:96` mentions it). Per
`feedback_root_cause_no_workarounds` (real tokens, never IDENTIFIER text-matching), add it as the repo's standard
context-sensitive keyword — FOUR mirrored touch points (the pattern documented at `CobolParserCore.g4:19–24`):

1. `Core/CobolLexer.g4` — `OVERRIDE : 'OVERRIDE' ;` next to `OVERFLOW` (:437 band; any position before
   IDENTIFIER works, maximal munch keeps `OVERRIDE-X` one IDENTIFIER).
2. `Core/CobolLexer.g4` `_dataNameTokens` (members block, :30–52) — add `OVERRIDE` (subscript-mode correctness
   for `OVERRIDE(1)` as a data name at '85).
3. `CobolParserCore.g4` `cobolWord` (:25–91) — add `| OVERRIDE  // context: METHOD-ID attribute (2002+, §11.7); a legal user word at 85 — the §8.9 funnel rejects it 0901 at 2002+ (ReservedWords.Table row, added 2002)`.
4. `EditionValidator.CheckedTokenTypes` (`src/Cobol.Net.Compiler/Validation/EditionValidator.cs:268–…`) — add
   `CobolLexer.OVERRIDE`. Position-blind is correct: its only keyword position (the METHOD-ID attribute slot)
   is a direct token, never a `cobolWord`, so every `cobolWord` occurrence is a genuine user-word use. The
   existing table row (2002+, high confidence) then gives the full per-edition 0901 behavior for free.

`FINAL` needs NONE of this — already a token, already '85-reserved (never a user word at any edition).
- **Rejected:** semantic-predicate matching on IDENTIFIER text (`{LT(1).Text=="OVERRIDE"}?`) — the repo bans
  IDENTIFIER workarounds. **Rejected:** making OVERRIDE unconditionally reserved — it must remain a legal user
  word at `--std 85` (version-matrix continuity invariant; the funnel owns per-edition reservation).

### D-C. Pass-1 attribute capture + SR3/SR4a/SR5 enforcement in OoClassTable — strict by default, `--permissive` keeps today's inference
`src/Cobol.Net.Compiler/Binding/OoClassTable.cs`:

- `OoClassSymbol` (:232): add `public bool IsFinal { get; init; }` — set in `Build` (:135–141) from
  `id.FINAL() is not null`.
- `OoMethodSymbol` (:280–282): add positional `bool HasOverride, bool IsFinal` (before `Ctx`), filled in the
  method loop (:158–178) from `m.OVERRIDE()` / `m.FINAL()`.
- After base-link resolution (:181–192): if `sym.Base is { IsFinal: true }` →
  `edition.Error("COBOLNET0838", "class '{sym.Name}': INHERITS FROM '{base.Name}', which is declared FINAL — a FINAL class shall not be a superclass (ISO §11.3.3 SR5 / §11.3.4 GR3)")`.
- REWRITE the override-marking loop (:210–224; delete its "documented leniency" comment block — the leniency
  is retired this wave). For each `m` in each class with a base, `baseM = sym.Base.FindMethod(m.Name)`
  (nearest definition on the chain — correct for FINAL layering):
  - `baseM != null && m.HasOverride` → `m.OverrideOf = baseM`; if `baseM.IsFinal` →
    `Error("COBOLNET0838", "… overrides '{base}.{name}', which is declared FINAL — the overridden method shall not be defined with the FINAL clause (ISO §11.7.3 SR3; §11.7.4 GR3)")`.
  - `baseM != null && !m.HasOverride` → **SR4a**: `edition.Removed("COBOLNET0836", "class '{cls}': method '{name}' redefines a method inherited from '{base.Name}' without the OVERRIDE attribute (ISO §11.7.3 SR4a — an inherited method may only be redefined with OVERRIDE; add OVERRIDE to the METHOD-ID paragraph)")`
    and STILL set `m.OverrideOf = baseM` in both severities — under strict the compile fails before emission
    anyway (setting it keeps 0829 messages coherent); under `--permissive` this IS the pre-wave inference,
    preserved as the documented migration leniency (two-axis dialect model, `project_dialect_two_axes`;
    §10 #1 migration contract precedent = `method-working-storage-window`).
  - `baseM == null && m.HasOverride` → **SR3**:
    `Error("COBOLNET0837", "class '{cls}': method '{name}' specifies OVERRIDE but no superclass defines a method with that signature{sym.Base is null ? " (the class has no INHERITS clause)" : ""} (ISO §11.7.3 SR3)")`.
    Fires for both the no-INHERITS class and the wrong-name case.
- `ValidateOverrideSignatures` (0829) is UNCHANGED and still a hard error even under `--permissive` (D9: no
  overloading — two same-named methods with different signatures cannot emit as C# at all; hiding is never
  emitted). Optionally append "; ISO §11.7.3 SR3 requires the same method resolution signature" to its message.
- Name-match ≡ signature-match for lookup is sound under D9 (unique method names per class, overloading
  deferred); the signature half of SR3/SR4a is exactly what 0829 already validates.
- **Severity seam:** route 0836 through the existing `EditionContext.Removed` (`Binding/EditionContext.cs:62–66`
  — error strict / warning permissive) and widen its XML doc in the same change set to "removed-construct AND
  documented-dialect-leniency gating" (`feedback_one_mechanism_per_job`: one policy seam, never a duplicate
  `Lenient()` method, never a local `if (Permissive)` test).
- **Rejected:** keeping by-name auto-override as default (the pre-wave behavior) — it silently accepts
  nonconforming source and was only ever a documented stopgap ("until they land" — OoClassTable.cs:210 comment).
  **Rejected:** enforcing SR4a only at 2023 — SR4a is 2002 surface (OVERRIDE shipped with OO in 2002); the rule
  applies at every edition that has classes. **Rejected:** a new ConstructDialectStatus window row — OVERRIDE/FINAL
  are introduced-2002, never removed, no intra-OO edition delta ⇒ no VCR row (same rationale as OO's own
  introduction, deep-dive "Version gating" item 3).

### D-D. Emission: FINAL class → `sealed` class; FINAL method → `sealed override` / non-virtual root; beware CS0549
`src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Oo.cs`:

- `OoEmitClassUnit` :117 — `public class {cls.CsName}` becomes
  `public {(cls.Symbol.IsFinal ? "sealed " : "")}class {cls.CsName} : {…}`.
- `OoEmitMethod` :145–150 — replace the modifier line with the TOTAL mapping (D7):
  ```csharp
  string modifier =
      m.OverrideOf is not null
          ? (m.IsFinal && !cls.Symbol.IsFinal ? "sealed override" : "override")
          : (m.IsFinal || cls.Symbol.IsFinal) ? ""     // FINAL root method, or any fresh slot in a sealed class
          : "virtual";
  ```
  and fix the interpolation for the empty case (`public {modifier} {…}` would double-space):
  `$"public {(modifier.Length == 0 ? "" : modifier + " ")}{retType} …"`.
- **The Roslyn trap this table exists for:** a `virtual` member inside a `sealed` class is **CS0549** — a
  Roslyn error on EMITTED code, i.e. a loud-failure-invariant violation. Hence a FINAL class forces all its
  fresh slots non-virtual; overrides inside a sealed class emit plain `override` (implicitly sealed; the
  explicit `sealed override` is reserved for the non-sealed-class case where it carries meaning).
- A FINAL root method (fresh slot, non-virtual `""`) is correct for dispatch: it can never be overridden
  (0838 rejects the attempt at bind), so a direct call IS runtime-class dispatch. The future `__CobolInvoke`
  switch (D10) is unaffected — it calls by name within the class, not through a vtable slot.
- **Rejected:** emitting FINAL methods as `virtual` + relying on the binder to reject overrides — leaves
  meaning out of the emitted C# (the deep-dive's "FINAL→sealed override / FINAL root→non-virtual" mapping is
  the D7 contract) and emits a vtable slot no conforming program can use.

### D-E. Edition posture — 2002+, no new window rows, funnel-only deltas
- Introduction: OVERRIDE/FINAL attributes ride `classDefinition`'s existing `{is2002()}?` gate — a class unit
  at `--std 85` already fails; no separate 0900 site.
- Continuity: `OVERRIDE` as a USER word — legal at 85, 0901 at 2002/2014/2023 (table row exists; D-B item 4
  activates it). `FINAL` as a user word — already 0901 at every edition (continuous since '85). No
  `ConstructDialectStatus` rows, no VCR edits (2002-introduced, never removed, no 2002→2023 delta).
- SR2/SR8 (no OVERRIDE/FINAL in method PROTOTYPES) — a FORWARD OBLIGATION recorded for the INTERFACE-ID wave
  (interfaces are not in the grammar yet); note it in the deep-dive's INTERFACE-ID open question so it cannot
  be lost.

### D-F. Corpus: fix the three nonconforming oo_*.cob sources; new golden ships in the same commit
The existing overrides in `tests/conformance/2002/` violate SR4a as written (they predate the attribute):
- `oo_inherit.cob` — DOG's `METHOD-ID. SPEAK.` (:49) → `METHOD-ID. SPEAK OVERRIDE.`
- `oo_super.cob` — DOG's `METHOD-ID. SPEAK.` → add `OVERRIDE`.
- `oo_self_polymorphic.cob` — DOG's `METHOD-ID. SOUND.` → add `OVERRIDE`.
Goldens (`.out`) are byte-identical — attributes are compile-surface only. This is fixing INVALID test source
to conform to ISO (tests verify, never scope — and `feedback_root_cause_no_workarounds` forbids only changing VALID source).
These files are greenfield-only (2002 manifest); the legacy NIST guard never compiles them.

## 2. Exact change list (files/seams)

| # | File | Change |
|---|------|--------|
| 1 | `src/Cobol.Net.Frontend/Grammar/Core/CobolOO.g4` :25–27, :43–49 | D-A rule bodies |
| 2 | `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4` ~:437 + members :30–52 | `OVERRIDE` token + `_dataNameTokens` |
| 3 | `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` :25–91 | `cobolWord` += OVERRIDE |
| 4 | `src/Cobol.Net.Compiler/Validation/EditionValidator.cs` :268 | `CheckedTokenTypes` += `CobolLexer.OVERRIDE` |
| 5 | `src/Cobol.Net.Compiler/Binding/OoClassTable.cs` :135–141, :158–178, :181–192, :210–224, :280–282 | D-C capture + SR checks; delete the leniency comment |
| 6 | `src/Cobol.Net.Compiler/Binding/EditionContext.cs` :58–61 | widen `Removed` XML doc (leniency seam) |
| 7 | `src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Oo.cs` :117, :142–150 | D-D `sealed`/modifier table + comment refresh (drop "documented grammar gap") |
| 8 | `tests/conformance/2002/oo_inherit.cob`, `oo_super.cob`, `oo_self_polymorphic.cob` | add OVERRIDE (goldens unchanged) |
| 9 | `tests/conformance/2002/oo_override_final.cob` + `.out` + `manifest.json` | new golden (below) |
| 10 | `tests/Cobol.Net.Tests.Conformance/OoAttributeTests.cs` (new) | negative/positive suite (below) |
| 11 | `docs/COBOLNET_OO_DESIGN.md` | same change set: D7 AS-BUILT note (attributes landed; SR4a strict + permissive inference); update the "Missing:" grammar-seam list (:301) and the Open-questions grammar bullet (:375) — OVERRIDE/IS FINAL no longer missing; edge-case list :237 now cites 0837/0838; record the SR2/SR8 forward obligation on the INTERFACE-ID question |
| 12 | `DEVLOG.md` | new TOP entry, real `date "+%Y-%m-%d %H:%M %Z"` stamp |

## 3. Diagnostics (band audit: 0813, 0820–0829 OO-used; 0830–0835 INITIALIZE; 0845–0847 INSPECT; **0836/0837/0838 verified FREE**)

| Code | Rule | Severity | Message anchor |
|------|------|----------|----------------|
| COBOLNET0836 | §11.7 SR4a — redefinition without OVERRIDE | `EditionContext.Removed` (error strict / warning + inferred override under `--permissive`) | cites §11.7.3 SR4a + "add OVERRIDE" fix-it hint |
| COBOLNET0837 | §11.7 SR3 — OVERRIDE with no matching superclass method (incl. no INHERITS at all) | error | cites §11.7.3 SR3 |
| COBOLNET0838 | §11.7 SR3/GR3 — override of a FINAL method; §11.3.3 SR5/GR3 — INHERITS FROM a FINAL class | error (both sites, one code — the FINAL-violation family) | cites the exact § per site |
| COBOLNET0829 | §9.3.8.2 signature conformance (existing) | error (both axes — D9 makes it unemittable) | unchanged |
| COBOLNET0901 | OVERRIDE as a user word at 2002+ (existing funnel + table row) | via `Removed` policy (existing) | activated by D-B item 4 |

## 4. Tests (same commit — `feedback_goldens_ship_with_the_feature`, `feedback_goldens_ship_with_the_feature`)

**Golden** `tests/conformance/2002/oo_override_final.cob` (+ `.out`, + `manifest.json` entry): driver +
`CLASS-ID. ANIMAL.` with `METHOD-ID. SPEAK.` (virtual) — `CLASS-ID. DOG INHERITS FROM ANIMAL.` with
`METHOD-ID. SPEAK OVERRIDE IS FINAL.` — `CLASS-ID. KEEPER IS FINAL.` with a fresh `METHOD-ID. GREET.`;
driver invokes SPEAK through an ANIMAL-typed reference holding a DOG (proves `sealed override` dispatch) and
GREET on a KEEPER (proves a sealed class with a non-virtual method compiles under Roslyn and runs — the CS0549
regression trap). Compiled strict `--std 2002`, run, byte-compared by CorpusRunnerTests like the other oo_* rows.

**`OoAttributeTests.cs`** (new; reuse `OoSpineTests`' `DriverAndClass`/`CompileAndRun`/`ErrorsOf` +
`EditionHarness.AssertHasDiagnostic` harness, `OoSpineTests.cs:19–207`):
1. SR4a strict: subclass redefines without OVERRIDE → 0836 error (`--std 2002` AND `2023` — edition-invariant).
2. SR4a permissive: same source + `--permissive` → compiles, RUNS with override dispatch (the inferred-override
   semantics proof: output shows the subclass method), 0836 present in Warnings.
3. SR3: `OVERRIDE` on a method of a class with no INHERITS → 0837; `OVERRIDE` naming no base method → 0837.
4. FINAL method overridden (base declares `IS FINAL`, subclass writes OVERRIDE) → 0838.
5. `INHERITS FROM` a `CLASS-ID … IS FINAL` class → 0838.
6. OVERRIDE + signature mismatch (e.g. formal PIC 9(4) vs 9(6)) → 0829 still fires (regression pin).
7. Three-level chain: A.M virtual, B.M `OVERRIDE IS FINAL`, C.M `OVERRIDE` → 0838 (nearest-definition FindMethod
   pin).
8. Reserved-word continuity: `01 OVERRIDE PIC 9.` compiles at `--std 85`; 0901 at `--std 2023` (funnel
   activation pin; style of `EditionGateDiagnosticTests`).
9. `IS`-optionality: `METHOD-ID. M OVERRIDE FINAL.` and `CLASS-ID. C FINAL.` parse (noise-word rule).

## 5. Process cost — this touches THREE .g4 files (the expensive kind of change)

- **Authorization:** grammar changes normally require owner approval (`feedback_grammar_preauthorized`) — this one is
  pre-authorized by the RATIFIED plan: the deep-dive's Open-questions grammar bullet explicitly schedules
  "the ISO method attributes (OVERRIDE / IS FINAL …) added incrementally … each behind the {is2002()}? dialect
  predicate" before the corresponding emit slices, and Phase 3 is the ratified roadmap phase. Log the change in
  DEVLOG regardless.
- **Regen:** `Generated/` is a BUILD OUTPUT (untracked since DEVLOG 596) — the ANTLR regen runs at build (java +
  pwsh prerequisites; a failed regen FAILS the build). Regen must be verified on the Windows build; the WSL/Linux
  side rides CI (`feedback_generated_parser_is_a_build_output`, `reference_wsl_linux_repro`).
- **Guard:** `scripts/guard-fast.sh` (~3.3 min) green after the grammar edit BEFORE layering the binder/emitter
  work (`feedback_grammar_version_factoring` — incremental, one guard-fast per grammar step), then the FULL
  legacy guard (`scripts/guard.sh`) + the full conformance battery + the corpus sweep before commit — a new
  lexer token can shift tokenization anywhere in the '85 corpus, and flipping SR4a from leniency to error is
  exactly the "flipped gated check needs an adversarial sweep, not just a corpus dry-run" lesson
  (`project_p1_diagnostics`).
- **Docs in the same change set:** deep-dive updates (row 11 above — `feedback_follow_the_deep_dive`),
  grammar comments already carry the doc-sync content (`feedback_grammar_preauthorized`), DEVLOG entry per commit.

## 6. Explicitly OUT of scope (forward obligations, recorded so they are not re-derived)

- `AS literal-1` on CLASS-ID/METHOD-ID (externalized names) — stays on the deferred list.
- GET/SET PROPERTY selector (:12810–12814) — the PROPERTY wave; the attribute slot in `methodDefinition` is
  already positioned after `methodName` so the selector alternative slots into the same brace later.
- SR2/SR8 (attributes forbidden in method prototypes) — lands with INTERFACE-ID; record on that open question.
- §11.3.3 SR6 (same-named methods inherited from multiple bases vs FINAL) — moot under the v1 single-inheritance
  restriction (SSOT §18.18).
- `__CobolInvoke` interaction — the dynamic-dispatch wave; no attribute impact (name-switch, not vtable).

---

# Brief: Universal object-reference dispatch (D10, §13.18.60.4)

> **IMPLEMENTED** per D-U1–D-U8, with these decisions beyond the brief body below: the diagnostic codes are
> **0866/0867/0868** (0836-0838 are held by FACTORY/OVERRIDE); a locked decision **D-U6a** (canonical-by-
> descriptor box forms — one canonical box form per descriptor so the caller and callee sides cannot
> desync); the SET F5 dataReference-sender path is a SEMANTIC re-route in BindSetTo (ANTLR
> alternative order — setToValueStatement wins the parse); SR13 factory-object senders are LIVE into
> UNIVERSAL receivers (typed receivers need the FACTORY OF usage phrase — not yet carried, 0867-named);
> the descriptor⇔mismatch invariant is proven BEHAVIORALLY (the 9(4)/9(8) hazard pair). The authoritative
> record is the deep-dive's D10 section.

# Implementation brief — UNIVERSAL object-reference dispatch (OO deep-dive D10 wave)

> Slice: the "universal reference" wave of the Phase-3 OO port (banner order: FACTORY → PROPERTY → INTERFACE-ID → **universal reference** → EC-OO; `docs/COBOLNET_OO_DESIGN.md`). Everything below is derived from `specs/ISO_COBOL.md` (ISO/IEC 1989:2023) with line anchors, and from the seams in the live tree. Style = the deep-dive's Decisions format: chosen design + rejected alternatives + citations + exact files/seams + diagnostics + tests.

## 0. Scope

IN: (a) per-class `__CobolInvoke` switch override emission (roster incl. inherited via base chaining); (b) INVOKE through a universal receiver, incl. identifier-2 dynamic method names; (c) the arg boxing/unboxing model vs the typed strict-conformance model — §14.9.23.4 GR7c runtime conformance → EC-OO-UNIVERSAL; (d) SET Format 5 object-reference-assignment (headline: SET universal TO typed); (e) object relation conditions (`U = NULL`, `U = G`) and the disposition of the non-ISO "IS class" test.
OUT (staged loud, owning wave named): FACTORY objects/`SET x TO ClassName` (FACTORY slice); object views + EC-OO-CONFORMANCE (§8.4.3.5, spec :6760; EC-OO wave); interface-name'd references and ACTIVE-CLASS (`INTERFACE-ID` slice; the USAGE grammar carries neither yet — `Core/CobolOO.g4:95-98` has only `OBJECT REFERENCE [className]`); EXCEPTION-OBJECT (implicitly universal, :7249 — EC-OO slice); OMITTED args.

## 1. Spec ground truth (all anchors = specs/ISO_COBOL.md line numbers)

- **Universal defined**: §13.18.60.2 GF — `OBJECT REFERENCE [interface-name-1 | [FACTORY OF] ACTIVE-CLASS | [FACTORY OF] object-class-name-1 [ONLY]]` (:22664-22668). §13.18.60.4 GR 22 (:22905): GR 22b (:22912) — no optional phrase ⇒ **universal object reference; "Its content may be a reference to any object."** Glossary :2352; Annex D rationale :46226. NULL is NOT universal (:7283); SELF/SUPER are NOT (:7331); EXCEPTION-OBJECT IS (:7249).
- **INVOKE**: format §14.9.23.2 :28376-28390 (no exception phrase — deep-dive Spec correction #3). SR4 (:28401): a NON-universal identifier-1 **shall** use literal-1. SR5 (:28427): for a non-universal receiver the BY-phrase pairing rules and **§14.8.2/§14.8.3 conformance apply (compile time)** — SR5c :28433. **SR6 (:28435): universal receiver ⇒ neither BY CONTENT nor BY VALUE shall be specified; BY REFERENCE is assumed implicitly.** **SR7 (:28437): identifier-2 (method name in a data item) only when identifier-1 is universal.** SR8 (:28439): identifier-2 alphanumeric or national. SR9/SR10 (:28441/:28443): argument storage-section rules; object (factory/instance) data may not be an argument crossing by reference. GR2a (:28494): the content of identifier-2 IS the method name per §8.3.2.2 (case-insensitive user-defined word). GR5 (:28506) EC-OO-NULL. GR7b (:28528) EC-OO-METHOD. **GR7c (:28530): universal receiver ⇒ no ANY LENGTH formal/returning, and §14.8.2/§14.8.3 conformance applies AT RUNTIME; violation ⇒ EC-OO-UNIVERSAL** ("if checking … enabled in both the activated method and the activating runtime element"), invocation unsuccessful. GR8 RETURNING delivery.
- **Runtime-vs-compile conformance**: §9.3.8.2.1 NOTE (:12291) — "Conformance checking is done at compile time only, except … object views and methods using universal object references is done at runtime." §14.8.2 (:25317): **universal ⇔ universal pairing** — if either the argument or the formal is universal, the other shall be too (this is why the existing strict `DescriptionMismatch` object-ref case, which requires equal declared-class-or-both-universal, is already spec-exact). :25262 — universal path raises EC-OO-UNIVERSAL when enabled.
- **EC**: Table 13 (:24748-24756) — **EC-OO-UNIVERSAL, Fatal, "A runtime type check failed" (:24756)**; EC-OO-METHOD :24753; EC-OO-NULL :24754; EC-OO-CONFORMANCE (:24750) is the OBJECT-VIEW condition, NOT this slice's.
- **SET Format 5** (object-reference-assignment) §14.9.39: format :31162-31166 — `SET {identifier-3}… TO {object-class-name-1 | identifier-4}`. SR8 (:31298): identifier-3 = any class-object receiving item. SR9 (:31300): identifier-4 = an object reference; **SUPER forbidden**. SR12 (:31335): when identifier-3 is class-described, identifier-4 shall be class-described (same class or subclass, :31341), ACTIVE-CLASS-described, SELF (:31353), or NULL (:31369) — **a universal identifier-4 is NOT in the list ⇒ SET typed TO universal is a compile-time error** (narrowing needs an object view). A UNIVERSAL identifier-3 is unconstrained (SR10/12/13 trigger only on interface-/class-described receivers) ⇒ **SET universal TO typed is unconditionally legal**. SR13 (:31371) object-class-name-1 sender = the factory object (FACTORY slice). GR9/GR10 (:31592/:31594): copy the reference into each identifier-3 in order.
- **Object relations**: §8.8.4.2.1 (:9495) — class-object operands form a message-tag-object-or-pointer-reference relation; comparisons defined for "Two operands of class object" (:9509). **Format 3 (:9591-9598): only `IS [NOT] EQUAL TO / IS [NOT] = / IS <>`.** SR5 (:9614): both operands class object, same category. §8.8.4.2.15 (:9765-9769): `identifier-3 = identifier-4` true iff **the same object** (reference identity).
- **"IS class-name" on an object reference is NOT ISO.** The class condition (page 193) is the character-class test (NUMERIC/ALPHABETIC/…) only; no instance-of condition exists anywhere in the 2023 text, and the live grammar has no surface for it (`Core/CobolExpressions.g4:133-139`). The ISO runtime-type-test surfaces are Format-3 relations (this slice) and object views (§8.4.3.5, identifier Format 5 :6760, EC-OO-CONFORMANCE — deferred).

## 2. Decisions

### D-U1. A universal object reference emits as C# `CobolObject?` (NOT `object?`).
`PicInfo.ClrType` (src/Cobol.Net.Compiler/Binding/PicInfo.cs:186-187) changes its universal arm from `"object?"` to `"CobolObject?"`. Rationale: CobolObject IS the runtime universal type (D2 — every emitted class derives from it); GR2b defers non-COBOL INVOKE, so "any object" = "any COBOL object" for v1; dispatch sites need no cast, SET/relations get implicit upcasts, and a non-CobolObject can never leak in. **Rejected**: `object?` (the deep-dive C# mapping sketch line :190) — forces a cast at every `__CobolInvoke` site and admits values the deferred-interop rule excludes. Update the deep-dive mapping in the same change set (rule 4). `DefaultInitializer` stays `null`. EXCEPTION-OBJECT (EC-OO slice) will follow the same choice.

### D-U2. `__CobolInvoke` signature = `void __CobolInvoke(string name, CobolInvokeArg[] args, CobolInvokeArg? returning)`; each class overrides with a switch over the methods it DECLARES that are NOT overrides; `default:` chains `base.__CobolInvoke(...)`.
- New runtime type **`src/Cobol.Net.Runtime/Control/CobolInvokeArg.cs`**: `public sealed class CobolInvokeArg(string descriptor, object? value = null) { public string Descriptor { get; } = descriptor; public object? Value { get; set; } = value; }` — mutable Value = the BY REFERENCE write-back channel (SR6: everything through a universal receiver is BY REFERENCE).
- Change `CobolObject.__CobolInvoke` (src/Cobol.Net.Runtime/Control/CobolObject.cs:30-33) to the new signature; the default body keeps throwing `CobolFatalException("EC-OO-METHOD", …)` (GR7b — reaching the root means no class in the chain declares the name). One-commit propagation; no other callers exist yet (no-transitional-hacks rule).
- Add `public static string NormalizeMethodName(string raw) => raw.TrimEnd().ToUpperInvariant();` to CobolObject — identifier-2 content is a user-defined word (GR2a + §8.3.2.2, case-insensitive; trailing PIC X padding is not part of the name). Case labels emit as `m.Name.ToUpperInvariant()` (the COBOL spelling, e.g. `"ADD-TO"`, not CsName). Roster dictionaries are already case-insensitive so uppercase labels cannot collide.
- **Roster rule**: emit cases only for `cls.Symbol.Methods.Where(m => m.OverrideOf is null)`. An override needs no case: the base class's case calls `this.M(...)`, which dispatches virtually to the override, and 0829 (`OoClassTable.ValidateOverrideSignatures`) already guarantees identical descriptors. Inherited methods resolve via the `default:` base chain — the chain IS §9.3.6 resolution order. **Rejected**: emitting cases for every method incl. overrides (redundant, and two case sites per method drift); a global `Dictionary<(Type,string),…>` (deep-dive D10 rejection stands).
- A class declaring zero non-override methods emits NO `__CobolInvoke` override.

### D-U3. Runtime conformance = descriptor-string equality, generated from the SAME rule as compile-time strict conformance (the singular-pattern requirement).
Add `public static string ConformanceDescriptor(DataItem item)` to **src/Cobol.Net.Compiler/Binding/OoClassTable.cs**, beside `DescriptionMismatch` (:68-117) — one home for both projections of §14.8.2/§14.8.3. Normative format (encodes exactly the facts DescriptionMismatch compares — i.e. the slice-2 crossing forms):
- fixed-point numeric → `N:{Usage}:{Digits}:{Scale}:{S|U}`; float → `F:float` / `F:double`;
- string crossing (group with a character image, or alphanumeric) → `S:{width}` (group ⇒ `ImageWidth`, alphanumeric ⇒ `Pic.Length`) — this reproduces DescriptionMismatch's deliberate group⇄alphanumeric image compatibility;
- Tier-C group (no character image) → `T:!` (never emitted for args — bind-rejected, mirroring DescriptionMismatch — so it can never match);
- numeric-edited → `E:{picture-text}:{Length}` (identical-description requirement §14.8.2);
- object reference → `O:{DECLARED-CLASS-UPPER}` or `O:*` (universal) — string equality reproduces :25317's universal⇔universal pairing and the strict same-class rule already in DescriptionMismatch:97-101.
**Locked invariant (unit-tested)**: for every category the typed path carries, `ConformanceDescriptor(a) == ConformanceDescriptor(b)` ⇔ `DescriptionMismatch(a,b) is null`. The two rules can never drift because the descriptor is derived next to (and tested against) the one mismatch function. **Rejected**: carrying .NET types only (lossy — PIC 9(4) and 9(8) are both `long`; scale/sign invisible ⇒ GR7c undetectable); reflection (D10 rejection, not AOT-safe); a runtime re-implementation of the conformance rules (two mechanisms for one job).

### D-U4. Callee-side generated switch (boxing/unboxing + GR7c enforcement).
Emitted at the end of `OoEmitClassUnit` (src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Oo.cs — after the methods loop at :124-125), new `OoEmitCobolInvoke(OoClassUnit cls, CodeWriter w)`:
```csharp
public override void __CobolInvoke(string name, CobolInvokeArg[] a, CobolInvokeArg? ret)
{
    switch (name)
    {
        case "ADDTO":   // METHOD-ID ADD-TO would emit case "ADD-TO"
        {
            if (a.Length != 1) throw new CobolFatalException("EC-OO-UNIVERSAL", "...arity... (ISO §14.9.23.4 GR7c/§14.8.2)");
            if (a[0].Descriptor != "N:Display:4:0:U") throw new CobolFatalException("EC-OO-UNIVERSAL", "...argument 1 does not conform... (GR7c)");
            if (ret is null || ret.Descriptor != "N:Display:4:0:U") throw new CobolFatalException("EC-OO-UNIVERSAL", "...returning item... (§14.9.23.4 GR7c/§14.8.3)");
            long __p0 = (long)a[0].Value!;
            ret.Value = this.ADDTO(ref __p0);   // virtual ⇒ an override wins even when this case sits in the base's switch
            a[0].Value = __p0;                  // SR6: BY REFERENCE write-back through the box
            return;
        }
        default: base.__CobolInvoke(name, a, ret); return;
    }
}
```
Descriptor consts come from `ConformanceDescriptor(f.Item)` over `m.Formals`/`m.Returning` (bound by `DataBinder.OoBindMethodData` before any emit). Unbox per the formal's crossing form (`OoStringCarried` — CSharpEmitter.Oo.cs:202-204): string-carried ⇒ `(string)`, native ⇒ `(long)/(Int128)/(float)/(double)`, object ref ⇒ `({FormalClass}?)` (descriptor equality makes the cast total; a null reference value is a legal argument — conformance is about descriptions). RETURNING presence mismatches in BOTH directions are EC-OO-UNIVERSAL (the runtime analog of the typed binder's dual 0828, StatementBinder.Oo.cs:345-359). Void method: `ret is not null` ⇒ EC-OO-UNIVERSAL; method with RETURNING invoked without ⇒ same.
**GR7c "enabled in both" decision**: v1 raises unconditionally, exactly like the EC-OO-NULL/EC-OO-METHOD precedent (CobolObject.cs:30-40 throws without a TURN gate). Rationale: when checking is off the spec leaves the outcome to §14.6.13.1.4, and proceeding with a nonconforming crossing in a typed-native model is memory-unsafe nonsense — the loud-failure invariant picks the fatal raise. Document in the deep-dive EC notes. ANY LENGTH (GR7c's other clause) has no implementation anywhere yet — moot; note it in the D10 AS-BUILT.

### D-U5. Binder: `InvokeForm` stays for the static forms; universal dispatch binds to a NEW `BoundInvokeUniversal` record (bound facts differ in KIND — there is no formal roster at compile time).
In **src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.Oo.cs**:
- `public sealed record BoundInvokeUniversal(Place Receiver, string? MethodLiteral, Place? MethodSource, IReadOnlyList<BoundUniversalArg> Args, Place? Returning, string? ReturningDescriptor) : BoundStatement;` and `public sealed record BoundUniversalArg(Place Source, string Descriptor);` (every arg is BY REFERENCE per SR6 ⇒ always write-back). This is not a second mechanism for one job: BoundInvoke carries binder-resolved rosters; BoundInvokeUniversal carries caller-side descriptors — different facts, one record each. **Rejected**: overloading BoundInvoke with nullable parallel lists (a two-shapes-in-one-type smell) and a fifth InvokeForm sharing the typed marshal path (the typed path's `BoundInvokeArg.Formal` is meaningless here).
- `OoBindInvoke` rewires: DELETE the identifier-2 BoundUnsupported (:174-176) and the universal BoundUnsupported (`OoBindInstanceInvoke`:295-297). New flow: if the receiver Place is `PicCategory.ObjectReference` with `ObjectClassName is null` ⇒ `OoBindUniversalInvoke`. Inside it: method selector = literal (normalized uppercase at bind time) OR identifier-2 dataReference — resolve; SR8: alphanumeric (national staged) else **0836**. If the receiver is TYPED and identifier-2 was used ⇒ **0836** (SR7 :28437). SELF/SUPER/NULL receivers are never universal (:7331/:7283) — existing paths unchanged.
- Argument rules (SR6 + SR9/SR10, all **0836** with citations): explicit `BY CONTENT`/`BY VALUE` forbidden (:28435); a literal or arithmetic-expression argument is rejected (BY REFERENCE is assumed implicitly and a literal cannot be a by-reference operand — SR6 + GR6's non-universal-only scope); an OBJECT-data argument is rejected outright (SR10 bans the by-reference crossing and SR6 removes the BY CONTENT fallback that GR6a2 gives the typed path — use `data.OoIsObjectData`, DataBinder.Oo.cs:51-57); explicit `BY REFERENCE` is fine. Tier-C groups (no character image) reject at bind (mirror DescriptionMismatch:74-77). Each accepted arg gets `ConformanceDescriptor(place.Item)`.
- RETURNING: resolve identifier-4, record `ConformanceDescriptor` — NO compile-time conformance (GR7c: runtime; §9.3.8.2.1 NOTE :12291); presence checking is also runtime-only (the callee cannot be known).

### D-U6. Caller-side emission (`OoEmitUniversalInvoke` in CSharpEmitter.Oo.cs; dispatch case added to the EmitStatement switch in CSharpEmitter.cs beside `BoundInvoke → OoEmitInvoke`).
```csharp
// INVOKE U MNAME USING X RETURNING R   (id = _ooInvokeCounter++)
var __ua3 = new CobolInvokeArg[] { new("N:Display:4:0:U", _X) };            // box per the ARG's own crossing form
var __ur3 = new CobolInvokeArg("N:Display:4:0:U");                          // null literal `(CobolInvokeArg?)null` when no RETURNING
CobolObject.RequireNonNull(_U).__CobolInvoke(CobolObject.NormalizeMethodName(_MNAME.Read()), __ua3, __ur3);   // GR5 guard unchanged
_X = (long)__ua3[0].Value!;                                                 // BY REFERENCE copy-out — descriptor equality ⇒ same form
_R = (long)__ur3.Value!;                                                    // GR8, via the receiver's own bridges when forms differ
```
Boxing per the ARG's crossing form (the callee's is identical by descriptor equality): string-carried ⇒ `place.Read()` / groups via `CallStringRead`; image-stored numerics box their image STRING; native numerics box the unscaled value; object refs box the reference. Copy-out mirrors: groups `CallStringWrite`, image-stored numerics write the image back, natives assign (subscripted args read once into the box and write once back — GR7a's evaluate-at-start for free). No direct-`ref` fast path exists here BY DESIGN — the box IS the crossing (rejected: a `ref`-capable fast path — the abstract dispatch signature cannot take refs without generics-per-signature, defeating the one-switch design). GR8 result delivery reuses the typed tail's bridge pattern (CSharpEmitter.Oo.cs:309-325) keyed on the RECEIVER's storage form. Literal method names through a universal receiver take the same path with a compile-time-normalized string const (SR4 permits literal-1 with universal; it still cannot bind statically).

### D-U7. SET Format 5 goes live (StatementBinder.cs:715-716 currently `BoundUnsupported`).
- New `OoBindSetObjectReference` (StatementBinder.Oo.cs) + `public sealed record BoundSetObjectRef(IReadOnlyList<Place> Targets, Place? Source, bool SourceIsNull, bool SourceIsSelf) : BoundStatement;` + `OoEmitSetObjectRef` (CSharpEmitter.Oo.cs): `tgt.Write(expr)` per target in order (GR9 :31592) where expr = `src.Read()` / `null` / `this`.
- Bind rules: target `PicCategory.ObjectReference` else **0837** (SR8 :31298). Sender `NULL` ⇒ ok (SR12d). `SELF` ⇒ only in a method (reuse the 0827 pattern); typed target additionally requires `OoCurrentClass.ConformsTo(targetClass)` (SR12c2 :31357) else **0837**. `SUPER` ⇒ **0837** (SR9 :31300). dataReference sender: resolve data-first; object-ref item required; **target universal ⇒ always conformant (SET universal TO typed — SR10/12 never trigger on a universal receiver)**; target typed ⇒ sender must be typed AND `senderCls.ConformsTo(targetCls)` (SR12a2 :31341) — **a universal sender into a typed target is 0837** (SR12's closed list; the narrowing tool is an object view, deferred). An unresolvable-as-data name that IS a class of `OoClasses` ⇒ the SR13 factory-object form ⇒ `BoundUnsupported` naming the FACTORY slice. ONLY/FACTORY phrases have no grammar surface yet — nothing to check (note in doc).
- Grammar delta (one token): `setObjectReferenceStatement : {is2002()}? SET dataReference+ TO objectReference` (src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4:1049-1051) to honor `{identifier-3}…`; run guard-fast after regen per the incremental-grammar rule; log under the Phase-3 OO drive's standing authorization (memory feedback_grammar_preauthorized analog — flag in the DEVLOG/commit).
- Registry: add `new("set-object-reference-2002", "SET … TO object-reference (Format 5)", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.39 F5 (OO); grammar-gated; LIVE as of the universal wave")` to src/Cobol.Net.Compiler/Validation/ConstructDialectStatus.cs (pattern of `invoke-2002`, :66).

### D-U8. Object relation conditions ride BoundRelational; "IS class" is struck as non-ISO.
- Binder (`BindComparison`, StatementBinder.cs:1170; figurative NULL already binds as `BoundFigurative('N')`, :866): when either resolved operand is an ObjectReference item ⇒ Format 3. Enforce: operator `=`/`<>` only (Format 3 :9591-9598) else **0838**; the other operand must be an ObjectReference item or figurative NULL (SR5 :9614 — both class object) else **0838**; typed-vs-typed of UNRELATED classes is LEGAL (SR5 requires same category only — reference identity may simply be false). SELF/SUPER as relation operands: stage `BoundUnsupported` (loud, rare; note in doc).
- Renderer (`RenderRelational`, src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs:38-53): add the object branch BEFORE the figurative branch at :42 (otherwise `'N'` falls into the width-materializing path and mis-renders): `object.ReferenceEquals(l.Read(), r.Read())` (§8.8.4.2.15 :9769 — same-object identity), `({x.Read()}) is null` for NULL, `!(…)` for NOT. C# implicit upcast covers typed-vs-universal (`CobolObject?`) mixes.
- **"IS class" disposition (spec-verified)**: NOT ISO — no spec surface, no grammar surface. Do NOT add it. STRIKE the deep-dive C# mapping/edge-case lines claiming `IF G IS GREETER → G is GREETER` (docs/COBOLNET_OO_DESIGN.md :191 and the "`o IS Class`" edge-case bullet :241) in the same change set (process rule 4), replacing them with: Format-3 relations (this slice) + object views §8.4.3.5 (:6760, EC-OO-CONFORMANCE :24750, deferred to the EC-OO/object-view wave); a vendor IS-class test would be dialect-gated extension surface, never default.

## 3. Diagnostics (new; the OO band 0813/0820-0829 is full — 0836-0838 are unused repo-wide, verified)
- **COBOLNET0836** — universal-INVOKE misuse: explicit BY CONTENT/BY VALUE via universal (SR6 :28435); identifier-2 with a non-universal receiver (SR7 :28437); identifier-2 not alphanumeric (SR8); literal/arith-expression argument via universal (SR6 — BY REFERENCE assumed, literal cannot cross by reference); OBJECT-data argument via universal (SR10 :28443 + SR6); Tier-C group argument (no character image).
- **COBOLNET0837** — SET Format 5 violations: non-object receiver (SR8 :31298); SUPER sender (SR9 :31300); universal-into-typed / non-conforming typed-into-typed (SR12 :31335-:31369); SELF outside a method / SELF class non-conformance (SR12c).
- **COBOLNET0838** — object relation violations: ordering operator on class-object operands (Format 3 :9591); object-vs-non-object operand mix (SR5 :9614).
- Runtime: `CobolFatalException("EC-OO-UNIVERSAL", …)` (Table 13 :24756, Fatal) from the generated switch — arity, per-arg descriptor, RETURNING presence/descriptor; EC-OO-METHOD from the CobolObject default (unchanged); EC-OO-NULL guard unchanged. Edition surface: everything is 2002+ and already rides the existing `{is2002()}?` gates + `invoke-2002`/`usage-object-reference-2002` rows; the one NEW row is `set-object-reference-2002` (D-U7). No VCR rows (OO's 2002 introduction has no VCR row by design — deep-dive "Version gating" §3).

## 4. File/seam summary
1. `src/Cobol.Net.Runtime/Control/CobolInvokeArg.cs` — NEW (D-U2).
2. `src/Cobol.Net.Runtime/Control/CobolObject.cs:30-33` — signature change + `NormalizeMethodName` (D-U2).
3. `src/Cobol.Net.Compiler/Binding/PicInfo.cs:187` — universal ClrType → `"CobolObject?"` (D-U1).
4. `src/Cobol.Net.Compiler/Binding/OoClassTable.cs` — `ConformanceDescriptor` beside `DescriptionMismatch` (D-U3).
5. `src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.Oo.cs` — `BoundInvokeUniversal`/`BoundUniversalArg`, `OoBindUniversalInvoke`, delete BoundUnsupporteds :174-176/:295-297, `OoBindSetObjectReference`, `BoundSetObjectRef` (D-U5/D-U7).
6. `src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.cs:715` (SET routing), `:1170` BindComparison object branch (D-U7/D-U8).
7. `src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.Oo.cs` — `OoEmitCobolInvoke` (hook: end of `OoEmitClassUnit`, after :124-125 loop), `OoEmitUniversalInvoke`, `OoEmitSetObjectRef` (D-U4/D-U6/D-U7); EmitStatement switch cases in `CSharpEmitter.cs` beside the BoundInvoke case.
8. `src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs:38-42` — object branch before the figurative branch (D-U8).
9. `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4:1049-1051` — `dataReference+` (D-U7; guard-fast after regen). NO INVOKE grammar change (identifier-2 alt already at `Core/CobolOO.g4:71-74`).
10. `src/Cobol.Net.Compiler/Validation/ConstructDialectStatus.cs` — `set-object-reference-2002` row.

## 5. Tests (verify, never scope — the features above are complete regardless)
Conformance goldens `tests/conformance/2002/` (+ manifest enable, run + byte-compare):
- `oo_universal.cob/.out` — two classes each declaring "DESCRIBE"; `01 U USAGE OBJECT REFERENCE.`; SET U TO each typed ref; INVOKE U "DESCRIBE" (polymorphism through universal + SET universal TO typed + literal selector). Doubles as the phase demo (verify output CORRECT).
- `oo_universal_name.cob` — `01 MNAME PIC X(10).`; MOVE "BUMP" TO MNAME; `INVOKE U MNAME USING N RETURNING R` — identifier-2 + trailing-space trim + write-back through the box + GR8 delivery.
- `oo_universal_inherit.cob` — inherited method through universal (default-chain) AND an overridden method through universal (base's case + virtual dispatch ⇒ subclass output).
- `oo_universal_relation.cob` — `IF U = NULL`, `SET U TO NULL`, `IF U NOT = NULL`, `IF U = G` (identity true and false).
Unit/adversarial `tests/Cobol.Net.Tests.Conformance/OoUniversalTests.cs` (OoSpineTests pattern):
- Descriptor⇔DescriptionMismatch invariant over a representative item matrix (D-U3's lock).
- THE polymorphic hazard: two classes, same method name, DIFFERENT PIC on the formal; invoke through universal with the wrong-shaped arg ⇒ EC-OO-UNIVERSAL, never silent wrong data.
- Arity mismatch ⇒ EC-OO-UNIVERSAL; RETURNING presence mismatch both directions ⇒ EC-OO-UNIVERSAL; unknown name in identifier-2 ⇒ EC-OO-METHOD; null universal receiver ⇒ EC-OO-NULL.
- Bind negatives: BY CONTENT/BY VALUE via universal ⇒ 0836; identifier-2 with typed receiver ⇒ 0836; literal arg via universal ⇒ 0836; object-data arg via universal ⇒ 0836; SET typed TO universal ⇒ 0837; SET … TO SUPER ⇒ 0837; SET non-object target ⇒ 0837; `IF U > G` ⇒ 0838; `IF U = 5` ⇒ 0838.
- `--std 85` negatives already covered by the invoke/usage gates (matrix rows exist).
Pre-commit: full battery + corpus sweep + legacy guard per the standing rules.

## 6. Doc updates (same change set — rule 4)
`docs/COBOLNET_OO_DESIGN.md`: D10 gains an AS-BUILT note (CobolInvokeArg signature vs the sketched `object?[]`; descriptor model + the D-U3 invariant; unconditional GR7c raise decision; roster = declared-non-override + base chaining); C# mapping :188-196 corrected (universal → `CobolObject?`; the __CobolInvoke sketch updated); edge-case bullets :240-241 rewritten (universal live; IS-class struck per D-U8); banner/Greenfield-seams staged-item lines for SET (StatementBinder.cs:668-669 reference) and D10 cleared. DEVLOG entry (descending, real timestamp) + the plan §0 banner + `docs/DOC_INDEX.md` untouched (no new doc).

---

# Brief: EC-OO (Table 13 + RAISE identifier + EXCEPTION-OBJECT)

> **IMPLEMENTED** per D-EO1–D-EO10, with these decisions beyond the brief body below: the diagnostic codes
> are **0848/0849/0858/0859** (0830-0833 are taken elsewhere); D-EO9's "minimal SET F5" is the
> EXCEPTION-OBJECT sender leg on the full SET Format 5; UNIVERSAL dispatch sites (D10) also run the
> pickup; the EXCEPTION-OBJECT reserved word carries a register-context 0901-funnel exemption
> (objectReference + the SET re-route shape). The authoritative record
> is `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md`'s EC-OO section + the deep-dive's closed bullet.

# EC-OO slice — implementation brief (OO deep-dive slice 6: the exception-object channel over the landed EC engine)

> **Status: DECISION-COMPLETE brief for roadmap Phase 3, OO port, slice 6 (EC-OO).** It builds on
> `OverrideOf`/`ConformsTo`/`InvokeForm.Self|Super`, all in the tree. The EC
> engine substrate is in place: `>>TURN`/TurnState fold, USE F3 `__EcDispatch`, RAISE/RESUME,
> `ExceptionState` + `CobolFatalException` + the GOBACK/EXIT RAISING **named** propagation with the CALL-site
> pickup. EC-OO-NULL / EC-OO-METHOD are already live in `CobolObject` (`src/Cobol.Net.Runtime/Control/CobolObject.cs:30-40`).
> This slice adds the **exception-object** channel: RAISE identifier-1, the EXCEPTION-OBJECT register,
> USE Format 4 (EXCEPTION OBJECT), RAISING on method PD headers, GOBACK/EXIT … RAISING identifier in methods
> AND programs, INVOKE-site + CALL-site object propagation pickup, and the unhandled-object → EC-OO-EXCEPTION
> conversion. Fold this brief into `docs/COBOLNET_OO_DESIGN.md` (slice-6 AS-BUILT) and
> `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN` in the SAME change set (feedback_follow_the_deep_dive).

## 1. Spec model (all anchors = specs/ISO_COBOL.md line numbers, verified this session)

- **Two kinds of exception**: "An exception condition is either a condition associated with a specific
  exception status indicator or an exception object. An exception object is any object raised by a RAISE
  statement or an EXIT, GOBACK, or STOP statement in which the object is specified in the RAISING phrase"
  (:24428/:24469). Exception OBJECTS are NOT exception-names — they are **not TURN-gated** (§7.3.25 TURN takes
  exception-names only); the object channel is always live once used.
- **RAISE §14.9.29** (:29727-29761): format `RAISE {EXCEPTION exception-name-1 | identifier-1}` (:29737-29741).
  SR2 (:29748): identifier-1 shall be an object reference; **NULL and SUPER shall not be specified** (SELF is
  legal). SR3: sending operand. GR2 (:29761): EXCEPTION-OBJECT is set to reference the object; **if there is no
  applicable declarative, processing continues with the statement following the RAISE** (never fatal by itself).
  GR1 (:29757): a NAMED raise sets EXCEPTION-OBJECT to null (already implemented — `ExceptionState.Set`).
- **Exception objects §14.6.13.1.5** (:24575-24608): on raise, (1) EXCEPTION-OBJECT := the object (:24579);
  (2) the last exception status indicates an exception object (:24581). RAISE-raised: run the associated
  declarative, then continue after the RAISE; none → continue (:24583). EXIT/GOBACK-raised, in the ACTIVATOR:
  (1) object not covered by the activated element's PD-header RAISING classes/interfaces → as if
  `EXCEPTION EC-OO-EXCEPTION` in the RAISING phrase, fatal per §14.6.13.1.3 (:24596-24602); (2) a USE (F4) in
  the activating element matches → declarative, then normal continuation (:24604); (3) PROPAGATE ON directive
  → re-propagate (:24606) — **directive not implemented in this compiler; recorded residue**; (4) otherwise as
  if `EXCEPTION EC-OO-EXCEPTION` (:24608).
- **Table 13 EC-OO family** (:24748-24756): EC-OO-EXCEPTION "An exception object was not handled" — **Fatal**
  (:24751); all EC-OO are Fatal except EC-OO-IMP. Already in `ExceptionCatalog` (ExceptionCatalog.cs:136-143).
- **GOBACK §14.9.18** (:27652-27753): SR4 (:27683-27699): identifier-1 shall be an object reference; SR4a — its
  data description's object-class-name (or a superclass) **shall be specified in the PD-header RAISING phrase**
  of the containing source element (FACTORY parity); SR4b interface / SR4c ACTIVE-CLASS (both later slices);
  **SR4d — identifier-1 shall not be a universal object reference** (:27699). SR5 (:27701): LAST only in a
  declarative / PERFORM WHEN. GR1b2 (:27720): the object becomes the current exception object in the ACTIVATING
  element. GR1b3a (:27724): RAISING LAST with an object → 14.6.13.1.5 rules. §14.9.18.4 GR1b ordering: the exception is
  raised in the activator "**after the result, if any, of the activated element is returned**" (:27716).
  EXIT PROGRAM RAISING mirrors (§14.9.14, :27370, SR at :27403-27413); EXIT METHOD RAISING exists only pre-2023
  (`exit-method-window` registry row already handles the 0902).
- **PD header §14.2** (:23624-23634): `RAISING {exception-name-1 | [FACTORY OF] object-class-name-1 |
  interface-name-1}…`. SR7 (:23686): exception-name-1 shall be a **level-3 EC-USER** name. SR8/9 (:23688-23690):
  class/interface names (REPOSITORY — greenfield resolves via the group `OoClassTable`, the same documented
  deviation as INVOKE class resolution).
- **EXCEPTION-OBJECT §8.4.3.6** (:7232-7256): a predefined object reference; SR1 — never a receiving operand;
  SR2 (:7249) — implicitly **universal**, external, class object; GR1 — references the current exception object,
  null when none; GR2 — ONE instance per run unit (⇒ a static register is correct).
- **USE Format 4 §14.9.49** (:32905-32910): `USE AFTER {EXCEPTION OBJECT | EO} {object-class-name-1 |
  interface-name-1}` (ONE operand). SR15 EO≡EXCEPTION OBJECT (:32972); SR16/17 repository names. GR3 (:32989):
  "If an exception object was raised, the method specified in General rule 14 is applied" — F4 selection
  REPLACES the F1/F3 tiers for object raises. GR14 (:33093-33097): source-order scan; class entry matches the
  object's class **or a subclass** (a); interface entry via IMPLEMENTS (b); no match → 14.6.13.1.5. GR15
  (:33099): **upon declarative entry EXCEPTION-OBJECT references the exception object**.
- **EXCEPTION-STATUS §15.33.3 r1** (:35701): returns the exception-name **or the value 'EXCEPTION-OBJECT'**,
  31 chars, space-filled; last-exception-status concept incl. "the fact that an exception object was raised"
  (:24481).
- **EC-RAISING-NOT-SPECIFIED** (:24787) applies to EC-USER names only (already handled, COBOLNET0717).
- Runtime type-check failure on a universal→typed crossing: §9.3.8.2 runtime conformance (:12291) +
  EC-OO-UNIVERSAL "A runtime type check failed" (:24756). (EC-OO-CONFORMANCE is the object-VIEW failure,
  §8.4.3.5 :7211-7229 — NOT this case.)

## 2. Decisions

### D-EO1. ONE signal architecture: extend the existing carriers, never a parallel OO mechanism
The object channel rides the LANDED shapes (feedback_one_mechanism_per_job): `BoundRaising` gains the object leg;
`ExceptionState` gains the object register/propagation slot beside `_propagated`; `CallEmitPropagationPickup`
(CSharpEmitter.Call.cs:696-709) grows the object branch and is invoked from **both** CALL and INVOKE sites.
**Rejected:** a distinct `ObjectException`/.NET-exception-based propagation (a thrown C# exception cannot honor
GR1b's "result returned first, exception raised after" ordering, and would fork the pickup protocol);
a separate BoundGobackObject node (two nodes for one statement family).

### D-EO2. EXCEPTION-OBJECT register = `ExceptionState.ExceptionObject` + the `"EXCEPTION-OBJECT"` LastName sentinel
`ExceptionState` (src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs) additions:
```csharp
public const string ObjectSentinel = "EXCEPTION-OBJECT";   // §15.33.3 r1's literal returned value
public static void SetObject(object? obj)     // RAISE id / F4 declarative entry (GR15) / GR1b2 activator side
    { LastName = ObjectSentinel; LastFatal = false; LastFile = null; LastIoStatus = null;
      LastStatement = null; LastLocation = null; ExceptionObject = obj; }
private static (bool Has, object? Obj) _propagatedObject;
public static void SetPropagatingObject(object? obj) { SetObject(obj); _propagatedObject = (true, obj); _propagated = null; }
public static bool TakePropagatedObject(out object? obj)  // mirrors TakePropagated; clears the slot
```
`SetPropagatingLast()` gains: when `LastName == ObjectSentinel`, stage `_propagatedObject` instead of the named
slot (GR1b3a :27724). `Set(...)` keeps nulling `ExceptionObject` (:24485) and must ALSO clear
`_propagatedObject` = default; `SetPropagating` clears the object slot (the two slots are mutually exclusive).
`Clear()` clears both slots. **EXCEPTION-STATUS works with ZERO changes** — `EcFunctions.Status()`
(EcFunctions.cs:16-17) pads `LastName`, and the sentinel IS the §15.33.3 r1 required string; `Location()`/
`Statement()`/`File()` degrade correctly (not an IO name → "00"; no location saved → spaces).
**Rejected:** a separate `LastIsObject` bool (special-cases every EXCEPTION-* function); making the sentinel a
catalog entry (it is NOT an exception-name — `ExceptionCatalog.TryGet` must keep failing on it, which it does).

### D-EO3. RAISE identifier-1 → `BoundRaiseObject`; grammar takes `objectReference`
Grammar (CobolParserCore.g4:1119-1121): `RAISE (EXCEPTION cobolWord | dataReference)` becomes
`RAISE (EXCEPTION cobolWord | objectReference)` (objectReference = dataReference | NULL_ | SELF | SUPER,
:1053-1056) so SR2's "NULL and SUPER shall not be specified" gets a TARGETED diagnostic instead of a parse
error, and `RAISE SELF` (legal, deep-dive :367 note) parses. Bind (`StatementBinder.Exceptions.cs` — replace
the `BoundUnsupported` at :53-54): keep the existing 0876 pre-2002 gate; NULL/SUPER → **COBOLNET0830**; SELF
outside a method → 0830; dataReference must resolve to `Pic.Category == PicCategory.ObjectReference` (else
0830); set `_ecRaise = true` (the machinery gate — NO TurnState consultation: objects are not TURN-gated, §1
above). New node: `public sealed record BoundRaiseObject(Place? Source) : BoundStatement;` (`Source == null`
⇔ SELF). Emission (`CSharpEmitter.Exceptions.cs`, new `EcEmitRaiseObject`, dispatch case beside BoundRaise in
CSharpEmitter.cs:346+):
```csharp
var __eo{n} = <Source.Read() | "this">;            // identifier-1, sending operand (SR3)
ExceptionState.SetObject(__eo{n});                  // §14.6.13.1.5 (1)/(2)
int __r{n} = {EcObjDispatchExpr("__eo" + n)};       // F4 GR14; "-3" const when the unit has no F4
if (__r{n} >= 0) { __pc = __r{n}; break; }          // RESUME AT procedure-name (§14.9.33.4 GR3)
// -1/-2/-3 all fall through: execution continues with the statement following the RAISE (§14.6.13.1.5/GR2)
```
**Rejected:** funneling through `BoundRaise` with a null EcName (BoundRaise carries TurnState-fold facts that
do not exist for objects); a raw throw (RAISE of an object is NEVER fatal by itself — GR2).

### D-EO4. RAISING identifier legs: extend `BoundRaising` with `Place? ObjectSource`; bind-time SR4 discharge
`BoundRaising` (StatementBinder.Exceptions.cs / bound records) becomes
`BoundRaising(string? EcName, bool IsLast, bool Fatal, bool Enabled, Place? ObjectSource = null)` — tri-state
invariant: exactly one of EcName / IsLast / ObjectSource. `EcBindRaising` (:149-169) identifier leg (replace
`return null` at :157): resolve the dataReference; require ObjectReference category; **SR4d** universal
(`Pic.ObjectClassName is null`) → **COBOLNET0831**; **SR4a** — the declared class, or one of its superclasses
(walk `OoClassSymbol.Base`), must appear in the containing source element's PD-header RAISING class list, else
0831 (compile-time; the FACTORY-parity / interface / ACTIVE-CLASS legs stage 0899). Callers stop degrading:
`CallBindGoback` (StatementBinder.Call.cs:218-222) and EXIT PROGRAM (StatementBinder.cs:249-251) drop their
`BoundUnsupported` fallbacks. **Method context** (StatementBinder.Oo.cs): `OoBindMethodGoback` (:504-510) and
`OoBindExitMethod` (:515-528) now bind the raising phrase — `BoundMethodReturn` becomes
`BoundMethodReturn(BoundRaising? Raising = null)`; RAISING LAST inside a method → reject (SR5: only in a
declarative/WHEN; methods have no declaratives yet — 0899 wording pointing at the method-declaratives stage).
GOBACK RETURNING/GIVING in a method stays unsupported (unchanged).

### D-EO5. The activated-side EC-OO-EXCEPTION check (§14.6.13.1.5 EXIT/GOBACK item 1) — static for `RAISING identifier-1`, run-time for `RAISING LAST EXCEPTION`
`RAISING identifier-1`: GOBACK SR4 / EXIT SR5 force the identifier's DECLARED class (or superclass, or an interface
it conforms to, with the SAME FACTORY presence) into the header list, and every object a typed reference holds
conforms to its declared class (§14.8), and an identifier-1 that is UNIVERSAL is refused (SR4d). So item 1 **always
holds** for the operand and the binder discharges it at COMPILE time (`EcBinder.RaisingObjectMismatch`,
COBOLNET0849); no generated check follows the operand's staging.
`RAISING LAST EXCEPTION` (kb/Work PB1121): the object it re-propagates is whatever the last exception status holds,
its class known only at run time, so the static discharge cannot reach it. The emitted staging passes
`ExceptionState.ExceptionObject is <census>` to `ExceptionState.SetPropagatingLast`, where `<census>` is the element's
ONE header-RAISING list of C# types (`EcState.PdRaisingObjectCsTypes`: each class entry's own type, a FACTORY OF entry's
factory type, each interface entry's implementing types; `false` when the header names none). A miss stages the NAMED
EC-OO-EXCEPTION instead of the object ("as if EXCEPTION EC-OO-EXCEPTION were specified in the RAISING phrase"), which
the activator raises only when checking for it is enabled there (§14.9.18.4 GR1 b)). The same census serves item 3
(PROPAGATE ON). **Rejected:** a second, narrower list for LAST (two copies of one membership rule).

### D-EO6. Staging + pickup: the GOBACK/EXIT/MethodReturn side stages, the activating SITE consumes
`CallEmitRaisingStage` (CSharpEmitter.Call.cs:863-883) gains the object leg:
`if (r.ObjectSource is { } os) { w.Line($"ExceptionState.SetPropagatingObject({os.Read()});"); return; }`
(no Enabled/Fatal logic — objects are not TURN-gated). `BoundMethodReturn` emission (CSharpEmitter.cs:421-423)
becomes: stage the Raising (same helper), THEN `throw new MethodReturn();` — the method entry's
`catch (MethodReturn)` (CSharpEmitter.Oo.cs:183-184) still runs the formal copy-outs and returns the RETURNING
local, which realizes GR1b's result-before-exception ordering for free. `CallEmitPropagationPickup`
(CSharpEmitter.Call.cs:696-709) grows the object branch FIRST (slots are mutually exclusive):
```csharp
if (ExceptionState.TakePropagatedObject(out var __po{n})) {          // §14.6.13.1.5 activator rules
    ExceptionState.SetObject(__po{n});                                // GR1b2 — current exception object HERE
    int __pr{n} = {EcObjDispatchExpr("__po" + n)};                    // rule 2 — USE F4 (GR14)
    if (__pr{n} >= 0) { __pc = __pr{n}; break; }
    if (__pr{n} == -3) {                                              // rule 3 PROPAGATE: not implemented (residue)
        ExceptionState.Set("EC-OO-EXCEPTION", true);                  // rule 4 — as if EXCEPTION EC-OO-EXCEPTION (:24608)
        int __pq{n} = {EcDispatchExpr("\"EC-OO-EXCEPTION\"", "\"\"")}; // F3 tiers: EC-OO-EXCEPTION/EC-OO/EC-ALL match
        if (__pq{n} >= 0) { __pc = __pq{n}; break; }
        if (__pq{n} != -2) throw new CobolFatalException("EC-OO-EXCEPTION",
            "an exception object was not handled (ISO 14.6.13.1.5; Table 13 - fatal)");
    }                                                                 // -1/-2: declarative completed / RESUME NEXT → continue
} else if (ExceptionState.TakePropagated(...)) { …existing named branch unchanged… }
```
**INVOKE sites call the SAME helper**: `OoEmitInstanceInvoke` (CSharpEmitter.Oo.cs:237-330) calls
`CallEmitPropagationPickup()` after the RETURNING delivery + copy-outs (`foreach (var p in post)`), for
Instance/Self/Super forms. NEW needs no pickup (the generated ctor runs no user statements in v1 — D4).
Gating is `_ecActive`, which ALREADY spans class units (CSharpEmitter.Call.cs:99-100: `classes.Any(c =>
c.Bound.Ec is { Any: true })`), and `_ecRaising` is set by the method binder — so a group whose only EC use is
a method GOBACK RAISING obj still gets pickups at every INVOKE site. The unhandled-in-Main safety net: the
fatal `CobolFatalException` from rule 4 is the boundary default (there is no registry between INVOKE sites, so
a missing pickup would silently drop the object — hence the rule: **pickup after EVERY Instance/Self/Super
INVOKE and every CALL when `_ecActive`**; add the sweep to the review checklist).
**Rejected:** routing method propagation through ProgramRegistry (INVOKE bypasses the registry by design —
deep-dive "Registry/GOBACK seam"); throwing from inside the method (breaks GR1b ordering, see D-EO1).

### D-EO7. USE Format 4 → `__EcObjDispatch` (the GR14 selector); grammar + BoundDeclarative extension
Grammar (Core/CobolControlFlow.g4:176-198): add alternative
`| USE AFTER (EXCEPTION OBJECT | EO) cobolWord` (ONE name operand per the format figure). `EO` joins the
context-sensitive word set exactly like `EC` (CobolParserCore.g4:40 + the lexer `_dataNameTokens` mirror —
the continuity invariant: EO stays a legal user word at every edition). Binder
(StatementBinder.Declaratives.cs): 2002+ gate mirroring F3's (0876-band); resolve the word against
`OoClasses` — unknown → **COBOLNET0833** (an interface name is indistinguishable until INTERFACE-ID lands, so
the message says "not a class of the compilation group; interface entries are the INTERFACE-ID slice");
`BoundDeclarative` (BoundTree.cs:59-68) gains `string? EoClassCsName = null`; binding an F4 sets `_ecF3 = true`
(the one "EC declaratives present" feature bit — F4 rides the same group gate). Emitter
(CSharpEmitter.Exceptions.cs): new `_ecUnitHasF4` beside `_ecUnitHasF3` (:29), 
`EcObjDispatchExpr(objExpr) => _ecUnitHasF4 ? $"__EcObjDispatch({objExpr})" : "-3"`, and the generated selector
(emitted beside `EcEmitDispatchSelector` at the `__RunUse` machinery site, CSharpEmitter.cs ~:123-177):
```csharp
private int __EcObjDispatch(object? __obj)
{
    // §14.9.49.4 GR14 — source order; class-or-subclass = C# `is` (GR14a); GR15: EXCEPTION-OBJECT already set
    if (__obj is FOO) return __RunUse(i, startPc, handlerEndPc);
    ...
    return -3;                                    // no match → 14.6.13.1.5 (the caller's rule-4 conversion)
}
```
`__obj is FOO` covers subclasses exactly per GR14a; a null object matches nothing → -3 (raising a null
identifier is degenerate but spec-literal: EXCEPTION-OBJECT holds null, no declarative can match). Program
units only — declaratives inside methods were then 0899; they are implemented since kb/Work PB1010 (COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN, METHOD-SCOPED SELECTION).
**Rejected:** matching by class-name string against `GetType().Name` (breaks on inheritance and is
reflection-shaped); folding F4 entries into `EcEntries` (different match domain — GR3 explicitly bypasses the
F1/F3 tiers for object raises).

### D-EO8. Method (and program) PD-header RAISING: partition into EC-USER names + classes
Retire the 0899 at DataBinder.Oo.cs:146-148. In `OoBindMethodData` (DataBinder has `OoClasses` — set at
CSharpEmitter.Oo.cs:50): for each `raisingClause` cobolWord — (a) a catalog level-3 **EC-USER** name → the
method's `RaisingEcNames`; (b) a class in `OoClassTable` → `RaisingClasses` (store `OoClassSymbol`); (c) any
other catalog EC name → **COBOLNET0832** (§14.2.2 SR7: exception-name-1 shall be level-3 for EC-USER); (d)
unresolvable → 0832 (message notes interface names are the INTERFACE-ID slice). Add both lists to
`OoMethodSymbol` (OoClassTable.cs:280+). PROGRAM headers: upgrade `EcCollectPdRaising`
(StatementBinder.Exceptions.cs:173-177) to the same partition — `_pdRaising` keeps the EC-USER names (the
existing 0717 SR2 check is untouched); a NEW `_pdRaisingClasses` set feeds the D-EO4 SR4a check. Per-source-
element scoping: `BindClassBody` (StatementBinder.Oo.cs:99+) resets/loads `_pdRaising`/`_pdRaisingClasses`
from each method's own PD header before binding that method's pcs (a method IS a source element — §14.9.18.3
SR2/SR4a say "the source element containing this GOBACK statement"). **Sweep obligation:** grep
`tests/conformance` for existing PD-header RAISING uses before tightening SR7 (0832) — any non-EC-USER name in
a header was previously accepted-uninterpreted.

### D-EO9. Observability: minimal SET Format 5 (`SET obj-ref TO {identifier | NULL | EXCEPTION-OBJECT}`)
Retire the stage at StatementBinder.cs:715-716 for these sender forms (SELF/SUPER senders stay 0899). New node
`BoundSetObjectRef(Place Target, Place? Source, bool FromExceptionObject, bool ToNull)` (one node, variant
flags — no fake Places for the register: EXCEPTION-OBJECT is NOT a DataItem). Resolution: the grammar's
`objectReference → dataReference` leg first tries `refs.Resolve`; an UNRESOLVED simple name spelled
`EXCEPTION-OBJECT` (case-insensitive) is the §8.4.3.6 register (implicitly universal, :7249; never a receiving
operand — SR1: a TARGET spelled EXCEPTION-OBJECT is a diagnostic, 0830-band). Conformance: target universal ←
anything; target typed ← typed requires sender class `ConformsTo` target class (bind-time, 0828-band wording);
target typed ← EXCEPTION-OBJECT (universal) emits the RUNTIME check —
`recv = ExceptionState.ExceptionObject as FOO; if (recv is null && ExceptionState.ExceptionObject is not null)
throw new CobolFatalException("EC-OO-UNIVERSAL", "runtime type check failed …(ISO 9.3.8.2 :12291; Table 13)")`.
Emission: `Target.Write(<expr>)` (object-ref fields are plain C# fields; `SET x TO NULL` → `null`).
**Rationale:** GR15/§8.4.3.6 are core slice surface and untestable without a read path; SET F5 is also the last
staged OO SET leg. **Rejected:** testing only via class-discriminating F4 declaratives (leaves the register
semantics unverified); implementing full universal-reference storage/INVOKE (the separate universal wave, D10).

### D-EO10. What stays staged LOUD (0899, named owning wave)
PROPAGATE ON directive (:4829/:24606 — preprocessor wave; the pickup's rule-3 hole is documented in the
generated comment); interface-name in F4 / header RAISING (INTERFACE-ID slice); FACTORY OF in header RAISING +
factory-object matching in GR14a (FACTORY slice); ACTIVE-CLASS (SR4c); STOP … RAISING (no 2023 grammar surface
in the tree; :24428 mentions it — verify §14.9.42 when the STOP status phrase wave opens); declaratives inside
methods; RAISING LAST inside a method (needs method declaratives); EXCEPTION-OBJECT as an INVOKE receiver
(universal dispatch, D10 wave).

## 3. Files & seams (complete change list)

| File | Change |
|---|---|
| `Grammar/CobolParserCore.g4:1119` | raiseStatement: dataReference → objectReference |
| `Grammar/Core/CobolControlFlow.g4:176` | useStatement: `USE AFTER (EXCEPTION OBJECT \| EO) cobolWord` alternative |
| `Grammar/CobolParserCore.g4:40` + lexer | EO context-sensitive word + `_dataNameTokens` mirror; regen both OSes (feedback_generated_parser_is_a_build_output); guard-fast after the grammar step (feedback_grammar_version_factoring). Grammar changes are inside the ratified Phase-3 OO scope (`docs/COMPLETION_ROADMAP_COUNCIL.md`) — log per feedback_grammar_preauthorized |
| `Runtime/Exceptions/ExceptionState.cs` | D-EO2: ObjectSentinel, SetObject, SetPropagatingObject/TakePropagatedObject, SetPropagatingLast object leg, slot exclusivity, Clear |
| `Binding/Bound/StatementBinder.Exceptions.cs:53-54` | BindRaise identifier leg → BoundRaiseObject (D-EO3) |
| `Binding/Bound/StatementBinder.Exceptions.cs:149-177` | EcBindRaising ObjectSource leg + SR4a/SR4d (D-EO4); EcCollectPdRaising partition (D-EO8) |
| `Binding/Bound/StatementBinder.Call.cs:218-222`, `StatementBinder.cs:249-251` | drop the identifier-form BoundUnsupported fallbacks |
| `Binding/Bound/StatementBinder.Oo.cs:504-528` | method GOBACK/EXIT METHOD RAISING → BoundMethodReturn(Raising); per-method _pdRaising reset in BindClassBody |
| `Binding/Bound/StatementBinder.cs:715-716` | SET F5 minimal (D-EO9) |
| `Binding/Bound/StatementBinder.Declaratives.cs` | USE F4 binding → BoundDeclarative.EoClassCsName; sets _ecF3 |
| `Binding/Bound/BoundTree.cs:59-68` | BoundDeclarative + EoClassCsName |
| `Binding/DataBinder.Oo.cs:146-148` | method header RAISING resolution (D-EO8) |
| `Binding/OoClassTable.cs` | OoMethodSymbol.RaisingEcNames/RaisingClasses |
| `CodeGen/CSharpEmitter.Exceptions.cs` | EcEmitRaiseObject; _ecUnitHasF4 + EcObjDispatchExpr + the __EcObjDispatch selector (D-EO7) |
| `CodeGen/CSharpEmitter.Call.cs:696-709, 863-883` | pickup object branch; CallEmitRaisingStage object leg |
| `CodeGen/CSharpEmitter.cs:346+, 421-423` | BoundRaiseObject/BoundSetObjectRef dispatch cases; BoundMethodReturn stages Raising before the throw; F4 selector emission beside EcEmitDispatchSelector |
| `CodeGen/CSharpEmitter.Oo.cs:237-330` | OoEmitInstanceInvoke tail calls CallEmitPropagationPickup (Instance/Self/Super; _ecActive-gated) |
| `docs/COBOLNET_OO_DESIGN.md`, `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN*` | slice-6 AS-BUILT + this brief's decisions; close the "Open questions" EC-OO bullet; DEVLOG entry; the plan §0 |

## 4. Diagnostics (new band; all COBOL-worded, never a Roslyn CS on user source — the G4 rule)

- **COBOLNET0830** — RAISE identifier-1 violations: not an object reference / NULL or SUPER specified
  (§14.9.29.3 SR2) / SELF outside a method; EXCEPTION-OBJECT as a receiving operand (§8.4.3.6 SR1).
- **COBOLNET0831** — GOBACK / EXIT … RAISING identifier-1: universal object reference (§14.9.18.3 SR4d /
  §14.9.14.3); declared class (and superclasses) not specified in the PD-header RAISING phrase (SR4a).
- **COBOLNET0832** — PD-header RAISING operand invalid: a non-EC-USER exception-name (§14.2.2 SR7) or an
  unresolvable word (SR8/SR9; interfaces = INTERFACE-ID slice).
- **COBOLNET0833** — USE AFTER EXCEPTION OBJECT: unknown class name (§14.9.49.3 SR16).
- Edition: RAISE below 2002 already 0876 (fires before the leg split — keep); USE F4 below 2002 → the same
  0876-band message as F3; EXIT METHOD RAISING at 2023 already rides `exit-method-window` (0902).
- Version-matrix negatives: RAISE identifier and USE F4 at `--std 85` (targeted diagnostic, not a parse error).

## 5. Tests (ship in the SAME commit — feedback_goldens_ship_with_the_feature)

Conformance goldens (`tests/conformance/2002/`): `oo_ec_raise_object.cob/.out` — RAISE obj (subclass) with two
F4 declaratives (base class + unrelated class): base-class declarative runs (GR14a subclass match), DISPLAYs
FUNCTION EXCEPTION-STATUS (= `EXCEPTION-OBJECT` padded), `SET U TO EXCEPTION-OBJECT` + class-discriminated
output, execution continues after RAISE; `oo_ec_goback_raising.cob/.out` — method `GOBACK RAISING obj` with
header RAISING, INVOKE-site F4 handles, RETURNING value verified delivered BEFORE the declarative runs (GR1b).

Unit/adversarial (`tests/Cobol.Net.Tests.Conformance/OoEcTests.cs`, OoSpineTests-style):
1. `RaiseObject_NoDeclarative_ContinuesNextStatement` (GR2 — never fatal).
2. `MethodGobackRaising_NoF4_EcOoException_FatalNonzero` (rule 4: stderr names EC-OO-EXCEPTION, exit ≠ 0).
3. `MethodGobackRaising_NoF4_F3CatchesEcOoException` (USE AFTER EC EC-OO — the rule-4 name enters the F3 tiers).
4. `Goback_RaisingObject_AcrossCALL_PickupRuns` (program→program; CALL-site branch).
5. `RaisingLast_WithObject_Repropagates` (declarative GOBACK RAISING LAST EXCEPTION → object slot staged).
6. `SetTypedFromExceptionObject_WrongClass_EcOoUniversal` (:12291/:24756).
7. `NestedInvoke_ObjectPropagatesTwoLevels` (SELF/SUPER-site pickups; each hop re-stages).
8. Diagnostics: 0830 (RAISE NULL / RAISE non-objref), 0831 (SR4a undeclared class; SR4d universal), 0832
   (header lists EC-SIZE-TRUNCATION), 0833, `--std 85` negatives.
9. `ExceptionState` pure-unit: sentinel via EXCEPTION-STATUS; Set clears the object; slot exclusivity;
   SET LAST EXCEPTION TO OFF clears everything; named raise nulls EXCEPTION-OBJECT (§14.9.29.4 GR1 / :24485).
Zero-scaffolding regression: an EC-free group's generated source is byte-identical (existing invariant test
must stay green — every new emission is `_ecActive`/`_ecUnitHasF4`/node-presence gated).

Order of work: grammar+regen+guard-fast → ExceptionState (+unit tests) → binder legs (0830-0833) → emitter
(RAISE obj, F4 selector, staging, pickup) → SET F5 → conformance goldens → full battery + guard + doc/DEVLOG
sync. Compile/test after every step (feedback_tiered_gates); one failing test at a time.

---

# Brief: INTERFACE-ID + IMPLEMENTS + PROPERTY (§11.6/§13.18.42)

> **IMPLEMENTED** — declarations complete (interfaces, IMPLEMENTS + the 0841 binder-authoritative
> conformance pass with covariant adapters, PROPERTY clause + explicit accessors, 0840/0841/0842 bands,
> registry rows, 4 goldens). Decisions beyond this brief: the diagnostic codes are 0840–0843 (0836 is held
> by FACTORY); factory IMPLEMENTS EMITS (D11 singletons, not the validate-only posture); interface GET/SET
> PROPERTY prototypes are STAGED under a named 0899 — a later increment. The authoritative record is the
> deep-dive's INTERFACE/PROPERTY section. **Property REFERENCES are also IMPLEMENTED** — the D-P2 desugar
> as designed (codes 0843; the polarity classifier is `Binding/Bound/BoundStores.StoreKindOf`, a total
> emitter-verified taxonomy walk; the factory leg is live directly). See the D-P2 realization notes below
> the rejected-alternatives block.

# Design brief — INTERFACE-ID + IMPLEMENTS + PROPERTY (OO port, post-FACTORY slice)

> **Scope.** The three remaining declared-surface OO features after FACTORY in the banner order
> (`docs/COBOLNET_OO_DESIGN.md` line 30: FACTORY → PROPERTY → INTERFACE-ID → universal reference → EC-OO; this
> brief covers PROPERTY and INTERFACE-ID/IMPLEMENTS together because PROPERTY's explicit `METHOD-ID. GET/SET
> PROPERTY` selector, the property-method conformance rule §11.7.3 SR9 (:13264), and interface method
> prototypes all share one signature machinery). All three are MANDATORY for a conforming 2023 implementation
> (§A.4.10 :40408-40414 — only the two INHERITS repetitions and parametric polymorphism are optional).
> Everything below is 2002-introduced surface: every new grammar rule is `{is2002()}?`-gated with a paired
> ConstructRegistry row (G1 dual obligation), and `tests/version-matrix/constructs.json` must gain the same
> rows in the same commit (ConstructRegistryDriftTests asserts registry↔json equality BOTH directions —
> `src/Cobol.Net.Compiler/Validation/ConstructDialectStatus.cs:20-25`).

**Dependency note.** The FACTORY slice precedes this one (banner order). This brief assumes `factoryParagraph`
exists in `Core/CobolOO.g4` and `OoClassSymbol` distinguishes factory vs instance methods. If FACTORY has not
landed, the IMPLEMENTS-on-FACTORY leg and factory properties stage loud (0899) and everything else lands
independently — no hard blocker.

---

## D-I1. INTERFACE-ID → one emitted C# `interface`; the BINDER's explicit conformance pass is the AUTHORITY; Roslyn satisfaction is a safety net PLUS a dispatch mechanism, never the checker

**Chosen design.** An `interface-definition` (§10.6 :12783-12796; §11.6 :13157-13199) becomes a pass-1
`OoInterfaceSymbol` in `OoClassTable` (prototype roster reusing `OoMethodSymbol`), is validated by a NEW
binder pass `OoClassTable.ValidateImplements(edition)` implementing §9.3.11 (:12458-12460) via §9.3.8.2.3
(:12309-12423), and is emitted as a real C# `public interface <CsName>` whose members carry the exact
signature shapes `OoEmitMethod` already produces (`ref` params, typed/`string` return). A class whose OBJECT
paragraph says `IMPLEMENTS I` emits `public class FOO : <Base or CobolObject>, I_CS`. Interface-typed object
references (`USAGE OBJECT REFERENCE interface-name`, §13.18.60.2 :22679-22682) become C# fields of the
interface type; INVOKE through them is static C# interface dispatch (§14.9.23.3 SR4e :28417) behind the
existing `CobolObject.RequireNonNull` GR5 guard — no `__CobolInvoke`, no universal-wave dependency.

**Why the explicit conformance pass is REQUIRED, not optional (the deep-dive's open question, line 376,
answered).** Roslyn-checked satisfaction is wrong in BOTH directions:
1. **Roslyn under-rejects.** COBOL conformance rule 3 (§9.3.8.2.3 :12335) demands identical PICTURE/USAGE/
   SIGN descriptions for non-object formals; the C# projection is lossy — `PIC 9(4)` and `PIC 9(8)` both emit
   `ref long`, so Roslyn happily accepts a class whose method does NOT conform to the interface prototype. A
   Roslyn-only check silently admits non-conforming COBOL.
2. **Roslyn over-rejects legal COBOL.** Rule 5c2 (:12375) allows an implementing method's object-reference
   RETURNING item to be a SUBCLASS of the prototype's class, and rule 5a (:12351) lets any object reference
   implement a universal returning item — C# interface implementation requires the EXACT return type (no
   covariant returns on interface implementations), so conforming COBOL would die as a CS error on user
   source, violating the G4 loud-failure invariant (a user-visible CS error is an emitter BUG — deep-dive D1).
The cure for (2): when the class method's C# signature differs from the interface member only in a
conformant-covariant return, emit an **explicit interface implementation adapter**:
`BASE_T I_CS.M(ref long a) => this.M(ref a);` (object references are class types — the implicit reference
conversion is free). Params never need adapters: conformance rule 2b/2c (:12321-12323) requires the SAME
interface/class name on corresponding object-reference params, and rule 3 forces identical non-object
descriptions, so parameter C# types are always identical when the binder pass succeeds.

**Conformance pass content (§9.3.8.2.3 mapped to checks, all through the ONE shared
`OoClassTable.DescriptionMismatch` — `OoClassTable.cs:61-117` — so INVOKE-arg, override (0829), and
implements conformance can never drift):**
- name match case-insensitive over the class's full instance interface (own + inherited methods, §9.3.8.2.2
  :12304); a missing method = 0837 (§9.3.11: "shall implement ALL the method specifications … including
  inherited ones").
- rule 1 (:12315) formal count (BY-mode consistency joins when header BY VALUE parses — today all-REFERENCE,
  trivially consistent); rules 2/3 via `DescriptionMismatch` (object-ref: same declared class — extend the
  `PicCategory.ObjectReference` arm to also match interface names; non-object: the carried description
  subset; JUSTIFIED/BLANK WHEN ZERO/ALIGNED/DYNAMIC LENGTH join `DescriptionMismatch` as those clauses start
  crossing INVOKE — documented approximation, same status as today's 0828/0829 checks).
- rule 4 (:12347) RETURNING presence; rules 5/6 returning descriptions with the 5a/5b2/5c2 covariant
  allowances (:12351-12379) — the ONLY place the check is looser than equality; a conformant-but-unequal
  return sets `NeedsAdapter` on the (class, interface-member) pair for the emitter.
- rule 8 (:12397) OPTIONAL/OMITTED — joins with the OMITTED slice; rule 9 RAISING (:12399-12418) — joins with
  the EC-OO slice (PD RAISING is 0899-staged today, `DataBinder.Oo.cs:146-148`). Rule 7 strongly-typed groups
  — moot until TYPEDEF lands. "Same entry conventions" — one convention exists in this compiler; trivially true.
- §11.8.4 GR2 (:13319-13325) closes the implements set: direct IMPLEMENTS + interfaces INHERITed by an
  implemented interface + interfaces implemented by an inherited class — compute the transitive closure per
  class before checking; the C# base-class emission gives (c) for free at the C# level, but the CHECK must
  still run per class so the diagnostic names the right class.

**Interface INHERITS.** `INTERFACE-ID … INHERITS FROM {interface-name-2}…` (§11.6.2 :13168-13170) maps to C#
interface inheritance `interface I1 : I2, I3` — C# supports the repetition natively, so implement it even
though it is one of the three optional items (:40408): reject nothing, and check §11.6.3 SR5 (:13184 — a
method inherited from several interfaces must be mutually conforming) and SR3/SR6 (:13180/:13186 — no cycles,
no duplicates) at pass-1 with 0836. This is deliberately ASYMMETRIC with the class-side single-inheritance
restriction (SSOT §18.18) — class `: Base` is a C# limit; interface lists are not.

**Rejected alternatives.**
- *Roslyn-only satisfaction* — wrong both directions (above); also violates G4 (binder owns conformance
  diagnostics for both backends — the future CIL backend gets no Roslyn check at all).
- *Conformance-pass-only without emitting C# interfaces* — loses the interface-typed field/dispatch story:
  `USAGE OBJECT REFERENCE interface-name` would have to fall to `CobolObject` + `__CobolInvoke` string
  dispatch (unidiomatic, defeats Roslyn typing, drags in the universal wave early).
- *A marker/registry-based runtime conformance table* — conformance checking is COMPILE-time except for
  object views/universal (§9.3.8.1 NOTE :12291); no runtime machinery is warranted here.
- *Depth-sorting interfaces before classes at emission* — unnecessary; Roslyn resolves declaration order
  (same reason the Cecil depth-sort died — `CSharpEmitter.Call.cs:137-141`); emit interfaces first purely
  for readability, source order within.

## D-I2. IMPLEMENTS placement and the FACTORY leg

`IMPLEMENTS` rides the FACTORY. and OBJECT. paragraph headers (factory-definition :12755, instance-definition
:12765, §11.8.2 :13305), NOT the CLASS-ID (the dead sketch `CobolParserOO.g4:42` puts it on classAttribute —
spec-WRONG like its `invokeOnException`; do not copy). Interface names in IMPLEMENTS must be REPOSITORY-declared
(§11.8.3 SR1 :13310) — enforced 0836 once the REPOSITORY INTERFACE specifier parses (below).

**Instance IMPLEMENTS** → the C# base list + the conformance pass + adapters (D-I1).
**Factory IMPLEMENTS** → conformance-pass-VALIDATED ONLY (0837 on violation), with NO C# interface emission
for the factory side: factory methods emit as `static` (deep-dive D7) and static members cannot implement C#
instance interfaces. A `USAGE OBJECT REFERENCE FACTORY OF …` reference (the only consumer of a factory
object's interface, :22681-22682) is not in the grammar yet — the whole FACTORY-OF/ONLY/ACTIVE-CLASS usage
tail stays staged (0899 naming the universal-reference/EC-OO waves). ⚑ OWNER-FLAG (record, not blocking): if
factory-interface polymorphism is ever demanded, the choices are C#11 static-abstract interface members vs a
per-class factory singleton object; neither is needed for the corpus or the conformance suite now.

## D-P1. PROPERTY clause → binder-SYNTHESIZED accessor method symbols; the emitter renders direct field bodies that are observably identical to the spec's implicit methods

**Chosen design.** A `PROPERTY [WITH NO {GET|SET}] [IS FINAL]` clause (§13.18.42.2 :21146-21148) on an
instance (or factory) WS item synthesizes, at class-data-bind time (inside `DataBinder`/`OoBindClassData`,
between `BindDeclarations` and `BindResolve` — the same window `OoBindMethodData` uses,
`CSharpEmitter.Oo.cs:48-64`), one or two `OoMethodSymbol`s on the class roster with a new
`PropertyAccessor { None, Get, Set }` discriminator + `PropertyName`:
- GET (unless WITH NO GET): `Returning` = a synthesized LINKAGE-shaped clone of the subject's description
  minus PROPERTY/VALUE/REDEFINES (GR1 :21170-21229 — the clone rule is EXPLICIT at :21225-21229); no formals.
- SET (unless WITH NO SET): one formal with the same cloned description, no RETURNING (GR2 :21231-21281).
The emitter renders synthesized accessors as direct bodies — GET: `return <field-read>;`, SET:
`<field-write>(param)` — instead of a `__MDispatch` frame. This is observably identical to the spec's implicit
`MOVE data-name TO LS-data-name / GOBACK` (:21214-21222) because the two descriptions are IDENTICAL by
construction, and MOVE identical→identical is a straight copy; the edited-category `(1:)` refmod trick
(:21204, NOTE 1 :21209) exists precisely to force a raw copy — which a direct field copy IS. Object/index/
pointer classes use SET semantics (:21175-21191) = reference assignment — also a straight copy in C#.

An EXPLICIT `METHOD-ID. GET|SET PROPERTY prop-name` (§10.6 :12810-12814; §11.7.2 :13220-13231) binds as a
normal method (full body, dispatcher, everything slice 2 built) but registers under the SAME accessor
identity. Checks: §11.7.3 SR6 (:13250) GET = no USING + exactly one RETURNING, not ACTIVE-CLASS; SR7 (:13252)
SET = exactly one USING + no RETURNING; §11.7.3 SR5 (:13248) a WS data-name that is a property-name must not
ALSO carry the PROPERTY clause (duplicate accessor → 0838); §13.18.42.3 SR4 (:21159) the subject's data-name
must not collide with a superclass property (walk `Base` chain rosters); §8.4.3.9.3 SR7 (:7388) when both
accessors exist, GET's returning description == SET's using description (`DescriptionMismatch` again → 0838).

**The subject, and where each §13.18.42.3 rule lives (kb/Work PB1273).** `BindEntry` records the written clause
on the ITEM (`DataItem.Property`, a `PropertyClauseSpec`), and `OoBindPropertyClauses` synthesizes from the object
/ factory working-storage items that carry it — never from a name lookup, which bound the first same-named item.
SR1 (only factory / instance working storage) is screened in `BindEntry`, because its axis is the source element;
SR3's elementary half and §13.16.3 SR21 are rows of the data-clause placement table; SR2 (subject to OCCURS in any
format, by ancestry — `RecordLayout.OccursSubjectOf`), SR3's qualification half, SR5 (CONSTANT RECORD requires
WITH NO SET) and SR6 (no ACTIVE-CLASS object reference) are asked in `OoBindPropertyClauses` (COBOLNET2563); SR4
keeps 0842.

**Naming (§11.7.4 GR1a :13271 — "implementor-defined"): PIN the implementor definition here:** the emitted C#
names are `__GET_<SANITIZED-PROP>` / `__SET_<SANITIZED-PROP>` (uppercase-sanitized like `CsName`). The `__`
prefix is the established cannot-collide-with-COBOL-derived-names rule (`DataBinder.Oo.cs:151-161`); the
deterministic scheme makes cross-class override (a subclass redefining a property accessor — the NOTE at
:21161) and C# interface satisfaction of `GET PROPERTY` prototypes fall out of the existing
`OverrideOf`/roster machinery unchanged. Record this pinned choice in the deep-dive's D-section when
implementing. Property accessors participate in override detection and 0829 signature validation exactly like
named methods (§11.7.3 SR9 :13264 routes property conformance through §9.3.8.2.3 — same pass as D-I1).

**Rejected alternatives.**
- *Textually synthesizing the spec's implicit COBOL method and re-binding it* — a second parse/bind pipeline
  for four generated statements; the direct-body emission is provably equivalent (identical descriptions ⇒
  copy) and keeps synthesized accessors out of the pc space.
- *Emitting C# properties (`public virtual long PROP { get; set; }`)* — attractive-looking but wrong:
  accessors must be independently overridable/final-able (PROPERTY IS FINAL; explicit GET + implicit SET
  mixes), must carry `ref`-style SET marshaling symmetric with methods, and must satisfy interface members
  generated from `GET PROPERTY` prototypes whose C# shape is a METHOD. Methods keep ONE signature machinery
  (feedback_one_mechanism_per_job).
- *v1-restricting PROPERTY to non-edited categories* — unnecessary; the direct-copy argument covers all
  categories the marshaling already carries; categories not yet crossing INVOKE stage loud via the existing
  `DescriptionMismatch` default arm.

## D-P2. Object-property REFERENCES (`prop OF obj`) — bind-time desugar to the EXISTING BoundInvoke over a synthesized temp (GR1–GR3), never an expression-embedded call

**Grammar: NO new syntax needed for access.** `property-name-1 OF {object-class-name-1 | identifier-1}`
(§8.4.3.9.2 :7357-7363) is textually a qualified data reference; `dataReference` already parses `A OF B`. The
work is binder-side: when normal qualification resolution FAILS and the qualifier resolves to (a) a typed
object-reference item or (b) a class name, and the head word is a property of the declared class's (or its
factory's) roster, bind a property reference. §8.4.3.9.3 SR1 (:7376) additionally requires the property-name
to appear in a REPOSITORY property-specifier (`PROPERTY prop [AS lit]`, §12.3.8 :14727-14729) — enforce as
0839 (strict; a dialect-leniency row can relax later if a corpus demands it). SR2 (:7378): the identifier
shall not be universal or NULL → 0839.

**Binding (the implicit INVOKE, §8.4.3.9.4 GR1–GR3 :7393-7397).** Bind-time lowering into bound facts (this
is NOT a lowered IR — it reuses the existing `BoundInvoke` node and the D6 marshaling exactly as the spec
words it: "as though the associated get property method were invoked, in accordance with the rules of the
INVOKE statement"):
- Sending occurrence → synthesize a compiler temp `DataItem` cloned from the GET method's returning
  description (temp-1, GR1), prepend `BoundInvoke(Instance, receiver, __GET_P, Returning: temp)`, and the
  statement binds over the temp's Place. GET must exist → else 0839 (SR3 :7380).
- Receiving occurrence → temp-2 (GR2), statement writes the temp, append
  `BoundInvoke(Instance, receiver, __SET_P, Args: [temp])`. SET must exist → else 0839 (SR4 :7382).
- Send+receive in one reference (e.g. `ADD 1 TO PROP OF O`) → ONE temp serving both (GR3: temp-2 redefines
  temp-1): get→temp, statement reads+writes temp, set(temp).
- `prop OF Class-name` → the factory GET/SET (SR3/SR4 "or in the factory object") → a static call on the
  class (factory-slice call form); stages 0899 until FACTORY lands.
Wrap the pre/statement/post triple in a new `BoundSequence(IReadOnlyList<BoundStatement>)` node (trivial for
both backends to render; add to `StatementBinder` results where a property operand was seen). The temp is a
local of the emitting method/program body (`FieldEmitter.RootDecl` shapes it, uid from the unit's band) —
uniquified `__prop<N>_<NAME>`.

**Rejected alternatives.**
- *A `PropertyPlace` whose `Read()` returns the call expression* — a Place read twice in one emitted
  statement would invoke the GET twice (observable if the method has effects); GR1 says ONE invocation per
  reference. The temp discipline makes once-only structural.
- *Restricting property access to MOVE/DISPLAY* — spec scoping violation (SR5/SR6 :7384-7386: the property is
  valid wherever a data item of that description is a sending/receiving item); the temp desugar makes every
  statement work without touching per-statement emitters.

**Realization notes.** Implemented exactly per this design, with these details:
(1) the diagnostic band is **0843** (0836 is held by the FACTORY wave; the family:
SR1 missing REPOSITORY specifier, SR2 universal receiver, SR3 sending-without-GET, SR4
receiving-without-SET — SR3/SR4 checked AGAINST THE CLASSIFIED POLARITY, so a WITH NO SET property
remains readable);
(2) the sending/receiving classifier is **`Binding/Bound/BoundStores.StoreKindOf`** — a TOTAL explicit
taxonomy over every BoundStatement returning None/Write/ReadWrite (GR1/GR2/GR3 selection), built from the
15-agent emitter-verified survey (scratchpad `bound_stores_classification.md`: 119 nodes, polarity per
store position — in-place arithmetic = ReadWrite, GIVING/MOVE = Write, STRING Into = ReadWrite per GR7,
CALL BY REFERENCE = ReadWrite, etc.); an unclassified node → 0843 LOUD, never a guess about whether a
side-effecting accessor runs;
(3) the hook lives at ReferenceResolver's resolution-failure exit and returns the TEMP ITEM into the
normal resolve tail (so reference-modification on the property value rides the existing RefModPlace path,
and subscripts reject on the OCCURS-less temp);
(4) the drain is `StatementBinder.OoWrapPropertyOps` at the ONE BindStatement chokepoint with
mark-on-entry/drain-own-suffix discipline (a reference in an IF condition belongs to the IF, not an arm);
(5) `prop OF Class-name` → the FACTORY accessors via the Factory call form (LIVE);
(6) the temp joins `DataBinder.Roots` post-resolve (FieldEmitter declares it; uid from the unit band;
`__prop<uid>_<PROP>`); GROUP-valued properties stage 0899 (temps over a group description — later);
(7) invocation-count semantics are GOLDEN-proven: oo_property_explicit_ref's side-effecting accessors
print exactly SET-CALLED then GET-CALLED for a receive-then-send pair (GR2 no-get / GR1 no-set);
oo_property_ref exercises all three GR forms over synthesized accessors (the first RUNTIME exercise);
oo_property_factory_ref proves the factory form. Interface GET/SET PROPERTY prototypes remain 0899
(the interface-property leg rides the universal-reference/EC-OO waves).

---

## Grammar additions (exact rules; each token/rule lands INCREMENTALLY with a full guard-fast run — the LL-regression lesson, deep-dive line 375 / feedback_grammar_version_factoring; OO-phase grammar work is ratified by the roadmap, log per feedback_grammar_preauthorized)

**Lexer** (`src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4`; `INTERFACE_ID`:125 and `FINAL`:358 already
exist): add `IMPLEMENTS : 'IMPLEMENTS';`, `PROPERTY : 'PROPERTY';`, `GET : 'GET';`, `OVERRIDE : 'OVERRIDE';`,
`INTERFACE : 'INTERFACE';` (END INTERFACE / repository specifier). ALL FIVE are 2002+ reserved words that
were user-definable at '85 (§8.9): after adding each token, sweep the corpus; any hit → add the token to the
`cobolWord` fallback alternatives (`CobolParserCore.g4:25-40` — the RAISE/RAISING/RESUME pattern, mirrored in
the lexer `_dataNameTokens` set) plus a `user-word-<w>-2002` interval registry row (the
`user-word-raising-2002` shape, `ConstructDialectStatus.cs:84`). `GET` is the likeliest '85 collision — check
it FIRST, budget for the fallback.

**`Core/CobolOO.g4`:**
```antlr
interfaceDefinition                       // §11.6 :13157; §10.6 :12783-12796 — NO data division at top level
    : (IDENTIFICATION DIVISION DOT)? INTERFACE_ID DOT interfaceName
      (INHERITS FROM interfaceName+)? DOT
      environmentDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END INTERFACE interfaceName DOT ;
interfaceName : cobolWord ;
implementsClause : IMPLEMENTS interfaceName+ DOT ;    // :12755/:12765/:13305 — its OWN trailing period
propertyName : cobolWord ;
```
- `objectParagraph` (`CobolOO.g4:36-41`): `OBJECT DOT implementsClause? …`; same on `factoryParagraph`.
- `methodDefinition` (`CobolOO.g4:43-49`) header becomes (per :12810-12814):
```antlr
    : (IDENTIFICATION DIVISION DOT)? METHOD_ID DOT
      ( methodName | (GET | SET) PROPERTY propertyName )
      OVERRIDE? (IS? FINAL)? DOT
      … END METHOD methodName? DOT ;
```
  (`AS literal` stays deferred, as today. Folding OVERRIDE/[IS FINAL] in here also closes the documented
  SR4a leniency — `OoClassTable.cs:210-217` — and §11.7.3 SR2/SR8 :13238/:13262 reject them in prototypes
  → 0836. If schedule demands, OVERRIDE/FINAL may land as its own micro-slice first; the rule shape is the same.)

**`Core/CobolData.g4`:** add to `dataDescriptionClause`:
`propertyClause : {is2002()}? PROPERTY (WITH? NO (GET | SET))? (IS? FINAL)? ;` (§13.18.42.2 :21146-21148 —
spec has `WITH NO`; accept optional WITH per the repo's IS?-style tolerance).

**`CobolParserCore.g4`:** `compilationGroup` (:123) → `(programUnit | {is2002()}? classDefinition |
{is2002()}? interfaceDefinition)+`; `repositoryEntry` (:427-431) gains
`| {is2002()}? INTERFACE interfaceName` and `| {is2002()}? PROPERTY propertyName` (AS-literal tail stays
deferred like the CLASS entry's).

**Dead sketch warning.** `CobolParserOO.g4` is reference-ONLY and spec-wrong twice over for this slice:
`invokeOnException` (:147) and IMPLEMENTS-on-CLASS-ID (:42). Take nothing from it.

## Files + seams (implementation map)

1. `src/Cobol.Net.Compiler/Binding/OoClassTable.cs` — `OoInterfaceSymbol` (Name/CsName/Ctx/Inherits list/
   prototype roster of `OoMethodSymbol`); `_byName` stays ONE namespace for classes+interfaces (a class and
   interface sharing a name → 0836; `usedCsNames` already guards the emitted-type collision); `Build()` takes
   the interface contexts too; prototype-shape checks (§10.6.2 SR4 :12852-12864: linkage-only data division,
   header-only PD → 0836); `OoClassSymbol.Implements` (+ factory list) with the §11.8.4 GR2 closure;
   `ValidateImplements(edition)` per D-I1, called from `CallEmitRunUnit` right after
   `ValidateOverrideSignatures` (`CSharpEmitter.Call.cs:90`) — i.e. after ALL formals resolve. Extend
   `DescriptionMismatch`'s ObjectReference arm to interface names + the rule-5 covariance overload
   (`ReturningConformance(proto, impl) → Conformant | ConformantNeedsAdapter | Mismatch(msg)`).
2. `Binding/DataBinder.Oo.cs` — `OoBindMethodData` learns prototypes (bind LINKAGE + header formals, no
   body); property-clause scan of the OBJECT/FACTORY WS entries → `OoPropertySymbol` + synthesized accessor
   `OoMethodSymbol`s (the description-clone per :21225-21229); §13.18.42.3 SR1-6 checks (:21153-21165 — WS of
   factory/instance only, no OCCURS subject, elementary + unqualified-unique, superclass collision, CONSTANT
   RECORD staged 0899, no ACTIVE-CLASS). The 0813 declared-class check accepts interface names.
3. `Binding/Bound/StatementBinder.Oo.cs` — `OoBindInstanceInvoke` accepts an interface-declared receiver
   (roster lookup over the interface + its INHERITS closure — SR4e :28417); property-reference fallback +
   `BoundSequence` desugar (D-P2); `InvokeForm` unchanged (interface dispatch IS the Instance form).
4. `CodeGen/CSharpEmitter.Oo.cs` — extract the signature builder from `OoEmitMethod` (:142-150) into a shared
   helper; `OoEmitInterfaceUnit` (members = signatures only); class header base-list append; synthesized
   accessor bodies (D-P1); explicit-interface adapters for `NeedsAdapter` pairs; `sealed`/`override` for
   FINAL/OVERRIDE attributes now that they parse (D7 mapping table, deep-dive :109-115).
5. `CodeGen/CSharpEmitter.Call.cs` — `CallCollectUnits` (:163-188) also collects
   `group.interfaceDefinition()`; `CallEmitRunUnit` emits interfaces before classes (:140).
6. `Validation/EditionValidator.cs` + `ConstructDialectStatus.cs` + `tests/version-matrix/constructs.json` —
   rows (all `IntroducedIn: 2002, EditionCodes.Introduction`): `interface-id-2002` (§11.6), `implements-2002`
   (§11.8/§11.4), `property-clause-2002` (§13.18.42), `method-property-selector-2002` (§11.7),
   `method-override-final-2002` (§11.7), `repository-interface-2002`, `repository-property-2002` (§12.3.8).
   Parse-gated rules get the W1.5 parse-layer 0900 mapping (the `repository-class-2002` pattern, :117).
7. Runtime: NO changes (`CobolObject.cs`/`MethodReturn.cs` untouched — interfaces/properties are compile-time
   constructs; dispatch and null-guard machinery already exist).

## Diagnostics (new band — 0830-0835 are TAKEN by INITIALIZE, `StatementBinder.Initialize.cs:67-114`; 0836-0839 are free)

- **COBOLNET0836** — interface/attribute structural: duplicate interface name or class/interface collision;
  END INTERFACE mismatch (§10.7); unknown/cyclic/duplicated INHERITS FROM interface (§11.6.3 SR2/SR3/SR6);
  SR5 multi-inherit method incompatibility; prototype-shape violations (§10.6.2 SR4); OVERRIDE/FINAL in a
  prototype (§11.7.3 SR2/SR8); IMPLEMENTS naming a non-REPOSITORY interface (§11.8.3 SR1).
- **COBOLNET0837** — IMPLEMENTS conformance (§9.3.11 via §9.3.8.2.3): missing method; each numbered-rule
  mismatch with the rule cited in the message (the 0829 wording style).
- **COBOLNET0838** — PROPERTY declaration: §13.18.42.3 SR1-SR6; §11.7.3 SR5/SR6/SR7; §8.4.3.9.3 SR7 get/set
  description mismatch; duplicate accessor (explicit + implicit, or two explicit) for one property-name.
- **COBOLNET0839** — object-property reference: unknown property / not REPOSITORY-declared (SR1); universal
  or NULL receiver (SR2); no GET when sending (SR3) / no SET when receiving (SR4, incl. WITH NO SET/GET).
- 0900/0902 via the registry rows at `--std 85`. Parameterized interfaces and classes (INTERFACE-ID /
  CLASS-ID USING, REPOSITORY EXPANDS) are no longer residue — LANDED by kb/Work PB759 as expansion-by-re-parse
  (`COBOLNET_OO_DESIGN.md` D12; COBOLNET2239/2240). 0899 staged-loud residue:
  `FACTORY OF`/`ONLY`/`ACTIVE-CLASS` usage tails, interface-typed RAISING conformance (EC-OO slice),
  CONSTANT RECORD properties, factory-interface-typed references.

## Tests (verify, never scope)

Conformance goldens `tests/conformance/2002/` (+ manifest enablement, CorpusRunnerTests): `oo_interface.cob`
(one interface, one implementing class, INVOKE through an interface-typed ref); `oo_interface_poly.cob` (two
classes implement one interface; an interface-typed ref holds each in turn — runtime dispatch);
`oo_interface_inherit.cob` (interface INHERITS; implementer must satisfy the closure);
`oo_property.cob` (PROPERTY clause; `MOVE x TO P OF O`, `DISPLAY P OF O`, `ADD 1 TO P OF O` — the GR3
one-temp case); `oo_property_methods.cob` (explicit GET/SET PROPERTY pair with logic in the body, proving the
implicit-INVOKE path). Adversarial unit tests (`tests/Cobol.Net.Tests.Conformance/OoSpineTests.cs` style,
one per diagnostic): the **PIC 9(4)-vs-9(8) implements case (0837)** — THE case Roslyn cannot catch; the
**covariant-return implements case** — must COMPILE (adapter emitted) and run; missing-method 0837;
WITH NO SET write → 0839; GET with USING → 0838; get/set description mismatch → 0838; property on OCCURS →
0838; superclass property-name collision → 0838; property overridden in a subclass dispatches virtually
(the trap-#1/#2 analog for accessors); duplicate/cyclic interface → 0836; prototype with a WS section → 0836.
Version-matrix negatives: every new construct at `--std 85` → its 0900 diagnostic (constructs.json rows in
the same commit — the drift test enforces this). Full battery + `scripts/guard.sh` green before each commit;
DEVLOG + deep-dive updates (new D-sections for D-I1/D-I2/D-P1/D-P2, the pinned accessor-name choice, and
flipping the two open-questions bullets at deep-dive :375-376 to SETTLED) ship in the same change set.

## Owner-decision flags

1. **None blocking.** The deep-dive's open question (line 376 — Roslyn-checked vs explicit conformance pass)
   is answered by correctness, not preference: Roslyn satisfaction is provably insufficient in both
   directions (D-I1), so the explicit pass is the only conforming design. Recorded decisions to ratify in
   passing: (a) the pinned implementor-defined property-method naming `__GET_/__SET_` (§11.7.4 GR1a);
   (b) interface INHERITS repetition SUPPORTED (optional per §A.4.10 but free in C#) while class INHERITS
   stays single (SSOT §18.18) — an intentional asymmetry; (c) factory IMPLEMENTS validated-not-emitted until
   a factory-interface-typed reference is demanded (future: C#11 static-abstracts vs factory singleton — the
   one genuine future owner choice).

