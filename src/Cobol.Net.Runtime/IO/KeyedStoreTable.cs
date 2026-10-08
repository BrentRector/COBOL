// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⭐ The per-PHYSICAL-FILE record stores (kb/Work PB143; ISO §14.9.10.4 GR5 — "the identified record has been
/// logically removed from THE PHYSICAL FILE and can no longer be accessed"). Every keyed connector used to load
/// a PRIVATE snapshot at OPEN and persist its WHOLE view at CLOSE, so a record DELETEd through one connector
/// stayed readable through another over the same host path, and the CLOSE ORDER decided which view survived on
/// disk — silent undeletion / silent data loss. Now the record images live HERE, keyed by resolved host path
/// (the same key the <see cref="PhysicalFileTable"/> arbitrates sharing and locks by): the FIRST opener loads
/// from disk, later openers ATTACH to the live store (never reload — the in-memory store IS the truth while any
/// connector holds it), every mutation is instantly visible to every attached connector, any CLOSE persists the
/// ONE shared state (order no longer matters), and the LAST detach drops the entry so a later OPEN re-reads the
/// disk. Position/key state (FPI, key of reference, sequential-WRITE slot, GR38 high-key) stays per-CONNECTOR.
/// Two SELECTs to one ASSIGN target need no SHARING clause to reach this, so the store is unconditional for the
/// keyed organizations; sequential connectors keep their OS-backed streams (the file system is their shared
/// store). Owned by the <see cref="FileRegistry"/>; cleared at run-unit Reset.
/// </summary>
internal sealed class KeyedStoreTable
{
    private sealed class Entry
    {
        public required object Store;
        public int Attached;
    }

    private readonly Dictionary<string, Entry> _byHost = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Attach to the RELATIVE store for <paramref name="host"/>: the live store when one exists (its
    /// content is the truth — no reload), else a fresh store populated by <paramref name="loadFirst"/>.</summary>
    public RelativeStore AttachRelative(string host, Action<RelativeStore> loadFirst)
        => Attach(host, () => { var s = new RelativeStore(); loadFirst(s); return s; });

    /// <summary>Attach to the INDEXED store for <paramref name="host"/> — same contract.</summary>
    public IndexedStore AttachIndexed(string host, Action<IndexedStore> loadFirst)
        => Attach(host, () => { var s = new IndexedStore(); loadFirst(s); return s; });

    /// <summary>⛔ ATTACH TO A STORE THAT IS BEING CREATED — <c>OPEN OUTPUT</c>, §14.9.27.4 GR18: a new, EMPTY store
    /// for <paramref name="host"/> that no other connector of this run unit holds (kb/Work PB754).
    /// <para>Emptying a store is a mutation every attached connector would see instantly (kb/Work PB143), so it is
    /// only ever done HERE, where the precondition that makes it safe is checked rather than remembered: Table 19
    /// refuses every OUTPUT open beside any other open connector — §9.1.13.9 1) e), <i>"An attempt is made to open a
    /// physical file in the output mode and the physical file is currently open by another file connector"</i> —
    /// and <c>FileRegistry.OpenShared</c> runs that arbitration BEFORE the connector's OPEN body. An entry that
    /// still exists here therefore means the arbitration was bypassed, and the answer is a loud defect, never a
    /// silent emptying of records another connector is reading (the analogous impossible state in
    /// <see cref="Attach{T}"/> is answered the same way).</para></summary>
    public T AttachCreated<T>(string host) where T : KeyedStore, new()
    {
        if (_byHost.TryGetValue(host, out var e))
            throw new InvalidOperationException(
                $"physical file '{host}' is being created by OPEN OUTPUT while {e.Attached} other connector(s) of this "
                + "run unit hold it open; Table 19 refuses that OPEN (§9.1.13.9 1) e)), so reaching here is an "
                + "arbitration defect (kb/Work PB754)");
        var store = new T();
        _byHost[host] = new Entry { Store = store, Attached = 1 };
        return store;
    }

    private T Attach<T>(string host, Func<T> create) where T : KeyedStore
    {
        if (_byHost.TryGetValue(host, out var e))
        {
            if (e.Store is not T live)
                throw new InvalidOperationException(
                    $"physical file '{host}' is open under two different organizations ({e.Store.GetType().Name} vs {typeof(T).Name}) — a compiler/registration defect (kb/Work PB143)");
            e.Attached++;
            return live;
        }
        var store = create();
        _byHost[host] = new Entry { Store = store, Attached = 1 };
        return store;
    }

    /// <summary>Detach one connector from <paramref name="host"/>'s store; the LAST detach drops the entry so a
    /// later OPEN reloads from disk. Unbalanced detaches are ignored (a failed OPEN never attached).</summary>
    public void Detach(string host)
    {
        if (!_byHost.TryGetValue(host, out var e)) return;
        if (--e.Attached <= 0) _byHost.Remove(host);
    }

    /// <summary>Run-unit start hygiene.</summary>
    public void Clear() => _byHost.Clear();
}

/// <summary>⭐ What every keyed record store knows about its agreement with the PHYSICAL FILE — the state the
/// cross-run-unit coherence of <c>KeyedConnector</c> runs on (kb/Work PB2660; ISO §9.1.15, <i>"Multiple paths of
/// access may exist in the same runtime element, contained elements, separate runtime elements within the same run
/// unit, or runtime elements in different run units"</i>).
/// <para>The store is this run unit's copy of the physical file. Within the run unit it IS the truth (kb/Work
/// PB143); across run units the file is, and these numbers say how far the copy may be trusted: whether it has
/// changed since it last agreed with the file (<see cref="Version"/> against <see cref="PersistedVersion"/>), which
/// image of the file it agreed with (<see cref="Generation"/>, the stamp every persist writes —
/// <see cref="RecordFraming.GenerationOffset"/>), and whether a statement of this run unit is already inside the
/// store's cross-run-unit mutex (<see cref="StatementDepth"/>).</para></summary>
internal abstract class KeyedStore
{
    /// <summary>Bumped by EVERY change to the records — the organizations' mutators are the only writers.</summary>
    public long Version { get; private set; }

