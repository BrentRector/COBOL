// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE ONE ISO §13.18.13.4 GR6 CODE-SET CONVERSION — "<i>On input, each coded character from the storage
/// medium is replaced with its associated native coded character as defined in the alphabet being used</i>"
/// (GR6 a) and "<i>On output, each native coded character in the record is replaced for the storage medium with
/// its associated coded character as defined in the alphabet being used</i>" (GR6 b). One instance per file
/// connector, or NULL — §13.18.13.4 GR7, "<i>If the CODE-SET clause is not specified, the native character set
/// is assumed for data on the external media</i>", which is the identity and costs nothing.
/// <para><b>The correspondence is the compiler's</b>, not the runtime's: the ALPHABET clause's coded character
/// set decides it (§12.3.7.4 GR7 c for STANDARD-1 / STANDARD-2, GR7 i for a code-name —
/// <c>CobolNet.Binding.CodedCharacterSet.MediumCorrespondence</c>) and the emitter hands the finished table down as
/// a literal, exactly as it hands down a collating sequence's weights. The table has one entry per character of
/// the coded character set: 256 for a complete single-byte code (EBCDIC), 128 for ISO/IEC 646 (STANDARD-1,
/// STANDARD-2, ASCII). This type holds no code page and knows no alphabet name.</para>
/// <para><b>Two questions, answered separately (kb/Work PB1150, PB1542).</b> (1) <see cref="Represents"/> —
/// MEMBERSHIP: does a native character have an "<i>associated coded character</i>" in this set at all? GR6 b can
/// replace only a character that has one, so a record holding a character that does not is refused by the output
/// STATEMENT, before anything reaches the medium — the ONE screen every organization's WRITE and REWRITE asks
/// (<c>FileConnector.RecordHasCharacterWithoutByteImage</c>, I-O status '91'). (2) <see cref="ToNative"/> /
/// <see cref="ToMedium"/> — the ENCODING of a record image across the one-byte record channel, a BIJECTION over
/// that channel: a medium code unit the set does not define is carried as the native character of the same value
/// on input and back to the same code unit on output, so a relative or indexed store that re-persists every record
/// at CLOSE rewrites a record it never changed byte-exactly. Membership is the statement's question; the encoding
/// is the boundary's, and after the screen it is total.</para>
/// <para><b>What is converted, and what is not.</b> GR6 speaks of the characters of the RECORD; §13.18.13.4 GR3
/// applies "<i>the specified alphabet … for code-set conversion of all data items in each record</i>". The
/// framing this processor adds around a record — the §9.1.7.2 record-length header of a VARYING or keyed file,
/// and a line sequential file's line delimiter — is not data of the record and stays in the native encoding;
/// §9.1.7.2 leaves that framing implementor-defined ("<i>any information the implementor may add to the record on
/// the physical storage medium (such as record length headers)</i>"), and keeping it native is what lets one
/// framing serve a converted and an unconverted file alike. The determination is published in
/// <c>docs/CONFORMANCE.md</c> §2 row 27.</para>
/// <para><b>The boundary.</b> Conversion happens where the record's characters physically cross to or from the
/// medium and NOWHERE else, so a record area, a key value, a comparison and a lock identity are always in the
/// native character set: <c>SequentialConnector.NextFrame</c> (the one physical framing walk) and its
/// <c>EmitRecord</c>/<c>EmitRecordLine</c> twins on the write side, and <see cref="RecordFraming"/>'s store
/// payload for the keyed organizations.</para>
/// </summary>
public sealed class CodeSetConversion
{
    /// <summary>The width of the record channel this conversion serves: one byte per character position, so
    /// medium code units — and the native characters a record of this class can hold on the medium — are
    /// 0…255.</summary>
    public const int ChannelUnits = 256;

    /// <summary>Medium code unit → the native character it represents (GR6 a). Its length is the number of
    /// characters in the coded character set; a single-byte code has 256, ISO/IEC 646 has 128.</summary>
    private readonly char[] _toNative;

    /// <summary>The inverse over the whole channel (GR6 b), indexed by native character 0…255: the medium code
    /// unit that native character is written as. A native character that is a member of the set maps to its
    /// associated code unit (below <c>_toNative.Length</c>); one that is not, to the channel code unit of the same
    /// value, which lies outside the set (see the type remarks — the encoding is a bijection, membership is
    /// <see cref="Represents"/>).</summary>
    private readonly char[] _toMedium;

