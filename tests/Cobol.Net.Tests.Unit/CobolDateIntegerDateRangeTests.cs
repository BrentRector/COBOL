// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A WEEK DATE CAN SATISFY EVERY SUBFIELD RULE AND STILL HAVE NO INTEGER DATE FORM (fix-queue PB23).
///
/// <para><b>ISO §15.3.1.7</b> constrains a week date's subfields <i>individually</i> — year 1601–9999, week 01–52
/// (01–53 in a long ISO year), day-of-week 1–7 — while <b>§15.5.2</b> caps the integer date form itself at
/// "the value of FUNCTION INTEGER-OF-DATE (99991231), which is 3,067,671". Because 9999-12-31 <i>is</i> ISO
/// 9999-W52-5, the values 9999-W52-6 and 9999-W52-7 pass every subfield check and denote 10000-01-01/02. No
/// per-subfield range table can catch them, because no subfield is wrong.</para>
///
/// <para><b>What it did:</b> <c>ISOWeek.ToDateTime</c> threw <c>ArgumentOutOfRangeException</c> from inside the
/// SHARED analyzer, so the raw CLR fault escaped THREE intrinsics — INTEGER-OF-FORMATTED-DATE,
/// TEST-FORMATTED-DATETIME and SECONDS-FROM-FORMATTED-TIME. §15.3 rule 14 permits only EC-ARGUMENT-FUNCTION or the
/// implementor-defined result.</para>
///
/// <para>⚠ These run with EC-ARGUMENT-FUNCTION checking OFF, so an argument/returned-value error surfaces as the
/// §15.3 default result 0 rather than a raise. The RAISE is pinned end-to-end by the golden
/// <c>pb23_week_date_beyond_integer_date_form</c>.</para>
/// </summary>
public sealed class CobolDateIntegerDateRangeTests
{
    private static readonly DateTime Epoch = new(1601, 1, 1);
    private const long MaxIntegerDate = 3_067_671;

    /// <summary>§15.5.2 states the ceiling as a NUMBER; this proves our epoch arithmetic actually produces it, so
    /// the constant and the calendar cannot drift apart.</summary>
    [Fact]
    public void TheSpecsStatedCeiling_IsWhatTheEpochArithmeticProduces()
    {
        Assert.Equal(MaxIntegerDate, (new DateTime(9999, 12, 31) - Epoch).Days + 1);
        Assert.Equal(MaxIntegerDate, CobolDate.IntegerOfFormattedDate("YYYYMMDD", "99991231"));
    }

    /// <summary>The two-day window, named exactly. W52-5 is the last representable week date; 6 and 7 are not.
    /// (The fix-queue's own earlier row cited only day 7 — recomputing the boundary showed it is two days.)</summary>
    [Theory]
    [InlineData("9999W521", 3067667L)]
    [InlineData("9999W522", 3067668L)]
    [InlineData("9999W523", 3067669L)]
    [InlineData("9999W524", 3067670L)]
    [InlineData("9999W525", MaxIntegerDate)]   // = 9999-12-31
    [InlineData("9999W526", 0L)]               // = 10000-01-01 — no integer date form
    [InlineData("9999W527", 0L)]               // = 10000-01-02 — no integer date form
    public void TheWeek52Boundary_IsTwoDaysWide(string data, long expected)
        => Assert.Equal(expected, CobolDate.IntegerOfFormattedDate("YYYYWwwD", data));

    /// <summary>The extended format reaches the same arithmetic — a second FORMAT, not a second defect.</summary>
    [Fact]
    public void TheExtendedWeekFormat_BehavesIdentically()
    {
        Assert.Equal(MaxIntegerDate, CobolDate.IntegerOfFormattedDate("YYYY-Www-D", "9999-W52-5"));
        Assert.Equal(0L, CobolDate.IntegerOfFormattedDate("YYYY-Www-D", "9999-W52-6"));
    }

    /// <summary>⛔ THE OTHER TWO INTRINSICS MUST NOT FOLLOW. They share the analyzer, so the crash reached them —
    /// but their rules do not. §15.92.4 requires any non-zero TEST answer to be "the ordinal character position at
    /// which the first error … was detected", and no CHARACTER of "9999W526" is in error (the identical '6' in
    /// "2009W526" is valid), so it is 0. §15.79.4 reads only the TIME subfields.</summary>
    [Fact]
    public void ValidityAndSeconds_AreUnaffectedByAnUnrepresentableDate()
    {
        Assert.Equal(0L, CobolDate.TestFormattedDatetime("YYYYWwwD", "9999W526"));
        Assert.Equal(0L, CobolDate.TestFormattedDatetime("YYYYWwwD", "9999W527"));
        Assert.Equal(0L, CobolDate.TestFormattedDatetime("YYYY-Www-D", "9999-W52-7"));
        Assert.Equal(0L, CobolDate.TestFormattedDatetime("YYYYWwwDThhmmss", "9999W527T101112"));
        Assert.Equal((Int128)36672, CobolDate.SecondsFromFormattedTime("YYYYWwwDThhmmss", "9999W527T101112", 0));
    }

