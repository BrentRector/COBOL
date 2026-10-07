// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// USE AFTER STANDARD ERROR/EXCEPTION DECLARATIVES (ISO/IEC 1989:2023 §14.3 DECLARATIVES, §14.9.49 USE,
/// §9.1.13.1 exception processing): spec-derived facts at COBOL-85, differential against the committed golden
/// (NIST RL/IX/SQ-green). The contract under test: a declarative runs after the FILE STATUS store for an
/// unsuccessful status NOT covered by the statement's own AT END ('1x') / INVALID KEY ('2x') phrase; file-scoped
/// USE beats mode-scoped (incl. the failed-OPEN being-opened mode, GR6b); at most ONE declarative per exception
/// (GR3/GR5); an active declarative is never re-entered (GR2).
/// </summary>
public sealed class UseDeclarativeDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    /// <summary>§14.9.30 GR24d + §9.1.13.1: READ at end WITHOUT an AT END phrase invokes the file-scoped
    /// declarative — and the FILE STATUS item already holds "10" inside it (the status stores FIRST, GR6).</summary>
    [Fact]
    public void ReadAtEnd_NoPhrase_FileScopedDeclarativeRuns()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud1file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            77 HITS PICTURE 9 VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON F1.
            D1-P.
                ADD 1 TO HITS.
                DISPLAY "DECL FS=" FS1.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                OPEN OUTPUT F1.
                MOVE "ONE RECORD" TO F1-REC.
                WRITE F1-REC.
                CLOSE F1.
                OPEN INPUT F1.
                READ F1.
                READ F1.
                DISPLAY "HITS=" HITS " FS=" FS1.
                CLOSE F1.
                STOP RUN.
            """);

    /// <summary>§9.1.13.1 / §14.9.30 GR24c: a READ at end WITH the AT END phrase does NOT invoke the
    /// declarative — the imperative runs instead.</summary>
    [Fact]
    public void ReadAtEnd_WithPhrase_DeclarativeSuppressed()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud2file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            77 HITS PICTURE 9 VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON F1.
            D1-P.
                ADD 1 TO HITS.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                OPEN OUTPUT F1.
                MOVE "ONE RECORD" TO F1-REC.
                WRITE F1-REC.
                CLOSE F1.
                OPEN INPUT F1.
                READ F1.
                READ F1 AT END DISPLAY "ATEND IMPERATIVE".
                DISPLAY "HITS=" HITS.
                CLOSE F1.
                STOP RUN.
            """);

    /// <summary>§14.9.49.4 GR6b: a failed OPEN INPUT (status 35, the file absent and not OPTIONAL) reaches a
    /// MODE-scoped USE … ON INPUT — the file was "in the process of being opened" in that mode.</summary>
    [Fact]
    public void FailedOpen_ReachesModeScopedDeclarative()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD3.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud3-absent-file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
            D1-P.
                DISPLAY "INPUT-DECL FS=" FS1.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                OPEN INPUT F1.
                DISPLAY "AFTER-OPEN FS=" FS1.
                STOP RUN.
            """);

    /// <summary>§14.9.49.4 GR3/GR5: the FILE-scoped USE takes precedence over the mode-scoped one for the same
    /// exception — exactly ONE declarative runs.</summary>
    [Fact]
    public void FileScope_BeatsModeScope_OneDeclarativeOnly()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD4.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud4-absent-file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            PROCEDURE DIVISION.
            DECLARATIVES.
            DF SECTION. USE AFTER STANDARD ERROR PROCEDURE ON F1.
            DF-P.
                DISPLAY "FILE-SCOPED".
            DM SECTION. USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
            DM-P.
                DISPLAY "MODE-SCOPED".
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                OPEN INPUT F1.
                STOP RUN.
            """);

    /// <summary>§14.9.49.4 GR2: a declarative that itself provokes an I/O exception on its own file is NOT
    /// re-entered (the RL111A re-entrancy shape — without the guard this recurses to a stack overflow).</summary>
    [Fact]
    public void Declarative_SelfExceptionDoesNotReenter()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD5.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud5-absent-file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            77 HITS PICTURE 99 VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON F1.
            D1-P.
                ADD 1 TO HITS.
                CLOSE F1.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                OPEN INPUT F1.
                DISPLAY "HITS=" HITS.
                STOP RUN.
            """);

    /// <summary>§14.9.49.4 GR6: a SUCCESSFUL completion (status 0x) never invokes a declarative, and after a
    /// non-fatal declarative return execution continues after the failing statement (GR7b).</summary>
    [Fact]
    public void SuccessNeverInvokes_AndExecutionContinuesAfterReturn()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD6.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud6file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            77 HITS PICTURE 99 VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON F1.
            D1-P.
                ADD 1 TO HITS.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                OPEN OUTPUT F1.
                MOVE "REC" TO F1-REC.
                WRITE F1-REC.
                CLOSE F1.
                DISPLAY "AFTER-SUCCESS HITS=" HITS.
                CLOSE F1.
                DISPLAY "AFTER-DOUBLE-CLOSE HITS=" HITS " FS=" FS1.
                STOP RUN.
            """);

    /// <summary>§14.9.49 SR4: a declarative paragraph may be PERFORMed from the nondeclarative body as a plain
    /// procedure (declaratives share the one pc space).</summary>
    [Fact]
    public void PerformIntoDeclarative_RunsAsPlainProcedure()
        => AssertMatchesGolden("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UD7.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "ud7file" STATUS IS FS1.
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 F1-REC PICTURE X(20).
            WORKING-STORAGE SECTION.
            77 FS1 PICTURE XX.
            77 HITS PICTURE 99 VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION. USE AFTER STANDARD ERROR PROCEDURE ON F1.
            D1-P.
                ADD 1 TO HITS.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                PERFORM D1-P.
                PERFORM D1-P.
                DISPLAY "HITS=" HITS.
                STOP RUN.
            """);
}
