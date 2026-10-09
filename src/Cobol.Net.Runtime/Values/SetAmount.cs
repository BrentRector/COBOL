// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>How a SET-family AMOUNT landed — the answer to §14.9.39.4's single recurring question, "does
/// arithmetic-expression-n evaluate to an integer", plus the one extra answer a typed carrier has to be able to
/// give. It names NO exception condition: the condition is the FORMAT's, never the amount's (kb/Work PB465).</summary>
public enum SetAmountLanding
{
    /// <summary>The amount evaluates to an integer and that integer is in <c>whole</c>.</summary>
    Integer,

    /// <summary>The amount does not evaluate to an integer — a fraction survived, or the value is a NaN or an
    /// infinity, neither of which is an integer. This is the antecedent of GR2 a) 1. a, GR3, GR19 and GR29;
    /// each format names its own exception condition for it.</summary>
    NotAnInteger,

    /// <summary>The amount IS an integer but is too large in magnitude for the widest integer carrier
    /// (<see cref="System.Int128"/>), so no exact integer value can be handed back. ⛔ This is NOT GR19's /
    /// GR3's case: the amount evaluates to an integer and those rules' antecedents are FALSE. It is the
    /// format's RANGE rule — §14.9.39.4 GR20 for a data pointer, GR2 a) 1. b / GR4 a) for an index, GR30 for a
    /// capacity — because no receiver in this implementation has a range that wide.</summary>
    BeyondCarrier,
}

/// <summary>
/// ⛔ THE ONE PLACE the SET-family integrality rule is written (ISO §14.9.39.4 GR2 a) 1. a · GR3 · GR19 · GR29).
///
/// <para><b>Why this type exists.</b> §14.9.39.4 states the SAME test four times, once per amount-taking format,
/// with the same three consequents (the condition is set to exist · the SET is unsuccessful · the receiving
/// operand is unchanged) and only the exception-condition NAME differing. kb/Work PB459 wrote the test once for
/// the three INDEX formats inside <see cref="CobolIndex"/>; the data-pointer format (GR19) kept its own copy in
/// <see cref="CobolPtr"/>, and that copy had a MAGNITUDE guard folded into it — so <c>SET P UP BY</c> an integral
/// 1.0E19 set EC-SIZE-ADDRESS, a condition whose antecedent is false, and aborted the run unit on legal COBOL
/// (kb/Work PB465). The test is therefore hoisted out of both: the integrality DECISION lives here and is
/// carrier-neutral, and each format maps the outcome to its own condition next to its own range rule.</para>
///
/// <para><b>It raises nothing.</b> A raise needs the format's exception-condition name, and naming one here
/// would put the format's rule back inside the shared test — the very shape PB465 is about. Both entry points
/// are pure functions of their argument.</para>
///
/// <para><b>Outside SET.</b> §14.9.41.4 GR14 asks the same integrality question of a START statement's WITH LENGTH
/// arithmetic-expression-1 ("does not evaluate to a positive nonzero integer …"), so <c>IO.StartKeyLength</c> lands
/// through here too and maps <see cref="SetAmountLanding.NotAnInteger"/> onto START's own '23' leg (kb/Work PB357).
/// </para>
/// </summary>
public static class SetAmount
{
    /// <summary>The first <see cref="double"/> magnitude <see cref="Int128"/> cannot hold: 2^127
    /// (<c>Int128.MaxValue</c> is 2^127 − 1 and the nearest double to it IS 2^127, while
    /// <c>Int128.MinValue</c> is exactly −2^127 and converts). A conversion outside this interval is UNDEFINED
    /// in C#, so the boundary is tested on the double BEFORE any cast — the shape kb/Work PB459 established.</summary>
    private static readonly double TwoPow127 = Math.ScaleB(1.0, 127);

    /// <summary>Land an amount that arrived as an EXACT scaled fixed-point value: <paramref name="scaled"/> is
    /// the value times 10^<paramref name="scale"/>, so the divisibility test IS the integrality test and no
    /// fraction has been lost on the way in.
    /// <para>⛔ A NEGATIVE <paramref name="scale"/> is a trailing-P amount (<c>PIC 9PP</c> stores 1 for the value
    /// 100), and its VALUE is <paramref name="scaled"/> × 10^−<paramref name="scale"/>: "the symbol 'P' specifies
    /// the location of an assumed decimal point when that point is not within the number that appears in the data
    /// item" (ISO §13.18.40.4 GR14). Handing back the stored digits moved <c>SET IX UP BY</c> such an item by 1,
    /// not 100, and fed the same wrong count to SET pointer UP BY, SET capacity and START … WITH LENGTH
    /// (kb/Work PB2697). That widening is the one way an exact amount can be an integer too large for the
    /// <see cref="Int128"/> carrier, so it is the one way this lane answers
    /// <see cref="SetAmountLanding.BeyondCarrier"/> — decided before the multiply by the shared
    /// <see cref="CobolNum.WideningFits"/>, so the product can never wrap.</para></summary>
    public static SetAmountLanding Land(Int128 scaled, int scale, out Int128 whole)
    {
        if (scale < 0)
        {
            whole = Int128.Zero;
            if (scaled == 0) return SetAmountLanding.Integer;   // 0 at every scale, even one past Pow10's table
            if (!CobolNum.WideningFits(scaled, -scale)) return SetAmountLanding.BeyondCarrier;
            whole = scaled * Pow10.AsWide(-scale);
            return SetAmountLanding.Integer;
        }
        if (scale == 0) { whole = scaled; return SetAmountLanding.Integer; }
        Int128 pow = Pow10.AsWide(scale);
        if (scaled % pow != 0) { whole = Int128.Zero; return SetAmountLanding.NotAnInteger; }
        whole = scaled / pow;
        return SetAmountLanding.Integer;
    }

    /// <summary>The NATIVE-FLOAT lane: the integrality test runs on the <c>double</c> ITSELF. An emitter-side
    /// <c>(long)(double)</c> narrowing truncates the fraction the test is looking for and wraps the magnitude
    /// the range rule rejects, so the double travels intact to here (kb/Work PB151, PB459, PB465).</summary>
    public static SetAmountLanding Land(double amount, out Int128 whole)
    {
        whole = Int128.Zero;
        // A NaN and an infinity do not evaluate to an integer, so they take the integrality leg — the outcome
        // the four rules state ("unsuccessful … the receiving operand is unchanged"), never a conversion.
        if (!double.IsFinite(amount) || amount != Math.Truncate(amount)) return SetAmountLanding.NotAnInteger;
        if (amount < -TwoPow127 || amount >= TwoPow127) return SetAmountLanding.BeyondCarrier;
        whole = (Int128)amount;
        return SetAmountLanding.Integer;
    }

    // ⛔ NO LOCAL POWER-OF-TEN LOOP. `Pow10` (Values/Numeric/Pow10.cs) is THE one power-of-ten source for the
    // runtime, and its doc-comment lists the six identical multiply loops it replaced; CobolIndex carried a
    // SEVENTH that the sweep missed, and it came here with the extraction before this review caught it.
}
