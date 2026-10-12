## Entry 262 — PADDING CHARACTER + RECORD DELIMITER SELECT clauses (parse + ignore) → SQ216A/218A/219A

Two SELECT-clause parse forms blocked SQ compiles. Both are accepted and ignored (they have no effect on
CobolSharp's record model):
- **PADDING CHARACTER** (ISO §12.4.5.9): `PADDING [CHARACTER] IS {data-name | literal}` — an obsolete
  block-padding control. `PADDING` was not a lexer token (it lexed as IDENTIFIER, and `genericClause` —
  `IDENTIFIER (IDENTIFIER|literal)*` — stopped at the `CHARACTER` keyword → "unexpected CHARACTER"). Added
  the `PADDING` reserved-word token (it appears only in NOTE comments across the suite, never as a
  data-name) and `paddingCharacterClause : PADDING CHARACTER? IS? (literal | dataReference)`.
- **RECORD DELIMITER** (ISO §12.4.5.11): `RECORD DELIMITER IS {STANDARD-1 | feature-name}` — selects the
  variable-length record length-determination method. CobolSharp length-frames variable records itself
  (DEVLOG 261), so it is ignored. Added `recordDelimiterClause : RECORD DELIMITER IS? (STANDARD-1 |
  cobolWord)`; it and `recordKeyClause` both begin with RECORD but disambiguate on the second token.

Both new clauses are unhandled by `SemanticBuilder.VisitFileControlClauseGroup` (it dispatches by clause
type), so they are silently accepted. Guard ALL GREEN: 1000 unit / 347 integration / **219 NIST**
(94 NC + 42 IF + 12 SM + 4 IC + 46 SQ + 19 RL + 2 IX), 0 regressions. SQ216A (7/7, PADDING), SQ218A/SQ219A
(6/6, RECORD DELIMITER) baselined. (SQ401M still COMPILE_FAILs on a further non-conforming clause and is a
flagging module anyway.) LINAGE-COUNTER is next — it needs runtime page-mechanics, not just a parse fix.

