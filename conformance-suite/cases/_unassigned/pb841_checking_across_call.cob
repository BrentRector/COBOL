      *>--- conformance-case
      *> id: pb841_checking_across_call
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB841 - the run-time checking state does not LEAK
      *> ACROSS A CALL. ISO 7.3.25.4 GR6 enables checking "for the
      *> procedure division statements and procedure division headers
      *> that follow in the compilation group", and 14.6.13.1.1: "if
      *> checking for an exception condition is not enabled, the
      *> exception condition will not be raised". PB841CB follows the
      *> TURN ... OFF below, so its out-of-range subscript (8.4.2.3.4
      *> GR2 - T has 3 occurrences, IDX is 5) is NOT checked although
      *> the CALL that activated it is: nothing is raised, B's
      *> EXCEPTION-STATUS is spaces, and A's declarative never runs.
      *> (Before the fix B ran with A's flags and A printed HANDLED.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB841CA.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "A HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           CALL "PB841CB".
       >>TURN EC-BOUND-SUBSCRIPT CHECKING OFF
           DISPLAY "A AFTER".
           STOP RUN.
       END PROGRAM PB841CA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB841CB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE T (IDX) TO R.
           DISPLAY "B STATUS=[" FUNCTION EXCEPTION-STATUS "]".
           GOBACK.
       END PROGRAM PB841CB.
