## Entry 1763 — 2026-09-28 16:50 PDT — The project consumes the current public skills: pin moved v1.10.0 → v1.13.0

After the owner's Claude Code restart the `brent-tools` plugin loaded from `tools/claude-skills`, which confirms the
self-hosting (PB1699) works. But the pin was still v1.10.0, so the project was consuming the skills from BEFORE
today's validation:
- no vetted-only LEARNINGS.md;
- no "not yet validated" labels;
- no devlog skill;
- none of the four encoded practices (re-probe, registrar re-check, worktree battery, conflict markers).

The pin moves to v1.13.0, and the four overlays and PB1699 name it.

The version check did its job on the way: it went RED because kb/Work/PB1701 still named the guard's public path,
which v1.11.0 withdrew. The note is corrected; the project's copy is `scripts/hooks/status_guard.py`.
`check_practices` and `work.py check` are GREEN. Every fleet script still resolves at the new pin (orient,
fix_clusters, status_delta, stall_watch, fleet_config).

The owner posted his reply to Moonveil's "whose job is the accuracy half?". The answer is the lander, which verifies
each "done" item against its commit, test and gate log, and it names the honest gap: nothing verifies a wrong "next
step" line.

