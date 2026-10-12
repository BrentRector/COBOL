## Entry 158 — 2026-03-27: Intrinsic Functions — Full Implementation (91 Functions)

**Context:** Intrinsic functions were entirely non-functional — the binder returned literal 0
for every FUNCTION call. The runtime had 38 dispatch entries (some buggy), but they were
unreachable from COBOL source.

**3 parallel agents + 1 test coverage audit:**

Agent 1 (binder pipeline): Removed `is2002()` grammar gate (1989 Amendment made functions part
of COBOL-85). Added BoundFunctionCallExpression → IrFunctionCall → CIL emission calling
IntrinsicFunctions.Call(). FUNCTION LENGTH resolved at compile time as field's ElementSize.
Added functionCall to moveSendingOperand grammar. Key design: function arguments parsed through
SUBSCRIPT lexer mode (subscript tokens in dataReference parentheses).

Agent 2 (COBOL-85 function fixes): Added RANDOM, DAY-TO-YYYYDDD. Fixed 11 bugs: NUMVAL/NUMVAL-C
rewritten with proper COBOL parser, MAX/MIN/ORD-MAX/ORD-MIN string overloads, TRIM LEADING/
TRAILING keywords, SUBSTITUTE variadic pairs, DATE-TO-YYYYMMDD/YEAR-TO-YYYY optional args,
WHEN-COMPILED 21-char UTC offset, CONCAT alias.

Agent 3 (COBOL-2002+ functions): Added 25 functions (17 real + 8 stubs): E, SECONDS-PAST-MIDNIGHT,
FIND-STRING, TEST-DATE-YYYYMMDD, TEST-DAY-YYYYDDD, TEST-NUMVAL, TEST-NUMVAL-C, NUMVAL-F,
COMBINED-DATETIME, BOOLEAN-OF-INTEGER, INTEGER-OF-BOOLEAN, FORMATTED-CURRENT-DATE/DATE/TIME,
HIGHEST/LOWEST-ALGEBRAIC, MODULE-NAME. Stubs: LOCALE-*, STANDARD-COMPARE, DISPLAY-OF,
NATIONAL-OF, CHAR-NATIONAL, CONVERT, BASECONVERT, EXCEPTION-*.

Test coverage audit: Added 188 unit tests ensuring every dispatched function has at least one test.
Then added 18 COBOL-level integration tests exercising functions end-to-end from compiled programs.

**Known limitation:** String literal arguments in FUNCTION calls don't work (SUBSCRIPT mode has
no string literal token). Field arguments and numeric literals work. SIGN, SUM, RANDOM are
reserved tokens that conflict with FUNCTION name parsing.

**Results:** 405 unit + 260 integration + 60 NIST guard = ALL GREEN.

---

