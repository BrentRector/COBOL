// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE IMPLEMENTOR-DEFINED LINE SEQUENTIAL CHARACTER SET — ISO/IEC 1989:2023 Annex A.1 item 115
/// ("Line sequential character set", required + documented). The determination itself is published at
/// <c>docs/CONFORMANCE.md</c> <c>DOC-A.1-115</c>; this type is its ONE executable copy.
/// <para><b>THE SET:</b> in an ALPHANUMERIC record area, every character from U+0020 (space) through U+00FF; in a
/// NATIONAL one, every character from U+0020 up. The characters BELOW U+0020 — the C0 controls U+0000–U+001F,
/// which include the two line delimiters CR U+000D and LF U+000A and the tab U+0009 — are outside it. DEL
/// (U+007F) through U+00FF are inside, so an ordinary 8-bit text file reads without '09'. An alphanumeric
/// character above U+00FF is outside because it has no byte image in the file's coded character set (owner
/// decision kb/Work R47, <see cref="FileCharacterSet"/>; kb/Work PB690) — so a WRITE or REWRITE of one is the
/// standard's '71' on this organization, where every other organization answers '91'. ⛔ The ceiling IS the
/// file's coded character set, so a file with a CODE-SET clause takes the CODE-SET alphabet's set instead
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
    /// record area of the given class — alphanumeric (<paramref name="national"/> false) is bounded by the file's
    /// coded character set: the characters with a byte image (<see cref="StorageByte.HasByte"/>) with no CODE-SET, the CODE-SET alphabet's own
    /// membership (<paramref name="codeSet"/>, <see cref="CodeSetConversion.Represents"/>) with one; national has
    /// no ceiling (see the type remarks).</summary>
    public static bool Contains(int codePoint, bool national, CodeSetConversion? codeSet) =>
        codePoint >= Lowest && (national || (codeSet is null
            ? codePoint <= char.MaxValue && StorageByte.HasByte((char)codePoint)
            : codePoint <= char.MaxValue && codeSet.Represents((char)codePoint)));

    /// <summary>True when the record area holds at least one character OUTSIDE the set — the single predicate
    /// behind '09' (READ), '71' (WRITE) and '71' (REWRITE).
    /// <para>⛔ CHARACTERS, NOT BYTES. The connector's record channel carries one char per BYTE (Latin1), and
    /// §14.9.30.4 GR15 is explicit that a record area is "specified implicitly or explicitly" as alphanumeric
    /// OR as national. A national character occupies two bytes, UTF-16BE (§13.18.60.4 GR8 / determination D-N1),
    /// so <c>N"CD"</c> occupies the bytes <c>00 43 00 44</c> — a byte-level test would read the 0x00 halves as
    /// control characters and refuse EVERY national line sequential record (it would have broken the standing
    /// golden <c>2002/pb327_national_line_sequential_fill</c>). <paramref name="national"/> is the connector's
    /// <c>NationalRecordArea</c>, the same flag <c>FitRecord</c>/<c>TrimRecordEnd</c> read, so the three
    /// record-area rules agree on what a character is. A trailing ODD byte is half a national position, whose
    /// content §14.9.30.4 GR14/GR15 leave undefined; it forms no character and is not tested.</para>
    /// <para><paramref name="codeSet"/> is the connector's CODE-SET conversion, or null — REQUIRED so no caller
    /// can ask the question without saying which coded character set the file is in (kb/Work PB1542).</para></summary>
    public static bool HasCharacterOutside(ReadOnlySpan<char> recordArea, bool national, CodeSetConversion? codeSet)
    {
        if (!national)
        {
            foreach (char c in recordArea)
                if (!Contains(c, national: false, codeSet)) return true;
            return false;
        }
        // Every unit of a national area on this channel is one BYTE of a UTF-16BE pair; a unit above U+00FF is
        // not a byte at all, so it cannot form a national character (FileCharacterSet's one-char-per-byte rule).
        if (FileCharacterSet.HasCharacterWithoutByteImage(recordArea)) return true;
        for (int i = 0; i + 1 < recordArea.Length; i += 2)
            if (!Contains((StorageByte.ToByte(recordArea[i]) << 8) | StorageByte.ToByte(recordArea[i + 1]), national: true, codeSet)) return true;
        return false;
    }
}