    /// <summary>The <see cref="Version"/> at which the store last agreed with the physical file: set by the load
    /// and by every persist, so <c>Version != PersistedVersion</c> is exactly "this run unit holds records the
    /// file does not".</summary>
    public long PersistedVersion { get; set; }

    /// <summary>The generation stamp of the physical file image the store last loaded or persisted.</summary>
    public ulong Generation { get; set; }

    /// <summary>How many statements of this run unit are inside the store's cross-run-unit mutex. A run unit is one
    /// thread of control, so a nested entry re-enters rather than deadlocking against itself.</summary>
    public int StatementDepth { get; set; }

    /// <summary>The handle holding the store's cross-run-unit mutex while <see cref="StatementDepth"/> is non-zero —
    /// null when no statement is inside it, or when the host cannot carry the lock.</summary>
    public Microsoft.Win32.SafeHandles.SafeFileHandle? MutexHolder { get; set; }

    /// <summary>The <see cref="Version"/> when the outermost statement entered — so the statement persists only
    /// what IT changed.</summary>
    public long VersionAtEntry { get; set; }

    /// <summary>Record that the records changed — called by every mutator, and by nothing else.</summary>
    protected void Mutated() => Version++;
}

/// <summary>The shared RELATIVE record store: RRN (1-based, §12.4.5.13 GR1) → record image, plus the ONE
/// number a sequential-access release needs — <see cref="Highest"/>, the highest RRN existing in the physical
/// file right now.
/// <para>⛔ THE SLOTS ARE READ-ONLY TO THE CONNECTOR (kb/Work PB739). ISO §14.9.51.4 GR29 a) assigns an extend
/// release <i>"a record number that is one greater than the highest relative record number existing in the
/// physical file"</i> and then says <i>"If the physical file is shared and the open mode is extend, the record
/// numbers are not necessarily consecutive"</i> — which is only true if the number is taken from the file at
/// the moment of the release. <c>RelativeConnector</c> took it once, at OPEN, into a private <c>_seqNext</c>, so
/// two connectors extending one shared file both minted RRN 2 and the second <c>Slots[2] = rec</c> silently
/// replaced the first record. Exposing the map as <see cref="IReadOnlyDictionary{TKey,TValue}"/> and routing
/// every mutation through <see cref="Put"/>/<see cref="Remove"/>/<see cref="Clear"/> makes the high-water mark
/// impossible to leave stale: there is no second way to change the store.</para>
/// <para>Maintained rather than scanned because a sequential extend asks for it once per WRITE, and a
/// <c>SortedDictionary</c> has no O(1) maximum — scanning would have made an n-record append O(n²). Only the
/// removal OF the maximum pays a scan, and only then.</para></summary>
internal sealed class RelativeStore : KeyedStore
{
    private readonly SortedDictionary<long, StoredFrame> _slots = new();

