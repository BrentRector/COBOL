// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A FILE CLAUSE'S data-name OPERAND IS A QUALIFIED-DATA-NAME, RESOLVED BY ISO/IEC 1989:2023 §8.4.2.2
/// (kb/Work PB489).
/// <para>Every clause below prints <i>data-name-n</i> in its general format — LINAGE (§13.18.34.2), FILE STATUS
/// (§12.4.5.8), RELATIVE KEY (§12.4.5.13), RECORD … DEPENDING ON (§13.18.43.2) — so each names a
/// qualified-data-name (§8.4.2.2.2 Format 1) and §8.4.2.2.1's uniqueness requirement is in force over it:
/// "uniqueness shall be established through qualification for each user-defined name explicitly referenced".
/// The binder used to keep only the operand's FIRST WORD and then resolve it with a first-match
/// <c>ByName[n][0]</c>, so a qualified operand bound the WRONG data item and an ambiguous one bound the
/// first-declared — both silently. On the LINAGE clause that was a measured wrong answer: the whole logical
/// page was built on another item's value.</para>
/// <para>⛔ EVERY CASE IS RUN ON ALL FOUR EDITIONS. The clauses, their operands and §8.4.2.2 all predate
/// COBOL-85, so an edition-shaped difference here would be a defect, and asserting it on 2023 alone would let
/// one appear.</para>
/// <para>⭐ THE DECOY IS DECLARED FIRST in every fixture. That is what makes these tests able to fail: a
/// first-match lookup returns the decoy, so a green run is evidence that the qualifier was read rather than
/// evidence that the program happened to have one candidate.</para>
/// </summary>
public sealed class ClauseOperandQualificationSpecTests
{
    private static readonly int[] AllEditions = [85, 2002, 2014, 2023];

    private static void AssertRejects(string source, string code)
    {
        foreach (int ed in AllEditions)
        {
            var (ok, diags) = EditionHarness.Compile(source, ed);
            Assert.False(ok, $"--std {ed}: accepted source the standard forbids; diagnostics: {string.Join(" | ", diags)}");
            Assert.Contains(diags, d => d.Contains(code, StringComparison.Ordinal));
        }
    }

    private static void AssertRuns(string source, string expected)
    {
        foreach (int ed in AllEditions)
        {
            var (ok, stdout, detail) = EditionHarness.CompileAndRun(source, ed);
            Assert.True(ok, $"--std {ed}: {detail}");
            Assert.Equal(expected, stdout.Replace("\r\n", "\n").TrimEnd('\n'));
        }
    }

    // ── LINAGE (ISO §13.18.34) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The qualified operand names GRP-B's SZ = 9, so §13.18.34.4 GR2's page size is 9: GR7 d) sets
    /// LINAGE-COUNTER to 1 at OPEN OUTPUT and GR7 c) 2 adds the ADVANCING 4, reaching 5 — inside a 9-line page
    /// body, so no end-of-page condition arises. Under the first-word defect the page size was GRP-A's 3 and the
    /// same write overflowed: EOP with the counter reset (measured).</summary>
    [Fact]
    public void LinageOperand_QualifiedByItsGroup_UsesThatGroupsItem()
        => AssertRuns(TwoSzProgram("SZ OF GRP-B"), "NO-EOP\nLC=005");

    /// <summary>§8.4.2.2.3 SR1 — two declarations of SZ and no qualifier, so no sequence of qualifiers precludes
    /// the ambiguity and the reference identifies no resource (§8.4.2.1). It used to resolve to the
    /// first-declared item and run.</summary>
    [Fact]
    public void LinageOperand_AmbiguousUnqualified_IsRejected()
        => AssertRejects(TwoSzProgram("SZ"), "COBOLNET1639");

    /// <summary>§8.4.3.14.3 SR1 — "LINAGE-COUNTER may be referenced only in procedure division statements"; a
    /// file description entry is not one, and the register is an §8.4.3.1 Format 10 identifier rather than the
    /// qualified-data-name §13.18.34.2 prints. It used to compile and die at OPEN naming the FILE as the missing
    /// data item, because the capture kept the first cobolWord — which for this alternative is the qualifier.</summary>
    [Fact]
    public void LinageOperand_WrittenAsTheRegister_IsRejected()
        => AssertRejects(TwoLinageFileProgram(), "COBOLNET2024");

    /// <summary>A subscript is not part of §8.4.2.2.2 Format 1, and §13.18.34.3 SR1 forecloses the only reason to
    /// write one. The subscript used to be discarded silently and the program died at OPEN.</summary>
    [Fact]
    public void LinageOperand_Subscripted_IsRejected()
        => AssertRejects(TableOperandProgram("T-LINES (2)"), "COBOLNET2024");

