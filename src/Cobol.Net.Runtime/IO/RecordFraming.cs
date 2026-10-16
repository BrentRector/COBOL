// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>What <see cref="RecordFraming.ReadHeader"/> found at the head of a keyed store.</summary>
internal enum StoreFormat
{
    /// <summary>The file holds no bytes (or the host would not let this process read them). It records no
    /// records and therefore no attribute, so it can misread nothing and contradicts nothing — the ONE
    /// tolerated shape that is not a §14.9.27.4 GR10 conflict.</summary>
    Empty,

    /// <summary>A header this build understands; its <see cref="FixedFileAttributes"/> are the physical file's
    /// §9.1.6 fixed file attributes.</summary>
    Described,

    /// <summary>Bytes that are not a header this build understands — a file some other tool wrote, a plain
    /// sequential file opened through a keyed description, or a store from a build before the header existed.
    /// It is not a store this processor can interpret, so GR10's answer is '39': LOUD, where reading it as
    /// frames would be a silent misread.</summary>
    Foreign,
}

/// <summary>One record as it travels with its EXTENT TABLE (determination D-FRA (v); kb/Work PB1053) — between a
/// keyed store and its connector, and through the sort store: the record's characters and, when the
/// variable-length group that sent it had them, where each variable-length component ended. The table is framing
/// information, never part of the record (§12.4.5.11.4 GR1).
/// <para><paramref name="Ordinals"/> is an INDEXED record's release-ordinal vector (the prime key's slot first, then
/// one per alternate key — <c>KeyedRec.Ordinals</c>): the §14.9.30.4 GR26 order of its duplicates under every key,
/// carried in the store so a reload keeps it (kb/Work PB2693). Null for every other frame.</para></summary>
internal readonly record struct StoredFrame(string Image, RecordExtents? Extents, long[]? Ordinals = null);

/// <summary>What <see cref="RecordFraming.ReadFrame"/> found at a frame boundary.</summary>
internal enum FrameKind
{
    /// <summary>A record's frame: its head is decoded (<see cref="FrameHead"/>) and its payload taken.</summary>
    Record,

    /// <summary>The empty-slot tag — a relative store's gap. A stream-level file holds none
    /// (<see cref="RecordFraming.ReadStreamFrame"/>).</summary>
    Gap,

    /// <summary>No frame: the data ends inside it (a torn tail — a run unit killed mid-WRITE, a file copied short) or
    /// its head is not one this writer produces. ⛔ THE FRAMED DATA ENDS HERE, for every reader of every shape: the
    /// record count, the backward-READ index, the forward READ and the OPEN's framing check all stop at this frame.</summary>
    End,
}

/// <summary>A decoded frame head: the payload's length in bytes, the EXTENT TABLE the frame carried (null when it
/// carried none) and an indexed store frame's release ordinals (null for every other frame) — see
/// <see cref="RecordFraming"/>'s class summary for the layout.</summary>
internal readonly record struct FrameHead(int PayloadLength, RecordExtents? Extents, long[]? Ordinals)
{
    /// <summary>The bytes the head occupies ahead of the payload — the ONE size formula
    /// (<see cref="RecordFraming.FrameBytes(int, RecordExtents?, int)"/>) over a payload of none.</summary>
    public long HeadBytes => RecordFraming.FrameHeadBytes(Extents, Ordinals?.Length ?? 0);
}

/// <summary>Where <see cref="RecordFraming.ReadFrame"/> takes a frame's bytes from — the ONE frame decoder reads every
/// shape through this, so the media differ only in how they hold bytes and never in what a frame is (kb/Work
/// PB2754): a keyed store's image in memory, a stream-level file walked by its prefixes, and the sequential
/// connector's Latin-1 read channel.</summary>
internal interface IFrameMedium
{
    /// <summary>Fill <paramref name="into"/> from the current position and advance past it; false when the data ends
    /// first.</summary>
    bool Fill(Span<byte> into);

    /// <summary>Take the <paramref name="length"/>-byte payload that follows the head and advance past it (a medium
    /// that needs no payload seeks over it); false when the data ends first. ⛔ The decoder calls it only after
    /// <see cref="Holds"/>(<paramref name="length"/>) answered true, so a medium that can answer Holds does not ask
    /// it again here: on a stream every ask was a file-length query (kb/Work PB2754, review R1-1).</summary>
    bool TakePayload(int length);

    /// <summary>Can <paramref name="bytes"/> more bytes still follow the current position? Asked BEFORE storage sized
    /// by a count read from the medium is allocated, so a corrupt length word or table count ends the data instead of
    /// allocating what no file holds. A medium that cannot say answers true and relies on the short read.</summary>
    bool Holds(long bytes);
}

