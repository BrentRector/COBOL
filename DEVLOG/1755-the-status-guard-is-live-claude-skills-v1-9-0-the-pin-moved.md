## Entry 1755 — 2026-09-28 11:51 PDT — The status guard is live: claude-skills v1.9.0, the pin moved, hooks wired, "after every commit"

The owner will post a reply saying the guard has been added, and asked that it be pushed first. It is now deployed
as decided in PB1701.

**Public.** claude-skills v1.9.0 (520a48b, tagged) publishes `agent-fleet/references/status_guard.py`:
- it refuses a `git commit` while STATUS.md does not describe HEAD (PreToolUse, no `if` filter);
- it refuses the agent's finish in that state (Stop / SubagentStop);
- it is silent without STATUS.md and fails open; the self-test covers 14 cases.
SKILL.md §4 and the README carry the measured reasons: 3 of 14 finished branches were stale with no crash; the stamp
test (11/44 coverage misjudgments without the stamp, 0/44 with it; −17 % tokens overall, −31 % when current); a
per-command reminder rejected at about +20 % cost; the `if` filter's blind spot. The leak scan was clean.

**Project.**
- The `tools/claude-skills` pin moved to v1.9.0. The four overlays and PB1699 name it, and `check_practices.py`'s
  version check went RED on the one place still naming 1.8.1 until that was fixed, so the drift check did its job.
- `.claude/settings.json` calls the hook directly from the submodule: PreToolUse on Bash and PowerShell, Stop, and a
  new SubagentStop.
- **Sibling fixed:** `devlog_staged.py` sat behind the same `if: Bash(git commit*)` filter, which misses
  `git add -A && git commit …`. The filter is removed; the script already looks for `git commit` anywhere.
- The dispatch spec, the fix-lane brief and MANDATORY-PRACTICES P4 now say "after EVERY commit", including the small
  trailing ones, and that the hook enforces it.
- Verified on real trees: the main checkout is silent, and the leftover unstamped wave-68 U worktree is blocked on
  Stop with the exact stamp to write.

Hooks load at session start, so wave 70, running in this session, is unaffected; the guard takes effect at the owner's
next restart. PB1701 is `half`: production measurement of waves 71+ against the 21 % baseline closes it.

