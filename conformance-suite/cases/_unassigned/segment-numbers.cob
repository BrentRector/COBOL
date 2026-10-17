*>--- conformance-case
*> id: segment-numbers
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSG01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9 VALUE 5.
PROCEDURE DIVISION.
FIRST-PART SECTION 10.
P1.
    DISPLAY W.
SECOND-HALF SECTION 50.
P2.
    STOP RUN.
