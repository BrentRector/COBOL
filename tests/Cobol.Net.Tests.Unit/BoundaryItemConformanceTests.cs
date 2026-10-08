// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE CONFORMANCE RULES A CALL ASKS AT RUN TIME OVER A BOUNDARY ITEM'S DESCRIPTION (kb/Work PB165), each stated from
/// the clause that gives it: <see cref="BoundaryItem.ArgumentViolation"/> for an argument and its formal when the activated
/// program is located by name at run time (ISO §14.9.4.4 GR3 d) — §14.8.2.3.2 / §14.8.2.3.3 rule 1 "the formal parameter
/// shall be of the same length as the corresponding argument" for a program the activating element has no
/// program-specifier for, rule 2 for one it has; §14.8.2.2's group rules, the object-reference and class-pointer rules on
/// both lanes) and <see cref="BoundaryItem.ReturningViolation"/> for a RETURNING pair (§14.8.3). The corpus goldens
/// <c>2002/pb165_dynamic_call_description_check</c> (rule 1) and <c>2002/pb165_repository_prototype_description_check</c>
/// (rule 2) witness the same tables end to end; this pins each row where a regression names it.
/// </summary>
public sealed class BoundaryItemConformanceTests
{
    private const bool Rule1 = false, Rule2 = true;

    private static readonly CobolPassMode[] AllModes = [CobolPassMode.Reference, CobolPassMode.Content, CobolPassMode.Value];

    private static BoundaryItem Item(ActivationDescription d, int length = CobolArg.Unstated) => new(null, length, d);

    private static ActivationDescription Alnum(int n) => new()
    {
        Shape = ActivationShape.Elementary, Category = ActivationCategory.Alphanumeric, Clauses = $"PICTURE X({n})",
        Positions = n, Table16 = Table16Category.Alphanumeric,
    };

    private static ActivationDescription Numeric(int digits, bool noninteger = false) => new()
    {
        Shape = ActivationShape.Elementary, Category = ActivationCategory.Numeric,
        Clauses = noninteger ? $"PICTURE 9({digits})V9" : $"PICTURE 9({digits})", Positions = digits,
        Table16 = noninteger ? Table16Category.NumericNoninteger : Table16Category.NumericInteger,
    };

    private static ActivationDescription Group(int n) => new()
    {
        Shape = ActivationShape.AlphanumericGroup, Category = ActivationCategory.Alphanumeric, Positions = n,
    };

    private static ActivationDescription Strong(string type) => new()
    {
        Shape = ActivationShape.StrongGroup, Category = "type:" + type, StrongType = type, Positions = 8,
    };

    private static ActivationDescription Pointer(string category, string restriction = ActivationDescription.Unrestricted) =>
        new() { Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = restriction };

    private static ActivationDescription Literal(string category, Table16Category row) =>
        new() { Shape = ActivationShape.Literal, Category = category, Table16 = row };

    // ── rule 1: the dynamic lane ──

    [Theory]
    [InlineData(4, 4, true)]    // same length
    [InlineData(4, 6, false)]   // §14.8.2.3.2 rule 1: the formal is longer
    [InlineData(6, 4, false)]   // … and shorter
    public void Rule1_Elementary_SameLengthOnly_InEveryMode(int argument, int formal, bool conforms)
    {
        foreach (var mode in AllModes)
            Assert.Equal(conforms, Item(Alnum(argument), argument).ArgumentViolation(mode, Item(Alnum(formal), formal), Rule1) is null);
        // A numeric item of the formal's length conforms: rule 1 compares lengths and nothing else.
        Assert.Null(Item(Numeric(4), 4).ArgumentViolation(CobolPassMode.Reference, Item(Alnum(4), 4), Rule1));
    }

    [Fact]
    public void Rule1_AnItemThatStatesNoLengthIsNotComparedByLength()
    {
        var unstated = new BoundaryItem(null);
        Assert.Null(Item(Alnum(4), 4).ArgumentViolation(CobolPassMode.Reference, unstated, Rule1));
        Assert.Null(unstated.ArgumentViolation(CobolPassMode.Reference, Item(Alnum(4), 4), Rule1));
    }

