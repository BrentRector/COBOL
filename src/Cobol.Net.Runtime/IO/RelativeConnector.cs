// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native RELATIVE-organization file connector (ISO/IEC 1989:2023 §9.1.7.3): a sparse 1-based slot model —
/// each record is identified by its relative record number (RRN); slots between records "do not exist" for READ.
/// The behavioral state machine ports the legacy <c>RelativeFileHandler</c> (NIST RL-suite-proven) onto the
/// character-image substrate: the file position indicator (§9.1.11) is a slot number + an inclusive flag (set by
/// OPEN/START, advanced exclusively by READ), '46' poisoning after an unsuccessful READ/START (§14.9.30 GR21 /
/// §9.1.13.7 item 6), and the '43' previous-op-was-READ rule for sequential-access REWRITE/DELETE (§14.9.35 GR5 /
/// §14.9.10 GR2). Records cross this boundary as their character image (a <see cref="string"/>).
/// </summary>
public sealed class RelativeConnector : KeyedConnector
{
    private readonly int _keyDigits;   // RELATIVE KEY digit capacity (0 = no RELATIVE KEY clause)

    /// <summary>The attached PER-PHYSICAL-FILE store (kb/Work PB143): every connector over one host path sees
    /// ONE slot dictionary, so a DELETE through one is a deletion for all and the close order cannot pick a
    /// surviving view. A placeholder until OPEN attaches (and again after CLOSE detaches).</summary>
    private RelativeStore _st = new();

    /// <summary>The sparse slot store: RRN (1-based, §12.4.5.13 GR1) → record image — the ATTACHED store's,
    /// READ-ONLY. Every mutation goes through the store's own Put/Remove/Clear, which maintain its
    /// high-water mark; there is deliberately no second way to change it (kb/Work PB739).</summary>
    private IReadOnlyDictionary<long, StoredFrame> _slots => _st.Slots;

    private long _fpi;                   // file position indicator: the current slot (§9.1.11)
    private bool _fpiValid;
    /// <summary>Which arm of §14.9.30.4 GR21's relative selection governs the next sequential READ: true while
    /// the file position indicator "was established by a prior successful OPEN or START statement" (rule b — the
    /// indicator is INCLUSIVE and the direction is not consulted), false once a READ established it (rule c —
    /// exclusive, and the direction selects). ⛔ ONE FIELD ANSWERS THIS ONE QUESTION (kb/Work PB343): a second
    /// `_positioner` char that distinguished OPEN from START lived here in lockstep with this flag and existed
    /// only to enforce the INDEXED block's after-OPEN carve-out on a relative file. Nothing in this
    /// organization's rules asks OPEN-or-START.</summary>
    private bool _inclusive;
    private long _pendingKey;            // the RELATIVE KEY item's value, set by the compiler before keyed verbs
    private long _lastReleasedSlot;      // highest RRN THIS connector has released since OPEN (§14.9.51 GR29a's "ascending")
    private long _lastSlot;              // last slot read/written — the GR25/GR29a store-back + seq REWRITE/DELETE target


