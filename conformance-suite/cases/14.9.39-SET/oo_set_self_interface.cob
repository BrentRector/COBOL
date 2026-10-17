*>--- conformance-case
*> id: oo_set_self_interface
*> rules: [SR-14.9.39.3-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 SR10d2: SET … TO SELF into a receiver described with an interface-name is legal
      *> exactly when the INSTANCE definition containing the SET "is described with an IMPLEMENTS clause that
      *> references int-1". CSELFY's OBJECT does IMPLEMENT ISELFY, so the SET conforms and SELF widens to the
      *> interface view. The positive control for the SR10d2 rejection in
      *> negative/oo-set-self-interface-not-implemented — the two differ only by the IMPLEMENTS clause, which
      *> is what makes this pair a test of the RULE rather than of the program.
      *> The interface view is then INVOKEd back through, so the widening is proved to reach a working
      *> receiver rather than merely to bind: expected output is PONG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOSELFY1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CSELFY
           INTERFACE ISELFY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE CSELFY.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CSELFY "NEW" RETURNING C.
           INVOKE C "GRAB".
           STOP RUN.
       END PROGRAM OOSELFY1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ISELFY.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE ISELFY.

       IDENTIFICATION DIVISION.
       CLASS-ID. CSELFY INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE ISELFY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS ISELFY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE ISELFY.
       PROCEDURE DIVISION.
       METHOD-ID. GRAB.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
           INVOKE R "PING".
       END METHOD GRAB.

       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PONG".
       END METHOD PING.
       END OBJECT.
       END CLASS CSELFY.