    [Theory]
    [InlineData(8, 10, false)]  // §14.8.2.2 1): the formal is described with MORE bytes than the argument
    [InlineData(10, 8, true)]   // … a smaller number is allowed
    [InlineData(8, 8, true)]    // … and so is the same number
    public void Group_ByReference_FormalNoLongerThanArgument_OnBothLanes(int argument, int formal, bool conforms)
    {
        foreach (bool lane in new[] { Rule1, Rule2 })
            Assert.Equal(conforms, Item(Group(argument), argument)
                .ArgumentViolation(CobolPassMode.Reference, Item(Group(formal), formal), lane) is null);
    }

    [Fact]
    public void Group_ByReference_TheOtherItemIsAGroupOrAnAlphanumericElementaryItem()
    {
        Assert.Null(Item(Alnum(8), 8).ArgumentViolation(CobolPassMode.Reference, Item(Group(8), 8), Rule1));
        Assert.Null(Item(Group(8), 8).ArgumentViolation(CobolPassMode.Reference, Item(Alnum(8), 8), Rule1));
        Assert.NotNull(Item(Numeric(8), 8).ArgumentViolation(CobolPassMode.Reference, Item(Group(8), 8), Rule1));
        Assert.NotNull(Item(Group(8), 8).ArgumentViolation(CobolPassMode.Reference, Item(Numeric(8), 8), Rule1));
    }

    [Fact]
    public void Group_ByContent_IsAMoveAndRelatesNoLengths()
    {
        Assert.Null(Item(Group(8), 8).ArgumentViolation(CobolPassMode.Content, Item(Group(10), 10), Rule1));
        Assert.Null(Item(Group(8), 8).ArgumentViolation(CobolPassMode.Value, Item(Numeric(3), 3), Rule1));
        // An arithmetic expression has no character image for the group move to copy (§14.9.25.4 GR4).
        var expression = new ActivationDescription
        {
            Shape = ActivationShape.Expression, Category = ActivationCategory.Numeric, Table16 = Table16Category.NumericNoninteger,
        };
        Assert.NotNull(Item(expression).ArgumentViolation(CobolPassMode.Content, Item(Group(8), 8), Rule2));
    }

    /// <summary>§14.8.2.2: "If either the formal parameter or the corresponding argument is a strongly-typed group item,
    /// both shall be of the same type" — on BOTH lanes (the registry used to exempt the pair from every check).</summary>
    [Fact]
    public void StronglyTypedGroups_AreTheSameType_InEveryModeAndLane()
    {
        foreach (bool lane in new[] { Rule1, Rule2 })
            foreach (var mode in AllModes)
            {
                Assert.Null(Item(Strong("T1")).ArgumentViolation(mode, Item(Strong("T1")), lane));
                Assert.NotNull(Item(Strong("T1")).ArgumentViolation(mode, Item(Strong("T2")), lane));
                Assert.NotNull(Item(Group(8), 8).ArgumentViolation(mode, Item(Strong("T1")), lane));
            }
    }

    // ── rule 2: a program the activating element has a program-specifier for ──

    [Fact]
    public void Rule2_ByReference_IsTheClauseIdentity_NotTheLength()
    {
        Assert.Null(Item(Alnum(4), 4).ArgumentViolation(CobolPassMode.Reference, Item(Alnum(4), 4), Rule2));
        Assert.NotNull(Item(Alnum(4), 4).ArgumentViolation(CobolPassMode.Reference, Item(Numeric(4), 4), Rule2));
        Assert.NotNull(Item(Numeric(4), 4).ArgumentViolation(CobolPassMode.Reference, Item(Numeric(5), 5), Rule2));
    }

    [Fact]
    public void Rule2_ByContent_NumericFormal_IsACompute()
    {
        Assert.Null(Item(Numeric(6), 6).ArgumentViolation(CobolPassMode.Content, Item(Numeric(3), 3), Rule2));
        Assert.Null(Item(Literal(ActivationCategory.Numeric, Table16Category.NumericNoninteger))
            .ArgumentViolation(CobolPassMode.Value, Item(Numeric(3), 3), Rule2));
        Assert.Null(Item(Literal(ActivationCategory.FigurativeZero, Table16Category.None))
            .ArgumentViolation(CobolPassMode.Content, Item(Numeric(3), 3), Rule2));
        Assert.NotNull(Item(Alnum(4), 4).ArgumentViolation(CobolPassMode.Content, Item(Numeric(4), 4), Rule2));
        Assert.NotNull(Item(Literal(ActivationCategory.Figurative, Table16Category.None))
            .ArgumentViolation(CobolPassMode.Content, Item(Numeric(3), 3), Rule2));
    }

