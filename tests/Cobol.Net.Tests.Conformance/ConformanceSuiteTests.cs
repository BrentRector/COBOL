// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ WISEOWL COBOL RUNS THE CONFORMANCE SUITE ON ITSELF, IN-PROCESS (DESIGN-conformance-suite.md §5 "In WiseOwl's own
/// pipeline", §13 S1c; kb/Work PB3176): every test of the generator's <c>wiseowl</c> manifest (both trees, portable or
/// not) at every edition it names, through <see cref="ConformanceSuiteRunner"/>, and every refusal of
/// <c>conformance-suite/wiseowl/diagnostics.json</c> at every edition it names.
/// </summary>
/// <remarks>
/// <para><b>A row is red exactly where <c>run.py</c> is red with no baseline</b> (§5): on FAIL or NO VERDICT. NOT
/// APPLICABLE is the profile speaking (a module WiseOwl does not claim; a flagged test whose control does not
/// compile, which the control's own row reports as a FAIL), never a pass over a test that ran.</para>
/// <para><b>A refusal</b> is WiseOwl's own stricter check, outside the compiler-neutral cases (§3.2): at each edition
/// its entry names, the ordinary compile (the warning mechanism not invoked) must refuse the source (the front end or
/// the binder, never the backend, whose refusal is the compiler's own failure) and report the entry's code or message
/// substring.</para>
/// </remarks>
public sealed class ConformanceSuiteTests
{
    public static IEnumerable<object[]> Tests() => ConformanceSuite.Tests();

    public static IEnumerable<object[]> Refusals() => ConformanceSuite.Refusals();

    [Theory]
    [MemberData(nameof(Tests))]
    public void SuiteTest_IsNoWorseThanItsVerdictAllows(string edition, string id)
    {
        SuiteTest test = ConformanceSuite.Manifest.Editions[edition][id];
        SuiteResult result = ConformanceSuiteRunner.Judge(edition, test);
        Assert.True(result.Verdict is ConformanceSuiteRunner.Pass or ConformanceSuiteRunner.NotApplicable,
            $"[{edition}/{test.Clause}/{id}] {result.Verdict}: {result.Reason}");
    }

    [Theory]
    [MemberData(nameof(Refusals))]
    public void WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic(string id, string edition)
    {
        SuiteRefusal refusal = ConformanceSuite.Manifest.Refusals[id];
        CompilerDriver.Result result = ConformanceSuiteRunner.CompileRefusal(refusal, edition);
        Assert.True(result.Status is CompilerDriver.Outcome.FrontendError or CompilerDriver.Outcome.BindError,
            $"[{edition}/{id}] must be REFUSED by the front end or the binder at --std {edition}; the compile ended "
            + $"{result.Status}: {string.Join("\n", result.Errors)}");
        EditionHarness.AssertHasDiagnostic(result.Errors, refusal.Expect);
    }
}
