## Entry 1757 — 2026-09-28 12:38 PDT — PB1704: a hung agent held wave 70 for 90 minutes unnoticed; stall_watch.py, a rolling-wave ceiling, practice O8

Wave 70's group C agent committed and gated PB1117, PB1174 and PB1268 (GREEN: Conformance 4,291 including the corpus
and NIST legs, Unit 29,694). At 11:02 it read the report template and never produced another token. U2 hung the same
way at 12:20. Quota was not the cause: the session meter read 37 % and the weekly 32 %. Nothing noticed until the
owner asked at 12:29, and the final train waited on it. The owner: "We just wasted 1.5 hours waiting for nothing.
Fix that".

**Measured first.** Over 150 agent transcripts (about 27,800 silences), a wait on the model is p99 30 s and p99.9
94 s, and a wait inside a tool call peaks at 585 s. So a stall is more than 10 min on the model, or more than 12 min
in one tool call. C would have been flagged at about 11:13.

**Fix, public first (claude-skills v1.10.0, 4e79f7a).**
- `agent-fleet/references/stall_watch.py` reads the journal's pending agents and each one's last-record timestamp,
  and exits when an agent crosses a threshold. Its first live run flagged C (89 min) and U2 (12 min), and not the
  working lander.
- The rolling wave's per-implementer ceiling is a `setTimeout` race; a probe workflow verified that timers are
  allowed in workflow scripts. At the ceiling, the agent is recorded `STALLED` and the wave moves on. Landers are
  excluded.
- SKILL §5 carries the procedure: let the others finish, stop the workflow, and dispatch the remainder from the
  stalled agent's branch.

**Project.**
- `wf_rolling_wave.js` gains the same ceiling (`implementer_ceiling_min`, 240).
- Simulations: a never-returning agent hangs the old script, and the fixed one finishes, records U as STALLED and
  lands. The PB1703 death case still passes.
- MANDATORY-PRACTICES O8: run the watchdog beside every fleet workflow.
- `check_practices.py` requires the ceiling, the rejection handling and O8.
- The pin moved to v1.10.0, and the overlays and PB1699 name it.

**Also.** The owner asked for a LEARNINGS.md in the public repo covering everything to date: each problem, its root
cause, and the fix in the skills. An agent is compiling it from DEVLOG, the evidence records, MANDATORY-PRACTICES,
kb/Work and the orchestrator's feedback memories.

