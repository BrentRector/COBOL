*> reject-at: 85 2002 2014 2023
      *> kb/Work PB1431 - 8.4.3.14.3 SR1: "LINAGE-COUNTER may be referenced only in procedure division statements."
      *> The bare SOURCE LINAGE-COUNTER OF LPF is the identifier form of the same position and is refused by the
      *> same rule, under COBOLNET2904 and not under a COBOLNET0899 "does not resolve" sentence about another rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1431BAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb1431bar.lst".
           SELECT RPT-F ASSIGN TO "pb1431bar.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  LPF LINAGE IS 5 LINES.
       01  LREC PIC X(10).
       FD  RPT-F REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  D1 TYPE DETAIL LINE 3.
           05  COLUMN 1 PIC 99 SOURCE LINAGE-COUNTER OF LPF.
       PROCEDURE DIVISION.
           STOP RUN.
