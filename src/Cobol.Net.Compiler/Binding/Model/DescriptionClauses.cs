// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>The clauses of the standard's identical-description lists that are properties of the ENTRY rather than
/// of <see cref="PicInfo"/> — written once for every asker: ISO §8.5.3.1 (type equivalence), §9.3.6 3) (method
/// matching), §9.3.8.2.3 rules 3/6 (interface conformance: overrides, IMPLEMENTS), §14.8.2.3.2 rule 2 (BY REFERENCE
/// arguments) and §14.8.3.3 (returning items). The PICTURE clause itself is <see cref="PictureClauseIdentity"/>,
/// carried on <see cref="PicInfo.Clause"/>.</summary>
public static class DescriptionClauses
{
    /// <summary>ALIGNED (§13.18.1) and DYNAMIC LENGTH with its LIMIT phrase and structure-name (§13.18.19), asked of
    /// both sides; null when they agree. kb/Work PB1166: the activation / signature comparator
    /// (<c>OoConformance.DescriptionMismatch</c>) compared neither, so <c>PIC 1(8) USAGE BIT ALIGNED</c> crossed BY
    /// REFERENCE into <c>PIC 1(8) USAGE BIT</c>, and <c>PIC X DYNAMIC LENGTH</c> overrode <c>PIC X</c>, while the
    /// §8.5.3.1 profile compare (<see cref="StrongTypeModel"/>) kept its own copy of the same conjuncts. Both now
    /// read THIS predicate, so the two lists cannot drift apart.</summary>
    /// <param name="x">The formal parameter (or the sending returning item / one type's item).</param>
    /// <param name="y">The argument (or the receiving returning item / the other type's item).</param>
    public static string? AlignedOrDynamicLengthMismatch(DataItem x, DataItem y)
    {
        if (x.IsAligned != y.IsAligned)
            return $"ALIGNED mismatch (the clause is specified for the {(x.IsAligned ? "formal" : "argument")} only — "
                + "the corresponding items shall have the same ALIGNED clause)";
        if (x.IsDynamicLength != y.IsDynamicLength)
            return "DYNAMIC LENGTH mismatch (the clause is specified for the "
                + $"{(x.IsDynamicLength ? "formal" : "argument")} only — the corresponding items shall have the "
                + "same DYNAMIC LENGTH clause)";
        // A name is unique in its source element and inherited by reference, so name equality is structure identity.
        if (x.IsDynamicLength && (x.DynMaxSize != y.DynMaxSize
                || !CobolNames.Same(x.DynStructure?.Name, y.DynStructure?.Name)))
            return $"DYNAMIC LENGTH mismatch (maximum size {x.DynMaxSize} vs {y.DynMaxSize}, structure "
                + $"{x.DynStructure?.Name ?? "(implementor)"} vs {y.DynStructure?.Name ?? "(implementor)"} — the "
                + "corresponding items shall have the same DYNAMIC LENGTH clause, its LIMIT and STRUCTURE phrases "
                + "included)";
        return null;
    }
}
