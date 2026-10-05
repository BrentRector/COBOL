// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD ON <see cref="RecordClauseRules"/>'S TABLE (kb/Work PB721) — the sibling of
/// <see cref="FileControlKeyRuleDriftTests"/>, over the RECORD clause's own size syntax rules
/// (ISO/IEC 1989:2023 §13.18.43.3).
/// <para>The table is the compiler's whole statement of what a RECORD clause's integers require, and it carries
/// two things a comment cannot be trusted with: a clause number per row, and a GENERAL FORMAT per row. Both are
/// re-derived here from <c>specs/ISO_COBOL.md</c> on every run — the format column from the standard's own bare
/// <c>FORMAT 1</c> / <c>FORMAT 2</c> / <c>FORMAT 3</c> division lines, which is what makes a row screened on the
/// wrong format red instead of shipping. CLAUDE.md rule 1's failure mode is the INHERITED clause number, and
/// <c>cite.py --check</c> on a number alone cannot catch it.</para>
/// <para>It also closes the loop rule 5 asks for: implementing the next rule of this subclause must be adding a
/// ROW, not writing a new <c>if</c> somewhere else.</para>
/// </summary>
public sealed class RecordClauseRuleDriftTests
{
    private const string Clause = "13.18.43.3";

    // ── The scanner is proven able to fail before any row is trusted ────────────────────────────────────────

    /// <summary>⛔ A GREEN CHECK THAT NEVER LOOKED AT ANYTHING IS NOT EVIDENCE. §13.18.43.3 has exactly nine
    /// printed syntax rules; the region is bounded by the NEXT heading, so a sentence from the neighbouring
    /// general-rules clause must NOT be found in it — and must be found where it really lives.</summary>
    [Fact]
    public void TheClauseScanner_FindsTheNineRulesAndRejectsAForeignSentence()
    {
        var lines = SpecClauseText.Lines();
        var rules = SpecClauseText.NumberedRules(SpecClauseText.ClauseRegion(lines, Clause));
        Assert.Equal(9, rules.Count);
        Assert.Contains("Integer-3 shall be greater than integer-2", rules[5]);

        // §13.18.43.4 GR7 is the GENERAL rule about what integer-2 MEANS. It is one subclause away and reads
        // like a syntax rule about the same operand — the exact neighbour a region that swallowed its successor
        // would absorb, and the exact confusion this note's own report was filed under.
        string foreign = SpecClauseText.Norm(
            "Integer-2 specifies the minimum number of bytes to be contained in any record of the file");
        Assert.DoesNotContain(SpecClauseText.ClauseRegion(lines, Clause),
            l => SpecClauseText.Norm(l).Contains(foreign));
        Assert.Contains(SpecClauseText.ClauseRegion(lines, "13.18.43.4"),
            l => SpecClauseText.Norm(l).Contains(foreign));
    }

    /// <summary>⛔ AND THE FORMAT SCANNER TOO: the division lines are the only thing that says which general
    /// format a rule is printed under, so the reader that finds them is proven on known ground first.</summary>
    [Fact]
    public void TheFormatScanner_ReadsTheStandardsOwnDivisionOfTheNineRules()
    {
        var headings = SpecClauseText.RuleFormatHeadings(SpecClauseText.ClauseRegion(SpecClauseText.Lines(), Clause));
        Assert.Equal("ALL FORMATS", headings[1]);
        Assert.Equal("ALL FORMATS", headings[2]);
        Assert.Equal("FORMAT 1", headings[3]);
        Assert.Equal("FORMAT 2", headings[4]);
        Assert.Equal("FORMAT 2", headings[7]);
        Assert.Equal("FORMAT 3", headings[8]);
        Assert.Equal("FORMAT 3", headings[9]);
    }

    // ── The guard itself ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every row's RULE TEXT is inside the clause the row cites — the mechanical
    /// <c>cite.py --check</c> guarantee, applied to the table itself rather than to a comment about it.</summary>
    [Fact]
    public void EveryRow_QuotesTextThatIsInsideTheClauseItCites()
    {
        var lines = SpecClauseText.Lines();
        Assert.True(RecordClauseRules.Catalog.Count >= 5,
            $"only {RecordClauseRules.Catalog.Count} rows — the table lost rules; fix the table, do not lower the floor.");
        foreach (var rule in RecordClauseRules.Catalog)
        {
            string needle = SpecClauseText.Norm(rule.RuleText);
            Assert.True(SpecClauseText.ClauseRegion(lines, rule.Clause).Any(l => SpecClauseText.Norm(l).Contains(needle)),
                $"§{rule.Clause} does not contain \"{rule.RuleText}\" — the row's clause number is wrong or the text drifted.");
        }
    }

