// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.100/§15.23/§15.25 Y2K windowing core + §15.80 SECONDS-PAST-MIDNIGHT (P11 Step 5) + the §15.21 /
/// §15.38 now-function seam (R21) — the legs the conformance golden <c>intrinsics_date_window</c> cannot pin:
/// the EXECUTION-TIME defaults (argument-3 omitted = the year at evaluation, §15.100.3 r5; argument-2
/// omitted = 50, r3) and the clock value itself, both deterministic here via an injected
/// <see cref="RunUnit.Clock"/>. The windowing values are hand-derived in PHASE-11-scout-notes.md
/// (spec:date-window); the pinned-argument shapes are covered by the golden.
/// </summary>
public sealed class CobolDateWindowingTests
{
    private sealed class FixedClock(DateTimeOffset at) : IClock
    {
        public DateTimeOffset Now() => at;
    }

    /// <summary>Run <paramref name="body"/> under a pinned <see cref="RunUnit.Clock"/>, restoring the prior
    /// clock afterward (the ambient RunUnit is AsyncLocal — xUnit runs each fact in its own context, but the
    /// restore keeps the discipline explicit).</summary>
    private static T UnderClock<T>(DateTimeOffset at, Func<T> body)
    {
        var prior = RunUnit.Current.Clock;
        RunUnit.Current.Clock = new FixedClock(at);
        try { return body(); }
        finally { RunUnit.Current.Clock = prior; }
    }

    [Fact]
    public void YearToYyyy_DefaultArg3_IsExecutionYear()
    {
        // §15.100.3 r5: argument-3 omitted = FUNCTION NUMVAL(FUNCTION CURRENT-DATE(1:4)) — the year at
        // EXECUTION time, read through the injectable clock. Exec year 2026, default arg-2 = 50 (r3) →
        // maximum-year 2076, window [1977, 2076]: MOD(2076,100) = 76 ≥ 76 → 2076 (r2a); 77 > 76 → 1977 (r2b).
        Assert.Equal(2076, UnderClock(new DateTime(2026, 6, 10), () => CobolDate.YearToYyyy(76)));
        Assert.Equal(1977, UnderClock(new DateTime(2026, 6, 10), () => CobolDate.YearToYyyy(77)));
    }

    [Fact]
    public void DateToYyyymmdd_Defaults_DelegateToWindowCore()
    {
        // §15.23.4 r1 delegation with both optional arguments defaulted, pinned to exec year 2026:
        // YY 49 → max 2076, MOD 76 ≥ 49 → 2049 → 20491231; YY 99 → 76 < 99 → 1999 → 19990704.
        Assert.Equal(20491231, UnderClock(new DateTime(2026, 1, 1), () => CobolDate.DateToYyyymmdd(491231)));
        Assert.Equal(19990704, UnderClock(new DateTime(2026, 1, 1), () => CobolDate.DateToYyyymmdd(990704)));
    }

    [Fact]
    public void DateToYyyymmdd_R6_SumIsArgument2PlusWrittenArgument3_NotClockYear()
    {
        // §15.23.3 r6 words the sum on "the year at the time of execution"; §15.23.1 defines that year as
        // argument-3 ("Argument-3 specifies the year at the time of execution"), so a WRITTEN argument-3 keys
        // the window (kb/Work PB311). Clock pinned to 2026 so the two readings disagree on every probe:
        // (8299, 1700) sums to 9999 — legal — where clock-year 2026 + 8299 = 10325 would be out of window;
        // (-400, 2100) sums to 1700 — legal — where 2026 - 400 = 1626 would be; (7000, 3000) sums to 10000 —
        // out of window — where 2026 + 7000 = 9026 would be legal. §15.100.4: 99851003 is r2a (MOD 99 >= 85,
        // 85 + 100*99 = 9985); 16851003 is r2b (MOD 0 < 85, 85 + 100*16 = 1685). Out of window is
        // EC-ARGUMENT-FUNCTION, whose checking-off value is 0.
        var at = new DateTime(2026, 6, 10);
        Assert.Equal(99851003, UnderClock(at, () => CobolDate.DateToYyyymmdd(851003, 8299, 1700)));
        Assert.Equal(16851003, UnderClock(at, () => CobolDate.DateToYyyymmdd(851003, -400, 2100)));
        Assert.Equal(0, UnderClock(at, () => CobolDate.DateToYyyymmdd(851003, 8300, 1700)));
        Assert.Equal(0, UnderClock(at, () => CobolDate.DateToYyyymmdd(851003, -401, 2100)));
        Assert.Equal(0, UnderClock(at, () => CobolDate.DateToYyyymmdd(851003, 7000, 3000)));
    }