/// <summary>
/// The ONE on-disk record framing shared by every organization (DESIGN-runtime-library §2.2): each framed record
/// is a 4-byte little-endian length prefix followed by that many Latin-1 payload bytes; an EMPTY slot (relative
/// gap) is the tag 0xFFFFFFFF with no payload. Explicit length framing carries occupancy soundly — the legacy's
/// fixed-format all-0x00/0xFF gap HEURISTIC could vanish a legitimate all-zero binary record — and the physical
/// format is implementor-defined by the spec, so only self-consistency matters: producer and consumer of a
/// corpus chain both run on this framing.
/// <para>⛔ THE LICENCE IS §9.1.7.2, NOT §9.1.13.6, and the correction matters (kb/Work PB292). This comment
/// cited §9.1.13.6's "externally-defined boundaries" until 2026-09-05 — a real sentence about a DIFFERENT
/// question: it is the I-O status 34 rule for writing past a physical FILE's boundary, not a statement about
/// record framing (feedback_a_real_clause_can_answer_a_different_question). The clause that actually grants
/// this is §9.1.7.2: "In record sequential files the length of each record is determined by any information the
/// implementor may add to the record on the physical storage medium (such as record length headers)" — this
/// prefix IS that header. §12.4.5.11.4 GR5 makes the same grant for the variable-length case where no RECORD
/// DELIMITER clause governs, which for WiseOwl COBOL is EVERY case (the clause is declined whole — COBOLNET1778,
/// docs/CONFORMANCE.md §2 row 26), and the determination is filed as Annex A.1 item 151 in
/// docs/CONFORMANCE.md §7. §12.4.5.11.4 GR1 is what this framing must not violate — "Any method used shall not
/// be reflected in the record area or the record size used within the function, method, or program" — so the
/// prefix is stripped before the record area is filled and never enters a record size a program can read.</para>
/// Two access shapes over the SAME record layout, differing in ONE thing — whether a STORE HEADER precedes the
/// frames:
/// <list type="bullet">
/// <item><b>Store-level</b> (<see cref="ComposeStore"/>/<see cref="DecodeStore"/>/<see cref="ReadHeader"/>) — the
/// whole-file rewrite/load the keyed connectors use (relative slots; indexed records in release order, each with
/// its release ordinals). It opens with the header described below.</item>
/// <item><b>Stream-level</b> (<see cref="ReadStreamFrame"/>/<see cref="FrameStarts"/>/<see cref="StreamFramingParses"/>)
/// — the incremental prefix-per-record shape the sequential connector streams through its Latin-1 reader/writer
/// (chars 0–255 map 1:1 to bytes under Latin-1, so the head it writes and reads is the store-level head's bytes,
/// converted once at the channel). It has NO header, deliberately: a header would stop a record sequential file
/// being plain bytes and a line sequential file being plain text, which is the interchange property those shapes
/// exist for.</item>
/// </list>
/// <para>⛔ ONE ENCODER AND ONE DECODER OF A FRAME HEAD, for both shapes (kb/Work PB2754): <see cref="EncodeFrameHead"/>
/// writes it and <see cref="ReadFrame"/> reads it, through an <see cref="IFrameMedium"/>. The decoder alone says what
/// a gap is and that a torn tail ends the data, so the record count, the backward-READ index, the forward READ and the
/// OPEN's framing check cannot disagree about which records a file holds (<c>FrameDecoderDriftTests</c>).</para>
/// <para>⛔ A FRAME MAY CARRY ITS RECORD'S EXTENT TABLE (determination D-FRA (v); kb/Work PB1053), in BOTH shapes and
/// in the same way. A variable-length group record is sent as its contiguous image (§8.5.1.11.2), and with two or
/// more variable-length components the characters alone cannot say where each ended, so the frame carries that
/// beside the payload — the same §9.1.7.2 licence as the length itself. Bit 31 of the length word says a table
/// follows it (a payload of 2^31 characters is not representable, and the gap tag keeps its own spelling), and the
/// table precedes the payload:</para>
/// <code>
///   word    4  payload length | 0x80000000
///   count   4  component count n
///   n × 8      per component: fixed-run offset(4) length in characters(4)
///   payload    the record, exactly as before
/// </code>
/// <para>Like the prefix, the table is stripped before the record area is filled and never counts in a record size
/// (§12.4.5.11.4 GR1): it reaches the program only as <c>FileConnector.CurrentRecordExtents</c>, which the
/// generated decomposition of a CORRESPONDING variable-length group reads (<c>CobolContiguousLayout.Decompose</c>).
/// A line sequential line and a fixed-length block have no frame and therefore carry no table.</para>
/// <para>⛔ THE STORE HEADER IS THE §9.1.6 FIXED FILE ATTRIBUTES OF A RELATIVE OR INDEXED FILE, and it is IN
/// THE FILE — owner decision 2026-09-07 (kb/Work PB802): <i>"Let's match GNUCobol's implementation in spirit.
/// No sidecar of any type."</i> GnuCOBOL keeps an indexed file's key definitions in the ISAM file's own header;
/// this is the same answer for the same reason. Nothing is written beside a data file. Layout, all
/// little-endian, immediately followed by the frames:</para>
/// <code>
///   0   8  magic "CBNFSTR" + format version (currently 4)
///   8   1  organization    'R' relative · 'I' indexed
///   9   1  record type     'F' fixed · 'V' varying
///  10   4  minimum logical record size (bytes)
///  14   4  maximum logical record size (bytes)
///  18   2  key count (0 for a relative store; the prime key plus each alternate key for an indexed one)
///  20   8  generation — the cross-run-unit coherence stamp (kb/Work PB2660; see GenerationOffset)
///  28   8  release mint — an indexed store's next release ordinal (kb/Work PB2693; see ReleaseMintOffset); 0 otherwise
///  36  ..  per key: offset(4) length(4) flags(1: bit0 DUPLICATES) suppress-length(2) suppress(Latin-1)
///          collation-length(1) collation(ASCII fingerprint)
/// </code>
/// <para>⛔ AN INDEXED FRAME CARRIES ITS RECORD'S RELEASE ORDINALS (kb/Work PB2693): key count × 8 bytes, the prime
/// key's slot first, after the length word and the extent table and before the payload. §14.9.30.4 GR26 orders an
/// alternate key's duplicates by release, and §14.9.35.4 GR24 b) moves a rewritten record last under each key it
/// changed, independently per key — so the order is one number per record PER KEY, which no single physical order of
/// the records can carry. A reload that derived the ordinals from file position renumbered them, and a connector
/// partway through a set of duplicates skipped a record when another run unit deleted an earlier one.</para>
/// <para>A format version this build does not know is <see cref="StoreFormat.Foreign"/> and NOT "attributes not
/// recorded": a store whose layout is unknown cannot have its frames located either, so the only safe answer is
/// to refuse it rather than to read it as if the header were absent.</para>
/// </summary>
internal static class RecordFraming
{
    /// <summary>The empty-slot (gap) tag — a length prefix of 0xFFFFFFFF with no payload.</summary>
    private const uint GapTag = 0xFFFFFFFF;

    /// <summary>Bit 31 of a frame's length word: an EXTENT TABLE follows the word (see the class summary).</summary>
    private const uint ExtentFlag = 0x80000000;

    /// <summary>The store header's magic: seven ASCII bytes, so a headerless file's first four bytes could
    /// only collide by naming a frame of 0x464E4243 (1.18 GB) and then spelling the rest exactly.</summary>
    private static ReadOnlySpan<byte> Magic => "CBNFSTR"u8;

