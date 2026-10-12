## Entry 034 — 2026-03-14: Debugging — ANTLR Warnings Eliminated

Three ANTLR warnings:
1. `implicit definition of token METHOD` — METHOD used in exitStatement but not in lexer
2. `implicit definition of token REPLACING` — REPLACING used in INSPECT but not in lexer
3. `parameterDescriptionBody optional block can match empty` — `dataDescriptionClauses?`
   where `dataDescriptionClauses: dataDescriptionClause*` can be empty

Fixes: METHOD and REPLACING tokens added to lexer. parameterDescriptionBody restructured
to `(dataDescriptionClause+)?`. exitStatement changed from string literal `'SECTION'` to
token `SECTION`.

Result: **zero ANTLR warnings, zero ANTLR errors.**

---

