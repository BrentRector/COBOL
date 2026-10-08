#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (startup), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: no pipe deadlock on a large prompt, the startup watchdog.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_startup.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

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
# A hang guard, never a speed bound: 90 s expired under the parallel self-test runner on a shared host (train 1037t).
$finished = $bp.WaitForExit(600000)
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
# Bounded by the property itself (the fake sleeps 600 s and exits 0; a supervisor that waited for it would record >= 600),
# never by host speed: a 60 s bound failed under the gate's parallel SELF-TESTS phase with the Linux gate running
# beside it (train 1037t, 2026-10-08), and passed alone. The kill itself is the `killed` check above.
Check 'the supervisor did not wait out the 600 s the unit would have slept' ($r.units[0].duration_s -lt 600) $true

Complete-OrchTest 'startup'
