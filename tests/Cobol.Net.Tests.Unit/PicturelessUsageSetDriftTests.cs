// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <see cref="UsageFamilies.IsPictureless"/> AGAINST ITS OWN SOURCE — ISO §13.16.3 SR8, re-read out of
/// <c>specs/ISO_COBOL.md</c> on every run (kb/Work PB495).
///
/// <para><b>Why the spec and not the compiler.</b> The set drives BOTH arms of the rule — the written-clause
/// screen in <c>DataBinder.BindEntry</c> and the §13.18.60.4 GR1 inherited screen in
/// <c>UsageInheritancePass</c> — which is the point of writing it down once, and which also means a probe of
/// the compiler cannot contradict it: a wrong entry moves both arms together and every behavioural test still
/// agrees with itself. Measured exactly that way while this file was being written — dropping BINARY-SHORT from
/// the set left <c>UsageInheritanceDriftTests</c> fully green, because both spellings then accepted the illegal
/// PICTURE identically. The only independent oracle is the standard's own sentence.</para>
///
/// <para>The transcription is also the thing most likely to move: SR8's list is one of the sentences a spec
/// repair touches, and this test carries the repair through instead of going stale.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PicturelessUsageSetDriftTests
{
    /// <summary>ISO §13.16.3 SR8's first sentence, read out of the spec, split into the usage names it lists:
    /// "The PICTURE clause shall not be specified for the subject of a RENAMES clause or for an item whose
    /// usage is binary-char, …, or program-pointer."</summary>
    private static List<string> Sr8UsageNames()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.16\.3\b"));
        Assert.True(start >= 0, "§13.16.3 is missing from specs/ISO_COBOL.md — this guard must follow the clause.");
        int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.16\.4\b"));
        Assert.True(end > start, "§13.16.4 not found after §13.16.3 — the heading shape changed.");

        string? sr8 = lines[start..end].FirstOrDefault(l => Regex.IsMatch(l, @"^8\\?\)\s"));
        Assert.NotNull(sr8);
        var m = Regex.Match(sr8!, @"for an item whose usage is (.+?)\.\s*(?:For any other|$)");
        Assert.True(m.Success,
            "§13.16.3 SR8's usage list did not parse — fix the scanner against the printed sentence, never the "
            + $"other way round. The clause reads: {sr8}");

        var names = m.Groups[1].Value
            .Replace(", or ", ", ").Replace(" or ", ", ")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        Assert.True(names.Count >= 12,
            $"only {names.Count} usage names parsed from §13.16.3 SR8 — fix the scanner, do not lower the floor.");
        return names;
    }

    /// <summary>Every usage §13.16.3 SR8 names, that this compiler has a <see cref="Usage"/> member for, is in
    /// the set. A name with no member yet would be a FORWARD OBLIGATION the test holds open by naming it, never
    /// a silent pass — and message-tag, the last one outstanding (a 2023 addition, VCR row 32), acquired its
    /// member with kb/Work PB487, so the expected set is now EMPTY. ⛔ If a future usage arrives without a
    /// member, name it here; do not relax the assertion to a subset check.</summary>
    [Fact]
    public void EverySr8NamedUsage_WithAMember_IsPictureless()
    {
        var byWord = Enum.GetValues<Usage>().ToLookup(u => UsageFamilies.UsageWord(u));
        var unmodelled = new List<string>();
        foreach (string name in Sr8UsageNames())
        {
            // The clause spells the usages in lower case with hyphens ("binary-char", "object reference"),
            // which is UsageWord's rendering lower-cased — the SAME table the diagnostics spell from.
            string word = name.ToUpperInvariant();
            if (!byWord.Contains(word)) { unmodelled.Add(name); continue; }
            foreach (Usage u in byWord[word])
                Assert.True(UsageFamilies.IsPictureless(u),
                    $"ISO §13.16.3 SR8 names '{name}' among the usages for which \"the PICTURE clause shall not "
                    + $"be specified\", but UsageFamilies.IsPictureless({u}) is false — a PICTURE beside it would "
                    + "be accepted, on the written-clause arm and the §13.18.60.4 GR1 inherited arm alike.");
        }
        // Every name SR8 spells now has a Usage member (message-tag was the last, kb/Work PB487). A non-empty
        // list here means the standard names a usage this compiler does not model — say WHICH, do not pass.
        Assert.Equal([], unmodelled);
    }

    /// <summary>The converse: every member the set claims is EITHER named by SR8 or one of the standard
    /// floating-point usages the DETERMINATION beside the predicate covers (§13.18.60.4 GR14–GR18 pin each to a
    /// named ISO/IEC 60559 interchange format that no picture character-string can denote, and SR8's list
    /// predates them). ⛔ That exception is a PREDICATE — <see cref="UsageFamilies.IsStandardFloat"/> — not a
    /// list, so it cannot quietly grow; anything else added to the set fails here until the determination beside
    /// it is written and this test is told about it.</summary>
    [Fact]
    public void EveryPicturelessMember_IsSr8Named_OrACoveredDetermination()
    {
        var named = Sr8UsageNames().Select(n => n.ToUpperInvariant()).ToHashSet();
        foreach (Usage u in Enum.GetValues<Usage>())
        {
            if (!UsageFamilies.IsPictureless(u)) continue;
            if (named.Contains(UsageFamilies.UsageWord(u))) continue;
            Assert.True(UsageFamilies.IsStandardFloat(u),
                $"UsageFamilies.IsPictureless({u}) is true, but ISO §13.16.3 SR8 does not name USAGE "
                + $"{UsageFamilies.UsageWord(u)} and it is not one of the standard floating-point usages the "
                + "documented determination covers. Either the standard names it (fix the scanner or the "
                + "transcription) or a determination has to be written down beside the predicate first.");
        }
    }
}
