      *> kb/Work PB1441, PB1394 - a prefixed literal is one token
      *>  whatever its neighbour, and a data item named X, NX, B or BX
      *>  is an ordinary data-name beside it (8.3.5 rule 5: X" B" BX"
      *>  NX" are opening delimiters). Expected, derived: X then X"4142"
      *>  = ZZZAB (8.3.3.2.4 hexadecimal pairs); B then B"01" = QQQ01 (a
      *>  boolean literal DISPLAYs its boolean characters); BX"A" = 1010
      *>  (8.3.3.4.4 GR5, one hex digit is four boolean characters);
      *>  NX"0041" moved to a national item is the national character A
      *>  (8.3.3.5.4 GR4, four digits per character), displayed as A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73APFX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "ZZZ".
       01 B PIC X(3) VALUE "QQQ".
       01 BX PIC X(3) VALUE "BBB".
       01 NX PIC X(3) VALUE "NNN".
       01 BB PIC 1(4) USAGE BIT.
       01 NA PIC N(1).
       PROCEDURE DIVISION.
           DISPLAY X X"4142".
           DISPLAY B B"01".
           MOVE BX"A" TO BB.
           DISPLAY BX " " BB.
           MOVE NX"0041" TO NA.
           DISPLAY NX " " NA.
           STOP RUN.
