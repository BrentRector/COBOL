#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (core), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the dry run, one recorded unit, a missing handoff, the STOP file before any unit, a quick done unit.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_core.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

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

Complete-OrchTest 'core'
