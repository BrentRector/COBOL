# WiseOwl COBOL — Anti-Pattern Catalog

**This document is the SSOT for the labelled anti-pattern catalog.** The labels (`[GodObject]`,
`[LayerViolation]`, …) are cited from `kb/Context/Doctrine & Anti-Patterns.md` and
`kb/Spec/Lookup/Constraints.md` — keep those in sync when a row changes here.

Scope note (single-write rule): **doctrine → `PROMPT.md`** · **process rules + the session-start sequence →
`CLAUDE.md`** · **phases, worklist and live state → `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0** ·
**history → `DEVLOG.md`**. This file holds the catalog and nothing else.

---

## Anti-Pattern Catalog (Non-Exhaustive)

You must actively search for and correct anti-patterns. The list below is illustrative,
not exhaustive — you must also identify and fix any other anti-patterns you find.

### Architectural and Layering Anti-Patterns

- **God objects / God classes** `[GodObject]`
  Classes that do too many things (e.g., mixing parsing, semantic analysis, and codegen).
  Action: Split into focused components with clear responsibilities and interfaces.

- **Leaky abstractions and cross-layer reach-through** `[LayerViolation]`
  Lower layers depending on higher layers (e.g., runtime knowing about parser internals).
  Action: Enforce strict layering (Lexer -> Parser -> Bound Tree -> Codegen -> Runtime).

- **Hidden global state / singletons / static mutable state** `[GlobalState]`
  Static mutable fields, implicit global configuration, or shared mutable caches.
  Action: Replace with explicit configuration objects, dependency injection, or immutable data.

- **Ad-hoc feature flags and scattered dialect checks** `[ScatteredFlags]`
  Per-edition `if` checks smeared across the binder and emitter.
  Action: Centralize edition gating in the one `VersionConformancePass` over the bound tree; the binder is
  edition-agnostic.

### Code-Level Anti-Patterns

- **Deeply nested conditionals and switch pyramids** `[DeepNesting]`
  Hard-to-follow logic with many nested if/else or switch statements.
  Action: Refactor into smaller methods, pattern matching, or data-driven tables.

- **Copy-paste logic / duplicated code** `[Duplication]`
  Repeated logic across modules (e.g., numeric formatting, PIC parsing, control flow lowering).
  Action: Extract shared helpers or canonical implementations.

- **Primitive obsession** `[PrimitiveObsession]`
  Using raw string, int, bool instead of domain types (e.g., PIC descriptors, token kinds, numeric formats).
  Action: Introduce domain-specific types and enums.

- **Magic constants and undocumented invariants** `[MagicValues]`
  Unexplained numeric or string constants, or implicit assumptions.
  Action: Replace with named constants, enums, or documented configuration.

- **Overuse of null and weak null-safety** `[NullHazard]`
  Unclear nullability, frequent null checks, or null-driven control flow.
  Action: Use nullable reference types, clear contracts, and avoid null where possible.

- **Exception misuse** `[ExceptionMisuse]`
  Using exceptions for control flow or swallowing exceptions silently.
  Action: Use explicit result types, error objects, or diagnostics pipelines.

- **Tight coupling to I/O or environment** `[IOBinding]`
  Compiler logic directly reading/writing files, console, or environment.
  Action: Abstract I/O behind interfaces; keep core compiler logic pure and testable.

### Performance and Memory Anti-Patterns

- **Excessive allocations in hot paths** `[HotAlloc]`
  Repeated string concatenations, boxing, or temporary collections in tight loops.
  Action: Use Span<T>, pooling, or more efficient data structures where appropriate.

- **Inefficient data structures** `[DataStructureMisfit]`
  Using List<T> or Dictionary<TKey,TValue> where a more suitable structure exists.
  Action: Choose structures based on access patterns and complexity.

- **Unbounded caches or collections** `[UnboundedGrowth]`
  Collections that grow without clear limits or eviction policies.
  Action: Add bounds, eviction, or redesign to avoid unbounded growth.

---

## How the catalog is used

- **Actively hunt, don't wait to trip over one.** When a change touches a file, the anti-patterns above are the
  review lens for what it touches.
- **Every instance, not the one you found.** An anti-pattern is a pattern: sweep the codebase and fix them together
  (`feedback_scan_all_similar`).
- **Fix it here, not around it.** Adding a case to a `[GodObject]` or wrapping a `[LayerViolation]` deepens the
  anti-pattern. Refactor the dispatch first, then add the case.
- **This list is illustrative, not exhaustive.** Anything meeting the same bar — hidden coupling, dead code,
  duplicated logic — is in scope whether or not it has a label here.

Phases, worklist and current status are NOT here — they live in `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0, and the
session-start sequence lives in `CLAUDE.md`.
