<#
.SYNOPSIS
  Close the orchestrator down as soon as possible without losing work, or clear a stop so it can run again.
.DESCRIPTION
  Default: create the STOP file in the coordination directory. A running unit is wound down gracefully by the supervisor
  (STOP-UNIT plus the fleet's graceful-stop file: every implementer and lander checkpoints, commits its WIP and returns
  SPLIT, the unit writes a handoff naming every branch, and the next start runs the `resume` unit); no unit running
  means the loop ends at once; a hold or a backoff is interrupted within a minute. Only a unit that ignores the
  wind-down for -GraceMinutes is killed, and that loses only uncheckpointed agent work.
  -Clear removes STOP, STOP-UNIT and the fleet stop file so the loop (or the logon start, which honours STOP) can run.
  -Status prints what is running and whether a stop is pending.
#>
[CmdletBinding()]
param(
    [switch]$Clear,
    [switch]$Status,
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' })
)
$ErrorActionPreference = 'Stop'
$stop = Join-Path $CoordDir 'STOP'
$stopUnit = Join-Path $CoordDir 'STOP-UNIT'
$fleetStop = Join-Path $CoordDir 'scratch/STOP'

function Get-Running {
    $lock = Join-Path $CoordDir 'orchestrate.lock'
    if (-not (Test-Path $lock)) { return $null }
    $l = Get-Content $lock -Raw | ConvertFrom-Json
    if (Get-Process -Id $l.pid -ErrorAction SilentlyContinue) { return $l }
    return $null
}

if ($Clear) {
    Remove-Item $stop, $stopUnit, $fleetStop -Force -ErrorAction SilentlyContinue
    Write-Host "stop cleared in $CoordDir (STOP, STOP-UNIT, scratch/STOP removed)"
    return
}
if ($Status) {
    $r = Get-Running
    Write-Host ("orchestrator: " + $(if ($r) { "running (pid $($r.pid), since $($r.started_at))" } else { 'not running' }))
    Write-Host ("STOP pending: " + (Test-Path $stop) + "; unit winding down (STOP-UNIT): " + (Test-Path $stopUnit))
    Write-Host ("ledger: " + ((& python (Join-Path $PSScriptRoot 'ledger_state.py') owed --coord $CoordDir) -join ' '))
    return
}
New-Item -ItemType Directory -Force -Path $CoordDir | Out-Null
New-Item -ItemType File -Force -Path $stop | Out-Null
$r = Get-Running
if ($r) {
    Write-Host "STOP created. The running supervisor (pid $($r.pid)) will wind its unit down: agents checkpoint, the unit hands off, the loop ends."
    Write-Host "Watch it end: pwsh scripts/orchestrator/stop.ps1 -Status   (or the last lines of $CoordDir\logs). Nothing is killed before the grace period."
} else {
    Write-Host 'STOP created. No supervisor is running; the next start will end at once until you run: pwsh scripts/orchestrator/stop.ps1 -Clear'
}
