// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// THE record-image codec for a fixed-point numeric item — the one place a value becomes the BYTES it occupies,
/// and back (COBOLNET_DESIGN §14.4, numeric design D17). Every byte boundary goes through it: the whole-group
/// character image, a file record, a SORT/MERGE key window and a Tier-B REDEFINES backing.
/// <para>
/// The bytes are carried in a <see cref="string"/> under the LATIN-1 convention the record framing already uses —
/// chars 0–255 map 1:1 to bytes (<c>RecordFraming</c>) — so a binary or packed leaf rides the SAME image carrier
/// as a DISPLAY leaf and no second whole-group mechanism appears.
/// </para>
/// <para>
/// The representations are implementor-defined (ISO/IEC 1989:2023 §13.18.60.4 GR4 BINARY "a radix of 2 is used",
/// GR11 PACKED-DECIMAL "a radix of 10 … each digit position shall occupy the minimum possible configuration",
/// GR7 DISPLAY) and §4.2.16 obliges us to document ours — <see cref="NumericByteForm"/> carries that
/// documentation and this file is its implementation. Widths come from <see cref="NumProfile.StorageLength"/>,
/// which is what <c>FUNCTION BYTE-LENGTH</c> reports (§15.14.4 r1): ONE width, never two answers.
/// </para>
/// <para>
/// Scale is not represented — the image carries the UNSCALED digits and the decimal point stays implied, exactly
/// as it is in storage (§13.18.40.4: V occupies no position).
/// </para>
/// </summary>
public static partial class CobolNum
{
    /// <summary>Encode a fixed-point value as the bytes it occupies in a record, per the item's
    /// <see cref="NumProfile.ByteForm"/>. The result is EXACTLY the item's image width: its
    /// <see cref="NumProfile.StorageLength"/> for a binary/packed form, its digit run (plus a separate sign
    /// position) for the zoned form.</summary>
    public static string FormatImage(Int128 unscaled, in NumProfile item) => item.ByteForm switch
    {
        NumericByteForm.Zoned => FormatDisplay(unscaled, item),
        NumericByteForm.Binary => FormatBinaryImage(unscaled, item),
        NumericByteForm.Packed or NumericByteForm.PackedNoSign => FormatPackedImage(unscaled, item),
        _ => throw NoByteImage(item),
    };

    /// <summary>Storage-shape bridge (the <see cref="FormatDisplay(string, in NumProfile)"/> pattern): a field whose
    /// backing the whole-group analysis already turned into its character IMAGE is in image form — pass it
    /// through. Lets the compiler emit ONE expression whose field storage is decided later.</summary>
    public static string FormatImage(string image, in NumProfile item) => image;

    /// <summary>Store a (possibly spliced) record image back into a numeric field — the write half of an image
    /// view over a numeric item (reference modification, a RENAMES span leaf). The <paramref name="current"/>
    /// dummy selects the conversion for the field's ACTUAL storage, the same overload bridge
    /// <see cref="StoreDisplay(string, in NumProfile, long)"/> is: a native field decodes, an image-stored field
    /// keeps the image.</summary>
    public static long StoreImage(string image, in NumProfile item, long current) => (long)ParseImage(image, item);

    /// <inheritdoc cref="StoreImage(string, in NumProfile, long)"/>
    public static Int128 StoreImage(string image, in NumProfile item, Int128 current) => ParseImage(image, item);

    /// <inheritdoc cref="StoreImage(string, in NumProfile, long)"/>
    public static string StoreImage(string image, in NumProfile item, string current) => image;

