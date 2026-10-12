## Entry 1882 — 2026-10-04 22:50 PDT — The skills adopt the orchestrator loop: the workstream overlay names it the default fix lane, and the public agent-fleet reference records what live runs validated (v1.18.0)

The owner asked that the skills and the skills repo use the new orchestration approach, which "appears to better handle autonomous progress towards the
compiler". Project skill: `.claude/skills/workstream/SKILL.md` now opens with the orchestrator loop as THE DEFAULT way to run the fix lane
(`orchestrate.ps1` and its flags, the supervisor owning each unit's lifetime, frequent handoffs, `stop.ps1`, the allocator, budget and planner), the
attended session's duties while it runs (publish the ledger when the supervisor logs `LEDGER PUBLISH OWED`, read handoffs, prune, battery cadence, no
edits in the main checkout while a unit runs), a restart note (a surviving checkpoint becomes the dead unit's handoff; read a dead unit's stderr first) and the
allocator caveat found today (a lander takes the DEVLOG top plus one whatever `alloc.py devlog` reserved: entry 1880 was renumbered after train 1018b took
1879). Public skill: `agent-fleet/references/orchestrator-loop.md` was labelled "not yet validated"; it now records nine defects the real runs exposed and
their fixes, generalized (the 600-second background ceiling of a one-shot `claude -p` and the open-stdin supervisor, read stderr first, frequent handoffs, STOP as a
wind-down, a fast done unit is not a failure, pass the budget decision to the unit, plan from the disk, a headless session may lack tools, measured recovery),
with the agent-fleet SKILL.md rules, a README entry and version 1.18.0 (lint passes, leak scan clean). Not landed: the public push. The harness denies
`git push` to that repository and the GitHub connector's token cannot create a branch (403), and the owner is on a phone, so v1.18.0 is committed locally
(`3bf8efd` in `E:\claude-skills`) and waits for the owner to push it from a desktop; the submodule pin stays at 1.17.0 until that commit exists on the remote,
because a pin to a missing commit breaks CI.

**Files:** `.claude/skills/workstream/SKILL.md`. No diagnostic code used.
