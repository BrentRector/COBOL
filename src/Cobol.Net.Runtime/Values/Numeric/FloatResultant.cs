// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE TRANSFER OF AN ARITHMETIC VALUE INTO A FLOATING-POINT RESULTANT IDENTIFIER (kb/Work PB1196) — the
/// float arm of the arithmetic store, the twin of the fixed-point arm's <c>CobolNum.TryStore</c>/<c>Store</c>.
/// <para>§14.7.4.3 rules 3–10 speak of "the resultant identifier" with no floating-point exemption, and a binary32
/// or binary64 resultant cannot represent most values exactly: 0.1 is not a FLOAT-SHORT value, and 16777217 is not
/// either. So every mode applies here exactly as it does at a decimal scale — rule 10 "If the TRUNCATION phrase is
/// specified or implied, and the arithmetic value cannot be represented exactly in the resultant identifier, the
/// arithmetic value is rounded to the nearest value nearer to zero that can be represented in the resultant
/// identifier" (cite.py-verified), rule 8's TOWARD-GREATER, rule 9's TOWARD-LESSER, rule 3's AWAY-FROM-ZERO, the three
/// NEAREST-* tie rules (4, 5, 6), and rule 7's PROHIBITED — "the EC-SIZE-TRUNCATION exception condition is set to
/// exist, the size error condition exists, and the content of the resultant identifier is unchanged"
/// (cite.py-verified). The store used to be a bare IEEE round-to-nearest-even cast whatever the phrase said, so
/// implied TRUNCATION stored 0.1 into a FLOAT-SHORT as 0.10000000149 — FARTHER from zero than the value.</para>
/// <para>THE CAPACITY HALF is §14.7.5 case 3 — "if, after radix point alignment and any applicable rounding
/// specifications, the result of an arithmetic statement is further from zero than permitted for the associated
/// resultant data item" (cite.py-verified). For a binary format that is ISO/IEC 60559's overflow: the value
/// rounded under the mode with an UNBOUNDED exponent exceeds the format's largest finite magnitude. So a value
/// just past <c>MaxValue</c> TRUNCATES back to <c>MaxValue</c> and fits (exactly as 9.99 truncates to 9.9 in a
/// <c>PIC 9V9</c>), while the same value rounded AWAY-FROM-ZERO is the size error — the exponent, not the
/// significand, is a binary format's "integer part".</para>
/// <para>THE ARITHMETIC VALUE is the one the carrier holds: a scaled <see cref="Int128"/>/<see cref="UInt128"/> or
/// an SDIDI is EXACT, and is converted in ONE rounding (never through a binary64 first — a double rounding lands
/// a value just above a binary32 midpoint on the even side, kb/Work PB1110); a binary64 intermediate is the value
/// native arithmetic produced (CONFORMANCE.md DOC-A.1-123 — an expression whose resultant set is all floating-point
/// evaluates in binary64), so a binary64 resultant holds it exactly under every mode and only a binary32 resultant
/// rounds it.</para>
/// <para>A NON-FINITE value (NaN, ±Infinity) transfers unchanged and is never the size error: it is not a result
/// that is "further from zero" than a finite one, it is the §14.6.13.2 item 3 EC-DATA-NOT-FINITE content its
/// sending read already answers for (CONFORMANCE.md DOC-A.1-70).</para>
/// <para>COST: a binary64 intermediate into a binary64 resultant is an identity; every other landing is the
/// nearest binary64 (<see cref="CobolFloat.ScaledToDouble"/> — one IEEE divide, or a decimal parse past 2^53) plus
/// at most four exact comparisons, which ride <see cref="CobolFloat.TryExactScaled"/>'s allocation-free carrier
/// path for every PICTURE-shaped scale; the tie test is integer arithmetic.</para>
/// </summary>
public static class FloatResultant
{
    // ── The entry points, one per value carrier; each has an UNCHECKED form (the no-phrase store) and a CHECKED
    //    Try form (ON SIZE ERROR / EC-SIZE checking — false is the size error condition, receiver unchanged). ──

    /// <summary>The unchecked landing of a binary64 intermediate: the mode-rounded value (a PROHIBITED-inexact value
    /// lands TRUNCATED, and an overflow lands as ISO/IEC 60559's overflow result for the mode — the no-phrase
    /// disposition, CONFORMANCE.md DOC-A.1-70).</summary>
    public static double FromReal(double v, CobolRounding mode, bool single) => LandReal(v, mode, single, out _);

