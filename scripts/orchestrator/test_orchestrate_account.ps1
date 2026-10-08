#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (account), driven by a fake -ClaudeExe (testdata/fake-claude.ps1); no Pester, no real
# session. Proves: the account is a parameter (-ConfigDir, start-session.ps1) and account-profile.ps1 seeds a named account's config dir.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_account.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 2b. the account is a parameter (kb/Work PB2479): -ConfigDir selects it, every child runs as exactly it (the variable
#     set for a named account, UNSET for the default one), the log and units.jsonl name it, an unknown dir is refused
$acct2 = Join-Path $HOME '.claude-acct2'
$r = Run-Orch 'acct2' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-ConfigDir', $acct2)
Check 'account 2: the log header names it' ($r.out -match 'account: account2 \(config dir ') $true
Check 'account 2: units.jsonl names it' $r.units[0].account 'account2'
Check 'account 2: the unit runs with its CLAUDE_CONFIG_DIR' ((Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw) -match [regex]::Escape("CONFIG_DIR <$acct2>")) $true
$saved = $env:CLAUDE_CONFIG_DIR
try {
    $env:CLAUDE_CONFIG_DIR = $acct2   # the default account must not inherit the caller's variable
    $r = Run-Orch 'acct1' 'good' @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-ConfigDir', (Join-Path $HOME '.claude'))
} finally { if ($saved) { $env:CLAUDE_CONFIG_DIR = $saved } else { Remove-Item Env:CLAUDE_CONFIG_DIR -ErrorAction SilentlyContinue } }
Check 'account 1: units.jsonl names it' $r.units[0].account 'account1'
Check 'account 1: the unit runs with CLAUDE_CONFIG_DIR unset' ((Get-Content (Join-Path $r.coord 'fake-invocations.txt') -Raw) -match 'CONFIG_DIR <>') $true
$r = Run-Orch 'acctdry' 'good' @('-DryRun', '-Unit', 'wave', '-ConfigDir', $acct2)
Check 'the transcripts are looked for under the account''s config dir' ($r.out -match [regex]::Escape((Join-Path $acct2 'projects'))) $true
$r = Run-Orch 'acctbad' 'good' @('-Unit', 'wave', '-ConfigDir', (Join-Path $Root 'not-an-account'))
Check 'an unknown config dir is refused' "$($r.code) $($r.runs)" '2 0'
Check 'the refusal names the table' ($r.out -match 'not in model_rules.json accounts.list') $true

$ss = (& pwsh -NoProfile -File (Join-Path (Split-Path $Here -Parent) 'start-session.ps1') -DryRun -ConfigDir $acct2) -join ' '
Check 'start-session.ps1 -ConfigDir starts that account' ($ss -match [regex]::Escape("as account account2 (CLAUDE_CONFIG_DIR = $acct2)")) $true
$ss = (& pwsh -NoProfile -File (Join-Path (Split-Path $Here -Parent) 'start-session.ps1') -DryRun -ConfigDir (Join-Path $HOME '.claude')) -join ' '
Check 'start-session.ps1 starts the default account with the variable unset' ($ss -match 'as account account1 \(CLAUDE_CONFIG_DIR unset\)') $true

# 2c. account-profile.ps1 seeds a named account's config dir from the default one (kb/Work PB2480), in a temp HOME: the
#     copies (a kept file, a completed directory), the memory junction, the Chrome flags with the rest of .claude.json
#     untouched, never the credentials; a re-run changes nothing; the seed check fails before and passes after
$ph = Join-Path $Root 'profile-home'
$src = Join-Path $ph '.claude'
$dst = Join-Path $ph '.claude-x'
$prepo = Join-Path $Root 'profile-repo'
$key = ($prepo -replace '[:\\/]', '-')
New-Item -ItemType Directory -Force -Path (Join-Path $src 'plugins/p1'), (Join-Path $src 'skills/s1'), (Join-Path $src "projects/$key/memory"), $prepo, (Join-Path $dst 'skills/own') | Out-Null
Set-Content (Join-Path $src 'settings.json') '{"env":{"A":"1"}}'; Set-Content (Join-Path $src 'config.json') '{}'
Set-Content (Join-Path $src '.credentials.json') 'SECRET'; Set-Content (Join-Path $src "projects/$key/memory/MEMORY.md") 'shared'
Set-Content (Join-Path $dst 'settings.json') '{"own":true}'
Set-Content (Join-Path $dst '.claude.json') '{"oauthAccount":{"accountUuid":"u2"},"firstStartTime":"2026-10-07T13:20:00.000Z","n":12345678901234}'
$prules = Join-Path $Root 'profile-rules.json'
$rj = Get-Content (Join-Path $Here 'model_rules.json') -Raw | ConvertFrom-Json -Depth 100
$rj.accounts.list = @(@{ name = 'a'; config_dir = $null; weekly_reset = $rj.accounts.list[0].weekly_reset },
                      @{ name = 'b'; config_dir = '~/.claude-x'; weekly_reset = $rj.accounts.list[1].weekly_reset })
Set-Content $prules ($rj | ConvertTo-Json -Depth 100)
$ProfilePs = Join-Path (Split-Path $Here -Parent) 'account-profile.ps1'
$savedHome = $env:USERPROFILE
try {
    $env:USERPROFILE = $ph
    & python (Join-Path $Here 'account.py') --rules $prules --config-dir $dst --repo $prepo --seed-check | Out-Null
    Check 'seed check fails on an unseeded dir' $LASTEXITCODE 1
    $o1 = (& pwsh -NoProfile -File $ProfilePs -ConfigDir $dst -RulesFile $prules -RepoDir $prepo) -join "`n"
    $c1 = $LASTEXITCODE
    $o2 = (& pwsh -NoProfile -File $ProfilePs -ConfigDir $dst -RulesFile $prules -RepoDir $prepo) -join "`n"
    $c2 = $LASTEXITCODE
    $o3 = (& pwsh -NoProfile -File $ProfilePs -ConfigDir $src -RulesFile $prules -RepoDir $prepo) -join "`n"
} finally { $env:USERPROFILE = $savedHome }
Check 'account-profile seeds and its check passes' "$c1 $c2" '0 0'
Check 'seeded: the missing config.json copied' (Test-Path (Join-Path $dst 'config.json')) $true
Check 'seeded: the account''s own settings.json kept' ((Get-Content (Join-Path $dst 'settings.json') -Raw).Trim()) '{"own":true}'
Check 'seeded: a directory completed, its own child kept' "$(Test-Path (Join-Path $dst 'skills/s1')) $(Test-Path (Join-Path $dst 'skills/own')) $(Test-Path (Join-Path $dst 'plugins/p1'))" 'True True True'
Check 'seeded: the memory is a junction to the shared one' ((Get-Content (Join-Path $dst "projects/$key/memory/MEMORY.md") -Raw).Trim()) 'shared'
Check 'seeded: never the credentials' (Test-Path (Join-Path $dst '.credentials.json')) $false
$cj = Get-Content (Join-Path $dst '.claude.json') -Raw
Check 'seeded: the Chrome flags set' (($cj | ConvertFrom-Json).claudeInChromeDefaultEnabled) $true
Check 'seeded: the rest of .claude.json untouched (timestamp, big number, sign-in)' (($cj -match '"firstStartTime": "2026-10-07T13:20:00.000Z"') -and ($cj -match '12345678901234') -and ($cj -match '"accountUuid": "u2"')) $true
Check 'a re-run changes nothing' (($o2 -match 'copied|linked|flagged')) $false
Check 'the default account is the source: nothing to do' ($o3 -match 'is the default account') $true

Complete-OrchTest 'account'
