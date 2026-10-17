      *>--- conformance-case
      *> id: pb2017-operator-in-subscript
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB2017 - ISO 8.7.1 applies to the operator of a
      * subscript's arithmetic expression (8.4.2.3.2) as to any
      * other: T (I+1) compiled and ran.  COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2017N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT PIC X(3) VALUE "123".
       01 T-TABLE REDEFINES TT.
          05 T PIC 9 OCCURS 3 TIMES.
       01 I PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY T (I+1).
           STOP RUN.
       END PROGRAM PB2017N4.
