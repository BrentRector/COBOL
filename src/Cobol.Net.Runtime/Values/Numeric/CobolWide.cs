// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE EXACT WIDE INTERMEDIATE OF NATIVE ARITHMETIC (kb/Work PB1900): a signed 256-bit integer — sign and a
/// <see cref="UInt128"/> pair of magnitude — that carries an UNSCALED value whose decimal scale the compiler tracks
/// (exactly as it tracks the scale of the <see cref="Int128"/> lane). It exists so that a native product past the
/// <see cref="Int128"/> carrier, NESTED in a larger expression, keeps every digit until the one final transfer
/// (§14.7.7 rule 3 NOTE 1: ROUNDED governs only that transfer): <c>COMPUTE R = A * B - C * D</c> over
/// <c>PIC 9(21)</c> operands forms two 41-digit products and subtracts them EXACTLY. The 34-digit SDIDI
/// (<see cref="CobolDec.MulToOdd"/>) it replaces rounded each product first, and the cancellation of the two nearly
/// equal products left only the rounding error.
/// <para>The implementor's choice (§8.8.1.3 — "Native arithmetic is an implementor-defined method of evaluating an
/// arithmetic expression"; §14.7.7 4) a) — "an implementor-defined intermediate data item is used"; CONFORMANCE.md
/// DOC-A.1-123) follows GnuCOBOL (CLAUDE.md rule 1), whose <c>cob_decimal_mul</c>, <c>cob_decimal_add</c> and
/// <c>cob_decimal_sub</c> are exact over an arbitrary-precision integer. This type is that integer, bounded: the
/// compiler selects it only where its digit-bound analysis PROVES the exact value fits <see cref="MaxDigits"/>
/// decimal digits (the bound is a property of the operands' PICTUREs, not of their values), and falls back to the
/// SDIDI's round-to-odd beyond it. Every constructive operation (<see cref="Mul"/>, <see cref="Up"/>, <see cref="Add"/>,
/// <see cref="Sub"/>) also checks its own overflow and raises EC-SIZE-OVERFLOW (§14.7.5 case 5, A.1 item 179), so the bound
/// is a compile-time optimisation of a run-time guarantee: this type never wraps.</para>
/// <para>It is NOT a carrier a consumer ever sees. <c>NumericRenderer</c> settles it at every public entry — into the
/// receiver's own scale and mode at a final transfer (<see cref="ToUnscaled"/>), into the SDIDI otherwise
/// (<see cref="ToDec"/>) — so the rest of the compiler keeps its three carriers.</para>
/// </summary>
public readonly struct CobolWide
{
    /// <summary>The widest decimal magnitude every value of which fits the 256-bit magnitude: 10^77 &lt; 2^256 ≈ 1.158×10^77,
    /// so any value below 10^77 is representable. The compiler's digit-bound analysis (<c>NumX.Digits</c>) selects this
    /// type only for bounds up to this width.</summary>
    public const int MaxDigits = 77;

    private readonly UInt128 _hi;
    private readonly UInt128 _lo;
    private readonly bool _negative;

    private CobolWide(bool negative, UInt128 hi, UInt128 lo)
    {
        _hi = hi;
        _lo = lo;
        _negative = negative && (hi != 0 || lo != 0);   // zero has no sign
    }

    private bool IsZero => _hi == 0 && _lo == 0;

    private int Sign => IsZero ? 0 : _negative ? -1 : 1;

    /// <summary>An <see cref="Int128"/> value (any scale) as a wide value of the same unscaled magnitude.</summary>
    public static CobolWide From(Int128 value) => new(value < 0, 0, CobolDec.UAbs(value));

    private static CobolSizeError Overflow() => new("a native intermediate exceeds the exact 256-bit range "
        + "(ISO §14.7.5 case 5 — the implementor-defined intermediate range is checked, A.1 item 179: EC-SIZE-OVERFLOW)",
        "EC-SIZE-OVERFLOW");

    public static CobolWide Negate(CobolWide a) => new(!a._negative, a._hi, a._lo);

    /// <summary>The exact product of the unscaled values (the caller adds the scales).</summary>
    public static CobolWide Mul(CobolWide a, CobolWide b) =>
        TryMul(a, b, out CobolWide product) ? product : throw Overflow();

    private static bool TryMul(CobolWide a, CobolWide b, out CobolWide product)
    {
        product = default;
        if (a.IsZero || b.IsZero) return true;
        if (a._hi != 0 && b._hi != 0) return false;                       // both past 2^128: the product is past 2^256
        var (hi, lo) = CobolDec.Mul128(a._lo, b._lo);
        if (a._hi != 0 || b._hi != 0)
        {
            // (ah·2^128 + al)·bl = ah·bl·2^128 + al·bl — the cross term must itself fit 128 bits, and so must its sum.
            var (crossHi, crossLo) = a._hi != 0 ? CobolDec.Mul128(a._hi, b._lo) : CobolDec.Mul128(a._lo, b._hi);
            if (crossHi != 0) return false;
            UInt128 sum = hi + crossLo;
            if (sum < hi) return false;
            hi = sum;
        }
        product = new CobolWide(a._negative != b._negative, hi, lo);
        return true;
    }

    /// <summary><paramref name="a"/> × 10^<paramref name="digits"/> — the alignment of the lower-scaled operand of a sum
    /// to the higher scale.</summary>
    public static CobolWide Up(CobolWide a, int digits) =>
        TryUp(a, digits, out CobolWide aligned) ? aligned : throw Overflow();

    private static bool TryUp(CobolWide a, int digits, out CobolWide aligned)
    {
        aligned = a;
        while (digits > 0)
        {
            int step = Math.Min(digits, 38);
            if (!TryMul(aligned, From(Pow10.AsWide(step)), out aligned)) return false;
            digits -= step;
        }
        return true;
    }

    /// <summary>The exact sum of two unscaled values ALREADY at one scale (the compiler aligns with <see cref="Up"/>).</summary>
    public static CobolWide Add(CobolWide a, CobolWide b)
    {
        if (a._negative == b._negative || a.IsZero || b.IsZero)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            UInt128 lo = a._lo + b._lo;
            UInt128 carry = lo < a._lo ? 1u : 0u;
            UInt128 hi = a._hi + b._hi;
            bool over = hi < a._hi;
            UInt128 hiCarried = hi + carry;
            if (over || hiCarried < hi) throw Overflow();
            return new CobolWide(a._negative, hiCarried, lo);
        }
        // Opposite signs: the larger magnitude less the smaller, carrying the larger one's sign.
        int order = CompareMagnitude(a, b);
        if (order == 0) return default;
        (CobolWide big, CobolWide small) = order > 0 ? (a, b) : (b, a);
        UInt128 dlo = big._lo - small._lo;
        UInt128 borrow = big._lo < small._lo ? 1u : 0u;
        return new CobolWide(big._negative, big._hi - small._hi - borrow, dlo);
    }

    /// <summary>The exact difference of two unscaled values already at one scale.</summary>
    public static CobolWide Sub(CobolWide a, CobolWide b) => Add(a, Negate(b));

    private static int CompareMagnitude(CobolWide a, CobolWide b) =>
        a._hi != b._hi ? (a._hi < b._hi ? -1 : 1)
        : a._lo != b._lo ? (a._lo < b._lo ? -1 : 1)
        : 0;

    /// <summary>The algebraic comparison (−1/0/+1) of two scaled values — exact, and never a size error: an operand whose
    /// alignment to the other's scale would leave the 256-bit range is larger in magnitude than the other BY THAT FACT.</summary>
    public static int Compare(CobolWide a, int aScale, CobolWide b, int bScale)
    {
        int sa = a.Sign, sb = b.Sign;
        if (sa != sb) return sa < sb ? -1 : 1;
        if (sa == 0) return 0;
        int magnitude;
        if (aScale == bScale) magnitude = CompareMagnitude(a, b);
        else if (aScale < bScale) magnitude = TryUp(a, bScale - aScale, out CobolWide aUp) ? CompareMagnitude(aUp, b) : 1;
        else magnitude = TryUp(b, aScale - bScale, out CobolWide bUp) ? CompareMagnitude(a, bUp) : -1;
        return sa < 0 ? -magnitude : magnitude;
    }

    /// <summary>⛔ THE FINAL TRANSFER (kb/Work PB1900): this value, at <paramref name="scale"/>, moved to the receiver's
    /// <paramref name="toScale"/> and rounded ONCE with the receiver's <paramref name="mode"/> —
    /// <see cref="CobolDec.TransferWide"/>, the kernel <see cref="CobolDec.MulAtScale"/> shares, with its two statement
    /// dispositions (<paramref name="checkedTransfer"/>: a value past the carrier is EC-SIZE-TRUNCATION and PROHIBITED
    /// raises on an inexact one; unchecked, the low-order digits a store can use).</summary>
    public Int128 ToUnscaled(int scale, int toScale, CobolRounding mode, bool checkedTransfer) =>
        IsZero ? 0 : CobolDec.TransferWide(_negative, _hi, _lo, scale - toScale, mode, checkedTransfer);

    /// <summary>This value, at <paramref name="scale"/>, as the SDIDI reduced to 34 significant digits by ROUND-TO-ODD
    /// (<see cref="CobolDec.MulToOdd"/>'s rule): the lowering a consumer that has no wide form takes, so that
    /// whatever rounds it next, in any mode, sees the exact value on the same side of every boundary.</summary>
    public CobolDec ToDec(int scale) => IsZero ? new CobolDec(0, 0) : CobolDec.FromWideToOdd(_negative, _hi, _lo, -scale);

    /// <summary>This value, at <paramref name="scale"/>, as binary64 (through the round-to-odd SDIDI, as every exact
    /// value reaches the float lane).</summary>
    public double ToDouble(int scale) => ToDec(scale).ToDouble();
}
