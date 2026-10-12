## Entry 171 — 2026-03-30: M300 Grammar Gap Verification — 56→36 verified remaining

Deployed 4 parallel agents to verify GRAMMAR_AUDIT.md claims against actual grammar files.
Found the audit **undercounted fixes by ~20 items**: many Procedure Division and Lexer Token
gaps were already fixed in prior sessions but not reflected in the audit counts.

**Verified counts**: 36 remaining (was claimed 56). Data Division 15, Procedure Division 10,
Environment Division 11, Expressions 0 (exponentiation is correct), Lexer Tokens 0 (43/45
present; COPY/REPLACE correctly omitted as preprocessor-only).

**Categorized all gaps**: 12 REJECT (valid COBOL-85 rejected — P1), 15 OVERLENIENT (accepts
invalid COBOL — P3), 9 SEMANTIC-ONLY (grammar fine, validation missing — cross-linked to
M302/M304/M310).

**NIST impact mapping**: USE GLOBAL blocks 6 NC tests + entire IC suite (47 programs).
ALPHABET FOR blocks NC215A/NC219A + IX suite (42). Combined non-NC unlocks: SQ(85)+ST(40)+IF(45).

**Fix plan**: 3 batches. Batch 1 (quick wins, ~4h): USE GLOBAL/EXCEPTION/modes, STOP RUN STATUS,
START WITH LENGTH, ALPHABET FOR, CLASS FOR. Batch 2 (medium, ~8h): SET ON/OFF, SORT Format 2,
SYMBOLIC CHARACTERS N:N. Batch 3 (architectural, deferred): CURRENCY WITH PICTURE SYMBOL
(PICMODE blocked), SCREEN SECTION (entire new section).

Created 27 new ledger items (M400-M426): 12 REJECT gaps + 15 OVERLENIENT gaps.
Ledger patch produced as m300-analysis-patch.json — awaiting human review.

---

