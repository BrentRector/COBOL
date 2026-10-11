// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <see cref="UsageFamilies.AdmitsNoValueLiteral"/> AGAINST ITS OWN SOURCES — ISO §13.16.3 SR10 (the classes, through
/// §8.5.2.1 Table 2) and §13.18.63.3 SR9 (four of them by usage), re-read out
/// of <c>specs/ISO_COBOL.md</c> on every run. The twin of <see cref="PicturelessUsageSetDriftTests"/>, for the
/// same reason and against the same failure: SR9 names FOUR usages and the compiler screened TWO of them —
/// exactly the two that already had a diagnostic band — for the life of the tree (kb/Work PB557). An
/// OBJECT REFERENCE entry with a quoted VALUE literal compiled, linked and ran clean with the literal silently
/// discarded; the same entry with VALUE NULL failed the Roslyn compilation instead.
///
/// <para><b>Why the spec and not the compiler.</b> A behavioural probe cannot contradict a wrong SET: drop an
/// arm and the screen and every test of the screen agree with each other again. The standard's own sentence is
/// the only independent oracle, and it is also the thing most likely to move, because SR9's list is one of the
/// sentences a transcription repair touches.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ValueClauseUsageSetDriftTests
{
    /// <summary>ISO §13.18.63.3 SR9, read out of the spec, split into the usage names it lists: "The VALUE
    /// clause shall not be specified if a USAGE clause with a phrase of FUNCTION-POINTER, MESSAGE-TAG,
    /// OBJECT-REFERENCE, or PROGRAM-POINTER is also specified."</summary>
    private static List<string> Sr9UsageNames()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.18\.63\.3\b"));
        Assert.True(start >= 0, "§13.18.63.3 is missing from specs/ISO_COBOL.md — this guard must follow the clause.");
        int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.18\.63\.4\b"));
        Assert.True(end > start, "§13.18.63.4 not found after §13.18.63.3 — the heading shape changed.");

        string? sr9 = lines[start..end].FirstOrDefault(
            l => Regex.IsMatch(l, @"^9\\?\)\s") && l.Contains("USAGE clause with a phrase of", StringComparison.Ordinal));
        Assert.NotNull(sr9);
        var m = Regex.Match(sr9!, @"USAGE clause with a phrase of (.+?) is also specified");
        Assert.True(m.Success,
            "§13.18.63.3 SR9's usage list did not parse — fix the scanner against the printed sentence, never "
            + $"the other way round. The clause reads: {sr9}");

        var names = m.Groups[1].Value
            .Replace(", or ", ", ").Replace(" or ", ", ")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        Assert.True(names.Count >= 4,
            $"only {names.Count} usage names parsed from §13.18.63.3 SR9 — fix the scanner, do not lower the floor.");
        return names;
    }

    /// <summary>Every usage SR9 names, that this compiler has a <see cref="Usage"/> member for, is in the set.
    /// A name with no member is a FORWARD OBLIGATION the test holds open by naming it, never a silent pass — and
    /// the list is empty today, because message-tag acquired its member with kb/Work PB487 and its arm reports
    /// beside the facility decline rather than instead of it (COBOLNET1943 + COBOLNET2168, measured).</summary>
    [Fact]
    public void EverySr9NamedUsage_WithAMember_AdmitsNoValueLiteral()
    {
        var byWord = Enum.GetValues<Usage>().ToLookup(u => UsageFamilies.UsageWord(u));
        var unmodelled = new List<string>();
        foreach (string name in Sr9UsageNames())
        {
            // SR9 spells them hyphenated and upper-case except OBJECT-REFERENCE, whose §13.18.60.2 general
            // format is the TWO words UsageWord renders ("OBJECT REFERENCE").
            string word = name.ToUpperInvariant().Replace("OBJECT-REFERENCE", "OBJECT REFERENCE");
            if (!byWord.Contains(word)) { unmodelled.Add(name); continue; }
            foreach (Usage u in byWord[word])
                Assert.True(UsageFamilies.AdmitsNoValueLiteral(u),
                    $"ISO §13.18.63.3 SR9 names '{name}' among the usages for which \"the VALUE clause shall not "
                    + $"be specified\", but UsageFamilies.AdmitsNoValueLiteral({u}) is false — a VALUE literal "
                    + "beside it reaches the code generator, where it is discarded in silence or fails the "
                    + "backend compilation.");
        }
        Assert.Equal([], unmodelled);
    }

    /// <summary>ISO §13.16.3 SR10's class list, read out of the spec: "The VALUE clause shall not be specified
    /// for data items of class index, message-tag, object, or pointer."</summary>
    private static List<string> Sr10ClassNames()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.16\.3\b"));
        Assert.True(start >= 0, "§13.16.3 is missing from specs/ISO_COBOL.md — this guard must follow the clause.");
        string? sr10 = lines[start..].Take(200).FirstOrDefault(
            l => Regex.IsMatch(l, @"^10\\?\)\s") && l.Contains("VALUE clause shall not be specified", StringComparison.Ordinal));
        Assert.NotNull(sr10);
        var m = Regex.Match(sr10!, @"data items of class (.+?)\.\s*$");
        Assert.True(m.Success, $"§13.16.3 SR10's class list did not parse — the clause reads: {sr10}");
        var names = m.Groups[1].Value.Replace(", or ", ", ").Replace(" or ", ", ")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        Assert.True(names.Count >= 4, $"only {names.Count} classes parsed from §13.16.3 SR10 — fix the scanner.");
        return names;
    }

    /// <summary>ISO §8.5.2.1 Table 2, read out of the spec: each class → the categories it holds.</summary>
    private static Dictionary<string, List<string>> TableTwo()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => l.Contains("Table 2 — Class and category relationships", StringComparison.Ordinal));
        Assert.True(start >= 0, "§8.5.2.1 Table 2 is missing from specs/ISO_COBOL.md.");
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (string l in lines[(start + 1)..].SkipWhile(l => !l.StartsWith('|')).TakeWhile(l => l.StartsWith('|')))
        {
            var cells = l.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length != 2 || cells[0] is "Class" || cells[0].StartsWith('-')) continue;
            map[cells[0]] = cells[1].Split("<br>").Select(c => c.Trim()).ToList();
        }
        Assert.True(map.Count >= 9, $"only {map.Count} Table 2 rows parsed — fix the scanner.");
        return map;
    }

    /// <summary>The USAGE word a Table 2 category of one of SR10's classes is declared with (§13.18.60.2): the
    /// category name IS the usage word, bar data-pointer (USAGE POINTER) and object-reference (the two words
    /// OBJECT REFERENCE).</summary>
    private static string UsageWordOfCategory(string category) => category.ToUpperInvariant() switch
    {
        "DATA-POINTER" => "POINTER",
        "OBJECT-REFERENCE" => "OBJECT REFERENCE",
        var w => w,
    };

    /// <summary>⛔ THE SET IS §13.16.3 SR10's, EXACTLY (kb/Work PB515): every usage whose Table 2 category sits in
    /// one of SR10's classes admits no VALUE clause, and no other usage is in the set. USAGE INDEX was the arm
    /// missing while the set was written from §13.18.63.3 SR9 alone — `77 I USAGE INDEX VALUE 7.` compiled and
    /// seeded the index item.</summary>
    [Fact]
    public void TheSet_IsSr10sClasses_ThroughTableTwo()
    {
        var table = TableTwo();
        var expected = new HashSet<string>();
        foreach (string cls in Sr10ClassNames())
        {
            Assert.True(table.TryGetValue(cls, out var cats),
                $"§13.16.3 SR10 names class '{cls}', which §8.5.2.1 Table 2 does not list — fix the scanner.");
            foreach (string c in cats!) expected.Add(UsageWordOfCategory(c));
        }
        var byWord = Enum.GetValues<Usage>().ToLookup(u => UsageFamilies.UsageWord(u));
        foreach (string word in expected)
        {
            Assert.True(byWord.Contains(word), $"§13.16.3 SR10 reaches USAGE {word}, which has no Usage member.");
            foreach (Usage u in byWord[word])
                Assert.True(UsageFamilies.AdmitsNoValueLiteral(u),
                    $"ISO §13.16.3 SR10 forbids a VALUE clause on USAGE {word} (its §8.5.2.1 Table 2 class is one "
                    + $"SR10 names), but UsageFamilies.AdmitsNoValueLiteral({u}) is false.");
        }
        foreach (Usage u in Enum.GetValues<Usage>())
            if (UsageFamilies.AdmitsNoValueLiteral(u))
                Assert.True(expected.Contains(UsageFamilies.UsageWord(u)),
                    $"UsageFamilies.AdmitsNoValueLiteral({u}) is true, but USAGE {UsageFamilies.UsageWord(u)} is "
                    + "not of a class §13.16.3 SR10 names — the set would refuse a VALUE the standard admits.");
    }

    /// <summary>A usage that admits no VALUE literal admits no PICTURE either — §13.16.3 SR8 names all six of
    /// them. The two sets are independent facts about the same usages, so a member that fell out of one and not
    /// the other is a drift nothing else would see: it would mean an item whose representation a picture
    /// character-string CAN describe was nevertheless refused an initial value.</summary>
    [Fact]
    public void EveryMember_IsAlsoPictureless()
    {
        foreach (Usage u in Enum.GetValues<Usage>())
            if (UsageFamilies.AdmitsNoValueLiteral(u))
                Assert.True(UsageFamilies.IsPictureless(u),
                    $"USAGE {UsageFamilies.UsageWord(u)} admits no VALUE literal but is not picture-less — one "
                    + "of the two sets has drifted from ISO §13.18.63.3 SR9 / §13.16.3 SR8.");
    }
}
