## Entry 1196 — 2026-08-07 04:35 PDT — PB35: one rule, six §15 clauses, one implementation — and the drift test caught a seventh spelling my own scan had missed

**`FUNCTION MAX("" "AB")` compiled clean at every edition.** So did MIN, ORD-MAX and ORD-MIN. The entry named
MAX/MIN and observed that "the screen would have to test LENGTH, not class" — both right. The COUNT was wrong:

| clause | function | rule | reach | before |
|---|---|---|---|---|
| §15.59.3 | MAX | **r3** | every argument (variadic argument-1) | ⛔ silent |
| §15.63.3 | MIN | **r3** | every argument | ⛔ silent |
| §15.71.3 | ORD-MAX | **r2** | every argument | ⛔ silent |
| §15.72.3 | ORD-MIN | **r2** | every argument | ⛔ silent |
| §15.66.3 | NATIONAL-OF | **r3** | argument-1 of two | ✅ enforced |
| §15.85.3 | STANDARD-COMPARE | **r4** | argument-1 AND argument-2 | n/a — Bind = Unsupported |

⭐ **THE ONE THAT WORKED IS WHY THE OTHERS DID NOT.** NATIONAL-OF's check is hand-written inside
`CheckRepertoireArgs`, a per-FUNCTION checker — so nothing about it generalized, and five siblings sharing the
identical rule each waited for someone to write it a sixth time (`feedback_one_rule_one_place`). The rule now
rides the argument SCHEMA (`ArgRule.NoZeroLengthClause`), declared beside each position's class rule and enforced
in the ONE shared per-position loop, so the seventh clause costs a table row.
⚠ Read off the general format, not the prose: `FUNCTION MAX ( { argument-1 } … )` makes "argument-1" the WHOLE
variadic list, so the prohibition covers every argument and lands on the schema's `Tail` — not on position 0.

⛔ **IT CARRIES A CLAUSE STRING RATHER THAN A `bool`, AND THAT IS THE LOAD-BEARING DETAIL.** The zero-length rule
is a DIFFERENT ORDINAL from the position's class rule in all six: MAX's class rule is §15.59.3 **r1**, its
zero-length rule is **r3**. A boolean would have made the diagnostic cite r1 for an r3 violation — a citation
that passes `cite.py --check`, because the text really is in the standard, while pointing the reader at the wrong
rule. CLAUDE.md rule 1's exact failure mode, and it is invisible unless you compare ORDINALS rather than clause
numbers (`feedback_a_real_clause_can_answer_a_different_question`).

⚙ **THE DRIFT TEST EARNED ITS KEEP ON ITS FIRST RUN, AGAINST ME.** `ZeroLengthLiteralArgumentDriftTests` derives
the function list from `specs/ISO_COBOL.md` rather than remembering it. My first pattern matched
`shall not be a zero-length literal` and returned FIVE of six — **§15.85.3 r4 states the identical rule as
"NEITHER argument-1 NOR argument-2 shall be a zero-length literal"**. What caught it was not the check but the
POPULATION assertion beside it (`found.Count >= 6`): a drift test whose own pattern has a blind spot reports
coverage it does not have, which is worse than no test at all (`feedback_verdict_evidence_invariant`).
⚠ **The subclause scoping is load-bearing in the other direction too.** §15.93/§15.94/§15.95 mention
"argument-1 is zero-length" in their RETURNED-VALUE rules — an error the function REPORTS, a value it computes,
the opposite of a prohibition. Matching the phrase anywhere in clause 15 would have demanded a screen rejecting
source the standard defines behaviour for. A second test asserts the TEST-NUMVAL family stays out.

⚠ **THE TEST IS ON THE LITERAL, NEVER ON A WIDTH** — each clause says "zero-length LITERAL" in those words. A
screen on the operand's measured width would reject items and figuratives, and PB1 is the standing proof of that
direction's cost. `pb35_zero_length_literal_legal_forms` pins seven shapes that must keep compiling, including
`FUNCTION MAX(SPACE "A")`, item operands and a numeric argument list; `--permissive` degrades to a warning per
the existing policy (verified).

**TWO ADJUDICATED OUT, each with its reason IN THE TEST rather than in a comment.** NATIONAL-OF stays in
`CheckRepertoireArgs` because its NARROWER class rule lives there — §15.66.3 r1 admits alphabetic or
alphanumeric but NOT national, which no single ArgKind code expresses ('s' spans national), and splitting one
function's rules across two mechanisms to unify one rule across six is the worse trade. STANDARD-COMPARE is
§A.4.9 documented non-support, so 1518 fires before any argument screen and a row would be a dead lookup — the
same disposition PB27 reached for the locale rows one entry earlier.

**GATE.** Conformance `~Corpus|~Negative|~Intrinsic|~VersionMatrix` **2642/2642** zero skipped · Unit
**4027/4027** zero skipped (4023 + 4 drift assertions). Golden `pb35_zero_length_literal_legal_forms` ·
negative `pb35-zero-length-literal-max-argument`.

