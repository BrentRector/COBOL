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
/// </remarks>
public static partial class CobolIntrinsics
{
    /// <summary>
    /// ⛔ THE ONE FLOAT→INTEGER-ARGUMENT LANDING for the §15.3 type-6 family (fix-queue PB21). Every integer-
    /// argument body below funnels through here so a float operand gets the IDENTICAL disposition a fixed-point
    /// one gets from <c>IntrinsicRenderer.AsInt</c> — truncation toward zero, the §15.3 implementor latitude for
    /// a value that is not the integer the rule demands. Two separate conversions would let the same function
    /// answer differently depending on how its argument happened to be stored, which is the receiver-shape class
    /// of defect PB13 closed; one helper makes that impossible rather than merely unlikely.
    /// <para>⚠ THE RANGE GUARD IS NOT DECORATION (PB22's lesson, applied before PB22 lands): a
    /// <c>(long)</c> cast on an out-of-range double is UNDEFINED in C#, so a huge operand would wrap into a
    /// plausible date rather than raising. Out of range ⇒ EC-ARGUMENT-FUNCTION (§15.3), never a wrapped value.
    /// The bound is stated as ±9.2e18 rather than long.MaxValue because the nearest double to long.MaxValue is
    /// ABOVE it — comparing against the exact integer would let the boundary value through the cast.</para>
    /// </summary>
    private static bool TryIntegerArg(double v, string fn, out long n)
    {
        if (double.IsFinite(v) && v > -9.2e18 && v < 9.2e18) { n = (long)Math.Truncate(v); return true; }
        Exceptions.ExceptionState.ArgumentError(
            $"{fn}: the floating-point argument {v} is outside the integer-argument range (ISO §15.3)");
        n = 0;
        return false;
    }

    // ── §15.3 type-6 INTEGER arguments reached with a FLOATING-POINT operand (PB21) ────────────────────────────
    // A COMP-2 item is category numeric (§8.5.2.12 item 2) hence class numeric (§8.5.2.1 Table 2), so
    // `Admissible('i')` ADMITS it — the integer-ness is a VALUE property, not a class the screen can reject on.
    // Without these bodies `IntrinsicRenderer.RenderNum`'s AnyRealArgument dispatch emitted a call to a member
    // that does not exist and the user saw a raw Roslyn CS0117 on conforming source.
    // ⚠ INTEGER-OF-BOOLEAN is DELIBERATELY ABSENT: §15.45.3 r1 requires class BOOLEAN, so PB19's screen rejects a
    // float operand at bind time and a body here would be unreachable code that reads as coverage.
    /// <summary>§15.22 DATE-OF-INTEGER with a floating-point argument.</summary>
    public static long DateOfIntegerReal(double v) => TryIntegerArg(v, "DATE-OF-INTEGER", out long n) ? CobolDate.DateOfInteger(n) : 0;
    /// <summary>§15.24 DAY-OF-INTEGER with a floating-point argument.</summary>
    public static long DayOfIntegerReal(double v) => TryIntegerArg(v, "DAY-OF-INTEGER", out long n) ? CobolDate.DayOfInteger(n) : 0;
    /// <summary>§15.46 INTEGER-OF-DATE with a floating-point argument.</summary>
    public static long IntegerOfDateReal(double v) => TryIntegerArg(v, "INTEGER-OF-DATE", out long n) ? CobolDate.IntegerOfDate(n) : 0;
    /// <summary>§15.47 INTEGER-OF-DAY with a floating-point argument.</summary>
    public static long IntegerOfDayReal(double v) => TryIntegerArg(v, "INTEGER-OF-DAY", out long n) ? CobolDate.IntegerOfDay(n) : 0;
    // ── The two TOTAL date validators (§15.90 / §15.91) — NOT members of the family above ──────────────────────
    // ⛔ THESE TWO MUST NOT USE TryIntegerArg (kb/Work PB254). Every OTHER function in this file is PARTIAL: its
    // argument rules bound the value (§15.5.2's date forms, §15.23.3 r1's "< 1 000 000", §15.100.3 r1/r4's
    // windows), so an argument the body cannot represent genuinely "results in an incorrect value … according to
    // the rules specified in the function definition" (§15.3) and the shared raise site is right. The TEST-
    // validators are TOTAL — §15.90.3 r1 / §15.91.3 r1 constrain nothing but integer-ness and r1a is a
    // CATCH-ALL — so `TryIntegerArg(…) ? body : 0` answered **0, "the date is valid"**, for every argument past
    // ±9.2e18: a COMP-2 holding 1.0E19 probed as VALID where §15.90.4 r1a requires 1. The literal `: 0` tail is
    // the PB32 defect shape wearing the shared raise site as camouflage — routing to the site is not enough when
    // the function has no incorrect argument to raise on in the first place.

