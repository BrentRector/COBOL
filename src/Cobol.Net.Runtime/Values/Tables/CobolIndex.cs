// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>Which SET-family INDEX rule an amount is being landed for. The integrality test is the SAME test in
/// all three, and the standard states it once per amount-taking format with a DIFFERENT exception-condition name
/// and a different extra obligation; this enum is the only thing that varies, so the test itself is written once
/// (<see cref="SetAmount.Land(System.Int128,int,out System.Int128)"/>) and each member below only maps its
/// outcome. §14.9.39.4's FOURTH amount-taking format, GR19's data pointer, has no member here because its
/// receiver is not an index: it maps the same landing to EC-SIZE-ADDRESS and GR20's EC-RANGE-PTR in
/// <see cref="CobolPtr.UpByAmount"/>.</summary>
public enum SetAmountRule
{
    /// <summary>ISO §14.9.39.4 GR2 a) 1. — Format 1, <c>SET index-name-1 TO arithmetic-expression-1</c>:
    /// a) non-integer ⇒ EC-BOUND-SUBSCRIPT; b) outside the §13.18.38.4 GR2 implementor index range ⇒
    /// EC-RANGE-INDEX. Both are "the execution of the SET statement is unsuccessful, and the content of the
    /// receiving operand is unchanged".</summary>
    IndexTo,

    /// <summary>ISO §14.9.39.4 GR3 — Format 2, <c>SET index-name-3 UP/DOWN BY arithmetic-expression-2</c>:
    /// a non-integer amount ⇒ EC-BOUND-SUBSCRIPT, unsuccessful, receiving operand unchanged. GR4 a)'s range
    /// obligation is on the RESULT, not the amount, and lives in <see cref="CobolIndex.Augment"/>.</summary>
    IndexBy,

    /// <summary>ISO §14.9.39.4 GR29 — Format 14, the dynamic-capacity amount: "If arithmetic-expression-4 does
    /// not evaluate to a NONNEGATIVE integer, the EC-BOUND-SUBSCRIPT exception condition is set to exist and the
    /// execution of the SET statement is unsuccessful." The sign test is GR29's own extra obligation; an amount
    /// past the 64-bit carrier is GR30's "exceeds the implementor's maximum capacity" ⇒
    /// EC-BOUND-TABLE-LIMIT.</summary>
    Capacity,
}

/// <summary>
/// THE ONE LANDING for every SET-family AMOUNT and for every modification of an index (ISO §14.9.39.4 GR2 a) 1.,
/// GR3, GR4 a), GR29; §13.18.38.4 GR2). kb/Work PB459.
///
/// <para><b>Why this type exists.</b> §14.9.39.4 states the SAME integrality guard once per amount-taking format
/// — GR2 a) 1. a (Format 1), GR3 (Format 2), GR19 (Format 10, the data pointer) and GR29 (Format 14) — each time
/// with the same three consequents (the condition is set to exist · the SET is unsuccessful · the receiving
/// operand is unchanged) and only the exception-condition NAME differing. Before PB459 exactly ONE of the four
/// was implemented (<see cref="CobolPtr"/>'s own copy, for GR19); the other three each rendered a bare
/// <c>(long)(Align(…, 0))</c> narrowing at the emitter, which TRUNCATES the fraction the test is supposed to see
/// and WRAPS a magnitude the carrier cannot hold. Writing the guard a fourth and fifth time would have repeated
/// the shape that produced the defect, so this type landed all three INDEX formats and named their conditions
/// through <see cref="SetAmountRule"/>. kb/Work PB465 then hoisted the integrality DECISION itself out to
/// <see cref="SetAmount"/>, because the fourth format's copy still lived in <see cref="CobolPtr"/> and had a
/// magnitude guard folded into it; what remains here is the INDEX half — this format's conditions and this
/// carrier's range.</para>
///
/// <para><b>The implementor index range.</b> §13.18.38.4 GR2 leaves "the rules for the range of values allowed in
/// the index defined by index-name-1" to the implementor, requiring only that the range cover occurrence numbers
/// (1 − integer-2) through (2 × integer-2). WiseOwl COBOL's index cell IS a C# <c>long</c> holding a 1-based
/// occurrence number (data-model D3), so the range is the full signed 64-bit interval
/// [−9 223 372 036 854 775 808, +9 223 372 036 854 775 807] — documented as DOC-A.1-128 in
/// <c>docs/CONFORMANCE.md</c> §7, which A.1 item 128 requires. A value OUTSIDE it is the EC-RANGE-INDEX case;
/// a value inside it but outside the TABLE is NOT — every one of these rules ends "even if that occurrence is
/// not a valid occurrence within this table", so an index pointing past its own table is legal and must not
/// raise. A table-bounds test here would reject conforming programs.</para>
///
/// <para><b>Checking off.</b> Every raise routes through an <c>ExceptionState</c> helper that returns when
/// checking for the condition is not enabled at this statement (§14.6.13.1.1). The callers here then still apply
/// the standard's own named outcome — unsuccessful, receiving operand unchanged — which is the owner's decided
/// rule for checking-off leniency: lenient WITH the outcome the standard states, never a fabricated one.</para>
/// </summary>
public static class CobolIndex
{
    /// <summary>The lowest occurrence number an index may hold (§13.18.38.4 GR2's implementor range; DOC-A.1-128).</summary>
    public const long MinIndex = long.MinValue;

