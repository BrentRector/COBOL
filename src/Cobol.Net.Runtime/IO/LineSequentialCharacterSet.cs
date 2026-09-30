// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE IMPLEMENTOR-DEFINED LINE SEQUENTIAL CHARACTER SET — ISO/IEC 1989:2023 Annex A.1 item 115
/// ("Line sequential character set", required + documented). The determination itself is published at
/// <c>docs/CONFORMANCE.md</c> <c>DOC-A.1-115</c>; this type is its ONE executable copy.
/// <para><b>THE SET:</b> with no CODE-SET clause the file is UTF-8 text (<see cref="LineSequentialEncoding"/>; owner
/// decision kb/Work R51, superseding R47's U+00FF ceiling for this organization; kb/Work PB1760), and the set is
/// every Unicode SCALAR VALUE from U+0020 (space) up, in an alphanumeric and a national record area alike — a
/// surrogate pair is one character, HIGH-VALUE (U+FFFF) is a member. The characters BELOW U+0020 — the C0 controls
/// U+0000–U+001F, which include the two line delimiters CR U+000D and LF U+000A and the tab U+0009 — are outside
/// it, and so is an UNPAIRED surrogate (it is not a character and has no UTF-8 form; a byte that was not UTF-8 reads
/// as one, U+DC80–U+DCFF, which is how such a line reports '09'). ⛔ The encoding IS part of the set, so a file
/// with a CODE-SET clause takes the CODE-SET alphabet's set instead
/// (§13.18.13.4 GR6; kb/Work PB1542): under STANDARD-1, STANDARD-2 or ASCII an alphanumeric character above
/// U+007F is outside, under EBCDIC every U+0020–U+00FF character is inside — the same membership
/// <see cref="CodeSetConversion.Represents"/> answers for every other organization's '91'. A national record
/// area's characters are written as UTF-16BE (§13.18.60.4 GR8 / D-N1) and keep no ceiling.</para>
/// <para><b>WHY ONE TYPE:</b> the standard names this one set from three places and they must never disagree —
/// §14.9.30.4 GR16 (a successful READ whose record area holds one ⇒ I-O status '09', §9.1.13.2 item 7),
/// §14.9.51.4 GR23 (WRITE ⇒ unsuccessful, '71') and §14.9.35.4 GR17 d) (REWRITE ⇒ unsuccessful, '71';
/// §9.1.13.10 item 1 covers both write directions and adds that the record area remains unchanged). Before
/// kb/Work PB329 only the REWRITE arm existed and it carried its own ad-hoc CR/LF test, so the WRITE arm was
/// missing outright and the READ arm could not produce '09' at all.</para>
/// <para><b>WHY THIS BOUNDARY</b> (the derivation the A.1 row publishes):
/// (1) the delimiters are FORCED out — §9.1.7.2 makes a line sequential record's extent "the number of
/// characters between the preceding line delimiter and the following line delimiter", so an area holding CR or
/// LF cannot round-trip as one record;
/// (2) the set cannot be ONLY "everything but the delimiters" — the reader consumes CR/LF as the framing, so a
/// record area could never hold one and GR16's '09' would be unreachable by construction; a REQUIRED A.1
/// determination that makes its own rule vacuous is not a determination;
/// (3) the remaining C0 controls are the stream/device repertoire, not text — NUL terminates a host string,
/// SUB (0x1A) marks end-of-file on DOS-descended hosts, VT/FF are page controls, ESC introduces an escape
/// sequence — and a line sequential file exists to BE plain text and interchange with non-COBOL tools (the same
/// property that keeps a sequential file's §9.1.6 fixed file attributes out of the §14.9.27.4 GR10 validated set
/// altogether — a line sequential file records none, because recording one would stop it being plain text);
/// (4) ⚖ surveyed, not assumed (the owner's standing latitude rule): GnuCOBOL's <c>COB_LS_VALIDATE</c> defaults
/// to true "per COBOL 2022" and validates "that the data should be validated as it is read (status 09) /
/// written (status 71)", treating data below SPACE as invalid — the same boundary, and the same two statuses.
/// </para></summary>
public static class LineSequentialCharacterSet
{
    /// <summary>The lowest code point in the set — U+0020, the space character.</summary>
    public const int Lowest = ' ';

