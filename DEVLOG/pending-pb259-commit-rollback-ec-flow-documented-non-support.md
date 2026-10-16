## PB259: COMMIT/ROLLBACK GR2 (EC-FLOW-COMMIT, EC-FLOW-ROLLBACK) recorded as documented non-support, with goldens for the dynamic reach

kb/Work PB259 was an adjudication. Two inventory rows sat at PARTIAL only so that a reader would still see them:
`GR-14.9.7.4-2` (COMMIT) and `GR-14.9.36.4-2` (ROLLBACK). Each statement's GR2 sets EC-FLOW-COMMIT or EC-FLOW-ROLLBACK
when the statement runs under the control of a recursive runtime element or an active file SORT or MERGE. The w1033
adjudication, upheld by its refuter, decided both rows are DOCUMENTED-NON-SUPPORT. This landing records that decision.
The ACCEPT row the note once held, `FMT-14.9.1.2`, had already closed CONFORMS through PB2499.

**Re-probe (wave 1095, base `60900b5cc`).** The adjudication's repros `CMS2.cob`, `RBS2.cob` and `CMR2.cob` behave as it
recorded. Each COMMIT or ROLLBACK site draws one COBOLNET1579 warning. The run prints `HELPER` /
`AFTER-COMMIT ST=[<31 spaces>]` / `SORTED`, and the ROLLBACK and recursive twins print the same way. No condition is
raised, even under `>>TURN EC-ALL CHECKING ON`. The refuter's `C1POS.cob` draws exactly 19 COBOLNET1579 warnings. A
sibling probe put COMMIT and ROLLBACK in a program contained in a RECURSIVE program; the RECURSIVE clause makes "the
program and any programs contained within it" recursive. Both statements are rejected with COBOLNET1690, so the static
reach has no gap. The compiler's behaviour does not change in this landing.

**Rule.** §14.9.7.4 GR2 and §14.9.36.4 GR2 set the conditions, and §9.1.18.7 states the same pair for the Commit and
Rollback facility. Annex A.4.3 makes that facility optional: item 1 is the facility, items 4 and 5 are the two
statements. `docs/CONFORMANCE.md` §5 records the facility as Not claimed. A.4.1 makes a module's "exception conditions"
optional along with the module. §14.6.13.1.1 does not require a level-3 condition of an unimplemented element to be
raised. Nothing else in GR2 survives the decline. The static reach is already rejected at bind time by §14.9.7.3 and
§14.9.36.3 SR1/SR2 (COBOLNET1690). The dynamic reach is GR1's CONTINUE.

**Shape.**
- The `commit-and-rollback-only` derived selector in `tests/version-matrix/inventory-schema.json` loses its
  `EC-FLOW-(COMMIT|ROLLBACK)` exclusion. Its $selector prose now explains why the two rows are in scope, and it no
  longer calls a general format "PB259's open question". The selector reaches 29 rows, all at DOCUMENTED-NON-SUPPORT.
- `DerivedVerdictDriftTests.TheCommitRollbackSelector_IsStillSharp` now asserts that both rows are selected.
- The summary of `StatementBinder.BindCommitRollback` cites GR2's dynamic reach and why it is unraised.
- Two goldens pin the dynamic reach under EC-ALL checking:
  - `conformance:2023/pb259_commit_rollback_under_sort` (PB259SRT): a paragraph outside both SORT procedures is
    PERFORMed from each of them, and the records still come back in key order.
  - `conformance:2023/pb259_commit_rollback_recursive` (PB259REC RECURSIVE): it calls the separately compiled PB259RSB.
- No negative was added below 2023. There, `COMMIT.` is a legal paragraph header, and the edition gate is already
  pinned by `user-word-commit` and the `user-word-commit-2023` construct.

**Rows.** Batch `w1095d-PB259` moved both rows from PARTIAL to DOCUMENTED-NON-SUPPORT, taking GAP from 91 to 89. PB259
is now landed with `closes_rows: [GR-14.9.7.4-2, GR-14.9.36.4-2]`. The note's bookmark sweep found no other row held
below its earned verdict for visibility: the three inventory notes that say "stays PARTIAL" each name a real residue.

**Branch review, round 1.** The reviewer found two stale texts this change left behind. The drift test's comment above
`GR-12.4.6.3.4-6` still described the anchored EC-FLOW exclusion the change deleted; it now says what the assertion
guards (the clause's own EC-FLOW-APPLY-COMMIT rule stays in, and an exclusion keyed on the EC-FLOW names must not
come back). `docs/CONFORMANCE.md`'s A.4.3 row quoted the selector as 26 rows (27 on main, 29 here) and did not name
the two GR2 rows. The sweep found the same rot in two of the four other rows of that table (screen handling said
178 rows against 181, FORMAT/SELECT WHEN 37 against 36; REWRITE/WRITE FILE's 12 and VALIDATE's 81 still matched). The table now carries no row counts: §5's preamble says the
selector is the set and `derive_verdict_batch.py --list` prints its count. The A.4.3 row now names both GR2 rows and
why they are declined.
