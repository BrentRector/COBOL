// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The CALL exception-phrase spelling × edition matrix (VERSION_TEST_MATRIX introduction/continuity invariants;
/// VERSION_CHANGE_REFERENCE row 3). <c>[NOT] ON EXCEPTION</c> is ANSI X3.23-1985 surface (CALL Format 2 —
/// CCVS-85 IC222A exercises both phrases: "'ON OVERFLOW' CAN BE USED IN PLACE OF 'ON EXCEPTION'"), valid at
/// EVERY edition; <c>ON OVERFLOW</c> is the COBOL-74-carried synonym, valid 85–2014 and REMOVED at 2023
/// (ISO/IEC 1989:2023 Annex E.2 item 1c — "ON OVERFLOW phrase of the CALL statement … ON EXCEPTION gives the
/// same result"). The pre-fix COBOLNET0881 gate (ON EXCEPTION rejected below 2002) was a mis-derived
/// introduction edge — no VERSION_CHANGE_REFERENCE row records a 2002 introduction.
/// </summary>
public sealed class CallExceptionPhraseEditionTests
{
    private static string CallWith(string phrase) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CXPED1.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-F PIC X VALUE "P".
        PROCEDURE DIVISION.
        MAIN-P.
            CALL "CXPED1S"
                {phrase}
            END-CALL.
            STOP RUN.
        END PROGRAM CXPED1.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CXPED1S.
        PROCEDURE DIVISION.
        SUB-P.
            EXIT PROGRAM.
        """;

    /// <summary>ON EXCEPTION compiles at every edition (X3.23-1985 CALL Format 2 → ISO §14.9.4 — a continuity
    /// invariant; the construct was never introduced later nor removed).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void OnException_CompilesAtEveryEdition(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(CallWith("ON EXCEPTION MOVE \"F\" TO WS-F"), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", diags)}");
    }

    /// <summary>NOT ON EXCEPTION compiles at every edition (X3.23-1985 CALL Format 2 carries the NOT phrase —
    /// CCVS-85 IC222A tests it explicitly).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void NotOnException_CompilesAtEveryEdition(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(CallWith("NOT ON EXCEPTION MOVE \"N\" TO WS-F"), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", diags)}");
    }

    /// <summary>Both phrases together compile at every edition (the IC222A combined shape).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void OnAndNotOnException_CompileAtEveryEdition(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(
            CallWith("ON EXCEPTION MOVE \"F\" TO WS-F NOT ON EXCEPTION MOVE \"N\" TO WS-F"), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", diags)}");
    }

    /// <summary>ON OVERFLOW compiles at 85/2002/2014 (the 74-carried synonym, still legal through 2014).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void OnOverflow_CompilesThrough2014(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(CallWith("ON OVERFLOW MOVE \"F\" TO WS-F"), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", diags)}");
    }

    /// <summary>ON OVERFLOW is REMOVED at 2023 (Annex E.2 item 1c; VERSION_CHANGE_REFERENCE row 3) — rejected
    /// with the targeted COBOLNET0882 diagnostic naming the removal and the fix.</summary>
    [Fact]
    public void OnOverflow_Rejected0882At2023()
    {
        var (ok, diags) = EditionHarness.Compile(CallWith("ON OVERFLOW MOVE \"F\" TO WS-F"), 2023);
        Assert.False(ok, "CALL … ON OVERFLOW must be rejected at --std 2023 (Annex E.2 item 1c)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0882");
    }
}