    /// <summary>Every row quotes the PRINTED ORDINAL its rule-id names — not merely something somewhere in the
    /// clause. This is the half a clause-only check misses: a real clause can answer a different question.</summary>
    [Fact]
    public void EveryRow_QuotesTheOrdinalItNames()
    {
        var lines = SpecClauseText.Lines();
        foreach (var rule in RecordClauseRules.Catalog)
        {
            var m = Regex.Match(rule.RuleId, @"^SR-(?<clause>[0-9.]+)-(?<n>\d+)$");
            Assert.True(m.Success, $"rule-id '{rule.RuleId}' is not the inventory's SR-<clause>-<n> shape.");
            Assert.Equal(rule.Clause, m.Groups["clause"].Value);
            var numbered = SpecClauseText.NumberedRules(SpecClauseText.ClauseRegion(lines, rule.Clause));
            int n = int.Parse(m.Groups["n"].Value);
            Assert.True(numbered.ContainsKey(n), $"§{rule.Clause} has no printed syntax rule {n}.");
            Assert.Contains(SpecClauseText.Norm(rule.RuleText), SpecClauseText.Norm(numbered[n]));
        }
    }

    /// <summary>⛔ THE FORMAT COLUMN IS RE-DERIVED FROM THE STANDARD, never trusted. §13.18.43.3 divides its
    /// rules with bare <c>FORMAT n</c> lines, and that division decides which clauses a rule is in force over —
    /// a row screened on the wrong format either fires on source the standard permits or is silent on source it
    /// forbids, and neither shows up as a compile error.</summary>
    [Fact]
    public void EveryRow_IsScreenedOnTheFormatTheStandardPrintsItUnder()
    {
        var headings = SpecClauseText.RuleFormatHeadings(SpecClauseText.ClauseRegion(SpecClauseText.Lines(), Clause));
        foreach (var rule in RecordClauseRules.Catalog)
        {
            int n = int.Parse(Regex.Match(rule.RuleId, @"-(\d+)$").Groups[1].Value);
            string expected = rule.Format switch
            {
                RecordClauseFormat.Fixed => "FORMAT 1",
                RecordClauseFormat.Varying => "FORMAT 2",
                RecordClauseFormat.FixedOrVariable => "FORMAT 3",
                _ => throw new InvalidOperationException($"unhandled format {rule.Format}"),
            };
            Assert.True(headings.TryGetValue(n, out string? printed),
                $"§{Clause} has no printed syntax rule {n}.");
            Assert.Equal(expected, printed);
        }
    }

    /// <summary>⛔ THE "READ THE WHOLE PRINTED RULE" CLAMP (kb/Work PB743's lesson, applied before the defect
    /// rather than after it). SR4 states TWO obligations in one numbered sentence — "neither … a lesser number
    /// of bytes than that specified by integer-2 NOR … a greater number of bytes than that specified by
    /// integer-3" — and a rule read as one predicate gets one screen, with the other arm silent forever. The
    /// split is re-derived FROM THE SPEC here, so a rule that grows a third obligation makes this red instead of
    /// silently shipping two thirds.</summary>
    [Fact]
    public void Sr4StatesTwoObligations_AndTheTableHasARowForEachArm()
    {
        string printed = SpecClauseText.NumberedRules(SpecClauseText.ClauseRegion(SpecClauseText.Lines(), Clause))[4];
        var arms = printed.Split(" nor ", 2, StringSplitOptions.None);
        Assert.True(arms.Length == 2,
            $"§{Clause} rule 4 no longer reads as two arms joined by \"nor\" — re-derive the split before trusting the rows.");

        var rows = RecordClauseRules.Catalog.Where(r => r.RuleId == $"SR-{Clause}-4").ToList();
        Assert.True(rows.Count >= 2, $"SR4 states two obligations but only {rows.Count} row(s) quote it.");
        foreach (string arm in new[] { arms[0], "nor " + arms[1] })
            Assert.True(rows.Any(r => SpecClauseText.Norm(arm).Contains(SpecClauseText.Norm(r.RuleText))),
                $"no row of SR-{Clause}-4 quotes the arm \"{arm.Trim()}\" — one obligation of the rule has no screen.");
    }

