      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1285. ISO/IEC 1989:2023 13.14.3 SR1: "There shall be one and only one REPORT clause specifying
      *> report-name-1 in a given file description entry." (cite.py OK). R-B has a report description entry and
      *> no file description entry names it (COBOLNET2714, which replaced the not-implemented code this arm
      *> carried).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1285N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1285-n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-A.
       REPORT SECTION.
       RD R-A.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "A".
       RD R-B.
       01 DET-B TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-A
           GENERATE DET-A
           TERMINATE R-A
           CLOSE RPT
           STOP RUN.
