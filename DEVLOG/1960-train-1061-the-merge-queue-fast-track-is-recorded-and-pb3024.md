## Entry 1960 — 2026-10-11 13:38 PDT — Train 1061: the merge-queue fast track is recorded, and PB3024 phase 1 is unblocked

Train 1061 is a blocking one-cluster landing (the operator's steer under the owner's 12:58 PDT fast track, an exception
to PB2981's trains of four to six): cluster N, lead PB3015. It carries only the commits on `pipeline-analysis-PB3015`
after `d2795ca82`, which train 1060 landed as `3915e4ad9`. No other branch rode with it: the records branch from train
1060 and the skills pin are held by the operator, and the pin lands with PB3024 phase 1.

**N — PB3015 Q5 + PB3016, PB3022, PB3023, PB3024, PB3025, PB3027, PB3029: the merge queue moves to the front of the
campaign.** The owner asked at about 12:58 "Why are we still using push-main? I thought we queued PRs to github and it
did the CIs merges to main?". Nothing of the new pipeline was implemented yet, because the notes were still on this
branch. Asked whether to bring the merge queue forward, the owner answered "Fast track now (Recommended)". PB3015
records it as its sixth decision. PB3024 is unblocked and split into two phases. Phase 1 lands today: the
`merge_group` trigger, the committed ruleset with `ruleset.py apply|check`, the lander opening and queueing a pull
request per train by hand instead of running `push-main.sh`, the deletion of `push-main.sh` and the landing lease, and
the doc sweep. That landing goes through `push-main.sh` one last time, and then the owner gives an explicit go to apply
the ruleset. Phase 2 is PB3023 (`enqueue.py` replaces the hand steps) and PB3025 (one DEVLOG file per entry), each
through the queue. DESIGN-test-build-ci §3.17.6's cutover bullet says the same. PB3016 is now blocked by PB3023, which
resolves it by removal. PB3022, PB3023, PB3024, PB3025, PB3027 and PB3029 carry NEEDS-OPUS, so `model_rules.json`
routes them to Opus; PB3026 stays Sonnet work. No note changed `status`. Four notes conflicted with main (PB3015,
PB3022, PB3023, PB3027): train 1060's landing had written their design-section references as "DESIGN §3.x" so the
doc-citation audit does not read them as ISO clauses. Each conflict kept main's form and the delta's additions. The
lander's review found two ordering statements the delta had missed, and fixed them in N's own commit. PB3024's "What
changes" item 1 still said "PB3023 and this note are ONE landing" and "the lander asks the owner". PB3015's campaign
list still had PB3024 "(after PB3023)". The citation audits and `work.py check` pass.

**The train.** This landing gated the owner's 12:58 fast-track way (PB3015 Q1a applied early, by the operator's steer):
the Windows `build-local -Mode implementer` gate (leg 1) plus the WSL Linux gate, not the lander's whole-population
gate. CI runs the whole population. The first pair of gate runs went red, and both reds were the lander's own doing: it
amended N's commit with the review fixes while the gates were still running. On Linux every leg was GREEN, but the
`repository` check saw HEAD move (`a2bf5cb8c` to `75606a170`). On Windows the self-test
`test_orchestrate_ledger.ps1` failed its check 'a published ledger is not announced again'. That test runs the loop
against the real repository, and `ledger_state.py` stamps a ledger with the last commit touching `kb/Work`. The amend
moved that stamp between the test's `mark-published` and its check. Both gates were re-run on the final head with no
edit in flight. Semgrep did not move (`verify.py` PASS). The GAP did not move either: no inventory
row changed and no diagnostic code was claimed.

