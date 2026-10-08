      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1954 - ISO 8.5.1.3.2 (cite.py OK): "All items that are immediately subordinate to a given group item
      *> shall be described using numerically equal level-numbers greater than the level-number used to describe that
      *> group item." ISO 13.18.33.1 (cite.py OK): level numbers 1 through 49 indicate position "within the hierarchical
      *> structure described by a data description entry, a report group description entry, or a screen description
      *> entry", so the rule governs report groups too. The two COLUMN entries (05, 04) are both immediately
      *> subordinate to G (03). Before the fix this compiled clean and printed "A B".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB1954RPTSIB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "w37bpb1954a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D TYPE DE LINE PLUS 1.
           03  G.
               05  COLUMN 1 PIC X VALUE "A".
               04  COLUMN 3 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
