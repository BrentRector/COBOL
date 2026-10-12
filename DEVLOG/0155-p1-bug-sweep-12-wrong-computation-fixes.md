## Entry 155 — 2026-03-27: P1 Bug Sweep — 12 Wrong-Computation Fixes

**Context:** After P0 fixes (Entry 154), dispatched 4 parallel agents for all 12 P1 bugs.
First attempt lost all work when I stashed to investigate a guard regression. Re-dispatched
cleanly — all 4 agents completed successfully.

**Fixes (12 bugs across 22 source files):**
- Bug 9: PERFORM WITH TEST AFTER — `IsTestAfter` flag on `BoundPerformStatement`, do-while lowering
- Bug 10: MOVE source subscript evaluated once — `IrCachedLocation` wrapper, CIL locals reuse
- Bug 11: DECIMAL-POINT IS COMMA dead code — removed identical ternary branches
- Bug 12: INTEGER intrinsic — `Math.Floor` (was `Math.Truncate`)
- Bug 13: MOD intrinsic — floor-based modulo (was C# `%`)
- Bug 14: WRITE ADVANCING identifier — dynamic `ReadFieldAsInt` (was hard-coded 1)
- Bug 15: ACCEPT DATE/DAY — YYYYMMDD/YYYYDDD lexer tokens, split runtime formatting
- Bug 16: Signed DISPLAY default — `TrailingOverpunch` for PIC S9 DISPLAY (was None)
- Bug 17: IndexedFileHandler — removed `TrimEnd()` from key comparison
- Bug 18: RelativeFileHandler — parse ASCII digits (was `BitConverter.ToInt32`)
- Bug 19: SEARCH ALL — compile-time unrolled binary search tree (was linear scan)
- Bug 20: SEARCH VARYING — varying variable now incremented in parallel with search index

**Lesson learned:** Never `git stash` while background agents are writing files. The stash
captures the agents' partial writes, and `stash pop` silently drops them if the agents
continued writing after the stash. Lost all 7 completed agents' work. Re-dispatched and
completed successfully on second pass.

**Results:** 218 unit + 200 integration tests pass (up from 216+191).

---

