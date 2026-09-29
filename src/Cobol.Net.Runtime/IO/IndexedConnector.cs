// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native INDEXED-organization file connector (ISO/IEC 1989:2023 §9.1.7.4): records identified by a unique
/// PRIME record key plus optional ALTERNATE record keys (each a byte range of the record's character image,
/// §12.4.5.12/§12.4.5.6). Ports the legacy <c>IndexedFileHandler</c>'s NIST-IX-proven design: the record list is the
/// SOLE source of truth (alternate orderings are derived on demand — a cloned index goes stale) and each record
/// carries a PER-KEY release ordinal (<see cref="KeyedRec.Ordinals"/>) realizing the duplicate-alternate retrieval
/// order (§14.9.30.4 GR26/GR32, §14.9.51.4 GR40, §14.9.35.4 GR24).
/// <para>⛔ THE FILE POSITION INDICATOR IS A KEY VALUE AND NOTHING ELSE (kb/Work PB342). §14.9.41.4 GR17 e) 1. —
/// "The file position indicator is set to the value of the key of reference in the first logical record whose key
/// satisfies the comparison" — and §14.9.30.4 GR21 g)/GR32 say the same of a READ. The standard never puts a
/// duplicate POSITION in it, which is why §14.9.30.4 GR21 rules e) and f) have to name "the record that was made
/// available by that prior READ statement" instead of consulting it. This connector models exactly that: the FPI
/// (<c>_fpiKey</c>) is the key value, and the prior READ's position within its set of duplicates
/// (<c>_readOrdinal</c>) is separate state consulted ONLY under rules e)/f). Modelling the FPI as the pair
/// (key, ordinal) made a START into a duplicate set resume at the wrong end and lose the rest of the set.
/// Positions are held as VALUES, not record references, so they survive an interleaved REWRITE/DELETE of the
/// current record (§14.9.10.4 GR9).</para>
/// Key comparisons are ordinal over the Latin-1 character image: record keys are category alphanumeric/national
/// (§12.4.5.12 SR2), for which ordinal IS the native collating sequence — correct for COBOL-85 NIST; the file-level
/// COLLATING SEQUENCE clause (§12.4.5.7, WRITE §14.9.51.4 GR35/GR42) is the SPECIAL-NAMES/alphabet subsystem's seam.
/// </summary>
public sealed class IndexedConnector : KeyedConnector
{
    private readonly int _primeOff, _primeLen;
    private readonly CobolCollation? _primeCollation;   // §12.4.5.7 prime-key collating sequence (the CobolCollation carrier); null = native ordinal
    private readonly List<(int Off, int Len, bool Dups, CobolCollation? Collation, string? Suppress, CobolContiguousLayout? Layout)> _alts = [];
    /// <summary>The prime key's record-type layout when a variable-length member precedes it (kb/Work PB1025) —
    /// <see cref="_primeOff"/> is then its offset in the record's FIXED run; null otherwise.</summary>
    private readonly CobolContiguousLayout? _primeLayout;
    /// <summary>True when ANY key's position varies with the record (<see cref="KeyOf"/>): such a key is located
    /// in the record AS WRITTEN — its length is what places it — so no key is ever sliced from a record image
    /// padded or truncated to the area width (<see cref="AreaKey"/>, and the WRITE / REWRITE key image).</summary>
    private bool _layoutKeys;
    /// <summary>The attached PER-PHYSICAL-FILE store (kb/Work PB143): every connector over one host path sees
    /// ONE record list and ONE release-ordinal mint. A placeholder until OPEN attaches (and after CLOSE
    /// detaches).</summary>
    private IndexedStore _st = new();

    private IReadOnlyList<KeyedRec> _recs => _st.Recs;   // load order — the persisted order (the ATTACHED store's; mutated only through it)
    private long _nextOrdinal { get => _st.NextOrdinal; set => _st.NextOrdinal = value; }

    /// <summary>The key-index spelling of THE PRIME RECORD KEY: <c>_alts</c> is indexed from 0, so the prime key
    /// is the one index below it. Every key-index parameter on this connector reads it (<see cref="KeyOf"/>,
    /// <see cref="Ordered"/>, <see cref="IsSuppressed"/>), and it is what §14.9.27.4 GR14 and §14.9.41.4
    /// GR18/GR19 mean by "the prime record key" / "the primary key".</summary>
    private const int PrimeKey = -1;

    private int _refKey = PrimeKey;      // key of reference: PrimeKey, or i = i-th alternate (§14.9.30.4 GR30/GR31)
    private string _fpiKey = "";         // THE file position indicator — a KEY VALUE (§14.9.41.4 GR17e1; §9.1.11)
    private long _readOrdinal;           // NOT the FPI: the prior READ's record's release ordinal under the key of
                                         // reference — §14.9.30.4 GR21 e)/f)'s "the record that was made available
                                         // by that prior READ statement" and its "logical position within the set
                                         // of duplicates". Meaningful only while _positioner is 'R'.
    private bool _fpiValid;
    private char _positioner = 'O';      // 'O' OPEN / 'S' START / 'R' READ — selects GR21 rule d) vs. e)/f)
    private string? _lastWrittenPrime;   // sequential-access ascending check (§14.9.51 GR38; EXTEND seeds highest)
    private string? _lastReadPrime;      // sequential-access REWRITE/DELETE target (§14.9.35 GR22 / §14.9.10 GR2)

    /// <summary>The prime record key of the most recently read record — the record-lock identity for §9.1.16
    /// record locking (Phase 4d M2-FILE-1). Null before the first successful READ.</summary>
    public string? LastReadPrime => _lastReadPrime;
    private string? _lastWrittenPrimeId;   // the record released by the last successful Write (§14.9.51 GR11)

    // ── Record-lock identity (ISO §9.1.16) — an indexed record's identity is its PRIME record key ────────────

    /// <inheritdoc/>
    public override string LastReadRecordId => _lastReadPrime ?? "";

    /// <inheritdoc/>  (sequential access targets the last-read record, §14.9.35.4 GR22 / §14.9.10.4 GR2;
    /// random/dynamic the record whose prime key is the record area's key slice, §14.9.35.4 GR23 /
    /// §14.9.10.4 GR3 — the ACCESS MODE alone selects the target, see <see cref="KeyedConnector"/>)
    protected override string MutationTargetRecordIdCore(string recordImage, RecordExtents? recordExtents) =>
        Access == KeyedAccess.Sequential
        ? LastReadRecordId
        : AreaKey(recordImage, recordExtents, PrimeKey);

    /// <inheritdoc/>
    public override string LastWrittenRecordId => _lastWrittenPrimeId ?? "";

    // RECORD IS VARYING: the RecordFraming store carries each record's exact length; key slices pad on demand
    // (KeyOf), so a varying record persists at its written length (§13.18.43 GR13) and reports it on READ
    // (GR15). Out-of-bounds WRITE/REWRITE is the GR14/§14.9.35 GR20 '44'.

    public IndexedConnector(string hostPath, int recordWidth, KeyedAccess access, int primeOffset, int primeLength,
        int varyMin = -1, int varyMax = -1, CobolCollation? primeCollation = null, CobolContiguousLayout? primeLayout = null)
        : base(hostPath, recordWidth, access, varyMin, varyMax)
    {
        _primeOff = primeOffset;
        _primeLen = primeLength;
        _primeCollation = primeCollation;
        _primeLayout = primeLayout;
        _layoutKeys = primeLayout is not null;
    }

