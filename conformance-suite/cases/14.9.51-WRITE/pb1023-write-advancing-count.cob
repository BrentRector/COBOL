      *>--- conformance-case
      *> id: pb1023-write-advancing-count
      *> rules: [SR-14.9.51.3-14, SR-14.9.51.3-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1023 - the WRITE ... ADVANCING count outside both of
      *> its alternatives. ISO 14.9.51.3 SR14: "Identifier-2 shall
      *> reference an integer data item." SR15: "Integer-1 shall be
      *> positive or zero." Each statement below compiled clean before
      *> PB1023: the PIC X item holding "2" advanced two lines, the PIC
      *> 9V9 item holding 1.5 advanced one, and the three literals are
      *> not unsigned integer literals (8.3.3.3.2: "An integer literal
      *> is a fixed-point numeric literal that contains no decimal
      *> point"). The rules are the same at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1023NG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb1023ng.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF.
       01 PR PIC X(5).
       WORKING-STORAGE SECTION.
       01 N-X PIC X VALUE "2".
       01 N-V PIC 9V9 VALUE 1.5.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRTF.
           MOVE "LINE1" TO PR.
           WRITE PR AFTER ADVANCING N-X LINES.
           WRITE PR AFTER ADVANCING N-V LINES.
           WRITE PR AFTER ADVANCING 1.5 LINES.
           WRITE PR AFTER ADVANCING "2" LINES.
           WRITE PR AFTER ADVANCING -1 LINES.
           CLOSE PRTF.
           STOP RUN.