    /// <summary>The UNSIGNED carrier lanes (kb/Work PB164): a <c>ulong</c>/<c>UInt128</c>-carried
    /// BinaryCapacity item (an unsigned <c>PIC 9(10..)</c> COMP-5 / BINARY-DOUBLE) writes its container value
    /// as the unsigned big-endian magnitude bytes at <see cref="NumProfile.StorageLength"/>.
    /// Without these overloads the group codec's emitted <c>FormatImage(field, …)</c> failed to COMPILE for
    /// such a leaf (UInt128→Int128 is an explicit conversion).
    /// <para>⛔ THE UNSIGNED VALUE MUST NOT ENTER THE SIGNED LANE (kb/Work PB164 — the Step D review's
    /// sweep-note #409). It was written as <c>FormatImage(unchecked((Int128)unscaled), item)</c> on the theory
    /// that the reinterpretation is bit-identical. It is not, because
    /// <see cref="FormatBinaryImage"/> applies §14.9.25.4 GR6b — "when an unsigned numeric item is the
    /// receiving item, the ABSOLUTE VALUE of the sending value is used" — and that rule is about a NEGATIVE
    /// VALUE, while a 16-byte container value ≥ 2^127 reinterprets to a negative <c>Int128</c> that is not a
    /// negative value at all. The rule then NEGATED the bit pattern: an item holding 2^128−1 encoded as
    /// <c>00…01</c>. Measured, not deduced — a windowed <c>PIC 9(31) COMP-5</c> round trip came back 1.
    /// The 8-byte lane never tripped it (every <c>ulong</c> is a non-negative <c>Int128</c>), but it routes
    /// through the same door so the fix cannot rot back in.</para></summary>
    public static string FormatImage(ulong unscaled, in NumProfile item) =>
        FormatImage((UInt128)unscaled, item);

    /// <inheritdoc cref="FormatImage(ulong, in NumProfile)"/>
    public static string FormatImage(UInt128 unscaled, in NumProfile item) =>
        item.ByteForm is NumericByteForm.Binary
            ? BinaryBytes(Mask(unscaled, Width(item)), Width(item))
            : FormatImage(unchecked((Int128)unscaled), item);

    /// <inheritdoc cref="FormatImage(ulong, in NumProfile)"/>
    public static ulong StoreImage(string image, in NumProfile item, ulong current) =>
        unchecked((ulong)ParseImage(image, item));

    /// <summary>The unsigned READ twins of <see cref="ParseImage"/> (the Step D arm-1 dissolution, kb/Work
    /// PB164): a ulong/UInt128-carried BinaryCapacity item read through a byte-form WINDOW must decode to
    /// its unsigned container value — the signed lane's Int128 result reinterprets bit-identically, exactly
    /// as the unsigned StoreImage lanes above do (the two's-complement bytes of the reinterpreted value ARE
    /// the unsigned magnitude bytes). Without these, a wide unsigned COMP-5 window decoded SIGNED — a silent
    /// wrong answer above Int128.MaxValue's bit pattern.</summary>
    public static ulong ParseImageU(string image, in NumProfile item) =>
        unchecked((ulong)ParseImage(image, item));

    /// <inheritdoc cref="ParseImageU(string, in NumProfile)"/>
    public static UInt128 ParseImageU128(string image, in NumProfile item) =>
        unchecked((UInt128)ParseImage(image, item));

    /// <inheritdoc cref="FormatImage(ulong, in NumProfile)"/>
    public static UInt128 StoreImage(string image, in NumProfile item, UInt128 current) =>
        unchecked((UInt128)ParseImage(image, item));

    /// <summary>The FLOAT carrier lanes (kb/Work PB164 wave 2): a <c>float</c>/<c>double</c>-carried item's
    /// image is its IEEE 754 interchange encoding, 4 bytes for <see cref="NumericByteForm.Ieee32"/> / 8 for
    /// <see cref="NumericByteForm.Ieee64"/> — bit reinterpretation, never a numeric conversion, packed one
    /// byte per char exactly like the binary lane. The byte order follows the profile's effective FLOAT-BINARY
    /// endianness (<see cref="NumProfile.FloatLittleEndian"/> — §13.18.60.4 GR19 + §11.9.8; big-endian is the
    /// documented default and the only order the non-standard float usages take). A double value entering an
    /// Ieee32 profile narrows to binary32 first (the item's own precision).
    /// DISTINCTLY NAMED (not FormatImage overloads): an integer argument converts implicitly to BOTH Int128
    /// and float, so overloading made every integer call site ambiguous (CS0121).</summary>
    public static string FormatImageFloat(double value, in NumProfile item) =>
        item.ByteForm is NumericByteForm.Ieee32
            ? FormatIeeeBits(BitConverter.SingleToUInt32Bits((float)value), 4, item.FloatLittleEndian)
            : FormatIeeeBits(BitConverter.DoubleToUInt64Bits(value), 8, item.FloatLittleEndian);

