#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (winddown), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: STOP while a unit runs winds it down without losing it; a stale fleet stop is cleared, the global stop ends the loop and is never removed.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_winddown.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 4f. STOP while a unit runs closes work down asap WITHOUT losing it: the supervisor creates STOP-UNIT and the LOOP's own
# fleet stop scratch/STOP-loop (never the owner's global scratch/STOP, which other sessions' agents obey; kb/Work PB2483),
# the unit checkpoints and hands off (split, next unit resume), and the loop then ends instead of starting another unit
$r = Run-Orch 'stopnow' 'stopsme' @('-Unit', 'wave', '-FastFailSeconds', '0')
Check 'stop during a unit exit' $r.code 0
Check 'stop during a unit ran exactly one unit' $r.runs 1
Check 'fleet STOP existed when the unit wound down' ((Get-Content (Join-Path $r.coord 'fleet-stop-seen.txt') -Raw).Trim()) 'True'
Check 'the wind-down did not create the global STOP' ((Get-Content (Join-Path $r.coord 'global-stop-seen.txt') -Raw).Trim()) 'False'
Check 'the unit was not killed' $r.units[0].killed $false
Check 'the wind-down handoff is kept for resume' ((Get-Content (Join-Path $r.coord 'handoff.last.json') -Raw) -match '"next_unit":\s*"resume"') $true
Check 'the owner STOP file is left in place' (Test-Path (Join-Path $r.coord 'STOP')) $true
# a fleet STOP left by that wind-down must not stop the NEXT unit's fleet at its first step
$r = Run-Orch 'stalefleetstop' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) New-Item -ItemType Directory -Force -Path (Join-Path $c 'scratch') | Out-Null
    Set-Content -Path (Join-Path $c 'scratch/STOP-loop') -Value 'stale'
    Set-Content -Path (Join-Path $c 'scratch/STOP-w77') -Value 'another session' }
Check 'a stale fleet STOP is cleared at unit start' (Test-Path (Join-Path $r.coord 'scratch/STOP-loop')) $false
Check 'another fleet''s stop is not the supervisor''s to clear' (Test-Path (Join-Path $r.coord 'scratch/STOP-w77')) $true
# the owner's GLOBAL stop ends the loop before any unit, and the supervisor never removes it
$r = Run-Orch 'globalstop' 'good' @('-Unit', 'wave') {
    param($c) New-Item -ItemType Directory -Force -Path (Join-Path $c 'scratch') | Out-Null
    Set-Content -Path (Join-Path $c 'scratch/STOP') -Value 'owner' }
Check 'the global STOP ends the loop at once' "$($r.code) $($r.runs)" '0 0'
Check 'the global STOP is left in place' (Test-Path (Join-Path $r.coord 'scratch/STOP')) $true

Complete-OrchTest 'winddown'
