// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>The ACCESS MODE of a keyed file connector (ISO/IEC 1989:2023 §12.4.5.5, ACCESS MODE clause). The
/// ordinals mirror the compiler's <c>FileAccessMode</c> enum — registration passes the raw int.</summary>
public enum KeyedAccess
{
    /// <summary>ACCESS SEQUENTIAL — records in ascending RRN / key-of-reference order (§12.4.5.5.3 GR2).</summary>
    Sequential = 0,
    /// <summary>ACCESS RANDOM — records selected by key value (§9.1.8.3).</summary>
    Random = 1,
    /// <summary>ACCESS DYNAMIC — both, chosen per statement form (§9.1.8.4).</summary>
    Dynamic = 2,
}

/// <summary>
/// The shared base of the KEYED connectors — RELATIVE (§9.1.7.3) and INDEXED (§9.1.7.4) — carrying the one fact
/// every keyed verb branches on: the connector's ACCESS MODE. A sequential-organization connector has no access
/// mode to carry (§12.4.5.5.2 SR2 — <i>"The DYNAMIC and RANDOM phrases shall not be specified for a
/// sequential file"</i>), which is why this sits below <see cref="FileConnector"/> rather than on it.
/// That premise is now ENFORCED rather than assumed: until kb/Work PB692 the rule had no check anywhere and
/// <c>ORGANIZATION IS SEQUENTIAL ACCESS MODE IS RANDOM</c> compiled and ran, so this class's reason for
/// existing was a comment about source the compiler accepted. <c>DataBinder.BindFileControl</c> screens it on
/// the file control entry (COBOLNET1858, every edition), which is what makes "a sequential-organization
/// connector has no access mode" a fact about every program that reaches the runtime.
///
/// ⛔ INVARIANT (kb/Work PB325) — <b>the OPEN MODE never SELECTS a keyed verb's branch; it is only ever
/// SCREENED.</b> Every branch the standard draws inside a keyed verb is drawn on the ACCESS MODE:
/// §14.9.51.4 GR29 a)/b) (relative WRITE: consecutive release vs. the staged RELATIVE KEY), GR38/GR39 (indexed
/// WRITE: the ascending-prime-key requirement vs. "in any order"), §14.9.35.4 GR5 vs. GR21/GR22/GR23 and
/// §14.9.10.4 GR2 vs. GR3/GR4 (REWRITE/DELETE target: the last-read record vs. the key item's). The OPEN MODE
/// enters only as the permission test that follows — §14.9.27.4 GR8's Table 20, whose unsuccessful cells
/// §9.1.13.7 items 8 and 9 name ('48' for WRITE, '49' for REWRITE/DELETE) — and item 8's own two arms are
/// themselves selected BY the access mode: a) sequential ⇒ extend or output, b) random or dynamic ⇒ I-O or
/// output.
///
/// Folding the open mode into a branch predicate ("sequential access <i>or</i> extend mode") therefore inverts
/// the dependency and makes the runtime's answer depend on a bind-time screen holding: §14.9.27.3 SR2 confines
/// EXTEND to sequential access, so a random- or dynamic-access connector open in the extend mode is a state the
/// SOURCE cannot legally reach — but the runtime must still answer '48' for it (Table 20 leaves the
/// Random/Extend and Dynamic/Extend WRITE cells blank), not divert into the sequential-release branch and
/// succeed. That divergence was PB325.
/// </summary>
public abstract class KeyedConnector : FileConnector
{
    protected KeyedConnector(string hostPath, int recordWidth, KeyedAccess access, int varyMin, int varyMax)
        : base(hostPath, recordWidth, varyMin, varyMax) => Access = access;

    /// <summary>The connector's ACCESS MODE (§12.4.5.5) — the sole discriminator of every keyed verb's branch;
    /// see the type remarks for why the open mode is never part of one.</summary>
    protected KeyedAccess Access { get; }

