// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1497 (+ PB1112's override leg) — the ACTIVE-CLASS object-reference DESCRIPTION is owner-less.
/// ISO §9.3.8.2.3 rule 2 d) (cite.py --check 9.3.8.2.3 "If the parameter in interface-2 is described with the
/// ACTIVE-CLASS phrase, the corresponding parameter in interface-1 is described with the ACTIVE-CLASS phrase" → OK
/// 9.3.8.2.3 2)) asks only for the phrase on both sides and the same FACTORY presence; the class the entry is written
/// in (§13.18.60.3 SR16) is no part of it — GR22 e) binds the class at activation.
/// </summary>
public sealed class ObjectRefActiveClassDescriptionTests
{
    [Fact]
    public void TwoActiveClassDescriptions_AgreeWhateverClassesTheyWereWrittenIn()
    {
        var inA = ObjectRefDescriptor.ActiveClass("A");
        var inB = ObjectRefDescriptor.ActiveClass("B");
        var ownerless = ObjectRefDescriptor.ActiveClass(null);   // an interface method prototype's formal
        Assert.True(inA.SameDescriptionAs(inB));
        Assert.True(inA.SameDescriptionAs(ownerless));
        Assert.True(ownerless.SameDescriptionAs(inB));
    }

    [Fact]
    public void TheFactoryPresence_StaysPartOfTheDescription()
    {
        Assert.False(ObjectRefDescriptor.ActiveClass("A", factory: true).SameDescriptionAs(ObjectRefDescriptor.ActiveClass("A")));
        Assert.True(ObjectRefDescriptor.ActiveClass(null, factory: true).SameDescriptionAs(ObjectRefDescriptor.ActiveClass("B", factory: true)));
    }

    [Fact]
    public void AnObjectClassDescription_StillComparesItsName()
    {
        Assert.True(ObjectRefDescriptor.ObjectClass("A").SameDescriptionAs(ObjectRefDescriptor.ObjectClass("a")));
        Assert.False(ObjectRefDescriptor.ObjectClass("A").SameDescriptionAs(ObjectRefDescriptor.ObjectClass("B")));
        Assert.False(ObjectRefDescriptor.ActiveClass("A").SameDescriptionAs(ObjectRefDescriptor.ObjectClass("A")));
    }

    [Fact]
    public void AnOwnerlessDescription_ProjectsAndSpellsWithoutAClass()
    {
        var ownerless = ObjectRefDescriptor.ActiveClass(null);
        Assert.Equal("CobolObject", ownerless.ClrTypeName);
        Assert.Contains("interface method prototype", ownerless.Spelled);
        Assert.Equal("OBJECT REFERENCE ACTIVE-CLASS", ownerless.ToString());
    }
}
