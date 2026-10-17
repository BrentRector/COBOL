*>--- conformance-case
*> id: oo_interface_conformance
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §4.2.2 (conformance checking, the interface leg) via §14.9.23.3 SR4e + §14.8.2 STRICT:
      *> an INVOKE through an INTERFACE-typed receiver resolves the method over the interface's prototype
      *> closure and its USING/RETURNING descriptions are conformance-checked against THE PROTOTYPE at bind
      *> time (the same ONE relation the class-typed path asks — ActivationRelations.MatchViolation and
      *> ParameterViolation for the argument, ParameterConformance.ReturningConformanceReason for the
      *> RETURNING item, kb/Work PB2077, PB3101 — P9 Step 12's positive proof).
      *> The argument is BY REFERENCE PIC 9(4) matching the prototype exactly; RETURNING matches PIC 9(4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOIFC1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CCALCIF
           INTERFACE ICALC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE ICALC.
       01 WS-IN PIC 9(4) VALUE 0007.
       01 WS-OUT PIC 9(4) VALUE 0000.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CCALCIF "NEW" RETURNING C.
           INVOKE C "ADDV" USING WS-IN RETURNING WS-OUT.
           DISPLAY "OUT=" WS-OUT.
           INVOKE C "ADDV" USING WS-IN RETURNING WS-OUT.
           DISPLAY "OUT=" WS-OUT.
           STOP RUN.
       END PROGRAM OOIFC1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ICALC.
       PROCEDURE DIVISION.
       METHOD-ID. ADDV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-V RETURNING LK-R.
       END METHOD ADDV.
       END INTERFACE ICALC.

       IDENTIFICATION DIVISION.
       CLASS-ID. CCALCIF INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE ICALC.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS ICALC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ACC PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. ADDV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-V RETURNING LK-R.
       MAIN.
           ADD LK-V TO ACC.
           MOVE ACC TO LK-R.
       END METHOD ADDV.
       END OBJECT.
       END CLASS CCALCIF.
