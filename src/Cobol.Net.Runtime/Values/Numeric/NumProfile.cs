// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// How a numeric item's capacity (the SIZE ERROR boundary) is defined. COBOL distinguishes three
/// overflow disciplines that <see cref="UsageKind"/> alone conflates (ISO §8.5.1.2 / §13.18.60).
/// </summary>
public enum NumericTruncation
{
    /// <summary>Overflow when the stored-digit count exceeds the PICTURE digit count. DISPLAY, COMP/COMPUTATIONAL,
    /// and BINARY all truncate by digit count: <c>PIC 99 COMP</c> holds 0–99, not 0–32767.</summary>
    DigitCount = 0,
    /// <summary>Overflow when the stored-digit count exceeds the packed-decimal capacity
    /// <c>2 × byteLength − 1</c>. COMP-3 / PACKED-DECIMAL.</summary>
    PackedDecimal = 1,
    /// <summary>Overflow when the value leaves the native two's-complement range of the byte width.
    /// COMP-5 / COMPUTATIONAL-5 (and the COBOL-2002 BINARY-* usages, which lower to COMP-5):
    /// <c>PIC 9(4) COMP-5</c> holds 0–65535, <c>PIC S9(4) COMP-5</c> holds −32768–32767.</summary>
    BinaryCapacity = 2,
}

/// <summary>
/// The BYTE REPRESENTATION a numeric item's storage takes — the one fact that decides what the item occupies at
/// every byte boundary: the record/group character image, a file record, a SORT key window and the REDEFINES
/// backing (COBOLNET_DESIGN §14.4). <see cref="NumericTruncation"/> cannot serve this role and never could: it
/// is the CAPACITY discipline, and DISPLAY and BINARY share <see cref="NumericTruncation.DigitCount"/> while
/// occupying entirely different bytes.
/// <para>
/// ISO/IEC 1989:2023 §13.18.60.4 leaves each of these representations to the implementor and §4.2.16 obliges us
/// to document the choice (Annex A.1 items 205 and 215 make USAGE BINARY's and USAGE PACKED-DECIMAL's "computer
/// storage allocation, alignment and representation of data" REQUIRED user-documentation items). These members
/// ARE that documentation, and the width they imply is <c>PicInfo.StorageWidth</c> — the SAME width
/// <c>FUNCTION BYTE-LENGTH</c> reports (§15.14.4 r1) and the SAME width the item occupies in a group's
/// character image (§15.50.4 r3): one width, one representation, everywhere.
/// </para>
/// </summary>
public enum NumericByteForm
{
    /// <summary>NO byte representation — the item never reaches a character image or a file record, so a codec
    /// that is handed one must reject it LOUDLY rather than invent bytes. No shipping numeric usage carries it
    /// any more (USAGE INDEX was the last, until the R40 owner decision pinned its 8-byte occurrence-number
    /// image — kb/Work PB164): it remains the value every NON-profile usage reads and the guard a NEW usage
    /// hits until somebody decides its representation. Value 0, so an unstated byte form fails loud instead of
    /// silently claiming to be DISPLAY.</summary>
    None = 0,

    /// <summary>USAGE DISPLAY (ISO §13.18.60.4 GR7 — "an alphanumeric coded character set shall be used to
    /// represent a data item … aligned on a character boundary"): ONE BYTE PER DIGIT POSITION, most significant
    /// first, the implied decimal point occupying no position, the sign carried per <see cref="NumProfile.SignKind"/>
    /// (an over-punch on the first/last digit, or a separate leading/trailing <c>+</c>/<c>-</c> byte that DOES
    /// occupy a position, §13.18.52).</summary>
    Zoned = 1,

