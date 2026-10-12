## Entry 167 — 2026-03-29: The ZERO Saga — Expert Team Cracks the "Impossible" One (91→92)

NC250A had `IF ZERO - WRK-DU-1V0-1 IS NEGATIVE` — ZERO as an arithmetic operand. Five
solo attempts over ~4 hours all failed:

1. **ZERO in `primaryExpression`**: worked for small files, caused exponential ANTLR prediction
   on NC250A's 1971 lines (75 ZERO tokens). Parser hung after 2+ minutes.
2. **Reorder `valueOperand`** (`nonNumericLiteral` first): ZERO matched as figurative in
   `IF ZERO - WRK` before arithmetic could try `ZERO - WRK` as subtraction.
3. **Separate `comparisonOperand`** (arithmetic first): ZERO entered arithmetic in ALL
   comparisons, breaking `IF ZERO IS NOT EQUAL TO alphanumeric-field` (NC103A regression —
   numeric 0 vs figurative "0" have different comparison semantics).
4. **SLL two-stage parsing**: NC250A hung in SLL mode. Added `BailErrorStrategy` to force
   SLL exceptions on ambiguity — but NC250A still hung in the LL fallback.
5. **Various grammar orderings**: every combination caused either performance regression,
   semantic regression (NC103A), or both.

**The breakthrough**: user directed me to "create a team of experts." Three specialist agents
were launched in parallel:

- **ANTLR expert**: proposed a virtual token `ZERO_ARITH` — rewrite ZERO→ZERO_ARITH in a
  pre-parser token stream pass when adjacent to arithmetic operators. ZERO_ARITH lives in
  `primaryExpression`; ZERO stays in `figurativeConstant`. Completely disjoint — zero ambiguity.
- **COBOL expert**: researched the ISO spec exhaustively. Key finding: ZERO is the ONLY
  figurative constant valid in arithmetic expressions (§8.8.1.1). The dual nature (numeric 0
  vs character '0') depends on context — exactly what the token rewriter implements.
- **C# expert**: built `ZeroTokenRewriter.cs` — O(n) scan of the token stream, checking
  adjacency to `+`, `-`, `*`, `/`, `**`, `(`, `)`.

The ANTLR expert's `ZERO_ARITH` design was the key insight. No grammar ambiguity means no
exponential prediction. NC250A compiles in <15 seconds and passes 111 tests.

**Lesson**: when you're stuck in a combinatorial design space (grammar ordering × semantic
correctness × parser performance), bring in specialists who can see the problem from different
angles. The token rewriting approach was outside my "grammar change" tunnel vision.

---

