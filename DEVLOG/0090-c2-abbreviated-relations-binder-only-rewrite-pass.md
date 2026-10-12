## Entry 090 — 2026-03-16: C2 — Abbreviated Relations (binder-only rewrite pass)

Implemented COBOL abbreviated relational conditions as a binder-level rewrite pass.
No grammar changes, no IR changes, no parser changes — pure bound tree transformation.

COBOL allows `IF A = B OR C` meaning `(A = B) OR (A = C)`, and `IF A > B AND C` meaning
`(A > B) AND (A > C)`. The parser already parses these as logical OR/AND with a bare operand
on the right side. The rewrite pass detects this pattern and expands it.

**Design**: `RewriteAbbreviatedRelations` is a static, recursive, bottom-up tree rewrite called
once from `BindCondition` after the initial binding pass completes. It walks the expression tree
looking for `BoundBinaryExpression(And/Or, relational_expr, bare_operand)` and expands the bare
operand into a full relational expression by propagating the subject and operator from the left
side.

**`ExtractRelationalContext`**: walks the rightmost branch of nested logical chains to find the
most recent relational expression, which provides the subject and operator for expansion. This
handles chained abbreviations like `IF A = B OR C OR D` correctly — each bare operand inherits
from the nearest relational on its left.

**`IsBareOperand`**: identifies `BoundIdentifierExpression` or `BoundLiteralExpression` — the
operands that indicate an abbreviated form. Fully explicit conditions like `IF A < B AND B < C`
pass through unchanged because both sides are relational expressions, not bare operands.

5 integration tests: OR-with-match, OR-no-match, AND-both-true, AND-one-fails,
explicit-not-rewritten.

All methods are `static` — no instance state needed for the rewrite, which makes the pass
easy to reason about and test in isolation.

---

