## Entry 010 — 2026-03-13: Phase 5 Complete — Intrinsic Functions, Report Writer, OO COBOL, and More

**Session**: #2 (continued)
**Time**: ~5 hours cumulative across sessions

### What Was Built

All 10 tasks of Phase 5, covering COBOL's advanced feature set:

1. **~70 intrinsic functions (5.1)**: Full dispatch infrastructure with implementations across all categories:
   - **Math**: ABS, ACOS, ASIN, ATAN, COS, SIN, TAN, SQRT, LOG, LOG10, MOD, REM, FACTORIAL, INTEGER, INTEGER-PART, and more.
   - **String**: CHAR, LENGTH, LOWER-CASE, UPPER-CASE, REVERSE, TRIM, CONCATENATE, SUBSTITUTE, ORD.
   - **Date/Time**: CURRENT-DATE, DATE-OF-INTEGER, INTEGER-OF-DATE, DATE-TO-YYYYMMDD, YEAR-TO-YYYY, DAY-TO-YYYYDDD, and more.
   - **Financial**: ANNUITY, PRESENT-VALUE.
   - **Aggregates**: MAX, MIN, MEDIAN, MEAN, MIDRANGE, RANGE, VARIANCE, STANDARD-DEVIATION, SUM, ORD-MIN, ORD-MAX.
   - **General**: WHEN-COMPILED, BYTE-LENGTH, NATIONAL-OF, DISPLAY-OF.

2. **Report Writer (5.2)**: Parsing-level implementation. REPORT SECTION in DATA DIVISION, RD entries, report groups (REPORT HEADING, PAGE HEADING, CONTROL HEADING, DETAIL, CONTROL FOOTING, PAGE FOOTING, REPORT FOOTING), INITIATE/GENERATE/TERMINATE statements, LINE/COLUMN/SOURCE/SUM/GROUP INDICATE clauses, CONTROL clause.

3. **Screen Section (5.3)**: Parsing-level. Screen description entries, ACCEPT/DISPLAY screen-name, FOREGROUND-COLOR, BACKGROUND-COLOR, HIGHLIGHT, REVERSE-VIDEO.

4. **Object-oriented COBOL (5.4)**: Parsing-level. CLASS-ID, FACTORY/OBJECT sections, METHOD-ID, INVOKE statement, INTERFACE-ID, inheritance.

5. **Exception handling (5.5)**: Parsing-level. RAISE/RESUME statements, declaratives-based exception model, EC- exception codes, TURN directive.

6. **National (UTF-16) data types (5.6)**: PIC N, USAGE NATIONAL, national literals N"...", national-edited pictures.

7. **Pointer and BASED data (5.7)**: USAGE POINTER, SET ... TO ADDRESS OF, SET ADDRESS OF ... TO, BASED clause.

8. **Communication Section (5.8)**: CD entries, SEND/RECEIVE/ACCEPT MESSAGE COUNT (parsed; largely obsolete in 2023 spec).

9. **Compiler directives (5.9)**: >>SOURCE FORMAT lexing support (FREE/FIXED), plus parsing infrastructure for CALL-CONVENTION, COBOL-WORDS, DEFINE, conditional compilation (IF/EVALUATE/WHEN), FLAG-02, FLAG-14, LISTING, PAGE, PUSH/POP, PROPAGATE, REPOSITORY, TURN.

10. **Standard classes (5.10)**: Parsing-level mapping for standard class library as specified in section 16.

**Test count**: 133 tests passing (up from 103 at end of Phase 4). 30 new intrinsic function unit tests.

### The Intrinsic Function Emission Bug

This was the most significant bug in Phase 5 and a direct callback to Entries 008 and 011.

**Symptoms**: All intrinsic function calls returned zero. The demo program called functions like `FUNCTION ABS(-42.5)` and `FUNCTION SQRT(144)` — every result was `0`.

**Investigation**: The parser was producing correct `FunctionCallExpression` AST nodes. The function dispatch infrastructure in the runtime was implemented and tested in isolation. The problem was in the CIL emitter.

**Root cause**: `EmitArithmeticExpression` in the CIL code generator had cases for `BinaryExpression`, `UnaryExpression`, `LiteralExpression`, and `IdentifierExpression` — but no case for `FunctionCallExpression`. When it encountered a function call node, it fell through to the default case, which pushed `0m` (decimal zero) onto the evaluation stack. No error, no warning — just silently wrong results.

**Fix**: Added `EmitIntrinsicFunctionCall` — a new emission method that evaluates function arguments, pushes them onto the stack, and calls the appropriate runtime dispatch method. Wired it into `EmitArithmeticExpression`'s switch statement.

**Why this matters**: This bug was invisible to unit tests because the parser tests verified correct AST construction and the runtime tests verified correct function computation — both passed. The gap was in the *glue* between them: the code generator that translates AST nodes into CIL. Only running the actual compiled program and checking its output revealed the bug. The user caught this by running the demo and noticing all function results were zero. This is the correct workflow: run the demo, check the output, and when it is wrong, fix the compiler. This reinforces Entry 008's lesson — the demo source was valid COBOL, and the fix belonged in the compiler, not the source.

### Compiler Directives and >>SOURCE FORMAT

The `>>SOURCE FORMAT` directive required changes at the lexer level, not the parser level. The directive tells the compiler whether subsequent source lines should be interpreted as free-form or fixed-form. Since the lexer is responsible for column-position-dependent tokenization (Area A/B in fixed-form), the format switch must happen before tokens are produced. This was implemented as a lexer-level directive scan that runs before the main tokenization loop for each line.

### Observations

**The unit test gap**: The intrinsic function emission bug is a textbook example of why integration tests matter. Unit tests for the parser confirmed correct AST output. Unit tests for the runtime confirmed correct function computation. But neither tested the full pipeline from source to executed result. The 30 new intrinsic function unit tests verify individual function correctness, but it was the end-to-end demo execution that found the emission gap.

**Parsing-level vs. full implementation**: Several Phase 5 features (Report Writer, Screen Section, OO COBOL, exception handling) are parsing-level only — the AST nodes are created but code generation and runtime support are not yet implemented. This is a deliberate strategy: getting the parser right ensures the language surface area is recognized, and full runtime support can be added incrementally in Phase 6 or beyond without parser rework.

**Feature breadth**: Phase 5 had the widest scope of any phase — 10 task groups spanning intrinsic functions, Report Writer, OO features, exception handling, compiler directives, national types, pointers, communication, and standard classes. The parsing-level approach for several features kept this manageable while still making meaningful progress across the entire spec surface area.

### What's Next

Phase 6: Production Quality & Conformance. Starting with 6.1: NIST COBOL85 test suite integration. This is where the compiler faces its first external validation — ~400 standardized test programs that every COBOL compiler is measured against.

---

