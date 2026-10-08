#Requires -Version 7.0
<#
.SYNOPSIS
  The OPERATOR's one way to steer the orchestrator loop: name the unit it runs next, with the reason and instruction.
.DESCRIPTION
  Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 4.8 (kb/Work PB2596). Writes the operator's handoff
  <coord>\handoff.operator.json ({unit: operator, outcome: done, next_unit, next_unit_reason, summary}), VALIDATED against
  handoff.schema.json before it lands (an invalid one is refused and nothing is written). It may be written at any time,
  while a unit runs or while the loop is stopped. At its next choice the supervisor passes it to next_unit.py, where it
  WINS over every other handoff (a unit's, or one the supervisor synthesized for a dead unit) and over every
  deterministic check; it steers exactly ONE choice: when the supervisor starts the unit it chose, it archives the file
  beside that unit's log and copies it to handoff.last.json, so the unit reads the operator's instruction as its previous
  handoff. `orchestrate.ps1 -Unit` writes its first unit through this script, so there is no second steering path.
  -Show prints the pending steer; -Withdraw removes it unconsumed.
.EXAMPLE
  pwsh scripts/orchestrator/steer.ps1 -Unit wave -Reason 'the operator wants a wave now' -Summary 'Run a wave; hold push-main until R2 has landed.'
  pwsh scripts/orchestrator/steer.ps1 -Show
#>
[CmdletBinding(DefaultParameterSetName = 'Steer')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Steer')]
    [ValidateSet('wave', 'campaign', 'land', 'resume', 'meter')]
    [string]$Unit,
    # Why this unit now: it becomes the choice's reason in the supervisor's log and units.jsonl.
    [Parameter(Mandatory, ParameterSetName = 'Steer')]
    [string]$Reason,
    # The instruction the unit reads as its previous handoff's summary (default: the reason).
    [Parameter(ParameterSetName = 'Steer')]
    [string]$Summary = '',
    [Parameter(Mandatory, ParameterSetName = 'Show')]
    [switch]$Show,
    [Parameter(Mandatory, ParameterSetName = 'Withdraw')]
    [switch]$Withdraw,
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' }),
    # Where the steer is written (default <coord>\handoff.operator.json). orchestrate.ps1 -DryRun -Unit points it at a
    # temporary file, because a dry run must leave nothing behind for the next real start.
    [string]$OutFile = ''
)
$ErrorActionPreference = 'Stop'
$path = if ($OutFile) { $OutFile } else { Join-Path $CoordDir 'handoff.operator.json' }

if ($Show) {
    if (Test-Path $path) { Get-Content $path -Raw } else { Write-Host "no operator steer pending ($path)" }
    return
}
if ($Withdraw) {
    if (Test-Path $path) { Remove-Item $path -Force; Write-Host "operator steer withdrawn ($path)" } else { Write-Host "no operator steer pending ($path)" }
    return
}

$h = [ordered]@{ schema_version = 1; unit = 'operator'; outcome = 'done'; summary = $(if ($Summary) { $Summary } else { $Reason })
    next_unit = $Unit; next_unit_reason = $Reason }
$json = $h | ConvertTo-Json -Depth 5
$schema = Join-Path $PSScriptRoot 'handoff.schema.json'
$errors = $null
if (-not ($json | Test-Json -SchemaFile $schema -ErrorVariable errors -ErrorAction SilentlyContinue)) {
    Write-Error "steer refused: the operator handoff does not validate against handoff.schema.json: $($errors -join '; ')"
    exit 1
}
New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null
# Written to a sibling and renamed over the target, so the supervisor never reads a half-written steer.
$tmp = "$path.$PID.tmp"
Set-Content -Path $tmp -Value $json -Encoding utf8NoBOM
Move-Item -Path $tmp -Destination $path -Force
Write-Host "operator steer written ($path): the loop's next unit is '$Unit' ($Reason)"
