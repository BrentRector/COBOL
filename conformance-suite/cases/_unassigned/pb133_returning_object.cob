*>--- conformance-case
*> id: pb133_returning_object
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave B - ISO 14.2.3 GR7 over an OBJECT REFERENCE: the identically-described
      *> returning pair (14.8.3's conforming case a prototype-less CALL realizes) delivers the created
      *> instance to the caller's typed carrier. Derived: O-SET.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORET.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBASE.
       PROCEDURE DIVISION.
       MAIN.
           CALL "MKR" AS NESTED RETURNING O
           IF O NOT = NULL DISPLAY "O-SET" END-IF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MKR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE CBASE.
       PROCEDURE DIVISION RETURNING L-O.
       P.
           INVOKE CBASE "NEW" RETURNING L-O
           GOBACK.
       END PROGRAM MKR.
       END PROGRAM ORET.
       IDENTIFICATION DIVISION.
       CLASS-ID. CBASE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS CBASE.
