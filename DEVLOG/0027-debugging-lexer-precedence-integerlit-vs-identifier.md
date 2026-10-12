## Entry 027 — 2026-03-14: Debugging — Lexer Precedence (INTEGERLIT vs IDENTIFIER)

**Failure:** Parser errors at first `01` level number in DATA DIVISION.
`extraneous input '01' expecting {<EOF>, 'IDENTIFICATION'}`

**Diagnosis:** `01` was lexed as IDENTIFIER because IDENTIFIER rule appeared before
INTEGERLIT in the lexer. ANTLR4's longest-match-first-rule tiebreaker gave IDENTIFIER
priority.

**Root cause identified by user:** Two fixes needed:
1. Move INTEGERLIT before IDENTIFIER (ordering precedence)
2. Restrict IDENTIFIER to start with a letter (COBOL spec)

Applied both. Parser now reaches DATA DIVISION.

---

