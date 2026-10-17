*>--- conformance-case
*> id: cobol_words_undefine
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>COBOL-WORDS UNDEFINE "MOVE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWUN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MOVETAB.
          05 MOVE PIC 9 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE MOVE(2) = 5.
           DISPLAY MOVE(2).
           STOP RUN.
