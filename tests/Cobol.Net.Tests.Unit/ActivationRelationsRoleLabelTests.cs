// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// An EC-OO-UNIVERSAL message names each operand of a §14.8.2.2 / §14.8.3.2 variable-length violation by its own role
/// (kb/Work PB480, train 1021 review D finding 6): the description printed after "the argument" is the argument's and
/// the one after "the formal parameter" the formal's, and likewise for the returning item (the method's, the sending
/// operand, §14.8.3.1) and the receiving item (the invocation's). The two calls into <c>VariableLengthViolation</c>
/// passed the role labels in the opposite order to the descriptions, so each message named one operand as the other.
/// </summary>
public sealed class ActivationRelationsRoleLabelTests
{
    /// <summary>A variable-length group of <paramref name="lead"/> characters followed by a dynamic-length item: two of
    /// different leads are incompatible (§8.5.1.12.2 — the dynamic-length items start at different positions).</summary>
    private static ActivationDescription Var(int lead) => new()
    {
        Shape = ActivationShape.VariableLengthGroup, Category = ActivationCategory.Alphanumeric,
        Atoms = [new GroupAtom(GroupAtomKind.Fixed, lead, lead), new GroupAtom(GroupAtomKind.DynamicLength, 0, 0)],
    };

    [Fact]
    public void ParameterViolation_NamesTheArgumentAndTheFormalByTheirOwnDescriptions()
    {
        string? reason = ActivationRelations.ParameterViolation(Var(2), Var(3));
        Assert.NotNull(reason);
        Assert.Contains("the argument (a variable-length group (2,D))", reason);
        Assert.Contains("the formal parameter (a variable-length group (3,D))", reason);
    }

    [Fact]
    public void ReturningViolation_NamesTheReturningAndTheReceivingItemByTheirOwnDescriptions()
    {
        string? reason = ActivationRelations.ReturningViolation(receiving: Var(4), sending: Var(5));
        Assert.NotNull(reason);
        Assert.Contains("the returning item (a variable-length group (5,D))", reason);
        Assert.Contains("the receiving item (a variable-length group (4,D))", reason);
    }
}
