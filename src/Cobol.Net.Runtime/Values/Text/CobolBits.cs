// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The <c>USAGE BIT</c> record-image codec (ISO §13.18.60.4 GR5 "bits shall be used"; §8.5.1.6.3 alignment;
/// design D19, fix-queue PB43).
///
/// <para><b>Two representations, one value.</b> A boolean item's VALUE carrier is a <c>'0'</c>/<c>'1'</c> string —
/// one character per boolean position — for BOTH usages, because every MOVE / compare / ref-mod / fill path is
/// character-shaped and §13.18.40.4 GR14 lets a boolean character be represented as an alphanumeric character. What
/// differs is what the item OCCUPIES in a record image:</para>
/// <list type="bullet">
///   <item><b>USAGE DISPLAY</b> (implied when no USAGE clause is written, §13.18.60.3 SR13(b)) — the carrier IS the
///         image, one character per position, per §13.18.60.4 GR7's "alphanumeric coded character set". These
///         helpers are never called for it.</item>
///   <item><b>USAGE BIT</b> — the image is the PACKED bits, <c>ceil(n / 8)</c> characters. That is what these
///         helpers do.</item>
/// </list>
///
/// <para>⛔ <b>THE BIT ORDER IS HIGH-ORDER-FIRST, AND IT IS NOT ARBITRARY.</b> §8.5.1.6.3 speaks throughout of
/// "the first bit position of the first available byte" and lays successive items at "the next bit position", so
/// boolean position 1 is the most significant bit of the first byte and the run fills toward the low-order end.
/// A trailing partial byte is zero-filled — those are §8.5.1.6.3's implicit filler bits, which §15.50.4 r5
/// requires be counted in the length (and they are: the width is the ceiling).</para>
///
/// <para>⚠ <b>A run may span several ITEMS.</b> Two bit items of the SAME LEVEL share a byte (§8.5.1.6.3's
/// next-bit-position rule), so the emitter concatenates their carriers and packs ONCE — which is why these take a
/// whole run's bits rather than one item's.</para>
/// </summary>
public static class CobolBits
{
    private const int BitsPerByte = 8;

    /// <summary>Pack a run's <c>'0'</c>/<c>'1'</c> carrier into its record image — <c>ceil(count / 8)</c>
    /// characters, high-order bit first, trailing filler bits zero. Any character other than <c>'1'</c> reads as
    /// zero, which makes a space-padded or uninitialized carrier decode as the all-zero boolean value §13.18.63
    /// gives it rather than throwing.</summary>
    /// <summary>Pack EVERY boolean position the carrier holds — the count is <paramref name="bits"/>.Length.
    /// The shape a REFERENCE-MODIFIED usage-bit operand needs: ISO/IEC 1989:2023 §8.4.3.3.4 GR5c gives the slice
    /// its own position count, which may be a runtime expression, so the caller never has to re-derive it (and
    /// a second evaluation of the slice expression is exactly what this overload avoids).</summary>
    public static string Pack(string bits) => Pack(bits, bits.Length);

    public static string Pack(string bits, int count)
    {
        // ⛔ ONE bit-order law, and no extra allocation to get it: the fresh image is all-zero, so blitting only
        // the '1' positions reproduces Pack's contract exactly ("any character other than '1' reads as zero",
        // trailing filler zero) while sharing the high-order-first arithmetic with WriteWindow. Going through
        // WriteWindow itself would have cost a throwaway zero STRING plus its ToCharArray on a path every
        // whole-group image of a bit run takes.
        var image = new char[(count + BitsPerByte - 1) / BitsPerByte];
        Blit(image, 0, bits, System.Math.Min(count, bits.Length));
        return new string(image);
    }

    /// <summary>Unpack <paramref name="count"/> boolean positions from a record image back into the
    /// <c>'0'</c>/<c>'1'</c> carrier — the exact inverse of <see cref="Pack"/>. A short image (the pad a short
    /// record legitimately deposits) yields <c>'0'</c> for the missing positions.</summary>
    public static string Unpack(string image, int count) => ReadWindow(image, 0, count);

