# A fake `claude -p` for test_orchestrate.ps1: emits canned stream-json and writes a handoff, per FAKE_CLAUDE_MODE.
#   good       two model calls, a valid handoff, exit 0
#   fastfail   exit 1 at once, no output
#   nohandoff  one call, exit 0, no handoff
#   bigcontext one call whose context exceeds any small -MaxContextTokens, then waits for STOP-UNIT and hands off split
#   owner      a valid handoff carrying an owner question
#   wakes      ends a turn with a background task running, is woken when it finishes, then hands off done
# Like the real session, a mode that emits `result` then STAYS ALIVE until the supervisor closes stdin (eof.txt records it).
# Every invocation appends its arguments to FAKE_CLAUDE_MARK, so a test can prove whether it ran and with what.
$ErrorActionPreference = 'Stop'
# The prompt arrives as the first stream-json line on stdin (the supervisor keeps stdin open); one mark line carries both.
$firstLine = [Console]::In.ReadLine()
if ($env:FAKE_CLAUDE_MARK) { Add-Content -Path $env:FAKE_CLAUDE_MARK -Value ((($args -join ' | ') + ' | STDIN ' + $firstLine) -replace '[\r\n]+', ' ') -Encoding utf8 }
$coord = $env:COBOL_COORD_DIR
$handoff = Join-Path $coord 'handoff.json'
$sid = $args[[array]::IndexOf($args, '--session-id') + 1]
function Emit($o) { [Console]::Out.WriteLine(($o | ConvertTo-Json -Compress -Depth 10)); [Console]::Out.Flush() }
function Call([string]$id, [long]$cacheRead) {
    $usage = @{ input_tokens = 3; output_tokens = 200; cache_read_input_tokens = $cacheRead; cache_creation_input_tokens = 1000 }
    # Two content blocks of one model call, each repeating the usage, as the real stream does.
    Emit @{ type = 'assistant'; session_id = $sid; message = @{ id = $id; content = @(@{ type = 'text'; text = 'working' }); usage = $usage } }
    Emit @{ type = 'assistant'; session_id = $sid; message = @{ id = $id; content = @(@{ type = 'tool_use'; name = 'Bash'; input = @{ command = 'ls' } }); usage = $usage } }
}
function WaitEof {
    [void][Console]::In.ReadToEnd()   # returns only when the supervisor closes stdin
    Set-Content -Path (Join-Path $coord 'eof.txt') -Value 'stdin closed by the supervisor'
}
function Hand($o) { Set-Content -Path $handoff -Value ($o | ConvertTo-Json -Depth 10) -Encoding utf8 }

Emit @{ type = 'system'; subtype = 'init'; session_id = $sid }
switch ($env:FAKE_CLAUDE_MODE) {
    'fastfail' { exit 1 }
    'nohandoff' { Call 'msg_1' 5000; exit 0 }
    'bigcontext' {
        Call 'msg_1' 900000
        $deadline = (Get-Date).AddSeconds(60)
        while (-not (Test-Path (Join-Path $coord 'STOP-UNIT')) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 200 }
        Hand @{ schema_version = 1; unit = 'wave'; outcome = 'split'; summary = 'stopped at STOP-UNIT'; next_unit = 'resume' }
        exit 0
    }
    'owner' {
        Call 'msg_1' 5000
        Hand @{ schema_version = 1; unit = 'resume'; outcome = 'owner-question'; summary = 'needs the owner'
            owner_question = @{ question = 'Adopt reading A or reading B of 13.18.38.3 SR6?'; context = 'kb/Work/PB1' } }
        exit 0
    }
    'crash' {
        # A unit that records a milestone, then dies with no handoff (a kill, a terminated background task).
        Call 'msg_1' 5000
        Add-Content -Path (Join-Path $coord 'milestones.jsonl') -Value '{"at":"t1","what":"plan written for wave 9"}' -Encoding utf8
        exit 1
    }
    'stopsme' {
        # The owner creates STOP while this unit runs; the supervisor must wind the unit down (STOP-UNIT and the fleet STOP).
        Call 'msg_1' 5000
        Set-Content -Path (Join-Path $coord 'STOP') -Value 'owner'
        $deadline = (Get-Date).AddSeconds(60)
        while (-not (Test-Path (Join-Path $coord 'STOP-UNIT')) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 200 }
        Set-Content -Path (Join-Path $coord 'fleet-stop-seen.txt') -Value (Test-Path (Join-Path $coord 'scratch/STOP'))
        Hand @{ schema_version = 1; unit = 'wave'; outcome = 'split'; summary = 'wound down by STOP'; next_unit = 'resume' }
        Emit @{ type = 'result'; subtype = 'success'; session_id = $sid }
        WaitEof
        exit 0
    }
    'wakes' {
        Call 'msg_1' 5000
        Emit @{ type = 'system'; subtype = 'background_tasks_changed'; tasks = @(@{ id = 'wf1' }); session_id = $sid }
        Emit @{ type = 'result'; subtype = 'success'; stop_reason = 'end_turn'; session_id = $sid }
        Start-Sleep -Seconds 2
        Emit @{ type = 'system'; subtype = 'background_tasks_changed'; tasks = @(); session_id = $sid }
        Emit @{ type = 'system'; subtype = 'task_notification'; task_id = 'wf1'; status = 'completed'; session_id = $sid }
        Call 'msg_2' 7000
        Hand @{ schema_version = 1; unit = 'wave'; outcome = 'done'; summary = 'woken by the task completion'; next_unit = 'meter' }
        Emit @{ type = 'result'; subtype = 'success'; total_cost_usd = 0.42; session_id = $sid }
        WaitEof
        exit 0
    }
    default {
        Call 'msg_1' 5000
        Call 'msg_2' 7000
        Hand @{ schema_version = 1; unit = 'wave'; outcome = 'done'; summary = 'fake unit done'; next_unit = 'meter' }
        Emit @{ type = 'result'; subtype = 'success'; total_cost_usd = 0.42; session_id = $sid }
        WaitEof
        exit 0
    }
}