    /// <summary>§13.18.34.3 SR1 over the RESOLVED item — the unsubscripted spelling of the same violation, which
    /// no written-shape screen can see. The rule had no site in the compiler at all before kb/Work PB489.</summary>
    [Fact]
    public void LinageOperand_SubjectToOccurs_IsRejected()
        => AssertRejects(TableOperandProgram("T-LINES"), "COBOLNET2025");

    /// <summary>§8.4.3.14.3 SR2 — "The LINAGE-COUNTER identifier shall not be referenced as a receiving operand."
    /// The outcome was already a rejection; the SENTENCE was "a reference shape WiseOwl COBOL does not yet implement
    /// as a receiver", which describes permanently illegal source as a pending feature (kb/Work PB489).</summary>
    [Fact]
    public void LinageCounter_AsAReceivingOperand_IsRejectedByItsOwnRule()
    {
        AssertRejects(ReceivingProgram(), "COBOLNET2026");
        foreach (int ed in AllEditions)
            Assert.DoesNotContain(EditionHarness.GetDiagnostics(ReceivingProgram(), ed),
                d => d.Contains("COBOLNET0899", StringComparison.Ordinal));
    }

    // ── The siblings: the SAME capture and the SAME resolver, on the other clauses that reduce a written
    //    reference to a data-name (kb/Work PB489's sibling sweep — none of these was probed when it was filed).

    /// <summary>FILE STATUS (ISO §12.4.5.8) — the status is placed in the QUALIFIED item, and the decoy declared
    /// before it is left at its VALUE. §12.4.5.8.4 GR1 puts the I-O status in data-name-1 after every statement
    /// referencing the file, so a successful OPEN writes "00".</summary>
    [Fact]
    public void FileStatusOperand_QualifiedByItsGroup_UsesThatGroupsItem()
        => AssertRuns(FileStatusProgram("FS OF G-B"), "A=99 B=00");

    /// <summary>§8.4.2.2.3 SR1 on the FILE STATUS operand — it used to take the first-declared FS silently.</summary>
    [Fact]
    public void FileStatusOperand_AmbiguousUnqualified_IsRejected()
        => AssertRejects(FileStatusProgram("FS"), "COBOLNET1639");

    /// <summary>RELATIVE KEY (ISO §12.4.5.13) — the qualifier used to be DROPPED ENTIRELY at capture (only the
    /// base word was kept), so the clause could not even be written qualified without binding another item. The
    /// program writes record 2 through the qualified key and reads it back.</summary>
    [Fact]
    public void RelativeKeyOperand_QualifiedByItsGroup_UsesThatGroupsItem()
        => AssertRuns(RelativeKeyProgram("RK OF G-B"), "GOT=BBBB");

    /// <summary>§8.4.2.2.3 SR1 on the RELATIVE KEY operand.</summary>
    [Fact]
    public void RelativeKeyOperand_AmbiguousUnqualified_IsRejected()
        => AssertRejects(RelativeKeyProgram("RK"), "COBOLNET1639");

    /// <summary>§8.4.2.2.3 SR1 on RECORD … DEPENDING ON (ISO §13.18.43) — the third clause whose capture kept
    /// only the base word.</summary>
    [Fact]
    public void RecordDependingOperand_AmbiguousUnqualified_IsRejected()
        => AssertRejects(DependingProgram("RL"), "COBOLNET1639");

    /// <summary>…and the qualified spelling binds the qualified item: §13.18.43.4 GR13 a) makes the WRITE release
    /// a record as long as the DEPENDING item says, so naming G-A's RL (3) and naming G-B's RL (6) must put
    /// THREE more characters on the medium in the second case.
    /// <para>⭐ THE ASSERTION IS THE DIFFERENCE, NOT EITHER LENGTH. What the standard fixes is the RECORD's
    /// length; how many bytes a record-sequential medium adds around it is §9.1.7's implementor-defined physical
    /// mapping, and pinning an absolute byte count here would be pinning that instead of GR13 a). The difference
    /// cancels it exactly, and it is the discriminator that matters: under the first-word capture BOTH spellings
    /// resolved to the first-declared RL and the two runs produced the SAME length.</para></summary>
    [Fact]
    public void RecordDependingOperand_QualifiedByItsGroup_UsesThatGroupsItem()
    {
        foreach (int ed in AllEditions)
        {
            int shortLen = DependingLength("RL OF G-A", ed);
            int longLen = DependingLength("RL OF G-B", ed);
            Assert.Equal(3, longLen - shortLen);
        }
    }

