// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// RELATIVE + INDEXED file organizations (ISO §9.1.7.3/.4): the keyed verbs' status machine and transfer of
/// control — WRITE/READ/REWRITE/DELETE/START with the §9.1.14 INVALID KEY contract, the RRN MOVE-backs
/// (§14.9.51 GR29a / §14.9.30 GR25), and the keyed status family ('21' '22' '23', §9.1.13.5). Pinned to the
/// former legacy engine (NIST RL/IX-green) — each compiler runs in its own temp directory, so the connectors'
/// on-disk stores never cross engines. Every test carries its governing ISO paragraph.
/// </summary>
public sealed class KeyedIoDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>§14.9.51 GR29a: sequential-access relative WRITE releases consecutive RRNs from 1 and MOVEs each
    /// into the RELATIVE KEY item; §14.9.30 GR25: each sequential READ MOVEs the RRN of the record made
    /// available; §14.9.30 GR24a: exhaustion sets '10' and takes AT END.</summary>
    [Fact]
    public void Relative_SequentialWriteReadBack_StoresRrnInKeyItem()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KRSEQ1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KRSEQ1F"
                    ORGANIZATION IS RELATIVE
                    ACCESS MODE IS SEQUENTIAL
                    RELATIVE KEY IS WS-RRN
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 KFIL-REC PIC X(12).
            WORKING-STORAGE SECTION.
            01 WS-RRN PIC 9(4).
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE "ALPHA" TO KFIL-REC.
                WRITE KFIL-REC.
                DISPLAY "W1 RRN=" WS-RRN " FS=" WS-FS.
                MOVE "BETA" TO KFIL-REC.
                WRITE KFIL-REC.
                DISPLAY "W2 RRN=" WS-RRN " FS=" WS-FS.
                CLOSE KFIL.
                OPEN INPUT KFIL.
            READ-LOOP.
                READ KFIL AT END GO TO DONE-PARA.
                DISPLAY "R RRN=" WS-RRN " REC=" KFIL-REC.
                GO TO READ-LOOP.
            DONE-PARA.
                DISPLAY "EOF FS=" WS-FS.
                CLOSE KFIL.
                STOP RUN.
            """);

    /// <summary>§14.9.51 GR33a: random WRITE of an occupied slot → '22' + INVALID KEY; §14.9.30 GR29: random READ
    /// of a nonexistent slot → '23' + INVALID KEY; an existing slot reads back its record.</summary>
    [Fact]
    public void Relative_RandomWriteAndRead_InvalidKeyStatuses()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KRRND2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KRRND2F"
                    ORGANIZATION IS RELATIVE
                    ACCESS MODE IS RANDOM
                    RELATIVE KEY IS WS-RRN
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 KFIL-REC PIC X(12).
            WORKING-STORAGE SECTION.
            01 WS-RRN PIC 9(4).
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE 3 TO WS-RRN.
                MOVE "THREE" TO KFIL-REC.
                WRITE KFIL-REC INVALID KEY DISPLAY "BAD-W1".
                MOVE "AGAIN" TO KFIL-REC.
                WRITE KFIL-REC INVALID KEY DISPLAY "DUP FS=" WS-FS.
                CLOSE KFIL.
                OPEN INPUT KFIL.
                MOVE 3 TO WS-RRN.
                READ KFIL INVALID KEY DISPLAY "BAD-R1".
                DISPLAY "GOT=" KFIL-REC.
                MOVE 7 TO WS-RRN.
                READ KFIL INVALID KEY DISPLAY "MISS FS=" WS-FS.
                CLOSE KFIL.
                STOP RUN.
            """);

    /// <summary>§14.9.35 GR21: random REWRITE of an absent slot → '23'; §14.9.10 GR4: random DELETE of an absent
    /// slot → '23'; §9.1.14: NOT INVALID KEY runs only on SUCCESSFUL completion.</summary>
    [Fact]
    public void Relative_RandomRewriteDelete_StatusAndNotInvalidContract()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KRUPD3.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KRUPD3F"
                    ORGANIZATION IS RELATIVE
                    ACCESS MODE IS RANDOM
                    RELATIVE KEY IS WS-RRN
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 KFIL-REC PIC X(12).
            WORKING-STORAGE SECTION.
            01 WS-RRN PIC 9(4).
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE 1 TO WS-RRN.
                MOVE "ONE" TO KFIL-REC.
                WRITE KFIL-REC.
                MOVE 2 TO WS-RRN.
                MOVE "TWO" TO KFIL-REC.
                WRITE KFIL-REC.
                CLOSE KFIL.
                OPEN I-O KFIL.
                MOVE 2 TO WS-RRN.
                MOVE "TWO-NEW" TO KFIL-REC.
                REWRITE KFIL-REC
                    INVALID KEY DISPLAY "RW-BAD"
                    NOT INVALID KEY DISPLAY "RW-OK".
                MOVE 9 TO WS-RRN.
                MOVE "NINE" TO KFIL-REC.
                REWRITE KFIL-REC
                    INVALID KEY DISPLAY "RW-MISS FS=" WS-FS
                    NOT INVALID KEY DISPLAY "RW-WRONG".
                MOVE 1 TO WS-RRN.
                DELETE KFIL RECORD
                    INVALID KEY DISPLAY "DL-BAD".
                MOVE 1 TO WS-RRN.
                DELETE KFIL RECORD
                    INVALID KEY DISPLAY "DL-GONE FS=" WS-FS.
                MOVE 2 TO WS-RRN.
                READ KFIL INVALID KEY DISPLAY "RD-BAD".
                DISPLAY "GOT=" KFIL-REC.
                CLOSE KFIL.
                STOP RUN.
            """);

    /// <summary>§14.9.51 GR42b: duplicate prime key → '22'; §14.9.30 GR32: random READ by RECORD KEY (absent →
    /// '23'); §14.9.35 GR23: random REWRITE replaces by prime key; §14.9.10 GR3: DELETE by prime key.</summary>
    [Fact]
    public void Indexed_RandomCrudByPrimeKey()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KXCRD4.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IXF ASSIGN TO "KXCRD4F"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS RANDOM
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IXF.
            01 IX-REC.
               05 IX-KEY PIC X(3).
               05 IX-DATA PIC X(9).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IXF.
                MOVE "K01" TO IX-KEY.
                MOVE "FIRST" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD1".
                MOVE "K02" TO IX-KEY.
                MOVE "SECOND" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD2".
                MOVE "K01" TO IX-KEY.
                MOVE "DUPED" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "DUP FS=" WS-FS.
                CLOSE IXF.
                OPEN I-O IXF.
                MOVE "K02" TO IX-KEY.
                READ IXF INVALID KEY DISPLAY "R-BAD".
                DISPLAY "GOT=" IX-DATA.
                MOVE "K09" TO IX-KEY.
                READ IXF INVALID KEY DISPLAY "MISS FS=" WS-FS.
                MOVE "K01" TO IX-KEY.
                MOVE "PATCHED" TO IX-DATA.
                REWRITE IX-REC
                    INVALID KEY DISPLAY "RW-BAD"
                    NOT INVALID KEY DISPLAY "RW-OK".
                MOVE "K01" TO IX-KEY.
                READ IXF INVALID KEY DISPLAY "R2-BAD".
                DISPLAY "NOW=" IX-DATA.
                MOVE "K01" TO IX-KEY.
                DELETE IXF RECORD INVALID KEY DISPLAY "DL-BAD".
                MOVE "K01" TO IX-KEY.
                READ IXF INVALID KEY DISPLAY "GONE FS=" WS-FS.
                CLOSE IXF.
                STOP RUN.
            """);

    /// <summary>§14.9.51 GR38/GR42a: sequential-access indexed WRITEs must release strictly ascending prime keys
    /// — an out-of-order key → '21' + INVALID KEY, and the record is NOT written (§9.1.14 — the file is not
    /// affected); the sequential read-back proves it.</summary>
    [Fact]
    public void Indexed_SequentialWrite_AscendingKeyOrder21()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KXSEQ5.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IXF ASSIGN TO "KXSEQ5F"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS SEQUENTIAL
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IXF.
            01 IX-REC.
               05 IX-KEY PIC X(3).
               05 IX-DATA PIC X(9).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IXF.
                MOVE "BBB" TO IX-KEY.
                MOVE "MIDDLE" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD1".
                MOVE "AAA" TO IX-KEY.
                MOVE "BACKWARD" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "SEQ FS=" WS-FS.
                MOVE "CCC" TO IX-KEY.
                MOVE "FORWARD" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD3".
                CLOSE IXF.
                OPEN INPUT IXF.
            READ-LOOP.
                READ IXF NEXT AT END GO TO DONE-PARA.
                DISPLAY "R=" IX-KEY "/" IX-DATA.
                GO TO READ-LOOP.
            DONE-PARA.
                CLOSE IXF.
                STOP RUN.
            """);

    /// <summary>§14.9.41 GR8–GR9: relative START positions the FPI at the first RRN satisfying the comparison
    /// (forward for >=); subsequent sequential READs walk from there; an unsatisfiable comparison → '23' +
    /// INVALID KEY (GR9c).</summary>
    [Fact]
    public void Relative_StartKeyGreaterOrEqual_PositionsThenWalks()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KRSTA6.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KRSTA6F"
                    ORGANIZATION IS RELATIVE
                    ACCESS MODE IS DYNAMIC
                    RELATIVE KEY IS WS-RRN
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 KFIL-REC PIC X(12).
            WORKING-STORAGE SECTION.
            01 WS-RRN PIC 9(4).
            01 WS-CNT PIC 9.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE 0 TO WS-CNT.
            FILL-PARA.
                ADD 1 TO WS-CNT.
                MOVE WS-CNT TO WS-RRN.
                MOVE WS-CNT TO KFIL-REC.
                WRITE KFIL-REC INVALID KEY DISPLAY "W-BAD".
                IF WS-CNT < 5 GO TO FILL-PARA.
                CLOSE KFIL.
                OPEN INPUT KFIL.
                MOVE 3 TO WS-RRN.
                START KFIL KEY IS NOT LESS THAN WS-RRN
                    INVALID KEY DISPLAY "ST-BAD".
                READ KFIL NEXT AT END DISPLAY "EOF-1".
                DISPLAY "P1 RRN=" WS-RRN " REC=" KFIL-REC.
                READ KFIL NEXT AT END DISPLAY "EOF-2".
                DISPLAY "P2 RRN=" WS-RRN " REC=" KFIL-REC.
                MOVE 9 TO WS-RRN.
                START KFIL KEY IS NOT LESS THAN WS-RRN
                    INVALID KEY DISPLAY "NO-POS FS=" WS-FS.
                CLOSE KFIL.
                STOP RUN.
            """);

    /// <summary>§14.9.41 SR6b/GR17: a START operand that is a SHORTER item beginning at the key's leftmost
    /// character position is a GENERIC (partial) key — the comparison takes only that many leftmost characters;
    /// GR16: the key becomes the key of reference for subsequent sequential READs.</summary>
    [Fact]
    public void Indexed_StartGenericPartialKey_PositionsAtFirstMatch()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KXSTA7.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IXF ASSIGN TO "KXSTA7F"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IXF.
            01 IX-REC.
               05 IX-KEY.
                  10 IX-K1 PIC XX.
                  10 IX-K2 PIC X.
               05 IX-DATA PIC X(9).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IXF.
                MOVE "AB1" TO IX-KEY.
                MOVE "R-AB1" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD1".
                MOVE "CD1" TO IX-KEY.
                MOVE "R-CD1" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD2".
                MOVE "AB2" TO IX-KEY.
                MOVE "R-AB2" TO IX-DATA.
                WRITE IX-REC INVALID KEY DISPLAY "W-BAD3".
                CLOSE IXF.
                OPEN INPUT IXF.
                MOVE "AB" TO IX-K1.
                START IXF KEY IS EQUAL TO IX-K1
                    INVALID KEY DISPLAY "ST-BAD FS=" WS-FS.
                READ IXF NEXT AT END DISPLAY "EOF-1".
                DISPLAY "P1=" IX-KEY "/" IX-DATA.
                READ IXF NEXT AT END DISPLAY "EOF-2".
                DISPLAY "P2=" IX-KEY "/" IX-DATA.
                READ IXF NEXT AT END DISPLAY "EOF-3".
                DISPLAY "P3=" IX-KEY "/" IX-DATA.
                CLOSE IXF.
                STOP RUN.
            """);

    /// <summary>§9.1.14 (final rule item 2): with only a NOT INVALID KEY phrase, an invalid-key completion
    /// ('22') takes NEITHER branch — the NOT phrase runs exclusively on successful completion, and the FILE
    /// STATUS item still records the condition.</summary>
    [Fact]
    public void Keyed_NotInvalidKey_RunsOnlyOnSuccess()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KRNIK8.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KRNIK8F"
                    ORGANIZATION IS RELATIVE
                    ACCESS MODE IS RANDOM
                    RELATIVE KEY IS WS-RRN
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 KFIL-REC PIC X(12).
            WORKING-STORAGE SECTION.
            01 WS-RRN PIC 9(4).
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE 1 TO WS-RRN.
                MOVE "FIRST" TO KFIL-REC.
                WRITE KFIL-REC NOT INVALID KEY DISPLAY "NI-1".
                MOVE 1 TO WS-RRN.
                MOVE "CLASH" TO KFIL-REC.
                WRITE KFIL-REC NOT INVALID KEY DISPLAY "NI-2".
                DISPLAY "AFTER FS=" WS-FS.
                CLOSE KFIL.
                STOP RUN.
            """);

    /// <summary>§12.4.5.12.3 SR2 / §12.4.5.6 SR2 + §8.4.2.2: the RECORD KEY / ALTERNATE RECORD KEY operands may
    /// be IN/OF-QUALIFIED — identically named key items under different areas of the record are legal and
    /// disambiguated by qualification (the IX215A shape; a glued GetText lookup could never resolve them).
    /// START's key operand resolves through the same qualified reference machinery (§14.9.41 SR6 then matches
    /// by storage position).</summary>
    [Fact]
    public void Indexed_QualifiedRecordAndAlternateKeys_ResolveByQualifier()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KIQUAL1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KIQUAL1F"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS THE-KEY IN PRIME-AREA
                    ALTERNATE RECORD KEY IS THE-KEY OF ALT-AREA WITH DUPLICATES
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 KFIL-REC.
               02 PRIME-AREA.
                  03 THE-KEY PIC X(4).
               02 ALT-AREA.
                  03 THE-KEY PIC X(4).
               02 PAYLOAD PIC X(8).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE "K001" TO THE-KEY IN PRIME-AREA.
                MOVE "A902" TO THE-KEY OF ALT-AREA.
                MOVE "PAYLOAD1" TO PAYLOAD.
                WRITE KFIL-REC.
                DISPLAY "W1 FS=" WS-FS.
                MOVE "K002" TO THE-KEY IN PRIME-AREA.
                MOVE "A901" TO THE-KEY OF ALT-AREA.
                MOVE "PAYLOAD2" TO PAYLOAD.
                WRITE KFIL-REC.
                DISPLAY "W2 FS=" WS-FS.
                CLOSE KFIL.
                OPEN INPUT KFIL.
                MOVE "K002" TO THE-KEY IN PRIME-AREA.
                READ KFIL INVALID KEY DISPLAY "RND INVALID".
                DISPLAY "RND FS=" WS-FS " REC=" KFIL-REC.
                MOVE "K001" TO THE-KEY IN PRIME-AREA.
                START KFIL KEY IS NOT LESS THAN THE-KEY IN PRIME-AREA
                    INVALID KEY DISPLAY "START INVALID".
                DISPLAY "START FS=" WS-FS.
                READ KFIL NEXT AT END DISPLAY "EOF".
                DISPLAY "RN FS=" WS-FS " REC=" KFIL-REC.
                CLOSE KFIL.
                STOP RUN.
            """);
}
