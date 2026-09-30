// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE LINE SEQUENTIAL FILE ENCODING — UTF-8 (owner decision kb/Work R51 item 3; design
/// <c>COBOLNET_FILES_DESIGN.md</c> D29; kb/Work PB1760): a LINE SEQUENTIAL file with no CODE-SET clause is TEXT,
/// and its characters travel as UTF-8, so every character a record area can hold — Latin-1, the rest of the BMP, a
/// surrogate pair — round-trips exactly. §12.4.5.10.3 GR2 leaves "the range of allowable characters in a line
/// sequential file" to the implementor, and §8.1.2 the encoding (DOC-A.1-31 / DOC-A.1-115). A fixed-record file keeps
/// one byte per position (<see cref="FileCharacterSet"/>), because a variable-width encoding would break its record
/// arithmetic; a line has no fixed width.
/// <para><b>The channel.</b> Like every other medium encoding here, this one maps between the NATIVE record image and
/// the byte CHANNEL the connector streams — one char per medium byte (U+0000–U+00FF) — so the line framing, the
/// delimiter scan and the physical byte anchors a REWRITE and a START use stay byte-exact and encoding-blind: the
/// delimiter LF (0x0A) and CR (0x0D) never occur inside a UTF-8 multi-byte sequence.</para>
/// <para><b>A national record area</b> reaches the channel as its UTF-16BE pair image (one char per byte, D-N1); its
/// CHARACTERS are what the text line holds, so it is decoded to characters before encoding and re-paired after
/// decoding. A national space is U+0020, one UTF-8 byte.</para>
/// <para><b>Bytes that are not UTF-8</b> (a legacy 8-bit file, a torn sequence) decode, byte for byte, to the lone
/// surrogates U+DC80–U+DCFF (the "surrogate escape" of Python's PEP 383). A lone surrogate is not a character, so it
/// is outside the line sequential character set (<see cref="LineSequentialCharacterSet"/>): the READ is successful
/// with I-O status '09' (§14.9.30.4 GR16 — the ONE predicate, no second test here), the record area keeps every
/// byte's identity, and a WRITE or REWRITE of such a record area is refused '71' (§14.9.51.4 GR23). ⚖ The status is
/// the standard's; what the record area holds for a non-character is the implementor's, and preserving the byte is
/// the GnuCOBOL behavior (a line sequential READ there transfers bytes unchanged).</para>
/// <para><b>Byte-order mark.</b> A UTF-8 BOM (EF BB BF) at the start of the file is accepted and is not record
/// data; a WRITE never writes one (<see cref="Bom"/>, <c>SequentialConnector.NextFrame</c>).</para></summary>
public static class LineSequentialEncoding
{
    /// <summary>The UTF-8 byte-order mark as channel characters — skipped at the start of a file, never written.</summary>
    public const string Bom = "ï»¿";

    /// <summary>The first lone surrogate of the byte escape: a non-UTF-8 byte b decodes to <c>EscapeBase + b</c>.</summary>
    private const int EscapeBase = 0xDC00;

    /// <summary>The native record image about to be written, as channel characters (one per UTF-8 byte). The
    /// statement's '71' screen has already refused a record area holding a non-member — every lone surrogate among
    /// them — so a lone surrogate here is a write path that skipped the screen, and it fails loudly.</summary>
    public static string ToChannel(string native, bool nationalArea)
    {
        string text = nationalArea ? CobolBits.NatReadWindow(native, 0, native.Length / CobolBits.BytesPerNational) : native;
        var sb = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            int cp = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                cp = char.ConvertToUtf32(text[i], text[++i]);
            else if (char.IsSurrogate(text[i]))
                throw new InvalidOperationException($"the record contains the lone surrogate U+{cp:X4}, which is outside "
                    + "the line sequential character set, and no WRITE or REWRITE screen refused it (ISO §14.9.51.4 GR23)");
            Put(sb, cp);
        }
        return sb.ToString();
    }

    /// <summary>The native record image of a line just read (channel characters, one per byte, the delimiter and any
    /// byte-order mark already removed).</summary>
    public static string FromChannel(string channel, bool nationalArea)
    {
        var sb = new System.Text.StringBuilder(channel.Length);
        for (int i = 0; i < channel.Length;)
        {
            int n = Sequence(channel, i, out int cp);
            if (n == 0) { sb.Append((char)(EscapeBase + (channel[i] & 0xFF))); i++; continue; }
            if (cp > char.MaxValue) sb.Append(char.ConvertFromUtf32(cp));
            else sb.Append((char)cp);
            i += n;
        }
        string text = sb.ToString();
        return nationalArea ? CobolBits.NatBytes(text) : text;
    }

    /// <summary>The well-formed UTF-8 sequence starting at <paramref name="at"/> (Unicode 15 Table 3-7): its length
    /// and code point, or 0 when the byte there does not start one.</summary>
    private static int Sequence(string b, int at, out int cp)
    {
        cp = 0;
        int b0 = b[at];
        if (b0 < 0x80) { cp = b0; return 1; }
        int n; int lo = 0x80, hi = 0xBF;
        if (b0 is >= 0xC2 and <= 0xDF) { n = 2; cp = b0 & 0x1F; }
        else if (b0 is >= 0xE0 and <= 0xEF)
        {
            n = 3; cp = b0 & 0x0F;
            if (b0 == 0xE0) lo = 0xA0;           // no overlong form
            else if (b0 == 0xED) hi = 0x9F;      // no surrogate code point
        }
        else if (b0 is >= 0xF0 and <= 0xF4)
        {
            n = 4; cp = b0 & 0x07;
            if (b0 == 0xF0) lo = 0x90;           // no overlong form
            else if (b0 == 0xF4) hi = 0x8F;      // nothing above U+10FFFF
        }
        else return 0;
        if (at + n > b.Length) return 0;
        for (int k = 1; k < n; k++)
        {
            int c = b[at + k];
            if (c < (k == 1 ? lo : 0x80) || c > (k == 1 ? hi : 0xBF)) return 0;
            cp = (cp << 6) | (c & 0x3F);
        }
        return n;
    }

    private static void Put(System.Text.StringBuilder sb, int cp)
    {
        if (cp < 0x80) { sb.Append((char)cp); return; }
        if (cp < 0x800) { sb.Append((char)(0xC0 | (cp >> 6))).Append((char)(0x80 | (cp & 0x3F))); return; }
        if (cp < 0x10000)
        {
            sb.Append((char)(0xE0 | (cp >> 12))).Append((char)(0x80 | ((cp >> 6) & 0x3F))).Append((char)(0x80 | (cp & 0x3F)));
            return;
        }
        sb.Append((char)(0xF0 | (cp >> 18))).Append((char)(0x80 | ((cp >> 12) & 0x3F)))
          .Append((char)(0x80 | ((cp >> 6) & 0x3F))).Append((char)(0x80 | (cp & 0x3F)));
    }
}