    /// <summary>Whether this run unit validates the INDEXED key table (<see cref="FileRegistry.KeyCheckVariable"/>
    /// — <c>COBOLNET_KEYCHECK=OFF</c> turns it off). Handed down by the registry at registration, exactly as
    /// <see cref="FileConnector.SharedStores"/> is, so a connector never has to ask an ambient run unit which
    /// registry it belongs to. A standalone connector (no registry) validates, the safe default.</summary>
    internal bool KeyCheck { get; set; } = true;

    /// <inheritdoc/>
    /// <remarks>⛔ THE KEYED ORGANIZATIONS' §14.9.27.4 GR10 ANSWER: the framed store's HEADER is the physical
    /// file's §9.1.6 fixed file attributes, so the comparison is that header against
    /// <see cref="FileConnector.DeclaredAttributes"/> (<see cref="FixedFileAttributes.Conflicts"/> — the one
    /// place the comparison is written). RELATIVE and INDEXED share this method because they share the format;
    /// what differs between them is only what <see cref="FileConnector.DeclaredKeys"/> supplies, which is where
    /// that difference belongs.
    /// <para>⛔ A STORE WITH NO READABLE HEADER IS A CONFLICT, NOT "attributes not recorded". Its layout is
    /// unknown, so its frames cannot be located either, and reading it anyway is the silent misread this rule
    /// exists to prevent — a plain sequential file opened through a RELATIVE description delivers rubbish with
    /// status '00' if nothing refuses it. A ZERO-BYTE file is the one exception
    /// (<see cref="StoreFormat.Empty"/>): it holds no records, so it can misread nothing and states no attribute
    /// to contradict.</para>
    /// <para>The header read is ONE bounded auxiliary open, and it replaces the catalog sidecar's open
    /// one-for-one — the file count went down by one and the open count did not go up (kb/Work PB802). It cannot
    /// ride the connector's own store load, because GR25 ("If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected") puts this check BEFORE <c>OpenCore</c>, whose creation arms
    /// truncate.</para>
    /// <para>⛔ A HOST REFUSAL OF THAT READ CANNOT SILENTLY SKIP THE CHECK, and the reason is structural rather
    /// than lucky: <see cref="RecordFraming.ReadHeader"/> answers <see cref="StoreFormat.Empty"/> (no conflict)
    /// on an I/O failure, but it asks for the LEAST a handle can — <c>HostFile.OpenAuxiliary</c>, read access,
    /// share <see cref="FileShare.ReadWrite"/> — and the connector's own store handle (<see cref="TakeFileLock"/>)
    /// asks for at least that a few statements later inside <c>OpenCore</c>, where the failure PROPAGATES to <c>FileConnector.Open</c>'s catch and
    /// becomes '37' or '30'. So any state that refuses the header read refuses the store load too, and the OPEN
    /// is unsuccessful either way; the swallow can lose a '39' only in favour of another unsuccessful status,
    /// never in favour of a successful OPEN over a store this connector could not interpret. Answering the
    /// authority statuses here instead would be a SECOND place §9.1.13.6 item 1's '30' is decided, which
    /// <c>FileConnector.Open</c>'s catch is the one place for.</para></remarks>
    protected override bool FixedAttributeConflict() =>
        RecordFraming.ReadHeader(HostPath, out var recorded) switch
        {
            StoreFormat.Empty => false,
            StoreFormat.Described => recorded!.Conflicts(DeclaredAttributes, KeyCheck),
            _ => true,   // Foreign — not a store this build can interpret
        };

    // ── The ISO §9.1.15 FILE LOCK, for the organizations whose store is a whole-file rewrite ────────────────

