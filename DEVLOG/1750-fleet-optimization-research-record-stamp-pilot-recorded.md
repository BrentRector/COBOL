## Entry 1750 — 2026-09-28 10:45 PDT — Fleet-optimization research record; stamp pilot recorded; public skills v1.7.0; self-hosting filed; WSL at package parity

Four owner directions this morning, all about the process rather than the compiler.

**Research record (owner ~10:35: "Track all this exploration and design data / attempts and result for fleet
optimization so we can write, sometime later, a research report").**
- New frozen-evidence directory `docs/rearchitecture/evidence/fleet-optimization/`. Each experiment is two files:
  - a `.md` write-up: question, hypothesis, design, every attempt, result, limits, decision;
  - a `.json` of raw per-agent data.
- The raw data comes from the new `scripts/telemetry/workflow_metrics.py`: turns, tool calls, tokens (total and
  non-cached) and wall time from a Workflow run's transcripts, plus each agent's verbatim result. The data outlives
  transcript pruning.
- The workstream skill now requires a record for every fleet experiment.
- PB1700 backfills the older measurements: the cost law, caps, trains, orientation share, rolling wave, clusters,
  model-per-role and the rest.
- The first record is the stamped-handoff pilot (PB1698). Stamped resumers used 31 % fewer turns and 33 % fewer
  tokens at equal accuracy (4/4 each), n = 4 per arm on one branch.

**Larger test (owner: "Can we do a larger test and get more definitive statistics?").**
- Scenarios are being prepared from real finished branches in two variants each:
  - CURRENT: the real STATUS.md;
  - STALE: the same file rewritten as it read before the last k commits.
- The design is 2 arms × 3 replicates, paired by scenario, analysed with a signed-rank test and bootstrap intervals.
- An incidental finding while preparing: 3 of 8 finished branches ended with a STATUS.md that did not describe their
  own last commit, although those agents did not die. Stale handoffs are common, not only a crash artifact.

**Public skills (owner ~10:20: "Keep the skills in the skills repo up to date with our latest processing. LinkedIn
readers have a link to that repo").**
- BrentRector/claude-skills v1.7.0 (9e61a7a) adds three practices to agent-fleet:
  - the stamped handoff and a generalized `status_delta.py`;
  - authorization carried into workflow prompts;
  - liveness judged by processes and the journal, not transcript mtimes.
- The stamped-handoff eval scored 1.00 with the skill and 0.00 without it. The leak scan was clean.
- Standing rule: every process change here is generalized there in the same session.

**Self-hosting (owner ~10:25: "Plus we should be self-hosting the skills, where applicable").** PB1699 files the
design:
- enable the `brent-tools` plugin at project scope;
- make the project's skills overlays on their public bases;
- give each generic script one source through a pinned submodule, with a drift check.

**WSL (owner: "install it in WSL as we keep tooling there updated with Windows").** scipy was installed on Windows,
and `scripts/wsl/setup-wsl.sh` gained a PY_PACKAGES step (pymupdf, scipy, numpy). The uv-managed interpreter is
PEP 668-managed, so the step uses `uv pip --system --break-system-packages`. Both hosts now match: pymupdf 1.28.2,
scipy 1.18.1, numpy 2.5.3.

