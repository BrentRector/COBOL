UNIT: enqueue.

The landing pipeline has work only an agent can do. Landing itself is a SCRIPT, `python scripts/orchestrator/enqueue.py
land <branch>` (kb/Work PB3023; DESIGN-test-build-ci.md section 3.17.2 step 3): it merges origin/main into the branch,
runs the gate on both hosts under the shared gate slot (a landing's gate is served first, kb/Work PB3103), merges main
in again if it moved meanwhile (no second gate: the merge group runs the whole population), stamps the branch's DEVLOG
entry, runs the landing checks and hands the branch to GitHub's merge queue as one pull request. Main takes only queued
pull requests, so while `ruleset.py check` does not report the queue ACTIVE the script refuses before it merges or
gates anything (exit 8). THE LANDING LANE runs it, not this unit (kb/Work PB3103; DESIGN-orchestrator-loop.md section
4.9): the supervisor ticks `python scripts/orchestrator/land_lane.py tick` beside every unit, which starts
`enqueue.py land` for every branch WAITING TO LAND the moment it is ready and retries what a script can see end. This
unit keeps the judgment: the reviews, the finishers, the leads, the records and the ledger.
1. Load the `workstream` skill (the dispatch guard refuses a finisher or reviewer otherwise); read
   .claude/skills/workstream/references/landing.md.
2. REVIEW FIRST (MANDATORY-PRACTICES I10; kb/Work PB3027): for each row `python scripts/prune_worktrees.py --brief`
   lists WAITING FOR REVIEW (a DONE branch whose report carries no `Review: CLEAN` stamp at its tip: a branch finished
   outside a rolling wave, or one whose wave stopped before its review), dispatch ONE read-only `cobol-reviewer` (Agent
   tool, model opus; all of them in one message) whose prompt gives the branch, its worktree, its tip, its report, the
   findings file `{COORD}\scratch\review-<branch>.jsonl`, the reviewer label `enqueue-review-<branch>-r1`, round 1, both
   stop files, and says: follow "The branch form" of `.claude/skills/review/SKILL.md`. A CLEAN stamp makes the row
   WAITING TO LAND for step 3; a FINDINGS stamp makes it a DECISION row whose finisher the next wave plans
   (plan_wave.py). Never fix a finding here. A CLEAN stamp on a branch whose lane attempt stopped `UNREVIEWED` (exit
   11) at the same tip changes no commit, so the lane would never land it again by itself: re-arm it,
   `python scripts/orchestrator/land_lane.py rearm <branch> --why "reviewed CLEAN at <tip>"`.
3. THE LANDING LANE'S RESULTS. This step starts no landing of its own: the lane lands every WAITING TO LAND branch
   (kb/Work PB3103). Read `python scripts/orchestrator/land_lane.py status`: one line per branch, its latest attempt
   (the `=== ENQUEUE: ` verdict, the log) and who acts next. When its header says the survey is STALE or that the lane
   has not ticked (the supervisor's lane is off or failing), run `python scripts/orchestrator/land_lane.py tick` ONCE
   (the same deterministic lane: it starts exactly what a supervisor tick would) and read `status` again. An operator
   steer that names branches (`handoff.last.json`) is the lane's order too: name it in the handoff if the lane did not
   take them. By class:
   - `RUNNING`: being landed; nothing to do (do not wait on it: a later tick reaps it).
   - `DONE` (`ENQUEUED`, or `NOTHING TO LAND`): the pull request is in the queue; GitHub tests it in a merge group and
     merges it. The branch is ON MAIN only once the PR has merged (`gh pr view <n> --json state,mergedAt`); until then
     it reads ENQUEUED. Its leads go to step 5 and its record to step 6b.
   - `STOPPED`: `CONFLICT` (exit 4, before or after the gate), `GATE RED` (exit 5) or a DEVLOG refusal (exit 6): the
     reason is in the log and in the branch's report (`BLOCKED: enqueue …`):
     step 4. `UNREVIEWED` (exit 11): the branch's tip is not covered by its CLEAN review stamp (MANDATORY-PRACTICES
     I10): it goes back to step 2's reviewer, never to a finisher and never landed another way. A repeated exit 2 or
     9 (main kept moving, GitHub refused twice at one head): name it in the handoff with its log.
   - `RETRY-WHEN-FREE` (exit 3, the landing check's STOP: the branch shares a file with earlier-dispatched in-flight
     work) and `RETRY-ONCE` (a first exit 2 or 9, or a runner that DIED): the lane's own; it retries them (exit 3 once
     `landing_check.py` passes on the branch). Nothing to do here.
   - `HALT` (exit 8, `QUEUE NOT ACTIVE`): the lane starts no landing until the operator runs `land_lane.py clear-halt`
     once `ruleset.py check` reports ACTIVE; it already named it in the operator's mailbox. Land nothing another way:
     name it in the handoff.
   When steps 4 and 5 have handled them, `python scripts/orchestrator/land_lane.py collect <branch> ...` for every
   STOPPED and DONE branch this unit handled, so the next choice of unit does not count them again (next_unit.py rule
   6). A finisher's new tip is landed by the lane once it is reviewed CLEAN (a new head). A stop resolved with NO new
   commit is landed again only on this unit's word: `python scripts/orchestrator/land_lane.py rearm <branch> --why
   "<what resolved it>"` (the lane lands it once more at the same head, on its next tick; steps 2 and 4 say when).
4. FINISHERS. For each branch that stopped in step 3, and each row `prune_worktrees.py --brief` lists BOUNCED (the merge
   queue removed its pull request; GitHub's reason named), dispatch ONE `cobol-finisher` (Agent tool, model opus; all
   of them in one message) with `.claude/skills/workstream/templates/finisher-brief.md`, its prompt naming the branch,
   its worktree, its report, the stop line verbatim (or the PR number and the bounce reason, with the merge group's
   failed run from `gh run list --event merge_group --limit 20`) and both stop files. The finisher fixes the branch at
   its root, gates it and returns DONE; it never lands: its commits are unreviewed, so the branch reads WAITING FOR
   REVIEW and the next enqueue unit's step 2 reviews the new tip before the landing lane lands it (`enqueue.py land`
   refuses an unreviewed tip with exit 11, UNREVIEWED: never a finisher's case to work around). Stay until every
   finisher returns. A finisher that resolved its stop with NO commit of its own (a bounce it attributes to
   infrastructure or load timing; a red already on main) leaves the head unchanged, so the lane would never land it
   again by itself: re-arm it (`land_lane.py rearm <branch> --why "<the finisher's finding>"`) when the cause is gone,
   and for a red still on main name it in the handoff and re-arm it once main carries the fix.
5. LEADS. The reports of the branches the lane queued (step 3's DONE lines, not yet collected) carry new defects as
   note-ready paragraphs (their "New defects" section, the implementers' `leads`). Dispatch ONE `cobol-clerk` with ids from `python scripts/orchestrator/alloc.py pb <n>`
   (one note per MECHANISM, grepped against kb/Work first, MANDATORY-PRACTICES R1-R3), in a worktree cut from
   origin/main, which commits the notes with a `DEVLOG/pending-<slug>.md` entry and lands its own branch with
   `enqueue.py land <branch> --lane clerk` (a lane MANDATORY-PRACTICES I10 does not review). Skip it when no report carries a lead.
6. `python scripts/prune_worktrees.py --apply` removes the branches that landed (O10).
6b. Complete the landing records (kb/Work PB3028): `enqueue.py` wrote each queued pull request's record;
   `python scripts/orchestrator/landing_measure.py complete` reads the merge time, the failed merge-group runs (bounces)
   and the CI wall from GitHub for every queued pull request that has no merge time yet; name a bounce's cause when its
   finisher knows it (`landing_measure.py record --pr <n> --bounces <k> --bounce-causes
   cluster-defect|load-timing|conflict|infrastructure`).
7. Render the ledger in the same turn: `python scripts/spec/gen_ledger.py --out {COORD}\ledger.html` (every figure on it
   is computed; its trend file gains a point when GAP or the register's series moved). You cannot PUBLISH it (a headless
   session has no Artifact tool): the supervisor announces the owed publish after you end, and the attended session
   publishes `{COORD}\ledger.html` to the owner's artifact. Do not look for a way around that.
8. Write the handoff (`landed`; the branches still pending, an ENQUEUED one with its PR number; `next_unit` null unless
   you know better).
