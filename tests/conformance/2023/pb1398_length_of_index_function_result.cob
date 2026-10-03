      *> kb/Work PB1398 - the INDEX leg of the function-result LENGTH
      *> golden (2002/pb1398_length_of_function_result carries the
      *> others). ISO 15.2 item 6: an index function has class and
      *> category index; 8.5.2.8 item 2 makes it an index data item, and
      *> 8.4.3.2.1 makes the function-identifier that item, so
      *> 15.50.3 r1 / 15.14.3 r1 ("a data item of any class or category")
      *> admit FUNCTION MAX over index data items as the argument of
      *> LENGTH and BYTE-LENGTH. The item is the index carrier, the one a
      *> USAGE INDEX data item has: 8 positions and 8 bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1398B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-T.
           05  W-E    PIC 9 OCCURS 3 INDEXED BY W-IA W-IB.
       01  W-DA       USAGE INDEX.
       01  W-DB       USAGE INDEX.
       PROCEDURE DIVISION.
           SET W-DA TO W-IA.
           SET W-DB TO W-IB.
           DISPLAY "IX-L "
               FUNCTION LENGTH(FUNCTION MAX(W-DA W-DB)).
           DISPLAY "IX-B "
               FUNCTION BYTE-LENGTH(FUNCTION MAX(W-DA W-DB)).
           DISPLAY "IX-ITEM-L " FUNCTION LENGTH(W-DA).
           STOP RUN.
