## Entry 1840 — 2026-10-02 19:30 PDT — Loop clock rebuilt to the reference design; Fable needs the owner's approval each time; models by role reviewed

- **Loop clock v2 (PB1912 experiment 2).** The first prototype (a ring per agent over wall time) was judged useless and unlike the
  reference (Scott Ernst's "loop clock"). `scripts/telemetry/loop_clock.py` is rewritten: the axis is mission progress (events
  ranked, not timed), the outer ring is the human's turns (filled petal = steering, hollow = approval), then orchestrator
  episodes as coils, then one ring per agent with rejection curls, and a mini-clock per agent on the right. Data comes through
  `workflow_metrics.agent_metrics` (now returning `tool_ts` and `events`) and a shared journal reader. On the 2026-10-01 runs it
  finds 21 rejections: 8 gate REDs, 1 CI red, 8 refuter overturns, 3 validator non-passes and 1 lander drop; 32 human turns,
  27 orchestrator episodes, 21 agents. Not drawn: the orchestrator's context size per turn, token cost, gate-slot waits, and the
  cluster each agent belongs to. A lander echoing an implementer's gate log shows extra green checks. The three tree-wide drift
  tests that scan scripts (FilterPopulationGuardDriftTests, GateLegDriftTests, TestLegReportDriftTests) run in CI at landing.
- **Models by role.** P1 now records the owner's 2026-09-30 allowance for Sonnet implementers (the rolling wave's
  `implementer_model` argument) and the owner's 2026-10-02 rule that FABLE needs explicit owner approval for each dispatch and is
  rationed: it is hugely more expensive than Opus and capped at 50 % of the weekly quota. The weekly meter is shared with the
  owner's other projects (E:\TENET used Fable the same day), so pacing reads the meter, not this project's token tally.
  Proposed and not decided: a Sonnet-to-Opus escalation after two failures, and an explicit judge-class floor.
- **Public skills repo.** `agent-fleet` was split into a 14 KB core plus references (was 40.8 KB loaded whole; lossless-checked)
  and a lint with CI for the repo was added; both are released through the skills repo, not here.

