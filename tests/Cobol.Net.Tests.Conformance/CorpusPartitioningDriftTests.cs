// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ NO SERIAL COLLECTION OF THE CONFORMANCE LEG MAY GROW BACK INTO ITS WALL (kb/Work PB2527): the corpus runner
/// keeps at least 24 partitions and the optional-word subset sweep at least 8, and no partition of either holds more
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
public sealed class CorpusPartitioningDriftTests
{
    /// <summary>Rows (positive and negative) one corpus partition may hold: 193 at 24 partitions when this was set
    /// (2,165 positive, 2,470 negative), about 35 s on one thread.</summary>
    private const int MaxCorpusRowsPerPartition = 250;

    /// <summary>Formats one optional-word partition may hold: each format's whole power set runs serially.</summary>
    private const int MaxFormatsPerOptionalWordPartition = 3;

    [Fact]
    public void TheCorpusRunner_KeepsItsPartitionCount_AndBoundsEveryPartition()
    {
        Assert.True(CorpusRunnerTestsBase<Slot0>.Partitions >= 24,
            $"CorpusRunnerTestsBase.Partitions is {CorpusRunnerTestsBase<Slot0>.Partitions}; the measured floor is 24 "
            + "(kb/Work PB2527): fewer partitions make each a serial pole of the Conformance leg again.");
        int total = CorpusRunnerTestsBase<Slot0>.AllEnabledPositive().Count()
                    + CorpusRunnerTestsBase<Slot0>.AllEnabledNegative().Count();
        int perPartition = (total + CorpusRunnerTestsBase<Slot0>.Partitions - 1) / CorpusRunnerTestsBase<Slot0>.Partitions;
        Assert.True(perPartition <= MaxCorpusRowsPerPartition,
            $"the corpus has grown to {total} rows, {perPartition} per partition at "
            + $"{CorpusRunnerTestsBase<Slot0>.Partitions} partitions (bound {MaxCorpusRowsPerPartition}): raise "
            + "CorpusRunnerTestsBase.Partitions, add the partition classes and slots, and re-measure (kb/Work PB2527).");
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
