// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The REPORTED side of the <c>&gt;&gt;LEAP-SECOND</c> directive at RUN time (ISO/IEC 1989:2023 §7.3.17.4 GR2 / GR3,
/// Annex A.1 item 111), pinned at the one instant where a reported 60 could arise: the last representable tick before
/// the 2016-12-31 leap second, 23:59:59.9999999, through the <c>COBOLNET_CLOCK</c> seam.
///
/// <para>GR3 — "When OFF is specified or implied, a value greater than 59 shall not be reported in the seconds
/// position of the value returned from" ACCEPT … TIME, CURRENT-DATE, FORMATTED-CURRENT-DATE and WHEN-COMPILED
/// (cite.py --check 7.3.17.4 → OK 3)); GR1 makes OFF the implied state with no directive (→ OK 1)). GR2 — "When ON is
/// specified or implied, the implementor defines whether a value greater than 59 may be reported" (→ OK 2)); ON is
/// IMPLIED by a bare <c>&gt;&gt;LEAP-SECOND</c>, because §7.3.17.2 prints ON without an underline and §5.2.3 optional
/// words are those "shown in uppercase and not underlined in general formats" (cite.py --check 5.2.3 → OK). A.1 item
/// 111 requires the determination and its maximum (cite.py --check A.1 → OK 111)); docs/CONFORMANCE.md DOC-A.1-111
/// answers NO, maximum 59, on every branch. So every arm below — ON specified, ON implied, OFF specified, OFF implied —
/// has the same observable: second 59, never 60.</para>
///
/// <para>Expected values are derived from the layout rules applied to the pinned instant, never copied from a run:
/// ACCEPT TIME is HHMMSShh (§14.9.1.4 11)) → hours 23, minutes 59, seconds 59, and "hundredths of a second past the
/// second, in the range 00 through 99" → 0.9999999 s past the second is 99 completed hundredths; CURRENT-DATE is the
/// §15.21.3 1) 21-character layout with the same time fields and a zero UTC offset rendered '+' ("the same as or ahead
/// of Coordinated Universal time") "00" "00"; FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss") is the basic combined format
/// (§15.3.3.7, §15.38.4 1)). A renderer that ROUNDED the sub-second tail, or carried it into the seconds field under ON
/// (where §15.3.3.3 lets a formatted-time seconds subfield ARGUMENT be 60 — the side pb65_leap_second_on pins), would
/// report 23:59:60 or roll to the next day; the determination says it never does.</para>
///
/// <para>The fourth source, WHEN-COMPILED, is read by the COMPILER, not by the run unit this pin reaches; its pin is
/// the compile-clock facts <c>WhenCompiledStampTests.WhenCompiled_LeapSecond*</c> in the Unit assembly, which set
/// <c>IntrinsicBinder.CompileClock</c> to the same instant. The live-clock goldens l1g4_leap_second_on_reported_seconds,
/// l1g4_leap_second_off_reported_seconds (2023) and l1g4_leap_second_off_explicit_2002 are LAYOUT witnesses only: on a
/// live clock they cannot fail on a seconds value above 59.</para>
///
/// <para>Editions: the directive's floor is 2002 (constructs.json leap-second-directive-2002). FORMATTED-CURRENT-DATE
/// is a 2014 intrinsic (IntrinsicCatalog), so the 2002 arms read ACCEPT TIME and CURRENT-DATE only.</para>
/// </summary>
public sealed class LeapSecondReportedSecondsPinTests
{
    /// <summary>The last tick .NET can represent before the 2016-12-31 23:59:60 UTC leap second.</summary>
    private const string LastTickBeforeLeapSecond = "2016-12-31T23:59:59.9999999+00:00";

    /// <summary>The 2002 run-time sources, derived as the class summary states: TIME, then CURRENT-DATE.</summary>
    private const string Expected2002 = "23595999\n2016123123595999+0000";

    /// <summary>The 2014+ run-time sources: the 2002 pair, then FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss").</summary>
    private const string Expected2023 = Expected2002 + "\n20161231T235959";

