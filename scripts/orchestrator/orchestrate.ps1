#Requires -Version 7.0
<#
.SYNOPSIS
The unattended orchestrator loop: one fresh headless `claude -p` session per bounded unit, chained by handoff files.

.DESCRIPTION
Design: docs/rearchitecture/DESIGN-orchestrator-loop.md (kb/Work PB1981). Each iteration, in this order:
  1. single instance (orchestrate.lock holds the PID; a dead PID's lock is stale and taken over)
  2. STOP file in the coordination directory ends the loop
  3. circuit breaker: three consecutive units that fail (nonzero exit, invalid or missing handoff, or under
     -FastFailSeconds) stop the loop with an owner note; exponential backoff between failures
  4. budget.py decides go / hold-session / hold-day / stop-week
  5. the next unit: -Unit for the first iteration, else next_unit.py (the handoff's next_unit, then the deterministic checks)
  6. run `claude -p` with the unit prompt, a fresh session id, stream-json to logs\; watch the context size and
     create STOP-UNIT past -MaxContextTokens; kill only after the grace period
  7. validate handoff.json against handoff.schema.json
  8. append one line to units.jsonl
Exit codes: 0 stopped (STOP, -MaxUnits, stop-week, -DryRun), 3 another instance runs, 4 circuit breaker,
5 an owner question is waiting in OWNER-QUESTIONS.md.

.EXAMPLE
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -DryRun
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -MaxUnits 1 -Unit meter
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    # The executable to run. A test points it at a fake that emits canned stream-json (a test seam, not a wrapper).
    [string]$ClaudeExe = 'claude',
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' }),
    [string]$RepoDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [int]$MaxContextTokens = 150000,
    [int]$MaxUnits = 0,
    [ValidateSet('acceptEdits', 'auto', 'bypassPermissions', 'dontAsk', 'plan', 'manual')]
    [string]$PermissionMode = 'auto',
    [double]$GraceMinutes = 30,
    [int]$BorrowDays = 0,
    [ValidateSet('', 'wave', 'land', 'resume', 'meter')]
    [string]$Unit = '',
    [switch]$Watch,
    [string]$Python = 'python',
    [string]$TelemetryDir = '',
    [int]$FastFailSeconds = 120,
    [int]$BackoffBaseSeconds = 60
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Here = $PSScriptRoot
$Schema = Join-Path $Here 'handoff.schema.json'
$UnitModel = @{ wave = 'opus'; land = 'opus'; resume = 'opus'; meter = 'sonnet' }
$Allowed = @('opus', 'sonnet')   # never Fable: it needs the owner's approval per dispatch (owner 2026-10-02)
New-Item -ItemType Directory -Force -Path $CoordDir, (Join-Path $CoordDir 'logs'), (Join-Path $CoordDir 'scratch') | Out-Null
$env:COBOL_COORD_DIR = $CoordDir
$Lock = Join-Path $CoordDir 'orchestrate.lock'
$StopFile = Join-Path $CoordDir 'STOP'
$StopUnit = Join-Path $CoordDir 'STOP-UNIT'
$Handoff = Join-Path $CoordDir 'handoff.json'
$LastHandoff = Join-Path $CoordDir 'handoff.last.json'
$UnitsLog = Join-Path $CoordDir 'units.jsonl'
$Questions = Join-Path $CoordDir 'OWNER-QUESTIONS.md'

function Say([string]$m) { Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $m" }

function Take-Lock {
    for ($i = 0; $i -lt 2; $i++) {
        try {
            $fs = [System.IO.File]::Open($Lock, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
            $b = [System.Text.Encoding]::UTF8.GetBytes((@{ pid = $PID; started_at = (Get-Date).ToString('o'); host = [Environment]::MachineName } | ConvertTo-Json -Compress))
            $fs.Write($b, 0, $b.Length); $fs.Close()
            return $true
        } catch [System.IO.IOException] {
            $held = $null
            try { $held = Get-Content $Lock -Raw | ConvertFrom-Json } catch { }
            if ($held -and $held.pid -and (Get-Process -Id $held.pid -ErrorAction SilentlyContinue)) {
                Say "another orchestrator is running (PID $($held.pid), since $($held.started_at)); refusing to start"
                return $false
            }
            Say "stale lock (PID $($held.pid) is gone): taking it over"
            Remove-Item $Lock -Force -ErrorAction SilentlyContinue
        }
    }
    return $false
}

# Sleep until $until, waking every minute so STOP is honoured. Returns $false when STOP appeared.
function Wait-Until([datetime]$until) {
    while ((Get-Date) -lt $until) {
        if (Test-Path $StopFile) { return $false }
        Start-Sleep -Seconds ([Math]::Max(1, [Math]::Min(60, ($until - (Get-Date)).TotalSeconds)))
    }
    return -not (Test-Path $StopFile)
}

function Invoke-Py([string[]]$ArgList) {
    $out = & $Python @ArgList 2>&1
    if ($LASTEXITCODE -ne 0) { throw "python $($ArgList -join ' ') failed ($LASTEXITCODE): $out" }
    return ($out -join "`n")
}

function Add-OwnerQuestion([string]$title, [string]$body) {
    Add-Content -Path $Questions -Encoding utf8 -Value "`n## $(Get-Date -Format 'yyyy-MM-dd HH:mm') — $title`n`n$body`n"
}

function Resolve-Launch([string]$exe) {
    # How to start the executable: a native exe directly; a .ps1 through pwsh; a .cmd/.bat through cmd. Launch
    # mechanics only, so -ClaudeExe can name the real CLI or a test fake.
    $cmd = Get-Command $exe -ErrorAction Stop
    $path = $cmd.Source
    switch ([IO.Path]::GetExtension($path).ToLowerInvariant()) {
        '.ps1' { return @((Get-Command pwsh).Source, @('-NoProfile', '-File', $path)) }
        { $_ -in '.cmd', '.bat' } { return @($env:ComSpec, @('/d', '/c', $path)) }
        default { return @($path, @()) }
    }
}

function Build-Prompt([string]$unit, [string]$scratch) {
    $sub = {
        param($t)
        $t.Replace('{COORD}', $CoordDir).Replace('{HANDOFF}', $Handoff).Replace('{STOP_UNIT}', $StopUnit).
           Replace('{PREV_HANDOFF}', $LastHandoff).Replace('{SCRATCH}', $scratch)
    }
    $common = & $sub (Get-Content (Join-Path $Here 'units/common.md') -Raw)
    $own = & $sub (Get-Content (Join-Path $Here "units/$unit.md") -Raw)
    return "$common`n$own"
}

function Get-ProjectTranscriptDir([string]$sessionId) {
    # Claude Code keeps a project's transcripts under ~/.claude/projects/<path with ':' '\' '/' as '-'>.
    $key = ($RepoDir -replace '[:\\/]', '-')
    return Join-Path $HOME ".claude/projects/$key/$sessionId"
}

function Invoke-Unit([string]$unit, [string]$model, [string]$sessionId, [string]$logBase) {
    $scratch = Join-Path $CoordDir 'scratch'
    $prompt = Build-Prompt $unit $scratch
    $claudeArgs = @('-p', $prompt, '--model', $model, '--permission-mode', $PermissionMode, '--permission-prompts', 'none',
        '--output-format', 'stream-json', '--verbose', '--session-id', $sessionId)
    if ($unit -eq 'meter') { $claudeArgs += '--chrome' }
    $exe, $pre = Resolve-Launch $ClaudeExe
    $psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
    foreach ($a in @($pre) + $claudeArgs) { $psi.ArgumentList.Add([string]$a) }
    $psi.WorkingDirectory = $RepoDir
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.Environment['COBOL_COORD_DIR'] = $CoordDir

    $stats = [ordered]@{ calls = 0; input = 0; output = 0; cache_read = 0; cache_creation = 0; peak_context = 0;
        context = 0; cost_usd = $null; stop_unit_sent = $false; killed = $false }
    $seen = @{}
    $graceMin = if ($unit -eq 'wave') { 3 * $GraceMinutes } else { $GraceMinutes }
    $stopAt = $null
    $watcher = $null
    $log = [System.IO.StreamWriter]::new("$logBase.jsonl", $false, [System.Text.UTF8Encoding]::new($false))
    $proc = [System.Diagnostics.Process]::Start($psi)
    $errTask = $proc.StandardError.ReadToEndAsync()
    if ($Watch -and $unit -eq 'wave') {
        try {
            $watcher = Start-Process -PassThru -WindowStyle Hidden -FilePath (Get-Command pwsh).Source -ArgumentList @(
                '-NoProfile', '-File', (Join-Path $Here 'open-watchers.ps1'), '-SessionDir', (Get-ProjectTranscriptDir $sessionId))
        } catch { Say "watchers did not start (the unit continues): $_" }
    }
    try {
        $read = $proc.StandardOutput.ReadLineAsync()
        while ($true) {
            if ($read.Wait(1000)) {
                $line = $read.Result
                if ($null -eq $line) { break }
                $log.WriteLine($line); $log.Flush()
                $ev = $null
                try { $ev = $line | ConvertFrom-Json -Depth 64 } catch { }
                if ($ev -and $ev.PSObject.Properties['type']) {
                    if ($ev.type -eq 'assistant' -and $ev.message -and $ev.message.PSObject.Properties['usage']) {
                        # stream-json repeats one model call's usage on each content block: count each message id once.
                        $id = [string]$ev.message.id
                        if (-not $seen.ContainsKey($id)) {
                            $seen[$id] = $true
                            $u = $ev.message.usage
                            $g = { param($n) if ($u.PSObject.Properties[$n]) { [long]$u.$n } else { 0 } }
                            $stats.calls++
                            $stats.input += & $g 'input_tokens'
                            $stats.output += & $g 'output_tokens'
                            $stats.cache_read += & $g 'cache_read_input_tokens'
                            $stats.cache_creation += & $g 'cache_creation_input_tokens'
                            $stats.context = (& $g 'input_tokens') + (& $g 'cache_read_input_tokens') + (& $g 'cache_creation_input_tokens')
                            $stats.peak_context = [Math]::Max($stats.peak_context, $stats.context)
                        }
                    } elseif ($ev.type -eq 'result' -and $ev.PSObject.Properties['total_cost_usd']) {
                        $stats.cost_usd = $ev.total_cost_usd
                    }
                }
                $read = $proc.StandardOutput.ReadLineAsync()
            }
            if (-not $stats.stop_unit_sent -and $stats.context -gt $MaxContextTokens) {
                Say "context $($stats.context) > ${MaxContextTokens}: STOP-UNIT (the unit hands off at its next step)"
                New-Item -ItemType File -Force -Path $StopUnit | Out-Null
                $stats.stop_unit_sent = $true
                $stopAt = Get-Date
            }
            if ($stopAt -and ((Get-Date) - $stopAt).TotalMinutes -gt $graceMin -and -not $proc.HasExited) {
                Say "no handoff $graceMin min after STOP-UNIT: killing the session (last resort)"
                $proc.Kill($true)
                $stats.killed = $true
            }
        }
        $proc.WaitForExit()
    } finally {
        $log.Close()
        if ($watcher -and -not $watcher.HasExited) { Stop-Process -Id $watcher.Id -Force -ErrorAction SilentlyContinue }
    }
    $err = $errTask.Result
    if ($err) { Set-Content -Path "$logBase.stderr.txt" -Value $err -Encoding utf8 }
    $stats.Remove('context')
    return @{ exit = $proc.ExitCode; stats = $stats }
}

if (-not (Take-Lock)) { exit 3 }
$exitCode = 0
try {
    $failures = 0
    $ran = 0
    $lastFailed = $false
    $lastStarted = $null
    while ($true) {
        if (Test-Path $StopFile) { Say "STOP file present: ending the loop"; break }
        if ($failures -ge 3) {
            $note = Join-Path $CoordDir "logs/BREAKER-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
            $tail = Get-Content $UnitsLog -Tail 3 -ErrorAction SilentlyContinue
            Set-Content -Path $note -Encoding utf8 -Value "# Circuit breaker: three consecutive failed units`n`n$($tail -join "`n")"
            Add-OwnerQuestion 'circuit breaker tripped' "Three consecutive units failed (fast exit, nonzero exit or no valid handoff). See $note and the logs it names. The loop stopped; restart it once the cause is fixed."
            Say "circuit breaker: three consecutive failed units; stopped (see $note)"
            $exitCode = 4; break
        }

        $budgetArgs = @((Join-Path $Here 'budget.py'), '--json', '--borrow-days', "$BorrowDays")
        if ($TelemetryDir) { $budgetArgs += @('--telemetry-dir', $TelemetryDir) }
        $budget = Invoke-Py $budgetArgs | ConvertFrom-Json
        Say "budget: weekly $($budget.weekly_est_pct) % of $($budget.allowance_pct) %, session $($budget.session_est_pct) % -> $($budget.decision)"
        if ($budget.decision -eq 'stop-week') { Say 'weekly cap reached: ending the loop'; break }
        $hold = $budget.decision -in @('hold-session', 'hold-day')
        if ($hold -and $DryRun) {
            Say "DRY RUN: would hold until $($budget.resume_at) (-BorrowDays moves a hold-day), then:"
        } elseif ($hold) {
            Say "holding until $($budget.resume_at)"
            # ConvertFrom-Json already turns an ISO time into a local DateTime; a string is parsed with its offset.
            $until = if ($budget.resume_at -is [datetime]) { $budget.resume_at } else { [datetimeoffset]::Parse([string]$budget.resume_at).LocalDateTime }
            if (-not (Wait-Until $until)) { Say 'STOP during the hold'; break }
            continue
        }

        if ($Unit -and $ran -eq 0) {
            $choice = [pscustomobject]@{ unit = $Unit; reason = 'named by -Unit' }
        } else {
            $na = @((Join-Path $Here 'next_unit.py'), '--repo', $RepoDir)
            if (Test-Path $LastHandoff) { $na += @('--handoff', $LastHandoff) }
            if ($lastFailed) { $na += '--last-failed' }
            if ($lastStarted) { $na += @('--last-started', $lastStarted.ToString('o')) }
            $choice = Invoke-Py $na | ConvertFrom-Json
        }
        if ($choice.unit -eq 'owner-question') {
            $q = Get-Content $LastHandoff -Raw | ConvertFrom-Json
            Add-OwnerQuestion "question from the $($q.unit) unit" "$($q.owner_question.question)`n`n$($q.owner_question.context)"
            Say "owner question written to $Questions; stopping (the loop never guesses)"
            $exitCode = 5; break
        }
        $model = $UnitModel[$choice.unit]
        if ($model -notin $Allowed) { throw "unit $($choice.unit) maps to model '$model', which is not allowed" }
        $sessionId = [guid]::NewGuid().ToString()
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $logBase = Join-Path $CoordDir "logs/$stamp-$($choice.unit)"
        if ($DryRun) {
            Say "DRY RUN: would run unit '$($choice.unit)' ($($choice.reason)) on $model, session $sessionId"
            Say "DRY RUN: $ClaudeExe -p <units/common.md + units/$($choice.unit).md> --model $model --permission-mode $PermissionMode --permission-prompts none --output-format stream-json --verbose --session-id $sessionId$(if ($choice.unit -eq 'meter') { ' --chrome' }) > $logBase.jsonl"
            break
        }

        Remove-Item $StopUnit, $Handoff -Force -ErrorAction SilentlyContinue
        Say "unit '$($choice.unit)' ($($choice.reason)) on $model, session $sessionId"
        $started = Get-Date
        $r = Invoke-Unit $choice.unit $model $sessionId $logBase
        $duration = ((Get-Date) - $started).TotalSeconds
        $valid = $false
        $outcome = 'missing'
        $h = $null
        if (Test-Path $Handoff) {
            $raw = Get-Content $Handoff -Raw
            $valid = [bool]($raw | Test-Json -SchemaFile $Schema -ErrorAction SilentlyContinue)
            if ($valid) {
                $h = $raw | ConvertFrom-Json
                $outcome = $h.outcome
                Copy-Item $Handoff "$logBase.handoff.json" -Force
                Copy-Item $Handoff $LastHandoff -Force
            } else { $outcome = 'invalid'; Copy-Item $Handoff "$logBase.handoff.invalid.json" -Force }
        }
        $failed = ($r.exit -ne 0) -or (-not $valid) -or ($duration -lt $FastFailSeconds)
        $failures = if ($failed) { $failures + 1 } else { 0 }
        $lastFailed = $failed
        $lastStarted = $started
        $line = [ordered]@{ unit = $choice.unit; reason = $choice.reason; model = $model; session_id = $sessionId
            started_at = $started.ToString('o'); ended_at = (Get-Date).ToString('o'); duration_s = [Math]::Round($duration, 1)
            exit_code = $r.exit; handoff_outcome = $outcome; next_unit = $(if ($h -and $h.PSObject.Properties['next_unit']) { $h.next_unit } else { $null })
            failed = $failed; log = "$logBase.jsonl" }
        foreach ($k in $r.stats.Keys) { $line[$k] = $r.stats[$k] }
        Add-Content -Path $UnitsLog -Encoding utf8 -Value ($line | ConvertTo-Json -Compress -Depth 5)
        Say "unit '$($choice.unit)' ended: exit $($r.exit), handoff $outcome, $([Math]::Round($duration)) s, $($r.stats.calls) calls$(if ($failed) { " — FAILURE $failures of 3" })"
        $ran++

        if ($valid -and $outcome -eq 'owner-question') {
            Add-OwnerQuestion "question from the $($choice.unit) unit" "$($h.owner_question.question)`n`n$(if ($h.owner_question.PSObject.Properties['context']) { $h.owner_question.context })"
            Say "owner question written to $Questions; stopping (the loop never guesses)"
            $exitCode = 5; break
        }
        if ($MaxUnits -gt 0 -and $ran -ge $MaxUnits) { Say "-MaxUnits $MaxUnits reached"; break }
        if ($failed -and $failures -lt 3) {
            $wait = [Math]::Min(1800, $BackoffBaseSeconds * [Math]::Pow(2, $failures - 1))
            if ($wait -gt 0) {
                Say "backing off $wait s"
                if (-not (Wait-Until ((Get-Date).AddSeconds($wait)))) { Say 'STOP during the backoff'; break }
            }
        }
    }
} finally {
    Remove-Item $Lock -Force -ErrorAction SilentlyContinue
}
exit $exitCode
