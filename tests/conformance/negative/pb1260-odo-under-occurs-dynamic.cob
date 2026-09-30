      *> reject-at: 2014 2023
      *> kb/Work PB1260 - ISO 13.18.38.3 SR1 b): the OCCURS clause "shall
      *>   not be specified in a data description entry that ... b) Has an
      *>   occurs-depending table subordinate to it". A Format 4 (DYNAMIC)
      *>   table is an OCCURS entry too: G below has an occurs-depending
      *>   table beneath it. (The same shape under a fixed OCCURS was
      *>   already COBOLNET0854; the dynamic arm compiled and aborted at
      *>   run time.)
      *> cite.py --check 13.18.38.3 "The OCCURS clause shall not be
      *>   specified in a data description entry that" -> OK
      *>   13.18.38.3 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1260B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 R.
          05 G OCCURS DYNAMIC FROM 2 CAPACITY IN C.
             10 T PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           STOP RUN.
