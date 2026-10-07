# WiseOwl COBOL — Consolidated Design (SSOT)

> **Status: LIVE / authoritative.** This is the single, internally-consistent source of truth that the WiseOwl COBOL
> implementation follows. It synthesizes the 11 subsystem designs (pipeline, data-model, numeric, control-flow,
> redefines/renames, string-ops, files, interprogram, OO, conditions/exceptions, intrinsics/registers) into one
> coherent design, reconciling every cross-subsystem conflict. Where two subsystem designs disagreed, the loser is
> named explicitly (see §14, Cross-Cutting Consistency). It refines `docs/COBOLNET_ARCHITECTURE.md` (the thin
> overview) and supersedes any contradicting statement in it (notably the §3 `decimal` rows — see §3.1 below).
>
> **Project:** WiseOwl COBOL — a greenfield compiler translating COBOL → idiomatic, typed-native **C# source**, compiled
> by **Roslyn**. New compiler in `src/Cobol.Net.Compiler` + `src/Cobol.Net.Cli` (exe `cobol`) + `src/Cobol.Net.Runtime`;
> the reused front-end (ANTLR lexer/parser/preprocessor) is extracted into `src/Cobol.Net.Frontend` (§17/G0 — DONE;
> front-end namespaces are `CobolNet.Frontend.*`; the legacy tree keeps its own `CobolSharp.Compiler.*`
> identity until G8, which is a pure *deletion* of that tree). The legacy byte-array implementation is rejected, kept
> only as a differential **behavioral oracle** (it passes 364 NIST tests) until cut-over (G8).

---

## 0. Reading guide

- **§1** Architecture overview + the locked invariants.
- **§2–§13** One section per subsystem: key decisions (with rationale + rejected alternative), the C# mapping, the
  hard problems/edge cases. Condensed — but every real decision survives.
- **§14** The CROSS-CUTTING CONSISTENCY pass: the unified lvalue model, control-flow/declaratives/EXIT interaction,
  REDEFINES × numeric × files, the runtime class roster, and every named conflict resolution.
- **§15** Consolidated OWNER-LEVEL open questions (deduped; the task-named ones verbatim).
- **§16** Dependency-ordered implementation sequence (G1–G8), each step NIST-testable, with the cross-design
  prerequisites surfaced.
- **§17** Project organization / rename / structural discipline (the G0 plan — executed).
- **§18** Settled decisions (the §15 questions, owner-resolved 2026-06-08).

---

## 0.5 Subsystem deep-dive docs

Each subsystem section in THIS document is a **condensed** view kept consistent across subsystems. The full,
decision-complete **deep dive** for each lives in its own doc (decisions + rationale + rejected alternatives + the
C# mapping with worked examples + hard problems + edge cases + ISO citations):

| § | Subsystem | Deep-dive doc |
|---|---|---|
| §2  | Pipeline & emitter architecture | `docs/COBOLNET_PIPELINE_DESIGN.md` |
| §3  | Data model (records/tables/refs) | `docs/COBOLNET_DATA_MODEL_DESIGN.md` |
| §4  | REDEFINES / RENAMES | `docs/COBOLNET_REDEFINES_DESIGN.md` |
| §5  | Control flow (PC dispatcher) | `docs/COBOLNET_CONTROL_FLOW_DESIGN.md` |
| §6  | Numeric model (scaled-integer) | `docs/COBOLNET_NUMERIC_DESIGN.md` |
| §7  | String operations | `docs/COBOLNET_STRING_OPS_DESIGN.md` |
| §8  | Files | `docs/COBOLNET_FILES_DESIGN.md` |
| §9  | Interprogram (CALL/cross-program) | `docs/COBOLNET_INTERPROGRAM_DESIGN.md` |
| §10 | OO → .NET classes | `docs/COBOLNET_OO_DESIGN.md` |
| §11 | Conditions & exceptions | `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` |
| §12 | Intrinsics, registers & misc | `docs/COBOLNET_INTRINSICS_DESIGN.md` |
| §14 | Report Writer (RWCS) | `docs/COBOLNET_REPORT_WRITER_DESIGN.md` |
| §12/§7 | The LOCALE facility (Annex A.4.9 — locale state, collation, classification, monetary editing, the locale intrinsics, STANDARD-COMPARE) | `docs/rearchitecture/DESIGN-locale-facility.md` (adopted 2026-08-18, implemented whole — T6 completed the A.4.9 claim 2026-08-28; owner decisions Q1–Q4 in its header) |
| §7/§12 | The collation ENGINE (the derived CLDR/UCA table, keys, streaming compare, tailoring — the substrate the locale facility's `CobolCollation` carrier and STANDARD-COMPARE run on; kb/Work PB101), the CLDR LOCALE LOADER (every CLDR locale's rules → its table + settings; PB105), the KEY CACHE (PB106), and the runtime's text-processing overview | `src/Cobol.Net.Runtime/README.md` (the overview: the six subsystems and their pipelines, 2026-08-19); `src/Cobol.Net.Runtime/Collation/README.md` (2026-08-18/19; generator `scripts/collation/`, data provenance `data/unicode/SOURCES.md`); `Collation/CLDR/README.md`; `Collation/Cache/README.md`; `Unicode/README.md` (normalization); `Unicode/Segmentation/README.md` (grapheme clusters, PB104) |
| §9/§10 | The §8.13 EXTERNAL REPOSITORY — the compiled assembly's metadata + the `CobolNet.Runtime.Repository` attribute family as the repository; the ONE `RepositoryResolver` (group provider, then metadata provider, documented search order) every REPOSITORY specifier kind resolves through; name-scoped facts recorded as externalized identities; `--repository-update` / `--repository-check` / `--repository DIR` / `--reference`; cross-assembly classes, interfaces, parameterized definitions and expansions; the locator shared with the run-time probe and the deployment contract; the TYPED ACTIVATION BOUNDARY (`Enter<TProto>`/`Leave`, a prototype interface per signature in its carrier, `ref T + bool` parameters, the dynamic door as a generated adapter onto the typed `Entry`, per-assembly namespaces) and the .NET-platform evaluation the owner requested | `docs/rearchitecture/DESIGN-external-repository.md` (adopted 2026-10-05, owner decision kb/Work PB1099 Q3; implementation tracked in `kb/Work/`) |
| §17 | Project organization & rename | `docs/COBOLNET_PROJECT_ORG_DESIGN.md` |

This SSOT stays authoritative for the **locked invariants (§1)**, the **cross-cutting consistency (§14)**, the
**settled decisions (§18)**, and the **build order (§16)**; the deep-dives are authoritative for their subsystem's
internal detail. Where a deep-dive conflicts with §1/§14/§18, the SSOT wins.

---

## 1. Architecture overview + locked invariants

### 1.1 Pipeline (6 phases; edition gating is its OWN pass; a backend-neutral bound tree feeds a SELECTABLE backend)

```
source.cob
  → Frontend  (REUSED ANTLR, SUPERSET grammar: Preprocess[reference-format, >>directives, COPY, NIST placeholders]
               → Lex → Parse; edition {isXXXX()}? gates REMOVED so all constructs parse at every --std; each
               version-gated rule stamps its construct-id — DESIGN-version-conformance-pipeline.md)
       → parse tree
  → Bind      (edition-AGNOSTIC (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1): resolve symbols + build a typed/categorized BOUND TREE that PRESERVES COBOL structure;
               ZERO edition Check calls)
       → BoundProgram
  → VersionConformancePass  (the ONE edition gate: walk the bound tree, Check each stamped construct-id + the few
               semantic gates vs --std; reject strict / accept-inert permissive)  → HALT if errors — no codegen on a bad tree
  → Desugar   (bound→bound passes: MOVE CORRESPONDING, PERFORM VARYING…AFTER, condition-name lowering)
       → BoundProgram (same tree type)
  → Backend   (ICodeGenBackend, SELECTABLE via --backend; reached ONLY on a clean tree):
       ├─ Roslyn (default): decomposed emitter → idiomatic C# (.g.cs) → CSharpCompilation → assembly + PDB
       └─ CIL (Cecil):      bound tree → typed-native CIL (its OWN internal structure→branch lowering) → assembly
```

**Edition conformance is a single dedicated pass** (`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`).
Edition gating is ONE mechanism: the grammar declares construct *identity* (a committed-match annotation local to each
version-gated rule; version *numbers* live only in `constructs.json`), the binder is edition-agnostic (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1), and the
`VersionConformancePass` over the bound tree is the sole gate — rejecting strict / accepting-inert permissive, and
HALTING before emit so codegen never runs on an errored tree. There is no parse-time `{isXXXX()}?` edition predicate,
no post-hoc reverse-signature recognizer, and no edition checks scattered through the binder.

**Backend-neutral bound tree + a selectable codegen backend (owner-confirmed 2026-06-08).** The bound semantic tree
is the single model that both backends consume; codegen is behind an **`ICodeGenBackend`** abstraction with two
implementations, chosen by `--backend roslyn|cil` (default **roslyn**):
- **RoslynBackend (primary):** maps the bound tree's COBOL structure ~1:1 to idiomatic, readable **C# source**
  (`IF`→`if/else`, `EVALUATE`→if-chain, inline `PERFORM`→loops, the PC-dispatcher→`while(true)switch`), then compiles
  it with Roslyn. This is the "best native .NET implementation" output and the v1 deliverable.
- **CilBackend (selectable, future-additive):** emits **typed-native CIL** directly via Mono.Cecil — for callers who
  want no C#-compile step / no Roslyn dependency / direct IL (AOT, faster compile). Because CIL is unstructured, this
  backend does its OWN internal lowering of structured constructs to branches; that lowering is **private to the CIL
  backend, NOT a shared phase**.

**There is NO *shared* lowered IR.** The legacy basic-block IR existed only because its sole target was CIL; here the
backend-neutral bound tree replaces it, and each backend lowers only as far as its target needs (Roslyn: none —
structure is preserved; CIL: branch-level, internally). *(Rejected: a direct parse-tree walk —
resolving only unqualified refs, conditions falling back to `"false"`, scale/category
re-derived per call-site; and a shared branch IR — it would re-impose the CIL-shaped lowering on the C# path and
destroy its readable output.)* Bonus: the differential harness (§2) can cross-check the two backends against each
other, not just against the legacy oracle.

### 1.2 The four OWNER-LOCKED invariants (design *within* these; never relitigate)

1. **No byte-array storage substrate.** A COBOL record IS a .NET `record struct`; an elementary item IS a native
   .NET field. No `ProgramState`, no `byte[]`-at-offset, no `(byte[],offset,length)`. A byte image exists only
   *transiently* at an unavoidable boundary (file I/O / `CODE-SET`, a runtime API that needs bytes, a genuine
   REDEFINES byte-pun), built into a fresh scratch buffer, never persisted as program data.
2. **Numerics are native.** Fixed-point → a native integer holding the **unscaled** value (all digits; the decimal
   point is compile-time scale metadata). ≤18 digits → `long`; 19–38 digits → `Int128`; `COMP-1`/`COMP-2` →
   `float`/`double`; `COMP-5` → native int by width (binary-wrap), and since the item owns its FULL container
   range the carrier is unsigned where a signed one cannot hold it: unsigned 8-byte container → `ulong`,
   unsigned 16-byte → `UInt128` (kb/Work R10; CONFORMANCE.md item 208). **No `decimal`, no `BigInteger`.**
3. **Control flow is a single program-counter dispatcher.** Paragraphs/sections are LABELS (PC cases) in one flow,
   NOT separate methods. `GO TO` sets the PC; fall-through is PC++; a `PERFORM` range is a recursive bounded
   dispatch.
4. **Output is idiomatic, readable C# where the construct allows.** Correctness wins over prettiness for irregular
   control flow (the dispatcher backbone is deliberately a state machine; "idiomatic" applies to the *case
   contents* — `IF`→`if/else`, `EVALUATE`→if/else-if chain, inline `PERFORM`→real loops, typed field ops).

### 1.3 Two universal abstractions (the spine of internal consistency — see §14.1, §14.2)

- **`Place`** — the ONE typed-lvalue model. `Place.Read()`/`Place.Write(rhs)` are the ROSLYN backend's RENDERING of a
  backend-neutral structural resolution (item + member-accessor chain + subscript expressions + ref-mod span + 88
  value-set) — the structure, not C# text, is the bound-tree contract (G4: bound nodes carry no pre-rendered
  C#-specific fragments; the CIL backend lowers the same structure to load/store sequences). Built once by
  `ReferenceResolver`. Consumed identically by MOVE, arithmetic, INSPECT/STRING/
  UNSTRING, file READ INTO / WRITE FROM, and CALL-by-reference. There is no second lvalue type.
- **The PC dispatcher** — one `Dispatch(int startPc, int exitPc)` method per program unit; STOP RUN and GOBACK are
  the only control transfers that use exceptions (everything else is integer-pc).

### 1.4 Loud-failure invariant

(1) An unbound/unsupported construct emits a **tracked deferral diagnostic** + a runtime guard
(`throw new NotImplementedCobolFeature(...)`), never a silent `// TODO` no-op. ⛔ The deferral is for LEGAL
source only. A statement the SOURCE got wrong — a shape no general format prints, an operand its syntax rules
exclude — binds to `BoundRejected`, whose factories record the error before they return (`BoundRejected.Report`)
or put the refusal on the ledger for a callee that reported it (`BoundRejected.Reported`), so a refusal can be
neither silent nor compiled (`StatementEmitter` throws if one arrives). ⛔ **THE REFUSAL LEDGER (kb/Work PB1029).**
Every node that stands for something the binder could not give a meaning to is obtainable only through a factory
that records itself on `EditionContext`: `BoundRejected`, and the operand-level `BoundExprError` /
`BoundOperandError` / `BoundBoolError`, each as `Refused` (the source is wrong; the site reported the rule —
`Report(edition, rule, message, feature)` does both), `Unbuilt` (a legal shape WiseOwl COBOL has not built) or `Carry`
(the same failure moved to another value channel); `BoundConditionError` has its one site in
`ConditionBinder.Refused`. The ONE funnel every statement passes, `StatementBinder.BindStatement`, reads the ledger:
a statement that bound a refusal and drew no error fails the compile with the internal COBOLNET2362 (a refused
condition keeps COBOLNET2319), and a statement that drew no error announces its `BoundUnsupported` or its first
unbuilt operand as the COBOLNET1756 warning. ⛔ **THE RESOLVER'S ANSWER IS CLOSED (kb/Work PB1030).**
`ReferenceResolver.Resolve` / `ResolveForItem` (and `ExpressionBinder.ResolveSending`) return a `RefResolution` —
exactly one of `Place`, `Reported` (the resolver reported the rule: an undefined name, a subscript screen, something in
a subscript position that is not a subscript — COBOLNET2363, a reference to a refused declaration — COBOLNET2364) or
`Deferred` (a legal shape not built, named by one `DeferredShape` census member). The resolver puts every deferral on
the unbuilt ledger before any caller sees it, so a caller that drops the answer cannot make it silent, and a caller
builds its node FROM the answer (`Refusal` / `OperandError` / `ExprError` / `BoolError`; `PlaceOrReported` where the
caller cannot carry a deferral, which reports it as recognized-not-implemented, COBOLNET0899). The receiving
chokepoint `ResolveReceiving` consumes the answer itself and still returns `Place?`, null meaning reported.
`RefResolutionDriftTests` pins the census, the ledger and the callers. `BoundDeferralDriftTests` fails on a
deferral whose message states a violated rule (kb/Work PB909); `RefusalNodeDriftTests` keeps the refusal
constructors private and keeps report-then-`BoundNop` and failure-guarded `BoundNop` out of the binder (a genuine
no-op on a guarded arm is marked `// no-op:`). (2) **Bind success ⇒ emit MUST
produce compilable C#** — any Roslyn error on generated code is an ICE (surfaced with the `.g.cs` path), never a user
error. This is the structural enforcement of the project's "fail LOUD" culture.

---

## 2. Pipeline & emitter architecture

**Decisions.**

- **Bound tree, not IR** (§1.1). The bound tree resolves qualified names (OF/IN), subscripts, ref-mod, and
  condition-names ONCE and stores them on the node — the single place to hang semantic diagnostics.
- **Single PC dispatcher per program unit** (§5 has the full design). `int Dispatch(int startPc, int exitPc)`.
- **Decomposed emitter over an immutable `EmitContext`** (CodeWriter + bound model + `NameAllocator` + EditionInfo +
  DiagnosticBag + EmitConfig; the receiver travels via a `ReceiverContext` parameter, not mutable `Target*` fields):
  `ProgramEmitter` (run-unit) → the `UnitEmitters` per-unit composition root → `DispatchEmitter`/`StatementEmitter`
  (the generated exhaustive `IBoundStatementVisitor<bool>`) + 18 `Verbs/*Emitter` + `EcEmitter` + the `DataDivision/`
  emitters behind `DataEmitter`, plus the `Emit/` renderers (`NumericRenderer`/`ConditionRenderer`/`BooleanRenderer`/
  `IntrinsicRenderer` [single channel since P7 Step 12 — the static string twin is deleted; instance renders via the
  ONE `NumericRenderer` under `ReceiverContext.None`]/`OperandText`/`EmitCore`); `CSharpEmitter` is the bind-host
  facade only until P9. Mirrors the
  legacy `Emission/` split (the legacy `CilEmitter` hit 2458 lines before it was split — direct evidence).
  *(Rejected: one growing `CSharpEmitter` god class.)*
- **One `NameAllocator`** owns C#-identifier generation: case-insensitive normalization (COBOL `FOO`==`foo`);
  namespace segregation by prefix (COBOL data `d_`; temporaries `__t`; dispatcher locals a fixed reserved set
  `{pc, startPc, exitPc, Main, Dispatch}`); **paragraphs get no identifier — they are pc indices**, so an entire
  identifier-collision class vanishes; allocate-once-store-on-symbol; deterministic `_2`/`_3` disambiguation.
  *(Rejected: the current per-call `Sanitize` — not collision-safe: hyphen non-injective `A-B`/`A_B`, case-sensitive,
  no dedup; hash-suffixing — destroys readability.)*
- **Binder scope tree** for multiple/nested/contained programs (GLOBAL/COMMON/EXTERNAL are lexical-nesting
  questions). The emitter walks ALL program units, not `FirstOrDefault()`.
- **Differential conformance harness:** `CobolNetCompiler` compiles and runs each case; `NistDifferentialTests`
  asserts `CobolNet stdout == nist/valid/*.txt` and the `*DifferentialTests` compare with the committed goldens under
  `tests/differential/` (recorded once from the legacy engine, which no test drives since kb/Work PB2108). Reuse
  the proven `guard-fast` parallelism; run each program in an ISOLATED working dir (file producer/consumer chains).
- **One `EditionInfo`** (`Cobol85/2002/2014/2023`) threaded CLI → driver. The grammar parses the SUPERSET of all
  editions and stamps each version-gated construct's construct-id; the binder is edition-AGNOSTIC (save the documented exception ledger, DESIGN-version-conformance-pipeline §1.1); the
  **`VersionConformancePass`** over the bound tree is the sole edition gate — reject strict / accept-inert permissive,
  HALT before emit (§1.1; `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`). The two-axis
  (version × strictness) model applies throughout.
  Default `--std` = COBOL-2023; `--nist` without an explicit `--std` targets 85. ⛔ Per-edition gating is a co-equal
  obligation: each construct must compile + behave per the spec in every edition that HAS it AND draw the correct
  diagnostic in every edition that LACKS it (not-yet-introduced or removed) — driven by
  `docs/VERSION_CHANGE_REFERENCE.md` (130-row edition-change checklist) and validated by
  `docs/VERSION_TEST_MATRIX_DESIGN.md` (the construct × edition matrix).
- **Deploy:** fixed TFM from `Directory.Build.props` — `net10.0` today, with a **.NET 11 upgrade pre-authorized**
  when its features make the goals easier/more productive (owner, 2026-06-10); never `Environment.Version`-by-luck;
  one ConsoleApplication
  assembly per compilation (the run-unit); emit a PDB mapped to the `.g.cs`; write `.g.cs` next to the assembly.

**Hard problems** are all control-flow/data and live in §5/§3. Pipeline-level: disable nullable + suppress unused-var
noise on generated C# so the compiler project's warnings-as-errors doesn't reject valid generated code; verify the
preprocessor (`>>IF`/`>>DEFINE`, COPY REPLACING, NIST placeholders) runs BEFORE binding.

---

## 3. Data model

### 3.1 The substrate (LOCKED — and the one doc correction)

`PicInfo.ClrType` already maps fixed-point → `long`-unscaled (no `decimal`). **The `docs/COBOLNET_ARCHITECTURE.md`
§3 table rows that say scaled/`COMP-3` → `decimal` are SUPERSEDED by this document and by `PicInfo.cs`:** scaled and
`COMP-3` are `long`-unscaled (scale is compile-time metadata); 19–38 digits → `Int128` via a `WidePrecision` flag.
This correction must be applied to the architecture doc in the same change set (no two SSOTs).

### 3.2 Shape mapping

| COBOL | .NET |
|---|---|
| `PIC X(n)` / `A(n)` / `PIC N(n)` (national) | `string` (UTF-16) |
| `PIC 9(n)` / `S9(n)` (≤18 digits, any scale) | `long` (unscaled; scale = metadata) |
| `PIC 9(n)`…(19–38 digits) | `Int128` (`WidePrecision`) |
| `COMP-1` / `COMP-2` | `float` / `double` |
| `COMP-5` / `BINARY-*` | the narrowest of `long`/`ulong`/`Int128`/`UInt128` holding the FULL container range (unsigned 8-byte → `ulong`, unsigned 16-byte → `UInt128`; kb/Work R10), binary-wrap. Value paths (store/DISPLAY/relations) carry the whole range via the runtime's U-lane; arithmetic funnels through `CobolNum.Widen` into the `Int128` intermediate (CONFORMANCE.md items 123/179) |
| numeric-edited (`Z * $ + - CR DB B 0 /`) | `string` (the formatted display image) |
| `PIC 1` / `USAGE BIT` | `bool` |
| `01`/group | nested `record struct` named `_T_<csname>` |
| fixed `OCCURS n` | `T[]` (length n) |
| `OCCURS m TO n DEPENDING ON d` | `T[]` allocated at MAX (n) + the length field `d` bounds the live range |
| `USAGE POINTER` / `BASED` / `ADDRESS OF` | `ManagedPointer` (managed ref — see §14.2 / §9) |
| `USAGE OBJECT REFERENCE [class]` / OO class | `class?` (typed) or `object?` (universal) / a .NET class |

Member access falls straight out: `VAL OF ITEMS(2) OF WS-REC` → `WsRec.Items[2 - 1].Val`.

### 3.3 The `Place` abstraction (the universal lvalue — §14.1)

```csharp
abstract record Place { DataItem Item; PicInfo Pic; /* structural core: accessor chain, subscripts, ref-mod */ }
// G4 note: the string Read()/Write(rhs) shown below are the RoslynBackend's RENDERING of this structure (C# text);
// the CIL backend lowers the SAME structural Place to load/store sequences — no C# fragments live in bound nodes.
// MemberPlace(path)        Read = path                Write = $"{path} = {rhs};"
// RefModPlace(inner,s,l)   Read = CobolString.RefMod(inner.Read(), s, l)
//                          Write = { var t=inner.Read(); inner.Write(CobolString.SpliceInto(t,s,l,rhs)); }
// Condition88Place(parent, valueset)   Read = CobolCond.In(parent.Read(), …)   Write(true) = set parent to value
```

One resolver — `ReferenceResolver.Resolve(DataReferenceContext) → Place` — is the single entry point for every operand.

### 3.4 The two-phase resolver (the grammar forces it)

The grammar gives `dataReference : cobolWord dataReferenceSuffix*`, and subscript/ref-mod content is a RAW
`subToken*` stream (SUBSCRIPT lexer mode); `(I J)` (subscripts) and `(3:2)` (ref-mod) are syntactically identical —
**the presence of `SUB_COLON` decides.** So:

- **Phase A (syntactic flatten):** walk suffixes into `{qualifiers[], subscriptGroups[][], refMod?}`. A `(...)` group
  is a ref-mod iff it contains `SUB_COLON`; else split on `SUB_WS`/`SUB_COMMA` into N subscripts. **Port the legacy
  `ExpressionBinder` SUB_* token interpreter verbatim** (proven over 364 NIST tests).
- **Phase B (semantic resolve):** resolve base + qualifiers via right-to-left narrowing (port `ResolveQualifiedName` +
  `FindChild`), build the member-access Place, attach subscripts to OCCURS levels (outer→inner), wrap in ref-mod if
  present. Subscripts/positions evaluated ONCE into temps (ISO single-evaluation).

### 3.5 Key data decisions

- **Multi-dimensional OCCURS → array-of-struct-containing-array** (`Rows[i-1].Cols[j-1]`), one C# index per COBOL
  subscript (1-based → `[expr - 1]`). NO flattened 1-D buffer, NO `stepSize` multipliers. *(Rejected: the legacy
  flattened-1-D-with-multipliers model — it IS the byte-offset arithmetic the owner abolished.)*
- **Index-names (`INDEXED BY`) → a C# `long` holding a 1-based OCCURRENCE NUMBER**, not a byte displacement. `SET TO`
  → the guarded store; `SET UP/DOWN BY k` → the guarded augment; subscript use → `[idx - 1]`; SEARCH/SEARCH ALL →
  integer loops. That carrier IS the implementor's index range (ISO §13.18.38.4 GR2; `docs/CONFORMANCE.md` §7 row
  `DOC-A.1-128`), so every value that reaches an index goes through `CobolIndex` — the ONE place §14.9.39.4's
  integrality, sign and range guards are written (D3 deep dive). *(Rejected: the legacy byte-displacement index —
  leaks table-element width into a program-visible value.)*
  **Each INDEXED BY DECLARATION owns its cell** — an `IndexDeclaration(name, table, cell)` minted when the table is
  registered (`DataBinder.RegisterIndexes`; cell `_IX_<table uid>[_k]`), held by the scope's `IndexNameRegistry`
  (the unit, a method's §11.7.4 GR5 overlay, and — by §8.4.6.2.3 — a containing program's GLOBAL tables, tagged
  with their nesting depth for §8.4.6.2.1 3)). A WRITTEN index-name, bare or qualified through its table
  (§8.4.2.2.2 Format 3; §8.4.2.2.3 SR6 — `IX OF T`), resolves through ONE resolution
  (`ReferenceResolver.ResolveIndexName` over `SymbolTable.IndexCandidates`) whose candidate count meets the same
  §8.4.2.2.3 SR1 verdict as a data-name (`DataBinder.UniqueOrReportAmbiguous<T>` over `NameCandidates<T>`,
  COBOLNET1639). A table's OWN index (SEARCH's first index-name) is read off `DataItem.Indexes` — a declaration,
  not a reference. *(Rejected: a `name → cell` map de-duplicated on insert — two tables' `INDEXED BY IX` silently
  shared one cell and the qualified form was "not defined", kb/Work PB919.)*
