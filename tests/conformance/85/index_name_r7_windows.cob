      *> kb/Work R29's legal half - 13.18.38.3 r7's windows, end-to-end: an index-name as an operand
      *> of a SUBSCRIPT expression, a SET amount, a bare RELATION operand, and PERFORM VARYING FROM.
      *> Each of these bound through the same expression arm the COMPUTE rejection now screens, so this
      *> golden is the over-rejection guard for the ArithmeticIndexWindow context threading. (An index-name
      *> INSIDE a relation's arithmetic expression, `IF IX + 1 = 4`, was listed here until kb/Work PB2018: r7
      *> and 8.8.4.2.13 name the relation's operand, not an operand of an expression, so it is the negative
      *> pb2018-index-name-in-relation-expression.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R29WIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG.
          05 T PIC 9 OCCURS 5 TIMES INDEXED BY IX.
       01 V PIC 9(4).
       PROCEDURE DIVISION.
           SET IX TO 2
           MOVE 7 TO T(IX + 1)
           DISPLAY T(IX + 1)
           SET IX UP BY 1
           IF IX = 3 DISPLAY "REL-OK" END-IF
           PERFORM VARYING V FROM IX BY 1 UNTIL V > 4
               CONTINUE
           END-PERFORM
           DISPLAY V.
           STOP RUN.