    /// <summary>⛔ THE BINARY32 CARRIER'S OWN ENCODE LANE (kb/Work PB961) — a <c>float</c> in, its binary32 bits
    /// out, with NO binary64 anywhere between. <see cref="FormatImageFloat(double, in NumProfile)"/> cannot be that
    /// lane for a <c>float</c> value: the argument widens to <c>double</c> at the call, and an ISO/IEC 60559 format
    /// conversion QUIETS a signaling NaN (x64 <c>cvtss2sd</c> sets the quiet bit), so SET CONTENT …
    /// FLOAT-NOT-A-NUMBER-SIGNALING into a windowed FLOAT-BINARY-32 stored <c>0x7FC00001</c> — a QUIET NaN — where
    /// ISO §14.9.39.4 rule 35 requires "a canonical representation of a signaling NaN", and a same-usage MOVE or a
    /// group image round trip quieted one that §14.9.25.4 rule 6 c) says "is transferred to the receiving data
    /// item without change". The emitter picks this lane from the ITEM (<c>RuntimeApi.NumFormatImageFloat</c>'s
    /// <c>single</c> argument), never from the value's C# type. Distinctly named for its twin's CS0121 reason.</summary>
    public static string FormatImageSingle(float value, in NumProfile item) =>
        item.ByteForm is NumericByteForm.Ieee32
            ? FormatIeeeBits(BitConverter.SingleToUInt32Bits(value), 4, item.FloatLittleEndian)
            : FormatImageFloat(value, item);

    /// <summary>The binary32 carrier's own DECODE lane — the twin of <see cref="FormatImageSingle"/>: the image's
    /// binary32 bits reinterpreted as a <c>float</c>, never through <see cref="ParseImageFloat"/>'s <c>double</c>,
    /// whose Ieee32 arm widens and so quiets a signaling NaN (kb/Work PB961). <see cref="ParseImageFloat"/> stays
    /// the ARITHMETIC read, where the widening is the operation.</summary>
    public static float ParseImageSingle(string image, in NumProfile item) =>
        item.ByteForm is NumericByteForm.Ieee32
            ? BitConverter.UInt32BitsToSingle((uint)ImageFloatBits(image, item))
            : (float)ParseImageFloat(image, item);

    /// <inheritdoc cref="FormatImageFloat(double, in NumProfile)"/>
    public static double ParseImageFloat(string image, in NumProfile item)
    {
        ulong bits = ImageFloatBits(image, item);
        return item.ByteForm is NumericByteForm.Ieee32
            ? BitConverter.UInt32BitsToSingle((uint)bits) : BitConverter.UInt64BitsToDouble(bits);
    }

    /// <summary>The RAW interchange-format bits of a float carrier's image (binary32 in the low 32 bits for
    /// <see cref="NumericByteForm.Ieee32"/>) — the decode <see cref="ParseImageFloat"/> performs, stopped BEFORE
    /// any numeric conversion. It exists because a binary32 → binary64 widening QUIETS a signaling NaN, so a
    /// question about WHICH NaN an image holds (ISO §8.8.4.4.4 GR3 j)/k), kb/Work PB225) must be asked of these
    /// bits and never of the decoded <c>double</c>.</summary>
    public static ulong ImageFloatBits(string image, in NumProfile item)
    {
        int n = item.ByteForm is NumericByteForm.Ieee32 ? 4 : 8;
        ulong bits = 0;
        int take = image is null ? 0 : Math.Min(n, image.Length);
        // HIGH-ORDER-RIGHT (§13.18.60.4 GR19b): the LAST byte is the most significant — walk reversed.
        if (item.FloatLittleEndian)
            for (int i = take - 1; i >= 0; i--) bits = (bits << 8) | (byte)image![i];
        else
            for (int i = 0; i < take; i++) bits = (bits << 8) | (byte)image![i];
        return bits;
    }