    /// <summary>⛔ THE HANDLE THAT IS THIS CONNECTOR'S §9.1.15 FILE LOCK — held for the life of the OPEN, and
    /// the ONE stream the store is loaded from and persisted through (kb/Work PB771).
    /// <para><i>"The successful opening of a file establishes a file lock for the applicable sharing rules,
    /// thereby preventing other run units from opening that file with incompatible sharing rules"</i>
    /// (§9.1.15 3)) — and a host share mode is the only thing a .NET process can say to another run unit, which
    /// means a connector that holds no handle says nothing. RELATIVE and INDEXED held none: their store was
    /// loaded at the OPEN and rewritten at the CLOSE through short-lived <c>HostFile.OpenAuxiliary</c> handles
    /// fixed at <see cref="FileShare.ReadWrite"/>, so <c>SHARING WITH NO OTHER</c> — <i>"exclusive access to a
    /// physical file"</i>, §9.1.15 1) — protected an indexed or relative file from nothing outside the run unit,
    /// and the CLOSE then truncated the file and rewrote it from the OPEN's snapshot, discarding another run
    /// unit's records with '00' reported on both sides. Measured, on both organizations.</para>
    /// <para>⛔ AND IT HAS TO BE THE SAME HANDLE THE STORE TRAVELS THROUGH. A second handle taken for the load
    /// or the persist would ask the operating environment for access this connector's own
    /// <see cref="FileShare.None"/> forbids, and the OPEN the standard ALLOWED would fail — kb/Work PB713's
    /// defect, re-opened by its own cure. That is why the load and the persist (<see cref="Load"/>,
    /// <see cref="Persist"/>) read and write positionally through THIS handle and nothing else.</para>
    /// <para>⛔ WHY <see cref="FileMode.Open"/> AND NEVER <c>Create</c>, even for <c>OPEN OUTPUT</c>: the handle
    /// outlives every persist, and <see cref="Persist"/> truncates as part of the rewrite. A creating MODE on the
    /// handle would make the truncation a property of when the handle happened to be taken.</para></summary>
    private FileStream? _store;

    /// <summary>The connector's own handle on the physical file while it is open — null before the OPEN body
    /// takes it, for an OPTIONAL file that is not present (there is no physical file to lock), and after the
    /// CLOSE. The organizations' load and persist go through it and nowhere else.</summary>
    protected FileStream? Store => _store;

    /// <inheritdoc/>
    /// <remarks>⛔ THE KEYED STORE IS REWRITTEN WHOLE, so every writable mode READS the physical file before it
    /// writes it — <c>OPEN EXTEND</c> included, where <see cref="FileLockPosture.AccessOf"/>'s mode-derived
    /// floor says <see cref="FileAccess.Write"/> alone. §14.9.51.4 GR29 a) is why the load is not optional for
    /// an extend: the release number is <i>"one greater than the highest relative record number existing in the
    /// physical file"</i>, which is a fact of the records already there. Stating it here is what lets the
    /// registry widen a SIBLING's file lock by the access this handle really takes (kb/Work PB771).</remarks>
    internal override FileAccess HostAccess(FileOpenMode mode) =>
        mode is FileOpenMode.Input ? FileAccess.Read : FileAccess.ReadWrite;

    /// <summary>Take the file lock: the connector's own handle on the physical file, carrying
    /// <see cref="FileConnector.HostShare"/> — the posture the registry derived and handed down immediately
    /// before this body ran. Called by each organization's <c>OpenCore</c> on every arm that has a physical file
    /// to hold, and by that arm ONLY: an arm with no file (an absent OPTIONAL one on INPUT) locks nothing.
    /// <para><paramref name="create"/> is §14.9.27.4 GR17/GR18's creation — the OUTPUT arm and the absent
    /// OPTIONAL I-O/EXTEND arms — and is the only difference between the arms, because the store's own
    /// truncation is <see cref="Persist"/>'s.</para>
    /// <para>The handle's buffer is this connector's input-output areas (ISO §12.4.5.14.3 GR1;
    /// <see cref="HostFile.OpenConnectorStore"/>, kb/Work PB643).</para></summary>
    /// <returns>The handle, so the arm that took it writes its store through that value rather than through a
    /// nullable field it has to re-assert.</returns>
    protected FileStream TakeFileLock(bool create)
    {
        var taken = HostFile.OpenConnectorStore(HostPath, create ? FileMode.OpenOrCreate : FileMode.Open,
            HostAccess(Mode), HostShare, InputOutputAreas);
        _store?.Dispose();   // no arm takes it twice; belt-and-braces so a future one cannot leak
        _store = taken;
        // Taken ONCE: the store is read and written positionally through it (never through the stream's buffer,
        // kb/Work PB2660), and it is the handle the store mutex and this connector's record locks are held by.
        _handle = taken.SafeFileHandle;
        _coherent = FileLockPosture.AdmitsAnotherWriter(HostShare);
        return taken;
    }

