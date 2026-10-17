*>--- conformance-case
*> id: free-non-pointer
*> rules: [SR-14.9.15.3-1]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT04.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC X(5).
PROCEDURE DIVISION.
MAIN.
    FREE W.
    STOP RUN.
