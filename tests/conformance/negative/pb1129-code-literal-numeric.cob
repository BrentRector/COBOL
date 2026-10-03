      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1129. ISO/IEC 1989:2023 13.18.12.3 SR1: "Literal-1 shall be an alphanumeric literal."
      *> (cite.py OK). CODE IS 12 writes a numeric literal (COBOLNET2713).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1129N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1129-n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1 CODE IS 12.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