    /// <summary>⛔ THE ABSENCES ARE DERIVED, NOT ASSUMED. The table deliberately carries no row for SR7 or SR8,
    /// and the whole justification is §5.5 1)'s sentence about what <c>integer-n</c> IS: "a fixed-point integer
    /// literal that shall be unsigned and nonzero unless otherwise specified in the associated rules". UNSIGNED
    /// is what makes a negative integer-2 unwritable — so the general format, not a screen, discharges those two
    /// rules — and "unless otherwise specified" is what makes SR7/SR8 the express permission for ZERO. If that
    /// sentence ever changes, the reasoning has to be redone rather than inherited.</summary>
    [Fact]
    public void TheReasonSr7AndSr8NeedNoRow_IsStillWhatTheStandardPrints()
    {
        var region = SpecClauseText.ClauseRegion(SpecClauseText.Lines(), "5.5");
        string needle = SpecClauseText.Norm(
            "it refers to a fixed-point integer literal that shall be unsigned and nonzero unless otherwise specified in the associated rules");
        Assert.Contains(region, l => SpecClauseText.Norm(l).Contains(needle));

        var rules = SpecClauseText.NumberedRules(SpecClauseText.ClauseRegion(SpecClauseText.Lines(), Clause));
        Assert.Contains("greater than or equal to zero", rules[7]);   // the override, for integer-2
        Assert.Contains("greater than or equal to zero", rules[8]);   // and for integer-4
        Assert.DoesNotContain(RecordClauseRules.Catalog, r => r.RuleId is $"SR-{Clause}-7" or $"SR-{Clause}-8");
    }

    /// <summary>The message a row ships names the row's OWN citation. A row whose sentence and whose printed §
    /// disagree is exactly the defect this file exists for, and the message is what a user reads.</summary>
    [Fact]
    public void EveryRow_ShipsAMessageThatNamesItsOwnCitation()
    {
        var file = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F" };
        var record = new DataItem { Level = 1, CobolName = "F-REC", CsName = "FRec" };
        var depending = new DataItem { Level = 1, CobolName = "F-LEN", CsName = "FLen", RootSection = EntrySection.File };
        foreach (var rule in RecordClauseRules.Catalog)
        {
            var clause = new RecordClauseFacts(rule.Format, 3, 7, default);
            // ⛔ THE SUBJECT CARRIES A REAL RECORD AND A REAL DEPENDING ITEM, so a message that renders their names,
            // size and section is exercised down the branch a program actually reads rather than down a fallback.
            var subject = new RecordClauseSubject(record, 3, 7, depending);
            Assert.Contains(rule.Citation, rule.Message(file, clause, subject));
        }
    }

    /// <summary>⛔ SR6 STATES THREE OBLIGATIONS — ELEMENTARY, UNSIGNED INTEGER, AND THE SECTION — and the table has a
    /// row for each (kb/Work PB858), the SR4 clamp one rule over. The arms are re-derived from the printed rule, so
    /// a rule that is reworded or grows a fourth conjunct makes this red instead of shipping a silent arm.</summary>
    [Fact]
    public void Sr6StatesThreeObligations_AndTheTableHasARowForEach()
    {
        string printed = SpecClauseText.Norm(
            SpecClauseText.NumberedRules(SpecClauseText.ClauseRegion(SpecClauseText.Lines(), Clause))[6]);
        string[] arms = ["data-name-1 shall describe an elementary", "elementary unsigned integer",
                         "in the working-storage, local-storage, or linkage section"];
        var rows = RecordClauseRules.Catalog.Where(r => r.RuleId == $"SR-{Clause}-6").ToList();
        Assert.Equal(arms.Length, rows.Count);
        foreach (string arm in arms)
        {
            Assert.Contains(SpecClauseText.Norm(arm), printed);
            Assert.Contains(rows, r => SpecClauseText.Norm(r.RuleText) == SpecClauseText.Norm(arm));
        }
        Assert.All(rows, r => Assert.Equal(RecordRuleSubject.DependingOperand, r.Subject));
    }

