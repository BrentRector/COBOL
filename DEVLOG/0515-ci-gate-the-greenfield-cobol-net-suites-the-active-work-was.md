## Entry 515 — 2026-06-09 15:14 PDT — CI: gate the greenfield COBOL.NET suites (the active work was untested in CI)

Entry 514 surfaced that CI ran ONLY the legacy guard (`scripts/guard.sh`) + the CobolSharp unit/integration suites —
the greenfield `Cobol.Net.Tests.Conformance` (the 15 byte-matching NC programs + every differential test) and
`Cobol.Net.Tests.Unit` were **not run in CI at all**, so a greenfield regression (e.g. a future re-break of the
PERFORM-range fix) would sail through green. The active compiler had no CI coverage.

Added both greenfield suites to `.github/workflows/build-and-test.yml`: to the Linux **guard** job (after
`guard.sh`; they build on demand — CI Linux has pwsh for the Frontend's ANTLR-gen step, and the committed
`Generated/*.cs` are current so regeneration is skipped) and to the **Windows** job (`--no-build`, reusing the
Release sln build). Separate steps per project so one suite's failure is never masked by the other's exit code.

**Validated on Linux first to avoid a red CI** (the recurring complaint): installed PowerShell on WSL as a dotnet
global tool (`dotnet tool install --global PowerShell`; run with `DOTNET_ROOT=$HOME/.dotnet`) so the greenfield
Frontend builds there, then ran the suites on Linux — **292 conformance + 14 unit, green 3/3 runs** (stable under the
child-process + parallel-compile load, with the DEVLOG-512 validator race fixed). WSL repro workflow captured in the
`reference_wsl_linux_repro` memory. No product code changed — CI wiring only.

