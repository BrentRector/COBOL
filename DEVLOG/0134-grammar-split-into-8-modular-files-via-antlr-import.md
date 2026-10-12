## Entry 134 — 2026-03-21: Grammar Split into 8 Modular Files via ANTLR Import

Split the 2027-line monolithic `CobolParserCore.g4` into 8 files using ANTLR4 `import`:

```
Grammar/CobolParserCore.g4          — top-level: compilationUnit, divisions, statement dispatcher
Grammar/Core/CobolExpressions.g4    — literals, arithmetic, conditions, comparisons
Grammar/Core/CobolData.g4           — data division, OCCURS, VALUE, INITIALIZE
Grammar/Core/CobolSpecialNames.g4   — SPECIAL-NAMES clauses
Grammar/Core/CobolReportWriter.g4   — REPORT SECTION, RD, TYPE, SUM
Grammar/Core/CobolIO.g4             — OPEN/CLOSE/READ/WRITE/STRING/UNSTRING/INSPECT/SORT
Grammar/Core/CobolControlFlow.g4    — PERFORM, IF, EVALUATE, GO TO, SEARCH, ALTER, USE
Grammar/Core/CobolExtensionsJsonXml.g4 — JSON/XML/INVOKE stubs
```

ANTLR `import` works correctly — imported grammars are bare `parser grammar` files with no
`options` block. The top-level grammar has `import` + `options { tokenVocab; superClass; }`.
Build script updated to copy `CobolLexer.tokens` into `Core/` temporarily during generation.

No rules duplicated. No behavior changes. All 119 unit + 176 integration tests pass.

---

