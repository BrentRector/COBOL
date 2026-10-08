// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>⛔ ISO §8.8.4.2.12 — THE COMPARISON OF TWO STRONGLY-TYPED GROUP ITEMS, lowered at bind to the comparisons
/// of their elementary items (kb/Work PB1469):
/// <para>"When two strongly-typed group items are compared, each elementary item of the first operand is compared
/// with the corresponding elementary item of the second operand, in accordance with the rules for comparison of
/// elementary items and in the order in which the elementary items are specified in the strongly-typed group items.
/// This comparison proceeds until a pair of elementary items is unequal or the final pair of elementary items is
/// compared. The operand that contains the elementary item that is greater than the corresponding elementary item
/// is determined to be the greater operand. Two strongly-typed group operands are determined to be equal if all
/// pairs of corresponding elementary items are equal."</para>
/// <para>⛔ WHY A LOWERING AND NOT AN IMAGE. The relation used to be emitted as ONE whole-group character-image
/// comparison, which is element order only when every leaf's character image happens to order like its value: a
/// FLOAT leaf's image orders by its sign/exponent characters (<c>-5 &lt; 3</c> answered GE), a SIGNED DISPLAY leaf's
/// overpunch does not order algebraically (it was staged COBOLNET0899 instead), a pointer leaf has no image at all
/// (the run unit aborted), and a collating sequence with equal weights makes image-unequal leaves element-equal.
/// Each elementary pair is instead a <see cref="BoundRelational"/> over the two leaves' own places, so every leaf
/// kind compares by the SAME rule an explicit relation between those two items uses (§8.8.4.2.4 numeric,
/// §8.8.4.2.7/.9 alphanumeric/national under the collating sequence, §8.8.4.2.8 boolean, §8.8.4.2.16 pointer) —
/// the NEXT leaf kind is automatic, because there is nothing here that knows about kinds.</para>
/// <para>The lexicographic order is composed from each pair's strict relation and its equality:
/// <c>A &lt; B ⟺ a₁ &lt; b₁ ∨ (a₁ = b₁ ∧ (a₂ &lt; b₂ ∨ (a₂ = b₂ ∧ …)))</c>, terminating in FALSE for a strict
/// operator and TRUE for <c>&lt;=</c> / <c>&gt;=</c> ("equal if all pairs … are equal") — built in halves
/// (<see cref="Before"/>) so its nesting is logarithmic in the number of elementary items. Written that way — rather
/// than as "not greater" — the relation stays exact for an UNORDERED pair (a NaN float leaf under native
/// arithmetic, §8.8.4.2.4): neither less, nor equal, so every ordering over it is false, as the pair's own
/// relation says. Evaluation is short-circuit, so no pair after the first unequal one is read ("proceeds until a
/// pair of elementary items is unequal") and none of its checks can fire.</para></summary>
internal sealed class StrongGroupComparison(BinderContext ctx)
{
    /// <summary>One corresponding pair of elementary items, with the run-time condition under which BOTH exist (an
    /// occurrence of an occurs-depending table beyond its minimum) or null when they always do.</summary>
    private readonly record struct ElementPair(Place Left, Place Right, BoundCondition? Present);

