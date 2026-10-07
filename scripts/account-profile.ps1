#Requires -Version 7.5
<#
.SYNOPSIS
  Seed (or repair) a named Claude account's config dir from the default account's, so a second account's session has the
  same settings, plugins, skills, project memory and feature flags (kb/Work PB2480).
.DESCRIPTION
  What the dir must carry is DATA, in scripts/orchestrator/model_rules.json `accounts.seed`; this script asks
  scripts/orchestrator/account.py for the concrete plan (--seed-plan) and carries it out, then verifies it with the same
  module (--seed-check), which tooling_check.py also runs at every session start. So the list lives in one place.
    copy      settings.json, config.json, plugins\, skills\ from the default account's dir: a missing file is copied, a
              missing child of a directory is copied; what the account already has is KEPT (it may differ on purpose),
              and -Force overwrites the top-level files only.
    junction  projects\<repo key>\memory -> the default account's, so both sessions read and write one memory. A junction
              pointing elsewhere is re-pointed; a real directory there is refused (its notes would be hidden).
    flags     the non-secret feature flags in the account's .claude.json (claudeInChromeDefaultEnabled,
              hasCompletedClaudeInChromeOnboarding), so the session has the Claude in Chrome tools.
    never     credentials (.credentials.json, the oauthAccount): the owner signs the account in with /login.
  Idempotent: a re-run repairs what is missing and changes nothing else. Run it while no session of that account is
  running (Claude Code rewrites .claude.json as it runs, and a concurrent write could be lost; a re-run repairs that).

  CLAUDE IN CHROME under a second account (measured 2026-10-07): the native messaging host Chrome starts is
  ~\.claude\chrome\chrome-native-host.bat, which runs `claude.exe --chrome-native-host` with no config dir, and the
  bridge it serves is the named pipe \\.\pipe\claude-mcp-browser-bridge-<Windows user name>: keyed by the WINDOWS USER,
  not by the Claude account or config dir. So one pairing serves a session of either account once its .claude.json has
  the flags above. What the browser shows is the claude.ai account the BROWSER is signed into, which may be the other
  account: the meter unit (units/meter.md) refuses a usage page whose email is not this session's
  (`account.py --field email`). Fallback when the tools are absent: `/usage` in that account's own terminal.

.EXAMPLE
  pwsh -NoProfile -File scripts/account-profile.ps1 -ConfigDir C:\Users\brent\.claude-acct2
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ConfigDir,
    # Overwrite the top-level files of `copy` even when the account has its own (directories are only ever completed).
    [switch]$Force,
    [string]$Python = 'python',
    # Test seams: another model_rules.json and repository (test_orchestrate.ps1 seeds a temp account from a temp source).
    [string]$RulesFile = '',
    [string]$RepoDir = (Split-Path $PSScriptRoot -Parent)
)
$ErrorActionPreference = 'Stop'
$Account = Join-Path $PSScriptRoot 'orchestrator/account.py'
$common = @('--config-dir', $ConfigDir, '--repo', $RepoDir) + $(if ($RulesFile) { @('--rules', $RulesFile) } else { @() })

$out = & $Python $Account @common --seed-plan 2>&1
if ($LASTEXITCODE -ne 0) { throw "account-profile: $($out -join ' ')" }
$plan = ($out -join "`n") | ConvertFrom-Json
if ($plan.source) { Write-Host "$($plan.account) is the default account, the source of every seed: nothing to do"; return }

New-Item -ItemType Directory -Force -Path $ConfigDir | Out-Null
foreach ($c in $plan.copy) {
    if (-not (Test-Path $c.from)) { Write-Host "skip     $($c.to): the source $($c.from) does not exist"; continue }
    if (Test-Path $c.from -PathType Container) {
        New-Item -ItemType Directory -Force -Path $c.to | Out-Null
        $added = 0
        foreach ($child in Get-ChildItem -LiteralPath $c.from -Force) {
            $dest = Join-Path $c.to $child.Name
            if (-not (Test-Path -LiteralPath $dest)) { Copy-Item -LiteralPath $child.FullName -Destination $dest -Recurse; $added++ }
        }
        Write-Host ("{0,-8} {1}" -f $(if ($added) { 'copied' } else { 'ok' }), "$($c.to)$(if ($added) { " ($added missing item(s) from $($c.from))" })")
    } elseif (-not (Test-Path $c.to) -or $Force) {
        Copy-Item -LiteralPath $c.from -Destination $c.to -Force
        Write-Host "copied   $($c.to)"
    } else {
        Write-Host "kept     $($c.to) (the account's own; -Force overwrites it)"
    }
}
foreach ($j in $plan.junction) {
    $item = Get-Item -LiteralPath $j.link -Force -ErrorAction SilentlyContinue
    if ($item -and $item.LinkType -eq 'Junction' -and ((@($item.Target)[0]).TrimEnd('\') -eq $j.target.TrimEnd('\'))) {
        Write-Host "ok       $($j.link) -> $($j.target)"; continue
    }
    if ($item -and -not $item.LinkType) {
        throw "account-profile: $($j.link) is a real directory, not a junction: move its notes into $($j.target) and re-run"
    }
    if ($item) { $item.Delete() }   # a link to the wrong place: removes the link only, never its target's contents
    New-Item -ItemType Directory -Force -Path (Split-Path $j.link -Parent) | Out-Null
    New-Item -ItemType Junction -Path $j.link -Target $j.target | Out-Null
    Write-Host "linked   $($j.link) -> $($j.target)"
}
if ($plan.flags) {
    $path = $plan.flags.path
    # -DateKind String keeps every timestamp exactly as written; the object keeps the file's key order.
    $cfg = if (Test-Path $path) { Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -DateKind String -Depth 100 } else { [pscustomobject]@{} }
    $changed = @()
    foreach ($p in $plan.flags.set.PSObject.Properties) {
        if (-not $cfg.PSObject.Properties[$p.Name] -or $cfg.($p.Name) -ne $p.Value) {
            $cfg | Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value -Force
            $changed += $p.Name
        }
    }
    if ($changed) {
        $tmp = "$path.$PID.tmp"
        Set-Content -LiteralPath $tmp -Value ($cfg | ConvertTo-Json -Depth 100) -Encoding utf8NoBOM
        Move-Item -LiteralPath $tmp -Destination $path -Force
        Write-Host "flagged  $path ($($changed -join ', '))"
    } else { Write-Host "ok       $path (flags set)" }
}
$check = & $Python $Account @common --seed-check 2>&1
Write-Host ($check -join "`n")
exit $LASTEXITCODE
