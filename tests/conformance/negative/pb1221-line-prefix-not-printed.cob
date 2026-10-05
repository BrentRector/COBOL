      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1221. ISO/IEC 1989:2023 13.18.35.2 Format 1 (the PDF
      *> diagram rendered) prints the LINE clause's keyword prefix as
      *> the three-way brace {LINE NUMBER IS | LINE NUMBERS ARE |
      *> LINES ARE} (cite.py: OK 13.18.35.2 "NUMBERS"). LINES IS pairs
      *> the plural spelling with IS, which no alternative prints, so
      *> the parser refuses it (COBOL0001), as it refuses LINES NUMBER
      *> IS and LINE NUMBER ARE. 13.18.35.3 SR2 ("LINE and LINES are
      *> synonyms", cite.py: OK 13.18.35.3 2)) gives the two words one
      *> meaning, not each other's companions. The positive twin is
      *> conformance:2002/pb1221_report_keyword_prefixes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1221NL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1221nl.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1.
       01 DET-A TYPE DE.
          02 LINES IS PLUS 1.
             03 COLUMN 5 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