    /// <inheritdoc cref="FormatImageFloat(double, in NumProfile)"/>
    public static float StoreImage(string image, in NumProfile item, float current) =>
        ParseImageSingle(image, item);

    /// <inheritdoc cref="FormatImageFloat(double, in NumProfile)"/>
    public static double StoreImage(string image, in NumProfile item, double current) =>
        ParseImageFloat(image, item);

    private static string FormatIeeeBits(ulong bits, int n, bool littleEndian)
    {
        var chars = new char[n];
        for (int i = 0; i < n; i++)
            chars[i] = (char)(byte)(bits >> (8 * (littleEndian ? i : n - 1 - i)));
        return new string(chars);
    }

    /// <summary>Decode an item's record-image bytes back to its unscaled value — the inverse of
    /// <see cref="FormatImage(Int128, in NumProfile)"/>.</summary>
    public static Int128 ParseImage(string image, in NumProfile item) => item.ByteForm switch
    {
        NumericByteForm.Zoned => ParseDisplay(image, item),
        NumericByteForm.Binary => ParseBinaryImage(image, item),
        NumericByteForm.Packed or NumericByteForm.PackedNoSign => ParsePackedImage(image, item),
        _ => throw NoByteImage(item),
    };

    // ── §14.6.13.2 rule 2 — the CHECKED sending read of a FIXED-POINT numeric item ─────────────────────────────
    //
    // ⛔ THIS IS RULE 3's TWIN, AND IT WAS THE MISSING HALF OF A TWO-ARM DISPATCH (kb/Work PB230). §14.6.13.2
    // states five sibling conditions over the SAME subject — the content of a sending operand that is not valid:
    //   rule 1  a BOOLEAN sending item that would evaluate false in a boolean class condition  → EC-DATA-INCOMPATIBLE
    //   rule 2  a NUMERIC sending item that is NOT standard floating-point and would evaluate  → EC-DATA-INCOMPATIBLE
    //           false in a numeric class condition
    //   rule 3  a STANDARD FLOATING-POINT sending operand that is ±Inf / NaN                   → EC-DATA-NOT-FINITE
    //   rule 4  the numeric-edited sender of a DE-EDITING MOVE holding a non-editing-result    → EC-DATA-INCOMPATIBLE
    // Rule 3 was wired end to end (CobolFloat.Sending at the two float sending-read chokepoints) and rule 4 was
    // wired for the de-edit (CobolEdit.DeEditFloat / DeEdit). Rule 2 — the BROAD one, the one that covers every
    // arithmetic statement's operands — had NO raise site at all, so `ADD 1 TO N` over a DISPLAY window holding
    // "AB1" fabricated a value in silence under `>>TURN EC-DATA-INCOMPATIBLE CHECKING ON`. This is that site.
    //
    // WHERE IT CAN FIRE. Only a numeric leaf whose storage is a CHARACTER WINDOW (a Tier-B REDEFINES view, or a
    // whole-group-aliased StoreAsImage leaf) can hold content that fails its own class condition — the typed-native
    // model stores every other numeric leaf in a native carrier that can only hold digits, which is exactly why
    // ConditionRenderer folds `IS NUMERIC` on such a leaf to the compile-time constant `true` (§8.8.4.4.4 GR3 n)1).
    // So the check rides the windowed decode, which is already the slow path, and a native-carrier read pays
    // nothing — not even a branch.

