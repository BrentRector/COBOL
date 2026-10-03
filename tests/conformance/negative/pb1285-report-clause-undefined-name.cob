      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1285. ISO/IEC 1989:2023 13.18.46.3 SR1: "Each report-name-1 shall be the subject of a report
      *> description entry in the report section of the same source element." (cite.py OK). The REPORT clause
      *> names R-NONE, which no RD defines (COBOLNET2714); before PB1285 it compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1285N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1285-n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORTS ARE R-A R-NONE.
       REPORT SECTION.
       RD R-A.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-A
           GENERATE DET-A
           TERMINATE R-A
           CLOSE RPT
           STOP RUN.
