// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Common;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §8.3.5 separator-context rules (kb/Work PB1394) and the §8.3.3 content-repertoire rule of the prefixed literal
/// formats (kb/Work PB1441), asked of source text lexed exactly as the front end lexes it. Every expectation is read off
/// the rule it cites, never off the compiler: §8.3.5 2) a comma or semicolon is a separator only when "immediately
/// followed by a space"; 3) a period only "when followed by a space"; 5) "The opening delimiter shall be immediately
/// preceded by a space, left parenthesis, or opening pseudo-text delimiter. The closing delimiter shall be immediately
/// followed by one of the separators space, comma, semicolon, period, right parenthesis, or closing pseudo-text
/// delimiter"; §8.3.3.2.3 SR5 / §8.3.3.5.3 SR4 / §8.3.3.4.3 SR3 hexadecimal digits, §8.3.3.4.3 SR2 '0' or '1'.
/// </summary>
public sealed class SeparatorRuleTests
{
    /// <summary>The code of every violation <see cref="SeparatorRule"/> finds in <paramref name="text"/>, in order.</summary>
    private static List<string> Codes(string text)
    {
        var tokens = new CommonTokenStream(new CobolLexer(new AntlrInputStream(text)));
        tokens.Fill();
        return [.. SeparatorRule.Violations(text, tokens.GetTokens()).Select(v => v.Descriptor.Code)];
    }

    [Theory]
    // Rule 2 — the separator forms, and the numeric-literal comma the rule leaves to the numeric-literal rules.
    [InlineData("MOVE 1 TO N, M.\n")]
    [InlineData("MOVE 1 TO N; M.\n")]
    [InlineData("MOVE 1,5 TO N.\n")]                  // DECIMAL-POINT IS COMMA's decimal point (§12.3.7.4 GR14 a))
    [InlineData("MOVE ,5 TO N.\n")]
    [InlineData("MOVE A(1, 2) TO N.\n")]              // inside a subscript list (FNARG_SEPARATOR)
    [InlineData("MOVE 1 TO N,\n")]                    // an end of line is a space to the lexer
    // Rule 3 — a period followed by a space or by the end of the text.
    [InlineData("DISPLAY N. DISPLAY M.")]
    [InlineData("MOVE 1.5 TO N.\n")]                  // a decimal point is part of the numeric literal's token
    // Rule 5 — every legal neighbour of both delimiters, in each literal format.
    [InlineData("DISPLAY \"AB\" N.\n")]
    [InlineData("DISPLAY (\"AB\").\n")]
    [InlineData("DISPLAY \"AB\", \"CD\"; X\"41\".\n")]
    [InlineData("DISPLAY N\"AB\" NX\"0041\" B\"01\" BX\"F\".\n")]
    [InlineData("REPLACE ==\"AB\"== BY ==\"CD\"==.\n")]  // the pseudo-text delimiters
    [InlineData("DISPLAY \"A\"\"B\".\n")]              // a doubled delimiter is content, one token
    [InlineData("MOVE FUNCTION UPPER-CASE(\"ab\") TO W.\n")]
    public void ConformingSeparators_AreNotReported(string text) => Assert.Empty(Codes(text));

    [Theory]
    [InlineData("MOVE 1 TO N,M.\n", "COBOLNET2631")]
    [InlineData("MOVE 1 TO N;M.\n", "COBOLNET2631")]          // the hidden-channel SEMICOLON
    [InlineData("MOVE A(I,J) TO N.\n", "COBOLNET2631")]      // a bare COMMA inside a subscript list
    [InlineData("MOVE A(1;2) TO N.\n", "COBOLNET2631")]      // a bare SEMICOLON (HIDDEN): never a decimal point
    [InlineData("MOVE N,5 TO M.\n", "COBOLNET2631")]         // a letter before it: no numeric literal
    [InlineData("DISPLAY N.DISPLAY M.\n", "COBOLNET2632")]
    [InlineData("DISPLAY \"AB\"N.\n", "COBOLNET2633")]          // closing delimiter
    [InlineData("MOVE\"AB\" TO A.\n", "COBOLNET2633")]          // opening delimiter
    [InlineData("IF \"AB\"=A DISPLAY \"Y\" END-IF.\n", "COBOLNET2633")]
    [InlineData("DISPLAY W\"AB\".\n", "COBOLNET2633")]          // a prefix-looking word: IDENTIFIER W + literal
    [InlineData("MOVE O::\"M\" TO A.\n", "COBOLNET2633")]
    [InlineData("DISPLAY \"AB\"(1:1).\n", "COBOLNET2633")]      // '(' is no closing separator
    public void NonconformingSeparator_IsReportedOnce(string text, string code) => Assert.Equal([code], Codes(text));

