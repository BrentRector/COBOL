## PB2099 part 1: the host's outermost activation boundary and R48's run-unit-ended result

Wave 1077 group E took kb/Work PB2099, slice 3 of DESIGN-external-repository: the typed host entry and the one host
surface. Two earlier waves had left it unstarted. The re-probe at a6744fa4d found none of the slice in the tree:
`ProgramTable.CallProgram` and `Cancel` took a non-null `callerPath`, `RunUnitEndedException`, `HostSurface`,
`HostLanding` and `HostEntryEmitter` did not exist, and `RoslynBackend` always emitted a console application. The
slice is five mechanisms. This branch lands the first one, the runtime boundary, and stops at that boundary for a
finisher (SPLIT).

**The mechanism.** A .NET host is an activator outside every program. `CallProgram(name, callerPath: null, ...)` now
means exactly that. A CALL written in a METHOD keeps `""`: it is contained in no program, but it runs inside the run
unit. The null arm, `ProgramTable.CallFromHost`, is the run unit's outermost boundary, the host's counterpart of
`RunMain`. The activation itself is still the one §14.9.4.4 GR3 boundary every COBOL CALL crosses (`Activate`). What
the arm adds is the ending. A STOP RUN performs "The operations described in 14.6.11, Normal run unit termination"
and then "Execution of the run unit terminates and control is transferred to the operating system" (§14.9.42.4 1) and
6)). A run-unit fatal terminates the run unit abnormally (§14.6.12). In both cases `RunUnit.EndForHost` performs the
§14.6.11 actions and replaces the host's ambient run unit with a new one, and the host receives
`RunUnitEndedException(ExitStatus, Abnormal)`, the one documented type owner decision R48 requires (DOC-A.1-167). A
`CobolCallException` still leaves as itself, which is item 2 of the design's exception contract. The arm recognizes
the ending with an exception filter and runs the epilogue after the try statement, never inside a catch block. A
catch block runs on top of the stack the condition was thrown from (kb/Work PB2659), and a host thread has only the
.NET stack margin.

**One list.** `ProgramTable.EndsRunUnitAbnormally` (`CobolFatalException`, which includes `CobolSizeError`, and
`CobolImplementorFatalException`) is now the one list of run-unit fatals. The COBOL main's boundary (`RunOnThisThread`)
and the host's boundary both read it. Before this, the main boundary spelled the list out as three catch clauses.
`RunUnit.Run` also ends a replacement run unit that its body's activations left behind.

**Witness.** `conformance-test:HostRunUnitEndingTests` is a real C# host process. It calls a counter program twice
(COUNT 1, COUNT 2), then a program that leaves a file open and runs STOP RUN WITH ERROR STATUS 7. The host gets
`ENDED STATUS=7 ABNORMAL=False`, and the file holds HELLO, so the implicit CLOSE ran. The counter then prints COUNT 1,
so the next activation ran in a new run unit. A NULL BASED reference gives `ENDED STATUS=1 ABNORMAL=True` with the
EC-DATA-PTR-NULL reason. The same sequence also runs inside `RunUnit.Run`. The host exits 0 with nothing on stderr.
With the arm disabled, the transcript stops after IN STOP SUB because `StopRun` escapes into the host. The three
existing test hosts (NonCobolActivatorReturn, NonCobolActivatorLinkage, DefaultLocaleDetermination) now pass the
host's `null`.

**Surface.** The four `CallProgram`/`Cancel` lines in `PublicAPI.Shipped.txt` change from `string!` to `string?`.
That is a nullable-annotation relaxation: the metadata signature and every existing caller stay the same, so
`RuntimeAbiPinDriftTests` is re-pinned without a major bump. `RunUnitEndedException` goes into Unshipped. The docs
changed in the same commit: DOC-A.1-167 names the type, and its "Diverges today" now covers only the ambient run
unit's files at process exit (PB1253). INTERPROGRAM gains the host-activator paragraph, and
DESIGN-external-repository §21 item 3 records what is in the tree and one question to settle before the emitter:
§4.3's `HostSurface.TypeOf` takes `DescriptionRecord`, a §6.1 type that slice 4 introduces.

**Not done (the finisher's):** `HostSurface`/`HostLanding`, the published `RecordStructEmitter` mode, `HostEntryEmitter`,
`OutputKind` library, the host harness, the §17.3 drift tests, and the retirement of the untyped host route.