    /// <summary>Give the file lock back — §9.1.15, <i>"The file lock is removed by an explicit or implicit CLOSE
    /// statement executed for that file connector"</i>. Runs from each organization's <c>CloseCore</c> finally
    /// (whatever the persist did) and from <see cref="AbandonOpen"/>.</summary>
    protected void ReleaseFileLock()
    {
        // The field is cleared whatever the disposal throws (a final flush refused on an exhausted medium — the
        // disposal still closes the host handle), so a later CLOSE or re-OPEN never meets a disposed handle.
        try { _store?.Dispose(); }
        finally { _store = null; _handle = null; _coherent = false; }
    }

    /// <inheritdoc/>
    protected override void AbandonOpen() => ReleaseFileLock();

    // ── The externally-defined boundary of a relative or indexed file (ISO §9.1.13.5 item 4) ───────────────

    /// <summary>The bytes of this connector's store header — measured once (the declared attributes are fixed for
    /// the connector's life), and -1 until first asked.</summary>
    private long _storeHeaderBytes = -1;

    /// <summary>⛔ THE ONE BOUNDARY TEST OF A RELATIVE OR INDEXED FILE (kb/Work PB1192): would a store whose frames
    /// occupy <paramref name="framedBytes"/> bytes still fit the format's capacity,
    /// <see cref="RecordFraming.MaxStoreBytes"/>? False is §14.9.51.4 GR33 b) / GR42 d)'s <i>"outside the
    /// externally defined boundaries"</i> — I-O status '24', the invalid key condition — decided HERE, at the
    /// WRITE or REWRITE, before anything is released: the store lives in memory while the file is open and is
    /// written whole at CLOSE, so a record it cannot hold used to be reported '00' and then fail at the CLOSE
    /// (or, for a relative key in the billions, kill the run unit there). Annex A.1 item 107 is the
    /// determination (docs/CONFORMANCE.md <c>DOC-A.1-107</c>).</summary>
    protected bool StoreHolds(long framedBytes)
    {
        if (_storeHeaderBytes < 0) _storeHeaderBytes = RecordFraming.HeaderBytes(DeclaredAttributes);
        return _storeHeaderBytes + framedBytes <= RecordFraming.MaxStoreBytes;
    }

    /// <summary>Whether a CLOSE that owes a persist still holds the handle it persists through. Every writable
    /// arm of every keyed OPEN takes it (<see cref="TakeFileLock"/>), so false is an invariant breach, and
    /// §9.1.13.6 item 1's '30' is the answer: the records this connector holds cannot be written, and reporting
    /// a successful CLOSE over them would be the silent loss kb/Work PB771 exists to remove.
    /// </summary>
    protected bool PersistIsReachable(bool owed) => !owed || _store is not null;

    // ── Coherence ACROSS RUN UNITS (ISO §9.1.15, §9.1.16; kb/Work PB2660) ────────────────────────────────────

    /// <summary>The connector's own handle — <see cref="Store"/>'s, taken once — through which the store is read
    /// and written POSITIONALLY and through which the store mutex and the record locks are held
    /// (<see cref="HostRegionLocks"/>). Null whenever <see cref="Store"/> is.</summary>
    private SafeFileHandle? _handle;

