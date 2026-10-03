      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1129. ISO/IEC 1989:2023 13.18.12.3 SR2: "Identifier-1 shall reference an alphanumeric data
      *> item that shall not be an occurs-depending-on group item, a variable-length group, or a
      *> dynamic-length elementary item." (cite.py OK). WS-CD is PIC 99, a numeric item (COBOLNET2713).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1129N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1129-n2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-CD PIC 99 VALUE 12.
       REPORT SECTION.
       RD R-1 CODE IS WS-CD.
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