    /// <summary>The highest occurrence number an index may hold (§13.18.38.4 GR2's implementor range; DOC-A.1-128).</summary>
    public const long MaxIndex = long.MaxValue;

    /// <summary>Land a SET-family amount that arrived as an EXACT scaled fixed-point value (the Int128 lane —
    /// <c>scaled</c> is the value times 10^<paramref name="scale"/>, so the fraction the integrality test needs
    /// is still present; a <c>(long)</c> narrowing at the emitter would have destroyed both the fraction and any
    /// magnitude past the carrier).
    /// <list type="bullet">
    /// <item>Not an integer ⇒ EC-BOUND-SUBSCRIPT (§14.9.39.4 GR2 a) 1. a / GR3 / GR29) and <c>false</c>.</item>
    /// <item><see cref="SetAmountRule.Capacity"/> and negative ⇒ EC-BOUND-SUBSCRIPT (GR29's "nonnegative")
    /// and <c>false</c>.</item>
    /// <item>Outside the implementor index range ⇒ EC-RANGE-INDEX (GR2 a) 1. b) — or, for a capacity amount,
    /// EC-BOUND-TABLE-LIMIT (GR30) — and <c>false</c>.</item>
    /// </list>
    /// <c>false</c> is "the execution of the SET statement is unsuccessful": the caller stores nothing, so every
    /// receiving operand is unchanged.</summary>
    public static bool TryAmount(Int128 scaled, int scale, SetAmountRule rule, string detail, out long value)
        => Land(SetAmount.Land(scaled, scale, out Int128 whole), whole, rule, detail, out value);

    /// <summary>The NATIVE-FLOAT lane of <see cref="TryAmount(System.Int128,int,SetAmountRule,string,out long)"/>
    /// — the integrality test runs on the <c>double</c> itself, exactly as <see cref="CobolPtr.UpByAmountReal"/>
    /// does for GR19 (kb/Work PB151: an emitter-side <c>(long)(double)</c> truncation bypasses the raise entirely).
    /// A NaN or an infinity is not an integer, so it takes the same unsuccessful leg.</summary>
    public static bool TryAmountReal(double v, SetAmountRule rule, string detail, out long value)
        => Land(SetAmount.Land(v, out Int128 whole), whole, rule, detail, out value);

    /// <summary>Map <see cref="SetAmount"/>'s carrier-neutral landing onto THIS format's exception conditions —
    /// the only part of the four rules that differs between them (kb/Work PB465). <c>false</c> is "the execution
    /// of the SET statement is unsuccessful", so the caller stores nothing and every receiving operand is
    /// unchanged.</summary>
    private static bool Land(SetAmountLanding landing, Int128 whole, SetAmountRule rule, string detail,
        out long value)
    {
        switch (landing)
        {
            case SetAmountLanding.NotAnInteger:
                ExceptionState.SubscriptError(NonIntegerDetail(rule, detail));
                value = 0;
                return false;
            case SetAmountLanding.BeyondCarrier:
                // An integral amount past Int128 is past every index range and every table capacity, so it takes
                // the same leg as a value merely outside the 64-bit index range — never the integrality leg.
                OutOfRange(rule, detail);
                value = 0;
                return false;
            default:
                return Accept(whole, rule, detail, out value);
        }
    }

