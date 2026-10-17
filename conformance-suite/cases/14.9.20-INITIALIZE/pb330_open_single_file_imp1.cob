*>--- conformance-case
*> id: pb330_open_single_file_imp1
*> rules: [SR-14.9.20.3-2, SR-14.9.27.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ONE OPERAND IN IMPERATIVE-STATEMENT-1 IS CONFORMING (kb/Work PB330, PB417).
      *> ISO 14.9.27.3 SR3 bans an OPEN that "specifies file-name-1 more
      *> than once" and 14.9.20.3 SR2 requires that "Identifier-1 shall be
      *> specified only once" in imperative-statement-1 of an
      *> exception-checking PERFORM. Each ban counts operands, so ONE
      *> file-name / ONE identifier-1 per statement is legal there, and a
      *> multi-operand OPEN in a WHEN body is outside the ban's region.
      *> DERIVATION - no exception occurs, so no WHEN body runs (14.9.28.4).
      *>  . OPEN OUTPUT F1 then OPEN OUTPUT F2: '00' each (9.1.13.2 rule 1).
      *>  . INITIALIZE N (PIC 9 VALUE 7) then INITIALIZE M: both become 0
      *>    (14.9.20.4 GR6 c): a numeric receiving-operand takes ZEROES).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB330POS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb330p1.txt" FILE STATUS IS S1.
           SELECT F2 ASSIGN TO "pb330p2.txt" FILE STATUS IS S2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       FD F2.
       01 R-F2 PIC X(5).
       WORKING-STORAGE SECTION.
       01 S1 PIC XX.
       01 S2 PIC XX.
       01 N  PIC 9 VALUE 7.
       01 M  PIC 9 VALUE 8.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               OPEN OUTPUT F1
               OPEN OUTPUT F2
               INITIALIZE N
               INITIALIZE M
           WHEN EXCEPTION F1
               OPEN OUTPUT F1 F2
           END-PERFORM.
           DISPLAY "S1=" S1 " S2=" S2 " N=" N " M=" M.
           CLOSE F1 F2.
           STOP RUN.
