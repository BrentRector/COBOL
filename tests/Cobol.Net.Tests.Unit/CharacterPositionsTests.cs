// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A CHARACTER POSITION IS ONE CHARACTER, NOT ONE UTF-16 CODE UNIT (kb/Work PB1966; docs/CONFORMANCE.md DOC-A.1-157;
/// ISO/IEC 1989:2023 §6.1 1) a) "Reference format is described in terms of character positions on a line", c) "The
/// implementor shall specify the meaning of lines and character positions"). Every expectation here is a position
/// COMPUTED from the rule — the characters written, one position each — never read off a run. A supplementary-plane
/// character (<c>U+1000D</c>, a Linear B syllable; <c>U+1F600</c>) is two <see cref="char"/>s and one position, so a
/// line holding N of them has margin R, the free-form 255 limit, the indicator area and every tab stop N units later
/// than a count of <c>string.Length</c> puts them.
/// </summary>
public sealed class CharacterPositionsTests
{
    /// <summary>One supplementary-plane character (two UTF-16 units).</summary>
    private static readonly string Wide = char.ConvertFromUtf32(0x1000D);

    private static string Wides(int n) => string.Concat(Enumerable.Repeat(Wide, n));

    [Theory] // positions of a text, whatever its UTF-16 length
    [InlineData("", 0)]
    [InlineData("ABC", 3)]
    [InlineData("é", 1)]              // BMP: one unit, one position
    public void Count_IsTheNumberOfCharacters(string text, int positions) => Assert.Equal(positions, CharacterPositions.Count(text));

    [Fact] // a surrogate that is not half of a valid pair is a character of its own
    public void Count_ALoneSurrogateIsOnePosition()
    {
        string high = new([(char)0xD800]), low = new([(char)0xDC00]);
        Assert.Equal(1, CharacterPositions.Count(high));
        Assert.Equal(1, CharacterPositions.Count(low));
        Assert.Equal(2, CharacterPositions.Count(high + "A"));
        Assert.Equal(2, CharacterPositions.Count(low + high));     // reversed: not a pair
        Assert.Equal(1, CharacterPositions.Count(high + low));     // in order: a pair
    }

    [Fact]
    public void Count_ASurrogatePairIsOnePosition()
    {
        string text = "A" + Wide + "B" + Wides(3);
        Assert.Equal(1 + 2 + 1 + 6, text.Length);
        Assert.Equal(1 + 1 + 1 + 3, CharacterPositions.Count(text));
    }

    [Fact]
    public void IndexAt_And_PositionAt_AreInverse_AtEveryCharacter()
    {
        string text = "A" + Wide + "B" + Wides(2) + "C";
        int[] indexes = [0, 1, 3, 4, 6, 8];                         // where each of the six characters begins
        for (int position = 0; position < indexes.Length; position++)
        {
            Assert.Equal(indexes[position], CharacterPositions.IndexAt(text, position));
            Assert.Equal(position, CharacterPositions.PositionAt(text, indexes[position]));
        }
        Assert.Equal(text.Length, CharacterPositions.IndexAt(text, 6));       // past the last character: the end
        Assert.Equal(text.Length, CharacterPositions.IndexAt(text, 99));
        Assert.Equal(7, CharacterPositions.IndexAt("ABCDEFG", 7));            // BMP text: the identity, clamped
        Assert.Equal(7, CharacterPositions.IndexAt("ABCDEFG", 12));
    }

    // ── the fixed-form line: sequence area 1-6, indicator 7, program text 8-72, margin R after 72 ────────────

    [Fact] // a supplementary character in the sequence area is ONE of its six positions, so the indicator is still position 7
    public void FixedFormLine_TheIndicatorIsPosition7_AfterWideCharactersInTheSequenceArea()
    {
        var line = new FixedFormLine(Wides(3) + "ABC" + "*" + " X");
        Assert.True(line.HasIndicator);
        Assert.Equal('*', line.Indicator.Value);
        Assert.Equal(Wides(3) + "ABC", line.SequenceArea.ToString());
        Assert.StartsWith(" X", line.ProgramText);
    }

    [Fact] // the indicator may itself be a supplementary character: one position, a Rune and not half a pair
    public void FixedFormLine_AWideIndicatorIsOneCharacter()
    {
        var line = new FixedFormLine("000100" + Wide + "DISPLAY");
        Assert.Equal(0x1000D, line.Indicator.Value);
        Assert.StartsWith("DISPLAY", line.ProgramText);
    }

