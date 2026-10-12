## Entry 1430 — 2026-09-02 17:08 PDT — The workstream doctrine: a session-limit kill costs one step, a restart repeats nothing — checkpoint to disk, fresh agents from checkpoints, a concurrency budget, finished work landed first

Owner standing instruction, twice in one day. First: "Run all this work so that we don't repeat effort when hitting a
limit and resuming … you've been running only a few minutes and used 20% of the session limit." Then: "Record standing
instructions to minimize token usage when running workstreams, such that they can restart if necessary without this
same massive token/session usage problem." This entry records the doctrine and where it lives; the measurements that
earned it are the day's own.

What was measured. Twenty-eight concurrent Opus agents burned ~20% of a session window in eleven minutes. Roughly fifty
had exhausted the previous window in ~2.5 hours; two cutoffs fell in one day (resets 12:20am and 4:10pm PT — the hour
is not fixed, it is read off the 429). The expensive shape was not the fleet width alone: a RESUMED agent re-reads its
whole transcript on every turn, so resuming eight 300-turn implementers at once was the costliest action of the day,
and fifteen refuters that wrote nothing to disk until they finished lost every rule they had decided when the window
closed. Meanwhile three FINISHED clusters sat un-landed behind a single serialized lander.

The doctrine, now in three places — the `workstream` skill (`.claude/skills/workstream/SKILL.md`, the operational SSOT,
with the day's proven briefs and workflow scripts as parameterized templates), one bullet in `PROMPT.md` §5, and the
orchestrator's memory:
1. Checkpoint to disk, never to a transcript. Implementers commit WIP on their worktree branch and keep `STATUS.md`
   (DONE / NEXT / BLOCKED / GATE / batch path / codes) after every mechanism and every gate; workflow stages append one
   JSON line per rule to a checkpoint file and read-and-skip it on start; landers commit per step; briefs and reports
   are files referenced by path.
2. A killed agent is replaced by a FRESH agent that reads the checkpoint — resume a transcript only within a step of
   its finish.
3. Concurrency budget: one lander + at most three implementers + one four-wide read-only chunk. A fleet over ~40 or an
   implementer over ~100 turns is the signal to split the work.
4. Land finished work first; one lander on main at a time; landers push `origin HEAD:main` as a fast-forward (a
   worktree agent cannot touch the shared checkout) and the orchestrator fast-forwards locally; no lander carries more
   than about three landings.
5. Ids are allocated centrally — five `kb/Work` id collisions in one day each cost a renumbering pass at landing.
6. Read-only fleets probe a pinned worktree with its own built compiler; adjudication stages write `out-<slug>.json`
   so a rewritten script never re-runs finished work.
7. On restart: the reset time, `git status` on main, dirt and `STATUS.md` per worktree, then fresh agents in landing
   order.

Applied today before it was written down: the batch-1 refute stage was stopped mid-flight and re-launched four at a
time from the fourteen adjudications already on disk; every running implementer was told to commit WIP and write
`STATUS.md`; the lane-1 validator was rewritten to checkpoint per draft; the landing queue was split into landers of at
most three. `DESIGN-spec-conformance-review.md` §5.3 points at the skill so the fleet-sizing paragraph and the doctrine
cannot drift apart.

