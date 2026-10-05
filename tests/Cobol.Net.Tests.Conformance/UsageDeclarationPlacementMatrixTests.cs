// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ISO §13.18.60.3 SR14 AS A MATRIX, over every phrase it names and both of the spellings that put the phrase
/// below level 1 (kb/Work PB819): "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER,
/// or PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data item
/// subordinate to a type declaration that includes the STRONG phrase."
/// <code>
///     01 G.  05 A USAGE u.            the ELEMENTARY item below level 1     (arm B)
///     01 G USAGE u.  05 A PIC X.      the GROUP spelling, §13.18.60.4 GR1    (arm A)
/// </code>
/// <para>Both are violations of the one rule, so both draw COBOLNET1724 — for EVERY phrase, whether the compiler
/// models the usage or refuses its keyword (MESSAGE-TAG: declined non-support, COBOLNET1943). The matrix is read off
/// <see cref="ItemCategory.Sr14PhraseOf(Usage?)"/>, so a usage added to SR14's population is a row here without anyone
/// remembering it; one hand-written <c>if</c> per phrase is how the elementary arm went blind to the staged
/// FUNCTION-POINTER and the declined MESSAGE-TAG while the group arm saw them.</para>
/// </summary>
public sealed class UsageDeclarationPlacementMatrixTests
{
    /// <summary>One row per usage SR14 names, spelled as a programmer writes it.</summary>
    public static TheoryData<Usage> Sr14Usages()
    {
        var d = new TheoryData<Usage>();
        foreach (Usage u in Enum.GetValues<Usage>())
            if (ItemCategory.Sr14PhraseOf(u) is not null) d.Add(u);
        return d;
    }

    private static string Program(string id, string ws) =>
        $"""
         IDENTIFICATION DIVISION.
         PROGRAM-ID. {id}.
         DATA DIVISION.
         WORKING-STORAGE SECTION.
         {ws}
         PROCEDURE DIVISION.
             STOP RUN.
         """;

    private static bool Reports1724(string source)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(source);
        return !ok && Regex.IsMatch(detail, @"COBOL(?:NET)?1724");
    }

    [Theory]
    [MemberData(nameof(Sr14Usages))]
    public void EveryPhrase_IsRefusedBelowLevel1_OnBothSpellings(Usage usage)
    {
        string word = UsageFamilies.UsageWord(usage);
        string tag = Regex.Replace(word, "[^A-Z0-9]", "");
        string elementary = Program($"P819E{tag}", $"01 G.\n    05 A USAGE {word}.");
        string group = Program($"P819G{tag}", $"01 G USAGE {word}.\n    05 A PIC X.");
        Assert.True(Reports1724(elementary),
            $"`05 A USAGE {word}.` inside an ordinary group violates §13.18.60.3 SR14 and must draw COBOLNET1724");
        Assert.True(Reports1724(group),
            $"`01 G USAGE {word}. 05 A PIC X.` violates §13.18.60.3 SR14 (via §13.18.60.4 GR1) and must draw COBOLNET1724");
    }
}
