## Entry 1861 — 2026-10-04 09:10 PDT — `gen_ledger.py` back-fills one trend point per inventory-moving commit

Root cause of the stale ledger found in entry 1860: a generator run measured ONE tree and appended at most one point, so a
session that landed several trains between runs recorded one point for all of them. `missed_points()` now measures every
commit that touched `traceability-inventory.json` after the series' last point, each from its own tree
(`git show <sha>:<inventory>`), and `trend_series` appends them before the current head's point, so the series is one point per
GAP-moving landing however rarely the generator runs. Verified by truncating the recorded series at train 1010 and
re-deriving the nine later points: identical gap, closed and dns for every sha; a complete series yields none; `--check` still
reports the rendered page matching. `references/landing.md` now says the ledger is refreshed in the same turn as each train's
landing report.

**Files:** `scripts/spec/gen_ledger.py`, `.claude/skills/workstream/references/landing.md`. No diagnostic code used.