    /// <summary>True when <paramref name="codePoint"/> is a member of the line sequential character set of a
    /// record area of the given class. With no CODE-SET the file is UTF-8 text (<see cref="LineSequentialEncoding"/>;
    /// kb/Work PB1760), so the members are the Unicode SCALAR VALUES from U+0020 up — every character UTF-8 can carry
    /// — and a surrogate CODE POINT (a lone surrogate, including the U+DC80–U+DCFF escape of a byte that was not
    /// UTF-8) is not a character at all. With a CODE-SET clause an alphanumeric area is bounded by the CODE-SET
    /// alphabet's own membership (<paramref name="codeSet"/>, <see cref="CodeSetConversion.Represents"/>) and a
    /// national one has no ceiling (see the type remarks).</summary>
    public static bool Contains(int codePoint, bool national, CodeSetConversion? codeSet) =>
        codePoint >= Lowest && (codeSet is null
            ? codePoint <= 0x10FFFF && codePoint is < 0xD800 or > 0xDFFF
            : national || (codePoint <= char.MaxValue && codeSet.Represents((char)codePoint)));

    /// <summary>True when the record area holds at least one character OUTSIDE the set — the single predicate
    /// behind '09' (READ), '71' (WRITE) and '71' (REWRITE).
    /// <para>⛔ CHARACTERS, NOT BYTES. The connector's record channel carries one char per BYTE, and
    /// §14.9.30.4 GR15 is explicit that a record area is "specified implicitly or explicitly" as alphanumeric
    /// OR as national. A national character occupies two bytes, UTF-16BE (§13.18.60.4 GR8 / determination D-N1),
    /// so <c>N"CD"</c> occupies the bytes <c>00 43 00 44</c> — a byte-level test would read the 0x00 halves as
    /// control characters and refuse EVERY national line sequential record (it would have broken the standing
    /// golden <c>2002/pb327_national_line_sequential_fill</c>). <paramref name="national"/> is the connector's
    /// <c>NationalRecordArea</c>, the same flag <c>FitRecord</c>/<c>TrimRecordEnd</c> read, so the three
    /// record-area rules agree on what a character is. A trailing ODD byte is half a national position, whose
    /// content §14.9.30.4 GR14/GR15 leave undefined; it forms no character and is not tested.</para>
    /// <para>⛔ A SURROGATE PAIR IS ONE CHARACTER. With no CODE-SET the area is walked as UTF-16, so a supplementary
    /// character (two positions, §8.5.1.4) is tested as its scalar value and only an UNPAIRED surrogate is outside.</para>
    /// <para><paramref name="codeSet"/> is the connector's CODE-SET conversion, or null — REQUIRED so no caller
    /// can ask the question without saying which coded character set the file is in (kb/Work PB1542).</para></summary>
    public static bool HasCharacterOutside(ReadOnlySpan<char> recordArea, bool national, CodeSetConversion? codeSet)
    {
        if (national)
        {
            // Every unit of a national area on this channel is one BYTE of a UTF-16BE pair; a unit that is not a
            // byte image cannot form a national character (the storage-byte law, StorageByte).
            if (FileCharacterSet.HasCharacterWithoutByteImage(recordArea)) return true;
            var chars = new char[recordArea.Length / CobolBits.BytesPerNational];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)((StorageByte.ToByte(recordArea[2 * i]) << 8) | StorageByte.ToByte(recordArea[2 * i + 1]));
            return HasCharacterOutsideOfClass(chars, national: true, codeSet);
        }
        return HasCharacterOutsideOfClass(recordArea, national: false, codeSet);
    }

    private static bool HasCharacterOutsideOfClass(ReadOnlySpan<char> text, bool national, CodeSetConversion? codeSet)
    {
        for (int i = 0; i < text.Length; i++)
        {
            int cp = text[i];
            if (codeSet is null && char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                cp = char.ConvertToUtf32(text[i], text[++i]);
            if (!Contains(cp, national, codeSet)) return true;
        }
        return false;
    }
}
