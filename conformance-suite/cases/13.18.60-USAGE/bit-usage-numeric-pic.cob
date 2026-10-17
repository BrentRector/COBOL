*>--- conformance-case
*> id: bit-usage-numeric-pic
*> rules: [SR-13.18.60.3-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.4 SR5: USAGE BIT requires a boolean PICTURE (symbol 1 only).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB04.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-B PIC 9(4) USAGE BIT.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
