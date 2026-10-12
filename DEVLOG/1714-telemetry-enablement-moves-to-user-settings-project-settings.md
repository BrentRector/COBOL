## Entry 1714 — 2026-09-25 17:20 PDT — Telemetry enablement moves to user settings (project settings are ignored)

Claude Code's system diagnostics reported that it IGNORES `CLAUDE_CODE_ENABLE_TELEMETRY`, `OTEL_METRICS_EXPORTER`,
`OTEL_LOGS_EXPORTER`, `OTEL_EXPORTER_OTLP_PROTOCOL` and `OTEL_EXPORTER_OTLP_ENDPOINT` in `.claude/settings.local.json`:
a project's settings files may only turn telemetry OFF. The R49 telemetry adoption (commit e105e0833) had placed the
switch there, so no telemetry was ever exported, while `tooling_check.py` read that same file and reported the sink
"OK" — a green check that measured the configuration it expected, not the configuration Claude Code honors.

Root-cause fix: the five variables now live in the user settings `~/.claude/settings.json` (per machine, untracked);
`.claude/settings.local.json` is reverted to permissions only. `scripts/hooks/tooling_check.py` now reads the telemetry
switch from `~/.claude/settings.json`, so its ASK-OWNER line points at the file Claude Code actually reads.
`scripts/telemetry/otlp_sink.py` docstring and `kb/Work/R49.md` row 5 updated to name the user settings. Effective from
the next session (env is read at startup).