    /// <summary>The store format's version, written as the byte immediately after <see cref="Magic"/> and
    /// CHECKED on every decode. A version this build does not know is <see cref="StoreFormat.Foreign"/>
    /// rather than "attributes not recorded": a layout this build cannot read cannot have its frames
    /// located either, so the only safe answer is to refuse the store instead of reading past it.
    /// <para>Version 2 (kb/Work PB1602) writes a key's SUPPRESS WHEN value as exact UTF-16 code units with an
    /// Int32 count. Version 1 wrote it through Latin-1 and so could not record every legal declaration. A
    /// version-1 store is Foreign to this build: no reader for it is kept.</para>
    /// <para>Version 3 (kb/Work PB2660) adds the store GENERATION to the fixed header (<see cref="GenerationOffset"/>),
    /// the stamp by which a run unit learns that another run unit has rewritten the store. A version-2 store is
    /// Foreign to this build, as version 1 is: no reader for it is kept (there is no backward-compatibility
    /// requirement, CLAUDE.md rule 4).</para>
    /// <para>Version 4 (kb/Work PB2693) adds the RELEASE MINT to the fixed header (<see cref="ReleaseMintOffset"/>) and
    /// each indexed record's release-ordinal vector to its frame. A version-3 store is Foreign to this build.</para></summary>
    private const byte FormatVersion = 4;

    /// <summary>The fixed part of the header — magic, organization, record type, the two record sizes, the key
    /// count, the generation and the release mint. Per-key descriptors follow it.</summary>
    private const int FixedHeaderBytes = 36;

    /// <summary>The offset of the RELEASE MINT in the fixed header, 8 bytes little-endian: an indexed store's next
    /// release ordinal (<c>IndexedStore.NextOrdinal</c>), so the run unit that writes next — this one after a CLOSE
    /// and OPEN, or another one sharing the file — releases its record AFTER every record already in the file
    /// (§14.9.30.4 GR26). Zero for a relative store, which has no release order.</summary>
    private const int ReleaseMintOffset = 28;

    /// <summary>The bytes one release ordinal occupies in an indexed frame.</summary>
    private const int OrdinalBytes = 8;

    // ── The store's capacity — the externally-defined boundary of a relative or indexed file ───────────────

    /// <summary>⛔ THE LARGEST STORE IMAGE THIS FORMAT CAN HOLD, in bytes — the externally-defined boundary of a
    /// physical relative or indexed file (ISO §9.1.13.5 item 4, <i>"The implementor specifies the manner in which
    /// these boundaries are defined"</i>; Annex A.1 item 107, docs/CONFORMANCE.md <c>DOC-A.1-107</c>;
    /// kb/Work PB1192). It is <see cref="Array.MaxLength"/> because the whole image is composed in one array by
    /// <see cref="ComposeStore"/> and taken back in one by <see cref="DecodeStore"/>: a store one byte larger could be
    /// neither persisted nor loaded. The keyed connectors test it AT THE WRITE (§14.9.51.4 GR33 b) / GR42 d)'s
    /// '24'), so a record the store cannot hold is refused before it is released instead of being reported '00'
    /// and failing at the CLOSE.</summary>
    public const long MaxStoreBytes = 0x7FFFFFC7;   // Array.MaxLength (a runtime property; its value is fixed)

    /// <summary>The bytes an EMPTY relative slot occupies — its <see cref="GapTag"/> alone. Every slot below the
    /// highest relative record number that holds no record costs this much, which is why the relative record
    /// number is bounded by the store's capacity long before it is bounded by its own range.</summary>
    public const int GapBytes = 4;

    /// <summary>The bytes one record occupies in the store: its 4-byte length word, its extent table (if any), its
    /// release ordinals (an indexed record's) and one byte per character — the file coded character set is one byte
    /// per character position (<see cref="FileCharacterSet"/>), and a CODE-SET conversion is a per-character map, so
    /// the payload length is the image's. The ONE size formula, shared by the WRITE-time boundary test and
    /// <see cref="ComposeStore"/>'s composition, so the two cannot disagree about what fits.</summary>
    public static long FrameBytes(StoredFrame frame) =>
        FrameBytes(frame.Image.Length, frame.Extents, frame.Ordinals?.Length ?? 0);

    /// <summary><see cref="FrameBytes(StoredFrame)"/> of a record of <paramref name="chars"/> characters with the
    /// extent table <paramref name="extents"/> and <paramref name="ordinalSlots"/> release ordinals — the same
    /// formula, for a caller that holds the parts rather than a frame.</summary>
    public static long FrameBytes(int chars, RecordExtents? extents, int ordinalSlots) =>
        4L + ExtentTableBytes(extents) + (long)OrdinalBytes * ordinalSlots + chars;

    /// <summary>The bytes a frame's HEAD occupies ahead of its payload — its length word, its extent table and its
    /// release ordinals: <see cref="FrameBytes(int, RecordExtents?, int)"/> of a payload of none.</summary>
    public static long FrameHeadBytes(RecordExtents? extents, int ordinalSlots) => FrameBytes(0, extents, ordinalSlots);

    /// <summary>The bytes the store header for <paramref name="attributes"/> occupies — measured by encoding it
    /// with the ONE header writer, so the size cannot drift from the layout.</summary>
    public static long HeaderBytes(FixedFileAttributes attributes)
    {
        var header = new MemoryStream(FixedHeaderBytes + (64 * attributes.Keys.Count));
        WriteHeader(header, attributes, generation: 0, releaseMint: 0);
        return header.Length;
    }

    /// <summary>How many release ordinals each frame of a store described by <paramref name="attributes"/> carries:
    /// one per key of an indexed store (<see cref="FixedFileAttributes.Keys"/> lists the prime key and each
    /// alternate), none for a relative one.</summary>
    public static int OrdinalSlots(FixedFileAttributes attributes) =>
        attributes.Organization == FixedFileAttributes.Indexed ? attributes.Keys.Count : 0;

    // ── Store-level (byte) shape — the keyed connectors' whole-store persist/load ───────────────────────────