    /// <summary>Compile at <paramref name="dialectLevel"/> (free form, so the directive line may start in column 1;
    /// free form is selectable at every edition) and run with the run unit's clock pinned to
    /// <see cref="LastTickBeforeLeapSecond"/>. At 2002 the FORMATTED-CURRENT-DATE lines are left out.</summary>
    private static void AssertReportedSeconds(string programId, string directiveLine, int dialectLevel = 2023)
    {
        bool formatted = dialectLevel >= 2014;
        string formattedLines = formatted
            ? """
                  MOVE FUNCTION FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss") TO FCD
                  DISPLAY FCD
              """
            : "";
        string dir = CutRunner.NewTempDir("ls60");
        try
        {
            string src = Path.Combine(dir, programId + ".cob");
            string dll = Path.Combine(dir, programId + ".dll");
            src = CompiledProgramCache.StageSource(src, $"""
                {directiveLine}
                IDENTIFICATION DIVISION.
                PROGRAM-ID. {programId}.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 TM  PIC X(8).
                01 CDT PIC X(21).
                01 FCD PIC X(15).
                PROCEDURE DIVISION.
                MAIN.
                    ACCEPT TM FROM TIME
                    DISPLAY TM
                    MOVE FUNCTION CURRENT-DATE TO CDT
                    DISPLAY CDT
                {formattedLines}
                    STOP RUN.
                """);
            var compiled = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel, SourceFormat: InitialReferenceFormat.Free));
            Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

            var (ok, stdout, detail) = CutRunner.Run(dll, dir, null,
                new Dictionary<string, string?> { [CobolNet.Runtime.IO.SystemClock.PinVariable] = LastTickBeforeLeapSecond });
            Assert.True(ok, detail);
            Assert.Equal(formatted ? Expected2023 : Expected2002, stdout);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    [Fact]
    public void ReportedSeconds_On_LastTickBeforeLeapSecond_NeverExceeds59() =>
        // GR2 + A.1 item 111, ON SPECIFIED: the determination is "never > 59, maximum 59" — so even under ON, at the
        // tick before the leap second, every source reports second 59 and hundredths 99.
        AssertReportedSeconds("L1G4PON", ">>LEAP-SECOND ON");

    [Fact]
    public void ReportedSeconds_ImpliedOn_BareDirective_LastTickBeforeLeapSecond_NeverExceeds59() =>
        // GR2's "or implied" arm: a bare >>LEAP-SECOND selects the un-underlined ON (§7.3.17.2, §5.2.3).
        AssertReportedSeconds("L1G4PIN", ">>LEAP-SECOND");

    [Fact]
    public void ReportedSeconds_On_2002_LastTickBeforeLeapSecond_NeverExceeds59() =>
        // GR2 at the directive's 2002 floor: ACCEPT TIME and CURRENT-DATE (FORMATTED-CURRENT-DATE is 2014+).
        AssertReportedSeconds("L1G4P2N", ">>LEAP-SECOND ON", dialectLevel: 2002);

    [Fact]
    public void ReportedSeconds_ImpliedOff_LastTickBeforeLeapSecond_NeverExceeds59() =>
        // GR3 with GR1's implied OFF (no directive): "shall not be reported" — the same observable, now required.
        AssertReportedSeconds("L1G4POF", "");

    [Fact]
    public void ReportedSeconds_SpecifiedOff_LastTickBeforeLeapSecond_NeverExceeds59() =>
        // GR3's "specified" arm: >>LEAP-SECOND OFF written.
        AssertReportedSeconds("L1G4PSF", ">>LEAP-SECOND OFF");

    [Fact]
    public void ReportedSeconds_SpecifiedOff_2002_LastTickBeforeLeapSecond_NeverExceeds59() =>
        // GR3 specified OFF at the directive's 2002 floor: ACCEPT TIME and CURRENT-DATE.
        AssertReportedSeconds("L1G4P2F", ">>LEAP-SECOND OFF", dialectLevel: 2002);
}
