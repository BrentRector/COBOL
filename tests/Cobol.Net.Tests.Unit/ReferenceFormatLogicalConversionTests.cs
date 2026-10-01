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
    private static (MappedText Text, DiagnosticBag Bag) Convert(string source, int std = 2023,
        InitialReferenceFormat initial = InitialReferenceFormat.Fixed)
    {
        var bag = new DiagnosticBag();
        return (ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, new ReferenceFormatDiagnostics(std, false, bag), "t.cob", initial), bag);
    }

    private static string[] Codes(DiagnosticBag bag) => bag.Diagnostics.Select(d => d.Code).ToArray();

    [Theory] // kb/Work PB1362 — §7.3.24.3 2) "The default reference format of a compilation group is fixed form":
    // with no directive and no selection, a non-numeric or blank sequence area and text past margin R stay fixed.
    [InlineData("ABC001 DISPLAY \"X\".\n", "DISPLAY \"X\".")]
    [InlineData("       DISPLAY \"X\".                                                     TAG00001\n", "DISPLAY \"X\".")]
    public void NoDirective_NoSelection_IsReadInFixedForm(string src, string expectedLine)
        => Assert.Equal(expectedLine, Convert(src).Text.Text.Split('\n')[0].Trim());

    [Fact] // kb/Work PB1362 — the Free selection reads the same text from column 1 (the §4.2.10 3) mechanism).
    public void FreeSelection_ReadsFromColumnOne()
        => Assert.Equal("ABC001 DISPLAY \"X\".",
            Convert("ABC001 DISPLAY \"X\".\n", initial: InitialReferenceFormat.Free).Text.Text.Split('\n')[0]);

    [Theory] // kb/Work PB1361 — §7.3.3 SR2/SR3 + §6.3.2: a directive is recognized in the program-text area OF THE
    // FORMAT IN EFFECT. Fixed: positions 8-72 of a source line, the sequence area holding anything; free: the line.
    // Each row: the text, then the resultant first two lines — a consumed directive leaves an empty slot and the next
    // line is read FREE (from column 1); otherwise the next line is read FIXED (from column 8).
    [InlineData("SEQ001 >>SOURCE FORMAT FREE\nDISPLAY \"F\".\n", "", "DISPLAY \"F\".")]               // letters in cols 1-6
    // program text before `>>`: not a directive (the next line is read FIXED), and §7.3.3 SR2 names it — COBOLNET2691 —
    // cutting the directive text (kb/Work PB1690)
    [InlineData("       12 >>SOURCE FORMAT FREE\n       DISPLAY \"F\".\n", "12", "DISPLAY \"F\".")]
    [InlineData("      *>>SOURCE FORMAT FREE\n     X DISPLAY \"F\".\n", "", "DISPLAY \"F\".")]           // a comment line
    public void FixedFormDirective_IsRecognizedInTheProgramTextArea(string src, string first, string second)
    {
        string[] lines = Convert(src, std: 2002).Text.Text.Split('\n');
        Assert.Equal(first, lines[0].TrimEnd());
        Assert.Equal(second, lines[1].TrimEnd());
    }

    [Theory] // kb/Work PB1361 — in free form the whole line is the program-text area (§6.4.1): a directive past column
    // 72 switches the format, and a sequence number before `>>` makes the line program text, not a directive.
    [InlineData(74, "000100     DISPLAY \"G\".", "DISPLAY \"G\".")]
    [InlineData(0, "000100     DISPLAY \"G\".", "DISPLAY \"G\".")]
    public void FreeFormDirective_AnywhereAfterSpaces_Switches(int indent, string fixedLine, string expected)
    {
        string src = ">>SOURCE FORMAT FREE\n" + new string(' ', indent) + ">>SOURCE FORMAT FIXED\n" + fixedLine + "\n";
        Assert.Equal(expected, Convert(src, std: 2002).Text.Text.Split('\n')[2].Trim());
    }

    [Theory] // kb/Work PB1494 — §6.3.3 / §6.2.2: a character that is not a fixed indicator is COBOLNET2616 and the line
    // is read as source; the NIST CCVS letters keep their CCVS meaning only under --nist (ccvsIndicators).
    [InlineData('E', false, true, "DISPLAY \"L\".")]
    [InlineData('X', false, true, "DISPLAY \"L\".")]
    [InlineData('E', true, false, "")]                 // CCVS: an excluded alternate line (discarded)
    [InlineData('X', true, false, "DISPLAY \"L\".")]   // CCVS: a primary-configuration line
    [InlineData('D', false, false, "<debug>DISPLAY \"L\".")]   // COBOL-85's debugging line, with or without --nist
    public void IndicatorArea_OnlyFixedIndicators_OutsideNist(char indicator, bool ccvs, bool diagnosed, string line)
    {
        var bag = new DiagnosticBag();
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped("000100" + indicator + "DISPLAY \"L\".\n",
            new ReferenceFormatDiagnostics(2023, false, bag), "t.cob", initialFixed: true, out _, ccvsIndicators: ccvs);
        Assert.Equal(diagnosed, Codes(bag).Contains("COBOLNET2616"));
        Assert.Equal(line.Replace("<debug>", ReferenceFormatProcessor.DebugLineCarrier), m.Text.Split('\n')[0]);
    }

    [Fact] // kb/Work PB1494 — §6.5 5): a source line's program-text area is copied. The comment-entry reading of
    // AUTHOR / INSTALLATION / DATE-WRITTEN / DATE-COMPILED / SECURITY / REMARKS belongs to the IDENTIFICATION DIVISION;
    // a PROCEDURE DIVISION paragraph so named and the lines after it were silently dropped.
    public void CommentEntryParagraphs_AreOnlyInTheIdentificationDivision()
    {
        string src = "000100 IDENTIFICATION DIVISION.\n"
                   + "000200 PROGRAM-ID. P.\n"
                   + "000300 AUTHOR. ANY TEXT, \"EVEN QUOTES.\n"
                   + "000400     MORE COMMENT-ENTRY TEXT.\n"
                   + "000500 PROCEDURE DIVISION.\n"
                   + "000600 REMARKS.\n"
                   + "000700     DISPLAY \"IN-REMARKS\".\n";
        string[] lines = Convert(src).Text.Text.Split('\n');
        // the comment-entry is commentary, discarded; the paragraph HEADER stays program text, its terminating period
        // written explicitly (kb/Work PB1494, PB1758), so the parser and the removal gate see the paragraph
        Assert.Equal(["AUTHOR. .", ""], lines[2..4]);
        Assert.Equal("REMARKS.", lines[5]);                            // a procedure paragraph is program text
        Assert.Equal("DISPLAY \"IN-REMARKS\".", lines[6].Trim());
    }

    [Fact] // kb/Work PB1361 + PB1690 — §7.3.3 SR2: in free form "000100 >>SOURCE ..." is NOT a directive (the sequence
           // number is program text, so the format is not switched) and is diagnosed by name, COBOLNET2691; the directive
           // text is cut so the parser does not add a nameless second error.
    public void FreeFormLine_WithTextBeforeTheIndicator_IsNotADirective_AndIsDiagnosed()
    {
        var (m, bag) = Convert(">>SOURCE FORMAT FREE\n000100 >>SOURCE FORMAT FIXED\n01 A PIC X.\n", std: 2002);
        Assert.Equal("000100", m.Text.Split('\n')[1].Trim());
        Assert.Equal("01 A PIC X.", m.Text.Split('\n')[2]);         // still read in FREE form: nothing was switched
        Assert.Equal(["COBOLNET2691"], Codes(bag));
    }

    [Theory] // §7.3.3 SR2 — a directive after program text is diagnosed in either reference format; a `>>` that is a
             // literal's content, a comment's, or not followed by a directive word is not a directive.
    [InlineData("000100     DISPLAY A >>DEFINE X AS 1\n", true)]
    [InlineData(">>SOURCE FORMAT FREE\nDISPLAY A >> DEFINE X AS 1\n", true)]
    [InlineData("000100     >>DEFINE X AS 1\n", false)]              // a directive LINE: only spaces precede it
    [InlineData("000100     DISPLAY \">>DEFINE X AS 1\".\n", false)] // literal content
    [InlineData("000100     DISPLAY A. *> >>DEFINE X AS 1\n", false)] // comment text
    [InlineData("000100     DISPLAY A >>NOTADIRECTIVE\n", false)]    // no compiler-directive word follows
    [InlineData("000100     >>PAGE any text STOP RUN. >>TURN x\n", false)]   // comment-text of a directive LINE (§7.3.19.3 SR1)
    public void DirectiveAfterProgramText_IsDiagnosedByName(string src, bool diagnosed)
        => Assert.Equal(diagnosed, Codes(Convert(src).Bag).Contains("COBOLNET2691"));

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
    [InlineData("000100     MOVE A TO X. *> c\n000200     MOVE A TO X. *> d\n", 85, 1, InitialReferenceFormat.Fixed)]
    [InlineData("000100     MOVE A TO X. *> c\n", 2002, 0, InitialReferenceFormat.Fixed)]
    [InlineData("000100* a fixed comment line is COBOL-85\n000200     MOVE A TO X.\n", 85, 0, InitialReferenceFormat.Fixed)]
    // free form selected by --source-format (a >>SOURCE line would be gated itself); kb/Work PB1362
    [InlineData("MOVE A TO X. *> c\n", 85, 0, InitialReferenceFormat.Free)]
    public void FloatingComment_IntroductionGate_FixedFormOnly(string src, int std, int expected, InitialReferenceFormat initial)
        => Assert.Equal(expected, Codes(Convert(src, std, initial).Bag).Count(c => c == "COBOLNET0900"));
}
