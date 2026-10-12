## Entry 455 — Phase B1: OO MULTI-METHOD classes + INVOKE SELF (the keystone)

The keystone OO slice: a `CLASS-ID` may now host **multiple `METHOD-ID` units**, each binding as its own .NET
instance method, and `INVOKE SELF "m"` calls a sibling method. This unblocks real multi-operation classes (and the
path to FACTORY). Empirically the three blockers were: `CBL3104` (two methods each with a `MAIN` paragraph collide),
only `methods[0]` emitted (`COBOL0600: class has no method 'DRIVE'`), and `COBOL0112` (SELF rejected).

**Per-method paragraph scoping (fixes CBL3104).** Each `METHOD-ID` gets its OWN paragraph scope, so sibling methods
may reuse paragraph names and a method's `PERFORM`/`GO TO` resolves method-locally:
- `SemanticBuilder.VisitMethodDefinition` creates a per-method `Scope` and records `(name, scope, using+returning)`
  in `ClassMethodScopes` (exposed on `SemanticModel`); `VisitProcedureDivision` pushes that scope inside a method;
  `VisitParagraphDefinition` declares a method's paragraphs ONLY in the method scope (NOT also in the program-wide
  `ProcedureDivisionScope` — that would collide across methods AND overwrite `DeclaringScope`).
- The two OTHER paragraph-resolution passes were made method-aware the same way: `ReferenceResolver` (the CBL3120
  PERFORM/GO-TO-target check) and `BoundTreeBuilder` + `ProcedureNameResolver` (via a new
  `BindingContext.ResolveParagraphScoped` — method scope first, then program-wide). Missing any one re-surfaced the
  failure (CBL3120, then COBOL0402, then the bound builder).

**Per-method roster + emit.** `IrModule.ClassMethods` (new `IrClassMethod{Name, EntryParagraphIndex,
LastParagraphIndex, UsingParameterNames}`) is built by the Binder by grouping `ParagraphDispatchOrder` by each
paragraph's `DeclaringScope` (= its method scope). `CilEmitter` emits the methods in two phases: signatures BEFORE
paragraph bodies (so an `INVOKE SELF` inside a body resolves a sibling, and the exact COBOL names are reserved so
paragraph methods uniquify around them), then bodies. Each public method dispatches its OWN range
`Dispatch(entry, last)` — exit-bounded at the method's LAST paragraph (NOT −1), so **method A cannot fall through
into method B** (guarded by `OoTests.Invoke_MultiMethod_FirstMethodDoesNotFallIntoSecond`). Paragraph Cecil names are
uniquified (`UniqueMethodName`) since two methods' `MAIN` can't share a MethodDefinition; `_methodMap` keys by
IrMethod identity so dispatch is unaffected.

**INVOKE SELF (lifts COBOL0112).** `CallBinder` builds a `BoundInvokeStatement{IsSelf}` (receiver = `this`, args via
the same path, COBOL0111 still loud for unsupported arg forms); `EmitInvoke` resolves the method on `_programType`
(the current class + its INHERITS chain) and emits `ldarg.0` + **`callvirt`** (virtual — an override in a subclass
wins, unlike SUPER's non-virtual `call`).

**Verified:** `oo_self` (COUNTER.DRIVE calls `INVOKE SELF "BUMP"` twice over a shared per-instance typed `N` →
`N=2`) + `oo_self_polymorphic` (an INHERITED base method's `INVOKE SELF "SOUND"` dispatches `callvirt` to the
subclass override → `WOOF`) — both new conformance tests; `OoTests` gets the two positives + a fall-through gate;
the old COBOL0112-FailsLoudly test is inverted. All existing single-method OO tests stay green (each is a 1-entry
roster).

**3-agent adversarial panel (the established gate) — ran probes + IL-disasm; confirmed the core correct AND found
real edge defects, all addressed before commit:**
- **SILENT (the dangerous one):** a method with a paragraph followed by a SECTION truncated the dispatch range to
  `[first..first]` and SILENTLY skipped the section's paragraphs (sections weren't method-scoped). **Fixed:** a
  SECTION inside a METHOD now fails loudly — **COBOL0116** (method-scoped sections are a later slice).
- **RUNTIME CRASH:** a multi-method class with parametered methods cross-wired LINKAGE (`FindLinkageField` is
  module-level → conflates sibling methods' LINKAGE sections → a zero-length param buffer). Per-method LINKAGE is a
  genuine later slice, so **reject loudly — COBOL0117** (single-method-with-params still works, e.g.
  `oo_method_args`). `OoTests.Method_WithSection_FailsLoudly` + `MultiMethod_WithParams_FailsLoudly` guard both.
- **Confirmed correct (panel ran them):** multi-paragraph methods, mid-roster exit boundaries, within-method GO TO
  incl. backward, method-local PERFORM (cross-method PERFORM rejected CBL3120), explicit GOBACK, the
  UniqueMethodName collision handling, no regression to normal programs / sections / duplicate-paragraph-across-
  sections (output-identical with vs without the change).
Still deferred (fail-closed COBOL0600, not silent — no clean diagnostic path in lowering/emit yet): passing a
typed object-data field by reference as an INVOKE arg, and INVOKE to an unknown method (typo). NEXT: per-method
LINKAGE (multi-method-with-args) + typed-field INVOKE args; subclass own typed OBJECT data (lift COBOL0113); FACTORY.

