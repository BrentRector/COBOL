// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The one reading of a literal's delimiters and of the comment indicator (<see cref="CobolSourceText"/>, kb/Work
/// PB2621), every expected value read from the rule: ISO §8.3.5 5) (the opening delimiters — a quotation symbol, B", N",
/// X", BX", NX" with either quotation symbol — and the closing one), §8.3.3.2.3 3) (two contiguous quotation symbols
/// matching the opening one are one character of the content) and §6.2.3.1 (<c>*&gt;</c> is the comment indicator).
/// </summary>
public sealed class CobolSourceTextTests
{
    [Theory]
    [InlineData("\"AB\"", 0, '\0', 0)]
    [InlineData("'AB'", 0, '\0', 0)]
    [InlineData("X\"41\"", 0, 'X', 1)]
    [InlineData("n'A'", 0, 'N', 1)]
    [InlineData("b\"1\"", 0, 'B', 1)]
    [InlineData("NX\"0041\"", 0, 'N', 2)]
    [InlineData("bx'1'", 0, 'B', 2)]
    [InlineData("(N\"A\")", 1, 'N', 1)]     // a left parenthesis may precede the opening delimiter
    [InlineData("ABX\"41\"", 3, '\0', 0)]   // the bare quotation symbol after the word ABX
    public void TryOpeningDelimiterAt_ReadsEachOpeningDelimiter(string text, int pos, char prefix, int prefixLength)
    {
        Assert.True(CobolSourceText.TryOpeningDelimiterAt(text, pos, out var opening));
        Assert.Equal(new OpeningDelimiter(prefix, prefixLength), opening);
    }

    [Theory]
    [InlineData("ABX\"41\"", 2)]   // X continues the word ABX: the lexer reads ABX, then the literal "41"
    [InlineData("A-N\"41\"", 2)]   // a hyphen is a word character too
    [InlineData("XN\"41\"", 0)]    // XN is no prefix
    [InlineData("NN'1'", 1)]       // the second N continues the word NN
    [InlineData("X 41", 0)]        // a prefix letter needs its quotation symbol at once
    [InlineData("NX", 0)]          // ... and the text may end first
    [InlineData("AB", 0)]
    public void TryOpeningDelimiterAt_RefusesWhatOpensNoLiteral(string text, int pos)
        => Assert.False(CobolSourceText.TryOpeningDelimiterAt(text, pos, out _));

    [Theory]
    [InlineData("\"AB\" C", 1, 3)]
    [InlineData("\"A\"\"B\" C", 1, 5)]    // the doubled quotation symbol is content
    [InlineData("'A\"B' C", 1, 4)]        // the other quotation symbol is content too
    [InlineData("\"AB", 1, 3)]            // unterminated: the text's length
    [InlineData("\"AB\"", 1, 3)]          // the last character: returned, the caller decides a continuation
    [InlineData("\"A\"\"", 1, 4)]         // a doubled pair that ends the text leaves the literal open
    public void ClosingDelimiterAt_SkipsEveryDoubledQuotationSymbol(string text, int from, int expected)
        => Assert.Equal(expected, CobolSourceText.ClosingDelimiterAt(text, from, text[0]));

    [Theory]
    [InlineData("'IT''S' X", 0, 7)]
    [InlineData("'IT''S", 0, 6)]
    [InlineData("A \"\" B", 2, 4)]        // the zero-length literal (§8.3.3.1: contiguous delimiters)
    public void LiteralEnd_IsPastTheClosingDelimiter(string text, int quotePos, int expected)
        => Assert.Equal(expected, CobolSourceText.LiteralEnd(text, quotePos));

    [Theory]
    [InlineData("'IT''S'", 0, "IT'S", 6)]
    [InlineData("N\"A\"\"\"\"B\"", 1, "A\"\"B", 8)]
    [InlineData("\"A'B\"", 0, "A'B", 4)]
    [InlineData("\"OPEN", 0, "OPEN", 5)]
    [InlineData("\"\"", 0, "", 1)]
    public void LiteralContent_ReadsEachDoubledQuotationSymbolAsOne(string text, int quotePos, string content, int closing)
    {
        Assert.Equal(content, CobolSourceText.LiteralContent(text, quotePos, out int at));
        Assert.Equal(closing, at);
    }

    [Theory]
    [InlineData("ON *> why", 3)]
    [InlineData("\"A *> B\" *> why", 9)]   // the one inside the literal is content
    [InlineData("'IT''S *>' X", 12)]       // a doubled quotation symbol does not end the literal
    [InlineData("\"OPEN *>", 8)]           // an unterminated literal runs to the end of the line
    [InlineData("A*>B", 1)]
    [InlineData("", 0)]
    public void InlineCommentAt_FindsTheFirstIndicatorOutsideALiteral(string text, int expected)
        => Assert.Equal(expected, CobolSourceText.InlineCommentAt(text));

    [Fact]
    public void CommentIndicatorAt_IsTheStarThenGreaterThan()
    {
        Assert.Equal("*>", CobolSourceText.CommentIndicator);
        Assert.True(CobolSourceText.CommentIndicatorAt("A *> B", 2));
        Assert.False(CobolSourceText.CommentIndicatorAt("A * > B", 2));
        Assert.False(CobolSourceText.CommentIndicatorAt("A *", 2));
        Assert.True(CobolSourceText.IsQuotationSymbol('"') && CobolSourceText.IsQuotationSymbol('\''));
        Assert.False(CobolSourceText.IsQuotationSymbol('`'));
    }
}
