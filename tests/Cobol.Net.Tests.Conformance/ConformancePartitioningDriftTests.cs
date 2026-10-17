// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ NO SERIAL COLLECTION OF THE CONFORMANCE LEG MAY GROW BACK INTO ITS WALL (kb/Work PB2527): the conformance
/// suite's runner (<see cref="ConformanceSuiteTestsBase{TSlot}"/>, the corpus's runner since kb/Work PB3177) keeps at least 24 partitions and the optional-word subset sweep at least 8, and no partition of either holds more
/// rows than its bound, so a corpus that grows past the bound turns this red instead of quietly lengthening the leg.
/// </summary>
/// <remarks>
/// xUnit v2 runs one test class as one serial collection (<see cref="TestPartitioning"/>). The w1033 gate diagnosis
/// measured the Conformance leg 32-wide for about 90-160 s and then 95-220 s at 1-4 threads, because the corpus ran as
/// 3 partitions of about 1,546 cases (246-282 s each) and the optional-word sweep as one 23-row class (131-152 s).
/// Replaying the measured durations onto 32 workers modeled the leg 247 s to 118 s with 24 corpus partitions, an 8-way
/// optional-word split and longest-first order (the order is the gate plan's, timed from the shared timings store:
/// <c>scripts/gate_plan.py</c>'s self-test arms). <see cref="TestPartitionAudit"/> proves each family's partitions cover
/// its rows exactly once; this test bounds how many rows one partition may hold.
/// </remarks>
public sealed class ConformancePartitioningDriftTests
{
    /// <summary>Rows (tests and refusals, each at one edition) one suite partition may hold: about 440 at 24
    /// partitions when the corpus moved into the suite (kb/Work PB3177: 2,491 test rows and 8,079 refusal rows, 441 a partition; a refusal
    /// row is one front-end compile at one edition, where the corpus runner's negative row compiled at each of its editions).</summary>
    private const int MaxSuiteRowsPerPartition = 500;

    /// <summary>Formats one optional-word partition may hold: each format's whole power set runs serially.</summary>
    private const int MaxFormatsPerOptionalWordPartition = 3;

    [Fact]
    public void TheSuiteRunner_KeepsItsPartitionCount_AndBoundsEveryPartition()
    {
        Assert.True(ConformanceSuiteTestsBase<Slot0>.Partitions >= 24,
            $"ConformanceSuiteTestsBase.Partitions is {ConformanceSuiteTestsBase<Slot0>.Partitions}; the measured floor "
            + "is 24 (kb/Work PB2527): fewer partitions make each a serial pole of the Conformance leg again.");
        int total = ConformanceSuiteTestsBase<Slot0>.AllTests().Count() + ConformanceSuiteTestsBase<Slot0>.AllRefusals().Count();
        int perPartition = (total + ConformanceSuiteTestsBase<Slot0>.Partitions - 1) / ConformanceSuiteTestsBase<Slot0>.Partitions;
        Assert.True(perPartition <= MaxSuiteRowsPerPartition,
            $"the suite has grown to {total} rows, {perPartition} per partition at "
            + $"{ConformanceSuiteTestsBase<Slot0>.Partitions} partitions (bound {MaxSuiteRowsPerPartition}): raise "
            + "ConformanceSuiteTestsBase.Partitions, add the partition classes and slots, and re-measure (kb/Work PB2527).");
    }

    [Fact]
    public void TheOptionalWordSweep_KeepsItsPartitionCount_AndBoundsEveryPartition()
    {
        Assert.True(OptionalWordSubsetDriftTestsBase<Slot0>.Partitions >= 8,
            $"OptionalWordSubsetDriftTestsBase.Partitions is {OptionalWordSubsetDriftTestsBase<Slot0>.Partitions}; the "
            + "measured floor is 8 (kb/Work PB2527).");
        int total = OptionalWordSubsetDriftTestsBase<Slot0>.AllCases().Count();
        int perPartition = (total + OptionalWordSubsetDriftTestsBase<Slot0>.Partitions - 1)
                           / OptionalWordSubsetDriftTestsBase<Slot0>.Partitions;
        Assert.True(perPartition <= MaxFormatsPerOptionalWordPartition,
            $"the optional-word sweep has {total} formats, {perPartition} per partition (bound "
            + $"{MaxFormatsPerOptionalWordPartition}): raise OptionalWordSubsetDriftTestsBase.Partitions.");
    }
}
