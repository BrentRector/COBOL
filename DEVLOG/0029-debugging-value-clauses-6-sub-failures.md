## Entry 029 — 2026-03-14: Debugging — VALUE Clauses (6 sub-failures)

**Failures in NC101A VALUE clauses:**
1. DECIMALLIT (333.333) not in `literal` rule
2. Figurative constants (ZERO, SPACE) not in `literal`
3. Signed literals (+022.00, -33) — PLUS/MINUS separate from number
4. Leading-dot decimals (.11111) — DECIMALLIT requires leading digits
5. VALUE IS noise word — IS not consumed
6. Comma/semicolon separators — COMMA token between data name and clause

**All 6 fixed in one batch** with user-provided patches:
- `literal` expanded: signedNumericLiteral, figurativeConstant, HEXLIT
- DECIMALLIT: `[0-9]+ '.' [0-9]+ | '.' [0-9]+`
- valueClause: `VALUE IS? literal`
- COMMA/SEMICOLON: `-> skip` in lexer (§8.3.5)
- FILLER added to dataName
- OPEN with openMode (INPUT/OUTPUT/EXTEND)

Result: entire DATA DIVISION now parses cleanly.

---

