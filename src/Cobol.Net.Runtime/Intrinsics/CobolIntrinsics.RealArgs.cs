// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F2-R — the FLOATING-POINT argument overloads of the exact numeric/statistics intrinsics
/// (fix-queue PB2).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THESE EXIST. The F2 family computes in exact base-10 <see cref="Int128"/> at a known decimal scale,
/// which is right and is the design (deep-dive D1). But a floating-point ARGUMENT is legal for every one of these
/// functions — ISO §15.7.3 r1 and its siblings require class NUMERIC, and a COMP-2 item is class numeric — and
/// there was no path for one. The emitter dispatched on the FUNCTION's family (<c>sig.Float</c>) rather than on
/// the ARGUMENT's type, so a double expression was handed to an <c>Int128</c> parameter and the user saw a raw
/// Roslyn error escape the compiler:
/// </para>
/// <code>
///   01 D USAGE COMP-2 VALUE -3.5.
///       COMPUTE R = FUNCTION ABS(D)
///   error: backend compilation failed (generated C# at F1.g.cs):
///     (43,69): error CS1503: Argument 1: cannot convert from 'double' to 'System.Int128'
/// </code>
/// <para>
/// Ten of the eleven functions probed failed that way. That is worse than a wrong answer: legal COBOL produced an
/// INTERNAL failure, phrased in the generated C# the user never asked to see.
/// </para>
/// <para>
/// <b>Why a double body is the CONFORMING answer here, not a shortcut.</b> Under native arithmetic — the default —
/// §15.4.1 makes "the characteristics and representation of the returned value … defined by the implementor", and
/// each of these functions is defined by an equivalent arithmetic expression evaluated in that same native
/// arithmetic. With a floating-point argument the EAE's own operands are binary64, so evaluating it in binary64 IS
/// the equivalent expression. Nothing exact is being given up: there was no exact value to preserve once the
/// argument arrived as a float.
/// </para>
/// <para>
/// ⛔ <b>THESE ARE NOT OVERLOADS, AND THE FIRST ATTEMPT PROVED WHY.</b> Sharing the F2 names looked ideal: the
/// emitter's float path already renders each argument through <c>Dbl()</c> and calls <c>sig.RuntimeMethod</c> by
/// name, so one dispatch line would have routed everything with no second naming scheme. It does not compile.
/// <c>Int128</c> has no implicit conversion from <see cref="double"/>, but an integer LITERAL converts implicitly
/// to BOTH — so <c>FUNCTION MAX(5 7)</c> emitted <c>MaxScaled(5, 7)</c> and C# reported
/// <c>CS0121: The call is ambiguous between 'MaxScaled(params Int128[])' and 'MaxScaled(params double[])'</c>,
/// breaking six previously-green corpus programs that had nothing to do with floats.
/// <para>
/// So the real bodies carry a <c>…Real</c> name — but by a CONVENTION, not a table: <c>XxxScaled</c> becomes
/// <c>XxxReal</c>, and any other name gains a <c>Real</c> suffix. One string transform in
/// <c>IntrinsicRenderer.RealMethod</c>, and <c>IntrinsicRealArgDriftTests</c> asserts every exact method
/// reachable with a real argument has its counterpart — so the convention is enforced rather than remembered.
/// </para>
/// <para>
/// ⛔ <b>NO BODY HERE TAKES A §15.3 TYPE-6 (INTEGER) ARGUMENT AS A DOUBLE</b> (kb/Work PB651). An integer has no
/// binary64-ness: the renderer's one argument entry (<c>IntrinsicRenderer.RawArg</c>) takes a type-6 operand's
/// INTEGER through the one type-6 intake before any lane is chosen, so a float there no longer moves a call to this
/// lane, and the integer-only functions' bodies (the date family, the TEST- validators, the windowing trio, MOD) were
/// deleted rather than kept as unreachable code. A mixed row keeps its body for its type-10 position:
/// <see cref="CombinedDatetimeReal"/> is reached by a float argument-2, and its argument-1 arrives an integer.
/// </para>
/// </remarks>
public static partial class CobolIntrinsics
{
    // ── NATIVE EXPONENTIATION (ISO §8.8.1.2 rule 6; fix-queue PB18 + PB28) ──────────────────────────────────────

