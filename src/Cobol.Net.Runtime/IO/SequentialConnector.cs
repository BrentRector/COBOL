// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native sequential file connector (COBOLNET_DESIGN §8) — the control logic the legacy
/// <c>SequentialFileHandler</c>/<c>FileRuntime</c> proved over the 364-NIST corpus, re-substrated from a byte buffer
/// to the record's <b>character image</b> (a C# <see cref="string"/>). A COBOL record IS a typed value; the only edge
/// where it becomes characters is here, at the on-disk boundary. Two on-disk shapes: <b>line-sequential</b>
/// (newline-framed text — <c>WriteLine</c>/<c>ReadLine</c>) and <b>record-sequential</b> (fixed-width blocks, plus the
/// print-control <c>WRITE … ADVANCING</c> raw stream). The ISO §9.1.13 I-O status codes and the §14.9.30/§14.9.35
/// read-position state machine live on the shared <see cref="FileConnector"/> base; the sequential-specific
/// stream/framing/LINAGE machinery is here.
/// </summary>
public sealed class SequentialConnector : FileConnector
{
    private readonly bool _lineSequential;

    private StreamReader? _reader;
    private StreamWriter? _writer;
    // ⛔ TWO QUESTIONS, TWO FIELDS (kb/Work PB864). One flag, `_afterAdvancing`, used to answer both, so a file
    // whose last WRITE carried a BEFORE phrase — whose own advance had already ended the line — got one more
    // line terminator at CLOSE, a blank line the program never wrote.
    /// <summary>Has this connector seen a print-control WRITE (an ADVANCING phrase) since the OPEN? Then an
    /// omitted ADVANCING phrase still line-advances (<see cref="Write"/>). Asked by the ROUTING, never by
    /// CLOSE.</summary>
    private bool _printControl;
    /// <summary>Is the device standing on a line that a presentation has written and no travel has ended yet —
    /// an AFTER write's record, which §14.9.51.4 GR25 f) presents AFTER its advance? Set by
    /// <see cref="Present"/>, cleared by every travel (<see cref="AdvanceLines"/> with a positive count, a
    /// form feed, a line sequential record's own delimiter). Asked by CLOSE, which terminates exactly such a
    /// line and nothing else.</summary>
    private bool _lineOpen;

    // The byte offset of the most recently read record's fixed-width block (for the in-place record-sequential
    // REWRITE) and the LOGICAL read offset it derives from. The logical offset counts characters CONSUMED from
    // the reader (Latin1 — one byte per character; a record-sequential file is pure fixed-width blocks): the
    // StreamReader BUFFERS, so BaseStream.Position is the buffer-fill boundary, never the read position —
    // deriving the block start from it corrupted the rewrite target (IX106A REWRITE-TEST-GF-02).
    private long _lastReadBlockStart = -1;
    private long _readOffset;
    // The component count of the EXTENT TABLE the last-read VARYING frame carried (determination D-FRA (v);
    // kb/Work PB1053), -1 when it carried none. The table sits immediately before _lastReadBlockStart, so an in-place
    // REWRITE can replace it with the record's new table — or void it — without changing the frame's size.
    private int _lastReadTableCount = -1;
    // §14.9.30 GR15 / NOTE 3: the unread tail of an over-length LINE-SEQUENTIAL record — the file position indicator
    // "next unread character in the record". The next READ returns this (chunked to the record width) before reading a
    // new physical line, so a program using the GR15 multi-read pattern sees the whole logical line, not silent loss.
    private string? _lineRemainder;
    // §14.9.35.4 GR17 (line-sequential REWRITE in place): the byte anchor + data length of the last-read physical
    // line (the "record being replaced"), and whether that read transferred only PART of the record (an over-length
    // '06' read or a served remainder) — GR17a then makes a REWRITE '44'. Under Latin1 one char == one byte, so
    // _lineByteOffset tracks the physical byte position exactly (the same logical-offset discipline as _readOffset).
    private long _lineByteOffset;
    private long _lastLineStart = -1;
    private int _lastLineBytes;
    private bool _lastReadLinePartial;

    // ── Record-lock identity (ISO §9.1.16 on sequential organization) ────────────────────────────────────────
    // A sequential record's lock identity is its 1-based ORDINAL position in the physical file — two connectors
    // reading the same physical file agree on ordinals, and the successor relationship (§14.9.51 GR17) makes the
    // ordinal stable. Reads count from OPEN (INPUT/I-O always position at the start).
    // ⛔ WRITES DO NOT COUNT FROM ANYWHERE ON THIS CONNECTOR (kb/Work PB739). The ordinal of a released record
    // is a property of the PHYSICAL FILE — §14.9.51.4 GR19 says the records added by two sharing connectors
    // "follow the records present in the physical file", which is every connector's releases and not this
    // one's — so it is minted from the shared PhysicalFileTable.State.ReleasedOrdinal at the moment of the
    // release (ReleaseRecord). A per-connector base plus a per-connector count had both connectors calling
    // their first appended record ordinal 2. Unshared connectors have no shared state and no observable
    // identity: _writeOrdinal stays 0 and LastWrittenRecordId is empty, exactly as before.
    private long _readOrdinal;        // ordinal of the record most recently made available by Read
    private long _writeOrdinal;       // ordinal of the record most recently released BY THIS connector; 0 = none

    // The physical file's release generation (PhysicalFileTable.State.ReleaseGeneration) this connector's
    // read-ahead is coherent with (kb/Work PB753). Set wherever the reader's buffer is known to agree with the
    // medium — when the handle is created (OpenReader), when it is repositioned (SeekToRecord), and when THIS
    // connector is the one that released (NoteRelease) — and compared before every physical frame.
    private long _coherentAt;

    // §13.18.43 GR2 frame offsets of a RECORD VARYING file, index = ordinal − 1. Built LAZILY, on the first
    // backward read only: a forward READ walk must not pay for a facility it never uses, and a fixed-width file
    // never needs it at all (its offsets are arithmetic). The physical frame layout cannot change while a
    // connector is open — §14.9.35.4 GR16 makes a record-sequential REWRITE size-preserving — so one build per
    // OPEN is enough; OpenCore drops it.
    private List<long>? _varyingStarts;

    /// <summary>The ordinal a sequential Read in the given DIRECTION would deliver (the §14.9.30.4 GR9 pre-read
    /// conflict target — knowable BEFORE the read because sequential retrieval moves by exactly one record).
    /// GR21's "When the file is a sequential file" rules decide it: rule b) — the file position indicator
    /// established by a prior OPEN selects "the first existing record … regardless of whether NEXT or PREVIOUS is
    /// specified", so both directions target ordinal 1; rule c) — after a successful READ, the record whose
    /// number is greater than the indicator for NEXT and less than it for PREVIOUS. 0 means no such record exists
    /// (the beginning of the file), which rule e) makes the at-end condition.
    /// <para>⛔ <c>_readOrdinal == 0</c> IS rule b's antecedent ONLY WHILE START HAS NO SEQUENTIAL ARM. The rule
    /// says "established by a prior successful OPEN <b>or START</b> statement", and §14.9.41.3 SR2 does admit a
    /// START on a sequential-organization file ("If the organization of the file referenced by file-name-1 is
    /// sequential, either the FIRST or the LAST phrase shall be specified"). <c>KeyedIoBinder.BindStart</c>
    /// declines that form LOUDLY today (<c>BoundUnsupported</c>, "a later slice"), so OPEN and READ are the only
    /// two ways this connector's indicator is ever set. Whoever lands START FIRST/LAST here shall revisit THIS
    /// method: a START-established indicator is INCLUSIVE, so rule b would then have to select the started-at
    /// record itself in either direction rather than ordinal 1.</para></summary>
    internal long TargetReadOrdinal(bool previous) =>
        !MovesBackward(previous) || _readOrdinal == 0 ? _readOrdinal + 1 : _readOrdinal - 1;

    /// <summary>⛔ THE ONE CONVERSION of a READ statement's direction phrase into this connector's physical
    /// direction of travel — §14.9.30.4 GR19's read kind XOR the COBOL-85 REVERSED phrase's standing reversal
    /// (<see cref="Reversed"/>). Every direction-sensitive member asks THIS and nothing else, so the phrase
    /// cannot be honoured by the retrieval and missed by the §14.9.30.4 GR9 pre-read peek.
    /// <para>XOR rather than a special case because REVERSED presents the file in reverse: PREVIOUS on a
    /// REVERSED file means back toward the reversed file's beginning, which is the physical end. The two can
    /// never actually co-occur in conforming source — REVERSED is COBOL-85 only and PREVIOUS is a COBOL-2002
    /// introduction — so the generalization costs nothing and leaves no undefined combination for
    /// <c>--permissive</c> to fall into (kb/Work PB668).</para></summary>
    private bool MovesBackward(bool previous) => previous ^ Reversed;

    /// <summary>The COBOL-85 <c>OPEN INPUT … REVERSED</c> standing reversal: while set, every READ of this
    /// connector travels in the opposite direction to the one its statement names. Established by
    /// <see cref="PositionReversed"/> after a successful OPEN and cleared by every OPEN, so it can never
    /// outlive the opening that wrote the phrase.</summary>
    internal bool Reversed { get; private set; }

    /// <summary>Apply <c>OPEN INPUT … REVERSED</c> (COBOL-85; VERSION_CHANGE_REFERENCE row 7.12) to a connector
    /// whose OPEN has just succeeded: the file is positioned at its END and retrieval runs backward, so the
    /// first READ makes the LAST record available and the at end condition arises at the first record.
    /// <para>The position is expressed in the SAME file position indicator §14.9.30.4 GR21 is written over —
    /// one past the last record — so GR21 rule c)'s decreasing arm selects the last record and rule e)'s
    /// "no record is found" is the at-end at ordinal 0. A file with no records leaves the indicator at 0, GR21
    /// rule b)'s own value, and the first READ then takes the ordinary empty-file at-end path.</para>
    /// <para>The count is <see cref="ExistingRecordCount"/> — the ONE measurement of how many records the
    /// physical file holds, shared with the §14.9.51.4 GR19 EXTEND ordinal base, never a second walk. A medium
    /// that cannot be positioned (LINE SEQUENTIAL, a non-seekable stream) leaves the indicator at 0 and the
    /// first backward READ reports the permanent error <see cref="SeekToOrdinal"/> already answers with — the
    /// same posture <c>READ … PREVIOUS</c> takes, and unreachable from conforming source because COBOLNET2210
    /// screens the organization at bind time.</para></summary>
    internal void PositionReversed()
    {
        Reversed = true;
        _readOrdinal = _reversedRecordCount > 0 ? _reversedRecordCount + 1 : 0;
    }

    /// <summary>Does the OPEN statement about to run on this connector carry the REVERSED phrase? Set by the
    /// registry's ONE open dispatch (<c>FileRegistry.OpenCore</c>) before EVERY open of a sequential connector,
    /// so it always describes the current statement and can never leak into the next one.</summary>
    internal bool ReversedRequested { private get; set; }

    /// <summary>The record count <see cref="PositionReversed"/> positions from, MEASURED INSIDE THE OPEN BODY
    /// (<see cref="OpenCore"/>'s INPUT arm) and only for a REVERSED open. ⛔ It used to be measured after the
    /// OPEN had returned, from <see cref="PositionReversed"/> itself — the one read of the physical file left
    /// outside <c>FileConnector.Open</c>'s try, against <c>FileRegistry.SharedOpenAttempt</c>'s rule that
    /// nothing after a successful open may touch it (kb/Work PB713), so a host failure while counting escaped
    /// the run unit instead of becoming the OPEN's '30' (kb/Work PB1512's sibling sweep). An absent OPTIONAL
    /// file counts none — THE ONE PRESENCE ANSWER PER OPEN (kb/Work PB323), never a second probe.</summary>
    private long _reversedRecordCount;

    /// <inheritdoc/>
    /// <remarks>The record sequential organization's §14.9.30.4 GR9 pre-read conflict target: the ordinal
    /// <see cref="TargetReadOrdinal"/> names, which is knowable without reading because sequential retrieval
    /// moves by exactly one record. The remaining precondition beyond
    /// <c>SequentialReadReachesRetrieval</c> is a live stream.</remarks>
    public override string PeekSequentialRecordId(bool previous) =>
        SequentialReadReachesRetrieval && _reader is not null && TargetReadOrdinal(previous) is > 0 and var t
            ? t.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "";   // ordinal 0 is GR21 rule e)'s beginning-of-file — no record, so nothing to conflict with

    /// <inheritdoc/>
    public override string LastReadRecordId =>
        _readOrdinal > 0 ? _readOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (a sequential REWRITE replaces the record obtained by the last successful READ, §14.9.35)
    protected override string MutationTargetRecordIdCore(string recordImage, RecordExtents? recordExtents) => LastReadRecordId;

    /// <inheritdoc/>
    public override string LastWrittenRecordId => _writeOrdinal > 0
        ? _writeOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <summary>The RELEASE of a logical record to the operating environment — ISO §14.9.51.4 GR12,
    /// <i>"The successful execution of a WRITE statement releases a logical record to the operating
    /// environment"</i> — and the ONE place a sequential release happens. Every successful WRITE arm ends here.
    /// <para>For a §9.1.15 participant the release is literal: the writer is flushed, so the record is IN the
    /// physical file before the WRITE statement returns. It has to be. The physical file is the medium two
    /// sharing connectors share (the keyed organizations share an in-memory store instead), so a record still
    /// sitting in this connector's buffer is a record the other connector cannot see, cannot count, and — with
    /// <see cref="SharedAppendStream"/> positioning each write at the end as it stands — would be written
    /// straight over. The ordinal is minted from the same shared state, so it names the record's real place in
    /// the file (§9.1.16: <i>"While locked by a given file connector, a record is not accessible to another
    /// file connector"</i> — the lock is only as good as the identity).</para>
    /// <para>A connector holding the ONLY writable handle flushes at CLOSE as it always did: no other writer
    /// can observe the record, and a flush per record on the ordinary WRITE path is a syscall per record for an
    /// answer nobody reads.</para>
    /// <para>⛔ THE TWO HALVES HAVE DIFFERENT ANTECEDENTS, so they are two statements (kb/Work PB740). The
    /// FLUSH is owed whenever another writer may be on the physical file, which is a property of the §9.1.15
    /// file lock this connector's handle carries — <c>SHARING WITH ALL OTHER</c>, or a clause-less connector
    /// the arbiter has admitted alongside a writing sibling, or the same connector after
    /// <see cref="Reposture"/> widened it. The ORDINAL is the §9.1.16 record-lock identity, and a connector
    /// that registered NEITHER a SHARING nor a LOCK MODE clause sets no record locks to identify — §12.4.5.9.4
    /// GR1 b) 2. leaves that case to the implementor (<i>"the type of record locking for that opening … is
    /// defined by the implementor … or specify that the default is no record locking"</i>) and this compiler's
    /// determination is no record locking (<see cref="FileLockMode.None"/>) — so the ordinal is minted only
    /// where the physical file's shared state exists to mint it from. Gating the flush on the ordinal's
    /// antecedent left
    /// a clause-less pair sharing a physical file writing through two buffers, where a mid-record flush of one
    /// buffer can split a record around the other's.</para></summary>
    private void ReleaseRecord()
    {
        if (FileLockPosture.AdmitsAnotherWriter(HostShare))
        {
            _writer!.Flush();                      // GR12 — released, not merely buffered
            NoteRelease();                         // …and now visible, so no sibling's read-ahead may hide it
        }
        if (SharedPhysical is { } shared)
            _writeOrdinal = ++shared.ReleasedOrdinal;   // §9.1.16 identity, minted from the physical file's mint
    }

