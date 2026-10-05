      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1221. ISO/IEC 1989:2023 13.18.14.3 SR4: "The keyword
      *> ARE may be specified only if COLUMNS, COLS, or NUMBERS is
      *> specified." (cite.py: OK 13.18.14.3 4)). COLUMN ARE 5 writes
      *> ARE after the SINGULAR spelling, so the clause is not COBOL;
      *> the parser refuses it (COBOL0001), as it refuses COLUMNS IS
      *> (SR5) and COLS NUMBER (the printed spelling brace). The
      *> positive twin is
      *> conformance:2002/pb1221_report_keyword_prefixes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1221NC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1221nc.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1.
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN ARE 5 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
