// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ AN OCCURRENCE NUMBER AS A SUBSCRIPT (OR AN OCCURS DEPENDING COUNT) READS IT (kb/Work PB2695): the <c>long</c>
/// every table accessor positions by, plus — only when the item's value left <c>long</c>'s range — the value itself,
/// for the diagnostic.
/// <para>The narrowing to <c>long</c> SATURATES (<see cref="CobolNum.Position(Int128)"/>): ISO §8.4.2.3.4 GR2 — "If the
/// value of the subscript is not a positive integer or is less than one or is greater than the highest permissible
/// occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist" — needs an out-of-range subscript to
/// stay out of range on the way in. The saturated <c>long</c> is therefore right for every decision and wrong for the
/// one thing a message must name: the value the program holds. The detail text of <see cref="CobolTable.At{T}(T[], Occurrence)"/>
/// printed <c>9223372036854775807</c> for a <c>PIC 9(21)</c> subscript holding 10^20 + 1. The exact text rides WITH
/// the position instead of beside it (a thread-static "last wide value" slot would be hidden state that any later
/// unrelated narrowing could leave stale), so the one reader that raises is handed the one value it must print.</para>
/// <para><b>A wide carrier's <c>CobolTable.Occ</c> returns this; a narrow one returns the plain <c>long</c>.</b> Both
/// convert to each other implicitly, so a consumer that only positions (an OCCURS DEPENDING group extent, a record
/// length) takes a <c>long</c> parameter unchanged, and a consumer that RAISES a subscript condition takes the
/// <c>Occurrence</c> overload. The exact text exists only at the two saturation extremes, so an in-range subscript
/// pays one null reference and no allocation.</para>
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

    /// <summary><see cref="Of(Int128, int)"/> for an UNSIGNED-WIDE value (a 16-byte unsigned COMP-5 item).</summary>
    internal static Occurrence Of(UInt128 unscaled, int scale)
    {
        long position = CobolNum.PositionOf(unscaled, scale);
        return position == long.MaxValue
            ? new Occurrence(position, CobolNum.IntegerValueText(unscaled, scale))
            : new Occurrence(position);
    }
}
