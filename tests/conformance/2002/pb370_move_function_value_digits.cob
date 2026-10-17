      *> ISO §14.9.25.4 GR1 writes a MOVE with several receiving
      *> operands out as an equivalence - "MOVE a (b) TO b, c (b)" is
      *> "equivalent to: MOVE a (b) TO temp / MOVE temp TO b / MOVE temp
      *> to c (b)", where 'temp' is "an intermediate result item provided
      *> by the implementor" - so each receiver of a multi-receiver MOVE
      *> holds what a single-receiver MOVE of the same sender stores in
      *> it. For a numeric function-identifier the intermediate is the
      *> §15.4 temporary, and it must keep every digit a receiver takes
      *> (kb/Work PB370):
      *>   F  MOVE FUNCTION PI TO S1 S2 S3 E1: each receiver equals the
      *>      single-receiver MOVE into its twin - twelve fraction digits
      *>      into PIC 9V9(12), five into PIC 9V9(5), fourteen into the
      *>      numeric-edited PIC 9.9(14).
      *>   I  MOVE FUNCTION NUMVAL (25 digits) TO B1 B2: each receiver
      *>      holds the 25-digit value NUMVAL returns, "the numeric
      *>      value represented by argument-1" (§15.67.4 1), as the
      *>      single receiver B3 does.
      *>   O  MOVE FUNCTION PI TO S (I) S (3) with EC-BOUND checking
      *>      enabled: two subscripted receivers store the same twelve
      *>      digits.
      *>   L  MOVE FUNCTION PI TO F (I) F (3), FLOAT-LONG receivers: each
      *>      stores the binary64 value the single-receiver MOVE into its
      *>      twin G stores, every significant digit of it.
      *>   M  MOVE FUNCTION EXP (100) TO F (I) F (3): a magnitude (about
      *>      2.69E+43) no 38-digit fixed-point intermediate holds.
      *>   N  MOVE FUNCTION NUMVAL ("1.23456789012345") TO F (I) F (3): an
      *>      exact-valued function, fourteen fraction digits.
      *>   Q  MOVE FUNCTION SQRT (2) TO H (I) H (3), FLOAT-SHORT receivers.
      *>   R  MOVE FUNCTION PI TO F1 F2: two FLOAT-LONG receivers.
      *>   X  MOVE FUNCTION PI TO F1 S1: a FLOAT-LONG receiver beside a
      *>      PIC 9V9(12) one - each equals its single-receiver twin.
      *>   Y  MOVE FUNCTION EXP (100) TO D (I) D (3), floating-point
      *>      numeric-edited receivers PIC +9.9(9)E+99 (no fixed scale):
      *>      the same edited image their twin DQ takes, +2.688117141E+43.
      *>   Z  MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO W (I)
      *>      W (3), PIC +9.9(20)E+99: an exact-valued function into a 21-digit
      *>      edited significand, truncated there (§14.6.8.4 2) to
      *>      +1.23456789012345678901E+00 as its twin WQ is - more digits
      *>      than a binary64 intermediate carries.
      *>   K  The same value TO WQ WS, a 21-digit and a 6-digit edited
      *>      receiver: each equals its single-receiver twin.
      *>   V  MOVE FUNCTION PI TO W (I) W (3): a binary64 value into the
      *>      21-digit edited receivers equals its twin.
      *>   P  MOVE FUNCTION NUMVAL ("-1.2345678901234567890123") TO F1 WQ:
      *>      a FLOAT-LONG beside an edited receiver - each equals its twin.
      *>   J  The 22-digit NUMVAL value TO SL (I) SL (3) and TO SL1 SL2,
      *>      all PIC S9(3)V9(28): every one of the 28 fraction digits the
      *>      receivers take (the returned value's 21 integer digits would
      *>      leave seventeen within a 38-digit intermediate).
      *>   B  MOVE FUNCTION NUMVAL ("1234567890123456789012.123456789")
      *>      TO XB YB, PIC 9(31) and PIC V9(28): 59 digit positions
      *>      between them, more than a 38-digit fixed-point intermediate
      *>      holds, and each receiver equals its single-receiver twin -
      *>      all 22 integer digits in XB and all nine fraction digits in
      *>      YB. The negative value into PIC S9(31) and PIC SV9(28) too,
      *>      and an INTEGER function into PIC 9(31) beside PIC V9(28).
      *>   W  MOVE FUNCTION INTEGER (I) TO V1 V2, PIC V99: an integer
      *>      value into all-fraction receivers stores zero in each.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370FD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC 9V9(12).
       01 S2 PIC 9V9(12).
       01 S3 PIC 9V9(5).
       01 E1 PIC 9.9(14).
       01 P12 PIC 9V9(12).
       01 P5 PIC 9V9(5).
       01 PE PIC 9.9(14).
       01 B1 PIC 9(25).
       01 B2 PIC 9(25).
       01 B3 PIC 9(25).
       01 ST.
          05 S PIC 9V9(12) OCCURS 3 TIMES.
       01 FT.
          05 F USAGE FLOAT-LONG OCCURS 3 TIMES.
          05 H USAGE FLOAT-SHORT OCCURS 3 TIMES.
       01 G  USAGE FLOAT-LONG.
       01 GH USAGE FLOAT-SHORT.
       01 F1 USAGE FLOAT-LONG.
       01 F2 USAGE FLOAT-LONG.
       01 DT.
          05 D PIC +9.9(9)E+99 OCCURS 3 TIMES.
       01 DQ PIC +9.9(9)E+99.
       01 WT.
          05 W PIC +9.9(20)E+99 OCCURS 3 TIMES.
       01 WQ PIC +9.9(20)E+99.
       01 WQ2 PIC +9.9(20)E+99.
       01 WS PIC -9.9(5)E+99.
       01 WS2 PIC -9.9(5)E+99.
       01 SLT.
          05 SL PIC S9(3)V9(28) OCCURS 3 TIMES.
       01 SL1 PIC S9(3)V9(28).
       01 SL2 PIC S9(3)V9(28).
       01 SLQ PIC S9(3)V9(28).
       01 V1 PIC V99.
       01 XB PIC 9(31).
       01 YB PIC V9(28).
       01 XBQ PIC 9(31).
       01 YBQ PIC V9(28).
       01 XS PIC S9(31).
       01 YS PIC SV9(28).
       01 XSQ PIC S9(31).
       01 YSQ PIC SV9(28).
       01 V2 PIC V99.
       01 I  PIC 99 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION PI TO P12
           MOVE FUNCTION PI TO P5
           MOVE FUNCTION PI TO PE
           MOVE FUNCTION PI TO S1 S2 S3 E1
           IF S1 = P12 AND S2 = P12 DISPLAY "F S1 S2 EQUAL"
                                ELSE DISPLAY "F S1 S2 DIFFER " S1 " " S2
           END-IF
           IF S3 = P5 DISPLAY "F S3 EQUAL"
                      ELSE DISPLAY "F S3 DIFFER " S3
           END-IF
           IF E1 = PE DISPLAY "F E1 EQUAL"
                      ELSE DISPLAY "F E1 DIFFER " E1
           END-IF
           MOVE FUNCTION NUMVAL ("1234567890123456789012345") TO B1 B2
           MOVE FUNCTION NUMVAL ("1234567890123456789012345") TO B3
           DISPLAY "I " B1 " " B2 " " B3
           >>TURN EC-BOUND CHECKING ON
           MOVE FUNCTION PI TO S (I) S (3)
           IF S (I) = P12 AND S (3) = P12 DISPLAY "O S (I) EQUAL"
                       ELSE DISPLAY "O S (I) DIFFER " S (I)
           END-IF
           MOVE FUNCTION PI TO F (I) F (3)
           MOVE FUNCTION PI TO G
           IF F (I) = G AND F (3) = G DISPLAY "L F (I) EQUAL " F (I)
                     ELSE DISPLAY "L F (I) DIFFER " F (I) " " G
           END-IF
           MOVE FUNCTION EXP (100) TO F (I) F (3)
           MOVE FUNCTION EXP (100) TO G
           IF F (I) = G AND F (3) = G DISPLAY "M F (I) EQUAL"
                     ELSE DISPLAY "M F (I) DIFFER " F (I) " " G
           END-IF
           MOVE FUNCTION NUMVAL ("1.23456789012345") TO F (I) F (3)
           MOVE FUNCTION NUMVAL ("1.23456789012345") TO G
           IF F (I) = G AND F (3) = G DISPLAY "N F (I) EQUAL"
                     ELSE DISPLAY "N F (I) DIFFER " F (I) " " G
           END-IF
           MOVE FUNCTION SQRT (2) TO H (I) H (3)
           MOVE FUNCTION SQRT (2) TO GH
           IF H (I) = GH AND H (3) = GH DISPLAY "Q H (I) EQUAL"
                      ELSE DISPLAY "Q H (I) DIFFER " H (I) " " GH
           END-IF
           MOVE FUNCTION PI TO F1 F2
           MOVE FUNCTION PI TO G
           IF F1 = G AND F2 = G DISPLAY "R F1 F2 EQUAL"
                             ELSE DISPLAY "R F1 F2 DIFFER " F1 " " F2
           END-IF
           MOVE FUNCTION PI TO F1 S1
           IF F1 = G AND S1 = P12 DISPLAY "X F1 S1 EQUAL"
                               ELSE DISPLAY "X F1 S1 DIFFER " F1 " " S1
           END-IF
           MOVE FUNCTION EXP (100) TO D (I) D (3)
           MOVE FUNCTION EXP (100) TO DQ
           IF D (I) = DQ AND D (3) = DQ DISPLAY "Y D (I) EQUAL " D (I)
                      ELSE DISPLAY "Y D (I) DIFFER " D (I) " " DQ
           END-IF
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123")
             TO W (I) W (3)
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO WQ
           IF W (I) = WQ AND W (3) = WQ DISPLAY "Z W (I) EQUAL " W (I)
                      ELSE DISPLAY "Z W (I) DIFFER " W (I) " " WQ
           END-IF
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO WS
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO WQ2 WS2
           IF WQ2 = WQ AND WS2 = WS DISPLAY "K WQ2 WS2 EQUAL " WS2
                       ELSE DISPLAY "K WQ2 WS2 DIFFER " WQ2 " " WS2
           END-IF
           MOVE FUNCTION PI TO W (I) W (3)
           MOVE FUNCTION PI TO WQ
           IF W (I) = WQ AND W (3) = WQ DISPLAY "V W (I) EQUAL"
                      ELSE DISPLAY "V W (I) DIFFER " W (I) " " WQ
           END-IF
           MOVE FUNCTION NUMVAL ("-1.2345678901234567890123") TO G
           MOVE FUNCTION NUMVAL ("-1.2345678901234567890123") TO WQ
           MOVE FUNCTION NUMVAL ("-1.2345678901234567890123") TO F1 WQ2
           IF F1 = G AND WQ2 = WQ DISPLAY "P F1 WQ2 EQUAL " WQ2
                              ELSE DISPLAY "P F1 WQ2 DIFFER " F1 " " WQ2
           END-IF
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO SLQ
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123")
             TO SL (I) SL (3)
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO SL1 SL2
           IF SL (I) = SLQ AND SL (3) = SLQ AND SL1 = SLQ AND SL2 = SLQ
                DISPLAY "J SL (I) SL1 SL2 EQUAL"
           ELSE DISPLAY "J SL (I) SL1 SL2 DIFFER " SL (I) " " SL1
           END-IF
           MOVE 0.5 TO V1 V2
           MOVE FUNCTION INTEGER (I) TO V1 V2
           IF V1 = 0 AND V2 = 0 DISPLAY "W V1 V2 ZERO"
                            ELSE DISPLAY "W V1 V2 DIFFER " V1 " " V2
           END-IF
           MOVE FUNCTION NUMVAL ("1234567890123456789012.123456789")
             TO XBQ
           MOVE FUNCTION NUMVAL ("1234567890123456789012.123456789")
             TO YBQ
           MOVE FUNCTION NUMVAL ("1234567890123456789012.123456789")
             TO XB YB
           IF XB = XBQ AND YB = YBQ DISPLAY "B XB YB EQUAL " XB " " YB
                        ELSE DISPLAY "B XB YB DIFFER " XB " " YB
           END-IF
           MOVE FUNCTION NUMVAL ("-1234567890123456789012.123456789")
             TO XSQ
           MOVE FUNCTION NUMVAL ("-1234567890123456789012.123456789")
             TO YSQ
           MOVE FUNCTION NUMVAL ("-1234567890123456789012.123456789")
             TO XS YS
           IF XS = XSQ AND YS = YSQ DISPLAY "B XS YS EQUAL " XS " " YS
                        ELSE DISPLAY "B XS YS DIFFER " XS " " YS
           END-IF
           MOVE FUNCTION INTEGER (1234567890123456789012345678901)
             TO XBQ
           MOVE 0.5 TO YB
           MOVE FUNCTION INTEGER (1234567890123456789012345678901)
             TO XB YB
           IF XB = XBQ AND YB = 0 DISPLAY "B XB YB INTEGER EQUAL " XB
                       ELSE DISPLAY "B XB YB INTEGER DIFFER " XB " " YB
           END-IF
           STOP RUN.
