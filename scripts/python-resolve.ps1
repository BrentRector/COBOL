# ⛔ THE ONE PYTHON INTERPRETER a PowerShell gate entry runs: the pwsh twin of scripts/python-resolve.sh (kb/Work PB1637,
# PB3197). Dot-sourced, never run: `. (Join-Path $PSScriptRoot 'python-resolve.ps1')`, then `& $PY script.py …`.
#
# WHY A TWIN. The probe runs before any Python can, so it is written in the shell that starts the gate, once per shell:
# this file and python-resolve.sh probe the SAME candidates in the SAME order with the SAME floor probe, and
# PythonInterpreterDriftTests holds them equal. python-resolve.sh says why each candidate is PROBED by running it: a
# name on PATH is not an interpreter (on the Windows dev box `python3` is the Microsoft Store alias, which runs, prints
# "Python was not found" and exits non-zero).
#
# Nothing runnable ⇒ a TERMINATING error naming what was tried. A dot-sourced `exit` would end only this file and leave
# its caller running with no interpreter, so the refusal is thrown into the caller: build-local.ps1 reports its gate
# NOT RUN; any other caller stops with exit 1. A gate that cannot run its checks is never silently skipped.
$PY = & {
    foreach ($candidate in @($env:PYTHON, 'python3', 'python')) {
        if (-not $candidate) { continue }
        try {
            & $candidate -c 'import sys; sys.exit(0 if sys.version_info >= (3, 12) else 1)' *> $null
        } catch [System.Management.Automation.CommandNotFoundException] {
            continue
        }
        if ($LASTEXITCODE -eq 0) { return $candidate }
    }
}
if (-not $PY) {
    throw 'python-resolve: ⛔ no runnable Python >= 3.12 (tried $PYTHON, python3, python) — the gate cannot run'
}
