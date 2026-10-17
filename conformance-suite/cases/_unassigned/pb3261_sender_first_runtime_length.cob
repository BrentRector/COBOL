      *>--- conformance-case
      *> id: pb3261_sender_first_runtime_length
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3261 review R1-1. ISO §14.9.49.4 GR11: "The
      *> exception condition that is used to determine which USE
      *> statement to select is the first one that occurs in the order
      *> of evaluation of the statement that causes the exception to
      *> exist." Every failing leg below fails on BOTH sides, so the
      *> declarative that runs names the side evaluated first. The
      *> sending side's reference modifier has INTEGER-LITERAL
      *> positions, and still fails at run time: ISO §8.4.3.3.4 5) c),
      *> "The sum of leftmost-position and length minus the value one
      *> shall be less than or equal to the number of positions in the
      *> data item referenced by identifier-1", and
      *>   D1-D4: DL is DYNAMIC LENGTH holding "AB", so DL(1:5) is out
      *>     of range only for its CURRENT length;
      *>   F1: X is PIC X(8), so X(1:20) is out of range by its
      *>     description - it binds, with its warning, because
      *>     EC-BOUND-REF-MOD checking is on.
      *> The receiving side is T(I) / N(I) with I = 9 into OCCURS 3.
      *>   MOVE: §14.9.25.4 GR1 - identifier-1's reference modifier "is
      *>     evaluated only once, immediately before data is moved to
      *>     the first of the receiving operands" (D1, D4, F1), and
      *>     the sender T(I) before the receiver DL(1:5) (R1).
      *>   STRING: §14.6.4 7) - "the identifiers within a statement are
      *>     evaluated in left to right order" (D2).
      *>   Arithmetic: §14.7.7 4) - the initial evaluation first, then
      *>     "Item identification for the receiving data items is done
      *>     as each data item is accessed" (D3).
      *> The V legs run the same shapes in range and check the values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3261RL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DL PIC X DYNAMIC LENGTH LIMIT 20.
       01 X  PIC X(8) VALUE "ABCDEFGH".
       01 I  PIC 99 VALUE 9.
       01 J  PIC 99 VALUE 2.
       01 TB.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 NB.
          05 N PIC 9(4) OCCURS 3 TIMES.
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
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-BOUND CHECKING ON
           MOVE "AB" TO DL
           MOVE ALL "-" TO TB
           MOVE ZERO TO NB
           DISPLAY "D1 MOVE DL(1:5) TO T(I)"
           MOVE DL(1:5) TO T(I)
           DISPLAY "D2 STRING DL(1:5) INTO T(I)"
           STRING DL(1:5) DELIMITED BY SIZE INTO T(I)
           DISPLAY "D3 COMPUTE N(I) = FUNCTION NUMVAL(DL(1:5))"
           COMPUTE N(I) = FUNCTION NUMVAL(DL(1:5))
           DISPLAY "D4 MOVE DL(1:5) TO T(I) T(J)"
           MOVE DL(1:5) TO T(I) T(J)
           DISPLAY "F1 MOVE X(1:20) TO T(I)"
           MOVE X(1:20) TO T(I)
           DISPLAY "R1 MOVE T(I) TO DL(1:5)"
           MOVE T(I) TO DL(1:5)
           DISPLAY "V1 MOVE DL(1:2) TO T(J)"
           MOVE DL(1:2) TO T(J)
           DISPLAY "  T(J)=" T(J)
           DISPLAY "V2 STRING DL(2:1) INTO T(J)"
           MOVE SPACES TO T(J)
           STRING DL(2:1) DELIMITED BY SIZE INTO T(J)
           DISPLAY "  T(J)=" T(J)
           DISPLAY "V3 COMPUTE N(J) = FUNCTION NUMVAL(DL(1:2))"
           MOVE "42" TO DL
           COMPUTE N(J) = FUNCTION NUMVAL(DL(1:2))
           DISPLAY "  N(J)=" N(J)
           DISPLAY "V4 MOVE X(3:4) TO T(J)"
           MOVE X(3:4) TO T(J)
           DISPLAY "  T(J)=" T(J)
           DISPLAY "END"
           STOP RUN.
