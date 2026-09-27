// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.IO;   // IClock — the injectable RunUnit.Clock seam (CobolDateWindowingTests' own using set)
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §15.38.4 r2 — the ACCURACY of FORMATTED-CURRENT-DATE's time portion (Annex A.1 item 87,
/// docs/CONFORMANCE.md row 87), measured through the injectable <see cref="RunUnit.Clock"/> seam.
/// <para>⛔ WHY THIS EXISTS BESIDE A GOLDEN. The conformance golden
/// <c>2023/l1_formatted_current_date_accuracy</c> measures the ACCURACY CEILING — that a fraction field
/// wider than the clock tick renders zeros past the seventh digit — which is deterministic under any
/// clock. It cannot measure the other half, that the first seven digits ARE the clock tick and nothing
/// is lost, because the corpus runner injects no environment and the host clock is not a value. That
/// half needs a pinned instant, which is what this test supplies. Neither half alone verifies r2's
/// determination; the pair does. (Until 2026-09-02 the row's evidence was
/// <c>CobolDateWindowingTests.SecondsPastMidnight_PinnedClock_ExactTicks</c> — item 171's test, which
/// never calls this function — and then <c>NowFunctions_OnePinnedClock_OneInstant</c>, which renders
/// <c>ss.ss</c>: TWO fraction digits of a seven-digit determination.)</para>
/// </summary>
public sealed class CobolDateAccuracyTests
{
    private sealed class FixedClock(DateTimeOffset at) : IClock
    {
        public DateTimeOffset Now() => at;
    }

    /// <summary>Run <paramref name="body"/> under a pinned <see cref="RunUnit.Clock"/>, restoring the
    /// prior clock afterward.</summary>
    private static T UnderClock<T>(DateTimeOffset at, Func<T> body)
    {
        var prior = RunUnit.Current.Clock;
        RunUnit.Current.Clock = new FixedClock(at);
        try { return body(); }
        finally { RunUnit.Current.Clock = prior; }
    }

    [Fact]
    public void FormattedCurrentDate_PinnedClock_ClockTickThenZeros()
    {
        // The instant: 2026-06-10 05:14:27.8124791 at +02:30 — a tick-exact local time whose SEVENTH
        // fraction digit is non-zero (1), so a coarser clock or a narrower carrier is visible, at an
        // offset no test machine runs at, so the offset demonstrably travelled through the seam.
        var at = new DateTimeOffset(2026, 6, 10, 5, 14, 27, TimeSpan.FromMinutes(150)).AddTicks(8_124_791);

        // §15.3.3.2's floor for the implementor maximum is nine digits; row 87 renders the clock tick's
        // SEVEN significant digits and zeros beyond. At width 7 nothing is lost …
        Assert.Equal("2026-06-10T05:14:27.8124791+02:30",
            UnderClock(at, () => CobolDate.FormattedCurrentDate("YYYY-MM-DDThh:mm:ss.sssssss+hh:mm")));
        // … at width 9 the two digits past the tick are ZEROS, not noise …
        Assert.Equal("2026-06-10T05:14:27.812479100+02:30",
            UnderClock(at, () => CobolDate.FormattedCurrentDate("YYYY-MM-DDThh:mm:ss.sssssssss+hh:mm")));
        // … and at WiseOwl COBOL's documented §15.3.3.2 maximum of 18 the field is still the tick followed
        // by zeros — the exact shape the conformance golden observes without being able to name the
        // instant that produced it.
        Assert.Equal("2026-06-10T05:14:27.812479100000000000+02:30",
            UnderClock(at, () => CobolDate.FormattedCurrentDate(
                "YYYY-MM-DDThh:mm:ss.ssssssssssssssssss+hh:mm")));
    }
}
