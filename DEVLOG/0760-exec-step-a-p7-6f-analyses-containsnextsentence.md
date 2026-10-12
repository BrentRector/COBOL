## Entry 760 — 2026-07-11 01:58 PDT — EXEC STEP A / P7 §6f-analyses: ContainsNextSentence + AlterCollectFields onto StatementChildren — two hand-walkers collapse, latent gaps closed

The two statement-analysis walkers now recurse over the generated `BoundStatementTree.StatementChildren` (6g) instead
of hand-listing containers:
- `ContainsNextSentence(stmts)` → `stmts.Any(HasNextSentence)` with `HasNextSentence(s) => s is BoundNextSentence ||
  s.StatementChildren().Any(HasNextSentence)`. Deleted: the 20-arm switch, `InSizeError`, and the entire
  `KeyedHasNextSentence`/`KeyedNs` pair (CSharpEmitter.KeyedIo.cs).
- `AlterCollectFields(s, fields)` → `if (s is BoundGoToAlterable g) fields.TryAdd(g.AlterField, g.DefaultPc); foreach
  (child in s.StatementChildren()) AlterCollectFields(child, fields)`. Deleted: the 13-arm switch, `AlterCollectLists`,
  `AlterCollectPhrase`.

Both hand-walkers were INCOMPLETE — `ContainsNextSentence` never recursed into EVALUATE/CALL/WRITE phrase bodies, and
`AlterCollectFields` missed SEQUENCE/CALL/WRITE/keyed/RETURN — a NEXT SENTENCE or alterable GO TO nested there would
have emitted a label-less goto / referenced an undeclared field (a LOUD generated-C# compile failure, per each method's
own doc-comment promise). Riding `StatementChildren` closes those gaps AND makes them drift-proof (a new container node
is covered automatically). Behavior-neutral on the corpus (32 characterization + 3158 conformance + 269 unit UNCHANGED
— nothing exercises the closed gaps), so the fixes are latent-correctness improvements, not regressions. Net −~55 lines
of hand-maintained walker code across three files.

**Step-6 status:** every rendering/classification consumer (6b/6d/6e/6f-OperandText) AND both analysis walkers are
now on the generated visitor/`StatementChildren`. The ONE remaining consumer is `StoreKindOf` (6c) — a per-node
polarity classification (NOT a uniform walk), so it becomes a `StoreKindVisitor : IBoundStatementVisitor<StoreKind?>`
whose recursion rides `StatementChildren`; it is NOT byte-exact-covered (its client is the OO property-ref desugar), so
it is the one to convert with an arm-by-arm diff.

