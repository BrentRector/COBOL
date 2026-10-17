*>--- conformance-case
*> id: algebraic-udf-argument
*> rules: [AR-15.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R26 (ledger F76) - 15.43.3 r1 admits a DATA ITEM and "shall not be an integer
      *> function or numeric function"; the exclusion must be written because 8.5.2.12 items 6/7
      *> make FUNCTIONS category numeric too. A user-defined function's result binds to a
      *> synthesized caller temp that wears the same bound shape as declared data, so this folded
      *> +9999 from the TEMP's PICTURE before the IsCompilerTemp discrimination landed.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. R26-FN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R-OUT PIC S9(4).
       PROCEDURE DIVISION RETURNING R-OUT.
           MOVE 7 TO R-OUT.
           GOBACK.
       END FUNCTION R26-FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. R26NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION R26-FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(FUNCTION R26-FN).
           DISPLAY R.
           STOP RUN.
