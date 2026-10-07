// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F1 — the floating-point math intrinsics (ISO §15; COBOLNET_INTRINSICS deep-dive D1: these compute in
/// <see cref="double"/> under the §15.4.1 native-arithmetic license — "the value returned is an implementor-defined
/// approximation of the value of [the equivalent arithmetic] expression"). Out-of-domain arguments produce NaN here
/// and quantize to the EC-ARGUMENT-FUNCTION default result 0 in <see cref="CobolIntrinsics.FromDouble"/> (§15.3).
/// <para><b>Standard arithmetic (ISO §15.4.1, P10 Step 12).</b> The trig/log/SQRT/E/PI rows stay conforming under
/// ARITHMETIC IS STANDARD / STANDARD-DECIMAL: their returned-value rules are prose approximations, NOT equivalent
/// arithmetic expressions, so the value is implementor-defined in EVERY mode (§15.4.1 last paragraph), and a
/// double result entering a standard-decimal expression converts through the one §8.8.1.5.2 r1 operand
/// conversion (<c>CobolDec.FromDouble</c>). ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION carry
/// EAEs with inexact divisions that §15.4.1 r1 requires be evaluated in SDIDI form — under the standard modes they
/// never reach this file: <c>IntrinsicRenderer.RenderDec</c> evaluates them on their <c>…Dec</c> bodies.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    // ── The argument-1 VALUE domains (ISO §15.3 rule 14; kb/Work PB952) ────────────────────────────────────────

    /// <summary>The rational bounds a §15.x.3 "The value of argument-1 shall be …" rule states for the §15.4.1
    /// binary64 family — the whole set, read off the standard (<c>IntrinsicArgumentDomainDriftTests</c> keeps the
    /// catalog's <c>Domain</c> column equal to it).</summary>
    public enum ArgumentDomain
    {
        /// <summary>[−1, +1] — ACOS §15.8.3 rule 2, ASIN §15.10.3 rule 2.</summary>
        ClosedUnit,
        /// <summary>[0, ∞) — SQRT §15.84.3 rule 2 ("zero or positive"), ANNUITY §15.9.3 rule 2.</summary>
        NonNegative,
        /// <summary>(0, ∞) — LOG §15.55.3 rule 2, LOG10 §15.56.3 rule 2 ("greater than zero").</summary>
        Positive,
        /// <summary>(−1, ∞) — PRESENT-VALUE §15.74.3 rule 2 ("greater than −1").</summary>
        AboveMinusOne,
    }

    /// <summary>⛔ THE ONE argument-domain SCREEN of the binary64 family, and it runs on the EXACT operand, BEFORE the
    /// binary64 conversion (kb/Work PB952). §15.3 rule 14: "If the evaluation of an argument results in an incorrect
    /// value for that argument … the EC-ARGUMENT-FUNCTION exception condition is set to exist" — a rule about the
    /// ARGUMENT'S value, and the binary64 the body computes on is not that value. The bodies used to decide it
    /// after the conversion, two ways and both partial: ACOS/ASIN by the NaN <c>Math.Acos</c> returns past ±1,
    /// SQRT/LOG/LOG10 by an explicit test on the double (PB246). A correctly-rounded conversion moves a value
    /// ACROSS a bound: <c>1.000000000000000001</c> (PIC 9V9(18), held exactly) becomes 1.0, so under armed checking
    /// <c>ASIN</c> answered π/2 and raised nothing; <c>-0.99999999999999999999</c> becomes −1.0, so PRESENT-VALUE
    /// raised on a legal rate; an SDIDI's <c>ToDouble</c> underflows a tiny negative to −0.0, which SQRT's
    /// <c>x &lt; 0</c> admitted. So each carrier is screened on its own terms: an exact scaled integer
    /// (<see cref="DomainScaled"/>), the SDIDI (<see cref="DomainDec"/>), or a value that IS a binary64 already —
    /// a float operand, whose double is its exact value (<see cref="DomainReal"/>).</summary>
    /// <returns>The binary64 the body computes on; when the argument is outside the domain, the condition is
    /// raised (fatal under checking) and NaN is returned, so the body's result is NaN and <see cref="RealResult"/>
    /// / <see cref="FromDouble"/> hand back the §15.3 documented default exactly as for every other rejected
    /// argument.</returns>
    public static double DomainScaled(Int128 unscaled, int scale, ArgumentDomain domain, string function, string rule) =>
        Admits(domain, CompareScaled(unscaled, scale, -1), (int)Int128.Sign(unscaled),
               CompareScaled(unscaled, scale, 1))
            ? CobolFloat.ScaledToDouble(unscaled, scale)
            : DomainViolation(domain, function, rule);

    /// <inheritdoc cref="DomainScaled"/>
    public static double DomainDec(CobolDec value, ArgumentDomain domain, string function, string rule) =>
        AdmitsDec(value, domain) ? value.ToDouble() : DomainViolation(domain, function, rule);

    /// <summary>The SAME screen as <see cref="DomainDec"/> — one predicate, <see cref="AdmitsDec"/> — for a body
    /// that takes its argument on the SDIDI carrier UNNARROWED (kb/Work PB999; the renderer's
    /// <c>IntrinsicRenderer.WholeRangeBodies</c>). It returns the admitted value itself rather than its binary64,
    /// because the SDIDI reaches 10^±6144 and binary64 does not: <c>DomainDec</c>'s <c>ToDouble</c> turned the
    /// LEGAL argument 10^−400 into +0.0, and LOG10 of it into −∞ where §15.56.4 r1 requires −400. A rejected
    /// argument raises exactly as <c>DomainDec</c> does and comes back <c>null</c> — the carrier's NaN — which the
    /// body turns into the NaN every rejected argument becomes on its way to the §15.3 default.</summary>
    public static CobolDec? DomainDecAdmitted(CobolDec value, ArgumentDomain domain, string function, string rule)
    {
        if (AdmitsDec(value, domain)) return value;
        DomainViolation(domain, function, rule);
        return null;
    }

    private static bool AdmitsDec(CobolDec value, ArgumentDomain domain) =>
        Admits(domain, Math.Sign(CobolDec.Compare(value, MinusOneDec)), Math.Sign(value.Sig.CompareTo(Int128.Zero)),
               Math.Sign(CobolDec.Compare(value, OneDec)));

    /// <inheritdoc cref="DomainScaled"/>
    public static double DomainReal(double value, ArgumentDomain domain, string function, string rule) =>
        !double.IsNaN(value) && Admits(domain, value.CompareTo(-1d), Math.Sign(value),
               value.CompareTo(1d))
            ? value
            : DomainViolation(domain, function, rule);

    private static readonly CobolDec OneDec = CobolDec.From(1, 0);
    private static readonly CobolDec MinusOneDec = CobolDec.From(-1, 0);

    /// <summary>The domain decided from the argument's sign against −1, 0 and +1 — the only bounds the rules state.</summary>
    private static bool Admits(ArgumentDomain domain, int vsMinusOne, int vsZero, int vsOne) => domain switch
    {
        ArgumentDomain.ClosedUnit => vsMinusOne >= 0 && vsOne <= 0,
        ArgumentDomain.NonNegative => vsZero >= 0,
        ArgumentDomain.Positive => vsZero > 0,
        ArgumentDomain.AboveMinusOne => vsMinusOne > 0,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "no such argument domain"),
    };

    /// <summary>sign(<c>unscaled × 10^−scale</c> − <paramref name="bound"/>) for a bound of ±1, EXACTLY: against
    /// 10^scale when the scale is non-negative (a magnitude past 10^38 at scale ≥ 39 is below 1); a negative scale
    /// is an integer multiple of ten, so only zero lies inside (−10, 10).</summary>
    private static int CompareScaled(Int128 unscaled, int scale, int bound)
    {
        if (scale < 0) return unscaled == 0 ? -bound : Math.Sign(unscaled.CompareTo(Int128.Zero));
        if (scale > 38) return -bound;
        Int128 unit = Pow10.AsWide(scale);
        return Math.Sign(unscaled.CompareTo(bound > 0 ? unit : -unit));
    }

    private static double DomainViolation(ArgumentDomain domain, string function, string rule)
    {
        Exceptions.ExceptionState.ArgumentError($"{function} argument-1 shall be {domain switch
        {
            ArgumentDomain.ClosedUnit => "greater than or equal to -1 and less than or equal to +1",
            ArgumentDomain.NonNegative => "greater than or equal to zero",
            ArgumentDomain.Positive => "greater than zero",
            _ => "greater than -1",
        }} (ISO {rule})");
        return double.NaN;
    }

    // ── Trigonometric / logarithmic (each returns the §15.x.4 equivalent-expression value) ────────────────────
    // ⛔ NO DOMAIN TEST IN ANY BODY BELOW. ACOS / ASIN / SQRT / LOG / LOG10 carry §15.x.3 rule-2 VALUE domains and
    // every one is decided by the screen above, on the exact operand, before the value reaches the body
    // (IntrinsicRenderer.FloatBody applies it from the catalog row's Domain column). A second test here, on the
    // double, would be the partial copy PB952 removed — and for a legal argument rounded onto a bound it would
    // raise where the rule does not.
    public static double Acos(double x) => Math.Acos(x);     // §15.8.4 r1 — arccos in [0, π]
    public static double Asin(double x) => Math.Asin(x);     // §15.10.4 r1 — arcsin in [−π/2, π/2]
    public static double Atan(double x) => Math.Atan(x);     // §15.11 — arctan in (−π/2, π/2)
    public static double Cos(double x) => Math.Cos(x);       // §15.20
    public static double Sin(double x) => Math.Sin(x);       // §15.82
    public static double Tan(double x) => Math.Tan(x);       // §15.89
    /// <summary>SQRT (§15.84) on the NATIVE carrier.</summary>
    /// <remarks><para>§15.84.3 <b>rule 2</b> ("The value of argument-1 shall be zero or positive") is decided by the
    /// <see cref="DomainScaled"/> screen on the exact operand (kb/Work PB952 — the explicit <c>x &lt; 0</c> test
    /// PB246 put here was the right rule on the wrong value: an SDIDI's <c>ToDouble</c> underflows a tiny negative
    /// argument to −0.0, which it admitted).</para>
    /// <para>⛔ §15.84.4 <b>rule 4</b> — "When native arithmetic is in effect, the returned value is the
    /// ABSOLUTE VALUE of the approximation of the square root of argument-1" (kb/Work PB246). IEC 60559 mandates
    /// <c>sqrt(−0) = −0</c> precisely so that sqrt is NOT the absolute value there, while <c>|−0.0|</c> is +0.0.
    /// −0.0 is a LEGAL argument-1 (rule 2 admits zero) that a COMP-1/COMP-2 item reaches by ordinary underflow,
    /// and the receiver-less/float arm renders it unquantized through <c>CobolFloat.Display</c>, which is
    /// IEEE-faithful and prints the sign. <c>Math.Abs</c> IS rule 4, and it is the only input on which it does
    /// any work.</para>
    /// <para>The APPROXIMATION half needs nothing: <c>Math.Sqrt</c> is IEC 60559 correctly-rounded, and SQRT's
    /// catalog row carries <c>Codomain.None</c> correctly — §15.84.4 states no bound, so the
    /// <c>FromDoubleBounded</c> clamp must not apply.</para></remarks>
    public static double Sqrt(double x) => Math.Abs(Math.Sqrt(x));   // §15.84.4 r4 — the ABSOLUTE value (|−0.0| = +0.0)
    public static double Log(double x) => Math.Log(x);       // §15.55 — domain: the screen (§15.55.3 r2)
    public static double Log10(double x) => Math.Log10(x);   // §15.56 — domain: the screen (§15.56.3 r2)

    // ── The EXACT-INTAKE bodies: an exact argument taken UNNARROWED (kb/Work PB999, PB1041) ─────────────────────
    // ⛔ THE RETURNED VALUE IS AN APPROXIMATION OF THE FUNCTION OF ARGUMENT-1, NOT OF ITS BINARY64. §15.4.1 licenses
    // the approximation of the RESULT; the argument is the exact value a scaled item or an SDIDI holds, and a body
    // that narrows it first answers for a different argument wherever the function AMPLIFIES the narrowing error:
    //   · past binary64's RANGE (the SDIDI reaches 10^±6144): LOG10(10^−400) computed log10(+0.0) = −∞, SQRT(10^−400)
    //     answered 0, SIN(10^400) computed sin(+∞) = NaN and RAISED EC-ARGUMENT-FUNCTION on a legal argument;
    //   · where the body is ILL-CONDITIONED — its relative error per unit of argument relative error, |x·f′(x)/f(x)|,
    //     is unbounded on the domain: sin / cos / tan at every multiple of π/2 (SIN(3.14159265358979323) answered
    //     1.22·10^−16 where the value is 8.46·10^−18), LOG / LOG10 at 1 (LOG(1.00000000000000000000000001) answered
    //     0, not 10^−26), ACOS / ASIN at ±1 (ACOS(0.99999999999999999999999999) answered 0, not 1.414·10^−13), and
    //     EXP / EXP10, whose condition number |x| reaches 709 (EXP(700.1234567890123456789) was 150 ulps off).
    // So each overload below takes the CobolDec itself (a domain row through the ONE screen, DomainDecAdmitted,
    // whose null is a rejected argument) and forms, on the EXACT carrier, the quantity the function is well
    // conditioned in — the quadrant and residue about the nearest multiple of π/2, x − 1, 1 − |x|, the integer and
    // fraction parts of x, or the decimal's own exponent — and narrows only that. Where no amplification exists
    // (|x| ≤ π/4 for the periodic trio, |x − 1| ≥ ½ for LOG, |x| ≤ ½ for ACOS / ASIN, |x| &lt; 1 for EXP) the
    // overload IS the double body above, ulp for ulp. Not members, each because its condition number is bounded by
    // a constant on the whole domain: ATAN (≤ 1) and SQRT inside binary64's range (½; SQRT takes an SDIDI only, for
    // the range). IntrinsicRenderer.WholeRangeBodies is the renderer's half of this set.

    /// <summary>§15.55 LOG of an exact argument: ln(Sig·10^Exp) = ln Sig + Exp·ln 10 off the normal binary64 range
    /// (|result| ≥ ~700 there, so the sum loses nothing to cancellation), and ln(1 + u) of the EXACT u = x − 1 near 1
    /// (<see cref="LogOnePlus"/>), where ln is ill-conditioned. <c>null</c> = rejected by §15.55.3 r2.</summary>
    public static double Log(CobolDec? x) =>
        x is not { } v ? double.NaN
        : NearOne(v, out double u) ? LogOnePlus(u)
        : v.ToDouble() is var d && double.IsNormal(d) ? Math.Log(d) : Math.Log((double)v.Sig) + v.Exp * Ln10;

    /// <summary>§15.56 LOG10 of an exact argument: log10 Sig + Exp off the normal binary64 range, and log10(1 + u)
    /// of the EXACT u = x − 1 near 1. <c>null</c> = rejected by §15.56.3 r2.</summary>
    public static double Log10(CobolDec? x) =>
        x is not { } v ? double.NaN
        : NearOne(v, out double u) ? Log10OnePlus(u)
        : v.ToDouble() is var d && double.IsNormal(d) ? Math.Log10(d) : Math.Log10((double)v.Sig) + v.Exp;

    /// <summary>Whether x lies within ½ of 1 — where ln x ≈ x − 1 and the logarithm's condition number 1/|ln x| is
    /// unbounded — and, if so, <paramref name="u"/> = x − 1 formed on the EXACT carrier (one 34-digit rounding at
    /// most, far below binary64's) and only then narrowed.</summary>
    private static bool NearOne(CobolDec v, out double u)
    {
        u = 0;
        if (!(Math.Abs(v.ToDouble() - 1) < 0.5)) return false;
        u = CobolDec.Sub(v, OneDec, CobolRounding.NearestEven).ToDouble();
        return true;
    }

    /// <summary>ln(1 + u) for a u that carries binary64's RELATIVE precision (Kahan's formulation: w = 1 + u rounds,
    /// and ln w · u / (w − 1) cancels that rounding exactly, since w − 1 is exact).</summary>
    private static double LogOnePlus(double u)
    {
        double w = 1 + u;
        return w == 1 ? u : Math.Log(w) * (u / (w - 1));
    }

    /// <summary>log10(1 + u) — <see cref="LogOnePlus"/>'s formulation over log10.</summary>
    private static double Log10OnePlus(double u)
    {
        double w = 1 + u;
        return w == 1 ? u * Log10E : Math.Log10(w) * (u / (w - 1));
    }

    /// <summary>§15.84 SQRT of an SDIDI argument under NATIVE arithmetic (§15.84.4 r4; the standard modes take
    /// <see cref="SqrtDec"/>). Off the normal binary64 range the root is taken on the exact carrier
    /// (<see cref="CobolDec.Sqrt"/>) and only THEN narrowed — SQRT(10^−400) is the representable 10^−200, not the
    /// root of an underflowed +0.0. <c>null</c> = rejected by §15.84.3 r2.</summary>
    public static double Sqrt(CobolDec? x) =>
        x is not { } v ? double.NaN
        : v.ToDouble() is var d && (v.Sig == 0 || double.IsNormal(d)) ? Sqrt(d)
        : CobolDec.Sqrt(v, CobolRounding.NearestEven).ToDouble();

    /// <summary>§15.8 ACOS of an exact argument: past |x| = ½ it is 2·asin(√((1 − |x|)/2)) (π minus that for a
    /// negative x) over the EXACT 1 − |x| — at ±1 the arccosine is ill-conditioned, and ACOS(1 − 10^−26) is
    /// 1.414·10^−13, where the arccosine of its binary64, 1.0, is 0. <c>null</c> = rejected by §15.8.3 r2.</summary>
    public static double Acos(CobolDec? x)
    {
        if (x is not { } v) return double.NaN;
        double d = v.ToDouble();
        if (Math.Abs(d) <= 0.5) return Math.Acos(d);
        double fromOne = AcosOfMagnitude(v);
        return v.Sig > 0 ? fromOne : (PiHi - fromOne) + PiLo;
    }

    /// <summary>§15.10 ASIN of an exact argument: past |x| = ½ it is ±(π/2 − arccos |x|) over the EXACT 1 − |x|
    /// (<see cref="AcosOfMagnitude"/>). <c>null</c> = rejected by §15.10.3 r2.</summary>
    public static double Asin(CobolDec? x)
    {
        if (x is not { } v) return double.NaN;
        double d = v.ToDouble();
        if (Math.Abs(d) <= 0.5) return Math.Asin(d);
        double r = (HalfPiHi - AcosOfMagnitude(v)) + HalfPiLo;
        return v.Sig > 0 ? r : -r;
    }

    /// <summary>arccos |x| for ½ &lt; |x| ≤ 1 as 2·asin(√((1 − |x|)/2)), with 1 − |x| formed on the EXACT carrier —
    /// the half-angle identity, well conditioned in 1 − |x| where arccos is not in x.</summary>
    private static double AcosOfMagnitude(CobolDec v)
    {
        double fromOne = CobolDec.Sub(OneDec, v with { Sig = Int128.Abs(v.Sig) }, CobolRounding.NearestEven).ToDouble();
        return 2 * Math.Asin(Math.Sqrt(fromOne / 2));
    }

    /// <summary>§15.82 SIN of an exact argument: sin(q·π/2 + r) by quadrant over the residue r formed EXACTLY
    /// (<see cref="ReduceQuarterTurns"/>) — sin(10^400) is an ordinary value in [−1, +1], not sin(+∞), sin(10^40) is
    /// sin of 10^40, not of the binary64 nearest it, and sin(3.14159265358979323) is the 8.46·10^−18 its residue
    /// makes it.</summary>
    public static double Sin(CobolDec x)
    {
        double d = x.ToDouble();
        if (Math.Abs(d) <= QuarterPi) return Math.Sin(d);
        var (q, hi, lo) = ReduceQuarterTurns(x);
        double v = (q & 1) == 0 ? SinDd(hi, lo) : CosDd(hi, lo);
        return (q >= 2) != (x.Sig < 0) ? -v : v;                       // sin(x + π) = −sin x; sin is odd
    }

    /// <summary>§15.20 COS of an exact argument (<see cref="ReduceQuarterTurns"/>).</summary>
    public static double Cos(CobolDec x)
    {
        double d = x.ToDouble();
        if (Math.Abs(d) <= QuarterPi) return Math.Cos(d);
        var (q, hi, lo) = ReduceQuarterTurns(x);
        return q switch                                                  // cos is even: |x| decides
        {
            0 => CosDd(hi, lo),
            1 => -SinDd(hi, lo),
            2 => -CosDd(hi, lo),
            _ => SinDd(hi, lo),
        };
    }

    /// <summary>§15.89 TAN of an exact argument: tan r in an even quadrant, −cot r in an odd one — TAN of an argument
    /// just below π/2 is the reciprocal of its residue, not tan of the binary64 nearest π/2
    /// (<see cref="ReduceQuarterTurns"/>).</summary>
    public static double Tan(CobolDec x)
    {
        double d = x.ToDouble();
        if (Math.Abs(d) <= QuarterPi) return Math.Tan(d);
        var (q, hi, lo) = ReduceQuarterTurns(x);
        double t = Math.Tan(hi);
        t += lo * (1 + t * t);                                           // tan(hi + lo) to first order in lo
        double v = (q & 1) == 0 ? t : -1 / t;
        return x.Sig < 0 ? -v : v;                                       // tan is odd
    }

    /// <summary>sin(hi + lo) to first order in lo (|lo| ≤ 2^−53·|hi|).</summary>
    private static double SinDd(double hi, double lo) => Math.Sin(hi) + lo * Math.Cos(hi);

    /// <summary>cos(hi + lo) to first order in lo.</summary>
    private static double CosDd(double hi, double lo) => Math.Cos(hi) - lo * Math.Sin(hi);

    private const double QuarterPi = Math.PI / 4;

    /// <summary>π as a double-double.</summary>
    private const double PiHi = Math.PI, PiLo = 1.2246467991473532e-16;

    /// <summary>ln 10, correctly rounded.</summary>
    private const double Ln10 = 2.302585092994045684017991454684364208;

    /// <summary>log10 e, correctly rounded.</summary>
    private const double Log10E = 0.4342944819032518276511289189166051;

    // ── THE RANGE OF THE RETURNED VALUE (kb/Work PB1566) ────────────────────────────────────────────────────────
    // ⛔ A RESULT PAST BINARY64 IS THE SIZE ERROR CONDITION, NEVER ±Infinity OR A FLUSHED ZERO. Under native
    // arithmetic "the characteristics and representation of the returned value are defined by the implementor"
    // (§15.4.1), and WiseOwl COBOL's determination is that this family's returned value IS a binary64 (CONFORMANCE.md
    // DOC-A.1-92) — so binary64's range is the range of the native intermediate the value is formed in. §14.7.5
    // case 5 makes a value outside that range the size error condition when "the implementor defines that the
    // range of values allowed for the intermediate data item is to be checked", which WiseOwl COBOL defines
    // (DOC-A.1-179), and the no-phrase rule 3 names the condition: "farther from zero or nearer to zero than is
    // allowed for the intermediate data item" — EC-SIZE-OVERFLOW or EC-SIZE-UNDERFLOW. That is the FACTORIAL
    // precedent on the other native carrier (CobolIntrinsics.Factorial: 34! past the Int128 intermediate), and
    // the standard modes already answer the same way past decimal128 (§8.8.1.5.2 r2, CobolDec). EXP(1000) was
    // printed "Infinity" and EXP(−1000) stored 0 with NOT ON SIZE ERROR taken. "Nearer to zero than allowed" is a
    // nonzero value that ROUNDS to zero: a subnormal is a binary64 value (IEC 60559 gradual underflow), not an
    // underflow. A NON-FINITE argument is not an arithmetic operation leaving the range — a COMP-2 already holding
    // ±Infinity or NaN keeps its DOC-A.1-70 disposition — so only a finite argument's result is checked.
    // ⛔ The check itself is CobolFloat.InBinary64Range — the SAME one the checked native binary64 operators use
    // (kb/Work PB1581), so FUNCTION EXP(1000) and `C * C` over COMP-2 answer one range question one way.

    /// <summary>A float-family returned value range-checked by the ONE binary64 intermediate check
    /// (<see cref="CobolFloat.InBinary64Range"/>), named for the function.</summary>
    private static double InBinary64Range(double r, bool nonzero, string function) =>
        CobolFloat.InBinary64Range(r, nonzero, $"FUNCTION {function}");

    /// <summary>An EXACT returned value narrowed to the family's binary64, range-checked against its own value
    /// (<see cref="InBinary64Range"/>) — the one narrowing of an SDIDI-evaluated body.</summary>
    private static double NarrowResult(CobolDec v, string function) =>
        InBinary64Range(v.ToDouble(), v.Sig != 0, function);

    /// <summary>§15.34 EXP of a floating argument — e^x is never zero, so a finite argument's zero is an underflow.</summary>
    public static double Exp(double x) =>                      // §15.34 — e ** argument (COBOL-2002+)
        double.IsFinite(x) ? InBinary64Range(Math.Exp(x), nonzero: true, "EXP") : Math.Exp(x);

    /// <summary>§15.35 EXP10 of a floating argument — as <see cref="Exp(double)"/>.</summary>
    public static double Exp10(double x) =>                    // §15.35 — 10 ** argument (COBOL-2002+)
        double.IsFinite(x) ? InBinary64Range(Math.Pow(10, x), nonzero: true, "EXP10") : Math.Pow(10, x);

    /// <summary>§15.34 EXP of an exact argument: e^i · e^f over x's integer part i and its EXACT fraction f = x − i.
    /// EXP's condition number is |x|, so the narrowed argument's error of |x|·2^−53 was a RELATIVE result error of
    /// the same size — 150 ulps at x ≈ 700 — while i is exact in binary64 and f is below 1. Below |x| = 1 nothing is
    /// amplified and the overload IS the double body; past |x| = 1000 the result is outside binary64 whatever the
    /// argument's digits, which <see cref="InBinary64Range"/> makes the size error condition (kb/Work PB1566) — an
    /// SDIDI argument past binary64's own range narrows to ±∞ and lands there too, correctly.</summary>
    public static double Exp(CobolDec x)
    {
        double d = x.ToDouble();
        if (!(Math.Abs(d) is >= 1 and <= 1000)) return InBinary64Range(Math.Exp(d), nonzero: true, "EXP");
        double i = Math.Truncate(d);
        return InBinary64Range(Math.Exp(i) * Math.Exp(FractionAfter(x, i)), nonzero: true, "EXP");
    }

    /// <summary>§15.35 EXP10 of an exact argument: 10^i · 10^f, as <see cref="Exp(CobolDec)"/> (condition number
    /// |x|·ln 10).</summary>
    public static double Exp10(CobolDec x)
    {
        double d = x.ToDouble();
        if (!(Math.Abs(d) is >= 1 and <= 400)) return InBinary64Range(Math.Pow(10, d), nonzero: true, "EXP10");
        double i = Math.Truncate(d);
        return InBinary64Range(Math.Pow(10, i) * Math.Pow(10, FractionAfter(x, i)), nonzero: true, "EXP10");
    }

    /// <summary>x − <paramref name="integer"/>, formed on the EXACT carrier and then narrowed. <paramref name="integer"/>
    /// is the truncated binary64 of x, so the difference is below 1 in magnitude (a binary64 that rounded up to the
    /// next integer leaves a small negative fraction, which is as good).</summary>
    private static double FractionAfter(CobolDec x, double integer) =>
        CobolDec.Sub(x, CobolDec.From((Int128)integer, 0), CobolRounding.NearestEven).ToDouble();

    /// <summary>E (§15.27, COBOL-2002+): the value of e — 2.718281828459045235360287… approximated in double.</summary>
    public static double E() => Math.E;

    /// <summary>PI (§15.73, COBOL-2002+): the value of π — 3.141592653589793238462643… approximated in double.</summary>
    public static double Pi() => Math.PI;

    // ── Financial (ISO §15.9 / §15.74) ─────────────────────────────────────────────────────────────────────────

    /// <summary>The ONE §15.9.3 r2/r3 raise site (both carriers funnel here — the PB32 one-site-per-rule
    /// discipline): a negative rate or a non-positive period count is an incorrect argument VALUE, so
    /// EC-ARGUMENT-FUNCTION per §15.3 (documented default 0). Fix-queue PB65 — the guards were absent while
    /// the same file's Log/Log10 carried theirs, and ANNUITY(-0.5 3) returned +0.0714 silently.</summary>
    internal static long AnnuityDomain(double rate, double periods) =>
        rate < 0
            ? Exceptions.ExceptionState.ArgumentError($"ANNUITY argument-1 {rate} is not greater than or equal to zero (§15.9.3 r2)")
            : Exceptions.ExceptionState.ArgumentError($"ANNUITY argument-2 {periods} is not a positive integer (§15.9.3 r3)");

    /// <summary>The ONE §15.74.3 r2 raise site (both carriers).</summary>
    internal static long PresentValueDomain(double rate) =>
        Exceptions.ExceptionState.ArgumentError($"PRESENT-VALUE argument-1 {rate} is not greater than -1 (§15.74.3 r2)");

    /// <summary>ANNUITY (§15.9.4): rate 0 ⇒ <c>1 / periods</c> (rule 1); else
    /// <c>rate / (1 − (1 + rate)^(−periods))</c> (rule 2). Domain: §15.9.3 r2 (rate ≥ 0) and r3
    /// (periods a positive integer — integrality is the upstream IntArg latitude; positivity is checked here).</summary>
    /// <remarks>⛔ THE DENOMINATOR IS −expm1(−periods · ln(1 + rate)), NEVER 1 − Math.Pow(1 + rate, −periods)
    /// (kb/Work PB1310). The two are the same number, but the second forms 1 + rate in binary64 FIRST, which is
    /// exactly 1.0 for every rate below 2^−53: ANNUITY(1E−17, 12) divided by 0 and stored 0 where the value is
    /// 1/12 + 13/24·rate. <see cref="LogOnePlus"/> and <see cref="ExpMinusOne"/> carry the rate at its own relative
    /// precision, so the result is the ordinary §15.4.1 approximation for every legal rate.</remarks>
    public static double Annuity(double rate, double periods) =>
        periods <= 0 ? AnnuityDomain(rate, periods)          // r2 (rate ≥ 0) is the screen's (kb/Work PB952)
        : rate == 0 ? 1d / periods : rate / -ExpMinusOne(-periods * LogOnePlus(rate));

    /// <summary>e^y − 1 for a y that carries binary64's RELATIVE precision (Kahan's formulation: u = e^y rounds, and
    /// (u − 1)·y / ln u cancels that rounding) — the <see cref="LogOnePlus"/> twin.</summary>
    private static double ExpMinusOne(double y)
    {
        double u = Math.Exp(y);
        if (u == 1) return y;
        double um1 = u - 1;
        return um1 == -1 || double.IsPositiveInfinity(u) ? um1 : um1 * y / Math.Log(u);
    }

    /// <summary>PRESENT-VALUE (§15.74.4): <c>Σ amountᵢ / (1 + rate)^i</c>, i = 1..n, over the DISCOUNT BASE
    /// <c>1 + rate</c> — which the caller forms on the rate's exact carrier (<see cref="PresentValueBase(CobolDec?)"/>)
    /// or its binary64 (<see cref="PresentValueBase(double)"/>). Domain: §15.74.3 r2 (rate &gt; −1) — a rate at or
    /// below −1 zeroes or inverts the base and the sum is undefined.</summary>
    /// <remarks>⛔ THE BODY TAKES THE BASE, NOT THE RATE (kb/Work PB1000). <c>1 + rate</c> cancels exactly where a
    /// rate is legal and close to its bound: the binary64 of −0.99999999999999999999 is −1.0, so a body forming the
    /// base from a NARROWED rate divided by 0 and answered ±∞ (then EC-ARGUMENT-FUNCTION) for a rate §15.74.3 r2
    /// admits, where the base is 10^−20 and the value 100 · 10^20. Formed on the exact carrier and only then
    /// narrowed, the base keeps binary64's RELATIVE error, which is the §15.4.1 native approximation.</remarks>
    public static double PresentValue(double discountBase, params double[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        // §15.74.3 r2 (rate > −1) is the argument-domain screen's (DomainScaled / DomainDecAdmitted, kb/Work
        // PB952): a legal rate just above −1 converts to −1.0, and a test here would raise on it.
        double pv = 0;
        for (int i = 0; i < amounts.Length; i++) pv += amounts[i] / Math.Pow(discountBase, i + 1);
        // The binary64 sum's range (kb/Work PB1581 — ListInRange); a zero-crossing sum is not known nonzero.
        return double.IsFinite(discountBase) ? ListInRange(pv, amounts, nonzero: false, "PRESENT-VALUE") : pv;
    }

    /// <summary>PRESENT-VALUE's discount base <c>1 + argument-1</c> formed on the rate's EXACT carrier (an SDIDI, or
    /// a scaled operand lifted exactly by <c>CobolDec.From</c>) and only then narrowed (kb/Work PB1000).
    /// <c>null</c> = rejected by the ONE screen (<see cref="DomainDecAdmitted"/>) — the carrier's NaN.</summary>
    public static double PresentValueBase(CobolDec? rate) =>
        rate is not { } r ? double.NaN : CobolDec.Add(OneDec, r, CobolRounding.NearestEven).ToDouble();

    /// <summary>The discount base of a FLOATING rate: the operand IS its binary64, so <c>1 + rate</c> is its own
    /// value (exact by Sterbenz's lemma for every rate in [−1, −½], where the cancellation lives).</summary>
    public static double PresentValueBase(double rate) => 1 + rate;

    // ── Statistics over doubles (ISO §15.86 / §15.98) ─────────────────────────────────────────────────────────

    /// <summary>VARIANCE (§15.98.4): the mean of the squared deviations from the arguments' arithmetic mean.</summary>
    public static double Variance(params double[] xs)
    {
        RequireArguments(xs.Length, "VARIANCE");            // §15.3 — at least one argument (kb/Work PB257)
        double mean = 0;
        foreach (double x in xs) mean += x;
        mean /= xs.Length;
        double sum = 0;
        foreach (double x in xs) sum += (x - mean) * (x - mean);
        // The binary64 value's range (kb/Work PB1581 — ListInRange): an overflowing mean or square reaches the
        // result as ±∞; the variance is nonzero exactly when the arguments are not all equal.
        double v = sum / xs.Length;
        return ListInRange(v, xs, nonzero: v == 0 && !AllEqual(xs), "VARIANCE");
    }

    /// <summary>True when every argument has the first one's value — the only lists whose variance is exactly zero.</summary>
    private static bool AllEqual(double[] xs)
    {
        foreach (double x in xs) if (x != xs[0]) return false;
        return true;
    }

    /// <summary>STANDARD-DEVIATION (§15.86.4 r1): the equivalent arithmetic expression is literally
    /// <c>(FUNCTION SQRT (FUNCTION VARIANCE (argument-list)))</c>, so it is written here as the two FUNCTION
    /// bodies composed — <see cref="Sqrt"/>, not a bare <c>Math.Sqrt</c>, so §15.84.3 r2's domain guard and
    /// §15.84.4 r4's absolute value are inherited from the ONE SQRT body rather than re-decided (kb/Work
    /// PB246/PB257).</summary>
    public static double StandardDeviation(params double[] xs)
    {
        RequireArguments(xs.Length, "STANDARD-DEVIATION");
        return Sqrt(Variance(xs));
    }

    // ── The VARIADIC exact-intake bodies (kb/Work PB1565) ─────────────────────────────────────────────────────
    // ⛔ THE ARGUMENT LIST IS NOT NARROWED BEFORE IT CANCELS. The exact-intake rule above holds for a LIST too:
    // §15.4.1 licenses an approximation of the RETURNED value of the equivalent arithmetic expression, not of each
    // argument, and these three EAEs subtract arguments from one another — §15.98.4's deviations
    // argument-1ᵢ − FUNCTION MEAN (argument-list), §15.86.4's SQRT over them, §15.74.4's Σ of signed amounts — so
    // narrowing each exact argument first cancels its low digits away: VARIANCE(100000000000000001
    // 100000000000000002) answered 0 where the value is 0.25, STANDARD-DEVIATION 0 where it is 0.5, and
    // PRESENT-VALUE(0 100000000000000001 −100000000000000000) 0 where it is 1. The binary64 bodies above stay for a
    // list with a FLOATING argument, whose EAE is evaluated in binary64 (CONFORMANCE.md DOC-A.1-92, the exact
    // family's determination for the same case). An all-exact list is lifted exactly into the SDIDI and the EAE is
    // evaluated there by the ONE Dec body of each function (VarianceDec / StdDevDec / PresentValueDec — the
    // standard modes' bodies, in the native intermediate rounding mode), and only the RESULT is narrowed, once,
    // range-checked (NarrowResult). An SDIDI argument under native already reaches those Dec bodies through
    // IntrinsicRenderer's native Dec arm; this is the fixed-point list's arm of the same evaluation.

    /// <summary>§15.98 VARIANCE of an all-exact argument list: §15.98.4's EAE on the SDIDI, then narrowed.</summary>
    public static double Variance(CobolRounding mode, params CobolDec[] xs)
    {
        RequireArguments(xs.Length, "VARIANCE");            // §15.3 — at least one argument (kb/Work PB257)
        return NarrowResult(VarianceDec(mode, xs), "VARIANCE");
    }

    /// <summary>§15.86 STANDARD-DEVIATION of an all-exact argument list: §15.86.4 r1's
    /// <c>(FUNCTION SQRT (FUNCTION VARIANCE (argument-list)))</c> on the SDIDI (<see cref="StdDevDec"/>), then
    /// narrowed.</summary>
    public static double StandardDeviation(CobolRounding mode, params CobolDec[] xs)
    {
        RequireArguments(xs.Length, "STANDARD-DEVIATION");
        return NarrowResult(StdDevDec(mode, xs), "STANDARD-DEVIATION");
    }

    /// <summary>§15.74 PRESENT-VALUE of an all-exact argument list: §15.74.4's Σ amountᵢ / (1 + rate)^i on the SDIDI
    /// (<see cref="PresentValueDec"/>), then narrowed. §15.74.3 r2 is decided where every SDIDI evaluation of this
    /// function decides it — <see cref="PresentValueDec"/>'s exact compare against −1, through the ONE
    /// <see cref="PresentValueDomain"/> raise site, whose §15.3 default 0 comes back unchanged — so a leading
    /// table(ALL) rate needs no nullable element inside the enumerated <c>CobolDec[]</c>.</summary>
    public static double PresentValue(CobolRounding mode, CobolDec rate, params CobolDec[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        return NarrowResult(PresentValueDec(mode, rate, amounts), "PRESENT-VALUE");
    }

    // ── RANDOM (ISO §15.75) ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>RANDOM with no argument (§15.75.3 rule 5): the next number of the CURRENT sequence; 0 ≤ r &lt; 1
    /// (§15.75.4 rule 1). The sequence is RUN-UNIT state (§15.75.3 rule 4 scopes the implementor seed to "the first
    /// reference to this function in the run unit") and lives on <see cref="RunUnit.Random"/> — a process-global
    /// static here let a second run unit in one process continue the first's seeded sequence (kb/Work PB307).
    /// The spec form (one current sequence that seeded calls restart and argument-less calls
    /// continue) is implemented here per the scout brief §4.1; it also satisfies the 0 ≤ r &lt; 1 NIST range checks.</summary>
    public static double Random() => RunUnit.Current.Random.Next();

    /// <summary>RANDOM (seed) (§15.75.3 rules 2/3): starts a NEW sequence from the seed and returns its first
    /// value. Same seed ⇒ same sequence on a given implementation (§15.75.4 rule 2 — per-process determinism is
    /// what NIST IF131A exercises; hazard H7). Seeds 0..32767 must yield distinct sequences (rule 3) — the .NET
    /// generator satisfies this for the whole int range. ⛔ A NEGATIVE seed violates r2 ("zero or a positive
    /// integer") and raises EC-ARGUMENT-FUNCTION (fix-queue PB65 — the old mask folded it onto a positive seed
    /// and RANDOM(-5) silently aliased RANDOM(0x7FFFFFFB &amp; …)); the mask remains only as the documented
    /// wide-seed mapping for legal values beyond the generator's int range.</summary>
    /// <remarks>
    /// ⛔ THE SEED IS A <b>TOTAL</b> §15 INTEGER ARGUMENT, AND THE <c>Int128</c> CARRIER IS THAT CLAIM
    /// (kb/Work PB636 — see <c>IntrinsicRenderer.AsIntWide</c>, whose pairing guard reads this signature).
    /// §15.75.3 r2 constrains the SIGN and nothing else — "If argument-1 is specified, it shall be zero or a
    /// positive integer" — and §15.75.4 r3 makes the distinct-sequence subset a FLOOR, not a domain: "This
    /// subset shall include the values from 0 through at least 32767", so a seed outside that subset is still a
    /// legal argument whose sequence r1/r2 define. §15.3's closing paragraph raises EC-ARGUMENT-FUNCTION only on
    /// "an incorrect value for that argument … according to the rules specified in the function definition", and
    /// no rule here makes a large seed incorrect. The parameter was <c>long</c>, so the renderer's narrowing
    /// intake screened the argument first: <c>FUNCTION RANDOM(S)</c> with <c>S PIC 9(19)</c> holding
    /// 9,999,999,999,999,999,999 terminated the run unit under <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING
    /// ON</c> and, with checking off, substituted the ARGUMENT 0 and ran the seed-0 sequence without a word.
    /// <para>The reduction to the generator's state is unchanged and stays the documented one
    /// (<c>docs/CONFORMANCE.md</c> row DOC-A.1-145 under §15.75.4 r3): the low 31 bits,
    /// <c>seed AND 0x7FFFFFFF</c>. It is the identity on 0..2³¹−1 — so the required 0..32 767 floor is injective
    /// with a wide margin — and folds every wider seed onto that window, which r3 permits: the subset yielding
    /// DISTINCT sequences need only include the floor, not exhaust the domain.</para>
    /// </remarks>
    public static double Random(Int128 seed)
    {
        if (seed < 0)
        {
            Exceptions.ExceptionState.ArgumentError($"RANDOM argument-1 {seed} is not zero or a positive integer (§15.75.3 r2)");
            return 0;
        }
        return RunUnit.Current.Random.Restart((int)(seed & 0x7FFFFFFF));
    }
}
