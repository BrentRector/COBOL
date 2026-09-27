#!/bin/bash
# ⛔ THE ONE PYTHON INTERPRETER the gate scripts run — sourced, never executed: `. "$(dirname "$0")/python-resolve.sh"`,
# then `"$PY" script.py …`.
#
# WHY THIS EXISTS (kb/Work PB1637). The gates spelled the interpreter `python3`. On the Windows dev box that name is
# the Microsoft Store APP-EXECUTION ALIAS once the real CPython install sits on PATH only as `python` (the 2026-09-24
# CPython 3.14 PATH change): it EXISTS, so `command -v python3` finds it, and it RUNS, printing "Python was not
# found" and exiting non-zero. Battery #87's phase −1 therefore ran none of its four citation audits and reported four
# ⛔ lines whose cause was not the tree, and guard-fast's leg reporter would have turned green legs red for the same
# reason. A name on PATH is not an interpreter, so each candidate is PROBED by running it.
#
# Candidates in order: an explicit $PYTHON (the override), `python3` (Linux CI, WSL, macOS), `python` (Windows).
# The probe also enforces the floor the scripts are written for (3.12: PEP 695 syntax and friends).
# Nothing runnable ⇒ exit non-zero, loudly: a gate that cannot run its checks is RED, never silently skipped.
PY=""
for __py_cand in ${PYTHON:+"$PYTHON"} python3 python; do
    if "$__py_cand" -c 'import sys; sys.exit(0 if sys.version_info >= (3, 12) else 1)' >/dev/null 2>&1; then
        PY="$__py_cand"
        break
    fi
done
unset __py_cand
if [ -z "$PY" ]; then
    echo "python-resolve: ⛔ no runnable Python >= 3.12 (tried \$PYTHON, python3, python) — the gate cannot run" >&2
    exit 1
fi
export PY
