// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The LINE-ENTRY stage of the reference-format pipeline (kb/Work PB1800, PB1586, PB1496, PB1803): where a text
/// becomes lines. Every expectation is read off the rule it cites — docs/CONFORMANCE.md DOC-A.1-156 (terminators;
/// ISO §6.1 3) b)) and DOC-A.1-157 (a tab advances to the next tab stop — positions 1, 9, 17, …; §6.1 1) c)) — never
/// off a run.
/// </summary>
public sealed class PhysicalLinesTests
{
    private static string[] Texts(string source, bool ccvs = false)
        => PhysicalLines.Read(source, ccvs).Lines.ToArray().Select(l => l.Text).ToArray();

    [Theory] // DOC-A.1-156: LINE FEED, or CR LF, ends a line; the end of the text ends the last line.
    [InlineData("", new[] { "" })]
    [InlineData("A", new[] { "A" })]
    [InlineData("A\nB", new[] { "A", "B" })]
    [InlineData("A\r\nB\r\n", new[] { "A", "B", "" })]
    [InlineData("A\n\nB", new[] { "A", "", "B" })]
    public void LineFeedAndCrLfEndALine(string source, string[] expected) => Assert.Equal(expected, Texts(source));

    [Theory] // DOC-A.1-156: a CR NOT followed by a LINE FEED is a character of the line (GnuCOBOL's reader); only ONE
    // CR before a LINE FEED belongs to the terminator; a CR at the very end of the text has no LINE FEED after it.
    [InlineData("A\rB", new[] { "A\rB" })]
    [InlineData("A\r\r\nB", new[] { "A\r", "B" })]
    [InlineData("A\r", new[] { "A\r" })]
    [InlineData("\r\n\r\n", new[] { "", "", "" })]
    public void ALoneCrIsACharacter(string source, string[] expected) => Assert.Equal(expected, Texts(source));

    [Theory] // DOC-A.1-157: a TAB occupies the positions up to the next tab stop — 1, 9, 17, 25, … (0-based: the next
    // multiple of 8 strictly above its own index), so a tab at index 0 fills 8 positions and one at index 7 fills 1.
    [InlineData("\tX", "        X")]
    [InlineData("A\tX", "A       X")]
    [InlineData("ABCDEFG\tX", "ABCDEFG X")]
    [InlineData("ABCDEFGH\tX", "ABCDEFGH        X")]
    [InlineData("\t\tX", "                X")]
    [InlineData("000100\t\tX", "000100          " + "X")]
    [InlineData("\"A\tB\"", "\"A      B\"")]      // inside a literal too (owner decision, kb/Work R55)
    public void ATabAdvancesToTheNextTabStop(string source, string expected) => Assert.Equal(expected, Texts(source)[0]);

    [Fact] // the tab map (kb/Work PB1801): born here, present only on a line that held a tab.
    public void TheTabMapIsPresentOnlyWhereATabWas()
    {
        var set = PhysicalLines.Read("plain\n\tX\n");
        Assert.Null(set[0].Tabs);
        Assert.NotNull(set[1].Tabs);
        Assert.Equal(2, set[1].Number);
    }

    [Fact] // For every physical index i: ToPhysical(ToExpanded(i)) == i; no U+0009 survives; every expanded position
    // names the character that produced it — over every line of up to four characters drawn from {X, TAB, space}.
    public void TabMapRoundTrips_AndNoTabSurvives()
    {
        char[] alphabet = ['X', '\t', ' '];
        for (int length = 0; length <= 4; length++)
        {
            int combinations = (int)Math.Pow(alphabet.Length, length);
            for (int n = 0; n < combinations; n++)
            {
                var raw = new char[length];
                int rest = n;
                for (int i = 0; i < length; i++) { raw[i] = alphabet[rest % alphabet.Length]; rest /= alphabet.Length; }
                string physical = new(raw);
                var line = PhysicalLines.Read(physical)[0];
                Assert.DoesNotContain('\t', line.Text);
                var tabs = line.Tabs;
                if (!physical.Contains('\t')) { Assert.Null(tabs); Assert.Equal(physical, line.Text); continue; }
                Assert.NotNull(tabs);
                for (int i = 0; i < physical.Length; i++)
                {
                    Assert.Equal(i, tabs.ToPhysical(tabs.ToExpanded(i)));
                    if (physical[i] != '\t') Assert.Equal(physical[i], line.Text[tabs.ToExpanded(i)]);
                }
                Assert.Equal(line.Text.Length, tabs.ToExpanded(physical.Length));
                Assert.Equal(physical.Length, tabs.ToPhysical(line.Text.Length));
                for (int e = 0; e < line.Text.Length; e++)
                    if (physical[tabs.ToPhysical(e)] == '\t') Assert.Equal(' ', line.Text[e]);
            }
        }
    }