    /// <summary>The RRN of the record last made available / released — the §14.9.30 GR25 and §14.9.51 GR29a
    /// MOVE-back source the generated code stores into the RELATIVE KEY item.</summary>
    public long LastSlot => _lastSlot;

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed operation (§14.9.30 GR29, §14.9.35 GR21,
    /// §14.9.51 GR29b, §14.9.10 GR4). The compiler decodes the TYPED key field — never raw bytes.</summary>
    public void SetPendingKey(long rrn) => _pendingKey = rrn;

    // ── Record-lock identity (ISO §9.1.16) — a relative record's identity IS its RRN ─────────────────────────

    /// <inheritdoc/>
    public override string LastReadRecordId =>
        _lastSlot > 0 ? _lastSlot.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (sequential access targets the last-read slot, §14.9.35.4 GR5 / §14.9.10.4 GR2;
    /// random/dynamic the slot named by the RELATIVE KEY item, §14.9.35.4 GR21 / §14.9.10.4 GR4 — the
    /// ACCESS MODE alone selects the target, see <see cref="KeyedConnector"/>)
    protected override string MutationTargetRecordIdCore(string recordImage, RecordExtents? recordExtents) => Access == KeyedAccess.Sequential
        ? LastReadRecordId
        : _pendingKey > 0 ? _pendingKey.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (Write sets <see cref="_lastSlot"/> to the released RRN on success, §14.9.51 GR29)
    public override string LastWrittenRecordId => LastReadRecordId;

    // RECORD IS VARYING: the RecordFraming store already carries each record's exact length, so a varying
    // record persists at the length it was written (§13.18.43 GR13) and reports it on READ (GR15);
    // WRITE/REWRITE outside the VaryMin/VaryMax bounds is the GR14/§14.9.35 GR20 '44'.

    public RelativeConnector(string hostPath, int recordWidth, KeyedAccess access, int relativeKeyDigits,
        int varyMin = -1, int varyMax = -1)
        : base(hostPath, recordWidth, access, varyMin, varyMax)
    {
        _keyDigits = relativeKeyDigits;
    }

    /// <inheritdoc/>
    /// <remarks>The RELATIVE KEY item's digit count is deliberately NOT a catalog attribute: §12.4.5.13.3
    /// SR3 puts that item OUTSIDE the record ("shall not be defined in a record description entry
    /// subordinate to the associated file-name"), it is a property of the referencing program rather than of the
    /// physical file, and §9.1.6's list does not name it. A too-small RELATIVE KEY item is the '14' the
    /// connector already reports (§9.1.13.4 item 2), not a file attribute conflict.</remarks>
    protected override string DeclaredOrganization => FixedFileAttributes.Relative;

    /// <summary>§14.9.6.4 GR2 d) — <i>"A file with organization other than sequential, that resides on a mass
    /// storage device."</i> The category is settled by the ORGANIZATION alone, so it needs no medium
    /// determination (kb/Work PB235).</summary>
    public override PhysicalFileCategory Category => PhysicalFileCategory.NonSequential;

    // ── OPEN / CLOSE (ISO §14.9.27 / §14.9.6) ────────────────────────────────────────────────────────────────

    /// <summary>The relative OPEN body. GR14: INPUT/I-O position the FPI at 1; GR15: EXTEND positions after the
    /// highest existing RRN; GR17: an absent OPTIONAL file on I-O/EXTEND is created ('05'); §9.1.13.6 item 5: an
    /// absent NON-optional file on INPUT/I-O/EXTEND is '35' — the already-open '41' guard, the attempted-mode
    /// bookkeeping, the position reset, the ONE presence probe behind <paramref name="presence"/>, GR3's
    /// authority '37' and the '37'/'30' exception mapping live on <see cref="FileConnector.Open"/>.</summary>
    protected override string OpenCore(FileOpenMode mode, FilePresence presence)
    {
        _pendingKey = 0;
        _lastSlot = 0;
        _lastReleasedSlot = 0;
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
                    OptionalAbsent = true;               // positioned "not present" (§14.9.27 GR13)
                    // NO file lock: §9.1.15 establishes one on a PHYSICAL FILE, and there is none here.
                    Attach();                            // empty — the file is absent
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                TakeFileLock(create: false);             // §9.1.15 — the lock, before the store is read through it
                Attach();
                break;
            case FileOpenMode.Output:
            {
                var fs = TakeFileLock(create: true);     // §14.9.27.4 GR18 — OUTPUT creates the physical file
                Attach();
                _st.Clear();                            // OPEN OUTPUT empties the SHARED view (kb/Work PB143)
                RecordFraming.WriteStore(fs, DeclaredAttributes, []);   // a new physical file; the header IS its §9.1.6 attributes
                break;
            }
            case FileOpenMode.IO:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    var io = TakeFileLock(create: true);
                    Attach();
                    _st.Clear();
                    RecordFraming.WriteStore(io, DeclaredAttributes, []);   // created as if OPEN OUTPUT + CLOSE (§14.9.27 GR17)
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
                    RecordFraming.WriteStore(ex, DeclaredAttributes, []);   // §14.9.27 GR17
                    status = FileStatusCode.OptionalFileNotFound;
                }
                else { TakeFileLock(create: false); Attach(); }
                // ⛔ NOTHING IS CAPTURED HERE (kb/Work PB739). The extend release number used to be
                // measured once, at this point, into a private _seqNext — and §14.9.51.4 GR29 a) says in
                // the same breath that under sharing "the record numbers are not necessarily consecutive",
                // which can only be true of a number taken from the file AT THE RELEASE. Two connectors
                // extending one shared file both captured 2 and the second overwrote the first record.
                // NextSequentialSlot reads the live store instead; §14.9.27 GR15's positioning IS that
                // reading, not a saved base.
                break;
        }
        _fpi = 1;                                              // §14.9.27 GR14 — FPI = 1 on INPUT/I-O
        _fpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        _inclusive = true;
        return status;
    }