    /// <summary>Compose the whole store: the §9.1.6 header stamped with <paramref name="generation"/>, then one frame
    /// per ordinal position; null = an empty (gap) slot. The image is returned, never written: the connector writes
    /// it through its own handle (<c>KeyedConnector</c>), so this class stays the FRAMING and owns no I/O.
    /// <para>The header is stamped on EVERY persist, from the writing connector's own declared attributes, so a
    /// store always describes itself. That is not a re-establishment of §9.1.6's "at the time it is created":
    /// a connector whose attributes disagreed with the file's could not have opened it (§14.9.27.4 GR10 answered
    /// '39' before <c>OpenCore</c>), so every persist writes back the attributes the file already had — except
    /// under the documented <c>COBOLNET_KEYCHECK</c> opt-out, where a connector admitted with a different key
    /// table writes ITS key table, which is what opting out of the key check means.</para>
    /// <para>⛔ THE IMAGE IS COMPOSED WHOLE, so the persist is ONE positional write through the connector's own
    /// handle (kb/Work PB771 — the handle is its §9.1.15 FILE LOCK, so the persist cannot take a second one) and
    /// it never depends on a stream buffer: a buffered read of a store another run unit has since rewritten would
    /// hand back the superseded bytes (kb/Work PB2660).</para></summary>
    /// <param name="attributes">The writing connector's §9.1.6 fixed file attributes — the header's content.</param>
    /// <param name="frames">One entry per ordinal position; null = an empty (gap) slot. The frames are the
    /// records in the NATIVE character set. Enumerated TWICE — once to size the image, once to compose it — so
    /// a relative store passes its slots lazily instead of materializing one entry per relative record number
    /// (a dense array sized by the highest RRN overflowed at a large key and allocated millions of slots for
    /// one record — kb/Work PB1192).</param>
    /// <param name="generation">The store's GENERATION — see <see cref="GenerationOffset"/>.</param>
    /// <param name="releaseMint">An indexed store's next release ordinal — see <see cref="ReleaseMintOffset"/>; 0 for
    /// a relative store.</param>
    /// <param name="codeSet">The file's §13.18.13 CODE-SET conversion, or null for the native character set
    /// (GR7). ⛔ It converts the PAYLOAD and not the frame: the 4-byte length prefix, the gap tag and this
    /// header are the §9.1.7.2 framing this processor adds, not data of the record (see
    /// <see cref="CodeSetConversion"/>).</param>
    /// <returns>The composed image; its <see cref="MemoryStream.Length"/> is the store's size.</returns>
    /// <exception cref="InvalidOperationException">A frame carries a number of release ordinals other than
    /// <see cref="OrdinalSlots"/> of <paramref name="attributes"/> — the decoder would mis-locate every later frame,
    /// so the persist is refused before the file is touched.</exception>
    public static MemoryStream ComposeStore(FixedFileAttributes attributes, IEnumerable<StoredFrame?> frames,
        ulong generation, long releaseMint, CodeSetConversion? codeSet = null)
    {
        int slots = OrdinalSlots(attributes);
        // Sized EXACTLY, by the one size formula the WRITE-time boundary test uses, so the compose never doubles.
        long size = HeaderBytes(attributes);
        foreach (StoredFrame? f in frames)
        {
            if (f is { } frame && (frame.Ordinals?.Length ?? 0) != slots)
                throw new InvalidOperationException($"a {attributes.Organization} store frame carries "
                    + $"{frame.Ordinals?.Length ?? 0} release ordinals where its header's key count makes it {slots}");
            size += f is { } framed ? FrameBytes(framed) : GapBytes;
        }
        // ⛔ The keyed WRITE and REWRITE refuse a record the store cannot hold ('24', DOC-A.1-107), so a store
        // past the boundary here is a defect upstream of this call; it must not become a truncated file. Raised
        // BEFORE the file is touched, as an IOException, so the statement reports §9.1.13.6 item 1's '30' and the
        // physical file keeps its previous image.
        if (size > MaxStoreBytes)
            throw new IOException($"the record store ({size} bytes) exceeds the {MaxStoreBytes}-byte capacity of a "
                + "relative or indexed file");
        var composed = new MemoryStream((int)size);
        WriteHeader(composed, attributes, generation, releaseMint);
        // One head buffer for the whole compose, grown only for a frame whose extent table outgrows it.
        byte[] head = new byte[FrameHeadBytes(null, slots) + 64];
        foreach (StoredFrame? stored in frames)
        {
            if (stored is not { } frame)
            {
                EncodeGap(head);
                composed.Write(head, 0, GapBytes);
                continue;
            }
            // The file coded character set's STRICT encoding (kb/Work PB690): the keyed WRITE/REWRITE refused any
            // record holding a character with no byte image ('91'), so an exception here is a missed refusal —
            // loud, raised before the file is truncated — never a silent '?'.
            byte[] payload = FileCharacterSet.Medium.GetBytes(FileCharacterSet.ToChannel(frame.Image, codeSet));
            int headBytes = (int)FrameHeadBytes(frame.Extents, slots);
            if (headBytes > head.Length) head = new byte[headBytes];
            EncodeFrameHead(head, payload.Length, frame.Extents, frame.Ordinals);
            composed.Write(head, 0, headBytes);
            composed.Write(payload, 0, payload.Length);
        }
        return composed;
    }

