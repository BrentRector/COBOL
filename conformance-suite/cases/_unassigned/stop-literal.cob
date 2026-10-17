*>--- conformance-case
*> id: stop-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSP01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9 VALUE 5.
PROCEDURE DIVISION.
MAIN.
    STOP "OPERATOR MESSAGE".
    DISPLAY W.
    STOP RUN.
