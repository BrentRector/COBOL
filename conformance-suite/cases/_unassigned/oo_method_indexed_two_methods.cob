*>--- conformance-case
*> id: oo_method_indexed_two_methods
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.7.4 GR5 — two METHODS each declaring INDEXED BY IX must get DISTINCT index cells (method-private;
      *> M2-OO-1h step 4). MA sets its IX=2 and INVOKEs MB (which sets ITS IX=3); on return MA's IX must still be 2
      *> (E1(IX)="BB"), NOT torn to 3 by a shared cell (which would give "CC").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOIDX2.
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
           INVOKE A "MA".
           STOP RUN.
       END PROGRAM OOIDX2.

       IDENTIFICATION DIVISION.
       CLASS-ID. AGG INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 T1.
          05 E1 PIC XX OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AA" TO E1(1).
           MOVE "BB" TO E1(2).
           MOVE "CC" TO E1(3).
           SET IX TO 2.
           INVOKE SELF "MB".
           DISPLAY "MA-E1=" E1(IX).
           GOBACK.
       END METHOD MA.
       METHOD-ID. MB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 T2.
          05 E2 PIC XX OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO 3.
           DISPLAY "MB-DONE".
           GOBACK.
       END METHOD MB.
       END OBJECT.
       END CLASS AGG.
