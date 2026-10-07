// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY FACT AN ACTIVATION DESCRIPTION CARRIES IS ASKED BY A RELATION (kb/Work PB480 / PB1576). A UNIVERSAL INVOKE
/// carries each operand's <see cref="ActivationDescription"/> to the generated dispatch switch, which asks the standard's
/// two relations of it (<see cref="ActivationRelations"/>: ISO §9.3.6 MATCH, §14.8.2 / §14.8.3 CONFORMANCE) and converts
/// a group it admitted in another shape (<see cref="UniversalGroupCarrier"/>). A field that is built and carried but
/// read by neither is a rule the relations silently skip — the shape of the string descriptor this replaced, which
/// carried JUSTIFIED but no relation honoured the group prefix. So each public field must be read in the relations'
/// or the carrier's source, and the compiler's one renderer (<c>RuntimeApi.ActivationDescriptionNew</c>, reflection over
/// the same properties) spells every one of them.
/// </summary>
public sealed class ActivationDescriptionFieldDriftTests
{
    [Fact]
    public void EveryDescriptionField_IsReadByARelationOrTheCarrier()
    {
        string relations = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Runtime", "Control", "ActivationRelations.cs"));
        string carrier = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Runtime", "Control", "CobolInvokeArg.cs"));
        var props = typeof(ActivationDescription).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.True(props.Length >= 15, $"only {props.Length} properties — the reflection no longer sees the description");
        foreach (var p in props)
            Assert.True(relations.Contains("." + p.Name, StringComparison.Ordinal)
                        || carrier.Contains("." + p.Name, StringComparison.Ordinal),
                $"ActivationDescription.{p.Name} is carried across a universal INVOKE but no relation or carrier reads it");
    }

    [Fact]
    public void TheRenderer_SpellsEveryNonDefaultField()
    {
        var d = new ActivationDescription
        {
            Shape = ActivationShape.ObjectReference, Category = "c", Clauses = "k", LocaleExternal = "en-US",
            LocaleFromLiteral = true, AnyLength = true, Positions = 7, Usage = "u", StrongType = "s",
            Table16 = Table16Category.NumericNoninteger, BinaryWidth = true,
            Atoms = [new GroupAtom(GroupAtomKind.Fixed, 7, 7)], ObjectKind = ObjectReferenceKind.ObjectClass, ObjectName = "C", Factory = true,
            Only = true, Optional = true, ByValue = true,
        };
        string text = CobolNet.CodeGen.RuntimeApi.ActivationDescriptionNew(d);
        foreach (var p in typeof(ActivationDescription).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Assert.Contains(p.Name + " = ", text);
    }
}
