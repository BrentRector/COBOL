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
