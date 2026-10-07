// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Reference-modification range checking (ISO/IEC 1989:2023 §8.4.3.3.4 item 5c; review C14 + V48). Item 5c: a
/// SPECIFIED length "shall result in a positive nonzero integer, unless the REF-MOD-ZERO-LENGTH directive is set to
/// ON, when the result may also be zero" — so a negative specified length is out of range and, under
/// <c>&gt;&gt;TURN EC-BOUND-REF-MOD CHECKING ON</c>, raises the fatal EC-BOUND-REF-MOD, while the REF-MOD-ZERO-LENGTH
/// directive relaxes ONLY the zero case. Before C14 the omitted-length "to the end" form and a specified negative
/// shared the −1 sentinel, so a negative could never raise. V48: an internal OCCURS-DEPENDING zero-extent group
/// receive is a no-op, never a ref-mod violation.
/// </summary>
public sealed class RefModRangeTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    private static string Prog(string directives, string ws, string proc) => $"""
        {directives}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RMR.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W PIC X(6) VALUE "ABCDEF".
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    [Fact]   // §8.4.3.3.4 item 5c: a specified negative length under EC-BOUND-REF-MOD checking raises the fatal EC.
    public void SpecifiedNegativeLength_UnderChecking_RaisesFatal()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog(
            ">>TURN EC-BOUND-REF-MOD CHECKING ON",
            "01 L PIC S9(4) VALUE -1.",
            """
                DISPLAY "BEFORE"
                DISPLAY W(2:L)
                DISPLAY "AFTER"
            """));
        Assert.False(ok, $"expected a fatal EC-BOUND-REF-MOD; ran clean:\n{stdout}");
        Assert.Contains("EC-BOUND-REF-MOD", detail);
        Assert.Equal("BEFORE", stdout);   // the display after the bad ref-mod never runs
    }

    [Fact]   // The REF-MOD-ZERO-LENGTH directive relaxes ONLY the zero case — a negative length STILL raises.
    public void SpecifiedNegativeLength_UnderRefModZeroLength_StillRaisesFatal()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog(
            ">>TURN EC-BOUND-REF-MOD CHECKING ON\n>>REF-MOD-ZERO-LENGTH ON",
            "01 L PIC S9(4) VALUE -1.",
            """
                DISPLAY "BEFORE"
                DISPLAY W(2:L)
                DISPLAY "AFTER"
            """));
        Assert.False(ok, $"expected a fatal EC-BOUND-REF-MOD (the directive relaxes only zero); ran clean:\n{stdout}");
        Assert.Contains("EC-BOUND-REF-MOD", detail);
        Assert.Equal("BEFORE", stdout);
    }

    [Fact]   // The OMITTED-length "to the end" form (identifier(start:)) is not a specified length — it never raises,
             // even under checking; it reads from the leftmost to the end of the item.
    public void OmittedLength_ToEnd_DoesNotRaise()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog(
            ">>TURN EC-BOUND-REF-MOD CHECKING ON",
            "",
            """
                DISPLAY "[" W(2:) "]"
                DISPLAY "[" W(3:2) "]"
                DISPLAY "DONE"
            """));
        Assert.True(ok, detail);
        Assert.Equal("[BCDEF]\n[CD]\nDONE", stdout);
    }

    [Fact]   // V48: an OCCURS 0 TO n DEPENDING group at count 0 is a zero-extent receive — a plain group MOVE is a
             // no-op (§13.18.38 GR8a), NOT a reference-modification violation, so it must not raise under checking.
    public void OdoGroup_ZeroExtentReceive_DoesNotRaise()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Prog(
            ">>TURN EC-BOUND-REF-MOD CHECKING ON",
            """
                01 CNT PIC 9 VALUE 0.
                01 G.
                   05 T OCCURS 0 TO 5 DEPENDING ON CNT PIC X.
            """,
            """
                DISPLAY "BEFORE"
                MOVE "ZZZZZ" TO G
                DISPLAY "AFTER"
            """));
        Assert.True(ok, $"a zero-extent ODO group MOVE must not raise EC-BOUND-REF-MOD (V48):\n{detail}");
        Assert.Equal("BEFORE\nAFTER", stdout);
    }
}