    /// <summary>The relation <paramref name="left"/> <paramref name="op"/> <paramref name="right"/> between two
    /// strongly-typed groups of the same type, as the condition over their elementary items — or null, with
    /// <paramref name="unbuilt"/> naming the member this implementation cannot yet reach.</summary>
    public BoundCondition? Lower(Place left, Place right, string op, out string? unbuilt)
    {
        unbuilt = null;
        if (PlaceCursor.Over(left) is not { } lc || PlaceCursor.Over(right) is not { } rc)
        {
            unbuilt = $"the storage form of '{left.Item.CobolName ?? left.Item.CsName}'";
            return null;
        }
        var ll = new List<(Place Leaf, BoundCondition? Present)>();
        var rl = new List<(Place Leaf, BoundCondition? Present)>();
        if (!Walk(lc, lc, present: null, ll, ref unbuilt) || !Walk(rc, rc, present: null, rl, ref unbuilt)) return null;
        // "the CORRESPONDING elementary item": §8.5.3.1 makes two type declarations equivalent when "for each
        // elementary item in one type declaration there is a corresponding elementary item in the other type
        // declaration, starting at the same relative byte or bit position" — a correspondence of ELEMENTARY items by
        // position, not of the declarations' group structure (two equivalent declarations in different source
        // elements may group their leaves differently). Declaration order is position order once REDEFINES entries
        // are set aside, so the two leaf lists pair by index.
        if (ll.Count != rl.Count)
        {
            unbuilt = $"the elementary items of '{left.Item.CobolName ?? left.Item.CsName}' (the operands' leaf counts differ)";
            return null;
        }
        var pairs = new List<ElementPair>(ll.Count);
        for (int i = 0; i < ll.Count; i++) pairs.Add(new ElementPair(ll[i].Leaf, rl[i].Leaf, ll[i].Present));
        return op switch
        {
            "==" => AllEqual(pairs),
            "!=" => new BoundNot(AllEqual(pairs)),
            "<" or "<=" => Lexicographic(pairs, "<", allEqualAnswer: op == "<="),
            ">" or ">=" => Lexicographic(pairs, ">", allEqualAnswer: op == ">="),
            _ => throw new InvalidOperationException($"relation operator '{op}' is not one §8.8.4.2 defines"),
        };
    }

    /// <summary>Collect the elementary items beneath <paramref name="cur"/> in declaration order ("in the order in
    /// which the elementary items are specified"), each with the condition under which it takes part.</summary>
    private bool Walk(PlaceCursor cur, PlaceCursor root, BoundCondition? present,
        List<(Place Leaf, BoundCondition? Present)> leaves, ref string? unbuilt)
    {
        foreach (var child in StorageChildren(cur.Item))
        {
            if (cur.Child(child) is not { } ccur)
            {
                unbuilt = $"the member '{child.CobolName ?? "FILLER"}' (a REDEFINES storage tier)";
                return false;
            }
            if (child.IsDynamicTable)
            {
                // A dynamic-capacity table has a per-operand RUN-TIME capacity (§8.5.1.9), so its occurrences cannot
                // be enumerated at bind, and what corresponds past the smaller capacity is §14.6.9.3's table rule.
                unbuilt = $"the dynamic-capacity table '{child.CobolName ?? "FILLER"}'";
                return false;
            }
            bool ok = child.IsTable ? WalkTable(child, ccur, root, present, leaves, ref unbuilt)
                : child.IsGroup ? Walk(ccur, root, present, leaves, ref unbuilt)
                : Add(leaves, ccur.ToPlace(), present);
            if (!ok) return false;
        }
        return true;
    }

    private static bool Add(List<(Place Leaf, BoundCondition? Present)> leaves, Place leaf, BoundCondition? present)
    {
        leaves.Add((leaf, present));
        return true;
    }

