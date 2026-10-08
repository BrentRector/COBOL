# The shared harness of the orchestrate.ps1 self-tests (scripts/orchestrator/test_orchestrate_*.ps1), dot-sourced by
# each part with `$Here` set to scripts/orchestrator. The cases were ONE file until kb/Work PB2563 split them so the
# self-test runner (scripts/self_tests.py) runs them in parallel: one file took 3-6 minutes, longer than every other
# self-test together. It is not a self-test itself (it is not named test_*), so the runner never runs it alone.
# Each part ends with `Complete-OrchTest '<part>'`, which prints its count and exits 0 only when every check passed.
$Orch = Join-Path $Here 'orchestrate.ps1'
$Fake = Join-Path $Here 'testdata/fake-claude.ps1'
$Root = Join-Path ([IO.Path]::GetTempPath()) "orch-test-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
$Tele = Join-Path $Root 'telemetry'
New-Item -ItemType Directory -Force -Path $Tele | Out-Null
$script:fails = @()
$script:checked = 0

function Check([string]$name, $got, $want) {
    $script:checked++
    if ("$got" -ne "$want") { $script:fails += "${name}: got '$got', want '$want'" }
}

function Run-Orch([string]$case, [string]$mode, [string[]]$extra, [scriptblock]$before) {
    $coord = Join-Path $Root $case
    New-Item -ItemType Directory -Force -Path $coord | Out-Null
    $mark = Join-Path $coord 'fake-invocations.txt'
    if ($before) { & $before $coord }
    $env:FAKE_CLAUDE_MODE = $mode
    $env:FAKE_CLAUDE_MARK = $mark
    $out = & pwsh -NoProfile -File $Orch -ClaudeExe $Fake -CoordDir $coord -TelemetryDir $Tele -BackoffBaseSeconds 0 -IdleCloseSeconds 1 @extra 2>&1
    $code = $LASTEXITCODE
    $runs = if (Test-Path $mark) { @(Get-Content $mark).Count } else { 0 }
    $units = if (Test-Path (Join-Path $coord 'units.jsonl')) { @(Get-Content (Join-Path $coord 'units.jsonl') | ForEach-Object { $_ | ConvertFrom-Json }) } else { @() }
    return [pscustomobject]@{ code = $code; out = ($out -join "`n"); runs = $runs; units = $units; coord = $coord }
}

function Complete-OrchTest([string]$part) {
    Remove-Item $Root -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($f in $script:fails) { Write-Host "FAIL: $f" }
    Write-Host "orchestrate self-test ($part): $($script:checked - $script:fails.Count)/$($script:checked) checks OK"
    exit $(if ($script:fails.Count) { 1 } else { 0 })
}
