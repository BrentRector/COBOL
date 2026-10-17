      *>--- conformance-case
      *> id: pb867_raise_checking_not_enabled
      *> rules: [GR-14.9.29.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB867 - RAISE EXCEPTION of a condition whose checking
      *> is NOT enabled raises nothing: no declarative runs, the last
      *> exception status is unchanged and EXCEPTION-OBJECT is not
      *> nulled. With checking enabled, all three happen.
      *> ISO 14.9.29.4 1) "If exception-name-1 is specified, the
      *>   associated exception condition is raised, and
      *>   EXCEPTION-OBJECT is set to null." -> OK 14.9.29.4 1)
      *> ISO 14.6.13.1.1 "when the general rules for a statement
      *>   indicate that a specific exception condition exists, it is
      *>   raised only if checking for that exception condition is
      *>   enabled"; "By default, checking is not enabled for any
      *>   exception condition"; and for EC-USER "Checking is not
      *>   enabled unless the TURN directive or WHEN phrase of the
      *>   PERFORM statement is used to enable checking for the
      *>   exception" -> OK 14.6.13.1.1. A raise is one event: "the
      *>   associated exception condition is raised, the last
      *>   exception status is set to indicate that exception
      *>   condition, and the predefined object reference
      *>   EXCEPTION-OBJECT is set to null" -> OK 14.6.13.1.1.
      *> ISO 14.6.13.1.4 "If checking is not enabled, execution
      *>   continues as if the exception did not occur" -> OK.
      *> ISO 7.3.25.4 6) TURN ... ON enables checking "for the
      *>   procedure division statements ... that follow" -> OK.
      *> ISO 14.6.13.1.5 1), 2) RAISE identifier sets EXCEPTION-OBJECT
      *>   and "The last exception status is set to indicate that an
      *>   exception object has been raised"; "If an exception object
      *>   is raised by a RAISE statement, the associated declarative
      *>   is executed" -> OK (an object raise is not TURN-gated).
      *> ISO 15.33.3 1) EXCEPTION-STATUS is 31 characters: "the
      *>   exception-name or the value 'EXCEPTION-OBJECT', as
      *>   applicable", spaces for no exception -> OK.
      *> DERIVATION of every .out line:
      *>   C1     un-TURNed RAISE: not raised, the declarative does
      *>          not run, the status is still "no exception" (spaces).
      *>   OBJ    RAISE E (an exception object): its declarative runs.
      *>   C2     un-TURNed RAISE after it: the status still says
      *>          EXCEPTION-OBJECT and EXCEPTION-OBJECT is still E.
      *>   DECL   TURN ON, RAISE: the declarative runs and sees the
      *>          name (14.6.13.1.1, 15.33.3).
      *>   C3     after it: the status names the condition and
      *>          EXCEPTION-OBJECT is null (14.9.29.4 1)).
      *>   C4     TURN OFF again, RAISE: not raised - the status keeps
      *>          the PREVIOUS name, it is not reset or re-set.
      *> 2002 dir: RAISE, >>TURN and the EC model are 2002
      *>   introductions; the rule is the same at 2002/2014/2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB867M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB867XE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E USAGE OBJECT REFERENCE PB867XE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PB867.
       U-P.
           DISPLAY "DECL [" FUNCTION EXCEPTION-STATUS "]".
       O-SEC SECTION.
           USE AFTER EXCEPTION OBJECT PB867XE.
       O-P.
           DISPLAY "OBJ".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           RAISE EXCEPTION EC-USER-PB867
           DISPLAY "C1 [" FUNCTION EXCEPTION-STATUS "]"
           INVOKE PB867XE "NEW" RETURNING E
           RAISE E
           RAISE EXCEPTION EC-USER-PB867
           DISPLAY "C2 [" FUNCTION EXCEPTION-STATUS "]"
           IF EXCEPTION-OBJECT = E
               DISPLAY "C2 EO-STILL-E"
           ELSE
               DISPLAY "C2 EO-CHANGED"
           END-IF
       >>TURN EC-USER-PB867 CHECKING ON
           RAISE EXCEPTION EC-USER-PB867
           DISPLAY "C3 [" FUNCTION EXCEPTION-STATUS "]"
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "C3 EO-NULL"
           ELSE
               DISPLAY "C3 EO-NOT-NULL"
           END-IF
       >>TURN EC-USER-PB867 CHECKING OFF
           RAISE EXCEPTION EC-USER-PB867
           DISPLAY "C4 [" FUNCTION EXCEPTION-STATUS "]"
           STOP RUN.
       END PROGRAM PB867M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB867XE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB867XE.
