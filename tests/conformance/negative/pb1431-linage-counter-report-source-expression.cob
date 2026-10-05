*> reject-at: 85 2002 2014 2023
      *> kb/Work PB1431 - 8.4.3.14.3 SR1: "LINAGE-COUNTER may be referenced only in procedure division statements."
      *> A report-section SOURCE clause is a data division entry, so SOURCE LINAGE-COUNTER + 1 is refused, as the
      *> data-name clause operands (OCCURS DEPENDING ON, FILE STATUS, ...) already were by COBOLNET2024. Before, the
      *> expression bound through the procedure-phase expression binder and read the live counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1431SRC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb1431src.lst".
           SELECT RPT-F ASSIGN TO "pb1431src.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  LPF LINAGE IS 5 LINES.
       01  LREC PIC X(10).
       FD  RPT-F REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  D1 TYPE DETAIL LINE 3.
           05  COLUMN 1 PIC 99 SOURCE LINAGE-COUNTER + 1.
       PROCEDURE DIVISION.
           STOP RUN.
