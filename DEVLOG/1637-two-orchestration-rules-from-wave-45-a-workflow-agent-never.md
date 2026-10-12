## Entry 1637 — 2026-09-22 11:47 PDT — Two orchestration rules from wave 45: a workflow agent never ends its turn on a background job, and every dispatch carries a graceful STOP

**Measured, not guessed.** Wave 45 (the first Opus 5.5 wave, seven groups) came back with FIVE implementers and the
train-46 lander reporting "gate PENDING — still running when the result was forced". Every one of those logs stops
mid-leg with no verdict line, and `t46/gate.log`'s mtime is the minute the lander returned. The briefs said *run the
gate with run_in_background and STOP and wait for the harness notification*; a workflow agent that stops ENDS ITS
TURN, is returned by the harness, and its background process dies with it. Train 46 therefore did not land (nothing
was pushed; main was untouched) and a fresh lander resumed from its checkpoint.

- `workstream` SKILL.md §1: the rule — start the job in the background to a log, then BLOCK in the foreground on
  `timeout 580 bash -c 'tail -n +1 -f <log> | grep -m1 <verdict>'`, re-issued until the verdict prints; push-main gets
  a `PUSH-MAIN-EXIT=$?` sentinel appended to its log. `lander-train-brief.md` step 7's wait instruction corrected.
- `workstream` SKILL.md §1: the GRACEFUL STOP — workflow agents cannot be messaged, so every dispatch checks for a
  `STOP` file before each new step and returns SPLIT at a step boundary. Owner instruction 2026-09-22 ("attempt to
  not lose work due to a quota kill"), for a week in which the fleet deliberately runs near its quota limits.

The self-report problem recurs in a new form here: the lander read each implementer's gate log itself and found
cluster A's had no verdict and cluster D's hid a real red (`FileIoDifferentialTests.WriteAdvancingMnemonic_ZeroLineAdvance_RecordAlwaysReleased`)
behind a "still running" claim. The train gate is the real gate; the fresh lander is told to bisect that red if it recurs.

