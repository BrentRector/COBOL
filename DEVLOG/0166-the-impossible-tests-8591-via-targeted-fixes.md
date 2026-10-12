## Entry 166 — 2026-03-29: The "Impossible" Tests — 85→91 via Targeted Fixes

Six more tests fell to targeted fixes, each exposing a different compiler gap:

1. **NC216A** (INSPECT multi-pattern): `inspectCountPhrase` and `inspectReplacingItem` made
   `ALL/LEADING/FIRST/TRAILING` optional so subsequent patterns inherit the keyword from the
   first in a FOR clause. Also relaxed INSPECT target validation to allow numeric items.

2. **NC225A** (EVALUATE class conditions): `LowerCondition` didn't handle bare
   `BoundIdentifierExpression` or `BoundLiteralExpression` in condition context — these appear
   from EVALUATE TRUE/FALSE with class condition subjects. Added truth-test fallbacks.

3. **NC125A** (PIC trailing period): `PIC 999999999999..` — the lexer's `PIC_STRING` rule
   greedily consumed both periods. Added a post-match action: if PIC string ends with `.`,
   trim it and back up `InputStream` by 1 so the sentence-ending `.` tokenizes as DOT.
   This was the "lexer-level fix is impossible" one — turned out to be 8 lines.

4. **NC205A** (preprocessor continuation): non-literal continuation handler wasn't stripping
   trailing spaces from the previous line before appending continuation content. `PIC S9(`
   + continuation `6)V9(6)` became `PIC S9(                        6)V9(6)` with 24 spaces.

5. **NC211A** (VALUE THRU negative numbers): `VALUE IS 5 -9999 THRU 10` — the parser saw
   `5 - 9999` as subtraction. Split `valueOperand` into two: `valueClauseOperand` uses
   `unaryExpression` (no binary arithmetic) for VALUE clauses; `valueOperand` keeps full
   `arithmeticExpression` for comparisons and EVALUATE WHEN.

6. **NC303M/NC401M** (flagging tests): just compiled and ran — no assertions, only diagnostics.
   Guard captures stdout for comparison.

---

