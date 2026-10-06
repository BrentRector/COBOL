// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

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
    /// <summary>The ONE hoist every verb emitter hands <see cref="Freeze(Place, Func{string, string})"/>: it writes
    /// <c>var __identN = &lt;fragment&gt;;</c> at the current point of the statement's emission and returns the local's
    /// name. A verb freezes ALL of its operands before it reads or stores any (§14.6.4 7), so the locals are assigned
    /// at the head of the statement. UNSTRING and INSPECT (kb/Work PB1123) share it, and so does any verb adopted
    /// next.</summary>
    public static Func<string, string> Hoister(EmitContext ctx) => fragment =>
    {
        string name = $"__ident{ctx.Names.NextIdentTmp()}";
        ctx.Writer.Line($"var {name} = {fragment};");
        return name;
    };

    /// <summary>The operand with its data reference frozen — a field operand's <see cref="Place"/>; a literal, a
    /// figurative or a function result has no run-time address of its own (a function-identifier's argument
    /// subscripts were hoisted at bind time, <c>DataBinder.PendingPreOps</c>) and is returned unchanged.</summary>
    public static BoundOperand? Freeze(BoundOperand? op, Func<string, string> hoist) =>
        op is BoundFieldOperand f ? f with { Place = Freeze(f.Place, hoist) } : op;

    /// <summary>The place with every run-time address fragment hoisted through <paramref name="hoist"/>.</summary>
    public static Place Freeze(Place place, Func<string, string> hoist) => place switch
    {
        MemberPlace m => m with { Path = FreezePath(m.Path, hoist) },
        DynTablePlace d => d with { Path = FreezePath(d.Path, hoist) },
        RedefViewPlace v => FreezeView(v, hoist),
        // The decorations that carry an address of their own: the slice, and the occurs-depending wrapper whose INNER
        // is the addressed storage. A reference modifier is evaluated AFTER its identifier's subscripts (§14.6.4
        // steps 6/7), so the inner is frozen first.
        RefModPlace r => FreezeRefMod(r, hoist),
        OdoGroupPlace o => o with { Inner = Freeze(o.Inner, hoist) },
        // Any other decoration (a table(ALL) enumeration …) answers a question about the operand, not its address.
        PlaceDecorator => place,
        _ => place,
    };

    /// <summary>A window's address fragments, and — for a window over a class's own STORAGE CELL — the CELL ITSELF.
    /// ⛔ THE CELL A CLASS LIVES IN IS A RUN-TIME ADDRESS FRAGMENT TOO (kb/Work PB2087). The root that names it is a
    /// member the activation machinery RE-SEATS: a method's per-activation cell (LOCAL-STORAGE claimed onto a cell) and an
    /// area formal's data-address pointer are instance members saved and re-seeded at every activation of the method
    /// (§8.6.4), and a BASED item's cell follows its pointer. A place that rendered the root at each access therefore
    /// followed it: an INVOKE of the same method, while the argument's view was still live as the formal of the
    /// activated element, redirected the view to the INNER activation's storage — although ISO §14.2.3 GR8 makes the
    /// formal occupy "the same storage area as the argument", the area identified when the statement began (§14.6.4 7;
    /// §14.9.23.4 GR7a / §14.9.4.4 GR3a). So the cell is held in a statement-local, and the backing — the cell's image,
    /// <c>Ref</c>, in every cell-backed class — and every coding that names the same cell are re-anchored on it. A
    /// formal's *-ARG-OMITTED guard stays on the new root (the hoist reads the cell without it, because §14.9.4.4 GR12 /
    /// §14.9.23.4 GR10 exempt a reference "as an argument", and an omitted formal's cell is a real, blank one), so a
    /// reference through the place is still checked where it was. A cell reached through a dynamic-capacity table's
    /// element (more than a root) keeps its own rendering: its receiving accessor grows the table (§8.5.1.9.3).</summary>
    private static RedefViewPlace FreezeView(RedefViewPlace v, Func<string, string> hoist)
    {
        var frozen = v with
        {
            Backing = FreezePath(v.Backing, hoist),
            OffsetExpr = HoistFragment(v.OffsetExpr, hoist),
            Coding = FreezeCoding(v.Coding, hoist),
            Cell = v.Cell is { } cell ? FreezePath(cell, hoist) : null,
            DynOrdinal = v.DynOrdinal is { } ordinal ? HoistFragment(ordinal, hoist) : null,
        };
        if (v.Cell is not { Segments: [RootFieldSegment root] }) return frozen;
        string local = hoist(root.CsField);
        var anchored = new AccessPath([new RootFieldSegment(local, root.Guard is { } g ? g with { CarrierPrefix = local } : null)]);
        return frozen with
        {
            Backing = anchored.Add(new MemberSegment(nameof(CobolNet.Runtime.StorageCell.Ref))),
            Cell = anchored,
            Coding = frozen.Coding is CellWindowCoding c && c.Cell is { Segments: [RootFieldSegment cr] } && cr.CsField == root.CsField
                ? c with { Cell = anchored } : frozen.Coding,
        };
    }

    private static RefModPlace FreezeRefMod(RefModPlace r, Func<string, string> hoist)
    {
        var inner = Freeze(r.Inner, hoist);
        string start = HoistFragment(r.Start, hoist);
        string? length = r.Length is null ? null : HoistFragment(r.Length, hoist);
        return r with { Inner = inner, Start = start, Length = length };
    }

    /// <summary>A cell window's own address fragments (kb/Work PB1042): the cell path — which, inside a
    /// dynamic-capacity table's element, carries that table's subscript — and the component ordinal, which carries a
    /// subscript term per enclosing fixed table.</summary>
    private static WindowCoding? FreezeCoding(WindowCoding? coding, Func<string, string> hoist) => coding switch
    {
        SlotWindow s => s with { Cell = FreezePath(s.Cell, hoist) },
        DynSlotWindow d => d with { Cell = FreezePath(d.Cell, hoist), Ordinal = HoistFragment(d.Ordinal, hoist) },
        VarGroupWindow g => g with { Cell = FreezePath(g.Cell, hoist), DynBase = HoistFragment(g.DynBase, hoist) },
        _ => coding,
    };

    private static AccessPath FreezePath(AccessPath path, Func<string, string> hoist)
    {
        if (!path.HasIndex) return path;
        var segments = new List<AccessSegment>(path.Segments.Count);
        foreach (var s in path.Segments)
            segments.Add(s switch
            {
                FixedTableSegment f => f with { OneBasedIndex = HoistFragment(f.OneBasedIndex, hoist) },
                DynTableSegment d => d with { OneBasedIndex = HoistFragment(d.OneBasedIndex, hoist) },
                CellTableSegment c => c with { Ordinal = HoistFragment(c.Ordinal, hoist) },   // kb/Work PB1042
                _ => s,
            });
        return new AccessPath(segments);
    }

    /// <summary>A bare integer literal evaluates to itself at any time; everything else is a run-time read.</summary>
    private static string HoistFragment(string fragment, Func<string, string> hoist) =>
        fragment.Length > 0 && fragment.All(char.IsAsciiDigit) ? fragment : hoist(fragment);
}
