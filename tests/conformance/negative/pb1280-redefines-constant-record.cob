      *> reject-at: 2002 2014 2023
      *> kb/Work PB1280 - SR13 - a CONSTANT RECORD redefined, then modified through the redefining name.
      *>   cite.py --check 13.18.44.3 "The data description entry for data-name-2 shall not contain the CONSTANT RECORD clause" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A CONSTANT RECORD.
          05 X PIC X(4) VALUE "ABCD".
       01 B REDEFINES A PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