    [Fact]
    public void Rule2_ByContent_OtherFormal_IsAMove_ByTable16()
    {
        Assert.Null(Item(Alnum(4), 4).ArgumentViolation(CobolPassMode.Content, Item(Alnum(6), 6), Rule2));
        Assert.Null(Item(Numeric(4), 4).ArgumentViolation(CobolPassMode.Content, Item(Alnum(6), 6), Rule2));
        // Table 16: Numeric Noninteger -> Alphanumeric is "No".
        Assert.NotNull(Item(Numeric(4, noninteger: true), 5).ArgumentViolation(CobolPassMode.Content, Item(Alnum(6), 6), Rule2));
        Assert.Null(Item(Literal(ActivationCategory.Figurative, Table16Category.None))
            .ArgumentViolation(CobolPassMode.Content, Item(Alnum(6), 6), Rule2));
    }

    [Fact]
    public void ClassPointer_IsTheSameCategoryAndRestriction_AndTakesNullByContent()
    {
        var data = Pointer(ActivationCategory.DataPointer);
        Assert.Null(Item(data).ArgumentViolation(CobolPassMode.Reference, Item(data), Rule1));
        Assert.NotNull(Item(Pointer(ActivationCategory.ProgramPointer)).ArgumentViolation(CobolPassMode.Reference, Item(data), Rule1));
        Assert.NotNull(Item(Pointer(ActivationCategory.DataPointer, "TO T1")).ArgumentViolation(CobolPassMode.Reference, Item(data), Rule2));
        var nullValue = new ActivationDescription { Shape = ActivationShape.PredefinedNull };
        Assert.Null(Item(nullValue).ArgumentViolation(CobolPassMode.Content, Item(data), Rule2));
        Assert.NotNull(Item(nullValue).ArgumentViolation(CobolPassMode.Content, Item(Alnum(4), 4), Rule2));
        Assert.NotNull(Item(Alnum(8), 8).ArgumentViolation(CobolPassMode.Content, Item(data), Rule2));
    }

    [Fact]
    public void ObjectReference_ByReference_IsTheSameDescription()
    {
        var universal = new ActivationDescription
        {
            Shape = ActivationShape.ObjectReference, Category = ActivationCategory.Object, ObjectKind = ObjectReferenceKind.Universal,
        };
        var ofClass = universal with { ObjectKind = ObjectReferenceKind.ObjectClass, ObjectName = "ACCOUNT" };
        Assert.Null(Item(universal).ArgumentViolation(CobolPassMode.Reference, Item(universal), Rule1));
        Assert.NotNull(Item(ofClass).ArgumentViolation(CobolPassMode.Reference, Item(universal), Rule1));
        Assert.NotNull(Item(Alnum(8), 8).ArgumentViolation(CobolPassMode.Reference, Item(universal), Rule2));
    }

    // ── RETURNING: §14.8.3 has one rule for every lane ──

    /// <summary>§14.8.3.2: an alphanumeric group pairs with a group or an elementary alphanumeric item of the same length;
    /// §14.8.3.3: two elementary items have the same clauses.</summary>
    [Fact]
    public void Returning_GroupsAndClauses()
    {
        Assert.Null(Item(Group(8), 8).ReturningViolation(Item(Group(8), 8)));
        Assert.Null(Item(Group(8), 8).ReturningViolation(Item(Alnum(8), 8)));
        Assert.NotNull(Item(Group(8), 8).ReturningViolation(Item(Group(10), 10)));
        Assert.NotNull(Item(Group(8), 8).ReturningViolation(Item(Numeric(8), 8)));
        Assert.Null(Item(Numeric(4), 4).ReturningViolation(Item(Numeric(4), 4)));
        Assert.NotNull(Item(Alnum(4), 4).ReturningViolation(Item(Numeric(4), 4)));   // same length, other PICTURE
    }

    [Fact]
    public void Returning_WithoutDescriptions_ComparesWhatBothState()
    {
        Assert.Null(new BoundaryItem(null, 4).ReturningViolation(new BoundaryItem(null, 4)));
        Assert.NotNull(new BoundaryItem(null, 4).ReturningViolation(new BoundaryItem(null, 6)));
        Assert.Null(new BoundaryItem(null).ReturningViolation(new BoundaryItem(null, 6)));
    }
}
