## Entry 1867 — 2026-10-04 14:45 PDT — Ledger refreshed after train 1015 (GAP 289); PB1981 filed (loop engineering and the orchestrator loop)

Refreshed in the same turn as train 1015's landing report: five more inventory-moving commits are in the trend (121 points),
`ledger-in-flight.md` names trains 1011 to 1015 and the dropped ODO cluster, and the page was republished. `kb/Work/PB1981.md`
records the evaluation of the "loop engineering" recipe and of Anthropic's own published techniques (the C compiler project, the
long-running-harness and evals articles, the multi-agent research system), with a ten-row change table and its dependencies:
a closed-rows ratchet, a locked allocator, a wave planner, a Release leg, deterministic performance baselines, nightly
deterministic jobs, a clone-detection slot, a `cobc` bisect aid, a re-measure of the turn caps, and a real-program canary
corpus. Measured the same day from the local telemetry: the orchestrator main thread was about 8 % of the estimated spend (about
350k tokens of context per call) and the agents about 92 %, and Opus agents about a third of the agent spend. An orchestrator
loop prototype (supervisor script, planner, allocator, ratchet, budget estimator) is being built on its own branch for the owner
to test; it is not landed.

**Files:** `docs/rearchitecture/evidence/ledger-trend.json`, `docs/rearchitecture/evidence/ledger-in-flight.md`,
`kb/Work/PB1981.md`. No code changed, no diagnostic code used.

