*>--- conformance-case
*> id: pb232-entry-convention-contained
*> rules: [SR-11.9.7.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §11.9.7.3 SR1: "The ENTRY-CONVENTION clause may be specified only in a class
      *> definition, a function definition, a function-prototype definition, an interface definition, a program
      *> prototype definition, or a program definition that is not contained within another program." Here it
      *> is written in a CONTAINED program, even with the one convention provided (COBOL), so the placement rule
      *> alone refuses it: COBOLNET2385 (kb/Work PB232). Below 2014 the clause does not exist (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56ECCN.
       PROCEDURE DIVISION.
           CALL "W56ECCI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W56ECCI.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED"
           GOBACK.
       END PROGRAM W56ECCI.
       END PROGRAM W56ECCN.
