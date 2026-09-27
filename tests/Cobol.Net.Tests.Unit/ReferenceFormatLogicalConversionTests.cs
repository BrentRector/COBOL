// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ISO §6.5 logical conversion as ONE pass (kb/Work PB1491, PB1493): a fixed-form continuation joins the LATEST
/// LOGICAL line, comment and blank lines are logically discarded without touching a continued literal, an inline
/// comment is removed on every line, the program-text area runs to margin R whatever the record length, and the
/// floating comment indicator is recognized — with its §6.2.3.2 syntax rules and its 2002 introduction gate — here and
/// nowhere else. The goldens 85/pb1491_continued_literal_logical_line and 2002/pb1493_floating_comment_* run the same
/// rules end to end; these pin the resultant TEXT and LINE MAP the later stages read.
/// </summary>
public sealed class ReferenceFormatLogicalConversionTests
{
    private static (MappedText Text, DiagnosticBag Bag) Convert(string source, int std = 2023)
    {
        var bag = new DiagnosticBag();
        return (ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, std, permissive: false, bag, "t.cob"), bag);
    }

    private static string[] Codes(DiagnosticBag bag) => bag.Diagnostics.Select(d => d.Code).ToArray();

    [Theory] // §6.3.5 "Comment lines and blank lines may be interspersed among lines containing the parts of a literal".
    [InlineData("000600* a comment with a \"quote")]
    [InlineData("000600/ a page-eject comment")]
    [InlineData("")]                                   // an EMPTY line — crashed the PB82 origin tracking before PB1491
    [InlineData("000600")]                             // §6.3.6: only spaces between margin C and margin R
    [InlineData("000600     *> a floating comment line")]
    public void ContinuedLiteral_JoinsAcrossADiscardedLine(string between)
    {
        string src = "000100 IDENTIFICATION DIVISION.\n"
                   + "000500 01 X PIC X(80) VALUE \"AB\n"
                   + between + "\n"
                   + "000700-    \"CD\".\n"
                   + "000800 01 Y PIC X.\n";
        var (m, bag) = Convert(src);
        var lines = m.Text.Split('\n');
        // §6.5 6) a): appended to the LATEST LOGICAL line, the continued line space-filled to margin R (DOC-A.1-157)
        Assert.Equal("01 X PIC X(80) VALUE \"AB" + new string(' ', 41) + "CD\".", lines[1]);
        Assert.Equal(2, m.Lines[1].Line);
        Assert.Equal("", lines[2]);                    // §6.5 2): the interspersed line is discarded, its slot kept
        Assert.Equal(3, m.Lines[2].Line);
        Assert.Equal("01 Y PIC X.", lines[3].Trim()); // the continuation occupies no line; Y keeps its number
        Assert.Equal(5, m.Lines[3].Line);
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Fact] // §6.5 3) on a CONTINUATION line: the comment goes before the next continuation is appended to the line.
    public void InlineCommentOnAContinuationLine_IsRemovedBeforeTheNextJoin()
    {
        string src = "000100 01 X PIC X(8) VALUE\n"
                   + "000200-     \"CD\" *> a comment \"with a quote\n"
                   + "000300-    .\n";
        var (m, _) = Convert(src);
        Assert.Equal("01 X PIC X(8) VALUE\"CD\".", m.Text.Split('\n')[0]);
    }

    [Fact] // §6.3.4 1) / §6.5 2): no comment-text reaches a later stage — the lexer's picture mode included.
    public void CommentLines_BecomeEmpty_InBothReferenceFormats()
    {
        var (fixedForm, _) = Convert("000100 01 X PIC\n000200* comment\n000300     X(4).\n");
        Assert.Equal(new[] { "01 X PIC", "", "X(4)." }, fixedForm.Text.Split('\n').Take(3).Select(l => l.Trim()));
        var (free, _) = Convert(">>SOURCE FORMAT FREE\n01 X PIC\n*> comment\n   X(4). *> inline \"quote\n");
        Assert.Equal(new[] { "", "01 X PIC", "", "X(4)." }, free.Text.Split('\n').Take(4).Select(l => l.Trim()));
    }

    [Fact] // A continued line whose literal ends on a quotation symbol AT margin R: the continuation's first quotation
           // symbol completes the doubled pair (content) and the one after it opens the continuation (stripped).
    public void QuotationSymbolAtMarginR_PairsWithTheContinuation()
    {
        string head = "000100 01 X PIC X(80) VALUE \"" + new string('A', 42) + "\"";
        Assert.Equal(72, head.Length);
        var (m, _) = Convert(head + "\n000200-    \"\"Z\".\n");
        Assert.Equal("01 X PIC X(80) VALUE \"" + new string('A', 42) + "\"\"Z\".", m.Text.Split('\n')[0]);
    }

    [Fact] // A *> inside a literal is literal content; a doubled quotation symbol does not end the literal.
    public void FloatingIndicatorInsideALiteral_IsContent()
    {
        var (m, _) = Convert(">>SOURCE FORMAT FREE\nDISPLAY \"A\"\"*>B\" *> gone\n");
        Assert.Equal("DISPLAY \"A\"\"*>B\"", m.Text.Split('\n')[1]);
    }

    [Theory] // §6.2.3.2 SR2 — COBOLNET2495, in either reference format; *> after a space is not diagnosed.
    [InlineData(">>SOURCE FORMAT FREE\nMOVE A TO X*> c\n", true)]
    [InlineData("000100     MOVE A TO X*> c\n", true)]
    [InlineData(">>SOURCE FORMAT FREE\nMOVE A TO X *>c\n", false)]
    [InlineData(">>SOURCE FORMAT FREE\n*>comment line\n", false)]
    public void FloatingComment_NotPrecededBySeparatorSpace_IsDiagnosed(string src, bool diagnosed)
        => Assert.Equal(diagnosed, Codes(Convert(src).Bag).Contains("COBOLNET2495"));

    [Theory] // §6.2.3.2 SR3 — COBOLNET2496 when a continuation completes *> or >> begun on the line it continues.
    [InlineData("000100     MOVE A TO X *\n000200-    > comment\n", true)]
    [InlineData("000100     MOVE A TO X >\n000200-    >IF\n", true)]
    [InlineData("000100     MOVE A TO X\n000200-    > comment\n", false)]
    public void FloatingIndicator_SplitAcrossTheJoin_IsDiagnosed(string src, bool diagnosed)
        => Assert.Equal(diagnosed, Codes(Convert(src).Bag).Contains("COBOLNET2496"));

    [Theory] // The 2002 introduction gate (floating-comment-indicator-2002) — fixed form only, once per compilation.
    [InlineData("000100     MOVE A TO X. *> c\n000200     MOVE A TO X. *> d\n", 85, 1)]
    [InlineData("000100     MOVE A TO X. *> c\n", 2002, 0)]
    [InlineData("000100* a fixed comment line is COBOL-85\n000200     MOVE A TO X.\n", 85, 0)]
    [InlineData("MOVE A TO X. *> c\n", 85, 0)]   // auto-detected free form (a >>SOURCE line would be gated itself)
    public void FloatingComment_IntroductionGate_FixedFormOnly(string src, int std, int expected)
        => Assert.Equal(expected, Codes(Convert(src, std).Bag).Count(c => c == "COBOLNET0900"));
}
