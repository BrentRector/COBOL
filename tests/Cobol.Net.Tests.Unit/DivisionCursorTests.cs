// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB2739: "which division is this text in" is ONE rule (<see cref="DivisionCursor"/>), read off text-words wherever
/// they begin (ISO §6.3.1: the program-text area has no Area A / Area B line), shared by the fixed-form converter's line
/// walk and the COPY driver's walk over logical text.
/// </summary>
public sealed class DivisionCursorTests
{
    private static string[] Convert(string fixedFormText, bool startsInIdentification = true)
        => ReferenceFormatProcessor.NormalizeToFreeFormMapped(fixedFormText, null, "t.cob", initialFixed: true, out _,
            startsInIdentificationDivision: startsInIdentification).Text.Split('\n');

    [Theory] // a division header is a division header in Area B too (column 12), and so is one split across two lines
    [InlineData("000300     PROCEDURE DIVISION.\n")]
    [InlineData("000300     PROCEDURE\n000310         DIVISION.\n")]
    [InlineData("000300 procedure division.\n")]
    public void DivisionHeader_AnywhereInTheProgramTextArea_LeavesTheIdentificationDivision(string header)
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n" + header
                                 + "000400 REMARKS.\n000500     DISPLAY \"X\".\n");
        Assert.Contains(lines, l => l.Trim() == "REMARKS.");        // an ordinary paragraph, header untouched
        Assert.Contains(lines, l => l.Trim() == "DISPLAY \"X\".");   // its statement was not discarded as a comment-entry
    }

    [Fact] // the header's own spelling does not matter: ID DIVISION re-enters, so a comment-entry paragraph is one again
    public void IdDivisionHeader_AnywhereInTheProgramTextArea_EntersTheIdentificationDivision()
    {
        string[] lines = Convert("000100     PROCEDURE DIVISION.\n000200       ID DIVISION.\n000300 REMARKS. TEXT\n000400     MORE TEXT\n");
        Assert.Equal("REMARKS. .", lines[2]);
        Assert.Equal("", lines[3]);
    }

    [Theory] // §11.2.1: the header is optional, so the unit's first paragraph enters the division — a word that merely
             // ENDS in -ID is a user's name (CUSTOMER-ID), not that paragraph
    [InlineData("MOVE 2 TO CUSTOMER-ID.", false)]
    [InlineData("PROGRAM-ID. P.", true)]
    [InlineData("CLASS-ID. C.", true)]
    [InlineData("METHOD-ID. M.", true)]
    [InlineData("FUNCTION-ID. F.", true)]
    [InlineData("INTERFACE-ID. I.", true)]
    public void OnlyAnIdentificationParagraph_EntersTheDivision_NotAWordEndingInId(string statement, bool enters)
    {
        string[] lines = Convert("000100 PROCEDURE DIVISION.\n000200     " + statement + "\n000300 REMARKS. TEXT\n000400     MORE TEXT\n");
        if (enters)
        {
            Assert.Equal("REMARKS. .", lines[2]);
            Assert.Equal("", lines[3]);
        }
        else
        {
            Assert.Equal("REMARKS. TEXT", lines[2].Trim());
            Assert.Equal("MORE TEXT", lines[3].Trim());
        }
    }

    [Fact] // the free text of a comment-entry is commentary, not words of the program: a DIVISION named in it moves nothing
    public void CommentEntryBody_NamingADivision_DoesNotLeaveTheIdentificationDivision()
    {
        string[] lines = Convert("000100 AUTHOR. THE PROCEDURE DIVISION IS FAR AWAY\n000200     THE DATA DIVISION TOO\n000300 REMARKS. MORE\n");
        Assert.Equal("AUTHOR. .", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("REMARKS. .", lines[2]);
    }

    [Fact] // a literal holding a division header is a literal, not a header
    public void DivisionHeader_InsideALiteral_IsNotOne()
    {
        string[] lines = Convert("000100     DISPLAY \"PROCEDURE DIVISION.\".\n000200 REMARKS. TEXT\n");
        Assert.Equal("REMARKS. .", lines[1]);
    }

    [Fact] // the cursor carries across the blocks of one text: a COPY after a directive line is judged by the text before it
    public void Cursor_CarriesItsStateAcrossTheBlocksOfOneText()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        string block1 = "PROCEDURE DIVISION.\nMAIN-PARA.";
        Assert.False(cursor.InIdentificationDivisionAt(block1, block1.Length));
        cursor.Finish(block1);
        string block2 = "PERFORM X.\nCOPY BOOK.";
        Assert.False(cursor.InIdentificationDivisionAt(block2, block2.IndexOf("COPY", StringComparison.Ordinal)));
    }

    [Fact] // the operands of a COPY statement are not text that stands in a division, and the library text's own end state is adopted
    public void Skip_PassesOverACopyStatement_AndAdoptTakesTheLibraryTextsDivision()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        string text = "COPY BOOK REPLACING ==A== BY ==PROCEDURE DIVISION==.\nREMARKS.";
        Assert.True(cursor.InIdentificationDivisionAt(text, 0));
        cursor.Skip(text, text.IndexOf('\n'));
        Assert.True(cursor.InIdentificationDivisionAt(text, text.Length));   // the pseudo-text names a division; the text did not

        var library = new DivisionCursor(startsInIdentificationDivision: true);
        library.Finish("PROCEDURE DIVISION.");
        cursor.Adopt(library);
        Assert.False(cursor.InIdentificationDivision);
    }
}
