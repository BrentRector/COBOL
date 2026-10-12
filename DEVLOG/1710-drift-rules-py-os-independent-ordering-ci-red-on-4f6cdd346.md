## Entry 1710 — 2026-09-25 13:06 PDT — drift_rules.py: OS-independent ordering (CI red on 4f6cdd346)

The drift-rules audit landed in Entry 1709 went red in the Linux CI `audits` job: `docs/DRIFT_RULES.md is STALE`.
Cause: sorting `Path` objects is case-INsensitive on Windows and case-sensitive on Linux, so the index generated
locally listed two rows in a different order from CI's. `drift_rules.py` now sorts on the repo-relative POSIX string;
`--check` verified current on Windows and under WSL Linux. The public `rule_index.py`
(BrentRector/claude-skills 9c68b2c) carried the same sort and got the same fix — the sibling sweep.