    /// <summary>Register one ALTERNATE RECORD KEY's (offset, length, WITH DUPLICATES) geometry (§12.4.5.6), with
    /// its optional §12.4.5.7 collating-weight table (null = native ordinal) and §12.4.5.6.4 GR6 SUPPRESS WHEN
    /// value (null = no suppression), and — for a key a variable-length member precedes — its record type's
    /// <paramref name="layout"/> (kb/Work PB1025).</summary>
    public void AddAlternateKey(int offset, int length, bool duplicates, CobolCollation? collation = null, string? suppress = null,
        CobolContiguousLayout? layout = null)
    {
        _alts.Add((offset, length, duplicates, collation, suppress, layout));
        _layoutKeys |= layout is not null;
    }

    /// <inheritdoc/>
    protected override string DeclaredOrganization => FixedFileAttributes.Indexed;

    /// <summary>§14.9.6.4 GR2 d) — <i>"A file with organization other than sequential, that resides on a mass
    /// storage device."</i> The category is settled by the ORGANIZATION alone, so it needs no medium
    /// determination (kb/Work PB235).</summary>
    public override PhysicalFileCategory Category => PhysicalFileCategory.NonSequential;

    /// <inheritdoc/>
    /// <remarks>§9.1.6 names the prime record key, the alternate record keys, the SUPPRESS WHEN attribute and
    /// "the collating sequence of the keys for indexed files" as fixed attributes of the physical file — the
    /// index structure this connector builds and persists is exactly those. Index 0 is the prime key; the
    /// alternates follow in declaration order, which is the order their key numbers are assigned in, and
    /// §12.4.5.6.4 GR6's key suppression value is an ALTERNATE key's attribute only.
    /// <para>⛔ FOR INDEXED FILES THIS IS NOT ONLY IMPLEMENTOR LATITUDE. §12.4.5.12.4 GR3: "The data description
    /// of data-name-1 or data-name-2 as well as their relative location within a record shall be the same as
    /// that used when the file was created", and §12.4.5.6.4 GR3 says the same of every alternate key AND that
    /// "The number of alternate record keys for the file shall also be the same as that used when the physical
    /// file was created". Those are normative requirements with no consequence stated where they are written;
    /// §14.9.27.4 GR10's file attribute conflict condition is the mechanism that detects a violation, so
    /// putting the key descriptors in the validated set is what gives those two rules an effect.</para>
    /// <para>The recorded descriptor is the key's BYTE WINDOW plus its collating sequence, and that is the whole
    /// of "the data description … as well as their relative location" this implementation can act on:
    /// §12.4.5.12.3 SR2 confines a record key to category alphanumeric or category national,
    /// §12.4.5.12.4 GR1 makes key equality a
    /// relation condition under the file's collating sequence (recorded, via
    /// <see cref="FixedFileAttributes.Fingerprint"/>), and both native sequences are one code-unit ordinal over
    /// the UTF-16 substrate (CONFORMANCE.md DOC-A.1-33/188) — so two descriptions with the same window and the
    /// same sequence order every key value identically, whatever their category.</para></remarks>
    protected override IReadOnlyList<FixedFileAttributes.KeyDescriptor> DeclaredKeys
    {
        get
        {
            var keys = new List<FixedFileAttributes.KeyDescriptor>(_alts.Count + 1)
            {
                new(_primeOff, _primeLen, false, null, FixedFileAttributes.Fingerprint(_primeCollation)),
            };
            foreach (var (off, len, dups, collation, suppress, _) in _alts)
                keys.Add(new(off, len, dups, suppress, FixedFileAttributes.Fingerprint(collation)));
            return keys;
        }
    }

    /// <summary>True when this record's <paramref name="keyIndex"/> alternate key equals that key's SUPPRESS WHEN
    /// value (ISO §12.4.5.6.4 GR6): the alternate access path to the record is NOT provided under this key, and
    /// the record "is not considered to exist" for READ/START (the GR6 NOTE). The comparison is the §14.9.51 GR35
    /// relation condition under this key's collating sequence (null weights = ordinal, <see cref="KeyEq"/>). The
    /// relation's operand SIZING is the compiler's, not this method's: the registered value is already the key's
    /// own image (a figurative or ALL literal repeated to the key's size, a shorter literal space-extended, a
    /// national value in national spaces — <c>DataBinder.KeySuppressionOf</c>, kb/Work PB1072), because only the
    /// compiler knows the literal's form and the key's category. The prime key (keyIndex &lt; 0) is NEVER
    /// suppressible (GR6 scopes suppression to alternate keys).</summary>
    private bool IsSuppressed(string image, RecordExtents? extents, int keyIndex)
    {
        if (keyIndex < 0 || _alts[keyIndex].Suppress is not { } lit) return false;
        return KeyEq(KeyOf(image, extents, keyIndex), lit, keyIndex);
    }

    /// <summary><see cref="IsSuppressed(string, RecordExtents?, int)"/> of a STORED record.</summary>
    private bool IsSuppressed(KeyedRec rec, int keyIndex) => IsSuppressed(rec.Image, rec.Extents, keyIndex);

    /// <summary>Compare two full key values under the key of reference's collating sequence (ISO §12.4.5.7.4 /
    /// §14.9.41 GR17e / §12.4.5.12.4 GR1). <paramref name="keyIndex"/> &lt; 0 (<see cref="PrimeKey"/>) selects
    /// the prime key. With no
    /// COLLATING SEQUENCE clause the key's weights are null and the comparison is native ordinal — byte-identical
    /// to the pre-§12.4.5.7 engine (the NIST-IX baseline), since keys are fixed-length so no space-extension
    /// differs. A declared alphabet routes through the §8.8.4.2.7 weighted relation-condition compare.</summary>
    private int KeyCompare(string a, string b, int keyIndex)
    {
        var c = keyIndex < 0 ? _primeCollation : _alts[keyIndex].Collation;
        if (c is null) return string.CompareOrdinal(a, b);
        // A LOCALE key sequence: compare materialized keys — the file's stored key values are compared on every
        // lookup and insert, and the collator's key cache builds each distinct value's key once (kb/Work PB106).
        if (c.SupportsKeys) return c.KeyOf(a)!.CompareTo(c.KeyOf(b));
        return c.Compare(a, b);
    }

    /// <summary>Key equality under the key of reference's collating sequence: two keys that differ in bytes but
    /// share collating weights are EQUAL (ISO §12.4.5.12.4 GR1 — "based on the collating sequence … according to
    /// the rules for a relation condition"). Drives uniqueness, random match, target lookup, and the '02'
    /// look-ahead.</summary>
    private bool KeyEq(string a, string b, int keyIndex) => KeyCompare(a, b, keyIndex) == 0;

    // ── OPEN / CLOSE (ISO §14.9.27 / §14.9.6) ────────────────────────────────────────────────────────────────