    /// <summary>
    /// ⛔ THE §8.8.1.2 RULE-6 SCREEN, WRITTEN ONCE FOR EVERY NATIVE ARM (fix-queue PB28).
    /// </summary>
    /// <remarks>
    /// Rule 6 is a GENERAL rule of arithmetic-expression evaluation — its own title is "Native, standard-binary,
    /// and standard-decimal arithmetic" — so it binds native `**` exactly as it binds the SDIDI one, and two of
    /// its three parts are mandatory <c>shall</c> requirements with a named exception condition:
    /// <list type="bullet">
    ///   <item><b>r6a</b> — a zero base shall have an exponent greater than zero, else EC-SIZE-EXPONENTIATION;</item>
    ///   <item><b>r6c</b> — a negative base shall have an integer exponent, else EC-SIZE-EXPONENTIATION.</item>
    /// </list>
    /// <c>CobolDec.Pow</c> has enforced both since it was written; every NATIVE arm went straight to
    /// <c>System.Math.Pow</c> with no rule-6 check at all, so the same program answered differently depending only
    /// on whether an ARITHMETIC clause was present. MEASURED at <c>--std 2023</c> with an <c>ON SIZE ERROR</c>
    /// phrase that did NOT fire: <c>0 ** 0</c> returned <b>1</b> (IEEE's convention, not COBOL's) and
    /// <c>-2 ** 0.5</c> returned <b>0</b> (<c>Math.Pow</c> yields NaN and <c>FromDouble(NaN, ws)</c> quantizes it
    /// to zero). Both are wrong ANSWERS delivered silently. EC-SIZE-EXPONENTIATION is Fatal in Table 14.
    /// <para>⚠ r6b — "if the evaluation yields both a positive and a negative real number, the value returned is
    /// the positive number" — is NOT a screen: it is a selection rule, and it cannot arise here because
    /// <c>Math.Pow</c> and the exact loop below each yield a single value. Checked in this pass rather than left
    /// to be discovered as a fourth leg.</para>
    /// </remarks>
    private static void CheckPowRule6(double b, double e)
    {
        if (b == 0 && e <= 0)
            throw new CobolSizeError(
                $"exponentiation: a zero base requires an exponent greater than zero, not {e} "
                + "(ISO §8.8.1.2 rule 6a)", "EC-SIZE-EXPONENTIATION");
        if (b < 0 && e != Math.Floor(e))
            throw new CobolSizeError(
                $"exponentiation: a negative base ({b}) requires an integer exponent, not {e} "
                + "(ISO §8.8.1.2 rule 6c)", "EC-SIZE-EXPONENTIATION");
    }

    /// <summary>Native <c>**</c> on the FLOATING arm — the §8.8.1.3 implementor-defined approximation, screened by
    /// <see cref="CheckPowRule6"/> first so the two mandatory legs of rule 6 hold on this arm too (PB28).</summary>
    public static double PowNativeReal(double b, double e)
    {
        CheckPowRule6(b, e);
        return Math.Pow(b, e);
    }

    /// <summary><see cref="PowNativeReal"/> where size-error checking is ENABLED — the power is an operation on the
    /// binary64 intermediate, so its result's range is §14.7.5 case 5 through the ONE check the other checked native
    /// operators use (<see cref="CobolFloat.InBinary64Range"/>, kb/Work PB1581): <c>C ** 2</c> over a COMP-2 holding
    /// 1.0E+300 is EC-SIZE-OVERFLOW, not +Infinity. A nonzero base's power is never zero.</summary>
    public static double PowNativeRealChecked(double b, double e)
    {
        double r = PowNativeReal(b, e);
        return double.IsFinite(b) && double.IsFinite(e) ? CobolFloat.InBinary64Range(r, nonzero: b != 0, "exponentiation") : r;
    }

