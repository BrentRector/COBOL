      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1129. ISO/IEC 1989:2023 13.18.12.3 SR2: "Identifier-1 shall reference an alphanumeric data
      *> item that shall not be an occurs-depending-on group item, a variable-length group, or a
      *> dynamic-length elementary item." (cite.py OK). WS-G is a group holding an OCCURS DEPENDING ON table,
      *> so its length is not fixed (COBOLNET2713).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1129N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1129-n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 2.
       01 WS-G.
          05 WS-T PIC X OCCURS 1 TO 3 TIMES DEPENDING ON WS-N.
       REPORT SECTION.
       RD R-1 CODE IS WS-G.
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