    /// <summary>⛔ THE ONE PLACE A RELEASE THAT HAS REACHED THE PHYSICAL FILE IS ANNOUNCED TO THE CONNECTORS
    /// READING IT (kb/Work PB753) — the read-side half of the release rule, and it is one rule for both verbs
    /// because the standard writes it once for each in the same words: §14.9.51.4 GR12, <i>"The successful
    /// execution of a WRITE statement releases a logical record to the operating environment"</i>, and
    /// §14.9.35.4 GR4, <i>"The successful execution of the REWRITE statement releases a logical record to the
    /// operating environment"</i>. §9.1.15 3) makes the concurrency legal — <i>"The sharing with all other mode
    /// allows concurrent access to a physical file through other file connectors specifying input, I-O, or
    /// extend mode"</i> — and §14.9.30.4 GR21 c)/d) say what the sibling's next READ owes: the record selected
    /// is <i>"the first existing record in the physical file whose relative key number is greater than the file
    /// position indicator"</i>, and THAT record <i>"is made available in the record area"</i>. A reader serving
    /// characters buffered before this release would make available a record the physical file no longer holds.
    /// <para>⛔ AND IT SETS THIS CONNECTOR'S OWN WATERMARK IN THE SAME BREATH. A connector's own release never
    /// invalidates its own read-ahead: a sequential REWRITE targets the record the last READ delivered, whose
    /// bytes are at or before the file position indicator, so nothing this connector will read again was
    /// touched. Advancing <c>_coherentAt</c> here is what keeps a READ/REWRITE loop on one I-O connector from
    /// re-filling its buffer once per record.</para>
    /// <para>⚠ It is NOT called where a release stays in this connector's own writer buffer. That case is the
    /// FLUSH's antecedent (see <see cref="ReleaseRecord"/>), and announcing bytes that have not reached the
    /// file would send a reader to a half-written frame instead of a whole stale one.</para></summary>
    private void NoteRelease()
    {
        if (Physical is { } physical) _coherentAt = ++physical.ReleaseGeneration;
    }

    /// <summary>⛔ THE READ-SIDE INVALIDATION, at the ONE point every physical read of this connector passes
    /// through (<see cref="NextFrame"/>): if a sibling connector has released a record to this physical file
    /// since this reader's buffer was filled, the buffer is discarded and the reader re-anchored at the file
    /// position indicator, so the next frame comes off the medium as it stands NOW (§14.9.30.4 GR21 c/d —
    /// see <see cref="NoteRelease"/> for the derivation).
    /// <para>The anchor is the LOGICAL offset, never <c>BaseStream.Position</c>: a <see cref="StreamReader"/>
    /// buffers ahead, so the base position is the buffer-fill boundary. That is the same fact
    /// <see cref="SeekToRecord"/> and <see cref="Reposture"/> are built on, and it is why a bare
    /// <c>DiscardBufferedData</c> would be wrong here — it would resume at the fill boundary and skip every
    /// record the buffer had already read ahead of.</para>
    /// <para>An over-length LINE SEQUENTIAL record's unread remainder (§14.9.30.4 GR15 / NOTE 3) is untouched
    /// on purpose: it is part of a record ALREADY made available, and <c>_lineByteOffset</c> is already past
    /// its physical line, so re-anchoring neither loses it nor re-reads it.</para>
    /// <para>⛔ A TRUNCATING SIBLING CANNOT ARISE, so no rule is written for one: every cell of Table 19's two
    /// OUTPUT request rows is <i>Unsuccessful open</i> (§14.9.27.4; §9.1.13.9 1) e) — <i>"An attempt is made to
    /// open a physical file in the output mode and the physical file is currently open by another file
    /// connector"</i>), so nothing may truncate this file while this connector holds it open.</para></summary>
    private void EnsureReaderCoherent()
    {
        if (Physical is not { } physical || physical.ReleaseGeneration == _coherentAt) return;
        _coherentAt = physical.ReleaseGeneration;
        if (_reader is not { BaseStream.CanSeek: true } reader) return;
        reader.BaseStream.Seek(_lineSequential ? _lineByteOffset : _readOffset, SeekOrigin.Begin);
        reader.DiscardBufferedData();
    }

    /// <summary>Re-derive this connector's ISO §9.1.15 file lock while it is open — see
    /// <see cref="FileConnector.Reposture"/> for why the registry calls it and why it shall not escape. A host
    /// share mode is fixed when the handle is created, so "widen" means <b>rebuild the handle</b>, at the same
    /// logical position, with nothing else about the connector disturbed.
    /// <para>The position is already tracked, and not by the base stream: <c>StreamReader</c> buffers ahead, so
    /// <c>BaseStream.Position</c> is the buffer-fill boundary and never the read position — the logical offsets
    /// (<c>_readOffset</c> for the framed readers, <c>_lineByteOffset</c> for LINE SEQUENTIAL; Latin-1, so one
    /// character is one byte) are the file position indicator's byte address, and the fresh reader is seeked
    /// straight to it. Everything derived from it survives untouched: the §14.9.30.4 GR15 unread remainder, the
    /// §14.9.35.4 GR17 REWRITE anchors (absolute file offsets, still valid on the new handle) and the §9.1.16
    /// read ordinal. A writer is FLUSHED first (§14.9.51.4 GR12) and reopened
    /// <see cref="FileMode.Append"/> — the current physical end is exactly where it was, and Append never
    /// truncates, so an <c>OPEN OUTPUT</c> connector keeps the file it has created.</para>
    /// <para>⚠ A writer, or an I-O reader, holds WRITE access, and the outgoing handle's own share mode is what
    /// denies a second one — so those cannot be opened before the old handle is released and the rebuild is
    /// dispose-then-open. If the host refuses the new posture in that window (only a foreign process can cause
    /// it) the OLD posture is reopened, which restores the connector exactly. A reader with no write access
    /// needs no window at all, so it is open-then-dispose. No mode holds both a reader and a writer — INPUT and
    /// I-O have only <c>_reader</c>, OUTPUT and EXTEND only <c>_writer</c> — so a half-repostured connector is
    /// not expressible.</para></summary>
    internal override void Reposture(FileShare share)
    {
        if (share == HostShare) return;
        if (!IsOpen) { base.Reposture(share); return; }
        if (_reader is { } reader)
        {
            long at = _lineSequential ? _lineByteOffset : _readOffset;
            if (RebuildMustReleaseFirst)
            {
                // WRITE access: the outgoing handle's share mode can forbid the incoming one, so release
                // first — the rule is FileConnector.RebuildMustReleaseFirst's and the keyed connectors read
                // the same predicate (kb/Work PB771). For a reader that is exactly the I-O mode.
                reader.Dispose();
                _reader = null;
                _reader = OpenReader(share, at, HostShare);
            }
            else
            {
                var fresh = OpenReader(share, at, null);
                reader.Dispose();
                _reader = fresh;
            }
        }
        else if (_writer is { } writer)
        {
            writer.Flush();                        // GR12 — nothing buffered may cross the handle boundary
            writer.Dispose();
            _writer = null;
            // FileMode.Append for BOTH the OUTPUT and the EXTEND writer: the physical end is where the flushed
            // handle stood, and Append does not truncate, so an OPEN OUTPUT connector keeps what it created.
            _writer = OpenWriter(FileMode.Append, share, HostShare);
        }
        base.Reposture(share);
    }

    /// <summary>⛔ THE ONE SPELLING OF THIS CONNECTOR'S READ HANDLE — the INPUT and I-O arms of
    /// <see cref="OpenCore"/> and the rebuild in <see cref="Reposture"/> are the same stream, and a second
    /// spelling would be a second answer to its encoding, its access and its <see cref="FileOptions"/>. The I-O
    /// arm asks for <see cref="FileAccess.ReadWrite"/> because §14.9.35 GR3's REWRITE writes through this
    /// reader's <c>BaseStream</c>, and takes NO <see cref="FileOptions.SequentialScan"/> for the same reason: it
    /// seeks, and the sequential-access hint asks the host to evict what a seek comes back for.
    /// <para><paramref name="at"/> is the byte the next READ shall deliver, and <paramref name="fallbackShare"/>
    /// the posture to restore when the host refuses <paramref name="share"/> and the outgoing handle is already
    /// gone (null = it is still open, so there is nothing to restore and the refusal propagates to a caller that
    /// still has one).</para></summary>
    private StreamReader OpenReader(FileShare share, long at, FileShare? fallbackShare)
    {
        StreamReader Open(FileShare s) => new(HostFile.OpenConnectorStream(HostPath, FileMode.Open,
            Mode == FileOpenMode.IO ? FileAccess.ReadWrite : FileAccess.Read, s,
            Mode == FileOpenMode.IO ? FileOptions.None : FileOptions.SequentialScan), Encoding.Latin1);
        StreamReader r;
        try { r = Open(share); }
        catch (IOException) when (fallbackShare is { } old) { r = Open(old); }
        if (at != 0) r.BaseStream.Seek(at, SeekOrigin.Begin);
        // A brand-new handle has read nothing, so it agrees with the medium by construction (kb/Work PB753).
        _coherentAt = Physical?.ReleaseGeneration ?? 0;
        return r;
    }

    /// <summary>⛔ THE ONE SPELLING OF THIS CONNECTOR'S WRITE HANDLE — the OUTPUT arm
    /// (<see cref="FileMode.Create"/>), the EXTEND arm (<see cref="FileMode.Append"/>) and the rebuild. The role
    /// decides plain-versus-repositioning from the posture; this decides the newline and the encoding, once.</summary>
    private StreamWriter OpenWriter(FileMode mode, FileShare share, FileShare? fallbackShare)
    {
        // The file coded character set's STRICT encoding (kb/Work PB690): every write arm refuses a record holding
        // a character with no byte image before it reaches this writer ('91' / '71'), so the exception fallback is
        // the guard that keeps that refusal the only answer — never Latin-1's silent '?'.
        StreamWriter Open(FileShare s) => new(HostFile.OpenConnectorWriteStream(HostPath, mode, s),
            FileCharacterSet.Medium) { NewLine = _lineEnd };
        try { return Open(share); }
        catch (IOException) when (fallbackShare is { } old) { return Open(old); }
    }

    /// <summary>The count of records ALREADY IN the physical file, in the framing this connector reads — the
    /// write-ordinal base a sharing-active <c>OPEN EXTEND</c> continues from (§14.9.51.4 GR18: <i>"If there are
    /// records in the physical file, the first record written after the execution of the OPEN statement with
    /// the EXTEND phrase is the successor of the last record in the physical file"</i>; GR19 fixes the
    /// measurement POINT for the shared case: <i>"the added records follow the records present in the physical
    /// file when it was opened"</i>).
    /// <para>⛔ IT IS MEASURED BEFORE THIS OPEN'S OWN WRITER EXISTS, and that ordering is the fix for kb/Work
    /// PB713, not an optimization. The measurement used to run from <c>FileRegistry.SharedOpenAttempt</c> AFTER
    /// <c>FileConnector.Open</c> returned, so it took a SECOND handle on a path this connector already held for
    /// WRITE: the line-sequential arm's <c>File.ReadLines</c> and the varying arm's three-argument
    /// <c>FileStream</c> both request <see cref="FileShare.Read"/>, which does not admit the outstanding Write
    /// access, so the operating environment refused — and, being outside <c>FileConnector.Open</c>'s try, the
    /// refusal escaped the run unit as an unhandled <c>IOException</c> rather than as an I-O status. Both halves
    /// are addressed here: the measurement happens where no second handle exists, and it happens INSIDE the
    /// OPEN body, where §14.9.27.4 GR25's <i>"the file is not affected"</i> and §9.1.13.6 item 1's '30' are the
    /// only outcomes an unreadable file can have. GR19 permits the earlier point because an <c>OPEN EXTEND</c>
    /// writes nothing: the record count at the writer's creation and at the OPEN's completion are the same
    /// number.</para>
    /// <para>The stream is <see cref="HostFile.OpenAuxiliary"/> — the ONE role for a bookkeeping handle, share
    /// <see cref="FileShare.ReadWrite"/> — so the ordering above is belt AND braces: a sibling connector of this
    /// run unit holding the same physical file under §9.1.15 sharing cannot refuse it either.</para></summary>
    private long ExistingRecordCount()
    {
        if (IsVarying)
        {
            // The frame WALK, not ReadStore: FrameStarts seeks over every payload instead of materializing it,
            // so counting an existing file costs one pass and no record storage (ReadStore allocated a string
            // per record purely to take .Count of the list).
            using var fs = HostFile.OpenAuxiliary(HostPath, FileMode.Open, FileAccess.Read);
            return RecordFraming.FrameStarts(fs).Count;
        }
        if (_lineSequential)
        {
            // Counted under the SAME delimiter rule a READ applies (ReadPhysicalLine, DOC-A.1-114): a record ends
            // at an LF (a CR LF pair ends at its LF) and a lone CR is data, so the count is the LF bytes plus a
            // final record with no delimiter. StreamReader.ReadLine would also end a record at a lone CR — the
            // rule kb/Work PB1540 removed from the reader — and decode and allocate a string per line besides.
            using var fs = HostFile.OpenAuxiliary(HostPath, FileMode.Open, FileAccess.Read);
            Span<byte> buffer = stackalloc byte[4096];
            long n = 0;
            bool open = false;   // bytes seen since the last delimiter: a final, undelimited record
            for (int got; (got = fs.Read(buffer)) > 0;)
            {
                ReadOnlySpan<byte> chunk = buffer[..got];
                n += chunk.Count((byte)'\n');
                open = chunk[^1] != (byte)'\n';
            }
            return open ? n + 1 : n;
        }
        // Fixed-width record-sequential: arithmetic over the file's length. No handle at all, which is why this
        // arm never showed PB713 (FileInfo.Length is metadata).
        return RecordWidth > 0 ? new FileInfo(HostPath).Length / RecordWidth : 0;
    }

    // A varying file's records are length-framed on disk (the ONE RecordFraming 4-byte little-endian length
    // prefix per record — the same framing the keyed connectors' store uses; the physical format is
    // implementor-defined, §13.18.43 GR2, and only self-consistency matters since producer and consumer run on
    // this connector). WRITE/REWRITE outside the VaryMin/VaryMax bounds is the GR14 '44'; a record-sequential
    // REWRITE must also match the replaced record's size (§14.9.35 GR16).

    // ── LINAGE logical-page state (ISO §13.18.34 / §14.9.51 GR25 g), GR26–28) ────────────────────────────────
    // ⛔ THE LINAGE FEATURE IS A PHYSICAL PAGE LAYOUT, NOT ONLY A COUNTER (kb/Work PB523). §13.18.34.4 GR1:
    // "The logical page size is the sum of the values referenced by each phrase except the FOOTING phrase" —
    // so the logical page IS top margin + page body + bottom margin (GR4: integer-3 "specifies the number of
    // lines in the top margin on the logical page"; GR5 the same for the bottom), and GR8 — "Each logical page
    // is contiguous to the next with no additional spacing provided" — lays those pages end to end on the
    // medium. GR8 forbids spacing ADDITIONAL to the logical page; it does not delete the margins the page is
    // defined to CONTAIN. Reading GR8 as "counter-only" left GR4 and GR5 with no content at all and made GR1's
    // "sum ... except the FOOTING phrase" a sum of one term, and it made §14.9.51.4 GR25 g) / GR26 a) — "the
    // device is repositioned to the first line that may be written on the next logical page" — a plain one-line
    // advance or a bare form feed.
    //
    // THE MODEL. The connector tracks WHERE ON THE CURRENT LOGICAL PAGE the device sits, page-relative:
    //   physical page line 1 … _top          the top margin      (GR4)      — never written on
    //   physical page line _top + c          page body line c    (GR2)      — c IS the LINAGE-COUNTER (GR7)
    //   … + _pageBody + 1 … + _bottom        the bottom margin   (GR5)      — never written on
    // so the device position is exactly `_top + LinageCounter` and needs no second variable — EXCEPT for the
    // one moment the two disagree: at the start of a page the counter already reads 1 while the top margin has
    // not yet reached the stream. <see cref="_topMarginPending"/> is that one bit, and materializing it is the
    // ONE thing every presentation and every advance does first (<see cref="EmitTopMarginIfPending"/>).
    // ⛔ THE OPERAND VALUES ARRIVE WITH THE STATEMENT (a LinagePage?, null = the file has no LINAGE clause), and
    // ONE argument serves both operand forms — a literal operand renders a constant, a data-name operand renders
    // the EXECUTING element's field read (§13.18.34 GR6a/GR6b). What the connector keeps is the page MODEL most
    // recently determined, because GR6 says "the value applies to the next logical page"; it keeps no source, or
    // a shared connector would answer with whichever element/activation installed one last (kb/Work PB673).
    private int _pageBody;      // page size — the writable page-body line count (GR2)
    private int? _footing;      // footing start (GR3 — footing area = [footing, page size] inclusive); null = the
                                // FOOTING phrase is absent (GR1), which is NOT the value 0 (kb/Work PB525)
    private int _top, _bottom;  // top/bottom margins (GR4/GR5) — lines OF the logical page (GR1), never written on

