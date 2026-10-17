*>--- conformance-case
*> id: oo_method_odo
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.7 / §13.18.38 — OCCURS … DEPENDING ON in a METHOD's LOCAL-STORAGE (M2-OO-1h step 2). The
      *> DEPENDING data-name-1 (CNT) resolves in the METHOD's own scope (§11.7.4 GR5), and a whole-group send of
      *> the table honors the CURRENT extent (GR8) — 3 elements, then 2 after CNT is lowered.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOODO.
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
       END PROGRAM OOODO.

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
       01 CNT PIC 9 VALUE 3.
       01 TBL.
          05 ELT PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "A" TO ELT(1).
           MOVE "B" TO ELT(2).
           MOVE "C" TO ELT(3).
           DISPLAY "TBL=" TBL.
           MOVE 2 TO CNT.
           DISPLAY "TBL2=" TBL.
           GOBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS AGG.
