      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1280 - SR2 - a level-77 data-name-2 and a level-01 subject (both roots, so name resolution alone cannot tell them apart).
      *>   cite.py --check 13.18.44.3 "The level-numbers of data-name-2 and the subject of the entry shall be identical" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N0.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 A PIC X(4) VALUE "ABCD".
       01 B REDEFINES A PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
