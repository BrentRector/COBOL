## Entry 1185 — 2026-08-05 22:18 PDT — ISO Table 15 is now a TABLE re-derived from the spec, an invalid EVALUATE pairing is a compile-time diagnostic, and the first version of the screen rejected legal source within a minute

**PB47**, the last of the three the owner named. `EVALUATE TRUE / WHEN W-Z` — an identifier object against a
TRUE subject — compiled CLEAN and threw `NotImplementedCobolFeatureException` when that WHEN was reached, and
only if it was reached. **§14.9.13.3 SR10 + Table 15 make it a SYNTAX RULE violation**, so it belongs at compile
time. The old message was wrong twice over: the feature is not "not yet implemented", and the source is
inadmissible. It was also coverage-shaped — an invalid pairing on a branch that never executed never reported.

**⛔ TABLE 15 IS A TABLE, AND IT IS RE-DERIVED RATHER THAN TRANSCRIBED.** `EvaluateOperandCombinations` holds the
9×6 matrix in the standard's own row and column order, and `EvaluateOperandCombinationsDriftTests` parses Table 15
out of `specs/ISO_COBOL.md` and compares **all 54 cells**, asserting the population so a broken scrape cannot pass
vacuously. A chain of `if`s over 54 cells is the hand-maintained list CLAUDE.md rule 5 forbids, and — worse — a
table nothing re-derives has never been contradicted. That is not a hypothetical here: PB1's `ArgKinds` column was
not merely unread but UNVERIFIED, and enforcing it as written rejected twelve legal corpus programs.

**⛔ AND THE FIRST VERSION OF THE SCREEN DID EXACTLY THAT — FOR ABOUT A MINUTE.** Classifying a BOOLEAN operand as
a condition (which `ConditionBinder.BareOperandAsCondition`'s §8.8.4.3 arm reports) rejected the legal
`EVALUATE BW WHEN B"01"` over a `PIC 1(2)` item, because identifier × condition is a blank cell. The wave-local
gate caught it immediately. The rule that makes it legal is **§14.9.13.3 SR6**, and it is the same
subject-dependence PB45 kept walking into:

> a) If the selection subject is TRUE or FALSE and the selection object is a boolean expression that results in
> one boolean character, the selection object is treated as a boolean condition and therefore **condition-2**.
> c) If the selection subject is other than TRUE or FALSE … it is treated as a boolean expression and therefore
> **boolean-expression-2**.

Two different Table 15 rows, chosen by the SUBJECT *and* by a "results in one boolean character" length test that
is not implemented. So a boolean operand on either side is now DECLINED outright, with SR6 cited at the
`return null`. **The screen declines rather than guesses on both sides**, and a null on either means no
diagnostic: it narrows what already failed and widens nothing. Over-rejection would be a worse defect than the
wrong stage it closes.

**What it DOES enforce, and the evidence for it.** Where both operands classify with certainty the blank cells are
now errors — including one that is easy to assume is permissible: **literal × literal is blank**, so
`EVALUATE "A" WHEN "B"` is invalid per SR10. §14.9.13.4 GR1 is what makes that classification safe ("If an operand
of the EVALUATE statement consists of a single literal, that operand is treated as a literal, not as an
expression"), and the comprehensive battery — NIST 353, the full corpus, the GnuCOBOL differential — is what says
no legal program relied on it. A new rejection path is exactly the change a filtered gate cannot clear.

**⚠ RECORDED SO THE GAP IS NOT MISTAKEN FOR COVERAGE** (`feedback_green_test_can_hold_a_gap_open`): Table 15's
**boolean-expression** and **partial-expression** rows are carried by the matrix but are NOT reachable today —
the grammar stages both (the DEVLOG-621 `comparisonExpression` note), so nothing classifies into them. The table
is right; the classifier's coverage is partial by construction, and the drift test keeps the table honest for when
the grammar catches up.

**New diagnostic COBOLNET1634** (`evaluate-operand-combination-invalid`), negative fixture
`pb47-evaluate-table15-invalid-combination`. ⚙ `DiagnosticRegistryDriftTests` failed the moment the descriptor
landed, because `docs/DIAGNOSTICS.md` was out of sync — the generator regenerated it and the gate went green. That
is the doc-sync rule enforced by a test rather than by discipline, and it worked.

**⭐ THE EXTERNAL ORACLE INDEPENDENTLY CORROBORATED THE NEW REJECTION, WHICH IS THE EVIDENCE THAT MATTERS.** The
comprehensive battery came back with the differential's verdict line reading `1 PER-CASE FLIP(S)` — and the flip
is `syn_misc:3506`, **`WE_ACCEPT_THEY_REJECT -> AGREE_REJECT`**, carrying COBOLNET1634's own text ("the selection
subject is TRUE or FALSE and the selection object 'a-variable' …"). Plan §0's rule reads it exactly: a
divergence→AGREE flip is a FIX, an AGREE→divergence flip is a REGRESSION. So a program we had been WRONGLY
ACCEPTING is now rejected, and GnuCOBOL had been rejecting it all along. A new rejection path is the change most
likely to turn away legal source, and the one case in 1,323 that moved moved TOWARD the other implementation.
That is worth more than the wave-local green, and it is precisely why a new rejection cannot land on a filtered
gate.

The baseline was regenerated with `--write-baseline` in this commit, with the single changed row attributed above
(the diff is one line), and a fresh differential run against it prints `0 PER-CASE FLIP(S)`. Every other leg was
green on this same tree: Conformance 4209/4209, Unit 3689/3689, characterization 33/33, `guard-fast` ALL GREEN
with NIST 353 MATCH / 0 REGRESSION and the audit CLEAN. ⚠ The battery's own verdict line said NOT GREEN, because
its differential gate requires zero flips against the COMMITTED baseline — which is the gate working as designed:
it refuses to let a verdict move without someone naming why.