    /// <summary>⛔ Whether ANOTHER RUN UNIT may write this physical file while this connector holds it open — its
    /// §9.1.15 file lock admits another writer (<see cref="FileLockPosture.AdmitsAnotherWriter"/>), which only
    /// <c>SHARING WITH ALL OTHER</c> does (§9.1.15 3), <i>"allows concurrent access to a physical file through other
    /// file connectors specifying input, I-O, or extend mode"</i>). Such a connector's store is a COPY of a file
    /// someone else may change, so every statement it executes is COHERENT (<see cref="BeginStatement"/>): it
    /// starts from the file as it is now and leaves the file as the statement left the records. A connector whose
    /// lock admits no other writer cannot meet one — Table 19 refuses that writer's OPEN in every run unit — and
    /// keeps the whole-store model (loaded at the OPEN, persisted at the CLOSE) with no per-statement cost.</summary>
    private bool _coherent;

    /// <summary>The store this connector is attached to (the run unit's ONE store for the physical file,
    /// kb/Work PB143).</summary>
    private protected abstract KeyedStore AttachedStore { get; }

    /// <summary>Replace the records of <paramref name="into"/> with <paramref name="frames"/>, the physical file's
    /// frames in ordinal order — the organization's half of a load or a reload. The store is the SHARED one, so
    /// every connector of the run unit attached to it sees the reload, which is the point.</summary>
    private protected abstract void Fill(KeyedStore into, List<StoredFrame?> frames);

    /// <summary>The store's frames in the order the organization persists them (null = a relative gap).</summary>
    private protected abstract IEnumerable<StoredFrame?> PersistFrames();

    /// <inheritdoc/>
    internal override SafeFileHandle? RecordLockHandle => _handle;

    /// <inheritdoc/>
    internal override bool RecordLockHandleWritable => _handle is not null && Mode is not FileOpenMode.Input;

    /// <summary>⛔ ENTER A COHERENT STATEMENT (kb/Work PB2660). For a connector another run unit may write
    /// (<see cref="_coherent"/>): take the store's cross-run-unit MUTEX (<see cref="HostRegionLocks.StoreMutexByte"/>)
    /// — waiting for another run unit's statement to finish — and, when the file's generation stamp differs from
    /// the one the store reflects, RELOAD the store from the file. Every governed record statement runs inside one
    /// (<c>FileRegistry</c>), so its record-lock check, its operation, its lock actions and the persist of what it
    /// changed are ONE step to every other run unit — which is what makes §9.1.16's <i>"While locked by a given
    /// file connector, a record is not accessible to another file connector in the same or a different run unit"</i>
    /// enforceable, and what makes §14.9.30.4 GR21's <i>"the first existing record in the physical file"</i> the
    /// file's and not a snapshot's. Inert (<c>default</c>) for every other connector.</summary>
    internal override StoreStatement BeginStatement()
    {
        if (!_coherent || _handle is null) return default;
        Enter(AttachedStore);
        return new StoreStatement(this);
    }

    private void Enter(KeyedStore store)
    {
        if (store.StatementDepth++ > 0) return;   // the run unit is already inside the mutex
        try
        {
            store.MutexHolder = HoldStoreMutex();
            if (ReadGeneration() is { } stamped && stamped != store.Generation) Reload(store);
            store.VersionAtEntry = store.Version;
        }
        catch
        {
            Leave(store);
            throw;
        }
    }

    /// <summary>Finish the statement's work on the physical file: when the statement changed the records, persist
    /// them now, under the mutex, so the next statement of ANY run unit sees them. A persist the medium refuses is
    /// the statement's §9.1.13.6 item 1 permanent error ('30'), reported by THIS statement rather than by a later
    /// CLOSE. Returns the status the statement ends with.</summary>
    internal string CompleteStatement(string status)
    {
        var store = AttachedStore;
        if (store.StatementDepth != 1 || store.Version == store.VersionAtEntry) return status;
        try
        {
            Persist(store);
            return status;
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            SetStatus(FileStatusCode.PermanentError);
            return FileStatusCode.PermanentError;
        }
    }