    [Fact] // a line shorter than 7 positions has no indicator, and a 7-position line no program text
    public void FixedFormLine_ShortLines()
    {
        Assert.False(new FixedFormLine("000100").HasIndicator);
        Assert.Equal(' ', new FixedFormLine("000100").Indicator.Value);
        Assert.True(new FixedFormLine("000100 ").HasIndicator);
        Assert.False(new FixedFormLine("000100 ").HasProgramText);
        Assert.True(new FixedFormLine("000100 A").HasProgramText);
        Assert.Equal(FixedFormLine.SourceAreaWidth, new FixedFormLine("").ProgramText.Length);
    }

    [Fact] // DOC-A.1-157: the program-text area is ALWAYS positions 8-72, a short line read as if space-filled to margin R
    public void FixedFormLine_ProgramText_IsSixtyFivePositions_ShortLinesPadded()
    {
        string area = new FixedFormLine("000100 " + Wides(10)).ProgramText;
        Assert.Equal(FixedFormLine.SourceAreaWidth, CharacterPositions.Count(area));
        Assert.Equal(Wides(10) + new string(' ', 55), area);
    }

    [Fact] // margin R is position 72: a text of exactly 65 wide characters in the area is read whole, anything past is not
    public void FixedFormLine_MarginR_IsPosition72_NotUnit72()
    {
        string line = "000100 " + Wides(65) + "TAIL";
        var fixedLine = new FixedFormLine(line);
        Assert.Equal(Wides(65), fixedLine.ProgramText);
        Assert.Equal("TAIL", fixedLine.BeyondMarginR.ToString());
    }

    [Fact] // BMP-only text is the identity (the unchanged common case)
    public void FixedFormLine_BmpLine()
    {
        var line = new FixedFormLine("000100 " + new string('A', 70));
        Assert.Equal(new string('A', 65), line.ProgramText);
        Assert.Equal(new string('A', 5), line.BeyondMarginR.ToString());
    }

    // ── DOC-A.1-157 through the whole logical conversion ───────────────────────────────────────────────────────

