#Requires -Version 7.0
# Self-test for orchestrate.ps1, driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real session.
# Proves: one unit runs and is recorded; the handoff is validated (a missing one is a failure); the STOP file ends the
# loop; the circuit breaker trips after three fast failures; a second instance is refused; -DryRun starts nothing;
# the context cap sends STOP-UNIT and the unit hands off; an owner question stops the loop; open-watchers.ps1 opens
# one titled, model-coloured Windows Terminal tab per agent (against a fake wt.exe).
# Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate.ps1
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
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
    $out = & pwsh -NoProfile -File $Orch -ClaudeExe $Fake -CoordDir $coord -TelemetryDir $Tele -BackoffBaseSeconds 0 @extra 2>&1
    $code = $LASTEXITCODE
    $runs = if (Test-Path $mark) { @(Get-Content $mark).Count } else { 0 }
    $units = if (Test-Path (Join-Path $coord 'units.jsonl')) { @(Get-Content (Join-Path $coord 'units.jsonl') | ForEach-Object { $_ | ConvertFrom-Json }) } else { @() }
    return [pscustomobject]@{ code = $code; out = ($out -join "`n"); runs = $runs; units = $units; coord = $coord }
}

# 1. -DryRun starts nothing
$r = Run-Orch 'dryrun' 'good' @('-DryRun', '-Unit', 'wave')
Check 'dry run exit' $r.code 0
Check 'dry run started nothing' $r.runs 0
Check 'dry run says what it would do' ($r.out -match "would run unit 'wave'") $true
Check 'dry run names the model' ($r.out -match '--model opus') $true

# 2. one unit runs, its handoff is validated and archived, its usage recorded
$r = Run-Orch 'one' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'one unit exit' $r.code 0
Check 'one unit ran once' $r.runs 1
Check 'units.jsonl line' $r.units.Count 1
Check 'handoff outcome recorded' $r.units[0].handoff_outcome 'done'
Check 'calls counted once per message id' $r.units[0].calls 2
Check 'output tokens summed' $r.units[0].output 400
Check 'peak context = input + cache read + cache write' $r.units[0].peak_context 8003
Check 'cost from the result event' $r.units[0].cost_usd 0.42
Check 'handoff archived as the last handoff' (Test-Path (Join-Path $r.coord 'handoff.last.json')) $true
Check 'stream logged' (Test-Path $r.units[0].log) $true
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
Check 'headless flags passed' ($inv -match '--output-format \| stream-json' -and $inv -match '--permission-prompts \| none' -and $inv -match '--session-id \| [0-9a-f-]{36}') $true
Check 'fable never selected' ($inv -match '--model \| fable') $false
Check 'lock released' (Test-Path (Join-Path $r.coord 'orchestrate.lock')) $false

# 3. a unit that writes no handoff is a failure
$r = Run-Orch 'nohandoff' 'nohandoff' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'missing handoff recorded' $r.units[0].handoff_outcome 'missing'
Check 'missing handoff is a failure' $r.units[0].failed $true

# 4. the STOP file ends the loop before any unit
$r = Run-Orch 'stop' 'good' @('-Unit', 'wave') { param($c) New-Item -ItemType File -Path (Join-Path $c 'STOP') | Out-Null }
Check 'STOP exit' $r.code 0
Check 'STOP started nothing' $r.runs 0
Check 'STOP reported' ($r.out -match 'STOP file present') $true

# 4b. a short unit that hands off `done` is not a fast failure (the meter unit takes about 40 s)
$r = Run-Orch 'quickdone' 'good' @('-Unit', 'meter', '-MaxUnits', '1', '-FastFailSeconds', '120')
Check 'quick done unit not failed' $r.units[0].failed $false

# 4c. the owner's -BorrowDays reaches the wave unit's plan_wave call (the supervisor's gate alone is not enough)
$r = Run-Orch 'borrow' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-BorrowDays', '2')
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
Check 'borrow days substituted into the unit prompt' ($inv -match '--borrow-days 2 --scratch') $true

# 5. the circuit breaker trips after three fast failures and leaves an owner note
$r = Run-Orch 'breaker' 'fastfail' @('-Unit', 'wave', '-FastFailSeconds', '120')
Check 'breaker exit' $r.code 4
Check 'breaker ran exactly three units' $r.runs 3
Check 'breaker owner note' ((Get-Content (Join-Path $r.coord 'OWNER-QUESTIONS.md') -Raw) -match 'circuit breaker') $true

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

# 8. an owner question stops the loop and is written down
$r = Run-Orch 'owner' 'owner' @('-Unit', 'resume', '-FastFailSeconds', '0')
Check 'owner exit' $r.code 5
Check 'owner ran once' $r.runs 1
Check 'owner question written' ((Get-Content (Join-Path $r.coord 'OWNER-QUESTIONS.md') -Raw) -match 'reading A or reading B') $true

# 9. open-watchers.ps1: one tab per agent whose meta.json exists, titled and coloured by model
$wf = Join-Path $Root 'session/subagents/workflows/wf_test-1'
New-Item -ItemType Directory -Force -Path $wf | Out-Null
'{}' | Set-Content (Join-Path $wf 'agent-a1.jsonl'); '{"description":"impl-X-PB1407","model":"sonnet"}' | Set-Content (Join-Path $wf 'agent-a1.meta.json')
'{}' | Set-Content (Join-Path $wf 'agent-a2.jsonl'); '{"description":"impl-Z2-PB988","model":"opus"}' | Set-Content (Join-Path $wf 'agent-a2.meta.json')
'{}' | Set-Content (Join-Path $wf 'agent-a3.jsonl')     # no meta.json yet: waits for the next scan
$env:FAKE_WT_MARK = Join-Path $Root 'wt.txt'
$Watchers = Join-Path $Here 'open-watchers.ps1'
& pwsh -NoProfile -File $Watchers -SessionDir (Join-Path $Root 'session') -WtExe (Join-Path $Here 'testdata/fake-wt.ps1') -Once | Out-Null
$tabs = @(Get-Content $env:FAKE_WT_MARK)
Check 'watchers: one tab per agent with a meta.json' $tabs.Count 2
Check 'watchers: sonnet tab' ($tabs[0] -match '^-w \| cobol-agents \| new-tab \| --title \| X PB1407 \| --tabColor \| #1E66F5 \| pwsh .*watch-agent\.ps1 \| .*agent-a1\.jsonl$') $true
Check 'watchers: opus tab' ($tabs[1] -match '--title \| Z2 PB988 \| --tabColor \| #FE640B') $true
& python (Join-Path $Here 'watch_agent.py') (Join-Path $wf 'agent-a1.jsonl') --once | Out-Null
Check 'watchers: the renderer reads a transcript' $LASTEXITCODE 0

Remove-Item $Root -Recurse -Force -ErrorAction SilentlyContinue
foreach ($f in $script:fails) { Write-Host "FAIL: $f" }
Write-Host "orchestrate self-test: $($script:checked - $script:fails.Count)/$($script:checked) checks OK"
exit $(if ($script:fails.Count) { 1 } else { 0 })
