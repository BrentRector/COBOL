      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1954 - ISO 8.5.1.3.2 (cite.py OK): "All items that are immediately subordinate to a given group item
      *> shall be described using numerically equal level-numbers greater than the level-number used to describe that
      *> group item." The two LINE entries (05, 03) are both immediately subordinate to the level-01 report group
      *> entry D (ISO 13.18.33.1, cite.py OK: the same level-number hierarchy as a data description entry). Before the
      *> fix this compiled clean and printed two lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB1954RPTLINE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "w37bpb1954b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D TYPE DE.
           05  LINE PLUS 1.
               07  COLUMN 1 PIC X VALUE "A".
           03  LINE PLUS 1.
               07  COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
