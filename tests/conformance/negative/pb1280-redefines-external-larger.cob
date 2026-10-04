      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1280 - SR8 - a level-1 data-name-2 WITH the EXTERNAL clause does not exempt a larger redefiner.
      *>   cite.py --check 13.18.44.3 "The storage area required for the subject of the entry shall not be larger than the storage area required" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) EXTERNAL.
       01 B REDEFINES A PIC X(8).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
