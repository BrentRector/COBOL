## PB603: the run unit's implicit CLOSE is the CLOSE statement, for every open file, even when one fails

kb/Work PB603 (wave 1095, group F). ISO §14.6.11 2): *"An implicit CLOSE statement without any phrases is executed
for each file that is in the open mode. These implicit close statements shall be executed for all open files in the
run unit, even when an error occurs during the execution of one or more of the CLOSE statements."* (`cite.py --check
14.6.11` OK, rule 2.) §9.1.15 3) and §9.1.16 give an implicit CLOSE the explicit one's file-lock and record-lock
release (both checked OK).

**What the re-probe showed.** The note's premise that `CobolFile._reg` is static, so a second run unit inherits the
first one's registrations, is stale: the registry has been `RunUnit.Current.Files` for a while, and PB321 had already
made `CloseAll` deregister. Three things still reproduced on this build, in a unit probe that became
`RunUnitImplicitCloseDriftTests` (a second `FileRegistry` is a second run unit). An injected close-time
`InvalidOperationException`, delivered through the held-line drain (the report writer's hook on every close path),
escaped `CloseAll` and left the failing file and every file after it open. A closed sequential connector came out of
termination with '42', from the "sequential connectors close unconditionally" carry-over. And `CloseAll`, `Reset` and
`CloseDisplaced` each called a connector's own `Close()` beside the Table 14 dispatch. The registrar's user-level
route (an EBCDIC indexed file holding U+20AC) was closed by PB1150's '91' screen, so the failure now needs a runtime
defect to reach it. The rule still requires the loop to survive one.

**Fix shape.** `FileRegistry.CloseAll` now closes each file through `CloseIfOpen`, the CLOSE dispatch's own entry
(`CloseByFormat`, Table 14's plain CLOSE row, `SharedClose`'s deregistration and record-lock release). The implicit
CLOSE is therefore the plain CLOSE, and a file that is not in the open mode is not touched. Each close is guarded.
Every file is closed first, then the failure is rethrown, with several failures thrown as one `AggregateException`.
`FileConnector.Close` runs `CloseCore` even when the drain throws, and it ends the cross-run-unit file lock and the
open mode in a `finally`. A failure that no I-O status describes can no longer leave a lock that refuses the next
run unit's OPEN with '61'. The `'30'` family (IO, access, out-of-memory) is unchanged.

The sibling sweep found two more arms. `Reset`, at run-unit start, closed only the sequential connectors and dropped
keyed ones still open, with their records unpersisted and their file lock held. It now runs `CloseAll` and clears
the tables in a `finally`. `CloseDisplaced` now uses `CloseIfOpen`. `SharedClose` takes the connector the dispatch
already holds and absorbed `DeregisterFromPhysical`. `RunUnit.Terminate` releases static storage in a `finally`.
`OnlyTheCloseDispatch_CallsAConnectorsOwnClose`, a Roslyn drift test, keeps `CloseByFormat` the only member of
`FileRegistry` that calls a connector's `Close()`. The close-path paragraph of `docs/COBOLNET_FILES_DESIGN.md` says so.

**Tests.** Four unit tests, three of them red before the fix: the failing close, the untouched closed connector, and
the structural drift test. The fourth, the implicit CLOSE releasing another run unit's record lock, was green before,
as the note predicted, because of PB321's half. No conformance golden is added, because no conforming program can
observe the change: the note is `process_only`.

**Rows.** `GR-14.6.11-2` PARTIAL → CONFORMS. `GR-14.9.27.4-22` DIVERGES → CONFORMS. Its three recorded divergences
had been closed by PB321/PB740, PB316 and PB833. They were re-probed here: `2002/pb316_open_group_scope` and
`2002/file_sharing` print their goldens, and `RunUnitFileLockDriftTests` passes. The deleted
`FileConnector.SharedStreams` witness is retired. GAP 88 → 86.

**Self-review.** Silent failures: the guard rethrows and swallows nothing, and the closing operations live in a
`finally` rather than in a broader catch. Two-arm check: the explicit, CANCEL, object-deletion, displaced, run-unit
start and run-unit end closes all enter one dispatch. Performance: one key snapshot per termination.
