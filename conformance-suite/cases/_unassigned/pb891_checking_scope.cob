      *>--- conformance-case
      *> id: pb891_checking_scope
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB891 - checking enablement belongs to the SOURCE TEXT
      *> of the executing statement, so the run-time checking state is
      *> SAVED and RESTORED around each statement, never reset to off.
      *> ISO 7.3.25.4 GR6: checking "is enabled for the procedure
      *> division statements and procedure division headers that follow
      *> in the compilation group". GR5: a TURN inside a statement
      *> "applies to any succeeding statement in the sequence of source
      *> lines, whether or not that succeeding statement is within the
      *> scope of the statement in which the TURN directive is
      *> specified". 14.6.13.1.1: "if checking for an exception
      *> condition is not enabled, the exception condition will not be
      *> raised". 8.4.2.3.4 GR2 sets EC-BOUND-SUBSCRIPT for a subscript
      *> above the highest occurrence number (T has 3; I reaches 4).
      *> RESUME AT NEXT STATEMENT (14.9.33.4 GR2) lands after the
      *> PERFORM whose UNTIL raised, so I is 04 there.
      *> CASE1/CASE2: the MOVE inside the loop body (inline, then a
      *>   performed paragraph) is itself checked; leaving it must not
      *>   turn OFF the PERFORM's own check of T (I) in the UNTIL.
      *>   (Before the fix the UNTIL never raised and the loop ran on.)
      *> CASE3: the MOVE follows a TURN ... OFF inside the IF, so it is
      *>   NOT checked although the IF's own line is.
      *> CASE4: paragraph Q lies in an OFF region; PERFORM Q is checked
      *>   but Q's out-of-range MOVE is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB891SCP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 I   PIC 9(2) VALUE 0.
       01 IDX PIC 9(2) VALUE 5.
       01 R   PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "HANDLED I=" I.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 1 TO T (1) T (2) T (3).
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           PERFORM VARYING I FROM 1 BY 1 UNTIL T (I) > 5
              MOVE T (1) TO R
           END-PERFORM
           DISPLAY "CASE1 I=" I
           PERFORM P VARYING I FROM 1 BY 1 UNTIL T (I) > 5
           DISPLAY "CASE2 I=" I
           IF T (1) = 1
       >>TURN EC-BOUND-SUBSCRIPT CHECKING OFF
              MOVE T (IDX) TO R
              DISPLAY "CASE3 INNER"
           END-IF
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           PERFORM Q
           DISPLAY "CASE4 AFTER"
           STOP RUN.
       P.
           MOVE T (1) TO R.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING OFF
       Q.
           MOVE T (IDX) TO R.
           DISPLAY "CASE4 Q".
