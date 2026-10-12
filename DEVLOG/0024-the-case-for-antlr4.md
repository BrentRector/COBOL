## Entry 024 — 2026-03-14: The Case for ANTLR4

The user made a compelling argument that the hand-written parser was fundamentally flawed:
the parser was a **separate artifact from the grammar**, and every bug existed because they
drifted apart. ANTLR4 eliminates this entire failure class — the grammar IS the parser.

The user identified 6 traps that break naïve COBOL grammars (context-sensitive keywords,
column-sensitive lexing, "everything is optional" problem, paired terminators, COPY/REPLACE,
free vs fixed format) and showed how a layered ANTLR4 architecture handles all of them.

Decision: clean break. Remove all hand-written lexer/parser/codegen. Rebuild with ANTLR4.

---

