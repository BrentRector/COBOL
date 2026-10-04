// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SOURCE AND SUM CLAUSES SHARE ONE OPERAND PRODUCTION AND ONE ROUNDED PHRASE, AND THIS KEEPS THEM
/// SHARING (kb/Work PB852 × PB883).
///
/// <para><b>The bug this guard is shaped from.</b> ISO §13.18.53.2 (SOURCE) and §13.18.54.2 (SUM) print the
/// SAME operand brace — <c>{ identifier-1 | arithmetic-expression-1 }</c>, SUM adding <i>data-name-1</i>, which
/// is an identifier too — and both close with <c>[ rounded-phrase ]</c>. The grammar had neither form in either
/// clause: <c>reportSourceClause</c> took <c>dataReference+</c> and <c>sumOperand</c> was
/// <c>dataReference (OF reportName)?</c>, so <c>SOURCE IS (A + B)</c>, <c>SOURCE IS A ROUNDED</c> and
/// <c>SUM A + 1</c> were all raw parse errors on conforming source, and §13.18.53.3 SR3/SR5/SR7, §13.18.54.3
/// SR3/SR6 and §13.18.53.4 GR2's implicit COMPUTE had no reachable population at all.</para>
///
/// <para><b>The invariant.</b> ONE production — <c>reportValueOperand</c> — is referenced by BOTH clauses, and
/// the ROUNDED phrase each carries is the SHARED <c>roundedPhrase</c> of §14.7.4, never a report-local copy.
/// The sharing is what makes the 2014 <c>MODE IS</c> introduction gate reach a report clause at all (the pass
/// fires on RECOGNITION of <c>roundedPhrase</c>, wherever it appears), and it is what stops the next operand
/// form from being added to one clause and forgotten in the other — this repo's most reproducible defect shape.
/// The FORM decision likewise lives in ONE classifier, <c>DataBinder.Reports.BareReferenceOf</c>.</para>
///
/// <para>⭐ IT READS THE GRAMMAR, NOT A REMEMBERED SHAPE: the assertions are regexes over
/// <c>CobolReportWriter.g4</c>, so a future edit that gives either clause its own operand rule fails here
/// rather than at a golden three waves later.</para>
/// </summary>
public sealed class ReportValueOperandDriftTests
{
    private static string Grammar =>
        File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Frontend"), "Grammar", "Core", "CobolReportWriter.g4"));

    private static string RuleBody(string grammar, string rule)
    {
        var m = Regex.Match(grammar, $@"^{Regex.Escape(rule)}\s*\n\s*:(?<body>.*?)^\s*;\s*$",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, $"the parser rule '{rule}' is gone from CobolReportWriter.g4 — "
            + "ISO §13.18.53.2 / §13.18.54.2 both need it");
        return m.Groups["body"].Value;
    }

    /// <summary>ISO §13.18.53.2 / §13.18.54.2 — both general formats print the same operand brace, so both
    /// clauses reference the ONE production. A second operand rule is the defect this pins.</summary>
    [Fact]
    public void BothValueClauses_ReferenceTheOneOperandProduction()
    {
        string g = Grammar;
        Assert.Contains("reportValueOperand", RuleBody(g, "reportSourceClause"));
        // §13.18.54.2's repeated `SUM OF … [UPON …]` group is its own rule (kb/Work PB1295): the RESET phrase and the
        // rounded-phrase follow the repetition, so the clause rule holds the groups and the group rule holds the operand.
        Assert.Contains("reportSumGroup", RuleBody(g, "reportSumClause"));
        Assert.Contains("reportValueOperand", RuleBody(g, "reportSumGroup"));
        // The operand IS an arithmetic expression: §8.4.3.1.2 Format 2 makes an identifier a primary of one, so
        // the identifier form is its degenerate case and the binder classifies which was written.
        Assert.Contains("arithmeticExpression", RuleBody(g, "reportValueOperand"));
        // Exactly one operand production: `sumOperand` was the second copy and is deleted.
        Assert.DoesNotContain("\nsumOperand", g);
    }

    /// <summary>ISO §13.18.53.2 / §13.18.54.2 — "where rounded-phrase is described in 14.7.4, ROUNDED phrase".
    /// Each clause carries the SHARED production, which is how the §14.7.4 <c>MODE IS</c> 2014 introduction gate
    /// reaches a report clause with no rule written down twice.</summary>
    [Fact]
    public void BothValueClauses_CarryTheSharedRoundedPhrase()
    {
        string g = Grammar;
        Assert.Contains("roundedPhrase", RuleBody(g, "reportSourceClause"));
        Assert.Contains("roundedPhrase", RuleBody(g, "reportSumClause"));
        // No report-local rounding rule may exist: the phrase is §14.7.4's, defined in CobolParserCore.g4.
        Assert.DoesNotContain("reportRoundedPhrase", g);
        Assert.False(Regex.IsMatch(g, @"^\s*roundedPhrase\s*\n\s*:", RegexOptions.Multiline),
            "CobolReportWriter.g4 redefines roundedPhrase — §13.18.53.2 and §13.18.54.2 both say the phrase "
            + "IS §14.7.4's, and a second definition would not carry its 2014 MODE IS introduction gate");
    }

    /// <summary>ISO §13.18.53.3 SR5 / §13.18.54.3 SR1 — which addend/operand FORM was written is one question,
    /// asked in one place. Two copies of the walk down <c>arithmeticExpression</c> would be the two-arm dispatch
    /// this repo keeps rediscovering.</summary>
    [Fact]
    public void TheOperandFormIsClassifiedInExactlyOnePlace()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        var callers = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("BareReferenceOf", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(new[] { "DataBinder.Reports.cs" }, callers);
    }
}
