*>--- conformance-case
*> id: pb1260-occurs-at-level-77
*> rules: [SR-13.18.38.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1260 - ISO 13.18.38.3 SR1 a): "The OCCURS clause shall
      *>   not be specified in a data description entry that: a) Has a
      *>   level-number of 01, 66, 77, or 88". A is a level-77 entry that
      *>   carries OCCURS (level 77 is a distinct level-number from 01 in
      *>   the sentence), refused COBOLNET2404 at all four editions.
      *> cite.py --check 13.18.38.3 "Has a level-number of 01, 66, 77,
      *>   or 88" -> OK  13.18.38.3 1) a)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1260L77.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 A PIC X OCCURS 3.
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
