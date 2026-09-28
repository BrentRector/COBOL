      *> reject-at: 2002 2014 2023
      *> kb/Work PB1284 — ISO/IEC 1989:2023 §13.18.45.3 SR10: "The area described by data-name-2 THROUGH data-name-3
      *> shall define an integral number of bytes." A (3 bits) and B (2 bits) are same-level USAGE BIT items, which
      *> §8.5.1.6.3 packs into one byte, so A THRU B is a 5-bit area. Before the fix it compiled and X2 displayed
      *> 10111.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1284RB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC 1(3) USAGE BIT VALUE B"101".
          05 B PIC 1(2) USAGE BIT VALUE B"11".
          05 C PIC 1(3) USAGE BIT VALUE B"000".
       66 X2 RENAMES A THRU B.
       PROCEDURE DIVISION.
           DISPLAY "[" X2 "]".
           STOP RUN.
