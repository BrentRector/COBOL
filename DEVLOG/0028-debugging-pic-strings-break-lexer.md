## Entry 028 — 2026-03-14: Debugging — PIC Strings Break Lexer

**Failure:** `PICTURE X(120)` produces 5 tokens: PICTURE, IDENTIFIER("X"), LPAREN,
INTEGERLIT("120"), RPAREN. Grammar expects PIC followed by a single token.

**Diagnosis:** PIC strings are bare character sequences with their own mini-grammar
(X, 9, S, V, parenthesized repeats, editing symbols). They cannot be tokenized by
normal lexer rules because they contain parentheses, periods, commas, plus signs, etc.

**User-provided solution:** PICMODE lexer mode. When lexer sees PIC/PICTURE, push into
PICMODE which captures the entire PIC string as one PIC_STRING token. Key insight:
PIC strings never contain spaces, so the rule `( ~[ \t\r\n.] | '.' ~[ \t\r\n] )+`
correctly handles embedded decimals (9.99) while stopping at sentence-ending periods.

This is how IBM, Micro Focus, and GnuCOBOL handle PIC strings.

---

