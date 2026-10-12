## Entry 141 — 2026-03-24: Condition Grammar Refactor — Spec-Correct Abbreviated Expansion

**Production-quality refactor of the condition binding pipeline.**

Three changes, each addressing a specific spec violation:

**1. Grammar: NOT made non-recursive** (`NOT primaryCondition` instead of `NOT unaryLogicalExpression`).
COBOL-85 §6.3.4 says NOT applies to ONE condition. The recursive form greedily consumed
`(THREE-SEVENTHS)` in `NOT (THREE-SEVENTHS) EQUAL TO FIVE`, leaving `EQUAL TO FIVE` orphaned.
Non-recursive NOT lets `primaryCondition` match the entire comparison.

**2. Grammar: signCondition reordered first** in `primaryCondition`. ANTLR picks first match;
`SIGN-1 POSITIVE` was being consumed by `comparisonExpression` as bare `SIGN-1` with `POSITIVE`
orphaned. Moving `signCondition` first gives the more specific rule priority.

**3. RewriteAbbreviatedRelations replaced with ExpandAbbreviatedConditions.** Clean spec-correct
expander with explicit handling:
- Simple conditions (condition-name, class, sign, switch) excluded at top — never expanded
- Bare operands expanded in ONE place using inherited (subject, operator) context
- NOT handling: expand inner first, then wrap — correct for `NOT (A = B) AND C`
- Context extraction looks through NOT to find inner relation
- No special left/right bare-operand hacks

**Result: NC211A compiles (was 2 errors) and passes 47 of 51 tests.** One condition failure
(GF-48 monster compound with sign+class+switch+abbreviated in one IF), two figurative
constant failures (ALL literal), one other. Zero regressions across 217 unit + 184
integration + 32 NIST guard tests.

---

