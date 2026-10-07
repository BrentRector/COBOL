// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// The test-tree half of the dead-artifact census, as compiler facts: scaffold types (no test method of their own or
/// inherited, no use from outside themselves in ANY assembly that compiles them — a linked <c>tests/_shared</c> file
/// is compiled into every test assembly), and the strings drift tests name, which <c>scripts/arch/census.py</c>
/// resolves against the tree.
/// </summary>
internal static class TestArtifacts
{
    private const string DriftSuffix = "DriftTests.cs";
    private const string Locator = "TestRepo";

    public static IReadOnlyList<RawScaffold> Scaffolds(LoadedSolution solution, ReferenceIndex index)
    {
        var byDeclaration = new Dictionary<(string File, string Id), (List<string> Projects, bool Used, int Line, int Lines)>();
        foreach (LoadedProject project in solution.Projects.Where(p => !p.IsCensus))
        {
            var authored = project.Documents.Where(d => !d.Generated).Select(d => d.Tree).ToHashSet();
            foreach (INamedTypeSymbol type in TypeInventory.AllTypes(project.Compilation.Assembly.GlobalNamespace))
            {
                SyntaxReference? first = type.DeclaringSyntaxReferences.FirstOrDefault(r => authored.Contains(r.SyntaxTree));
                if (first is null || IsTestOrDiscovered(type, index) || SymbolKeys.Key(type) is not { } key)
                {
                    continue;
                }

                bool used = index.OutsideUsersOf(key).Count > 0;
                FileLinePositionSpan span = first.SyntaxTree.GetLineSpan(first.Span);
                var at = (solution.Paths.Relative(first.SyntaxTree.FilePath), SymbolKeys.MetadataName(type));
                if (!byDeclaration.TryGetValue(at, out var entry))
                {
                    entry = ([], false, span.StartLinePosition.Line + 1,
                        span.EndLinePosition.Line - span.StartLinePosition.Line + 1);
                }

                entry.Projects.Add(project.Name);
                byDeclaration[at] = entry with { Used = entry.Used || used };
            }
        }

        return [.. byDeclaration.Where(e => !e.Value.Used)
            .Select(e => new RawScaffold(e.Key.Id, e.Value.Projects, e.Key.File, e.Value.Line, e.Value.Lines))
            .OrderBy(s => s.File, StringComparer.Ordinal).ThenBy(s => s.Line)];
    }

    /// <summary>A test class (it or a base declares an xunit-attributed method), a type a framework discovers (an
    /// xunit or BenchmarkDotNet attribute on it), or a type named in a string (xunit resolves its framework by name).</summary>
    private static bool IsTestOrDiscovered(INamedTypeSymbol type, ReferenceIndex index)
    {
        for (INamedTypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (t.GetMembers().Any(m => m.GetAttributes().Any(a => IsFrameworkAttribute(a.AttributeClass))))
            {
                return true;
            }
        }

        return type.GetAttributes().Any(a => IsFrameworkAttribute(a.AttributeClass))
               || type.GetMembers(WellKnownMemberNames.EntryPointMethodName).Any(m => m.IsStatic)
               || index.StringLiterals.Contains(type.ToDisplayString())
               || index.StringLiterals.Contains(SymbolKeys.MetadataName(type));
    }

    private static bool IsFrameworkAttribute(INamedTypeSymbol? attribute)
    {
        string name = attribute?.ToDisplayString() ?? "";
        return name.StartsWith("Xunit.", StringComparison.Ordinal) || name.StartsWith("BenchmarkDotNet.", StringComparison.Ordinal);
    }

