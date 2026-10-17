*>--- conformance-case
*> id: allocate-chars-no-returning
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P USAGE POINTER.
PROCEDURE DIVISION.
MAIN.
    ALLOCATE 10 CHARACTERS.
    STOP RUN.
