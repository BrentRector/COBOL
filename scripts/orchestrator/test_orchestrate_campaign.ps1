#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (campaign), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the campaign lane (-Cluster); one campaign rule decides every wave-type choice, a handoff's `wave`
# included, several leads take turns and a starved lead is reported and logged (kb/Work PB2522); one lander on main at
# a time (the landing lease, kb/Work PB2537), whose deferral survives the campaign rule; open-watchers.ps1.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_campaign.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 8b. THE CAMPAIGN LANE (-Cluster, kb/Work PB2120). A fixture register: cluster CAMP has a ready note and one it blocks;
# cluster DONE is landed. A temp -RepoDir (not a git tree: clean, nothing unpushed) and a fresh meter reading let
# next_unit.py reach its last rule, where the lane decides.
$fx = Join-Path $Root 'campaign'
$fxWork = Join-Path $fx 'Work'
$fxRepo = Join-Path $fx 'repo'
New-Item -ItemType Directory -Force -Path $fxWork, $fxRepo | Out-Null
function New-FxNote([string]$id, [string]$status, [string]$cluster, [string]$blockedBy) {
    $blocked = if ($blockedBy -and $status -ne 'landed') { 'true' } else { 'false' }
    Set-Content -Path (Join-Path $fxWork "$id.md") -Encoding utf8 -Value @(
        '---', "title: `"$id - a campaign step`"", "id: $id", 'kind: defect', "status: $status", 'area: build/ci',
        'process_only: true', "blocked: $blocked", "blocked_by: [$blockedBy]", "cluster: [$cluster]", '---', '', "# $id")
}
New-FxNote 'PB901' 'open' 'CAMP' ''
New-FxNote 'PB902' 'open' 'CAMP' 'PB901'
New-FxNote 'PB903' 'landed' 'DONE' ''
# A reading belongs to an account (kb/Work PB2478): the fresh one is THIS process's account's, as account.py resolves it.
$Me = (& python (Join-Path $Here 'account.py') --field name)
$fresh = { param($c) Set-Content -Path (Join-Path $c 'readings.json') -Encoding utf8 -Value (ConvertTo-Json -Compress @(@{
    noted_at = (Get-Date).ToUniversalTime().ToString('o'); account = $Me; weekly_pct = 1; session_pct = 1; session_reset = (Get-Date).AddHours(4).ToUniversalTime().ToString('o') })) }
$NextUnit = Join-Path $Here 'next_unit.py'
function Next-Unit([string]$coord, [string[]]$extra) {
    return (& python $NextUnit --repo $fxRepo --coord $coord @extra) -join ''
}
$nc = Join-Path $Root 'nextunit'
New-Item -ItemType Directory -Force -Path $nc | Out-Null
& $fresh $nc
Check 'the fix lane''s choice is unchanged without -Cluster (byte for byte)' (Next-Unit $nc @()) '{"unit": "wave", "reason": "nothing pending"}'
# another account's fresh reading is not this account's meter: the next unit reads the meter
$other = Join-Path $Root 'nextunit-other'
New-Item -ItemType Directory -Force -Path $other | Out-Null
Set-Content -Path (Join-Path $other 'readings.json') -Encoding utf8 -Value (ConvertTo-Json -Compress @(@{
    noted_at = (Get-Date).ToUniversalTime().ToString('o'); account = 'some-other-account'; weekly_pct = 1; session_pct = 1; session_reset = $null }))
Check 'another account''s reading leaves this account''s meter unread' ((Next-Unit $other @()) | ConvertFrom-Json).unit 'meter'
$c1 = Next-Unit $nc @('--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
Check 'a cluster with a ready note gets a campaign wave' "$($c1.unit) $($c1.campaign)" 'campaign run'
Check 'the campaign reason names the ready note' ($c1.reason -match 'PB901') $true
Set-Content -Path (Join-Path $nc 'units.jsonl') -Encoding utf8 -Value '{"unit":"campaign"}', '{"unit":"meter"}'
$c2 = Next-Unit $nc @('--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
Check 'after a campaign wave, a fix-lane wave' "$($c2.unit) $($c2.campaign)" 'wave between'
Add-Content -Path (Join-Path $nc 'units.jsonl') -Encoding utf8 -Value '{"unit":"wave"}'
$c3 = Next-Unit $nc @('--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
Check 'after a fix-lane wave, the campaign again' $c3.unit 'campaign'
$c4 = Next-Unit $nc @('--cluster', 'DONE', '--work', $fxWork) | ConvertFrom-Json
Check 'a landed cluster ends the lane' "$($c4.unit) $($c4.campaign)" 'wave landed'
New-FxNote 'PB904' 'open' 'HELD' 'PB901'
$c5 = Next-Unit $nc @('--cluster', 'HELD', '--work', $fxWork) | ConvertFrom-Json
Check 'a cluster with no ready note waits and runs the fix lane' "$($c5.unit) $($c5.campaign)" 'wave waiting'

# 8c. THE ALTERNATION IS THE SUPERVISOR'S, NOT THE MODEL'S (kb/Work PB2522): a handoff that names a generic `wave` decided
# the unit FIRST, so the campaign rule was unreachable while handoffs named a unit (PB2151 sat ready 05:40-14:31 PDT on
# 2026-10-07). A handoff's `wave` now yields `campaign` when the lane says so; `land`, `resume` and an owner question
# still outrank it.
$hc = Join-Path $Root 'nextunit-handoff'
New-Item -ItemType Directory -Force -Path $hc | Out-Null
& $fresh $hc
function Set-Handoff([string]$coord, [string]$name, [hashtable]$fields) {
    $h = [ordered]@{ schema_version = 1; unit = 'wave'; outcome = 'done'; summary = 'fixture' }
    foreach ($k in $fields.Keys) { $h[$k] = $fields[$k] }
    $p = Join-Path $coord "handoff-$name.json"
    Set-Content -Path $p -Encoding utf8 -Value ($h | ConvertTo-Json -Compress -Depth 5)
    return $p
}
Set-Content -Path (Join-Path $hc 'units.jsonl') -Encoding utf8 -Value '{"unit":"campaign","cluster":"CAMP"}', '{"unit":"wave"}'
$hw = Set-Handoff $hc 'wave' @{ next_unit = 'wave'; next_unit_reason = 'a wave should run the finishers' }
$h1 = Next-Unit $hc @('--handoff', $hw, '--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
Check 'a handoff naming wave after a fix-lane wave, with a ready campaign note: campaign' "$($h1.unit) $($h1.campaign) $($h1.cluster)" 'campaign run CAMP'
Check 'the campaign choice keeps the handoff''s reason' ($h1.reason -match 'the handoff named wave: a wave should run the finishers') $true
Add-Content -Path (Join-Path $hc 'units.jsonl') -Encoding utf8 -Value '{"unit":"campaign","cluster":"CAMP"}'
$h2 = Next-Unit $hc @('--handoff', $hw, '--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
Check 'a handoff naming wave after a campaign wave: the fix-lane wave' "$($h2.unit) $($h2.campaign)" 'wave between'
Check 'the fix-lane choice keeps the handoff''s reason' ($h2.reason -match 'a wave should run the finishers') $true
$hcm = Set-Handoff $hc 'campaign' @{ next_unit = 'campaign'; next_unit_reason = 'the campaign again' }
Check 'a handoff naming campaign right after a campaign wave: the fix-lane wave' ((Next-Unit $hc @('--handoff', $hcm, '--cluster', 'CAMP', '--work', $fxWork)) | ConvertFrom-Json).unit 'wave'
Add-Content -Path (Join-Path $hc 'units.jsonl') -Encoding utf8 -Value '{"unit":"wave"}'
$hl = Set-Handoff $hc 'land' @{ next_unit = 'land'; next_unit_reason = 'three branches are DONE' }
Check 'a handoff naming land outranks the campaign' ((Next-Unit $hc @('--handoff', $hl, '--cluster', 'CAMP', '--work', $fxWork)) | ConvertFrom-Json).unit 'land'
$hr = Set-Handoff $hc 'resume' @{ next_unit = 'resume'; next_unit_reason = 'finish the gates' }
Check 'a handoff naming resume outranks the campaign' ((Next-Unit $hc @('--handoff', $hr, '--cluster', 'CAMP', '--work', $fxWork)) | ConvertFrom-Json).unit 'resume'
$ho = Set-Handoff $hc 'owner' @{ outcome = 'owner-question'; next_unit = 'wave'; owner_question = @{ question = 'A or B?'; context = 'fixture' } }
Check 'an owner question outranks the campaign' ((Next-Unit $hc @('--handoff', $ho, '--cluster', 'CAMP', '--work', $fxWork)) | ConvertFrom-Json).unit 'owner-question'
Check 'without -Cluster a handoff''s wave is unchanged (byte for byte)' (Next-Unit $hc @('--handoff', $hw)) '{"unit": "wave", "reason": "named by the last handoff: a wave should run the finishers"}'

# 8d. MORE THAN ONE CAMPAIGN (-Cluster A,B): the ready clusters take campaign turns round-robin, least recently run first,
# still alternating with fix-lane waves; a landed cluster leaves the list and the others go on.
New-FxNote 'PB905' 'open' 'CAMP2' ''
$rr = Join-Path $Root 'nextunit-rr'
New-Item -ItemType Directory -Force -Path $rr | Out-Null
& $fresh $rr
Set-Content -Path (Join-Path $rr 'units.jsonl') -Encoding utf8 -Value '{"unit":"campaign","cluster":"CAMP"}', '{"unit":"wave"}'
$m1 = Next-Unit $rr @('--cluster', 'CAMP', '--cluster', 'CAMP2', '--work', $fxWork) | ConvertFrom-Json
Check 'two campaigns: the one that has not run goes next' "$($m1.unit) $($m1.cluster)" 'campaign CAMP2'
Add-Content -Path (Join-Path $rr 'units.jsonl') -Encoding utf8 -Value '{"unit":"campaign","cluster":"CAMP2"}'
Check 'two campaigns: a fix-lane wave between them' ((Next-Unit $rr @('--cluster', 'CAMP', '--cluster', 'CAMP2', '--work', $fxWork)) | ConvertFrom-Json).unit 'wave'
Add-Content -Path (Join-Path $rr 'units.jsonl') -Encoding utf8 -Value '{"unit":"wave"}'
$m3 = Next-Unit $rr @('--cluster', 'CAMP', '--cluster', 'CAMP2', '--work', $fxWork) | ConvertFrom-Json
Check 'two campaigns: then the least recently run one' "$($m3.unit) $($m3.cluster)" 'campaign CAMP'
$m4 = Next-Unit $rr @('--cluster', 'DONE', '--cluster', 'CAMP2', '--work', $fxWork) | ConvertFrom-Json
Check 'a landed cluster among two: reported landed, the other runs' "$($m4.campaigns.DONE) $($m4.unit) $($m4.cluster)" 'landed campaign CAMP2'

# 8e. STARVATION IS VISIBLE: units.jsonl records each choice's per-cluster state, so the choice reports a cluster that has
# had a ready note through more than one wave-type unit without a campaign wave of its own.
$sv = Join-Path $Root 'nextunit-starved'
New-Item -ItemType Directory -Force -Path $sv | Out-Null
& $fresh $sv
$t0 = (Get-Date).ToUniversalTime()
Set-Content -Path (Join-Path $sv 'units.jsonl') -Encoding utf8 -Value @(
    (@{ unit = 'campaign'; cluster = 'CAMP'; started_at = $t0.AddHours(-12).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
    (@{ unit = 'resume'; started_at = $t0.AddHours(-9).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
    (@{ unit = 'wave'; started_at = $t0.AddHours(-9).AddMinutes(2).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
    (@{ unit = 'land'; started_at = $t0.AddHours(-6).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
    (@{ unit = 'wave'; started_at = $t0.AddHours(-5).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress))
$hs = Set-Handoff $sv 'starved' @{ next_unit = 'land'; next_unit_reason = 'a train is ready' }
$s1 = Next-Unit $sv @('--handoff', $hs, '--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
$st1 = @($s1.starved | Where-Object { $_ })   # absent before the change: an empty list, never an error
Check 'a starved campaign is reported even when another unit is chosen' "$($s1.unit) $($st1.Count) $(if ($st1) { "$($st1[0].cluster) $($st1[0].waves)" })" 'land 1 CAMP 2'
Check 'the starvation is measured from the first choice that saw it ready' $(if ($st1) { [Math]::Round([double]$st1[0].hours) }) 9
# (ConvertFrom-Json turns the ISO string into a DateTime)
Check 'the starvation names the last campaign wave' $(if ($st1) { ([datetime]$st1[0].last_campaign_at).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm') }) $t0.AddHours(-12).ToString('yyyy-MM-ddTHH:mm')
Set-Content -Path (Join-Path $sv 'units.jsonl') -Encoding utf8 -Value @(
    (@{ unit = 'campaign'; cluster = 'CAMP'; started_at = $t0.AddHours(-3).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
    (@{ unit = 'wave'; started_at = $t0.AddHours(-2).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress))
$s2 = Next-Unit $sv @('--handoff', $hs, '--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json
Check 'a cluster that waited through one wave only is not starved' @($s2.starved | Where-Object { $_ }).Count 0
# a line written before the supervisor recorded the lane's state proves nothing: the walk stops there (never over-reports)
Set-Content -Path (Join-Path $sv 'units.jsonl') -Encoding utf8 -Value '{"unit":"wave"}', '{"unit":"wave"}', '{"unit":"wave"}'
Check 'unrecorded lines are not counted as waiting' @((Next-Unit $sv @('--handoff', $hs, '--cluster', 'CAMP', '--work', $fxWork) | ConvertFrom-Json).starved | Where-Object { $_ }).Count 0

# 8f. ONE LANDER ON MAIN AT A TIME (kb/Work PB2537): while another lander holds the landing lease, finished branches
# and a handoff naming `land` do not start a land unit; the land waits and the reason names the holder. An expired
# lease binds no one, and with no lease the land runs as before.
$lc = Join-Path $Root 'leaseunit'
New-Item -ItemType Directory -Force -Path $lc | Out-Null
& $fresh $lc
$doneHandoff = Join-Path $lc 'handoff-done.json'
Set-Content -Path $doneHandoff -Encoding utf8 -Value '{"outcome": "done", "branches_pending": [{"branch": "w9a", "status": "DONE"}]}'
$landHandoff = Join-Path $lc 'handoff-land.json'
Set-Content -Path $landHandoff -Encoding utf8 -Value '{"outcome": "done", "next_unit": "land", "next_unit_reason": "train ready"}'
Check 'no lease: finished branches start a land unit' ((Next-Unit $lc @('--handoff', $doneHandoff)) | ConvertFrom-Json).unit 'land'
$Lease = Join-Path $Here 'landing_lease.py'
& python $Lease acquire --holder 'attended R1 lander' --reason 'R1' --worktree $fxRepo --coord $lc | Out-Null
$l1 = (Next-Unit $lc @('--handoff', $doneHandoff)) | ConvertFrom-Json
Check 'a held lease: finished branches wait, the loop runs a wave' $l1.unit 'wave'
Check 'a held lease: the reason names the waiting branch and the holder' ($l1.reason -match '^nothing pending; land deferred \(w9a\): the landing lease is held by attended R1 lander') $true
$l2 = (Next-Unit $lc @('--handoff', $landHandoff)) | ConvertFrom-Json
Check 'a held lease: a handoff naming land waits too' "$($l2.unit) $($l2.reason -match 'land deferred \(named by the last handoff\)')" 'wave True'
$l3 = (Next-Unit $lc @('--handoff', $doneHandoff, '--cluster', 'CAMP', '--work', $fxWork)) | ConvertFrom-Json
Check 'a held lease in the campaign lane: the lane decides, the land waits' "$($l3.unit) $($l3.reason -match 'land deferred')" 'campaign True'
$l5 = (Next-Unit $lc @('--handoff', $landHandoff, '--cluster', 'CAMP', '--work', $fxWork)) | ConvertFrom-Json
Check 'a held lease, a handoff naming land, the campaign lane: the deferred land named no wave' "$($l5.unit) $($l5.reason -match 'land deferred \(named by the last handoff\)') $($l5.reason -match 'the handoff named wave')" 'campaign True False'
Check 'stop.ps1 -Status names the lease holder' ((& pwsh -NoProfile -File (Join-Path $Here 'stop.ps1') -Status -CoordDir $lc) -join ' ' -match 'landing lease: held by attended R1 lander') $true
$later = (Get-Date).ToUniversalTime().AddHours(2).ToString('o')
$l4 = (Next-Unit $lc @('--handoff', $doneHandoff, '--now', $later)) | ConvertFrom-Json
Check 'an expired lease binds no one: the land runs' $l4.unit 'land'
& python $Lease release --worktree $fxRepo --coord $lc | Out-Null
Check 'a released lease: the land runs' ((Next-Unit $lc @('--handoff', $doneHandoff)) | ConvertFrom-Json).unit 'land'
Check 'stop.ps1 -Status says the lease is free' ((& pwsh -NoProfile -File (Join-Path $Here 'stop.ps1') -Status -CoordDir $lc) -join ' ' -match 'landing lease: free') $true

$camp = @('-RepoDir', $fxRepo, '-WorkDir', $fxWork)
$r = Run-Orch 'campdry' 'good' (@('-DryRun', '-Cluster', 'CAMP') + $camp) $fresh
Check 'campaign dry run: the supervisor names the lane' ($r.out -match 'campaign lane: cluster CAMP, 2 open note\(s\), 1 ready') $true
Check 'campaign dry run: would run the campaign unit' ($r.out -match "would run unit 'campaign'") $true
Check 'campaign dry run: from the wave prompt, with --cluster' ($r.out -match 'units/wave\.md with --cluster CAMP') $true
$r = Run-Orch 'campdone' 'good' (@('-DryRun', '-Cluster', 'DONE') + $camp) $fresh
Check 'a landed cluster: the supervisor says the lane ends' ($r.out -match 'campaign DONE: every note is landed or retired; the campaign lane ends') $true
Check 'a landed cluster: the fix lane runs' ($r.out -match "would run unit 'wave'") $true
$r = Run-Orch 'campbad' 'good' (@('-DryRun', '-Cluster', 'NOSUCH') + $camp)
Check 'an unknown cluster refuses to start' "$($r.code) $($r.runs)" '2 0'
$r = Run-Orch 'campnolead' 'good' @('-DryRun', '-Unit', 'campaign')
Check '-Unit campaign without -Cluster refuses to start' $r.code 2
$r = Run-Orch 'camprun' 'good' (@('-Unit', 'campaign', '-Cluster', 'CAMP', '-MaxUnits', '1', '-FastFailSeconds', '0') + $camp)
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
Check 'the campaign unit''s plan_wave call carries --cluster' ($inv -match 'STOP-loop --cluster CAMP` plans the wave') $true
Check 'the campaign unit is recorded as such' $r.units[0].unit 'campaign'
Check 'the campaign unit runs on opus' ($inv -match '--model \| opus') $true
Check 'the campaign unit''s units.jsonl line names its cluster' $r.units[0].cluster 'CAMP'
# two campaign leads: each is validated and named at start, and the chosen one's lead reaches the prompt and units.jsonl
$r = Run-Orch 'camptwo' 'good' (@('-Cluster', 'CAMP,CAMP2', '-MaxUnits', '1', '-FastFailSeconds', '0') + $camp) {
    param($c) & $fresh $c; Set-Content -Path (Join-Path $c 'units.jsonl') -Encoding utf8 -Value '{"unit":"campaign","cluster":"CAMP"}', '{"unit":"wave"}' }
$inv = if ($r.runs) { Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw } else { '' }
Check 'two leads: the supervisor names both lanes' (($r.out -match 'campaign lane: cluster CAMP, 2 open') -and ($r.out -match 'campaign lane: cluster CAMP2, 1 open')) $true
Check 'two leads: the least recently run lead''s campaign runs' "$($r.units[-1].unit) $($r.units[-1].cluster)" 'campaign CAMP2'
Check 'two leads: its plan_wave call carries that lead' ($inv -match 'STOP-loop --cluster CAMP2` plans the wave') $true
Check 'the choice''s per-cluster state is recorded in units.jsonl' "$($r.units[-1].campaigns.CAMP) $($r.units[-1].campaigns.CAMP2)" 'ready ready'
$r = Run-Orch 'camponeleft' 'good' (@('-DryRun', '-Cluster', 'DONE,CAMP') + $camp) $fresh
Check 'two leads, one landed: its lane ends and the other goes on' (($r.out -match 'campaign DONE: every note is landed or retired; the campaign lane ends \(campaigns CAMP go on\)') -and ($r.out -match "would run unit 'campaign' .*\n.*with --cluster CAMP>")) $true
# a ready note that waited through more than one wave-type unit is announced by the supervisor (kb/Work PB2522)
$r = Run-Orch 'campstarved' 'good' (@('-DryRun', '-Cluster', 'CAMP') + $camp) {
    param($c) & $fresh $c
    $t = (Get-Date).ToUniversalTime()
    Set-Content -Path (Join-Path $c 'units.jsonl') -Encoding utf8 -Value @(
        (@{ unit = 'campaign'; cluster = 'CAMP'; started_at = $t.AddHours(-10).ToString('o') } | ConvertTo-Json -Compress),
        (@{ unit = 'wave'; started_at = $t.AddHours(-9).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
        (@{ unit = 'land'; started_at = $t.AddHours(-6).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress),
        (@{ unit = 'wave'; started_at = $t.AddHours(-5).ToString('o'); campaigns = @{ CAMP = 'ready' } } | ConvertTo-Json -Compress)) }
Check 'a starved campaign is announced' ($r.out -match 'campaign CAMP ready for 9\.\d h \(2 wave-type unit\(s\)\), last campaign wave at \d{4}-\d\d-\d\d \d\d:\d\d') $true
$r = Run-Orch 'wavedefault' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0')
$inv = Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw
Check 'the fix lane''s wave prompt is unchanged (no --cluster, no placeholder)' (($inv -match 'STOP-loop` plans the wave') -and ($inv -notmatch '--cluster') -and ($inv -notmatch 'CLUSTER_ARG')) $true

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

Complete-OrchTest 'campaign'
