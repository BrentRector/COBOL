*>--- conformance-case
*> id: pb815-raising-factory-presence-mismatch
*> rules: [SR-14.9.18.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB815 -- ISO 14.9.18.3 SR4 a): "the presence or absence of the FACTORY phrase is the same in
      *> the data description entry of identifier-1 as in the RAISING phrase of the procedure division header
      *> of the containing source element".  The header lists CN815A WITHOUT the FACTORY phrase; identifier-1
      *> is described FACTORY OF CN815A.  The class matches, the FACTORY axis does not -> COBOLNET0849 (SR4a).
      *> Before PB815 this axis could not be written on the header end at all (RAISING cobolWord+).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PN815A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CN815A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE FACTORY OF CN815A.
       PROCEDURE DIVISION RAISING CN815A.
       MAIN-P.
           SET F TO CN815A.
           GOBACK RAISING F.
       END PROGRAM PN815A.

       IDENTIFICATION DIVISION.
       CLASS-ID. CN815A.
       END CLASS CN815A.