    /// <summary>USAGE BINARY / COMPUTATIONAL / COMP-5 / BINARY-CHAR..DOUBLE (ISO §13.18.60.4 GR4 "a radix of 2 is
    /// used", GR6, GR12): a two's-complement integer of the UNSCALED value in exactly
    /// <see cref="NumProfile.StorageLength"/> bytes, MOST SIGNIFICANT BYTE FIRST (big-endian). The width table is
    /// pinned by digit count — 1-2-4-8 bytes for 1-2 / 3-4 / 5-9 / 10-18 digits — and the fixed-width usages own
    /// their width directly (GR12). Big-endian is the implementor choice §13.18.60.4 GR4 asks for; it is what
    /// IBM, Micro Focus and GnuCOBOL all write for USAGE BINARY, so a data file interchanges.</summary>
    Binary = 2,

    /// <summary>USAGE PACKED-DECIMAL / COMP-3 (ISO §13.18.60.4 GR11 — "a radix of 10 … each digit position shall
    /// occupy the minimum possible configuration"): binary-coded decimal, TWO DIGITS PER BYTE, most significant
    /// first, with a TRAILING SIGN NIBBLE in the low half of the last byte (<c>0x0C</c> positive, <c>0x0D</c>
    /// negative, and <c>0x0F</c> for an unsigned item's implied positive). The digit count is padded with a
    /// leading zero nibble when even, giving <c>Digits / 2 + 1</c> bytes.</summary>
    Packed = 3,

    /// <summary>USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.4 GR11, a COBOL-2023 addition — "the
    /// representation of the data item in the storage of the computer reserves no storage for representing any
    /// sign value"): <see cref="Packed"/> without the trailing sign nibble, so <c>ceil(Digits / 2)</c> bytes with
    /// a leading pad nibble when the digit count is odd. SR31 forbids an <c>S</c> in the picture; the value is
    /// "always considered to have a zero, or positive value".</summary>
    PackedNoSign = 4,

    /// <summary>COMP-1 / FLOAT-SHORT / FLOAT-BINARY-32: the IEEE 754 binary32 interchange FORMAT, 4 bytes
    /// (kb/Work PB164 wave 2 — the byte-form pin that admits a float leaf to group images/records). The FORMAT
    /// and the BYTE ORDER are separate spec channels: §13.18.60.4 GR14 pins FLOAT-BINARY-32 to the binary32
    /// interchange format (ISO/IEC 60559) while GR13/GR21 leave COMP-1/FLOAT-SHORT to the implementor — one
    /// form serves both. Byte order: §13.18.60.4 GR19 (HIGH-ORDER-LEFT = big-endian, HIGH-ORDER-RIGHT =
    /// little-endian; for the STANDARD usages the implied phrase comes from §11.9.8, whose SR1 makes the
    /// no-clause default OUR documented choice — HIGH-ORDER-LEFT, Annex A.1 item 48), carried per profile by
    /// <see cref="NumProfile.FloatLittleEndian"/>. The implementor-defined usages (COMP-1/FLOAT-SHORT) are
    /// PINNED big-endian, matching <see cref="Binary"/>'s byte order so a record interchanges.</summary>
    Ieee32 = 5,

    /// <summary>COMP-2 / FLOAT-LONG / FLOAT-EXTENDED / FLOAT-BINARY-64: the IEEE 754 binary64 interchange
    /// FORMAT, 8 bytes (kb/Work PB164 wave 2; FLOAT-EXTENDED maps to binary64 — the documented GR13 subset
    /// nesting, no .NET quad). Format pinned by §13.18.60.4 GR15 for FLOAT-BINARY-64, implementor-chosen
    /// (GR13/GR21) for the rest; byte order per GR19 + §11.9.8 exactly as <see cref="Ieee32"/> documents —
    /// big-endian unless <see cref="NumProfile.FloatLittleEndian"/>.</summary>
    Ieee64 = 6,
}

