      *> reject-at: 2002 2014 2023
      *> kb/Work PB1280 - SR10 - the entries giving the new description follow the entries defining the area: a LOCAL-STORAGE 01 does not redefine a WORKING-STORAGE 01.
      *>   cite.py --check 13.18.44.3 "The entries giving the new descriptions of the storage area shall follow the entries defining the area of data-name-2, without intervening entries that define new storage areas" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(2) VALUE "WW".
       LOCAL-STORAGE SECTION.
       01 L REDEFINES A PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
