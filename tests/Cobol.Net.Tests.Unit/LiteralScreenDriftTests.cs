// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using CobolNet.Validation;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LITERAL'S OWN SYNTAX RULES ARE ASKED ONCE, OF EVERY LITERAL TOKEN, AT ONE SITE (kb/Work PB1393): the §8.3.3
/// length rule (COBOLNET0814) and hexadecimal grouping rule (COBOLNET1635) live in <c>LiteralScreenPass</c>, which
/// walks every token of the unit's tree, and nowhere else; and the pass's token set is every token the lexer defines
/// over a literal body fragment, so a new literal token — a new lexer mode's twin — joins the screen or fails here.
/// </summary>
/// <remarks>
/// Each rule used to be written in the one funnel its author was fixing (the length cap in two procedure-operand
/// arms; the grouping rule in two version-pass visitors), and every other position a literal can be written in —
/// VALUE, level-88, CONSTANT, ALL literal-1, a concatenation operand, a keyword-omitted intrinsic argument —
/// compiled the violation in silence. The keyword-omitted argument is the reason for the token-set half: it is
/// screened as its SUBSCRIPT-mode token because its re-parsed fragment is not part of the walked tree, which was a
/// hole exactly as long as <c>X"…"</c> had no SUBSCRIPT-mode twin.
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
        Assert.True(defined.Count >= 8,
            $"expected the four DEFAULT-mode literal tokens and their SUBSCRIPT twins, found {defined.Count}");
        var screened = LiteralScreenPass.LiteralTokenTypes
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t)).ToHashSet();
        Assert.Equal(defined.Order(), screened.Order());
    }

    /// <summary>The rule predicates and their descriptors have ONE production caller, and no emit site spells either
    /// code as a bare string.</summary>
    [Theory]
    [InlineData("CobolLiteral.HexGroupViolation(", true)]
    [InlineData("CobolLiteral.LengthViolation(", true)]
    [InlineData("DiagnosticCatalog.HexLiteralDigitGrouping", true)]
    [InlineData("DiagnosticCatalog.LiteralTooLong", true)]
    [InlineData("\"COBOLNET0814\"", false)]
    [InlineData("\"COBOLNET1635\"", false)]
    public void LiteralRule_HasOneReportingSite(string needle, bool askedByTheScreen)
    {
        char sep = Path.DirectorySeparatorChar;
        var sites = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Contains($"{sep}Cobol.Net.") && !p.Contains($"{sep}obj{sep}") && !p.Contains($"{sep}bin{sep}")
                        && Path.GetFileName(p) is not ("DiagnosticCatalog.cs" or "CobolLiteral.cs"))
            .Where(p => File.ReadAllText(p).Contains(needle, StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p))
            .ToList();
        string[] expected = askedByTheScreen ? ["LiteralScreenPass.cs"] : [];
        Assert.True(sites.SequenceEqual(expected),
            $"'{needle}' is used at [{string.Join(", ", sites)}], expected [{string.Join(", ", expected)}] — a literal's "
            + "own §8.3.3 rules are a property of the TOKEN; a second site is a second funnel the next literal position "
            + "will miss");
    }
}
