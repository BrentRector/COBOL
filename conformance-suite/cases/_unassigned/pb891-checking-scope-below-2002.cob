*>--- conformance-case
*> id: pb891-checking-scope-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:cases/_unassigned/pb891_checking_scope (and of
      *> its group twins pb841_checking_across_call and
      *> pb893_close_report_completes). Per-statement checking scopes
      *> exist only through the exception-condition facility: the >>TURN
      *> directive (ISO 7.3.25), USE AFTER EXCEPTION CONDITION and
      *> RESUME are ISO/IEC 1989:2002 introductions, so at COBOL-85 this
      *> source does not describe a program and the compiler refuses the
      *> FIRST construct the edition does not have - the >>TURN line -
      *> with COBOLNET0900 rather than running the statements unchecked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB891N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 I   PIC 9(2) VALUE 0.
       01 R   PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO T (1) T (2) T (3).
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           PERFORM VARYING I FROM 1 BY 1 UNTIL T (I) > 5
              MOVE T (1) TO R
           END-PERFORM
           DISPLAY "I=" I
           STOP RUN.
