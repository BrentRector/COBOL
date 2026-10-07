// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CobolNet.Tools.ArchCensus;

/// <summary>
/// Type-2 clone families over the authored census documents: every method, constructor, operator, accessor, local
/// function and expression-bodied property or indexer body of at least <c>minTokens</c> tokens is fingerprinted as
/// its token-KIND stream, with identifiers canonicalized to <c>$id</c> and literals to <c>$lit</c>, so a copy that
/// only renamed things still matches. The fingerprint and the body set are exactly the roslyn-analysis skill's
/// detector (<c>tools/claude-skills/skills/roslyn-analysis/references/detect-clones.cs</c>, default mode), so a
/// family here reproduces with that tool. A family is a CANDIDATE: two same-shaped bodies that call different members
/// match too, and the finding says to triage it semantically before extracting anything.
/// </summary>
internal static class CloneFamilies
{
    public static IReadOnlyList<RawCloneFamily> Detect(IEnumerable<LoadedDocument> documents, int minTokens)
    {
        var groups = new Dictionary<string, List<RawCloneInstance>>(StringComparer.Ordinal);
        var tokenCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (LoadedDocument document in documents.OrderBy(d => d.RepoPath, StringComparer.Ordinal))
        {
            foreach (SyntaxNode body in document.Tree.GetRoot().DescendantNodes().Select(BodyOf).OfType<SyntaxNode>())
            {
                var tokens = body.DescendantTokens().ToList();
                if (tokens.Count < minTokens)
                {
                    continue;
                }

                var fingerprint = new StringBuilder();
                foreach (SyntaxToken token in tokens)
                {
                    fingerprint.Append(Canonical(token)).Append(' ');
                }

                string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString())));
                FileLinePositionSpan span = document.Tree.GetLineSpan(body.Span);
                if (!groups.TryGetValue(key, out List<RawCloneInstance>? set))
                {
                    groups[key] = set = [];
                    tokenCounts[key] = tokens.Count;
                }

                set.Add(new RawCloneInstance(document.RepoPath, span.StartLinePosition.Line + 1,
                    span.EndLinePosition.Line + 1, OwnerOf(body)));
            }
        }

        return [.. groups.Where(g => g.Value.Count > 1)
            .Select(g => new RawCloneFamily(tokenCounts[g.Key], g.Value))
            .OrderByDescending(f => (f.Instances.Count - 1) * (f.Instances[0].End - f.Instances[0].Start + 1))
            .ThenBy(f => f.Instances[0].File, StringComparer.Ordinal)
            .ThenBy(f => f.Instances[0].Start)];
    }

    private static SyntaxNode? BodyOf(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
        LocalFunctionStatementSyntax f => (SyntaxNode?)f.Body ?? f.ExpressionBody,
        AccessorDeclarationSyntax a => (SyntaxNode?)a.Body ?? a.ExpressionBody,
        PropertyDeclarationSyntax p => p.ExpressionBody,
        IndexerDeclarationSyntax x => x.ExpressionBody,
        _ => null,
    };

    private static string Canonical(SyntaxToken token) => token.Kind() switch
    {
        SyntaxKind.IdentifierToken => "$id",
        SyntaxKind.StringLiteralToken or SyntaxKind.NumericLiteralToken or SyntaxKind.CharacterLiteralToken
            or SyntaxKind.InterpolatedStringTextToken or SyntaxKind.Utf8StringLiteralToken
            or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken => "$lit",
        var kind => kind.ToString(),
    };

    /// <summary>The declaring type and member a body belongs to (<c>Type.Member</c>), for the finding's site.</summary>
    private static string OwnerOf(SyntaxNode body)
    {
        SyntaxNode? member = body.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax or LocalFunctionStatementSyntax);
        string name = member switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            ConstructorDeclarationSyntax c => c.Identifier.Text + ".ctor",
            PropertyDeclarationSyntax p => p.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            OperatorDeclarationSyntax o => "operator " + o.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax c => "operator " + c.Type,
            LocalFunctionStatementSyntax f => f.Identifier.Text,
            EventDeclarationSyntax e => e.Identifier.Text,
            DestructorDeclarationSyntax d => "~" + d.Identifier.Text,
            _ => "?",
        };
        string type = body.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "?";
        return $"{type}.{name}";
    }
}