    /// <summary>Leave the statement; the outermost leave gives the mutex back.</summary>
    internal void EndStatement() => Leave(AttachedStore);

    private static void Leave(KeyedStore store)
    {
        if (--store.StatementDepth > 0) return;
        store.StatementDepth = 0;
        if (store.MutexHolder is { } holder) HostRegionLocks.Release(holder, HostRegionLocks.StoreMutexByte);
        store.MutexHolder = null;
    }

    /// <summary>⛔ WAIT OUTSIDE THE MUTEX. A statement that meets another run unit's record lock and carries a RETRY
    /// phrase waits for that run unit to give the lock back (§14.7.9.3) — which it can only do by executing a
    /// statement of its own, inside this same mutex. So the wait lets go of the mutex, and the statement re-enters
    /// (re-reading the file if it changed) before its next attempt. Nothing has been committed when a conflict is
    /// being retried — every conflict check precedes its operation (kb/Work PB338) — so there is nothing to persist
    /// first.</summary>
    internal override void WaitOutsideStatement(Action wait)
    {
        var store = AttachedStore;
        if (store.StatementDepth == 0 || store.MutexHolder is null) { wait(); return; }
        int depth = store.StatementDepth;
        store.StatementDepth = 1;
        Leave(store);
        try { wait(); }
        finally
        {
            Enter(store);
            store.StatementDepth = depth;
        }
    }

    /// <summary>The store mutex, held for the length of one statement — exclusive through a writable handle, shared
    /// through an <c>OPEN INPUT</c> connector's read-only one (a reader never changes the store, and Linux refuses
    /// an exclusive lock through a read-only descriptor; <see cref="HostRegionLocks.Hold"/>). Null when the host
    /// cannot carry the lock, and the run unit then has no protection against another's writer but the file lock
    /// (docs/CONFORMANCE.md DOC-A.1-75).</summary>
    private SafeFileHandle? HoldStoreMutex() =>
        HostRegionLocks.Hold(_handle!, HostRegionLocks.StoreMutexByte, exclusive: RecordLockHandleWritable, wait: true)
            == OfdRegionLocks.Result.Ok ? _handle : null;

    /// <summary>The generation stamped in the physical file now — null for a file holding no store header this
    /// build reads (an empty file, before its creation arm writes one).</summary>
    private ulong? ReadGeneration()
    {
        Span<byte> head = stackalloc byte[RecordFraming.GenerationProbeBytes];
        int got = ReadAt(_handle!, head, 0);
        return RecordFraming.DecodeGeneration(head[..got]);
    }

    private void Reload(KeyedStore store)
    {
        Fill(store, ReadImage(store));
        store.PersistedVersion = store.Version;
    }

    /// <summary>⛔ THE ONE LOAD — the first opener's (the <c>KeyedStoreTable</c> attach callback) and every coherent
    /// reload's. It reads under the store mutex, so it never sees a store another run unit is half-way through
    /// rewriting; an absent OPTIONAL file (no handle) loads nothing.</summary>
    private protected void Load(KeyedStore into)
    {
        if (_handle is null)
        {
            Fill(into, []);
            into.PersistedVersion = into.Version;
            return;
        }
        if (into.StatementDepth > 0) { Reload(into); return; }   // already inside the mutex
        var holder = HostRegionLocks.Hold(_handle, HostRegionLocks.StoreMutexByte, exclusive: RecordLockHandleWritable,
            wait: true) == OfdRegionLocks.Result.Ok ? _handle : null;
        try { Reload(into); }
        finally { if (holder is not null) HostRegionLocks.Release(holder, HostRegionLocks.StoreMutexByte); }
    }

