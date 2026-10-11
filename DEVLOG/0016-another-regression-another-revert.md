## Entry 016 — 2026-03-14: Another Regression, Another Revert

### The Pattern

Applied two changes (IsEndProgram + parenthesized conditions) without verifying each
independently. NIST pass rate dropped from 79 to 29. Reverted parenthesized change only,
still 29. Reverted both to get back to 79 baseline.

### Root Cause of the Regression

`IsEndProgram()` was added to every loop in the parser (SkipToPeriodOrKeyword, procedure
division loops, paragraph loops, sentence parsing). It checks for `EndKeyword` followed
by identifier "PROGRAM". But `EndKeyword` (`END`) appears in many COBOL contexts
(END-IF, END-PERFORM, etc. are separate keywords, but standalone `END` is used in
`AT END` clauses). The check was too aggressive and caused the parser to prematurely
exit loops.

### Repeated Failure Pattern

This is the same mistake documented in entries 009, 011, 013:
- Making changes based on guessing instead of reading the spec grammar
- Not testing each change independently
- Not comparing the implementation to the grammar production rules

### What Should Be Done Instead

The parser should be a 1:1 mapping of the grammar in GRAMMAR-REFERENCE.md:
- Each grammar production → one parse method
- Each alternative → one branch in the method
- No heuristics, no guesses, no "this looks right"

The grammar reference has the correct rules. The parser should implement them exactly.

---

