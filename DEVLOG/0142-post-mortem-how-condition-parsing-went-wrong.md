## Entry 142 — 2026-03-24: Post-Mortem — How Condition Parsing Went Wrong

This entry is a retrospective on *why* the condition grammar was incorrect despite starting from
"valid COBOL grammar." The refactor in Entry 141 fixed the damage, but the failure mode is worth
documenting because it's a pattern that will recur in any compiler built incrementally.

### What we started with

The original grammar came from the ANTLR grammars-v4 community Cobol85.g4. This is a
widely-referenced grammar, but it is **not spec-accurate** — it's a best-effort community
contribution that prioritizes parsing breadth over semantic correctness. Specifically:

1. **Recursive NOT**: The community grammar defines `NOT condition` recursively, allowing
   `NOT NOT NOT X`. COBOL-85 §6.3.4 defines NOT as applying to exactly one condition:
   `NOT simple-condition` or `NOT (conditional-expression)`. There is no recursive NOT in the spec.
   `NOT NOT X` without parentheses is not valid COBOL.

2. **No abbreviated conditions in grammar**: The community grammar doesn't model abbreviated
   combined relation conditions at all. It treats `IF A = B OR C` as `A = B` OR `C` (a bare
   identifier used as a boolean). The spec says `C` is an abbreviated operand that inherits
   the subject `A` and operator `=` from the preceding relation.

3. **Sign/class condition ordering**: The community grammar doesn't account for ANTLR's
   first-match semantics when sign conditions (`IS POSITIVE`) and comparison expressions
   share lexical prefixes. `SIGN-1 POSITIVE` parses as identifier `SIGN-1` followed by
   orphaned `POSITIVE`, not as a sign condition.

### How the errors accumulated

The grammar wasn't wrong on day one — it worked fine for simple conditions like `IF A = B` and
`IF A > B AND C < D`. Problems appeared only when NIST tests exercised the full condition
grammar: abbreviated chains, NOT with parenthesized operands, mixed sign/class/condition-name
expressions in compound conditions.

Each problem was patched incrementally:
- **Abbreviated conditions**: Added `abbreviatedRelation` grammar rule and
  `RewriteAbbreviatedRelations` post-binding pass. This worked for `IF A = B OR = C` (explicit
  operator abbreviation) but not for `IF A = B OR C` (bare operand abbreviation).
- **Bare operand expansion**: Added special-case checks in the rewrite pass for right operands,
  then left operands. Each patch fixed one NIST test but introduced fragility.
- **NOT interaction**: The recursive NOT consumed parenthesized arithmetic operands as negated
  conditions, breaking `NOT (expr) EQUAL TO operand`. This was invisible until NC211A.

The result was a condition pipeline with **three layers of patches** on top of an **incorrect
foundation**: a grammar that didn't model COBOL conditions correctly, a binding pass that
compensated with heuristics, and a rewrite pass that special-cased edge cases.

### The fix

The refactor replaced all three layers:
1. **Grammar**: NOT made non-recursive (one rule change). signCondition reordered before
   comparisonExpression (one reorder). Two lines of grammar change.
2. **Binding**: Extracted `BindPrimaryCondition` to match the new grammar shape cleanly.
3. **Rewrite**: Replaced `RewriteAbbreviatedRelations` (80+ lines, 4 helper methods, multiple
   special cases) with `ExpandAbbreviatedConditions` (60 lines, 1 helper, zero special cases).
   The new expander has ONE expansion point for bare operands, explicit exclusion of simple
   conditions, and spec-correct NOT handling.

### Lessons

1. **Community grammars are starting points, not specs.** The grammars-v4 Cobol85.g4 is
   useful for getting a parser off the ground, but it encodes assumptions that diverge from
   ISO 1989. Every rule that touches conditions, abbreviated forms, or NOT needed to be
   validated against the actual spec text.

2. **Incremental patching hides architectural debt.** Each abbreviated-condition patch fixed
   a NIST test, so the pipeline appeared to be converging. But the patches were compensating
   for a grammar that couldn't represent the spec's condition model. The right move was to
   fix the grammar first, not patch the binding layer.

3. **NOT is deceptively simple.** In most languages, NOT is a simple prefix operator.
   In COBOL, NOT has THREE meanings depending on context: logical negation (`NOT condition`),
   operator modifier (`NOT EQUAL`, `NOT GREATER`), and abbreviated negation
   (`A = B AND NOT C` → `NOT (A = C)`). A grammar that treats NOT as a single recursive
   prefix operator gets all three wrong in edge cases.

4. **Test against the hardest cases first.** NC211A's GF-48 test (the "monster compound")
   combines all condition types in one IF statement. If we'd tried to compile GF-48 earlier,
   the grammar issues would have surfaced before 140 entries of incremental patches.

---

