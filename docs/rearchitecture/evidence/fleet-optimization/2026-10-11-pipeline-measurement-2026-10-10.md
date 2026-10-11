# 2026-10-11 — where the landing pipeline's hours and tokens went on account 2's first day (2026-10-10)

> ⛔ FROZEN EVIDENCE. The measurement below is the read-only record a `cobol-locator` agent produced on 2026-10-11 for
> kb/Work PB3015; it is never edited to stay current. The companion `2026-10-11-pipeline-measurement-2026-10-10.json`
> holds the eight unit rows verbatim from `E:\COBOL-coord\units.jsonl`.

**Question.** The owner, 2026-10-11 ~10:12 PDT: "The concurrent Opus session is running but seems to be having
significant difficulties actually getting changes actually made, tested, and pushed to main"; ~10:20: "Yesterday, it
used 13% of its weekly quota without delivering a single update to main"; ~10:38: "We are free to completely
rearchitect the build pipeline as needed to be maximally productive and minimally costly." Where did the 8 orchestrator
units of 2026-10-10 10:00 → 2026-10-11 01:50 PDT spend their 917 minutes and $262?

**Hypothesis (stated before the measurement, from `units.jsonl` and `train-measurements.jsonl` alone).** Most lander time
goes to whole-population gate runs and to reds induced by host load, not to the fixes the trains carry.

**Design.** One read-only measurement agent (cobol-locator, Sonnet, 56 tool calls, 19.4 min) over the unit transcripts,
their subagent transcripts, the gate logs, the surviving `verdict.json` files, the landing-lease log and the train
measurements; six tables with a source column per cell; a cell that could not be measured is `UNMEASURED` with the
reason. The brief: `scratchpad/pipeline-measure/brief-locator-pipeline-measure.md` (Mythos session 3bb68bc5).

**Attempts.** One run; no re-scoring. The agent's parsers (`parse.py`, `tok.py`, `gl2.py`, `gl3.py`, `impl_gates.py`,
`decomp.py`, `decomp2.py`, `agg.py`) lived in its scratchpad and are not preserved; the tables name their sources so
every number can be re-derived.

**Result.** The six tables below. Headline: 567 lander minutes, of which 233 (41 %) inside Windows/WSL gate runs and 94
(16.5 %) inside push-main + CI; 14 lander RED events, 8 containing a load or timing failure; wave 1051 spent 246 of 294
gate-minutes on gates that ran no test leg; CI ran the same population on both OSes in 13 minutes on 20 runners.

**Limits.** Per-agent USD, lease wait as a timestamp, CI wait separate from push-main, and the implementers of other
sessions' waves (w1051v/w/y/z, w1052, w1053) are UNMEASURED; the time decomposition credits an interval to the last tool
call, a proxy, with the gate and push-main log windows overriding it.

**Decision.** Pending: kb/Work PB3015 (status `owner`), question Q1 to the owner on 2026-10-11.

