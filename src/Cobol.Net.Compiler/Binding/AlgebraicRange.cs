// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>The extreme values ONE data description permits, rendered as decimal literal TEXT.</summary>
/// <param name="Farthest">The POSITIVE algebraic value of greatest finite magnitude the description permits —
/// ISO §15.43.4 r2 ("the positive algebraic value of greatest finite magnitude that may be represented in
/// argument-1") and §14.9.39.4 GR32 a) ("the value farthest away from zero permitted by the specifications of
/// identifier-14") name the same quantity.</param>
/// <param name="FarthestNegative">The NEGATIVE value of greatest finite magnitude, or <c>null</c> when the
/// description cannot represent a sign at all. ⚠ It is NOT always <c>-Farthest</c>: a two's-complement container
/// reaches one further down (§13.18.60.4 GR12 — <c>PIC S9(4) COMP-5</c> spans −32768..32767), which is exactly the
/// asymmetry §14.9.39.3 SR31 a) makes the SIGN phrase mandatory for.</param>
/// <param name="Nearest">The nonzero value NEAREST to zero the description permits — §15.83.4 r2's "smallest
/// algebraic value that may represent the difference between two values represented in argument-1" and
/// §14.9.39.4 GR36 a)'s "the nonzero value nearest to zero permitted by the specifications of identifier-14".
/// Always a POSITIVE magnitude; a caller wanting the negative one prefixes the sign. <c>null</c> for a
/// FLOATING-POINT numeric-edited picture, and deliberately so: no rule in the standard asks for it (§15.83.3 r1
/// admits category numeric ONLY, and §14.9.39.3 SR31 likewise), so deriving one here would be a lookup nothing
/// reads and therefore nothing has ever contradicted (feedback_a_dead_lookup_is_also_unverified). A future
/// caller derives it from §13.18.40.4 GR13's significand normalization, which is the part this would have to
/// guess at.</param>
/// <param name="Zero">Zero rendered AT THIS DESCRIPTION'S OWN SCALE — <c>"0.00"</c> for a <c>9V99</c> item, not
/// <c>"0"</c>. It rides here rather than being recomputed by callers because the scale that produces it is an
/// EDITED mask's fractional width, which is not <c>PicInfo.Scale</c>; §15.58.4's lowest value for an item that
/// cannot represent a sign IS this, and a literal at the wrong scale then feeds whatever arithmetic reads it.</param>
internal readonly record struct AlgebraicRange(string Farthest, string? FarthestNegative, string? Nearest, string Zero);

