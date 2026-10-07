// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SIGNED-LITERAL SHAPE SYMMETRY (kb/Work R17 — ledger F12). The lexer has three numeric literal
/// BODIES (integer, decimal, float), and a sign-adjacent form must exist for each — the <c>FN_SIGNED_*</c> twins, whose
/// <c>OnSignedLiteral</c> action keeps the signed literal only inside a list region (an argument list or, since kb/Work
/// PB2113 removed the SUBSCRIPT lexer mode, a reference's subscript list) where §8.3.3.3.2 makes the sign part of it,
/// and otherwise splits the sign off as the operator.
/// The float shape was the one signed-capable body with NO twins, so <c>FUNCTION EXP(-1.5E3)</c> lexed as
/// TWO arguments (the signed-decimal rule won maximal munch at "-1.5" and orphaned "E3") and drew a false
/// arity diagnostic. This scrape makes the next literal body unable to join only one of the two shape sets silently.
/// </summary>
public sealed class SignedLiteralShapeDriftTests
{
    private static string LexerSource() =>
        System.IO.File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4"));

    /// <summary>The numeric literal body fragments (INT/DEC/FLOAT — scraped, not hand-listed, so a NEW numeric
    /// body automatically joins the assertion).</summary>
    private static string[] NumericBodies(string src) =>
        [.. Regex.Matches(src, @"fragment\s+(?<b>(?:INT|DEC|FLOAT)\w*_BODY)\s*:").Select(m => m.Groups["b"].Value)];

    [Fact]
    public void EveryNumericBody_HasItsFunctionArgumentSignedTwin()
    {
        string src = LexerSource();
        var bodies = NumericBodies(src);
        Assert.NotEmpty(bodies);
        foreach (string body in bodies)
            Assert.True(
                Regex.IsMatch(src, @"FN_SIGNED_\w+\s*:\s*\[\+-\]\s*" + body + @"\s*\{\s*OnSignedLiteral\("),
                $"{body} has no FN_SIGNED twin — a sign-adjacent literal of this shape in a FUNCTION argument "
                + "region will be split by maximal munch into a shorter signed literal plus an orphan (the F12 "
                + "false-arity failure). Add the twin, with its OnSignedLiteral action, beside FN_SIGNED_DECIMALLIT.");
    }

    [Fact]
    public void EveryDefaultModeNumericToken_ReferencesAFragmentBody()
    {
        // ⛔ The scrape above keys on `fragment *_BODY`, so a numeric token written with an INLINE body would
        // be structurally invisible to both assertions — which is precisely how the float hole survived: the
        // pre-R17 FLOATLIT carried its body inline, no fragment existed, and nothing demanded its twins. This
        // assertion closes that route: the three DEFAULT-mode numeric literal tokens must be bare fragment
        // references, so a NEW shape must mint a fragment and thereby joins the twin assertions.
        string src = LexerSource();
        foreach (string tok in new[] { "INTEGERLIT", "DECIMALLIT", "FLOATLIT" })
            Assert.True(Regex.IsMatch(src, @"^" + tok + @"\s*:\s*\w+_BODY\s*;", RegexOptions.Multiline),
                $"{tok} does not reference a *_BODY fragment — an inline body is invisible to the twin "
                + "assertions in this suite (the pre-R17 float hole's exact shape)");
    }
}
