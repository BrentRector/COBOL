## Entry 1965 — 2026-10-11 20:23 PDT — Train 1066: the landing is measured per pull request (PB3028, PB3040) and the repository's move to WiseOwlSoftware is recorded (PB3078), through the merge queue

**Why two clusters.** The operator's steer (handoff.last.json, train 1066) names exactly these two, through the merge
queue: C2 (`worktree-wf_7ea6bc1f-05f-8` at `e29c15465`, report w1064c2-PB3028, reviewed CLEAN by w1064c2-review-r2 at
that head) and H (`repo-transfer-PB3078` at `d293fa06e`, report w1064h-PB3078, reviewed CLEAN by
hand-review-repo-transfer-PB3078-r3 at that head). F (PB3029) is held to land last and alone after A; A lands through A2;
the notes branch for PB3084 has findings. None of those is in this train.

**C2 — PB3028, also PB3040 (wave 1062 C2, finished in wave 1064 C2 after two review rounds).** `landing_measure.py`
replaces `train_measure.py` with one record per pull request: `enqueue.py queue --changes <k>` writes it (each change's
DONE, the enqueue time, the worktree's local gate seconds), `complete` reads the merge time, the bounces and their causes
and the merge-group CI wall from GitHub, `summary` compares the queue pipeline against the frozen 2026-10-10 train
records (its BEFORE column), and `freeze` keeps the first after-summary write-once. DONE is git's commit time of each
finished report's HEAD, the earliest in a finisher chain; round 2's review finding R1-1 made it skip every report whose
HEAD is already an ancestor of origin/main (as seed and as chain member, one cached ancestry check per HEAD), so a reused
lead or branch name never inherits an earlier landing's finish (the real-repository re-probe moved from 00:44:49 to
18:30:50). A train's changes come from its commits' `Report:` lines, and the ledger labels the trains' minutes as the
floor they are. `train_measure.py`, `TrainMeasureDriftTests` and every caller are deleted in the same change;
`LandingMeasureDriftTests` pins the arms, the deletion and the queue step's record. PB3040's plan (R1-2) now retires a
finished lander's ledger entry on its terminal report or its branch's merged `landing-measurements.jsonl` record, never
on the retired train file. The lander briefs' step 9, MANDATORY-PRACTICES, the land unit and DESIGN-test-build-ci.md
§3.17.5 follow. PB3028 is `landed` (`closes_rows: []`, process tooling); PB3040 stays open. No code, no golden. The
branch carried C (already on main) and train 1064's content; `train_apply.py` diffed from its old base `01f392184`, and
its one conflict, `DEVLOG.md`, held only entries main already has, so main's form was kept. The staged tree was checked
to equal the branch head plus train 1065's six paths.

**H — PB3078 (owner decision 2026-10-11 18:50).** GitHub offers the merge queue only to organization-owned repositories:
the second `ruleset.py apply` was refused with `Invalid rule 'merge_queue'` on the personal account's repository, so the
repository was transferred to `WiseOwlSoftware/COBOL` at 19:11 and the ruleset applied at 19:14 (`ruleset.py check`
ACTIVE): landing is `enqueue.py queue` from then on. The current-state references to the repository name are swept,
`session-probe.ps1` reads the repository from the remote like `push-main.sh` and `ruleset.py`, PB3024 is open behind
PB3023 with its remaining work restated, and `push-main.sh` with `landing_lease.py` and the `ci/**` trigger is retired by
PB3024's closing change after PB3023 lands. PB3078 flips `open` → `landed` in this landing (`closes_rows: []`, process
tooling); PB3024 stays open.

**The train.** `train_apply.py` GREEN (C2 resumed after the DEVLOG resolution; H 13 paths, 10 as on the branch, 3 merged
with earlier changes; citation audits and `work.py check` PASS). Lease taken at 03:12Z; origin/main was still
`79d760ab6`, so no rebase. Windows gate (leg 1, Normal priority, run 20261012T031242Z-ca1de4) `LEG 1 ONLY (batched
gating, PB2515): GREEN` (Conformance 4,816/11,488, Unit 32,585/33,450, Characterization 36/36; the three
`LandingMeasureDriftTests` passed in leg 1; leg 2 left to the merge group); Linux gate GREEN on the same head `a990e48f8`
(selftests, unit, characterization, conformance, guard). Arch oracle IDENTICAL (7,780 cases, baseline fb934cb4a329;
`landing_oracle.py` PASS, no re-record). Semgrep: no count rose (3, 44, 2, 268). GAP unchanged (no row re-verdicted).
Nothing ejected, nothing fixed in the train.

