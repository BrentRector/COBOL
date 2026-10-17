*>--- conformance-case
*> id: pb365-report-group-ambiguous
*> rules: [SR-14.9.49.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO §8.4.2.2.3 SR1: "For each non unique user-defined name that is explicitly referenced, uniqueness shall
*> be established through a sequence of qualifiers that precludes any ambiguity of reference."  §8.4.2.2.1
*> states the same rule and lists its six exceptions; none of them covers a report-group name (2 REDEFINES,
*> 3 VARYING, 4 an unreferenced type declaration, 5 a data-name in a clause subordinate to the same group,
*> 6 a paragraph-name in its own section).
*>
*> DET-A is described in BOTH report description entries, and BOTH the USE BEFORE REPORTING declarative and
*> the GENERATE name it WITHOUT the report-name qualifier §8.4.2.2.2 Format 1 provides.  Each is an ambiguous
*> reference and each is COBOLNET1920.  Rejected at EVERY edition: the uniqueness rule has no introduction
*> edition and the Report Writer module carries it in all four.  Before kb/Work PB365 this program compiled
*> with ZERO diagnostics and bound both references to R-A's group, purely because R-A is written first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB365AMB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA ASSIGN TO "pb365-amb-a.rpt".
           SELECT RB ASSIGN TO "pb365-amb-b.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RA REPORT IS R-A.
       FD RB REPORT IS R-B.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-A PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "A=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       RD R-B PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "B=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR SECTION.
           USE BEFORE REPORTING DET-A.
       BR-P.
           ADD 1 TO WS-N.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RA.
           OPEN OUTPUT RB.
           INITIATE R-A.
           INITIATE R-B.
           GENERATE DET-A.
           TERMINATE R-A.
           TERMINATE R-B.
           CLOSE RA.
           CLOSE RB.
           DISPLAY "N=" WS-N.
           STOP RUN.
