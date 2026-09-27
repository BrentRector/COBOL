// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>The length field a PREFIXED dynamic-length structure puts in front of the data (ISO §12.3.7.4 GR18):
/// "If SIGNED is specified, the length field is a signed binary field; otherwise the length field is an unsigned
/// binary field", and SHORT selects the shorter field of GR18's table.</summary>
public enum DynamicLengthPrefix
{
    /// <summary>No PREFIXED phrase — a DELIMITED-only structure, or a physical-structure-name.</summary>
    None,
    /// <summary><c>PREFIXED</c> — an unsigned 32-bit binary length field.</summary>
    Unsigned32,
    /// <summary><c>SIGNED PREFIXED</c> — a signed 32-bit binary length field.</summary>
    Signed32,
    /// <summary><c>SHORT PREFIXED</c> — an unsigned 16-bit binary length field.</summary>
    Unsigned16,
    /// <summary><c>SIGNED SHORT PREFIXED</c> — a signed 16-bit binary length field.</summary>
    Signed16,
}

/// <summary>
/// ONE <c>DYNAMIC LENGTH STRUCTURE</c> declaration of a SPECIAL-NAMES paragraph (ISO §12.3.7.2; kb/Work PB829) —
/// the physical layout a dynamic-length-structure-name stands for (§8.3.2.2.8: "A dynamic-length-structure-name
/// specifies the physical layout of a dynamic-length elementary item"), referenced by the DYNAMIC LENGTH clause
/// (§13.18.19.3 SR2) and visible in contained source elements (§8.4.6.1).
/// <para><b>What the layout decides, in a typed-native compiler.</b> A dynamic-length item IS a native .NET
/// <c>string</c> (§8.5.1.10.3 leaves its location to the implementor), so the layout's bytes exist nowhere a
/// program can address. What the declaration DOES decide is observable and is carried here: the item's MAXIMUM
/// SIZE, whose second §8.5.1.10.1 candidate is "the largest integer that can be stored in an item of the usage
/// specified in the PREFIXED phrase" (<see cref="PrefixedMaximum"/>), and with it the bound §13.18.19.3 SR4 puts on
/// the LIMIT phrase (<see cref="MaximumLength"/>). The layout itself (<see cref="Prefix"/>,
/// <see cref="Delimited"/>) is recorded as declared, so a byte image of the item — should one ever be materialized
/// at a file boundary — has exactly one place to read it from.</para>
/// </summary>
/// <param name="Name">dynamic-length-structure-name-1, as written.</param>
/// <param name="Prefix">The PREFIXED phrase's length field, or <see cref="DynamicLengthPrefix.None"/>.</param>
/// <param name="Delimited">The DELIMITED phrase is specified (§12.3.7.4 GR19 — a binary-zero delimiter follows
/// the data).</param>
/// <param name="PhysicalStructureName">physical-structure-name-1 when the clause names an implementor layout
/// instead of PREFIXED / DELIMITED — none is provided (§12.3.7.3 SR32; docs/CONFORMANCE.md §3 D-DL3), so such a
/// declaration is refused and this is carried only so references to the name resolve without a cascade.</param>
public sealed record DynamicLengthStructure(
    string Name, DynamicLengthPrefix Prefix, bool Delimited, string? PhysicalStructureName)
{
    /// <summary>§8.5.1.10.1's second candidate — the largest integer the PREFIXED phrase's length field can hold,
    /// or null when there is no PREFIXED phrase. WiseOwl COBOL's length fields are exactly the binary fields GR18
    /// names (32-bit, or 16-bit with SHORT), so these are GR18's own table values.</summary>
    public long? PrefixedMaximum => Prefix switch
    {
        DynamicLengthPrefix.Unsigned32 => uint.MaxValue,      // PREFIXED                 4294967295
        DynamicLengthPrefix.Signed32 => int.MaxValue,         // SIGNED PREFIXED          2147483647
        DynamicLengthPrefix.Unsigned16 => ushort.MaxValue,    // SHORT PREFIXED                65535
        DynamicLengthPrefix.Signed16 => short.MaxValue,       // SIGNED SHORT PREFIXED         32767
        _ => null,
    };

    /// <summary>"The maximum length associated with dynamic-length-structure-name-1" (§13.18.19.3 SR4): the most
    /// characters an item described with this structure can ever contain — the smaller of the length field's
    /// capacity and the implementor maximum (§8.5.1.10.1 with no LIMIT phrase), computed by the ONE producer.</summary>
    public int MaximumLength => CobolDynString.MaxSizeOf(null, PrefixedMaximum);
}
