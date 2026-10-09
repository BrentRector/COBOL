// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.40.4 r2 UTC roll against the integer-date-form range (kb/Work R25) — the EMIT-side sibling of
/// PB23's analyzer-side crash: every argument individually legal (the §15.40.3 r4/r5 screens pass), and the
/// r2 adjustment then carries the DATE outside 1..3,067,671 (§15.5.2). Before the fix,
/// <c>FORMATTED-DATETIME("YYYYMMDDThhmmssZ", 3067671, 86399, -1439)</c> threw a raw CLR
/// <c>ArgumentOutOfRangeException</c> out of <c>Epoch.AddDays</c>, and the low-end mirror emitted year 1600
/// (§15.3.1.3 requires "greater than 1600"). §15.3 permits only EC-ARGUMENT-FUNCTION or the default.
/// <para>⚠ These run with EC checking OFF, so the error surfaces as the §15.3 default "" — the RAISE is
/// pinned end-to-end by the golden <c>r25_utc_roll_date_range</c>.</para>
/// </summary>
public sealed class CobolDateUtcRollRangeTests
{
    [Fact]
    public void RollPastTheMaxIntegerDate_IsTheDefault_NotACrash()
    {
        // 86399 s + 1439 min westward = past midnight of 9999-12-31 → integer date 3,067,672: no form.
        Assert.Equal("", CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 3067671, 86399, 0, -1439));
    }

    [Fact]
    public void RollBelowDayOne_IsTheDefault_NotYear1600()
    {
        // 0 s − 1439 min eastward = before midnight of 1601-01-01 → integer date 0: §15.3.1.3 bars 1600.
        Assert.Equal("", CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 1, 0, 0, 1439));
    }

    [Fact]
    public void LegalRollsAcrossTheBoundary_StillEmit()
    {
        // One day inside each end: the same offsets roll INTO range and must keep emitting.
        Assert.Equal("99991231T235859Z",
            CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 3067670, 86399, 0, -1439));
        Assert.Equal("16010101T000100Z",
            CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 2, 0, 0, 1439));
    }

    [Fact]
    public void FormattedTime_WithADateBearingFormat_ReachesTheSameGuard()
    {
        // FORMATTED-TIME shares EmitFormatted with day = 1; a combined format's eastward roll hits day 0.
        Assert.Equal("", CobolDate.FormattedTime("YYYYMMDDThhmmssZ", 0, 0, 1439));
    }

    [Fact]
    public void TimeOnlyFormats_RollFreely_TheGuardIsDateGated()
    {
        // A time-only format never reads the day (the §15.41 normal case) — the roll must stay unguarded.
        Assert.Equal("000100Z", CobolDate.FormattedTime("hhmmssZ", 0, 0, 1439));
    }

    [Fact]
    public void TheLeapSecond_IsSecond60OfItsShiftedMinute_NeverTheNextDay()
    {
        // kb/Work PB2629: under >>LEAP-SECOND ON 86 400 s is 23:59:60 (§7.3.17.4 GR4). An offset of 0 (what an
        // omitted offset is, §15.40.3 r7) must not roll it into 00:00:00 of the next day, and at the last integer
        // date that roll used to leave the §15.5.2 range and return the default.
        Assert.Equal("99991231T235960Z",
            CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 3067671, 86400, 0, 0, leapSecond: true));
        // A whole-minute offset shifts the minute and keeps the second at 60 (§15.40.4 r2).
        Assert.Equal("16010102T005960Z",
            CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 1, 86400, 0, -60, leapSecond: true));
        Assert.Equal("225960Z", CobolDate.FormattedTime("hhmmssZ", 86400, 0, 60, leapSecond: true));
    }
}
