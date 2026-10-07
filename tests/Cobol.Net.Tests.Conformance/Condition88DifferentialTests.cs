// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2c capability checkpoint: <b>level-88 condition-names</b> and <b>sign conditions</b> (COBOLNET_DESIGN §3.5 /
/// §8.8.4.1). An 88 reference is a membership test over its conditional variable (singletons + THRU ranges, multiple
/// VALUEs); <c>SET cond TO TRUE</c> moves the first VALUE into the parent. Pinned to the committed golden.
/// </summary>
public sealed class Condition88DifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. C88TEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void BooleanFlag_AndSetToTrue()
        => AssertMatchesGolden(Program("""
            01 WS-FLAG PIC X VALUE "N".
               88 FLAG-YES VALUE "Y".
               88 FLAG-NO  VALUE "N".
            """, """
                IF FLAG-YES DISPLAY "Y1" ELSE DISPLAY "N1" END-IF.
                IF FLAG-NO  DISPLAY "ISNO" END-IF.
                SET FLAG-YES TO TRUE.
                IF FLAG-YES DISPLAY "Y2" ELSE DISPLAY "N2" END-IF.
                DISPLAY WS-FLAG.
            """));

    // PIC 9(3), not 9(2): the level-88 literal 100 must be a permissible value of the conditional variable's
    // PICTURE (ISO §13.18.63.3 SR2; kb/Work PB586) — under 9(2) the program is illegal and draws COBOLNET1625.
    [Fact]
    public void NumericRange_Thru()
        => AssertMatchesGolden(Program("""
            01 WS-GRADE PIC 9(3) VALUE 75.
               88 PASSING VALUE 60 THRU 100.
               88 FAILING VALUE 0 THRU 59.
               88 PERFECT VALUE 100.
            """, """
                IF PASSING DISPLAY "PASS" ELSE DISPLAY "NOPASS" END-IF.
                IF FAILING DISPLAY "FAIL" ELSE DISPLAY "NOFAIL" END-IF.
                IF PERFECT DISPLAY "PERFECT" ELSE DISPLAY "IMPERFECT" END-IF.
            """));

    [Fact]
    public void MultipleValues()
        => AssertMatchesGolden(Program("""
            01 WS-CODE PIC X VALUE "E".
               88 VOWEL VALUE "A", "E", "I", "O", "U".
            """, """
                IF VOWEL DISPLAY "VOWEL" ELSE DISPLAY "CONSONANT" END-IF.
                MOVE "B" TO WS-CODE.
                IF VOWEL DISPLAY "VOWEL2" ELSE DISPLAY "CONSONANT2" END-IF.
            """));

    [Fact]
    public void ConditionInCompoundExpression()
        => AssertMatchesGolden(Program("""
            01 WS-A PIC 9 VALUE 5.
               88 A-IS-FIVE VALUE 5.
            01 WS-B PIC X VALUE "Q".
               88 B-IS-Q VALUE "Q".
            """, """
                IF A-IS-FIVE AND B-IS-Q DISPLAY "BOTH" ELSE DISPLAY "NOTBOTH" END-IF.
                IF A-IS-FIVE OR B-IS-Q DISPLAY "EITHER" END-IF.
                IF NOT A-IS-FIVE DISPLAY "NOTFIVE" ELSE DISPLAY "ISFIVE" END-IF.
            """));

    [Fact]
    // A level-88 over a Tier-B REDEFINES view: an elementary item (BB) redefined by a group (BB-2) whose subordinates
    // (AAA/BBB) carry their own condition-names. The conditional variable must resolve to the shared backing window
    // (the same item→Place builder a verb operand uses), so a condition test — and SET cond TO TRUE — sees the storage
    // through every view (ISO §13.18.44, §8.8.4.5). The NC211A/NC250A IF-D35 shape.
    public void ConditionName_OverRedefinesView()
        => AssertMatchesGolden(Program("""
            01 IF-D35.
               02 AA PIC X(2).
               02 BB PIC X(2).
                  88 B2 VALUE "CD".
               02 BB-2 REDEFINES BB.
                  03 AAA PIC X.
                     88 AA1 VALUE "A".
                     88 AA2 VALUE "C".
                  03 BBB PIC X.
                     88 BB2 VALUE "D".
            """, """
                MOVE "CD" TO BB.
                IF B2  DISPLAY "B2"  ELSE DISPLAY "NB2"  END-IF.
                IF AA2 DISPLAY "AA2" ELSE DISPLAY "NAA2" END-IF.
                IF BB2 DISPLAY "BB2" ELSE DISPLAY "NBB2" END-IF.
                SET AA1 TO TRUE.
                IF AA1 DISPLAY "AA1" ELSE DISPLAY "NAA1" END-IF.
                DISPLAY BB.
            """));

    [Fact]
    // A level-88 condition-name on a GROUP conditional variable. Per ISO §8.8.4.5 GR2 the test follows the relation-
    // condition rules, and §8.8.4.1 treats an alphanumeric group as an elementary alphanumeric item — so the group's
    // character IMAGE is compared, not the raw struct (the NC211A/NC250A TABLE-86 shape).
    public void ConditionName_OnGroupItem()
        => AssertMatchesGolden(Program("""
            01 TABLE-86.
               88 A86 VALUE "ABC".
               88 B86 VALUE "ABCABC".
               02 DATANAME-86 PIC XXX VALUE "ABC".
               02 DNAME-86.
                  03 FILLER PIC X VALUE "A".
                  03 FILLER PIC X VALUE "B".
                  03 FILLER PIC X VALUE "C".
            """, """
                IF A86 DISPLAY "A86" ELSE DISPLAY "NA86" END-IF.
                IF B86 DISPLAY "B86" ELSE DISPLAY "NB86" END-IF.
            """));

    [Theory]
    // Sign conditions (POSITIVE / NEGATIVE / ZERO), with and without NOT.
    [InlineData("01 N PIC S9(3) VALUE -5.", "    IF N IS NEGATIVE DISPLAY \"NEG\" END-IF.\n    IF N IS NOT POSITIVE DISPLAY \"NOTPOS\" END-IF.")]
    [InlineData("01 N PIC S9(3) VALUE 0.", "    IF N IS ZERO DISPLAY \"ZERO\" END-IF.\n    IF N IS POSITIVE DISPLAY \"POS\" ELSE DISPLAY \"NOTPOS\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 7.", "    IF N IS POSITIVE DISPLAY \"POS\" END-IF.")]
    public void SignConditions(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));
}