    /// <summary>Every occurrence of the table <paramref name="table"/>, in order — each one's elementary items are
    /// "the elementary items … specified" in the group, one occurrence after another.
    /// <para>⚠ AN OCCURS-DEPENDING TABLE'S OCCURRENCES BEYOND ITS MINIMUM EXIST ONLY UP TO THE DEPENDING VALUE:
    /// §13.18.38.4 GR8 makes a strongly-typed group with such a table an occurs-depending group item, of which "only
    /// that part of the table area that is specified by the value of the data item referenced by data-name-1 at the
    /// start of the operation will be used" (a) outside the group, b) inside it and sending — a comparison operand
    /// is never receiving). ONE count answers for BOTH operands: data-name-1 outside the group is one item for every
    /// group of the type; inside it, §13.18.38.3 SR20 places it BEFORE the table ("shall not occupy a byte position
    /// within the range of the first byte position defined by the data description entry containing the OCCURS
    /// clause and the last byte position defined by the record description entry") and SR22 lets nothing but the
    /// table's own subordinates follow the table — so the two counts were already compared, as elementary items, and
    /// found equal before the first occurrence is reached. The occurrence is therefore present in both or in
    /// neither, and "neither" ends the comparison (nothing follows the table).</para></summary>
    private bool WalkTable(DataItem table, PlaceCursor cur, PlaceCursor root, BoundCondition? present,
        List<(Place Leaf, BoundCondition? Present)> leaves, ref string? unbuilt)
    {
        int max = table.Occurs ?? table.OccursSpec?.Max ?? 0, always = max;
        Place? count = null;
        if (table.OccursSpec is { Depending: { } dep } odo)
        {
            always = odo.Min;
            count = CountPlace(dep, root);
            if (count is null)
            {
                unbuilt = $"the OCCURS DEPENDING ON object of '{table.CobolName ?? "FILLER"}'";
                return false;
            }
        }
        for (int k = 1; k <= max; k++)
        {
            BoundCondition? here = present;
            if (k > always)
            {
                // ISO §13.18.38.4 GR8 — occurrence k takes part iff data-name-1 ≥ k.
                var exists = new BoundRelational(new BoundFieldOperand(count!), ">=", new BoundNumericLiteral(k.ToString()));
                here = present is null ? exists : new BoundLogical("&&", [present, exists]);
            }
            PlaceCursor occurrence = cur.Indexed(new PositionConstant(k));
            bool ok = table.IsGroup ? Walk(occurrence, root, here, leaves, ref unbuilt)
                : Add(leaves, occurrence.ToPlace(), here);
            if (!ok) return false;
        }
        return true;
    }

    /// <summary>The place of data-name-1 as the operand rooted at <paramref name="root"/> holds it: an item inside
    /// the group is reached through the group's own cursor (so a subscripted group's subscripts stay on the path);
    /// one outside it is the item itself. Null when the item sits under an OCCURS within the group.</summary>
    private Place? CountPlace(DataItem dep, PlaceCursor root)
    {
        if (!OdoModel.IsWithin(dep, root.Item)) return ctx.Refs.ResolveItem(dep);
        var chain = new List<DataItem>();
        for (DataItem? n = dep; n is not null && !ReferenceEquals(n, root.Item); n = n.Parent) chain.Add(n);
        chain.Reverse();
        PlaceCursor? cur = root;
        foreach (var step in chain)
        {
            if (step.IsTable || cur is null) return null;
            cur = cur.Child(step);
        }
        return cur?.ToPlace();
    }

    /// <summary>The subordinate entries that are elementary items or groups of STORAGE, in declaration order.
    /// Condition-names and level-66 entries describe no storage of their own. ⚠ DETERMINATION: an entry with a
    /// REDEFINES clause (and its subordinates) is not a separate elementary item of the comparison — it describes
    /// the storage of the entry it redefines, which is already compared at that entry's position (the same
    /// exclusion §8.5.1.12.1 makes for group compatibility, "any subordinate data items that specify the
    /// REDEFINES clause … are ignored", and §14.9.20.4 GR5 a) 3) for INITIALIZE).</summary>
    private static List<DataItem> StorageChildren(DataItem group) =>
        [.. group.Children.Where(c => (c.IsGroup || c.IsElementary) && c.Renames is null && c.RedefinesTargetName is null)];

    /// <summary>"Two strongly-typed group operands are determined to be equal if all pairs of corresponding
    /// elementary items are equal." A pair that is absent from both operands is not a pair.</summary>
    private static BoundCondition AllEqual(List<ElementPair> pairs) => AllEqual(pairs, 0, pairs.Count);

