# WiseOwl COBOL — Pipeline & Emitter Architecture (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §2; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

The WiseOwl COBOL compiler is a 6-phase pipeline: Frontend (reused ANTLR preprocess/lex/parse of the SUPERSET grammar — one grammar recognising the union of all editions → parse tree) → Bind (edition-AGNOSTIC (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1): resolve symbols + build a typed/categorized BOUND TREE that PRESERVES COBOL structure — zero edition checks) → VersionConformancePass (the ONE edition-gating funnel over the bound tree; the driver HALTS before emit when any diagnostics exist — canonical mechanism doc: `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`) → Desugar (bound→bound passes: MOVE CORR, PERFORM VARYING AFTER, condition-names) → Emit (reachable only on a clean tree; codegen behind `ICodeGenBackend` over the ONE backend-neutral bound tree, selectable `--backend roslyn|cil`, default **roslyn**: the RoslynBackend renders idiomatic C# via a decomposed emitter, NO lowered IR) → Roslyn compile → assembly + .g.cs (+ PDB). A CilBackend (Mono.Cecil) is future-additive behind the same interface with its OWN private structure→branch lowering. Daily discipline: ALL semantics live in the binder/bound tree; emitters only RENDER — bound nodes never carry pre-rendered C#-specific fragments where a structured form is feasible (SSOT §18 decision 23).

TWO MAKE-OR-BREAK DECISIONS (both validated against owner-locked constraints):

1) NO lowered IR. Emit C# DIRECTLY from a bound semantic tree. Rationale: a basic-block/SSA IR exists in the legacy ONLY because it targets CIL (which lacks if/while/switch/try/GC/exceptions); C# has all of them, so lowering to branches and reconstructing structure both wastes work AND destroys the idiomatic readable output the owner requires. Reject the current direct parse-tree walk (CSharpEmitter is already failing: Resolve handles only unqualified refs, RenderCondition falls back to "false", scale/category re-derived per call-site). The bound tree is THE single model (feedback_one_mechanism_per_job); DataBinder/DataItem is already its data half.

