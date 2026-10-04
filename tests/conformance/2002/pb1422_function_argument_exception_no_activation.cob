      *> kb/Work PB1422 - the evaluation of a user-defined function's
      *> arguments precedes its activation, and an exception condition
      *> raised there stops the activation.
      *> RULE (8.4.3.2.4 GR6 a): "Each argument-1 is evaluated at the
      *> beginning of the evaluation of the function-identifier. If an
      *> exception condition exists, no function is activated and
      *> execution proceeds as specified in General rule 6f."
      *> GR6 f): "If an exception condition exists, any declarative or
      *> WHEN phrase of a PERFORM statement that is associated with that
      *> exception condition is executed. Execution then proceeds as
      *> defined for the exception condition and execution of the
      *> declarative" - here RESUME AT NEXT STATEMENT (14.9.33).
      *> The argument WA / WB divides by zero (EC-SIZE-ZERO-DIVIDE, on by
      *> the directive), the declarative runs and resumes after the
      *> COMPUTE: FACT never prints ACTIVATED and R keeps its value 7.
      *> With WB = 3 the argument is 5 / 3 = 1 (the no-ROUNDED store of
      *> 14.8.2.3.3 rule 2 a into PIC S9(4)), FACT activates, R = 1.
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1422FACT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 ARG1 PIC S9(4).
       01 RES PIC S9(4).
       PROCEDURE DIVISION USING ARG1 RETURNING RES.
           DISPLAY "ACTIVATED"
           MOVE ARG1 TO RES
           GOBACK.
       END FUNCTION PB1422FACT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1422FARG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1422FACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC S9(4) VALUE 5.
       01 WB PIC S9(4) VALUE 0.
       01 R  PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
       D1A.
           DISPLAY "DECL"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
           DISPLAY "START"
           COMPUTE R = FUNCTION PB1422FACT(WA / WB)
           DISPLAY "R=" R
           MOVE 3 TO WB
           COMPUTE R = FUNCTION PB1422FACT(WA / WB)
           DISPLAY "R=" R
           STOP RUN.
