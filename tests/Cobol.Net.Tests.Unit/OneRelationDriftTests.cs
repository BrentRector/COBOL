// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE CONFORMANCE QUESTION HAS ONE ASKER: THE RELATION OVER TWO DESCRIPTIONS (kb/Work PB3101; COBOLNET_OO_DESIGN D6
/// item 7). ISO §9.3.5.3 item 6 writes the description identity once (the eight clauses) and the standard asks it from five
/// places — the §9.3.6 match, §14.8.2.3.2 rule 2, §9.3.8.2.3 rule 3, §11.7.3 SR3 and §13.7.3 SR2. The compiler answered
/// them twice, in two currencies: a <c>DataItem</c> comparator family re-derived each rule from the items' fields while the
/// run-time relation (<c>ActivationRelations</c>) read the <c>ActivationDescription</c> the compiler builds from the same
/// fields, and the two disagreed exactly where one copy was corrected and the other was not (a reference-modified argument,
/// a group opposite a <c>PIC X(n)</c>, an ANY LENGTH clause). The comparators are deleted; every activation and pair-mode
/// question describes its items through <c>ActivationDescriptions</c> and asks the relation.
/// <para>So in the conformance layer — the OO conformance (<c>src/Cobol.Net.Compiler/Oo/</c>), the verb binders
/// (<c>Binding/Procedure/Verbs/</c>) and the prototype signatures (<c>Binding/PrototypeSignature*.cs</c>) — no member
/// compares two items' description fields against each other (<c>a.Usage != b.Usage</c>,
/// <c>Equals(a.Clause, b.Clause)</c>, …): that comparison belongs to the relation, and a second copy here is the shape
/// this note removed. The builder (<c>ActivationDescriptions.cs</c>) reads the fields of ONE item at a time and is the
/// one place they enter a description. (§8.5.3.1's type-declaration equivalence, <c>StrongTypeModel</c>, is a different
/// rule over two type declarations and lives outside this layer.)</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OneRelationDriftTests
{
    /// <summary>The description facts an item carries that the identity of §9.3.5.3 item 6 / §14.8.2.3.2 rule 2 /
    /// §9.3.8.2.3 rule 3 is made of — its category, usage, PICTURE identity, ANY LENGTH, and the entry clauses.</summary>
    private static readonly HashSet<string> DescriptionFacts = new(StringComparer.Ordinal)
    {
        "Category", "Usage", "Clause", "IsAnyLength", "Justified", "BlankWhenZero", "SignKind", "Digits", "Scale",
        "Signed", "IsAligned", "IsDynamicLength", "EditMask", "LocaleEdit", "ObjectRef", "AsIfPic", "GroupUsage",
        "RestrictedPrototypeName", "RestrictedTypeName", "PointerRestrictionIdentity",
    };

    private static IEnumerable<string> ConformanceLayer()
    {
        string compiler = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(Path.Combine(compiler, "Oo"), "*.cs", SearchOption.AllDirectories))
            yield return f;
        foreach (string f in Directory.EnumerateFiles(
                     Path.Combine(compiler, "Binding", "Procedure", "Verbs"), "*.cs", SearchOption.AllDirectories))
            yield return f;
        foreach (string f in Directory.EnumerateFiles(Path.Combine(compiler, "Binding"), "PrototypeSignature*.cs"))
            yield return f;
    }

    /// <summary>The fact a side of a comparison reads, when it is <c>receiver.Fact</c> (or <c>receiver?.Fact</c>) — with
    /// the receiver's text, so <c>a.Usage == a.Usage</c>-style reads of ONE item are not a pair.</summary>
    private static (string Fact, string Receiver)? FactOf(ExpressionSyntax e) => e switch
    {
        MemberAccessExpressionSyntax m when DescriptionFacts.Contains(m.Name.Identifier.Text)
            => (m.Name.Identifier.Text, m.Expression.ToString()),
        ConditionalAccessExpressionSyntax { WhenNotNull: MemberBindingExpressionSyntax b } c
            when DescriptionFacts.Contains(b.Name.Identifier.Text)
            => (b.Name.Identifier.Text, c.Expression.ToString()),
        ParenthesizedExpressionSyntax p => FactOf(p.Expression),
        _ => null,
    };

    private static bool IsPair(ExpressionSyntax left, ExpressionSyntax right) =>
        FactOf(left) is { } l && FactOf(right) is { } r && l.Fact == r.Fact && l.Receiver != r.Receiver;

    [Fact]
    public void NoMemberOfTheConformanceLayer_ComparesTwoItemsDescriptions()
    {
        var files = ConformanceLayer().ToList();
        Assert.True(files.Count > 20, $"only {files.Count} files — the layer's paths moved and the pin measures nothing");
        var offenders = new List<string>();
        foreach (string file in files)
        {
            if (Path.GetFileName(file) == "ActivationDescriptions.cs") continue;   // the ONE builder
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var node in root.DescendantNodes())
            {
                bool pair = node switch
                {
                    BinaryExpressionSyntax b when b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression)
                        => IsPair(b.Left, b.Right),
                    InvocationExpressionSyntax { ArgumentList.Arguments.Count: >= 2 } i
                        when (i.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Equals" }
                              || i.Expression is IdentifierNameSyntax { Identifier.Text: "Equals" })
                        => IsPair(i.ArgumentList.Arguments[0].Expression, i.ArgumentList.Arguments[1].Expression),
                    _ => false,
                };
                if (pair)
                    offenders.Add($"{Path.GetFileName(file)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {node}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A conformance-layer member compares two items' description facts itself instead of asking the relation over "
            + "their descriptions (ActivationRelations through ActivationDescriptions; kb/Work PB3101):\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The walk sees the shape it forbids: the deleted comparator's own lines are pairs.</summary>
    [Fact]
    public void ThePairDetector_SeesTheComparatorShape()
    {
        foreach (string line in new[]
                 {
                     "f.Usage != a.Usage", "formal.IsAnyLength != arg.IsAnyLength", "formal.Justified != arg.Justified",
                     "fa.Pic?.Category == fb.Pic?.Category",
                 })
        {
            var b = (BinaryExpressionSyntax)SyntaxFactory.ParseExpression(line);
            Assert.True(IsPair(b.Left, b.Right), $"the detector misses '{line}'");
        }
        var call = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression("Equals(fp.Clause, ap.Clause)");
        Assert.True(IsPair(call.ArgumentList.Arguments[0].Expression, call.ArgumentList.Arguments[1].Expression));
        var one = (BinaryExpressionSyntax)SyntaxFactory.ParseExpression("p.Usage == Usage.Index");
        Assert.False(IsPair(one.Left, one.Right));
    }
}