    /// <summary>⛔ THE ONE PERSIST — every statement that changed a coherent store, every CLOSE that owes one, and the
    /// creation of a new store. It writes the whole image positionally through the connector's own handle, stamped
    /// with one more than the generation the file carries NOW, and truncates whatever a longer previous image left;
    /// then the store agrees with the file again. The caller holds the mutex or takes it here.</summary>
    private protected void Persist(KeyedStore store)
    {
        var handle = _handle!;
        SafeFileHandle? holder = null;
        if (store.StatementDepth == 0)
            holder = HostRegionLocks.Hold(handle, HostRegionLocks.StoreMutexByte, exclusive: true, wait: true)
                == OfdRegionLocks.Result.Ok ? handle : null;
        try
        {
            ulong generation = (ReadGeneration() ?? store.Generation) + 1;
            using var image = RecordFraming.ComposeStore(DeclaredAttributes, PersistFrames(), generation, CodeSet);
            RandomAccess.Write(handle, new ReadOnlySpan<byte>(image.GetBuffer(), 0, (int)image.Length), 0);
            RandomAccess.SetLength(handle, image.Length);
            store.Generation = generation;
            store.PersistedVersion = store.Version;
        }
        finally { if (holder is not null) HostRegionLocks.Release(holder, HostRegionLocks.StoreMutexByte); }
    }

    /// <summary>Persist at the CLOSE when this run unit holds records the file does not — the whole-store model's
    /// one write, and a no-op for a coherent connector, whose statements already persisted.</summary>
    private protected void PersistIfChanged(KeyedStore store)
    {
        if (store.Version != store.PersistedVersion) Persist(store);
    }

    /// <summary>The whole physical file, in ONE positional read under the caller's mutex, decoded; the store is
    /// stamped with the generation the image carries.</summary>
    private List<StoredFrame?> ReadImage(KeyedStore into)
    {
        var handle = _handle!;
        int size = checked((int)RandomAccess.GetLength(handle));
        // RENTED, not allocated: a keyed store is routinely past the 85 KB large-object threshold, and this runs at
        // every OPEN and every coherent reload.
        byte[] all = ArrayPool<byte>.Shared.Rent(Math.Max(size, 1));
        try
        {
            int got = ReadAt(handle, all.AsSpan(0, size), 0);
            into.Generation = RecordFraming.DecodeGeneration(all.AsSpan(0, got)) ?? 0;
            return RecordFraming.DecodeStore(all, got, CodeSet);
        }
        finally { ArrayPool<byte>.Shared.Return(all); }
    }

    /// <summary>Fill <paramref name="into"/> from <paramref name="offset"/> until it is full or the file ends;
    /// returns the bytes read.</summary>
    private static int ReadAt(SafeFileHandle handle, Span<byte> into, long offset)
    {
        int total = 0;
        while (total < into.Length)
        {
            int n = RandomAccess.Read(handle, into[total..], offset + total);
            if (n <= 0) break;
            total += n;
        }
        return total;
    }
}

/// <summary>⛔ ONE STATEMENT'S HOLD ON A KEYED STORE that another run unit may write (kb/Work PB2660) — returned by
/// <see cref="FileConnector.BeginStatement"/> and inert (<c>default</c>) for every connector that cannot meet such a
/// writer. <c>FileRegistry</c> opens one around each governed record statement, ends the statement through
/// <see cref="Complete"/> (which persists what the statement changed and reports a refused persist as the
/// statement's status); a RETRY wait lets the mutex go through <see cref="FileConnector.WaitOutsideStatement"/>.</summary>
internal readonly struct StoreStatement : IDisposable
{
    private readonly KeyedConnector? _owner;

    internal StoreStatement(KeyedConnector owner) => _owner = owner;

    /// <summary>Persist what the statement changed; returns the status the statement ends with.</summary>
    public string Complete(string status) => _owner is { } owner ? owner.CompleteStatement(status) : status;

    /// <summary>Leave the statement (the outermost leave gives the mutex back).</summary>
    public void Dispose() => _owner?.EndStatement();
}