2) Control flow = a SINGLE program-counter DISPATCHER per program/method (owner-locked: "paragraphs/sections are LABELS (PC cases) in one flow, NOT separate methods"). Realize as (see SSOT §5.1/§14.6): `int pc = startPc; while ((uint)pc < (uint)N) { bool atExit = pc==exitPc; switch (pc) { case 0: ...; pc = 1; break; } if (atExit && pc == exitPc+1) return pc; }`. Fall-through = `pc = i+1; break;` GO TO = `pc = target; break;` PERFORM range = a RECURSIVE bounded `Dispatch(start,exit)`; STOP RUN throws `StopRun` (unwinds ALL Dispatch frames in ALL programs, caught at run-unit `Main`); GOBACK/EXIT PROGRAM throws a DISTINCT `ProgramReturn` (caught at the program's `Entry`). This ports the legacy's PROVEN return-address dispatch SEMANTICS (DEVLOG 259-260) but in the single-dispatcher SHAPE, not the legacy methods-returning-PC shape. Critical synergy: paragraphs become pc INDICES, not C# identifiers — an entire identifier-collision class disappears.

Plus: a decomposed emitter (mirror the legacy CilArithmetic/Comparison/ControlFlow/Data/Expression/String split, shared EmitContext) so it never becomes a god class; a name-allocator that segregates namespaces by prefix (COBOL data d_, temporaries __t, generated names can NEVER collide); a binder scope-tree for multiple/nested/contained programs (GLOBAL/COMMON/EXTERNAL); a DIFFERENTIAL conformance harness (run legacy + CobolNet on each NIST program, assert identical stdout — turns 364 passing legacy tests into an instant regression net); ONE typed edition carrier (EditionInfo) threaded from CLI to the driver — the grammar parses the superset, the binder is edition-agnostic (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1), and edition gating + flagging lives in the single VersionConformancePass; and the loud-failure invariant: bind success ⇒ emit MUST produce compilable C#; any Roslyn error on generated code is an ICE, never silent.

## Decisions

### D1. Introduce a BOUND SEMANTIC TREE (resolved symbols + typed/categorized expressions + structure-preserving statement nodes) and emit C# directly from it. Do NOT introduce a lowered basic-block/SSA IR. The [IR?] in the architecture diagram resolves to: no separate IR layer; the bound tree IS the model.

**Rationale.** The target language is C#, which natively has if/while/for/switch/try/using, GC, and exceptions. A lowered branch-based IR exists in the legacy compiler ONLY because its target (CIL) lacks those constructs. Lowering COBOL structure to branches and then reconstructing it into idiomatic C# is both wasted work and actively destroys the readable output the owner mandates ('idiomatic, readable C# where the construct allows'). A bound tree lets MOVE/IF/EVALUATE/PERFORM map almost 1:1 to their C# equivalents. It also gives ONE place to resolve qualified names (OF/IN), subscripts, ref-mod, and condition-names (resolve once, store on the node) and ONE place to hang semantic diagnostics — exactly what the current parse-tree-walk emitter cannot do.

**Rejected alternatives.** (a) Direct parse-tree walk (the current CSharpEmitter): already empirically failing — Resolve() handles only unqualified/unsubscripted refs, RenderCondition() returns 'false' for class/sign/condition-name conditions, scale+category are re-derived ad hoc at every call site, and there is no semantic-diagnostic seam. Rejected: re-resolves the same symbol N times and cannot scale to qualified names. (b) A lowered IR like the legacy IR/ (IrModule/IrInstruction/basic blocks): rejected because it is the very thing being escaped — it presupposes a CIL target and destroys structure.

**Dual-backend note (SSOT §18 decision 23).** 'NO lowered IR' means no SHARED lowered-IR pipeline phase: the backend-neutral bound tree is the single model behind `ICodeGenBackend` (`--backend roslyn|cil`, default roslyn). The future-additive CilBackend performs its OWN private structure→branch lowering INSIDE the backend — an implementation detail of that one backend, never a phase the RoslynBackend consumes.

### D2. Realize ALL control flow as a single program-counter dispatcher per program/method (the dispatcher IDEA is owner-locked; the concrete SHAPE is below — canonical: SSOT §5.1/§14.6 + `COBOLNET_CONTROL_FLOW_DESIGN.md`).

> **The dispatcher SHAPE** is a **`pc`-variable + bounded `while` loop with a PRE-body `atExit` check** (SSOT §14.6):
> `int pc = startPc; while ((uint)pc < (uint)N) { bool atExit = pc==exitPc; switch (pc) { … pc = next; break; } if
> (atExit && pc == exitPc+1) return pc; } return pc;`. **Why not `goto case`:** it cannot express PERFORM-THRU exit
> detection (no clean named-exit boundary — the `atExit`-captured-before-the-body check is needed because the body
> overwrites `pc`), and C# forbids `goto` *into* another switch section. **GOBACK is NOT "return the current Dispatch
> level"** (SSOT §14.5): a C# `return` exits only the innermost recursive `Dispatch`, so a GOBACK nested inside a
> PERFORM would resume the PERFORM caller, not the program's caller — GOBACK/EXIT PROGRAM throw a distinct
> `ProgramReturn` caught at the program `Entry`; STOP RUN throws `StopRun` caught at run-unit `Main`. This shape is
> implemented in `DispatchEmitter.EmitDispatcher` (G4 ✅).

**Rationale.** This is OWNER-LOCKED ('paragraphs/sections are LABELS (PC cases) in one flow, NOT separate methods. GO TO sets the PC; fall-through is PC++; PERFORM range is a recursive bounded dispatch'). It is the ONLY shape that correctly expresses GO TO, ALTER, and PERFORM THRU-with-an-internal-GO-TO. It ports the legacy's PROVEN return-address dispatch SEMANTICS (DEVLOG 260: a return-address model, not a physical-range model; follows each paragraph's next-pc anywhere, returns only when the exit paragraph falls off its end — handles inverted/non-contiguous THRU ranges per ISO 14.9.30) without re-deriving them. ALTER is the tie-breaker FOR switch-on-pc over goto/labels: ALTER mutates a GO-TO target at runtime, trivial when the target is a real `pc` variable, awkward in pure labels. Bonus: paragraphs become pc indices/enum constants, so paragraph names NEVER become C# identifiers — a whole collision class vanishes.

**Rejected alternatives.** (a) The current G1 scaffold (Main calls each paragraph-method in sequence, catch StopRun): CANNOT express GO TO, ALTER, or PERFORM THRU with a mid-range GO TO. It is a throwaway scaffold, to be replaced wholesale, not extended. (b) A literal port of the legacy CilControlFlowEmitter shape (paragraph-methods-returning-PC + a Dispatch loop calling MethodMap[target]): forbidden by the owner ('NOT separate methods'). (c) Pure C# goto/labels with no pc variable: defeated by ALTER (runtime-mutable target) and by the owner's explicit 'PC cases' wording. Kept in the back pocket ONLY as a LATER 'pretty output for reducible/well-behaved flow' pass (the task's 'idiomatic where the construct allows'), never v1.

### D3. Decompose the emitter into focused collaborators over a shared EmitContext (CodeWriter + bound model + NameTable + DiagnosticBag + EmitConfig — no edition state: emitters carry no edition gating): CSharpEmitter (orchestrator), DataEmitter (fields/record structs/arrays/VALUE init), DispatchEmitter (the pc-dispatcher + PERFORM/GO TO/ALTER), StatementEmitter (MOVE/arithmetic/IF/EVALUATE/STRING/INSPECT/DISPLAY/ACCEPT), ExpressionEmitter (arithmetic/scale-tracking → CobolNum calls), ConditionEmitter (relational/class/sign/condition-name/abbreviated), and a ProgramEmitter (class shell, multiple/nested programs). Mirror the legacy Emission/ split, which already proved this decomposition viable.

