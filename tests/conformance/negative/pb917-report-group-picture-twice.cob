      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB917 - ISO 5.2.6.2 and 5.2.7 (cite.py OK on both).  13.15.2 prints [ picture-clause ] once with no ellipsis, so a report group description entry that
      *> writes PICTURE twice is non-conforming.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB917RGPICTWICE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W13PPB917RGPICTWICE.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X PIC 9 VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
