*>--- conformance-case
*> id: boolean-ordering-relation
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.8.4.2.2 Format 2: boolean operands compare for equality only - no ordering relation
*> is defined for class boolean (8.8.4.2.1 F1 SR2/SR3 exclude the class from the general relation).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB05.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-B PIC 1(4) VALUE B"0101".
PROCEDURE DIVISION.
MAIN.
    IF W-B < B"1000" DISPLAY "NO".
    STOP RUN.