    /// <summary>Decode a whole store image: one entry per frame, null for a gap. A torn tail ends the store.
    /// An image whose header this build does not understand yields NO frames — its layout is unknown, so the
    /// bytes after it are not frames this build can locate. (A keyed OPEN never reaches here on such a file:
    /// <see cref="ReadHeader"/> answered <see cref="StoreFormat.Foreign"/> and the OPEN was '39' before
    /// <c>OpenCore</c> ran.)
    /// <para>The connector reads the image in ONE positional read through its own handle — its §9.1.15 file lock
    /// (kb/Work PB771) — and never through a stream buffer (kb/Work PB2660, see <see cref="ComposeStore"/>).</para></summary>
    /// <param name="image">The whole physical file in its first <paramref name="size"/> bytes (a rented buffer may
    /// be longer).</param>
    /// <param name="size">The physical file's length.</param>
    /// <param name="codeSet">The file's §13.18.13 CODE-SET conversion, or null — see
    /// <see cref="ComposeStore"/>. The frames come back in the NATIVE character set (§13.18.13.4 GR6 a).</param>
    /// <param name="releaseMint">The store's release mint (<see cref="ReleaseMintOffset"/>); 0 when the image holds no
    /// header this build understands.</param>
    public static List<StoredFrame?> DecodeStore(byte[] image, int size, out long releaseMint,
        CodeSetConversion? codeSet = null)
    {
        var frames = new List<StoredFrame?>();
        releaseMint = 0;
        // The ONE decoder still answers "where do the frames start" — over the bytes already in hand, so a header
        // this build cannot read still yields no frames (see the summary of DecodeHeader).
        var over = new MemoryStream(image, 0, size, writable: false);
        if (DecodeHeader(over) is not { } attributes) return frames;   // empty, or a layout this build cannot locate frames in
        releaseMint = BinaryPrimitives.ReadInt64LittleEndian(image.AsSpan(ReleaseMintOffset));
        int slots = OrdinalSlots(attributes);
        var medium = new ImageMedium(image, size, (int)over.Position);
        while (ReadFrame(ref medium, slots, out FrameHead head) is var kind && kind != FrameKind.End)
            frames.Add(kind == FrameKind.Gap ? null
                : new StoredFrame(FileCharacterSet.FromChannel(medium.Payload!, codeSet), head.Extents, head.Ordinals));
        return frames;
    }

    /// <summary>A store image in memory as a frame medium: its first <c>size</c> bytes, read from <see cref="At"/>.
    /// The payload is taken as its Latin-1 characters (one per byte), the channel shape the frames are converted from.</summary>
    private struct ImageMedium(byte[] image, int size, int at) : IFrameMedium
    {
        /// <summary>The next byte to read.</summary>
        public int At = at;

        /// <summary>The payload the last <see cref="TakePayload"/> took.</summary>
        public string? Payload;

        public bool Fill(Span<byte> into)
        {
            if (!Holds(into.Length)) return false;
            image.AsSpan(At, into.Length).CopyTo(into);
            At += into.Length;
            return true;
        }

        public bool TakePayload(int length)
        {
            Payload = Encoding.Latin1.GetString(image, At, length);   // ReadFrame asked Holds(length) first
            At += length;
            return true;
        }

        public readonly bool Holds(long bytes) => bytes <= size - At;
    }

    // ── The store GENERATION — the cross-run-unit coherence stamp (kb/Work PB2660) ───────────────────────────

    /// <summary>⛔ THE STORE'S GENERATION lives in the fixed header at this offset, 8 bytes little-endian, and it
    /// is stamped by EVERY persist: the persisting connector writes one more than the generation it read from the
    /// file under the store's cross-run-unit mutex. A connector that may meet another run unit's writer
    /// (<c>KeyedConnector</c>, kb/Work PB2660) reads these 8 bytes at the start of each statement and reloads the
    /// store only when they differ from the generation its in-memory store reflects — so the common statement pays
    /// one small read, never a whole-store load. It is the keyed twin of the sequential organization's
    /// <c>PhysicalFileTable.State.ReleaseGeneration</c>, which can live in memory because a sequential sibling is
    /// in the same run unit; another run unit can only be told through the file.</summary>
    public const int GenerationOffset = 20;

    /// <summary>The bytes a reader takes to learn a store's generation: the fixed part of the header.</summary>
    public const int GenerationProbeBytes = FixedHeaderBytes;

    /// <summary>The generation stamped in <paramref name="fixedHeader"/> — the first
    /// <see cref="GenerationProbeBytes"/> bytes of a store — or null when they are not a header this build
    /// understands (an empty file included).</summary>
    public static ulong? DecodeGeneration(ReadOnlySpan<byte> fixedHeader)
    {
        if (fixedHeader.Length < FixedHeaderBytes || !fixedHeader[..Magic.Length].SequenceEqual(Magic)
            || fixedHeader[Magic.Length] != FormatVersion)
            return null;
        return BinaryPrimitives.ReadUInt64LittleEndian(fixedHeader[GenerationOffset..]);
    }

