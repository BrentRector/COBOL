*>--- conformance-case
*> id: based-redefines-conflict
*> rules: [SR-13.16.3-3]
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPT06.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC X(5).
01 B REDEFINES W PIC X(5) BASED.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
