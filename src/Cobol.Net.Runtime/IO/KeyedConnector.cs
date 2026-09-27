// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    /// on an I/O failure, but it takes exactly the handle shape — <c>HostFile.OpenAuxiliary</c>, share
    /// <see cref="FileShare.ReadWrite"/> — that <see cref="RecordFraming.ReadStore"/> takes a few statements
    /// later inside <c>OpenCore</c>, where the failure PROPAGATES to <c>FileConnector.Open</c>'s catch and
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
    /// defect, re-opened by its own cure. That is why <c>RecordFraming.ReadStore</c>/<c>WriteStore</c> take a
    /// stream and no longer a path.</para>
    /// <para>⛔ WHY <see cref="FileMode.Open"/> AND NEVER <c>Create</c>, even for <c>OPEN OUTPUT</c>: the handle
    /// outlives every persist, and <c>WriteStore</c> truncates as part of the rewrite. A creating MODE on the
    /// handle would make the truncation a property of when the handle happened to be taken, and
    /// <see cref="Reposture"/> rebuilds it mid-open — which would then silently empty the file.</para></summary>
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
    /// truncation is <c>RecordFraming.WriteStore</c>'s.</para></summary>
    /// <returns>The handle, so the arm that took it writes its store through that value rather than through a
    /// nullable field it has to re-assert.</returns>
    protected FileStream TakeFileLock(bool create)
    {
        var taken = OpenLockHandle(create ? FileMode.OpenOrCreate : FileMode.Open, HostShare);
        _store?.Dispose();   // no arm takes it twice; belt-and-braces so a future one cannot leak
        return _store = taken;
    }

    /// <summary>⛔ THE ONE SPELLING OF THIS CONNECTOR'S HANDLE — <see cref="TakeFileLock"/> and the rebuild in
    /// <see cref="Reposture"/> are the same stream, so its access and its mode have one answer each.</summary>
    private FileStream OpenLockHandle(FileMode mode, FileShare share) =>
        HostFile.OpenConnectorStream(HostPath, mode, HostAccess(Mode), share);

    /// <summary>Give the file lock back — §9.1.15, <i>"The file lock is removed by an explicit or implicit CLOSE
    /// statement executed for that file connector"</i>. Runs from each organization's <c>CloseCore</c> finally
    /// (whatever the persist did) and from <see cref="AbandonOpen"/>.</summary>
    protected void ReleaseFileLock()
    {
        // The field is cleared whatever the disposal throws (a final flush refused on an exhausted medium — the
        // disposal still closes the host handle), so a later CLOSE or re-OPEN never meets a disposed handle.
        try { _store?.Dispose(); }
        finally { _store = null; }
    }

    /// <inheritdoc/>
    protected override void AbandonOpen() => ReleaseFileLock();

    /// <inheritdoc/>
    /// <remarks>A share mode is fixed when a handle is created, so widening means REBUILDING the handle. For
    /// these organizations that is ALL it means — the store lives in memory (<c>KeyedStoreTable</c>) and this
    /// handle carries no position a COBOL statement can observe, unlike the sequential connector's reader, which
    /// has to be re-seeked to its file position indicator.
    /// <para>The order is <see cref="FileConnector.RebuildMustReleaseFirst"/>'s and the reasoning lives there,
    /// with the sequential connector reading the same predicate: every writable mode of these organizations
    /// holds WRITE access (the store is rewritten whole), and a handle holding write access is itself what would
    /// refuse the replacement. ⛔ A REFUSED REBUILD LEAVES THE POSTURE WHERE IT WAS, not merely the handle:
    /// <see cref="FileConnector.HostShare"/> is the connector's claim about what its handle admits, so returning
    /// without calling the base keeps the two in step, and the OPEN that asked for the widening then fails
    /// inside its own try with §9.1.13.6 item 1's '30' instead of being told it succeeded.</para></remarks>
    internal override void Reposture(FileShare share)
    {
        if (share == HostShare) return;
        if (!IsOpen || _store is null) { base.Reposture(share); return; }
        var held = HostShare;
        if (RebuildMustReleaseFirst)
        {
            _store.Dispose();
            _store = null;
            try { _store = OpenLockHandle(FileMode.Open, share); }
            catch (IOException)
            {
                // Only a foreign process can have taken the file in that window. Restore exactly what was
                // there; if even that is gone the connector is left lockless and CloseCore answers '30'
                // (§9.1.13.6 item 1) rather than persisting through a handle it does not have.
                try { _store = OpenLockHandle(FileMode.Open, held); }
                catch (IOException) { }
                return;
            }
        }
        else
        {
            var fresh = OpenLockHandle(FileMode.Open, share);   // read-only: no window, nothing to restore
            _store.Dispose();
            _store = fresh;
        }
        base.Reposture(share);
    }

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

    /// <summary>Whether a CLOSE that owes a persist can still reach the physical file — false only when a
    /// <see cref="Reposture"/> rebuild lost the handle to a foreign process (see there). §9.1.13.6 item 1's
    /// '30' is the answer in that case, because the records this connector holds cannot be written: reporting a
    /// successful CLOSE over them would be the silent loss kb/Work PB771 exists to remove, one step along.
    /// </summary>
    protected bool PersistIsReachable(bool owed) => !owed || _store is not null;
}
