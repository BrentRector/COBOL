## Entry 1855 — 2026-10-03 17:51 PDT — Train 1010: wave 1010 groups B, A (the EVALUATE boolean-expression operands; SUM data-name-1 rolled totals and report-writer constants)

**Group B — PB1412, the EVALUATE half.** The pending 2023 golden drew COBOLNET1511 and COBOLNET1634 three times on
today's tree. The cause was in the grammar: `evaluateSubject` and `evaluateWhenItem` had no boolean-expression
alternative, so `EVALUATE A B-AND C WHEN B"1000"` parsed its subject as a condition and was held to §8.8.4.3.3 SR1's
length-1 rule. Both rules now carry `{boolExprAhead()}? booleanExpression` after `valueOperand` and before
`condition` (§14.9.13.2 boolean-expression-1/-2). `boolExprAhead` also stops at ALSO, so an operator in a later
subject no longer reaches an earlier one. On the binder side, `ConditionBinder.AnalyzeBooleanExpression` is the one
analysis: the Boolean form plus the §8.8.2 rule 10 result length. Table 15 and SR6 a)-d) therefore read the same
facts they read for a boolean item. The subject is one bound operand, held through
`SendingValueTemp.MaterializeBoolean` when more than one arm reads it (§14.9.13.4 GR3 d)). The object is built through
the one relation checkpoint (GR4 a) 6.). The group's self-review fixed two more things in the same files. An invalid
Table 15 pair now binds nothing further, so `EVALUATE BW WHEN TRUE` over a wide item gets one diagnostic instead of
COBOLNET1634 followed by COBOLNET2318. A sign partial-expression over a boolean-expression subject is refused by name
under §8.8.4.7.3 SR1. Two new goldens and five negatives were added. Row GR-8.8.2-8 went from PARTIAL to CONFORMS,
and witnesses were added to FMT-14.9.13.2 and SR-14.9.13.3-6/-8/-10. GAP 504 → 503.

**Group A — PB1947 and PB1294.** PB1947: before this change, a constant-name at a report-writer integer-n was a
parse error (COBOL0309 for LINE, COLUMN, PAGE and NEXT GROUP; COBOL0307 for OCCURS STEP). §13.10.3 SR2 allows it. One
grammar rule, `integerOperand`, replaces the OCCURS-only `occursBound`, and every one of those integers now uses it.
One reader, `DataBinder.IntegerOperandValue`, substitutes the constant. It holds the value to §5.5 1)'s unsigned and
nonzero rule, using each clause's own zero permission from `IntegerOperandRules`, and to the host limit. A zero
constant in a nonzero slot is now COBOLNET2386. PB1294: a SUM clause's data-name-1 (a rolled total) failed on today's
tree. Over an unprintable SOURCE entry it drew COBOLNET0899, and over a printable one it hit a staged descriptor. The
root cause was that the entry carrying SUM/SOURCE/VALUE was never modelled as the unit that rolls. It is now
(`ReportEntryFamily`, `ReportSourceFamily`, `ReportItemOccurrence`). §13.18.54.3 SR4 a)-g) is screened once per
written addend, which introduces COBOLNET2730 and COBOLNET2731. The addition is registered on the group that holds the
addend and runs in that group's prologue (GR7 a)/b)), but not on an OR PAGE reprint. It reuses the closure a GENERATE
term builds and follows GR11 absence and GR6 dependency order. A cross-report UPON registers on the other report's
engine (GR7 c) 2)). One positive golden for PB1947, five for PB1294 (the 85 introducing edition and four at 2002),
and twelve negatives were added. PB1294's seven rows are now CONFORMS. PB1947 closes no row (its reason is recorded),
and SR-13.10.3-2 stays PARTIAL under PB780. GAP 503 → 497. The group's one lead, the non-report integer-n positions
(RESERVE, BLOCK CONTAINS, RECORD CONTAINS, DYNAMIC LENGTH LIMIT and their siblings), is filed as PB1948.

**The train.** Both clusters were cut from fb6a6747e, which was also origin/main when the train landed. Their patches
applied cleanly in order: B touched 26 files, A touched 64. Diffstats were identical to each implementer branch, and
no conflict markers were found in the tree or the index. Re-applying the three verdict batches in a dry run on the
merged tree was a no-op, and the inventory reads GAP 497. Lander gate (one leg, whole population, run
20261004T004459Z-b4cacd): `=== BUILD-LOCAL GATE: GREEN — Conformance 10,040/10,040 · Unit 30,417/30,417 ·
Characterization 35/35 cases ran (skipped 0)`. The legacy integration assembly passed 503, failed 0 and skipped 1. The
Linux gate was `GREEN (legs unit characterization conformance; HEAD 61067add2)`. semgrep verify reported PASS with no
count up. audit_code_citations, audit_doc_citations, audit_evidence_supersession and audit_witness_loss (0
unexcused, 2 retired, 9 re-sited) passed, along with drift_rules --check and work.py check. Five spot-checked
citations were all OK. The review pass over the train diff (grammar order, the ALSO boundary, the rolled-addition
order and the GR11 slot, a DECIMAL-POINT IS COMMA VALUE addend probe that summed correctly) found 0 defects and
dropped 0 clusters. Codes COBOLNET2730-2731 were claimed; 2732-2737 are returned unused. GAP 504 → 497.