**Rationale.** The current single CSharpEmitter is ~800 lines and already over the god-class line; the legacy CilEmitter reached 2458 lines BEFORE it was split into 9 collaborators + an EmissionContext — direct evidence of where this goes unmanaged. A shared EmitContext (not inheritance) is the proven seam (legacy EmissionContext). This satisfies the commercial-quality/decades-sustainable north star and feedback_change_the_dispatch_not_the_callers.

**Rejected alternatives.** (a) Keep one growing CSharpEmitter: rejected — empirically becomes a 2000+ line god class (legacy proof). (b) Visitor-pattern double-dispatch on bound nodes: viable but heavier than needed; the switch-on-bound-node-type with per-category collaborator methods is simpler and matches the legacy idiom the team knows.

### D4. A single NameAllocator owns C#-identifier generation. It (1) normalizes case-insensitively (COBOL FOO==foo collide), (2) SEGREGATES namespaces by prefix — COBOL data 'd_', paragraphs are pc indices (no name), generated temporaries '__t', dispatcher locals fixed reserved set (pc, __dispatch) — so generated names can NEVER collide with COBOL-derived ones, (3) allocates a name ONCE per bound symbol and STORES it on the symbol, (4) disambiguates duplicates deterministically per C# scope (suffix _2, _3). Groups become nested record structs, so duplicate data-names in different groups don't collide at all.

**Rationale.** The current DataItem.Sanitize is NOT collision-safe: hyphen→underscore is non-injective (A-B and A_B both → A_B), it is case-sensitive (misses FOO/foo), it does not dedup duplicate data-names, and FILLER synthetics (_filler0) can collide with a real data-name FILLER-0. The current code ALSO computes paragraph method names in two places (CollectParagraphs + MethodOf) — the exact two-sources-of-truth bug pattern the legacy hit and solved with UniqueMethodName. Allocate-once-store-on-symbol is the cure. Prefix segregation makes generated-vs-COBOL collisions structurally impossible.

