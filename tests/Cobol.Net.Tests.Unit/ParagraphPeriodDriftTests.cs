// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY OPTIONAL SEPARATOR PERIOD THE GRAMMAR WRITES IS READ BY THE ONE PARAGRAPH-PERIOD SCREEN (kb/Work PB2727).
///
/// <para><b>Why it exists.</b> ISO §12.4.6.2, §12.3.7.2 and §12.3.8.2 print ONE separator period after the
/// I-O-CONTROL, SPECIAL-NAMES and REPOSITORY paragraphs' entries, and the three grammar rules wrote
/// <c>(entry DOT?)*</c> with nothing reading the periods — so a period after every entry, or none after the last,
/// compiled clean at every edition. The grammar keeps the punctuation superset so <c>ClosedFormatPass</c> can refuse
/// the misplaced period BY NAME (COBOLNET3407 / COBOLNET3408) through <see cref="ParagraphPeriods"/>. These facts
/// hold the two together in BOTH directions: every grammar rule that writes <c>DOT?</c> has a row, and every row
/// names a rule the grammar still writes that way, with the periods as the rule's OWN children (which is what
/// <see cref="ParagraphPeriods.Sentences"/> reads). A fourth rule written with the shape cannot parse into silence.</para>
/// </summary>
public sealed class ParagraphPeriodDriftTests : CobolNetTestBase
{
    private static string[] GrammarFiles() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}"))
                 .ToArray();

    /// <summary>Every parser rule whose body (comments stripped) writes an optional separator period, by rule name.</summary>
    private static List<string> RulesWritingAnOptionalPeriod()
    {
        var rules = new List<string>();
        foreach (string path in GrammarFiles())
        {
            string code = Regex.Replace(File.ReadAllText(path), @"//[^\n]*", "");
            foreach (Match m in Regex.Matches(code, @"(?ms)^([a-z][A-Za-z0-9_]*)\s*\r?\n\s*:(.*?)^\s*;"))
                if (Regex.IsMatch(m.Groups[2].Value, @"\bDOT\s*\?")) rules.Add(m.Groups[1].Value);
        }
        return rules;
    }

    private static string ContextName(string rule) => char.ToUpperInvariant(rule[0]) + rule[1..] + "Context";

    [Fact]
    public void TheGrammarProbe_FindsTheParagraphRules()
    {
        // A probe that finds nothing would make the obligation below vacuously green.
        var rules = RulesWritingAnOptionalPeriod();
        Assert.Contains("ioControlParagraph", rules);
        Assert.Contains("specialNamesParagraph", rules);
        Assert.Contains("repositoryParagraph", rules);
    }

    [Fact]
    public void EveryRuleWritingAnOptionalPeriod_HasAParagraphPeriodRow()
    {
        var rows = ParagraphPeriods.ByContext.Keys.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var missing = RulesWritingAnOptionalPeriod().Where(r => !rows.Contains(ContextName(r))).ToList();
        Assert.True(missing.Count == 0,
            "a grammar rule writes an optional separator period (`DOT?`) that no ParagraphPeriods row reads, so a "
            + "period the general format does not print — or a missing one — would compile in silence (kb/Work "
            + "PB2727): " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryRow_NamesARuleThatStillWritesThePeriodAsItsOwnChild()
    {
        var rules = RulesWritingAnOptionalPeriod().Select(ContextName).ToHashSet(StringComparer.Ordinal);
        foreach (var (type, row) in ParagraphPeriods.ByContext)
        {
            Assert.True(rules.Contains(type.Name), $"{type.Name} has a ParagraphPeriods row but its rule no longer "
                + "writes `DOT?` — a dead row (the screen would read nothing)");
            // The periods must be the paragraph's OWN children: ParagraphPeriods.Sentences reads only those.
            var dot = type.GetMethod("DOT", Type.EmptyTypes);
            Assert.True(dot is not null && dot.ReturnType == typeof(ITerminalNode[]),
                $"{type.Name}: the paragraph rule must spell its separator periods itself (a DOT() accessor returning "
                + "every period), not inside an entry rule");
            Assert.True(row.Clause.Length > 0 && row.Subject.Length > 0 && row.Printed.Length > 0, $"{type.Name}: row incomplete");
            Assert.True(typeof(CobolParserCore).GetNestedTypes().Contains(type), $"{type.Name}: not a generated context");
        }
    }

    [Fact]
    public void TheTwoDiagnostics_AreRegisteredErrors()
    {
        foreach (var d in new[] { DiagnosticCatalog.ParagraphInteriorPeriod, DiagnosticCatalog.ParagraphPeriodMissing })
        {
            Assert.Equal(EditionSeverity.Error, d.Severity);
            Assert.Single(DiagnosticCatalog.All, x => x.Code == d.Code);
        }
    }
}
