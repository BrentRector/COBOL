// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Tests.Shared;
using CobolNet.Validation;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LITERAL'S OWN SYNTAX RULES ARE ASKED ONCE, OF EVERY LITERAL TOKEN, AT ONE SITE (kb/Work PB1393): the §8.3.3
/// length rule (COBOLNET0814), hexadecimal grouping rule (COBOLNET1635) and content repertoire rule (COBOLNET2630,
/// kb/Work PB1441) live in <c>LiteralScreenPass</c>, which walks every token of the unit's tree, and nowhere else; and
/// the literal token set (<c>LiteralTokens.Types</c>, shared with the §8.3.5 <c>SeparatorRule</c>) is every token the
/// lexer defines over a literal body fragment, so a new literal token joins both rules or fails here.
/// </summary>
/// <remarks>
/// Each rule used to be written in the one funnel its author was fixing (the length cap in two procedure-operand
/// arms; the grouping rule in two version-pass visitors), and every other position a literal can be written in —
/// VALUE, level-88, CONSTANT, ALL literal-1, a concatenation operand, a keyword-omitted intrinsic argument —
/// compiled the violation in silence. The keyword-omitted argument was the reason for the token-set half while it was
/// a SUBSCRIPT-mode capture (a hole exactly as long as <c>X"…"</c> had no twin there); since kb/Work PB2113 it is
/// parsed in place, so its literals are the ordinary tokens of the walked tree.
/// </remarks>
public sealed class LiteralScreenDriftTests
{
    /// <summary>The lexer fragments whose match IS a literal of class alphanumeric, boolean or national.</summary>
    private static readonly string[] LiteralBodies = ["STR_BODY", "NAT_BODY", "BOOL_BODY", "HEX_BODY"];

    private static readonly string LexerText =
        File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4"));

    [Fact]
    public void ScreenTokenSet_IsEveryTokenDefinedOverALiteralBody()
    {
        foreach (string body in LiteralBodies)
            Assert.Matches(new Regex($@"(?m)^fragment\s+{body}\s*:"), LexerText);   // a rename must fail here, not empty the set
        var defined = Regex.Matches(LexerText, $@"(?m)^([A-Z_]+)\s*:\s*({string.Join("|", LiteralBodies)})\s*;")
            .Select(m => m.Groups[1].Value).ToHashSet();
        Assert.True(defined.Count >= 4,
            $"expected the four literal tokens (STRINGLIT, HEXLIT, NATLIT, BOOLLIT), found {defined.Count}");
        var screened = LiteralTokens.Types
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t)).ToHashSet();
        Assert.Equal(defined.Order(), screened.Order());
    }

    /// <summary>The three rule predicates are asked only through <c>CobolLiteral.SyntaxViolation</c>, their one order;
    /// that is asked once per lexing of literal tokens — the unit's tree (<c>LiteralScreenPass</c>) and a compiler-directive
    /// operand, lexed apart from the unit (<c>CompileTimeExpressionEvaluator</c>, kb/Work PB1441); the descriptors have ONE
    /// emit site, and no emit site spells a code as a bare string.</summary>
    [Theory]
    [InlineData("CobolLiteral.SyntaxViolation(", "CompileTimeExpressionEvaluator.cs,LiteralScreenPass.cs")]
    [InlineData("CobolLiteral.HexGroupViolation(", "")]
    [InlineData("CobolLiteral.RepertoireViolation(", "")]
    [InlineData("CobolLiteral.LengthViolation(", "")]
    [InlineData("DiagnosticCatalog.LiteralContentRepertoire", "LiteralScreenPass.cs")]
    [InlineData("DiagnosticCatalog.HexLiteralDigitGrouping", "LiteralScreenPass.cs")]
    [InlineData("DiagnosticCatalog.LiteralTooLong", "LiteralScreenPass.cs")]
    [InlineData("\"COBOLNET2630\"", "")]
    [InlineData("\"COBOLNET0814\"", "")]
    [InlineData("\"COBOLNET1635\"", "")]
    public void LiteralRule_HasOneReportingSite(string needle, string expectedSites)
    {
        char sep = Path.DirectorySeparatorChar;
        var sites = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Contains($"{sep}Cobol.Net.") && !p.Contains($"{sep}obj{sep}") && !p.Contains($"{sep}bin{sep}")
                        && Path.GetFileName(p) is not ("DiagnosticCatalog.cs" or "CobolLiteral.cs"))
            .Where(p => File.ReadAllText(p).Contains(needle, StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p))
            .Order(StringComparer.Ordinal)
            .ToList();
        string[] expected = expectedSites.Length == 0 ? [] : expectedSites.Split(',');
        Assert.True(sites.SequenceEqual(expected),
            $"'{needle}' is used at [{string.Join(", ", sites)}], expected [{string.Join(", ", expected)}] — a literal's "
            + "own §8.3.3 rules are a property of the TOKEN; a second site is a second funnel the next literal position "
            + "will miss");
    }
}