- **Level-88 → C# `bool` properties** over the parent Place (`Ok => St == 1`); SET cond TO TRUE moves the 88's first
  VALUE (low bound of a THRU range) into the parent (ISO §14.9.34/§14.9.39). The binder must **stop skipping 88s**
  (currently dropped) and capture the full multi-literal + THRU value list. *(Rejected: stored bools kept in sync on
  every parent write — redundant state, sync bugs.)*
- **Level-66 RENAMES** folds into the REDEFINES/RENAMES tiers (§4) as a COMPOSED view over existing fields (adds no
  storage). The binder must **stop skipping 66s.**
- **VALUE init** is one recursive object-initializer composed from the leaves, emitted in the static field decl
  (program) or the instance ctor (OO). Extensions: group VALUE, OCCURS VALUE (`Tbl = [.. n elements]`), figurative
  constants (§11), and the **Format 2 (table) VALUE** (§13.18.63.2 — literals keyed to occurrence ranges by a
  mandatory `FROM (subscript)` phrase). Its per-occurrence map (GR12's odometer over the whole subscript tuple,
  GR13 cyclic reuse under TO, GR14 no-TO = fill to the maximum, GR15 later-FROM-wins, GR16's dynamic initial
  capacity) is resolved in the BINDER — `DataBinder.ResolveTableValues`, a post-forest pass that also enforces
  §13.18.63.3 SR18–SR23 — and stored as a `TableValuePlan` on the entry. **`DataItem.ValueAt(subs)` is THE reader
  and BOTH storage lanes take it**: `ValueInitializer.InitializerFor` builds the record-struct array literal and
  `GroupImageCodec.ImageInitOf` composes the per-occurrence character images (kb/Work PB208 — the image lane read
  only `item.RawValue`, which is null for a table VALUE, and repeated ONE occurrence image, so a format-2 VALUE was
  silently discarded for every image-stored leaf). Both lanes thread the OCCURRENCE CONTEXT — the subscripts of
  the OCCURS levels already entered — so the VALUE may sit on an entry SUBORDINATE to the OCCURS (§13.18.63.3 SR18)
  or span several dimensions, and a GROUP entry's table VALUE deposits §13.18.63.4 GR5's area per occurrence
  (kb/Work PB505 — one staged COBOLNET0899 used to refuse all three, conforming source included).
  **Its literals ride the same screen as a format-1 one**: §13.18.63.3 SR2 is an ALL FORMATS rule and SR16 carries
  SRs 10–15 into format 2, so `DataBinder.ScreenTableValueLiterals` calls `DataBinder.ScreenValueLiteral` per
  occurrence-literal at ENTRY BIND — beside the format-1 call site — and stores the text it returns (the
  `--permissive` numeric rewrite therefore reaches the emitter's per-occurrence override exactly as it reaches
  `item.RawValue`). The Format-1
  glued-multi-literal defect is fixed: `DataBinder.ExtractValue` GLUES a bare multi-operand list via `GetText` over
  the collapsed `valueItem` (not "first-only") — a data-item VALUE with >1 operand and no FROM is now rejected
  (COBOLNET1585); 88s bind through `BindCondition`'s own per-operand loop (never `ExtractValue`).
