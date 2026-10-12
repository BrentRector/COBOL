## Entry 1881 — 2026-10-04 22:30 PDT — The ledger is published after every landing: version 93 is live, a publish owed is detected and announced, and the dead stale-ledger rule is gone (PB2016)

The owner: "Publish ledger each time", and "Prune as appropriate". Version 93 of the owner's artifact is published (GAP 255, measured tree
`5d77035ac`), after the narrative in `ledger-in-flight.md` was rewritten (it still said "No wave is running" from the morning, with GAP 281)
and the trend series gained its wave-1018 points. Pruned: `prune_worktrees.py --apply` removed ten LANDED and MERGED branches and their
worktrees and archived their tips in `E:\Temp\pruned-branches-20261004-222300.bundle`; it skipped five touched in the last three hours or dirty.
Making "each time" real hit a limit: a headless unit has no `Artifact` tool (a Haiku probe answers NO), so the unit cannot publish. The split follows
the capability: units render `{COORD}\ledger.html`; `ledger_state.py owed` compares the page's own stamp (`gen_ledger.STAMP_PATHS`, imported) with the
last published one; the supervisor prints `LEDGER PUBLISH OWED` after every unit and `stop.ps1 -Status` shows it; the attended session publishes and runs
`ledger_state.py mark-published`. While there I found `next_unit.py` rule 7 comparing `docs/rearchitecture/evidence/conformance-ledger.html`, a file no
generator writes, so every idle loop chose `land` and every dry run said "would run unit 'land'"; a `land` unit could not have cured it anyway. The rule is
deleted. `test_ledger_state.py` and `test_orchestrate.ps1` 71/71 (an owed publish is announced, a marked one is not). In a fully unattended run the publish
waits for the next attended session; the log line and the status command are how it is seen.

**Files:** `scripts/orchestrator/ledger_state.py`, `test_ledger_state.py`, `next_unit.py`, `orchestrate.ps1`, `stop.ps1`, `test_orchestrate.ps1`, `units/wave.md`,
`units/land.md`, `docs/rearchitecture/DESIGN-orchestrator-loop.md` (section 14), `.claude/skills/workstream/references/landing.md`,
`docs/rearchitecture/evidence/ledger-in-flight.md`, `ledger-trend.json`, `kb/Work/PB2016.md`. No diagnostic code used.

