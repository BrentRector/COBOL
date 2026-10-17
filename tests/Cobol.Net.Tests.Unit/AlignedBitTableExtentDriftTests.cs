// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE EXTENT OF N OCCURRENCES OF A BIT TABLE IS ONE RULE IN ONE PLACE, AND ITS LAST OCCURRENCE CARRIES NO TRAILING
/// FILLER (kb/Work PB3156). §13.18.1.4 GR2 puts every occurrence of an ALIGNED table at the first bit of a byte, so the
/// occurrences sit one STRIDE apart; §8.5.1.6.3 generates implicit filler only "as needed to advance alignment to a
/// required natural boundary for the next item", and the last occurrence has no next one, so the table's extent is
/// <c>stride × (n − 1) + width</c> for n ≥ 1 and nothing for none. That is <c>BitLayout.TableBits</c>; it had been spelled
/// three times (the layout walk, the record-size minimum, the OCCURS DEPENDING record limit) and a fourth reader — FUNCTION
/// LENGTH of a bit group, counted in boolean positions (§15.50.4 r1) — was missing it, so the table was a named stage. These
/// facts keep it one rule:
/// <list type="bullet">
/// <item>no source under <c>src/</c> but <c>BitLayout.cs</c> writes the extent out as <c>StrideBits(…) * (… − 1)</c>;</item>
/// <item>the runtime's two readers of the same rule, <c>CobolTable.OdoExtentTrimmed</c> (an OCCURS DEPENDING extent in
/// positions) and <c>CobolBits.Strided</c> (the carrier of the occurrences), agree with it at every count.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class AlignedBitTableExtentDriftTests
{
    [Fact]
    public void NoSourceButBitLayout_SpellsTheTableExtentAsAStrideTimesOccurrencesLessOne()
    {
        var spelling = new Regex(@"StrideBits\([^()]*\)\s*\*\s*\([^()]*-\s*1\)", RegexOptions.CultureInvariant);
        var offenders = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => Path.GetFileName(f) != "BitLayout.cs")
            .Where(f => spelling.IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "A source spells a bit table's extent as stride × (n − 1) + width itself; ask BitLayout.TableBits, the one rule "
            + "(kb/Work PB3156). Offenders: " + string.Join(", ", offenders));
    }

    /// <summary>The table of the golden (<c>05 TA PIC 1(3) USAGE BIT ALIGNED OCCURS 0 TO 4</c> after one bit item): the
    /// prefix is 8 bits (the bit item and its 7 filler bits), the stride 8, the width 3, so the trailing filler is 5 and
    /// the extent at 0, 1, 2 and 4 occurrences is 8, 11, 19 and 35 positions — the same numbers the fixed table has.</summary>
    [Theory]
    [InlineData(0, 8)]
    [InlineData(1, 11)]
    [InlineData(2, 19)]
    [InlineData(4, 35)]
    public void OdoExtentTrimmed_GivesBackTheLastOccurrencesFiller(int count, int positions) =>
        Assert.Equal(positions, CobolTable.OdoExtentTrimmed(count, 0, 4, 8, 8, 5));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void OdoExtentTrimmed_WithNoFiller_IsTheContiguousExtent(int count) =>
        Assert.Equal(CobolTable.OdoExtent(count, 0, 4, 8, 3), CobolTable.OdoExtentTrimmed(count, 0, 4, 8, 3, 0));

    [Fact]
    public void Strided_PadsEveryOccurrenceButTheLast()
    {
        Assert.Equal("10100000" + "011", CobolBits.Strided(["101", "011"], 8));
        Assert.Equal("101", CobolBits.Strided(["101"], 8));
        Assert.Equal("", CobolBits.Strided([], 8));
        // No ALIGNED clause: the stride is the width and the carrier is the plain concatenation.
        Assert.Equal("101011", CobolBits.Strided(["101", "011"], 3));
    }

    /// <summary>The third reader of the rule, <see cref="OdoTail"/> (the variable-length group codec's and a cell window's
    /// geometry of the table, kb/Work PB3167 review R1-1), over a table that strides whole bytes but begins INSIDE a
    /// character: <c>05 ABA PIC 1(2) USAGE BIT. 05 ASUB GROUP-USAGE BIT OCCURS 0 TO 3 DEPENDING ON N. 10 ASA PIC 1 USAGE
    /// BIT. 10 ATA PIC 1(5) USAGE BIT ALIGNED.</c> — lead 2 bits, stride 16, width 11, so trailing filler 5. Its characters
    /// at every count are the character channel's (<see cref="CobolTable.OdoExtentChars"/> over the same fixed bits), and
    /// at the maximum the table is 6 characters (bit 45), never the stride count's 7 (bit 50). The receiving splice keeps
    /// the occurrences past the count from bit <c>lead + count × stride</c> of the member.</summary>
    [Fact]
    public void OdoTail_AgreesWithTheCharacterExtent_AtEveryCount_WhenTheTableBeginsInsideACharacter()
    {
        var tail = new OdoTail(16, 3, 0, 2, 8, 5);
        Assert.Equal(6, tail.Width);
        for (int count = 0; count <= 3; count++)
            Assert.Equal(CobolTable.OdoExtentChars(count, 0, 3, 2, 16, 5, 8), tail.Width - tail.CutAt(count));
        // The member's image at the maximum is the table's 6 characters (lead bits included): ABA = 11, then occurrence
        // 1 ASA = 1, TA = 10101; occurrence 2 ASA = 1, TA = 01010; occurrence 3 all zero. The receiver keeps occurrence
        // 3 (bits 34..44) when the count is 2.
        static string Image(string bits) => CobolBits.Pack(bits.PadRight(48, '0'));
        string received = Image("11" + "100000" + "10101" + "000" + "00" + "100000" + "01010" + "000" + "00" + "000000" + "00000");
        string current = Image("00" + "000000" + "00000" + "000" + "00" + "000000" + "00000" + "000" + "00" + "100000" + "11111");
        Assert.Equal(Image("11" + "100000" + "10101" + "000" + "00" + "100000" + "01010" + "000" + "00" + "100000" + "11111"),
            tail.KeepUnused(received, current, 2));
    }

    [Fact]
    public void OdoExtentTrimmed_AndStrided_AgreeAtEveryCount()
    {
        string[] occurrences = ["101", "011", "110", "001"];
        for (int count = 0; count <= 4; count++)
        {
            int carrier = CobolBits.Strided(occurrences[..count], 8).Length;
            Assert.Equal(CobolTable.OdoExtentTrimmed(count, 0, 4, 0, 8, 5), carrier);
        }
    }
}
