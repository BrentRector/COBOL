#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (ledger), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the owed ledger publish is announced and posted to the operator's mailbox once per stamp, and a
# landing pushed from ANOTHER clone while the supervisor's checkout stays behind is announced (kb/Work PB2987: the stamp
# is main's, fetched). The repository is a scratch clone of a scratch bare origin, never this machine's real one.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_ledger.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

function G { & git -c user.email=t@t -c user.name=t @args 2>&1 | Out-Null; if ($LASTEXITCODE) { throw "git $args failed" } }
function Land([string]$clone, [string]$file, [string]$text) {
    New-Item -ItemType Directory -Force -Path (Split-Path (Join-Path $clone $file)) | Out-Null
    Set-Content -Path (Join-Path $clone $file) -Value $text -Encoding utf8
    G -C $clone add -A; G -C $clone commit -q -m "land $file"; G -C $clone push -q origin main
}
$Origin = Join-Path $Root 'origin.git'
$LedgerRepo = Join-Path $Root 'checkout'
$Lander = Join-Path $Root 'lander'
G init -q --bare -b main $Origin
G init -q -b main $LedgerRepo
G -C $LedgerRepo remote add origin $Origin
Land $LedgerRepo 'kb/Work/PB1.md' 'a ledger input'
G clone -q $Origin $Lander

# 4i. PUBLISH THE LEDGER EACH TIME: a headless unit cannot publish, so the supervisor announces an owed publish after every unit
# (and says nothing once the current stamp is marked published)
$r = Run-Orch 'ledgerowed' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-RepoDir', $LedgerRepo)
Check 'an owed ledger publish is announced after a unit' ($r.out -match 'LEDGER PUBLISH OWED') $true
# ... and posted to the operator's mailbox (kb/Work PB2482), as a well-formed `publish` from the loop
$mb = @(Get-ChildItem (Join-Path $r.coord 'mailbox/to-operator') -Filter '*.json' -ErrorAction SilentlyContinue)
Check 'the owed publish is one message in the operator inbox' $mb.Count 1
if ($mb.Count) {
    $m = Get-Content $mb[0].FullName -Raw | ConvertFrom-Json
    Check 'the publish message: from the loop, kind publish, a ref to the unit log' "$($m.from) $($m.kind) $([bool](Test-Path $m.refs[0]))" 'loop publish True'
}
$r2 = Run-Orch 'ledgerowed' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-RepoDir', $LedgerRepo)
Check 'a second owed unit adds no second message for the same stamp' @(Get-ChildItem (Join-Path $r.coord 'mailbox/to-operator') -Filter '*.json').Count 1
$r = Run-Orch 'ledgercurrent' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-RepoDir', $LedgerRepo) {
    param($c) & python (Join-Path (Split-Path $Orch) 'ledger_state.py') mark-published --repo $LedgerRepo --coord $c --url 'https://claude.ai/artifact/test' | Out-Null }
Check 'a published ledger is not announced again' ($r.out -match 'LEDGER PUBLISH OWED') $false
# kb/Work PB2987: train 1056 landed from a lander's worktree, the supervisor's checkout stayed behind, and no publish was
# announced. A landing pushed from another clone is owed at the next unit, and the line names the stale checkout.
Land $Lander 'kb/Work/PB2.md' 'landed elsewhere'
$r = Run-Orch 'ledgercurrent' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-RepoDir', $LedgerRepo)
Check 'a landing pushed from another clone is announced owed (main, not the checkout)' ($r.out -match 'LEDGER PUBLISH OWED') $true
Check '... naming the stale checkout and its fast-forward' ($r.out -match 'merge --ff-only origin/main') $true

Complete-OrchTest 'ledger'
