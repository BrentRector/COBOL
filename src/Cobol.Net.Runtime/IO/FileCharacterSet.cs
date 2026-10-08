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
/// <para><b>HIGH-VALUE is the byte 0xFF</b> (owner decisions kb/Work R51 / R52; design D29; kb/Work PB1759). The
/// native HIGH-VALUE character is U+FFFF, and it crosses the medium by the ONE storage-byte law
/// (<see cref="StorageByte"/>): U+FFFF is written as 0xFF and 0xFF is read as U+FFFF. That is the channel mapping
/// <see cref="ToChannel"/> / <see cref="FromChannel"/> perform on the NATIVE side of a CODE-SET conversion, so a
/// record struct, a REDEFINES backing and an elementary record cross it identically and a byte-form leaf's 0xFF
/// (imaged U+FFFF in memory by the same law) survives the round trip. The accepted cost: a real <c>ÿ</c>
/// (U+00FF) also occupies 0xFF, so it reads back as HIGH-VALUE (DOC-A.1-31).</para></summary>
public static class FileCharacterSet
{
    /// <summary>True when <paramref name="record"/> holds at least one character with no byte image in the file
    /// coded character set — a code unit above U+00FF other than the HIGH-VALUE character
    /// (<see cref="StorageByte.HasByte"/>). A national record area's characters reach the connector already as
    /// their UTF-16BE byte pairs (one char per byte by the storage-byte law, §13.18.60.4 GR8 / D-N1), so the one
    /// test serves both record-area classes.</summary>
    public static bool HasCharacterWithoutByteImage(ReadOnlySpan<char> record) => StorageByte.AnyWithoutByte(record);

    /// <summary>⛔ THE NATIVE → CHANNEL mapping of a record image about to be written — every physical write of
    /// record data passes through here (<c>FileConnector.ToMedium</c>, <see cref="RecordFraming.ComposeStore"/>).
    /// The HIGH-VALUE character takes its byte 0xFF (<see cref="StorageByte"/>), and a CODE-SET conversion
    /// (§13.18.13.4 GR6 b) then replaces each native channel character with its coded character; with none
    /// (GR7) the channel IS the medium. The output statement has already refused a record holding a character
    /// with no byte image (<see cref="HasCharacterWithoutByteImage"/>, <see cref="CodeSetConversion.HasCharacterWithoutImage"/>).</summary>
    public static string ToChannel(string native, CodeSetConversion? codeSet) =>
        codeSet is not null ? codeSet.ToMedium(native)
        : native.Replace(NativeCollatingSequence.HighValue, (char)StorageByte.ToByte(NativeCollatingSequence.HighValue));

    /// <summary>⛔ THE CHANNEL → NATIVE mapping of a record image just read — the inverse of <see cref="ToChannel"/>:
    /// a CODE-SET conversion first (§13.18.13.4 GR6 a), then the storage-byte law, which reads 0xFF as the HIGH-VALUE
    /// character.</summary>
    public static string FromChannel(string channel, CodeSetConversion? codeSet) =>
        codeSet is not null ? codeSet.ToNative(channel)
        : channel.Replace((char)StorageByte.ToByte(NativeCollatingSequence.HighValue), NativeCollatingSequence.HighValue);

    /// <summary>The file coded character set as an <see cref="Encoding"/>: ISO/IEC 8859-1 with EXCEPTION
    /// fallbacks, so an unrepresentable character can never be silently replaced (see the type remarks).</summary>
    public static Encoding Medium { get; } = Encoding.GetEncoding(
        "iso-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
}

/// <summary>⛔ THE ENCODING OF A FILE'S RECORD DATA ON ITS MEDIUM — the three cases a connector's
/// <c>FileConnector.MediumEncoding</c> answers (owner decision kb/Work R51 item 3; design D29). Each maps the native record
/// image to the byte CHANNEL (one char per medium byte) and back, so the framing above it never depends on it.</summary>
public enum MediumEncoding
{
    /// <summary>One byte per character position, ISO/IEC 8859-1 under the storage-byte law (<see cref="FileCharacterSet"/>):
    /// the fixed-record organizations with no CODE-SET clause (DOC-A.1-31; owner decision kb/Work R47).</summary>
    SingleByte,

    /// <summary>The CODE-SET alphabet's coded character set (§13.18.13.4 GR6, <see cref="CodeSetConversion"/>).</summary>
    CodeSet,

    /// <summary>UTF-8 text (<see cref="LineSequentialEncoding"/>): a LINE SEQUENTIAL file with no CODE-SET clause
    /// (DOC-A.1-115; kb/Work PB1760).</summary>
    Utf8,
}
