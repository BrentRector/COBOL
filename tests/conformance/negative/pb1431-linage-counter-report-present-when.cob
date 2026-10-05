*> reject-at: 2002 2014 2023
      *> kb/Work PB1431 - 8.4.3.14.3 SR1: "LINAGE-COUNTER may be referenced only in procedure division statements."
      *> A PRESENT WHEN condition (13.18.41, COBOL-2002 and later) is a data division entry, so
      *> PRESENT WHEN LINAGE-COUNTER > 1 is refused; before, it bound and compared the live counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1431PWH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb1431pwh.lst".
           SELECT RPT-F ASSIGN TO "pb1431pwh.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  LPF LINAGE IS 5 LINES.
       01  LREC PIC X(10).
       FD  RPT-F REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  D1 TYPE DETAIL LINE 3 PRESENT WHEN LINAGE-COUNTER > 1.
           05  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
           STOP RUN.
