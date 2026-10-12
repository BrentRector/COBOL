## Entry 036 — 2026-03-14: Semantic Layer — Symbol Table + Two-Pass Analysis

User provided concrete C# symbol table design. Implemented:
- Symbol hierarchy: Symbol, DataSymbol, ProgramSymbol, SectionSymbol, ParagraphSymbol,
  FileSymbol, ConditionSymbol
- Scope model: hierarchical parent-chain resolution, case-insensitive
- SymbolTable facade: PushScope/Dispose pattern for scoped declaration

SemanticBuilder (Pass 1): walks ANTLR parse tree, creates symbols for data items
(with PIC/USAGE extraction), files, sections, paragraphs.

ReferenceResolver (Pass 2): validates PERFORM/GO TO targets, file name references.

Pipeline wired: Parse → SemanticBuilder → ReferenceResolver.
NC101A passes both semantic passes with zero diagnostics.

Binder design documented for next phase: BoundNode tree between parse tree and CIL codegen.

### Recurring Failures Documented

- **Entry 023 (grammar compliance):** Kept making ad-hoc fixes without reading the grammar.
  User called this out repeatedly. Same failure from Entries 011-022.
- **Entry 027 (overcomplicated diagnosis):** Tried writing test programs instead of using
  the `preprocess` CLI command. User pointed out the obvious approach.
- **Entry 033 (false hang diagnosis):** Attributed a file lock to grammar backtracking and
  made unnecessary changes. Should have checked process list first.

---

