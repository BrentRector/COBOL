*>--- conformance-case
*> id: pb1551-object-view-initialize
*> rules: [SR-8.4.3.5.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - ISO 8.4.3.5.3 SR2: "An object-view shall not be
      *> specified as a receiving operand." (cite.py: OK 8.4.3.5.3 2))
      *> The view of a PLAIN name as INITIALIZE's identifier-1. INITIALIZE of
      *> an object reference is legal (it is set to null), so the view is the
      *> one violation; it used to be a parse error on AS (COBOLNET0901).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551VM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1551VC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-P.
           INITIALIZE U
           INITIALIZE U AS PB1551VC
           STOP RUN.
       END PROGRAM PB1551VM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551VC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB1551VC.