    /// <summary>⛔ "THIS CONNECTOR HAS A LIVE LOGICAL PAGE", written down ONCE. A page model exists only between
    /// an OPEN OUTPUT that established it (§13.18.34.4 GR6 b) 1 and GR7 d) name that mode and no other) and the
    /// CLOSE that ends it (<see cref="EndLinagePage"/>), and GR6's value rules make the page size positive, so a
    /// zero body IS the absence of a page. Every arm that has to choose between the logical page and the plain
    /// print stream asks HERE — <see cref="Position"/>, <see cref="Present"/> and
    /// <see cref="EmitLineSequentialRecord"/> — rather than each spelling the test its own way, because three
    /// spellings of one predicate is how one of them ends up disagreeing.</summary>
    private bool HasLogicalPage => _pageBody > 0;

    /// <summary>The current logical page's top margin (§13.18.34.4 GR4) has not yet reached the medium: the
    /// device is on physical page line 1 while <see cref="LinageCounter"/> already reads 1 (body line 1).
    /// <para>⛔ THE MARGIN IS MATERIALIZED LAZILY, AT THE FIRST THING WRITTEN ON THE PAGE, and that is the one
    /// latitude this model takes — a latitude the standard GRANTS: §14.9.27.4 GR18, <i>"If physical pages have
    /// meaning for the file, the positioning of the output medium with respect to physical page boundaries is
    /// implementor-defined following the successful execution of the OPEN statement, whether or not the LINAGE
    /// clause is specified"</i>. The standard fixes the LOGICAL position (GR7 d) sets the counter to one at OPEN
    /// OUTPUT, i.e. body line 1) and leaves the medium's own position open, and a stream file has no position
    /// other than the bytes in it: a file opened OUTPUT and closed with no WRITE released nothing
    /// (§14.9.51.4 GR12), so it shall not acquire a page's worth of blank lines. Whenever a record IS presented
    /// the bytes are identical to the eager reading, and the surveyed implementation (GnuCOBOL,
    /// `flag_needs_top`) defers it the same way (feedback_follow_gnucobol_on_split_latitude).</para></summary>
    private bool _topMarginPending;

    /// <summary>The LINAGE-COUNTER register (ISO §8.4.3.14): the line number at which the device is positioned
    /// within the current page body (§13.18.34 GR7). Only this connector (the I-O control system) modifies it (GR7b).</summary>
    public long LinageCounter { get; private set; }

    /// <summary>The end-of-page condition of the most recent WRITE (ISO §14.9.51 GR26): page overflow (GR26a) or
    /// printing/spacing within the footing area (GR26b). Reset at the start of every counter-advancing write.</summary>
    public bool EndOfPage => _endOfPage is not null;

    /// <summary>⛔ WHICH end-of-page condition, as the exception-name §14.9.51.4 GR27 a) sets to exist for it —
    /// <i>"If the end-of-page condition was caused by the action in General rule 26a, the EC-I-O-EOP-OVERFLOW
    /// exception condition is set to exist. If the end-of-page condition was caused by the action in General rule
    /// 26b, the EC-I-O-EOP exception condition is set to exist."</i> The two arms are told apart HERE, where
    /// <see cref="PositionOnLogicalPage"/> decides them, so the name cannot be re-derived from a counter later and
    /// disagree; <see cref="WriteSucceeded"/> reports it beside the SUCCESSFUL status GR27 prescribes ("the WRITE
    /// statement is successful"), exactly as §13.18.34.4 GR6 b) 2's EC-I-O-LINAGE rides its status (kb/Work
    /// PB854 — both names were catalogued with no mask bit and no raise site, so neither ever existed).</summary>
    private string? _endOfPage;

    /// <summary>A WRITE's successful completion: '00', carrying GR27 a)'s exception-name when the write caused an
    /// end-of-page condition. EVERY successful return of a WRITE arm that can travel a logical page goes through
    /// here.</summary>
    private string WriteSucceeded() => _endOfPage is { } ec
        ? SetIoCondition(FileStatusCode.Success, ec)
        : Status = FileStatusCode.Success;

    /// <summary>⛔ THE §13.18.34.4 GR6 b) 2 LATCH — <i>"the LINAGE-COUNTER is set to 0 and remains at that value
    /// until the file is closed; and all subsequent WRITE statements referencing the file cause the
    /// EC-I-O-LINAGE exception condition to continue to exist until the file is closed"</i>. Set by
    /// <see cref="EvaluateLinage"/> when the operand values do not conform to GR6 b)'s two value rules, cleared
    /// only by <see cref="EndLinagePage"/> (CLOSE) — a fresh OPEN OUTPUT ends the file's open mode first, so
    /// "until the file is closed" and "cleared at CLOSE" are the same moment.
    /// <para>It is a SEPARATE bit from <see cref="HasLogicalPage"/> on purpose. A broken page has no geometry, so
    /// the model is zeroed with it; if the latch were read off the zeroed model, every later WRITE would take the
    /// plain print-stream arm — the arm for a file with NO LINAGE clause — and would SUCCEED, which is the one
    /// outcome GR6 b) 2's last clause forbids.</para></summary>
    private bool _linagePageBroken;

    /// <summary>ISO §13.18.34 GR6 b) 1 — establish the logical page model <i>"at the completion of an OPEN
    /// statement with the OUTPUT phrase"</i>, plus GR7 d)'s counter reset. Called by the registry after a
    /// SUCCESSFUL OPEN OUTPUT, with the page the EXECUTING element's own LINAGE clause evaluates to. Returns the
    /// §9.1.13.11 LINAGE value-rule status when §13.18.34.4 GR6 b)'s value rules are violated at the completion of the open,
    /// and <see langword="null"/> when they hold — <b>null, not '00'</b>, because the OPEN's own successful
    /// status is not always '00' ('05' for an absent OPTIONAL file, '07' for a phrase on a non-reel medium) and
    /// this evaluation has nothing to say about it.
    /// <para>⚠ A violation here does NOT un-open the connector: GR6 b) 2 pins the counter at 0 <i>"until the file
    /// is closed"</i> and makes <i>"all subsequent WRITE statements referencing the file"</i> re-raise, both of
    /// which presuppose an open file connector. What the OPEN reports is an unsuccessful I-O status (so the
    /// standard I-O exception processing of §9.1.12 / §14.9.49.4 GR6 sees it); what it leaves behind is an open
    /// connector with a broken page.</para></summary>
    public string? BeginLinagePage(LinagePage page)
    {
        _linagePageBroken = false;   // a fresh OPEN OUTPUT re-determines the values (GR6 b) 1)
        LinageCounter = 1;      // GR7d — the counter is set to one at OPEN OUTPUT
        _topMarginPending = true;   // …and the device is at body line 1, i.e. past this page's top margin (GR4)
        _endOfPage = null;
        return EvaluateLinage(page) ? null : LinageViolationStatus();
    }

    /// <summary>⛔ THE END OF THE LINAGE PAGE REGIME — the page model does NOT outlive the open mode that
    /// established it. §13.18.34.4 GR6 b) 1 determines the values "at the completion of an OPEN statement with
    /// the OUTPUT phrase" and GR7 d) sets the counter there; no rule establishes one for any other open mode.
    /// Without this reset a connector CLOSEd after OPEN OUTPUT and reopened in ANY other mode kept the closed
    /// file's page size, margins and counter and laid the old page's geometry over the new open — a stale model
    /// driving real bytes. Cleared here, such a write finds no <see cref="HasLogicalPage"/> and takes the plain print
    /// stream, which is what a mode with no logical page owes. (⚠ The EXTEND case is not legal source anyway —
    /// §14.9.27.3 SR2, "The EXTEND phrase shall be specified only if the access mode of the file connector
    /// referenced by file-name-1 is sequential and the LINAGE clause is not specified in the file description
    /// entry for file-name-1" — but that syntax rule is NOT diagnosed today, so the reset is what stands between
    /// that source and a stale page model; OPEN INPUT/I-O of a LINAGE file is legal and is covered by the same
    /// clearing.) The counter itself is left where the last write put it: §8.4.3.14
    /// gives LINAGE-COUNTER no value for a closed file, and the next OPEN OUTPUT sets it (GR7 d).</summary>
    private void EndLinagePage() =>
        (_pageBody, _footing, _top, _bottom, _topMarginPending, _linagePageBroken) = (0, null, 0, 0, false, false);

    /// <summary>Adopt the LINAGE operand values for the (next) logical page (ISO §13.18.34.4 GR6 b: at OPEN
    /// OUTPUT completion, during a WRITE ADVANCING PAGE, and during a page-overflow WRITE — <i>"the value applies
    /// to the next logical page"</i>), and APPLY GR6 b)'s two value rules. Returns whether the values conform.
    /// <list type="number">
    /// <item>§13.18.34.4 GR6 b) 1 — <i>"The page size shall be greater than zero."</i></item>
    /// <item>§13.18.34.4 GR6 b) 2 — <i>"The footing start shall be greater than zero and not greater than the
    ///   page size."</i> ⛔ APPLIED EXACTLY WHEN THE FOOTING PHRASE IS SPECIFIED (kb/Work PB525): there is a
    ///   footing start to validate only when there is a FOOTING phrase, and GR1 — <i>"If the FOOTING phrase is
    ///   not specified, no end-of-page condition independent of the page overflow condition exists"</i> — is the
    ///   rule for the absent one. A specified phrase holding 0 is a VIOLATION, not an absence, which is why
    ///   <see cref="LinagePage.Footing"/> is nullable and this test is on the null-ness, never on the value.</item>
    /// </list>
    /// <para>⛔ A VIOLATION IS AN EXCEPTION CONDITION, NOT A PROCESS KILL (kb/Work PB526). GR6 b) 2 continues:
    /// <i>"If the value does not conform to these two rules, the EC-I-O-LINAGE exception condition is set to
    /// exist. If execution continues after processing of this exception condition, it continues with the
    /// statement following the WRITE statement; the LINAGE-COUNTER is set to 0 and remains at that value until
    /// the file is closed; and all subsequent WRITE statements referencing the file cause the EC-I-O-LINAGE
    /// exception condition to continue to exist until the file is closed."</i> All three consequences are
    /// realized here and in <see cref="LinageViolationStatus"/>: the counter goes to 0, the page model is
    /// discarded, <see cref="_linagePageBroken"/> latches until CLOSE, and the operation reports the I-O status
    /// and the exception-name that carry the condition to §9.1.12's exception processing. The raise itself is
    /// NOT made here — it is the ONE EC-I-O channel every other I-O condition uses, the emitted
    /// <c>__IoCheckEc</c> hook after the verb (§9.1.13.1 / §14.6.13.1.3 #3), which owns the USE Format-1 and
    /// Format-3 tiers, the exception-checking PERFORM frames and the fatal default.</para></summary>
    private bool EvaluateLinage(LinagePage page)
    {
        var (body, footing, top, bottom) = page;
        if (body <= 0 || (footing is { } f && (f <= 0 || f > body)))
        {
            // GR6 b) 2's continuation, in the order the sentence writes it.
            LinageCounter = 0;
            (_pageBody, _footing, _top, _bottom, _topMarginPending) = (0, null, 0, 0, false);
            _linagePageBroken = true;
            return false;
        }
        (_pageBody, _footing, _top, _bottom) = (body, footing, top, bottom);
        return true;
    }

    /// <summary>The I-O status of an operation stopped by the §13.18.34.4 GR6 b) 2 LINAGE value-rule violation —
    /// set with the exception-name that rule NAMES, so §9.1.13.1's status→EC correspondence (which has no entry
    /// for EC-I-O-LINAGE) does not answer in its place. See <see cref="FileStatusCode.LinageValueViolation"/> for
    /// the determination behind the value and <see cref="FileConnector.IoConditionName"/> for why the name
    /// travels beside it.
    /// <para>⛔ EVERY PATH OUT OF A VIOLATION PASSES THROUGH HERE — the detecting operation's and every later
    /// WRITE's — so <see cref="EndOfPage"/> is cleared HERE and not in <see cref="EvaluateLinage"/>: the latched
    /// WRITE never re-evaluates anything, and leaving the previous write's flag standing would let a stale
    /// end-of-page condition answer for it. §14.9.51.4 GR27 makes an end-of-page WRITE SUCCESSFUL, so an
    /// unsuccessful one has none by construction.</para></summary>
    private string LinageViolationStatus()
    {
        _endOfPage = null;
        return SetIoCondition(FileStatusCode.LinageValueViolation, Exceptions.ExceptionCatalog.IoLinage);
    }

    /// <summary>
    /// ⛔ THE ONE PLACE A WRITE MOVES THE DEVICE ON A LOGICAL PAGE — the physical travel AND the
    /// LINAGE-COUNTER, decided together because they are one rule: §14.9.51.4 GR26 a) says what the counter
    /// does and where the device goes in the SAME sentence. <paramref name="lines"/> &lt; 0 = ADVANCING PAGE.
    /// Rules:
    /// <list type="bullet">
    /// <item>ADVANCING PAGE resets the counter to 1 (§13.18.34 GR7c1) and repositions the device — §14.9.51.4
    ///   GR25 g), <i>"The repositioning is to the first line that may be written on the next logical page as
    ///   specified in the LINAGE clause"</i>. ⛔ NOT a form feed: GR25 h) is the form-feed arm and it is the
    ///   NO-LINAGE case. No observable end-of-page (§14.9.51 SR18 bars PAGE+EOP in one statement).</item>
    /// <item>ADVANCING n travels n lines and adds n (GR7c2/GR25a); a plain WRITE travels 1 and adds 1 (GR7c3 —
    ///   the caller passes 1). n = 0 is GR25 c)'s "no repositioning".</item>
    /// <item>Counter past the page body ⇒ page overflow (§14.9.51 GR26a): the device is <i>"repositioned to the
    ///   first line that may be written on the next logical page"</i> — over the rest of THIS page's body, its
    ///   bottom margin and the next page's top margin, all of which are lines of the logical pages GR1 defines
    ///   and GR8 lays contiguously — and counter := 1 (GR7c4 — never a modulo carry), overflow end-of-page.</item>
    /// <item>Else, FOOTING specified and counter at/past the footing start ⇒ footing end-of-page (GR26b).</item>
    /// <item>⚖ <b>counter == page body IS AN ADJUDICATED BOUNDARY — do not "correct" either comparison to match
    ///   GR26's printed words.</b> Arm a) as printed fires at counter ≥ page size and arm b) is clamped to
    ///   counter &lt; page size; at counter == page size those cannot both hold with §13.18.34 GR2 (all
    ///   page-size lines "may be written or spaced"), GR3 (the footing area is [footing, page size]
    ///   INCLUSIVE) or GR26's own lead sentence (the lines "do not fit within the current page body"). Under
    ///   the printed arm a) the line NUMBERED page size could never receive a record — an N-line body would
    ///   hold N−1 written lines forever, FOOTING phrase or not. The strict boundary below is
    ///   docs/CONFORMANCE.md §4 "DETERMINATION — the §14.9.51.4 GR26 a)/b) boundary at LINAGE-COUNTER = page
    ///   size" (kb/Work PB686), which carries the survey and the NIST SQ201M evidence; it is pinned at the
    ///   boundary by tests/conformance/2023/pb686_linage_gr26_boundary.cob (+ the 85 twin) on BOTH arms of the
    ///   FOOTING dispatch, and by LinageConformanceTests.Gr26ab_CounterEqualsBody_IsFootingEopNotOverflow.</item>
    /// <item>GR6b2/3: at the two page transitions — AFTER the overflow decision was made against the OLD page
    ///   body AND after all positioning on the current page (GR6 b) 2's own words: <i>"This occurs before the
    ///   device is positioned and after all positioning on the current page"</i>) — re-evaluate the operand
    ///   values; they apply to the NEXT logical page (§13.18.34 GR6). <see cref="BeginNextLogicalPage"/> is
    ///   split along exactly that sentence.</item>
    /// </list>
    /// The AT END-OF-PAGE branch observes the POST-advance counter (SQ201M's footing lines print the triggering
    /// write's line number), which holds however the caller orders this against the record's presentation:
    /// §14.9.51.4 GR25 e)/f) place the advance before or after the line, and both orders leave the same counter.
    /// </summary>
    private bool PositionOnLogicalPage(int lines, LinagePage page)
    {
        _endOfPage = null;   // reset at the start of every counter-advancing write (the legacy entry reset)
        // The device is on the page body only once this page's top margin is behind it (GR4); every travel and
        // every presentation materializes it first, so the two call sites agree by construction.
        EmitTopMarginIfPending();
        if (lines < 0)
        {
            // ADVANCING PAGE (§14.9.51.4 GR25 g) + §13.18.34 GR7c1): the device is repositioned to the first
            // line that may be written on the next logical page — never GR25 h)'s form feed, which is the arm
            // for a file with NO LINAGE clause.
            return BeginNextLogicalPage(page);   // GR6 b) 2 — the re-evaluation may break the page
        }
        // ADVANCING n (n >= 0) or plain WRITE (n = 1): the counter is incremented (GR7c2/c3) and the device
        // travels the same n lines (GR25 a); n = 0 is GR25 c)'s "no repositioning ... is performed").
        if (LinageCounter + lines > _pageBody)
        {
            // Page overflow (§14.9.51 GR26a): the line does not fit in the page body — the device repositions
            // to the first writable line of the succeeding page and the counter resets to 1 (GR7c4).
            // ⚖ STRICT `>`, NOT `>=` — the adjudicated boundary (docs/CONFORMANCE.md §4, kb/Work PB686). GR26a
            // as printed says "equal to or exceeds the page size"; `>=` here would push a record that lands on
            // the LAST body line onto the next page and make that line unwritable forever, against §13.18.34
            // GR2. The doc comment above carries the full derivation and the survey.
            if (!BeginNextLogicalPage(page)) return false;   // GR6 b) 3's re-evaluation broke the page
            _endOfPage = Exceptions.ExceptionCatalog.IoEopOverflow;   // §14.9.51.4 GR27 a) — caused by GR26 a)
            return true;
        }
        AdvanceLines(lines);
        LinageCounter += lines;
        if (_footing is { } footingStart && LinageCounter >= footingStart)
        {
            // Footing-area end-of-page (§14.9.51 GR26b): FOOTING is specified and this WRITE prints or spaces
            // within the footing area (counter at/past the footing start, still within the page body).
            // ⚖ NO UPPER CLAMP — the adjudicated boundary (docs/CONFORMANCE.md §4, kb/Work PB686). GR26b as
            // printed adds "and is less than the page size"; honouring that clamp would exclude the page-size
            // line, which §13.18.34 GR3 places INSIDE the footing area ("between the footing start and the
            // page size, inclusive"). The overflow arm above already took every counter past the body, so
            // reaching here means counter ≤ page body and the clamp has nothing left to exclude but GR3's own
            // last line. IBM Enterprise COBOL documents the footing condition with no upper clamp likewise.
            // ⛔ The test is on the PHRASE'S PRESENCE, not on a positive value (kb/Work PB525): a specified
            // footing start is in (0, page size] by GR6 b) 2, which EvaluateLinage has already enforced, so a
            // `_footing > 0` guard here would be re-deciding presence from a value — the sentinel collision.
            _endOfPage = Exceptions.ExceptionCatalog.IoEop;   // §14.9.51.4 GR27 a) — caused by GR26 b)
        }
        return true;
    }

