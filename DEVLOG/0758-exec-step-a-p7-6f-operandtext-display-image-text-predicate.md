## Entry 758 — 2026-07-11 01:22 PDT — EXEC STEP A / P7 §6f (OperandText): DISPLAY-image + text-predicate onto the exhaustive operand visitor — the last RENDERING consumer

`OperandText` (a `static` class) `AsString`/`IsString` now dispatch through cached private nested operand-visitors, so
the class stays static and every caller is unchanged: `AsStringVisitor(deSign:false)` / `AsStringVisitor(deSign:true)`
(the `deSign` flag carried on the instance → the two cached instances mean ZERO per-call allocation, no boxing) and a
single `IsStringVisitor`, each `IBoundOperandVisitor<string/bool>`. The eight `Visit` methods are the former switch arms
verbatim; the two pattern-guarded `BoundComputedOperand` arms merged into one `Visit` with an inner intrinsic-category
guard. `AsString`'s `_ =>` caught `BoundBoolOperand` (now an explicit loud `Visit`, byte-identical value); `IsString`'s
four `_ => false` leaves (`BoundBoolOperand`/`BoundFigurative`/`BoundNumericLiteral`/`BoundOperandError`) are now
explicit. `FieldAsString`/`PExpand` untouched. Byte-exact: 32 characterization + 3158 conformance + 265 unit UNCHANGED.

**Step-6 milestone — the clean seam.** With 6f-OperandText this converts EVERY rendering/classification consumer onto
the 6a exhaustive visitor (6b emitter, 6d NumericRenderer, 6e ConditionRenderer, 6f OperandText) — each a one-result-
per-node dispatch, all byte-exact-proven, all loud `_ =>`/`default` arms deleted. What REMAINS are the statement-tree
WALKERS: `BoundStores.StoreKindOf` (6c) and the 6f analyses `AlterCollectFields`/`ContainsNextSentence`/
`KeyedHasNextSentence`. These recurse into nested phrase bodies, so the simple no-default `IBoundStatementVisitor<T>`
would force all 79 `Visit`s for a ~5-arm analysis — the wrong fit. The right tool is a NEW generator variant: a
default-recurse `Bound{Root}Walker` base whose default `Visit` recurses every child Bound node, so a consumer overrides
only the nodes it collects at (and a new leaf still forces the base to recurse it — the exact property that would have
prevented the PHASE-05 UsageCollectionPass nested-handler gaps). Designing that walker variant is deferred (a
`BoundVisitorGenerator` enhancement); `StoreKindOf`'s 9 `_ => null` nodes + coverage caveat are captured in PHASE-07
§6c. This is a natural, coherent stopping point for the visitor conversion.

