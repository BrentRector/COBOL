## PB3311: the wave planner no longer re-plans a review finisher that is running, and one predicate says a dispatch returned

Wave 1105's unit (the loop, account 2) ran `plan_wave.py --from-budget --borrow-days 0` at 17:12 PDT. Its plan held
eight groups. Group A was a `FINISH-FIRST` for PB3198, answering the round-3 branch review finding of w1101a. An
account-1 session was answering that same finding at that moment, in the branch's own worktree (`wf_7bb8ec7e-39f-11`):
a WIP commit for R3-1 at 17:02, origin/main merged in at 17:03, a `build-local.ps1` gate running since 17:04. The unit
dropped A from the Workflow args, released `1105a` from the dispatch ledger and launched B to H. It filed PB3311 with
two holes; the branch review of that filing (R1-1) found the note's second fix shape rewrote the answered test that
open PB3152 already owned.

**Hole 1, fixed.** A finisher group's own branch is exempt from the file-set collision (`Group.continues`), and the
FINDINGS arm never asked whether that branch has a live owner. `plan()` now takes `states` ({branch: the branch
survey's lifecycle state}, the map `classify_inflight` reads) and a FINDINGS report whose branch the survey reads IN
FLIGHT (a live agent's lock, a dispatch out, a touch inside `--min-age`: the way a resumed branch whose tip moved past
the reviewed head reads) goes to `waiting` with the reason named, in the fix lane and under `--drain`, for an open note
and an unseen lead alike. The SPLIT and unlanded arms are untouched on purpose: a just-returned SPLIT branch is IN
FLIGHT for `--min-age` after its last touch, so the rule there would stall every SPLIT finisher three hours; PB2974
owns that arm. Live probe on this build: PB3174, whose FINDINGS branch is IN FLIGHT, printed as waiting with no
finisher.

**Hole 2, fixed once for both readers (R1-1).** `held_by_dispatch` released a hold when any report written since the
dispatch named one of its notes, by mtime, and the branch reviewer's `Review:` stamp rewrites the reviewed report, so
every review released the hold of a finisher still running. `inflight_file_sets` had a second rule (a bare slug match,
no time bound: PB3152's wave 1098 witness). Both now call `plan_wave.answers(report, group)`: the report is the group's
own (its slug, `w<wave><letter>`) and was written at or after the group's `at`. That is the replacement PB3152
prescribes (a report's dispatch is its slug, which names the ledger group and its notes), so the two notes no longer
prescribe different predicates. PB3152 keeps the `Report.ids` mention readers (`newest`, `attempts`). Residual: an
earlier report of a reused slug restamped after a new dispatch still answers it.

**Sweep.** `prune_worktrees.py` reads a report's mtime only as its age (the 24-hour DECISION staleness);
`land_lane.py` and `next_unit.py` read neither a report's mtime nor `Group.continues`.

**Tests.** `scripts/orchestrator/test_plan_wave.py` (214 checks): a FINDINGS branch IN FLIGHT plans no finisher in
either mode and DECISION plans one; a SPLIT branch IN FLIGHT is still planned; a reviewer's stamp on another
dispatch's report, a header that mentions the note and an earlier dispatch's report of the slug leave both the hold and
the file set; the group's own report releases both; an unreadable report answers nothing. Each fails against the old
rule (mutated and run). The old PB2795 test now uses a real report file with a post-dispatch mtime.