    /// <summary>The records, in ascending RRN order, each with the extent table it was released with (if any —
    /// D-FRA (v), kb/Work PB1053). Read-only by construction — see the type summary.</summary>
    public IReadOnlyDictionary<long, StoredFrame> Slots => _slots;

    /// <summary>The highest relative record number existing in the physical file (0 when it holds none) —
    /// §14.9.51.4 GR29 a)'s "the highest relative record number existing in the physical file".</summary>
    public long Highest { get; private set; }

    /// <summary>The bytes the records occupy in the persisted store (<see cref="RecordFraming.FrameBytes"/>
    /// summed) — maintained by the three mutators, like <see cref="Highest"/>, so the WRITE-time boundary test
    /// is O(1).</summary>
    private long _recordBytes;

    /// <summary>The bytes the frames of the persisted store would occupy after <see cref="Put"/>(<paramref
    /// name="rrn"/>, <paramref name="record"/>): every record's frame plus a <see cref="RecordFraming.GapBytes"/>
    /// tag for every empty slot below the highest RRN — the same image <see cref="RecordFraming.ComposeStore"/>
    /// composes, so the keyed WRITE and REWRITE can answer §14.9.51.4 GR33 b)'s '24' BEFORE the record is
    /// released (Annex A.1 item 107; kb/Work PB1192).</summary>
    public long FramedBytesAfterPut(long rrn, StoredFrame record)
    {
        bool replaces = _slots.TryGetValue(rrn, out StoredFrame replaced);
        long records = _recordBytes + RecordFraming.FrameBytes(record) - (replaces ? RecordFraming.FrameBytes(replaced) : 0);
        long count = _slots.Count + (replaces ? 0 : 1);
        return records + (RecordFraming.GapBytes * (Math.Max(Highest, rrn) - count));
    }

    /// <summary>The store's frames in ordinal order — null for an empty slot — produced LAZILY from the sparse
    /// map, so a persist never materializes one entry per relative record number (a dense array sized by
    /// <see cref="Highest"/> overflowed at a large key and allocated millions of slots for one record —
    /// kb/Work PB1192).</summary>
    public IEnumerable<StoredFrame?> Ordinal()
    {
        long next = 1;
        foreach (var (rrn, record) in _slots)
        {
            for (; next < rrn; next++) yield return null;
            yield return record;
            next = rrn + 1;
        }
    }

    /// <summary>Release or replace the record at <paramref name="rrn"/>.</summary>
    public void Put(long rrn, StoredFrame record)
    {
        if (_slots.TryGetValue(rrn, out StoredFrame replaced)) _recordBytes -= RecordFraming.FrameBytes(replaced);
        Mutated();
        _slots[rrn] = record;
        _recordBytes += RecordFraming.FrameBytes(record);
        if (rrn > Highest) Highest = rrn;
    }

    /// <summary>Remove the record at <paramref name="rrn"/> (§14.9.10.4 GR5 — "logically removed from the
    /// physical file"); false when there was none. Removing the maximum re-derives it, which is the only
    /// scan this store ever pays.</summary>
    public bool Remove(long rrn)
    {
        if (!_slots.TryGetValue(rrn, out StoredFrame removed)) return false;
        Mutated();
        _slots.Remove(rrn);
        _recordBytes -= RecordFraming.FrameBytes(removed);
        if (rrn == Highest) Highest = _slots.Count == 0 ? 0 : _slots.Keys.Max();
        return true;
    }