    /// <summary>The indexed OPEN body (§14.9.27). GR14: INPUT/I-O set the FPI to the lowest collating position
    /// and the PRIME key becomes the key of reference; GR15: EXTEND positions after the highest prime key
    /// (seeding the GR38 ascending-sequence check). An absent NON-optional file on INPUT/I-O/EXTEND is '35'
    /// (§9.1.13.6 item 5) — pinned to the spec; the legacy created a missing file on I-O with '00' (brief §2.3
    /// #3, version-invariant). The '41' guard, mode bookkeeping, position reset, the ONE presence probe behind
    /// <paramref name="presence"/>, GR3's authority '37' and the '37'/'30' exception mapping live on
    /// <see cref="FileConnector.Open"/>.</summary>
    protected override string OpenCore(FileOpenMode mode, FilePresence presence)
    {
        _refKey = PrimeKey;                                                 // §14.9.27 GR14 — prime key of reference
        _fpiKey = ""; _readOrdinal = 0; _positioner = 'O';
        _lastWrittenPrime = null;
        _lastReadPrime = null;
        _lastWrittenPrimeId = null;
        // Table 18's availability axis; GR3 has already turned an Unauthorized probe into '37' upstream, so
        // a refusal can never be read here as "unavailable" (kb/Work PB323).
        bool exists = presence is FilePresence.Present;
        string status = FileStatusCode.Success;
        switch (mode)
        {
            case FileOpenMode.Input:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    OptionalAbsent = true;
                    // NO file lock: §9.1.15 establishes one on a PHYSICAL FILE, and there is none here.
                    Attach();                              // empty — the file is absent
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                TakeFileLock(create: false);               // §9.1.15 — the lock, before the store is read through it
                Attach();
                break;
            case FileOpenMode.Output:
            {
                var fs = TakeFileLock(create: true);       // §14.9.27.4 GR18 — OUTPUT creates the physical file
                Attach();
                _st.Clear();                               // OPEN OUTPUT empties the SHARED view (kb/Work PB143)
                _nextOrdinal = 1;
                RecordFraming.WriteStore(fs, DeclaredAttributes, []);
                break;
            }
            case FileOpenMode.IO:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;   // '35' — spec-pinned
                    var io = TakeFileLock(create: true);
                    Attach();
                    _st.Clear();
                    _nextOrdinal = 1;
                    RecordFraming.WriteStore(io, DeclaredAttributes, []);   // §14.9.27 GR17
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                TakeFileLock(create: false);
                Attach();
                break;
            case FileOpenMode.Extend:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    var ex = TakeFileLock(create: true);
                    Attach();
                    _st.Clear();
                    _nextOrdinal = 1;
                    RecordFraming.WriteStore(ex, DeclaredAttributes, []);
                    status = FileStatusCode.OptionalFileNotFound;
                }
                else { TakeFileLock(create: false); Attach(); }
                if (_recs.Count > 0)
                {
                    var ordered = Ordered(PrimeKey);
                    _lastWrittenPrime = KeyOf(ordered[^1], PrimeKey);   // §14.9.51 GR38 — highest existing
                }
                break;
        }
        _fpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        return status;
    }

    /// <summary>Attach the per-physical-file store (kb/Work PB143): the LIVE store when another connector holds
    /// this host open (its content and release-ordinal mint are the truth — never reloaded), else a fresh one loaded
    /// from disk. With no registry table, a private freshly-loaded store — the pre-PB143 shape.</summary>
    private void Attach()
    {
        if (SharedStores is { } t) { _st = t.AttachIndexed(HostPath, Load); return; }
        var s = new IndexedStore();
        Load(s);
        _st = s;
    }

    /// <summary>The indexed CLOSE body (§14.9.6): a writable mode persists the store in <see cref="PersistOrder"/>
    /// — the ONE physical order that reproduces §14.9.30.4 GR26's duplicate retrieval order under EVERY key of
    /// reference after the reload — so the order survives a CLOSE/OPEN cycle and run-unit termination.
    /// The not-open '42' guard lives on <see cref="FileConnector.Close"/>.</summary>
    protected override string CloseCore()
    {
        // A persist IOException maps to '30' on FileConnector.Close (§9.1.13.6 item 1 — the ONE mapping),
        // which ends the open mode either way; ModeKnown then stays true for the USE-declarative scoping.
        // OptionalAbsent (the FPI's "not present" state) survives the CLOSE — §14.9.6.4 GR6 (kb/Work PB140).
        // The DETACH runs whatever the persist outcome (kb/Work PB143): the connector leaves the shared
        // store, and never aliases a detached one.
        try
        {
            bool owed = !OptionalAbsent && Mode is not FileOpenMode.Input;
            // ⛔ A PERSIST THAT CANNOT REACH THE FILE IS NOT A SUCCESSFUL CLOSE (kb/Work PB771). The only way
            // to get here without the handle is a Reposture rebuild whose fallback was also refused, i.e. a
            // foreign process took the file; reporting '00' over records this connector still holds would be
            // the silent loss this note exists to remove.
            if (!PersistIsReachable(owed))
                throw new IOException($"the §9.1.15 file lock on '{HostPath}' was lost while the file was open, "
                    + "so the record store cannot be persisted");
            if (owed && Store is { } fs)
                RecordFraming.WriteStore(fs, DeclaredAttributes, PersistOrder().Select(r => (StoredFrame?)new StoredFrame(r.Image, r.Extents)).ToList(), CodeSet);
        }
        finally
        {
            // §9.1.15 — "The file lock is removed by an explicit or implicit CLOSE statement executed for that
            // file connector", whatever the persist outcome was (kb/Work PB771).
            ReleaseFileLock();
            SharedStores?.Detach(HostPath);
            _st = new IndexedStore();
        }
        ModeKnown = false;   // 9.1.4 - after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
    }

    // ── READ (ISO §14.9.30) ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ [NEXT]</c> in (key-of-reference value, THAT key's release ordinal) order
    /// (§14.9.30.4 GR21 indexed rules d–g; GR26 — duplicates in release order). Sets '02' when the key of
    /// reference is an alternate and the FOLLOWING record duplicates the key just read (GR27 a).</summary>
    public string ReadNext(out string image) => ReadSequential(out image, previous: false);

    /// <summary>Sequential <c>READ PREVIOUS</c> (COBOL-2002+; compiler-gated) — the same walk in reverse
    /// (§14.9.30.4 GR21 d–g apply symmetrically, and GR26 makes a duplicate set's reverse order the reverse of
    /// its release order — the legacy ignored key-of-reference/arrival on PREVIOUS, brief §2.3 #5; this connector
    /// orders both directions identically). Immediately after OPEN → at end at 2023, the first existing record
    /// at 2002/2014 (<see cref="DialectBehavior.IndexedReadPreviousAfterOpenAtEnd"/>,
    /// VERSION_CHANGE_REFERENCE row 29 — kb/Work PB344).</summary>
    public string ReadPrevious(out string image) => ReadSequential(out image, previous: true);

    /// <summary>ISO §14.9.30.4 GR21's "When the file is an indexed file" SELECTION — rules d)/e)/f) over the
    /// key-of-reference ordering, returning the record to be made available (<see langword="null"/> is rule
    /// d) 3/e) 3/f) 3's at-end condition) and its index in <paramref name="seq"/>, which GR27's '02' look-ahead
    /// needs.
    /// <para>⛔ SELECTION ONLY — IT COMMITS NOTHING (kb/Work PB338). Splitting it out of the read is what lets
    /// <see cref="PeekSequentialRecordId"/> answer GR9's "the record identified for access" BEFORE the file
    /// position indicator and the duplicate-set ordinal move, without a second copy of these rules.</para>
    /// </summary>
    private KeyedRec? SelectSequentialRecord(bool previous, List<KeyedRec> seq, out int foundIdx)
    {
        KeyedRec? found = null;
        foundIdx = -1;
        // ⛔ WHICH RULE GOVERNS THIS WALK IS DECIDED BY THE PREVIOUS OPERATION, NOT BY A STORED POSITION
        // (kb/Work PB342). §14.9.30.4 GR21 rule d) applies when the previous operation was an OPEN or a START:
        // the FPI then holds a KEY VALUE and nothing else (§14.9.41.4 GR17 e) 1.), so the comparison is on the
        // key ALONE and a duplicate set is entered at the end §14.9.30.4 GR26 names for the direction —
        // first-released going forward, last-released going backward. Rules e)/f) apply when it was a READ:
        // only then is there a "record that was made available by that prior READ statement" whose "logical
        // position within the set of duplicates" the walk resumes strictly after (or before).
        bool fromRead = _positioner == 'R';
        // ⛔ §14.9.30.4 GR21 d) 3 — "If no such record is found or PREVIOUS is specified and the previous
        // operation on the file was an OPEN statement, the at end condition exists". THAT SENTENCE IS THE 2023
        // RULE, and Annex E.2 item 22 is the informative record that 2023 AMENDED it: "READ PREVIOUS statement
        // following an OPEN statement. Ensure that an at end condition occurs", justified by "the rule itself
        // stated that the first record would be retrieved. The rule itself has been amended such that an at end
        // condition would occur." So at 2002/2014 the after-OPEN case is DIRECTION-BLIND — the first existing
        // record under the key of reference, exactly what NEXT yields and exactly what the relative block's own
        // rule b) still says at every edition. (E.2 also mentions a prior NOTE that said at end normally
        // exists; this document is drafted to the ISO/IEC Directives Part 2, under which a note carries no
        // requirement, so the prior standard's normative content is the rule. And were the prior behaviour
        // already at end, E.2 — the list of SUBSTANTIVE CHANGES potentially affecting existing programs —
        // would have no item 22 to carry.) The edition is the COMPILING program's, baked on the connector at
        // registration; kb/Work PB344, VERSION_CHANGE_REFERENCE row 29.
        if (previous && _positioner == 'O'
            && DialectBehaviors.IsActive(DialectBehavior.IndexedReadPreviousAfterOpenAtEnd, Edition))
            found = null;
        else if (_positioner == 'O')
        {
            // §14.9.27 GR14 — OPEN INPUT/I-O positions the FPI at the LOWEST record in the key-of-reference's
            // collating sequence, so the first READ NEXT yields it — and, at 2002/2014, so does the first READ
            // PREVIOUS, which is why this arm is reached for BOTH directions there (the gate above).
            // Read it off the ordered sequence directly
            // rather than comparing against the empty-string OPEN sentinel: under a §12.4.5.7 alphabet where the
            // pad SPACE weighs high, that sentinel is NOT the lowest value, so a PositionCompare walk would find
            // nothing (the collating-sequence AT-END bug). Ordinal order is unaffected — seq[0] is still lowest.
            if (seq.Count > 0) { found = seq[0]; foundIdx = 0; }
        }
        else if (!previous)
        {
            for (int i = 0; i < seq.Count; i++)
            {
                int c = PositionCompare(seq[i]);
                if (c > 0 || (!fromRead && c == 0)) { found = seq[i]; foundIdx = i; break; }
            }
        }
        else
        {
            for (int i = seq.Count - 1; i >= 0; i--)
            {
                int c = PositionCompare(seq[i]);
                if (c < 0 || (!fromRead && c == 0)) { found = seq[i]; foundIdx = i; break; }
            }
        }
        return found;

        int PositionCompare(KeyedRec rec)
        {
            int c = KeyCompare(KeyOf(rec, _refKey), _fpiKey, _refKey);
            // ⛔ The duplicate-set tie-break is consulted ONLY under rules e)/f). Under rule d) the position IS
            // the key value (§14.9.41.4 GR17 e) 1.) and consulting a stored ordinal would resume the walk at
            // whichever duplicate the START happened to stop on — losing the rest of the set (kb/Work PB342).
            return c != 0 || !fromRead ? c : Ordinal(rec, _refKey).CompareTo(_readOrdinal);
        }
    }

    /// <inheritdoc/>
    /// <remarks>The indexed organization's §14.9.30.4 GR9 pre-read conflict target: the PRIME key of the record
    /// <see cref="SelectSequentialRecord"/> names, which is this organization's lock identity (§9.1.16). The
    /// remaining precondition beyond <c>SequentialReadReachesRetrieval</c> is a valid file position indicator
    /// (a failed START leaves none — §14.9.41.4 GR7).</remarks>
    public override string PeekSequentialRecordId(bool previous) =>
        SequentialReadReachesRetrieval && _fpiValid
        && SelectSequentialRecord(previous, Ordered(_refKey), out _) is { } found
            ? KeyOf(found, PrimeKey)
            : "";

    /// <inheritdoc/>
    /// <remarks>§14.9.30.4 GR32's lookup, run WITHOUT assigning the key of reference — GR10 d) requires it
    /// UNCHANGED when the record operation conflict condition arises, and <see cref="ReadRandom"/> assigns
    /// <c>_refKey</c> as its first act (kb/Work PB338).</remarks>
    public override string PeekRandomReadRecordId(int keyIndex, string recordImage, RecordExtents? recordExtents)
    {
        if (ReadOpenModeGuard() is not null || OptionalAbsent) return "";
        return FindRandom(keyIndex, AreaKey(recordImage, recordExtents, keyIndex)) is { } found ? KeyOf(found, PrimeKey) : "";
    }

    /// <summary>ISO §14.9.30.4 GR32's record identification for a random READ — the first record whose key of
    /// reference equals <paramref name="value"/>, and among duplicate alternates <i>"the first record in a
    /// sequence of duplicates that was released to the operating environment"</i> (the smallest release ordinal
    /// under THAT key); §12.4.5.6.4 GR6's SUPPRESS WHEN records do not exist for the walk. ⛔ SELECTION ONLY —
    /// it establishes no key of reference and moves no position (kb/Work PB338).</summary>
    private KeyedRec? FindRandom(int keyIndex, string value)
    {
        KeyedRec? found = null;
        foreach (var rec in _recs)
            if (!IsSuppressed(rec, keyIndex) && KeyEq(KeyOf(rec, keyIndex), value, keyIndex)
                && (found is null || Ordinal(rec, keyIndex) < Ordinal(found, keyIndex)))
                found = rec;
        return found;
    }

    private string ReadSequential(out string image, bool previous)
    {
        image = new string(' ', RecordWidth);
        if (SequentialReadGuard() is { } pre) return Status = pre;   // '47'/'46'/'10' — FileConnector
        if (!_fpiValid) return Status = FileStatusCode.NoValidNextRecord;

        var seq = Ordered(_refKey);
        if (SelectSequentialRecord(previous, seq, out int foundIdx) is not { } found)
        {
            LastReadUnsuccessful = true;
            _fpiValid = false;                                             // §14.9.30 GR24b
            return Status = FileStatusCode.AtEnd;
        }
        // §14.9.30 GR27 — '02' look-ahead: alternate key of reference and the adjacent record duplicates it.
        string status = FileStatusCode.Success;
        if (_refKey >= 0)
        {
            KeyedRec? adjacent = previous
                ? (foundIdx > 0 ? seq[foundIdx - 1] : null)
                : (foundIdx + 1 < seq.Count ? seq[foundIdx + 1] : null);
            if (adjacent is not null && KeyEq(KeyOf(adjacent, _refKey), KeyOf(found, _refKey), _refKey))
                status = FileStatusCode.DuplicateAlternateKey;
        }
        _fpiKey = KeyOf(found, _refKey);                             // GR21 rule g — a KEY VALUE
        _readOrdinal = Ordinal(found, _refKey);                            // GR21 e)/f) — where in its set of duplicates
        _fpiValid = true; _positioner = 'R';
        _lastReadPrime = KeyOf(found, PrimeKey);
        NoteRecordRead(found.Image, found.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Image);
        return ReadSucceeded(status);
    }

    /// <summary>Format-2 random READ (§14.9.30 GR30–GR32): <paramref name="keyIndex"/> becomes the key of
    /// reference (persisting for subsequent dynamic sequential reads, GR30/GR31); the key VALUE is sliced from
    /// the record area image (<paramref name="keyedRecordImage"/> — GR32, the program stored it there). Among
    /// duplicate alternates the FIRST RELEASED record is made available (GR32 — the smallest release ordinal
    /// under THAT key); no record →
    /// invalid key '23'; an absent optional file → '23' (GR28).</summary>
    public string ReadRandom(int keyIndex, string keyedRecordImage, out string image, RecordExtents? areaExtents = null)
    {
        image = new string(' ', RecordWidth);
        if (ReadOpenModeGuard() is { } notOpen) return Status = notOpen;                  // '47' §14.9.30.4 GR2
        _refKey = keyIndex;                                                // GR30/GR31
        if (RandomReadAbsentOptionalGuard() is { } absent) return Status = absent;        // '23' §9.1.13.5 3 b)
        string value = AreaKey(keyedRecordImage, areaExtents, keyIndex);
        if (FindRandom(keyIndex, value) is not { } found)   // §14.9.30.4 GR32 + §12.4.5.6.4 GR6 — the ONE copy
        {
            LastReadUnsuccessful = true;
            return Status = FileStatusCode.RecordNotFound;                 // '23' GR32
        }
        // GR32 sets the FPI "to the value in the key of reference" — the key VALUE the program supplied; the
        // duplicate-set position of the record made available is separate state (GR21 e)/f), kb/Work PB342).
        _fpiKey = value; _readOrdinal = Ordinal(found, keyIndex); _fpiValid = true; _positioner = 'R';
        _lastReadPrime = KeyOf(found, PrimeKey);
        NoteRecordRead(found.Image, found.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Image);
        return ReadSucceeded(FileStatusCode.Success);
    }

    // ── WRITE / REWRITE / DELETE (ISO §14.9.51 / §14.9.35 / §14.9.10) ───────────────────────────────────────

    /// <summary>WRITE (§14.9.51 GR34–GR42): sequential access requires strictly ascending prime keys ('21',
    /// GR38/GR42a); a duplicate prime key → '22' (GR36/GR42b); a duplicate no-DUPLICATES alternate → '22'
    /// (GR40/GR42c); a permitted duplicate alternate succeeds with '02' (§9.1.13.2 2c). The released record takes
    /// the next release ordinal under EVERY key at once, so sequential retrieval order is the actual write order
    /// (GR40). Open-mode legality per §9.1.13.7 item 8.</summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is written
    /// (determination D-FRA (v); kb/Work PB1053): stored and persisted with it, and what locates a key the record's
    /// variable-length members precede.</param>
    public string Write(string image, int length = -1, RecordExtents? extents = null)
    {
        if (Stored(image, length) is not { } stored)
            return Status = FileStatusCode.RecordSizeViolation;            // '44' §13.18.43 GR14a
        if (RecordHasCharacterWithoutByteImage(stored))
            return Status = FileStatusCode.CharacterWithoutByteImage;      // '91' A.1 item 31 (R47)
        extents = Framed(stored, image, extents).Extents;
        image = _layoutKeys ? stored : Fit(image);   // key slices come from the record-area image (KeyOf pads on demand)
        // §14.9.51.4 GR38 "If the access mode of the write file connector is sequential, records shall be
        // released … in ascending order of prime record key values" vs. GR39 "If the access mode … is random
        // or dynamic, WRITE statements may release records … in any order": the ACCESS MODE alone selects the
        // ordering rule (the open mode only seeds GR38's first key — the highest existing one under extend).
        // ⛔ The open mode is NOT a disjunct here (kb/Work PB325): a random- or dynamic-access connector open
        // in the extend mode is illegal source (§14.9.27.3 SR2) but a REACHABLE runtime state, and Table 20
        // leaves its WRITE cell blank — item 8 b) is what it must answer, not GR38's append.
        bool sequentialRelease = Access == KeyedAccess.Sequential;
        if (sequentialRelease)
        {
            if (!IsOpen || Mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' §9.1.13.7 8a
        }
        // §9.1.13.7 8 b) — "If the access mode is dynamic or random, the file connector is not open in the
        // I-O or output mode"; Table 20's Random/Extend and Dynamic/Extend WRITE cells are blank.
        else if (!IsOpen || Mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' §9.1.13.7 8b
        string prime = KeyOf(image, extents, PrimeKey);
        if (sequentialRelease && _lastWrittenPrime is { } lastPrime && KeyCompare(prime, lastPrime, PrimeKey) <= 0)
            return Status = FileStatusCode.SequenceError;                  // '21' GR38/GR42a
        if (_recs.Any(r => KeyEq(KeyOf(r, PrimeKey), prime, PrimeKey)))
            return Status = FileStatusCode.DuplicateKey;                   // '22' GR36/GR42b
        bool duplicateAlt = false;
        for (int i = 0; i < _alts.Count; i++)
        {
            if (IsSuppressed(image, extents, i)) continue;   // §14.9.51 GR41 — a suppressed alternate key provides no access path and never duplicates
            string value = KeyOf(image, extents, i);
            bool exists = _recs.Any(r => !IsSuppressed(r, i) && KeyEq(KeyOf(r, i), value, i));
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;   // '22' GR40/GR42c
            if (exists) duplicateAlt = true;
        }
        // §14.9.51.4 GR42 d) — "When the record that is to be released to the operating environment would reside
        // outside the externally defined boundaries of the physical file, the I-O status … is set to '24'":
        // the store's capacity (Annex A.1 item 107, KeyedConnector.StoreHolds), tested before the release.
        if (!StoreHolds(_st.RecordBytes + IndexedStore.FrameBytes(stored, extents)))
            return Status = FileStatusCode.BoundaryViolation;              // '24' GR42d
        // §14.9.51.4 GR40 — the WRITE RELEASES the record, so it is positioned last in the duplicate set of
        // EVERY key at once: one fresh ordinal stamped into every slot (the prime slot doubles as the record's
        // release order in the physical file — see KeyedRec.Ordinals).
        _st.Add(new KeyedRec { Image = stored, Extents = extents, Ordinals = ReleaseOrdinals(_nextOrdinal++) });
        if (sequentialRelease) _lastWrittenPrime = prime;   // GR38's running "highest … written" — sequential access only
        _lastWrittenPrimeId = prime;   // §9.1.16 lock identity of the record just released (§14.9.51 GR11)
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;
    }

    /// <summary>REWRITE (§14.9.35): open mode I-O ('49'); sequential access requires the previous op to be a
    /// successful READ ('43', GR5) AND the prime key to equal the last-read prime ('21', GR22/GR25a);
    /// random/dynamic an existing prime ('23', GR23/GR25b); a no-DUPLICATES alternate conflict with ANOTHER
    /// record → '22' (GR25c); a CHANGED alternate key — and ONLY a changed one, GR24 a) — repositions the record
    /// LAST in that key's duplicate set (GR24 b: it takes the next release ordinal under that key alone); a
    /// duplicate key value CREATED under a permitted-duplicates alternate → '02' (§9.1.13.2 2 c).</summary>
    /// <param name="extents">The replacing record's EXTENT TABLE (D-FRA (v); kb/Work PB1053), replaced with it.</param>
    public string Rewrite(string image, int length = -1, RecordExtents? extents = null)
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        // §14.9.35 GR18 — an indexed record's size MAY differ from the replaced record's; GR20 still bounds it.
        if (Stored(image, length) is not { } stored)
            return Status = FileStatusCode.RecordSizeViolation;                                 // '44' GR20
        if (RecordHasCharacterWithoutByteImage(stored))
            return Status = FileStatusCode.CharacterWithoutByteImage;                           // '91' (R47)
        extents = Framed(stored, image, extents).Extents;
        image = _layoutKeys ? stored : Fit(image);   // the WRITE's key image rule (kb/Work PB1025)
        string prime = KeyOf(image, extents, PrimeKey);
        if (Access == KeyedAccess.Sequential)   // §14.9.35.4 GR22 vs. GR23 — the ACCESS MODE alone
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43' GR5
            // '21' §14.9.35 GR22 — the prime key of the replaced record must EQUAL that of the last record read;
            // equality is collating-sequence-based per §12.4.5.12.4 GR1 (KeyEq honors _primeCollation), not ordinal.
            if (_lastReadPrime is not { } lastPrime || !KeyEq(prime, lastPrime, PrimeKey))
                return Status = FileStatusCode.SequenceError;
        }
        KeyedRec? target = _recs.FirstOrDefault(r => KeyEq(KeyOf(r, PrimeKey), prime, PrimeKey));
        if (target is null) return Status = FileStatusCode.RecordNotFound;                      // '23' GR23
        // ⛔ TWO PASSES, AND THE ORDER MATTERS. Pass 1 VALIDATES ONLY: an early '22' leaves the REWRITE
        // unsuccessful, so nothing may have been repositioned by then. Pass 2 applies GR24's repositioning, and
        // both run while target.Image is still the REPLACED record — AltChanged compares against it.
        bool duplicateAlt = false;
        for (int i = 0; i < _alts.Count; i++)
        {
            // §12.4.5.6.4 GR6 — a key whose value equals its SUPPRESS WHEN value provides no access path, so it
            // is skipped for the uniqueness check (it can never duplicate, §14.9.51.4 GR41 by parity with WRITE).
            if (IsSuppressed(image, extents, i)) continue;
            string newValue = KeyOf(image, extents, i);
            bool exists = _recs.Any(r => !ReferenceEquals(r, target) && !IsSuppressed(r, i) && KeyEq(KeyOf(r, i), newValue, i));
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;          // '22' GR25c
            // §9.1.13.2 2 c) — '02' is "the record just written CREATED a duplicate key value": a key whose
            // value and suppression state this REWRITE did not change duplicated before the statement ran too,
            // so it created nothing (kb/Work PB341).
            if (exists && AltChanged(i)) duplicateAlt = true;
        }
        // §9.1.13.5 item 4 — a replacing record the store cannot hold (a longer record, §14.9.35.4 GR18) is an
        // attempt to write outside the file's externally-defined boundaries: '24', still inside the validation
        // pass, so nothing below has been repositioned (Annex A.1 item 107).
        if (!StoreHolds(_st.RecordBytes + IndexedStore.FrameBytes(stored, extents) - IndexedStore.FrameBytes(target.Image, target.Extents)))
            return Status = FileStatusCode.BoundaryViolation;                                   // '24'
        // §14.9.35.4 GR24 a) — "When the value of a specific alternate record key is not changed, the order of
        // retrieval when that key is the key of reference remains unchanged" — so ONLY the keys this REWRITE
        // actually changed are re-stamped; b) puts the record "last within the set of duplicate records" of
        // each of those, which one ordinal off the monotone mint does for all of them at once.
        long repositioned = 0;
        for (int i = 0; i < _alts.Count; i++)
        {
            if (!AltChanged(i)) continue;
            if (repositioned == 0) repositioned = _nextOrdinal++;
            Stamp(target, i, repositioned);                                                     // GR24 b)
        }
        _st.Replace(target, stored, extents);
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;

        // The ONE definition of "this REWRITE changed alternate key i" that GR24 a)/b), GR24's SUPPRESS WHEN
        // sub-rules and §9.1.13.2 2 c) all key off. §14.9.35.4 GR24's closing sentence — "The comparison used for
        // determining changes to the key is based on the collating sequence for the file according to the rules
        // for a relation condition" — makes it KeyEq, not an ordinal string compare; entering OR leaving
        // suppression is a change of its own (GR24's two SUPPRESS WHEN sub-rules both reposition).
        bool AltChanged(int i) =>
            !KeyEq(KeyOf(image, extents, i), KeyOf(target, i), i)
            || IsSuppressed(image, extents, i) != IsSuppressed(target, i);
    }

    /// <summary>DELETE RECORD (§14.9.10): open mode I-O ('49', GR1); sequential access removes the prior READ's
    /// record ('43' without one, GR2); random/dynamic removes the record whose PRIME key equals the prime key
    /// item's content (sliced from the record area image — GR3; absent → invalid key '23'). The FPI is unaffected
    /// (GR9) — the position survives because it is held as values (key + release ordinal) the next READ
    /// re-derives from, not as a reference to the deleted record.</summary>
    public string Delete(string keyedRecordImage, RecordExtents? areaExtents = null)
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        string prime;
        if (Access == KeyedAccess.Sequential)   // §14.9.10.4 GR2 vs. GR3 — the ACCESS MODE alone
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
            prime = _lastReadPrime ?? "";
        }
        else
            prime = AreaKey(keyedRecordImage, areaExtents, PrimeKey);
        KeyedRec? target = _recs.FirstOrDefault(r => KeyEq(KeyOf(r, PrimeKey), prime, PrimeKey));
        if (target is null) return Status = FileStatusCode.RecordNotFound;
        _st.Remove(target);
        return Status = FileStatusCode.Success;
    }

    // ── START (ISO §14.9.41) ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>START KEY rel-op (GR16/GR17): the comparison takes the leftmost <paramref name="compareLength"/>
    /// characters of each stored key (the GR17 temporary-area partial-key model — a generic/shorter operand or
    /// the 2002+ WITH LENGTH count). An out-of-range length → '23' (GR14). Forward search (=, &gt;, &gt;=) takes
    /// the FIRST record in (key value, release ordinal) order satisfying the comparison; reverse (&lt;, &lt;=) the LAST — the
    /// indexed analogue of the relative GR9b reverse search. Success establishes <paramref name="keyIndex"/> as
    /// the key of reference for subsequent sequential READs (GR16); failure invalidates the FPI and leaves the
    /// key of reference undefined (GR7).</summary>
    /// <param name="keyedRecordImage">⛔ THE RECORD AREA, NOT data-name-1's own content (kb/Work PB355).
    /// §14.9.41.4 GR17 a) — "The specified key is set up by moving the relevant parts of the record area into a
    /// temporary data area" — names the record area as the SOURCE and GR16's key of reference as the key, so the
    /// first temporary area is <see cref="KeyOf"/> the area under <paramref name="keyIndex"/>, truncated to the
    /// GR17 b) length the caller passes as <paramref name="compareLength"/>. That is the SAME extraction the
    /// random READ (§14.9.30.4 GR32) and DELETE (§14.9.10.4 GR3) make from the same image; taking data-name-1's
    /// own bytes and PADDING them to the count instead made a LENGTH that reaches past the operand search for
    /// invented spaces where the record area holds the rest of the key.</param>
    /// <param name="length">The GR17 b) length EXACTLY as arithmetic-expression-1 produced it (kb/Work PB357) —
    /// GR14's positive-nonzero-integer-within-the-key predicate is asked of it HERE, once, and never of a value an
    /// emit-time narrowing has already forced into range.</param>
    public string Start(int keyIndex, string op, string keyedRecordImage, StartKeyLength length,
        RecordExtents? areaExtents = null)
    {
        if (StartOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();                           // '23' GR5
        int keyLength = keyIndex < 0 ? _primeLen : _alts[keyIndex].Len;
        if (!length.TryCompareLength(keyLength, out int compareLength)) return StartFail();   // '23' GR14
        // GR17 a)/b) — the first temporary area. KeyOf pads the area to the key's own span, so the slice is
        // always in range once GR14 above has bounded compareLength by that span.
        string value = AreaKey(keyedRecordImage, areaExtents, keyIndex)[..compareLength];
        var seq = Ordered(keyIndex);
        KeyedRec? found = null;
        bool forward = op is "==" or ">" or ">=";
        for (int i = 0; i < seq.Count; i++)
        {
            var rec = seq[forward ? i : seq.Count - 1 - i];
            string part = KeyOf(rec, keyIndex)[..compareLength];
            int c = KeyCompare(part, value, keyIndex);   // §14.9.41 GR17e — the file's collating sequence
            bool satisfied = op switch
            {
                "==" => c == 0,
                ">" => c > 0,
                ">=" => c >= 0,
                "<" => c < 0,
                "<=" => c <= 0,
                _ => false,
            };
            if (satisfied) { found = rec; break; }
        }
        if (found is null) return StartFail();                             // '23' — comparison not satisfied
        // GR16 — "The key specified in the KEY phrase … becomes the key of reference", and GR17 e) 1. sets the
        // file position indicator to ITS value in the record the comparison stopped on.
        return StartSucceeded(keyIndex, found);
    }

    /// <summary>START FIRST/LAST (COBOL-2002+) on an indexed file: §14.9.41.4 GR18 — "If FIRST is specified, the
    /// file position indicator is set to the value of the primary key of the first existing logical record in the
    /// physical file and the key of reference is set to the primary key" — and GR19, the same for LAST and the
    /// last existing record. An empty or absent-optional file → invalid key '23' (GR18/GR19's own second
    /// sentence).</summary>
    /// <remarks>⛔ THE PRIME KEY IS BOTH THE ORDERING AND THE OUTCOME (kb/Work PB356). GR18/GR19 name the primary
    /// key twice over: the record selected is the first/last one under the PRIME ordering, and the key of
    /// reference the statement leaves behind for every subsequent sequential READ (GR16's last sentence,
    /// §14.9.30.4 GR21 b)'s "otherwise") is the prime key too. Walking the INHERITED key of reference instead
    /// — which this method used to do, justified by §14.9.27.4 GR14, a real clause about what OPEN establishes
    /// and not about what START FIRST/LAST does — answered LAST with the alternate ordering's last record and
    /// left every following READ NEXT walking that alternate; and where the alternate carries SUPPRESS WHEN
    /// (§12.4.5.6.4 GR6) it additionally hid records the primary-key view must include.</remarks>
    public string StartFirstLast(bool last)
    {
        if (StartOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent || _recs.Count == 0) return StartFail();
        var seq = Ordered(PrimeKey);   // GR18/GR19 — "in the physical file": the PRIME ordering, not the inherited one
        return StartSucceeded(PrimeKey, last ? seq[^1] : seq[0]);
    }

    /// <summary>The epilogue of EVERY successful START on an indexed file (§14.9.41.4): the format's own
    /// key-of-reference decision, then the file position indicator set to THAT key's value in the selected
    /// record.</summary>
    /// <remarks>⛔ <paramref name="keyOfReference"/> IS A REQUIRED ARGUMENT so that no START format can leave the
    /// previous one standing (kb/Work PB356). Each format decides it and the standard says so separately — GR16
    /// for the KEY phrase, GR18/GR19's "the key of reference is set to the primary key" for FIRST/LAST — so an
    /// epilogue that read <c>_refKey</c> instead would silently inherit whatever the last START or random READ
    /// established. The file position indicator is a KEY VALUE and nothing else (§14.9.41.4 GR17 e) 1.,
    /// kb/Work PB342): which record of a duplicate set the search stopped on is NOT recorded, so the following
    /// READ enters the set at the end §14.9.30.4 GR26 names for ITS direction — which is why
    /// <c>_readOrdinal</c> is deliberately untouched here and <c>_positioner</c> becomes 'S'.</remarks>
    private string StartSucceeded(int keyOfReference, KeyedRec found)
    {
        _refKey = keyOfReference;
        _fpiKey = KeyOf(found, keyOfReference);
        _fpiValid = true; _positioner = 'S';
        LastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    /// <inheritdoc/>  (a keyed FPI is a key VALUE plus a validity bit; §14.9.41.4 GR7 clears the bit too,
    /// and its second sentence leaves the key of reference undefined, so _refKey needs no reset)
    protected override void InvalidateFilePosition()
    {
        _fpiValid = false;
        base.InvalidateFilePosition();   // → '46' on the next sequential READ (§9.1.13.7 item 6 a))
    }

    /// <summary>An unsuccessful START whose status is the invalid key condition's '23'
    /// (§9.1.13.5 item 3): §14.9.41.4 GR7's invalidation plus that value.</summary>
    private string StartFail()
    {
        InvalidateFilePosition();
        return Status = FileStatusCode.RecordNotFound;
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The records ordered by (key-of-reference value, THAT KEY'S release ordinal) under the key of
    /// reference's collating sequence (ISO §12.4.5.5.3 GR2c) — derived per call from the one record list (the
    /// legacy's lesson: a maintained clone index goes stale across REWRITE/DELETE). With no §12.4.5.7 COLLATING
    /// clause the key's weights are null and the ordering is native ordinal, byte-identical to the pre-clause
    /// engine. The tie-break is <see cref="Ordinal"/> for THIS key, never a per-record number: §14.9.30.4 GR26's
    /// release order is a property of the key of reference (kb/Work PB341).</summary>
    private List<KeyedRec> Ordered(int keyIndex) =>
        [.. _recs.Where(r => !IsSuppressed(r, keyIndex))   // §12.4.5.6.4 GR6 — suppressed records are not provided under this key
                 .OrderBy(r => KeyOf(r, keyIndex),
                Comparer<string>.Create((x, y) => KeyCompare(x, y, keyIndex))).ThenBy(r => Ordinal(r, keyIndex))];

    // ── The per-key release-ordinal model (ISO §14.9.30.4 GR26, §14.9.35.4 GR24; kb/Work PB341) ──────────────

    /// <summary>The ordinal slot for a key index: 0 the prime key, <c>i + 1</c> the i-th alternate. A record
    /// stored by a connector that declared FEWER keys reads 0 for the missing slots — unreachable while the
    /// §12.4.5.6.4 GR3 key count is a fixed file attribute the §14.9.27.4 GR10 conflict check enforces ('39'),
    /// and a total order either way.</summary>
    private static long Ordinal(KeyedRec rec, int keyIndex)
    {
        int slot = keyIndex + 1;
        return (uint)slot < (uint)rec.Ordinals.Length ? rec.Ordinals[slot] : 0;
    }

    /// <summary>Stamp <paramref name="keyIndex"/>'s release ordinal, growing the vector if this connector
    /// declares keys the stored record predates.</summary>
    private void Stamp(KeyedRec rec, int keyIndex, long ordinal)
    {
        int slot = keyIndex + 1;
        if (slot >= rec.Ordinals.Length) Array.Resize(ref rec.Ordinals, _alts.Count + 1);
        rec.Ordinals[slot] = ordinal;
    }

    /// <summary>A freshly released record's ordinal vector: ONE ordinal in every slot — a WRITE releases the
    /// record under every key at the same instant (§14.9.51.4 GR40), and a load makes the physical file order
    /// the release order under every key (§14.9.30.4 GR26).</summary>
    private long[] ReleaseOrdinals(long ordinal)
    {
        var ordinals = new long[_alts.Count + 1];
        Array.Fill(ordinals, ordinal);
        return ordinals;
    }

    /// <summary>The ONE physical order to persist the store in at CLOSE.
    /// <para>A reload can only give every key the file's own order, so the file has to be written in an order
    /// that is simultaneously each key's §14.9.30.4 GR26 release order within each of ITS duplicate sets. Those
    /// per-key orders are independent (§14.9.35.4 GR24 a) keeps an untouched key's order while b) moves the
    /// record last under a changed one), so this is a topological sort: one edge per adjacent pair of a
    /// duplicate set, Kahn's algorithm, ties broken by the prime-key ordinal — the record's own release order,
    /// which is never re-stamped. With no REWRITE repositioning every key's order already IS release order, so
    /// the result is exactly release order and the on-disk shape is unchanged.</para>
    /// <para>⛔ RESIDUE: the per-key orders can be made mutually CYCLIC (rewrite one record's key A into another
    /// record's duplicate set while both stay duplicates under key B), and then NO single sequence of record
    /// images can carry them — the physical format has one order and the model has one per key. Those records
    /// are appended in release order, which loses the repositioning of at least one key. Carrying them all needs
    /// the per-key ordinals in the physical file, which this framing has no slot for.</para></summary>
    private List<KeyedRec> PersistOrder()
    {
        int n = _recs.Count;
        var order = new List<KeyedRec>(n);
        int[] byRelease = [.. Enumerable.Range(0, n).OrderBy(i => Ordinal(_recs[i], PrimeKey))];
        if (n < 2 || _alts.Count == 0)
        {
            foreach (int i in byRelease) order.Add(_recs[i]);
            return order;
        }
        // KeyedRec overrides neither Equals nor GetHashCode, so the default comparer IS reference identity.
        var slotOf = new Dictionary<KeyedRec, int>(n);
        for (int i = 0; i < n; i++) slotOf[_recs[i]] = i;
        var successors = new List<int>?[n];
        var indegree = new int[n];
        for (int k = 0; k < _alts.Count; k++)
        {
            var seq = Ordered(k);   // this key's retrieval sequence — the ONE place that ordering is written
            for (int j = 1; j < seq.Count; j++)
            {
                if (KeyCompare(KeyOf(seq[j - 1], k), KeyOf(seq[j], k), k) != 0)
                    continue;   // a set boundary constrains nothing — key order alone decides across sets
                int before = slotOf[seq[j - 1]], after = slotOf[seq[j]];
                (successors[before] ??= []).Add(after);
                indegree[after]++;
            }
        }
        var ready = new PriorityQueue<int, long>();
        foreach (int i in byRelease)
            if (indegree[i] == 0) ready.Enqueue(i, Ordinal(_recs[i], PrimeKey));
        var placed = new bool[n];
        while (ready.TryDequeue(out int i, out _))
        {
            order.Add(_recs[i]);
            placed[i] = true;
            if (successors[i] is not { } next) continue;
            foreach (int j in next)
                if (--indegree[j] == 0) ready.Enqueue(j, Ordinal(_recs[j], PrimeKey));
        }
        if (order.Count < n)
            foreach (int i in byRelease)
                if (!placed[i]) order.Add(_recs[i]);   // the cyclic residue — see the remark above
        return order;
    }

    /// <summary>The key value at <paramref name="keyIndex"/> (−1 = prime) — an (offset, length) slice of the
    /// record's character image (§12.4.5.12 GR2 — the key IS its position range in the record). A key that
    /// follows a variable-length member is found in THIS record through its record type's layout (kb/Work PB1025;
    /// docs/CONFORMANCE.md §3 D-KWV): the image is contiguous (§8.5.1.11.2), so its position moves with the
    /// preceding members' current lengths — read from the record's own EXTENT TABLE when it carries one that
    /// describes it (<paramref name="extents"/>; D-FRA (v), kb/Work PB1053), by the take step otherwise, exactly as
    /// a READ of the record decomposes it.</summary>
    private string KeyOf(string image, RecordExtents? extents, int keyIndex)
    {
        var (off, len, layout) = keyIndex < 0 ? (_primeOff, _primeLen, _primeLayout)
            : (_alts[keyIndex].Off, _alts[keyIndex].Len, _alts[keyIndex].Layout);
        if (layout is not null) off = layout.Position(image, off, extents);
        if (image.Length < off + len) image = image.PadRight(off + len, ' ');
        return image.Substring(off, len);
    }

    /// <summary>The key value in the RECORD AREA image a statement supplies (§14.9.30.4 GR32, §14.9.10.4 GR3,
    /// §14.9.41.4 GR17 a)): fitted to the area width, as the record area is — except on a connector whose key
    /// positions vary with the record, where the image is taken as composed, because truncating it would move
    /// the key (<see cref="_layoutKeys"/>).</summary>
    private string AreaKey(string areaImage, RecordExtents? areaExtents, int keyIndex) =>
        KeyOf(_layoutKeys ? areaImage : Fit(areaImage), _layoutKeys ? areaExtents : null, keyIndex);

    /// <summary>The key value of a STORED record — <see cref="KeyOf(string, RecordExtents?, int)"/> over its image
    /// and the extent table it was released with.</summary>
    private string KeyOf(KeyedRec rec, int keyIndex) => KeyOf(rec.Image, rec.Extents, keyIndex);

    private void Load(IndexedStore into)
    {
        into.Clear();
        into.NextOrdinal = 1;
        // ⛔ NO SECOND PRESENCE QUESTION, AND NO SECOND HANDLE (kb/Work PB771) — see RelativeConnector.Load:
        // the load reads the store through the connector's OWN handle, its §9.1.15 file lock, and the handle's
        // existence is the answer this used to ask HostFile.Probe for a second time in the same OPEN.
        if (Store is not { } fs) return;
        // A varying file's frames keep their exact stored lengths (§13.18.43 GR15 reports them on READ);
        // fixed frames normalize to the record width.
        foreach (StoredFrame? stored in RecordFraming.ReadStore(fs, CodeSet))
            if (stored is { } frame)
                // The physical file order IS the release order under every key (§14.9.30.4 GR26) — PersistOrder
                // wrote it that way, so one ordinal per record fills the whole vector (kb/Work PB341).
                into.Add(new KeyedRec
                {
                    Image = IsVarying ? frame.Image : Fit(frame.Image),
                    Extents = IsVarying ? frame.Extents : null,   // a fitted fixed record is not the image a table describes
                    Ordinals = ReleaseOrdinals(into.NextOrdinal++),
                });
    }
}
