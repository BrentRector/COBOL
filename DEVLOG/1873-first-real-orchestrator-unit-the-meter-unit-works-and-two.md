## Entry 1873 — 2026-10-04 16:12 PDT — First real orchestrator unit: the `meter` unit works, and two defects it exposed are fixed

`orchestrate.ps1 -MaxUnits 1 -Unit meter -BorrowDays 1` ran a real `claude -p` on Sonnet with Chrome: 39 s, 9 calls, $0.42, a
schema-valid handoff, and a correct reading (weekly 16 %, session 9 %, reset 18:30 PDT) appended to `readings.json`. The prototype
had two defects that its fake-claude self-test could not see. (1) `E:\COBOL-coord\readings.json` held the six readings of the
earlier session with display-string resets (`6:30 PM PDT`), written by hand around `budget.py --record`, which refuses anything but
an ISO instant, so every `budget.py` call, and the whole `-DryRun`, died on `fromisoformat`. The data was rewritten as ISO
instants (the reader stays strict: the one writer validates, so a bad file is a defect to see, not to guess around). (2) The
circuit breaker counted any unit under `-FastFailSeconds` (120) as failed, so a legitimate 39 s `meter` unit scored "FAILURE 1 of 3"
despite a valid `done` handoff; three meter readings in a row would have stopped the loop. The rule now fails a fast unit only
when it did not hand off `done` (a start-up crash still trips it); `test_orchestrate.ps1` gained the case (40/40) and the design
doc states the rule. Also confirmed: the `-Unit` override, the budget gate (`-BorrowDays 1` moves `hold-day` to `go` against the
owner's 28.6 % allowance for today) and the planner dry run (wave 1016: 8 groups, 2 trains, 2.42 of 8 points).

**Files:** `scripts/orchestrator/orchestrate.ps1`, `scripts/orchestrator/test_orchestrate.ps1`,
`docs/rearchitecture/DESIGN-orchestrator-loop.md`. No diagnostic code used.

