      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1260 - ISO 13.18.38.3 SR1 a): "The OCCURS clause shall
      *>   not be specified in a data description entry that: a) Has a
      *>   level-number of 01, 66, 77, or 88". A is a level-01 entry that
      *>   carries OCCURS, which compiled as a table before the
      *>   clause-placement table's NotAtLevel row (COBOLNET2404). The
      *>   rule has no version proviso, so it is refused at all four
      *>   editions; the legal spelling puts the table under a group.
      *> cite.py --check 13.18.38.3 "Has a level-number of 01, 66, 77,
      *>   or 88" -> OK  13.18.38.3 1) a)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1260L1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X OCCURS 3.
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
