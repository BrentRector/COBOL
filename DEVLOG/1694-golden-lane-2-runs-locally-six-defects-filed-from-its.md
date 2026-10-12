## Entry 1694 — 2026-09-24 22:37 PDT — Golden lane #2 runs locally; six defects filed from its refuters (PB1549–PB1554)

Owner: "We should finish the remaining items the claude cloud didn't finish" — the cloud golden lane stopped at wave 4
with 24 input files (~229 rows) unstarted and ~35 rows overturned or withheld. The lane now runs LOCALLY:

- The cloud briefs are made path-agnostic instead of forked: `adjudication/golden-lane-1/scratch/WRITER.md` and
  `REFUTER.md` take `{REPO}` (a pinned, built worktree — `E:\wt\gl2-pin` at f0d582ccd, so no landing swaps the compiler
  under a writer) and `{GL}` (the lane directory); `integrate.py` reads `GL_REPO` / `GL_DIR`. Both briefs carry the P10
  public-skills pointer (spec-oracle, pr-test-analyzer).
- WRITER.md gains "Lessons from golden lane #2 refuters" — each one an overturn that recurred: a negative must be illegal
  ONLY under its rule; no leg may depend on an undecided implementor default (PB322 SHARING); a documented-choice golden
  must use inputs a different choice would print differently; a fatal EC with no handler ends the run unit; lowest
  edition always.
- Progress at this entry: 9 writers done (dns-witness, misc-p16…p22), 8 refuted. Upheld/overturned: p16 5/0, p17 10/2,
  p18 8/1, p19 9/2, p20 12/1 (two citation overturns fixed by the orchestrator in the draft header), p21 7/0, p22 7/0,
  dns-witness 1/3.

Defects filed from the refuters (each confirmed against the pinned source, code sites in the notes):
- **PB1549** (wrong answer) — a failed `SET … TO ADDRESS OF PROGRAM` with EC-PROGRAM-NOT-FOUND checking enabled and no
  handler does not terminate the run unit (`PtrEmitter.EmitSetEntry`); the CALL arm does — a two-arm defect.
- **PB1550** — the shared SORT/MERGE collating binder cites §14.9.40.3 in every MERGE COBOLNET0898 message.
- **PB1551** — NULL/SELF/SUPER as a receiving operand get COBOLNET0901 (reserved word as user word) instead of their own
  rule; two negatives (misc-p19, misc-p20) are HELD on it.
- **PB1552** — the COBOLNET0805 message cites "§14.7 rule 2", which cite.py fails (§14.7.7 2) a)).
- **PB1553** — 20 national- and boolean-literal inventory rows list edition 85; both kinds start in 2002.
- **PB1554** — cite.py labels a paragraph that follows a numbered list with the last item's number.

Also: `tests/conformance/2023/pb65_leap_second_on.cob` called the reported-side leap-second determination "A.1 item 112";
it is item 111 (specs/ISO_COBOL.md Annex A.1, confirmed by the misc-p16 refuter). CONFORMANCE.md was already right.

