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
      *>   O  MOVE FUNCTION PI TO S (I) with EC-BOUND checking enabled: a
      *>      subscripted receiver whose MOVE freezes the sender to
      *>      evaluate it first (§14.6.4) stores the same twelve digits.
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
           MOVE FUNCTION PI TO S (I)
           IF S (I) = P12 DISPLAY "O S (I) EQUAL"
                       ELSE DISPLAY "O S (I) DIFFER " S (I)
           END-IF
           STOP RUN.
