// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ WISEOWL COBOL RUNS THE CONFORMANCE SUITE ON ITSELF, IN-PROCESS (DESIGN-conformance-suite.md §5 "In WiseOwl's own
/// pipeline", §13 S1c, S2; kb/Work PB3176, PB3177): every test of the generator's <c>wiseowl</c> manifest (both trees,
/// portable or not) at every edition it names, through <see cref="ConformanceSuiteRunner"/>, and every refusal of
/// <c>conformance-suite/wiseowl/diagnostics.json</c> at every edition it names. Since S2 that is the whole corpus the
/// former <c>tests/conformance/</c> held: its positive programs are accepted tests, its negative programs refusals.
/// </summary>
/// <remarks>
/// <para><b>A row is red exactly where <c>run.py</c> is red with no baseline</b> (§5): on FAIL or NO VERDICT. NOT
/// APPLICABLE is the profile speaking (a module WiseOwl does not claim; a flagged test whose control does not
/// compile, which the control's own row reports as a FAIL), never a pass over a test that ran.</para>
/// <para><b>A refusal</b> is WiseOwl's own stricter check, outside the compiler-neutral cases (§3.2): at each edition
/// its entry names, the ordinary compile (the warning mechanism not invoked) must refuse the source (the front end or
/// the binder, never the backend, whose refusal is the compiler's own failure) and report the entry's code or message
/// substring.</para>
/// <para>⛔ <b>THIS FAMILY IS PARTITIONED</b> (<see cref="TestPartitioning"/>; kb/Work PB2527): xUnit 2 runs one test
/// class as one collection, serially, and the corpus is about 10,600 rows, so one class would be the Conformance leg's
/// wall. <see cref="Partitions"/> classes of about 440 rows each run in parallel; <c>ConformancePartitioningDriftTests</c>
/// pins the count against the measured bound, and <see cref="ArchOracle"/> enrolls both row sources.</para>
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class ConformanceSuiteTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count: the corpus runner's measured 24 (kb/Work PB2527: about 190 cases, about 35 s each, at
    /// the former 4,659-row corpus; the suite's rows are cheaper per row, a refusal compiling once per edition).</summary>
    public const int Partitions = 24;

    [PartitionedRowSource(nameof(Tests))]
    public static IEnumerable<object[]> AllTests() => ConformanceSuite.Tests();

    /// <summary>This partition's share of the suite's tests.</summary>
    public static IEnumerable<object[]> Tests() => TestPartitioning.SliceRows<TSlot>(AllTests(), Partitions);

    [PartitionedRowSource(nameof(Refusals))]
    public static IEnumerable<object[]> AllRefusals() => ConformanceSuite.Refusals();

    /// <summary>This partition's share of the refusals.</summary>
    public static IEnumerable<object[]> Refusals() => TestPartitioning.SliceRows<TSlot>(AllRefusals(), Partitions);

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

// ⛔ THE 24 PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover both row sources exactly
// once; ConformanceSuitePartitioningDriftTests pins the count (kb/Work PB2527).

/// <summary>Conformance-suite partition 0 of <see cref="ConformanceSuiteTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class ConformanceSuiteTests_P0 : ConformanceSuiteTestsBase<Slot0>;

/// <summary>Conformance-suite partition 1.</summary>
public sealed class ConformanceSuiteTests_P1 : ConformanceSuiteTestsBase<Slot1>;

/// <summary>Conformance-suite partition 2.</summary>
public sealed class ConformanceSuiteTests_P2 : ConformanceSuiteTestsBase<Slot2>;

/// <summary>Conformance-suite partition 3.</summary>
public sealed class ConformanceSuiteTests_P3 : ConformanceSuiteTestsBase<Slot3>;

/// <summary>Conformance-suite partition 4.</summary>
public sealed class ConformanceSuiteTests_P4 : ConformanceSuiteTestsBase<Slot4>;

/// <summary>Conformance-suite partition 5.</summary>
public sealed class ConformanceSuiteTests_P5 : ConformanceSuiteTestsBase<Slot5>;

