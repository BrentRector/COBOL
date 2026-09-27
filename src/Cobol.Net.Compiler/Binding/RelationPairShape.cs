// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>⛔ WHICH §8.8.4.2 COMPARISON A RELATION'S OPERAND PAIR SELECTS, decided ONCE, from BOTH operands, for
/// every relation surface (IF, EVALUATE pairings and ranges, PERFORM UNTIL, SEARCH WHEN, abbreviated relations) —
/// the question kb/Work PB1469 / PB1467 found answered nowhere: every group pair used to be emitted as a
/// whole-group character IMAGE comparison, which is the rule for exactly ONE of the three shapes below.</summary>
internal enum RelationPairShape
{
    /// <summary>Each operand compares as ONE item — an elementary item under its own class rules, a fixed-length
    /// group as ISO §8.8.4.2.1 says: "For comparison, an alphanumeric group item shall be treated as an elementary
    /// alphanumeric data item" — so a (weakly-typed, fixed-length) group's character image is its operand value.</summary>
    Flat,

    /// <summary>Two strongly-typed group items of the same type (§8.8.4.2.3 SR1): ISO §8.8.4.2.12 — "each
    /// elementary item of the first operand is compared with the corresponding elementary item of the second
    /// operand, in accordance with the rules for comparison of elementary items and in the order in which the
    /// elementary items are specified". IMAGE order is not element order for a signed, float, national-collated or
    /// pointer leaf, so this pair is lowered at bind to its element comparisons
    /// (<c>StrongGroupComparison</c>).</summary>
    StrongElementOrder,

    /// <summary>A pair one or both of whose operands is a variable-length group (§8.5.1.12.1 — "a group item whose
    /// data description has at least one dynamic-length elementary item or dynamic-capacity table as a
    /// subordinate item"), which "is not equivalent to an alphanumeric data item and may not undergo a comparison
    /// … unless the other operand is a compatible group"; a compatible pair compares by §8.8.4.2.17.</summary>
    VariableLengthGroup,
}

/// <summary>The ONE classifier of a relation's operand pair (<see cref="RelationPairShape"/>). Asked by the relation
/// checkpoint's screens (<c>StatementValidation.CheckRelationalOperands</c>) and by the relation's construction
/// (<c>ConditionBinder.CheckedRelational</c>), so what is screened and what is emitted cannot disagree about which
/// comparison the pair is.</summary>
internal static class RelationPair
{
    /// <summary>The shape of the pair (<paramref name="left"/>, <paramref name="right"/>).
    /// <para>A strong pair is classified FIRST: a strongly-typed group may itself be variable-length (a DYNAMIC
    /// LENGTH leaf, §13.18.58.3 places no restriction on what a strong type holds), and §8.8.4.2.12 is the rule
    /// written for exactly that pair — its elementary items compare one by one, a dynamic-length leaf at its
    /// current length (§8.5.1.10.4), so the element order carries it without a §8.8.4.2.17 walk.</para></summary>
    public static RelationPairShape Classify(BoundOperand left, BoundOperand right)
    {
        DataItem? l = GroupItemOf(left), r = GroupItemOf(right);
        if (l is not null && r is not null && StrongTypeModel.IsStrongGroup(l) && StrongTypeModel.IsStrongGroup(r)
            && StrongTypeModel.SameType(l, r))
            return RelationPairShape.StrongElementOrder;
        if ((l is not null && VariableLengthCompatibility.IsVariableLength(l))
            || (r is not null && VariableLengthCompatibility.IsVariableLength(r)))
            return RelationPairShape.VariableLengthGroup;
        return RelationPairShape.Flat;
    }

    /// <summary>The group ITEM an operand denotes, or null. ⛔ The identity question is
    /// <see cref="Place.DenotedItem"/>'s (kb/Work PB602): a reference-modified group is "a unique data item" of
    /// class alphanumeric (§8.4.3.3.4 GR5/GR6), not the group, and its place denotes no declared item. The group
    /// question is the CATEGORY one (<see cref="ItemCategory.IsGroupItem"/>), which a level-66 THROUGH alias also
    /// answers.</summary>
    public static DataItem? GroupItemOf(BoundOperand operand) =>
        operand is BoundFieldOperand { Place.DenotedItem: { } item } && ItemCategory.IsGroupItem(item) ? item : null;
}
