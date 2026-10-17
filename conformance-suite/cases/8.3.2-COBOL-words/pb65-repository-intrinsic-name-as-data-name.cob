*>--- conformance-case
*> id: pb65-repository-intrinsic-name-as-data-name
*> rules: [SR-8.3.2.1-5, FMT-15.43.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1 rule 5: an intrinsic-function-name "identified in a function-specifier in the REPOSITORY
      *> paragraph" shall not be used as a user-defined word. Under `FUNCTION HIGHEST-ALGEBRAIC INTRINSIC` a table
      *> named HIGHEST-ALGEBRAIC compiled clean and `HIGHEST-ALGEBRAIC(A1)` silently read the table element where
      *> 15.43.4 r2 requires +999 (kb/Work PB65, FMT-15.43.2). COBOLNET1649 at the declaration now.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65NR5DATA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION HIGHEST-ALGEBRAIC INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 HIGHEST-ALGEBRAIC PIC S999 OCCURS 3 TIMES.
       01 A1 PIC S999 VALUE 2.
       01 R1 PIC S9(6)V999.
       PROCEDURE DIVISION.
           MOVE HIGHEST-ALGEBRAIC(A1) TO R1.
           STOP RUN.
