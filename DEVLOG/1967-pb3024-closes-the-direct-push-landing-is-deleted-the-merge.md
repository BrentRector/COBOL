## Entry 1967 — 2026-10-11 23:48 PDT — PB3024 closes: the direct-push landing is deleted, the merge queue is the only path; PB3095 (a `cd` in a backgrounded list) and PB3096 (pre-queue reds) fixed on the way

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

**The docs.** `DOC_INDEX.md` lost the push-main row; plan section 0's pipeline bullet and section 9's landing paragraph describe
the queue; `DESIGN-test-build-ci.md` sections 1.4, 3.17.6 and 3.17.8's arm read as landed; `DESIGN-orchestrator-loop.md`
section 3.2 rules 2 and 6 lost the deferral and section 4.7 is now a dated past decision; the gate, land-a-fix, workstream and
architecture-review skills and MANDATORY-PRACTICES P2, P14, L1, L3 and L13 name `enqueue.py land`.

### PB3095: the guard hook now enforces P14(b)'s worst case

The operator's train-1067 land unit ran its audits as `cd X && a & b &`. A single `&` backgrounds the whole and-or
list, so the `cd` moved only that subshell: `b` read the caller's tree and reported a false OK into the queue.
The shared parser `shell_location.py` now models it: a list that ends in `&` has its state restored at the `&`
(Bash subshell or PowerShell 7 job), and the first step after it carries that list's location changes in
`Step.stranded`. `forbidden_commands.py` rule 7 blocks such a step. `cd X; a & b`, `cd X && a`, `cd X && a &` alone
and `(cd X && a) & b` still pass.

The branch review (R1-1) found the first version's rule 7 in a private splitter that knew neither comments nor
escapes: `git status  # don't worry` on one line and `cd /e/x && a & b` on the next passed, and so did an escaped
quote before the chain. The shared parser already handled both but made only the single command before a `&` transient,
so it gave the command after `cd X && a &` the cwd X, and the build guard and `status_guard.py` judged it that way. The
fix moved the and-or list into the parser and deleted the private splitter. Rules 5 and 6 had the same splitter
(`_segments`), so they now group the parser's steps too: a comment, an escaped quote or a heredoc before
`dotnet test … && git commit` no longer hides the chain, and a `bash -c 'wsl --shutdown'` or `echo y | wsl
--terminate` is seen. Every new case was RED against the reviewed head: hook 117/117, parser 73/73.

### PB3096: a pull request's red before the queue is measured

`landing_measure.py complete` counted only failed merge-group runs, so a pull request whose own `pull_request` run went
red before the queue took it was recorded as 0 bounces. It now records `pre_queue_reds` from the pull request's head
branch; a branch with no such run stays UNMEASURED, never 0, and `summary` prints the figure beside bounces. Self-test
arm 9, pinned by `LandingMeasureDriftTests`; a mutation that always records the count fails it.

**Self-review.** Architecture: one landing path, one deferral rule removed rather than wrapped; the hook's new rule is
read through the shared parser, as rules 5 and 6 now are (no hook splits a command line itself). Code: every arm of the
deleted mechanism found by the tree sweep, the arm itself, and `git grep` on lease, `READY-TO-PUSH` and `ci/<sha>`;
the docstrings that now name `enqueue.py land` as `landing_check.py`'s caller were checked against the workflow (CI runs only its self-test).
Performance: the new sweep reads the tracked tree once, as its sibling sweep does. Duplication: none added.

**Gate.** The first gate stopped RED in its audits (a bare section sign naming a design-doc section in the PB3024 note read as an ISO clause; written as "section N"). The re-gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,524 · Unit 772 … Conformance 4,964/11,488 · Unit 32,680/33,452 · Characterization 36/36 cases ran (skipped 0) in 1 of 2 leg(s)` (run 20261012T055750Z-09d458); every `LandingPipelineDriftTests` and `LandingMeasureDriftTests` case passed in leg 1, and the self-tests audit re-ran the hook, campaign, steer, enqueue and prune self-tests GREEN. Semgrep: no rule up. After the review fix (R1-1), the review finisher's gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 6,605 · Unit 777 … Conformance 4,883/11,488 · Unit 32,675/33,452 · Characterization 36/36 cases ran (skipped 0) in 1 of 2 leg(s)` (run 20261012T062508Z-8dc27e); the shell_location, forbidden_commands, worktree_rm_allow and dispatch_guard self-tests re-ran GREEN on their changed inputs.