    /// <summary>
    /// Read <paramref name="count"/> boolean positions starting at ABSOLUTE bit position
    /// <paramref name="startBit"/> of a byte image — the §13.18.44.4 GR1 storage association read for a bit
    /// item that shares its storage area with other data descriptions ("Storage association for the subject of
    /// the entry starts at the first BIT of the data item referenced by data-name-2 and continues over an area
    /// sufficient to contain the number of BITS required"). <see cref="Unpack"/> is the
    /// <paramref name="startBit"/> = 0 case.
    /// <para>A position past the end of the image yields <c>'0'</c>, the same benign decode a short record's
    /// pad gets — §13.18.63's all-zero boolean initial state rather than a throw.</para>
    /// </summary>
    public static string ReadWindow(string image, int startBit, int count)
    {
        var bits = new char[count];
        for (int i = 0; i < count; i++)
        {
            int p = startBit + i, b = p / BitsPerByte;
            bits[i] = b >= 0 && b < image.Length
                      && (image[b] & (1 << (BitsPerByte - 1 - p % BitsPerByte))) != 0 ? '1' : '0';
        }
        return new string(bits);
    }

    /// <summary>
    /// Splice <paramref name="bits"/> into <paramref name="image"/> starting at ABSOLUTE bit position
    /// <paramref name="startBit"/>, returning the new image and leaving EVERY OTHER BIT of it untouched — the
    /// receiving twin of <see cref="ReadWindow"/>, and the reason a sub-byte bit item can share a byte with its
    /// siblings (§8.5.1.6.3: "an elementary bit data item immediately following an elementary bit data item or
    /// bit group item of the same level" is placed "at the next bit position in storage"). A write that would
    /// run past the image stops at its end — the same truncation a short window gets everywhere else.
    /// </summary>
    public static string WriteWindow(string image, int startBit, string bits)
    {
        var buf = image.ToCharArray();
        Blit(buf, startBit, bits, bits.Length);
        return new string(buf);
    }

