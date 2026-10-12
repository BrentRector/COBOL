## Entry 1946 — 2026-10-08 22:26 PDT — Train 1047y: the session-start hook names the skills to load; note-t1047-carry recorded abandoned

**Cluster A (branch `abandon-t1047-carry`, the 22:24 land unit's record).** kb/Work/PB2708 records `note-t1047-carry`
ABANDONED: train 1047z carried its three commits (PB2844-PB2850 and PB2855, the train-1047 trend points). The operator
verified every added line on main (83192a33c) and deleted the branch and its worktree.

**Cluster B (branch `session-skills`, PB2861; owner 2026-10-08 ~22:05: "Be sure to setup all session start and resume
processes to always use these skills, as appropriate").** A resumed attended session (`claude --resume`) had been told
to load no skill: only `scripts/start-session.ps1`'s restart prompt and the loop units' prompts named `session-start`.
`scripts/hooks/session_start.py`, which runs at every start, resume, clear and compaction in every lane, now heads the
session's context with a SKILLS block chosen by the hook payload's `source` and the session's lane (`session_lane()`:
mailbox.py's lane rule; `loop:<type>` from `COBOL_LOOP_UNIT`; `solo` without a coordination directory): start, resume
or clear invoke `session-start` first, then the lane's skills (operator: `workstream` before it supervises, steers,
lands or dispatches, and arm its inbox watcher; Mythos: owner-approved tasks, `workstream` before any dispatch, arm its
watcher), then the work skills as work arises; a compaction keeps the loaded skills; a loop unit loads `session-start`
and its unit prompt's skills, and a `meter` unit skips `session-start` as its prompt says. `--self-test` (11 checks) is
discovered by `scripts/self_tests.py`, so every gate and CI runs it; the hook's top-level code moved under `main()`.
CLAUDE.md "Start here every session" gains step 0; DOC_INDEX's hook row says what the block does. PB2861 landed.

The skills themselves: a RED baseline (18 fresh agents, six scenarios from the day's lessons — cap hits, squash-landed
branch cleanup, relaunch state and quota, drain versus STOP, the mailbox after a restart, prohibition witnesses) passed
3/3 in every scenario with the current public `brent-tools` skills, so the public skills repo is unchanged
(superpowers:writing-skills: a control that does not fail means there is nothing to author). The one gap, the
`session-start` skill's list of probe lines, landed in train 1047z.

**Landed by the operator** (the loop ended at 22:24 on the weekly cap, 97.05 %; owner: finish in-flight items): two
branches in one train, the lander's whole-population gate, the Linux gate, push-main.

