      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1129 / PB1050. ISO/IEC 1989:2023 13.18.12.3 SR3: "If the CODE clause is specified for any
      *> report, it shall be specified for each report associated with the same report file." (cite.py OK).
      *> R-A carries CODE and R-B, on the same file, does not (COBOLNET2713).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1129N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1129-n4.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORTS ARE R-A R-B.
       REPORT SECTION.
       RD R-A CODE IS "A".
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "A".
       RD R-B.
       01 DET-B TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-A R-B
           GENERATE DET-A
           GENERATE DET-B
           TERMINATE R-A R-B
           CLOSE RPT
           STOP RUN.
