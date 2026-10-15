## One lifecycle rule for a finished branch: the report, the review and the landing lane's attempt at the tip (PB3181, PB3125)

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

Gate: GATE-LINE
