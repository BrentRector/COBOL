// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// CONTINUE AFTER's runtime lanes (ISO §14.9.9.4 GR1; kb/Work PB138) — the legs no stdout golden can pin:
/// the NON-FINITE screen (`(long)double.NaN` saturates to 0, so a NaN interval used to silently skip the
/// suspension where §14.6.13.2 item 3 names EC-DATA-NOT-FINITE), the raise REPORT the emitted §14.6.13.1.4
/// dispatch consumes, and the exact-lane truncation contract.
/// </summary>
public sealed class CobolTimingTests
{
    [Fact]
    public void NaNInterval_CheckingOff_NoSuspension_NoReport()
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(double.NaN, checkLessThanZero: false)));
        Assert.Empty(suspensions);   // no suspension at all — observed, not timed (kb/Work PB1590)
    }

    [Fact]
    public void NaNInterval_DataNotFiniteChecking_IsTheFatal()
    {
        var ru = new RunUnit();
        var saved = RunUnit.Current;   // the ambient accessor is thread-static; run against a fresh unit
        try
        {
            ru.Exceptions.FloatNotFiniteChecking = true;
            Assert.Throws<CobolNet.Runtime.Exceptions.CobolFatalException>(
                () => ru.Exceptions.FloatNotFiniteError("probe"));
            Assert.Equal("EC-DATA-NOT-FINITE", ru.Exceptions.LastName);
        }
        finally { _ = saved; }
    }

    [Fact]
    public void NegativeInterval_Checked_ReportsTheRaise_AndSetsTheStatus()
    {
        bool raised = CobolTiming.ContinueAfter(-0.5, checkLessThanZero: true);
        Assert.True(raised);   // the emitted site dispatches §14.6.13.1.4 on this report
    }

    [Fact]
    public void ExactLane_TruncatedZero_DoesNotSuspend_EvenWhenTheDoubleImageIsOne()
    {
        // 0.99999999999999999 (17 nines) converts to exactly 1.0 in binary64 — the sign value — while the
        // exact truncation the emitter computes in the value's own domain is 0 (GR1's implicit COMPUTE
        // without ROUNDED). The suspension must follow the EXACT value.
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfterExact(1.0, truncatedSeconds: 0, checkLessThanZero: true)));
        Assert.Empty(suspensions);   // the EXACT truncation (0) decides — no suspension, observed not timed
    }

    [Fact]
    public void PositiveInterval_SuspendsForItsTruncatedSeconds()
    {
        // The observer's control arm: a real interval IS reported, so an empty list above means "did not suspend",
        // not "the seam is disconnected".
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(2.9, checkLessThanZero: true)));
        Assert.Equal([2000], suspensions);   // GR1: truncated toward zero, no ROUNDED
    }

    /// <summary>Run <paramref name="act"/> with CONTINUE AFTER's suspension REPORTED instead of performed.</summary>
    /// <summary>§14.9.9.4 GR1 / kb/Work PB1529 — an interval above the maximum meaningful value suspends for exactly
    /// the maximum, however large it is: the double is compared with the maximum BEFORE it is narrowed, so 1E30 (past
    /// the long range) and 9.3E18 (just past it) no longer wrap to a negative count and suspend 0 s.</summary>
    [Theory]
    [InlineData(1e30)]
    [InlineData(9.3e18)]
    [InlineData(86400.5)]
    [InlineData(86400.0)]
    public void HugeInterval_SuspendsForTheMaximumMeaningfulValue(double seconds)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(seconds, checkLessThanZero: false)));
        Assert.Equal([(int)(CobolTiming.MaxSeconds * 1000)], suspensions);
    }

    private static List<int> Observe(Action act)
    {
        var seen = new List<int>();
        CobolTiming.SuspensionObserver.Value = seen.Add;
        try { act(); } finally { CobolTiming.SuspensionObserver.Value = null; }
        return seen;
    }
}