    /// <summary>The checked read of a FIXED-POINT numeric SENDING operand stored as its character image
    /// (ISO §14.6.13.2 rule 2): return the decoded value, but when EC-DATA-INCOMPATIBLE checking is enabled and
    /// the content "would evaluate to false in a numeric class condition" — <see cref="IsNumericImage"/>, the ONE
    /// §8.8.4.4.4 GR3 n)1 predicate — raise the fatal EC-DATA-INCOMPATIBLE (via
    /// <see cref="ExceptionState.DataIncompatibleError"/>). The twin of <see cref="CobolFloat.Sending(double)"/>,
    /// emitted at the same two sending-read chokepoints (the numeric-value read and the string-image read).
    /// <para>Rule 2's exemption list is EXACTLY TWO entries — "a sending item is referenced in a class condition"
    /// and "a sending item is processed in a VALIDATE statement" — where rule 3's is four (it adds a sign
    /// condition and a same-usage MOVE). The two lists are carried by the compiler's <c>SendingRef</c> and
    /// realized as a RAW <see cref="ParseImage"/> at the exempt sites, so this wrap never appears there.</para>
    /// <para>With checking OFF the flag test short-circuits before the O(digits) scan and the caller's tolerant
    /// value stands — byte-behaviour-identical to a pre-slice build, which is conformant because the standard
    /// makes the result of the reference UNDEFINED in exactly this case.</para></summary>
    public static Int128 ParseImageSending(string image, in NumProfile item)
    {
        if (ExceptionState.DataIncompatibleChecking && !IsNumericImage(image, item))
            ExceptionState.DataIncompatibleError(Rule2Detail);
        return ParseImage(image, item);
    }

    /// <summary>The one wording for rule 2's condition detail — both checked reads report the same thing, and
    /// two copies of it would be two things to keep in step for no reason.</summary>
    private const string Rule2Detail =
        "the content of a numeric sending item is not valid for its data description "
        + "(ISO §14.6.13.2 rule 2 — it would evaluate to false in a numeric class condition)";

    /// <inheritdoc cref="ParseImageSending(string, in NumProfile)"/>
    public static UInt128 ParseImageU128Sending(string image, in NumProfile item) =>
        unchecked((UInt128)ParseImageSending(image, item));

    /// <summary>The rule-2 checked sending read on the STRING channel — a ZONED window whose stored image IS its
    /// text, so it is handed on verbatim rather than decoded (<c>OperandText.FieldAsString</c>: a decode-and-
    /// reformat round trip would silently turn the incompatible content into zeros, which is the very content
    /// this condition exists to report). §14.6.13.2 rule 2 speaks of the content being "referenced during the
    /// execution of a statement" with no numeric-context qualification, so <c>DISPLAY N</c> and
    /// <c>MOVE N TO alphanumeric-item</c> reference it exactly as <c>ADD 1 TO N</c> does — the same two
    /// exemptions, the same raise, the same verbatim value once it has been reported.</summary>
    public static string SendingImage(string image, in NumProfile item)
    {
        if (ExceptionState.DataIncompatibleChecking && !IsNumericImage(image, item))
            ExceptionState.DataIncompatibleError(Rule2Detail);
        return image;
    }

    /// <summary>
    /// ⛔ THE ONE NUMERIC CLASS CONDITION over a numeric item's STORED IMAGE (ISO §8.8.4.4.4 GR3 n)1) — "the content
    /// of a data item … consists entirely of a valid representation for the usage". Two callers need this exact
    /// question and writing it twice would guarantee drift: the class condition itself (<c>IF W IS NUMERIC</c> over
    /// a REDEFINES window) and §14.6.13.2 rule 2's checked sending read above, whose test the standard defines BY
    /// REFERENCE to the class condition ("would evaluate to false in a numeric class condition"). One rule, one
    /// place.
    /// <para><b>Keyed on the BYTE FORM, because that is what "a valid representation for the usage" means</b>
    /// (§13.18.60.4 GR7/GR11/GR12 leave each representation to the implementor and <see cref="NumericByteForm"/>
    /// is WiseOwl COBOL's documentation of ours):</para>
    /// <list type="bullet">
    /// <item>ZONED — GR3 n)1.a exactly: "the presence or absence of an operational sign … is in agreement with
    /// the data description … and … the content, except for the operational sign, consists entirely of the
    /// characters 0, 1, 2, …, 9". Delegated to the same <see cref="CobolClass.IsNumericZoned"/> /
    /// <see cref="CobolClass.IsNumeric"/> the class condition already emitted for this case.</item>
    /// <item>PACKED / PACKED-DECIMAL WITH NO SIGN — GR3 n)1.c: every digit nibble is 0–9, and for the signed form
    /// the trailing nibble is a SIGN nibble (0xA–0xF), never a digit. A window shorter than the pinned width is
    /// not a representation of the item at all.</item>
    /// <item>BINARY — GR3 n)1.c's second half. Every two's-complement byte pattern IS a valid representation, so
    /// the only test left is the range one: "if a PICTURE clause is specified, the numeric value is within the
    /// range of values implied by the PICTURE clause". That range is the item's own capacity discipline
    /// (<see cref="NumProfile.Truncation"/> — the SAME discipline <c>TryStore</c> bounds a store by, never a
    /// second opinion): 10^Digits for <see cref="NumericTruncation.DigitCount"/>/<see cref="NumericTruncation.PackedDecimal"/>,
    /// and for <see cref="NumericTruncation.BinaryCapacity"/> the CONTAINER range — a COMP-5 / BINARY-* item's
    /// value range is its storage, so testing it against a picture would condemn values the item legitimately
    /// holds.</item>
    /// <item>Standard floating-point — NOT this predicate's question. GR3 n)1.b is finiteness and §14.6.13.2
    /// rule 3 owns it as EC-DATA-NOT-FINITE (<see cref="CobolFloat.Sending(double)"/>); answering <c>true</c>
    /// here keeps the fixed-point raise off a float operand rather than double-reporting it.</item>
    /// </list>
    /// </summary>
    public static bool IsNumericImage(string? image, in NumProfile item) => item.ByteForm switch
    {
        NumericByteForm.Zoned => IsNumericZonedImage(image, item),
        NumericByteForm.Packed or NumericByteForm.PackedNoSign => IsNumericPackedImage(image, item),
        NumericByteForm.Binary => image is not null && image.Length >= Width(item)
                                  && InPictureRange(ParseBinaryImage(image, item), item),
        // Ieee32/Ieee64 (rule 3's subject) and None (a construction bug the codec's own guard catches) are not
        // this rule's question — never claim incompatibility we have not tested for.
        _ => true,
    };

