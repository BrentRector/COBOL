## Entry 507 — COBOL.NET: abbreviated combined relation conditions (ISO §8.8.4.12); spec-anchored tests

With the level-88 fixes (Entry 506) NC211A compiled and ran but stopped at its first ABBREVIATED test
(`CC--TEST-GF-11`) on the runtime loud guard `unsupported condition form`. The blocker is **abbreviated combined
relation conditions** (ISO §8.8.4.12) — `IF A > B OR < C` (subject omitted), `IF A = B AND C` (subject AND operator
omitted), `A NOT < C` (NOT as part of the carried operator). The grammar already parses the abbreviated forms
(`abbreviatedAndChain` / `abbreviatedRelation`); only the binder fell through to the loud guard.

**Implemented FROM the spec.** §8.8.4.12.4 GR1: "the last preceding stated subject … and the last stated relational
operator" are inserted for an omitted subject/operator, and the insertion terminates at a complete simple condition.
`StatementBinder`'s condition binding now threads an `AbbrevCarry { Subject, Op }` left-to-right in SOURCE ORDER through
the OR / XOR / AND / abbreviated-AND sequences (`BindFlatSequence`): a fully-stated relation sets both; an abbreviated
relation (`comparisonOperator comparisonOperand`) carries the subject and updates the operator; a bare operand that is
NOT a level-88 condition-name carries both (condition-name takes precedence, per the standard disambiguation). A
class / sign / condition-name / parenthesized condition is a complete simple condition that RESETS the carry (GR1); a
parenthesized sub-condition binds in a FRESH scope. A leading logical NOT (`NOT A = B`) is the unary negation of
relation-condition-1 and does NOT become the carried operator, while `NOT <` (an extended relational operator) DOES —
matching the §8.8.4.12.4 NOTE's worked examples. Each statement's condition starts from a fresh carry (the public
`BindCondition` entry), so nothing leaks across IF/PERFORM-UNTIL statements. The design (`COBOLNET_DESIGN.md` §11.4)
already named the approach "expand subject+operator"; this realizes it.

NC211A now runs through GF-11…GF-22 (ABBREVIATED / ABBREV. W/PARENS / NESTED IF all pass); it next stops on an
unrelated, explicitly-deferred feature — `MOVE` to a mixed-usage group with a COMP/binary leaf (the Tier-C byte path,
`IF-TABLE`) at GF-23 — so abbreviated conditions are complete but do not yet green NC211A.

**Tests — SPEC-ANCHORED (not oracle-anchored).** Per the owner's #1 rule (the ISO spec controls ALL behavior; verify
against the SPEC, not the legacy/test cases), `AbbreviatedConditionDifferentialTests` asserts, for 15 forms, the
result HAND-EXPANDED from §8.8.4.12.4 GR1 (e.g. `A > B AND NOT < C OR D` ≡ `((A>B) AND (A NOT< C)) OR (A NOT< D)` → Y),
then cross-checks the legacy oracle agrees — so a legacy non-conformance on any form would FAIL the test rather than
be enshrined. Conformance 248 → 263; 14 unit; the 7 green NC golden programs unaffected. Greenfield-only.

