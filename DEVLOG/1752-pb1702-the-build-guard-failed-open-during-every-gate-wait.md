## Entry 1752 — 2026-09-28 10:55 PDT — PB1702: the build guard failed open during every gate wait; correction of the "lazy transcript" claim; PB1701 filed

**PB1702 landed.** `scripts/hooks/fleet_active_build.py` counted a foreign agent as live only if its transcript was
written in the last 120 s. But a tool call writes nothing between its `tool_use` record and its `tool_result`, and
the fix lane's gate wait (`timeout 580 … tail -f <log> | grep -m1 …`) runs about 580 s.
- Measured on train 69 (three finishers and the lander): 5–7 silences over 120 s per agent, the longest 582–585 s,
  each opened by a Bash `tool_use`.
- So the guard ALLOWED builds in a live agent's tree during every gate wait, exactly when its binaries were in use.

The fix adds a second window: a transcript whose last decisive record is an unanswered tool call is live for up to
660 s (the shell tools' 600 s maximum plus a margin). The bound keeps a killed agent's dangling call from denying
forever. It reads only the tail, and a parse failure falls back to the old rule, so the change can only add
liveness. The self-test gained 6 liveness cases over real files with set mtimes, and all cases pass. The sibling
sweep found no other mtime-based liveness check.

**Correction.** This morning (DEVLOG 1750, orchestrator memory, public agent-fleet v1.7.0) the orchestrator
attributed wave 69's 20+ minute transcript silence at 00:32–00:53 to "lazily written" transcripts. That was wrong:
- The owner's `/clear` at ~00:33 started a new session id.
- The running workflow's agents wrote the rest of their transcripts into the new session's directory, while its
  journal stayed in the old one.
- The watchdog read the old directory.
Memory is corrected, and public v1.8.0 carries the corrected "why". PB1702 records both the wrong first filing and
the measured defect.

**PB1701 filed (owner: "Add the rewrite-status-after-every-commit practice and test it").**
- 3 of 14 finished branches (21 %) ended with a stale STATUS.md with no crash, so the written rule alone is not
  enough.
- `status_guard.py` enforces it: after a commit it reminds, before the next commit it refuses, and it refuses the
  agent's stop while the stamp is not HEAD. It is silent without STATUS.md and fails open. The self-test covers 14
  cases and caught `git commit-tree` being matched as a commit.
- The A/B uses headless `claude -p` sandboxes, because hooks load at session start. The first smoke run showed a
  15-turn toy task is too easy: the rule-only arm complied, which would give a null result that says nothing. The
  task was rebuilt to resemble real work: 9 bugs across 3 modules, a long protocol document with the rule buried in
  it, and a gate that goes red after the tests pass. The A/B (10 per arm) is running.
- Rollout comes after the result and after wave 70 finishes, whose unstamped STATUS files the hook would refuse.

