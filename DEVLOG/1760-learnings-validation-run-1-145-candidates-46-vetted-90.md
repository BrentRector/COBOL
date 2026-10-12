## Entry 1760 — 2026-09-28 14:55 PDT — Learnings validation run 1: 145 candidates → 46 VETTED, 90 UNPROVEN, 9 REFUTED; the incremental learnings ledger

The owner: only vetted, battle-tested learnings go into LEARNINGS.md and into skills; then "start the validation run".
Then: "We should persistently track what devlog entries we've processed into tangible vetted learnings and
incorporated into skills".

**The run** (workflow wf_cf692111-b7b; 31 agents, 0 errors, about 5.1 M subagent tokens, about 60 min):
- 16 read-only Opus validators over the 145 candidates from consolidation 1, batched by theme at 12 or fewer.
- Each validator tried to disprove its items against the primary sources: DEVLOG (a later reversing entry is
  decisive), the evidence records, kb/Work, MANDATORY-PRACTICES and the owner's recorded corrections.
- VETTED required all four criteria: root cause confirmed, evidence re-checked, not contradicted, and used since and
  shown useful. When uncertain, the verdict was UNPROVEN.
- An independent refuter then tried to overturn every VETTED verdict.

**Result: 46 VETTED, 90 UNPROVEN, 9 REFUTED.** The refuters overturned 21 of the validators' 67 VETTED verdicts.
- The strongest themes are gates and verification (14 vetted) and spec as oracle and citation discipline (8).
- Cost and transcript length vetted 0. The quadratic cost law is UNPROVEN: its mechanism holds for all tokens
  including cache reads, and DEVLOG 1635 (2026-09-22) already demoted it, because the weekly meter does not track
  that total. The candidate had restated it without noticing the demotion.
- Examples of REFUTED: "a continuation after a kill never re-applies" (DEVLOG 1556 and 1559 show re-application);
  "one mechanism per implementer" (superseded by the owner's 2026-09-13 group rule); "worktree-isolated agents cannot
  reach other worktrees" (contradicted by agent transcripts). Several refutations reject part of a claim, not all of
  it, and are to be reviewed before anything is republished.
- Most UNPROVEN verdicts name a before-and-after measurement of the practice's effect that was never made.

**The learnings ledger** (`docs/rearchitecture/evidence/fleet-optimization/learnings-ledger/`, append-only) makes the
next run incremental:
- `watermark.json`: consolidated through Entry 1755.
- `candidates.jsonl`: 145, with stable ids C1–C145; 140 carry devlog-entry provenance.
- `verdicts.jsonl`: validator and refuter verdicts, where the latest line for an id is its state.
- `encoded.jsonl`: which skill, section and version each vetted learning went into; not yet created.
- The ledger also keeps the run's inputs, workflow script, aggregation script and final results.
- The next run reads only entries above 1755, merges restatements, and re-checks vetted ids against the new entries
  only.

**Pending the owner:**
- publishing a vetted-only LEARNINGS.md;
- auditing the public skills against the vetted list, which decides what happens to rules backed only by UNPROVEN
  learnings;
- incorporating vetted findings that are not yet in the skills.
The DEVLOG LinkedIn post waits for these.

