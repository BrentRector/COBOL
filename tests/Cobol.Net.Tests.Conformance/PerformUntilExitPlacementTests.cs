// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §14.9.28.3 SR8 — "The UNTIL EXIT phrase shall not be specified in a PERFORM statement with or under a
/// PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST AFTER phrase" (kb/Work PB434;
/// <c>cite.py --check 14.9.28.3</c> → OK 8)). Every row's verdict is computed from that sentence and from
/// §14.9.28.4 GR1, "The range includes all statements that are executed as the result of a transfer of control in
/// the range of the PERFORM statement" — never from an oracle (GnuCOBOL 3.2.0 enforces neither half).
///
/// <para>Each REFUSED row asserts COBOLNET2954 exactly ONCE — one statement violates, so one report — which a
/// negative golden's substring match cannot. The rows vary the three arms: <b>with</b> (the statement itself writes
/// VARYING or a TEST phrase, including the superset EXIT after a varying-phrase's own UNTIL or an AFTER level's),
/// <b>under, lexically</b> (inside an inline PERFORM that writes one, at any depth), and <b>under, out of line</b>
/// (in a procedure such a PERFORM's range reaches through an out-of-line PERFORM, a THRU range, a chain of
/// PERFORMs, an exception-checking PERFORM's handler, or a SORT input procedure). Each ADMITTED row asserts the
/// compile SUCCEEDS: a TIMES PERFORM and an until-phrase with no TEST phrase written do not bar UNTIL EXIT, because
/// SR1's ASSUMED TEST BEFORE cannot count (SR1 governs the until-phrase UNTIL EXIT itself is, so SR8 would then
/// forbid every UNTIL EXIT).</para>
/// </summary>
public sealed class PerformUntilExitPlacementTests
{
    private const string Sr8 = "COBOLNET2954";

    private const string Loop = "PERFORM UNTIL EXIT\n    ADD 1 TO W-N\n    EXIT PERFORM\nEND-PERFORM";

