      *>--- conformance-case
      *> id: pb1177_invoke_self_new_raising
      *> rules: [GR-14.9.18.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1177 (review finding R2-1's sibling sweep) - an
      *> INVOKE SELF "NEW" in an inherited factory method reaches a
      *> subclass's NEW OVERRIDE (11.7.3 SR3; kb/Work PB1582), a user
      *> method like any other. 14.9.18.4 GR1 b): "If the RAISING
      *> phrase is specified, an exception condition is raised in the
      *> activating runtime element if checking for that exception
      *> condition is enabled in the activating runtime element, and
      *> execution continues in that runtime element as specified in
      *> the rules for the activating statement after the result, if
      *> any, of the activated element is returned".
      *> EXPECTED (computed from the rules, not from a run):
      *>   MAKE's USE procedure takes the condition the override's
      *>   GOBACK raised, at the INVOKE SELF "NEW" (MAKE-DECL), and
      *>   RESUME AT NEXT STATEMENT continues after it
      *>   (MAKE-AFTER-NEW); MAKE returns normally (MAIN-AFTER-MAKE)
      *>   with the object, which was returned first (S=SET).
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1177NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE PB1177NC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB1177NC "MAKE" RETURNING S.
           DISPLAY "MAIN-AFTER-MAKE".
           IF S = NULL DISPLAY "S=NULL" ELSE DISPLAY "S=SET" END-IF.
           STOP RUN.
       END PROGRAM PB1177N.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1177NB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK.
       DECLARATIVES.
       HU SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
       HU-P.
           DISPLAY "MAKE-DECL".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-1.
           INVOKE SELF "NEW" RETURNING LK.
           DISPLAY "MAKE-AFTER-NEW".
       END METHOD MAKE.
       END FACTORY.
       END CLASS PB1177NB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1177NC INHERITS FROM PB1177NB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1177NB.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. NEW OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK.
       N-MAIN.
           INVOKE SUPER "NEW" RETURNING LK.
           GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
       END METHOD NEW.
       END FACTORY.
       END CLASS PB1177NC.
