// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO CONFORMANCE RULES OVER A BOUNDARY ITEM'S DESCRIPTION (kb/Work PB165), each stated from the clause that gives
/// it: <see cref="BoundaryItem.ArgumentConforms"/> for an argument and its formal at a dynamic Format-1 CALL
/// (ISO §14.8.2.3.2 / §14.8.2.3.3 rule 1 "the formal parameter shall be of the same length as the corresponding argument";
/// §14.8.2.2 1) the group rule BY REFERENCE, 2) the MOVE rule BY CONTENT) and <see cref="BoundaryItem.Conforms"/> for a
/// RETURNING pair (§14.8.3.2). The corpus golden <c>2002/pb165_dynamic_call_description_check</c> witnesses the same
/// table end to end; this pins each row of it where a regression names it.
/// </summary>
public sealed class BoundaryItemConformanceTests
{
    private static BoundaryItem Elem(int length, BoundaryClass cls = BoundaryClass.Other) => new(null, length, cls);
    private static BoundaryItem Group(int length) => new(null, length, BoundaryClass.Group);

    [Theory]
    [InlineData(4, 4, true)]    // same length
    [InlineData(4, 6, false)]   // §14.8.2.3.2 rule 1: the formal is longer
    [InlineData(6, 4, false)]   // … and shorter
    public void Elementary_SameLengthOnly_ByReferenceAndByContent(int argument, int formal, bool conforms)
    {
        foreach (var mode in new[] { CobolPassMode.Reference, CobolPassMode.Content, CobolPassMode.Value })
            Assert.Equal(conforms, Elem(argument).ArgumentConforms(mode, Elem(formal)));
    }

    [Fact]
    public void Elementary_AnItemThatStatesNothingIsNeverCompared()
    {
        var unstated = new BoundaryItem(null);
        Assert.True(Elem(4).ArgumentConforms(CobolPassMode.Reference, unstated));
        Assert.True(unstated.ArgumentConforms(CobolPassMode.Reference, Elem(4)));
        Assert.True(new BoundaryItem(null, CobolArg.Unstated, BoundaryClass.Alphanumeric)
            .ArgumentConforms(CobolPassMode.Reference, Elem(4)));   // a reference-modified operand: a class, no length
    }

    [Theory]
    [InlineData(8, 10, false)]  // §14.8.2.2 1): the formal is described with MORE bytes than the argument
    [InlineData(10, 8, true)]   // … a smaller number is allowed
    [InlineData(8, 8, true)]    // … and so is the same number
    public void Group_ByReference_FormalNoLongerThanArgument(int argument, int formal, bool conforms) =>
        Assert.Equal(conforms, Group(argument).ArgumentConforms(CobolPassMode.Reference, Group(formal)));

    [Fact]
    public void Group_ByReference_TheOtherItemIsAGroupOrAnAlphanumericElementaryItem()
    {
        Assert.True(Elem(8, BoundaryClass.Alphanumeric).ArgumentConforms(CobolPassMode.Reference, Group(8)));
        Assert.True(Group(8).ArgumentConforms(CobolPassMode.Reference, Elem(8, BoundaryClass.Alphanumeric)));
        Assert.False(Elem(8).ArgumentConforms(CobolPassMode.Reference, Group(8)));   // a numeric / edited / national item
        Assert.False(Group(8).ArgumentConforms(CobolPassMode.Reference, Elem(8)));
    }

    [Fact]
    public void Group_ByContent_IsAMoveAndRelatesNoLengths()
    {
        Assert.True(Group(8).ArgumentConforms(CobolPassMode.Content, Group(10)));
        Assert.True(Group(8).ArgumentConforms(CobolPassMode.Value, Elem(3)));
    }

    [Fact]
    public void Exempt_StronglyTypedAndVariableLengthGroupsAreNotComparedHere()
    {
        var exempt = Elem(8, BoundaryClass.Exempt);
        Assert.True(exempt.ArgumentConforms(CobolPassMode.Reference, Group(2)));
        Assert.True(Group(2).ArgumentConforms(CobolPassMode.Reference, exempt));
        Assert.True(exempt.Conforms(Elem(8)));    // RETURNING: the group-class rule lifts, the length comparison stays
        Assert.False(exempt.Conforms(Elem(3)));
    }

    /// <summary>§14.8.3.2: an alphanumeric group pairs with a group or an elementary alphanumeric item of the same length —
    /// the RETURNING rule the group class completes (it used to compare lengths alone, so a group "conformed" to a
    /// numeric-edited receiver of the same length).</summary>
    [Fact]
    public void Returning_AlphanumericGroupPairsOnlyWithGroupOrAlphanumeric_OfTheSameLength()
    {
        Assert.True(Group(8).Conforms(Group(8)));
        Assert.True(Group(8).Conforms(Elem(8, BoundaryClass.Alphanumeric)));
        Assert.True(Elem(8, BoundaryClass.Alphanumeric).Conforms(Group(8)));
        Assert.False(Group(8).Conforms(Group(10)));
        Assert.False(Group(8).Conforms(Elem(8)));
        Assert.False(Elem(8).Conforms(Group(8)));
    }
}
