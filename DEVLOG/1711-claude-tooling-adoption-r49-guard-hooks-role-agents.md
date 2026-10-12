## Entry 1711 — 2026-09-25 13:14 PDT — Claude tooling adoption (R49): guard hooks, role agents, telemetry, lander review

The owner answered the twelve ranked tooling recommendations (kb/Work R49): a STANDING Workflow opt-in; hooks, a longer
subagent cache, per-role model/effort, OpenTelemetry, plugin evals, the C# LSP, a lander review step and /skill-doctor adopted;
no routine for the daily resume; /compact deferred until evaluated. What landed here:

- **Guard hook** `scripts/hooks/forbidden_commands.py` on every Bash and PowerShell call: blocks `git stash` (list/show pass)
  and `--autostash`, a direct push to main in THIS repo (a `cd` into another repo is exempt), backslash escapes in a heredoc
  body, a property-less `~X` test-filter term, and an unredirected filtered `dotnet test`. `permissions.deny` adds the stash and
  push shapes; the old `Bash(git stash:*)` ALLOW rule is gone. A 21-case self-test (each rule fires, each legitimate
  neighbour passes) runs in CI `audits` and build-local. It fired on the orchestrator's own `\n`-bearing heredoc minutes
  after registration.
- **Role agents** `.claude/agents/cobol-{implementer,lander,refuter,adjudicator,clerk}.md`: Opus `high` for implementer,
  lander and adjudicator, `xhigh` for the refuter (the quality gate), Sonnet `medium` for mechanical chores; turn caps 220/160/80;
  `experimental.cacheTtl: 1h` — the research agent confirmed there is no global subagent-cache setting, so the cache lives
  per role. Refuter and adjudicator attach `scripts/hooks/readonly_repo.py` (Write/Edit inside any git tree is refused; the
  scratchpad checkpoint files pass), with its own self-test. The wf templates' adjudicate/validate/refute stages now pass
  `agentType`, and `check_practices.py` requires it. NOT YET PROVEN LIVE: the agent registry loads at session start, so the
  first dispatch after a restart is the proof.
- **Telemetry**: `scripts/telemetry/otlp_sink.py` (loopback OTLP/HTTP-JSON receiver appending to `~/.claude/telemetry/`,
  started by a machine-local SessionStart hook) and `usage_report.py` (tokens and cost per agent/skill/model). Verified with
  a synthetic event end to end; real events begin with the next session.
- **Lander review**: lander-train-brief step 5b + L9 — the `review` skill over the train diff before push-main; a confirmed
  correctness finding drops its cluster. (`/code-review` is interactive-only; the owner runs `/code-review ultra` on big batches.)
- **No manual step, no silent skip** (owner, mid-change): `scripts/hooks/tooling_check.py`, run by the SessionStart hook,
  checks every capability above each session — OK / REPAIRED (the sink is restarted automatically) / N/A (cloud) /
  ASK-OWNER, and the session-start skill (step 3b) and O6 make every ASK-OWNER line a question to the owner. O7 turns
  the trigger-bound owner steps (`/code-review ultra` at a multi-train battery close, public-skill evals before a push)
  into questions at their trigger; P13 and the implementer dispatch template carry LSP-first C# navigation.
- `FilterPopulationGuardDriftTests` flagged the guard hook and its self-test as "filtered dotnet test sites": they QUOTE
  those command shapes to recognize them and run nothing. The test now exempts `scripts/hooks/` (a hook receives commands
  as data), next to the guard's existing self-exemption; no hook runs a test (grepped). Wave-local gate
  `~Drift|~EditionGate` GREEN.
- MANDATORY-PRACTICES P12, P13, L9, O4–O7; workstream + session-start skills; DOC_INDEX row.
- Outside the repo: `csharp-ls` 0.28.0 installed (the `csharp-lsp` plugin was enabled without its server — the LSP tool
  reports it missing until the next session start); the public skills repo's eval suite is being built separately.

