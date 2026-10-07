// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A TEST THAT CANNOT FAIL IS NOT A TEST, AND A SECTION WITH NO TEST IN IT IS NOT COVERAGE (kb/Work PB387).
///
/// <para><b>What happened.</b> The legacy unit project carried 45 <c>[Fact]</c> methods named
/// <c>Rejects_&lt;construct&gt;</c> across 15 files, every body <c>{ }</c>. xUnit ran and reported all 45 as PASSING in
/// every gate, and every name was a conformance claim — "the compiler refuses this" — that nothing measured. Two of
/// the claims were FALSE (the compiler accepted the source), and a name-based reader of the suite saw a green test for
/// exactly the rule it was looking for and stopped. Beside them, an integration file carried a
/// <c>// ── NEXT SENTENCE ──</c> section banner with no test under it at all. An empty test is the one defect a test
/// suite structurally cannot report about itself, so this test reports it.</para>
///
/// <para><b>The two rules, over every C# file of the three test assemblies.</b>
/// <list type="number">
///   <item>A <c>[Fact]</c> / <c>[Theory]</c> method that is not skipped must be ABLE to fail: its body contains at
///     least one invocation, object creation or throw. A body with none of them — <c>{ }</c>, or only local
///     declarations of constants — passes whatever the compiler does.</item>
///   <item>A section banner (<c>// ── title ──</c>) must have a member under it: a banner whose next member is
///     preceded by a SECOND banner, or which is followed by the type's closing brace, heads an empty section.</item>
/// </list>
/// Each detector is driven first by a fixture that MUST trip it — one of the deleted stub files verbatim, and the
/// empty-banner shape — so a detector that silently stopped looking would turn this test red rather than green.</para>
/// </summary>
public sealed class VacuousTestDriftTests
{
    private static readonly string[] TestAssemblies =
        ["Cobol.Net.Tests.Unit", "Cobol.Net.Tests.Conformance", "Cobol.Net.Tests.Characterization"];

    /// <summary>A section banner: a line comment that opens with a box-drawing rule run, then a TITLE. A bare rule
    /// (<c>// ──────</c>) is the closing edge of a comment block, not a section, and heads nothing by design.</summary>
    private static readonly Regex Banner = new(@"^//\s*─{2,}\s+[^─\s]", RegexOptions.Compiled);

    [Fact]
    public void EveryTest_CanFail()
    {
        var vacuous = SourceFiles().SelectMany(f => VacuousTests(f.Path, f.Text)).ToList();
        Assert.True(vacuous.Count == 0,
            "a [Fact]/[Theory] whose body contains no invocation, object creation or throw passes whatever the code "
            + "under test does — assert the behaviour its name claims, or delete it (kb/Work PB387):\n  "
            + string.Join("\n  ", vacuous));
    }

    [Fact]
    public void EverySectionBanner_HeadsAMember()
    {
        var empty = SourceFiles().SelectMany(f => EmptySections(f.Path, f.Text)).ToList();
        Assert.True(empty.Count == 0,
            "a section banner with no member under it reads as coverage that does not exist — add the tests it "
            + "announces, or remove the banner (kb/Work PB387):\n  " + string.Join("\n  ", empty));
    }

    /// <summary>The detectors look: the deleted <c>M413_OverlenientIfElseTests.cs</c>, verbatim, trips the first
    /// rule three times, and the empty-banner shape trips the second, while a real test and a populated section
    /// trip neither.</summary>
    [Fact]
    public void TheDetectors_FlagTheShapesTheyExistFor()
    {
        const string deletedStub = """
            using Xunit;

            namespace CobolSharp.Tests.Unit.Overlenient;

            public sealed class M413_OverlenientIfElseTests
            {
                [Fact]
                public void Rejects_MissingCondition()
                {
                }

                [Fact]
                public void Rejects_ExtraElseBranch()
                {
                }

                [Fact]
                public void Rejects_IfWithoutThenOrStatement()
                {
                }
            }
            """;
        Assert.Equal(3, VacuousTests("M413.cs", deletedStub).Count());

        const string sections = """
            public sealed class ConditionTests
            {
                // ── NEXT SENTENCE ──────────
                // ── abbreviated relations ──────────
                [Fact]
                public void Abbreviated() => Assert.True(Check());

                [Fact(Skip = "pending")]
                public void Skipped() { }

                // ── trailing ──────────
            }
            """;
        Assert.Empty(VacuousTests("ConditionTests.cs", sections));
        Assert.Equal(2, EmptySections("ConditionTests.cs", sections).Count());
    }

    private static IEnumerable<(string Path, string Text)> SourceFiles() =>
        TestAssemblies.SelectMany(a => Directory.EnumerateFiles(TestRepo.Tests(a), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(p => (Path.GetRelativePath(TestRepo.Root, p), File.ReadAllText(p)));

    /// <summary>Rule 1: every non-skipped <c>[Fact]</c> / <c>[Theory]</c> whose body cannot fail.</summary>
    private static IEnumerable<string> VacuousTests(string path, string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var test = method.AttributeLists.SelectMany(l => l.Attributes)
                .FirstOrDefault(a => a.Name.ToString() is "Fact" or "Theory" or "FactAttribute" or "TheoryAttribute");
            if (test is null) continue;
            if (test.ArgumentList?.Arguments.Any(a => a.NameEquals?.Name.Identifier.Text == "Skip") == true) continue;
            SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;
            bool canFail = body is not null && body.DescendantNodesAndSelf().Any(n =>
                n is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
                    or ThrowStatementSyntax or ThrowExpressionSyntax);
            if (!canFail)
                yield return $"{path}:{method.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {method.Identifier.Text}";
        }
    }

    /// <summary>Rule 2: every banner that heads no member — one followed by another banner before the next member,
    /// or by the closing brace of its type.</summary>
    private static IEnumerable<string> EmptySections(string path, string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var anchors = type.Members.Select(m => m.GetFirstToken()).Append(type.CloseBraceToken);
            foreach (var token in anchors)
            {
                var banners = token.LeadingTrivia
                    .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) && Banner.IsMatch(t.ToString()))
                    .ToList();
                // Every banner but the last before a member heads nothing; before the closing brace, none heads anything.
                int empties = token.IsKind(SyntaxKind.CloseBraceToken) ? banners.Count : Math.Max(0, banners.Count - 1);
                foreach (var b in banners.Take(empties))
                    yield return $"{path}:{b.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {b}";
            }
        }
    }
}