    /// <summary>§15.90 TEST-DATE-YYYYMMDD with a floating-point argument — TOTAL, like its exact twin.</summary>
    public static long TestDateYyyymmddReal(double v) =>
        TryTotalIntegerArg(v, out Int128 n) ? CobolDate.TestDateYyyymmdd(n)
                                            : NotAnIntegerDateArgument(v, "TEST-DATE-YYYYMMDD");

    /// <summary>§15.91 TEST-DAY-YYYYDDD with a floating-point argument — TOTAL.</summary>
    public static long TestDayYyyydddReal(double v) =>
        TryTotalIntegerArg(v, out Int128 n) ? CobolDate.TestDayYyyyddd(n)
                                            : NotAnIntegerDateArgument(v, "TEST-DAY-YYYYDDD");

    /// <summary>The binary64 argument of a TOTAL integer function, as the <c>Int128</c> its ONE body takes.
    /// False only for a NaN.</summary>
    /// <remarks>
    /// ⛔ IT DOES NOT RE-DECIDE THE VERDICT. The §15.90.4 / §15.91.4 r1a windows live in <c>CobolDate</c> and
    /// NOWHERE ELSE — a rule written once per carrier is exactly the drift PB32 was opened for, and this file
    /// is where that drift happened. A finite magnitude inside <c>Int128</c> converts exactly (truncated —
    /// §15.3's integer-argument latitude, the same latitude <see cref="TryIntegerArg"/> takes); a larger one,
    /// ±∞ included, SATURATES to the corresponding extreme, which is verdict-preserving because both extremes
    /// lie outside both r1a windows and r1a is a CATCH-ALL: the returned value depends on being outside, never
    /// on how far outside. The 1.7e38 bound is <c>FromDouble</c>'s saturation constant, below
    /// <c>Int128.MaxValue</c>, so the boundary value cannot slip through the cast.
    /// </remarks>
    private static bool TryTotalIntegerArg(double v, out Int128 n)
    {
        if (double.IsNaN(v)) { n = Int128.Zero; return false; }
        n = v >= 1.7e38 ? Int128.MaxValue
          : v <= -1.7e38 ? Int128.MinValue
          : (Int128)Math.Truncate(v);
        return true;
    }

    /// <summary>The ONE input to a §15.90/§15.91 argument that is the value of NO integer: a NaN. §15.3 sets
    /// EC-ARGUMENT-FUNCTION (the shared raise site, fatal under checking); with checking disabled §15.3's
    /// closing sentence makes the result implementor-defined, and COBOL.NET defines it as the r1a verdict
    /// <b>1</b> — "the date is not valid" — never the 0 that would report a non-date as a VALID date.
    /// <para>±∞ is deliberately NOT routed here: infinity saturates into the body and r1a answers it, so there
    /// is nothing to raise on.</para></summary>
    private static long NotAnIntegerDateArgument(double v, string fn)
    {
        Exceptions.ExceptionState.ArgumentError(
            $"{fn}: the floating-point argument {v} is not the value of any integer (ISO §15.3)");
        return 1;                                                               // the implementor-defined result (§15.3)
    }

