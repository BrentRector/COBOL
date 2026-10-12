## Entry 165 — 2026-03-28: SLL Two-Stage Parsing — 6× Speedup

Added ANTLR4 SLL prediction mode with LL fallback to `Compilation.cs`. Integration tests
dropped from 18s → 3s. Most COBOL files parse correctly with SLL's faster single-context
prediction; only ambiguous files fall back to full LL.

Added `BailErrorStrategy` for the SLL phase so ambiguous constructs throw immediately instead
of hanging in slow adaptive prediction. Without this, NC250A and other files with complex
VALUE THRU patterns would cause the SLL parser to spin.

The performance gain compounds: every guard run (95 NIST tests) benefits, and developer
iteration speed improves dramatically. The COBOL grammar has many optional keywords that
create prediction ambiguity — SLL handles these by picking the first viable alternative.

---

