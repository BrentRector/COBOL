## Entry 089 — 2026-03-16: C1 — NEXT SENTENCE (production-quality sentence structure)

Implementing NEXT SENTENCE forced a structural refactor of the bound tree — and the result is a
cleaner, more accurate model of the COBOL domain.

**The problem**: `BoundParagraph` held a flat `IReadOnlyList<BoundStatement>`. Sentence boundaries
were discarded during binding — `BoundTreeBuilder` iterated `sentence.statement()` and flattened
everything into one list. This made NEXT SENTENCE impossible to implement correctly, since there
was no sentence to jump past.

**The refactor**: Introduced `BoundSentence` as a first-class node holding
`IReadOnlyList<BoundStatement>`. Changed `BoundParagraph` from flat statement list to
`IReadOnlyList<BoundSentence>`. The bound tree now models the COBOL structure faithfully:
program → paragraphs → sentences → statements.

**Binder changes**: Paragraph lowering now iterates sentences explicitly. Each sentence gets a
`sentenceEnd` basic block. A `_currentSentenceEnd` field tracks the active target.
`LowerNextSentence` emits an `IrJump` to it and creates a dead block for unreachable code after
the jump. No new IR nodes needed — reuses existing `IrJump`.

**No regressions**: The sentence-aware lowering preserves existing behavior perfectly because the
sentenceEnd blocks simply fall through in normal flow. All 6 NIST programs remain at 100%.

3 integration tests: skip-rest-of-sentence, skip-multiple-statements, nested-IF escape.

---

