#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (wakes), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the supervisor owns the session's lifetime: a turn that ends with a background task running is woken, and stdin is closed only once the stream is idle.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_wakes.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 4e. the SUPERVISOR owns the session's lifetime: a turn that ends with a background task running does not end the unit,
# the task's completion wakes the model (a second call), and stdin is closed only once the stream is idle with no task
$r = Run-Orch 'wakes' 'wakes' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'woken unit exit' $r.code 0
Check 'woken unit made both calls' $r.units[0].calls 2
Check 'woken unit handed off done' $r.units[0].handoff_outcome 'done'
Check 'supervisor closed stdin when idle' (Test-Path (Join-Path $r.coord 'eof.txt')) $true
$r = Run-Orch 'goodeof' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'a finished unit is closed by the supervisor, not left running' (Test-Path (Join-Path $r.coord 'eof.txt')) $true

Complete-OrchTest 'wakes'