    /// <summary>§8.8.4.4.4 GR3 n)1.a over a ZONED image — the class condition's own predicates, picked by the
    /// item's <see cref="NumProfile.SignKind"/> so the "in agreement with the data description" half is the
    /// item's declared sign presentation and not a guess. <see cref="NumericSign.BinaryMinus"/> on a zoned item
    /// is the leading-<c>-</c>-only form <see cref="ParseDisplay"/> decodes, so it is tested as written: an
    /// optional leading minus over an otherwise all-digit run.</summary>
    private static bool IsNumericZonedImage(string? image, in NumProfile item)
    {
        if (!item.Signed) return CobolClass.IsNumeric(image);
        return item.SignKind switch
        {
            NumericSign.LeadingSeparate => CobolClass.IsNumericZoned(image, 2, leading: true, item.SignEncoding),
            NumericSign.TrailingSeparate => CobolClass.IsNumericZoned(image, 2, leading: false, item.SignEncoding),
            NumericSign.LeadingOverpunch => CobolClass.IsNumericZoned(image, 1, leading: true, item.SignEncoding),
            NumericSign.BinaryMinus => !string.IsNullOrEmpty(image)
                && CobolClass.IsNumeric(image[0] == '-' ? image[1..] : image),
            // TrailingOverpunch — the DISPLAY default; the valid-sign set is the item's own §13.18.52.4 GR5 b)
            // convention (kb/Work PB803), never a fixed one.
            _ => CobolClass.IsNumericZoned(image, 1, leading: false, item.SignEncoding),
        };
    }

    /// <summary>§8.8.4.4.4 GR3 n)1.c over a PACKED image: every digit nibble is 0–9, the signed form's trailing
    /// nibble is a sign nibble (0xA–0xF — a digit there is not a representation of any sign), and the decoded
    /// value is within the picture's range. The nibble layout is <see cref="ParsePackedImage"/>'s, read the same
    /// way in both directions.</summary>
    private static bool IsNumericPackedImage(string? image, in NumProfile item)
    {
        int n = Width(item);
        if (image is null || image.Length < n) return false;
        bool hasSignNibble = item.ByteForm is NumericByteForm.Packed;
        int digitNibbles = 2 * n - (hasSignNibble ? 1 : 0);
        for (int i = 0; i < digitNibbles; i++)
            if (((i % 2 == 0 ? image[i / 2] >> 4 : image[i / 2]) & 0x0F) > 9) return false;
        if (hasSignNibble && (image[n - 1] & 0x0F) < 0x0A) return false;
        return InPictureRange(ParsePackedImage(image, item), item);
    }

