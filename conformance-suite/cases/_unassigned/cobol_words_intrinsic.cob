*>--- conformance-case
*> id: cobol_words_intrinsic
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>COBOL-WORDS EQUATE "MAX" WITH "LARGEST"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LARGEST(3 7 5).
           DISPLAY R.
           STOP RUN.