    [Fact] // two literals that touch: the closing delimiter's report covers the boundary — one diagnostic, not two
    public void AdjacentLiterals_ReportTheBoundaryOnce() => Assert.Equal(["COBOLNET2633"], Codes("DISPLAY \"A\"'B'.\n"));

    [Fact] // a comma that is no separator AND ends a literal's closing context is rule 2's, reported on the comma
    public void CommaAfterLiteral_IsRuleTwo() => Assert.Equal(["COBOLNET2631"], Codes("DISPLAY \"AB\",N.\n"));

    [Theory]
    [InlineData("X\"41\"")]
    [InlineData("x'4a4B'")]
    [InlineData("X\"\"")]
    [InlineData("NX\"0041\"")]
    [InlineData("BX\"F0a\"")]
    [InlineData("B\"0101\"")]
    [InlineData("B''")]
    [InlineData("\"GG\"")]      // Format 1 alphanumeric: any content
    [InlineData("N\"GG\"")]     // Format 1 national: any content
    public void InRepertoire_IsNotAViolation(string raw) => Assert.Null(CobolLiteral.RepertoireViolation(raw));

    [Theory]
    [InlineData("X\"GG\"", "§8.3.3.2.3 SR5", 'G')]
    [InlineData("X\"4G\"", "§8.3.3.2.3 SR5", 'G')]
    [InlineData("X\"4\"\"1\"", "§8.3.3.2.3 SR5", '"')]   // a doubled delimiter is content — not a hexadecimal digit
    [InlineData("NX\"00G1\"", "§8.3.3.5.3 SR4", 'G')]
    [InlineData("BX\"G1\"", "§8.3.3.4.3 SR3", 'G')]
    [InlineData("B\"012\"", "§8.3.3.4.3 SR2", '2')]
    [InlineData("B'1 0'", "§8.3.3.4.3 SR2", ' ')]
    public void OutsideRepertoire_NamesTheCharacterAndTheRule(string raw, string clause, char bad)
    {
        string? message = CobolLiteral.RepertoireViolation(raw);
        Assert.NotNull(message);
        Assert.Contains(clause, message);
        Assert.Contains($"'{bad}'", message);
        Assert.Null(CobolLiteral.HexGroupViolation(raw));   // the repertoire rule precedes the grouping rule
        if (!clause.EndsWith("SR2")) Assert.Equal("", CobolLiteral.Decode(raw));   // a hex decoder never throws on it
    }

    [Theory] // every prefixed literal is ONE token of its own type, whatever its content (the lexer no longer judges it)
    [InlineData("X\"GG\"", CobolLexer.HEXLIT)]
    [InlineData("NX\"00G1\"", CobolLexer.NATLIT)]
    [InlineData("BX\"G1\"", CobolLexer.BOOLLIT)]
    [InlineData("B\"012\"", CobolLexer.BOOLLIT)]
    [InlineData("X\"4\"\"1\"", CobolLexer.HEXLIT)]
    public void PrefixedLiteral_IsOneTokenWhateverItsContent(string raw, int type)
    {
        var tokens = new CobolLexer(new AntlrInputStream(raw)).GetAllTokens();
        Assert.Equal((type, raw), (Assert.Single(tokens).Type, tokens[0].Text));
    }
}
