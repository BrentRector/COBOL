## Entry 759 — 2026-07-11 01:40 PDT — EXEC STEP A / P7 §6g: the WALKER foundation — a GENERATED `StatementChildren` (the drift-proof statement-tree shape)

The rendering consumers (6b/6d/6e/6f) fit the exhaustive no-default `IBoundStatementVisitor<T>`; the WALKER consumers
(`StoreKindOf`, `AlterCollectFields`, `ContainsNextSentence`, `KeyedHasNextSentence`) do NOT — a walker recurses into
nested statements, and forcing a `Visit` per node does not stop it forgetting to RECURSE into a new node's children
(the exact PHASE-05 `UsageCollectionPass` completeness bug). The fix is to centralize + drift-proof the "child
statements of node X" knowledge. Landed the FOUNDATION for that (additive, like 6a):

**`BoundVisitorGenerator` now also emits `BoundStatementTree.StatementChildren(this BoundStatement) : IEnumerable<BoundStatement>`.**
It discovers each leaf's directly-nested statements by reading EVERY property through the semantic model — completeness
BY CONSTRUCTION. Per-property rule: a prop that IS-or-derives `BoundStatement` → `One(x.P)`; `IReadOnlyList<BoundStatement>`
→ `Nz(x.P)`; a single-depth *statement-bearing helper record* (SizeErrorPhrase / KeyedInvalidKey / BoundSearchWhen /
BoundEvaluateWhen — detected generically, no hard-coding) → recurse its props (`x.P?.Q`); a list of such → `SelectMany`.
A visited-set bounds the walk through cyclic data-model types (`DataItem.Children → DataItem`); the Bound-root
hierarchies (Expr/Condition/Operand/…) are pruned (they never hold statements). Key bug found + fixed while building:
the child/list props are typed as the *root* `BoundStatement`, so the match must be IS-**or-equals** the root, not just
strict derivation (`IsStatementLike`) — the first generation with a derives-only check produced an empty switch.

The emitted 28-arm switch was verified against BOTH the ISO child-phrase taxonomy AND `StoreKindOf`'s hand-listed
`Kids` calls — an EXACT match, now derived instead of hand-maintained (e.g. `BoundOutOfLinePerform` correctly has NO
arm — its body is a pc-range, not nested statements). `BoundStatementChildrenTests` (4) locks it: direct-children,
one-level-not-transitive, empty-leaf, and — the robustness guard — EVERY one of the 79 leaves called on an
uninitialized node returns empty without throwing (exercises each arm's `Nz`/`One`/`?.`/`?? Empty`). Additive /
behavior-neutral: 3158 conformance + 269 unit (+4) + 32 characterization UNCHANGED (no consumer wired yet — like the 6a
visitor foundation). **NEXT: wire the four walkers onto `StatementChildren` — `StoreKindOf` (6c, per-node polarity +
`Kids`-over-`StatementChildren`; the 9 `_ => null` nodes stay null; diff arm-by-arm as it is not byte-exact-covered)
and the three analyses (generic recurse + node-specific collect).**