/// <summary>
/// WHERE a signed numeric item presents its sign in its DISPLAY image (ISO §13.18.52 SIGN / §8.5.1.2): fused onto
/// the leading or trailing digit position, or in a separate leading/trailing character position. For binary/packed
/// usages the DISPLAY image carries a leading minus only when negative.
/// <para>WHICH characters a fused sign uses is the ORTHOGONAL axis <see cref="NumProfile.SignEncoding"/> carries
/// (kb/Work PB803) — conflating the two is what would make a convention option a per-position enum explosion.</para>
/// </summary>
public enum NumericSign
{
    /// <summary>USAGE DISPLAY default: the sign is over-punched onto the LAST digit. WHICH characters the punch
    /// uses is the orthogonal axis <see cref="NumProfile.SignEncoding"/> carries (kb/Work PB803) — this member is
    /// the POSITION only.</summary>
    TrailingOverpunch = 0,
    /// <summary>USAGE DISPLAY, SIGN LEADING (no SEPARATE): over-punched onto the first digit.</summary>
    LeadingOverpunch = 1,
    /// <summary>USAGE DISPLAY, SIGN LEADING SEPARATE: a leading <c>+</c>/<c>-</c> character (always present).</summary>
    LeadingSeparate = 2,
    /// <summary>USAGE DISPLAY, SIGN TRAILING SEPARATE: a trailing <c>+</c>/<c>-</c> character (always present).</summary>
    TrailingSeparate = 3,
    /// <summary>Binary/packed (COMP/COMP-3/COMP-5): a leading <c>-</c> only when negative; positive/zero is bare.</summary>
    BinaryMinus = 4,
}

