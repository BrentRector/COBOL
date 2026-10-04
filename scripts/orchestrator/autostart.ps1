#Requires -Version 7.0
<#
.SYNOPSIS
Start the orchestrator loop after logon (the reboot path). Launched in a Windows Terminal tab by the Startup entry that
install-autostart.ps1 writes. Design: docs/rearchitecture/DESIGN-orchestrator-loop.md, section "Starting at logon".

.DESCRIPTION
Replaces the old %LOCALAPPDATA%\CobolNet\cobolnet-autoresume.ps1 (an interactive `claude` session whose prompt had to re-create a
session-only cron job). The supervisor needs no cron: it owns its own timing (budget.py holds until the session reset or until
03:05 the next day) and every unit is a fresh session chained by a handoff file, so a reboot loses at most the unit that was running.

Order: wait for the desktop, honour the STOP file, fast-forward the checkout when it is clean, run orchestrate.ps1 in THIS window
(so the owner can watch it), and print what its exit code means. -DryRun prints each step and starts nothing.
#>
[CmdletBinding()]
param(
    [int]$DelaySeconds = 25,
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' }),
    [string]$RepoDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [switch]$DryRun,
    # Extra arguments for orchestrate.ps1 (for example -MaxUnits 1); the Startup entry passes none.
    [string[]]$SupervisorArgs = @()
)
$ErrorActionPreference = 'Stop'

function Say([string]$m) { Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $m" }

$Stop = Join-Path $CoordDir 'STOP'
$Questions = Join-Path $CoordDir 'OWNER-QUESTIONS.md'
$Supervisor = Join-Path (Join-Path $RepoDir 'scripts') 'orchestrator/orchestrate.ps1'

if ($DelaySeconds -gt 0 -and -not $DryRun) {
    Say "waiting $DelaySeconds s for the desktop, network and WSL to come up"
    Start-Sleep -Seconds $DelaySeconds
}
New-Item -ItemType Directory -Force -Path $CoordDir | Out-Null

if (Test-Path $Stop) {
    Say "STOP is present ($Stop): not starting. Delete it to let the loop run."
    return
}
if (Test-Path $Questions) {
    Say "an owner question is waiting in $Questions; read and answer it, delete the file, then start the loop."
    Get-Content $Questions -TotalCount 30
    return
}

# Bring the checkout up to date only when nothing but the always-dirty settings file is modified (never stash, never reset).
$dirty = @(& git -C $RepoDir status --porcelain 2>$null | Where-Object { $_ -notmatch 'settings\.local\.json' })
if ($dirty.Count -eq 0) {
    if ($DryRun) { Say "DRY RUN: git -C $RepoDir pull --ff-only" }
    else {
        $out = & git -C $RepoDir pull --ff-only 2>&1
        Say "git pull --ff-only: $(($out | Select-Object -Last 1))"
    }
}
else { Say "the checkout has $($dirty.Count) uncommitted change(s); skipping the pull" }

$argsList = @('-NoProfile', '-File', $Supervisor, '-Watch') + $SupervisorArgs
if ($DryRun) {
    Say "DRY RUN: pwsh $($argsList -join ' ')"
    return
}
Say "starting the supervisor: pwsh $($argsList -join ' ')"
& pwsh @argsList
$code = $LASTEXITCODE
$meaning = switch ($code) {
    0 { 'stopped normally (STOP file, -MaxUnits, stop-week budget)' }
    3 { 'another supervisor instance is already running' }
    4 { 'the circuit breaker tripped (three failed units); read the newest log in the coordination directory' }
    5 { "an owner question is waiting in $Questions" }
    default { 'unexpected exit code' }
}
Say "supervisor exited with ${code}: $meaning"
if ($code -eq 5 -and (Test-Path $Questions)) { Get-Content $Questions -TotalCount 40 }
