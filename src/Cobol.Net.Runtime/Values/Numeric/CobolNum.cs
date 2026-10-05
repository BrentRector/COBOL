// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The value-level numeric engine for WiseOwl COBOL, operating entirely on hardware-native <see cref="long"/>.
/// </summary>
/// <remarks>
/// <para>A COBOL fixed-point datum is represented as a native integer holding its <b>unscaled</b> value (every
/// digit, with the decimal point implied by a compile-time scale) — exactly the COBOL definition of fixed-point.
/// So all arithmetic is native integer math; nothing on an arithmetic path uses the software <c>decimal</c> or
/// <c>BigInteger</c> types (the two cold exceptions are BASECONVERT's digit accumulation and the exact expansion of
/// a binary64 PAST the Int128 carrier in a truncating landing — <c>CobolFloat.LowOrderDigits</c>, kb/Work PB77).
/// (Pictures wider than 18 digits — COBOL-2002+ allows 31–38 — use a fixed-size <c>Int128</c> value-type escape
/// hatch (far cheaper than <c>BigInteger</c>), added when a program needs it; <c>COMP-1</c>/<c>COMP-2</c> are
/// <c>float</c>/<c>double</c> and bypass this engine.)</para>
/// <para>COBOL arithmetic operates on the algebraic VALUE of operands regardless of representation (ISO §8.8.1):
/// the compiler aligns operand scales, does the native integer op, and hands the result here with the receiver's
/// <see cref="NumProfile"/>; this rescales to the receiver's scale (rounding by one of the eight modes), truncates
/// any high-order digits beyond the picture, and applies the unsigned-magnitude rule. Representation (DISPLAY /
/// COMP / COMP-3 / COMP-5) changes only the capacity discipline and the external byte image — not the value.</para>
/// </remarks>
public static partial class CobolNum
{
    /// <summary>
    /// Rescale an unscaled integer from <paramref name="fromScale"/> to <paramref name="toScale"/> fractional
    /// digits: widening multiplies by a power of ten (exact); narrowing divides, rounding with <paramref name="mode"/>.
    /// </summary>
    public static Int128 Rescale(Int128 value, int fromScale, int toScale, CobolRounding mode)
    {
        if (toScale == fromScale) return value;
        if (toScale > fromScale) return value * Pow10Wide(toScale - fromScale);
        return RoundDiv(value, Pow10Wide(fromScale - toScale), mode);
    }

    /// <summary>Whether the EXACT widening <paramref name="value"/> × 10^<paramref name="up"/> is representable
    /// in the Int128 carrier — the ONE test the three widening siblings share
    /// (<see cref="RescaleEscape"/>, <see cref="RescaleChecked"/>, <see cref="TryStore(Int128, int, in NumProfile, CobolRounding, out Int128)"/>).
    /// <para>⛔ kb/Work PB639: it is also the test that decides whether a CHECKED store may look at the aligned
    /// value at all. <see cref="Rescale"/>'s widening multiply wraps in the carrier, and a wrap can land back
    /// INSIDE the receiver's capacity — <c>COMPUTE WC = 340282366920938463463374607432 ON SIZE ERROR</c> into
    /// <c>PIC S9(9)V9(9)</c> (a legal 30-digit literal, §8.3.3.3.2) wrapped to 0.231788544, passed the capacity
    /// check, ran NOT ON SIZE ERROR and overwrote the receiver where §14.7.5 storing rule 2 requires it
    /// unchanged. The size error condition is a fact about the ALGEBRAIC result ("further from zero than
    /// permitted for the associated resultant data item", §14.7.5 case 3), never about a wrapped carrier.</para>
    /// <para>Returning false is a size error for every receiver whose capacity the carrier can express: the
    /// widest decimal capacity is 10^38 − 1 and every binary container up to a SIGNED 16-byte one is inside
    /// <see cref="Int128"/>, so a value the carrier cannot form at <paramref name="up"/> exceeds them all.
    /// (The same argument <c>CobolDec.ToUnscaledChecked</c> makes for the SDIDI lane, kb/Work PB74.) The one
    /// container reaching past the carrier is an UNSIGNED 16-byte <c>COMP-5</c>, whose top half lies above the
    /// documented native intermediate (<c>CONFORMANCE.md</c> DOC-A.1-123: a scaled <see cref="Int128"/>,
    /// magnitude bound ≈ 1.7 × 10^38). An alignment landing there is the §14.7.5 case-5 INTERMEDIATE range
    /// condition that item 179 (1) already documents as CHECKED, so false is the right answer there too — and
    /// it is the answer <see cref="RescaleEscape"/> gives on the value-semantics side. What it must never be is
    /// a wrap: before kb/Work PB639 such a store kept |value − 2^128|, silently.</para>
    /// <para>MinValue-safe by construction: the bound is written on the SIGNED value, never through
    /// <c>Int128.Abs</c>, which throws on <see cref="Int128.MinValue"/>.</para></summary>
    internal static bool WideningFits(Int128 value, int up)
    {
        if (up <= 0 || value == 0) return true;
        if (up > 38) return false;                       // 10^39 is past the carrier for every nonzero value
        Int128 f = Pow10Wide(up);                        // f ≥ 10, so both quotients are representable
        return value <= Int128.MaxValue / f && value >= Int128.MinValue / f;
    }

    /// <summary>The size-error-CHECKED sibling of <see cref="Rescale"/> for a numeric-EDITED receiver's final
    /// transfer: under <see cref="CobolRounding.Prohibited"/> an inexact narrowing throws
    /// <see cref="CobolSizeError"/> (ISO §14.7.4.3 r7), and a WIDENING the carrier cannot form throws the
    /// §14.7.5 case-3 size error ("if, after radix point alignment and any applicable rounding specifications,
    /// the result of an arithmetic statement is further from zero than permitted for the associated resultant
    /// data item") — the caller's ON SIZE ERROR / EC-SIZE machinery catches either and leaves the receiver
    /// UNCHANGED. Mirror of the numeric path's <see cref="TryStore(Int128, int, in NumProfile, CobolRounding, out Int128)"/>
    /// and the standard-decimal <c>CobolDec.ToUnscaledChecked</c>, so all three receiver categories agree.
    /// Every other mode rescales normally (a rounding mode is never a size error by rescale).
    /// <para>⛔ kb/Work PB639: the widening arm was the plain <see cref="Rescale"/>, so the alignment wrapped and
    /// <c>CobolEdit.TryFormat</c> then capacity-checked the WRAPPED value — a no-op guard on exactly the
    /// magnitudes it exists to catch.</para></summary>
    public static Int128 RescaleChecked(Int128 value, int fromScale, int toScale, CobolRounding mode)
    {
        if (mode == CobolRounding.Prohibited && IsInexactAtScale(value, fromScale, toScale))
            throw new CobolSizeError("ROUNDED MODE IS PROHIBITED on an inexact transfer to an edited receiver "
                + "(ISO §14.7.4.3 r7 — EC-SIZE-TRUNCATION; the receiver is left unchanged)", "EC-SIZE-TRUNCATION");
        if (!WideningFits(value, toScale - fromScale))
            throw new CobolSizeError($"radix point alignment to {toScale} fraction digits is further from zero "
                + "than permitted for the receiving data item (ISO §14.7.5 case 3 — EC-SIZE-TRUNCATION; the "
                + "receiver is left unchanged)", "EC-SIZE-TRUNCATION");
        return Rescale(value, fromScale, toScale, mode);
    }

    /// <summary>
    /// Store an arithmetic result (the unscaled integer <paramref name="value"/> at <paramref name="valueScale"/>)
    /// into the receiver: rescale to the receiver's scale (rounding with <paramref name="mode"/>), drop any
    /// high-order digits beyond the picture (the no-ON-SIZE-ERROR behavior), and apply the unsigned-magnitude rule
    /// for an unsigned receiver (ISO §14.9.25.4 GR6d2b). Returns the receiver's stored unscaled integer.
    /// <para>This IS the determination <c>CONFORMANCE.md</c> DOC-A.1-70 documents for a §14.7.5 case-3 size error
    /// with no SIZE ERROR phrase and EC-SIZE checking off (§14.7.5 no-phrase rule 4 names the condition;
    /// §14.6.13.1.3 item 8 hands the disposition to the implementor): "execution continues and the resultant
    /// identifier receives the LOW-ORDER digits of the result aligned at its scale". It is the same rule for a
    /// MOVE, whose §14.6.8.2 r4 alignment is "transferred to the receiving digits with zero fill or truncation on
    /// either end".</para>
    /// <para>⛔ kb/Work PB639 — WHY THE WIDENING IS SPLIT BY CAPACITY DISCIPLINE. "Low-order digits" is a DECIMAL
    /// statement for a DISPLAY/COMP/PACKED receiver and the container's two's-complement residue for a
    /// BinaryCapacity one; neither is "the Int128 carrier's binary wrap, then a decimal mod". Aligning through the
    /// plain <see cref="Rescale"/> and only THEN taking the decimal mod composed the two:
    /// <c>COMPUTE WC = 1000000000000000000000000000000</c> into <c>PIC S9(9)V9(9)</c> stored
    /// −123822295.304634368 — the low digits of 10^39 wrapped modulo 2^128 — where the result's low-order 18
    /// digits are all zero. <see cref="RescaleStoreCap"/> drops the digits a ≤38-digit store could never use
    /// BEFORE the multiply, so the composition is exact: 10^D divides 10^38 for every D ≤ 38, hence
    /// (v·10^s mod 10^38) mod 10^D = v·10^s mod 10^D. The BinaryCapacity arm keeps <see cref="Rescale"/>, whose
    /// carrier wrap IS modulo 2^128 and therefore already exact modulo the container's 2^bits.
    /// (<see cref="StoreU"/> has had this split — its cap argument — since R10; this was the unfixed arm.)</para>
    /// </summary>
    public static Int128 Store(Int128 value, int valueScale, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation)
    {
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
            // Native two's-complement width (COMP-5 / BINARY-CHAR family): 2^bits divides the carrier's own
            // 2^128 wrap, so the aligned residue is exact without a decimal cap.
            return WrapBinary(Rescale(value, valueScale, receiver.FractionScale, mode), receiver);
        Int128 v = RescaleStoreCap(value, valueScale, receiver.FractionScale, mode);
        v %= Pow10Wide(receiver.Digits);      // high-order digit truncation (DISPLAY / COMP / BINARY)
        return receiver.Signed ? v : Int128.Abs(v);
    }

