// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE DOOR RULES OF THE .NET HOST ENTRY, WRITTEN ONCE (DESIGN-external-repository §4.3; kb/Work PB2099). A generated
/// host entry (<c>HostEntryEmitter</c>) is the .NET counterpart of a CALL statement: it lands every host value into a
/// detached storage of its formal's description before anything is activated, and decodes that storage back after the
/// activation. ISO §14.2.3 9) lands a BY CONTENT argument into "a data item with the same description and the same number
/// of bytes as the formal parameter" by "a COMPUTE statement without the ROUNDED phrase" when the formal is numeric and
/// "otherwise, a MOVE statement"; §14.2.3 10) lands BY VALUE by the same COMPUTE. §14.2.3 13): "When either the activating
/// or the activated runtime element is other than a COBOL runtime element, the implementor shall specify the restrictions
/// and mechanisms for all supported language products" — and the ONE restriction stated for .NET (owner decision
/// 2026-10-05) is that <b>the landing loses nothing</b>. A host site has no ON SIZE ERROR, and a silently narrowed amount
/// is a wrong answer, so every landing here is the COMPUTE or MOVE exactly when it would store the value exactly, and an
/// <see cref="ArgumentException"/> naming the parameter (and the member, for a group) otherwise.
/// <para>Every landing, alias and validity decision a host entry makes is a call to this class, never an inline test in
/// emitted code (§17.3 "The door rules are one function"). The numeric landings run the runtime's own COMPUTE store
/// (<see cref="CobolNum.TryStore(Int128, int, in NumProfile, CobolRounding, out Int128)"/> under
/// <see cref="CobolRounding.Prohibited"/>, which refuses an inexact transfer), so "exact" is decided by the same capacity
/// rule every COBOL receiver of that description obeys; the text landing is the MOVE's own alignment
/// (<see cref="CobolString.Store"/>); the validity decode is the class test NUMERIC (ISO §8.8.4.4.4 3) n)) as
/// <see cref="CobolNum.IsNumericImage"/> answers it.</para>
/// <para>It is the generated code's ABI, called by host entries compiled into other assemblies, and carries no contract
/// for a hand-written caller: a .NET host calls the generated entry, never this class.</para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HostLanding
{
    // ── Numeric landings: a host number → the formal's UNSCALED carrier value (§14.2.3 9)/10)'s COMPUTE, exact) ──────

    /// <summary>Land an integer host value into a fixed-point numeric formal of description <paramref name="formal"/>;
    /// returns the formal's unscaled value, which fits the formal's carrier because the landing is exact.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The COMPUTE would lose a digit or the sign.</exception>
    public static Int128 Land(long value, in NumProfile formal, string parameter, string? member) =>
        LandUnscaled(value, 0, formal, parameter, member);

    /// <inheritdoc cref="Land(long, in NumProfile, string, string?)"/>
    public static Int128 Land(Int128 value, in NumProfile formal, string parameter, string? member) =>
        LandUnscaled(value, 0, formal, parameter, member);

    /// <summary>Land an unsigned 128-bit host value — the host type of an UNSIGNED 16-byte binary item with no fraction
    /// positions, whose container holds [0, 2^128) (ISO §13.18.60.4 12); kb/Work R10) — into <paramref name="formal"/>.
    /// For a 16-byte container the result is the container's BITS, which the emitted <c>unchecked((UInt128))</c> store
    /// reinterprets exactly (<see cref="CobolNum.StoreU"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The COMPUTE would lose a digit.</exception>
    public static Int128 Land(UInt128 value, in NumProfile formal, string parameter, string? member) =>
        CobolNum.TryStoreU(value, 0, formal, CobolRounding.Prohibited, out Int128 stored)
            ? stored
            : throw Lossy(parameter, member, value.ToString(System.Globalization.CultureInfo.InvariantCulture), formal,
                CobolNum.TryStoreU(value, 0, formal, CobolRounding.Truncation, out _) ? Loss.LowOrder : Loss.HighOrder);

    /// <summary>Land a <see cref="decimal"/> host value — the host type of a fixed-point item with fraction positions —
    /// into <paramref name="formal"/>. The decimal's own scale is the sending operand's, so <c>1.50m</c> and <c>1.5m</c>
    /// land alike; a value with more nonzero fraction digits than the formal's scale is refused.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The COMPUTE would lose a digit or the sign.</exception>
    public static Int128 Land(decimal value, in NumProfile formal, string parameter, string? member)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        Int128 magnitude = ((Int128)(uint)bits[2] << 64) | ((Int128)(uint)bits[1] << 32) | (uint)bits[0];
        int scale = (bits[3] >> 16) & 0xFF;
        return LandUnscaled(bits[3] < 0 ? -magnitude : magnitude, scale, formal, parameter, member);
    }

    /// <summary>Land a <see cref="CobolDec"/> host value — the host type of a fixed-point item whose storage holds more
    /// digits than <see cref="decimal"/> carries — into <paramref name="formal"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The COMPUTE would lose a digit or the sign.</exception>
    public static Int128 Land(CobolDec value, in NumProfile formal, string parameter, string? member)
    {
        if (value.Sig < 0 && !formal.Signed)
            throw Lossy(parameter, member, Text(value), formal, Loss.Sign);
        return CobolNum.TryStore(value, formal, CobolRounding.Prohibited, out Int128 stored)
            ? stored
            : throw Lossy(parameter, member, Text(value), formal,
                CobolNum.TryStore(value, formal, CobolRounding.Truncation, out _) ? Loss.LowOrder : Loss.HighOrder);
    }

    private static Int128 LandUnscaled(Int128 value, int scale, in NumProfile formal, string parameter, string? member)
    {
        if (formal.ByteForm is NumericByteForm.Ieee32 or NumericByteForm.Ieee64)
            throw new InvalidOperationException($"HostLanding: '{parameter}' is a floating-point formal, which takes its own "
                + "carrier unconverted (the host type of a floating-point item is the item's carrier)");
        // A negative value into an unsigned item: "the absolute value of the sending value is used, and no operational
        // sign is generated for the receiving item" (ISO §14.9.25.4 6)), which drops the sign — a loss.
        if (value < 0 && !formal.Signed)
            throw Lossy(parameter, member, CobolNum.PlainValue(value, scale), formal, Loss.Sign);
        if (CobolNum.TryStore(value, scale, formal, CobolRounding.Prohibited, out Int128 stored))
            return stored;
        throw Lossy(parameter, member, CobolNum.PlainValue(value, scale), formal,
            CobolNum.TryStore(value, scale, formal, CobolRounding.Truncation, out _) ? Loss.LowOrder : Loss.HighOrder);
    }

    // ── Text landings: a host string → the formal's character content (§14.2.3 9)'s MOVE, exact) ─────────────────────

    /// <summary>Land a host string into an elementary formal of FIXED length <paramref name="positions"/> — alphanumeric,
    /// alphabetic, national, boolean, alphanumeric-edited or numeric-edited — as the MOVE fills it: aligned on the left
    /// and filled on the right, or on the right under JUSTIFIED (ISO §13.18.32.4 2)), with spaces for a character item and
    /// zeros for a boolean one. An edited formal takes the string as its character content; a host that holds the number
    /// formats it. The string is refused when it is longer than the item (the MOVE would truncate it) or when a boolean
    /// formal is given a character other than <c>0</c> or <c>1</c>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null (an OPTIONAL formal's omission is the
    /// entry's, decided before any landing).</exception>
    /// <exception cref="ArgumentOutOfRangeException">The MOVE would truncate the string, or a boolean position would not
    /// hold its character.</exception>
    public static string LandText(string? value, int positions, bool justified, bool boolean, string parameter,
        string? member)
    {
        string text = Present(value, parameter, member);
        RequireClass(text, boolean, parameter, member);
        if (text.Length > positions)
            throw new ArgumentOutOfRangeException(parameter, Quote(text),
                $"{Subject(parameter, member)} holds {positions} character position(s), and the {text.Length}-character "
                + "string would be truncated by the MOVE (ISO §14.2.3 9); a .NET host's argument is landed only when the "
                + "landing loses nothing, DESIGN-external-repository §4.3).");
        return CobolString.Store(text, positions, justified, boolean ? '0' : ' ');
    }

    /// <summary>Land a host string into a formal whose length is the argument's — a DYNAMIC LENGTH item, bounded by
    /// <paramref name="maximum"/> (ISO §13.18.19.4 2)), or an ANY LENGTH item (<paramref name="maximum"/>
    /// <see cref="CobolArg.Unstated"/>; §13.18.2.4 1): its length is the argument's). Nothing is filled.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The string is longer than the item's maximum, or a boolean position
    /// would not hold its character.</exception>
    public static string LandVariable(string? value, int maximum, bool boolean, string parameter, string? member)
    {
        string text = Present(value, parameter, member);
        RequireClass(text, boolean, parameter, member);
        if (maximum != CobolArg.Unstated && text.Length > maximum)
            throw new ArgumentOutOfRangeException(parameter, Quote(text),
                $"{Subject(parameter, member)} holds at most {maximum} character position(s), and the string has "
                + $"{text.Length} (DESIGN-external-repository §4.3).");
        return text;
    }

    private static string Present(string? value, string parameter, string? member) =>
        value ?? throw new ArgumentNullException(parameter,
            $"{Subject(parameter, member)} is not OPTIONAL, so a .NET host must pass a value for it (ISO §14.2.3 13); "
            + "DESIGN-external-repository §4.3).");

    private static void RequireClass(string text, bool boolean, string parameter, string? member)
    {
        if (!boolean) return;
        int bad = text.AsSpan().IndexOfAnyExcept('0', '1');
        if (bad >= 0)
            throw new ArgumentOutOfRangeException(parameter, Quote(text),
                $"{Subject(parameter, member)} is a boolean item, whose content is only the boolean values 0 and 1 (the class "
                + "test BOOLEAN, ISO §8.8.4.4.4 3) e)), "
                + $"and position {bad + 1} of the string is '{text[bad]}'.");
    }

    // ── Shape rules: one storage for one variable; an OCCURS array's length ──────────────────────────────────────────

    /// <summary>Whether two <c>ref</c> parameters of one CLR type that refer to <paramref name="sameVariable"/> are
    /// given ONE storage: true when they do and their formals have one description, so <c>Call(ref a, ref a)</c> is the
    /// host's <c>CALL … USING A A</c> (ISO §14.2.3 8): the activated element "operates as if the formal parameter occupies
    /// the same storage area as the argument"). One storage cannot be two different descriptions, so a variable passed
    /// to two formals of different descriptions is refused before anything is activated.</summary>
    /// <exception cref="ArgumentException">One variable was given to two formals of different descriptions.</exception>
    public static bool SharesStorage(bool sameVariable, bool sameDescription, string first, string second) =>
        sameVariable && (sameDescription
            ? true
            : throw new ArgumentException(
                $"The parameters '{first}' and '{second}' refer to one variable, and their formals have different "
                + "descriptions, which one storage cannot be (ISO §14.2.3 8); DESIGN-external-repository §4.3).", second));

    /// <summary>Require a DYNAMIC-CAPACITY table member's array to be a capacity the table can have: its length is the
    /// table's current capacity, which is never below the minimum capacity ("Integer-4 is the minimum capacity of the
    /// table", ISO §13.18.38.4 16)) and never above the maximum capacity, the implementor's limit (§8.5.1.9.1,
    /// <see cref="CobolDynTable{T}.MaxOccurrences"/>); returns the array.</summary>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">The array's length lies outside the table's capacities.</exception>
    public static T[] RequireCapacity<T>(T[]? array, int minimum, string parameter, string member)
    {
        if (array is null)
            throw new ArgumentNullException(parameter, $"{Subject(parameter, member)} is a table, and its array is null.");
        if (array.Length < minimum || array.Length > CobolDynTable<T>.MaxOccurrences)
            throw new ArgumentException($"{Subject(parameter, member)} is a dynamic-capacity table of at least {minimum} "
                + $"and at most {CobolDynTable<T>.MaxOccurrences} entries, and its array has {array.Length} (ISO §13.18.38.4 "
                + "16), §8.5.1.9.1).", parameter);
        return array;
    }

    /// <summary>Require an OCCURS member's array to have the table's <paramref name="occurrences"/> entries — the
    /// declared count of a fixed table, the DEPENDING ON value of an OCCURS DEPENDING ON table — and that count to lie
    /// within the table's bounds <paramref name="minimum"/>..<paramref name="maximum"/>; returns the array.</summary>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">The array's length is not <paramref name="occurrences"/>, or
    /// <paramref name="occurrences"/> lies outside the table's bounds.</exception>
    public static T[] RequireOccurrences<T>(T[]? array, long occurrences, int minimum, int maximum, string parameter,
        string member)
    {
        if (array is null)
            throw new ArgumentNullException(parameter, $"{Subject(parameter, member)} is a table, and its array is null.");
        if (occurrences < minimum || occurrences > maximum)
            throw new ArgumentException($"{Subject(parameter, member)} is a table of {minimum} to {maximum} entries, and "
                + $"its DEPENDING ON value is {occurrences} (ISO §13.18.38.4 7)).", parameter);
        if (array.Length != occurrences)
            throw new ArgumentException($"{Subject(parameter, member)} has {occurrences} entries, and its array has "
                + $"{array.Length} (DESIGN-external-repository §4.3).", parameter);
        return array;
    }

    /// <summary>The current number of occurrences of an OCCURS DEPENDING ON table in a record a host is landing: "The value
    /// of the data item referenced by data-name-1 represents the current number of occurrences of the subject of the
    /// entry" (ISO §13.18.38.4 7)), so it is the DEPENDING item's content in the record image — <paramref name="image"/>,
    /// the item's own character image, already landed — decoded by the validity rule, and it "shall fall within the
    /// bounds from integer-1 through integer-2" (the same rule). The caller then requires the table's array to have that
    /// many entries (<see cref="RequireOccurrences{T}"/>).</summary>
    /// <exception cref="ArgumentException">The DEPENDING item's content is not a valid representation of its description
    /// (it is no member the host lands: FILLER, or a REDEFINES view), or its value lies outside the table's bounds.</exception>
    public static int LandedOccurrences(string image, in NumProfile depending, int minimum, int maximum, string parameter,
        string member, string dependingName)
    {
        FormatException? bad = null;
        if (!TryOccurrences(image, depending, minimum, maximum, member, dependingName, ref bad, out int occurrences))
            throw new ArgumentException(bad!.Message, parameter, bad);
        return occurrences;
    }

    /// <summary>The current number of occurrences of an OCCURS DEPENDING ON table in a record image being decoded (ISO
    /// §13.18.38.4 7)): the DEPENDING item's content (<paramref name="image"/>) decoded by the validity rule
    /// (<see cref="TryDecode"/>) and within the table's bounds. Otherwise the record holds no table a .NET array can
    /// present — a reference to it in COBOL would set EC-BOUND-ODO — so the method returns false, the first such
    /// refusal is kept in <paramref name="failure"/>, and the record is not decoded.</summary>
    public static bool TryOccurrences(string image, in NumProfile depending, int minimum, int maximum, string member,
        string dependingName, ref FormatException? failure, out int occurrences)
    {
        occurrences = 0;
        if (!TryDecode(image, depending, dependingName, ref failure, out Int128 unscaled))
            return false;
        // Data-name-1 describes an integer (§13.18.38.3 17)); its P positions, if any, scale it up, and the scaling stops
        // once the value is past the bounds, so no product overflows.
        Int128 value = unscaled;
        for (int p = -depending.FractionScale; p > 0 && Int128.Abs(value) <= maximum; p--)
            value *= 10;
        if (value < minimum || value > maximum)
        {
            failure ??= new FormatException($"'{member}' is a table of {minimum} to {maximum} entries, and its DEPENDING ON "
                + $"item '{dependingName}' holds {value}, outside those bounds (ISO §13.18.38.4 7)), so it was not decoded "
                + "and its .NET variable is unchanged (DESIGN-external-repository §4.3).");
            return false;
        }
        occurrences = (int)value;
        return true;
    }

    /// <summary>Require a record image a host hands a published record's <c>FromImage</c> to be the record's whole
    /// storage, <paramref name="width"/> characters (one per byte, the group image every COBOL record image is); returns
    /// the image. A shorter or longer string is not an image of that description: padding or cutting it would invent or
    /// drop content, so it is refused.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="image"/> is not <paramref name="width"/> characters.</exception>
    public static string RequireImage(string? image, int width, string parameter)
    {
        if (image is null)
            throw new ArgumentNullException(parameter, $"The record image for '{parameter}' is null.");
        if (image.Length != width)
            throw new ArgumentException($"The record '{parameter}' is {width} character(s) of storage, and the image has "
                + $"{image.Length} (DESIGN-external-repository §4.3).", parameter);
        return image;
    }

    // ── Decodes: the formal's storage → the host type (exact by HostSurface.TypeOf's construction) ───────────────────

    /// <summary>The host integer of a fixed-point item with no fraction positions: <paramref name="unscaled"/> at
    /// <paramref name="scale"/> (≤ 0 — trailing P positions multiply it).</summary>
    /// <exception cref="OverflowException">The value has more than 38 digits, which no description whose host type is an
    /// integer can hold (a generated entry never passes one).</exception>
    public static Int128 Integer(Int128 unscaled, int scale) =>
        scale >= 0 ? unscaled : checked(unscaled * Pow10.AsWide(-scale));

    /// <summary>The host <see cref="decimal"/> of a fixed-point item with fraction positions, whose unscaled value has at
    /// most 28 digits and whose scale is at most 28 (the <see cref="decimal"/> row of the host-type table).</summary>
    /// <exception cref="OverflowException">The value is outside <see cref="decimal"/>'s 96-bit significand or 28-digit
    /// scale, which no description whose host type is <see cref="decimal"/> can hold (a generated entry never passes
    /// one); never a silently truncated significand.</exception>
    public static decimal Decimal(Int128 unscaled, int scale)
    {
        Int128 magnitude = Int128.Abs(unscaled);
        if (magnitude >> 96 != 0 || scale is < 0 or > 28)
            throw new OverflowException($"HostLanding.Decimal: {unscaled} at scale {scale} is not a decimal value");
        return new decimal((int)(uint)magnitude, (int)(uint)(magnitude >> 32), (int)(uint)(magnitude >> 64),
            unscaled < 0, (byte)scale);
    }

    /// <summary>The host <see cref="CobolDec"/> of a fixed-point item whose storage holds more digits than
    /// <see cref="decimal"/> carries: <paramref name="unscaled"/> × 10^−<paramref name="scale"/>, exactly.</summary>
    public static CobolDec Dec(Int128 unscaled, int scale) => CobolDec.From(unscaled, scale);

    /// <summary>The host <see cref="CobolDec"/> of an UNSIGNED 16-byte binary item with a scale — the one storage no
    /// exact .NET type holds: its container reaches 2^128 units (ISO §13.18.60.4 12); kb/Work R10) and
    /// <see cref="CobolDec"/>'s significand 2^127. Content below 2^127 units decodes exactly; content at or above it is
    /// the stated ISO §14.2.3 13) restriction for .NET (DESIGN-external-repository §4.3): it is refused like content
    /// that is not numeric — false, the host variable unchanged, and the first refusal kept in
    /// <paramref name="failure"/> for the entry to throw after every other write-back.</summary>
    public static bool TryDec(UInt128 unscaled, int scale, string name, ref FormatException? failure, out CobolDec value)
    {
        if (unscaled <= (UInt128)Int128.MaxValue)
        {
            value = CobolDec.From((Int128)unscaled, scale);
            return true;
        }
        value = default;
        failure ??= new FormatException($"'{name}' holds {CobolNum.PlainValue(unscaled, scale)}, which is 2^127 units or "
            + "more: an unsigned 16-byte binary item with fraction or P positions crosses to .NET only below 2^127 units "
            + "(ISO §14.2.3 13); DESIGN-external-repository §4.3), so its .NET variable is unchanged.");
        return false;
    }

    /// <summary>⛔ THE VALIDITY RULE (DESIGN-external-repository §4.3 "Write-back and RETURNING — one validity rule"): decode
    /// the character <paramref name="image"/> of a numeric item of description <paramref name="item"/> only when it is a
    /// valid representation of that description — the class test NUMERIC is true of it (ISO §8.8.4.4.4 3) n)), as
    /// <see cref="CobolNum.IsNumericImage"/>, the runtime's one implementation of that test, answers. Content a
    /// REDEFINES alias, a reference modification or a non-conforming activator left there is refused: the method returns
    /// false, the host variable is left unchanged, and the FIRST such refusal is kept in <paramref name="failure"/> as the
    /// <see cref="FormatException"/> naming <paramref name="name"/> and quoting the characters, which the entry throws
    /// after every other write-back (and never in place of an exception already propagating from the boundary).</summary>
    public static bool TryDecode(string? image, in NumProfile item, string name, ref FormatException? failure,
        out Int128 unscaled)
    {
        if (image is not null && CobolNum.IsNumericImage(image, item))
        {
            unscaled = CobolNum.ParseImage(image, item);
            return true;
        }
        unscaled = 0;
        failure ??= new FormatException($"'{name}' holds {(image is null ? "no content" : Quote(image))}, which is not a "
            + "valid representation of its numeric description (the class test NUMERIC, ISO §8.8.4.4.4 3) n), is false), "
            + "so it was not decoded and its .NET variable is unchanged (DESIGN-external-repository §4.3).");
        return false;
    }

    // ── Messages ─────────────────────────────────────────────────────────────────────────────────────────────────────

    private enum Loss { HighOrder, LowOrder, Sign }

    private static ArgumentOutOfRangeException Lossy(string parameter, string? member, string value, in NumProfile formal,
        Loss loss) =>
        new(parameter, value, $"{Subject(parameter, member)} is {Describe(formal)}, and the value {value} "
            + loss switch
            {
                Loss.Sign => "is negative: the COMPUTE into an unsigned item would drop its sign",
                Loss.LowOrder => "has digits right of what the item holds: the COMPUTE without ROUNDED would truncate "
                    + "them at the low-order end",
                _ => "is further from zero than the item holds: the COMPUTE would truncate it at the high-order end",
            }
            + " (ISO §14.2.3 9)/10); a .NET host's argument is landed only when the landing loses nothing, "
            + "DESIGN-external-repository §4.3).");

    private static string Describe(in NumProfile formal) =>
        $"{(formal.Signed ? "signed" : "unsigned")} numeric with {formal.Digits} digit(s) and scale {formal.FractionScale}"
        + (formal.Truncation == NumericTruncation.BinaryCapacity ? $" in a {formal.StorageLength}-byte binary container" : "");

    private static string Subject(string parameter, string? member) =>
        member is null ? $"The formal '{parameter}'" : $"The member '{member}' of the formal '{parameter}'";

    private static string Quote(string text) => $"[{text}]";

    private static string Text(CobolDec value) =>
        value.Exp <= 0 ? CobolNum.PlainValue(value.Sig, -value.Exp) : $"{value.Sig}E{value.Exp}";
}
