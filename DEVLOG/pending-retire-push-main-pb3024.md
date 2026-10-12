## PB3024 closes: the direct-push landing is deleted, the merge queue is the only path; PB3095 (a `cd` in a backgrounded list) and PB3096 (pre-queue reds) fixed on the way

### PB3024's closing change: no leftover path to main

Since the ruleset went ACTIVE (2026-10-11 19:14) main has refused every direct push, and since train 1067 the one
landing step is `enqueue.py land`. The direct-push landing script, the landing lease that serialized it, their drift
tests and the `ci/**` landing-branch trigger were still in the tree, along with 36 files that described them as current
(CLAUDE.md rule 4: no leftover paths). This change deletes `scripts/push-main.sh`, `scripts/orchestrator/landing_lease.py`
and `LandingLeaseDriftTests`, and sweeps every current-state mention.

**The workflow.** `push.branches` is `[ main ]`. Two arms died with `ci/**` and went too: the `changes` job's
new-branch base (only main is pushed now, so a `before` the runner cannot resolve simply runs the matrix) and the
DEVLOG step's push arm (the step runs on `pull_request` and `merge_group` only, and fails closed on anything else).
**PB3024 item 3, measured:** main's `push` run after each of the first three queued landings (PR 7 → 79d760ab6, PR 8 →
424e699ec, PR 9 → 9d18aa9ab) printed `run_matrix=false (code=true verified=true) … a non-PR run already succeeded on
<sha>`; for 9d18aa9ab the one green run is the `merge_group` run on `gh-readonly-queue/main/pr-9-…`. The merge-queue
commit is the merged sha, so the matrix runs once per landing and the short-circuit needed no fix; its comment now
says so.

**The loop.** `next_unit.py` lost the lease deferral: finished branches start an `enqueue` unit, always
(`test_orchestrate_campaign.ps1` 8f proves it in both lanes); `stop.ps1 -Status` lost the lease line. `ruleset.py`'s
ABSENT and SKIPPED details say landing is BLOCKED until the queue is ACTIVE, because the queue is the only path.

**The arm.** `LandingPipelineDriftTests.TheDirectPushLanding_IsRetired_AndNothingNamesIt` replaces the test that
read the deleted script: the four retired files (both forms of the push script, the lease, the train applier) do not
exist, the push trigger is `[ main ]`, and no tracked file outside `DEVLOG/`, `docs/rearchitecture/evidence/`,
`kb/Work/` and `adjudication/` names any of them. A planted-name arm fires its failure branch. A past fact that must
stay says "the former direct-push landing script" or "the retired train applier".

**The docs.** `DOC_INDEX.md` lost the push-main row; plan §0's pipeline bullet and §9's landing paragraph describe the
queue; `DESIGN-test-build-ci.md` §1.4, §3.17.6 and the §3.17.8 arm read as landed; `DESIGN-orchestrator-loop.md` §3.2
rules 2 and 6 lost the deferral and §4.7 is now a dated past decision; the gate, land-a-fix, workstream and
architecture-review skills and MANDATORY-PRACTICES P2, P14, L1, L3 and L13 name `enqueue.py land`.

### PB3095: the guard hook now enforces P14(b)'s worst case

The operator's train-1067 land unit ran its audits as `cd X && a & b &`. A single `&` backgrounds the whole and-or
list, so the `cd` moved only that subshell: `b` read the caller's tree and reported a false OK into the queue.
`forbidden_commands.py` rule 7 splits a command line into and-or lists (quotes respected; `2>&1`, `&>`, `|&` and
PowerShell's `&` call operator are not background operators) and blocks a list that holds a location change, ends in
`&` and has another command after it. `cd X; a & b`, `cd X && a` and `cd X && a &` alone still pass; 105/105 hook
cases green.

### PB3096: a pull request's red before the queue is measured

`landing_measure.py complete` counted only failed merge-group runs, so a pull request whose own `pull_request` run went
red before the queue took it was recorded as 0 bounces. It now records `pre_queue_reds` from the pull request's head
branch; a branch with no such run stays UNMEASURED, never 0, and `summary` prints the figure beside bounces. Self-test
arm 9, pinned by `LandingMeasureDriftTests`; a mutation that always records the count fails it.

**Self-review.** Architecture: one landing path, one deferral rule removed rather than wrapped; the hook's new rule is
its own parser beside `_segments`, which splits on different separators for a different rule. Code: every arm of the
deleted mechanism found by the tree sweep, the arm itself, and `git grep` on lease, `READY-TO-PUSH` and `ci/<sha>`;
the docstrings that now name `enqueue.py land` as `landing_check.py`'s caller were checked against the workflow (CI runs only its self-test).
Performance: the new sweep reads the tracked tree once, as its sibling sweep does. Duplication: none added.
