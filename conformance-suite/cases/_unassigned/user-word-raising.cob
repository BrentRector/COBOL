*>--- conformance-case
*> id: user-word-raising
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGUW02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 RAISING PIC 9 VALUE 6.
PROCEDURE DIVISION.
MAIN.
    DISPLAY RAISING.
    STOP RUN.
