// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// COBOL-85's areas A and B (kb/Work PB3147; <c>AreaPlacementPass</c>). ANSI X3.23-1985 begins an area-A item — a
/// division, section or paragraph header, a level indicator, level-number 01 or 77, DECLARATIVES, END DECLARATIVES,
/// an end marker — on its line in area A (positions 8–11), and every other line's text in area B; ISO/IEC 1989:2002
/// removed the areas (ISO §6.3.1). Each case is compiled at --std 85, where the placement is refused, and at --std 2002,
/// where the same source is legal. The positions are the reference format's own (a tab advances to the next tab stop,
/// DOC-A.1-157) and travel through COPY (a header in library text is placed where the library text writes it).
/// </summary>
public sealed class AreaPlacementPassTests
{
    /// <summary>A fixed-form line: an empty sequence area, a space indicator, then <paramref name="programText"/> from
    /// position 8.</summary>
    private static string L(string programText) => "       " + programText;

    private static string Source(params string[] lines) => string.Join("\n", lines) + "\n";

    private static CompilerDriver.Result Check(int edition, string source, params (string Name, string Text)[] library)
    {
        string dir = Directory.CreateTempSubdirectory("areas").FullName;
        try
        {
            string src = Path.Combine(dir, "MAIN.cob");
            File.WriteAllText(src, source);
            foreach (var (name, text) in library) File.WriteAllText(Path.Combine(dir, name), text);
            return CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CopyPaths: [dir],
                CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    private static void AssertRejectedAt85Only(string source, string expected, params (string Name, string Text)[] library)
    {
        var at85 = Check(85, source, library);
        Assert.False(at85.Success);
        Assert.Contains(at85.Errors, e => e.Contains(expected, StringComparison.OrdinalIgnoreCase));   // a library file is named as the resolver spelled it
        var at2002 = Check(2002, source, library);
        Assert.True(at2002.Success, string.Join("\n", at2002.Errors));
    }

    [Fact] // the note's repro: a paragraph header written in column 12
    public void ParagraphHeaderInAreaB_IsRefusedAt85()
        => AssertRejectedAt85Only(Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARPARA."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA."),
            L("    DISPLAY \"A\"."),
            L("    NEXT-PARA."),
            L("    STOP RUN.")), "(6,12): error COBOLNET3734");

