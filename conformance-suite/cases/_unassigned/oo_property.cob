*>--- conformance-case
*> id: oo_property
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOPR1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CACC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE CACC1.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CACC1 "NEW" RETURNING A.
           INVOKE A "SHOW".
           INVOKE A "BUMP".
           INVOKE A "SHOW".
           STOP RUN.
       END PROGRAM OOPR1.

       IDENTIFICATION DIVISION.
       CLASS-ID. CACC1 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 T PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 42 TO T
           ADD T TO BAL.
       END METHOD BUMP.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY BAL.
       END METHOD SHOW.
       END OBJECT.
       END CLASS CACC1.
