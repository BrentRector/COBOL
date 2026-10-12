## Entry 117 — 2026-03-20: Unified COBOL Diagnostic Codes Across All Compiler Phases

### The problem

49 NIST tests fail to compile. A COBOL programmer looking at the errors sees three different
coding schemes depending on which compiler phase failed: `ANTLR` codes from the parser,
`CS08xx` codes from the binder, and `CIL` codes from emission. The messages themselves ranged
from meaningless ("cannot parse construct near 'IDENT-1'") to too-technical
("CS0872: unresolved reference"). NC203A alone produced 42 cascading errors for what was
fundamentally one repeated pattern (`NOT =` abbreviated conditions).

### What was done

**Structured `DiagnosticHint` in CobolErrorStrategy** — replaced `List<string>` with
`record struct DiagnosticHint(Code, Message, Priority)`. `BuildMessage` now deduplicates by
code prefix, sorts by priority (lower = more important), and caps at 2 hints per error.
The first hint's code becomes a `[COBOLxxxx]` prefix that the error listener extracts.

**Unified code scheme across all phases:**
- `COBOL0001-0099` — General syntax errors (fallback)
- `COBOL0100-0199` — Feature not yet supported (correct COBOL, not yet implemented)
- `COBOL0200-0299` — Reserved word / naming conflicts
- `COBOL0300-0399` — Structural errors (missing period, missing keyword)
- `COBOL0400-0499` — Binder/semantic errors (procedure names, CORRESPONDING, subscripts)
- `COBOL0500-0599` — Lowering errors (PERFORM index, GO TO targets)
- `COBOL0600` — Internal compiler error (CIL emission failure)

**Error count cap (20 per file)** — `CobolErrorListener` now counts errors and silently drops
after 20. NC203A went from 42 errors to exactly 20, all with the same root-cause code.

**Three new parser heuristics:**
- `#22 COBOL0311`: `NOT =` / `NOT >` / `NOT <` abbreviated conditions. The grammar has
  `NOT EQUAL` (word form) but not `NOT EQUALS` (symbol form). First attempt used rule-stack
  checks (`IsInRule(ruleStack, "relationalExpression")`) — failed because ANTLR4's adaptive
  LL(*) prediction reports errors before entering the target rule method. Broadened to pure
  token-pattern matching: `prev==NOT && token.Type==EQUALS`. Distinctive enough to avoid
  false positives.
- `#23 COBOL0108`: Multi-target SET (`SET id1 id2 TO value`). The grammar allows one
  identifier before TO/UP/DOWN. The heuristic fires when an identifier appears in a SET
  context. Had to handle `NoViableAlternative` separately (expectedTokens is null) vs
  `InputMismatch` (expectedTokens contains 'TO').
- `#25 COBOL0312`: FILE CONTROL context errors.

### The AI-friction moment

The `NOT =` heuristic took three iterations. My first version required the rule stack to include
`relationalExpression` — seemed logical since that's where the grammar fails. But ANTLR4's
adaptive prediction runs in the ATN, not via recursive descent, so by the time the error
strategy fires, the rule stack reflects the prediction entry point, not the target rule.
The second version broadened to check `relationalExpression || relationalOperator || condition` —
still didn't match. The third version dropped the rule-stack requirement entirely: `NOT`
followed by `=`/`>`/`<` is distinctive enough in COBOL that no rule context is needed.
This is a good lesson: when pattern-matching parser errors, the token sequence is more reliable
than the rule stack.

### Diagnostic migration

Every `CS08xx` code across BoundTreeBuilder (8 codes), Binder (10 codes), CorrespondingMatcher
(3 codes), and Compilation.cs (1 code) migrated to `COBOLxxxx` scheme with human-readable
messages. Examples:
- `CS0872: unresolved reference` → `COBOL0402: Paragraph or section 'X' not found. Check spelling or verify it is defined in the PROCEDURE DIVISION.`
- `CIL: emission failed` → `COBOL0600: Internal compiler error while generating code for 'PROGRAM-ID'. Please report this.`

### Test coverage

19 error strategy tests (up from 12): abbreviated conditions (NC172A, NC203A), multi-target SET
(NC131A, NC140A), diagnostic code assertions (COBOL01xx, COBOL0311, COBOL0108, COBOL0200),
error count cap verification.

### Numbers

- 119 unit tests pass, 188 integration tests pass (1 skip), 10 NIST at 100%
- guard.sh: ALL GREEN

---

