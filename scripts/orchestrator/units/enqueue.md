UNIT: enqueue.

Finished branches are waiting to land. Landing is a SCRIPT, `python scripts/orchestrator/enqueue.py land <branch>`
(kb/Work PB3023; DESIGN-test-build-ci.md section 3.17.2 step 3): it merges origin/main into the branch, runs the gate on
both hosts under the shared gate slot, stamps the branch's DEVLOG entry, runs the landing checks and hands the branch to
GitHub's merge queue as one pull request (or, while `ruleset.py check` does not report the queue ACTIVE, lands it
through push-main.sh). There is no train and no lander: this unit runs the script and dispatches an agent only for
what a script cannot decide.
1. Load the `workstream` skill (the dispatch guard refuses a finisher or reviewer otherwise); read
   .claude/skills/workstream/references/landing.md.
2. REVIEW FIRST (MANDATORY-PRACTICES I10; kb/Work PB3027): for each row `python scripts/prune_worktrees.py --brief`
   lists WAITING FOR REVIEW (a DONE branch whose report carries no `Review: CLEAN` stamp at its tip: a branch finished
   outside a rolling wave, or one whose wave stopped before its review), dispatch ONE read-only `cobol-reviewer` (Agent
   tool, model opus; all of them in one message) whose prompt gives the branch, its worktree, its tip, its report, the
   findings file `{COORD}\scratch\review-<branch>.jsonl`, the reviewer label `enqueue-review-<branch>-r1`, round 1, both
   stop files, and says: follow "The branch form" of `.claude/skills/review/SKILL.md`. A CLEAN stamp makes the row
   WAITING TO LAND for step 3; a FINDINGS stamp makes it a DECISION row whose finisher the next wave plans
   (plan_wave.py). Never fix a finding here.
3. LAND. The candidates are the rows `python scripts/prune_worktrees.py --brief` lists WAITING TO LAND, in the order it
   lists them (the most harmful lead first, kb/Work PB2981's ranking); an operator steer that names branches
   (`handoff.last.json`) wins. For each, `python scripts/orchestrator/enqueue.py land <branch>` with `run_in_background`
   to its own log, then BLOCK on the log's `=== ENQUEUE: ` line (`timeout 580 bash -c 'until grep -q "=== ENQUEUE: "
   <log>; do sleep 5; done'`, re-issued until it prints; never end a turn while one runs). While the merge queue is
   ACTIVE start them all at once: each gate takes the shared gate slot, so at most the cap gate together and the rest
   wait there. While it is not, run them ONE AT A TIME (push-main.sh holds the landing lease and waits for CI). Read
   each verdict:
   - `ENQUEUED`: the pull request is in the queue; GitHub tests it in a merge group and merges it. The branch is ON
     MAIN only once the PR has merged (`gh pr view <n> --json state,mergedAt`); until then it reads ENQUEUED.
   - `LANDED` (push-main): on main.
   - `CONFLICT` (exit 4), `GATE RED` (exit 5), a DEVLOG refusal (exit 6), a stale arch-oracle baseline (exit 7) or a
     push-main CI red (exit 10): the reason is in the log and in the branch's report (`BLOCKED: enqueue …`): step 4.
   - exit 3 (the landing check's STOP: the branch shares a file with earlier-dispatched in-flight work): never retry
     as is; name it in the handoff with the check's lines.
   - exit 9 (GitHub refused the push, the PR or the queue) or exit 10 for a held landing lease: re-run once; if it
     repeats, name it in the handoff.
   - A permission refusal of `enqueue.py land` (the auto-mode classifier, a denied prompt): never retry it, rephrase it
     or land another way (MANDATORY-PRACTICES L13); hand off `READY-TO-LAND <worktree> <branch>` in `summary`.
4. FINISHERS. For each branch that stopped in step 3, and each row `prune_worktrees.py --brief` lists BOUNCED (the merge
   queue removed its pull request; GitHub's reason named), dispatch ONE `cobol-finisher` (Agent tool, model opus; all
   of them in one message) with `.claude/skills/workstream/templates/finisher-brief.md`, its prompt naming the branch,
   its worktree, its report, the stop line verbatim (or the PR number and the bounce reason, with the merge group's
   failed run from `gh run list --event merge_group --limit 20`) and both stop files. The finisher fixes the branch at
   its root and lands it with `enqueue.py land` itself; stay until every finisher returns.
5. LEADS. The reports this unit landed carry new defects as note-ready paragraphs (their "New defects" section, the
   implementers' `leads`). Dispatch ONE `cobol-clerk` with ids from `python scripts/orchestrator/alloc.py pb <n>`
   (one note per MECHANISM, grepped against kb/Work first, MANDATORY-PRACTICES R1-R3), in a worktree cut from
   origin/main, which commits the notes with a `DEVLOG/pending-<slug>.md` entry and lands its own branch with
   `enqueue.py land`. Skip it when no report carries a lead.
6. `python scripts/prune_worktrees.py --apply` removes the branches that landed (O10).
7. Render the ledger in the same turn: `python scripts/spec/gen_ledger.py --out {COORD}\ledger.html` (every figure on it
   is computed; its trend file gains a point when GAP or the register's series moved). You cannot PUBLISH it (a headless
   session has no Artifact tool): the supervisor announces the owed publish after you end, and the attended session
   publishes `{COORD}\ledger.html` to the owner's artifact. Do not look for a way around that.
8. Write the handoff (`landed`; the branches still pending, an ENQUEUED one with its PR number; `next_unit` null unless
   you know better).
