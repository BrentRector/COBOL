// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The floating-point (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) value helpers (numeric design D16). A float
/// elementary item holds a native IEEE <see cref="float"/>/<see cref="double"/>; the arithmetic pipeline evaluates
/// any float-bearing expression in binary64. This class is the boundary between that native-double world and the
/// scaled-integer substrate (<see cref="CobolNum"/>): <see cref="ToScaled"/> lands a double into an unscaled
/// <see cref="Int128"/> at a receiver's scale (so the existing store funnel applies ROUNDED + SIZE ERROR), and
/// <see cref="Display(double)"/> renders the algebraic value for DISPLAY (ISO §14.9.11 GR1 — implementor-defined).
/// </summary>
public static class CobolFloat
{
    /// <summary>DISPLAY of a float item (§14.9.11 GR1, implementor-defined): the .NET shortest round-trippable
    /// decimal in the invariant culture (a leading '-' only when negative; E-notation for extreme magnitudes, as
    /// .NET produces). The ONE float→string path — never a bare <c>.ToString()</c>.</summary>
    public static string Display(float v) => v.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc cref="Display(float)"/>
    public static string Display(double v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>⛔ THE ONE scaled-value→double conversion (kb/Work PB115): the CORRECTLY-ROUNDED double of the
    /// algebraic value <c>unscaled × 10^(−scale)</c>. Two paths, both single-rounded: when the unscaled magnitude
    /// fits 2^53 and |scale| ≤ 22, both operands are EXACT doubles and one IEEE divide/multiply rounds once; past
    /// either bound, the decimal-string round-trip (<see cref="double.Parse(string)"/> is IEEE correctly rounded
    /// in this runtime). It replaced <c>(double)(x) / 10^scale</c> with an emit-time repeated-multiplication
    /// divisor — exact only through 1e22, so at scale ≥ 23 the divisor sat one ulp low and a LEGAL
    /// <c>ASIN(|x| ≤ 1)</c> argument (§15.10.3 r2) arrived above 1.0, evaluated NaN and drew the §15.3
    /// EC-ARGUMENT-FUNCTION where §15.10.4 r1 requires an approximation of the arcsine; <c>CobolDec.ToDouble</c>'s
    /// <c>(double)Sig * Math.Pow(10, Exp)</c> failed independently at other scales. A NEGATIVE scale (a PICTURE-P
    /// trailing-scaled operand, or an SDIDI with a positive exponent) multiplies: 10^(−scale).</summary>
    public static double ScaledToDouble(Int128 unscaled, int scale)
    {
        if (scale == 0) return (double)unscaled;
        // ⛔ The magnitude test is on the UNSIGNED magnitude (kb/Work PB2641): negating the signed value leaves
        // Int128.MinValue (-2^127, a legal signed 16-byte COMP-5 value) unchanged and NEGATIVE, so it passed
        // "≤ 2^53" and (long)unscaled kept its low 64 bits — zero. CobolDec.UAbs is the MinValue-safe magnitude.
        if (CobolDec.UAbs(unscaled) <= (1UL << 53) && scale is >= -22 and <= 22)
        {
            double v = (double)(long)unscaled;                 // exact: |value| ≤ 2^53
            return scale > 0 ? v / ExactPow10[scale] : v * ExactPow10[-scale];
        }
        // The slow path: one correctly-rounded decimal parse of "<unscaled>E<-scale>".
        return double.Parse(
            unscaled.ToString(CultureInfo.InvariantCulture) + "E" + (-scale).ToString(CultureInfo.InvariantCulture),
            NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>⛔ THE ONE scaled-value→binary32 conversion (kb/Work PB1110): the CORRECTLY-ROUNDED binary32 of the
    /// algebraic value <c>unscaled × 10^(−scale)</c> in ONE rounding (ISO §14.6.8.3 rule 2: "in a manner consistent
    /// with the specifications of ISO/IEC 60559:2020", one conversion under roundTiesToEven). Narrowing
    /// <see cref="ScaledToDouble"/> is a SECOND rounding: a value a hair above a binary32 midpoint rounds onto the
    /// midpoint in binary64 and the cast then ties to even, storing the lower neighbour. When the unscaled magnitude
    /// fits 2^24 and |scale| ≤ 10 both operands are exact binary32 values (10^10 = 2^10·5^10, 5^10 &lt; 2^24), so one
    /// IEEE single divide/multiply rounds once; past either bound, the decimal-string parse
    /// (<see cref="float.Parse(string)"/> is IEEE correctly rounded in this runtime). ±Infinity is the overflow
    /// result — the caller's checked store decides whether that raises.</summary>
    public static float ScaledToSingle(Int128 unscaled, int scale)
    {
        if (unscaled == 0) return 0f;
        if (CobolDec.UAbs(unscaled) <= (1UL << 24) && scale is >= -10 and <= 10)   // unsigned magnitude: MinValue-safe (PB2641)
        {
            float v = (float)(int)unscaled;                    // exact: |value| ≤ 2^24
            return scale > 0 ? v / ExactPow10Single[scale] : v * ExactPow10Single[-scale];
        }
        return float.Parse(
            unscaled.ToString(CultureInfo.InvariantCulture) + "E" + (-scale).ToString(CultureInfo.InvariantCulture),
            NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static readonly float[] ExactPow10Single =
        [1f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f, 1e7f, 1e8f, 1e9f, 1e10f];

    /// <summary>⛔ THE ONE WAY GENERATED CODE MATERIALIZES A FLOAT FROM ITS INTERCHANGE BITS (ISO §14.9.39.4 rules
    /// 33–35 — the SET Format-15 canonical values; kb/Work PB961). An OPAQUE call, deliberately: a
    /// <c>BitConverter.Int32BitsToSingle(&lt;constant&gt;)</c> written inline in the generated C# is a JIT
    /// constant, and RyuJIT carries a floating constant as a <c>double</c> — so an optimized build folded the
    /// binary32 signaling NaN <c>0x7F800001</c> through a binary32 → binary64 conversion and stored the QUIET
    /// <c>0x7FC00001</c> (measured: Release, x64, .NET 10; the unoptimized build kept the bits). No arithmetic and
    /// no conversion can reach the bits through a <see cref="MethodImplOptions.NoInlining"/> call whose argument
    /// is an integer, so rule 35's "canonical representation of a signaling NaN" survives by construction, not
    /// by the JIT's current folding policy. The binary64 twin is not affected today and takes the same shape so
    /// there is one mechanism, not a width-dependent pair.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static float FromBinary32Bits(uint bits) => BitConverter.UInt32BitsToSingle(bits);

    /// <inheritdoc cref="FromBinary32Bits"/>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static double FromBinary64Bits(ulong bits) => BitConverter.UInt64BitsToDouble(bits);

    /// <summary>10^0 … 10^22 — every power of ten a double represents EXACTLY.</summary>
    private static readonly double[] ExactPow10 =
    [
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
        1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
    ];

    /// <summary>The checked read of a standard-float SENDING operand (ISO §14.6.13.2 item 3): return the value, but
    /// when EC-DATA-NOT-FINITE checking is enabled and the content is NaN or ±Infinity, raise the fatal
    /// EC-DATA-NOT-FINITE (via <see cref="ExceptionState.FloatNotFiniteError"/>). Always-emitted at the two float
    /// sending-read chokepoints (the numeric-value read and the string-image read), mirroring the always-emitted
    /// <c>CobolString.RefMod</c>: the fast path is a single <c>IsFinite</c> test then an immediate return
    /// (JIT-inlinable), so a directive-free run pays only that test and is byte-behaviour-identical. The four
    /// exemptions (class condition, sign condition, same-usage MOVE, VALIDATE) are realized as a RAW read at those
    /// sites, so this wrap never appears there.</summary>
    public static double Sending(double v)
    {
        if (!double.IsFinite(v))
            ExceptionState.FloatNotFiniteError("a NaN or infinite standard-float sending operand was referenced (ISO §14.6.13.2 item 3)");
        return v;
    }

    /// <inheritdoc cref="Sending(double)"/>
    public static float Sending(float v)
    {
        if (!float.IsFinite(v))
            ExceptionState.FloatNotFiniteError("a NaN or infinite standard-float sending operand was referenced (ISO §14.6.13.2 item 3)");
        return v;
    }

    /// <summary>The checked store of a MOVE algebraic value that is a FIXED-POINT scaled sender into a SINGLE-precision
    /// receiver, converted ONCE by <see cref="ScaledToSingle"/> (kb/Work PB1110). A scaled value is never
    /// ±Infinity, so an infinite result IS the overflow past the single-precision range (§14.9.25.4 GR6 d)4.a).</summary>
    public static float StoreScaledSingleChecked(float converted)
    {
        if (float.IsInfinity(converted))
            ExceptionState.FloatOverflowError("a MOVE algebraic value overflows the single-precision float receiver (ISO §14.9.25.4 GR6 d)4.a)");
        return converted;
    }

    /// <summary>The checked store of a MOVE algebraic value into a SINGLE-precision float receiver (ISO
    /// §14.9.25.4 GR6 d)4.a): cast to <see cref="float"/> and, when a FINITE source overflows the single-precision
    /// exponent range to ±Infinity and EC-DATA-OVERFLOW checking is enabled, raise the fatal EC-DATA-OVERFLOW (via
    /// <see cref="ExceptionState.FloatOverflowError"/>). The <c>double.IsFinite(src)</c> guard keeps a NaN/±Infinity
    /// source out of overflow (that is EC-DATA-NOT-FINITE at the sending read, or the valid §14.6.8.3 GR1 result under
    /// checking OFF). The test is cast-based — never <c>Math.Abs(src) &gt; float.MaxValue</c>, since a double in
    /// (float.MaxValue, ~3.4028235678e38] rounds to a FINITE <c>float.MaxValue</c>, not ±Infinity.
    /// <para>⛔ THIS OVERLOAD CANNOT SEE A DECIMAL SENDER'S MAGNITUDE — use <see cref="StoreChecked(CobolDec,bool)"/>
    /// for one. A binary64 conversion happens before this is called, so an SDIDI past binary64's range arrives
    /// ALREADY ±Infinity and <c>double.IsFinite(src)</c> is false.</para></summary>
    public static float StoreSingleChecked(double src)
    {
        float r = (float)src;
        if (double.IsFinite(src) && float.IsInfinity(r))
            ExceptionState.FloatOverflowError("a MOVE algebraic value overflows the single-precision float receiver (ISO §14.9.25.4 GR6 d)4.a)");
        return r;
    }

    /// <summary>The checked store of a MOVE algebraic value that is a STANDARD-DECIMAL intermediate into a float
    /// receiver (ISO §14.9.25.4 GR6 d)4.a; kb/Work PB271).
    /// <para>⛔ THE TEST IS ON THE ALGEBRAIC VALUE, WHICH MEANS IT MUST HOLD THE <see cref="CobolDec"/> — after
    /// <see cref="CobolDec.ToDouble"/> the magnitude is gone. An SDIDI reaches ±9.999…E+6144, twenty decades past
    /// binary64 and 6100 past binary32, so <c>MOVE 1.0E+400 TO F</c> collapsed to ±Infinity at the conversion and
    /// BOTH float arms then failed to notice: the double arm was a bare cast on the premise that "a double receiver
    /// cannot overflow from a finite double" (true, but the sender reaching it stopped being a finite double), and
    /// <see cref="StoreSingleChecked(double)"/>'s finite-source guard was already false. GR6 d)4.a requires the
    /// fatal EC-DATA-OVERFLOW here: "If the algebraic value of the sending operand is farther from zero than is
    /// permitted by the usage specifications of the receiving data item, the EC-DATA-OVERFLOW exception condition
    /// is set to exist, and the content of the receiving data item is undefined" (cite.py-verified).</para>
    /// <para>A <see cref="CobolDec"/> has no infinity, so <c>IsInfinity</c> on the CORRECTLY-ROUNDED conversion is
    /// exactly "farther from zero than the usage permits" — a value between the receiver's maximum and the next
    /// representable magnitude rounds back to that maximum and is NOT an overflow, which a
    /// <c>&gt; MaxValue</c> comparison would get wrong.</para>
    /// <para>SCOPE. GR6 d)4.a binds a receiver "described with a standard floating-point usage" — FLOAT-BINARY-32
    /// and FLOAT-BINARY-64 here (§13.18.60.4 GR14/GR15). For COMP-1/COMP-2/FLOAT-SHORT/FLOAT-LONG/FLOAT-EXTENDED
    /// §14.6.8.3 rule 1 instead says "the implementor specifies any exception conditions that might be set to
    /// exist during data conversion" (cite.py-verified), and WiseOwl COBOL's determination is the SAME condition —
    /// one rule for every float receiver, published in CONFORMANCE.md. The single-precision arm has raised it for
    /// COMP-1 since D21, so this makes the family consistent rather than adding a second regime.</para></summary>
    public static double StoreChecked(CobolDec src, bool single)
    {
        // ONE rounding into the receiver's own format (kb/Work PB1110) — a binary32 receiver never sees a binary64 detour.
        double r = single ? src.ToSingle() : src.ToDouble();
        if (double.IsInfinity(r))
            ExceptionState.FloatOverflowError("a MOVE algebraic value is farther from zero than the "
                + (single ? "single" : "double") + "-precision float receiver's usage permits "
                + "(ISO §14.9.25.4 GR6 d)4.a)");
        return r;
    }

    // ── THE ONE binary64 → scaled-integer landing (kb/Work PB623) ────────────────────────────────────────────

    /// <summary>The EXACT value of a finite binary64 at <paramref name="scale"/> fraction digits, rounded per
    /// <paramref name="mode"/>, WHEN that value fits the <see cref="Int128"/> carrier — the ONE landing every
    /// float→fixed transfer is defined by (kb/Work PB623). ISO §14.6.8.2 rule 1 makes it a rule and not a
    /// latitude: "If the sending operand is an intermediate data item or a data item described with a standard
    /// floating-point usage, the value is treated as if it had been converted to a fixed-point value"
    /// (cite.py-verified) — THE VALUE, which for a binary64 is the exact ±man·2^exp it holds and always a
    /// terminating decimal, never a re-rounded surrogate for it. Rule 2 leaves a FLOAT-SHORT/-LONG/-EXTENDED
    /// sender's conversion to the implementor ("the implementor defines the manner in which the value is
    /// converted to a fixed-point value", cite.py-verified) and WiseOwl COBOL's determination is that SAME exact
    /// conversion — one rule for every float sender rather than two regimes.
    /// <para>⛔ WHY NOT <c>v * 10^scale</c> IN BINARY64, which is what this replaced. That product is ITSELF a
    /// rounded double once |v|·10^scale passes 2^53, so the landing answered with a value the sender never held —
    /// and the two entry points rounded that surrogate at different scales, which is how ONE returned value
    /// reached two receivers differently: <c>MOVE FUNCTION TAN(x) TO PIC S9(28)V99</c> stored 16331239353195368.96
    /// where <c>COMPUTE</c> of the same call stored 16331239353195369.92 and the returned value is exactly
    /// 16331239353195370. §15.4.1 — "the returned value is the same for all instances of a given function within a
    /// single execution of the runtime element so long as the value and order of the arguments, the collating
    /// sequence, and the locale are unchanged" (cite.py-verified) — cannot survive two transfers that disagree,
    /// and its NATIVE latitude is over "the characteristics and representation of the returned value", not over
    /// two different transfers of one representation into one receiver.</para>
    /// <para>THE FAST PATH IS A PREDICATE WITH A PROOF, not a magnitude guess. A finite double is ±man·2^exp
    /// exactly with man &lt; 2^53 (<see cref="Decompose"/>), so v·10^scale = ±man·5^scale·2^(exp+scale): ONE
    /// integer multiply and ONE shift, both exact in the carrier whenever man·5^scale fits it. man &lt; 2^53 and
    /// 5^31 &lt; 2^72 give man·5^scale &lt; 2^125 &lt; 2^127 for every scale ≤ 31 — which covers every PICTURE
    /// fraction, since §13.18.40.3 rule 14 caps a PICTURE at 31 digit positions ("For data items of category
    /// numeric, and for fixed-point data items of category numeric-edited, the number of digit positions described
    /// by character-string-1 shall range from 1 through 31", cite.py-verified). A wider scale, a shift
    /// past the carrier, or a negative scale (a trailing-P receiver, whose 5^|scale| is a DIVISOR) falls to the
    /// same expansion over <see cref="BigInteger"/>. No binary64 multiply is left anywhere in the landing.</para>
    /// <para>ROUNDING MODE PROHIBITED lands here TRUNCATED (<c>RoundDiv</c>'s truncation arm); the emitter gates
    /// the STORE with <see cref="InexactAtScale"/> so an inexact float→fixed transfer raises SIZE ERROR and leaves
    /// the receiver unchanged (§14.7.4.3 item 7) before this is reached.</para></summary>
    /// <returns><c>true</c> with the landed value; <c>false</c> when <paramref name="v"/> is not finite or its
    /// exact expansion does not fit the carrier — the caller then applies ITS landing form (kb/Work PB77: the
    /// CHECKED landing saturates so the store's capacity check raises the size error; the UNCHECKED one keeps the
    /// LOW-ORDER digits, which have no sentinel to truncate).</returns>
    public static bool TryExactScaled(double v, int scale, CobolRounding mode, out Int128 landed)
    {
        landed = Int128.Zero;
        if (!double.IsFinite(v)) return false;
        var (man, exp, neg) = Decompose(v);
        if (man == 0) return true;                                  // ±0 lands zero at every scale and every mode
        if ((uint)scale <= FastScaleMax)
        {
            Int128 p = (Int128)man * Pow10.FiveAsWide(scale);       // exact: man < 2^53 and 5^31 < 2^72
            int e2 = exp + scale;
            if (e2 >= 0)
            {
                // p·2^e2 is an INTEGER — every rounding mode agrees on it; it either fits the carrier or the
                // caller's landing form answers.
                if (e2 > 126 || p > Int128.MaxValue >> e2) return false;
                p <<= e2;
                landed = neg ? -p : p;
                return true;
            }
            if (e2 >= -126)                                          // 2^-e2 is representable as the divisor
            {
                landed = CobolNum.RoundDiv(neg ? -p : p, Int128.One << -e2, mode);
                return true;
            }
        }
        BigInteger exact = ExactScaled(v, scale, mode);
        if (exact < MinWide || exact > MaxWide) return false;
        landed = (Int128)exact;
        return true;
    }

    /// <summary>The widest scale <see cref="TryExactScaled"/>'s carrier fast path is PROVEN exact for: 5^31 &lt;
    /// 2^72 and a significand below 2^53 put man·5^scale below 2^125. Raising it needs the proof re-run, not a
    /// bigger number.</summary>
    private const uint FastScaleMax = 31;

    private static readonly BigInteger MaxWide = (BigInteger)Int128.MaxValue;
    private static readonly BigInteger MinWide = (BigInteger)Int128.MinValue;
    private static readonly BigInteger Pow10Big38 = BigInteger.Pow(10, 38);

    /// <summary>The EXACT decomposition of a finite binary64: <c>v = (neg ? −1 : +1) × man × 2^exp</c>, with the
    /// significand's trailing zero bits shifted into the exponent so <c>man</c> is ODD (or zero, for ±0). Every
    /// exact statement about a double's value in this file is built from this ONE decomposition — the landing, its
    /// residue past the carrier, and the PROHIBITED gate's divisibility question.</summary>
    private static (long man, int exp, bool neg) Decompose(double v)
    {
        long bits = BitConverter.DoubleToInt64Bits(v);
        bool neg = bits < 0;
        int biased = (int)((bits >> 52) & 0x7FF);
        long man = bits & 0xF_FFFF_FFFF_FFFFL;
        if (biased == 0) biased = 1; else man |= 1L << 52;          // subnormal / normal significand
        if (man == 0) return (0, 0, neg);
        int tz = System.Numerics.BitOperations.TrailingZeroCount(man);
        return (man >> tz, biased - 1075 + tz, neg);
    }

    /// <summary>The EXACT value of a finite binary64 at <paramref name="scale"/> fraction digits as a signed
    /// numerator over a POSITIVE denominator: <c>v·10^scale = ±man·5^scale·2^(exp+scale)</c>, with a negative
    /// scale's 5^|scale| and a negative exp+scale's 2^|exp+scale| joining the denominator. ⛔ THE ONE PLACE the
    /// exact value of a binary64 at a decimal scale is written down — the landing ROUNDS this ratio, the
    /// PROHIBITED gate asks whether it DIVIDES. <see cref="Decompose"/> leaves the numerator odd in its factor 2,
    /// so the ratio is already in lowest terms as to 2 and "the denominator is 1" is exactly "the value has no
    /// digits past this scale".</summary>
    private static (BigInteger num, BigInteger den) ExactRatio(double v, int scale)
    {
        var (man, exp, neg) = Decompose(v);
        BigInteger num = neg ? -man : man, den = BigInteger.One;
        if (scale >= 0) num *= BigInteger.Pow(5, scale); else den = BigInteger.Pow(5, -scale);
        int e2 = exp + scale;
        if (e2 >= 0) num <<= e2; else den <<= -e2;
        return (num, den);
    }

    /// <summary>The EXACT value of a finite binary64 at <paramref name="scale"/> fraction digits, rounded per
    /// <paramref name="mode"/>, over arbitrary precision — the DEFINITION that <see cref="TryExactScaled"/>'s
    /// carrier arm is an allocation-free optimization of, and the residue path for a value the carrier cannot
    /// hold. The quotient rounds through the ONE <c>CobolNum.RoundDiv</c> kernel (its divisor is positive by
    /// construction), so a mode is never re-implemented here. Cold by construction: the fast path answers every
    /// PICTURE-shaped scale inside the carrier.</summary>
    private static BigInteger ExactScaled(double v, int scale, CobolRounding mode)
    {
        var (num, den) = ExactRatio(v, scale);
        return den.IsOne ? num : CobolNum.RoundDiv(num, den, mode);
    }

    /// <summary>Convert a native double to an UNSCALED <see cref="Int128"/> at <paramref name="scale"/> fraction
    /// digits, rounded per <paramref name="mode"/> — the CHECKED double→scaled-integer landing for a store INTO a
    /// fixed-point receiver (D16). The result then flows through the existing <c>CobolNum.Store</c>/<c>TryStore</c>
    /// funnel (whose rescale is identity, since we land AT the receiver scale — no double-rounding), which applies
    /// the digit capacity + SIZE ERROR check. NaN → 0 (implementor-defined; the resulting 0 is in range and exact,
    /// so the store commits it silently — NO SIZE ERROR / EC-SIZE is raised for a NaN source). ±Infinity and any
    /// magnitude beyond the wide engine SATURATE to <see cref="Int128.MaxValue"/>/<see cref="Int128.MinValue"/> so
    /// that capacity check fires SIZE ERROR reliably — never a silent-wrong store.
    /// <para>⛔ THIS IS THE CHECKED LANDING (kb/Work PB77) — the form for a store whose capacity check RAISES: an
    /// arithmetic statement under ON SIZE ERROR / EC-SIZE checking (§14.7.5 case 3), and every intermediate consumer
    /// with no capacity check downstream (an alignment, an argument), where a huge sentinel is the loud answer. A
    /// TRUNCATING landing — a MOVE (§14.6.8.2 r4: "truncation on either end"), the no-phrase arithmetic store
    /// (§14.6.13.1.3 item 8 — "the implementor defines … how any receiving operands are affected", and WiseOwl COBOL's
    /// documented disposition is the low-order digits), INVOKE BY CONTENT — takes <see cref="ToScaledUnchecked"/>
    /// instead: it has no check to see the sentinel, and truncating a sentinel stores garbage
    /// (<c>MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)</c> stored 884105727, the low digits of
    /// <c>Int128.MaxValue</c>). The SDIDI carrier's <c>CobolDec.ToUnscaledChecked</c> / <c>ToUnscaled</c> pair is the
    /// same two-form rule (PB74). The two forms differ ONLY in what they do with a value the carrier cannot hold:
    /// the VALUE itself is <see cref="TryExactScaled"/> for both (kb/Work PB623).</para></summary>
    public static Int128 ToScaled(double v, int scale, CobolRounding mode)
    {
        if (double.IsNaN(v)) return Int128.Zero;
        if (TryExactScaled(v, scale, mode, out Int128 landed)) return landed;
        return v > 0 ? Int128.MaxValue : Int128.MinValue;           // ±Infinity, and every magnitude past the carrier
    }

    /// <summary>The UNCHECKED landing of a binary64 into a fixed-point receiver (kb/Work PB77) — a MOVE (§14.6.8.2
    /// r1/r2/r4: the value converted to fixed point, aligned by decimal point, "zero fill or truncation on either
    /// end"), the no-phrase arithmetic store, INVOKE BY CONTENT. Within the Int128 carrier it is <see cref="ToScaled"/>
    /// exactly (ONE <see cref="TryExactScaled"/> call, so there is no second rounding rule to diverge); beyond it the
    /// value's exact decimal expansion keeps supplying the LOW-ORDER digits (<see cref="LowOrderDigits"/>) — never a
    /// saturation sentinel, which has no capacity check downstream to expose it. A non-finite value (NaN, ±Infinity —
    /// EC-DATA-NOT-FINITE at the sending read under checking, §14.6.13.2 item 3; with checking off the receiving
    /// operand's disposition is the implementor's, §14.6.13.1.3 item 8) lands as ZERO: not a number, no digits — the
    /// disposition <see cref="ToScaled"/> already gave NaN.</summary>
    public static Int128 ToScaledUnchecked(double v, int scale, CobolRounding mode)
    {
        if (!double.IsFinite(v)) return Int128.Zero;
        return TryExactScaled(v, scale, mode, out Int128 landed) ? landed : LowOrderDigits(v, scale, mode);
    }

    /// <summary>The 38 LOW-ORDER digits, sign kept, of a finite binary64's EXACT value at <paramref name="scale"/>
    /// fraction digits, rounded per <paramref name="mode"/> (kb/Work PB77) — the digits a truncating landing keeps of a
    /// value the Int128 carrier cannot hold (the receiver's own store then keeps ITS low-order digits of these, so the
    /// composition is exact whenever the receiver's digit positions fit under 38 minus the landing's excess scale —
    /// which <c>ReceiverContext.WorkingScale</c>'s cap guarantees). It is <see cref="ExactScaled"/> — the same exact
    /// expansion the in-carrier landing uses (kb/Work PB623) — taken modulo 10^38, so "past the carrier" changes only
    /// WHICH digits survive, never WHAT the value is. Cold path: a magnitude at or past 1.7×10^38 at the landing
    /// scale, so it rides <see cref="BigInteger"/> (as BASECONVERT's digit accumulation does) while the engine's hot
    /// paths stay native (<c>CobolNum</c>'s design note). Its callers guard non-finite before calling.</summary>
    public static Int128 LowOrderDigits(double v, int scale, CobolRounding mode) =>
        (Int128)(ExactScaled(v, scale, mode) % Pow10Big38);         // sign-preserving remainder — the low-order digits

    /// <summary>True when <paramref name="v"/> carries a nonzero fraction beyond <paramref name="scale"/> digits — an
    /// INEXACT float→fixed transfer that ROUNDED MODE PROHIBITED must reject with SIZE ERROR / EC-SIZE-TRUNCATION,
    /// leaving the receiver unchanged (ISO §14.7.4.3 item 7: "If the PROHIBITED phrase is specified, and the
    /// arithmetic value cannot be represented exactly in the resultant identifier, the EC-SIZE-TRUNCATION exception
    /// condition is set to exist, the size error condition exists, and the content of the resultant identifier is
    /// unchanged" — cite.py-verified).
    /// <para>⛔ THE QUESTION IS ASKED OF THE EXACT VALUE, and it is the SAME ratio the landing rounds
    /// (<see cref="ExactRatio"/>, kb/Work PB623) — never of a binary64 product, which cannot answer it: the double
    /// nearest 0.1 is 0.1000000000000000055511151231257827…, so it does NOT fit one fraction digit, yet
    /// <c>0.1 * 10.0</c> is exactly 1.0 in binary64 and the old product test called it exact. A gate that says
    /// "representable" where the landing then truncates digits away is the two-arm defect in one statement.</para>
    /// <para>NaN/±Infinity are handled by the store's saturation path, not here.</para></summary>
    public static bool InexactAtScale(double v, int scale)
    {
        if (!double.IsFinite(v)) return false;
        var (num, den) = ExactRatio(v, scale);
        return !BigInteger.Remainder(num, den).IsZero;
    }

    // ── THE binary64 INTERMEDIATE'S RANGE (kb/Work PB1566, PB1581, PB1147) ───────────────────────────────────
    // Under native arithmetic an expression with a floating-point operand — or whose resultants are all
    // floating-point — evaluates in binary64 (CONFORMANCE.md DOC-A.1-123), so binary64's range IS the intermediate
    // data item's range, and §14.7.5 case 5 makes an operation leaving it the size error condition "if native
    // arithmetic is in effect and the implementor defines that the range of values allowed for the intermediate
    // data item is to be checked" — which WiseOwl COBOL defines (DOC-A.1-179). The no-phrase rule 3 names the
    // condition: "farther from zero or nearer to zero than is allowed for the intermediate data item" —
    // EC-SIZE-OVERFLOW or EC-SIZE-UNDERFLOW (cite.py-verified). "Nearer to zero than allowed" is a nonzero exact
    // result that ROUNDS to zero: a subnormal is a binary64 value (ISO/IEC 60559 gradual underflow), not an
    // underflow. A NON-FINITE operand is not an operation leaving the range — a COMP-2 already holding ±Infinity
    // or NaN keeps its DOC-A.1-70 disposition — so only an operation over finite operands is checked.

    /// <summary>⛔ THE ONE range check of a binary64 intermediate whose exact value the caller KNOWS to be nonzero
    /// or not (<paramref name="nonzero"/>): ±∞ is EC-SIZE-OVERFLOW, a nonzero value rounded to zero is
    /// EC-SIZE-UNDERFLOW (§14.7.5 case 5 / no-phrase rule 3), each the <see cref="CobolSizeError"/> the statement's
    /// SIZE ERROR phrase or EC-SIZE checking takes. <paramref name="what"/> names the operation for the message.
    /// Shared by the float-family returned values (FUNCTION EXP, the list bodies) and the checked native
    /// operations below, so an intrinsic and an operator cannot answer the same range question differently.</summary>
    public static double InBinary64Range(double r, bool nonzero, string what)
    {
        if (double.IsInfinity(r))
            throw new CobolSizeError($"{what}: the result is farther from zero than the binary64 intermediate "
                + "allows (ISO §14.7.5 case 5; CONFORMANCE.md DOC-A.1-179)", "EC-SIZE-OVERFLOW");
        if (r == 0 && nonzero)
            throw new CobolSizeError($"{what}: the nonzero result is nearer to zero than the binary64 intermediate "
                + "allows (ISO §14.7.5 case 5; CONFORMANCE.md DOC-A.1-179)", "EC-SIZE-UNDERFLOW");
        return r;
    }

    /// <summary>The CHECKED native binary64 sum — emitted where size-error checking is enabled (an ON SIZE ERROR
    /// phrase, EC-SIZE checking), the twin of <c>CobolNum.AddChecked</c> on the scaled carrier. A sum of finite
    /// binary64 values that is nonzero is never rounded to zero (every such sum is a multiple of the smallest
    /// subnormal), so only the overflow half applies.</summary>
    public static double AddChecked(double a, double b)
    {
        double r = a + b;
        return double.IsFinite(a) && double.IsFinite(b) ? InBinary64Range(r, nonzero: false, "addition") : r;
    }

    /// <inheritdoc cref="AddChecked"/>
    public static double SubChecked(double a, double b)
    {
        double r = a - b;
        return double.IsFinite(a) && double.IsFinite(b) ? InBinary64Range(r, nonzero: false, "subtraction") : r;
    }

    /// <summary>The CHECKED native binary64 product (the twin of <c>CobolNum.MulChecked</c>): overflow, and the
    /// underflow of a product of two nonzero operands.</summary>
    public static double MulChecked(double a, double b)
    {
        double r = a * b;
        return double.IsFinite(a) && double.IsFinite(b) ? InBinary64Range(r, nonzero: a != 0 && b != 0, "multiplication") : r;
    }

    /// <summary>The CHECKED native binary64 quotient (the twin of <c>CobolNum.DivideOrThrow</c>): a zero divisor is
    /// §14.7.5 case 2 — "if the divisor in a divide operation or in a DIVIDE statement is zero" (cite.py-verified),
    /// EC-SIZE-ZERO-DIVIDE by the no-phrase rule 2 — whatever the dividend holds (a zero of either sign is zero);
    /// then overflow, and the underflow of a nonzero dividend's quotient. Before it the floating arm rendered a bare
    /// <c>/</c>: <c>COMPUTE FR = FL / FZ ON SIZE ERROR</c> took NOT ON SIZE ERROR and stored +Infinity (kb/Work
    /// PB1147).</summary>
    public static double DivChecked(double a, double b)
    {
        double r = Div(a, b);
        return double.IsFinite(a) && double.IsFinite(b) ? InBinary64Range(r, nonzero: a != 0, "division") : r;
    }

    /// <summary>The native binary64 quotient where size-error checking is NOT enabled — the IEEE operator, except that
    /// a zero divisor (either sign) is still §14.7.5 case 2 and raises <see cref="CobolSizeError.ZeroDivide"/>: the
    /// condition exists whether or not checking is enabled, and CONFORMANCE.md DOC-A.1-70's disposition for it with
    /// checking off is abnormal termination, exactly as the scaled lane's <c>CobolNum.Divide</c> and the SDIDI lane's
    /// <c>CobolDec.Div</c> (kb/Work PB1605). The bare <c>/</c> this replaced stored +Infinity, or NaN for 0/0, and
    /// the run went on. Overflow and underflow of a nonzero divisor's quotient stay the IEEE results, as the
    /// unchecked sum, difference and product do.</summary>
    public static double Div(double a, double b) => b == 0 ? throw CobolSizeError.ZeroDivide() : a / b;
}
