## Entry 007 — 2026-03-13: Phase 3 Complete — Control Flow, Strings, Preprocessor, Multi-Program

**Session**: #2 (continued)
**Time**: ~3 hours cumulative across sessions

### What Was Built

All 10 tasks of Phase 3, completing the procedural COBOL feature set:

1. **Sections (3.1)**: Section definitions in the procedure division, section-level PERFORM, fall-through semantics between paragraphs and sections, PERFORM paragraph THRU paragraph.

2. **GO TO (3.2)**: GO TO paragraph, GO TO ... DEPENDING ON. Implemented as call+return from paragraph methods. Note: this does not correctly handle GO TO that crosses PERFORM boundaries — a full solution requires a state machine approach, deferred to Phase 6.

3. **String statement parsing (3.3)**: STRING ... DELIMITED BY ... INTO ... WITH POINTER / ON OVERFLOW. UNSTRING ... DELIMITED BY ... INTO ... TALLYING / ON OVERFLOW. INSPECT (TALLYING, REPLACING, CONVERTING). These are parsed but runtime execution is deferred.

4. **CALL/CANCEL parsing (3.4)**: CALL literal/identifier, BY REFERENCE / BY CONTENT / BY VALUE, RETURNING, ON EXCEPTION / NOT ON EXCEPTION, CANCEL statement, linkage section semantics.

5. **COPY preprocessor (3.5)**: COPY library-name, COPY ... REPLACING with pseudo-text and identifier replacement, nested COPY support, library search path configuration.

6. **REPLACE (3.6)**: REPLACE ==pseudo-text== BY ==pseudo-text==, REPLACE OFF, interaction with COPY REPLACING.

7. **Fixed-form reference format (3.7)**: Columns 1-6 sequence numbers, column 7 indicator area (*, /, D, -), Area A (8-11), Area B (12-72), identification area (73+), continuation lines, auto-detection of fixed vs. free form.

8. **Miscellaneous statements (3.8)**: ACCEPT (FROM DATE, DAY, TIME), CONTINUE, EXIT (PARAGRAPH, SECTION, PROGRAM, PERFORM), INITIALIZE.

9. **Nested programs (3.9)**: Programs within programs, COMMON clause, scope of names.

10. **Compilation group / multi-program (3.10)**: Multiple programs in a single source file, END PROGRAM header matching.

**Test count**: 97 tests passing (up from 94 at end of Phase 2).

### The Preprocessor String Literal Bug

The most instructive bug in Phase 3. The COPY preprocessor scans source text *before* lexing — this is how COBOL specifies it. The preprocessor searches for the keyword `COPY` followed by a library name and a period. The problem: it was doing naive text scanning without tracking whether it was inside a string literal. So this code:

```cobol
DISPLAY "COPY THIS FILE TO OUTPUT".
```

...triggered the preprocessor to interpret `COPY` as a COPY statement, attempting to find and expand a copybook named `THIS`.

**Root cause**: The preprocessor's `FindCopyStatement` and `FindReplaceStatement` methods scanned raw text character by character looking for keywords, but had no concept of string literal boundaries. Since COBOL string literals are delimited by quotes (`"` or `'`), any occurrence of `COPY` or `REPLACE` inside a quoted string would be misinterpreted as a preprocessor directive.

**Fix**: Added string literal tracking to both `FindCopyStatement` and `FindReplaceStatement`. When scanning, the methods now track whether the current position is inside a quote-delimited string and skip keyword matching while inside literals.

**The deeper lesson**: Text-level preprocessing in COBOL happens *before* lexing, so the preprocessor is not a full lexer — but it still must respect string boundaries even though it isn't performing full tokenization. This is a fundamental tension in COBOL's design: the preprocessor operates at the text level but must understand just enough of the language's lexical structure to avoid false matches. This will likely recur with any future text-level processing we add.

### The Fixed-Form Detection False Positive

The auto-detection heuristic for fixed-form vs. free-form source files initially checked whether lines had consistent patterns in columns 1-6 and column 7. The problem: free-form COBOL files that happened to use consistent 7-space indentation (a common coding style) were being detected as fixed-form, because the leading spaces matched the expected pattern for a fixed-form file with blank sequence numbers.

**Fix**: Strengthened the detection by requiring at least one line with actual numeric sequence numbers in columns 1-6. Blank sequence number areas are ambiguous, but numeric content in columns 1-6 is a strong signal of fixed-form format. This eliminated the false positives without rejecting legitimate fixed-form files that do use sequence numbers.

### GO TO Limitations — A Deliberate Deferral

GO TO is implemented as a method call to the target paragraph's method followed by a return. This works for simple cases but breaks when GO TO crosses PERFORM boundaries. Consider:

```cobol
PERFORM PARA-A THRU PARA-C.
...
PARA-A.
    GO TO PARA-C.
PARA-B.
    DISPLAY "SKIPPED".
PARA-C.
    DISPLAY "END".
```

The current implementation calls PARA-C's method and returns, but the PERFORM THRU expects sequential execution through PARA-A, PARA-B, PARA-C. The GO TO should skip PARA-B and continue at PARA-C *within the PERFORM range*, not exit the PERFORM entirely.

The correct solution is a state machine approach where paragraphs are states and GO TO sets the next state, with PERFORM tracking the range boundaries. This is substantially more complex and is deferred to Phase 6 (production quality), where it belongs alongside other control flow edge cases like ALTER.

### Observations

**Preprocessor complexity**: The COPY/REPLACE preprocessor was the most conceptually tricky part of Phase 3, not because the logic is complicated, but because it operates in a twilight zone between raw text and structured tokens. It needs to understand *just enough* about the source language to do its job without being a full lexer. This is historically where COBOL compilers have bugs, and we found the same class of bug ourselves.

**Test growth slowing**: We went from 94 to 97 tests — only 3 new tests for 10 tasks. This is because several tasks (CALL/CANCEL, string statements) were parsing-only without runtime execution, so integration tests aren't yet possible. The test count will increase significantly when runtime support is added for these features.

### What's Next

Phase 4: File I/O. Starting with 4.1: Environment division file control (SELECT ... ASSIGN TO, ORGANIZATION, ACCESS MODE, RECORD KEY, FILE STATUS). This is the gateway to all file operations.

---

