## Entry 1975 — 2026-10-12 15:54 PDT — PB2842: a coherent keyed statement whose store reload meets a host failure answers '30' instead of killing the run unit

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

Sibling sweep: the other reloads/persists (`Load`, `Persist`, `PersistIfChanged`) run at OPEN and CLOSE. OPEN already
mapped `IOException`, `UnauthorizedAccessException` and `OutOfMemoryException`; `FileConnector.Close` mapped only the
first two, so a CLOSE owing a persist (`ComposeStore`, the one large allocation) let an `OutOfMemoryException` out of
the program - this entry first claimed Close mapped it, which was wrong (review round 1, R1-2); `Close` now maps it to
'30' like OPEN. `CompleteStatement` already mapped `IOException` and `UnauthorizedAccessException` (it now shares the
predicate and gains `OutOfMemoryException`).

Review round 1 (R1-1): mapping the reload's `OutOfMemoryException` to '30' and carrying on is only safe if the
reload leaves the shared store honest, and it did not. `ReadImage` stamped `store.Generation` before decoding, and
`Fill` empties the store before refilling it, so a refusal in either left a store trusted as current (a second SELECT
of the same file in the run unit skipped the reload and answered from stale records; its next WRITE persisted them
over the other run unit's) or half-filled and owed to the file (a CLOSE wrote it over the file). `Reload` now reads
and decodes first, touching nothing of the store (`ReadImage` returns the generation instead of stamping it), stamps
the generation only after `Fill` has put the records in, and marks a store whose `Fill` failed as owing the file
nothing (`PersistedVersion = Version`); because the generation is still the old one, every later coherent statement
entry of the run unit reloads again. The refusal cannot be produced on demand, so `KeyedConnector.HostFaultObserver`
(an `AsyncLocal` test seam, the `FileRegistry.PauseObserver` precedent) throws at `ReloadDecoded`, `ReloadFilling`
(after `Fill`'s Clear, in both organizations) and `PersistComposing`.

### Witness

`KeyedStoreReloadFailureDriftTests` (Unit, 22 cases, plus 8 for review round 1: the two reload
refusals x 2 organizations, the persist refusal at the statement and at the CLOSE x 2 organizations; verified red against
the old stamping order, the missing reset and the missing `Close` catch): 2 organizations x 9 verbs (shared and plain entries of READ NEXT,
random READ, WRITE, REWRITE, DELETE, START FIRST, START key) on the process lock host, READ NEXT on the lock-file host,
and the RETRY re-entry on both organizations. Each asserts '30', the connector's status, no record delivered, the
replay on the next statement, and that the CLOSE ends the condition. Verified red before the fix, green after; the
retry cases were re-verified red by disabling the catch.

Rows: none claimed, none closed (`closes_rows_reason` on the note). Design: `docs/COBOLNET_FILES_DESIGN.md`, the coherent
statement entry bullet. Semgrep counts unchanged. No diagnostic codes used (COBOLNET3572-3574 returned).

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 5,129/11,488 - Unit 32,741/33,474 -
Characterization 36/36 cases in leg 1).

