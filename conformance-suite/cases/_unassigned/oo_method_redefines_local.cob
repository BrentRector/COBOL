*>--- conformance-case
*> id: oo_method_redefines_local
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.7 / §13.18.44 — REDEFINES in a METHOD's LOCAL-STORAGE (M2-OO-1h step 3). A subordinate (02)
      *> REDEFINES over a method record leaf (PARTS over FULL), and a 01-level Tier-B REDEFINES (NCHARS X(4) over
      *> NUM 9(4)) whose string backing is a METHOD LOCAL. Targets resolve within the method's own scope.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOREDF.
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
       END PROGRAM OOREDF.

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
          05 FULL PIC X(6) VALUE "ABCDEF".
          05 PARTS REDEFINES FULL.
             10 P1 PIC XXX.
             10 P2 PIC XXX.
       01 NUM PIC 9(4) VALUE 1234.
       01 NCHARS REDEFINES NUM PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P1=" P1.
           DISPLAY "P2=" P2.
           DISPLAY "NC=" NCHARS.
           GOBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS AGG.
