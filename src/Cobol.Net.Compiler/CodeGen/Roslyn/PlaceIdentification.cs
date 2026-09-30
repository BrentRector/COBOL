// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ ITEM IDENTIFICATION, DONE ONCE (ISO §14.6.4; kb/Work PB1123) — the ONE mechanism that turns a
/// <see cref="Place"/> whose address is computed from RUN-TIME values (a subscript, a reference-modification
/// start/length, a Tier-B window offset) into a place whose address is FROZEN at the moment the statement begins.
/// <para>§14.6.4 7) — "the identifiers within a statement are evaluated in left to right order as the first
/// operation of the execution of that statement" — and a verb whose own general rules add no other timing (INSPECT
/// §14.9.22.4 GR6: "Item identification for any identifier is done only once as the first operation in the
/// execution of the INSPECT statement"; GR19 extends it across a format 3's two passes) owes exactly that. A
/// <see cref="Place"/> carries its subscripts as RENDER-TIME text, so an emitter that simply renders it at each point
/// of use re-evaluates the subscript there: <c>INSPECT YE(J) TALLYING J FOR ALL "A" REPLACING ALL "A" BY "Z"</c>
/// counted J up before the write-back, so the replaced image landed in <c>YE(4)</c>, not the <c>YE(1)</c> the
/// statement identified.</para>
/// <para><b>Freeze</b> hoists each such run-time fragment into a caller-supplied local (<c>hoist</c> emits
/// <c>var __t = &lt;fragment&gt;;</c> and returns the local's name) and returns the place rewritten to read the
/// locals. A caller freezes every operand of the statement in SOURCE order (left to right, §14.6.4), before the first
/// one is read or stored. A fragment that is a bare integer literal is left alone. A place kind this does not know
/// how to rewrite — a leaf with no run-time address (a register place, a RENAMES span) or a decoration that answers
/// a question about the operand's extent — is returned unchanged: the conservative answer is the pre-PB1123 one,
/// never a wrong one.</para>
/// </summary>
internal static class PlaceIdentification
{
    /// <summary>The place with every run-time address fragment hoisted through <paramref name="hoist"/>.</summary>
    public static Place Freeze(Place place, Func<string, string> hoist) => place switch
    {
        MemberPlace m => m with { Path = FreezePath(m.Path, hoist) },
        DynTablePlace d => d with { Path = FreezePath(d.Path, hoist) },
        RedefViewPlace v => v with { Backing = FreezePath(v.Backing, hoist), OffsetExpr = HoistFragment(v.OffsetExpr, hoist) },
        // The decorations that carry an address of their own: the slice, and the occurs-depending wrapper whose INNER
        // is the addressed storage. A reference modifier is evaluated AFTER its identifier's subscripts (§14.6.4
        // steps 6/7), so the inner is frozen first.
        RefModPlace r => FreezeRefMod(r, hoist),
        OdoGroupPlace o => o with { Inner = Freeze(o.Inner, hoist) },
        // Any other decoration (a table(ALL) enumeration …) answers a question about the operand, not its address.
        PlaceDecorator => place,
        _ => place,
    };

    private static RefModPlace FreezeRefMod(RefModPlace r, Func<string, string> hoist)
    {
        var inner = Freeze(r.Inner, hoist);
        string start = HoistFragment(r.Start, hoist);
        string? length = r.Length is null ? null : HoistFragment(r.Length, hoist);
        return r with { Inner = inner, Start = start, Length = length };
    }

    private static AccessPath FreezePath(AccessPath path, Func<string, string> hoist)
    {
        if (!path.HasIndex) return path;
        var segments = new List<AccessSegment>(path.Segments.Count);
        foreach (var s in path.Segments)
            segments.Add(s switch
            {
                FixedTableSegment f => f with { OneBasedIndex = HoistFragment(f.OneBasedIndex, hoist) },
                DynTableSegment d => d with { OneBasedIndex = HoistFragment(d.OneBasedIndex, hoist) },
                _ => s,
            });
        return new AccessPath(segments);
    }

    /// <summary>A bare integer literal evaluates to itself at any time; everything else is a run-time read.</summary>
    private static string HoistFragment(string fragment, Func<string, string> hoist) =>
        fragment.Length > 0 && fragment.All(char.IsAsciiDigit) ? fragment : hoist(fragment);
}
