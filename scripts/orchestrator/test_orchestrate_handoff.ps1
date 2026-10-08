#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (handoff), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: frequent handoffs: a crashed unit's synthesized handoff, periodic checkpoints, a supervisor that died mid-unit.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_handoff.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 4h. FREQUENT HANDOFFS: a unit that dies with no handoff leaves a synthesized one for its successor (from the supervisor's
# checkpoint, the unit's milestone lines and the worktrees), and the checkpoint and milestones are archived with the logs
$r = Run-Orch 'crash' 'crash' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
$hl = Get-Content (Join-Path $r.coord 'handoff.last.json') -Raw | ConvertFrom-Json
Check 'a crashed unit still counts as failed' $r.units[0].failed $true
Check 'handoff outcome says synthesized' ($r.units[0].handoff_outcome -like 'synthesized*') $true
Check 'the synthesized handoff names resume as the next unit' $hl.next_unit 'resume'
Check 'the synthesized handoff is flagged' $hl.synthesized $true
Check 'the unit''s milestone reaches the synthesized summary' ($hl.summary -match 'plan written for wave 9') $true
Check 'the final checkpoint is archived with the log' (@(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*.checkpoint.json').Count) 1
Check 'the milestones are archived with the log' (@(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*.milestones.jsonl').Count) 1
Check 'no live checkpoint is left behind' (Test-Path (Join-Path $r.coord 'checkpoint.json')) $false

# a checkpoint every few seconds while a unit runs, and one when the background-task set changes (the `wakes` fake)
$r = Run-Orch 'periodic' 'wakes' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-CheckpointSeconds', '1')
$cp = Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*.checkpoint.json' | Select-Object -First 1 | Get-Content -Raw | ConvertFrom-Json
Check 'the archived checkpoint is the unit''s final state' ($cp.unit, $cp.calls) @('wave', 2)

# a supervisor that DIED mid-unit leaves checkpoint.json behind; the next start turns it into the missing handoff
$r = Run-Orch 'diedmid' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) Set-Content -Path (Join-Path $c 'checkpoint.json') -Value '{"schema_version":1,"unit":"wave","calls":77,"worktrees":[],"milestones":[]}' }
Check 'a leftover checkpoint is announced' ($r.out -match "survives from a supervisor that died mid-unit") $true
Check 'the leftover checkpoint is archived, not left to repeat' (@(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*wave.checkpoint.json').Count -ge 1) $true

Complete-OrchTest 'handoff'