    [Fact]
    public void DayToYyyyddd_R6_SumIsArgument2PlusArgument3()
    {
        // §15.25.3 r6 — the same bounds on the sibling arm (the DAY form is the same delegation).
        var at = new DateTime(2026, 6, 10);
        Assert.Equal(9985365, UnderClock(at, () => CobolDate.DayToYyyyddd(85365, 8299, 1700)));
        Assert.Equal(1685365, UnderClock(at, () => CobolDate.DayToYyyyddd(85365, -400, 2100)));
        Assert.Equal(0, UnderClock(at, () => CobolDate.DayToYyyyddd(85365, 8300, 1700)));
        Assert.Equal(0, UnderClock(at, () => CobolDate.DayToYyyyddd(85365, -401, 2100)));
    }

    [Fact]
    public void SecondsPastMidnight_PinnedClock_ExactTicks()
    {
        // §15.80.3 r1-r3 + Annex D.31.5.4: local time 05:14:27.8124791 → 18867.8124791 seconds. The runtime
        // returns the day's TICK count = the unscaled value at scale 7 (the documented 100 ns precision):
        // 18867.8124791 s × 10^7 = 188 678 124 791 ticks.
        var at = new DateTime(2026, 6, 10, 5, 14, 27).AddTicks(8124791);
        Assert.Equal(188_678_124_791L, UnderClock(at, CobolDate.SecondsPastMidnight));
    }

    [Fact]
    public void SecondsPastMidnight_Range_StandardNumericTimeForm()
    {
        // §15.5.5 under the (only) LEAP-SECOND OFF mode: [0, 86400) seconds — i.e. [0, 864_000_000_000) ticks.
        Assert.Equal(0L, UnderClock(new DateTime(2026, 6, 10, 0, 0, 0), CobolDate.SecondsPastMidnight));
        Assert.Equal(863_999_999_999L, UnderClock(
            new DateTime(2026, 6, 10, 23, 59, 59).AddTicks(9_999_999), CobolDate.SecondsPastMidnight));
    }

    [Fact]
    public void YearToYyyy_SpecNote1_Examples()
    {
        // §15.100.4 NOTE 1's own pair — the same anchors the conformance golden pins end-to-end.
        Assert.Equal(2004, CobolDate.YearToYyyy(4, 23, 1995));
        Assert.Equal(1898, CobolDate.YearToYyyy(98, -15, 2008));
    }

    [Fact]
    public void CurrentDate_PinnedClock_Renders21CharsWithOffset()
    {
        // §15.21.3 r1 through the seam (R21): the full 21 characters, including the local time differential
        // factor at positions 17–21 — pinned at +02:30, an offset no test machine runs at, so a pass proves
        // the offset travelled through the seam rather than leaking in from the machine.
        var at = new DateTimeOffset(2026, 6, 10, 14, 30, 45, TimeSpan.FromMinutes(150)).AddTicks(6_700_000);
        Assert.Equal("2026061014304567+0230", UnderClock(at, CobolDate.CurrentDate));
    }

    [Fact]
    public void NowFunctions_OnePinnedClock_OneInstant()
    {
        // §15.38.4 r1 through the seam — the R21 defect was two now-functions reading two clocks in one run
        // unit: one pinned instant, three functions, one answer.
        var at = new DateTimeOffset(2026, 6, 10, 5, 14, 27, TimeSpan.FromMinutes(150)).AddTicks(8_124_791);
        Assert.Equal("2026-06-10T05:14:27.81+02:30",
            UnderClock(at, () => CobolDate.FormattedCurrentDate("YYYY-MM-DDThh:mm:ss.ss+hh:mm")));
        Assert.Equal(188_678_124_791L, UnderClock(at, CobolDate.SecondsPastMidnight));
        Assert.Equal("05142781", UnderClock(at, CobolDate.CurrentDate).Substring(8, 8));
    }
}
