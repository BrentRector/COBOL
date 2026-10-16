# Architecture Evaluation — ANTLR4 Leverage & Tree Traversal: are we reinventing the wheel?

- **Type:** EVALUATION / RECOMMENDATION (owner-requested, 2026-07-10)
- **Question (owner):** "We use ANTLR4 to parse. Are we leveraging all ANTLR4 functionality efficiently and effectively?
  Why can't ANTLR AST tree walker(s) and similar features collect all the necessary information we later use in
  subsequent phases? Do we reinvent the wheel too much?"
- **Method:** top-down pipeline read + a 4-agent grounded-facts research pass (`wf_f4af214c-cdf`).

---

## TL;DR verdict

**The core architecture is SOUND and standard; the *traversal machinery* is not — and it is actively costing us
correctness.**

- ✅ **KEEP the two-tier `CST → bound tree` design.** ANTLR is a *parser generator*: it produces a Concrete Syntax
  Tree and does no semantic analysis. A separate semantic/bound tree is what **every serious compiler** builds —
  including **Roslyn, our own emit target** (`SyntaxTree → Binder → BoundTree → Emit`). The bound tree is the
  necessary semantic layer, **not** a reinvented wheel.
- ⚠ **But we materially UNDER-leverage ANTLR and standard patterns**, in ways the recent bugs prove are expensive:
  no shared tree-traversal, a hand-rolled binder dispatch, zero listener use, and no symbol table.
- 🔧 **The fix is NOT a rewrite.** It is largely what the rearchitecture plan **already schedules** — **P6 (SymbolTable
  + BindPhase)** and **P7 (exhaustive visitor + emitter decomposition)** — pulled *forward*, plus generating/adopting
  one canonical bound-tree visitor.

## The direct answer: why ANTLR walkers *can't* collect everything in one pass

Because COBOL has **forward references** (a group / `REDEFINES` / paragraph named before it is declared) and
cross-cutting facts (whole-group image usage depends on the *whole* procedure division). You cannot resolve those in a
single top-down listener pass — you must build the symbol table **completely first**, then run resolution/analysis
passes over it. This is inherent to compiling *any* real language (Roslyn, gcc, every production compiler do the same).
So **multiple passes are a necessity, not our over-engineering.** What IS on us: we could collect far more *per* walk,
and share *one* traversal — and we do neither.

## Grounded facts (evidence)

