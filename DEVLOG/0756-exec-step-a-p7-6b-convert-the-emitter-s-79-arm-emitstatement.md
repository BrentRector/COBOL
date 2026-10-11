## Entry 756 — 2026-07-11 00:54 PDT — EXEC STEP A / P7 §6b: convert the emitter's 79-arm EmitStatement onto the exhaustive IBoundStatementVisitor — the loud `default` is GONE

First consumer converted onto the 6a generator. `CSharpEmitter` now implements `IBoundStatementVisitor<bool>` (the
interface added on a new partial `CodeGen/CSharpEmitter.Dispatch.cs`); the 79 `case Bound…` arms of `EmitStatement`
became 79 `public bool Visit(BoundX n)` methods (each arm body verbatim — the pattern var renamed to `n`, `w`
re-localized per method), and `EmitStatement` collapsed to `private bool EmitStatement(BoundStatement s) => s.Accept(this);`.
The `default: LoudStmt($"bound statement '{s.GetType().Name}'")` is **deleted** — that "forgot an arm" runtime guard is
now a **compile error**: adding a `BoundStatement` leaf regenerates `IBoundStatementVisitor` with a new `Visit`, and
`CSharpEmitter` fails to compile until it handles it. (bool = "unconditionally transfers control out of the paragraph
case" — the contract is unchanged.)

As-built refinement vs the doc sketch (kept CURRENT in PHASE-07 §Step-6 6b): the emitter stays ONE class rather than
splitting a separate `StatementEmitter` collaborator — that decomposition needs the P6 immutable `EmitContext`/
`BinderContext` and is deferred to Exec Step D; pulling it out now would only fight the shared mutable emitter state
(`_ctx`/`_currentPc`/`_sentenceEndLabel`/`_num`). A partial adding the interface is the clean, zero-risk seam.

Byte-exact behavior-neutral: 32 characterization snapshots + 3158 conformance UNCHANGED (dispatch mechanism swapped,
emitted C# identical). Compiler + full battery green. NEXT: 6c — `BoundStores.StoreKindOf` onto its `IBound*Visitor`,
then the renderers (6d `NumericRenderer`, 6e `ConditionRenderer`, 6f `OperandText` + the small statement analyses).

