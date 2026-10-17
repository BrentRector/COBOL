*>--- conformance-case
*> id: oo_method_renames
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.7 / §13.18.45 — level-66 RENAMES in a METHOD's LOCAL-STORAGE (M2-OO-1h step 1). The alias
      *> resolves FROM/THRU structurally within the method's own record, independent of method-name scoping.
      *> BOTH renames A-PART THRU B-PART (the 6-char span); JUSTA renames A-PART (inherits PIC XXX).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OORENM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS AGG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE AGG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE AGG "NEW" RETURNING A.
           INVOKE A "DOIT".
           STOP RUN.
       END PROGRAM OORENM.

       IDENTIFICATION DIVISION.
       CLASS-ID. AGG INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 REC.
          05 A-PART PIC XXX VALUE "ABC".
          05 B-PART PIC XXX VALUE "DEF".
       66 BOTH RENAMES A-PART THRU B-PART.
       66 JUSTA RENAMES A-PART.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BOTH=" BOTH.
           DISPLAY "JUSTA=" JUSTA.
           MOVE "XYZ" TO JUSTA.
           DISPLAY "REC=" REC.
           GOBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS AGG.
