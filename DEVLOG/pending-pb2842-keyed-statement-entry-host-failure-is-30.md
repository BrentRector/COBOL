## PB2842: a coherent keyed statement whose store reload meets a host failure answers '30' instead of killing the run unit

### The defect

A relative or indexed connector opened `SHARING WITH ALL OTHER` reloads its store at the start of a statement when
another run unit moved the file's generation stamp (`KeyedConnector.BeginStatement` -> `Enter`). `ReadImage` refuses a
file longer than `RecordFraming.MaxStoreBytes` with an `IOException`; OPEN maps that to '30' (ISO §9.1.13.6 1),
"A permanent error exists and no further information is available"), but the in-statement reload had no catch, so the
exception left the READ, WRITE, REWRITE, DELETE or START and ended the run unit. The RETRY wait's re-entry
(`WaitOutsideStatement`, which re-enters the mutex in a `finally`) had the same hole.

### What the re-probe showed

A unit test with two `FileRegistry` instances (two run units) reproduces it: the second run unit writes (the
generation moves), the test extends the file past `MaxStoreBytes` through a third handle (sparse, header intact), and
the first connector's next statement threw `IOException: ... longer than any store this format describes`. All nine
verb entries on both organizations threw, as did the RETRY re-entry.

### The fix

`KeyedConnector.Enter` catches a host failure (`IsHostStoreFailure`: `IOException`, `UnauthorizedAccessException`,
`OutOfMemoryException`, the set OPEN maps, and the one predicate `CompleteStatement`'s persist now shares) and sets
I-O status '30'. The Status setter starts the §9.1.13.1 permanent error ("remains in effect for all subsequent
input-output operations on the file"). The statement stays entered holding what it took, so depth and mutex unwind
through the ordinary `Dispose`, and every verb's first line (`FileConnector.PermanentErrorReplay`) ends the statement
before the stale store is read; READ and START invalidate the file position indicator as for any permanent error. So
no new refusal protocol is threaded through the 16 `BeginStatement` call sites: the permanent-error replay that every
verb already makes is the mechanism. A failure that is not the host's (anything else) still undoes the entry and
escapes.

Sibling sweep: the other reloads/persists (`Load`, `Persist`, `PersistIfChanged`) run at OPEN and CLOSE, which
`FileConnector.Open`/`Close` already map; `CompleteStatement` already mapped `IOException` and
`UnauthorizedAccessException` (it now shares the predicate and gains `OutOfMemoryException`, the one large allocation
`ComposeStore` makes).

### Witness

`KeyedStoreReloadFailureDriftTests` (Unit, 22 cases): 2 organizations x 9 verbs (shared and plain entries of READ NEXT,
random READ, WRITE, REWRITE, DELETE, START FIRST, START key) on the process lock host, READ NEXT on the lock-file host,
and the RETRY re-entry on both organizations. Each asserts '30', the connector's status, no record delivered, the
replay on the next statement, and that the CLOSE ends the condition. Verified red before the fix, green after; the
retry cases were re-verified red by disabling the catch.

Rows: none claimed, none closed (`closes_rows_reason` on the note). Design: `docs/COBOLNET_FILES_DESIGN.md`, the coherent
statement entry bullet. Semgrep counts unchanged. No diagnostic codes used (COBOLNET3572-3574 returned).

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 5,129/11,488 - Unit 32,741/33,474 -
Characterization 36/36 cases in leg 1).