/// <summary>⛔ THE ONE EVALUATOR for "what are the extreme values this data description permits", because the
/// standard gives that one quantity TWO surfaces and says so itself:
/// <list type="bullet">
/// <item>the §15.43 / §15.58 / §15.83 HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC / SMALLEST-ALGEBRAIC intrinsics
/// (<c>IntrinsicBinder.BindAlgebraicFold</c>), and</item>
/// <item>the §14.9.39.2 Format 15 <c>SET CONTENT OF … TO FARTHEST-FROM-ZERO / NEAREST-TO-ZERO</c> statement
/// (<c>SetBinder.BindSetContent</c>).</item>
/// </list>
/// Annex D.32 states the equivalence in normative-adjacent words — "<c>SET CONTENT OF numeric-item TO
/// FARTHEST-FROM-ZERO</c> … That result is the same as that which might be obtained with the statement <c>MOVE
/// HIGHEST-ALGEBRAIC (numeric-item) TO numeric-item</c>" — so a second copy of this arithmetic is a rule written
/// down twice, and the two copies would disagree the first time a usage's capacity discipline changed
/// (feedback_one_rule_one_place).
/// <para>⚠ WHAT THIS DOES NOT DO. The two surfaces have DIFFERENT SCREENS and each keeps its own: §15.43.3 /
/// §15.58.3 / §15.83.3 r1's data-item/category rules and r2/r3's arithmetic-mode usage bars, plus the §15.x.4 r1
/// well-formedness test, belong to the intrinsics; §14.9.39.3 SR31/SR32 belong to the statement. This is the
/// VALUE only, and every caller screens before it asks. §15.43.1 is explicit that the two differ — "The content
/// of any numeric data item can be set exactly to the highest algebraic value permitted for it using the SET
/// statement, regardless of the mode of arithmetic that is in effect."</para></summary>
internal static class AlgebraicRanges
{
    /// <summary>The extremes of <paramref name="pic"/>. The caller has already established that the item is an
    /// admissible operand for its own rule; this reads the description's capacity discipline and nothing else.
    /// Returns <c>null</c> only for a description with no numeric capacity at all (a group, an index, a
    /// non-numeric category) — a shape every caller screens out first.</summary>
    /// <param name="decimalPointIsComma">The §12.3.7 SPECIAL-NAMES DECIMAL-POINT IS COMMA clause — read only to parse an
    /// EDITED item's own picture, never to render the result (the returned text is C#-facing, '.' radix always).</param>
    internal static AlgebraicRange? Of(PicInfo pic, bool decimalPointIsComma)
    {
        if (pic.Usage is Usage.Index) return null;                 // class index — not category numeric (§13.18.60)
        bool edited = pic.Category is PicCategory.NumericEdited;
        if (pic.Category is not PicCategory.Numeric && !edited) return null;

        // ── Standard / native floating-point USAGES (§13.18.60.4 GR13–GR18) ────────────────────────────────
        // The extremes are properties of the CARRIER this implementation maps the usage onto (PicInfo.ClrType),
        // which is what §15.43.4 r2's "represented in argument-1" and GR32 a)'s "permitted by the specifications
        // of identifier-14" both denote. Every float usage is signed, so FarthestNegative is always the mirror.
        if (pic.IsFloat)
        {
            var (max, min) = pic.Usage switch
            {
                Usage.Float or Usage.FloatShort or Usage.FloatBinary32
                    => ("3.4028235E+38", "1.401298464324817070923729583289916E-45"),
                Usage.FloatDecimal16 or Usage.FloatDecimal34
                    => ("79228162514264337593543950335", "1E-28"),
                // binary64 carrier: COMP-2 / FLOAT-LONG / FLOAT-EXTENDED / FLOAT-BINARY-64 / -128
                _ => ("1.7976931348623157E+308", "4.940656458412465441765687928682214E-324"),
            };
            return new AlgebraicRange(max, "-" + max, min, Zero: "0");
        }

        // ── A FLOATING-POINT numeric-edited picture (§13.18.40, D21/kb/Work PB66) ──────────────────────────
        // The extreme is the all-nines significand at the mask's maximum exponent; an UNSIGNED mask has no
        // negative extreme at all (the standard's own §15.43.4 NOTE table shows `$**,**9.99` reaching 0 downward).
        if (edited && pic.IsFloatEdited)
        {
            var fm = CobolNet.Runtime.CobolEdit.FloatMask.Parse(pic.EditMask!, decimalPointIsComma);
            int intDigits = fm.SigDigits - fm.SigScale;
            string nines = new('9', fm.SigDigits);
            string mantissa = fm.SigScale > 0 ? nines[..intDigits] + "." + nines[intDigits..] : nines;
            string far = $"{mantissa}E+{fm.MaxExp}";
            // A floating-point picture has no fixed scale, so its zero is the bare literal.
            return new AlgebraicRange(far, fm.SigSign == '\0' ? null : "-" + far, Nearest: null, Zero: "0");
        }

        int scale;
        BigInteger unscaled;
        bool signable;
        if (edited && pic.LocaleEdit is not null)
        {
            // A format-2 (LOCALE) picture (§13.18.40 format 2; kb/Work PB64 T6): capacity = the Z+9 digit
            // positions at the picture's scale; signable = a '+' in character-string-1 (§13.18.40.5 r13).
            scale = pic.Scale;
            unscaled = Pow10(pic.DigitPositions) - 1;
            signable = pic.Signed;
        }
        else if (edited)
        {
            // The EDITING rules travel with the mask: a FLOATING extended editing sign control symbol's
            // repetitions are digit positions (§13.18.40.5 rule 6), so the capacity is wrong without them
            // (kb/Work PB491).
            var (cap, frac) = CobolNet.Runtime.CobolEdit.MaskCapacity(pic.EditMask!, '$', decimalPointIsComma,
                pic.EditingRules as CobolNet.Runtime.CobolEdit.EditRule[]);
            scale = frac;
            unscaled = Pow10(cap) - 1;      // all-nines over the mask's digit positions (§13.18.40.4)
            signable = pic.EditMask!.IndexOf('+') >= 0 || pic.EditMask!.IndexOf('-') >= 0
                       || pic.EditMask!.Contains("CR") || pic.EditMask!.Contains("DB");
        }
        else if (pic.Truncation == CobolNet.Runtime.NumericTruncation.BinaryCapacity)
        {
            // ⛔ Keyed on the ONE capacity-discipline datum (PicInfo.Truncation), never on a usage list: the
            // item owns its container's FULL two's-complement range (§13.18.60.4 GR12), which is the shape whose
            // |min| ≠ |max| makes §14.9.39.3 SR31 a) demand the SIGN phrase. AlgebraicFoldContainerAgreementTests
            // pins this bound against the runtime capacity for every BinaryCapacity profile.
            scale = pic.Scale;
            int bits = 8 * pic.StorageWidth;
            BigInteger hi = pic.Signed ? (BigInteger.One << (bits - 1)) - 1 : (BigInteger.One << bits) - 1;
            BigInteger lo = pic.Signed ? -(BigInteger.One << (bits - 1)) : BigInteger.Zero;
            return new AlgebraicRange(
                Decimalize(hi, scale, negative: false),
                pic.Signed ? Decimalize(lo, scale, negative: true) : null,
                Decimalize(BigInteger.One, scale, negative: false),
                Decimalize(BigInteger.Zero, scale, negative: false));
        }
        else
        {
            scale = pic.Scale;
            unscaled = Pow10(pic.Digits) - 1;   // all-nines (the DigitCount discipline)
            signable = pic.Signed;
        }

        string farthest = Decimalize(unscaled, scale, negative: false);
        return new AlgebraicRange(
            farthest,
            signable ? Decimalize(unscaled, scale, negative: true) : null,
            // The smallest positive increment 10^(−scale) — independent of digit count, sign and container
            // (§15.83.4 r2; §14.9.39.4 GR36 a).
            Decimalize(BigInteger.One, scale, negative: false),
            Decimalize(BigInteger.Zero, scale, negative: false));
    }

