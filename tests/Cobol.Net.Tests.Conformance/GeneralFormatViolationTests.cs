// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The 42 conformance CLAIMS the deleted legacy <c>Overlenient/</c> suite made by NAME and never measured — every
/// body was <c>{ }</c> (kb/Work PB387; <c>VacuousTestDriftTests</c> now fails on that shape). Each claim
/// <c>Rejects_&lt;construct&gt;</c> is written here as the spelling its name describes, with the verdict computed
/// from the statement's printed general format and the metalanguage that reads it: §5.2.6.2 (only BRACKETED
/// portions may be omitted), §5.2.6.3 ("one of the alternatives contained within the braces shall be explicitly
/// specified") and §5.2.6.4 (choice indicators: each alternative at most once). The general formats are §14.9.1.2
/// ACCEPT, §14.9.11.2 DISPLAY, §14.9.19.2 IF, §14.9.13.2 EVALUATE, §14.9.28.2 PERFORM, §14.9.43.2 STRING, §14.9.48.2
/// UNSTRING, §14.9.22.2 INSPECT, §14.9.4.2 CALL, §14.9.39.2 SET, §14.9.40.2 SORT, §14.9.24.2 MERGE, §14.9.51.2
/// WRITE, §14.9.30.2 READ, §14.9.27.2 OPEN and §14.9.6.2 CLOSE (each <c>cite.py --check</c>ed).
///
/// <para><b>Ten of the names were false claims about COBOL.</b> A PERFORM VARYING without BY, a STRING or
/// UNSTRING without DELIMITED, a CALL without USING, a READ without INTO, a WRITE without FROM, mixed ASCENDING
/// and DESCENDING keys on SORT and MERGE, an OPEN naming two modes for two files, and an INSPECT with both
/// TALLYING and REPLACING (Format 3) are all LEGAL — each phrase is bracketed or repeatable in its format. Those
/// spellings are pinned as ACCEPTED, so a regression that refused them would be red; the illegal reading of the
/// same name (an empty USING / INTO / FROM, two modes for one file, REPLACING before TALLYING) is pinned as
/// refused.</para>
///
/// <para>Each refused row names the code the compiler gives today. Several are the bare parse codes
/// (COBOL0001 "no viable alternative", COBOL0307 "unexpected") rather than a code that names the rule; a change
/// that names the rule instead updates the row's code, which is the point of pinning it.</para>
/// </summary>
public sealed class GeneralFormatViolationTests
{
    [Theory]
    // ACCEPT / DISPLAY (M412) — FROM and UPON take a mnemonic-name or a device word, never a data item.
    [InlineData("PBGF01", "ACCEPT W-A FROM W-B", "COBOLNET0817")]
    [InlineData("PBGF02", "DISPLAY W-A UPON W-B", "COBOLNET0817")]
    [InlineData("PBGF03", "ACCEPT W-A FROM DATE WITH NO ADVANCING", "COBOL0307")]
    // IF (M413) — condition-1 and statement-1 are unbracketed; ELSE is one bracketed phrase.
    [InlineData("PBGF04", "IF DISPLAY \"X\" END-IF", "COBOL0001")]
    [InlineData("PBGF05", "IF W-N = 1 DISPLAY \"A\" ELSE DISPLAY \"B\" ELSE DISPLAY \"C\" END-IF", "COBOL0307")]
    [InlineData("PBGF06", "IF W-N = 1 END-IF", "COBOLNET2072")]
    // EVALUATE (M414) — a WHEN phrase needs its selection object; WHEN OTHER is one trailing phrase.
    [InlineData("PBGF07", "EVALUATE W-N WHEN DISPLAY \"X\" END-EVALUATE", "COBOL0001")]
    [InlineData("PBGF08", "EVALUATE W-N WHEN OTHER DISPLAY \"O\" WHEN 1 DISPLAY \"1\" END-EVALUATE", "COBOLNET2073")]
    [InlineData("PBGF09", "EVALUATE W-N WHEN 1 DISPLAY \"1\" WHEN OTHER DISPLAY \"O\" WHEN OTHER DISPLAY \"P\" END-EVALUATE", "COBOLNET2073")]
    // PERFORM (M415) — UNTIL takes a condition; the varying-phrase's UNTIL is unbracketed.
    [InlineData("PBGF10", "PERFORM UNTIL DISPLAY \"X\" END-PERFORM", "COBOL0001")]
    [InlineData("PBGF12", "PERFORM VARYING W-N FROM 1 BY 1 DISPLAY W-N END-PERFORM", "COBOL0001")]
    // STRING (M416) — INTO is unbracketed; ON OVERFLOW sits in choice indicators, so at most once.
    [InlineData("PBGF13", "STRING W-A DELIMITED BY SIZE END-STRING", "COBOL0001")]
    [InlineData("PBGF15", "STRING W-A DELIMITED BY SIZE INTO W-B ON OVERFLOW DISPLAY \"1\" ON OVERFLOW DISPLAY \"2\" END-STRING", "COBOL0307")]
    // UNSTRING (M417) — INTO is unbracketed; TALLYING is one bracketed phrase.
    [InlineData("PBGF16", "UNSTRING W-A DELIMITED BY \",\" END-UNSTRING", "COBOL0001")]
    [InlineData("PBGF18", "UNSTRING W-A DELIMITED BY \",\" INTO W-B W-C TALLYING IN W-N TALLYING IN W-K END-UNSTRING", "COBOL0307")]
    // INSPECT (M418) — FOR and BY are required words; Format 3 puts TALLYING before REPLACING.
    [InlineData("PBGF19", "INSPECT W-A TALLYING W-N ALL \",\"", "COBOL0001")]
    [InlineData("PBGF20", "INSPECT W-A REPLACING ALL \",\" \".\"", "COBOL0001")]
    [InlineData("PBGF21", "INSPECT W-A REPLACING ALL \",\" BY \".\" TALLYING W-N FOR ALL \",\"", "COBOL0307")]
    // CALL (M419) — a written USING needs an argument; RETURNING takes an identifier; USING is written once.
    [InlineData("PBGF22", "CALL \"PBSUB\" USING", "COBOL0001")]
    [InlineData("PBGF23", "CALL \"PBSUB\" RETURNING 5", "COBOL0308")]
    [InlineData("PBGF24", "CALL \"PBSUB\" USING W-A USING W-B", "COBOL0307")]
    // SET (M420) — a receiving operand is required; UP BY / DOWN BY is one brace choice over index-names.
    [InlineData("PBGF25", "SET TO 5", "COBOL0001")]
    [InlineData("PBGF26", "SET W-N UP BY 1", "COBOLNET2112")]
    [InlineData("PBGF27", "SET IX UP BY 1 DOWN BY 1", "COBOL0307")]
    // SORT / MERGE (M422, M423) — the file format's ON … KEY phrase is a brace group, data-name-1 unbracketed.
    [InlineData("PBGF28", "SORT SW USING F1 GIVING F2", "COBOLNET1757")]
    [InlineData("PBGF29", "SORT SW ON ASCENDING KEY USING F1 GIVING F2", "COBOLNET1757")]
    [InlineData("PBGF31", "MERGE SW USING F1 F2 GIVING F3", "COBOL0001")]
    [InlineData("PBGF32", "MERGE SW ON ASCENDING KEY USING F1 F2 GIVING F3", "COBOL0001")]
    // WRITE (M424) — a written FROM needs its operand, once; ADVANCING takes an integer or identifier.
    [InlineData("PBGF34", "WRITE R1 FROM", "COBOL0001")]
    [InlineData("PBGF35", "WRITE R1 AFTER ADVANCING \"A\" LINES", "COBOLNET2365")]
    [InlineData("PBGF36", "WRITE R1 FROM W-A FROM W-B", "COBOL0307")]
    // READ (M425) — a written INTO needs its operand; AT END once; KEY only for an indexed file.
    [InlineData("PBGF37", "READ F1 INTO", "COBOL0001")]
    [InlineData("PBGF38", "READ F1 AT END DISPLAY \"E\" AT END DISPLAY \"F\" END-READ", "COBOL0307")]
    [InlineData("PBGF39", "READ F1 KEY IS W-A", "COBOLNET0864")]
    // OPEN / CLOSE (M426) — a mode is required, one per file group; CLOSE names a file.
    [InlineData("PBGF40", "OPEN F1", "COBOL0001")]
    [InlineData("PBGF41", "CLOSE W-A", "COBOLNET1639")]
    [InlineData("PBGF42", "OPEN INPUT OUTPUT F1", "COBOL0001")]
    public void AStatementOutsideItsGeneralFormat_IsRefused(string pid, string statement, string code)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' is outside its general format, and it compiled");
        EditionHarness.AssertHasDiagnostic(diagnostics, code);
    }

    [Theory]
    [InlineData("PBGA11", "PERFORM VARYING W-N FROM 1 UNTIL W-N > 2 DISPLAY W-N END-PERFORM")]
    [InlineData("PBGA14", "STRING W-A INTO W-B END-STRING")]
    [InlineData("PBGA17", "UNSTRING W-A INTO W-B W-C END-UNSTRING")]
    [InlineData("PBGA30", "SORT SW ON ASCENDING KEY SW-K ON DESCENDING KEY SW-D USING F1 GIVING F2")]
    [InlineData("PBGA33", "MERGE SW ON ASCENDING KEY SW-K ON DESCENDING KEY SW-D USING F1 F2 GIVING F3")]
    [InlineData("PBGA22", "CALL \"PBSUB\" ON EXCEPTION CONTINUE END-CALL")]
    [InlineData("PBGA37", "OPEN INPUT F1 READ F1 AT END CONTINUE END-READ CLOSE F1")]
    [InlineData("PBGA34", "OPEN OUTPUT F1 WRITE R1 CLOSE F1")]
    [InlineData("PBGA42", "OPEN INPUT F1 OUTPUT F2 CLOSE F1 F2")]
    [InlineData("PBGA21", "INSPECT W-A TALLYING W-N FOR ALL \",\" REPLACING ALL \",\" BY \".\"")]
    public void TheLegalSpellingAMisnamedClaimDescribed_IsAccepted(string pid, string statement)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.True(ok, $"{pid}: '{statement}' is inside its general format; got:\n{string.Join("\n", diagnostics)}");
    }

    private static string Program(string pid, string statement) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{pid}1.DAT".
            SELECT F2 ASSIGN TO "{pid}2.DAT".
            SELECT F3 ASSIGN TO "{pid}3.DAT".
            SELECT SW ASSIGN TO "{pid}.TMP".
        DATA DIVISION.
        FILE SECTION.
        FD F1.
        01 R1 PIC X(10).
        FD F2.
        01 R2 PIC X(10).
        FD F3.
        01 R3 PIC X(10).
        SD SW.
        01 SW-REC.
           05 SW-K PIC X(4).
           05 SW-D PIC X(6).
        WORKING-STORAGE SECTION.
        01 W-A PIC X(10) VALUE "A,B".
        01 W-B PIC X(10).
        01 W-C PIC X(10).
        01 W-N PIC 9(4) VALUE 0.
        01 W-K PIC 9(4) VALUE 0.
        01 T.
           05 T-E PIC X OCCURS 5 INDEXED BY IX.
        PROCEDURE DIVISION.
        MAIN-PARA.
            {statement}
            STOP RUN.

        """;
}
