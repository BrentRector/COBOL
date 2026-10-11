## Entry 1635 — 2026-09-22 09:59 PDT — The fleet moves to Opus 5.5: the subagent pin becomes the `opus` alias, verified by a probe agent

The owner switched the session to Opus 5.5 and directed that subagents use the default Opus 5 model, which as of
today is 5.5. The fleet had been held on the old model by a DATED id: `~/.claude/settings.json` →
`env.CLAUDE_CODE_SUBAGENT_MODEL = "claude-opus-5"`, which overrides the `model: 'opus'` every workflow passes. The
pin is now the ALIAS `opus`, so the fleet tracks the latest Opus without another edit, and still never inherits a
Fable orchestrator. **Verified, not assumed:** a one-line probe agent reported its model id as `claude-opus-5-5`.
The `workstream` skill's model sentence is updated to match (never pin a dated id).

⚠ **Every steering number was fitted on Opus 5** — the ~0.55 M agent tokens per weekly point calibration, the
quadratic turn-cost law and the 160/220 turn caps. They are now hypotheses to re-measure against the meter on the
first 5.5 wave, not constants. The re-created 03:05 auto-resume cron says so in its prompt.

Housekeeping: the last locked battery worktree (`wf_77fd8a7b-5e3-7`) was removed — its process (pid 41792) was gone,
its tree clean, and its head `e69515fe7` already on main.

