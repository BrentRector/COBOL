## Entry 086 — 2026-03-16: Level-88 Condition Names — Full Pipeline

Implemented level-88 condition names end-to-end:

**SemanticBuilder**: Level-88 entries now properly find their parent DataSymbol from the data stack, extract VALUE clauses (single values, multiple values, THRU ranges), and populate `ConditionSymbol.ValueRanges`. Previously created with `null!` parent and no values.

**BoundConditionNameExpression**: New bound node carrying the `ConditionSymbol` and optional `IsNegated` flag. Resolved in `BindRelational` when a bare identifier matches a level-88 name, and in `BindEvaluateWhenGroup` for EVALUATE TRUE.

**LowerConditionName**: Expands level-88 tests into IR — for each value in the condition's ranges, emits numeric or string comparison against the parent field, then ORs all match results. Supports single values, multiple values, and THRU ranges.

4 integration tests: single value, multiple values (VALUES 6 7), EVALUATE TRUE with condition names, alphanumeric parent (PIC X, VALUE "Y"/"N").

Class conditions (IF NUMERIC/ALPHABETIC) deferred — requires NUMERIC/ALPHABETIC lexer tokens which would be a grammar change. Abbreviated relations deferred — requires grammar extension for relation chains.

---

