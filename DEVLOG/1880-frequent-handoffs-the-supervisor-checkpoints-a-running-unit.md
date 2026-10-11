## Entry 1880 — 2026-10-04 21:05 PDT — Frequent handoffs: the supervisor checkpoints a running unit and writes the handoff a dead unit never did (PB2015)

The owner, two hours into the third `wave` unit: "In the future, we need more frequent handoffs written." Until now a unit's
handoff was written once, by its model, at the very end, so wave 1017's death at minute 11 left nothing and a death at minute 100 would
have lost proportionally more; a supervisor that died mid-unit (a reboot) left no record at all. Three mechanisms, ordered by how little
they trust the model (design doc section 5.1, `scripts/orchestrator/checkpoint.py`). (1) `checkpoint.json`, written by the supervisor
with no model: at unit start, every `-CheckpointSeconds` (300), when the background-task set changes (a Workflow starts, a train lands)
and at the end; it holds the stream counters and every linked worktree's head, commits ahead of `origin/main`, uncommitted-file count
and `STATUS.md` headline. (2) `milestones.jsonl`, one line the unit's model appends per milestone, for what the supervisor cannot see
(a decision and its reason). (3) A synthesized handoff (`outcome: split`, `next_unit: resume`, `synthesized: true`) whenever a unit ends
without a valid one; a checkpoint that survives into the next supervisor start becomes the missing handoff first. The unit still
counts as failed for the breaker and `units.jsonl` says `synthesized (missing)`. `test_checkpoint.py` uses real linked worktrees and
checks the synthesized handoff against the schema's required and permitted keys; `test_orchestrate.ps1` is 69/69 (a crash with a
milestone, the periodic and final checkpoint, a supervisor that died mid-unit). Built in its own worktree, because the running wave's
main checkout had to stay clean; landed after the wave ended.

**Files:** `scripts/orchestrator/checkpoint.py`, `test_checkpoint.py`, `orchestrate.ps1`, `handoff.schema.json`, `units/common.md`,
`testdata/fake-claude.ps1`, `test_orchestrate.ps1`, `docs/rearchitecture/DESIGN-orchestrator-loop.md`, `kb/Work/PB2015.md`.
No diagnostic code used.
