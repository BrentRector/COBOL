*>--- conformance-case
*> id: pb894-call-via-prototype-definition-arity
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.8.4 GR10 b): the prototype PGNX has no in-group
      *> definition, but it has an in-group program PROTOTYPE definition,
      *> so "the details are taken from that program prototype
      *> definition" -- two formals -- and 14.9.4.3 SR25 applies 14.8.2 to
      *> the CALL: one argument where two formals are declared (14.8.2.1).
      *> Before kb/Work PB894 the prototype definition could not be
      *> written, so GR10 b) had no arm and the arity was unchecked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNX IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-N L-R.
       END PROGRAM PGNX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNXM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PGNX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(4) VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL PGNX USING W-N
           STOP RUN.
       END PROGRAM PGNXM.