    /// <summary>⛔ THE ONE PAGE TRANSITION, shared by §14.9.51.4 GR25 g)'s ADVANCING PAGE and GR26 a)'s page
    /// overflow because the standard writes the SAME destination for both — <i>"the first line that may be
    /// written on the next logical page"</i>. The travel is derived from the LOGICAL PAGE, never from the
    /// WRITE's own line count, and it is split in three along §13.18.34.4 GR6 b)'s own sentence
    /// (<i>"before the device is positioned and after all positioning on the current page"</i>):
    /// <list type="number">
    /// <item>all remaining positioning on the CURRENT page — its unwritten body lines plus its bottom margin
    ///   (GR5), against the OLD page model;</item>
    /// <item>the operand re-evaluation (GR6 b) 2 for ADVANCING PAGE, GR6 b) 3 for overflow), whose values
    ///   <i>"apply to the next logical page"</i>;</item>
    /// <item>one line onto the next page's first line — GR8's <i>"Each logical page is contiguous to the next
    ///   with no additional spacing provided"</i> is what makes it exactly one — leaving the device on page
    ///   line 1 with the NEW top margin still to be materialized, which is the same state OPEN OUTPUT leaves
    ///   (<see cref="BeginLinagePage"/>). The two page starts are therefore ONE state, not two.</item>
    /// </list>
    /// The counter is reset here and nowhere else for a transition (§13.18.34 GR7 c) 1 and c) 4).</summary>
    /// <returns><see langword="false"/> when step 2's re-evaluation found values that violate §13.18.34.4 GR6 b)
    /// — step 3 does NOT happen (there is no next logical page to step onto), the counter is 0 rather than 1, and
    /// the caller abandons the rest of the WRITE. Step 1 has already happened, and correctly so: GR6 b) 2/3 place
    /// the re-evaluation <i>"after all positioning on the current page"</i>, and that positioning belonged to the
    /// page whose values were still valid.</returns>
    private bool BeginNextLogicalPage(LinagePage page)
    {
        // 1. the rest of THIS page. The narrowing is total: LINAGE-COUNTER is a long only because
        // §8.4.3.14 gives the register the widest numeric carrier, and the clamped difference cannot
        // exceed _pageBody, an int.
        AdvanceLines((int)Math.Max(0, _pageBody - LinageCounter) + _bottom);
        if (!EvaluateLinage(page)) return false;                               // 2. GR6 b) 2 / b) 3
        AdvanceLines(1);                                                       // 3. onto the next page (GR8)
        LinageCounter = 1;
        _topMarginPending = true;
        return true;
    }

    /// <summary>Materialize the current logical page's top margin (§13.18.34.4 GR4) — the <c>_top</c> lines
    /// between page line 1, where <see cref="BeginLinagePage"/> and <see cref="BeginNextLogicalPage"/> leave
    /// the device, and body line 1, where <see cref="LinageCounter"/> already says it is. Idempotent, and
    /// called from BOTH the travel and the presentation so neither can reach the page body without it.</summary>
    private void EmitTopMarginIfPending()
    {
        if (!_topMarginPending) return;
        _topMarginPending = false;
        AdvanceLines(_top);
    }

    /// <summary>The device travel of ONE WRITE, whichever kind of file it is: a logical page for a file with an
    /// established LINAGE page model, and the plain print stream (<see cref="Advance"/>, form feed included)
    /// otherwise. <paramref name="page"/> null = the FD has no LINAGE clause; a non-null page with
    /// no <see cref="HasLogicalPage"/> is a LINAGE FD opened in a mode that establishes no page — §13.18.34.4
    /// GR6 b) 1 and GR7 d) name OPEN OUTPUT alone — and it too takes the plain stream.</summary>
    /// <returns><see langword="false"/> when the travel crossed a page boundary whose re-evaluated LINAGE values
    /// violate §13.18.34.4 GR6 b) — the caller shall then abandon the rest of the WRITE (GR6 b) 2: <i>"it
    /// continues with the statement following the WRITE statement"</i>).</returns>
    private bool Position(int lines, LinagePage? page)
    {
        if (page is { } pg && HasLogicalPage) return PositionOnLogicalPage(lines, pg);
        Advance(lines);
        return true;
    }

    /// <summary>Present one line on the current logical page — the record, preceded by the page's top margin
    /// when this is the first thing written on the page (§13.18.34.4 GR4). ⛔ EVERY record that reaches a
    /// LINAGE file's medium goes through here or through <see cref="EmitLineSequentialRecord"/>, so a page's
    /// margin cannot be skipped by adding a write arm.</summary>
    private void Present(string text, LinagePage? page)
    {
        if (page is not null && HasLogicalPage) EmitTopMarginIfPending();
        EmitRecord(text);
        _lineOpen = true;   // presented, not yet travelled past
    }

    /// <summary>True between a successful OPEN and the matching CLOSE (an absent-OPTIONAL INPUT open counts —
    /// the connector is open at EOF with no physical stream).</summary>
    public SequentialConnector(string hostPath, int recordWidth, bool lineSequential,
        int varyMin = -1, int varyMax = -1)
        : base(hostPath, recordWidth, varyMin, varyMax)
    {
        _lineSequential = lineSequential;
        _lineEnd = lineSequential ? LineSequentialDelimiter : PrintLineEnd;
    }

    /// <summary>⛔ THE LINE SEQUENTIAL LINE DELIMITER — Annex A.1 item 114, <c>docs/CONFORMANCE.md</c>
    /// <c>DOC-A.1-114</c>: the HOST platform's newline (CR LF on Windows, LF on Linux and macOS). §12.4.5.10.3
    /// GR2: <i>"Each record in a line sequential file is terminated by an implementor-defined line
    /// delimiter"</i>. The standard leaves it open, so CLAUDE.md rule 1's precedent decides: GnuCOBOL writes a
    /// line sequential file in the C library's text mode, i.e. the host newline. (It was CR LF on every host
    /// until kb/Work PB1540, so a file written on Linux was not a Linux text file.) The READ side accepts BOTH
    /// forms on every host — see <see cref="ReadPhysicalLine"/>.</summary>
    internal static readonly string LineSequentialDelimiter = Environment.NewLine;

    /// <inheritdoc/>
    protected override bool LineSequential => _lineSequential;

    /// <summary>The PRINT STREAM's line control — the line end of a record sequential file written with an
    /// ADVANCING phrase or a LINAGE clause (the one on-disk shape a record sequential print file has; the
    /// report writer's lines are the same stream). CR LF on every host. It is not a line sequential file, so
    /// item 114 does not govern it; kb/Work PB1540 changed the line sequential delimiter only.</summary>
    private const string PrintLineEnd = "\r\n";

    /// <summary>This connector's ONE line end — <see cref="LineSequentialDelimiter"/> on a line sequential file,
    /// <see cref="PrintLineEnd"/> on a record sequential print stream. Every line this connector ends — a
    /// record's delimiter, an ADVANCING or LINAGE line, the CLOSE of an open line — writes exactly this, so a
    /// margin line, a page-fill line and a record's delimiter are the same bytes on one file.</summary>
    private readonly string _lineEnd;

    /// <inheritdoc/>
    /// <remarks>Both §9.1.7.2 types of sequential file — record sequential and line sequential — record this
    /// ONE organization, because §9.1.6 names exactly three ("There are three organizations: sequential,
    /// relative, and indexed") and the delimiter that separates the two types is §9.1.6's SEPARATELY listed
    /// <i>record delimiter</i>, not a fourth organization. Neither sequential format RECORDS this organization
    /// on the medium — a fixed-length record sequential file is plain bytes and a line sequential file is plain
    /// text — so it is never compared either; see <see cref="FixedAttributeConflict"/>.</remarks>
    protected override string DeclaredOrganization => FixedFileAttributes.Sequential;

    /// <inheritdoc/>
    /// <remarks>⛔ THE SEQUENTIAL ORGANIZATION'S WHOLE §14.9.27.4 GR10 ANSWER, and it is ONE question: does a
    /// RECORD VARYING record-sequential file's own record-length framing PARSE? Nothing else about a sequential
    /// file is recorded on the medium to be contradicted.
    /// <list type="bullet">
    /// <item><b>Fixed-length record sequential — nothing.</b> §9.1.7.2: "In record sequential files the length
    /// of each record is determined by any information the implementor may add to the record on the physical
    /// storage medium (such as record length headers)", and this processor adds none to a fixed-length one. The
    /// file is plain bytes and states no §9.1.6 attribute at all.</item>
    /// <item><b>Line sequential — nothing.</b> §9.1.7.2: "In line sequential files the length of each record is
    /// determined by the number of characters between the preceding line delimiter and the following line
    /// delimiter or the end of file if no line delimiter is present". Delimiters encode no attribute, and the
    /// file being plain text is the interchange property the shape exists for.</item>
    /// <item><b>RECORD VARYING record sequential — the prefix.</b> The 4-byte length prefix IS §9.1.7.2's
    /// "record length header", so a file whose first prefix names more bytes than the file holds is not a framed
    /// file and the declared VARIABLE record type contradicts the medium: '39'
    /// (<see cref="RecordFraming.StreamFramingParses"/> carries the rule and its boundaries).</item>
    /// </list>
    /// <para>⛔ THE RECORD SIZES ARE NOT COMPARED, ON ANY SEQUENTIAL FILE, AND THAT IS A DETERMINATION RATHER
    /// THAN AN OMISSION. §9.1.13.2 answers every disagreement a sequential re-read can produce with a SUCCESSFUL
    /// completion — item 3's '04' ("A READ statement is successfully executed but the physical record from the
    /// file is shorter than or longer than the minimum or maximum length of records allowed for the fixed file
    /// attributes for that file"), item 5's '06' and item 7's '09' — and writing a print, report or extract file
    /// and reading it back under a different record description is exactly the idiom those three exist for. It
    /// was MEASURED: a validated record size took six conforming programs of this repository's own corpus red,
    /// one of them into an infinite READ loop. <see cref="RecordLayoutNotice"/> is the one mechanism for the
    /// arithmetic case, and it leaves the I-O status alone.</para>
    /// <para>⛔ AND ONE ATTRIBUTE THE PHYSICAL FILE ITSELF STATES: ITS ORGANIZATION, WHEN IT IS A KEYED STORE
    /// (kb/Work PB1098). The questions above ask what THIS organization's format records; a relative or indexed
    /// store records its own organization in its header (<see cref="RecordFraming.ReadHeader"/>, the §9.1.6
    /// attributes of kb/Work PB802), and §12.4.5.10.3 GR1 says what that attribute means — <i>"The file
    /// organization is established at the time a physical file is created and cannot subsequently be
    /// changed."</i> An OPEN that would let this connector WRITE into such a file — <c>I-O</c> (whose REWRITE
    /// overwrote the header, after which the file opened only with '39' under its own description) and
    /// <c>EXTEND</c> (whose records would follow the frames as bytes no store can parse) — is therefore the file
    /// attribute conflict, '39'. <c>INPUT</c> changes nothing and stays unvalidated (PB802's stated consequence,
    /// GnuCOBOL's spirit), and <c>OUTPUT</c> never reaches this question: it CREATES a new physical file, which
    /// establishes an organization afresh (§14.9.27.4 GR18). The header read is the same bounded auxiliary open
    /// <c>KeyedConnector.FixedAttributeConflict</c> takes, paid only by the two writing modes.</para></remarks>
    protected override bool FixedAttributeConflict() =>
        (IsVarying && !_lineSequential && !RecordFraming.StreamFramingParses(HostPath))
        || (Mode is FileOpenMode.IO or FileOpenMode.Extend
            && RecordFraming.ReadHeader(HostPath, out _) is StoreFormat.Described);