    [Theory]
    // ── with ──
    [InlineData("PBUX01", "PERFORM WITH TEST BEFORE UNTIL EXIT\n    ADD 1 TO W-N\n    EXIT PERFORM\nEND-PERFORM", "")]
    [InlineData("PBUX02", "PERFORM SUB-P TEST AFTER UNTIL EXIT", "SUB-P.\n    ADD 1 TO W-N.")]
    [InlineData("PBUX03", "PERFORM VARYING W-N FROM 1 BY 1 UNTIL EXIT\n    EXIT PERFORM\nEND-PERFORM", "")]
    [InlineData("PBUX04", "PERFORM SUB-P VARYING W-N FROM 1 BY 1 UNTIL W-N > 2\n    AFTER W-M FROM 1 BY 1 UNTIL EXIT",
        "SUB-P.\n    ADD 1 TO W-K.")]
    // ── under, lexically ──
    [InlineData("PBUX05", "PERFORM VARYING W-M FROM 1 BY 1 UNTIL W-M > 2\n    " + "PERFORM UNTIL EXIT\n        EXIT PERFORM\n    END-PERFORM\nEND-PERFORM", "")]
    [InlineData("PBUX06", "PERFORM WITH TEST AFTER UNTIL W-M > 2\n    ADD 1 TO W-M\n    PERFORM 2 TIMES\n        "
        + "PERFORM UNTIL EXIT\n            EXIT PERFORM\n        END-PERFORM\n    END-PERFORM\nEND-PERFORM", "")]
    [InlineData("PBUX07", "PERFORM VARYING W-M FROM 1 BY 1 UNTIL W-M > 2\n    PERFORM SUB-P UNTIL EXIT\nEND-PERFORM",
        "SUB-P.\n    STOP RUN.")]
    // ── under, out of line ──
    [InlineData("PBUX08", "PERFORM VARYING W-M FROM 1 BY 1 UNTIL W-M > 2\n    PERFORM SUB-P\nEND-PERFORM",
        "SUB-P.\n    " + Loop + ".")]
    [InlineData("PBUX09", "PERFORM SUB-P VARYING W-M FROM 1 BY 1 UNTIL W-M > 2", "SUB-P.\n    " + Loop + ".")]
    [InlineData("PBUX10", "PERFORM SUB-P WITH TEST AFTER UNTIL W-M > 2",
        "SUB-P.\n    ADD 1 TO W-M\n    PERFORM SUB-Q.\nSUB-Q.\n    " + Loop + ".")]
    [InlineData("PBUX11", "PERFORM SUB-P THRU SUB-R VARYING W-M FROM 1 BY 1 UNTIL W-M > 2",
        "SUB-P.\n    ADD 1 TO W-K.\nSUB-Q.\n    " + Loop + ".\nSUB-R.\n    ADD 1 TO W-K.")]
    [InlineData("PBUX12", "PERFORM VARYING W-M FROM 1 BY 1 UNTIL W-M > 2\n    PERFORM\n        ADD 1 TO W-N\n    "
        + "WHEN EC-SIZE\n        PERFORM SUB-P\n    END-PERFORM\nEND-PERFORM", "SUB-P.\n    " + Loop + ".")]
    public void UntilExit_WithOrUnderAVaryingOrTestPerform_IsRefusedOnce(string pid, string body, string paragraphs)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, body, paragraphs), 2023);
        Assert.False(ok, $"{pid}: §14.9.28.3 SR8 refuses this program, and it compiled");
        Assert.True(diagnostics.Count(d => d.Contains(Sr8, StringComparison.Ordinal)) == 1,
            $"{pid}: expected exactly one {Sr8}; got:\n{string.Join("\n", diagnostics)}");
    }

    /// <summary>The SORT arm of the out-of-line half: an INPUT PROCEDURE runs from the SORT statement and returns
    /// (§14.9.40.4 GR10), so a VARYING PERFORM of the paragraph holding the SORT puts the input procedure, and the
    /// UNTIL EXIT in it, in its range.</summary>
    [Fact]
    public void UntilExit_InASortInputProcedureUnderAVaryingPerform_IsRefusedOnce()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PBUX13.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SW ASSIGN TO "PBUX13.TMP".
            DATA DIVISION.
            FILE SECTION.
            SD SW.
            01 SW-REC PIC X(4).
            WORKING-STORAGE SECTION.
            01 W-M PIC 9(2) VALUE 0.
            01 W-N PIC 9(2) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM DO-SORT VARYING W-M FROM 1 BY 1 UNTIL W-M > 1.
                STOP RUN.
            DO-SORT.
                SORT SW ON ASCENDING KEY SW-REC INPUT PROCEDURE IS FEED OUTPUT PROCEDURE IS DRAIN.
            FEED.
                PERFORM UNTIL EXIT
                    ADD 1 TO W-N
                    EXIT PERFORM
                END-PERFORM.
            DRAIN.
                CONTINUE.

            """;
        var (ok, diagnostics) = EditionHarness.Compile(source, 2023);
        Assert.False(ok, "PBUX13: §14.9.28.3 SR8 refuses this program, and it compiled");
        Assert.True(diagnostics.Count(d => d.Contains(Sr8, StringComparison.Ordinal)) == 1,
            $"PBUX13: expected exactly one {Sr8}; got:\n{string.Join("\n", diagnostics)}");
    }

    [Theory]
    // A TIMES phrase is neither VARYING nor TEST.
    [InlineData("PBUA01", "PERFORM 2 TIMES\n    " + "PERFORM UNTIL EXIT\n        EXIT PERFORM\n    END-PERFORM\nEND-PERFORM", "")]
    // An until-phrase with NO TEST phrase written: SR1's assumed TEST BEFORE is not "specified".
    [InlineData("PBUA02", "PERFORM UNTIL W-M > 1\n    ADD 1 TO W-M\n    PERFORM UNTIL EXIT\n        EXIT PERFORM\n    END-PERFORM\nEND-PERFORM", "")]
    [InlineData("PBUA03", "PERFORM SUB-P 2 TIMES\n    PERFORM SUB-P UNTIL W-M > 1", "SUB-P.\n    ADD 1 TO W-M\n    " + Loop + ".")]
    // A VARYING PERFORM whose range does NOT reach the UNTIL EXIT paragraph — the text after it is not its range.
    [InlineData("PBUA04", "PERFORM SUB-P VARYING W-M FROM 1 BY 1 UNTIL W-M > 2\n    PERFORM SUB-Q",
        "SUB-P.\n    ADD 1 TO W-K.\nSUB-Q.\n    " + Loop + ".")]
    public void UntilExit_NotUnderAVaryingOrTestPerform_IsAccepted(string pid, string body, string paragraphs)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, body, paragraphs), 2023);
        Assert.True(ok, $"{pid}: §14.9.28.3 SR8 does not reach this program; got:\n{string.Join("\n", diagnostics)}");
    }

    /// <summary>Below 2023 there is no UNTIL EXIT phrase at all (§14.9.28.4 GR11 is a COBOL-2023 addition), so the
    /// superset EXIT after a varying-phrase's UNTIL draws the edition's COBOLNET0900 there, beside SR8.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void VaryingUntilExit_BelowTheIntroducingEdition_NamesTheEdition(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("PBUX14", "PERFORM VARYING W-N FROM 1 BY 1 UNTIL EXIT\n    EXIT PERFORM\nEND-PERFORM", ""), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOLNET0900");
    }

    private static string Program(string pid, string body, string paragraphs) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-K PIC 9(2) VALUE 0.
        01 W-M PIC 9(2) VALUE 0.
        01 W-N PIC 9(2) VALUE 0.
        PROCEDURE DIVISION.
        MAIN-PARA.
            {body.Replace("\n", "\n    ")}.
            STOP RUN.
        {paragraphs}

        """;
}