    [Fact] // kb/Work PB1803: the CCVS archive markers are blanked only under the CCVS dialect (--nist), and a blanked
    // marker keeps its slot (LINE-COUNT PRESERVING, kb/Work PB82).
    public void ArchiveMarkers_AreBlankedOnlyUnderTheCcvsDialect()
    {
        string src = "*HEADER,COBOL,SM101A\n       IDENTIFICATION DIVISION.\n  *END-OF,SM101A\n";
        Assert.Equal(new[] { "*HEADER,COBOL,SM101A", "       IDENTIFICATION DIVISION.", "  *END-OF,SM101A", "" }, Texts(src));
        Assert.Equal(new[] { "", "       IDENTIFICATION DIVISION.", "", "" }, Texts(src, ccvs: true));
    }

    [Fact] // the marker test reads the EXPANDED line: a leading TAB is white space before it.
    public void ArchiveMarker_AfterATab_IsStillAMarker()
        => Assert.Equal(new[] { "" }, Texts("\t*HEADER,X", ccvs: true));

    // ── the readers of the stage ───────────────────────────────────────────────────────────────────────────────

    private static (string Text, DiagnosticBag Bag) Normalize(string source, InitialReferenceFormat initial,
        int std = 2023, bool ccvs = false)
    {
        var bag = new DiagnosticBag();
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, new ReferenceFormatDiagnostics(std, false, bag), "t.cob",
            initial.InitialFixed(), out _, ccvs);
        return (m.Text, bag);
    }

    [Fact] // kb/Work PB1586 (DOC-A.1-157): a TAB is expanded BEFORE the indicator area is read — `<TAB><TAB>DISPLAY` is
    // a SOURCE line whose program text starts at position 17; before, 'L' was read as the indicator (COBOLNET2616).
    public void FixedForm_TabsAreExpandedBeforeTheIndicatorAreaIsRead()
    {
        var (text, bag) = Normalize("\t\tDISPLAY \"T\".\n", InitialReferenceFormat.Fixed, std: 85);
        Assert.Empty(bag.Diagnostics);
        Assert.Equal(new string(' ', 9) + "DISPLAY \"T\".", text.Split('\n')[0]);   // positions 8-16 (spaces) + the text from 17
    }

    [Fact] // a TAB after the sequence number fills the indicator area: position 7 is a SPACE, the text starts at 9.
    public void FixedForm_ATabAfterTheSequenceNumber_FillsTheIndicatorArea()
    {
        var (text, bag) = Normalize("000100\tDISPLAY \"T\".\n", InitialReferenceFormat.Fixed);
        Assert.Empty(bag.Diagnostics);
        Assert.Equal(" DISPLAY \"T\".", text.Split('\n')[0]);   // position 8 (a space) then the text from position 9
    }

    [Fact] // §6.3.1 / item 158: text a TAB pushes past margin R is outside the program-text area, like any such text.
    public void FixedForm_ATabPushingTextPastMarginR_IsTruncated()
    {
        // 7 + 12 characters, then spaces to the tab's own 0-based index 64: the tab fills 64-71, so what follows it
        // starts at position 73.
        var (text, bag) = Normalize(new string(' ', 7) + "DISPLAY \"A\"." + new string(' ', 64 - 7 - 12) + "\tDISPLAY \"B\".\n",
            InitialReferenceFormat.Fixed);
        Assert.Empty(bag.Diagnostics);
        Assert.DoesNotContain("\"B\"", text);
        Assert.Contains("\"A\"", text);
    }

    [Fact] // the free-form reading expands tabs too, and TAB-indented lines are ordinary lines.
    public void FreeForm_TabsAreExpanded()
    {
        var (text, bag) = Normalize("\tDISPLAY \"F\".\n", InitialReferenceFormat.Free);
        Assert.Empty(bag.Diagnostics);
        Assert.Equal("        DISPLAY \"F\".", text.Split('\n')[0]);
    }

    [Fact] // the Auto detector reads the EXPANDED lines: a TAB in the sequence area is white space, not a character
    // that makes the digits-or-spaces test fail (GnuCOBOL's probe expands tabs before it looks at column 7).
    public void AutoDetector_ReadsTheExpandedLines()
    {
        string src = "000100\t  IDENTIFICATION DIVISION.\n000200\t  PROGRAM-ID. X.\n000300\t  PROCEDURE DIVISION.\n";
        Assert.True(ReferenceFormatProcessor.IsFixedForm(PhysicalLines.Read(src).Lines));
    }

    [Theory] // §6.1 3) a): "ranging from a minimum of 0 to a maximum of 255" — a free-form line of more than 255
    // positions is COBOLNET2653, at every edition the free-form reading exists in (kb/Work PB1496).
    [InlineData(255, 0)]
    [InlineData(256, 1)]
    [InlineData(300, 1)]
    public void FreeForm_LineOfMoreThan255Positions_IsDiagnosed(int positions, int expectedDiagnostics)
    {
        foreach (int std in new[] { 85, 2002, 2014, 2023 })
        {
            var (_, bag) = Normalize(new string('A', positions) + "\n", InitialReferenceFormat.Free, std);
            Assert.Equal(expectedDiagnostics, bag.Diagnostics.Count(d => d.Code == "COBOLNET2653"));
        }
    }

    [Fact] // the limit counts the EXPANDED line: a TAB counts the positions it advances over (DOC-A.1-157).
    public void FreeForm_TheLimitCountsTheExpandedLine()
    {
        string ok = new string('A', 247) + "\t";   // 247 + the one-position gap to the tab stop at 248 → 248
        string tooLong = new string('A', 248) + "\t";   // 248 + 8 → 256
        Assert.DoesNotContain(Normalize(ok + "\n", InitialReferenceFormat.Free).Bag.Diagnostics, d => d.Code == "COBOLNET2653");
        Assert.Contains(Normalize(tooLong + "\n", InitialReferenceFormat.Free).Bag.Diagnostics, d => d.Code == "COBOLNET2653");
    }

    [Fact] // §6.1 2) b): a fixed-form line may be longer than a free-form one — only its program-text area counts.
    public void FixedForm_IsNotSubjectToThe255PositionLimit()
    {
        var (_, bag) = Normalize("       DISPLAY \"A\"." + new string(' ', 240) + "TAG\n", InitialReferenceFormat.Fixed);
        Assert.DoesNotContain(bag.Diagnostics, d => d.Code == "COBOLNET2653");
    }

    [Fact] // a >>SOURCE FORMAT FREE segment of a fixed-form file is a free-form reading too: the limit applies there.
    public void FreeSegmentOfAFixedFile_IsSubjectToTheLimit()
    {
        var (_, bag) = Normalize(">>SOURCE FORMAT FREE\n" + new string('A', 256) + "\n", InitialReferenceFormat.Fixed);
        Assert.Equal(1, bag.Diagnostics.Single(d => d.Code == "COBOLNET2653").Location.Line);   // line 2 (0-based 1)
    }

    [Fact] // DOC-A.1-156: a lone CR does not end a line — `>>` after it is not at a line start.
    public void ALoneCr_DoesNotStartADirectiveLine()
    {
        var (text, bag) = Normalize("DISPLAY \"A\"\r>>SOURCE FORMAT FIXED\nX\n", InitialReferenceFormat.Free);
        // one line, the format NOT switched; the `>>` follows program text, so §7.3.3 SR2 names it (kb/Work PB1690)
        Assert.Equal("DISPLAY \"A\"", text.Split('\n')[0]);
        Assert.Equal("X", text.Split('\n')[1]);
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET2691");
    }

    [Fact] // the lines of a SOURCE FORMAT segment keep the FILE's numbers (the stage numbers the lines once).
    public void SegmentsKeepFileLineNumbers()
    {
        var bag = new DiagnosticBag();
        var m = ReferenceFormatProcessor.NormalizeToFreeFormMapped(
            ">>SOURCE FORMAT FIXED\n      *\n" + "000100" + "X" + "DISPLAY \"A\".\n", new ReferenceFormatDiagnostics(2023, false, bag), "t.cob",
            InitialReferenceFormat.Free);
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET2616" && d.Location.Line == 2);   // line 3 (0-based 2)
        Assert.Equal(3, m.Lines[2].Line);
    }
}
