## Entry 1155 — 2026-08-03 15:46 PDT — §5b step 4 done: one grammar pass now feeds both ledgers, and it refuses to manufacture the coverage it cannot prove

With the audit runnable again (entry 1154), step 4 proper: make ONE pass emit both the grammar finding and the
inventory verdict, instead of auditing the same ~1,670 FMT/SR rules twice.

**THE SHAPE MISMATCH IS THE WHOLE PROBLEM.** A grammar finding is about a CONSTRUCT ("§14.9.1 ACCEPT, format 1");
an inventory row is about a RULE (`SR-14.9.1.3-4`). The cheap conversion — take a finding for §14.9.1 and stamp
it across all of that clause's FMT/SR rows — is available, obvious, and wrong: nineteen rules would acquire a
verdict from ONE observation, and the inventory would then read as adjudicated territory nobody examined. That is
manufacturing coverage, and it is the same currency this whole session has been about.

So the workflow's finding schema now REQUIRES `rule_ids` — the catalog ids the agent actually checked, taken from
its own STEP 1 listing — and the agent is told outright not to pad it: *"an un-adjudicated rule staying a GAP is
CORRECT — a wrong verdict is far more expensive than a missing one."*
`scripts/spec/grammar_findings_to_batch.py` then converts:

    MATCHES          -> CONFORMS          (code-location = the .g4 site)
    DIVERGES         -> DIVERGES          (notes carry the divergence kind and the exact ISO syntax)
    NOT-IMPLEMENTED  -> NOT-IMPLEMENTED
    UNCLEAR          -> NOTHING AT ALL

**UNCLEAR emits no record on purpose.** The workflow defines it as "no decisive repro — do NOT guess", which is
the ABSENCE of an adjudication, not a kind of one. Recording it as NEEDS-OWNER-DECISION would file an agent's
uncertainty as a question for the owner and quietly take the row out of the queue.

The converter also refuses what it cannot justify: a finding with no `rule_ids`, a rule-id not in the catalog, a
**GR** rule (a grammar pass has nothing to say about what the implementation DOES — that is the other vein), and
two findings disagreeing about one rule. Each is a reported PROBLEM that blocks the whole batch, never a silently
dropped row.

**PROVEN END TO END, on `--dry-run` only** (synthetic findings must never reach the inventory): findings → batch
→ `record_verdicts.py` accepted 3 records — 2 CONFORMS-but-untested, 1 DIVERGES — and closed **nothing**.
That last part is the design working: a grammar MATCHES records a verdict but the row stays open until a
SPEC-DERIVED test covers it. The unification moves the map, not the GAP; only tests move the GAP.

⚠ **AND MY OWN COVERAGE REPORT HAD THE SESSION'S SIGNATURE BUG.** `--coverage` names the FMT/SR rules a pass did
not settle, so a pass that adjudicated 3 of a clause's 19 rules cannot look like an audit of the clause. The
first draft counted any rule a finding NAMED as covered — including one it returned UNCLEAR for. A pass that
decided nothing would therefore have reported FULL coverage and zero records. Fixed, and pinned as its own
self-test case: only a verdict that can become a record counts as adjudication. Nine cases in `--self-test`.

