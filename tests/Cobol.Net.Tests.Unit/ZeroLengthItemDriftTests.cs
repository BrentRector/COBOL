// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §8.5.4's NINE SHAPES, EACH ASKED OF THE ONE STRUCTURE THAT ANSWERS THEM
/// (<see cref="DataItem.MinimumLengthIsZero"/>; kb/Work PB896).
/// <para>The clause's stem is the rule — "A zero-length item is a data item or a literal whose minimum length is
/// zero and whose length at runtime is zero" — and its enumeration is nine consequences of it, not nine separate
/// tests. That is why the compiler holds the recurrence rather than the bullets; this file is the other half of
/// that bargain (CLAUDE.md rule 5 — the structure pairs with a drift test so "automatic" stays true), asking
/// each enumerated GROUP shape of the structure and asking the ordinary shapes too, so a change to the
/// recurrence that quietly widens or narrows it fails here.</para>
/// </summary>
public sealed class ZeroLengthItemDriftTests
{
    private static PicInfo AlnumX(int n) =>
        new(PicCategory.Alphanumeric, Usage.Display, Length: n, Digits: 0, Scale: 0, Signed: false);

    private static DataItem Elem(string name, int width = 1, OccursSpec? occurs = null, int? fixedOccurs = null) =>
        new()
        {
            Level = 5, CobolName = name, CsName = name, Pic = AlnumX(width),
            OccursSpec = occurs, Occurs = fixedOccurs,
        };

    private static DataItem Group(string name, params DataItem[] children)
    {
        var g = new DataItem { Level = 1, CobolName = name, CsName = name };
        foreach (var c in children) g.AddMember(c);
        return g;
    }

    private static OccursSpec Odo(int min, int max) =>
        new() { Min = min, Max = max, DependingName = "N" };

    /// <summary>§8.5.4 item 1 — "A group data item containing only an occurs-depending table in which the number
    /// of occurrences is zero." integer-1 of the OCCURS is what makes the MINIMUM zero (§13.18.38 SR16 permits
    /// it); the run-time half is the length test the MOVE emitter writes.</summary>
    [Fact]
    public void Item1_OccursDependingGroupWithZeroMinimum_HasZeroMinimumLength() =>
        Assert.True(Group("ZG", Elem("ZE", occurs: Odo(0, 5))).MinimumLengthIsZero);

    /// <summary>The discriminator for item 1: integer-1 ONE is not zero, so the same group is never a
    /// zero-length item and §14.9.25.4 GR1's antecedent can never be satisfied.</summary>
    [Fact]
    public void Item1_OccursDependingGroupWithNonZeroMinimum_DoesNot() =>
        Assert.False(Group("ZG", Elem("ZE", occurs: Odo(1, 5))).MinimumLengthIsZero);

    /// <summary>§8.5.4 item 2 — "A group data item containing only a subordinate zero-length item", here a
    /// DYNAMIC LENGTH member (item 4), and NESTED, because the rule is a recurrence.</summary>
    [Fact]
    public void Item2_GroupOverAZeroLengthMember_HasZeroMinimumLength()
    {
        var dyn = Elem("D");
        dyn.IsDynamicLength = true;
        Assert.True(Group("G", dyn).MinimumLengthIsZero);
        Assert.True(Group("OUTER", Group("G", dyn)).MinimumLengthIsZero);
    }

    /// <summary>§8.5.4 item 7 — "A variable-length group containing only dynamic-capacity tables each of whose
    /// current capacity is zero": a Format-4 table's minimum occurrence count is zero (§8.5.1.9).</summary>
    [Fact]
    public void Item7_DynamicCapacityTableGroup_HasZeroMinimumLength() =>
        Assert.True(Group("VLG",
            Elem("VE", occurs: new OccursSpec { Min = 0, Max = 0, IsDynamic = true })).MinimumLengthIsZero);

    /// <summary>§8.5.4 items 3 and 4 as ELEMENTARY items, which is the form the MOVE route reads directly.</summary>
    [Fact]
    public void Items3And4_AnyLengthAndDynamicLengthElementaryItems_HaveZeroMinimumLength()
    {
        var any = Elem("A", width: 4);
        any.IsAnyLength = true;
        var dyn = Elem("D", width: 4);
        dyn.IsDynamicLength = true;
        Assert.True(any.MinimumLengthIsZero);
        Assert.True(dyn.MinimumLengthIsZero);
    }

    /// <summary>⛔ AND THE ORDINARY SHAPES MUST ANSWER NO. §8.5.4 is an enumeration of exceptions; a recurrence
    /// that answered "yes" for a plain item or a fixed table would put every MOVE through GR1's run-time test
    /// and, worse, substitute SPACE for a sender that is never zero-length.</summary>
    [Fact]
    public void OrdinaryShapes_AreNotZeroLengthItems()
    {
        Assert.False(Elem("X", width: 4).MinimumLengthIsZero);
        Assert.False(Group("G",
            Elem("T", width: 2, occurs: new OccursSpec { Min = 3, Max = 3 }, fixedOccurs: 3)).MinimumLengthIsZero);
        // A group with ONE zero-length member and one ordinary member is not zero-length: §8.5.4 items 1, 2 and
        // 7 all say "containing ONLY".
        Assert.False(Group("MIXED", Elem("ZE", occurs: Odo(0, 5)), Elem("P", width: 2)).MinimumLengthIsZero);
    }
}
