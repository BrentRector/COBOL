## PB3023 + PB3016: the landing is a script — enqueue.py land; the train and the lander retire

Wave 1062 group A2 (kb/Work PB3023, cluster PB3015; PB3016 closed by removal). The owner's 2026-10-11 decisions (PB3015
Q1b "GitHub merge queue", Q3 "one PR per finished branch", ~12:58 "Fast track now") left one step to build: the
landing itself, as a deterministic script, with the train, the lander role and its briefs gone.

**Built on its predecessors.** The branch merges group A (PB2987, PB3025: the DEVLOG as one file per entry, devlog.py),
group C (PB3024 phase 1: `enqueue.py queue|checks`, `ruleset.py`, the queue guard) and group E (PB3027: review on the
branch). The merge of C and A conflicted where C still named `landing_devlog.py` and `DEVLOG.md`; resolved to A's
`devlog.py check`, and `enqueue.py checks` now runs `devlog.py check` (exit 6), `landing_oracle.py` (7) and
`landing_check.py` (3).

**`enqueue.py land <branch>`.** Clean worktree, fetch, then MERGE origin/main into the branch. The design said rebase;
this very branch disproved it: it merged three siblings with conflict resolutions in its merge commits, which a rebase
throws away and asks for again, and the queue lands by merge commit anyway (DESIGN-test-build-ci.md §3.17.2 corrected).
A conflict aborts the merge, appends `BLOCKED: enqueue — … conflict in <files>` to the branch's report and exits 4.
Then the gate per host (the one gate, `build-local.ps1` at Normal priority under the shared gate slot, PB3022, then
`linux-gate.sh --nice` per the operator's steer until PB3022 item 10): only a GREEN verdict line passes (exit 5), and a commit gated GREEN is recorded in the
worktree's git directory so a re-run after a GitHub refusal does not spend another gate (the stamp commit, DEVLOG/
only, moves the mark). Then `devlog.py stamp`, which now re-stamps an entry main has overtaken (a bounce, a finisher):
`unstamp_overtaken` turns it back into a pending entry and the allocator numbers it above main's (devlog self-test
34/34). Then `queue` (the PR titled `DEVLOG NNNN: <title>`, its body the entry and the commit list) while `ruleset.py
check` reports ACTIVE, else `push-main.sh` (exit 10 when it does not land). Every run ends on one `=== ENQUEUE: ` line.
Seven self-test arms, pinned by LandingPipelineDriftTests.

**prune_worktrees.py** learns ENQUEUED and BOUNCED from ONE GraphQL read (`isInMergeQueue`, `autoMergeRequest`, the
newest `RemovedFromMergeQueueEvent`; probed live: the query runs and returns no open PRs today), offline git-only; a
predecessor contained in an enqueued or bounced branch lands through it. The train is gone (`--train`, TRAIN_MAX,
next_train, the lease-train and queued-train member sets); `--brief` lists WAITING TO LAND in PB2981's harm order.

**The loop.** `units/land.md` → `units/enqueue.md` (review, land, ONE finisher per stop or bounce, a clerk for the
reports' leads, prune, ledger, handoff); the unit name `enqueue` through next_unit.py, orchestrate.ps1, steer.ps1,
handoff.schema.json and the orchestrator self-tests. Retired: train_apply.py, lander-train-brief.md, lander-brief.md,
dispatch_guard.py rule 4, wf_rolling_wave.js's land() and train args (PB3016), model_rules.json's train keys. Renamed:
cobol-lander → cobol-finisher (+ finisher-brief.md), golden-lander-brief → golden-lane-brief. The planner prices each
group's landing (`landing_tokens`, seeded at the old lander's cost over five groups so a planned wave costs the same;
the plan golden re-recorded, the same groups chosen). MANDATORY-PRACTICES' landing section and O11, the workstream
skill, landing.md, the gate skill, CLAUDE.md Testing, DOC_INDEX and both design docs describe it.

**The branch review's seven findings** (wave 1064's review finisher). C and E had reached main as squash commits and
moved on, so a plain merge raised 33 conflicts; merged instead against a virtual base (this branch with A, C and E in
and none of its own commits), only A2's own changes met main's, 17 conflicts, each resolved toward main's current
content (the one gate shape of PB3022 carried into enqueue.py and every successor brief). The DEVLOG was re-split from
main's monolith through Entry 1963. Two mechanisms were missing: `devlog.py stamp` refused the two pending entries a
successor carries when its predecessor lands through it, and now folds them into the one entry, oldest first; and the
review-before-landing rule (MANDATORY-PRACTICES I10) was enforced nowhere once the lander went, so `enqueue.py land` now
refuses with exit 11 a tip that gained anything but main's commits, a clean merge of main or a DEVLOG-only commit since
its CLEAN stamp, and the finisher returns DONE for a re-review instead of landing its own fix (the registrar, golden
and clerk lanes, which I10 does not review, pass `--lane`). The rest was stale text naming retired artifacts or the
inner `queue` verb as the route.

The second review round found the landing step itself could not land in the live configuration: its push-main arm
merged and gated for half an hour with no landing lease held, though main now refuses a direct push; any pull request
the queue merged during a gate made `queue` refuse a head that no longer descended from main, and every re-run
re-gated into the same race; and the pull request was titled from a file-name test that read this migrating branch as
1,940 new entries. `land` now refuses before it merges or gates anything while the queue is not ACTIVE, merges a moved
main in again after its gate without a second gate (the merge group runs the whole population on the current main),
and titles the pull request from `devlog.new_entries`, the one definition the DEVLOG check and the re-stamp read too.

Left for others, named in the notes: push-main.sh, landing_lease.py and `ci/**` are no longer called by any landing
step and are deleted by PB3024's remaining step; the landing measurement with PB3028.
