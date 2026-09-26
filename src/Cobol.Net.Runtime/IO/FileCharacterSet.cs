// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE FILE CODED CHARACTER SET — the ONE executable copy of the determination ISO/IEC 1989:2023
/// §8.1.2 requires (<i>"The implementor shall specify the set of characters in and the encoding of each of the
/// computer's alphanumeric character set and the computer's national character set"</i>; Annex A.1 item 31,
/// docs/CONFORMANCE.md <c>DOC-A.1-31</c>) as it applies to a FILE with no CODE-SET conversion: owner decision
/// kb/Work R47 — <b>ISO/IEC 8859-1, one byte per character position</b>, a character U+0000–U+00FF being the byte
/// of the same value (the USAGE DISPLAY encoding of Annex A.1 item 209).
/// <para><b>WHY A TYPE:</b> the in-memory alphanumeric repertoire is UTF-16 (a character position may hold any
/// BMP code unit), so the medium's one-byte set is a strict SUBSET of what a record area can hold, and the
/// difference must be answered — never absorbed. Before kb/Work PB690 every connector encoded with
/// <see cref="Encoding.Latin1"/>, whose replacement fallback writes <c>?</c> (0x3F) for a character above
/// U+00FF: a record holding U+20AC went to the medium as <c>?</c> with status '00' and read back as <c>?</c>.
/// R47's answer is to REFUSE such a record with an I-O status — '71' for a line sequential file (the character
/// is outside that organization's character set, Annex A.1 item 115, <see cref="LineSequentialCharacterSet"/>)
/// and '91' for every other organization and for a report file (<see cref="FileStatusCode.CharacterWithoutByteImage"/>).</para>
/// <para><b>TWO GUARANTEES, ONE SET:</b> <see cref="HasCharacterWithoutByteImage"/> is the test every WRITE and
/// REWRITE asks BEFORE anything reaches the medium (so the statement is unsuccessful and the medium unchanged),
/// and <see cref="Medium"/> is the encoding every record-data write uses, with an EXCEPTION fallback: a path
/// that ever skipped the test fails loudly instead of reviving the silent <c>?</c>. The test is the answer; the
/// encoding is the guard that keeps it the only answer.</para>
/// <para>Reading needs no rule: every byte decodes to U+0000–U+00FF. A file with a CODE-SET conversion is out of
/// scope here — its conversion (§13.18.13, <see cref="CodeSetConversion"/>) decides what has an image.</para></summary>
public static class FileCharacterSet
{
    /// <summary>The highest character with a byte image — U+00FF, the last ISO/IEC 8859-1 code point.</summary>
    public const char Highest = 'ÿ';

    /// <summary>True when <paramref name="record"/> holds at least one character with no byte image in the file
    /// coded character set — a code unit above <see cref="Highest"/>. A national record area's characters reach
    /// the connector already as their UTF-16BE byte pairs (one char per byte, §13.18.60.4 GR8 / D-N1), so the one
    /// test serves both record-area classes.</summary>
    public static bool HasCharacterWithoutByteImage(ReadOnlySpan<char> record) =>
        record.ContainsAnyExceptInRange('\0', Highest);

    /// <summary>The file coded character set as an <see cref="Encoding"/>: ISO/IEC 8859-1 with EXCEPTION
    /// fallbacks, so an unrepresentable character can never be silently replaced (see the type remarks).</summary>
    public static Encoding Medium { get; } = Encoding.GetEncoding(
        "iso-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
}
