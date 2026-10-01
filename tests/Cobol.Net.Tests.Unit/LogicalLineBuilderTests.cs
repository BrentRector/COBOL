// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// THE §6.5 LOGICAL-LINE BUILDER, one rule at a time, THE SAME CASE THROUGH BOTH REFERENCE FORMATS (kb/Work PB1804,
/// PB1359, PB1492). Fixed and free form each supply only a per-line classifier to ONE builder, so a continuation
/// rule is written once; a case that gives a different logical line in the two formats is a defect, which is why
/// each theory row below builds the fixed-form source and the free-form source from the same text lines and asserts
/// ONE expected result for both.
/// </summary>
public sealed class LogicalLineBuilderTests
{
    private static (MappedText Text, DiagnosticBag Bag) Normalize(string source, InitialReferenceFormat format, int std = 2023,
        bool permissive = false, bool ccvs = false, string file = "t.cob")
    {
        var bag = new DiagnosticBag();
        var gates = new ReferenceFormatDiagnostics(std, permissive, bag);
        return (ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, gates, file, format.InitialFixed(), out _, ccvs), bag);
    }

    /// <summary>The same program-text lines as a fixed-form source (sequence area, a space indicator, the text at
    /// column 8) or a free-form source (the text as written).</summary>
    private static string Lines(bool fixedForm, params string[] text)
        => string.Concat(text.Select((t, i) => (fixedForm ? $"{(i + 1) * 100:D6} " + t : t) + "\n"));

    private static InitialReferenceFormat Format(bool fixedForm) => fixedForm ? InitialReferenceFormat.Fixed : InitialReferenceFormat.Free;

    private static string[] Codes(DiagnosticBag bag) => bag.Diagnostics.Select(d => d.Code).ToArray();

    [Theory] // §6.5 4) + 8): the floating indicator ends the line's program text before it; the continuation line's
             // content after its initial quotation symbol is appended to the latest logical line.
    [InlineData(true)]
    [InlineData(false)]
    public void FloatingContinuation_JoinsTheLiteral(bool fixedForm)
    {
        var (m, bag) = Normalize(Lines(fixedForm, "01 X PIC X(20) VALUE \"ABC\"-", "    \"DEF\"."), Format(fixedForm), std: 2002);
        Assert.Equal("01 X PIC X(20) VALUE \"ABCDEF\".", m.Text.Split('\n')[0]);
        Assert.Equal(1, m.Lines[0].Line);
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Theory] // §6.5 4): trailing spaces BEFORE the indicator are literal content; "The continuation indicator may
             // optionally be followed by one or more spaces" (§6.4.2).
    [InlineData(true)]
    [InlineData(false)]
    public void FloatingContinuation_KeepsSpacesBeforeTheIndicator_AndIgnoresSpacesAfterIt(bool fixedForm)
    {
        var (m, bag) = Normalize(Lines(fixedForm, "DISPLAY \"AB  \"-   ", "    \"CD\"."), Format(fixedForm), std: 2002);
        Assert.Equal("DISPLAY \"AB  CD\".", m.Text.Split('\n')[0]);
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Theory] // §6.3.5 / §6.4.2: comment lines and blank lines may be interspersed; their slots are kept, and the
             // continuation line still occupies no resultant line of its own.
    [InlineData(true)]
    [InlineData(false)]
    public void FloatingContinuation_SkipsCommentAndBlankLines(bool fixedForm)
    {
        string src = fixedForm
            ? "000100 DISPLAY \"AB\"-\n000200* a comment\n000300\n000400     \"CD\"-\n000500     \"EF\".\n000600     STOP RUN.\n"
            : "DISPLAY \"AB\"-\n*> a comment\n\n    \"CD\"-\n    \"EF\".\nSTOP RUN.\n";
        var (m, bag) = Normalize(src, Format(fixedForm), std: 2002);
        string[] lines = m.Text.Split('\n');
        Assert.Equal("DISPLAY \"ABCDEF\".", lines[0].Trim());
        Assert.Equal("", lines[1]);                     // the comment line's slot
        Assert.Equal("", lines[2]);                     // the blank line's slot
        Assert.Equal("STOP RUN.", lines[3].Trim());
        Assert.Equal(6, m.Lines[3].Line);               // the two continuation lines took no slot, the numbering is physical
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Theory] // §6.2.3.1: the indicator's quotation symbol is the one that opened the literal; the other symbol is content.
    [InlineData("MOVE 'IT\"S'-", "    'OK' TO X.", "MOVE 'IT\"SOK' TO X.")]
    [InlineData("MOVE \"IT'S\"-", "    \"OK\" TO X.", "MOVE \"IT'SOK\" TO X.")]
    [InlineData("MOVE X\"4142\"-", "    \"43\" TO X.", "MOVE X\"414243\" TO X.")]
    [InlineData("MOVE B\"0101\"-", "    \"1111\" TO X.", "MOVE B\"01011111\" TO X.")]
    [InlineData("MOVE N\"AB\"-", "    \"CD\" TO X.", "MOVE N\"ABCD\" TO X.")]
    [InlineData("MOVE NX\"0041\"-", "    \"0042\" TO X.", "MOVE NX\"00410042\" TO X.")]
    public void FloatingContinuation_EveryLiteralKind_BothQuotationSymbols(string first, string second, string joined)
    {
        foreach (bool fixedForm in new[] { true, false })
        {
            var (m, bag) = Normalize(Lines(fixedForm, first, second), Format(fixedForm), std: 2002);
            Assert.Equal(joined, m.Text.Split('\n')[0]);
            Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
        }
    }

    [Theory] // A literal is a quotation symbol followed by a hyphen at the END of the line only: a hyphen with anything but
             // spaces after it is a closed literal (and a separator error of its own, COBOLNET2633, from a later stage).
    [InlineData(true)]
    [InlineData(false)]
    public void ClosedLiteralFollowedByHyphenAndText_IsNotAContinuationIndicator(bool fixedForm)
    {
        var (m, bag) = Normalize(Lines(fixedForm, "DISPLAY \"A\"- B."), Format(fixedForm), std: 2002);
        Assert.Equal("DISPLAY \"A\"- B.", m.Text.Split('\n')[0]);
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    /// <summary>One negative per rule: the source lines, and the diagnostic they must draw in BOTH formats (the fixed-form
    /// indicator column cases are separate, below).</summary>
    public static TheoryData<string, string, string, string> BothFormatsNegatives => new()
    {
        // §6.2.3.2 SR6 / §6.4.2 / §6.5 8): the first nonblank character of the continuation line is not the opening quote.
        { "DISPLAY \"AB\"-", "    CD\".", "COBOLNET2684", "no quotation symbol" },
        { "DISPLAY \"AB\"-", "    'CD'.", "COBOLNET2684", "the other quotation symbol" },
        // §6.3.7.3 / §6.4.4.3: an inline comment on a line that holds a floating literal continuation indicator.
        { "DISPLAY \"AB\"- *> comment", "    \"CD\".", "COBOLNET2689", "comment after the indicator" },
    };

    [Theory]
    [MemberData(nameof(BothFormatsNegatives))]
    public void RuleViolations_AreDiagnosedByName_InBothFormats(string first, string second, string code, string why)
    {
        foreach (bool fixedForm in new[] { true, false })
        {
            var (m, bag) = Normalize(Lines(fixedForm, first, second), Format(fixedForm), std: 2002);
            Assert.True(Codes(bag).Contains(code), $"{code} ({why}), {(fixedForm ? "fixed" : "free")}: {string.Join(",", Codes(bag))}");
            Assert.StartsWith("DISPLAY \"ABCD", m.Text.Split('\n')[0]);   // still read as ONE logical line
        }
    }

    [Fact] // §6.4.2: "At least one … of the literal content shall be specified on the continued line and on each continuation
           // line" — a free-form rule.
    public void FreeForm_ContinuationLineWithNoContent_IsDiagnosed()
    {
        Assert.Contains("COBOLNET2690", Codes(Normalize("DISPLAY \"AB\"-\n    \"\".\n", InitialReferenceFormat.Free, 2002).Bag));
        Assert.Contains("COBOLNET2690", Codes(Normalize("DISPLAY \"\"-\n    \"AB\".\n", InitialReferenceFormat.Free, 2002).Bag));
        Assert.DoesNotContain("COBOLNET2690", Codes(Normalize("DISPLAY \"AB\"-\n    \"\"\"\".\n", InitialReferenceFormat.Free, 2002).Bag));
    }

    [Fact] // §6.2.3.2 SR5: a floating indicator on a line that holds the fixed continuation indicator.
    public void FloatingIndicator_OnAFixedContinuationLine_IsDiagnosed()
    {
        string open = "000100 DISPLAY \"" + new string('A', 56);
        Assert.Equal(72, open.Length);
        var (_, bag) = Normalize(open + "\n000200-    \"CD\"-\n000300     \"EF\".\n", InitialReferenceFormat.Fixed, 2002);
        Assert.Contains("COBOLNET2686", Codes(bag));
    }

    [Fact] // §6.2.3.2 SR4: one form of continuation per literal — either order.
    public void LiteralContinuedWithBothForms_IsDiagnosed()
    {
        string open = "000100 DISPLAY \"" + new string('A', 56);
        var (_, floatingThenFixed) = Normalize("000100 DISPLAY \"AB\"-\n000200-    \"CD\".\n", InitialReferenceFormat.Fixed, 2002);
        Assert.Contains("COBOLNET2687", Codes(floatingThenFixed));
        // floating on line 1, a continuation line that ends open at margin R, then a hyphen line: the literal was
        // continued by the floating form, and now by the fixed one.
        string middle = "000200 \"" + new string('B', 64);
        Assert.Equal(72, middle.Length);
        var (_, viaMargin) = Normalize("000100 DISPLAY \"AB\"-\n" + middle + "\n000300-    \"CD\".\n", InitialReferenceFormat.Fixed, 2002);
        Assert.Contains("COBOLNET2687", Codes(viaMargin));
        Assert.DoesNotContain("COBOLNET2687", Codes(Normalize(open + "\n000200-    \"CD\".\n", InitialReferenceFormat.Fixed, 2002).Bag));
    }

    [Theory] // §6.3.5 2): "National literals may be continued only with a floating literal continuation indicator."
    [InlineData("N\"", true)]
    [InlineData("NX\"", true)]
    [InlineData("\"", false)]
    [InlineData("B\"", false)]
    [InlineData("X\"", false)]
    public void NationalLiteral_FixedContinuation_IsDiagnosed(string opening, bool diagnosed)
    {
        string head = "000100 01 X PIC N(80) VALUE " + opening + new string('A', 72 - 28 - opening.Length);
        Assert.Equal(72, head.Length);
        var (_, bag) = Normalize(head + "\n000200-    \"CD\".\n", InitialReferenceFormat.Fixed, 2002);
        Assert.Equal(diagnosed, Codes(bag).Contains("COBOLNET2685"));
    }

    [Theory] // §6.3.5 2): the join must not SPELL a multiple-character separator, invocation operator or indicator.
    [InlineData("000100     REPLACE =\n000200-    =A== BY ==B==.\n", "COBOLNET2688")]
    [InlineData("000100     INVOKE X :\n000200-    :M.\n", "COBOLNET2688")]
    [InlineData("000100     MOVE A TO X *\n000200-    > c\n", "COBOLNET2496")]
    public void MultipleCharacterToken_SplitAcrossTheJoin_IsDiagnosed(string src, string code)
        => Assert.Contains(code, Codes(Normalize(src, InitialReferenceFormat.Fixed, 2002).Bag));

    [Theory] // The 2002 introduction gate (floating-literal-continuation-2002): fixed form only, once per compilation.
    [InlineData(true, 85, 1)]
    [InlineData(true, 2002, 0)]
    [InlineData(false, 85, 0)]   // free form is itself a 2002 introduction, reached below 2002 only through the extension
    public void FloatingContinuation_IntroductionGate(bool fixedForm, int std, int expected)
    {
        var (_, bag) = Normalize(Lines(fixedForm, "DISPLAY \"A\"-", "    \"B\".", "DISPLAY \"C\"-", "    \"D\"."), Format(fixedForm), std);
        Assert.Equal(expected, Codes(bag).Count(c => c == "COBOLNET0900"));
    }

    [Theory] // kb/Work R61, row debugging-line-removed-2014: the D indicator is accepted at 85, obsolete at 2002 (the
             // obsolete warning, COBOLNET0903), removed at 2014 (COBOLNET0902, an error strict, a warning permissive) —
             // once per compilation; the NIST S / Y debugging letters are the CCVS dialect and never gated.
    [InlineData(85, false, "D", false, null, false)]
    [InlineData(2002, false, "D", false, "COBOLNET0903", false)]
    [InlineData(2014, false, "D", false, "COBOLNET0902", true)]
    [InlineData(2023, false, "D", false, "COBOLNET0902", true)]
    [InlineData(2023, true, "D", false, "COBOLNET0902", false)]
    [InlineData(2023, false, "S", true, null, false)]
    [InlineData(2023, false, "Y", true, null, false)]
    public void DebuggingLine_EditionWindow(int std, bool permissive, string indicator, bool ccvs, string? code, bool isError)
    {
        var (m, bag) = Normalize("000100" + indicator + "    DISPLAY \"A\".\n000200" + indicator + "    DISPLAY \"B\".\n", InitialReferenceFormat.Fixed,
            std, permissive, ccvs);
        var found = bag.Diagnostics.Where(d => d.Code is "COBOLNET0902" or "COBOLNET0903").ToList();
        Assert.Equal(code is null ? 0 : 1, found.Count);                       // once per compilation
        if (code is not null)
        {
            Assert.Equal(code, found[0].Code);
            Assert.Equal(isError, found[0].IsError);
        }
        Assert.StartsWith(ReferenceFormatProcessor.DebugLineCarrier, m.Text.Split('\n')[0]);   // a debugging line stays a comment
    }

    [Theory] // kb/Work PB1494 / PB1758: the comment-entry paragraph HEADER stays program text (with its terminating period
             // written explicitly) and only the comment-entry is discarded — at every edition, so the one removal gate sees
             // a fixed-form paragraph as it sees a free-form one.
    [InlineData("AUTHOR")]
    [InlineData("INSTALLATION")]
    [InlineData("DATE-WRITTEN")]
    [InlineData("DATE-COMPILED")]
    [InlineData("SECURITY")]
    [InlineData("REMARKS")]
    public void CommentEntryParagraph_KeepsItsHeader(string word)
    {
        string src = "000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n000300 " + word + ". FIRST \"LINE,\n"
                   + "000400     SECOND LINE. WITH A PERIOD\n000500-    AND A CONTINUATION\n000600 PROCEDURE DIVISION.\n";
        var (m, bag) = Normalize(src, InitialReferenceFormat.Fixed, 85);
        string[] lines = m.Text.Split('\n');
        Assert.Equal(word + ". .", lines[2]);
        Assert.Equal(["", ""], lines[3..5]);
        Assert.Equal("PROCEDURE DIVISION.", lines[5].Trim());
        Assert.Empty(bag.Diagnostics);
    }

    [Theory] // kb/Work PB1494: library text is converted in the division its COPY statement stands in — in a PROCEDURE
             // DIVISION a paragraph named REMARKS is program text (§6.5 5)), in an IDENTIFICATION DIVISION it is the
             // comment-entry paragraph.
    [InlineData(true, "REMARKS. .", "")]
    [InlineData(false, "REMARKS.", "DISPLAY \"X\".")]
    public void LibraryText_IsConvertedInTheDivisionItsCopyStatementStandsIn(bool inIdentification, string header, string body)
    {
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped("000100 REMARKS.\n000200     DISPLAY \"X\".\n", null, "b.cpy",
            initialFixed: true, out _, startsInIdentificationDivision: inIdentification);
        string[] lines = m.Text.Split('\n');
        Assert.Equal(header, lines[0]);
        Assert.Equal(body, lines[1].Trim());
    }

    [Fact] // the division state crosses a SOURCE FORMAT segment boundary: a fixed segment that follows a free one is read
           // in the division the text had reached, not reset to the identification division
    public void DivisionState_IsCarriedAcrossFixedSegments()
    {
        string src = "000100 PROCEDURE DIVISION.\n       >>SOURCE FORMAT FREE\nDISPLAY \"A\".\n>>SOURCE FORMAT FIXED\n000200 REMARKS.\n"
                   + "000300     DISPLAY \"B\".\n";
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(src, null, "t.cob", InitialReferenceFormat.Fixed);
        string[] lines = m.Text.Split('\n');
        Assert.Equal("REMARKS.", lines[4]);              // a procedure paragraph: kept as written, with its body
        Assert.Equal("DISPLAY \"B\".", lines[5].Trim());
    }

    [Fact] // kb/Work PB1640: ONE ReferenceFormatDiagnostics serves the main source and its library text, sited at each
           // file — a once-per-compilation gate fires once, a syntax rule is reported at the copybook's own file and line.
    public void OneGatesInstance_ServesEveryFile_SitedAtTheFileThatBrokeTheRule()
    {
        var bag = new DiagnosticBag();
        var gates = new ReferenceFormatDiagnostics(2023, false, bag);
        string hyphen = "000100     MOVE 1 TO X\n000200-    .\n";
        ReferenceFormatProcessor.NormalizeToFreeFormMapped(hyphen, gates, "main.cob", InitialReferenceFormat.Fixed);
        ReferenceFormatProcessor.NormalizeToFreeFormMapped(hyphen + "000300     MOVE A TO X*> c\n", gates, "book.cpy",
            InitialReferenceFormat.Fixed);
        Assert.Single(bag.Diagnostics, d => d.Code == "COBOLNET0903");           // the first use, in the main source
        var unseparated = Assert.Single(bag.Diagnostics, d => d.Code == "COBOLNET2495");
        Assert.Equal("book.cpy", unseparated.Location.FileName);
        Assert.Equal(2, unseparated.Location.Line);                              // 0-based: physical line 3 of the copybook
    }
}
