      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1456 - ISO 8.4.2.2.3 SR9: "Whenever the LINE-COUNTER of a different report is referenced, LINE-COUNTER
      *> shall be qualified explicitly by the report-name associated with the different report." The qualifier D2 names a
      *> report GROUP, not a report, so the reference is a qualification violation: COBOLNET2729 (the report counter
      *> qualification rule, not the not-yet-implemented band the legal qualified form used to draw).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1456N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb1456n1a.txt".
           SELECT P2 ASSIGN TO "pb1456n1b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P1 REPORT IS R1.
       FD  P2 REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE LINE-COUNTER OF D2.
       RD  R2 PAGE LIMIT IS 30 LINES.
       01  D2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
           OPEN OUTPUT P1 P2.
           INITIATE R1 R2.
           GENERATE D1.
           TERMINATE R1 R2.
           CLOSE P1 P2.
           STOP RUN.
