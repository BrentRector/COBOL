*>--- conformance-case
*> id: oo_method_args
*> rules: [GR-14.9.23.4-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.7/§14.9.23 — INVOKE … USING … RETURNING: an OO instance method that takes a parameter,
      *> mutates per-instance OBJECT data, and returns a value. Two independent objects prove per-instance state:
      *> each ACC accumulates its OWN balance (the whole point of the per-instance ProgramState model).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOARGS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ACC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE ACC.
       01 A2 USAGE OBJECT REFERENCE ACC.
       01 AMT PIC 9(4) VALUE 0.
       01 R   PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE ACC "NEW" RETURNING A1.
           INVOKE ACC "NEW" RETURNING A2.
           MOVE 10 TO AMT.
           INVOKE A1 "ADDTO" USING AMT RETURNING R.
           DISPLAY "A1=" R.
           MOVE 100 TO AMT.
           INVOKE A2 "ADDTO" USING AMT RETURNING R.
           DISPLAY "A2=" R.
           MOVE 5 TO AMT.
           INVOKE A1 "ADDTO" USING AMT RETURNING R.
           DISPLAY "A1=" R.
           STOP RUN.
       END PROGRAM OOARGS.

       IDENTIFICATION DIVISION.
       CLASS-ID. ACC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. ADDTO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-AMT PIC 9(4).
       01 LK-RES PIC 9(4).
       PROCEDURE DIVISION USING LK-AMT RETURNING LK-RES.
       MAIN.
           ADD LK-AMT TO BAL.
           MOVE BAL TO LK-RES.
       END METHOD ADDTO.
       END OBJECT.
       END CLASS ACC.
