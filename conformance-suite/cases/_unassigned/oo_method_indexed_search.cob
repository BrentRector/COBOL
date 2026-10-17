*>--- conformance-case
*> id: oo_method_indexed_search
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.7 / §13.18.38 / §14.9.37 — OCCURS … INDEXED BY + SEARCH in a METHOD's LOCAL-STORAGE
      *> (M2-OO-1h step 4). The index-name IX is method-private (§11.7.4 GR5); SEARCH drives the method's OWN
      *> index cell, and SET reads its occurrence number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOIDX.
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
       END PROGRAM OOIDX.

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
       01 TBL.
          05 ELT PIC X OCCURS 5 INDEXED BY IX.
       01 POS PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "A" TO ELT(1).
           MOVE "B" TO ELT(2).
           MOVE "C" TO ELT(3).
           MOVE "D" TO ELT(4).
           MOVE "E" TO ELT(5).
           SET IX TO 1.
           SEARCH ELT
              AT END DISPLAY "NOTFOUND"
              WHEN ELT(IX) = "C"
                 SET POS TO IX
                 DISPLAY "FOUND-AT=" POS
           END-SEARCH.
           GOBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS AGG.
