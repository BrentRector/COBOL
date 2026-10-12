## Entry 1780 — 2026-09-29 13:04 PDT — PB1743: the prescribed blocking wait idled to its timeout; pin claude-skills v1.14.1

**Found.** During wave 72 the owner saw the fleet at ~5 % CPU and "far far far longer than it should". Group H's final
gate went GREEN at 11:56, but its wait returned only at 12:03:48, killed by its timeout (exit 124). The wait was the
form every brief prescribed:
`timeout 580 bash -c 'tail -n +1 -f <log> | grep -m1 "<verdict>"'`
- `grep -m1` exits on the match, but `tail -f` learns its reader is gone only at its next WRITE. A verdict line is
  normally the log's last, so the pipeline lives until the timeout.
- Measured on a two-line log: the `tail -f` form returned at the 8 s timeout, while
  `until grep -q …; do sleep 5; done` returned in 0 s.
- The orchestrator's own push-main wait that morning did the same, and the train-72 lander lost ~7 minutes to it after
  its gate went green at 12:25:40.
- Every gate, push-main and battery wait in the fleet therefore idled up to one timeout after its result.
- The form was prescribed in MANDATORY-PRACTICES P2, the workstream skill, the implementer dispatch spec and the
  lander-train brief, and in the public agent-fleet skill, brief template and README. `check_practices.py` even
  REQUIRED `tail -n \+1 -f` in two briefs.

**Fixed.**
- **Every project copy** now reads `timeout 580 bash -c 'until grep -q "<verdict>" <log>; do sleep 5; done'`. The
  dispatch spec blocks on `=== BUILD-LOCAL GATE: `, M13's verdict line.
- **`check_practices.py`** requires `until grep -q` where it required `tail -n \+1 -f`, and REJECTS any brief carrying
  `tail -f … | grep -m1` (`HANGING_WAIT`).
- **Commit timing.** These edits sat uncommitted in the main checkout from ~12:10, so train 72's lander and wave 73's
  implementers read the fixed wait from disk. Train 72's M13 rewrote the same briefs, so the fix was re-applied on
  its versions (at `86cdf4456`) and is committed only now that train 72 has landed.
- **Public skills.** claude-skills v1.13.4 fixed the agent-fleet skill and the brief template, and v1.14.1 the last
  copy, in the skill's README. The owner pushed both.

**Pin.** `tools/claude-skills` moves 1.13.2 → 1.14.1. The four project skills (gate, review, spec-lookup, workstream)
and PB1699 name it. Between those versions the public repo also gained GLOSSARY.md (1.13.3) and the
`performance-diagnosis` and `cross-platform` skills (1.14.0).

**Checks.** `check_practices` is GREEN, `work.py check` passes, and PB1743 has landed.