- **A GROUP-level VALUE initializes the AREA, and there is ONE rule that says what the area contains.**
  ISO §13.18.63.4 GR5 — "the group area is initialized without consideration for the individual elementary or group
  items contained within this group" — so the operand's characters are deposited positionally across the group's
  storage (GR7 aligns them per §14.6.8: left-justified, space fill, no editing, JUSTIFIED inert) and each
  subordinate is then whatever its own description makes of the characters under it. That rule is
  **`GroupValueSlicer.AreaOf`**, and it covers all three operand forms — a quoted literal, `ALL "literal"`,
  and a **figurative constant word** — under ONE rule, **§8.3.3.6.4 GR2**: it is the branch for the case "the
  length of the string is specified in the rules for the context", it names the VALUE clause explicitly, and it
  carries both the repeat-to-width and the truncate-from-the-right. (GR3 is the complementary branch — the length
  *not* specified — and its sub-rule a is scoped to a concatenation expression, which is `ConcatFolder`'s rule and
  not this one. §13.18.63.4 GR5 makes the group area the receiving field, so the length is always specified here.
  The fill CHARACTER is GR1's, with the per-format GR4–GR8 identities.)
  TWO lanes consume it and neither may re-derive it: `GroupValueSlicer.ComposedInit` distributes the area into
  the typed-native record-struct fields, and **`GroupImageCodec.ImageInitOf`** — THE seeder for every
  character-image backing (Tier-B REDEFINES, EXTERNAL run-unit cells, BASED/ADDRESS-OF cells, OO backings) —
  emits it as the backing's initial image.
- **The area has TWO UNITS, and the rule reports which one** (`GroupArea(Text, Bits)`; kb/Work PB207). A
  **BIT GROUP**'s area is not made of characters: §13.18.29.4 GR1a makes a `GROUP-USAGE BIT` group "a bit group
  and also a bit data item; its class and category are boolean", and GR1b describes it "as though it were an
  elementary data item of usage bit … described with PICTURE 1(m), where m is the bit length of the group" — so
  GR5 deposits **m boolean positions** laid out by the §8.5.1.6.3 walk, where `DataItem.ImageWidth` reports the
  `ceil(m/8)` characters it OCCUPIES. The two are different numbers and the members do not tile the character
  count (two `PIC 1(2)` members are one character between them and `ceil(2/8)` = one character *each*), which is
  why a positional character slice over such a group crashed the slicer. GR7's §14.6.8 arm follows the unit:
  §14.6.8.6 for a boolean area — "with **zero fill** or truncation to the right" — and §14.6.8.5's space fill for
  every other category. The record-struct lane takes `SliceBitInit` (members placed by
  `BitLayout.StartBitWithin`) and the image lane packs the same area through `CobolBits.Pack`. A group that is
  NOT a bit group but has a `USAGE BIT` descendant returns no area at all: it is never conforming source
  (§13.18.63.3 SR14 requires usage DISPLAY of every item subordinate to an alphanumeric group item carrying a
  VALUE — COBOLNET1702; §13.18.29.3 SR3 requires usage national under a national group), and both lanes stay
  total over that residue.
- **A bit group's IMAGE is its packed area, never its members' images concatenated**
  (`GroupImageCodec.BitAreaOf` — ONE composition, ridden by the generated `AsBits()`/`FromBits`, by the
  compile-time image seed, and by the GR5 arm above). Each member sits where `BitLayout.StartBitWithin` puts it,
  with §8.5.1.6.3's implicit-filler zeros between and after; the width is `BitLayout.ExtentBits`, never a sum of
  the members' widths — a member whose LEVEL NUMBER differs from its predecessor's is not "of the same level" and
  starts at the next byte.
- **The group-level VALUE SYNTAX rules are screened at bind time, in one place**
  (`DataBinder.CheckGroupValueDeclarations` → COBOLNET1702 / COBOLNET1703; ISO §13.18.63.3 SR1 + SR13/SR14, and
  SR16 extends SR13/SR14 to the format 2 table VALUE). SR1 bars a strongly-typed group item or a variable-length
  group from BEING the subject; SR14 forbids a JUSTIFIED or SYNCHRONIZED subordinate **and** requires every data
  item subordinate to an *alphanumeric* group item carrying a VALUE to be explicitly or implicitly usage DISPLAY;
  SR13 sentence 2 forbids a VALUE at subordinate levels within that group. "Alphanumeric group item" is
  §13.18.29.4 GR3's, in FULL — no GROUP-USAGE clause specified or implied, **and** not strongly typed, **and** not
  a variable-length group; dropping the last two qualifiers made SR1's own population answer the SR14 usage arm.
  ⛔ **SR14 is why the area rule needs no character-vs-byte width question**: for the whole conforming population
  every subordinate is usage DISPLAY, so the character image IS the byte image. A COMP/COMP-3/COMP-5/float/INDEX/
  NATIONAL-usage leaf under a group VALUE is NOT conforming source and is rejected — it is not a distribution case
  (kb/Work PB184, which was registered on the opposite premise). The pass runs after `UsageInheritancePass` so
  "implicitly" sees an inherited group USAGE.
- ⛔ **The group-level VALUE's LITERAL is screened by the SAME method every elementary subject uses —
  `DataBinder.ValidateValueCategory` — because the rules are the same rules** (kb/Work PB206). SR4, SR5 and SR10
  say "the item" / "the subject of the entry", never "the elementary item", and §8.5.2.1 gives a group a class and
  category of its own ("an alphanumeric group item has class and category alphanumeric"; "a bit group item …
  boolean"; "a national group item … national"); §13.18.63.3 SR13 sentence 1 — "literal-1 shall be of the same
  category as the group item or shall be a figurative constant that is permitted in a MOVE statement to a
  receiving item of that category" — is those rules restated for a group subject. So the screen takes the
  subject's category PICTURE (`GroupSubjectPic` — §13.18.29.4 GR1b/GR2b's as-if picture for a bit / national
  group, GR3's alphanumeric residue at `ImageWidth` otherwise) and asks the one method. It is NOT
  `DataItem.OperandPic`, which is deliberately null for an alphanumeric group because §14.9.25.4 GR4 makes such a
  group a conversion-free character copy Table 16 does not describe; as the SUBJECT OF A VALUE CLAUSE the group
  has a category outright.
  **Three structural consequences, all of them landed defects:**
  1. `ValidateValueCategory` takes the SUBJECT as a `ValueSubject` descriptor, never `pic.Length` and never a
     size a caller computed. SR4/SR5/SR10 each carry the size sentence TWICE — once for an elementary item ("the
     size indicated by an **explicit** PICTURE clause") and once for a group ("the size of the group item") — and
     a **DYNAMIC LENGTH** (or ANY LENGTH) subject indicates NO size at all:
     its picture is "one instance of the picture symbol 'N', or 'X'" (§13.18.19.3 SR1)
     and "the picture symbol determines the class" (§13.18.19.4 GR1).
     Passing `pic.Length` there REJECTED LEGAL SOURCE — `01 UN PIC N DYNAMIC LENGTH VALUE
     N"SEED".` drew COBOLNET0898 while its alphanumeric twin compiled and ran (pinned by
     `2014/pb206_value_size_categories`).
  2. **The size belongs to the subject KIND, and there are THREE kinds, not two** (kb/Work PB598). Those size
     sentences name an elementary item and a group item; the third subject §13.18.63.3 addresses is a
     CONDITION-NAME (SR33 — "Formats 3 and 5 may be specified only when the level-number of the subject of the
     entry is 88"), and it has no size sentence at all: a Format-3 entry is `88 condition-name-1 value-clause .`
     (§13.16.2), which admits no PICTURE clause, so nothing EXPLICIT indicates a size, and §13.18.63.4 GR19 gives
     a condition-name its conditional variable's characteristics only IMPLICITLY. While the size was an
     `int? sizePositions` parameter the three level-88 call sites each computed the CONDITIONAL VARIABLE's size
     and handed it over, so `01 XV PIC X. 88 XC VALUE "cd".` was COBOLNET1740 — legal source rejected, and a
     GnuCOBOL differential flip. It is now `ValueSubject` (`Binding/ValueSubject.cs`), a descriptor built only by
     `ForElementary` / `ForGroup` / `ForConditionName`, each stating its size rule with its citation: a fourth
     subject kind must add a factory and cannot inherit a size by accident. The CLASS half of SR4/SR5/SR10 is
     unaffected and still binds a condition-name.
  3. The ALPHANUMERIC size arm had **no implementation anywhere** while its national and boolean twins had theirs,
     so BOTH of SR4's size sentences silently truncated (`PIC X(2) VALUE "ABCD"` → `AB`; a 6-character literal on
     a 4-character group → `ABCD`). It is now **COBOLNET1740**, error at every edition — Annex E.2 item 27's 2023
     size change is scoped to NUMERIC-EDITED items (COBOLNET1570), not to this. A FIGURATIVE operand is exempt by
     rule, not omission: §8.3.3.6.4 GR2 repeats it to the subject's size and truncates from the right.
  The one asymmetry is deliberate: the SR4-sentence-1 vendor leniency that stores a numeric literal's digits on an
  alphanumeric subject under `--permissive` is withheld from a group subject, because GR5's area deposit is
  defined over the operand's CHARACTERS and a numeric literal has none (measured: the group seeded SPACES). The
  group ARM is format 1 only, but the SCREEN is not: SR16 carries SR13 to the format 2 table VALUE and
  `ValidateTableValues` puts every per-occurrence literal through this same funnel with a
  `ValueSubject.ForElementary` describing the ELEMENT (kb/Work PB208, landed), so the two formats cannot disagree
  about a category or a size.
- ⛔ **The screen walks `DataBinder.CompositionForest`, and that distinction is a design commitment, not a
  detail.** There are TWO forests over the bound data, and a pass picks by what its rule is ABOUT.
  `ConformanceForest` is the WRITTEN-ENTRY forest — the once-per-source-entry set the per-entry data-attribute
  gates fire over — and it excludes TYPE/SAME AS clone subtrees because the template carried the same clauses and
  was gated once. `CompositionForest` adds them back. A rule whose subject is a property of the **composed** entry
  has no counterpart on the template: `01 R TYPE T VALUE "ABCD".` composes an SR14 violation out of a VALUE the
  reference site wrote and a usage the template wrote, and neither entry carries it alone — that program compiled
  clean on the screen's first landing while its byte-identical inline spelling was rejected. Provenance keeps the
  verdict count honest: `DataItem.ValueIsCopied` marks a VALUE this entry only ASSUMED (§13.18.57.4 GR1 /
  §13.18.49 GR1), so the screen answers once per WRITTEN VALUE clause rather than once per reference site.
- **A staged loud beats a silent wrong answer, and a crash is neither — and a refusal is retired the moment the
  rule is expressible.** The bit-packed group VALUE was the standing example: before it was refused, the
  multi-member shape raised an unhandled `ArgumentOutOfRangeException` out of `GroupValueSlicer.SliceInit` and
  the single-member shape stored ONE boolean position where the literal has four, so it was refused by name
  (COBOLNET0899, `bit-group-level-value`) instead. That area rule is now EXPRESSED — in boolean positions, the
  two bullets above — so the refusal and its descriptor are DELETED (kb/Work PB207); COBOLNET0899 keeps its
  other populations and `bit-group-level-value` is gone. A staged loud is a debt with a name, not a resting
  state: it is removed in the change set that implements the rule, never left standing beside it.
- **`ByName` becomes a MULTIMAP** (`Dictionary<string,List<DataItem>>`) — COBOL permits duplicate names disambiguated
  only by qualification; the current single-value Dictionary silently overwrites (latent wrong-item bug).
- **SYNCHRONIZED is a no-op for in-memory typed data** (the CLR aligns a `long` naturally); honored only at the
  byte-serialization boundary (files / REDEFINES Tier-C). **JUSTIFIED RIGHT** and **BLANK WHEN ZERO** are
  display/store-time rules on `PicInfo`. (`DataItem` must gain `IsJustifiedRight`, `IsSynchronized`, `BlankWhenZero`,
  `RedefinesName`, `RenamesInfo`, `WidePrecision`, the 88 value-set, and figurative/ALL-pattern fields.)

### 3.6 Hard problems / edge cases

- **REDEFINES** is a byte-overlay with no clean typed form — resolved by the 4-tier model in §4 (this section just
  captures the clauses; §4 owns the semantics).
- **Ref-mod as a RECEIVER:** C# strings are immutable → `CobolString.SpliceInto(field, s, l, value)` rebuilds the
  string. (§7 owns the string runtime.)
- **OCCURS DEPENDING ON:** array at MAX; the length var bounds the live range; whole-group sending uses `[0..N)`,
  receiving uses MAX (ISO OCCURS GR7 — the ST146A lesson).
- **Whole-group-as-alphanumeric:** a group has no scalar field — its image is the concatenation of leaf display
  images via a generated `string AsImage()` per struct (THE single whole-group image facility — see §14.4). This is
  the G6 boundary; the hook is designed now.
- **FILLER:** a synthetic `_fillerN` member only when it has a VALUE or affects group serialization.

---

## 4. REDEFINES / RENAMES (the storage-overlay subsystem)

> **Conflict resolved (named loser): data-model §8's "independent typed fields, a write to one is not visible in the
> other" is SUPERSEDED.** That phrasing is the *incoherence trap* — it reproduces the exact silent-stale-read that
> triggered the blank-slate pivot. The authoritative model is the 4-tier **one-canonical-backing** model below. (See
> §14.3.)

### 4.1 The spine

Two differently-typed C# reps over one storage stay coherent with NO shared `byte[]` only if **they do not both
exist**: a "redefines class" (all entries sharing a storage area) has exactly ONE stored backing — the *canonical* —
and EVERY other view is a **computed accessor (a C# property)** over it. Never two stored fields per storage area.

### 4.2 The 3 tiers (priority cascade D > B > A; lattice A ⊑ B ⊑ D, join = max tier)

- **A — Alias** (identical PIC+USAGE, or RENAMES without THRU): one typed field; other names are pass-through
  properties (`WS_COUNT_ALIAS { get => WS_COUNTER; set => WS_COUNTER = value; }`).
- **B — StringCanonical** (the whole class has a byte image): canonical = ONE `string` of class-max width under the
  char==byte convention, and each view is a typed `(offset, width)` accessor over it. ⛔ **A view's window holds the
  leaf's PINNED `NumericByteForm` BYTES, not its digits** (V59 — *bytes are not text*): zoned characters for
  DISPLAY, radix-2 two's complement for BINARY/COMP-5/BINARY-*, BCD for PACKED-DECIMAL, the big-endian IEEE
  interchange encoding for the float family, and the 8-byte big-endian occurrence number for USAGE INDEX. ISO
  §13.18.60.4 GR4/GR11/GR13–GR15 leave each representation, including its sign, to the implementor; ours is pinned
  once in `NumericByteForm` and documented in CONFORMANCE.md items 205–215. A read decodes that window and
  re-renders the item's DISPLAY image, so a windowed leaf prints exactly what its natively-carried twin prints
  (§13.18.44.4 GR1 gives them one storage). **This is the dominant real case and covers the ENTIRE NIST path**
  (incl. SAME RECORD AREA / multi-01 FD classes with COMP leaves), and since the Step D arm-1 dissolution it also
  covers every MIXED-USAGE pun — float, COMP-5 and INDEX included.
  <br>⚠ It once read "a fixed-point COMP/COMP-3 leaf's window IS its zoned digit image", with a classification-time
  `SignKind` rewrite to `TrailingOverpunch`. That was the PRE-V59 representation; no such rewrite exists in the
  binder now, and the bytes are not digits.
- ~~**C — ByteCanonical**~~ — **DELETED (Step D, kb/Work PB164).** The tier was reserved for puns observing a
  representation with "no fixed character image cross-view" (float, COMP-5, INDEX) and was to become a class-scoped
  `byte[]` canonical. It never needed to exist: once every numeric usage had a pinned `NumericByteForm`, those puns
  became ordinary Tier-B byte windows over the one string backing. The enum member and `StorageForm.TierCWindow`
  are gone; the COMP-5 range concern is answered by the item owning its full container range (R10), decoded through
  the unsigned lanes rather than a Digits-wide window.
- **D — Reject loud** (spec-forbidden/unmodelable: object/pointer/message-tag/strongly-typed SR12/14; OCCURS
  DEPENDING ON / variable-length / dynamic-length SR5/17): a diagnostic — conformant, since these are already illegal.

Tier selection **reuses the legacy `RecordClassificationPass` transitive-closure shape** (byte propagates across the
REDEFINES class + to all subordinates; monotone; terminating), re-verdicted to the lattice.

### 4.3 Critical sub-decisions

- **Init only from the original** (REDEFINES SR9): a view emits NO stored value field and NO initializer; only the
  canonical original initializes the backing.
- **Class width = MAX storage width across all views** (SR8 exception: a level-01 non-EXTERNAL original may be
  redefined larger).
- **A numeric view still emits its `NumProfile`** (`_P_<view>`) and carries its PICTURE's natural surface type even
  though its stored VALUE field is suppressed — because `EmitArithAssign` references `_P_<CsName>`, so a numeric view
  used as an arithmetic target must compile. (Advisor correction baked in.)
- **RENAMES THRU over a heterogeneous span** composes via each leaf's EXISTING read/write: get = concat each leaf's
  DISPLAY image (`CobolNum.FormatDisplay` for a numeric leaf); set = distribute the value left-to-right by width via
  each leaf's MOVE-into path. Not raw `+` on a long.
- **Cross-type-read guard:** emit a LOUD diagnostic when a write to one view is followed (conservatively: any program
  that both writes and reads two different-typed views of one region) so nothing silently corrupts.
- **`[FieldOffset]`/`StructLayout(Explicit)` overlay is rejected outright** — cannot overlay a `string` on a `long`;
  the dominant pun is alphanumeric↔numeric which it cannot express; even long-over-long overlays VALUES not the byte
  REINTERPRETATION COBOL means.

### 4.4 Edge cases

REDEFINES inside an OCCURS element (overlay is per element); nested REDEFINES chains (follow `RedefinesTarget`
transitively to the true anchor); signed-DISPLAY overpunch in a numeric view (needs §6's `FormatDisplaySigned`/
`ParseDisplay`); lossless 8-bit carrier = the **storage-byte law** (`StorageByte`: byte 0xFF ↔ U+FFFF, the native HIGH-VALUE; every other byte k ↔ U+00kk;
kb/Work R51/R52, COBOLNET_FILES_DESIGN D29) — a CROSS-SUBSYSTEM constant shared with whole-group-image and files (§14.4). RENAMES must immediately follow the record's last entry
(SR2) and is attached to the owning record as a sibling, not into the storage tree.

---

## 5. Control flow

### 5.1 The dispatcher shape (authoritative; pipeline's `goto case` shape is the loser — §14.6)

One C# method per program unit:

```csharp
private const int N = /* paragraph count */;
private static int Dispatch(int startPc, int exitPc) {
  int pc = startPc;
  while ((uint)pc < (uint)N) {
    bool atExit = pc == exitPc;                 // captured BEFORE the body (the body overwrites pc)
    switch (pc) {
      case 0: /* para 0 body */ pc = 1; break;  // fall-through = pc = i+1
      case 1: if (cond) { pc = 3; break; }       // GO TO sets pc
              pc = 2; break;
      // …
      default: pc = N; break;
    }
    if (atExit && pc == exitPc + 1) return pc;   // named THRU exit paragraph fell off its end
  }
  return pc;
}
internal static void Main() { try { Dispatch(/*EntryParagraphIndex*/0, -1); } catch (StopRun) { } }
```

This realizes the legacy's PROVEN **return-address / exit-bounded** dispatch in idiomatic C#.
Control is followed by **pc value, never by physical block extent** — which is why inverted THRU ranges and
GO-TO-out-of-and-back-into a PERFORM range are correct *for free*.

### 5.2 Statement → emission table

| COBOL | C# |
|---|---|
| sequential fall-through | `pc = i+1; break;` (the last stmt of each non-terminating case) |
| `GO TO p` | `pc = idxP; break;` |
| `GO TO p1 p2 … DEPENDING ON sel` | `switch ((int)sel) { case 1: pc=idx1; break; … default: /* no transfer → fall-through */ }` |
| `ALTER g TO PROCEED TO t` | `_alter_g = idxT;` (a mutable `private static int _alter_g = <defaultTarget>;`); the alterable GO TO emits `pc = _alter_g; break;` |
| out-of-line `PERFORM p [THRU q]` | `Dispatch(idxP, idxQ); pc=i+1; break;` (recursive bounded dispatch; idxQ=idxP if no THRU) |
| out-of-line `PERFORM` of an EMPTY range | the control-phrase loop with an EMPTY body and NO `Dispatch` call — a zero-paragraph section (§14.4.2) makes GR4's specified set empty, and GR4's "function identically" makes that the inline emission minus the body |
| `PERFORM p n TIMES` | `for (long i=0;i<n;i++) Dispatch(idxP, idxQ);` |
| inline `PERFORM … END-PERFORM` | a REAL C# loop INSIDE the case (`for`/`while`/`do…while`), never a Dispatch call |
| `EXIT PERFORM` / `EXIT PERFORM CYCLE` | `break;` / `continue;` (scoped to the nearest inline PERFORM) |
| `EXIT PARAGRAPH` | `pc = myIdx+1; break;` |
| `EXIT SECTION` | `pc = lastParaIdxInSection+1; break;` |
| `EXIT` (bare) / `CONTINUE` | no-op (no statement) |
| `NEXT SENTENCE` | forward `goto __sent_<n>;` to a sentence-boundary label in the same case |
| `STOP RUN` | `throw new StopRun();` (caught only at run-unit `Main`) |
| `GOBACK` / `EXIT PROGRAM` | `throw new ProgramReturn();` (caught at the current program's `Entry`) |

### 5.3 Key decisions

- **STOP RUN vs GOBACK are DISTINCT exceptions** (the authoritative resolution — see §14.5). `StopRun` unwinds ALL
  Dispatch frames in ALL programs (caught at run-unit `Main`); `ProgramReturn` unwinds only THIS program's PERFORM
  frames and returns to the CALL site (caught at the program's `Entry` wrapper; carries the RETURNING value). Integer
  pc is used ONLY for in-program transfers. *(Rejected: pipeline's "GOBACK → `return;` from the current Dispatch
  level" — a C# `return` exits only the innermost recursive Dispatch, so a GOBACK nested inside a PERFORM would
  wrongly resume the PERFORM caller instead of returning to the program's caller. Also rejected: the legacy
  `pc = -1` return-code propagation — unwinds only one frame and can't cross a CALL boundary.)*
- **ALTER → a mutable `int` field**, not the legacy `int[] _alterTable`. The pc-variable model makes this trivial —
  the decisive reason for switch-on-pc over pure labels.
- **DECLARATIVES/USE:** ONE pc index space over ALL paragraphs INCLUDING declaratives (so every pc value agrees), but
  `Main` starts at `EntryParagraphIndex` (first paragraph after END DECLARATIVES, ISO §14.4). A declarative is
  reached ONLY via a `Dispatch(declStart, declEnd)` call from the runtime I/O/error path, never main fall-through.
  The declaratives' STRUCTURAL syntax rules live at three funnels, never per verb (kb/Work PB361–PB363): §14.9.49.3
  SR1's USE placement — the legal first sentence is consumed by `DeclCollectSection`, so any `useStatement` that
  reaches `BindStatementCore` is misplaced (COBOLNET2377); SR3/SR4's reference boundary — asked of section identity
  in `ResolveProcedureOperand`, the one procedure-name funnel, with only PERFORM (SR4) and RESUME (SR3) exempt
  (COBOLNET2376 error / COBOLNET2375 warning, determination D-DECLREF); SR10/SR11's USE BEFORE REPORTING
  restrictions — `ctx.Enclosing.InBeforeReportingProcedure`, read by GENERATE/INITIATE/TERMINATE and by
  `BindStatement`'s store check over `BoundStores.StoresInto` (COBOLNET2378).
  (Avoid the legacy off-by-N: dispatch order excluding declaratives while pc values include them.)
- **EVALUATE → a chained if/else-if/else, NOT a C# switch** (ISO §14.9.13.4 GR4: process each WHEN left-to-right,
  first match). WHEN arms are ranges/conditions/multiple-ALSO/ANY/partial-expressions — not constant case labels.
  Selection subjects hoisted into locals (evaluated once, GR3).
- **PERFORM VARYING…AFTER:** each outer increment RESETS all inner (AFTER) identifiers to their FROM values before
  re-testing the outer UNTIL (ISO §14.9.28 + Annex D.26 figs D.11–D.14 — the #1 VARYING gotcha).
- **Dead-code suppression:** track a `terminated` flag; once an unconditional transfer is emitted (GO TO/STOP/GOBACK/
  EXIT *), stop emitting the rest of that case (unreachable-but-legal COBOL would fail warnings-as-errors).

### 5.4 Hard problems

GO TO that exits/re-enters a PERFORM range (free — return-address model); inverted THRU `B` before `A` (free — never
iterate `[min,max]`; NC102A); **an EMPTY procedure range is NOT free and is not derivable from the pc pair** — a
zero-paragraph section and an ADJACENT inverted THRU range are the same `(s, s-1)`, so emptiness is a CARRIED bit on
`PcRange` and the dispatcher is never handed an empty range (kb/Work PB440; control-flow deep-dive D5a);
overlapping/recursive PERFORM (the C# call stack IS the return-address stack); duplicate
paragraph names across SECTIONs (resolve by paragraph SYMBOL to a distinct PcIndex at bind time, never by name);
NEXT SENTENCE goes to the next *period*, NOT past a scope delimiter (ISO Annex F.1 — the common misconception).

---

## 6. Numeric model

### 6.1 The substrate + the value engine (the central hardening)

Storage is the narrow native type (`long`/`Int128`/native-int/`float`/`double`). The **value engine is
Int128-monomorphic** via a single intermediate carrier:

```csharp
public readonly record struct CobolInt(Int128 Unscaled, int Scale) {
  public static (CobolInt,CobolInt) Align(CobolInt a, CobolInt b);   // scale-align to max(scales)
  public static CobolInt Add(CobolInt a, CobolInt b);                // result scale = max(scales)
  public static CobolInt Sub(CobolInt a, CobolInt b);
  public static CobolInt Mul(CobolInt a, CobolInt b);                // result scale = sum(scales)
  public static CobolInt Div(CobolInt a, CobolInt b, int guardScale, CobolRounding m);
}
```

Every operand widens `long`→`Int128` at op entry, scales-align, computes in `Int128`, and a single
`TryStore` rescales/rounds/truncates/bounds-checks back into the receiver's storage type. **The current
`CobolNum` is long-only and silently overflows real `COMPUTE`** (e.g. `c = a * b` on two `PIC 9(18)` = 36
digits) — `Int128` (38 digits) covers every legal fixed-point picture and its intermediates. *(Rejected:
keep long-only — silently wrong; `decimal`/`BigInteger` intermediates — owner-locked out; generic
`INumber<T>` monomorphized per width — JIT-bloat + reintroduces width-typed overflow.)*

### 6.2 Intermediate precision (ISO §8.8.1; mined from the legacy `decimal` path)

- ADD/SUBTRACT result scale = `max(scales)`; MULTIPLY = `sum(scales)`.
- **DIVIDE/COMPUTE-division** quotient at an explicit `guardScale = max(all receiver scales, all operand scales) +
  DIV_GUARD_DIGITS`, **`DIV_GUARD_DIGITS = 14`** (reproducing the legacy `decimal` accumulator's ~28-sig-digit
  headroom — the one scale `decimal` auto-picked that `Int128` forces explicit); the final per-receiver store rounds
  ONCE to the receiver scale per the ROUNDED mode.
- EXPONENTIATION integer powers expand to repeated multiply (§8.8.1.5.4).
- **Statement arithmetic** (ADD/SUB/MUL/DIV with GIVING) enforces the 31-digit composite-of-operands limit at COMPILE
  time (guaranteed to fit `Int128`); **COMPUTE expressions have NO composite limit** (§8.8.1.2 rule 7) — `Int128` is
  the cap, EC-SIZE-OVERFLOW past ~38 digits.
- **v1 arithmetic mode = NATIVE** (§8.8.1.3, implementor-defined = `Int128` fixed-point) — the corpus default.
  STANDARD-DECIMAL (decimal128) and STANDARD-BINARY (spec-obsolete) are owner-gated/deferred (§15).

### 6.3 Usages & store

- **A usage decides THREE orthogonal facts, and conflating any two is the V59 defect class.** ① the CAPACITY
  discipline (`NumProfile.Truncation` — where SIZE ERROR bites), ② the BYTE REPRESENTATION
  (`NumProfile.ByteForm` — what the item occupies in a record image, a file record, a SORT key window and a
  REDEFINES backing), ③ the SIGN PRESENTATION (`SignKind`). DISPLAY and BINARY share ONE capacity discipline and
  occupy entirely different bytes, so the discipline can never stand in for the representation — that substitution
  is exactly how `PIC 9(4) COMP` came to reach a file as the four ASCII bytes `31 32 33 34`.
- **The pinned representation table (`NumericByteForm`) — implementor-defined by §13.18.60.4 GR4/GR7/GR11/GR12
  and therefore ours to state, with the §4.2.16 / Annex A.1 items 205 and 215 documentation obligation attached:**
  `Zoned` = USAGE DISPLAY, one byte per digit position, sign per `SignKind` · `Binary` = BINARY/COMP/COMP-5/
  BINARY-CHAR..DOUBLE, two's complement, BIG-ENDIAN, `StorageLength` bytes (1-2-4-8 by digit count; the survey is
  unanimous — IBM, Micro Focus and GnuCOBOL all write big-endian for USAGE BINARY) · `Packed` = PACKED-DECIMAL,
  BCD two digits per byte with a trailing sign nibble (`0xC` positive, `0xD` negative, `0xF` for an unsigned
  item) · `PackedNoSign` = the 2023 WITH NO SIGN form, digit nibbles only · `None` = reaches no byte image at all
  (USAGE INDEX, §13.18.60.4 GR10 — a codec handed one must reject it LOUDLY, never invent bytes). The width each
  implies IS `PicInfo.StorageWidth`, which is the same width `FUNCTION BYTE-LENGTH` reports (§15.14.4 r1) and
  the same width the item occupies in a group image (§15.50.4 r3). **One width, one representation, everywhere**
  (§14.4). `NumericByteFormDriftTests` pins the whole table, so a new usage cannot inherit a representation
  nobody chose.
- DISPLAY/COMP/COMP-4/BINARY → DigitCount discipline; COMP-3/PACKED → 2n−1; COMP-5/BINARY-* → native
  two's-complement width (binary-WRAP, not digit truncation — `PIC S9(4) COMP-5` = −32768..32767, `PIC 9(4) COMP-5`
  = 0..65535, an unsigned 8-byte needs `ulong`); COMP-1/COMP-2 → IEEE, bypass the scaled engine.
- The 8 ROUNDED modes are the existing `CobolRounding` enum. **`Store` is hardened to `TryStore`** (returns `bool`;
  receiver UNCHANGED on overflow; ROUNDED MODE PROHIBITED → SIZE ERROR on an inexact result). The current `Store`
  (`%= Pow10(Digits)` silent truncate) is only the no-ON-SIZE-ERROR branch. *(See §14.7: `TryStore` and the
  conditions design's `StoreChecked` are the SAME method — settle on `TryStore`.)*

### 6.4 Signed-DISPLAY overpunch (`NumProfile.SignKind` = the POSITION, `NumProfile.SignEncoding` = the CHARACTERS)

`NumProfile` carries `SignKind ∈ {TrailingOverpunch[default], LeadingOverpunch, LeadingSeparate, TrailingSeparate,
BinaryMinus}` — WHERE the sign sits — and `SignEncoding ∈ {Ibm[default], Ascii}` — WHICH characters a fused sign
uses. The two are orthogonal, and both tables of both conventions live in ONE place, `ZonedSign`, which the encoder,
the decoder and the NUMERIC class condition all consult (kb/Work PB803; the numeric deep-dive D5 is the full
determination, and `docs/CONFORMANCE.md` DOC-A.1-177 / DOC-A.1-178 are the Annex A.1 rows). Under the `ibm` default,
verified NIST-exact against the legacy: positive `0→'{',1→'A'…9→'I'`; negative `0→'}',1→'J'…9→'R'`.
`PIC S9(3)` +42→`"04B"`, −42→`"04K"`, −150→`"0015}"`, SIGN LEADING −37→`"}37"`. The default with no SIGN
clause is TRAILING overpunch. Under `ascii` (the compile option `--sign-encoding=ascii`) a positive digit is left
alone and a negative digit is digit+0x40: `PIC S9(4)` −1234→`"123t"`, SIGN LEADING→`"q234"`.
**`CobolNum.FormatDisplaySigned`/`ParseDisplay` (encode+decode incl. overpunch + separate sign) must land before §4
Tier-B/C numeric-view accessors are exact** — the current `FormatDisplay` returns magnitude.

### 6.5 Numeric-edited formatting

PORT the proven two-pass legacy `FormatByEditPattern`/`FormatNumericEdited` verbatim into
`CobolEdit.Format(CobolInt value, EditPattern pat, env) → string` (the field's CLR storage is `string`). Covers
`Z * $ + - CR DB B 0 / . ,` fixed+floating insertion, asterisk check-protect, BLANK WHEN ZERO, full-field-blank,
DECIMAL-POINT IS COMMA, CURRENCY SIGN. *(Rejected: rewrite from the §14.9.x grammar — high regression risk against a
battle-tested 364-NIST oracle; .NET format strings — can't express floating insertion/check-protect/overpunch.)*

### 6.6 Hard problems / edge cases

- MOVE rescale is value-preserving with TRUNCATION rounding (never ROUNDED); a scaled→integer MOVE drops the
  fraction; an unsigned receiver stores the magnitude (GR8; watch `long.MinValue`).
- Comparisons align scales then compare the `Int128` unscaled values; `+0 == −0`.
- IS NUMERIC on a typed numeric field folds to `true` (it can't hold non-digits) UNLESS the field aliases external
  bytes (REDEFINES/file/ACCEPT) — then validate the char image (`CobolNum.IsNumericClass(image, profile)`).
- ON SIZE ERROR is two-phase (ISO §14.7.5 rule 4): (a) intermediate-evaluation error → no receiver changes; (b) per-
  receiver store error → only THAT receiver unchanged; the phrase fires if ANY failed (generated C# accumulates a
  `bool __sizeErr`).
- P-scaling (leading adds to FractionScale; trailing rounds to the 10^P grid); divide-by-zero → SIZE ERROR (guard
  `b.Unscaled == 0`, never a .NET `DivideByZeroException`).

---

## 7. String operations

### 7.1 Representation + the runtime class

Alphanumeric/national elementary items are `string` at rest; every mutating op takes the value in and returns the new
value; the emitter assigns ONCE: `FIELD = CobolStrings.InspectReplace(FIELD, …);`. **A new
`CobolNet.Runtime.CobolStrings`** holds the ported INSPECT/STRING/UNSTRING algorithms (mined verbatim from the legacy
`InspectRuntime`/`StorageArea` — proven over 364 NIST tests; only the I/O type changes from `(byte[],offset,len)` to
`string`). *(Rejected: `Span<char>` mutable view — `System.String` is immutable; a byte[] scratch — reintroduces the
banned substrate.)* See §14.8 for the `CobolString` vs `CobolStrings` roster split.

### 7.2 The C# mappings

- **Reference modification read:** `CobolString.RefMod(s, leftmost, length)` (1-based; length omitted → to end as
  `-1`); raises EC-BOUND-REF-MOD on out-of-range/zero (unless REF-MOD-ZERO-LENGTH on → `""`).
- **Ref-mod write:** `RESULT = CobolString.SpliceInto(dst, leftmost, length, newSlice)` rebuilds the string;
  positions evaluated once. Editing is NOT re-applied (spec NOTE 21209).
- **INSPECT** marshals operands into parallel arrays and calls ONE method per statement (per comparison cycle —
  TALLYING/REPLACING share a single left-to-right cycle, ISO §14.9.22.4 GR8). Counters/COUNT route through
  `CobolNum` + `NumProfile` (a counter is NOT initialized, GR11). BACKWARD = reverse target+patterns, run forward,
  reverse back.
- **STRING** seeds the working buffer from the dest's current value (GR7: only written positions change); pointer is
  a C# `int` round-tripped via `CobolNum`; overflow when pointer<1 or >len before a char move (GR8).
- **UNSTRING** uses a per-INTO `UnstringExtract` (earliest delimiter wins, tie→first-listed, ALL skips contiguous
  repeats; two contiguous delimiters → empty extract → space/zero fill via `CobolString.Store`/`CobolNum.Store`).
  Overlapping operands (§14.9.48.4 GR18, Annex A.2 item 61) run IN PLACE, as libcob does (CONFORMANCE.md D-UNS1 to
  D-UNS4): `StringEmitter` re-reads the sender and each identifier delimiter before every receiving area after the
  first, but only where `StorageSharing.MayShare` says a store (INTO, DELIMITER IN, COUNT IN) can reach it; the TALLYING
  item receives its content at the end plus the count, stored before the POINTER item.
- **National (PIC N)** uses the IDENTICAL string helpers with NO surrogate-aware handling — one COBOL char = one
  UTF-16 code unit (ISO §8.5.1.4: "each two-octet code element of UTF-16 is treated as though it were itself a
  character"). C# string indexing IS per-code-unit, so .NET `string` is the exact COBOL national model.

### 7.3 Key decisions

- **Short-circuit `&&`/`||`** for AND/OR (a deliberate divergence from the eager legacy oracle) — empirically
  corpus-safe (a scan found ZERO guard-then-same-subscript idioms). *(Local escape hatch if a future program needs
  eager eval: hoist the right operand before `&&`.)*
- **Figurative constants** map: SPACE→`' '`, ZERO→`'0'`/`0L` (by receiver category), QUOTE→`'"'`, **HIGH-VALUE →
  U+FFFF, LOW-VALUE → U+0000, in both classes** (the cross-subsystem settlement — §14.9; owner decision kb/Work R52), ALL "x"
  repeat-to-width.
- **The predefined NULL is an IDENTIFIER, never a figurative constant** (kb/Work PB1427). §8.4.3.1.2 lists it as
  identifier Format 8 (predefined-address) and the NULL arm of Format 6 (predefined-object); §8.3.3.6.2's seven
  figurative formats do not include it. The grammar gives it its own rule, `predefinedNull`, carried by
  `nonNumericLiteral` (every operand slot that parsed it while it was a `figurativeConstant` arm still parses it —
  superset parse) and by the SET senders, `callByReference` and `invokeArgument`'s BY REFERENCE arm. It binds to
  ONE node, `BoundPredefinedNull`, produced ONLY by `ExpressionBinder.NullAdmittingOperand` in a §8.4.3.10.3 SR1
  context (INITIALIZE REPLACING, a program-prototype CALL / function-prototype activation / method-invocation
  argument, a relation or EVALUATE operand); every other slot binds through `NonNumericLiteralOperand`, which refuses
  it — **COBOLNET2576**, the one SR1 screen — so a slot nobody thought about is refused by construction instead of
  running NULL as LOW-VALUE. Admission is not acceptance: each context then decides NULL against its other operand
  or formal (the Format-3 relation band, §14.8.2 conformance, INITIALIZE's SET-form category agreement). The
  renderers throw on it outside those contexts. The positions that decode a literal WITHOUT the operand model — a
  constant entry's AS operand, every VALUE clause operand (data item, condition-name, report group; the one
  `DataBinder.IsLiteralValueOperand` predicate, which does not count NULL a literal), a concatenation operand, an
  externalized name — refuse it with the same code and the same text, `PredefinedNullRule.Report`; §13.16.3 SR10's
  class-pointer VALUE screen (COBOLNET2168) keys on the clause being WRITTEN, so `USAGE POINTER VALUE NULL` draws
  both rules. `PredefinedNullContextDriftTests` pins the producer and its callers, the one reporter, and that every
  literal decoder outside the PROCEDURE DIVISION binders decides the `predefinedNull` arm.
- **A typed `StringLvalue` IS a `Place`** (§14.1) — not a second abstraction.

### 7.4 Hard problems

Non-alphanumeric/group operands "treated as if redefined alphanumeric of same size" → the materialize/writeback path
(§14.4 whole-group image); numeric source → unsigned digit string (GR4d de-signing). The lvalue/subscript/ref-mod
discrimination is the §3.4 SUB_* interpreter. Overlap (spec-undefined) → deterministic-and-safe via read-once /
single-write-back.

---

## 8. Files

### 8.1 The model

The FD/SD record IS a `record struct` (the record area is a typed field, not a byte buffer). **Every FD has one**,
including the entry §13.4.5.3 SR3 lets you write with NO record description entries: §14.9.30.4 GR6's implied
"alphanumeric group item of the maximum size established by the RECORD clause" is synthesized at bind time
(`COBOLNET_FILES_DESIGN` D18), so `FileModel.AreaRecord` is null only for a REPORT file and an FD-less SELECT. The
only bytes are at
the on-disk edge, produced by a compiler-GENERATED per-layout codec (`Serialize`/`Deserialize`) running only at READ
and WRITE. **CODE-SET is one `Encoding` parameter threaded into that codec.** The proven 364-NIST legacy handlers are
ported VERBATIM for *control logic* (open-mode tables, ISO §9.1.13 status codes, the file-position-indicator +
key-of-reference + duplicate-ordering state machines) but re-substrated from a byte array to a generic
`FileConnector<TRec>` + an `IRecordCodec`.

```
FD CUST-REC { CUST-ID PIC 9(5); CUST-NAME PIC X(20); CUST-BAL PIC S9(7)V99 COMP-3 }
  → public record struct CUST_REC { public long CUST_ID; public string CUST_NAME; public long CUST_BAL; }
    (CUST_BAL is the UNSCALED long; scale 2 = metadata)
```

The connector exposes Open/Close/Read/ReadPrevious/ReadByKey/Write/Rewrite/Delete/Start/SetKey + `CurrentSlot`,
`LastRecordLength`, `EndOfPage`, `LastStatus`. **There is NO program-supplied key parameter** — the key is the current
value of the typed RECORD KEY / RELATIVE KEY field.

### 8.2 Key decisions

- **Ordering/lookup keys are a typed-derived `CobolKey` comparable** (numeric by decoded value; alphanumeric by image
  + collating with shorter-operand space-extension; composite component-wise), DECOUPLED from the stored payload.
  ONE comparison policy shared by indexed files AND SORT. *(Rejected: the legacy Latin-1 byte-string sorted-dictionary
  trick — silently mis-orders COMP/COMP-3/signed keys, whose on-disk image is not order-preserving.)*
- **Stores:** sequential = `StreamReader/Writer` (line-sequential) or `FileStream` + a 4-byte little-endian length
  prefix (varying) — a variable-length group record's frame also carries its extent table, COBOLNET_FILES_DESIGN
  D27 / CONFORMANCE.md §3 D-FRA (v); relative = sorted dict `int slot → byte image` with `0xFF` gaps; indexed = sorted dict
  `CobolKey → byte image` (sole source of truth, alternates derived on demand + a PER-KEY release ordinal for
  duplicate ordering). The in-memory PAYLOAD is the serialized image (exactly the form persisted on CLOSE; bounded
  memory), deserialized to the typed record only on hand-back. *(This is owner-flagged — §15 Q-file-2.)*
- **The indexed duplicate order is per KEY OF REFERENCE, and the file position indicator is a KEY VALUE.** ISO
  §14.9.30.4 GR26 scopes the release order to "an alternate record key that is the key of reference" and
  §14.9.35.4 GR24 a) freezes an untouched key's order across a REWRITE, so the release ordinal is a VECTOR — one
  slot per key, slot 0 the prime (assigned at release, never re-stamped, so it doubles as the record's physical
  release order). §14.9.41.4 GR17 e) 1. puts only a KEY VALUE in the file position indicator, so a START-seeded
  walk enters a duplicate set at the end GR26 names for its direction; the duplicate-set position of a prior READ
  is separate connector state, which is what §14.9.30.4 GR21 rules e)/f) name instead of the FPI. CLOSE persists a
  topological order of the per-key duplicate orders so a reload — which can only give every key the file's own
  order — reproduces all of them (kb/Work PB341, PB342).
- **Multiple 01s under one FD (and SAME RECORD AREA) = a discriminated record-area wrapper:** READ deserializes the
  raw bytes into EVERY 01 view; WRITE serializes the named view. This is the file-edge analogue of REDEFINES (ISO
  §9.1.2 NOTE / §13.18.33 GR3).
- **SORT/MERGE:** SD record is a typed struct; the sort store holds serialized images ordered by the same `CobolKey`
  policy; key offsets AND WIDTHS are BYTE windows into the deterministic serialized image, computed at compile time
  (a national position is two of those bytes — §13.18.60.4 GR8, D-N1). Format-2 in-place table SORT operates on the
  typed array directly (the one place the two SORT forms diverge — a typed comparer), over the table's CURRENT
  occurrences only (§14.9.40.4 GR20): `CobolTable.SortInPlace` receives the span the one
  `ReferenceResolver.CurrentOccurrenceCount` model names — an OCCURS DEPENDING data-name-1's count, a
  dynamic-capacity table's `CurrentOccurrences`, a fixed table's whole array (kb/Work PB1174). **The subject's
  STORAGE FORM is decided once, by the binder, as `BoundTableSort.Storage`** (kb/Work PB1175, PB1055): a table with
  its own element array is `TableSortStorage.TypedArray` (the structural whole-table path `BuildTablePath(table,
  outer)`, one index for each ENCLOSING table — §14.9.40.3 SR13 with §8.4.2.3.3 SR3, SR5 e), SR6: the subject writes
  the enclosing tables' subscripts, a rightmost ALL standing for its own level; `ReadTableSubjectSubscripts`), and a
  table whose storage is a shared byte area — a REDEFINES class, a record area shared by several 01s, a BASED or
  EXTERNAL record — is `TableSortStorage.SharedArea`: it has no array, so the statement sorts the element NUMBERS
  with the same key comparer (`EmitKeyComparer`, every key read through its own window, `ResolveItemAt`) and places
  the element images back through the class's windows, every image read before the first write (§14.9.40.4 GR24;
  §13.18.44.4 GR1) — and the managed SLOTS of the element's pointer-class members (a pointer's value is not in the
  bytes, `SlotWindow.MembersOf`) are read with each element and written back by the same permutation (kb/Work PB1922).
  A typed-array table whose KEY has no stored field on the element struct (a member of a REDEFINES view of the
  element) is `TypedArray` with `KeyWindowOuter` set: every key is then read through its window at an element number,
  the NUMBERS are sorted and the elements permuted (kb/Work PB599) — one comparer, the member-path form kept for the
  common case. **The record sizes of the files of one SORT or MERGE are ONE screen**, `SortBinder.ScreenTransferRecordSizes`
  over `FileModel.RecordSizeRange` (§14.9.40.3 SR5 / SR11, §14.9.24.3 SR3 / SR12: a variable-length file bounds both ends
  of the other's records, a fixed-length file only the upper one; USING bounded by the SD, the SD bounded by GIVING;
  kb/Work PB995). **A fixed-length SD's record is the largest record description's** (the implied Format 1,
  §13.18.43.4 GR5 a)), so a RELEASE of a shorter record description sends the record area's image like WRITE does
  (D-WRT1; `SequentialIoEmitter.SentRecordImage`, kb/Work PB322 F). **WHICH items may be a key is ONE predicate** — `SortKeyAdmission.Violation(key, rules)`
  (kb/Work PB1173, PB1052), asked of the data description by all three key-naming statements (SORT Format 1 §14.9.40.3
  SR6, Format 2 SR14, MERGE §14.9.24.3 SR4): a key subject to OCCURS (file verbs; Format 2's own is SR14 e) over the walk
  to data-name-2), of class boolean / object / pointer (message-tag has no representation yet), a dynamic-length
  elementary item, a variable-length group or an occurs-depending group item is refused BY NAME, COBOLNET1757, before
  the record's image or the key's window is asked about. The file verbs read SR6 d)'s "occurs-depending-on data item" as
  including the occurs-depending GROUP (§13.18.38.4 GR8): a key whose length varies cannot be "the same byte positions"
  of SR6 e). `RecordLayout.OffsetInRecord` bails on OCCURS ahead of its REDEFINES-class branch, so a multi-01 SD no
  longer places a table key at its first occurrence. **The USING and GIVING files are of ANY organization**
  (kb/Work PB994): the as-if OPEN / READ NEXT / WRITE / CLOSE of §14.9.40.4 GR12 / GR15 and §14.9.24.4 GR7 / GR12 run
  through the one runtime file facade, which dispatches to the sequential, relative and indexed connectors, so
  `SortMapIoFiles` admits a relative or indexed file unless its access mode is RANDOM (§12.4.5.5.2 SR1, §14.9.40.3 SR12 /
  §14.9.24.3 SR13). A relative GIVING file's records are relative records 1..n and its RELATIVE KEY item indicates the last
  one returned (GR15 b); an indexed GIVING file is written in the sort's order, which `ScreenIndexedGivingKey` makes the
  prime-key order (§14.9.40.3 SR9 / §14.9.24.3 SR10: first key ASCENDING, on the prime key's bytes). **A key's COMPARATOR is
  selected by the key's CLASS, never by the statement** (§14.9.40.4 GR5 / §14.9.24.4 GR5 resolve TWO sequences and
  each key takes the one its class names): the bound key carries a `CollatingClass`, the file sort passes it to the
  runtime as `CobolSort.KeyClass` and the table sort picks the comparer's carrier argument from it. A NATIONAL key
  decodes its UTF-16BE byte pairs back to national POSITIONS before comparing (§8.8.4.2.9 compares national
  character positions), and a NUMERIC key decodes algebraically through its own profile (§8.8.4.2.4); a BOOLEAN key
  does not exist — §14.9.40.3 SR6 c) / SR14 c) / §14.9.24.3 SR4 c) refuse it (`SortKeyAdmission`).
