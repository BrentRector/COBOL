# A fake `claude -p` for test_orchestrate.ps1: emits canned stream-json and writes a handoff, per FAKE_CLAUDE_MODE.
#   good       two model calls, a valid handoff, exit 0
#   fastfail   exit 1 at once, no output
#   nohandoff  one call, exit 0, no handoff
#   bigcontext one call whose context exceeds any small -MaxContextTokens, then waits for STOP-UNIT and hands off split
#   owner      a valid handoff carrying an owner question
# Every invocation appends its arguments to FAKE_CLAUDE_MARK, so a test can prove whether it ran and with what.
$ErrorActionPreference = 'Stop'
if ($env:FAKE_CLAUDE_MARK) { Add-Content -Path $env:FAKE_CLAUDE_MARK -Value (($args -join ' | ') -replace '[\r\n]+', ' ') -Encoding utf8 }
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
    default {
        Call 'msg_1' 5000
        Call 'msg_2' 7000
        Hand @{ schema_version = 1; unit = 'wave'; outcome = 'done'; summary = 'fake unit done'; next_unit = 'meter' }
        Emit @{ type = 'result'; subtype = 'success'; total_cost_usd = 0.42; session_id = $sid }
        exit 0
    }
}
