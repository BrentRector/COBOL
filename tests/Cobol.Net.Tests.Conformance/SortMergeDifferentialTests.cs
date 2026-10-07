// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// SORT / MERGE / RELEASE / RETURN (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34): file sort with
/// USING/GIVING and INPUT/OUTPUT PROCEDURE, key direction + significance (GR1/GR2), DUPLICATES IN ORDER stability
/// (GR3), algebraic numeric keys (GR8), the GR5 collating precedence, MERGE's equal-key file order (GR4), RELEASE
/// FROM (GR4 of §14.9.32), RETURN INTO (GR5 of §14.9.34) and the SR4 reversed AT END order, plus the COBOL-2002+
/// table sort (Format 2) edition gate. Pinned to the committed golden (NIST ST-suite green) except where the legacy
/// is not authoritative (the SR4 reversed order, the per-edition gates) — those pin to the spec.
/// </summary>
public sealed class SortMergeDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();
    private static readonly CobolNetCompiler CobolNet2002 = new(2002);

    /// <summary>Spec-pinned facts (memory feedback_use_the_spec): the former legacy engine truncates a GO-TO loop
    /// inside a SORT INPUT/OUTPUT PROCEDURE to a single iteration (the same loop in a plain section works — see
    /// the passing USING/GIVING facts), so these six assert the ISO-derived output directly; the oracle is a
    /// regression net, not authority.</summary>
    private static void AssertSpecPinned(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(expected, cout);
    }

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>A SORT program over one SD with an input and an output procedure (both SECTIONs — the procedure
    /// range is the WHOLE section, first paragraph through last, like PERFORM section). The output procedure
    /// RETURNs every record and DISPLAYs <c>R= key… text</c> until AT END.</summary>
    private static string ProcedureSortProgram(string specialNames, string sdRecord, string sortClauses, string releases) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SRTPROC.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        {specialNames}
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SF ASSIGN TO "SRT-WORK".
        DATA DIVISION.
        FILE SECTION.
        SD SF.
        {sdRecord}
        WORKING-STORAGE SECTION.
        01 WS-EOF PIC X VALUE "N".
        PROCEDURE DIVISION.
        MAIN-SEC SECTION.
        MAIN-1.
            SORT SF {sortClauses}
                INPUT PROCEDURE IS LOAD-SEC
                OUTPUT PROCEDURE IS DUMP-SEC.
            STOP RUN.
        LOAD-SEC SECTION.
        LOAD-1.
        {releases}
        DUMP-SEC SECTION.
        DUMP-1.
            RETURN SF AT END MOVE "Y" TO WS-EOF
                NOT AT END DISPLAY "R=" S-REC
            END-RETURN.
            IF WS-EOF = "N" GO TO DUMP-1.
        """;

    // ── Key direction, significance, and the three phases (ISO §14.9.40 GR1/GR2/GR8/GR9/GR11/GR14) ──────────

    /// <summary>GR1: ASCENDING/DESCENDING is transitive across the data-names of one phrase (S-K2 AND S-K3 both
    /// descend); GR2: key significance is statement order regardless of phrase grouping; GR8a/b: lower value first
    /// under ASCENDING, higher first under DESCENDING. Input/output procedures are whole-SECTION ranges with the
    /// compiler-inserted return mechanism after the last statement (GR11/GR14).</summary>
    [Fact]
    public void Sort_InputOutputProcedures_MultiKeyMixedDirections()
        => AssertSpecPinned(ProcedureSortProgram("", """
            01 S-REC.
               05 S-K1 PIC 9.
               05 S-K2 PIC XX.
               05 S-K3 PIC 9.
               05 S-TXT PIC X(4).
            """,
            "ON ASCENDING KEY S-K1 ON DESCENDING KEY S-K2 S-K3",
            """
                MOVE 1 TO S-K1 MOVE "BB" TO S-K2 MOVE 5 TO S-K3 MOVE "R1" TO S-TXT.
                RELEASE S-REC.
                MOVE 1 TO S-K1 MOVE "AA" TO S-K2 MOVE 7 TO S-K3 MOVE "R2" TO S-TXT.
                RELEASE S-REC.
                MOVE 2 TO S-K1 MOVE "AA" TO S-K2 MOVE 3 TO S-K3 MOVE "R3" TO S-TXT.
                RELEASE S-REC.
                MOVE 1 TO S-K1 MOVE "BB" TO S-K2 MOVE 2 TO S-K3 MOVE "R4" TO S-TXT.
                RELEASE S-REC.
                MOVE 1 TO S-K1 MOVE "AA" TO S-K2 MOVE 9 TO S-K3 MOVE "R5" TO S-TXT.
                RELEASE S-REC.
            """),
            // GR1 transitive desc, GR2 significance, GR8a/b
            "R=1BB5R1\nR=1BB2R4\nR=1AA9R5\nR=1AA7R2\nR=2AA3R3");

    /// <summary>GR3b: with DUPLICATES IN ORDER, records whose keys are ALL equal return in RELEASE order — the
    /// sequence phase must be stable.</summary>
    [Fact]
    public void Sort_DuplicatesInOrder_PreservesReleaseOrder()
        => AssertSpecPinned(ProcedureSortProgram("", """
            01 S-REC.
               05 S-K PIC XX.
               05 S-TXT PIC X(4).
            """,
            "ON ASCENDING KEY S-K WITH DUPLICATES IN ORDER",
            """
                MOVE "BB" TO S-K MOVE "B-1 " TO S-TXT RELEASE S-REC.
                MOVE "AA" TO S-K MOVE "A-1 " TO S-TXT RELEASE S-REC.
                MOVE "BB" TO S-K MOVE "B-2 " TO S-TXT RELEASE S-REC.
                MOVE "AA" TO S-K MOVE "A-2 " TO S-TXT RELEASE S-REC.
                MOVE "BB" TO S-K MOVE "B-3 " TO S-TXT RELEASE S-REC.
            """),
            // GR3b stable within equal keys
            "R=AAA-1\nR=AAA-2\nR=BBB-1\nR=BBB-2\nR=BBB-3");

    /// <summary>GR8 (→ §8.8.4.2.4): a NUMERIC key compares ALGEBRAICALLY by decoded value — the signed zoned
    /// images of −10/−3/0/5 order numerically, never by their character (over-punch) images, and never through a
    /// collating sequence.</summary>
    [Fact]
    public void Sort_SignedNumericKeys_CompareAlgebraically()
        => AssertSpecPinned(ProcedureSortProgram("", """
            01 S-REC.
               05 S-K PIC S99.
               05 S-TXT PIC X(4).
            """,
            "ON ASCENDING KEY S-K",
            """
                MOVE 5 TO S-K MOVE "PLUS" TO S-TXT RELEASE S-REC.
                MOVE -10 TO S-K MOVE "MTEN" TO S-TXT RELEASE S-REC.
                MOVE 0 TO S-K MOVE "ZERO" TO S-TXT RELEASE S-REC.
                MOVE -3 TO S-K MOVE "MTRI" TO S-TXT RELEASE S-REC.
            """),
            // -10 < -3 < 0 < 5 (GR8 algebraic); zoned over-punch images
            "R=1}MTEN\nR=0LMTRI\nR=0{ZERO\nR=0EPLUS");

    /// <summary>GR5a: the statement's COLLATING SEQUENCE phrase takes precedence over the native order — alphabet
    /// SEQ-1 reorders "C" &lt; "A" &lt; "B" (ISO §12.3.7 GR7 literal-phrase positions), so the ascending result is
    /// C, A, B. Numeric keys would ignore the sequence (GR8); this key is alphanumeric.</summary>
    [Fact]
    public void Sort_StatementCollatingSequence_OverridesNative()
        => AssertSpecPinned(ProcedureSortProgram("""
            SPECIAL-NAMES.
                ALPHABET SEQ-1 IS "C" "A" "B".
            """, """
            01 S-REC.
               05 S-K PIC X.
               05 S-TXT PIC X(4).
            """,
            "ON ASCENDING KEY S-K COLLATING SEQUENCE IS SEQ-1",
            """
                MOVE "A" TO S-K MOVE "ROWA" TO S-TXT RELEASE S-REC.
                MOVE "B" TO S-K MOVE "ROWB" TO S-TXT RELEASE S-REC.
                MOVE "C" TO S-K MOVE "ROWC" TO S-TXT RELEASE S-REC.
            """),
            // GR5a: C < A < B under SEQ-1 (12.3.7 GR7 positions)
            "R=CROWC\nR=AROWA\nR=BROWB");

    // ── USING / GIVING — the implicit transfers (ISO §14.9.40 GR12/GR15/GR16) ───────────────────────────────

    /// <summary>GR12: USING is the implicit OPEN INPUT → READ → RELEASE loop → CLOSE; GR15: GIVING the implicit
    /// OPEN OUTPUT → RETURN → WRITE loop → CLOSE. The program writes the unsorted input itself, sorts, and reads
    /// the GIVING file back.</summary>
    [Fact]
    public void Sort_UsingGiving_SingleAscendingKey()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRTUSG.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SIN ASSIGN TO "SRT-IN1".
                SELECT SOUT ASSIGN TO "SRT-OUT1".
                SELECT SF ASSIGN TO "SRT-WK1".
            DATA DIVISION.
            FILE SECTION.
            FD SIN.
            01 IN-REC PIC X(10).
            FD SOUT.
            01 OUT-REC PIC X(10).
            SD SF.
            01 S-REC.
               05 S-KEY PIC X(3).
               05 S-DATA PIC X(7).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            M-1.
                OPEN OUTPUT SIN.
                MOVE "DDD ROW-D " TO IN-REC WRITE IN-REC.
                MOVE "AAA ROW-A " TO IN-REC WRITE IN-REC.
                MOVE "CCC ROW-C " TO IN-REC WRITE IN-REC.
                MOVE "BBB ROW-B " TO IN-REC WRITE IN-REC.
                CLOSE SIN.
                SORT SF ON ASCENDING KEY S-KEY USING SIN GIVING SOUT.
                OPEN INPUT SOUT.
            M-LOOP.
                READ SOUT AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "R=" OUT-REC
                END-READ.
                IF WS-EOF = "N" GO TO M-LOOP.
            M-END.
                CLOSE SOUT.
                STOP RUN.
            """);

    /// <summary>GR15: with several GIVING files, EVERY file receives the FULL sorted result (the return cursor
    /// rewinds per file).</summary>
    [Fact]
    public void Sort_GivingMultipleFiles_EachReceivesFullResult()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRTGV2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SIN ASSIGN TO "SRT-IN2".
                SELECT SOUT1 ASSIGN TO "SRT-OT2A".
                SELECT SOUT2 ASSIGN TO "SRT-OT2B".
                SELECT SF ASSIGN TO "SRT-WK2".
            DATA DIVISION.
            FILE SECTION.
            FD SIN.
            01 IN-REC PIC X(6).
            FD SOUT1.
            01 OUT-REC1 PIC X(6).
            FD SOUT2.
            01 OUT-REC2 PIC X(6).
            SD SF.
            01 S-REC.
               05 S-KEY PIC X(2).
               05 S-TXT PIC X(4).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            M-1.
                OPEN OUTPUT SIN.
                MOVE "22TWO " TO IN-REC WRITE IN-REC.
                MOVE "11ONE " TO IN-REC WRITE IN-REC.
                CLOSE SIN.
                SORT SF ON ASCENDING KEY S-KEY USING SIN GIVING SOUT1 SOUT2.
                OPEN INPUT SOUT1.
            M-LOOP1.
                READ SOUT1 AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "F1=" OUT-REC1
                END-READ.
                IF WS-EOF = "N" GO TO M-LOOP1.
            M-MID.
                CLOSE SOUT1.
                MOVE "N" TO WS-EOF.
                OPEN INPUT SOUT2.
            M-LOOP2.
                READ SOUT2 AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "F2=" OUT-REC2
                END-READ.
                IF WS-EOF = "N" GO TO M-LOOP2.
            M-END.
                CLOSE SOUT2.
                STOP RUN.
            """);

    // ── MERGE (ISO §14.9.24 GR1/GR4) ────────────────────────────────────────────────────────────────────────

    /// <summary>GR4: records with equal keys return in USING-file statement order — ALL of file-1's equal-key
    /// records before file-2's. The two inputs are written pre-sorted (GR6's ordering requirement).</summary>
    [Fact]
    public void Merge_EqualKeys_KeepUsingFileOrder()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MRGORD.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT MIN1 ASSIGN TO "MRG-IN1".
                SELECT MIN2 ASSIGN TO "MRG-IN2".
                SELECT MOUT ASSIGN TO "MRG-OUT".
                SELECT MF ASSIGN TO "MRG-WK".
            DATA DIVISION.
            FILE SECTION.
            FD MIN1.
            01 IN1-REC PIC X(9).
            FD MIN2.
            01 IN2-REC PIC X(9).
            FD MOUT.
            01 OUT-REC PIC X(9).
            SD MF.
            01 M-REC.
               05 M-KEY PIC X(3).
               05 M-TXT PIC X(6).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            M-1.
                OPEN OUTPUT MIN1.
                MOVE "AAA F1-A " TO IN1-REC WRITE IN1-REC.
                MOVE "CCC F1-C " TO IN1-REC WRITE IN1-REC.
                CLOSE MIN1.
                OPEN OUTPUT MIN2.
                MOVE "AAA F2-A " TO IN2-REC WRITE IN2-REC.
                MOVE "BBB F2-B " TO IN2-REC WRITE IN2-REC.
                CLOSE MIN2.
                MERGE MF ON ASCENDING KEY M-KEY USING MIN1 MIN2 GIVING MOUT.
                OPEN INPUT MOUT.
            M-LOOP.
                READ MOUT AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "R=" OUT-REC
                END-READ.
                IF WS-EOF = "N" GO TO M-LOOP.
            M-END.
                CLOSE MOUT.
                STOP RUN.
            """);

    // ── RELEASE FROM / RETURN INTO (ISO §14.9.32 GR4 / §14.9.34 GR5) ────────────────────────────────────────

    /// <summary>§14.9.32 GR4: RELEASE … FROM identifier-1 ≡ MOVE identifier-1 TO record-name-1, then the same
    /// RELEASE without FROM (the COBOL-85 form — a literal FROM operand is 2002+ gated).</summary>
    [Fact]
    public void Release_From_EquivalentToMoveThenRelease()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RELFROM.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SF ASSIGN TO "REL-WK".
            DATA DIVISION.
            FILE SECTION.
            SD SF.
            01 S-REC.
               05 S-K PIC XX.
               05 S-TXT PIC X(4).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            01 WS-BUF PIC X(6).
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-1.
                SORT SF ON ASCENDING KEY S-K
                    INPUT PROCEDURE IS LOAD-SEC
                    OUTPUT PROCEDURE IS DUMP-SEC.
                STOP RUN.
            LOAD-SEC SECTION.
            LOAD-1.
                MOVE "ZZLAST" TO WS-BUF RELEASE S-REC FROM WS-BUF.
                MOVE "AAFRST" TO WS-BUF RELEASE S-REC FROM WS-BUF.
            DUMP-SEC SECTION.
            DUMP-1.
                RETURN SF AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "R=" S-REC
                END-RETURN.
                IF WS-EOF = "N" GO TO DUMP-1.
            """,
            // 14.9.32 GR4 - FROM = MOVE + RELEASE; asc key orders AA before ZZ
            "R=AAFRST\nR=ZZLAST");

    /// <summary>§14.9.34 GR5: RETURN … INTO ≡ RETURN then MOVE record-area → identifier-1 (skipped at end); the
    /// record stays available in BOTH the record area and the INTO receiver.</summary>
    [Fact]
    public void Return_Into_MovesRecordToReceiver()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RETINTO.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SF ASSIGN TO "RET-WK".
            DATA DIVISION.
            FILE SECTION.
            SD SF.
            01 S-REC.
               05 S-K PIC 9.
               05 S-TXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            01 WS-RECV PIC X(6).
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-1.
                SORT SF ON ASCENDING KEY S-K
                    INPUT PROCEDURE IS LOAD-SEC
                    OUTPUT PROCEDURE IS DUMP-SEC.
                STOP RUN.
            LOAD-SEC SECTION.
            LOAD-1.
                MOVE 2 TO S-K MOVE "TWO" TO S-TXT RELEASE S-REC.
                MOVE 1 TO S-K MOVE "ONE" TO S-TXT RELEASE S-REC.
            DUMP-SEC SECTION.
            DUMP-1.
                RETURN SF INTO WS-RECV AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "AREA=" S-REC
                        DISPLAY "INTO=" WS-RECV
                END-RETURN.
                IF WS-EOF = "N" GO TO DUMP-1.
            """,
            // 14.9.34 GR5 - record in BOTH the area and INTO receiver
            "AREA=1ONE\nINTO=1ONE\nAREA=2TWO\nINTO=2TWO");

    /// <summary>§14.9.34.3 SR4: the AT END and NOT AT END phrases may be written in REVERSED order. SPEC-PINNED —
    /// the former legacy engine's grammar predates this allowance, so the expected output derives from the spec: keys
    /// 2,1,3 ascending return ONE, TWO, THREE. (Depends on the returnAtEndPhrase reversed-order grammar
    /// alternative shipped with this slice.)</summary>
    [Fact]
    public void Return_ReversedAtEndPhrases_SpecPinned()
    {
        var (ok, output, detail) = CobolNet.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RETREV.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SF ASSIGN TO "REV-WK".
            DATA DIVISION.
            FILE SECTION.
            SD SF.
            01 S-REC.
               05 S-K PIC 9.
               05 S-TXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC X VALUE "N".
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-1.
                SORT SF ON ASCENDING KEY S-K
                    INPUT PROCEDURE IS LOAD-SEC
                    OUTPUT PROCEDURE IS DUMP-SEC.
                STOP RUN.
            LOAD-SEC SECTION.
            LOAD-1.
                MOVE 2 TO S-K MOVE "TWO" TO S-TXT RELEASE S-REC.
                MOVE 1 TO S-K MOVE "ONE" TO S-TXT RELEASE S-REC.
                MOVE 3 TO S-K MOVE "THREE" TO S-TXT RELEASE S-REC.
            DUMP-SEC SECTION.
            DUMP-1.
                RETURN SF NOT AT END DISPLAY "R=" S-TXT
                    AT END MOVE "Y" TO WS-EOF
                END-RETURN.
                IF WS-EOF = "N" GO TO DUMP-1.
            """);
        Assert.True(ok, detail);
        Assert.Equal("R=ONE\nR=TWO\nR=THREE", output);
    }

    // ── SORT Format 2 — the table sort (ISO §14.9.40 GR18–GR24; COBOL-2002+) ────────────────────────────────

    private const string TableSortSource = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. TBLSORT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 TBL.
           05 ITM OCCURS 5 TIMES.
              10 IK PIC 99.
              10 IT PIC XX.
        PROCEDURE DIVISION.
        MAIN-1.
            MOVE "05AA01BB03CC02DD04EE" TO TBL.
            SORT ITM ON ASCENDING KEY IK.
            DISPLAY TBL.
            STOP RUN.
        """;

    /// <summary>The version-matrix invariant (feedback_version_test_matrix): a construct is REJECTED below its
    /// introduction edition with a SPECIFIC diagnostic. Table SORT (Format 2) is absent from ANSI X3.23-1985 —
    /// COBOL-85 SORT operates on sort-merge files only (M2 catalog, docs/ISO2023_CONFORMANCE_PLAN.md).</summary>
    [Fact]
    public void TableSort_RejectedAt85_WithEditionDiagnostic()
    {
        var (ok, diagnostics) = EditionHarness.Compile(TableSortSource, 85);
        Assert.False(ok, "table SORT (Format 2) must be rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(diagnostics, "requires COBOL-2002");   // the pass band text (Exec Step E fold)
    }

    /// <summary>GR18/GR24 at --std 2002: the table sorts IN PLACE on the typed element array; GR19a ascending by
    /// the numeric key IK. SPEC-PINNED (the per-edition compile is WiseOwl COBOL's own; the expected image follows
    /// directly from GR19/GR24): elements reorder to 01BB 02DD 03CC 04EE 05AA.</summary>
    [Fact]
    public void TableSort_2002_SortsElementsInPlace()
    {
        var (ok, output, detail) = CobolNet2002.CompileAndRun(TableSortSource);
        Assert.True(ok, detail);
        Assert.Equal("01BB02DD03CC04EE05AA", output);
    }
}