    /// <summary>§8.8.4.4.4 GR3 n)1.c's range half — "the numeric value is within the range of values implied by the
    /// PICTURE clause" — expressed as the item's OWN capacity discipline, the same one
    /// <see cref="CobolNum.TryStore"/> bounds a store by. A <see cref="NumericTruncation.BinaryCapacity"/> item
    /// (COMP-5 / BINARY-CHAR..DOUBLE) takes its value range from its storage, not from a picture
    /// (§13.18.60.4 GR12 — "the implementor may allow a wider range"), so every container value is in range.</summary>
    private static bool InPictureRange(Int128 unscaled, in NumProfile item) =>
        item.Truncation == NumericTruncation.BinaryCapacity
        || Int128.Abs(unscaled) < Pow10Wide(item.Digits);

    /// <summary>An item with no byte representation (<see cref="NumericByteForm.None"/> — no shipping
    /// numeric usage since the R40 INDEX pin; the guard for an unstated future usage) reached a byte boundary. That is a
    /// compiler invariant break, never a COBOL runtime condition: the binder's <c>IsImageCapable</c> gate is
    /// supposed to make it unreachable. Fail LOUD rather than invent bytes — inventing them is exactly the class
    /// of defect this codec exists to retire.</summary>
    private static InvalidOperationException NoByteImage(in NumProfile item) =>
        new($"no byte representation for a numeric item with ByteForm={item.ByteForm} "
            + $"(Digits={item.Digits}, StorageLength={item.StorageLength}) — it must never reach a record image");

    // ── BINARY (radix 2, §13.18.60.4 GR4/GR6/GR12) ────────────────────────────────────────────────────────────
    // Two's complement, MOST SIGNIFICANT BYTE FIRST, in exactly StorageLength bytes. Big-endian is the choice
    // GR4 asks the implementor to make and is what IBM, Micro Focus and GnuCOBOL all write for USAGE BINARY, so
    // a data file interchanges. An UNSIGNED item holds the magnitude (§14.9.25.4 GR8 — "the absolute value").

    private static string FormatBinaryImage(Int128 unscaled, in NumProfile item)
    {
        int n = Width(item);
        // §14.9.25.4 GR6b — an unsigned receiving item takes the ABSOLUTE VALUE of the sending value. This is a
        // VALUE-domain rule; the unsigned CARRIER lanes above therefore never come through here, because their
        // "negative" Int128 is a reinterpreted bit pattern, not a negative value.
        if (!item.Signed && unscaled < 0) unscaled = -unscaled;
        return BinaryBytes(Mask(unchecked((UInt128)unscaled), n), n);
    }

    /// <summary>The radix-2 rendering itself: <paramref name="raw"/>'s low <paramref name="n"/> bytes, most
    /// significant first, one byte per char. The ONE place binary bytes are laid out — the signed lane reaches
    /// it after §14.9.25.4 GR6b normalization, the unsigned carrier lanes directly.</summary>
    private static string BinaryBytes(UInt128 raw, int n)
    {
        var chars = new char[n];
        for (int i = 0; i < n; i++) chars[i] = (char)(byte)(raw >> (8 * (n - 1 - i)));
        return new string(chars);
    }

    private static Int128 ParseBinaryImage(string image, in NumProfile item)
    {
        int n = Width(item);
        UInt128 raw = 0;
        // A window shorter than the pinned width can only arrive from incompatible data (a short record's pad,
        // §14.6.13.2 leaves it undefined): the bytes present are read as the LOW-order bytes, deterministically.
        int take = image is null ? 0 : Math.Min(n, image.Length);
        for (int i = 0; i < take; i++) raw = (raw << 8) | (byte)image![i];
        // UNCHECKED: a 16-byte unsigned container value ≥ 2^127 has no Int128 image, and the reinterpretation is
        // exactly what ParseImageU128 converts back — an overflow throw here would be a wrong stage, not a
        // diagnosis (the value is legal; §13.18.60.4 GR12 gives the item its full container range).
        if (!item.Signed) return unchecked((Int128)raw);
        // Sign-extend from the pinned width: a set top bit is a negative two's-complement value.
        if (n >= 16) return unchecked((Int128)raw);
        UInt128 span = UInt128.One << (8 * n);
        return raw >= (span >> 1) ? (Int128)raw - (Int128)span : (Int128)raw;
    }