    /// <summary>§14.9.6.4 GR2 a) — <i>"A file whose input or output medium is such that the concepts of rewind
    /// and units have no meaning."</i> A sequential connector holds one <see cref="FileConnector.HostPath"/> on
    /// mass storage; there is no reel, unit or volume behind it. FORCED, not chosen: §9.1.13.2 item 6 defines
    /// the '07' this connector reports for the NO REWIND and REEL/UNIT phrases as the status of a CLOSE that
    /// <i>"references a physical file on a non-reel/unit medium"</i>, and Table 14 prints symbol g only in the
    /// Non-unit column (kb/Work PB235; documented at `docs/CONFORMANCE.md` §7, A.1 item 24).</summary>
    public override PhysicalFileCategory Category => PhysicalFileCategory.NonUnit;

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The sequential OPEN body (ISO §14.9.25 / §9.1.13.4) — the shared preamble/guards, the ONE
    /// presence probe behind <paramref name="presence"/>, and §14.9.27.4 GR3's '37' all live on
    /// <see cref="FileConnector.Open"/>.</summary>
    protected override string OpenCore(FileOpenMode mode, FilePresence presence)
    {
        _printControl = false;
        _lineOpen = false;
        _lastReadBlockStart = -1;
        _lastReadTableCount = -1;
        _readOffset = 0;
        _lineRemainder = null;
        _lineByteOffset = 0;
        _lastLineStart = -1;
        _lastLineBytes = 0;
        _lastReadLinePartial = false;
        _readOrdinal = 0;
        // ⛔ CLEARED BY EVERY OPEN, so a COBOL-85 REVERSED opening cannot outlive itself: the phrase belongs to
        // the OPEN statement that wrote it, and ReversedPhraseEffect re-establishes it AFTER this body runs
        // (kb/Work PB668). Without this, `OPEN INPUT F REVERSED. CLOSE F. OPEN INPUT F.` read backward.
        Reversed = false;
        _reversedRecordCount = 0;
        _varyingStarts = null;   // rebuilt on demand against THIS open's physical file
        // §9.1.16 record-lock identity: this connector has released nothing yet. The MINT lives on the shared
        // physical-file state (kb/Work PB739), and only the OUTPUT and EXTEND arms below touch it — INPUT and
        // I-O release no records through this connector and shall not disturb another connector's numbering.
        _writeOrdinal = 0;
        // Table 18's "file is available" / "file is unavailable" axis. The base has already answered
        // §14.9.27.4 GR3, so on every mode that consults this an Unauthorized probe has become '37' and can
        // never be read here as "unavailable" (kb/Work PB323).
        bool exists = presence is FilePresence.Present;
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!exists)
                    {
                        if (!IsOptional) return FileStatusCode.FileNotFound;
                        OptionalAbsent = true;                 // positioned at EOF (ISO §9.1.13.2 item 5b)
                        return FileStatusCode.OptionalFileNotFound;
                    }
                    // The §9.1.15 file lock this connector's handle carries is DERIVED — by FileLockPosture,
                    // from the arbitrated sharing mode and the connectors Table 19 has already admitted on this
                    // physical file — and handed down by the registry as HostShare (kb/Work PB740). It is never
                    // decided here: the Table-19 registry is the in-run-unit arbiter and the handle's share mode
                    // is the file lock against OTHER RUN UNITS, and a boolean cannot be both.
                    _reader = OpenReader(HostShare, at: 0, fallbackShare: null);
                    NoticeIfLayoutDisagrees();
                    // OPEN INPUT … REVERSED: the count its position needs is taken HERE, inside the OPEN's try,
                    // so a host failure is this OPEN's '30' (see _reversedRecordCount).
                    if (ReversedRequested) _reversedRecordCount = ExistingRecordCount();
                    break;

                case FileOpenMode.Output:
                    // Table 19 admits an incoming EXTEND/I-O against a connector already open in the OUTPUT mode
                    // (its existing-side column group is `extend I-O output`), so the OUTPUT writer takes the
                    // same posture-derived role the EXTEND writer does — a second writer is possible here too
                    // (kb/Work PB740; the anchoring rule itself is PB739's).
                    _writer = OpenWriter(FileMode.Create, HostShare, fallbackShare: null);
                    // OPEN OUTPUT truncates, so the physical file holds no records and the shared mint restarts
                    // at 0. §14.9.51.4 GR17 is the sequential-organization rule for it: "The successor
                    // relationship of a sequential file is established by the order of execution of WRITE
                    // statements when the physical file is created" — a creation starts the relationship over,
                    // so the first record released is ordinal 1. (§9.1.15 item 3 does not admit output as a
                    // concurrent mode, so no other connector is numbering against this file at the time.)
                    if (SharedPhysical is { } outShared) outShared.ReleasedOrdinal = 0;
                    break;

                case FileOpenMode.Extend:
                    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
                    // EXTEND is checked for the SAME arithmetic reason as INPUT, and the stakes are higher: a
                    // program that APPENDS to a file whose existing layout disagrees writes a file interleaving
                    // two record layouts, which is permanent corruption rather than a wrong computation.
                    NoticeIfLayoutDisagrees();
                    // ⛔ THE WRITE-ORDINAL BASE IS MEASURED HERE, BEFORE THE WRITER HANDLE EXISTS (kb/Work
                    // PB713). §14.9.51.4 GR19 — "the added records follow the records present in the physical
                    // file when it was opened" — is what makes this point the right one, and an OPEN EXTEND
                    // writes nothing, so the count is the same number the completed OPEN would have seen. See
                    // ExistingRecordCount for why the ORDER, not merely the share mode, is the fix.
                    // What it seeds is the PHYSICAL FILE's mint, not a base of this connector's own (kb/Work
                    // PB739): a second connector extending the same file measures the same physical file and
                    // writes the same number, and from then on the two share one ascending sequence. A file
                    // that is not there holds no records, so the mint is 0.
                    if (SharedPhysical is { } extShared)
                        extShared.ReleasedOrdinal = exists ? ExistingRecordCount() : 0;
                    // ⛔ NOT FileMode.Append THROUGH OpenConnectorStream (kb/Work PB739). .NET's Append seeks
                    // to the end ONCE, at open; under §9.1.15 sharing two connectors then anchor at the same
                    // offset and the later flush lands on top of the earlier record.
                    // HostFile.OpenConnectorWriteStream is the role that knows the difference, and it reads the
                    // difference off the POSTURE — a handle whose file lock admits another writer gets the
                    // repositioning stream — so a clause-less pair the arbiter permits is covered by the same
                    // rule as a SHARING WITH ALL OTHER pair (kb/Work PB740).
                    _writer = OpenWriter(FileMode.Append, HostShare, fallbackShare: null);
                    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
                    break;

