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
    $out = & pwsh -NoProfile -File $Orch -ClaudeExe $Fake -CoordDir $coord -TelemetryDir $Tele -BackoffBaseSeconds 0 -IdleCloseSeconds 1 @extra 2>&1
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
Check 'missing handoff recorded, and replaced by a synthesized one' $r.units[0].handoff_outcome 'synthesized (missing)'
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

# 4d. the wave unit's prompt names the Workflow task-output directory of ITS session, so it can block on the result
$r = Run-Orch 'tasksdir' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
$sid = [regex]::Match($inv, '--session-id \| ([0-9a-f-]{36})').Groups[1].Value
Check 'tasks dir names the unit session' ($inv -match [regex]::Escape("claude/E--COBOL/$sid/tasks")) $true
Check 'no placeholder left in the prompt' ($inv -match '\{TASKS_DIR\}') $false

# 4e. the SUPERVISOR owns the session's lifetime: a turn that ends with a background task running does not end the unit,
# the task's completion wakes the model (a second call), and stdin is closed only once the stream is idle with no task
$r = Run-Orch 'wakes' 'wakes' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'woken unit exit' $r.code 0
Check 'woken unit made both calls' $r.units[0].calls 2
Check 'woken unit handed off done' $r.units[0].handoff_outcome 'done'
Check 'supervisor closed stdin when idle' (Test-Path (Join-Path $r.coord 'eof.txt')) $true
$r = Run-Orch 'goodeof' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'a finished unit is closed by the supervisor, not left running' (Test-Path (Join-Path $r.coord 'eof.txt')) $true

# 4f. STOP while a unit runs closes work down asap WITHOUT losing it: the supervisor creates STOP-UNIT and the fleet STOP,
# the unit checkpoints and hands off (split, next unit resume), and the loop then ends instead of starting another unit
$r = Run-Orch 'stopnow' 'stopsme' @('-Unit', 'wave', '-FastFailSeconds', '0')
Check 'stop during a unit exit' $r.code 0
Check 'stop during a unit ran exactly one unit' $r.runs 1
Check 'fleet STOP existed when the unit wound down' ((Get-Content (Join-Path $r.coord 'fleet-stop-seen.txt') -Raw).Trim()) 'True'
Check 'the unit was not killed' $r.units[0].killed $false
Check 'the wind-down handoff is kept for resume' ((Get-Content (Join-Path $r.coord 'handoff.last.json') -Raw) -match '"next_unit":\s*"resume"') $true
Check 'the owner STOP file is left in place' (Test-Path (Join-Path $r.coord 'STOP')) $true
# a fleet STOP left by that wind-down must not stop the NEXT unit's fleet at its first step
$r = Run-Orch 'stalefleetstop' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) New-Item -ItemType Directory -Force -Path (Join-Path $c 'scratch') | Out-Null
    Set-Content -Path (Join-Path $c 'scratch/STOP') -Value 'stale' }
Check 'a stale fleet STOP is cleared at unit start' (Test-Path (Join-Path $r.coord 'scratch/STOP')) $false

# 4g. stop.ps1: creates STOP, reports status, and -Clear removes STOP, STOP-UNIT and the fleet STOP
$sc = Join-Path $Root 'stopscript'
New-Item -ItemType Directory -Force -Path (Join-Path $sc 'scratch') | Out-Null
$StopPs = Join-Path (Split-Path $Orch) 'stop.ps1'
& pwsh -NoProfile -File $StopPs -CoordDir $sc | Out-Null
Check 'stop.ps1 creates STOP' (Test-Path (Join-Path $sc 'STOP')) $true
Check 'stop.ps1 -Status reports it' ((& pwsh -NoProfile -File $StopPs -Status -CoordDir $sc) -join ' ' -match 'STOP pending: True') $true
Set-Content -Path (Join-Path $sc 'STOP-UNIT') -Value 'x'; Set-Content -Path (Join-Path $sc 'scratch/STOP') -Value 'x'
& pwsh -NoProfile -File $StopPs -Clear -CoordDir $sc | Out-Null
Check 'stop.ps1 -Clear removes all three' (@('STOP', 'STOP-UNIT', 'scratch/STOP') | Where-Object { Test-Path (Join-Path $sc $_) }).Count 0

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

# 4i. PUBLISH THE LEDGER EACH TIME: a headless unit cannot publish, so the supervisor announces an owed publish after every unit
# (and says nothing once the current stamp is marked published)
$r = Run-Orch 'ledgerowed' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
Check 'an owed ledger publish is announced after a unit' ($r.out -match 'LEDGER PUBLISH OWED') $true
$r = Run-Orch 'ledgercurrent' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) & python (Join-Path (Split-Path $Orch) 'ledger_state.py') mark-published --coord $c | Out-Null }
Check 'a published ledger is not announced again' ($r.out -match 'LEDGER PUBLISH OWED') $false

# 4j. NO PIPE DEADLOCK: the real CLI writes a large `init` event before it reads stdin, and the wave prompt is larger than the
# stdin pipe buffer. A supervisor that writes the prompt synchronously before reading stdout deadlocks with it (the
# 2026-10-05 wave unit sat two hours with a 0-byte log). The fake writes 300 KB first; the unit must still finish, and the run
# is bounded so a regression FAILS instead of hanging the suite.
$bigDir = Join-Path $Root 'bigstart'
New-Item -ItemType Directory -Force -Path $bigDir | Out-Null
$env:FAKE_CLAUDE_MODE = 'bigstart'
$env:FAKE_CLAUDE_MARK = Join-Path $bigDir 'fake-invocations.txt'
$bp = Start-Process -PassThru -WindowStyle Hidden -FilePath (Get-Command pwsh).Source -RedirectStandardOutput (Join-Path $bigDir 'out.txt') -ArgumentList @(
    '-NoProfile', '-File', $Orch, '-ClaudeExe', $Fake, '-CoordDir', $bigDir, '-TelemetryDir', $Tele, '-BackoffBaseSeconds', '0',
    '-IdleCloseSeconds', '1', '-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
$finished = $bp.WaitForExit(90000)
if (-not $finished) { Stop-Process -Id $bp.Id -Force -ErrorAction SilentlyContinue; Get-CimInstance Win32_Process -Filter "ParentProcessId=$($bp.Id)" | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue } }
Check 'a prompt larger than the pipe buffer does not deadlock against a large init event' $finished $true
$bigUnits = if (Test-Path (Join-Path $bigDir 'units.jsonl')) { @(Get-Content (Join-Path $bigDir 'units.jsonl') | ForEach-Object { $_ | ConvertFrom-Json }) } else { @() }
Check 'the big-start unit completed with its handoff' ($bigUnits.Count -eq 1 -and $bigUnits[0].handoff_outcome -eq 'done') $true

# 4k. STARTUP WATCHDOG: a unit that never emits any event is killed after -StartupTicks ticks, recorded as failed, and its handoff is
# synthesized, instead of the loop waiting on it forever
$r = Run-Orch 'silent' 'silent' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-StartupTicks', '3')
Check 'a unit silent at start is killed by the watchdog' $r.units[0].killed $true
Check 'and counted as failed' $r.units[0].failed $true
Check 'and its handoff is synthesized' ($r.units[0].handoff_outcome -like 'synthesized*') $true
Check 'the watchdog says so' ($r.out -match 'hung at start') $true
Check 'the supervisor returned long before the 600 s the unit would have slept' ($r.units[0].duration_s -lt 60) $true

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
