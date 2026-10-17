// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for <c>docs/CONFORMANCE.md</c> §2 — the Annex A.3 processor-dependent element register, which
/// ISO §4.2.6 makes NORMATIVE user documentation and not a summary table: "To meet the requirements of standard
/// COBOL, the implementor shall document the processor-dependent language elements for which the implementation
/// claims support", and "The absence of processor-dependent elements from an implementation shall be specified in
/// the implementor's user documentation." A row of that register is therefore a conformance artifact, and its
/// citation is load-bearing — a reader who cannot reach the clause cannot check the claim.
///
/// <para>⛔ WHY THIS EXISTS (kb/Work PB292). Eight of the register's forty rows carried an UNRESOLVED PLACEHOLDER
/// in the § column — <c>13.x</c> on rows 5, 12, 13, 26 and 40, plus <c>8.x</c>, <c>9.x</c> and <c>11.x</c> — and
/// they had been there since the document was created. PB292's own note recorded the class as FIVE rows, because
/// it was found by grepping the one spelling <c>13.x</c>; the other three are the same defect wearing a different
/// chapter number (feedback_scan_all_similar). A placeholder is not a small blemish here: §4.2.6's documentation
/// obligation is discharged by the row, and a row that cites nothing discharges nothing. Resolving them by hand
/// once fixes eight rows; this test is what stops the ninth.</para>
///
/// <para>⛔ AND WHY IT IS A TEST RATHER THAN A CHECKLIST. A "remaining citations" list would be a second work
/// register (CLAUDE.md rule 8) and would go stale the moment a row was added. The DOCUMENT is the register; this
/// class only holds it to two shapes it must already have — every § cell resolves, and every diagnostic code the
/// register names is a code that actually exists.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class AnnexA3RegisterDriftTests
{
    /// <summary>The literal §2 heading — the register's own anchor in the conformance document.</summary>
    private const string SectionHeading = "## 2. Annex A.3";

    /// <summary>An unresolved citation: a clause path whose last component is the literal <c>x</c> standing in
    /// for the part nobody derived — <c>13.x</c>, <c>8.x</c>, and equally <c>12.4.x</c>, which the first draft
    /// of this pattern missed because it anchored on the FIRST component instead of the last (the same
    /// one-spelling mistake that made PB292's note say five placeholder rows where there were eight). A real
    /// clause path such as <c>13.18.10</c> or a pair such as <c>12.4.5.12.2 / 12.4.5.6.2</c> cannot match: the
    /// trailing lookahead rejects any further digit or letter.</summary>
    private static readonly Regex Placeholder = new(@"[0-9]\.x(?![0-9A-Za-z])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>One parsed row of the §2 table: the A.3 item number cell (which may name a RANGE, "28–30"), the
    /// element, the § citation cell, the disposition and the note.</summary>
    internal sealed record Row(string Items, string Element, string Section, string Disposition, string Note);

    /// <summary>Parse §2's markdown table. Stops at the next <c>## </c> heading, so a pipe table elsewhere in
    /// the document can never be mistaken for this register. The one parser of the register:
    /// <see cref="ReadmeDeclinedSummaryDriftTests"/> reads the rows through it.</summary>
    internal static List<Row> Register()
    {
        string[] lines = File.ReadAllLines(TestRepo.Docs("CONFORMANCE.md"));
        var rows = new List<Row>();
        bool inSection = false;
        foreach (string line in lines)
        {
            if (line.StartsWith(SectionHeading, StringComparison.Ordinal)) { inSection = true; continue; }
            if (!inSection) continue;
            if (line.StartsWith("## ", StringComparison.Ordinal)) break;
            if (!line.StartsWith("| ", StringComparison.Ordinal)) continue;
            var cells = line.Trim().Trim('|').Split('|');
            if (cells.Length < 5) continue;
            string items = cells[0].Trim();
            if (items is "A.3 #" || items.StartsWith("---", StringComparison.Ordinal)) continue;   // header / rule
            rows.Add(new Row(items, cells[1].Trim(), cells[2].Trim(), cells[3].Trim(),
                string.Join('|', cells[4..]).Trim()));
        }
        return rows;
    }

    /// <summary>The population assertion (feedback_verdict_evidence_invariant). Annex A.3 lists 46 numbered
    /// items and the register covers all of them across 40 rows (seven rows name a range). A parse that found
    /// nothing — a renamed heading, a table converted to a list — would make every obligation below vacuously
    /// green, which is the exact failure this whole register exists to stop.</summary>
    [Fact]
    public void TheRegister_Parses_AndCoversEveryAnnexA3Item()
    {
        var rows = Register();
        Assert.True(rows.Count >= 40,
            $"docs/CONFORMANCE.md §2 parsed to only {rows.Count} row(s) — the table scanner is broken and every "
            + "check in this class would pass without looking at anything");

        var covered = new HashSet<int>();
        foreach (var r in rows)
            foreach (Match m in Regex.Matches(r.Items, @"[0-9]+"))
            {
                int n = int.Parse(m.Value);
                // A range cell is written with an EN DASH ("28–30" / "6–7"); fill it in.
                covered.Add(n);
            }
        foreach (var r in rows)
        {
            var range = Regex.Match(r.Items, @"^([0-9]+)\s*[–—-]\s*([0-9]+)$");
            if (!range.Success) continue;
            for (int n = int.Parse(range.Groups[1].Value); n <= int.Parse(range.Groups[2].Value); n++)
                covered.Add(n);
        }
        var missing = Enumerable.Range(1, 46).Where(n => !covered.Contains(n)).ToList();
        Assert.True(missing.Count == 0,
            "docs/CONFORMANCE.md §2 has no row for Annex A.3 item(s) " + string.Join(", ", missing)
            + " — ISO §4.2.6 obliges the implementor to document the disposition of every processor-dependent "
            + "element, claimed or absent");
    }

    /// <summary>⛔ THE OBLIGATION. No § cell is an unresolved placeholder. §4.2.6 makes each row user
    /// documentation of a claim or of an absence; a row citing <c>13.x</c> points the reader at a chapter and
    /// leaves them to find the clause, which is not a citation and cannot be checked (CLAUDE.md rule 1).</summary>
    [Fact]
    public void EveryRow_CitesAResolvedClause()
    {
        var unresolved = Register()
            .Where(r => Placeholder.IsMatch(r.Section))
            .Select(r => $"item {r.Items} ({r.Element}) cites '{r.Section}'")
            .ToList();
        Assert.True(unresolved.Count == 0,
            "docs/CONFORMANCE.md §2 row(s) with an UNRESOLVED clause citation — derive the clause from "
            + "specs/ISO_COBOL.md and validate it with `python scripts/spec/cite.py --check`:\n"
            + string.Join("\n", unresolved));
    }

    /// <summary>Prove the placeholder guard can FAIL (feedback_green_gates_arent_evidence). Run against
    /// fabricated cells rather than by mutating the register, and pin the NEGATIVE side too — a real clause
    /// path must not trip the pattern, or the guard would be unusable and get deleted.</summary>
    [Fact]
    public void ThePlaceholderGuard_CanFail_AndDoesNotFireOnRealClauses()
    {
        foreach (string bad in new[] { "13.x", "8.x", "9.x", "11.x", "12.4.x", "13.x / A.3 item 26" })
            Assert.True(Placeholder.IsMatch(bad), $"the placeholder pattern missed '{bad}'");
        foreach (string good in new[]
                 {
                     "12.4.5.11", "13.18.10", "11.9.8", "8.1.2", "9.3.5.3 / 9.3.6",
                     "12.4.5.12.2 / 12.4.5.6.2", "13.18.43.3 SR7/SR8", "13.18.13 / A.3 item 27", "SPECIAL-NAMES",
                 })
            Assert.False(Placeholder.IsMatch(good), $"the placeholder pattern wrongly fired on '{good}'");
    }

    /// <summary>Every <c>COBOLNET####</c> the register names is a code the compiler really emits. A row that
    /// cites a diagnostic is telling the reader how to OBSERVE the disposition it records — the whole point of
    /// §4.2.6 ¶3's warning mechanism — so a stale code silently converts an observable claim into an
    /// unobservable one, which is the PB292 defect in a different disguise.
    /// <para>⛔ THE ORACLE IS "IS IT EMITTED", NOT "IS IT IN THE CATALOGUE", and the difference was MEASURED
    /// here: rows 2, 17 and 19 cite COBOLNET0806 and COBOLNET1564, both still emitted as BARE STRING LITERALS from
    /// the binder rather than as <see cref="DiagnosticCatalog"/> descriptors (`OptionsBinder`,
    /// `DataBinder`/`IntrinsicArgumentRules`; row 45's COBOLNET0822 became a descriptor in kb/Work PB745 and moved to
    /// §5, the register of the annex it declines). A catalogue-only test would have gone red on rows that document
    /// the truth, so it would have been weakened or deleted; migrating those descriptors is a separate mechanism, and this register's obligation is
    /// only that its citations are live. <c>DiagnosticRegistryDriftTests</c> owns the catalogue-membership
    /// question and must stay the only place that asks it (feedback_one_rule_one_place).</para></summary>
    [Fact]
    public void EveryDiagnosticCodeNamedInTheRegister_IsEmittedByTheCompiler()
    {
        var emitted = DiagnosticCatalog.All.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"COBOLNET[0-9]{4}"))
                emitted.Add(m.Value);
        }
        Assert.True(emitted.Count >= 200,
            $"only {emitted.Count} diagnostic code(s) found across the catalogue and src/ — the source scan is "
            + "broken and this check would pass on a register full of dangling codes");

        var cited = Register()
            .SelectMany(r => Regex.Matches(r.Note + " " + r.Disposition, @"COBOLNET[0-9]{4}")
                .Select(m => (Row: r, Code: m.Value)))
            .ToList();
        Assert.True(cited.Count >= 4,
            $"only {cited.Count} diagnostic code(s) found in §2 — the table scanner is broken; rows 2, 25, 26 "
            + "and 27 each name one");
        var dangling = cited.Where(c => !emitted.Contains(c.Code))
            .Select(c => $"item {c.Row.Items} names {c.Code}, which nothing in src/ emits")
            .Distinct().ToList();
        Assert.True(dangling.Count == 0, string.Join("\n", dangling));
    }

    /// <summary>Prove the dangling-code guard can FAIL (feedback_green_gates_arent_evidence): a code of the
    /// right SHAPE that nothing emits must not be found. Uses a fabricated code rather than mutating the
    /// register, and pins a code that IS emitted as the control.</summary>
    [Fact]
    public void TheDanglingCodeGuard_CanFail()
    {
        var emitted = DiagnosticCatalog.All.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"COBOLNET[0-9]{4}"))
                emitted.Add(m.Value);
        }
        Assert.DoesNotContain("COBOLNET9999", emitted);
        Assert.Contains("COBOLNET1778", emitted);
    }
}
