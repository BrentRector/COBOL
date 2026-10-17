      *>--- conformance-case
      *> id: pb241_continue_bare_equals_after_zero
      *> rules: [SR-14.9.9.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB241 - ISO 14.9.9.3 SR2: "If the AFTER phrase is not
      *> specified, then the statement is processed as if an AFTER
      *> phrase were specified with arithmetic-expression-1 specified as
      *> zero." The rule's whole content is an EQUIVALENCE, so this
      *> program runs bare CONTINUE and CONTINUE AFTER 0 SECONDS side by
      *> side, under checking ON WITH LOCATION for the one condition the
      *> AFTER phrase can raise, and asserts the two leave the same
      *> observable state. 14.9.9.4 GR1: zero is not "less than zero",
      *> so neither spelling suspends or sets
      *> EC-CONTINUE-LESS-THAN-ZERO; 14.9.9.1: execution continues with
      *> the next statement.
      *>
      *> LEG 1 - from no exception. Each spelling's triple (EXCEPTION-
      *> STATUS, -STATEMENT, -LOCATION) is captured and compared: SAME.
      *> 15.33.3 r1: no exception -> 31 spaces; 15.30.3 r2 a): no
      *> exception -> one space. Fails if either spelling raised.
      *> LEG 2 - after a real raise (CONTINUE AFTER -1 SECONDS, GR1 b):
      *> status EC-CONTINUE-LESS-THAN-ZERO, statement CONTINUE (15.32.3
      *> r2, Table 12), location "PB241CEQ; MAIN; 55" (15.30.3 r2 b) -
      *> the third part is the physical source line, docs/CONFORMANCE.md
      *> 3). The last exception status is a RUN-UNIT entity
      *> (14.6.13.1.1), so a statement that raises nothing leaves it as
      *> it was: each spelling KEEPS the triple. Fails if either
      *> spelling clears or overwrites it.
      *> LEG 3 - inside an exception-checking PERFORM (14.9.28.4 GR17):
      *> neither spelling reaches the WHEN phrase; the control
      *> (AFTER -1) does, which proves the handler is live.
       >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB241CEQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NEG PIC S9 VALUE -1.
       01 SAVED-T.
          05 S-STATUS PIC X(31).
          05 S-STMT   PIC X(63).
          05 S-LOC    PIC X(80).
       01 NOW-T.
          05 N-STATUS PIC X(31).
          05 N-STMT   PIC X(63).
          05 N-LOC    PIC X(80).
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
           PERFORM SAVE-T.
           CONTINUE AFTER 0 SECONDS.
           PERFORM NOW-T-P.
           DISPLAY "1 STATUS [" N-STATUS "] LOC ["
               FUNCTION EXCEPTION-LOCATION "]".
           IF NOW-T = SAVED-T
               DISPLAY "1 BARE AND AFTER0 SAME"
           ELSE
               DISPLAY "1 BARE AND AFTER0 DIFFERENT"
           END-IF.
           CONTINUE AFTER WS-NEG SECONDS.
           PERFORM SAVE-T.
           DISPLAY "2 STATUS [" S-STATUS "]".
           DISPLAY "2 STMT [" FUNCTION TRIM(S-STMT) "]".
           DISPLAY "2 LOC [" FUNCTION TRIM(S-LOC) "]".
           CONTINUE.
           PERFORM NOW-T-P.
           IF NOW-T = SAVED-T
               DISPLAY "2 BARE KEEPS"
           ELSE
               DISPLAY "2 BARE CHANGED"
           END-IF.
           CONTINUE AFTER 0 SECONDS.
           PERFORM NOW-T-P.
           IF NOW-T = SAVED-T
               DISPLAY "2 AFTER0 KEEPS"
           ELSE
               DISPLAY "2 AFTER0 CHANGED"
           END-IF.
           PERFORM
               CONTINUE
               CONTINUE AFTER 0 SECONDS
               DISPLAY "3 NO HANDLER"
           WHEN EC-CONTINUE-LESS-THAN-ZERO
               DISPLAY "3 HANDLER RAN"
           END-PERFORM.
           PERFORM
               CONTINUE AFTER WS-NEG SECONDS
           WHEN EC-CONTINUE-LESS-THAN-ZERO
               DISPLAY "3 CONTROL HANDLED"
           END-PERFORM.
           STOP RUN.
       SAVE-T.
           MOVE FUNCTION EXCEPTION-STATUS TO S-STATUS.
           MOVE FUNCTION EXCEPTION-STATEMENT TO S-STMT.
           MOVE FUNCTION EXCEPTION-LOCATION TO S-LOC.
       NOW-T-P.
           MOVE FUNCTION EXCEPTION-STATUS TO N-STATUS.
           MOVE FUNCTION EXCEPTION-STATEMENT TO N-STMT.
           MOVE FUNCTION EXCEPTION-LOCATION TO N-LOC.
