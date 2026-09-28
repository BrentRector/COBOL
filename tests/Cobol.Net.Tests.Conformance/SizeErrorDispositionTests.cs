// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Compiler;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB75 — a size error raised OUTSIDE an arithmetic statement's own catch is the fatal exception condition
/// ISO §14.7.5's no-phrase rules name ("EC-SIZE-OVERFLOW … is set to exist, and processing proceeds as specified in
/// 14.6.13.1.3"), never a raw CLR crash. The golden <c>pb75_sdidi_overflow_outside_arithmetic</c> pins #4/#5 (USE /
/// PERFORM WHEN dispatch with RESUME); this class pins the two TERMINATING dispositions: #7 (checking enabled, no
/// handler resumes) and #8 (checking NOT enabled — the implementor's choice, documented as loud termination). Before
/// this landing every one of these was an unhandled <c>CobolSizeError</c> stack trace, exit 127.
/// </summary>
public sealed class SizeErrorDispositionTests
{
    private static string Prog(string turn, string body) => $$"""
        {{turn}}
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB75DISP.
               OPTIONS.
                   ARITHMETIC IS STANDARD-DECIMAL.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-X PIC 9(5).
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    private static (int Exit, string Stdout, string Stderr) Run(string source)
    {
        string dir = CutRunner.NewTempDir("pb75");
        try
        {
            string src = Path.Combine(dir, "prog.cob"), dll = Path.Combine(dir, "prog.dll");
            src = CompiledProgramCache.StageSource(src, source);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "[compile] " + string.Join("\n", r.Errors));
            return CutRunner.RunExit(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The NATIVE-arithmetic twin (kb/Work PB639): the size error is on the RESULT's transfer into the
    /// receiver (§14.7.5 case 3), not on an intermediate, so the receiver is a fixed-point picture the aligned
    /// value cannot hold rather than a decimal128 escape.</summary>
    private static string ProgNative(string turn, string body) => $$"""
        {{turn}}
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB639DISP.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WC PIC S9(9)V9(9) VALUE 7.
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    /// <summary>kb/Work PB639 — §14.7.5 NO-PHRASE RULE 4 over the RESULT: "if the result of the arithmetic
    /// statement is a value further from zero than permitted for the associated resultant data item, the
    /// EC-SIZE-TRUNCATION exception condition is set to exist", disposed by §14.6.13.1.3 #7 (checking enabled,
    /// nothing resumes) — the run unit terminates abnormally NAMING that condition, never EC-SIZE-OVERFLOW (which
    /// is the INTERMEDIATE's condition, no-phrase rule 3) and never silence.
    /// <para>⛔ The second row is the discriminator. 340282366920938463463374607432 is a legal 30-digit literal
    /// (§8.3.3.3.2) equal to ceil(2^128 / 10^9), so aligning it at the receiver's scale 9 WRAPS to 231788544 —
    /// well inside <c>PIC S9(9)V9(9)</c>. The store's capacity check looked at that wrap, found it fitting, and
    /// the program ran to "AFTER" with no exception at all: an exception condition the source asked to be told
    /// about, silently absent. Both rows are checked so a fix that only handles the obvious magnitude is
    /// visible.</para></summary>
    [Theory]
    [InlineData("COMPUTE WC = 1000000000000000000000000000000.")]
    [InlineData("COMPUTE WC = 340282366920938463463374607432.")]
    [InlineData("MULTIPLY 1000000000000000000000 BY 1000000000 GIVING WC.")]
    public void NoPhraseResultOverflow_UnderChecking_TerminatesNamingEcSizeTruncation(string body)
    {
        var (exit, stdout, stderr) = Run(ProgNative(">>TURN EC-SIZE CHECKING ON", body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("EC-SIZE-TRUNCATION", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>The same two results with checking NOT enabled: §14.6.13.1.3 #8 hands the disposition to the
    /// implementor and CONFORMANCE.md DOC-A.1-70 documents it as "execution continues and the resultant
    /// identifier receives the LOW-ORDER digits of the result aligned at its scale" — so the program MUST reach
    /// "AFTER" and exit 0. This is the other half of the same rule: rule 4 sets the condition in both cases; only
    /// the disposition differs. (The stored digits themselves are pinned by the golden
    /// <c>pb639_store_landing_low_order_digits</c>.)</summary>
    [Theory]
    [InlineData("COMPUTE WC = 1000000000000000000000000000000.")]
    [InlineData("COMPUTE WC = 340282366920938463463374607432.")]
    public void NoPhraseResultOverflow_CheckingOff_ContinuesPerTheDocumentedDetermination(string body)
    {
        var (exit, stdout, stderr) = Run(ProgNative("", body));
        Assert.Equal(0, exit);
        Assert.Contains("AFTER", stdout);
        Assert.DoesNotContain("EC-SIZE", stderr);
    }

    /// <summary>§14.6.13.1.3 #8 — checking NOT enabled: the SDIDI range overflow in a CONDITION reaches the run-unit
    /// boundary and terminates loudly (this implementation's documented choice) — the abnormal-termination surface
    /// names the condition, exit 1; never a .NET stack trace / exit 127.</summary>
    [Theory]
    [InlineData("IF 10 ** 100000 > 5 DISPLAY \"GT\" ELSE DISPLAY \"LE\" END-IF.")]
    [InlineData("DISPLAY \"V=\" FUNCTION ABS(10 ** 100000).")]
    [InlineData("MOVE FUNCTION INTEGER-PART(10 ** 100000) TO WS-X.")]
    [InlineData("COMPUTE WS-X = 10 ** 100000.")]
    public void CheckingOff_TerminatesLoudly_NeverARawCrash(string body)
    {
        var (exit, stdout, stderr) = Run(Prog("", body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal)", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>kb/Work PB1605 — the ZERO DIVISOR over every value lane and every divide shape. §14.7.5 case 2 is the
    /// size error condition wherever a divisor is zero, and no-phrase rule 2 sets EC-SIZE-ZERO-DIVIDE; with checking
    /// not enabled §14.6.13.1.3 item 8 hands the disposition to the implementor, and CONFORMANCE.md DOC-A.1-70's is
    /// that a quotient which cannot be formed has no value to store: the run unit terminates abnormally naming the
    /// condition, exit 1. <c>ARITHMETIC</c> is the lane switch — native (the scaled Int128 carrier, and binary64
    /// wherever a FLOAT-LONG operand or receiver is present) or STANDARD-DECIMAL (the SDIDI intermediate, which
    /// alone terminated before PB1605). The scaled rows stored 000 and ran on; the binary64 rows stored +Infinity
    /// (FR), and the float-to-fixed row stored 000.</summary>
    [Theory]
    [InlineData("NATIVE", "COMPUTE A = B / Z.")]
    [InlineData("NATIVE", "COMPUTE A = 1 + B / Z.")]                  // a nested (non-final) quotient
    [InlineData("NATIVE", "DIVIDE Z INTO A.")]
    [InlineData("NATIVE", "DIVIDE B BY Z GIVING A REMAINDER R.")]
    [InlineData("NATIVE", "IF B / Z > 1 DISPLAY \"GT\" END-IF.")]     // receiverless: a condition
    [InlineData("NATIVE", "COMPUTE FR = FL / FZ.")]
    [InlineData("NATIVE", "DIVIDE FZ INTO FR.")]
    [InlineData("NATIVE", "COMPUTE A = FL / FZ.")]
    [InlineData("NATIVE", "IF FL / FZ > 1 DISPLAY \"GT\" END-IF.")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = B / Z.")]
    public void ZeroDivisor_CheckingOff_TerminatesNamingEcSizeZeroDivide(string arithmetic, string body)
    {
        var (exit, stdout, stderr) = Run(ProgZeroDivisor(arithmetic, body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-ZERO-DIVIDE (fatal)", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>The same zero divisors under the SIZE ERROR phrase: §14.7.5 phrase rule 1 leaves every resultant
    /// unchanged and rule 3 runs the phrase, then execution continues — the unchecked kernels' raise must not
    /// change the checked path. A (VALUE 7) and FR (VALUE 9) are displayed as their initial values.</summary>
    [Theory]
    [InlineData("NATIVE", "COMPUTE A = B / Z ON SIZE ERROR DISPLAY \"SE\" END-COMPUTE.")]
    [InlineData("NATIVE", "COMPUTE FR = FL / FZ ON SIZE ERROR DISPLAY \"SE\" END-COMPUTE.")]
    [InlineData("STANDARD-DECIMAL", "COMPUTE A = B / Z ON SIZE ERROR DISPLAY \"SE\" END-COMPUTE.")]
    public void ZeroDivisor_UnderThePhrase_LeavesTheResultantsAndContinues(string arithmetic, string body)
    {
        var (exit, stdout, _) = Run(ProgZeroDivisor(arithmetic, body +" DISPLAY \"A=\" A \" FR=\" FR."));
        Assert.Equal(0, exit);
        Assert.Contains("SE", stdout);
        Assert.Contains("A=007 FR=9", stdout);
        Assert.Contains("AFTER", stdout);
    }

    private static string ProgZeroDivisor(string arithmetic, string body) => $$"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB1605ZD.
               OPTIONS.
                   ARITHMETIC IS {{arithmetic}}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 B  PIC 9 VALUE 5.
               01 Z  PIC 9 VALUE 0.
               01 A  PIC 9(3) VALUE 7.
               01 R  PIC 9(3) VALUE 9.
               01 FL USAGE FLOAT-LONG VALUE 5.
               01 FZ USAGE FLOAT-LONG VALUE 0.
               01 FR USAGE FLOAT-LONG VALUE 9.
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    /// <summary>§14.6.13.1.3 #7 — checking enabled and nothing resumes: the guarded statement sets the status,
    /// finds no USE / WHEN, and the run unit terminates abnormally naming the condition.</summary>
    [Fact]
    public void CheckingOn_NoHandler_TerminatesAbnormally()
    {
        var (exit, stdout, stderr) = Run(Prog(">>TURN EC-SIZE-OVERFLOW CHECKING ON",
            "IF 10 ** 100000 > 5 DISPLAY \"GT\" ELSE DISPLAY \"LE\" END-IF."));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("EC-SIZE-OVERFLOW", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
    }

    /// <summary>ONE dispatch per raise (§14.6.13.1.3 #5 → #7): a fatal condition raised by a statement INSIDE a
    /// PERFORM is processed by that statement's guard — the USE declarative runs once — and, unresumed, terminates
    /// the run unit; the enclosing PERFORM's guard lets the already-dispatched condition pass. Before this landing
    /// every enclosing statement re-dispatched it: the declarative ran once per nesting level.</summary>
    [Fact]
    public void FatalRaiseInsideAPerform_IsDispatchedOnce_ThenTerminates()
    {
        string src = """
                  >>TURN EC-BOUND-REF-MOD CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB75ONCE.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC X(5) VALUE "HELLO".
                   01 Y PIC X(2).
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   H SECTION.
                       USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
                   H-P.
                       DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       DISPLAY "R1".
                       PERFORM 2 TIMES
                           MOVE X(9:1) TO Y
                           DISPLAY "IN-LOOP"
                       END-PERFORM.
                       DISPLAY "R2".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = Run(src);
        Assert.Equal(1, exit);
        Assert.Equal(1, stdout.Split("CAUGHT=").Length - 1);   // exactly ONE dispatch
        Assert.DoesNotContain("IN-LOOP", stdout);
        Assert.Contains("abnormal run-unit termination: EC-BOUND-REF-MOD (fatal)", stderr);
    }

    /// <summary>The native carrier's receiverless-lane raise (§8.8.1 alignment past the Int128 escape boundary —
    /// <c>CobolNum.RescaleEscape</c>, kb/Work PB69's "stays loud") is the same condition and takes the same
    /// disposition: a subscript expression whose alignment overflows terminates loudly, not with a stack trace.</summary>
    [Fact]
    public void NativeReceiverlessOverflow_TerminatesLoudly()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB75NAT.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 BIG PIC 9(31) VALUE 9999999999999999999999999999999.
                   01 T.
                      05 E PIC X OCCURS 3.
                   01 F PIC 9V9(9) VALUE 1.5.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       DISPLAY "BEFORE".
                       DISPLAY FUNCTION MAX(BIG F).
                       DISPLAY "AFTER".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = Run(src);
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal)", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
    }
}
