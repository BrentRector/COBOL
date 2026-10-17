*>--- conformance-case
*> id: pic-pointer-conflict
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT05.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P PIC X(4) USAGE POINTER.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
