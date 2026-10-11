# A `python` for the orchestrate.ps1 self-tests (orchestrate.ps1 -Python): every call goes to the real python, except the
# FIRST `checkpoint.py write` in a coordination directory, which first sleeps FAKE_PYTHON_CHECKPOINT_SECONDS (default
# 40): the worktree probe on a loaded host (kb/Work PB2590, 2026-10-10: minutes with six gates running). A supervisor
# whose event loop waits on that write cannot read the unit or tick its startup watchdog until it returns.
$ErrorActionPreference = 'Stop'
if ($args.Count -ge 2 -and ([string]$args[0]).EndsWith('checkpoint.py') -and $args[1] -eq 'write') {
    $mark = Join-Path $env:COBOL_COORD_DIR 'fake-python-slow-checkpoint'
    if (-not (Test-Path $mark)) {
        Set-Content -Path $mark -Value (Get-Date -Format o)
        Start-Sleep -Seconds ([int]($env:FAKE_PYTHON_CHECKPOINT_SECONDS ?? '40'))
    }
}
& python @args
exit $LASTEXITCODE
