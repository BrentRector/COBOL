      *> kb/Work R29's legal half - 13.18.38.3 r7's windows, end-to-end: an index-name as an operand
      *> of a SUBSCRIPT expression, a SET amount, a RELATION condition (bare and inside an
      *> expression), and PERFORM VARYING FROM. Each of these bound through the same expression arm
      *> the COMPUTE rejection now screens, so this golden is the over-rejection guard for the
      *> ArithmeticIndexWindow context threading.
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
           IF IX + 1 = 4 DISPLAY "RELEXPR-OK" END-IF
           PERFORM VARYING V FROM IX BY 1 UNTIL V > 4
               CONTINUE
           END-PERFORM
           DISPLAY V.
           STOP RUN.