    /// <summary>The §9.1.6 fixed file attributes the store at <paramref name="path"/> RECORDS — the read half of
    /// §14.9.27.4 GR10 for the RELATIVE and INDEXED organizations, called once per OPEN by
    /// <c>KeyedConnector.FixedAttributeConflict</c>.
    /// <para>An I/O failure answers <see cref="StoreFormat.Empty"/> and never a conflict: this bounded read is
    /// bookkeeping, and the authority questions (§14.9.27.4 GR3's '37', §9.1.13.6 item 1's '30') are answered in
    /// ONE place each — <c>HostFile.Probe</c>, <c>HostFile.PermitsWrite</c> and <c>FileConnector.Open</c>'s catch
    /// — by the connector's own stream, not by guessing here.</para></summary>
    public static StoreFormat ReadHeader(string path, out FixedFileAttributes? attributes)
    {
        attributes = null;
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            if (fs.Length == 0) return StoreFormat.Empty;
            attributes = DecodeHeader(fs);
            return attributes is null ? StoreFormat.Foreign : StoreFormat.Described;
        }
        catch (IOException) { return StoreFormat.Empty; }
        catch (UnauthorizedAccessException) { return StoreFormat.Empty; }
        catch (ArgumentException) { return StoreFormat.Empty; }
        catch (NotSupportedException) { return StoreFormat.Empty; }
    }

    /// <summary>Write the store header. Kept beside <see cref="DecodeHeader"/> so the two halves of one byte
    /// layout cannot drift apart.</summary>
    private static void WriteHeader(Stream fs, FixedFileAttributes a, ulong generation, long releaseMint)
    {
        var head = new byte[FixedHeaderBytes];
        Magic.CopyTo(head);
        head[Magic.Length] = FormatVersion;
        head[8] = a.Organization == FixedFileAttributes.Indexed ? (byte)'I' : (byte)'R';
        head[9] = a.Varying ? (byte)'V' : (byte)'F';
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(10), a.MinRecordSize);
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(14), a.MaxRecordSize);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(18), (ushort)a.Keys.Count);
        BinaryPrimitives.WriteUInt64LittleEndian(head.AsSpan(GenerationOffset), generation);
        BinaryPrimitives.WriteInt64LittleEndian(head.AsSpan(ReleaseMintOffset), releaseMint);
        fs.Write(head, 0, head.Length);
        Span<byte> word = stackalloc byte[4];
        foreach (var k in a.Keys)
        {
            BinaryPrimitives.WriteInt32LittleEndian(word, k.Offset);
            fs.Write(word);
            BinaryPrimitives.WriteInt32LittleEndian(word, k.Length);
            fs.Write(word);
            fs.WriteByte(k.Duplicates ? (byte)1 : (byte)0);
            // §12.4.5.6.4 GR6 admits ANY figurative-constant or literal SUPPRESS WHEN value, so it is written
            // with an explicit length rather than a delimiter: a value carrying a NUL or a line ending is legal.
            // ⛔ It is written as its exact UTF-16 CODE UNITS (kb/Work PB1602). The header records what the
            // program DECLARED, and §12.4.5.3's OPEN comparison asks it back, so the encoding must be lossless over
            // the whole repertoire (UTF-16 — the alphanumeric AND national literal both). Latin-1 turned "€€" into
            // "??", and the program could never reopen the file it had just written ('39'). An Encoding would also
            // be lossy: it replaces a lone surrogate (a legal NX"D800" national literal) with U+FFFD.
            WriteCodeUnits(fs, k.Suppress);
            byte[] collation = Encoding.ASCII.GetBytes(k.Collation);
            fs.WriteByte((byte)collation.Length);
            fs.Write(collation, 0, collation.Length);
        }
    }

    /// <summary>Write an optional string as a little-endian Int32 code-unit count biased by one (0 = absent), then
    /// its UTF-16 code units, each little-endian — the exact <see cref="string"/>, lone surrogates included
    /// (kb/Work PB1602). <see cref="ReadCodeUnits"/> is its inverse.</summary>
    private static void WriteCodeUnits(Stream fs, string? s)
    {
        var buf = new byte[4 + 2 * (s?.Length ?? 0)];
        BinaryPrimitives.WriteInt32LittleEndian(buf, s is null ? 0 : s.Length + 1);
        for (int i = 0; i < (s?.Length ?? 0); i++)
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(4 + 2 * i), s![i]);
        fs.Write(buf, 0, buf.Length);
    }

    /// <summary>The inverse of <see cref="WriteCodeUnits"/>; false when the stream ends inside the value or the
    /// count is not one a header can hold.</summary>
    private static bool ReadCodeUnits(Stream fs, out string? s)
    {
        s = null;
        var word = new byte[4];
        if (!FillExactly(fs, word)) return false;
        int biased = BinaryPrimitives.ReadInt32LittleEndian(word);
        if (biased == 0) return true;
        if (biased < 0 || biased - 1 > Array.MaxLength / 2) return false;
        var buf = new byte[2 * (biased - 1)];
        if (!FillExactly(fs, buf)) return false;
        var chars = new char[biased - 1];
        for (int i = 0; i < chars.Length; i++) chars[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(2 * i));
        s = new string(chars);
        return true;
    }

    /// <summary>Decode the store header from the CURRENT position, leaving the stream on the first frame; null
    /// when the bytes are not a header this build understands (an empty file included). The ONE decoder — both
    /// <see cref="DecodeStore"/> and <see cref="ReadHeader"/> reach the frames through it, so "where do the frames
    /// start" has exactly one answer.</summary>
    private static FixedFileAttributes? DecodeHeader(Stream fs)
    {
        var head = new byte[FixedHeaderBytes];
        if (!FillExactly(fs, head)) return null;
        if (!head.AsSpan(0, Magic.Length).SequenceEqual(Magic) || head[Magic.Length] != FormatVersion)
            return null;
        string org = head[8] switch
        {
            (byte)'R' => FixedFileAttributes.Relative,
            (byte)'I' => FixedFileAttributes.Indexed,
            _ => "",
        };
        if (org.Length == 0) return null;
        if (head[9] is not ((byte)'F' or (byte)'V')) return null;
        int min = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(10));
        int max = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(14));
        if (min < 0 || max < 0) return null;
        int count = BinaryPrimitives.ReadUInt16LittleEndian(head.AsSpan(18));
        var keys = new List<FixedFileAttributes.KeyDescriptor>(count);
        var word = new byte[4];
        for (int i = 0; i < count; i++)
        {
            if (!FillExactly(fs, word)) return null;
            int off = BinaryPrimitives.ReadInt32LittleEndian(word);
            if (!FillExactly(fs, word)) return null;
            int len = BinaryPrimitives.ReadInt32LittleEndian(word);
            int dup = fs.ReadByte();
            if (dup < 0) return null;
            if (!ReadCodeUnits(fs, out string? suppress)) return null;
            int collLen = fs.ReadByte();
            if (collLen < 0) return null;
            var coll = new byte[collLen];
            if (!FillExactly(fs, coll)) return null;
            keys.Add(new FixedFileAttributes.KeyDescriptor(off, len, dup != 0, suppress, Encoding.ASCII.GetString(coll)));
        }
        return new FixedFileAttributes(org, head[9] == (byte)'V', min, max, keys);
    }

    /// <summary>The byte offset of every frame in a STREAM-LEVEL framed physical file, in order (index =
    /// ordinal − 1) — a RECORD VARYING record-sequential file, which carries no header.
    /// This is the positioning index a §14.9.30.4 GR21 BACKWARD sequential READ needs on a RECORD VARYING file,
    /// whose frames are not uniformly wide the way a fixed record-sequential file's blocks are. Only the frame
    /// heads are read — each payload is SEEKED over, never materialized — so the index costs one pass and no
    /// record storage. Where the frames end is the one decoder's answer (<see cref="ReadStreamFrame"/>), so the
    /// index holds exactly the records a forward READ delivers.
    /// <para>⛔ IT INDEXES THE CONNECTOR'S OWN OPEN STREAM, NOT A PATH. Only a backward READ on an ALREADY-OPEN
    /// varying file asks for this index, so the presence question is already answered and asking the host again
    /// would be a second answer to it: <see cref="HostFile.Probe"/> is the ONE place the runtime asks the
    /// operating environment about a physical file and it is asked once per OPEN by <c>FileConnector.Open</c>,
    /// never by an organization body (<c>HostFileProbeDriftTests</c>, kb/Work PB323). Re-opening by path would
    /// also take a SECOND handle on a file the connector may hold under a deny-share mode, and could index a
    /// different file than the one the connector is reading. The walk moves the stream position and leaves it
    /// where it stops; the backward READ reaches <c>SequentialConnector.SeekToRecord</c> immediately afterwards,
    /// which seeks and discards the reader's buffer. The record COUNTS of <c>SequentialConnector.CountRecords</c>
    /// walk an auxiliary stream instead, because they run where the connector's own handle is a writer.</para>
    /// <para><paramref name="from"/> and <paramref name="to"/> bound the walk to the frames that START in
    /// [from, to): <paramref name="from"/> shall be a frame boundary (kb/Work PB2692 counts only the frames
    /// another run unit appended since the last count).</para></summary>
    public static List<long> FrameStarts(Stream fs, long from = 0, long to = long.MaxValue)
    {
        var starts = new List<long>();
        fs.Seek(from, SeekOrigin.Begin);
        var medium = new StreamMedium(fs);
        for (long at = from; at < to && ReadStreamFrame(ref medium, out _); at = fs.Position)
            starts.Add(at);
        return starts;
    }

    /// <summary>Does the STREAM-LEVEL record-length framing at <paramref name="path"/> PARSE — the whole of
    /// §14.9.27.4 GR10 for a RECORD VARYING record-sequential file, and the only §9.1.6 attribute such a file's
    /// format encodes.
    /// <para>§9.1.7.2 makes the prefix the file's own statement of its record type: "In record sequential files
    /// the length of each record is determined by any information the implementor may add to the record on the
    /// physical storage medium (such as record length headers)". A first frame that the one decoder cannot read
    /// (<see cref="ReadStreamFrame"/>: a head naming more bytes than the file holds, a gap tag, a table this writer
    /// cannot have produced) is not a framed file at all, so a connector declaring the VARIABLE record type
    /// contradicts the medium and the OPEN is unsuccessful with '39' (§9.1.13.6 item 7). ⛔ THE BOUND IS THE FILE,
    /// NOT THE RECORD CLAUSE: a prefix that parses but falls outside the connector's VaryMin/VaryMax is exactly
    /// §9.1.13.2 item 3's '04' — "A READ statement is successfully executed but the physical record from the
    /// file is shorter than or longer than the minimum or maximum length of records allowed for the fixed file
    /// attributes for that file" — a SUCCESSFUL completion that a '39' at the OPEN would make unreachable.</para>
    /// <para>Only the FIRST frame is tested, and that is the rule rather than a shortcut: a torn or ragged TAIL
    /// already ends the data for every reader (<see cref="FrameKind.End"/>), so a full walk would answer a question
    /// the framing has already settled, at a cost proportional to the file. It is also what the surveyed
    /// implementation does. A file shorter than one length word states nothing and is not a conflict. An I/O
    /// failure likewise answers "no conflict" — the authority statuses are the connector's own stream's to
    /// raise.</para></summary>
    public static bool StreamFramingParses(string path)
    {
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            var medium = new StreamMedium(fs);
            return !medium.Holds(4) || ReadStreamFrame(ref medium, out _);   // fewer than a length word: nothing is claimed
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
        catch (ArgumentException) { return true; }
        catch (NotSupportedException) { return true; }
    }

    /// <summary>A stream-level framed file walked from its current position as a frame medium: the heads are read
    /// and each payload is SEEKED over, never materialized.
    /// <para>⛔ THE WALK READS THE FILE AS IT STOOD WHEN THE MEDIUM WAS MADE: the stream's length is asked ONCE, here,
    /// and every <see cref="Holds"/> and <see cref="Fill"/> is bounded by that snapshot. A stream's length is an
    /// uncached operating-environment query whenever the handle admits other writers, and every bookkeeping handle
    /// does (<c>HostFile.OpenAuxiliary</c> shares <see cref="FileShare.ReadWrite"/>), so asking it per frame made it
    /// the dominant cost of an O(file) walk (kb/Work PB2754, review R1-1). Bytes another run unit appends during the
    /// walk lie past the snapshot and end the data there, as an append that has not completed does; the frame
    /// index and the record count this walk produces are point-in-time answers already.</para></summary>
    internal readonly struct StreamMedium(Stream fs) : IFrameMedium
    {
        /// <summary>The stream's length when the walk began: the end of the data this medium reads.</summary>
        private readonly long _end = fs.Length;

        public bool Fill(Span<byte> into) => Holds(into.Length) && FillExactly(fs, into);

        public bool TakePayload(int length)
        {
            fs.Seek(length, SeekOrigin.Current);   // ReadFrame asked Holds(length) first
            return true;
        }

        public bool Holds(long bytes) => bytes <= _end - fs.Position;
    }

    private static bool FillExactly(Stream s, Span<byte> buf) =>
        s.ReadAtLeast(buf, buf.Length, throwOnEndOfStream: false) == buf.Length;

    // ── THE frame head — one encoder, one decoder, for both shapes (kb/Work PB2754) ───────────────────────────

    /// <summary>⛔ THE ONE FRAME DECODER. Reads the frame at <paramref name="medium"/>'s position — its length word,
    /// its EXTENT TABLE when bit 31 says one follows, <paramref name="ordinalSlots"/> release ordinals, and its
    /// payload — and says what it found (<see cref="FrameKind"/>). It alone decides the two questions the framing
    /// has to answer about a damaged file, so no reader can answer them differently (kb/Work PB2754):
    /// <list type="bullet">
    /// <item>a GAP is the length word <see cref="GapTag"/>, with nothing after it;</item>
    /// <item>a frame the data ends inside — or whose head this writer cannot have produced (a table component of
    /// negative length) — is <see cref="FrameKind.End"/>: a torn tail ends the data. A count or a length is checked
    /// against what the medium still holds (<see cref="IFrameMedium.Holds"/>) BEFORE anything is sized by it.</item>
    /// </list>
    /// The framing is the §9.1.7.2 information "the implementor may add to the record on the physical storage medium";
    /// none of it reaches the record area (§12.4.5.11.4 GR1).</summary>
    public static FrameKind ReadFrame<TMedium>(ref TMedium medium, int ordinalSlots, out FrameHead head)
        where TMedium : struct, IFrameMedium
    {
        head = default;
        Span<byte> word = stackalloc byte[OrdinalBytes];
        if (!medium.Fill(word[..4])) return FrameKind.End;
        uint lengthWord = BinaryPrimitives.ReadUInt32LittleEndian(word);
        if (lengthWord == GapTag) return FrameKind.Gap;
        RecordExtents? extents = null;
        if ((lengthWord & ExtentFlag) != 0 && (extents = ReadExtentTable(ref medium)) is null) return FrameKind.End;
        long[]? ordinals = null;
        if (ordinalSlots > 0)
        {
            ordinals = new long[ordinalSlots];
            for (int k = 0; k < ordinalSlots; k++)
            {
                if (!medium.Fill(word)) return FrameKind.End;
                ordinals[k] = BinaryPrimitives.ReadInt64LittleEndian(word);
            }
        }
        int payloadLength = (int)(lengthWord & ~ExtentFlag);
        if (!medium.Holds(payloadLength) || !medium.TakePayload(payloadLength)) return FrameKind.End;
        head = new FrameHead(payloadLength, extents, ordinals);
        return FrameKind.Record;
    }

    /// <summary><see cref="ReadFrame"/> over a STREAM-LEVEL file, true for a record's frame. A stream-level file
    /// holds no gaps — no writer of one writes the tag, and a payload long enough to spell it is not representable —
    /// so a gap word there is not a frame of the file: it ends the data like a torn tail. The one answer for the
    /// forward READ (<c>SequentialConnector.NextFrame</c>), the record count and the backward-READ index
    /// (<see cref="FrameStarts"/>) and the OPEN's framing check (<see cref="StreamFramingParses"/>).</summary>
    public static bool ReadStreamFrame<TMedium>(ref TMedium medium, out FrameHead head)
        where TMedium : struct, IFrameMedium =>
        ReadFrame(ref medium, ordinalSlots: 0, out head) == FrameKind.Record;

    /// <summary>The extent table that follows a length word: its count, then (fixed-run offset, length) per
    /// component. Null when the data ends inside it or a length is negative (a table this writer cannot have
    /// produced) — <see cref="ReadFrame"/> then answers <see cref="FrameKind.End"/>.</summary>
    private static RecordExtents? ReadExtentTable<TMedium>(ref TMedium medium) where TMedium : struct, IFrameMedium
    {
        Span<byte> component = stackalloc byte[8];
        if (!medium.Fill(component[..4])) return null;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(component);
        if (!medium.Holds(8L * count)) return null;
        var fixedAt = new int[count];
        var lengths = new int[count];
        for (int k = 0; k < count; k++)
        {
            if (!medium.Fill(component)) return null;
            fixedAt[k] = BinaryPrimitives.ReadInt32LittleEndian(component);
            lengths[k] = BinaryPrimitives.ReadInt32LittleEndian(component[4..]);
            if (lengths[k] < 0) return null;
        }
        return new RecordExtents(fixedAt, lengths);
    }

    /// <summary>⛔ THE ONE FRAME-HEAD ENCODER, for both shapes: into the first <see cref="FrameHeadBytes"/> bytes of
    /// <paramref name="into"/>, the length word (<paramref name="payloadLength"/>, with bit 31 when
    /// <paramref name="extents"/> is given), the extent table and the release ordinals. The payload follows it.
    /// The sequential connector writes these bytes through its Latin-1 channel, one character per byte.</summary>
    public static void EncodeFrameHead(Span<byte> into, int payloadLength, RecordExtents? extents, ReadOnlySpan<long> ordinals)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(into, (uint)payloadLength | (extents is null ? 0u : ExtentFlag));
        int at = 4;
        if (extents is not null) at += EncodeExtentTable(into[at..], extents);
        foreach (long o in ordinals)
        {
            BinaryPrimitives.WriteInt64LittleEndian(into[at..], o);
            at += OrdinalBytes;
        }
    }

    /// <summary>An empty slot's frame — the <see cref="GapTag"/> alone — into the first <see cref="GapBytes"/> bytes
    /// of <paramref name="into"/>.</summary>
    private static void EncodeGap(Span<byte> into) => BinaryPrimitives.WriteUInt32LittleEndian(into, GapTag);

    /// <summary>A VOID table of <paramref name="count"/> components: every fixed-run offset is -1, which no record
    /// layout has, so it corresponds to no receiving record (<see cref="RecordExtents.Describes"/>) and the record it
    /// frames reads back by the take step. What an in-place REWRITE leaves in a frame whose table cannot hold the
    /// replacing record's — the frame's size is fixed (§14.9.35.4 GR16), and keeping the replaced record's table
    /// would describe characters that are no longer there.</summary>
    public static RecordExtents VoidExtents(int count)
    {
        var fixedAt = new int[count];
        Array.Fill(fixedAt, -1);
        return new RecordExtents(fixedAt, new int[count]);
    }

    /// <summary>The bytes an extent table occupies on the medium (0 when there is none).</summary>
    public static int ExtentTableBytes(RecordExtents? extents) => extents is null ? 0 : 4 + (8 * extents.Count);

    /// <summary>The ONE encoder of an extent table — its count, then (fixed-run offset, length) per component — into
    /// the first <see cref="ExtentTableBytes"/> bytes of <paramref name="into"/>; returns that count of bytes. The
    /// sequential connector's in-place REWRITE replaces a frame's table with these bytes.</summary>
    public static int EncodeExtentTable(Span<byte> into, RecordExtents extents)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(into, (uint)extents.Count);
        for (int k = 0; k < extents.Count; k++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(into[(4 + (8 * k))..], extents.FixedAt[k]);
            BinaryPrimitives.WriteInt32LittleEndian(into[(8 + (8 * k))..], extents.Lengths[k]);
        }
        return ExtentTableBytes(extents);
    }
}
