// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE PHYSICAL LAYOUT OF ONE DYNAMIC-LENGTH ITEM IN A RECORD IMAGE (ISO §12.3.7.4 GR18 + GR19; docs/CONFORMANCE.md
/// §3 determination D-DL3; kb/Work PB1094). A dynamic-length item named by a DYNAMIC LENGTH STRUCTURE clause occupies,
/// in the record a file carries, its data PREFIXED by a length field (PREFIXED / SHORT PREFIXED, GR18) and FOLLOWED
/// by a delimiter (DELIMITED, GR19), in that order whichever order the clause wrote them:
/// <code>[ length field ][ data ][ delimiter ]</code>
/// <para>The program never sees these characters. §8.5.1.11.2 makes a variable-length item behave "as though it were
/// in fact contiguous with its neighbors whenever a procedural operation is applied to a group containing it", so a
/// MOVE, a comparison or a DISPLAY of the record is over the data alone; the layout is the FILE'S form of the record
/// (§8.5.1.10.3 lets the item sit within the record it is subordinate to, which is what a record on a medium is),
/// and it is applied exactly once on the way out (<see cref="Frame"/>, through
/// <see cref="CobolContiguousLayout.MediumImage"/>) and once on the way in (<see cref="ContentOf"/> /
/// <see cref="TakeAt"/>, through <see cref="CobolContiguousLayout.Decompose"/>).</para>
/// <para><b>The length field</b> is the item's current length in CHARACTER POSITIONS as an unsigned (or signed —
/// the value is never negative, so the bytes are the same) binary integer in this implementation's binary byte order,
/// MOST SIGNIFICANT BYTE FIRST (docs/CONFORMANCE.md DOC-A.1-205), <see cref="PrefixBytes"/> characters wide — four for
/// PREFIXED, two for SHORT PREFIXED, the widths GR18's table names — one image character per byte (the storage-byte
/// law). <b>The delimiter</b> is one character position of binary zeroes (GR19): <see cref="Unit"/> image characters of
/// U+0000.</para>
/// <para><b>Unit</b> is how many image characters one character position of the item's data occupies: one for an
/// alphanumeric item and for a national member of a group (a dynamic-length item contributes its content character
/// for character, DOC-A.1-63), two for a national item that IS the record (its image is its UTF-16BE byte pairs,
/// D-N1). The length field counts positions, the delimiter is one position wide, so both follow the data's own unit.</para>
/// </summary>
public sealed class CobolDynStructure
{
    /// <summary>The structure of an item whose clause wrote <paramref name="prefixBytes"/> (0, 2 or 4) and
    /// <paramref name="delimited"/>, over data of <paramref name="unit"/> image characters per position.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A length-field width GR18 does not name, a non-positive unit, or a
    /// structure that is neither prefixed nor delimited (§12.3.7.4 requires one or more of the two).</exception>
    public CobolDynStructure(int prefixBytes, bool delimited, int unit = 1)
    {
        if (prefixBytes is not (0 or 2 or 4)) throw new ArgumentOutOfRangeException(nameof(prefixBytes));
        if (unit < 1) throw new ArgumentOutOfRangeException(nameof(unit));
        if (prefixBytes == 0 && !delimited) throw new ArgumentOutOfRangeException(nameof(delimited), "a structure is PREFIXED, DELIMITED or both");
        PrefixBytes = prefixBytes;
        Delimited = delimited;
        Unit = unit;
    }

    /// <summary>The width of the length field in characters — 0 when the structure has no PREFIXED phrase.</summary>
    public int PrefixBytes { get; }

    /// <summary>The DELIMITED phrase is specified: a one-position binary-zero delimiter follows the data.</summary>
    public bool Delimited { get; }

    /// <summary>Image characters per character position of the data (see the class summary).</summary>
    public int Unit { get; }

    /// <summary>The characters this layout adds around the data — the length field and the delimiter.</summary>
    public int Overhead => PrefixBytes + (Delimited ? Unit : 0);

    // ── The compact constant form the compiler passes (a layout is generated data, never allocated per call) ──

    /// <summary>The structure as ONE int constant — bits 0-2 the length-field width, bit 3 DELIMITED, bits 4-7 the
    /// unit — so a generated layout, and the cell-backed arm that cannot keep a static field, spell it as constant
    /// data. 0 is "no structure" (a structure always has a length field or a delimiter, so no structure encodes to 0).</summary>
    public int Code => PrefixBytes | (Delimited ? 8 : 0) | (Unit << 4);

    // A structure is immutable and there are a handful of them (width x delimiter x unit), so each is made once.
    private static readonly CobolDynStructure?[] Interned = new CobolDynStructure?[64];

    /// <summary>The structure <paramref name="code"/> encodes (<see cref="Code"/>), or null for 0. Interned: a WRITE
    /// or READ of a structured elementary record asks for it every time.</summary>
    public static CobolDynStructure? FromCode(int code)
    {
        if (code == 0) return null;
        if ((uint)code >= (uint)Interned.Length) return new CobolDynStructure(code & 7, (code & 8) != 0, code >> 4);
        return Interned[code] ??= new CobolDynStructure(code & 7, (code & 8) != 0, code >> 4);   // a lost race makes an equal twin
    }