    /// <summary>Empty the store (OPEN OUTPUT, and the OPEN I-O/EXTEND creation of an absent OPTIONAL file).</summary>
    public void Clear()
    {
        Mutated();
        _slots.Clear();
        _recordBytes = 0;
        Highest = 0;
    }
}

/// <summary>One stored indexed record: its character image and its PER-KEY release ordinals — lifted out of
/// <see cref="IndexedConnector"/> when the store became shared (kb/Work PB143).
/// <para>⛔ THE RELEASE ORDINAL IS PER KEY OF REFERENCE, NOT PER RECORD (kb/Work PB341). ISO §14.9.30.4 GR26
/// names the retrieval order of duplicates under "an alternate record key that IS THE KEY OF REFERENCE", and
/// §14.9.35.4 GR24 a) — "When the value of a specific alternate record key is not changed, the order of
/// retrieval when that key is the key of reference remains unchanged" — makes each key's order independent of
/// every other key's: a REWRITE repositions the record ONLY in the duplicate sets of the keys it actually
/// changed (GR24 b). One number per record could not express that, so a REWRITE that changed one alternate key
/// silently reordered every OTHER alternate key's duplicate sequence. <see cref="Ordinals"/> is therefore a
/// VECTOR: slot 0 the prime key (assigned once at release and never re-stamped — a prime key value cannot
/// change, §14.9.35.4 GR22/GR23 identify the record BY it — so it doubles as the record's release order in the
/// physical file), slot <c>i + 1</c> the i-th alternate key.</para></summary>
internal sealed class KeyedRec
{
    public string Image = "";
    /// <summary>The EXTENT TABLE the record was released with (determination D-FRA (v); kb/Work PB1053) — null when
    /// the record carries none. Replaced with <see cref="Image"/>, never apart from it.</summary>
    public RecordExtents? Extents;
    public long[] Ordinals = [];
}

/// <summary>The shared INDEXED record store: the records plus the release-ordinal mint — shared so a WRITE
/// through one connector takes the next GLOBAL ordinal and §14.9.30.4 GR26's duplicate-alternate retrieval
/// order holds across connectors.</summary>
internal sealed class IndexedStore : KeyedStore
{
    private readonly List<KeyedRec> _recs = [];

    /// <summary>The records, in load order — the persisted order. READ-ONLY to the connector, exactly as
    /// <see cref="RelativeStore.Slots"/> is: every mutation goes through <see cref="Add"/>, <see cref="Replace"/>,
    /// <see cref="Remove"/> or <see cref="Clear"/>, which keep <see cref="RecordBytes"/> true (kb/Work PB1192).</summary>
    public IReadOnlyList<KeyedRec> Recs => _recs;

    public long NextOrdinal = 1;

    /// <summary>The bytes the records' frames occupy in the persisted store (<see cref="RecordFraming.FrameBytes"/>
    /// summed) — maintained by the mutators, so §14.9.51.4 GR42 d)'s boundary test at the WRITE is O(1)
    /// (Annex A.1 item 107).</summary>
    public long RecordBytes { get; private set; }

    /// <summary>The frame bytes of one stored record — <see cref="RecordFraming.FrameBytes"/> of its image and
    /// extent table.</summary>
    public static long FrameBytes(string image, RecordExtents? extents) =>
        RecordFraming.FrameBytes(new StoredFrame(image, extents));

    /// <summary>Release a record into the store (a WRITE, or the OPEN's load).</summary>
    public void Add(KeyedRec rec)
    {
        Mutated();
        _recs.Add(rec);
        RecordBytes += FrameBytes(rec.Image, rec.Extents);
    }

    /// <summary>Replace a stored record's content in place (§14.9.35 REWRITE) — its image and extent table
    /// together, never apart (D-FRA (v)).</summary>
    public void Replace(KeyedRec rec, string image, RecordExtents? extents)
    {
        RecordBytes += FrameBytes(image, extents) - FrameBytes(rec.Image, rec.Extents);
        Mutated();
        rec.Image = image;
        rec.Extents = extents;
    }

    /// <summary>Remove a record (§14.9.10 DELETE); false when it was not in the store.</summary>
    public bool Remove(KeyedRec rec)
    {
        if (!_recs.Remove(rec)) return false;
        Mutated();
        RecordBytes -= FrameBytes(rec.Image, rec.Extents);
        return true;
    }

    /// <summary>Empty the store (OPEN OUTPUT, the absent-OPTIONAL creation, and the OPEN's reload).</summary>
    public void Clear()
    {
        Mutated();
        _recs.Clear();
        RecordBytes = 0;
    }
}
