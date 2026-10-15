## A run unit no longer writes its host process's exit code (PB3163)

kb/Work PB3163 reported that a run unit writes the HOST process's exit code. Re-probe on a Debug build: a Roslyn-compiled
C# host that set `Environment.ExitCode = 5` and ran `STOP RUN WITH ERROR STATUS 24` through `ProgramRegistry.RunMain` read
`process=24`, and the process ended with 24. Three writers shared the root, one of them not named by the note: the
`RunUnit.ExitStatus` setter (flushed at the write site), `RunUnit.StartAfter` (every `Begin` and `Run` restated the cell to 0)
and `ProgramTable.AbnormalTermination` (a fatal condition wrote 1).

Root cause: the run unit's termination status was modelled as the process exit code instead of as the run unit's own state.
DOC-A.1-167 (owner decision R48) says STOP RUN and a fatal condition end the run unit only, and a standalone `cobol`
executable is its own host. Fix shape: `RunUnit.ExitStatus` is plain run-unit state (the status still crosses assembly
boundaries because every module writes the one shared field); `AbnormalTermination` sets it to 1; the one boundary that IS
the process, `ProgramTable.RunModule` (the generated `Main`'s single statement), delivers it to `Environment.ExitCode` once in
a `finally` (so STOP RUN's unwind is covered), clamped by `CobolNum.Position32` as PB1178 requires. `RunMain` and `RunUnit.Run`
write nothing; a host reads `RunUnit.ExitStatus`. The generated `Main` is unchanged (scaffolding-free).

Witness: `RunUnitHostExitCodeTests` (a host that sets its own code, then runs a STOP RUN WITH ERROR STATUS 24 run unit, a
fatal CALL of an absent program and a status-free one; expects the process code 5 throughout, run-unit statuses 24, 1, 0 and
a process exit of 5). `RunUnitStateDriftTests.EveryMember_IsFreshInTheNextRunUnit` now asserts the host's code is untouched
instead of the old restated 0. `StopGobackExitCodeTests` (standalone and cross-assembly status) stay green. DESIGN (three
sites) and CONFORMANCE §4.2.16 describe the delivery boundary. No inventory row closes. A sibling host-process mutation of
another kind (`StandardStreams.EnsureUtf8` replaces Console.Out/Error on every `RunMain`) is a lead, not part of this change.
