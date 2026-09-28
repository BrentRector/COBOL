# 2026-09-28 — Enforcing "rewrite STATUS.md after every commit": two sandbox A/Bs, both null for staleness

**Note:** `kb/Work/PB1701`. **Raw data:** `2026-09-28-status-guard-ab.json`. **Hook under test:** `status_guard.py`
(orchestrator scratch `status-guard/`, to be published in the public agent-fleet skill).

## Question

The stamped-handoff scale test (`2026-09-28-stamped-handoff-scale.md`) found 3 of 14 finished real branches (21 %)
ending with a STATUS.md that did not describe their own last commit, with no crash involved. The owner asked: "Add
the rewrite-status-after-every-commit practice and test it."

## Hypotheses

1. A hook that enforces the rule lowers the stale-at-exit rate compared with the written rule alone.
2. (Added after run 1.) The real templates' wording, "after every mechanism and every gate", is the cause, because
   it does not cover a trailing small commit. "After every commit" wording would close the gap.

## Design

- **Sessions.** Fresh headless `claude -p` Opus sessions, one per sandbox git repo. Headless sessions are used because
  hooks load at session start, so in-session subagents cannot be split into arms by hook configuration.
- **Task (`template2`).** Nine seeded bugs across three modules, with tests. A gate script goes RED after the tests
  pass (a stray `print`, missing docstrings, CHANGELOG entries), which invites the trailing gate-fix commits seen in
  real branches. A long PROTOCOL.md carries the STATUS rule as item 5.3 among 30 others. The brief is only "read
  PROTOCOL.md, make the suite pass and the gate GREEN".
- **Outcome.** STATUS.md's stamp equals HEAD at exit, and the count of undescribed commits.
- **Also measured.** Commits, tests passing, turns, dollar cost, and hook interventions counted in each session
  transcript.

## Attempts

1. **Smoke run (15-turn toy task, `template`).** The GUARD arm failed to start because the `--settings` path was
   relative and each session ran from its own sandbox (a runner bug; fixed). On rerun, both arms complied. The toy
   was judged too easy to reproduce the failure, and the task was rebuilt as `template2`.
2. **Run 1: RULE vs GUARD, "after EVERY commit" wording, n = 10 each.** Both arms ended with stamp = HEAD 10/10 and
   tests 10/10.
   - The transcripts showed that the GUARD hook had MOSTLY NOT RUN. It was registered with `if: Bash(git commit*)`,
     but agents commit via `git add -A && git commit …`, and five of ten wrote their own checkpoint scripts
     (`sh .git/ckpt.sh`) that commit and rewrite STATUS.md in one step. A command-text filter sees neither.
   - The hook was changed to register on every shell command and to judge PostToolUse by STATE (stamp against HEAD)
     rather than command text. The self-test grew to 15 cases, all passing.
3. **Run 2: old wording ("after every fix and every gate run"), without vs with the fixed hook, n = 10 each.** Both
   arms ended with stamp = HEAD 10/10 and tests 10/10. The fixed hook did run: it issued 83 post-command reminders
   over 10 sessions (0–36 per session) and refused one session's commits twice. That session had tried to commit
   while its STATUS.md was stale; the hook forced the rewrite first. There were no stop blocks.

## Result

| arm | wording | hook | stamp = HEAD at exit | tests | mean turns | mean cost |
|---|---|---|---|---|---|---|
| RULE | every commit | none | 10/10 | 10/10 | 31.9 | $0.61 |
| GUARD (mis-registered) | every commit | mostly did not run | 10/10 | 10/10 | 34.5 | $0.68 |
| OLDRULE | every fix / gate | none | 10/10 | 10/10 | 30.5 | $0.58 |
| OLDGUARD | every fix / gate | fixed, state-based | 10/10 | 10/10 | 36.7 | $0.69 |

- **Staleness at exit: 0 of 40 in every arm.** Neither hypothesis could be tested, because the sandbox did not
  reproduce the failure. Hypothesis 2 is NOT supported: the old wording was followed 10/10.
- **Cost of the state-based hook:** about +20 % turns and +19 % dollars against its own control arm (OLDGUARD vs
  OLDRULE). It comes mostly from post-command reminders, which fire in the ordinary window between a commit and the
  agent's next-step rewrite. n = 10, so this is indicative only.
- **One real catch.** In 1 of 10 sessions the hook refused a commit made while STATUS.md was stale, so an
  intermediate staleness was prevented. The session would probably have ended compliant anyway.

## Limits

- **The sandbox does not reproduce the real failure rate** (21 % in real branches, 0 % here). Real sessions run 100–220
  turns with long gate waits, compaction, deaths and restarts. Sandbox sessions ran 13–58 turns.
- Only one task, one model (Opus 5.5) and n = 10 per arm.

## Decision (recommended to the owner)

- **Deploy the cheap parts:**
  - PreToolUse (refuse a commit while STATUS.md is stale);
  - Stop / SubagentStop (refuse to finish while it is stale).
  Both fire only on an actual violation.
- **Drop the per-command PostToolUse reminder.** It is the source of the cost and noise.
- **Change the template wording to "after every commit"** anyway. It is the owner's stated practice, and it costs
  nothing.
- **Measure the real effect in production:** the stale-at-finish rate over waves 71+ against the 21 % baseline. The
  sandbox cannot answer it.
