## Entry 2053 — 2026-10-16 03:57 PDT — One lifecycle rule for a finished branch: the report, the review and the landing lane's attempt at the tip (PB3181, PB3125); One dispatch ledger with structured writers, one file-set partition for every plan, and a landing check that binds a branch to one dispatch (PB2807, PB3186)

### One lifecycle rule for a finished branch: the report, the review and the landing lane's attempt at the tip (PB3181, PB3125)

The branch survey (`scripts/prune_worktrees.py`) admitted a report to the landing states only when it said DONE and
was no older than the branch's last activity plus ten minutes. Three finished shapes fell through to DECISION, so the
operator was asked to decide branches that were only waiting (kb/Work PB3181), and a branch whose landing-lane attempt
had STOPPED had no state of its own (kb/Work PB3125). Re-probed on this tree before the change:
`worktree-wf_15ec6338-3a0-11` (PB934, DISCHARGED with its retirement and DEVLOG entry, reviewed CLEAN at its tip) read
DECISION; and `3a0-19`, `423-5` (exit 4, CONFLICT) and `423-8` (exit 5, Linux GATE RED) read WAITING TO LAND although
the lane's newest attempt at each tip had STOPPED and the lane never relaunches a stop at the same head. The operator's
shape 3 (w1080f's report began with an empty line and no `Status:`; `review_findings.py stamp` stamped it under line 0)
was read on the same tree.

The fix is ONE rule of three coverages, decided by the Mythos session (Decision 1 of
`E:/COBOL-coord/scratch/mythos-item1/decisions-item1-20261015.md`):

- **The report.** `plan_wave.Report.finished` is the one predicate of a finished report: DONE, or DISCHARGED with a HEAD
  past its base (`Report.base`, from the header's `Base:` field). It decides the branch only where its HEAD covers the
  tip, the tip itself or only mechanical commits after it (`unreviewed_since`, the coverage rule the review stamp and
  `enqueue.py land` already read). The age test and `REPORT_SLACK_MIN` are deleted. A report naming no HEAD, or followed
  by a commit no report describes, is a DECISION that says so. The header's HEAD and BASE are now read field-first: the
  first-match rule read w1080f's predecessor head ("(reviewed head db7e19186 merged in) · head 5df3c56a3"). Measured
  over the 360 reports on disk, the new reading changes that one, reads 17 the old one missed, and 12 name no head (no
  live branch carries one of those 12).
- **The review.** Unchanged: a CLEAN stamp covering the tip.
- **The landing lane.** The survey reads `land_lane.view()` (imported lazily, one reader; its status text is never
  parsed) and passes it to the pure `lifecycle()`. The lane's newest attempt at the tip decides the state: its STOPPED
  class gives the new state `STOPPED`, which names the attempt and its owner and carries the predecessors it contains
  (they read IN FLIGHT); exit 0 reads ENQUEUED; a running, retryable or re-armed attempt reads WAITING TO LAND with the
  lane's own action text. A STOPPED branch never reads WAITING TO LAND, which honours PB3125's rejected shape.

The rolling wave's JavaScript copy of the predicate (`hasWork`) is deleted. A workflow script runs no process, so one
mechanical `cobol-locator` runs `plan_wave.py --finished <report>` (exit 0 finished, 1 not, 2 unreadable) on each DONE
or DISCHARGED return. The wave treats a refused return as SPLIT and does not review it. `review_findings.py stamp` now
refuses a report that is not finished, instead of stamping it. The sibling arms that read `status == "DONE"` as
"finished" now use the predicate: `plan_wave`'s branch carrier, finisher pred and FINDINGS arm, and `landing_measure`'s
seed reports. Every reader of the state vocabulary gained STOPPED: the verdict line, `CARRIER_STATES`,
`land_lane.candidates`/`agent_work`, `next_unit.py`, `session-probe.ps1`, the enqueue and resume unit prompts,
MANDATORY-PRACTICES O10/O11, DESIGN-orchestrator-loop sections 3.2 and 4.9, and the workstream and review skills.

After the change the live survey read `3a0-11` WAITING TO LAND and `3a0-19`, `423-5` and `423-8` STOPPED with their
exits and owners. Within the hour the enqueue unit's finishers took all three, and the survey now reads them IN FLIGHT.
`3a0-4` and `db5-30` have since landed. Self-tests: `prune_worktrees --self-test` gains a STOPPED branch after a clean
merge of main plus a DEVLOG stamp, with its contained predecessor IN FLIGHT. It also covers an exit-3 attempt, an exit-0
attempt, a running attempt, an attempt at another head, a DISCHARGED report with and without branch work, a report with
no HEAD, and `resumed` rewritten to a non-mechanical commit after the report. `land_lane`, `review_findings` and
`test_plan_wave` gain the matching arms.

The owner's release record went into the register on this branch: "Release to Mythos now" (2026-10-15 ~12:45 PDT).
PB2911 and PB1754 now say that the hold no longer covers the Mythos lane, and PB2119 is unblocked.

Gate (run `TestResults/build-local/20261015T202819Z-fa373b`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 6,065/11,708 · Unit 33,128/33,667 · Characterization 6/36 ran; the 28 changed script self-tests GREEN, among them prune_worktrees, land_lane, review_findings, test_plan_wave, landing_measure and enqueue). Linux gate: the landing's (batched gating).

### One dispatch ledger with structured writers, one file-set partition for every plan, and a landing check that binds a branch to one dispatch (PB2807, PB3186)

Wave 1083 group B, the Mythos lane's second branch (it merges group A, PB3181 + PB3125, which lands through it). The
fix shapes are the Mythos session's decision 2 of 2026-10-15, read from the code on main f5fec711b.

**PB3186 (filed here): the ledger's hand entries.** `scripts/hooks/dispatch_guard.py` rule 3 took every `PB<digits>`
id an admitted Agent or Workflow call's prompt mentioned, and every id in each `.md`/`.txt`/`.json` file the prompt
named, and recorded them through `plan_wave.py --record-dispatch` as one `hand` entry with the union of their computed
file sets. A brief names reference notes, so a finisher, a clerk or a Workflow call each recorded hundreds of files as
dispatched work: six entries of 339 to 890 files at 12:50 PDT, five of 303 to 910 files at 13:40. The landing check's
`identity()` then bound an in-flight worktree to the union of every ledger group its diff met, so fix-lane worktrees on
`DataBinder.*.cs` read as "PB1682, PB1698, PB1699, PB1701 work by its dispatch ledger" and, those notes being R3 work,
the check stopped fix lane against fix lane: 23 exit-3 stops over six branches in the landing-lane logs of 2026-10-14
23:23 to 2026-10-15 18:16. The fix: the ledger has structured writers only. `plan_wave.ledger_groups` is the one
reader, and it ignores an entry with `hand: true` or a wave that is not a number; `plan_wave.record_dispatch` is an
UPSERT keyed on (wave, letter) that drops those entries, so today's junk retires at the first record after this lands,
with no hand edit of coordination state; `make_dispatch_specs.py` records every group it renders (`record()`, its
`file_set` LIST, which `plan_wave.py` now writes into `groups.json`, or the set computed from the group's notes when a
hand-built groups file carries none: `plan_wave.computed_file_set`, moved from `landing_check.computed_declared`).
The planner no longer writes the ledger itself: it runs the renderer. Deleted: `dispatch_guard.py`'s rule 3
(`dispatched_ids`, `record_hand_dispatch`, the `PB` and `NAMED_FILE` regexes, `READ_ONLY_ROLES`, `PLAN_WAVE`);
`plan_wave.py`'s `record_hand_dispatch`, `record_dispatch_main`, `--record-dispatch`, `--label`, `HAND_TTL_SECONDS` and
the hand arms of `inflight_file_sets` and `held_by_dispatch`. `landing_check.identity()` binds a worktree to ONE
dispatch: its fix-lane report (the newest whose branch field names it) and that report's ledger group, else its commit
subjects, else the one group that best covers its diff (`best_group`: most of its files, then the smallest declared
set, then the latest). The train manifests it also read have had no writer since the train retired (PB3023); they are
gone from the check (a merged predecessor is the landing's own by ancestry, re-committed content by content).
`prune_worktrees.in_flight_slugs` reads the entries' recorded slugs instead of parsing owner labels.

**PB2807: the partition for every plan.** Re-probed at 13:40: the dry fix-lane plan put PB3168 on
`ReferenceFormatProcessor.LogicalLineBuilder.cs`, the file of the loop's running wave 1084 group H (PB1804), because
`busy` was built only under `--cluster`. The note's premise ("only the landing check catches it") was corrected: the
check stops only R3 overlaps, and a fix-lane overlap costs a conflict finisher at the merge. Now `plan_wave.py` builds
the in-flight set for every plan and `classify_inflight` reads each owner through the branch survey's lifecycle
state (`prune_worktrees.survey`, the one lifecycle rule): a group meeting work WAITING FOR REVIEW, WAITING TO LAND or
ENQUEUED is planned as its continuation (`continuation_pred`: merge that branch first, no `after:`); a group meeting a
dispatch with no report, or a branch IN FLIGHT, STOPPED, BOUNCED or undecided (DECISION), waits with the collision and
the owner's state named; a LANDED branch and a report-answered dispatch are gone; a group's own work never collides
(`Group.continues`, set by every finisher arm; a dispatch or branch on the group's own notes). The external-repository
slices still gate the campaign only. Measured on a copy of the ledger without wave 1085: PB3168 waits on `w1084h`
[DISPATCHED, NO REPORT YET] and the Mythos lane's in-flight `faa-5`; PB3165 and PB3153 wait on DECISION branches the
survey names; PB2099's finisher, PB3187 and PB3127 are planned.

**Self-tests.** `test_plan_wave.py` 184/184 (new: running work defers in the fix lane, a finished branch is continued
with its pred, a dispatch or earlier branch on the group's own notes never defers, a finisher never collides with its
own predecessor, `classify_inflight` over every survey state, the upsert and the dropped hand entry, the renderer's
record from a `file_set` list and from the computed set; the plan golden rewritten for the new `file_set` key only).
`landing_check.py --self-test` 35/35 (new PB3186a-d: a fix-lane worktree on a file an R3 group also declares binds to
the group covering its diff and passes; by coverage a WIP worktree is R3 work and stops, its report binds it first and
it passes; a hand entry and a non-numeric wave are no dispatch; K3 reworked without manifests). `test_dispatch_guard.py`
23/23 (the hook writes no ledger, for a hand dispatch, a brief-only prompt, an implementer and a Workflow).
`prune_worktrees.py --self-test` GREEN (+ the recorded-slug reader).

**Docs.** MANDATORY-PRACTICES O9, the workstream SKILL's Specs bullet, the architecture-review SKILL, DESIGN-architecture-review
section 8.7, DESIGN-orchestrator-loop section 9 (item 1b, new) and its coordination table describe the current
writers and the one partition rule.

**Gate.** === BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN === (run TestResults/build-local/20261015T210113Z-619d82; the changed self-tests test_plan_wave, test_dispatch_guard, landing_check, prune_worktrees, enqueue GREEN); semgrep verify.py PASS. LINUX GATE: the lander's (batched gating). `work.py check` green. PB2807 and PB3186 landed, `closes_rows: []` (process tooling).

**Branch review round 1 (two findings, both fixed).** R1-1: the branch fixed the mechanism of three notes that were
already open (PB2614, the guard recorded every Workflow and lander call; PB2701, identity() bound a worktree to the
union of its ledger groups; PB3040, hand entries never retired) and left them open with code sites it deleted, so the
campaign planner still listed PB3040 as work. They are landed now, each with what removed its mechanism, and PB3186
names them. The sibling sweep of every open note naming the hand ledger found PB2795 (a RETURNED group kept its whole
declared set in flight at the landing check): the planner had dropped report-answered groups privately in
`classify_inflight` since this branch, the landing check had not. The rule moved to `inflight_file_sets` source 3
(`reports=`), the one reader the planner, the landing check and the survey's in-flight slugs share, and PB2795 landed
with it; PB2974 stays open with a note on why the partition does not close it. R1-2: the planner surveyed branches
without the open pull requests, so it could never read BOUNCED and read a bounced branch as ENQUEUED (a CONTINUATION)
where the landing lane read it BOUNCED (wait). `prune_worktrees.live_survey` is now the one production survey (reports,
in-flight slugs, pull requests), called by `prune_worktrees.py`, `land_lane.survey_rows` and the planner, and
`survey(prs=)` is required so no caller reads a git-only lifecycle by omission. New self-test checks: live_survey reads
ENQUEUED and BOUNCED; no orchestrator script calls `prune_worktrees.survey` directly; an answered group is not in flight
(prune_worktrees), holds no declared file (test_plan_wave), and frees its untouched declared files at the landing check
(landing_check PB2795).Re-gate after the review fixes: === BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN === (run TestResults/build-local/20261015T212233Z-74273d; Conformance 6,102, Unit 33,266, Characterization 6 passed, 0 failed; the changed self-tests plan_wave, prune_worktrees, landing_check, land_lane, enqueue GREEN); semgrep verify.py PASS; `work.py check` green. PB2614, PB2701, PB3040 and PB2795 landed, `closes_rows: []` (process tooling).

