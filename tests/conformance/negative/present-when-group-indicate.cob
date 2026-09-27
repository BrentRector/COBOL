      *> reject-at: 2002 2014 2023
      *> ISO §13.15.3 SR17 — the GROUP INDICATE clause shall not be specified
      *> in an entry in which the PRESENT WHEN clause is specified (GROUP
      *> INDICATE IS a fixed-condition PRESENT WHEN, §13.18.28.4 GR1 —
      *> COBOLNET1559).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWPWGIP10RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rwpwgi-p10rp.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 W-F PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1.
       01 D-1 TYPE DE.
          03 LINE PLUS 1.
             05 COLUMN 1 PIC X(2) VALUE "GI"
                GROUP INDICATE
                PRESENT WHEN W-F = 1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE D-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