    /// <summary>
    /// Store an arithmetic result with SIZE ERROR checking (ISO §14.7.5 phase-b): rescale to the receiver scale
    /// (rounding with <paramref name="mode"/>) and bounds-check against the picture before committing. Returns
    /// <c>false</c> — leaving <paramref name="stored"/> meaningless and the receiver to be left unchanged by the
    /// caller — when (a) the rescaled value's magnitude exceeds the receiver's digit capacity (high-order overflow,
    /// §14.7.5 case 3), or (b) <paramref name="mode"/> is <see cref="CobolRounding.Prohibited"/> and the rescale is
    /// inexact (a nonzero fraction would be dropped, §14.7.4.3 r7). Otherwise stores the value (unsigned-magnitude
    /// rule applied, §14.9.25.4 GR6d2b) and returns <c>true</c>. This is the checked sibling of <see cref="Store"/>, used
    /// only when an ON SIZE ERROR phrase is present.
    /// <para>⛔ kb/Work PB639: the capacity check must never look at a WRAPPED alignment. The widening multiply
    /// in <see cref="Rescale"/> wraps in the carrier, and a wrap can land back inside the receiver's capacity, so
    /// the guard silently passed exactly the magnitudes it exists to catch — see <see cref="WideningFits"/>,
    /// which is now consulted first.</para>
    /// </summary>
    public static bool TryStore(Int128 value, int valueScale, in NumProfile receiver, CobolRounding mode, out Int128 stored)
    {
        stored = 0;
        // PROHIBITED: an inexact rescale is a size error regardless of capacity (§14.7.4.3 r7) — receiver unchanged.
        if (mode == CobolRounding.Prohibited && IsInexactAtScale(value, valueScale, receiver.FractionScale))
            return false;

        // §14.7.5 case 3 on the ALIGNMENT itself: a widening the Int128 carrier cannot form is further from zero
        // than ANY fixed-point receiver permits (kb/Work PB639) — storing rule 2 leaves the receiver unchanged.
        if (!WideningFits(value, receiver.FractionScale - valueScale)) return false;

        Int128 v = Rescale(value, valueScale, receiver.FractionScale, mode);

        // BinaryCapacity (COMP-5 / BINARY-CHAR family): the SIZE ERROR boundary is the native two's-complement
        // range of the byte width (ISO §13.18.60.4 GR12) — signed [-2^(bits-1), 2^(bits-1)), unsigned magnitude
        // [0, 2^bits) (§14.9.25.4 GR6d2b for the unsigned-magnitude rule).
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
        {
            if (!InBinaryRange(v, receiver)) return false;
            stored = receiver.Signed ? v : Int128.Abs(v);
            return true;
        }

        // High-order capacity check by digit count (DISPLAY/COMP/BINARY). The compare avoids
        // Math.Abs(long.MinValue) by bounding both signs against the positive limit.
        Int128 limit = Pow10Wide(receiver.Digits);   // Digits ≤ 38 within the wide engine
        if (v >= limit || v <= -limit) return false;

        // Unsigned receiver stores the magnitude (§14.9.25.4 GR6d2b); v is bounded (it passed the capacity check),
        // so Math.Abs is safe.
        stored = receiver.Signed ? v : Int128.Abs(v);
        return true;
    }

    /// <summary>Store a STANDARD-DECIMAL intermediate (§8.8.1.5) into the receiver: the §14.7 final transfer —
    /// the SDIDI rescales to the receiver's scale with the statement's ROUNDED <paramref name="mode"/>, then the
    /// normal capacity rules apply.</summary>
    public static Int128 Store(CobolDec value, in NumProfile receiver, CobolRounding mode = CobolRounding.Truncation)
    {
        Int128 v = value.ToUnscaled(receiver.FractionScale, mode);
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
            return WrapBinary(v, receiver);
        v %= Pow10Wide(receiver.Digits);
        return receiver.Signed ? v : Int128.Abs(v);
    }

    /// <summary>The SIZE-ERROR-checked sibling of the SDIDI store: false on capacity overflow or a
    /// PROHIBITED-inexact transfer (receiver to be left unchanged by the caller).</summary>
    public static bool TryStore(CobolDec value, in NumProfile receiver, CobolRounding mode, out Int128 stored)
    {
        stored = 0;
        Int128 v;
        // The CHECKED transfer (kb/Work PB74): a magnitude past the Int128 carrier is a size error here, never the
        // low-order digits the unchecked ToUnscaled keeps — those passed the capacity check below as 0 and stored,
        // so COMPUTE X5 = 10 ** 100 ON SIZE ERROR ran NOT ON SIZE ERROR under STANDARD-DECIMAL.
        try { v = value.ToUnscaledChecked(receiver.FractionScale, mode); }
        catch (CobolSizeError) { return false; }   // PROHIBITED-inexact transfer (§14.7.4.3 r7) or the §14.7.5 case-3 overflow
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
        {
            if (!InBinaryRange(v, receiver)) return false;
            stored = receiver.Signed ? v : Int128.Abs(v);
            return true;
        }
        Int128 limit = Pow10Wide(receiver.Digits);
        if (v >= limit || v <= -limit) return false;
        stored = receiver.Signed ? v : Int128.Abs(v);
        return true;
    }

    /// <summary>⛔ THE RAISING FINAL TRANSFER — the store for a receiver whose statement has EC-SIZE checking
    /// enabled but NO <c>ON SIZE ERROR</c> flag to latch, because the store is not inside an arithmetic
    /// statement at all (kb/Work PB640). The two dispositions of a §14.7.5 case-3 overflow are already written
    /// once each: <see cref="Store(Int128, int, in NumProfile, CobolRounding)"/> is the checking-OFF one
    /// (<c>CONFORMANCE.md</c> DOC-A.1-70 — the result's LOW-ORDER digits) and
    /// <see cref="TryStore(Int128, int, in NumProfile, CobolRounding, out Int128)"/> is the phrase-driven one
    /// (false ⇒ the receiver is left unchanged). This is the THIRD caller shape, and it is an expression:
    /// §14.7.5's no-phrase rule 4 — "if the result of the arithmetic statement is a value further from zero
    /// than permitted for the associated resultant data item, the EC-SIZE-TRUNCATION exception condition is set
    /// to exist" — with the disposition of §14.6.13.1.3, which for this implementation is a
    /// <see cref="CobolSizeError"/> the enclosing statement's fatal-ambient guard dispatches
    /// (<c>EcEmitter.FatalAmbientGates</c>).
    /// <para>Its live callers are the ACTIVATING element's §14.2.3 GR9/GR10 argument crossings — "if the formal
    /// parameter is numeric, a COMPUTE statement without the ROUNDED phrase" — on the CALL lane (through
    /// <c>CobolArgAdapt.LandForFormal</c>) and the INVOKE lane (<c>OoEmitter</c>'s BY CONTENT arms), which have
    /// no SIZE ERROR phrase to offer and no arithmetic statement to hang one on. Every such crossing is
    /// compiled as this store when EC-SIZE-TRUNCATION checking is enabled at the activating statement and as
    /// the plain <see cref="Store(Int128, int, in NumProfile, CobolRounding)"/> when it is not — the same
    /// compile-time kernel selection the arithmetic store makes.</para></summary>
    public static Int128 StoreOrRaise(Int128 value, int valueScale, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation) =>
        TryStore(value, valueScale, receiver, mode, out Int128 stored) ? stored : throw SizeTruncation(receiver);

    /// <summary>The unsigned-wide lane of <see cref="StoreOrRaise(Int128, int, in NumProfile, CobolRounding)"/>
    /// (kb/Work R10's container-bits contract — named, never overloaded, because an int constant converts
    /// implicitly to both wide types).</summary>
    public static Int128 StoreUOrRaise(UInt128 value, int valueScale, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation) =>
        TryStoreU(value, valueScale, receiver, mode, out Int128 stored) ? stored : throw SizeTruncation(receiver);

    /// <summary>The STANDARD-DECIMAL lane of <see cref="StoreOrRaise(Int128, int, in NumProfile, CobolRounding)"/>
    /// — the §14.7 final transfer of an SDIDI intermediate (§8.8.1.5).</summary>
    public static Int128 StoreOrRaise(CobolDec value, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation) =>
        TryStore(value, receiver, mode, out Int128 stored) ? stored : throw SizeTruncation(receiver);

    /// <summary>The one message for every <see cref="StoreOrRaise(Int128, int, in NumProfile, CobolRounding)"/>
    /// lane (ISO §14.7.5 case 3 + no-phrase rule 4).</summary>
    private static CobolSizeError SizeTruncation(in NumProfile receiver) =>
        new($"the value is further from zero than the {receiver.Digits}-digit receiving item permits "
            + "(ISO §14.7.5 case 3 — EC-SIZE-TRUNCATION)", "EC-SIZE-TRUNCATION");

    /// <summary>Reduce an unscaled value to the native two's-complement range of a BinaryCapacity receiver's
    /// storage width — the deterministic no-ON-SIZE-ERROR truncation for COMP-5 / the BINARY-CHAR family (the
    /// width analog of the DigitCount path's <c>%= 10^Digits</c>). A signed receiver folds by modulo 2^bits into
    /// [-2^(bits-1), 2^(bits-1)) (exactly a native sbyte/short/int/long cast); an unsigned receiver stores the
    /// magnitude (ISO §14.9.25.4 GR6d2b) reduced modulo 2^bits into [0, 2^bits). ISO §13.18.60.4 GR12/GR21.
    /// <para>⛔ THE 16-BYTE TIER RETURNS CONTAINER BITS (kb/Work R10, F74). A C# <c>Int128</c> shift count is
    /// masked to 7 bits, so the old <c>1 &lt;&lt; 128</c> silently produced modulus 1 and every 16-byte store
    /// collapsed to 0. At bits = 128 the container range equals (unsigned: doubles) the <c>Int128</c> domain, so
    /// the residue is returned as the container's TWO'S-COMPLEMENT BITS: an unsigned 16-byte receiver's stored
    /// value in [2^127, 2^128) lands with its high bit in the sign position, and the emitted store's
    /// <c>unchecked((UInt128))</c> cast reinterprets it exactly. The magnitude of <c>Int128.MinValue</c> (2^127,
    /// which <c>Int128.Abs</c> cannot represent) is exactly its own bit pattern.</para></summary>
    private static Int128 WrapBinary(Int128 value, in NumProfile receiver)
    {
        int bits = 8 * receiver.StorageLength;
        if (bits >= 128)
            return receiver.Signed ? value                          // Int128 IS the signed 16-byte container
                 : value == Int128.MinValue ? value                  // |MinValue| = 2^127: its own bit pattern
                 : Int128.Abs(value);                                // magnitude < 2^127: bits = value
        Int128 modulus = (Int128)1 << bits;
        if (!receiver.Signed)
            return Int128.Abs(value) % modulus;
        Int128 m = ((value % modulus) + modulus) % modulus;   // non-negative residue in [0, 2^bits)
        return m >= (modulus >> 1) ? m - modulus : m;          // fold the high half to the negative range
    }