**Rejected alternatives.** (a) Current per-call Sanitize: rejected — not collision-safe, computed in multiple places. (b) Escape every COBOL name with @ and hope: rejected — does not solve case-folding or hyphen non-injectivity or duplicate names. (c) Hash-suffix every name: rejected — destroys readability (the owner's stated value); deterministic _N suffixing on actual collision keeps clean names for the common no-collision case.

### D5. Model multiple/nested/contained programs as a binder SCOPE TREE: each program (incl. nested/contained) → its own C# class; the program nest gives lexical visibility. GLOBAL data/files are visible to contained programs (lexical); COMMON programs are callable by siblings; EXTERNAL storage → a static shared field (run-unit lifetime). An in-compilation CALL to a known program → a direct C# call where statically resolvable, else a by-name call through the runtime registry. The emitter walks ALL program units, not FirstOrDefault.

**Rationale.** The current emitter uses `FirstOrDefault()` and silently ignores every program but the first — wrong for any multi-program or nested-program source (and the NIST IC suite is built on inter-program CALL). The scope-tree is the natural home for GLOBAL (ISO 8.4.2/14)/COMMON/EXTERNAL visibility rules, which are inherently lexical/scoped. The legacy already implements cross-program GLOBAL FD, IS INITIAL, and EXTERNAL shared storage — mine its behavior. Stating the full model now (even if implementation is staged G2→G7) prevents a redesign when CALL/nested programs land.

**Rejected alternatives.** (a) One program per compilation (current): rejected — cannot compile a source with nested or contained programs at all. (b) Flat list of programs with no scope tree: rejected — GLOBAL/COMMON visibility is intrinsically a lexical-nesting question; a flat list cannot answer 'is this GLOBAL item visible here'.

### D6. Build a DIFFERENTIAL conformance harness: parameterize the test runner over a compiler-under-test abstraction (Legacy vs CobolNet); for each NIST program run BOTH and assert identical stdout; ALSO keep the .txt-oracle path (assert vs tests/nist/valid/*.txt). New post-85 features keep the tests/conformance/<ver>/ .cob+.out auto-discovery (feedback_goldens_ship_with_the_feature). Point a new CobolNet-specific harness at the corpus; parallelize emit+Roslyn+run (reuse the proven guard-fast parallelism).

**Rationale.** The legacy passes 364 NIST programs, so it is a PROVEN oracle. A differential harness turns the ENTIRE corpus into an instant regression net for CobolNet with ZERO hand-written expectations, and keeps the legacy-as-oracle role productive right up to cut-over (G8). The .txt oracle remains the ground truth so a shared bug in both compilers cannot hide. This directly serves G5 ('drive NIST to green'). The harness's THIRD leg is the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md`, Phase 0 done): each construct case from `docs/VERSION_CHANGE_REFERENCE.md` is compiled at all four `--std` editions and asserted to either compile with that edition's behavior or be rejected with the correct diagnostic — the per-edition negative corpus the NIST-85 suite cannot provide.

**Rejected alternatives.** (a) Only the .txt oracle: rejected — loses the free 364-program diff net and the legacy's edge-case coverage. (b) Only the differential diff: rejected — a bug present in BOTH compilers would pass; the .txt oracle backstops it. (c) Hand-write CobolNet expectations: rejected — enormous, error-prone, and redundant with the existing corpus.

### D7. Thread exactly ONE typed edition carrier (EditionInfo: Cobol85/2002/2014/2023 + the strictness axis, NOT a bare int) from CLI → driver. The grammar parses the SUPERSET of all editions; the binder is edition-AGNOSTIC (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1); edition gating AND flagging — not-yet-introduced, removed/obsolete, extension diagnostics — lives in the single VersionConformancePass over the bound tree. Keep the legacy two-axis model (version vs strictness) rather than invent a new one.

**Rationale.** A bare int has no place for the strictness axis or the flagging behavior that FlaggingConformanceTests exercises. A single typed edition carrier held by the driver (one source of truth) matches feedback_one_mechanism_per_job and the documented project_dialect_two_axes two-axis model. Concentrating gating in ONE bound-tree pass — never scattered parse-time predicates or binder-embedded checks — keeps the grammar a superset, keeps the binder edition-agnostic, and gives every edition diagnostic one funnel with full bound-node context (a parse-time reject at a stuck token misattributes plain typos as edition errors). Flagging (e.g. an OBSOLETE construct used under --std 2023) is a conformance-pass diagnostic, not a parse error. The only surviving parse-time predicates are the two load-bearing forward-detects (the openClause `{is2002() || retryPhraseAhead()}?` and the `boolExprAhead()`-based boolean-condition ENTRY) — identity-carrying disambiguation, not edition gates. Emit is unreachable when any diagnostics exist (the driver runs bind → pass → HALT on errors → emit).

**⛔ "SUPERSET" IS ACROSS EDITIONS, NEVER ACROSS THE PRINTED GENERAL FORMATS (kb/Work PB412, PB421).** D7 licenses ONE widening and only one: a construct some edition lacks still PARSES, so the VersionConformancePass can name the edition instead of the user getting a stuck-token syntax error. It is NOT a licence to write a statement rule as the UNION of its clause's general formats with every element optional and leave the complement — every shape no format prints — to be answered downstream. §4.2.2's first paragraph fixes what may be ACCEPTED ("An implementation shall accept the syntax and provide the functionality for all standard language elements required by this Working Draft International Standard and the optional or processor-dependent language elements for which support is claimed"), and a construct no general format prints is neither; §4.2.2's second paragraph then obliges the compiler to be able to indicate violations of the general formats. So a statement rule carries **one alternative per printed general format**, plus an explicitly adjudicated arm where an edition-gated legacy form needs to be named rather than refused (`goToStatement`'s ANSI-85 target-less arm is the one such arm today). Where the shape question then has to be asked in C#, it is asked ONCE — `CobolNet.Frontend.Parsing.GoToFormats` is that classifier for GO TO — never re-derived from optional children at each reader. `PrintedFormatAlternativeDriftTests` holds both halves true from the spec transcription and the grammar text, and `CobolErrorStrategy` re-codes the resulting syntax error so the diagnostic names the format (COBOLNET2172, COBOLNET2173) rather than the token the parser stopped on. Two defects taught this: `goToStatement` was `GO TO? procedureName? (procedureName)* (DEPENDING ON? dataReference)?`, so `GO TO A B.` bound its first target and DISCARDED the rest in silence; `moveReceivingPhrase` re-spelled MOVE's Format-2 keyword, so `MOVE A CORRESPONDING G1 TO G2.` compiled clean and died at run time on a D8 loud stage — the loud stage used as a parser's error handler, for source the standard has no format for.

**Per-edition gating (the four-compilers-in-one mission, G1).** Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Gating is driven by `docs/VERSION_CHANGE_REFERENCE.md` — the 130-row edition-change checklist (2002→2023 deltas ONLY — it has NO 85→2002 rows; derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog) — and validated by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` — the (construct × edition) matrix; Phase 0 done). Default `--std` is COBOL-2023; `--nist` without an explicit `--std` targets 85.

**Rejected alternatives.** (a) Bare int DialectLevel: rejected — no strictness axis, no flagging seam. (b) A new dialect model: rejected — the legacy two-axis (version × strictness) model is already designed, documented, and tested; reuse it. (c) Parse-time `{isXXXX()}?` edition predicates + binder-embedded edition checks: rejected — they scatter one job (edition gating) across phases against feedback_one_mechanism_per_job, fork the grammar per edition, and re-diagnose typos as edition errors at the stuck token; the sole edition gate is the VersionConformancePass.

### D8. Establish the LOUD-FAILURE invariant as a stated design contract: (1) an unbound or unsupported construct emits a TRACKED DEFERRAL DIAGNOSTIC (e.g. COBOLNET-defer-XXXX), never a silent `// TODO` no-op; (2) IF bind succeeds, emit MUST produce compilable C# — any Roslyn error on the generated code is an INTERNAL COMPILER ERROR (ICE), surfaced with the .g.cs path, never reported as a user error.

