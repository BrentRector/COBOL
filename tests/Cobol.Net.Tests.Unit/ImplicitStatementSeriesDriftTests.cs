// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PER-IMPLICIT-STATEMENT RESUMPTION BOUNDARY STAYS AUTOMATIC (kb/Work PB419, CLAUDE.md rule 5).
/// <para>The standard states ONE rule SEVEN times, once per multi-operand verb: "the result of executing this …
/// statement is the same as if a separate … statement had been written for each … in the same order", followed by
/// "If an implicit … statement results in the execution of a declarative procedure that executes a RESUME
/// statement with the NEXT STATEMENT phrase, processing resumes at the next implicit … statement, if any."
/// A verb that binds its operand list into ONE statement site has no boundary for that resumption point, and the
/// failure is SILENT: the declarative runs, control leaves the whole verb, and every remaining operand is skipped
/// with no diagnostic anywhere (measured on INITIALIZE — <c>INITIALIZE A B</c> left B at its old value).</para>
/// <para>So the census is taken FROM THE SPEC, not from a hand-kept list: whichever verbs the standard gives this
/// rule to must bind through <c>BoundImplicitSeries</c>. An eighth verb — or an implementation of the one verb
/// currently exempt because its facility is owner-declined and it binds to nothing — fails this test rather than
/// shipping the defect again.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ImplicitStatementSeriesDriftTests
{
    private static readonly string VerbsDir =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs");

    /// <summary>The sentence that IS this rule, with the verb and the owning general rule left open. The verb
    /// word is what the census keys on; the clause number comes from the enclosing heading.</summary>
    private static readonly Regex ResumptionRule = new(
        @"processing resumes at the next implicit ([A-Z][A-Z]+(?: [A-Z]+)?) statement",
        RegexOptions.Compiled);

    private static readonly Regex ClauseHeading = new(@"^#{3,6}\s+(\d+(?:\.\d+)+)\s", RegexOptions.Compiled);

    /// <summary>⛔ THE ONE EXEMPTION, AND IT IS SELF-INVALIDATING. VALIDATE (ISO §14.9.50) is the OWNER-DECLINED
    /// Annex A.4.14 facility (docs/CONFORMANCE.md §4 item 3): the grammar recognizes it only to NAME the refusal
    /// (<c>validateFacilityStatement</c>), it binds to no node, and a statement that never binds has no statement
    /// site to give its operands. <see cref="EveryVerbTheSpecGivesTheResumptionRule_BindsThroughTheImplicitSeries"/>
    /// asserts THAT premise rather than assuming it, so the day the facility is implemented and a
    /// <c>BoundValidate</c> exists, this exemption stops being true and the census fails until the verb joins.</summary>
    private const string DeclinedVerb = "VALIDATE";

    /// <summary>Every (verb, clause) the spec gives the per-implicit-statement resumption rule to.</summary>
    private static Dictionary<string, string> SpecFamily()
    {
        var family = new Dictionary<string, string>(StringComparer.Ordinal);
        string clause = "";
        foreach (string line in File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md")))
        {
            if (ClauseHeading.Match(line) is { Success: true } h)
            {
                clause = h.Groups[1].Value;
                continue;
            }
            foreach (Match m in ResumptionRule.Matches(line))
            {
                family[m.Groups[1].Value] = clause;
            }
        }

        return family;
    }

    /// <summary>Source text with comments removed — a doc comment naming a symbol documents it, it does not call
    /// it, and the subjects here are named in several.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    /// <summary>The compiler must route EVERY verb the standard gives the resumption rule to through
    /// <c>BoundImplicitSeries</c>, and the binder that does it must cite the verb's own general rule — one verb
    /// silently left on the flat shape is one verb whose remaining operands a RESUME … NEXT STATEMENT skips.</summary>
    [Fact]
    public void EveryVerbTheSpecGivesTheResumptionRule_BindsThroughTheImplicitSeries()
    {
        var family = SpecFamily();

        Assert.True(family.Count >= 7,
            "specs/ISO_COBOL.md no longer yields the per-implicit-statement resumption rule for the known verbs "
            + $"(found {family.Count}: {string.Join(", ", family.Keys.OrderBy(k => k, StringComparer.Ordinal))}). "
            + "Either the transcription changed or the regex stopped matching — re-derive it before trusting a "
            + "green run; a census that matches nothing passes vacuously.");

        Assert.True(family.ContainsKey(DeclinedVerb),
            $"{DeclinedVerb} no longer carries the resumption rule in the spec — re-derive the exemption.");

        // The premise of the one exemption, asserted rather than assumed (memory: validate_the_premise_not_only
        // _the_rule). The day VALIDATE binds to a node, this fires and the census below must cover it.
        string boundNodes = string.Concat(Directory.EnumerateFiles(
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.DoesNotContain("record BoundValidate", boundNodes, StringComparison.Ordinal);

        var seriesBinders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .Select(p => (Path: p, Text: File.ReadAllText(p)))
            .Where(f => CodeOf(f.Path).Contains("BoundImplicitSeries.Of(", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(seriesBinders);

        foreach (var (verb, clause) in family.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (verb == DeclinedVerb)
            {
                continue;
            }

            Assert.True(
                seriesBinders.Any(f => f.Text.Contains(clause, StringComparison.Ordinal)),
                $"No verb binder that calls BoundImplicitSeries.Of cites §{clause} — the clause that makes a "
                + $"multi-operand {verb} a SERIES of implicit {verb} statements whose RESUME … NEXT STATEMENT "
                + $"resumes at the next one (ISO §{clause}). Either {verb} still binds its whole operand list "
                + "into one statement site — the PB419 defect, silent and a wrong answer — or the binder stopped "
                + "citing the rule it implements. Binders that do route through the series: "
                + string.Join(", ", seriesBinders.Select(f => Path.GetFileName(f.Path))));
        }
    }

    /// <summary>The boundary only exists because the CHECKED WRAPPER distributes over the series' members: the
    /// wrapper IS the statement site a declarative's RESUME … NEXT STATEMENT (the <c>-2</c> dispatch action) falls
    /// out of, so one wrapper around the whole series puts the landing past the LAST operand — exactly the defect,
    /// restored, with every verb still binding to a series.</summary>
    [Fact]
    public void EcWrap_DistributesTheCheckedWrapperOverTheSeriesMembers()
    {
        string code = CodeOf(Path.Combine(VerbsDir, "EcBinder.cs"));

        Assert.Matches(
            new Regex(@"bound is BoundImplicitSeries\s+\w+[\s\S]{0,400}?new BoundEcChecked\("),
            code);

        // The enabled set of a series is the UNION over its members (one written statement, one >>TURN scope): the ONE
        // enumeration, EcBinder.DeclaredRaisableNames (kb/Work PB3102), reads every member.
        string declarations = CodeOf(Path.Combine(VerbsDir, "EcBinder.RaiseDeclarations.cs"));
        Assert.Matches(new Regex(@"series\.Members\.SelectMany\(\w+ => DeclaredRaisableNames\(\w+, scope\)\)"), declarations);
    }

    /// <summary>A bind-time desugar (the user-function activation hoist, the OO property pre-op triple) wraps the
    /// statement in a <c>BoundSequence</c>. Applied to a series that would bury it, and <c>EcWrap</c> — which only
    /// distributes over a series it can SEE — would go back to enclosing it: the PB419 defect would return for
    /// exactly the statements carrying a function-identifier or a property reference, and nothing would say so.
    /// The desugars therefore go through <c>BoundImplicitSeries.Rewrap</c>, which keeps the series outermost.</summary>
    [Fact]
    public void TheDesugarWraps_GoThroughRewrapSoTheSeriesStaysOutermost()
    {
        string code = CodeOf(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound", "StatementBinder.cs"));

        foreach (string desugar in new[] { "UdfWrapCalls", "OoWrapPropertyOps" })
        {
            var calls = Regex.Matches(code, @"[\w.]*\b" + desugar + @"\(");
            Assert.True(calls.Count > 0, $"StatementBinder no longer applies {desugar} — re-derive this test.");
            Assert.All(calls.Cast<Match>(), m =>
                Assert.True(
                    code.LastIndexOf("BoundImplicitSeries.Rewrap(", m.Index, StringComparison.Ordinal) is var r
                        && r >= 0 && m.Index - r < 200,
                    $"{desugar} is applied outside BoundImplicitSeries.Rewrap, so a desugared multi-operand "
                    + "CLOSE/FREE/INITIALIZE/INITIATE/OPEN/TERMINATE would be wrapped in a BoundSequence that "
                    + "hides the series from EcWrap — restoring kb/Work PB419 silently for those statements."));
        }
    }
}