    /// <summary>⛔ THE ONE WRITE-SIDE bit-order law: set/clear <paramref name="count"/> boolean positions of
    /// <paramref name="buf"/> starting at bit <paramref name="startBit"/>, high-order bit of each byte first.
    /// Shared by <see cref="Pack"/> (into a fresh all-zero image) and <see cref="WriteWindow"/> (into a copy of
    /// an existing one) so the two cannot disagree about which bit is position 1. A position past the end of the
    /// buffer is dropped — the same truncation a short window gets everywhere else.</summary>
    private static void Blit(char[] buf, int startBit, string bits, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int p = startBit + i, b = p / BitsPerByte;
            if (b < 0) continue;
            if (b >= buf.Length) break;
            int mask = 1 << (BitsPerByte - 1 - p % BitsPerByte);
            buf[b] = (char)(bits[i] == '1' ? buf[b] | mask : buf[b] & ~mask);
        }
    }

    /// <summary>The slice of a run's unpacked carrier belonging to one item — the emitter distributes a shared
    /// byte back to the several fields that occupy it.</summary>
    public static string Slice(string carrier, int offset, int count) =>
        offset >= carrier.Length ? new string('0', count)
        : carrier.Substring(offset, System.Math.Min(count, carrier.Length - offset)).PadRight(count, '0');

    /// <summary>The UTF-16BE byte serialization of a NATIONAL string — one char per BYTE, two bytes (high-order
    /// first) per national position (D-N1: national is UTF-16, one code unit per character position). ⛔ THE ONE
    /// national→bytes reduction: <c>CobolIntrinsics.Convert</c>'s NAT source arm and the compiler-emitted CONVERT
    /// ANY raw-storage channel (a national leaf's storage bits, §15.19.3 r7) both ride it — never a second
    /// serializer (fix-queue PB59 family 5b). Not a BIT codec, but it lives here because this class is where the
    /// storage-byte reductions are: the char==byte string convention is <see cref="Pack"/>'s.</summary>
    public static string NatBytes(string s)
    {
        var image = new char[s.Length * BytesPerNational];
        for (int i = 0; i < s.Length; i++) PutNat(image, BytesPerNational * i, s[i]);
        return new string(image);
    }

    /// <summary>⛔ THE ONE national byte-order law, write side: the high-order byte of a national character
    /// position occupies the LOWER-addressed byte (UTF-16 <b>BE</b>, D-N1). Shared by <see cref="NatBytes"/> (into
    /// a fresh image) and <see cref="NatWriteWindow"/> (into a copy of an existing one), exactly as
    /// <see cref="Blit"/> is shared by <see cref="Pack"/> and <see cref="WriteWindow"/>, so the two cannot
    /// disagree about which byte comes first.</summary>
    private static void PutNat(char[] buf, int at, char c)
    {
        buf[at] = (char)(c >> 8);
        buf[at + 1] = (char)(c & 0xFF);
    }

    /// <summary>Bytes per national character position — ISO §13.18.60.4 GR8 leaves the size to the implementor
    /// ("National characters shall be represented in the storage of the computer as characters of a uniform size
    /// equal to or a multiple of the size of characters in the computer's alphanumeric character set. Each
    /// implementor shall specify the size and representation of characters stored for usage NATIONAL") and
    /// WiseOwl COBOL pins <b>2</b> — determination D-N1/D-N3, and the answer <c>FUNCTION BYTE-LENGTH</c>, CONVERT's
    /// raw-storage channel and CONFORMANCE.md items 33/188 already give.</summary>
    public const int BytesPerNational = 2;

    /// <summary>⛔ THE INVERSE OF <see cref="NatBytes"/>: decode <paramref name="positions"/> national character
    /// positions from the UTF-16BE byte pairs at <paramref name="startByte"/> of a byte image (char==byte). A
    /// pair that runs past the end of the image decodes as the national SPACE — the same benign short-window
    /// decode <see cref="ReadWindow"/> gives a bit run, never a throw (a shared storage area may legitimately be
    /// shorter than a view over it, §13.18.44.4 GR1's larger-of-the-two allocation notwithstanding, and a BASED
    /// window over a hand-sized ALLOCATE is the reachable case).</summary>
    public static string NatReadWindow(string image, int startByte, int positions)
    {
        var chars = new char[positions];
        for (int i = 0; i < positions; i++)
        {
            int hi = startByte + BytesPerNational * i;
            chars[i] = hi >= 0 && hi + 1 < image.Length
                ? (char)((image[hi] << 8) | (image[hi + 1] & 0xFF))
                : ' ';
        }
        return new string(chars);
    }

    /// <summary>The receiving twin of <see cref="NatReadWindow"/>: splice <paramref name="value"/>, fitted to
    /// exactly <paramref name="positions"/> national positions, into <paramref name="image"/> as UTF-16BE byte
    /// pairs starting at <paramref name="startByte"/>, leaving every other byte of the image untouched (ISO
    /// §13.18.44.4 GR1 — the shared area is one storage area and a write through one view must not disturb the
    /// bytes of another). Fitting happens in POSITIONS, before serialization: padding the BYTES with 0x20 would
    /// manufacture U+2020 national characters instead of national spaces.</summary>
    public static string NatWriteWindow(string image, int startByte, int positions, string value)
    {
        var buf = image.ToCharArray();
        for (int i = 0; i < positions; i++)
        {
            int hi = startByte + BytesPerNational * i;
            if (hi < 0) continue;
            if (hi + 1 >= buf.Length) break;
            PutNat(buf, hi, i < value.Length ? value[i] : ' ');
        }
        return new string(buf);
    }
}
