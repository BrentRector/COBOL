## Entry 176 — 2026-03-31: LPAREN Subscript Trigger Refactor — whitelist HashSet

Production-quality refactor of the LPAREN subscript mode trigger mechanism.

**Problem**: Every new keyword token required updating TWO places: the parser's cobolWord
rule AND an ever-growing chain of || conditions in the LPAREN lexer action. Adding 16
screen-related tokens made this untenable.

**Failed approach**: Blacklist (list tokens that are NOT data names). Failed because:
(1) SUB_RPAREN omission broke reference modification `ITEM(2)(2:2)`, (2) statement
keywords like IF, EVALUATE incorrectly triggered subscript mode on `IF (condition)`,
causing 5 NIST regressions.

**Expert analysis**: ANTLR expert and COBOL expert agents independently analyzed the
problem. ANTLR expert recommended whitelist (small set, safe failure mode). COBOL expert
identified 12 statement keywords that need blacklisting if using blacklist. Whitelist won:
19 tokens to maintain vs 200+, and forgetting a whitelist entry = safe parse error vs
forgetting a blacklist entry = silent wrong behavior.

**Solution**: `_dataNameTokens` HashSet in lexer @members containing IDENTIFIER + all
cobolWord tokens + functionName tokens. `PreviousTokenCouldBeDataName()` does a single
`Contains()` call. LPAREN action: `if (PreviousTokenCouldBeDataName()) PushMode(SUBSCRIPT)`.
Clear comment documenting that `_dataNameTokens` mirrors cobolWord + functionName.

Regression: 922 unit + 329 integration + 95 NIST = 0 failures.

---

