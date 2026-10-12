## Entry 1754 — 2026-09-28 11:34 PDT — PB1703: the rolling wave deadlocked on a dead agent; status-guard A/Bs recorded; PB1701 decided

**PB1703 (landed).** The owner asked why the statusline showed a "rolling-wave" task running for 16 h 16 m that looked
finished. It was wave 68's workflow (`wf_e9a82a33-6b6`, task `w0pnnjh1q`), deadlocked:
- group U's agent died on an API error, so `agent()` REJECTED instead of returning null;
- the rejection escaped the worker in `wf_rolling_wave.js` with `running` still counted and no wake-up sent;
- U2, `after: U`, therefore never became eligible, and the parked worker waited forever.

The workflow was stopped with TaskStop. The worker now records a rejection as `NO-RESULT`, decrements `running` in
a `finally`, and wakes the other workers in a `finally`. A simulation runs the real script body with a mock `agent()`
that rejects for U: the pre-fix script hangs with only 2 agents started, and the fixed one finishes with U
NO-RESULT, U2 started fresh and a train landed. The public `rolling-wave.js` carried the same bug; it is fixed as
claude-skills v1.8.2 (a7c7708, tagged), proven with the same simulation. The project's pin stays at v1.8.1 until the
rolling wave migrates onto the public script after wave 70.

**PB1701: status-guard A/Bs recorded, owner decision taken.** Record:
`docs/rearchitecture/evidence/fleet-optimization/2026-09-28-status-guard-ab.*`, with the raw data, the runner, the
hook as tested and the sandbox task.
- Two sandbox A/Bs, 40 headless sessions, ended with 0 stale handoffs in every arm, so the sandbox does not
  reproduce the real 21 %.
- The first GUARD arm was mis-registered (`if: Bash(git commit*)` misses `git add -A && git commit` and
  agent-written checkpoint scripts).
- The fixed state-based per-command reminder cost about +20 % turns and dollars.
- **Owner:** "Go with your recommendation for the status guard. We cannot afford a 20% more cost." Then: "We must
  reduce cost where possible but not at the risk of reducing correctness."
- **Deploy after wave 70:** PreToolUse (refuse a commit while STATUS.md is stale, no `if` filter) and Stop /
  SubagentStop (refuse to finish while it is stale). The per-command reminder is removed from the script. The
  template wording becomes "after every commit". The effect is measured in production against the 21 % baseline.

**Also.**
- PB1699 (self-hosting) landed as DEVLOG 1753. The main checkout now carries `tools/claude-skills`.
- Entries 1750 and the orchestrator's memory checkpoint carried estimated rather than measured times; both are noted.
  Header times are read from `date`.