    // The Y2K windowing trio (§15.23 / §15.25 / §15.100) — the optional argument-2/argument-3 keep the SAME
    // C#-optional defaults the exact bodies use (50 / the argument-3 = 0 execution-year sentinel), because the
    // renderer emits only the arguments actually written.
    /// <summary>§15.23 DATE-TO-YYYYMMDD with a floating-point argument.</summary>
    public static long DateToYyyymmddReal(double date, double off = 50) =>
        TryIntegerArg(date, "DATE-TO-YYYYMMDD", out long d) && TryIntegerArg(off, "DATE-TO-YYYYMMDD", out long o)
        ? CobolDate.DateToYyyymmdd(d, o) : 0;
    /// <summary>§15.24 DATE-TO-YYYYMMDD, argument-3 written.</summary>
    public static long DateToYyyymmddReal(double date, double off, double baseYear) =>
        TryIntegerArg(date, "DATE-TO-YYYYMMDD", out long d) && TryIntegerArg(off, "DATE-TO-YYYYMMDD", out long o)
        && TryIntegerArg(baseYear, "DATE-TO-YYYYMMDD", out long b) ? CobolDate.DateToYyyymmdd(d, o, b) : 0;
    /// <summary>§15.25 DAY-TO-YYYYDDD with a floating-point argument.</summary>
    public static long DayToYyyydddReal(double day, double off = 50) =>
        TryIntegerArg(day, "DAY-TO-YYYYDDD", out long d) && TryIntegerArg(off, "DAY-TO-YYYYDDD", out long o)
        ? CobolDate.DayToYyyyddd(d, o) : 0;
    /// <summary>§15.25 DAY-TO-YYYYDDD, argument-3 written.</summary>
    public static long DayToYyyydddReal(double day, double off, double baseYear) =>
        TryIntegerArg(day, "DAY-TO-YYYYDDD", out long d) && TryIntegerArg(off, "DAY-TO-YYYYDDD", out long o)
        && TryIntegerArg(baseYear, "DAY-TO-YYYYDDD", out long b) ? CobolDate.DayToYyyyddd(d, o, b) : 0;
    /// <summary>§15.100 YEAR-TO-YYYY with a floating-point argument. ⛔ The OVERLOAD SPLIT mirrors the exact
    /// core (kb/Work PB119 — the two-arm dispatch shape's sixth occurrence, and the sweep found all THREE
    /// windowing twins carrying it): the former single signature defaulted an OMITTED argument-3 to an explicit
    /// 0, which the core's §15.100.3 r4 "greater than 1600" screen rejected — the §15.3 default 0 (or an abort
    /// under checking) where r3 requires windowing against the execution year; and a WRITTEN 0 must still reach
    /// that screen, so a sentinel cannot express the difference — only the arity can.</summary>
    public static long YearToYyyyReal(double yy, double off = 50) =>
        TryIntegerArg(yy, "YEAR-TO-YYYY", out long y) && TryIntegerArg(off, "YEAR-TO-YYYY", out long o)
        ? CobolDate.YearToYyyy(y, o) : 0;
    /// <summary>§15.100 YEAR-TO-YYYY, argument-3 written.</summary>
    public static long YearToYyyyReal(double yy, double off, double baseYear) =>
        TryIntegerArg(yy, "YEAR-TO-YYYY", out long y) && TryIntegerArg(off, "YEAR-TO-YYYY", out long o)
        && TryIntegerArg(baseYear, "YEAR-TO-YYYY", out long b) ? CobolDate.YearToYyyy(y, o, b) : 0;

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
            Int128 r = 1, mag = Int128.Abs(b);
            bool fits = true;
            for (Int128 i = 0; i < e && fits; i++)
            {
                if (mag > 1 && Int128.Abs(r) > Int128.MaxValue / mag) { fits = false; break; }
                r *= b;
            }
            if (fits) return CobolDec.From(r, 0);
        }
        return CobolDec.FromDouble(Math.Pow((double)b, (double)e));
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

    /// <summary>§15.64 MOD — the FLOORED modulus, whose result takes the sign of argument-2
    /// (§15.64.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER (argument-1 / argument-2))</c>).
    /// ⛔ The zero-divisor leg calls the SHARED <see cref="ModZeroDivisor"/> rather than carrying its own guard
    /// (fix-queue PB32): this body used to answer <c>b == 0 ? 0 : …</c>, which returned the §15.3 default WITHOUT
    /// ever setting the fatal EC-ARGUMENT-FUNCTION its exact twin sets.
    /// <para>⛔ COMPUTED AS THE EXACT CARRIER REMAINDER, NOT THE LITERAL EAE (fix-queue PB65 / RV-15.64.4-1
    /// sweep): the C# <c>%</c> on doubles is IEC 60559's exact truncated remainder of the two carrier values,
    /// adjusted by one addition to the floored form §15.64.4 defines. The literal
    /// <c>a − (b × Math.Floor(a / b))</c> form cancels catastrophically once <c>b·⌊a/b⌋</c> exceeds 2⁵³ —
    /// measured returning 320612800-class garbage for arguments whose true remainder the carrier represents
    /// exactly — and §15.4.1's "implementor-defined approximation" has to approximate the EXPRESSION, which
    /// the exact remainder does and the cancellation does not.</para></summary>
    public static double ModReal(double a, double b)
    {
        if (b == 0) return ModZeroDivisor();
        double r = a % b;
        return r != 0 && (r < 0) != (b < 0) ? r + b : r;
    }

    /// <summary>§15.77 REM — the TRUNCATED remainder, whose result takes the sign of argument-1
    /// (§15.77.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2))</c>).
    /// ⚠ Distinct from <see cref="ModReal(double, double)"/> exactly as the two exact bodies are: REM(−7, 3) is
    /// −1 where MOD(−7, 3) is 2. Shares <see cref="RemZeroDivisor"/> with the exact carrier for the same reason,
    /// and rides the same exact <c>%</c> (which IS the truncated remainder) for the same PB65 sweep reason.</summary>
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
