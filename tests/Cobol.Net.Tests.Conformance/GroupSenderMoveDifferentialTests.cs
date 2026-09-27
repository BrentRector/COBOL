// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// MOVE with a GROUP sender into an ELEMENTARY receiver — a GROUP MOVE (ISO/IEC 1989:2023 §14.9.25.4 GR4: a move
/// is elementary only when the sender is a literal or elementary item AND the receiver is elementary; "Any move
/// that is not an elementary move … is treated exactly as if it were an alphanumeric to alphanumeric elementary
/// move, except that there is no conversion of data from one form of internal representation to another. In such
/// a move, the receiving area will be filled without consideration for the individual elementary or group items").
/// Alignment is §14.6.8 (left-justified, right space-fill / right truncation). Version-invariant — the identical
/// rule is ANSI X3.23-1985 VI-102 6.18.2 (the rule the NC105A CCVS FAIL rows cite), so the NIST-green legacy is a
/// sound oracle; the receiver-JUSTIFIED case is spec-pinned (no NIST coverage). The five differential shapes
/// mirror NC105A MOVE-TEST-F1-16/-17/-20/-36/-38, each through a different mis-routable ConvertSource arm.
/// </summary>
public sealed class GroupSenderMoveDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static void AssertSpecPinned(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(expected, cout);
    }

    /// <summary>The NC105A sending group: image "123ABC" (a numeric leaf + an alphabetic leaf).</summary>
    private static string Program(string receivers, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. GSMOV1.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP-SEND.
           02 GS-NUM PIC 999 VALUE 123.
           02 GS-ALP PIC AAA VALUE "ABC".
        {receivers}
        PROCEDURE DIVISION.
        MAIN-P.
        {proc}
            STOP RUN.
        """;

    /// <summary>F1-16 shape: group → elementary NUMERIC receiver. GR4 — raw characters left-justified,
    /// right-truncated to the 2 character positions ("12"), NEVER the numeric decode of the image (which would
    /// store 23). The receiver is displayed through a same-width REDEFINES so its raw character content is
    /// asserted, not a re-formatting of it.</summary>
    [Fact]
    public void GroupToElementaryNumeric_RawCharacters_NoConversion()
        => AssertSameAsLegacy(Program("""
            01 RCV-N PIC 99.
            01 RCV-NX REDEFINES RCV-N PIC XX.
            """, """
                MOVE GRP-SEND TO RCV-N.
                DISPLAY "N=" RCV-NX "]".
                IF RCV-N EQUAL TO 12 DISPLAY "PASS" ELSE DISPLAY "FAIL".
            """));

    /// <summary>F1-17 shape: group → elementary numeric receiver WITH a V scale (PIC 9999V999, 7 character
    /// positions) read back through a REDEFINES window. GR4 deposits "123ABC" + 1 fill space — the spec
    /// deliberately deposits non-numeric characters in a numeric item; the test reads them back through the
    /// REDEFINES, never numerically.</summary>
    [Fact]
    public void GroupToScaledNumeric_RawCharacters_ThroughRedefines()
        => AssertSameAsLegacy(Program("""
            01 RCV-V PIC 9999V999.
            01 RCV-VX REDEFINES RCV-V PIC X(7).
            """, """
                MOVE GRP-SEND TO RCV-V.
                IF RCV-VX EQUAL TO "123ABC " DISPLAY "PASS" ELSE DISPLAY "FAIL " RCV-VX "]".
            """));

    /// <summary>F1-20 shape: group → ALPHANUMERIC-EDITED receiver (PIC 0XXXXX0). GR4 — NO editing (GR5 editing
    /// applies only to valid ELEMENTARY moves): the receiver holds the raw "123ABC " image, not "0123AB0".</summary>
    [Fact]
    public void GroupToAlphanumericEdited_NoEditing()
        => AssertSameAsLegacy(Program("""
            01 RCV-AE PIC 0XXXXX0.
            """, """
                MOVE GRP-SEND TO RCV-AE.
                IF RCV-AE EQUAL TO "123ABC " DISPLAY "PASS" ELSE DISPLAY "FAIL " RCV-AE "]".
            """));

    /// <summary>F1-38 shape: group → NUMERIC-EDITED receiver (PIC ZZ9.99, 6 positions). GR4 — NO editing, no
    /// numeric decode: the receiver holds the raw "123ABC", not an edited "123.00"-like image.</summary>
    [Fact]
    public void GroupToNumericEdited_NoEditing()
        => AssertSameAsLegacy(Program("""
            01 RCV-NE PIC ZZ9.99.
            """, """
                MOVE GRP-SEND TO RCV-NE.
                IF RCV-NE EQUAL TO "123ABC" DISPLAY "PASS" ELSE DISPLAY "FAIL " RCV-NE "]".
            """));

    /// <summary>Group → plain alphanumeric receiver wider than the sender: §14.6.8 left-justify + right
    /// space-fill (the baseline group-move alignment).</summary>
    [Fact]
    public void GroupToWiderAlphanumeric_LeftJustifiedSpaceFilled()
        => AssertSameAsLegacy(Program("""
            01 RCV-X PIC X(9).
            """, """
                MOVE GRP-SEND TO RCV-X.
                IF RCV-X EQUAL TO "123ABC   " DISPLAY "PASS" ELSE DISPLAY "FAIL " RCV-X "]".
            """));

    /// <summary>SPEC-PINNED (no NIST coverage): the receiver's JUSTIFIED clause still applies — GR4 reads
    /// "exactly as if it were an alphanumeric to alphanumeric elementary move" and the JUSTIFIED clause is the
    /// receiver's own alignment rule (§14.6.8 exception; §14.9.25.4 GR6c): left space-fill into the wider
    /// JUSTIFIED RIGHT receiver.</summary>
    [Fact]
    public void GroupToJustifiedReceiver_RightJustified()
        => AssertSpecPinned(Program("""
            01 RCV-J PIC X(9) JUSTIFIED RIGHT.
            """, """
                MOVE GRP-SEND TO RCV-J.
                DISPLAY "[" RCV-J "]".
            """), "[   123ABC]");

    /// <summary>The implicit-MOVE funnel (§14.9.30 GR24 READ INTO is "the equivalent of … MOVE"): READ … INTO an
    /// elementary numeric receiver from a (group) record area follows the same GR4 group-move rule — raw
    /// characters, no numeric conversion.</summary>
    [Fact]
    public void ReadInto_ElementaryNumeric_IsGroupMove()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GSMOV2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "GSMOV2-T1".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC.
               02 F-NUM PIC 999.
               02 F-ALP PIC AAA.
            WORKING-STORAGE SECTION.
            01 RCV-N PIC 99.
            01 RCV-NX REDEFINES RCV-N PIC XX.
            PROCEDURE DIVISION.
            MAIN-P.
                OPEN OUTPUT F.
                MOVE 123 TO F-NUM.
                MOVE "ABC" TO F-ALP.
                WRITE F-REC.
                CLOSE F.
                OPEN INPUT F.
                READ F INTO RCV-N AT END DISPLAY "EOF".
                CLOSE F.
                DISPLAY "N=" RCV-NX "]".
                STOP RUN.
            """);
}
