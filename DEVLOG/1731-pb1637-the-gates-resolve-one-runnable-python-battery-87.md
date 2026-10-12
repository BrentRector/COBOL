## Entry 1731 — 2026-09-27 04:40 PDT — PB1637: the gates resolve ONE runnable Python; battery #87 restarted

Battery #87's first attempt (pinned at 2b5f95282) printed four ⛔ citation-audit lines, but none of the audits had
run. `citations.log` held four "Python was not found" messages from the Microsoft Store alias `python3`. Since the
2026-09-24 CPython 3.14 PATH change, that name is on PATH yet not an interpreter. Phase 3's GnuCOBOL differential and
guard-fast's Unit/Integration leg reporters called it too. CI's ubuntu `python3` is real, which is why only this host
showed it. `scripts/python-resolve.sh` is now the one resolver, sourced by `battery.sh` and `guard-fast.sh`. It runs
each candidate (`$PYTHON`, `python3`, `python`, requiring >= 3.12), exports `$PY`, and fails loudly when none runs.
Verified on Windows (`python`), WSL (`python3`) and an empty PATH (rc 1). The stale battery and its child processes
were stopped. Battery #87 re-runs from the head that carries this fix.

