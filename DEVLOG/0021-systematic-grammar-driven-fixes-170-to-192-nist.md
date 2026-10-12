## Entry 021 — 2026-03-14: Systematic Grammar-Driven Fixes — 170 to 192 NIST

Each fix now cites the grammar rule:
- §8.3.5 comma separators in ParseSentence: +21 programs
- §7.2 ADD GIVING format without TO: +9 programs
- §5.3.2 END PROGRAM as procedure division boundary: +6 programs
- §7.4 CLOSE WITH LOCK / WITH NO REWIND: +10 programs
- §7.19 PERFORM VARYING AFTER (nested varying): +8 programs
- EmitDecimalConstant crash fix: +16 programs (emitter, not parser)
- §8.8.4.9 Parenthesized conditions: +3 programs

Total: 78 → 95 → 139 → 170 → 186 → 192/391 (49.1%)

### Remaining error categories (199 programs):
- 17x COPY-related undefined names (preprocessor needs NIST copybooks)
- 14x continuation lines in preprocessor (string literals split across lines)
- 12x PROCEDURE keyword in unexpected context
- 11x section header parsing (SQ module section names)
- 5x expected expression 'BY' (CALL BY CONTENT not handled)
- Various: ALSO in EVALUATE, FUNCTION name parsing, MERGE, DISABLE

---

