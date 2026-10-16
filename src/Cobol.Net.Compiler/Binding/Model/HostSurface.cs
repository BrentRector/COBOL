// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>Which row of the host-type table (<see cref="HostSurface.TypeOf"/>) a description takes — the row decides the
/// C# type a .NET host sees and which <c>HostLanding</c> landing and decode the generated host entry calls for it.</summary>
public enum HostTypeKind
{
    /// <summary>A fixed-point numeric item with no positions right of the decimal point: <c>long</c>, <c>Int128</c> or
    /// <c>UInt128</c>, the first that holds every value the item's storage can hold.</summary>
    Integer,
    /// <summary>A fixed-point numeric item with fraction positions whose unscaled value fits <see cref="decimal"/>.</summary>
    Decimal,
    /// <summary>A fixed-point numeric item whose storage holds more than <see cref="decimal"/> carries:
    /// <see cref="CobolDec"/>, an exact significand-and-exponent value.</summary>
    Dec,
    /// <summary>A floating-point item: its own CLR carrier.</summary>
    Float,
    /// <summary>An alphanumeric, alphabetic, national, boolean, alphanumeric-edited or numeric-edited item, DYNAMIC
    /// LENGTH or ANY LENGTH included: <see cref="string"/>.</summary>
    Text,
    /// <summary>An INDEX data item: <c>long</c>.</summary>
    Index,
    /// <summary>A data, program or function pointer: the runtime's carrier (<see cref="ManagedPointer"/>,
    /// <see cref="ProgramPointer"/>, <see cref="FunctionPointer"/>).</summary>
    Pointer,
    /// <summary>An object reference: the referenced class's or interface's public CLR type, <see cref="CobolObject"/>
    /// when universal.</summary>
    ObjectReference,
    /// <summary>A group: the published record struct of its description, named by the host entry's allocator.</summary>
    Group,
}

/// <summary>The host type of one description: its row and, for an elementary item, the C# type text (root-relative, as
/// every emitted file's <c>using global::CobolNet.Runtime;</c> resolves it). A group's type is the published record
/// struct the host entry's own name allocator names, so its <see cref="CsType"/> is null here.</summary>
public readonly record struct HostType(HostTypeKind Kind, string? CsType);

/// <summary>
/// ⛔ THE HOST TYPE OF A DESCRIPTION, DECIDED ONCE (DESIGN-external-repository §4.3 "The host type of a description";
/// kb/Work PB2099). A generated host entry presents every formal, RETURNING item and published record member to a .NET
/// host as the type this function names, and lands and decodes it through <c>HostLanding</c> by the same row, so the
/// emitter, the published record struct and (from slice 4) the repository record's decoder cannot disagree about it.
/// ISO §14.2.3 13): "When either the activating or the activated runtime element is other than a COBOL runtime element,
/// the implementor shall specify the restrictions and mechanisms for all supported language products"; this table is
/// that specification's type mapping for .NET.
/// <para>⛔ <b>DECODING NEVER NARROWS:</b> the host type of a description holds EVERY value its storage can hold, so a
/// write-back never has a value it cannot return. A fixed-point item's storage holds what its capacity discipline
/// admits — a DISPLAY, PACKED or plain COMPUTATIONAL item its PICTURE's digits, a binary item whose truncation is by
/// capacity (COMP-5, BINARY-CHAR … BINARY-DOUBLE) its whole container (ISO §13.18.60.4 12); kb/Work R10) —
/// <see cref="PicInfo.UnscaledDigitBound"/>, the one statement of that bound. The one storage no exact .NET type holds,
/// an UNSIGNED 16-byte container with a scale (a 19- or more-digit COMP-5 PICTURE with V or P positions), takes
/// <see cref="CobolDec"/> under a stated GR13 restriction: content of 2^127 units or more is refused by the validity
/// decode (<c>HostLanding.TryDecode</c>), never narrowed.</para>
/// <para>⛔ <b>KEYED ON THE BOUND DESCRIPTION.</b> The design's §6.3 decodes a repository record into a codec-owned,
/// throw-away forest of bound <see cref="DataItem"/>s so every derived member answers as on a bound item, so the bound
/// description is the one input both the emitter (slice 3) and the record decoder (slice 4) hold; there is no second
/// model to re-key this function onto (DESIGN-external-repository §21 item 3, settled 2026-10-14).</para>
/// <para>The type describes ONE occurrence: an OCCURS member's host property is an array of this type, which the
/// published record struct writes.</para>
/// </summary>
public static class HostSurface
{
    /// <summary>The host type of <paramref name="description"/> (§4.3's table).</summary>
    public static HostType TypeOf(DataItem description)
    {
        if (description.IsGroup) return new HostType(HostTypeKind.Group, null);
        PicInfo pic = description.Pic
            ?? throw new InvalidOperationException($"HostSurface.TypeOf: '{description.CobolName}' has no description");
        return pic.Category switch
        {
            PicCategory.Numeric when pic.Usage is Usage.Index => new HostType(HostTypeKind.Index, "long"),
            PicCategory.Numeric when pic.IsFloat => new HostType(HostTypeKind.Float, pic.ClrType),
            PicCategory.Numeric => FixedPoint(pic),
            PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National or PicCategory.Boolean
                => new HostType(HostTypeKind.Text, "string"),
            PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer
                => new HostType(HostTypeKind.Pointer, pic.ClrType),
            PicCategory.ObjectReference => new HostType(HostTypeKind.ObjectReference, pic.ClrType),
            _ => throw new InvalidOperationException(
                $"HostSurface.TypeOf: '{description.CobolName}' is of category {pic.Category}, which has no host type"),
        };
    }

