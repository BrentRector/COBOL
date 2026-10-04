      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1222 - ISO 13.18.14.3 SR8 c): "If the report line ends in a set of relative printable items or consists only
      *> of such, they shall not cause the page width to be exceeded unless each of them is subject to a different PRESENT WHEN
      *> clause".   cite.py: OK  13.18.14.3 8) c)  (Syntax rules)
      *> 13.18.14.4 GR8: the leftmost column of a relative item is the horizontal counter plus integer-2.
      *> cite.py: OK  13.18.14.4 8)  (General rules)
      *> The first item ends in column 6, so COLUMN PLUS 3 starts at column 9 and its five columns end at 13, past the page width
      *> 12, and the item is subject to no PRESENT WHEN clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W11EPB1222RELATIVETAILPA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W11EPB1222RELATIVETAILPA.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC X VALUE "A".
       01  WB PIC X VALUE "B".
       01  WN PIC 9 VALUE 1.
       01  WS-ON PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES 12 COLUMNS.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(6) VALUE "ABCDEF".
           03  COLUMN PLUS 3 PIC X(5) VALUE "VWXYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
