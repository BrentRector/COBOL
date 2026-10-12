## Entry 1786 — 2026-09-29 15:32 PDT — Branch cleanup: PB1522's eleven cloud-adjudication lanes archived onto main; stale branches removed

The owner noticed 24 open branches on GitHub and asked whether all were in use. None was: nothing was running.

**Deleted from GitHub (owner-approved):**
- **Seven `worktree-agent-*` implementer branches from 2026-09-05.** Every kb/Work note they touched is landed. The
  one that touched no note was probing PB697, which is also landed. Git showed them unmerged only because landers
  apply patches rather than merging.
- **`claude/adj-registrar-1`, `adj-doc-rows-1`, `golden-lane-1` and `phase-14`:** already merged into main.

**Archived, then to be deleted:** the eleven raw cloud-adjudication lanes of PB1522.
- They held the only copy of the 208 original verdicts, pinned at `0caa7d5`, of which 71 were NEEDS-OWNER-DECISION.
- Main's inventory has since re-adjudicated most of those rows, so its verdicts differ, but the originals are
  evidence.
- Each branch added only its own `adjudication/<lane>/` directory, and none existed on main. They are copied
  unchanged (79 files) beside the lanes already archived there (`registrar-1/`, `doc-rows-1/`, `doc-rows-2/`,
  `golden-lane-1/`).
- PB1522 points to the paths. The citation audits are clean over the archive (one probe is a `.cs` harness, outside
  every project).
- The branches are deleted once this commit is on main.

**Local cleanup:** 32 worktrees and 171 branches removed, each proven landed first: the head is on main, or every
note the branch touched is landed. 17 worktrees and 42 branches remain, each touching an open note or no note:
- wave 73's split branches E, F, B2 and D2, whose STATUS.md holds the next-step designs for PB480, PB1042,
  PB322/PB833 and PB1422;
- two preserved wave-68 WIP worktrees;
- branches touching open notes such as PB1466, PB661 and PB370.

