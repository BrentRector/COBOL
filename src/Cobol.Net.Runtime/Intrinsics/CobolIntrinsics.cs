// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The intrinsic-function runtime (ISO/IEC 1989:2023 §15; COBOLNET_INTRINSICS_DESIGN spine 1). One static partial
/// class per the deep-dive's runtime home: this file holds the shared conversion spine; the families live in
/// <c>CobolIntrinsics.Float.cs</c> (§15.4.1 double math), <c>CobolIntrinsics.Exact.cs</c> (exact scaled-long
/// numerics + NUMVAL), and <c>CobolIntrinsics.Text.cs</c> (character functions); date/time is
/// <see cref="CobolDate"/>. Typed-native discipline (design D1): exact numerics flow as UNSCALED
/// <see cref="Int128"/>/<see cref="long"/> values at a known decimal scale, floating-point math computes in
/// <see cref="double"/> (the §15.4.1 native-arithmetic implementor-approximation license), strings are UTF-16
/// <see cref="string"/>s. NO decimal, NO byte substrate.
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>
    /// THE one canonical double → scaled-long quantization (every floating-point intrinsic result funnels through
    /// here — singular-pattern rule). <paramref name="scale"/> and <paramref name="mode"/> are the landing the
    /// emitter chose, together, at <c>ReceiverContext.FloatLanding</c> — the resultant identifier's scale and the
    /// statement's ROUNDED mode for a FINAL TRANSFER, the ≥ 9 working scale (hazard H1's floor) with TRUNCATION
    /// for a nested intermediate.
    /// <para>⛔ IT USED TO ROUND NEAREST-AWAY-FROM-ZERO UNCONDITIONALLY, ON THE RATIONALE THAT ROUNDING IS THE
    /// BETTER APPROXIMATION (numeric-design hazard H2 — truncation turns the double artifact
    /// LOG10(1000) = 2.9999999999999996 into 2.999999999). THAT RATIONALE IS RETIRED (kb/Work PB647). It is a
    /// statement about the returned VALUE, which §15.4.1's last paragraph does leave to the implementor; the
    /// rounding of the TRANSFER into a resultant identifier is not left to the implementor — §14.7.4.1 puts it
    /// "relative to the size provided for the resultant identifier" and §14.7.4.3 rule 2 makes a no-phrase store
    /// TRUNCATION. Preferring the better approximation here meant `MOVE FUNCTION LOG10(1000) TO R` and
    /// `COMPUTE R = FUNCTION LOG10(1000)` disagreed about ONE returned value, which §15.4.1's identity sentence
    /// forbids; when the two readings collide, the identity outranks the approximation, and a program that wants
    /// the rounded landing writes ROUNDED — which on this family now actually does something.</para>
    /// NaN (ACOS of |x|&gt;1, SQRT of a negative)
    /// maps to the EC-ARGUMENT-FUNCTION default result 0 — §15.3: "the implementor defines the result of the function
    /// reference" while EC checking is disabled. ±∞ SATURATES to long.Max/MinValue: it is NOT necessarily a domain
    /// error — a LEGAL class-numeric argument whose e**x / 10**x result merely overflows binary64 (EXP(710) ≈ 2.25e308
    /// = +∞) is a genuine huge result under §14.7.5 receiver handling, exactly like a FINITE over-range value
    /// (EXP10(30)). The domain-edge −∞ of LOG(0)/LOG10(0) is a real §15.55.3/§15.56.3 violation and is caught at the
    /// FUNCTION BODY (see <see cref="Float.Log"/>), never here — so this saturation cannot mask it. (CA24.)
    /// <para>
    /// ⛔ <b><paramref name="scale"/> IS NOT FREE — IT MUST BE <c>ReceiverContext.FloatWorkingScale</c> (PB13).</b>
    /// This function saturating is SAFE only when the caller chose a scale that leaves the sentinel above the
    /// receiver's digit capacity, so the store's capacity check raises the size error. That is a property of the
    /// CALLER, not of this function — it cannot be checked here, because a scaled unscaled-value carries no
    /// receiver. <c>CobolFloat.ToScaled</c> gets the same guarantee for free by landing AT the receiver's scale;
    /// this one lands at a WORKING scale, and a later rescale down is what used to hide the sentinel.
    /// </para>
    /// </summary>
    /// <remarks>
    /// ⛔ RETURNS <see cref="Int128"/>, NOT <see cref="long"/> — and that is a CORRECTNESS fix, not a widening for
    /// comfort (fix-queue PB5). The scaled domain of this compiler IS Int128 (every <c>…Scaled</c> body takes
    /// one), but this function saturated at <c>long.MaxValue</c>. Its caller quantizes at
    /// <c>ws = max(Receiver.Scale, 9)</c>, so the old clamp bit at |value| ≈ <b>9.2 × 10⁹</b> — an utterly
    /// ordinary COBOL magnitude. A twelve-digit money field is routine, and every float-family result at or above
    /// that magnitude was silently replaced by 9223372036.85:
    /// <code>
    ///   01 R PIC 9(12)V99.
    ///       COMPUTE R = FUNCTION ANNUITY(10000000000 1)   *> §15.9.4 r1b gives exactly 10000000001.00
    ///         ON SIZE ERROR ... NOT ON SIZE ERROR ...     *> prints NO SIZE ERROR
    ///   R = 00922337203685
    /// </code>
    /// <c>SQRT(1e20)</c>, <c>EXP(23.3)</c>, <c>ABS</c> and <c>MAX</c> over a COMP-2 all produced the same constant.
    /// No diagnostic, no size error — §14.7.4 never saw an overflow because the value had already been clamped to
    /// something that fits. §15.4.1's native-arithmetic licence permits an implementor-defined APPROXIMATION of
    /// the equivalent arithmetic expression; 9223372036.85 is not an approximation of 10000000001.
    /// <para>
    /// ⛔ <b>THE CLAIM THAT USED TO SIT HERE — "past the 10¹⁸ any PICTURE can describe, so the saturation is now
    /// unreachable from a declarable receiver" — IS FALSE, AND BELIEVING IT IS WHY PB5 WAS RECORDED AS CLOSED.</b>
    /// It is wrong twice over. (1) A PICTURE can describe far more than 10¹⁸: <c>PictureAnalyzer</c> caps digit
    /// positions at 31 (CA33), so <c>PIC 9(31)</c> is legal and holds 10³⁰ comfortably. (2) The clamp does not
    /// need a receiver at all — it fires at the FUNCTION's quantization point, before any store.
    /// Both are reproduced (fix-queue PB13):
    /// <code>
    ///   01 R PIC 9(31).
    ///       COMPUTE R = FUNCTION EXP(70) ON SIZE ERROR … NOT ON SIZE ERROR …
    ///   -> prints NO SIZE ERROR;  R = 0170141183460469231731687303715
    ///      where §15.34.4 r1 + §15.4.1 require ≈2.5154386709191670×10³⁰ — wrong by a factor of ~15, silently.
    ///
    ///   IF FUNCTION EXP10(30) = FUNCTION EXP10(31)   -> TRUE
    ///   -> two values a FACTOR OF TEN apart compare equal, with no receiver in the statement: a relation
    ///      operand renders under ReceiverContext.None, so ws = 9 and both sides saturate to Int128.MaxValue.
    /// </code>
    /// The saturation is therefore REACHABLE and SILENT, which is the defect PB5 fixed one instance of.
    /// ⚠ Do not "fix" this by widening the clamp: at ws = 9 the intermediate needs
    /// (receiver integer digits + 9) decimal digits, and Int128 supplies ~38, so a 31-digit receiver is 2 digits
    /// short no matter what constant is chosen.
    /// </para>
    /// <para>
    /// ✅ <b>BOTH CASES ARE CLOSED, AND THE FIX IS ENTIRELY EMITTER-SIDE — WHICH THE QUEUE ENTRY SAID IT COULD NOT
    /// BE.</b> Its recipe read "the runtime must stop silently saturating, which no emitter-side choice can
    /// reach"; that followed from taking the only emitter lever to be the working SCALE. The actual lever is
    /// WHETHER TO QUANTIZE AT ALL, and using it makes this function's saturation correct as it stands:
    /// <list type="number">
    ///   <item><b>With a receiver</b> — <c>ReceiverContext.FloatWorkingScale</c> caps the working scale at the
    ///         receiver's Int128 headroom (ws ≤ 38 − integer digits). A value that fits can no longer saturate,
    ///         and a value that does not saturates to a sentinel that still exceeds the receiver's capacity after
    ///         the rescale — so the store RAISES the size error (§14.7.5 case 5: a native intermediate out of its
    ///         implementor-defined range). <c>COMPUTE R = FUNCTION EXP(700)</c> into <c>PIC 9(31)</c> now reports
    ///         SIZE ERROR where it used to store a saturated value silently. Nothing here had to change.</item>
    ///   <item><b>With no receiver</b> — the render never reaches this function. §15.4.1 leaves "the
    ///         characteristics and representation of the returned value" to the implementor under native
    ///         arithmetic, and WiseOwl COBOL's determination is that the float family's value IS a binary64; the
    ///         quantization exists only to land it in a fixed-point receiver, whose scale defines it. A relation
    ///         then compares natively (§8.8.4.2.4) and the text channel renders through
    ///         <c>CobolFloat.Display</c>, which is what docs/CONFORMANCE.md already required.</item>
    /// </list>
    /// ⚠ So this function is NOT the place to add a raise. Doing so would convert an EC-SIZE-TRUNCATION that the
    /// capacity check already reports correctly into an EC-SIZE-OVERFLOW with different fatality, for no gain.
    /// What keeps the guarantee true is <c>FloatQuantizeHeadroomDriftTests</c>, which proves both invariants over
    /// every legal picture shape and fails if a caller hand-rolls the working scale again.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The EC-ARGUMENT-FUNCTION screen for a float-family result that is NOT quantized — the receiver-less and
    /// float-receiver arms of <c>IntrinsicRenderer.RenderFloat</c>, which keep the value in binary64 (PB13).
    /// <para>
    /// ⛔ THIS EXISTS BECAUSE PB13 MOVED THE RAISE SITE OUT FROM UNDER THOSE ARMS. <see cref="FromDouble"/> is
    /// where an out-of-domain NaN became the §15.3 default result (or the raise, under checking), and keeping a
    /// receiver-less render in binary64 skips it — so <c>COMPUTE R = FUNCTION ACOS(2)</c> yielded the default 0
    /// while the receiver-less <c>IF FUNCTION ACOS(2) = 0</c> propagated a raw NaN and compared FALSE. Two things
    /// were wrong with that: a function's returned value must not depend on the shape of its receiver (§15.4 puts
    /// it in a temporary of the function's own characteristics), and with EC-ARGUMENT-FUNCTION checking ENABLED
    /// §14.6.13.1 requires the condition to be RAISED — which cannot happen if nothing looks at the value.
    /// </para>
    /// <para>Found by the Phase-B refute stage on §15.55 (LOG), against code landed the same day. The domain
    /// violation itself is still caught at the FUNCTION BODY where the body can name its own rule (see
    /// <see cref="Float.Log"/>, §15.55.3 r2); this is the catch-all for the families whose out-of-domain result
    /// is simply NaN — ACOS/ASIN of |x|&gt;1 (§15.8.3/§15.10.3), SQRT of a negative (§15.84.3).</para>
    /// <para>±∞ is deliberately NOT screened here, exactly as <see cref="FromDouble"/> does not screen it: an
    /// overflowing EXP is a legal huge result, not a domain error (CA24).</para>
    /// </summary>
    /// <summary>
    /// ⛔ THE §15.3 INTEGER-ARGUMENT LANDING (fix-queue PB22) — the ONE place a wide intrinsic argument narrows
    /// to the <c>long</c> the integer bodies take. The emitter used to write a bare <c>(long)(…)</c> over an
    /// <c>Int128</c>-typed expression, and <c>RoslynBackend</c> sets no <c>checkOverflow</c>, so that cast WRAPS
    /// MODULO 2⁶⁴ — silently, and BEFORE the function's own range guard can see the value.
    /// <para>
    /// The guard inside <c>CobolDate.IntegerOfDay</c> (§15.5.2 — 1601..9999 / 1..366) is correct and was simply
    /// unreachable: <c>FUNCTION INTEGER-OF-DAY(P * 100 + 62)</c> with <c>P PIC 9(18) VALUE 184467440737115466</c>
    /// evaluates 18446744073711546662, which is 2⁶⁴ + 1995046, so the cast delivered 1995046 and the function
    /// answered 143951 — a plausible date, from an argument nineteen orders of magnitude away, with NO
    /// EC-ARGUMENT-FUNCTION even under enabled checking.
    /// </para>
    /// <para>
    /// ⚠ ONE CAST FED SEVEN RENDERER ARMS over eleven functions, which is why this is a landing rather than
    /// seven repairs: a value the receiving body cannot represent is an incorrect argument (§15.3), and the
    /// place to say so is where the narrowing happens.
    /// </para></summary>
    public static long IntegerArg(Int128 v) =>
        v >= long.MinValue && v <= long.MaxValue
            ? (long)v
            : Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {v} is outside the range the function can represent (ISO §15.3)");

    // ── THE §15.3 TYPE-6 INTEGRALITY RULE — ONE PREDICATE, ONE RAISE SITE, EVERY CARRIER (kb/Work PB1526) ──────
    // §15.3 type 6: "An arithmetic expression that will always result in an integer value or an integer data item
    // shall be specified", and the closing paragraph of §15.3 makes a value the function's argument rules do not
    // admit an "incorrect value for that argument": the EC-ARGUMENT-FUNCTION exception condition is set to exist,
    // and "if … checking for EC-ARGUMENT-FUNCTION is not enabled, the implementor defines the result of the
    // function reference". The compile-time screen (IntrinsicArgumentRules) can only refuse an argument that is
    // PROVABLY not always integral and fails open on everything else, so the VALUE is the rule's real test: an
    // argument that is an integer is an integer EXACTLY (docs/CONFORMANCE.md DOC-A.1-124 — the ONE definition the
    // subscript, reference-modification and SET-index intakes use, CobolNum.HasFraction — except that a subscript or
    // reference-modification bound that is an EXPRESSION or a float item still tests a copy truncated at 9 fraction
    // digits, kb/Work PB1890), never a rounded or truncated copy. This intake used to truncate the fraction silently on every carrier, so
    // FACTORIAL(X / 2 + 1) with X = 5 answered FACTORIAL(3) with no condition even under enabled checking.
    // WiseOwl COBOL's determination for the unchecked result is the truncated integer (the lenient continue
    // CobolNum.PositionOf documents for the same rule at a subscript), so checking off changes nothing a program
    // could observe beyond the missing condition; checking on raises it, fatal, per Table 13.

    /// <summary>The ONE raise site of the §15.3 type-6 integrality rule. <paramref name="fractional"/> is the
    /// carrier's own exact test of "the value has a nonzero digit right of the decimal point"; the value is shown
    /// as its plain decimal text, built only on the failing path.</summary>
    private static void RequireIntegral(bool fractional, Func<string> shown)
    {
        if (fractional)
            Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {shown()} is not an integer (ISO §15.3 type 6 — an integer "
                + "argument shall have an integral value; DOC-A.1-124)");
    }

    /// <summary>A scaled fixed-point integer argument's value, de-scaled to scale 0 with the §15.3 type-6
    /// integrality test applied to the EXACT value (<see cref="CobolNum.HasFraction"/>). A negative scale is a
    /// trailing-P item whose value is the stored digits times 10^|scale|; a product the Int128 carrier cannot
    /// form is past every integer argument's range (the §15.3 incorrect-value path, default 0).</summary>
    private static Int128 IntegerValue(Int128 unscaled, int scale)
    {
        if (scale > 0)
        {
            RequireIntegral(CobolNum.HasFraction(unscaled, scale), () => CobolNum.PlainValue(unscaled, scale));
            return unscaled / Pow10.AsWide(scale);
        }
        if (scale < 0 && unscaled != 0)
        {
            if (!CobolNum.WideningFits(unscaled, -scale))
            {
                Exceptions.ExceptionState.ArgumentError(
                    $"the intrinsic integer argument {unscaled}E{-scale} is outside the range the function can represent (ISO §15.3)");
                return 0;
            }
            return unscaled * Pow10.AsWide(-scale);
        }
        return unscaled;
    }

    /// <summary>The SDIDI operand's value at scale 0 with the same integrality test (<see cref="CobolDec.HasFraction"/>);
    /// a magnitude past the Int128 intermediate is EC-SIZE-OVERFLOW (A.1 item 179 — the landing's own answer).</summary>
    private static Int128 IntegerValue(CobolDec v)
    {
        RequireIntegral(v.HasFraction, () => v.ToDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return v.TruncatedInteger();
    }

    /// <summary>The binary64 operand's integrality test: a finite value is an integer exactly when it equals its
    /// own truncation (COMP-2 <c>2.0</c> is, <c>2 + 1.0E-10</c> is not). ±∞ and NaN are not fractional — they are
    /// out of every range, which each landing's own guard answers.</summary>
    private static void RequireIntegralReal(double v) =>
        RequireIntegral(double.IsFinite(v) && v != Math.Truncate(v),
            () => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>The scaled-operand intake for a BOUNDED (<c>long</c>) integer argument — <see cref="IntegerArg(Int128)"/>
    /// behind the §15.3 type-6 integrality test. Scale 0 needs no test and takes <see cref="IntegerArg(Int128)"/> itself.</summary>
    public static long IntegerArgScaled(Int128 unscaled, int scale) => IntegerArg(IntegerValue(unscaled, scale));

    /// <summary>The SDIDI-operand intake for a BOUNDED integer argument (see <see cref="IntegerArgScaled"/>).</summary>
    public static long IntegerArgDec(CobolDec v) => IntegerArg(IntegerValue(v));

    /// <summary>The scaled-operand intake for a TOTAL (<c>Int128</c>) integer argument — no narrowing, therefore no
    /// range raise point, but the same integrality test (§15.90.3 r1 / §15.91.3 r1 / §15.37.3 r3 / §15.75.3 r2 all
    /// still require an INTEGER; they place no constraint on its magnitude).</summary>
    public static Int128 IntegerArgWideScaled(Int128 unscaled, int scale) => IntegerValue(unscaled, scale);

    /// <summary>The SDIDI-operand intake for a TOTAL integer argument (see <see cref="IntegerArgWideScaled"/>).</summary>
    public static Int128 IntegerArgWideDec(CobolDec v) => IntegerValue(v);

    /// <summary>The <see cref="IntegerArg(Int128)"/> twin for a FLOATING-POINT argument — a distinct NAME rather
    /// than an overload, deliberately: an integer literal converts implicitly to BOTH <c>Int128</c> and
    /// <c>double</c>, so an overload pair would make <c>FUNCTION FACTORIAL(5)</c> a CS0121 ambiguity. That exact
    /// collision is documented in <c>CobolIntrinsics.RealArgs.cs</c> and it broke six corpus programs once.
    /// <para>Reached by every bounded integer argument whose operand is floating-point — the §15.3 type-6
    /// integrality test applies to it like any other carrier's (kb/Work PB1526).</para></summary>
    public static long IntegerArgReal(double v)
    {
        RequireIntegralReal(v);
        return double.IsFinite(v) && v > -9.2e18 && v < 9.2e18
            ? (long)Math.Truncate(v)
            : Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {v} is outside the range the function can represent (ISO §15.3)");
    }

    /// <summary>The WIDE floating-point twin of <see cref="IntegerArgReal"/> — the Int128-carrier intake for the
    /// §15 integer arguments whose domain exceeds <c>long</c>: BOOLEAN-OF-INTEGER's argument-1 (§15.13.3 r1
    /// admits any positive integer, and the bit configuration of a value at or above 2⁶³ is legal and
    /// representable, fix-queue PB65 / RV-15.13.4-1 D1). The range guard follows <see cref="FromDouble"/>'s
    /// saturation constant: 1.7e38 is BELOW <c>Int128.MaxValue</c>, so the boundary value cannot slip through
    /// the cast (the PB22 argument, at the wide carrier).</summary>
    public static Int128 IntegerArgWideReal(double v)
    {
        RequireIntegralReal(v);
        return double.IsFinite(v) && v > -1.7e38 && v < 1.7e38
            ? (Int128)Math.Truncate(v)
            : Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {v} is outside the range the function can represent (ISO §15.3)");
    }

    public static double RealResult(double d) => double.IsNaN(d)
        ? Exceptions.ExceptionState.ArgumentError("floating-point intrinsic argument out of domain (NaN result)")
        : d;

    /// <summary>THE one place a variadic intrinsic body asserts that its argument list is NOT EMPTY — the §15.3
    /// invariant every statistical body silently depends on when it indexes <c>xs[0]</c> or divides by
    /// <c>xs.Length</c>.</summary>
    /// <remarks><para>⛔ ONE MECHANISM, NOT TWO ANSWERS (kb/Work PB257). The two VARIANCE carriers DISAGREED
    /// about this case: <c>CobolIntrinsics.Variance</c> answered a silent <c>0</c> where no value is defined,
    /// while <c>VarianceDec</c> had no guard at all and would die inside <c>SumDec</c>'s <c>xs[0]</c> with an
    /// <c>IndexOutOfRangeException</c> — the two-arm dispatch, with neither arm right. Both now assert the
    /// invariant here, so the pair cannot drift and no body invents a value for an empty list.</para>
    /// <para>⚠ IT IS AN ASSERTION, NOT A ROUTINE PATH, and the reachability was measured rather than assumed.
    /// The written lane is held by the arity gate (<c>IntrinsicBinder</c>, COBOLNET1504 at <c>MinArgs</c>); the
    /// table(ALL) lane is held by <see cref="Values.Tables.CobolTable.AllArgs{T}"/>, which raises this same
    /// condition on an enumeration over no occurrences. The arity gate ALONE does not hold it —
    /// <c>TableAllPlace.StaticCount</c> is null for an OCCURS DEPENDING or DYNAMIC ALL level, so
    /// <c>FUNCTION VARIANCE(TBL(ALL))</c> passes arity with <c>given == 1</c> whatever the run-time count is,
    /// and <c>AllArgs</c> is the body that actually guarantees it. Duplicating the raise here costs one
    /// comparison and removes the dependence of every statistical body on a third body's behaviour.</para></remarks>
    internal static void RequireArguments(int n, string function)
    {
        if (n != 0) return;
        string why = $"FUNCTION {function} was evaluated over an EMPTY argument list — the evaluation of an ALL "
            + "subscript shall result in at least one argument (ISO §15.3)";
        Exceptions.ExceptionState.ArgumentError(why);   // EC-ARGUMENT-FUNCTION when checking is on (fatal)
        throw new Exceptions.CobolFatalException("EC-ARGUMENT-FUNCTION", why);
    }

    /// <summary>The scale-37 maxima of the bounded codomains (fix-queue PB65 / RV-15.75.4-1): the largest
    /// unscaled magnitude PERMITTED inside each codomain, at 37 fraction digits — so the per-scale limit is one
    /// exact integer division, <c>max37 / 10^(37−scale)</c> (floor, for a positive dividend), never a double
    /// computation that loses the bound past 15 digits. For the OPEN unit bound the constant is 10³⁷ − 1
    /// (equality excluded); for the irrational π bounds it is ⌊bound·10³⁷⌋ (equality unreachable — the bound is
    /// not a decimal), independently derived: π to 40 places via Decimal in python, floor at 37. Both fit
    /// Int128 (max ≈ 1.70e38).</summary>
    public static readonly Int128 CodomainBelowOne37 = Int128.Parse("9999999999999999999999999999999999999");
    public static readonly Int128 CodomainHalfPi37 = Int128.Parse("15707963267948966192313216916397514420");
    public static readonly Int128 CodomainPi37 = Int128.Parse("31415926535897932384626433832795028841");

    /// <summary>
    /// ⛔ THE BOUNDED-CODOMAIN QUANTIZER (fix-queue PB65 / RV-15.75.4-1). When <see cref="FromDouble"/> ROUNDS —
    /// which since kb/Work PB647 is when the statement asked it to, not unconditionally — the top half-ulp of a
    /// codomain whose §15.x.4 rule bounds the returned value rounds OUT of it: RANDOM's [0, 1) produced exactly
    /// 1.000000000 in a PIC 9V9(9) receiver under NEAREST-AWAY-FROM-ZERO (§15.75.4 r1's
    /// "less than one" is strict), and ASIN(1) at scale 9 rounded to 1.570796327 &gt; π/2 — outside §15.10.4 r1's
    /// CLOSED bound, because the bound is irrational and the nearest scaled value above it is not "equal to π/2".
    /// The DOUBLE never exits the codomain (Math.PI/2 &lt; π/2; NextDouble() &lt; 1); only the quantization does,
    /// which is why the clamp acts on the QUANTIZED value. The clamp is symmetric in magnitude — every family
    /// member's codomain is symmetric or zero-anchored on the safe side (ACOS ≥ 0, RANDOM ≥ 0 hold in double
    /// already). The limit divides exactly from the scale-37 constant; <paramref name="scale"/> ≤ 31 always
    /// (a working scale is capped at the receiver's Int128 headroom and a PICTURE at 31 digits, §13.18.40.3 SR14 —
    /// and a FINAL-TRANSFER scale is a PICTURE's own, so the same bound holds).
    /// <para>⛔ IT IS A NO-OP UNDER TRUNCATION, AND THAT IS NOT A REASON TO DELETE IT (kb/Work PB647). A truncating
    /// landing cannot leave a codomain the double is already inside, so the no-phrase COMPUTE — now the common
    /// case — never reaches the clamp; an explicit ROUNDED still does, and `2023/pb65_codomain_clamp` carries
    /// ROUNDED legs precisely so this stays a MEASURED rule rather than a green test proving nothing.</para>
    /// </summary>
    public static Int128 FromDoubleBounded(double d, int scale, CobolRounding mode, Int128 max37, bool checkedLanding = false)
    {
        Int128 q = FromDouble(d, scale, mode, checkedLanding);
        Int128 limit = max37 / Pow10.AsWide(37 - scale);
        return q > limit ? limit : q < -limit ? -limit : q;
    }

    /// <param name="checkedLanding">The LANDING's form (kb/Work PB77 — the same two-form rule as
    /// <c>CobolFloat.ToScaled</c> / <c>ToScaledUnchecked</c> and <c>CobolDec.ToUnscaledChecked</c> / <c>ToUnscaled</c>):
    /// <c>true</c> under ON SIZE ERROR / EC-SIZE checking, where a value past the carrier SATURATES so the store's
    /// capacity check raises the size error (PB13's invariant — the working scale keeps the sentinel above the
    /// receiver's capacity); <c>false</c> for the no-phrase store (§14.6.13.1.3 item 8 — the receiver takes the
    /// LOW-ORDER digits of the result, the documented disposition), where the sentinel had no check to expose it and
    /// its low digits were stored as the answer (<c>COMPUTE X = FUNCTION EXP10(40)</c> stored 03715 in PIC 9(5)). An
    /// unchecked ±∞ lands as zero (no digits), exactly as the MOVE landing does. Since kb/Work PB647 moved the
    /// FINAL TRANSFER's rounding decision into this method, it is also the gate on the §14.7.4.3 rule 7
    /// PROHIBITED raise below — the same condition the emitter's own float-source gate uses (an ON SIZE ERROR /
    /// EC-SIZE statement), so the two channels agree on when the condition is raised.</param>
    /// <param name="mode">⛔ THE STATEMENT'S ROUNDING DECISION, AND IT HAS NO DEFAULT ON PURPOSE (kb/Work PB647).
    /// This quantizer is the ARITHMETIC channel's float→fixed landing; <c>CobolFloat.ToScaledUnchecked</c> is the
    /// MOVE channel's, and it has always taken the receiver's mode. While this one hard-coded
    /// <see cref="CobolRounding.NearestAwayFromZero"/>, ONE returned value landed TWO ways in ONE receiver —
    /// <c>MOVE FUNCTION SQRT(3) TO S</c> gave 1.732050807 into <c>PIC 9V9(9)</c> and
    /// <c>COMPUTE S = FUNCTION SQRT(3)</c> gave 1.732050808 — which §15.4.1 forbids ("the returned value is the
    /// same for all instances of a given function within a single execution of the runtime element so long as the
    /// value and order of the arguments, the collating sequence, and the locale are unchanged", cite.py-verified),
    /// and the ROUNDED phrase was a no-op on the whole family. <c>ReceiverContext.FloatLanding</c> is the ONE
    /// emitter-side rule that chooses it: the resultant identifier's own mode for a final transfer
    /// (§14.7.4.3 rule 2 — "If the ROUNDED phrase is not specified, execution is as if ROUNDED MODE IS TRUNCATION
    /// had been specified"), TRUNCATION for a nested intermediate. A new call site must decide, not inherit.</param>
    public static Int128 FromDouble(double d, int scale, CobolRounding mode, bool checkedLanding = false)
    {
        // EC-ARGUMENT-FUNCTION raise point (§14.6.13.1.6 — the exception-condition table gives it Fatal): the
        // §15.3 default 0 when checking is off, the raise (throw) when the statement carries enabled checking (the
        // ambient gate). NaN only — an over-range ±∞ is a legal overflow that saturates like a finite over-range
        // value (the receiver store size-truncates).
        if (double.IsNaN(d)) return Exceptions.ExceptionState.ArgumentError("floating-point intrinsic argument out of domain (NaN result)");
        if (double.IsInfinity(d)) return !checkedLanding ? Int128.Zero : d > 0 ? Int128.MaxValue : Int128.MinValue;
        // ⛔ THE SAME LANDING AS A MOVE'S, BY CONSTRUCTION (kb/Work PB623). This is the ARITHMETIC channel's
        // quantizer — `COMPUTE r = FUNCTION TAN(x)` reaches the receiver through here where `MOVE FUNCTION TAN(x)
        // TO r` reaches it through CobolFloat.ToScaledUnchecked — and while each held its own `d * 10^scale` in
        // binary64 the ONE returned value §15.4.1 promises landed as two different numbers (16331239353195368.96
        // against 16331239353195369.92, the exact value being 16331239353195370). CobolFloat.TryExactScaled is
        // now the single definition of "this double at this scale"; only the past-the-carrier FORM is this
        // landing's own (kb/Work PB77), and since kb/Work PB647 the MODE is the statement's, exactly as the MOVE
        // landing's is — the last thing that still made one returned value land two ways.
        //
        // ⛔ ROUNDING MODE IS PROHIBITED, AT THE ONE PLACE THE TRANSFER'S ROUNDING NOW HAPPENS (§14.7.4.3 rule 7:
        // "If the PROHIBITED phrase is specified, and the arithmetic value cannot be represented exactly in the
        // resultant identifier, the EC-SIZE-TRUNCATION exception condition is set to exist, the size error
        // condition exists", cite.py-verified — the receiver is left unchanged, which the caller's catch does).
        // It has to be HERE and not at the store: a final transfer lands AT the resultant identifier's scale, so
        // the store's rescale is the identity and its own PROHIBITED test can no longer see the discarded
        // fraction. TryExactScaled documents that PROHIBITED itself lands truncated, so the raise must precede
        // it. Gated on checkedLanding — the same ON SIZE ERROR / EC-SIZE scope the emitter's float-source gate
        // has; without the phrase the no-phrase store's silent disposition stands, unchanged.
        if (mode == CobolRounding.Prohibited && checkedLanding && CobolFloat.InexactAtScale(d, scale))
            throw new CobolSizeError(
                "ROUNDED MODE IS PROHIBITED on an inexact float-to-fixed arithmetic transfer "
                + "(ISO §14.7.4.3 r7 — EC-SIZE-TRUNCATION; the receiver is left unchanged)", "EC-SIZE-TRUNCATION");
        if (CobolFloat.TryExactScaled(d, scale, mode, out Int128 landed)) return landed;
        return checkedLanding ? (d > 0 ? Int128.MaxValue : Int128.MinValue)
             : CobolFloat.LowOrderDigits(d, scale, mode);   // the same rounding as above
    }

}