    /// <summary>Every pair in [<paramref name="from"/>, <paramref name="to"/>) equal — ONE flat conjunction.</summary>
    private static BoundCondition AllEqual(List<ElementPair> pairs, int from, int to) =>
        new BoundLogical("&&", [.. pairs.Skip(from).Take(to - from).Select(p => Guarded(p, Relation(p, "=="), whenAbsent: true))]);

    /// <summary>The lexicographic order over <paramref name="pairs"/> with the strict per-pair operator
    /// <paramref name="strict"/> (see the type's summary); <paramref name="allEqualAnswer"/> is the relation's
    /// answer when every pair is equal — true for <c>&lt;=</c> / <c>&gt;=</c> (<c>strict ∨ all-equal</c>), false for
    /// <c>&lt;</c> / <c>&gt;</c>.</summary>
    private static BoundCondition Lexicographic(List<ElementPair> pairs, string strict, bool allEqualAnswer)
    {
        if (pairs.Count == 0) return Constant(allEqualAnswer);
        BoundCondition before = Before(pairs, 0, pairs.Count, strict);
        return allEqualAnswer ? new BoundLogical("||", [before, AllEqual(pairs)]) : before;
    }

    /// <summary>"The first unequal pair in [<paramref name="from"/>, <paramref name="to"/>) is ordered by
    /// <paramref name="strict"/>" — false when every pair there is equal.
    /// <para>⛔ SPLIT IN HALVES, NOT CHAINED PAIR BY PAIR. The chain <c>a₁ &lt; b₁ ∨ (a₁ = b₁ ∧ (a₂ &lt; b₂ ∨ …))</c>
    /// nests one level per elementary item, and a strong group's OCCURS table makes that count arbitrary: at 1000
    /// leaves the recursion over the nested condition overflowed the COMPILER's stack (measured on the PB1469
    /// build). The halves are the same order — <c>before(L ++ R) = before(L) ∨ (equal(L) ∧ before(R))</c> — and
    /// nest only log₂ n deep; <c>equal(L)</c> is one flat conjunction. Evaluation stays short-circuit: the right half
    /// is read only when the left half is wholly equal ("proceeds until a pair of elementary items is
    /// unequal").</para></summary>
    private static BoundCondition Before(List<ElementPair> pairs, int from, int to, string strict)
    {
        if (to - from == 1)
        {
            var p = pairs[from];
            // An absent occurrence is neither ordered nor — below — unequal: the comparison has ended equal there,
            // and every later pair is absent with it (WalkTable).
            return Guarded(p, Relation(p, strict), whenAbsent: false);
        }
        int mid = from + (to - from) / 2;
        return new BoundLogical("||", [Before(pairs, from, mid, strict),
            new BoundLogical("&&", [AllEqual(pairs, from, mid), Before(pairs, mid, to, strict)])]);
    }

    /// <summary><paramref name="step"/> when the pair is present; <paramref name="whenAbsent"/> otherwise.</summary>
    private static BoundCondition Guarded(ElementPair p, BoundCondition step, bool whenAbsent) =>
        p.Present is not { } present ? step
        : whenAbsent ? new BoundLogical("||", [new BoundNot(present), step])
        : new BoundLogical("&&", [present, step]);

    /// <summary>The comparison of one corresponding pair "in accordance with the rules for comparison of elementary
    /// items" — the relation between the two leaves, rendered by the same rules as any relation written between
    /// them.</summary>
    private static BoundRelational Relation(ElementPair p, string op) =>
        new(new BoundFieldOperand(p.Left), op, new BoundFieldOperand(p.Right));

    /// <summary>A constant condition — an empty conjunction is TRUE (the EVALUATE ANY convention of
    /// <see cref="BoundLogical"/>), its negation FALSE.</summary>
    private static BoundCondition Constant(bool value) =>
        value ? new BoundLogical("&&", []) : new BoundNot(new BoundLogical("&&", []));
}