    private static int DependingLength(string operand, int edition)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(DependingProgram(operand), edition);
        Assert.True(ok, $"--std {edition}: {detail}");
        string line = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        Assert.StartsWith("LEN=", line, StringComparison.Ordinal);
        return int.Parse(line["LEN=".Length..]);
    }

    // ── Fixtures. The DECOY GROUP IS ALWAYS DECLARED FIRST. ─────────────────────────────────────────────────

    private static string TwoSzProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ1.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "clq1.prt".
        DATA DIVISION.
        FILE SECTION.
        FD LPF LINAGE IS {operand} LINES.
        01 P-REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 GRP-A.
           05 SZ PIC 99 VALUE 3.
        01 GRP-B.
           05 SZ PIC 99 VALUE 9.
        01 LC PIC 9(3).
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
            MOVE "AAAA" TO P-REC.
            WRITE P-REC AFTER ADVANCING 4 LINES
                AT END-OF-PAGE DISPLAY "EOP"
                NOT AT END-OF-PAGE DISPLAY "NO-EOP"
            END-WRITE.
            MOVE LINAGE-COUNTER OF LPF TO LC.
            DISPLAY "LC=" LC.
            CLOSE LPF.
            STOP RUN.
        """;

    private static string TwoLinageFileProgram() => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ2.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "clq2a.prt".
            SELECT LPG ASSIGN TO "clq2b.prt".
        DATA DIVISION.
        FILE SECTION.
        FD LPF LINAGE IS 9 LINES.
        01 F-REC PIC X(4).
        FD LPG LINAGE IS LINAGE-COUNTER OF LPF LINES.
        01 G-REC PIC X(4).
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
            OPEN OUTPUT LPG.
            CLOSE LPF.
            CLOSE LPG.
            STOP RUN.
        """;

    private static string TableOperandProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ3.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "clq3.prt".
        DATA DIVISION.
        FILE SECTION.
        FD LPF LINAGE IS {operand} LINES.
        01 P-REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 T-TAB.
           05 T-LINES PIC 99 OCCURS 3 TIMES.
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
            CLOSE LPF.
            STOP RUN.
        """;

    private static string ReceivingProgram() => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ4.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "clq4.prt".
        DATA DIVISION.
        FILE SECTION.
        FD LPF LINAGE IS 9 LINES.
        01 P-REC PIC X(4).
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
            MOVE 3 TO LINAGE-COUNTER.
            CLOSE LPF.
            STOP RUN.
        """;

    private static string FileStatusProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ5.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SEQF ASSIGN TO "clq5.dat"
                FILE STATUS IS {operand}.
        DATA DIVISION.
        FILE SECTION.
        FD SEQF.
        01 S-REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 G-A.
           05 FS PIC XX VALUE "99".
        01 G-B.
           05 FS PIC XX VALUE "99".
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT SEQF.
            DISPLAY "A=" FS OF G-A " B=" FS OF G-B.
            CLOSE SEQF.
            STOP RUN.
        """;

    private static string RelativeKeyProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ6.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RELF ASSIGN TO "clq6.dat"
                ORGANIZATION IS RELATIVE
                ACCESS MODE IS RANDOM
                RELATIVE KEY IS {operand}.
        DATA DIVISION.
        FILE SECTION.
        FD RELF.
        01 R-REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 G-A.
           05 RK PIC 9(3) VALUE 1.
        01 G-B.
           05 RK PIC 9(3) VALUE 1.
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT RELF.
            MOVE 2 TO RK OF G-B.
            MOVE 9 TO RK OF G-A.
            MOVE "BBBB" TO R-REC.
            WRITE R-REC.
            CLOSE RELF.
            OPEN INPUT RELF.
            MOVE 2 TO RK OF G-B.
            MOVE 9 TO RK OF G-A.
            READ RELF.
            DISPLAY "GOT=" R-REC.
            CLOSE RELF.
            STOP RUN.
        """;

    private static string DependingProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLQ7.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT VARF ASSIGN TO "clq7.dat".
            SELECT RDV ASSIGN TO "clq7.dat".
        DATA DIVISION.
        FILE SECTION.
        FD VARF RECORD IS VARYING IN SIZE FROM 1 TO 9 CHARACTERS
                DEPENDING ON {operand}.
        01 V-REC PIC X(9).
        FD RDV.
        01 D-CHAR PIC X.
        WORKING-STORAGE SECTION.
        01 G-A.
           05 RL PIC 9(3) VALUE 3.
        01 G-B.
           05 RL PIC 9(3) VALUE 6.
        01 POSN PIC 9(3) VALUE 0.
        01 EOF-SW PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT VARF.
            MOVE "ABCDEFGHI" TO V-REC.
            WRITE V-REC.
            CLOSE VARF.
            OPEN INPUT RDV.
            PERFORM UNTIL EOF-SW = 1
                READ RDV
                    AT END MOVE 1 TO EOF-SW
                    NOT AT END ADD 1 TO POSN
                END-READ
            END-PERFORM.
            CLOSE RDV.
            DISPLAY "LEN=" POSN.
            STOP RUN.
        """;
}