    /// <summary>Every string literal in a drift test, classified (<see cref="RawLiteral"/>), and every repository
    /// path a <c>TestRepo</c> call with only literal arguments builds. The locator's prefixes are READ from its own expression bodies (<c>Src(..)</c> is
    /// <c>At(["src", .. segments])</c>), never restated here.</summary>
    public static IReadOnlyList<RawLiteral> DriftLiterals(LoadedSolution solution)
    {
        var literals = new SortedSet<RawLiteral>(Comparer<RawLiteral>.Create((a, b) =>
            string.CompareOrdinal($"{a.File}\0{a.Line:D6}\0{a.Kind}\0{a.Text}", $"{b.File}\0{b.Line:D6}\0{b.Kind}\0{b.Text}")));
        foreach (LoadedProject project in solution.Projects.Where(p => !p.IsCensus))
        {
            IReadOnlyDictionary<string, string> prefixes = LocatorPrefixes(project);
            foreach (LoadedDocument document in project.Documents.Where(d => !d.Generated
                         && d.RepoPath.EndsWith(DriftSuffix, StringComparison.Ordinal)))
            {
                foreach (SyntaxNode node in document.Tree.GetRoot().DescendantNodes())
                {
                    int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (node is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        literals.Add(new RawLiteral(document.RepoPath, line, literal.Token.ValueText,
                            InStaticList(literal) ? "list" : "other"));
                    }
                    else if (node is InvocationExpressionSyntax
                             {
                                 Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: Locator } } call,
                             } invocation
                             && prefixes.TryGetValue(call.Name.Identifier.Text, out string? prefix)
                             && invocation.ArgumentList.Arguments.Count > 0
                             && invocation.ArgumentList.Arguments.All(a => a.Expression.IsKind(SyntaxKind.StringLiteralExpression)))
                    {
                        IEnumerable<string> parts = invocation.ArgumentList.Arguments
                            .Select(a => ((LiteralExpressionSyntax)a.Expression).Token.ValueText);
                        literals.Add(new RawLiteral(document.RepoPath, line, prefix + string.Join("/", parts), "path"));
                    }
                }
            }
        }

        return [.. literals];
    }

    /// <summary>An element of the collection that initializes a static field — an exemption or allow list.</summary>
    private static bool InStaticList(LiteralExpressionSyntax literal) =>
        literal.Parent is ExpressionElementSyntax or InitializerExpressionSyntax
        && literal.FirstAncestorOrSelf<FieldDeclarationSyntax>() is { } field
        && field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword))
        && literal.Ancestors().Any(a => a is CollectionExpressionSyntax or InitializerExpressionSyntax);

    /// <summary>
    /// The path prefix of each <c>TestRepo</c> method whose body is <c>At(...)</c> itself (<c>Path.Combine([Root, ..
    /// segments])</c>, the empty prefix) or a call to another such method with literal segments before the spread —
    /// resolved from the locator's own source. A method of any other shape is left out, so its calls are reported
    /// only as their literal arguments.
    /// </summary>
    private static IReadOnlyDictionary<string, string> LocatorPrefixes(LoadedProject project)
    {
        var bodies = new Dictionary<string, ExpressionSyntax>(StringComparer.Ordinal);
        foreach (LoadedDocument document in project.Documents)
        {
            foreach (ClassDeclarationSyntax locator in document.Tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                         .Where(c => c.Identifier.Text == Locator))
            {
                foreach (MethodDeclarationSyntax method in locator.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (method.ExpressionBody?.Expression is { } body)
                    {
                        bodies[method.Identifier.Text] = body;
                    }
                }
            }
        }

        var prefixes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in bodies.Keys)
        {
            if (Resolve(name, bodies, depth: 0) is { } prefix)
            {
                prefixes[name] = prefix;
            }
        }

        return prefixes;
    }

    private static string? Resolve(string method, Dictionary<string, ExpressionSyntax> bodies, int depth)
    {
        if (depth > 8 || !bodies.TryGetValue(method, out ExpressionSyntax? body)
            || body is not InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: CollectionExpressionSyntax segments }] } call)
        {
            return null;
        }

        string callee = call.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
            _ => "",
        };
        if (segments.Elements.Count == 0 || segments.Elements[^1] is not SpreadElementSyntax)
        {
            return null;
        }

        if (callee == "Combine")
        {
            // Path.Combine([Root, .. segments]): the root itself, with no literal segment.
            return segments.Elements is [ExpressionElementSyntax { Expression: IdentifierNameSyntax }, _] ? "" : null;
        }

        var literal = segments.Elements.SkipLast(1).ToList();
        if (!literal.All(e => e is ExpressionElementSyntax { Expression: LiteralExpressionSyntax }))
        {
            return null;
        }

        string own = string.Concat(literal.Select(e =>
            ((LiteralExpressionSyntax)((ExpressionElementSyntax)e).Expression).Token.ValueText + "/"));
        return Resolve(callee, bodies, depth + 1) is { } outer ? outer + own : null;
    }
}
