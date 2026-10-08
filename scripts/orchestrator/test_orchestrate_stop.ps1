#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (stop), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: stop.ps1, a second instance refused, the context cap and a subagent's context, an owner question.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_stop.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 4g. stop.ps1: creates STOP, reports status, and -Clear removes STOP, STOP-UNIT and the fleet STOP
$sc = Join-Path $Root 'stopscript'
New-Item -ItemType Directory -Force -Path (Join-Path $sc 'scratch') | Out-Null
$StopPs = Join-Path (Split-Path $Orch) 'stop.ps1'
& pwsh -NoProfile -File $StopPs -CoordDir $sc | Out-Null
Check 'stop.ps1 creates STOP' (Test-Path (Join-Path $sc 'STOP')) $true
Check 'stop.ps1 -Status reports it' ((& pwsh -NoProfile -File $StopPs -Status -CoordDir $sc) -join ' ' -match 'STOP pending: True') $true
Check 'stop.ps1 does not create the global stop' (Test-Path (Join-Path $sc 'scratch/STOP')) $false
Set-Content -Path (Join-Path $sc 'STOP-UNIT') -Value 'x'; Set-Content -Path (Join-Path $sc 'scratch/STOP-loop') -Value 'x'
& pwsh -NoProfile -File $StopPs -Clear -CoordDir $sc | Out-Null
Check 'stop.ps1 -Clear removes all three' (@('STOP', 'STOP-UNIT', 'scratch/STOP-loop') | Where-Object { Test-Path (Join-Path $sc $_) }).Count 0
& pwsh -NoProfile -File $StopPs -Global -CoordDir $sc | Out-Null
Check 'stop.ps1 -Global creates the loop and the global stop' "$(Test-Path (Join-Path $sc 'STOP')) $(Test-Path (Join-Path $sc 'scratch/STOP'))" 'True True'
Check 'stop.ps1 -Status names the global stop' ((& pwsh -NoProfile -File $StopPs -Status -CoordDir $sc) -join ' ' -match 'GLOBAL stop \(scratch/STOP\): True') $true
& pwsh -NoProfile -File $StopPs -Clear -CoordDir $sc | Out-Null
Check 'stop.ps1 -Clear leaves the global stop' (Test-Path (Join-Path $sc 'scratch/STOP')) $true
& pwsh -NoProfile -File $StopPs -Clear -Global -CoordDir $sc | Out-Null
Check 'stop.ps1 -Clear -Global removes it' (Test-Path (Join-Path $sc 'scratch/STOP')) $false

# 6. a second instance is refused while the first one's PID is alive (this test's own PID stands in for it)
$r = Run-Orch 'second' 'good' @('-Unit', 'wave') {
    param($c) Set-Content -Path (Join-Path $c 'orchestrate.lock') -Value (@{ pid = $PID; started_at = 'now' } | ConvertTo-Json -Compress) }
Check 'second instance exit' $r.code 3
Check 'second instance started nothing' $r.runs 0
Check 'live lock kept' (Test-Path (Join-Path $r.coord 'orchestrate.lock')) $true
# ...and a lock left by a dead process is stale and taken over
$r = Run-Orch 'stale' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) Set-Content -Path (Join-Path $c 'orchestrate.lock') -Value (@{ pid = 999999; started_at = 'long ago' } | ConvertTo-Json -Compress) }
Check 'stale lock taken over' $r.runs 1

# 7. the context cap sends STOP-UNIT and the unit hands off gracefully (no kill)
$r = Run-Orch 'cap' 'bigcontext' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-MaxContextTokens', '1000')
Check 'cap: STOP-UNIT sent' $r.units[0].stop_unit_sent $true
Check 'cap: not killed' $r.units[0].killed $false
Check 'cap: handoff split' $r.units[0].handoff_outcome 'split'

# 7b. a SUBAGENT's context past the cap is not the unit's context: the unit is not wound down
$r = Run-Orch 'capsub' 'bigsubagent' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-MaxContextTokens', '50000')
Check 'capsub: no STOP-UNIT' $r.units[0].stop_unit_sent $false
Check 'capsub: handoff done' $r.units[0].handoff_outcome 'done'
Check 'capsub: subagent peak recorded' ($r.units[0].peak_subagent_context -gt 900000) $true

# 8. an owner question stops the loop and is written down
$r = Run-Orch 'owner' 'owner' @('-Unit', 'resume', '-FastFailSeconds', '0')
Check 'owner exit' $r.code 5
Check 'owner ran once' $r.runs 1
Check 'owner question written' ((Get-Content (Join-Path $r.coord 'OWNER-QUESTIONS.md') -Raw) -match 'reading A or reading B') $true

Complete-OrchTest 'stop'
