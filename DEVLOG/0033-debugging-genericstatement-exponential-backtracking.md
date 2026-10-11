## Entry 033 — 2026-03-14: Debugging — genericStatement Exponential Backtracking

**Failure:** Apparent parser hang on NC101A. Initially diagnosed as INSPECT grammar
causing exponential backtracking (IDENTIFIER-first alternatives).

**Real cause:** File lock from a previously killed process. Not a grammar issue.
However, the INSPECT grammar WAS problematic (IDENTIFIER as first token in phrase
alternatives violates LL(1)). User provided LL(1)-safe INSPECT with keyword-discriminated
alternatives (ALL/LEADING/FIRST/BEFORE as discriminators).

genericStatement catch-all also removed as a backtracking risk.

---

