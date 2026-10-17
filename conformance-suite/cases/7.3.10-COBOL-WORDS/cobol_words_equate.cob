*>--- conformance-case
*> id: cobol_words_equate
*> rules: [GR-7.3.10.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWEQ.
       PROCEDURE DIVISION.
       MAIN.
           SHOW "EQUATE WORKS".
           STOP RUN.
