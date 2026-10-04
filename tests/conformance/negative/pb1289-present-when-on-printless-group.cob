      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1289 - ISO 13.15.3 SR16: "Condition-1 shall not reference any sum counter, LINE-COUNTER, PAGE-COUNTER, or
      *> other report section data item."   cite.py: OK  13.15.3 16)  (Syntax rules)
      *> D2 has a PRESENT WHEN clause naming LINE-COUNTER and produces no line, no printable item and no counter. The binder
      *> scanned only the conditions of what the walk produced, so the written clause was never read; the same condition on an
      *> entry that carries a line was refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1289PRESENTWHENONP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1289PRESENTWHENONP.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 CONTROL FINAL PAGE LIMIT IS 20 LINES.
       01  D1 TYPE DE.
           03  LINE 1.
               05  COLUMN 1 PIC 9 SOURCE WN.
       01  D2 TYPE DE PRESENT WHEN LINE-COUNTER > 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