    /// <summary>The extremes of a COUNTER REGISTER — LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER, category-numeric
    /// data items by §8.5.2.12 items 3–5 — or <c>null</c> for any other expression (kb/Work R26, PB1401). Each is
    /// "a temporary unsigned integer data item" (§8.4.3.14.4 GR1 / §8.4.3.15.4 GR1), so its nearest nonzero value
    /// is 1, its zero is 0 and it has no negative extreme; its capacity is:
    /// <list type="bullet">
    /// <item>LINAGE-COUNTER — "whose size is equal to the page size specified in the LINAGE clause" (§8.4.3.14.4
    /// GR1): a literal page size directly; a data-name page size is set at run time, so the capacity is the
    /// largest page size that item can hold, its all-nines. The data-name is read from its RESOLVED item
    /// (<see cref="LinageOperand.Item"/>, qualified per §8.4.2.2 — kb/Work PB489), never re-looked-up by its bare
    /// name.</item>
    /// <item>LINE-COUNTER / PAGE-COUNTER — the standard gives no size, and WiseOwl COBOL's documented shape is
    /// PIC 9(18), the all-nines of the runtime's long carrier (CONFORMANCE.md §3, "the counter registers'
    /// declared capacity").</item>
    /// </list>
    /// ⛔ THE ONE STATEMENT OF THE REGISTERS' CAPACITY: the §15.43/§15.58/§15.83 algebraic folds and the §8.8.4.4.4
    /// GR3 g)/l)/m) class conditions both ask it here, so a HIGHEST-ALGEBRAIC of the register and an
    /// <c>IS FARTHEST-FROM-ZERO</c> test of it cannot name two different values.</summary>
    internal static AlgebraicRange? OfCounterRegister(Bound.BoundExpr register) =>
        CounterCapacity(register) is { } capacity ? UnsignedCounter(capacity) : null;

    /// <summary>True for a COUNTER REGISTER read — LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER — which the binder
    /// carries as a computed operand (the register is runtime state, never storage) although the standard makes it a
    /// DATA ITEM: "a temporary unsigned integer data item of class and category numeric" (§8.4.3.14.4 GR1 /
    /// §8.4.3.15.4 GR1), referable "in any context where an integer data item may appear" (§8.4.3.15.3 SR1). Every
    /// consumer that asks "is this an integer data item" or "what is its character image" asks HERE (kb/Work PB1153,
    /// PB1199, PB1318), so a new register is one arm of <see cref="CounterCapacity"/>, not one per consumer.</summary>
    internal static bool IsCounterRegister(Bound.BoundExpr e) => CounterCapacity(e) is not null;

