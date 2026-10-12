## Entry 095 — 2026-03-16: ACCEPT FROM DATE/TIME/DAY/DAY-OF-WEEK

Implemented ACCEPT with intrinsic date/time sources.

**AI misstep — caught by user**: Initially tried to keep DATE/TIME/DAY as identifiers in the
grammar and resolve them via string comparisons in the binder (`FROM identifier` → check if
identifier text equals "DATE"). This is the exact kind of half-measure the user has repeatedly
flagged: when the spec defines keywords, use proper lexer tokens. The correct approach is
DATE, TIME, DAY as lexer keywords and DAY-OF-WEEK as a hyphenated compound token, with
a typed `acceptSource` parser rule that references these tokens directly. No string comparisons,
no ambiguity, no silent failures if someone misspells "DATEE". The grammar enforces correctness
at parse time, which is the whole point of having a grammar.

**Lesson reinforced**: The feedback_proper_fixes memory says "always add lexer tokens, never
IDENTIFIER workarounds." I had the memory, read it at session start, and still reached for the
lazy approach. The pattern to break: when implementing a new feature, the FIRST thing to check
is whether new lexer tokens are needed, before writing any binding code.

**Runtime**: `AcceptRuntime.Accept(byte[] area, int offset, int length, int sourceKind)` — one
method with a switch on source kind. Formats: DATE → YYYYMMDD or YYMMDD (based on field length),
TIME → HHMMSScc, DAY → YYYYDDD, DAY-OF-WEEK → 1-7 (ISO 8601: Monday=1). Writes ASCII digits
directly into storage, pads with spaces.

**Lexer tokens added**: DATE, TIME (regular keywords), DAY (regular keyword), DAY_OF_WEEK
(hyphenated compound token, placed before IDENTIFIER in lexer ordering).

5 tests: DATE 8-digit, DATE 6-digit, TIME, DAY, DAY-OF-WEEK — all assert shape invariants
(digit count, range checks) rather than exact clock values.

---

