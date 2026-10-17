      *> kb/Work PB3261. ISO §14.9.49.4 GR11: "The exception condition
      *> that is used to determine which USE statement to select is the
      *> first one that occurs in the order of evaluation of the
      *> statement that causes the exception to exist." Every leg below
      *> fails on BOTH sides, the sending side with EC-BOUND-REF-MOD
      *> (L = 50 into an 8-position X) and the receiving side with
      *> EC-BOUND-SUBSCRIPT (I = 9 into OCCURS 3), so the declarative
      *> that runs names the side the statement evaluates first.
      *>   MOVE: §14.9.25.4 GR1, identifier-1's reference modifier "is
      *>     evaluated only once, immediately before data is moved to
      *>     the first of the receiving operands", and "Item
      *>     identification for identifier-2 is performed immediately
      *>     before the data is moved" - the sender first, whichever
      *>     side carries which failure (M1-M5).
      *>   Arithmetic: §14.7.7 rule 4, the initial evaluation first,
      *>     then "Item identification for the receiving data items is
      *>     done as each data item is accessed" (A1-A4).
      *>   STRING: no timing of its own, so §14.6.4 7): "the
      *>     identifiers within a statement are evaluated in left to
      *>     right order" - identifier-1 and the delimiter before
      *>     identifier-3 (S1-S2).
      *>   INITIALIZE: the same §14.6.4 7) order puts identifier-1, the
      *>     receiver, before the REPLACING sender (I1).
      *>   EVALUATE: the NONFATAL EC-RANGE-INVALID of the first WHEN's
      *>     reversed THRU range occurs before the second WHEN's
      *>     subscript fails, so its declarative runs first; it
      *>     returns (§14.6.13.1.4) and the fatal EC-BOUND-SUBSCRIPT
      *>     follows (E1).
      *> The V legs run the same shapes in range and check the values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3261SF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 NB.
          05 N PIC 9(4) OCCURS 3 TIMES.
       01 GB.
          05 G OCCURS 3 TIMES.
             10 G1 PIC X(2).
             10 G2 PIC X(2).
       01 X  PIC X(8) VALUE "ABCDEFGH".
       01 Y  PIC X(8) VALUE "12345678".
       01 I  PIC 99 VALUE 9.
       01 L  PIC 99 VALUE 50.
       01 Z  PIC 99 VALUE 0.
       01 J  PIC 99 VALUE 2.
       01 K  PIC 99 VALUE 3.
       01 W  PIC 9(4) VALUE 1.
       01 V  PIC X VALUE "M".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-SUB-P.
           DISPLAY "  EC-BOUND-SUBSCRIPT"
           RESUME AT NEXT STATEMENT.
       D-REF SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       D-REF-P.
           DISPLAY "  EC-BOUND-REF-MOD"
           RESUME AT NEXT STATEMENT.
       D-RNG SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-INVALID.
       D-RNG-P.
           DISPLAY "  EC-RANGE-INVALID".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-BOUND EC-RANGE CHECKING ON
           DISPLAY "M1 MOVE X(1:L) TO T(I)"
           MOVE X(1:L) TO T(I)
           DISPLAY "M2 MOVE T(I) TO X(1:L)"
           MOVE T(I) TO X(1:L)
           DISPLAY "M3 MOVE T(I) TO X(Z:1)"
           MOVE T(I) TO X(Z:1)
           DISPLAY "M4 MOVE X(1:L) TO N(I)"
           MOVE X(1:L) TO N(I)
           DISPLAY "M5 MOVE X(1:L) TO G(I)"
           MOVE X(1:L) TO G(I)
           DISPLAY "A1 COMPUTE N(I) = NUMVAL(X(1:L))"
           COMPUTE N(I) = FUNCTION NUMVAL(X(1:L))
           DISPLAY "A2 ADD NUMVAL(X(1:L)) TO N(I)"
           ADD FUNCTION NUMVAL(X(1:L)) TO N(I)
           DISPLAY "A3 DIVIDE NUMVAL(X(1:L)) INTO N(I)"
           DIVIDE FUNCTION NUMVAL(X(1:L)) INTO N(I)
           DISPLAY "A4 ADD NUMVAL(X(1:L)) TO W GIVING N(I)"
           ADD FUNCTION NUMVAL(X(1:L)) TO W GIVING N(I)
           DISPLAY "S1 STRING X(1:L) INTO T(I)"
           STRING X(1:L) DELIMITED BY SIZE INTO T(I)
           DISPLAY "S2 STRING X DELIMITED BY X(1:L) INTO T(I)"
           STRING X DELIMITED BY X(1:L) INTO T(I)
           DISPLAY "I1 INITIALIZE T(I) REPLACING BY X(1:L)"
           INITIALIZE T(I) REPLACING ALPHANUMERIC DATA BY X(1:L)
           DISPLAY "E1 EVALUATE V WHEN Z THRU A WHEN T(I)"
           EVALUATE V
               WHEN "Z" THRU "A" DISPLAY "  W1"
               WHEN T(I) DISPLAY "  W2"
               WHEN OTHER DISPLAY "  W3"
           END-EVALUATE
           MOVE SPACES TO TB
           MOVE ZERO TO NB
           MOVE 3 TO L
           MOVE X(1:L) TO T(J)
           MOVE X(K:2) TO G(J)
           COMPUTE N(J) = FUNCTION NUMVAL(Y(1:L))
           ADD FUNCTION NUMVAL(Y(K:2)) TO N(J)
           ADD FUNCTION NUMVAL(Y(1:2)) TO W GIVING N(K)
           STRING Y(1:L) DELIMITED BY SIZE INTO T(K)
           DISPLAY "V [" T(J) "] [" G(J) "] [" T(K) "] "
               N(J) " " N(K)
           DISPLAY "END"
           STOP RUN.
