      *> kb/Work PB2466 (and PB1902) - ISO 13.18.45.4 GR2: "When the THROUGH phrase is specified,
      *>   data-name-1 defines an alphanumeric group item that includes all elementary items starting
      *>   with data-name-2 ..." - the alias is a storage window, addressed as an alphanumeric group
      *>   whatever the classes of the leaves under it.
      *>   cite.py: OK  13.18.45.4 2)  (General rules)
      *>   13.18.45.3 SR10 asks only that the area be "an integral number of bytes":
      *>   cite.py: OK  13.18.45.3 10)  (Syntax rules)
      *> A national character position is two bytes (D-N1), so an alphanumeric REDEFINES view can
      *> leave a window byte that only HALF a national character covers:
      *>   RA RENAMES X1 THRU T: X1 is bytes 2-3 of REC, byte 4 is the second byte of N's second
      *>     character (GX is 3 bytes, G is 4), T is bytes 5-6 - an INTERIOR half character (PB2466).
      *>   RB RENAMES N1 THRU Y2: bytes 1-3 of REC2 - the window ENDS on the first byte of N2 (PB1902).
      *>   RC RENAMES Z1 THRU T3: bytes 2-8 of REC3 - byte 4 is the second byte of NT(2), a table
      *>     cell, then NT(3) whole and T3.
      *> Values: MOVE "WXYZEF" TO REC puts W X Y Z E F in its six bytes, so RA reads XYZEF (5 bytes);
      *> MOVE "abcde" TO RA leaves REC = Wabcde. RB reads the first three of REC2's bytes, and
      *> MOVE "klm" TO RB leaves REC2 = klmSTU. RC reads the last seven of REC3's bytes, and a MOVE to
      *> it pads with spaces to its 7 bytes. Every byte is written and read through the alias exactly
      *> as the alphanumeric views see it.
      *> Before the fix each alias was refused COBOLNET1655 "the record's leaves do not tile the
      *> alias's storage window".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2466H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 G.
             10 N PIC N(2).
          05 GX REDEFINES G.
             10 X0 PIC X.
             10 X1 PIC X(2).
          05 T PIC X(2).
       66 RA RENAMES X1 THRU T.
       01 REC2.
          05 G2.
             10 N1 PIC N.
             10 N2 PIC N.
          05 GX2 REDEFINES G2.
             10 Y1 PIC X.
             10 Y2 PIC X(2).
             10 Y3 PIC X.
          05 T2 PIC X(2).
       66 RB RENAMES N1 THRU Y2.
       01 REC3.
          05 G3.
             10 NT PIC N OCCURS 3.
          05 GX3 REDEFINES G3.
             10 Z0 PIC X.
             10 Z1 PIC X(2).
          05 T3 PIC X(2).
       66 RC RENAMES Z1 THRU T3.
       01 W5 PIC X(5).
       PROCEDURE DIVISION.
           MOVE "WXYZEF" TO REC.
           DISPLAY FUNCTION LENGTH(RA) " " RA.
           MOVE "abcde" TO RA.
           DISPLAY REC "|" X1 "|" T.
           MOVE RA TO W5.
           DISPLAY W5.
           MOVE "PQRSTU" TO REC2.
           DISPLAY FUNCTION LENGTH(RB) " " RB.
           MOVE "klm" TO RB.
           DISPLAY REC2 "|" Y1 "|" Y2 "|" Y3.
           MOVE "ABCDEFGH" TO REC3.
           DISPLAY FUNCTION LENGTH(RC) " " RC.
           MOVE "1234567" TO RC.
           DISPLAY REC3 "|" Z1 "|" T3.
           MOVE "xyz" TO RC.
           DISPLAY REC3.
           STOP RUN.
