*>--- conformance-case
*> id: ec_oo_null_invoke
*> rules: [GR-14.9.23.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.23.4 GR5: "If identifier-1 is null, the EC-OO-NULL exception condition is set to exist and
      *> execution of the INVOKE statement is terminated." Table 13: Fatal, so the USE declarative runs and
      *> RESUME AT NEXT STATEMENT (§14.9.33) keeps the run unit alive past the terminated INVOKE.
      *>
      *> O is an object reference that is never given an instance, so INVOKE O "PING" invokes on null. Before
      *> this fix the runtime threw uncaught: no statement guard was emitted around an INVOKE, so the
      *> declarative could not select and the run unit died with the condition unhandled.
      *>
      *> ⚠ The raise stays UNCONDITIONAL whether or not checking is enabled — GR5 terminates the INVOKE and a
      *> typed-native model has no conforming value to return from an invocation that did not happen. What
      *> checking ON adds is the guard that lets the named condition reach a declarative, which is what this
      *> golden proves.
      >>TURN EC-OO-NULL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECOONULL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPING.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPING.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-NULL.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE O "PING".
           DISPLAY "AFTER".
           STOP RUN.
       END PROGRAM ECOONULL.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPING.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS CPING.
