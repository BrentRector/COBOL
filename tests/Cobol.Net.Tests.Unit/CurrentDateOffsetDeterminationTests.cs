// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Binding.Procedure;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.21.3 r1 / §15.99.3 r1 21-character layout, position by position — and the ONE owner determination
/// that layout forces (owner answered 2026-08-30: <b>report the TRUE offset</b>).
///
/// <para><b>The determination, and why the standard leaves no conforming alternative.</b> §15.21.3 r1 fixes the
/// UTC-offset hours in positions 18–19 at "00 through 13" when position 17 is '+' and "00 through 12" when it is
/// '–' (cite.py --check 15.21.3 "two numeric digits are returned in the range 00 through 13 indicating the number
/// of hours that the local time is ahead of Coordinated Universal Time" → OK; §15.99.3 r1 carries the identical
/// table, verified the same way). Real offsets exceed both bounds: Pacific/Kiritimati is UTC+14:00, a zone both
/// IANA and Windows ship ("Line Islands Standard Time"), and the <c>COBOLNET_CLOCK</c> pin reaches −14:00 through
/// <see cref="DateTimeOffset"/>, whose offset range is ±14:00. For such a host the rule's table offers NO in-range
/// value, so every available behavior departs from something: emitting 14 leaves the stated range, clamping to 13
/// MISREPORTS the offset, and writing '0'/'00'/'00' asserts position 17's "the system … does not have the facility
/// to provide the local time differential factor" (cite.py --check 15.21.3 → OK), which is false. WiseOwl COBOL
/// reports the true offset and documents the departure — <c>docs/CONFORMANCE.md</c> §7, and inventory rows
/// RV-15.21.3-1 / RV-15.99.3-1 at DOCUMENTED-NON-SUPPORT (§4.2.6/§4.2.7 documentation obligation).</para>
///
/// <para><b>What these tests pin.</b> Not merely "14 comes out". The determination's SCOPE: positions 18–19 are
/// the ONLY positions that ever leave their §15.21.3 r1 range, on either sign, and every other position stays in
/// range in exactly the cases where the offset is extreme — which is what makes the departure documentable as one
/// determination rather than an open-ended divergence. <see cref="R1Violations"/> is a full re-reading of the rule
/// table, not a spot check, so a future change that broke position 15–16 or 20–21 at +14:00 would fail here even
/// though the headline value still "looked right".</para>
///
/// <para>One formatter serves both functions (<c>CobolDate.Format21</c>), which is why the two rules share one
/// determination; <see cref="WhenCompiled_AndCurrentDate_AgreeOnPositions17To21_UnderOneClock"/> proves the
/// sharing rather than assuming it — the compile-time bake and the run-time read are two independent seams.</para>
/// </summary>
[Collection("process-globals")]   // kb/Work PB126: the same-clock test sets-and-restores IntrinsicBinder.
                                  // CompileClock, a PROCESS-global static WhenCompiledStampTests also mutates —
                                  // measured, not anticipated: without this the two raced on the first full run.
public sealed class CurrentDateOffsetDeterminationTests : CobolNetTestBase
{
    /// <summary>A clock pinned to one instant — the in-process form of the <c>COBOLNET_CLOCK</c> seam
    /// (<c>Clock.cs</c>: "Injectable per run unit … in an in-process test").</summary>
    private sealed record FixedClock(DateTimeOffset At) : IClock
    {
        public DateTimeOffset Now() => At;
    }