    /// <summary>GR29's sign test and the implementor-range test, shared by both lanes.</summary>
    private static bool Accept(Int128 whole, SetAmountRule rule, string detail, out long value)
    {
        if (rule == SetAmountRule.Capacity && whole < 0)
        {
            // §14.9.39.4 GR29 — "does not evaluate to a NONNEGATIVE integer". GR30's minimum-capacity clamp
            // cannot stand in for this: the clamp is the rule for a legal new capacity below the OCCURS
            // minimum, and GR29 rejects the operand BEFORE any new capacity is computed.
            ExceptionState.SubscriptError(
                $"{detail} — a negative amount (ISO 14.9.39.4 GR29 requires a nonnegative integer)");
            value = 0;
            return false;
        }
        if (whole < MinIndex || whole > MaxIndex)
        {
            OutOfRange(rule, detail);
            value = 0;
            return false;
        }
        value = (long)whole;
        return true;
    }

    /// <summary>Modify an index by ±<paramref name="amount"/> occurrence numbers (ISO §14.9.39.4 GR4 — Format 2;
    /// §14.9.28.4 GR13 — PERFORM VARYING, which §13.18.38.4 GR2 names alongside SET as a statement that may
    /// modify an index). GR4 a): if the incremented/decremented occurrence number "is outside the limit
    /// specified in General rule 2 of 13.18.38" the EC-RANGE-INDEX exception condition is set to exist, the SET
    /// is unsuccessful, and the content of the receiving operand is unchanged — so this returns
    /// <paramref name="index"/> itself on that leg. GR4 b) is the ordinary result, "even if that occurrence is
    /// not a valid occurrence within this table", so no table bound is consulted.</summary>
    /// <remarks>The amount is <see cref="Int128"/>, not <see cref="long"/>: a PERFORM VARYING BY operand is an
    /// integer data item (§14.9.28.3 SR4 a) of up to 31 digits, and narrowing it at the emitter would wrap it
    /// before this guard could see it — the very shape kb/Work PB459 is about.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Augment(long index, Int128 amount, bool down, string detail)
    {
        // The sum is formed WIDER than the carrier so the 64-bit boundary is a value this code can see rather
        // than a wrap it cannot: `SET IDX UP BY 9223372036854775800` then `UP BY 100` used to leave IDX holding
        // a NEGATIVE occurrence number and the program carried on (kb/Work PB459's measured repro).
        Int128 r = down ? (Int128)index - amount : (Int128)index + amount;
        if (r < MinIndex || r > MaxIndex)
        {
            ExceptionState.RangeIndexError(
                $"{detail} — the resulting occurrence number is outside the implementor index range "
                + "(ISO 14.9.39.4 GR4 a) / 13.18.38.4 GR2)");
            return index;   // GR4 a) verbatim — the content of the receiving operand is unchanged
        }
        return (long)r;
    }

    private static void OutOfRange(SetAmountRule rule, string detail)
    {
        if (rule == SetAmountRule.Capacity)
            // §14.9.39.4 GR30 — "If the new capacity of the table exceeds the implementor's maximum capacity for
            // this dynamic-capacity table, the EC-BOUND-TABLE-LIMIT exception condition is set to exist and the
            // capacity of the table is unchanged." An amount past the 64-bit carrier is past every table's max.
            ExceptionState.BoundTableLimitError(
                $"{detail} — the requested capacity exceeds the implementor maximum (ISO 14.9.39.4 GR30)");
        else
            ExceptionState.RangeIndexError(
                $"{detail} — outside the implementor index range (ISO 14.9.39.4 GR2 a) 1. b / 13.18.38.4 GR2)");
    }

    private static string NonIntegerDetail(SetAmountRule rule, string detail) => rule switch
    {
        SetAmountRule.IndexTo => $"{detail} — a non-integer value (ISO 14.9.39.4 GR2 a) 1. a)",
        SetAmountRule.IndexBy => $"{detail} — a non-integer amount (ISO 14.9.39.4 GR3)",
        _ => $"{detail} — a non-integer amount (ISO 14.9.39.4 GR29)",
    };
}
