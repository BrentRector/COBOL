      *>--- conformance-case
      *> id: l1_exit_raising_object_sending
      *> rules: [GR-14.6.13.1.5-1, SR-14.9.14.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.3 SR4 — "Identifier-1 is a sending operand."
      *> The EXIT PROGRAM RAISING statement READS the object reference
      *> named by identifier-1; the operand is never a receiving item
      *> of the statement. §14.6.13.1.5 item 1 states that property as
      *> an effect and is the clause this golden derives from: "The
      *> predefined object reference EXCEPTION-OBJECT is set to the
      *> CONTENT of the object reference specified in the RAISE
      *> statement or the RAISING phrase of the EXIT or GOBACK
      *> statement that caused the exception object to be raised."
      *> So the object placed INTO W-E in L1SUB03 is exactly what
      *> EXCEPTION-OBJECT holds in the activator: the value travels OUT
      *> of identifier-1, which is what "sending operand" means. Had
      *> the statement written to the operand instead, no object could
      *> arrive and OBJECT-SENT could not print.
      *> Expected ORDER, from §14.6.13.1.5's EXIT/GOBACK list: item 1
      *> does not apply (W-E's class CL1ERR IS named in L1SUB03's
      *> procedure division header RAISING phrase, satisfying §14.9.14.3
      *> SR5 a), so no EC-OO-EXCEPTION substitution); item 2 applies —
      *> "if a USE statement in the activating runtime element specifies
      *> an applicable class or interface, the associated declarative is
      *> executed. If execution of the declarative completes normally,
      *> execution continues as specified in the activating statement
      *> for normal execution."  Hence SUB-RAISING (before the EXIT),
      *> then the declarative, then AFTER-CALL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EXT03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CL1ERR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CL1ERR.
       ERR-P.
           DISPLAY "HANDLED".
           SET U TO EXCEPTION-OBJECT.
           IF U NOT = NULL DISPLAY "OBJECT-SENT" END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "L1SUB03".
           DISPLAY "AFTER-CALL".
           STOP RUN.
       END PROGRAM L1EXT03.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SUB03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CL1ERR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E USAGE OBJECT REFERENCE CL1ERR.
       PROCEDURE DIVISION RAISING CL1ERR.
       SUB-P.
           INVOKE CL1ERR "NEW" RETURNING W-E.
           DISPLAY "SUB-RAISING".
           EXIT PROGRAM RAISING W-E.
       END PROGRAM L1SUB03.

       IDENTIFICATION DIVISION.
       CLASS-ID. CL1ERR INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS CL1ERR.
