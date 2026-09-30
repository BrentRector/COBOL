      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1830 - ISO 13.18.54.3 SR5: "If the addend is identifier-1, it shall specify a numeric data item
      *> not defined in the report section." (cite.py --check 13.18.54.3 "it shall specify a numeric data item"). A
      *> USAGE INDEX item is class INDEX, never numeric (8.5.2.1 Table 2), so it is no SUM addend; its storage
      *> PICTURE (category numeric, zero digits) used to pass the screen. Expected: COBOLNET2045 at every edition
      *> that has the Report Writer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1830SI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1830.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  IX USAGE INDEX.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT 10 LINES.
       01  DL TYPE DETAIL LINE 1.
           05 COLUMN 1 PIC 9(4) SUM IX.
       PROCEDURE DIVISION.
           STOP RUN.
