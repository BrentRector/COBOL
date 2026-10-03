      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1285. ISO/IEC 1989:2023 13.18.46.3 SR2: "Each report-name-1 may appear in only one REPORT
      *> clause." (cite.py OK), and 13.14.3 SR1: "There shall be one and only one REPORT clause specifying
      *> report-name-1 in a given file description entry." Both FDs name R-A (COBOLNET2714); before PB1285 the
      *> second was ignored and the report went to the first file alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1285N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1285-n2.rpt".
           SELECT RPT2 ASSIGN TO "pb1285-n2b.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-A.
       FD RPT2 REPORT IS R-A.
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
