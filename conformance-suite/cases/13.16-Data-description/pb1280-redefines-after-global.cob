*>--- conformance-case
*> id: pb1280-redefines-after-global
*> rules: [SR-13.16.3-4, SR-13.18.44.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1280 - SR1 - REDEFINES written after GLOBAL, on a group entry.
      *>   cite.py --check 13.18.44.3 "The REDEFINES clause shall immediately follow the entry-name clause" -> OK
      *> COBOLNET2739 (redefines-entry-rule; SR8 is COBOLNET1539, SR10 COBOLNET1654). The positive twin is
      *> conformance/2002/pb1280_redefines_entry_rules_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       01 B GLOBAL REDEFINES A.
          05 B1 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
