      *> kb/Work PB1969 -- ADDRESS OF G (n:m) over a group that holds an
      *> OCCURS DEPENDING ON table is range-checked against the group's
      *> CURRENT extent, the area a read of the same reference measures.
      *> 8.4.3.3.4 5): a value "that references a position outside the
      *> area of identifier-1" sets EC-BOUND-REF-MOD; 13.18.38.4 GR8: the
      *> group "operand" is its current-count part, here N positions.
      *> With N = 3 the group G is 3 positions long though its maximum
      *> is 8, so G (3:1) is legal, G (5:1) and G (2:3) are outside the
      *> area and set the fatal EC-BOUND-REF-MOD under checking, the same
      *> as the read G (5:1) does.  The USE AFTER EXCEPTION declarative
      *> reports it and RESUME AT NEXT STATEMENT skips the SET, so P keeps
      *> its previous value.  Raising N to 6 makes position 5 legal.
      *> A NATIONAL group (positions are national characters) and a BIT
      *> group (positions are bits, 8.4.3.3.4 GR5 a)) are bounded the same
      *> way by their own current extent.
      >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1969ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 3.
       01 K PIC 9 VALUE 5.
       01 G.
          05 GT PIC X OCCURS 1 TO 8 DEPENDING ON N.
       01 W PIC X.
       01 P USAGE POINTER.
       01 V1 PIC X BASED.
       01 NN PIC 9 VALUE 2.
       01 NG GROUP-USAGE NATIONAL.
          05 NT PIC N OCCURS 1 TO 4 DEPENDING ON NN.
       01 VN PIC N BASED.
       01 BN PIC 9 VALUE 3.
       01 BG GROUP-USAGE BIT.
          05 BT PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON BN.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "XYZ" TO G
           SET P TO ADDRESS OF G(3:1)
           SET ADDRESS OF V1 TO P
           DISPLAY "AT-END=" V1
           SET P TO NULL
           SET P TO ADDRESS OF G(K:1)
           IF P = NULL
               DISPLAY "P-UNCHANGED"
           END-IF
           MOVE G(K:1) TO W
           SET P TO ADDRESS OF G(2:3)
           IF P = NULL
               DISPLAY "P-UNCHANGED"
           END-IF
           MOVE 6 TO N
           MOVE "UVWXYZ" TO G
           SET P TO ADDRESS OF G(K:1)
           SET ADDRESS OF V1 TO P
           DISPLAY "NOW=" V1
           MOVE N"ab" TO NG
           SET P TO ADDRESS OF NG(2:1)
           SET ADDRESS OF VN TO P
           DISPLAY "NAT=" VN
           SET P TO NULL
           SET P TO ADDRESS OF NG(3:1)
           IF P = NULL DISPLAY "NAT-UNCHANGED" END-IF
           MOVE B"101" TO BG
           SET P TO ADDRESS OF BG(1:2)
           IF P = NULL DISPLAY "BIT-NULL" ELSE DISPLAY "BIT-SET" END-IF
           SET P TO NULL
           SET P TO ADDRESS OF BG(9:1)
           IF P = NULL DISPLAY "BIT-UNCHANGED" END-IF
           STOP RUN.