    /// <summary>Attach the per-physical-file store (kb/Work PB143): the LIVE store when another connector holds
    /// this host open (its content is the truth — never reloaded from disk), else a fresh one loaded from disk.
    /// With no registry table (a standalone connector), a private freshly-loaded store — the pre-PB143 shape.</summary>
    private void Attach()
    {
        if (SharedStores is { } t) { _st = t.AttachRelative(HostPath, Load); return; }
        var s = new RelativeStore();
        Load(s);
        _st = s;
    }

    /// <summary>The relative CLOSE body (ISO §14.9.6): a writable mode persists the store — including via the
    /// run-unit-termination <c>CloseAll</c>, which keyed chains (RL208A) depend on. The not-open '42' guard
    /// lives on <see cref="FileConnector.Close"/>.</summary>
    protected override string CloseCore()
    {
        // A persist IOException maps to '30' on FileConnector.Close (§9.1.13.6 item 1 — the ONE mapping),
        // which ends the open mode either way; ModeKnown then stays true for the USE-declarative scoping.
        // OptionalAbsent (the FPI's "not present" state) survives the CLOSE — §14.9.6.4 GR6 says the file
        // position indicator is unchanged; the next OPEN resets it (FileConnector.Open) — kb/Work PB140.
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
            if (owed) Persist();
        }
        finally
        {
            // §9.1.15 — "The file lock is removed by an explicit or implicit CLOSE statement executed for that
            // file connector", whatever the persist outcome was (kb/Work PB771).
            ReleaseFileLock();
            SharedStores?.Detach(HostPath);
            _st = new RelativeStore();
        }
        ModeKnown = false;   // 9.1.4 - after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
    }

    // ── READ (ISO §14.9.30) ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ [NEXT]</c> — the first existing slot at/after the FPI (inclusive after
    /// OPEN/START, exclusive after a READ; §14.9.30 GR21 relative rules b/c). No such slot → at end '10' (GR24);
    /// an RRN whose significant digits exceed the RELATIVE KEY item's size → '14', at end (GR21 rule d).</summary>
    public string ReadNext(out string image) => ReadSequential(out image, previous: false);

    /// <summary>Sequential <c>READ PREVIOUS</c> (COBOL-2002+; the compiler edition-gates the phrase — the ONE
    /// <c>IBoundRead</c> arm of <c>VersionConformancePass</c>, COBOLNET0900). On THIS organization it selects a
    /// record by exactly the same §14.9.30.4 GR21 rules a READ NEXT does — see
    /// <see cref="SelectSequentialSlot"/> for why the after-OPEN at-end carve-out is the INDEXED block's alone
    /// (kb/Work PB343) and why the selection therefore takes no edition parameter.</summary>
    public string ReadPrevious(out string image) => ReadSequential(out image, previous: true);

    /// <summary>ISO §14.9.30.4 GR21's "When the file is a relative file" SELECTION — rules b)/c): the file
    /// position indicator set by a prior OPEN or START selects "the first existing record that is selected …
    /// regardless of whether NEXT or PREVIOUS is specified" (the inclusive bound), one set by a prior READ
    /// selects the first existing relative key number greater than it under NEXT and less than it under
    /// PREVIOUS. <see langword="null"/> is rule e)'s at-end condition.
    /// <para>⛔ SELECTION ONLY — IT COMMITS NOTHING (kb/Work PB338). Splitting it out of the read is what lets
    /// <see cref="PeekSequentialRecordId"/> answer GR9's "the record identified for access" BEFORE the position
    /// moves, without a second copy of these rules living in the peek.</para></summary>
    private long? SelectSequentialSlot(bool previous)
    {
        // ⛔ RULE b) IS DIRECTION-BLIND, AND IT IS THE WHOLE RULE FOR AN INCLUSIVE INDICATOR (kb/Work PB343).
        // §14.9.30.4 GR21, "When the file is a relative file": "b) If the file position indicator was
        // established by a prior successful OPEN or START statement, the first existing record that is selected
        // is made available, regardless of whether NEXT or PREVIOUS is specified." One walk serves both
        // directions, so `previous` is not consulted at all here: the answer is the first existing record at or
        // after the indicator. That is well defined for every way an inclusive indicator can arise —
        // §14.9.41.4 GR9 a)/b), GR11 and GR12 only ever set a RELATIVE file's indicator to "the relative record
        // number of" an EXISTING record (a comparison satisfied by none is GR9 c)'s invalid key, which
        // invalidates it), so after a START the started-at record itself is the answer in either direction; the
        // one inclusive indicator that can name an empty slot is §14.9.27.4 GR14's OPEN INPUT/I-O "set to 1"
        // over a file whose lowest RRN is higher, and rule b's "first existing record" is then that lowest RRN
        // whichever direction was written.
        // ⛔ THE INDEXED AFTER-OPEN CARVE-OUT DOES NOT REACH THIS ORGANIZATION. GR21 rule d) 3 — "If no such
        // record is found or PREVIOUS is specified and the previous operation on the file was an OPEN
        // statement, the at end condition exists" — is written in the "When the file is an indexed file" block
        // alone, and Annex E.2 item 22, the amendment that put it there, is INFORMATIVE (Annex E is headed
        // "(informative)") and names no organization. Enforcing it here returned '10' from a READ PREVIOUS
        // immediately after OPEN INPUT where rule b) owes the first record. Because the relative block's rule
        // b) reads identically in 2002, 2014 and 2023, this selection takes NO edition parameter —
        // docs/COBOLNET_FILES_DESIGN.md, "READ PREVIOUS is not COBOL-85"; VERSION_CHANGE_REFERENCE row 29 is
        // the indexed leg only.
        if (_inclusive)
        {
            foreach (long k in _slots.Keys) if (k >= _fpi) return k;   // rule b) — ascending, direction ignored
            return null;                                              // rule e) — no record found: the at end condition
        }
        // Rule c) — an indicator established by a prior successful READ is EXCLUSIVE and the direction selects:
        // "the first existing record in the physical file whose relative key number is greater than the file
        // position indicator if NEXT is specified or implied or is less than the file position indicator if
        // PREVIOUS is specified is selected".
        long? slot = null;
        if (previous)
        {
            foreach (long k in _slots.Keys) { if (k < _fpi) slot = k; else break; }
        }
        else
        {
            foreach (long k in _slots.Keys) if (k > _fpi) { slot = k; break; }
        }
        return slot;
    }

    /// <inheritdoc/>
    /// <remarks>The relative organization's §14.9.30.4 GR9 pre-read conflict target: the RRN
    /// <see cref="SelectSequentialSlot"/> names. The remaining precondition beyond
    /// <c>SequentialReadReachesRetrieval</c> is a valid file position indicator (a failed START leaves
    /// none — §14.9.41.4 GR7).</remarks>
    public override string PeekSequentialRecordId(bool previous) =>
        SequentialReadReachesRetrieval && _fpiValid && SelectSequentialSlot(previous) is { } s
            ? s.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "";

    /// <inheritdoc/>
    /// <remarks>§14.9.30.4 GR29 — a random READ positions to the RRN staged in the RELATIVE KEY item by
    /// <see cref="SetPendingKey"/>; a slot that does not exist is the invalid key condition and holds no lock.
    /// <paramref name="keyIndex"/> and <paramref name="recordImage"/> are the indexed organization's key
    /// selectors and carry nothing here (GR29 names only the RELATIVE KEY item).</remarks>
    public override string PeekRandomReadRecordId(int keyIndex, string recordImage, RecordExtents? recordExtents)
    {
        _ = keyIndex; _ = recordImage;
        return ReadOpenModeGuard() is null && !OptionalAbsent && _slots.ContainsKey(_pendingKey)
            ? _pendingKey.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "";
    }

    private string ReadSequential(out string image, bool previous)
    {
        image = BlankRecordArea();
        if (SequentialReadGuard() is { } pre) return Status = pre;   // '47'/'46'/'10' — FileConnector
        if (!_fpiValid) return Status = FileStatusCode.NoValidNextRecord;              // '46' §9.1.13.7 6a (failed START)

        if (SelectSequentialSlot(previous) is not { } s)
        {
            LastReadUnsuccessful = true;
            _fpiValid = false;                                             // §14.9.30 GR24b
            return Status = FileStatusCode.AtEnd;
        }
        if (_keyDigits > 0 && s.ToString().Length > _keyDigits)
        {
            // §14.9.30 GR21 relative rule d: significant digits of the RRN exceed the RELATIVE KEY item → '14',
            // the at-end condition, FPI invalidated.
            LastReadUnsuccessful = true;
            _fpiValid = false;
            return Status = FileStatusCode.RelativeKeyOverflow;
        }
        _fpi = s; _fpiValid = true; _inclusive = false;   // GR21 rule f + rule c's exclusive bound
        _lastSlot = s;
        var read = _slots[s];
        NoteRecordRead(read.Image, read.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(read.Image);
        return ReadSucceeded(FileStatusCode.Success);
    }

    /// <summary>Format-2 random READ (§14.9.30 GR29): the FPI takes the RELATIVE KEY item's value (staged by
    /// <see cref="SetPendingKey"/>); no such record → invalid key '23'; an absent optional file → '23' (GR28).</summary>
    public string ReadRandom(out string image)
    {
        image = BlankRecordArea();
        if (ReadOpenModeGuard() is { } notOpen) return Status = notOpen;                  // '47' §14.9.30.4 GR2
        if (RandomReadAbsentOptionalGuard() is { } absent) return Status = absent;        // '23' §9.1.13.5 3 b)
        if (!_slots.TryGetValue(_pendingKey, out StoredFrame rec))
        {
            LastReadUnsuccessful = true;
            return Status = FileStatusCode.RecordNotFound;                 // '23' §9.1.13.5 3a
        }
        _fpi = _pendingKey; _fpiValid = true; _inclusive = false;
        _lastSlot = _pendingKey;
        NoteRecordRead(rec.Image, rec.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(rec.Image);
        return ReadSucceeded(FileStatusCode.Success);
    }

    // ── WRITE / REWRITE / DELETE (ISO §14.9.51 / §14.9.35 / §14.9.10) ───────────────────────────────────────

    /// <summary>⛔ THE HIGHEST RELATIVE RECORD NUMBER PERMITTED FOR A RELATIVE FILE — ISO §14.9.51.4 GR29 b)'s
    /// <i>"If the relative key data item contains a value that is less than 1 or greater than the highest relative
    /// record number permitted for the file, the execution of the WRITE statement is unsuccessful, and the I O
    /// status for the write file connector is set to '34'"</i>. The implementor's number (Annex A.1 item 107,
    /// docs/CONFORMANCE.md <c>DOC-A.1-107</c>): 2 147 483 647. A key between the store's capacity and this
    /// number is a well-formed relative record number that does not fit — the '24' boundary of GR33 b)
    /// (<see cref="KeyedConnector.StoreHolds"/>), which also takes the INVALID KEY phrase; a key above it is not
    /// a relative record number this implementation permits at all.</summary>
    public const long HighestRelativeRecordNumber = int.MaxValue;

    /// <summary>WRITE (§14.9.51.4 GR29): sequential access releases consecutive RRNs (OUTPUT from 1, EXTEND from
    /// highest+1); RRN digit overflow of the key item → invalid key '24' (GR29a/GR33c). Random/dynamic writes the
    /// slot staged in the key item: occupied → '22' (GR33a), key &lt; 1 or above
    /// <see cref="HighestRelativeRecordNumber"/> → permanent error '34' (GR29b). A record the store cannot hold →
    /// invalid key '24' (GR33 b), <see cref="KeyedConnector.StoreHolds"/>); a record holding a character with no
    /// byte image → '91' (<see cref="FileConnector.RecordHasCharacterWithoutByteImage"/>). Open-mode legality per
    /// §9.1.13.7 item 8 ('48').</summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is written
    /// (determination D-FRA (v); kb/Work PB1053), stored and persisted with it.</param>
    public string Write(string image, int length = -1, RecordExtents? extents = null)
    {
        // §14.9.51.4 GR29 a) "If the access mode of the write file connector is sequential…" vs. GR29 b)
        // "If the access mode … is random or dynamic…": the ACCESS MODE alone selects the release rule
        // (the open mode only picks the STARTING RRN inside GR29 a) — 1 for output, highest+1 for extend).
        // ⛔ The open mode is NOT a disjunct here (kb/Work PB325): a random- or dynamic-access connector open
        // in the extend mode is illegal source (§14.9.27.3 SR2) but a REACHABLE runtime state, and Table 20
        // leaves its WRITE cell blank — item 8 b) below is what it must answer, not GR29 a)'s append.
        if (PermanentErrorReplay() is { } stuck) return Status = stuck;   // §9.1.13.1 — a permanent error in effect (kb/Work PB1541)
        bool sequentialRelease = Access == KeyedAccess.Sequential;
        if (sequentialRelease)
        {
            if (!IsOpen || Mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' §9.1.13.7 8a
            long slot = NextSequentialSlot();
            if (_keyDigits > 0 && slot.ToString().Length > _keyDigits)
                return Status = FileStatusCode.BoundaryViolation;          // '24' §14.9.51 GR29a
            if (Stored(image, length) is not { } seqRec)
                return Status = FileStatusCode.RecordSizeViolation;        // '44' §13.18.43 GR14a
            if (RecordHasCharacterWithoutByteImage(seqRec))
                return Status = FileStatusCode.CharacterWithoutByteImage;  // '91' A.1 item 31 (R47)
            var seqFrame = Framed(seqRec, image, extents);
            if (!StoreHolds(_st.FramedBytesAfterPut(slot, seqFrame)))
                return Status = FileStatusCode.BoundaryViolation;          // '24' §14.9.51.4 GR33 b)
            _st.Put(slot, seqFrame);
            _lastReleasedSlot = slot;
            _lastSlot = slot;                                              // GR29a — MOVEd back into the key item
            return Status = FileStatusCode.Success;
        }
        // §9.1.13.7 8 b) — "If the access mode is dynamic or random, the file connector is not open in the
        // I-O or output mode"; Table 20's Random/Extend and Dynamic/Extend WRITE cells are blank.
        if (!IsOpen || Mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' §9.1.13.7 8b
        long key = _pendingKey;
        if (key is < 1 or > HighestRelativeRecordNumber)
            return Status = FileStatusCode.PermanentBoundary;              // '34' §14.9.51.4 GR29 b)
        if (_slots.ContainsKey(key)) return Status = FileStatusCode.DuplicateKey;   // '22' §14.9.51 GR33a
        if (Stored(image, length) is not { } rec)
            return Status = FileStatusCode.RecordSizeViolation;            // '44' §13.18.43 GR14a
        if (RecordHasCharacterWithoutByteImage(rec))
            return Status = FileStatusCode.CharacterWithoutByteImage;      // '91' A.1 item 31 (R47)
        var frame = Framed(rec, image, extents);
        if (!StoreHolds(_st.FramedBytesAfterPut(key, frame)))
            return Status = FileStatusCode.BoundaryViolation;              // '24' §14.9.51.4 GR33 b)
        _st.Put(key, frame);
        _lastSlot = key;
        return Status = FileStatusCode.Success;
    }

    /// <summary>The relative record number the NEXT sequential-access release takes — ISO §14.9.51.4
    /// GR29 a): <i>"If the open mode of the write file connector is output, the first record released after
    /// the OPEN is 1. If the open mode is extend, the first record released after the OPEN is assigned a
    /// record number that is one greater than the highest relative record number existing in the physical
    /// file. Subsequent records released have relative record numbers that are ascending ordinal numbers.
    /// If the physical file is shared and the open mode is extend, the record numbers are not necessarily
    /// consecutive. Otherwise, they are consecutive."</i>
    /// <para>ONE expression answers all four sentences, which is why there is no captured base any more
    /// (kb/Work PB739). <c>_st.Highest</c> is the highest RRN existing in the physical file AT THIS MOMENT,
    /// so OPEN OUTPUT (which empties the store) yields 1, OPEN EXTEND yields highest+1, an unshared file
    /// yields consecutive numbers because nothing else releases between two of this connector's writes, and
    /// a SHARED file yields the non-consecutive-but-ascending sequence the rule's own last two sentences
    /// describe — the other connector's release has already raised the high-water mark.</para>
    /// <para><c>_lastReleasedSlot</c> is the "ascending" clamp, and it is not redundant: a connector open
    /// I-O may DELETE the top record (§14.9.10.4 GR5) while this one is extending, which lowers the
    /// highest EXISTING number below one this connector already used. The record numbers it releases still
    /// have to ascend.</para></summary>
    private long NextSequentialSlot() => Math.Max(_st.Highest, _lastReleasedSlot) + 1;

    /// <summary>REWRITE (§14.9.35): open mode must be I-O ('49', §9.1.13.7 item 9). Sequential access replaces
    /// the prior READ's record (no prior successful READ → '43', GR5); random/dynamic replaces the slot named by
    /// the key item (absent → '23', GR21). The FPI is unaffected (GR13). A replacing record the store cannot hold
    /// (a longer varying record, §14.9.35.4 GR18) is §9.1.13.5 item 4's "attempt … to write outside the
    /// externally-defined boundaries" — '24', the WRITE's own boundary (<see cref="Replace"/>); one holding a
    /// character with no byte image is '91'.</summary>
    /// <param name="extents">The replacing record's EXTENT TABLE (D-FRA (v); kb/Work PB1053) — a relative record is
    /// replaced whole (GR18), so its table is replaced with it.</param>
    public string Rewrite(string image, int length = -1, RecordExtents? extents = null)
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        // §14.9.35 GR18 — a relative record's size MAY differ from the replaced record's; GR20 still bounds it.
        if (Access == KeyedAccess.Sequential)   // §14.9.35.4 GR5 vs. GR21 — the ACCESS MODE alone
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43'
            if (Stored(image, length) is not { } seqRec)
                return Status = FileStatusCode.RecordSizeViolation;                             // '44' GR20
            return Status = Replace(_lastSlot, seqRec, image, extents);
        }
        if (!_slots.ContainsKey(_pendingKey)) return Status = FileStatusCode.RecordNotFound;    // '23' GR21
        if (Stored(image, length) is not { } rec)
            return Status = FileStatusCode.RecordSizeViolation;                                 // '44' GR20
        return Status = Replace(_pendingKey, rec, image, extents);
    }

    /// <summary>The REWRITE's one replacement, after its own target and size rules: the record-content ('91') and
    /// boundary ('24') tests every write of a record into this store answers, then the replacement.</summary>
    private string Replace(long slot, string stored, string image, RecordExtents? extents)
    {
        if (RecordHasCharacterWithoutByteImage(stored)) return FileStatusCode.CharacterWithoutByteImage;   // '91'
        var frame = Framed(stored, image, extents);
        if (!StoreHolds(_st.FramedBytesAfterPut(slot, frame))) return FileStatusCode.BoundaryViolation;   // '24'
        _st.Put(slot, frame);
        return FileStatusCode.Success;
    }

    /// <summary>DELETE RECORD (§14.9.10): open mode must be I-O ('49', GR1). Sequential access removes the prior
    /// READ's record ('43' without one, GR2); random/dynamic removes the slot named by the key item (absent →
    /// invalid key '23', GR4). The FPI is unaffected (GR9).</summary>
    public string Delete()
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        if (Access == KeyedAccess.Sequential)   // §14.9.10.4 GR2 vs. GR4 — the ACCESS MODE alone
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
            _st.Remove(_lastSlot);
            return Status = FileStatusCode.Success;
        }
        if (!_st.Remove(_pendingKey)) return Status = FileStatusCode.RecordNotFound;
        return Status = FileStatusCode.Success;
    }

    // ── START (ISO §14.9.41) ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>START KEY rel-op (GR9): a NUMERIC comparison over RRNs against <paramref name="operand"/> (the
    /// RELATIVE KEY item's value, GR10) — forward search for =, &gt;, &gt;=; REVERSE search for &lt;, &lt;= (the
    /// first record satisfying the comparison searching the file in reverse order, GR9b). Not satisfied → invalid
    /// key '23' with the FPI invalidated (GR7/GR9c). Open mode must be input or I-O (GR1 → '47').</summary>
    public string Start(string op, long operand)
    {
        if (StartOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();                           // '23' §14.9.41 GR5
        long? found = null;
        switch (op)
        {
            case "==": if (_slots.ContainsKey(operand)) found = operand; break;
            case ">": foreach (long k in _slots.Keys) if (k > operand) { found = k; break; } break;
            case ">=": foreach (long k in _slots.Keys) if (k >= operand) { found = k; break; } break;
            case "<": foreach (long k in _slots.Keys) { if (k < operand) found = k; else break; } break;
            case "<=": foreach (long k in _slots.Keys) { if (k <= operand) found = k; else break; } break;
        }
        return found is { } f ? StartAt(f) : StartFail();
    }

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12): the lowest / highest existing RRN; an empty
    /// (or absent optional) file → invalid key '23'.</summary>
    public string StartFirstLast(bool last)
    {
        if (StartOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent || _slots.Count == 0) return StartFail();
        return StartAt(last ? _st.Highest : _slots.Keys.Min());
    }

    private string StartAt(long slot)
    {
        _fpi = slot; _fpiValid = true; _inclusive = true;
        LastReadUnsuccessful = false;
        return Status = FileStatusCode.Success;
    }

    /// <inheritdoc/>  (a keyed FPI is a key VALUE plus a validity bit; §14.9.41.4 GR7 clears the bit too)
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

    // ── Persistence ──────────────────────────────────────────────────────────────────────────────────────────

    private void Load(RelativeStore into)
    {
        into.Clear();
        // ⛔ NO SECOND PRESENCE QUESTION, AND NO SECOND HANDLE (kb/Work PB771). The load reads the store
        // through the connector's OWN handle — its §9.1.15 file lock — and the handle's existence IS the answer
        // this used to ask HostFile.Probe for a second time in the same OPEN. The only arm that reaches here
        // without one is an absent OPTIONAL file, which holds no records; a file that is PRESENT but refused
        // never reaches here at all (§14.9.27.4 GR3 answered '37' on FileConnector.Open), and for OPEN OUTPUT
        // the handle open itself raises the authority failure the base maps to '37'.
        if (Store is not { } fs) return;
        var frames = RecordFraming.ReadStore(fs, CodeSet);
        for (int i = 0; i < frames.Count; i++)
            if (frames[i] is { } rec)
                into.Put(i + 1, rec);       // slot ordinal = frame ordinal (1-based RRN, §12.4.5.13 GR1)
    }

    private void Persist()
    {
        if (Store is not { } fs) return;   // an absent OPTIONAL file holds no physical store to rewrite
        // The slots LAZILY, gaps included — never a dense array sized by the highest RRN (kb/Work PB1192).
        RecordFraming.WriteStore(fs, DeclaredAttributes, _st.Ordinal(), CodeSet);
    }

}
