// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DENOMINATOR IS COMPLETE — per clause, counted, every build.
///
/// <para><c>docs/rearchitecture/spec-rule-catalog.json</c> is the enumeration the PHASE-14 traceability
/// inventory is built from, and its row count is the denominator v1.0 is defined against (owner decision D13:
/// zero GAP = P14 done). Every completeness critic inside <c>scripts/spec/extract_rule_catalog.py</c> asks
/// "did this rule block yield SOMETHING?" — and a block that yields 16 of its 42 rules passes all of them,
/// because 16 is not zero. That is precisely what happened (fix-queue PB689): the transcription rendered the
/// in-clause group label <c>SEQUENTIAL FILES</c> as <c>## SEQUENTIAL FILES</c> inside §14.9.51.4's general
/// rules, the block scan closed the block at it, and GR17–GR42 of the WRITE statement — twenty-six normative
/// rules including the line-sequential '71' status — never became inventory rows. Five clauses were short by
/// sixty rules in total, and the published GAP was therefore measured against a denominator that flattered the
/// project without anyone being able to see it.</para>
///
/// <para>So this gate counts. For every clause the catalog holds rules for, the ordinals the TRANSCRIPTION
/// PRINTS at column 0 must equal the rows the CATALOG HOLDS — no segmentation reasoning, no vocabulary, just
/// two counts that have to agree. The extractor may decide what an ordinal is CALLED (top-level <c>GR-x-7</c>
/// versus sub-list <c>GR-x-L3.2</c>); it may never lose one, and an ordinal it cannot place is filed as a nest
/// and reported. A clause below its printed count has lost rules whatever the segmentation says, and a clause
/// above it has invented them — the more dangerous direction, because inflation looks like thoroughness.</para>
///
/// <para>⚠ This is a SPEC-side gate that deliberately does not run the Python extractor: it re-derives the
/// printed count from <c>specs/ISO_COBOL.md</c> independently, so it can contradict the code that built the
/// catalog. A critic that reuses the machinery it audits cannot see that machinery's bug.</para>
///
/// <para>⚠ <see cref="TheseChecks_ActuallyFail_OnAFabricatedSpec"/> exists because a gate that has only ever
/// been observed green is indistinguishable from one that inspects nothing
/// (<c>feedback_green_gates_arent_evidence</c>). It re-runs both predicates over a spec built to break them.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CatalogCoverageDriftTests
{
    // ── the shapes, shared with scripts/spec/extract_rule_catalog.py ────────────────────────────────────

    /// <summary>A markdown heading of any level.</summary>
    private static readonly Regex AnyHeading = new(@"^#{1,6}\s+\S", RegexOptions.Compiled);

    /// <summary>A heading whose text OPENS WITH A CLAUSE NUMBER — with or without a title. The terminology
    /// clause transcribes 300+ terms as a bare <c>### 3.1</c> with the term on the next line, and those are
    /// numbered clauses all the same.</summary>
    private static readonly Regex NumberedHeading =
        new(@"^#{1,6}\s+(?<num>\d+(?:\.\d+)*)\s*(?<title>.*?)\s*$", RegexOptions.Compiled);

    /// <summary>A heading that ends a numbered clause without carrying a clause number: the annex, bibliography
    /// and front-matter divisions. Everything else unnumbered is INTERIOR to the clause enclosing it — which is
    /// the defect shape this gate exists to make impossible.</summary>
    private static readonly Regex StructuralHeading = new(
        @"^#{1,6}\s+\*{0,2}(?:Annex\s+[A-Z]\b|Bibliography\b|Index\b|INTERNATIONAL\s+STANDARD\b|"
        + @"Information\s+technology\b|BIBLIOGRAPHY\b|Preface\b|Foreword\b|Introduction\b|Tables\b|Figures\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Page-break furniture spliced into rule prose at a page boundary; skipped before anything else,
    /// exactly as the extractor does.</summary>
    private static readonly Regex Furniture = new(
        @"^\s*(?:-{3,}|<a\s+id=""[^""]*""></a>|(?:\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?"
        + @"|(?:\d+\s+)?(?:©|\(c\))?\s*ISO/IEC\s+2023|Licensed to .*)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>A top-level rule ordinal at column 0. The transcription writes both <c>1\)</c> and <c>1\.</c>,
    /// and escapes the delimiter because an unescaped <c>1)</c> is a Markdown list marker.</summary>
    private static readonly Regex Ordinal = new(@"^(?<n>\d+)\\?[.)]\s", RegexOptions.Compiled);

    // ── the two artifacts ──────────────────────────────────────────────────────────────────────────────

    private static string[] SpecLines() =>
        File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));

    /// <summary>Catalog rows per clause, counting only the numbered-rule kinds. FMT (general-format diagrams)
    /// and DOC (Annex A.1 items) carry no ordinal in the clause body and are not part of this comparison.</summary>
    private static Dictionary<string, int> CatalogRowsByClause()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var rule in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            string kind = rule.GetProperty("kind").GetString()!;
            if (kind is "FMT" or "DOC")
            {
                continue;
            }

            string section = rule.GetProperty("section").GetString()!;
            counts[section] = counts.TryGetValue(section, out int n) ? n + 1 : 1;
        }

        Assert.True(counts.Count >= 600,
            $"only {counts.Count} clauses carry rules in spec-rule-catalog.json — the catalog is not the "
            + "denominator this gate was written against; regenerate it before trusting anything here");
        return counts;
    }

    /// <summary>Every FMT (general-format) row of the catalog, as (id, text).</summary>
    private static List<(string Id, string Text)> CatalogFmtRows()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        var rows = new List<(string, string)>();
        foreach (var rule in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            if (rule.GetProperty("kind").GetString() == "FMT")
            {
                rows.Add((rule.GetProperty("id").GetString()!, rule.GetProperty("text").GetString()!));
            }
        }

        Assert.True(rows.Count >= 300,
            $"only {rows.Count} FMT rows in spec-rule-catalog.json — the general-format scan has stopped finding "
            + "the diagrams; regenerate the catalog before trusting the general-format gate");
        return rows;
    }

    /// <summary>A general-format DIAGRAM as the transcription writes one (<c>&lt;pre&gt;</c> today; the fenced
    /// and <c>$$</c> forms are earlier renderings).</summary>
    private static readonly Regex FmtDiagram = new(@"<pre\b|```|\$\$", RegexOptions.Compiled);

    /// <summary>The numbered-and-named label of one format of a multi-format construct (§5.2.1: "the general
    /// format is separated into multiple formats that are numbered and named").</summary>
    private static readonly Regex FmtLabel = new(@"\bFormat\s+\d+", RegexOptions.Compiled);

    // ── the predicates, over injectable inputs so the fabricated-input test can reach them ─────────────

    /// <summary>Every unnumbered, non-structural heading, with the numbered clause it sits inside.</summary>
    private static List<(int Line, string Clause, string Text)> InteriorHeadings(string[] lines)
    {
        var found = new List<(int, string, string)>();
        string? clause = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (Furniture.IsMatch(line))
            {
                continue;
            }

            if (!AnyHeading.IsMatch(line))
            {
                continue;
            }

            var m = NumberedHeading.Match(line);
            if (m.Success && !line.TrimStart().StartsWith('['))
            {
                clause = m.Groups["num"].Value;
            }
            else if (StructuralHeading.IsMatch(line))
            {
                clause = null;
            }
            else if (clause is not null)
            {
                found.Add((i + 1, clause, line.TrimEnd()));
            }
        }

        return found;
    }

    /// <summary>Column-0 ordinal lines per numbered clause — what the transcription PRINTS as that clause's
    /// rules. A clause body runs to the next numbered or structural heading; an unnumbered interior heading is
    /// NOT a boundary, which is the whole point (a boundary there is how sixty rules disappeared). An in-clause
    /// group label is a heading in neither the printed standard nor this count.</summary>
    private static Dictionary<string, int> PrintedOrdinalsByClause(string[] lines)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        string? clause = null;
        foreach (string line in lines)
        {
            if (Furniture.IsMatch(line))
            {
                continue;
            }

            if (AnyHeading.IsMatch(line))
            {
                var m = NumberedHeading.Match(line);
                if (m.Success && !line.TrimStart().StartsWith('['))
                {
                    clause = m.Groups["num"].Value;
                    counts.TryAdd(clause, 0);
                }
                else if (StructuralHeading.IsMatch(line))
                {
                    clause = null;
                }

                // ⛔ ANYTHING ELSE UNNUMBERED IS NOT A BOUNDARY. Closing the clause here is the extractor bug
                // this gate exists to contradict, so the count deliberately keeps reading — a truncating
                // extractor then reports FEWER rows than the transcription prints, and this test goes red.
                continue;
            }

            if (clause is not null && Ordinal.IsMatch(line))
            {
                counts[clause] = counts.TryGetValue(clause, out int n) ? n + 1 : 1;
            }
        }

        return counts;
    }

    /// <summary>The FMT rows that carry neither a diagram nor a Format label — a row with nothing a grammar rule
    /// could be verified against.</summary>
    private static List<string> UndrawnFmtRows(IEnumerable<(string Id, string Text)> rows) =>
        [.. rows.Where(r => !FmtDiagram.IsMatch(r.Text) && !FmtLabel.IsMatch(r.Text))
                .Select(r => $"{r.Id}: {(r.Text.Length > 80 ? r.Text[..80] + "…" : r.Text)}")];

    private static List<string> Disagreements(Dictionary<string, int> printed, Dictionary<string, int> held) =>
        [.. held.Where(kv => printed.GetValueOrDefault(kv.Key, 0) != kv.Value)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"§{kv.Key}: the transcription prints {printed.GetValueOrDefault(kv.Key, 0)} "
                              + $"ordinal line(s), the catalog holds {kv.Value} row(s)")];

    // ── the gates ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The count gate. A clause below its printed count has LOST rules from the denominator; a clause
    /// above it has INVENTED them.</summary>
    [Fact]
    public void EveryClause_HoldsExactlyTheOrdinals_TheTranscriptionPrints()
    {
        var disagreements = Disagreements(PrintedOrdinalsByClause(SpecLines()), CatalogRowsByClause());
        Assert.True(disagreements.Count == 0,
            "the rule catalog and the transcription disagree on how many rules a clause has. A clause SHORT of "
            + "its printed count has lost normative rules from the P14 denominator (fix-queue PB689 — sixty "
            + "rules across five clauses, invisible because none of the catalog's critics count); a clause OVER "
            + "it has absorbed text that is not a rule. Regenerate the catalog "
            + "(python scripts/spec/extract_rule_catalog.py) and, if it still disagrees, read the clause and "
            + "the printed page before changing either number:\n  "
            + string.Join("\n  ", disagreements));
    }

    /// <summary>The mechanism gate. An unnumbered heading inside a numbered clause is a transcription defect —
    /// the printed standard has no heading there — and it is the shape that hides rules from every tool that
    /// keys on the clause hierarchy, not only from this one.</summary>
    [Fact]
    public void NoUnnumberedInteriorHeading_SitsInsideANumberedClause()
    {
        var interior = InteriorHeadings(SpecLines());
        Assert.True(interior.Count == 0,
            "specs/ISO_COBOL.md renders a heading inside a numbered clause's text. The standard prints in-clause "
            + "group labels (SEQUENTIAL FILES, FORMAT 3, ALL FORMATS) in BODY type, not as headings — verified in "
            + "the PDF text layer on pp146, 463, 609, 680 and 820 — and the transcription's own convention is a "
            + "plain column-0 line (209 of 218 labels). Demote each of these to a plain line:\n  "
            + string.Join("\n  ", interior.Select(h => $"line {h.Line} in §{h.Clause}: {h.Text}")));
    }

    /// <summary>⛔ AN FMT ROW IS A FORMAT THE STANDARD PRINTS (kb/Work PB1536 Q2). The extractor used to emit
    /// one for any heading titled "General formats", and §5.2 — the description-techniques clause ABOUT general
    /// formats, whose body is only its sub-clause §5.2.1 — manufactured FMT-5.2: a permanent GAP row whose text
    /// was a bare anchor. A row with neither a diagram nor a Format label is that shape again, or a format whose
    /// diagram the transcription lost (§15.78.2 REVERSE printed as bare text until wave 60) — either way nothing
    /// a grammar rule can be verified against. Measured over the WRITTEN catalog, not the extractor's code.</summary>
    [Fact]
    public void EveryFmtRow_CarriesADiagramOrAFormatLabel()
    {
        var undrawn = UndrawnFmtRows(CatalogFmtRows());
        Assert.True(undrawn.Count == 0,
            "spec-rule-catalog.json holds general-format (FMT) rows with no diagram and no Format label. A "
            + "heading titled 'General format(s)' that is a PARENT clause (its body is its own sub-clauses) is not "
            + "a construct's format and must not be a row; a LEAF format clause without a diagram lost it in "
            + "transcription — render the printed page and restore the <pre>/<u> block, then regenerate the "
            + "catalog (python scripts/spec/extract_rule_catalog.py):\n  " + string.Join("\n  ", undrawn));
    }

    /// <summary>⛔ Every predicate fired against inputs built to break it. Without this, a refactor that made
    /// either scrape return nothing would leave two permanently-green assertions over an empty set.</summary>
    [Fact]
    public void TheseChecks_ActuallyFail_OnAFabricatedSpec()
    {
        // A minimal spec: one rule block of three rules, with a group label between rules 2 and 3.
        string[] good =
        [
            "#### 14.9.51 WRITE statement",
            "##### 14.9.51.4 General rules",
            @"1\) first rule.",
            @"2\) second rule.",
            "SEQUENTIAL FILES",
            @"3\) third rule.",
            "#### 14.9.52 Next statement",
        ];
        Assert.Empty(InteriorHeadings(good));
        Assert.Equal(3, PrintedOrdinalsByClause(good)["14.9.51.4"]);
        Assert.Empty(Disagreements(PrintedOrdinalsByClause(good),
            new Dictionary<string, int>(StringComparer.Ordinal) { ["14.9.51.4"] = 3 }));

        // PB689 itself: the label transcribed as a heading. The count must NOT change (the scan may not stop
        // there) and the interior-heading gate must report it.
        string[] headingified = [.. good.Select(l => l == "SEQUENTIAL FILES" ? "## SEQUENTIAL FILES" : l)];
        Assert.Equal(3, PrintedOrdinalsByClause(headingified)["14.9.51.4"]);
        var reported = InteriorHeadings(headingified);
        Assert.Single(reported);
        Assert.Equal("14.9.51.4", reported[0].Clause);

        // A catalog short of the printed count — the truncation PB689 shipped for months.
        Assert.Single(Disagreements(PrintedOrdinalsByClause(good),
            new Dictionary<string, int>(StringComparer.Ordinal) { ["14.9.51.4"] = 2 }));

        // ... and one over it, the inflation direction.
        Assert.Single(Disagreements(PrintedOrdinalsByClause(good),
            new Dictionary<string, int>(StringComparer.Ordinal) { ["14.9.51.4"] = 4 }));

        // An annex heading still ends a clause, so annex prose can never be counted as that clause's rules.
        string[] intoAnnex =
        [
            "##### 16.2.2.2 General rules",
            @"1\) only rule.",
            "## Annex A (normative)",
            "1. not a rule.",
            "2. also not a rule.",
        ];
        Assert.Equal(1, PrintedOrdinalsByClause(intoAnnex)["16.2.2.2"]);
        Assert.Empty(InteriorHeadings(intoAnnex));

        // PB1536 Q2: the row §5.2's heading manufactured (its text was only the anchor of §5.2.1) is reported; a
        // drawn format and a labelled multi-format construct are not.
        var undrawn = UndrawnFmtRows(
        [
            ("FMT-5.2", "<a id=\"section-5-2-1\"></a>"),
            ("FMT-15.78.2", "<pre style=\"line-height:1\"> <u>FUNCTION</u> <u>REVERSE</u> ( argument-1 ) </pre>"),
            ("FMT-7.2.4.2", "Format 1 (replacing): == pseudo-text-1 =="),
        ]);
        Assert.Single(undrawn);
        Assert.StartsWith("FMT-5.2:", undrawn[0], StringComparison.Ordinal);
    }
}