**Rationale.** The current EmitStatement emits `// TODO` and CONTINUES for unsupported verbs — so a NIST program 'compiles and runs' while silently doing nothing. That is precisely the silent-miscompile the project's whole COBOL0111-0117 'fail LOUD' culture exists to prevent (feedback_spec_is_the_oracle, the adversarial-panel discipline). Making 'bind-success ⇒ compilable-emit' an invariant turns every Roslyn error into a caught ICE, which is itself a growth-discipline (it forces emit to keep pace with bind). Program.cs already surfaces the .g.cs path on backend failure — this just makes it the stated contract.

**Rejected alternatives.** (a) Silent `// TODO` continue (current): rejected — produces silently-wrong programs that pass a naive 'it ran' check, the worst failure mode. (b) Throw on first unsupported construct: rejected for the bring-up phase — a tracked deferral diagnostic lets a partially-supported program still compile+run its supported parts (useful while coverage grows) while making the gap LOUD and counted.

### D9. Assembly/runtime deploy: target an EXPLICITLY PINNED TFM — net10.0 today; the .NET 11 / C# 15 upgrade is pre-authorized whenever its features advance the goals (owner directive feedback_north_star_commercial_quality: bump the pin deliberately) — not Environment.Version-by-luck; one ConsoleApplication assembly per compilation with the main program as entry point; emit a PDB and map sequence points back to the .g.cs so COBOL→C#→step-through is debuggable; write the .g.cs next to the assembly; reference assemblies from the TPA set + CobolNet.Runtime.

**Rationale.** The current RoslynBackend derives the runtimeconfig TFM from Environment.Version, which is fragile (it happens to be 10.0 today). The owner directive is an explicit net10.0 target. The PDB+.g.cs mapping delivers the architecture doc's stated 'debuggable, inspectable' value — currently the .g.cs is written but not debug-mapped. One assembly per compilation matches the COBOL run-unit model.

**Rejected alternatives.** (a) Environment.Version TFM (current): rejected — implicit, breaks if the compiler is ever run on a different runtime. (b) No PDB: rejected — loses step-through debugging of generated code, a stated value. (c) Per-program assemblies: an open question (see openQuestions), default is one assembly per compilation.

## C# mapping

PIPELINE TYPES (namespaces `CobolNet.*`, in the projects `src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}` — see `COBOLNET_PROJECT_ORG_DESIGN.md`):
- CobolNet.Frontend.Frontend (exists) → parse tree.
- CobolNet.Binding: DataBinder/DataItem/PicInfo (exist, data half) + NEW ProcedureBinder → a BoundProgram tree.
- CobolNet.Binding.Bound (NEW): BoundProgram, BoundProcedure, BoundParagraph (carries int PcIndex), BoundStatement subtypes (BoundMove, BoundArith, BoundIf, BoundEvaluate, BoundPerform{Single|Thru|Times|Until|Varying}, BoundGoTo (resolved target PcIndex), BoundAlter, BoundStopRun, BoundGoback, BoundCall, BoundInvoke, ...), BoundExpression subtypes (BoundFieldRef (resolved DataItem + subscripts + refmod), BoundLiteral, BoundBinary, BoundCondition...). Mine legacy Semantics/Bound/ for WHICH nodes; build CobolNet's own.
- VersionConformancePass (over the bound tree, between Bind and Desugar): the ONE edition-gating funnel — bound nodes carry a `.Syntax` back-reference it reads; the driver order is bind → pass → HALT on errors → emit.
- CobolNet.Binding.Desugar (NEW): bound→bound passes (MoveCorrLowering, PerformVaryingAfterLowering, ConditionNameLowering). NOT a second tree type.
- CobolNet.CodeGen: CSharpEmitter (orchestrator), + DataEmitter/DispatchEmitter/StatementEmitter/ExpressionEmitter/ConditionEmitter/ProgramEmitter, all sharing EmitContext; CodeWriter (exists), RoslynBackend (exists), NameTable/NameAllocator (NEW).

CONTROL-FLOW MAPPING (the centerpiece):
COBOL:
  PROCEDURE DIVISION.
  A. DISPLAY "A". 
  B. IF X = 1 GO TO D.
  C. DISPLAY "C".
  D. DISPLAY "D". STOP RUN.
C# (the pc-variable + bounded-while shape — as implemented in DispatchEmitter.EmitDispatcher, G4 ✅):
  static void Main(){ try { __Dispatch(0, -1); } catch (StopRun) { } }
  const int __N = 4;
  static int __Dispatch(int __startPc, int __exitPc){
    int __pc = __startPc;
    while ((uint)__pc < (uint)__N) {
      bool __atExit = __pc == __exitPc;                                       // captured BEFORE the body overwrites __pc
      switch (__pc) {
        case 0: Console.WriteLine("A"); __pc = 1; break;                      // A: fall through
        case 1: if (CobolNum.Cmp(d_X, 1) == 0) { __pc = 3; break; }           // B: GO TO D
                __pc = 2; break;
        case 2: Console.WriteLine("C"); __pc = 3; break;                      // C
        case 3: Console.WriteLine("D"); throw new StopRun();                  // D
        default: __pc = __N; break;
      }
      if (__atExit && __pc == __exitPc + 1) return __pc;                      // a named THRU exit paragraph fell off its end
    }
    return __pc;
  }