    /// <summary>Build the conversion from §12.3.7.4 GR7's correspondence: <paramref name="toNative"/>[u] is the
    /// native character that medium code unit <c>u</c> represents.</summary>
    /// <exception cref="ArgumentException">The correspondence is not one this record channel can invert: it has no
    /// characters or more than the channel's 256, or two code units share a native character, or a native
    /// character it names lies outside the one-byte record channel, or a member's native character is the value of
    /// a code unit OUTSIDE the set (the channel bijection would then give two medium units one native character).
    /// A registered single-byte code page and the ISO/IEC 646 identity satisfy all four; the check states the
    /// requirement rather than trusting the caller.</exception>
    public CodeSetConversion(char[] toNative)
    {
        ArgumentNullException.ThrowIfNull(toNative);
        if (toNative.Length is 0 or > ChannelUnits)
            throw new ArgumentException($"a CODE-SET coded character set shall be a single-byte code on this "
                + $"medium: {toNative.Length} characters (ISO §13.18.13.4 GR6)", nameof(toNative));
        _toNative = toNative;
        _toMedium = new char[ChannelUnits];
        var assigned = new bool[ChannelUnits];
        for (int unit = 0; unit < toNative.Length; unit++)
        {
            char native = toNative[unit];
            if (native >= ChannelUnits)
                throw new ArgumentException($"medium code unit {unit} corresponds to U+{(int)native:X4}, which "
                    + "is outside the one-byte record channel (ISO §13.18.13.4 GR6 b)", nameof(toNative));
            if (assigned[native])
                throw new ArgumentException($"medium code units {(int)_toMedium[native]} and {unit} both "
                    + $"correspond to U+{(int)native:X4}: the correspondence is not invertible "
                    + "(ISO §13.18.13.4 GR6 b)", nameof(toNative));
            _toMedium[native] = (char)unit;
            assigned[native] = true;
        }
        // The channel units the set does not define travel as themselves (ToNative's pass-through arm), so their
        // native values must be free for the inverse to be one.
        for (int unit = toNative.Length; unit < ChannelUnits; unit++)
        {
            if (assigned[unit])
                throw new ArgumentException($"medium code unit {(int)_toMedium[unit]} corresponds to U+{unit:X4}, "
                    + $"which is also the value of code unit {unit} outside the {toNative.Length}-character set: "
                    + "the channel encoding would not be invertible (ISO §13.18.13.4 GR6)", nameof(toNative));
            _toMedium[unit] = (char)unit;
        }
    }

    /// <summary>§13.18.13.4 GR6 b's MEMBERSHIP question — true when <paramref name="native"/> has an "<i>associated
    /// coded character as defined in the alphabet being used</i>", i.e. is a character of this coded character
    /// set. A native character above the one-byte channel never is.</summary>
    public bool Represents(char native) =>
        StorageByte.HasByte(native) && _toMedium[StorageByte.ToByte(native)] < _toNative.Length;

    /// <summary>True when <paramref name="record"/> holds at least one character this coded character set does
    /// not represent (<see cref="Represents"/>) — GR6 b cannot replace it, so the output statement is
    /// unsuccessful. The ONE predicate behind '91' (and '71' / '09' on a line sequential file) for a file with
    /// a CODE-SET conversion.</summary>
    public bool HasCharacterWithoutImage(ReadOnlySpan<char> record)
    {
        foreach (char c in record)
            if (!Represents(c)) return true;
        return false;
    }

    /// <summary>GR6 a — the native form of a record image just read from the storage medium.</summary>
    public string ToNative(string mediumImage)
    {
        if (mediumImage.Length == 0) return mediumImage;
        return string.Create(mediumImage.Length, (mediumImage, _toNative), static (dst, s) =>
        {
            var (src, map) = s;
            for (int i = 0; i < src.Length; i++)
            {
                char u = src[i];
                // A code unit outside the set has no associated native character; it is left as it stands rather
                // than mapped to something it is not — the medium held a byte this coded character set does not
                // define, which is a property of the FILE, not an error this READ may invent a status for
                // (§13.18.13.4 names no condition for it; the one exception is a line sequential file, whose
                // character set it is outside of — '09', SequentialConnector.RecordAreaOutsideLineCharacterSet).
                // It is not a member (Represents), so a WRITE or REWRITE of it is refused like any other
                // non-member, and ToMedium carries it back to the same unit when a store re-persists the record.
                // Unreachable for a complete single-byte page.
                dst[i] = StorageByte.ToChar((byte)(u < map.Length ? map[u] : u));
            }
        });
    }

    /// <summary>GR6 b — the storage-medium form of a record image about to be written. The output statement
    /// has already refused a record holding a non-member (<see cref="HasCharacterWithoutImage"/>), so every
    /// character here is a member, or a channel unit the medium itself supplied (see the type remarks).</summary>
    /// <exception cref="InvalidOperationException">A character above the one-byte channel reached the medium —
    /// a write path skipped the statement's screen. Like <see cref="FileCharacterSet.Medium"/>'s exception
    /// fallback for a file with no CODE-SET, it fails loudly rather than write a silent substitute.</exception>
    public string ToMedium(string nativeImage)
    {
        if (nativeImage.Length == 0) return nativeImage;
        return string.Create(nativeImage.Length, (nativeImage, _toMedium), static (dst, s) =>
        {
            var (src, map) = s;
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                if (!StorageByte.HasByte(c))
                    throw new InvalidOperationException($"the record contains U+{(int)c:X4}, which the file's "
                        + "CODE-SET coded character set does not represent, and no WRITE or REWRITE screen refused it "
                        + "(ISO §13.18.13.4 GR6 b)");
                dst[i] = map[StorageByte.ToByte(c)];
            }
        });
    }
}