    /// <summary>Whether a value fits the native two's-complement range of a BinaryCapacity receiver's storage
    /// width — the SIZE ERROR test for COMP-5 / the BINARY-CHAR family: signed [-2^(bits-1), 2^(bits-1));
    /// unsigned magnitude [0, 2^bits) (ISO §13.18.60.4 GR12; §14.9.25.4 GR6d2b for the unsigned-magnitude rule).
    /// At bits = 128 every <c>Int128</c> value is in range — signed because the domains coincide, unsigned
    /// because every magnitude ≤ 2^127 &lt; 2^128 (kb/Work R10, F74 — the shift-mask bug made this leg answer
    /// "[0, 1)" and the signed leg answer "never", so SIZE ERROR fired on every in-range 16-byte store).</summary>
    private static bool InBinaryRange(Int128 value, in NumProfile receiver)
    {
        int bits = 8 * receiver.StorageLength;
        if (bits >= 128) return true;
        Int128 modulus = (Int128)1 << bits;
        if (!receiver.Signed)
            return Int128.Abs(value) < modulus;
        Int128 half = modulus >> 1;
        return value >= -half && value < half;
    }

    // ── The UNSIGNED WIDE lane (kb/Work R10 — owner decision 2026-08-07: unsigned COMP-5 carriers are
    // ulong / UInt128, and the item owns its full container range, ISO §13.18.60.4 GR12). A value in
    // (Int128.MaxValue, 2^128) exists ONLY in a 16-byte unsigned BinaryCapacity item; it enters generated code
    // as a UInt128-typed expression (the item's field read, or the HIGHEST-ALGEBRAIC fold literal §15.43.4 r2),
    // and the EMITTER routes it here by the operand's static carrier (NumX.U) — the U-suffixed names are
    // deliberate, see FormatDisplayU's remark on the int-constant CS0121 ambiguity an overload pair causes.
    // The VALUE-preserving paths (store, display, compare) take the full range; the ARITHMETIC engine's
    // intermediate stays Int128 (documented, CONFORMANCE.md §4.2.16), so an operand beyond it goes through
    // Widen, which raises the size-error condition rather than wrapping. ─────────────────────────────────────────

    /// <summary>Narrow an unsigned wide value into the native arithmetic intermediate (<see cref="Int128"/> —
    /// the documented §8.8.1.3 native technique). A value beyond <see cref="Int128.MaxValue"/> cannot enter the
    /// intermediate: it raises the size-error condition (EC-SIZE-OVERFLOW; ON SIZE ERROR catches it, without the
    /// phrase it surfaces loud) — never a silent two's-complement wrap to a negative operand.</summary>
    public static Int128 Widen(UInt128 value) =>
        value <= (UInt128)Int128.MaxValue
            ? (Int128)value
            : throw new CobolSizeError("operand " + value + " exceeds the native arithmetic intermediate range "
                + "(Int128 — the documented native technique, ISO §8.8.1.3 / CONFORMANCE.md §4.2.16)", "EC-SIZE-OVERFLOW");

    /// <summary>Store an unsigned wide value (a 16-byte unsigned COMP-5 item's full-container value, or the
    /// HIGHEST-ALGEBRAIC fold of one — §15.43.4 r2) into the receiver: rescale to the receiver's scale, then the
    /// receiver's capacity discipline. The unsigned-magnitude rule (§14.9.25.4 GR6d2b) is a no-op — the sending
    /// value is non-negative. Returns the stored unscaled integer; ⛔ for a 16-byte BinaryCapacity receiver the
    /// return is the container's two's-complement BITS (see <see cref="WrapBinary"/>) — the emitted
    /// <c>unchecked((UInt128))</c> store cast reinterprets exactly.</summary>
    public static Int128 StoreU(UInt128 value, int valueScale, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation)
    {
        bool binary = receiver.Truncation == NumericTruncation.BinaryCapacity;
        UInt128 v = RescaleU(value, valueScale, receiver.FractionScale, mode,
            binary ? BinaryModulusU(8 * receiver.StorageLength) : Pow10U(receiver.Digits));
        if (binary)
        {
            UInt128 m = BinaryModulusU(8 * receiver.StorageLength);
            UInt128 residue = m == 0 ? v : v % m;               // m == 0 encodes modulus 2^128
            return unchecked((Int128)residue);                   // ≤ 8-byte: exact; 16-byte: container bits
        }
        return (Int128)(v % Pow10U(receiver.Digits));            // DigitCount / PackedDecimal: ≤ 10^31 fits Int128
    }

    /// <summary>The SIZE-ERROR-checked sibling of <see cref="StoreU"/>: false on capacity overflow or a
    /// PROHIBITED-inexact transfer (receiver left unchanged by the caller).</summary>
    public static bool TryStoreU(UInt128 value, int valueScale, in NumProfile receiver, CobolRounding mode, out Int128 stored)
    {
        stored = 0;
        if (mode == CobolRounding.Prohibited && valueScale > receiver.FractionScale
            && value % Pow10U(valueScale - receiver.FractionScale) != 0)
            return false;   // inexact narrowing under PROHIBITED (§14.7.4.3 r7)
        // Rescale exactly for the range check: a WIDENING beyond UInt128 already exceeds every capacity below it.
        int up = receiver.FractionScale - valueScale;
        if (up > 0 && value != 0 && (up > 38 || value > UInt128.MaxValue / Pow10U(up))) return false;
        UInt128 v = up > 0 ? value * Pow10U(up)
                  : up < 0 ? RoundDiv(value, Pow10U(-up), mode)
                  : value;
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
        {
            int bits = 8 * receiver.StorageLength;
            // Signed container: v ≤ 2^(bits−1) − 1 (the sending value is non-negative). Unsigned: v < 2^bits;
            // at bits = 128 every UInt128 is in range.
            UInt128 signedMax = ((UInt128)1 << (Math.Min(bits, 128) - 1)) - 1;
            if (receiver.Signed && v > signedMax) return false;
            if (!receiver.Signed && bits < 128 && v >= (UInt128)1 << bits) return false;
            UInt128 m = BinaryModulusU(bits);
            stored = unchecked((Int128)(m == 0 ? v : v % m));
            return true;
        }
        if (v >= Pow10U(receiver.Digits)) return false;
        stored = (Int128)v;
        return true;
    }

    /// <summary>2^bits as a <see cref="UInt128"/> capacity modulus, with <c>0</c> encoding the full 2^128 (which
    /// <see cref="UInt128"/> cannot represent; a C# shift count is masked, so <c>1 &lt;&lt; 128</c> would silently
    /// be 1 — the F74 bug shape this helper exists to fence off).</summary>
    private static UInt128 BinaryModulusU(int bits) => bits >= 128 ? 0 : (UInt128)1 << bits;

    /// <summary>Compare two Int128-lane operands by algebraic VALUE at their own scales (ISO §8.8.4.2.4) —
    /// sign split, then the magnitude compare rides the unsigned overload's non-widening alignment trick.
    /// ⛔ THE REASON THIS EXISTS (fix-queue PB65): aligning both sides to the common scale first widens with an
    /// UNCHECKED multiply, and at (max integer digits) + (max fraction digits) &gt; 38 the widened operand wrapped
    /// SILENTLY — <c>IF BIGV &gt; SMLV</c> over a <c>PIC 9(24)</c> and a <c>PIC 9V9(15)</c>, two legal in-range
    /// items, evaluated FALSE. A comparison has a defined answer for every pair of legal operands, so it must
    /// never widen at all.</summary>
    public static int Compare(Int128 a, int aScale, Int128 b, int bScale)
    {
        if (a < 0 && b >= 0) return -1;
        if (a >= 0 && b < 0) return 1;
        int c = CompareU(a < 0 ? (UInt128)(-a) : (UInt128)a, aScale,
                         b < 0 ? (UInt128)(-b) : (UInt128)b, bScale);
        return a < 0 ? -c : c;
    }

    /// <summary>The <see cref="Rescale"/> sibling whose WIDENING is overflow-checked: a value that cannot be
    /// represented at <paramref name="toScale"/> inside the Int128 intermediate raises the size-error condition
    /// (EC-SIZE-OVERFLOW) instead of wrapping — the D1 escape-boundary policy (kb/Work PB32/PB65: a 39-digit
    /// intermediate is not a computable value, and a silent wrap handed MIN a NEGATIVE result over two positive
    /// arguments). Narrowing rounds exactly as <see cref="Rescale"/> does. Value-semantics consumers (intrinsic
    /// argument alignment, subscripts, comparisons that still align) ride this; the arithmetic store path keeps
    /// <see cref="Rescale"/>'s documented wrap-without-phrase determination (CONFORMANCE.md §7 item 179).</summary>
    public static Int128 RescaleEscape(Int128 value, int fromScale, int toScale, CobolRounding mode)
    {
        // The bound is the shared WideningFits predicate (kb/Work PB639) — written on the SIGNED value rather
        // than through Int128.Abs, which throws on Int128.MinValue, a value the R10 bits contract can deliver
        // here (kb/Work PB288's sibling sweep). One rule, one place: this arm, RescaleChecked and TryStore all
        // ask the same question and differ only in what they do with the answer.
        if (!WideningFits(value, toScale - fromScale))
            throw new CobolSizeError($"scale alignment to {toScale} fraction digits exceeds the Int128 "
                + "intermediate (ISO §8.8.1 alignment at the D1 escape boundary — EC-SIZE-OVERFLOW)", "EC-SIZE-OVERFLOW");
        return Rescale(value, fromScale, toScale, mode);
    }

