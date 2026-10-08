#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (prompt), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: -BorrowDays and the session's tasks directory reach the wave unit's prompt.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_prompt.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 4c. the owner's -BorrowDays reaches the wave unit's plan_wave call (the supervisor's gate alone is not enough)
$r = Run-Orch 'borrow' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-BorrowDays', '2')
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
Check 'borrow days substituted into the unit prompt' ($inv -match '--borrow-days 2 --scratch') $true
Check 'the wave unit plans with the loop''s own fleet stop' ($inv -match 'scratch[\\/]+STOP-loop` plans the wave') $true
Check 'no stop placeholder left in the prompt' ($inv -match '\{(FLEET|GLOBAL)_STOP\}') $false

# 4d. the wave unit's prompt names the Workflow task-output directory of ITS session, so it can block on the result
$r = Run-Orch 'tasksdir' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
$sid = [regex]::Match($inv, '--session-id \| ([0-9a-f-]{36})').Groups[1].Value
Check 'tasks dir names the unit session' ($inv -match [regex]::Escape("claude/E--COBOL/$sid/tasks")) $true
Check 'no placeholder left in the prompt' ($inv -match '\{TASKS_DIR\}') $false

Complete-OrchTest 'prompt'
