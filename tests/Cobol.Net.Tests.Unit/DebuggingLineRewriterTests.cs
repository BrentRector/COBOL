// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1705 (owner decision R56): a fixed-form debugging line reaches the lexer as
/// <see cref="ReferenceFormatProcessor.DebugLineCarrier"/> plus its program text, and <see cref="DebuggingLineRewriter"/>
/// keeps its tokens (the line is SOURCE) or moves them to the <see cref="CobolLexer.ABSENT_DEBUG_LINE"/> channel (the line
/// is a COMMENT) according to the program's SOURCE-COMPUTER … WITH DEBUGGING MODE clause. The scope of the clause is
/// ISO §12.3.5.4 GR1: "All clauses of the SOURCE-COMPUTER paragraph apply to the source unit in which they are
/// explicitly or implicitly specified and to any source unit contained within that source unit" — the source unit, so a
/// sibling program does not inherit it and a contained one does.
/// </summary>
public sealed class DebuggingLineRewriterTests
{
    private static readonly string Carrier = ReferenceFormatProcessor.DebugLineCarrier;

    /// <summary>A debugging line as the logical conversion writes it.</summary>
    private static string D(string text) => Carrier + text;

    private static List<IToken> Rewritten(string[] lines, bool hideAll = false)
    {
        var stream = new CommonTokenStream(new CobolLexer(new AntlrInputStream(string.Join("\n", lines) + "\n")));
        if (hideAll) DebuggingLineRewriter.HideAll(stream);
        else DebuggingLineRewriter.Rewrite(stream);
        return [.. stream.GetTokens()];
    }

    /// <summary>The words of the debugging line whose text contains <paramref name="marker"/>, with the channel each token
    /// ended on — the default channel when the line is source, the absent-line channel when it is a comment.</summary>
    private static int[] ChannelsOf(List<IToken> tokens, string marker)
    {
        int at = tokens.FindIndex(t => t.Type == CobolLexer.DEBUG_LINE && Lines(tokens, t).Any(x => x.Text == marker));
        Assert.True(at >= 0, $"no debugging line holds the word {marker}");
        return [.. Lines(tokens, tokens[at]).Select(t => t.Channel)];
    }

    private static IEnumerable<IToken> Lines(List<IToken> tokens, IToken marker)
        => tokens.Skip(tokens.IndexOf(marker) + 1).TakeWhile(t => t.Type != TokenConstants.EOF && t.Line == marker.Line);

    private static void AssertSource(List<IToken> tokens, string marker)
        => Assert.All(ChannelsOf(tokens, marker), c => Assert.Equal(TokenConstants.DefaultChannel, c));

    private static void AssertComment(List<IToken> tokens, string marker)
        => Assert.All(ChannelsOf(tokens, marker), c => Assert.Equal(CobolLexer.ABSENT_DEBUG_LINE, c));

    [Fact]
    public void NoClause_TheLineIsAComment_AndTheMarkerStaysHidden()
    {
        var tokens = Rewritten(["PROGRAM-ID. P.", "SOURCE-COMPUTER. IBM-PC.", D("DISPLAY NOCLAUSE."), "STOP RUN."]);
        AssertComment(tokens, "NOCLAUSE");
        Assert.Equal(TokenConstants.HiddenChannel, tokens.Single(t => t.Type == CobolLexer.DEBUG_LINE).Channel);
        Assert.Contains(tokens, t => t.Text == "STOP" && t.Channel == TokenConstants.DefaultChannel);
    }

    [Theory]
    [InlineData("SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.")]
    [InlineData("SOURCE-COMPUTER. IBM-PC DEBUGGING MODE.")]                  // WITH is not underlined: optional
    [InlineData("SOURCE-COMPUTER. IBM-PC\n WITH DEBUGGING MODE\n .")]       // across lines
    public void TheClause_KeepsTheLineAsSource(string paragraph)
    {
        var tokens = Rewritten(["PROGRAM-ID. P.", .. paragraph.Split('\n'), D("DISPLAY CLAUSED."), "STOP RUN."]);
        AssertSource(tokens, "CLAUSED");
    }

    [Fact]
    public void ALineBeforeTheClause_InTheSameUnit_IsKept()
    {
        // The clause is the unit's, wherever its line stands relative to it.
        var tokens = Rewritten(["PROGRAM-ID. P.", D("DISPLAY EARLY."), "SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.", "STOP RUN."]);
        AssertSource(tokens, "EARLY");
    }

