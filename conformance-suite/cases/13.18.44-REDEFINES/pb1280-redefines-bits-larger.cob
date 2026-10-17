*>--- conformance-case
*> id: pb1280-redefines-bits-larger
*> rules: [SR-13.18.44.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1280 - SR8 - the area is measured in BITS (13.18.44.4 GR1): 8 bits over 4, inside one byte.
      *>   cite.py --check 13.18.44.3 "The storage area required for the subject of the entry shall not be larger than the storage area required" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC 1(4) USAGE BIT.
          05 B REDEFINES A PIC 1(8) USAGE BIT.
          05 C PIC X.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