    /// <summary>…and TEST still reports the right POSITION for genuinely invalid data, so the new arm did not
    /// swallow the old one. Week 53 in a common ISO year is detectable at the second week digit (§15.92.4).</summary>
    [Theory]
    [InlineData("YYYYWwwD", "9999W531", 7L)]    // 9999 is a 52-week ISO year
    [InlineData("YYYYWwwD", "1601W531", 7L)]    // matches the external GnuCOBOL corpus' own expectation
    [InlineData("YYYYWwwD", "2009W531", 0L)]    // 2009 IS a long ISO year — 53 weeks, valid
    [InlineData("YYYYMMDD", "99990231", 7L)]
    [InlineData("YYYYDDD", "9999366", 7L)]
    public void GenuinelyInvalidData_StillReportsItsErrorPosition(string format, string data, long expected)
        => Assert.Equal(expected, CobolDate.TestFormattedDatetime(format, data));

    /// <summary>⛔ THE EQUIVALENCE PROOF FOR REPLACING <c>ISOWeek.ToDateTime</c> WITH INTEGER ARITHMETIC. The fix
    /// swapped a library call for a hand-derived computation (week 1 is the week containing Jan 4, §15.3.1.7), and
    /// a hand-derived calendar is exactly the kind of thing that is right at the boundary tested and wrong three
    /// centuries away. This asserts our result equals <c>ISOWeek</c>'s for EVERY (year, week, day) it can express —
    /// and that ours merely returns the §15.3 default where <c>ISOWeek</c> throws, rather than throwing too.</summary>
    [Fact]
    public void TheIntegerWeekArithmetic_AgreesWithISOWeek_Everywhere()
    {
        int compared = 0, beyond = 0;
        for (int y = 1601; y <= 9999; y++)
        {
            int weeks = ISOWeek.GetWeeksInYear(y);
            // Every year's FIRST and LAST week (where the year-boundary arithmetic actually bites), all 7 days.
            foreach (int w in weeks == 53 ? new[] { 1, 2, 52, 53 } : [1, 2, 51, 52])
                for (int d = 1; d <= 7; d++)
                {
                    string data = string.Create(CultureInfo.InvariantCulture, $"{y:D4}W{w:D2}{d}");
                    long actual = CobolDate.IntegerOfFormattedDate("YYYYWwwD", data);
                    DateTime expected;
                    try { expected = ISOWeek.ToDateTime(y, w, d == 7 ? DayOfWeek.Sunday : (DayOfWeek)d); }
                    catch (ArgumentOutOfRangeException) { Assert.Equal(0L, actual); beyond++; continue; }

                    long want = (expected - Epoch).Days + 1;
                    if (want is > 0 and <= MaxIntegerDate) { Assert.Equal(want, actual); compared++; }
                    else { Assert.Equal(0L, actual); beyond++; }
                }
        }
        // ⛔ Assert the POPULATION, not just the comparisons: a sweep that silently compared nothing would pass.
        Assert.True(compared > 200_000, $"only {compared} week dates were compared — the sweep did not run");
        Assert.True(beyond >= 2, $"the out-of-range window was never reached ({beyond}) — the boundary is untested");
    }

    /// <summary>Exhaustive over a handful of years chosen for their shape, so the sampled sweep above cannot hide a
    /// mid-year drift: long ISO years, century non-leap years, and the endpoints.</summary>
    [Theory]
    [InlineData(1601)] [InlineData(1900)] [InlineData(2000)] [InlineData(2009)]
    [InlineData(2020)] [InlineData(2100)] [InlineData(9998)] [InlineData(9999)]
    public void TheIntegerWeekArithmetic_AgreesWithISOWeek_ExhaustivelyForOneYear(int y)
    {
        for (int w = 1; w <= ISOWeek.GetWeeksInYear(y); w++)
            for (int d = 1; d <= 7; d++)
            {
                string data = string.Create(CultureInfo.InvariantCulture, $"{y:D4}W{w:D2}{d}");
                long actual = CobolDate.IntegerOfFormattedDate("YYYYWwwD", data);
                DateTime expected;
                try { expected = ISOWeek.ToDateTime(y, w, d == 7 ? DayOfWeek.Sunday : (DayOfWeek)d); }
                catch (ArgumentOutOfRangeException) { Assert.Equal(0L, actual); continue; }
                long want = (expected - Epoch).Days + 1;
                Assert.Equal(want is > 0 and <= MaxIntegerDate ? want : 0L, actual);
            }
    }
}
