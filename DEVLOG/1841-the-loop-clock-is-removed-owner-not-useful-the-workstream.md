## Entry 1841 — 2026-10-02 21:10 PDT — The loop clock is removed (owner: not useful); the workstream skill is split into a core plus references

- **Loop clock removed.** The owner judged the visualization not useful ("I don't find it useful") after two builds. Removed:
  `scripts/telemetry/loop_clock.py`, the extra fields the clock added to `workflow_metrics.agent_metrics` (restored to the
  2026-09-28 version, `c26b5a3ba`), the PB1912 experiment-2 section (now a short "abandoned, do not rebuild" note), and the
  published page and scratch files outside the repository. DEVLOG entries 1839 and 1840 describe it and stay as history.
  Experiment 1 of PB1912, the return-payload caps in `wf_rolling_wave.js`, is kept and waits to be measured on the next wave.
- **`workstream` skill split.** `.claude/skills/workstream/SKILL.md` went from 30 KB to 12 KB: the checkpoint protocol, the cost
  law with the turn caps and the landing mechanics moved verbatim into `references/` (checked lossless: 35 of 35 original
  paragraphs present), each leaving a one-line rule and a "read this before ..." pointer in the core. This follows the owner's
  "update all skills ... with your suggestions" and the public skills repo's `agent-fleet` split (a dispatch loads both, which was
  about 71 KB).

