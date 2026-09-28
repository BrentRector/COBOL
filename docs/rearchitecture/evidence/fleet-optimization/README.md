# Fleet-optimization evidence

**Owner, 2026-09-28 ~10:35 PDT:** "Track all this exploration and design data / attempts and result for fleet
optimization so we can write, sometime later, a research report."

This directory is the RAW RECORD of every experiment, measurement and design attempt aimed at making the agent fleet
cheaper, faster or more reliable: orientation, handoffs, gates, landing trains, concurrency, model-per-role. It is
source material for a later research report, so it keeps failures, reversals and null results as carefully as wins.

⛔ **Frozen evidence** (the directory rule of `docs/rearchitecture/evidence/`, owner decision `kb/Work/PB785`): a
record is written once, when its experiment ends, and never edited to stay current. A later result that refutes an
earlier one gets its own record, and the earlier record gains a `superseded_by` marker only. The CURRENT practice lives
in `.claude/skills/workstream/` and its public generalization, `BrentRector/claude-skills` (agent-fleet); the open
work is in `kb/Work/`. This directory holds neither.

## One experiment = two files, named `YYYY-MM-DD-<slug>`

- **`<name>.md`, the write-up.** It has these sections:
  - **Question:** what cost or failure motivated the experiment, with the owner's words where they asked.
  - **Hypothesis:** stated before the run.
  - **Design:** scenarios, arms, sample size, what was held constant, and how correctness was scored.
  - **Attempts:** every run, including aborted, broken or re-scored ones, and why each changed.
  - **Result:** the table, the statistics and the effect size with its interval.
  - **Limits:** what the data cannot say.
  - **Decision:** what was adopted or declined, by whom, and where it now lives.
  - **Links:** the kb/Work note, DEVLOG entry, workflow run ids and commits.
- **`<name>.json`, the raw data.** It opens with a `_frozen` banner, then the experiment metadata, then one row per
  agent as measured by `scripts/telemetry/workflow_metrics.py`: turns, tool calls, tokens (total and non-cached), wall
  time, and the agent's verbatim structured result. The data must outlive the transcripts, which are pruned.

Measurements that predate this directory were recorded elsewhere and are not copied here; `kb/Work/PB1700` backfills
their records. They include the 2026-09-04 cost law and caps (`../PROCESS-REVIEW-2026-09-04.md`), the orientation
share, the rolling wave, computed fix clusters, lander throughput and the model-per-role split.
