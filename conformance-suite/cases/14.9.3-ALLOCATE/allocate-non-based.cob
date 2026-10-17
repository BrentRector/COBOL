*>--- conformance-case
*> id: allocate-non-based
*> rules: [SR-14.9.3.3-1]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC X(5).
01 P USAGE POINTER.
PROCEDURE DIVISION.
MAIN.
    ALLOCATE W RETURNING P.
    STOP RUN.
