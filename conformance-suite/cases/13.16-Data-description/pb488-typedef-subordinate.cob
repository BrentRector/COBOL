*>--- conformance-case
*> id: pb488-typedef-subordinate
*> rules: [SR-13.16.3-15]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 15's LEVEL-NUMBER half, subordinate spelling - a TYPEDEF clause on an
*> entry that lands under an open parent, whose level-number is by construction not 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-TYPEDEF-SUBORDINATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T IS TYPEDEF PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