    /// <summary>The low-order <paramref name="digits"/> of <paramref name="v"/>, sign preserved —
    /// §14.9.12.4 GR6c's subsidiary-quotient cap (kb/Work PB129): the quotient used to form DIVIDE's
    /// remainder has the GIVING receiver's digit count, exactly the value the §14.7.5 no-phrase store
    /// leaves in it (high-order truncation keeps the low digits).</summary>
    public static Int128 CapDigits(Int128 v, int digits) =>
        digits is <= 0 or > 38 ? v : v % Pow10.AsWide(digits);

    /// <summary>The store-semantics widening sibling of <see cref="RescaleEscape"/>: digits a ≤38-digit store
    /// could never use are dropped BEFORE the multiply (decimal high-order truncation — the same cap
    /// <c>CobolDec.ToUnscaled</c>'s widening arm applies), so a receiver-bound rescale composes with the
    /// store's own digit-capacity mod instead of wrapping in binary. Selection results landing in a KNOWN
    /// receiver ride this (a MOVE has §14.9.25.4 GR6 truncation semantics, not raise semantics), as does the
    /// §14.2.3 GR9/GR10 CALL-argument landing (kb/Work PB288 — <c>CobolArgAdapt.Land</c>); the receiverless
    /// lane stays loud on <see cref="RescaleEscape"/>.
    /// <para>The cap test is the <c>keep</c> power-of-ten compare, not a digit-count loop: with
    /// <c>keep = max(0, 38 − shift)</c>, "<c>mag</c> has more than <c>keep</c> digits" IS
    /// <c>mag &gt;= 10^keep</c>, so the test is a table lookup and a compare on what is now a PER-ACCESS ABI
    /// path (<c>CobolArgAdapt.Num</c>'s §14.2.3 GR8 view re-lands on every reference to the formal).</para></summary>
    public static Int128 RescaleStoreCap(Int128 value, int fromScale, int toScale, CobolRounding mode)
    {
        if (toScale <= fromScale) return Rescale(value, fromScale, toScale, mode);
        int shift = toScale - fromScale;
        if (value == 0) return 0;
        bool neg = value < 0;
        // ⛔ Int128.Abs THROWS on Int128.MinValue, which this path can be handed: the R10 bits contract lets a
        // 16-byte unsigned carrier's top-half value arrive as MinValue. Its magnitude is not representable, but
        // its low 38 digits are — and a 39-digit magnitude always takes the cap branch below anyway, so
        // pre-reducing modulo 10^38 loses nothing the result could have used.
        Int128 mag = value == Int128.MinValue ? -(value % Pow10Wide(38)) : Int128.Abs(value);
        Int128 keep = Pow10Wide(Math.Max(0, 38 - shift));
        if (mag >= keep)
        {
            mag %= keep;
            if (mag == 0) return 0;
        }
        Int128 r = mag * Pow10Wide(shift);
        return neg ? -r : r;
    }

    /// <summary>Compare an unsigned wide operand against an Int128-lane operand by algebraic VALUE at their own
    /// scales (ISO §8.8.4.2.4). Returns &lt;0 / 0 / &gt;0. A negative right side is always the lesser; the
    /// non-negative compare rides the both-unsigned overload.</summary>
    public static int CompareU(UInt128 a, int aScale, Int128 b, int bScale) =>
        b < 0 ? 1 : CompareU(a, aScale, (UInt128)b, bScale);

    /// <summary>The mirrored operand order (an Int128-lane left side against an unsigned wide right side).</summary>
    public static int CompareU(Int128 a, int aScale, UInt128 b, int bScale) => -CompareU(b, bScale, a, aScale);

    /// <summary>Both sides unsigned wide: align the smaller-scale side up by a power of ten and compare. When the
    /// scale-up would overflow <see cref="UInt128"/>, that side's algebraic value strictly exceeds the other
    /// (which fits) — no wider intermediate is needed.</summary>
    public static int CompareU(UInt128 a, int aScale, UInt128 b, int bScale)
    {
        if (aScale == bScale) return a.CompareTo(b);
        if (aScale < bScale)
        {
            UInt128 f = Pow10U(bScale - aScale);
            if (a != 0 && a > UInt128.MaxValue / f) return 1;   // a's value overflows the alignment ⇒ a > b
            return (a * f).CompareTo(b);
        }
        return -CompareU(b, bScale, a, aScale);
    }

    /// <summary>The DISPLAY image of an unsigned wide item's value — the unsigned-wide sibling of
    /// <see cref="FormatDisplay(Int128, in NumProfile)"/> (a 16-byte unsigned COMP-5 item is always unsigned, so
    /// the image is the bare magnitude run at the picture's digit count). ⚠ The digit count is the PICTURE's even
    /// for a BinaryCapacity item holding a beyond-PICTURE value — a DETERMINATION, not an oversight: ISO §14.9.11.4
    /// GR1 leaves "any conversion of data required between … identifier-1 and the device" to the implementor, the
    /// vendors split (IBM prints the container value, GnuCOBOL the PICTURE-digit image), and the owner chose
    /// GnuCOBOL (kb/Work R13; docs/CONFORMANCE.md DOC-A.1-56; golden <c>comp5_display_beyond_picture</c>).
    /// kb/Work PB825 re-filed it as a defect and was retired against that decision. ⛔ DISTINCTLY NAMED, not an overload:
    /// an <c>int</c> constant converts implicitly to BOTH <see cref="Int128"/> and <see cref="UInt128"/>
    /// (the constant-expression conversion chains through <c>uint</c>), so a same-name overload pair makes
    /// every emitted <c>Store(0, …)</c>-shaped call a CS0121 ambiguity — 119 corpus programs failed exactly
    /// that way. The emitter picks the U-named lane from the operand's static carrier (NumX.U).</summary>
    public static string FormatDisplayU(UInt128 value, in NumProfile receiver) =>
        FormatUnsignedDisplayU(value, receiver.Digits);