    /// <summary>Every position of a 21-character CURRENT-DATE / WHEN-COMPILED value that leaves the range
    /// §15.21.3 r1 states for it, named by its position span. EMPTY means the value satisfies the rule outright.
    ///
    /// <para>This is a transcription of the rule's table, deliberately re-derived here rather than compared
    /// against the formatter's own logic: a validator written from the implementation could only ever agree with
    /// it. Positions 13–14 are checked against 00–59 because no LEAP-SECOND directive is in effect in these
    /// fixtures (§7.3.17.4 GR1 implies OFF, GR3 then forbids &gt; 59).</para></summary>
    private static IReadOnlyList<string> R1Violations(string v)
    {
        var bad = new List<string>();
        if (v.Length != 21) return ["length"];
        void Digits(string span, int from1, int to1, int lo, int hi)
        {
            string s = v[(from1 - 1)..to1];
            if (!s.All(char.IsAsciiDigit) || int.Parse(s) < lo || int.Parse(s) > hi) bad.Add(span);
        }

        Digits("1-4", 1, 4, 0, 9999);        // four numeric digits of the year in the Gregorian calendar
        Digits("5-6", 5, 6, 1, 12);          // month of the year, 01 through 12
        Digits("7-8", 7, 8, 1, 31);          // day of the month, 01 through 31
        Digits("9-10", 9, 10, 0, 23);        // hours past midnight, 00 through 23
        Digits("11-12", 11, 12, 0, 59);      // minutes past the hour, 00 through 59
        Digits("13-14", 13, 14, 0, 59);      // seconds past the minute (LEAP-SECOND OFF — §7.3.17.4 GR1/GR3)
        Digits("15-16", 15, 16, 0, 99);      // hundredths of a second, 00 through 99

        char sign = v[16];                   // position 17: '–', '+' or '0'
        if (sign is not ('+' or '-' or '0')) bad.Add("17");

        // Positions 18-19: '+' ⇒ 00..13, '–' ⇒ 00..12, '0' ⇒ 00. Positions 20-21: 00..59, or 00 when 17 is '0'.
        int hoursMax = sign switch { '+' => 13, '-' => 12, _ => 0 };
        Digits("18-19", 18, 19, 0, hoursMax);
        Digits("20-21", 20, 21, 0, sign == '0' ? 0 : 59);
        return bad;
    }

