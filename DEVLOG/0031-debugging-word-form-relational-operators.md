## Entry 031 — 2026-03-14: Debugging — Word-Form Relational Operators

**Failure:** `IF REC-CT NOT EQUAL TO ZERO` — parser sees NOT as boolean negation,
not as part of relational operator.

**Root cause:** Grammar only had symbol operators (=, <>, <, >, <=, >=). COBOL also uses
word forms: EQUAL TO, NOT EQUAL TO, GREATER THAN, LESS THAN, with optional IS prefix.

**User provided canonical ISO 2023 relational operator set:**
```
IS? EQUAL (TO | THAN)?
IS? NOT EQUAL (TO | THAN)?
IS? GREATER THAN?
IS? NOT GREATER THAN?
IS? LESS THAN?
IS? NOT LESS THAN?
```
Also identified: `GREATER` without `THAN` is valid (COBOL allows bare GREATER).

---

