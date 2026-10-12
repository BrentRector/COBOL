## Entry 622 — 2026-07-06 00:18 PDT — the boolean CONDITION/RELATION forms (DEVLOG-621 residue) — CLOSED via a semantic predicate

**The increment-2 residue is resolved: boolean expressions now work in CONDITIONS — the boolean relation
(§8.8.4.2.2), the simple boolean condition (§8.8.4.3), UNPARENTHESIZED (`IF a B-AND b`, `IF a B-AND b = c`) and
parenthesized alike — with ZERO regression (the 31 legacy integration tests that the increment-2 grammar broke
all pass).** The fix is the lesson from DEVLOG 621 applied: I did NOT touch the shared `comparisonExpression`
rule. Instead a new `primaryCondition` alternative is gated by a semantic predicate `boolExprAhead()`
(CobolParserCoreBase) that scans the token stream for a B-operator ahead of the current condition's boundary (a
period, the logical connectives AND/OR/THEN/ELSE, WHEN/END-*/UNTIL/VARYING, or any statement-starting keyword —
so it never crosses into an IF body). When no B-op is present the predicate returns false and the parser falls to
`comparisonExpression` UNCHANGED — so `IF ELEM(I) = x`, every SEARCH WHEN, and all ordinary comparisons at 2002+
bind exactly as before. When a B-op is present the boolean alt fires; the binder (`BindPrimaryBoolean`) unwraps a
B-op-free relation operand back to its normal binding (so `(a B-AND b) = c` compares the boolean expression
against the boolean literal `c` correctly) and produces the boolean channel otherwise.

Why this works where the `comparisonExpression` alt didn't: a predicate-gated alt is evaluated at parse time and
prunes cleanly — it never enters ANTLR's static DFA for the untouched comparison rule, so it cannot disturb the
subscript/ref-mod comparison prediction that the earlier structural change broke. Restored the binder condition
wiring (BindPrimaryBoolean + BindBoolOrValueOperand + HasBoolOp + UnwrapBareBool + BoolValued, all removed in the
621 revert) and re-enabled the `boolean_ops` golden's relation + condition + B-NOT lines (byte-exact:
REL-EQ=YES / REL-NE=NO / FG-OR-ON / FG-AND-OFF / NOT-G-ON). BooleanOperatorTests grew the relation + condition +
a NormalComparison_WithBooleanGrammarPresent regression-guard fact.

**Battery:** conformance **1812** · unit **187** · FULL legacy guard ALL GREEN (353 NIST MATCH, 0 regressions,
1204 unit, **556 integration — the 31 previously-broken tests all green**). The comparisonExpression + booleanExpression
residue in the reconciliation is now CLOSED; only the diagnostic-quality 85-boolean-COMPUTE-message and the
larger-catalog items (shift operators, BX literals, `&`-concat, intrinsic boolean args) remain.

