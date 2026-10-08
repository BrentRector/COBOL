#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (ledger), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the owed ledger publish is announced and posted to the operator's mailbox once per stamp.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_ledger.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 4i. PUBLISH THE LEDGER EACH TIME: a headless unit cannot publish, so the supervisor announces an owed publish after every unit
# (and says nothing once the current stamp is marked published)
$r = Run-Orch 'ledgerowed' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'an owed ledger publish is announced after a unit' ($r.out -match 'LEDGER PUBLISH OWED') $true
# ... and posted to the operator's mailbox (kb/Work PB2482), as a well-formed `publish` from the loop
$mb = @(Get-ChildItem (Join-Path $r.coord 'mailbox/to-operator') -Filter '*.json' -ErrorAction SilentlyContinue)
Check 'the owed publish is one message in the operator inbox' $mb.Count 1
if ($mb.Count) {
    $m = Get-Content $mb[0].FullName -Raw | ConvertFrom-Json
    Check 'the publish message: from the loop, kind publish, a ref to the unit log' "$($m.from) $($m.kind) $([bool](Test-Path $m.refs[0]))" 'loop publish True'
}
$r2 = Run-Orch 'ledgerowed' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'a second owed unit adds no second message for the same stamp' @(Get-ChildItem (Join-Path $r.coord 'mailbox/to-operator') -Filter '*.json').Count 1
$r = Run-Orch 'ledgercurrent' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) & python (Join-Path (Split-Path $Orch) 'ledger_state.py') mark-published --coord $c --url 'https://claude.ai/artifact/test' | Out-Null }
Check 'a published ledger is not announced again' ($r.out -match 'LEDGER PUBLISH OWED') $false

Complete-OrchTest 'ledger'