    /// <summary>The number of digit positions of a counter register's implicit description — the unsigned integer
    /// item <c>PIC 9(d)</c> whose all-nines is the declared capacity (docs/CONFORMANCE.md, "the counter registers'
    /// declared capacity"): the digits of the LINAGE page size, and 18 for LINE-/PAGE-COUNTER. It sizes the register's
    /// character image, exactly as an integer data item's PICTURE sizes its own (§14.9.25.4 GR6's numeric-integer
    /// sender; §8.8.4.2.5's "number of digits in the integer"). <c>null</c> for any other expression.</summary>
    internal static int? CounterRegisterDigits(Bound.BoundExpr e) => CounterCapacity(e)?.Length;

    /// <summary>⛔ THE ONE STATEMENT of a register's capacity, as its all-nines-or-literal text — both
    /// <see cref="OfCounterRegister"/> and <see cref="CounterRegisterDigits"/> read it, so the class conditions,
    /// the algebraic folds and the character image cannot disagree about one register.</summary>
    private static string? CounterCapacity(Bound.BoundExpr register) => register switch
    {
        Bound.BoundLinageCounterRef lc => LinagePageCapacity(lc.File),
        Bound.BoundReportCounterRef => ReportCounterCapacity,
        _ => null,
    };

    /// <summary>The documented LINE-COUNTER / PAGE-COUNTER capacity, PIC 9(18) (see <see cref="OfCounterRegister"/>).</summary>
    private const string ReportCounterCapacity = "999999999999999999";

    private static AlgebraicRange UnsignedCounter(string capacity) =>
        new(capacity, FarthestNegative: null, Nearest: "1", Zero: "0");

    private static string LinagePageCapacity(FileModel file)
    {
        var body = file.Linage?.Body;
        if (body?.Literal is { } lit) return lit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (body?.Item?.Pic is { Digits: > 0 } p) return new string('9', p.Digits);
        // An unresolved page-size operand was already reported by its own resolution (DataBinder.ResolveLinage);
        // the register is still a long-carried counter, so its carrier's bound stands in for the missing size.
        return ReportCounterCapacity;
    }

    /// <summary>The extremes of a numeric or integer FUNCTION's returned value (§8.5.2.12 items 6–7) — the
    /// "temporary elementary data item" §15.4 places it in — for a function whose value is NOT a binary64 (that
    /// family is asked on its own bits by <c>CobolFloatClass</c>, whose extremes are binary64's). §15.4.1 leaves the
    /// characteristics and representation of the returned value to the implementor under native arithmetic, and
    /// WiseOwl COBOL's determination (CONFORMANCE.md DOC-A.1-92) fixes it:
    /// <list type="bullet">
    /// <item><paramref name="standardDecimal"/> — a value carried as the SDIDI (NUMVAL / NUMVAL-C in every mode,
    /// every function under a standard arithmetic mode): the SDIDI's own extremes, the same text
    /// <see cref="ArithmeticModes.IntermediateExtremes"/> states for that form.</item>
    /// <item>otherwise the §15.4 temporary's description (<c>SendingValueTemp</c>): S9(30) for an INTEGER function
    /// (§15.2 item 5 — "no digits to the right of the decimal point", so its nearest nonzero value is 1), and
    /// S9(21)V9(9) for a NUMERIC function.</item>
    /// </list></summary>
    internal static AlgebraicRange OfFunctionReturnedValue(bool integer, bool standardDecimal, bool decimalPointIsComma)
    {
        if (standardDecimal)
        {
            var (far, near) = ArithmeticModes.IntermediateExtremes(ArithmeticMode.StandardDecimal);
            return new AlgebraicRange(far, "-" + far, near, Zero: "0");
        }
        return Of(integer ? Procedure.SendingValueTemp.IntegerFunctionValuePic : Procedure.SendingValueTemp.FunctionValuePic,
            decimalPointIsComma)!.Value;
    }

    private static BigInteger Pow10(int n) => BigInteger.Pow(10, Math.Max(0, n));