    [Fact] // an area-A item that does not BEGIN its line is not in area A either
    public void ParagraphHeaderAfterASentence_IsRefusedAt85()
        => AssertRejectedAt85Only(Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARMID."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA. DISPLAY \"A\". NEXT-PARA."),
            L("    STOP RUN.")), "(4,32): error COBOLNET3734");

    [Fact]
    public void StatementInAreaA_IsRefusedAt85()
        => AssertRejectedAt85Only(Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARSTMT."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA."),
            L("DISPLAY \"A\"."),
            L("    STOP RUN.")), "(5,8): error COBOLNET3735");

    [Fact] // a level-number other than 01 and 77 may begin in either area; an entry continued on a new line may not
    public void EntryContinuedInAreaA_IsRefusedAt85_ButAnIndentedLevelIsNot()
    {
        string source = Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARLEVEL."),
            L("DATA DIVISION."),
            L("WORKING-STORAGE SECTION."),
            L("01  G."),
            L("05  X PIC X."),
            L("88  X-ON VALUE \"Y\"."),
            L("01  H"),
            L("PIC X."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA."),
            L("    STOP RUN."));
        AssertRejectedAt85Only(source, "(9,8): error COBOLNET3735");
        Assert.Single(Check(85, source).Errors, e => e.Contains("COBOLNET373", StringComparison.Ordinal));
    }

    [Fact] // DOC-A.1-157: a tab advances to the next tab stop, so a tab-indented header stands at position 9 (area A)
    public void TabIndentedText_IsPlacedByItsReferenceFormatPosition()
    {
        var r = Check(85, Source(
            "\tIDENTIFICATION DIVISION.",
            "\tPROGRAM-ID. ARTAB.",
            "\tPROCEDURE DIVISION.",
            "\tMAIN-PARA.",
            "\t\tDISPLAY \"A\".",
            "\t\tSTOP RUN."));
        Assert.True(r.Success, string.Join("\n", r.Errors));
    }

    [Fact] // a header in library text is placed, and reported, where the library text writes it
    public void HeaderInAreaBOfLibraryText_IsReportedAtTheLibraryText()
        => AssertRejectedAt85Only(Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARCOPY."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA."),
            L("    PERFORM LIB-PARA."),
            L("    STOP RUN."),
            L("COPY ARLIB.")),
            "ARLIB.cpy(1,12): error COBOLNET3734",
            ("ARLIB.cpy", Source(L("    LIB-PARA."), L("    DISPLAY \"L\"."))));

    [Fact] // every area-A item of the family, each written in area A, compiles at 85
    public void EveryAreaAItemInAreaA_IsAccepted()
    {
        var r = Check(85, Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARALL."),
            L("ENVIRONMENT DIVISION."),
            L("CONFIGURATION SECTION."),
            L("SOURCE-COMPUTER. X."),
            L("INPUT-OUTPUT SECTION."),
            L("FILE-CONTROL."),
            L("    SELECT F ASSIGN TO \"F\"."),
            L("DATA DIVISION."),
            L("FILE SECTION."),
            L("FD  F."),
            L("01  R PIC X."),
            L("WORKING-STORAGE SECTION."),
            L("77  Y PIC X."),
            L("PROCEDURE DIVISION."),
            L("DECLARATIVES."),
            L("D1 SECTION."),
            L("    USE AFTER STANDARD ERROR PROCEDURE ON F."),
            L("END DECLARATIVES."),
            L("M SECTION."),
            L(" P0."),
            L("    STOP RUN."),
            L("END PROGRAM ARALL.")));
        Assert.True(r.Success, string.Join("\n", r.Errors));
    }

    // ── kb/Work PB3168: X3.23-1985 "Area A of a continuation line must be blank" ─────────────────────────────

    /// <summary>A fixed-form CONTINUATION line: a hyphen in position 7, then <paramref name="programText"/> from position 8.</summary>
    private static string C(string programText) => "      -" + programText;

    /// <summary>The first 52 characters of a literal: positions 21-72 of a line whose DISPLAY stands in position 12, so the
    /// literal runs to margin R without a closing delimiter and the next line continues it with a FIXED continuation
    /// indicator (§6.3.5 1)); that next line is the continuation line, by §6.3.5's paragraph common to both indicators:
    /// "the next line that is not a comment line or a blank line is the continuation line".</summary>
    private const string FirstPartOfContinuedLiteral = "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private static string ContinuedLiteral(string continuation) => Source(
        L("IDENTIFICATION DIVISION."),
        L("PROGRAM-ID. ARCONT."),
        L("PROCEDURE DIVISION."),
        L("MAIN-PARA."),
        L("    DISPLAY \"" + FirstPartOfContinuedLiteral),
        continuation,
        L("    STOP RUN."));

    [Theory] // the literal's tail is no token's start, so only the reference format can see it: every position of area A
    [InlineData("\"JK\".", 8)]
    [InlineData(" \"JK\".", 9)]
    [InlineData("   \"JK\".", 11)]
    public void ContinuedLiteralTextInAreaA_IsRefusedAt85Only(string text, int column)
        => AssertRejectedAt85Only(ContinuedLiteral(C(text)), $"(6,{column}): error COBOLNET3735");

    [Fact] // position 12 is area B: the same continuation compiles at 85
    public void ContinuedLiteralTextInAreaB_IsAccepted()
    {
        var r = Check(85, ContinuedLiteral(C("    \"JK\".")));
        Assert.True(r.Success, string.Join("\n", r.Errors));
    }

    [Fact] // a non-literal continuation line is a continuation line too, and its period is ONE diagnostic, not two
    public void ContinuationLineHoldingAPeriodInAreaA_IsReportedOnce()
    {
        string source = Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARPERIOD."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA."),
            L("    DISPLAY \"A\""),
            C("  ."),
            L("    STOP RUN."));
        AssertRejectedAt85Only(source, "(6,10): error COBOLNET3735");
        Assert.Single(Check(85, source).Errors, e => e.Contains("COBOLNET373", StringComparison.Ordinal));
    }

    [Fact] // a blank continuation line has no text to place; a library text's continuation line is reported where it is written
    public void ContinuationLineOfLibraryTextInAreaA_IsReportedAtTheLibraryText()
        => AssertRejectedAt85Only(Source(
            L("IDENTIFICATION DIVISION."),
            L("PROGRAM-ID. ARCOPYC."),
            L("PROCEDURE DIVISION."),
            L("MAIN-PARA."),
            L("    COPY ARCONT."),
            L("    STOP RUN.")),
            "ARCONT.cpy(2,8): error COBOLNET3735",
            ("ARCONT.cpy", Source(L("    DISPLAY \"" + FirstPartOfContinuedLiteral), C("\"JK\"."), C("        "))));
}
