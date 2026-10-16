## PB2758: every connector's file-lock handle is measured against the HostAccess it states

Wave 1092 group G, an R2 unify finding of the PB1754 architecture review (runtime-io-3 duplication).
`FileConnector.HostAccess` is the one statement of the access a connector's long-lived handle takes of its physical
file. A sibling's §9.1.15 file lock is derived to admit it, and `FileLockPostureDriftTests` measures its band. The
finding was that the sequential handles re-derived the access themselves, so the band measured a copy that no
sequential handle read.

**Re-probe.** Two thirds of the target had already landed with PB2692 (wave 1047 B): `OpenReader` passes
`HostAccess(Mode)`, and `HostFile.OpenConnectorWriteStream` takes the access as a parameter. Two things were still
open. The drift test still asked each connector what it said it took and never looked at a handle. And a fourth site
still re-derived the handle's access from the open mode: both organizations made their region-lock endpoint with
`writable: Mode is not FileOpenMode.Input`, and the sequential writer kept a `_writerReads` flag computed from the
requested access rather than the granted one.

**Fix shape.** `FileConnector.FileLock` is a new abstract member: the host stream that IS the connector's §9.1.15
file lock while it is open, null before the OPEN body takes it, after the CLOSE, and for an absent OPTIONAL INPUT
file. Its `CanRead` and `CanWrite` report the access the host granted. `KeyedConnector` returns its store handle (the
dead `protected Store`, which had no caller, leaves the class and `PublicAPI.Shipped.txt`). `SequentialConnector`
returns the host stream beneath its reader or writer through `HostFile.FileOf`, which replaces `HostFile.HandleOf`
because the repositioning `SharedAppendStream` reports `CanRead` false over a read-write handle;
`SharedAppendStream.Host` replaces its `SafeFileHandle`. Both region-lock endpoints now take `writable` from the
handle's `CanWrite`, and `ReleaseRecord` reads `FileLock.CanRead` where `_writerReads` stood. The values are the same
on every arm: INPUT is the only mode whose handle has no write access, and the band forbids adding one. The change
is therefore behavior-neutral. Two stale comments that named a "reader rebuild", which PB322 had removed, are
corrected.

**Drift test.** `FileLockPostureDriftTests.EveryOrganisationsLiveHandleTakesItsHostAccess` covers 3 organizations ×
4 open modes × the clause-less and record-locking postures (24 cases). For each, it opens a real file and checks
three things: the granted access equals `HostAccess(mode)`, the region endpoint is writable exactly when the handle
is, and the CLOSE gives the handle back (§9.1.15 3), checked). To see it fail, an INPUT reader was opened
`FileAccess.ReadWrite`: the new test went red on both Sequential Input cases while
`EveryOrganisationsHandleAccessLiesInThatBand` stayed green, which is the scenario the finding described.
DESIGN-runtime-library.md and COBOLNET_FILES_DESIGN.md name the new member and the test. No inventory row is
affected (a behavior-neutral restructuring), and no golden is added because nothing a program can observe changed.

**Runtime ABI pin.** The first gate went red on one test,
`RuntimeAbiPinDriftTests.TheRuntimeMajor_IsPinnedBesideTheShippedSurfaceAndTheCodecImages`, because removing
`KeyedConnector.Store` changed the shipped public surface. The surface hash is re-pinned (BD194B97DB604BBC to
623DD4736ADB8680) and the major stays 5. This is the test's "re-pin alone" case: nothing that crosses a module
boundary changed. `Store` was `protected` on a class with `private protected abstract` members, so no assembly other
than the runtime could ever subclass `KeyedConnector` and reach it. No compiled module could reference it, and
raising the major would refuse every module compiled at 5 for no reason. The compare oracle reads IDENTICAL over
7999 cases, and the semgrep counts are unchanged.