**Links.** kb/Work PB3015 (analysis), PB3016 (defect found the same morning), PB2515, PB2537, PB2602, PB2882, PB2914;
CI run 38125180249 (train 1057's head, 13 min wall); the operator's landings 2026-10-10: trains 1052, 1053, 1056,
1055, 1056b, 1057.

---

# Pipeline measurement: 2026-10-10 10:00 to 2026-10-11 01:50 PDT (8 loop units)

READ-ONLY measurement. All times PDT. A cell I could not measure is UNMEASURED with the reason.

## Sources (the footnote column cites these ids)

- S1 `E:\COBOL-coord\units.jsonl` (8 records with started_at in the window; fields duration_s, cost_usd, calls, cache_read, handoff_outcome).
- S2 Session transcripts `C:\Users\brent\.claude-acct2\projects\E--COBOL\<session_id>.jsonl` and `<session_id>\subagents\**\agent-*.jsonl` (+ `.meta.json` for agentType/description). Turns = distinct assistant message ids; tool calls = tool_use blocks. Parser: `scratchpad\pipeline-measure\parse.py`, `tok.py`.
- S3 Gate/push logs `E:\COBOL-coord\scratch\<train dir>\*.log`: run wall = file creation time to last-write time (Windows ctime/mtime). Checked against S8: 1055 gate1 log 13.1 min vs verdict.json wall_s 787.8 (=13.1); 1056b gate1 8.6 vs 517.8 s; 1056b gate3 7.4 vs 445.1 s; 1057 7.6 vs 456.5 s. Scripts `gl2.py`, `gl3.py`, `impl_gates.py`.
- S4 `E:\COBOL-coord\landing-lease.jsonl` (held_min). S5 `E:\COBOL-coord\train-measurements.jsonl`.
- S6 Each lander's final report (last assistant message in its S2 transcript) and `E:\COBOL-coord\scratch\reports\t10*-lander-report.md`.
- S7 `E:\COBOL-coord\logs\<stamp>-<unit>.handoff.json` / `.milestones.jsonl`.
- S8 `E:\COBOL\.claude\worktrees\*\TestResults\build-local\*\verdict.json` (only 18 still exist).
- S9 Time decomposition scripts `decomp.py`, `decomp2.py`, `agg.py` (step attribution rule stated under table 5).

## Table 1. Per unit (8 rows)

| # | unit | start | wall min | cost USD | model turns (main) | tool calls (main) | subagents (count: roles) | outcome | src |
|---|---|---|---|---|---|---|---|---|---|
| 1 | wave | 10-10 10:18 | 171.8 | 78.06 | 84 | 88 | 9: 8 cobol-implementer, 1 cobol-lander (lander-train1051, 0.7 min, stopped on STOP-loop) | split: STOP-loop at 12:30; nothing landed; B,F DONE; A,C,D,E SPLIT; G,H never started | S1,S2,S7 |
| 2 | land | 10-10 13:22 | 231.0 | 49.27 | 86 | 92 | 8: 4 cobol-lander (train 1052 phases 1a, 1b, 2, resumer), 4 general-purpose review shards | done: train 1052 (13 clusters) GREEN-gated, push-main blocked once (exit 3), landed 17:07:41 | S1,S2,S5,S6 |
| 3 | resume | 10-10 17:16 | 8.0 | 2.76 | 47 | 51 | 0 | done (kb DECISION rows) | S1,S2 |
| 4 | land | 10-10 17:24 | 116.6 | 24.12 | 81 | 81 | 3: 1 cobol-lander (train 1053), 2 review | done: train 1053 (5) landed 18:46 | S1,S2,S5 |
| 5 | wave | 10-10 19:20 | 181.9 | 48.21 | 92 | 95 | 9: 8 cobol-implementer (A-H), 1 cobol-lander (train 1056) | done: train 1056 (5 clusters) landed 22:18; A,G,H DONE held | S1,S2,S7 |
| 6 | land | 10-10 22:22 | 111.2 | 39.08 | 71 | 73 | 6: 2 cobol-lander (train 1055 first + resumer), 4 review | done: train 1055 (6) landed 00:07 | S1,S2,S5 |
| 7 | land | 10-11 00:14 | 63.2 | 15.78 | 51 | 51 | 2: 1 cobol-lander (train 1056b), 1 review | done: train 1056b landed 01:11, 5 of 6 (A ejected) | S1,S2,S5 |
| 8 | land | 10-11 01:17 | 33.7 | 5.13 | 35 | 36 | 1: cobol-lander (train 1057) | owner-question handoff; train 1057 (4) landed 01:47:58 | S1,S2,S5 |
| | **total** | | **917.4** | **262.41** | 547 | 567 | 38 | 6 trains, 38 clusters on main (13+5+5+6+5+4); first commit on main 17:07:41, 6 h 49 min after unit 1 started | S1,S5 |

Notes: units.jsonl `calls`/`cache_read` for the two wave units equal the main thread only (84 and 92 calls match S2 main turns), while for land units they include the landers (unit 2: 121.99 M = my main 15.36 M + subagents 106.63 M). cost_usd is as recorded in S1; per-agent USD is UNMEASURED (the telemetry sink `usage_report.py` is bounded by UTC day, mixes both accounts, and cannot be cut to a unit: header showed 9,884 events for 10-10/10-11 across 2 accounts).

## Table 2. Per lander (all cobol-lander subagents in these units)

Turns = assistant messages (S2). Train 1052 note (S5) counts the phases as 60/105/195/95 "turns"; my counts are 67/150/179/81 (the difference is the counting rule, not measured further). Gate times from S3. Lease wait: UNMEASURED as a timestamp (the lease log records acquire/release only); every lander report says the lease was acquired with no wait (S6), so 0 min stated, not measured.

| lander (agent id) | unit | wall min | turns / tool calls | Windows gate runs (min, verdict) | Linux gate runs (min, verdict) | turn cap / SPLIT | lease held min | first gate start to push-main exit | push-main + CI wait min | src |
|---|---|---|---|---|---|---|---|---|---|---|
| 1052 phase 1a (a2433084d) | 2 | 6.6 | 67 / 110 | none | none | no (PHASE-1A-DONE: applied 7 clusters) | n/a | n/a | 0 | S2,S6 |
| 1052 phase 1b (abd9b15e3) | 2 | 44.3 | 150 / 184 | none | none | no (PHASE-1B-DONE: applied 6 clusters, 2 PublicAPI conflicts) | n/a | n/a | 0 | S2,S6 |
| 1052 phase 2 (a243e819e) | 2 | 117.3 | 179 / 232 | run1 14:34-15:27 = 53.1 RED; run2 16:07 = 0.4 aborted (no verdict) | run1 14:33-14:39 = 6.0 RED; run2 16:07-16:14 = 7.2 RED | SPLIT at about 195 turns | 106.1 (14:32-16:18) | 14:34 to 17:09 (with resumer) = 154.9 | 0 | S2,S3,S4,S6 |
| 1052 resumer (a1bf27aa8) | 2 | 31.9 | 81 / 95 | run3 16:23-16:31 = 8.7 GREEN | run3 16:35-16:39 = 3.5 GREEN | BLOCKED: push-main exit 3 (12 landing-check stops) | 27.6 (16:20-16:48), then push-main.sh by the land unit 15.5 (16:53-17:09, exit 0) | (see above) | 0.4 blocked + 15.5 landed | S3,S4,S6 |
| 1053 (a44b6a644) | 4 | 81.2 | 196 / 224 | run1 17:40-17:52 = 11.7 GREEN; run2 18:04-18:18 = 13.5 RED; run3 18:19-18:31 = 11.3 GREEN | run1 5.1 GREEN; run2 5.1 GREEN | no | 71.0 (17:35-18:46) | 17:40 to 18:46 = 66.1 | 14.7 | S2,S3,S4,S6 |
| 1056 (a1ac546d3, in wave unit 5) | 5 | 102.5 | 156 / 188 | run1 20:42-21:09 = 27.4 RED; run2 21:21-21:46 = 25.0 RED (verdict RED from self-tests only, 0 failing cases) | run1 20:41-20:48 = 6.5 RED; run2 21:21-21:31 = 9.7 GREEN | no | 87.4 (exit 1, 20:40-22:07) + 7.1 (exit 0) | 20:42 to 22:18 = 96.5 | 9.5 (CI RED) + 7.0 (rerun GREEN) = 16.5 | S2,S3,S4,S6 |
| 1055 first (ae6fcaa30) | 6 | 44.0 | 220 / 280 | run1 22:32-22:45 = 13.1 RED | run1 22:32-22:37 = 5.1 RED | HIT 220-turn cap; fresh lander took over | 97.4 across both (22:30-00:07) | 22:32 to 00:07 = 95.6 | 0 | S2,S3,S4,S5 |
| 1055 resumer (a670ff267) | 6 | 55.3 | 133 / 153 | run2 23:14-23:27 = 13.5 RED; run3 23:34-23:46 = 11.9 GREEN | run2 5.1 GREEN; run3 5.4 GREEN | no | (above) | (above) | 15.8 | S2,S3,S4,S6 |
| 1056b (aae7969bf) | 7 | 54.8 | 170 / 196 | run1 00:22-00:31 = 8.6 RED; run2 00:33-00:40 = 7.4 RED; run3 00:45-00:52 = 7.4 GREEN | run1 5.5 RED; run2 5.5 GREEN; run3 5.5 GREEN | no (cluster A ejected) | 48.9 (00:22-01:11) | 00:22 to 01:11 = 48.7 | 15.3 | S2,S3,S4,S6 |
| 1057 (a39944ea2) | 8 | 29.3 | 69 / 80 | run1 01:22-01:30 = 7.6 GREEN | run1 5.5 GREEN | no | 25.6 (01:22-01:48) | 01:22 to 01:48 = 25.4 | 15.4 | S2,S3,S4,S6 |
| **total (10 landers)** | | **567.2** | | 15 runs, 8 RED, 1 aborted | 14 runs, 5 RED | 1 cap, 1 SPLIT, 1 BLOCKED | | | 93.6 | |

Gate-run internals that survive as verdict.json (S8, lander mode, whole population in one leg), seconds:

| run | verdict | wall | build | legs total | audits | Conformance / Unit / Characterization |
|---|---|---|---|---|---|---|
| 1055 gate1 (053150Z) | RED | 787.8 | 25.5 | 743.4 | 763.6 | 743.2 / 455.1 / 16.9 |
| 1055 gate2 (061343Z) | RED | 808.9 | 38.6 | 751.5 | 773.5 | 751.3 / 431.3 / 24.2 |
| 1055 gate3 (063356Z) | GREEN | 712.6 | 23.0 | 675.6 | 693.7 | 675.4 / 365.1 / 13.0 |
| 1056b gate1 (072223Z) | RED | 517.8 | 36.6 | 460.5 | 437.3 | 460.3 / 323.2 / 18.8 |
| 1056b gate2 (073233Z) | RED | 446.3 | 7.1 | 427.3 | 394.7 | 427.1 / 335.8 / 10.7 |
| 1056b gate3 (074500Z) | GREEN | 445.1 | 25.9 | 403.7 | 386.6 | 403.6 / 312.0 / 10.6 |
| 1057 (082237Z) | GREEN | 456.5 | 49.7 | 384.8 | 395.2 | 384.6 / 295.9 / 24.5 |

The Conformance leg is the longest leg in all seven runs (384-751 s); the audits (self-tests) run beside the legs and take about as long as the legs (387-774 s).

Minutes waiting for CI as a separate figure: UNMEASURED apart from the push-main window above (push-main.sh pushes and waits for CI in one process; the log records no separate wait timestamps).

## Table 3. Per red (every RED across landers and implementers in these units)

Cost = minutes from the RED verdict (log last-write) to the next GREEN verdict of the same platform (or to acceptance/ejection). Attribution is the lander's own (S6, S5), not mine.

### Landers (14 RED events: 8 Windows, 5 Linux, 1 CI)

| train / run | failing test(s) | lander attribution | action | cost min | src |
|---|---|---|---|---|---|
| 1052 Win run1 (53.1 min run) | GateLegDriftTests.Arm3_TheGateDriver_FiresEveryArmOnPlantedInputs; GateSlotDriftTests.SelfTest_DrivesAndPassesEveryArm; SelfTestDiscoveryDriftTests.TheRunner_ProvesEveryArm | first two: harness 120 s limit under heavy host load (PB2882; 46 s and 40 s run quietly); third: cluster 51V (two expected arm texts out of date) | fixed in train (ff36e5aaa); load reds re-run quiet | 64.5 (15:27 to GREEN 16:31) | S3,S6 |
| 1052 Linux run1 | CorpusRunnerTests_P6 (golden 2014/pb2764_locale_text_normalized); LocaleTextNormalizationDriftTests.ArabicAndSpanishTimes_AreTheDocumentedBytes; SelfTestDiscoveryDriftTests | cluster 52F (es-US AM/PM text differs Windows vs Linux); cluster 51V | fixed in train (cce7a6af3, ff36e5aaa) | 119.6 (14:39 to Linux GREEN 16:39, includes run2) | S3,S6 |
| 1052 Linux run2 | SidecarColumnLockHostDriftTests.TwoSimultaneousExclusiveTakes_NeverBothHold (10 of 10 reruns) | caused by the lander's own review fix cc42e84c8 for 52B | fix withdrawn (8dd7c0f88); Windows run2 aborted | 24.7 | S3,S6 |
| 1053 Win run2 | GateSlotDriftTests.SelfTest_DrivesAndPassesEveryArm | environmental: harness saw no process finish (two 120 s timeouts) while Linux gate used the machine; passes alone in 37 s; no cluster touches gate_slot.py | re-ran; attributed in 2 min | 13.1 | S3,S5,S6 |
| 1056 Win run1 | pb156_float_literal_exact_native (CorpusRunnerTests_P7); AnnexA1RegisterDriftTests; SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree; GateLegDriftTests.Arm3; GateSlotDriftTests.SelfTest; 1 script self-test (test_orchestrate_startup.ps1, 1010 s) | C: pb156 lane; D: two drift tests; the gate/self-test items are host-load timeouts | C and D fixed in train; load items not fixed | 37.2 (21:09 to run2 verdict 21:46; run2 had no failing case) | S3,S6,S5 |
| 1056 Linux run1 | AnnexA1RegisterDriftTests; SpecTraceabilityInventoryDriftTests; CorpusRunnerTests_P7 | clusters C and D (same as above) | fixed in train | 42.9 (20:48 to Linux GREEN 21:31) | S3,S6 |
| 1056 Win run2 | none failing; SELF-TESTS RED: landing_lease, worktree_rm_allow, forbidden_commands, alloc, dispatch_guard, gate_slot self-tests timed out/RED at 171-683 s | host load; train touches no script; each passes alone | lander accepted, no rerun | 11.3 (to push start) | S3,S6 |
| 1056 CI run1 (CI run 38113267204) | SidecarColumnLockHostDriftTests.TwoSimultaneousExclusiveTakes_NeverBothHold | known open race PB2917; no cluster touches the lock | `gh run rerun --failed`, green | 11.1 (22:07 to 22:18) | S3,S6 |
| 1055 Win run1 (first lander) | StandardClassBaseTests x2 (Universal/Factory); pb1760 (CorpusRunnerTests_P9); AnnexA1RegisterDriftTests; GateSlotDriftTests.SelfTest; DRIFT RULES INDEX | C 2, D 2, interaction 1 (drift index), GateSlot timing (PB2882) | all fixed in train; GateSlot by root fix (01e02b208) | 60.9 (22:45 to Win GREEN 23:46) | S3,S5,S6 |
| 1055 Linux run1 | FileAuthorityPresenceTests.ResolveHostPath(length 40000); AnnexA1RegisterDriftTests; StandardClassBaseTests x2; pb1760 | D (Linux-only one), C, D | fixed in train | 41.8 (22:37 to Linux GREEN 23:18) | S3,S6 |
| 1055 Win run2 (resumer) | GateSlotDriftTests.SelfTest (timed out twice at 120 s; 314.9 s under load, 9.2 s alone); WITNESS LOSS GR-16.2.2.2-1 | timing (PB2882) and cluster C (renamed test hand-edited in an inventory row) | root fix for the test; witness retirement via record_verdicts | 18.6 | S3,S6 |
| 1056b Win run1 | SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest | cluster C renamed a drift test, leaving an inventory row pointing at a missing test | fixed in train (record_verdicts batch) | 21.4 (00:31 to GREEN 00:52) | S3,S6 |
| 1056b Linux run1 | same EveryTestRef test; selftests-built NOT RUN | cluster C; plus a race on main (guard leg wrote temp files inside the clone) | fixed in train (PB2989) | 10.2 | S3,S6 |
| 1056b Win run2 | SidecarColumnLockHostDriftTests.ManyTestersSweepingOneDeadFileNeverRefuseEachOther; WITNESS LOSS | timing red in cluster A (probe gives up after 5 x 1 ms) plus a separate review defect in A | cluster A EJECTED; C witness fixed | 12.4 | S3,S5,S6 |

Of the 14 lander RED events, 8 include at least one failure the lander attributed to host load or a timing race (1052 Win1, 1053 Win2, 1056 Win1, 1056 Win2, 1056 CI, 1055 Win1, 1055 Win2, 1056b Win2), and 6 are cluster, merge-artifact or infrastructure reds only (1052 Linux1, 1052 Linux2, 1056 Linux1, 1055 Linux1, 1056b Win1, 1056b Linux1).

### Implementers (gate verdicts read from S3 `*gate*.log` in each implementer's scratch dir; Linux and baseline logs excluded)

| group | gate runs | RED, NO LEG RAN (SELF-TESTS / DRIFT / DOC CITATIONS red before any leg) | RED leg 1 (failing test) | GREEN leg 1 only | cost of first red to first GREEN |
|---|---|---|---|---|---|
| wave 1051 A-H (unit 1) | 18 runs, 294.0 summed gate min | 13 runs, 246.2 min (83.7% of the group's gate minutes) | 2: w1051a gate3 (EmittedCSharpSnapshotTests, DocumentedNonSupportWitnessTests, CorpusRunnerTests_P20); w1051f gate2 (RunUnitStateDriftTests.NoStaticMutableCollection_OutsideTheDocumentedProcessStores) | 3 (B, D, F) | B 88.8, D 79.7, F 17.8; A, C, E never reached GREEN |
| wave 1056 A-H (unit 5) | 10 runs, 173.7 summed gate min | 1: w1056c gate1 DRIFT RULES INDEX RED, 30.1 min | 1: w1056d gate1 (ClassClauseForPhraseTests.MoreCharactersThanTheInSet_IsSR17_5; InitialStateBackgroundDriftTests.Sr1Admits...) 32.1 min | 8 | C 4.8, D 6.5 |
| w1051 v/w/y/z (other session, landed in unit 2) | 8 runs | 4 (78.2 min) | 0 | 3 | UNMEASURED per branch |
| w1052 a-h (other session, landed in unit 2) | 18 runs | 9 (173.1 min) | 2 (RunUnitStateDriftTests; RuntimeAbiPinDriftTests) | 7 | UNMEASURED per branch |
| w1053 a-p (other session, landed in unit 4) | 25 runs | 19 (430.3 min) | 1 (RuntimeAbiPinDriftTests) | 3 | UNMEASURED per branch |

Cause of the wave-1051 NO LEG RAN reds (S7 milestones, 12:24 filing of PB2914): every wave gate ended SELF-TESTS RED because `scripts/self_tests.py` gave each script self-test a fixed 900 s limit while N gates ran their self-test phases at once (landing_check self-test alone took 955 s). PB2914 landed in train 1052 at 17:07; the wave-1056 gates (19:40 onward) had 1 of 10 runs NO LEG RAN.

## Table 4. Per implementer (wave units 1 and 5)

Wall = first to last timestamp of the agent's transcript (S2). Gate runs from S3 (letter-to-directory mapping by worktree name wf_4151e8da-91b-N / wf_8f6c2cfa-5a7-N; the implementer's own other test runs are not counted). "NO LEG RAN" means every gate run it made ended before any leg.

| unit | agent | group | wall min | turns / tools | gate runs and verdicts | gate ever ran a leg | outcome | src |
|---|---|---|---|---|---|---|---|---|
| 1 | a3d386396 | A PB2695 | 158.3 | 216 / 265 | 21.8 RED-NLR; 17.7 RED-NLR; 8.9 RED leg1 | yes (RED) | SPLIT | S2,S3,S7 |
| 1 | acf52637e | B PB2843 | 135.2 | 238 / 283 | 15.3 RED-NLR; 22.8 RED-NLR; 19.0 GREEN | yes | DONE | " |
| 1 | ac64c5f6c | C PB2834 | 135.5 | 201 / 250 | 12.7, 20.2, 22.5 all RED-NLR | NO LEG RAN | SPLIT | " |
| 1 | a5ad14388 | D PB2588 | 154.4 | 212 / 263 | 19.5 RED-NLR; 23.7 RED-NLR; 6.0 GREEN | yes | SPLIT | " |
| 1 | a636c9a52 | E PB2833 | 148.4 | 196 / 235 | 15.2, 18.6, 24.4 all RED-NLR | NO LEG RAN | SPLIT | " |
| 1 | a0a40a44c | F PB2846 | 158.5 | 235 / 310 | 11.8 RED-NLR; 7.6 RED leg1; 6.3 GREEN | yes | DONE | " |
| 1 | a4285919 | G PB1688 | 2.9 | 5 / 7 | none | n/a | never started (STOP-loop) | " |
| 1 | ae8d11f7d | H PB2463 | 1.3 | 5 / 6 | none | n/a | never started (STOP-loop) | " |
| 5 | a453e8bcc | A PB2917 | 105.8 | 159 / 221 | 19.9 GREEN | yes | DONE (held; later ejected in train 1056b) | S2,S3,S7 |
| 5 | ae6dc2a14 | B PB2920 | 31.7 | 84 / 107 | 15.3 GREEN | yes | DONE (landed 1056) | " |
| 5 | ab52ca72c | C PB2918 | 68.4 | 130 / 151 | 30.1 RED-NLR (DRIFT RULES INDEX); 3.1 GREEN | yes | DONE (landed 1056) | " |
| 5 | ac02ded40 | D PB2919 | 68.2 | 132 / 157 | 32.1 RED leg1; 3.0 GREEN | yes | DONE (landed 1056) | " |
| 5 | a96a2c371 | E PB2938 | 67.9 | 116 / 127 | 26.4 GREEN | yes | DONE (landed 1056) | " |
| 5 | a031596eb | F PB2818 | 63.7 | 122 / 154 | 30.8 GREEN | yes | DONE (landed 1056) | " |
| 5 | a7dc57f18 | G PB2840 | 70.6 | 136 / 148 | 10.3 GREEN | yes | DONE (held; landed 1056b as PB2840/PB1938) | " |
| 5 | a9c5fd614 | H PB1938 | 30.8 | 64 / 94 | 2.7 GREEN | yes | DONE (held; landed 1056b) | " |

Implementer wall total: unit 1 = 894.5 min (8 agents), unit 5 = 507.1 min (8 agents). The 6 wave-1051 agents that did work (A-F) made 18 gate runs, 6.0-24.4 min each (S3).

Surviving verdict.json for an implementer gate under fleet load (S8): wave-1056 A (`wf_8f6c2cfa-5a7-1`, run 034945Z): wall 1187.0 s, build 9.0, legs 45.0, audits 1053.8 (audits 88.8% of wall). Wave-1055 implementer (`wf_6f53c0b7-a74-3`, run 013805Z): wall 2241.1 s, legs 351.7, audits 1788.2 (79.8%). Quiet reruns of the same worktrees: 155.1 s and 191.2 s.

## Table 5. Time decomposition of each land unit (minutes)

Rule (S9): between consecutive transcript records of a lander, the interval is credited to the category of the last tool call it issued; the windows of the Windows and Linux gate logs (union) and of the push-main logs override that tagging. "fix reds" = test runs and Edit/Write of source; it is a proxy, not an accounting of fixes. "merge" = git apply/cherry-pick/merge/switch/commit. "other" = commands and model time not tagged. Phases 1a and 1b of train 1052 ran no gate; their 50.9 min are all applying clusters (the merge column undercounts them because the apply ran inside scripts tagged "other"). Lease wait 0 in every unit (reports say no wait). Review agents' spans are given in the last column; they overlapped gating except where noted.

| unit | wall | lander windows | merge/rebase | build | gating wall (gate-log union + monitoring) | fix reds | review (tagged) | DEVLOG / notes / ledger / oracle | push-main + CI | lease wait | other in lander | main thread outside landers | review agent spans |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 2 land 13:22 | 231.0 | 200.1 | 7.4 (+43.5 applying inside "other" = 50.9 for 1a+1b) | 6.3 | 90.3 (log windows 73.2) | 35.3 | 0 | 25.5 | 1.3 (+15.5 push by the unit main thread) | 0 | 38.2 | about 30 | 4 shards, 26-45 min each, 14:39-15:23, wholly inside the 14:34-15:27 Windows gate |
| 4 land 17:24 | 116.6 | 81.2 | 2.3 | 1.6 | 38.2 (36.8) | 4.5 | 0 | 10.7 | 18.2 | 0 | 6.5 | 34.5 | 2 reviews, 7.4 and 13.1 min, inside the lander window |
| 5 wave 19:20 (lander only) | 181.9 | 102.5 | 3.8 | 0.4 | 54.6 (52.7) | 4.1 | 0 | 13.5 | 18.7 | 0 | 7.5 | about 5 (rest is the implementer fleet) | none |
| 6 land 22:22 | 111.2 | 99.3 | 3.8 | 0.9 | 51.6 (38.8) | 7.0 | 0.6 | 15.6 | 15.9 | 0 | 11.7 | 10.9 | 4 reviews 5.3-8.7 min, 22:57-23:06, between gate1 (ended 22:45) and the resumer's gate2 (23:14): on the critical path |
| 7 land 00:14 | 63.2 | 54.8 | 0.8 | 0.0 | 25.4 (23.7) | 2.7 | 0 | 4.3 | 16.0 | 0 | 6.0 | 7.6 | 1 review 13.0 min, 00:22-00:35, over gate1 and gate2 |
| 8 land 01:17 | 33.7 | 29.3 | 0.5 | 0.0 | 8.6 (7.7) | 0.7 | 0 | 3.3 | 15.5 | 0 | 0.9 | 3.8 | none |

Unit 1 (wave, 171.8 min) and unit 5 (181.9 min) are fleet units: in unit 1 the implementers ran for 159.1 min (union of agent windows), the main thread outside agents 2.3 min; in unit 5 171.5 min and 5.6 min. Unit 3 (resume, 8.0 min): main thread only, 0 subagents.

Totals over the 10 landers (567.2 lander wall min): gate-log windows 232.9 min (41.1%), push-main + CI 93.6 min (16.5%).

## Table 6. Token decomposition (cache_read tokens, from S2 transcripts; message ids de-duplicated)

| unit | own thread cache_read | subagents cache_read | subagent share | detail by role (cache_read, turns) |
|---|---|---|---|---|
| 1 wave | 10,941,597 | 354,622,967 | 97.0% | 8 implementers 354,294,755 (1,308 turns, 270,868 per turn); lander 328,212 (7 turns) |
| 5 wave | 13,271,285 | 209,194,847 | 94.0% | 8 implementers 180,344,410 (943 turns, 191,245 per turn); lander 28,850,437 (156 turns) |
| wave units together | 24,212,882 | 563,817,814 | 95.9% | |
| 2 land | 15,360,341 | 106,626,242 | 87.4% | 4 landers 79,293,192 (477 turns); 4 reviews 27,333,050 (195 turns) |
| 3 resume | 4,939,831 | 0 | 0% | |
| 4 land | 12,875,516 | 49,624,492 | 79.4% | lander 40,390,572 (196 turns); 2 reviews 9,233,920 (84 turns) |
| 6 land | 10,731,266 | 96,718,786 | 90.0% | 2 landers 69,070,165 (353 turns); 4 reviews 27,648,621 (215 turns) |
| 7 land | 7,002,872 | 34,614,152 | 83.2% | lander 32,188,799 (170 turns); review 2,425,353 (23 turns) |
| 8 land | 3,512,117 | 7,363,654 | 67.7% | lander 7,363,654 (69 turns) |
| land units 2,4,6,7,8 together | 49,482,112 | 294,947,326 | 85.6% | landers 228.30 M (77.4% of subagent total); reviews 66.64 M (22.6%) |

Caveat: units.jsonl cache_read for the wave units is the main thread only; the 563.8 M subagent tokens of the two waves appear in no units.jsonl field. Unit 1 recorded $78.06 although its main thread read 10.9 M cache tokens, so its cost_usd evidently includes the workflow agents; I could not verify this from the files (UNMEASURED).

## What the numbers say

- 917.4 min and $262.41 across 8 units; the first commit reached main at 17:07:41, 6 h 49 min after unit 1 began. Unit 1 (171.8 min, $78.06, 8 implementers) landed nothing; 13 of its 18 implementer gate runs (246 of 294 gate minutes) ended in SELF-TESTS/DRIFT/DOC RED with no leg run.
- Six trains then put 38 clusters on main (1052: 13, 1053: 5, 1056: 5, 1055: 6, 1056b: 5 of 6, 1057: 4).
- Of 567.2 lander wall minutes, 232.9 (41%) were Windows/Linux gate runs (union) and 93.6 (16.5%) were push-main plus CI; train 1052's gate run 1 alone took 53.1 min.
- Quiet-host whole-population Windows gate runs took 7.4-13.5 min (the Conformance leg 384-751 s dominates); runs under concurrent load took 25.0-53.1 min.
- 14 lander RED events: 8 included a failure the lander attributed to host load or a timing race; 6 were cluster, merge-artifact or infrastructure reds. Per train, first RED verdict to final GREEN ranged 13.1 to 64.5 min.
- 4 of the 6 trains had a RED first whole-population gate run (1052, 1056, 1055, 1056b); 1053 and 1057 were GREEN first (S5 first_run).
- Wave 1056 gates (after PB2914 landed) ran a leg in 9 of 10 runs; wave 1051 gates ran a leg in 5 of 18.
- Lander turn limits: 1 cap hit (1055 first lander, 220 turns), 1 SPLIT (1052 phase 2), 1 push-main BLOCKED (12 landing-check stops, 27.6 min lease) released by the land unit.
- Subagents hold 95.9% of cache_read tokens in the wave units and 85.6% in the land units; within land units landers 77.4%, review shards 22.6%.
- UNMEASURED: per-agent USD; lease wait as a timestamp; CI wait separate from push-main; turn counts and wall for the w1051v/w/y/z, w1052 and w1053 implementers (their transcripts are outside the 8 sessions); first-red-to-GREEN for those branches.
