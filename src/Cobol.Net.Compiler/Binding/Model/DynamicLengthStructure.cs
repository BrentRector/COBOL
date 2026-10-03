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
/// program can address — a procedural operation sees the data alone (§8.5.1.11.2). The declaration decides two things
/// that ARE observable. (1) The item's MAXIMUM SIZE, whose second §8.5.1.10.1 candidate is "the largest integer that can
/// be stored in an item of the usage specified in the PREFIXED phrase" (<see cref="PrefixedMaximum"/>), and with it the
/// bound §13.18.19.3 SR4 puts on the LIMIT phrase (<see cref="MaximumLength"/>). (2) The item's PHYSICAL form in the
/// record a file carries — its data prefixed by the length field and followed by the delimiter
/// (<see cref="Layout"/>, §12.3.7.4 GR18 / GR19; docs/CONFORMANCE.md §3 D-DL3; kb/Work PB1094), realized once on the
/// way out and once on the way in by the record-image codec (<c>CobolContiguousLayout</c>), and counted by every record
/// size the file's RECORD clause states (<c>FileModel.MaxDynamicExtent</c>).</para>
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

    /// <summary>The width, in characters, of the PREFIXED phrase's length field — GR18's binary field: four for
    /// PREFIXED and SIGNED PREFIXED, two for SHORT PREFIXED and SIGNED SHORT PREFIXED, zero when there is no PREFIXED
    /// phrase. SIGNED changes the field's range (<see cref="PrefixedMaximum"/>), never its width.</summary>
    public int PrefixBytes => Prefix switch
    {
        DynamicLengthPrefix.Unsigned32 or DynamicLengthPrefix.Signed32 => 4,
        DynamicLengthPrefix.Unsigned16 or DynamicLengthPrefix.Signed16 => 2,
        _ => 0,
    };

    /// <summary>Does the declaration lay the item out at all — a PREFIXED length field, a DELIMITED delimiter or both
    /// (§12.3.7.4 GR18/GR19). False only for a physical-structure-name, which is refused (COBOLNET2257), so no item
    /// that reaches code generation names one.</summary>
    public bool HasLayout => PrefixBytes > 0 || Delimited;

    /// <summary>⛔ THE LAYOUT AS THE RUNTIME APPLIES IT (<see cref="CobolDynStructure"/>; kb/Work PB1094), over data
    /// that occupies <paramref name="unit"/> image characters per character position — null when the declaration lays
    /// nothing out. The ONE place the compiler turns a declaration into the runtime's layout: the record image
    /// codec's component table, the record-size accounting (<see cref="CobolDynStructure.Overhead"/>) and the
    /// elementary-record arm all read it through here, so they cannot disagree about what a structured item occupies.</summary>
    public CobolDynStructure? Layout(int unit) => HasLayout ? new CobolDynStructure(PrefixBytes, Delimited, unit) : null;

    /// <summary>"The maximum length associated with dynamic-length-structure-name-1" (§13.18.19.3 SR4): the most
    /// characters an item described with this structure can ever contain — the smaller of the length field's
    /// capacity and the implementor maximum (§8.5.1.10.1 with no LIMIT phrase), computed by the ONE producer.</summary>
    public int MaximumLength => CobolDynString.MaxSizeOf(null, PrefixedMaximum);
}
