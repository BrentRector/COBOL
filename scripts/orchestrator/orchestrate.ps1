#Requires -Version 7.0
<#
.SYNOPSIS
The unattended orchestrator loop: one fresh headless `claude -p` session per bounded unit, chained by handoff files.

.DESCRIPTION
Design: docs/rearchitecture/DESIGN-orchestrator-loop.md (kb/Work PB1981). Each iteration, in this order:
  1. single instance (orchestrate.lock holds the PID; a dead PID's lock is stale and taken over)
  2. STOP file in the coordination directory (or the owner's global scratch\STOP) ends the loop; during a unit it winds
     the unit down gracefully first (STOP-UNIT plus the LOOP's own fleet stop scratch\STOP-loop, so nothing is lost and
     no other session's agents are stopped; kb/Work PB2483), see stop.ps1
  3. circuit breaker: three consecutive units that fail (nonzero exit, invalid or missing handoff, or under
     -FastFailSeconds without a `done` handoff) stop the loop with an owner note; exponential backoff between failures
  4. budget.py decides go / hold-session / hold-day / stop-week
  5. the next unit: -Unit for the first iteration, else next_unit.py (the handoff's next_unit, then the deterministic checks);
     with -Cluster LEAD[,LEAD...] the CAMPAIGN lane (kb/Work PB2120): next_unit.py alternates `campaign` waves
     (plan_wave.py --cluster LEAD) with fix-lane waves while a lead's cluster has a ready note, the ready leads taking
     turns, and a lead's lane ends when its cluster is landed. The alternation is the supervisor's rule: a handoff that
     names `wave` names only a wave, never its lane (kb/Work PB2522), and a lead whose ready note has waited through
     more than one wave-type unit is logged `campaign LEAD ready for N h`
  6. run `claude -p` with the unit prompt (first stream-json message on stdin, which stays open so the session outlives
     a model turn that ends with a Workflow in flight; the supervisor closes it when the stream is idle with no
     background task), a fresh session id, stream-json to logs\; watch the context size and wind the unit down past
     -MaxContextTokens or on STOP; kill only after the grace period
  7. validate handoff.json against handoff.schema.json
  8. append one line to units.jsonl
The Claude ACCOUNT the loop spends is a parameter (kb/Work PB2479; design section 2.1): -ConfigDir names its config dir,
default the one account.py resolves from CLAUDE_CONFIG_DIR. The supervisor exports exactly that account to every child
(CLAUDE_CONFIG_DIR set to the dir, or UNSET for the default account), finds the unit transcripts under that dir, and
names the account in its log header and in every units.jsonl line.
Exit codes: 0 stopped (STOP, -MaxUnits, stop-week, -DryRun), 3 another instance runs, 4 circuit breaker,
5 an owner question is waiting in OWNER-QUESTIONS.md, 2 -Cluster names no kb/Work cluster (or -Unit campaign without it),
or -ConfigDir names a config dir model_rules.json `accounts` does not know.

.EXAMPLE
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -DryRun
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -MaxUnits 1 -Unit meter
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -Cluster PB2108 -DryRun
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -ConfigDir C:\Users\brent\.claude-acct2
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    # The owner lifts a quota hold for the named work (hold-session / hold-day only; stop-week still ends the loop).
    [switch]$OverrideHold,
    # The executable to run. A test points it at a fake that emits canned stream-json (a test seam, not a wrapper).
    [string]$ClaudeExe = 'claude',
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' }),
    [string]$RepoDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [int]$MaxContextTokens = 150000,
    [int]$MaxUnits = 0,
    [ValidateSet('acceptEdits', 'auto', 'bypassPermissions', 'dontAsk', 'plan', 'manual')]
    [string]$PermissionMode = 'bypassPermissions',   # owner 2026-10-04 (D1): allowed for the COBOL work; the repo's hooks are the guard
    [double]$GraceMinutes = 30,
    [int]$BorrowDays = 0,
    [ValidateSet('', 'wave', 'campaign', 'land', 'resume', 'meter')]
    [string]$Unit = '',
    # The campaign lane: the kb/Work clusters (each a lead note's id, or a campaign name; several are comma-separated,
    # because `pwsh -File` passes `-Cluster A,B` as one string) whose waves run between fix-lane waves, in turn.
    [string]$Cluster = '',
    # Test seam: the register directory next_unit.py and work.py read (default kb/Work).
    [string]$WorkDir = '',
    [switch]$Watch,
    [string]$Python = 'python',
    [string]$TelemetryDir = '',
    # The Claude account's config dir (default: account.py's resolution of CLAUDE_CONFIG_DIR, i.e. this process's account).
    [string]$ConfigDir = '',
    [int]$FastFailSeconds = 120,
    [int]$IdleCloseSeconds = 20,
    [int]$CheckpointSeconds = 300,
    [int]$StartupTicks = 300,
    [int]$BackoffBaseSeconds = 60
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Here = $PSScriptRoot
$Schema = Join-Path $Here 'handoff.schema.json'
$UnitModel = @{ wave = 'opus'; campaign = 'opus'; land = 'opus'; resume = 'opus'; meter = 'sonnet' }
# A `campaign` unit is a `wave` unit whose plan_wave.py call carries --cluster: one prompt file, one placeholder.
$PromptFile = @{ campaign = 'wave' }
$Allowed = @('opus', 'sonnet')   # never Fable or Mythos: each needs the owner's approval per dispatch (owner 2026-10-02; kb/Work R69 2026-10-06)
New-Item -ItemType Directory -Force -Path $CoordDir, (Join-Path $CoordDir 'logs'), (Join-Path $CoordDir 'scratch') | Out-Null
$env:COBOL_COORD_DIR = $CoordDir
$Lock = Join-Path $CoordDir 'orchestrate.lock'
$StopFile = Join-Path $CoordDir 'STOP'
$StopUnit = Join-Path $CoordDir 'STOP-UNIT'
# The stop files are SCOPED (kb/Work PB2483; coord.py global_stop/fleet_stop; design section 4.6): the owner's GLOBAL stop
# stops every agent of every session, and the loop's FLEET stop only the agents this loop dispatched. The supervisor
# creates only its own fleet stop; a shared scratch\STOP split another session's refuter on 2026-10-07.
$GlobalStop = Join-Path $CoordDir 'scratch/STOP'
$FleetStop = Join-Path $CoordDir 'scratch/STOP-loop'
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

# The loop ends on its own STOP or on the owner's global stop (which stops every agent anyway).
function Test-Stop { return (Test-Path $StopFile) -or (Test-Path $GlobalStop) }

# Sleep until $until, waking every minute so STOP is honoured. Returns $false when STOP appeared.
function Wait-Until([datetime]$until) {
    while ((Get-Date) -lt $until) {
        if (Test-Stop) { return $false }
        Start-Sleep -Seconds ([Math]::Max(1, [Math]::Min(60, ($until - (Get-Date)).TotalSeconds)))
    }
    return -not (Test-Stop)
}

# The supervisor's own frequent handoff (checkpoint.py): never fatal, a missed checkpoint must not end the unit.
function Write-Checkpoint([string]$unit, [string]$sessionId, [datetime]$started, $stats, [int]$bgTasks, [datetime]$lastEventAt) {
    try {
        [void](Invoke-Py @((Join-Path $Here 'checkpoint.py'), 'write', '--unit', $unit, '--session', $sessionId,
            '--started', $started.ToString('o'), '--last-event', $lastEventAt.ToString('o'), '--calls', "$($stats.calls)",
            '--context', "$($stats.context)", '--cost', "$([double]$(if ($null -ne $stats.cost_usd) { $stats.cost_usd } else { 0 }))",
            '--bg-tasks', "$bgTasks", '--repo', $RepoDir))
    } catch { Say "checkpoint not written (the unit continues): $_" }
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

function Build-Prompt([string]$unit, [string]$scratch, [string]$sessionId) {
    $tasksDir = (Join-Path $env:TEMP "claude/E--COBOL/$sessionId/tasks") -replace '\\', '/'   # forward slashes: the unit uses it inside bash
    $sub = {
        param($t)
        $t.Replace('{TASKS_DIR}', $tasksDir).Replace('{COORD}', $CoordDir).Replace('{HANDOFF}', $Handoff).Replace('{STOP_UNIT}', $StopUnit).
           Replace('{PREV_HANDOFF}', $LastHandoff).Replace('{SCRATCH}', $scratch).Replace('{FLEET_STOP}', $FleetStop).Replace('{GLOBAL_STOP}', $GlobalStop).
           Replace('{BORROW_DAYS}', "$BorrowDays").Replace('{CLUSTER_ARG}', $(if ($unit -eq 'campaign') { " --cluster $UnitCluster" } else { '' }))
    }
    $common = & $sub (Get-Content (Join-Path $Here 'units/common.md') -Raw)
    $own = & $sub (Get-Content (Join-Path $Here "units/$(Get-PromptFile $unit).md") -Raw)
    return "$common`n$own"
}

function Get-PromptFile([string]$unit) { if ($PromptFile.ContainsKey($unit)) { $PromptFile[$unit] } else { $unit } }

function Get-ProjectTranscriptDir([string]$sessionId) {
    # The account's config dir holds the unit transcripts (account.py project_dir: projects/<repo key>), never $HOME/.claude.
    return Join-Path $Account.project_dir $sessionId
}

function Invoke-Unit([string]$unit, [string]$model, [string]$sessionId, [string]$logBase) {
    $scratch = Join-Path $CoordDir 'scratch'
    $prompt = Build-Prompt $unit $scratch $sessionId
    # The prompt goes in as the first stream-json message and stdin STAYS OPEN. Once a one-shot `claude -p` model ends a
    # turn the process waits for background tasks only up to CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS (600 s), then
    # terminates them ("Background tasks still running after 600s; terminating": wave 1017's Workflow, 653 s). With stdin
    # open the session never enters that exit wait, and a finishing task's notification wakes the model (probed
    # 2026-10-04 with the ceiling set to 5 s: turn ended at 6 s, task done and model woken at 30 s, session alive).
    # The SUPERVISOR decides when the unit is over (idle, below), never the model's choice of words.
    $claudeArgs = @('-p', '--input-format', 'stream-json', '--model', $model, '--permission-mode', $PermissionMode,
        '--permission-prompts', 'none', '--output-format', 'stream-json', '--verbose', '--session-id', $sessionId)
    if ($unit -eq 'meter') { $claudeArgs += '--chrome' }
    $exe, $pre = Resolve-Launch $ClaudeExe
    $psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
    foreach ($a in @($pre) + $claudeArgs) { $psi.ArgumentList.Add([string]$a) }
    $psi.WorkingDirectory = $RepoDir
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardInputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.Environment['COBOL_COORD_DIR'] = $CoordDir
    # Exactly the loop's account: the dir for a named one; UNSET for the default (set to ~/.claude, Claude Code would look
    # for its sign-in in ~/.claude/.claude.json and find none). account.py child_config_dir is the one rule.
    if ($Account.child_config_dir) { $psi.Environment['CLAUDE_CONFIG_DIR'] = $Account.child_config_dir } else { [void]$psi.Environment.Remove('CLAUDE_CONFIG_DIR') }

    $stats = [ordered]@{ calls = 0; input = 0; output = 0; cache_read = 0; cache_creation = 0; peak_context = 0; peak_subagent_context = 0;
        context = 0; cost_usd = $null; stop_unit_sent = $false; killed = $false }
    $seen = @{}
    $graceMin = if ($unit -in @('wave', 'campaign')) { 3 * $GraceMinutes } else { $GraceMinutes }
    $stopAt = $null
    $watcher = $null
    $log = [System.IO.StreamWriter]::new("$logBase.jsonl", $false, [System.Text.UTF8Encoding]::new($false))
    $proc = [System.Diagnostics.Process]::Start($psi)
    $errTask = $proc.StandardError.ReadToEndAsync()
    # ⛔ NEVER WRITE THE PROMPT SYNCHRONOUSLY. The CLI writes a large `init` event to stdout BEFORE it reads stdin; once that
    # fills the stdout pipe it blocks until we read, and a synchronous write of a prompt larger than the stdin pipe buffer
    # (about 4 KB: the wave prompt grew past it on 2026-10-04) blocks until it reads: a cross-pipe deadlock, found as a wave
    # unit that sat two hours with a 0-byte log. The write is a task, and the read loop below starts at once.
    $promptWrite = $proc.StandardInput.WriteLineAsync((@{ type = 'user'; message = @{ role = 'user'; content = $prompt } } | ConvertTo-Json -Compress -Depth 5))
    $startTicks = 0          # loop ticks (one per second waited) with no event at all: immune to the clock, unlike a timestamp
    $anyEvent = $false
    $resultSeen = $false     # the model has ended a turn and nothing has started since
    $bgTasks = 0             # background tasks (a Workflow, a gate) the session reports as running
    $lastEventAt = Get-Date
    $stdinClosed = $false
    $unitStarted = Get-Date
    $lastCheckpoint = Get-Date
    $lastBgChecked = 0
    Write-Checkpoint $unit $sessionId $unitStarted $stats $bgTasks $lastEventAt   # one at once: a crash in the first minutes still leaves one
    if ($Watch -and $unit -eq 'wave') {
        try {
            $watcher = Start-Process -PassThru -WindowStyle Hidden -FilePath (Get-Command pwsh).Source -ArgumentList @(
                '-NoProfile', '-File', (Join-Path $Here 'open-watchers.ps1'), '-SessionDir', (Get-ProjectTranscriptDir $sessionId))
        } catch { Say "watchers did not start (the unit continues): $_" }
    }
    try {
        $read = $proc.StandardOutput.ReadLineAsync()
        while ($true) {
            if (-not $read.Wait(1000)) {
                # Startup watchdog: a unit that has emitted NO event after -StartupTicks ticks is hung at start (a deadlock, a
                # network or hook stall); a healthy session emits its init event within seconds. Count ticks, not wall time,
                # so a suspended machine cannot trip it. The unit counts as failed and the supervisor synthesizes its handoff.
                if (-not $anyEvent) {
                    $startTicks++
                    if ($startTicks -ge $StartupTicks -and -not $proc.HasExited) {
                        Say "no event from the unit in $StartupTicks s of waiting: it is hung at start; killing it (the loop continues)"
                        $proc.Kill($true)
                        $stats.killed = $true
                    }
                }
            }
            if ($promptWrite -and $promptWrite.IsCompleted) {
                if ($promptWrite.IsFaulted) {
                    Say "the prompt could not be delivered ($($promptWrite.Exception.GetBaseException().Message)); killing the unit"
                    if (-not $proc.HasExited) { $proc.Kill($true) }
                    $stats.killed = $true
                }
                $promptWrite = $null
            }
            if ($read.IsCompleted) {
                $line = $read.Result
                if ($null -eq $line) { break }
                $anyEvent = $true
                $log.WriteLine($line); $log.Flush()
                $lastEventAt = Get-Date
                $ev = $null
                try { $ev = $line | ConvertFrom-Json -Depth 64 } catch { }
                if ($ev -and $ev.PSObject.Properties['type']) {
                    if ($ev.type -eq 'assistant') { $resultSeen = $false }
                    elseif ($ev.type -eq 'result') { $resultSeen = $true }
                    elseif ($ev.type -eq 'system' -and $ev.subtype -eq 'background_tasks_changed') { $bgTasks = @($ev.tasks).Count }
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
                            $ctx = (& $g 'input_tokens') + (& $g 'cache_read_input_tokens') + (& $g 'cache_creation_input_tokens')
                            # A subagent's messages stream into this log too, tagged with parent_tool_use_id: they are another
                            # transcript with its own turn cap (maxTurns), so only the unit's OWN messages measure its context.
                            if ($ev.PSObject.Properties['parent_tool_use_id'] -and $ev.parent_tool_use_id) {
                                $stats.peak_subagent_context = [Math]::Max($stats.peak_subagent_context, $ctx)
                            } else {
                                $stats.context = $ctx
                                $stats.peak_context = [Math]::Max($stats.peak_context, $ctx)
                            }
                        }
                    } elseif ($ev.type -eq 'result' -and $ev.PSObject.Properties['total_cost_usd']) {
                        $stats.cost_usd = $ev.total_cost_usd
                    }
                }
                $read = $proc.StandardOutput.ReadLineAsync()
            }
            # The unit is over when its model ended a turn, no background task is running and the stream has been quiet
            # for -IdleCloseSeconds (a finishing task's completion event arrives just after the task list empties, and
            # wakes the model, so a close on the first idle instant could cut a woken turn off).
            if (-not $stdinClosed -and $resultSeen -and $bgTasks -eq 0 -and ((Get-Date) - $lastEventAt).TotalSeconds -ge $IdleCloseSeconds) {
                try { $proc.StandardInput.Close() } catch { }
                $stdinClosed = $true
            }
            # Frequent handoffs: a checkpoint every -CheckpointSeconds, and at once (at most every 10 s) when the set of
            # background tasks changes, because that is when a Workflow starts, a train lands or a gate ends.
            $sinceCp = ((Get-Date) - $lastCheckpoint).TotalSeconds
            if ($CheckpointSeconds -gt 0 -and ($sinceCp -ge $CheckpointSeconds -or ($bgTasks -ne $lastBgChecked -and $sinceCp -ge 10))) {
                Write-Checkpoint $unit $sessionId $unitStarted $stats $bgTasks $lastEventAt
                $lastCheckpoint = Get-Date
                $lastBgChecked = $bgTasks
            }
            # Two reasons to wind the unit down gracefully: its context passed the cap, or the owner created STOP. Both do
            # the same thing, so no work is lost: STOP-UNIT (the unit hands off at its next step) and the LOOP's own fleet
            # stop (every implementer and lander it dispatched checkpoints and returns SPLIT), then wait for the handoff.
            # Never the global scratch\STOP: that is the owner's, and every session's agents obey it (kb/Work PB2483).
            $windDown = $null
            if ($stats.context -gt $MaxContextTokens) { $windDown = "context $($stats.context) > ${MaxContextTokens}" }
            elseif (Test-Stop) { $windDown = 'STOP file present' }
            if (-not $stats.stop_unit_sent -and $windDown) {
                Say "${windDown}: winding the unit down (STOP-UNIT and the loop's fleet stop $FleetStop; the unit checkpoints and hands off, the loop then ends if STOP is set)"
                New-Item -ItemType File -Force -Path $StopUnit, $FleetStop | Out-Null
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
        Write-Checkpoint $unit $sessionId $unitStarted $stats $bgTasks $lastEventAt   # the final state, for the log and a successor
    } finally {
        $log.Close()
        if ($watcher -and -not $watcher.HasExited) { Stop-Process -Id $watcher.Id -Force -ErrorAction SilentlyContinue }
    }
    $err = $errTask.Result
    if ($err) { Set-Content -Path "$logBase.stderr.txt" -Value $err -Encoding utf8 }
    $stats.Remove('context')
    return @{ exit = $proc.ExitCode; stats = $stats }
}

# The account this loop spends: resolved once, exported to every child (the Python tools resolve the same one from it).
$acctArgs = @((Join-Path $Here 'account.py'), '--json', '--repo', $RepoDir) + $(if ($ConfigDir) { @('--config-dir', $ConfigDir) } else { @() })
$acctOut = & $Python @acctArgs 2>&1
if ($LASTEXITCODE -ne 0) { Say "$($acctOut -join ' '); refusing to start"; exit 2 }
$Account = ($acctOut -join "`n") | ConvertFrom-Json
if ($Account.child_config_dir) { $env:CLAUDE_CONFIG_DIR = $Account.child_config_dir } else { Remove-Item Env:CLAUDE_CONFIG_DIR -ErrorAction SilentlyContinue }
Say "account: $($Account.name) (config dir $($Account.config_dir); week resets weekday $($Account.weekly_reset.weekday) $($Account.weekly_reset.hour):$('{0:d2}' -f [int]$Account.weekly_reset.minute) $($Account.weekly_reset.tz))"

# The campaign lane needs a cluster the register knows: a misspelled one would quietly run the fix lane alone.
$Clusters = [Collections.Generic.List[string]]@($Cluster -split '[,\s]+' | Where-Object { $_ })
if ($Unit -eq 'campaign' -and -not $Clusters.Count) { Say '-Unit campaign needs -Cluster <lead>'; exit 2 }
foreach ($lead in $Clusters) {
    $wa = @((Join-Path (Split-Path $Here -Parent) 'spec/work.py'), 'next', '--cluster', $lead, '--json')
    if ($WorkDir) { $wa += @('--work', $WorkDir) }
    $view = & $Python @wa 2>&1
    if ($LASTEXITCODE -ne 0) { Say "no kb/Work note names the cluster '$lead' in its cluster: list; refusing to start"; exit 2 }
    $v = ($view -join "`n") | ConvertFrom-Json
    Say "campaign lane: cluster $lead, $(@($v.notes).Count) open note(s), $(@($v.notes | Where-Object { $_.ready }).Count) ready"
}
# The lead the current `campaign` unit runs (its prompt's --cluster and its units.jsonl line).
$UnitCluster = ''

if (-not (Take-Lock)) { exit 3 }
$exitCode = 0
try {
    $leftover = Join-Path $CoordDir 'checkpoint.json'
    if ((Test-Path $leftover) -and -not $DryRun) {
        # The previous supervisor died mid-unit (a reboot, a kill): its last checkpoint is the only record. Turn it into the
        # handoff the dead unit never wrote, so the first unit of this run is a `resume` with facts.
        $cpUnit = [string]((Get-Content $leftover -Raw | ConvertFrom-Json).unit)
        Say "a checkpoint of unit '$cpUnit' survives from a supervisor that died mid-unit: synthesizing its handoff"
        [void](Invoke-Py @((Join-Path $Here 'checkpoint.py'), 'synthesize', '--unit', $cpUnit, '--out', $LastHandoff, '--repo', $RepoDir))
        Move-Item $leftover (Join-Path $CoordDir "logs/$(Get-Date -Format 'yyyyMMdd-HHmmss')-$cpUnit.checkpoint.json") -Force
    }
    $failures = 0
    $ran = 0
    $lastFailed = $false
    $lastStarted = $null
    while ($true) {
        if (Test-Stop) { Say "STOP file present ($(if (Test-Path $StopFile) { $StopFile } else { "the owner's global $GlobalStop" })): ending the loop"; break }
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
        Say "budget ($($budget.account)): weekly $($budget.weekly_est_pct) % of $($budget.allowance_pct) %, session $($budget.session_est_pct) % -> $($budget.decision)$(if (-not $budget.anchor) { " (no $($budget.account) meter reading this week)" })$(if ($budget.unstamped_readings) { " ($($budget.unstamped_readings) unstamped reading(s) anchor nothing)" })"
        if ($budget.decision -eq 'stop-week') { Say 'weekly cap reached: ending the loop'; break }
        $hold = $budget.decision -in @('hold-session', 'hold-day')
        if ($hold -and $OverrideHold -and -not $DryRun) { Say "owner override: $($budget.decision) lifted (-OverrideHold)"; $hold = $false }
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
            # -Unit campaign runs the first lead; next_unit.py takes the turns from then on.
            $choice = [pscustomobject]@{ unit = $Unit; reason = 'named by -Unit' }
            if ($Unit -eq 'campaign') { $choice | Add-Member cluster $Clusters[0] }
        } else {
            $na = @((Join-Path $Here 'next_unit.py'), '--repo', $RepoDir)
            if (Test-Path $LastHandoff) { $na += @('--handoff', $LastHandoff) }
            if ($lastFailed) { $na += '--last-failed' }
            if ($lastStarted) { $na += @('--last-started', $lastStarted.ToString('o')) }
            foreach ($lead in $Clusters) { $na += @('--cluster', $lead) }
            if ($Clusters.Count -and $WorkDir) { $na += @('--work', $WorkDir) }
            $choice = Invoke-Py $na | ConvertFrom-Json
        }
        # With -Cluster every choice carries each lead's lane state (`campaigns`) and the starved leads (`starved`,
        # kb/Work PB2522): a landed lead's lane ends, and a ready note that waits through wave after wave is said aloud.
        $campaigns = if ($choice.PSObject.Properties['campaigns']) { $choice.campaigns } else { $null }
        if ($campaigns) {
            foreach ($lead in @($Clusters)) {
                if ($campaigns.PSObject.Properties[$lead] -and $campaigns.$lead -eq 'landed') {
                    [void]$Clusters.Remove($lead)
                    Say "campaign ${lead}: every note is landed or retired; the campaign lane ends$(if ($Clusters.Count) { " (campaigns $($Clusters -join ', ') go on)" } else { ' and the fix lane continues' })"
                }
            }
            foreach ($s in @($choice.starved | Where-Object { $_ })) {
                Say "campaign $($s.cluster) ready for $('{0:0.0}' -f [double]$s.hours) h ($($s.waves) wave-type unit(s)), last campaign wave at $(if ($s.last_campaign_at) { Get-Date $s.last_campaign_at -Format 'yyyy-MM-dd HH:mm' } else { 'none recorded' })"
            }
        }
        $UnitCluster = if ($choice.PSObject.Properties['cluster']) { [string]$choice.cluster } else { '' }
        if ($choice.unit -eq 'campaign' -and $UnitCluster -notin $Clusters) {
            $choice = [pscustomobject]@{ unit = 'wave'; reason = "a campaign was named but no campaign lane runs$(if ($UnitCluster) { " for $UnitCluster" }) (no -Cluster, or its cluster landed): $($choice.reason)" }
            $UnitCluster = ''
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
            Say "DRY RUN: would run unit '$($choice.unit)' ($($choice.reason)) on $model, session $sessionId, account $($Account.name) (transcripts under $(Get-ProjectTranscriptDir $sessionId))"
            Say "DRY RUN: $ClaudeExe -p <units/common.md + units/$(Get-PromptFile $choice.unit).md$(if ($choice.unit -eq 'campaign') { " with --cluster $UnitCluster" })> --model $model --permission-mode $PermissionMode --permission-prompts none --output-format stream-json --verbose --session-id $sessionId$(if ($choice.unit -eq 'meter') { ' --chrome' }) > $logBase.jsonl"
            break
        }

        # The loop's fleet stop left by a previous wind-down would stop this unit's fleet at its first step. Only the
        # loop's own: the owner's global stop is never the supervisor's to remove.
        Remove-Item $StopUnit, $Handoff, $FleetStop, (Join-Path $CoordDir 'milestones.jsonl'), (Join-Path $CoordDir 'checkpoint.json') -Force -ErrorAction SilentlyContinue
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
        if (-not $valid) {
            # No usable handoff from the model (a crash, a kill, a terminated background task): the supervisor writes
            # one from its last checkpoint and the worktrees as they are now, so the successor starts with facts, not a
            # guess. The unit still counts as failed for the breaker.
            try {
                [void](Invoke-Py @((Join-Path $Here 'checkpoint.py'), 'synthesize', '--unit', $choice.unit, '--out', $Handoff, '--repo', $RepoDir))
                Copy-Item $Handoff $LastHandoff -Force
                Copy-Item $Handoff "$logBase.handoff.synthesized.json" -Force
                $outcome = "synthesized ($outcome)"
            } catch { Say "handoff not synthesized: $_" }
        }
        # Moved, not copied: a checkpoint.json that is still here at the next supervisor start means the supervisor itself died mid-unit.
        if (Test-Path (Join-Path $CoordDir 'checkpoint.json')) { Move-Item (Join-Path $CoordDir 'checkpoint.json') "$logBase.checkpoint.json" -Force }
        if (Test-Path (Join-Path $CoordDir 'milestones.jsonl')) { Move-Item (Join-Path $CoordDir 'milestones.jsonl') "$logBase.milestones.jsonl" -Force }
        $failed = ($r.exit -ne 0) -or (-not $valid) -or (($duration -lt $FastFailSeconds) -and ($outcome -ne 'done'))
        $failures = if ($failed) { $failures + 1 } else { 0 }
        $lastFailed = $failed
        $lastStarted = $started
        $line = [ordered]@{ unit = $choice.unit; reason = $choice.reason; model = $model; account = $Account.name; session_id = $sessionId
            started_at = $started.ToString('o'); ended_at = (Get-Date).ToString('o'); duration_s = [Math]::Round($duration, 1)
            exit_code = $r.exit; handoff_outcome = $outcome; next_unit = $(if ($h -and $h.PSObject.Properties['next_unit']) { $h.next_unit } else { $null })
            failed = $failed; log = "$logBase.jsonl" }
        # The campaign record next_unit.py reads back: whose turn a campaign was, and each lead's lane at this choice
        # (the round-robin and the starvation measure, kb/Work PB2522).
        if ($UnitCluster) { $line['cluster'] = $UnitCluster }
        if ($campaigns) { $line['campaigns'] = $campaigns }
        foreach ($k in $r.stats.Keys) { $line[$k] = $r.stats[$k] }
        Add-Content -Path $UnitsLog -Encoding utf8 -Value ($line | ConvertTo-Json -Compress -Depth 5)
        Say "unit '$($choice.unit)' ended: exit $($r.exit), handoff $outcome, $([Math]::Round($duration)) s, $($r.stats.calls) calls$(if ($failed) { " — FAILURE $failures of 3" })"
        $ran++
        # A headless unit cannot publish the owner's ledger artifact (no Artifact tool); say loudly when a publish is owed
        # so the attended session does it (ledger_state.py compares the page's stamp with the last published one).
        try {
            $ls = Invoke-Py @((Join-Path $Here 'ledger_state.py'), 'owed', '--repo', $RepoDir)
            if ($ls -match '^owed') {
                Say "LEDGER PUBLISH OWED ($ls): render with gen_ledger.py (it writes $CoordDir\ledger.html and prints this account's artifact URL), publish it there, then ledger_state.py mark-published (--url the first time an account publishes)"
                # The attended operator session is woken by its mailbox, not by this log (kb/Work PB2482): one open
                # `publish` message per stamp (--unless-pending), pointing at this unit's log.
                $stampOwed = ($ls -split ' ')[1]
                try { [void](Invoke-Py @((Join-Path $Here 'mailbox.py'), '--coord', $CoordDir, 'send', '--to', 'operator', '--from', 'loop',
                    '--kind', 'publish', '--subject', "Ledger publish owed at $stampOwed ($($Account.name))",
                    '--body', "The supervisor found the ledger publish owed after unit '$($choice.unit)': $ls. Render with gen_ledger.py, publish to this account's artifact, then ledger_state.py mark-published.",
                    '--ref', "$logBase.jsonl", '--session', "orchestrate.ps1 pid $PID", '--unless-pending')) }
                catch { Say "publish message not posted to the operator's mailbox (the loop continues): $_" }
            }
        } catch { Say "ledger state not read (the loop continues): $_" }

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
