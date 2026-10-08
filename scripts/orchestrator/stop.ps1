<#
.SYNOPSIS
  Close the orchestrator down as soon as possible without losing work, or clear a stop so it can run again.
.DESCRIPTION
  Default: create the STOP file in the coordination directory. A running unit is wound down gracefully by the supervisor
  (STOP-UNIT plus the LOOP's own fleet stop, scratch\STOP-loop: every implementer and lander the loop dispatched
  checkpoints, commits its WIP and returns SPLIT, the unit writes a handoff naming every branch, and the next start runs
  the `resume` unit); no unit running means the loop ends at once; a hold or a backoff is interrupted within a minute.
  Only a unit that ignores the wind-down for -GraceMinutes is killed, and that loses only uncheckpointed agent work.
  Another session's agents are NOT stopped: their dispatch names their own fleet stop (kb/Work PB2483).
  -Global also creates the owner's GLOBAL stop, scratch\STOP, which every agent of every session obeys (a quota kill is
  imminent, the machine must go down); use it only when every session's work must wind down.
  -Clear removes STOP, STOP-UNIT and the loop's fleet stop so the loop (or the logon start, which honours STOP) can run;
  -Clear -Global also removes the global stop (the loop will not start while it exists).
  -Status prints what is running, which stops are pending, whether a ledger publish is owed, and who holds the landing
  lease (scripts/orchestrator/landing_lease.py, kb/Work PB2537).
#>
[CmdletBinding()]
param(
    [switch]$Clear,
    [switch]$Status,
    [switch]$Global,
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' })
)
$ErrorActionPreference = 'Stop'
$stop = Join-Path $CoordDir 'STOP'
$stopUnit = Join-Path $CoordDir 'STOP-UNIT'
$fleetStop = Join-Path $CoordDir 'scratch/STOP-loop'    # the loop's own fleet stop (orchestrate.ps1 $FleetStop)
$globalStop = Join-Path $CoordDir 'scratch/STOP'        # the owner's: every agent of every session obeys it

function Get-Running {
    $lock = Join-Path $CoordDir 'orchestrate.lock'
    if (-not (Test-Path $lock)) { return $null }
    $l = Get-Content $lock -Raw | ConvertFrom-Json
    if (Get-Process -Id $l.pid -ErrorAction SilentlyContinue) { return $l }
    return $null
}

if ($Clear) {
    Remove-Item $stop, $stopUnit, $fleetStop -Force -ErrorAction SilentlyContinue
    if ($Global) { Remove-Item $globalStop -Force -ErrorAction SilentlyContinue }
    Write-Host "stop cleared in $CoordDir (STOP, STOP-UNIT, scratch/STOP-loop removed$(if ($Global) { ', and the global scratch/STOP' }))"
    if (Test-Path $globalStop) { Write-Host "the owner's GLOBAL stop $globalStop is still set: no agent and no loop runs until stop.ps1 -Clear -Global" }
    return
}
if ($Status) {
    $r = Get-Running
    Write-Host ("orchestrator: " + $(if ($r) { "running (pid $($r.pid), since $($r.started_at))" } else { 'not running' }))
    Write-Host ("STOP pending: " + (Test-Path $stop) + "; unit winding down (STOP-UNIT): " + (Test-Path $stopUnit) +
        "; loop fleet stop (scratch/STOP-loop): " + (Test-Path $fleetStop) + "; GLOBAL stop (scratch/STOP): " + (Test-Path $globalStop))
    Write-Host ("ledger: " + ((& python (Join-Path $PSScriptRoot 'ledger_state.py') owed --coord $CoordDir) -join ' '))
    # who holds main (kb/Work PB2537): free, held by a lander (holder, worktree, heartbeat, expiry, reason), or dead
    Write-Host ((& python (Join-Path $PSScriptRoot 'landing_lease.py') status --coord $CoordDir) -join ' ')
    return
}
New-Item -ItemType Directory -Force -Path $CoordDir, (Join-Path $CoordDir 'scratch') | Out-Null
New-Item -ItemType File -Force -Path $stop | Out-Null
if ($Global) {
    New-Item -ItemType File -Force -Path $globalStop | Out-Null
    Write-Host "GLOBAL stop created ($globalStop): every agent of every session checkpoints and returns at its next step."
}
$r = Get-Running
if ($r) {
    Write-Host "STOP created. The running supervisor (pid $($r.pid)) will wind its unit down: agents checkpoint, the unit hands off, the loop ends."
    Write-Host "Watch it end: pwsh scripts/orchestrator/stop.ps1 -Status   (or the last lines of $CoordDir\logs). Nothing is killed before the grace period."
} else {
    Write-Host 'STOP created. No supervisor is running; the next start will end at once until you run: pwsh scripts/orchestrator/stop.ps1 -Clear'
}
