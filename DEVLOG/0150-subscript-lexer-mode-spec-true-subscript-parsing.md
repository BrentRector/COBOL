## Entry 150 — 2026-03-25: SUBSCRIPT Lexer Mode — Spec-True Subscript Parsing

**The production-quality fix for the subscript +N ambiguity.** After two rounds of failed
hacking (Entries 148-149), implemented the correct solution: a dedicated ANTLR4 lexer mode
that preserves spacing inside subscript parentheses.

**Architecture:**
- New `SUBSCRIPT` lexer mode entered when `(` follows an IDENTIFIER
- `SIGNED_INTEGERLIT` token captures `+N`/`-N` (sign adjacent to digits) as a single token
- `SUB_PLUS`/`SUB_MINUS` remain separate for spaced operators (`I + 1`)
- `SUB_WS` preserved (not skipped) so the binding layer can split on subscript boundaries
- `SUB_OF`/`SUB_IN`/`SUB_ALL` keywords listed before `SUB_IDENTIFIER` (ANTLR first-match)
- `SUB_RPAREN` pops mode; `SUB_LPAREN` pushes for nested parens
- `@members` with `NextToken` override tracks last non-WS token type for mode entry

**Parser:** Flat `subToken+` rule captures all SUBSCRIPT-mode content. Binding layer interprets:
- `SUB_COLON` present → ref-mod (start:length with arithmetic)
- No colon → subscripts, split on WS/COMMA boundaries using sign-adjacency for disambiguation

**Binding layer token interpreter:**
- `SplitSubscriptTokens`: splits on whitespace boundaries, won't split after operators or
  OF/IN qualifiers
- `BindSubscriptSegment`: handles signed literals, unsigned literals, qualified identifiers
  with relative offset
- `BindSubscriptTokensAsArithmetic`: general arithmetic for ref-mod start/length

**Collateral fixes:**
- Replaced implicit string literals in parser (`'+' '-' '*' '/' '**' 'REFERENCE' 'CONTENT'
  'RECORD' 'BEFORE' 'AFTER'`) with explicit token names — required because ANTLR4 can't
  create implicit tokens for characters that also appear in mode-specific rules

**Results:**
- NC134A: 20/20 — 100% (was "won't compile" — the original subscript blocker)
- NC206A, NC224A: zero regressions (qualified subscripts work correctly)
- NC121M: relative subscripting `(INDEX1 + 2)` still works
- All 63 guard tests pass
- All 216 unit + 181 integration tests pass (3 skipped: COBOL-2002 features)

**ANTLR4 token dump** (used to diagnose the `OF`-as-`SUB_IDENTIFIER` bug): temporarily added
`COBOL_DUMP_TOKENS` env var support to print SUBSCRIPT-mode tokens. Found that `SUB_OF : 'OF'`
must precede `SUB_IDENTIFIER` in the lexer (same-length match → first rule wins).

---

