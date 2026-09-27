      *> ISO 8.8.4.2.12 (kb/Work PB1469): two strongly-typed group items compare ELEMENT BY ELEMENT - "each
      *> elementary item of the first operand is compared with the corresponding elementary item of the second
      *> operand, in accordance with the rules for comparison of elementary items and in the order in which the
      *> elementary items are specified", until a pair is unequal. Each expected line is that rule applied by
      *> hand: a numeric pair by ALGEBRAIC value (8.8.4.2.4 - a FLOAT, a SIGN SEPARATE and a PACKED-DECIMAL leaf
      *> alike), a character pair by 8.8.4.2.7/.9, a pointer pair for equality only (8.8.4.2.3 SR4, 8.8.4.2.16),
      *> and an OCCURS DEPENDING table over its current occurrences only (13.18.38.4 GR8). Before PB1469 the
      *> relation was a whole-group IMAGE comparison: F1/F3 printed GE/LE (a float image does not order
      *> algebraically), every signed-leaf ordering was refused COBOLNET0899, and P1 = P2 aborted the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRONG-GROUP-ELEMENT-ORDER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F-T TYPEDEF STRONG.
          05 FX USAGE FLOAT-LONG.
       01 FA TYPE F-T.
       01 FB TYPE F-T.
       01 S-T TYPEDEF STRONG.
          05 SX PIC S9(3).
       01 SA TYPE S-T.
       01 SB TYPE S-T.
       01 M-T TYPEDEF STRONG.
          05 MA PIC X(2).
          05 MS PIC S9(3) SIGN LEADING SEPARATE.
          05 MF USAGE FLOAT-SHORT.
          05 MG.
             10 MN PIC N(2).
             10 MT PIC S9(2) USAGE PACKED-DECIMAL OCCURS 2.
       01 A TYPE M-T.
       01 B TYPE M-T.
       01 P-T TYPEDEF STRONG.
          05 PX USAGE PROGRAM-POINTER.
          05 PY PIC X.
       01 P1 TYPE P-T.
       01 P2 TYPE P-T.
       01 O-T TYPEDEF STRONG.
          05 OC PIC 9.
          05 OX PIC X OCCURS 1 TO 3 DEPENDING ON OC.
       01 C TYPE O-T.
       01 D TYPE O-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *>   -5 < 3 and -2 > -300 algebraically (8.8.4.2.4).
           MOVE -5 TO FX OF FA
           MOVE 3 TO FX OF FB
           IF FA < FB DISPLAY "F1 LT" ELSE DISPLAY "F1 GE" END-IF
           MOVE -2 TO FX OF FA
           MOVE -300 TO FX OF FB
           IF FA > FB DISPLAY "F3 GT" ELSE DISPLAY "F3 LE" END-IF
           MOVE -5 TO SX OF SA
           MOVE 3 TO SX OF SB
           IF SA < SB DISPLAY "S1 LT" ELSE DISPLAY "S1 GE" END-IF
      *>   MA equal, so MS decides: -7 < 2.
           MOVE "AB" TO MA OF A MA OF B
           MOVE -7 TO MS OF A
           MOVE 2 TO MS OF B
           IF A < B DISPLAY "M1 LT" ELSE DISPLAY "M1 GE" END-IF
           IF A >= B DISPLAY "M2 GE" ELSE DISPLAY "M2 LT" END-IF
      *>   Everything equal up to MT(2): -3 < 4 decides.
           MOVE 2 TO MS OF A
           MOVE 1.5 TO MF OF A MF OF B
           MOVE N"XY" TO MN OF A MN OF B
           MOVE 5 TO MT OF A (1) MT OF B (1)
           MOVE -3 TO MT OF A (2)
           MOVE 4 TO MT OF B (2)
           IF A < B DISPLAY "M3 LT" ELSE DISPLAY "M3 GE" END-IF
           IF A = B DISPLAY "M4 EQ" ELSE DISPLAY "M4 NE" END-IF
      *>   All pairs equal: equal, <=, not >, and an EVALUATE pairing (14.9.13.4 GR2).
           MOVE 4 TO MT OF A (2)
           IF A = B DISPLAY "M5 EQ" ELSE DISPLAY "M5 NE" END-IF
           IF A <= B DISPLAY "M6 LE" ELSE DISPLAY "M6 GT" END-IF
           IF A > B DISPLAY "M7 GT" ELSE DISPLAY "M7 LE" END-IF
           EVALUATE A
               WHEN B DISPLAY "M8 EQ"
               WHEN OTHER DISPLAY "M8 NE"
           END-EVALUATE
      *>   Both program-pointers hold their initial NULL: equal; then PY differs.
           IF P1 = P2 DISPLAY "P1 EQ" ELSE DISPLAY "P1 NE" END-IF
           MOVE "Z" TO PY OF P1
           IF P1 NOT = P2 DISPLAY "P2 NE" ELSE DISPLAY "P2 EQ" END-IF
      *>   Two current occurrences each: "B" < "C" at OX(2); OX(3) of C is not compared.
           MOVE 2 TO OC OF C OC OF D
           MOVE "A" TO OX OF C (1) OX OF D (1)
           MOVE "B" TO OX OF C (2)
           MOVE "C" TO OX OF D (2)
           MOVE "Z" TO OX OF C (3)
           IF C < D DISPLAY "O1 LT" ELSE DISPLAY "O1 GE" END-IF
           MOVE "C" TO OX OF C (2)
           IF C = D DISPLAY "O2 EQ" ELSE DISPLAY "O2 NE" END-IF
      *>   The DEPENDING items are compared first (they precede the table, 13.18.38.3 SR20): 3 > 2.
           MOVE 3 TO OC OF C
           IF C > D DISPLAY "O3 GT" ELSE DISPLAY "O3 LE" END-IF
           STOP RUN.