/// <summary>
/// The compact, runtime-facing numeric profile of a COBOL data item: just enough to scale, round and bound-check
/// a value, and to lay it out at a byte boundary. The WiseOwl COBOL compiler builds it directly from a <c>PicInfo</c>
/// (digits, scale, sign, usage→capacity discipline, usage→byte form) and threads it into every numeric store
/// so arithmetic obeys the receiver's PICTURE+USAGE (truncation / ROUNDED / SIZE ERROR) and into the record-image
/// codec so the item occupies its true bytes (COBOLNET_DESIGN §14.4).
/// <para><b>Three orthogonal axes, and conflating any two is a defect:</b> <see cref="Truncation"/> is the
/// CAPACITY discipline (where SIZE ERROR bites), <see cref="ByteForm"/> is the BYTE REPRESENTATION (what the
/// item occupies in a record), and <see cref="SignKind"/> is the SIGN PRESENTATION. DISPLAY and BINARY share one
/// truncation discipline and differ entirely in representation — which is precisely how the record image came to
/// disagree with <c>FUNCTION BYTE-LENGTH</c> (V59).</para>
/// </summary>
public readonly record struct NumProfile
{
    /// <summary>Count of digit positions in the PICTURE (the '9' count; integer + fraction).</summary>
    public required int Digits { get; init; }

    /// <summary>The net signed fraction scale: V-fraction digits, plus leading-P positions, minus trailing-P
    /// positions (ISO §13.18.40). MAY BE NEGATIVE — a trailing-P item (e.g. <c>99P</c>) stores digits that are
    /// multiples of 10^|scale|; leading P (e.g. <c>P(4)9</c>) puts the point left of every digit. The runtime rescales
    /// to this scale natively (a single signed scale is the one canonical representation — no separate P fields).</summary>
    public required int FractionDigits { get; init; }

    /// <summary>Whether the item carries an operational sign (PIC S or a SIGN clause).</summary>
    public required bool Signed { get; init; }

    /// <summary>How the sign is represented in the DISPLAY image (only consulted when <see cref="Signed"/>).</summary>
    public NumericSign SignKind { get; init; }

    /// <summary>The OVER-PUNCH CONVENTION this item's compiled program uses (kb/Work PB803, owner decision
    /// 2026-09-09; Annex A.1 items 177 and 178, ISO §13.18.52.4 GR4 / GR5 b). Consulted only for an
    /// over-punched sign — <see cref="NumericSign.TrailingOverpunch"/> / <see cref="NumericSign.LeadingOverpunch"/>
    /// — since §13.18.52.4 GR6 b) pins the SEPARATE signs to '+'/'-' with no latitude. Orthogonal to
    /// <see cref="SignKind"/>, which is the sign's POSITION: the four combinations are all reachable.
    /// <para><see cref="Runtime.SignEncoding.Ibm"/> (value 0) is the default and the documented one, so a profile
    /// that does not state it — including the runtime's own hand-built decoders, which are all unsigned — is the
    /// IBM/Micro Focus convention, and the emitted profile of every default compile is byte-identical to before the
    /// option existed.</para></summary>
    public SignEncoding SignEncoding { get; init; }

    /// <summary>Which capacity discipline bounds the value (the SIZE ERROR boundary).</summary>
    public required NumericTruncation Truncation { get; init; }

    /// <summary>The item's BYTE REPRESENTATION — what it occupies in a record image, a file record, a SORT key
    /// window and a REDEFINES backing. <c>required</c> deliberately: it cannot be inferred from
    /// <see cref="Truncation"/> (DISPLAY and BINARY are both <see cref="NumericTruncation.DigitCount"/>), and a
    /// profile built without stating it would silently claim to be one byte per digit. Every profile the compiler
    /// emits states it; the runtime's own hand-built profiles are character-image decoders and state
    /// <see cref="NumericByteForm.Zoned"/>.</summary>
    public required NumericByteForm ByteForm { get; init; }

    /// <summary>The effective FLOAT-BINARY endianness for a STANDARD binary floating-point item
    /// (<see cref="NumericByteForm.Ieee32"/>/<see cref="NumericByteForm.Ieee64"/> under USAGE
    /// FLOAT-BINARY-32/-64): <c>true</c> = HIGH-ORDER-RIGHT, the item's interchange bytes are LITTLE-endian
    /// (ISO §13.18.60.4 GR19b); <c>false</c> = HIGH-ORDER-LEFT / big-endian — GR19a, and the documented
    /// implied-phrase default when no OPTIONS FLOAT-BINARY clause is specified (§11.9.8.3 SR1 makes that
    /// default the implementor's REQUIRED-documented choice, Annex A.1 item 48). Always <c>false</c> for the
    /// implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED, GR13/GR21 — pinned
    /// big-endian regardless of the clause, which speaks only to the standard usages, GR19c). Set at bind by
    /// <c>PicInfo.FloatItem</c> — the ONE place the §11.9.8 implied-phrase rule is applied.</summary>
    public bool FloatLittleEndian { get; init; }

    /// <summary>Storage width in bytes — used for <see cref="NumericTruncation.PackedDecimal"/> capacity
    /// (2n−1 digits) and <see cref="NumericTruncation.BinaryCapacity"/> two's-complement range, and it is the
    /// EXACT width <see cref="NumericByteForm.Binary"/> / <see cref="NumericByteForm.Packed"/> /
    /// <see cref="NumericByteForm.PackedNoSign"/> lay out. Zero for <see cref="NumericByteForm.Zoned"/>,
    /// whose width is <see cref="Digits"/> plus a separate-sign position (the digit run IS its own byte form).</summary>
    public int StorageLength { get; init; }

    /// <summary>The signed fractional scale a value is rescaled/rounded to when stored into this item — the net
    /// <see cref="FractionDigits"/> (V fraction + leading P − trailing P). May be negative; <see cref="CobolNum.Rescale"/>
    /// handles a negative scale natively.</summary>
    public int FractionScale => FractionDigits;

    /// <summary>True when this item's BYTE IMAGE can hold a value the signed <see cref="Int128"/> decode lane cannot
    /// represent: an UNSIGNED 16-byte radix-2 image, whose [0, 2^128) container range (kb/Work R10 — the item
    /// owns its full container) reaches past <see cref="Int128.MaxValue"/>. <see cref="CobolNum.ParseImage"/> returns such a value
    /// reinterpreted — bit-identical but NEGATIVE at or above 2^127 — so a reader that ORDERS decoded values (a
    /// SORT/MERGE key column) must take the <see cref="CobolNum.ParseImageU128"/> lane instead (kb/Work PB186). The
    /// runtime twin of the compiler's <c>PicInfo.IsUnsignedWideBinary</c>, stated over the image rather than the
    /// carrier: a narrower unsigned binary image fits Int128 whole, and a signed one is ordered by its sign.</summary>
    public bool ImageExceedsInt128 => ByteForm == NumericByteForm.Binary && !Signed && StorageLength >= 16;
}
