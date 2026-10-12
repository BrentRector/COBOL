## Entry 1966 — 2026-10-11 21:28 PDT — PB2987 + PB3025: the ledger's owed check reads main, and the DEVLOG becomes one file per entry; PB3023 + PB3016: the landing is a script — enqueue.py land; the train and the lander retire

### PB2987 + PB3025: the ledger's owed check reads main, and the DEVLOG becomes one file per entry

**PB2987 — the owed-publish check read a stale checkout.** After train 1056 landed the supervisor announced no ledger
publish: `ledger_state.py owed --repo $RepoDir` stamped the main checkout's HEAD, and every landing is pushed from a
lander's own worktree, so that checkout stays at whatever train someone last fast-forwarded it to. The stamp is now
read on `origin/main`, fetched first (`--main-ref`); the checkout's own stamp is reported beside it, because the page
is still rendered from the checkout: `owed` and `gen_ledger.py` name a stale checkout and the fast-forward that cures
it, and `mark-published` refuses while the checkout is not main's, so a page that does not describe main is never
marked current. `test_ledger_state.py` now runs against a bare origin and pushes a landing from a second clone. The
other `--repo $RepoDir` readers read branches, worktrees and the coordination directory, not the tree; `next_unit.py`'s
campaign lane reads `kb/Work` from the scripts' own checkout and is stale the same way (reported, not fixed here).

**PB3025 — one file per DEVLOG entry (owner 2026-10-11, PB3015 Q2).** With a merge queue, two landings that both
insert at the top of one `DEVLOG.md` collide. `scripts/devlog.py` is now the one tool: `render` (the README, then the
entries newest first, concatenated byte for byte), `stamp <branch>` (the implementer's unnumbered
`DEVLOG/pending-<slug>.md` gets its number from `alloc.py devlog` and its header stamp, in one commit), `check` (the
landing rule `landing_devlog.py` was: exactly one new stamped entry numbered above main's, and nothing malformed,
pending, duplicated, deleted or monolithic), `next`, and a 35-arm self-test that also lints the repository's own log.
The landing checks (`enqueue.py checks`, exit 6, which `push-main.sh` runs too) and a new CI `audits` step run `check`; `alloc.py`, `plan_wave.py`, `inventory_ratchet.py`,
`census.py` and `check_practices.py` read `DEVLOG/`; `plan_wave.py` plans no DEVLOG number any more.
`landing_devlog.py` and `prepend-devlog.py` are deleted.

The migration splits the committed `DEVLOG.md` (mixed CRLF and LF line ends) into a README and one LF entry file per
header; on main 01f392184 that was 1,982 files through Entry 1962, and rendering them reproduces the LF-normalized
monolith byte for byte (sha256 72a60802…). The landing re-runs it on main's `DEVLOG.md` and deletes the script.

The branch review found two holes, both fixed before landing. `check` counted a deleted entry only against the base's
`DEVLOG/` files, so the one landing whose base still holds the monolith could keep a stale split, drop main's newest
entries and pass; it now counts the monolith's headers too, with their repeats, and names each one no entry carries.
And the README, the tool's docstring and the design named the split's range as a fixed count that every re-run would
make stale; the migration now rewrites the README's range itself and the others name none.
Every header was kept as written. The history held four surprises: twenty numbers used twice (008–014, 171–185),
entries 1191a–1191d, twelve headers with a double-encoded dash, and undated early entries. A letter in the FILE name
(`0185b-…` above `0185a-…`) keeps each duplicate pair in its original order without a hand list. CLAUDE.md rule 6, the
briefs (the implementer writes the pending entry; a train lander folds its clusters' pending files into one and
stamps it), the skills, DOC_INDEX, the plan and the two design docs describe the new shape.

### PB3023 + PB3016: the landing is a script — enqueue.py land; the train and the lander retire

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

