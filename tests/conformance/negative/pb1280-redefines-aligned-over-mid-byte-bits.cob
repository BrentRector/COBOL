      *> reject-at: 2002 2014 2023
      *> kb/Work PB1280 - SR15 - an ALIGNED bit subject (13.18.1.4 GR1: the first bit of a byte) over a bit item that begins at bit 3.
      *>   cite.py --check 13.18.44.3 "The description of the subject of the entry shall be such that its required alignment is the same as the alignment" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC 1(3) USAGE BIT.
          05 B PIC 1(5) USAGE BIT.
          05 C REDEFINES B PIC 1(5) USAGE BIT ALIGNED.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