    /// <summary>The unsigned-wide sibling of <see cref="FormatUnsignedDisplay(Int128, int)"/> (distinctly
    /// named — see <see cref="FormatDisplayU"/>).</summary>
    public static string FormatUnsignedDisplayU(UInt128 value, int digits)
    {
        if (digits <= 0) return "";
        UInt128 v = value % Pow10U(digits);
        return v.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    /// <summary>Rescale an unsigned wide unscaled value between fraction scales. Narrowing divides with the
    /// rounding <paramref name="mode"/> (the shared generic <see cref="RoundDiv{T}"/> kernel). Widening multiplies
    /// by 10^k; a product beyond <see cref="UInt128"/> reduces MODULO the receiver's capacity
    /// (<paramref name="capacityModulus"/>) one decade at a time — the deterministic no-ON-SIZE-ERROR truncation,
    /// identical in effect to the in-range path followed by the caller's capacity reduction.</summary>
    private static UInt128 RescaleU(UInt128 value, int fromScale, int toScale, CobolRounding mode, UInt128 capacityModulus)
    {
        if (toScale == fromScale || value == 0) return value;
        if (toScale < fromScale) return RoundDiv(value, Pow10U(fromScale - toScale), mode);
        int k = toScale - fromScale;
        if (k <= 38 && value <= UInt128.MaxValue / Pow10U(k)) return value * Pow10U(k);
        // The exact product exceeds UInt128 — reduce modulo the receiver's capacity one decade at a time (each
        // step's operand stays below capacity × 10 ≤ 2^128); modulus 2^128 arrives encoded as 0, where the
        // unchecked multiply IS the reduction.
        UInt128 r = capacityModulus == 0 ? value : value % capacityModulus;
        for (int i = 0; i < k; i++)
            r = capacityModulus == 0 ? unchecked(r * 10) : (r * 10) % capacityModulus;
        return r;
    }

    /// <summary>10^n as a <see cref="UInt128"/> (n in 0..38 — every capacity bound below 2^128).</summary>
    private static UInt128 Pow10U(int n) => (UInt128)Pow10Wide(n);

    /// <summary>True when rescaling <paramref name="value"/> from <paramref name="fromScale"/> to a smaller
    /// <paramref name="toScale"/> would drop a nonzero fraction (an inexact transfer).</summary>
    private static bool IsInexactAtScale(Int128 value, int fromScale, int toScale) =>
        toScale < fromScale && value % Pow10Wide(fromScale - toScale) != 0;

    // ── The ORDINAL-POSITION integrality rule (ISO §8.4.2.3.4 GR1b · §8.4.3.3.4 rule 5)c); fix-queue PB41) ──────
    // A subscript and a reference-modifier leftmost-position/length are the VALUE of an arithmetic expression, and
    // BOTH clauses make a non-integer value an exception condition rather than a truncation. A WiseOwl COBOL numeric
    // item stores UNSCALED (PIC 9V9 VALUE 2.0 is the field 20L, scale 1), so the value question and the storage
    // question are different questions — reading the storage as an occurrence number is how PB41 indexed
    // occurrence 20 for the subscript 2.0. These two helpers are the ONE place that difference is resolved; the
    // two positions differ ONLY in which Table 13 condition they name (EC-BOUND-SUBSCRIPT vs EC-BOUND-REF-MOD),
    // which is why the wrappers live with their position (CobolTable.Occ / CobolString.RefModPosition) and the
    // arithmetic lives here.

    /// <summary>True when <paramref name="unscaled"/> at <paramref name="scale"/> carries a nonzero fraction —
    /// i.e. the ordinal position it denotes is NOT an integer (ISO §8.4.2.3.4 GR1b / §8.4.3.3.4 rule 5)c)).
    /// A scale of zero is the ordinary integer item and can never be fractional.</summary>
    public static bool HasFraction(Int128 unscaled, int scale) =>
        scale > 0 && unscaled % Pow10Wide(scale) != 0;

    /// <summary>The integer ordinal position <paramref name="unscaled"/> at <paramref name="scale"/> denotes —
    /// the VALUE, de-scaled, truncating toward zero. The caller has already raised its position's exception
    /// condition for a fractional value (<see cref="HasFraction"/>); with that condition's checking OFF the
    /// truncated position is the lenient continue, matching the surrounding out-of-range scratch policy.
    /// <para>A NEGATIVE scale is a trailing-P item (<c>PIC 9P</c> stores <c>2</c> for the value 20 — the signed
    /// scale of <see cref="NumProfile.FractionDigits"/>), whose VALUE is the stored digits times 10^|scale|; reading
    /// the storage instead positioned occurrence 2 for the subscript 20 (kb/Work PB1117's sibling sweep). A
    /// product past <c>long</c>'s range saturates by sign, as <see cref="Position(Int128)"/> does.</para></summary>
    public static long PositionOf(Int128 unscaled, int scale) => scale switch
    {
        > 0 => Position(unscaled / Pow10Wide(scale)),
        < 0 when unscaled == 0 => 0,
        // |unscaled| ≤ long.MaxValue and at most 18 places keep the product inside Int128 (< 10^37); beyond
        // either bound the value is past every table's range, so it saturates by sign.
        < 0 when -scale <= 18 && Int128.Abs(unscaled) <= long.MaxValue => Position(unscaled * Pow10Wide(-scale)),
        < 0 => unscaled > 0 ? long.MaxValue : long.MinValue,
        _ => Position(unscaled),
    };

    /// <summary>⛔ THE ONE NARROWING of a wide position value to the <c>long</c> every table/ref-mod accessor
    /// takes, and it SATURATES rather than wraps. ISO §8.4.2.3.4 GR2: "If the value of the subscript is not a
    /// positive integer or is less than one or is greater than the highest permissible occurrence number, the
    /// EC-BOUND-SUBSCRIPT exception condition is set to exist" — so an out-of-range position MUST stay
    /// out of range on the way in. A plain <c>(long)</c> cast is the opposite: it wraps, and a wrapped value can
    /// land back INSIDE 1..n, converting a condition the standard requires to be detected into a silent wrong
    /// answer. (The same reasoning the D18 §15.4 segment temp is sized by: "high-order truncation could WRAP an
    /// out-of-range subscript into an in-range one".) Reachable from every wide carrier — a &gt;18-digit
    /// item, an unsigned BINARY-CAPACITY container, and an alphanumeric image decoded by
    /// <see cref="FromAlphanumeric"/>, which returns up to 38 digits.</summary>
    public static long Position(Int128 value) =>
        value > long.MaxValue ? long.MaxValue : value < long.MinValue ? long.MinValue : (long)value;

    /// <inheritdoc cref="Position(Int128)"/>
    public static long Position(UInt128 value) =>
        value > (UInt128)long.MaxValue ? long.MaxValue : (long)value;

    // ── The position of an EXPRESSION, on the carrier the expression evaluated on (kb/Work PB1890) ─────────────
    // A subscript or reference-modifier bound that is an arithmetic expression (or a floating-point item) is
    // evaluated once, as a statement pre-operation, and these intakes ask the integrality question of the value the
    // native or standard intermediate HOLDS — docs/CONFORMANCE.md DOC-A.1-124 — before anything stores it. The
    // scaled carrier needs no new intake (HasFraction / PositionOf above); the SDIDI and binary64 carriers are below.

    /// <summary>True when the binary64 value has a nonzero fraction: a finite value is an integer exactly when it
    /// equals its own truncation (COMP-2 <c>2.0</c> is, <c>2 + 1.0E-10</c> is not). ±∞ and NaN are not fractional —
    /// they are out of every range, which each consumer's range test answers. THE ONE binary64 integrality
    /// predicate: the §15.3 type-6 intrinsic intake and the two position intakes ask it.</summary>
    public static bool HasFraction(double value) => double.IsFinite(value) && value != Math.Truncate(value);

    /// <summary>The integer ordinal position an SDIDI value denotes, truncated toward zero and SATURATED to
    /// <c>long</c> (<see cref="Position(Int128)"/>'s reason: an out-of-range position must stay out of range). The
    /// caller has already raised its position's condition for a fractional value (<see cref="CobolDec.HasFraction"/>).
    /// A magnitude past the <c>long</c> range — which the Int128 truncation could not even form for a large exponent —
    /// saturates by sign before it is truncated.</summary>
    public static long PositionOf(CobolDec value)
    {
        double approx = value.ToDouble();
        if (approx >= 9.2e18) return long.MaxValue;
        if (approx <= -9.2e18) return long.MinValue;
        return Position(value.TruncatedInteger());
    }

    /// <summary>The integer ordinal position a binary64 value denotes, truncated toward zero and saturated to
    /// <c>long</c>; NaN denotes no position and reads as 0, which every table and every reference modification
    /// rejects as "less than one" (§8.4.2.3.4 GR2, §8.4.3.3.4 rule 5)c)).</summary>
    public static long PositionOf(double value) =>
        double.IsNaN(value) ? 0
        : value >= 9.2e18 ? long.MaxValue
        : value <= -9.2e18 ? long.MinValue
        : (long)Math.Truncate(value);

    /// <summary>The <see cref="Position(Int128)"/> narrowing to an <c>int</c> — for the consumers whose host
    /// parameter is an <c>int</c>: a reference-modifier position or length, a GO TO … DEPENDING selector, an
    /// ADVANCING or LINAGE line count, an OCCURS DEPENDING current count (kb/Work PB1033). The same rule, the same
    /// reason: those consumers used to take a bare <c>(int)(…)</c> of the integer landing, and a C# cast WRAPS —
    /// <c>GO TO P1 P2 DEPENDING ON G</c> with <c>G = 4294967297</c> went to P1 where ISO §14.9.17.4 GR2 transfers
    /// nowhere, and a leftmost position of 4294967297 read position 1 where §8.4.3.3.4 5) c) raises
    /// EC-BOUND-REF-MOD. Saturation is the out-of-range answer at every one of them, because every range they
    /// test lies strictly inside the carrier: no item, record, page or table in this implementation has
    /// <c>int.MaxValue</c> positions, so a saturated value is still out of range; and the sign survives, so a
    /// negative value stays negative. A VALUE a consumer keeps (an ADVANCING count) is bounded at the carrier —
    /// docs/CONFORMANCE.md §3 "Integer operands and host carriers" documents it.</summary>
    public static int Position32(Int128 value) => int.CreateSaturating(value);

    /// <summary>A COBOL integer LITERAL's value, for the emitter to fold a literal through <see cref="Position(Int128)"/>
    /// / <see cref="Position32"/> at COMPILE time, so a literal and a data item holding the same value can never
    /// narrow differently. A fixed-point literal has at most 31 digits (ISO §8.3.3.3.2) and 10^31 &lt; 2^127, so
    /// every legal integer literal is exact here.</summary>
    public static Int128 IntegerLiteralValue(string literal) =>
        Int128.Parse(literal, System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>An unscaled/scale pair rendered as its plain decimal VALUE — for the diagnostic text of the two
    /// position conditions, so the message names the value the program computed (2.5) and never its storage (25).</summary>
    public static string PlainValue(Int128 unscaled, int scale)
    {
        if (scale <= 0) return unscaled.ToString();
        bool neg = unscaled < 0;
        Int128 mag = neg ? -unscaled : unscaled, div = Pow10Wide(scale);
        return $"{(neg ? "-" : "")}{mag / div}.{(mag % div).ToString().PadLeft(scale, '0')}";
    }

    /// <summary>Multiply two unscaled operands with overflow checking against the Int128 carrier: raises
    /// <see cref="CobolSizeError"/> EC-SIZE-OVERFLOW (the size error condition, ISO §14.7.5 case 5) when the product
    /// leaves it. ⛔ THE RENDERER EMITS IT WHENEVER THE OPERANDS' DIGIT BOUNDS DO NOT PROVE THE PRODUCT FITS (kb/Work
    /// PB1143 — <c>NumericRenderer.Multiply</c>): a product whose bounds prove N+M ≤ 38 digits is a bare multiply, one
    /// that may exceed 38 is formed on the SDIDI instead, and an unknown bound lands HERE in every statement — a
    /// silent two's-complement wrap is no longer a possible outcome of a native product. Being a method call (not a
    /// constant expression), a constant product is checked at run time, never folded to a compile-time error.</summary>
    public static Int128 MulChecked(Int128 a, Int128 b)
    {
        // kb/Work PB91: the overflow is the size error CONDITION (§14.7.5 case 5) wherever the checked kernel runs —
        // inside an arithmetic statement its try catches CobolSizeError (and reads EcName), and in a NON-arithmetic
        // context under EC-SIZE checking (a condition, an argument, a subscript) the statement's ambient EC guard
        // dispatches it as the fatal EC-SIZE-OVERFLOW; a bare OverflowException escaped that guard as a crash.
        try { return checked(a * b); }
        catch (OverflowException) { throw new CobolSizeError("intermediate product exceeds the Int128 carrier", "EC-SIZE-OVERFLOW"); }
    }

    /// <summary>The additive siblings of <see cref="MulChecked"/> (the same §14.7.5 case-5 mapping, kb/Work
    /// R10): a sum/difference at the very top of a 16-byte BinaryCapacity container can exceed the Int128
    /// engine itself (HIGHEST-ALGEBRAIC of PIC S9(19) COMP-5 is exactly <see cref="Int128.MaxValue"/>, so
    /// <c>ADD 1</c> overflows the CARRIER, not merely the receiver), and an unchecked <c>+</c> would wrap to
    /// the far end of the container and store "in range" with NO size error. Emitted inside a statement that
    /// carries an ON SIZE ERROR phrase and (kb/Work PB91) in the receiver-less renders of an EC-SIZE-checked
    /// statement — elsewhere an add stays a bare unchecked <c>+</c>, exactly like the multiply. Raises
    /// <see cref="CobolSizeError"/> (the size error condition), never a bare OverflowException.</summary>
    public static Int128 AddChecked(Int128 a, Int128 b)
    {
        try { return checked(a + b); }
        catch (OverflowException) { throw new CobolSizeError("intermediate sum exceeds the Int128 carrier", "EC-SIZE-OVERFLOW"); }
    }

    /// <inheritdoc cref="AddChecked"/>
    public static Int128 SubChecked(Int128 a, Int128 b)
    {
        try { return checked(a - b); }
        catch (OverflowException) { throw new CobolSizeError("intermediate difference exceeds the Int128 carrier", "EC-SIZE-OVERFLOW"); }
    }

    /// <summary>
    /// The CHECKED quotient — <see cref="Divide"/> (whose zero divisor already raises §14.7.5 case 2 in every
    /// context), plus the one size error only a checked context owes: ROUNDED MODE IS PROHIBITED on an inexact
    /// quotient (§14.7.4.3 r7, EC-SIZE-TRUNCATION). Emitted where an ON SIZE ERROR phrase or EC-SIZE checking is in
    /// effect; unchecked, PROHIBITED keeps CONFORMANCE.md DOC-A.1-70's truncated landing (kb/Work PB1196).
    /// </summary>
    public static Int128 DivideOrThrow(Int128 a, int aScale, Int128 b, int bScale, int resultScale, CobolRounding mode)
    {
        Int128 quotient = Divide(a, aScale, b, bScale, resultScale, mode);
        // ROUNDED MODE IS PROHIBITED: an inexact quotient AT resultScale is a size error (§14.7.4.3 r7). When the
        // division rounds directly at the receiver scale (the outermost-division case), the inexactness is consumed
        // inside Divide, so it must be detected here from the exact remainder rather than by the receiver's TryStore.
        if (mode == CobolRounding.Prohibited && DivisionLosesPrecision(a, aScale, b, bScale, resultScale))
            throw new CobolSizeError("ROUNDED MODE IS PROHIBITED on an inexact quotient (ISO §14.7.4.3 r7 — "
                + "EC-SIZE-TRUNCATION; the receiver is left unchanged)", "EC-SIZE-TRUNCATION");
        return quotient;
    }

    /// <summary>True when <c>a/10^aScale ÷ b/10^bScale</c> cannot be represented exactly at <paramref name="resultScale"/>
    /// fractional digits (a nonzero remainder after radix alignment) — used to detect a PROHIBITED violation in a
    /// division. Mirrors <see cref="Divide"/>'s scaling. Assumes <paramref name="b"/> != 0.</summary>
    private static bool DivisionLosesPrecision(Int128 a, int aScale, Int128 b, int bScale, int resultScale)
    {
        int exp = bScale + resultScale - aScale;
        // An alignment past the carrier: exact on the remainder of a × 10^exp (the scale-down case — a divisor scaled
        // past the carrier — leaves the whole dividend as the remainder, which is nonzero for a nonzero dividend).
        if (a != 0 && (exp >= 0 ? !WideningFits(a, exp) : !WideningFits(b, -exp)))
            return exp < 0 || CobolDec.QuotientHasRemainder(a, b, exp);
        Int128 num = a, den = b;   // wide radix alignment — mirrors Divide exactly
        if (exp >= 0) num *= Pow10Wide(exp); else den *= Pow10Wide(-exp);
        return num % den != 0;
    }

    /// <summary>
    /// The COBOL DISPLAY image of a fixed-point value: its unscaled digits, zero-padded on the left to the
    /// picture's digit count, with no decimal point (the point is implied). A signed item carries its sign per the
    /// receiver's <see cref="NumProfile.SignKind"/> (over-punch, separate sign, or a binary leading minus); an
    /// unsigned item is the bare magnitude.
    /// </summary>
    public static string FormatDisplay(Int128 unscaled, in NumProfile receiver) =>
        receiver.Signed
            ? FormatDisplaySigned(unscaled, receiver)
            : FormatUnsignedDisplay(unscaled, receiver.Digits);

    /// <summary>Storage-form bridge (the <c>CobolTable.Occ</c> pattern): a numeric-DISPLAY field whose backing the
    /// post-bind whole-group analysis turned into its character IMAGE is already display-formatted — pass through.
    /// Lets the compiler emit ONE expression for an image view whose field's storage form is decided later.</summary>
    public static string FormatDisplay(string image, in NumProfile receiver) => image;

    /// <summary>Store a (possibly spliced) DISPLAY image back into a numeric field — the write half of an image
    /// view over a numeric item (reference modification / a RENAMES span leaf, ISO §8.4.3.3.4 GR6 / §13.18.45). The
    /// overload set is the storage-form bridge: the <paramref name="current"/> dummy selects the conversion for
    /// the field's ACTUAL storage (native <c>long</c>/<c>Int128</c> → sign-aware decode; an image-stored string
    /// field keeps the image).</summary>
    public static long StoreDisplay(string image, in NumProfile receiver, long current) =>
        (long)ParseDisplay(image, receiver);

    /// <inheritdoc cref="StoreDisplay(string, in NumProfile, long)"/>
    public static Int128 StoreDisplay(string image, in NumProfile receiver, Int128 current) =>
        ParseDisplay(image, receiver);

    /// <inheritdoc cref="StoreDisplay(string, in NumProfile, long)"/>
    public static string StoreDisplay(string image, in NumProfile receiver, string current) => image;

    /// <inheritdoc cref="StoreDisplay(string, in NumProfile, long)"/>
    /// <remarks>The unsigned-carrier bridges (kb/Work R10): the parsed image of an unsigned item is a
    /// non-negative digit run of at most 31 positions, so the casts are exact.</remarks>
    public static ulong StoreDisplay(string image, in NumProfile receiver, ulong current) =>
        (ulong)ParseDisplay(image, receiver);

    /// <inheritdoc cref="StoreDisplay(string, in NumProfile, ulong)"/>
    public static UInt128 StoreDisplay(string image, in NumProfile receiver, UInt128 current) =>
        (UInt128)ParseDisplay(image, receiver);

    // ⛔ THE OVER-PUNCH TABLES LIVE IN ZonedSign, NOT HERE (kb/Work PB803). They used to be two private consts on
    // this type, which meant the CLASS CONDITION's valid-sign set had to be written a second time in CobolClass —
    // one rule in two places, and the copy that had to move when the owner made the convention an option.

    /// <summary>
    /// The DISPLAY image of a <b>signed</b> fixed-point value, applying the receiver's sign convention to the
    /// zero-padded magnitude digits (COBOLNET_DESIGN §6.4):
    /// <list type="bullet">
    ///   <item><see cref="NumericSign.TrailingOverpunch"/>/<see cref="NumericSign.LeadingOverpunch"/> — fuse the
    ///         sign onto the last / first digit via the program's <see cref="ZonedSign"/> tables (ISO §13.18.52.4
    ///         GR4 / GR5, Annex A.1 item 177 — the convention is <c>receiver.SignEncoding</c>, kb/Work PB803);</item>
    ///   <item><see cref="NumericSign.LeadingSeparate"/>/<see cref="NumericSign.TrailingSeparate"/> — a leading /
    ///         trailing <c>+</c>/<c>-</c> character (always present);</item>
    ///   <item><see cref="NumericSign.BinaryMinus"/> — a leading <c>-</c> only when negative (positive/zero bare).</item>
    /// </list>
    /// </summary>
    public static string FormatDisplaySigned(Int128 unscaled, in NumProfile receiver) =>
        FormatDisplaySigned(unscaled, unscaled < 0, receiver);

    /// <summary>The signed DISPLAY image with the sign given APART from the magnitude — the only way to write a
    /// NEGATIVE ZERO, which a value cannot be (<see cref="RetainSign"/>).</summary>
    private static string FormatDisplaySigned(Int128 unscaled, bool neg, in NumProfile receiver)
    {
        string mag = FormatUnsignedDisplay(unscaled, receiver.Digits);
        return receiver.SignKind switch
        {
            NumericSign.BinaryMinus => neg ? "-" + mag : mag,
            NumericSign.LeadingSeparate => (neg ? "-" : "+") + mag,
            NumericSign.TrailingSeparate => mag + (neg ? "-" : "+"),
            NumericSign.LeadingOverpunch => Overpunch(mag, 0, neg, receiver.SignEncoding),
            _ => Overpunch(mag, mag.Length - 1, neg, receiver.SignEncoding),   // TrailingOverpunch (the default)
        };
    }

    /// <summary>Replace the digit at <paramref name="pos"/> of <paramref name="mag"/> with its signed over-punch
    /// under <paramref name="encoding"/> — the program's §13.18.52.4 GR4/GR5 convention (kb/Work PB803).</summary>
    private static string Overpunch(string mag, int pos, bool negative, SignEncoding encoding)
    {
        if (pos < 0 || pos >= mag.Length) return mag;   // no digit positions (Digits == 0)
        int v = mag[pos] - '0';
        if ((uint)v > 9) return mag;
        char op = ZonedSign.Punch(encoding, v, negative);
        // The ASCII convention's POSITIVE table is the plain digits, so the punch is the character already there —
        // return the string unchanged rather than rebuilding it (the +ve DISPLAY path is the common one).
        return op == mag[pos] ? mag : mag[..pos] + op + mag[(pos + 1)..];
    }

    /// <summary>The DISPLAY image of an unsigned integer with <paramref name="digits"/> digit positions: the
    /// magnitude's low <paramref name="digits"/> digits, zero-padded.</summary>
    public static string FormatUnsignedDisplay(Int128 value, int digits)
    {
        if (digits <= 0) return "";
        Int128 v = value % Pow10Wide(digits);
        string s = (v < 0 ? -v : v).ToString(CultureInfo.InvariantCulture);
        return s.PadLeft(digits, '0');
    }

    /// <summary>
    /// ⛔ THE TEXT IMAGE OF AN INTRINSIC FUNCTION'S RETURNED VALUE (DA2). A function's value lives in a
    /// "temporary elementary data item" (ISO §15.4) whose characteristics, under native arithmetic, are
    /// explicitly <b>defined by the implementor</b> (§15.4.1); §14.9.11.4 GR1 likewise makes any conversion
    /// between a DISPLAY operand and the device implementor-defined. WiseOwl COBOL's determination — documented as
    /// an Annex A.1 item — is the <b>literal form of the value</b>: the significant digits with no leading-zero
    /// padding, a leading <c>-</c> when negative, and a decimal point followed by exactly
    /// <paramref name="scale"/> fraction digits when the value is scaled.
    /// <para>
    /// ⛔ WHY THIS RULE AND NOT A FIXED FIELD WIDTH: an intrinsic whose arguments are all literals is FOLDED at
    /// compile time to a numeric literal, and a numeric literal renders as its own text (<c>DISPLAY 42</c> →
    /// <c>42</c>). If a computed result rendered in a padded fixed width instead, then
    /// <c>DISPLAY FUNCTION LENGTH(X)</c> and <c>DISPLAY FUNCTION ORD(C)</c> would print in different formats for
    /// no reason a COBOL programmer can see — the optimization would have become OBSERVABLE. One rule, so the
    /// fold cannot be detected.
    /// </para>
    /// </summary>
    /// <param name="deSign">⛔ §14.9.25.4 GR6a — "If the sending operand is described as being signed numeric, the
    /// operational sign is not moved." True for every context that MOVES this value to an alphanumeric/national/
    /// edited receiver, compares it as text (§8.8.4.2.5 routes that through the MOVE rules), or INSPECTs it: the
    /// MAGNITUDE travels, never the sign. This is a GENERAL rule with no implementor latitude — the §15.4.1 /
    /// §14.9.11.4 GR1 latitude this determination rests on covers the FORM of the text, not whether the sign
    /// travels. False for DISPLAY, where the sign IS part of the value being shown.</param>
    public static string FormatFunctionText(Int128 unscaled, int scale, bool deSign = false)
    {
        if (deSign && unscaled < 0) unscaled = -unscaled;
        bool neg = unscaled < 0;
        string digits = (neg ? -unscaled : unscaled).ToString(CultureInfo.InvariantCulture);
        string body;
        if (scale <= 0)
        {
            // A NEGATIVE scale is a P-scaled value: the Ps are trailing zeros that occupy no storage
            // (§13.18.40.3 symbol-P operations item b), so they are restored here.
            body = scale < 0 ? digits + new string('0', -scale) : digits;
        }
        else
        {
            // Left-pad so a value smaller than one full unit still shows the leading "0." (1 unscaled at
            // scale 2 is 0.01, never .01).
            digits = digits.PadLeft(scale + 1, '0');
            body = digits[..^scale] + "." + digits[^scale..];
        }
        return neg ? "-" + body : body;
    }

    // ── THE ALPHANUMERIC SENDING OPERAND (ISO §14.9.25.4 GR6 d) ─────────────────────────────────────────────
    //
    // GR6 d) governs "when a numeric or numeric-edited item is the receiving item". Its three clauses about an
    // ALPHANUMERIC or NATIONAL sending operand are THREE SEPARATE QUESTIONS, and each has exactly one method
    // here — that separation is the point of this block (kb/Work PB426 + PB844, one mechanism):
    //
    //   d) 3     — HOW BIG is the operand:  the SIZE RULE       → FromAlphanumeric        (the rightmost 31)
    //   d) 1     — is its content VALID:    EC-DATA-INCOMPATIBLE → FromAlphanumericSending (the checked read)
    //   §14.6.13.2 — what the digits decode to when it is not   → DigitMagnitude          (the tolerant scan)
    //
    // ⛔ DigitMagnitude IS NOT A SPELLING OF FromAlphanumeric. The size rule belongs to the alphanumeric SENDING
    // OPERAND and to nothing else: a caller decoding an image whose size is already fixed by its OWN data
    // description — a numeric item's character image on the storage-form bridges (CobolTable.Occ,
    // CobolString.RefModPosition, INSPECT's re-store of a replaced numeric image), or GR6 d) 3 b)'s figurative
    // replication across the RECEIVER's digit positions — takes DigitMagnitude, because the standard asks no size
    // question of them at all.
    //
    // MEASURED, so that the next reader does not have to re-derive it: those bridges DO see 32-character images
    // today (a PIC S9(31) SIGN TRAILING SEPARATE leaf under a whole-group MOVE images as 31 digits plus the sign
    // character — probed), and routing them through the capped entry was tried and changed NO answer, because the
    // position the window would drop is a leading zero in every reachable case. The split is therefore not a bug
    // fix on those paths; it is what stops the NEXT change to the alphanumeric sending rule — a widened window, a
    // sign reading, a raise — from silently becoming a change to five callers that never asked for it.

    /// <summary>The deterministic digit decode of a character image whose SIZE is already fixed by its own data
    /// description — no size rule is applied here (ISO §14.6.13.2: incompatible content in a numeric context is
    /// undefined, and WiseOwl COBOL's determination is deterministic — a non-digit position contributes no digit, an
    /// all-non-digit or empty image is 0). The unsigned MAGNITUDE only: no operational sign is decoded.
    /// <para>⛔ An ALPHANUMERIC or NATIONAL SENDING OPERAND takes <see cref="FromAlphanumeric"/> instead, which
    /// applies §14.9.25.4 GR6 d) 3's 31-character size rule before this scan.</para></summary>
    public static Int128 DigitMagnitude(string image) => DigitMagnitude(image.AsSpan());

    /// <inheritdoc cref="DigitMagnitude(string)"/>
    /// <remarks>The span overload is the core, so <see cref="FromAlphanumeric"/>'s 31-position window costs no
    /// allocation — it slices rather than substrings. The emitted C# always passes a <c>string</c> and binds to
    /// the overload above.</remarks>
    public static Int128 DigitMagnitude(ReadOnlySpan<char> image)
    {
        Int128 mag = 0;
        foreach (char c in image)
            if (c is >= '0' and <= '9') mag = mag * 10 + (c - '0');
        return mag;
    }

    /// <summary>⛔ THE SIZE OF AN ALPHANUMERIC SENDING OPERAND, AND THE REASON THE Int128 CARRIER IS UNREACHABLE
    /// (ISO §14.9.25.4 GR6 d) 3; kb/Work PB426). When the sending operand is described as alphanumeric or
    /// national it "is treated as if it were an unsigned integer of category numeric" whose size the standard
    /// CAPS: a) "the number of digits is the number of character positions in the sending data item unless the
    /// number of character positions is greater than 31, in which case the rightmost 31 character positions are
    /// used"; c) for a literal, "if the number of characters exceeds 31, the size of the sending operand is 31
    /// digits and only the rightmost 31 characters in the literal are used". Both clauses say the same thing —
    /// the rightmost 31 CHARACTER POSITIONS — so one window serves both.
    /// <para>The cap is on CHARACTER POSITIONS, not on digit characters: §14.6.13.2 lets a non-digit position
    /// contribute no digit, so the window is taken over the IMAGE and only then scanned
    /// (<see cref="DigitMagnitude"/>). <c>"12X4567890123456789012345678901234"</c> and
    /// <c>"1234567890123456789012345678901234"</c> therefore give different answers, as they must.</para>
    /// <para>Without the cap the accumulator consumed every position of the operand and a 39-character digit run
    /// WRAPPED the signed Int128: <c>MOVE A40 TO PIC 9(9)</c> stored 838277934 where §14.6.8.2 GR4's
    /// decimal-point alignment of the capped operand gives 234567890 — a silent wrong answer on ordinary source
    /// at every edition. 31 digits &lt; 10^31 &lt; Int128.MaxValue, so with the cap in place no alphanumeric
    /// operand can reach the carrier at all.</para></summary>
    public static Int128 FromAlphanumeric(string image)
    {
        ReadOnlySpan<char> s = image;
        return DigitMagnitude(s.Length > MaxSendingCharacterPositions ? s[^MaxSendingCharacterPositions..] : s);
    }

    /// <summary>§14.9.25.4 GR6 d) 3 a) / c): the number of character positions of an alphanumeric or national
    /// sending operand that a numeric receiver sees.</summary>
    public const int MaxSendingCharacterPositions = 31;

    /// <summary>The CHECKED read of an alphanumeric or national sending operand moving to a numeric or
    /// numeric-edited receiver (ISO §14.9.25.4 GR6 d) 1's closing sentence; kb/Work PB844): "Otherwise, if the
    /// content of the sending operand would result in a false value in a numeric class condition, the
    /// EC-DATA-INCOMPATIBLE exception condition is set to exist, and the results of the execution of the MOVE
    /// statement are undefined." The class condition is §8.8.4.4.4 GR3 n) 2 — a non-numeric-category operand is
    /// numeric iff its content "consists entirely of the characters 0, 1, 2, 3, …, 9" — so the ONE predicate
    /// <see cref="CobolClass.IsNumeric"/> answers it, including the zero-length answer (GR1: false).
    /// <para>The exact twin of <see cref="ParseImageSending"/>, which carries §14.6.13.2 rule 2 for a numeric
    /// sending item; that rule cannot cover this case, because an alphanumeric item holding <c>"Q"</c> is
    /// perfectly valid for its OWN data description — it is the MOVE that asks a numeric question of it. With
    /// checking OFF the flag test short-circuits before the scan and the tolerant decode stands, which is
    /// conformant precisely because the standard makes the result undefined here.</para>
    /// <para>⛔ The raise is the WHOLE content's, not the capped window's: d) 1 speaks of "the content of the
    /// sending operand" and is evaluated before d) 3 narrows it, so <c>PIC X(40) VALUE "Q0000…"</c> raises even
    /// though its rightmost 31 positions are all digits.</para></summary>
    public static Int128 FromAlphanumericSending(string image)
    {
        if (ExceptionState.DataIncompatibleChecking && !CobolClass.IsNumeric(image))
            ExceptionState.DataIncompatibleError(
                "the content of an alphanumeric or national sending operand is not numeric "
                + "(ISO §14.9.25.4 GR6 d) 1 — it would result in a false value in a numeric class condition)");
        return FromAlphanumeric(image);
    }

    /// <summary>
    /// Decode a USAGE DISPLAY numeric item's character image back to its unscaled <see cref="long"/> value — the
    /// inverse of <see cref="FormatDisplay"/> (COBOLNET_DESIGN §6.4). The image is the zoned digit run at the item's
    /// scale (no decimal point — the point is implied); the operational sign is decoded per the receiver's
    /// <see cref="NumProfile.SignKind"/> (over-punch, separate <c>+</c>/<c>-</c>, or a leading minus). Non-digit
    /// characters (e.g. the spaces a whole-group MOVE legitimately deposits — ISO/IEC 1989:2023 §14.9.25.4 MOVE GR4 fills
    /// without consideration for subordinate items) contribute no digit; an all-non-digit image decodes to 0, since
    /// using incompatible data in a numeric context is undefined (§14.6.13.2 / the EC-DATA-INCOMPATIBLE condition),
    /// so a deterministic 0 is conformant.
    /// </summary>
    public static Int128 ParseDisplay(string image, in NumProfile receiver)
    {
        Int128 mag = ParseDisplayMagnitude(image, receiver, out bool negative);
        return negative ? -mag : mag;
    }

    /// <summary>The sign an item's DISPLAY image CARRIES — the one fact an <see cref="Int128"/> cannot hold for a
    /// zero (there is no negative zero in a value). The same decode <see cref="ParseDisplay"/> performs, stopped
    /// before the magnitude is signed: an unsigned item, an empty image and a positive or unpunched sign answer
    /// false (ISO §14.9.22.4 GR4 d), kb/Work PB1128).</summary>
    public static bool ImageIsNegative(string image, in NumProfile receiver)
    {
        ParseDisplayMagnitude(image, receiver, out bool negative);
        return negative;
    }

    /// <summary>⛔ THE REPLACED IMAGE OF A SIGNED NUMERIC DISPLAY ITEM KEEPS ITS ORIGINAL SIGN, EVEN OVER A ZERO
    /// MAGNITUDE (ISO §14.9.22.4 GR4 d: "if identifier-1 is a signed numeric item, the original value of the sign
    /// is retained upon completion of the INSPECT statement"; kb/Work PB1128). <paramref name="digits"/> is the
    /// digit run INSPECT produced (never signed), <paramref name="current"/> the item's image BEFORE the
    /// statement. The result is the item's own signed image — in its own sign convention (over-punch, separate or
    /// binary minus) — carrying <paramref name="current"/>'s sign. A negative item whose digits were all replaced by
    /// zeros stays NEGATIVE ZERO (<c>00}</c>), which a value-carrying store (<c>FormatDisplay(-0)</c>) can never
    /// write, since it takes the sign from the value.</summary>
    public static string RetainSign(string digits, string current, in NumProfile receiver) =>
        FormatDisplaySigned(DigitMagnitude(digits), ImageIsNegative(current, receiver), receiver);

    /// <summary>The magnitude half of <see cref="ParseDisplay"/>, with the decoded sign returned apart from it.</summary>
    private static Int128 ParseDisplayMagnitude(string image, in NumProfile receiver, out bool negative)
    {
        negative = false;
        if (string.IsNullOrEmpty(image)) return 0;
        var chars = image.ToCharArray();   // a mutable copy so a sign carrier can be reduced to its digit

        if (receiver.Signed)
        {
            switch (receiver.SignKind)
            {
                case NumericSign.LeadingSeparate:
                    negative = chars[0] == '-';
                    if (chars[0] is '-' or '+') chars[0] = ' ';
                    break;
                case NumericSign.TrailingSeparate:
                    negative = chars[^1] == '-';
                    if (chars[^1] is '-' or '+') chars[^1] = ' ';
                    break;
                case NumericSign.BinaryMinus:
                    negative = chars[0] == '-';
                    if (chars[0] == '-') chars[0] = ' ';
                    break;
                case NumericSign.LeadingOverpunch:
                    DecodeOverpunch(chars, 0, ref negative, receiver.SignEncoding);
                    break;
                default:   // TrailingOverpunch (the no-SIGN-clause default)
                    DecodeOverpunch(chars, chars.Length - 1, ref negative, receiver.SignEncoding);
                    break;
            }
        }

        Int128 mag = 0;
        foreach (char c in chars)
            if (c is >= '0' and <= '9')
                mag = mag * 10 + (c - '0');
        return mag;
    }

    /// <summary>Reduce the over-punch character at <paramref name="pos"/> (in place) to its underlying digit, setting
    /// <paramref name="negative"/> when the punch is in <paramref name="encoding"/>'s NEGATIVE table (kb/Work PB803 —
    /// the program's §13.18.52.4 GR5 b) sign set, <see cref="ZonedSign"/>). A character in neither table — a plain
    /// digit under the IBM convention, or genuinely incompatible content (§14.6.13.2) — is left as-is and the sign
    /// stays positive.</summary>
    private static void DecodeOverpunch(char[] chars, int pos, ref bool negative, SignEncoding encoding)
    {
        if (pos < 0 || pos >= chars.Length) return;
        if (ZonedSign.TryUnpunch(encoding, chars[pos], out int digit, out bool neg))
        {
            chars[pos] = (char)('0' + digit);
            negative = neg;
        }
    }

    /// <summary>
    /// Divide two fixed-point operands and return the quotient as an unscaled integer at <paramref name="resultScale"/>
    /// fractional digits, rounding with <paramref name="mode"/>. Operands are given as unscaled integers with their
    /// own scales (<c>a/10^aScale ÷ b/10^bScale</c> rendered at <paramref name="resultScale"/>). The radix alignment
    /// (<c>a × 10^exp</c>) runs in <see cref="Int128"/> — an 18-significant-digit dividend scaled by the receiver's
    /// fraction digits exceeds the long range MID-computation even though the QUOTIENT fits (ISO §8.8.1: arithmetic
    /// operates on the algebraic values; intermediate width is the implementor's problem, not the program's) — and an
    /// alignment that would leave Int128 is formed on the SDIDI instead (see the body; kb/Work PB1143).
    /// <para>A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> — §14.7.5 case 2 — checked or NOT: the
    /// condition exists either way, and checking decides only its disposition (the SIZE ERROR phrase, an EC-SIZE
    /// declarative or PERFORM WHEN, else abnormal termination — CONFORMANCE.md DOC-A.1-70's determination for the
    /// unchecked §14.6.13.1.3 item 8 case). This unchecked kernel returned 0 until kb/Work PB1605, so
    /// <c>COMPUTE A = B / Z</c> stored 000 and the run went on.</para>
    /// </summary>
    public static Int128 Divide(Int128 a, int aScale, Int128 b, int bScale, int resultScale, CobolRounding mode)
    {
        if (b == 0) throw CobolSizeError.ZeroDivide();
        int exp = bScale + resultScale - aScale;     // quotient_unscaled = round(a × 10^exp / b)
        // ⛔ THE RADIX ALIGNMENT NEVER WRAPS (kb/Work PB1143's DIVIDE sibling). The alignment multiplies the
        // dividend (or the divisor) by 10^|exp|, and a 31-digit dividend scaled by a receiver's fraction digits is
        // past Int128 although the QUOTIENT is not — `PIC 9(10)` over a `PIC 9V9(20)` divisor into
        // `PIC 9(10)V9(10)` needs 1.2e9 × 10^30. The unchecked multiply wrapped modulo 2^128 and the quotient of the
        // WRAPPED value was stored silently. When the alignment fits the carrier (every ordinary shape) it is the
        // Int128 divide it always was; when it does not, the quotient is formed on the SDIDI, which owns its exponent
        // (the same carrier a product past 38 digits takes, NumericRenderer.Multiply): 34 significant digits by
        // ROUND-TO-ODD (CobolDec.DivToOdd — an inexact quotient keeps an odd last digit, so the ONE rounding to the
        // result scale below, in any mode, equals rounding the exact quotient; a receiver holds ≤ 31 digit positions,
        // far inside the 34). A quotient the carrier cannot hold at that scale is the §14.7.5 case-5 size error, never
        // a wrapped digit string.
        if (a == 0) return 0;                        // 0 ÷ b at any scale (b ≠ 0, checked above)
        bool fits = exp >= 0 ? WideningFits(a, exp) : WideningFits(b, -exp);
        // A dividend that scales up past the carrier: the exact quotient is formed on the 256-bit numerator and rounded
        // once by its true remainder (CobolDec.QuotientAtScale), so a receiver that holds more than the SDIDI's 34 digits
        // — a 16-byte COMP-5 item, 38 — still gets every digit (kb/Work PB1143's review finding N1). Only a scale-down
        // alignment (a divisor scaled past the carrier, whose quotient is below one unit of the carrier's own range) or
        // an exponent past 10^38 takes the SDIDI.
        if (!fits)
            return exp is >= 0 and <= 38
                ? CobolDec.QuotientAtScale(a, b, exp, mode)
                : CobolDec.DivToOdd(CobolDec.From(a, aScale), CobolDec.From(b, bScale))
                    .ToUnscaledIntermediate(resultScale, mode);
        Int128 num = a, den = b;
        if (exp >= 0) num *= Pow10Wide(exp); else den *= Pow10Wide(-exp);
        if (den < 0) { num = -num; den = -den; }     // RoundDiv requires a positive divisor
        return RoundDiv(num, den, mode);
    }

    /// <summary>
    /// Integer division of <paramref name="value"/> by <paramref name="divisor"/> rounding the (nonzero) remainder
    /// per a COBOL ROUNDED mode — the kernel for scale reduction. GENERIC over the integer carrier so the
    /// <see cref="Int128"/> lane and the unsigned-wide <see cref="UInt128"/> lane (kb/Work R10) round by the ONE
    /// implementation rather than a hand-synced copy (the negative-value arms are simply unreachable for an
    /// unsigned carrier) — and <see cref="System.Numerics.BigInteger"/> for the one cold path that divides an exact
    /// binary64 expansion by a power of two (<c>CobolFloat.LowOrderDigits</c>, kb/Work PB77). <paramref name="divisor"/>
    /// is positive (a power of ten, or that power of two).
    /// </summary>
    internal static T RoundDiv<T>(T value, T divisor, CobolRounding mode) where T : System.Numerics.INumber<T>
    {
        T q = value / divisor, rem = value % divisor;
        if (rem == T.Zero) return q;
        bool neg = value < T.Zero;
        T step = neg ? -T.One : T.One;                       // the away-from-zero unit (never negated for unsigned)
        T two = T.One + T.One;
        T twiceRem = T.Abs(rem) * two;
        return mode switch
        {
            CobolRounding.Truncation or CobolRounding.Prohibited => q,                 // toward zero
            CobolRounding.AwayFromZero => q + step,
            CobolRounding.TowardGreater => !neg ? q + T.One : q,                        // ceiling
            CobolRounding.TowardLesser => neg ? q - T.One : q,                          // floor
            CobolRounding.NearestAwayFromZero => twiceRem >= divisor ? q + step : q,
            CobolRounding.NearestTowardZero => twiceRem > divisor ? q + step : q,
            CobolRounding.NearestEven => twiceRem > divisor || (twiceRem == divisor && q % two != T.Zero) ? q + step : q,
            _ => q,
        };
    }

    /// <summary>10^n as an <see cref="Int128"/> (n in 0..38 — the wide intermediate range, COBOLNET_DESIGN §18 #4)
    /// — a name-stable wrapper over the ONE <see cref="Pow10"/> table (DESIGN-runtime-library §2.3).
    /// Internal: <see cref="CobolEdit.TryFormat"/> uses it for the edited-receiver digit-capacity bound.</summary>
    internal static Int128 Pow10Wide(int n) => Pow10.AsWide(n);
}