    /// <summary>Native <c>**</c> with an integer base and an integer exponent — EVERY shape (kb/Work PB69 — the
    /// literal-exponent Int128 twin <c>PowNativeInt</c> is gone: its past-the-carrier fallback saturated to
    /// Int128.MaxValue, a sentinel that FUNCTION MOD and every comparison consumed as a value). The owner-decided
    /// values (2026-08-03): the exact Int128 loop while the result fits — <c>CobolDec.From</c> keeps the full
    /// significand, so a 38-digit power is exact on this carrier too — the documented double approximation past
    /// it, the §8.8.1.2 reciprocal via double for a negative exponent; returned on the SDIDI carrier, which owns
    /// its scale at run time and which every downstream consumer handles (fix-queue PB65 / RV-15.64.4-1).</summary>
    /// <remarks>The exponent's SIGN is a run-time fact for a data-item exponent, and the two regimes need
    /// different scales — 0 for a positive power (any larger scale eats exactness headroom: scale 9 corrupted
    /// every power past 29 digits), fraction digits for a reciprocal — so no compile-time scale serves both.
    /// The SDIDI owns its exponent at run time and every downstream consumer is carrier-total over Dec
    /// (the PB32/PB14 sweep), which also makes the value receiver-independent (§15.4): DISPLAY, an IF operand,
    /// a COMPUTE receiver and a FUNCTION argument all see this one result.</remarks>
    public static CobolDec PowNativeIntDec(Int128 b, Int128 e)
    {
        CheckPowRule6((double)b, (double)e);
        if (e >= 0)
        {
            // ⛔ A BASE OF 0, 1 OR -1 NEVER LEAVES THE CARRIER, SO ITS POWER IS NOT A LOOP (kb/Work PB2632): §8.8.1.2
            // r6a admits a zero base with any positive exponent (the screen above took 0 ** 0) and nothing bounds
            // the exponent of 1 or -1, so `B ** N` with a data-item N of 999999999999999999 ran 10^18 multiplications
            // for a constant result. The value is 0 (b = 0), 1 (b = 1) or +/-1 by the exponent's parity (b = -1).
            if (b == 0 || b == 1) return CobolDec.From(b, 0);
            if (b == -1) return CobolDec.From((e & 1) == 0 ? 1 : -1, 0);
            // |b| >= 2: every multiplication at least doubles the magnitude, so the loop leaves the carrier - and
            // stops - after at most 127 steps whatever the exponent.
            // ⛔ THE MAGNITUDE IS UNSIGNED (kb/Work PB2698): a signed 16-byte base can hold Int128.MinValue, whose
            // Int128.Abs throws OverflowException (the statement then reported a size error, whatever the exponent).
            // CobolDec.UAbs is the repository's one total magnitude. |Int128.MinValue| = 2^127 is past the carrier's
            // positive range, so its powers take the documented double approximation below like any other power that
            // leaves it - and no SDIDI ever carries Int128.MinValue as a significand (its own Int128.Abs sites would
            // throw), which is why B ** 1 is not special-cased to return B.
            UInt128 mag = CobolDec.UAbs(b), acc = 1;
            bool fits = true;
            for (Int128 i = 0; i < e; i++)
            {
                if (acc > (UInt128)Int128.MaxValue / mag) { fits = false; break; }
                acc *= mag;
            }
            if (fits) return CobolDec.From(b < 0 && (e & 1) != 0 ? -(Int128)acc : (Int128)acc, 0);
        }
        return CobolDec.FromDouble(Math.Pow((double)b, (double)e));
    }

    /// <summary>⛔ Native <c>**</c> of a SCALED or SDIDI-carried base to an INTEGER exponent (kb/Work PB2617) — the twin of
    /// <see cref="PowNativeIntDec"/> for a base that is not an integer. §8.8.1.3 leaves the method of native evaluation to
    /// the implementor and the precedence follows GnuCOBOL (CLAUDE.md rule 1), whose <c>cob_decimal_pow</c> raises a
    /// decimal to an integer power by an exact <c>mpz_pow_ui</c> and adds <c>scale × n</c>; a binary64 <c>Math.Pow</c> is
    /// the approximation of the NON-integer exponent only. So the power is formed on the exact 256-bit lane
    /// (<see cref="CobolWide.TryPow"/>) and lowered once to the SDIDI by round-to-odd, exactly as every nested wide
    /// intermediate is; a power past that lane is the square-and-multiply chain of <see cref="CobolDec.MulToOdd"/>
    /// (34 digits, round-to-odd, range-checked). A negative exponent is §8.8.1.5.4 r3's reciprocal <c>1 / (b ** |e|)</c>,
    /// and the §8.8.1.2 rule-6 screen is <see cref="CheckPowRule6"/>'s, written once.</summary>
    public static CobolDec PowNativeDec(CobolDec b, Int128 e)
    {
        CheckPowRule6(Int128.Sign(b.Sig), (double)e);
        if (b.Sig == 0) return new CobolDec(0, 0);   // rule 6a held: the exponent is positive
        if (e == 0) return CobolDec.From(1, 0);
        Int128 n = Int128.Abs(e);
        CobolDec power = PowMagnitudeToOdd(b, n);
        return e < 0 ? CobolDec.DivToOdd(CobolDec.From(1, 0), power) : power;
    }

