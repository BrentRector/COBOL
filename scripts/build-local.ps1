# build-local.ps1 — THE GATE, as one command (kb/Work PB1708, PB1721; docs/rearchitecture/DESIGN-test-build-ci.md
# §3.14.1–3.14.6; the pwsh twin of build-local.sh). It sets the process PRIORITY and hands the gate to its driver,
# scripts/run_gate_legs.py, which holds this worktree's gate lock, takes a gate slot (implementer), runs the audits and
# the solution build, lists the population, plans the order and runs the WHOLE discovered population of Conformance,
# Unit and Characterization — then checks that population and prints `=== BUILD-LOCAL GATE: … ===`.
# ⛔ ORDER, DON'T SKIP (owner, 2026-09-28): no gate filters. -Mode is REQUIRED and has no default — the caller names it:
#   implementer  two legs, the likely-red cases first, FAIL-FAST, a gate slot (at most N implementer gates at once);
#   lander       one leg, every red of every cluster in one run, no slot (the lander never waits).
# Usage:  pwsh scripts/build-local.ps1 -Mode implementer -Priority BelowNormal     (every IMPLEMENTER gate)
#         pwsh scripts/build-local.ps1 -Mode lander                                (the LANDER, at Normal priority)
# ⛔ -Priority (owner decision 2026-09-22): the LANDER's gate is the serial bottleneck of the whole campaign and it
# shares one 32-core host with the implementer gates — its whole-Conformance leg measured 9.6 min quiet, 18.5 min at
# battery #84 and 30.6 min in train 48. Windows passes BelowNormal/Idle DOWN to child processes (and only those two
# classes), so an implementer that sets it here runs its build, dotnet test, every testhost and every compiled COBOL
# program below the lander's Normal-priority gate. The lander and the battery leave it at Normal.
param([Parameter(Mandatory = $true)][ValidateSet('implementer', 'lander')][string]$Mode,
      [ValidateSet('Normal', 'BelowNormal', 'Idle')][string]$Priority = 'Normal')
$ErrorActionPreference = 'Continue'
if ($Priority -ne 'Normal') {
    [System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::$Priority
    Write-Host "build-local: running at $Priority priority (inherited by build, test hosts and compiled programs)"
}
Set-Location (Split-Path -Parent $PSScriptRoot)
python scripts/run_gate_legs.py --mode $Mode
exit $LASTEXITCODE
