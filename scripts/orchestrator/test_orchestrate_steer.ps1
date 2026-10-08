#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (steer), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the operator steers the next unit through steer.ps1 only (kb/Work PB2596): the steer is validated,
# outranks every handoff (a synthesized one included), is consumed by the unit it chose, and -Unit goes through it.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_steer.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 10. OPERATOR STEERING (kb/Work PB2596): steer.ps1 is the one path. Its handoff is validated, wins over a synthesized one
# (the 2026-10-07 23:53 restart ran `resume` from a dead meter unit's synthesized handoff), survives a unit that ends while
# it is pending, is consumed by the unit it chose (which reads it as its previous handoff), and -Unit goes through it.
$StopPs = Join-Path $Here 'stop.ps1'
$SteerPs = Join-Path $Here 'steer.ps1'
$synth = { param($c) Set-Content -Path (Join-Path $c 'handoff.last.json') -Encoding utf8 -Value '{"schema_version":1,"unit":"meter","outcome":"split","summary":"synthesized for a dead meter unit","next_unit":"resume","synthesized":true}' }
$st = Join-Path $Root 'steerscript'
& pwsh -NoProfile -File $SteerPs -Unit wave -Reason 'run a wave; hold push-main' -CoordDir $st | Out-Null
$sh = Get-Content (Join-Path $st 'handoff.operator.json') -Raw
Check 'steer.ps1 writes the operator handoff' "$LASTEXITCODE $(($sh | ConvertFrom-Json).unit) $(($sh | ConvertFrom-Json).next_unit)" '0 operator wave'
Check 'the operator handoff validates against handoff.schema.json' ($sh | Test-Json -SchemaFile (Join-Path $Here 'handoff.schema.json')) $true
Check 'stop.ps1 -Status shows the pending steer' ((& pwsh -NoProfile -File $StopPs -Status -CoordDir $st) -join ' ' -match "operator steer: next unit 'wave' \(run a wave; hold push-main\)") $true
& pwsh -NoProfile -File $StopPs -Clear -CoordDir $st | Out-Null
Check 'stop.ps1 -Clear leaves the steer' (Test-Path (Join-Path $st 'handoff.operator.json')) $true
& pwsh -NoProfile -File $SteerPs -Unit land -Reason ('x' * 401) -CoordDir $st 2>$null | Out-Null
Check 'an invalid steer is refused and the pending one kept' "$LASTEXITCODE $(((Get-Content (Join-Path $st 'handoff.operator.json') -Raw) | ConvertFrom-Json).next_unit)" '1 wave'
& pwsh -NoProfile -File $SteerPs -Withdraw -CoordDir $st | Out-Null
Check 'steer.ps1 -Withdraw removes it' (Test-Path (Join-Path $st 'handoff.operator.json')) $false
$opJson = { param($c, $u) & pwsh -NoProfile -File $SteerPs -Unit $u -Reason "the operator wants $u" -CoordDir $c | Out-Null }

