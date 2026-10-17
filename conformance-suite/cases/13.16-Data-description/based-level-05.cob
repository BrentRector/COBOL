*>--- conformance-case
*> id: based-level-05
*> rules: [SR-13.16.3-16]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT07.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 B PIC X(5) BASED.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