    private static UInt128 Mask(UInt128 v, int bytes) =>
        bytes >= 16 ? v : v & ((UInt128.One << (8 * bytes)) - 1);

    // ── PACKED-DECIMAL (radix 10 BCD, §13.18.60.4 GR11) ───────────────────────────────────────────────────────
    // Two digits per byte, most significant first, zero-padded on the left. NumericByteForm.Packed reserves
    // the LOW nibble of the last byte for the sign — 0xC positive, 0xD negative, 0xF for an item with no
    // operational sign (the IBM / Micro Focus / GnuCOBOL convention); NumericByteForm.PackedNoSign is the 2023
    // WITH NO SIGN form, which "reserves no storage for representing any sign value" (GR11), so every nibble is a
    // digit. The two forms can occupy the SAME number of bytes at an odd digit count — 3 digits is 2 bytes either
    // way — which is why the form, never the width, decides whether a sign nibble is present.

    private const byte SignPositive = 0x0C;
    private const byte SignNegative = 0x0D;
    private const byte SignUnsigned = 0x0F;

    private static string FormatPackedImage(Int128 unscaled, in NumProfile item)
    {
        int n = Width(item);
        bool hasSignNibble = item.ByteForm is NumericByteForm.Packed;
        int digitNibbles = 2 * n - (hasSignNibble ? 1 : 0);
        bool negative = item.Signed && unscaled < 0;
        Int128 mag = unscaled < 0 ? -unscaled : unscaled;
        if (item.Digits > 0) mag %= Pow10Wide(item.Digits);   // the picture's digit capacity, as the zoned form does

        var nibbles = new byte[2 * n];
        for (int i = digitNibbles - 1; i >= 0; i--)
        {
            nibbles[i] = (byte)(mag % 10);
            mag /= 10;
        }
        if (hasSignNibble)
            nibbles[^1] = item.Signed ? (negative ? SignNegative : SignPositive) : SignUnsigned;

        var chars = new char[n];
        for (int i = 0; i < n; i++) chars[i] = (char)((nibbles[2 * i] << 4) | nibbles[2 * i + 1]);
        return new string(chars);
    }

    private static Int128 ParsePackedImage(string image, in NumProfile item)
    {
        int n = Width(item);
        bool hasSignNibble = item.ByteForm is NumericByteForm.Packed;
        int take = image is null ? 0 : Math.Min(n, image.Length);
        int digitNibbles = 2 * take - (hasSignNibble ? 1 : 0);

        Int128 mag = 0;
        for (int i = 0; i < digitNibbles; i++)
        {
            int nib = (i % 2 == 0 ? image![i / 2] >> 4 : image![i / 2]) & 0x0F;
            // A non-decimal nibble is incompatible data (§14.6.13.2, undefined) — contribute no digit, exactly
            // as the zoned decoder ignores a non-digit character, so the decode stays deterministic.
            if (nib <= 9) mag = mag * 10 + nib;
        }
        if (!hasSignNibble || take == 0) return mag;
        // The IBM sign-nibble reading every packed producer agrees on: 0xB and 0xD are negative, everything else
        // (0xA, 0xC, 0xE, 0xF, and a digit written by a careless producer) is positive.
        int sign = image![take - 1] & 0x0F;
        return sign is 0x0B or 0x0D ? -mag : mag;
    }

    /// <summary>The pinned byte width a binary/packed form lays out. Zero would mean the profile claims a byte
    /// form without a width — a construction bug (<c>NumericByteFormDriftTests</c> pins the pairing), so it
    /// fails loud here rather than silently producing an empty image.</summary>
    private static int Width(in NumProfile item) =>
        item.StorageLength > 0 ? item.StorageLength : throw NoByteImage(item);
}
