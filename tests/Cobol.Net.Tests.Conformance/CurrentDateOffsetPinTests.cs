// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// CURRENT-DATE end to end, through the <c>COBOLNET_CLOCK</c> seam — what a COBOL program actually receives in
/// the §15.21.3 r1 21-character layout when the run unit's clock carries an extreme UTC offset.
///
/// <para>The unit table (<c>CurrentDateOffsetDeterminationTests</c>) pins the formatter position by position.
/// This pins the whole path a user's program travels: the intrinsic reference binds, the renderer emits the
/// runtime call, the runtime reads <c>RunUnit.Current.Clock</c>, and the 21 characters land in a PIC X(21)
/// receiver in a separate process. The determination (owner answered 2026-08-30 — report the TRUE offset) is only
/// meaningful at this level: it says what a PROGRAM sees, and positions 18-19 carrying "14" is the observable it
/// documents.</para>
///
/// <para>Expected values are derived from §15.21.3 r1's table applied to the pinned instant (cite.py --check
/// 15.21.3 "The character positions returned, numbered from left to right, are" → OK), never copied from a run.
/// Both signs are exercised: '+' at 14 exceeds r1's "00 through 13", and '–' at 14 exceeds its "00 through 12" —
/// the minus side is unreachable from any IANA/Windows host zone but reachable through the pin, because
/// <see cref="DateTimeOffset"/> admits offsets to ±14:00 and the pin parses one directly.</para>
/// </summary>
public sealed class CurrentDateOffsetPinTests
{
    /// <summary>Compile at 2023-strict and run with the run unit's clock pinned to <paramref name="clock"/>
    /// (<c>SystemClock.PinVariable</c> — the cross-process seam the temporal goldens use).</summary>
    private static void AssertCurrentDateUnderClock(string programId, string clock, string expected)
    {
        string dir = CutRunner.NewTempDir("cd21");
        try
        {
            string src = Path.Combine(dir, programId + ".cob");
            string dll = Path.Combine(dir, programId + ".dll");
            src = CompiledProgramCache.StageSource(src, $"""
                IDENTIFICATION DIVISION.
                PROGRAM-ID. {programId}.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 CD PIC X(21).
                PROCEDURE DIVISION.
                MAIN.
                    MOVE FUNCTION CURRENT-DATE TO CD
                    DISPLAY CD
                    STOP RUN.
                """);
            var compiled = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

            var (ok, stdout, detail) = CutRunner.Run(dll, dir, null,
                new Dictionary<string, string?> { [CobolNet.Runtime.IO.SystemClock.PinVariable] = clock });
            Assert.True(ok, detail);
            Assert.Equal(expected, stdout);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    [Fact]
    public void CurrentDate_PlusFourteen_ReportsTrueOffset_PinnedToSpec() =>
        // Pacific/Kiritimati. §15.21.3 r1 positions 18-19 admit only "00 through 13" under a '+' sign, and the
        // table offers no other value for such a host — clamping to 13 would misreport and '0' would assert the
        // false "no facility to provide the local time differential factor" case. The owner determination is to
        // report the true offset; this is that determination's observable, documented in CONFORMANCE.md §7.
        AssertCurrentDateUnderClock("DDCDP14", "2026-08-31T10:20:30.45+14:00", "2026083110203045+1400");

    [Fact]
    public void CurrentDate_MinusFourteen_ReportsTrueOffset_PinnedToSpec() =>
        // The '–' side of the same determination (r1: "00 through 12"). No host zone reaches -14:00, but the
        // pin does — so the departure is symmetric and the documentation must name both signs.
        AssertCurrentDateUnderClock("DDCDM14", "2026-08-31T10:20:30.45-14:00", "2026083110203045-1400");

    [Fact]
    public void CurrentDate_InRangeOffsets_ConformOutright_PinnedToSpec()
    {
        // The determination is NOT a licence to ignore the layout: every offset the rule admits is rendered
        // exactly as r1 states, including the 45-minute case in positions 20-21 and the UTC boundary that r1
        // assigns to '+' ("the same as or ahead of Coordinated Universal time"), not to '0' or '–'.
        AssertCurrentDateUnderClock("DDCDP13", "2026-08-31T10:20:30.45+13:00", "2026083110203045+1300");
        AssertCurrentDateUnderClock("DDCDK45", "2026-08-31T10:20:30.45+05:45", "2026083110203045+0545");
        AssertCurrentDateUnderClock("DDCDUTC", "2026-08-31T10:20:30.45+00:00", "2026083110203045+0000");
        AssertCurrentDateUnderClock("DDCDM93", "2026-08-31T10:20:30.45-09:30", "2026083110203045-0930");
    }
}
