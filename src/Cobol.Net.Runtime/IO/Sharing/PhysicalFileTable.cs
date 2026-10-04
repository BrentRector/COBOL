// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The physical-file sharing/record-lock registry (ISO §9.1.15 sharing / §9.1.16 record locking; design D1 —
/// the sharing/locking statuses 51/52/61 are defined over <em>"another file connector"</em> (§9.1.13.9), and two
/// SELECTs bound to one resolved host path are two connectors over one physical file within one run unit). One
/// instance owned by the <see cref="FileRegistry"/>: the per-host open-connector table (sharing mode + open mode
/// — the Table-19 conflict inputs) and the record locks (owner connector per record id).
/// </summary>
internal sealed class PhysicalFileTable
{
    /// <summary>The live state of one physical file (keyed by resolved host path).</summary>
    internal sealed class State
    {
        /// <summary>connector name → the (sharing mode, open mode) it is currently open under — Table 19's two
        /// conflict inputs. EVERY successful OPEN is here, not only the connectors that wrote a SHARING or LOCK
        /// MODE clause (§9.1.15's gate is written over the physical file, kb/Work PB321); a connector with
        /// neither clause nor phrase is recorded under the implementor default of its open mode
        /// (<see cref="FileRegistry.ImplementorDefaultSharing"/>).</summary>
        public readonly Dictionary<string, (FileSharing Sharing, FileOpenMode Mode)> Open =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>record-id → the connector name that holds its lock.</summary>
        public readonly Dictionary<string, string> RecordLocks = new(StringComparer.Ordinal);

        /// <summary>⭐ The SEQUENTIAL release-ordinal mint for this physical file (kb/Work PB739): the ordinal
        /// of the record most recently released to the operating environment through any sharing-active
        /// connector over this path, so the next release takes <c>++ReleasedOrdinal</c>.
        /// <para>It is here, on the ONE object that stands for the physical file, for the reason §14.9.51.4
        /// GR19 gives — <i>"If two or more file connectors for a sequential file add records by sharing the
        /// physical file after opening it in extend mode, the added records follow the records present in the
        /// physical file when it was opened"</i>: the records present are the records EVERY connector has
        /// released, so a per-connector counter cannot name them. Two shared <c>OPEN EXTEND</c> connectors each
        /// kept their own base plus their own count, so both called their first appended record ordinal 2 —
        /// and §9.1.16's <i>"While locked by a given file connector, a record is not accessible to another file
        /// connector"</i> is written over exactly that identity, so the locks landed on the wrong records.</para>
        /// <para>Seeded by a sharing-active <c>OPEN EXTEND</c> from the records already in the physical file
        /// (§14.9.51.4 GR18) and reset to 0 by a sharing-active <c>OPEN OUTPUT</c>, which truncates. The keyed
        /// organizations do not use it: their release identity is a key or an RRN minted from the shared record
        /// store, which is their physical file.</para></summary>
        public long ReleasedOrdinal;

        /// <summary>⭐ The RELEASE GENERATION of this physical file (kb/Work PB753): a count of the logical
        /// records that have been released to the operating environment THROUGH THIS PATH and have reached the
        /// physical file. A reader records the value its read-ahead was filled at and re-anchors when they
        /// differ, so it can never serve an image a sibling connector has since replaced.
        /// <para>It is the READ-SIDE twin of <see cref="ReleasedOrdinal"/>, and it counts BOTH verbs that
        /// release, because the standard gives them the same sentence: §14.9.51.4 GR12 — <i>"The successful
        /// execution of a WRITE statement releases a logical record to the operating environment"</i> — and
        /// §14.9.35.4 GR4 — <i>"The successful execution of the REWRITE statement releases a logical record to
        /// the operating environment"</i>. What a READ owes against that is §14.9.30.4 GR21 c) and d): the
        /// record selected is <i>"the first existing record IN THE PHYSICAL FILE whose relative key number is
        /// greater than the file position indicator"</i> and it is that record that <i>"is made available in
        /// the record area"</i> — the physical file as it stands at the READ, never a snapshot of what it said
        /// when a buffer was filled.</para>
        /// <para>⛔ IT COUNTS RELEASES THAT HAVE REACHED THE FILE, not WRITE statements. A connector whose
        /// §9.1.15 file lock admits no other writer keeps its buffered writer and flushes at CLOSE (see
        /// <c>SequentialConnector.ReleaseRecord</c>), so its records are not in the physical file yet and a
        /// reader told to re-anchor for them would read a half-written frame instead of a stale whole one.
        /// The generation moves exactly where the bytes do.</para>
        /// <para>The keyed organizations do not use it, for the reason that names the fix: their record images
        /// live in the ONE <see cref="KeyedStoreTable"/> store for the path (kb/Work PB143), so a sibling's
        /// REWRITE is visible to every attached connector the instant it happens — measured, not assumed.
        /// This is the same rule for the organization whose medium is the host file itself.</para></summary>
        public long ReleaseGeneration;
    }

