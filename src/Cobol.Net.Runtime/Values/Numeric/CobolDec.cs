// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The STANDARD-DECIMAL intermediate data item (SDIDI, ISO §8.8.1.5): an abstract signed decimal floating-point
/// temporary — value = <see cref="Sig"/> × 10^<see cref="Exp"/> — whose results are equivalent to IEC 60559:2020
/// decimal128 (34 significant decimal digits). Every operation computes the EXACT result (a 256-bit scratch for
/// products/quotients) and rounds ONCE to 34 significant digits with the program's INTERMEDIATE ROUNDING mode
/// (§11.9.11: default NEAREST-AWAY-FROM-ZERO; NEAREST-EVEN; TRUNCATION; PROHIBITED ⇒ EC-SIZE-TRUNCATION when
/// inexact — surfaced as <see cref="CobolSizeError"/> until the EC model lands). Fixed-point operands (≤31
/// digits) convert EXACTLY — no rounding on entry. The final transfer into a receiver applies the statement's
/// ROUNDED mode (§14.7 NOTE 1: ROUNDED governs only that transfer).
/// <para>The 256÷128 division uses a simple shift-subtract loop — exact and bounded (≤256 iterations); division
/// is rare enough in COBOL flows that clarity wins until profiling says otherwise (commercial-bar note).</para>
/// </summary>
public readonly record struct CobolDec(Int128 Sig, int Exp)
{
    private static readonly Int128 Limit34 = Pow10.AsWide(34);

    /// <summary>Lift a fixed-point operand (unscaled value + scale) into SDIDI form — exact (≤31 digits always
    /// fits the 34-digit significand, §8.8.1.5.2).</summary>
    public static CobolDec From(Int128 unscaled, int scale) => new(unscaled, -scale);

    /// <summary>Lift an exactly-parsed decimal — significand × 10^<paramref name="exp"/>, the value a NUMVAL-F
    /// argument represents under standard-decimal arithmetic (§15.69.4 r3; fix-queue PB60) — into SDIDI form
    /// through the ONE rounding funnel. A ≤34-digit significand passes exactly (no rounding); the §8.8.1.5.2 r2
    /// range check applies, which <see cref="From"/> may skip only because a fixed-point operand can never leave
    /// the decimal128 range — a 4-digit E-exponent can (10^9999 ⇒ EC-SIZE-OVERFLOW; 10^-9999 rounds onto the
    /// 10^-6176 subnormal quantum under <paramref name="mode"/> and, at zero, EC-SIZE-UNDERFLOW).</summary>
    public static CobolDec FromParsed(Int128 sig, int exp, CobolRounding mode) => Round34(sig, exp, sticky: false, mode);

    /// <summary>FUNCTION E under a standard arithmetic mode — the EXACT §15.27.3 r3 value
    /// (2.718281828459045235360287471352662, the full 34-digit SDIDI significand; kb/Work R18). The compiler
    /// folds FUNCTION E to THIS constant and evaluates EXP's §15.34.4 equivalent arithmetic expression
    /// (FUNCTION E ** argument-1) over it, so the function and its hand-written EAE agree by construction
    /// (§15.4.1 r1).</summary>
    public static readonly CobolDec E = new(Int128.Parse("2718281828459045235360287471352662"), -33);

    /// <summary>FUNCTION PI under a standard arithmetic mode — the EXACT §15.73.3 r3 value
    /// (3.141592653589793238462643383279503; kb/Work R18 — the E sibling, same rule shape).</summary>
    public static readonly CobolDec Pi = new(Int128.Parse("3141592653589793238462643383279503"), -33);

    /// <summary>Lift a FLOATING-POINT operand into SDIDI form — the ISO §8.8.1.5.1 implementor-defined
    /// float→SDIDI conversion: the SHORTEST ROUND-TRIP decimal representation of the IEEE value (.NET "R" —
    /// ≤17 significant digits, so it always fits the 34-digit significand EXACTLY and the conversion itself
    /// never rounds; §8.8.1.5.2 r1's "cannot be expressed exactly" case does not arise). The shortest form
    /// makes decimal-clean float values convert to their decimal identity (a COMP-2 holding 0.1 becomes the
    /// SDIDI 0.1, not 0.1000000000000000055…). An infinite operand exceeds the decimal128 range
    /// (EC-SIZE-OVERFLOW, §8.8.1.5.2 r2); a NaN operand is the IEC 60559 'invalid operation' state
    /// (EC-DATA-INCOMPATIBLE, §8.8.1.5.1).</summary>
    public static CobolDec FromDouble(double d)
    {
        if (double.IsNaN(d))
            throw new CobolSizeError("NaN floating-point operand in standard-decimal arithmetic (ISO §8.8.1.5.1 — "
                + "the IEC 60559 invalid-operation state)", "EC-DATA-INCOMPATIBLE");
        if (double.IsInfinity(d))
            throw new CobolSizeError("infinite floating-point operand exceeds the decimal128 range "
                + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW");
        if (d == 0) return new CobolDec(0, 0);
        // "R" = the shortest decimal string that round-trips the double: [-]digits[.digits][E±dd]. Parsed
        // directly to (significand, power-of-ten) — no decimal intermediary (decimal is 28-digit/limited-range).
        string s = d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        bool neg = s[0] == '-';
        int i = neg ? 1 : 0;
        Int128 sig = 0;
        int frac = 0, exp10 = 0;
        bool inFrac = false;
        for (; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '.') { inFrac = true; continue; }
            if (c is 'E' or 'e')
            {
                exp10 = int.Parse(s[(i + 1)..], System.Globalization.CultureInfo.InvariantCulture);
                break;
            }
            sig = sig * 10 + (c - '0');
            if (inFrac) frac++;
        }
        return new CobolDec(neg ? -sig : sig, exp10 - frac);
    }

    /// <summary>True when the VALUE has a nonzero digit right of the decimal point — the SDIDI spelling of
    /// <see cref="CobolNum.HasFraction"/>, asked of the exact value (a trailing-zero significand is not a fraction:
    /// <c>10 × 10⁻¹</c> is the integer 1). An exponent more than 38 places below the point can divide a nonzero
    /// Int128 significand only when the significand is zero, so every nonzero such value is fractional.</summary>
    public bool HasFraction =>
        Sig != 0 && Exp < 0 && (-Exp > 38 || Sig % Pow10.AsWide(-Exp) != 0);

    /// <summary>The value truncated to an integer on the native Int128 carrier — the INTERMEDIATE landing at scale 0
    /// (a magnitude past the carrier is EC-SIZE-OVERFLOW, A.1 item 179), for a consumer that has already decided
    /// the value is an integer (<see cref="HasFraction"/>) or that truncation is the documented continue.</summary>
    public Int128 TruncatedInteger() => ToUnscaledIntermediate(0, CobolRounding.Truncation);

    /// <summary>The multiplicative identity (the §8.8.1.5.4 r1/r3 constant 1).</summary>
    private static readonly CobolDec One = new(1, 0);

    /// <summary>Exponentiation in standard-decimal arithmetic (ISO §8.8.1.5.4). An INTEGER exponent evaluates by
    /// binary square-and-multiply over <see cref="Mul"/> — for exponents 1–4 this performs exactly the r2a–r2d
    /// equivalent expressions (SDIDI multiplication is commutative with a single per-operation rounding, so
    /// b×(b×b) ≡ (b×b)×b digit-for-digit), and for larger integers it is the r2e implementor-defined equivalent
    /// expression whose every multiplication/division follows the §8.8.1.5.3 IEC 60559 rules; EVERY negative
    /// exponent is r3's 1/(b ** |e|) via <see cref="Div"/>, written once here over
    /// <see cref="PowMagnitude"/>. A NON-integer exponent (positive base only —
    /// §8.8.1.2 r6c) is the r2e implementor-defined equivalent expression, and THIS implementation's choice is
    /// stated in <see cref="PowCore"/> / <see cref="Half"/>: <c>FUNCTION SQRT(operand-1)</c> at |operand-2| = ½,
    /// and <c>exp(operand-2 × ln operand-1)</c> developed entirely on SDIDI operands everywhere else
    /// (owner decision D-C, 2026-08-30; kb/Work PB167). EC-SIZE-EXPONENTIATION legs: 0 ** 0 (r4),
    /// zero base with a non-positive exponent (§8.8.1.2 r6a), and a negative base with a non-integer exponent
    /// (§8.8.1.2 r6c). Every step is range-checked at the decimal128 bounds (§8.8.1.5.2 r2, via the ONE
    /// <see cref="Round34Wide"/> clamp).</summary>
    public static CobolDec Pow(CobolDec b, CobolDec e, CobolRounding mode)
    {
        bool eInt = TryIntegerValue(e, out long eBoundProbe, out bool eNegative, out bool eEven);
        if (b.Sig == 0)
        {
            // §8.8.1.2 r6a / §8.8.1.5.4 r4: a zero base requires an exponent greater than zero.
            if (e.Sig <= 0)
                throw new CobolSizeError("exponentiation of zero with a non-positive exponent "
                    + "(ISO §8.8.1.2 r6a / §8.8.1.5.4 r4)", "EC-SIZE-EXPONENTIATION");
            return new CobolDec(0, 0);
        }
        if (e.Sig == 0) return One;                              // §8.8.1.5.4 r1: b ** 0 = 1 (b ≠ 0)
        if (b.Sig < 0 && !eInt)
            throw new CobolSizeError("exponentiation of a negative base with a non-integer exponent "
                + "(ISO §8.8.1.2 r6c)", "EC-SIZE-EXPONENTIATION");
        // ⛔ §8.8.1.5.4 r3 IS THE OUTER CONSTRUCTION, IT IS WRITTEN HERE ONCE, AND NOTHING BELOW EVER SEES A
        // NEGATIVE EXPONENT (kb/Work PB266). r3 is not implementor latitude — r2's latitude is scoped "When
        // the value of operand-2 is greater than zero" — it is an equivalence: "When operand-2 is less than
        // zero, the result shall be equivalent to the evaluation of the arithmetic expression
        // (1 / (operand-1 ** FUNCTION ABS (operand-2)))" (cite.py-verified, both halves). Under §8.8.1.5.2's
        // per-operation 34-digit rounding that expression is a DIVISION whose divisor is this same
        // development at |operand-2|, and it is NOT the same value as exp(−|p| × ln b): the reciprocal form
        // carries one extra correctly-rounded division and no negated exp argument. r3 used to be spelled in
        // TWO of the four arms — the within-bound integer one and PowCore's −½ short-circuit — so the general
        // non-integer arm and the past-loop-bound integer escape carried the SIGN INSIDE the exponent and
        // diverged from r3 in 79% of sampled cases (2 ** -0.25 gave …2332146 where …2332141 is owed). The
        // divergence sits in the 34th significant digit, which no ≤31-digit PICTURE receiver shows — but a
        // relation between two SDIDI intermediates compares them EXACTLY (`IF 2 ** -0.25 = 1 / (2 ** 0.25)`
        // displayed NE), and an exact cancellation lifts it into plain view
        // (`(2 ** -0.25 − 1 / (2 ** 0.25)) * 10 ** 33` displayed 0.5 where 0 is owed).
        // ⚠ r3 is an IDENTITY requirement, not an accuracy one: at 2 ** -0.25 the reciprocal form is 8 ulp
        // from the true value where the old direct form was 3 — honouring r3 costs accuracy and the standard
        // asks for it anyway, which is exactly why the fix is the literal expression and not a tolerance.
        var magnitude = PowMagnitude(b, Magnitude(e), eInt, eBoundProbe, eEven, mode);
        return eNegative ? Div(One, magnitude, mode) : magnitude;
    }

    /// <summary><c>operand-1 ** |operand-2|</c> — §8.8.1.5.4 r1/r2's whole development and never r3's
    /// reciprocal, which is its caller's and is written exactly once.
    /// <para><paramref name="eBoundProbe"/> is the exponent's integer magnitude CLAMPED to
    /// <see cref="long.MaxValue"/>. It is a LOOP-BOUND PROBE and never a value: past the bound the exponent
    /// is <paramref name="eAbs"/>, the operand itself.</para></summary>
    private static CobolDec PowMagnitude(CobolDec b, CobolDec eAbs, bool eInt, long eBoundProbe, bool eEven,
        CobolRounding mode)
    {
        // r2e (non-integer exponent, positive base): the implementor's equivalent arithmetic expression, whose
        // operands "shall be in SDIDI form" and whose every add/sub/mul/div "shall be performed in accordance
        // with the corresponding rules in ISO/IEC 60559:2020" (§8.8.1.5.4 r2e — cite.py-verified).
        if (!eInt) return PowCore(b, eAbs, mode);
        // |e| is loop-bounded; past the bound the escape is MAGNITUDE-AWARE (kb/Work PB145 — the old
        // escape's guard comment claimed |n·log10|b|| ≥ |n|/34, false within 10⁻³³ of 1, so a near-unit
        // base raised a spurious size error; and it named OVERFLOW for BOTH out-of-range directions).
        const long LoopBound = 500_000;
        if (eBoundProbe > LoopBound)
        {
            // |b| = 1 resolves by the EXACT parity of the exponent's integer value (carried out of
            // TryIntegerValue on the Int128 — the old code took the parity of the CLAMPED long, and
            // long.MaxValue is odd, so (−1) ** 10²⁰ answered −1; §8.8.1.5.4 r2).
            if (IsUnitMagnitude(b)) return b.Sig > 0 || eEven ? One : new CobolDec(-1, 0);
            // Past the loop bound the r2e equivalent expression takes over — the SAME SDIDI development the
            // non-integer arm uses (kb/Work PB167: there were two r2e sites and they were two different
            // approximations; there is now one). A negative base keeps its exact parity sign.
            // ⛔ THE EXPONENT OPERAND, NEVER THE CLAMPED PROBE (kb/Work PB267). This used to rebuild the
            // exponent as `new CobolDec(±eBoundProbe, 0)`, so EVERY exponent past the long range was silently
            // replaced by 9223372036854775807 and the answer was for a different expression than the one
            // written: `(1 + 10⁻³³) ** 1.0E+20` answered 1.0000000000000 where 1.0000000000001 is owed, and
            // `0.9999999999999999 ** 1.0E+25` answered a silent 0 where the value is below the decimal128
            // range (§8.8.1.5.2 r2 owes EC-SIZE-UNDERFLOW). There is nothing to reconstruct — an SDIDI
            // carries |operand-2| exactly, so eAbs IS the exponent.
            // ⚠ AND THE DISPOSITION IS NOT "|b| ≷ 1 ⇒ out of range". The closest SDIDI value to one is
            // 1 ± 10⁻³³, and (1 + 10⁻³³) ** 10²⁰ = 1.0000000000001 — comfortably in range. Screening a
            // clamped exponent by the base's side of 1 would raise the same SPURIOUS size error PB145 removed.
            var far = PowCore(Magnitude(b), eAbs, mode);
            return b.Sig < 0 && !eEven ? new CobolDec(-far.Sig, far.Exp) : far;
        }
        CobolDec acc = One, sq = b;
        long m = eBoundProbe;
        bool first = true;
        while (m > 0)
        {
            if ((m & 1) != 0) { acc = first ? sq : Mul(acc, sq, mode); first = false; }
            m >>= 1;
            if (m > 0) sq = Mul(sq, sq, mode);
        }
        return acc;
    }

    /// <summary>|v| — exact: the magnitude of a sign-magnitude carrier is a sign flip, never an operation.</summary>
    private static CobolDec Magnitude(CobolDec v) => v.Sig < 0 ? new CobolDec(-v.Sig, v.Exp) : v;

    /// <summary>The value's decimal ORDER — the count of digits left of the point, i.e. the unique k with
    /// 10^(k−1) ≤ |v| &lt; 10^k for v ≠ 0.</summary>
    private static int Order(CobolDec v) => DigitCount(Int128.Abs(v.Sig)) + v.Exp;

    // ── §8.8.1.5.4 r2e — THE equivalent arithmetic expression, developed in SDIDI form ────────────────────────
    // ⛔ ONE DEVELOPMENT, TWO CALLERS (owner decision D-C, 2026-08-30; kb/Work PB167). Before this there were
    // two r2e sites and they were two DIFFERENT approximations — `FromDouble(Math.Pow(…))` for a non-integer
    // exponent and a `PowByLogs` binary64 log decomposition for the past-loop-bound integer escape — so the same
    // mathematical value could arrive by two roads with two answers. Both now enter here.
    //
    // WHAT r2e ACTUALLY CONSTRAINS, AND WHAT IT DOES NOT. It leaves the equivalent expression to the implementor
    // but binds the DEVELOPMENT: every operand in SDIDI form, every operation a 60559 decimal operation
    // (§8.8.1.5.3 → formatOf-addition/-subtraction/-multiplication/-division). Nothing below leaves that lane —
    // no binary64 bridge, no Math.Pow, no Math.Log10 (CobolDecPowDevelopmentTests holds that true by scanning
    // this file). What r2e does NOT promise is 34 CORRECT digits: an SDIDI operation rounds to 34 digits
    // (§8.8.1.5.2), so a development of N such operations carries the accumulated rounding of all of them.
    //
    // THE ACCURACY DETERMINATION, published in CONFORMANCE.md §7 and MEASURED, not estimated (kb/Work PB269 —
    // the numbers below come from an independent 34-digit emulation of this file checked against 120-digit
    // truth, and this comment, CONFORMANCE.md §7, PB167.md and the GR-8.8.1.5.4-2 inventory note state the
    // SAME determination in the same terms). An r2e-conforming SDIDI development delivering:
    //   · ≈33 correct significant digits for a moderate exponent (33.00–33.58 measured over the six
    //     CobolDecPowDevelopmentTests rows; 30.87 was the floor over a 400-case randomized in-range sweep);
    //   · degrading as ≈ 33 − log₁₀|operand-2 × ln operand-1| once that product grows past 1 — an exp
    //     development turns ABSOLUTE error in its argument into RELATIVE error in the result, so this driver
    //     IS inherent to the equivalent expression rather than to this coding of it (measured: 32.15 digits at
    //     |p·ln b| = 7.1, 31.12 at 69, 30.11 at 693, 28.78 at 13 863).
    // ⛔ THERE WAS A SECOND DRIVER AND IT WAS NOT INHERENT — IT WAS THIS CODING (kb/Work PB269). A base near 1
    // lost the whole of b−1 into the LOGARITHM's three-square-root reduction: 26.6 correct digits at
    // 1.00001 ** 1000000, 18.5 at a 31-digit near-unit base, and 3.0 at (1+10⁻³³) ** 10³⁰, where the reduction
    // returned ln b = 0 exactly and the answer was a flat 1. Both this comment and CONFORMANCE.md §7 called
    // the degradation "inherent to the equivalent expression"; for that driver it was false. <see
    // cref="LnReduced"/>'s near-unit arm removes it — the same three cases now measure 32.4, 29.1 and 33.2.
    //
    // THE EXPRESSION.  operand-1 ** operand-2  ≡  exp(|operand-2| × ln(operand-1)),  operand-1 > 0, with
    // |operand-2| = ½ short-circuited to FUNCTION SQRT (see the r2e-½ arm above): SQRT is the one §15 function
    // whose standard-decimal value the standard fixes EXACTLY (§15.84.4 r2 — "the exact square root … rounded to
    // 34 digits"), so choosing it as the equivalent expression at ½ makes `b ** 0.5` and `FUNCTION SQRT(b)`
    // equal BY CONSTRUCTION rather than by luck — and r3's one reciprocal in <see cref="Pow"/> then gives
    // 1/(b ** ½) at −½ without a second short-circuit to keep in step.

    /// <summary>½, the exponent at which the r2e equivalent expression is <c>FUNCTION SQRT(operand-1)</c>.</summary>
    private static readonly CobolDec Half = new(5, -1);

    /// <summary>Ten.</summary>
    private static readonly CobolDec Ten = new(10, 0);

    /// <summary>Two — the <c>2 + δ</c> of the near-unit logarithm reduction.</summary>
    private static readonly CobolDec Two = new(2, 0);

    /// <summary>¼ — the near-unit logarithm band, chosen by measurement (see <see cref="LnReduced"/>).</summary>
    private static readonly CobolDec Quarter = new(25, -2);

    /// <summary>The r2e development: <c>exp(p × ln b)</c> for <c>b &gt; 0</c> and <c>p &gt; 0</c>, every step an
    /// SDIDI operation. p is a MAGNITUDE — §8.8.1.5.4 r3's reciprocal belongs to <see cref="Pow"/> and is
    /// written there once, so this arm never sees a negative exponent and needs no −½ twin: ½ routes to
    /// <see cref="Sqrt"/>, and r3 makes −½ that root's reciprocal by construction.</summary>
    private static CobolDec PowCore(CobolDec b, CobolDec p, CobolRounding mode)
    {
        if (Compare(p, Half) == 0) return Sqrt(b, mode);
        if (IsUnitMagnitude(b) && b.Sig > 0) return One;                                  // 1 ** anything = 1
        // ln 10 is the reduction constant of BOTH halves of the development — the ln decomposition's and the
        // exp one's — and its value depends only on the rounding mode, so it is developed ONCE PER MODE
        // (see <see cref="Ln10"/>), not once per exponentiation.
        var ln10 = Ln10(mode);
        var lnB = Ln(b, ln10, mode);
        // ⛔ THE PRODUCT p × ln b CAN ITSELF LEAVE THE DECIMAL128 RANGE, AND ITS OWN CLAMP WOULD THEN NAME THE
        // WRONG CONDITION (kb/Work PB267). Mul lands through Round34Wide → Clamp, which raises
        // EC-SIZE-OVERFLOW for any |p × ln b| past 9.999…E+6144 — whichever direction the RESULT is out of
        // range in; and when ln b < 0 the result is out of range DOWNWARD, which §8.8.1.5.2 r2 gives its own
        // name. It became reachable the moment the escape started carrying the exponent OPERAND rather than a
        // clamped long: `1.0E-6176 ** 1.0E+6144` has |p × ln b| ≈ 1.4E+6148.
        // Decide by ORDER, before the product exists. 10^(Order(v)−1) ≤ |v| < 10^Order(v), so
        // Order(p) + Order(ln b) > 6 ⇒ |p × ln b| ≥ 10⁵ ⇒ |q| = |p × ln b| ÷ ln 10 > 43 000 — past BOTH of
        // Exponential's range screens, so the result is certainly out of range; and p > 0 makes the DIRECTION
        // the sign of ln b alone. Below that threshold the product cannot overflow and Exponential's own
        // screens (which see the exact q) own the verdict, so this adds no second range authority.
        if (lnB.Sig != 0 && Order(p) + Order(lnB) > 6)
            throw lnB.Sig > 0
                ? new CobolSizeError("standard-decimal exponentiation exceeds the decimal128 range "
                    + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW")
                : new CobolSizeError("standard-decimal exponentiation is below the decimal128 range "
                    + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-UNDERFLOW");
        return Exponential(Mul(p, lnB, mode), ln10, mode);
    }

    // ── ln 10, once per mode ─────────────────────────────────────────────────────────────────────────────────
    // A pure function of the INTERMEDIATE ROUNDING mode (§11.9.11 offers exactly four), developed by the same
    // series as every other logarithm — there is nothing here to transcribe wrongly and nothing to re-derive
    // when the development changes. It used to be re-developed on EVERY exponentiation: three BigInteger square
    // roots and ~20 series terms per call, and PowCore calls it once for the ln half and once for the exp half
    // of the same expression, so a single `**` paid for six roots where three are owed (kb/Work PB268).
    private static readonly Lazy<CobolDec> Ln10Nearest =
        new(() => LnReduced(Ten, CobolRounding.NearestAwayFromZero));
    private static readonly Lazy<CobolDec> Ln10NearestEven = new(() => LnReduced(Ten, CobolRounding.NearestEven));
    private static readonly Lazy<CobolDec> Ln10Truncation = new(() => LnReduced(Ten, CobolRounding.Truncation));

    /// <summary>ln 10 under <paramref name="mode"/>. <see cref="Lazy{T}"/> because the PROHIBITED entry must
    /// THROW rather than hold a value, so no eager table can exist.</summary>
    private static CobolDec Ln10(CobolRounding mode) => mode switch
    {
        CobolRounding.NearestAwayFromZero => Ln10Nearest.Value,      // the §11.9.11 r3a default
        CobolRounding.NearestEven => Ln10NearestEven.Value,
        CobolRounding.Truncation => Ln10Truncation.Value,
        // ⛔ PROHIBITED IS NEVER CACHED, AND MUST NOT BE. ln 10 has no exact SDIDI form, so §11.9.11.2 r3d
        // requires this development to RAISE EC-SIZE-TRUNCATION every time it is asked for — the raise IS the
        // observable, and a cached value would answer where the rule requires an exception.
        // Any other CobolRounding value cannot be an INTERMEDIATE ROUNDING setting (§11.9.11 names four); it
        // develops uncached rather than borrowing a cache entry whose mode it is not.
        _ => LnReduced(Ten, mode),
    };

    /// <summary>The natural logarithm of <paramref name="v"/> (&gt; 0) in SDIDI form.
    /// <para>ARGUMENT REDUCTION. v = m × 10^k with m ∈ [1, 10) read straight off the significand (exact — a
    /// decimal carrier's decimal exponent needs no arithmetic), then m halved into [√⅒, √10) when it exceeds
    /// √10, so ln v = k·ln 10 + ln m with |ln m| ≤ ½·ln 10. <see cref="LnReduced"/> then takes ln m by one of
    /// two argument reductions — three exact square roots (<see cref="Sqrt"/>) to u = m^(1/8) ∈ [0.87, 1.16)
    /// away from 1, and ln(1+δ) over δ = m−1 directly inside |m−1| ≤ ¼, where the roots would cancel the
    /// difference away; the band and its measured digits are documented there.</para>
    /// <para>THE SERIES. 2·atanh(z) = 2·(z + z³/3 + z⁵/5 + …), one <see cref="AtanhSeries"/> for both
    /// reductions — the odd-only form, so each term costs |z|² and the tail is cut when a term can no longer
    /// change the accumulator's 34th digit. Every VALUE-producing operation is <see cref="Add"/>/
    /// <see cref="Sub"/>/<see cref="Mul"/>/<see cref="Div"/> under the CALLER's INTERMEDIATE ROUNDING mode
    /// (§11.9.11), which is what makes INTERMEDIATE ROUNDING IS PROHIBITED report this development as inexact —
    /// §11.9.11.2 rule 3 d, and the same thing <see cref="Sqrt"/> already did for an inexact root.</para>
    /// <para>ln 10 is not a magic constant: it is <see cref="LnReduced"/>(10) — the same series over the same
    /// reduction — so there is nothing here to transcribe wrongly and nothing to re-derive when the development
    /// changes. It is developed once per rounding mode, not once per call (<see cref="Ln10"/>).</para></summary>
    private static CobolDec Ln(CobolDec v, CobolDec ln10, CobolRounding mode)
    {
        Int128 s = Int128.Abs(v.Sig);
        int dc = DigitCount(s);
        var m = new CobolDec(s, -(dc - 1));            // m ∈ [1, 10) — exact, no arithmetic
        int k = v.Exp + dc - 1;                        // v = m × 10^k
        // m ≥ √10 ⇔ m² ≥ 10 — asked of the SQUARE so the band test needs no square root of its own (m ≤ 34
        // digits, so m² is one Mul; √10 is irrational and can never sit exactly on the boundary).
        // ⛔ TRUNCATION, NOT THE CALLER'S MODE, AND DELIBERATELY. This Mul is a THRESHOLD TEST, not a step in the
        // development of the returned value, so §11.9.11's intermediate rounding does not govern it — and under
        // INTERMEDIATE ROUNDING IS PROHIBITED taking the caller's mode would raise EC-SIZE-TRUNCATION for
        // choosing a branch, which is not an intermediate value the rule can be talking about.
        if (Compare(Mul(m, m, CobolRounding.Truncation), Ten) >= 0)
        {
            m = new CobolDec(m.Sig, m.Exp - 1);        // m/10 ∈ [√⅒, 1) — exact
            k++;
        }
        var lnM = LnReduced(m, mode);
        return k == 0 ? lnM : Add(Mul(new CobolDec(k, 0), ln10, mode), lnM, mode);
    }

    /// <summary>ln of a value already in the reduction band, by the atanh series over one of TWO argument
    /// reductions. Also serves ln 10 itself (10 needs no band reduction: three roots take it to 1.334).
    /// <para>⛔ THE NEAR-UNIT ARM IS NOT AN OPTIMIZATION — IT IS THE ACCURACY (kb/Work PB269). The three-root
    /// reduction shifts the whole of m−1 into the ROUNDING of u: for m = 1 + δ the three 34-digit-rounded
    /// roots put an absolute error of ~1.5×10⁻³⁴ into u while u−1 is only ≈ δ/8, so <c>Sub(u, One)</c> keeps a
    /// relative error of ~10⁻³³/δ and ln m loses log₁₀(1/δ) digits — measured 17.05 correct digits at the worst
    /// point of |m−1| ≤ ¼, and EXACTLY ZERO at m = 1 + 10⁻³³ (the closest SDIDI value to one), where the first
    /// root rounds straight back to 1. Taking the same series over δ instead — ln(1+δ) = 2·atanh(δ/(2+δ)) —
    /// has no cancellation to lose: <c>Sub(m, One)</c> is EXACT for m in this band (both operands within a
    /// factor of two, so the difference is representable), and the measured floor over the same band is 32.74
    /// correct digits. It is also CHEAPER — no square root at all, and ≤20 series terms against the
    /// reduction's three BigInteger roots plus ~10 — so the band is chosen where the direct arm is both more
    /// accurate and less work, and the reduction keeps everything else (at |m−1| > ¼ the series over δ would
    /// need up to ~58 terms, and there u−1 is large enough that the reduction loses nothing).</para></summary>
    private static CobolDec LnReduced(CobolDec m, CobolRounding mode)
    {
        if (Compare(m, One) == 0) return new CobolDec(0, 0);
        var delta = Sub(m, One, mode);                                 // EXACT in the band
        if (Compare(Magnitude(delta), Quarter) <= 0)
            // ln(1 + δ) = 2·atanh(z), z = δ/(2+δ) — the same series, reached without cancelling m against 1.
            return Mul(Two, AtanhSeries(Div(delta, Add(Two, delta, mode), mode), mode), mode);
        var u = Sqrt(Sqrt(Sqrt(m, mode), mode), mode);                 // u = m^(1/8)
        var z = Div(Sub(u, One, mode), Add(u, One, mode), mode);       // z = (u−1)/(u+1)
        return Mul(new CobolDec(16, 0), AtanhSeries(z, mode), mode);   // 8 · (2·atanh z)
    }

    /// <summary>atanh(z) = z + z³/3 + z⁵/5 + … — the odd-only form, so each term costs |z|² and the tail is
    /// cut when a term can no longer move the accumulator's 34th digit. ONE series, both reductions.</summary>
    private static CobolDec AtanhSeries(CobolDec z, CobolRounding mode)
    {
        var z2 = Mul(z, z, mode);
        CobolDec term = z, sum = z;
        for (int n = 3; n < 200; n += 2)
        {
            term = Mul(term, z2, mode);
            if (term.Sig == 0) break;
            var next = Add(sum, Div(term, new CobolDec(n, 0), mode), mode);
            if (Compare(next, sum) == 0) { sum = next; break; }        // the term no longer moves the 34th digit
            sum = next;
        }
        return sum;
    }

    /// <summary>e^<paramref name="x"/> in SDIDI form.
    /// <para>ARGUMENT REDUCTION. n = the nearest integer to x / ln 10, r = x − n·ln 10 with |r| ≤ ½·ln 10 ≈
    /// 1.1513; then e^x = 10^n · e^r, and multiplying by 10^n is EXACT on a decimal carrier — it is an exponent
    /// field adjustment, so the whole decimal128 exponent range is reached without a single rounding. The
    /// §8.8.1.5.2 r2 range verdict stays <see cref="Clamp"/>'s (one place); n is screened first only so the
    /// exponent arithmetic cannot itself overflow.</para>
    /// <para>THE SERIES. e^r = Σ rⁿ/n!, accumulated as term ← term·r/n — one multiplication and one division per
    /// term, ~35 terms at the band edge, cut when a term can no longer move the accumulator's 34th digit. No
    /// squaring step, so no error amplification beyond the terms' own roundings.</para></summary>
    private static CobolDec Exponential(CobolDec x, CobolDec ln10, CobolRounding mode)
    {
        if (x.Sig == 0) return One;
        var q = Div(x, ln10, mode);
        // n = nearest integer to q. Beyond the decimal128 exponent range the result cannot exist; say so with
        // r2's OWN names before the exponent arithmetic below could wrap.
        if (Compare(q, new CobolDec(MaxAdjustedExp + 2, 0)) > 0)
            throw new CobolSizeError("standard-decimal intermediate exceeds the decimal128 range "
                + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW");
        if (Compare(q, new CobolDec(MinExp - 36, 0)) < 0)
            throw new CobolSizeError("standard-decimal intermediate is below the decimal128 range "
                + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-UNDERFLOW");
        int n = (int)ToRoundedIntegerValue(q);
        var r = Sub(x, Mul(new CobolDec(n, 0), ln10, mode), mode);
        CobolDec term = One, sum = One;
        for (int i = 1; i < 200; i++)
        {
            term = Div(Mul(term, r, mode), new CobolDec(i, 0), mode);
            if (term.Sig == 0) break;
            var next = Add(sum, term, mode);
            if (Compare(next, sum) == 0) { sum = next; break; }
            sum = next;
        }
        return Clamp(sum.Sig, sum.Exp + n, mode);      // × 10^n — exact on a decimal carrier
    }

    /// <summary>The nearest integer to an SDIDI value whose magnitude is known to fit <see cref="long"/> (the
    /// <see cref="Exponential"/> reduction's n, screened against the decimal128 exponent range by its caller).</summary>
    private static long ToRoundedIntegerValue(CobolDec v)
    {
        var half = v.Sig < 0 ? new CobolDec(-5, -1) : Half;
        // Truncation for the same reason the band test above uses it: choosing the reduction's integer n is not
        // a step in the development of the returned value (n is recovered EXACTLY afterwards, as 10^n).
        var t = Add(v, half, CobolRounding.Truncation);          // round-half-up via truncate(v ± ½)
        if (t.Exp >= 0)
        {
            Int128 r = t.Sig;
            for (int i = 0; i < t.Exp; i++) r *= 10;
            return (long)r;
        }
        var (qi, _, _) = DivRemPow10(t.Sig, -t.Exp);
        return (long)qi;
    }

    /// <summary>Whether the value is an INTEGER (a trailing-zero significand normalizes into the exponent
    /// first), with its magnitude in <paramref name="boundProbe"/>, its sign in <paramref name="negative"/>
    /// and its exact parity in <paramref name="even"/>.
    /// <para>⛔ <paramref name="boundProbe"/> IS A LOOP-BOUND PROBE, NEVER A VALUE. A magnitude past the long
    /// range CLAMPS to <see cref="long.MaxValue"/>, which answers "past the caller's 500 000 loop bound" and
    /// nothing else. This doc used to say the loop bound "rejects it before the MAGNITUDE is used" — it does
    /// not, it only ROUTES it, and the escape then raised the base to the clamped magnitude as though it were
    /// the written exponent (kb/Work PB267). The escape now carries the exponent OPERAND; the only consumer of
    /// this number is the `&gt; LoopBound` comparison and the square-and-multiply loop below it, where by
    /// construction it has not clamped.</para>
    /// <para>The SIGN and the exact PARITY ride their own flags, computed on the Int128 value — never
    /// re-derived from the clamped long, whose fixed parity (long.MaxValue is odd) gave (−1) ** 10²⁰ the wrong
    /// sign (kb/Work PB145).</para></summary>
    private static bool TryIntegerValue(CobolDec v, out long boundProbe, out bool negative, out bool even)
    {
        Int128 sig = v.Sig;
        int exp = v.Exp;
        while (exp < 0 && sig % 10 == 0) { sig /= 10; exp++; }   // normalize trailing zeros into the exponent
        negative = sig < 0;
        if (exp < 0) { boundProbe = 0; even = false; return false; }
        // |sig × 10^exp| is even iff any power of ten multiplies in (exp ≥ 1) or the significand is even.
        even = exp >= 1 || Int128.Abs(sig) % 2 == 0;
        // The magnitude sig × 10^exp, clamped where it leaves the long range (see the bound-probe note above).
        Int128 wide = Int128.Abs(sig);
        for (int i = 0; i < exp && wide <= long.MaxValue; i++) wide *= 10;
        boundProbe = wide > long.MaxValue ? long.MaxValue : (long)wide;
        return true;
    }

    /// <summary>Whether |value| = 1 (the Pow large-exponent parity shortcut).</summary>
    private static bool IsUnitMagnitude(CobolDec v)
    {
        Int128 sig = Int128.Abs(v.Sig);
        int exp = v.Exp;
        while (exp < 0 && sig % 10 == 0) { sig /= 10; exp++; }
        return sig == 1 && exp == 0;
    }

    public static CobolDec Add(CobolDec a, CobolDec b, CobolRounding mode) => AddSigned(a, b, negateB: false, mode);

    public static CobolDec Sub(CobolDec a, CobolDec b, CobolRounding mode) => AddSigned(a, b, negateB: true, mode);

    private static CobolDec AddSigned(CobolDec a, CobolDec b, bool negateB, CobolRounding mode)
    {
        Int128 bSig = negateB ? -b.Sig : b.Sig;
        if (a.Sig == 0) return Round34(bSig, b.Exp, sticky: false, mode);
        if (bSig == 0) return Round34(a.Sig, a.Exp, sticky: false, mode);

        // Align to the smaller exponent. Shift the higher-exponent significand UP while it fits the wide scratch
        // (38 digits); if the gap is larger, shift the LOWER one DOWN and account for its dropped tail — which has
        // the low operand's SIGN and so can pull the exact magnitude below the shifted sum as well as above it
        // (kb/Work PB1510; see the residual arm below). The tail lies below the result's 34-digit precision.
        (Int128 hiSig, int hiExp, Int128 loSig, int loExp) =
            a.Exp >= b.Exp ? (a.Sig, a.Exp, bSig, b.Exp) : (bSig, b.Exp, a.Sig, a.Exp);
        int gap = hiExp - loExp;
        int upRoom = 38 - DigitCount(Int128.Abs(hiSig));
        int up = Math.Min(gap, upRoom);
        hiSig *= Pow10.AsWide(up);
        int residual = gap - up;
        if (residual > 0)
        {
            // Down-shift the low operand by the residual; the dropped tail is nonzero exactly when `dropped`, and it
            // has the SIGN OF THE LOW OPERAND.
            Int128 loOriginal = loSig;
            (loSig, bool dropped) = ShiftDownSticky(loSig, residual);
            loExp += residual;
            Int128 near = hiSig + loSig;
            if (!dropped) return Round34(near, loExp, sticky: false, mode);
            // ⛔ THE DROPPED TAIL IS A SIGNED QUANTITY, AND THE STICKY BIT IS NOT (kb/Work PB1510). C# division
            // truncates toward zero, so the down-shifted low operand is always a little CLOSER TO ZERO than the
            // exact one: the exact sum is `near + tail` with |tail| < 1 unit and sign(tail) = sign(loOriginal).
            //   · tail and `near` of one sign — the exact magnitude is |near| + |tail|: `near` with sticky is the
            //     exact "truncated result plus a positive remainder" Round34Wide rounds.
            //   · tail and `near` of opposite signs (a SUBTRACTION, or an addition of opposite-signed operands) —
            //     the exact magnitude is |near| − |tail|, i.e. one unit below |near| plus a positive remainder, so
            //     the truncated magnitude is |near| − 1 and the remainder is (1 − |tail|) > 0. Folding the tail in as
            //     a bare sticky bit read it as EXCESS in the result's direction, which over-stated the magnitude:
            //     `1 − 1.0E−50` held 1.000…0 (and TRUNCATION kept it) where §11.9.11.2 GR3 e) owes the nearest SDIDI
            //     value NEARER ZERO, 0.999…9 (34 nines).
            // The jam is exact only while the digits that decide the rounding all come from `near`, i.e. while
            // |near| − 1 still has ≥ 35 digits. That holds whenever the operands are the ≤ 34-digit results every
            // operation returns (the shifted high operand alone is ≥ 10^37 and the shifted low one < 10^33); a
            // 38-digit operand built by CobolDec.From can cancel `near` down to a few digits, where the tail's OWN
            // digits are significant — that case is computed exactly instead: near·10^residual + (loOriginal mod
            // 10^residual) is an Int128 whenever |near| < 10^35 (residual is 1 there: the shifted high operand alone is ≥ 10^37).
            int tailSign = loOriginal < 0 ? -1 : 1;
            if (near == 0 || Int128.Abs(near) < Pow10.AsWide(35))
            {
                Int128 scale = Pow10.AsWide(residual);
                Int128 exact = near * scale + loOriginal % scale;
                return Round34(exact, loExp - residual, sticky: false, mode);
            }
            if (Int128.Sign(near) != tailSign) near -= Int128.Sign(near);   // one unit toward zero; the remainder is positive
            return Round34(near, loExp, sticky: true, mode);
        }
        return Round34(hiSig + loSig, loExp, sticky: false, mode);
    }

    /// <summary>Multiply: the exact 256-bit product reduces to 34 significant digits (§8.8.1.5).</summary>
    public static CobolDec Mul(CobolDec a, CobolDec b, CobolRounding mode)
    {
        bool negative = (a.Sig < 0) ^ (b.Sig < 0);
        var (hi, lo) = Mul128(UAbs(a.Sig), UAbs(b.Sig));
        return Round34Wide(hi, lo, negative, a.Exp + b.Exp, mode);
    }

    /// <summary>⛔ THE PRODUCT A NATIVE STATEMENT FORMS ON THE SDIDI WHEN ITS SCALED OPERANDS COULD LEAVE <c>Int128</c>
    /// (kb/Work PB1143 — <c>NumericRenderer.Multiply</c>): the exact 256-bit product reduced to 34 significant digits
    /// by ROUND-TO-ODD. Native arithmetic rounds ONCE, at the final transfer (§14.7 NOTE 1), and that transfer's mode
    /// has to see whether anything lay beyond the 34th digit — a bare truncation hid it (a product 1 + 2e-30 + 1e-60
    /// under ROUNDED AWAY-FROM-ZERO stored …002 where …003 is owed; a product above a NEAREST-EVEN tie became the tie;
    /// PROHIBITED stored an inexact product silently). Round-to-odd keeps the truncated magnitude when its last digit is
    /// odd and the next one up when it is even, whenever digits were dropped: the stored neighbour is never on a
    /// rounding boundary of a coarser precision, so rounding THIS value to the receiver's ≤ 31 digit positions (a
    /// margin of at least 3 digits, only 2 are needed) in ANY mode equals rounding the exact product once. A product
    /// of ≤ 34 digits is exact and unchanged.</summary>
    public static CobolDec MulToOdd(CobolDec a, CobolDec b)
    {
        bool negative = (a.Sig < 0) ^ (b.Sig < 0);
        var (hi, lo) = Mul128(UAbs(a.Sig), UAbs(b.Sig));
        return Round34Wide(hi, lo, negative, a.Exp + b.Exp, CobolRounding.Truncation, jam: true);
    }

    /// <summary>Divide: the dividend pre-scales so the exact quotient carries ≥34 significant digits, the
    /// shift-subtract 256÷128 division yields quotient+remainder, and one rounding lands the SDIDI result.
    /// A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (§14.7.5 case 2), checked or not — the SDIDI
    /// arm of the three value lanes' one zero-divisor rule.</summary>
    public static CobolDec Div(CobolDec a, CobolDec b, CobolRounding mode) => Div(a, b, mode, jam: false);

    /// <summary>The quotient, reduced to 34 significant digits by ROUND-TO-ODD — <see cref="MulToOdd"/>'s sibling for
    /// <c>CobolNum.Divide</c>'s alignment-past-the-carrier path (kb/Work PB1143): the remainder of the division and any
    /// digit dropped by the reduction make the result inexact, and an inexact result keeps an ODD last digit, so the
    /// caller's single rounding to the result scale (any mode, ≥ 2 digits coarser) equals rounding the exact quotient.</summary>
    public static CobolDec DivToOdd(CobolDec a, CobolDec b) => Div(a, b, CobolRounding.Truncation, jam: true);

    private static CobolDec Div(CobolDec a, CobolDec b, CobolRounding mode, bool jam)
    {
        if (b.Sig == 0) throw CobolSizeError.ZeroDivide();
        if (a.Sig == 0) return new CobolDec(0, 0);
        bool negative = (a.Sig < 0) ^ (b.Sig < 0);
        UInt128 den = UAbs(b.Sig);

        // Pre-scale the numerator so the integer quotient has 34–36 significant digits.
        // ⛔ THE PRE-SCALE MAY EXCEED 10^38 AND MUST STILL BE APPLIED IN FULL (kb/Work PB83 — found landing PB69):
        // a short numerator over a long denominator wants scaleUp = 34 + digits(den) − digits(num) + 1, up to 73
        // for a 1-digit numerator and a 38-digit denominator. The multiplier used to be CAPPED at 10^38 while the
        // result exponent still subtracted the UNCAPPED scaleUp, so `100000 / 123456789012345678901234567890`
        // (scaleUp 59) answered 0 under STANDARD-DECIMAL where 8.1E-25 is owed — wrong by 10^(scaleUp − 38). The
        // scale is applied in two exact steps: first inside the numerator's own Int128 headroom
        // (k = 38 − digits(num), exact), then the remainder through the 256-bit product — which is at most 10^36
        // (scaleUp − k = digits(den) − 3 ≤ 36 for a ≤39-digit divisor), so the second factor always fits.
        UInt128 num = UAbs(a.Sig);
        int scaleUp = Math.Max(0, 34 + DigitCount(den) - DigitCount(num) + 1);
        int k = Math.Min(scaleUp, 38 - DigitCount(num));
        num *= (UInt128)Pow10.AsWide(k);                                    // exact — digits(num) + k ≤ 38
        var (hi, lo) = Mul128(num, (UInt128)Pow10.AsWide(scaleUp - k));   // scaleUp − k ≤ 36
        var (q, rem) = DivRem256(hi, lo, den);

        // q < 10^37 by construction → fits Int128. Round to 34 digits, folding the division remainder into sticky.
        return Round34Wide(0, q, negative, a.Exp - scaleUp - b.Exp, mode, extraSticky: rem != 0, jam: jam);
    }

    /// <summary>Algebraic comparison (−1/0/+1) — exact: equal orders of magnitude align within the wide range;
    /// different orders decide by magnitude.</summary>
    public static int Compare(CobolDec a, CobolDec b)
    {
        int sa = a.Sig == 0 ? 0 : a.Sig < 0 ? -1 : 1;
        int sb = b.Sig == 0 ? 0 : b.Sig < 0 ? -1 : 1;
        if (sa != sb) return sa.CompareTo(sb);
        if (sa == 0) return 0;
        int oa = DigitCount(Int128.Abs(a.Sig)) + a.Exp;   // order of magnitude (digits left of 10^0)
        int ob = DigitCount(Int128.Abs(b.Sig)) + b.Exp;
        if (oa != ob) return sa > 0 ? oa.CompareTo(ob) : ob.CompareTo(oa);
        // Same order ⇒ aligning to the smaller exponent lands both within 34+|order-gap=0| ≤ 38 digits.
        int e = Math.Min(a.Exp, b.Exp);
        Int128 av = a.Sig * Pow10.AsWide(a.Exp - e), bv = b.Sig * Pow10.AsWide(b.Exp - e);
        return av.CompareTo(bv);
    }

    /// <summary>The value as an unscaled integer at <paramref name="scale"/> fraction digits, rounded with the
    /// RECEIVER's mode — the UNCHECKED §14.7 final transfer (a MOVE, or an arithmetic statement with no SIZE
    /// ERROR phrase and no EC-SIZE checking): a magnitude past the Int128 carrier keeps only the low-order digits
    /// a ≤38-digit store could use, which is the same high-order truncation the store then applies to a value
    /// that overflows its picture (§14.9.25.4 GR6 for MOVE; the documented no-phrase disposition for arithmetic).
    /// The CHECKED transfer is <see cref="ToUnscaledChecked"/>.</summary>
    public Int128 ToUnscaled(int scale, CobolRounding mode) => ToUnscaledCore(scale, mode, checkedTransfer: false);

    /// <summary>The SIZE-ERROR-CHECKED §14.7 final transfer (kb/Work PB74): identical to <see cref="ToUnscaled"/>
    /// except that a magnitude the Int128 carrier cannot hold — which no fixed-point receiver can hold either —
    /// raises <see cref="CobolSizeError"/> EC-SIZE-TRUNCATION (§14.7.5 case 3: "the result of an arithmetic
    /// statement is further from zero than permitted for the associated resultant data item"; no-phrase rule 4
    /// names the condition) instead of returning the low-order digits. ⛔ The unchecked arm's "keep only the
    /// digits a store could use" returned 0 for 10¹⁰⁰, and <c>CobolNum.TryStore(CobolDec, …)</c> then
    /// capacity-checked THAT 0 and stored it — under STANDARD-DECIMAL <c>COMPUTE X5 = 10 ** 100 ON SIZE ERROR</c>
    /// ran NOT ON SIZE ERROR and overwrote the receiver, where storing rule 1 requires it unchanged.
    /// <c>TryStore(CobolDec)</c> and the emitter's checked numeric-edited transfer ride this one.</summary>
    public Int128 ToUnscaledChecked(int scale, CobolRounding mode) => ToUnscaledCore(scale, mode, checkedTransfer: true);

    /// <summary>The INTERMEDIATE landing (kb/Work PB69): an SDIDI value entering the native Int128 carrier as an
    /// argument, an arithmetic operand or a subscript — never a store. A magnitude the carrier cannot hold at
    /// <paramref name="scale"/> is the §14.7.5 case-5 size error condition (the intermediate range is checked,
    /// A.1 item 179): EC-SIZE-OVERFLOW, never the low-order digits — a value-semantics consumer has no capacity
    /// check downstream to catch a truncated value (a native integer power past the window used to reach
    /// FUNCTION MOD as such digits, or, before that, as a saturated sentinel).</summary>
    public Int128 ToUnscaledIntermediate(int scale, CobolRounding mode) => ToUnscaledCore(scale, mode, checkedTransfer: true, intermediate: true);

    private Int128 ToUnscaledCore(int scale, CobolRounding mode, bool checkedTransfer, bool intermediate = false)
    {
        int shift = Exp + scale;
        if (Sig == 0) return 0;
        if (shift >= 0)
        {
            Int128 sig = Sig;
            if (DigitCount(Int128.Abs(sig)) + shift > 38)
            {
                if (intermediate)
                    throw new CobolSizeError("an intermediate value exceeds the native Int128 carrier at this scale "
                        + "(ISO §14.7.5 case 5 — the implementor-defined intermediate range is checked, A.1 item 179: EC-SIZE-OVERFLOW)",
                        "EC-SIZE-OVERFLOW");
                if (checkedTransfer)
                    throw new CobolSizeError("standard-decimal result is further from zero than any fixed-point "
                        + "receiver permits (ISO §14.7.5 case 3 — EC-SIZE-TRUNCATION; the receiver is left unchanged)",
                        "EC-SIZE-TRUNCATION");
                // Widening: keep only digits a ≤38-digit store could ever use; the store's own capacity rules apply.
                sig %= Pow10.AsWide(Math.Max(0, 38 - shift));
            }
            if (sig == 0) return 0;                            // a far-out-of-range value keeps no store digits
            return sig * Pow10.AsWide(shift);
        }
        var (q, rem, den) = DivRemPow10(Sig, -shift);
        return RoundFromRemainder(q, rem, den, sticky: false, mode);
    }

    /// <summary>The SDIDI square root (ISO §15.84.4 r1/r2; kb/Work PB116) — the ONE §15 function whose
    /// standard-mode returned value the standard fixes EXACTLY: "computed to 34 digits, and the result rounded
    /// to 34 digits according to the rules for standard-decimal arithmetic" (r2), with "argument-1 is not
    /// rounded" (r1 — the EXACT operand enters: a fixed-point operand converts exactly, §8.8.1.5.2). Computed
    /// as an EXACT integer floor square root over the significand scaled to ≥ 71 digits at an even power — the
    /// 36–37-digit integer root carries 2–3 guard digits below the 34-digit rounding position, and the floor
    /// remainder becomes the sticky bit, so <see cref="Round34(Int128, int, bool, CobolRounding)"/>'s landing is
    /// CORRECTLY rounded in every mode (a tie exists only when the root is exact, and then sticky is false).
    /// Negative arguments are the caller's §15.84.3 r2 screen; a defensive zero comes back for them.</summary>
    public static CobolDec Sqrt(CobolDec v, CobolRounding mode)
    {
        if (v.Sig <= 0) return new CobolDec(0, 0);
        var sig = (System.Numerics.BigInteger)v.Sig;
        int digits = 1;
        for (var t = sig; t >= 10; t /= 10) digits++;
        int k = 72 - digits;
        if (((v.Exp - k) & 1) != 0) k++;
        var n = sig * System.Numerics.BigInteger.Pow(10, k);
        var q = ISqrt(n);
        bool exact = q * q == n;
        return Round34((Int128)q, (v.Exp - k) / 2, sticky: !exact, mode);
    }

    /// <summary>The exact floor integer square root (Newton over <see cref="System.Numerics.BigInteger"/> —
    /// monotone descent from an over-estimate; terminates in O(log log n) iterations).</summary>
    private static System.Numerics.BigInteger ISqrt(System.Numerics.BigInteger n)
    {
        if (n < 2) return n;
        var x = System.Numerics.BigInteger.One << (int)(n.GetBitLength() / 2 + 1);
        while (true)
        {
            var y = (x + n / x) >> 1;
            if (y >= x) return x;
            x = y;
        }
    }

    /// <summary>The value as a <see cref="double"/> (the float-context bridge, e.g. exponentiation) — the
    /// CORRECTLY-ROUNDED double, through the ONE scaled→double conversion (kb/Work PB115: the former
    /// <c>(double)Sig * Math.Pow(10, Exp)</c> rounded twice over an inexact power and overshot at scale 25,
    /// independently of the emit lane's sibling defect).</summary>
    public double ToDouble() => CobolFloat.ScaledToDouble(Sig, -Exp);

    /// <summary>The CORRECTLY-ROUNDED binary32 of this value in one rounding — <see cref="CobolFloat.ScaledToSingle"/>
    /// (kb/Work PB1110), never <see cref="ToDouble"/> narrowed.</summary>
    public float ToSingle() => CobolFloat.ScaledToSingle(Sig, -Exp);

    /// <summary>The text image of an SDIDI intermediate used as an intrinsic function's returned value in a
    /// string context (DA2). An SDIDI carries its own exponent, so the fixed-point scale is <c>-Exp</c>; routing
    /// through <see cref="CobolNum.FormatFunctionText"/> rather than formatting here keeps ONE rendering rule for
    /// a function result, whichever arithmetic mode produced it (§8.8.1.5 vs native, ISO §15.4.1).</summary>
    public string ToFunctionText(bool deSign = false) => CobolNum.FormatFunctionText(Sig, -Exp, deSign);

    // ── 34-digit rounding core ───────────────────────────────────────────────────────────────────────────────

    private static CobolDec Round34(Int128 sig, int exp, bool sticky, CobolRounding mode)
    {
        bool negative = sig < 0;
        UInt128 mag = UAbs(sig);
        return Round34Wide(0, mag, negative, exp, mode, extraSticky: sticky);
    }

    /// <summary>Reduce a 256-bit magnitude (<paramref name="hi"/>:<paramref name="lo"/>) to a ≤34-digit SDIDI
    /// significand: divide by 10 until in range, capturing the LAST dropped digit (the round digit) and whether
    /// any earlier dropped digit was nonzero (sticky); then apply the INTERMEDIATE ROUNDING mode (§11.9.11 —
    /// PROHIBITED ⇒ size error when anything was dropped).</summary>
    private static CobolDec Round34Wide(UInt128 hi, UInt128 lo, bool negative, int exp, CobolRounding mode,
        bool extraSticky = false, bool jam = false)
    {
        bool sticky = extraSticky;
        int roundDigit = 0;
        while (hi != 0 || lo >= (UInt128)Limit34)
        {
            sticky |= roundDigit != 0;
            (hi, lo, roundDigit) = DivRem10_256(hi, lo);
            exp++;
        }
        Int128 sig = (Int128)lo;
        bool inexact = roundDigit != 0 || sticky;
        if (inexact && jam)
        {
            // ROUND-TO-ODD (kb/Work PB1143 — see MulToOdd): keep the truncated magnitude if its last digit is odd, else the
            // next one up. Neither neighbour lies ON a coarser rounding boundary (a multiple of 10 is even), so any later
            // rounding to ≤ 32 digits, in any mode, sees the exact value on the same side of every boundary.
            if (sig % 2 == 0) sig++;
            if (sig == (Int128)Limit34) { sig /= 10; exp++; }
        }
        else if (inexact)
        {
            switch (mode)
            {
                case CobolRounding.Prohibited:
                    // §11.9.11.2 r3d: PROHIBITED + not exactly representable in SDIDI form ⇒ EC-SIZE-TRUNCATION,
                    // results undefined. The level-3 NAME travels with the raise (kb/Work PB74's sweep): a
                    // >>TURN EC-SIZE program selecting on EXCEPTION-STATUS saw the default EC-SIZE-OVERFLOW here.
                    throw new CobolSizeError("INTERMEDIATE ROUNDING IS PROHIBITED: inexact standard-decimal intermediate "
                        + "(ISO §11.9.11.2 r3d — EC-SIZE-TRUNCATION)", "EC-SIZE-TRUNCATION");
                case CobolRounding.Truncation:
                    break;
                case CobolRounding.NearestEven:
                    if (roundDigit > 5 || (roundDigit == 5 && (sticky || sig % 2 != 0))) sig++;
                    break;
                default:   // NearestAwayFromZero — the §11.9.11 r3a default
                    if (roundDigit >= 5) sig++;
                    break;
            }
            if (sig == (Int128)Limit34) { sig /= 10; exp++; }   // 999…9 rounded up → 100…0 × 10
        }
        return Clamp(negative ? -sig : sig, exp, mode);
    }

    // decimal128 range bounds (ISO §8.8.1.5.2 NOTE 2): largest |value| 9.999…9E+6144 (34 nines), smallest
    // positive nonzero (subnormal) 1.0E−6176.
    private const int MaxAdjustedExp = 6144;
    private const int MinExp = -6176;

    /// <summary>The §8.8.1.5.2 r2 decimal128 range check, applied by the ONE rounding funnel to every operation
    /// result: a value whose adjusted exponent exceeds +6144 raises the size error condition with
    /// EC-SIZE-OVERFLOW; a value below the smallest subnormal quantum (10^−6176) re-rounds onto that quantum
    /// (the IEC 60559 subnormal range) under the INTERMEDIATE ROUNDING mode — a nonzero value that rounds to
    /// zero there is too small to be contained and raises EC-SIZE-UNDERFLOW.</summary>
    private static CobolDec Clamp(Int128 sig, int exp, CobolRounding mode)
    {
        if (sig == 0) return new CobolDec(0, 0);
        if (DigitCount(Int128.Abs(sig)) + exp - 1 > MaxAdjustedExp)
            throw new CobolSizeError("standard-decimal intermediate exceeds the decimal128 range "
                + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW");
        if (exp < MinExp)
        {
            // Re-round onto the 10^−6176 quantum (drop exp − MinExp digits with the true remainder).
            var (q, rem, den) = DivRemPow10(sig, MinExp - exp);
            // Under INTERMEDIATE ROUNDING IS PROHIBITED the below-range re-round is still r2's TOO-SMALL
            // condition — EC-SIZE-UNDERFLOW, never §14.7.4.3 r7's inexact-transfer TRUNCATION (kb/Work PB145).
            // ⚠ THE SCOPE OF THAT RULE IS THIS RE-ROUND, AND ONLY IT. PB145 established one name for one
            // OUT-OF-RANGE condition; it did not make EC-SIZE-TRUNCATION unreachable under PROHIBITED. An
            // in-range intermediate that simply cannot be held exactly in 34 digits is a different physical
            // condition and §11.9.11.2 r3d REQUIRES the truncation name for it ("If the PROHIBITED phrase is
            // specified and an intermediate value cannot be represented exactly in SDIDI form, the
            // EC-SIZE-TRUNCATION exception condition is set to exist" — cite.py-verified), which is what
            // Round34Wide raises above. The transcendental development reaches that arm on its first inexact
            // step, so `0.5 ** 600000` under PROHIBITED reports EC-SIZE-TRUNCATION and never gets far enough
            // to be out of range — correct, and not a breach of the rule this comment states.
            if (mode == CobolRounding.Prohibited && rem != 0)
                throw new CobolSizeError("standard-decimal intermediate is below the decimal128 range "
                    + "(ISO §8.8.1.5.2 r2; INTERMEDIATE ROUNDING IS PROHIBITED)", "EC-SIZE-UNDERFLOW");
            Int128 r = RoundFromRemainder(q, rem, den, sticky: false, mode);
            if (r == 0)
                throw new CobolSizeError("standard-decimal intermediate is below the decimal128 range "
                    + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-UNDERFLOW");
            return new CobolDec(r, MinExp);
        }
        return new CobolDec(sig, exp);
    }

    private static Int128 RoundFromRemainder(Int128 q, Int128 rem, Int128 den, bool sticky, CobolRounding mode)
    {
        if (rem == 0 && !sticky) return q;
        Int128 absRem2 = Int128.Abs(rem) * 2;
        int sign = q < 0 || rem < 0 ? -1 : 1;
        return mode switch
        {
            // §14.7.4.3 r7 — the level-3 name is EC-SIZE-TRUNCATION (PB74's sweep: the default name latched OVERFLOW).
            CobolRounding.Prohibited => throw new CobolSizeError("ROUNDED MODE IS PROHIBITED on an inexact transfer "
                + "(ISO §14.7.4.3 r7 — EC-SIZE-TRUNCATION; the receiver is left unchanged)", "EC-SIZE-TRUNCATION"),
            CobolRounding.Truncation => q,
            CobolRounding.AwayFromZero => q + sign,
            CobolRounding.TowardGreater => sign > 0 ? q + 1 : q,
            CobolRounding.TowardLesser => sign < 0 ? q - 1 : q,
            CobolRounding.NearestEven => absRem2 > den || (absRem2 == den && !sticky && q % 2 != 0) ? q + sign : q,
            CobolRounding.NearestTowardZero => absRem2 > den || (absRem2 == den && sticky) ? q + sign : q,
            _ => absRem2 >= den ? q + sign : q,   // NearestAwayFromZero
        };
    }

    // ── wide scratch primitives (256-bit as UInt128 hi:lo) ──────────────────────────────────────────────────

    private static (UInt128 Hi, UInt128 Lo) Mul128(UInt128 a, UInt128 b)
    {
        // Schoolbook over 64-bit limbs via Math.BigMul.
        ulong a0 = (ulong)a, a1 = (ulong)(a >> 64);
        ulong b0 = (ulong)b, b1 = (ulong)(b >> 64);
        UInt128 p00 = (UInt128)Math.BigMul(a0, b0, out ulong p00lo) << 64 | p00lo;
        // p00 = a0*b0 (exact 128); cross terms shift 64; top term shifts 128.
        UInt128 cross1 = (UInt128)a0 * b1;
        UInt128 cross2 = (UInt128)a1 * b0;
        UInt128 top = (UInt128)a1 * b1;

        UInt128 lo = p00;
        UInt128 hi = top;
        UInt128 mid = cross1 + cross2;
        bool midCarry = mid < cross1;                     // 129th bit of the cross sum
        UInt128 midLoPart = mid << 64;
        lo += midLoPart;
        if (lo < midLoPart) hi += 1;
        hi += (mid >> 64) + (midCarry ? (UInt128)1 << 64 : 0);
        return (hi, lo);
    }

    private static (UInt128 Hi, UInt128 Lo, int Digit) DivRem10_256(UInt128 hi, UInt128 lo)
    {
        UInt128 qHi = hi / 10;
        UInt128 rHi = hi % 10;
        // lo with the carried remainder: process as two 64-bit limbs to stay in UInt128 range.
        UInt128 cur1 = (rHi << 64) | (lo >> 64);
        UInt128 q1 = cur1 / 10, r1 = cur1 % 10;
        UInt128 cur0 = (r1 << 64) | (ulong)lo;
        UInt128 q0 = cur0 / 10, r0 = cur0 % 10;
        return (qHi, (q1 << 64) | q0, (int)r0);
    }

    /// <summary>Exact 256 ÷ 128 by binary shift-subtract (≤256 iterations) — clarity over speed until profiled.</summary>
    private static (UInt128 Quotient, UInt128 Remainder) DivRem256(UInt128 hi, UInt128 lo, UInt128 den)
    {
        if (hi == 0) return (lo / den, lo % den);
        UInt128 q = 0, rem = 0;
        for (int i = 255; i >= 0; i--)
        {
            rem <<= 1;
            UInt128 word = i >= 128 ? hi : lo;
            if (((word >> (i & 127)) & 1) != 0) rem |= 1;
            if (rem >= den)
            {
                rem -= den;
                if (i < 128) q |= (UInt128)1 << i;
                // a set quotient bit at i ≥ 128 cannot occur: the caller bounds the quotient below 2^128
            }
        }
        return (q, rem);
    }

    /// <summary>Divide by 10^<paramref name="n"/> keeping the true remainder for the rounding decision. Past the
    /// Int128 carrier (<paramref name="n"/> &gt; 38) the quotient is 0 and the value is a NONZERO remainder that is
    /// strictly BELOW HALF a unit — the marker is <c>(0, ±1, 4)</c>, i.e. rem/den = ¼ carrying the value's sign.
    /// ⛔ It was <c>(0, 1, 2)</c> — EXACTLY HALF — so <see cref="RoundFromRemainder"/>'s NEAREST arms treated a
    /// value 10⁻⁴⁴ units below the target scale as a tie and lifted it to one unit: under STANDARD-DECIMAL
    /// <c>COMPUTE R9 ROUNDED = 10 ** -20</c> stored 0.000000001 into <c>V9(9)</c> (§14.7.4.3 r4 — "the nearest value
    /// that can be represented"; a tie is "two such values equally near", which this is not), and the unsigned
    /// marker turned AWAY-FROM-ZERO / TOWARD-GREATER of a NEGATIVE value toward +∞. The 34-digit significand
    /// makes the shape common (1/10²⁰ is 10³³×10⁻⁵³, 44 places below scale 9); kb/Work PB76.</summary>
    private static (Int128 Q, Int128 Rem, Int128 Den) DivRemPow10(Int128 v, int n)
    {
        Int128 den = Pow10.AsWide(Math.Min(n, 38));
        if (n > 38) return (0, v == 0 ? 0 : v < 0 ? -1 : 1, 4);   // far below precision: below-half inexact marker
        return (v / den, v % den, den);
    }

    private static UInt128 UAbs(Int128 v) => v < 0 ? (UInt128)(-v) : (UInt128)v;

    private static int DigitCount(Int128 mag)
    {
        int n = 1;
        while (mag >= 10) { mag /= 10; n++; }
        return n;
    }

    private static int DigitCount(UInt128 mag)
    {
        int n = 1;
        while (mag >= 10) { mag /= 10; n++; }
        return n;
    }

    private static (Int128 Sig, bool Sticky) ShiftDownSticky(Int128 sig, int n)
    {
        if (n > 38) return (0, sig != 0);
        Int128 den = Pow10.AsWide(n);
        return (sig / den, sig % den != 0);
    }

}
