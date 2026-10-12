## Entry 132 — 2026-03-21: Grammar Batch — OR EQUAL TO, INITIALIZE ALPHABETIC, EVALUATE Class+FALSE+NOT WHEN

Batch of grammar fixes from user-provided unified patch plus incremental debugging:

1. **GREATER THAN OR EQUAL TO** in comparisonOperator — NC201A's `IF X GREATER THAN OR
   EQUAL TO Y` no longer misparsed with OR as boolean. Added all 4 combined forms
   (GREATER/LESS × positive/negative) before the plain GREATER/LESS alternatives.

2. **INITIALIZE REPLACING ALPHABETIC DATA BY** — NC223A uses `REPLACING ALPHABETIC DATA BY`.
   Added ALPHABETIC to initializeReplacingItem. Also made DATA optional (`DATA?`) since
   NC223A also uses `REPLACING ALPHANUMERIC BY` (no DATA). Added `ALPHANUMERIC-EDITED`
   and `NUMERIC-EDITED` as lexer tokens for the hyphenated forms.

3. **EVALUATE subject class conditions** — `evaluateSubject: arithmeticExpression (IS? NOT?
   classCondition)?` allows `EVALUATE WRK-FIELD NUMERIC`. Used semantic design: added
   `TRUE_ | FALSE_` to the `condition` rule itself (not just evaluateWhenItem), so boolean
   literals are conditions everywhere.

4. **EVALUATE FALSE** — Added `FALSE_` to evaluateSubject alongside `TRUE_`.

5. **WHEN NOT** — `evaluateWhenGroup: NOT? evaluateWhenItem+` for negated WHEN ranges.

NC223A now compiles (52/94 — INITIALIZE semantics issues remain). NC225A down to 5 errors
(EVALUATE WHEN THROUGH prediction issue — ANTLR choosing condition over range).

---