    private static (string[] Lines, DiagnosticBag Bag) Normalize(string source, InitialReferenceFormat format, int std = 2023)
    {
        var bag = new DiagnosticBag();
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, new ReferenceFormatDiagnostics(std, false, bag), "t.cob",
            format.InitialFixed(), out _);
        return (m.Text.Split('\n'), bag);
    }

    [Fact] // kb/Work PB1966's repro: a word of 40 wide characters in positions 11-50 and text ending exactly at position 72
    public void FixedForm_TextEndingAtPosition72_IsReadWhole_AfterWideCharacters()
    {
        string head = "000100 01  " + Wides(40) + " ";
        string line = head + new string(' ', 72 - CharacterPositions.Count(head) - "PIC X.".Length) + "PIC X.";
        Assert.Equal(72, CharacterPositions.Count(line));
        var (lines, bag) = Normalize(line + "\n", InitialReferenceFormat.Fixed);
        Assert.Equal("01  " + Wides(40) + " " + new string(' ', 72 - 7 - 4 - 40 - 1 - 6) + "PIC X.", lines[0]);
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Fact] // text after position 72 is ignored (Annex A item 158), however many wide characters precede it
    public void FixedForm_TextPastPosition72_IsIgnored_AfterWideCharacters()
    {
        string line = "000100 " + Wides(65) + "TAIL";
        var (lines, _) = Normalize(line + "\n", InitialReferenceFormat.Fixed);
        Assert.Equal(Wides(65), lines[0]);
    }

    [Fact] // §6.1 1) c) + §6.3.5: a literal continued by the fixed indicator carries every position up to margin R
    public void FixedForm_ContinuedLiteral_CarriesPositionsToMarginR_AfterWideCharacters()
    {
        string first = "000100 01  " + Wides(20) + " PIC X(80) VALUE \"";
        first += new string('A', 72 - CharacterPositions.Count(first));
        var (lines, bag) = Normalize(first + "\n000200-    \"BB\".\n", InitialReferenceFormat.Fixed);
        Assert.Equal(72, CharacterPositions.Count(first));
        Assert.Equal("01  " + Wides(20) + " PIC X(80) VALUE \"" + new string('A', 72 - 7 - 4 - 20 - " PIC X(80) VALUE \"".Length) + "BB\".", lines[0]);
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Fact] // a national literal holding a supplementary character shifts the columns the same way (the note's second shape)
    public void FixedForm_NationalLiteralWithWideCharacters_EndingAtPosition72()
    {
        string head = "000100     DISPLAY N\"" + Wides(10) + "\"";
        string line = head + new string(' ', 71 - CharacterPositions.Count(head)) + ".";
        Assert.Equal(72, CharacterPositions.Count(line));
        var (lines, bag) = Normalize(line + "\n", InitialReferenceFormat.Fixed);
        Assert.Contains("N\"" + Wides(10) + "\"", lines[0]);
        Assert.EndsWith(".", lines[0]);                                     // the period at position 72 is not cut
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
    }

    [Fact] // DOC-A.1-157: tab stops are POSITIONS 1, 9, 17, … — after a wide character the next stop is by position
    public void ATabAfterWideCharacters_AdvancesToTheNextTabStopByPosition()
    {
        // 3 wide characters = positions 1-3 (units 0-5); the tab fills positions 4-8 (5 spaces), the next stop being 9
        string text = PhysicalLines.Read(Wides(3) + "\tX").Lines[0].Text;
        Assert.Equal(Wides(3) + "     " + "X", text);
        Assert.Equal(9, CharacterPositions.Count(text));                    // X stands at position 9
        // a tab exactly at a stop fills a whole tab width
        Assert.Equal(Wides(8) + new string(' ', 8) + "X", PhysicalLines.Read(Wides(8) + "\tX").Lines[0].Text);
    }

    [Fact] // the tab map still names UTF-16 INDEXES of the physical line (the tab is one unit, its fill is several)
    public void TheTabMap_IsInUtf16Indexes()
    {
        var line = PhysicalLines.Read(Wides(3) + "\tX").Lines[0];
        Assert.NotNull(line.Tabs);
        Assert.Equal(6, line.Tabs!.ToExpanded(6));                           // the tab begins after six units
        Assert.Equal(6, line.Tabs.ToPhysical(6));                            // and its first fill space is that tab
        Assert.Equal(7, line.Tabs.ToPhysical(11));                           // X is physical index 7
    }

    [Fact] // §6.1 3) a): a free-form line has at most 255 character positions — 128 wide characters are 128, not 256
    public void FreeForm_255Positions_AreCountedAsCharacters()
    {
        var (_, within) = Normalize("DISPLAY " + Wides(247) + "\n", InitialReferenceFormat.Free);   // 8 + 247 = 255
        Assert.DoesNotContain(within.Diagnostics, d => d.Code == "COBOLNET2653");
        var (_, over) = Normalize("DISPLAY " + Wides(248) + "\n", InitialReferenceFormat.Free);     // 256
        var diagnostic = Assert.Single(over.Diagnostics, d => d.Code == "COBOLNET2653");
        Assert.Contains("256 character positions", diagnostic.Message);
    }

    [Fact] // §6.3.3 / §6.2.2: an indicator that is not a fixed indicator is diagnosed AS THE CHARACTER, not as half of a pair
    public void FixedForm_AWideIndicator_IsNamedWhole()
    {
        var (lines, bag) = Normalize("000100" + Wide + "DISPLAY \"A\".\n", InitialReferenceFormat.Fixed);
        var diagnostic = Assert.Single(bag.Diagnostics, d => d.Code == "COBOLNET2616");
        Assert.Contains("'" + Wide + "'", diagnostic.Message);
        Assert.StartsWith("DISPLAY \"A\".", lines[0]);                         // then read as a source line
    }

    [Fact] // a diagnostic's column is a CHARACTER position of the line (the lexer's own columns are code points)
    public void FixedForm_ADiagnosticColumn_CountsCharacters()
    {
        // a floating comment indicator directly after text, in a COBOL-2002 source: COBOLNET2650-family "unseparated"
        // is asked at the indicator's column; with 3 wide characters before it the column is by position, not by unit.
        string line = "000100 " + Wides(3) + "*> remark";
        var (_, bag) = Normalize(line + "\n", InitialReferenceFormat.Fixed, std: 2002);
        var diagnostic = Assert.Single(bag.Diagnostics, d => d.Message.Contains("separator space", StringComparison.Ordinal));
        Assert.Equal(7 + 3, diagnostic.Location.Column);                      // 0-based position of the `*` in the line
    }

    [Fact] // the Auto detector reads the same columns: a sequence area of supplementary DIGITS (U+1D7CE.., category Nd) is
           // six positions of digits, and the character after them is the indicator
    public void IsFixedForm_ReadsColumnsByPosition()
    {
        string digits = string.Concat(Enumerable.Range(0, 6).Select(i => char.ConvertFromUtf32(0x1D7CE + i)));
        PhysicalLine[] lines = PhysicalLines.Read(digits + " DISPLAY \"A\".\n" + digits + "*  comment\n").Lines.ToArray();
        Assert.True(ReferenceFormatProcessor.IsFixedForm(lines));
    }
}