    /// <summary>The checked landing of a binary64 intermediate: <c>false</c> is the size error condition —
    /// §14.7.5 case 3 (further from zero than the resultant permits) or §14.7.4.3 rule 7 (PROHIBITED, inexact).</summary>
    public static bool TryFromReal(double v, CobolRounding mode, bool single, out double landed)
    {
        landed = LandReal(v, mode, single, out Outcome o);
        return Admits(o, mode);
    }

    /// <summary>The unchecked landing of an exact scaled value <c>unscaled × 10^(−scale)</c>.</summary>
    public static double FromScaled(Int128 unscaled, int scale, CobolRounding mode, bool single) =>
        LandExact(unscaled < 0, CobolDec.UAbs(unscaled), scale, mode, single, out _);

    /// <inheritdoc cref="TryFromReal"/>
    public static bool TryFromScaled(Int128 unscaled, int scale, CobolRounding mode, bool single, out double landed)
    {
        landed = LandExact(unscaled < 0, CobolDec.UAbs(unscaled), scale, mode, single, out Outcome o);
        return Admits(o, mode);
    }

    /// <summary>The unchecked landing of an exact UNSIGNED-wide scaled value (a 16-byte unsigned COMP-5 operand,
    /// whose container range reaches 2^128 − 1 — kb/Work R10).</summary>
    public static double FromUnsignedScaled(UInt128 unscaled, int scale, CobolRounding mode, bool single) =>
        LandExact(false, unscaled, scale, mode, single, out _);

    /// <inheritdoc cref="TryFromReal"/>
    public static bool TryFromUnsignedScaled(UInt128 unscaled, int scale, CobolRounding mode, bool single, out double landed)
    {
        landed = LandExact(false, unscaled, scale, mode, single, out Outcome o);
        return Admits(o, mode);
    }

    /// <summary>The unchecked landing of an SDIDI <c>Sig × 10^Exp</c> (exact — its significand never exceeds 34
    /// digits, so its magnitude always fits the carrier).</summary>
    public static double FromDec(CobolDec v, CobolRounding mode, bool single) =>
        LandExact(v.Sig < 0, CobolDec.UAbs(v.Sig), -v.Exp, mode, single, out _);

    /// <inheritdoc cref="TryFromReal"/>
    public static bool TryFromDec(CobolDec v, CobolRounding mode, bool single, out double landed)
    {
        landed = LandExact(v.Sig < 0, CobolDec.UAbs(v.Sig), -v.Exp, mode, single, out Outcome o);
        return Admits(o, mode);
    }

    // ── The RAISING forms: the third caller shape — an EXPRESSION with no SIZE ERROR phrase to offer and no
    //    arithmetic statement to hang a flag on, compiled when EC-SIZE-TRUNCATION checking is enabled at the
    //    activating statement (§14.7.5 case 3 + no-phrase rule 4; the float twin of CobolNum.StoreOrRaise). Its live
    //    callers are the §14.2.3 GR9/GR10 argument crossings — "a COMPUTE statement without the ROUNDED phrase" into
    //    a floating-point formal — on the CALL lane (CobolArgAdapt.LandForFormal) and the INVOKE lane
    //    (kb/Work PB1114). ──

    /// <summary>The raising landing of a binary64 intermediate.</summary>
    public static double FromRealOrRaise(double v, CobolRounding mode, bool single) =>
        TryFromReal(v, mode, single, out double landed) ? landed : throw CobolSizeError.FloatTruncation(single);

    /// <summary>The raising landing of an exact scaled value.</summary>
    public static double FromScaledOrRaise(Int128 unscaled, int scale, CobolRounding mode, bool single) =>
        TryFromScaled(unscaled, scale, mode, single, out double landed) ? landed : throw CobolSizeError.FloatTruncation(single);

    /// <summary>The raising landing of an exact UNSIGNED-wide scaled value.</summary>
    public static double FromUnsignedScaledOrRaise(UInt128 unscaled, int scale, CobolRounding mode, bool single) =>
        TryFromUnsignedScaled(unscaled, scale, mode, single, out double landed) ? landed : throw CobolSizeError.FloatTruncation(single);

    /// <summary>The raising landing of an SDIDI.</summary>
    public static double FromDecOrRaise(CobolDec v, CobolRounding mode, bool single) =>
        TryFromDec(v, mode, single, out double landed) ? landed : throw CobolSizeError.FloatTruncation(single);

    // ── The rounding ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What a landing did: stored the value itself, rounded it (the mode chose a neighbor), or found it
    /// further from zero than the resultant permits (§14.7.5 case 3).</summary>
    private enum Outcome { Exact, Rounded, Overflow }