- **The SORT/MERGE procedure PHASE lives in the sort store (kb/Work PB349).** A store exists only while its
  SORT/MERGE statement executes (`Init` → `Close`); the emitter marks the INPUT and OUTPUT procedure ranges with
  `CobolSort.EnterProcedure`, `Sort` ends the input phase, and an output-procedure at-end sets the GR3 latch. The
  program's RELEASE and RETURN STATEMENTS render the checked entries `ReleaseStatement` / `ReturnStatement`, which
  test that phase before acting — §14.9.32.4 GR1 EC-FLOW-RELEASE, §14.9.34.4 GR1 EC-FLOW-RETURN, §14.9.34.4 GR3
  EC-SORT-MERGE-RETURN, each a `FatalAmbientGates` row whose `QueryFor` arm is the bound RELEASE / RETURN node —
  while the implicit USING release and GIVING return keep the unchecked `Release` / `Return` primitives, because
  each verb's GR1 governs the statement the program writes. With checking off nothing is raised and the statement
  proceeds (a RELEASE with no SORT executing has no store to release to).
- **The sort-merge STATEMENT conditions are raised by the store too (kb/Work PB1036).** EC-SORT-MERGE-ACTIVE reads
  the same phase: `Init` raises it while ANY store is in a procedure phase (§14.9.40.4 GR10/GR13, §14.9.24.4 GR8 —
  before the store is touched, so an enclosing statement on the same file survives), `ReleaseStatement` while one is
  in an output phase and `ReturnStatement` while one is in an input phase, ahead of the EC-FLOW-* test
  (docs/CONFORMANCE.md §3 D-SMA). EC-SORT-MERGE-FILE-OPEN is `FileNotOpen`, emitted per file where the rule places it
  — SORT at the start of phase a) for USING and of phase c) for GIVING (§14.9.40.4 GR9), MERGE at statement start for
  both (§14.9.24.4 GR7/GR12) — and only when the name is enabled there. EC-SORT-MERGE-RELEASE is the record-range
  test of `ReleaseStatement` (§13.18.43.4 GR14 b)/GR19 b), which also slices the DEPENDING ON size itself so no
  reference modification can raise EC-BOUND-REF-MOD first) and of the implicit `Release` (§14.9.40.4 GR12 b),
  §14.9.24.4 GR7 b) — the READ size against the SD's range, min 0 for a fixed SD). EC-SORT-MERGE-SEQUENCE is a
  per-stream scan in `Merge` over the SAME key columns the merge compares with, run only while enabled
  (§14.9.24.4 GR6). The statement's body after `Init` is a `try … finally { Close }`, so every way out — including
  a raise the guard disposes of after the unwind — releases the store. `EcBinder.QueryFor`'s SORT/MERGE arms ask
  these names AND the EC-I-O family per USING/GIVING file, which is what arms the implicit transfers'
  `__IoCheckEc` hooks. The disposition is `EcEmitter.VerbDisposes`: a condition the SORT/MERGE statement itself
  raised is disposed of by the verb (§14.6.13.1.3 2) — declarative, then the statement ends, the run unit
  continues). "Itself" is MARKED by the store: `Init`, `FileNotOpen`, the implicit `Release`, `Sort` and `Merge` run
  under an exception filter that sets `CobolFatalException.RaisedBySortMerge` (an EC-LOCALE-* from a locale-collated
  key included), and the SORT/MERGE guard re-throws only unmarked conditions — a RELEASE or RETURN statement's raise
  and a procedure statement's keep the general fatal disposition.
- **The SD/FD record codec IS the generated `AsImage()`/`FromImage()` pair (Phase 1E):** for every image-capable
  record — including mixed-usage records with fixed-point BINARY/PACKED leaves, which serialize each such leaf as
  its §14.4 zoned digit image (width = `Pic.Digits`, trailing-overpunch sign; ISO §13.18.60 USAGE GR4 implementor
  representation) — WRITE/RELEASE send `AsImage()`, READ/RETURN distribute via `FromImage()`. `IRecordCodec` for
  these records is realized by that pair; no separate serializer exists (singular-pattern). A signed COMP/COMP-3
  SORT/MERGE key's compile-time descriptor carries `PicInfo.ImageSignKind` (the IMAGE sign — trailing overpunch),
  never the leaf's stored `BinaryMinus`, so `CobolSort.NumericKey` decodes the zoned window algebraically
  (§14.9.40 GR8 / §8.8.4.2.4 — negatives order before positives; ST108A/ST127A/ST133A/ST134A). The greenfield
  on-disk record width for such records legitimately differs from the legacy's raw-byte form (e.g. 80 chars where
  the legacy wrote 72+4 binary bytes) — chains are same-engine, cross-engine file compatibility is not required.
  Only float (COMP-1/2), COMP-5, and INDEX leaves keep a record outside the codec (loud Tier-C island, §4.2).

### 8.3 Hard problems / edge cases

Variable-length REWRITE must equal the replaced length (status 44; remember last-read length); the read-position
state machine (READ NEXT after AT END = 46; sequential REWRITE/DELETE without a preceding READ = 43; START =
inclusive FPI); EXTERNAL FDs — **IMPLEMENTED (IC227A; §13.18.22.4 GR4a/GR4b/GR5)**: the connector keys
the run-unit registry by `"::EXT::" + externalized-name` (= the FD name, GR5) instead of the per-program
`PROG::FILE` qualification, so every describer's verbs converge on ONE connector (`CobolFile.Register` keeps an
existing `::EXT::` key — a later describer's activation never clobbers the live connector; CANCEL's `CloseFiles`
skips external connectors, §14.9.5 GR8/GR9), and the record area re-bases onto ONE run-unit `ExternalStore` cell
keyed `"FD::" + externalized-name` via the same Tier-B string-canonical machinery WS EXTERNAL 01s use
(`DataBinder.Linkage.cs::CallBindExternalAndGlobal`); FILE STATUS items stay per-program; the GR6 same-byte-count
cross-describer check is §14.8.4 EC-band work (documented-deferred). GLOBAL FD inheritance — **IMPLEMENTED
(IC233A/IC234A; §13.18.27.4 GR1)**: ancestors' GLOBAL FileModels merge into a contained unit's `FilesByName`
ONLY (never `Files` — no re-registration/re-qualification/CANCEL-close; the shared FileModel reference keys the
child's verbs to the owner's connector), the GLOBAL FD's records join `CallGlobalRoots` (record-names are global
names → the standard `__outer` ref-bridges), `StatementValidation.ResolveRecordName` — the ONE `record-name-1`
operand rule, shared by WRITE §14.9.51.3 SR5, REWRITE §14.9.35.3 SR1 and RELEASE §14.9.32.3 SR1, and the
successor to `SequentialIoBinder.FileOfRecord`, which tested CONTAINMENT where the rule says IDENTITY
(`COBOLNET_FILES_DESIGN.md` §"The `record-name-1` operand") — resolves a contained WRITE/REWRITE of the owner's
record through the merge, and the I-O status routes to the owner's local status item (§12.4.5.8.4 GR1 NOTE 1).
A record may be GLOBAL when its FD is not (§13.18.27.3 SR1 b)): the record-name is visible in a contained program
and its FILE is not, so the container's non-global FileModels are kept in `DataBinder.ContainerLocalFiles` for ONE
question — `ResolveRecordName` refuses a contained WRITE / REWRITE of such a record by WRITE §14.9.51.3 SR21 /
REWRITE §14.9.35.3 SR3 (COBOLNET1757; kb/Work PB1193), never by the false "not a logical record" arm;
OPTIONAL files (OPEN INPUT missing → 05 + EOF; non-optional missing → 35);
sequential RELATIVE WRITE assigns the next slot and MOVEs it into the RELATIVE KEY field; LINAGE (LINAGE-COUNTER,
page reset/overflow, footing area, END-OF-PAGE). After each I/O verb the compiler stores `LastStatus` into the FILE
STATUS item then branches AT END / INVALID KEY on the first char (1 at-end, 2 invalid-key, 3/4/7/9 fatal → a USE
declarative). READ INTO → Read + a typed group MOVE (receiving uses MAX length for ODO records — ST146A).

---

## 9. Interprogram (CALL / cross-program data)

### 9.1 The ONE carrier

`ManagedRef<T>` (the typed-native re-implementation of `ManagedPointer`) serves BY REFERENCE args, LINKAGE items,
USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET ADDRESS OF — honoring the singular-pattern rule. Two
construction modes + a Null state:

```csharp
// accessor-over-native-field (the common case — WORKING-STORAGE stays native, zero boxing):
ManagedRef<long>.OverField(() => WS_X, v => WS_X = v)
// standalone cell (LINKAGE / BY CONTENT copy):
ManagedRef<long>.Cell(5L)
ManagedRef<long>.Null
```

> **DATA-POINTER carrier (the ALLOCATE backing).** DATA-POINTER
> VALUES are a third concrete carrier, `CellPointer(StorageCell, byteOffset)` — a window position inside ONE
> shared character-storage cell (`StorageCell` = the Tier-B string backing lifted onto the heap; EXTERNAL
> records, ADDRESS-OF-taken items, and ALLOCATEd areas all use it). WHY not the closures: an accessor pair has
> no address identity for §8.8.4.2 STRUCTURAL pointer equality (two independent `ADDRESS OF X` must compare
> EQUAL) and no byte offset for §14.9.39 Format-10 arithmetic. `OverField`/`Cell` stay exactly what they are —
> the CALL-ABI carriers.

Crucially the carrier does NOT box WORKING-STORAGE: an ordinary `01 WS-X PIC 9(4)` stays a native `long`; a carrier
is built ONLY at a call site as an accessor over the caller's native field, so DISPLAY/MOVE/arithmetic keep zero
indirection. *(Rejected: a NEW parallel `CobolRef<T>` — violates singular-pattern; making every aliasable item itself
a heap cell — the byte-State sin in new clothes; C# `ref` everywhere — can't be stored in a LINKAGE field,
re-pointed by SET ADDRESS OF, or crossed over the opaque dynamic-CALL ABI; the legacy `ManagedPointer(byte[],offset,
length)` — the abandoned byte substrate.)* **`ManagedRef<T>.OverField(place.Read, place.Write)` is built FROM a
`Place`** — so it is not a third lvalue mechanism (§14.2).

### 9.2 Two-layer calling convention

- **Uniform opaque ABI** for dynamic + cross-assembly CALL: `interface ICobolProgram { int Call(CobolArgs args); void
  Cancel(); }` where `CobolArgs` = ordered `(PassMode, caller PicMeta, carrier)` — the typed analog of the rejected
  `Entry(ManagedPointer[])`. The callee maps positionally onto LINKAGE items.
- **Typed fast path** for same-assembly, statically-resolvable, PIC-conforming calls: a direct typed method
  `R = _SUB.Run(carrier…)`. RETURNING → the C# method return value (idiomatic, ISO-faithful). *(Designing the opaque
  ABI first makes the fast path a pure optimization, not a retrofit. Rejected: typed-signatures-only — dynamic/
  cross-assembly CALL would retrofit badly; trailing-scratch-buffer RETURNING — the byte ABI.)*

### 9.3 Program model + state matrix

Each program → its own **instantiable** (non-static) C# class; nested/contained → nested classes; run unit → one
assembly, first program = entry. State→storage: plain → instance fields on a cached singleton (last-used); INITIAL →
re-init WS to VALUE each activation; RECURSIVE/function/method → fresh instance per activation; LOCAL-STORAGE → re-init
per activation; EXTERNAL → one static run-unit holder per name (not reset by CANCEL); GLOBAL → field on the outer
class visible to nested classes; LINKAGE → carrier-bound (never initialized); CANCEL → re-init on next CALL. *(The
current single `static class Program` cannot host multiple/nested programs or recurse — this replaces it.)*

### 9.4 Hard problems / edge cases

- **BY REFERENCE category mismatch** (arg `PIC X(4)` seen as formal `PIC 9(4)`) is the ONE sanctioned transient-byte
  boundary: a scratch byte image the callee's `ManagedRef` decodes/encodes; same-category (the common case) is always
  fully typed.
- **Group BY REFERENCE:** the carrier round-trips the WHOLE struct per access (a value type; a closure copies on
  read) so subordinate mutations propagate as a unit; OCCURS args pass the `T[]` reference directly.
- **Args evaluated once at CALL time** (§14.9.4.4 GR3a): capture subscript/ref-mod into locals BEFORE building lazy
  carriers.
- **GOBACK in a called program = `ProgramReturn` caught at `Entry`** (§5.3 / §14.5) — not a competing mechanism; OO's
  "method return" and this "called-program return" are both just *what the `Entry` catch does*.
- Transitive passing mode (default BY REFERENCE; a phrase applies to all following args); OMITTED → `Null` carrier;
  EXTERNAL survives CANCEL; COMMON callable by siblings; a managed ref cannot serialize to stable bytes (reject
  pointer-to-file / REDEFINES-pointer-as-bytes as undefined).

---

## 10. OO

### 10.1 The model

ONE real C# class per CLASS-ID (`public class Foo : CobolObject` or `: Base`); the driver PROGRAM stays the static
`Program` class; instance fields per OBJECT data item; real C# methods per METHOD-ID; INVOKE → real C#
`new`/`obj.M(...)`/`base.M(...)`. **Let Roslyn perform cross-type binding, virtual dispatch, and conformance
checking** — the emitter's "two-pass" shrinks to building its OWN symbol table (class → method → {param modes,
return}) so INVOKE can marshal args + pick the call form. *(Rejected: port the legacy per-instance-ProgramState byte
model — owner-locked out + discards Roslyn's free type-checking; emit via Mono.Cecil — re-introduces the manual
cross-type registry the source target eliminates.)*

`CobolObject` is a runtime base class (every COBOL class derives from it directly or via its `INHERITS` chain) — a
single reflection-free home for universal/dynamic dispatch, NULL/`IS class` semantics, and a future EC-OO surface
(AOT/WASM-safe). *(Rejected: derive from `System.Object` — universal dispatch then needs reflection.)*

### 10.2 Storage scopes (two counterintuitive)

OBJECT-para WORKING-STORAGE → **INSTANCE** fields; METHOD WORKING-STORAGE → **STATIC** fields (ISO §11.7 GR5/§11.8:
method WS persists across activations, shared across instances — NOT per-instance); METHOD LOCAL-STORAGE → C# locals
(re-init each call); LINKAGE → method parameters; a method-local name SHADOWS object data. FACTORY → static
members/methods + static data.

### 10.3 INVOKE + attributes

