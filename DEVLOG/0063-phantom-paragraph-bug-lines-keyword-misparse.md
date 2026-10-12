## Entry 063 — 2026-03-15: Phantom Paragraph Bug — LINES Keyword Misparse

### The Bug

`WRITE DUMMY-RECORD AFTER ADVANCING 1 LINES.` — the grammar's `writeBeforeAfter`
rule consumed `AFTER ADVANCING 1` but NOT `LINES`. The unconsumed `LINES` token
followed by `.` was misinterpreted as a paragraph definition (`LINES.`), creating
a phantom paragraph at index 17 that shifted ALL subsequent paragraph indices.
Every `GO TO` targeting a paragraph after index 17 jumped to the wrong destination.

This was the root cause of F1-1 showing DE-LETE instead of PASS — the `GO TO
MPY-WRITE-F1-1` resolved to the wrong index and landed on MPY-DELETE-F1-1.

### AI Failure: IDENTIFIER Workaround

First attempt at fixing this was wrong: added `writeAdvancingUnit: IDENTIFIER` to
consume the stray token. This is incorrect because it accepts ANY identifier, not
just LINE/LINES. The user correctly rejected this and demanded the proper fix.

**Lesson:** When a token is needed in a split grammar, add it to the LEXER as a
real token. Never use IDENTIFIER as a catch-all workaround. This is the second
time the user has had to correct a "shortcut instead of proper fix" pattern.

### The Correct Fix

1. **Lexer**: Added `LINE` and `LINES` as real keyword tokens in CobolLexer.g4
2. **Parser**: `writeBeforeAfter` now uses `(LINE | LINES)?` with proper tokens
3. **Parser**: Added `superClass = CobolParserCoreBase` option
4. **Parser**: `paragraphName` rule now has `{IsAtLineStart()}?` semantic predicate
   to prevent stray identifiers from becoming paragraph names
5. **Parser base class**: `CobolParserCoreBase.IsAtLineStart()` checks if the
   current token is the first token on its line

### Also Fixed: EmitLoadDecimal Precision

Separate discovery: `EmitLoadDecimal` was converting decimal→double→decimal via
`ldc.r8` + `new decimal(double)`, introducing floating-point precision loss.
320.48m round-tripped through double is not exactly 320.48m. Fixed to use
`decimal.GetBits()` + the 5-arg `decimal(lo, mid, hi, isNeg, scale)` constructor.

### Still Missing (from this entry)

- Binder phantom paragraph validation
- Binder GO TO target validation
- Regression tests for phantom paragraphs

---

---

