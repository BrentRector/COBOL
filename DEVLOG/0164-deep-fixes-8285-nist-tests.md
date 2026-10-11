## Entry 164 — 2026-03-28: Deep Fixes — 82→85 NIST Tests

**3 more NIST tests pass** via deeper fixes:

1. **NC302M** — OBJECT-COMPUTER computerAttributes uses `~(DOT|PROGRAM)+` to accept any
   tokens including MEMORY SIZE. STOP literal (Format 2) added as grammar alternative.
2. **NC252A** — RENAMES THRU qualified name resolution: store OF/IN qualifier from
   dataReference, resolve by walking parent chain. `ResolveQualifiedDataName` helper.
3. **NC208A** — Qualified paragraph names: `ExtractProcedureNameText()` extracts first
   IDENTIFIER from procedureName, ignoring OF/IN qualifiers. Fixed in BoundTreeBuilder
   (PERFORM, GO TO, ALTER, SORT/MERGE) and ReferenceResolver.

Also: obsolete ID paragraph content rules use `~DOT+` (any tokens until period).

**Remaining (10 NC tests — all need deep architectural work):**
- **Preprocessor:** NC205A (continuation splits keywords mid-word)
- **Lexer:** NC125A (PIC string period ambiguity)
- **Grammar:** NC250A (ZERO in arithmetic — ANTLR ambiguity), NC216A (INSPECT multi-pattern)
- **Semantic:** NC225A (EVALUATE class conditions + TRUE/FALSE lowering)
- **Runtime:** NC201A (PERFORM VARYING subscripted loop variable), NC220M, NC237A (known)
- **Flagging:** NC303M, NC401M (DISPLAY-only, no test assertions)

---

