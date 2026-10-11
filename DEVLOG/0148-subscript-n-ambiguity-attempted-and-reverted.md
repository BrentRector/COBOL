## Entry 148 — 2026-03-25: Subscript +N Ambiguity — Attempted and Reverted

Attempted to fix the signed literal subscript ambiguity where `ANIMAL (+8 W-2 +3)` parses
`+8 +1 +3` as one arithmetic expression instead of three subscripts.

**Three approaches tried, all failed:**
1. `signedIntegerLiteral | ALL | arithmeticExpression` — `+N` at start of subscript was matched
   correctly, but `W-2 +3` still consumed `+3` as binary addition in the multiplicative path.
2. `multiplicativeExpression ( (PLUS|MINUS) multiplicativeExpression )?` — blocked addition
   between subscripts but broke `ITEM(I + 1)` (relative subscript with integer offset).
3. `multiplicativeExpression ( (PLUS|MINUS) IDENTIFIER )?` — fixed the integer case but broke
   `ITEM(I + 1)` because `1` is `INTEGERLIT`, not `IDENTIFIER`.

**Root cause**: `(I + 1)` and `(+8 W-2 +3)` are fundamentally ambiguous in ANTLR LL(*) without
commas. `I + 1` is one subscript with addition; `W-2 +3` is two subscripts. The only difference
is context (OCCURS depth), which isn't available at parse time.

**Decision**: Reverted to `arithmeticExpression` subscripts. NC134A/NC139A remain blocked.
NC138A and NC245A gained compilation (different subscript pattern). This is a known limitation
documented for future work — may need a post-parse subscript rewrite pass similar to
abbreviated conditions.

---