**Train 1067 (the lander's paragraph).** One cluster, A2, carrying A through it (operator steer: A2 alone through the
merge queue). `train_apply.py` stopped before applying anything: `git apply -3` could not apply DEVLOG.md and three
files A2 deletes (`lander-brief.md`, `lander-train-brief.md`, `units/land.md`) that train 1066 had modified, so the same
base..head change came in by `git merge --squash` onto main 424e699ec, fourteen conflicts resolved by hand, every path
only one side touched verified byte-identical to its side (silent-loss check). DEVLOG.md: main's file (through Entry
1965) taken and the branch's `devlog_migrate.py` re-run, 1,985 entry files rendering IDENTICAL to the LF-normalized
monolith (sha256 e6248fd4...), README range 001-1965, both pending entries kept; the script is deleted in this landing
as its docstring, DESIGN-test-build-ci.md 3.17.4 and PB3025 require. Train 1066's PB3028 (`landing_measure.py`) is
carried into A2's world: `enqueue.py land` gains a `measure` collaborator passed to `queue`, so a landing by `land`
writes its record as `queue` does (new self-test arm, pinned in LandingPipelineDriftTests); the retired land unit's
`landing_measure.py complete` step moves to `units/enqueue.md` step 6b; MANDATORY-PRACTICES L12, the workstream gate
line, DOC_INDEX, DESIGN 3.17.2, plan section 0 and PB3024 keep main's facts (the queue ACTIVE since 2026-10-11 19:14,
`landing_rate_input`, `landing_cost`). An INTERACTION the gate would have hit: main's LandingMeasureDriftTests exempted
`DEVLOG.md` as narrative, so after the migration every history entry naming `train_measure.py` was an offender; it now
exempts `DEVLOG/`. Gates on 9567bf246: the first Linux gate went RED on
`LandingPipelineDriftTests.TheQueueAndTheRuleset_DeclareEveryArm` (the resolution had renamed a pinned enqueue
self-test arm; the arm was restored and the measurement became its own arm, then both gates re-ran). Windows
`LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 4,769/11,488 · Unit 32,622/33,451 · Characterization 36/36;
leg 2 to CI's merge group), `LINUX GATE: GREEN` (unit 33,484, conformance 11,488, characterization 36, guard NIST 362
MATCH, self-tests), ARCH-ORACLE IDENTICAL (7,780 cases), semgrep PASS (no count rose), citation audits 0 findings,
GAP unchanged (no inventory rows). No cluster dropped or ejected. (The `work.py check` OK first reported here was false: the lander ran it in a backgrounded subshell that a `cd ... &&` did not reach, so it, the doc-citation audit and semgrep checked the main checkout, not the train.)

**PR 9's first CI run was RED, and the gate is fixed (the CI invariant, kb/Work PB1957 row 44).** The pull request's own run (38204092358) failed CI's `audits` job on `work.py check`: PB3024 still said `blocked: true` while its `blocked_by` PB3023 lands in this very train (an interaction of train 1066's H, which set the blocker, with A2, which lands it), so the queue never took the PR. A2's cluster commit now sets `blocked: false`, keeping `blocked_by` as the record. The local gate had been GREEN over that tree because CI's `audits` job ran `work.py check` and `check_practices.py` and `scripts/run_gate_legs.py` AUDITS ran neither, a gap PB1957 row 44 had recorded since 2026-10-04 with no guard. AUDITS now carries WORK REGISTER and FLEET PRACTICES; AUDIT_PREREQUISITES runs CI's own `git submodule update --init tools/claude-skills` first (a failed prerequisite is RED and no audit runs, a new self-test arm); and `SelfTestDiscoveryDriftTests.EveryCommandOfCisAuditsJob_IsAGateAudit_OrSaysWhereItRuns` maps every command of the job to an audit, a prerequisite or a recorded reason, so the next audit CI gains fails a test until the gate runs it. The one recorded reason is `devlog.py check`: a landing check, not a tree audit, because an implementer's entry stays `DEVLOG/pending-*.md` until `enqueue.py` stamps it and the check refuses a pending entry; `enqueue.py checks` and CI run it on every landing. The same train fixes three stale references the lander had found (I9): `devlog.py`'s monolith arm pointed at the deleted `devlog_migrate.py`, `landing_measure.py` described the retired `train_apply.py` in the present tense, and DOC_INDEX named a `push-main.ps1` deleted by PB2234. Both gates re-ran on the new head.

