## Entry 1768 — 2026-09-28 20:50 PDT — PB1714: the stall watchdog no longer trusts the scheduler (claude-skills v1.13.1)

A LinkedIn commenter observed that silence-based detection cannot see the 16-hour wave-68 outage (PB1703), because
there it was the SCHEDULER that reported "running". Replaying wave 68's journal with the v1.13.0 watchdog showed he
was right in substance.
- It WOULD have raised an alarm, but only because it ignored the journal's `failed` record and misreported the dead
  agent as "waiting on the model".
- With every agent finished or failed, it checked nothing at all. Any scheduler hang with nothing in flight was
  invisible to it.

**The fix** (public `agent-fleet/references/stall_watch.py`, v1.13.1, `897b2b5`, pushed by the owner):
- `failed` closes an agent, and the agent is reported as dead.
- A new IDLE exit fires when no agent is in flight and no agent transcript has a record newer than `--idle` (600 s).
  Idleness is timed from the agents' own record timestamps, since the journal has none, and in live mode from the
  watch's start at the earliest.

**Replays:**
- wave 68: U and Y FAILED, and IDLE fires;
- the running design workflow: not flagged;
- a finished workflow: IDLE, the benign case (stop the watcher on the completion notice);
- live mode with `--idle 4`: exits after 5 s.

**In the project:** the submodule is pinned at v1.13.1, all four project skills (review, spec-lookup, workstream,
gate) and PB1699 cite the new pin, and PB1714 has landed.