    /// <summary>The fixture table. Every expected value is hand-derived from §15.21.3 r1's position table — the
    /// year/month/day/hour/minute/second/hundredth of the instant, then the sign, then |hours|, then |minutes| —
    /// never read off a run of the formatter. <c>determinationSpan</c> names the position span the determination
    /// permits outside the rule for that case, or null when the case is fully in range.</summary>
    public static TheoryData<string, DateTimeOffset, string, string?> Layout() => new()
    {
        // Pacific/Kiritimati — the real UTC+14:00 zone. '+' hours = 14, outside r1's 00..13.
        { "+14:00 Kiritimati", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(14)),
          "2026083110203045+1400", "18-19" },
        // The '+' boundary the rule DOES admit — 13 is in range, so nothing here may be flagged.
        { "+13:00 boundary", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(13)),
          "2026083110203045+1300", null },
        // Asia/Kathmandu — a 45-minute offset, exercising positions 20-21 away from 00.
        { "+05:45 Kathmandu", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, new TimeSpan(5, 45, 0)),
          "2026083110203045+0545", null },
        // UTC itself: r1 assigns "the same as … Coordinated Universal time" to '+', not to '0' or '–'.
        { "+00:00 UTC", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.Zero),
          "2026083110203045+0000", null },
        // A negative half-hour offset (Marquesas): sign '-', minutes 30.
        { "-09:30 Marquesas", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, new TimeSpan(-9, -30, 0)),
          "2026083110203045-0930", null },
        // The '–' boundary r1 admits — 12 is in range.
        { "-12:00 boundary", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(-12)),
          "2026083110203045-1200", null },
        // ⛔ THE MINUS SIDE IS REACHABLE TOO. An earlier record claimed the '–' branch "cannot exceed 12 because
        // no TimeZoneInfo zone is below -12:00". True of the HOST zone; false of the value CURRENT-DATE reports,
        // because COBOLNET_CLOCK pins a DateTimeOffset directly and DateTimeOffset admits -14:00.
        { "-14:00 pinned clock", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(-14)),
          "2026083110203045-1400", "18-19" },
        // Positions 15-16 at their floor — r1 allows 00 both for a true zero and for a system with no
        // fractional-second facility; nothing distinguishes them in the value.
        { "zero hundredths", new DateTimeOffset(2026, 8, 31, 10, 20, 30, 0, TimeSpan.FromHours(2)),
          "2026083110203000+0200", null },
        // Single-digit month, day, hour, minute, second and hundredth: every field zero-padded to its width.
        { "single-digit fields", new DateTimeOffset(2026, 1, 2, 3, 4, 5, 60, TimeSpan.FromHours(-7)),
          "2026010203040506-0700", null },
    };

    [Theory]
    [MemberData(nameof(Layout))]
    public void Format21_EveryPosition_PinnedToSpec(string name, DateTimeOffset at, string expected, string? determinationSpan)
    {
        string actual = CobolDate.Format21(at);
        Assert.Equal(expected, actual);
        Assert.Equal(21, actual.Length);   // §15.21.1: "returns a 21-character alphanumeric value"

        // The determination's SCOPE, asserted both ways: an in-range case must satisfy every position of r1, and
        // a determination case must depart in EXACTLY the one span the determination covers and nowhere else.
        var violations = R1Violations(actual);
        if (determinationSpan is null)
            Assert.True(violations.Count == 0,
                $"{name}: §15.21.3 r1 violated at {string.Join(", ", violations)} in '{actual}'");
        else
            Assert.Equal([determinationSpan], violations);
    }

    [Fact]
    public void Determination_CoversOnlyOffsetHours_OnBothSigns()
    {
        // The whole table at once: across every fixture, the ONLY position span that ever leaves §15.21.3 r1 is
        // 18-19. If a change made, say, position 17 emit '0' for an extreme offset, or truncated the value to 20
        // characters, this fails even though each individual case might still look plausible.
        var spans = Layout()
            .Select(row => CobolDate.Format21((DateTimeOffset)row[1]))
            .SelectMany(R1Violations)
            .Distinct()
            .Order()
            .ToList();
        Assert.Equal(["18-19"], spans);

        // And both signs are genuinely reachable — a determination documented for '+' only would be incomplete.
        Assert.Equal("+1400", CobolDate.Format21(new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(14)))[16..]);
        Assert.Equal("-1400", CobolDate.Format21(new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(-14)))[16..]);
    }

    [Fact]
    public void CurrentDate_ReadsTheClockOffsetUnaltered()
    {
        // §15.21.1: the value is the "calendar date, time of day, and local time differential factor provided by
        // the system on which the function is evaluated". The run unit's clock IS that system reading, so the
        // offset it carries must reach positions 17-21 unmodified — no TimeZoneInfo.Local re-derivation, no clamp.
        var at = new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(14));
        var prior = RunUnit.Current.Clock;                 // AsyncLocal ambient run unit — restored below
        try
        {
            RunUnit.Current.Clock = new FixedClock(at);
            Assert.Equal("2026083110203045+1400", CobolDate.CurrentDate());
        }
        finally { RunUnit.Current.Clock = prior; }
    }

    [Fact]
    public void WhenCompiled_AndCurrentDate_AgreeOnPositions17To21_UnderOneClock()
    {
        // §15.99.3 r1's table is character-for-character §15.21.3 r1's, and the determination is stated ONCE for
        // both — which is only legitimate if the two paths really share the formatter. They are independent
        // seams: WHEN-COMPILED is baked at compile time from IntrinsicBinder.CompileClock, CURRENT-DATE is read
        // at run time from RunUnit.Current.Clock. Pin both to ONE instant and require the same 21 characters,
        // positions 17-21 (the offset the determination is about) asserted in their own right.
        var at = new DateTimeOffset(2026, 8, 31, 10, 20, 30, 450, TimeSpan.FromHours(14));

        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DDWC1400.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WC PIC X(21).
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION WHEN-COMPILED TO WC
                DISPLAY WC
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "ddwc1400.cob");
        File.WriteAllText(srcPath, source);

        string generated;
        var priorCompile = IntrinsicBinder.CompileClock;
        try
        {
            IntrinsicBinder.CompileClock = () => at;
            var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, DialectLevel: 2023));
            Assert.True(result.Success, string.Join("\n", result.Errors));
            generated = File.ReadAllText(result.GeneratedCsPath!);
        }
        finally { IntrinsicBinder.CompileClock = priorCompile; }

        // Read the stamp OUT of the generated source by its shape rather than searching for the value we expect —
        // searching for the expectation would pass on a compiler that baked nothing at all.
        var m = Regex.Match(generated, "\"(?<v>[0-9]{16}[-+0][0-9]{4})\"");
        Assert.True(m.Success, "no §15.99.3 21-character stamp was baked into the generated source");
        string baked = m.Groups["v"].Value;

        string currentDate;
        var priorClock = RunUnit.Current.Clock;
        try
        {
            RunUnit.Current.Clock = new FixedClock(at);
            currentDate = CobolDate.CurrentDate();
        }
        finally { RunUnit.Current.Clock = priorClock; }

        Assert.Equal(currentDate, baked);
        Assert.Equal(currentDate[16..], baked[16..]);      // positions 17-21 — the determination's subject
        Assert.Equal("+1400", baked[16..]);
    }
}
