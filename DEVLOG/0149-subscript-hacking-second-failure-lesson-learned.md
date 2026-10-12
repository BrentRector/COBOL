## Entry 149 — 2026-03-25: Subscript Hacking — Second Failure, Lesson Learned

Second round of subscript attempts, all failed for the same reason as the first: trying to
work around an incorrect grammar instead of implementing the spec.

**What was tried:**
1. Spec-correct `subscriptEntry` with `IDENTIFIER qualification* ((PLUS|MINUS) INTEGERLIT)?`
   — worked for signed literals but relative subscripting consumed `+N` greedily
2. Semantic predicate `{TokenStream.LT(3).Type == RPAREN}?` on relative offset — broke
   multi-subscript relative forms like `(W-3 + 5  W-2 - 10  W-1 + 2)`
3. Preprocessor comma insertion — technically worked but defeated by `COMMA_SEP -> skip`
   lexer rule that swallows commas before the parser sees them

**Why it failed:** Every attempt tried to patch one symptom while creating another. The
`COMMA_SEP` skip rule, the `arithmeticExpression` greedy consumption, the relative offset
optional match — all are consequences of a grammar not designed for COBOL-85 subscripts.

**The lesson (again):** The COBOL-85 spec defines subscripts as a restricted form that is
LL(1)-parseable. The grammar should implement this form directly. There is no ambiguity
to "solve" — there's only a wrong grammar to replace with the right one. The `COMMA_SEP`
skip rule, relative subscripting, and signed literals all work correctly when the grammar
matches the spec.

**Action:** Reverted all changes. This needs a proper spec-driven grammar redesign with
user approval before implementation.

---

