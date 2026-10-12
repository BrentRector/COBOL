## Entry 441 — Retarget to .NET 10 / C# 14 (owner directive), guard re-proven green

Mid-handoff, the owner directed: **"This should be implemented on .NET 10."** Retargeted the whole solution from
`net9.0` to **`net10.0` / C# 14** as one self-contained, guard-verified change.

**What changed (build config only — the touch-points are centralized):**
- `Directory.Build.props`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`; `<LangVersion>13</LangVersion>` → `14`.
- `global.json`: SDK `9.0.312` → `10.0.204` (kept `rollForward: latestPatch`). The .NET 10 SDK (10.0.204) was already
  installed.
- Hardcoded `bin/Debug/net9.0/` build-output paths → `net10.0` in `scripts/guard.sh`, `scripts/guard-fast.sh`,
  `scripts/guard-run-group.sh`, `scripts/run-suite.sh`, and the `CliExitCodeTests` CLI-locator.

**No code change was needed** beyond config: `Compilation.EmitRuntimeConfig` already derives the target-framework
moniker + framework version from `Environment.Version` (exactly the fix logged in the 2026-03-18 net8→net9 Phase-1
entry — a hardcoded `net8.0` runtimeconfig had caused 153 failures then), so every emitted program's
`.runtimeconfig.json` picked up .NET 10 automatically.

**Verification (the real test — `TreatWarningsAsErrors=true` makes any new C# 14 / .NET 10 analyzer warning a hard
build failure):** purged the stale `net9.0` bin/obj (NuGet assets are framework-specific), clean-rebuilt the solution
= **0 warnings / 0 errors**; grammar regenerated cleanly. Then re-ran the full parallel guard: **ALL GREEN on .NET 10
— 1196 unit / 509 integration / 364 NIST, 0 regressions.** Because the unit suite includes the numeric (`CobolNum`)
and string (`CobolString`) differential oracles, the green run also proves .NET 10 introduced no
arithmetic/text behavioral drift.

**Docs:** updated the *forward-looking* stack references (PROMPT.md doctrine, README build requirement, PROJECT_PLAN
language + KTD #5, the data-model ADR target header, MASTER_PLAN §2) to .NET 10 / C# 14, and added a MIGRATION_LEDGER
retarget note. **Left historical records intact** (the 2026-03-18 Phase-1 ledger entry, `CONSTRAINTS.md` migration
phases, `AUDIT_REPORT.md`, `audit/` logs) — they document the net8→net9 work and rewriting them would falsify the
record (`feedback_transparency`). Also bumped the **disabled CI template** (`.github/workflows/build-and-test.yml.disabled`)
SDK pin `8.0.x` → `10.0.x` so the Phase-A "enable CI" work starts on the right runtime. Deliberately left as-is:
machine-managed permission allowlists (`.claude/settings*.json`, stale `net9.0`/`net8.0` paths — harmless,
auto-regenerated) and the pre-existing stale `samples/*.runtimeconfig.json` build artifacts (generated `net8.0`,
predate this change, out of scope).

**Verification (`feedback_scan_all_similar`):** a 3-lens adversarial panel (exhaustive residual-token grep ·
build-config/hidden-file consistency · history-integrity) confirmed **zero forward-looking refs remain, all historical
records intact, and build config uniformly net10.0** — the only flags were the deliberately-left allowlists/artifacts
above (now all documented) and the CI template (now fixed).

