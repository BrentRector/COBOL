# 2026-09-28 — Stamped handoff, scale A/B

**Note:** `kb/Work/PB1698`. **Pilot:** `2026-09-28-stamped-handoff-pilot.md`. **Workflow runs:** prep `wf_bc6224a5-632` +
`wf_3bd4496c-da1`, run `wf_1fe49615-44a`.
**Raw data:** `2026-09-28-stamped-handoff-scale.json` (truth, prep verdicts, per-agent metrics and verbatim results).
**Scripts:** `.prep.js`, `.run.js` and `.analyze.py` beside it.

## Question

The pilot (n = 4 per arm, one branch) showed 31 % fewer turns at equal accuracy. The owner asked: "Can we do a larger
test and get more definitive statistics?" The results will be cited in a public reply, so the effect must hold across
branches and the limits must be stated.

## Hypothesis

Across real branches, a stamped STATUS.md plus `status_delta.py` reduces the turns, tokens and wall time a fresh agent
needs to establish a correct resume state. It should help in BOTH cases: a STALE summary (the delta lists exactly what
to read) and a CURRENT one (the agent can skip re-reading history).

## Design

- **Scenario source.** 14 real finished implementer branches from waves 65–67 (3–10 commits each). A clerk agent per
  branch (Sonnet, writes only to scratch) did three things:
  1. judged whether the branch's STATUS.md describes its HEAD;
  2. copied it verbatim as the CURRENT variant;
  3. rewrote it as it would have read immediately after `HEAD~k` (k = 1 or 2, alternating by branch) as the STALE
     variant: DONE, GATE, batch and codes lines lose exactly what the last k commits did, NEXT names that work as not
     started, and nothing else changes.
  The stamped copies prepend `STATUS-AT:` naming HEAD (CURRENT) or `HEAD~k` (STALE). One STALE variant was
  spot-checked by the orchestrator against `git log`.
- **Exclusions.** 3 of 14 branches were excluded: their real STATUS.md did NOT describe their own last commit
  (`26b-2`, `26b-5`, `07e-2`), so they have no clean truth. See the incidental finding below.
- **Scenarios.** 11 branches × {STALE, CURRENT} = 22.
- **Arms.**
  - OLD: the unstamped file and the pre-stamp rule, "read it and `git log`, continue from NEXT".
  - NEW: the stamped file and the rule "run `status_delta.py` first".
  The task text is otherwise identical. The previous agent is described as having stopped "(it may have finished a
  step, or died mid-step)", in both arms and both variants.
- **Agents.** Fresh Opus, role `cobol-adjudicator` (read-only by hook), 2 replicates per arm per scenario = 88 agents,
  8 concurrent, interleaved so arms alternate. Orientation only: no edits, builds or tests.
- **Correct** means the agent's set of uncovered commits equals the truth set (7-character prefix match). For CURRENT
  the truth set is empty; for STALE it is the last k commits.
- **Statistics.** Paired by scenario: each scenario's arm mean is one observation, so replicates do not inflate n. They
  are an exact Wilcoxon signed-rank test on the 22 (or 11 per variant) paired means, a 95 % bootstrap CI (10 000
  resamples of scenarios, seed 20260928) on mean(NEW)/mean(OLD), and Fisher's exact test on accuracy. Computed with
  scipy 1.18.1.

## Attempts

1. **Prep run 1** (8 branches). 3 came back not current: 37.5 % of that sample.
2. **Prep run 2** (6 more branches). All 6 were current. Together: 11 clean branches of 14.
3. **The run.** 88/88 agents returned, 5.84 M subagent tokens, 862 s wall time, concurrency 8.
   - The workflow's `meta.description` still says "16 × 3" from the first draft; the arguments really were 22 × 2.
4. **Scoring.** The mechanical scorer marked 11 OLD answers wrong. All 11 were read by hand (listed below) and the
   score was kept. The mismatch is real, but it is mostly over-reporting rather than missing work, and the write-up
   says so.

## Result

Ratios are NEW / OLD. Rows are ALL (22 paired scenarios), then STALE and CURRENT (11 each).