    /// <summary>The per-file-connector record-lock ceiling (§12.4.5.9 GR7 — implementor max, ≥15) → status 54.</summary>
    private const int ConnectorLockMax = 15;
    /// <summary>The per-run-unit record-lock ceiling (§12.4.5.9 GR7 — implementor max, ≥255) → status 53.</summary>
    private const int RunUnitLockMax = 255;

    private readonly Dictionary<string, State> _byHost = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The state for <paramref name="host"/>, created on first reference.</summary>
    public State For(string host)
    {
        if (!_byHost.TryGetValue(host, out var st)) _byHost[host] = st = new State();
        return st;
    }

    /// <summary>The state for <paramref name="host"/> if one exists (no creation).</summary>
    public bool TryGet(string host, out State st) => _byHost.TryGetValue(host, out st!);

    /// <summary>Drop every physical-file state (run-unit start hygiene).</summary>
    public void Clear() => _byHost.Clear();

    /// <summary>Acquire a lock on <paramref name="recId"/> for connector <paramref name="name"/>; re-locking a
    /// record the connector already holds is idempotent (GR8 self-access). Enforces the connector ceiling (54)
    /// and the run-unit ceiling (53). Returns 00 on grant.</summary>
    public string LockRecord(State st, string name, string recId)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner))
            return string.Equals(owner, name, StringComparison.OrdinalIgnoreCase)
                ? FileStatusCode.Success                     // self re-lock (GR8) — idempotent
                : FileStatusCode.RecordLocked;               // 51 — another connector holds it
        int mine = 0;
        foreach (var o in st.RecordLocks.Values)
            if (string.Equals(o, name, StringComparison.OrdinalIgnoreCase)) mine++;
        if (mine >= ConnectorLockMax) return FileStatusCode.ConnectorLockLimit;   // 54 (GR7)
        if (TotalRunUnitLocks() >= RunUnitLockMax) return FileStatusCode.RunUnitLockLimit;   // 53 (GR7)
        st.RecordLocks[recId] = name;
        return FileStatusCode.Success;
    }

    /// <summary>True when <paramref name="recId"/> is locked by a connector OTHER than <paramref name="name"/>.</summary>
    public static bool IsLockedByOther(State st, string name, string recId) =>
        st.RecordLocks.TryGetValue(recId, out var owner)
        && !string.Equals(owner, name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Release every record lock held by <paramref name="name"/> on <paramref name="st"/> (UNLOCK, CLOSE).</summary>
    public static void ReleaseAllForConnector(State st, string name)
    {
        var mine = st.RecordLocks.Where(kv => string.Equals(kv.Value, name, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key).ToList();
        foreach (var k in mine) st.RecordLocks.Remove(k);
    }

    /// <summary>Release a single record lock a connector holds (the LOCK MODE single-lock discipline, GR6).</summary>
    public static void ReleaseSingle(State st, string name, string recId)
    {
        if (st.RecordLocks.TryGetValue(recId, out var owner)
            && string.Equals(owner, name, StringComparison.OrdinalIgnoreCase))
            st.RecordLocks.Remove(recId);
    }

    /// <summary>Release every record lock <paramref name="name"/> holds EXCEPT one on <paramref name="keepId"/>
    /// — the single-lock begin-of-statement discipline (§12.4.5.9 GR6): a REWRITE/DELETE releases a self-lock
    /// held on a record OTHER than its target at the beginning of execution (§14.9.35 GR12a2 / §14.9.10 GR7a2);
    /// the target's own lock survives to the completion rules.</summary>
    public static void ReleaseAllExcept(State st, string name, string keepId)
    {
        var mine = st.RecordLocks
            .Where(kv => string.Equals(kv.Value, name, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(kv.Key, keepId, StringComparison.Ordinal))
            .Select(kv => kv.Key).ToList();
        foreach (var k in mine) st.RecordLocks.Remove(k);
    }

    /// <summary>Would acquiring one NEW lock (a record <paramref name="name"/> does not already hold) exceed a
    /// §12.4.5.9 GR7 ceiling? Returns 54/53/00 WITHOUT acquiring — the pre-flight for the mutating verbs, whose
    /// statement must be unsuccessful with the operation NOT performed (§14.9.51 GR15 / §14.9.35 GR14) rather
    /// than lock-fail after the record already changed.</summary>
    public string PreflightNewLock(State st, string name)
    {
        int mine = 0;
        foreach (var o in st.RecordLocks.Values)
            if (string.Equals(o, name, StringComparison.OrdinalIgnoreCase)) mine++;
        if (mine >= ConnectorLockMax) return FileStatusCode.ConnectorLockLimit;     // 54 (GR7)
        if (TotalRunUnitLocks() >= RunUnitLockMax) return FileStatusCode.RunUnitLockLimit;   // 53 (GR7)
        return FileStatusCode.Success;
    }

    private int TotalRunUnitLocks()
    {
        int n = 0;
        foreach (var st in _byHost.Values) n += st.RecordLocks.Count;
        return n;
    }
}
