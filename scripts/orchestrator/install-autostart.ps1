#Requires -Version 7.0
<#
.SYNOPSIS
Install (or remove) the logon entry that starts the orchestrator loop in a Windows Terminal tab. Run by the OWNER, once.

.DESCRIPTION
Writes ONE file into the Startup folder, `cobolnet-autoresume.cmd` (the name the old entry used, so it is replaced in place),
which opens Windows Terminal in the repo and runs scripts/orchestrator/autostart.ps1 from the repository. The script itself lives in
git, so a change to it is a reviewed commit and not a hand edit under %LOCALAPPDATA%. The old entry's script
(%LOCALAPPDATA%\CobolNet\cobolnet-autoresume.ps1) is moved, with the old .cmd, into %LOCALAPPDATA%\CobolNet\replaced\ so the owner can
restore it (a rollback copy, not a compatibility layer: nothing calls it any more).

-Uninstall removes the new entry and leaves the Startup folder without one. -DryRun prints the file it would write.
Idempotent: running it twice writes the same file.
#>
[CmdletBinding()]
param(
    [string]$StartupDir = [Environment]::GetFolderPath('Startup'),
    [string]$RepoDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [string]$OldDir = (Join-Path $env:LOCALAPPDATA 'CobolNet'),
    [string]$Wt = (Join-Path $env:LOCALAPPDATA 'Microsoft/WindowsApps/wt.exe'),
    [switch]$Uninstall,
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'

function Say([string]$m) { Write-Host $m }

$Cmd = Join-Path $StartupDir 'cobolnet-autoresume.cmd'
$Script = Join-Path (Join-Path $RepoDir 'scripts') 'orchestrator/autostart.ps1'
if (-not (Test-Path $Script)) { throw "autostart.ps1 not found at $Script" }

$content = @"
@echo off
rem COBOL orchestrator: after desktop login open Windows Terminal in the repo and start the supervisor loop
rem (scripts/orchestrator/autostart.ps1, in git). Replaces the old interactive-session auto-resume (owner 2026-10-04).
rem To stop it from starting: create E:\COBOL-coord\STOP, or run scripts/orchestrator/install-autostart.ps1 -Uninstall.
"$Wt" -w cobol-supervisor -d "$RepoDir" pwsh -NoExit -NoProfile -ExecutionPolicy Bypass -File "$Script"
"@

if ($Uninstall) {
    if ($DryRun) { Say "DRY RUN: would delete $Cmd"; return }
    if (Test-Path $Cmd) { Remove-Item $Cmd -Force; Say "removed $Cmd" } else { Say "nothing to remove: $Cmd does not exist" }
    return
}

if ($DryRun) {
    Say "DRY RUN: would move the old entry's files from $OldDir to $OldDir\replaced and write $Cmd :"
    Say $content
    return
}

$replaced = Join-Path $OldDir 'replaced'
$oldPs1 = Join-Path $OldDir 'cobolnet-autoresume.ps1'
if (Test-Path $oldPs1) {
    New-Item -ItemType Directory -Force -Path $replaced | Out-Null
    Move-Item $oldPs1 (Join-Path $replaced 'cobolnet-autoresume.ps1') -Force
    Say "moved the old script to $replaced"
}
if ((Test-Path $Cmd) -and -not ((Get-Content $Cmd -Raw) -match 'orchestrator/autostart\.ps1')) {
    New-Item -ItemType Directory -Force -Path $replaced | Out-Null
    Copy-Item $Cmd (Join-Path $replaced 'cobolnet-autoresume.cmd') -Force
    Say "kept a copy of the old Startup entry in $replaced"
}
New-Item -ItemType Directory -Force -Path $StartupDir | Out-Null
Set-Content -Path $Cmd -Value $content -Encoding ascii
Say "wrote $Cmd"
Say "It runs at the next logon. Test it now without rebooting:  pwsh -File $Script -DryRun"
