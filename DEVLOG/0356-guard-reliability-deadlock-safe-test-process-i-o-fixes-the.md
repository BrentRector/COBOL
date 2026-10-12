## Entry 356 — Guard-reliability: deadlock-safe test process I/O (fixes the recurring file-I/O parallel-load flake)

Two transient guard failures this session (FileIO_Start DEVLOG 352, ReadPrevious_AfterStartEqual DEVLOG 355) —
each passing in isolation but failing once under the full guard's parallel load. Root cause: `EndToEndTestBase`'s
`CompileAndRun` / `CompileMultipleAndRun` ran the compiled program with synchronous
`process.StandardOutput.ReadToEnd()` + `StandardError.ReadToEnd()` + `WaitForExit(10000)`, then read
`process.ExitCode`. That pattern is deadlock-prone (a process filling one pipe while the parent blocks reading the
other) and, when a slow-under-load process exceeds the 10s wait, accessing `ExitCode` on a still-running process
throws — surfacing as a flaky failure.

Fix: both methods now use the async-read + bounded-wait + kill-on-timeout pattern that `CompileNistAndRun` already
used — `ReadToEndAsync()` for both streams, `WaitForExit(30000)`, and on timeout Kill + return a clear "timed out"
result instead of throwing. Normal (fast-exit) tests are unaffected (the wait returns immediately, the read tasks
are already complete). The 30s ceiling keeps a heavily-loaded-but-progressing process from being mistaken for a
hang.

Guard ALL GREEN: **1047 unit / 433 integration / 364 NIST**, 0 regressions.

