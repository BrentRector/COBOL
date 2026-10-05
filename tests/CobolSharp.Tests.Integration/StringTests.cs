using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class StringTests : EndToEndTestBase
{
    [Fact]
    public void InspectReplacing_ReplacesCharacters()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING ALL "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXBBXXCCXX", stdout);
    }


    [Fact]
    public void Inspect_TallyingAll()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            01 WS-COUNT PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA TALLYING WS-COUNT
                    FOR ALL "AA".
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("03", stdout);
    }


    [Fact]
    public void Inspect_ReplacingFirst()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING FIRST "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXBBAACCAA", stdout);
    }


    [Fact]
    public void Inspect_ReplacingLeading()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AAAABBCCAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING LEADING "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXXXBBCCAA", stdout);
    }


    [Fact]
    public void Inspect_Converting()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "ABCABCABCA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA CONVERTING "ABC" TO "XYZ".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XYZXYZXYZX", stdout);
    }


    [Fact]
    public void Inspect_ReplacingAllBeforeAfter()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(12) VALUE "AABBCCAADDAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING ALL "AA" BY "XX"
                    AFTER "CC" BEFORE "DD".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("AABBCCXXDDAA", stdout);
    }

    // ── ACCEPT ──


    [Fact]
    public void String_ConcatenatesLiterals()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO T.
                STRING "AB" "CD" DELIMITED BY SIZE
                    INTO T
                END-STRING.
                DISPLAY T.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // DISPLAY trims trailing spaces; STRING only writes to positions 1-4,
        // remaining positions keep their SPACES value but are trimmed by DISPLAY
        Assert.Equal("ABCD", stdout);
    }


    [Fact]
    public void String_DelimitedByLiteral()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO T.
                STRING "ABXCD" DELIMITED BY "X"
                       "ZZ" DELIMITED BY SIZE
                    INTO T
                END-STRING.
                DISPLAY T.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABZZ", stdout);
    }


    [Fact]
    public void String_OnOverflow()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO T.
                STRING "ABCDEFGH" DELIMITED BY SIZE
                    INTO T
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "OK"
                END-STRING.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OVF", stdout);
    }

    // ── UNSTRING ──


    [Fact]
    public void Unstring_BasicSplit()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UNSTR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SRC PIC X(10) VALUE "AA,BB,CC".
            01 A   PIC X(4).
            01 B   PIC X(4).
            01 C   PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                UNSTRING SRC DELIMITED BY ","
                    INTO A
                         B
                         C
                END-UNSTRING.
                DISPLAY A.
                DISPLAY B.
                DISPLAY C.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("AA", lines[0]);
        Assert.Equal("BB", lines[1]);
        Assert.Equal("CC", lines[2]);
    }


    [Fact]
    public void Unstring_NoDelimiterMatch_MovesFullSource()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UNSTR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SRC PIC X(10) VALUE "HELLO".
            01 A   PIC X(10).
            01 B   PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                UNSTRING SRC DELIMITED BY ","
                    INTO A
                         B
                END-UNSTRING.
                DISPLAY A.
                DISPLAY B.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // No delimiter match: full source goes to A. B is space-filled.
        // DISPLAY trims trailing spaces, so B outputs nothing visible.
        Assert.Equal("HELLO", stdout);
    }


    [Fact]
    public void Unstring_OnOverflow()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UNSTR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SRC PIC X(10) VALUE "A,B,C".
            01 X   PIC X(4).
            01 Y   PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                UNSTRING SRC DELIMITED BY ","
                    INTO X
                         Y
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "OK"
                END-UNSTRING.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Source has 3 fields (A, B, C) but only 2 INTO targets.
        // After X gets "A" and Y gets "B", "C" remains unexamined.
        // Per ISO §14.9.44: overflow occurs when all receiving areas have been
        // acted upon but unexamined characters remain in the source.
        Assert.Equal("OVF", stdout);
    }

    [Fact]
    public void Inspect_Replacing_OnSignedNumeric_DeSignsAndRetainsSign()
    {
        // ISO 1989:1985 14.9.22 GR 4d: a signed numeric item is inspected as though moved
        // to an UNSIGNED numeric item (operational sign removed), and the original sign is
        // retained on completion. PIC S9(5) -12345 is stored with a trailing overpunch
        // ("1234N"), so REPLACING ALL "5" must see the de-signed digits "12345", replace the
        // '5' -> "12348", and keep the value negative => -12348.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSPSGN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC S9(5) VALUE -12345.
            01 ABSV PIC 9(5).
            01 SGN PIC X.
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT N REPLACING ALL "5" BY "8".
                MOVE N TO ABSV.
                IF N IS NEGATIVE
                    MOVE "-" TO SGN
                ELSE
                    MOVE "+" TO SGN
                END-IF.
                DISPLAY SGN ABSV.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("-12348", stdout);
    }

    // ── SECTION control flow ──

}
