## Entry 133 — 2026-03-21: Grammar Feature-Complete for COBOL-85

Major grammar restructure from user-provided unified patches:

**Condition/expression refactor**: Introduced `valueOperand`, `valueRange`, `booleanLiteral`,
`signCondition`, `primaryCondition` as distinct rules. `condition` no longer directly contains
TRUE_/FALSE_ — those are in `booleanLiteral` used by `primaryCondition`. Sign conditions
(IS POSITIVE/NEGATIVE/ZERO) are first-class. Parenthesized conditions supported.
`comparisonOperand` delegates to `valueOperand`. EVALUATE uses `valueRange` for WHEN ranges,
fixing the THROUGH prediction issue.

**New lexer tokens**: POSITIVE, NEGATIVE, RESERVE, SYMBOLIC, ALPHABET, CRT, CURSOR, CHANNEL,
PROCEED, USE, STANDARD, REPORTING, SUM, REPORT, RD, ALPHANUMERIC_EDITED, NUMERIC_EDITED, TEST.

**SPECIAL-NAMES expansion**: CLASS definition, SYMBOLIC CHARACTERS, ALPHABET, CRT STATUS,
CURSOR, CHANNEL, RESERVE clauses.

**REPORT SECTION**: RD entries, report group entries with TYPE/SUM/generic clauses.

**New statements**: ALTER (§14.9.2), USE (§14.9.45 — BEFORE REPORTING / AFTER ERROR).

**EVALUATE**: FALSE_ subject, NOT? WHEN groups, class conditions on subjects, GREATER THAN
OR EQUAL TO family in comparisonOperator.

**INITIALIZE**: ALPHABETIC DATA BY, DATA optional, hyphenated ALPHANUMERIC-EDITED/NUMERIC-EDITED.

---

