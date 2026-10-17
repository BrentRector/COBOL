// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace CobolNet.Tests.Shared;

/// <summary>
/// ⛔ THE ONE READER of <c>specs/ISO_COBOL.md</c> FOR A DRIFT GUARD: a clause's own region, the numbered rules
/// inside it, and the normalization that decides what "contains" means.
/// <para>CLAUDE.md rule 1 names the failure mode every rule table in this repository is exposed to — a clause
/// number is INHERITED rather than re-derived, and <c>cite.py --check</c> on the number alone passes because the
/// quoted sentence really is somewhere in the standard. The answer is a guard that re-derives every row from the
/// transcription on every run, and every such guard needs exactly these three operations. They lived as private
/// copies inside <c>FileControlKeyRuleDriftTests</c>; the second table to need them
/// (<c>RecordClauseRuleDriftTests</c>) is the moment to extract rather than to copy, because the copy is the one
/// that would not learn about the next heading form the transcription grows.</para>
/// <para>⚖ NOT the same query as <c>VcrDriftTests.SpecClauseRegions</c>, which builds a normalized region map
/// for EVERY clause at once to answer "does this citation resolve anywhere in the standard". That one is a
/// whole-document index; this one is a per-clause reader that keeps the RAW lines, because a syntax-diagram
/// assertion has to see the punctuation the normalizer removes.</para>
/// </summary>
internal static class SpecClauseText
{
    /// <summary>A clause heading of any depth: <c>##### 13.18.43.3 Syntax rules</c>, or an annex's
    /// <c>### A.3 …</c>. The capture is the clause number.</summary>
    private static readonly Regex Heading =
        new(@"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(\s|$)", RegexOptions.Compiled);

    /// <summary>One printed, numbered rule: the transcription escapes the delimiter (<c>1\)</c>) so Markdown does
    /// not eat it as a list, and both forms are matched.</summary>
    private static readonly Regex Numbered = new(@"^(\d+)\\?\)\s+(.*)$", RegexOptions.Compiled);

    /// <summary>A clause 15 function heading, <c>### 15.60 MEAN function</c>: the captures are the section and the
    /// function name.</summary>
    private static readonly Regex FunctionHeading =
        new(@"\n### (15\.\d+) ([A-Z0-9-]+) function\s*\n", RegexOptions.Compiled);

    /// <summary>Every intrinsic function clause 15 defines, as (section, name) in document order: <c>15.60</c>,
    /// <c>MEAN</c>. The one map from a §15.x rule to the function it belongs to. Three drift guards wrote this
    /// regex privately until the third copy (kb/Work PB3173, branch review round 1) extracted it.</summary>
    public static List<(string Section, string Name)> FunctionSections(string spec) =>
        [.. FunctionHeading.Matches(spec).Select(m => (m.Groups[1].Value, m.Groups[2].Value))];

    /// <summary>The transcription, line by line. Read on every call: a drift guard that cached it could not be
    /// used to prove a repair landed.</summary>
    public static string[] Lines() => File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

    /// <summary>Compare on WORDS only — punctuation, quoting and line wrapping are typography, not content. The
    /// same normalization <c>scripts/spec/cite.py</c> uses, so the two agree about what "contains" means.</summary>
    public static string Norm(string s) =>
        Regex.Replace(Regex.Replace(s, @"[^\w\s]", " "), @"\s+", " ").Trim().ToLowerInvariant();

    /// <summary>The RAW lines of one clause's OWN region: from its heading to the next heading of any depth —
    /// the region <c>cite.py --check</c> asserts against, which is what makes a wrong clause number fail. Raw,
    /// not normalized, because a general-format figure is nothing but the punctuation <see cref="Norm"/> strips.
    /// <para>Throws when the clause is absent, naming the clause: a row citing a clause the transcription does
    /// not have is a red test, never an empty region that quietly matches nothing.</para></summary>
    public static string[] ClauseRegion(string[] lines, string clause)
    {
        int start = Array.FindIndex(lines,
            l => Heading.Match(l) is { Success: true } m && m.Groups[1].Value == clause);
        if (start < 0)
            throw new InvalidOperationException(
                $"§{clause} is missing from specs/ISO_COBOL.md — a table row cites a clause the transcription does not have.");
        int end = Array.FindIndex(lines, start + 1, l => Heading.IsMatch(l));
        return lines[start..(end < 0 ? lines.Length : end)];
    }

    /// <summary>The printed, numbered rules of a clause region, keyed by their printed number.</summary>
    public static Dictionary<int, string> NumberedRules(string[] region)
    {
        var rules = new Dictionary<int, string>();
        foreach (string l in region)
            if (Numbered.Match(l) is { Success: true } m)
                rules[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();
        return rules;
    }

    /// <summary>The FORMAT heading each numbered rule of a region is printed under, keyed by rule number.
    /// §13.18.43.3 and its kin divide their syntax rules with bare <c>ALL FORMATS</c> / <c>FORMAT 1</c> lines
    /// rather than with subheadings, and that division is NORMATIVE — it is what says SR3 is a Format 1 rule and
    /// SR4 a Format 2 rule. A table with a format column can therefore have that column re-derived from the
    /// standard instead of trusted.
    /// <para>A rule printed before any such line maps to <c>null</c> (the clause states no division).</para></summary>
    public static Dictionary<int, string?> RuleFormatHeadings(string[] region)
    {
        var headings = new Dictionary<int, string?>();
        string? current = null;
        foreach (string l in region)
        {
            string t = l.Trim();
            if (t is "ALL FORMATS" || Regex.IsMatch(t, @"^FORMAT [0-9]+$")) current = t;
            else if (Numbered.Match(l) is { Success: true } m) headings[int.Parse(m.Groups[1].Value)] = current;
        }
        return headings;
    }
}
