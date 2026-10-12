## Entry 398 — Stage 2 cont.: the RecordClassificationPass Phase B (procedure-division scan) + Phase C (cross-edge fixpoint) — the classifier is now complete

Phase A (Entry 397) classified the data-division triggers; the classifier must be *complete* before any Stage-3
typed flip (ADR §3 soundness), so this slice adds the two remaining phases — the procedure-division bound-tree
scan (B) and the cross-edge fixpoint (C). Additive and still **not consumed by codegen** (Stage 2 keeps
everything byte-backed), so it changes no behavior; the suite verifies the classifier as a spec.

**A 4-agent read-only investigation first mapped the territory** (the value of the same recon that preceded
Phase A): the complete bound-tree traversal surface (every `BoundStatement`/`BoundExpression` nested-statement
and expression slot, with 16 traversal gotchas — `BoundPerformVarying.Next` recursion, `BoundSizeErrorClause`
inside arithmetic/CORR, EVALUATE `SubjectConditions` Values/Ranges/Condition, all file-IO key/end blocks,
STRING/UNSTRING sendings/intos/overflow, INSPECT `DataRef`s, condition subjects); the `SemanticModel` access
points (`DataItemsInOrder`, `GetStorageLocation(s).Pic.Category`, `.Offset`/`.Length`); ODO detection
(`Occurs.DependingOnSymbol`); and the confirmation that pointers/`ADDRESS OF` are unreachable today
(`SET ADDRESS OF` is parsed but unbound, DEVLOG 389). The recon also surfaced — and the actual pipeline code
**refuted** — a claim that layout offsets are unavailable at classifier time: `Compilation.cs:123` runs
`StorageLayoutComputer` *before* the bound tree is built (`binder.Bind`, line 142), so offsets **are** available
and the precise §3.4 dissimilar-layout test is feasible (the agent had confused `StorageLayoutComputer` with the
later IR-phase `RecordLayoutBuilder`).

**Phase B — `ProcedureScanner`** walks the procedure division and marks the use-observable triggers, faithful to
ADR §3 (and its "any doubt → byte" doctrine):
- **(3) reference modification** — a refmod base demotes **unless** it is a single elementary item whose typed
  form is a UTF-16 `string` (category Alphanumeric / National / Alphabetic; ADR §1.2). A typed `long`/`decimal`/
  `bool` has no character image to slice, so numeric-DISPLAY (incl. overpunch-signed), edited, boolean, pointer,
  and **all groups** (not elementary) demote — the everyday `MOVE WS-DATE(1:4)` on a `PIC 9(8)` idiom. §3.3c
  (variable-bound refmod over a heterogeneous group) is subsumed: a group is never elementary, so it always
  demotes regardless of literal-vs-variable bounds.
- **(11) `CALL … USING … BY REFERENCE`** — each by-reference argument's base symbol demotes **unconditionally**
  (ISO §14.2.3 GR8: the callee occupies the *same storage*; its LINKAGE re-description is unknowable here). BY
  CONTENT / BY VALUE pass a copy and stay typed.
- **(15) ODO whole-group operand** — a group identifier referenced as a whole operand that transitively contains
  an `OCCURS DEPENDING ON` item demotes (sender = current count, receiver = MAX; ISO §13.18.39.3 — one typed
  shape cannot carry both). Element access produces the *element's* identifier, never the group's, so it does not
  trigger; this matches the `RETURN INTO` an ODO record / `READ INTO` MAX behavior (ST146A/RL210A/211A).
- **(4a) group MOVE** — the destination group demotes (it must hold the raw moved image) **unless** the source
  is an unsubscripted group of provably-identical layout — a value-type struct copy, kept typed but recorded as
  a Phase-C *edge*. `SameLayout` compares total length + the ordered (relative-offset, length, category, usage)
  signature of every elementary descendant, so differing SYNC-aligned offsets count as dissimilar even when the
  declared fields match (ADR §3.4). With no layout accessor, two distinct groups are dissimilar (any doubt →
  byte). Per ADR §3.4 a group **comparison** / class-condition / `CORRESPONDING` does **not** demote — a typed
  group materializes its canonical byte image on demand (CORR lowers field-wise) — so the scanner deliberately
  leaves those typed.

**Phase C** runs the structural closure (REDEFINES-class + downward island-membership) **and** the struct-copy
edges in one combined `while(changed)` fixpoint: a same-layout group MOVE is a byte-exact struct copy only when
*both* ends share a representation (a byte source — e.g. an FD record — preserves its exact, possibly
non-canonical, bytes; a typed destination would normalize them, ISO §14.9.25 GR4), so byte on either end of an
edge demotes both. All moves are monotone typed → byte (lattice height 1), so it terminates.

**Three triggers deliberately deferred, documented in the pass:** (14) the write-pattern perf peephole (ADR §2.6
— a performance optimization, never a correctness trigger; omitting it cannot corrupt, and it is coupled to the
Stage-3 typed-string codegen that does not exist yet); (6) `ADDRESS OF` (unreachable — `SET ADDRESS OF` unbound);
and **(16) `USE FOR DEBUGGING` / `DEBUG-ITEM`** (ADR §12 tracked completeness investigation — the monitored
item's character image populates `DEBUG-CONTENTS`; unreachable today because `BoundUseStatement` is a stub). All
three must be closed before a Stage-3 typed flip of an item they could reach.

**Process (the proven pattern):** implemented on `main` → 4-lens / 5-agent **adversarial review** (under-
classification / walker-completeness / ADR-fidelity+SameLayout / termination, each defaulting to refuted) →
**1 confirmed (low) / ~14 refuted**. The one confirmed item was the `BoundUseStatement`/USE-FOR-DEBUGGING
walker-miss above — its adjudicated fix is *documentation only* for Stage 2 (added trigger #16 to the deferred
list). Notable refutations: the "CRITICAL" `GetValueOrDefault`/absent-symbol finding was backwards — an absent
symbol resolves to **byte** at the consumer (`RecordClassification.Get`), the safe direction, never a silent
typed corruption; and the `LayoutSignature` leading group-length tuple is load-bearing-safe (it rejects two
groups of differing total length). +16 classifier unit tests (refmod of numeric/string/group, CALL by-ref vs
by-content/value, ODO whole-group vs element-only, group MOVE identical/dissimilar/byte-source-Phase-C,
group-compare-no-demote, nested-in-IF recursion).

Guard **ALL GREEN: 1175 unit / 481 integration / 364 NIST** (+16 unit; additive — no codegen/NIST change). The
classifier is now complete (Phases A+B+C). Next: a Stage-0 scaffolding slice (`IrDataSlot`/`ByteWindowSlot` +
`Span<byte>` adapters, `PicDescriptor`→`FieldShape`/`NumProfile` split per ADR M6), then wire the classifier into
the Binder and the **first character-data typed flip** (PIC X → .NET `string`, ADR §10 Stage 3).

