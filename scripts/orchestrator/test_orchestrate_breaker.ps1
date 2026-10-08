#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (breaker), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the circuit breaker trips after three fast failures.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_breaker.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 5. the circuit breaker trips after three fast failures and leaves an owner note
$r = Run-Orch 'breaker' 'fastfail' @('-Unit', 'wave', '-FastFailSeconds', '120')
Check 'breaker exit' $r.code 4
Check 'breaker ran exactly three units' $r.runs 3
Check 'breaker owner note' ((Get-Content (Join-Path $r.coord 'OWNER-QUESTIONS.md') -Raw) -match 'circuit breaker') $true

Complete-OrchTest 'breaker'
