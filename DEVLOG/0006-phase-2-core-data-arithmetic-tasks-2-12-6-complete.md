## Entry 006 — 2026-03-13: Phase 2 Core Data & Arithmetic — Tasks 2.1–2.6 Complete

**Session**: #2 (continued)
**Time**: ~2.5 hours cumulative across sessions

### What Was Built

Tasks 2.1 through 2.6 of Phase 2, covering the core data model and arithmetic/conditional
infrastructure:

1. **Full PICTURE clause parsing (2.1)**: All PICTURE symbols — 9, X, A, V, S, P, Z, *, +, -, CR, DB, B, 0, /, comma, period, currency symbol. Repeat counts (`9(5)`, `X(10)`). Edited pictures (numeric edited, alphanumeric edited). Category determination from PICTURE string.

2. **USAGE clause (2.2)**: DISPLAY (default), BINARY/COMP/COMP-4/COMP-5, PACKED-DECIMAL/COMP-3, INDEX, POINTER, FUNCTION-POINTER, PROCEDURE-POINTER. Storage size calculation per USAGE type. Alignment rules.

3. **Data hierarchy and groups (2.3)**: Level numbers 01-49, 66, 77, 88. Group items as composite structures. OCCURS clause (fixed and DEPENDING ON). REDEFINES clause. RENAMES (level 66). Condition-names (level 88). FILLER items. JUSTIFIED, BLANK WHEN ZERO, VALUE, SYNCHRONIZED clauses.

4. **MOVE statement — full semantics (2.4)**: Numeric-to-numeric (scaling, truncation, sign handling). Numeric-to-alphanumeric/edited. Alphanumeric-to-alphanumeric (space-padding, truncation). Group MOVE (byte-level copy). MOVE CORRESPONDING.

5. **Arithmetic statements (2.5)**: ADD, SUBTRACT, MULTIPLY, DIVIDE (all forms including GIVING, CORRESPONDING, REMAINDER). COMPUTE with full arithmetic expression support. ROUNDED phrase. ON SIZE ERROR / NOT ON SIZE ERROR.

6. **Conditional expressions (2.6)**: IF/ELSE/END-IF. Relation conditions. Class conditions (NUMERIC, ALPHABETIC). Sign conditions. Condition-name conditions (level 88). Combined conditions (AND, OR, NOT). Abbreviated combined conditions. EVALUATE/WHEN/WHEN OTHER/END-EVALUATE.

**Test count**: 88 tests passing (up from 43 at end of Phase 1).

### The REDEFINES Offset Bug

The only bug found in this batch of work. When processing REDEFINES, items were being assigned
sequential offsets (each item placed after the previous one) instead of sharing the offset of
the item being redefined. For example:

```cobol
01 WS-DATE         PIC 9(8).
01 WS-DATE-PARTS REDEFINES WS-DATE.
   05 WS-YEAR      PIC 9(4).
   05 WS-MONTH     PIC 9(2).
   05 WS-DAY       PIC 9(2).
```

`WS-DATE-PARTS` must start at the same offset as `WS-DATE` — they share the same memory. The
bug was assigning `WS-DATE-PARTS` a new sequential offset, so it occupied different memory than
`WS-DATE`, completely defeating the purpose of REDEFINES.

This is exactly the kind of semantic bug that's easy to write and hard to spot visually. The
tests caught it.

### Level Numbers: Lexer vs. Parser — An Architecturally Correct Decision

An interesting design challenge carried forward from the Phase 1 lexer bug (#2 in Entry 005):
COBOL level numbers (01, 05, 10, 66, 77, 88) look identical to integer literals. The string
`05` in a DATA DIVISION is a level number; the string `05` in a COMPUTE statement is the number
five.

The Phase 1 fix moved level number recognition from the lexer to the parser, treating all
digit sequences as numeric tokens and letting the parser decide based on context whether it's a
level number or a literal. This turned out to be the architecturally correct decision for
COBOL — the lexer produces context-free tokens, and the parser applies context-sensitive
interpretation. This pattern served us well throughout all of Phase 2's data hierarchy work,
where level numbers appear constantly and must be distinguished from numeric operands.

### The C# Ternary Lesson Carries Forward

The C# ternary type coercion bug from Phase 1 (Entry 005, Bug #1) was a good lesson that
carried forward. Throughout Phase 2 implementation, we were more careful about implicit type
conversions in conditional expressions, avoiding the pattern of boxing different numeric types
through ternary operators. Once bitten, twice shy — and having the bug documented in the devlog
made it easy to remember.

### Observations

**Growing test suite confidence**: Going from 43 to 88 tests means the test suite is becoming
a real safety net. The REDEFINES offset bug was caught purely by tests — it would have been
nearly invisible to manual code review since the offset calculation logic looks plausible at a
glance.

**Pace**: Six task groups completed in one continued session. The data model work (PICTURE,
USAGE, hierarchy) is foundational — everything in later phases depends on getting this right.
The time investment here pays dividends later.

### What's Next

Task 2.7: PERFORM statement. This is a significant control flow challenge — out-of-line
PERFORM (paragraph/section), PERFORM THRU, inline PERFORM/END-PERFORM, PERFORM TIMES,
PERFORM UNTIL, PERFORM VARYING (single and nested), TEST BEFORE/TEST AFTER. After that,
table handling (2.8), reference modification (2.9), and figurative constants (2.10) to
complete Phase 2.

---

