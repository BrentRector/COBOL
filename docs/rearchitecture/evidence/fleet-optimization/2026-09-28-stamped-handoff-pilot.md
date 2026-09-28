# 2026-09-28 — Stamped handoff, pilot A/B

**Note:** `kb/Work/PB1698`. **DEVLOG:** 1749 (the mechanism). **Workflow run:** `wf_2190fcf0-fec`.
**Raw data:** `2026-09-28-stamped-handoff-pilot.json`.

## Question

An outside reviewer on the owner's LinkedIn thread asked whether "read the commits, not the summary" gives back the
orientation savings that summaries were meant to buy. That rule was adopted after wave 68, whose agents died after
committing but before rewriting STATUS.md. The owner asked (~10:05 PDT) to build the fix and "evaluate its
effectiveness", and will not post the reply until there is data.

## Hypothesis

A STATUS.md that names the commit it describes (`STATUS-AT: <sha>`), plus a tool that lists only the commits after
it (`scripts/spec/status_delta.py`), lets a fresh agent reach a correct resume state in fewer turns and tokens, with
no loss of accuracy.

## Design

- **Scenario.** One real stale branch, `worktree-wf_e9a82a33-6b6-4` (wave-68 group U; 6 commits since the base). Its
  STATUS.md describes 59f19d6e8. The agent then died on an API error, and the orchestrator committed its in-progress
  PB1579 edits as aaf1823fc (4 files, never built or gated), which the summary does not mention.
- **Arms.**
  - OLD: the unstamped STATUS.md and the pre-stamp resume rule, "read it and `git log`, continue from NEXT".
  - NEW: the same STATUS.md with `STATUS-AT: 59f19d6e8` prepended, and a rule to run `status_delta.py` first.
- **Agents.** Fresh Opus agents (role `cobol-adjudicator`, read-only by hook), 4 per arm, all 8 in parallel, with
  identical task text except the rule. Orientation only: no edits, builds or tests.
- **Correct** means all three of:
  1. named aaf1823fc as uncovered and characterized it as partial PB1579 work;
  2. said the branch was never gated;
  3. chose to resume on top of it rather than restart.
- **Held constant:** the model, the role, the scenario, the STATUS text apart from the stamp line, and the output
  schema.

## Attempts

- One run, launched at 10:12 PDT; 8/8 agents returned, 623 k subagent tokens, 173 s wall time.
- The scorer's regex for "never gated" missed NEW-1's wording ("GATE: never run on this branch"). The answer was
  re-read by hand and it is correct.

## Result

| arm | turns | tool calls | tokens (incl. cache reads) | wall time | commits inspected | correct |
|---|---|---|---|---|---|---|
| OLD | 14.8 (11–19) | 15.8 | 1.00 M | 147 s | 6.5 | 4/4 |
| NEW | 10.2 (9–12) | 11.8 | 0.67 M | 116 s | 4.5 | 4/4 |

- The stamp cut turns by 31 %, tokens by 33 % and wall time by 21 %, at equal accuracy.
- Stamped agents still spot-checked earlier commits (1, 5, 6 and 6 inspected). That is the intended "navigation, not
  evidence" behavior: the saving came from not having to reconstruct coverage, not from skipping verification.

## Limits

- n = 4 per arm, one scenario of 6 commits, no significance test.
- Only the STALE case was measured; the CURRENT case (stamp = HEAD) was not.
- Orientation only, so the effect on the downstream work is not measured.

## Decision

The owner asked for "a larger test [with] more definitive statistics". That is the scale experiment:
16 scenarios (8 real branches × CURRENT/STALE) × 2 arms × 3 replicates, paired by scenario. The mechanism itself landed
before this pilot (DEVLOG 1749) and was generalized into the public `agent-fleet` skill v1.7.0 without effectiveness
claims.