PERFORM A THRU C  → __Dispatch(0, 2);   (recursive bounded dispatch; returns when para 2 falls off its end — DEVLOG 260 return-address model, handles inverted ranges)
PERFORM P 3 TIMES → for (long i=0;i<3;i++) __Dispatch(idxP, idxP);
GOBACK            → `throw new ProgramReturn();` (caught at the program Entry — NOT a C# `return`, which would only exit the innermost recursive __Dispatch; SSOT §14.5); STOP RUN → `throw new StopRun();` (unwinds all levels, caught at run-unit Main).
ALTER X TO PROCEED TO Y → a mutable target var: the GO TO at X emits `pc = _alter_X; continue;` and ALTER emits `_alter_X = idxY;` (trivial because pc/target is a real variable — the decisive reason for switch-on-pc).
DECLARATIVES/USE: declarative paragraphs occupy their own pc indices but Main starts at EntryParagraphIndex (first non-declarative, ISO 14.4); a USE handler is reached only via a Dispatch(declStart, declEnd) call, never main fall-through.

DATA MAPPING (already established by DataItem/PicInfo, kept): PIC X→string, PIC 9/S9→long (unscaled, scale=metadata), scaled/COMP-3→long unscaled too (the implied decimal point is compile-time Scale metadata; 19–38 digits → Int128 via WidePrecision — NO decimal/BigInteger, owner lock; SSOT §4), COMP-1/2→float/double, group→nested record struct, OCCURS→T[], POINTER→ManagedPointer, OBJECT REFERENCE/class→.NET class. CobolNum/CobolString runtime substrates carry truncation/ROUNDED/SIZE ERROR.

NAME ALLOCATION: NameAllocator.ForData(DataItem)→"d_"+normalize; generated temps "__t0.."; reserved {pc, exitPc, startPc, Main, Dispatch}; paragraphs get no identifier (pc index). Stored on the bound symbol once.

PROGRAM NESTING: each BoundProgram→`internal [static] class <Prog>`; nested/contained → nested classes; GLOBAL items emitted in the container, referenced by name from contained scopes; EXTERNAL → `static` shared field; CALL "X" → direct call if X in compilation, else runtime by-name.

EDITION: EditionInfo held by the driver; the VersionConformancePass is its sole gating consumer — the frontend parses the superset, the binder and emitters are edition-agnostic, and emit is unreachable when any diagnostics exist.

HARNESS: CobolNetCompiler { (ok,stdout,detail) CompileAndRun(src) } at a chosen edition; NistDifferentialTests asserts CobolNet stdout == nist/valid/*.txt, and the *DifferentialTests compare with the committed goldens under tests/differential/.

## Hard problems

### GO TO that EXITS a PERFORM range (control transfers out of the bounded dispatch and never returns to the PERFORM site).

The recursive Dispatch(start,exit) is a RETURN-ADDRESS model, not a physical-range model (legacy DEVLOG 260). A GO TO sets pc to a target that may be OUTSIDE [start,exit]; the switch follows it anywhere. The bounded level returns ONLY when the EXIT paragraph completes by falling off its end (`if (exitPc == k) return;`). A GO TO that lands outside the range and never falls through the exit paragraph means control legitimately left the PERFORM — the dispatch keeps running until it hits STOP RUN (throw, unwinds all levels) or another exit. This exactly mirrors ISO 14.9.39 semantics and the legacy's proven helper. Cite DEVLOG 260.

### Inverted / non-contiguous PERFORM A THRU B where B physically PRECEDES A (ISO 14.9.30 permits it: enter at A, return when B falls off its end).

Do NOT swap into a contiguous [min,max] block (the legacy's original bug that executed the wrong paragraphs, NC102A 'RETURN MECHANISM LOST'). The switch covers the FULL paragraph table; Dispatch(idxA, idxB) enters at idxA, follows fall-through/GO TO anywhere, and returns only when paragraph idxB falls off its end. The pc-variable model makes this free. Cite DEVLOG 260 + ISO 14.9.30.

### DECLARATIVES/USE procedures must NOT be reached by main fall-through, only via the exception/error dispatch, yet they share the same pc index space.

Port the legacy fix (DEVLOG 259): ONE index space over ALL paragraphs INCLUDING declaratives (so every pc value agrees), but Main starts Dispatch at EntryParagraphIndex (first paragraph after END DECLARATIVES, ISO 14.4). Declarative paragraphs occupy low pc indices in the switch but are unreachable by the main loop; a triggered USE handler calls Dispatch(declStart, declEnd). The off-by-N bug (dispatch order excluding declaratives while pc values included them) is the precise trap to avoid.

### Duplicate paragraph names across different SECTIONs (legal in COBOL) — name-based GO TO resolution disagrees with index-based dispatch.

Resolve every control transfer by a paragraph SYMBOL (scope-qualified), not by name (legacy DEVLOG 260). The binder assigns each paragraph a distinct PcIndex and resolves a (possibly section-qualified) GO TO target to the correct duplicate's PcIndex at bind time, storing it on BoundGoTo. The emitter never does name lookup. A half-measure (symbol dispatch + name GO TO) regresses — they must move together.

### Qualified data references (X OF Y OF Z), subscripts (T(I,J)), and reference modification (X(p:l)) — pervasive, and the current Resolve() handles none of them.

Resolve entirely in the binder: a BoundFieldRef carries the resolved DataItem (found by walking the group tree for OF/IN qualification), the resolved subscript BoundExpressions, and optional refmod (offset,length) BoundExpressions. Because groups → nested record structs, a subscript is a C# array index and OF-qualification is C# member access (a.b.c) — mostly free. Ref-mod on a string → a substring/Span helper; on a numeric → materialize the display image then slice (the deferred whole-group-alphanumeric case, G6). Resolve ONCE, store on the node.

### EVALUATE (the COBOL case statement) with multiple WHEN subjects, TRUE/FALSE subjects, THRU ranges, and ANY — does not map cleanly to a C# switch.

Bind EVALUATE to a BoundEvaluate with a list of subjects and WHEN branches each holding per-subject selection objects (value/range/TRUE/FALSE/ANY/condition). Emit as an if/else-if chain (each WHEN = the AND of its per-subject comparisons), NOT a C# switch (C# switch cannot express multi-subject AND, THRU ranges, or condition subjects). This is correctness-first; a real C# switch is a later 'pretty' optimization for the single-subject value-only case.

### Multiple/nested/contained programs with GLOBAL/COMMON/EXTERNAL visibility and CALL resolution.

Binder scope tree (program nest = lexical scope). GLOBAL data declared in a container is visible to contained programs (emit it in the container class, reference by name). COMMON programs are callable by siblings. EXTERNAL storage → a static shared field (run-unit lifetime), keyed by name. CALL 'LIT' to a program in the compilation → a direct C# method call; CALL identifier or to an unknown name → a runtime by-name dispatch (a registry). Mine the legacy's cross-program GLOBAL FD / IS INITIAL / EXTERNAL behavior. Cite ISO 8.4.2, 14.

### Scale-correct fixed-point arithmetic with ROUNDED (8 modes) and ON SIZE ERROR, on native long/Int128 (NO decimal/BigInteger substrate, owner lock — SSOT §4: the Int128-monomorphic `CobolInt` carrier; storage stays the narrow native type).

The ExpressionEmitter tracks (expr, scale) pairs (the existing NumX pattern is the right idea) and routes every store through CobolNum.Store(value, sourceScale, NumProfile) which applies the receiver's PICTURE truncation, ROUNDED mode, and SIZE-ERROR detection. ON SIZE ERROR becomes a tryStore-returns-bool + a guarded block. 19-38 digit pictures → Int128 (owner lock); the NumProfile/CobolNum must be Int128-aware. This is a runtime-subsystem detail but the pipeline must thread the profile to every arithmetic emit site (one chokepoint).

### The loud-failure invariant under partial feature coverage during bring-up: how to compile+run the supported parts while making gaps loud, without silent no-ops.

An unsupported bound construct emits a tracked deferral diagnostic AND emits a runtime guard (throw new NotImplementedCobolFeature("...")) rather than a silent `// TODO`. So the program still compiles (invariant: bind-success ⇒ compilable C#), but executing the unsupported path fails LOUD at runtime with a precise message, and the deferral is counted in diagnostics. Any Roslyn error on emitted C# is treated as an ICE (the .g.cs path is surfaced), never a user error.

## Edge cases

- FILLER synthetic names (_filler0) must not collide with a real data-name like FILLER-0 — the NameAllocator must register synthetics in the same namespace and dedup.
- COBOL case-insensitivity: data-names FOO and foo are the SAME item; the NameAllocator must normalize case before collision detection (current Sanitize is case-sensitive and would emit two C# fields).
- Hyphen non-injectivity: A-B and A_B both sanitize to A_B — must be detected as a collision and disambiguated, not silently merged.
- A COBOL paragraph/method literally named Main, Dispatch, pc, or StopRun — impossible to collide because paragraphs become pc indices (no identifier) and data uses the d_ prefix; document the reserved set anyway.
- Empty PROCEDURE DIVISION / a program with no paragraphs — Dispatch must handle a zero-case switch (emit an empty Main).
- A paragraph that is the LAST in the program and falls off the end with no STOP RUN — implicit GOBACK/program termination at end of PROCEDURE DIVISION (ISO 14.4); the final case must `return;`/throw, not run off the switch.
- PERFORM of a single paragraph vs PERFORM THRU where start==end — both → Dispatch(idx, idx); verify the single-paragraph fall-through-to-exit boundary fires.
- Nested PERFORM and recursive PERFORM (a PERFORM whose range contains another PERFORM of an overlapping range) — recursive Dispatch calls naturally nest; verify the pc/exit locals are per-call (they are, as method parameters/locals).
- GO TO DEPENDING with an out-of-range index → no transfer (fall through), per ISO 14.9.16 — the computed-pc switch must guard the index.
- An ALTERed GO TO before any ALTER executes uses its default target (IrModule.AlterDefaults in the legacy) — the _alter_X variable must be initialized to the default PcIndex.
- DISPLAY with no operands, DISPLAY of a numeric (display image with sign/scale) vs alphanumeric — already partially handled; ensure numeric uses CobolNum.FormatDisplay not raw ToString.
- Roslyn nullable/unused-variable warnings on machine-generated C# — disable nullable context on generated code (RoslynBackend already does) and suppress unused-var noise so warnings-as-errors in the COMPILER project doesn't reject valid generated code.
- A NIST program that reads DATA files / writes report files — the differential harness must run legacy and CobolNet in ISOLATED working dirs (the guard's chain-isolation lesson) so file producer/consumer chains don't cross-contaminate between the two compilers.
- Conditional-compilation (>>IF/>>DEFINE) and COPY REPLACING already handled by the reused preprocessor — verify they run BEFORE binding (they do, in Frontend.Preprocess) and that NIST placeholder substitution still works through the new pipeline.

## ISO citations

- ISO/IEC 1989:2023 §14.4 — execution begins at the first paragraph/section after END DECLARATIVES (drives EntryParagraphIndex; declaratives reached only via USE dispatch)
- ISO/IEC 1989:2023 §14.9.39 (PERFORM) — out-of-line/inline, THRU range, TIMES/UNTIL/VARYING, TEST BEFORE/AFTER (the recursive bounded-dispatch model)
- ISO/IEC 1989:2023 §14.9.30 (GO TO) — including the inverted/non-contiguous THRU range allowance (return when the exit paragraph falls off its end)
- ISO/IEC 1989:2023 §14.9.16 (GO TO ... DEPENDING ON) — out-of-range index falls through (no transfer)
- ISO/IEC 1989:2023 §14.9.1 (ALTER) — runtime mutation of a GO-TO target (the decisive case for the pc-variable model)
- ISO/IEC 1989:2023 §8.4.2 — GLOBAL / EXTERNAL attributes and contained-program visibility (the binder scope tree)
- ISO/IEC 1989:2023 §11.2, §11.7 — OO CLASS-ID / METHOD-ID → .NET class / methods (G7, but the program model must accommodate it)
- ISO/IEC 1989:2023 §8.8.1 / §8.8.4 — arithmetic expression composite-of-operands, intermediate scale, and relation-condition comparison rules (ExpressionEmitter scale tracking)
- ISO/IEC 1989:2023 §14.9.43 (STOP RUN) — run-unit termination (throw StopRun unwinding all Dispatch levels)
- ISO/IEC 1989:2023 §13.18.x — DATA DIVISION clauses (PICTURE/USAGE/VALUE/OCCURS/REDEFINES/RENAMES) governing the data→.NET mapping and the deferred G6 cases

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Diagnostic numbering: reuse the legacy COBOLxxxx / CBLxxxx code scheme for continuity with existing docs/tests, or adopt a fresh COBOLNET-prefixed scheme for the clean-slate compiler? (Affects FlaggingConformanceTests and every diagnostic-asserting test.)
- Is it acceptable for the conformance/CI harness to DEPEND on the legacy compiler as a differential oracle until cut-over (G8)? This keeps the legacy build in the test graph for the duration; the alternative is .txt-oracle-only (loses the free 364-program diff net).
- One .g.cs + one assembly per compilation (current default), or one per program unit when a source contains multiple/contained programs? (Affects CALL linkage — same-assembly direct call vs cross-assembly by-name — and the deploy story.)
- Should the v1 always emit the pc-dispatcher, or should reducible/well-behaved control flow (no GO TO, no ALTER, simple PERFORM) emit idiomatic structured C# (plain method calls / while loops) from the start? The task sanctions correctness-first (dispatcher v1), but the owner values readable output — confirm whether the 'pretty pass' is in-scope for early milestones or strictly deferred.
- For 19-38 digit pictures the owner lock specifies Int128. Confirm the CobolNum/NumProfile runtime substrate (reused) is Int128-capable, or whether that is net-new runtime work that the pipeline design must schedule before such pictures are bound (RESOLVED — SSOT §4: the value engine is Int128-monomorphic via the `CobolInt` carrier; `PicInfo` maps fixed-point numerics to `long`, with 19–38-digit pictures → `Int128` via `WidePrecision`; no `decimal` anywhere.)
