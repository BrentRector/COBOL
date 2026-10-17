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

    /// <summary>⛔ THE SENDING OPERAND IS EVALUATED BEFORE THE RECEIVING OPERAND IS IDENTIFIED (kb/Work PB3261) — the
    /// store <paramref name="store"/> of <paramref name="value"/> (the rendered sending value) into
    /// <paramref name="receiver"/>, ordered so that every run-time step of evaluating the sender happens before any
    /// run-time step of identifying the receiver.
    /// <para><b>The rule.</b> ISO §14.9.25.4 GR1: "If identifier-1 is reference-modified, subscripted, or is a
    /// function-identifier, the reference modifier, subscript, or function-identifier is evaluated only once,
    /// immediately before data is moved to the first of the receiving operands", and "Item identification for
    /// identifier-2 is performed immediately before the data is moved to the respective data item" — the sender
    /// first, then each receiver. Which exception condition a statement raises when both sides fail is observable:
    /// §14.9.49.4 GR11 selects the declarative by "the first one that occurs in the order of evaluation of the
    /// statement that causes the exception to exist".</para>
    /// <para><b>Why a store needs this at all.</b> A store renders as <c>&lt;receiver&gt; = &lt;value&gt;</c>, and C#
    /// evaluates the left operand's address first: <c>CobolTable.At(TB.T, CobolTable.Occ(I, …)) =
    /// CobolString.Store(CobolString.RefMod(X, …), 4)</c> range-tests the receiving subscript (EC-BOUND-SUBSCRIPT)
    /// before the sending reference modifier (EC-BOUND-REF-MOD), so <c>MOVE X(1:L) TO T(I)</c> ran the wrong USE
    /// declarative. A reference-modified receiver splices (<c>X = SpliceInto(X, start, length, value)</c>), whose
    /// start and length are converted before the value argument, so the same inversion reached it by argument order.
    /// When both sides identify at run time the value is evaluated into a block-scoped local first; otherwise the
    /// order is unobservable and the store is rendered as it always was.</para>
    /// <para>A caller whose verb identifies the receiver FIRST (§14.6.4 7)'s left-to-right default with the receiver
    /// written first, as INITIALIZE's identifier-1) does not call this.</para></summary>
    public static string SenderFirst(BoundOperand sender, Place receiver, string value, Func<string, string> store) =>
        IdentifiesAtRunTime(sender) && IdentifiesAtRunTime(receiver)
            ? $"{{ var {SendingValueLocal} = {value}; {store(SendingValueLocal)} }}"
            : store(value);

    /// <summary>The block-scoped local <see cref="SenderFirst"/> evaluates the sending value into. Each use is its own
    /// block, and no store nests another, so one name serves every statement.</summary>
    private const string SendingValueLocal = "__sending";

    /// <summary>⛔ DOES IDENTIFYING THIS OPERAND EVALUATE ANYTHING AT RUN TIME? — the question <see cref="SenderFirst"/>
    /// asks of each side. A field operand answers by its <see cref="Place"/>; a literal, a figurative constant, the
    /// predefined NULL and an operand already refused evaluate nothing; every other operand (a function-identifier's
    /// value, an arithmetic expression, a record area) is evaluated when it is read, and answers true.</summary>
    public static bool IdentifiesAtRunTime(BoundOperand operand) => operand switch
    {
        BoundFieldOperand f => IdentifiesAtRunTime(f.Place),
        BoundStringLiteral or BoundNumericLiteral or BoundFigurative or BoundAllLiteral or BoundPredefinedNull
            or BoundOperandError => false,
        _ => true,
    };

    /// <summary>True when identifying <paramref name="place"/> evaluates something at run time: an address fragment
    /// <see cref="Freeze(Place, Func{string, string})"/> would hoist (a subscript, a reference modifier, a window
    /// offset — asked of Freeze itself, so the two cannot drift), an occurs-depending length (§14.6.4 step 6, which
    /// reads the DEPENDING ON item and may raise EC-BOUND-ODO), a reference modification's range test, or the guard
    /// of a formal that may be omitted (EC-PROGRAM-ARG-OMITTED).
    /// <para>⛔ A REFERENCE MODIFICATION IS RANGE-TESTED AT RUN TIME WHATEVER ITS POSITIONS (kb/Work PB3261 review
    /// R1-1). ISO §8.4.3.3.4 5) c): "The sum of leftmost-position and length minus the value one shall be less than or
    /// equal to the number of positions in the data item referenced by identifier-1", and every read and splice of a
    /// <see cref="RefModPlace"/> tests it when it executes (<c>StrRefMod</c> / <c>StrSpliceInto</c>), raising
    /// EC-BOUND-REF-MOD. Integer-literal positions are no exception: over a DYNAMIC LENGTH or ANY LENGTH identifier-1
    /// the number of positions is the item's current one, so <c>MOVE DL(1:5) TO T(I)</c> with DL holding two
    /// characters failed the sender's test only after the receiver's subscript, and over a fixed-size item a literal
    /// out of range binds (with its warning) where EC-BOUND-REF-MOD checking is on
    /// (<c>ReferenceResolver.LiteralRefModRangeViolation</c>). Answering "can this test fail?" would restate that
    /// screen here; a test that cannot fail costs the hoist one block-scoped local, so the answer is the rule's,
    /// true.</para></summary>
    public static bool IdentifiesAtRunTime(Place place)
    {
        bool runTime = false;
        Freeze(place, fragment => { runTime = true; return fragment; });
        return runTime || EvaluatesBesidesItsAddress(place);
    }

    private static bool EvaluatesBesidesItsAddress(Place place) => place switch
    {
        OdoGroupPlace or RefModPlace => true,
        PlaceDecorator d => EvaluatesBesidesItsAddress(d.Inner),
        MemberPlace m => Guarded(m.Path),
        DynTablePlace d => Guarded(d.Path),
        RedefViewPlace v => Guarded(v.Backing),
        _ => false,
    };

    private static bool Guarded(AccessPath path) => path.Segments is [RootFieldSegment { Guard: not null }, ..];

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
            Offset = Hoist(v.Offset, hoist),
            Coding = FreezeCoding(v.Coding, hoist),
            Cell = v.Cell is { } cell ? FreezePath(cell, hoist) : null,
            DynOrdinal = v.DynOrdinal is { } ordinal ? Hoist(ordinal, hoist) : null,
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
        var start = Hoist(r.Start, hoist);
        var length = r.Length is null ? null : Hoist(r.Length, hoist);
        return r with { Inner = inner, Start = start, Length = length };
    }

    /// <summary>A cell window's own address fragments (kb/Work PB1042): the cell path — which, inside a
    /// dynamic-capacity table's element, carries that table's subscript — and the component ordinal, which carries a
    /// subscript term per enclosing fixed table.
    /// <para>A <see cref="BitWindow"/>'s offset is not hoisted, because no verb that identifies its operands here can
    /// receive a USAGE BIT item with a run-time subscript: a BY REFERENCE bit argument's subscripts "consist of only
    /// fixed-point numeric literals or arithmetic expressions … in which all operands are numeric literals" (ISO
    /// §14.9.4.3 SR6, COBOLNET1683; INVOKE's twin is §14.9.23.3 SR12), and INSPECT, STRING and UNSTRING take only
    /// usage display or national operands (§14.9.22.3 SR1, §14.9.43.3 SR1, §14.9.48.3 SR4). The view's byte <see cref="RedefViewPlace.Offset"/>, which every other coding reads, is
    /// hoisted above.</para></summary>
    private static WindowCoding? FreezeCoding(WindowCoding? coding, Func<string, string> hoist) => coding switch
    {
        SlotWindow s => s with { Cell = FreezePath(s.Cell, hoist) },
        DynSlotWindow d => d with { Cell = FreezePath(d.Cell, hoist), Ordinal = Hoist(d.Ordinal, hoist) },
        VarGroupWindow g => g with { Cell = FreezePath(g.Cell, hoist), DynBase = Hoist(g.DynBase, hoist) },
        _ => coding,
    };

    private static AccessPath FreezePath(AccessPath path, Func<string, string> hoist)
    {
        if (!path.HasIndex) return path;
        var segments = new List<AccessSegment>(path.Segments.Count);
        foreach (var s in path.Segments)
            segments.Add(s switch
            {
                FixedTableSegment f => f with { OneBasedIndex = Hoist(f.OneBasedIndex, hoist) },
                DynTableSegment d => d with { OneBasedIndex = Hoist(d.OneBasedIndex, hoist) },
                CellTableSegment c => c with { Ordinal = Hoist(c.Ordinal, hoist) },   // kb/Work PB1042
                _ => s,
            });
        return new AccessPath(segments);
    }

    /// <summary>A self-evaluating position (<see cref="Position.IsSelfEvaluating"/>) is the same at any time; everything
    /// else is a run-time read, hoisted into a statement local (kb/Work PB2151: the constancy test is structural, never
    /// a scan of text).</summary>
    private static Position Hoist(Position position, Func<string, string> hoist) =>
        position.IsSelfEvaluating ? position : new PositionLocal(hoist(PositionRenderer.Render(position)));
}