    /// <summary>A §14.7.4.3 mode restated on the MAGNITUDE (the sign is re-applied after the rounding): which of the
    /// two representable magnitudes bracketing an inexact value a mode takes, and for the NEAREST-* modes which
    /// one a tie takes.</summary>
    private enum Dir { Down, Up, NearestEven, NearestUp, NearestDown }

    private static Dir DirOf(CobolRounding mode, bool neg) => mode switch
    {
        CobolRounding.Truncation or CobolRounding.Prohibited => Dir.Down,   // r10 (r7 lands truncated when unchecked)
        CobolRounding.AwayFromZero => Dir.Up,                               // r3
        CobolRounding.TowardGreater => neg ? Dir.Down : Dir.Up,             // r8 — the nearest LARGER value
        CobolRounding.TowardLesser => neg ? Dir.Up : Dir.Down,              // r9 — the nearest SMALLER value
        CobolRounding.NearestAwayFromZero => Dir.NearestUp,                 // r4
        CobolRounding.NearestEven => Dir.NearestEven,                       // r5
        CobolRounding.NearestTowardZero => Dir.NearestDown,                 // r6
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "not a §14.7.4.2 rounding mode"),
    };

    /// <summary>Whether a checked store commits: never on an overflow (§14.7.5 case 3), and under PROHIBITED only
    /// when the value was representable exactly (§14.7.4.3 r7).</summary>
    private static bool Admits(Outcome o, CobolRounding mode) =>
        o == Outcome.Exact || (o == Outcome.Rounded && mode != CobolRounding.Prohibited);


    /// <summary>A binary64 intermediate. Into a binary64 resultant it IS representable — every mode stores it
    /// unchanged; into a binary32 resultant it is the exact value <paramref name="v"/> to round (sticky 0).</summary>
    private static double LandReal(double v, CobolRounding mode, bool single, out Outcome outcome)
    {
        outcome = Outcome.Exact;
        if (!single || !double.IsFinite(v) || v == 0) return v;
        bool neg = v < 0;
        return Signed(neg, LandBinary32(Math.Abs(v), 0, DirOf(mode, neg), out outcome));
    }

    /// <summary>An EXACT value <c>±magnitude × 10^(−scale)</c>: its correctly rounded binary64 (the nearest,
    /// ties-to-even) plus the ONE exact comparison that says which side of it the value lies on, then the mode's
    /// choice between the two representable neighbors.</summary>
    private static double LandExact(bool neg, UInt128 mag, int scale, CobolRounding mode, bool single, out Outcome outcome)
    {
        outcome = Outcome.Exact;
        if (mag == 0) return 0.0;
        var x = new ExactMagnitude(mag, scale);
        double near = x.NearestBinary64();
        int sticky = double.IsInfinity(near) ? 1 : -x.SignAgainst(near);   // sign(|V| − near)
        Dir dir = DirOf(mode, neg);
        double landed = single ? LandBinary32(near, sticky, dir, out outcome) : LandBinary64(near, sticky, x, dir, out outcome);
        return Signed(neg, landed);
    }

    private static double Signed(bool neg, double magnitude) => neg ? -magnitude : magnitude;

    /// <summary>2^128 — the first magnitude past binary32's largest finite value, as the virtual "next value up"
    /// from <see cref="float.MaxValue"/> (the ISO/IEC 60559 unbounded-exponent successor).</summary>
    private static readonly double Binary32Top = Math.ScaleB(1.0, 128);

    /// <summary>Round the magnitude <c>near + sticky·ε</c> (<paramref name="near"/> ≥ 0 a binary64 or +∞; ε smaller
    /// than any binary64 spacing, so <paramref name="sticky"/> only breaks a tie or decides an exact landing) to
    /// binary32. Everything is exact in binary64: two adjacent binary32 values and their midpoint all are.</summary>
    private static double LandBinary32(double near, int sticky, Dir dir, out Outcome outcome)
    {
        double lo, hi;
        if (near > Binary32Top || (near == Binary32Top && sticky >= 0))
            return Overflowed(dir, float.MaxValue, out outcome);           // at or past 2^128: every mode overflows
        if (near > float.MaxValue) { lo = float.MaxValue; hi = Binary32Top; }
        else
        {
            double f = (float)near;                                         // a binary32 neighbor of near
            if (f == near)
            {
                if (sticky == 0) { outcome = Outcome.Exact; return f; }
                if (sticky > 0) { lo = f; hi = UpBinary32(f); } else { lo = DownBinary32(f); hi = f; }
            }
            else if (f > near) { lo = DownBinary32(f); hi = f; }
            else { lo = f; hi = UpBinary32(f); }
        }
        double pick = dir switch
        {
            Dir.Down => lo,
            Dir.Up => hi,
            _ => CompareToMidpoint(near, sticky, lo, hi) switch
            {
                < 0 => lo,
                > 0 => hi,
                _ => dir == Dir.NearestUp ? hi
                   : dir == Dir.NearestDown ? lo
                   : (BitConverter.SingleToInt32Bits((float)lo) & 1) == 0 ? lo : hi,   // r5: the even significand
            },
        };
        if (pick == Binary32Top) return Overflowed(dir, float.MaxValue, out outcome);
        outcome = Outcome.Rounded;
        return pick;
    }

    /// <summary>The sign of <c>(near + sticky·ε) − (lo + hi)/2</c> — the midpoint of two adjacent binary32 values is
    /// exact in binary64.</summary>
    private static int CompareToMidpoint(double near, int sticky, double lo, double hi)
    {
        int c = near.CompareTo((lo + hi) * 0.5);
        return c != 0 ? c : sticky;
    }

    private static double UpBinary32(double f) => f == float.MaxValue ? Binary32Top : MathF.BitIncrement((float)f);

    private static double DownBinary32(double f) => MathF.BitDecrement((float)f);

    /// <summary>Round an EXACT magnitude whose correctly rounded (ties-to-even) binary64 is <paramref name="near"/>
    /// and which lies on the <paramref name="sticky"/> side of it. The two representable neighbors are
    /// <paramref name="near"/> and its successor/predecessor; the nearest-even choice is <paramref name="near"/>
    /// itself, and the other two nearest modes differ from it only on an exact tie (<see cref="ExactMagnitude.IsBinary64Tie"/>).</summary>
    private static double LandBinary64(double near, int sticky, in ExactMagnitude x, Dir dir, out Outcome outcome)
    {
        if (double.IsInfinity(near))
        {
            // |V| ≥ MaxValue + ½ulp. At or past 2^1024 every mode overflows; below it the neighbors are MaxValue and
            // the virtual 2^1024, and a nearest mode takes the virtual one unless the value is exactly the tie and
            // the mode rounds a tie toward zero.
            if (x.AtLeastTwoTo1024()) return Overflowed(dir, double.MaxValue, out outcome);
            bool down = dir == Dir.Down || (dir == Dir.NearestDown && x.IsBinary64Tie());
            if (!down) return Overflowed(dir, double.MaxValue, out outcome);
            outcome = Outcome.Rounded;
            return double.MaxValue;
        }
        if (sticky == 0) { outcome = Outcome.Exact; return near; }
        double lo = sticky > 0 ? near : Math.BitDecrement(near);
        double hi = sticky > 0 ? Math.BitIncrement(near) : near;             // MaxValue's successor is +∞ (virtual)
        double pick = dir switch
        {
            Dir.Down => lo,
            Dir.Up => hi,
            Dir.NearestEven => near,
            _ => !x.IsBinary64Tie() ? near : dir == Dir.NearestUp ? hi : lo,
        };
        if (double.IsInfinity(pick)) return Overflowed(dir, double.MaxValue, out outcome);
        outcome = Outcome.Rounded;
        return pick;
    }

    /// <summary>An overflow (§14.7.5 case 3). The checked store rejects it; the unchecked one stores ISO/IEC
    /// 60559's overflow result for the direction — the largest finite magnitude when the mode rounds toward zero,
    /// +∞ otherwise.</summary>
    private static double Overflowed(Dir dir, double max, out Outcome outcome)
    {
        outcome = Outcome.Overflow;
        return dir == Dir.Down ? max : double.PositiveInfinity;
    }

    /// <summary>The exact magnitude <c>Mag × 10^(−Scale)</c> and the three exact questions the rounding asks of it —
    /// all answered in integer arithmetic over the carrier (no arbitrary-precision type: CobolFloat.TryExactScaled's
    /// exact expansion is the ONE place a binary64's decimal value is written down, kb/Work PB623).</summary>
    private readonly record struct ExactMagnitude(UInt128 Mag, int Scale)
    {
        /// <summary>The correctly rounded (ties-to-even) binary64 of the magnitude — <c>CobolFloat.ScaledToDouble</c>,
        /// the ONE scaled→double conversion, or its decimal-string twin past the signed carrier.</summary>
        public double NearestBinary64() => Mag <= (UInt128)Int128.MaxValue
            ? CobolFloat.ScaledToDouble((Int128)Mag, Scale)
            : double.Parse(Mag.ToString(CultureInfo.InvariantCulture) + "E" + (-Scale).ToString(CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>sign(c − magnitude) for a finite binary64 <paramref name="c"/> ≥ 0. A magnitude past the signed
        /// carrier compares HALVES — c/2 is exact (c is nowhere near the subnormals there) and Mag/2 splits into an
        /// integer part and a half bit.</summary>
        public int SignAgainst(double c) => Mag <= (UInt128)Int128.MaxValue
            ? CompareScaled(c, Scale, (Int128)Mag, half: false)
            : CompareScaled(c * 0.5, Scale, (Int128)(Mag >> 1), half: (Mag & 1) != 0);

        /// <summary>|V| ≥ 2^1024 — compared as 2^1023 against |V|/2, both of which the arithmetic here holds.</summary>
        public bool AtLeastTwoTo1024() =>
            CompareScaled(Math.ScaleB(1.0, 1023), Scale, (Int128)(Mag >> 1), half: (Mag & 1) != 0) <= 0;

        /// <summary>True when the magnitude is EXACTLY the midpoint of two adjacent binary64 values (normal or
        /// subnormal, or MaxValue and the virtual 2^1024). A midpoint is a dyadic rational <c>O × 2^e</c> with O odd
        /// sitting half a quantum above a grid point, so a value with any factor of 5 left over (10^−Scale's odd
        /// cofactor not cancelled by the digits) can never be one — which is why this is integer arithmetic.</summary>
        public bool IsBinary64Tie()
        {
            UInt128 w = Mag;
            int e2;
            if (Scale > 0)
            {
                if (Scale > 55) return false;                               // 5^56 > 2^128 > Mag: no multiple of it
                UInt128 p5 = Scale <= 54 ? (UInt128)Pow10.FiveAsWide(Scale) : (UInt128)Pow10.FiveAsWide(54) * 5;
                if (w % p5 != 0) return false;
                w /= p5;
                e2 = -Scale;
            }
            else e2 = -Scale;                                               // 10^k = 5^k × 2^k (k = −Scale ≥ 0)
            int tz = (int)UInt128.TrailingZeroCount(w);
            w >>= tz;
            e2 += tz;
            if (Scale < 0)
            {
                // The odd part gains 5^k; a binary64 midpoint's odd part has at most 54 bits, and 5^24 > 2^55.
                int k = -Scale;
                if (k > 23) return false;
                UInt128 p5 = (UInt128)Pow10.FiveAsWide(k);
                if (w > (UInt128.One << 54) / p5) return false;
                w *= p5;
            }
            int bits = 128 - (int)UInt128.LeadingZeroCount(w);
            // V = w·2^e2 (w odd). The grid quantum at V's binade is 2^max(e2 + bits − 53, −1074); a midpoint sits
            // exactly half a quantum off the grid.
            return e2 == Math.Max(e2 + bits - 53, -1074) - 1;
        }

        /// <summary>sign(x·10^s − (h + half/2)) for a finite binary64 <paramref name="x"/> ≥ 0 and an integer
        /// <paramref name="h"/> ≥ 0 — through <see cref="CobolFloat.TryExactScaled"/>'s exact expansion of x.</summary>
        private static int CompareScaled(double x, int s, Int128 h, bool half)
        {
            if (!CobolFloat.TryExactScaled(x, s, CobolRounding.TowardLesser, out Int128 floor)) return 1;   // past the carrier
            if (floor != h) return floor > h ? 1 : -1;                      // |x·10^s − floor| < 1, and h is an integer
            bool integral = CobolFloat.TryExactScaled(x, s, CobolRounding.TowardGreater, out Int128 ceil) && ceil == floor;
            if (integral) return half ? -1 : 0;
            if (!half) return 1;                                            // x·10^s ∈ (h, h + 1)
            // x·10^s ∈ (h, h + 1) against h + ½: the two nearest roundings disagree exactly on the half. Each lands
            // on h or h + 1, and a landing the carrier cannot hold is h + 1 = 2^127 (h is Int128.MaxValue there).
            bool awayUp = !CobolFloat.TryExactScaled(x, s, CobolRounding.NearestAwayFromZero, out Int128 away) || away != h;
            bool towardUp = !CobolFloat.TryExactScaled(x, s, CobolRounding.NearestTowardZero, out Int128 toward) || toward != h;
            return awayUp != towardUp ? 0 : awayUp ? 1 : -1;
        }
    }
}
