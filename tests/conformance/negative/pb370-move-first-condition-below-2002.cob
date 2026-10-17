      *> reject-at: 85
      *> The REJECT half of conformance:2002/pb370_move_first_condition
      *> (kb/Work PB370). Which declarative a MOVE whose two operands
      *> both raise selects is ISO §14.9.49.4 GR11's question, and it
      *> exists only through the exception-condition facility: the
      *> >>TURN directive (ISO 7.3.25) and USE AFTER EXCEPTION CONDITION
      *> are ISO/IEC 1989:2002 introductions, so at COBOL-85 this source
      *> does not describe a program and the compiler refuses the FIRST
      *> construct the edition does not have - the >>TURN line - with
      *> COBOLNET0900 rather than running the MOVE unchecked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370MN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 X  PIC X(8) VALUE "ABCDEFGH".
       01 I  PIC 99 VALUE 9.
       01 L  PIC 99 VALUE 50.
       PROCEDURE DIVISION.
       MAIN-P.
       >>TURN EC-BOUND CHECKING ON
           MOVE X (1:L) TO T (I)
           STOP RUN.
