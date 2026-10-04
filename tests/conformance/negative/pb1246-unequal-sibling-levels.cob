      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1246 - ISO 8.5.1.3.2 (cite.py OK): "All items that are immediately subordinate to a given group item shall be described using numerically equal
      *> level-numbers greater than the level-number used to describe that group item."  A (05) and B (03) are both immediately subordinate to G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1246UNEQUALSIB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A PIC X VALUE "A".
           03  B PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY A B.
           STOP RUN.
