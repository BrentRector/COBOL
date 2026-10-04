      *> kb/Work PB1280 - THE LEGAL SPELLINGS NEXT TO THE REDEFINES ENTRY RULES (negatives pb1280-redefines-*).
      *>   13.18.44.3 SR7 + NOTE 1: `05 N / 05 N REDEFINES N / 05 M REDEFINES N` - M names the entry that ORIGINALLY defined
      *>     the area (the first N), so DISPLAY M shows the first character of "AB".
      *>   SR8: a level-1 data-name-2 WITHOUT the EXTERNAL clause may be redefined by a LARGER entry; a smaller or equal
      *>     redefiner is always legal, in bytes and in bits (F is 2 of D's 3 bits).
      *>   SR15 / 8.5.1.6.3: a character subject needs a byte boundary, and K redefines a bit item that BEGINS one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1280OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC X(2) VALUE "AB".
          05 N REDEFINES N PIC X(2).
          05 M REDEFINES N PIC X.
       01 X PIC X(4) VALUE "WXYZ".
       01 Y REDEFINES X PIC X(2).
       01 EX PIC X(4) EXTERNAL.
       01 EY REDEFINES EX PIC X(4).
       01 BIG PIC X(2) VALUE "PQ".
       01 BIG2 REDEFINES BIG PIC X(8).
       01 BITS.
          05 D PIC 1(8) USAGE BIT.
          05 K REDEFINES D PIC X.
          05 E PIC 1(3) USAGE BIT.
          05 F REDEFINES E PIC 1(2) USAGE BIT.
       PROCEDURE DIVISION.
           DISPLAY M " " Y " " BIG
           STOP RUN.
