// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PERFORMANCE BASELINE'S COMPARISON MUST BE ABLE TO FAIL (kb/Work PB2117): <c>scripts/arch/perf_baseline.py
/// --self-test</c> plants a regression, a vanished row, an allocation change and a machine change into a synthetic
/// record and must flag each, stay silent on a delta inside the noise band, round-trip its own record format, and find
/// the hot-path programs, their witnesses and the benchmark's <c>[Params]</c> to be one set.
/// </summary>
/// <remarks>
/// Every R3 and R4 wave of the architecture review proves "performance not worse than the R0 baseline beyond noise"
/// (<c>docs/rearchitecture/DESIGN-architecture-review.md</c> §4 item 5) with <c>perf_baseline.py --against</c>, and
/// nothing else compares a time (no test asserts one: kb/Work PB1590). A comparison that had quietly stopped flagging
/// would turn every one of those proofs into a false green, so the self-test runs here, every gate, and the case NAMES
/// are asserted: a self-test reduced to its happy path still exits 0. This test measures nothing and times nothing.
/// </remarks>
public sealed class PerfBaselineSelfTestDriftTests
{
    [Fact]
    public void TheBaselineComparison_ProvesEveryArmCanFail()
    {
        string script = TestRepo.Scripts("arch", "perf_baseline.py");
        Assert.True(File.Exists(script), $"the performance-baseline script is missing: {script}");
        var r = PythonInstrument.Run(script, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("SELF-TEST: PASS", StringComparison.Ordinal),
            $"perf_baseline.py --self-test failed (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        foreach (string arm in new[] { "silent on identical records", "silent within the band", "fires REGRESSION",
                     "fires ALLOC", "fires MISSING", "flags CONDITIONS", "round-trips the record", "programs agree" })
        {
            Assert.True(r.Stdout.Contains("ok   " + arm, StringComparison.Ordinal),
                $"`perf_baseline.py --self-test` no longer drives '{arm}' — a comparison never seen to fail is no "
                + $"evidence that a later wave stayed inside the baseline.\n{r.Stdout}{r.Stderr}");
        }
    }
}