    /// <summary>The structure of each component of a layout from its <paramref name="codes"/>, or null when no
    /// component has one (so an unstructured layout carries nothing).</summary>
    internal static CobolDynStructure?[]? FromCodes(IReadOnlyList<int>? codes)
    {
        if (codes is null || codes.All(c => c == 0)) return null;
        var all = new CobolDynStructure?[codes.Count];
        for (int k = 0; k < all.Length; k++) all[k] = FromCode(codes[k]);
        return all;
    }

    /// <summary><see cref="Frame"/> through the structure <paramref name="code"/> encodes — what the WRITE of a
    /// dynamic-length ELEMENTARY RECORD that names a structure sends (its image is the one component).</summary>
    public static string FrameWith(int code, string content) =>
        (FromCode(code) ?? throw new ArgumentOutOfRangeException(nameof(code), "no structure")).Frame(content);

    /// <summary>The data of a dynamic-length ELEMENTARY RECORD that names a structure, from the record the file
    /// holds (<see cref="TakeAt"/> at its start — the record has no extent table, so the structure itself says where
    /// the data ends).</summary>
    public static string UnframeWith(int code, string record)
    {
        (FromCode(code) ?? throw new ArgumentOutOfRangeException(nameof(code), "no structure")).TakeAt(record, 0, out string content);
        return content;
    }

    // ── Out: the data, framed (§12.3.7.4 GR18, GR19) ────────────────────────────────────────────────────────────

    /// <summary>The item's image in a record: its length field (GR18), the <paramref name="content"/> and its
    /// delimiter (GR19). <paramref name="content"/> is the item's data in the image's characters, so its length is a
    /// whole number of <see cref="Unit"/>s.</summary>
    public string Frame(string content)
    {
        var sb = new System.Text.StringBuilder(content.Length + Overhead);
        if (PrefixBytes > 0)
        {
            long positions = content.Length / Unit;
            for (int shift = (PrefixBytes - 1) * 8; shift >= 0; shift -= 8) sb.Append((char)((positions >> shift) & 0xFF));
        }
        sb.Append(content);
        if (Delimited) sb.Append('\0', Unit);
        return sb.ToString();
    }

    // ── In: the item's extent is known (the frame's extent table), or it is not (a file another program wrote) ──

    /// <summary>The data of an item whose whole extent <paramref name="extent"/> in the record is known — the
    /// characters the extent table says it occupies. The length field names the data when there is one (clamped to what
    /// the extent holds, so a damaged field cannot read past it). A DELIMITED-only item whose extent ENDS in its
    /// delimiter is exactly the data before it: the extent table is the length, so data that itself holds a binary-zero
    /// character survives the round trip (§12.3.7.4 GR19 puts a delimiter after the data and says nothing of data that
    /// holds one; a reader with no table cannot tell, see <see cref="TakeAt"/>). Otherwise — the fixed form's padded
    /// extent — the first delimiter ends it, and an extent with none is all data.</summary>
    public string ContentOf(string extent)
    {
        int at = PrefixBytes;
        if (extent.Length < at) return "";
        if (PrefixBytes > 0)
        {
            long n = LengthField(extent, 0) * Unit;
            return extent.Substring(at, (int)Math.Min(n, extent.Length - at));
        }
        if (IsDelimiterAt(extent, extent.Length - Unit)) return extent[..^Unit];
        int end = DelimiterAt(extent, at);
        return end < 0 ? extent[at..] : extent.Substring(at, end - at);
    }

    /// <summary>Take one item from <paramref name="record"/> at <paramref name="at"/> with no extent table to say where
    /// it ends — the structure itself must: the length field gives the data's length, otherwise the delimiter ends it.
    /// A DELIMITED item also consumes its delimiter when it is there. Returns the characters consumed (length field +
    /// data + delimiter) and the data in <paramref name="content"/>; a record that ends early gives what is left.</summary>
    public int TakeAt(string record, int at, out string content)
    {
        int remaining = Math.Max(0, record.Length - at);
        if (PrefixBytes > 0)
        {
            if (remaining < PrefixBytes) { content = ""; return remaining; }
            long n = LengthField(record, at) * Unit;
            int dataAt = at + PrefixBytes;
            int len = (int)Math.Min(n, record.Length - dataAt);
            content = record.Substring(dataAt, len);
            int used = PrefixBytes + len;
            if (Delimited && IsDelimiterAt(record, at + used)) used += Unit;
            return used;
        }
        int start = at, end = DelimiterAt(record, start);
        if (end < 0) { content = record.Substring(Math.Min(start, record.Length)); return remaining; }
        content = record.Substring(start, end - start);
        return end - start + Unit;
    }

    // The unsigned big-endian value of the length field at `at` (the caller has checked it fits).
    private long LengthField(string s, int at)
    {
        long n = 0;
        for (int i = 0; i < PrefixBytes; i++) n = (n << 8) | (s[at + i] & 0xFFu);
        return n;
    }

    // The start of the first delimiter at or after `from`, aligned to a position of the data's unit — a binary-zero
    // POSITION, so a national pair whose first byte is zero is not one; -1 when there is none.
    private int DelimiterAt(string s, int from)
    {
        for (int i = from; i + Unit <= s.Length; i += Unit)
            if (IsDelimiterAt(s, i)) return i;
        return -1;
    }

    private bool IsDelimiterAt(string s, int at)
    {
        if (at < 0 || at + Unit > s.Length) return false;
        for (int i = 0; i < Unit; i++)
            if (s[at + i] != '\0') return false;
        return true;
    }
}
