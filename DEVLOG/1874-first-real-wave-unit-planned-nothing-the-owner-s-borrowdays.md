## Entry 1874 — 2026-10-04 16:40 PDT — First real `wave` unit planned nothing: the owner's `-BorrowDays` never reached the unit's own plan call

`orchestrate.ps1 -MaxUnits 1 -Unit wave -BorrowDays 1` ran Opus for 42 s (10 calls, $0.57), loaded the workstream skill, called
`plan_wave.py --from-budget`, and handed off `done` with "planned 0 groups (0.00 of 0.00 weekly points)": the unit's call defaulted
`--borrow-days` to 0, so its own `budget.py` reading said `hold-day` (16.15 % against 14.3 %) while the supervisor's gate, run with
the owner's borrowed day, had said `go`. Two readings of one allowance disagreed because the allowance was an argument of the
supervisor only. The supervisor now substitutes `{BORROW_DAYS}` into the unit prompt exactly as it does `{SCRATCH}`, and
`units/wave.md` passes `--borrow-days {BORROW_DAYS}`; `test_orchestrate.ps1` checks the substitution (41/41) and the design doc
lists the placeholder. The unit behaved correctly given what it saw: it dispatched nothing, allocated nothing and said why.

**Files:** `scripts/orchestrator/orchestrate.ps1`, `scripts/orchestrator/units/wave.md`, `scripts/orchestrator/test_orchestrate.ps1`,
`docs/rearchitecture/DESIGN-orchestrator-loop.md`. No diagnostic code used.

