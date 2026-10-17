*>--- conformance-case
*> id: move-binary-to-national
*> rules: [SR-14.9.25.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.25.3 SR8: a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sender requires a numeric or
*> numeric-edited receiver; SR10 (Table 16) applies only to cases not covered by SR8.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB07.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-BL USAGE BINARY-LONG VALUE 42.
01 W-N PIC N(5).
PROCEDURE DIVISION.
MAIN.
    MOVE W-BL TO W-N.
    STOP RUN.