    /// <summary>A fixed-point item's row. With no fraction positions (<c>Scale ≤ 0</c>) a binary item whose truncation is
    /// by capacity is mapped by its container's exact range — a signed container of up to 8 bytes or an unsigned one of
    /// fewer is <c>long</c>, an unsigned 8-byte or a signed 16-byte container <c>Int128</c>, an unsigned 16-byte one
    /// <c>UInt128</c> — and every other item by its value's digits (the unscaled bound plus its trailing P positions):
    /// up to 18 <c>long</c>, up to 38 <c>Int128</c>, beyond that <see cref="CobolDec"/>. With fraction positions an
    /// unscaled bound and a scale of at most 28 each is <see cref="decimal"/>, and anything wider
    /// <see cref="CobolDec"/>.</summary>
    private static HostType FixedPoint(PicInfo pic)
    {
        bool capacity = pic.Truncation == NumericTruncation.BinaryCapacity;
        // The digits the UNSCALED value can have: a capacity item's container (UnscaledDigitBound), every other item its
        // PICTURE's 9 positions — "The symbol 'P' is not counted in the size of the item" (ISO §13.18.40.4 14)), so its P
        // positions scale the value and are counted by Scale, never twice (UnscaledDigitBound counts them, an upper bound
        // for its own use, which at worst gives a capacity item with P positions the wider of two exact types).
        int unscaledDigits = capacity ? pic.UnscaledDigitBound : pic.Digits;
        if (pic.Scale <= 0)
        {
            if (capacity && pic.Scale == 0)
                return new HostType(HostTypeKind.Integer, (pic.StorageWidth, pic.Signed) switch
                {
                    (16, false) => "UInt128",
                    (16, true) or (8, false) => "Int128",
                    _ => "long",
                });
            int valueDigits = unscaledDigits - pic.Scale;
            return valueDigits <= 18 ? new HostType(HostTypeKind.Integer, "long")
                : valueDigits <= 38 ? new HostType(HostTypeKind.Integer, "Int128")
                : new HostType(HostTypeKind.Dec, "CobolDec");
        }
        return unscaledDigits <= 28 && pic.Scale <= 28
            ? new HostType(HostTypeKind.Decimal, "decimal")
            : new HostType(HostTypeKind.Dec, "CobolDec");
    }
}
