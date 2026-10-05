// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F2 — exact numeric / integer / statistics / NUMVAL intrinsics (ISO §15; deep-dive D1: exact-numeric
/// functions stay base-10 EXACT as unscaled <see cref="Int128"/> values at a known decimal scale — never double,
/// never decimal). The "<c>…Scaled</c>" variadic entries take arguments ALREADY ALIGNED by the emitter to one
/// common scale (the same Align machinery the arithmetic verbs use, ISO §8.8.1), so value comparison/arithmetic on
/// the unscaled longs IS comparison/arithmetic on the algebraic values.
/// <para>
/// ⚠ <b>"Never double" scopes the ARGUMENT REPRESENTATION these bodies accept, not the family's whole surface.</b>
/// A floating-point argument is legal for every one of these functions (§15.7.3 r1 and siblings require class
/// NUMERIC; §8.5.2.12 item 2 makes COMP-1/COMP-2 category numeric, hence class numeric by §8.5.2.1 Table 2), and
/// it is served by the <c>…Real</c> twins in <c>CobolIntrinsics.RealArgs.cs</c> — reached by routing on the
/// ARGUMENT's type rather than the function's family. Nothing exact is surrendered: once the operand arrives as
/// binary64 there is no exact value left to preserve, and §15.4.1 makes the returned value implementor-defined
/// under native arithmetic. Read as an absolute, the sentence above said a legal program had no path at all —
/// which is exactly what it meant before PB2, when such a program emitted a raw Roslyn <c>CS1503</c>.
/// </para>
/// <para><b>Standard arithmetic (ISO §15.4.1 r1 / §8.8.1.5, P10 Step 12).</b> Under ARITHMETIC IS STANDARD /
/// STANDARD-DECIMAL a function with an equivalent arithmetic expression must return EXACTLY that expression's
/// SDIDI-evaluated value. Where the two carriers agree they agree DIGIT-FOR-DIGIT (§8.8.1.5.2 — an exact ≤34-digit
/// result never rounds), which covers ABS/SIGN/INTEGER/INTEGER-PART/FRACTION-PART and the pure-SELECTION
/// MAX/MIN/ORD-MAX/ORD-MIN outright.
/// <para>⛔ <b>They do NOT agree wherever the emitter first ALIGNS the arguments to one common scale</b>, and an
/// earlier revision of this paragraph asserted they did — "MOD/REM … ≤31 digits", "SUM (≤32-digit sums per step)".
/// Both bounds are the UNALIGNED ones. Alignment multiplies: a 31-digit integer beside a scale-18 operand needs 49
/// digits, past this carrier entirely, and three aligned 38-digit SUM arguments exceed it while each one passes the
/// per-argument escape. That false premise is precisely why the always-SDIDI routing was built one arm at a time
/// (kb/Work PB62, then PB252). The rule is now structural and lives in ONE place — <c>IntrinsicRenderer</c>'s
/// <c>CrossAlignedNativeArms</c>: <b>every native arm that cross-aligns (MOD · REM · SUM · RANGE · MEAN · MEDIAN ·
/// MIDRANGE) routes to its <c>…Dec</c> body under a standard mode</b>, so §15.4.1 r1 is satisfied by the SDIDI and
/// not by a bound on this carrier. Under NATIVE arithmetic these bodies keep the exact-Int128 lane and reaching the
/// D1 boundary is the §14.7.5 SIZE ERROR condition, never a wrap (see <c>SizeEscape</c>).</para>
/// <para>The ONE recorded residue: a result whose exact form exceeds 34 significant digits (FACTORIAL of 31–33)
/// stays exact-Int128 here where the SDIDI evaluation would round each step to 34 digits — a divergence in the
/// direction of MORE precision, undetectable at bind time (argument values are runtime data) and recorded in
/// COBOLNET_NUMERIC_DESIGN §D3 rather than staged.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    // ── Integer functions (ISO §15.36 / §15.44 / §15.49 / §15.81) ─────────────────────────────────────────────

    /// <summary>FACTORIAL (§15.36.4) on the NATIVE lane: 0 ⇒ 1; n ⇒ n × (n−1)!. Computed in
    /// <see cref="Int128"/>: 33! ≈ 8.68e36 fits (Int128.Max ≈ 1.70e38); 34! is the first overflow. A NEGATIVE
    /// argument violates §15.36.3 r1 — EC-ARGUMENT-FUNCTION, the §15.3 default 0 with checking disabled. A
    /// 34+ argument CONFORMS (r1 admits every nonnegative integer), so it is NOT an argument error (kb/Work
    /// PB125 — the old arm returned the default 0 there, and zero is no §15.4.1 "approximation" of 2.95e38):
    /// it is the size error condition — the §15.36.4 r1c equivalent arithmetic expression's value exceeds the
    /// native Int128 intermediate (CONFORMANCE.md item 179's class: ON SIZE ERROR / EC-SIZE checking take it;
    /// without either, item 70's an-intermediate-that-cannot-be-formed fatal termination). Under a STANDARD
    /// mode the renderer routes to <see cref="FactorialDec"/> instead, where 34! is exact.</summary>
    public static Int128 Factorial(long n)
    {
        if (n < 0)                                           // EC-ARGUMENT-FUNCTION raise point / §15.3 default 0
            return Exceptions.ExceptionState.ArgumentError($"FACTORIAL argument {n} violates §15.36.3 rule 1 (shall be an integer greater than or equal to zero)");
        if (n > 33)
            throw new CobolSizeError($"FACTORIAL({n}) exceeds the native Int128 intermediate (33! is the "
                + "largest representable factorial; ISO §15.36.4 r1c evaluated per §8.8.1.3)", "EC-SIZE-OVERFLOW");
        Int128 r = 1;
        for (long i = 2; i <= n; i++) r *= i;
        return r;
    }

    /// <summary>SIGN (§15.81.4, COBOL-2002+): −1 / 0 / +1 by the argument's algebraic sign (scale-independent —
    /// the unscaled carrier has the value's sign).</summary>
    public static long SignOf(Int128 v) => v > 0 ? 1 : v < 0 ? -1 : 0;

    /// <summary>INTEGER (§15.44.4): the greatest integer ≤ the argument (floor). <paramref name="v"/> is the
    /// unscaled value at <paramref name="scale"/> fraction digits; the result is a scale-0 integer.</summary>
    public static Int128 Floor(Int128 v, int scale)
    {
        if (scale <= 0) return v * Pow10.AsWide(-scale);           // already an integer (negative scale = P-trailing zeros)
        Int128 d = Pow10.AsWide(scale);
        Int128 q = v / d;
        return v < 0 && v % d != 0 ? q - 1 : q;
    }

    /// <summary>INTEGER-PART (§15.49.4): the integer part of the argument (truncation toward zero), scale 0.</summary>
    public static Int128 Truncate(Int128 v, int scale) => scale <= 0 ? v * Pow10.AsWide(-scale) : v / Pow10.AsWide(scale);

    /// <summary>ABS (§15.7.4, COBOL-2014+): the absolute value, at the argument's own scale.</summary>
    public static Int128 AbsScaled(Int128 v) => v < 0 ? -v : v;

    /// <summary>FRACTION-PART (§15.42.4, COBOL-2002+): <c>argument − FUNCTION INTEGER-PART(argument)</c> — the
    /// fractional part with the argument's sign, at the argument's own scale.</summary>
    public static Int128 FractionPart(Int128 v, int scale) => scale <= 0 ? 0 : v % Pow10.AsWide(scale);

    // ── MOD / REM (ISO §15.64 / §15.77) — over scale-ALIGNED unscaled values ──────────────────────────────────

    /// <summary>
    /// ⛔ THE §15.64.3 r2 ZERO-DIVISOR RULE — ONE RAISE SITE PER RULE, NOT ONE PER CARRIER (fix-queue PB32).
    /// </summary>
    /// <remarks>
    /// MOD is implemented TWICE — the exact <see cref="ModScaled"/> and the binary64 <c>ModReal</c> that
    /// <c>IntrinsicRenderer.RenderNum</c>'s <c>AnyRealArgument</c> route reaches — and the rule was written into
    /// both, then corrected in only one. <c>ModReal</c> read <c>b == 0 ? 0 : …</c>: a SECOND, INDEPENDENT
    /// zero-divisor guard that returned a finite 0.0 and never called <see cref="Exceptions.ExceptionState.ArgumentError"/>,
    /// so the exception condition was not mis-defaulted — it was never SET. Neither screen the renderer relies on
    /// could see it either, because <c>RealResult</c> and <c>FromDouble</c> both test only for NaN. Measured under
    /// <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING ON</c>: <c>DISPLAY FUNCTION MOD(A ** 2, Z)</c> printed 0 and
    /// execution CONTINUED past a condition Table 13 (§14.6.13.1.1) makes FATAL, while the very next statement —
    /// the same function through the exact body — correctly terminated the run unit.
    /// <para>The <c>long</c> return converts implicitly to both <see cref="Int128"/> and <see cref="double"/>, so
    /// every carrier's body can <c>return</c> this directly and the §15.3 default, the message and the clause
    /// citation cannot drift apart again. <c>IntrinsicCarrierAgreementDriftTests</c> asserts the two carriers
    /// agree on it.</para>
    /// </remarks>
    internal static long ModZeroDivisor() =>
        Exceptions.ExceptionState.ArgumentError("MOD with a zero divisor (ISO §15.64.3 rule 2)");

    /// <summary>The §15.77.3 r2 zero-divisor rule — one raise site for REM, whichever carrier evaluates it
    /// (see <see cref="ModZeroDivisor"/> for why this is not written per body).</summary>
    internal static long RemZeroDivisor() =>
        Exceptions.ExceptionState.ArgumentError("REM with a zero divisor (ISO §15.77.3 rule 2)");

    /// <summary>MOD (§15.64.4): <c>a − b × FUNCTION INTEGER(a / b)</c> — the floored modulus (the spec NOTE's sign
    /// table: −11 MOD 5 = 4, 11 MOD −5 = −4). Operands aligned to one scale, result at that scale (0 for the §15.64.3
    /// integer arguments). A zero divisor violates rule 2 → <see cref="ModZeroDivisor"/>.</summary>
    public static Int128 ModScaled(Int128 a, Int128 b)
    {
        if (b == 0) return ModZeroDivisor();                 // the ONE §15.64.3 r2 raise site (both carriers)
        Int128 q = a / b;                                    // truncating quotient of the ALIGNED values
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;        // → floor (FUNCTION INTEGER of the true ratio)
        // §15.64.3 r1 makes both arguments INTEGERS, so on conforming source the common scale is 0, no alignment
        // happens and |b×q| ≤ |a| ≤ 10³¹ — the D1 boundary is unreachable. It IS reachable under --permissive,
        // whose coercion extension admits a scaled operand and so a nonzero common scale: the floor adjustment
        // then lets |b×q| reach |a|+|b|, and with two aligned near-carrier operands that product wraps. Loud, by
        // the same policy as every other exact-carrier step (see SizeEscape; kb/Work PB252).
        try { return checked(a - b * q); }
        catch (OverflowException) { return SizeEscape("MOD"); }
    }

    /// <summary>REM (§15.77.4): <c>a − b × FUNCTION INTEGER-PART(a / b)</c> — the truncated remainder (sign follows
    /// the dividend). Operands aligned to one scale, result at that scale. Zero divisor → <see cref="RemZeroDivisor"/>.</summary>
    public static Int128 RemScaled(Int128 a, Int128 b) => b == 0
        ? RemZeroDivisor()                                   // the ONE §15.77.3 r2 raise site (both carriers)
        : a % b;   // C# % truncates toward zero — exactly INTEGER-PART

    // ── Variadic statistics over scale-ALIGNED unscaled values (ISO §15.59–§15.63, §15.71–72, §15.76, §15.88) ──

    /// <summary>MAX (§15.59.4): the greatest argument value, at the common scale.</summary>
    public static Int128 MaxScaled(params Int128[] xs)
    {
        RequireArguments(xs.Length, "MAX");
        Int128 m = xs[0];
        foreach (var x in xs) if (x > m) m = x;
        return m;
    }

    /// <summary>MIN (§15.63.4): the least argument value, at the common scale.</summary>
    public static Int128 MinScaled(params Int128[] xs)
    {
        RequireArguments(xs.Length, "MIN");
        Int128 m = xs[0];
        foreach (var x in xs) if (x < m) m = x;
        return m;
    }

    /// <summary>SUM (§15.88.4): Σ arguments, at the common scale, EXACT in Int128 or not at all —
    /// <paramref name="fn"/> names the COBOL function whose equivalent arithmetic expression is being evaluated,
    /// because this body serves TWO of them: §15.88.4's own Σ and the numerator of §15.60.4's <c>Σ / n</c>
    /// (the renderer emits <c>SumScaled("MEAN", …)</c> for MEAN — <c>IntrinsicRenderer</c>'s MeanScaled arm).</summary>
    /// <remarks>The accumulator is <c>checked</c> under ONE try for the whole loop (entering a try region is free;
    /// a per-iteration guard would not be). §15.88.3 r1 admits any class-numeric argument list, so the aligned
    /// values are bounded only by <see cref="NumericRenderer"/>'s PER-ARGUMENT escape — which says nothing about
    /// their SUM: three aligned 38-digit arguments exceed the carrier while each one passes. See
    /// <see cref="SizeEscape"/> for why a wrap is never the answer (kb/Work PB252).</remarks>
    public static Int128 SumScaled(string fn, params Int128[] xs)
    {
        RequireArguments(xs.Length, fn);
        try
        {
            Int128 s = 0;
            foreach (var x in xs) s = checked(s + x);
            return s;
        }
        catch (OverflowException) { return SizeEscape(fn); }
    }

    /// <summary>RANGE (§15.76.4 r1): <c>FUNCTION MAX − FUNCTION MIN</c>, at the common scale. The SUBTRACTION is
    /// the escape: both operands are argument CONTENTS that individually passed the per-argument escape, and their
    /// difference is up to twice the larger — a positive maximum less a negative minimum wrapped to a NEGATIVE
    /// range, a value §15.76.4 r1 cannot produce (kb/Work PB252).</summary>
    public static Int128 RangeScaled(params Int128[] xs)
    {
        RequireArguments(xs.Length, "RANGE");
        return ExactSub(MaxScaled(xs), MinScaled(xs), "RANGE");
    }

    /// <summary>
    /// ⛔ THE EXACT CARRIER'S ESCAPE BOUNDARY, WRITTEN ONCE (fix-queue PB32; generalized to the whole exact
    /// family by kb/Work PB252).
    /// </summary>
    /// <remarks>
    /// MEDIAN and MIDRANGE return at scale common+1 so that their halving is EXACT (odd: ×10; even/midrange: ×5).
    /// That trick is correct in the middle of the range and is exactly where they break at the top of it: it
    /// spends a decimal digit of <see cref="Int128"/> headroom that MAX / MIN / SUM / RANGE do not spend, so these
    /// two wrap at ONE FIFTH the magnitude their siblings survive. MEASURED, with an <c>ON SIZE ERROR</c> phrase
    /// present and NOT taken: for <c>P PIC S9(29)V99 = 99999999999999999999999999999.98</c> and
    /// <c>Q PIC SV9(9) = 0.000000002</c>, MAX returned <c>…999.98</c> and MIN returned <c>0.00</c> — both exact —
    /// while MIDRANGE returned <c>15971763307906153653662539256.81</c> against a true value of
    /// <c>49999999999999999999999999999.99</c> that FITS the receiver, and the compiler's OWN hand-written
    /// §15.62.4 EAE <c>(FUNCTION MAX(P Q) + FUNCTION MIN(P Q)) / 2</c> produced that correct value in the same run.
    /// <para><c>COBOLNET_NUMERIC_DESIGN.md</c>'s substrate paragraph already fixes the policy and the code simply
    /// did not implement it: "the Int128 escape boundary is reached only when a single product … exceeds Int128
    /// (~38 digits) → EC-SIZE-OVERFLOW". A wrap is never a conforming answer — §15.4.1 asks at worst for "an
    /// implementor-defined approximation", and a value 3.2× out with its top thirty digits wrong approximates
    /// nothing. <see cref="CobolSizeError"/> is the runtime's own carrier for that condition and lands on the
    /// statement's ON SIZE ERROR arm through the existing <c>ArithmeticEmitter</c> handler, leaving the receiver
    /// unchanged (§14.7.5 rule 1 under the SIZE ERROR phrase).</para>
    /// <para>⛔ The GOVERNING clause is <b>§14.7.5 rule 5</b>: "if native arithmetic is in effect and the
    /// implementor defines that the range of values allowed for the intermediate data item is to be checked, when
    /// an arithmetic operation on the intermediate data item would cause the new value to be outside of the
    /// allowed range". §8.8.1.3 makes native arithmetic implementor-defined, D1 defines the intermediate as
    /// Int128 and declares its range CHECKED, and r5 then makes each step past it the size error condition
    /// exactly. PB32's original message cited "§8.8.1.2 rule 7" — a REAL clause answering a DIFFERENT question
    /// (arithmetic expressions are exempt from the composite-of-operands limit; every other citation of r7 in the
    /// tree uses it correctly for that). Corrected by kb/Work PB252; it had been user-visible text.</para>
    /// <para>Left-to-right accumulation is normative (§8.8.1.2 rule 3), so a partial sum that escapes the carrier
    /// IS an r5 size error even when later terms would bring the total back inside — the modular cancellation the
    /// old unchecked <c>s += x</c> happened to deliver for such a list was never the standard's answer, it was
    /// two's complement's.</para>
    /// <para>⛔ PB32 wrote the policy for MEDIAN/MIDRANGE only, and the names said so (<c>ScaleForHalving</c>,
    /// <c>AddForHalving</c>) — so SUM's and RANGE's identical accumulators sat unguarded for another eight months
    /// (kb/Work PB252, the same two-arm shape PB62 left on the renderer's side). The policy is the FILE's, not
    /// MEDIAN's, so the primitives are now named for the OPERATION they guard and every arithmetic step on the
    /// exact carrier goes through one of them or an inline <c>checked</c> block —
    /// <c>ExactCarrierBoundaryDriftTests</c> holds that closed by REFLECTION over the whole entry-point set, so a
    /// new exact function is guarded or explicitly exempted before it can ship.</para>
    /// <para>Note this is deliberately per-site <c>checked</c> and NOT
    /// <c>&lt;CheckForOverflowUnderflow&gt;</c> on the project: the runtime's binary-field stores and truncation
    /// paths wrap MODULARLY on purpose (§13.18.40, §14.9.25.4 GR6), and a project-wide switch would turn every one
    /// of those conforming wraps into an exception.</para>
    /// </remarks>
    private static Int128 SizeEscape(string fn) =>
        throw new CobolSizeError(
            $"FUNCTION {fn}: an arithmetic operation took the intermediate outside the Int128 carrier's range "
            + "(COBOLNET_NUMERIC_DESIGN.md D1; ISO §14.7.5 rule 5)", "EC-SIZE-OVERFLOW");

    /// <summary>The sum of two aligned unscaled operands at the exact carrier's boundary
    /// (see <see cref="SizeEscape"/>).</summary>
    private static Int128 ExactAdd(Int128 a, Int128 b, string fn)
    {
        try { return checked(a + b); }
        catch (OverflowException) { return SizeEscape(fn); }
    }

    /// <summary>The difference of two aligned unscaled operands at the exact carrier's boundary
    /// (see <see cref="SizeEscape"/>).</summary>
    private static Int128 ExactSub(Int128 a, Int128 b, string fn)
    {
        try { return checked(a - b); }
        catch (OverflowException) { return SizeEscape(fn); }
    }

    /// <summary>The product of two unscaled operands at the exact carrier's boundary — the ×10 / ×5 that makes
    /// MEDIAN's and MIDRANGE's halving exact (see <see cref="SizeEscape"/>).</summary>
    private static Int128 ExactMul(Int128 a, Int128 b, string fn)
    {
        try { return checked(a * b); }
        catch (OverflowException) { return SizeEscape(fn); }
    }

    /// <summary>MEDIAN (§15.61.4): odd count ⇒ the middle of the sorted arguments (rule 1); even count ⇒ the mean
    /// of the two middles, <c>(b + c) / 2</c> (rule 2). Returned at scale common+1 — the ×10 makes the halving
    /// EXACT in both branches (odd: middle × 10; even: (b + c) × 5), so no rounding decision is buried here.
    /// The scale bump costs a decimal digit of carrier headroom, which is why both branches go through
    /// <see cref="ExactMul"/> rather than multiplying raw (fix-queue PB32).</summary>
    public static Int128 MedianScaled(params Int128[] xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        var sorted = (Int128[])xs.Clone();
        Array.Sort(sorted);
        int mid = sorted.Length / 2;
        return sorted.Length % 2 != 0
            ? ExactMul(sorted[mid], 10, "MEDIAN")
            : ExactMul(ExactAdd(sorted[mid - 1], sorted[mid], "MEDIAN"), 5, "MEDIAN");
    }

    /// <summary>MIDRANGE (§15.62.4): <c>(MAX + MIN) / 2</c> — returned at scale common+1 ((max+min) × 5, exact),
    /// through <see cref="ExactMul"/> for the headroom reason documented there (fix-queue PB32).</summary>
    public static Int128 MidrangeScaled(params Int128[] xs)
    {
        RequireArguments(xs.Length, "MIDRANGE");
        return ExactMul(ExactAdd(MaxScaled(xs), MinScaled(xs), "MIDRANGE"), 5, "MIDRANGE");
    }

    /// <summary>ORD-MAX (§15.71.4): the 1-based ordinal position of the greatest argument; ties take the FIRST
    /// occurrence (strictly-greater update — the legacy-proven rule the NIST goldens encode).</summary>
    public static long OrdMax(params Int128[] xs)
    {
        RequireArguments(xs.Length, "ORD-MAX");
        Int128 m = xs[0];
        long idx = 1;
        for (int i = 1; i < xs.Length; i++) if (xs[i] > m) { m = xs[i]; idx = i + 1; }
        return idx;
    }

    /// <summary>ORD-MIN (§15.72.4): the 1-based ordinal position of the least argument; ties take the FIRST.</summary>
    public static long OrdMin(params Int128[] xs)
    {
        RequireArguments(xs.Length, "ORD-MIN");
        Int128 m = xs[0];
        long idx = 1;
        for (int i = 1; i < xs.Length; i++) if (xs[i] < m) { m = xs[i]; idx = i + 1; }
        return idx;
    }

    // ── MAX/MIN/ORD-MAX/ORD-MIN over all-alphanumeric/national arguments (category-polymorphic; §15.59/§15.63/§15.71/§15.72) ──
    // §15.59.4 r1 etc.: the greatest/least is determined by §8.8.4.2 simple-condition rules — the alphanumeric PROGRAM
    // COLLATING SEQUENCE (§8.8.4.2.7) or national PCS (§8.8.4.2.9), WITH §8.8.4.2.7 r2 space-padding of unequal-length
    // operands. All realized by CobolString.Compare, NEVER string.CompareOrdinal (which ignores the PCS and does not
    // space-pad). Three collation variants funnel through ONE selection helper (singular pattern): the native sequence
    // (the parameterless pad-compare body), a non-identity alphanumeric PCS (weights table — the emitter passes
    // __COLLATE first), and a non-native national PCS (the emitter passes __COLLATE_NAT first). (CA23.)

    /// <summary>The 0-based index of the extreme (greatest if <paramref name="max"/>, else least) under
    /// <paramref name="cmp"/>; first-wins on a tie (strict &gt;/&lt;). The one selection algorithm the MAX/MIN/
    /// ORD-MAX/ORD-MIN families and their three collation variants all share.</summary>
    private static int ExtremeIndex(string[] xs, bool max, Func<string, string, int> cmp)
    {
        RequireArguments(xs.Length, max ? "MAX / ORD-MAX" : "MIN / ORD-MIN");
        int k = 0;
        for (int i = 1; i < xs.Length; i++) { int c = cmp(xs[i], xs[k]); if (max ? c > 0 : c < 0) k = i; }
        return k;
    }

    /// <summary>MAX (§15.59) — the greatest argument per the effective collating sequence; the value IS the selected string.</summary>
    public static string MaxString(params string[] xs) => xs[ExtremeIndex(xs, true, static (a, b) => CobolString.Compare(a, b))];
    public static string MaxString(CobolCollation collation, params string[] xs) => xs[ExtremeIndex(xs, true, collation.Compare)];

    /// <summary>MIN (§15.63) — the least argument per the effective collating sequence.</summary>
    public static string MinString(params string[] xs) => xs[ExtremeIndex(xs, false, static (a, b) => CobolString.Compare(a, b))];
    public static string MinString(CobolCollation collation, params string[] xs) => xs[ExtremeIndex(xs, false, collation.Compare)];

    /// <summary>ORD-MAX (§15.71) — 1-based position of the greatest; tie = first.</summary>
    public static long OrdMaxString(params string[] xs) => ExtremeIndex(xs, true, static (a, b) => CobolString.Compare(a, b)) + 1;
    public static long OrdMaxString(CobolCollation collation, params string[] xs) => ExtremeIndex(xs, true, collation.Compare) + 1;

    /// <summary>ORD-MIN (§15.72) — 1-based position of the least; tie = first.</summary>
    public static long OrdMinString(params string[] xs) => ExtremeIndex(xs, false, static (a, b) => CobolString.Compare(a, b)) + 1;
    public static long OrdMinString(CobolCollation collation, params string[] xs) => ExtremeIndex(xs, false, collation.Compare) + 1;

    // ── NUMVAL / NUMVAL-C (ISO §15.67 / §15.68) ───────────────────────────────────────────────────────────────

    /// <summary>
    /// NUMVAL (§15.67): parse the human-formatted numeric string to its value, returned as the unscaled long at
    /// the compile-time <paramref name="scale"/> the emitter requested (≥ 6 — the hazard-H1 floor; parsing to
    /// (unscaled, actual-scale) then rescaling is hazard H2's discipline). The §15.67.3 formats: optional leading
    /// sign OR trailing sign/CR/DB (case-insensitive), spaces ignored leading/trailing and before the first digit
    /// (rule 2), one decimal separator — '.' normally, ',' under DECIMAL-POINT IS COMMA
    /// (<paramref name="commaMode"/>, rule 5). Malformed content → the EC-ARGUMENT-FUNCTION default 0 (§15.3).
    /// </summary>
    /// <summary>
    /// ⛔ THE NUMVAL-FAMILY DIGIT CAP — ONE RAISE SITE FOR THE VALUE-PRODUCING HALF (fix-queue PB33 + PB34).
    /// </summary>
    /// <remarks>
    /// §15.67.3 r3/r4 (NUMVAL), §15.68.3 r6/r7 (NUMVAL-C) and §15.69.3 r2/r3 (NUMVAL-F) all cap argument-1 at
    /// <b>31 digits under native arithmetic, 34 under standard-decimal</b> (35 standard-binary, unreachable —
    /// that mode is loudly refused). ⚠ NUMVAL-F counts the digits of the <b>significand</b>, not of the whole
    /// literal, which is why the check sits on each body's own count rather than on a shared scan.
    /// <para><b>THE THREE VALIDATORS ENFORCED IT AND THE THREE VALUE PRODUCERS DID NOT</b> — the same
    /// validating-twin-fixed asymmetry as PB32's MOD, three times over. MEASURED at <c>--std 2023</c> under
    /// <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING ON</c>, with a 34-digit argument:
    /// <c>TEST-NUMVAL</c> and <c>TEST-NUMVAL-C</c> both correctly reported position 32, while <c>NUMVAL</c>,
    /// <c>NUMVAL-C</c> and <c>NUMVAL-F</c> each returned an <c>Int128.MaxValue</c> SATURATION ARTIFACT
    /// (0141183460469231731687303715884) and <b>execution continued past all three</b> — a fatal exception
    /// condition never set, and a plausible-looking 31-digit number returned instead.</para>
    /// </remarks>
    internal static Int128 DigitCapExceeded(string fn, int digits, int cap, string clause) =>
        Exceptions.ExceptionState.ArgumentError(
            $"{fn}: argument-1 has more than {cap} digits ({digits} so far), which the arithmetic mode in "
            + $"effect does not permit ({clause})");

    /// <summary>The §15.67 reject projection of a non-conforming scan — ONE message per family, shared by the
    /// native (Int128) and standard-decimal (<see cref="NumvalDec"/>) value projections so the two arithmetic
    /// modes cannot drift apart on what they say about the same malformed argument. Returns the §15.3
    /// implementor-defined default (0) with checking off.</summary>
    private static Int128 NumvalReject(NvParse p, string text, int digitCap) =>
        p.CapHit
            ? DigitCapExceeded("NUMVAL", digitCap + 1, digitCap, "ISO §15.67.3 rules 3-4")
            : Exceptions.ExceptionState.ArgumentError(
                $"NUMVAL argument-1 \"{text}\" does not conform to the §15.67.3 formats (first character "
                + $"in error at position {p.ErrPos}; §15.93 TEST-NUMVAL reports the same position)");

    // ── The SDIDI projections of the same scans (fix-queue PB60, RV-15.67.4-1a; kb/Work PB251) ────────────────
    // §15.4.1: under a standard arithmetic mode "the returned value for numeric and integer functions is
    // contained in a temporary standard data item in the intermediate form defined for the arithmetic mode in
    // effect" — the SDIDI (§8.8.1.5.2, 34 digits). §15.67.4 r1 / §15.68.4 r1 fix that value with no latitude
    // ("the numeric value represented by argument-1"), and §15.69.4 r3 says it outright for NUMVAL-F ("If
    // standard-decimal arithmetic is in effect, the returned value is the numeric value represented by
    // argument-1" — where r2 grants native arithmetic only an approximation). So the standard-mode value is the
    // scan's (sign, unscaled, frac[, exp]) lifted to CobolDec EXACTLY at the parsed scale — no working scale, no
    // receiver, no ≥6/≥9 floor. Before these landed the standard-mode value rode the NATIVE Int128 projection at
    // the item-92 working scale: a receiver-less DISPLAY of NUMVAL("1.2345678") printed 1.234567, and a 34-digit
    // argument (legal under the r4 cap) rescaled past the Int128 carrier and rendered a SATURATION artifact
    // (170141183460469231731687303715884.105727) as if it were the value. The digit cap keeps the significand
    // ≤34 digits, which Int128 carries exactly, so the lift itself never rounds; NUMVAL-F's E-exponent can reach
    // past decimal128's range and passes through the ONE §8.8.1.5.2 r2 range check (CobolDec.FromParsed).
    //
    // ⛔ NUMVAL AND NUMVAL-C REACH HERE UNDER **NATIVE** ARITHMETIC TOO (kb/Work PB251). §15.4.1's closing
    // sentence withdraws the implementor latitude "unless otherwise specified in the function definition", and
    // §15.67.4 r1 / §15.68.4 r1 specify it — with no arithmetic-mode qualification — so their native value is the
    // parsed value exactly, and only the REPRESENTATION is the implementor's. NUMVAL-F is the one that does NOT:
    // §15.69.4 r2 grants native and standard-binary "an approximation", which is why r3 has to state the
    // standard-decimal value separately and why NumvalFDec is a standard-mode-only body.
    //
    // ⛔ ONE digitCap CONVENTION FOR THE WHOLE FAMILY: the omitted default is the NATIVE cap 31 (§15.67.3 r3), and
    // the emitter NAMES the cap exactly when the mode's cap differs (IntrinsicRenderer.DigitCapFlag, keyed on
    // ArithmeticModes.NumvalDigitCap against its DefaultDigitCap = 31). These four bodies defaulted to 34 while
    // every Int128 body and every TEST- validator defaulted to 31 — harmless only while they were standard-only,
    // and a silent 34-digit native admission the moment PB251 routed native NUMVAL here.

    /// <summary>NUMVAL's §15.67.4 r1 value as an SDIDI, exact at the parsed scale — the projection EVERY
    /// arithmetic mode uses (see the family comment above). Same <see cref="NvScan"/>, same reject projection as
    /// the TEST- validator, so the value can never accept what the validator rejects.</summary>
    public static CobolDec NumvalDec(string text, bool commaMode = false, int digitCap = 31)
    {
        NvParse p = NvScan(text, commaMode, "", false, digitCap, allowGroup: false);
        if (p.ErrPos != 0) return CobolDec.From(NumvalReject(p, text, digitCap), 0);
        return CobolDec.From(p.Neg ? -p.Unscaled : p.Unscaled, p.Frac);
    }

    /// <summary>NUMVAL-C's §15.68.4 r1 value as an SDIDI, exact at the parsed scale — the projection EVERY
    /// arithmetic mode uses; the currency and grouping rules are the one scan's.</summary>
    public static CobolDec NumvalCDec(string text, string currency, bool commaMode = false, bool anycase = false,
                                      int digitCap = 31)
    {
        if (InvalidCurrency(currency)) return CobolDec.From(NumvalCInvalidCurrency(currency), 0);
        NvParse p = NvScan(text, commaMode, currency, anycase, digitCap, allowGroup: true);
        if (p.ErrPos != 0) return CobolDec.From(NumvalCReject(p, text, digitCap), 0);
        return CobolDec.From(p.Neg ? -p.Unscaled : p.Unscaled, p.Frac);
    }

    /// <summary>NUMVAL-F under STANDARD-DECIMAL arithmetic — §15.69.4 r3's exact value as an SDIDI:
    /// significand × 10^(exponent − fraction digits), through the §8.8.1.5.2 r2 range check (a 4-digit
    /// E-exponent can exceed decimal128's ±6144 adjusted exponent — EC-SIZE-OVERFLOW / UNDERFLOW, the same
    /// disposition every SDIDI operation result gets). <paramref name="mode"/> is the INTERMEDIATE ROUNDING
    /// mode (§11.9.11) the range check's subnormal re-round uses.</summary>
    public static CobolDec NumvalFDec(CobolRounding mode, string text, bool commaMode = false, int digitCap = 31)
    {
        NvfParse p = NvfScan(text, commaMode, digitCap);
        if (p.ErrPos != 0) return CobolDec.From(NumvalFReject(p, text, digitCap), 0);
        return CobolDec.FromParsed(p.Neg ? -p.Unscaled : p.Unscaled, p.Exp - p.Frac, mode);
    }

    /// <summary>Shift an exact unscaled value by <paramref name="shift"/> decimal places. Past the <c>Int128</c>
    /// carrier the LANDING decides (kb/Work PB77 — the two-form rule every carrier now follows, PB74's
    /// <c>CobolDec.ToUnscaledChecked</c>/<c>ToUnscaled</c> being the first): a CHECKED landing (ON SIZE ERROR /
    /// EC-SIZE checking) SATURATES instead of wrapping — safe for the same reason <c>ReceiverContext.WorkingScale</c>
    /// makes the float quantizer's safe: the emitter caps the working scale at the receiver's headroom, so a
    /// saturated value still exceeds the receiver's capacity after the store's rescale and RAISES the size error
    /// (§14.7.5 case 5) rather than storing silently; an UNCHECKED landing (a MOVE sender — §14.6.8.2 r4; the
    /// no-phrase store — §14.6.13.1.3 item 8) keeps the LOW-ORDER digits through <c>CobolNum.RescaleStoreCap</c>
    /// (the digits a ≤38-digit store could never use are dropped BEFORE the multiply), because a truncating store
    /// has no check to expose a sentinel and truncating one stored garbage (<c>MOVE FUNCTION NUMVAL-F("5E+30") TO
    /// PIC 9(5)</c> stored 03715, the low digits of <c>Int128.MaxValue</c>; §14.6.8.2 r4 says 00000).
    /// ⚠ <c>Pow10.AsWide</c> itself WRAPS past 10³⁸ (its fallback loop is unchecked), so the exponent is bounded
    /// BEFORE the call — without that guard a large <c>E±nn</c> would multiply by a wrapped power and produce a
    /// plausible wrong value rather than a saturated one.</summary>
    private static Int128 Rescaled(Int128 unscaled, int shift, bool checkedLanding)
    {
        if (unscaled == 0) return Int128.Zero;
        if (shift == 0) return unscaled;
        if (shift < 0) return -shift > 38 ? Int128.Zero : unscaled / Pow10.AsWide(-shift);
        if (!checkedLanding) return CobolNum.RescaleStoreCap(unscaled, 0, shift, CobolRounding.Truncation);
        if (shift > 38) return unscaled > 0 ? Int128.MaxValue : Int128.MinValue;
        Int128 limit = Int128.MaxValue / Pow10.AsWide(shift);
        if (Int128.Abs(unscaled) > limit) return unscaled > 0 ? Int128.MaxValue : Int128.MinValue;
        return unscaled * Pow10.AsWide(shift);
    }

    /// <summary>NUMVAL-F (§15.69, COBOL-2014+): the floating NUMVAL — a signed mantissa (with an optional decimal
    /// point) and an optional <c>E±exponent</c> (1..4 exponent digits). Parsed exactly to (unscaled, effective
    /// scale) then rescaled to the emitter's working <paramref name="scale"/> (native arithmetic ⇒ the §15.69.4 r2
    /// approximation license). Malformed content → EC-ARGUMENT-FUNCTION and the §15.3 default 0. Space placement
    /// follows §15.69.3 r5 exactly — the value path and TEST-NUMVAL-F share ONE scan (PB60).</summary>
    /// <param name="digitCap">§15.69.3 r2/r3. ⚠ NUMVAL-F caps the digits of the SIGNIFICAND, not of the whole
    /// literal — the exponent's own 1..4 digits do not count toward it (fix-queue PB34).</param>
    /// <remarks>⛔ ONE SCAN, TWO PROJECTIONS (fix-queue PB60): the value is a projection of the SAME positional
    /// <see cref="NvfScan"/> that answers §15.95 TEST-NUMVAL-F. The old body opened with
    /// <c>text.Replace(" ", "")</c> — deleting exactly the spaces §15.69.3 r5's except-clause makes ILLEGAL
    /// ("Embedded spaces … are ignored EXCEPT between the first numeric digit and the last digit that precedes
    /// a letter 'E'"), so <c>NUMVAL-F("1 2")</c> valued 12 while TEST-NUMVAL-F correctly reported the error —
    /// the two halves of one rule disagreeing. §15.69.4 r2's approximation license governs only the RESCALE of
    /// a CONFORMING argument, never admission. Int128 + the saturating shared shift per <see cref="Rescaled"/>
    /// (PB13's sweep — the old long clamp returned 9223372036 for NUMVAL-F("1E+20")).</remarks>
    /// <param name="checkedLanding">The landing's form past the carrier — see <see cref="Rescaled"/> (kb/Work PB77).</param>
    public static Int128 NumvalF(string text, int scale, bool commaMode = false, int digitCap = 31, bool checkedLanding = false)
    {
        NvfParse p = NvfScan(text, commaMode, digitCap);
        if (p.ErrPos != 0) return NumvalFReject(p, text, digitCap);
        Int128 r = Rescaled(p.Unscaled, scale + p.Exp - p.Frac, checkedLanding);
        return p.Neg ? -r : r;
    }

    /// <summary>NUMVAL-F's value converted ONCE into a BINARY32 receiver (ISO §14.6.8.3 rule 2: "in a manner
    /// consistent with the specifications of ISO/IEC 60559:2020" — one correctly rounded conversion; kb/Work
    /// PB1110): <see cref="CobolFloat.ScaledToSingle"/> of the scan's exact (significand, scale). Narrowing
    /// <see cref="NumvalFDouble"/> would round twice — a text a hair above a binary32 midpoint lands ON that
    /// midpoint in binary64 and the cast then ties to even. The result is returned as the binary64 that holds the
    /// binary32 EXACTLY, so the caller's <c>(float)</c> cast is exact (the <see cref="FloatResultant"/>
    /// convention). Same scan and reject projection as <see cref="NumvalFDouble"/>.</summary>
    public static double NumvalFSingle(string text, bool commaMode = false, int digitCap = 31)
    {
        NvfParse p = NvfScan(text, commaMode, digitCap);
        if (p.ErrPos != 0) return (double)NumvalFReject(p, text, digitCap);
        return CobolFloat.ScaledToSingle(p.Neg ? -p.Unscaled : p.Unscaled, p.Frac - p.Exp);
    }

    /// <summary>NUMVAL-F under NATIVE arithmetic in a receiver-less or float-receiver context — the §15.69.4 r2
    /// approximation ("the returned value is an approximation of the numeric value represented by argument-1")
    /// carried as binary64, exactly the FLOAT family's documented determination (CONFORMANCE.md item 92;
    /// <c>IntrinsicRenderer.RenderFloat</c>'s Receiverless/Real arm, PB13): the returned value IS a binary64 and
    /// the Int128 quantization exists only to land it in a FIXED-POINT arithmetic receiver, whose scale defines
    /// it. ⛔ THIS ARM WAS NEVER SWEPT TO NUMVAL-F (fix-queue PB60 / RV-15.69.4-2): the receiver-less channels
    /// rode the Int128 projection at the ws-9 floor, so <c>DISPLAY FUNCTION NUMVAL-F("5E+30")</c> printed the
    /// saturation sentinel 170141183460469231731687303715.884105727, <c>NUMVAL-F("5E+30") = NUMVAL-F("9E+30")</c>
    /// was TRUE, a COMP-2 receiver got 1.7E+29, and <c>NUMVAL-F("1.5E-12")</c> was 0 in every one of them.
    /// The conversion is ONE correctly-rounded <c>double.Parse</c> of the scan's canonical
    /// <c>[-]digits E exp</c> — never a multiply chain of two roundings; a magnitude past binary64 is ±Infinity,
    /// the same disposition the float family's EXP(1000) has. Same scan, same reject projection as the twins.</summary>
    public static double NumvalFDouble(string text, bool commaMode = false, int digitCap = 31)
    {
        NvfParse p = NvfScan(text, commaMode, digitCap);
        if (p.ErrPos != 0) return (double)NumvalFReject(p, text, digitCap);
        if (p.Unscaled == 0) return 0d;
        return double.Parse(
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{(p.Neg ? "-" : "")}{p.Unscaled}E{p.Exp - p.Frac}"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The §15.69 reject projection — shared by <see cref="NumvalF"/>, <see cref="NumvalFDouble"/> and
    /// <see cref="NumvalFDec"/> (see <see cref="NumvalReject"/>).</summary>
    private static Int128 NumvalFReject(NvfParse p, string text, int digitCap) =>
        p.CapHit
            ? DigitCapExceeded("NUMVAL-F", digitCap + 1, digitCap, "ISO §15.69.3 rules 2-3")
            : Exceptions.ExceptionState.ArgumentError(
                $"NUMVAL-F argument-1 \"{text}\" does not conform to the §15.69.3 format (first character "
                + $"in error at position {p.ErrPos}; §15.95 TEST-NUMVAL-F reports the same position)");

    /// <summary>TEST-NUMVAL-F (§15.95, COBOL-2014+): 0 if the string conforms to the NUMVAL-F format; else the
    /// 1-based position of the first character in error (an embedded space inside the significand ⇒ the first
    /// non-space after it, r b.1; a significand longer than the cap ⇒ the cap+1-th significand digit, r b.2;
    /// a standard-decimal magnitude past the SDIDI's ±6144 range ⇒ the exponent's first digit, r b.6 —
    /// kb/Work PB121); else (all spaces, empty, or a valid-but-incomplete string like <c>" +."</c>) LENGTH+1
    /// (r c). A projection of the ONE <see cref="NvfScan"/> the NUMVAL-F value path also rides (PB60).</summary>
    /// <param name="digitCap">The §15.95.4 r1b sub-note cap — 31 native, 34 standard-decimal; the emitter's
    /// <c>DigitCapFlag</c>, exactly as TEST-NUMVAL/TEST-NUMVAL-C already receive it (the hardcoded 31 this
    /// replaces was the PB60 probe fleet's sibling find).</param>
    public static long TestNumvalF(string text, bool commaMode = false, int digitCap = 31)
    {
        // §15.95.4 r1b.6 (kb/Work PB121): a conforming scan can still carry the standard-decimal capacity
        // flag — TEST-NUMVAL-F reports it; the value twins do not (their own range check disposes).
        NvfParse p = NvfScan(text, commaMode, digitCap);
        return p.ErrPos != 0 ? p.ErrPos : p.CapPos;
    }

    /// <summary>The §15.68.3 r2 runtime reject of an invalid argument-2 — shared by <see cref="NumvalC"/> and
    /// <see cref="NumvalCDec"/> (TEST-NUMVAL-C carries its own r1c LENGTH+1 leg beside this raise).</summary>
    private static Int128 NumvalCInvalidCurrency(string currency) =>
        Exceptions.ExceptionState.ArgumentError(
            $"NUMVAL-C argument-2 \"{currency}\" shall contain at least one non-space character and none "
            + "of the digits 0-9, the characters '*' '+' '-' ',' '.', or the letter pair CR/DB in any "
            + "case (§15.68.3 rule 2)");

    /// <summary>The §15.68 reject projection — shared by <see cref="NumvalC"/> and <see cref="NumvalCDec"/>
    /// (see <see cref="NumvalReject"/>).</summary>
    private static Int128 NumvalCReject(NvParse p, string text, int digitCap) =>
        p.CapHit
            ? DigitCapExceeded("NUMVAL-C", digitCap + 1, digitCap, "ISO §15.68.3 rules 6-7")
            : Exceptions.ExceptionState.ArgumentError(
                $"NUMVAL-C argument-1 \"{text}\" does not conform to either §15.68.3 r4a format "
                + $"(first character in error at position {p.ErrPos}; §15.94 TEST-NUMVAL-C reports the same position)");

    /// <summary>§15.68.3 r2's content bans on the currency string (after the rule's own edge-space trim):
    /// empty/all-space, any digit 0-9, any of <c>* + - , .</c> (the characters are named outright — the
    /// comma/period ban does not flex with DECIMAL-POINT IS COMMA), or a CR/DB letter pair in any case.
    /// Shared by NUMVAL-C and TEST-NUMVAL-C (§15.94.3 r1 mirrors the argument rules).</summary>
    private static bool InvalidCurrency(string currency)
    {
        string cur = currency.Trim();
        return cur.Length == 0
            || cur.Any(c => char.IsAsciiDigit(c) || c is '*' or '+' or '-' or ',' or '.')
            || cur.Contains("CR", StringComparison.OrdinalIgnoreCase)
            || cur.Contains("DB", StringComparison.OrdinalIgnoreCase);
    }

    // ── The §15.93/§15.94 TEST validators — position-reporting scanners beside their value parsers ────────────

    /// <summary>TEST-NUMVAL (§15.93.4): 0 when argument-1 conforms to the §15.67.3 NUMVAL formats (r1a); else
    /// the 1-based ordinal position of the first character in error (r1b — an embedded space after the first
    /// digit reports the first NON-space character following it, sub-note 1: <c>"0 1"</c> → 3; the
    /// <paramref name="digitCap"/>+1-th digit reports its own position, sub-notes 2/4 — 31 native, 34
    /// standard-decimal and 2002's STANDARD [sub-note 3's standard-binary 35 is in
    /// <c>ArithmeticModes.NumvalDigitCap</c> and unreachable: the mode is declined at bind, kb/Work PB198]);
    /// else — no specific character in error: zero-length, only spaces, or valid-but-incomplete like
    /// <c>" +."</c> — LENGTH+1 (r1c). A pure projection of the ONE <see cref="NvScan"/> the NUMVAL value path also rides
    /// (PB60) — positions are ordinal in the ORIGINAL string, which is exactly why the scan never
    /// pre-normalizes.</summary>
    public static long TestNumval(string text, bool commaMode = false, int digitCap = 31) =>
        NvScan(text, commaMode, "", false, digitCap, allowGroup: false).ErrPos;

    /// <summary>TEST-NUMVAL-C (§15.94.4): the §15.93.4-shaped verdict over the §15.68.3 NUMVAL-C formats —
    /// <c>[sp] [sign] [sp] [currency] [sp] digits[,digits]… [dec [digits]] [sp]</c> (format A, sign BEFORE the
    /// currency) or <c>[sp] [currency] [sp] digits… [sp] [sign|CR|DB] [sp]</c> (format B, trailing sign). The
    /// currency (argument-2, or the binder-injected compilation-unit currency, §15.68.3 r3) matches character
    /// for character (r4a) — case-folded under ANYCASE (r4f); it appears at most once, BEFORE the digits (no
    /// trailing-currency or currency-then-sign form). Grouping separators are ARBITRARY-length digit groups
    /// (r4a's <c>digit [, digit]…</c> — no 3-digit constraint; <c>"1,23,4.5"</c> conforms) and are illegal
    /// after the decimal separator; DECIMAL-POINT IS COMMA SWAPS the two roles (r4d). Verdicts: 0 (r1a) /
    /// first-error position (r1b, same sub-notes as TEST-NUMVAL) / LENGTH+1 (r1c).</summary>
    public static long TestNumvalC(string text, string currency, bool commaMode = false, bool anycase = false,
        int digitCap = 31)
    {
        // §15.68.3 r2 via §15.94.3 r1 — an invalid runtime currency raises EC-ARGUMENT-FUNCTION; the
        // checking-off verdict is the r1c "no specific character in error" LENGTH+1 leg (no character OF
        // ARGUMENT-1 is in error — the argument-2 is — and 0 would falsely certify conformance).
        if (InvalidCurrency(currency))
        {
            Exceptions.ExceptionState.ArgumentError(
                $"TEST-NUMVAL-C argument-2 \"{currency}\" shall contain at least one non-space character and "
                + "none of the digits 0-9, the characters '*' '+' '-' ',' '.', or the letter pair CR/DB in "
                + "any case (§15.68.3 rule 2 via §15.94.3 rule 1)");
            return text.Length + 1;
        }
        return NvScan(text, commaMode, currency, anycase, digitCap, allowGroup: true).ErrPos;
    }

    // ── The ONE positional format scan per family (fix-queue PB60) ─────────────────────────────────────────────
    // §15.67.3 (NUMVAL) and §15.68.3 r4a (NUMVAL-C) share one grammar shape — [sp] [sign] [sp] [currency] [sp]
    // digit-groups [dec [digits]] [sp] [trailing sign|CR|DB] [sp] — differing only in whether a currency and
    // grouping separators are admitted. §15.69.3 (NUMVAL-F) is the E-form. Each scan VALIDATES positionally
    // (the §15.93/94/95 error-position contract, r1b/r1c) and ACCUMULATES the value in the same pass, so the
    // TEST- twins and the value functions are projections of ONE parse and can never disagree about what
    // conforms — the structural end of the remove-then-scan shape (Trim/Replace pre-normalization) whose
    // measured costs were a TAB-led argument valuing clean, "-12-" valuing positive, an embedded significand
    // space valuing clean, and a conforming "R123.45CR" valuing 0 because the currency Replace consumed the R
    // of CR. The digit cap is tested BEFORE accumulating, so the cap+1-th digit reports its own position
    // (§15.93.4 r1b sub-notes 2/4) and the accumulator can never saturate into a plausible value.

    /// <summary>One scan's result: <c>ErrPos</c> 0 = conforming (the TEST- verdict), else the 1-based first
    /// error position (LENGTH+1 for the no-specific-character legs); <c>CapHit</c> distinguishes the digit-cap
    /// error so value projections raise the dedicated §15.67.3 r3/r4-family message; the value fields are
    /// meaningful only when <c>ErrPos</c> is 0.</summary>
    private readonly record struct NvParse(long ErrPos, bool CapHit, bool Neg, Int128 Unscaled, int Frac);

    private static NvParse NvScan(string text, bool commaMode, string currency, bool anycase,
        int digitCap, bool allowGroup)
    {
        char dec = commaMode ? ',' : '.';
        char group = commaMode ? '.' : ',';
        string cur = currency.Trim();                                 // r2 — argument-2's edge spaces are ignored
        var cmp = anycase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int n = text.Length, i = 0, digits = 0, frac = -1;
        Int128 unscaled = 0;
        bool anyDigit = false, sawDot = false, leadSign = false, sawCur = false, neg = false;
        bool AtCurrency() => cur.Length > 0 && !sawCur
            && i + cur.Length <= n && text.AsSpan(i, cur.Length).Equals(cur, cmp);
        while (i < n && text[i] == ' ') i++;                          // leading space-string (§15.67.3 r2 — SPACE only)
        if (i < n && text[i] is '+' or '-') { leadSign = true; neg = text[i] == '-'; i++; }   // format-A sign, BEFORE the currency
        while (i < n && text[i] == ' ') i++;
        if (AtCurrency()) { sawCur = true; i += cur.Length; }         // the at-most-once currency, AT ITS r4a POSITION
        while (i < n && text[i] == ' ') i++;                          // spaces after the currency / before the first digit
        for (; i < n; i++)                                            // digit groups + one decimal separator
        {
            char c = text[i];
            if (char.IsAsciiDigit(c))
            {
                anyDigit = true;
                if (++digits > digitCap) return new(i + 1, true, false, 0, 0);   // r1b sub-notes 2/4
                unscaled = unscaled * 10 + (c - '0');
                if (frac >= 0) frac++;
                continue;
            }
            if (allowGroup && c == group && !sawDot && anyDigit) continue;   // grouping only BEFORE the decimal (§15.68.4 r2)
            if (c == dec && !sawDot) { sawDot = true; frac = 0; continue; }
            break;
        }
        // No digit anywhere: a scan that BROKE on a real character reports that character (r1b — e.g. a
        // misplaced sign); a scan that ran off the end with only valid-but-incomplete content (" +.",
        // all-spaces, zero-length) is the r1c LENGTH+1 leg.
        if (!anyDigit) return new(i < n ? i + 1 : n + 1, false, false, 0, 0);
        while (i < n && text[i] == ' ') i++;                          // spaces before a trailing sign
        if (i < n)
        {
            char c = text[i];
            // Format-B trailing sign / CR / DB (any case, §15.67.3 r1) — only when no leading sign was taken
            // (the two formats are ALTERNATIVES, so a second sign is an ERROR POSITION, never a toggle).
            // §15.67.4 r2's "contains CR, DB, or the minus sign, the returned value is negative" holds by construction: the one
            // sign the format admits decides.
            if (!leadSign && c is '+' or '-') { neg = c == '-'; i++; }
            else if (!leadSign && i + 1 < n
                     && ((c is 'C' or 'c' && text[i + 1] is 'R' or 'r')
                         || (c is 'D' or 'd' && text[i + 1] is 'B' or 'b'))) { neg = true; i += 2; }
            else return new(i + 1, false, false, 0, 0);               // r1b — first char in error ("0 1" → 3)
        }
        while (i < n && text[i] == ' ') i++;                          // trailing space-string
        if (i != n) return new(i + 1, false, false, 0, 0);
        return new(0, false, neg, unscaled, frac < 0 ? 0 : frac);
    }

    /// <summary>The §15.69.3 E-form scan (see the family comment above): mantissa sign · significand with one
    /// decimal separator · optional <c>E{+|-}n(1..4)</c>. Spaces are legal leading, trailing, between sign and
    /// first digit, and around the exponent parts — and a space may not SPLIT either digit run, the significand's
    /// (r5's except-clause) or the exponent's <c>n</c> (the determination at the exponent loop below); §15.95.4
    /// r1 b) 1 reports the first non-space character following such a space in both.</summary>
    /// <param name="CapPos">§15.95.4 r1b sub-note 6 (kb/Work PB121): when the scan CONFORMS but the magnitude
    /// exceeds the standard intermediate data item's capacity for the mode the <c>digitCap</c> encodes
    /// (34 ⇒ standard-decimal, the SDIDI's ±6144 adjusted-exponent range, §8.8.1.5.2 NOTE 2), the 1-based
    /// position of the FIRST DIGIT of the exponent; else 0. A separate field, not an <c>ErrPos</c>, because the
    /// twins project it differently: TEST-NUMVAL-F reports it (r1b.6), while NUMVAL-F's ARGUMENT rules
    /// (§15.69.3) say nothing about magnitude — there the value path's own §8.8.1.5.2 range check disposes
    /// (EC-SIZE-OVERFLOW), so the scan must NOT reject. Native arithmetic (cap 31) has no r1b.6 leg — the rule
    /// names only the standard modes — and standard-binary (cap 35) is unreachable (COBOLNET0806 rejects
    /// ARITHMETIC IS STANDARD-BINARY outright; its SBIDI range joins this map if that ever lands).</param>
    private readonly record struct NvfParse(long ErrPos, bool CapHit, bool Neg, Int128 Unscaled, int Frac, int Exp,
        long CapPos = 0);

    private static NvfParse NvfScan(string text, bool commaMode, int digitCap)
    {
        char dec = commaMode ? ',' : '.';
        int n = text.Length, i = 0, sig = 0, frac = -1, exp = 0;
        Int128 unscaled = 0;
        bool anyDigit = false, sawDot = false, pendingSpace = false, neg = false;
        long Pos() => i + 1;
        while (i < n && text[i] == ' ') i++;                          // leading spaces (r5)
        if (i < n && (text[i] == '+' || text[i] == '-')) { neg = text[i] == '-'; i++; }
        while (i < n && text[i] == ' ') i++;                          // spaces before the first digit (r5)
        for (; i < n; i++)                                            // significand: { digit [ . [digit] ] | . digit }
        {
            char c = text[i];
            if (char.IsAsciiDigit(c))
            {
                // §15.95.4 r1 b) 1 — the first non-space character following a space embedded in a digit run.
                // The significand is where §15.69.3 r5's EXCEPT-clause puts it beyond argument; the exponent's
                // run answers the same way, by the determination at the exponent loop below.
                if (pendingSpace) return new(Pos(), false, false, 0, 0, 0);
                anyDigit = true;
                if (++sig > digitCap) return new(Pos(), true, false, 0, 0, 0);   // r b.2 — the cap+1-th significand digit
                unscaled = unscaled * 10 + (c - '0');
                if (frac >= 0) frac++;
                continue;
            }
            if (c == dec && !sawDot) { if (pendingSpace) return new(Pos(), false, false, 0, 0, 0); sawDot = true; frac = 0; continue; }
            if (c == ' ') { pendingSpace = true; continue; }          // trailing/interior space — decided by what follows
            break;                                                    // 'E' or a bad char — the exponent scan decides
        }
        // No digit anywhere: a scan that BROKE on a real character reports that character (r1b — "ABC" → 1,
        // "--1" → 2, "$1.5" → 1); only a scan that ran off the end with valid-but-incomplete content
        // (zero-length, all-spaces, " +.") is the r1c LENGTH+1 leg — the same discrimination NvScan makes
        // (kb/Work PB121: this arm returned n+1 unconditionally, misreporting leg b as leg c).
        if (!anyDigit) return new(i < n ? Pos() : n + 1, false, false, 0, 0, 0);
        pendingSpace = false;
        long expDig1 = 0;                                             // position of the exponent's first digit (r1b.6)
        if (i < n && (text[i] == 'E' || text[i] == 'e'))
        {
            i++;
            while (i < n && text[i] == ' ') i++;
            if (i >= n) return new(n + 1, false, false, 0, 0, 0);     // r c — dangling E
            bool eneg;
            if (text[i] == '+') { eneg = false; i++; }
            else if (text[i] == '-') { eneg = true; i++; }
            else return new(Pos(), false, false, 0, 0, 0);            // a sign is required after E (§15.69.3)
            while (i < n && text[i] == ' ') i++;
            int ed = 0, ev = 0;
            // ⛔ DETERMINATION — A SPACE DOES NOT SPLIT THE EXPONENT'S `n` (kb/Work PB256; the loop is
            // CONTIGUOUS by construction, and that is the recorded reading of a normative tension, not an
            // oversight). §15.69.3 r5 makes embedded spaces "ignored except between the first numeric digit and
            // the last digit that precedes a letter 'E'" — the significand — which read as "elide the space and
            // JOIN the runs on either side" would make "1E+1 2345" a FIVE-digit exponent, and §15.95.4 r1 b) 5
            // would then want the position of its fifth digit (9). It does not, on two grounds that agree:
            //   (a) §15.69.3 r1's figure (printed page 898, rendered — the transcription is faithful) draws `n`
            //       as ONE contiguous metavariable, "one, two, three, or four digits representing the exponent",
            //       with [ space-string ] only BEFORE and AFTER it. Nothing admits a space inside it.
            //   (b) §15.95.4 r1 b) 1 governs the case directly and is UNQUALIFIED as to which run: "if one or
            //       more spaces are embedded within a string of numeric characters, the returned value is the
            //       position of the first non-space character following the spaces" — where sub-notes 2, 3 and 4
            //       each say "of the significand" explicitly. The drafters distinguished inside one list, so
            //       sub-note 1 reaches the exponent's digit string too.
            // So "ignored" is about a space CONTRIBUTING NOTHING where the figure admits one, never about
            // joining digit runs: "1E+1 2345" → the space is the figure's trailing space-string, '2' is the
            // first character in error → 6; "1E+ 1234" and "1E+1234 " → 0. Sub-note 5 fires only on a
            // CONTIGUOUS fifth digit ("1E+12345" → 8). Witnessed by conformance:2023/pb256_test_numval_f_spaces
            // and recorded in docs/CONFORMANCE.md §3.
            while (i < n && char.IsAsciiDigit(text[i]))               // n = 1..4 exponent digits
            {
                if (++ed > 4) return new(Pos(), false, false, 0, 0, 0);   // §15.95.4 r1 b) 5 — the FIFTH digit
                if (ed == 1) expDig1 = Pos();
                ev = ev * 10 + (text[i] - '0');
                i++;
            }
            if (ed == 0) return new(n + 1, false, false, 0, 0, 0);    // r c — E± with no exponent digit
            exp = eneg ? -ev : ev;
        }
        while (i < n && text[i] == ' ') i++;                          // trailing spaces (r5)
        if (i != n) return new(Pos(), false, false, 0, 0, 0);         // any leftover char is in error
        // §15.95.4 r1b.6 (kb/Work PB121): under standard-decimal arithmetic (digitCap 34 — see CapPos) a
        // conforming argument whose magnitude exceeds the SDIDI's range flags the exponent's first digit.
        // The most-significant digit's exponent is (digits₁₀(unscaled) − 1) + exp − frac; with ≤34 significand
        // digits the value is representable exactly iff that is ≤ 6144 (§8.8.1.5.2 NOTE 2). Underflow is NOT
        // this leg — a small magnitude does not "exceed the capacity" (the value twin's subnormal re-round
        // disposes of it). Overflow implies a written exponent, so expDig1 is always set when this fires.
        long capPos = 0;
        if (digitCap == 34 && unscaled != 0)
        {
            int d10 = 0;
            for (Int128 t = unscaled; t > 0; t /= 10) d10++;
            if (d10 - 1 + exp - (frac < 0 ? 0 : frac) > 6144) capPos = expDig1;
        }
        return new(0, false, neg, unscaled, frac < 0 ? 0 : frac, exp, capPos);
    }
}