    [Fact]
    public void AContainedUnit_InheritsItsContainersClause_AndASiblingDoesNot()
    {
        var tokens = Rewritten([
            "PROGRAM-ID. OUTER.", "SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.", D("DISPLAY OUTERLINE."),
            "PROGRAM-ID. INNER.", D("DISPLAY INNERLINE."), "END PROGRAM INNER.",
            D("DISPLAY AFTERINNER."), "END PROGRAM OUTER.",
            "PROGRAM-ID. SIBLING.", D("DISPLAY SIBLINGLINE."), "END PROGRAM SIBLING."]);
        AssertSource(tokens, "OUTERLINE");
        AssertSource(tokens, "INNERLINE");        // §12.3.5.4 GR1: any source unit contained within it
        AssertSource(tokens, "AFTERINNER");       // back in the container once the contained unit ended
        AssertComment(tokens, "SIBLINGLINE");     // a separate unit names no clause
    }

    [Fact]
    public void AContainedUnitsOwnClause_DoesNotReachItsContainer()
    {
        var tokens = Rewritten([
            "PROGRAM-ID. OUTER.", D("DISPLAY OUTERLINE."),
            "PROGRAM-ID. INNER.", "SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.", D("DISPLAY INNERLINE."),
            "END PROGRAM INNER.", D("DISPLAY AFTERINNER."), "END PROGRAM OUTER."]);
        AssertComment(tokens, "OUTERLINE");
        AssertSource(tokens, "INNERLINE");
        AssertComment(tokens, "AFTERINNER");
    }

    [Fact]
    public void AClauseWrittenOnADebuggingLine_DoesNotDecideItsOwnLine()
    {
        var tokens = Rewritten(["PROGRAM-ID. P.", D("SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE."), D("DISPLAY SELF.")]);
        AssertComment(tokens, "SELF");
    }

    [Fact]
    public void AnEmptySourceComputerParagraph_DoesNotReadTheNextParagraphsWords()
    {
        // §12.3.5.3 SR1 lets the second period go with no computer-name: the scan ends at the next header word.
        var tokens = Rewritten(["PROGRAM-ID. P.", "SOURCE-COMPUTER.", "OBJECT-COMPUTER. IBM-PC DEBUGGING MODE.", D("DISPLAY SPILL.")]);
        AssertComment(tokens, "SPILL");
    }

    [Fact]
    public void HideAll_HidesEveryLine_ClauseOrNot()
    {
        var tokens = Rewritten(["PROGRAM-ID. P.", "SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.", D("DISPLAY FRAGMENT.")], hideAll: true);
        AssertComment(tokens, "FRAGMENT");
    }

    [Fact]
    public void ASourceWithNoDebuggingLine_IsLeftByteIdentical()
    {
        var stream = new CommonTokenStream(new CobolLexer(new AntlrInputStream("PROGRAM-ID. P.\nSTOP RUN.\n")));
        stream.Fill();
        var before = stream.GetTokens().ToArray();
        DebuggingLineRewriter.Rewrite(stream);
        Assert.True(before.SequenceEqual(stream.GetTokens()), "no token object may be replaced when no line is carried");
    }

    [Fact]
    public void ADebuggingLineInsideASubscript_StaysAComment_ForEveryReader()
    {
        // SUBSCRIPT mode keeps its own comment skip: the carrier never becomes a marker there, and the fragment re-parse of the
        // captured text (which hides every line) reads the same.
        var tokens = Rewritten(["MOVE A(1", D("+ 2"), ") TO B."]);
        Assert.DoesNotContain(tokens, t => t.Type == CobolLexer.DEBUG_LINE);
    }

    /// <summary>The carrier the logical conversion writes IS the spelling of the lexer's marker rule — one value, two
    /// places (a grammar literal cannot reference a C# constant), held equal here.</summary>
    [Fact]
    public void TheCarrier_IsExactlyTheLexersMarkerToken()
    {
        var first = new CobolLexer(new AntlrInputStream(Carrier + "DISPLAY X.")).NextToken();
        Assert.Equal(CobolLexer.DEBUG_LINE, first.Type);
        Assert.Equal(Carrier, first.Text);
        Assert.Equal(TokenConstants.HiddenChannel, first.Channel);
    }
}
