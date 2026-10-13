## Entry 2001 — 2026-10-13 11:24 PDT — PB3125 filed (the branch survey cannot see a stopped landing-lane attempt); db5-2 abandoned as superseded

Filed kb/Work/PB3125. A branch whose landing-lane attempt stopped before a pull request reads DECISION in
`prune_worktrees.py`, and so does every predecessor it carries. The cause is that enqueue.py's catch-up merge and
DEVLOG stamp commits postdate the DONE report. Meanwhile `land_lane.py status` names the owner (the enqueue unit's
step 4 finisher) and `plan_wave.py` lists the branch as awaiting landing. The instance is
`worktree-wf_d63f7207-db5-30` (PB2720 + PB374): both gates GREEN, then `=== LANDING ORACLE: REFUSED ===` (exit 7) at
06:36Z. It and its ancestors db5-28 and db5-9 read DECISION.

kb/Work/PB3030 records `worktree-wf_d63f7207-db5-2` (wave 1069 B) ABANDONED as superseded. Wave 1070 A cherry-picked
all six of its commits onto `worktree-wf_787860dd-104-1`; `git range-diff` shows five equal and one that differs
only in manifest context.

The same resume unit decided the other DECISION rows outside git, in the fix-lane reports under
`E:\COBOL-coord\scratch\reports`:
- The SPLIT branches 104-1 (PB546, PB2988, PB2921, PB3030's first half) and 104-5 (PB3102) land their finished
  parts. PB3030's bit arm, PB3101 and PB2099 stay open for fresh groups.
- db5-11 (PB2842) is DONE now that PB3109 has landed.
- 104-18 (PB2077) goes to the next wave's finisher for its review finding.

