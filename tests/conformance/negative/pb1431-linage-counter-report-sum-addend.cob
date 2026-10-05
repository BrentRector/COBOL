*> reject-at: 85 2002 2014 2023
      *> kb/Work PB1431 - 8.4.3.14.3 SR1: "LINAGE-COUNTER may be referenced only in procedure division statements."
      *> A SUM addend written as LINAGE-COUNTER is the same reference in the same clause family; it used to be
      *> refused as an addend that "does not resolve to a data item" (13.18.54.3 SR5), the right outcome with the
      *> wrong rule's sentence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1431SUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb1431sum.lst".
           SELECT RPT-F ASSIGN TO "pb1431sum.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  LPF LINAGE IS 5 LINES.
       01  LREC PIC X(10).
       FD  RPT-F REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  D1 TYPE DETAIL LINE 3.
           05  COLUMN 1 PIC 99 SUM LINAGE-COUNTER.
       PROCEDURE DIVISION.
           STOP RUN.
