*>--- conformance-case
*> id: pb26_ec_argument_function_every_statement
*> rules: [AR-15.56.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 item 14 - "If the evaluation of an argument results in an
      *> incorrect value for that argument or for the returned value according to
      *> the rules specified in the function definition and no exception condition
      *> was raised during item identification or expression evaluation, the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist."
      *>
      *> READ WHAT THAT RULE QUALIFIES ON: the FUNCTION REFERENCE. There is no
      *> statement-kind qualification anywhere in it, so the identical reference
      *> must raise in every statement that can contain it (fix-queue PB26).
      *>
      *> IT DID NOT. The ambient checking gate was emitted only for the statement
      *> kinds enumerated in a hand-written switch ending in `_ => false`, so
      *> FUNCTION LOG10(0) raised in COMPUTE, MOVE, DISPLAY and IF and was SILENT
      *> in STRING - a wrong answer that depended on which verb enclosed it. The
      *> switch is now a generated walk over each statement's own value nodes, so a
      *> statement kind added later is covered without an edit.
      *>
      *> 15.56.3 AR2: LOG10's argument "shall be greater than zero", so LOG10(0)
      *> violates it. Each line below prints its statement kind, then CAUGHT from
      *> the declarative - a kind whose CAUGHT is missing is the defect returning.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB26ECEVERY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC S9(4)V99 VALUE 0.
       01 W-T PIC X(20) VALUE SPACES.
       01 W-U PIC X(20) VALUE "AB CD".
       01 W-P1 PIC X(4) VALUE SPACES.
       01 W-P2 PIC X(4) VALUE SPACES.
       01 W-Z PIC 9 VALUE 0.
       01 W-I PIC 9(4) VALUE 0.
       01 W-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "1-COMPUTE".
           COMPUTE W-R = FUNCTION LOG10(W-Z).
           DISPLAY "2-MOVE".
           MOVE FUNCTION LOG10(W-Z) TO W-R.
           DISPLAY "3-DISPLAY".
           DISPLAY FUNCTION LOG10(W-Z).
           DISPLAY "4-IF".
           IF FUNCTION LOG10(W-Z) > 0
               CONTINUE
           END-IF.
           DISPLAY "5-NESTED-ARG".
           COMPUTE W-R = FUNCTION ABS(FUNCTION LOG10(W-Z)).
      *> 6 - THE ONE THAT WAS SILENT. STRING holds its sending operands in a LIST
      *> OF HELPER RECORDS, two hops from the statement - the shape the old switch
      *> had no arm for and a one-level walk still misses.
           DISPLAY "6-STRING".
           STRING FUNCTION LOG10(W-Z) DELIMITED BY SIZE INTO W-T.
           DISPLAY "7-UNSTRING".
      *> The delimiter must be CATEGORY ALPHANUMERIC (14.9.48.3 SR2 -
      *> kb/Work PB155's screen; numeric LOG10 was illegal here), so the
      *> EC-ARGUMENT probe in this position is CHAR(0) - 15.15.3 AR2
      *> requires an argument greater than zero and within the collating
      *> sequence, and 0 violates it exactly as LOG10(0) violated
      *> 15.56.3 AR2.
           UNSTRING W-U DELIMITED BY FUNCTION CHAR(W-Z)
               INTO W-P1 W-P2.
           DISPLAY "8-PERFORM-UNTIL".
           PERFORM UNTIL FUNCTION LOG10(W-Z) >= 0
               EXIT PERFORM
           END-PERFORM.
           DISPLAY "DONE".
           STOP RUN.
