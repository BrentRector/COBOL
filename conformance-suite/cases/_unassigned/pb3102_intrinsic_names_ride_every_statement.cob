*>--- conformance-case
*> id: pb3102_intrinsic_names_ride_every_statement
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3102 (the model is PB1762, D13 of the conditions-and-exceptions design) - the exception-names a
      *> function reference can raise ride EVERY statement that holds the reference, because the raise belongs to
      *> the DETECTION (ISO 14.6.13.1.1: "If checking for an exception condition is enabled and an exception status
      *> indicator is set as a result of an exception detected during the execution of a statement, the associated
      *> exception condition is raised"), never to the statement kind. EC-ORDER-NOT-SUPPORTED (15.85.4 rule 2: "the
      *> cultural ordering table is not available on the processor") is the FATAL name here: "NO SUCH TABLE" is legal
      *> source (12.3.7.4 GR17 leaves the allowable content to the implementor) that this processor cannot resolve.
      *> Table 13 makes it Fatal, so 14.6.13.1.3 #5 runs the declarative and RESUME AT NEXT STATEMENT (14.9.33.4 GR2)
      *> continues AFTER the interrupted statement - so each statement below is interrupted and its receiving item,
      *> and every phrase that would have run on a result, are untouched. The reference is held in STRING, EVALUATE,
      *> SEARCH (a WHEN condition), a subscript and an IF whose NESTED statement holds the reference; the nested case
      *> is its own statement (kb/Work PB441), so its declarative runs ONCE (14.6.13.1.3 #5 - one raise, one
      *> selection), the enclosing IF raising nothing of its own.
       >>TURN EC-ORDER-NOT-SUPPORTED CHECKING ON
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3102INT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE BADT IS "NO SUCH TABLE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T PIC X(8) VALUE "UNCHANGD".
       01 W-N PIC 9 VALUE 9.
       01 W-R PIC S9(4)V99 VALUE 7.
       01 W-Z PIC 9 VALUE 0.
       01 TBL.
          05 E PIC 9 OCCURS 3 TIMES INDEXED BY IX VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-ORDER-NOT-SUPPORTED.
       H1-P.
           DISPLAY "  ORDER " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       H2 SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H2-P.
           DISPLAY "  ARG " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "1-STRING".
           STRING FUNCTION STANDARD-COMPARE("a" "b" BADT)
               DELIMITED BY SIZE INTO W-T.
           DISPLAY "  W-T=[" W-T "]".
           DISPLAY "2-EVALUATE".
           EVALUATE FUNCTION STANDARD-COMPARE("a" "b" BADT)
               WHEN "<" DISPLAY "  LESS"
               WHEN OTHER DISPLAY "  OTHER"
           END-EVALUATE.
           DISPLAY "3-SEARCH".
           SET IX TO 1.
           SEARCH E
               WHEN FUNCTION STANDARD-COMPARE("a" "b" BADT) = "<"
                   DISPLAY "  FOUND"
           END-SEARCH.
           DISPLAY "4-SUBSCRIPT".
           MOVE E(FUNCTION ORD(FUNCTION STANDARD-COMPARE("a" "b" BADT)))
               TO W-N.
           DISPLAY "  W-N=[" W-N "]".
           DISPLAY "5-NESTED".
           IF W-Z = 0
               MOVE FUNCTION LOG10(W-Z) TO W-R
           END-IF.
           DISPLAY "  W-R=[" W-R "]".
           DISPLAY "DONE".
           STOP RUN.
