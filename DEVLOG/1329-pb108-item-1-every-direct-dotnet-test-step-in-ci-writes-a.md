## Entry 1329 — 2026-08-19 09:58 PDT — PB108 item 1: every direct `dotnet test` step in CI writes a TRX and its job uploads it with `if: always()` — a red is diagnosable from the artifact; two external "hard implementation mode" prompts evaluated, mostly rejected, three tooling gaps adopted before T1

**The evaluations.** Two more externally-written prompts arrived (the source does not know this repository). The
first asked for a build+CI pipeline (exists — multi-OS, sharded, population-guarded, nightly oracle), a shared
opcode IR with `IR_COMPARE_STRING_COLLATION`-style opcodes (⛔ contradicts the owner-locked "no shared lowered IR"
— the bound tree feeds `ICodeGenBackend`), invented syntax (`IF A < B COLLATION`, `NORMALIZE(A)` — not in any
edition of ISO/IEC 1989; the legitimate part, `SET LOCALE`, IS T1 in the standard's Format 11 shape), `.NET 8` (the
tree is net10.0), the legacy project names, a cache wired into `Collator.Compare` by default (⛔ measured regression:
streaming wins short pairs), appsettings in a dependency-free runtime, a `DefaultNormalizationForm` knob (UCA is
over NFD — not a knob). Rejected on those grounds; three small pieces had real value and the owner confirmed them:
TRX artifacts in CI, a drift-guarded REGISTRY of the runtime's environment knobs, a local fresh-build-then-filtered-
gate wrapper. The second prompt restated exactly those three; its remaining facts were corrected in the evaluation
(`CobolSharp.sln` not `Cobol.Net.sln`; `COBOL_COLLATION_CACHE_EVICTION`; the knobs it missed — `COBOL_SYSTEM_LOCALE`,
`COBOLNET_CLOCK`, the dynamic `COBOL_<SWITCH-NAME>` family; `battery.sh --local` does not exist and the wave-local
gate is a FILTER, not a command; a literal grep for `GetEnvironmentVariable("COBOL_` finds zero sites because every
call passes a constant; tests live under `tests/`; `${{ matrix.shard }}` is empty outside a matrix job). Owner
decision: do the three BEFORE T1, in order — CI diagnosability helps the locale wave directly. Registered as
`kb/Work/PB108` (process-only) before a line of it was written (rule 8).

**Item 1 — landed here.** `.github/workflows/build-and-test.yml`: `--logger "trx;LogFileName=<step>.trx"
--results-directory TestResults/<OS>-<job>/` on all ten direct `dotnet test` steps (Linux + Windows conformance
shards, greenfield unit + characterization, INV-1-strong, the Windows legacy unit/integration + greenfield pair, the
nightly legacy-oracle verify), and one `actions/upload-artifact@v7` step per job with `if: ${{ always() }}`,
`retention-days: 14`, `if-no-files-found: ignore`, named `test-results-<OS>-<shard|unit|inv1-strong|build-test|
legacy-oracle>` — the shard jobs also upload `shard.log` (their console). The two OSes share shard NAMES, so the name
carries both. Untouched: the matrix, shard filters and math (the console logger stays when a TRX logger is added —
probed locally: the `Total:` line the population guard greps is still printed, the named TRX is written), the
concurrency cancel, the doc-only `paths-ignore`, the nightly job, `workflow_dispatch`, and `guard-fast.sh` (its
verdict is its own NIST-loop output). The population job's `--list-tests` is discovery, not a run — no TRX. YAML
re-parsed after the edit; a script asserted every test step has a logger and every test job an upload.

Next: item 2 (`Control/RuntimeConfig.cs` + the two constant extractions + the two-direction drift test), item 3
(`scripts/build-local.{sh,ps1}`), then T1.