| subset | metric | OLD mean | NEW mean | NEW / OLD | 95 % CI | NEW lower in | Wilcoxon p |
|---|---|---|---|---|---|---|---|
| ALL | turns | 8.91 | 7.41 | 0.83 | 0.71–0.96 | 16/22 | 0.022 |
| ALL | tokens | 537 k | 444 k | 0.83 | 0.68–1.00 | 16/22 | 0.023 |
| ALL | wall time | 78.7 s | 64.6 s | 0.82 | 0.71–0.94 | 18/22 | 0.012 |
| STALE | turns | 9.50 | 8.64 | 0.91 | 0.72–1.13 | 6/11 | 0.43 |
| STALE | tokens | 585 k | 551 k | 0.94 | 0.71–1.23 | 6/11 | 0.58 |
| CURRENT | turns | 8.32 | 6.18 | 0.74 | 0.63–0.88 | 10/11 | 0.014 |
| CURRENT | tokens | 488 k | 337 k | 0.69 | 0.57–0.84 | 10/11 | 0.010 |
| CURRENT | wall time | 74.0 s | 50.5 s | 0.68 | 0.58–0.80 | 10/11 | 0.003 |

**Accuracy** (the agent's uncovered set equals the truth set):

| subset | OLD | NEW | Fisher p |
|---|---|---|---|
| ALL | 33/44 (75 %) | 44/44 (100 %) | 0.0005 |
| STALE | 18/22 | 22/22 | 0.11 |
| CURRENT | 15/22 | 22/22 | 0.009 |

**The 11 OLD misses, read by hand:**

- **1 true miss.** `07e-3` STALE, rep 1, did not report the uncovered commit eff7995.
- **10 over-reports.** The agent flagged a commit as uncovered that the summary does cover, but only loosely:
  - a test class named only in the gate filter (`07e-1`, 4 answers);
  - doc and citation rewording filed under "docs" (`07e-3`, 2 answers);
  - drift fixes riding in a commit whose main work is described (`eda-2`, 2 answers);
  - a gate-1 RED fix the summary reports only as the final GREEN (`eda-3`);
  - a base commit that is on the branch but was rebased on main (`eda-1`).
  One `07e-3` STALE answer also found the true commit, but added an extra one.
- Many of these are defensible judgments, and an over-report costs re-reading rather than a wrong resume. They show
  that without a stamp, "does the summary cover this commit?" is a JUDGMENT, and fresh agents make it inconsistently
  (the same scenario drew different answers across replicates). With the stamp it becomes a mechanical fact, and all
  44 NEW answers agreed with it.

## Incidental finding: stale handoffs are routine, not a crash artifact

3 of the 14 finished branches (21 %) ended with a STATUS.md that did not describe their own last commit. None of
those agents died: each finished and returned a report. The common shape was a small final commit after the
checkpoint (a gate-red fix, a DRIFT_RULES regeneration, a self-review fix) that the agent never folded into
STATUS.md. The resume rule has to assume a stale summary even when nothing crashed. The stamp makes that staleness
detectable. It does not prevent it; that needs a rule that STATUS.md is rewritten after every commit, or a hook.

## Limits

- **Orientation only.** It measures reaching a resume state, not the downstream work.
- **Synthetic STALE variants.** They were produced by rewriting real files (clerk agents, one spot-checked). The
  CURRENT variants are the real files. The truth for STALE is defined by construction.
- **Accuracy is partly definitional for NEW.** The stamp defines coverage, so NEW's 44/44 means "consistent with the
  stamp". It does not mean NEW agents judged coverage better unaided.
- **Pilot vs scale.** The STALE-case saving did not replicate at scale. The pilot's 33 % came from one branch whose
  uncovered commit was a large, obvious preservation commit.
- **Sample.** 11 branches from waves 65–67 of one project, one model (Opus 5.5), 2 replicates. Wall time was measured
  under a concurrent 6-implementer wave, so it is noisy.

## Decision

- **Keep the stamp** (landed in DEVLOG 1749). It is now justified mainly on accuracy and consistency, and on cost in
  the common CURRENT case.
- **Public claims** must be the measured ones: about 17 % fewer turns and tokens overall; about 30 % in the CURRENT
  case; no significant saving when STALE; consistent coverage (0 vs 11 of 44 disagreements); and the 21 %
  stale-without-a-crash rate.
- **Follow-up.** STATUS.md should be rewritten after EVERY commit (enforceable by a post-commit check), a candidate
  practice to test next. Production measurement (waves 70+) continues under PB1698.
