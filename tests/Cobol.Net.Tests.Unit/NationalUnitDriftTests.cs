// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ HOW MANY BYTES A POSITION OCCUPIES IS A FUNCTION OF THE USAGE, AND THE COMPILER SPELLS IT ONCE — <see
/// cref="PicInfo.BytesPerPosition"/> (kb/Work PB546; docs/COBOLNET_DATA_MODEL_DESIGN.md D-N1). ISO §13.18.60.4 GR8 makes
/// USAGE NATIONAL the representation, and §13.18.60.3 SR12 lets a usage-national item be of category boolean, national,
/// national-edited, numeric or numeric-edited, so a national byte count chosen by <c>PicCategory.National</c> puts the
/// boolean, numeric and numeric-edited items of usage national in the one-byte arm. That was measured twice: a boolean
/// usage-national slice serialized one byte a position (<c>BYTE-LENGTH(BN(1:2))</c> = 2) and a group MOVE into a national
/// DYNAMIC LENGTH item converted characters where §14.9.25.4 GR4 copies bytes. This class walks every C# file of the
/// compiler with Roslyn and is red when a national-unit operation — a <c>NatBytes</c>, <c>NatReadWindow</c>,
/// <c>NatWriteWindow</c> or <c>NatFromStorage</c> emission, the <c>ByteLengthNational</c> runtime entry, or a read of
/// <c>BytesPerNational</c> — is chosen by a condition that tests the category national and not the usage, or when a
/// conditional chooses the literal 2 by the category. The literal and function-result sites are the named exceptions:
/// a national literal and a function of type national are usage national by definition.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NationalUnitDriftTests
{
    /// <summary>The national-unit operations: the runtime serializers and their decoders, the BYTE-LENGTH entry for a
    /// national string and the pinned unit itself.</summary>
    private static readonly HashSet<string> UnitOperations = new(StringComparer.Ordinal)
    {
        "NatBytes", "NatReadWindow", "NatWriteWindow", "NatFromStorage", "ByteLengthNational", "BytesPerNational",
    };

    /// <summary>A guard that tests an operand whose usage is national by definition: a national LITERAL (its pattern
    /// names <c>BoundStringLiteral</c>) or a FUNCTION result of type national (<c>IntrinsicResultType.OperandCategory</c>).</summary>
    private static bool IsDefinitionallyNational(string guard) =>
        guard.Contains("BoundStringLiteral", StringComparison.Ordinal)
        || guard.Contains("OperandCategory(", StringComparison.Ordinal);

    private static bool TestsCategoryNational(string text) => text.Contains("PicCategory.National", StringComparison.Ordinal);

    private static bool TestsUsage(string text) =>
        text.Contains("Usage.National", StringComparison.Ordinal) || text.Contains("BytesPerPosition", StringComparison.Ordinal)
        || text.Contains("PositionBits", StringComparison.Ordinal) || text.Contains("NationalWindow", StringComparison.Ordinal)
        || text.Contains("PositionsOf(", StringComparison.Ordinal);

    /// <summary>Every condition that selects <paramref name="node"/>: the test of an enclosing conditional expression or
    /// <c>if</c> on the branch it sits in, and the pattern and <c>when</c> clause of an enclosing switch arm or case.</summary>
    private static IEnumerable<string> GuardsOf(SyntaxNode node)
    {
        SyntaxNode child = node;
        for (var p = node.Parent; p is not null && p is not MemberDeclarationSyntax; child = p, p = p.Parent)
        {
            switch (p)
            {
                case ConditionalExpressionSyntax c when c.Condition != child:
                    yield return c.Condition.ToString();
                    break;
                case IfStatementSyntax i when i.Condition != child:
                    yield return i.Condition.ToString();
                    break;
                case SwitchExpressionArmSyntax a when a.Expression == child:
                    // The governing expression is part of the test: `OperandCategory(x) switch { PicCategory.National => … }`.
                    yield return (a.Parent as SwitchExpressionSyntax)?.GoverningExpression + " " + a.Pattern + " " + a.WhenClause;
                    break;
                case SwitchSectionSyntax s:
                    yield return string.Join(" ", s.Labels);
                    break;
                case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression } b when b.Right == child:
                    yield return b.Left.ToString();
                    break;
            }
        }
    }

    private static IEnumerable<(string Rel, int Line, SyntaxNode Node)> CompilerNodes()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f);
            if (rel.StartsWith("obj", StringComparison.Ordinal) || rel.StartsWith("bin", StringComparison.Ordinal)) continue;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(f));
            foreach (var n in tree.GetRoot().DescendantNodes())
                yield return (rel.Replace('\\', '/'), n.GetLocation().GetLineSpan().StartLinePosition.Line + 1, n);
        }
    }

    private static string? UnitOperationOf(SyntaxNode n) => n switch
    {
        MemberAccessExpressionSyntax m when UnitOperations.Contains(m.Name.Identifier.ValueText) => m.Name.Identifier.ValueText,
        LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } l
            when UnitOperations.Contains(l.Token.ValueText) => l.Token.ValueText,
        _ => null,
    };

    [Fact]
    public void EveryNationalUnitOperation_IsChosenByTheUsage_NeverByTheCategoryAlone()
    {
        var nodes = CompilerNodes().ToList();
        var sites = nodes.Select(n => (n.Rel, n.Line, n.Node, Op: UnitOperationOf(n.Node))).Where(s => s.Op is not null).ToList();
        // The walk finds what it guards — a walk that matched nothing would pass vacuously.
        Assert.Contains(sites, s => s.Op == "NatBytes" && s.Rel.EndsWith("OperandText.cs", StringComparison.Ordinal));
        Assert.Contains(sites, s => s.Op == "NatFromStorage" && s.Rel.EndsWith("ReceivingStore.cs", StringComparison.Ordinal));
        Assert.Contains(sites, s => s.Op == "BytesPerNational" && s.Rel.EndsWith("PicInfo.cs", StringComparison.Ordinal));
        var categoryKeyed = sites
            .Where(s => GuardsOf(s.Node).Any(g => TestsCategoryNational(g) && !TestsUsage(g) && !IsDefinitionallyNational(g)))
            .Select(s => $"{s.Rel}:{s.Line}: {s.Op}")
            .ToList();
        Assert.True(categoryKeyed.Count == 0,
            "a national-unit operation is chosen by PicCategory.National alone, so a boolean, numeric or numeric-edited "
            + "item of usage national (§13.18.60.3 SR12) takes the one-byte arm (kb/Work PB546) — test the usage, through "
            + "PicInfo.BytesPerPosition:\n  " + string.Join("\n  ", categoryKeyed));
    }

    [Fact]
    public void NoConditional_ChoosesTheLiteralTwoByTheCategoryNational()
    {
        var hardCoded = CompilerNodes()
            .Where(n => n.Node is ConditionalExpressionSyntax c && TestsCategoryNational(c.Condition.ToString())
                        && (IsLiteralTwo(c.WhenTrue) || IsLiteralTwo(c.WhenFalse)))
            .Select(n => $"{n.Rel}:{n.Line}: {n.Node}")
            .ToList();
        Assert.True(hardCoded.Count == 0,
            "a conditional picks the national byte count 2 by the category — the unit is PicInfo.BytesPerPosition's "
            + "(kb/Work PB546):\n  " + string.Join("\n  ", hardCoded));

        static bool IsLiteralTwo(ExpressionSyntax e) => e is LiteralExpressionSyntax { Token.ValueText: "2" };
    }

    [Fact]
    public void TheUnitReader_AnswersByUsage_ForEveryUsageNationalCategory()
    {
        foreach (var category in new[] { PicCategory.National, PicCategory.Boolean, PicCategory.Numeric, PicCategory.NumericEdited })
            Assert.Equal(CobolBits.BytesPerNational,
                new PicInfo(category, Usage.National, 4, Digits: 0, Scale: 0, Signed: false).BytesPerPosition);
        Assert.Equal(1, new PicInfo(PicCategory.Alphanumeric, Usage.Display, 4, Digits: 0, Scale: 0, Signed: false).BytesPerPosition);
        Assert.Equal(1, new PicInfo(PicCategory.Boolean, Usage.Display, 4, Digits: 0, Scale: 0, Signed: false).BytesPerPosition);
    }
}
