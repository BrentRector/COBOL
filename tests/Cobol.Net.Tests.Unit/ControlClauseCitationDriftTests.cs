// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INHERITED-CITATION GUARD FOR THE CONTROL CLAUSE (kb/Work PB177 arm C).
/// <para>CLAUDE.md rule 1 names the failure mode precisely: "it is not inventing a citation, it is INHERITING
/// one" — a § propagates into code comments and shipping message text and the clause NUMBER is never
/// re-derived. TWO live diagnostics carried exactly that defect. <c>DataBinder.Reports</c> cited
/// "ISO §13.18.16.3 SR3" for an UNRESOLVABLE CONTROL operand and <c>ReportWriterEmitter</c> cited the SAME SR3
/// for a float/INDEX one — but SR3 is "Data-name-1 shall not be subject to any OCCURS clauses", a real clause
/// answering a different question. <c>cite.py --check</c> on the clause NUMBER alone passes either way, which
/// is exactly why a number-only check is not enough and this test reads the RULE TEXT.</para>
/// <para>⭐ IT CARRIES NO LIST OF ITS OWN. The expected rule text is re-derived from
/// <c>specs/ISO_COBOL.md</c> on every run, so a transcription repair flows through instead of going stale.</para>
/// </summary>
public sealed class ControlClauseCitationDriftTests
{
    /// <summary>The lettered syntax rules of §13.18.16.3, keyed by their printed number, read out of the spec.</summary>
    private static Dictionary<int, string> ControlSyntaxRules()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.18\.16\.3\b"));
        Assert.True(start >= 0, "§13.18.16.3 is missing from specs/ISO_COBOL.md — this guard must follow the clause.");
        int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l, @"^#{2,6}\s+13\.18\.16\.4\b"));
        Assert.True(end > start, "§13.18.16.4 not found after §13.18.16.3 — the heading shape changed.");

        var rules = new Dictionary<int, string>();
        foreach (string l in lines[start..end])
            if (Regex.Match(l, @"^(\d+)\\?\)\s+(.*)$") is { Success: true } m)
                rules[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();
        Assert.True(rules.Count >= 7, $"only {rules.Count} syntax rules parsed from §13.18.16.3 — fix the scanner, do not lower the floor.");
        return rules;
    }

    /// <summary>Every §13.18.16.3 rule number a shipping message quotes must be the rule that actually says
    /// what the message is about. The screen enforces SR3, SR5 and SR7; each of those three sentences must be
    /// findable in the code that cites it, and no site may cite SR3 for anything but the OCCURS rule.</summary>
    [Fact]
    public void ControlOperandDiagnostics_CiteTheRuleTheyActuallyEnforce()
    {
        var rules = ControlSyntaxRules();
        // The three the shape screen enforces, each identified by a distinctive phrase of its OWN sentence.
        Assert.Contains("subject to any OCCURS", rules[3]);
        Assert.Contains("occurs-depending table subordinate to it", rules[5]);
        Assert.Contains("variable-length group", rules[7]);

        string binder = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "Binding", "DataBinder.Reports.cs"));
        string emitter = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "ReportWriterEmitter.cs"));

        // Each enforced rule's message paraphrases its own sentence AND names its own number.
        // Phrases chosen to survive the source's own line wrapping — each is a contiguous span of one message.
        foreach ((int n, string phrase) in new[] { (3, "subject to any OCCURS clauses"), (5, "occurs-depending table subordinate to it"), (7, "data-name-1 shall not reference a ") })
        {
            Assert.Contains(phrase, binder);
            Assert.Contains($"§13.18.16.3 SR{n}", binder);
        }

        // ⛔ THE REGRESSION ITSELF: neither repaired site may cite SR3 again. The unresolvable-operand case is
        // ordinary name resolution (§8.4.2.1 — no §13.18.16.3 rule governs resolution failure); what remains at
        // the EMITTER is an implementation limit under §13.18.16.4 GR3.
        Assert.Contains("ISO §8.4.2.1", binder);
        Assert.DoesNotContain("§13.18.16.3 SR3)", emitter);
        Assert.Contains("§13.18.16.4 GR3", emitter);

        // ⛔ AND THE SECOND HALF OF THE SAME REPAIR, WHICH THE FIRST CUT GOT WRONG IN THE OPPOSITE DIRECTION.
        // The repaired emitter comment asserted "A float / INDEX operand violates no CONTROL syntax rule at
        // all". That is true of FLOAT and FALSE of INDEX: §13.18.60.3 SR10 closes the set of contexts in which
        // an index data item may be referenced explicitly, and §8.4.5 makes a data-division clause naming a data
        // item exactly such an explicit reference — so the INDEX operand is illegal source that was reaching a
        // RUNTIME loud. It is now COBOLNET1700 at bind, and the false sentence is gone. Re-derived from the
        // spec, like everything else here, rather than quoted from memory.
        string[] specLines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        string sr10 = Array.Find(specLines, l => l.Contains("An index data item may be referenced explicitly only"))
            ?? throw new Xunit.Sdk.XunitException("§13.18.60.3 SR10's sentence is missing from specs/ISO_COBOL.md.");
        Assert.Contains("SEARCH or SET statement", sr10);
        Assert.DoesNotContain("violates no CONTROL syntax rule at all", emitter);
        Assert.Contains("§13.18.60.3 SR10", binder);
        Assert.Contains("§8.4.5", binder);
        // The FLOAT limb is no longer a loud (kb/Work PB1234): no §13.18.16.3 rule excludes a floating-point
        // data-name-1, so the emitter gives it the prior-control channel GR3/GR4 require — the item's bit pattern
        // on its own carrier. The old loud message must not come back, and the float arm must stay present.
        Assert.DoesNotContain("no prior-control RESTORE channel", emitter);
        Assert.Contains("ReportFloatControlKey", emitter);
    }
}
