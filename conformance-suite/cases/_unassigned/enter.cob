*>--- conformance-case
*> id: enter
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGEN01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9 VALUE 7.
PROCEDURE DIVISION.
MAIN.
    ENTER LINKAGE.
    DISPLAY W.
    ENTER COBOL.
    STOP RUN.