    /// <summary>Render an unscaled <see cref="BigInteger"/> at <paramref name="scale"/> fractional digits as a
    /// decimal literal string ('.' radix always — an internal C#-facing literal, never COBOL source, so
    /// DECIMAL-POINT IS COMMA does not apply). A negative scale (trailing P) appends |scale| zeros; a positive
    /// scale inserts the point.
    /// <para>⛔ DELEGATES to <see cref="CobolNet.Runtime.CobolNum.FormatFunctionText"/> — the SAME rule the
    /// RUNTIME uses to render a computed intrinsic's value as text (DA2). These are the two halves of one job:
    /// this one folds a constant-argument intrinsic (or a Format-15 SET) to a literal at COMPILE time, that one
    /// renders a runtime-computed result, and a COBOL programmer cannot tell which fired. A hand-written second
    /// copy is the two-mechanisms anti-pattern, and the copies HAD already drifted: this method early-returned
    /// <c>"0"</c> for a zero magnitude and so DROPPED the scale, making <c>LOWEST-ALGEBRAIC</c> of an unsigned
    /// scaled item fold to <c>"0"</c> where the runtime rule gives <c>"0.00"</c>.</para>
    /// <para>The BigInteger arm is LIVE, not defensive: the positive extreme of a 16-byte UNSIGNED container is
    /// 2^128−1 (39 digits, beyond <see cref="Int128"/>), and this arm renders it; <c>EmitCore.IntLiteralX</c>
    /// carries such a magnitude on the unsigned-wide lane (kb/Work R10, F73).</para></summary>
    internal static string Decimalize(BigInteger unscaled, int scale, bool negative)
    {
        var mag = BigInteger.Abs(unscaled);
        if (mag <= (BigInteger)Int128.MaxValue)
        {
            Int128 v = (Int128)mag;
            return CobolNet.Runtime.CobolNum.FormatFunctionText(negative ? -v : v, scale);
        }
        string s = mag.ToString();
        string sign = negative ? "-" : "";
        if (scale <= 0) return sign + s + new string('0', -scale);          // S9PP: 99 @ −2 → "9900"; 1 @ −2 → "100"
        if (s.Length <= scale) s = s.PadLeft(scale + 1, '0');               // 1 @ 3 → "0001" → "0.001"
        return sign + s[..^scale] + "." + s[^scale..];                      // 99999 @ 3 → "99.999"
    }

    /// <summary>Compare two decimal literal TEXTS by magnitude, exactly (no binary float anywhere in the path):
    /// −1 when |<paramref name="a"/>| &lt; |<paramref name="b"/>|, 0 when equal, +1 when greater. Both surfaces
    /// of §14.9.39.4 GR32 b / GR36 b need it — "whichever is closer to zero" / "whichever is farther from zero"
    /// — and the operands are an item's extreme and an ARITHMETIC MODE'S extreme, which reach 10^6144 and
    /// 10^−6176 (<see cref="ArithmeticModes.IntermediateExtremes"/>): no CLR floating type spans that, and
    /// <see cref="decimal"/> spans neither the range nor the 34-digit precision. Scaled BigInteger does.</summary>
    internal static int CompareMagnitude(string a, string b)
    {
        var (ma, ea) = ParseMagnitude(a);
        var (mb, eb) = ParseMagnitude(b);
        if (ma.IsZero || mb.IsZero) return ma.IsZero && mb.IsZero ? 0 : ma.IsZero ? -1 : 1;
        // Bring both to the same power of ten before comparing (exact — no rounding, no widening).
        int e = Math.Min(ea, eb);
        ma *= Pow10(ea - e);
        mb *= Pow10(eb - e);
        return ma.CompareTo(mb);
    }

    /// <summary>Decompose a decimal literal into (unsigned mantissa, power of ten) with value = mantissa·10^exp.
    /// Accepts the forms this file and <see cref="ArithmeticModes.IntermediateExtremes"/> produce: an optional
    /// sign (ignored — magnitude only), digits, an optional '.', and an optional 'E'±exponent.</summary>
    private static (BigInteger Mantissa, int Exp10) ParseMagnitude(string lit)
    {
        ReadOnlySpan<char> s = lit.AsSpan().Trim();
        if (s.Length > 0 && (s[0] == '+' || s[0] == '-')) s = s[1..];
        int e = s.IndexOfAny('E', 'e');
        int exp = 0;
        if (e >= 0)
        {
            exp = int.Parse(s[(e + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            s = s[..e];
        }
        int dot = s.IndexOf('.');
        if (dot >= 0)
        {
            exp -= s.Length - dot - 1;
            Span<char> joined = s.Length <= 128 ? stackalloc char[s.Length - 1] : new char[s.Length - 1];
            s[..dot].CopyTo(joined);
            s[(dot + 1)..].CopyTo(joined[dot..]);
            return (BigInteger.Parse(joined, provider: System.Globalization.CultureInfo.InvariantCulture), exp);
        }
        return (BigInteger.Parse(s, System.Globalization.CultureInfo.InvariantCulture), exp);
    }
}
