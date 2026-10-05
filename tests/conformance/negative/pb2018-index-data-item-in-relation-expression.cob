*> reject-at: 85 2002 2014 2023
      *> kb/Work PB2018 - the index DATA ITEM half. 13.18.60.3 SR10 lets an index data item be referenced in "a
      *> relation condition", the relation's operand; inside an arithmetic expression it is an operand of the
      *> expression, and 8.8.1.1 admits only numeric data items, and an index data item is class index (8.5.2.1
      *> Table 2), never numeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2018IDI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       01 N PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
           IF N < IDX + 1 DISPLAY "UNDER-REJECT" END-IF
           STOP RUN.
