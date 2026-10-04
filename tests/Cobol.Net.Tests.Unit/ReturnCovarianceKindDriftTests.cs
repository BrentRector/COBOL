// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY OBJECT-REFERENCE RETURN KIND ISO §9.3.8.2.3 RULE 5 a) ADMITS HAS A LEG ON BOTH PATHS THAT NEED THE C#
/// CONVERSION (kb/Work PB1499). Where interface-2's RETURNING item is universal, interface-1's may be any object
/// reference, and interface-1 is a class IMPLEMENTING the prototype (the covariant-return adapter) and a method
/// OVERRIDING the overridden one (§11.7.3 SR9). C# spells the two differently — an explicit interface implementation
/// over the adapter, an <c>override</c> keeping the overridden method's return type — and an INTERFACE-typed value
/// converts to the universal class only by a cast, so a kind with no leg is a kind whose conversion nobody has seen
/// compile. The theory is exhaustive over <see cref="ObjectRefKind"/> (the switch throws for a kind it does not name, so a
/// new kind fails here until it has a leg) and requires the golden to declare a RETURNING item of that kind in an IMPLEMENTS leg
/// (<c>METHOD-ID. GETIT</c>) and in an OVERRIDE leg (<c>METHOD-ID. MAKE OVERRIDE</c>).
/// </summary>
public sealed class ReturnCovarianceKindDriftTests
{
    private const string Golden = "pb1499_interface_return_covariance.cob";

    /// <summary>The LINKAGE declaration of a RETURNING item of each kind, as the golden writes it.</summary>
    private static string Declaration(ObjectRefKind kind) => kind switch
    {
        ObjectRefKind.Universal => @"USAGE OBJECT REFERENCE\.",
        ObjectRefKind.ObjectClass => @"USAGE OBJECT REFERENCE KCV\.",
        ObjectRefKind.Interface => @"USAGE OBJECT REFERENCE IKCV\.",
        ObjectRefKind.ActiveClass => @"USAGE OBJECT REFERENCE ACTIVE-CLASS\.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a new object-reference kind needs a leg in the golden"),
    };

    public static TheoryData<ObjectRefKind> Kinds()
    {
        var data = new TheoryData<ObjectRefKind>();
        foreach (var kind in Enum.GetValues<ObjectRefKind>()) data.Add(kind);
        return data;
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryKind_HasAnImplementsLeg_AndAnOverrideLeg(ObjectRefKind kind)
    {
        string text = File.ReadAllText(TestRepo.Tests("conformance", "2002", Golden));
        string decl = Declaration(kind);
        Assert.Matches(new Regex(@"METHOD-ID\. GETIT\.\s+DATA DIVISION\.\s+LINKAGE SECTION\.\s+01 R " + decl), text);
        Assert.Matches(new Regex(@"METHOD-ID\. MAKE OVERRIDE\.\s+DATA DIVISION\.\s+LINKAGE SECTION\.\s+01 R " + decl), text);
    }

    [Fact]
    public void OnlyAnInterfaceKind_NeedsTheExplicitConversionToTheUniversalType()
    {
        // The emitter's conversion test (OoEmitter.OoReturnClrType) is ObjectRefDescriptor.IsClrInterface; a kind that
        // C# cannot convert to the universal class implicitly must answer true, and the class-shaped kinds false.
        foreach (var kind in Enum.GetValues<ObjectRefKind>())
        {
            var descriptor = new ObjectRefDescriptor(kind, kind is ObjectRefKind.Universal ? null : "X", false, false);
            Assert.Equal(kind is ObjectRefKind.Interface, descriptor.IsClrInterface);
        }
    }
}
