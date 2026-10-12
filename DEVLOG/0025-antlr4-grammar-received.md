## Entry 025 — 2026-03-14: ANTLR4 Grammar Received

The user provided a complete, layered ANTLR4 grammar set, delivered division-by-division:
- CobolLexer.g4 — shared lexer
- CobolParserCore.g4 — procedural core (expressions, conditions, all statements)
- CobolParserOO.g4 — OO: CLASS-ID, METHOD, INVOKE
- CobolParserGenerics.g4 — TYPEDEF GENERIC, type specifiers
- CobolParserJsonXml.g4 — JSON/XML PARSE/GENERATE
- CobolDialect.g4 — COBOL-85/2002/2014/2023 dialect gates
- CobolPreprocessor.g4 — COPY/REPLACE/pseudo-text

Each grammar file was provided with architectural rationale. Statement set expanded
iteratively: arithmetic → STRING/UNSTRING → SEARCH → CALL/SET → SORT/MERGE →
RETURN/RELEASE/REWRITE → DELETE FILE → STOP/GOBACK/EXIT → START/READ/WRITE.

Saved all grammar files and 4 reference documents:
- ANTLR4-GRAMMAR-ARCHITECTURE.md (607 lines, 14 sections)
- ANTLR4-RATIONALE.md (design rationale)
- SEMANTIC-ANALYSIS-ARCHITECTURE.md (10 semantic passes)
- IL-BYTECODE-GENERATION-DESIGN.md (IL model, codegen)

---