                case FileOpenMode.IO:
                    // I-O permits both READ and REWRITE on the one open connector (ISO §14.9.35 GR3 — REWRITE
                    // requires open mode I-O; its format-1 contract replaces the record retrieved by the last
                    // successful READ in place), so the underlying stream must open ReadWrite — Rewrite's
                    // seek-and-write path writes through the reader's BaseStream. An absent non-optional file
                    // is 35; an optional one is created (05).
                    // §14.9.27 GR17: an ABSENT OPTIONAL file opened I-O is CREATED (as if OPEN OUTPUT then CLOSE),
                    // then opened I-O like an existing file — so it always ends with a ReadWrite _reader and the FPI is
                    // effectively 1 (GR14). A first READ then finds no record → AtEnd '10' (§14.9.30 GR21 rule e + GR24),
                    // NOT '47' (which is only for a connector not open in input/I-O, §9.1.13.7 item 7). OPEN still
                    // returns '05'. No OptionalAbsent needed — the file now physically exists (empty).
                    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
                    // create empty, then close — an auxiliary handle, not the connector's stream
                    if (!exists) using (HostFile.OpenAuxiliary(HostPath, FileMode.Create, FileAccess.Write)) { }
                    // ReadWrite access and NO FileOptions.SequentialScan for this mode — both decided once, in
                    // OpenReader, which is also what the reposture rebuild uses.
                    _reader = OpenReader(HostShare, at: 0, fallbackShare: null);
                    NoticeIfLayoutDisagrees();
                    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
                    break;
            }
            // ISO §13.18.34 GR6 b) 1 reads the operand values "at the COMPLETION of an OPEN statement with the
            // OUTPUT phrase", so the page model is established by the registry's OPEN dispatch once this body has
            // succeeded (FileRegistry.SharedOpenAttempt → BeginLinagePage) — with the page the EXECUTING element's
            // LINAGE clause evaluates to, which is why it cannot be read from connector state here (PB673).
            return FileStatusCode.Success;
        }
    }

    /// <inheritdoc/>
    /// <remarks>The sibling of <see cref="CloseCore"/>'s stream disposal, for the OPEN that never became one
    /// (kb/Work PB771). §9.1.15 establishes a file lock on <i>"The SUCCESSFUL opening of a file"</i>, so a body
    /// that opened its reader or writer and then failed — or threw into <c>FileConnector.Open</c>'s catch, where
    /// the result is '37' or '30' — shall not leave the handle behind: <c>Close()</c> answers '42' for a
    /// connector that is not open and would never dispose it. No arm reaches this state today; the override is
    /// what keeps that true of the arms added later, and it is the same two lines the CLOSE runs.</remarks>
    protected override void AbandonOpen()
    {
        // Both handles go whatever either disposal throws: a writer whose final flush is refused would otherwise
        // leave the reader held — a file lock no COBOL statement can remove (kb/Work PB771's reason for this
        // member) — and FileConnector.Open reports the refusal as nothing more than the OPEN's own status.
        try { _reader?.Dispose(); }
        finally
        {
            try { _writer?.Dispose(); }
            finally { _reader = null; _writer = null; }
        }
    }

    /// <summary>Report a FIXED-LENGTH record-sequential file whose byte length is not a whole multiple of its
    /// record length (<see cref="RecordLayoutNotice"/>) — the arithmetic proof that the file's layout and the
    /// record description disagree, raised at OPEN rather than at the trailing '04' a program may never inspect.
    /// Called on every mode that consults an EXISTING file's bytes — INPUT, I-O and EXTEND. OUTPUT is excluded
    /// because it truncates: whatever the file held is discarded, so nothing can disagree.
    /// Excluded, because a differing physical length is NORMAL for them: LINE SEQUENTIAL (records are delimited,
    /// §9.1.13.2) and RECORD IS VARYING (each record carries its own length prefix, so a mismatch is detected
    /// per record — §14.9.30.4 GR14).</summary>
    private void NoticeIfLayoutDisagrees()
    {
        if (_lineSequential || IsVarying) return;
        try
        {
            // No existence test at all — not FileInfo.Exists (which swallows an access error into "no file"
            // exactly as File.Exists does, kb/Work PB323) and not HostFile.Probe (a second syscall for an
            // answer the very next one already gives). FileInfo.Length raises FileNotFoundException for an
            // absent file and UnauthorizedAccessException for a refused one, and BOTH catches below mean the
            // same thing here: no notice. A notice is never worth failing an OPEN over.
            RecordLayoutNotice.CheckFixedLengthFile(HostPath, new FileInfo(HostPath).Length, RecordWidth);
        }
        catch (IOException) { }              // absent, or unreadable — either way, nothing to report
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>The sequential CLOSE body (ISO §14.9.7). A print stream whose last line is still unterminated —
    /// an AFTER write's record, presented after its advance (§14.9.51.4 GR25 f)) — is ended with one line
    /// terminator; a stream whose last travel already ended the line (a BEFORE write, GR25 e)) gets none
    /// (<see cref="_lineOpen"/>, kb/Work PB864). The report writer's lines are AFTER-placed and reach this same
    /// CLOSE. The not-open guard lives on <see cref="FileConnector.Close"/>.</summary>
    protected override string CloseCore()
    {
        try
        {
            // An unterminated line — an AFTER write's record — is ended here; a line some travel already ended
            // gets nothing (kb/Work PB864).
            if (_lineOpen) { _writer?.Write(_lineEnd); _lineOpen = false; }
            _reader?.Dispose();
            _writer?.Dispose();   // a flush failure here maps to '30' on FileConnector.Close (§9.1.13.6 item 1)
        }
        finally
        {
            // Whatever the outcome, the streams never survive the CLOSE — a stale disposed _writer after a
            // reopen-for-INPUT would take the WRITE arms (kb/Work PB140). OptionalAbsent (the FPI's "not
            // present" state) DOES survive — §14.9.6.4 GR6; the next OPEN resets it.
            _reader = null;
            _writer = null;
            EndLinagePage();   // …and neither does the logical page — see EndLinagePage
        }
        ModeKnown = false;   // §9.1.4 — after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
    }

    // ── The line sequential character set (ISO Annex A.1 item 115) ───────────────────────────────────────────

    /// <summary>⛔ THE ONE LINE-CHARACTER-SET TEST of a record area, shared by every direction so READ and WRITE
    /// can never disagree about what a line sequential character is (kb/Work PB329): §14.9.30.4 GR16 answers it
    /// with '09' on a successful READ, §14.9.51.4 GR23 and §14.9.35.4 GR17 d) with '71' on an unsuccessful
    /// WRITE / REWRITE. False for a record sequential file — all three rules are organization-scoped ("for a
    /// line sequential file"), and a record sequential record is plain bytes that must round-trip byte-exact.
    /// The set and its Annex A.1 item 115 derivation live in <see cref="LineSequentialCharacterSet"/>.
    /// <para>The subject is the RECORD AREA, which is what all three rules name — not the trimmed image a line
    /// sequential WRITE presents, and not the length-limited prefix a varying record transfers. Trailing area
    /// positions are spaces, which are IN the set, so the wider subject cannot manufacture a status; a character
    /// outside the set anywhere in the area is what the rules ask about.</para>
    /// <para>The set's alphanumeric ceiling is the file's coded character set, so the connector's CODE-SET
    /// conversion travels with the question (kb/Work PB1542): a character a STANDARD-1 CODE-SET cannot represent
    /// is '71' on the WRITE, exactly as '91' on every other organization, and a medium byte outside that set is
    /// '09' on the READ.</para></summary>
    private bool RecordAreaOutsideLineCharacterSet(ReadOnlySpan<char> recordArea) =>
        _lineSequential && LineSequentialCharacterSet.HasCharacterOutside(recordArea, NationalRecordArea, CodeSet);

    // ── WRITE ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.46): line-sequential writes the trimmed image as a line;
    /// record-sequential writes a fixed-width block; a VARYING file writes a length-framed record of
    /// <paramref name="length"/> bytes (the DEPENDING item's content, §13.18.43 GR13a) or the image's own length
    /// (GR13b/c), failing with '44' outside the declared bounds (GR14). Valid only when open OUTPUT/EXTEND
    /// (else 48).</summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is written
    /// (determination D-FRA (v); kb/Work PB1053) — framed beside a RECORD VARYING record-sequential record, and
    /// carried by no other shape (a line has no frame).</param>
    public string Write(string image, int length, LinagePage? page, RecordExtents? extents = null)
    {
        try { return WriteRecord(image, length, page, extents); }
        catch (IOException refused) { return Status = FileStatusCode.ForWriteFailure(refused); }   // '34' / '30'
    }

    /// <summary>⛔ THE WRITE-SIDE MEDIUM BOUNDARY'S ONE CATCH SHAPE (kb/Work PB1192), written at each of the three
    /// public entry points a record reaches the medium through — <see cref="Write"/>, <see cref="WriteAdvancing"/>
    /// and <see cref="Rewrite"/> — and nowhere below them. A host refusal to store the bytes is the file's
    /// externally-defined boundary when the host says the medium is exhausted (§14.9.51.4 GR20 / §9.1.13.6
    /// item 3's '34' — <see cref="HostFile.IsMediumBoundary"/>; Annex A.1 item 108, DOC-A.1-108) and §9.1.13.6
    /// item 1's '30' otherwise; before this, the <see cref="IOException"/> escaped the statement and killed the run
    /// unit. The writer BUFFERS (a flush per record is paid only when a sibling connector may read the file —
    /// <see cref="ReleaseRecord"/>), so the refusal surfaces on the WRITE whose record fills the buffer: that
    /// WRITE is the one the boundary is reported on, and a CLOSE whose final flush is refused answers '30'
    /// through <see cref="FileConnector.Close"/>'s own catch.</summary>
    private string WriteRecord(string image, int length, LinagePage? page, RecordExtents? extents)
    {
        _endOfPage = null;   // an end-of-page condition is the CURRENT write's or none (§14.9.51.4 GR27)
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        // §13.18.34.4 GR6 b) 2 — "all subsequent WRITE statements referencing the file cause the EC-I-O-LINAGE
        // exception condition to continue to exist until the file is closed". Ahead of everything, because the
        // statement is not executed at all: nothing reaches the medium, nothing is released (GR12), the counter
        // stays 0. Both WRITE entries carry it because a plain WRITE on a LINE SEQUENTIAL LINAGE file does not
        // route through WriteAdvancing (the two-arm dispatch this connector has been bitten by twice).
        if (_linagePageBroken) return LinageViolationStatus();
        // On a PRINT file (this connector has seen print-control advancing) — or a LINAGE file, which is
        // line-oriented from its FIRST write (the FILES deep-dive rule; its plain WRITE acts as AFTER
        // ADVANCING 1, ISO §14.9.51 GR25, and the counter advances by one, §13.18.34 GR7c3) — an omitted
        // ADVANCING phrase still line-advances (a raw fixed-width block would weld onto the previous line). In
        // the pending-advance stream model (each AFTER-write leads with its newline; CLOSE supplies the final
        // one), the write-then-advance shape reproduces the print stream the golden corpus encodes — on a line
        // nothing left OPEN. Where a line IS open (an AFTER write's record, presented after its advance), the one
        // implicit advance is placed FIRST, as GR25 f) places AFTER's: see ImplicitAdvanceIsBefore (kb/Work PB964).
        if ((_printControl || page is not null) && !_lineSequential)
            return WriteAdvancingRecord(image, length, 1, before: ImplicitAdvanceIsBefore, page);
        // §14.9.51.4 GR23: "For a line sequential file, if the record area contains one or more characters that
        // are not in the implementor-defined character set defined for a line sequential file, the execution of
        // the WRITE statement is unsuccessful and the I-O status in the write file connector is set to '71'."
        // Ahead of every shape branch because the rule is organization-scoped, not shape-scoped, and ahead of
        // any stream traffic because §9.1.13.10 item 1 requires the record area (and the medium) to be left
        // unchanged. This arm did not exist before kb/Work PB329 — only REWRITE's GR17 d) twin did.
        if (RecordAreaOutsideLineCharacterSet(image)) return Status = FileStatusCode.LineRecordInvalidChar;
        // Owner decision kb/Work R47 (Annex A.1 item 31): a record holding a character with no byte image in the
        // file coded character set is REFUSED, never written as '?' — the record sequential twin of the '71'
        // above, which already covers a line sequential file (its character set excludes the same characters).
        // Asked of what the WRITE TRANSFERS: a varying record's positions past its length never reach the medium.
        int len = WrittenLength(image, length);
        if (!_lineSequential && RecordHasCharacterWithoutByteImage(image.AsSpan(0, Math.Min(image.Length, len))))
            return Status = FileStatusCode.CharacterWithoutByteImage;   // '91' §9.1.13.11 (DOC-A.1-110)
        if (OutsideVaryingBounds(len)) return Status = FileStatusCode.RecordSizeViolation;   // '44' §14.9.51.4 GR14
        if (IsVarying)
        {
            if (_lineSequential) { if (!EmitLineSequentialRecord(TrimRecordEnd(FitRecord(image, len)), page)) return LinageViolationStatus(); }
            else
            {
                // The table describes the image the group composed; a record sent at another length (GR13 a) is
                // not that image, so it goes without one rather than with a table that would misdescribe it.
                string record = FitRecord(image, len);
                RecordFraming.WriteFrameHead(_writer, len, Framed(record, image, extents).Extents);
                EmitRecord(record);
            }
        }
        else if (_lineSequential) { if (!EmitLineSequentialRecord(TrimRecordEnd(image), page)) return LinageViolationStatus(); }
        else EmitRecord(Fit(image));
        ReleaseRecord();   // §14.9.51.4 GR12 — released to the operating environment, and numbered there
        return WriteSucceeded();   // a LINE SEQUENTIAL LINAGE write travels the page too (EmitLineSequentialRecord)
    }

    /// <summary>⛔ THE ONE LENGTH DECISION OF A WRITE, asked by BOTH write arms — the plain record
    /// (<see cref="WriteRecord"/>) and the print-control line (<see cref="WriteAdvancingRecord"/>), which a
    /// print or LINAGE file's plain WRITE is rerouted to. §13.18.43.4 GR13: the number of bytes in the record is
    /// data-name-1's content (a), passed as <paramref name="length"/>, else the record's own (b/c) — the image's
    /// length on a variable-length file, the record area's width on a fixed one (every record of a fixed file is
    /// integer-1 bytes, GR6).
    /// <para>⛔ The print arm had no length at all (kb/Work PB1190): a RECORD VARYING … DEPENDING file's
    /// ADVANCING write — and, once one had been seen, every later plain WRITE, rerouted to the print arm ahead
    /// of the varying branch — skipped §14.9.51.4 GR14's bound and presented the whole record area, so a
    /// DEPENDING value outside the bounds wrote the record with '00'. The two-arm dispatch: the plain arm had the
    /// rule, the print arm never did.</para></summary>
    private int WrittenLength(string image, int length) =>
        !IsVarying ? RecordWidth : length >= 0 ? length : image.Length;

    /// <summary>§14.9.51.4 GR14 — <i>"The number of bytes in the runtime representation of literal-1, the data
    /// item referenced by identifier-1, or the record referenced by record-name-1 … shall not be larger than the
    /// largest or smaller than the smallest number of bytes allowed by the RECORD IS VARYING clause … If this
    /// rule is violated, the execution of the WRITE statement is unsuccessful and the I-O status of the write file
    /// connector is set to '44'"</i> (§9.1.13.7 item 4 a); §13.18.43.4 GR14 a)). GR15 then says the write does
    /// not take place, so every write arm asks it before anything reaches the medium.</summary>
    private bool OutsideVaryingBounds(int len) => IsVarying && (len < VaryMin || len > VaryMax);

    /// <summary>A line sequential record and its delimiter (ISO §9.1.13.2 — a line sequential record is
    /// delimited, not fixed-width).
    /// <para>⛔ ON A LINAGE FILE THE DELIMITER IS THE WRITE'S OWN ONE-LINE ADVANCE, not a delimiter that happens
    /// to look like one. §14.9.51.4 GR25 makes an omitted ADVANCING phrase a one-line advance and §13.18.34.4
    /// GR7 c) 3 advances the counter by one for it, so the "newline" this write emits is the device travelling
    /// one line on the logical page — which means it may instead be a page transition, and it may be preceded by
    /// the page's top margin. Emitting it as a bare <c>WriteLine</c> is what let a LINE SEQUENTIAL LINAGE file
    /// ignore its margins while the record-sequential twin (rerouted to <see cref="WriteAdvancing"/>) was fixed:
    /// the two-arm dispatch with one arm fixed (kb/Work PB523).</para>
    /// <para>The travel happens BEFORE <see cref="ReleaseRecord"/> flushes, so the record reaches the medium
    /// delimited — §14.9.51.4 GR12's release is of a whole record, and a sharing sibling shall not meet a line
    /// with no terminator.</para></summary>
    /// <returns><see langword="false"/> when the one-line travel's page transition broke the LINAGE page model
    /// (§13.18.34.4 GR6 b) — see <see cref="Position"/>).</returns>
    private bool EmitLineSequentialRecord(string data, LinagePage? page)
    {
        if (page is null || !HasLogicalPage)
        {
            // The one implicit advance, placed by the same question the print arm asks: on an open line it is
            // the travel that ENDS that line (§14.9.51.4 GR25 f)'s AFTER placement — kb/Work PB964, where the
            // record used to weld onto the AFTER write's line); otherwise it is the record's own delimiter.
            if (!ImplicitAdvanceIsBefore) AdvanceLines(1);
            EmitRecordLine(data);
            return true;
        }
        if (ImplicitAdvanceIsBefore) { Present(data, page); return Position(1, page); }
        if (!Position(1, page)) return false;
        Present(data, page);
        return true;
    }

    /// <summary>⛔ THE ONE PLACEMENT QUESTION OF A WRITE THAT HAS NO ADVANCING PHRASE, asked by every arm that
    /// performs one (the print/LINAGE arm through <see cref="WriteAdvancing"/>, and both line sequential arms of
    /// <see cref="EmitLineSequentialRecord"/>). §14.9.51.4 GR25: <i>"If the ADVANCING phrase is not used,
    /// automatic advancing shall be provided by the implementor to act as if the user has specified AFTER
    /// ADVANCING 1 LINE."</i> The advance is ONE line — §13.18.34.4 GR7 counts it once — and its placement
    /// follows the device's LINE STATE (<see cref="_lineOpen"/>): when an AFTER-placed record still stands on an
    /// unterminated line, the advance comes first (GR25 f), "the line is presented after the representation of
    /// the printed page is advanced") and ends that line, so the record lands on the next one; when no line is
    /// open, the stream's established write-then-advance shape (a record followed by its line terminator — the
    /// shape a line sequential record and the NIST print corpus both carry) already puts it on a fresh line.
    /// Before kb/Work PB964 the print arm always placed it second and the line sequential arm never advanced at
    /// all, so a plain WRITE after an AFTER write welded two records onto one physical line.</summary>
    private bool ImplicitAdvanceIsBefore => !_lineOpen;

    /// <summary>Print-control <c>WRITE record [BEFORE] [AFTER] ADVANCING {n LINES | PAGE}</c> (ISO §14.9.51.4
    /// GR25): for
    /// AFTER, advance then write the trimmed image; for BEFORE, write then advance. <paramref name="lines"/> = -1
    /// means ADVANCING PAGE — a form feed on a file with no LINAGE clause (GR25 h), a reposition to the next
    /// logical page on one that has (GR25 g). The leading/trailing newline structure matches the legacy print
    /// stream; the LOGICAL-page geometry lives in <see cref="Position"/> and <see cref="Present"/>.
    /// <paramref name="length"/> is the record length exactly as <see cref="Write"/> takes it (the DEPENDING
    /// item's content, −1 = the record's own size — <see cref="WrittenLength"/>).</summary>
    public string WriteAdvancing(string image, int lines, bool before, LinagePage? page, int length = -1)
    {
        try { return WriteAdvancingRecord(image, length, lines, before, page); }
        catch (IOException refused) { return Status = FileStatusCode.ForWriteFailure(refused); }   // '34' / '30' — see Write
    }

    /// <summary>The body of <see cref="WriteAdvancing"/>, inside its medium-boundary catch — and the one
    /// <see cref="WriteRecord"/> reroutes a print or LINAGE file's plain WRITE to, so the reroute stays inside the
    /// catch that WRITE already opened.</summary>
    private string WriteAdvancingRecord(string image, int length, int lines, bool before, LinagePage? page)
    {
        _endOfPage = null;   // an end-of-page condition is the CURRENT write's or none (§14.9.51.4 GR27)
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (_linagePageBroken) return LinageViolationStatus();   // §13.18.34.4 GR6 b) 2's latch — see Write()
        // §14.9.51.4 GR23 again — the SECOND WRITE ARM. GR23 is a property of the FILE, so it binds every entry
        // point a WRITE statement can reach on a line sequential connector, not just the plain-record one; it is
        // tested on the raw record area.
        if (RecordAreaOutsideLineCharacterSet(image)) return Status = FileStatusCode.LineRecordInvalidChar;
        // Owner decision kb/Work R47 again, on the PRINT arm — the two-arm dispatch this connector has been bitten
        // by before. This is also the report writer's line (Annex A.1 item 159: '91' in the report file's
        // connector). ⛔ A print line is the file coded character set like any other record: one byte per column,
        // U+0000–U+00FF the byte of the same value, so there is NO print-specific character map: one that wrote a
        // character above U+007F as '?' would be a second, silent answer to the question this refusal answers
        // (kb/Work PB690; DOC-A.1-159 / DOC-A.1-31).
        // The ONE length decision (kb/Work PB1190): what the '91' test asks of, GR14's bound, and the characters
        // presented are all the record at §13.18.43.4 GR13's length — the plain arm's rule, now this arm's too.
        int len = WrittenLength(image, length);
        if (!_lineSequential && RecordHasCharacterWithoutByteImage(image.AsSpan(0, Math.Min(image.Length, len))))
            return Status = FileStatusCode.CharacterWithoutByteImage;   // '91' §9.1.13.11 (DOC-A.1-110)
        if (OutsideVaryingBounds(len)) return Status = FileStatusCode.RecordSizeViolation;   // '44' §14.9.51.4 GR14/GR15
        _printControl = true;
        string text = TrimRecordEnd(IsVarying ? FitRecord(image, len) : image);
        // §14.9.51.4 GR25 e)/f) — the ONE advance, placed before or after the presentation by the statement's
        // own word. On a LINAGE file both halves travel the LOGICAL page (GR25 g), GR26 a)); on any other print
        // file they are the plain stream. The pair is written once, and the page-awareness lives inside
        // Present/Position, so a future arm cannot get the placement right and the geometry wrong.
        // ⛔ A §13.18.34.4 GR6 b) VIOLATION AT THE PAGE TRANSITION ABANDONS THE REST OF THE STATEMENT — GR6 b) 2:
        // "it continues with the statement following the WRITE statement". So an AFTER write never presents its
        // record (the positioning is what failed), and a BEFORE write keeps the line it had already presented on
        // the page whose values were still valid (GR25 e) puts the presentation first) but does not RELEASE it:
        // §14.9.51.4 GR12's release is of a SUCCESSFUL write, and this one is not.
        if (before) { Present(text, page); if (!Position(lines, page)) return LinageViolationStatus(); }
        else if (!Position(lines, page)) return LinageViolationStatus();
        else Present(text, page);
        // §14.9.51.4 GR12 — "The successful execution of a WRITE statement releases a logical record to the
        // operating environment" — is an ALL FILES rule, so a print-control WRITE releases an ordinal-identified
        // record exactly as the plain one does, and GR11's WITH LOCK needs that identity. Released HERE and not
        // in Write(), which delegates to this method for a print/LINAGE file (kb/Work PB683, which added the
        // count; kb/Work PB739 made it a RELEASE — the flush and the shared mint — in every write arm:
        // three of them then, TWO since kb/Work PB712 deleted `WriteBeforeAndAfter`).
        ReleaseRecord();
        // The LINAGE counter advanced with the device, inside Position() above — one decision, not two — so an
        // AT END-OF-PAGE branch reads the POST-advance counter of the triggering write whichever side of the
        // presentation the advance fell on (§13.18.34 GR7c; SQ201M's footing lines print line 45).
        return WriteSucceeded();   // §14.9.51.4 GR27 — "the WRITE statement is successful", with GR27 a)'s name
    }


    // ⛔ `WriteBeforeAndAfter` LIVED HERE AND IS GONE (kb/Work PB712). It was the THIRD write arm, taking a
    // BEFORE amount AND an AFTER amount and advancing — and counting LINAGE — twice, because the grammar spelled
    // the ADVANCING phrase twice. §14.9.51.2 Format 1 prints ONE `ADVANCING` operand (the choice indicators
    // enclose only the words BEFORE and AFTER) and §14.9.51.4 GR25 a) advances the page "the number of lines
    // equal to that value" — one advance, whose PLACEMENT GR25 e)/f) decide. The combined COBOL-2023 form is
    // therefore `WriteAdvancing(…, before: true, …)`, and the end-of-page condition the second
    // `AdvanceLinageCounter` call used to erase (PB686's observation) cannot arise: there is one call.
    /// <summary>⛔ THE WRITE-SIDE MEDIUM BOUNDARY — record DATA reaches this connector's writer through here
    /// and through <see cref="EmitRecordLine"/>, and through nothing else, so ISO §13.18.13.4 GR6 b's CODE-SET
    /// conversion is applied exactly once per written record however the WRITE was spelled (plain, VARYING,
    /// line sequential, print-control advancing, REWRITE). Everything else this connector writes — the
    /// §9.1.7.2 length prefix, the line delimiter, an ADVANCING newline or form feed — is FRAMING, not data of
    /// the record, and stays in the native encoding (see <see cref="CodeSetConversion"/>).</summary>
    private void EmitRecord(string data) => _writer!.Write(ToMedium(data));

    /// <summary>— and the line sequential twin: the record's data converted, its delimiter native.</summary>
    private void EmitRecordLine(string data)
    {
        _writer!.WriteLine(ToMedium(data));
        _lineOpen = false;   // the record's own delimiter ended the line
    }

    /// <summary>The print stream's own advance: <paramref name="lines"/> lines, or a form feed for ADVANCING
    /// PAGE. ⛔ The form feed is §14.9.51.4 GR25 h) — <i>"If PAGE is specified and the LINAGE clause is NOT
    /// specified"</i> — so this arm is reached only for a file with no logical page; a LINAGE file's ADVANCING
    /// PAGE is GR25 g) and goes through <see cref="BeginNextLogicalPage"/>.</summary>
    private void Advance(int lines)
    {
        if (lines < 0) { _writer!.Write('\f'); _lineOpen = false; return; }   // ADVANCING PAGE — GR25 h), the NO-LINAGE arm
        AdvanceLines(lines);
    }

    /// <summary>Move the device down <paramref name="lines"/> lines (never negative) — the one place a blank
    /// line reaches the medium, so a margin line, a page-fill line and an ADVANCING line are the same byte.</summary>
    private void AdvanceLines(int lines)
    {
        for (int i = 0; i < lines; i++) _writer!.Write(_lineEnd);
        if (lines > 0) _lineOpen = false;   // the travel ended the line the device stood on
    }

    // ── READ ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ … {NEXT | PREVIOUS}</c> (ISO §14.9.30 Format 1). Returns the record image
    /// (padded to the record width) in <paramref name="image"/> and <see langword="true"/> on success;
    /// <see langword="false"/> at end-of-file / beginning-of-file (AT END, status 10) or any unsuccessful read.
    /// Sets the status.
    /// <para><paramref name="previous"/> is §14.9.30.4 GR19's read kind, taken from the statement's PREVIOUS
    /// phrase. It selects the record NUMBER (GR21 rules b/c), and nothing else about the read changes — which is
    /// why the reposition below feeds the SAME physical read body rather than a second reader.</para></summary>
    public bool Read(bool previous, out string image)
    {
        try { return ReadRecord(previous, out image); }
        catch (IOException) { image = new string(' ', RecordWidth); return ReadFailed(); }   // '30' — see ReadFailed
    }

    /// <summary>⛔ THE READ-SIDE MEDIUM BOUNDARY'S ONE CATCH SHAPE (kb/Work PB1512) — the twin of the write side's
    /// (<see cref="WriteRecord"/>'s summary, kb/Work PB1192), written at each public entry point that reads the
    /// medium — <see cref="Read"/> and <see cref="StartFirstLast"/> — and nowhere below them. A host failure while
    /// the record is being fetched (a region another process has locked, a device error) is §9.1.13.6 item 1's
    /// permanent error: <i>"I-O status = 30. A permanent error exists and no further information is available
    /// concerning the input-output operation."</i> Before this, the <see cref="IOException"/> escaped the
    /// statement and killed the run unit, while the OPEN, CLOSE, WRITE and REWRITE arms of the same connector
    /// already answered with a status. The READ is unsuccessful, so §14.9.30.4 GR18 sets the file position
    /// indicator to "no valid record position" (the next sequential READ is then '46', §9.1.13.7 item 6); the
    /// status reaches the §9.1.13.1 exception path (USE procedures, the Annex A.1 item 103 action) like any
    /// other '30'.</summary>
    private bool ReadFailed()
    {
        InvalidateFilePosition();
        Status = FileStatusCode.PermanentError;
        return false;
    }

    /// <summary>The body of <see cref="Read"/>, inside its medium-boundary catch.</summary>
    private bool ReadRecord(bool previous, out string image)
    {
        image = new string(' ', RecordWidth);
        if (SequentialReadGuard() is { } guard) { Status = guard; return false; }   // '47'/'46'/'10' — FileConnector
        if (_reader is null) { Status = FileStatusCode.ReadNotOpenForInput; return false; }

        // §14.9.30.4 GR21, "When the file is a sequential file": the direction selects the record NUMBER, so a
        // backward read is a REPOSITION to that ordinal followed by the ordinary physical read. Rule e) — "If no
        // record is found that satisfies the above rules, the at end condition exists" — is the target-below-1
        // arm; §14.9.30.4 GR24 then sets '10' and the AT END imperative runs, exactly as it does at EOF.
        // The direction is the statement's phrase through MovesBackward, so COBOL-85's OPEN … REVERSED reaches
        // the SAME reposition rather than a second backward walk of its own (kb/Work PB668).
        if (MovesBackward(previous))
        {
            long target = TargetReadOrdinal(previous);   // the STATEMENT's phrase — MovesBackward owns the XOR
            if (target < 1) { LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }   // GR21 e
            // §14.9.30.4 GR20 — "If the PREVIOUS phrase is specified, the physical file associated with the file
            // connector referenced by file-name-1 shall be a single reel/unit mass storage file." A stream that
            // cannot be positioned is not one; report the permanent error rather than silently reading something
            // else (the same posture the non-seekable REWRITE arm below takes).
            if (!SeekToOrdinal(target)) { Status = FileStatusCode.PermanentError; return false; }
        }

        // §14.9.30 GR14 (READ) / §9.1.13.2 item 3: a RECORD-sequential physical record whose length is outside the file's
        // min/max record size is a SUCCESSFUL read with status '04' (the record is still delivered). Line-sequential
        // is excluded — its short/long conditions are '06'/'09', never '04'.
        bool shortLong = false;
        bool lineTooLong = false;
        bool lineBadChar = false;
        if (_lineSequential)
        {
            // §14.9.30 GR15: an over-length line-sequential record is truncated on the right to the record width, the
            // READ is SUCCESSFUL with I-O status '06', and the file position indicator references the next unread
            // character IN THE RECORD (NOTE 3) — a subsequent READ continues the remainder up to the line delimiter.
            // _lineRemainder models that FPI: service a pending remainder before reading a new physical line.
            string? line;
            if (_lineRemainder is not null)
            {
                line = _lineRemainder;
                _lineRemainder = null;
                _lastReadLinePartial = true;   // a served remainder is only PART of its physical line (§14.9.35 GR17a)
            }
            else if (NextFrame(out _, out _) is { } physical) line = physical;
            else { LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            // The bound is the maximum size the RECORD DESCRIPTIONS specify (GR15's own words), which is the
            // record area's width except for a variable-length record (MaxRecordSize — kb/Work PB981).
            if (line.Length > MaxRecordSize)
            {
                _lineRemainder = line[MaxRecordSize..];
                NoteRecordRead(line[..MaxRecordSize], null);   // a line carries no extent table
                image = Fit(CurrentRecord);
                lineTooLong = true;
            }
            else { NoteRecordRead(line, null); image = Fit(line); }   // §14.9.30.4 GR15 fill — national-aware (kb/Work PB327)
            // §14.9.30.4 GR16: "If the execution of the READ statement is successful but the record area
            // contains one or more characters not in the implementor-defined character set for a line
            // sequential file, the I-O status in the read file connector is set to '09'" (§9.1.13.2 item 7).
            // ⛔ Tested on the DELIVERED record area, which is what GR16 names — so a character in the unread
            // remainder of an over-length line belongs to the READ that DELIVERS it, not to this one. GR16 is
            // stated after GR15 and asks only that the read be successful, so it also lands on a truncated
            // ('06') read; the status arbitration below follows that order (kb/Work PB329).
            lineBadChar = RecordAreaOutsideLineCharacterSet(image);
        }
        else if (NextFrame(out _, out RecordExtents? extents) is { } data)
        {
            NoteRecordRead(data, extents);
            image = Fit(data);   // §14.9.30.4 GR14/GR15 fill — national-aware (kb/Work PB327)
            // A VARYING record outside the file's min/max is §14.9.30.4 GR14's '04' (§9.1.13.2 item 3): its
            // length is the frame's length word. A FIXED-length record sequential file carries no length at all
            // — §9.1.7.2 makes a record sequential record's length "determined by any information the implementor
            // may add to the record on the physical storage medium", and this format adds none (docs/CONFORMANCE.md
            // DOC-A.1-146 (a); GnuCOBOL's fixed-length format, CLAUDE.md rule 1) — so its physical record IS the
            // next integer-1 bytes of the READING description (kb/Work PB1514). Such a record is never longer
            // than the maximum, by that definition, and is shorter only at the end of the file: that partial
            // final record is the '04'. A file written under another record length is not detected here — no
            // boundary exists on the medium to detect it by; RecordLayoutNotice reports the arithmetic case at
            // OPEN.
            shortLong = IsVarying ? data.Length < VaryMin || data.Length > VaryMax : data.Length < RecordWidth;
        }
        else { LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
        _readOrdinal++;   // the record just made available is ordinal N+1 (§9.1.16 lock identity)
        ReadSucceeded(lineBadChar ? FileStatusCode.LineRecordInvalidCharRead   // '09' §14.9.30.4 GR16 — stated after GR15, so it wins over '06'
            : lineTooLong ? FileStatusCode.LineRecordTooLong
            : shortLong ? FileStatusCode.RecordLengthShortLong : FileStatusCode.Success);
        return true;
    }

    /// <summary>Position the reader at the first character of record <paramref name="ordinal"/> (1-based) so the
    /// ordinary read body delivers it — the ONE reposition, used by the §14.9.30.4 GR21 backward read.
    /// <para>Returns false when the physical file cannot support it, which §14.9.30.4 GR20 makes the program's
    /// error rather than a behaviour: a stream that cannot seek, or a LINE SEQUENTIAL file — for which
    /// §14.9.30.3 SR7 forbids the PREVIOUS phrase outright, so the binder has already reported COBOLNET1720 and
    /// this arm is the defensive floor under <c>--permissive</c>.</para>
    /// <para>The offset is ARITHMETIC on a fixed-width record-sequential file and comes from the frame index on
    /// a RECORD VARYING one. ⛔ This member answers only WHERE the record starts; the reposition itself is
    /// <see cref="SeekToRecord"/>, the ONE primitive that also resets every derived read fact (kb/Work PB352) —
    /// a second seek-and-discard here would be the same rule written twice.</para></summary>
    private bool SeekToOrdinal(long ordinal)
    {
        if (_lineSequential || ordinal < 1 || _reader is not { BaseStream: { CanSeek: true } stream }) return false;
        long start;
        if (IsVarying)
        {
            _varyingStarts ??= RecordFraming.FrameStarts(stream);
            // A target past the last frame is NOT a positioning failure — it is §14.9.30.4 GR21 e), "no record
            // is found that satisfies the above rules", and the read body reports it as the ordinary at-end
            // once positioned at the end of data. (Reachable: a PREVIOUS on a freshly opened EMPTY varying
            // file targets ordinal 1 by rule b and there is no ordinal 1.) The fixed-width branch below gets
            // this for free, because seeking past the end yields a zero-character read.
            start = ordinal <= _varyingStarts.Count ? _varyingStarts[(int)(ordinal - 1)] : stream.Length;
        }
        else
        {
            if (RecordWidth <= 0) return false;
            start = (ordinal - 1) * RecordWidth;
        }
        SeekToRecord(start, ordinal - 1);   // the shared read body's post-increment then lands on `ordinal`
        return true;
    }

    /// <summary>⛔ THE ONE PHYSICAL FRAMING WALK for the three on-disk shapes a sequential file has — a
    /// newline-delimited line, a 4-byte-length-prefixed VARYING frame, and a fixed-width block. Advances the
    /// reader by exactly ONE physical record, returns its RAW data (neither padded nor truncated) and reports
    /// the record's byte anchor in <paramref name="frameStart"/>; <see langword="null"/> at end-of-data.
    /// <para>It carries no I-O status, no record-area fill and no <see cref="_lineRemainder"/> chunking: those
    /// are §14.9.30's rules and belong to <see cref="Read"/>. Extracted so START's record scan
    /// (<see cref="StartFirstLast"/>) walks the file with the SAME framing the reader uses instead of a second
    /// copy of it (kb/Work PB352) — the byte anchors it needs are exactly the ones REWRITE already
    /// tracks.</para>
    /// <para>⛔ AND IT IS THEREFORE THE ONE PLACE THE READ-AHEAD IS CHECKED AGAINST THE MEDIUM. Being the only
    /// walk is what makes <see cref="EnsureReaderCoherent"/> a property of this connector rather than a
    /// courtesy at a call site: every character this connector ever reads is read below this line (kb/Work
    /// PB753; <c>SharedReadCoherenceDriftTests</c> proves the "only" rather than asserting it).</para>
    /// <para>⛔ AND IT IS THEREFORE THE ONE PLACE §13.18.13.4 GR6 a's CODE-SET conversion happens on the read
    /// side: the frame comes off the medium in the file's coded character set and leaves this method in the
    /// NATIVE one, so every rule above it — the record-area fill, the line sequential character-set test, START's
    /// key scan — reads native characters, which is what those rules are written about (kb/Work PB793).</para>
    /// </summary>
    /// <param name="extents">The EXTENT TABLE a RECORD VARYING frame carried (D-FRA (v); kb/Work PB1053); null for
    /// every other frame.</param>
    private string? NextFrame(out long frameStart, out RecordExtents? extents)
    {
        EnsureReaderCoherent();   // kb/Work PB753 — a sibling's release since the buffer was filled
        extents = null;
        if (_lineSequential)
        {
            frameStart = _lineByteOffset;
            string? line = ReadPhysicalLine(out int delimBytes);
            if (line is null) return null;
            // A UTF-8 byte-order mark opening the file is not record data (LineSequentialEncoding; DOC-A.1-115).
            int bom = frameStart == 0 && MediumEncoding is MediumEncoding.Utf8
                      && line.StartsWith(LineSequentialEncoding.Bom, StringComparison.Ordinal)
                ? LineSequentialEncoding.Bom.Length : 0;
            _lastLineStart = frameStart + bom;                    // byte anchor of the record (§14.9.35 GR17 REWRITE)
            _lineByteOffset += line.Length + delimBytes;          // the channel is one char per BYTE (past data + delimiter)
            _lastLineBytes = line.Length - bom;                   // the BYTES the record being replaced occupies
            string record = FromMedium(bom == 0 ? line : line[bom..]);
            // An over-length read transfers only part (GR17a) — measured in the record area's units, as the READ's
            // own truncation is (§14.9.30.4 GR15; a UTF-8 line's characters, not its bytes).
            _lastReadLinePartial = record.Length > RecordWidth;
            return record;
        }
        // The block start is the LOGICAL offset (characters consumed so far — 1:1 with bytes under Latin1),
        // never BaseStream.Position: the StreamReader buffers ahead, so the base position is the buffer-fill
        // boundary, not the read position.
        frameStart = _readOffset;
        if (IsVarying)
        {
            // Length-framed record: 4-byte LE prefix + payload (see the framing note at the field declarations).
            var pre = new char[4];
            if (FillChars(pre, 4) < 4) return null;
            int len = RecordFraming.PrefixLength(pre, out bool tableFollows);
            int head = 4;
            _lastReadTableCount = -1;
            if (tableFollows)
            {
                // The frame's EXTENT TABLE (RecordFraming's class summary): a count, then 8 characters per
                // component. A table torn by the end of the file ends the data, as a torn payload does.
                if (FillChars(pre, 4) < 4) return null;
                int count = RecordFraming.ExtentCount(pre);
                var table = new char[8 * count];
                if (FillChars(table, table.Length) < table.Length) return null;
                extents = RecordFraming.ExtentTableFromChars(table);
                _lastReadTableCount = count;
                head += 4 + table.Length;
            }
            var vbuf = new char[len];
            int vn = FillChars(vbuf, len);
            _lastReadBlockStart = _reader!.BaseStream.CanSeek ? _readOffset + head : -1;
            _readOffset += head + vn;
            return FromMedium(new string(vbuf, 0, vn));
        }
        var buf = new char[RecordWidth];
        int n = FillChars(buf, RecordWidth);
        if (n == 0) return null;
        _lastReadBlockStart = _reader!.BaseStream.CanSeek ? _readOffset : -1;
        _readOffset += n;
        return FromMedium(new string(buf, 0, n));
    }

    /// <summary>Fill exactly <paramref name="count"/> characters (or to end-of-stream): StreamReader.Read may
    /// return fewer than requested at an internal buffer boundary, which is not end-of-data.</summary>
    private int FillChars(char[] buf, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = _reader!.Read(buf, total, count - total);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    /// <summary>Read one physical line's DATA, up to and consuming its line delimiter, and report the delimiter's
    /// byte width (0 at a final line with no delimiter) so the caller can track the physical byte offset for a
    /// line-sequential REWRITE (§14.9.35.4 GR17). Returns null only at end-of-file with no characters read.
    /// <para>⛔ THE DELIMITER READ IS DOC-A.1-114's (Annex A.1 item 114): an LF, or a CR LF pair, on EVERY host,
    /// so a file written on either platform reads back on the other — and NOTHING ELSE. A lone CR (one not
    /// followed by LF) is record DATA, as GnuCOBOL reads it (CLAUDE.md rule 1's precedent, kb/Work PB1540); CR is
    /// outside the line sequential character set (item 115), so the READ that delivers it answers '09'
    /// (§14.9.30.4 GR16). Before PB1540 a lone CR ended the record, which is <see cref="StreamReader.ReadLine"/>'s
    /// rule, not this determination's.</para>
    /// <para>Char-by-char rather than ReadLine because ReadLine hides the delimiter width, which the byte anchor
    /// needs (under Latin1 one char == one byte).</para></summary>
    private string? ReadPhysicalLine(out int delimBytes)
    {
        delimBytes = 0;
        int ci = _reader!.Read();
        if (ci < 0) return null;                                    // EOF, no data
        var sb = new StringBuilder();
        while (true)
        {
            char c = (char)ci;
            if (c == '\n') { delimBytes = 1; break; }
            if (c == '\r' && _reader.Peek() == '\n') { _reader.Read(); delimBytes = 2; break; }
            sb.Append(c);
            ci = _reader.Read();
            if (ci < 0) break;                                      // final line with no terminator
        }
        return sb.ToString();
    }

    // ── REWRITE ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>REWRITE record</c> (ISO §14.9.35): replace the last-read record in place. Valid only
    /// when open I-O (else 49) and the immediately-previous op was a successful READ (else 43).
    /// <paramref name="length"/> is §13.18.43.4 GR13's number of bytes in the record — the DEPENDING item's
    /// content (a), or the number of bytes in record-name-1 (b/c), which the compiler passes for a fixed-length
    /// file; −1 = the image's own length on a variable-length file, the record area's width on a fixed one.
    /// The size rules then split on the ORGANIZATION, never on fixed-versus-varying (kb/Work PB1168): see
    /// <see cref="RewriteRecordSequential"/> and <see cref="RewriteLine"/>.</summary>
    /// <param name="extents">The replacing record's EXTENT TABLE (D-FRA (v); kb/Work PB1053). The frame keeps its
    /// size (GR16), so it replaces the table the replaced frame carried only when it has as many components; a
    /// frame whose table cannot take it has that table VOIDED (it would otherwise describe the replaced record),
    /// and a frame that carried none stays without one — the record then reads back by the take step.</param>
    public string Rewrite(string image, int length = -1, RecordExtents? extents = null)
    {
        try { return RewriteRecord(image, length, extents); }
        catch (IOException refused) { return Status = FileStatusCode.ForWriteFailure(refused); }   // '34' / '30' — see Write
    }

    /// <summary>The body of <see cref="Rewrite"/>, inside its medium-boundary catch: the all-organization
    /// preconditions, the §13.18.43.4 GR14 a) bounds of a variable-length file, and then ONE dispatch on the
    /// organization, because that is the axis §14.9.35.4 states its size rules on — GR16 under "For a record
    /// sequential file", GR17 under "For a line sequential file".
    /// <para>⛔ THE SIZE RULE WAS KEYED ON THE WRONG AXIS (kb/Work PB1168). It read <c>IsVarying &amp;&amp; len !=
    /// LastReadLength</c>, so GR16 ran for every VARYING file and for no FIXED one — a 10-byte record-name-1
    /// REWRITE over a 20-byte record of a fixed file was padded and replaced the record with '00' — and it ran
    /// ahead of the line sequential arm, so a varying LINE SEQUENTIAL REWRITE of a shorter record got GR16's '44'
    /// where GR17 c) pads it and succeeds.</para></summary>
    private string RewriteRecord(string image, int length, RecordExtents? extents)
    {
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        if (!PrevOpWasSuccessfulRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
        // §13.18.43.4 GR13 — the number of bytes in the record: data-name-1's content (a), else the bytes in the
        // record (b/c), which the compiler passes for a fixed-length file. −1 is the image's own length.
        int len = length >= 0 ? length : IsVarying ? image.Length : RecordWidth;
        // §13.18.43.4 GR14 a) / §9.1.13.7 item 4 a) — a record outside the RECORD IS VARYING bounds, whatever
        // the organization.
        if (IsVarying && (len < VaryMin || len > VaryMax)) return Status = FileStatusCode.RecordSizeViolation;
        return _lineSequential ? RewriteLine(image, len) : RewriteRecordSequential(image, len, extents);
    }

    /// <summary>§14.9.35.4 GR16 — <i>"For a record sequential file, if the number of bytes in the data item
    /// referenced by identifier-1, the runtime representation of literal-1, or the record referenced by
    /// record-name-1 is not equal to the number of bytes in the record being replaced, the execution of the
    /// REWRITE statement is unsuccessful and the I-O status in the rewrite file connector is set to '44'"</i>
    /// (§9.1.13.7 item 4 b)). EVERY record sequential file, fixed-length or variable: a fixed file's records are
    /// integer-1 bytes (§13.18.43.4 GR6), so a shorter record description cannot replace one, and GR14 then
    /// leaves the record and the record area as they were. The frame therefore never changes size, which is
    /// what makes the overwrite in place exact.</summary>
    private string RewriteRecordSequential(string image, int len, RecordExtents? extents)
    {
        if (len != LastReadLength) return Status = FileStatusCode.RecordSizeViolation;   // '44' GR16
        if (_lastReadBlockStart < 0 || _reader is not { BaseStream: { CanSeek: true, CanWrite: true } stream })
            return NotRewritableInPlace();
        string record = FitRecord(image, len);
        if (RecordHasCharacterWithoutByteImage(record))
            return Status = FileStatusCode.CharacterWithoutByteImage;   // '91' — the WRITE's rule (R47)
        if (_lastReadTableCount < 0) return OverwriteInPlace(stream, _lastReadBlockStart, record);
        RecordExtents? replacing = Framed(record, image, extents).Extents;
        string table = RecordFraming.ExtentTableChars(replacing is not null && replacing.Count == _lastReadTableCount
            ? replacing : RecordFraming.VoidExtents(_lastReadTableCount));
        return OverwriteInPlace(stream, _lastReadBlockStart - table.Length, table, record);
    }

    /// <summary>§14.9.35.4 GR17, the line sequential REWRITE, in its own order: a) a READ that transferred only
    /// part of the record ⇒ '44' (§9.1.13.7 item 4 d)); d) a record area holding a character outside the line
    /// sequential character set ⇒ '71'; b) a record of MORE bytes than the record being replaced ⇒ '44';
    /// c) a record of fewer bytes is space-filled to the replaced record's length and replaces it ⇒ '00'.
    /// <para>⛔ b) and c) compare <paramref name="len"/> — the number of bytes in record-name-1 (or data-name-1's
    /// content), UNTRIMMED. The trailing-space rule a line sequential WRITE applies (§14.9.51.4 GR21, "any spaces
    /// to the right of the rightmost non-space character are not transferred") is a WRITE rule; GR17 has none,
    /// and comparing the trimmed content let a 10-byte record-name-1 replace a 2-byte line whenever its last
    /// eight bytes were spaces (kb/Work PB1168). The replacement is the record padded to the replaced line's
    /// length, so the physical byte span and the delimiter's position are unchanged.</para></summary>
    private string RewriteLine(string image, int len)
    {
        if (_lastLineStart < 0 || _reader is not { BaseStream: { CanSeek: true, CanWrite: true } stream })
            return NotRewritableInPlace();
        if (_lastReadLinePartial) return Status = FileStatusCode.RecordSizeViolation;   // '44' GR17 a)
        // GR17 d) routes through the SHARED set (kb/Work PB329): a REWRITE and a WRITE of the identical record
        // area reach the identical verdict (Annex A.1 item 115).
        if (RecordAreaOutsideLineCharacterSet(image)) return Status = FileStatusCode.LineRecordInvalidChar;   // '71' GR17 d)
        // b) and c) compare BYTES — the record's bytes ON THE MEDIUM against the bytes of the record it replaces in
        // place (DOC-A.1-115): under a one-byte encoding that is its length; a UTF-8 character may take two to four.
        string record = FitRecord(image, len);
        int bytes = ToMedium(record).Length;
        if (bytes > _lastLineBytes) return Status = FileStatusCode.RecordSizeViolation;   // '44' GR17 b)
        // c) space-fills to the replaced record's byte length: one space per missing byte — the space is one byte in
        // every encoding, and a NATIONAL area's space is a two-char pair in the image but one UTF-8 byte on the medium.
        int perByte = MediumEncoding is MediumEncoding.Utf8 && NationalRecordArea ? CobolBits.BytesPerNational : 1;
        return OverwriteInPlace(stream, _lastLineStart, FitRecord(record, len + ((_lastLineBytes - bytes) * perByte)));   // '00' GR17 c)
    }

    /// <summary>A REWRITE whose record cannot be overwritten in place — a stream that cannot seek or write, or no
    /// record anchor — reports §9.1.13.6 item 1's permanent error, so the program's FILE STATUS and declarative
    /// path observe it (never a silent no-op).</summary>
    private string NotRewritableInPlace() => Status = FileStatusCode.PermanentError;

    /// <summary>⛔ THE ONE IN-PLACE OVERWRITE — the record-sequential and the line-sequential REWRITE arms
    /// differ in the rules that VALIDATE the replacement (§14.9.35.4 GR16/GR20 versus GR17 a)–d)) and not at
    /// all in what they then do to the physical file, so the release itself is written once (kb/Work PB753;
    /// it used to be two copies and only one of them could have been given the announcement below).
    /// <para>The write goes through the I-O reader's own base stream (§14.9.35 GR3 — a REWRITE replaces the
    /// record the last READ retrieved, through the one connector) and is FLUSHED, because §14.9.35.4 GR4 says
    /// <i>"The successful execution of the REWRITE statement releases a logical record to the operating
    /// environment"</i>: the '00' this returns is a promise that the record is IN the physical file, not that
    /// it is queued. <see cref="NoteRelease"/> then tells the physical file's other connectors, whose
    /// read-ahead would otherwise keep serving the image this call just superseded.</para>
    /// <para>The base stream is left where it was found. It is the buffer-fill boundary of the
    /// <see cref="StreamReader"/> above it, not the file position indicator, so restoring it is what keeps
    /// THIS connector's own buffered characters valid across its own REWRITE.</para></summary>
    private string OverwriteInPlace(Stream stream, long anchor, string content) =>
        OverwriteInPlace(stream, anchor, "", content);

    /// <summary><see cref="OverwriteInPlace(Stream, long, string)"/> with a FRAME part ahead of the record: the
    /// replacing record's extent table (D-FRA (v); kb/Work PB1053), which is framing, not data of the record, so it
    /// goes to the medium as it is and is never converted to the file's coded character set.</summary>
    private string OverwriteInPlace(Stream stream, long anchor, string framing, string content)
    {
        long resume = stream.Position;
        stream.Seek(anchor, SeekOrigin.Begin);
        if (framing.Length > 0)
        {
            byte[] head = Encoding.Latin1.GetBytes(framing);
            stream.Write(head, 0, head.Length);
        }
        // ⛔ The REWRITE arm's §13.18.13.4 GR6 b boundary, for the same reason EmitRecord is the WRITE arm's:
        // `content` is the record's data in the NATIVE character set, and what goes on the medium is the file's
        // coded character set. Being the ONE in-place overwrite makes this the one place either arm converts.
        byte[] bytes = FileCharacterSet.Medium.GetBytes(ToMedium(content));
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();                 // §14.9.35.4 GR4 — released to the operating environment
        stream.Seek(resume, SeekOrigin.Begin);
        NoteRelease();                  // …and announced, so no sibling reader serves the superseded image
        return Status = FileStatusCode.Success;
    }

    // ── START (ISO §14.9.41, SEQUENTIAL FILES — General rules 1–7 and 20/21) ─────────────────────────────────

    /// <summary>START FIRST / LAST on a sequential-organization file. §14.9.41.3 SR2 makes these the ONLY two
    /// forms the statement has on this organization, and §14.9.41.4 GR20/GR21 state their outcome:
    /// <list type="bullet">
    /// <item>GR20 — <i>"If FIRST is specified, the file position indicator is set to 1 if records exist in the
    /// physical file. If no records exist in the file, or the physical file does not support the ability to
    /// position at the first record, the I-O status value … is set to '23', the invalid key condition exists,
    /// and the execution of the START statement is unsuccessful."</i></item>
    /// <item>GR21 — the LAST twin, positioning at <i>"the record number of the last existing logical
    /// record"</i>.</item>
    /// </list>
    /// GR1's open mode is §9.1.13.7 item 7's '47' (<i>"a READ or START statement … not open in the input or I-O
    /// mode"</i>); GR5's absent OPTIONAL file and both GR20/GR21 failure arms are §9.1.13.5 item 3's '23', whose
    /// clause d) — <i>"a START statement is attempted on a sequential file that has no records or that does not
    /// support the ability to position at the specified record"</i> — is written for exactly this connector.
    /// <para>⛔ AN IMPLEMENTATION THAT CANNOT SEEK STILL OWES AN ANSWER, NEVER AN ABORT: that is the shape of
    /// GR20/GR21's second arm, and it is why this member exists at all (kb/Work PB352 — the statement used to
    /// bind to a loud runtime stage that killed the run unit). Positioning is INCLUSIVE, matching the keyed
    /// connectors: the next sequential READ delivers the record START selected.</para></summary>
    public string StartFirstLast(bool last)
    {
        // The read-side medium boundary (ReadFailed): the scan below reads the medium. An unsuccessful START
        // invalidates the file position indicator (§14.9.41.4 GR7), exactly as ReadFailed does for a READ.
        try { return StartFirstLastRecord(last); }
        catch (IOException) { ReadFailed(); return Status; }   // '30' §9.1.13.6 item 1
    }

    /// <summary>The body of <see cref="StartFirstLast"/>, inside its medium-boundary catch.</summary>
    private string StartFirstLastRecord(bool last)
    {
        if (StartOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();                        // GR5 / §9.1.13.5 item 3 b)
        // "or the physical file does not support the ability to position at the first/last record" — a stream
        // that cannot be repositioned. A FileStream always can; a future non-seekable medium answers '23' here
        // rather than mis-positioning.
        if (_reader is not { BaseStream.CanSeek: true }) return StartFail();

        // The scan is the ONE framing walk (NextFrame), so the record boundaries START positions on are exactly
        // the ones READ would deliver — including a short final block ('04') and a line-sequential file's
        // physical lines. FIRST stops at the first frame; LAST walks to end-of-data remembering the last one.
        SeekToRecord(0, 0);
        long foundStart = -1, foundOrdinal = 0, seen = 0;
        while (NextFrame(out long frameStart, out _) is not null)
        {
            seen++;
            foundStart = frameStart;
            foundOrdinal = seen;
            if (!last) break;
        }
        if (foundStart < 0) { SeekToRecord(0, 0); return StartFail(); }   // no records exist — GR20/GR21
        SeekToRecord(foundStart, foundOrdinal - 1);
        LastReadUnsuccessful = false;   // a successful START is a reposition: §14.9.30.4 GR21's '46' poison clears
        return Status = FileStatusCode.Success;
    }

    /// <summary>An unsuccessful START: §14.9.41.4 GR7 — <i>"the file position indicator is set to indicate that
    /// no valid record position has been established"</i>, which on this connector is precisely the state
    /// §9.1.13.7 item 6 a) reads back as '46' on the next sequential READ — and the '23' GR20/GR21 name.</summary>
    private string StartFail()
    {
        InvalidateFilePosition();
        return Status = FileStatusCode.RecordNotFound;
    }

    /// <summary>Reposition the reader at <paramref name="byteOffset"/>, which shall be the START of a record,
    /// and reset every piece of derived read state so the next <see cref="Read"/> behaves as if the file had
    /// been read up to that point: the logical offsets (both framings), the §14.9.30 GR15 unread remainder, the
    /// REWRITE byte anchors (a START is not a READ, so §14.9.35's "record being replaced" no longer exists),
    /// the §9.1.16 record ordinal, which becomes <paramref name="ordinalBefore"/> so the next record read is
    /// numbered correctly, and the physical file's release generation the emptied buffer now agrees with
    /// (kb/Work PB753). <see cref="StreamReader.DiscardBufferedData"/> is required: the reader buffers ahead
    /// of the base stream, so seeking the stream alone would keep serving stale characters.</summary>
    private void SeekToRecord(long byteOffset, long ordinalBefore)
    {
        _reader!.BaseStream.Seek(byteOffset, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        _coherentAt = Physical?.ReleaseGeneration ?? 0;
        _readOffset = byteOffset;
        _lineByteOffset = byteOffset;
        _lineRemainder = null;
        _lastReadBlockStart = -1;
        _lastReadTableCount = -1;
        _lastLineStart = -1;
        _lastLineBytes = 0;
        _lastReadLinePartial = false;
        _readOrdinal = ordinalBefore;
    }

    /// <summary>The AT END condition (ISO §14.9.30): true only at end-of-file (status 10).</summary>
    public bool AtEnd => Status == FileStatusCode.AtEnd;
}
