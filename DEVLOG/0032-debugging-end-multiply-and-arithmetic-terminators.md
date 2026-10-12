## Entry 032 — 2026-03-14: Debugging — END-MULTIPLY and Arithmetic Terminators

**Failure:** `MULTIPLY ... ON SIZE ERROR ... END-MULTIPLY` — parser doesn't recognize
END-MULTIPLY as a scope terminator.

**Root cause:** All arithmetic statement rules (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE)
were missing their `END_xxx?` tokens. Also `ifStatement` was missing `DOT?`.

**Fix:** Added END_ADD?, END_SUBTRACT?, END_MULTIPLY?, END_DIVIDE?, END_COMPUTE? to all
arithmetic statement rules. Added DOT? to ifStatement.

---

