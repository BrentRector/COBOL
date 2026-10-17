*>--- conformance-case
*> id: oo_property_explicit_ref
*> rules: [SR-8.4.3.9.3-5, SR-8.4.3.9.3-7, GR-11.7.4-6, GR-11.7.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOPRF2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CACC8
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE CACC8.
       01 W PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CACC8 "NEW" RETURNING A.
           MOVE 7 TO BAL OF A.
           MOVE BAL OF A TO W.
           DISPLAY "W=" W.
           STOP RUN.
       END PROGRAM OOPRF2.

       IDENTIFICATION DIVISION.
       CLASS-ID. CACC8 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAL PIC 9(5).
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           DISPLAY "GET-CALLED".
           MOVE W-BAL TO LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(5).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           DISPLAY "SET-CALLED".
           MOVE LK-V TO W-BAL.
       END METHOD.
       END OBJECT.
       END CLASS CACC8.