`Class "New" RETURNING o`→`o = (C?)C__FACTORY.__Instance.__New()` (New is the standard class BASE's factory method,
§16.2 — present only in a class that INHERITS FROM BASE, COBOLNET2448 otherwise; its creation step is the generated
ctor: base ctor first, then VALUE-init own instance fields — OO deep-dive D4); `obj "M"`→`obj.M(args)` (virtual); `SELF "M"`→`this.M()` (virtual — runtime-class dispatch,
§8.4.3.8 GR2); `SUPER "M"`→`base.M()`; `Class "M"` (non-NEW)→static call; dynamic/universal (method-name in a data
item, or a universal `object?` receiver)→`recv.__CobolInvoke(name, args)` (a per-class switch — reflection-free).
Instance methods are `virtual` by default (COBOL forbids implicit hiding, §11.7 SR4a → never emit C# `new`); OVERRIDE→
`override`; FINAL→`sealed override`; ABSTRACT→`abstract`. BY REFERENCE → typed `ref` (§9.3.6 match-rule 3c requires
same class/category — so `ref` is conformant); RETURNING → C# return value. **Parametric polymorphism is DECLINED, not
merely deferred** — an OPTIONAL element (ISO **§9.3.5.3**, rule 7 "Parametric polymorphism is an optional feature in
this Working Draft International Standard"; Annex **A.4.10 item 3**) whose support WiseOwl COBOL does not claim: a duplicate
method name is refused BY NAME with `COBOLNET0822`, naming the facility and its A.4.10 item. (`PIC 9(4)`/`PIC 9(8)`
both map to `long`, so a signature-keyed resolution would collide in the emitted type anyway.) ⚠ The `§12063` this
paragraph and the two `COBOLNET0822` emit sites used to cite is **not a clause** — ISO/IEC 1989:2023 ends at §16 and
that number was a stray line anchor (`DESIGN-SPEC-RECONCILIATION.md` finding 44); all three now carry the
`cite.py --check`ed §9.3.5.3.

### 10.4 The correctness blocker (resolved)

**GOBACK in a method ≠ STOP RUN.** Throwing `StopRun` for BOTH — inside a method — would unwind past the INVOKE
caller and drop the RETURNING value. Resolution = the §5.3/§14.5 model: GOBACK/
EXIT METHOD → **throw `ProgramReturn` (carrying the RETURNING value), caught at the method's entry wrapper** (the same
signal/catch a called program uses, §14.5); STOP RUN keeps throwing the run-unit `StopRun`. A bare C# `return` is
WRONG here for the identical reason it is wrong at program level (§14.5): a method has its own `Dispatch` loop and
paragraphs, so GOBACK can sit inside a PERFORM (a recursive `Dispatch` call within the method), and a `return` would
exit only the innermost `Dispatch`, not the method.

### 10.5 Hard problems / edge cases

Multiple class inheritance (`INHERITS FROM {name}…`) — C# has only single inheritance → **v1 restricts to single
inheritance** (verified sufficient for the whole corpus), rejects 2+ bases LOUDLY (owner question — §15); per-instance
vs static field selection threads through the WHOLE statement emitter via the §14.1 Place owner-scope chokepoint;
INVOKE on null → EC-OO-NULL (guard before the call); the binder must add an ObjectReference item kind (currently
silently ignored). Object GROUP/OCCURS data → per-instance `record struct`/array, byte-identical to PROGRAM data
except instance-vs-static.

---

## 11. Conditions & exceptions

### 11.1 Two code shapes

(1) Conditions are PURE side-effect-free C# boolean expressions (`RenderCondition(node) → string`) so they compose
into `if`/`while(!(…))`/`?:`/EVALUATE arms/88 bool properties. The grammar's rule cascade (Or→Xor→And→unary→primary)
already encodes COBOL precedence NOT > AND > XOR > OR — preserved by construction. (2) The EC exception model is
stateful runtime + emitted guards that appear ONLY when a program uses the feature — **EC checking is OFF by default**
(ISO §14.6.13.1.1), so the typed-native fast path emits zero exception scaffolding in the common case.

### 11.2 Key decisions

- **Fully parenthesize every emitted binary boolean node** (`(a && b)`, `(a || b)`, `(a ^ b)`): C# precedence has `^`
  binding TIGHTER than `&&`/`||`, which does NOT match COBOL AND > XOR > OR — explicit parens make grouping match the
  parse tree.
- **Short-circuit `&&`/`||`** (the §7.3 corpus-safe divergence).
- **EVALUATE → chained if/else-if** (§5.3).
- **Level-88 → expression-bodied `bool` properties** derived from the live parent value (§3.5); SET cond TO TRUE
  moves the first VALUE; SET TO FALSE moves the WHEN SET TO FALSE literal (§14.9.39.4 GR6/GR7 — ONE store path
  with a `ToTrue` selector; COBOLNET2049 when the FALSE phrase is absent, COBOLNET2048 when §13.18.63.3 SR27
  makes literal-4 a TRUE value).
- **Class conditions** run over the character image via a new `CobolClass` runtime; for a pure numeric `long`,
  IS NUMERIC folds to `true` (revisit when REDEFINES/file aliasing lets it hold non-digits). ALPHABETIC is the closed
  Latin set `{A-Z,a-z,space}` (ISO §8.8.4.4) — NOT `char.IsLetter`.
- **Conditional phrases (ON SIZE ERROR / AT END / INVALID KEY / ON OVERFLOW / ON EXCEPTION) are ALWAYS active when
  written** and do NOT require `>>TURN`; `>>TURN` is resolved at COMPILE time (a `TurnState` walking the procedure
  division) and decides WHETHER an EC guard is emitted at all — OFF compiles to nothing.
- **USE…EXCEPTION/ERROR declaratives** → paragraph-methods + a compile-time registry keyed (EC/file/open-mode); the
  declarative is a pc range run by the generated `__RunUse`, which turns a RESUME's `ResumeSignal` into the
  `DispatchResult` the raise site reads (`Normal` fell off the end, `ResumeNext` RESUME NEXT STATEMENT, `NoHandler` none,
  `HandledNonfatal` handled nonfatal, `≥0` RESUME AT pc — the conditions deep-dive's "declarative dispatch-result protocol").
- The exception-checking **PERFORM…WHEN** form (M2) is the one place a real C# `try/catch` is used; RAISE/RESUME and
  fatal/nonfatal termination are runtime calls (`CobolException.Raise`; an unhandled fatal EC → `CobolFatalException`
  caught at `Main` → nonzero exit).

### 11.3 EC runtime

`CobolNet.Runtime.Exceptions`: `ExceptionCatalog` (generated from ISO Table 13 — level-3→level-2→EC-ALL hierarchy +
fatality), `ExceptionState` (last-exception register, EXCEPTION-OBJECT, file/location/statement), `CobolException`/
`CobolFatalException`, `ExceptionDispatch` (declarative registry). New diagnostics → a `COBOLNET07xx` band (§14.10).

### 11.4 Hard problems / edge cases

NEXT SENTENCE ≠ CONTINUE (labeled-block + goto, §5.3); IS NUMERIC sign-image validity (overpunch/separate); NOT
POSITIVE = ≤0 (includes zero); abbreviated combined conditions (expand subject+operator); ON SIZE ERROR leaves the
receiver UNCHANGED, ROUNDED before the size test (§6.6); whole-group comparison needs the §14.4 image facility.

---

## 12. Intrinsics, special registers & misc surfaces

### 12.1 Intrinsic catalog (the graded spine)

Reject porting the legacy `decimal`-typed signatures. Build ONE declarative `IntrinsicCatalog`: name → {ISO §15.2
function-type, result category, arity (fixed/optional-trailing/variadic), per-arg category, binding =
compile-time-fold | runtime-method}. §15.2's six types ARE the return-type column, mapped to the substrate:

- **integer-function → `long`** (`Int128` past 18 digits — FACTORIAL);
- **floating-point math (SQRT, trig, LOG/EXP, PI, STANDARD-DEVIATION, VARIANCE, ANNUITY, PRESENT-VALUE, RANDOM) →
  `double`**;
- **exact numeric (SUM, MEAN, MEDIAN, MAX/MIN-numeric, MOD, REM, INTEGER, INTEGER-PART, FRACTION-PART, ABS, SIGN,
  NUMVAL, NUMVAL-C, NUMVAL-F) → `CobolInt`** (so it flows straight into `TryStore` with the receiver's ROUNDED);
- **alphanumeric/national (UPPER-CASE, LOWER-CASE, REVERSE, TRIM, CONCAT [§15.18, 2023 — CONCATENATE is NOT an
  ISO function at any edition, PHASE-11 Step 7], SUBSTITUTE, CHAR, NATIONAL-OF, DISPLAY-OF, date strings) →
  `string`**; **boolean → `bool`** (BOOLEAN-OF-INTEGER on the D-B1 substrate).

Runtime homes: `CobolNet.Runtime.CobolIntrinsics` + `CobolNet.Runtime.CobolDate`. Mine the legacy `IntrinsicFunctions`
for BEHAVIOR only (NaN/out-of-domain → EC-ARGUMENT-FUNCTION default result; ORD-MAX/ORD-MIN tie = first; NUMVAL
parsing; date algorithms), not types. MAX/MIN are category-polymorphic (resolve by arg category at the call site);
`table(ALL)` expansion + variadic `params` port from legacy; **FUNCTION LENGTH folds at compile time from PIC
metadata** (and is kept distinct from LENGTH OF / BYTE-LENGTH, which count *bytes*).

### 12.2 Special-register registry

Model every special register as a SYNTHESIZED `DataItem` registered in `DataBinder.ByName` — so a register read/store
reuses the EXACT `CobolNum`/`CobolString` paths (zero special-case in the verb emitters). RETURN-CODE/TALLY/
SORT-RETURN → static `long`; WHEN-COMPILED → a compile-time constant string (timestamp injectable for determinism);
LENGTH OF/BYTE-LENGTH → a folded `long` byte-size from PIC+USAGE; ADDRESS OF → `ManagedPointer`; LINAGE-COUNTER/
LINE-COUNTER/PAGE-COUNTER + XML-*/JSON-* register NAMES are reserved by the registry but attach to their (scope-flagged)
subsystems. **The run-unit exit code is ONE canonical field** (`RunUnit.ExitStatus`) — written by the STOP RUN /
main-program GOBACK **WITH {NORMAL|ERROR} STATUS** phrase (§14.9.42.4 GR5 / §14.9.18.4 GR10) and by RETURN-CODE
when implemented; the setter flushes it to `Environment.ExitCode` AT THE WRITE SITE, so a status set by a
separately-compiled CALLed module crosses the assembly boundary to the process exit code (STOP RUN terminates the
whole run unit from anywhere — the flush cannot live in the main group's generated `Main`, which never sees the
sibling module's parse tree). A cross-subsystem contract (§14/§15); the DISTINCT RETURNING/GIVING activation
result does not feed it — decision 20.

### 12.3 Smaller surfaces

- **Figurative constants** = context-materialized sentinels resolved at each use site against the ASSOCIATED
  operand's category+width (§8.3.3.6.4 GR2, whose NOTE 1 makes a comparison an association exactly as a MOVE is;
  GR3 b/c is the complementary length-unspecified branch — one character, or literal-1's length); HIGH/LOW-VALUE
  per §14.9. The sizing itself is ONE function, `CobolString.FigToWidth`: the VALUE-clause and fixed-receiver
  sites fold it at compile time through `EmitText.RepeatToWidth`, and a RELATION CONDITION calls it at runtime
  through `CobolString.CompareFig`, because there the associated operand may be a reference-modified item
  (§8.4.3.3.4 GR5 — the unique data item's own positions) or a function result, whose character-position count is
  not a compile-time property of any PICTURE (kb/Work PB297).
- **INITIALIZE** = a compile-time tree-walk to per-elementary typed stores (default/VALUE/REPLACING; FILLER skipped;
  OCCURS → a for-loop). §14.9.20.4 GR4's "series of implicit MOVE or SET statements" is expanded at BIND time, and
  the lane keeps the standard's TWO rules apart: `Qualify` answers GR5c ("is this a receiving-operand, and WHY")
  and `SenderFor` answers GR6, whose three arms are keyed on that answer (kb/Work PB418).
  **The §14.9.20.3 OPERAND SCREENS are asked where the operands are resolved, and three of the four are ROUTINGS
  rather than checks** (kb/Work PB416). SR7 — "the data item referenced by identifier-1 is the receiving operand" —
  is a consequence, not a designation: identifier-1 therefore resolves through `ExpressionBinder.ResolveReceiving`,
  the ONE receiving chokepoint, and collects every receiving-operand prohibition living there (a CONSTANT RECORD's
  content §13.18.15.3 SR2, a constant-name §13.10.4 GR1, LINE-COUNTER, PAGE-COUNTER, the OCCURS DYNAMIC CAPACITY
  register, and an unimplemented receiver shape) so the NEXT prohibition is free. SR4's MOVE half —
  "a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the specified category as the
  receiving operand shall be valid" — is asked of **`MoveTable16`, the one home of the whole MOVE
  validity question** (§14.9.25.3 SR1's sending half, SR6, SR7, SR8, SR9 and SR10's Table 16), over
  `InitializeCategories.Table16Receiver`, which is the category-name's Table-16 receiving position; the rule needs
  no receiver walk because its receiving operand is the CATEGORY.
  **A THIRD STATEMENT ASKS THE SAME QUESTION OVER TWO DATA ITEMS, so the home has a composite item-keyed entry**
  (kb/Work PB391): §14.7.6 rule 2 — "In a MOVE statement, at least one of the data items is an elementary data
  item and the resulting move is valid according to the rules for the MOVE statement" — makes MOVE / ADD /
  SUBTRACT CORRESPONDING's pairing decision the MOVE statement's own validity, with no bound operand in hand (none
  exists for a pair until after the decision) and, crucially, NO DIAGNOSTIC: a pair whose move would be invalid
  simply does not correspond. `MoveTable16.DataItemRefusal(sender, receiver)` is that question in SR order — SR8,
  then SR9, then SR10's table — and `CorrespondingBinder.CorrRule2MoveValid` is ONE call to it, so the next MOVE
  syntax rule reaches CORRESPONDING without an edit. Its two constituents are each written once and read twice:
  `ShapeRefusal` has an item-keyed overload that the bound-operand entry DELEGATES to (SR7/SR6 stay at the bound
  entry, since a data item can never be a figurative constant), and `VariableLengthRefusal` is SR9's relation,
  framed for the written MOVE by `MoveBinder.MoveCategoryLegality` — which keeps only the MOVE statement's framing
  (which operand shapes unwrap to a data item, and COBOLNET1931) and still reads the ONE
  `VariableLengthCompatibility` module. Asking Table 16 ALONE was measurably wrong, because SR10 governs only
  "all other cases not described in Syntax rules 8 and 9": a `BINARY-LONG` namesake paired with a `PIC X(5)` one
  and overwrote it while the written MOVE of the same two items was COBOLNET0819, and a variable-length-group
  namesake paired with an elementary one and reached the run time. SR4's SET half stays
  `CheckInitializeReplacingSetCategoryAgrees`, and `InitializeLaneDriftTests` pins that every category-name is
  screened by exactly one of SR4's two paragraphs. SR1 (identifier-1's class) is one predicate over the one
  §8.5.2.1 Table-2 classifier — class INDEX is the single exclusion — written as its OWN screen because GR5a1's
  exclusion of an index item *contained in* identifier-1 is a different rule that must keep its silence. SR5's
  RENAMES check runs on the RESOLVED item. ⚠ One residue: §14.9.25.3 SR5's figurative→numeric prohibition is an
  EDITION row in `VersionConformancePass.GateMove`, re-derived from a bound MOVE this statement never builds, so
  `INITIALIZE … REPLACING NUMERIC DATA BY SPACE` is un-gated at 2023 where the explicit MOVE is COBOLNET0902.
  A receiver whose category
  is one of GR4's five — data-pointer, function-pointer, message-tag, object-reference, program-pointer — takes the
  implicit **SET** rather than the MOVE path: `InitializeSetNull` for GR6a1/GR6a2 and GR6c's NULL rows,
  `InitializeSetFrom` for GR6b's `SET receiver TO identifier-2`, each rendered exactly as the explicit SET statement
  renders that operand pair.
  **`category-name` IS A SET, not a word** (kb/Work PB415): §14.9.20.2's figure encloses THIRTEEN category names in
  a brace carrying CHOICE INDICATORS, so §5.2.6.4 makes a category-name one or more of them, each at most once —
  `REPLACING NUMERIC ALPHANUMERIC DATA BY …` is conforming COBOL-85 source. `InitializeCategorySet` (a bitmask over
  `InitializeCategory`) is that set, so §14.9.20.3 SR6's "the same category shall not be repeated in a REPLACING
  phrase" and §5.2.6.4's "only once" are ONE check over ONE accumulator, and the fourteenth word would be one enum
  member. The `{ALL | category-name}` before TO VALUE is a plain BRACE — mandatory (§5.2.6.3), COBOLNET1981 when
  omitted; `TO` and `THEN` are optional words in both phrases (not underlined). The eight post-85 category names are
  gated per word at the edition §8.9 reserves the word (`initialize-category-2002/-2014/-2023`), and
  `InitializeLaneDriftTests` pins the grammar rule, the enum and those bands against `reserved-words.json`.
- **SET** = dispatch by target kind (index→long, pointer→`ManagedPointer`/NULL, switch→bool, cond-name TO TRUE→store
  the 88's first VALUE) — depends on the §3.5 88/INDEXED-BY binding.
  **Format 15 (numeric-content, §14.9.39.2, 2014) is a STATEMENT SURFACE OVER AN EXISTING COMPUTATION, not a new
  value calculator** (kb/Work PB452). `SET CONTENT OF x … TO FARTHEST-FROM-ZERO / NEAREST-TO-ZERO` names exactly the
  quantity §15.43/§15.58/§15.83's HIGHEST-/LOWEST-/SMALLEST-ALGEBRAIC name — Annex D.32 says so outright — so the
  extremes live in **ONE evaluator, `AlgebraicRanges.Of`** (`Binding/AlgebraicRange.cs`), and both surfaces read it;
  the `AlgebraicRange` record carries `Farthest` / `FarthestNegative` / `Nearest` / `Zero` because the two surfaces
  select *different* members of the same computation (an unsigned item's LOWEST-ALGEBRAIC is 0, but its
  FARTHEST-FROM-ZERO SIGN NEGATIVE is its magnitude negated). The whole statement folds at BIND time to one store
  per receiver: a numeric literal through the ONE MOVE path for GR32/GR36, and a bit-exact carrier write for
  GR33–GR35's infinity and NaNs, whose encodings and A.1 item 176 payload live in `IeeeSpecials`. The
  IN-ARITHMETIC-RANGE clamp compares against `ArithmeticModes.IntermediateExtremes` — a table distinct from
  `IntermediateExponentRange`, which is a decade BOUND for a screen and would store an unrepresentable value if
  used here.
- **ACCEPT/DISPLAY system sources** = a `CobolSystem` runtime with an INJECTABLE clock (DATE/DAY/TIME/DAY-OF-WEEK/
  YYYYMMDD/YYYYDDD; DAY-OF-WEEK remap `((int)DayOfWeek + 6) % 7 + 1` = 1=Mon..7=Sun) + console UPON SYSOUT/SYSERR.
- **SPECIAL-NAMES implementor system-names** (§12.3.7.3 SR8; A.1 items 189/190/191) = ONE table,
  `Cobol.Net.Editions/ImplementorNames.cs` (kb/Work PB862): each row is a name, its ONE type (§8.3.2.3.1 — device / feature /
  switch), a device's input/output capability and a feature's WRITE ADVANCING rule. Both walkers of the
  switch-name/feature-name/device-name entry — `DataBinder.BindImplementorNameEntry` (switch mnemonics + status
  condition-names) and `Procedure.MnemonicRegistry` (the per-unit mnemonic map, OO scopes included) — classify
  through ONE reader, `ImplementorNameEntry`; an unavailable name is COBOLNET2241 at the declaration. The mnemonic
  map's value is the ROW, so every use is decided by the name's KIND: ACCEPT/DISPLAY (`MnemonicDevice`, SR7 +
  capability, COBOLNET0817), SET (switch only, §14.9.39.3 SR5), WRITE ADVANCING (feature only, §14.9.51.3 SR16,
  COBOLNET2243 — C01 = the PAGE advance, CSP = zero lines). A new name is a ROW; `ImplementorNamesDriftTests`
  keeps docs/CONFORMANCE.md §7 and the diagnostic text in step with the table. The entry's SHAPE (a required
  continuation; ON/OFF in either order, each once) is the grammar's (kb/Work PB716).
- **ALPHABET/CLASS/CURRENCY/DECIMAL-POINT IS COMMA** = a SPECIAL-NAMES config object threaded into emit (mostly
  compile-time); CURRENCY SIGN is a SET (symbol → string, r25's implied '$' included) whose per-picture string
  travels on `PicInfo.CurrencyString` — `COBOLNET_NUMERIC_DESIGN.md` D4's edge-case list carries the model.
- **SCREEN SECTION, JSON/XML GENERATE/PARSE** are scope-flagged big subsystems — designed only to the
  seam (reserve their register names, one-paragraph deferral each). Their scope is an owner question (§15).
  **REPORT WRITER is IMPLEMENTED**: the deep-dive
  is `docs/COBOLNET_REPORT_WRITER_DESIGN.md` — the `CobolReport` engine, compose-at-presentation lines, the
  LINE-/PAGE-COUNTER registers, CONTROL/SUM, USE BEFORE REPORTING; RW101A–RW104A byte-match.

---

## 13. The runtime (`CobolNet.Runtime`) — consolidated surface

The generated C# calls a small set of runtime classes (the roster is settled in §14.8):

| Class | Responsibility |
|---|---|
| `CobolInt` (record struct) | the Int128-monomorphic arithmetic carrier (Align/Add/Sub/Mul/Div) |
| `CobolNum` | numeric store/format: `TryStore`, `Rescale`, `FormatDisplay`/`FormatDisplaySigned`/`ParseDisplay`, `IsNumericClass` |
| `CobolEdit` | numeric-edited formatting (`Format(CobolInt, EditPattern, env)`) |
| `CobolString` | single-string ops: `Store`, `Compare`, `RefMod`, `SpliceInto` |
| `CobolStrings` | multi-operand INSPECT/STRING/UNSTRING (`InspectTally`/`InspectReplace`/`InspectConvert`/`StringInto`/`UnstringExtract`) |
| `CobolClass` | class-condition predicates (`IsNumeric`/`IsAlphabetic`/`IsAlphabeticUpper`/`IsUserClass`) |
| `CobolCond` | condition-name membership (`In`) |
| `CobolIntrinsics` / `CobolDate` | the FUNCTION library + date/time |
| `CobolSystem` | ACCEPT/DISPLAY system sources (injectable clock) |
| `ManagedPointer`/`ManagedRef<T>` | the ONE managed-reference carrier (§9.1 / §14.2) |
| `FileConnector<TRec>` / `IRecordCodec` / `CobolKey` / `RedefCodec` | files (§8) / REDEFINES Tier-C codec (§4) |
| `StopRun` / `ProgramReturn` / `CobolException` / `CobolFatalException` / `ExceptionCatalog` / `ExceptionState` / `ExceptionDispatch` | control-flow + EC signals (§5/§11) |
| `NotImplementedCobolFeature` | the loud-failure runtime guard (§1.4) |

Cross-subsystem runtime constants: the **storage-byte law** (`StorageByte` — byte 0xFF ↔ U+FFFF, every other byte k ↔ U+00kk) is the ONE
lossless 8-bit byte↔char carrier (files, REDEFINES Tier-C, whole-group image — §14.4/§14.9); the fixed-record file medium is its
ISO/IEC 8859-1 channel (`FileCharacterSet`), and a LINE SEQUENTIAL file is UTF-8 (D29).

---

## 14. CROSS-CUTTING CONSISTENCY (the reconciliation pass)

This section is the reason the SSOT exists. Where two subsystem designs picked different answers, the **loser is
named**.

### 14.1 ONE lvalue model: `Place` (used identically by every consumer)

`Place` (§3.3) is the universal typed-lvalue. `Place.Read()` / `Place.Write(rhs)` are consumed identically by:
**MOVE** (`dst.Write(srcImage)`), **arithmetic** (`receiver.Write(CobolNum.TryStore(...))`), **INSPECT/STRING/
UNSTRING** (`field.Write(CobolStrings.…(field.Read(), …))`), **files** (`READ INTO` = `Read()` + a typed group MOVE;
`WRITE FROM` = a MOVE + `Write()`), and **CALL/INVOKE BY REFERENCE** (`ManagedRef.OverField(place.Read, place.Write)`).
**Named losers:** string-ops' `StringLvalue` and the idea of a separate per-verb lvalue are SUPERSEDED — `StringLvalue`
IS a `Place`. The legacy `IrLocation(Area,Offset,Length)` is rejected (it IS the byte substrate).

### 14.2 The CALL-by-reference carrier is built FROM a Place (one carrier, one lvalue)

`ManagedRef<T>` (§9.1) is the ONE managed-reference carrier; it is NOT a second lvalue abstraction. At a call site,
`ManagedRef<T>.OverField(place.Read, place.Write)` wraps a `Place`'s closures. **Named loser:** there is no
`CobolRef<T>`. The carrier's PUBLIC name vs the legacy `ManagedPointer` is an owner question (§15) — the type is
typed-native (`ManagedRef<T>`), never the legacy `(byte[],offset,length)`.

### 14.3 The data-model lvalue model is used identically by MOVE / arithmetic / files / CALL — including REDEFINES

REDEFINES is reconciled to §4's **one-canonical-backing** model. The redefining views are `Place`s that compute over
the canonical backing, so MOVE/arithmetic/files/CALL touch them through the SAME `Read()`/`Write()` contract. **Named
loser (the single most important reconciliation):** data-model §8's "separate typed fields; a write to one is not
visible in the other" is SUPERSEDED by redefines/renames' 4-tier model, because separate fields reproduce the exact
silent-stale-read that triggered the blank-slate rewrite. (A write through a view IS visible through every other view of
the same class — Tier A/B accessors share the canonical; Tier C shares the class `byte[]`.)

### 14.4 ONE whole-group / materialize-to-image facility (G6)

> ✅ **THE RE-BASE HAS LANDED (V59, 2026-07-29): ONE BYTE REPRESENTATION AT EVERY BYTE BOUNDARY.** A fixed-point
> BINARY or PACKED leaf occupies its TRUE BYTES in the image — radix-2 two's complement (big-endian) or BCD, of
> exactly `PicInfo.StorageWidth` — everywhere the image is consumed: the whole-group codec, file records, SORT/MERGE
> key windows and the Tier-B REDEFINES backing. `CobolNum.FormatImage`/`ParseImage` (`CobolNum.Image.cs`) is the ONE
> codec; `DataItem.ImageWidth` and `DataItem.ByteWidth` are now two views of one fact, asserted by
> `ImageWidthIsStorageWidthTests`. The zoned digit image described below survives ONLY as USAGE DISPLAY's own byte
> form, and `PicInfo.ImageSignKind` is DELETED — a binary leaf's sign lives in its bytes, so there is no
> image-specific sign convention left to carry. **What is NOT the record image: an item's DISPLAY-statement text.**
> An ELEMENTARY numeric operand used as text still renders its DIGITS (§14.9.25.4 GR6 / §8.8.4.2.2 — "as though it
> were moved to an alphanumeric data item"), which is why `DataItem.DisplayTextWidth` exists beside `ImageWidth` and
> why `OperandText` decodes a non-zoned stored image before rendering it. A GROUP operand is the opposite case:
> §8.5.2.1 gives an alphanumeric group class and category alphanumeric and a usage of DISPLAY, so its character
> positions are the items' REPRESENTATIONS and its text IS the record image, bytes and all.
> **⚠ MIGRATION — THE ON-DISK RECORD LAYOUT CHANGED.** A data file written by a build BEFORE this carries a
> BINARY/PACKED field as its zoned digit characters; this build reads that window as radix-2 / BCD bytes. A
> fixed-length sequential file carries no self-description, so the failure would present as silent garbage. Detection
> where it is provable — a fixed-length file whose byte length is not a multiple of the declared record length —
> is plan §0 NEXT 1 step ⑤. Files written and read by ONE build are unaffected, and the pinned forms match IBM /
> Micro Focus / GnuCOBOL, so this build's files interchange where the previous ones did not.


There is ONE facility that turns a typed group/numeric into its alphanumeric image and back: a generated
`string AsImage()` (and `FromImage`) per `record struct`, used by (a) whole-group MOVE/compare, (b) INSPECT/STRING/
UNSTRING of a group/numeric operand (§7.4), (c) ref-mod of a numeric receiver, and (d) RENAMES THRU composition over a
heterogeneous span (§4.3). **Named loser:** the three names (`AsImage` / `GroupImage` / "materialize") are ONE thing
— canonical name `AsImage()`/`FromImage()`. It uses the storage-byte-law carrier (§14.9; D29). This is **transient** (built on
demand, never persisted) and is DISTINCT from REDEFINES Tier-C's PERSISTENT class-scoped `byte[]` (which would be the
only storage for a float/COMP-5/INDEX pun class). `AsImage` IS the permanent mechanism for
mixed-usage (DISPLAY+BINARY+PACKED) groups — the byte path remains only for the genuinely non-character-imageable
usages (see the total rule below).

**Mixed-usage (COMP-leaf) groups — the TOTAL rule.** The
standard leaves a binary item's representation to the implementor (§13.18.60.4 GR4 — "Each implementor specifies
the precise effect of the USAGE BINARY clause upon the … representation of the data item …, including the
representation of any algebraic sign"; GR11 the same for PACKED-DECIMAL; §8.5.2.1 — an alphanumeric group
item "has class and category alphanumeric" and "is treated as though it had a usage of display", so its
character positions are its leaves' representations, and §8.8.4.2.1 — "For comparison, an alphanumeric group
item shall be treated as an elementary alphanumeric data item"). ⚠ The third premise USED to cite §8.8.4.1.1,
a clause the standard does not have; kb/Work PB182 re-derived it as the §8.5.2.1 + §8.8.4.2.1 pair above, and
the rule is unchanged by the correction — both real clauses say what the phantom was standing in for. The typed-native backend DEFINES that representation, totally, and
it is ONE representation everywhere: **a fixed-point leaf's image IS its bytes** — zoned digits for USAGE DISPLAY,
radix-2 two's complement (big-endian) for BINARY, BCD with a `0xC`/`0xD`/`0xF` sign nibble for PACKED — of exactly
`PicInfo.StorageWidth` (`NumericByteForm`, §6.3). The generated `AsImage()`/`FromImage()` (gated on
`DataItem.IsImageCapable`) implements it for every consumer — whole-group MOVE/compare senders, WRITE/RELEASE,
READ/RETURN distribution, SORT/MERGE key decode, the Tier-B REDEFINES backing — through the ONE codec
`CobolNum.FormatImage`/`ParseImage` with the leaf's OWN `_P_` profile. There is no image-specific sign convention:
a binary leaf's sign is in its bytes, so `SignKind` (a DISPLAY concern) is not consulted, and DISPLAY-statement
output of the same leaf is unchanged (`OperandText` decodes a non-zoned stored image and renders the digits —
§14.9.25.4 GR6 treats an elementary numeric operand used as text as though it were MOVED to an alphanumeric item).
**Named losers:** (a) `OperandText.MixedGroupImage` (an inline concat) — RETIRED: it formatted a signed leaf with
its own `BinaryMinus` profile, a latent VARIABLE-WIDTH bug that would shift every following leaf for a negative
value, and it bailed on fixed-OCCURS children the generated codec handles; (b) leading-separate-sign images (buying
only debuggability); (c) **the fixed-width ZONED DIGIT image with a trailing-overpunch sign — the form this design
carried through P13 and V59 retired.** It was defensible on §13.18.60.4 GR4's representation latitude alone, but it
contradicted the compiler's OWN pinned widths: `BYTE-LENGTH(G) = 5` while `LENGTH(G) = 8` for one group
(§15.14.4 r1 vs §15.50.4 r3, which cannot disagree in a single-byte-character model), it made a conforming
program observe the disagreement with no file and no byte pun, and it put a `PIC 9(4) COMP` on disk as the ASCII
bytes `31 32 33 34`. Excluded — kept loud: float (no fixed decimal width), COMP-5 (`BinaryCapacity` stores values
beyond the PICTURE digit count, so its bytes and its picture disagree), INDEX (§13.18.60.4 GR10 — no image at all). A whole-group **MOVE between two mixed groups with
positionally IDENTICAL leaf layouts** (same usage/digits/scale/sign leaf-by-leaf) is still emitted as a **memberwise
leaf copy** (`MoveEmitter.AlignedLeafPairs`, tried FIRST) — for identical layouts the §14.9.25.4 GR4
representation copy and the memberwise copy are indistinguishable, and the memberwise path skips the encode/decode
round trip (the locked NC107A shape: `MOVE U5 TO U9`, `IF U22 > U12`). Non-aligned receivers fall through to
`FromImage` (ST127A's 10→11-leaf MOVE; ST134A's class-view→SD MOVE). Conformance:
`MixedUsageRecordImageDifferentialTests` + NIST ST108A/ST127A/ST133A/ST134A.

### 14.5 Control-flow PC × declaratives × EXIT × STOP RUN/GOBACK

- **Two exceptions, never integer-pc, for termination:** `StopRun` (run-unit, caught at `Main`) and `ProgramReturn`
  (program boundary, caught at `Entry`). **Named loser:** pipeline's "GOBACK → `return;` from the current Dispatch
  level" — a C# `return` exits only the innermost recursive `Dispatch`, so a GOBACK nested in a PERFORM would resume
  the PERFORM caller, not the program's caller. (Verify against any NIST test with GOBACK inside PERFORM.) OO's
  method-return and interprogram's called-program-return are both *what the `Entry` catch does* — not competing
  mechanisms.
- **Declaratives share the pc index space but are unreachable by fall-through:** ONE index space over ALL paragraphs
  (so every pc value agrees), `Main` starts at `EntryParagraphIndex`; a USE handler runs via `Dispatch(declStart,
  declEnd)` from the runtime I/O/error path and returns the int dispatch result (§11.2) — the same result a RESUME
  produces — one mechanism.
  **IMPLEMENTED with one refinement:** the handler invocation is the generated `__RunUse(id, start, end)`
  (a GR2 re-entrancy-guarded bounded `__Dispatch`) called from the generated `__IoCheck` selector
  emitted after every FILE STATUS store — selection is COMPILE-TIME knowledge (the program's USE set), so there is
  no runtime registry for local dispatch. The return is VOID only in an EC-FREE group (continue after the failing
  statement, GR7b); a group that uses the EC model gets the int-returning `__RunUse` and an int `__IoCheck` that hand
  the dispatch result back to the verb site (kb/Work PB141). One settled deviation: a successful
  CLOSE resets the connector's open-mode view to none (§9.1.4) — a failed OPEN records the ATTEMPTED mode for
  GR6b "being opened" scoping.
  **⛔ THE PAIR HANDED TO `__RunUse` IS THE DECLARATIVE SECTION'S OWN `PcRange`, AND NOTHING SHORTENS IT**
  (`BoundDeclarative.Range`, rendered at the ONE site `DispatchState.RunUseCall`). §14.9.49.3 SR1 makes the use
  procedure "the remainder of the section", §14.4.2 ends that section only at the next section header or END
  DECLARATIVES, §14.6.3 rule 1 puts the implied transfer back to the controlling USE at the last statement of the
  last procedure IN THE RANGE, and §14.9.14.4 GR7's NOTE places the USE return mechanism after the section's last
  paragraph. There WAS a "CCVS termination-tail accommodation" here — a trivial EXIT/CONTINUE paragraph followed
  anywhere later in the section by a STOP RUN / EXIT PROGRAM / GOBACK paragraph capped the handler end at the
  exit paragraph, so a user declarative written in that ordinary shape executed only in part on every selection
  path (kb/Work PB367). It is DELETED. It was never a fatal-path latitude question: it truncated the use
  procedure itself, before any of GR7/GR12/GR13 chose a return. Its one load-bearing corpus case was NIST
  SQ212A, whose declarative copies the whole CCVS termination boilerplate inside the section — measured as the
  only NIST program affected (348 of 349 unchanged); its golden had been self-baselined FROM the accommodation
  and is re-baselined to the conforming run, with `tests/nist/corpus.tsv` carrying the citation. **Cross-program GLOBAL dispatch (GR4b) is
  IMPLEMENTED (IC233A/IC234A)** as a compile-time `__outer` instance-chain walk — no runtime registry
  (local dispatch uses no runtime registry; ONE pattern): `__IoCheck`'s fallthrough (no
  local match, GR4a) calls `__outer.__RunGlobalUse(fileKey)`, which examines that container's `USE … GLOBAL`
  declaratives (file scope before mode scope, GR5), on a match runs the handler via its own `__RunUse` — in the
  DECLARING program's instance, its data (§8.4.6.2) — else forwards to ITS `__outer` ("repeated with the next
  higher directly containing source element", GR4b) or stops at the outermost. A contained program with NO local
  declaratives still emits `__IoCheck` + hooks when an ancestor has GLOBAL ones. The §12.4.5.8.4 GR1 NOTE-1
  corollary is implemented with it: a contained program's I-O on an inherited GLOBAL file stores the I-O status
  into the OWNER's (local-name) FILE STATUS item through the `__outer` chain
  (`EmitterState.InheritedStatusPlace`).
- **EXIT family is pure pc moves** (§5.2): EXIT PARAGRAPH/SECTION set pc; EXIT PERFORM/CYCLE map to break/continue in
  the inline-PERFORM loop; bare EXIT/CONTINUE are no-ops. They never touch the termination exceptions.

### 14.6 Dispatcher shape: `pc`-variable + `while` loop (pipeline's `goto case` is the loser)

The dispatcher uses `int pc; while ((uint)pc<(uint)N) switch(pc){…} pre-body atExit check` (§5.1). **Named loser:**
pipeline's `goto case N` for fall-through — it cannot express PERFORM-THRU exit detection (no clean named-exit
boundary) and C# forbids `goto` into another switch section. ALTER/computed-GO-TO/GO-TO-out all reduce cleanly to
"set pc" only with a pc variable.

### 14.7 `TryStore` (numeric) ≡ `StoreChecked` (conditions) — ONE method

The numeric design's `TryStore(CobolInt, NumProfile, mode, out stored) → bool` and the conditions design's
`StoreChecked(value, scale, profile, out sizeErr)` are the SAME operation (store + capacity/inexact check, receiver
unchanged on overflow). **Settle on `CobolNum.TryStore`** (returns `bool`, `false` = ON SIZE ERROR). The conditions
emitter calls it; there is no second checked-store.

### 14.8 Runtime class roster (settled — §13)

`CobolString` = single-string (Store/Compare/RefMod/SpliceInto); `CobolStrings` = multi-operand INSPECT/STRING/
UNSTRING. **Named loser:** the data-model design's `CobolString.RefMod/SpliceInto` and the string-ops design's
`CobolStrings.RefMod` — RefMod/SpliceInto live on `CobolString` (single-string); the multi-operand verbs live on
`CobolStrings`. Numeric store/format on `CobolNum`; edited formatting on `CobolEdit`; class predicates on `CobolClass`;
88-membership on `CobolCond`.

### 14.9 Figurative HIGH-VALUE/LOW-VALUE + the byte↔char codepage (settled)

- **HIGH-VALUE → U+FFFF, LOW-VALUE → U+0000, in both classes** (owner decision kb/Work R52; kb/Work PB1093): the extremes of the native
  sequence, which is the 65,536 UTF-16 code units for alphanumeric and national alike (§8.3.3.6.4 GR6/GR7), read from
  ONE member, `CobolNet.Runtime.NativeCollatingSequence`.
- **The storage-byte law is the ONE byte↔char mapping** (`CobolNet.Runtime.StorageByte`; COBOLNET_FILES_DESIGN D29):
  byte 0xFF is U+FFFF (HIGH-VALUE), every other byte k is U+00kk, a character's byte is its low byte. Every byte-form
  image (binary, IEEE, pointer, bit, national pairs), the whole-group image (§14.4) and the one-byte record medium
  (§8) use it, so U+00FF and U+FFFF share the byte 0xFF (the accepted cost, DOC-A.1-31). A LINE SEQUENTIAL file is
  UTF-8 text instead (kb/Work PB1760). Full custom-ALPHABET/CODE-SET collating fidelity sits on
  the char↔byte boundary deferred to G6; the API seam `CobolString.Compare(a, b, weights?)` is fixed now so call sites
  never change.

### 14.10 Diagnostic numbering (settled for new diagnostics)

New WiseOwl COBOL diagnostics use ONE band: **`COBOLNET07xx`** (conditions), and per-subsystem sub-bands within a single
`COBOLNET`-prefixed scheme (REDEFINES uses `COBOLNET_REDEF_*` *names* mapped into the numeric band). **Named loser:**
mixing the legacy `COBOLxxxx`/`CBLxxxx` codes into new diagnostics. Whether to additionally *reuse* legacy codes for
continuity with existing diagnostic-asserting tests is an owner question (§15) — but the default for new work is the
`COBOLNET` scheme.

### 14.11 Dialect, EC-default, and the differential oracle are threaded consistently

ONE `EditionInfo` (§2) is the single edition/dialect source; its sole gating consumer is the `VersionConformancePass`
(§1.1) — the binder is edition-agnostic save the documented exception ledger (DESIGN-version-conformance-pipeline §1.1) and the emitters carry no edition gating. EC checking is OFF by default
everywhere (§11.1); conditional phrases are always active. The differential harness (§2) uses the legacy as an oracle
until G8 — keeping the legacy build in the test graph for the duration is an owner question (§15).

---

## 15. OWNER-LEVEL open questions (consolidated; the task-named ones verbatim) — **RESOLVED: see §18** (kept for the option analysis / rationale)

> Deduped across all 11 designs. The five the task names explicitly — **REDEFINES representation, standard-vs-native
> arithmetic, file serialization, signed-DISPLAY overpunch, SCREEN/REPORT-WRITER/JSON-XML scope** — appear verbatim.

1. **REDEFINES representation (Tier-C byte[]).** Confirm the 4-tier model (§4) with a PERSISTENT class-scoped `byte[]`
   canonical for genuine mixed-USAGE byte-puns as the accepted realization of "bytes only at a boundary"
   (**recommended** — Tiers A/B stay 100% typed and cover the entire near-term DISPLAY-homogeneous NIST path, so the
   fork is off the critical path), OR mandate rejecting mixed-USAGE REDEFINES loudly (collapse Tier C into Tier D),
   trading real-program coverage (X-over-COMP layouts are common in production) for zero-byte purity.

2. **Standard-vs-native arithmetic.** v1 ships **NATIVE** arithmetic only (`Int128` fixed-point, §6.2 — fully
   conformant as the default, what the 364-NIST corpus uses). `ARITHMETIC IS STANDARD-DECIMAL` (decimal128, 34
   digits) is incompatible with the locked substrate AND with .NET `decimal`. DECISION NEEDED: (a) permanently scope
   WiseOwl COBOL to native arithmetic (**recommended**), or (b) later add a quarantined decimal-float intermediate type
   usable ONLY under `ARITHMETIC IS STANDARD-DECIMAL`. STANDARD-BINARY is spec-obsolete → stays unimplemented. Also
   confirm `DIV_GUARD_DIGITS = 14` against the NIST division-rounding tests in G5.

3. **File serialization format.** The on-disk format for relative/indexed/variable-sequential files is an internal
   framed convention (4-byte little-endian prefix, `0xFF` gaps), NOT a standard interchange format. (Q-file-1) Keep for
   v1 + add a pluggable file-format provider later, or does commercial quality demand an interoperable format (real
   ISAM / GnuCOBOL-compatible)? (Q-file-2) The relative/indexed connector holds the whole file in memory (byte-image
   payload per record, deserialized only on hand-back) — accept the legitimate-confined-file-bytes framing, or switch
   to an all-typed sorted-dictionary-of-typed-records store (a one-line flip)? (Q-file-3) Scope v1 to in-memory load/
   flush vs a later pluggable on-disk B-tree/SQLite backend?

4. **Signed-DISPLAY overpunch convention.** ✅ **DECIDED BY THE OWNER, 2026-09-09 (kb/Work PB803, questions 28 and
   29): "Keep both behind an option with the default being IBM and Micro Focus compatibility."** The two surveyed
   conventions are the compile option `--sign-encoding=ibm|ascii`, default `ibm` (`{`/`}`, `A-I`/`J-R`) with
   TRAILING overpunch as the no-SIGN-clause position (§6.4). Annex A.1 items 177 and 178 are documented at
   `docs/CONFORMANCE.md` DOC-A.1-177 / DOC-A.1-178; the determination is the numeric deep-dive's D5. Never re-ask.

5. **SCREEN / REPORT WRITER / JSON-XML scope.** These are designed only to the seam (registers reserved, deferred to
   their own subsystems — §12.3). Confirm they are NOT part of the near-term graded deliverable, and the intended M2/
   M3/M4 ordering (REPORT WRITER is a COBOL-85 feature the legacy fully implements; SCREEN + JSON/XML are 2002/2014).

6. **Program/assembly model.** Compile the whole run unit to ONE assembly (typed fast path available everywhere,
   nested = nested classes — **recommended**) vs support separately-compiled `<name>.dll` programs (the uniform opaque
   ABI becomes mandatory across assemblies, the typed fast path unavailable). This decides how much of the calling
   convention can be fully typed. (Raised by both pipeline and interprogram.)

7. **Managed-pointer naming.** Confirm the carrier may be the typed-native `ManagedRef<T>` (NOT the legacy
   `(byte[],offset,length)` `ManagedPointer`). Keep the PUBLIC name `ManagedPointer` over the typed carrier (the
   owner's prior choice), or rename to `ManagedRef`? The `feedback_one_mechanism_per_job` memory text still describes the
   byte form (the abandoned byte-substrate era).

8. **Multiple class inheritance (OO).** v1 restricts to single inheritance (sufficient for the whole corpus) and
   rejects 2+ `INHERITS FROM` bases loudly. When a multi-base program appears: (a) linearize to one C# base + extract
   secondary supers as C# interfaces the class IMPLEMENTS (with member forwarding), or (b) declare it unsupported.

9. **Int128 substrate timing.** `PicInfo.WidePrecision` + `CobolNum`/`CobolInt` `Int128` overloads are needed before
   any 19–38-digit picture compiles. Is there a corpus program needing >18 digits in the early NIST waves (NC/SM/IC/
   IF), or can `Int128` wait until a later wave? (Resolved in the §16 sequence as a checkpoint, not a floating TODO.)

10. **COMP-5 binary-wrap semantics.** Confirm true two's-complement binary-WRAP by width (`PIC S9(4) COMP-5` wraps at
    ±32768) vs digit-count truncation. §6.3 specifies binary-wrap; the current `CobolNum.Store` has a digit-truncation
    TODO.

11. **Whole-group `AsImage` permanence.** Is the generated `string AsImage()` (clean, readable) acceptable as the
    PERMANENT typed-native mechanism for whole-group MOVE/compare, or must mixed-usage (national/COMP-member) groups go
    through the G6 byte path for byte-exact fidelity? (Pure DISPLAY groups are fine via `AsImage`.)

12. **Diagnostic numbering scheme.** Adopt the fresh `COBOLNET`-prefixed scheme for all new diagnostics (the §14.10
    default), or also reuse the legacy `COBOLxxxx`/`CBLxxxx` codes for continuity with existing diagnostic-asserting
    tests?

13. **Differential-oracle dependency.** Is it acceptable for the conformance/CI harness to DEPEND on the legacy
    compiler as a differential oracle until cut-over (G8)? (Keeps the legacy build in the test graph; the alternative
    is `.txt`-oracle-only, losing the free 364-program diff net.)

14. **Fatal-EC termination policy.** ISO §14.6.13.1.3 lets the implementor continue or terminate an unhandled fatal
    EC. Recommendation: terminate the run unit with a diagnostic + nonzero exit (commercial-quality, safest).

15. **Out-of-range / zero / non-integer reference modification.** Default = THROW (raise EC-BOUND-REF-MOD →
    `CobolException`; conformant, since results are otherwise undefined). Offer a lenient dialect that clamps?
    (Owner-gated, mirrors the legacy dialect-strictness model.)

16. **REDEFINES cross-type-read detection precision.** The loud guard (§4.3) needs a "cross-type read" definition that
    does not false-positive on the safe alias pattern. Proposed conservative over-approximation: flag any program that
    both writes AND reads two different-typed views of one redefined region (routes more programs to the byte fallback
    than strictly necessary). Acceptable for v1?

---

## 16. Dependency-ordered implementation sequence (G1–G8, each NIST-testable)

Threads the existing G1–G8 spine; each step ends at a checkpoint testable against the differential harness (§2). The
cross-design prerequisites the subsystem designs flagged are surfaced inline.

**STATUS:** G0 ✅ · G1 ✅ · G2 ✅ · G3 ✅ · G4 ✅ · G5 ✅ (files / interprogram / SORT) · G6 ✅
(REDEFINES/RENAMES 4-tier, AsImage, ON SIZE ERROR, PICTURE P). G7 (per-edition correctness) is IN PROGRESS; G8
(cut-over) pending. The rearchitecture roadmap (`docs/COBOLNET_REARCHITECTURE_PLAN.md`) is the go-forward plan and
carries the live resume point (its §0 banner); default `--std` = COBOL-2023.

### G1 — Bootstrap ✅ (done)
HELLO end-to-end (preprocess→parse→emit C#→Roslyn→run); DISPLAY of literals; STOP RUN.
**Checkpoint:** the differential harness runs at all; `oo_hello`-class trivial programs.

### G2 — Bind + data model + the universal abstractions (the foundation everything else needs)
- **Pre-1 (owner-gated):** resolve **Q9 Int128 timing** and **Q10 COMP-5 wrap** before flipping any typed numeric.
  Add `Int128`/`CobolInt` overloads to `CobolNum` (the monomorphic value engine, §6.1) and `WidePrecision` to
  `PicInfo`.
- Build the **bound tree** (§2) + `ProcedureBinder`.
- Build **`ReferenceResolver` → `Place`** (§3.3/§3.4) — port the legacy SUB_* interpreter; this is the lvalue every
  later verb uses, so it MUST exist before G3.
- **`DataBinder` must stop dropping** REDEFINES, level-66, level-88; build the `ByName` MULTIMAP; collect OCCURS dims
  + INDEXED-BY index-names; add the deferred-resolution pass for REDEFINES/RENAMES/DEPENDING-ON targets.
- **`NumProfile` gains `SignKind`** (§6.4) and `CobolNum` gains `FormatDisplaySigned`/`ParseDisplay` — needed by
  DISPLAY of signed items AND by §4 numeric views.
- Emit nested `record struct` types + composed VALUE initializers + arrays (replacing the flatten-to-leaves
  stopgap). **Correct the architecture-doc §3 `decimal` rows** (§3.1) in this same change set.
**Checkpoint:** a program with groups/tables/signed-DISPLAY/88s binds and DISPLAYs its data byte-identically to the
legacy (a slice of NC).

### G3 — Core verbs on typed values (everything routes through `Place` + `CobolNum.TryStore`)
- MOVE (incl. JUSTIFIED, numeric↔alphanumeric image), arithmetic (the `CobolInt` engine + `TryStore`, ROUNDED, ON
  SIZE ERROR two-phase §6.6), IF/EVALUATE (pure conditions §11, fully-parenthesized short-circuit), DISPLAY/ACCEPT
  (system sources §12.3 with the injectable clock), inline PERFORM (real loops §5).
- `CobolStrings` INSPECT/STRING/UNSTRING + `CobolString.RefMod`/`SpliceInto` (§7); `CobolClass` class conditions;
  `CobolCond` 88-membership; `CobolEdit` numeric-edited.
**Checkpoint:** the bulk of NC (the arithmetic/MOVE/IF/EVALUATE/PERFORM-inline/INSPECT/STRING NIST programs) green.

### G4 — Control-flow engine (the PC dispatcher)
- `Dispatch(start,exit)` (§5.1, the `pc`-variable+loop shape §14.6); fall-through/GO TO/GO TO DEPENDING/ALTER;
  out-of-line PERFORM/THRU (recursive bounded dispatch); PERFORM TIMES/UNTIL/VARYING(+AFTER reset); EXIT family;
  NEXT SENTENCE; declaratives at low pc indices + `EntryParagraphIndex`.
- **`StopRun` vs `ProgramReturn`** distinct signals (§5.3/§14.5) — even though CALL lands in G7, define both now so
  GOBACK is never miscompiled.
**Checkpoint:** the GO TO/ALTER/PERFORM-THRU/inverted-range NIST programs (NC102A, NC208A, etc.) green.

### G5 — Drive the NIST corpus to green (NC → SM/IC/IF → SQ/RL/IX/ST) ✅ COMPLETE
- Files subsystem (§8): `FileConnector`/`IRecordCodec`/`CobolKey`; sequential → relative → indexed; FILE STATUS,
  AT END/INVALID KEY, USE declaratives (dispatched via §14.5); SORT/MERGE.
- Interprogram (§9): the opaque ABI + typed fast path; LINKAGE/USING/RETURNING; CALL/CANCEL; EXTERNAL/GLOBAL/COMMON;
  pointers (`ManagedRef`/ADDRESS OF/BASED/ALLOCATE).
- Confirm **Q2 `DIV_GUARD_DIGITS`** empirically here.
**Checkpoint MET:** every golden-bearing NIST program locked byte-exact (318 = 93 NC + 29 ST + 32 RL + 40 IX +
23 IC + 69 SQ + 42 IF + 15 SM + 4 RW + 2 OBSQ); the final census is 357/403 GREEN with zero diffs (residue is
golden-less by NIST design). Includes the intrinsic catalog (§12.1), COPY/SM, LINAGE, and the Report Writer
(`COBOLNET_REPORT_WRITER_DESIGN.md`). Goldens that fossilized verified legacy holes were re-baselined to the
ISO-conforming output under the `LEGACY_DIVERGENT` protocol (`scripts/guard.sh` carries the list + citations).

### G6 — Deferred data cases (the byte-boundary islands) ✅ COMPLETE
- REDEFINES/RENAMES 4-tier model (§4) — Tiers A/B (no bytes) first; Tier C resolved as the ZONED DIGIT-IMAGE
  codec (§4.2 — no `RedefCodec` byte plan needed; ISO §13.18.60 GR4 implementor latitude; Q1 thereby settled:
  the island narrowed to float/COMP-5 puns, which stay loud).
- The whole-group **`AsImage()`/`FromImage()`** facility (§14.4) — wired into whole-group MOVE/compare, file
  records, and SORT, including fixed-point BINARY/PACKED leaves.
- File-record serialization edge cases (variable-length §13.18.43 end-to-end with length framing, multi-01
  overlay, SAME RECORD AREA) finalized; CODE-SET remains accepted-inert (no NIST exercise).
**Checkpoint MET:** the REDEFINES/RENAMES/whole-group-compare programs + all deferred file cases green.

### G7 — Per-edition correctness: features AND diagnostics per `--std` (85|2002|2014|2023; default 2023)

> ⛔ **Four compilers in one executable.** Every feature carries TWO co-equal obligations (owner, 2026-06-10):
> (1) the complete ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that
> LACKS it (not-yet-introduced or removed). Gating is driven by `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row
> edition-change checklist) and validated by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` — test
> the compiler as N per-edition compilers; Phase 0 done). NIST-85 is the 85 positive corpus; the negative
> (rejected-construct) corpus is NEW.
> ✔ Reconciled (ALTER family): ALTER + the target-less GO TO are 85-only — REJECTED at
> `--std 2002|2014|2023` as deleted elements (COBOLNET0810/0811, matching the ISO history and the legacy
> CBL3601/3605); §18 #10 and the control-flow deep-dive edition table updated in the same change set.

- OO → .NET classes (§10): single-inheritance, NEW/INVOKE/SELF/SUPER, instance vs static scopes, FACTORY, PROPERTY;
  the conformance corpus (`tests/conformance/<ver>/`).
- Full intrinsic catalog (§12.1); EC exception model (§11: `>>TURN`, RAISE/RESUME, USE…EXCEPTION, PERFORM…WHEN);
  national/boolean; UDF; per the `docs/ISO2023_CONFORMANCE_PLAN.md` §3 catalog. Resolve **Q5** scope for SCREEN/RW/
  JSON-XML.
> ✔ **G7 execution detail is now `docs/COMPLETION_ROADMAP_COUNCIL.md` (RATIFIED 2026-07-03;
> ISO-VALIDATED against the spec — 0 refuted claims; its Appendix carries the audit + the
> amendments: boolean operations, `&`-concatenation, CONSTANT entries, DYNAMIC-LENGTH items,
> the §4.2.16 full conformance-documentation set, >>PROPAGATE re-editioned ≤2014)** —
> Phases 0–8 with exit criteria. Ratified amendments to THIS section: **JSON/XML is removed from the ISO
> catalog** (zero hits in the 2023 spec — re-tagged vendor-dialect, deferred post-G8; the Q5 JSON-XML leg is
> thereby resolved, SCREEN = documented non-support per the A.4 line, RW stays as implemented); a behavioral leg at
> `--std 2023` (318-golden re-run) attaches to the validator's permissive flip and INV-1-strong at the default
> edition joins the G7 exit criteria; new named workstreams (W1.5 gate-diagnostic upgrades, the silent-misbind
> loud-guard sweep, the 43-row intrinsics backlog, the ~44 VCR Table-1/5 behavior-row wave, discovery runners,
> CI sweep wiring, the ISO §4.2 conformance document).
**Checkpoint:** the conformance corpus + the post-85 dialect-gated NIST programs green.

### G8 — Cut over
Retire the byte engine, drop the legacy from the test graph (resolve **Q13**), rename `CobolSharp`→`WiseOwl COBOL`
(exe `cobol.exe`), final architecture/doc pass.
> ✔ **Ratified refinement (2026-07-03, `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 9):** G8 executes as THREE
> serial cuts — Cut 1 test-graph (Q13 resolved: the ~47 differential files convert to pinned goldens; the CI
> guard step swaps to the in-repo greenfield guard), Cut 2 byte-engine deletion (legacy preserved at a git
> tag), Cut 3 the atomic rename + committed regen + final doc pass + the ISO §4.2 conformance document. A
> greenfield-guard vs legacy-guard **equivalence proof (roadmap Phase 8) is a hard precondition of Cut 1** —
> the legacy must still run when the verdict-diff executes.
**Checkpoint:** full guard green on WiseOwl COBOL alone; the architecture doc + this SSOT reconciled.

---

*End of SSOT. Every subsystem decision in the 11 input designs is captured above; every named conflict is resolved
with the loser stated (§14); the five task-named owner questions appear verbatim (§15 items 1–5). Implementation
follows §16.*

---

## 17. Project organization, rename & code-structure plan

> Scope of this section: the target solution/project layout, the front-end extraction, the rename, the no-god-class structural rules, and the C# 14/.NET 10 usage guidelines. It expands `COBOLNET_ARCHITECTURE.md` §5 (which currently sketches the layout in three bullets) into the decision-complete plan. Implementation status: **G0 is DONE** — the tree now IS `src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}` + `tests/Cobol.Net.Tests.{Unit,Conformance}` (Steps 1–5 below were executed; the tables are kept as the executed record + rationale). The namespace big-bang + legacy deletion (Step 6) lands at **G8** cut-over.

### A. Findings that drive every decision below

These were verified against the live tree (2026-06-08), not assumed:

1. **The front-end is assembly-cleanly separable.** `CobolNet` consumes exactly four legacy namespaces — `CobolSharp.Compiler.Diagnostics`, `.Generated`, `.Parsing`, `.Preprocessor` (plus `.Common` transitively). The dirs `Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/` have **zero** `using` references to the legacy `Semantics`/`IR`/`CodeGen`/`FlowAnalysis` layers, and the front-end does not reference `CobolSharp.Runtime`. So the front-end can be lifted into its own assembly with no code edits to the moved files.
2. **The dependency the task wants killed is an *assembly* dependency, not a namespace dependency.** Moving `*.cs` files into a new `.csproj` ends the new compiler's reference to `CobolSharp.Compiler.dll` **without renaming a single namespace** — namespaces are independent of the project that compiles them. This lets us split "physical move" (G0) from "cosmetic namespace rename" (G8) and stay green throughout.
3. **The legacy byte engine must keep parsing.** `COBOLNET_ARCHITECTURE.md` keeps `CobolSharp.Compiler` alive as a differential oracle until G8. After extraction, the *legacy* engine, the legacy CLI, and **both test projects** (all four currently reference `CobolSharp.Compiler.csproj`) must repoint to the new Frontend assembly for parsing/diagnostics. The front-end namespaces are consumed compiler-wide by the legacy `Semantics`/`IR`/`CodeGen` (every parse-tree `CobolParserCore.*` context; `DiagnosticBag` everywhere) — which is *why* a namespace rename has a wide blast radius and is deferred.
4. **The front-end is more than `.cs`.** It includes the grammar-generation machinery: `Grammar/` + `Grammar/Core/*.g4`, `GenerateIfNewer.ps1`, `Invoke-Antlr4CSharp.ps1`, the `ANTLR4/antlr-4.13.2-complete.jar`, the `EnsureGeneratedFiles`/`CleanGenerated` MSBuild targets, and the `Generated/` output. These move as one unit; the generated namespace is set by the generation script and is held constant through the move.

---

### 1. Solution / project reorganization + rename

### 1.1 Target project set (exact names)

| # | Project (assembly) | Kind | `RootNamespace` | `AssemblyName` | Purpose |
|---|---|---|---|---|---|
| P1 | **`Cobol.Net.Frontend`** | library | `CobolNet.Frontend` | `Cobol.Net.Frontend` | Preprocessor + ANTLR lexer/parser + parse-tree + diagnostics. Extracted from `CobolSharp.Compiler`. The single front-end for both the new compiler and (until G8) the legacy oracle. |
| P2 | **`Cobol.Net.Compiler`** | library | `CobolNet` | `Cobol.Net.Compiler` | Bind → lower → emit C# → Roslyn backend. The compiler proper, minus the CLI shell. |
| P3 | **`Cobol.Net.Cli`** | exe | `CobolNet.Cli` | **`cobol`** | Thin command-line driver (`Main`, arg parsing, file orchestration). Produces **`cobol.exe`**. |
| P4 | **`Cobol.Net.Runtime`** | library | `CobolNet.Runtime` | `Cobol.Net.Runtime` | The typed-native runtime the *generated* programs call (`CobolNum`, `CobolString`, `NumProfile`, `ManagedPointer`, file/format helpers). |
| T1 | **`Cobol.Net.Tests.Unit`** | xUnit | `CobolNet.Tests.Unit` | — | Unit tests for the new compiler + runtime. |
| T2 | **`Cobol.Net.Tests.Conformance`** | xUnit | `CobolNet.Tests.Conformance` | — | NIST + post-85 conformance corpus, run against the new compiler. |

**Decision — name form.** Assembly/package/folder names use the dotted product brand **`Cobol.Net.*`** (reads as the product "WiseOwl COBOL"); **root namespaces stay the single token `CobolNet`** (e.g. `CobolNet.Frontend`, `CobolNet.CodeGen`). Rationale: dotted `Cobol.Net.*` is the marketing/NuGet identity; `CobolNet` as the namespace root avoids a clash with the `.Net`/`System.Net` reading and keeps `using CobolNet.CodeGen;` clean. One rule, applied consistently. (Owner may prefer `Cobol.Net` namespaces too — trivially flippable since it is just the `<RootNamespace>` value; not load-bearing.)

**Decision — CLI split (P2/P3).** Today `Program.cs` lives *inside* the exe project, so tests cannot reference the compiler without referencing an exe. Split it: `Cobol.Net.Compiler` (library, everything except the CLI shell) + `Cobol.Net.Cli` (exe, `<AssemblyName>cobol</AssemblyName>`, ~120-line driver). This mirrors the proven legacy `CobolSharp.Compiler`/`CobolSharp.CLI` split and lets the test projects reference a library.

**Decision — Diagnostics/Common placement.** Fold `Diagnostics/` and `Common/` into `Cobol.Net.Frontend` (a `Diagnostics/` folder + a `Common/` folder) for v1. They are small (4 + 3 files), have no independent consumer, and a separate `Cobol.Net.Diagnostics` would be premature. Revisit only if a non-frontend consumer of diagnostics appears.

### 1.2 Folder layout per project (folder = subsystem)

```
src/Cobol.Net.Frontend/
  Cobol.Net.Frontend.csproj          (carries the ANTLR codegen targets)
  Grammar/         CobolParserCore.g4, CobolDialect/OO/JsonXml/Generics.g4, CobolPreprocessor.g4
    Core/          CobolLexer.g4, CobolData/ControlFlow/Expressions/IO/OO/ReportWriter/Screen/SpecialNames.g4
  Generated/       CobolLexer.cs, CobolParserCore.cs, *Visitor.cs  (build output; git-ignored or tracked per current policy)
  Parsing/         CobolParserCoreBase.cs, CobolErrorListener.cs, CobolErrorStrategy.cs, ZeroTokenRewriter.cs
  Preprocessor/    ReferenceFormatProcessor.cs, ConditionalCompilationProcessor.cs, CopyProcessor.cs, NistPreprocessor.cs
  Diagnostics/     Diagnostic.cs, DiagnosticBag.cs, DiagnosticDescriptors.cs, DiagnosticSeverity.cs
  Common/          SourceText.cs, SourceLocation.cs, TextSpan.cs
  ANTLR4/          antlr-4.13.2-complete.jar
  GenerateIfNewer.ps1, Invoke-Antlr4CSharp.ps1
  Pipeline/        Frontend.cs        (the orchestrator — MOVED from src/CobolNet/Frontend/, the one client of all of the above)

src/Cobol.Net.Compiler/
  Cobol.Net.Compiler.csproj
  Binding/         DataItem.cs, DataBinder.cs, PicInfo.cs, (later: FileBinder, LinkageBinder, OoBinder, ConditionNameBinder)
  Lowering/        (G3/G4: the C#-oriented model — control-flow normalization, CORR expansion, etc.; mirrors legacy Lowering/)
  Emit/            EmissionContext.cs, CSharpProgramEmitter.cs, + one emitter per statement-family (see §2)
    Numerics/      NumericExprRenderer.cs (the NumX machinery), ScaleMath.cs
    Conditions/    ConditionRenderer.cs, ComparisonRenderer.cs
  CodeGen/         CodeWriter.cs, RoslynBackend.cs, ReferenceAssemblies.cs, RuntimeConfigWriter.cs
  CompilerDriver.cs                  (the library entry: source path → result; what Program.Main calls)

src/Cobol.Net.Cli/
  Cobol.Net.Cli.csproj               (<OutputType>Exe</OutputType>, <AssemblyName>cobol</AssemblyName>)
  Program.cs                         (Main + arg orchestration)
  CliOptions.cs                      (the parsed-options record — extracted from Program.cs)

src/Cobol.Net.Runtime/                (role-based folders — DESIGN-runtime-library §2; namespaces stay
  Cobol.Net.Runtime.csproj             CobolNet.Runtime[.IO|.Exceptions] until the G8 flip)
  Values/
    Numeric/   CobolNum.cs, CobolDec.cs, CobolFloat.cs, CobolEdit.cs, CobolRounding.cs, CobolSizeError.cs,
               NumProfile.cs, Pow10.cs (the ONE power-of-ten table)
    Text/      CobolString.cs, CobolBool.cs, CobolClass.cs
    Tables/    CobolTable.cs, CobolDynTable.cs
  Verbs/       CobolStringOps.cs (STRING/UNSTRING), CobolInspect.cs (INSPECT)
  IO/          FileConnector.cs (abstract base: §9.1.13 status machine + read-position pair) +
               SequentialConnector/RelativeConnector/IndexedConnector, FileRegistry.cs (ONE polymorphic
               registry), RecordFraming.cs (the ONE 4-byte-LE framing), CobolFile.cs (static facade →
               RunUnit.Current.Files), FileStatus.cs, FileSupport.cs, Clock.cs (IClock/SystemClock),
               AcceptSource.cs, CobolSort.cs, ReportWriter.cs, Sharing/PhysicalFileTable.cs
  Control/     RunUnit.cs (the ONE run-unit-state owner: Programs/Exceptions/External/Modules/Switches/
               Files/Clock, ambient AsyncLocal), ProgramTable.cs (+ ProgramRegistry.cs static shim),
               ExternalTable.cs (+ ExternalStore.cs shim), ModuleStack.cs (+ CobolModule.cs shim),
               SwitchStore.cs (+ ExternalSwitches.cs shim), ManagedPointer.cs, CellPointer.cs,
               StorageCell.cs, CallAbi.cs, CobolObject.cs, CobolPtr.cs, CobolInvokeArg.cs,
               Signals/ StopRun.cs, ProgramReturn.cs, MethodReturn.cs, ResumeSignal.cs, NotImplemented.cs
  Exceptions/  ExceptionState.cs (ExceptionEngine instance + ExceptionState static shim),
               ExceptionCatalog.cs (immutable — legitimately static), EcFunctions.cs, CobolFatalException.cs
  Intrinsics/  CobolIntrinsics.{cs,Exact,Float,Text}.cs, CobolDate.cs
```

> The runtime subsystem folders (`Numeric/`, `Text/`, …) mirror `COBOLNET_ARCHITECTURE.md` §3's data-model rows, so a reader maps "COBOL national string" → `Text/` and "USAGE POINTER" → `Pointers/` directly.

### 1.3 Complete item-by-item mapping

| Current item | Verb | Destination |
|---|---|---|
| `src/CobolNet/Program.cs` | **split + move** | `Cobol.Net.Cli/Program.cs` (Main + Run); the `CliOptions` record → `Cobol.Net.Cli/CliOptions.cs`; the compile orchestration body → `Cobol.Net.Compiler/CompilerDriver.cs` |
| `src/CobolNet/Frontend/Frontend.cs` | **move** | `Cobol.Net.Frontend/Pipeline/Frontend.cs` (it *is* the front-end orchestrator; belongs with what it drives) |
| `src/CobolNet/Binding/*.cs` | move | `Cobol.Net.Compiler/Binding/` |
| `src/CobolNet/CodeGen/CSharpEmitter.cs` | **decompose + move** | `Cobol.Net.Compiler/Emit/**` (see §2 for the split) |
| `src/CobolNet/CodeGen/CodeWriter.cs`, `RoslynBackend.cs` | move | `Cobol.Net.Compiler/CodeGen/` (and factor `ReferenceAssemblies`/`RuntimeConfigWriter` out of `RoslynBackend`) |
| `src/CobolNet.Runtime/**` | move (rename project) | `src/Cobol.Net.Runtime/**`, re-foldered (`CobolString.cs`→`Text/`, `StopRun.cs`→`Control/`) |
| `src/CobolSharp.Compiler/Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/`, `Grammar/`, `ANTLR4/`, `GenerateIfNewer.ps1`, `Invoke-Antlr4CSharp.ps1`, the ANTLR MSBuild targets | **extract** | `src/Cobol.Net.Frontend/` (same subfolder names) |
| `src/CobolSharp.Compiler/` remainder — `Semantics/`, `IR/`, `CodeGen/` (the 11 `Cil*`/`*Lowerer`), `FlowAnalysis/`, `Compilation.cs`, `CompilationResult.cs` | **retire at G8** | deleted at cut-over; until then stays as `CobolSharp.Compiler` (the differential oracle), now referencing `Cobol.Net.Frontend` |
| `src/CobolSharp.Runtime/**` | **retire at G8** | the byte engine's runtime; deleted at cut-over (its clean substrates already ported into `Cobol.Net.Runtime`) |
| `src/CobolSharp.CLI/**` | **retire at G8** | replaced by `Cobol.Net.Cli`; until then repointed to `Cobol.Net.Frontend` |
| `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/` | keep (legacy), **add new** | stay until G8 (test the oracle). New `tests/Cobol.Net.Tests.Unit/` + `tests/Cobol.Net.Tests.Conformance/` added in G0; legacy test projects deleted at G8 |
| `tests/nist/`, `tests/conformance/` | **keep in place** | the corpus is compiler-agnostic; the new conformance test project points at it |
| `docs/COBOLNET_ARCHITECTURE.md`, `COBOLNET_DESIGN.md` (this doc), `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` | keep | update §5 of ARCHITECTURE to point here; add a `DOC_INDEX.md` row for `COBOLNET_DESIGN.md` |

### 1.4 Front-end extraction — how, precisely

The new `Cobol.Net.Frontend.csproj`:
- Inherits TFM/lang from `Directory.Build.props` (net10.0 / C# 14) — **do not** re-declare.
- `<PackageReference Include="Antlr4.Runtime.Standard" />` — **no `Version`** (central package management via `Directory.Packages.props`). It needs `Antlr4.Runtime`; it does **not** need `Mono.Cecil` (that was the byte emitter's IL writer).
- No `ProjectReference` (the front-end is self-contained — confirmed: it doesn't reference `CobolSharp.Runtime`).
- Carries the ANTLR generation: copy `EnsureGeneratedFiles` + `CleanGenerated` targets and the `<None Include="Grammar\…">`/jar items verbatim from the legacy csproj; the `Inputs`/`Outputs` paths stay relative so they work post-move.
- `<InternalsVisibleTo Include="Cobol.Net.Tests.Unit" />` (replaces the legacy one) if any internals need testing.

**Namespaces (⛔ SUPERSEDED at rearchitecture P1 — this deferral was reversed).** The original plan kept the front-end under `CobolSharp.Compiler.*` through G0–G7 and did the `CobolSharp.Compiler.* → CobolNet.Frontend.*` rename as a single big-bang at G8. **Rearchitecture PHASE 01 pulled that rename FORWARD** (`docs/rearchitecture/PHASE-01-mechanical-rename-deadcode.md`): the five front-end sub-namespaces are now `CobolNet.Frontend.{Common,Diagnostics,Generated,Parsing,Preprocessor}`, flipped across greenfield src/tests AND the legacy tree's shared-front-end *imports*, so later phases edit final names and **G8 is a pure deletion**, not a rename-plus-deletion. The legacy tree keeps its OWN `CobolSharp.Compiler.*` identity (root + `.Semantics`/`.CodeGen`/`.IR`/`.FlowAnalysis`) until G8. (The original rationale below is retained for history: doing the rename at G0 would have force-touched all of legacy `Semantics`/`IR`/`CodeGen` — but P1 mitigated that with per-segment segment-literal seds + a handful of namespace-relative-shorthand aliases, keeping the diff mechanical and the full battery green.)

### 1.5 Ordered git-mv sequence (build + guard green per step)

Each step is a self-contained commit; `dotnet build Cobol.Net.sln` (and the guard, once scripts are repointed) is green at the end of each. `git mv` preserves history.

**Step 1 — Extract the front-end (kills the legacy-assembly dependency).**
1. `git mv` `Parsing/ Preprocessor/ Diagnostics/ Common/ Generated/ Grammar/ ANTLR4/ GenerateIfNewer.ps1 Invoke-Antlr4CSharp.ps1` from `src/CobolSharp.Compiler/` → `src/Cobol.Net.Frontend/`.
2. Create `Cobol.Net.Frontend.csproj` (with the ANTLR targets); add it to `Cobol.Net.sln`.
3. Repoint the **four** consumers' `ProjectReference` from `CobolSharp.Compiler.csproj` → `Cobol.Net.Frontend.csproj` **and add** a `CobolSharp.Compiler → Cobol.Net.Frontend` reference (the byte engine now consumes the extracted front-end). Consumers: `src/CobolNet`, `src/CobolSharp.CLI`, both `tests/CobolSharp.Tests.*`. (`CobolSharp.Compiler` keeps its `Cobol.Net.Frontend` ref + its own `Mono.Cecil`/`CobolSharp.Runtime` refs for the byte engine.)
4. Build. The new compiler no longer references `CobolSharp.Compiler.dll`. ✅ *Goal "stop depending on the legacy assembly" met here, with zero namespace churn.*

**Step 2 — Rename the runtime.** `git mv src/CobolNet.Runtime src/Cobol.Net.Runtime`; rename the `.csproj`; set `<AssemblyName>Cobol.Net.Runtime</AssemblyName>`/`<RootNamespace>CobolNet.Runtime</RootNamespace>`; re-fold (`Text/`, `Numeric/`, `Control/`); update the `RoslynBackend` runtime-DLL path constant (`CobolNet.Runtime.dll` → `Cobol.Net.Runtime.dll`); update `.sln`. Build + run a HELLO compile. ✅

**Step 3 — Split + rename the compiler & CLI.** `git mv src/CobolNet src/Cobol.Net.Compiler`; create `Cobol.Net.Cli` and move `Program.cs`/`CliOptions` into it; extract `CompilerDriver` into the library; set assembly/root-namespace/`OutputType` per §1.1 (Cli `<AssemblyName>cobol</AssemblyName>`); update `.sln`. Build; `cobol hello.cob --run`. ✅

**Step 4 — Add the new test projects.** Create `tests/Cobol.Net.Tests.Unit` + `tests/Cobol.Net.Tests.Conformance`, referencing the new compiler library + the in-place `tests/nist`/`tests/conformance` corpus; add `InternalsVisibleTo`. Legacy test projects untouched (still guarding the oracle). ✅

**Step 5 — Solution / scripts / CI / props (one commit).**
- Rename `Cobol.Net.sln` → `Cobol.Net.sln` (or keep the filename and just update entries — owner taste; recommend rename for brand consistency).
- Update the **five** scripts that hardcode paths: `scripts/guard.sh`, `guard-fast.sh`, `guard-run-group.sh`, `nist-batch.sh`, `run-suite.sh` (the `.sln` name and any `src/CobolNet*`/`CobolSharp.*` project paths and the runtime-DLL copy target).
- Update CI `.github/workflows/build-and-test.yml` (it hardcodes `Cobol.Net.sln` and the two `tests/CobolSharp.Tests.*` paths) in this same commit so CI tracks the rename.
- `Directory.Build.props` / `Directory.Packages.props` need **no functional change** (TFM/lang/central-versions are name-agnostic); confirm the package-id list still covers `Antlr4.Runtime.Standard`, `Microsoft.CodeAnalysis.CSharp`, the test packages. (`Mono.Cecil` becomes legacy-only; keep its `PackageVersion` until G8.)
- Full guard green. ✅

**Step 6 — (at G8, not G0) Namespace big-bang + legacy deletion.** Delete `CobolSharp.Compiler` (`Semantics`/`IR`/`CodeGen`/`FlowAnalysis`/`Compilation*`), `CobolSharp.Runtime`, `CobolSharp.CLI`, and the legacy test projects. Rename `CobolSharp.Compiler.* → CobolNet.Frontend.*` across the surviving Frontend files + the new compiler's `using`s (one search-replace, now small because only the new compiler consumes it). Drop the `Mono.Cecil` `PackageVersion`. Update the generation script's emitted namespace. Final guard green. ✅

> **`.sln` / config implications, summarized:** central package management means every new `.csproj` `PackageReference` is **version-less**; `Directory.Build.props` is the single TFM/lang/nullable/warnings-as-errors source — projects never re-declare those; the `.sln`, the 5 scripts, and the CI YAML are the only places project/solution *paths* are hardcoded and must move in lockstep (Step 5).

---

### 2. No god classes — structural discipline

The legacy `CilEmitter` reached 2600 lines before being split into 11 `Cil*Emitter`s sharing an `EmissionContext`. `CSharpEmitter.cs` is already ~790 lines and growing (display + move + add/subtract/multiply/divide/compute + if + perform + conditions + the whole `NumX` numeric renderer + reference resolution). **Decompose it now, in G0, before G2/G3 push it past 1000 lines** — the proven legacy shape is the template, applied pre-emptively rather than as rescue surgery.

### 2.1 Rules (non-negotiable, in PROMPT.md spirit)

1. **Shared state lives in a context object, never a mega-class.** An `EmissionContext` (the `CodeWriter`, the `DataBinder`, the paragraph table, the division working-scale `_targetScale`, the dialect level) is passed to every emitter. Emitters are stateless-but-for-the-context cooperating units — exactly the legacy `EmissionContext`/`LoweringContext` pattern that already works here.
2. **One file per statement-family emitter.** A verb family = a file. Adding a verb = a method in its family's emitter (or a new file for a new family), never a new branch threaded into a shared switch in a 2000-line file.
3. **Respect the bind → lower → emit boundary** (and `project_dual_backend_goal`): the **binder** produces the typed model (`DataItem`/`PicInfo`) and resolves references; **lowering** (G3/G4) normalizes hard COBOL shapes (CORR expansion, control-flow flattening) into a C#-friendly form; **emit** turns that into C# text. An emitter must not re-discover semantics the binder owns (e.g. category compatibility), and the binder must not emit text.
4. **Dispatch generically, refactor-first** (`feedback_change_the_dispatch_not_the_callers`): the statement dispatcher routes by node type to the owning emitter; you never add per-caller if-else chains. New variant ⇒ extend the dispatch table, not each call site.
5. **Size is a *smell*, not the law — SRP is the law.** Heuristic thresholds: a class > ~400 lines or a method > ~60 lines triggers a "does this have one responsibility?" review. *But note `CilDataEmitter.cs` is 44 KB even after the split* — data is intrinsically broad; the test is cohesion, not line count. A 500-line class with one job is fine; a 200-line class doing two jobs is not.
6. **Runtime split by concern** (already followed): `Numeric/`, `Text/`, `Control/`, `Pointers/`, `Files/` — never a `CobolRuntime` god class.

### 2.2 Concrete decomposition of `CSharpEmitter` (the class list)

| New class (file) | Responsibility | Lifted from current `CSharpEmitter` |
|---|---|---|
| `EmissionContext.cs` | Holds `CodeWriter`, `DataBinder`, paragraph table (`_paras`/`_paraIndex`), `_targetScale`, dialect. The shared spine. | the private fields scattered today |
| `CSharpProgramEmitter.cs` | Top-level orchestration: class shell, `Main`, the paragraph→method loop. ~80 lines. | `Emit`, the Main/paragraph loop |
| `Emit/Data/FieldEmitter.cs` | DATA DIVISION → C# fields/profiles; VALUE initializers; (G2) group→`record struct`, OCCURS→`T[]`. | `EmitWorkingStorage`, `EmitFieldRecursive`, `InitializerFor`, `UnscaledAtScale`, `ProfileName` |
| `Emit/Statements/DisplayEmitter.cs` | `DISPLAY` (and later `ACCEPT`). | `EmitDisplay` |
| `Emit/Statements/MoveEmitter.cs` | `MOVE` (+ later CORR). | `EmitMove`, `ConvertToTarget`, `SendAsString`, `SendAsNumber` |
| `Emit/Statements/ArithmeticEmitter.cs` | `ADD`/`SUBTRACT`/`MULTIPLY`/`DIVIDE`/`COMPUTE`; GIVING/ROUNDED/SIZE ERROR. | `EmitAdd`…`EmitCompute`, `AssignScaled`, `AssignDivide`, `EmitArithAssign` |
| `Emit/Statements/ConditionalEmitter.cs` | `IF`/`EVALUATE` block splitting + branch emit. | `EmitIf`, `EmitBlocks` |
| `Emit/Statements/PerformEmitter.cs` | `PERFORM` (inline/out-of-line/THRU/TIMES/UNTIL; G4 VARYING/GO TO). | `EmitPerform`, `EmitInlinePerform`, `EmitUntil`, `EmitLoop`, `TimesCount` |
| `Emit/StatementDispatcher.cs` | Routes a `StatementContext` to its owning emitter (the generic switch). | `EmitStatement` |
| `Emit/Numerics/NumericExprRenderer.cs` | The whole `NumX` scale-tracked renderer (`Num`, `NumChain`, `Combine`, `Align`, `UnscaledLit`, `FieldNum`, …). One cohesive unit. | the `NumX` region |
| `Emit/Conditions/ConditionRenderer.cs` | COBOL condition → C# boolean (`RenderCondition`, logical chains). | `RenderCondition` |
| `Emit/Conditions/ComparisonRenderer.cs` | Relational comparison + `MapOperator` + operand string/number rendering. | `RenderComparison`, `MapOperator`, `IsStringOperand`, `OperandAsString`, `OperandNum` |
| `Binding/ReferenceResolver.cs` | `DataReferenceContext` → `DataItem` (name/qualified/subscript). | `Resolve`, `ReadAsString` |
| `CodeGen/CodeWriter.cs` | unchanged (already single-purpose). | — |
| `CodeGen/RoslynBackend.cs` | split: keep `Compile`; extract `ReferenceAssemblies.cs` + `RuntimeConfigWriter.cs` (each already a distinct concern in the file). | — |

The free helpers (`DecodeCobolString`, `CsStringLiteral`, `Children`, `DataRefs`, `FirstToken`) become a small internal `EmitHelpers` static class or move next to their primary user. `RenderLiteralAsString` lives with whoever owns literals (the renderers).

---

### 3. C# 14+ / .NET 10+ feature usage guideline (.NET 11 upgrade pre-authorized when it pays for itself)

**Principle: readability and correctness first, not feature golf.** A modern feature earns its place only when it makes the *emitter* code clearer or safer. Examples below are drawn from constructs already in this codebase.

| Feature | Use it when | Avoid when | In-repo example |
|---|---|---|---|
| `record` / **`record struct`** | immutable value bundles with value equality — bound model, small renderer results | a type with identity/mutable lifecycle | `readonly record struct NumX(string Expr, int Scale)`; `record struct Result(bool, IReadOnlyList<Diagnostic>)` |
| **primary constructors** | a class/struct whose ctor just captures collaborators into fields | when the param needs validation/transformation before storing | `readonly struct BlockScope(CodeWriter writer)`; apply to the new emitters: `sealed class MoveEmitter(EmissionContext ctx)` |
| **collection expressions `[]`** | initializing lists/arrays | — (always clearer than `new List<T>()`) | `List<Para> _paras = [];`, `_copySearchPaths = []` |
| **property / list patterns** | inspecting the bound model without temp vars | deeply nested patterns that out-clever the reader | `is { Pic: { Category: PicCategory.Numeric, IsFloat: false } } t` |
| **switch expressions** | total mapping from one closed set to another | side-effecting branches (use a `switch` statement) | `MapOperator`, `Combine`, `PicInfo.ClrType` |
| **file-scoped namespaces** | every file (one ns per file here) | — | every `.cs` in the project |
| **`using` aliases** | taming a long generated type name | aliasing for brevity's sake | `using Core = CobolParserCore;` |
| **`required` members** | a non-nullable field with no sensible default | optional/defaulted state | `DataItem.Level`/`.CsName` are `required` |
| **raw / interpolated string literals** | emitting multi-line C#/JSON templates | single tokens | the `$$"""…"""` `runtimeconfig.json` in `RoslynBackend` |
| **`static` lambdas** | a closure that captures nothing (signals + prevents capture) | when capture is intended | `.Where(static p => p.EndsWith(".dll", …))` |
| **`params` collections (C# 13+)** | variadic helpers taking spans/lists | — | candidate for `CodeWriter` multi-line helpers |
| **`field` keyword (C# 14)** | a property needing light backing-field logic without declaring the field | trivial auto-props (no gain) | future: a lazily-built profile cache on `PicInfo` |

**Before / after (compiler-relevant):**

*Statement dispatch — switch statement with cohesive arms (keep) vs. an if-else ladder (avoid):*
```csharp
// GOOD — the existing pattern-switch dispatch, one arm per family, easy to extend:
case var _ when s.moveStatement()    is { } m: _move.Emit(m);   break;
case var _ when s.addStatement()     is { } a: _arith.EmitAdd(a); break;
// BAD — a growing if/else ladder in every caller (forbidden by feedback_change_the_dispatch_not_the_callers)
```

*Result bundle — `record struct` vs. out-params:*
```csharp
public readonly record struct Result(bool Success, IReadOnlyList<Diagnostic> Diagnostics); // GOOD: named, immutable, value-equal
// BAD: bool Compile(..., out IReadOnlyList<Diagnostic> diags)  — positional, easy to misuse
```

*Emitter construction — primary ctor + context (the decomposition target):*
```csharp
internal sealed class ArithmeticEmitter(EmissionContext ctx)   // GOOD: collaborators captured once
{
    public void EmitAdd(Core.AddStatementContext add) { ... ctx.Writer.Line(...); }
}
// BAD: a 790-line CSharpEmitter holding _data, _paras, _targetScale, and every Emit* method.
```

**Standing conventions** (already in force, restated): full XML doc comments on public surface + inline rationale on non-obvious COBOL semantics (with ISO §citations — `feedback_spec_is_the_oracle`); generated C# written to `<name>.g.cs`, always inspectable; `SymbolDisplay.FormatLiteral` for every emitted string literal (never hand-rolled escaping).

---

## 18. Settled decisions (the §15 open questions, resolved)

**OWNER-CONFIRMED 2026-06-08.** All four consequential forks were reviewed with the owner and the **recommended**
option chosen for each: **#1** mixed-USAGE REDEFINES → confined byte[] pun (in-memory program data stays 100% typed);
**#9** control flow → PC-dispatcher for v1, idiomatic "pretty pass" later; **#2** arithmetic → native only
(ARITHMETIC IS STANDARD-DECIMAL out of scope); **conformance** → the differential harness (legacy vs WiseOwl COBOL
identical stdout). The remaining items below stand as the mechanical defaults (owner reviewed the full list).

1. **Byte-at-boundaries (REDEFINES Tier C + file records).** DECISION: in-memory **program data is 100% typed** (no
   `ProgramState`, ever). A `byte[]` exists ONLY at two genuine boundaries, exactly as the owner-co-authored ADR
   already sanctioned ("byte image only as a classifier-scoped fallback for REDEFINES/RENAMES type-puns, file
   records"): (a) the **external medium** for file records, serialized at READ/WRITE via an `IRecordCodec`
   (CODE-SET = the codec); (b) a **tightly REDEFINES-class-confined** canonical `byte[]` ONLY for a genuine
   **mixed-USAGE type-pun** REDEFINES (Tier C). Tiers A/B (homogeneous redefines — the entire near-term NIST path)
   stay 100% typed. This honors "no byte substrate for program data" without rejecting common production layouts
   (X-over-COMP records). **OWNER-VETOABLE:** if you want zero `byte[]` anywhere, mixed-USAGE REDEFINES and
   non-typed file formats are instead rejected loudly (Tier C → Tier D).
2. **Arithmetic mode.** Native arithmetic only (ISO §8.8.1.3) — fully conformant as the default and what the 364
   NIST corpus uses. `ARITHMETIC IS STANDARD-DECIMAL` (decimal128) would require a banned software decimal-float
   type; not implemented (documented). `STANDARD-BINARY` is spec-obsolete — not implemented.
3. **Numeric carrier + division guard.** `long` (≤18 digits), `Int128` (19–38); a picture/intermediate needing
   >38 digits is rejected loudly (outside the 2002/2014/2023 `--std` conformance scope; revisit only if a target needs it).
   `DIV_GUARD_DIGITS = 14`, validated/tuned against the NIST division-rounding tests in G5.
4. **Int128 timing.** Build the `Int128`/`CobolInt` overloads + `PicInfo.WidePrecision` in the wave that first needs
   a >18-digit picture (not the early NC/SM/IC/IF waves) — not earlier.
5. **COMP-5 / BINARY-\*.** True binary-width two's-complement **wrap** (`PIC S9(4) COMP-5` wraps at ±32768) — the
   locked semantics; replace `CobolNum.Store`'s digit-truncation TODO before COMP-5 tests are in scope.
6. **Diagnostics.** A fresh **`COBOLNET-`**-prefixed code scheme (clean slate); the legacy `COBOLxxxx`/`CBLxxxx`
   codes are not reused.
7. **Conformance harness.** YES — depend on the legacy compiler as a **differential oracle** until cut-over (G8):
   run legacy + CobolNet on each NIST program and assert identical stdout. Turns the 364 passing legacy tests into
   a free regression net; the `.txt` oracles remain the ground truth.
8. **Assembly granularity.** One `.g.cs` + one assembly per compilation (multiple/contained programs → nested
   classes, same-assembly direct CALL); a per-unit/by-name option is a later need.
9. **Dispatcher vs pretty output.** v1 always emits the PC dispatcher (correctness first). Lifting provably
   well-behaved (no GO TO/ALTER) paragraphs to idiomatic structured C# is a **post-conformance** "pretty pass",
   deferred.
10. **ALTER / GOBACK / UNTIL EXIT.** ALTER + the target-less GO TO are **85-ONLY**: obsolete in ANSI X3.23-1985
    (accepted at `--std 85`, no failing diagnostic), DELETED by ISO/IEC 1989:2002 → REJECTED at 2002/2014/2023
    (COBOLNET0810/0811) — the earlier "gated ON through 2014" was reconciled against the ISO history (the 2023
    standard has no ALTER; §14.9.17 GO TO has only Formats 1–2). Realization: the D4 per-paragraph mutable field.
    `PERFORM UNTIL EXIT` in scope (`while(true)` + EXIT PERFORM=`break`); a STOP RUN / main-program `GOBACK` WITH
    {NORMAL|ERROR} STATUS (§14.9.42.4 GR5 / §14.9.18.4 GR10) writes the termination status into the run-unit
    `RunUnit.ExitStatus` field, whose setter flushes it to `Environment.ExitCode` at the write site (the status
    rides the run-unit field, NOT a `ProgramReturn`/`StopRun` payload — `ProgramReturn` is caught at the dispatcher,
    never reaching `Main`; the write-site flush is why a separately-compiled module's STOP RUN … WITH STATUS reaches
    the exit code even when the main group has no status phrase; a called-program GOBACK status is inert, GR2).
11. **CALL BY REFERENCE of an irregular receiver.** Array element → C# `ref`; a reference-modified splice →
    promote to BY CONTENT (lenient default), diagnosable under a strict dialect.
12. **Pointer carrier.** A typed `ManagedRef<T>` (managed reference; NOT the abandoned `byte[]`+offset+length form);
    keep the public name **`ManagedPointer`** (owner preference). The `feedback_one_mechanism_per_job` memory note
    describes the abandoned byte form (pre-rewrite) and is updated.
13. **Boundary codec.** The storage-byte law (`CobolNet.Runtime.StorageByte`: byte 0xFF ↔ U+FFFF, every other byte
    k ↔ U+00kk) is the ONE byte↔char mapping — used by every byte-form image, REDEFINES Tier C, the whole-group
    image and the one-byte record medium (`FileCharacterSet`, which encodes the channel as strict ISO/IEC 8859-1). A
    LINE SEQUENTIAL file is UTF-8 text (`LineSequentialEncoding`). Design COBOLNET_FILES D29; kb/Work PB1759, PB1760.
14. **Figurative HIGH/LOW-VALUE + collating.** No PCS ⇒ `HIGH-VALUE`=U+FFFF / `LOW-VALUE`=U+0000 in BOTH classes —
    the extremes of the 65,536-position native sequence (§8.3.3.6.4 GR6/GR7; owner decision kb/Work R52, which
    retired the former alphanumeric U+00FF pin — kb/Work PB1093; `CobolNet.Runtime.NativeCollatingSequence`). With a
    PROGRAM COLLATING SEQUENCE they are the sequence's EXTREME characters (ISO §8.3.3.6 GR6/7 +
    §12.3.7 GR8/9 — character identity, ties: highest→last-specified, lowest→first-specified). The custom-`ALPHABET`
    subsystem is LIVE in BOTH classes (§12.3.7.2 two-branch format, the FOR phrase in its ISO position between the
    name and IS; a FOR phrase written after the definition — once an accepted "historical superset" on ALPHABET and a
    bare parse error on CLASS — is the grammar's `misplacedSpecialNamesForPhrase` error production, refused by name
    (COBOLNET2315) for ALPHABET, CLASS and SYMBOLIC CHARACTERS alike in `ClosedFormatPass`, kb/Work PB977):
    - **⛔ ONE literal-operand decoder for the ALPHABET and CLASS clauses** (kb/Work PB976). §12.3.7.3 states the
      same operand rules twice — SR14 b/c for the ALPHABET literal phrase, SR17 b/c for the CLASS clause, under
      different item numbers — so `DataBinder.LiteralPhraseOperand` decodes every operand of both, held to a
      per-clause `LiteralPhraseRules` value (the clause's descriptor, rule number and item numbers, its class, and
      the CLASS clause's IN alphabet). The CLASS clause had its own copy that ignored the FOR phrase entirely, so
      SR17's class-dependent half (the IN alphabet's class, each literal's class, one character under THROUGH) went
      unenforced and figurative constants decoded as their own spelling. The FOR phrase of all three clauses is read
      by ONE helper, `ForPhraseIsNational`; a user class records its class (`UserClassDef.National`).
    - **⛔ ONE table, ONE builder, ONE runtime carrier for BOTH classes** (kb/Work PB770). §12.3.7.4 GR7 k states its
      six sub-rules ONCE, "where the native coded character set is the type of coded character set or collating
      sequence being defined, either alphanumeric or national", and both native sets are the 65,536 UTF-16 code units
      here (implementor item 188 / D-N1) — so `CollatingTable` (binder), `LiteralPhraseCollation` (runtime, with
      `AlphanumericCollation` / `NationalCollation` as its two named arms) and `DataBinder.Switches`'s
      `AlphabetLiteralPhrase` / `AlphabetOperands` each exist exactly once and take a `national` flag that reaches
      only the diagnostics and the per-operand literal CLASS. The arms drifted for as long as they were separate:
      the alphanumeric one implemented NONE of §12.3.7.3 SR14 (duplicates, the ordinal range, the literal class, the
      one-character THROUGH/ALSO operand) and masked every operand into a 256-wide Latin-1 block, so
      `ALPHABET A IS 305 THRU 300` — legal source naming U+012B..U+0130 — silently reversed `'+'`…`'0'`.
    - **The table is SPARSE in both classes**: only the SPECIFIED characters are tabulated; every unspecified one
      takes its GR7 k3 position arithmetically, and the runtime materializes an O(1) weight cache up to the highest
      specified character so the comparison hot path costs one array index. The width is therefore the REPERTOIRE,
      not a block — §12.3.7.4 GR7 1., whose sub-rule **3** places the unspecified tail:
      "Any characters of the native collating sequence that are not specified in the literal phrase shall assume a
      position in the collating sequence that is greater than that of the highest character specified in this
      literal phrase. The relative order within the set of these unspecified characters is unchanged from the
      native collating sequence."), built in `DataBinder.Switches` (`Alphabets`/`Collating`), rendered as
      the generated `__COLLATE` field (declared by `ObjectComputerEmit.EmitMembers` for EVERY runtime-module
      type — a program class and a CLASS-ID's instance/factory classes alike, kb/Work PB111, beside the
      `__COLLATE_NAT` and `__CLASSIFY` members), consumed by the settled seam `CobolString.Compare(a,b,weights)` at every
      relation/condition-name comparison site (§12.3.6 GR11), by CHAR/ORD (H5 flag), and by the PCS-aware
      figurative fills.
    - **National** (P10 Step 4): the SAME `CollatingTable` over the 65,536 UTF-16 code units — built by
      `AlphabetBindNational` through the shared `AlphabetLiteralPhrase` (`NationalAlphabets`/`NationalCollating`),
      rendered as the generated `__COLLATE_NAT` `NationalCollation` instance, consumed by
      `CobolString.Compare(a,b,national)` at national relation/condition-name sites (§8.8.4.2.9 / §12.3.6 GR11),
      by CHAR-NATIONAL/ORD-over-national (`CollateNat`, §15.16.4/§15.70.4 r2), and by the national figurative
      fills (§12.3.7 GR8/9). Code-set phrases: **UCS-4** names the ISO 10646 collating sequence — on the D-N1
      one-code-unit-per-position substrate that IS the native code-unit order (§8.5.1.4 denies surrogate-pair
      recognition, so the supplementary-plane codepoint/code-unit divergence is unreachable; the correspondence is
      the BMP identity, implementor item 188) — and **UTF-8/UTF-16** name coded character sets ONLY (§12.3.7 GR7
      Table 6): referencing them as a collating sequence is rejected (0898), and their coded-set role reaches the
      medium only as the documented A.3 item 27 non-support — a variable-width medium encoding is not a code this
      processor writes (item 14a below; the CODE-SET boundary codec itself is LIVE, kb/Work PB793).
    **§12.3.7.3 SR14/SR15 are enforced once, for both arms** (`COBOLNET1906` / `COBOLNET1907`): a) no character
    twice (recovery keeps the first), b1/c1 BOTH halves of its one sentence -- an UNSIGNED integer AND the 1..65,536 range, b2/c2 the operand's literal class,
    b3/c3 one character under THROUGH/ALSO, and SR15 — **a TABLE, `Binding/ImplementorCodeNames.cs`, not a pair of
    arms** (kb/Work PB793). SR15 says "The implementor shall specify the names supported for code-name-1 and
    code-name-2 in the ALPHABET clause, if any", and the answer is one row per supported name carrying everything
    §12.3.7.4 GR7 i/j obliges the implementor to supply for it — the coded character set's ordinal count, the
    collating positions, and the correspondence with the native set. `DataBinder.AlphabetCodeName` is a LOOKUP
    keyed by (spelling, class) with the refusal on miss, so **the next supported code-name is a row and no code
    path at all**, and the diagnostic names the supported set FROM the table. The set (owner decision, PB793;
    `follow_gnucobol_on_split_latitude`): code-name-1 = **`ASCII`** (ISO/IEC 646 IRV — 128 ordinals, the identity
    correspondence, the native order, i.e. exactly GR7 c's STANDARD-1 set under the spelling other vendors use)
    and **`EBCDIC`** (IBM CCSID 37 — 256 ordinals, collating position = the page's code unit, correspondence = the
    page, DERIVED from `Encoding.GetEncoding(37)` at bind time and never transcribed); code-name-2 = none. Both
    columns of Table 6 are live for a code-name: the sequence rides the SAME sparse `CollatingTable` (built through
    the shared `CollatingTable.Build`, which is also where §12.3.7.4 GR8/GR9's extremes are computed for the
    literal-phrase arm), and the set rides `CodedCharacterSet`, whose `OrdinalCount` and new `Medium` read the row.
    A bare word that is NOT in the table and is not a general-format keyword or a §12.3.7.4 GR10 figurative
    constant is a diagnostic and never (as it was) the characters of its own spelling. b4/c4 needs no check: a) makes every specified character
    distinct, and distinct characters of a set cannot outnumber the set. `LiteralChars` — the general helper whose
    ordinal branch carried the CLASS clause's descriptor, message and rule number into the ALPHABET and CURRENCY
    clauses — is now `ClassLiteralChars` with a REQUIRED class-name, beside an ordinal-free `LiteralCharsOf`.
    SORT/MERGE: the statement resolves BOTH sequences as a `SortCollation` PAIR — §14.9.40.4 GR5 / §14.9.24.4 GR5
    determine them SEPARATELY, alphabet-name-1 (GR5a) or the alphanumeric program collating sequence (GR5b) for keys
    of class alphabetic and alphanumeric, alphabet-name-2 / the FOR NATIONAL form (GR5a) or the national program
    collating sequence (GR5b) for keys of class national — and each KEY takes the one its class names, through the
    ONE classifier `CollatingSelection.Of` (kb/Work PB678). A key of class boolean or numeric takes NEITHER: GR5
    names no sequence for those classes, and §8.8.4.2.8 / §8.8.4.2.4 compare them by value.
    RELATION CONDITIONS ask a **different question with a different classifier**, `CollatingSelection.ForComparison`
    (kb/Work PB741): which of §8.8.4.2's per-class comparison clauses does this relation select, given **BOTH**
    operands' categories — boolean if either is boolean (§8.8.4.2.8), else national if either is national
    (§8.8.4.2.9, which §8.8.4.2.6 and §8.8.4.2.5 both route into), else numeric only if **both** are numeric
    (§8.8.4.2.4), else alphanumeric (§8.8.4.2.7, carrying §8.8.4.2.1's normalizations — an alphanumeric group and a
    class alphabetic operand are "treated as" elementary alphanumeric — and §8.8.4.2.5's numeric integer against an
    alphanumeric operand). `EmitContext.CollateArgFor(left, right)` takes the PAIR, so a caller cannot supply half a
    comparison, and all four relation surfaces go through it: the direct relation, the figurative relation, the
    level-88 membership test (§8.8.4.5.3 GR2) and the EVALUATE THRU range (§14.7.8 rule 2). ⛔ The two classifiers
    must stay separate: folding the comparison onto `Of` made `IF numeric-item < SPACE` take the SORT-key answer
    ("numeric ⇒ no sequence") and silently drop the PROGRAM COLLATING SEQUENCE — NIST NC215A SEQ-TEST-GF-6/-7.
    `CollatingComparisonClassDriftTests` pins the whole category matrix and the exact pair where they disagree.
14a. **The alphabet as a CODED CHARACTER SET (ISO §12.3.7.4 GR7 Table 6 — kb/Work PB110 / PB109).** An
    alphabet-name references a collating sequence, a coded character set, or both; NOTE 2 of GR7 names the four
    sites that reference the CODED CHARACTER SET — "*the class condition, the CLASS clause …, a SYMBOLIC CHARACTERS
    clause, or the CODE-SET clause*" — and Table 6 which phrases define one (every phrase but LOCALE). ONE model
    serves all four: `Binding/CodedCharacterSet.cs` — `CodedCharacterSet.Of(AlphabetDef)` / `Of(NationalAlphabetDef)`
    (null for a LOCALE alphabet, and the ONE refusal `DataBinder.CodedCharacterSetOf(name, site, rule)` → COBOLNET1669
    at every site — §8.8.4.4.3 SR2, §12.3.7.3 SR16g / SR17d, §13.18.13.3 SR1/SR2), with `CharAt(ordinal)` (the
    character at an ORDINAL POSITION — GR11 b/c, GR12 a), `Contains(rune)` (the class test, §8.8.4.4.4 GR3 a) and
    `Medium` + `MediumCorrespondence` (the medium codec, §13.18.13.4 GR2/GR6 — the ONE classifier that says whether
    a set crosses the storage boundary as the identity, through a conversion, or not at all: `CodeSetMedium`
    `Identity` / `Translated` / `NotProvided`, the last being the documented A.3 item 27 non-support. A `Translated`
    set — a code-name's GR7 i correspondence (EBCDIC, 256 entries) or the 128-entry identity of an ISO/IEC 646
    set (STANDARD-1/2 by GR7 c, the ASCII code-name), carried so the output statement can refuse what the set
    lacks with '91' / '71' (kb/Work PB1150, PB1542) — hands the emitter its correspondence, which the connector
    applies as
    `CobolNet.Runtime.IO.CodeSetConversion` at the ONE physical boundary of each organization —
    `SequentialConnector.NextFrame` and its `EmitRecord`/`EmitRecordLine`/`OverwriteInPlace` twins, and
    `RecordFraming`'s store payload for RELATIVE and INDEXED. The §9.1.7.2 framing this processor adds AROUND a
    record — the length prefix, the line delimiter — is not data of the record and stays native, kb/Work PB793). **Determinations (implementor items per GR7 c/d/f–k; documented
    in CONFORMANCE.md):** the native ALPHANUMERIC set is the UTF-16 repertoire in code-unit order — ordinal n is
    code unit n−1, 1 ≤ n ≤ 65 536 (the same ordinal FUNCTION CHAR / ORD use — one correspondence, never two);
    **STANDARD-1 / STANDARD-2** are ISO/IEC 646 IRV (128 characters; STANDARD-2's national version IS the IRV), ordinal
    n = code n−1, the correspondence to the native set the identity on U+0000–U+007F; **NATIVE / UTF-16** (national)
    = UTF-16 code units, ordinal n = code unit n−1; **UCS-4 / UTF-8** (national) = ISO/IEC 10646 scalar values,
    ordinal n = scalar n−1 (a supplementary character is ONE character — its UTF-16 surrogate pair); a **literal
    phrase** defines a code set whose ordinals ARE its collating positions (GR7 k4 — "*the implementor defines the
    ordinal number … for each character … not specified*": the unspecified characters follow the highest specified
    position in native relative order, exactly as the sequence does — `CollatingTable.RepByPos` / `NextFree` and the
    code-unit tail; the national twin over the sparse table); an ALSO group's representative is literal-1 (GR7 k6's
    last sentence); and a **code-name** (GR7 i/j) defines whatever its `ImplementorCodeNames` row says — `ASCII` the
    ISO/IEC 646 IRV set with 128 ordinals and the identity correspondence, `EBCDIC` IBM CCSID 37 with 256 ordinals
    whose ordinal n is the native character that page assigns to code unit n−1, both derived rather than transcribed
    (item 14 above; CONFORMANCE.md §7 item 183 publishes them). `CodedCharacterSet` carries the row, so
    `OrdinalCount` (the SR16 e/f and SR17 b2 range bound) and `Medium` come from the table and not from a switch on
    the phrase name. **Consumers:** (a) `SYMBOLIC CHARACTERS` — each symbolic-character-1 defines a FIGURATIVE CONSTANT
    (GR11 a) whose value is the character at ordinal integer-1 in the native set or the `IN` alphabet's set (GR11 b/c;
    SR16 a/c/e/f/g checked); it is carried as `DataBinder.SymbolicCharacters` (name → the one-character literal and
    its class) and SUBSTITUTED where a figurative constant may stand (§8.3.3.6.2 Format 7 `[ALL] symbolic-character-1`;
    §8.3.3.6.4 GR2 — it FILLS a fixed-length receiver / VALUE like SPACE, GR3 b — one character in DISPLAY / STRING):
    the substituted form IS the ALL-literal NODE of that one character (`BoundAllLiteral` — the fill semantics of
    §8.3.3.6.4 GR2/GR10 in every context; the VALUE path's raw text `ALL"c"`), carrying `BeginsWithAll = false`
    for the BARE form so the screens that bar "a figurative constant that begins with the word ALL" (STRING /
    UNSTRING §14.9.43.3 SR2, INSPECT §14.9.22.3 SR3) fire only on the written word; `ALL symbolic-character-1`
    is the grammar arm `figurativeConstant : … | ALL cobolWord` (LAST, so the keyword and literal forms win);
    the bare form parses as a data reference and substitutes at the SAME hooks a constant-name does
    (`ExpressionBinder.FieldOperand` → `SymbolicOperand`, the VALUE capture, INSPECT's operand arm) — ONE
    mechanism for "a word that stands for a literal"; a symbolic character is not a receiver. (b) `CLASS … IN alphabet-name-4` — an
    integer literal is the ordinal IN THAT SET (GR12 a; SR17 b2's range is that set's), a non-numeric literal the
    character itself (GR12 b), THROUGH the native-contiguous range between the two resolved characters; both CLASS
    and SYMBOLIC CHARACTERS bind AFTER the SPECIAL-NAMES walk (the clauses are order-free; the ALPHABET they name may
    follow them — the `ResolveProgramCollating` shape). The walk is DEPENDENCY-ordered throughout: ALPHABET itself
    binds after the paragraph's other clauses, because its `IS LOCALE locale-name-2` names a LOCALE clause the
    §12.3.7.2 format prints AFTER it (kb/Work PB1558). (c) The class condition `identifier IS alphabet-name-1`
    (§8.8.4.4.4 GR3 a — kb/Work PB109): `BoundCodedSetClassCondition` → `CobolClass.IsInCodedSet(s, kind)` — STANDARD-1/2
    ⇒ every code unit ≤ U+007F; NATIVE / a literal phrase ⇒ every native character (TRUE for any content — GR7 k4 puts
    the whole native set in the code set); the national sets ⇒ well-formed UTF-16 (an unpaired surrogate is not a
    character of UCS-4 / UTF-8 / UTF-16). (d) `CODE-SET` — PB110 part 2 (`COBOLNET_FILES_DESIGN.md`): the 2002 format
    (`IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS … | FOR NATIONAL IS …}…`), SR1/SR2 through the ONE
    refusal, and the set's `Encoding` threaded into the connector as §8.1's one codec parameter (STANDARD-1/2 = ASCII
    with '?' for an unmappable character; NATIVE = the Latin-1 boundary codepage of item 13; UTF-8 / UTF-16 / UCS-4 =
    their .NET encodings, no BOM, little-endian; a literal phrase = one byte per character, the ordinal − 1).
15. **Reference modification out of range.** Throw (`EC-BOUND-REF-MOD` → `CobolRuntimeException`) by default; a
    lenient clamping dialect is a later option.
16. **Exception model default.** EC checking OFF by default (NIST-faithful, fast — ISO §5000), enabled only by
    `>>TURN`/phrases; an unhandled **fatal** EC terminates the run unit with a diagnostic + nonzero exit.
17. **VALIDATE.** Implemented minimally for the conformance corpus, marked obsolete (2023 Table 13).
18. **OO multiple inheritance.** Single inheritance in v1 (covers the whole current corpus); a multi-base program
    triggers an owner decision then (linearize one C# base + `IMPLEMENTS` interfaces with forwarding).
19. **Intrinsic internals.** Never `decimal` — exact via unscaled `long`/`Int128`, float via `double` (extends the
    owner's decimal/BigInteger ban into intrinsic internals).
20. **RETURN-CODE / termination status.** The run-unit exit code is ONE canonical field — `RunUnit.ExitStatus`
    (long), whose setter flushes to `Environment.ExitCode` AT THE WRITE SITE (so a separately-compiled CALLed
    module's STOP RUN … WITH STATUS crosses the assembly boundary to the process exit code — the flush is
    runtime-side, never per-`Main` scaffolding, upholding §18.16). It is written by the STOP RUN / GOBACK
    **WITH {NORMAL|ERROR} STATUS** phrase (§14.9.42.4 GR5 / §14.9.18.4 GR10 — the OS termination status) and, when
    implemented, by the RETURN-CODE special register; a single cross-subsystem owner AND a single flush, never duplicated. NOTE:
    RETURNING / GIVING is the **activation result** (§14.9.18.4 GR2 — moved into the caller's RETURNING item), a
    DISTINCT mechanism from the OS status — it does NOT feed the exit code.
21. **Whole-group-as-alphanumeric.** A generated `string AsImage()` / `FromImage()` per record struct is the
    PERMANENT typed-native mechanism for whole-group MOVE/compare of **DISPLAY-homogeneous** groups; a group with a
    COMP/COMP-3/COMP-5/float (non-character) leaf is the genuine mixed-USAGE byte-island routed to the Tier-C/file
    codec (#1). **Numeric-DISPLAY leaves are INCLUDED in the AsImage path (spec-grounded):** ISO §14.9
    MOVE GR4 fills a group with no conversion (a numeric-DISPLAY subordinate may legitimately hold spaces — using it
    numerically is then incompatible data, §14.6.13.2), so a numeric-DISPLAY leaf *under a whole-referenced group* is
    stored as its character image (`DataItem.StoreAsImage`; numeric use via `CobolNum.ParseDisplay`/`FormatDisplay`) —
    byte-faithful with NO byte[]. A numeric-DISPLAY leaf never referenced as part of a whole group stays a native
    `long` (invariant #2).
22. **Doc reconciliation.** `COBOLNET_ARCHITECTURE.md` §3's `decimal` rows are corrected to native
    `long`/`Int128`-unscaled in the same change set that lands this document (no second SSOT).
23. **Selectable codegen backend (owner-confirmed 2026-06-08).** Codegen is behind an `ICodeGenBackend` abstraction
    over the backend-neutral bound tree, selectable via `--backend roslyn|cil` (default **roslyn**): RoslynBackend
    (idiomatic C# source, the v1 deliverable) and CilBackend (typed-native CIL via Mono.Cecil, future-additive —
    no C#-compile step / no Roslyn dependency, for AOT/direct-IL). The CIL backend's structure→branch lowering is
    private to it; there is no shared lowered IR (§1.1). The two backends can cross-check each other in the
    differential harness.
