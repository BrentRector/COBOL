// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.CodeGen;
using CobolNet.CodeGen.Emit;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// CONTINUE AFTER's runtime lanes (ISO §14.9.9.4 GR1; kb/Work PB138, PB1529) — the legs no stdout golden can pin:
/// the NON-FINITE screen (`(long)double.NaN` saturates to 0, so a NaN interval used to silently skip the
/// suspension where §14.6.13.2 item 3 names EC-DATA-NOT-FINITE), the raise REPORT the emitted §14.6.13.1.4
/// dispatch consumes, the exact truncation contract, and the saturation of a huge interval to the maximum
/// meaningful value on EVERY carrier the interval can evaluate on. A suspension is OBSERVED through
/// <see cref="CobolTiming.SuspensionObserver"/>, never timed (kb/Work PB1590: no test asserts a wall clock, and a
/// 86,400-second pause is the very thing under test).
/// </summary>
public sealed class CobolTimingTests
{
    private const int MaxMs = (int)(CobolTiming.MaxSeconds * 1000);

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
    public void ScaledLane_TruncatedZero_DoesNotSuspend_EvenWhenTheDoubleImageIsOne()
    {
        // 0.99999999999999999 (17 nines) converts to exactly 1.0 in binary64, while the exact truncation, taken from
        // the unscaled value in its own domain, is 0 (GR1's implicit COMPUTE without ROUNDED). The suspension must
        // follow the EXACT value.
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter((Int128)99_999_999_999_999_999, 17, checkLessThanZero: true)));
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

    /// <summary>§14.9.9.4 GR1 / kb/Work PB1529 — an interval above the maximum meaningful value suspends for exactly
    /// the maximum, however large it is: the double is compared with the maximum BEFORE it is narrowed, so 1E30 (past
    /// the long range) and 9.3E18 (just past it) no longer wrap to a negative count and suspend 0 s.</summary>
    [Theory]
    [InlineData(1e30)]
    [InlineData(9.3e18)]
    [InlineData(86400.5)]
    [InlineData(86400.0)]
    [InlineData(double.MaxValue)]
    public void HugeInterval_SuspendsForTheMaximumMeaningfulValue(double seconds)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(seconds, checkLessThanZero: false)));
        Assert.Equal([MaxMs], suspensions);
    }

    /// <summary>The SCALED lane (kb/Work PB1529): a value at or past 2^63 saturates instead of wrapping — 2^64 used to
    /// suspend 0 s and 2^64 + 3 suspended 3 s — at every scale, including the trailing-P negative scale (a stored
    /// 1 at scale -30 is 10^30).</summary>
    [Theory]
    [InlineData("18446744073709551616", 0)]        // 2^64
    [InlineData("18446744073709551619", 0)]        // 2^64 + 3
    [InlineData("9223372036854775808", 0)]         // 2^63, the first value an unchecked (long) cast turns negative
    [InlineData("99999999999999999999999999999999999999", 0)]   // 38 nines
    [InlineData("170141183460469231731687303715884105727", 0)] // Int128.MaxValue
    [InlineData("99999999999999999999999999999999999999", 20)]  // 10^18 seconds at scale 20
    [InlineData("1", -30)]                                       // trailing P: 10^30
    [InlineData("86401", 0)]
    public void ScaledLane_HugeInterval_SuspendsForTheMaximum(string unscaled, int scale)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(Int128.Parse(unscaled), scale, checkLessThanZero: false)));
        Assert.Equal([MaxMs], suspensions);
    }

    [Theory]
    [InlineData("2900", 3, 2000)]            // 2.900 truncates toward zero
    [InlineData("86400", 0, MaxMs)]          // exactly the maximum
    [InlineData("2", -3, 2_000_000)]         // trailing P: 2 x 10^3 = 2000 s, under the maximum
    public void ScaledLane_InRange_SuspendsForItsTruncatedSeconds(string unscaled, int scale, int expectedMs)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(Int128.Parse(unscaled), scale, checkLessThanZero: false)));
        Assert.Equal([expectedMs], suspensions);
    }

    /// <summary>GR1a/GR1b on the scaled lane: the sign is the unscaled value's, so -0.5 (which truncates to 0 s) still
    /// raises the nonfatal condition when checked and does nothing when not.</summary>
    [Fact]
    public void ScaledLane_NegativeFraction_RaisesWhenChecked_AndNeverSuspends()
    {
        var suspensions = Observe(() =>
        {
            Assert.True(CobolTiming.ContinueAfter((Int128)(-5), 1, checkLessThanZero: true));
            Assert.False(CobolTiming.ContinueAfter((Int128)(-5), 1, checkLessThanZero: false));
            Assert.False(CobolTiming.ContinueAfter(Int128.MinValue, 0, checkLessThanZero: false));
        });
        Assert.Empty(suspensions);
    }

    /// <summary>The UNSIGNED-WIDE lane (kb/Work PB1529, R10): a 16-byte unsigned COMP-5 item at or above 2^127 used to
    /// reach the runtime through an <c>(Int128)</c> cast that is NEGATIVE there, so the largest possible interval
    /// suspended 0 s.</summary>
    [Theory]
    [InlineData("340282366920938463463374607431768211455", 0)]   // 2^128 - 1
    [InlineData("170141183460469231731687303715884105728", 0)]   // 2^127
    [InlineData("340282366920938463463374607431768211455", 5)]
    [InlineData("1", -30)]
    [InlineData("9223372036854775808", 0)]                       // 2^63
    public void UnsignedWideLane_HugeInterval_SuspendsForTheMaximum(string unscaled, int scale)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(UInt128.Parse(unscaled), scale, checkLessThanZero: true)));
        Assert.Equal([MaxMs], suspensions);
    }

    [Theory]
    [InlineData("2900", 3, 2000)]
    [InlineData("5", 1, 0)]                                      // 0.5 truncates to 0 s: no suspension
    public void UnsignedWideLane_InRange_SuspendsForItsTruncatedSeconds(string unscaled, int scale, int expectedMs)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(UInt128.Parse(unscaled), scale, checkLessThanZero: false)));
        Assert.Equal(expectedMs == 0 ? [] : new List<int> { expectedMs }, suspensions);
    }

    /// <summary>The SDIDI lane (kb/Work PB1529): <c>CobolDec.ToUnscaled</c> keeps only the low-order digits a store
    /// could use, so 10^40 landed as 0 and the program suspended 0 s. The interval is now read through the saturating
    /// <see cref="CobolNum.PositionOf(CobolDec)"/>: BIG * BIG (3.4 x 10^38), 10^40, 10^6000 and the largest 34-digit
    /// significand at the largest exponent are all above the maximum.</summary>
    [Theory]
    [InlineData("1", 40)]
    [InlineData("1", 6000)]
    [InlineData("34028236692093846346337460743176821145", 1)]    // 3.4 x 10^38, the order of BIG * BIG for BIG = 2^64
    [InlineData("9999999999999999999999999999999999", 6111)]
    [InlineData("864001", -1)]                                    // 86400.1
    public void SdidiLane_HugeInterval_SuspendsForTheMaximum(string sig, int exp)
    {
        var suspensions = Observe(() => Assert.False(CobolTiming.ContinueAfter(new CobolDec(Int128.Parse(sig), exp), checkLessThanZero: false)));
        Assert.Equal([MaxMs], suspensions);
    }

    [Fact]
    public void SdidiLane_InRange_TruncatesInItsOwnDomain_AndNegativesRaise()
    {
        var suspensions = Observe(() =>
        {
            Assert.False(CobolTiming.ContinueAfter(new CobolDec(29, -1), checkLessThanZero: true));              // 2.9 -> 2 s
            Assert.False(CobolTiming.ContinueAfter(new CobolDec(999, -3), checkLessThanZero: true));             // 0.999 -> 0 s
            Assert.False(CobolTiming.ContinueAfter(new CobolDec(0, 0), checkLessThanZero: true));
            Assert.True(CobolTiming.ContinueAfter(new CobolDec(-5, -1), checkLessThanZero: true));               // -0.5: GR1b
            Assert.False(CobolTiming.ContinueAfter(new CobolDec(-1, 40), checkLessThanZero: false));             // -10^40: GR1a, no suspension
        });
        Assert.Equal([2000], suspensions);
    }

    /// <summary>⛔ THE EMITTED CALL IS TOTAL OVER EVERY CARRIER A NumX CAN BE (kb/Work PB1529; the
    /// <c>IntegerIntake</c> sibling is <c>IntrinsicCarrierAgreementDriftTests</c>). The carrier list is NOT kept here:
    /// <see cref="NumXCarrier"/> is the compiler's own enumeration, so a fifth carrier fails THIS test (the emitter
    /// throws on a carrier it does not map) before a user meets it. Each carrier must reach the runtime overload of
    /// its own domain, and none may be narrowed by the emitter.</summary>
    [Fact]
    public void ContinueAfterCall_IsTotalOverTheNumXCarriers_AndNarrowsNothing()
    {
        foreach (var carrier in Enum.GetValues<NumXCarrier>())
        {
            var x = carrier switch
            {
                NumXCarrier.Scaled => new NumX("e", 2),
                NumXCarrier.UnsignedWide => new NumX("u", 2, U: true),
                NumXCarrier.Sdidi => new NumX("d", 0, Dec: true),
                NumXCarrier.Binary64 => new NumX("r", 0, Real: true),
                _ => throw new InvalidOperationException($"the test builds no operand for the new carrier {carrier}"),
            };
            Assert.Equal(carrier, x.Carrier);
            string call = StatementEmitter.ContinueAfterCall(x, "false");
            string expected = carrier switch
            {
                NumXCarrier.Scaled => "CobolTiming.ContinueAfter((Int128)(e), 2, false)",
                NumXCarrier.UnsignedWide => "CobolTiming.ContinueAfter((UInt128)(u), 2, false)",
                NumXCarrier.Sdidi => "CobolTiming.ContinueAfter(d, false)",
                _ => "CobolTiming.ContinueAfter(r, false)",
            };
            Assert.Equal(expected, call);
            Assert.DoesNotContain("(long)", call);
            Assert.DoesNotContain("ToUnscaled", call);
            Assert.DoesNotContain("Position(", call);
        }
    }

    /// <summary>⛔ END TO END, NOT SLEEPING (kb/Work PB1529): a program whose intervals sit on every lane — a 2^64-second
    /// literal, a PIC 9(20) item, a PIC 9(31) item, a scaled item, a product the planner lifts to the SDIDI
    /// (BIG * BIG = 2^128), a 16-byte unsigned COMP-5 item set to 2^128 - 1 through its alphanumeric image — is compiled
    /// and ACTIVATED IN PROCESS with the suspension observer installed, so the test sees every pause the run unit
    /// would take. Each is above the maximum meaningful value (§14.9.9.4 GR1), so each is exactly 86,400 s; the last
    /// two statements are an in-range interval (3.9 s truncates to 3) and a below-maximum product, the control arm
    /// that says the observer was connected.</summary>
    [Fact]
    public void CompiledProgram_EveryLaneSuspendsForTheMaximum_NeverForTheWrappedValue()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1529E2E.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BIG   PIC 9(20) VALUE 18446744073709551616.
            01 HUGE  PIC 9(31) VALUE 9999999999999999999999999999999.
            01 SCL   PIC 9(22)V9(8) VALUE 9999999999999999999999.5.
            01 SM    PIC 9(2) VALUE 2.
            01 U     PIC 9(31) COMP-5.
            01 UX    REDEFINES U PIC X(16).
            01 SMALL PIC 9V9 VALUE 3.9.
            PROCEDURE DIVISION.
                MOVE HIGH-VALUES TO UX.
                CONTINUE AFTER 18446744073709551616 SECONDS.
                CONTINUE AFTER BIG SECONDS.
                CONTINUE AFTER HUGE SECONDS.
                CONTINUE AFTER SCL SECONDS.
                CONTINUE AFTER BIG * BIG SECONDS.
                CONTINUE AFTER HUGE * HUGE SECONDS.
                CONTINUE AFTER SM ** 70 SECONDS.
                CONTINUE AFTER U SECONDS.
                CONTINUE AFTER SMALL SECONDS.
                STOP RUN.
            """;
        var suspensions = RunObserved(source, "pb1529_e2e");
        Assert.Equal([MaxMs, MaxMs, MaxMs, MaxMs, MaxMs, MaxMs, MaxMs, MaxMs, 3000], suspensions);
    }

    /// <summary>The same program under STANDARD-DECIMAL arithmetic: every expression is lifted to the SDIDI, whose
    /// 10^40-class values used to land as zero.</summary>
    [Fact]
    public void CompiledProgram_StandardDecimal_EveryExpressionSuspendsForTheMaximum()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1529SDI.
            OPTIONS.
                ARITHMETIC IS STANDARD-DECIMAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BIG   PIC 9(20) VALUE 18446744073709551616.
            01 HUGE  PIC 9(31) VALUE 9999999999999999999999999999999.
            01 SM    PIC 9(2) VALUE 2.
            PROCEDURE DIVISION.
                CONTINUE AFTER BIG * BIG SECONDS.
                CONTINUE AFTER HUGE * HUGE * HUGE SECONDS.
                CONTINUE AFTER SM ** 70 SECONDS.
                CONTINUE AFTER 1.5 SECONDS.
                STOP RUN.
            """;
        Assert.Equal([MaxMs, MaxMs, MaxMs, 1000], RunObserved(source, "pb1529_sdi"));
    }

    /// <summary>Compile <paramref name="source"/> (COBOL-2023) and activate its program in this process, returning
    /// every suspension the run unit requested, in milliseconds. <paramref name="assemblyName"/> is unique per test: .NET
    /// serves a stale assembly of the same name. The generated program references the SAME
    /// <c>Cobol.Net.Runtime</c> this test does, so the ambient observer sees its <c>CONTINUE AFTER</c> calls.</summary>
    private static List<int> RunObserved(string source, string assemblyName)
    {
        string dir = Directory.CreateTempSubdirectory("pb1529e2e").FullName;
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, assemblyName + ".dll"), DialectLevel: 2023, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the program must compile: " + string.Join("; ", r.Errors));
            var assembly = Assembly.LoadFrom(Path.Combine(dir, assemblyName + ".dll"));
            Type program = assembly.GetTypes().Single(t => typeof(ICobolProgram).IsAssignableFrom(t) && !t.IsAbstract);
            var instance = (ICobolProgram)Activator.CreateInstance(program, nonPublic: true)!;
            return Observe(() =>
            {
                try { instance.Activate(); }
                catch (StopRun) { }
            });
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    /// <summary>Run <paramref name="act"/> with CONTINUE AFTER's suspension REPORTED instead of performed.</summary>
    private static List<int> Observe(Action act)
    {
        var seen = new List<int>();
        CobolTiming.SuspensionObserver.Value = seen.Add;
        try { act(); } finally { CobolTiming.SuspensionObserver.Value = null; }
        return seen;
    }
}