$r = Run-Orch 'steerwins' 'good' @('-MaxUnits', '1', '-FastFailSeconds', '0') { param($c) & $synth $c; & $opJson $c 'wave' }
Check 'the steer wins over a synthesized handoff' "$($r.units[0].unit) $($r.units[0].reason)" 'wave the operator: the operator wants wave'
Check 'the steer is consumed when its unit starts' (Test-Path (Join-Path $r.coord 'handoff.operator.json')) $false
Check 'the consumed steer is archived beside the unit''s log' (@(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*-wave.handoff.operator.json').Count) 1
Check 'the unit reads the operator''s instruction as its previous handoff' ((Get-Content (Join-Path $r.coord 'prev-handoff-seen.json') -Raw | ConvertFrom-Json).unit) 'operator'
$r = Run-Orch 'steercampaign' 'good' @('-MaxUnits', '1', '-FastFailSeconds', '0') { param($c) & $synth $c; & $opJson $c 'campaign' }
Check 'a campaign steer with no -Cluster runs a wave' $r.units[0].unit 'wave'
Check 'and is still consumed (else rule 0 chooses it on every later iteration)' (Test-Path (Join-Path $r.coord 'handoff.operator.json')) $false
$r = Run-Orch 'steerlater' 'steersme' @('-Unit', 'wave', '-MaxUnits', '2', '-FastFailSeconds', '0')
Check 'a steer written while a unit runs beats that unit''s handoff (it named land)' "$($r.units.Count) $($r.units[1].unit) $($r.units[1].reason)" '2 resume the operator: operator: hold the land unit'
$r = Run-Orch 'orphan' 'good' @('-MaxUnits', '1', '-FastFailSeconds', '0') {
    param($c) & $synth $c; Set-Content -Path (Join-Path $c 'handoff.json') -Value '{"schema_version":1,"unit":"wave","outcome":"done","summary":"operator by hand","next_unit":"wave"}' }
Check 'a hand-written handoff.json steers nothing' $r.units[0].unit 'resume'
Check 'and is archived and said, not deleted in silence' "$($r.out -match 'no unit of this run wrote was archived') $(@(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*.handoff.orphan.json').Count)" 'True 1'
$r = Run-Orch 'steerbad' 'good' @('-DryRun') { param($c) & $synth $c; Set-Content -Path (Join-Path $c 'handoff.operator.json') -Value '{"next_unit":"wave"}' }
Check 'a hand-edited invalid steer is set aside loudly' "$($r.out -match 'operator steer does not validate') $(Test-Path (Join-Path $r.coord 'handoff.operator.json'))" 'True False'
Check 'and the loop falls back to the last handoff' ($r.out -match "would run unit 'resume'") $true
$r = Run-Orch 'unitdry' 'good' @('-DryRun', '-Unit', 'land')
Check '-Unit is a steer through steer.ps1' ($r.out -match "would run unit 'land' \(the operator: named by orchestrate.ps1 -Unit at start\)") $true
Check 'a dry run leaves no steer behind' "$(Test-Path (Join-Path $r.coord 'handoff.operator.json')) $(@(Get-ChildItem ([IO.Path]::GetTempPath()) -Filter 'orchestrate-dryrun-*.handoff.operator.json').Count)" 'False 0'
$r = Run-Orch 'dryopen' 'good' @('-DryRun') { param($c) & $opJson $c 'meter' }
Check 'a dry run shows a pending steer and does not consume it' "$($r.out -match "would run unit 'meter'") $(Test-Path (Join-Path $r.coord 'handoff.operator.json'))" 'True True'

# next_unit.py itself: rule 0 outranks rule 1 (an owner question already answered by the steer)
$nc = Join-Path $Root 'nextunit'
New-Item -ItemType Directory -Force -Path $nc | Out-Null
$opFile = Join-Path $Root 'op.json'
Set-Content -Path $opFile -Value '{"schema_version":1,"unit":"operator","outcome":"done","summary":"s","next_unit":"land","next_unit_reason":"r"}'
$ownerQ = Join-Path $Root 'oq.json'
Set-Content -Path $ownerQ -Value '{"schema_version":1,"unit":"resume","outcome":"owner-question","summary":"q","owner_question":{"question":"A or B?"}}'
function Next-Unit([string]$coord, [string[]]$extra) { return (& python (Join-Path $Here 'next_unit.py') --repo $nc --coord $coord @extra) -join '' }
Check 'next_unit.py: the steer outranks an owner question already answered' (((Next-Unit $nc @('--operator-handoff', $opFile, '--handoff', $ownerQ)) | ConvertFrom-Json).unit) 'land'
Check 'next_unit.py: without a steer the owner question still stops the loop' (((Next-Unit $nc @('--handoff', $ownerQ)) | ConvertFrom-Json).unit) 'owner-question'

Complete-OrchTest 'steer'
