// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ AN ORDINAL POSITION THAT MAY LEAVE <c>long</c> READS AS THIS — AN OCCURRENCE NUMBER (kb/Work PB2695) OR A
/// REFERENCE-MODIFIER LEFTMOST POSITION OR LENGTH (kb/Work PB2938): the <c>long</c> every accessor positions by, plus —
/// only when the item's value left <c>long</c>'s range — the value itself, for the diagnostic.
/// <para>The narrowing to <c>long</c> SATURATES (<see cref="CobolNum.Position(Int128)"/>): ISO §8.4.2.3.4 GR2 — "If the
/// value of the subscript is not a positive integer or is less than one or is greater than the highest permissible
/// occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist" — needs an out-of-range subscript to
/// stay out of range on the way in, and §8.4.3.3.4 rule 5 says the same of a reference modifier ("a value that
/// references a position outside the area of identifier-1"). The saturated <c>long</c> is therefore right for every
/// decision and wrong for the one thing a message must name: the value the program holds. The detail text of
/// <see cref="CobolTable.At{T}(T[], Occurrence)"/> printed <c>9223372036854775807</c> for a <c>PIC 9(21)</c> subscript
/// holding 10^20 + 1, and <see cref="CobolString.RefMod"/> printed <c>2147483647</c> for the same item as a leftmost
/// position. The exact text rides WITH the position instead of beside it (a thread-static "last wide value" slot would
/// be hidden state that any later unrelated narrowing could leave stale), so the one reader that raises is handed the
/// one value it must print.</para>
/// <para><b>A wide carrier's <c>CobolTable.Occ</c> / <c>CobolString.RefModPosition</c> returns this; a narrow one
/// returns the plain <c>long</c>.</b> Both convert to each other implicitly, so a consumer that only positions (an
/// OCCURS DEPENDING group extent, a record length) takes a <c>long</c> parameter unchanged, and a consumer that RAISES a
/// position condition takes the <c>Occurrence</c> overload. The exact text exists only at the two saturation extremes
/// (a value inside <c>long</c> prints as the <c>long</c> itself), so an in-range position pays one null reference and no
/// allocation. The name is the subscript's, the first consumer: a reference modifier's position is an ordinal position
/// of the same kind, and a second carrier type would be a second copy of this rule.</para>
/// </summary>
public readonly struct Occurrence
{
    private readonly string? _exact;

    /// <summary>The occurrence number every table accessor positions by (saturated at <c>long</c>'s extremes).</summary>
    public long Value { get; }

    /// <summary>An occurrence that is exactly <paramref name="value"/>.</summary>
    public Occurrence(long value)
    {
        Value = value;
        _exact = null;
    }

    private Occurrence(long saturated, string exact)
    {
        Value = saturated;
        _exact = exact;
    }

    /// <summary>True when <see cref="Value"/> is the saturated extreme of a value that left <c>long</c>'s range (or a
    /// genuine extreme, which is as out of range): out of EVERY table's range, whatever the table.</summary>
    internal bool IsSaturated => _exact is not null;

    /// <summary>This position floored at <paramref name="floor"/>. A floor that moves the value keeps the exact text of
    /// the value it replaced, so a diagnostic still names what the program holds; the reference-modifier length uses
    /// it to keep a hugely negative SPECIFIED length off the omitted-length sentinel
    /// (<see cref="CobolString.SpecifiedRefModLength"/>).</summary>
    internal Occurrence AtLeast(long floor) => Value >= floor ? this : new Occurrence(floor, ToString());

    /// <summary>A native <c>long</c> subscript, as the occurrence it is.</summary>
    public static implicit operator Occurrence(long value) => new(value);

    /// <summary>The position every non-raising consumer takes (saturated if the value left <c>long</c>).</summary>
    public static implicit operator long(Occurrence occurrence) => occurrence.Value;

    /// <summary>The occurrence number the program holds — the exact value even past <c>long</c> — for a diagnostic.</summary>
    public override string ToString() => _exact ?? Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>The same position as an <see cref="Int128"/> — the host narrowings (<c>CobolNum.Position32</c>) take one,
    /// and <c>long</c> → <see cref="Int128"/> is itself a user-defined conversion, which C# does not chain after the
    /// conversion to <c>long</c> above.</summary>
    public static implicit operator Int128(Occurrence occurrence) => occurrence.Value;

    /// <summary>The occurrence <paramref name="unscaled"/> at <paramref name="scale"/> denotes
    /// (<see cref="CobolNum.PositionOf(Int128, int)"/>), carrying its exact value when that position saturated. A value
    /// at <c>long</c>'s extreme is indistinguishable from a saturated one, so it carries the text too: that is the only
    /// place the text is built, and a genuine <c>long.MaxValue</c> subscript is as out of range as any other.</summary>
    internal static Occurrence Of(Int128 unscaled, int scale)
    {
        long position = CobolNum.PositionOf(unscaled, scale);
        return position is long.MaxValue or long.MinValue
            ? new Occurrence(position, CobolNum.IntegerValueText(unscaled, scale))
            : new Occurrence(position);
    }

    /// <summary>The position an integer LITERAL of the program denotes when it left <c>long</c>'s range (kb/Work PB2938):
    /// the compiler emits it for a literal subscript or reference-modifier position past <c>long</c>, as
    /// <paramref name="integer"/> — optional sign and decimal digits (at most 38: the compiler saturates a longer literal at
    /// <see cref="Int128.MaxValue"/>), which is what the program wrote. Saturated like every other carrier, and carrying
    /// the literal's value for the diagnostic.</summary>
    public static Occurrence OfLiteral(string integer)
    {
        Int128 value = Int128.Parse(integer, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        return new Occurrence(CobolNum.Position(value), value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary><see cref="Of(Int128, int)"/> for an UNSIGNED-WIDE value (a 16-byte unsigned COMP-5 item).</summary>
    internal static Occurrence Of(UInt128 unscaled, int scale)
    {
        long position = CobolNum.PositionOf(unscaled, scale);
        return position == long.MaxValue
            ? new Occurrence(position, CobolNum.IntegerValueText(unscaled, scale))
            : new Occurrence(position);
    }
}
