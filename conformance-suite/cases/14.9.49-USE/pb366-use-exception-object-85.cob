*>--- conformance-case
*> id: pb366-use-exception-object-85
*> rules: [GR-14.9.49.4-14]
*> source-format: free
*> portable: unverified
*>---
      *> The COBOL-2002 edition gate on USE AFTER EXCEPTION OBJECT (Format 4,
      *> ISO §14.9.49.2) - COBOLNET0876.  The positive twin
      *> tests/conformance/2002/pb366_use_exception_object_factory.cob proves
      *> the SAME source is accepted and selects its declaratives at 2002,
      *> 2014 and 2023; this fixture holds the gating diagnostic at 85.
      *> kb/Work PB366.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB366B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PBGBASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BASE-SEC SECTION.
           USE AFTER EXCEPTION OBJECT PBGBASE.
       BASE-P.
           DISPLAY "BASE-HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET U TO PBGBASE.
           RAISE U.
           DISPLAY "AFTER-1".
           STOP RUN.
       END PROGRAM PB366B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PBGBASE.
       END CLASS PBGBASE.
