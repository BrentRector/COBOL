*>--- conformance-case
*> id: pb365-use-eo-not-in-repository
*> rules: [SR-14.9.49.3-16, SR-14.9.49.3-17]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.49.3 SR16: "Object-class-name-1 shall be the name of a class specified in the REPOSITORY
*> paragraph."  ISO §8.4.6.4 is the general rule it instantiates: "The object-class-name of an object class
*> referenced within a source element shall be either the name of the containing object class definition or
*> declared in the REPOSITORY paragraph of that or a containing source element."
*>
*> THIS PROGRAM HAS NO REPOSITORY PARAGRAPH AT ALL, and its declarative names a class defined later in the
*> SAME compilation group.  Membership of the group is not membership of the REPOSITORY, and until kb/Work
*> PB365 the compiler tested the group: this program compiled with ZERO diagnostics, and the miss diagnostic
*> named the wrong condition out loud ("does not name a class of the compilation group").
*>
*> Rejected at 2002/2014/2023 — every edition at which Format 4 exists.  Below 2002 the FORMAT does not exist
*> and its introduction gate (COBOLNET0876, use-after-exception-object-2002) answers first with a different
*> code, which is why 85 is not listed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB365NOREP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       EO-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CNR365.
       EO-P.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM PB365NOREP.

       IDENTIFICATION DIVISION.
       CLASS-ID. CNR365.
       END CLASS CNR365.
