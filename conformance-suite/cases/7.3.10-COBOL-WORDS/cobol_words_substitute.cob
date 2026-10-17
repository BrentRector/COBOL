*>--- conformance-case
*> id: cobol_words_substitute
*> rules: [GR-7.3.10.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>COBOL-WORDS SUBSTITUTE "DISPLAY" BY "SHOW"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CWSU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DISPLAY PIC X(9) VALUE "AS-A-NAME".
       PROCEDURE DIVISION.
       MAIN.
           SHOW DISPLAY.
           STOP RUN.