    /// <summary>⛔ THE OPERAND NAMES ARE THE FORMAT'S OWN. §13.18.43.2 spells Format 1 with integer-1, Format 2
    /// with integer-2/integer-3 and Format 3 with integer-4/integer-5, and a message that named the wrong pair
    /// would send the reader to the wrong general format. Re-derived from the printed rules: SR3 names
    /// integer-1, SR5 names integer-2 and integer-3, SR9 names integer-4 and integer-5.</summary>
    [Fact]
    public void TheOperandNames_MatchTheGeneralFormatTheyBelongTo()
    {
        var rules = SpecClauseText.NumberedRules(SpecClauseText.ClauseRegion(SpecClauseText.Lines(), Clause));
        // ⛔ THROUGH THE NORMALIZER, because the standard capitalizes an operand that OPENS a sentence — SR5 is
        // "Integer-3 shall be greater than integer-2" and SR9 "Integer-5 shall be greater than integer-4", so a
        // case-sensitive search for "integer-3" finds the operand in one position and misses it in the other.
        // Norm() is the same case-folding cite.py uses, which is what makes "contains" mean the same thing here
        // as it does in the citation gate.
        foreach ((int ordinal, string operand) in new[]
                 { (3, "integer-1"), (5, "integer-2"), (5, "integer-3"), (9, "integer-4"), (9, "integer-5") })
            Assert.Contains(SpecClauseText.Norm(operand), SpecClauseText.Norm(rules[ordinal]));

        Assert.Equal("integer-1", new RecordClauseFacts(RecordClauseFormat.Fixed, null, 1, default).UpperName);
        var varying = new RecordClauseFacts(RecordClauseFormat.Varying, 1, 2, default);
        Assert.Equal(("integer-2", "integer-3"), (varying.LowerName, varying.UpperName));
        var fov = new RecordClauseFacts(RecordClauseFormat.FixedOrVariable, 1, 2, default);
        Assert.Equal(("integer-4", "integer-5"), (fov.LowerName, fov.UpperName));
    }

    /// <summary>⛔ THE FORMAT PROJECTION IS ONE-TO-ONE WITH THE MODEL, and this is the arm a "does it answer the
    /// right thing" assertion passes without: every way the binder can record a RECORD clause maps to exactly one
    /// §13.18.43.2 format, and an entry with NO clause maps to none — which is what keeps the screen silent where
    /// §13.18.43.4 GR5 implies a clause from the record descriptions.</summary>
    [Fact]
    public void TheClauseProjection_MapsEachBoundShapeToItsOwnFormatAndNoClauseToNone()
    {
        var none = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F" };
        Assert.Null(none.RecordClause);

        var fixedLen = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F", RecordContains = 80 };
        Assert.Equal(RecordClauseFormat.Fixed, fixedLen.RecordClause!.Value.Format);
        Assert.Equal(80, fixedLen.RecordClause!.Value.Upper);
        Assert.Null(fixedLen.RecordClause!.Value.Lower);

        var varying = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F",
            Varying = new VaryingRecordInfo(0, 20, "L", VaryingClause: true) };
        Assert.Equal(RecordClauseFormat.Varying, varying.RecordClause!.Value.Format);
        Assert.Equal((0, 20), (varying.RecordClause!.Value.Lower, varying.RecordClause!.Value.Upper));

        var fov = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F",
            Varying = new VaryingRecordInfo(4, 12, null, VaryingClause: false) };
        Assert.Equal(RecordClauseFormat.FixedOrVariable, fov.RecordClause!.Value.Format);
    }

    /// <summary>⭐ THE "NEXT RULE IS A ROW" CLAMP, in both directions: a rule the traceability inventory credits
    /// to this file must have a row, and a row's rule-id must be a real inventory id that the inventory does not
    /// credit somewhere else. A row claiming an id filed against a different site is a claim on the burn-down
    /// that the code does not back.</summary>
    [Fact]
    public void EveryScreenedRuleInTheInventory_IsARowInTheTable()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.At("tests", "version-matrix", "traceability-inventory.json")));
        var claimed = new SortedSet<string>(StringComparer.Ordinal);
        var locatedElsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        var allIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            string id = row.GetProperty("rule-id").GetString()!;
            allIds.Add(id);
            string where = row.TryGetProperty("code-location", out var cl) ? cl.GetString() ?? "" : "";
            if (where.Contains("RecordClauseRules", StringComparison.Ordinal)) claimed.Add(id);
            else if (where.Length > 0) locatedElsewhere[id] = where;
        }
        var inTable = new SortedSet<string>(
            RecordClauseRules.Catalog.Select(r => r.RuleId), StringComparer.Ordinal);

        Assert.True(claimed.Count > 0,
            "no inventory row points at RecordClauseRules — either the batch was never applied or the screen moved.");
        Assert.Empty(claimed.Except(inTable));
        foreach (string id in inTable)
        {
            Assert.True(allIds.Contains(id), $"row '{id}' is not a rule-id the traceability inventory knows.");
            Assert.False(locatedElsewhere.TryGetValue(id, out string? where),
                $"row '{id}' is screened here, but the inventory credits it to '{where}'.");
        }
    }
}