/// <summary>Conformance-suite partition 6.</summary>
public sealed class ConformanceSuiteTests_P6 : ConformanceSuiteTestsBase<Slot6>;

/// <summary>Conformance-suite partition 7.</summary>
public sealed class ConformanceSuiteTests_P7 : ConformanceSuiteTestsBase<Slot7>;

/// <summary>Conformance-suite partition 8.</summary>
public sealed class ConformanceSuiteTests_P8 : ConformanceSuiteTestsBase<Slot8>;

/// <summary>Conformance-suite partition 9.</summary>
public sealed class ConformanceSuiteTests_P9 : ConformanceSuiteTestsBase<Slot9>;

/// <summary>Conformance-suite partition 10.</summary>
public sealed class ConformanceSuiteTests_P10 : ConformanceSuiteTestsBase<Slot10>;

/// <summary>Conformance-suite partition 11.</summary>
public sealed class ConformanceSuiteTests_P11 : ConformanceSuiteTestsBase<Slot11>;

/// <summary>Conformance-suite partition 12.</summary>
public sealed class ConformanceSuiteTests_P12 : ConformanceSuiteTestsBase<Slot12>;

/// <summary>Conformance-suite partition 13.</summary>
public sealed class ConformanceSuiteTests_P13 : ConformanceSuiteTestsBase<Slot13>;

/// <summary>Conformance-suite partition 14.</summary>
public sealed class ConformanceSuiteTests_P14 : ConformanceSuiteTestsBase<Slot14>;

/// <summary>Conformance-suite partition 15.</summary>
public sealed class ConformanceSuiteTests_P15 : ConformanceSuiteTestsBase<Slot15>;

/// <summary>Conformance-suite partition 16.</summary>
public sealed class ConformanceSuiteTests_P16 : ConformanceSuiteTestsBase<Slot16>;

/// <summary>Conformance-suite partition 17.</summary>
public sealed class ConformanceSuiteTests_P17 : ConformanceSuiteTestsBase<Slot17>;

/// <summary>Conformance-suite partition 18.</summary>
public sealed class ConformanceSuiteTests_P18 : ConformanceSuiteTestsBase<Slot18>;

/// <summary>Conformance-suite partition 19.</summary>
public sealed class ConformanceSuiteTests_P19 : ConformanceSuiteTestsBase<Slot19>;

/// <summary>Conformance-suite partition 20.</summary>
public sealed class ConformanceSuiteTests_P20 : ConformanceSuiteTestsBase<Slot20>;

/// <summary>Conformance-suite partition 21.</summary>
public sealed class ConformanceSuiteTests_P21 : ConformanceSuiteTestsBase<Slot21>;

/// <summary>Conformance-suite partition 22.</summary>
public sealed class ConformanceSuiteTests_P22 : ConformanceSuiteTestsBase<Slot22>;

/// <summary>Conformance-suite partition 23.</summary>
public sealed class ConformanceSuiteTests_P23 : ConformanceSuiteTestsBase<Slot23>;

/// <summary>The suite's whole-manifest facts, run once (an inherited fact would run once per partition).</summary>
public sealed class ConformanceSuiteTests
{
    /// <summary>A WiseOwl compile option (<c>wiseowl/options.json</c>, kb/Work PB803, PB1525) reaches the compile: the
    /// golden of the alternative sign convention carries it in the manifest the runner reads, so the option is
    /// exercised by a program that runs and compares its output, never only by the generator's validation.</summary>
    [Fact]
    public void TheAlternativeSignConventionTest_CarriesItsWiseOwlOption()
    {
        SuiteTest test = ConformanceSuite.Manifest.Editions["2023"]["pb803_sign_encoding_ascii"];
        Assert.Equal("ascii", test.Options["sign-encoding"]);
        Assert.Equal(ConformanceSuiteRunner.Pass, ConformanceSuiteRunner.Judge("2023", test).Verdict);
    }
}
