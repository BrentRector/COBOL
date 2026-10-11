// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;

using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GUARD THAT KEEPS "ONE MODEL, SEVEN PREDICATES" TRUE (kb/Work PB445). ISO §14.9.37.3 carries SEVEN
/// consecutive FORMAT-2 syntax rules — SR7 through SR13 — and every one of them was unenforced, TOGETHER, for one
/// structural reason: they all read either the OCCURS KEY phrase of identifier-1 or the WHEN phrase decomposed
/// into its Format-2 operands, and neither existed as a model. <see cref="CobolNet.Binding.Validation"/>'s
/// <c>SearchAllFormat2Rules</c> builds both and writes each rule as a predicate over them.
/// <para>The value of that shape is that the NEXT Format-2 rule is a predicate rather than another tree walk —
/// and this test is what keeps the claim honest. It SCRAPES the Format-2 syntax rules out of
/// <c>specs/ISO_COBOL.md</c> rather than listing them, so a rule added to the transcription (a later edition, or
/// a transcription repair that recovers one the OCR lost) turns red here until it is both implemented and
/// assigned a diagnostic code. A hand-written list of "the seven rules" would have gone quietly stale instead.
/// </para>
/// <para>⚠ It reads the MODULE SOURCE for the citation, not the compiled assembly: what is being guarded is that
/// the rule is written down in the one place the rules live, with its number, so the next reader can find it.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SearchAllFormat2RuleCoverageDriftTests
{
    /// <summary>The three codes PB445 allocated, and nothing else: the Format-2 rules divide by the OPERAND a
    /// violation is about, which is what a user has to change. A WHEN whose SHAPE is not Format 2 at all is the
    /// existing COBOLNET1757 and is deliberately NOT in this set — it is a general-format verdict, not a
    /// syntax-rule one, so it claims no SR number.</summary>
    private static readonly string[] Format2Codes = ["COBOLNET1964", "COBOLNET1965", "COBOLNET1966"];

    /// <summary>(section → rule numbers) for §14.9.37.3, scraped from the transcription. The clause prints its
    /// rules under three banners — ALL FORMATS, FORMAT 1, FORMAT 2 — and which banner a rule sits under is the
    /// whole question here, so the scrape tracks them rather than assuming a numeric range.</summary>
    private static Dictionary<string, List<int>> ScrapeSearchSyntaxRules()
    {
        string[] lines = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md")).Split('\n');
        var by = new Dictionary<string, List<int>>(StringComparer.Ordinal)
        {
            ["ALL FORMATS"] = [], ["FORMAT 1"] = [], ["FORMAT 2"] = [],
        };
        bool inClause = false;
        string section = "ALL FORMATS";

        foreach (string raw in lines)
        {
            string line = raw.TrimEnd('\r').Trim();
            if (Regex.IsMatch(line, @"^#+\s+14\.9\.37\.3\s+Syntax rules\s*$")) { inClause = true; continue; }
            if (!inClause) continue;
            if (line.StartsWith('#')) break;                       // the next heading closes the clause
            if (by.ContainsKey(line)) { section = line; continue; }
            var m = Regex.Match(line, @"^(\d+)\\?\)\s");
            if (m.Success) by[section].Add(int.Parse(m.Groups[1].Value));
        }

        // SHAPE BEFORE CONTENT: if the scrape silently lost the clause every assertion below passes on empty
        // sets, which is the failure mode feedback_green_gates_arent_evidence is about.
        int total = by.Values.Sum(v => v.Count);
        Assert.True(total >= 13, $"scraped only {total} syntax rules from §14.9.37.3 — the scrape lost the clause");
        Assert.True(by["FORMAT 2"].Count >= 7,
            $"scraped only {by["FORMAT 2"].Count} FORMAT 2 rules from §14.9.37.3 — the section split broke");
        return by;
    }

    [Fact]   // The negative control: the scrape really does tell the three banners apart.
    public void TheScrape_PutsRule6UnderFormat1_AndRule7UnderFormat2()
    {
        var by = ScrapeSearchSyntaxRules();
        Assert.Contains(6, by["FORMAT 1"]);          // "Condition-1 may be any conditional expression…"
        Assert.Contains(7, by["FORMAT 2"]);          // "The OCCURS clause … shall contain the KEY phrase."
        Assert.DoesNotContain(6, by["FORMAT 2"]);
        Assert.DoesNotContain(2, by["FORMAT 2"]);    // an ALL FORMATS rule
        // The three banners partition the rules — no number is counted twice.
        var all = by.Values.SelectMany(v => v).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]   // Every FORMAT-2 syntax rule is written down in the one module that owns them.
    public void EveryFormat2SyntaxRule_IsCitedInTheRuleModule()
    {
        var by = ScrapeSearchSyntaxRules();
        string module = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Validation",
                                                      "SearchAllFormat2Rules.cs"));
        var missing = by["FORMAT 2"].Where(n => !Regex.IsMatch(module, $@"\bSR{n}\b")).ToList();
        Assert.True(missing.Count == 0,
            "ISO §14.9.37.3 prints a FORMAT 2 syntax rule that SearchAllFormat2Rules.cs does not cite: "
            + string.Join(", ", missing.Select(n => "SR" + n))
            + ". The seven rules are predicates over ONE model (the ordered OCCURS KEY phrase and the decomposed "
            + "WHEN); a new one is another predicate over the same model, not another tree walk — add it there "
            + "and give it a code below.");
    }

    [Fact]   // …and every one is claimed by a diagnostic, so a new rule forces a code decision.
    public void EveryFormat2SyntaxRule_IsClaimedByOneOfTheThreeCodes()
    {
        var by = ScrapeSearchSyntaxRules();
        var descriptors = DiagnosticCatalog.All.Where(d => Format2Codes.Contains(d.Code)).ToList();
        Assert.Equal(Format2Codes.Length, descriptors.Count);

        string claimed = string.Join(" ", descriptors.Select(d => d.IsoSection));
        var unclaimed = by["FORMAT 2"].Where(n => !Regex.IsMatch(claimed, $@"\bSR{n}\b")).ToList();
        Assert.True(unclaimed.Count == 0,
            "ISO §14.9.37.3 prints a FORMAT 2 syntax rule no PB445 diagnostic claims in its IsoSection: "
            + string.Join(", ", unclaimed.Select(n => "SR" + n))
            + ". The codes divide the rules by the OPERAND the violation is about — the table's declaration "
            + "(COBOLNET1964), the KEY side of a WHEN (COBOLNET1965), the SENDING side (COBOLNET1966) — so put "
            + "the new rule under the one whose operand it is about, and say so in that descriptor's IsoSection.");
    }

    [Fact]   // The KEY phrase is ONE ordered list, which is what makes SR11 expressible at all.
    public void TheOccursKeyPhrase_IsOneOrderedList_NotAListPerDirectionWord()
    {
        string odo = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Model", "OdoModel.cs"));
        Assert.Contains("List<OccursKey> Keys", odo);
        // §14.9.37.3 SR11 is a rule about the ORDER of the KEY phrase and §13.18.38.4 GR3 makes that order the
        // order of significance, so a mixed `ASCENDING KEY IS A B DESCENDING KEY IS C` phrase loses which key
        // precedes which the moment it is split per direction word. The split shape is what PB445 replaced.
        Assert.DoesNotContain("AscendingKeyNames", odo);
        Assert.DoesNotContain("DescendingKeyNames", odo);
    }
}
