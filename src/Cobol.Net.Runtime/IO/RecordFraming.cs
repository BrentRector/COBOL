// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;
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
/// information, never part of the record (§12.4.5.11.4 GR1).</summary>
internal readonly record struct StoredFrame(string Image, RecordExtents? Extents);

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
/// <item><b>Store-level</b> (<see cref="WriteStore"/>/<see cref="ReadStore"/>/<see cref="ReadHeader"/>) — the
/// whole-file rewrite/load the keyed connectors use (relative slots + the indexed persist order —
/// IndexedConnector.PersistOrder). It opens with the header described below.</item>
/// <item><b>Stream-level</b> (<see cref="WritePrefix"/>/<see cref="PrefixLength"/>/<see cref="FrameStarts"/>) —
/// the incremental prefix-per-record shape the sequential connector streams through its Latin-1 reader/writer
/// (chars 0–255 map 1:1 to bytes under Latin-1, so the char-shaped prefix is byte-identical to the store-level
/// one). It has NO header, deliberately: a header would stop a record sequential file being plain bytes and a
/// line sequential file being plain text, which is the interchange property those shapes exist for.</item>
/// </list>
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
///   0   8  magic "CBNFSTR" + format version (currently 1)
///   8   1  organization    'R' relative · 'I' indexed
///   9   1  record type     'F' fixed · 'V' varying
///  10   4  minimum logical record size (bytes)
///  14   4  maximum logical record size (bytes)
///  18   2  key count (0 for a relative store)
///  20  ..  per key: offset(4) length(4) flags(1: bit0 DUPLICATES) suppress-length(2) suppress(Latin-1)
///          collation-length(1) collation(ASCII fingerprint)
/// </code>
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
    /// version-1 store is Foreign to this build: no reader for it is kept.</para></summary>
    private const byte FormatVersion = 2;

    /// <summary>The fixed part of the header — magic, organization, record type, the two record sizes and the
    /// key count. Per-key descriptors follow it.</summary>
    private const int FixedHeaderBytes = 20;

    // ── The store's capacity — the externally-defined boundary of a relative or indexed file ───────────────

    /// <summary>⛔ THE LARGEST STORE IMAGE THIS FORMAT CAN HOLD, in bytes — the externally-defined boundary of a
    /// physical relative or indexed file (ISO §9.1.13.5 item 4, <i>"The implementor specifies the manner in which
    /// these boundaries are defined"</i>; Annex A.1 item 107, docs/CONFORMANCE.md <c>DOC-A.1-107</c>;
    /// kb/Work PB1192). It is <see cref="Array.MaxLength"/> because the whole image is composed in one array by
    /// <see cref="WriteStore"/> and taken back in one by <see cref="ReadStore"/>: a store one byte larger could be
    /// neither persisted nor loaded. The keyed connectors test it AT THE WRITE (§14.9.51.4 GR33 b) / GR42 d)'s
    /// '24'), so a record the store cannot hold is refused before it is released instead of being reported '00'
    /// and failing at the CLOSE.</summary>
    public const long MaxStoreBytes = 0x7FFFFFC7;   // Array.MaxLength (a runtime property; its value is fixed)

    /// <summary>The bytes an EMPTY relative slot occupies — its <see cref="GapTag"/> alone. Every slot below the
    /// highest relative record number that holds no record costs this much, which is why the relative record
    /// number is bounded by the store's capacity long before it is bounded by its own range.</summary>
    public const int GapBytes = 4;

    /// <summary>The bytes one record occupies in the store: its 4-byte length word, its extent table (if any)
    /// and one byte per character — the file coded character set is one byte per character position
    /// (<see cref="FileCharacterSet"/>), and a CODE-SET conversion is a per-character map, so the payload length
    /// is the image's. The ONE size formula, shared by the WRITE-time boundary test and <see cref="WriteStore"/>'s
    /// composition, so the two cannot disagree about what fits.</summary>
    public static long FrameBytes(StoredFrame frame) => 4L + ExtentTableBytes(frame.Extents) + frame.Image.Length;

    /// <summary>The bytes the store header for <paramref name="attributes"/> occupies — measured by encoding it
    /// with the ONE header writer, so the size cannot drift from the layout.</summary>
    public static long HeaderBytes(FixedFileAttributes attributes)
    {
        var header = new MemoryStream(FixedHeaderBytes + (64 * attributes.Keys.Count));
        WriteHeader(header, attributes);
        return header.Length;
    }

    // ── Store-level (byte) shape — the keyed connectors' whole-store persist/load ───────────────────────────

    /// <summary>Write the whole store: the §9.1.6 header, then one frame per ordinal position; null = an empty
    /// (gap) slot.
    /// <para>The header is stamped on EVERY persist, from the writing connector's own declared attributes, so a
    /// store always describes itself. That is not a re-establishment of §9.1.6's "at the time it is created":
    /// a connector whose attributes disagreed with the file's could not have opened it (§14.9.27.4 GR10 answered
    /// '39' before <c>OpenCore</c>), so every persist writes back the attributes the file already had — except
    /// under the documented <c>COBOLNET_KEYCHECK</c> opt-out, where a connector admitted with a different key
    /// table writes ITS key table, which is what opting out of the key check means.</para>
    /// <para>⛔ IT WRITES THROUGH THE CONNECTOR'S OWN HANDLE, AND THE WHOLE STORE IS ONE <c>Write</c>
    /// (kb/Work PB771). The handle is the connector's §9.1.15 FILE LOCK — the only thing that says anything to
    /// another run unit — so the persist cannot take a second one: a <c>SHARING WITH NO OTHER</c> connector's
    /// <see cref="FileShare.None"/> would refuse it, and the previous shape (a fresh
    /// <c>HostFile.OpenAuxiliary(path, FileMode.Create, …)</c> per persist) is exactly why the keyed
    /// organizations held no lock at all. The stream is TRUNCATED here rather than by a <c>FileMode.Create</c>
    /// at open, because the handle outlives every persist and a store may shrink. Composing the bytes first and
    /// writing them once also makes the persist independent of the handle's buffer size — which the posture
    /// decides (<see cref="HostFile.OpenConnectorStream"/>) and which a whole-store rewrite must not depend
    /// on.</para></summary>
    /// <param name="fs">The connector's own open handle on the physical file, positioned anywhere.</param>
    /// <param name="attributes">The writing connector's §9.1.6 fixed file attributes — the header's content.</param>
    /// <param name="frames">One entry per ordinal position; null = an empty (gap) slot. The frames are the
    /// records in the NATIVE character set. Enumerated TWICE — once to size the image, once to compose it — so
    /// a relative store passes its slots lazily instead of materializing one entry per relative record number
    /// (a dense array sized by the highest RRN overflowed at a large key and allocated millions of slots for
    /// one record — kb/Work PB1192).</param>
    /// <param name="codeSet">The file's §13.18.13 CODE-SET conversion, or null for the native character set
    /// (GR7). ⛔ It converts the PAYLOAD and not the frame: the 4-byte length prefix, the gap tag and this
    /// header are the §9.1.7.2 framing this processor adds, not data of the record (see
    /// <see cref="CodeSetConversion"/>).</param>
    public static void WriteStore(Stream fs, FixedFileAttributes attributes, IEnumerable<StoredFrame?> frames,
        CodeSetConversion? codeSet = null)
    {
        // Sized EXACTLY, by the one size formula the WRITE-time boundary test uses, so the compose never doubles.
        long size = HeaderBytes(attributes);
        foreach (StoredFrame? f in frames) size += f is { } frame ? FrameBytes(frame) : GapBytes;
        // ⛔ The keyed WRITE and REWRITE refuse a record the store cannot hold ('24', DOC-A.1-107), so a store
        // past the boundary here is a defect upstream of this call; it must not become a truncated file. Raised
        // BEFORE the stream is touched, as an IOException, so the CLOSE reports §9.1.13.6 item 1's '30' and the
        // physical file keeps its previous image.
        if (size > MaxStoreBytes)
            throw new IOException($"the record store ({size} bytes) exceeds the {MaxStoreBytes}-byte capacity of a "
                + "relative or indexed file");
        var composed = new MemoryStream((int)size);
        WriteHeader(composed, attributes);
        Span<byte> len = stackalloc byte[4];
        foreach (StoredFrame? stored in frames)
        {
            if (stored is not { } frame)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(len, GapTag);
                composed.Write(len);
                continue;
            }
            // The file coded character set's STRICT encoding (kb/Work PB690): the keyed WRITE/REWRITE refused any
            // record holding a character with no byte image ('91'), so an exception here is a missed refusal —
            // loud, raised before the stream is truncated — never a silent '?'.
            byte[] payload = FileCharacterSet.Medium.GetBytes(codeSet is null ? frame.Image : codeSet.ToMedium(frame.Image));
            BinaryPrimitives.WriteUInt32LittleEndian(len, FrameWord(payload.Length, frame.Extents));
            composed.Write(len);
            if (frame.Extents is { } extents) composed.Write(EncodeExtentTable(extents));
            composed.Write(payload, 0, payload.Length);
        }
        fs.Seek(0, SeekOrigin.Begin);
        fs.SetLength(0);
        fs.Write(composed.GetBuffer(), 0, (int)composed.Length);
        fs.Flush();
    }

    /// <summary>Read the whole store back: one entry per frame, null for a gap. A torn tail ends the store.
    /// A file whose header this build does not understand yields NO frames — its layout is unknown, so the
    /// bytes after it are not frames this build can locate. (A keyed OPEN never reaches here on such a file:
    /// <see cref="ReadHeader"/> answered <see cref="StoreFormat.Foreign"/> and the OPEN was '39' before
    /// <c>OpenCore</c> ran. The arm exists because <c>OPEN OUTPUT</c> loads before it truncates, and it must
    /// load nothing rather than garbage.)
    /// <para>⛔ IT READS THROUGH THE CONNECTOR'S OWN HANDLE, IN ONE PASS (kb/Work PB771), for the reason
    /// <see cref="WriteStore"/> gives: that handle IS the connector's §9.1.15 file lock, so the load cannot take
    /// a second one. The whole file is taken in a single <c>Read</c> and parsed in memory — the store's records
    /// are materialized as strings anyway, so the transient byte array is proportionate, and it makes the load
    /// independent of the handle's posture-decided buffer size instead of paying two reads per frame through
    /// it.</para></summary>
    /// <param name="fs">The connector's own open handle on the physical file, positioned anywhere.</param>
    /// <param name="codeSet">The file's §13.18.13 CODE-SET conversion, or null — see
    /// <see cref="WriteStore"/>. The frames come back in the NATIVE character set (§13.18.13.4 GR6 a).</param>
    public static List<StoredFrame?> ReadStore(Stream fs, CodeSetConversion? codeSet = null)
    {
        var frames = new List<StoredFrame?>();
        fs.Seek(0, SeekOrigin.Begin);
        int size = checked((int)fs.Length);
        // RENTED, not allocated: a keyed store is routinely past the 85 KB large-object threshold and this runs
        // once per OPEN, so a fresh array per open would be LOH churn for a buffer that dies immediately.
        byte[] all = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            fs.ReadExactly(all, 0, size);
            // The ONE decoder still answers "where do the frames start" — over the bytes already in hand, so a
            // header this build cannot read still yields no frames (see the summary of DecodeHeader). The length
            // is the FILE's, never the rented array's, which is only >= it.
            var over = new MemoryStream(all, 0, size, writable: false);
            if (DecodeHeader(over) is null) return frames;   // empty, or a layout this build cannot locate frames in
            int at = (int)over.Position;
            while (at + 4 <= size)
            {
                uint word = BinaryPrimitives.ReadUInt32LittleEndian(all.AsSpan(at));
                at += 4;
                if (word == GapTag) { frames.Add(null); continue; }
                RecordExtents? extents = null;
                if ((word & ExtentFlag) != 0)
                {
                    if (DecodeExtentTable(all.AsSpan(at, size - at), out int tableBytes) is not { } table) break;   // torn
                    extents = table;
                    at += tableBytes;
                }
                uint n = word & ~ExtentFlag;
                if ((long)at + n > size) break;          // a torn tail ends the store (long: n is a uint)
                string image = Encoding.Latin1.GetString(all, at, (int)n);
                at += (int)n;
                frames.Add(new StoredFrame(codeSet is null ? image : codeSet.ToNative(image), extents));
            }
            return frames;
        }
        finally { ArrayPool<byte>.Shared.Return(all); }
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
    private static void WriteHeader(Stream fs, FixedFileAttributes a)
    {
        var head = new byte[FixedHeaderBytes];
        Magic.CopyTo(head);
        head[Magic.Length] = FormatVersion;
        head[8] = a.Organization == FixedFileAttributes.Indexed ? (byte)'I' : (byte)'R';
        head[9] = a.Varying ? (byte)'V' : (byte)'F';
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(10), a.MinRecordSize);
        BinaryPrimitives.WriteInt32LittleEndian(head.AsSpan(14), a.MaxRecordSize);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(18), (ushort)a.Keys.Count);
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
        if (!FillExactly(fs, word, 4)) return false;
        int biased = BinaryPrimitives.ReadInt32LittleEndian(word);
        if (biased == 0) return true;
        if (biased < 0 || biased - 1 > Array.MaxLength / 2) return false;
        var buf = new byte[2 * (biased - 1)];
        if (!FillExactly(fs, buf, buf.Length)) return false;
        var chars = new char[biased - 1];
        for (int i = 0; i < chars.Length; i++) chars[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(2 * i));
        s = new string(chars);
        return true;
    }

    /// <summary>Decode the store header from the CURRENT position, leaving the stream on the first frame; null
    /// when the bytes are not a header this build understands (an empty file included). The ONE decoder — both
    /// <see cref="ReadStore"/> and <see cref="ReadHeader"/> reach the frames through it, so "where do the frames
    /// start" has exactly one answer.</summary>
    private static FixedFileAttributes? DecodeHeader(Stream fs)
    {
        var head = new byte[FixedHeaderBytes];
        if (!FillExactly(fs, head, FixedHeaderBytes)) return null;
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
            if (!FillExactly(fs, word, 4)) return null;
            int off = BinaryPrimitives.ReadInt32LittleEndian(word);
            if (!FillExactly(fs, word, 4)) return null;
            int len = BinaryPrimitives.ReadInt32LittleEndian(word);
            int dup = fs.ReadByte();
            if (dup < 0) return null;
            if (!ReadCodeUnits(fs, out string? suppress)) return null;
            int collLen = fs.ReadByte();
            if (collLen < 0) return null;
            var coll = new byte[collLen];
            if (!FillExactly(fs, coll, collLen)) return null;
            keys.Add(new FixedFileAttributes.KeyDescriptor(off, len, dup != 0, suppress, Encoding.ASCII.GetString(coll)));
        }
        return new FixedFileAttributes(org, head[9] == (byte)'V', min, max, keys);
    }

    /// <summary>The byte offset of every frame in a STREAM-LEVEL framed physical file, in order (index =
    /// ordinal − 1) — a RECORD VARYING record-sequential file, which carries no header.
    /// This is the positioning index a §14.9.30.4 GR21 BACKWARD sequential READ needs on a RECORD VARYING file,
    /// whose frames are not uniformly wide the way a fixed record-sequential file's blocks are. Only the length
    /// prefixes are read — each payload is SEEKED over, never materialized — so the index costs one pass and no
    /// record storage. A torn tail ends the store, the same rule <see cref="ReadStore"/> applies.
    /// <para>⛔ IT INDEXES THE CONNECTOR'S OWN OPEN STREAM, NOT A PATH. Only a backward READ on an ALREADY-OPEN
    /// varying file asks for this index, so the presence question is already answered and asking the host again
    /// would be a second answer to it: <see cref="HostFile.Probe"/> is the ONE place the runtime asks the
    /// operating environment about a physical file and it is asked once per OPEN by <c>FileConnector.Open</c>,
    /// never by an organization body (<c>HostFileProbeDriftTests</c>, kb/Work PB323). Re-opening by path would
    /// also take a SECOND handle on a file the connector may hold under a deny-share mode, and could index a
    /// different file than the one the connector is reading. The walk moves the stream position and leaves it
    /// where it stops; every caller reaches <c>SequentialConnector.SeekToRecord</c> immediately afterwards,
    /// which seeks and discards the reader's buffer.</para></summary>
    public static List<long> FrameStarts(Stream fs)
    {
        var starts = new List<long>();
        fs.Seek(0, SeekOrigin.Begin);
        var len = new byte[4];
        while (true)
        {
            long at = fs.Position;
            if (!FillExactly(fs, len, 4)) break;
            uint word = BinaryPrimitives.ReadUInt32LittleEndian(len);
            if (word != GapTag)
            {
                long skip = word & ~ExtentFlag;
                if ((word & ExtentFlag) != 0)
                {
                    if (!FillExactly(fs, len, 4)) break;   // a torn table ends the store
                    skip += 8L * BinaryPrimitives.ReadUInt32LittleEndian(len);
                }
                if (fs.Position + skip > fs.Length) break;   // a torn tail ends the store
                fs.Seek(skip, SeekOrigin.Current);
            }
            starts.Add(at);
        }
        return starts;
    }

    /// <summary>Does the STREAM-LEVEL record-length framing at <paramref name="path"/> PARSE — the whole of
    /// §14.9.27.4 GR10 for a RECORD VARYING record-sequential file, and the only §9.1.6 attribute such a file's
    /// format encodes.
    /// <para>§9.1.7.2 makes the prefix the file's own statement of its record type: "In record sequential files
    /// the length of each record is determined by any information the implementor may add to the record on the
    /// physical storage medium (such as record length headers)". A first prefix naming more bytes than the file
    /// holds is not a framed file at all, so a connector declaring the VARIABLE record type contradicts the
    /// medium and the OPEN is unsuccessful with '39' (§9.1.13.6 item 7). ⛔ THE BOUND IS THE FILE, NOT THE
    /// RECORD CLAUSE: a prefix that parses but falls outside the connector's VaryMin/VaryMax is exactly
    /// §9.1.13.2 item 3's '04' — "A READ statement is successfully executed but the physical record from the
    /// file is shorter than or longer than the minimum or maximum length of records allowed for the fixed file
    /// attributes for that file" — a SUCCESSFUL completion that a '39' at the OPEN would make unreachable.</para>
    /// <para>Only the FIRST frame is tested, and that is the rule rather than a shortcut: a torn or ragged TAIL
    /// already ends the store for <see cref="FrameStarts"/> and <see cref="ReadStore"/>, so a full walk would
    /// answer a question the framing has already settled, at a cost proportional to the file. It is also what
    /// the surveyed implementation does. A file shorter than one prefix states nothing and is not a conflict.
    /// An I/O failure likewise answers "no conflict" — the authority statuses are the connector's own stream's
    /// to raise.</para></summary>
    public static bool StreamFramingParses(string path)
    {
        try
        {
            using var fs = HostFile.OpenAuxiliary(path, FileMode.Open, FileAccess.Read);
            var len = new byte[4];
            if (!FillExactly(fs, len, 4)) return true;   // fewer than four bytes: nothing is claimed
            uint word = BinaryPrimitives.ReadUInt32LittleEndian(len);
            if (word == GapTag) return false;
            long frame = 4L + (word & ~ExtentFlag);
            if ((word & ExtentFlag) != 0)
            {
                if (!FillExactly(fs, len, 4)) return false;
                frame += 4L + (8L * BinaryPrimitives.ReadUInt32LittleEndian(len));
            }
            return frame <= fs.Length;
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
        catch (ArgumentException) { return true; }
        catch (NotSupportedException) { return true; }
    }

    private static bool FillExactly(Stream s, byte[] buf, int count)
    {
        int got = 0;
        while (got < count)
        {
            int n = s.Read(buf, got, count - got);
            if (n == 0) return false;
            got += n;
        }
        return true;
    }

    // ── Stream-level (char) shape — the sequential connector's varying-record framing ───────────────────────

    /// <summary>Write a record's frame head through a Latin-1 text writer (chars 0–255 map 1:1 to bytes, so the
    /// on-disk bytes equal the store-level frame head): its 4-byte little-endian length word and, when the record
    /// was sent with one, its EXTENT TABLE (see the class summary). The payload follows.</summary>
    public static void WriteFrameHead(TextWriter w, int len, RecordExtents? extents)
    {
        WriteWord(w, FrameWord(len, extents));
        if (extents is not null) w.Write(ExtentTableChars(extents));
    }

    /// <summary>An extent table as the Latin-1 characters of its on-medium bytes (its count and its components,
    /// without the length word) — the shape the sequential connector's in-place REWRITE replaces.</summary>
    public static string ExtentTableChars(RecordExtents extents) => Encoding.Latin1.GetString(EncodeExtentTable(extents));

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

    private static void WriteWord(TextWriter w, uint word)
    {
        w.Write((char)(word & 0xFF));
        w.Write((char)((word >> 8) & 0xFF));
        w.Write((char)((word >> 16) & 0xFF));
        w.Write((char)((word >> 24) & 0xFF));
    }

    /// <summary>Decode a 4-byte little-endian length word read as Latin-1 chars: the payload length, and whether an
    /// extent table follows the word.</summary>
    public static int PrefixLength(ReadOnlySpan<char> pre, out bool extentTableFollows)
    {
        uint word = CharWord(pre);
        extentTableFollows = word != GapTag && (word & ExtentFlag) != 0;
        return (int)(word & ~ExtentFlag);
    }

    /// <summary>The number of components an extent table announces, from its 4-byte count read as Latin-1 chars;
    /// <see cref="ExtentTableFromChars"/> then decodes the 8 characters per component that follow.</summary>
    public static int ExtentCount(ReadOnlySpan<char> count) => (int)Math.Min(CharWord(count), int.MaxValue / 8);

    /// <summary>Decode the per-component part of an extent table read as Latin-1 chars (8 per component); null when a
    /// length is negative (a table this writer cannot have produced).</summary>
    public static RecordExtents? ExtentTableFromChars(ReadOnlySpan<char> components)
    {
        int n = components.Length / 8;
        var fixedAt = new int[n];
        var lengths = new int[n];
        for (int k = 0; k < n; k++)
        {
            fixedAt[k] = (int)CharWord(components.Slice(k * 8, 4));
            lengths[k] = (int)CharWord(components.Slice((k * 8) + 4, 4));
            if (lengths[k] < 0) return null;
        }
        return new RecordExtents(fixedAt, lengths);
    }

    private static uint CharWord(ReadOnlySpan<char> c) => (uint)(c[0] | (c[1] << 8) | (c[2] << 16) | (c[3] << 24));

    // ── The extent table (both shapes) ─────────────────────────────────────────────────────────────────────

    /// <summary>A frame's length word: the payload length, with <see cref="ExtentFlag"/> when a table follows.</summary>
    private static uint FrameWord(int payloadLength, RecordExtents? extents) =>
        (uint)payloadLength | (extents is null ? 0u : ExtentFlag);

    /// <summary>The bytes an extent table occupies on the medium (0 when there is none).</summary>
    private static int ExtentTableBytes(RecordExtents? extents) => extents is null ? 0 : 4 + (8 * extents.Count);

    /// <summary>The ONE encoder of an extent table — its count, then (fixed-run offset, length) per component.</summary>
    private static byte[] EncodeExtentTable(RecordExtents extents)
    {
        var table = new byte[ExtentTableBytes(extents)];
        BinaryPrimitives.WriteUInt32LittleEndian(table, (uint)extents.Count);
        for (int k = 0; k < extents.Count; k++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(4 + (8 * k)), extents.FixedAt[k]);
            BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(8 + (8 * k)), extents.Lengths[k]);
        }
        return table;
    }

    /// <summary>The ONE byte decoder of an extent table at the head of <paramref name="at"/>; null when the bytes
    /// end inside it or describe no valid table (a torn tail, which ends the store like a torn payload).</summary>
    private static RecordExtents? DecodeExtentTable(ReadOnlySpan<byte> at, out int tableBytes)
    {
        tableBytes = 0;
        if (at.Length < 4) return null;
        long n = BinaryPrimitives.ReadUInt32LittleEndian(at);
        if (4 + (8 * n) > at.Length) return null;
        var fixedAt = new int[n];
        var lengths = new int[n];
        for (int k = 0; k < n; k++)
        {
            fixedAt[k] = BinaryPrimitives.ReadInt32LittleEndian(at[(4 + (8 * k))..]);
            lengths[k] = BinaryPrimitives.ReadInt32LittleEndian(at[(8 + (8 * k))..]);
            if (lengths[k] < 0) return null;
        }
        tableBytes = (int)(4 + (8 * n));
        return new RecordExtents(fixedAt, lengths);
    }
}