| Fact | Number | Source |
|---|---|---|
| ANTLR generation flags | `-visitor` **`-no-listener`** | `Invoke-Antlr4CSharp.ps1` (its ANTLR step) |
| Uses of ANTLR **listeners** in greenfield | **0** | grep `ParseTreeListener/Walker` = none |
| Greenfield files that subclass the generated **visitor** | **1** (`VersionConformancePass`, 57 overrides) | grep |
| Binder statement dispatch | a **50-arm** `_ when s.xStatement() is {}` chain (not the visitor) | `StatementBinder.cs:172` |
| `GetText()` "context-poking" calls in the compiler | **334** | grep |
| Bound-tree node types | **142** (52 `BoundStatement` subclasses) | `Binding/Bound/*.cs` |
| Shared bound-tree traversal (`Accept`/`IBoundVisitor`/base walker) | **none** | grep |
| Bespoke `case Bound…` traversal arms, **duplicated across ~5 walkers** | **205** (emitter 101, `UsageCollectionPass` 51, `VersionConformancePass` 35, …) | grep |
| How the parallel walkers stay in sync | a **prose comment** ("cross-checked against VersionConformancePass.Recurse") | `UsageCollectionPass.cs:60` |
| Consequence, observed | **6 missed statement types** in `UsageCollectionPass` (this session's review) | DEVLOG 753 |
| Emitter size | **7,941 lines / 15 partials** (79 `case Bound` in the main dispatch) | `CodeGen/` |
| Binder size | **18,166 lines** | `Binding/` |
| Resolve pass pipeline | **19 entries** | `BindPipeline.cs` |

## What is sound — keep

1. **The bound tree.** Correct semantic layer; mirrors Roslyn. Emitting correct COBOL semantics (REDEFINES tiers,
   OCCURS, USAGE scaled-integers, the PC-dispatcher control flow, EC) from the raw CST + a symbol table alone would be
   *far* messier, not cleaner.
2. **The grammar.** Idiomatic ANTLR (imports, `superClass`, virtual tokens), the word set single-sourced + drift-guarded.
3. **`VersionConformancePass` using the generated visitor** for edition gating — the one place we *do* leverage ANTLR's
   typed exhaustive dispatch.

## What is not — fix (prioritized)

### 1. [HIGH] ONE canonical bound-tree visitor — the single highest-leverage move (this is P7's core)
The bound tree has **no** `Accept`/`IBoundVisitor`, so **205 `case Bound` arms** are duplicated across ~5 hand-walkers
kept in sync by *prose*. This is the direct cause of the `UsageCollectionPass` 6-missed-types bug and of the
`DynTablePlace`/`RELEASE`/keyed-handler gaps — a C# `switch` over a class hierarchy has **no exhaustiveness check**.
**Fix:** generate (or hand-write once) a bound-tree walker with an exhaustive `Visit` per node type + a default that
recurses children; every pass overrides only the arms it cares about. This collapses the 205 arms to one authoritative
traversal, and makes "did we miss a statement type?" a *structural* guarantee instead of a review lottery.

### 2. [HIGH] A real SymbolTable (this is P6)
The binder resolves names via ad-hoc `ByName` dictionaries + `_rootNames` HashSets scattered through an 18k-line binder.
**Fix:** one `SymbolTable` populated during binding, the single authority for name/scope resolution — the standard
compiler component we currently hand-roll piecemeal.

### 3. [MED] Drive the binder from the generated visitor + shrink `GetText()` poking
The **50-arm `when` dispatch** and **334 `GetText()`** context-pokes reinvent what ANTLR's generated visitor gives for
free (typed double-dispatch + typed accessors). **Fix:** bind by subclassing `CobolParserCoreBaseVisitor`, capturing
structured content once per node instead of re-extracting token text 334 times.

### 4. [MED] Adopt ANTLR *listeners* for single-pass syntactic fact-collection
We generate no listener and use none. A `ParseTreeListener` + `ParseTreeWalker` is the idiomatic, low-ceremony way to
collect purely-syntactic facts in one automatic walk (e.g. the on-recognition edition gating, `>>TURN` events, some
usage facts) — instead of a bespoke visitor. Re-enable listener generation and use it where a stateful single pass fits.

### 5. [LOW / evaluate] Pass consolidation & the emitter's size
19-entry pipeline + a **7,941-line** emitter. Some resolve passes are fusible (no inter-dependency). The large emitter
is the expected cost of "**bound tree, no lowering IR**" (COBOLNET_DESIGN §2) — complexity that a normal compiler puts
in a *lowering* phase lands in the emitter here. **Evaluate at P7** whether a thin lowering step (or a shared visitor
that hosts the desugars) would shrink and de-risk the emitter, without adopting a heavyweight IR.

## What NOT to do

- **Don't** emit directly from the CST + a symbol table (the owner's "why not one walk?" taken to its extreme) — it
  loses the semantic layer and makes correct COBOL semantics *harder*, not easier.
- **Don't** adopt a heavy multi-level IR unless P7 shows the emitter demands it.
- **Don't** rewrite. Every item above is *consolidation onto standard patterns we already planned*, not new architecture.

## Sequencing recommendation

The **single most valuable action is #1 (the canonical visitor)** — it directly kills the completeness-gap bug class
that has already bitten us twice this session and de-duplicates 205 arms. It is the core of **P7**; **#2 (symbol table)**
is **P6**.

**Recommendation:** bring the **P6 + P7 traversal/symbol foundations forward** — ahead of finishing the feature/gating
phases — because *every* current phase is paying the ad-hoc-walker and no-symbol-table tax. Concretely: the pending
edition-gate remediation (folding ~15 inline gates into the two-arm pass) and the rest of Phase 5 would both be
*cleaner and safer built on a canonical visitor*. Doing the foundation first is the "measure twice, cut once" that the
owner's reservation is pointing at.

This is the owner's call to make; the two viable paths are **(A)** foundation-first (P6/P7 core now, then resume gate
migration + Phase 5 on top), or **(B)** finish the in-flight remediation + Phase 5 on the current foundation, then do
P6/P7. Path A is the higher-quality, lower-rework choice and is what this evaluation recommends.
