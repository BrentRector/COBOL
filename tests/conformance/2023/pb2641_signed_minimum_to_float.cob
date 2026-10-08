      *> kb/Work PB2641 - 14.6.8.3 GR2: the algebraic value of a numeric operand is converted to a floating-point
      *> receiving item "in a manner consistent with the specifications of ISO/IEC 60559".  The lowest algebraic
      *> value of a signed 16-byte COMP-5 item is -2**127 (13.18.60.4 GR12, the container range; 15.43.4 gives
      *> LOWEST-ALGEBRAIC of it).  The scaled-value converter negated that value to take its magnitude, -2**127
      *> negated is itself, so it took the "exact small value" path and returned 0.  EXACT, hand-derived:
      *>   2**127 = 170141183460469231731687303715884105728
      *>   S  PIC S9(29)V99 COMP-5 holds -2**127 / 100 = -1.70141183460469231731687303715884105728E36
      *>   N  PIC S9(31)    COMP-5 holds -2**127       = -1.70141183460469231731687303715884105728E38
      *>   multiplying by the COMP-2 item E = 1 keeps the operand an exact-converted float operand, so the result is
      *>   the correctly rounded binary64 / binary32 of the value, between -1.7015E+36 and -1.7014E+36 (resp. E+38).
      *>   ATAN of a magnitude that large is -pi/2 = -1.570796 (to six places).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2641FM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC S9(29)V99 COMP-5.
       01 N  PIC S9(31) COMP-5.
       01 E  COMP-2 VALUE 1.
       01 D  COMP-2.
       01 F  COMP-1.
       01 R  PIC -9.9(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LOWEST-ALGEBRAIC(S) TO S
           MOVE FUNCTION LOWEST-ALGEBRAIC(N) TO N
           COMPUTE D = S * E
           IF D < -1.7014E+36 AND D > -1.7015E+36
               DISPLAY "T1 binary64 scaled operand ok"
           ELSE
               DISPLAY "T1 WRONG"
           END-IF
           COMPUTE F = S * E
           IF F < -1.7014E+36 AND F > -1.7015E+36
               DISPLAY "T2 binary32 scaled operand ok"
           ELSE
               DISPLAY "T2 WRONG"
           END-IF
           COMPUTE F = N * E
           IF F < -1.7014E+38 AND F > -1.7015E+38
               DISPLAY "T3 binary32 integer operand ok"
           ELSE
               DISPLAY "T3 WRONG"
           END-IF
           COMPUTE D = FUNCTION ATAN(S)
           MOVE D TO R
           DISPLAY "T4 atan " R
           MOVE N TO F
           IF F < -1.7014E+38 AND F > -1.7015E+38
               DISPLAY "T5 MOVE to binary32 ok"
           ELSE
               DISPLAY "T5 WRONG"
           END-IF
           MOVE S TO F
           IF F < -1.7014E+36 AND F > -1.7015E+36
               DISPLAY "T6 MOVE scaled to binary32 ok"
           ELSE
               DISPLAY "T6 WRONG"
           END-IF
           STOP RUN.