    /// <summary><c>b ** n</c> for n &gt; 0 — the exact wide lane while the power fits it, else the round-to-odd SDIDI chain.</summary>
    private static CobolDec PowMagnitudeToOdd(CobolDec b, Int128 n)
    {
        long scaledExp = n <= int.MaxValue ? (long)b.Exp * (long)n : long.MaxValue;
        if (scaledExp is > int.MinValue and <= int.MaxValue
            && CobolWide.TryPow(CobolWide.From(b.Sig), (long)n, out CobolWide exact))
            return exact.ToDec(-(int)scaledExp);
        CobolDec power = CobolDec.From(1, 0), square = b;
        for (Int128 m = n; m > 0; m >>= 1)
        {
            if ((m & 1) != 0) power = CobolDec.MulToOdd(power, square);
            if (m > 1) square = CobolDec.MulToOdd(square, square);
        }
        return power;
    }

    /// <summary>§15.7 ABS — the absolute value of a floating-point argument.</summary>
    public static double AbsReal(double v) => Math.Abs(v);

    /// <summary>§15.81 SIGN — −1 / 0 / +1. Mirrors the exact body: only a value that IS zero returns 0, so a
    /// negative zero reports 0 rather than −1 (IEEE −0.0 compares equal to 0.0).</summary>
    public static long SignOfReal(double v) => v > 0 ? 1 : v < 0 ? -1 : 0;

    /// <summary>§15.44 INTEGER — the greatest integer not greater than the argument (a FLOOR, so −3.5 gives −4).</summary>
    public static double FloorReal(double v) => Math.Floor(v);

    /// <summary>§15.49 INTEGER-PART — truncation toward zero (so −3.5 gives −3, unlike §15.44).</summary>
    public static double TruncateReal(double v) => Math.Truncate(v);

    /// <summary>§15.42 FRACTION-PART — argument minus its integer part, keeping the argument's sign.</summary>
    public static double FractionPartReal(double v) => v - Math.Truncate(v);

    /// <summary>§15.77 REM — the TRUNCATED remainder, whose result takes the sign of argument-1
    /// (§15.77.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2))</c>).
    /// ⚠ Distinct from MOD exactly as the two exact bodies are: REM(−7, 3) is −1 where MOD(−7, 3) is 2. MOD has no
    /// binary64 body: both its positions are §15.3 type 6, so the renderer takes their integers first and the call
    /// never reaches this lane (kb/Work PB651). Shares <see cref="RemZeroDivisor"/> with the exact carrier (one raise
    /// site per rule, fix-queue PB32), and computes C#'s <c>%</c> on the two doubles — IEC 60559's exact truncated
    /// remainder, which IS §15.77.4's value; the literal <c>a − (b × truncate(a / b))</c> cancels catastrophically
    /// once <c>b·⌊a/b⌋</c> passes 2⁵³ (fix-queue PB65's sweep).</summary>
    public static double RemReal(double a, double b) => b == 0 ? RemZeroDivisor() : a % b;

    /// <summary>§15.59 MAX — the greatest argument value.</summary>
    public static double MaxReal(params double[] xs) { RequireArguments(xs.Length, "MAX"); return xs.Max(); }

    /// <summary>§15.63 MIN — the least argument value.</summary>
    public static double MinReal(params double[] xs) { RequireArguments(xs.Length, "MIN"); return xs.Min(); }

    // ⛔ THE binary64 LIST BODIES FORM THEIR EQUIVALENT ARITHMETIC EXPRESSION IN THE binary64 INTERMEDIATE (kb/Work
    // PB1581), so a sum or a half-sum that leaves binary64's range is §14.7.5 case 5 — EC-SIZE-OVERFLOW, or
    // EC-SIZE-UNDERFLOW for a nonzero value rounded to zero — through the ONE check the checked native operators
    // and FUNCTION EXP use (CobolFloat.InBinary64Range), never a silent ±Infinity: SUM(C C) over a COMP-2 C
    // holding 1.0E+308 printed Infinity. A list with a NON-FINITE argument keeps its DOC-A.1-70 disposition
    // (ListInRange), exactly as a non-finite operand does.

    /// <summary>A list body's binary64 value, range-checked when every argument is finite.</summary>
    private static double ListInRange(double r, double[] xs, bool nonzero, string function)
    {
        foreach (double x in xs) if (!double.IsFinite(x)) return r;
        return InBinary64Range(r, nonzero, function);
    }

    /// <summary>The half-sum <c>(a + b) / 2</c> a list body forms (MIDRANGE's EAE, MEDIAN's even-count middle), each
    /// operation range-checked.</summary>
    private static double HalfSumInRange(double a, double b, double[] xs, string function)
    {
        double s = ListInRange(a + b, xs, nonzero: false, function);
        return ListInRange(s / 2.0, xs, nonzero: s != 0, function);
    }

    /// <summary>§15.88 SUM.</summary>
    public static double SumReal(params double[] xs)
    {
        RequireArguments(xs.Length, "SUM");
        double t = 0;
        foreach (double x in xs) t += x;
        return ListInRange(t, xs, nonzero: false, "SUM");
    }

    /// <summary>§15.76 RANGE — MAX minus MIN.</summary>
    public static double RangeReal(params double[] xs)
    {
        RequireArguments(xs.Length, "RANGE");
        return ListInRange(xs.Max() - xs.Min(), xs, nonzero: false, "RANGE");
    }

    /// <summary>§15.61 MEDIAN — the middle value of the sorted arguments; the mean of the two middle values when
    /// the count is even (§15.61.4).</summary>
    public static double MedianReal(params double[] xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        double[] s = [.. xs];
        Array.Sort(s);
        int m = s.Length / 2;
        return (s.Length & 1) == 1 ? s[m] : HalfSumInRange(s[m - 1], s[m], xs, "MEDIAN");
    }

    /// <summary>§15.62 MIDRANGE — the mean of the greatest and least arguments.</summary>
    public static double MidrangeReal(params double[] xs)
    {
        RequireArguments(xs.Length, "MIDRANGE");
        return HalfSumInRange(xs.Max(), xs.Min(), xs, "MIDRANGE");
    }

    /// <summary>§15.60 MEAN — the arithmetic mean of the arguments.</summary>
    public static double MeanReal(params double[] xs)
    {
        RequireArguments(xs.Length, "MEAN");
        double t = 0;
        foreach (double x in xs) t += x;
        _ = ListInRange(t, xs, nonzero: false, "MEAN");                  // the EAE's sum, then its quotient
        return ListInRange(t / xs.Length, xs, nonzero: t != 0, "MEAN");
    }

    /// <summary>§15.71 ORD-MAX — the 1-based ORDINAL POSITION of the greatest argument, leftmost on a tie
    /// (§15.71.4 r3). Returns a position, never a value, so the count — not the arguments — bounds the result.</summary>
    public static double OrdMaxReal(params double[] xs)
    {
        RequireArguments(xs.Length, "ORD-MAX");
        int at = 0;
        for (int i = 1; i < xs.Length; i++) if (xs[i] > xs[at]) at = i;
        return at + 1;
    }

    /// <summary>
    /// §15.17 COMBINED-DATETIME — <c>argument-1 + (argument-2 / 100000)</c> exactly as §15.17.4 r1 writes it.
    /// </summary>
    /// <remarks>
    /// Argument-2 is "in standard numeric time form" (§15.17.3 r2) and §15.6 types it <c>Num2</c>, so it may
    /// legitimately be a floating-point item — which is why this body is needed even though argument-1 is an
    /// integer date. The exact twin in <c>CobolDate</c> expresses the same expression as a scale shift
    /// (<c>date × 10^(scale+5) + secUnscaled</c> read at scale+5), and the two agree by construction.
    /// <para>⛔ AND IT ASKS THE ONE §15.17.3 SCREEN, like its two exact-carrier twins (kb/Work PB620). This body
    /// had no argument rule at all, so a COMP-2 argument-2 of 86400 computed a fabricated 1.8639999999999 in
    /// silence where the identical VALUE in a fixed-point item terminated the run unit with
    /// EC-ARGUMENT-FUNCTION — a rule about a VALUE answering differently for each CARRIER, which is the PB32
    /// shape. The substituted result is §15.3's documented default 0, the one both other carriers return, and
    /// the LEAP-SECOND state reaches here for the same reason it reaches them: §7.3.17.4 GR4/GR5 is what sets
    /// the bound.</para>
    /// </remarks>
    public static double CombinedDatetimeReal(double integerDate, double seconds, bool leapSecond = false) =>
        CobolDate.CombinedDatetimeOutOfRangeReal(integerDate, seconds, leapSecond)
            ? 0
            : integerDate + (seconds / 100000.0);

    /// <summary>§15.72 ORD-MIN — the 1-based ordinal position of the least argument, leftmost on a tie.</summary>
    public static double OrdMinReal(params double[] xs)
    {
        RequireArguments(xs.Length, "ORD-MIN");
        int at = 0;
        for (int i = 1; i < xs.Length; i++) if (xs[i] < xs[at]) at = i;
        return at + 1;
    }
}
